using OmsiLaunch.Api;
using OmsiLaunch.Core;

var root = Path.Combine(Path.GetTempPath(), "omsilaunch-profile-tests-" + Guid.NewGuid().ToString("N"));
try
{
    var package = Path.Combine(root, ".omsilaunch", "session-profiles", "grundorf");
    Directory.CreateDirectory(Path.Combine(package, "assets", "splash"));
    File.WriteAllText(Path.Combine(package, "profile.yaml"), "schema: omsilaunch.session-profile/v1\nid: grundorf\nname: Grundorf\nauthor: Tests\nversion: '1'\ncompatibility:\n  maps:\n    - maps\\Grundorf\\global.cfg\nnew:\n  map: maps\\Grundorf\\global.cfg\n  entrypoint-index: 1\npresets:\n  - index: 1\n    id: low\n    name: Low\n    settings:\n      graphics.maxFPS: 30\n    presentation:\n      splash:\n        mode: managed\n        assets: assets\\splash\n");
    var profile = SessionProfileCompiler.Load(root, "grundorf", 1);
    var none = new Dictionary<string, OptionalValue<string>>();
    var seed = new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new());
    var applied = SessionProfileCompiler.Apply(profile, seed, WorldMode.NewMap);
    if (applied.World.MapIdentity.Value != "maps\\Grundorf\\global.cfg" || applied.Environment.General["graphics.maxFPS"].Value != "30" || applied.SessionProfile?.PresetId != "low") throw new InvalidOperationException("Profile did not compile into LaunchSpec.");
    var saved = SessionProfileCompiler.Apply(profile, seed with { World = seed.World with { Mode = WorldMode.SavedSituation, SituationIdentity = OptionalValue<string>.Set("situations\\sample.osn") } }, WorldMode.SavedSituation);
    if (saved.World.MapIdentity.IsSet || saved.World.PresentedEntrypointIndex.IsSet) throw new InvalidOperationException("The new block leaked into saved-situation semantics.");
    try { SessionProfileCompiler.Load(root, "..", 1); throw new InvalidOperationException("Profile traversal was accepted."); }
    catch (SessionProfileException exception) when (exception.Code == "OL_E_SESSION_PROFILE_PATH_ESCAPE") { }
    var invalidRoot = Path.Combine(root, ".omsilaunch", "session-profiles");
    WriteProfile(invalidRoot, "unknown-schema", "schema: future/v2\nid: unknown-schema\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n");
    Expect("OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED", () => SessionProfileCompiler.Load(root, "unknown-schema", 1));
    WriteProfile(invalidRoot, "duplicate-index", "schema: omsilaunch.session-profile/v1\nid: duplicate-index\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n  - index: 1\n    id: two\n    name: Two\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "duplicate-index", 1));
    WriteProfile(invalidRoot, "unknown-setting", "schema: omsilaunch.session-profile/v1\nid: unknown-setting\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    settings:\n      graphics.maxFSP: 30\n");
    Expect("OL_E_SESSION_PROFILE_SETTING_UNKNOWN", () => SessionProfileCompiler.Load(root, "unknown-setting", 1));
    WriteProfile(invalidRoot, "path-escape", "schema: omsilaunch.session-profile/v1\nid: path-escape\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    presentation:\n      splash:\n        mode: managed\n        assets: ..\\outside\n");
    Expect("OL_E_SESSION_PROFILE_PATH_ESCAPE", () => SessionProfileCompiler.Load(root, "path-escape", 1));
    WriteProfile(invalidRoot, "unknown-key", "schema: omsilaunch.session-profile/v1\nid: unknown-key\nname: Test\nauthor: Test\nversion: '1'\nrun: anything\npresets:\n  - index: 1\n    id: one\n    name: One\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "unknown-key", 1));
    Console.WriteLine("PASS session-profiles.strict-compiler");

    // Beta 4: a package holds 1..99 presets (Beta 3: 1..5).
    WriteProfile(invalidRoot, "range-99", PresetYaml("range-99", Enumerable.Range(1, 99)));
    foreach (var index in new[] { 1, 6, 50, 99 })
        if (SessionProfileCompiler.Load(root, "range-99", index).Preset.Index != index) throw new InvalidOperationException("Preset " + index + " of 99 was not selected.");
    Expect("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", () => SessionProfileCompiler.Load(root, "range-99", 0));
    Expect("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", () => SessionProfileCompiler.Load(root, "range-99", 100));
    WriteProfile(invalidRoot, "sparse", PresetYaml("sparse", new[] { 7, 99 }));
    if (SessionProfileCompiler.Load(root, "sparse", 99).Preset.Index != 99) throw new InvalidOperationException("Sparse preset 99 was not selected.");
    Expect("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND", () => SessionProfileCompiler.Load(root, "sparse", 1));
    WriteProfile(invalidRoot, "too-many", PresetYaml("too-many", Enumerable.Range(1, 100)));
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "too-many", 1));
    WriteProfile(invalidRoot, "index-100", PresetYaml("index-100", new[] { 1, 100 }));
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "index-100", 1));
    WriteProfile(invalidRoot, "index-0", PresetYaml("index-0", new[] { 0 }));
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "index-0", 1));
    Console.WriteLine("PASS session-profiles.preset-range-1-99");

    // Beta 4 HOF management in presets: package-relative, collection, vehicle,
    // mirror mode and file filters compile into HofManagement operations.
    Directory.CreateDirectory(Path.Combine(invalidRoot, "hofs", "hof"));
    WriteProfile(invalidRoot, "hofs", "schema: omsilaunch.session-profile/v1\nid: hofs\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - package: hof\n        targets:\n          - Vehicles\\MAN_SD200\\MAN_SD77.bus\n          - Vehicles/GPM_MAN_NL_NG\n        mode: mirror\n      - collection: berlin\n        files:\n          - Line5.hof\n        targets:\n          - Vehicles\\MB_O530\n  - index: 2\n    id: two\n    name: Two\n");
    var hofProfile = SessionProfileCompiler.Load(root, "hofs", 1);
    var hofOps = hofProfile.Preset.Hofs;
    if (hofOps.Count != 2 || hofOps[0].SourceKind != HofSourceKind.Directory || hofOps[0].Source != ".omsilaunch\\session-profiles\\hofs\\hof" || hofOps[0].Mode != HofApplyMode.Mirror || hofOps[0].TargetVehicles[1] != "Vehicles\\GPM_MAN_NL_NG"
        || hofOps[1].SourceKind != HofSourceKind.Collection || hofOps[1].Source != "berlin" || hofOps[1].Mode != HofApplyMode.Add || hofOps[1].Files?.Single() != "Line5.hof")
        throw new InvalidOperationException("Profile hofs did not compile into HOF operations.");
    if (SessionProfileCompiler.Apply(hofProfile, seed, WorldMode.NewMap).EffectiveHofManagement.Operations.Count != 2) throw new InvalidOperationException("Profile hofs were not applied to the LaunchSpec.");
    if (SessionProfileCompiler.Apply(SessionProfileCompiler.Load(root, "hofs", 2), seed, WorldMode.NewMap).EffectiveHofManagement.Operations.Count != 0) throw new InvalidOperationException("A preset without hofs must not carry HOF operations.");
    WriteProfile(invalidRoot, "hofs-two-sources", "schema: omsilaunch.session-profile/v1\nid: hofs-two-sources\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - collection: a\n        vehicle: Vehicles\\B\n        targets:\n          - Vehicles\\C\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "hofs-two-sources", 1));
    WriteProfile(invalidRoot, "hofs-no-targets", "schema: omsilaunch.session-profile/v1\nid: hofs-no-targets\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - collection: a\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "hofs-no-targets", 1));
    WriteProfile(invalidRoot, "hofs-escape", "schema: omsilaunch.session-profile/v1\nid: hofs-escape\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - package: ..\\grundorf\n        targets:\n          - Vehicles\\C\n");
    Expect("OL_E_SESSION_PROFILE_PATH_ESCAPE", () => SessionProfileCompiler.Load(root, "hofs-escape", 1));
    WriteProfile(invalidRoot, "hofs-missing", "schema: omsilaunch.session-profile/v1\nid: hofs-missing\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - package: absent\n        targets:\n          - Vehicles\\C\n");
    Expect("OL_E_SESSION_PROFILE_ASSET_MISSING", () => SessionProfileCompiler.Load(root, "hofs-missing", 1));
    WriteProfile(invalidRoot, "hofs-mode", "schema: omsilaunch.session-profile/v1\nid: hofs-mode\nname: Test\nauthor: Test\nversion: '1'\npresets:\n  - index: 1\n    id: one\n    name: One\n    hofs:\n      - collection: a\n        mode: replace\n        targets:\n          - Vehicles\\C\n");
    Expect("OL_E_SESSION_PROFILE_INVALID", () => SessionProfileCompiler.Load(root, "hofs-mode", 1));
    Console.WriteLine("PASS session-profiles.hofs");

    // Discovery compiles every package and reports failures instead of hiding them.
    var discovered = SessionProfileCompiler.Discover(root).ToDictionary(x => x.Id, StringComparer.Ordinal);
    if (!discovered.TryGetValue("grundorf", out var grundorf) || !grundorf.IsValid || grundorf.MapIdentity != "maps\\Grundorf\\global.cfg" || grundorf.Presets.Count != 1 || grundorf.Presets[0].Id != "low")
        throw new InvalidOperationException("Discovery did not describe the grundorf package.");
    if (!discovered.TryGetValue("range-99", out var range) || !range.IsValid || range.Presets.Count != 99 || range.Presets[98].Index != 99)
        throw new InvalidOperationException("Discovery did not list 99 presets in index order.");
    if (!discovered.TryGetValue("unknown-schema", out var broken) || broken.IsValid || broken.ErrorCode != "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED")
        throw new InvalidOperationException("Discovery did not report an invalid package with its error code.");
    if (discovered.ContainsKey("junction-escape")) throw new InvalidOperationException("Discovery listed a package created later in the test.");
    if (SessionProfileCompiler.Discover(Path.Combine(root, "no-installation")).Count != 0) throw new InvalidOperationException("Discovery without a profiles directory must be empty.");
    Console.WriteLine("PASS session-profiles.discovery");

    // S-17: lexical confinement is not enough; a junction inside the package
    // resolves outside it and must be rejected.
    var junctionPackage = Path.Combine(invalidRoot, "junction-escape"); Directory.CreateDirectory(junctionPackage);
    var outside = Path.Combine(root, "outside-assets"); Directory.CreateDirectory(outside);
    var link = Path.Combine(junctionPackage, "assets");
    var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + outside + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
    mklink.WaitForExit();
    if (mklink.ExitCode != 0 || !Directory.Exists(link)) throw new InvalidOperationException("junction fixture could not be created: " + mklink.StandardError.ReadToEnd());
    File.WriteAllText(Path.Combine(junctionPackage, "profile.yaml"), "schema: omsilaunch.session-profile/v1\nid: junction-escape\nname: Test\nauthor: Test\nversion: \'1\'\npresets:\n  - index: 1\n    id: one\n    name: One\n    presentation:\n      splash:\n        mode: managed\n        assets: assets\n");
    try { Expect("OL_E_SESSION_PROFILE_PATH_ESCAPE", () => SessionProfileCompiler.Load(root, "junction-escape", 1)); }
    finally { Directory.Delete(link); }
    Console.WriteLine("PASS session-profiles.reparse-point-rejected");
}
finally { if (Directory.Exists(root)) Directory.Delete(root, true); }

static void WriteProfile(string profilesRoot, string id, string yaml)
{
    var package = Path.Combine(profilesRoot, id);
    Directory.CreateDirectory(package);
    File.WriteAllText(Path.Combine(package, "profile.yaml"), yaml);
}

static string PresetYaml(string id, IEnumerable<int> indexes) =>
    "schema: omsilaunch.session-profile/v1\nid: " + id + "\nname: Test\nauthor: Test\nversion: '1'\npresets:\n" +
    string.Concat(indexes.Select(index => "  - index: " + index + "\n    id: p" + index + "\n    name: Preset " + index + "\n"));

static void Expect(string code, Action action)
{
    try { action(); throw new InvalidOperationException("Expected profile error " + code); }
    catch (SessionProfileException exception) when (exception.Code == code) { }
}
