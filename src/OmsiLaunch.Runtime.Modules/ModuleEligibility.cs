using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// How the plan sees a capability a module names.
public enum ModuleCapabilityState : byte { Available, Pending, Unavailable, Unknown }

// What a session asks for: modules requested and disabled (LaunchSpec,
// command line, session profile), settings by "<module-id>.<key>", the user's
// settings file, and the OMSI plugin DLLs that are switched on (DLL file name
// to the .opl that loads it).
public sealed record ModuleSelection(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable, IReadOnlyDictionary<string, string> Settings, ModuleSettingsFile File, IReadOnlyDictionary<string, string> ActivePlugins)
{
    public static ModuleSelection Requesting(IReadOnlyList<string> enable) =>
        new(enable, Array.Empty<string>(), new Dictionary<string, string>(StringComparer.Ordinal), ModuleSettingsFile.Empty, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
}

// The plan's modules, the problems found in the request and the settings
// file, and the modules a disable kept out.
public sealed record ModulePlan(IReadOnlyList<PlannedModule> Modules, IReadOnlyList<ModuleSettingProblem> Problems, IReadOnlyList<string> Disabled);

// Lifecycle step 2 (controller): which discovered modules the session loads.
// A module joins when it is requested, switched on in the settings file,
// bound to the session's map (activation content) or loaded by default
// (activation default), unless it is disabled. It is ELIGIBLE when it is
// usable (intact), speaks a bridge protocol this build serves, every required
// capability is available or pending in-process resolution, its content
// binding holds and no conflicting OMSI plugin is switched on.
public static class ModuleEligibility
{
    public static IReadOnlyList<PlannedModule> Evaluate(
        IReadOnlyList<DiscoveredModule> discovered,
        IReadOnlyList<string> requested,
        string installationRoot,
        string? sessionMapIdentity,
        Func<string, ModuleCapabilityState> capability) =>
        Plan(discovered, ModuleSelection.Requesting(requested), installationRoot, sessionMapIdentity, capability).Modules;

    public static ModulePlan Plan(
        IReadOnlyList<DiscoveredModule> discovered,
        ModuleSelection selection,
        string installationRoot,
        string? sessionMapIdentity,
        Func<string, ModuleCapabilityState> capability)
    {
        var result = new List<PlannedModule>();
        var problems = new List<ModuleSettingProblem>();
        var disabledByUser = new List<string>();
        var byId = discovered.Where(x => x.Manifest is not null).GroupBy(x => x.Manifest!.Id, StringComparer.Ordinal).ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
        var byDirectory = discovered.ToDictionary(x => x.DirectoryName, StringComparer.OrdinalIgnoreCase);
        var enable = selection.Enable.Distinct(StringComparer.Ordinal).ToArray();
        var disable = new HashSet<string>(selection.Disable, StringComparer.Ordinal);
        foreach (var id in enable.Where(disable.Contains)) problems.Add(new(id, "the module is both enabled and disabled", true));
        foreach (var id in disable.Where(x => !RuntimeModuleProtocol.IsModuleId(x))) problems.Add(new(id, "'" + id + "' is not a module id", true));
        var requestedSettings = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        foreach (var (name, value) in selection.Settings)
        {
            var dot = name.IndexOf('.');
            var id = dot > 0 ? name[..dot] : name; var key = dot > 0 ? name[(dot + 1)..] : "";
            if (!RuntimeModuleProtocol.IsModuleId(id) || !RuntimeModuleProtocol.IsSettingKey(key)) { problems.Add(new(id, "'" + name + "' is not <module-id>.<setting>", true)); continue; }
            if (!requestedSettings.TryGetValue(id, out var keys)) requestedSettings[id] = keys = new(StringComparer.Ordinal);
            keys[key] = value;
        }
        PlannedModule Consider(DiscoveredModule module, bool requested, string activation)
        {
            var id = module.Manifest?.Id ?? module.DirectoryName;
            return Evaluate(module, requested, activation, installationRoot, sessionMapIdentity, capability, selection.ActivePlugins,
                requestedSettings.TryGetValue(id, out var keys) ? keys : new Dictionary<string, string>(StringComparer.Ordinal), selection.File.Section(id), problems);
        }

        // 1. Requested explicitly; an explicit request wins over the settings file.
        foreach (var id in enable.Where(x => !disable.Contains(x)))
        {
            if (!RuntimeModuleProtocol.IsModuleId(id)) { result.Add(Rejected(id, null, true, PlannedModule.ActivationRequested, PublicErrorCodes.MODULE_NOT_FOUND, "'" + id + "' is not a module id")); continue; }
            if (byId.TryGetValue(id, out var module) || byDirectory.TryGetValue(id, out module)) result.Add(Consider(module, true, PlannedModule.ActivationRequested));
            else result.Add(Rejected(id, null, true, PlannedModule.ActivationRequested, PublicErrorCodes.MODULE_NOT_FOUND, "no module '" + id + "' is installed below " + RuntimeModuleProtocol.ModulesDirectory));
        }
        // 2. Switched on in the settings file.
        foreach (var id in selection.File.ModuleIds.Where(x => selection.File.Enabled(x) == true && !disable.Contains(x)).OrderBy(x => x, StringComparer.Ordinal))
        {
            if (result.Any(x => x.Id == id)) continue;
            if (byId.TryGetValue(id, out var module) || byDirectory.TryGetValue(id, out module)) result.Add(Consider(module, false, PlannedModule.ActivationSettingsFile));
            else problems.Add(new(id, RuntimeModuleProtocol.SettingsFile + " enables module '" + id + "', which is not installed; ignored", false));
        }
        // 3. Content modules bound to this map and 4. default modules, unless
        // disabled by the request or by the settings file.
        foreach (var module in discovered.Where(x => x.IsUsable && x.Manifest!.Activation is ModuleActivation.Content or ModuleActivation.Default))
        {
            var manifest = module.Manifest!;
            if (result.Any(x => x.Id == manifest.Id)) continue;
            if (manifest.Activation == ModuleActivation.Content && (sessionMapIdentity is null || !string.Equals(manifest.ContentBinding!.Map, sessionMapIdentity, StringComparison.OrdinalIgnoreCase))) continue;
            if (disable.Contains(manifest.Id) || selection.File.Enabled(manifest.Id) == false) { disabledByUser.Add(manifest.Id); continue; }
            result.Add(Consider(module, false, manifest.Activation == ModuleActivation.Content ? PlannedModule.ActivationContent : PlannedModule.ActivationDefault));
        }
        // Settings requested for a module this session does not load.
        foreach (var id in requestedSettings.Keys.Where(id => !result.Any(x => x.Id == id && x.IsEligible)))
            problems.Add(new(id, "module '" + id + "' is not loaded in this session; its settings are ignored", false));
        return new(result, problems, disabledByUser);
    }

    private static PlannedModule Evaluate(DiscoveredModule module, bool requested, string activation, string installationRoot, string? sessionMapIdentity, Func<string, ModuleCapabilityState> capability,
        IReadOnlyDictionary<string, string> activePlugins, IReadOnlyDictionary<string, string> requestedSettings, IReadOnlyDictionary<string, string>? fileSettings, List<ModuleSettingProblem> problems)
    {
        var manifest = module.Manifest;
        if (manifest is null || module.Code is not null) return Rejected(manifest?.Id ?? module.DirectoryName, manifest, requested, activation, module.Code ?? PublicErrorCodes.MODULE_MANIFEST_INVALID, module.Message ?? "the module is not usable");
        if (!RuntimeModuleProtocol.IsCompatible(manifest.BridgeProtocolMin, manifest.BridgeProtocolMax))
            return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_PROTOCOL_UNSUPPORTED, "the module needs bridge protocol " + manifest.BridgeProtocolMin + ".." + manifest.BridgeProtocolMax + "; this build serves " + RuntimeModuleProtocol.MinimumBridgeProtocol + ".." + RuntimeModuleProtocol.BridgeProtocol);
        foreach (var name in manifest.RequiredCapabilities)
        {
            var state = capability(name);
            if (state is ModuleCapabilityState.Unknown) return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "the capability '" + name + "' is not provided by this build");
            if (state is ModuleCapabilityState.Unavailable) return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "the required capability '" + name + "' is not available");
        }
        foreach (var name in manifest.OptionalCapabilities)
            if (capability(name) is ModuleCapabilityState.Unknown) return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "the capability '" + name + "' is not provided by this build");
        if (manifest.ContentBinding is { } binding)
        {
            var failure = CheckBinding(binding, installationRoot, sessionMapIdentity);
            if (failure is not null) return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_CONTENT_BINDING, failure);
        }
        foreach (var plugin in manifest.EffectiveConflictingPlugins)
            if (activePlugins.TryGetValue(plugin, out var opl))
                return Rejected(manifest.Id, manifest, requested, activation, PublicErrorCodes.MODULE_CONFLICT, opl + " loads " + plugin + ", which must not run next to this module; remove or switch off that plugin");
        var settings = ModuleSettings.Resolve(manifest, requestedSettings, fileSettings, problems)
            .Select(x => new PlannedModuleSetting(x.Key, x.Value, ModuleSettings.SourceName(x.Source))).ToArray();
        return new(manifest.Id, manifest.Version, PlannedModule.Eligible, requested, Policy(manifest.FailurePolicy), Operations(manifest), ManifestSha256: module.ManifestSha256, Activation: activation, Settings: settings);
    }

    // Null when the binding holds: the session runs the bound map and every
    // bound file has its pinned SHA-256. This is context validation, not
    // protection against copying.
    public static string? CheckBinding(ModuleContentBinding binding, string installationRoot, string? sessionMapIdentity)
    {
        if (sessionMapIdentity is null) return "the session map is unknown; the module is bound to " + binding.Map;
        if (!string.Equals(binding.Map, sessionMapIdentity, StringComparison.OrdinalIgnoreCase)) return "the module is bound to " + binding.Map + ", the session runs " + sessionMapIdentity;
        var root = Path.GetFullPath(installationRoot).TrimEnd('\\') + "\\";
        foreach (var file in binding.Files)
        {
            var path = Path.GetFullPath(Path.Combine(root, file.Path));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase) || !File.Exists(path)) return "the bound file " + file.Path + " is missing";
            try
            {
                using var stream = File.OpenRead(path);
                if (!string.Equals(Convert.ToHexString(ModuleCatalog.HashStream(stream)), file.Sha256, StringComparison.OrdinalIgnoreCase)) return "the bound file " + file.Path + " does not match its pinned SHA-256";
            }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return "the bound file " + file.Path + " cannot be read: " + exception.Message; }
        }
        return null;
    }

    public static string Policy(ModuleFailurePolicy policy) => policy switch
    {
        ModuleFailurePolicy.FailSession => "fail-session",
        ModuleFailurePolicy.DisableModule => "disable-module",
        _ => "degrade"
    };

    public static string ActivationName(ModuleActivation activation) => activation switch
    {
        ModuleActivation.Content => PlannedModule.ActivationContent,
        ModuleActivation.Default => PlannedModule.ActivationDefault,
        _ => PlannedModule.ActivationRequested
    };

    private static IReadOnlyList<string> Operations(ModuleManifest manifest) => manifest.Operations.Select(x => manifest.OperationName(x.Id)).ToArray();
    private static PlannedModule Rejected(string id, ModuleManifest? manifest, bool requested, string activation, string code, string reason) =>
        new(id, manifest?.Version ?? "", PlannedModule.Rejected, requested, manifest is null ? "disable-module" : Policy(manifest.FailurePolicy), Array.Empty<string>(), code, reason, Activation: activation);
}
