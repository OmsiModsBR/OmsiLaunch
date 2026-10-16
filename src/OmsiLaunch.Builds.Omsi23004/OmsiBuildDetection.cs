using System.Diagnostics;
using System.Security.Cryptography;
using OmsiLaunch.NativeResolution;
using ResolvedImage = OmsiLaunch.NativeResolution.NativeResolution;

namespace OmsiLaunch.Builds.Omsi23004;

// Result of inspecting an installation's Omsi.exe on disk. IsOmsi2 is the only
// compatibility gate: any OMSI 2 executable is accepted. Hashes and the code
// identity are diagnostics and select the known-layout fast path; capability
// availability is decided in process by the resolver. StaticCapabilities is a
// prediction from the file image only (a DRM-protected file can differ from
// its loaded image, so an unresolved prediction is not an error).
public sealed record OmsiBuildReport(
    bool Found,
    bool IsOmsi2,
    string? Reason,
    string? FileSha256,
    long FileSize,
    string? FileVersion,
    string? CompanyName,
    string? CodeSha256,
    bool KnownIdentity,
    string BuildId,
    string? KnownFileVariant,
    IReadOnlyDictionary<string, bool> StaticCapabilities);

public static class OmsiBuildDetection
{
    public const string UnrecognizedBuildId = "unrecognized";
    private static readonly Dictionary<string, OmsiBuildReport> Cache = new(StringComparer.OrdinalIgnoreCase);

    public static OmsiBuildReport Detect(string installationRoot)
    {
        var path = Path.Combine(installationRoot, "Omsi.exe");
        var empty = new Dictionary<string, bool>(StringComparer.Ordinal);
        if (!File.Exists(path)) return new(false, false, "Omsi.exe was not found in the installation root.", null, 0, null, null, null, false, UnrecognizedBuildId, null, empty);
        // The installation layout is checked per root; only the executable
        // analysis is cached (by file hash).
        if (!Directory.Exists(Path.Combine(installationRoot, "maps")) || !Directory.Exists(Path.Combine(installationRoot, "Vehicles")))
            return new(true, false, "The installation root has no maps\\ or Vehicles\\ directory.", null, 0, null, null, null, false, UnrecognizedBuildId, null, empty);
        var bytes = File.ReadAllBytes(path);
        var fileSha256 = Convert.ToHexString(SHA256.HashData(bytes));
        lock (Cache) if (Cache.TryGetValue(fileSha256, out var cached)) return cached;

        string? fileVersion = null, company = null; var major = 0;
        try { var info = FileVersionInfo.GetVersionInfo(path); fileVersion = info.FileVersion; company = info.CompanyName; major = info.FileMajorPart; }
        catch (FileNotFoundException) { }
        NativeImage image;
        try { image = NativeImage.FromFile(bytes); }
        catch (InvalidDataException exception) { return Remember(fileSha256, new(true, false, "Omsi.exe is not a 32-bit x86 PE image: " + exception.Message, fileSha256, bytes.LongLength, fileVersion, company, null, false, UnrecognizedBuildId, null, empty)); }
        if (major != 2) return Remember(fileSha256, new(true, false, "Omsi.exe does not carry an OMSI 2 version (2.x); found " + (fileVersion ?? "none") + ".", fileSha256, bytes.LongLength, fileVersion, company, image.CodeSha256, false, UnrecognizedBuildId, null, empty));

        ResolvedImage resolution = NativeResolver.Resolve(image, NativeTargetCatalog.Pack);
        var variant = string.Equals(fileSha256, Omsi23004.Profile.Executable.Sha256, StringComparison.OrdinalIgnoreCase) ? "reference"
            : Omsi23004.Profile.CompatibleExecutableHashes.TryGetValue(fileSha256, out var name) ? name : null;
        return Remember(fileSha256, new(true, true, null, fileSha256, bytes.LongLength, fileVersion, company, image.CodeSha256, resolution.KnownIdentity,
            resolution.KnownIdentity ? NativeTargetCatalog.PackName : UnrecognizedBuildId, variant,
            resolution.Capabilities.ToDictionary(x => x.Key, x => x.Value.Available, StringComparer.Ordinal)));
    }

    private static OmsiBuildReport Remember(string key, OmsiBuildReport report) { lock (Cache) Cache[key] = report; return report; }
}
