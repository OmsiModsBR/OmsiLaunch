using System.Diagnostics;
using System.Text;
using OmsiLaunch.NativeResolution;

// The OMSI product version comes from the literal the game logs ("Version:
// 2.3.004"), never from the PE file version (2.2.032 on the reference build).
internal static class OmsiVersionLiteralTests
{
    public static void Run()
    {
        FindsTheDelphiLiteral();
        RejectsAnythingThatIsNotTheExactLayout();
        AmbiguityIsUnknown();
        ReferenceExecutableReportsTheProductVersionNotTheFileVersion();
        Console.WriteLine("PASS omsi-version.literal");
    }

    // { codepage 1200, elemsize 2, refcount -1, length } + UTF-16 + 0 0.
    private static byte[] DelphiString(string text, int? declaredLength = null, bool terminate = true)
    {
        var bytes = new List<byte> { 0xB0, 0x04, 0x02, 0x00, 0xFF, 0xFF, 0xFF, 0xFF };
        bytes.AddRange(BitConverter.GetBytes(declaredLength ?? text.Length));
        bytes.AddRange(Encoding.Unicode.GetBytes(text));
        if (terminate) bytes.AddRange(new byte[] { 0, 0 });
        return bytes.ToArray();
    }

    private static byte[] Image(params byte[][] parts) => new byte[] { 0x90, 0x90, 0x90, 0x90 }.Concat(parts.SelectMany(x => new byte[] { 0xCC, 0xCC }.Concat(x))).Concat(new byte[] { 0xCC }).ToArray();

    private static void FindsTheDelphiLiteral()
    {
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Release 2.3.004"), DelphiString("Version: 2.3.004"))) == "2.3.004", "the Version: literal is found among other version strings");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.004"), DelphiString("Version: 2.3.004"))) == "2.3.004", "identical repeats still answer");
    }

    private static void RejectsAnythingThatIsNotTheExactLayout()
    {
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Release 2.3.004"))) is null, "'Release' is not the log literal");
        Require(OmsiVersionLiteral.Find(Encoding.Unicode.GetBytes("xxVersion: 2.3.004\0")) is null, "a bare UTF-16 string has no Delphi header");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.004", declaredLength: 20))) is null, "the declared length must match");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.004", terminate: false))) is null, "the terminating zero is required");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.00x"))) is null, "digits only");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3"))) is null, "n.n.nnn only");
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.004 beta"))) is null, "no trailing text");
        Require(OmsiVersionLiteral.Find(Array.Empty<byte>()) is null, "empty image");
    }

    private static void AmbiguityIsUnknown() =>
        Require(OmsiVersionLiteral.Find(Image(DelphiString("Version: 2.3.004"), DelphiString("Version: 2.2.032"))) is null, "two different versions are not guessed");

    // Optional: runs only where the reference installation exists (read only).
    private static void ReferenceExecutableReportsTheProductVersionNotTheFileVersion()
    {
        var path = Environment.GetEnvironmentVariable("OMSILAUNCH_TEST_OMSI_EXE") ?? @"D:\BBF2024\OMSI2 - Clean\Omsi.exe";
        if (!File.Exists(path)) { Console.WriteLine("SKIP omsi-version.reference-executable (no Omsi.exe at " + path + ")"); return; }
        var bytes = File.ReadAllBytes(path);
        var fileVersion = FileVersionInfo.GetVersionInfo(path).FileVersion;
        var found = OmsiVersionLiteral.Find(bytes);
        Require(found == "2.3.004", "the reference executable reports 2.3.004, found " + (found ?? "null"));
        Require(found != fileVersion, "the product version is not the PE file version (" + fileVersion + ")");
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("omsi-version: " + message); }
}
