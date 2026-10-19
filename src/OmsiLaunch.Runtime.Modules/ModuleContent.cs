using System.Security.Cryptography;
using System.Text;
using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// Content a module reads that its author cannot know when the module is built
// (bridge protocol 3, manifest content_inputs). Unlike assets there is no
// SHA-256 to pin: the content is made by the community, for a map, after the
// module. The bridge only locates it and checks that reading it is safe.
public enum ModuleContentInputKind : byte { RealmapPackage }

public sealed record ModuleContentInput(ModuleContentInputKind Kind, bool Required);

// Where a package of the map is and a change detector for it. The fingerprint
// is a cache key (it changes when a file is added, removed, resized or
// rewritten); it is never an integrity check or a trust decision.
public sealed record RealmapPackageLocation(string MapIdentity, string Directory, string Fingerprint, int FileCount, long TotalBytes);

// The answer of IModuleContent.LocateRealmapPackage: Found false with no code
// means the map has no package (not an error); a code means the package exists
// but cannot be read safely.
public sealed record ModuleContentResult(bool Found, RealmapPackageLocation? Package, string? ErrorCode, string? Detail);

public interface IModuleContent
{
    // True when the manifest declares the input kind.
    bool IsDeclared(ModuleContentInputKind kind);
    // The openomsi-realmap package of a map (maps\<folder>\global.cfg), below
    // the map's own folder. The package is data made by the community: read it
    // in managed code, treat every value as untrusted, and degrade by resource.
    ModuleContentResult LocateRealmapPackage(string mapIdentity);
}

// The folder the openOMSI map package tooling writes next to a map.
public static class RealmapPackage
{
    public const string FolderName = "openomsi-realmap";
    // A package is cartography, regions, markers and lines: bounded, so a
    // hostile or broken folder cannot stall the UI thread that locates it.
    public const int MaxFiles = 50_000;
    public const long MaxTotalBytes = 4L * 1024 * 1024 * 1024;

    // maps\<folder>\global.cfg -> <root>\maps\<folder>\openomsi-realmap, or null
    // when the identity is not a map identity.
    public static string? Directory(string installationRoot, string mapIdentity)
    {
        var normalized = mapIdentity.Replace('/', '\\');
        if (!normalized.StartsWith(@"maps\", StringComparison.OrdinalIgnoreCase) || !normalized.EndsWith(@"\global.cfg", StringComparison.OrdinalIgnoreCase) || !ModuleManifestParser.IsRelative(normalized)) return null;
        var folder = normalized[..normalized.LastIndexOf('\\')];
        if (folder.Split('\\').Length != 2) return null;
        return Path.Combine(installationRoot, folder, FolderName);
    }

    public static ModuleContentResult Locate(string installationRoot, string mapIdentity)
    {
        var directory = Directory(installationRoot, mapIdentity);
        if (directory is null) return new(false, null, PublicErrorCodes.INVALID_ARGUMENT, @"the map identity must be maps\<folder>\global.cfg");
        var root = Path.GetFullPath(installationRoot).TrimEnd('\\') + "\\";
        var full = Path.GetFullPath(directory);
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) return Unsafe("the package folder is outside the installation");
        if (!System.IO.Directory.Exists(full)) return new(false, null, null, null);
        // No part of the path below the installation root may be a link.
        for (var current = full; current.Length > root.Length; current = Path.GetDirectoryName(current)!.TrimEnd('\\'))
            if (ModuleCatalog.IsReparsePoint(current)) return Unsafe("the package is reached through a reparse point");
        var entries = new List<(string Relative, long Length, long Ticks)>();
        long total = 0;
        try
        {
            foreach (var path in System.IO.Directory.EnumerateFileSystemEntries(full, "*", SearchOption.AllDirectories))
            {
                if (ModuleCatalog.IsReparsePoint(path)) return Unsafe("the package contains a reparse point: " + Path.GetRelativePath(full, path));
                if (System.IO.Directory.Exists(path)) continue;
                var info = new FileInfo(path);
                total += info.Length;
                if (entries.Count >= MaxFiles) return Unsafe("the package has more than " + MaxFiles + " files");
                if (total > MaxTotalBytes) return Unsafe("the package is larger than " + MaxTotalBytes / (1024 * 1024) + " MiB");
                entries.Add((Path.GetRelativePath(full, path).Replace('/', '\\'), info.Length, info.LastWriteTimeUtc.Ticks));
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { return Unsafe("the package cannot be read: " + exception.Message); }
        var text = new StringBuilder();
        foreach (var (relative, length, ticks) in entries.OrderBy(x => x.Relative, StringComparer.Ordinal))
            text.Append(relative).Append('|').Append(length).Append('|').Append(ticks).Append('\n');
        var fingerprint = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text.ToString())));
        return new(true, new(mapIdentity.Replace('/', '\\'), full, fingerprint, entries.Count, total), null, null);

        static ModuleContentResult Unsafe(string detail) => new(false, null, PublicErrorCodes.MODULE_CONTENT_INPUT_INVALID, detail);
    }
}
