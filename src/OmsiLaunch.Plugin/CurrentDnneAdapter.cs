using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using DNNE;
using OmsiLaunch.Api;

namespace OmsiLaunch.Plugin;

// Current-only adapter: exports OMSI's plugin ABI and schedules PluginRuntime
// on the original OMSI UI thread. It owns no launch semantics.
public static class CurrentDnneAdapter
{
    // Created in PluginStart after the native targets are resolved: the
    // runtime control reads resolved addresses, never fixed constants.
    private static PluginRuntime? runtime;
    private static PluginRuntime Runtime => runtime ?? throw new InvalidOperationException("The plugin runtime is not started.");
    private static readonly TimerProc TimerCallback = OnTimer;
    private static nuint timer;
    private static bool runtimePolling;
    private static int pluginStartInvoked;

    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "PluginStart")]
    public static void PluginStart(nint owner)
    {
        // OMSI can invoke PluginStart more than once during startup. The first
        // invocation owns the one-shot headless hook; re-entering after that
        // hook changes the profiled VMT would otherwise look like a bad build.
        if (Interlocked.Exchange(ref pluginStartInvoked, 1) != 0) return;
        // Resolve and verify every native target before anything is patched,
        // so the code identity and every guard see the pristine image.
        try { CurrentTelemetrySink.Emit("native.capabilities", PluginNativeTargets.Report(PluginNativeTargets.Resolve())); }
        catch (Exception exception) { CurrentTelemetrySink.Emit("native.resolution.failed", new Dictionary<string, string> { ["detail"] = exception.Message }); }
        var root = PluginModuleEnvironment.InstallationRoot();
        var control = new CurrentRuntimeControl();
        runtime = new PluginRuntime(new NativeServices(), CurrentTelemetrySink.Emit, control, handoff => new Runtime.Modules.ModuleHost(new PluginModuleEnvironment(handoff, null, control)), root);
        if (string.Equals(Environment.GetEnvironmentVariable("OMSILAUNCH_INTERNET_TEXTURES_MODE"), "Disabled", StringComparison.OrdinalIgnoreCase))
        {
            if (!PluginNativeTargets.IsAvailable("internet-textures.disabled"))
                CurrentTelemetrySink.Emit("internet-textures.suppression.failed", new Dictionary<string, string> { ["reason"] = "capability-unresolved" });
            else if (NativeSuppressInternetTextures() == 0)
                CurrentTelemetrySink.Emit("internet-textures.suppression.failed", new Dictionary<string, string>());
            else
                CurrentTelemetrySink.Emit("internet-textures.suppressed", new Dictionary<string, string>());
        }
        Runtime.Start(ScheduleOnUiThread);
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "PluginFinalize")]
    public static void PluginFinalize()
    {
        if (timer != 0) { KillTimer(0, timer); timer = 0; }
        runtimePolling = false;
        runtime?.Shutdown();
        NativeRestoreInternetTextures();
        Volatile.Write(ref pluginStartInvoked, 0);
    }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessVariable")]
    public static void AccessVariable(ushort variableIndex, nint value, nint writeValue) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessTrigger")]
    public static void AccessTrigger(ushort variableIndex, nint triggerScript) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessStringVariable")]
    public static void AccessStringVariable(ushort variableIndex, nint value, nint writeValue) { }
    [UnmanagedCallersOnly(CallConvs = new[] { typeof(CallConvCdecl) }, EntryPoint = "AccessSystemVariable")]
    public static void AccessSystemVariable(ushort variableIndex, nint value, nint writeValue) { }

    private static void ScheduleOnUiThread(Action action)
    {
        pending = action; runtimePolling = false; timer = SetTimer(0, 0, 1, TimerCallback);
    }
    private static Action? pending;
    private static void OnTimer(nint window, uint message, nuint id, uint time)
    {
        var hadPendingWorld = Runtime.HasPendingWorld;
        pending?.Invoke();
        // Startup is retried only while PluginRuntime owns a pending world;
        // runtime polling must not replay a completed startup continuation.
        if (!Runtime.HasPendingWorld) pending = null;
        // Runtime commands and lifecycle polling start on the tick after the
        // world start, never inside the startup continuation.
        if (!hadPendingWorld) Runtime.PollRuntimeCommands();
        if (!Runtime.HasPendingWorld && Runtime.HasRuntimeCommandChannel && !runtimePolling && timer != 0)
        {
            KillTimer(0, timer); timer = SetTimer(0, 0, 50, TimerCallback); runtimePolling = true;
        }
        if (!Runtime.HasPendingWorld && !Runtime.HasRuntimeCommandChannel && timer != 0) { pending = null; KillTimer(0, timer); timer = 0; }
    }

    private sealed class NativeServices : IPluginNativeServices
    {
        // A start needs the headless-start capability and the world-mode
        // capability, resolved and verified on this build.
        public bool ValidateStart(WorldMode mode, out string missing)
        {
            var needed = mode switch
            {
                WorldMode.SavedSituation => new[] { "boot.headless-start", "world.saved-situation" },
                WorldMode.LastMapState => new[] { "boot.headless-start", "world.last-map-state" },
                _ => new[] { "boot.headless-start", "world.new-map" }
            };
            missing = string.Join(",", needed.Where(capability => !PluginNativeTargets.IsAvailable(capability)));
            return missing.Length == 0;
        }
        // Live state of the Start form and its unpatched hook sites.
        public bool CheckLiveStartForm() => NativeValidateBuild() != 0;
        public bool ArmHeadlessStart() => NativeArmHeadlessStart() != 0;
        public int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity) => NativeStartNewMap(mapIdentity, presentedEntrypointIndex, entrypointIdentity);
        public int StartLastMapState(string mapIdentity) => NativeStartLastMapState(mapIdentity);
        public bool SetStartDateTime(StartDateTime start) => NativeSetStartDateTime(
            (start.Year is null ? 0 : 0x01) | (start.DayOfYear is null ? 0 : 0x02) | (start.Hour is null ? 0 : 0x04) | (start.Minute is null ? 0 : 0x08) | (start.Second is null ? 0 : 0x10),
            start.Year ?? 0, start.DayOfYear ?? 0, start.Hour ?? 0, start.Minute ?? 0, start.Second ?? 0) == 1;
        public int StartSavedSituation(string situationIdentity) => NativeStartSavedSituation(situationIdentity);
        public bool StartWeatherAvailable() => PluginNativeTargets.IsAvailable("world.start-weather");
        public int ApplyStartWeather(StartWeather weather) => NativeApplyStartWeather((int)weather.Mode, weather.Preset, weather.Icao);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeApplyStartWeather(int mode, string preset, string icao);
        public bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection)
        {
            var presented = new StringBuilder(512); var raw = new StringBuilder(512);
            if (NativeGetLastEntrypointSelection(out var presentedIndex, out var rawIndex, presented, presented.Capacity, raw, raw.Capacity) == 0)
            {
                selection = null!; return false;
            }
            selection = new(presentedIndex, rawIndex, presented.ToString(), raw.ToString()); return true;
        }
        public void InstallMainThreadGateway() => NativeInstallMainThreadGateway();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeValidateBuild();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeArmHeadlessStart();
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeStartNewMap(string mapIdentity, int presentedIndex, string entrypointIdentity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeStartLastMapState(string mapIdentity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeSetStartDateTime(int present, int year, int dayOfYear, int hour, int minute, int second);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeStartSavedSituation(string situationIdentity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Unicode)] private static extern int NativeGetLastEntrypointSelection(out int presentedIndex, out int rawIndex, StringBuilder presentedName, int presentedNameCapacity, StringBuilder rawName, int rawNameCapacity);
        [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeInstallMainThreadGateway();
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeSuppressInternetTextures();
    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern void NativeRestoreInternetTextures();

    private delegate void TimerProc(nint window, uint message, nuint id, uint time);
    [DllImport("user32.dll", SetLastError = true)] private static extern nuint SetTimer(nint window, nuint id, uint elapsedMilliseconds, TimerProc callback);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool KillTimer(nint window, nuint id);
}
