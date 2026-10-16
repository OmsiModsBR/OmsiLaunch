using OmsiLaunch.Api;
using OmsiLaunch.Content;
using OmsiLaunch.Core;

// ENTRYPOINT BY IDENTITY (BI-001): OMSI presents the first record of each
// exact, case-sensitive label; the startup selects it by that label.
internal static class EntrypointIdentityTests
{
    private const string Map = @"maps\Grundorf\global.cfg";

    public static void Resolved()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Entry-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "maps", "Grundorf"));
        Directory.CreateDirectory(Path.Combine(root, "Vehicles"));
        try
        {
            File.WriteAllText(Path.Combine(root, "maps", "Grundorf", "global.cfg"),
                "[friendlyname]\r\nGrundorf\r\n[entrypoints]\r\n3\r\n" + Record("1.000", "Depot") + Record("2.000", "Depot") + Record("3.000", "depot"));
            var catalog = new FileSystemContentCatalog(root);
            var first = catalog.EnumerateEntrypoints(Map).Single(entry => entry.DisplayName == "Depot" && catalog.ResolveEntrypointLabel(Map, entry.Identity).Presented);
            var second = catalog.EnumerateEntrypoints(Map).Single(entry => entry.DisplayName == "Depot" && entry.Identity != first.Identity);

            Require(catalog.ResolveEntrypointLabel(Map, "Depot") == new EntrypointLabel(true, true, "Depot"), "an exact label must resolve to its first record");
            Require(catalog.ResolveEntrypointLabel(Map, "depot") == new EntrypointLabel(true, true, "depot"), "a label differing only by case is its own presented entrypoint");
            Require(catalog.ResolveEntrypointLabel(Map, "DEPOT") == new EntrypointLabel(false, false, null), "labels compare case-sensitively");
            Require(catalog.ResolveEntrypointLabel(Map, second.Identity) == new EntrypointLabel(true, false, "Depot"), "a later record sharing a label is not presented");

            var planned = Plan(root, first.Identity);
            Require(planned.ResolvedContent.Any(content => content.Kind == "entrypoint" && content.DisplayName == "Depot"), "the plan must carry the presented label");
            Require(!planned.Diagnostics.Any(d => d.Code is "OL_E_CAPABILITY_UNAVAILABLE" or "OL_E_ENTRYPOINT_NOT_FOUND" or "OL_E_ENTRYPOINT_NOT_PRESENTED"), "an identity must be planned: " + string.Join("; ", planned.Diagnostics.Select(d => d.Code)));
            Require(planned.RequiredCapabilities.Any(c => c.Name == "world.entrypoint-identity" && c.Available), "the identity capability must be required and available");
            Require(Plan(root, second.Identity).Diagnostics.Any(d => d.Code == "OL_E_ENTRYPOINT_NOT_PRESENTED"), "an unpresented record must be refused");
            Require(Plan(root, "Nowhere").Diagnostics.Any(d => d.Code == "OL_E_ENTRYPOINT_NOT_FOUND"), "an unknown label must be refused");
            Console.WriteLine("PASS entrypoint-identity.resolved");
        }
        finally { Directory.Delete(root, true); }
    }

    // Eleven scalar lines, then the label (OMSI 2.3.004 global.cfg).
    private static string Record(string x, string label) => $"0\r\n0\r\n0\r\n{x}\r\n0.000\r\n0.000\r\n0.000\r\n0.000\r\n0.000\r\n1.000\r\n0\r\n{label}\r\n";

    private static SessionPlan Plan(string root, string entrypoint) =>
        new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(Spec(root, entrypoint)).GetAwaiter().GetResult();

    private static LaunchSpec Spec(string root, string entrypoint)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Set(Map), OptionalValue<string>.Unset, OptionalValue<int>.Unset, OptionalValue<string>.Set(entrypoint)), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new());
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
