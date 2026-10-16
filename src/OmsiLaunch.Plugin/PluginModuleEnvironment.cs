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
    public PluginModuleEnvironment(StartupHandoff handoff, Action<string, IReadOnlyDictionary<string, string>>? emit = null)
    {
        this.handoff = handoff; this.emit = emit ?? CurrentTelemetrySink.Emit;
    }

    public static string InstallationRoot()
    {
        var executable = Environment.ProcessPath;
        return executable is null ? Environment.CurrentDirectory : Path.GetDirectoryName(executable)!;
    }

    public Guid SessionId => handoff.SessionId;
    string IModuleHostEnvironment.InstallationRoot => InstallationRoot();
    public string ProductVersion { get; } = typeof(PluginModuleEnvironment).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
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

    public IModuleAuxiliaryProcess StartAuxiliaryProcess(string path, string arguments) => AuxiliaryProcess.Start(path, arguments);

    public IModuleNativeComponent StartNativeComponent(ModuleManifest manifest, string directory, ModuleComponent component, Func<string, string?> values) =>
        NativeModuleComponent.Start(manifest, Path.GetFullPath(Path.Combine(directory, component.Path)), emit, values);

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

        public static AuxiliaryProcess Start(string path, string arguments)
        {
            var job = CreateJobObject(0, null);
            if (job == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
            var limits = new JobObjectExtendedLimitInformation { BasicLimitInformation = new() { LimitFlags = JobObjectLimitKillOnJobClose } };
            if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ref limits, Marshal.SizeOf<JobObjectExtendedLimitInformation>())) { var error = Marshal.GetLastWin32Error(); CloseHandle(job); throw new Win32Exception(error); }
            Process? process = null;
            try
            {
                process = Process.Start(new ProcessStartInfo(path, arguments) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = Path.GetDirectoryName(path)! }) ?? throw new InvalidOperationException("the auxiliary process did not start");
                if (!AssignProcessToJobObject(job, process.Handle)) throw new Win32Exception(Marshal.GetLastWin32Error());
                return new AuxiliaryProcess(process, job);
            }
            catch
            {
                try { process?.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                process?.Dispose(); CloseHandle(job);
                throw;
            }
        }

        public void Dispose()
        {
            try { if (!process.HasExited) { process.Kill(entireProcessTree: true); process.WaitForExit(2000); } }
            catch (InvalidOperationException) { }
            // Closing the job terminates anything the process left in it.
            CloseHandle(job);
        }

        private const uint JobObjectLimitKillOnJobClose = 0x2000;
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
