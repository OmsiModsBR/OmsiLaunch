using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace OmsiLaunch.Process;

// A runtime module's session program (bridge protocol 2): an executable from
// the module's own folder that the controller starts before OMSI and that
// ends with the session. It is created suspended and put in a job object
// before it runs a single instruction. The job ends it when the session ends,
// allows no second process (it cannot start other programs), and refuses
// changes to system parameters, display settings, the desktop and logoff or
// shutdown. It inherits exactly two handles: its stdin (JSON lines from the
// controller) and its stdout (JSON lines to the controller).
public sealed class ModuleSessionProcess : IDisposable
{
    public const int MaxLineBytes = 64 * 1024;
    private readonly SafeProcessHandle process;
    private readonly nint job;
    private readonly FileStream input;
    private readonly Thread reader;
    private readonly object writeGate = new();
    private bool disposed;
    // Kept when the handle is released, so status stays readable afterwards.
    private int? finalExitCode;

    private ModuleSessionProcess(SafeProcessHandle process, nint job, int processId, FileStream input, FileStream output, Action<string> onLine, string name)
    {
        this.process = process; this.job = job; ProcessId = processId; this.input = input;
        reader = new Thread(() => ReadLines(output, onLine)) { IsBackground = true, Name = "OmsiLaunch module session program: " + name };
        reader.Start();
    }

    public int ProcessId { get; }
    public bool HasExited => disposed || WaitForSingleObject(process, 0) == WaitObject0;

    public int? ExitCode => disposed ? finalExitCode : HasExited && GetExitCodeProcess(process, out var code) ? unchecked((int)code) : null;

    // executable must be the pinned file the caller verified. The command line
    // is fixed: "<executable>" --omsilaunch-session <session id>.
    public static ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)
    {
        var job = CreateJobObjectW(0, null);
        if (job == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        SafeFileHandle? childInput = null, parentInput = null, parentOutput = null, childOutput = null;
        nint attributes = 0, handleList = 0;
        var information = default(ProcessInformation);
        try
        {
            ConfigureJob(job);
            var inherit = new SecurityAttributes { Length = Marshal.SizeOf<SecurityAttributes>(), InheritHandle = 1 };
            if (!CreatePipe(out childInput, out parentInput, ref inherit, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
            if (!CreatePipe(out parentOutput, out childOutput, ref inherit, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());
            // The controller's ends stay in the controller.
            if (!SetHandleInformation(parentInput, HandleFlagInherit, 0) || !SetHandleInformation(parentOutput, HandleFlagInherit, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());

            // Only the two pipe ends are inherited, whatever else is inheritable.
            var size = (nint)0;
            InitializeProcThreadAttributeList(0, 1, 0, ref size);
            attributes = Marshal.AllocHGlobal(size);
            if (!InitializeProcThreadAttributeList(attributes, 1, 0, ref size)) throw new Win32Exception(Marshal.GetLastWin32Error());
            handleList = Marshal.AllocHGlobal(2 * IntPtr.Size);
            Marshal.WriteIntPtr(handleList, 0, childInput.DangerousGetHandle());
            Marshal.WriteIntPtr(handleList, IntPtr.Size, childOutput.DangerousGetHandle());
            if (!UpdateProcThreadAttribute(attributes, 0, ProcThreadAttributeHandleList, handleList, 2 * IntPtr.Size, 0, 0)) throw new Win32Exception(Marshal.GetLastWin32Error());

            var startup = new StartupInfoEx { AttributeList = attributes };
            startup.StartupInfo.Size = Marshal.SizeOf<StartupInfoEx>();
            startup.StartupInfo.Flags = StartfUseStdHandles;
            startup.StartupInfo.StdInput = childInput.DangerousGetHandle();
            startup.StartupInfo.StdOutput = childOutput.DangerousGetHandle();
            startup.StartupInfo.StdError = 0;
            var command = new StringBuilder("\"" + executable + "\" --omsilaunch-session " + sessionId.ToString("D"));
            if (!CreateProcessW(executable, command, 0, 0, true, CreateSuspended | ExtendedStartupInfoPresent | CreateNoWindow, 0, workingDirectory, ref startup, out information))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            var processHandle = new SafeProcessHandle(information.Process, ownsHandle: true);
            if (!AssignProcessToJobObject(job, information.Process))
            {
                var error = Marshal.GetLastWin32Error();
                TerminateProcess(information.Process, 1);
                processHandle.Dispose(); CloseHandle(information.Thread);
                throw new Win32Exception(error);
            }
            if (ResumeThread(information.Thread) == uint.MaxValue)
            {
                var error = Marshal.GetLastWin32Error();
                TerminateJobObject(job, 1);
                processHandle.Dispose(); CloseHandle(information.Thread);
                throw new Win32Exception(error);
            }
            CloseHandle(information.Thread);
            // The child holds its ends now; closing ours makes EOF reach both sides.
            childInput.Dispose(); childOutput.Dispose();
            var inputStream = new FileStream(parentInput, FileAccess.Write, 1, false);
            var outputStream = new FileStream(parentOutput, FileAccess.Read, 4096, false);
            return new ModuleSessionProcess(processHandle, job, information.ProcessId, inputStream, outputStream, onLine, Path.GetFileName(executable));
        }
        catch
        {
            childInput?.Dispose(); childOutput?.Dispose(); parentInput?.Dispose(); parentOutput?.Dispose();
            CloseHandle(job);
            throw;
        }
        finally
        {
            if (attributes != 0) { DeleteProcThreadAttributeList(attributes); Marshal.FreeHGlobal(attributes); }
            if (handleList != 0) Marshal.FreeHGlobal(handleList);
        }
    }

    // One JSON line to the program; false when it no longer reads.
    public bool TryWriteLine(string line)
    {
        var bytes = Encoding.UTF8.GetBytes(line.Replace('\n', ' ').Replace('\r', ' ') + "\n");
        lock (writeGate)
        {
            if (disposed) return false;
            try { input.Write(bytes, 0, bytes.Length); input.Flush(); return true; }
            catch (IOException) { return false; }
            catch (ObjectDisposedException) { return false; }
        }
    }

    public bool WaitForExit(TimeSpan timeout) => disposed || WaitForSingleObject(process, (uint)Math.Clamp(timeout.TotalMilliseconds, 0, int.MaxValue)) == WaitObject0;

    // Ends the program and anything left in its job.
    public void Kill() { lock (writeGate) if (!disposed) TerminateJobObject(job, 1); }

    public void Dispose()
    {
        lock (writeGate)
        {
            if (disposed) return;
            try { input.Dispose(); } catch (IOException) { }
            // Closing the job (kill on close) ends whatever is still running in it.
            CloseHandle(job);
            WaitForSingleObject(process, 2000);
            finalExitCode = WaitForSingleObject(process, 0) == WaitObject0 && GetExitCodeProcess(process, out var code) ? unchecked((int)code) : null;
            disposed = true;
        }
        process.Dispose();
        reader.Join(TimeSpan.FromSeconds(2));
    }

    // Lines are UTF-8 and end with LF; a line longer than MaxLineBytes is
    // dropped (the reader resynchronizes at the next LF).
    private static void ReadLines(FileStream output, Action<string> onLine)
    {
        var line = new List<byte>(256);
        var buffer = new byte[4096];
        var dropping = false;
        try
        {
            using (output)
            {
                int count;
                while ((count = output.Read(buffer, 0, buffer.Length)) > 0)
                    for (var index = 0; index < count; index++)
                    {
                        var value = buffer[index];
                        if (value == (byte)'\n')
                        {
                            if (!dropping && line.Count != 0) Deliver(onLine, Encoding.UTF8.GetString(line.ToArray()).TrimEnd('\r'));
                            line.Clear(); dropping = false;
                            continue;
                        }
                        if (dropping) continue;
                        if (line.Count >= MaxLineBytes) { line.Clear(); dropping = true; continue; }
                        line.Add(value);
                    }
                if (!dropping && line.Count != 0) Deliver(onLine, Encoding.UTF8.GetString(line.ToArray()).TrimEnd('\r'));
            }
        }
        catch (IOException) { }
        catch (ObjectDisposedException) { }
    }

    private static void Deliver(Action<string> onLine, string line)
    {
        try { onLine(line); } catch (Exception) { }
    }

    private static void ConfigureJob(nint job)
    {
        var limits = new JobObjectExtendedLimitInformation
        {
            BasicLimitInformation = new() { LimitFlags = JobObjectLimitKillOnJobClose | JobObjectLimitActiveProcess | JobObjectLimitDieOnUnhandledException, ActiveProcessLimit = 1 }
        };
        if (!SetInformationJobObject(job, JobObjectExtendedLimitInformationClass, ref limits, Marshal.SizeOf<JobObjectExtendedLimitInformation>())) throw new Win32Exception(Marshal.GetLastWin32Error());
        var ui = new JobObjectBasicUiRestrictions { UiRestrictionsClass = UiLimitSystemParameters | UiLimitDisplaySettings | UiLimitDesktop | UiLimitExitWindows };
        if (!SetInformationJobObjectUi(job, JobObjectBasicUiRestrictionsClass, ref ui, Marshal.SizeOf<JobObjectBasicUiRestrictions>())) throw new Win32Exception(Marshal.GetLastWin32Error());
    }

    private const uint WaitObject0 = 0;
    private const int HandleFlagInherit = 1;
    private const uint CreateSuspended = 0x00000004, CreateNoWindow = 0x08000000, ExtendedStartupInfoPresent = 0x00080000;
    private const int StartfUseStdHandles = 0x00000100;
    private static readonly nint ProcThreadAttributeHandleList = 0x00020002;
    private const uint JobObjectLimitActiveProcess = 0x00000008, JobObjectLimitDieOnUnhandledException = 0x00000400, JobObjectLimitKillOnJobClose = 0x00002000;
    private const uint UiLimitSystemParameters = 0x00000008, UiLimitDisplaySettings = 0x00000010, UiLimitDesktop = 0x00000040, UiLimitExitWindows = 0x00000080;
    private const int JobObjectBasicUiRestrictionsClass = 4, JobObjectExtendedLimitInformationClass = 9;

    [StructLayout(LayoutKind.Sequential)] private struct SecurityAttributes { public int Length; public nint SecurityDescriptor; public int InheritHandle; }
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size; public string? Reserved; public string? Desktop; public string? Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, Reserved2Size; public nint Reserved2, StdInput, StdOutput, StdError;
    }
    [StructLayout(LayoutKind.Sequential)] private struct StartupInfoEx { public StartupInfo StartupInfo; public nint AttributeList; }
    [StructLayout(LayoutKind.Sequential)] private struct ProcessInformation { public nint Process; public nint Thread; public int ProcessId; public int ThreadId; }
    [StructLayout(LayoutKind.Sequential)] private struct JobObjectBasicLimitInformation { public long PerProcessUserTimeLimit; public long PerJobUserTimeLimit; public uint LimitFlags; public nuint MinimumWorkingSetSize; public nuint MaximumWorkingSetSize; public uint ActiveProcessLimit; public nuint Affinity; public uint PriorityClass; public uint SchedulingClass; }
    [StructLayout(LayoutKind.Sequential)] private struct IoCounters { public ulong ReadOperationCount, WriteOperationCount, OtherOperationCount, ReadTransferCount, WriteTransferCount, OtherTransferCount; }
    [StructLayout(LayoutKind.Sequential)] private struct JobObjectExtendedLimitInformation { public JobObjectBasicLimitInformation BasicLimitInformation; public IoCounters IoInfo; public nuint ProcessMemoryLimit; public nuint JobMemoryLimit; public nuint PeakProcessMemoryUsed; public nuint PeakJobMemoryUsed; }
    [StructLayout(LayoutKind.Sequential)] private struct JobObjectBasicUiRestrictions { public uint UiRestrictionsClass; }

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern nint CreateJobObjectW(nint attributes, string? name);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetInformationJobObject(nint job, int infoClass, ref JobObjectExtendedLimitInformation info, int length);
    [DllImport("kernel32.dll", SetLastError = true, EntryPoint = "SetInformationJobObject")] private static extern bool SetInformationJobObjectUi(nint job, int infoClass, ref JobObjectBasicUiRestrictions info, int length);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool AssignProcessToJobObject(nint job, nint process);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateJobObject(nint job, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CreatePipe(out SafeFileHandle read, out SafeFileHandle write, ref SecurityAttributes attributes, int size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool SetHandleInformation(SafeHandle handle, int mask, int flags);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool InitializeProcThreadAttributeList(nint list, int count, int flags, ref nint size);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool UpdateProcThreadAttribute(nint list, uint flags, nint attribute, nint value, nint size, nint previous, nint returnSize);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern void DeleteProcThreadAttributeList(nint list);
    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)] private static extern bool CreateProcessW(string application, StringBuilder command, nint processAttributes, nint threadAttributes, bool inheritHandles, uint flags, nint environment, string currentDirectory, ref StartupInfoEx startup, out ProcessInformation information);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint ResumeThread(nint thread);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool TerminateProcess(nint process, uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern uint WaitForSingleObject(SafeProcessHandle handle, uint milliseconds);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetExitCodeProcess(SafeProcessHandle handle, out uint exitCode);
    [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(nint handle);
}
