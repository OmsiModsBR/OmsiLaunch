using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.NativeResolution;

namespace OmsiLaunch.Plugin;

// The OMSI product version a runtime module may read ("omsi_version"):
// what the game reports about itself, never the executable's PE file version.
// 1. The "Version: n.n.nnn" literal of the running image (the same text OMSI
//    writes on line 7 of its logfile.txt); it is what the game says it is.
// 2. For a build whose code identity the catalog knows, the product version of
//    that profile (2.3.004 for the reference build).
// 3. Otherwise "unknown". The PE file version (2.2.032 on the reference
//    build) is not a fallback.
internal static class PluginOmsiVersion
{
    public const string Unknown = "unknown";
    private static string? value;

    public static string Value => value ??= Resolve();

    private static string Resolve()
    {
        try
        {
            var literal = OmsiVersionLiteral.Find(NativeImage.FromCurrentProcess().Bytes);
            if (literal is not null) return literal;
        }
        catch (Exception) { }
        return PluginNativeTargets.Resolution is { KnownIdentity: true } ? Omsi23004.Profile.Executable.ProductRuntimeVersion : Unknown;
    }
}
