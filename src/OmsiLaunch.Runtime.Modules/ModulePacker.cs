using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// One file of a module the packer looked at: the SHA-256 the manifest had (null
// when it had none), the SHA-256 the file has now and whether they differ.
public sealed record ModulePackFile(string Path, string? Previous, string Current)
{
    // The manifest has no pin for the file, or a different one.
    public bool NeedsWrite => !string.Equals(Previous, Current, StringComparison.OrdinalIgnoreCase);
    // The manifest pins the file and the pin is wrong (a file without a pin is not a mismatch).
    public bool Mismatch => Previous is not null && NeedsWrite;
}

// The result of packing or verifying a module directory. Written is true when
// module.json was rewritten.
public sealed record ModulePackResult(string ModuleId, IReadOnlyList<ModulePackFile> Files, bool Written)
{
    // Every file has the pin the manifest names for it (or no pin at all).
    public bool Intact => Files.All(x => !x.Mismatch);
    // module.json already holds the current SHA-256 of every file.
    public bool UpToDate => Files.All(x => !x.NeedsWrite);
}

// The authoring helper for community modules (bridge protocol 3): writes the
// pins for the files a module.json names (its components and assets) so an
// author never edits a SHA-256 by hand. Pins are optional and only for
// whoever wants OmsiLaunch to catch a corrupted or half-updated module; the
// plan-time snapshot protects every module whether it is packed or not.
// Packing rewrites only the sha256 values (the rest of the manifest, including
// keys this build does not know, is kept) and re-reads the result with the
// manifest parser before it writes anything.
public static class ModulePacker
{
    private static readonly JsonSerializerOptions Output = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static ModulePackResult Pack(string directory, bool write = true)
    {
        var manifestPath = Path.Combine(directory, RuntimeModuleProtocol.ManifestFileName);
        if (!File.Exists(manifestPath)) throw new ModuleException(PublicErrorCodes.MODULE_MANIFEST_INVALID, "module.json is missing");
        var bytes = File.ReadAllBytes(manifestPath);
        // Same parser the bridge uses: a manifest that does not parse is not packed.
        var manifest = ModuleManifestParser.Parse(bytes);
        var start = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? 3 : 0;
        var document = JsonNode.Parse(bytes.AsSpan(start).ToArray(), documentOptions: new JsonDocumentOptions { MaxDepth = 16 }) as JsonObject
            ?? throw new ModuleException(PublicErrorCodes.MODULE_MANIFEST_INVALID, "the root is not an object");
        var root = Path.GetFullPath(directory).TrimEnd('\\') + "\\";
        var files = new List<ModulePackFile>();
        foreach (var key in new[] { "components", "assets" })
        {
            if (document[key] is not JsonArray array) continue;
            foreach (var node in array)
            {
                if (node is not JsonObject entry || entry["path"] is not JsonValue pathValue || pathValue.GetValue<string>() is not { } relative) continue;
                var full = Path.GetFullPath(Path.Combine(directory, relative.Replace('/', '\\')));
                if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new ModuleException(PublicErrorCodes.MODULE_INTEGRITY, relative + " escapes the module directory");
                if (!File.Exists(full)) throw new ModuleException(PublicErrorCodes.MODULE_INTEGRITY, relative + " is missing");
                for (var current = full; current.Length > root.Length; current = Path.GetDirectoryName(current)!)
                    if (ModuleCatalog.IsReparsePoint(current)) throw new ModuleException(PublicErrorCodes.MODULE_INTEGRITY, relative + " is reached through a reparse point");
                string actual;
                using (var stream = File.OpenRead(full)) actual = Convert.ToHexString(ModuleCatalog.HashStream(stream));
                var previous = entry["sha256"] is JsonValue existing && existing.TryGetValue<string>(out var text) ? text : null;
                files.Add(new(relative.Replace('/', '\\'), previous, actual));
                entry["sha256"] = actual;
            }
        }
        var rewritten = JsonSerializer.SerializeToUtf8Bytes(document, Output);
        // The packed manifest must still be a valid one and describe the same module.
        var check = ModuleManifestParser.Parse(rewritten);
        if (check.Id != manifest.Id) throw new ModuleException(PublicErrorCodes.MODULE_MANIFEST_INVALID, "packing changed the module identity");
        var changed = files.Any(x => x.NeedsWrite);
        if (write && changed) File.WriteAllBytes(manifestPath, rewritten);
        return new(manifest.Id, files, write && changed);
    }

    // Reads the same files and reports whether every pin the manifest has
    // matches; nothing is written. A file without a pin reports Previous null.
    public static ModulePackResult Verify(string directory) => Pack(directory, write: false);
}
