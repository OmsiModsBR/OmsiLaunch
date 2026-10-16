using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Content;
using OmsiLaunch.Configuration;
using OmsiLaunch.Process;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Core;

public sealed class SessionPlanner
{
    private readonly IRuntimePlatform platform;
    public SessionPlanner(IRuntimePlatform platform) => this.platform = platform;

    public Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var diagnostics = new List<LaunchDiagnostic>(LaunchValidation.Validate(spec)); var mutations = new List<PlannedMutation>();
        if (spec.SessionProfile is { } sessionProfile)
            diagnostics.Add(new("session_profile.selected", sessionProfile.Id, new Dictionary<string, string>
            {
                ["session_profile.id"] = sessionProfile.Id, ["session_profile.name"] = sessionProfile.Name,
                ["session_profile.version"] = sessionProfile.Version, ["session_profile.author"] = sessionProfile.Author,
                ["session_profile.preset_id"] = sessionProfile.PresetId, ["session_profile.preset_index"] = sessionProfile.PresetIndex.ToString(System.Globalization.CultureInfo.InvariantCulture),
                ["session_profile.preset_name"] = sessionProfile.PresetName, ["session_profile.path"] = sessionProfile.PackagePath
            }));
        var resolved = new List<ContentIdentity>(); var required = new List<Capability>(); var unsupported = new List<Capability>();
        var detected = platform.Detect(spec.Installation.RootPath);
        Require("runtime.current-windows-x64", detected.CurrentPlatformSupported, "Current Windows x64 platform validation", required, diagnostics, "OL_E_UNSUPPORTED_OPERATING_SYSTEM");
        Require("transaction.exact-restore", detected.ExactRestoreSupported && detected.InstallationWritable, "installation transaction and exact restore", required, diagnostics, "OL_E_INSTALLATION_NOT_WRITABLE");
        // Compatibility is "OMSI 2 installed". The executable's hashes are
        // diagnostics; native capabilities are resolved and verified in process.
        var build = OmsiBuildDetection.Detect(spec.Installation.RootPath);
        Require("omsi.omsi2", build.IsOmsi2, build.Reason ?? "OMSI 2 installation", required, diagnostics, "OL_E_UNSUPPORTED_BUILD");
        if (build.IsOmsi2)
        {
            diagnostics.Add(new("omsi.build.detected", build.BuildId, new Dictionary<string, string>
            {
                ["build_id"] = build.BuildId, ["identity"] = build.KnownIdentity ? "known" : "unknown", ["file_sha256"] = build.FileSha256 ?? "",
                ["file_version"] = build.FileVersion ?? "", ["code_sha256"] = build.CodeSha256 ?? "", ["file_variant"] = build.KnownFileVariant ?? ""
            }));
            // An optional pin: the caller names the exact Omsi.exe it expects.
            if (spec.Installation.ExpectedExecutableSha256 is { Length: > 0 } pin && LaunchValidation.IsSha256(pin) && !string.Equals(pin, build.FileSha256, StringComparison.OrdinalIgnoreCase))
                diagnostics.Add(new("OL_E_UNSUPPORTED_BUILD", "Omsi.exe does not match ExpectedExecutableSha256.", new Dictionary<string, string>
                {
                    ["expected_sha256"] = pin.ToLowerInvariant(), ["file_sha256"] = build.FileSha256 ?? ""
                }));
            if (!build.KnownIdentity)
                diagnostics.Add(new("OL_W_BUILD_UNRECOGNIZED", "This OMSI 2 executable has no known layout. Each native capability is resolved and verified in process; a capability that cannot be verified is unavailable and a start that needs it fails with OL_E_CAPABILITY_UNRESOLVED."));
        }
        var catalog = new FileSystemContentCatalog(spec.Installation.RootPath);
        if (spec.World.Mode == WorldMode.NewMap && spec.World.MapIdentity.IsSet)
        {
            try
            {
                var map = catalog.ResolveMap(spec.World.MapIdentity.Value!); resolved.Add(new(map.Identity, "map", map.DisplayName)); RequireNative("world.new-map", "native NEW_MAP pipeline", build, required, diagnostics);
                if (spec.World.EntrypointIdentity.IsSet) ResolveEntrypoint(spec.World.EntrypointIdentity.Value!, map.Identity, catalog, resolved, required, diagnostics);
                else Require("world.presented-entrypoint", spec.World.PresentedEntrypointIndex.IsSet, "presented-index entrypoint selection", required, diagnostics, "OL_E_ENTRYPOINT_REQUIRED");
                RequireNative("boot.headless-start", "synchronous Start hook", build, required, diagnostics);
            }
            catch (FileNotFoundException) { Require("content.map", false, "requested map identity", required, diagnostics, "OL_E_MAP_NOT_FOUND"); }
        }
        if (spec.World.Mode == WorldMode.SavedSituation)
        {
            TryResolveSituation(spec, catalog, resolved, required, unsupported, diagnostics, build);
        }
        // LAST MAP STATE: OMSI's own per-map laststn.osn, loaded through the
        // Start form's last-state branch (never the newest .osn of a folder).
        if (spec.World.Mode == WorldMode.LastMapState)
        {
            try
            {
                var map = spec.World.MapIdentity.IsSet ? catalog.ResolveMap(spec.World.MapIdentity.Value!) : throw new FileNotFoundException();
                resolved.Add(new(map.Identity, "map", map.DisplayName));
                var lastState = Path.Combine(spec.Installation.RootPath, Path.GetDirectoryName(map.Identity) ?? "", "laststn.osn");
                if (!File.Exists(lastState)) diagnostics.Add(new("OL_E_SITUATION_NOT_FOUND", "The map has no last state (" + Path.Combine(Path.GetDirectoryName(map.Identity) ?? "", "laststn.osn") + " does not exist)."));
                RequireNative("world.last-map-state", "the Start form's last-state branch", build, required, diagnostics);
                RequireNative("boot.headless-start", "synchronous Start hook", build, required, diagnostics);
            }
            catch (FileNotFoundException) { Require("content.map", false, "requested map identity", required, diagnostics, "OL_E_MAP_NOT_FOUND"); }
        }
        // Start date/time: NEW_MAP writes OMSI's start clock before NewSituation.
        // A saved situation carries its own time ([time] of the .osn).
        var startModes = new[] { ("world.explicit-date", spec.Date.Mode), ("world.explicit-time", spec.Time.Mode), ("world.explicit-year", spec.EffectiveYear.Mode) };
        if (spec.World.Mode == WorldMode.NewMap)
        {
            StartDateTimeResolver.Resolve(spec, DateTime.Now, out var startError);
            if (startError is not null) diagnostics.Add(new("OL_E_DATE_TIME_APPLY_FAILED", "Start date/time: " + startError + "."));
            foreach (var (name, mode) in startModes) if (mode != DateTimeMode.Unset) RequireNative(name, "start clock written before NewSituation", build, required, diagnostics);
        }
        else foreach (var (name, mode) in startModes) RequireOptional(name, mode != DateTimeMode.Unset, false, "Only a NEW_MAP session sets the start date and time; a saved situation keeps its own.", unsupported, diagnostics);
        if (spec.EffectiveWeather.Mode != WeatherMode.Unset)
        {
            if (StartWeatherResolver.Resolve(spec, out var weatherError) is null) diagnostics.Add(new(PublicErrorCodes.WEATHER_INVALID, "Start weather: " + weatherError + "."));
            else RequireNative("world.weather", "start weather applied after the world starts", build, required, diagnostics);
        }
        if (spec.PlayerVehicle.IsSet)
        {
            var player = spec.PlayerVehicle.Value!;
            if (player.Model.IsSet)
            {
                try { var vehicle = catalog.ResolveVehicle(player.Model.Value!); resolved.Add(new(vehicle.Identity, "vehicle", vehicle.DisplayName)); }
                catch (FileNotFoundException) { Require("content.vehicle", false, "requested player vehicle identity", required, diagnostics, "OL_E_VEHICLE_NOT_FOUND"); }
            }
            if (player.Repaint.IsSet && player.Model.IsSet)
            {
                try { var repaint = catalog.EnumerateRepaints(player.Model.Value!).SingleOrDefault(x => string.Equals(x.Identity, player.Repaint.Value, StringComparison.OrdinalIgnoreCase)) ?? throw new FileNotFoundException(); resolved.Add(new(repaint.Identity, "repaint", repaint.DisplayName)); }
                catch (FileNotFoundException) { Require("content.repaint", false, "requested repaint identity", required, diagnostics, "OL_E_REPAINT_NOT_FOUND"); }
            }
            if (player.Hof.IsSet)
            {
                try { var hof = catalog.ResolveHof(player.Hof.Value!); resolved.Add(new(hof.Identity, "hof")); }
                catch (FileNotFoundException) { Require("content.hof", false, "requested HOF identity", required, diagnostics, "OL_E_HOF_NOT_FOUND"); }
            }
            RequireOptional("player-vehicle.model", player.Model.IsSet, false, "Player vehicle model creation is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.repaint", player.Repaint.IsSet, false, "Player vehicle repaint selection is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.hof", player.Hof.IsSet, false, "Player vehicle HOF assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.fleet-number", player.FleetNumber.IsSet, false, "Player vehicle fleet number assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
            RequireOptional("player-vehicle.registration", player.Registration.IsSet, false, "Player vehicle registration assignment is not implemented in the Current plugin runtime.", unsupported, diagnostics);
        }
        // Input documents replace OMSI's keyboard or controller file for the session.
        var inputDocuments = InputDocuments.Load(spec, out var inputError);
        if (inputError is not null) diagnostics.Add(new("OL_E_INVALID_ARGUMENT", inputError));
        foreach (var document in inputDocuments)
        {
            required.Add(new(document.Capability, true, PublicCapabilityRegistry.Get(document.Capability).RuntimeValidation, "replaces " + document.Target + " for the session"));
            mutations.Add(new(document.Target, document.Capability, document.Source, "exact-file-overlay"));
        }
        foreach (var setting in AllSettings(spec))
        {
            if (!ConfigurationCatalog.TryGet(setting.Key, out var definition)) { diagnostics.Add(new("OL_E_UNKNOWN_SETTING", "Unknown semantic setting: " + setting.Key)); continue; }
            if (!definition.Writable) { diagnostics.Add(new("OL_E_SETTING_NOT_WRITABLE", "Known setting is not writable: " + setting.Key)); continue; }
            mutations.Add(new(definition.FileName, setting.Key, setting.Value.Value!, definition.VectorIndex is null ? "token-patch" : "vector-component-patch"));
        }
        var artifacts = new[] { "permanent plugins\\OmsiLaunch.Plugin.opl", "permanent plugins\\OmsiLaunch.PluginNE.dll", "permanent plugins\\OmsiLaunch.Native.x86.dll", "OmsiLaunch startup handoff" };
        try
        {
            foreach (var path in SessionVisualAssets.DescribeTouched(spec)) mutations.Add(new(path,
                path.EndsWith(".itx", StringComparison.OrdinalIgnoreCase) ? "internet-textures.override" :
                path.EndsWith(".ipr", StringComparison.OrdinalIgnoreCase) ? "internet-textures.cache" :
                path.Contains("\\texture\\", StringComparison.OrdinalIgnoreCase) ? "internet-textures.target" :
                "session-presentation.splash", "session", "exact-file-overlay"));
            if (spec.EffectiveInternetTextures.Mode == InternetTexturesMode.Disabled) RequireNative("internet-textures.disabled", "in-process downloader suppression", build, required, diagnostics);
        }
        catch (Exception exception) { diagnostics.Add(new("OL_E_SESSION_PRESENTATION_INVALID", exception.Message)); }
        try { mutations.AddRange(HofPlanner.Build(spec).Mutations); }
        catch (HofPlanningException exception) { diagnostics.Add(new(exception.Code, exception.Message)); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { diagnostics.Add(new("OL_E_HOF_INVALID", exception.Message)); }
        var sessionMap = spec.World.Mode == WorldMode.NewMap ? (spec.World.MapIdentity.IsSet ? spec.World.MapIdentity.Value : null) : resolved.FirstOrDefault(x => x.Kind == "situation-map")?.Identity;
        var modules = PlanModules(spec, build, sessionMap, diagnostics);
        var touched = mutations.Select(x => x.RelativePath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        // Informational provenance (for example session_profile.selected) must
        // not turn an otherwise valid plan into a non-runnable plan. Public
        // planning errors are explicitly identified by the stable OL_E_ code.
        var runnable = diagnostics.All(diagnostic => !diagnostic.Code.StartsWith("OL_E_", StringComparison.Ordinal));
        return Task.FromResult(new SessionPlan(Guid.NewGuid(), build.BuildId, spec, detected, resolved, touched, artifacts, required, unsupported, mutations, diagnostics, runnable, modules));
    }

    // Lifecycle step 2: runtime modules. Loading is open (E21): every module
    // installed below plugins\OmsiLaunch.Modules may join. A requested module
    // that cannot be loaded, or an invalid module request or setting, makes
    // the plan not runnable; a module that joined by itself (settings file,
    // content, default) and cannot be loaded is OL_W_MODULE_NOT_LOADED, and a
    // settings-file entry that cannot be used is OL_W_MODULE_SETTING_IGNORED.
    private static IReadOnlyList<PlannedModule> PlanModules(LaunchSpec spec, OmsiBuildReport build, string? sessionMap, List<LaunchDiagnostic> diagnostics)
    {
        var root = spec.Installation.RootPath;
        var request = spec.EffectiveModules;
        var file = ModuleSettingsFile.Read(root);
        foreach (var problem in file.Problems) diagnostics.Add(new(PublicErrorCodes.MODULE_SETTING_IGNORED, problem));
        var modulesRoot = Path.Combine(root, RuntimeModuleProtocol.ModulesDirectory);
        if (request.Enable.Count == 0 && request.EffectiveDisable.Count == 0 && request.EffectiveSettings.Count == 0 && !file.ModuleIds.Any() && !Directory.Exists(modulesRoot)) return Array.Empty<PlannedModule>();
        var selection = new ModuleSelection(request.Enable, request.EffectiveDisable, request.EffectiveSettings, file, PluginInventory.ActiveLibraries(root));
        var plan = ModuleEligibility.Plan(ModuleCatalog.Discover(root), selection, root, sessionMap, name => ModuleCapability(name, build));
        foreach (var problem in plan.Problems)
            diagnostics.Add(new(problem.Error ? PublicErrorCodes.MODULE_SETTING_INVALID : PublicErrorCodes.MODULE_SETTING_IGNORED, "Runtime module " + problem.ModuleId + ": " + problem.Message, new Dictionary<string, string> { ["module"] = problem.ModuleId }));
        foreach (var id in plan.Disabled) diagnostics.Add(new("module.disabled", id, new Dictionary<string, string> { ["module"] = id }));
        foreach (var module in plan.Modules)
        {
            var data = new Dictionary<string, string> { ["module"] = module.Id, ["status"] = module.Status, ["activation"] = module.Activation, ["code"] = module.Code ?? "", ["reason"] = module.Reason ?? "" };
            if (module.IsEligible) diagnostics.Add(new("module.planned", module.Id, data));
            else if (module.Requested) diagnostics.Add(new(module.Code ?? PublicErrorCodes.MODULE_NOT_FOUND, "Runtime module " + module.Id + " cannot be loaded: " + module.Reason, data));
            else diagnostics.Add(new(PublicErrorCodes.MODULE_NOT_LOADED_WARNING, "Runtime module " + module.Id + " (" + module.Activation + ") is not loaded: " + module.Reason, data));
        }
        // The startup handoff carries at most StartupHandoffWire.MaxModules modules; more would
        // make the plugin reject the handoff after the plan was accepted.
        if (plan.Modules.Count(module => module.IsEligible) > StartupHandoffWire.MaxModules)
            diagnostics.Add(new(PublicErrorCodes.INVALID_ARGUMENT, "At most " + StartupHandoffWire.MaxModules + " runtime modules can join one session; disable some."));
        return plan.Modules;
    }

    private static ModuleCapabilityState ModuleCapability(string name, OmsiBuildReport build)
    {
        if (RuntimeModuleProtocol.BridgeFeatures.Contains(name))
            return RuntimeModuleProtocol.BridgeFeatureNeeds.TryGetValue(name, out var need) ? ModuleCapability(need, build) : ModuleCapabilityState.Available;
        if (!NativeTargetCatalog.Capabilities.Any(x => x.Id == name)) return ModuleCapabilityState.Unknown;
        if (!build.IsOmsi2) return ModuleCapabilityState.Unavailable;
        return build.KnownIdentity || (build.StaticCapabilities.TryGetValue(name, out var available) && available) ? ModuleCapabilityState.Available : ModuleCapabilityState.Pending;
    }

    // A native capability the start needs. Its availability is decided in
    // process; the plan records the evidence the executable file gives:
    // the known layout, a static resolution of an unknown build, or nothing
    // (then the start is still attempted and the plugin decides).
    private static void RequireNative(string name, string reason, OmsiBuildReport build, List<Capability> required, List<LaunchDiagnostic> diagnostics)
    {
        var native = PublicCapabilityRegistry.Get(name).NativeCapability ?? name;
        if (!build.IsOmsi2) return;
        var evidence = build.KnownIdentity ? "RESOLVED_KNOWN_LAYOUT"
            : build.StaticCapabilities.TryGetValue(native, out var available) && available ? "STATICALLY_RESOLVED" : "PENDING_IN_PROCESS_RESOLUTION";
        required.Add(new(name, true, evidence, reason));
        if (evidence == "PENDING_IN_PROCESS_RESOLUTION")
            diagnostics.Add(new("OL_W_CAPABILITY_PENDING", "Capability " + name + " could not be resolved from the executable file; it is resolved in process at start."));
    }

    private static void Require(string name, bool available, string reason, List<Capability> required, List<LaunchDiagnostic> diagnostics, string code)
    {
        PublicCapabilityRegistry.Get(name);
        required.Add(new(name, available, available ? "STATICALLY_VALIDATED" : "UNAVAILABLE", reason));
        if (!available && !string.IsNullOrWhiteSpace(code)) diagnostics.Add(new(code, "Required capability unavailable: " + name));
    }

    // The startup selects the entrypoint by the exact label OMSI presents; the
    // handoff carries that label (the "entrypoint" entry of ResolvedContent).
    private static void ResolveEntrypoint(string identity, string mapIdentity, FileSystemContentCatalog catalog, List<ContentIdentity> resolved, List<Capability> required, List<LaunchDiagnostic> diagnostics)
    {
        var entrypoint = catalog.ResolveEntrypointLabel(mapIdentity, identity);
        if (!entrypoint.Found) { diagnostics.Add(new(PublicErrorCodes.ENTRYPOINT_NOT_FOUND, "The map has no entrypoint with this identity or exact label.")); return; }
        if (!entrypoint.Presented) { diagnostics.Add(new(PublicErrorCodes.ENTRYPOINT_NOT_PRESENTED, "OMSI presents only the first entrypoint with the label '" + entrypoint.Label + "'; this one cannot be selected at start.")); return; }
        resolved.Add(new(identity, "entrypoint", entrypoint.Label!));
        required.Add(new("world.entrypoint-identity", true, PublicCapabilityRegistry.Get("world.entrypoint-identity").RuntimeValidation, "entrypoint selected by its exact presented label"));
    }

    private static void TryResolveSituation(LaunchSpec spec, FileSystemContentCatalog catalog, List<ContentIdentity> resolved, List<Capability> required, List<Capability> unsupported, List<LaunchDiagnostic> diagnostics, OmsiBuildReport build)
    {
        try
        {
            var situation = catalog.ResolveSituation(spec.World.SituationIdentity.Value!);
            resolved.Add(new(situation.Identity, "situation", situation.MapIdentity));
            if (!string.IsNullOrWhiteSpace(situation.MapIdentity))
            {
                try { var map = catalog.ResolveMap(situation.MapIdentity); resolved.Add(new(map.Identity, "situation-map", map.DisplayName)); }
                catch (FileNotFoundException) { Require("content.situation-map", false, "map referenced by the selected .osn", required, diagnostics, "OL_E_SITUATION_MAP_NOT_FOUND"); }
            }
            RequireNative("world.saved-situation", "Start-form situation selection and Button1Click", build, required, diagnostics);
            RequireNative("boot.headless-start", "synchronous Start hook", build, required, diagnostics);
        }
        catch (FileNotFoundException)
        {
            Require("content.situation", false, "requested situation identity", required, diagnostics, "OL_E_SITUATION_NOT_FOUND");
        }
    }

    // Evidence comes from the canonical catalog (R3); an undeclared id throws.
    private static void RequireOptional(string name, bool requested, bool available, string reason, List<Capability> unsupported, List<LaunchDiagnostic> diagnostics)
    {
        if (!requested) return;
        unsupported.Add(new(name, available, PublicCapabilityRegistry.Get(name).RuntimeValidation, reason));
        if (!available) diagnostics.Add(new("OL_E_CAPABILITY_UNAVAILABLE", "Requested capability unavailable: " + name));
    }
    private static IEnumerable<KeyValuePair<string, OptionalValue<string>>> AllSettings(LaunchSpec spec) => spec.Environment.General.Concat(spec.Environment.Advanced).Concat(spec.Environment.Graphics).Concat(spec.Environment.AdvancedGraphics).Concat(spec.Environment.Sound).Concat(spec.Environment.AiPassengers).Concat(spec.Environment.Keyboard).Concat(spec.Environment.Controllers).Where(x => x.Value.IsSet);
}
