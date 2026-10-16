using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OmsiLaunch.Runtime.Modules;

public enum ModuleSettingSource : byte { Default, SettingsFile, Request }

// One effective setting of a planned module.
public sealed record ModuleSettingValue(string Key, string Value, ModuleSettingSource Source);

// A problem found while selecting modules or resolving their settings. Error
// is true for an explicit request (LaunchSpec, command line, session profile),
// which makes the plan not runnable; a settings-file entry is only ignored.
public sealed record ModuleSettingProblem(string ModuleId, string Message, bool Error);

// Module settings (bridge protocol 2). The effective value of a declared key
// is, in order: the request (LaunchSpec, command line or session profile),
// the settings file (.omsilaunch\modules.ini), the manifest default.
public static class ModuleSettings
{
    public const string Auto = "auto";

    // Validates one value and returns its normalized text.
    public static bool TryNormalize(ModuleSettingDescriptor setting, string raw, out string value, out string reason)
    {
        value = ""; reason = "";
        var text = raw.Trim();
        if (setting.AllowsAuto && text.Equals(Auto, StringComparison.OrdinalIgnoreCase)) { value = Auto; return true; }
        switch (setting.Type)
        {
            case ModuleSettingType.Boolean:
                switch (text.ToLowerInvariant())
                {
                    case "true" or "1" or "yes" or "on": value = "true"; return true;
                    case "false" or "0" or "no" or "off": value = "false"; return true;
                }
                reason = "'" + Shorten(text) + "' is not true or false";
                return false;
            case ModuleSettingType.Integer:
                if (!long.TryParse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var number)) { reason = "'" + Shorten(text) + "' is not an integer"; return false; }
                if (number < setting.Minimum || number > setting.Maximum) { reason = number.ToString(CultureInfo.InvariantCulture) + " is outside " + Bound(setting.Minimum) + ".." + Bound(setting.Maximum); return false; }
                value = number.ToString(CultureInfo.InvariantCulture);
                return true;
            case ModuleSettingType.Choice:
                var choice = setting.Choices.FirstOrDefault(x => x.Equals(text, StringComparison.OrdinalIgnoreCase));
                if (choice is null) { reason = "'" + Shorten(text) + "' is not one of " + string.Join(", ", setting.Choices); return false; }
                value = choice;
                return true;
            default:
                if (text.Length > setting.MaxLength) { reason = "is longer than " + setting.MaxLength.ToString(CultureInfo.InvariantCulture) + " characters"; return false; }
                if (text.Any(char.IsControl)) { reason = "contains control characters"; return false; }
                value = text;
                return true;
        }
    }

    // Every declared key with its effective value. request and file are keyed
    // by setting key (without the module id).
    public static IReadOnlyList<ModuleSettingValue> Resolve(ModuleManifest manifest, IReadOnlyDictionary<string, string> request, IReadOnlyDictionary<string, string>? file, List<ModuleSettingProblem> problems)
    {
        var values = new List<ModuleSettingValue>();
        foreach (var key in request.Keys.Where(key => manifest.EffectiveSettings.All(x => x.Key != key)))
            problems.Add(new(manifest.Id, "the module declares no setting '" + key + "'", true));
        if (file is not null)
            foreach (var key in file.Keys.Where(key => key != "enabled" && manifest.EffectiveSettings.All(x => x.Key != key)))
                problems.Add(new(manifest.Id, RuntimeModuleProtocol.SettingsFile + ": the module declares no setting '" + key + "'; ignored", false));
        foreach (var setting in manifest.EffectiveSettings)
        {
            if (request.TryGetValue(setting.Key, out var requested))
            {
                if (TryNormalize(setting, requested, out var value, out var reason)) { values.Add(new(setting.Key, value, ModuleSettingSource.Request)); continue; }
                problems.Add(new(manifest.Id, "setting '" + setting.Key + "': " + reason, true));
            }
            else if (file is not null && file.TryGetValue(setting.Key, out var stored))
            {
                if (TryNormalize(setting, stored, out var value, out var reason)) { values.Add(new(setting.Key, value, ModuleSettingSource.SettingsFile)); continue; }
                problems.Add(new(manifest.Id, RuntimeModuleProtocol.SettingsFile + ": setting '" + setting.Key + "': " + reason + "; the default is used", false));
            }
            values.Add(new(setting.Key, setting.Default, ModuleSettingSource.Default));
        }
        return values;
    }

    public static string SourceName(ModuleSettingSource source) => source switch
    {
        ModuleSettingSource.Request => "request",
        ModuleSettingSource.SettingsFile => "settings-file",
        _ => "default"
    };

    private static string Bound(long value) => value switch { long.MinValue => "", long.MaxValue => "", _ => value.ToString(CultureInfo.InvariantCulture) };
    private static string Shorten(string text) => text.Length <= 64 ? text : text[..64] + "...";
}

// The user's module settings: <installation>\.omsilaunch\modules.ini. One
// [<module-id>] section per module with key=value lines; "enabled" switches a
// module on or off for every session; ';' and '#' start comment lines.
// OmsiLaunch only reads this file. An entry it cannot use is reported and
// ignored; it never makes a plan unrunnable.
public sealed class ModuleSettingsFile
{
    public const long MaxBytes = 256 * 1024;
    private static readonly Regex SectionPattern = new(@"^\[\s*([^\]]*?)\s*\]$", RegexOptions.CultureInvariant);
    private readonly Dictionary<string, Dictionary<string, string>> sections;
    private ModuleSettingsFile(Dictionary<string, Dictionary<string, string>> sections, IReadOnlyList<string> problems) { this.sections = sections; Problems = problems; }

    public static ModuleSettingsFile Empty { get; } = new(new(StringComparer.Ordinal), Array.Empty<string>());
    public IReadOnlyList<string> Problems { get; }
    public IEnumerable<string> ModuleIds => sections.Keys;

    public IReadOnlyDictionary<string, string>? Section(string moduleId) => sections.TryGetValue(moduleId, out var section) ? section : null;

    // True or false when the section sets "enabled"; null when it does not.
    public bool? Enabled(string moduleId) =>
        sections.TryGetValue(moduleId, out var section) && section.TryGetValue("enabled", out var value) ? value == "true" : null;

    public static ModuleSettingsFile Read(string installationRoot)
    {
        var path = Path.Combine(installationRoot, RuntimeModuleProtocol.SettingsFile);
        try
        {
            if (!System.IO.File.Exists(path)) return Empty;
            if (new FileInfo(path).Length > MaxBytes) return new(new(StringComparer.Ordinal), new[] { RuntimeModuleProtocol.SettingsFile + " is larger than 256 KiB; ignored" });
            return Parse(System.IO.File.ReadAllText(path, Encoding.UTF8));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return new(new(StringComparer.Ordinal), new[] { RuntimeModuleProtocol.SettingsFile + " cannot be read (" + exception.Message + "); ignored" });
        }
    }

    public static ModuleSettingsFile Parse(string text)
    {
        var sections = new Dictionary<string, Dictionary<string, string>>(StringComparer.Ordinal);
        var problems = new List<string>();
        Dictionary<string, string>? current = null;
        var lineNumber = 0;
        foreach (var rawLine in text.Split('\n'))
        {
            lineNumber++;
            var line = rawLine.Trim();
            if (line.Length == 0 || line[0] is ';' or '#') continue;
            var where = RuntimeModuleProtocol.SettingsFile + " line " + lineNumber.ToString(CultureInfo.InvariantCulture);
            if (SectionPattern.Match(line) is { Success: true } section)
            {
                var id = section.Groups[1].Value.ToLowerInvariant();
                if (!RuntimeModuleProtocol.IsModuleId(id)) { problems.Add(where + ": [" + Shorten(id) + "] is not a module id; the section is ignored"); current = null; continue; }
                if (!sections.TryGetValue(id, out current)) sections[id] = current = new(StringComparer.Ordinal);
                continue;
            }
            var equals = line.IndexOf('=');
            if (equals <= 0) { problems.Add(where + ": not key=value; ignored"); continue; }
            if (current is null) { problems.Add(where + ": outside a [module-id] section; ignored"); continue; }
            var key = line[..equals].Trim().ToLowerInvariant();
            var value = line[(equals + 1)..].Trim();
            if (key != "enabled" && !RuntimeModuleProtocol.IsSettingKey(key)) { problems.Add(where + ": '" + Shorten(key) + "' is not a setting key; ignored"); continue; }
            if (key == "enabled")
            {
                var enabled = value.ToLowerInvariant() switch { "true" or "1" or "yes" or "on" => "true", "false" or "0" or "no" or "off" => "false", _ => null };
                if (enabled is null) { problems.Add(where + ": enabled must be true or false; ignored"); continue; }
                value = enabled;
            }
            if (current.ContainsKey(key)) problems.Add(where + ": '" + key + "' is repeated; the last value is used");
            current[key] = value;
        }
        return new(sections, problems);
    }

    private static string Shorten(string text) => text.Length <= 48 ? text : text[..48] + "...";
}

// OMSI plugins that are switched on: every plugins\*.opl and the DLLs its
// [dll] section names. A module lists, under conflicts.plugins, plugin DLLs
// that must not run next to it (for example an older plugin installing the
// same hooks); OmsiLaunch only reads these files and never changes them.
public static class PluginInventory
{
    // DLL file name (case-insensitive) -> the .opl that loads it, relative to
    // the installation root.
    public static IReadOnlyDictionary<string, string> ActiveLibraries(string installationRoot)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var plugins = Path.Combine(installationRoot, "plugins");
        if (!Directory.Exists(plugins)) return result;
        IEnumerable<string> files;
        try { files = Directory.EnumerateFiles(plugins, "*.opl", SearchOption.TopDirectoryOnly).Where(x => x.EndsWith(".opl", StringComparison.OrdinalIgnoreCase)).ToArray(); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return result; }
        foreach (var file in files)
        {
            string[] lines;
            try { if (new FileInfo(file).Length > 64 * 1024) continue; lines = System.IO.File.ReadAllLines(file); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { continue; }
            var inDll = false;
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.StartsWith('[')) { inDll = line.Equals("[dll]", StringComparison.OrdinalIgnoreCase); continue; }
                if (!inDll || line.Length == 0) continue;
                var library = Path.GetFileName(line);
                if (library.Length != 0) result.TryAdd(library, @"plugins\" + Path.GetFileName(file));
            }
        }
        return result;
    }
}

// Text a module shows to the user (a notice in the tray) is data: bounded,
// without control characters, never a command or a link to open.
public static class ModuleNotices
{
    public const string EventName = "notice";
    public const int MaxTitle = 63;
    public const int MaxText = 255;

    public static Dictionary<string, string> Sanitize(IReadOnlyDictionary<string, string> data)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        if (data.TryGetValue("title", out var title)) result["title"] = Clean(title, MaxTitle);
        result["text"] = Clean(data.TryGetValue("text", out var text) ? text : "", MaxText);
        return result;
    }

    private static string Clean(string value, int limit)
    {
        var builder = new StringBuilder(Math.Min(value.Length, limit));
        foreach (var character in value)
        {
            if (builder.Length >= limit) break;
            builder.Append(char.IsControl(character) ? ' ' : character);
        }
        return builder.ToString().Trim();
    }
}
