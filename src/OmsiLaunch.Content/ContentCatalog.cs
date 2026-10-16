using System.Security.Cryptography;
using System.Text;

namespace OmsiLaunch.Content;

public sealed record MapContent(string Identity, string RelativePath, string AbsolutePath, string? DisplayName, bool IsReadable);
public sealed record SituationContent(string Identity, string RelativePath, string AbsolutePath, string? MapIdentity, bool IsReadable);
public sealed record VehicleContent(string Identity, string RelativePath, string AbsolutePath, string? DisplayName, bool IsReadable);
public sealed record RepaintContent(string VehicleIdentity, string Identity, string RelativePath, string AbsolutePath, string? DisplayName, bool IsReadable);
public sealed record HofContent(string Identity, string RelativePath, string AbsolutePath, bool CompatibilityProven);
public sealed record FleetNumberSource(string VehicleIdentity, string SourceIdentity, bool Enumerated);
public sealed record RegistrationSource(string VehicleIdentity, string Mode, string? Value, bool Enumerated);
public sealed record AddonContent(string Identity, string RelativePath, string AbsolutePath, string DiscoveryConfidence);
// The raw entrypoint record, not its display label, is the persistent offline
// identity. OMSI's presented list is resolved separately during startup.
public sealed record EntrypointContent(string MapIdentity, string Identity, string DisplayName, string RecordFingerprint, bool IsReadable);
// The label OMSI's position dialog shows for a requested entrypoint. OMSI
// presents only the first record (file order) of each exact, case-sensitive
// label; Presented is false for a later record sharing its label.
public sealed record EntrypointLabel(bool Found, bool Presented, string? Label);

public interface IOmsiContentCatalog
{
    IReadOnlyList<MapContent> EnumerateMaps();
    IReadOnlyList<SituationContent> EnumerateSituations();
    IReadOnlyList<VehicleContent> EnumerateVehicles();
    IReadOnlyList<RepaintContent> EnumerateRepaints(string vehicleIdentity);
    IReadOnlyList<HofContent> EnumerateHofs();
    IReadOnlyList<FleetNumberSource> EnumerateFleetNumbers(string vehicleIdentity);
    IReadOnlyList<RegistrationSource> EnumerateRegistrations(string vehicleIdentity);
    IReadOnlyList<AddonContent> EnumerateAddons();
    IReadOnlyList<EntrypointContent> EnumerateEntrypoints(string mapIdentity);
    MapContent ResolveMap(string identity);
    SituationContent ResolveSituation(string identity);
    VehicleContent ResolveVehicle(string identity);
    HofContent ResolveHof(string identity);
}

public sealed class FileSystemContentCatalog : IOmsiContentCatalog
{
    private readonly string installationRoot;

    // Recursive discovery must not follow directory junctions or symbolic links:
    // a junction cycle inside maps\, Vehicles\ or situations\ would otherwise make
    // discovery (and therefore PlanSession) spin until the path length limit trips.
    // AttributesToSkip is set to ONLY ReparsePoint on purpose. The EnumerationOptions
    // default also skips Hidden|System, which SearchOption.AllDirectories never did,
    // and hidden or system-flagged content files must keep being discovered.
    // MatchType.Win32 preserves the pattern semantics SearchOption.AllDirectories used.
    private static readonly EnumerationOptions RecursiveNoReparse = new()
    {
        RecurseSubdirectories = true,
        AttributesToSkip = FileAttributes.ReparsePoint,
        IgnoreInaccessible = true,
        MatchCasing = MatchCasing.CaseInsensitive,
        MatchType = MatchType.Win32,
    };

    // OMSI content files (.bus, .cti, .hof, .osn, global.cfg) are Windows-1252 ANSI,
    // not UTF-8, so Encoding.Default (UTF-8 on .NET 6) mangles accented friendly
    // names into U+FFFD. Fall back to Latin1 if the code page provider is unavailable;
    // both map byte 0xE7 to 'ç' and the rest of the common range identically.
    private static readonly Encoding Ansi = CreateAnsiEncoding();

    private static Encoding CreateAnsiEncoding()
    {
        try
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            return Encoding.GetEncoding(1252);
        }
        catch
        {
            return Encoding.Latin1;
        }
    }

    public FileSystemContentCatalog(string installationRoot) => this.installationRoot = Path.GetFullPath(installationRoot);

    public IReadOnlyList<MapContent> EnumerateMaps() => EnumerateFiles("maps", "global.cfg")
        .Select(file => new MapContent(Canonical(file), Relative(file), file, DisplayName(Path.GetDirectoryName(file)!), CanRead(file)))
        .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<SituationContent> EnumerateSituations() => EnumerateByExtension("situations", ".osn")
        .Select(file => new SituationContent(Canonical(file), Relative(file), file, ReadSituationMap(file), CanRead(file)))
        .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<VehicleContent> EnumerateVehicles() => EnumerateByExtension("Vehicles", ".bus")
        .Select(file => new VehicleContent(Canonical(file), Relative(file), file, ReadBusDisplayName(file), CanRead(file)))
        .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<RepaintContent> EnumerateRepaints(string vehicleIdentity)
    {
        var vehicle = ResolveVehicle(vehicleIdentity);
        var directory = Path.GetDirectoryName(vehicle.AbsolutePath)!;
        return Directory.EnumerateFiles(directory, "*.cti", RecursiveNoReparse)
            .SelectMany(file => ReadCtiItems(file).Select(item => new RepaintContent(vehicle.Identity, Canonical(file) + "#item:" + item.Ordinal, Relative(file), file, item.Name, CanRead(file))))
            .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<HofContent> EnumerateHofs() => EnumerateByExtension("Vehicles", ".hof")
        .Select(file => new HofContent(Canonical(file), Relative(file), file, false))
        .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();

    public IReadOnlyList<FleetNumberSource> EnumerateFleetNumbers(string vehicleIdentity)
    {
        var vehicle = ResolveVehicle(vehicleIdentity); var source = ReadTaggedValue(vehicle.AbsolutePath, "number");
        return string.IsNullOrWhiteSpace(source) ? Array.Empty<FleetNumberSource>() : new[] { new FleetNumberSource(vehicle.Identity, VehicleRelative(vehicle, source), false) };
    }
    public IReadOnlyList<RegistrationSource> EnumerateRegistrations(string vehicleIdentity)
    {
        var vehicle = ResolveVehicle(vehicleIdentity); var lines = ReadLines(vehicle.AbsolutePath).ToArray(); var result = new List<RegistrationSource>();
        foreach (var mode in new[] { "registration_automatic", "registration_list", "registration_free" })
        {
            var index = Array.FindIndex(lines, x => x.Trim().Equals("[" + mode + "]", StringComparison.OrdinalIgnoreCase)); if (index < 0) continue;
            var value = mode == "registration_free" ? null : lines.Skip(index + 1).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && !x.TrimStart().StartsWith("[", StringComparison.Ordinal))?.Trim();
            result.Add(new(vehicle.Identity, mode, value, false));
        }
        return result;
    }

    public IReadOnlyList<AddonContent> EnumerateAddons()
    {
        var addons = Path.Combine(installationRoot, "Addons");
        if (!Directory.Exists(addons)) return Array.Empty<AddonContent>();
        return Directory.EnumerateDirectories(addons, "*", SearchOption.TopDirectoryOnly)
            .Select(path => new AddonContent(Canonical(path), Relative(path), path, "directory-only"))
            .OrderBy(x => x.Identity, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public IReadOnlyList<EntrypointContent> EnumerateEntrypoints(string mapIdentity) =>
        EntrypointsInFileOrder(mapIdentity).OrderBy(entry => entry.Identity, StringComparer.Ordinal).ToArray();

    // identity: an opaque identity from EnumerateEntrypoints or an exact label
    // (research report beta4-static entrypoint-last-player-vehicle.md section 1).
    public EntrypointLabel ResolveEntrypointLabel(string mapIdentity, string identity)
    {
        var entries = EntrypointsInFileOrder(mapIdentity);
        var record = entries.FirstOrDefault(entry => string.Equals(entry.Identity, identity, StringComparison.OrdinalIgnoreCase))
            ?? entries.FirstOrDefault(entry => string.Equals(entry.DisplayName, identity, StringComparison.Ordinal));
        if (record is null) return new(false, false, null);
        var first = entries.First(entry => string.Equals(entry.DisplayName, record.DisplayName, StringComparison.Ordinal));
        return new(true, ReferenceEquals(first, record), record.DisplayName);
    }

    private IReadOnlyList<EntrypointContent> EntrypointsInFileOrder(string mapIdentity)
    {
        var map = ResolveMap(mapIdentity);
        var lines = ReadLines(map.AbsolutePath, 16_384).ToArray();
        var marker = Array.FindIndex(lines, line => line.Trim().Equals("[entrypoints]", StringComparison.OrdinalIgnoreCase));
        if (marker < 0 || marker + 1 >= lines.Length || !int.TryParse(lines[marker + 1].Trim(), out var count) || count < 0 || count > 4096)
            return Array.Empty<EntrypointContent>();

        var entries = new List<EntrypointContent>(count);
        var cursor = marker + 2;
        for (var ordinal = 0; ordinal < count && cursor + 11 < lines.Length; ordinal++)
        {
            // OMSI 2.3.004 serializes each entrypoint as eleven scalar lines
            // followed by a label. Keep the complete normalized record opaque.
            var fields = lines.Skip(cursor).Take(12).Select(line => line.Trim()).ToArray();
            if (fields.Length != 12 || fields.Any(string.IsNullOrWhiteSpace)) break;
            cursor += 12;
            var canonicalRecord = string.Join("\n", fields);
            var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalRecord)));
            entries.Add(new EntrypointContent(map.Identity, map.Identity + "#entrypoint:" + fingerprint, fields[11], fingerprint, map.IsReadable));
        }
        return entries;
    }

    public MapContent ResolveMap(string identity) => EnumerateMaps().SingleOrDefault(x => SameIdentity(x.Identity, identity))
        ?? throw new FileNotFoundException("The requested map is not an installed OMSI map.", identity);

    public SituationContent ResolveSituation(string identity) => EnumerateSituations().SingleOrDefault(x => SameIdentity(x.Identity, identity))
        ?? throw new FileNotFoundException("The requested situation is not an installed OMSI situation.", identity);

    public VehicleContent ResolveVehicle(string identity) => EnumerateVehicles().SingleOrDefault(x => SameIdentity(x.Identity, identity))
        ?? throw new FileNotFoundException("The requested vehicle is not an installed OMSI vehicle.", identity);
    public HofContent ResolveHof(string identity) => EnumerateHofs().SingleOrDefault(x => SameIdentity(x.Identity, identity))
        ?? throw new FileNotFoundException("The requested HOF is not installed.", identity);

    private IEnumerable<string> EnumerateFiles(string root, string name)
    {
        var directory = Path.Combine(installationRoot, root);
        return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, name, RecursiveNoReparse) : Array.Empty<string>();
    }

    private IEnumerable<string> EnumerateByExtension(string root, string extension)
    {
        var directory = Path.Combine(installationRoot, root);
        return Directory.Exists(directory) ? Directory.EnumerateFiles(directory, "*" + extension, RecursiveNoReparse) : Array.Empty<string>();
    }

    private string Relative(string path) => Path.GetRelativePath(installationRoot, path).Replace('/', '\\');
    private string Canonical(string path) => Relative(path);
    private static bool SameIdentity(string left, string right) => string.Equals(left.Replace('/', '\\'), right.Replace('/', '\\'), StringComparison.OrdinalIgnoreCase);
    private static bool CanRead(string path) { try { using var _ = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite); return true; } catch { return false; } }
    private static string DisplayName(string path) => Path.GetFileName(path);

    private static string? ReadSituationMap(string path)
    {
        foreach (var line in ReadLines(path))
        {
            var value = line.Trim();
            if (value.StartsWith("maps\\", StringComparison.OrdinalIgnoreCase) && value.EndsWith("global.cfg", StringComparison.OrdinalIgnoreCase)) return value.Replace('/', '\\');
        }
        return null;
    }

    private static string? ReadBusDisplayName(string path)
    {
        var lines = ReadLines(path).ToArray();
        for (var index = 0; index < lines.Length; index++)
        {
            if (!lines[index].Trim().Equals("[friendlyname]", StringComparison.OrdinalIgnoreCase)) continue;
            var name = lines.Skip(index + 1).TakeWhile(x => !string.IsNullOrWhiteSpace(x) && !x.TrimStart().StartsWith("[", StringComparison.Ordinal)).Select(x => x.Trim()).ToArray();
            return name.Length == 0 ? Path.GetFileNameWithoutExtension(path) : string.Join(" ", name);
        }
        return Path.GetFileNameWithoutExtension(path);
    }

    private static IEnumerable<(int Ordinal, string Name)> ReadCtiItems(string path)
    {
        var lines = ReadLines(path).ToArray(); var ordinal = 0;
        for (var index = 0; index + 1 < lines.Length; index++)
        {
            if (!lines[index].Trim().Equals("[item]", StringComparison.OrdinalIgnoreCase)) continue;
            var name = lines[index + 1].Trim(); if (!string.IsNullOrWhiteSpace(name)) yield return (++ordinal, name);
        }
    }

    private static string? ReadTaggedValue(string path, string token)
    {
        var lines = ReadLines(path).ToArray(); var index = Array.FindIndex(lines, x => x.Trim().Equals("[" + token + "]", StringComparison.OrdinalIgnoreCase));
        return index < 0 ? null : lines.Skip(index + 1).FirstOrDefault(x => !string.IsNullOrWhiteSpace(x) && !x.TrimStart().StartsWith("[", StringComparison.Ordinal))?.Trim();
    }
    private static string VehicleRelative(VehicleContent vehicle, string source) => Path.GetRelativePath(Path.GetDirectoryName(vehicle.AbsolutePath)!, Path.GetFullPath(Path.Combine(Path.GetDirectoryName(vehicle.AbsolutePath)!, source))).Replace('/', '\\');

    private static IEnumerable<string> ReadLines(string path, int maximum = 512)
    {
        // File.ReadLines still honours a UTF-8/UTF-16 BOM when one is present; the
        // supplied encoding only decides how BOM-less (i.e. ordinary OMSI) files decode.
        try { return File.ReadLines(path, Ansi).Take(maximum).ToArray(); }
        catch { return Array.Empty<string>(); }
    }
}
