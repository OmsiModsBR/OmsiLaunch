using System.Globalization;
using OmsiLaunch.Api;
using OmsiLaunch.Configuration;
using OmsiLaunch.Runtime.Modules;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace OmsiLaunch.Core;

/// <summary>Strict compiler for public, declarative session-profile packages.</summary>
public static class SessionProfileCompiler
{
    public const string Schema = "omsilaunch.session-profile/v1";
    public const long MaxBytes = 256 * 1024;
    // A package holds 1..99 presets; /predefined-profile-index selects one by
    // its declared index. Beta 3 allowed 1..5.
    public const int MinPresetIndex = 1;
    public const int MaxPresetIndex = 99;
    // Every key the strict compiler accepts, by mapping context. Documentation
    // and the documentation gate are derived from this table, and the Mapping
    // calls below must stay in agreement with it.
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal)
    {
        ["profile"] = new[] { "schema", "id", "name", "author", "version", "compatibility", "new", "presets" },
        ["compatibility"] = new[] { "maps" },
        ["new"] = new[] { "map", "entrypoint-index", "entrypoint", "date", "time", "year", "weather" },
        ["new.date"] = new[] { "mode", "value" },
        ["new.time"] = new[] { "mode", "value" },
        ["new.weather"] = new[] { "mode", "preset", "icao" },
        ["preset"] = new[] { "index", "id", "name", "settings", "presentation", "internet-textures", "behavior", "hofs", "modules" },
        // One entry per runtime module id under modules.
        ["module"] = new[] { "enabled", "settings" },
        ["hofs"] = new[] { "package", "collection", "directory", "vehicle", "file", "targets", "files", "mode" },
        ["presentation"] = new[] { "splash" },
        ["presentation.splash"] = new[] { "mode", "language", "assets" },
        ["internet-textures"] = new[] { "mode", "profile" },
        ["behavior"] = new[] { "startup-timeout", "shutdown-timeout" }
    };

    public static SessionProfilePackage Load(string installationRoot, string id, int presetIndex)
    {
        ValidateId(id);
        if (presetIndex is < MinPresetIndex or > MaxPresetIndex) throw Error("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", "The session profile preset index must be between 1 and 99.");
        var parsed = Parse(installationRoot, id);
        var preset = parsed.Presets.SingleOrDefault(x => x.Index == presetIndex) ?? throw Error("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", "The requested preset was not found in this session profile.");
        return new SessionProfilePackage(parsed.RootPath, new(id, parsed.Name, parsed.Version, parsed.Author, preset.Id, preset.Index, preset.Name, parsed.RootPath), parsed.CompatibleMaps, parsed.New, preset);
    }

    /// <summary>
    /// Lists every package under <c>.omsilaunch\session-profiles</c>. Each package
    /// is compiled with the same strict rules as a launch; a package that does
    /// not compile is reported with its error code instead of being skipped.
    /// Nothing is written.
    /// </summary>
    public static IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)
    {
        var profiles = Path.GetFullPath(Path.Combine(installationRoot, ".omsilaunch", "session-profiles"));
        if (!Directory.Exists(profiles)) return Array.Empty<SessionProfileSummary>();
        var output = new List<SessionProfileSummary>();
        foreach (var directory in Directory.EnumerateDirectories(profiles).OrderBy(x => x, StringComparer.OrdinalIgnoreCase))
        {
            var id = Path.GetFileName(directory);
            try
            {
                ValidateId(id);
                var parsed = Parse(installationRoot, id);
                output.Add(new(id, parsed.Name, parsed.Version, parsed.Author, parsed.CompatibleMaps, parsed.New.Map,
                    parsed.Presets.OrderBy(x => x.Index).Select(x => new SessionProfilePresetSummary(x.Index, x.Id, x.Name)).ToArray(), true, null, null));
            }
            catch (SessionProfileException exception)
            {
                output.Add(new(id, null, null, null, Array.Empty<string>(), null, Array.Empty<SessionProfilePresetSummary>(), false, exception.Code, exception.Message));
            }
        }
        return output;
    }

    private static void ValidateId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || id.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 || id.Contains("..", StringComparison.Ordinal))
            throw Error("OL_E_SESSION_PROFILE_PATH_ESCAPE", "The session profile identifier is not a package directory name.");
    }

    private static ParsedProfile Parse(string installationRoot, string id)
    {
        var profiles = Path.GetFullPath(Path.Combine(installationRoot, ".omsilaunch", "session-profiles"));
        var root = Confined(profiles, id);
        var yamlPath = Path.Combine(root, "profile.yaml");
        if (!File.Exists(yamlPath)) throw Error("OL_E_SESSION_PROFILE_NOT_FOUND", "Session profile '" + id + "' was not found.");
        if (new FileInfo(yamlPath).Length > MaxBytes) throw Error("OL_E_SESSION_PROFILE_INVALID", "The session profile exceeds the 256 KiB safety limit.");

        try
        {
            using var reader = File.OpenText(yamlPath);
            var stream = new YamlStream(); stream.Load(reader);
            if (stream.Documents.Count != 1 || stream.Documents[0].RootNode is not YamlMappingNode document)
                throw Error("OL_E_SESSION_PROFILE_INVALID", "The session profile must contain one root mapping.");
            RejectAnchors(document);
            var rootMap = Mapping(document, "profile", "schema", "id", "name", "author", "version", "compatibility", "new", "presets");
            var schema = RequiredScalar(rootMap, "schema");
            if (!string.Equals(schema, Schema, StringComparison.Ordinal)) throw Error("OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED", "Unsupported session profile schema: " + schema);
            var declaredId = RequiredScalar(rootMap, "id");
            if (!string.Equals(declaredId, id, StringComparison.Ordinal)) throw Error("OL_E_SESSION_PROFILE_INVALID", "The profile id must match its package directory name.");
            var name = RequiredScalar(rootMap, "name");
            var author = RequiredScalar(rootMap, "author");
            var version = RequiredScalar(rootMap, "version");
            var maps = ReadCompatibility(rootMap);
            var world = rootMap.TryGetValue("new", out var worldNode) ? ReadNew(Mapping(worldNode, "new", "map", "entrypoint-index", "entrypoint", "date", "time", "year", "weather")) : new ProfileNew();
            var presets = ReadPresets(rootMap, root, installationRoot);
            return new ParsedProfile(root, name, version, author, maps, world, presets);
        }
        catch (SessionProfileException) { throw; }
        catch (YamlException exception) { throw Error("OL_E_SESSION_PROFILE_INVALID", "Invalid YAML: " + exception.Message); }
        catch (Exception exception) when (exception is InvalidDataException or FormatException or OverflowException)
        { throw Error("OL_E_SESSION_PROFILE_INVALID", exception.Message); }
    }

    public static LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)
    {
        var world = baseline.World;
        var date = baseline.Date; var time = baseline.Time; var year = baseline.EffectiveYear; var weather = baseline.EffectiveWeather;
        if (selectedWorldMode == WorldMode.NewMap)
        {
            if (profile.New.Map is not null) world = world with { MapIdentity = OptionalValue<string>.Set(profile.New.Map) };
            if (profile.New.EntrypointIndex is { } index) world = world with { PresentedEntrypointIndex = OptionalValue<int>.Set(index), EntrypointIdentity = OptionalValue<string>.Unset };
            if (profile.New.EntrypointIdentity is not null) world = world with { EntrypointIdentity = OptionalValue<string>.Set(profile.New.EntrypointIdentity), PresentedEntrypointIndex = OptionalValue<int>.Unset };
            if (profile.New.Date is not null) date = new(DateTimeMode.Explicit, OptionalValue<SemanticDate>.Set(profile.New.Date));
            if (profile.New.Time is not null) time = new(DateTimeMode.Explicit, OptionalValue<SemanticTime>.Set(profile.New.Time));
            if (profile.New.Year is { } value) year = new(DateTimeMode.Explicit, OptionalValue<int>.Set(value));
            if (profile.New.Weather is not null) weather = profile.New.Weather;
        }
        var settings = new Dictionary<string, OptionalValue<string>>(baseline.Environment.General, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in profile.Preset.Settings) settings[pair.Key] = OptionalValue<string>.Set(pair.Value);
        if (selectedWorldMode == WorldMode.NewMap) ValidateMapCompatibility(profile, baseline.Installation.RootPath, world, selectedWorldMode);
        return baseline with
        {
            World = world,
            Date = date,
            Time = time,
            Year = year,
            Weather = weather,
            Environment = baseline.Environment with { General = settings },
            Presentation = profile.Preset.Presentation ?? baseline.Presentation,
            InternetTextures = profile.Preset.InternetTextures ?? baseline.InternetTextures,
            Behavior = profile.Preset.Behavior ?? baseline.Behavior,
            HofManagement = profile.Preset.Hofs.Count != 0 ? new HofManagementSpec(profile.Preset.Hofs) : baseline.HofManagement,
            Modules = profile.Preset.Modules is { } modules ? MergeModules(baseline.EffectiveModules, modules) : baseline.Modules,
            SessionProfile = profile.Metadata
        };
    }

    public static void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode) => ValidateMapCompatibility(profile, installationRoot, world, mode);

    // The preset's modules join what the baseline (a /spec file) asked for;
    // a module setting the preset names wins over the baseline's.
    private static RuntimeModulesSpec MergeModules(RuntimeModulesSpec baseline, RuntimeModulesSpec preset)
    {
        var settings = new Dictionary<string, string>(baseline.EffectiveSettings, StringComparer.Ordinal);
        foreach (var pair in preset.EffectiveSettings) settings[pair.Key] = pair.Value;
        var disable = baseline.EffectiveDisable.Concat(preset.EffectiveDisable).Distinct(StringComparer.Ordinal).ToArray();
        var enable = baseline.Enable.Where(x => !preset.EffectiveDisable.Contains(x)).Concat(preset.Enable).Distinct(StringComparer.Ordinal).ToArray();
        return new(enable, disable.Where(x => !preset.Enable.Contains(x)).ToArray(), settings);
    }
    private static void ValidateMapCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)
    {
        if (profile.CompatibleMaps.Count == 0) return;
        string? map = mode switch
        {
            WorldMode.NewMap when world.MapIdentity.IsSet => world.MapIdentity.Value,
            WorldMode.SavedSituation when world.SituationIdentity.IsSet => new OmsiLaunch.Content.FileSystemContentCatalog(installationRoot).ResolveSituation(world.SituationIdentity.Value!).MapIdentity,
            _ => null
        };
        if (map is null || !profile.CompatibleMaps.Contains(Canonical(map), StringComparer.OrdinalIgnoreCase))
            throw Error("OL_E_SESSION_PROFILE_MAP_MISMATCH", "The selected world is not compatible with this session profile.");
    }

    private static IReadOnlyList<ProfilePreset> ReadPresets(IReadOnlyDictionary<string, YamlNode> root, string packageRoot, string installationRoot)
    {
        if (!root.TryGetValue("presets", out var node) || node is not YamlSequenceNode sequence || sequence.Children.Count is < 1 or > MaxPresetIndex)
            throw Error("OL_E_SESSION_PROFILE_INVALID", "A session profile requires between 1 and 99 presets.");
        var output = new List<ProfilePreset>();
        foreach (var child in sequence.Children)
        {
            var preset = Mapping(child, "preset", "index", "id", "name", "settings", "presentation", "internet-textures", "behavior", "hofs", "modules");
            var index = int.Parse(RequiredScalar(preset, "index"), CultureInfo.InvariantCulture);
            if (index is < MinPresetIndex or > MaxPresetIndex || output.Any(x => x.Index == index)) throw Error("OL_E_SESSION_PROFILE_INVALID", "Preset indexes must be unique values between 1 and 99.");
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (preset.TryGetValue("settings", out var settingsNode))
                foreach (var pair in Mapping(settingsNode, "settings"))
                {
                    var key = ScalarKey(pair.Key); var value = Scalar(pair.Value, "settings." + key);
                    if (!ConfigurationCatalog.TryGet(key, out var setting)) throw Error("OL_E_SESSION_PROFILE_SETTING_UNKNOWN", "Unknown session-profile setting: " + key);
                    if (!setting.Writable) throw Error("OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE", "Session-profile setting is not writable: " + key);
                    settings.Add(key, value);
                }
            SessionPresentationSpec? presentation = preset.TryGetValue("presentation", out var presentationNode) ? ReadPresentation(Mapping(presentationNode, "presentation", "splash"), packageRoot) : null;
            InternetTexturesSpec? internet = preset.TryGetValue("internet-textures", out var internetNode) ? ReadInternet(Mapping(internetNode, "internet-textures", "mode", "profile"), packageRoot) : null;
            LaunchBehaviorSpec? behavior = preset.TryGetValue("behavior", out var behaviorNode)
                ? ReadBehavior(Mapping(behaviorNode, "behavior", "startup-timeout", "shutdown-timeout"))
                : null;
            var hofs = preset.TryGetValue("hofs", out var hofsNode) ? ReadHofs(hofsNode, packageRoot, installationRoot) : Array.Empty<HofOperationSpec>();
            var modules = preset.TryGetValue("modules", out var modulesNode) ? ReadModules(modulesNode) : null;
            output.Add(new(index, RequiredScalar(preset, "id"), RequiredScalar(preset, "name"), settings, presentation, internet, behavior, hofs, modules));
        }
        return output;
    }

    // modules: { <module-id>: { enabled: true|false, settings: { <key>: <value> } } }.
    // Values are validated against the module's declaration when the session
    // is planned; here only the shape is checked.
    private static RuntimeModulesSpec ReadModules(YamlNode node)
    {
        var enable = new List<string>(); var disable = new List<string>();
        var settings = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (id, value) in Mapping(node, "modules"))
        {
            if (!RuntimeModuleProtocol.IsModuleId(id)) throw Error("OL_E_SESSION_PROFILE_INVALID", "modules: '" + id + "' is not a module id.");
            var module = Mapping(value, "modules." + id, "enabled", "settings");
            if (module.TryGetValue("enabled", out var enabledNode))
            {
                var enabled = Scalar(enabledNode, "modules." + id + ".enabled").ToLowerInvariant() switch
                {
                    "true" => true,
                    "false" => false,
                    _ => throw Error("OL_E_SESSION_PROFILE_INVALID", "modules." + id + ".enabled must be true or false.")
                };
                (enabled ? enable : disable).Add(id);
            }
            if (module.TryGetValue("settings", out var settingsNode))
                foreach (var (key, setting) in Mapping(settingsNode, "modules." + id + ".settings"))
                {
                    if (!RuntimeModuleProtocol.IsSettingKey(key)) throw Error("OL_E_SESSION_PROFILE_INVALID", "modules." + id + ".settings: '" + key + "' is not a setting key.");
                    settings[id + "." + key] = Scalar(setting, "modules." + id + ".settings." + key);
                }
        }
        return new(enable, disable, settings);
    }

    private static ProfileNew ReadNew(IReadOnlyDictionary<string, YamlNode> map)
    {
        var result = new ProfileNew { Map = OptionalScalar(map, "map"), EntrypointIdentity = OptionalScalar(map, "entrypoint") };
        if (map.TryGetValue("entrypoint-index", out var entry)) result = result with { EntrypointIndex = int.Parse(Scalar(entry, "entrypoint-index"), CultureInfo.InvariantCulture) };
        if (map.TryGetValue("date", out var date)) result = result with { Date = ReadDate(Mapping(date, "new.date", "mode", "value")) };
        if (map.TryGetValue("time", out var time)) result = result with { Time = ReadTime(Mapping(time, "new.time", "mode", "value")) };
        if (map.TryGetValue("year", out var year)) result = result with { Year = int.Parse(Scalar(year, "year"), CultureInfo.InvariantCulture) };
        if (map.TryGetValue("weather", out var weather)) result = result with { Weather = ReadWeather(Mapping(weather, "new.weather", "mode", "preset", "icao")) };
        return result;
    }
    private static SemanticDate ReadDate(IReadOnlyDictionary<string, YamlNode> map) { RequireMode(map, "date"); var value = DateOnly.Parse(RequiredScalar(map, "value"), CultureInfo.InvariantCulture); return new(value.Year, value.Month, value.Day); }
    private static SemanticTime ReadTime(IReadOnlyDictionary<string, YamlNode> map) { RequireMode(map, "time"); var value = TimeOnly.Parse(RequiredScalar(map, "value"), CultureInfo.InvariantCulture); return new(value.Hour, value.Minute, value.Second); }
    private static WeatherSpec ReadWeather(IReadOnlyDictionary<string, YamlNode> map)
    {
        var mode = RequiredScalar(map, "mode").ToLowerInvariant();
        return mode switch { "preset" => new(WeatherMode.Preset, OptionalValue<string>.Set(RequiredScalar(map, "preset")), OptionalValue<string>.Unset), "icao" => new(WeatherMode.Icao, OptionalValue<string>.Unset, OptionalValue<string>.Set(RequiredScalar(map, "icao"))), "real" => new(WeatherMode.RealCurrent, OptionalValue<string>.Unset, OptionalValue<string>.Unset), _ => throw Error("OL_E_SESSION_PROFILE_INVALID", "Unsupported weather mode: " + mode) };
    }
    private static SessionPresentationSpec ReadPresentation(IReadOnlyDictionary<string, YamlNode> map, string root)
    {
        if (!map.TryGetValue("splash", out var splash)) throw Error("OL_E_SESSION_PROFILE_INVALID", "Presentation requires splash.");
        var source = Mapping(splash, "presentation.splash", "mode", "language", "assets");
        var mode = RequiredScalar(source, "mode").ToLowerInvariant() switch { "managed" => SplashMode.Managed, "unset" or "native" => SplashMode.Unset, _ => throw Error("OL_E_SESSION_PROFILE_INVALID", "Unsupported splash mode.") };
        var assets = OptionalScalar(source, "assets");
        if (assets is not null) { var resolved = Confined(root, assets); if (!Directory.Exists(resolved)) throw Error("OL_E_SESSION_PROFILE_ASSET_MISSING", "Profile splash assets are missing."); assets = resolved; }
        return new(mode, OptionalValue<string>.Set(OptionalScalar(source, "language") ?? "ENG"), assets is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(assets));
    }
    private static InternetTexturesSpec ReadInternet(IReadOnlyDictionary<string, YamlNode> map, string root)
    {
        var mode = RequiredScalar(map, "mode").ToLowerInvariant() switch { "native" => InternetTexturesMode.Native, "disabled" => InternetTexturesMode.Disabled, "override" => InternetTexturesMode.Override, _ => throw Error("OL_E_SESSION_PROFILE_INVALID", "Unsupported internet-textures mode.") };
        var path = OptionalScalar(map, "profile");
        if (mode == InternetTexturesMode.Override) { if (path is null) throw Error("OL_E_SESSION_PROFILE_INVALID", "Override internet textures requires profile."); path = Confined(root, path); if (!File.Exists(path)) throw Error("OL_E_SESSION_PROFILE_ASSET_MISSING", "Profile internet-textures asset is missing."); }
        return new(mode, path is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(path));
    }
    private static LaunchBehaviorSpec ReadBehavior(IReadOnlyDictionary<string, YamlNode> map)
    {
        int? startup = map.TryGetValue("startup-timeout", out var startupNode) ? int.Parse(Scalar(startupNode, "behavior.startup-timeout"), CultureInfo.InvariantCulture) : null;
        int? shutdown = map.TryGetValue("shutdown-timeout", out var shutdownNode) ? int.Parse(Scalar(shutdownNode, "behavior.shutdown-timeout"), CultureInfo.InvariantCulture) : null;
        if (startup is <= 0 || shutdown is <= 0) throw Error("OL_E_SESSION_PROFILE_INVALID", "Profile timeouts must be positive seconds.");
        return new LaunchBehaviorSpec(StartupTimeoutSeconds: startup ?? 180, ShutdownTimeoutSeconds: shutdown ?? 30);
    }
    // Each entry names exactly one source: package (a directory inside this
    // profile package), collection (.omsilaunch\hof\<name>), directory (an
    // installation-relative directory), vehicle or file. Target and source
    // existence are checked when the session is planned, not here.
    private static IReadOnlyList<HofOperationSpec> ReadHofs(YamlNode node, string packageRoot, string installationRoot)
    {
        if (node is not YamlSequenceNode sequence || sequence.Children.Count == 0) throw Error("OL_E_SESSION_PROFILE_INVALID", "hofs must be a non-empty sequence.");
        var output = new List<HofOperationSpec>();
        foreach (var child in sequence.Children)
        {
            var map = Mapping(child, "hofs", "package", "collection", "directory", "vehicle", "file", "targets", "files", "mode");
            var sources = new[] { "package", "collection", "directory", "vehicle", "file" }.Where(map.ContainsKey).ToArray();
            if (sources.Length != 1) throw Error("OL_E_SESSION_PROFILE_INVALID", "Each hofs entry requires exactly one of package, collection, directory, vehicle or file.");
            var value = RequiredScalar(map, sources[0]);
            HofOperationSpec operation;
            var targets = Sequence(map, "targets", required: true);
            var files = Sequence(map, "files", required: false);
            var mode = (OptionalScalar(map, "mode") ?? "add").ToLowerInvariant() switch { "add" => HofApplyMode.Add, "mirror" => HofApplyMode.Mirror, var other => throw Error("OL_E_SESSION_PROFILE_INVALID", "Unsupported hofs mode: " + other) };
            if (sources[0] == "package")
            {
                var directory = Confined(packageRoot, value);
                if (!Directory.Exists(directory)) throw Error("OL_E_SESSION_PROFILE_ASSET_MISSING", "Profile HOF directory is missing: " + value);
                if (!InstallationPaths.TryGetContainedRelativePath(installationRoot, directory, out var relative)) throw Error("OL_E_SESSION_PROFILE_PATH_ESCAPE", "Profile HOF directory is outside the installation.");
                operation = new(HofSourceKind.Directory, relative, targets, mode, files.Count == 0 ? null : files);
            }
            else
            {
                var kind = sources[0] switch { "collection" => HofSourceKind.Collection, "directory" => HofSourceKind.Directory, "vehicle" => HofSourceKind.Vehicle, _ => HofSourceKind.File };
                operation = new(kind, Canonical(value), targets, mode, files.Count == 0 ? null : files);
            }
            output.Add(operation);
        }
        return output;
    }
    private static IReadOnlyList<string> Sequence(IReadOnlyDictionary<string, YamlNode> map, string key, bool required)
    {
        if (!map.TryGetValue(key, out var node)) return required ? throw Error("OL_E_SESSION_PROFILE_INVALID", "Missing required property: " + key) : Array.Empty<string>();
        if (node is not YamlSequenceNode sequence || sequence.Children.Count == 0) throw Error("OL_E_SESSION_PROFILE_INVALID", key + " must be a non-empty sequence.");
        return sequence.Children.Select(x => Canonical(Scalar(x, key))).ToArray();
    }
    private static IReadOnlyList<string> ReadCompatibility(IReadOnlyDictionary<string, YamlNode> root)
    { if (!root.TryGetValue("compatibility", out var node)) return Array.Empty<string>(); var map = Mapping(node, "compatibility", "maps"); if (!map.TryGetValue("maps", out var values) || values is not YamlSequenceNode sequence) return Array.Empty<string>(); return sequence.Children.Select(x => Canonical(Scalar(x, "compatibility.maps"))).ToArray(); }
    private static IReadOnlyDictionary<string, YamlNode> Mapping(YamlNode node, string context, params string[] allowed)
    { if (node is not YamlMappingNode mapping) throw Error("OL_E_SESSION_PROFILE_INVALID", context + " must be a mapping."); var output = new Dictionary<string, YamlNode>(StringComparer.Ordinal); foreach (var pair in mapping.Children) { var key = ScalarKey(pair.Key); if (allowed.Length != 0 && !allowed.Contains(key, StringComparer.Ordinal)) throw Error("OL_E_SESSION_PROFILE_INVALID", "Unknown property in " + context + ": " + key); output.Add(key, pair.Value); } return output; }
    private static string RequiredScalar(IReadOnlyDictionary<string, YamlNode> map, string key) => map.TryGetValue(key, out var value) ? Scalar(value, key) : throw Error("OL_E_SESSION_PROFILE_INVALID", "Missing required property: " + key);
    private static string? OptionalScalar(IReadOnlyDictionary<string, YamlNode> map, string key) => map.TryGetValue(key, out var value) ? Scalar(value, key) : null;
    private static string Scalar(YamlNode node, string field) => node is YamlScalarNode scalar && scalar.Value is not null ? scalar.Value : throw Error("OL_E_SESSION_PROFILE_INVALID", field + " must be a scalar.");
    private static string ScalarKey(YamlNode node) => Scalar(node, "mapping key");
    private static void RequireMode(IReadOnlyDictionary<string, YamlNode> map, string field) { if (!string.Equals(RequiredScalar(map, "mode"), "explicit", StringComparison.OrdinalIgnoreCase)) throw Error("OL_E_SESSION_PROFILE_INVALID", field + " must use explicit mode."); }
    private static string Confined(string root, string relative)
    {
        // Lexical rules first (no rooted paths, no ".." segment), then the shared
        // containment definition in InstallationPaths.
        if (Path.IsPathRooted(relative) || relative.StartsWith("\\", StringComparison.Ordinal) || InstallationPaths.Segments(relative).Any(x => x == "..")) throw Error("OL_E_SESSION_PROFILE_PATH_ESCAPE", "Profile paths must remain inside their package.");
        if (!InstallationPaths.TryGetContainedRelativePath(root, relative, out var contained)) throw Error("OL_E_SESSION_PROFILE_PATH_ESCAPE", "Profile paths must remain inside their package.");
        var full = Path.Combine(InstallationPaths.NormalizeRoot(root), contained);
        // Lexical confinement is not enough: a junction or symbolic link inside
        // the package would resolve outside it at open time.
        if (PathConfinement.ContainsReparsePoint(root, full)) throw Error("OL_E_SESSION_PROFILE_PATH_ESCAPE", "Profile paths must not traverse junctions or symbolic links.");
        return full;
    }
    private static string Canonical(string value) => value.Replace('/', '\\');
    private static void RejectAnchors(YamlNode node) { if (!node.Anchor.IsEmpty) throw Error("OL_E_SESSION_PROFILE_INVALID", "YAML anchors are not supported."); if (node is YamlMappingNode map) foreach (var pair in map.Children) { RejectAnchors(pair.Key); RejectAnchors(pair.Value); } if (node is YamlSequenceNode sequence) foreach (var child in sequence.Children) RejectAnchors(child); }
    private static SessionProfileException Error(string code, string message) => new(code, message);
}

public sealed class SessionProfileException : IOException { public SessionProfileException(string code, string message) : base(code + ": " + message) { Code = code; } public string Code { get; } }

// Reparse-point inspection for paths that were already confined lexically.
internal static class PathConfinement
{
    // True when any component strictly below root, up to and including the
    // final path, carries the ReparsePoint attribute. Missing components are
    // not reparse points; callers decide separately whether they must exist.
    public static bool ContainsReparsePoint(string root, string fullPath)
    {
        var baseFull = InstallationPaths.NormalizeRoot(root);
        var current = InstallationPaths.NormalizeRoot(fullPath);
        while (current.Length > baseFull.Length && current.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                if (File.Exists(current) || Directory.Exists(current))
                {
                    if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint)) return true;
                }
            }
            catch (IOException) { return true; }
            catch (UnauthorizedAccessException) { return true; }
            var parent = Path.GetDirectoryName(current);
            if (parent is null) break;
            current = parent;
        }
        return false;
    }
}
internal sealed record ParsedProfile(string RootPath, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, ProfileNew New, IReadOnlyList<ProfilePreset> Presets);
public sealed record SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset);
public sealed record ProfileNew(string? Map = null, int? EntrypointIndex = null, string? EntrypointIdentity = null, SemanticDate? Date = null, SemanticTime? Time = null, int? Year = null, WeatherSpec? Weather = null);
public sealed record ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec? Presentation, InternetTexturesSpec? InternetTextures, LaunchBehaviorSpec? Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec? Modules = null);
