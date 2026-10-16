using OmsiLaunch.Api;
using OmsiLaunch.Core;

// LAST MAP STATE (/last): the map's own laststn.osn, never a newest-.osn guess.
internal static class LastMapStateTests
{
    public static void Planned()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Last-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "maps", "Grundorf"));
        Directory.CreateDirectory(Path.Combine(root, "Vehicles"));
        try
        {
            File.WriteAllText(Path.Combine(root, "maps", "Grundorf", "global.cfg"), "[friendlyname]\r\nGrundorf\r\n");
            var missing = Plan(root, @"maps\Grundorf\global.cfg");
            Require(missing.Diagnostics.Any(d => d.Code == "OL_E_SITUATION_NOT_FOUND" && d.Message.Contains("laststn.osn")), "a map without laststn.osn must be refused");
            File.WriteAllText(Path.Combine(root, "maps", "Grundorf", "laststn.osn"), "[map]\r\nmaps\\Grundorf\\global.cfg\r\n");
            var present = Plan(root, @"maps\Grundorf\global.cfg");
            Require(!present.Diagnostics.Any(d => d.Code is "OL_E_SITUATION_NOT_FOUND" or "OL_E_CAPABILITY_UNAVAILABLE"), "a map with laststn.osn must be planned: " + string.Join("; ", present.Diagnostics.Select(d => d.Code)));
            Require(present.ResolvedContent.Any(c => c.Kind == "map"), "the map must be resolved");
            Require(LaunchValidation.Validate(Spec(root, null)).Any(d => d.Code == "OL_E_MAP_NOT_FOUND"), "/last without a map must be refused");
            Console.WriteLine("PASS last-map-state.planned");
        }
        finally { Directory.Delete(root, true); }
    }

    private static SessionPlan Plan(string root, string map) =>
        new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(Spec(root, map)).GetAwaiter().GetResult();

    private static LaunchSpec Spec(string root, string? map)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(root), new(WorldMode.LastMapState, map is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(map), OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new());
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
