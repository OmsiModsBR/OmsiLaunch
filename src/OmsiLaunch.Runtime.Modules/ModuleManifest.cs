using System.Text.Json;
using System.Text.RegularExpressions;
using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// How a module joins a session: requested (LaunchSpec, command line, session
// profile or the settings file), content (its binding names the session's
// map) or default (every session unless disabled; bridge protocol 2).
public enum ModuleActivation : byte { Requested, Content, Default }
public enum ModuleFailurePolicy : byte { FailSession, DisableModule, Degrade }
// Managed and native components run inside OMSI. An auxiliary program is
// started from inside OMSI by the module. A host program (the module's
// session program, bridge protocol 2) is started by OmsiLaunch before OMSI
// and ends with the session.
public enum ModuleComponentKind : byte { Managed, Native, Auxiliary, Host }
public enum ModuleSettingType : byte { Boolean, Integer, Choice, Text }

public sealed record ModuleFileReference(string Path, string Sha256);
public sealed record ModuleComponent(ModuleComponentKind Kind, string Path, string Sha256, string? Entry);
public sealed record ModuleContentBinding(string Map, IReadOnlyList<ModuleFileReference> Files);
public sealed record ModuleOperationArgument(string Name, bool Required);
public sealed record ModuleOperationDescriptor(string Id, PublicCapabilityKind Kind, IReadOnlyList<ModuleOperationArgument> Arguments, int TimeoutMilliseconds);
// PrepareMilliseconds: how long OmsiLaunch waits, before starting OMSI, for
// the host program to report its preparation done. HostStopMilliseconds: how
// long the host program has to finish after the session asks it to stop.
public sealed record ModuleTimeouts(int ArmMilliseconds, int GameplayReadyMilliseconds, int StopMilliseconds, int PrepareMilliseconds = RuntimeModuleProtocol.DefaultPrepareMilliseconds, int HostStopMilliseconds = RuntimeModuleProtocol.DefaultHostStopMilliseconds);
// A setting the module declares (bridge protocol 2). Default is normalized;
// "auto" means the module decides at run time (AllowsAuto). Flag is an
// optional command-line shortcut the module names: /<flag>:<value>.
public sealed record ModuleSettingDescriptor(string Key, ModuleSettingType Type, string Default, bool AllowsAuto, long Minimum, long Maximum, IReadOnlyList<string> Choices, int MaxLength, string? Flag, string? Description);

// A parsed omsilaunch.runtime-module/v1 manifest (module.json).
public sealed record ModuleManifest(
    string Id,
    string Version,
    string Name,
    int BridgeProtocolMin,
    int BridgeProtocolMax,
    ModuleActivation Activation,
    IReadOnlyList<string> RequiredCapabilities,
    IReadOnlyList<string> OptionalCapabilities,
    ModuleContentBinding? ContentBinding,
    IReadOnlyList<ModuleFileReference> Assets,
    IReadOnlyList<ModuleComponent> Components,
    ModuleFailurePolicy FailurePolicy,
    IReadOnlyList<ModuleOperationDescriptor> Operations,
    ModuleTimeouts Timeouts,
    bool Trace,
    IReadOnlyList<ModuleSettingDescriptor>? Settings = null,
    IReadOnlyList<string>? ConflictingPlugins = null)
{
    public ModuleComponent ManagedComponent => Components.Single(x => x.Kind == ModuleComponentKind.Managed);
    public ModuleComponent? HostComponent => Components.FirstOrDefault(x => x.Kind == ModuleComponentKind.Host);
    public IReadOnlyList<ModuleSettingDescriptor> EffectiveSettings => Settings ?? Array.Empty<ModuleSettingDescriptor>();
    // OMSI plugin DLLs (plugins\*.opl) that must not run next to this module,
    // for example an older plugin that installs the same hooks.
    public IReadOnlyList<string> EffectiveConflictingPlugins => ConflictingPlugins ?? Array.Empty<string>();
    // Every file the manifest pins, relative to the module directory.
    public IEnumerable<ModuleFileReference> PinnedFiles => Components.Select(x => new ModuleFileReference(x.Path, x.Sha256)).Concat(Assets);
    public string OperationName(string operation) => RuntimeModuleProtocol.OperationPrefix + Id + "." + operation;
}

public sealed class ModuleException : Exception
{
    public string Code { get; }
    public ModuleException(string code, string message) : base(code + ": " + message) => Code = code;
}

public static class RuntimeModuleProtocol
{
    public const string Schema = "omsilaunch.runtime-module/v1";
    // The bridge protocol this build implements, and the oldest one it still
    // serves. Protocol 2 adds settings, default activation and plugin conflicts.
    public const int BridgeProtocol = 2;
    public const int MinimumBridgeProtocol = 1;
    public static bool IsCompatible(int min, int max) => min <= BridgeProtocol && max >= MinimumBridgeProtocol;
    public const string ManifestFileName = "module.json";
    // Modules live below the plugins folder: plugins\OmsiLaunch.Modules\<id>\.
    public const string ModulesDirectory = @"plugins\OmsiLaunch.Modules";
    // Writable per-installation data of a module: .omsilaunch\module-data\<id>\.
    public const string ModuleDataDirectory = @".omsilaunch\module-data";
    // The user's module settings: one [<module-id>] section per module.
    public const string SettingsFile = @".omsilaunch\modules.ini";
    public const string OperationPrefix = "module.";
    // Capabilities the bridge itself provides to every module, next to the
    // native capabilities of the native target catalog.
    public static readonly IReadOnlySet<string> BridgeFeatures = new HashSet<string>(StringComparer.Ordinal) { "bridge.events", "bridge.background-threads", "bridge.auxiliary-processes", "bridge.hooks.iat", "bridge.hooks.d3d9" };
    // A bridge feature that also needs a native capability of the build.
    public static readonly IReadOnlyDictionary<string, string> BridgeFeatureNeeds = new Dictionary<string, string>(StringComparer.Ordinal) { ["bridge.hooks.d3d9"] = "d3d.texture" };
    public const int MaxTimeoutMilliseconds = 30_000;
    public const int DefaultPhaseTimeoutMilliseconds = 5_000;
    public const int DefaultOperationTimeoutMilliseconds = 2_000;
    public const int DefaultPrepareMilliseconds = 120_000;
    public const int MaxPrepareMilliseconds = 1_800_000;
    public const int DefaultHostStopMilliseconds = 10_000;
    public const int MaxHostStopMilliseconds = 60_000;
    private static readonly Regex IdPattern = new("^[a-z0-9][a-z0-9-]{1,47}$", RegexOptions.CultureInvariant);
    private static readonly Regex OperationPattern = new("^[a-z0-9][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex SettingKeyPattern = new("^[a-z][a-z0-9_]{0,31}$", RegexOptions.CultureInvariant);
    private static readonly Regex FlagPattern = new("^[a-z][a-z0-9-]{2,31}$", RegexOptions.CultureInvariant);
    public static bool IsModuleId(string value) => IdPattern.IsMatch(value);
    public static bool IsOperationId(string value) => OperationPattern.IsMatch(value) && !value.EndsWith('.') && !value.Contains("..", StringComparison.Ordinal);
    // "enabled" is the settings file's own key for switching a module on or off.
    public static bool IsSettingKey(string value) => SettingKeyPattern.IsMatch(value) && value != "enabled";
    public static bool IsFlagName(string value) => FlagPattern.IsMatch(value) && !value.EndsWith('-');
    public static string DataDirectory(string installationRoot, string moduleId) => Path.Combine(installationRoot, ModuleDataDirectory, moduleId);
    // The pipe a module's host program serves for the module's components in
    // OMSI (current user only; the host program checks the OMSI process id).
    public static string SessionChannelName(Guid sessionId, string moduleId) => @"\\.\pipe\OmsiLaunch.Module." + sessionId.ToString("N") + "." + moduleId;

    // "module.<id>.<operation>" -> (id, operation).
    public static bool TrySplitOperation(string operation, out string moduleId, out string moduleOperation)
    {
        moduleId = moduleOperation = "";
        if (!operation.StartsWith(OperationPrefix, StringComparison.Ordinal)) return false;
        var rest = operation[OperationPrefix.Length..];
        var dot = rest.IndexOf('.');
        if (dot <= 0 || dot == rest.Length - 1) return false;
        moduleId = rest[..dot]; moduleOperation = rest[(dot + 1)..];
        return IsModuleId(moduleId) && IsOperationId(moduleOperation);
    }
}

public static class ModuleManifestParser
{
    private static readonly HashSet<string> RootKeys = new(StringComparer.Ordinal) { "schema", "id", "version", "name", "bridge_protocol", "activation", "required_capabilities", "optional_capabilities", "content_binding", "assets", "components", "failure_policy", "operations", "timeouts", "diagnostics", "settings", "conflicts" };
    private static readonly HashSet<string> SettingKeys = new(StringComparer.Ordinal) { "key", "type", "default", "auto", "min", "max", "choices", "max_length", "flag", "description" };
    private static readonly Regex VersionPattern = new(@"^\d{1,5}\.\d{1,5}\.\d{1,5}(-[0-9A-Za-z.-]{1,32})?$", RegexOptions.CultureInvariant);
    private static readonly Regex CapabilityPattern = new("^[a-z0-9][a-z0-9.-]{0,63}$", RegexOptions.CultureInvariant);
    private static readonly Regex ChoicePattern = new("^[a-z0-9][a-z0-9_.-]{0,31}$", RegexOptions.CultureInvariant);
    private static readonly Regex PluginLibraryPattern = new(@"^[A-Za-z0-9][A-Za-z0-9_.-]{0,63}\.dll$", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

    public static ModuleManifest Parse(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) bytes = bytes[3..];
        if (bytes.Length > 256 * 1024) throw Invalid("larger than 256 KiB");
        JsonDocument document;
        try { document = JsonDocument.Parse(bytes.ToArray(), new JsonDocumentOptions { MaxDepth = 16 }); }
        catch (JsonException exception) { throw Invalid("not JSON (" + exception.Message + ")"); }
        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("the root is not an object");
            foreach (var property in root.EnumerateObject()) if (!RootKeys.Contains(property.Name)) throw Invalid("unknown key '" + property.Name + "'");
            if (Text(root, "schema") != RuntimeModuleProtocol.Schema) throw Invalid("schema must be " + RuntimeModuleProtocol.Schema);
            var id = Text(root, "id");
            if (!RuntimeModuleProtocol.IsModuleId(id)) throw Invalid("id must match [a-z0-9][a-z0-9-]{1,47}");
            var version = Text(root, "version");
            if (!VersionPattern.IsMatch(version)) throw Invalid("version must be major.minor.patch");
            var name = OptionalText(root, "name") ?? id;
            if (name.Length > 128) throw Invalid("name is longer than 128 characters");
            var protocol = Object(root, "bridge_protocol");
            var min = Integer(protocol, "min", 1, 1000); var max = Integer(protocol, "max", 1, 1000);
            if (min > max) throw Invalid("bridge_protocol.min is greater than max");
            var activation = OptionalText(root, "activation") switch
            {
                null or "requested" => ModuleActivation.Requested,
                "content" => ModuleActivation.Content,
                "default" => ModuleActivation.Default,
                var other => throw Invalid("activation '" + other + "' is not requested, content or default")
            };
            var required = Capabilities(root, "required_capabilities");
            var optional = Capabilities(root, "optional_capabilities");
            if (required.Intersect(optional, StringComparer.Ordinal).Any()) throw Invalid("a capability is both required and optional");
            ModuleContentBinding? binding = null;
            if (root.TryGetProperty("content_binding", out var bindingElement))
            {
                if (bindingElement.ValueKind != JsonValueKind.Object) throw Invalid("content_binding is not an object");
                foreach (var property in bindingElement.EnumerateObject()) if (property.Name is not "map" and not "files") throw Invalid("unknown key 'content_binding." + property.Name + "'");
                var map = Text(bindingElement, "map").Replace('/', '\\');
                if (!map.StartsWith(@"maps\", StringComparison.OrdinalIgnoreCase) || !map.EndsWith(@"\global.cfg", StringComparison.OrdinalIgnoreCase) || !IsRelative(map)) throw Invalid(@"content_binding.map must be maps\<folder>\global.cfg");
                binding = new(map, Files(bindingElement, "files"));
            }
            else if (activation == ModuleActivation.Content) throw Invalid("activation content requires content_binding");
            // A default module joins every session; a binding would contradict that.
            if (activation == ModuleActivation.Default && binding is not null) throw Invalid("activation default cannot have a content_binding (use activation content)");
            var assets = Files(root, "assets");
            var components = Components(root);
            var policy = Text(root, "failure_policy") switch
            {
                "fail-session" => ModuleFailurePolicy.FailSession,
                "disable-module" => ModuleFailurePolicy.DisableModule,
                "degrade" => ModuleFailurePolicy.Degrade,
                var other => throw Invalid("failure_policy '" + other + "' is not fail-session, disable-module or degrade")
            };
            var operations = Operations(root);
            var timeouts = new ModuleTimeouts(RuntimeModuleProtocol.DefaultPhaseTimeoutMilliseconds, RuntimeModuleProtocol.DefaultPhaseTimeoutMilliseconds, RuntimeModuleProtocol.DefaultPhaseTimeoutMilliseconds);
            if (root.TryGetProperty("timeouts", out var timeoutElement))
            {
                if (timeoutElement.ValueKind != JsonValueKind.Object) throw Invalid("timeouts is not an object");
                foreach (var property in timeoutElement.EnumerateObject()) if (property.Name is not "arm_ms" and not "gameplay_ready_ms" and not "stop_ms" and not "prepare_ms" and not "host_stop_ms") throw Invalid("unknown key 'timeouts." + property.Name + "'");
                timeouts = new(OptionalInteger(timeoutElement, "arm_ms", timeouts.ArmMilliseconds), OptionalInteger(timeoutElement, "gameplay_ready_ms", timeouts.GameplayReadyMilliseconds), OptionalInteger(timeoutElement, "stop_ms", timeouts.StopMilliseconds),
                    timeoutElement.TryGetProperty("prepare_ms", out _) ? Integer(timeoutElement, "prepare_ms", 1, RuntimeModuleProtocol.MaxPrepareMilliseconds) : RuntimeModuleProtocol.DefaultPrepareMilliseconds,
                    timeoutElement.TryGetProperty("host_stop_ms", out _) ? Integer(timeoutElement, "host_stop_ms", 1, RuntimeModuleProtocol.MaxHostStopMilliseconds) : RuntimeModuleProtocol.DefaultHostStopMilliseconds);
            }
            var trace = false;
            if (root.TryGetProperty("diagnostics", out var diagnostics))
            {
                if (diagnostics.ValueKind != JsonValueKind.Object) throw Invalid("diagnostics is not an object");
                foreach (var property in diagnostics.EnumerateObject()) if (property.Name != "trace") throw Invalid("unknown key 'diagnostics." + property.Name + "'");
                if (diagnostics.TryGetProperty("trace", out var traceElement)) trace = traceElement.ValueKind == JsonValueKind.True || (traceElement.ValueKind == JsonValueKind.False ? false : throw Invalid("diagnostics.trace is not a boolean"));
            }
            var settings = Settings(root);
            var conflicts = Conflicts(root);
            // Protocol 2 features: a bridge that speaks only protocol 1 would
            // reject them as unknown keys, so the module must say it needs 2.
            var protocol2 = new List<string>();
            if (activation == ModuleActivation.Default) protocol2.Add("activation default");
            if (settings.Count != 0) protocol2.Add("settings");
            if (conflicts.Count != 0) protocol2.Add("conflicts");
            if (components.Any(x => x.Kind == ModuleComponentKind.Host)) protocol2.Add("a host component");
            if (root.TryGetProperty("timeouts", out var hostTimeouts) && (hostTimeouts.TryGetProperty("prepare_ms", out _) || hostTimeouts.TryGetProperty("host_stop_ms", out _))) protocol2.Add("host timeouts");
            if (protocol2.Count != 0 && min < 2) throw Invalid(string.Join(", ", protocol2) + " need bridge_protocol.min 2");
            var pinned = components.Select(x => x.Path).Concat(assets.Select(x => x.Path)).ToArray();
            if (pinned.Distinct(StringComparer.OrdinalIgnoreCase).Count() != pinned.Length) throw Invalid("a file is pinned twice");
            if (pinned.Any(x => x.Equals(RuntimeModuleProtocol.ManifestFileName, StringComparison.OrdinalIgnoreCase))) throw Invalid("module.json cannot pin itself");
            return new(id, version, name, min, max, activation, required, optional, binding, assets, components, policy, operations, timeouts, trace, settings, conflicts);
        }
    }

    private static IReadOnlyList<ModuleComponent> Components(JsonElement root)
    {
        var list = new List<ModuleComponent>();
        foreach (var element in Array(root, "components", required: true))
        {
            if (element.ValueKind != JsonValueKind.Object) throw Invalid("a component is not an object");
            foreach (var property in element.EnumerateObject()) if (property.Name is not "kind" and not "path" and not "sha256" and not "entry") throw Invalid("unknown key 'components." + property.Name + "'");
            var kind = Text(element, "kind") switch
            {
                "managed" => ModuleComponentKind.Managed,
                "native" => ModuleComponentKind.Native,
                "auxiliary" => ModuleComponentKind.Auxiliary,
                "host" => ModuleComponentKind.Host,
                var other => throw Invalid("component kind '" + other + "' is not managed, native, auxiliary or host")
            };
            var file = File(element);
            var expectedExtension = kind is ModuleComponentKind.Auxiliary or ModuleComponentKind.Host ? ".exe" : ".dll";
            if (!file.Path.EndsWith(expectedExtension, StringComparison.OrdinalIgnoreCase)) throw Invalid(kind.ToString().ToLowerInvariant() + " component must be a " + expectedExtension + " file");
            var entry = OptionalText(element, "entry");
            if (kind == ModuleComponentKind.Managed && (entry is null || !Regex.IsMatch(entry, @"^[A-Za-z_][A-Za-z0-9_]*(\.[A-Za-z_][A-Za-z0-9_]*)+$"))) throw Invalid("a managed component needs entry: the full name of its IRuntimeModule type");
            if (kind != ModuleComponentKind.Managed && entry is not null) throw Invalid("entry is only valid for a managed component");
            list.Add(new(kind, file.Path, file.Sha256, entry));
        }
        if (list.Count(x => x.Kind == ModuleComponentKind.Managed) != 1) throw Invalid("exactly one managed component is required");
        if (list.Count(x => x.Kind == ModuleComponentKind.Native) > 1) throw Invalid("at most one native component is allowed");
        if (list.Count(x => x.Kind == ModuleComponentKind.Host) > 1) throw Invalid("at most one host component is allowed");
        return list;
    }

    private static IReadOnlyList<ModuleOperationDescriptor> Operations(JsonElement root)
    {
        var list = new List<ModuleOperationDescriptor>();
        foreach (var element in Array(root, "operations", required: false))
        {
            if (element.ValueKind != JsonValueKind.Object) throw Invalid("an operation is not an object");
            foreach (var property in element.EnumerateObject()) if (property.Name is not "id" and not "kind" and not "arguments" and not "timeout_ms") throw Invalid("unknown key 'operations." + property.Name + "'");
            var id = Text(element, "id");
            if (!RuntimeModuleProtocol.IsOperationId(id)) throw Invalid("operation id '" + id + "' is not valid");
            var kind = Text(element, "kind") switch
            {
                "read" => PublicCapabilityKind.Read,
                "write" => PublicCapabilityKind.Write,
                "action" => PublicCapabilityKind.Action,
                var other => throw Invalid("operation kind '" + other + "' is not read, write or action")
            };
            var arguments = new List<ModuleOperationArgument>();
            foreach (var argument in Array(element, "arguments", required: false))
            {
                if (argument.ValueKind != JsonValueKind.Object) throw Invalid("an operation argument is not an object");
                foreach (var property in argument.EnumerateObject()) if (property.Name is not "name" and not "required") throw Invalid("unknown key 'operations.arguments." + property.Name + "'");
                var name = Text(argument, "name");
                if (!Regex.IsMatch(name, "^[a-z][a-z0-9_]{0,31}$")) throw Invalid("argument name '" + name + "' is not valid");
                var isRequired = argument.TryGetProperty("required", out var requiredElement) && (requiredElement.ValueKind == JsonValueKind.True || (requiredElement.ValueKind == JsonValueKind.False ? false : throw Invalid("argument required is not a boolean")));
                arguments.Add(new(name, isRequired));
            }
            if (arguments.Select(x => x.Name).Distinct(StringComparer.Ordinal).Count() != arguments.Count) throw Invalid("operation '" + id + "' repeats an argument");
            list.Add(new(id, kind, arguments, OptionalInteger(element, "timeout_ms", RuntimeModuleProtocol.DefaultOperationTimeoutMilliseconds)));
        }
        if (list.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count() != list.Count) throw Invalid("an operation id is repeated");
        return list;
    }

    private static IReadOnlyList<ModuleSettingDescriptor> Settings(JsonElement root)
    {
        var list = new List<ModuleSettingDescriptor>();
        foreach (var element in Array(root, "settings", required: false))
        {
            if (element.ValueKind != JsonValueKind.Object) throw Invalid("a setting is not an object");
            foreach (var property in element.EnumerateObject()) if (!SettingKeys.Contains(property.Name)) throw Invalid("unknown key 'settings." + property.Name + "'");
            var key = Text(element, "key");
            if (!RuntimeModuleProtocol.IsSettingKey(key)) throw Invalid("setting key '" + key + "' must match [a-z][a-z0-9_]{0,31} and must not be 'enabled'");
            var type = Text(element, "type") switch
            {
                "boolean" => ModuleSettingType.Boolean,
                "integer" => ModuleSettingType.Integer,
                "choice" => ModuleSettingType.Choice,
                "text" => ModuleSettingType.Text,
                var other => throw Invalid("setting type '" + other + "' is not boolean, integer, choice or text")
            };
            var allowsAuto = element.TryGetProperty("auto", out var autoElement) && (autoElement.ValueKind == JsonValueKind.True || (autoElement.ValueKind == JsonValueKind.False ? false : throw Invalid("setting '" + key + "': auto is not a boolean")));
            long minimum = long.MinValue, maximum = long.MaxValue;
            var hasMin = element.TryGetProperty("min", out var minElement); var hasMax = element.TryGetProperty("max", out var maxElement);
            if ((hasMin || hasMax) && type != ModuleSettingType.Integer) throw Invalid("setting '" + key + "': min and max are only valid for an integer");
            if (hasMin) minimum = minElement.ValueKind == JsonValueKind.Number && minElement.TryGetInt64(out var parsedMin) ? parsedMin : throw Invalid("setting '" + key + "': min is not an integer");
            if (hasMax) maximum = maxElement.ValueKind == JsonValueKind.Number && maxElement.TryGetInt64(out var parsedMax) ? parsedMax : throw Invalid("setting '" + key + "': max is not an integer");
            if (minimum > maximum) throw Invalid("setting '" + key + "': min is greater than max");
            IReadOnlyList<string> choices = System.Array.Empty<string>();
            if (element.TryGetProperty("choices", out _))
            {
                if (type != ModuleSettingType.Choice) throw Invalid("setting '" + key + "': choices are only valid for a choice");
                choices = Array(element, "choices", required: true).Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()! : throw Invalid("setting '" + key + "': choices must be strings")).ToArray();
                if (choices.Count == 0 || choices.Any(x => !ChoicePattern.IsMatch(x) || x == ModuleSettings.Auto)) throw Invalid("setting '" + key + "': each choice must match [a-z0-9][a-z0-9_.-]{0,31} and must not be 'auto'");
                if (choices.Distinct(StringComparer.Ordinal).Count() != choices.Count) throw Invalid("setting '" + key + "' repeats a choice");
            }
            else if (type == ModuleSettingType.Choice) throw Invalid("setting '" + key + "' of type choice needs choices");
            var maxLength = 256;
            if (element.TryGetProperty("max_length", out _))
            {
                if (type != ModuleSettingType.Text) throw Invalid("setting '" + key + "': max_length is only valid for text");
                maxLength = Integer(element, "max_length", 1, 4096);
            }
            var flag = OptionalText(element, "flag");
            if (flag is not null && !RuntimeModuleProtocol.IsFlagName(flag)) throw Invalid("setting '" + key + "': flag '" + flag + "' must match [a-z][a-z0-9-]{2,31}");
            var description = OptionalText(element, "description");
            if (description is { Length: > 256 }) throw Invalid("setting '" + key + "': description is longer than 256 characters");
            if (!element.TryGetProperty("default", out var defaultElement)) throw Invalid("setting '" + key + "' needs a default");
            var rawDefault = defaultElement.ValueKind switch
            {
                JsonValueKind.String => defaultElement.GetString()!,
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                JsonValueKind.Number => defaultElement.GetRawText(),
                _ => throw Invalid("setting '" + key + "': default must be a string, a number or a boolean")
            };
            var descriptor = new ModuleSettingDescriptor(key, type, "", allowsAuto, minimum, maximum, choices, maxLength, flag, description);
            if (!ModuleSettings.TryNormalize(descriptor, rawDefault, out var normalized, out var reason)) throw Invalid("setting '" + key + "': the default " + reason);
            list.Add(descriptor with { Default = normalized });
        }
        if (list.Select(x => x.Key).Distinct(StringComparer.Ordinal).Count() != list.Count) throw Invalid("a setting key is repeated");
        var flags = list.Where(x => x.Flag is not null).Select(x => x.Flag!).ToArray();
        if (flags.Distinct(StringComparer.Ordinal).Count() != flags.Length) throw Invalid("a setting flag is repeated");
        return list;
    }

    private static IReadOnlyList<string> Conflicts(JsonElement root)
    {
        if (!root.TryGetProperty("conflicts", out var element)) return System.Array.Empty<string>();
        if (element.ValueKind != JsonValueKind.Object) throw Invalid("conflicts is not an object");
        foreach (var property in element.EnumerateObject()) if (property.Name != "plugins") throw Invalid("unknown key 'conflicts." + property.Name + "'");
        var plugins = Array(element, "plugins", required: false).Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()! : throw Invalid("conflicts.plugins must contain strings")).ToArray();
        foreach (var plugin in plugins) if (!PluginLibraryPattern.IsMatch(plugin)) throw Invalid("conflicts.plugins entry '" + plugin + "' is not a plugin DLL file name");
        if (plugins.Distinct(StringComparer.OrdinalIgnoreCase).Count() != plugins.Length) throw Invalid("conflicts.plugins repeats a name");
        return plugins;
    }

    private static IReadOnlyList<ModuleFileReference> Files(JsonElement parent, string key)
    {
        var list = new List<ModuleFileReference>();
        foreach (var element in Array(parent, key, required: false))
        {
            if (element.ValueKind != JsonValueKind.Object) throw Invalid(key + " entry is not an object");
            foreach (var property in element.EnumerateObject()) if (property.Name is not "path" and not "sha256") throw Invalid("unknown key '" + key + "." + property.Name + "'");
            list.Add(File(element));
        }
        return list;
    }

    private static ModuleFileReference File(JsonElement element)
    {
        var path = Text(element, "path").Replace('/', '\\');
        if (!IsRelative(path)) throw Invalid("path '" + path + "' must be relative without '..'");
        var sha256 = Text(element, "sha256");
        if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit)) throw Invalid("sha256 of '" + path + "' is not 64 hexadecimal digits");
        return new(path, sha256.ToUpperInvariant());
    }

    internal static bool IsRelative(string path) =>
        path.Length is > 0 and <= 240 && !Path.IsPathRooted(path) && !path.Contains(':') && path.IndexOfAny(Path.GetInvalidPathChars()) < 0 &&
        path.Split('\\').All(segment => segment.Length > 0 && segment != "." && segment != ".." && segment.Trim() == segment);

    private static IReadOnlyList<string> Capabilities(JsonElement root, string key)
    {
        var values = Array(root, key, required: false).Select(x => x.ValueKind == JsonValueKind.String ? x.GetString()! : throw Invalid(key + " must contain strings")).ToArray();
        foreach (var value in values) if (!CapabilityPattern.IsMatch(value)) throw Invalid("capability '" + value + "' is not valid");
        if (values.Distinct(StringComparer.Ordinal).Count() != values.Length) throw Invalid(key + " repeats a capability");
        return values;
    }

    private static IEnumerable<JsonElement> Array(JsonElement parent, string key, bool required)
    {
        if (!parent.TryGetProperty(key, out var element)) return required ? throw Invalid(key + " is required") : System.Array.Empty<JsonElement>();
        if (element.ValueKind != JsonValueKind.Array) throw Invalid(key + " is not an array");
        if (element.GetArrayLength() > 256) throw Invalid(key + " has more than 256 entries");
        return element.EnumerateArray().ToArray();
    }
    private static JsonElement Object(JsonElement parent, string key) =>
        parent.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.Object ? element : throw Invalid(key + " is required and must be an object");
    private static string Text(JsonElement parent, string key) =>
        parent.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()) ? element.GetString()! : throw Invalid(key + " is required and must be a non-empty string");
    private static string? OptionalText(JsonElement parent, string key) =>
        !parent.TryGetProperty(key, out var element) ? null : element.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(element.GetString()) ? element.GetString() : throw Invalid(key + " must be a non-empty string");
    private static int Integer(JsonElement parent, string key, int min, int max) =>
        parent.TryGetProperty(key, out var element) && element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value) && value >= min && value <= max ? value : throw Invalid(key + " must be an integer in " + min + ".." + max);
    private static int OptionalInteger(JsonElement parent, string key, int fallback) =>
        parent.TryGetProperty(key, out _) ? Integer(parent, key, 1, RuntimeModuleProtocol.MaxTimeoutMilliseconds) : fallback;
    private static ModuleException Invalid(string message) => new(PublicErrorCodes.MODULE_MANIFEST_INVALID, message);
}
