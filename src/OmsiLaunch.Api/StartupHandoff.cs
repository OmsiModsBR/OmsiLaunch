using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace OmsiLaunch.Api;

public sealed record StartupHandoff(
    Guid SessionId,
    string BuildProfileId,
    WorldMode WorldMode,
    string MapIdentity,
    int PresentedEntrypointIndex,
    bool HeadlessStart,
    bool PlayerVehicleEnabled,
    DateTimeMode DateMode,
    DateTimeMode TimeMode,
    string EntrypointIdentity = "",
    string SituationIdentity = "",
    IReadOnlyList<StartupModule>? Modules = null,
    bool PluginTrace = false,
    bool NativeTrace = false,
    StartDateTime? Start = null,
    StartWeather? Weather = null);

// The weather a session asks for at start (wire version 7, after the start
// block): an OMSI preset file relative to the installation (Preset), live
// METAR weather for a station (Icao), or live weather for the station OMSI
// already has configured (RealCurrent).
public sealed record StartWeather(WeatherMode Mode, string Preset = "", string Icao = "");

// The start date and time a NEW_MAP session asks for, resolved by the
// controller into OMSI's own representation (wire version 7): year, 1-based
// day of the year, hour, minute, second. A null field keeps OMSI's own value.
public sealed record StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)
{
    public bool IsEmpty => Year is null && DayOfYear is null && Hour is null && Minute is null && Second is null;
}

// A runtime module the controller planned (Beta 4, wire version 5): its
// directory relative to the installation root and the SHA-256 of the
// module.json it evaluated. Settings (wire version 6) are the module's
// effective settings as a JSON object of strings; empty when it has none.
public sealed record StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "");

// Canonical portable wire protocol: little-endian fixed-width fields, UTF-8,
// and SHA-256 payload integrity. It contains no CLR object or pointer layout.
public static class StartupHandoffWire
{
    public const uint Magic = 0x4F4C5348;
    public const ushort Version = 7;
    private const int MaxModuleSettingsBytes = 64 * 1024;
    private const int HeaderSize = 64;
    private const int StartBlockSize = 1 + 5 * 4;
    private const int MaxWeatherPresetBytes = 1024;
    public static byte[] Serialize(StartupHandoff value)
    {
        var profile = Encoding.UTF8.GetBytes(value.BuildProfileId); var map = Encoding.UTF8.GetBytes(value.MapIdentity); var entrypoint = Encoding.UTF8.GetBytes(value.EntrypointIdentity); var situation = Encoding.UTF8.GetBytes(value.SituationIdentity);
        var modules = (value.Modules ?? Array.Empty<StartupModule>()).Select(x => (Directory: Encoding.UTF8.GetBytes(x.RelativeDirectory), Sha: Convert.FromHexString(x.ManifestSha256), Settings: Encoding.UTF8.GetBytes(x.Settings))).ToArray();
        if (modules.Any(x => x.Sha.Length != 32)) throw new ArgumentException("A module manifest SHA-256 must be 32 bytes.", nameof(value));
        if (modules.Any(x => x.Settings.Length > MaxModuleSettingsBytes)) throw new ArgumentException("A module's settings exceed 64 KiB.", nameof(value));
        var weather = value.Weather is { Mode: not WeatherMode.Unset } requested ? requested : null;
        var preset = Encoding.UTF8.GetBytes(weather?.Preset ?? ""); var icao = Encoding.UTF8.GetBytes(weather?.Icao ?? "");
        if (preset.Length > MaxWeatherPresetBytes || icao.Length > 8) throw new ArgumentException("The start weather is too long.", nameof(value));
        var payload = new byte[4 + profile.Length + 4 + map.Length + 4 + entrypoint.Length + 4 + situation.Length + 8 + 4 + modules.Sum(x => 4 + x.Directory.Length + 32 + 4 + x.Settings.Length) + StartBlockSize + 1 + 4 + preset.Length + 4 + icao.Length]; var p = payload.AsSpan();
        BinaryPrimitives.WriteUInt32LittleEndian(p, (uint)profile.Length); profile.CopyTo(p[4..]); var offset = 4 + profile.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)map.Length); map.CopyTo(p[(offset + 4)..]); offset += 4 + map.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)entrypoint.Length); entrypoint.CopyTo(p[(offset + 4)..]); offset += 4 + entrypoint.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)situation.Length); situation.CopyTo(p[(offset + 4)..]); offset += 4 + situation.Length;
        BinaryPrimitives.WriteInt32LittleEndian(p[offset..], value.PresentedEntrypointIndex); offset += 4;
        p[offset++] = (byte)value.WorldMode;
        // Flags: 0x01 headless start, 0x02 player vehicle, 0x04 plugin trace, 0x08 native trace.
        p[offset++] = (byte)((value.HeadlessStart ? 0x01 : 0) | (value.PlayerVehicleEnabled ? 0x02 : 0) | (value.PluginTrace ? 0x04 : 0) | (value.NativeTrace ? 0x08 : 0));
        p[offset++] = (byte)value.DateMode;
        p[offset++] = (byte)value.TimeMode;
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)modules.Length); offset += 4;
        foreach (var module in modules)
        {
            BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)module.Directory.Length); module.Directory.CopyTo(p[(offset + 4)..]); offset += 4 + module.Directory.Length;
            module.Sha.CopyTo(p[offset..]); offset += 32;
            BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)module.Settings.Length); module.Settings.CopyTo(p[(offset + 4)..]); offset += 4 + module.Settings.Length;
        }
        // Version 7: start date/time. Presence bits 0x01 year, 0x02 day of year,
        // 0x04 hour, 0x08 minute, 0x10 second; then year, day, hour, minute, second as int32.
        var start = value.Start;
        p[offset++] = (byte)((start?.Year is not null ? 0x01 : 0) | (start?.DayOfYear is not null ? 0x02 : 0) | (start?.Hour is not null ? 0x04 : 0) | (start?.Minute is not null ? 0x08 : 0) | (start?.Second is not null ? 0x10 : 0));
        foreach (var field in new[] { start?.Year, start?.DayOfYear, start?.Hour, start?.Minute, start?.Second }) { BinaryPrimitives.WriteInt32LittleEndian(p[offset..], field ?? 0); offset += 4; }
        // Then the start weather: mode byte, preset and ICAO as length-prefixed UTF-8.
        p[offset++] = (byte)(weather?.Mode ?? WeatherMode.Unset);
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)preset.Length); preset.CopyTo(p[(offset + 4)..]); offset += 4 + preset.Length;
        BinaryPrimitives.WriteUInt32LittleEndian(p[offset..], (uint)icao.Length); icao.CopyTo(p[(offset + 4)..]); offset += 4 + icao.Length;
        var output = new byte[HeaderSize + payload.Length]; var h = output.AsSpan(); BinaryPrimitives.WriteUInt32LittleEndian(h, Magic); BinaryPrimitives.WriteUInt16LittleEndian(h[4..], Version); BinaryPrimitives.WriteUInt16LittleEndian(h[6..], HeaderSize); BinaryPrimitives.WriteUInt32LittleEndian(h[8..], (uint)output.Length); value.SessionId.TryWriteBytes(h[12..28]); BinaryPrimitives.WriteUInt32LittleEndian(h[28..], (uint)payload.Length); SHA256.HashData(payload, h[32..64]); payload.CopyTo(h[64..]); return output;
    }
    public static bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff? value)
    {
        value = null; if (bytes.Length < HeaderSize || BinaryPrimitives.ReadUInt32LittleEndian(bytes) != Magic || BinaryPrimitives.ReadUInt16LittleEndian(bytes[6..]) != HeaderSize || BinaryPrimitives.ReadUInt32LittleEndian(bytes[8..]) != bytes.Length) return false;
        var version = BinaryPrimitives.ReadUInt16LittleEndian(bytes[4..]); if (version is < 3 or > Version) return false;
        var payloadSize = BinaryPrimitives.ReadUInt32LittleEndian(bytes[28..]); if (payloadSize != bytes.Length - HeaderSize) return false; var payload = bytes[HeaderSize..]; if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(payload), bytes[32..64]) || payload.Length < 20) return false;
        var profileLength = BinaryPrimitives.ReadUInt32LittleEndian(payload); if (profileLength > payload.Length - 20) return false; var offset = 4 + checked((int)profileLength); var mapLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); if (mapLength > payload.Length - offset - 16) return false;
        var profile = Encoding.UTF8.GetString(payload.Slice(4, checked((int)profileLength))); var map = Encoding.UTF8.GetString(payload.Slice(offset + 4, checked((int)mapLength))); offset += 4 + checked((int)mapLength);
        var entrypointLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); if (entrypointLength > payload.Length - offset - (version == 3 ? 12 : 16)) return false;
        var entrypoint = Encoding.UTF8.GetString(payload.Slice(offset + 4, checked((int)entrypointLength))); offset += 4 + checked((int)entrypointLength);
        var situation = string.Empty;
        if (version >= 4)
        {
            var situationLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); if (situationLength > payload.Length - offset - 12) return false;
            situation = Encoding.UTF8.GetString(payload.Slice(offset + 4, checked((int)situationLength))); offset += 4 + checked((int)situationLength);
        }
        var index = BinaryPrimitives.ReadInt32LittleEndian(payload[offset..]); offset += 4;
        var worldMode = (WorldMode)payload[offset++]; var flags = payload[offset++]; var dateMode = (DateTimeMode)payload[offset++]; var timeMode = (DateTimeMode)payload[offset++];
        if (!Enum.IsDefined(typeof(WorldMode), worldMode) || !Enum.IsDefined(typeof(DateTimeMode), dateMode) || !Enum.IsDefined(typeof(DateTimeMode), timeMode)) return false;
        List<StartupModule>? modules = null;
        if (version >= 5)
        {
            if (payload.Length - offset < 4) return false;
            var count = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); offset += 4;
            if (count > 64) return false;
            for (var moduleIndex = 0; moduleIndex < count; moduleIndex++)
            {
                if (payload.Length - offset < 4) return false;
                var length = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); offset += 4;
                if (length == 0 || length > 512 || length > payload.Length - offset - 32) return false;
                var directory = Encoding.UTF8.GetString(payload.Slice(offset, (int)length)); offset += (int)length;
                var sha = Convert.ToHexString(payload.Slice(offset, 32)); offset += 32;
                var settings = string.Empty;
                if (version >= 6)
                {
                    if (payload.Length - offset < 4) return false;
                    var settingsLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); offset += 4;
                    if (settingsLength > MaxModuleSettingsBytes || settingsLength > payload.Length - offset) return false;
                    settings = Encoding.UTF8.GetString(payload.Slice(offset, (int)settingsLength)); offset += (int)settingsLength;
                }
                (modules ??= new()).Add(new StartupModule(directory, sha, settings));
            }
        }
        StartDateTime? startValue = null;
        StartWeather? weatherValue = null;
        if (version >= 7)
        {
            if (payload.Length - offset < StartBlockSize + 9) return false;
            var present = payload[offset++];
            var fields = new int?[5];
            for (var field = 0; field < 5; field++) { var number = BinaryPrimitives.ReadInt32LittleEndian(payload[offset..]); offset += 4; if ((present & (1 << field)) != 0) fields[field] = number; }
            if ((present & ~0x1F) != 0) return false;
            if (fields[0] is < 1 or > 9999 || fields[1] is < 1 or > 366 || fields[2] is < 0 or > 23 || fields[3] is < 0 or > 59 || fields[4] is < 0 or > 59) return false;
            startValue = new StartDateTime(fields[0], fields[1], fields[2], fields[3], fields[4]);
            if (startValue.IsEmpty) startValue = null;
            var weatherMode = (WeatherMode)payload[offset++];
            if (!Enum.IsDefined(typeof(WeatherMode), weatherMode)) return false;
            var presetLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); offset += 4;
            if (presetLength > MaxWeatherPresetBytes || presetLength > payload.Length - offset - 4) return false;
            var preset = Encoding.UTF8.GetString(payload.Slice(offset, (int)presetLength)); offset += (int)presetLength;
            var icaoLength = BinaryPrimitives.ReadUInt32LittleEndian(payload[offset..]); offset += 4;
            if (icaoLength > 8 || icaoLength > payload.Length - offset) return false;
            var icao = Encoding.UTF8.GetString(payload.Slice(offset, (int)icaoLength)); offset += (int)icaoLength;
            if (weatherMode != WeatherMode.Unset) weatherValue = new StartWeather(weatherMode, preset, icao);
        }
        if (offset != payload.Length) return false;
        value = new StartupHandoff(new Guid(bytes.Slice(12,16)), profile, worldMode, map, index, (flags & 0x01) != 0, (flags & 0x02) != 0, dateMode, timeMode, entrypoint, situation, modules, (flags & 0x04) != 0, (flags & 0x08) != 0, startValue, weatherValue); return true;
    }
}
