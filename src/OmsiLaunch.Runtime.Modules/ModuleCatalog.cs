using System.Security.Cryptography;
using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// A module found below plugins\OmsiLaunch.Modules. Manifest is null when the
// module.json could not be parsed; Code then says why.
public sealed record DiscoveredModule(
    string DirectoryName,
    string Directory,
    string RelativeDirectory,
    string? ManifestSha256,
    ModuleManifest? Manifest,
    string? Code,
    string? Message,
    string? SnapshotSha256 = null)
{
    public bool IsUsable => Manifest is not null && Code is null;
}

// Discovery and integrity (lifecycle step 1, controller side, and the
// in-process re-verification). Loading is open: any module installed below
// plugins\OmsiLaunch.Modules may load (decision E21). The SHA-256 pins prove
// only that the module's files are the ones its manifest names, so a module
// that is corrupted or half updated is rejected; they are not a trust check.
public static class ModuleCatalog
{
    public static IReadOnlyList<DiscoveredModule> Discover(string installationRoot)
    {
        var modulesRoot = Path.Combine(installationRoot, RuntimeModuleProtocol.ModulesDirectory);
        if (!System.IO.Directory.Exists(modulesRoot)) return Array.Empty<DiscoveredModule>();
        return System.IO.Directory.EnumerateDirectories(modulesRoot)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(Inspect)
            .ToArray();
    }

    // Planning: the module is read, every pin is checked and the snapshot of
    // its files is taken (SnapshotSha256). The snapshot is what travels with
    // the plan: the plugin and the controller compare the files they are about
    // to run against it, whether or not the author pinned them.
    public static DiscoveredModule Inspect(string directory)
    {
        var read = Read(directory);
        if (read.Code is not null || read.Manifest is null) return read;
        var hashes = HashNamedFiles(directory, read.Manifest, out var failure);
        if (failure is not null) return read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = failure };
        return read with { SnapshotSha256 = SnapshotOf(hashes!) };
    }

    // In process (or when the controller starts a session program): the module
    // the controller planned. The module.json must be the planned one (same
    // SHA-256), every pin must still hold and, when the plan carried a
    // snapshot, the module's files must be exactly the ones it recorded.
    public static DiscoveredModule InspectPlanned(string directory, string plannedManifestSha256, string? plannedSnapshotSha256 = null)
    {
        var read = Read(directory);
        if (read.Code is not null || read.Manifest is null) return read;
        if (!string.Equals(read.ManifestSha256, plannedManifestSha256, StringComparison.OrdinalIgnoreCase)) return read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = "module.json changed after planning" };
        var hashes = HashNamedFiles(directory, read.Manifest, out var failure);
        if (failure is not null) return read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = failure };
        var snapshot = SnapshotOf(hashes!);
        if (plannedSnapshotSha256 is { Length: > 0 } && !string.Equals(snapshot, plannedSnapshotSha256, StringComparison.OrdinalIgnoreCase))
            return read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = "the module's files changed after planning" };
        return read with { SnapshotSha256 = snapshot };
    }

    // The component the bridge is about to run (a native DLL, an auxiliary or
    // a host program): its pin, when it has one, and in every case the plan's
    // snapshot, so a file the author did not pin is still the planned file.
    // Null when it holds, otherwise the reason.
    public static string? VerifyComponent(string directory, ModuleManifest manifest, ModuleComponent component, string? plannedSnapshotSha256)
    {
        var single = manifest with { Components = new[] { component }, Assets = Array.Empty<ModuleFileReference>() };
        if (VerifyPins(directory, single) is { } failure) return failure;
        if (plannedSnapshotSha256 is not { Length: > 0 }) return null;
        var hashes = HashNamedFiles(directory, manifest, out var snapshotFailure);
        if (snapshotFailure is not null) return snapshotFailure;
        return string.Equals(SnapshotOf(hashes!), plannedSnapshotSha256, StringComparison.OrdinalIgnoreCase) ? null : "the module's files changed after planning";
    }

    // The manifests that parse, without hashing the modules' files: enough for
    // the command line to resolve the flags modules declare.
    public static IReadOnlyList<ModuleManifest> ReadDeclarations(string installationRoot)
    {
        var modulesRoot = Path.Combine(installationRoot, RuntimeModuleProtocol.ModulesDirectory);
        if (!System.IO.Directory.Exists(modulesRoot)) return Array.Empty<ModuleManifest>();
        return System.IO.Directory.EnumerateDirectories(modulesRoot)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .Select(Read)
            .Where(x => x.Code is null && x.Manifest is not null)
            .Select(x => x.Manifest!)
            .ToArray();
    }

    private static DiscoveredModule Read(string directory)
    {
        var name = Path.GetFileName(directory.TrimEnd('\\'));
        var relative = RuntimeModuleProtocol.ModulesDirectory + "\\" + name;
        DiscoveredModule Reject(string code, string message, string? sha = null, ModuleManifest? manifest = null) => new(name, directory, relative, sha, manifest, code, message);
        if (IsReparsePoint(directory)) return Reject(PublicErrorCodes.MODULE_INTEGRITY, "the module directory is a reparse point");
        var manifestPath = Path.Combine(directory, RuntimeModuleProtocol.ManifestFileName);
        if (!File.Exists(manifestPath)) return Reject(PublicErrorCodes.MODULE_MANIFEST_INVALID, "module.json is missing");
        byte[] bytes;
        try { bytes = File.ReadAllBytes(manifestPath); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return Reject(PublicErrorCodes.MODULE_MANIFEST_INVALID, exception.Message); }
        var manifestSha = Convert.ToHexString(SHA256.HashData(bytes));
        ModuleManifest manifest;
        try { manifest = ModuleManifestParser.Parse(bytes); }
        catch (ModuleException exception) { return Reject(exception.Code, exception.Message, manifestSha); }
        if (!string.Equals(manifest.Id, name, StringComparison.Ordinal)) return Reject(PublicErrorCodes.MODULE_MANIFEST_INVALID, "the module id '" + manifest.Id + "' does not equal its directory name", manifestSha, manifest);
        return new(name, directory, relative, manifestSha, manifest, null, null);
    }

    // Null when every file the manifest names exists (not through a reparse
    // point) and every pinned one has its pinned SHA-256; otherwise the first
    // problem. A file without a pin is checked for existence and path safety only.
    public static string? VerifyPins(string directory, ModuleManifest manifest)
    {
        HashNamedFiles(directory, manifest, out var failure);
        return failure;
    }

    // SHA-256 of every named file (pinned or not), keyed by its path as the
    // manifest writes it; null with a failure on the first problem (a missing
    // file, a path that escapes the module directory, a reparse point, an I/O
    // error, a pin that does not match).
    private static SortedDictionary<string, string>? HashNamedFiles(string directory, ModuleManifest manifest, out string? failure)
    {
        failure = null;
        var root = Path.GetFullPath(directory).TrimEnd('\\') + "\\";
        var hashes = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in manifest.NamedFiles)
        {
            var path = Path.GetFullPath(Path.Combine(directory, file.Path));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) { failure = file.Path + " escapes the module directory"; return null; }
            if (!File.Exists(path)) { failure = file.Path + " is missing"; return null; }
            for (var current = path; current.Length > root.Length; current = Path.GetDirectoryName(current)!)
                if (IsReparsePoint(current)) { failure = file.Path + " is reached through a reparse point"; return null; }
            string actual;
            try { using var stream = File.OpenRead(path); actual = Convert.ToHexString(ModuleCatalog.HashStream(stream)); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { failure = file.Path + ": " + exception.Message; return null; }
            if (file.Sha256 is not null && !string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase)) { failure = file.Path + " does not match its pinned SHA-256"; return null; }
            hashes[file.Path] = actual;
        }
        return hashes;
    }

    // One digest of the named files and their content: the plan's snapshot. It
    // is a consistency check (the files run are the files planned), not a
    // trust decision, and it covers files the author never pinned.
    private static string SnapshotOf(SortedDictionary<string, string> hashes)
    {
        var text = new System.Text.StringBuilder();
        foreach (var (path, hash) in hashes) text.Append(path.ToLowerInvariant()).Append('|').Append(hash).Append('\n');
        return Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(text.ToString())));
    }

    // SHA256.HashData(Stream) needs .NET 7.
    internal static byte[] HashStream(Stream stream) { using var sha = SHA256.Create(); return sha.ComputeHash(stream); }

    public static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return true; }
    }
}
