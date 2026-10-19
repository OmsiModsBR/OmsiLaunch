using System.IO.MemoryMappedFiles;
using System.Buffers.Binary;
using OmsiLaunch.Api;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Plugin;

public interface IPluginNativeServices
{
    bool ValidateStart(WorldMode mode, out string missing);
    bool CheckLiveStartForm();
    bool ArmHeadlessStart();
    int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity);
    // The start date/time the next StartNewMap writes before NewSituation.
    // False when the targets it needs are not resolved on this build.
    bool SetStartDateTime(StartDateTime start) => false;
    // LAST MAP STATE (/last): 0 success; 1 unavailable, 2 no last state of the map, 3 not accepted.
    int StartLastMapState(string mapIdentity) => 1;
    int StartSavedSituation(string situationIdentity);
    // The start weather, applied after the world started: 0 applied; 1
    // unavailable; 2 OMSI has no ICAO configured; 3 invalid request.
    bool StartWeatherAvailable() => false;
    int ApplyStartWeather(StartWeather weather) => 1;
    bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection);
    void InstallMainThreadGateway();
}

public sealed record NativeEntrypointSelection(int PresentedIndex, int RawIndex, string PresentedLabel, string RawLabel);

// DNNE-independent runtime semantics. The Current adapter supplies scheduling
// onto OMSI's UI message pump and can later be replaced by a legacy adapter.
public sealed class PluginRuntime
{
    private readonly IPluginNativeServices native;
    private readonly Action<string, IReadOnlyDictionary<string, string>> telemetry;
    private readonly IPluginRuntimeControl? runtimeControl;
    // Creates the module host for a session (Beta 4 Runtime Modules); null
    // disables modules (tests of the start path).
    private readonly Func<StartupHandoff, ModuleHost>? moduleHostFactory;
    private readonly string installationRoot;
    private ModuleHost? modules;
    private StartupHandoff? pending;
    private CurrentRuntimeCommandMailbox? mailbox;
    private bool executing;
    private int startupAccepted;
    public bool HasPendingWorld => pending is not null;
    public bool HasRuntimeCommandChannel => mailbox is not null;

    public PluginRuntime(IPluginNativeServices native, Action<string, IReadOnlyDictionary<string, string>>? telemetry = null, IPluginRuntimeControl? runtimeControl = null, Func<StartupHandoff, ModuleHost>? moduleHostFactory = null, string? installationRoot = null)
    {
        this.native = native; this.runtimeControl = runtimeControl; this.moduleHostFactory = moduleHostFactory;
        this.installationRoot = installationRoot ?? Environment.CurrentDirectory;
        this.telemetry = telemetry ?? ((_, _) => { });
    }
    public ModuleHost? Modules => modules;

    public bool Start(Action<Action> scheduleOnUiThread)
    {
        var name = Environment.GetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME");
        if (string.IsNullOrWhiteSpace(name) || !TryReadHandoff(name, out var handoff)) { Emit("plugin.handoff.invalid"); return false; }
        // OMSI may notify the same plugin more than once during bootstrap. A
        // successful handoff owns a one-shot native hook, so only its first
        // accepted Start may validate and arm that hook.
        if (Interlocked.CompareExchange(ref startupAccepted, 1, 0) != 0) return true;
        try
        {
        Emit("plugin.started", ("session_id", handoff.SessionId.ToString("D")));
        PluginDiagnostics.Configure(handoff.PluginTrace, handoff.NativeTrace);
        // A start date/time is resolved by the controller and applies to NEW_MAP only.
        var startTimeOk = handoff.WorldMode == WorldMode.NewMap ? handoff.Start is not null || (handoff.DateMode == DateTimeMode.Unset && handoff.TimeMode == DateTimeMode.Unset) : handoff.Start is null && handoff.DateMode == DateTimeMode.Unset && handoff.TimeMode == DateTimeMode.Unset;
        if ((handoff.WorldMode is not WorldMode.NewMap and not WorldMode.SavedSituation and not WorldMode.LastMapState) || (handoff.WorldMode == WorldMode.LastMapState && string.IsNullOrWhiteSpace(handoff.MapIdentity)) || !handoff.HeadlessStart || handoff.PlayerVehicleEnabled || !startTimeOk || (handoff.WorldMode == WorldMode.SavedSituation && string.IsNullOrWhiteSpace(handoff.SituationIdentity))) { Emit("plugin.request.unsupported"); Volatile.Write(ref startupAccepted, 0); return false; }
        if (handoff.Start is { } start && !native.SetStartDateTime(start)) { Emit("plugin.capability.unresolved", ("capabilities", "world.start-date-time")); Volatile.Write(ref startupAccepted, 0); return false; }
        if (handoff.Weather is not null && !native.StartWeatherAvailable()) { Emit("plugin.capability.unresolved", ("capabilities", "world.start-weather")); Volatile.Write(ref startupAccepted, 0); return false; }
        if (!native.ValidateStart(handoff.WorldMode, out var missing)) { Emit("plugin.capability.unresolved", ("capabilities", missing)); Volatile.Write(ref startupAccepted, 0); return false; }
        if (!native.CheckLiveStartForm()) { Emit("plugin.build.invalid"); Volatile.Write(ref startupAccepted, 0); return false; }
        Emit("plugin.build.validated");
        if (!native.ArmHeadlessStart()) { Emit("headless.arm.failed"); Volatile.Write(ref startupAccepted, 0); return false; }
        Emit("headless.armed");
        // Runtime modules: load (re-verified against the plan) and arm before
        // the world starts. A fail-session module failure stops the start.
        if (handoff.Modules is { Count: > 0 } planned && moduleHostFactory is not null)
        {
            modules = moduleHostFactory(handoff);
            modules.Load(planned.Select(x => new ModuleLoadRequest(Path.Combine(installationRoot, x.RelativeDirectory), x.ManifestSha256, ModuleSettingsFromHandoff(x.Settings), x.SnapshotSha256.Length == 0 ? null : x.SnapshotSha256)));
            modules.Arm();
            if (modules.SessionFailureRequested) { modules.Stop(); Volatile.Write(ref startupAccepted, 0); return false; }
        }
        var runtimeChannel = Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL");
        if (!string.IsNullOrWhiteSpace(runtimeChannel) && runtimeControl is not null) mailbox = new CurrentRuntimeCommandMailbox(runtimeChannel, handoff.SessionId);
        native.InstallMainThreadGateway(); pending = handoff;
        scheduleOnUiThread(ConsumePendingWorld);
        return true;
        }
        catch
        {
            Volatile.Write(ref startupAccepted, 0);
            throw;
        }
    }

    // Called by the adapter's OMSI UI-thread timer. No runtime command is ever
    // executed on an IPC worker thread.
    public void PollRuntimeCommands()
    {
        if (runtimeControl is null) return;
        foreach (var lifecycleEvent in runtimeControl.PollLifecycle()) telemetry(lifecycleEvent.Name, lifecycleEvent.Data);
        mailbox?.TryDispatch(modules is null ? runtimeControl : new ModuleDispatch(runtimeControl, modules));
        TickModules();
    }

    // Modules that asked for a tick (capability bridge.tick): called from the
    // same 50 ms UI timer. A nested modal loop keeps the timer firing, so a
    // tick never re-enters itself.
    private bool ticking;
    private void TickModules()
    {
        if (modules is null || ticking) return;
        ticking = true;
        try { modules.Tick(); }
        finally { ticking = false; }
    }

    public void Shutdown()
    {
        mailbox = null;
        pending = null;
        modules?.Stop(); modules = null;
        runtimeControl?.Shutdown();
        Volatile.Write(ref startupAccepted, 0);
    }

    // The requested start weather, after the world started and before
    // gameplay is reported. A failure fails the start (the caller asked for it).
    private bool ApplyStartWeather()
    {
        if (pending?.Weather is not { } weather) return true;
        var status = native.ApplyStartWeather(weather);
        if (status == 0) { Emit("world.weather.applied", ("mode", weather.Mode.ToString())); return true; }
        Emit("world.failed", ("native_status", status.ToString()), ("code", PublicErrorCodes.WEATHER_APPLY_FAILED),
            ("detail", status == 2 ? "OMSI has no ICAO station configured for live weather" : status == 3 ? "the start weather request is invalid" : "the start weather targets are unavailable"));
        pending = null;
        return false;
    }

    private void ConsumePendingWorld()
    {
        if (pending is null || executing) return;
        executing = true;
        try
        {
            if (pending.WorldMode == WorldMode.LastMapState)
            {
                Emit("world.last-state.starting", ("map", pending.MapIdentity));
                var lastResult = native.StartLastMapState(pending.MapIdentity);
                if (lastResult == 0) { Emit("world.last-state.loaded", ("map", pending.MapIdentity)); if (!ApplyStartWeather()) return; Emit("gameplay.entered", ("map", pending.MapIdentity)); ModulesGameplayReady(); pending = null; return; }
                Emit("world.situation.failed", ("native_status", lastResult.ToString()), ("map", pending.MapIdentity), ("mode", "last-map-state")); pending = null; return;
            }
            if (pending.WorldMode == WorldMode.SavedSituation)
            {
                Emit("world.situation.starting", ("situation", pending.SituationIdentity));
                var savedResult = native.StartSavedSituation(pending.SituationIdentity);
                if (savedResult == 0) { Emit("world.situation.loaded", ("situation", pending.SituationIdentity)); if (!ApplyStartWeather()) return; Emit("gameplay.entered", ("situation", pending.SituationIdentity)); ModulesGameplayReady(); pending = null; return; }
                Emit("world.situation.failed", ("native_status", savedResult.ToString()), ("situation", pending.SituationIdentity)); pending = null; return;
            }
            Emit("world.starting", ("map", pending.MapIdentity), ("presented_index", pending.PresentedEntrypointIndex.ToString()), ("entrypoint_identity", pending.EntrypointIdentity));
            var result = native.StartNewMap(pending.MapIdentity, pending.PresentedEntrypointIndex, pending.EntrypointIdentity);
            if (result == 0) {
                Emit("world.loaded");
                if (!ApplyStartWeather()) return;
                if (native.TryGetLastEntrypointSelection(out var selection))
                {
                    Emit("world.entrypoint.selected", ("presented_index", selection.PresentedIndex.ToString()), ("raw_index", selection.RawIndex.ToString()), ("presented_label", selection.PresentedLabel), ("raw_label", selection.RawLabel));
                    // The readiness event repeats the selected identity so a
                    // reader that only looks at gameplay.entered has it.
                    Emit("gameplay.entered", ("presented_index", selection.PresentedIndex.ToString()), ("raw_index", selection.RawIndex.ToString()), ("presented_label", selection.PresentedLabel), ("raw_label", selection.RawLabel));
                }
                else Emit("gameplay.entered", ("entrypoint_diagnostics", "unavailable"));
                ModulesGameplayReady();
                pending = null;
            }
            else if (result is 3 or 4) { Emit("world.waiting-native-ready", ("native_status", result.ToString())); }
            else if (result is 6 or 7) { Emit("world.failed", ("native_status", result.ToString()), ("code", PublicErrorCodes.DATE_TIME_APPLY_FAILED), ("detail", result == 6 ? "OMSI uses a fixed start time in this mode" : "the running clock does not hold the requested start time")); pending = null; }
            else { Emit("world.failed", ("native_status", result.ToString())); pending = null; }
        }
        finally { executing = false; }
    }

    // module.<id>.<operation> goes to the module host; everything else to the
    // product's runtime control. Both run on OMSI's UI thread.
    internal sealed class ModuleDispatch : IPluginRuntimeControl
    {
        private readonly IPluginRuntimeControl inner; private readonly ModuleHost host;
        public ModuleDispatch(IPluginRuntimeControl inner, ModuleHost host) { this.inner = inner; this.host = host; }
        public RuntimeCommandResult Execute(RuntimeCommand command)
        {
            if (!command.Operation.StartsWith(RuntimeModuleProtocol.OperationPrefix, StringComparison.Ordinal)) return inner.Execute(command);
            var result = host.Execute(command.Operation, command.Arguments);
            return new RuntimeCommandResult(command.SessionId, command.RequestId, result.Succeeded, result.ErrorCode, result.Values);
        }
        public IReadOnlyList<PluginRuntimeEvent> PollLifecycle() => inner.PollLifecycle();
        public void ActivateDirect3D() => inner.ActivateDirect3D();
        public void Shutdown() => inner.Shutdown();
    }

    // Before a module that hooks the Direct3D device hears gameplay-ready,
    // the bridge starts observing the device (sessions without such a module
    // keep the lazy Beta 3 behavior).
    private void ModulesGameplayReady()
    {
        if (modules is null) return;
        if (runtimeControl is not null && modules.RequiresCapability("bridge.hooks.d3d9"))
        {
            runtimeControl.ActivateDirect3D();
            foreach (var lifecycleEvent in runtimeControl.PollLifecycle()) telemetry(lifecycleEvent.Name, lifecycleEvent.Data);
        }
        modules.GameplayReady();
    }

    private void Emit(string name, params (string Key, string Value)[] values) => telemetry(name, values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
    // The settings the controller resolved (handoff v6): a JSON object of
    // strings. Anything else yields no values; the module then gets its
    // manifest defaults.
    private static IReadOnlyDictionary<string, string>? ModuleSettingsFromHandoff(string json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try
        {
            using var document = System.Text.Json.JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
            return document.RootElement.EnumerateObject().Where(x => x.Value.ValueKind == System.Text.Json.JsonValueKind.String).ToDictionary(x => x.Name, x => x.Value.GetString()!, StringComparer.Ordinal);
        }
        catch (System.Text.Json.JsonException) { return null; }
    }

    private static bool TryReadHandoff(string name, out StartupHandoff handoff)
    {
        handoff = null!;
        try
        {
            using var mapping = MemoryMappedFile.OpenExisting(name, MemoryMappedFileRights.Read);
            using var view = mapping.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
            if (view.Capacity < 64) return false;
            var header = new byte[64]; view.ReadArray(0, header, 0, header.Length);
            var total = BinaryPrimitives.ReadUInt32LittleEndian(header.AsSpan(8));
            if (total < 64 || total > view.Capacity || total > 1024 * 1024) return false;
            var data = new byte[checked((int)total)]; view.ReadArray(0, data, 0, data.Length);
            if (!StartupHandoffWire.TryDeserialize(data, out var decoded) || decoded is null) return false;
            handoff = decoded; return true;
        }
        catch (FileNotFoundException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
    }
}
