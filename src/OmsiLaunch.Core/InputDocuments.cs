using OmsiLaunch.Api;
using OmsiLaunch.Configuration;

namespace OmsiLaunch.Core;

// LaunchSpec.Input: a keyboard or controller file in OMSI's own format that
// the session uses in place of Inputs\keyboard.cfg or Inputs\gamectrler.cfg.
// The file is an overlay of the session transaction, so the installation's
// own file is restored byte for byte when the session ends.
internal static class InputDocuments
{
    public const string KeyboardTarget = @"Inputs\keyboard.cfg";
    public const string ControllerTarget = @"Inputs\gamectrler.cfg";
    private const long MaxBytes = 1 << 20;

    public sealed record Document(string Capability, string Target, string Source, byte[] Bytes);

    // The documents a spec asks for; error names the first one that cannot be used.
    public static IReadOnlyList<Document> Load(LaunchSpec spec, out string? error)
    {
        error = null;
        var documents = new List<Document>();
        var input = spec.EffectiveInput;
        if (input.KeyboardDocument.IsSet && Read(spec.Installation.RootPath, input.KeyboardDocument.Value, true, ref error) is { } keyboard)
            documents.Add(new("input.keyboard", KeyboardTarget, input.KeyboardDocument.Value!, keyboard));
        if (input.ControllerDocument.IsSet && Read(spec.Installation.RootPath, input.ControllerDocument.Value, false, ref error) is { } controller)
            documents.Add(new("input.controller", ControllerTarget, input.ControllerDocument.Value!, controller));
        return documents;
    }

    private static byte[]? Read(string root, string? document, bool keyboard, ref string? error)
    {
        var what = keyboard ? "KeyboardDocument" : "ControllerDocument";
        if (string.IsNullOrWhiteSpace(document)) { error ??= what + " is empty."; return null; }
        string path;
        try { path = Path.GetFullPath(Path.IsPathRooted(document) ? document : Path.Combine(root, document)); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or PathTooLongException) { error ??= what + " is not a valid path."; return null; }
        var info = new FileInfo(path);
        if (!info.Exists) { error ??= what + " does not exist: " + document; return null; }
        if (info.Length == 0 || info.Length > MaxBytes) { error ??= what + " must be a non-empty file of at most 1 MiB."; return null; }
        byte[] bytes;
        try { bytes = File.ReadAllBytes(path); }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { error ??= what + " cannot be read: " + exception.Message; return null; }
        try
        {
            var valid = keyboard ? new KeyboardDocument(bytes).Bindings.Count > 0 : new GameControllerDocument(bytes).Devices.Count > 0;
            if (!valid) { error ??= what + (keyboard ? " has no [entry] binding." : " has no [ctrl] device."); return null; }
        }
        catch (Exception exception) when (exception is FormatException or InvalidDataException or ArgumentException or IndexOutOfRangeException)
        {
            error ??= what + " is not an OMSI " + (keyboard ? "keyboard" : "controller") + " file: " + exception.Message;
            return null;
        }
        return bytes;
    }
}
