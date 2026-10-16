using OmsiLaunch.Api;

namespace OmsiLaunch.Core;

// Session HOF management. Resolves every HOF operation of a LaunchSpec into
// exact file overlays and session deletions for the generic transaction, which
// stays the sole backup and restore authority. Nothing here writes a file.
// Sources and targets are confined to the installation and never traverse a
// junction or symbolic link; conflicting operations are rejected, not merged.
internal static class HofPlanner
{
    internal const long MaxFileBytes = 8L * 1024 * 1024;
    internal const int MaxFilesPerOperation = 256;
    internal const long MaxTotalBytes = 256L * 1024 * 1024;

    internal sealed record Plan(IReadOnlyDictionary<string, byte[]> Overlays, IReadOnlyList<string> Deletions, IReadOnlyList<PlannedMutation> Mutations)
    {
        public static readonly Plan Empty = new(new Dictionary<string, byte[]>(), Array.Empty<string>(), Array.Empty<PlannedMutation>());
    }

    internal static Plan Build(LaunchSpec spec)
    {
        var operations = spec.EffectiveHofManagement.Operations;
        if (operations.Count == 0) return Plan.Empty;
        var root = InstallationPaths.NormalizeRoot(spec.Installation.RootPath);
        var overlays = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        var sources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var deletions = new List<string>();
        var mutations = new List<PlannedMutation>();
        long total = 0;
        foreach (var operation in operations)
        {
            var (sourceIdentity, files) = ResolveSource(root, operation);
            if (operation.TargetVehicles is null || operation.TargetVehicles.Count == 0) throw Error("OL_E_HOF_INVALID", "A HOF operation requires at least one target vehicle.");
            foreach (var targetFolder in operation.TargetVehicles.Select(target => ResolveTargetFolder(root, target)).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var existing = ExistingHofs(root, targetFolder);
                foreach (var file in files)
                {
                    var relative = targetFolder + "\\" + (existing.FirstOrDefault(x => x.Equals(file.Name, StringComparison.OrdinalIgnoreCase)) ?? file.Name);
                    if (overlays.TryGetValue(relative, out var planned))
                    {
                        if (!planned.AsSpan().SequenceEqual(file.Bytes)) throw Error("OL_E_HOF_CONFLICT", "Two HOF operations write different content to " + relative + ".");
                        continue;
                    }
                    if (deletions.Contains(relative, StringComparer.OrdinalIgnoreCase)) throw Error("OL_E_HOF_CONFLICT", "A HOF operation writes " + relative + ", which another operation removes.");
                    var full = Path.Combine(root, relative);
                    // An identical file needs no transaction entry: the session
                    // would observe the same bytes either way.
                    if (File.Exists(full) && Equal(full, file.Bytes)) { sources[relative] = sourceIdentity; continue; }
                    total += file.Bytes.Length;
                    if (total > MaxTotalBytes) throw Error("OL_E_HOF_INVALID", "HOF operations exceed the 256 MiB session limit.");
                    overlays[relative] = file.Bytes; sources[relative] = sourceIdentity;
                    mutations.Add(new(relative, "hof." + operation.Mode.ToString().ToLowerInvariant(), sourceIdentity, "exact-file-overlay"));
                }
                if (operation.Mode != HofApplyMode.Mirror) continue;
                // The folder's HOF set during the session is what exists plus
                // what earlier operations write there; a mirror covers both.
                var plannedNames = overlays.Keys.Concat(sources.Keys)
                    .Where(key => string.Equals(Path.GetDirectoryName(key), targetFolder, StringComparison.OrdinalIgnoreCase))
                    .Select(key => Path.GetFileName(key));
                foreach (var name in existing.Concat(plannedNames).Distinct(StringComparer.OrdinalIgnoreCase).Where(name => !files.Any(file => file.Name.Equals(name, StringComparison.OrdinalIgnoreCase))))
                {
                    var relative = targetFolder + "\\" + name;
                    if (overlays.ContainsKey(relative) || sources.ContainsKey(relative)) throw Error("OL_E_HOF_CONFLICT", "A HOF mirror removes " + relative + ", which another operation writes.");
                    if (deletions.Contains(relative, StringComparer.OrdinalIgnoreCase)) continue;
                    deletions.Add(relative);
                    mutations.Add(new(relative, "hof.mirror", sourceIdentity, "session-deletion"));
                }
            }
        }
        return new Plan(overlays, deletions, mutations);
    }

    private sealed record SourceFile(string Name, byte[] Bytes);

    private static (string Identity, IReadOnlyList<SourceFile> Files) ResolveSource(string root, HofOperationSpec operation)
    {
        if (string.IsNullOrWhiteSpace(operation.Source)) throw Error("OL_E_HOF_INVALID", "A HOF operation requires a source.");
        string directory; string identity; IEnumerable<string> candidates;
        switch (operation.SourceKind)
        {
            case HofSourceKind.Collection:
                if (operation.Source.IndexOfAny(new[] { '\\', '/', ':' }) >= 0 || operation.Source.Contains("..", StringComparison.Ordinal))
                    throw Error("OL_E_HOF_PATH_ESCAPE", "A HOF collection name must be a plain directory name.");
                identity = ".omsilaunch\\hof\\" + operation.Source;
                directory = ContainedExisting(root, identity, directoryExpected: true);
                candidates = Directory.EnumerateFiles(directory, "*.hof", SearchOption.TopDirectoryOnly);
                break;
            case HofSourceKind.Directory:
                identity = Contained(root, operation.Source);
                directory = ContainedExisting(root, identity, directoryExpected: true);
                candidates = Directory.EnumerateFiles(directory, "*.hof", SearchOption.TopDirectoryOnly);
                break;
            case HofSourceKind.Vehicle:
                identity = ResolveTargetFolder(root, operation.Source, sourceRole: true);
                directory = Path.Combine(root, identity);
                candidates = Directory.EnumerateFiles(directory, "*.hof", SearchOption.TopDirectoryOnly);
                break;
            case HofSourceKind.File:
                identity = Contained(root, operation.Source);
                if (!identity.EndsWith(".hof", StringComparison.OrdinalIgnoreCase)) throw Error("OL_E_HOF_INVALID", "A HOF file source must be a .hof file.");
                var file = ContainedExisting(root, identity, directoryExpected: false);
                directory = Path.GetDirectoryName(file)!;
                candidates = new[] { file };
                break;
            default:
                throw Error("OL_E_HOF_INVALID", "Unknown HOF source kind.");
        }
        var all = candidates
            .Where(path => path.EndsWith(".hof", StringComparison.OrdinalIgnoreCase))
            .Where(path => !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            .ToDictionary(path => Path.GetFileName(path), path => path, StringComparer.OrdinalIgnoreCase);
        IEnumerable<string> selected;
        if (operation.Files is { Count: > 0 } requested)
        {
            foreach (var name in requested)
                if (!all.ContainsKey(name ?? string.Empty)) throw Error("OL_E_HOF_SOURCE_NOT_FOUND", "HOF '" + name + "' is not in source " + identity + ".");
            selected = requested.Distinct(StringComparer.OrdinalIgnoreCase).Select(name => all[name]);
        }
        else selected = all.Values;
        var files = selected.OrderBy(path => Path.GetFileName(path), StringComparer.OrdinalIgnoreCase).Select(path =>
        {
            var length = new FileInfo(path).Length;
            if (length > MaxFileBytes) throw Error("OL_E_HOF_INVALID", "HOF file exceeds the 8 MiB limit: " + Path.GetFileName(path));
            return new SourceFile(Path.GetFileName(path), File.ReadAllBytes(path));
        }).ToArray();
        if (files.Length == 0) throw Error("OL_E_HOF_SOURCE_NOT_FOUND", "HOF source " + identity + " contains no .hof file.");
        if (files.Length > MaxFilesPerOperation) throw Error("OL_E_HOF_INVALID", "A HOF operation is limited to 256 files.");
        return (identity, files);
    }

    // A target is an installed vehicle: the identity of its .bus/.ovh file or
    // its folder, below Vehicles\. Returns the installation-relative folder.
    private static string ResolveTargetFolder(string root, string target, bool sourceRole = false)
    {
        var code = sourceRole ? "OL_E_HOF_SOURCE_NOT_FOUND" : "OL_E_HOF_TARGET_NOT_FOUND";
        var relative = Contained(root, target);
        var segments = InstallationPaths.Segments(relative);
        if (segments.Count < 2 || !segments[0].Equals("Vehicles", StringComparison.OrdinalIgnoreCase)) throw Error("OL_E_HOF_PATH_ESCAPE", "HOF vehicles must be below Vehicles\\: " + target);
        var full = Path.Combine(root, relative);
        string folder;
        if (File.Exists(full))
        {
            if (!relative.EndsWith(".bus", StringComparison.OrdinalIgnoreCase) && !relative.EndsWith(".ovh", StringComparison.OrdinalIgnoreCase)) throw Error("OL_E_HOF_INVALID", "A HOF vehicle must be a .bus or .ovh file or a vehicle folder: " + target);
            folder = Path.GetDirectoryName(relative)!;
        }
        else if (Directory.Exists(full)) folder = relative;
        else throw Error(code, "Vehicle not found: " + target);
        if (PathConfinement.ContainsReparsePoint(root, Path.Combine(root, folder))) throw Error("OL_E_HOF_PATH_ESCAPE", "HOF vehicle folders must not traverse junctions or symbolic links: " + target);
        return folder;
    }

    private static IReadOnlyList<string> ExistingHofs(string root, string folder) =>
        Directory.EnumerateFiles(Path.Combine(root, folder), "*.hof", SearchOption.TopDirectoryOnly)
            .Where(path => path.EndsWith(".hof", StringComparison.OrdinalIgnoreCase) && !File.GetAttributes(path).HasFlag(FileAttributes.ReparsePoint))
            .Select(path => Path.GetFileName(path)).OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();

    private static string Contained(string root, string candidate)
    {
        if (Path.IsPathRooted(candidate) || candidate.StartsWith("\\", StringComparison.Ordinal) || InstallationPaths.Segments(candidate).Any(x => x == "..") || !InstallationPaths.TryGetContainedRelativePath(root, candidate, out var relative))
            throw Error("OL_E_HOF_PATH_ESCAPE", "HOF paths must be relative to the installation and stay inside it: " + candidate);
        return relative;
    }

    private static string ContainedExisting(string root, string relative, bool directoryExpected)
    {
        var full = Path.Combine(root, relative);
        if (directoryExpected ? !Directory.Exists(full) : !File.Exists(full)) throw Error("OL_E_HOF_SOURCE_NOT_FOUND", "HOF source not found: " + relative);
        if (PathConfinement.ContainsReparsePoint(root, full)) throw Error("OL_E_HOF_PATH_ESCAPE", "HOF sources must not traverse junctions or symbolic links: " + relative);
        return full;
    }

    private static bool Equal(string path, byte[] bytes)
    {
        var info = new FileInfo(path);
        if (info.Length != bytes.Length) return false;
        return File.ReadAllBytes(path).AsSpan().SequenceEqual(bytes);
    }

    private static HofPlanningException Error(string code, string message) => new(code, message);
}

public sealed class HofPlanningException : InvalidOperationException
{
    public HofPlanningException(string code, string message) : base(code + ": " + message) { Code = code; }
    public string Code { get; }
}
