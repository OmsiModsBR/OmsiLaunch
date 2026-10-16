using System.IO.MemoryMappedFiles;
using System.Text;
using System.Text.Json;
using OmsiLaunch.Api;

namespace OmsiLaunch.Plugin;

// Writes plugin events into the session's telemetry ring (TelemetryRingWire).
// Every event keeps its own slot, so a burst (for example the PluginStart
// sequence) reaches the host in order instead of overwriting itself.
internal static class CurrentTelemetrySink
{
    // Plugin callbacks can emit on more than one OMSI callback path; the ring
    // has one writer, so emits are serialized.
    private static readonly object gate = new();
    // Monotonic session clock (milliseconds since the plugin started): orders
    // events independently of wall-clock changes (reserved multiplayer primitive).
    private static readonly System.Diagnostics.Stopwatch clock = System.Diagnostics.Stopwatch.StartNew();
    public static void Emit(string name, IReadOnlyDictionary<string, string> data)
    {
        var mappingName = Environment.GetEnvironmentVariable("OMSILAUNCH_TELEMETRY_NAME");
        if (string.IsNullOrWhiteSpace(mappingName)) return;
        var tick = clock.ElapsedMilliseconds;
        var payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { name, data, tick }));
        // An event larger than a slot is replaced by a marker that names it.
        if (payload.Length > TelemetryRingWire.MaxPayload)
            payload = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { name = "telemetry.oversized", data = new Dictionary<string, string> { ["event"] = name, ["bytes"] = payload.Length.ToString(System.Globalization.CultureInfo.InvariantCulture) }, tick }));
        try
        {
            lock (gate)
            {
                using var mapping = MemoryMappedFile.OpenExisting(mappingName, MemoryMappedFileRights.ReadWrite);
                using var view = mapping.CreateViewAccessor(0, TelemetryRingWire.Capacity, MemoryMappedFileAccess.ReadWrite);
                TelemetryRingWire.Write(view, payload);
            }
        }
        catch (FileNotFoundException) { }
        catch (UnauthorizedAccessException) { }
        catch (IOException) { }
    }
}
