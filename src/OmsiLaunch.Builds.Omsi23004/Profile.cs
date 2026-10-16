using System.Security.Cryptography;

namespace OmsiLaunch.Builds.Omsi23004;

public sealed record ExecutableFingerprint(long Size, string Sha256, string PeFileVersion, string ProductRuntimeVersion)
{
    public bool Matches(FileInfo executable)
    {
        if (!executable.Exists || executable.Length != Size) return false;
        using var stream = executable.OpenRead();
        using var algorithm = SHA256.Create();
        return string.Equals(Convert.ToHexString(algorithm.ComputeHash(stream)), Sha256, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record NativeSymbol(string Name, uint Address, byte[]? ExpectedBytes = null);
public sealed record FormProfile(string Name, uint LiveSlot, uint Vmt, IReadOnlyDictionary<string, uint> Fields, IReadOnlyDictionary<string, NativeSymbol> Methods);
public sealed record OmsiBuildProfile(string Identity, ExecutableFingerprint Executable, IReadOnlyDictionary<string, uint> Globals, IReadOnlyDictionary<string, FormProfile> Forms, IReadOnlyDictionary<string, NativeSymbol> Methods, IReadOnlyDictionary<string, NativeSymbol> Callsites)
{
    // Some official LAA distributions differ only in executable headers. They
    // share this profiled native layout, but remain exact SHA-256 allow-list
    // entries rather than broad version-based acceptance.
    public IReadOnlyDictionary<string, string> CompatibleExecutableHashes { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public bool MatchesExecutable(FileInfo executable)
    {
        if (Executable.Matches(executable)) return true;
        if (!executable.Exists) return false;
        using var stream = executable.OpenRead();
        using var algorithm = SHA256.Create();
        return CompatibleExecutableHashes.ContainsKey(Convert.ToHexString(algorithm.ComputeHash(stream)));
    }
    // Object field offsets are build metadata just like method targets. A
    // layout contains no public API contract and is consumed only in-process.
    public IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> ObjectLayouts { get; init; } = new Dictionary<string, IReadOnlyDictionary<string, uint>>(StringComparer.Ordinal);
    // Semantic evidence lookup only. It intentionally does not disclose a raw
    // address to Core/API consumers.
    public bool HasEvidence(string semanticName) =>
        Globals.ContainsKey(semanticName) ||
        Methods.ContainsKey(semanticName) ||
        Callsites.ContainsKey(semanticName) ||
        Forms.Values.Any(form => form.Fields.ContainsKey(semanticName) || form.Methods.ContainsKey(semanticName));
}

public static class Omsi23004
{
    public const string ProfileIdentity = "Omsi23004_692EBFBF";
    public static readonly OmsiBuildProfile Profile = new(
        ProfileIdentity,
        new ExecutableFingerprint(8_503_440, "692EBFBF2CD32FAB05A8B934E52C2BE14594E939882F3DBF2BA4E2B66CCC6243", "2.2.032", "2.3.004"),
        new Dictionary<string,uint>(StringComparer.Ordinal) { ["SelectedMap"] = 0x008591A4, ["ProgMan"] = 0x00858BDC,
            // The current-build OmsiGlobals.Map slot. 0x00859D94 is not an
            // OmsiMap object and produced structurally invalid map snapshots.
            ["Map"] = 0x00861588, ["SetPos"] = 0x00859BC8, ["RawEntrypoints"] = 0x00858F34, ["RawEntrypointIndex"] = 0x00858A50,
            ["StartTransition"] = 0x00858DA8, ["AiConfig"] = 0x0085912C,
            // Fixed scalar locations reconciled against the pinned OmsiHook
            // time wrapper and the matching static extraction.
            ["TimeHour"] = 0x0086176C, ["TimeMinute"] = 0x0086176D, ["TimeSecond"] = 0x00861770,
            ["TimeDay"] = 0x00861778, ["TimeMonth"] = 0x0086178C, ["TimeYear"] = 0x00861790,
            ["RoadVehicles"] = 0x00861508, ["RoadVehicleTypes"] = 0x008615A8,
            // ProgMan indirection resolves through 0x00858BDC to this direct
            // live-object slot. The distinction is proven in MakeVehicle-
            // Reconciliation-002 and is material to the MakeVehicle ABI.
            ["ProgManLiveObject"] = 0x00862F28,
            ["PlayerVehicleIndex"] = 0x00861740, ["Weather"] = 0x008617D0,
            ["ActualWeather"] = 0x00861278, ["Humans"] = 0x0086172C, ["TimeTableManager"] = 0x008614E8,
            ["Drivers"] = 0x008614F8, ["SelectedDriver"] = 0x008614FC, ["TicketPack"] = 0x008611FC,
            ["TimeTableLogs"] = 0x00861750,
            ["Camera"] = 0x008616E0, ["CameraFamily"] = 0x008616A4,
            // Exact-build runtime validation 4378899d-6e99-4a18-a554-b70eb933fe3e:
            // the slot yields an IUnknown whose IDirect3DDevice9 QI and
            // TestCooperativeLevel both return S_OK. Ownership is acquired
            // only through QI; the slot itself is never released.
            ["D3DDevice"] = 0x008627D0 },
        new Dictionary<string,FormProfile>(StringComparer.Ordinal)
        {
            ["Start"] = new("Tform_start",0x008612F8,0x006759C8,new Dictionary<string,uint>{{"MapStrings",0x3F0},{"MapCompanions",0x3F4},{"SituationCollection",0x3F8},{"SituationControl",0x3E4},{"ModeNewMap",0x434},{"ModeSelectedMap",0x438},{"ModeSavedSituation",0x43C},{"ModalResult",0x2C0},{"Visible",0x61}},new Dictionary<string,NativeSymbol>{{"FormCreate",new("Tform_start.FormCreate",0x0067739C)},{"FormShow",new("Tform_start.FormShow",0x00676FC4)},{"ButtonDriverNewClick",new("Tform_start.ButtonDriverNewClick",0x00676884)},{"SituationSelectionChanged",new("Tform_start.SituationSelectionChanged",0x00679250)},{"Button1Click",new("Tform_start.Button1Click",0x006768F8)},{"RadioSetChecked",new("TRadioButton.SetChecked",0x0059BE58)},{"FormHide",new("Tform_start.FormHide",0x006785B4)},{"ShowModal",new("TCustomForm.ShowModal",0x00551D18)}}),
            ["SetPos"] = new("Tform_setpos",0x00859BC8,0x00679EB8,new Dictionary<string,uint>{{"ListBox",0x398},{"AskForPosition",0x3A0},{"SelectedText",0x3A4}},new Dictionary<string,NativeSymbol>{{"FormShow",new("Tform_setpos.FormShow",0x0067A6F0)},{"Button1Click",new("Tform_setpos.Button1Click",0x0067A2E8)},{"ShowModal",new("TCustomForm.ShowModal",0x00551D18)}})
        },
        new Dictionary<string,NativeSymbol>(StringComparer.Ordinal) { ["CloseMap"] = new("TProgMan.CloseMap",0x006E57B0), ["NewSituation"] = new("TProgMan.NewSituation",0x006E5C0C), ["CalcAISum"] = new("TProgMan.CalcAISum",0x00720F74), ["SetAIVehicles"] = new("TProgMan.SetAIVehicles",0x00709350), ["SetTime"] = new("TProgMan.SetTime",0x00707B94), ["SetActualDateTime"] = new("TProgMan.SetActualDateTime",0x0070885C), ["InitializeScheduledAI"] = new("TProgMan.InitializeScheduledAI",0x0070D160), ["MapLoadGlobalFile"] = new("TMap.loadGlobalFile",0x007860B0), ["StartLoadSelectedSituation"] = new("Tform_start.LoadSelectedSituation",0x0064307C), ["TProgManMakeVehicle"] = new("TProgMan.MakeVehicle",0x0070A250), ["TTempRVListCreate"] = new("TTempRVList.Create",0x0074A0E0), ["CopyTempListIntoMainList"] = new("TProgMan.CopyTempListIntoMainList",0x0074A240), ["TProgManPlaceRandomBus"] = new("TProgMan.PlaceRandomBus",0x00708F8C), ["SetBusPosition"] = new("TProgMan.SetBusPosition",0x007099F4), ["DownloadInternetTextures"] = new("TProgMan.DownloadInternetTextures",0x006E7164,new byte[]{0x55,0x8B,0xEC,0xB9,0x15}), ["DelphiGetMem"] = new("System.GetMem",0x00404614), ["ManagedAssignAnsi"] = new("System.@UStrToLStr",0x004092CC) },
        new Dictionary<string,NativeSymbol>(StringComparer.Ordinal) { ["StartInitialShowModal"] = new("TProgMan.Initialisieren.Start.ShowModal",0x006E8573), ["SetVisibleTrue"] = new("TCustomForm.Show.SetVisible",0x00551C2F,new byte[]{0xE8,0x74,0xB0,0xFF,0xFF}), ["NewSituationAskForPosition"] = new("TProgMan.NewSituation.askForPos",0x006E60F5) })
    {
        CompatibleExecutableHashes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["7DAB063D1F62E73B3A2C7A6AC1921D7EDF5E5DB0FBC731481D117EEC8DE7D759"] = "Steam LAA"
        },
        // Reconciled from the pinned OmsiHook wrappers against this exact
        // profile's global map. Read-only fields are deliberately prioritized.
        ObjectLayouts = new Dictionary<string, IReadOnlyDictionary<string, uint>>(StringComparer.Ordinal)
        {
            ["Map"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Loaded"] = 0x120, ["LoadedTiles"] = 0xF8, ["Tiles"] = 0x118, ["LeftHandTraffic"] = 0x35, ["CenterTile"] = 0x144, ["Name"] = 0x150, ["Filename"] = 0x154, ["FriendlyName"] = 0x158, ["Description"] = 0x15C, ["MaxSpeed"] = 0x1B0, ["YearStart"] = 0x1BC, ["YearEnd"] = 0x1C0, ["RealYearOffset"] = 0x1C4 },
            // OmsiMap.Kacheln is a Delphi pointer array. Tile snapshots are
            // deliberately limited to load-state and source identity; object,
            // path and render subgraphs remain separate bounded capabilities.
            ["MapTile"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["X"] = 0xD0, ["Y"] = 0xD4, ["Index"] = 0x144, ["Filename"] = 0x2C, ["Version"] = 0x30, ["Unsaved"] = 0x34, ["Loaded"] = 0x38, ["LoadRequest"] = 0x39, ["Failed"] = 0x3A, ["ThreadLoading"] = 0x3B, ["PreparedForOde"] = 0x3E, ["WidthSouth"] = 0x40, ["WidthNorth"] = 0x44, ["Objects"] = 0x5C, ["PathGroups"] = 0x60, ["PathSegments"] = 0x64, ["SurfaceObjects"] = 0x78, ["SplineSegments"] = 0x7C },
            ["Weather"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["PrimaryLightFactor"] = 0x58, ["SecondaryLightFactor"] = 0x5C, ["AmbientLightFactor"] = 0x60, ["FogDensity"] = 0xA4, ["Lightness"] = 0xA8, ["CloudType"] = 0xB4, ["CloudTransparency"] = 0xCC, ["CloudCategory"] = 0xD0, ["PrecipitationSet"] = 0xD8, ["WetGround"] = 0x148, ["WindSpeed"] = 0x150, ["WindDirection"] = 0x154, ["RelativeHumidity"] = 0x164, ["AbsoluteHumidity"] = 0x168,
                // ActWeather is a value record at +0x08. The fields at
                // +0x150/+0x154 are derived current wind values; the weather
                // loop replaces them from this record. Writes therefore use
                // the source fields below, not their derived projections.
                ["ActiveWindSpeed"] = 0x18, ["ActiveWindDirection"] = 0x1C,
                ["ActiveTemperature"] = 0x20, ["ActiveDewPoint"] = 0x24, ["ActivePressure"] = 0x28, ["ActivePrecipitation"] = 0x2C, ["ActivePrecipitationRate"] = 0x2D,
                // The rest of the source record (research report beta4-static
                // weather-and-delphi-strings.md section 1). FogDensity/Lightness
                // above (+0xA4/+0xA8) are their per-tick projections.
                ["ActiveFogDensity"] = 0x10, ["ActiveLightness"] = 0x14, ["ActiveCloudType"] = 0x3C, ["ActiveCloudHeight"] = 0x40,
                ["ActiveGroundMoisture"] = 0x48, ["ActiveGroundMoistureCover"] = 0x4C, ["ActiveGroundWetness"] = 0x50, ["ActiveSnow"] = 0x54, ["ActiveSnowOnRoads"] = 0x55 },
            ["ActualWeather"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Active"] = 0x8, ["Icao"] = 0xC, ["LastDownloaded"] = 0x10, ["InvalidIcao"] = 0x18, ["Counter"] = 0x1C, ["Process"] = 0x20 },
            ["Camera"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Name"] = 0x4, ["Ratio"] = 0x8, ["FieldOfView"] = 0x38, ["NormalFieldOfView"] = 0x31C, ["Distance"] = 0x24 },
            // TMyOmsiList owns a MemArrayList at +0x28. The latter is a
            // three-field wrapper (data pointer at +0x4, length at +0x8),
            // rather than a Delphi dynamic array directly at this offset.
            ["RoadVehicleList"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Items"] = 0x28, ["Count"] = 0x2C, ["Ai"] = 0x34 },
            ["MakeVehicle"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["TempListVmt"] = 0x0074802C, ["ProgramManagerCriticalSection"] = 0x1B4 },
            // OmsiRoadVehicleInst inherits the moving/map-object fields.
            // These offsets are read-only release telemetry; spatial writes
            // remain deliberately outside the profiled public surface.
            // IdCode (+0x1E4): process-unique, never reused instance number of every map object
            // (research report beta4-static runtime-map-data-shutdown.md section 1.2).
            ["RoadVehicle"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["IdCode"] = 0x1E4, ["Position"] = 0x4, ["Rotation"] = 0x50, ["Tile"] = 0x74, ["RuntimeIndex"] = 0x258, ["MarkedForKilling"] = 0x25C, ["Steering"] = 0x268, ["TrafficType"] = 0x3F4, ["Tacho"] = 0x424, ["GroundSpeed"] = 0x428, ["Kilometres"] = 0x430, ["Throttle"] = 0x5DC, ["Brake"] = 0x5E0, ["Clutch"] = 0x5E4, ["ActiveDriverCamera"] = 0x5C0, ["ActivePassengerCamera"] = 0x5C4, ["AiEnabled"] = 0x624, ["AiMode"] = 0x625, ["AiLight"] = 0x634, ["AiInteriorLight"] = 0x638, ["AiIndicatorLeft"] = 0x63C, ["AiIndicatorRight"] = 0x640, ["AiBrakeLight"] = 0x644 },
            ["Human"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Position"] = 0x4, ["Tile"] = 0x74, ["RuntimeIndex"] = 0x258, ["MarkedForKilling"] = 0x25C, ["HumanIndex"] = 0x5AC, ["CollisionType"] = 0x5B8, ["RenderMe"] = 0x5BC, ["IdCode"] = 0x1E4, ["Target"] = 0x5BD, ["TargetStation"] = 0x5F4, ["PreTargetStation"] = 0x5FC, ["Departure"] = 0x608, ["EnterBusAt"] = 0x60C, ["SeatBus"] = 0x610, ["SeatStation"] = 0x618, ["TicketType"] = 0x61C, ["TicketIndex"] = 0x61D, ["TicketReady"] = 0x629, ["State"] = 0x64C, ["Speed"] = 0x6A4, ["BusIndex"] = 0x6B0, ["Station"] = 0x6BC, ["AiMode"] = 0x6C4, ["AiModeEx"] = 0x6C5, ["AiSubMode"] = 0x6C6, ["CollisionState"] = 0x6C7 },
            ["ScriptVariables"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["ComplMapObject"] = 0x210, ["ComplObjectInstance"] = 0x214, ["VariableNames"] = 0x1EC, ["StringVariableNames"] = 0x1F0, ["PublicValues"] = 0x23C, ["StringValues"] = 0x2C, ["SoundPack"] = 0x21C, ["MaximumVariables"] = 16_384 },
            // OmsiComplMapObj -> TConstBlock -> dynamic arrays. These are
            // read-only Wave A records reconciled directly from the pinned
            // OmsiHook wrappers for this exact executable profile.
            ["ScriptConstants"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["ConstBlock"] = 0x220, ["Values"] = 0x4, ["Names"] = 0x8, ["Functions"] = 0xC, ["FunctionNames"] = 0x10, ["FunctionPoints"] = 0x4, ["MaximumElements"] = 16_384 },
            ["RoadVehicleDefinition"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Definition"] = 0x710, ["Hofs"] = 0x5E4, ["HofName"] = 0x4, ["HofServiceTrip"] = 0xC },
            ["Driver"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x68, ["Filename"] = 0x0, ["Name"] = 0x4, ["Gender"] = 0x8, ["Birthday"] = 0x10, ["DateOfHire"] = 0x18, ["BusStops"] = 0x20, ["Crashes"] = 0x30, ["Passengers"] = 0x54, ["Tickets"] = 0x58, ["Cash"] = 0x5C },
            ["TicketPack"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Filename"] = 0x0, ["VoicePath"] = 0x4, ["Tickets"] = 0x8, ["Stamper"] = 0xC, ["Buy"] = 0x10, ["Chattiness"] = 0x14, ["Whinge"] = 0x18, ["TicketSize"] = 0x2C },
            ["Ticket"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Name"] = 0x0, ["EnglishName"] = 0x4, ["DisplayName"] = 0x8, ["MaximumStations"] = 0xC, ["MinimumAge"] = 0x10, ["MaximumAge"] = 0x14, ["Value"] = 0x18, ["DayTicket"] = 0x1C, ["Probability"] = 0x20 },
            ["TimeTableLog"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x18, ["BusStopName"] = 0x0, ["EstimatedArrival"] = 0x4, ["EstimatedDeparture"] = 0x8, ["ActualArrival"] = 0xC, ["ActualDeparture"] = 0x10, ["ArrivalOk"] = 0x14, ["DepartureOk"] = 0x15 },
            // NoRvList: a TStringList of "group<TAB>number" (research report beta4-static
            // runtime-map-data-shutdown.md section 3; OmsiHook's string-array reading is wrong here).
            ["TimeTableManager"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Invalid"] = 0x4, ["Tracks"] = 0x8, ["Trips"] = 0xC, ["BusStops"] = 0x10, ["StationLinks"] = 0x14, ["Lines"] = 0x18, ["RvFiles"] = 0x20, ["NoRvList"] = 0x24 },
            ["StringList"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Vmt"] = 0x0043F6F0, ["Items"] = 0x2C, ["Count"] = 0x30, ["ItemStride"] = 0x8 },
            ["TimeTableRvFile"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x1C, ["StartDateRel2000"] = 0x0, ["EndDateRel2000"] = 0x4, ["Line"] = 0x8, ["NumberTours"] = 0xC, ["TypeTours"] = 0x10, ["TypeLineFiles"] = 0x14, ["TypeLineProbability"] = 0x18 },
            ["TimeTableTrack"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x10, ["Filename"] = 0x0, ["FilePath"] = 0x4, ["Entries"] = 0x8, ["Length"] = 0xC },
            ["TimeTableTrackEntry"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x28, ["IdCode"] = 0x0, ["PathIndexOnObject"] = 0x4, ["PathTile"] = 0x8, ["PathIndex"] = 0xC, ["RelativeDistance"] = 0x10, ["Distance"] = 0x14, ["Valid"] = 0x18, ["PathOrderCheck"] = 0x19, ["AllowedFstrn"] = 0x1C, ["ChronoOrigin"] = 0x20, ["BadChronos"] = 0x24 },
            ["TimeTableTrip"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x28, ["Filename"] = 0x0, ["ChronoOrigin"] = 0x4, ["Target"] = 0x8, ["Line"] = 0xC, ["TrainReverse"] = 0x10, ["Invalid"] = 0x11, ["Profiles"] = 0x14, ["BusStops"] = 0x18, ["TrackName"] = 0x1C, ["TrackIndex"] = 0x20, ["StationLinkList"] = 0x24 },
            ["TimeTableLine"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x10, ["Name"] = 0x0, ["UserAllowed"] = 0x4, ["Priority"] = 0x5, ["Tours"] = 0x8, ["ChronoOrigin"] = 0xC },
            ["TimeTableBusStop"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x30, ["Supplement"] = 0x0, ["Name"] = 0x4, ["Tile"] = 0x8, ["IdCode"] = 0xC, ["ParentIdCode"] = 0x10, ["PresetAlighting"] = 0x14, ["BadChronos"] = 0x18, ["Index"] = 0x1C, ["StartingLinks"] = 0x20, ["EndingLinks"] = 0x24, ["ChronoOrigin"] = 0x28, ["ChronoRename"] = 0x2C },
            ["TimeTableStationLink"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x44, ["TrackEntries"] = 0x0, ["Length"] = 0x4, ["StartBusStopId"] = 0x8, ["EndBusStopId"] = 0xC, ["StartPathDistance"] = 0x10, ["EndPathDistance"] = 0x14, ["NeededChronos"] = 0x18, ["BadChronos"] = 0x1C, ["StartBusStop"] = 0x20, ["EndBusStop"] = 0x24, ["ChronoOrigin"] = 0x28, ["Valid"] = 0x2C, ["StartTrackEntry"] = 0x30, ["EndTrackEntry"] = 0x34, ["StartRelativeDistance"] = 0x38, ["EndRelativeDistance"] = 0x3C, ["Visible"] = 0x40 },
            ["TimeTableTour"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x30, ["Name"] = 0x0, ["AiGroup"] = 0x4, ["AiType"] = 0x8, ["AiGroupIndex"] = 0xC, ["VehicleReservations"] = 0x10, ["VehicleIndices"] = 0x14, ["HasNormalVehicle"] = 0x18, ["CompletedDay"] = 0x1C, ["Invalid"] = 0x2A, ["Entries"] = 0x2C },
            ["TimeTableProfile"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x14, ["Name"] = 0x0, ["TotalTime"] = 0x4, ["StopTimes"] = 0x8, ["TrackEntryTimes"] = 0xC, ["ServiceTrip"] = 0x10 },
            ["TimeTableTourEntry"] = new Dictionary<string,uint>(StringComparer.Ordinal) { ["Size"] = 0x18, ["Trip"] = 0x0, ["TripIndex"] = 0x4, ["Profile"] = 0x8, ["StartTime"] = 0xC, ["EndTime"] = 0x10, ["SmoothTransition"] = 0x14 }
        }
    };
}
