using System.IO.MemoryMappedFiles;
using System.Buffers.Binary;
using OmsiLaunch.Api;
using OmsiLaunch.Interop;
using OmsiLaunch.Plugin;

var handoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004_692EBFBF", WorldMode.NewMap, "maps\\Grundorf\\global.cfg", -1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "Nordspitze Bauernhof");
var bytes = StartupHandoffWire.Serialize(handoff); var name = "OmsiLaunch.PluginTest." + handoff.SessionId.ToString("N");
using var mapping = MemoryMappedFile.CreateNew(name, bytes.Length, MemoryMappedFileAccess.ReadWrite);
using (var view = mapping.CreateViewAccessor(0, bytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, bytes, 0, bytes.Length);
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", name);
var runtimeName = "OmsiLaunch.RuntimeTest." + handoff.SessionId.ToString("N");
using var runtimeMapping = MemoryMappedFile.CreateNew(runtimeName, 65_536, MemoryMappedFileAccess.ReadWrite);
Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", runtimeName);
Action? callback = null; var native = new FakeNative(); var events = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
var runtimeControl = new FakeRuntimeControl();
var runtime = new PluginRuntime(native, (eventName, data) => events.Add((eventName, data)), runtimeControl);
if (!runtime.Start(action => callback = action) || callback is null) throw new InvalidOperationException("PluginRuntime did not accept the portable handoff.");
if (!runtime.Start(_ => throw new InvalidOperationException("Duplicate PluginRuntime.Start must not schedule another startup.")) || native.BuildValidationCount != 1)
    throw new InvalidOperationException("PluginRuntime did not make duplicate startup notification idempotent.");
callback();
if (!native.Armed || native.Map != "maps\\Grundorf\\global.cfg" || native.Index != -1 || native.EntrypointIdentity != "Nordspitze Bauernhof" || !events.Any(x => x.Name == "gameplay.entered" && x.Data.TryGetValue("raw_label", out var label) && label == "Nordspitze Bauernhof")) throw new InvalidOperationException("PluginRuntime did not dispatch the semantic NEW_MAP request.");
Console.WriteLine("PASS plugin-runtime.handoff-new-map");

var savedHandoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004_692EBFBF", WorldMode.SavedSituation, "", -1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "", "situations\\23A 2005.osn");
var savedBytes = StartupHandoffWire.Serialize(savedHandoff);
if (!StartupHandoffWire.TryDeserialize(savedBytes, out var decodedSaved) || decodedSaved is null || decodedSaved.SituationIdentity != savedHandoff.SituationIdentity || decodedSaved.WorldMode != WorldMode.SavedSituation) throw new InvalidOperationException("Startup handoff v4 did not preserve saved-situation identity.");
var savedName = "OmsiLaunch.PluginTest." + savedHandoff.SessionId.ToString("N");
using var savedMapping = MemoryMappedFile.CreateNew(savedName, savedBytes.Length, MemoryMappedFileAccess.ReadWrite);
using (var view = savedMapping.CreateViewAccessor(0, savedBytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, savedBytes, 0, savedBytes.Length);
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", savedName);
var savedEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
Action? savedCallback = null; var savedNative = new FakeNative();
if (!new PluginRuntime(savedNative, (eventName, data) => savedEvents.Add((eventName, data))).Start(action => savedCallback = action) || savedCallback is null) throw new InvalidOperationException("PluginRuntime did not accept the profiled SAVED_SITUATION handoff.");
savedCallback();
if (savedNative.Situation != savedHandoff.SituationIdentity || !savedEvents.Any(x => x.Name == "gameplay.entered" && x.Data.TryGetValue("situation", out var situation) && situation == savedHandoff.SituationIdentity)) throw new InvalidOperationException("PluginRuntime did not dispatch the full saved-situation Start flow.");
Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", name);
Console.WriteLine("PASS handoff.saved-situation-v4");

// Beta 4: a start whose world-mode capability is not resolved on this build
// is rejected before any hook is armed, and names the missing capability.
var blockedEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
var blockedNative = new FakeNative { MissingCapabilities = "world.new-map" };
if (new PluginRuntime(blockedNative, (eventName, data) => blockedEvents.Add((eventName, data))).Start(_ => throw new InvalidOperationException("An unresolved capability must not schedule a start.")))
    throw new InvalidOperationException("PluginRuntime accepted a start whose capability is unresolved.");
if (blockedNative.Armed || !blockedEvents.Any(x => x.Name == "plugin.capability.unresolved" && x.Data.TryGetValue("capabilities", out var missing) && missing == "world.new-map"))
    throw new InvalidOperationException("An unresolved capability did not produce plugin.capability.unresolved.");
Console.WriteLine("PASS plugin-runtime.capability-unresolved-blocks-start");

// Runtime Modules v1 through the real bridge environment: the null test
// module is loaded from its own directory in its own load context, armed,
// told gameplay is ready, reached through module.<id>.<operation> dispatch,
// and stopped with its tracked resource released.
{
    var moduleRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-module-integration-" + Guid.NewGuid().ToString("N"));
    var savedHandoffName = Environment.GetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME");
    var savedChannel = Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL");
    try
    {
        var moduleDirectory = Path.Combine(moduleRoot, "plugins", "OmsiLaunch.Modules", "null-test");
        Directory.CreateDirectory(moduleDirectory);
        var component = Path.Combine(moduleDirectory, "OmsiLaunch.Module.NullTest.dll");
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), component);
        var componentSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(component)));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"null-test\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 1, \"max\": 1 }, \"required_capabilities\": [\"bridge.events\"], " +
            "\"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + componentSha + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" } ], \"failure_policy\": \"disable-module\", " +
            "\"operations\": [ { \"id\": \"ping\", \"kind\": \"read\", \"arguments\": [ { \"name\": \"text\" } ] }, { \"id\": \"fail\", \"kind\": \"action\" } ] }");
        File.WriteAllBytes(Path.Combine(moduleDirectory, "module.json"), manifestBytes);
        var manifestSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes));
        var moduleHandoff = handoff with { SessionId = Guid.NewGuid(), Modules = new[] { new StartupModule(@"plugins\OmsiLaunch.Modules\null-test", manifestSha) } };
        var moduleBytes = StartupHandoffWire.Serialize(moduleHandoff); var moduleName = "OmsiLaunch.ModuleTest." + moduleHandoff.SessionId.ToString("N");
        using var moduleMapping = MemoryMappedFile.CreateNew(moduleName, moduleBytes.Length, MemoryMappedFileAccess.ReadWrite);
        using (var view = moduleMapping.CreateViewAccessor(0, moduleBytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, moduleBytes, 0, moduleBytes.Length);
        Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", moduleName);
        Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", null);
        var moduleEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
        void Record(string eventName, IReadOnlyDictionary<string, string> data) { lock (moduleEvents) moduleEvents.Add((eventName, data)); }
        Action? moduleCallback = null;
        var moduleRuntime = new PluginRuntime(new FakeNative(), Record, new FakeRuntimeControl(), h => new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(h, Record)), moduleRoot);
        if (!moduleRuntime.Start(action => moduleCallback = action) || moduleCallback is null) throw new InvalidOperationException("A start with a module was not accepted: " + string.Join(", ", moduleEvents.Select(x => x.Name)));
        if (!moduleEvents.Any(x => x.Name == "module.null-test.armed") || moduleRuntime.Modules?.Status.Single().State != OmsiLaunch.Runtime.Modules.ModuleState.Armed) throw new InvalidOperationException("The module was not loaded and armed: " + string.Join(", ", moduleEvents.Select(x => x.Name + " " + string.Join(",", x.Data.Values))));
        moduleCallback();
        if (!moduleEvents.Any(x => x.Name == "module.null-test.gameplay-ready")) throw new InvalidOperationException("The module was not told gameplay is ready.");
        var dispatch = new PluginRuntime.ModuleDispatch(new FakeRuntimeControl(), moduleRuntime.Modules!);
        var ping = dispatch.Execute(new RuntimeCommand(moduleHandoff.SessionId, 1, "module.null-test.ping", new Dictionary<string, string> { ["text"] = "hello" }));
        if (!ping.Succeeded || ping.Values?["echo"] != "hello") throw new InvalidOperationException("module.null-test.ping did not reach the module.");
        var failed = dispatch.Execute(new RuntimeCommand(moduleHandoff.SessionId, 2, "module.null-test.fail", null));
        if (failed.Succeeded || failed.ErrorCode != PublicErrorCodes.RUNTIME_VALUE_OUT_OF_RANGE) throw new InvalidOperationException("A typed module failure lost its code: " + failed.ErrorCode);
        var product = dispatch.Execute(new RuntimeCommand(moduleHandoff.SessionId, 3, "time.read", null));
        if (!product.Succeeded) throw new InvalidOperationException("Product operations must still reach the runtime control.");
        moduleRuntime.Shutdown();
        if (!moduleEvents.Any(x => x.Name == "module.null-test.stopping") || !moduleEvents.Any(x => x.Name == "module.null-test.released") || !moduleEvents.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "complete"))
            throw new InvalidOperationException("The module was not stopped and released.");
        // A module.json changed after planning is refused in process.
        File.AppendAllText(Path.Combine(moduleDirectory, "module.json"), " ");
        var refusedEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
        var refused = new PluginRuntime(new FakeNative(), (n, d) => refusedEvents.Add((n, d)), new FakeRuntimeControl(), h => new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(h, (n, d) => refusedEvents.Add((n, d)))), moduleRoot);
        refused.Start(_ => { });
        if (!refusedEvents.Any(x => x.Name == "module.load.failed" && x.Data["code"] == PublicErrorCodes.MODULE_INTEGRITY)) throw new InvalidOperationException("A module.json changed after planning was loaded.");
        refused.Shutdown();
    }
    finally
    {
        Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", savedHandoffName);
        Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", savedChannel);
        try { Directory.Delete(moduleRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
Console.WriteLine("PASS plugin-runtime.modules-lifecycle");
// DNNE loads the plugin into an isolated load context, not the default one
// (round B found modules failing there: their contracts were looked up in the
// default context). The plugin and its product assemblies are loaded into
// such a context here, and a module is created through the bridge
// environment of that context: it must implement that context's contract.
{
    var moduleRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-isolated-module-" + Guid.NewGuid().ToString("N"));
    try
    {
        var directory = Path.Combine(moduleRoot, "plugins", "OmsiLaunch.Modules", "null-test");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"));
        var componentSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"))));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"null-test\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 1, \"max\": 1 }, " +
            "\"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + componentSha + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" } ], \"failure_policy\": \"disable-module\" }");
        File.WriteAllBytes(Path.Combine(directory, "module.json"), manifestBytes);
        var isolated = new IsolatedPluginContext(AppContext.BaseDirectory);
        var plugin = isolated.LoadFromAssemblyPath(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Plugin.dll"));
        var contracts = isolated.LoadFromAssemblyName(new System.Reflection.AssemblyName("OmsiLaunch.Runtime.Modules"));
        var api = isolated.LoadFromAssemblyName(new System.Reflection.AssemblyName("OmsiLaunch.Api"));
        if (contracts == typeof(OmsiLaunch.Runtime.Modules.ModuleHost).Assembly || api == typeof(StartupHandoff).Assembly) throw new InvalidOperationException("The test context is not isolated.");
        var discovered = contracts.GetType("OmsiLaunch.Runtime.Modules.ModuleCatalog", true)!.GetMethod("InspectPlanned")!.Invoke(null, new object?[] { directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes)), null })!;
        var manifest = discovered.GetType().GetProperty("Manifest")!.GetValue(discovered) ?? throw new InvalidOperationException("The isolated catalog did not read the manifest.");
        var worldMode = Enum.ToObject(api.GetType("OmsiLaunch.Api.WorldMode", true)!, 0);
        var dateMode = Enum.ToObject(api.GetType("OmsiLaunch.Api.DateTimeMode", true)!, 0);
        var handoffType = api.GetType("OmsiLaunch.Api.StartupHandoff", true)!;
        var isolatedHandoff = handoffType.GetConstructors().Single().Invoke(new object?[] { Guid.NewGuid(), "Omsi23004", worldMode, @"maps\Grundorf\global.cfg", 1, true, false, dateMode, dateMode, "", "", null, false, false, null, null });
        var environmentType = plugin.GetType("OmsiLaunch.Plugin.PluginModuleEnvironment", true)!;
        Action<string, IReadOnlyDictionary<string, string>> ignore = (_, _) => { };
        var environment = environmentType.GetConstructors(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic).Single().Invoke(new object?[] { isolatedHandoff, ignore, null });
        object? module;
        try { module = environmentType.GetMethod("CreateModule")!.Invoke(environment, new[] { manifest, directory }); }
        catch (System.Reflection.TargetInvocationException error) { throw new InvalidOperationException("A module cannot be created in an isolated plugin context: " + error.InnerException?.Message, error.InnerException); }
        if (module is null || !contracts.GetType("OmsiLaunch.Runtime.Modules.IRuntimeModule", true)!.IsInstanceOfType(module)) throw new InvalidOperationException("The module does not implement the isolated context's IRuntimeModule.");
    }
    finally { try { Directory.Delete(moduleRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
Console.WriteLine("PASS plugin-runtime.modules-isolated-load-context");
// A module that declares bridge.hooks.d3d9 makes the bridge observe the
// Direct3D device before gameplay-ready (round B: device hooks found no
// device); a module without it leaves the lazy D3D behavior unchanged.
foreach (var declaresDirect3D in new[] { false, true })
{
    var moduleRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-d3d-module-" + Guid.NewGuid().ToString("N"));
    var savedHandoffName = Environment.GetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME");
    var savedChannel = Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL");
    try
    {
        var directory = Path.Combine(moduleRoot, "plugins", "OmsiLaunch.Modules", "d3d-test");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"));
        var componentSha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"))));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"d3d-test\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 1, \"max\": 1 }, " +
            (declaresDirect3D ? "\"optional_capabilities\": [\"bridge.hooks.d3d9\"], " : "") +
            "\"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + componentSha + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" } ], \"failure_policy\": \"disable-module\" }");
        File.WriteAllBytes(Path.Combine(directory, "module.json"), manifestBytes);
        var d3dHandoff = handoff with { SessionId = Guid.NewGuid(), Modules = new[] { new StartupModule(@"plugins\OmsiLaunch.Modules\d3d-test", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes))) } };
        var d3dBytes = StartupHandoffWire.Serialize(d3dHandoff); var d3dName = "OmsiLaunch.D3DModuleTest." + d3dHandoff.SessionId.ToString("N");
        using var d3dMapping = MemoryMappedFile.CreateNew(d3dName, d3dBytes.Length, MemoryMappedFileAccess.ReadWrite);
        using (var view = d3dMapping.CreateViewAccessor(0, d3dBytes.Length, MemoryMappedFileAccess.Write)) view.WriteArray(0, d3dBytes, 0, d3dBytes.Length);
        Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", d3dName);
        Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", null);
        var d3dEvents = new List<string>();
        var control = new FakeRuntimeControl();
        Action? d3dCallback = null;
        var d3dRuntime = new PluginRuntime(new FakeNative(), (name, _) => d3dEvents.Add(name), control, h => new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(h, (name, _) => d3dEvents.Add(name))), moduleRoot);
        if (!d3dRuntime.Start(action => d3dCallback = action) || d3dCallback is null) throw new InvalidOperationException("The D3D module start was not accepted: " + string.Join(", ", d3dEvents));
        if (control.Direct3DActivated) throw new InvalidOperationException("Direct3D was observed before gameplay.");
        d3dCallback();
        if (control.Direct3DActivated != declaresDirect3D) throw new InvalidOperationException("Direct3D activation before gameplay-ready: expected " + declaresDirect3D + ".");
        if (!d3dEvents.Contains("module.d3d-test.gameplay-ready")) throw new InvalidOperationException("The module did not hear gameplay-ready.");
        d3dRuntime.Shutdown();
    }
    finally
    {
        Environment.SetEnvironmentVariable("OMSILAUNCH_HANDOFF_NAME", savedHandoffName);
        Environment.SetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL", savedChannel);
        try { Directory.Delete(moduleRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
    }
}
Console.WriteLine("PASS plugin-runtime.modules-direct3d-activation");

var command = new RuntimeCommand(handoff.SessionId, 7, "time.read"); var commandBytes = RuntimeCommandWire.SerializeRequest(command);
using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(4, commandBytes.Length); view.WriteArray(8, commandBytes, 0, commandBytes.Length); view.Write(0, 1); view.Flush(); }
// Lifecycle probing is deferred until OMSI crosses gameplay readiness.
Thread.Sleep(2_050);
runtime.PollRuntimeCommands();
using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.Read)) { if (view.ReadInt32(0) != 2) throw new InvalidOperationException("PluginRuntime did not complete runtime request."); var length = view.ReadInt32(4); var response = new byte[length]; view.ReadArray(8, response, 0, length); if (!RuntimeCommandWire.TryDeserializeResponse(response, out var result) || result is null || !result.Succeeded || result.RequestId != 7) throw new InvalidOperationException("Runtime command response did not preserve session binding."); }
if (!events.Any(entry => entry.Name == "d3d.ready")) throw new InvalidOperationException("PluginRuntime did not publish the runtime-control lifecycle event.");
RuntimeCommandResult DispatchFixture(ulong id, string operation, out int state, Guid? sessionOverride = null) => DispatchFixtureSized(id, operation, out state, out _, sessionOverride);
RuntimeCommandResult DispatchFixtureSized(ulong id, string operation, out int state, out int responseBytes, Guid? sessionOverride = null)
{
    responseBytes = 0;
    var request = RuntimeCommandWire.SerializeRequest(new RuntimeCommand(sessionOverride ?? handoff.SessionId, id, operation));
    using (var view = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(4, request.Length); view.WriteArray(8, request, 0, request.Length); view.Write(0, 1); view.Flush(); }
    runtime.PollRuntimeCommands();
    using var read = runtimeMapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite);
    state = read.ReadInt32(0);
    if (state != 2) return new RuntimeCommandResult(handoff.SessionId, 0, false);
    var length = read.ReadInt32(4); responseBytes = length; var bytes = new byte[length]; read.ReadArray(8, bytes, 0, length); read.Write(0, 0); read.Flush();
    return RuntimeCommandWire.TryDeserializeResponse(bytes, out var decoded) && decoded is not null ? decoded : throw new InvalidOperationException("fixture response could not be decoded");
}
var oversized = DispatchFixtureSized(21, "fixture.oversize", out var oversizedState, out var oversizedBytes);
if (oversizedState != 2 || oversized.Succeeded || oversized.ErrorCode != "OL_E_RUNTIME_RESPONSE_TOO_LARGE" || oversized.RequestId != 21) throw new InvalidOperationException("Oversized plugin response was not reported as a typed error.");
// The error envelope for "too large" must itself be small and bounded.
if (oversizedBytes <= 0 || oversizedBytes > 512) throw new InvalidOperationException("The too-large error envelope is not bounded: " + oversizedBytes + " bytes.");
var bounded = DispatchFixtureSized(28, "fixture.bounded-list-oversize", out var boundedState, out var boundedBytes);
var boundedRows = bounded.Values?.Keys.Where(key => key.StartsWith("track_entry.", StringComparison.Ordinal)).Select(key => int.Parse(key.Split('.')[1], System.Globalization.CultureInfo.InvariantCulture)).Distinct().OrderBy(row => row).ToArray() ?? Array.Empty<int>();
if (boundedState != 2 || !bounded.Succeeded || bounded.Values!["truncated"] != "true" || bounded.Values["count"] != "3000"
    || int.Parse(bounded.Values["returned_count"], System.Globalization.CultureInfo.InvariantCulture) != boundedRows.Length || boundedRows.Length == 0
    || !boundedRows.SequenceEqual(Enumerable.Range(0, boundedRows.Length)) || boundedBytes > 65_536 - 8)
    throw new InvalidOperationException("An oversized bounded list was not truncated to fit the slot (state " + boundedState + ", code " + bounded.ErrorCode + ", rows " + boundedRows.Length + ").");
// Beta 4 pagination: the next page starts at the first row that was dropped.
if (bounded.Values["next_cursor"] != "i:" + boundedRows.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)) throw new InvalidOperationException("A shortened list did not continue at its first dropped row: " + bounded.Values["next_cursor"]);
Console.WriteLine("PASS plugin-runtime.bounded-list-fits-slot");
// ListPage: cursor and limit parsing, and the page fields.
{
    var page = OmsiLaunch.Interop.ListPage.From(new Dictionary<string, string> { ["cursor"] = "i:256", ["limit"] = "10" });
    if (page.StartIndex() != 256 || page.Take(128) != 10 || OmsiLaunch.Interop.ListPage.From(new Dictionary<string, string> { ["limit"] = "999" }).Take(128) != 128) throw new InvalidOperationException("ListPage parsing failed.");
    foreach (var bad in new[] { "h:1", "i:-1", "x", "i:", "i:1a" })
    {
        try { new OmsiLaunch.Interop.ListPage(bad).StartIndex(); throw new InvalidOperationException("an invalid cursor was accepted: " + bad); }
        catch (ArgumentException error) when (PublicErrorCodes.FromMessage(error.Message) == PublicErrorCodes.RUNTIME_CURSOR_INVALID) { }
    }
    try { OmsiLaunch.Interop.ListPage.From(new Dictionary<string, string> { ["limit"] = "0" }); throw new InvalidOperationException("limit 0 was accepted"); }
    catch (ArgumentOutOfRangeException error) when (PublicErrorCodes.FromMessage(error.Message) == PublicErrorCodes.RUNTIME_VALUE_OUT_OF_RANGE) { }
    var middle = new Dictionary<string, string>(); OmsiLaunch.Interop.ListPage.CompleteIndex(middle, 300, 128, 128);
    var last = new Dictionary<string, string>(); OmsiLaunch.Interop.ListPage.CompleteIndex(last, 300, 256, 44);
    if (middle["next_cursor"] != "i:256" || middle["truncated"] != "true" || last.ContainsKey("next_cursor") || last["truncated"] != "false" || last["returned_count"] != "44") throw new InvalidOperationException("Page fields are wrong.");
    var handles = new Dictionary<string, string>(); OmsiLaunch.Interop.ListPage.CompleteHandles(handles, 5, 2, 17, more: true);
    if (handles["next_cursor"] != "h:17" || OmsiLaunch.Interop.ListPage.HandleNumber("rv-000017") != 17 || new OmsiLaunch.Interop.ListPage("h:17").AfterHandle() != 17) throw new InvalidOperationException("Handle page fields are wrong.");
}
Console.WriteLine("PASS runtime-lists.pagination");
// Native hook engine (Beta 4): IAT, stacked slot, foreign-overwrite and
// prologue/trampoline hooks on fixtures inside the native bridge itself.
unsafe
{
    var nativeDirectory = new DirectoryInfo(AppContext.BaseDirectory);
    while (nativeDirectory is not null && !File.Exists(Path.Combine(nativeDirectory.FullName, "OmsiLaunch.sln"))) nativeDirectory = nativeDirectory.Parent;
    var nativePath = nativeDirectory is null ? "" : Path.Combine(nativeDirectory.FullName, "artifacts", "x86", "Release", "OmsiLaunch.Native.x86.dll");
    if (!File.Exists(nativePath)) throw new InvalidOperationException("Native bridge not built: " + nativePath);
    var library = System.Runtime.InteropServices.NativeLibrary.Load(nativePath);
    var selfCheck = (delegate* unmanaged[Cdecl]<int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeHookSelfCheck");
    var passed = selfCheck();
    if (passed != 0x7F) throw new InvalidOperationException("Native hook self-check failed: 0x" + passed.ToString("X2", System.Globalization.CultureInfo.InvariantCulture));
    var count = (delegate* unmanaged[Cdecl]<nint, int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeHookCount");
    if (count(0) != 0) throw new InvalidOperationException("The self-check left hooks installed.");
}
Console.WriteLine("PASS native-hooks.self-check");
// The pause menu boundary without resolved targets: it fails closed and never
// reaches for OMSI (no table configured in this process).
unsafe
{
    var repositoryRoot = new DirectoryInfo(AppContext.BaseDirectory);
    while (repositoryRoot is not null && !File.Exists(Path.Combine(repositoryRoot.FullName, "OmsiLaunch.sln"))) repositoryRoot = repositoryRoot.Parent;
    var library = System.Runtime.InteropServices.NativeLibrary.Load(Path.Combine(repositoryRoot!.FullName, "artifacts", "x86", "Release", "OmsiLaunch.Native.x86.dll"));
    var state = (delegate* unmanaged[Cdecl]<int*, int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeUiPauseState");
    var takeOver = (delegate* unmanaged[Cdecl]<int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeUiPauseTakeOver");
    var restore = (delegate* unmanaged[Cdecl]<int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeUiPauseRestoreNative");
    var action = (delegate* unmanaged[Cdecl]<int, int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeUiPauseAction");
    var buffer = stackalloc int[10];
    for (var index = 0; index < 10; index++) buffer[index] = 0x7F;
    if (state(buffer) != 1 || buffer[0] != 1 || buffer[1] != 0) throw new InvalidOperationException("The pause state did not fail closed without targets.");
    if (takeOver() != 1 || restore() != 1) throw new InvalidOperationException("Take-over or restore did not fail closed without targets.");
    if (action(1) != 1 || action(5) != 1) throw new InvalidOperationException("An action was accepted without targets.");
    if (action(0) != 4 || action(6) != 4 || action(-1) != 4) throw new InvalidOperationException("An action outside 1..5 was not refused as invalid.");
    if (state(null) != 1) throw new InvalidOperationException("A null state buffer was accepted.");
    // Input capture without resolved targets: refused; with a stand-in window the hook records and swallows.
    var capture = (delegate* unmanaged[Cdecl]<int, int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeInputCapture");
    if (capture(1) != 1 || capture(0) != 0) throw new InvalidOperationException("Input capture did not fail closed without targets.");
    var probe = (delegate* unmanaged[Cdecl]<int>)System.Runtime.InteropServices.NativeLibrary.GetExport(library, "NativeInputSelfCheck");
    var probed = probe();
    if (probed != 0x1F) throw new InvalidOperationException("The input hook self-check failed: 0x" + probed.ToString("X"));
}
Console.WriteLine("PASS ui-pause.fails-closed");
Console.WriteLine("PASS ui-input.hook-self-check");
// A native module component through the real bridge environment: loaded,
// its IAT hook installed through the registry under module:<id>, refusals
// for unknown and undeclared targets and for a request outside start, and
// every hook removed before the DLL is unloaded.
unsafe
{
    var repositoryRoot = new DirectoryInfo(AppContext.BaseDirectory);
    while (repositoryRoot is not null && !File.Exists(Path.Combine(repositoryRoot.FullName, "OmsiLaunch.sln"))) repositoryRoot = repositoryRoot.Parent;
    var nativeModule = Path.Combine(repositoryRoot!.FullName, "artifacts", "x86", "Release", "tests", "OmsiLaunch.Module.NativeTest.dll");
    if (!File.Exists(nativeModule)) throw new InvalidOperationException("Native test module not built: " + nativeModule);
    var count = (delegate* unmanaged[Cdecl]<byte*, int>)System.Runtime.InteropServices.NativeLibrary.GetExport(System.Runtime.InteropServices.NativeLibrary.Load(Path.Combine(repositoryRoot.FullName, "artifacts", "x86", "Release", "OmsiLaunch.Native.x86.dll")), "NativeHookCount");
    int OwnerHooks(string owner) { var bytes = System.Text.Encoding.ASCII.GetBytes(owner + "\0"); fixed (byte* pointer = bytes) return count(pointer); }
    var moduleRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-native-module-" + Guid.NewGuid().ToString("N"));
    try
    {
        var directory = Path.Combine(moduleRoot, "plugins", "OmsiLaunch.Modules", "native-test");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"));
        File.Copy(nativeModule, Path.Combine(directory, "native.dll"));
        string Sha(string file) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, file))));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"native-test\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 2, \"max\": 2 }, \"required_capabilities\": [\"bridge.events\", \"bridge.hooks.iat\"], " +
            "\"settings\": [ { \"key\": \"sample\", \"type\": \"integer\", \"default\": 7, \"min\": 0, \"max\": 100000 } ], " +
            "\"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + Sha("OmsiLaunch.Module.NullTest.dll") + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" }, { \"kind\": \"native\", \"path\": \"native.dll\", \"sha256\": \"" + Sha("native.dll") + "\" } ], \"failure_policy\": \"disable-module\" }");
        File.WriteAllBytes(Path.Combine(directory, "module.json"), manifestBytes);
        var nativeEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
        var host = new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(handoff, (name, data) => nativeEvents.Add((name, data))));
        host.Load(new[] { new OmsiLaunch.Runtime.Modules.ModuleLoadRequest(directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes)), new Dictionary<string, string> { ["sample"] = "8192" }) });
        host.Arm();
        var started = nativeEvents.FirstOrDefault(x => x.Name == "module.native-test.started").Data;
        // get_value (bridge protocol 2): the planned setting, 12 with the size
        // needed for a buffer that is too small, 2 for an unknown name.
        if (started is null || started["iat"] != "0" || started["missing"] != "2" || started["undeclared"] != "11" || started["chain"] != "1"
            || started["setting"] != "0" || started["value"] != "8192" || started["small"] != "12" || started["needed"] == "0" || started["unknown"] != "2")
            throw new InvalidOperationException("Native component start: " + string.Join("; ", nativeEvents.Select(x => x.Name + " " + string.Join(",", x.Data.Select(d => d.Key + "=" + d.Value)))));
        if (OwnerHooks("module:native-test") != 1) throw new InvalidOperationException("The module's IAT hook is not in the registry.");
        host.GameplayReady();
        if (!nativeEvents.Any(x => x.Name == "module.native-test.ready")) throw new InvalidOperationException("gameplay_ready was not called.");
        host.Stop();
        var stopped = nativeEvents.FirstOrDefault(x => x.Name == "module.native-test.stopped").Data;
        if (stopped is null || stopped["late"] != "11") throw new InvalidOperationException("A hook request outside start was not refused.");
        if (OwnerHooks("module:native-test") != 0 || !nativeEvents.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "complete")) throw new InvalidOperationException("The module's hooks were not removed at cleanup.");
        // The DLL stays loaded and inert; its bridge table refuses late calls
        // (checked by the component itself: "late" = 11 above).
        if (nativeEvents.Any(x => x.Name == "module.hooks.retained")) throw new InvalidOperationException("Hooks were retained.");
    }
    finally { try { Directory.Delete(moduleRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
Console.WriteLine("PASS runtime-modules.native-component");

// Bridge protocol 3 through the real bridge environment: a catalog operation
// run in process by the native and the managed component (declared only), the
// OMSI version value, the tick on both halves and the managed -> native call.
unsafe
{
    var repositoryRoot = new DirectoryInfo(AppContext.BaseDirectory);
    while (repositoryRoot is not null && !File.Exists(Path.Combine(repositoryRoot.FullName, "OmsiLaunch.sln"))) repositoryRoot = repositoryRoot.Parent;
    var nativeModule = Path.Combine(repositoryRoot!.FullName, "artifacts", "x86", "Release", "tests", "OmsiLaunch.Module.NativeTest.dll");
    var moduleRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-protocol3-module-" + Guid.NewGuid().ToString("N"));
    try
    {
        var directory = Path.Combine(moduleRoot, "plugins", "OmsiLaunch.Modules", "native-test3");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"));
        File.Copy(nativeModule, Path.Combine(directory, "native.dll"));
        string Sha(string file) => Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, file))));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"native-test3\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 3, \"max\": 3 }, " +
            "\"required_capabilities\": [\"bridge.events\", \"bridge.hooks.iat\", \"bridge.runtime\", \"bridge.tick\"], \"runtime_operations\": [\"time.read\"], \"tick_ms\": 50, " +
            "\"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + Sha("OmsiLaunch.Module.NullTest.dll") + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" }, " +
            "{ \"kind\": \"native\", \"path\": \"native.dll\", \"sha256\": \"" + Sha("native.dll") + "\" } ], \"failure_policy\": \"disable-module\", " +
            "\"operations\": [ { \"id\": \"runtime-time\", \"kind\": \"read\" }, { \"id\": \"native-echo\", \"kind\": \"read\" } ] }");
        File.WriteAllBytes(Path.Combine(directory, "module.json"), manifestBytes);
        var p3Events = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
        var host = new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(handoff, (name, data) => p3Events.Add((name, data)), new FakeRuntimeControl()));
        host.Load(new[] { new OmsiLaunch.Runtime.Modules.ModuleLoadRequest(directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes))) });
        host.Arm();
        var expectedVersion = OmsiLaunch.Plugin.PluginOmsiVersion.Value;
        var started = p3Events.FirstOrDefault(x => x.Name == "module.native-test3.started3").Data;
        if (started is null || started["version"] != "0" || started["omsi"] != expectedVersion || started["run"] != "0" || started["succeeded"] != "1" || started["refused"] != "0" || started["declared"] != "0" || started["small"] != "12" || started["needed"] == "0" || started["device"] != "11")
            throw new InvalidOperationException("Native protocol 3 start: " + string.Join("; ", p3Events.Select(x => x.Name + " " + string.Join(",", x.Data.Select(d => d.Key + "=" + d.Value)))));
        host.GameplayReady();
        var p3Runtime = host.Execute("module.native-test3.runtime-time", null);
        if (!p3Runtime.Succeeded || p3Runtime.Values["succeeded"] != "true" || p3Runtime.Values["undeclared_code"] != PublicErrorCodes.MODULE_OPERATION_NOT_DECLARED || p3Runtime.Values["omsi_version"] != expectedVersion)
            throw new InvalidOperationException("Managed p3Runtime operations: " + p3Runtime.ErrorCode + " " + string.Join(",", p3Runtime.Values.Select(d => d.Key + "=" + d.Value)));
        var echo = host.Execute("module.native-test3.native-echo", null);
        if (!echo.Succeeded || !echo.Values["answer"].Contains("\"op\":\"echo\"") || !echo.Values["answer"].Contains("\"a\":\"b\""))
            throw new InvalidOperationException("Managed -> native execute: " + echo.ErrorCode + " " + string.Join(",", echo.Values.Select(d => d.Key + "=" + d.Value)));
        for (var tick = 0; tick < 3; tick++) { Thread.Sleep(70); host.Tick(); }
        if (!p3Events.Any(x => x.Name == "module.native-test3.ticked" && x.Data.ContainsKey("ticks")) || !p3Events.Any(x => x.Name == "module.native-test3.ticked" && x.Data.ContainsKey("elapsed_ms")))
            throw new InvalidOperationException("Native and managed ticks were not both delivered: " + string.Join("; ", p3Events.Select(x => x.Name)));
        host.Stop();
        if (!p3Events.Any(x => x.Name == "module.cleanup" && x.Data["result"] == "complete") || p3Events.Any(x => x.Name == "module.hooks.retained"))
            throw new InvalidOperationException("Protocol 3 module cleanup was not complete.");
        // After stop the bridge table refuses run_operation (checked on the managed side too).
        if (host.Execute("module.native-test3.runtime-time", null).ErrorCode != PublicErrorCodes.MODULE_DISABLED && host.Execute("module.native-test3.runtime-time", null).ErrorCode != PublicErrorCodes.MODULE_NOT_LOADED)
            throw new InvalidOperationException("A stopped module still ran an operation.");
    }
    finally { try { Directory.Delete(moduleRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
Console.WriteLine("PASS runtime-modules.protocol-3");

// Auxiliary programs (bridge protocol 3): created suspended, placed in the job
// object with the manifest's priority and memory limit, resumed afterwards;
// what the program starts is in the job too and ends with it.
{
    var auxRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-auxiliary-" + Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(auxRoot);
    try
    {
        var program = Path.Combine(auxRoot, "helper.exe");
        File.Copy(Path.Combine(Environment.SystemDirectory, "cmd.exe"), program);
        var auxEnvironment = new PluginModuleEnvironment(handoff, (_, _) => { });
        // cmd waits for ping, a child it starts at once.
        var process = auxEnvironment.StartAuxiliaryProcess(program, "/c ping -n 60 127.0.0.1 > nul", new OmsiLaunch.Runtime.Modules.ModuleAuxiliaryOptions(OmsiLaunch.Runtime.Modules.ModuleAuxiliaryPriority.BelowNormal, 256));
        var pingIds = Array.Empty<int>();
        for (var attempt = 0; attempt < 100 && pingIds.Length == 0; attempt++) { Thread.Sleep(50); pingIds = System.Diagnostics.Process.GetProcessesByName("PING").Select(x => x.Id).ToArray(); }
        if (pingIds.Length == 0) throw new InvalidOperationException("The auxiliary program's child never started.");
        using (var started = System.Diagnostics.Process.GetProcessById(process.ProcessId))
            if (started.PriorityClass != System.Diagnostics.ProcessPriorityClass.BelowNormal) throw new InvalidOperationException("The job's priority class was not applied: " + started.PriorityClass);
        if (process.HasExited) throw new InvalidOperationException("The auxiliary program exited early.");
        process.Dispose();
        var gone = false;
        for (var attempt = 0; attempt < 100 && !gone; attempt++)
        {
            Thread.Sleep(50);
            gone = pingIds.All(id => { try { using var p = System.Diagnostics.Process.GetProcessById(id); return p.HasExited; } catch (ArgumentException) { return true; } });
        }
        if (!process.HasExited || !gone) throw new InvalidOperationException("The auxiliary program or its child survived the job.");
    }
    finally { try { Directory.Delete(auxRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
Console.WriteLine("PASS runtime-modules.auxiliary-job");

// A module that names a loaded plugin DLL under conflicts.plugins is not
// loaded in process (OL_E_MODULE_CONFLICT); the plan checks the .opl files,
// this checks the libraries OMSI actually loaded.
{
    var conflictRoot = Path.Combine(Path.GetTempPath(), "omsilaunch-module-conflict-" + Guid.NewGuid().ToString("N"));
    try
    {
        var directory = Path.Combine(conflictRoot, "plugins", "OmsiLaunch.Modules", "conflict-test");
        Directory.CreateDirectory(directory);
        File.Copy(Path.Combine(AppContext.BaseDirectory, "OmsiLaunch.Module.NullTest.dll"), Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"));
        var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(Path.Combine(directory, "OmsiLaunch.Module.NullTest.dll"))));
        var manifestBytes = System.Text.Encoding.UTF8.GetBytes("{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"conflict-test\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 2, \"max\": 2 }, \"required_capabilities\": [\"bridge.events\"], " +
            "\"conflicts\": { \"plugins\": [\"kernel32.dll\"] }, \"components\": [ { \"kind\": \"managed\", \"path\": \"OmsiLaunch.Module.NullTest.dll\", \"sha256\": \"" + sha + "\", \"entry\": \"OmsiLaunch.Module.NullTest.NullTestModule\" } ], \"failure_policy\": \"disable-module\" }");
        File.WriteAllBytes(Path.Combine(directory, "module.json"), manifestBytes);
        var conflictEvents = new List<(string Name, IReadOnlyDictionary<string, string> Data)>();
        var host = new OmsiLaunch.Runtime.Modules.ModuleHost(new PluginModuleEnvironment(handoff, (name, data) => conflictEvents.Add((name, data))));
        host.Load(new[] { new OmsiLaunch.Runtime.Modules.ModuleLoadRequest(directory, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(manifestBytes))) });
        if (host.Status.Count != 0 || !conflictEvents.Any(x => x.Name == "module.load.failed" && x.Data["code"] == PublicErrorCodes.MODULE_CONFLICT))
            throw new InvalidOperationException("A module next to a conflicting loaded plugin was not refused: " + string.Join("; ", conflictEvents.Select(x => x.Name + " " + string.Join(",", x.Data.Select(d => d.Key + "=" + d.Value)))));
    }
    finally { try { Directory.Delete(conflictRoot, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { } }
}
Console.WriteLine("PASS runtime-modules.plugin-conflict");
var afterOversize = DispatchFixture(24, "time.read", out var afterOversizeState);
if (afterOversizeState != 2 || !afterOversize.Succeeded || afterOversize.RequestId != 24) throw new InvalidOperationException("Mailbox was not usable after an oversized response.");
var thrown = DispatchFixture(25, "fixture.throw", out var thrownState);
if (thrownState != 2 || thrown.Succeeded || thrown.ErrorCode != "OL_E_RUNTIME_OPERATION_FAILED" || thrown.RequestId != 25) throw new InvalidOperationException("An operation exception was not reported as a typed error.");
var foreign = DispatchFixture(26, "time.read", out var foreignState, Guid.NewGuid());
if (foreignState != 2 || foreign.Succeeded || foreign.ErrorCode != "OL_E_RUNTIME_SESSION_MISMATCH" || foreign.RequestId != 26) throw new InvalidOperationException("A request for another session was not reported as a typed error.");
var afterForeign = DispatchFixture(27, "time.read", out var afterForeignState);
if (afterForeignState != 2 || !afterForeign.Succeeded || afterForeign.RequestId != 27) throw new InvalidOperationException("Mailbox was not usable after a rejected request.");
DispatchFixture(22, "fixture.abandon", out var abandonedState);
if (abandonedState != 0) throw new InvalidOperationException("Plugin published a response for a request the host had abandoned.");
var afterAbandon = DispatchFixture(23, "time.read", out var afterState);
if (afterState != 2 || !afterAbandon.Succeeded || afterAbandon.RequestId != 23) throw new InvalidOperationException("Mailbox was not usable after an abandoned request.");
Console.WriteLine("PASS plugin-runtime.oversized-and-abandoned-responses");
runtime.Shutdown();
if (!runtimeControl.ShutdownObserved) throw new InvalidOperationException("PluginRuntime did not transfer shutdown ownership to runtime control.");
Console.WriteLine("PASS plugin-runtime.command-channel");

var memory = new FakeMemory();
memory.WriteUInt32(0x100, 0x200); memory.WriteInt32(0x1FC, 4); memory.WriteBytes(0x200, System.Text.Encoding.Unicode.GetBytes("Test"));
if (await memory.ReadStringAsync(0x100, OmsiStringEncoding.Unicode) != "Test") throw new InvalidOperationException("UnicodeString marshalling failed.");
memory.WriteUInt32(0x300, 0x400); memory.WriteInt32(0x3FC, 2); memory.WriteUInt32(0x400, 0x500); memory.WriteUInt32(0x404, 0x600);
memory.WriteInt32(0x4FC, 1); memory.WriteBytes(0x500, System.Text.Encoding.Unicode.GetBytes("A")); memory.WriteInt32(0x5FC, 1); memory.WriteBytes(0x600, System.Text.Encoding.Unicode.GetBytes("B"));
var strings = await memory.ReadStringArrayAsync(0x300, OmsiStringEncoding.Unicode);
if (strings.Length != 2 || strings[0] != "A" || strings[1] != "B") throw new InvalidOperationException("UnicodeString array marshalling failed.");
memory.WriteUInt32(0x700 + 0x04, 0x800); memory.WriteInt32(0x700 + 0x08, 2); memory.WriteInt32(0x7FC, 2); memory.WriteUInt32(0x800, 0x900); memory.WriteUInt32(0x804, 0);
var objects = await new OmsiPointerList<FakeObject>(memory, 0x700, OmsiPointerListLayout.DelphiTList, (source, address) => new FakeObject(source, address)).SnapshotAsync();
if (objects.Count != 2 || objects[0]?.Address != 0x900 || objects[1] is not null) throw new InvalidOperationException("TList pointer collection marshalling failed.");
memory.WriteUInt32(0xA00, 0xB00); memory.WriteInt32(0xA10, 42);
var profiled = new OmsiProfiledObject(memory, 0xA00, new OmsiObjectLayout("TestObject", 0xB00, new Dictionary<string, uint> { ["Answer"] = 0x10 }));
if (!await profiled.ValidateAsync() || await profiled.ReadFieldAsync<int>("Answer") != 42) throw new InvalidOperationException("Profiled object field read failed.");
await profiled.WriteFieldAsync("Answer", 43); if (await profiled.ReadFieldAsync<int>("Answer") != 43) throw new InvalidOperationException("Profiled object field write failed.");
memory.WriteUInt32(0xB10 + 0x04, 0xB40); memory.WriteInt32(0xB10 + 0x08, 1); memory.WriteUInt32(0xB40, 0xB80); memory.WriteInt32(0xB7C, 2); memory.WriteBytes(0xB80, new byte[] { (byte)'P', (byte)'T' });
var ansiList = await new OmsiStringList(memory, 0xB10, OmsiPointerListLayout.DelphiTList, OmsiStringEncoding.Ansi).SnapshotAsync();
if (ansiList.Count != 1 || ansiList[0] != "PT") throw new InvalidOperationException("TList string marshalling failed.");
Console.WriteLine("PASS interop.delphi-memory-primitives");

sealed class FakeNative : IPluginNativeServices
{
    public bool Armed { get; private set; } public string? Map { get; private set; } public int Index { get; private set; } public string? EntrypointIdentity { get; private set; }
    public int BuildValidationCount { get; private set; }
    // Capability availability is decided by the in-process resolution; the
    // fake reports every start capability as resolved unless told otherwise.
    public string MissingCapabilities { get; init; } = "";
    public bool ValidateStart(WorldMode mode, out string missing) { BuildValidationCount++; missing = MissingCapabilities; return missing.Length == 0; }
    public bool CheckLiveStartForm() => true;
    public bool ArmHeadlessStart() { Armed = true; return true; }
    public int StartNewMap(string mapIdentity, int presentedEntrypointIndex, string entrypointIdentity) { Map = mapIdentity; Index = presentedEntrypointIndex; EntrypointIdentity = entrypointIdentity; return 0; }
    public string? Situation { get; private set; }
    public int StartSavedSituation(string situationIdentity) { Situation = situationIdentity; return 0; }
    public bool TryGetLastEntrypointSelection(out NativeEntrypointSelection selection) { selection = new(1, 0, "", "Nordspitze Bauernhof"); return true; }
    public void InstallMainThreadGateway() { }
}

sealed class FakeRuntimeControl : IPluginRuntimeControl
{
    public bool Direct3DActivated { get; private set; }
    public void ActivateDirect3D() => Direct3DActivated = true;
    private bool emitted;
    public bool ShutdownObserved { get; private set; }
    public RuntimeCommandResult Execute(RuntimeCommand command)
    {
        // Correction-pass fixtures: an oversized result, and a request that the
        // host abandons (timeout reset) while the plugin is still executing it.
        if (command.Operation == "fixture.throw") throw new InvalidOperationException("simulated native fault inside an operation");
        if (command.Operation == "fixture.oversize") return new(command.SessionId, command.RequestId, true, Values: new Dictionary<string, string> { ["payload"] = new string('x', 70_000) });
        // Documentation audit BUG-05: a bounded list whose row bound still exceeds the slot.
        if (command.Operation == "fixture.bounded-list-oversize")
        {
            var rows = new Dictionary<string, string> { ["count"] = "3000", ["returned_count"] = "2000", ["truncated"] = "true" };
            for (var row = 0; row < 2000; row++) { rows["track_entry." + row + ".track"] = row.ToString(System.Globalization.CultureInfo.InvariantCulture); rows["track_entry." + row + ".name"] = new string('n', 60); }
            return new(command.SessionId, command.RequestId, true, Values: rows);
        }
        if (command.Operation == "fixture.abandon")
        {
            using var mapping = MemoryMappedFile.OpenExisting(Environment.GetEnvironmentVariable("OMSILAUNCH_RUNTIME_CHANNEL")!, MemoryMappedFileRights.ReadWrite);
            using var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite); view.Write(0, 0); view.Flush();
        }
        return new(command.SessionId, command.RequestId, true, Values: new Dictionary<string, string> { ["source"] = "fake" });
    }
    public IReadOnlyList<PluginRuntimeEvent> PollLifecycle()
    {
        if (emitted) return Array.Empty<PluginRuntimeEvent>();
        emitted = true;
        return new[] { new PluginRuntimeEvent("d3d.ready", new Dictionary<string, string> { ["state"] = "READY" }) };
    }
    public void Shutdown() => ShutdownObserved = true;
}

sealed class FakeMemory : IOmsiMemory
{
    private readonly byte[] bytes = new byte[0x1000];
    public ValueTask<int> ReadAsync(nint address, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        bytes.AsMemory((int)address, destination.Length).CopyTo(destination);
        return ValueTask.FromResult(destination.Length);
    }
    public ValueTask WriteAsync(nint address, ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        source.CopyTo(bytes.AsMemory((int)address, source.Length));
        return ValueTask.CompletedTask;
    }
    public void WriteUInt32(int address, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(address), value);
    public void WriteInt32(int address, int value) => BinaryPrimitives.WriteInt32LittleEndian(bytes.AsSpan(address), value);
    public void WriteBytes(int address, byte[] value) => value.CopyTo(bytes, address);
}

sealed class FakeObject : OmsiRemoteObject
{
    public FakeObject(IOmsiMemory memory, uint address) : base(memory, address) { }
}

// Loads OmsiLaunch assemblies from one directory into its own context, like
// the isolated component context DNNE uses for the plugin.
sealed class IsolatedPluginContext : System.Runtime.Loader.AssemblyLoadContext
{
    private readonly string directory;
    public IsolatedPluginContext(string directory) : base("OmsiLaunch.Tests.IsolatedPlugin") => this.directory = directory;
    protected override System.Reflection.Assembly? Load(System.Reflection.AssemblyName name)
    {
        if (name.Name is null || !name.Name.StartsWith("OmsiLaunch.", StringComparison.Ordinal) || name.Name == "OmsiLaunch.Module.NullTest") return null;
        var path = Path.Combine(directory, name.Name + ".dll");
        return File.Exists(path) ? LoadFromAssemblyPath(path) : null;
    }
}
