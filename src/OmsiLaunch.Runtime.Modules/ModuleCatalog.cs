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
    string? Message)
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

    public static DiscoveredModule Inspect(string directory)
    {
        var read = Read(directory);
        if (read.Code is not null || read.Manifest is null) return read;
        var integrity = VerifyPins(directory, read.Manifest);
        return integrity is null ? read : read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = integrity };
    }

    // In process: the module the controller planned. The module.json must be
    // the planned one (same SHA-256) and every pin must still hold.
    public static DiscoveredModule InspectPlanned(string directory, string plannedManifestSha256)
    {
        var read = Read(directory);
        if (read.Code is not null || read.Manifest is null) return read;
        if (!string.Equals(read.ManifestSha256, plannedManifestSha256, StringComparison.OrdinalIgnoreCase)) return read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = "module.json changed after planning" };
        var integrity = VerifyPins(directory, read.Manifest);
        return integrity is null ? read : read with { Code = PublicErrorCodes.MODULE_INTEGRITY, Message = integrity };
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

    // Null when every pinned file exists (not through a reparse point) and has
    // its pinned SHA-256; otherwise the first mismatch.
    public static string? VerifyPins(string directory, ModuleManifest manifest)
    {
        var root = Path.GetFullPath(directory).TrimEnd('\\') + "\\";
        foreach (var file in manifest.PinnedFiles)
        {
            var path = Path.GetFullPath(Path.Combine(directory, file.Path));
            if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return file.Path + " escapes the module directory";
            if (!File.Exists(path)) return file.Path + " is missing";
            for (var current = path; current.Length > root.Length; current = Path.GetDirectoryName(current)!)
                if (IsReparsePoint(current)) return file.Path + " is reached through a reparse point";
            string actual;
            try { using var stream = File.OpenRead(path); actual = Convert.ToHexString(ModuleCatalog.HashStream(stream)); }
            catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return file.Path + ": " + exception.Message; }
            if (!string.Equals(actual, file.Sha256, StringComparison.OrdinalIgnoreCase)) return file.Path + " does not match its pinned SHA-256";
        }
        return null;
    }

    // SHA256.HashData(Stream) needs .NET 7.
    internal static byte[] HashStream(Stream stream) { using var sha = SHA256.Create(); return sha.ComputeHash(stream); }

    public static bool IsReparsePoint(string path)
    {
        try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0; }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return true; }
    }
}
