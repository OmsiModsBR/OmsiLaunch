using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text.Json;
using OmsiLaunch.Api;

namespace OmsiLaunch.Process;

public sealed record RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size);

// The product-owned plugin closure. Expected hashes come from the release
// manifest when the controller runs from an installed package; without a
// manifest (development layout) the reference closure is the build output.
public sealed class RuntimeArtifactSet
{
    public IReadOnlyList<RuntimeArtifact> Artifacts { get; }
    public IReadOnlyDictionary<string, string>? ExpectedHashes { get; }
    public string IntegrityReference => ExpectedHashes is null ? "self" : "manifest";
    private RuntimeArtifactSet(IReadOnlyList<RuntimeArtifact> artifacts, IReadOnlyDictionary<string, string>? expectedHashes) { Artifacts = artifacts; ExpectedHashes = expectedHashes; }

    public static RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string>? expectedHashes = null)
    {
        if (!Directory.Exists(pluginBuildDirectory)) throw new DirectoryNotFoundException(pluginBuildDirectory);
        // CopyLocalLockFileAssemblies places the managed closure beside the
        // plugin. Stage only our named product assemblies plus DNNE metadata;
        // never sweep arbitrary DLLs from a build directory into OMSI.
        var required = new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json" }
            .Select(file => Path.Combine(pluginBuildDirectory, file));
        var managedClosure = Directory.EnumerateFiles(pluginBuildDirectory, "OmsiLaunch.*.dll", SearchOption.TopDirectoryOnly)
            .Where(path => !string.Equals(Path.GetFileName(path), "OmsiLaunch.PluginNE.dll", StringComparison.OrdinalIgnoreCase));
        var sources = required.Concat(managedClosure).Append(nativeBuildPath).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        var artifacts = sources.Select(source =>
        {
            if (!File.Exists(source)) throw new FileNotFoundException("Required OmsiLaunch runtime artifact is missing.", source);
            var info = new FileInfo(source); return new RuntimeArtifact(source, Path.Combine("plugins", info.Name), Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(source))), info.Length);
        }).ToArray();
        if (!artifacts.Any(x => string.Equals(Path.GetFileName(x.SourcePath), "OmsiLaunch.Plugin.dll", StringComparison.OrdinalIgnoreCase)))
            throw new FileNotFoundException("The plugin assembly is not present in the managed dependency closure.", Path.Combine(pluginBuildDirectory, "OmsiLaunch.Plugin.dll"));
        return new RuntimeArtifactSet(artifacts, expectedHashes is null ? null : new Dictionary<string, string>(expectedHashes, StringComparer.OrdinalIgnoreCase));
    }

    // These are permanent product files under plugins\. A session validates
    // them, but never stages, snapshots, restores, or removes them. With a
    // manifest, every installed file must match the packaged hash; without
    // one, validation can only compare against the reference closure.
    public void ValidateInstalled(string installationRoot)
    {
        // Every plugin file the manifest promises must be installed, not only
        // the files that happen to be present in the closure directory.
        // Files below plugins\<folder>\ (runtime modules) are not part of the
        // plugin closure; the module catalog verifies them.
        if (ExpectedHashes is not null)
            foreach (var expectedPath in ExpectedHashes.Keys.Where(key => key.Count(c => c == '/') == 1))
                if (!Artifacts.Any(artifact => string.Equals(NormalizeKey(artifact.DestinationRelativePath), expectedPath, StringComparison.OrdinalIgnoreCase)))
                    throw new FileNotFoundException("OL_E_PERMANENT_PLUGIN_MISSING", Path.Combine(installationRoot, expectedPath.Replace('/', '\\')));
        foreach (var artifact in Artifacts)
        {
            var destination = Path.Combine(installationRoot, artifact.DestinationRelativePath);
            if (!File.Exists(destination)) throw new FileNotFoundException("OL_E_PERMANENT_PLUGIN_MISSING", destination);
            var expected = artifact.Sha256;
            if (ExpectedHashes is not null && !ExpectedHashes.TryGetValue(NormalizeKey(artifact.DestinationRelativePath), out expected))
                throw new InvalidDataException("OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE: " + artifact.DestinationRelativePath);
            var hash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination)));
            if (!string.Equals(hash, expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("OL_E_PERMANENT_PLUGIN_HASH_MISMATCH: " + artifact.DestinationRelativePath + " (reinstall the OmsiLaunch package so plugins\\ and release-manifest.json agree)");
        }
    }

    internal static string NormalizeKey(string relativePath) => relativePath.Replace('\\', '/');
}

// Reads the plugin closure hashes recorded by New-ReleasePackage.ps1. Only the
// plugins/ entries are consumed; the manifest is data, never executable policy.
public static class ReleaseManifest
{
    public const string FileName = "release-manifest.json";

    public static IReadOnlyDictionary<string, string>? TryReadPluginHashes(string manifestPath)
    {
        if (!File.Exists(manifestPath)) return null;
        // Windows PowerShell 5.1 writes UTF-8 with a BOM; the byte-span JSON
        // reader rejects it. The manifest is accepted with or without one.
        return ParsePluginHashes(File.ReadAllBytes(manifestPath));
    }

    // Strict reading. Every entry of the manifest is validated, not only the
    // plugin entries the controller consumes: a manifest that is ambiguous or
    // malformed anywhere is not trusted anywhere.
    public static IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)
    {
        var content = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF ? bytes.AsMemory(3) : bytes.AsMemory();
        if (content.Span.Trim((byte)' ').IsEmpty) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: empty");
        JsonDocument document;
        try { document = JsonDocument.Parse(content); }
        catch (JsonException exception) { throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: not JSON (" + exception.Message + ")"); }
        using (document)
        {
            if (document.RootElement.ValueKind != JsonValueKind.Object || !document.RootElement.TryGetProperty("files", out var files) || files.ValueKind != JsonValueKind.Array) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: files");
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var hashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in files.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: entry");
                var path = entry.TryGetProperty("path", out var pathValue) && pathValue.ValueKind == JsonValueKind.String ? pathValue.GetString() : null;
                var sha256 = entry.TryGetProperty("sha256", out var hashValue) && hashValue.ValueKind == JsonValueKind.String ? hashValue.GetString() : null;
                if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(sha256)) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: entry");
                if (sha256.Length != 64 || !sha256.All(Uri.IsHexDigit)) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: hash " + path);
                var key = RuntimeArtifactSet.NormalizeKey(path);
                var segments = key.Split('/');
                if (key.StartsWith('/') || key.Contains(':') || Path.IsPathRooted(path) || segments.Any(segment => segment.Length == 0 || segment == "." || segment == ".."))
                    throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: path " + path);
                // Windows paths are case-insensitive: two spellings of one file
                // would make the expected hash ambiguous.
                if (!seen.Add(key)) throw new InvalidDataException("OL_E_RELEASE_MANIFEST_INVALID: duplicate " + path);
                if (key.StartsWith("plugins/", StringComparison.OrdinalIgnoreCase)) hashes[key] = sha256;
            }
            return hashes;
        }
    }
}

public sealed class CurrentStartupHandoffStore : IDisposable
{
    private readonly MemoryMappedFile mapping;
    public string Name { get; }
    public Guid SessionId { get; }
    private CurrentStartupHandoffStore(MemoryMappedFile mapping, string name, Guid sessionId) { this.mapping = mapping; Name = name; SessionId = sessionId; }

    public static CurrentStartupHandoffStore Create(StartupHandoff handoff)
    {
        var bytes = StartupHandoffWire.Serialize(handoff); var name = "OmsiLaunch.Handoff." + handoff.SessionId.ToString("N");
        var mapping = MemoryMappedFile.CreateNew(name, bytes.Length, MemoryMappedFileAccess.ReadWrite);
        using var view = mapping.CreateViewAccessor(0, bytes.Length, MemoryMappedFileAccess.Write); view.WriteArray(0, bytes, 0, bytes.Length); view.Flush();
        return new CurrentStartupHandoffStore(mapping, name, handoff.SessionId);
    }
    public void Dispose() => mapping.Dispose();
}

public sealed class CurrentTelemetryStore : IDisposable
{
    private readonly MemoryMappedFile mapping;
    private long lastSequence;
    public string Name { get; }
    private CurrentTelemetryStore(MemoryMappedFile mapping, string name) { this.mapping = mapping; Name = name; }
    public static CurrentTelemetryStore Create(Guid sessionId)
    {
        var name = "OmsiLaunch.Telemetry." + sessionId.ToString("N");
        var mapping = MemoryMappedFile.CreateNew(name, TelemetryRingWire.Capacity, MemoryMappedFileAccess.ReadWrite);
        using (var view = mapping.CreateViewAccessor(0, TelemetryRingWire.Capacity, MemoryMappedFileAccess.ReadWrite)) TelemetryRingWire.Initialize(view);
        return new CurrentTelemetryStore(mapping, name);
    }
    // Every event committed since the previous call, oldest first, and how
    // many were overwritten before they could be read.
    public (IReadOnlyList<(long Sequence, string Payload)> Events, long Lost) ReadNew()
    {
        using var view = mapping.CreateViewAccessor(0, TelemetryRingWire.Capacity, MemoryMappedFileAccess.Read);
        var (events, last, lost) = TelemetryRingWire.ReadSince(view, lastSequence);
        lastSequence = last;
        return (events, lost);
    }
    public void Dispose() => mapping.Dispose();
}

// Single-flight session mailbox. It has no global discovery, binds every
// request to the handoff session, and is disposed with the session transaction.
public sealed class CurrentRuntimeCommandStore : IDisposable
{
    private const int Capacity = 65_536;
    private const int StateOffset = 0;
    private const int LengthOffset = 4;
    private const int DataOffset = 8;
    private const int StateIdle = 0;
    private const int StateRequested = 1;
    private const int StateResponded = 2;
    private readonly MemoryMappedFile mapping;
    private readonly SemaphoreSlim gate = new(1, 1);
    public string Name { get; }
    public Guid SessionId { get; }
    private CurrentRuntimeCommandStore(MemoryMappedFile mapping, string name, Guid sessionId) { this.mapping = mapping; Name = name; SessionId = sessionId; }
    public static CurrentRuntimeCommandStore Create(Guid sessionId)
    {
        var name = "OmsiLaunch.Runtime." + sessionId.ToString("N");
        return new CurrentRuntimeCommandStore(MemoryMappedFile.CreateNew(name, Capacity, MemoryMappedFileAccess.ReadWrite), name, sessionId);
    }
    public async Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        if (command.SessionId != SessionId) throw new InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH");
        var request = RuntimeCommandWire.SerializeRequest(command); if (request.Length > Capacity - DataOffset) throw new ArgumentOutOfRangeException(nameof(command));
        await gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        MemoryMappedViewAccessor? view = null;
        try
        {
            view = mapping.CreateViewAccessor(0, Capacity, MemoryMappedFileAccess.ReadWrite);
            var state = view.ReadInt32(StateOffset);
            if (state == StateResponded || state == StateRequested)
            {
                // Leftover from an earlier request: a response that arrived after
                // its request ended, or a request the plugin never answered. It
                // is cleared before anything else so it can never block the
                // channel or be read as the answer to this request.
                var stale = TryReadSlot(view);
                ResetSlot(view);
                if (state == StateResponded && stale is not null && RuntimeCommandWire.TryReadRequestId(stale, out var staleId) && staleId == command.RequestId) throw new InvalidOperationException("OL_E_RUNTIME_REQUEST_ID_REUSED");
            }
            else if (state != StateIdle) { ResetSlot(view); throw new InvalidDataException("OL_E_RUNTIME_CHANNEL_STATE_INVALID"); }
            view.Write(LengthOffset, request.Length); view.WriteArray(DataOffset, request, 0, request.Length); view.Write(StateOffset, StateRequested); view.Flush();
            var deadline = DateTimeOffset.UtcNow + timeout;
            while (view.ReadInt32(StateOffset) != StateResponded)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (DateTimeOffset.UtcNow >= deadline) throw new TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT");
                await Task.Delay(20, cancellationToken).ConfigureAwait(false);
            }
            var response = TryReadSlot(view);
            if (response is null) throw new InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID");
            if (!RuntimeCommandWire.TryDeserializeResponse(response, out var result) || result is null || result.SessionId != SessionId || result.RequestId != command.RequestId) throw new InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID");
            return result;
        }
        finally
        {
            // Single cleanup point for every terminal path (success, typed
            // error, malformed/oversized/foreign response, timeout, cancellation,
            // decode exception): the slot returns to Idle with no length and no
            // envelope header left that a later request could interpret. A
            // plugin still executing an abandoned request sees the slot is no
            // longer its request and does not publish.
            if (view is not null) { try { ResetSlot(view); } catch (ObjectDisposedException) { } view.Dispose(); }
            gate.Release();
        }
    }
    private static void ResetSlot(MemoryMappedViewAccessor view)
    {
        view.Write(StateOffset, StateIdle);
        view.Write(LengthOffset, 0);
        view.WriteArray(DataOffset, new byte[RuntimeCommandWire.HeaderSize], 0, RuntimeCommandWire.HeaderSize);
        view.Flush();
    }
    private static byte[]? TryReadSlot(MemoryMappedViewAccessor view)
    {
        var length = view.ReadInt32(LengthOffset); if (length <= 0 || length > Capacity - DataOffset) return null;
        var bytes = new byte[length]; view.ReadArray(DataOffset, bytes, 0, length); return bytes;
    }
    public void Dispose() { gate.Dispose(); mapping.Dispose(); }
}
