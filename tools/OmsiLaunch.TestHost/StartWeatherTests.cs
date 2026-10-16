using OmsiLaunch.Api;
using OmsiLaunch.Core;

// START WEATHER (BI-003): a preset is an installed weather\<name>.owt, an ICAO
// is four letters or digits; the plan requires world.weather.
internal static class StartWeatherTests
{
    public static void Resolved()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Weather-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "weather"));
        Directory.CreateDirectory(Path.Combine(root, "maps", "Grundorf"));
        Directory.CreateDirectory(Path.Combine(root, "Vehicles"));
        try
        {
            File.WriteAllText(Path.Combine(root, "weather", "Regen.owt"), "[name]\r\nRegen\r\n");
            File.WriteAllText(Path.Combine(root, "maps", "Grundorf", "global.cfg"), "[friendlyname]\r\nGrundorf\r\n");

            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set("Regen"), OptionalValue<string>.Unset)), out _) == new StartWeather(WeatherMode.Preset, @"weather\Regen.owt"), "a preset name resolves to its file");
            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set(@"weather\Regen.owt"), OptionalValue<string>.Unset)), out _) == new StartWeather(WeatherMode.Preset, @"weather\Regen.owt"), "a preset path resolves");
            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set("Sturm"), OptionalValue<string>.Unset)), out var missing) is null && missing is not null, "a missing preset is refused");
            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set(@"..\options.cfg"), OptionalValue<string>.Unset)), out var escape) is null && escape is not null, "a preset outside the weather folder is refused");
            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Icao, OptionalValue<string>.Unset, OptionalValue<string>.Set("eddf"))), out _) == new StartWeather(WeatherMode.Icao, Icao: "EDDF"), "an ICAO is normalized");
            Require(StartWeatherResolver.Resolve(Spec(root, new(WeatherMode.Icao, OptionalValue<string>.Unset, OptionalValue<string>.Set("ED-F"))), out var badIcao) is null && badIcao is not null, "an invalid ICAO is refused");

            var planned = Plan(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set("Regen"), OptionalValue<string>.Unset)));
            // Without an Omsi.exe the fixture has no native requirement to record; the
            // weather must simply not be reported as unsupported.
            Require(!planned.UnsupportedRequestedFeatures.Any(c => c.Name == "world.weather"), "start weather must not be unsupported");
            Require(!planned.Diagnostics.Any(d => d.Code is "OL_E_WEATHER_INVALID" or "OL_E_CAPABILITY_UNAVAILABLE"), "a valid preset must be planned: " + string.Join("; ", planned.Diagnostics.Select(d => d.Code)));
            Require(Plan(Spec(root, new(WeatherMode.Preset, OptionalValue<string>.Set("Sturm"), OptionalValue<string>.Unset))).Diagnostics.Any(d => d.Code == "OL_E_WEATHER_INVALID"), "the planner must refuse a missing preset");
            Console.WriteLine("PASS start-weather.resolved");
        }
        finally { Directory.Delete(root, true); }
    }

    private static SessionPlan Plan(LaunchSpec spec) =>
        new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(spec).GetAwaiter().GetResult();

    private static LaunchSpec Spec(string root, WeatherSpec weather)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(0)), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new()) { Weather = weather };
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
