using OmsiLaunch.Api;
using OmsiLaunch.Core;

// LaunchSpec.Input: keyboard and controller files replace Inputs\keyboard.cfg
// and Inputs\gamectrler.cfg for the session as transaction overlays.
internal static class InputDocumentTests
{
    public static void PlannedAsOverlays()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Input-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "Inputs"));
        try
        {
            File.WriteAllText(Path.Combine(root, "keyboard-session.cfg"), "[entry]\r\nquicksave\r\n31\r\n4\r\n");
            File.WriteAllText(Path.Combine(root, "controller-session.cfg"), "[ctrl]\r\nWheel\r\n1\r\n[FFScale]\r\n1.000\r\n1.000\r\n");
            File.WriteAllText(Path.Combine(root, "not-keyboard.cfg"), "[options]\r\nnothing\r\n");
            var plan = Plan(root, "keyboard-session.cfg", Path.Combine(root, "controller-session.cfg"));
            Require(plan.RequiredCapabilities.Any(c => c.Name == "input.keyboard" && c.Available) && plan.RequiredCapabilities.Any(c => c.Name == "input.controller" && c.Available), "both input documents must be planned");
            Require(plan.TouchedFiles.Contains(@"Inputs\keyboard.cfg", StringComparer.OrdinalIgnoreCase) && plan.TouchedFiles.Contains(@"Inputs\gamectrler.cfg", StringComparer.OrdinalIgnoreCase), "the input files must be session-owned");
            Require(!plan.Diagnostics.Any(d => d.Code == "OL_E_INVALID_ARGUMENT" || d.Code == "OL_E_CAPABILITY_UNAVAILABLE"), "valid documents must not be rejected");
            Require(Plan(root, "missing.cfg", null).Diagnostics.Any(d => d.Code == "OL_E_INVALID_ARGUMENT" && d.Message.Contains("does not exist")), "a missing document must be rejected");
            Require(Plan(root, "not-keyboard.cfg", null).Diagnostics.Any(d => d.Code == "OL_E_INVALID_ARGUMENT" && d.Message.Contains("[entry]")), "a file that is not a keyboard file must be rejected");
            Console.WriteLine("PASS input-documents.planned-as-overlays");
        }
        finally { Directory.Delete(root, true); }
    }

    private static SessionPlan Plan(string root, string? keyboard, string? controller)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        var input = new InputSpec(keyboard is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(keyboard), controller is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(controller));
        var spec = new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new(), Input: input);
        return new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(spec).GetAwaiter().GetResult();
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
