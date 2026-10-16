using OmsiLaunch.Api;

namespace OmsiLaunch.Core;

// Resolves LaunchSpec.Weather into the startup handoff's StartWeather: a
// preset becomes the installed weather\<name>.owt path OMSI loads (relative to
// the installation, as OMSI's own calls pass it); an ICAO is four letters or
// digits. Null (with error) when the request cannot be applied; null without
// error when no weather is requested.
public static class StartWeatherResolver
{
    public static StartWeather? Resolve(LaunchSpec spec, out string? error)
    {
        error = null;
        var weather = spec.EffectiveWeather;
        switch (weather.Mode)
        {
            case WeatherMode.Unset:
                return null;
            case WeatherMode.Preset:
                var name = weather.Preset.IsSet ? weather.Preset.Value!.Trim() : "";
                if (name.Length == 0) { error = "a preset name is required"; return null; }
                if (name.StartsWith("weather\\", StringComparison.OrdinalIgnoreCase) || name.StartsWith("weather/", StringComparison.OrdinalIgnoreCase)) name = name[8..];
                if (!name.EndsWith(".owt", StringComparison.OrdinalIgnoreCase)) name += ".owt";
                if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.Length > 200) { error = "the preset must be a file name in the weather folder"; return null; }
                var file = Path.Combine(spec.Installation.RootPath, "weather", name);
                if (!File.Exists(file)) { error = "no weather preset weather\\" + name; return null; }
                return new StartWeather(WeatherMode.Preset, "weather\\" + Path.GetFileName(file));
            case WeatherMode.Icao:
                var icao = weather.Icao.IsSet ? weather.Icao.Value!.Trim().ToUpperInvariant() : "";
                if (icao.Length != 4 || !icao.All(character => character is >= 'A' and <= 'Z' or >= '0' and <= '9')) { error = "an ICAO code is four letters or digits"; return null; }
                return new StartWeather(WeatherMode.Icao, Icao: icao);
            case WeatherMode.RealCurrent:
                return new StartWeather(WeatherMode.RealCurrent);
            default:
                error = "unknown weather mode";
                return null;
        }
    }
}
