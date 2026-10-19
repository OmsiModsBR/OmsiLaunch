using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using OmsiLaunch.Api;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Plugin;

// The bridge side of Runtime Modules v1 inside OMSI: capability answers from
// the in-process resolution, telemetry through the event ring, managed
// components in their own load context, auxiliary processes bound to OMSI by
// a kill-on-close job object.
internal sealed class PluginModuleEnvironment : IModuleHostEnvironment
{
    private readonly StartupHandoff handoff;
    private readonly Action<string, IReadOnlyDictionary<string, string>> emit;
    private readonly IPluginRuntimeControl? control;
    private ulong requestId;
    // control runs the catalog operations a module declared (runtime_operations);
    // without it every such call answers OL_E_RUNTIME_OPERATION_UNKNOWN.
    public PluginModuleEnvironment(StartupHandoff handoff, Action<string, IReadOnlyDictionary<string, string>>? emit = null, IPluginRuntimeControl? control = null)
    {
        this.handoff = handoff; this.emit = emit ?? CurrentTelemetrySink.Emit; this.control = control;
    }

    public static string InstallationRoot()
    {
        var executable = Environment.ProcessPath;
        return executable is null ? Environment.CurrentDirectory : Path.GetDirectoryName(executable)!;
    }

    public Guid SessionId => handoff.SessionId;
    string IModuleHostEnvironment.InstallationRoot => InstallationRoot();
    public string ProductVersion { get; } = typeof(PluginModuleEnvironment).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    public string OmsiVersion => PluginOmsiVersion.Value;
    public bool InputAvailable => PluginNativeTargets.IsAvailable("ui.input.capture");
    public bool SetInputCapture(bool enabled) => InputAvailable && NativeInputCapture(enabled ? 1 : 0) == 0;
    public int DrainInput(List<ModuleInputEvent> events)
    {
        var buffer = new NativeInputEvent[256];
        var count = NativeInputDrain(buffer, buffer.Length);
        for (var index = 0; index < count; index++)
        {
            var e = buffer[index];
            if (e.Kind is >= 1 and <= 8 && e.Button <= 3) events.Add(new((ModuleInputKind)e.Kind, e.Key, e.X, e.Y, (ModuleMouseButton)e.Button, e.Delta, e.Modifiers));
        }
        return count;
    }
    [StructLayout(LayoutKind.Sequential)] private struct NativeInputEvent { public uint Kind; public int Key, X, Y; public uint Button; public int Delta, Modifiers; }
    // 0 done; 1 not available; 2 not on the thread that started the capture; 3 already capturing.
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeInputCapture(int enable);
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeInputDrain([Out] NativeInputEvent[] events, int capacity);
    public ModuleCallResult ExecuteRuntimeOperation(string operation, IReadOnlyDictionary<string, string>? arguments)
    {
        if (control is null) return new(false, PublicErrorCodes.RUNTIME_OPERATION_UNKNOWN, new Dictionary<string, string>(StringComparer.Ordinal) { ["detail"] = "no runtime control in this session" });
        var result = control.Execute(new RuntimeCommand(handoff.SessionId, ++requestId, operation, arguments));
        return new(result.Succeeded, result.ErrorCode, result.Values ?? new Dictionary<string, string>(StringComparer.Ordinal));
    }
    public bool IsPluginLibraryLoaded(string fileName) => GetModuleHandleW(fileName) != 0;
    public bool IsCapabilityAvailable(string capability) => RuntimeModuleProtocol.BridgeFeatures.Contains(capability)
        ? !RuntimeModuleProtocol.BridgeFeatureNeeds.TryGetValue(capability, out var need) || PluginNativeTargets.IsAvailable(need)
        : PluginNativeTargets.IsAvailable(capability);
    // The map the start requested (NEW_MAP); a saved situation's map was
    // checked at planning.
    public string? LoadedMapIdentity => handoff.WorldMode == WorldMode.NewMap && handoff.MapIdentity.Length > 0 ? handoff.MapIdentity : null;
    public long TickMilliseconds => Environment.TickCount64;
    public void Emit(string name, IReadOnlyDictionary<string, string> data) => emit(name, data);

    public IRuntimeModule CreateModule(ModuleManifest manifest, string directory)
    {
        var component = manifest.ManagedComponent;
        var context = new ModuleLoadContext(manifest.Id, directory);
        var assembly = context.LoadFromAssemblyPath(Path.GetFullPath(Path.Combine(directory, component.Path)));
        var type = assembly.GetType(component.Entry!, throwOnError: true)!;
        if (!typeof(IRuntimeModule).IsAssignableFrom(type)) throw new InvalidOperationException(component.Entry + " does not implement IRuntimeModule");
        return (IRuntimeModule)Activator.CreateInstance(type)!;
    }

    public IModuleAuxiliaryProcess StartAuxiliaryProcess(string path, string arguments, ModuleAuxiliaryOptions options) => AuxiliaryProcess.Start(path, arguments, options);

    public IModuleNativeComponent StartNativeComponent(ModuleManifest manifest, string directory, ModuleComponent component, Func<string, string?> values, Func<string, IReadOnlyDictionary<string, string>?, ModuleCallResult> runtime) =>
        NativeModuleComponent.Start(manifest, Path.GetFullPath(Path.Combine(directory, component.Path)), emit, values, runtime);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string name);

    // Module dependencies resolve from the module directory; the contracts
    // (OmsiLaunch.Api, OmsiLaunch.Runtime.Modules) are always the bridge's own
    // loaded assemblies, so the module implements the very IRuntimeModule the
    // bridge knows. They are returned directly: DNNE loads the plugin into an
    // isolated load context, where the default context cannot find them.
    internal sealed class ModuleLoadContext : AssemblyLoadContext
    {
        private static readonly Assembly Contracts = typeof(IRuntimeModule).Assembly;
        private static readonly Assembly Api = typeof(PublicErrorCodes).Assembly;
        private readonly string directory;
        public ModuleLoadContext(string id, string directory) : base("OmsiLaunch.Module." + id) => this.directory = directory;
        protected override Assembly? Load(AssemblyName name)
        {
            if (string.Equals(name.Name, Contracts.GetName().Name, StringComparison.OrdinalIgnoreCase)) return Contracts;
            if (string.Equals(name.Name, Api.GetName().Name, StringComparison.OrdinalIgnoreCase)) return Api;
            var candidate = Path.Combine(directory, name.Name + ".dll");
            return File.Exists(candidate) ? LoadFromAssemblyPath(candidate) : null;
        }
    }

    private sealed class AuxiliaryProcess : IModuleAuxiliaryProcess
    {
        private readonly Process process; private readonly nint job;
        private AuxiliaryProcess(Process process, nint job) { this.process = process; this.job = job; }
        public int ProcessId => process.Id;
        public bool HasExited { get { try { return process.HasExited; } catch (InvalidOperationException) { return true; } } }

        // The program is created suspended, placed in a kill-on-close job object
        // (with the manifest's priority and memory limit) and only then resumed:
        // nothing it starts can run outside the job, and the limits hold from
        // its first instruction. The job has no process-count limit, so a
        // program that needs children (a web view host) can have them, all in
        // the job.
        public static AuxiliaryProcess Start(string path, string arguments, ModuleAuxiliaryOptions options)
        {
            var job = CreateJobObject(0, null);
            if (job == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            var limits = new JobObjectExtendedLimitInformation { BasicLimitInformation = new() { LimitFlags = JobObjectLimitKillOnJobClose } };
            if (options.Priority != ModuleAuxiliaryPriority.Normal)
            {
                limits.BasicLimitInformation.LimitFlags |= JobObjectLimitPriorityClass;
                limits.BasicLimitInformation.PriorityClass = options.Priority == ModuleAuxiliaryPriority.Idle ? IdlePriorityClass : BelowNormalPriorityClass;
            }
            if (options.MemoryLimitMegabytes > 0)
            {
                limits.BasicLimitInformation.LimitFlags |= JobObjectLimitJobMemory;
                limits.JobMemoryLimit = (nuint)options.MemoryLimitMegabytes * 1024 * 1024;
            }
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ref limits, Marshal.SizeOf<JobObjectExtendedLimitInformation>())) { var error = Marshal.GetLastWin32Error(); CloseHandle(job); throw new Win32Exception(error); }
            var startup = new StartupInfo { Size = (uint)Marshal.SizeOf<StartupInfo>() };
            var commandLine = new System.Text.StringBuilder("\"" + path + "\" " + arguments);
            if (!CreateProcessW(null, commandLine, 0, 0, false, CreateSuspended | CreateNoWindow, 0, Path.GetDirectoryName(path)!, ref startup, out var created)) { var error = Marshal.GetLastWin32Error(); CloseHandle(job); throw new Win32Exception(error); }
            try
            {
                if (!AssignProcessToJobObject(job, created.Process)) throw new Win32Exception(Marshal.GetLastWin32Error());
                if (ResumeThread(created.Thread) == uint.MaxValue) throw new Win32Exception(Marshal.GetLastWin32Error());
                var process = Process.GetProcessById((int)created.ProcessId);
                return new AuxiliaryProcess(process, job);
            }
            catch
            {
                TerminateProcess(created.Process, 1);
                CloseHandle(job);
                throw;
            }
            finally { CloseHandle(created.Thread); CloseHandle(created.Process); }
        }

        public void Dispose()
        {
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(2000); } }
            catch (InvalidOperationException) { }
            // Closing the job terminates anything the process left in it.
            CloseHandle(job);
        }

        private const uint JobObjectLimitKillOnJobClose = 0x2000, JobObjectLimitPriorityClass = 0x20, JobObjectLimitJobMemory = 0x200;
        private const uint BelowNormalPriorityClass = 0x4000, IdlePriorityClass = 0x40, CreateSuspended = 0x4, CreateNoWindow = 0x08000000;
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct StartupInfo { public uint Size; public string? Reserved, Desktop, Title; public uint X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags; public ushort ShowWindow, Reserved2; public nint Reserved2Pointer, StdInput, StdOutput, StdError; }
        [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public nint Process, Thread; public uint ProcessId, ThreadId; }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool CreateProcessW(string? application, System.Text.StringBuilder commandLine, nint processAttributes, nint threadAttributes, bool inheritHandles, uint flags, nint environment, string? currentDirectory, ref StartupInfo startup, out ProcessInformation information);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(nint thread);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(nint process, uint exitCode);
        private const int JobObjectExtendedLimitInformationClass = 9;
        [StructLayout(LayoutKind.Sequential)] private struct JobObjectBasicLimitInformation { public long PerProcessUserTimeLimit; public long PerJobUserTimeLimit; public uint LimitFlags; public nuint MinimumWorkingSetSize; public nuint MaximumWorkingSetSize; public uint ActiveProcessLimit; public nuint Affinity; public uint PriorityClass; public uint SchedulingClass; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
        [StructLayout(LayoutKind.Sequential)] private struct JobObjectExtendedLimitInformation { public JobObjectBasicLimitInformation BasicLimitInformation; public IoCounters IoInfo; public nuint ProcessMemoryLimit; public nuint JobMemoryLimit; public nuint PeakProcessMemoryUsed; public nuint PeakJobMemoryUsed; }
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern nint CreateJobObject(nint attributes, string? name);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(nint job, int infoClass, ref JobObjectExtendedLimitInformation info, int length);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(nint job, nint process);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(nint handle);
    }
}
