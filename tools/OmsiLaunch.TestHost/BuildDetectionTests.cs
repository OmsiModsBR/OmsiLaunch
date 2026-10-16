using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Core;
using OmsiLaunch.NativeResolution;

// Beta 4 compatibility: "OMSI 2 installed" is the only gate; hashes are
// diagnostics; native capabilities are resolved per capability.
internal static class BuildDetectionTests
{
    public static void RejectsWhatIsNotOmsi2()
    {
        var root = Root(); try
        {
            Require(!OmsiBuildDetection.Detect(root).Found, "a missing Omsi.exe must be reported as not found");
            File.WriteAllText(Path.Combine(root, "Omsi.exe"), "not an executable");
            var notPe = OmsiBuildDetection.Detect(root);
            Require(notPe.Found && !notPe.IsOmsi2 && notPe.Reason!.Contains("PE"), "a non-PE Omsi.exe must not be OMSI 2");
            // A plan for it is not runnable with OL_E_UNSUPPORTED_BUILD.
            var plan = Plan(root);
            Require(!plan.IsRunnable && plan.Diagnostics.Any(d => d.Code == "OL_E_UNSUPPORTED_BUILD"), "a non-OMSI-2 executable must yield OL_E_UNSUPPORTED_BUILD");
            Directory.Delete(Path.Combine(root, "Vehicles"));
            var noVehicles = OmsiBuildDetection.Detect(root);
            Require(!noVehicles.IsOmsi2 && noVehicles.Reason!.Contains("Vehicles"), "an installation without Vehicles\\ must not be OMSI 2");
        }
        finally { Directory.Delete(root, true); }
    }

    // Runs only when OMSILAUNCH_REFERENCE_EXE names the reference executable;
    // the executable is copied into a temporary installation, never modified.
    public static void ReferenceExecutable()
    {
        var reference = Environment.GetEnvironmentVariable("OMSILAUNCH_REFERENCE_EXE");
        if (string.IsNullOrWhiteSpace(reference) || !File.Exists(reference)) { Console.WriteLine("SKIP build-detection.reference-executable (OMSILAUNCH_REFERENCE_EXE not set)"); return; }
        var root = Root(); try
        {
            File.Copy(reference, Path.Combine(root, "Omsi.exe"));
            Directory.CreateDirectory(Path.Combine(root, "maps", "Grundorf"));
            File.WriteAllText(Path.Combine(root, "maps", "Grundorf", "global.cfg"), "[entrypoints]\r\n0\r\n");
            var build = OmsiBuildDetection.Detect(root);
            Require(build.IsOmsi2 && build.KnownIdentity && build.BuildId == NativeTargetCatalog.PackName && build.StaticCapabilities.Values.All(x => x), "the reference build must be known with every capability predicted");
            var plan = Plan(root);
            Require(plan.BuildProfileId == NativeTargetCatalog.PackName && plan.Diagnostics.Any(d => d.Code == "omsi.build.detected" && d.Data!["identity"] == "known"), "the plan must report the known build");
            Require(plan.RequiredCapabilities.Any(c => c.Name == "world.new-map" && c.EvidenceState == "RESOLVED_KNOWN_LAYOUT"), "native capabilities must carry the known-layout evidence: " + string.Join("; ", plan.RequiredCapabilities.Select(c => c.Name + "=" + c.EvidenceState)) + " | " + string.Join("; ", plan.Diagnostics.Select(d => d.Code)));
            Require(!plan.Diagnostics.Any(d => d.Code == "OL_E_UNSUPPORTED_BUILD" || d.Code == "OL_W_BUILD_UNRECOGNIZED"), "the reference build must not be unsupported or unrecognized");
            // ExpectedExecutableSha256 pins the executable; RestoreConfiguration cannot be switched off.
            var fileSha = build.FileSha256!;
            Require(!Plan(root, fileSha.ToLowerInvariant()).Diagnostics.Any(d => d.Code == "OL_E_UNSUPPORTED_BUILD"), "a matching executable pin must be accepted");
            var mismatch = Plan(root, new string('0', 64));
            Require(!mismatch.IsRunnable && mismatch.Diagnostics.Any(d => d.Code == "OL_E_UNSUPPORTED_BUILD" && d.Data!["file_sha256"] == fileSha), "a different executable pin must make the plan not runnable");
            Require(LaunchValidation.Validate(Spec(root, "not-a-hash")).Any(d => d.Code == "OL_E_INVALID_ARGUMENT"), "a malformed executable pin must be rejected");
            Require(LaunchValidation.Validate(Spec(root) with { Behavior = new(RestoreConfiguration: false) }).Any(d => d.Code == "OL_E_INVALID_ARGUMENT"), "RestoreConfiguration=false must be rejected");
            Require(!LaunchValidation.Validate(Spec(root)).Any(d => d.Code == "OL_E_INVALID_ARGUMENT"), "the default spec must validate");
            // The same image with its identity withheld: every start
            // capability still resolves through build-independent strategies.
            var image = NativeImage.FromFile(File.ReadAllBytes(reference));
            var generic = NativeResolver.Resolve(image, NativeTargetCatalog.Pack with { KnownCodeSha256 = Array.Empty<string>() });
            foreach (var capability in new[] { "boot.headless-start", "world.new-map", "world.saved-situation", "internet-textures.disabled", "time.read", "time.set", "d3d.texture" })
                Require(generic.IsAvailable(capability), "capability " + capability + " must resolve without the code identity");
            Require(!generic.IsAvailable("vehicles.list"), "layout-dependent capabilities must stay unavailable without the code identity");
            foreach (var target in generic.Targets.Values.Where(t => t.Verified))
                Require(target.Value == NativeTargetCatalog.Pack.Target(target.Id).KnownValue, "generic resolution of " + target.Id + " must reproduce the reference value");
            Console.WriteLine("PASS build-detection.reference-executable");
        }
        finally { Directory.Delete(root, true); }
    }

    private static SessionPlan Plan(string root, string? executablePin = null) =>
        new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(Spec(root, executablePin)).GetAwaiter().GetResult();

    private static LaunchSpec Spec(string root, string? executablePin = null)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(root, executablePin), new(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new(), Presentation: new(SplashMode.Unset));
    }

    private static string Root()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Build-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "maps")); Directory.CreateDirectory(Path.Combine(root, "Vehicles"));
        return root;
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
