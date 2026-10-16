using System.Buffers.Binary;
using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Interop;

var makeVehicleSymbols = Omsi23004.Profile.Methods;
if (new SessionPresentationSpec().SuppressTrayIcon || !new SessionPresentationSpec(SuppressTrayIcon: true).SuppressTrayIcon)
    throw new InvalidOperationException("Session presentation tray-suppression API compatibility regression.");
if (!Omsi23004.Profile.CompatibleExecutableHashes.TryGetValue("7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759", out var steamVariant) || steamVariant != "Steam LAA" ||
    Omsi23004.Profile.CompatibleExecutableHashes.ContainsKey("0000000000000000000000000000000000000000000000000000000000000000"))
    throw new InvalidOperationException("Compatible OMSI LAA executable hash allow-list regression.");
if (Omsi23004.Profile.Globals["ProgMan"] != 0x00858BDC || Omsi23004.Profile.Globals["ProgManLiveObject"] != 0x00862F28 ||
    Omsi23004.Profile.Globals["RoadVehicles"] != 0x00861508 || Omsi23004.Profile.Globals["RoadVehicleTypes"] != 0x008615A8 ||
    makeVehicleSymbols["TTempRVListCreate"].Address != 0x0074A0E0 || makeVehicleSymbols["TProgManMakeVehicle"].Address != 0x0070A250 ||
    makeVehicleSymbols["CopyTempListIntoMainList"].Address != 0x0074A240 || Omsi23004.Profile.ObjectLayouts["MakeVehicle"]["TempListVmt"] != 0x0074802C ||
    Omsi23004.Profile.ObjectLayouts["MakeVehicle"]["ProgramManagerCriticalSection"] != 0x1B4)
    throw new InvalidOperationException("MakeVehicle profile reconciliation changed unexpectedly.");

if (!Omsi23004.Profile.Globals.ContainsKey("Drivers") || !Omsi23004.Profile.Globals.ContainsKey("TicketPack") ||
    !Omsi23004.Profile.Globals.ContainsKey("TimeTableLogs") || !Omsi23004.Profile.ObjectLayouts.ContainsKey("ScriptConstants") ||
    !Omsi23004.Profile.ObjectLayouts.ContainsKey("RoadVehicleDefinition") || !Omsi23004.Profile.ObjectLayouts.ContainsKey("Driver") ||
    !Omsi23004.Profile.ObjectLayouts.ContainsKey("TicketPack") || !Omsi23004.Profile.ObjectLayouts.ContainsKey("TimeTableLog") ||
    !Omsi23004.Profile.ObjectLayouts.ContainsKey("MapTile") || !Omsi23004.Profile.ObjectLayouts["Map"].ContainsKey("Tiles"))
    throw new InvalidOperationException("Wave A profiled runtime layouts are incomplete.");

if (Omsi23004.Profile.Globals["Map"] != 0x00861588)
    throw new InvalidOperationException("OmsiMap must resolve through the current-build OmsiGlobals.Map slot.");
if (Omsi23004.Profile.Globals["AiConfig"] != 0x0085912C ||
    Omsi23004.Profile.Globals["RawEntrypointIndex"] != 0x00858A50 ||
    Omsi23004.Profile.Methods["DownloadInternetTextures"].Address != 0x006E7164)
    throw new InvalidOperationException("Current native-only profile bindings were not catalogued.");

if (Omsi23004.Profile.Globals["D3DDevice"] != 0x008627D0 || !Omsi23004.Profile.HasEvidence("D3DDevice") ||
    PublicCapabilityRegistry.Get("d3d.texture").EffectiveOperations.Count != 5)
    throw new InvalidOperationException("Wave D device binding or public operation catalog is incomplete.");

if (Omsi23004.Profile.ObjectLayouts["Weather"]["ActiveWindSpeed"] != 0x18 ||
    Omsi23004.Profile.ObjectLayouts["Weather"]["ActiveWindDirection"] != 0x1C)
    throw new InvalidOperationException("Weather apply-lifecycle source candidates changed unexpectedly.");

// /trace-plugin and /trace-native travel in the handoff flags byte.
foreach (var (pluginTrace, nativeTrace) in new[] { (true, false), (false, true), (false, false) })
{
    var traced = new StartupHandoff(Guid.NewGuid(), "Omsi23004", WorldMode.NewMap, @"maps\Grundorf\global.cfg", 1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, PluginTrace: pluginTrace, NativeTrace: nativeTrace);
    if (!StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(traced), out var read) || read is null || read.PluginTrace != pluginTrace || read.NativeTrace != nativeTrace || !read.HeadlessStart || read.PlayerVehicleEnabled)
        throw new InvalidOperationException("Handoff trace flags do not round-trip.");
}

// Start date/time travels resolved in handoff v7.
{
    var handoff = new StartupHandoff(Guid.NewGuid(), "Omsi23004", WorldMode.NewMap, @"maps\Grundorf\global.cfg", 0, true, false, DateTimeMode.Explicit, DateTimeMode.Explicit, Start: new StartDateTime(2024, 61, 8, null, 30));
    if (!StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(handoff), out var roundTrip) || roundTrip?.Start != new StartDateTime(2024, 61, 8, null, 30))
        throw new InvalidOperationException("Handoff v7 start date/time does not round-trip.");
}

var weatherWriteCapability = PublicCapabilityRegistry.All.Single(capability => capability.Id == "weather.set");
// Beta 4: weather.set writes the source record and stays Experimental until
// runtime validated; it never claims runtime evidence it does not have.
if (weatherWriteCapability.Classification != PublicCapabilityClassification.PublicExperimental ||
    weatherWriteCapability.RuntimeValidation != PublicEvidence.RuntimeValidationRequired ||
    weatherWriteCapability.NativeCapability != "weather.set" ||
    !weatherWriteCapability.Operations!.Any(operation => operation.Id == "weather.metar.set"))
    throw new InvalidOperationException("Weather mutation must be the experimental source-record writer.");

// R3: the canonical catalog is internally consistent and every native
// capability it names exists in the native target catalog.
{
    var ids = PublicCapabilityRegistry.All.Select(x => x.Id).ToArray();
    if (ids.Length != ids.Distinct(StringComparer.Ordinal).Count()) throw new InvalidOperationException("Capability ids are not unique.");
    var operations = PublicCapabilityRegistry.All.SelectMany(x => x.EffectiveOperations).ToArray();
    if (operations.Length != operations.Select(x => x.Id).Distinct(StringComparer.Ordinal).Count()) throw new InvalidOperationException("Runtime operation ids are not unique.");
    var routes = operations.SelectMany(x => x.CliRoutes).ToArray();
    if (routes.Length != routes.Distinct(StringComparer.Ordinal).Count() || PublicCapabilityRegistry.CliRoutes.Count != routes.Length) throw new InvalidOperationException("CLI routes are not unique.");
    var native = NativeTargetCatalog.Capabilities.Select(x => x.Id).ToHashSet(StringComparer.Ordinal);
    foreach (var capability in PublicCapabilityRegistry.All)
    {
        if (!PublicEvidence.All.Contains(capability.RuntimeValidation)) throw new InvalidOperationException("Capability evidence outside the shared vocabulary: " + capability.Id + " " + capability.RuntimeValidation);
        foreach (var name in capability.NativeCapabilities) if (!native.Contains(name)) throw new InvalidOperationException("Native capability not in NativeTargetCatalog: " + capability.Id + " -> " + name);
        if (capability.EffectiveOperations.Count > 0 && capability.Classification is PublicCapabilityClassification.InternalOnly) throw new InvalidOperationException("Internal capability exposes public operations: " + capability.Id);
        if (capability.Classification is PublicCapabilityClassification.Unsupported && capability.RuntimeValidation != PublicEvidence.Unavailable) throw new InvalidOperationException("Unsupported capability must carry UNAVAILABLE: " + capability.Id);
    }
    foreach (var operation in operations)
    {
        if (!PublicMutationPolicy.All.Contains(operation.MutationPolicy)) throw new InvalidOperationException("Unknown mutation policy: " + operation.Id);
        var owner = PublicCapabilityRegistry.All.Single(x => x.EffectiveOperations.Contains(operation));
        if (owner.Kind == PublicCapabilityKind.Read && operation.MutationPolicy != PublicMutationPolicy.None) throw new InvalidOperationException("A read capability declares a mutation: " + operation.Id);
    }
    if (operations.Length != 54 || PublicCapabilityRegistry.PublicRuntimeOperationIds.Contains("internal.road-vehicles.make-basic")) throw new InvalidOperationException("Public runtime operation set changed.");
    Console.WriteLine("PASS capability-catalog.consistent");
}


var memory = new FakeMemory();
const uint list = 0x1000, wrapper = 0x2000, vehicleData = 0x3000, vehicle = 0x4000;
memory.WriteUInt32(Omsi23004.Profile.Globals["RoadVehicles"], list);
memory.WriteUInt32(list + Omsi23004.Profile.ObjectLayouts["RoadVehicleList"]["Items"], wrapper);
memory.WriteUInt32(wrapper + 4, vehicleData); memory.WriteInt32(wrapper + 8, 1); memory.WriteUInt32(vehicleData, vehicle);
memory.WriteInt32(vehicle + Omsi23004.Profile.ObjectLayouts["RoadVehicle"]["RuntimeIndex"], 42);
memory.WriteInt32(Omsi23004.Profile.Globals["PlayerVehicleIndex"], 0);

const uint humanData = 0x7000, human = 0x8000;
memory.WriteUInt32(Omsi23004.Profile.Globals["Humans"], humanData); memory.WriteInt32(humanData - 4, 1); memory.WriteUInt32(humanData, human);
memory.WriteInt32(human + Omsi23004.Profile.ObjectLayouts["Human"]["HumanIndex"], 7);
memory.WriteByte(human + Omsi23004.Profile.ObjectLayouts["Human"]["RenderMe"], 1); // in the world (a spawned slot)

var readers = new OmsiRuntimeReaders(memory, Omsi23004.Profile.Globals, Omsi23004.Profile.ObjectLayouts);
var missingHofHandle = PublicCapabilityRegistry.ValidateRuntimeArguments("vehicle.hofs.read", new Dictionary<string, string>());
if (missingHofHandle.Accepted || missingHofHandle.ErrorCode != "OL_E_RUNTIME_ARGUMENT_REQUIRED") throw new InvalidOperationException("HOF handle contract was not enforced.");
var validHofHandle = PublicCapabilityRegistry.ValidateRuntimeArguments("vehicle.hofs.read", new Dictionary<string, string> { ["handle"] = "rv-000001" });
if (!validHofHandle.Accepted) throw new InvalidOperationException("Valid HOF handle contract was rejected.");
var missingVariableArguments = PublicCapabilityRegistry.ValidateRuntimeArguments("vehicle.variable.set", new Dictionary<string, string> { ["handle"] = "rv-000001" });
if (missingVariableArguments.Accepted || missingVariableArguments.Message is null || !missingVariableArguments.Message.Contains("name", StringComparison.Ordinal) || !missingVariableArguments.Message.Contains("value", StringComparison.Ordinal))
    throw new InvalidOperationException("Numeric variable write contract did not require name and value.");
var missingTextureArguments = PublicCapabilityRegistry.ValidateRuntimeArguments("d3d.texture.create", new Dictionary<string, string> { ["width"] = "32" });
if (missingTextureArguments.Accepted || missingTextureArguments.Message is null || !missingTextureArguments.Message.Contains("height", StringComparison.Ordinal) || !missingTextureArguments.Message.Contains("format", StringComparison.Ordinal))
    throw new InvalidOperationException("Texture create contract did not require height and format.");
var missingSpawnModel = PublicCapabilityRegistry.ValidateRuntimeArguments("road-vehicles.spawn", new Dictionary<string, string>());
if (missingSpawnModel.Accepted || missingSpawnModel.ErrorCode != "OL_E_RUNTIME_ARGUMENT_REQUIRED")
    throw new InvalidOperationException("Public RoadVehicle spawn contract did not require a model identity.");
var missingCameraFamily = PublicCapabilityRegistry.ValidateRuntimeArguments("camera.lock", new Dictionary<string, string>());
if (missingCameraFamily.Accepted || missingCameraFamily.ErrorCode != "OL_E_RUNTIME_ARGUMENT_REQUIRED")
    throw new InvalidOperationException("Camera lock contract did not require a camera family.");
var vehicles = await readers.ListRoadVehiclesAsync();
if (!vehicles.TryGetValue("vehicle.0.handle", out var vehicleHandle)) throw new InvalidOperationException("Road vehicle handle was not assigned.");
var vehicleSnapshot = await readers.ReadRoadVehicleAsync(vehicleHandle);
if (vehicleSnapshot["runtime_index"] != "42") throw new InvalidOperationException("Road vehicle profile layout was not applied.");
var repeatedVehicles = await readers.ListRoadVehiclesAsync();
if (repeatedVehicles["vehicle.0.handle"] != vehicleHandle) throw new InvalidOperationException("Road vehicle handle changed across repeated snapshots.");
var playerVehicle = await readers.ReadPlayerVehicleAsync();
if (playerVehicle["present"] != "true" || playerVehicle["handle"] != vehicleHandle || playerVehicle["runtime_index"] != "42")
    throw new InvalidOperationException("Player vehicle did not retain the RoadVehicles snapshot identity.");
var cameraLock = new OmsiCameraLockWriter(memory, Omsi23004.Profile.Globals, Omsi23004.Profile.ObjectLayouts);
await cameraLock.ApplyAsync(0, 3);
if (await memory.ReadValueAsync<int>(Omsi23004.Profile.Globals["CameraFamily"]) != 0 ||
    await memory.ReadValueAsync<int>(vehicle + Omsi23004.Profile.ObjectLayouts["RoadVehicle"]["ActiveDriverCamera"]) != 3)
    throw new InvalidOperationException("Camera lock did not apply the driver family and preset.");
await cameraLock.ApplyAsync(2, null);
if (await memory.ReadValueAsync<int>(Omsi23004.Profile.Globals["CameraFamily"]) != 2)
    throw new InvalidOperationException("Camera lock did not apply the external family.");
try { await cameraLock.ApplyAsync(2, 1); throw new InvalidOperationException("Camera lock accepted a preset for an unsupported family."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED") { }

var humans = await readers.ListHumansAsync();
if (!humans.TryGetValue("human.0.handle", out var humanHandle)) throw new InvalidOperationException("Human handle was not assigned.");
var humanSnapshot = await readers.ReadHumanAsync(humanHandle);
if (humanSnapshot["human_index"] != "7") throw new InvalidOperationException("Human profile layout was not applied.");

var variables = Omsi23004.Profile.ObjectLayouts["ScriptVariables"];
const uint definition = 0x9000, state = 0xA000, variableNames = 0xB000, stringVariableNames = 0xB100, valueSlot = 0xC000, values = 0xD000, numericValue = 0xE000, stringValues = 0xF000;
memory.WriteUInt32(vehicle + variables["ComplMapObject"], definition);
memory.WriteUInt32(vehicle + variables["ComplObjectInstance"], state);
memory.WriteAnsiArray(definition + variables["VariableNames"], variableNames, "throttle");
memory.WriteAnsiArray(definition + variables["StringVariableNames"], stringVariableNames, "destination");
memory.WriteUInt32(vehicle + variables["PublicValues"], valueSlot); memory.WriteUInt32(valueSlot, values); memory.WriteInt32(values - 4, 1); memory.WriteUInt32(values, numericValue); memory.WriteSingle(numericValue, 12.5f);
memory.WriteUInt32(state + variables["StringValues"], stringValues); memory.WriteInt32(stringValues - 4, 1); memory.WriteUnicodeStringArrayEntry(stringValues, 0, "Spandau");
var listedVariables = await readers.ListRoadVehicleVariablesAsync(vehicleHandle, stringVariables: false);
if (listedVariables["name.0"] != "throttle" || listedVariables["truncated"] != "false") throw new InvalidOperationException("Numeric variable names were not decoded.");
var variable = await readers.ReadRoadVehicleVariableAsync(vehicleHandle, "throttle");
if (variable["value"] != "12.5") throw new InvalidOperationException("Numeric vehicle variable was not decoded.");
var writtenVariable = await readers.WriteRoadVehicleVariableAsync(vehicleHandle, "throttle", 7.25f);
if (writtenVariable["requested_value"] != "7.25" || writtenVariable["value"] != "7.25") throw new InvalidOperationException("Numeric vehicle variable write did not read back.");
var restoredVariable = await readers.WriteRoadVehicleVariableAsync(vehicleHandle, "throttle", 12.5f);
if (restoredVariable["value"] != "12.5") throw new InvalidOperationException("Numeric vehicle variable restore did not read back.");
var listedStringVariables = await readers.ListRoadVehicleVariablesAsync(vehicleHandle, stringVariables: true);
if (listedStringVariables["name.0"] != "destination") throw new InvalidOperationException("String variable names were not decoded.");
var stringVariable = await readers.ReadRoadVehicleStringVariableAsync(vehicleHandle, "destination");
if (stringVariable["value"] != "Spandau") throw new InvalidOperationException("String vehicle variable was not decoded.");

const uint timetableManager = 0x18000, timetableTracks = 0x18100, timetableEntries = 0x18200;
var timetableManagerLayout = Omsi23004.Profile.ObjectLayouts["TimeTableManager"];
var timetableTrackLayout = Omsi23004.Profile.ObjectLayouts["TimeTableTrack"];
var timetableEntryLayout = Omsi23004.Profile.ObjectLayouts["TimeTableTrackEntry"];
memory.WriteUInt32(Omsi23004.Profile.Globals["TimeTableManager"], timetableManager);
memory.WriteUInt32(timetableManager + timetableManagerLayout["Tracks"], timetableTracks); memory.WriteInt32(timetableTracks - 4, 1);
memory.WriteUInt32(timetableTracks + timetableTrackLayout["Entries"], timetableEntries); memory.WriteInt32(timetableEntries - 4, 1);
memory.WriteUInt32(timetableEntries + timetableEntryLayout["IdCode"], 77); memory.WriteInt32(timetableEntries + timetableEntryLayout["PathIndexOnObject"], 4);
memory.WriteInt32(timetableEntries + timetableEntryLayout["PathTile"], 9); memory.WriteInt32(timetableEntries + timetableEntryLayout["PathIndex"], 6);
memory.WriteSingle(timetableEntries + timetableEntryLayout["RelativeDistance"], 2.5f); memory.WriteSingle(timetableEntries + timetableEntryLayout["Distance"], 12.5f);
memory.WriteBool(timetableEntries + timetableEntryLayout["Valid"], true); memory.WriteByte(timetableEntries + timetableEntryLayout["PathOrderCheck"], 3);
memory.WriteInt32(timetableEntries + timetableEntryLayout["ChronoOrigin"], 5);
var trackEntries = await readers.ListTimeTableTrackEntriesAsync();
if (trackEntries["count"] != "1" || trackEntries["track_entry.0.id"] != "77" || trackEntries["track_entry.0.path_tile"] != "9" || trackEntries["track_entry.0.distance"] != "12.5") throw new InvalidOperationException("Timetable track-entry layout was not decoded.");

memory.WriteUInt32(vehicleData, 0);
try { await readers.ReadRoadVehicleAsync(vehicleHandle); throw new InvalidOperationException("Stale vehicle handle was accepted."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
memory.WriteUInt32(vehicleData, vehicle);
var replacementVehicles = await readers.ListRoadVehiclesAsync();
if (replacementVehicles["vehicle.0.handle"] == vehicleHandle) throw new InvalidOperationException("Reused native address revived a stale RoadVehicle handle.");

Console.WriteLine("PASS interop.profiled-entity-snapshots");

// S-19 address reuse (ABA): the object is destroyed and a different one is constructed at the
// same address between two list reads. The address never leaves the live collection, so only the
// identity pinned at handle allocation can reject the alias. Neither handle format changes.
var humanLayout = Omsi23004.Profile.ObjectLayouts["Human"];
var vehicleDefinitionLayout = Omsi23004.Profile.ObjectLayouts["RoadVehicleDefinition"];
var currentVehicleHandle = replacementVehicles["vehicle.0.handle"];
if ((await readers.ReadRoadVehicleAsync(currentVehicleHandle))["runtime_index"] != "42") throw new InvalidOperationException("Fresh vehicle handle did not resolve.");
memory.WriteUInt32(vehicle, 0x00700000);
try { await readers.ReadRoadVehicleAsync(currentVehicleHandle); throw new InvalidOperationException("Reused vehicle address (new VMT) was resolved through a stale handle."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
var reusedVehicles = await readers.ListRoadVehiclesAsync();
if (reusedVehicles["vehicle.0.handle"] == currentVehicleHandle || !reusedVehicles["vehicle.0.handle"].StartsWith("rv-", StringComparison.Ordinal))
    throw new InvalidOperationException("Reused vehicle address kept its stale handle.");
try { await readers.ReadRoadVehicleAsync(currentVehicleHandle); throw new InvalidOperationException("Stale vehicle handle was revived by a list read."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
if ((await readers.ReadRoadVehicleAsync(reusedVehicles["vehicle.0.handle"]))["runtime_index"] != "42") throw new InvalidOperationException("Replacement vehicle handle did not resolve.");
if ((await readers.ReadPlayerVehicleAsync())["handle"] != reusedVehicles["vehicle.0.handle"]) throw new InvalidOperationException("Player vehicle reported a retired handle for a reused address.");
// Same class, different definition (model) at the same address, with no resolve in between: the list itself must retire the alias.
currentVehicleHandle = reusedVehicles["vehicle.0.handle"];
memory.WriteUInt32(vehicle + vehicleDefinitionLayout["Definition"], 0x20000);
reusedVehicles = await readers.ListRoadVehiclesAsync();
if (reusedVehicles["vehicle.0.handle"] == currentVehicleHandle) throw new InvalidOperationException("Reused vehicle address (new definition) kept its stale handle on list.");
try { await readers.ReadRoadVehicleAsync(currentVehicleHandle); throw new InvalidOperationException("Stale vehicle handle survived a definition change."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
// Same class and model at the same address: only OMSI's never-reused IDCode tells the new instance apart.
currentVehicleHandle = reusedVehicles["vehicle.0.handle"];
memory.WriteUInt32(vehicle + Omsi23004.Profile.ObjectLayouts["RoadVehicle"]["IdCode"], 1234);
try { await readers.ReadRoadVehicleAsync(currentVehicleHandle); throw new InvalidOperationException("Stale vehicle handle survived an IDCode change."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }

if ((await readers.ReadHumanAsync(humanHandle))["human_index"] != "7") throw new InvalidOperationException("Fresh human handle did not resolve.");
// A despawned human (RenderMe 0) keeps its pooled slot: its handle must not resolve any more.
memory.WriteByte(human + humanLayout["RenderMe"], 0);
try { await readers.ReadHumanAsync(humanHandle); throw new InvalidOperationException("A despawned human resolved through its handle."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
memory.WriteByte(human + humanLayout["RenderMe"], 1);
memory.WriteUInt32(human, 0x00710000); memory.WriteInt32(human + humanLayout["HumanIndex"], 9);
try { await readers.ReadHumanAsync(humanHandle); throw new InvalidOperationException("Reused human address was resolved through a stale handle."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
var reusedHumans = await readers.ListHumansAsync();
if (reusedHumans["human.0.handle"] == humanHandle || !reusedHumans["human.0.handle"].StartsWith("hb-", StringComparison.Ordinal))
    throw new InvalidOperationException("Reused human address kept its stale handle.");
try { await readers.ReadHumanAsync(humanHandle); throw new InvalidOperationException("Stale human handle was revived by a list read."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
if ((await readers.ReadHumanAsync(reusedHumans["human.0.handle"]))["human_index"] != "9") throw new InvalidOperationException("Replacement human handle did not resolve to the new object.");
// Reuse with no resolve in between: the list itself must retire the alias.
var previousHumanHandle = reusedHumans["human.0.handle"];
memory.WriteInt32(human + humanLayout["HumanIndex"], 11);
reusedHumans = await readers.ListHumansAsync();
if (reusedHumans["human.0.handle"] == previousHumanHandle) throw new InvalidOperationException("Reused human address kept its stale handle on list.");
// Removal observed by a list read retires the alias even when an identical object later returns at the same address.
previousHumanHandle = reusedHumans["human.0.handle"];
memory.WriteInt32(humanData - 4, 0);
try { await readers.ReadHumanAsync(previousHumanHandle); throw new InvalidOperationException("Removed human was resolved through a stale handle."); }
catch (InvalidOperationException exception) when (exception.Message == "OL_E_RUNTIME_OBJECT_HANDLE_STALE") { }
memory.WriteInt32(humanData - 4, 1);
reusedHumans = await readers.ListHumansAsync();
if (reusedHumans["human.0.handle"] == previousHumanHandle) throw new InvalidOperationException("Reused native address revived a stale human handle.");

Console.WriteLine("PASS interop.handle-reuse-rejected");
NativeResolverTests.Run();

sealed class FakeMemory : IOmsiMemory
{
    private readonly Dictionary<uint, byte> bytes = new();
    public ValueTask<int> ReadAsync(nint address, Memory<byte> destination, CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < destination.Length; index++) destination.Span[index] = bytes.TryGetValue(checked((uint)address + (uint)index), out var value) ? value : (byte)0;
        return ValueTask.FromResult(destination.Length);
    }
    public ValueTask WriteAsync(nint address, ReadOnlyMemory<byte> source, CancellationToken cancellationToken = default)
    {
        for (var index = 0; index < source.Length; index++) bytes[checked((uint)address + (uint)index)] = source.Span[index];
        return ValueTask.CompletedTask;
    }
    public void WriteUInt32(uint address, uint value) => WriteBytes(address, BitConverter.GetBytes(value));
    public void WriteInt32(uint address, int value) => WriteBytes(address, BitConverter.GetBytes(value));
    public void WriteSingle(uint address, float value) => WriteBytes(address, BitConverter.GetBytes(value));
    public void WriteBool(uint address, bool value) => bytes[address] = value ? (byte)1 : (byte)0;
    public void WriteByte(uint address, byte value) => bytes[address] = value;
    public void WriteUnicodeArray(uint pointerField, uint dataAddress, string value)
    {
        WriteUInt32(pointerField, dataAddress); WriteInt32(dataAddress - 4, 1); WriteUnicodeStringArrayEntry(dataAddress, 0, value);
    }
    public void WriteAnsiArray(uint pointerField, uint dataAddress, string value)
    {
        WriteUInt32(pointerField, dataAddress); WriteInt32(dataAddress - 4, 1); WriteAnsiStringArrayEntry(dataAddress, 0, value);
    }
    public void WriteAnsiStringArrayEntry(uint arrayData, int index, string value)
    {
        var dataAddress = checked(0x16000u + (arrayData - 0xB000u) + (uint)index * 0x100u);
        WriteUInt32(arrayData + checked((uint)index * 4), dataAddress); WriteInt32(dataAddress - 4, value.Length); WriteBytes(dataAddress, System.Text.Encoding.ASCII.GetBytes(value));
    }
    public void WriteUnicodeStringArrayEntry(uint arrayData, int index, string value)
    {
        var dataAddress = checked(0x11000u + (arrayData - 0xB000u) + (uint)index * 0x100u);
        WriteUInt32(arrayData + checked((uint)index * 4), dataAddress); WriteInt32(dataAddress - 4, value.Length); WriteBytes(dataAddress, System.Text.Encoding.Unicode.GetBytes(value));
    }
    private void WriteBytes(uint address, byte[] value) { for (var index = 0; index < value.Length; index++) bytes[address + (uint)index] = value[index]; }
}
