using System.Globalization;

namespace OmsiLaunch.Interop;

// Profile-backed weather writer. It writes TWeather.actWeather, the source
// record OMSI's weather tick derives every per-frame value from (research
// report beta4-static weather-and-delphi-strings.md section 2.3); the derived
// projections themselves are never written. The cloud type (an AnsiString)
// and the METAR ICAO are assigned by the native boundary.
public sealed class OmsiWeatherWriter
{
    internal enum FieldKind { Single, Byte, Boolean }
    internal sealed record Field(string Layout, FieldKind Kind, float Minimum, float Maximum);

    // Public key -> source field. Ranges keep values inside what OMSI's own
    // weather dialog and .owt files produce.
    private static readonly IReadOnlyDictionary<string, Field> Fields = new Dictionary<string, Field>(StringComparer.Ordinal)
    {
        ["fog_density"] = new("ActiveFogDensity", FieldKind.Single, 0, 100_000),
        ["lightness"] = new("ActiveLightness", FieldKind.Single, 0, 1),
        ["wind_speed"] = new("ActiveWindSpeed", FieldKind.Single, 0, 200),
        ["wind_direction"] = new("ActiveWindDirection", FieldKind.Single, 0, 360),
        ["temperature"] = new("ActiveTemperature", FieldKind.Single, -60, 60),
        ["dew_point"] = new("ActiveDewPoint", FieldKind.Single, -60, 60),
        ["pressure"] = new("ActivePressure", FieldKind.Single, 850, 1100),
        ["precipitation"] = new("ActivePrecipitation", FieldKind.Byte, 0, 2),
        ["precipitation_rate"] = new("ActivePrecipitationRate", FieldKind.Byte, 0, 255),
        ["cloud_height"] = new("ActiveCloudHeight", FieldKind.Single, 0, 20_000),
        ["ground_moisture"] = new("ActiveGroundMoisture", FieldKind.Single, 0, 1),
        ["ground_moisture_cover"] = new("ActiveGroundMoistureCover", FieldKind.Single, 0, 1),
        ["ground_wetness"] = new("ActiveGroundWetness", FieldKind.Single, 0, 1),
        ["snow"] = new("ActiveSnow", FieldKind.Boolean, 0, 1),
        ["snow_on_roads"] = new("ActiveSnowOnRoads", FieldKind.Boolean, 0, 1),
    };

    // OMSI recomputes these from the source record on every weather tick.
    private static readonly IReadOnlyDictionary<string, string> Derived = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["relative_humidity"] = "dew_point", ["absolute_humidity"] = "dew_point",
        ["primary_light_factor"] = "lightness", ["secondary_light_factor"] = "lightness", ["ambient_light_factor"] = "lightness",
        ["cloud_transparency"] = "cloud_type", ["precipitation_set"] = "precipitation", ["wet_ground"] = "ground_wetness",
    };

    public sealed class PreparedWeather
    {
        internal PreparedWeather(IReadOnlyList<(Field Field, float Value)> values) => Values = values;
        internal IReadOnlyList<(Field Field, float Value)> Values { get; }
        public int Count => Values.Count;
    }

    private readonly IOmsiMemory memory;
    private readonly IReadOnlyDictionary<string, uint> globals;
    private readonly IReadOnlyDictionary<string, uint> layout;
    private readonly IReadOnlyDictionary<string, uint> actualLayout;

    public OmsiWeatherWriter(IOmsiMemory memory, IReadOnlyDictionary<string, uint> globals, IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> layouts)
    {
        this.memory = memory;
        this.globals = globals;
        layout = layouts.TryGetValue("Weather", out var value) ? value : throw new InvalidOperationException("Missing profiled weather layout.");
        actualLayout = layouts.TryGetValue("ActualWeather", out var actual) ? actual : throw new InvalidOperationException("Missing profiled actual-weather layout.");
    }

    // Validates every value before anything is written. An empty set is
    // allowed (a request that only changes the cloud type).
    public PreparedWeather Prepare(IReadOnlyDictionary<string, string> values)
    {
        var prepared = new List<(Field, float)>();
        foreach (var pair in values)
        {
            if (Derived.TryGetValue(pair.Key, out var source))
                throw new InvalidOperationException($"OL_E_RUNTIME_SETTING_NOT_PERSISTENT: {pair.Key} is derived by OMSI on every weather tick; set {source} instead.");
            if (!Fields.TryGetValue(pair.Key, out var field)) throw new InvalidOperationException("OL_E_RUNTIME_SETTING_UNAVAILABLE: " + pair.Key);
            prepared.Add((field, Parse(field, pair.Key, pair.Value)));
        }
        return new PreparedWeather(prepared);
    }

    public async ValueTask WriteAsync(PreparedWeather values, CancellationToken cancellationToken = default)
    {
        var address = await InstanceAsync("Weather", cancellationToken).ConfigureAwait(false);
        await StopMetarAsync(cancellationToken).ConfigureAwait(false);
        foreach (var (field, value) in values.Values)
        {
            var target = address + layout[field.Layout];
            switch (field.Kind)
            {
                case FieldKind.Single: await memory.WriteValueAsync(target, value, cancellationToken).ConfigureAwait(false); break;
                case FieldKind.Byte: await memory.WriteValueAsync(target, (byte)value, cancellationToken).ConfigureAwait(false); break;
                default: await memory.WriteValueAsync(target, value != 0, cancellationToken).ConfigureAwait(false); break;
            }
        }
    }

    // After the native boundary assigned the ICAO: the flags OMSI's weather
    // dialog sets (Tform_weather.Button1Click), so the next update downloads.
    public async ValueTask RequestMetarAsync(CancellationToken cancellationToken = default)
    {
        var address = await InstanceAsync("ActualWeather", cancellationToken).ConfigureAwait(false);
        await memory.WriteValueAsync(address + actualLayout["InvalidIcao"], false, cancellationToken).ConfigureAwait(false);
        await memory.WriteValueAsync(address + actualLayout["LastDownloaded"], 0d, cancellationToken).ConfigureAwait(false);
        await memory.WriteValueAsync(address + actualLayout["Process"], (byte)0, cancellationToken).ConfigureAwait(false);
        await memory.WriteValueAsync(address + actualLayout["Active"], true, cancellationToken).ConfigureAwait(false);
    }

    // A live METAR download replaces the whole record every few minutes.
    private async ValueTask StopMetarAsync(CancellationToken cancellationToken)
    {
        var address = await InstanceAsync("ActualWeather", cancellationToken).ConfigureAwait(false);
        await memory.WriteValueAsync(address + actualLayout["Active"], false, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<uint> InstanceAsync(string global, CancellationToken cancellationToken)
    {
        if (!globals.TryGetValue(global, out var cell)) throw new InvalidOperationException("Missing profiled " + global + " global.");
        var address = await memory.ReadPointer32Async(cell, cancellationToken).ConfigureAwait(false);
        return address != 0 ? address : throw new InvalidOperationException("OL_E_RUNTIME_OPERATION_UNAVAILABLE: OMSI's " + global + " object is unavailable.");
    }

    private static float Parse(Field field, string key, string raw)
    {
        if (field.Kind == FieldKind.Boolean)
            return raw.Equals("true", StringComparison.OrdinalIgnoreCase) || raw == "1" ? 1
                : raw.Equals("false", StringComparison.OrdinalIgnoreCase) || raw == "0" ? 0
                : throw new ArgumentException("OL_E_RUNTIME_VALUE_INVALID", key);
        if (field.Kind == FieldKind.Byte)
            return byte.TryParse(raw, NumberStyles.None, CultureInfo.InvariantCulture, out var whole) && whole >= field.Minimum && whole <= field.Maximum
                ? whole
                : throw new ArgumentOutOfRangeException(key, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        if (!float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) || float.IsNaN(value) || float.IsInfinity(value) || value < field.Minimum || value > field.Maximum)
            throw new ArgumentOutOfRangeException(key, "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
        return value;
    }
}
