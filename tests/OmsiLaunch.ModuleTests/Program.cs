using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Runtime.Modules;

// Runtime Modules offline gates: manifest (protocols 1 and 2), integrity
// (loading is open, E21), plan eligibility and selection (requested, settings
// file, content, default, disabled), settings, plugin conflicts, the
// in-process lifecycle with a failure injected in every phase under every
// failure policy, cleanup of every tracked resource, and the handoff wire that
// carries planned modules and their settings to the plugin.
var failures = new List<string>();
var root = Path.Combine(Path.GetTempPath(), "omsilaunch-module-tests-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try
{
    Gate("manifest.valid", () =>
    {
        var manifest = ModuleManifestParser.Parse(Encoding.UTF8.GetBytes(Manifest("sample")));
        Check(manifest.Id == "sample" && manifest.ManagedComponent.Entry == "Sample.Module" && manifest.FailurePolicy == ModuleFailurePolicy.DisableModule, "fields");
        Check(manifest.Operations.Single().Id == "ping" && manifest.OperationName("ping") == "module.sample.ping", "operation name");
        Check(manifest.Timeouts.ArmMilliseconds == RuntimeModuleProtocol.DefaultPhaseTimeoutMilliseconds, "default timeouts");
    });

    Gate("manifest.invalid", () =>
    {
        var cases = new Dictionary<string, string>
        {
            ["unknown key"] = Manifest("sample").Replace("\"name\":", "\"run\": 1, \"name\":"),
            ["schema"] = Manifest("sample").Replace("runtime-module/v1", "runtime-module/v2"),
            ["id"] = Manifest("Sample"),
            ["version"] = Manifest("sample").Replace("\"1.0.0\"", "\"1\""),
            ["protocol range"] = Manifest("sample").Replace("\"min\": 1, \"max\": 1", "\"min\": 2, \"max\": 1"),
            ["two managed"] = Manifest("sample", extraComponents: ", { \"kind\": \"managed\", \"path\": \"b.dll\", \"sha256\": \"" + new string('A', 64) + "\", \"entry\": \"B.Module\" }"),
            ["no entry"] = Manifest("sample").Replace(", \"entry\": \"Sample.Module\"", ""),
            ["path escape"] = Manifest("sample").Replace("\"sample.dll\"", "\"../sample.dll\""),
            ["rooted path"] = Manifest("sample").Replace("\"sample.dll\"", "\"C:/sample.dll\""),
            ["bad sha"] = Manifest("sample").Replace(Sha("sample"), "XYZ"),
            ["content without binding"] = Manifest("sample").Replace("\"requested\"", "\"content\""),
            ["policy"] = Manifest("sample").Replace("disable-module", "ignore"),
            ["duplicate operation"] = Manifest("sample").Replace("\"operations\": [", "\"operations\": [ { \"id\": \"ping\", \"kind\": \"read\" },"),
            ["timeout range"] = Manifest("sample").Replace("\"arm_ms\": 5000", "\"arm_ms\": 999999"),
            ["required and optional"] = Manifest("sample").Replace("\"optional_capabilities\": []", "\"optional_capabilities\": [\"bridge.events\"]"),
            ["auxiliary not exe"] = Manifest("sample", extraComponents: ", { \"kind\": \"auxiliary\", \"path\": \"helper.dll\", \"sha256\": \"" + new string('B', 64) + "\" }"),
            ["not json"] = "{",
            ["default with binding"] = ManifestV2("sample", ", \"content_binding\": { \"map\": \"maps/Grundorf/global.cfg\" }").Replace("\"requested\"", "\"default\""),
            ["protocol 2 feature on protocol 1"] = Manifest("sample", extraRoot: ", \"settings\": [ { \"key\": \"a\", \"type\": \"boolean\", \"default\": true } ]"),
            ["default activation on protocol 1"] = Manifest("sample").Replace("\"requested\"", "\"default\""),
            ["setting key enabled"] = ManifestV2("sample", Settings("{ \"key\": \"enabled\", \"type\": \"boolean\", \"default\": true }")),
            ["setting type"] = ManifestV2("sample", Settings("{ \"key\": \"a\", \"type\": \"float\", \"default\": 1 }")),
            ["choice without choices"] = ManifestV2("sample", Settings("{ \"key\": \"a\", \"type\": \"choice\", \"default\": \"x\" }")),
            ["default out of range"] = ManifestV2("sample", Settings("{ \"key\": \"a\", \"type\": \"integer\", \"default\": 9, \"min\": 0, \"max\": 5 }")),
            ["min on text"] = ManifestV2("sample", Settings("{ \"key\": \"a\", \"type\": \"text\", \"default\": \"x\", \"min\": 0 }")),
            ["repeated flag"] = ManifestV2("sample", Settings("{ \"key\": \"a\", \"type\": \"boolean\", \"default\": true, \"flag\": \"same-flag\" }, { \"key\": \"b\", \"type\": \"boolean\", \"default\": true, \"flag\": \"same-flag\" }")),
            ["conflict not a dll"] = ManifestV2("sample", ", \"conflicts\": { \"plugins\": [\"../evil.exe\"] }"),
            ["host component"] = Manifest("sample", extraComponents: ", { \"kind\": \"host\", \"path\": \"host.exe\", \"sha256\": \"" + new string('C', 64) + "\" }"),
        };
        foreach (var (name, json) in cases)
        {
            try { ModuleManifestParser.Parse(Encoding.UTF8.GetBytes(json)); Check(false, "accepted: " + name); }
            catch (ModuleException exception) { Check(exception.Code == PublicErrorCodes.MODULE_MANIFEST_INVALID, name + " -> " + exception.Code); }
        }
    });

    Gate("catalog.open-loading-and-integrity", () =>
    {
        var install = NewInstallation();
        var (directory, manifestSha, _) = InstallModule(install, "sample");
        var installed = ModuleCatalog.Discover(install).Single();
        Check(installed.IsUsable && installed.ManifestSha256 == manifestSha, "any installed, intact module is usable (open loading): " + installed.Code);
        File.WriteAllText(Path.Combine(directory, "sample.dll"), "tampered");
        Check(ModuleCatalog.Discover(install).Single().Code == PublicErrorCodes.MODULE_INTEGRITY, "a file that does not match its pin fails integrity");
        Check(ModuleCatalog.ReadDeclarations(install).Single().Id == "sample", "declarations are read without hashing the module's files");
        var misnamed = Path.Combine(install, "plugins", "OmsiLaunch.Modules", "other");
        Directory.CreateDirectory(misnamed); File.WriteAllText(Path.Combine(misnamed, "module.json"), Manifest("sample"));
        Check(ModuleCatalog.Discover(install).Single(x => x.DirectoryName == "other").Code == PublicErrorCodes.MODULE_MANIFEST_INVALID, "id must equal the directory name");
        Check(ModuleCatalog.InspectPlanned(directory, new string('0', 64)).Code == PublicErrorCodes.MODULE_INTEGRITY, "in process, a module.json other than the planned one is rejected");
    });

    Gate("eligibility", () =>
    {
        var install = NewInstallation();
        InstallModule(install, "sample");
        InstallModule(install, "newer", manifest: Manifest("newer").Replace("\"min\": 1, \"max\": 1", "\"min\": 3, \"max\": 4"));
        InstallModule(install, "v2", manifest: ManifestV2("v2", ""));
        InstallModule(install, "needs-d3d", manifest: Manifest("needs-d3d").Replace("\"required_capabilities\": [\"bridge.events\"]", "\"required_capabilities\": [\"d3d.texture\"]"));
        InstallModule(install, "unknown-cap", manifest: Manifest("unknown-cap").Replace("\"required_capabilities\": [\"bridge.events\"]", "\"required_capabilities\": [\"teleport\"]"));
        var mapFile = Path.Combine(install, "maps", "Grundorf", "global.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(mapFile)!); File.WriteAllText(mapFile, "[name]\nGrundorf\n");
        var mapSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(mapFile)));
        var binding = ", \"content_binding\": { \"map\": \"maps/Grundorf/global.cfg\", \"files\": [ { \"path\": \"maps/Grundorf/global.cfg\", \"sha256\": \"" + mapSha + "\" } ] }";
        InstallModule(install, "grundorf-hud", manifest: Manifest("grundorf-hud", extraRoot: binding).Replace("\"requested\"", "\"content\""));
        InstallModule(install, "bad-binding", manifest: Manifest("bad-binding", extraRoot: binding.Replace(mapSha, new string('C', 64))));
        var discovered = ModuleCatalog.Discover(install);
        ModuleCapabilityState Capability(string name) => name switch { "bridge.events" => ModuleCapabilityState.Available, "d3d.texture" => ModuleCapabilityState.Unavailable, _ => ModuleCapabilityState.Unknown };
        var grundorf = @"maps\Grundorf\global.cfg";
        var plan = ModuleEligibility.Evaluate(discovered, new[] { "sample", "newer", "v2", "needs-d3d", "unknown-cap", "bad-binding", "missing" }, install, grundorf, Capability).ToDictionary(x => x.Id);
        Check(plan["v2"].IsEligible, "a protocol 2 module is served by this bridge");
        Check(plan["sample"].IsEligible && plan["sample"].Operations.SequenceEqual(new[] { "module.sample.ping" }) && plan["sample"].ManifestSha256 is { Length: 64 }, "sample eligible");
        Check(plan["newer"].Code == PublicErrorCodes.MODULE_PROTOCOL_UNSUPPORTED, "protocol: " + plan["newer"].Code);
        Check(plan["needs-d3d"].Code == PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "unavailable capability");
        Check(plan["unknown-cap"].Code == PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "unknown capability");
        Check(plan["bad-binding"].Code == PublicErrorCodes.MODULE_CONTENT_BINDING, "binding hash");
        Check(plan["missing"].Code == PublicErrorCodes.MODULE_NOT_FOUND, "not installed");
        Check(plan["grundorf-hud"].IsEligible && !plan["grundorf-hud"].Requested && plan["grundorf-hud"].Activation == PlannedModule.ActivationContent, "a content module joins on its map");
        var elsewhere = ModuleEligibility.Evaluate(discovered, Array.Empty<string>(), install, @"maps\Other\global.cfg", Capability);
        Check(elsewhere.Count == 0, "a content module must not load on another map");
        var requestedElsewhere = ModuleEligibility.Evaluate(discovered, new[] { "grundorf-hud" }, install, @"maps\Other\global.cfg", Capability).Single();
        Check(requestedElsewhere.Code == PublicErrorCodes.MODULE_CONTENT_BINDING, "a bound module requested on another map is rejected");
        var pending = ModuleEligibility.Evaluate(discovered, new[] { "needs-d3d" }, install, grundorf, _ => ModuleCapabilityState.Pending).Single(x => x.Id == "needs-d3d");
        Check(pending.IsEligible, "a pending capability is decided in process");
    });

    // Lifecycle with a failure injected in each phase under each policy.
    foreach (var policy in new[] { "fail-session", "disable-module", "degrade" })
        foreach (var phase in new[] { "arm", "gameplay-ready", "operation", "stop", "arm-timeout", "operation-timeout" })
            Gate("lifecycle." + phase + "." + policy, () => LifecycleCase(phase, policy));

    Gate("lifecycle.success-and-cleanup-order", () =>
    {
        var (host, environment, module) = NewHost("clean", "disable-module", _ => { });
        host.Arm(); host.GameplayReady();
        var result = host.Execute("module.clean.ping", new Dictionary<string, string> { ["text"] = "hi" });
        Check(result.Succeeded && result.Values["echo"] == "hi" && !result.Values.ContainsKey("internal_address"), "operation result (internal keys scrubbed)");
        host.Stop();
        Check(module.Stopped && environment.Disposed.SequenceEqual(new[] { "second", "first" }), "tracked resources released in reverse order: " + string.Join(",", environment.Disposed));
        Check(environment.Events.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "complete"), "cleanup complete");
        Check(environment.Events.Any(x => x.Name == "module.clean.armed"), "module events are namespaced");
        Check(host.Status.Single().State == ModuleState.Stopped && host.Status.Single().TrackedResources == 0, "stopped with nothing tracked");
        Check(!host.SessionFailureRequested, "no session failure");
    });

    Gate("lifecycle.operation-contract", () =>
    {
        var (host, _, _) = NewHost("contract", "degrade", _ => { });
        Check(host.Execute("module.contract.ping", null).ErrorCode == PublicErrorCodes.MODULE_NOT_LOADED, "operations before gameplay-ready are rejected");
        host.Arm(); host.GameplayReady();
        Check(host.Execute("module.contract.nope", null).ErrorCode == PublicErrorCodes.MODULE_OPERATION_UNKNOWN, "undeclared operation");
        Check(host.Execute("module.other.ping", null).ErrorCode == PublicErrorCodes.MODULE_NOT_LOADED, "unknown module");
        Check(host.Execute("module.contract.ping", new Dictionary<string, string>()).ErrorCode == PublicErrorCodes.RUNTIME_ARGUMENT_REQUIRED, "required argument");
        Check(host.Execute("module.contract.ping", new Dictionary<string, string> { ["text"] = "a", ["other"] = "b" }).ErrorCode == PublicErrorCodes.RUNTIME_ARGUMENT_REQUIRED, "undeclared argument");
        var typed = host.Execute("module.contract.ping", new Dictionary<string, string> { ["text"] = "typed" });
        Check(typed.ErrorCode == PublicErrorCodes.RUNTIME_VALUE_OUT_OF_RANGE, "a catalogued module code survives: " + typed.ErrorCode);
        var foreign = host.Execute("module.contract.ping", new Dictionary<string, string> { ["text"] = "foreign" });
        Check(foreign.ErrorCode == PublicErrorCodes.MODULE_OPERATION_FAILED && foreign.Values["module_code"] == "SAMPLE_E_WHATEVER", "an uncatalogued module code is carried as module_code");
        host.Stop();
    });

    Gate("lifecycle.cleanup-incomplete", () =>
    {
        var (host, environment, _) = NewHost("stuck", "disable-module", context => context.StartAuxiliaryProcess("helper.exe", ""), auxiliary: true);
        environment.AuxiliaryRefusesToExit = true;
        host.Arm(); host.GameplayReady(); host.Stop();
        Check(environment.Events.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "incomplete" && x.Data["code"] == PublicErrorCodes.MODULE_CLEANUP_INCOMPLETE), "a process that survives cleanup is reported");
    });

    Gate("lifecycle.background-thread", () =>
    {
        var started = new ManualResetEventSlim();
        var (host, environment, _) = NewHost("worker", "disable-module", context => context.RunBackground("loop", token => { started.Set(); token.WaitHandle.WaitOne(); }));
        host.Arm(); Check(started.Wait(2000), "background work started"); host.Stop();
        Check(environment.Events.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "complete"), "a cooperative background thread ends at cleanup");
    });

    Gate("lifecycle.native-component", () =>
    {
        var install = NewInstallation();
        var (directory, manifestSha, _) = InstallModule(install, "native", manifest: Manifest("native", extraComponents: ", { \"kind\": \"native\", \"path\": \"native.dll\", \"sha256\": \"" + Sha("native") + "\" }"), extraFiles: new() { ["native.dll"] = "native" });
        var environment = new FakeEnvironment(_ => { });
        var host = new ModuleHost(environment);
        host.Load(new[] { new ModuleLoadRequest(directory, manifestSha) });
        host.Arm(); host.GameplayReady(); host.Stop();
        Check(environment.NativeLog.SequenceEqual(new[] { "start", "gameplay-ready", "dispose" }), "native component lifecycle: " + string.Join(",", environment.NativeLog));
        Check(environment.Disposed.SequenceEqual(new[] { "second", "first", "native" }), "the native component is released last: " + string.Join(",", environment.Disposed));
        var failing = new FakeEnvironment(_ => { }) { NativeStartFails = true };
        var failingHost = new ModuleHost(failing);
        var (failDirectory, failSha, _) = InstallModule(install, "native-bad", manifest: Manifest("native-bad", extraComponents: ", { \"kind\": \"native\", \"path\": \"native.dll\", \"sha256\": \"" + Sha("native") + "\" }"), extraFiles: new() { ["native.dll"] = "native" });
        failingHost.Load(new[] { new ModuleLoadRequest(failDirectory, failSha) }); failingHost.Arm();
        Check(failingHost.Status.Single().State == ModuleState.Disabled && failing.Events.Any(x => x.Name == "module.cleanup"), "a native start failure disables the module and cleans up");
        File.WriteAllText(Path.Combine(directory, "native.dll"), "tampered");
        var tampered = new FakeEnvironment(_ => { });
        var tamperedHost = new ModuleHost(tampered);
        tamperedHost.Load(new[] { new ModuleLoadRequest(directory, manifestSha) });
        Check(tamperedHost.Status.Count == 0 && tampered.NativeLog.Count == 0, "a tampered native component is never loaded");
    });

    Gate("lifecycle.load-rejections", () =>
    {
        var install = NewInstallation();
        var environment = new FakeEnvironment(_ => { });
        var host = new ModuleHost(environment);
        var (capDirectory, capSha, _) = InstallModule(install, "needs-time", manifest: Manifest("needs-time").Replace("\"required_capabilities\": [\"bridge.events\"]", "\"required_capabilities\": [\"time.read\"]"));
        environment.Unavailable.Add("time.read");
        host.Load(new[] { new ModuleLoadRequest(capDirectory, capSha) });
        Check(environment.Events.Any(x => x.Name == "module.load.failed" && x.Data["code"] == PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED), "a capability unresolved in process blocks the load");
        var (fsDirectory, fsSha, _) = InstallModule(install, "must-run", manifest: Manifest("must-run").Replace("disable-module", "fail-session").Replace("\"required_capabilities\": [\"bridge.events\"]", "\"required_capabilities\": [\"time.read\"]"));
        host.Load(new[] { new ModuleLoadRequest(fsDirectory, fsSha) });
        Check(host.SessionFailureRequested && environment.Events.Any(x => x.Name == "module.session-failure"), "a fail-session module that cannot load fails the session");
    });

    Gate("lifecycle.content-binding-in-process", () =>
    {
        var install = NewInstallation();
        var (directory, sha, _) = InstallModule(install, "bound", manifest: Manifest("bound", extraRoot: ", \"content_binding\": { \"map\": \"maps/Grundorf/global.cfg\" }"));
        var environment = new FakeEnvironment(_ => { }) { LoadedMapIdentity = @"maps\Other\global.cfg" };
        var host = new ModuleHost(environment);
        host.Load(new[] { new ModuleLoadRequest(directory, sha) }); host.Arm(); host.GameplayReady();
        Check(host.Status.Single().State == ModuleState.Disabled && host.Status.Single().LastErrorCode == PublicErrorCodes.MODULE_CONTENT_BINDING, "a module bound to another map is disabled in process");
    });

    Gate("operation-names", () =>
    {
        foreach (var name in new[] { "module.null-test.ping", "module.ab.x", "module.hud.overlay.show", "module.a.x", "module.Hud.x", "module.hud.", "module.hud..x", "module.hud", "time.read", "module..x" })
        {
            var split = RuntimeModuleProtocol.TrySplitOperation(name, out _, out _);
            Check(split == PublicCapabilityRegistry.IsModuleOperation(name), "registry and protocol disagree on " + name);
        }
        Check(PublicCapabilityRegistry.ValidateRuntimeArguments("module.null-test.ping", null).Accepted, "a module operation passes the frontend check; the session decides");
    });

    Gate("handoff.v6-modules", () =>
    {
        var modules = new[] { new StartupModule(@"plugins\OmsiLaunch.Modules\sample", new string('A', 64)), new StartupModule(@"plugins\OmsiLaunch.Modules\grundorf-hud", new string('B', 64)) };
        var handoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004", WorldMode.NewMap, @"maps\Grundorf\global.cfg", 1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "", "", modules);
        var bytes = StartupHandoffWire.Serialize(handoff);
        Check(StartupHandoffWire.TryDeserialize(bytes, out var decoded) && decoded is not null && decoded.Modules!.SequenceEqual(modules) && decoded with { Modules = null } == handoff with { Modules = null }, "v5 round trip");
        var none = handoff with { Modules = null };
        Check(StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(none), out var plain) && plain == none, "no modules decodes to null");
        // A version 4 writer: same payload without the module section (and without the version 7 start block of 21 bytes and empty weather block of 9).
        var v5 = StartupHandoffWire.Serialize(none);
        var payload = v5.AsSpan(64, v5.Length - 64 - 4 - 21 - 9).ToArray();
        var v4 = new byte[64 + payload.Length]; v5.AsSpan(0, 64).CopyTo(v4);
        BinaryPrimitives.WriteUInt16LittleEndian(v4.AsSpan(4), 4); BinaryPrimitives.WriteUInt32LittleEndian(v4.AsSpan(8), (uint)v4.Length); BinaryPrimitives.WriteUInt32LittleEndian(v4.AsSpan(28), (uint)payload.Length);
        SHA256.HashData(payload).CopyTo(v4.AsSpan(32)); payload.CopyTo(v4.AsSpan(64));
        Check(StartupHandoffWire.TryDeserialize(v4, out var old) && old == none, "version 4 still decodes");
        // Version 7: the start weather follows the start block.
        foreach (var weather in new[] { new StartWeather(WeatherMode.Preset, @"weather\Regen.owt"), new StartWeather(WeatherMode.Icao, Icao: "EDDF"), new StartWeather(WeatherMode.RealCurrent) })
            Check(StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(none with { Weather = weather }), out var withWeather) && withWeather == none with { Weather = weather }, "start weather round trip: " + weather.Mode);
        var trailing = bytes.Concat(new byte[] { 0 }).ToArray();
        Check(!StartupHandoffWire.TryDeserialize(trailing, out _), "trailing bytes are rejected");
        // Version 6: a module's settings travel as a JSON object of strings.
        var withSettings = handoff with { Modules = new[] { new StartupModule(@"plugins\OmsiLaunch.Modules\sample", new string('A', 64), "{\"ram\":\"auto\",\"monitor\":\"true\"}") } };
        Check(StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(withSettings), out var settingsDecoded) && settingsDecoded!.Modules!.Single() == withSettings.Modules!.Single(), "v6 settings round trip");
        // A version 5 writer: one module, no settings length after its SHA-256.
        var singleV6 = StartupHandoffWire.Serialize(handoff with { Modules = new[] { new StartupModule(@"plugins\OmsiLaunch.Modules\sample", new string('A', 64)) } });
        var asV5Payload = singleV6.AsSpan(64, singleV6.Length - 64 - 4 - 21 - 9).ToArray();
        var asV5 = new byte[64 + asV5Payload.Length]; singleV6.AsSpan(0, 64).CopyTo(asV5);
        BinaryPrimitives.WriteUInt16LittleEndian(asV5.AsSpan(4), 5); BinaryPrimitives.WriteUInt32LittleEndian(asV5.AsSpan(8), (uint)asV5.Length); BinaryPrimitives.WriteUInt32LittleEndian(asV5.AsSpan(28), (uint)asV5Payload.Length);
        SHA256.HashData(asV5Payload).CopyTo(asV5.AsSpan(32)); asV5Payload.CopyTo(asV5.AsSpan(64));
        Check(StartupHandoffWire.TryDeserialize(asV5, out var fromV5) && fromV5!.Modules!.Single() == new StartupModule(@"plugins\OmsiLaunch.Modules\sample", new string('A', 64)), "version 5 still decodes (no settings)");
    });

    Gate("selection.activation-and-disable", () =>
    {
        var install = NewInstallation();
        var mapFile = Path.Combine(install, "maps", "Grundorf", "global.cfg");
        Directory.CreateDirectory(Path.GetDirectoryName(mapFile)!); File.WriteAllText(mapFile, "[name]\nGrundorf\n");
        InstallModule(install, "req");
        InstallModule(install, "dflt", manifest: ManifestV2("dflt", "").Replace("\"requested\"", "\"default\""));
        InstallModule(install, "dflt-off", manifest: ManifestV2("dflt-off", "").Replace("\"requested\"", "\"default\""));
        InstallModule(install, "file-on");
        InstallModule(install, "file-off", manifest: ManifestV2("file-off", "").Replace("\"requested\"", "\"default\""));
        InstallModule(install, "bound", manifest: Manifest("bound", extraRoot: ", \"content_binding\": { \"map\": \"maps/Grundorf/global.cfg\" }").Replace("\"requested\"", "\"content\""));
        var file = ModuleSettingsFile.Parse("; user settings\n[file-on]\nenabled=yes\n[FILE-OFF]\nenabled = false\n[ghost]\nenabled=true\n");
        Check(file.Problems.Count == 0 && file.Enabled("file-on") == true && file.Enabled("file-off") == false, "settings file sections and enabled: " + string.Join("; ", file.Problems));
        var selection = new ModuleSelection(new[] { "req" }, new[] { "dflt-off" }, new Dictionary<string, string>(), file, new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase));
        var plan = ModuleEligibility.Plan(ModuleCatalog.Discover(install), selection, install, @"maps\Grundorf\global.cfg", _ => ModuleCapabilityState.Available);
        var byId = plan.Modules.ToDictionary(x => x.Id);
        Check(byId["req"].IsEligible && byId["req"].Requested && byId["req"].Activation == PlannedModule.ActivationRequested, "requested");
        Check(byId["dflt"].IsEligible && !byId["dflt"].Requested && byId["dflt"].Activation == PlannedModule.ActivationDefault, "a default module joins every session");
        Check(byId["file-on"].IsEligible && !byId["file-on"].Requested && byId["file-on"].Activation == PlannedModule.ActivationSettingsFile, "switched on in the settings file");
        Check(byId["bound"].IsEligible && byId["bound"].Activation == PlannedModule.ActivationContent, "content module on its map");
        Check(!byId.ContainsKey("dflt-off") && !byId.ContainsKey("file-off") && plan.Disabled.OrderBy(x => x).SequenceEqual(new[] { "dflt-off", "file-off" }), "disabled by the request and by the settings file: " + string.Join(",", plan.Disabled));
        Check(plan.Problems.Any(x => x.ModuleId == "ghost" && !x.Error), "the settings file enabling a module that is not installed is a warning");
        var explicitWins = ModuleEligibility.Plan(ModuleCatalog.Discover(install), selection with { Enable = new[] { "file-off" }, Disable = Array.Empty<string>() }, install, null, _ => ModuleCapabilityState.Available);
        Check(explicitWins.Modules.Any(x => x.Id == "file-off" && x.IsEligible && x.Requested), "an explicit request wins over enabled=false in the settings file");
        var both = ModuleEligibility.Plan(ModuleCatalog.Discover(install), selection with { Enable = new[] { "req" }, Disable = new[] { "req" } }, install, null, _ => ModuleCapabilityState.Available);
        Check(both.Problems.Any(x => x.ModuleId == "req" && x.Error), "a module both enabled and disabled is an error");
    });

    Gate("settings.resolve", () =>
    {
        var manifest = ModuleManifestParser.Parse(Encoding.UTF8.GetBytes(ManifestV2("tuned", Settings(
            "{ \"key\": \"ram_mb\", \"type\": \"integer\", \"default\": \"auto\", \"auto\": true, \"min\": 256, \"max\": 12288, \"flag\": \"tuned-ram\" }, " +
            "{ \"key\": \"monitor\", \"type\": \"boolean\", \"default\": false, \"flag\": \"tuned-monitor\" }, " +
            "{ \"key\": \"mode\", \"type\": \"choice\", \"choices\": [\"fast\", \"safe\"], \"default\": \"fast\" }, " +
            "{ \"key\": \"label\", \"type\": \"text\", \"default\": \"\", \"max_length\": 8 }"))));
        var ram = manifest.EffectiveSettings.Single(x => x.Key == "ram_mb");
        Check(ram.Default == "auto" && ram.AllowsAuto && ram.Minimum == 256 && ram.Maximum == 12288 && ram.Flag == "tuned-ram", "parsed integer setting");
        var problems = new List<ModuleSettingProblem>();
        var file = ModuleSettingsFile.Parse("[tuned]\nram_mb=4096\nmonitor=yes\nmode=turbo\nunknown=1\n").Section("tuned");
        var values = ModuleSettings.Resolve(manifest, new Dictionary<string, string> { ["ram_mb"] = "8192" }, file, problems).ToDictionary(x => x.Key);
        Check(values["ram_mb"].Value == "8192" && values["ram_mb"].Source == ModuleSettingSource.Request, "the request wins over the settings file");
        Check(values["monitor"].Value == "true" && values["monitor"].Source == ModuleSettingSource.SettingsFile, "the settings file wins over the default");
        Check(values["mode"].Value == "fast" && values["mode"].Source == ModuleSettingSource.Default, "an invalid settings-file value falls back to the default");
        Check(values["label"].Value == "" && values["label"].Source == ModuleSettingSource.Default, "default");
        Check(problems.Count == 2 && problems.All(x => !x.Error), "settings-file problems are warnings: " + string.Join("; ", problems.Select(x => x.Message)));
        var errors = new List<ModuleSettingProblem>();
        ModuleSettings.Resolve(manifest, new Dictionary<string, string> { ["ram_mb"] = "99999", ["nope"] = "1", ["label"] = "far too long" }, null, errors);
        Check(errors.Count == 3 && errors.All(x => x.Error), "invalid requested values and unknown keys are errors: " + string.Join("; ", errors.Select(x => x.Message)));
        string Normalized(string key, string raw) => ModuleSettings.TryNormalize(manifest.EffectiveSettings.Single(x => x.Key == key), raw, out var value, out _) ? value : "<invalid>";
        Check(Normalized("monitor", " On ") == "true" && Normalized("monitor", "0") == "false" && Normalized("monitor", "maybe") == "<invalid>", "boolean normalization");
        Check(Normalized("ram_mb", "AUTO") == "auto" && Normalized("ram_mb", "+512") == "512" && Normalized("ram_mb", "100") == "<invalid>", "integer normalization");
        Check(Normalized("mode", "SAFE") == "safe" && Normalized("label", "a\tb") == "<invalid>", "choice and text normalization");
    });

    Gate("settings-file.problems", () =>
    {
        var file = ModuleSettingsFile.Parse("orphan=1\n[Not A Module]\nx=1\n[ok]\nnot a pair\nenabled=perhaps\nBad Key=1\nvalue=1\nvalue=2\n");
        Check(file.Problems.Count == 7, "every unusable entry is reported: " + string.Join(" | ", file.Problems));
        Check(file.Section("ok")!["value"] == "2" && file.Enabled("ok") is null, "the last repeated value wins; an invalid enabled is ignored");
    });

    Gate("plugins.conflict", () =>
    {
        var install = NewInstallation();
        var plugins = Path.Combine(install, "plugins");
        File.WriteAllText(Path.Combine(plugins, "old.opl"), "[dll]\nOldPoC.dll\n");
        File.WriteAllText(Path.Combine(plugins, "off.opl.disabled"), "[dll]\nSwitchedOff.dll\n");
        File.WriteAllText(Path.Combine(plugins, "other.opl"), "[options]\nx=1\n[dll]\nsub\\Other.dll\n");
        var active = PluginInventory.ActiveLibraries(install);
        Check(active.TryGetValue("oldpoc.dll", out var opl) && opl == @"plugins\old.opl" && active.ContainsKey("Other.dll") && !active.ContainsKey("SwitchedOff.dll"), "active plugin libraries: " + string.Join(",", active.Keys));
        InstallModule(install, "modern", manifest: ManifestV2("modern", ", \"conflicts\": { \"plugins\": [\"OldPoC.dll\"] }"));
        var selection = new ModuleSelection(new[] { "modern" }, Array.Empty<string>(), new Dictionary<string, string>(), ModuleSettingsFile.Empty, active);
        var planned = ModuleEligibility.Plan(ModuleCatalog.Discover(install), selection, install, null, _ => ModuleCapabilityState.Available).Modules.Single();
        Check(planned.Code == PublicErrorCodes.MODULE_CONFLICT && planned.Reason!.Contains(@"plugins\old.opl", StringComparison.Ordinal), "a module next to a conflicting plugin is rejected: " + planned.Code + " " + planned.Reason);
        var environment = new FakeEnvironment(_ => { });
        environment.LoadedPlugins.Add("OldPoC.dll");
        var (directory, sha, _) = InstallModule(install, "modern2", manifest: ManifestV2("modern2", ", \"conflicts\": { \"plugins\": [\"OldPoC.dll\"] }"));
        var host = new ModuleHost(environment);
        host.Load(new[] { new ModuleLoadRequest(directory, sha) });
        Check(host.Status.Count == 0 && environment.Events.Any(x => x.Name == "module.load.failed" && x.Data["code"] == PublicErrorCodes.MODULE_CONFLICT), "in process, a loaded conflicting plugin blocks the module");
    });

    Gate("host.settings-data-and-notices", () =>
    {
        var install = NewInstallation();
        var manifestText = ManifestV2("tuned", Settings("{ \"key\": \"sample\", \"type\": \"integer\", \"default\": 7 }, { \"key\": \"other\", \"type\": \"text\", \"default\": \"x\" }"),
            extraComponents: ", { \"kind\": \"native\", \"path\": \"native.dll\", \"sha256\": \"" + Sha("native") + "\" }");
        var (directory, sha, _) = InstallModule(install, "tuned", manifest: manifestText, extraFiles: new() { ["native.dll"] = "native" });
        IRuntimeModuleContext? seen = null;
        var environment = new FakeEnvironment(context => seen = context) { InstallationRoot = install };
        var host = new ModuleHost(environment);
        host.Load(new[] { new ModuleLoadRequest(directory, sha, new Dictionary<string, string> { ["sample"] = "3" }) });
        host.Arm();
        Check(seen is not null && seen.Settings["sample"] == "3" && seen.Settings["other"] == "x", "settings reach the module; a missing key gets its default");
        var data = seen!.DataDirectory;
        Check(data == Path.Combine(install, ".omsilaunch", "module-data", "tuned") && Directory.Exists(data), "the data directory is created on first use: " + data);
        Check(seen.ProductVersion == environment.ProductVersion, "product version");
        seen.Emit("notice", new Dictionary<string, string> { ["title"] = "Pre\u0007load", ["text"] = new string('x', 400), ["url"] = "https://example.invalid" });
        var notice = environment.Events.Single(x => x.Name == "module.tuned.notice").Data;
        Check(notice["title"] == "Pre load" && notice["text"].Length == ModuleNotices.MaxText && !notice.ContainsKey("url"), "a notice is bounded text without control characters or extra fields");
        var values = environment.NativeValues!;
        Check(values("setting.sample") == "3" && values("module_version") == "1.0.0" && values("session_id") == environment.SessionId.ToString("D") && values("no.such") is null, "native get_value");
        host.Stop();
    });

    // Session programs (component kind "host"): started before OMSI with the
    // context on stdin, awaited until ready, refused a second process by their
    // job, told OMSI's process id, stopped at the end of the session. Each
    // failure follows the module's policy.
    Gate("session-program.lifecycle", () =>
    {
        var install = NewInstallation();
        var program = SessionTestProgram();
        var ready = InstallSessionModule(install, "prep-ready", program, "degrade", 30_000, 5_000);
        var slow = InstallSessionModule(install, "prep-slow", program, "disable-module", 1_500, 5_000);
        var linger = InstallSessionModule(install, "prep-linger", program, "degrade", 30_000, 500);
        var tampered = InstallSessionModule(install, "prep-tampered", program, "disable-module", 30_000, 5_000, tamper: true);
        var plan = SessionPlanFor(install, new[] { ("prep-ready", ready, "ready", "degrade"), ("prep-slow", slow, "slow", "disable-module"), ("prep-linger", linger, "linger", "degrade"), ("prep-tampered", tampered, "ready", "disable-module") });
        var events = new List<EmittedEvent>(); var diagnostics = new List<LaunchDiagnostic>();
        IReadOnlyList<EmittedEvent> Events() { lock (events) return events.ToArray(); }
        var programs = ModuleSessionPrograms.Start(plan, (name, data) => { lock (events) events.Add(new(name, data)); }, diagnostic => { lock (diagnostics) diagnostics.Add(diagnostic); }, (_, _) => { }, CancellationToken.None);
        try
        {
            var seen = Events();
            Check(seen.Any(x => x.Name == "module.prep-ready.host.ready") && seen.Any(x => x.Name == "module.prep-linger.host.ready"), "ready programs report ready: " + string.Join(", ", seen.Select(x => x.Name)));
            var notice = seen.FirstOrDefault(x => x.Name == "module.prep-ready.notice")?.Data;
            Check(notice is not null && notice["title"] == "Test notice" && notice["text"] == @"first open of maps\Test\global.cfg", "a notice is sanitized and the context names the map");
            var progress = seen.FirstOrDefault(x => x.Name == "module.prep-ready.progress")?.Data;
            Check(progress is not null && progress["schema"] == ModuleSessionPrograms.ContextSchema && progress["channel"] == RuntimeModuleProtocol.SessionChannelName(plan.SessionId, "prep-ready"), "context schema and channel");
            Check(seen.FirstOrDefault(x => x.Name == "module.prep-ready.spawn")?.Data["result"] == "refused", "the job refuses a second process");
            Check(programs.Excluded.SetEquals(new[] { "prep-slow", "prep-tampered" }), "disable-module failures are excluded from OMSI: " + string.Join(",", programs.Excluded));
            Check(seen.Any(x => x.Name == "module.prep-slow.host.failed" && x.Data["code"] == PublicErrorCodes.MODULE_PHASE_TIMEOUT), "prepare timeout");
            Check(seen.Any(x => x.Name == "module.prep-tampered.host.failed" && x.Data["code"] == PublicErrorCodes.MODULE_INTEGRITY) && !seen.Any(x => x.Name == "module.prep-tampered.host.started"), "a program that does not match its pin is never started");
            programs.NotifyOmsiStarted(4242);
            var deadline = Environment.TickCount64 + 5_000;
            while (Environment.TickCount64 < deadline && !Events().Any(x => x.Name == "module.prep-ready.omsi")) Thread.Sleep(50);
            Check(Events().FirstOrDefault(x => x.Name == "module.prep-ready.omsi")?.Data["pid"] == "4242", "the program learns OMSI's process id");
        }
        finally { programs.Stop(); }
        var after = Events();
        Check(File.Exists(Path.Combine(install, ".omsilaunch", "module-data", "prep-ready", "stopped.txt")) && after.Any(x => x.Name == "module.prep-ready.host.exited" && x.Data["exit_code"] == "0"), "a program asked to stop finishes and exits");
        Check(after.Any(x => x.Name == "module.prep-linger.host.killed"), "a program that ignores stop is ended after host_stop_ms");
    });

    Gate("session-program.fail-session", () =>
    {
        var install = NewInstallation();
        var crash = InstallSessionModule(install, "prep-crash", SessionTestProgram(), "fail-session", 30_000, 5_000);
        var plan = SessionPlanFor(install, new[] { ("prep-crash", crash, "crash", "fail-session") });
        try
        {
            ModuleSessionPrograms.Start(plan, (_, _) => { }, _ => { }, (_, _) => { }, CancellationToken.None);
            Check(false, "a fail-session program that exits before ready must fail the start");
        }
        catch (InvalidOperationException exception) { Check(exception.Message.StartsWith(PublicErrorCodes.MODULE_SESSION_FAILED, StringComparison.Ordinal), "typed failure: " + exception.Message); }
    });
}
finally
{
    try { Directory.Delete(root, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
}

if (failures.Count != 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures)); Environment.ExitCode = 1; }
else Console.WriteLine("PASS module-tests (all)");

void LifecycleCase(string phase, string policy)
{
    var id = "case-" + phase.Replace("-", "") + "-" + policy.Replace("-", "");
    var (host, environment, module) = NewHost(id, policy, _ => { }, configure: fake =>
    {
        fake.ThrowIn = phase is "arm" or "gameplay-ready" or "operation" or "stop" ? phase : null;
        fake.SlowIn = phase switch { "arm-timeout" => "arm", "operation-timeout" => "operation", _ => null };
    });
    host.Arm(); host.GameplayReady();
    var operation = host.Execute("module." + id + ".ping", new Dictionary<string, string> { ["text"] = "x" });
    host.Stop();
    var state = host.Status.Single().State;
    var lifecycle = phase is "arm" or "gameplay-ready" or "arm-timeout";
    var runtime = phase is "operation" or "operation-timeout";
    // Under degrade an operation failure is reported as module.degraded.
    var failed = environment.Events.FirstOrDefault(x => x.Name == (policy == "degrade" && runtime ? "module.degraded" : "module.failed"));
    Check(failed is not null, "a failure event is emitted");
    if (phase.EndsWith("-timeout", StringComparison.Ordinal)) Check(failed?.Data["code"] == PublicErrorCodes.MODULE_PHASE_TIMEOUT, "overrun is a phase timeout: " + failed?.Data["code"]);
    // Resources are released on every path.
    Check(environment.Disposed.Count == module.Tracked, "every tracked resource is released: " + environment.Disposed.Count + "/" + module.Tracked);
    Check(environment.Events.Count(x => x.Name == "module.cleanup") == 1, "cleanup runs exactly once");
    if (phase == "stop") { Check(state == ModuleState.Stopped && host.Status.Single().LastErrorCode == PublicErrorCodes.MODULE_PHASE_FAILED, "a stop failure still ends stopped, with its code: " + state); Check(!host.SessionFailureRequested, "a failure while stopping does not fail the session"); return; }
    if (policy == "fail-session") { Check(host.SessionFailureRequested && state == ModuleState.Failed && environment.Events.Any(x => x.Name == "module.session-failure"), "fail-session fails the session"); return; }
    Check(!host.SessionFailureRequested, "only fail-session fails the session");
    if (policy == "degrade" && runtime) { Check(!operation.Succeeded && state == ModuleState.Stopped && environment.Events.Any(x => x.Name == "module.degraded"), "degrade keeps the module active after an operation failure (then stops normally): " + state); return; }
    Check(state == ModuleState.Disabled, (lifecycle ? "a lifecycle" : "an operation") + " failure disables the module: " + state);
    if (lifecycle) Check(operation.ErrorCode is PublicErrorCodes.MODULE_DISABLED, "a disabled module rejects operations: " + operation.ErrorCode);
}

(ModuleHost Host, FakeEnvironment Environment, FakeModule Module) NewHost(string id, string policy, Action<IRuntimeModuleContext> onArm, Action<FakeModule>? configure = null, bool auxiliary = false)
{
    var install = NewInstallation();
    var extra = auxiliary ? ", { \"kind\": \"auxiliary\", \"path\": \"helper.exe\", \"sha256\": \"" + Sha("helper") + "\" }" : "";
    var (directory, sha, _) = InstallModule(install, id, manifest: Manifest(id, extraComponents: extra).Replace("disable-module", policy), extraFiles: auxiliary ? new() { ["helper.exe"] = "helper" } : null);
    var environment = new FakeEnvironment(onArm);
    configure?.Invoke(environment.Module);
    var host = new ModuleHost(environment);
    host.Load(new[] { new ModuleLoadRequest(directory, sha) });
    Check(host.Status.Count == 1, "module loaded: " + string.Join(";", environment.Events.Select(x => x.Name + " " + string.Join(",", x.Data.Values))));
    return (host, environment, environment.Module);
}

string NewInstallation()
{
    var install = Path.Combine(root, Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(Path.Combine(install, "plugins", "OmsiLaunch.Modules"));
    return install;
}

(string Directory, string ManifestSha, string ComponentSha) InstallModule(string install, string id, string? manifest = null, Dictionary<string, string>? extraFiles = null)
{
    var directory = Path.Combine(install, "plugins", "OmsiLaunch.Modules", id);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, "sample.dll"), "sample");
    foreach (var (name, content) in extraFiles ?? new()) File.WriteAllText(Path.Combine(directory, name), content);
    var bytes = Encoding.UTF8.GetBytes(manifest ?? Manifest(id));
    File.WriteAllBytes(Path.Combine(directory, "module.json"), bytes);
    return (directory, Convert.ToHexString(SHA256.HashData(bytes)), Sha("sample"));
}

static string Sha(string content) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(content)));

// The built test session program (tests\OmsiLaunch.Module.SessionTest).
string SessionTestProgram()
{
    var repository = new DirectoryInfo(AppContext.BaseDirectory);
    while (repository is not null && !File.Exists(Path.Combine(repository.FullName, "OmsiLaunch.sln"))) repository = repository.Parent;
    var built = Directory.GetFiles(Path.Combine(repository!.FullName, "artifacts", "bin", "OmsiLaunch.Module.SessionTest"), "OmsiLaunch.Module.SessionTest.exe", SearchOption.AllDirectories)
        .Where(x => x.Contains(Path.DirectorySeparatorChar + "Release" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)).OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
    return built is null ? throw new InvalidOperationException("The test session program is not built.") : Path.GetDirectoryName(built)!;
}

// A protocol 2 module whose host component is the test session program.
string InstallSessionModule(string install, string id, string programDirectory, string policy, int prepareMs, int hostStopMs, bool tamper = false)
{
    var directory = Path.Combine(install, "plugins", "OmsiLaunch.Modules", id);
    Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory, "sample.dll"), "sample");
    foreach (var file in Directory.GetFiles(programDirectory, "OmsiLaunch.Module.SessionTest.*")) File.Copy(file, Path.Combine(directory, Path.GetFileName(file)));
    string FileSha(string name) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, name))));
    const string executable = "OmsiLaunch.Module.SessionTest.exe";
    var manifest = ManifestV2(id,
            Settings("{ \"key\": \"mode\", \"type\": \"choice\", \"choices\": [\"ready\", \"slow\", \"crash\", \"linger\"], \"default\": \"ready\" }") +
            ", \"assets\": [ { \"path\": \"OmsiLaunch.Module.SessionTest.dll\", \"sha256\": \"" + FileSha("OmsiLaunch.Module.SessionTest.dll") + "\" } ]",
            extraComponents: ", { \"kind\": \"host\", \"path\": \"" + executable + "\", \"sha256\": \"" + FileSha(executable) + "\" }")
        .Replace("disable-module", policy)
        .Replace("\"stop_ms\": 5000", "\"stop_ms\": 5000, \"prepare_ms\": " + prepareMs + ", \"host_stop_ms\": " + hostStopMs);
    var bytes = Encoding.UTF8.GetBytes(manifest);
    File.WriteAllBytes(Path.Combine(directory, "module.json"), bytes);
    if (tamper) File.AppendAllText(Path.Combine(directory, executable), "x");
    return Convert.ToHexString(SHA256.HashData(bytes));
}

// A runnable plan whose eligible modules are the given (id, manifest SHA-256,
// mode setting, policy).
static SessionPlan SessionPlanFor(string install, IEnumerable<(string Id, string ManifestSha, string Mode, string Policy)> modules)
{
    var none = new Dictionary<string, OptionalValue<string>>();
    var spec = new LaunchSpec(new(install), new(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Test\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(0)),
        new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset,
        new(none, none, none, none, none, none, none, none), new());
    var platform = new RuntimePlatformInfo("Windows", "10", "X64", "X64", "X86", "X86", true, false, true, true, true, true, true, true, true);
    var planned = modules.Select(x => new PlannedModule(x.Id, "1.0.0", PlannedModule.Eligible, true, x.Policy, Array.Empty<string>(), ManifestSha256: x.ManifestSha, Settings: new[] { new PlannedModuleSetting("mode", x.Mode, "request") })).ToArray();
    return new SessionPlan(Guid.NewGuid(), "Omsi23004", spec, platform, Array.Empty<ContentIdentity>(), new[] { "options.cfg" }, Array.Empty<string>(), Array.Empty<Capability>(), Array.Empty<Capability>(), Array.Empty<PlannedMutation>(), Array.Empty<LaunchDiagnostic>(), true, planned);
}

// A protocol 2 manifest (settings, conflicts, default activation).
static string ManifestV2(string id, string extraRoot, string extraComponents = "") => Manifest(id, extraComponents, extraRoot).Replace("\"min\": 1, \"max\": 1", "\"min\": 2, \"max\": 2");
static string Settings(string items) => ", \"settings\": [ " + items + " ]";

static string Manifest(string id, string extraComponents = "", string extraRoot = "") => "{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"" + id + "\", \"version\": \"1.0.0\", \"name\": \"Sample\", " +
    "\"bridge_protocol\": { \"min\": 1, \"max\": 1 }, \"activation\": \"requested\", \"required_capabilities\": [\"bridge.events\"], \"optional_capabilities\": [], " +
    "\"components\": [ { \"kind\": \"managed\", \"path\": \"sample.dll\", \"sha256\": \"" + Sha("sample") + "\", \"entry\": \"Sample.Module\" }" + extraComponents + " ], " +
    "\"failure_policy\": \"disable-module\", \"operations\": [ { \"id\": \"ping\", \"kind\": \"read\", \"arguments\": [ { \"name\": \"text\", \"required\": true } ] } ], " +
    "\"timeouts\": { \"arm_ms\": 5000, \"gameplay_ready_ms\": 5000, \"stop_ms\": 5000 }" + extraRoot + " }";

void Gate(string name, Action action)
{
    var before = failures.Count;
    try { action(); }
    catch (Exception exception) { failures.Add(name + ": " + exception); }
    for (var index = before; index < failures.Count; index++) if (!failures[index].StartsWith(name + ":", StringComparison.Ordinal)) failures[index] = name + ": " + failures[index];
    Console.WriteLine((failures.Count == before ? "PASS module." : "FAIL module.") + name);
}

void Check(bool condition, string message) { if (!condition) failures.Add(message); }

sealed record EmittedEvent(string Name, IReadOnlyDictionary<string, string> Data);

sealed class FakeEnvironment : IModuleHostEnvironment
{
    private readonly Action<IRuntimeModuleContext> onArm;
    public FakeEnvironment(Action<IRuntimeModuleContext> onArm) { this.onArm = onArm; Module = new FakeModule(this); }
    public FakeModule Module { get; }
    public List<EmittedEvent> Events { get; } = new();
    public List<string> Disposed { get; } = new();
    public HashSet<string> Unavailable { get; } = new();
    public bool AuxiliaryRefusesToExit { get; set; }
    public long Clock;
    public Guid SessionId { get; } = Guid.NewGuid();
    public string InstallationRoot { get; set; } = Path.Combine(Path.GetTempPath(), "omsilaunch-module-tests-unused");
    public string ProductVersion => "9.9.9-test";
    public HashSet<string> LoadedPlugins { get; } = new(StringComparer.OrdinalIgnoreCase);
    public bool IsPluginLibraryLoaded(string fileName) => LoadedPlugins.Contains(fileName);
    public Func<string, string?>? NativeValues { get; private set; }
    public bool IsCapabilityAvailable(string capability) => !Unavailable.Contains(capability);
    public string? LoadedMapIdentity { get; set; }
    public long TickMilliseconds => Clock;
    public void Emit(string name, IReadOnlyDictionary<string, string> data) => Events.Add(new(name, data));
    public IRuntimeModule CreateModule(ModuleManifest manifest, string directory) { Module.Arming = onArm; return Module; }
    public IModuleAuxiliaryProcess StartAuxiliaryProcess(string path, string arguments) => new FakeProcess(this);
    public List<string> NativeLog { get; } = new();
    public bool NativeStartFails { get; set; }
    public IModuleNativeComponent StartNativeComponent(ModuleManifest manifest, string directory, ModuleComponent component, Func<string, string?> values)
    {
        NativeValues = values;
        if (NativeStartFails) throw new ModuleOperationException(PublicErrorCodes.MODULE_PHASE_FAILED, "native start returned 1");
        NativeLog.Add("start");
        return new FakeNative(this);
    }

    private sealed class FakeNative : IModuleNativeComponent
    {
        private readonly FakeEnvironment owner;
        public FakeNative(FakeEnvironment owner) => this.owner = owner;
        public void GameplayReady() => owner.NativeLog.Add("gameplay-ready");
        public void Dispose() { owner.NativeLog.Add("dispose"); owner.Disposed.Add("native"); }
    }

    private sealed class FakeProcess : IModuleAuxiliaryProcess
    {
        private readonly FakeEnvironment owner; private bool exited;
        public FakeProcess(FakeEnvironment owner) => this.owner = owner;
        public int ProcessId => 4242;
        public bool HasExited => exited;
        public void Dispose() { owner.Disposed.Add("process"); exited = !owner.AuxiliaryRefusesToExit; }
    }
}

sealed class FakeModule : IRuntimeModule
{
    private readonly FakeEnvironment environment;
    public FakeModule(FakeEnvironment environment) => this.environment = environment;
    public Action<IRuntimeModuleContext>? Arming { get; set; }
    public string? ThrowIn { get; set; }
    public string? SlowIn { get; set; }
    public bool Stopped { get; private set; }
    public int Tracked { get; private set; }

    public void Arm(IRuntimeModuleContext context)
    {
        context.Track("first", new Named(environment, "first")); context.Track("second", new Named(environment, "second")); Tracked = 2;
        context.Emit("armed");
        Arming?.Invoke(context);
        Inject("arm");
    }
    public void GameplayReady() => Inject("gameplay-ready");
    public IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)
    {
        if (arguments.TryGetValue("text", out var text) && text == "typed") throw new ModuleOperationException(PublicErrorCodes.RUNTIME_VALUE_OUT_OF_RANGE, "typed");
        if (text == "foreign") throw new ModuleOperationException("SAMPLE_E_WHATEVER", "foreign");
        Inject("operation");
        return new Dictionary<string, string> { ["echo"] = text ?? "", ["internal_address"] = "0x1234" };
    }
    public void Stop() { Stopped = true; Inject("stop"); }

    private void Inject(string phase)
    {
        if (SlowIn == phase) environment.Clock += 60_000;
        if (ThrowIn == phase) throw new InvalidOperationException("injected failure in " + phase);
    }

    private sealed class Named : IDisposable
    {
        private readonly FakeEnvironment environment; private readonly string name;
        public Named(FakeEnvironment environment, string name) { this.environment = environment; this.name = name; }
        public void Dispose() => environment.Disposed.Add(name);
    }
}
