namespace OmsiLaunch.Api;

// The single canonical capability catalog (Beta 4, R3). Every capability,
// every public runtime operation with its arguments, CLI routes and native
// capability, and the one evidence state of each capability are declared here
// once. The runtime operation set, argument validation, CLI command routes,
// the plugin's per-operation native gate, GetCapabilitiesAsync and the
// generated documentation catalog are all derived from it.
public enum PublicCapabilityClassification : byte
{
    PublicStableBeta,
    PublicExperimental,
    InternalOnly,
    Unsupported
}

public enum PublicCapabilityKind : byte { Read, Write, Action, Event }

// Evidence vocabulary shared with docs/status/runtime-validation-status.md.
public static class PublicEvidence
{
    public const string RuntimeValidated = "RUNTIME_VALIDATED";
    public const string StaticallyValidated = "STATICALLY_VALIDATED";
    public const string Partial = "PARTIAL";
    public const string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED";
    public const string Unavailable = "UNAVAILABLE";
    public static readonly IReadOnlyList<string> All = new[] { RuntimeValidated, StaticallyValidated, Partial, RuntimeValidationRequired, Unavailable };
}

// Concrete runtime operations may require values even when the public catalog
// groups them under one capability. This metadata is shared by every frontend
// so a missing value is rejected before it reaches the OMSI mailbox.
public sealed record PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description);
public sealed record PublicRuntimeArgumentValidation(bool Accepted, string? ErrorCode = null, string? Message = null);

// One concrete runtime operation: its id on the wire, its arguments, the CLI
// command routes that reach it, and the native capability it needs.
// Paged operations take the optional arguments cursor and limit and answer
// count, returned_count, truncated and next_cursor.
// MutationPolicy says what a call changes and what undoes it (a PublicMutationPolicy value).
public sealed record PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = PublicMutationPolicy.None);

// No runtime operation is journaled: the session transaction covers files,
// never OMSI's memory.
public static class PublicMutationPolicy
{
    // Reads only.
    public const string None = "none";
    // Changes OMSI's in-memory state; nothing undoes it; it ends with the OMSI process.
    public const string OmsiState = "omsi-state";
    // Creates a session-owned resource, released explicitly or at session end.
    public const string SessionResource = "session-resource";
    // Installs a session policy the plugin re-applies until it is removed or the session ends.
    public const string SessionPolicy = "session-policy";
    public static readonly IReadOnlyList<string> All = new[] { None, OmsiState, SessionResource, SessionPolicy };
}

public sealed record PublicCapabilityDescriptor(
    string Id,
    string Family,
    PublicCapabilityClassification Classification,
    PublicCapabilityKind Kind,
    bool RequiresSession,
    bool RequiresExactProfile,
    string ApiRoute,
    string CliRoute,
    string RuntimeValidation,
    string Description,
    IReadOnlyList<string>? HandleTypes = null,
    IReadOnlyList<PublicRuntimeOperation>? Operations = null,
    string? NativeCapability = null,
    string? EvidenceNote = null)
{
    public IReadOnlyList<PublicRuntimeOperation> EffectiveOperations => Operations ?? Array.Empty<PublicRuntimeOperation>();
    // Native capabilities this capability depends on (its own and its operations').
    public IReadOnlyList<string> NativeCapabilities => EffectiveOperations.Select(o => o.NativeCapability).Append(NativeCapability).Where(x => !string.IsNullOrEmpty(x)).Select(x => x!).Distinct(StringComparer.Ordinal).ToArray();
}

public static class PublicCapabilityRegistry
{
    public const string ProtocolVersion = "0.1";
    private const PublicCapabilityClassification Stable = PublicCapabilityClassification.PublicStableBeta;
    private const PublicCapabilityClassification Experimental = PublicCapabilityClassification.PublicExperimental;
    private const PublicCapabilityClassification Internal = PublicCapabilityClassification.InternalOnly;
    private const PublicCapabilityClassification Unsupported = PublicCapabilityClassification.Unsupported;
    private const string Runtime = PublicEvidence.RuntimeValidated, Static = PublicEvidence.StaticallyValidated, Partial = PublicEvidence.Partial, Unavailable = PublicEvidence.Unavailable;

    private static PublicRuntimeOperation Op(string id, string native, string cliRoute, params string[] requiredArguments) =>
        new(id, native, Required(requiredArguments), cliRoute.Length == 0 ? Array.Empty<string>() : cliRoute.Split(" | "));
    private static PublicRuntimeOperation Paged(PublicRuntimeOperation operation) => operation with { Paged = true };
    private static PublicRuntimeOperation Mutates(string policy, PublicRuntimeOperation operation) => operation with { MutationPolicy = policy };
    private const string OmsiState = PublicMutationPolicy.OmsiState, SessionResource = PublicMutationPolicy.SessionResource, SessionPolicy = PublicMutationPolicy.SessionPolicy;
    private const string Handle = "handle", HandleText = "Session-scoped RoadVehicle handle.";

    public static readonly IReadOnlyList<PublicCapabilityDescriptor> All = new PublicCapabilityDescriptor[]
    {
        // Session.
        new("session.plan", "session", Stable, PublicCapabilityKind.Action, false, true, "session.plan", "/plan", Runtime, "Compile a LaunchSpec without starting OMSI."),
        new("session.start", "session", Stable, PublicCapabilityKind.Action, false, true, "session.start", "/new | /saved:<situation.osn> | /spec:<spec.json>", Runtime, "Start a transactional managed OMSI session."),
        new("session.status", "session", Stable, PublicCapabilityKind.Read, true, false, "session.status", "session status", Runtime, "Read the semantic lifecycle state."),
        new("session.stop", "session", Stable, PublicCapabilityKind.Action, true, false, "session.stop", "session stop", Runtime, "End the session: OMSI is terminated (with Behavior.CooperativeShutdown it is first asked to quit through its own exit routine) and every session-owned file is restored exactly."),
        new("session.recover", "session", Stable, PublicCapabilityKind.Action, false, true, "session.recover", "/recovery-status | /recover", Runtime, "Recover a stale durable transaction."),
        new("events.read", "events", Experimental, PublicCapabilityKind.Event, true, false, "session.events", "events read | events watch", Runtime, "Read or observe bounded session runtime events.", EvidenceNote: "Beta 4: plugin events travel through a numbered ring; a reader that falls behind receives telemetry.lost with the count."),

        // Installation, platform and start.
        new("runtime.current-windows-x64", "platform", Stable, PublicCapabilityKind.Read, false, false, "RuntimePlatformInfo", "", Runtime, "Windows 10 or later, 64-bit host."),
        new("transaction.exact-restore", "platform", Stable, PublicCapabilityKind.Action, false, false, "FileConfigurationTransaction", "", Runtime, "Journaled overlays with byte-exact restore and crash recovery."),
        new("omsi.omsi2", "platform", Stable, PublicCapabilityKind.Read, false, false, "OmsiBuildDetection", "profiles", Runtime, "The installation is an OMSI 2 installation (the only compatibility gate).", EvidenceNote: "Beta 4 round A: known layout."),
        new("boot.headless-start", "world", Stable, PublicCapabilityKind.Action, false, true, "session.start", "", Runtime, "Start OMSI without its Start dialog.", NativeCapability: "boot.headless-start"),
        new("world.new-map", "world", Stable, PublicCapabilityKind.Action, false, true, "LaunchSpec.World (NewMap)", "/new /map:<identity> /entrypoint-index:<n>", Runtime, "Start on a map at a presented entrypoint.", NativeCapability: "world.new-map"),
        new("world.presented-entrypoint", "world", Stable, PublicCapabilityKind.Action, false, true, "LaunchSpec.World.PresentedEntrypointIndex", "/entrypoint-index:<n>", Runtime, "Select the entrypoint by its presented index.", NativeCapability: "world.new-map"),
        new("world.entrypoint-identity", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.World.EntrypointIdentity", "/entrypoint:<identity>", PublicEvidence.RuntimeValidationRequired, "Select the entrypoint by its exact label or its discovery identity.", NativeCapability: "world.new-map", EvidenceNote: "Beta 4 (BI-001): OMSI presents the first record of each exact label; the startup selects it by that label. Closed statically; runtime validation pending."),
        new("world.saved-situation", "world", Stable, PublicCapabilityKind.Action, false, true, "LaunchSpec.World (SavedSituation)", "/saved:<situation.osn>", Runtime, "Start a saved situation through the Start form.", NativeCapability: "world.saved-situation"),
        new("world.last-map-state", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.World (LastMapState)", "/last /map:<identity>", PublicEvidence.RuntimeValidationRequired, "Start a map's last state (its laststn.osn) through the Start form's last-state branch.", NativeCapability: "world.last-map-state", EvidenceNote: "Beta 4: the native branch is closed statically (BI-006); runtime validation pending. Never a newest-.osn heuristic."),
        new("world.explicit-date", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.Date", "/date:<yyyy-mm-dd>", PublicEvidence.RuntimeValidationRequired, "Explicit or system start date of a NEW_MAP session.", NativeCapability: "world.start-date-time", EvidenceNote: "Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending."),
        new("world.explicit-time", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.Time", "/time:<hh:mm>", PublicEvidence.RuntimeValidationRequired, "Explicit or system start time of a NEW_MAP session.", NativeCapability: "world.start-date-time", EvidenceNote: "Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending."),
        new("world.explicit-year", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.Year", "/year:<n>", PublicEvidence.RuntimeValidationRequired, "Explicit or system start year of a NEW_MAP session.", NativeCapability: "world.start-date-time", EvidenceNote: "Beta 4: written into OMSI's start clock before NewSituation (static analysis); runtime validation pending."),
        new("world.weather", "world", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.Weather", "/weather:<preset> | /weather-icao:<code> | /weather-real", PublicEvidence.RuntimeValidationRequired, "Start weather: an OMSI weather preset, live METAR weather for a station, or live weather for the station OMSI has configured.", NativeCapability: "world.start-weather",
            EvidenceNote: "Beta 4 (BI-003): applied right after the world starts, through OMSI's own loadWeather or its METAR flags; it overrides a saved situation's weather. Closed statically; runtime validation pending."),
        new("player-vehicle.model", "player", Unsupported, PublicCapabilityKind.Action, false, true, "LaunchSpec.PlayerVehicle.Model", "/vehicle:<identity>", Unavailable, "Start with a player vehicle.", EvidenceNote: "BI-007; resolved for diagnostics, not applied."),
        new("player-vehicle.repaint", "player", Unsupported, PublicCapabilityKind.Action, false, true, "LaunchSpec.PlayerVehicle.Repaint", "/repaint:<id>", Unavailable, "Player vehicle repaint at start.", EvidenceNote: "BI-007."),
        new("player-vehicle.hof", "player", Unsupported, PublicCapabilityKind.Action, false, true, "LaunchSpec.PlayerVehicle.Hof", "/hof:<id>", Unavailable, "Player vehicle HOF at start.", EvidenceNote: "BI-007."),
        new("player-vehicle.fleet-number", "player", Unsupported, PublicCapabilityKind.Action, false, true, "LaunchSpec.PlayerVehicle.FleetNumber", "/fleet:<n>", Unavailable, "Player vehicle fleet number at start.", EvidenceNote: "BI-007."),
        new("player-vehicle.registration", "player", Unsupported, PublicCapabilityKind.Action, false, true, "LaunchSpec.PlayerVehicle.Registration", "/registration:<r>", Unavailable, "Player vehicle registration at start.", EvidenceNote: "BI-007."),
        new("configuration.options.semantic", "configuration", Stable, PublicCapabilityKind.Write, false, false, "LaunchSpec.Environment", "/set:<key>=<value>", Runtime, "Session-scoped semantic options.cfg overlays.", EvidenceNote: "RV-005."),
        new("input.keyboard", "configuration", Experimental, PublicCapabilityKind.Write, false, false, "LaunchSpec.Input.KeyboardDocument", "", Static, "A keyboard file in OMSI's format used in place of Inputs\\keyboard.cfg for the session; restored exactly at the end.", EvidenceNote: "Beta 4: transactional overlay, validated offline."),
        new("input.controller", "configuration", Experimental, PublicCapabilityKind.Write, false, false, "LaunchSpec.Input.ControllerDocument", "", Static, "A controller file in OMSI's format used in place of Inputs\\gamectrler.cfg for the session; restored exactly at the end.", EvidenceNote: "Beta 4: transactional overlay, validated offline."),
        new("presentation.splash", "presentation", Stable, PublicCapabilityKind.Write, false, false, "LaunchSpec.Presentation", "/splash:Managed | /splash:Unset", Runtime, "Managed splash overlays, restored exactly."),
        new("internet-textures.disabled", "presentation", Experimental, PublicCapabilityKind.Action, false, true, "LaunchSpec.InternetTextures (Disabled)", "/internet-textures:Disabled", Runtime, "Suppress OMSI's in-process texture downloader.", NativeCapability: "internet-textures.disabled", EvidenceNote: "Beta 4 round B: the suppression is applied and reported (internet-textures.suppressed) through the telemetry ring."),
        new("internet-textures.override", "presentation", Experimental, PublicCapabilityKind.Write, false, false, "LaunchSpec.InternetTextures (Override)", "/internet-textures:Override", Runtime, "Session-scoped .itx profile and target deletions.", EvidenceNote: "Runtime closure I01."),
        new("session-profiles", "profiles", Stable, PublicCapabilityKind.Action, false, false, "SessionProfileCompiler", "/predefined-profile:<id> /predefined-profile-index:<n> /new | /list:SessionProfiles", Runtime, "Declarative session profiles (1..99 presets) and their discovery.", EvidenceNote: "RA-009 profile start; 1..99 and discovery offline."),
        new("hof.management", "hof", Stable, PublicCapabilityKind.Write, false, false, "LaunchSpec.HofManagement", "/hof-source:collection:<name> /hof-target:<vehicle>", Runtime, "Session-scoped HOF copy and mirror into vehicle folders, restored exactly.", EvidenceNote: "Beta 4 round A C01, C02."),
        new("content.discovery", "content", Stable, PublicCapabilityKind.Read, false, false, "DiscoverAsync", "/list:<category>", Runtime, "Discover maps, situations, vehicles, repaints, HOFs, entrypoints, add-ons and session profiles."),
        new("content.map", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Runtime, "Resolve the requested map identity."),
        new("content.situation", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Runtime, "Resolve the requested saved situation."),
        new("content.situation-map", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Runtime, "Resolve the map referenced by a saved situation."),
        new("content.vehicle", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Static, "Resolve a vehicle identity."),
        new("content.repaint", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Static, "Resolve a repaint identity."),
        new("content.hof", "content", Stable, PublicCapabilityKind.Read, false, false, "PlanSessionAsync", "", Static, "Resolve a HOF identity."),

        // Runtime (require a Running session and their native capability).
        new("time.read", "time", Stable, PublicCapabilityKind.Read, true, true, "runtime.time.read", "time get", Runtime, "Read OMSI clock and calendar fields.",
            Operations: new[] { Op("time.read", "time.read", "time get") }),
        new("time.set", "time", Experimental, PublicCapabilityKind.Write, true, true, "runtime.time.set", "time set", Runtime, "Set the OMSI clock through the native SetTime operation.",
            Operations: new[] { Mutates(OmsiState, Op("time.set", "time.set", "time set")) }),
        new("weather.read", "weather", Stable, PublicCapabilityKind.Read, true, true, "runtime.weather.read", "weather get", Runtime, "Read weather state.",
            Operations: new[] { Op("weather.read", "weather.read", "weather get") }),
        new("weather.set", "weather", Experimental, PublicCapabilityKind.Write, true, true, "runtime.weather.set", "weather set", PublicEvidence.RuntimeValidationRequired, "Set the current weather through OMSI's source weather record (manual weather: live METAR updates stop), or switch to live METAR weather for an ICAO station.",
            Operations: new[] { Mutates(OmsiState, Op("weather.set", "weather.set", "weather set")), Mutates(OmsiState, Op("weather.metar.set", "weather.metar.set", "weather metar set", "icao", "Four-letter ICAO station code (A-Z, 0-9).")) },
            NativeCapability: "weather.set", EvidenceNote: "Beta 4: writes actWeather (Weather+0x08), the record OMSI's weather tick derives from; closed statically. Derived values (humidity, light factors, actual wind) stay read-only (OL_E_RUNTIME_SETTING_NOT_PERSISTENT). Runtime validation pending."),
        new("weather.actual.read", "weather", Experimental, PublicCapabilityKind.Read, true, true, "runtime.weather.actual.read", "weather actual get", Runtime, "Read actual/ICAO weather-controller state.",
            Operations: new[] { Op("weather.actual.read", "weather.actual.read", "weather actual get") }),
        new("map.read", "map", Stable, PublicCapabilityKind.Read, true, true, "runtime.map.read", "map get", Runtime, "Read stable map identity and load state.",
            Operations: new[] { Op("map.read", "map.read", "map get"), Paged(Op("map.tiles.list", "map.read", "map tiles list")) }),
        new("camera.read", "camera", Stable, PublicCapabilityKind.Read, true, true, "runtime.camera.read", "camera get", Runtime, "Read camera family and supported fields.",
            Operations: new[] { Op("camera.read", "camera.read", "camera get") }),
        new("camera.set", "camera", Experimental, PublicCapabilityKind.Write, true, true, "runtime.camera.set", "camera set", Runtime, "Set supported camera scalars such as FOV.",
            Operations: new[] { Mutates(OmsiState, Op("camera.set", "camera.set", "camera set")) }),
        new("ui.pause", "ui", Experimental, PublicCapabilityKind.Action, true, true, "runtime.ui.pause", "ui pause-state | ui pause-take-over | ui pause-restore | ui pause-action --action=<resume|new-game|save|options|quit>", PublicEvidence.RuntimeValidationRequired,
            "Take over OMSI's native pause menu: hide it through the VCL while OMSI stays paused, call its own actions, give it back.",
            Operations: new[]
            {
                Op("ui.pause.state", "ui.pause-menu.replace", "ui pause-state"),
                Mutates(OmsiState, Op("ui.pause.take-over", "ui.pause-menu.replace", "ui pause-take-over")),
                Mutates(OmsiState, Op("ui.pause.restore-native", "ui.pause-menu.replace", "ui pause-restore")),
                Mutates(OmsiState, Op("ui.pause.action", "ui.pause-menu.replace", "ui pause-action", "action", "resume, new-game, save, options or quit: the native pause menu's own action; it runs after this answer and its outcome is in ui.pause.state.")),
            },
            EvidenceNote: "Beta 5: statically validated design (research beta4-static pause-menu-and-input.md); the native calls are not yet observed in a real session."),
        new("camera.lock", "camera", Experimental, PublicCapabilityKind.Action, true, true, "runtime.camera.lock", "camera lock --family=<0..3> [--preset=<n>]", Runtime, "Session-scoped camera-family policy; optional driver/passenger preset while preserving head-look and FOV.",
            Operations: new[] { Mutates(SessionPolicy, Op("camera.lock", "camera.lock", "camera lock", "family", "Camera family: driver=0, passenger=1, external=2, map=3.")), Mutates(SessionPolicy, Op("camera.unlock", "camera.lock", "camera unlock")) },
            EvidenceNote: "Needs a PlayerVehicle (a saved situation provides one); OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE on a headless NEW_MAP."),
        new("vehicles.list", "vehicles", Stable, PublicCapabilityKind.Read, true, true, "runtime.road-vehicles.list", "vehicles list", Runtime, "List opaque RoadVehicle handles.", new[] { "RoadVehicle" },
            Operations: new[] { Paged(Op("road-vehicles.list", "vehicles.list", "vehicles list")) }),
        new("vehicles.get", "vehicles", Stable, PublicCapabilityKind.Read, true, true, "runtime.road-vehicle.read", "vehicles get", Runtime, "Read an opaque RoadVehicle snapshot.", new[] { "RoadVehicle" }, EvidenceNote: "Stale detection after natural removal (RV-002) has no safe runtime producer and is validated offline.",
            Operations: new[] { Op("road-vehicle.read", "vehicles.get", "vehicles get", Handle, HandleText) }),
        new("vehicles.summary", "vehicles", Stable, PublicCapabilityKind.Read, true, true, "runtime.road-vehicles.read", "vehicles summary", Runtime, "Read the road-vehicle collection summary (counts and player state) without handles.",
            Operations: new[] { Op("road-vehicles.read", "vehicles.summary", "vehicles summary") }),
        new("vehicles.spawn", "vehicles", Experimental, PublicCapabilityKind.Action, true, true, "runtime.road-vehicles.spawn", "vehicles spawn --model=<Vehicles\\...\\*.bus>", Runtime, "Create one RoadVehicle from a canonical .bus identity; this does not assign PlayerVehicle.", new[] { "RoadVehicle" },
            Operations: new[] { Mutates(OmsiState, Op("road-vehicles.spawn", "vehicles.spawn", "vehicles spawn", "model", "Canonical .bus identity below Vehicles\\.")) }),
        new("vehicles.place-random", "vehicles", Experimental, PublicCapabilityKind.Action, true, true, "runtime.road-vehicles.place-random", "vehicles place-random", Runtime, "Invoke OMSI's PlaceRandomBus.",
            Operations: new[] { Mutates(OmsiState, Op("road-vehicles.place-random", "vehicles.place-random", "vehicles place-random")) }),
        new("player.read", "player", Stable, PublicCapabilityKind.Read, true, true, "runtime.player-vehicle.read", "player get", Runtime, "Read the active PlayerVehicle or semantic null.", EvidenceNote: "present=false in a headless start; the full snapshot with a saved situation.",
            Operations: new[] { Op("player-vehicle.read", "player.read", "player get") }),
        new("humans.list", "humans", Experimental, PublicCapabilityKind.Read, true, true, "runtime.humans.list", "humans list", Runtime, "List opaque human handles.", new[] { "Human" },
            Operations: new[] { Paged(Op("humans.list", "humans.list", "humans list")) }),
        new("humans.get", "humans", Experimental, PublicCapabilityKind.Read, true, true, "runtime.human.read", "humans get", Runtime, "Read a human snapshot.", new[] { "Human" },
            Operations: new[] { Op("human.read", "humans.get", "humans get", Handle, "Session-scoped Human handle.") }),
        new("humans.summary", "humans", Experimental, PublicCapabilityKind.Read, true, true, "runtime.humans.read", "humans summary", Runtime, "Read the human collection summary (counts) without handles.",
            Operations: new[] { Op("humans.read", "humans.summary", "humans summary") }),
        new("timetable.read", "timetable", Stable, PublicCapabilityKind.Read, true, true, "runtime.timetable.read", "timetable get", Runtime, "Read timetable manager state and records.",
            Operations: new[]
            {
                Op("timetable.read", "timetable.read", "timetable get"), Paged(Op("timetable.tracks.list", "timetable.read", "timetable tracks list")),
                Paged(Op("timetable.trips.list", "timetable.read", "timetable trips list")), Paged(Op("timetable.lines.list", "timetable.read", "timetable lines list")),
                Paged(Op("timetable.rv-files.list", "timetable.read", "")), Paged(Op("timetable.track-entries.list", "timetable.read", "")),
                Paged(Op("timetable.bus-stops.list", "timetable.read", "timetable bus-stops list")), Paged(Op("timetable.station-links.list", "timetable.read", "timetable station-links list")),
                Paged(Op("timetable.tours.list", "timetable.read", "timetable tours list")), Paged(Op("timetable.profiles.list", "timetable.read", "timetable profiles list")),
                Paged(Op("timetable.tour-entries.list", "timetable.read", "")), Paged(Op("timetable.logs.read", "timetable.read", "timetable logs list")),
                Paged(Op("timetable.no-rv-numbers.list", "timetable.read", "timetable no-rv-numbers list")),
            }),
        new("scripts.numeric", "scripts", Experimental, PublicCapabilityKind.Write, true, true, "runtime.vehicle.variable.*", "scripts variable list | scripts variable get | scripts variable set", Runtime, "List, read and write numeric script variables.", new[] { "RoadVehicle" },
            Operations: new[]
            {
                Paged(Op("vehicle.variables.list", "scripts.numeric", "scripts variable list", Handle, HandleText)),
                Op("vehicle.variable.get", "scripts.numeric", "scripts variable get", Handle, HandleText, "name", "Open numeric script-variable name."),
                Mutates(OmsiState, Op("vehicle.variable.set", "scripts.numeric", "scripts variable set", Handle, HandleText, "name", "Open numeric script-variable name.", "value", "Finite numeric script-variable value.")),
            }),
        new("scripts.string.read", "scripts", Experimental, PublicCapabilityKind.Read, true, true, "runtime.vehicle.string-variable.*", "scripts string list | scripts string get", Runtime, "List and read string script variables.", new[] { "RoadVehicle" }, EvidenceNote: "Writes: scripts.string.write.",
            Operations: new[]
            {
                Paged(Op("vehicle.string-variables.list", "scripts.string.read", "scripts string list", Handle, HandleText)),
                Op("vehicle.string-variable.get", "scripts.string.read", "scripts string get", Handle, HandleText, "name", "Open string script-variable name."),
            }),
        new("scripts.string.write", "scripts", Experimental, PublicCapabilityKind.Write, true, true, "runtime.vehicle.string-variable.set", "scripts string set", PublicEvidence.RuntimeValidationRequired, "Write a string script variable through OMSI's own SetStringVariable.", new[] { "RoadVehicle" },
            NativeCapability: "scripts.string.write", EvidenceNote: "Beta 4 (BI-004): OMSI copies a constant-shaped string; closed statically. Runtime validation pending.",
            Operations: new[] { Mutates(OmsiState, Op("vehicle.string-variable.set", "scripts.string.write", "scripts string set", Handle, HandleText, "name", "String script-variable name the vehicle script declares (case-sensitive).", "value", "New value, 1 to 1024 characters.")) }),
        new("scripts.trigger", "scripts", Experimental, PublicCapabilityKind.Action, true, true, "runtime.vehicle.trigger | runtime.vehicle.sound-trigger", "scripts trigger | scripts sound-trigger", PublicEvidence.RuntimeValidationRequired, "Raise a vehicle script trigger or sound trigger as OMSI's input handlers do.", new[] { "RoadVehicle" },
            NativeCapability: "scripts.trigger", EvidenceNote: "Beta 4 (BI-004): virtual TriggerXML/SoundTrigger through the profiled VMT slots; closed statically. Runtime validation pending.",
            Operations: new[]
            {
                Mutates(OmsiState, Op("vehicle.trigger", "scripts.trigger", "scripts trigger", Handle, HandleText, "name", "Script trigger name (code page 1252, up to 128 characters).", "value", "1 press, 0 release (name_off), 2 drag (name_drag).")),
                Mutates(OmsiState, Op("vehicle.sound-trigger", "scripts.trigger", "scripts sound-trigger", Handle, HandleText, "trigger", "Sound trigger name (code page 1252, up to 128 characters).")),
            }),
        new("constants", "constants", Stable, PublicCapabilityKind.Read, true, true, "runtime.vehicle.constant.*", "constants list | constants get", Runtime, "List and read vehicle constants.", new[] { "RoadVehicle" },
            Operations: new[]
            {
                Paged(Op("vehicle.constants.list", "constants", "constants list", Handle, HandleText)),
                Op("vehicle.constant.get", "constants", "constants get", Handle, HandleText, "name", "Open constant name."),
            }),
        new("curves", "curves", Stable, PublicCapabilityKind.Read, true, true, "runtime.vehicle.curve.*", "curves list | curves evaluate", Runtime, "List and evaluate vehicle curves.", new[] { "RoadVehicle" },
            Operations: new[]
            {
                Paged(Op("vehicle.curves.list", "curves", "curves list", Handle, HandleText)),
                Op("vehicle.curve.evaluate", "curves", "curves evaluate", Handle, HandleText, "name", "Open curve name.", "x", "Finite curve input."),
            }),
        new("hof.read", "hof", Stable, PublicCapabilityKind.Read, true, true, "runtime.vehicle.hofs.read", "hof get", Runtime, "Read vehicle HOF metadata.", new[] { "RoadVehicle" },
            Operations: new[] { Paged(Op("vehicle.hofs.read", "hof.read", "hof get", Handle, HandleText)) }),
        new("drivers.read", "drivers", Experimental, PublicCapabilityKind.Read, true, true, "runtime.drivers.read", "drivers list", Runtime, "Read driver records.",
            Operations: new[] { Paged(Op("drivers.read", "drivers.read", "drivers list")) }),
        new("tickets.read", "tickets", Experimental, PublicCapabilityKind.Read, true, true, "runtime.tickets.read", "tickets get", Runtime, "Read ticket-pack records.",
            Operations: new[] { Paged(Op("tickets.read", "tickets.read", "tickets get")) }),
        new("d3d.texture", "d3d", Experimental, PublicCapabilityKind.Action, true, true, "runtime.d3d.texture.*", "/runtime:d3d.status | /runtime:d3d.texture.create | /runtime:d3d.texture.describe | /runtime:d3d.texture.update | /runtime:d3d.texture.release", Runtime, "Create, update, describe and release opaque D3D texture resources.", new[] { "D3DTexture" },
            Operations: new[]
            {
                Op("d3d.status", "d3d.texture", ""),
                Mutates(SessionResource, Op("d3d.texture.create", "d3d.texture", "", "width", "Texture width in pixels.", "height", "Texture height in pixels.", "format", "Supported D3D texture format.")),
                Op("d3d.texture.describe", "d3d.texture", "", Handle, "Session-scoped D3D texture handle."),
                Mutates(SessionResource, Op("d3d.texture.update", "d3d.texture", "", Handle, "Session-scoped D3D texture handle.", "width", "Updated rectangle width in pixels.", "height", "Updated rectangle height in pixels.", "pixels_base64", "Base64 texture pixel payload.")),
                Mutates(SessionResource, Op("d3d.texture.release", "d3d.texture", "", Handle, "Session-scoped D3D texture handle.")),
            },
            EvidenceNote: "Device Reset observed (resetting, restored, generation invalidation); a distinct lost transition was not produced (RV-007)."),
        new("internal.make-basic", "vehicles", Internal, PublicCapabilityKind.Action, true, true, "internal.road-vehicles.make-basic", "", Runtime, "Profiled research primitive without PlayerVehicle assignment semantics."),
        new("calendar.set-actual-date-time", "time", Unsupported, PublicCapabilityKind.Write, true, true, "", "", Unavailable, "Set the OMSI calendar date.", EvidenceNote: "BI-002 closed statically: SetActualDateTime only copies the PC clock into the start clock; start dates use world.explicit-date. Superseded: native ABI and postconditions are not closed."),
        new("player.assign-headless", "player", Unsupported, PublicCapabilityKind.Action, true, true, "", "", Unavailable, "Assign a PlayerVehicle in a headless start.", EvidenceNote: "BI-007: the native assignment sequence is closed statically (research/reports/beta4-static/entrypoint-last-player-vehicle.md section 3) but not implemented before runtime validation; planned for a later beta.")
    };

    private static readonly IReadOnlyDictionary<string, PublicCapabilityDescriptor> CapabilityIndex = All.ToDictionary(x => x.Id, StringComparer.Ordinal);

    public static bool TryGet(string id, out PublicCapabilityDescriptor descriptor) => CapabilityIndex.TryGetValue(id, out descriptor!);

    // Every capability id the product reports must be declared here.
    public static PublicCapabilityDescriptor Get(string id) =>
        CapabilityIndex.TryGetValue(id, out var descriptor) ? descriptor : throw new InvalidOperationException("Capability is not declared in PublicCapabilityRegistry: " + id);

    private static readonly IReadOnlyDictionary<string, (PublicCapabilityDescriptor Capability, PublicRuntimeOperation Operation)> OperationIndex =
        All.SelectMany(capability => capability.EffectiveOperations.Select(operation => (capability, operation))).ToDictionary(x => x.operation.Id, x => (x.capability, x.operation), StringComparer.Ordinal);

    public static bool IsPublicRuntimeOperation(string operation) => OperationIndex.ContainsKey(operation);

    // Every concrete runtime operation a public frontend may forward. Internal
    // research primitives are deliberately absent from this list.
    public static IReadOnlyCollection<string> PublicRuntimeOperationIds => OperationIndex.Keys.ToArray();

    public static IReadOnlyList<PublicRuntimeOperation> RuntimeOperations => OperationIndex.Values.Select(x => x.Operation).ToArray();

    public static bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)
    {
        if (OperationIndex.TryGetValue(operation, out var entry)) { capability = entry.Capability; descriptor = entry.Operation; return true; }
        capability = null!; descriptor = null!; return false;
    }

    // The native capability a runtime operation needs, or null for an
    // operation that is not public.
    public static string? NativeCapabilityOf(string operation) => OperationIndex.TryGetValue(operation, out var entry) ? entry.Operation.NativeCapability : null;

    // CLI command route ("time get") -> runtime operation id.
    public static IReadOnlyDictionary<string, string> CliRoutes { get; } =
        OperationIndex.Values.SelectMany(x => x.Operation.CliRoutes.Select(route => (route, x.Operation.Id))).ToDictionary(x => x.route, x => x.Id, StringComparer.Ordinal);

    // Result values that would disclose native layout never cross the public
    // boundary, whichever bridge produced them.
    public static bool IsInternalResultKey(string key) =>
        key.StartsWith("internal_", StringComparison.Ordinal) ||
        key.EndsWith("_address", StringComparison.Ordinal) ||
        key.EndsWith("_pointer", StringComparison.Ordinal) ||
        key.EndsWith("_vmt", StringComparison.Ordinal);

    public static IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation) =>
        OperationIndex.TryGetValue(operation, out var entry) ? entry.Operation.Arguments : Array.Empty<PublicRuntimeArgumentDescriptor>();

    // A runtime module operation, module.<id>.<operation> (Runtime Modules v1).
    // Whether it exists depends on the session: the controller checks it
    // against the modules the session loaded, the plugin checks its arguments.
    public static bool IsModuleOperation(string operation) => ModuleOperationPattern.IsMatch(operation) && !operation.EndsWith('.') && !operation.Contains("..", StringComparison.Ordinal);
    private static readonly System.Text.RegularExpressions.Regex ModuleOperationPattern = new(@"^module\.[a-z0-9][a-z0-9-]{1,47}\.[a-z0-9][a-z0-9.-]{0,63}$", System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    public static PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string>? arguments)
    {
        if (IsModuleOperation(operation)) return new(true);
        if (!IsPublicRuntimeOperation(operation))
            return new(false, "OL_E_RUNTIME_OPERATION_UNKNOWN", "The runtime operation is not part of the public Beta control surface.");

        var missing = GetRuntimeArguments(operation)
            .Where(argument => argument.Required && (arguments is null || !arguments.TryGetValue(argument.Name, out var value) || string.IsNullOrWhiteSpace(value)))
            .Select(argument => argument.Name)
            .ToArray();
        return missing.Length == 0
            ? new(true)
            : new(false, "OL_E_RUNTIME_ARGUMENT_REQUIRED", "Runtime operation requires: " + string.Join(", ", missing) + ".");
    }

    private static IReadOnlyList<PublicRuntimeArgumentDescriptor> Required(params string[] values)
    {
        if (values.Length % 2 != 0) throw new ArgumentException("Argument metadata requires name/description pairs.", nameof(values));
        var result = new PublicRuntimeArgumentDescriptor[values.Length / 2];
        for (var index = 0; index < values.Length; index += 2)
            result[index / 2] = new PublicRuntimeArgumentDescriptor(values[index], true, values[index + 1]);
        return result;
    }
}
