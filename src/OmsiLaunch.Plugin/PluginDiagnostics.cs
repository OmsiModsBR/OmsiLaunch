using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace OmsiLaunch.Plugin;

// The session's diagnostic switches (startup handoff flags, from /trace-plugin
// and /trace-native). Trace events travel through the telemetry ring and are
// written to the session's host trace (.omsilaunch\diagnostics).
internal static class PluginDiagnostics
{
    public static bool PluginTrace { get; private set; }
    public static bool NativeTrace { get; private set; }

    public static void Configure(bool pluginTrace, bool nativeTrace)
    {
        PluginTrace = pluginTrace;
        NativeTrace = nativeTrace;
        if (nativeTrace) unsafe { NativeSetTraceCallback(&OnNativeTrace); }
    }

    // plugin.trace.operation: every runtime operation, its outcome and duration.
    public static void Operation(string operation, bool succeeded, string? code, long elapsedMicroseconds)
    {
        if (!PluginTrace) return;
        CurrentTelemetrySink.Emit("plugin.trace.operation", new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["operation"] = operation, ["succeeded"] = succeeded ? "true" : "false", ["code"] = code ?? "",
            ["elapsed_us"] = elapsedMicroseconds.ToString(CultureInfo.InvariantCulture)
        });
    }

    // native.trace: what the native layer reports (hook installs and removals).
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) })]
    private static unsafe void OnNativeTrace(byte* boundary, byte* data)
    {
        try
        {
            CurrentTelemetrySink.Emit("native.trace", new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["boundary"] = boundary == null ? "" : Marshal.PtrToStringUTF8((nint)boundary) ?? "",
                ["data"] = data == null ? "" : Marshal.PtrToStringUTF8((nint)data) ?? ""
            });
        }
        catch (Exception) { }
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)]
    private static extern unsafe void NativeSetTraceCallback(delegate* unmanaged[Cdecl]<byte*, byte*, void> callback);
}
