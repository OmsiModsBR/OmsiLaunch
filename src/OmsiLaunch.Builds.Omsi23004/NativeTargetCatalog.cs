using OmsiLaunch.NativeResolution;

namespace OmsiLaunch.Builds.Omsi23004;

// The single source of every native address, slot and field offset that
// OmsiLaunch uses inside OMSI (managed readers and the native boundary alike).
// Declarations are hand-written; guards and signatures are derived from the
// reference executable by tools\OmsiLaunch.SignatureTool into
// NativeTargetCatalog.Signatures.g.cs. KnownValue is the reference layout and
// is only a fast path: every target is verified by its guard in process.
public static partial class NativeTargetCatalog
{
    public const string PackName = "Omsi23004";
    public const uint PreferredBase = 0x00400000;
    // Code-section SHA-256 of builds whose layout equals the reference. The file
    // SHA-256 of the executable is diagnostic only; LAA variants that differ in
    // headers share this code identity.
    public static readonly string[] KnownCodeSha256 = { "13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028" };

    private static NativeTargetSpec F(string id, uint known, string? cls = null, string? rtti = null, string? call = null) => new(id, NativeTargetKind.Function, known, ClassName: cls, RttiName: rtti, CallTarget: call);
    private static NativeTargetSpec C(string id, uint known, string? call = null) => new(id, NativeTargetKind.CallSite, known, CallTarget: call);
    private static NativeTargetSpec G(string id, uint known) => new(id, NativeTargetKind.Global, known);
    private static NativeTargetSpec V(string id, uint known, string cls) => new(id, NativeTargetKind.Vmt, known, ClassName: cls);
    private static NativeTargetSpec S(string id, uint known, string vmt, string method) => new(id, NativeTargetKind.VmtSlot, known, SlotOf: vmt, SlotValue: method);
    private static NativeTargetSpec O(string id, uint known, string? cls = null, string? rtti = null) => new(id, NativeTargetKind.FieldOffset, known, ClassName: cls, RttiName: rtti);
    private static NativeTargetSpec L(string id) => new(id, NativeTargetKind.Layout, 0);

    // Declarations (id, kind, reference value, build-independent hints).
    public static readonly IReadOnlyList<NativeTargetSpec> Declarations = new[]
    {
        // Delphi classes (RTTI class name + vmtSelfPtr).
        V("vmt.Tform_start", 0x006759C8, "Tform_start"),
        V("vmt.Tform_setpos", 0x00679EB8, "Tform_setpos"),
        V("vmt.TTempRVList", 0x0074802C, "TTempRVList"),
        V("vmt.TListBox", 0x004AA048, "TListBox"),
        V("vmt.TWeather", 0x00752020, "TWeather"),
        V("vmt.TActuWeather", 0x0063CCC4, "TActuWeather"),
        V("vmt.TRoadVehicleInst", 0x007C9800, "TRoadVehicleInst"),

        // Published form handlers (RTTI method table).
        F("start.FormShow", 0x00676FC4, "Tform_start", "FormShow"),
        F("start.FormHide", 0x006785B4, "Tform_start", "FormHide"),
        F("start.FormCreate", 0x0067739C, "Tform_start", "FormCreate"),
        F("start.Button1Click", 0x006768F8, "Tform_start", "Button1Click"),
        // OMSI's own exit routine (what runs after the user confirms quitting;
        // research report beta4-static runtime-map-data-shutdown.md section 5).
        F("main.CloseOMSI", 0x008298A4, "TForm_main", "CloseOMSI"),
        F("start.RadioButton1Click", 0x00678A88, "Tform_start", "RadioButton1Click"),
        F("start.ComboBox1Change", 0x00678BC8, "Tform_start", "ComboBox1Change"),
        F("start.SituationSelectionChanged", 0x00679250, "Tform_start", "ComboBox3Change"),
        F("start.ButtonDriverNewClick", 0x00676884),
        F("setpos.FormShow", 0x0067A6F0, "Tform_setpos", "FormShow"),
        F("setpos.Button1Click", 0x0067A2E8, "Tform_setpos", "Button1Click"),

        // VCL and RTL functions.
        F("vcl.TCustomForm.ShowModal", 0x00551D18),
        new("vcl.TControl.SetVisible", NativeTargetKind.Function, 0x0054CCA8, ClassName: "TForm", RttiProperty: "Visible.set"),
        new("vcl.TRadioButton.SetChecked", NativeTargetKind.Function, 0x0059BE58, ClassName: "TRadioButton", RttiProperty: "Checked.set"),
        F("vcl.TCustomListBox.SetItemIndex", 0x004B56DC),
        F("vcl.TCustomListBox.GetItemIndex", 0x004B5644),
        F("rtl.GetMem", 0x00404614),
        // AnsiString assignment; a constant-shaped source (refCnt -1) is copied
        // into OMSI's heap (research report beta4-static weather-and-delphi-strings.md section 3).
        F("rtl.LStrAsg", 0x00408BD4),
        // Re-resolves the cloud type when CloudTypeNum < 0 (same report, section 2.3).
        F("weather.RefreshWeather", 0x00754D84),
        // TWeather.loadWeather(filename; nil = actWeather): what NewSituation and
        // a saved situation use (same report, section 2.3).
        F("weather.LoadWeather", 0x00753658),
        // Vehicle scripts (research report beta4-static weather-and-delphi-strings.md
        // section 4.3). SetStringVariable assigns through @UStrAsg; TriggerXML and
        // SoundTrigger are virtual and are called through the instance's VMT.
        F("complmapobjinst.SetStringVariable", 0x007BD038),
        F("roadvehicleinst.TriggerXML", 0x007E9338),
        F("roadvehicleinst.SoundTrigger", 0x00612C98),
        F("rtl.UStrToLStr", 0x004092CC),

        // OMSI program manager and world functions.
        F("progman.CloseMap", 0x006E57B0),
        F("progman.NewSituation", 0x006E5C0C),
        F("progman.CalcAISum", 0x00720F74),
        F("progman.SetAIVehicles", 0x00709350),
        F("progman.SetTime", 0x00707B94),
        F("progman.SetActualDateTime", 0x0070885C),
        F("progman.InitializeScheduledAI", 0x0070D160),
        F("progman.MakeVehicle", 0x0070A250),
        F("progman.PlaceRandomBus", 0x00708F8C),
        F("progman.SetBusPosition", 0x007099F4),
        F("progman.CopyTempListIntoMainList", 0x0074A240),
        F("progman.DownloadInternetTextures", 0x006E7164),
        F("TTempRVList.Create", 0x0074A0E0),
        F("map.LoadGlobalFile", 0x007860B0),
        F("start.LoadSelectedSituation", 0x0064307C),

        // Call and patch sites.
        C("site.ShowModal.SetVisible", 0x00551C2F, "vcl.TControl.SetVisible"),
        C("site.Initialisieren.ShowModal", 0x006E8573),
        C("site.NewSituation.AskForPosition", 0x006E60F5),
        // NewSituation's copy of the start clock into the running clock
        // (research report beta4-static start-date-time.md, section 5).
        C("site.NewSituation.StartTimeCopy", 0x006E5E52),

        // VMT method slots.
        S("slot.Tform_start.ShowModal", 0x00675AE8, "vmt.Tform_start", "vcl.TCustomForm.ShowModal"),
        S("slot.Tform_setpos.ShowModal", 0x00679FD8, "vmt.Tform_setpos", "vcl.TCustomForm.ShowModal"),
        // ItemIndex setter of the situation list box: the slot offset is
        // slot - vmt (a virtual call through the object's own VMT).
        S("slot.TListBox.SetItemIndex", 0x004AA144, "vmt.TListBox", "vcl.TCustomListBox.SetItemIndex"),
        S("slot.TListBox.GetItemIndex", 0x004AA140, "vmt.TListBox", "vcl.TCustomListBox.GetItemIndex"),
        S("slot.TRoadVehicleInst.TriggerXML", 0x007C981C, "vmt.TRoadVehicleInst", "roadvehicleinst.TriggerXML"),
        S("slot.TRoadVehicleInst.SoundTrigger", 0x007C9814, "vmt.TRoadVehicleInst", "roadvehicleinst.SoundTrigger"),

        // Global slots.
        G("global.StartForm", 0x008612F8),
        G("global.SetPosForm", 0x00859BC8),
        G("global.SelectedMap", 0x008591A4),
        G("global.ProgMan", 0x00858BDC),
        G("global.ProgManLiveObject", 0x00862F28),
        G("global.Map", 0x00861588),
        G("global.RawEntrypoints", 0x00858F34),
        G("global.RawEntrypointIndex", 0x00858A50),
        G("global.StartTransition", 0x00858DA8),
        G("global.AiConfig", 0x0085912C),
        G("global.TimeHour", 0x0086176C), G("global.TimeMinute", 0x0086176D), G("global.TimeSecond", 0x00861770),
        G("global.TimeDay", 0x00861778), G("global.TimeMonth", 0x0086178C), G("global.TimeYear", 0x00861790),
        // The start clock NewSituation copies into the running clock, the
        // running day of the year (the authoritative date; day and month are
        // derived by SetTime), and the flags that make NewSituation use a
        // fixed time (tutorial/demo, INFERRED) or SetTime return early.
        G("global.MainForm", 0x00862ED8), G("global.ExitDone", 0x00862FB4),
        G("global.StartYear", 0x00861758), G("global.StartDayOfYear", 0x0086175C), G("global.StartHour", 0x00861760),
        G("global.StartMinute", 0x00861764), G("global.StartSecond", 0x00861768), G("global.TimeDayOfYear", 0x00861774),
        G("global.FixedTimeFlag", 0x00862F30), G("global.FixedTimeMode", 0x00862F34), G("global.TimeHold", 0x00861718),
        G("global.RoadVehicles", 0x00861508),
        G("global.RoadVehicleTypes", 0x008615A8),
        G("global.PlayerVehicleIndex", 0x00861740),
        G("global.Weather", 0x008617D0),
        G("global.ActualWeather", 0x00861278),
        G("global.Humans", 0x0086172C),
        G("global.TimeTableManager", 0x008614E8),
        G("global.Drivers", 0x008614F8),
        G("global.SelectedDriver", 0x008614FC),
        G("global.TicketPack", 0x008611FC),
        G("global.TimeTableLogs", 0x00861750),
        G("global.Camera", 0x008616E0),
        G("global.CameraFamily", 0x008616A4),
        G("global.D3DDevice", 0x008627D0),

        // Field offsets used by the native boundary.
        O("field.Tform_start.SituationControl", 0x3E4, "Tform_start", "ListBox1"),
        O("field.Tform_start.MapStrings", 0x3F0),
        O("field.Tform_start.MapCompanions", 0x3F4),
        O("field.Tform_start.SituationCollection", 0x3F8),
        // Static analysis (beta4-static entrypoint-last-player-vehicle.md 2.1):
        // +0x434 is the LAST MAP STATE radio and +0x438 the NEW MAP radio; the
        // two ids below keep their historical names (only cleared together).
        O("field.Tform_start.ModeNewMap", 0x434),
        O("field.Tform_start.ModeLastState", 0x434),
        O("field.Tform_start.MapCombo", 0x3B8, "Tform_start", "ComboBox1"),
        O("field.Tform_start.ModeSelectedMap", 0x438),
        O("field.Tform_start.ModeSavedSituation", 0x43C),
        O("field.Tform_setpos.ListBox", 0x398, "Tform_setpos", "ListBox1"),
        O("field.Tform_setpos.Button", 0x390, "Tform_setpos", "Button1"),
        new("field.TListBox.Items", NativeTargetKind.FieldOffset, 0x288, ClassName: "TListBox", RttiProperty: "Items.get"),
        O("field.Tform_setpos.AskForPosition", 0x3A0),
        O("field.Tform_setpos.SelectedText", 0x3A4),
        O("field.TCustomForm.ModalResult", 0x2C0),
        new("field.TCustomForm.Visible", NativeTargetKind.FieldOffset, 0x61, ClassName: "TForm", RttiProperty: "Visible.get"),
        O("field.TProgMan.CriticalSection", 0x1B4),
        // Known-identity-only offsets (no semantically sound derivation).
        O("field.RawEntrypoint.Stride", 0x30),
        O("field.RawEntrypoint.Name", 0x2C),
        O("field.RoadVehicleList.Items", 0x28),
        O("field.MemArrayList.Data", 0x04),
        O("field.MemArrayList.Count", 0x08),
        // Known identity only (extended, not published, RTTI). TWeather.actWeather
        // (+0x08) is the source record; CloudType is its AnsiString.
        O("field.TWeather.CloudType", 0x3C),
        O("field.TWeather.CloudTypeNum", 0xB4),
        O("field.TActuWeather.Icao", 0x0C),
        O("field.TActuWeather.Active", 0x08),
        O("field.TActuWeather.LastDownloaded", 0x10),
        O("field.TActuWeather.InvalidIcao", 0x18),
        O("field.TActuWeather.Process", 0x20),

        // Object layout families read by managed readers (known identity only).
        L("layout.Map"), L("layout.MapTile"), L("layout.Weather"), L("layout.ActualWeather"), L("layout.Camera"),
        L("layout.RoadVehicleList"), L("layout.RoadVehicle"), L("layout.Human"), L("layout.ScriptVariables"),
        L("layout.ScriptConstants"), L("layout.RoadVehicleDefinition"), L("layout.Driver"), L("layout.TicketPack"),
        L("layout.Ticket"), L("layout.TimeTable"),
    };

    private static readonly string[] StartForm = { "global.StartForm", "vmt.Tform_start", "field.TCustomForm.ModalResult", "field.TCustomForm.Visible" };
    private static readonly string[] Time = { "global.TimeHour", "global.TimeMinute", "global.TimeSecond", "global.TimeDay", "global.TimeMonth", "global.TimeYear" };

    // Native capability -> targets it needs. A capability is available only
    // when every listed target is resolved and verified.
    public static readonly IReadOnlyList<NativeCapabilitySpec> Capabilities = new[]
    {
        new NativeCapabilitySpec("boot.headless-start", StartForm.Concat(new[] { "slot.Tform_start.ShowModal", "vcl.TCustomForm.ShowModal", "site.ShowModal.SetVisible", "vcl.TControl.SetVisible", "start.FormShow", "start.FormHide" }).ToArray()),
        new NativeCapabilitySpec("world.new-map", StartForm.Concat(new[] { "global.ProgMan", "global.SelectedMap", "global.Map", "global.AiConfig", "global.StartTransition", "global.RawEntrypoints", "global.RawEntrypointIndex", "global.SetPosForm", "vmt.Tform_setpos", "slot.Tform_setpos.ShowModal", "setpos.FormShow", "setpos.Button1Click", "field.Tform_setpos.ListBox", "field.Tform_setpos.Button", "field.Tform_setpos.AskForPosition", "field.Tform_setpos.SelectedText", "vmt.TListBox", "field.TListBox.Items", "slot.TListBox.GetItemIndex", "slot.TListBox.SetItemIndex", "site.NewSituation.AskForPosition", "progman.CloseMap", "progman.NewSituation", "progman.CalcAISum", "progman.SetAIVehicles", "progman.SetTime", "progman.InitializeScheduledAI", "field.Tform_start.MapStrings", "field.Tform_start.MapCompanions", "field.Tform_start.ModeNewMap", "field.Tform_start.ModeSelectedMap", "start.ButtonDriverNewClick", "vcl.TRadioButton.SetChecked", "rtl.UStrToLStr" }).Concat(Time).ToArray()),
        new NativeCapabilitySpec("world.saved-situation", StartForm.Concat(new[] { "field.Tform_start.SituationControl", "field.Tform_start.SituationCollection", "field.Tform_start.ModeSavedSituation", "vcl.TRadioButton.SetChecked", "start.SituationSelectionChanged", "start.Button1Click", "vmt.TListBox", "slot.TListBox.SetItemIndex" }).ToArray()),
        new NativeCapabilitySpec("internet-textures.disabled", new[] { "progman.DownloadInternetTextures" }),
        new NativeCapabilitySpec("time.read", Time),
        new NativeCapabilitySpec("time.set", Time.Concat(new[] { "global.ProgMan", "progman.SetTime" }).ToArray()),
        new NativeCapabilitySpec("world.last-map-state", StartForm.Concat(new[] { "start.RadioButton1Click", "start.ComboBox1Change", "start.Button1Click", "field.Tform_start.ModeLastState", "field.Tform_start.MapCombo", "field.Tform_start.MapStrings", "field.Tform_start.MapCompanions", "vmt.TListBox", "slot.TListBox.SetItemIndex" }).ToArray()),
        new NativeCapabilitySpec("session.cooperative-exit", new[] { "main.CloseOMSI", "global.MainForm", "global.ExitDone" }),
        new NativeCapabilitySpec("world.start-date-time", new[] { "global.StartYear", "global.StartDayOfYear", "global.StartHour", "global.StartMinute", "global.StartSecond", "global.TimeDayOfYear", "global.TimeYear", "global.TimeHour", "global.TimeMinute", "global.FixedTimeFlag", "global.FixedTimeMode", "global.TimeHold", "site.NewSituation.StartTimeCopy", "progman.NewSituation", "progman.SetTime" }),
        new NativeCapabilitySpec("weather.read", new[] { "global.Weather", "layout.Weather" }),
        new NativeCapabilitySpec("weather.actual.read", new[] { "global.ActualWeather", "layout.ActualWeather" }),
        new NativeCapabilitySpec("weather.set", new[] { "global.Weather", "global.ActualWeather", "vmt.TWeather", "vmt.TActuWeather", "layout.Weather", "layout.ActualWeather", "rtl.LStrAsg", "weather.RefreshWeather", "field.TWeather.CloudType", "field.TWeather.CloudTypeNum" }),
        new NativeCapabilitySpec("world.start-weather", new[] { "global.Weather", "global.ActualWeather", "vmt.TWeather", "vmt.TActuWeather", "weather.LoadWeather", "rtl.LStrAsg", "field.TActuWeather.Icao", "field.TActuWeather.Active", "field.TActuWeather.LastDownloaded", "field.TActuWeather.InvalidIcao", "field.TActuWeather.Process" }),
        new NativeCapabilitySpec("weather.metar.set", new[] { "global.ActualWeather", "vmt.TActuWeather", "layout.ActualWeather", "rtl.LStrAsg", "field.TActuWeather.Icao" }),
        new NativeCapabilitySpec("map.read", new[] { "global.Map", "layout.Map", "layout.MapTile" }),
        new NativeCapabilitySpec("camera.read", new[] { "global.Camera", "global.CameraFamily", "layout.Camera" }),
        new NativeCapabilitySpec("camera.set", new[] { "global.Camera", "global.CameraFamily", "layout.Camera" }),
        new NativeCapabilitySpec("camera.lock", new[] { "global.Camera", "global.CameraFamily", "layout.Camera", "global.RoadVehicles", "global.PlayerVehicleIndex", "layout.RoadVehicleList", "layout.RoadVehicle" }),
        new NativeCapabilitySpec("vehicles.list", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle" }),
        new NativeCapabilitySpec("vehicles.get", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle" }),
        new NativeCapabilitySpec("vehicles.summary", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle" }),
        new NativeCapabilitySpec("vehicles.spawn", new[] { "global.ProgManLiveObject", "global.RoadVehicles", "global.RoadVehicleTypes", "vmt.TTempRVList", "TTempRVList.Create", "progman.MakeVehicle", "progman.CopyTempListIntoMainList", "rtl.GetMem", "field.TProgMan.CriticalSection", "field.RoadVehicleList.Items", "field.MemArrayList.Data", "field.MemArrayList.Count", "layout.RoadVehicleList" }),
        new NativeCapabilitySpec("vehicles.place-random", new[] { "global.ProgManLiveObject", "global.RoadVehicles", "progman.PlaceRandomBus", "field.TProgMan.CriticalSection", "field.RoadVehicleList.Items", "field.MemArrayList.Data", "field.MemArrayList.Count", "layout.RoadVehicleList" }),
        new NativeCapabilitySpec("player.read", new[] { "global.PlayerVehicleIndex", "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle" }),
        new NativeCapabilitySpec("humans.list", new[] { "global.Humans", "layout.Human" }),
        new NativeCapabilitySpec("humans.get", new[] { "global.Humans", "layout.Human" }),
        new NativeCapabilitySpec("humans.summary", new[] { "global.Humans", "layout.Human" }),
        new NativeCapabilitySpec("timetable.read", new[] { "global.TimeTableManager", "global.TimeTableLogs", "layout.TimeTable" }),
        new NativeCapabilitySpec("scripts.numeric", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptVariables" }),
        new NativeCapabilitySpec("scripts.string.read", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptVariables" }),
        new NativeCapabilitySpec("scripts.string.write", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptVariables", "vmt.TRoadVehicleInst", "complmapobjinst.SetStringVariable" }),
        new NativeCapabilitySpec("scripts.trigger", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptVariables", "vmt.TRoadVehicleInst", "roadvehicleinst.TriggerXML", "roadvehicleinst.SoundTrigger", "slot.TRoadVehicleInst.TriggerXML", "slot.TRoadVehicleInst.SoundTrigger" }),
        new NativeCapabilitySpec("constants", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptConstants" }),
        new NativeCapabilitySpec("curves", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.ScriptConstants" }),
        new NativeCapabilitySpec("hof.read", new[] { "global.RoadVehicles", "layout.RoadVehicleList", "layout.RoadVehicle", "layout.RoadVehicleDefinition" }),
        new NativeCapabilitySpec("drivers.read", new[] { "global.Drivers", "global.SelectedDriver", "layout.Driver" }),
        new NativeCapabilitySpec("tickets.read", new[] { "global.TicketPack", "layout.TicketPack", "layout.Ticket" }),
        new NativeCapabilitySpec("d3d.texture", new[] { "global.D3DDevice" }),
    };

    // Profile global names read by the managed readers -> catalog targets.
    public static readonly IReadOnlyDictionary<string, string> GlobalTargets = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["SelectedMap"] = "global.SelectedMap", ["ProgMan"] = "global.ProgMan", ["Map"] = "global.Map", ["SetPos"] = "global.SetPosForm",
        ["RawEntrypoints"] = "global.RawEntrypoints", ["RawEntrypointIndex"] = "global.RawEntrypointIndex", ["StartTransition"] = "global.StartTransition",
        ["AiConfig"] = "global.AiConfig", ["TimeHour"] = "global.TimeHour", ["TimeMinute"] = "global.TimeMinute", ["TimeSecond"] = "global.TimeSecond",
        ["TimeDay"] = "global.TimeDay", ["TimeMonth"] = "global.TimeMonth", ["TimeYear"] = "global.TimeYear", ["RoadVehicles"] = "global.RoadVehicles",
        ["RoadVehicleTypes"] = "global.RoadVehicleTypes", ["ProgManLiveObject"] = "global.ProgManLiveObject", ["PlayerVehicleIndex"] = "global.PlayerVehicleIndex",
        ["Weather"] = "global.Weather", ["ActualWeather"] = "global.ActualWeather", ["Humans"] = "global.Humans", ["TimeTableManager"] = "global.TimeTableManager",
        ["Drivers"] = "global.Drivers", ["SelectedDriver"] = "global.SelectedDriver", ["TicketPack"] = "global.TicketPack", ["TimeTableLogs"] = "global.TimeTableLogs",
        ["Camera"] = "global.Camera", ["CameraFamily"] = "global.CameraFamily", ["D3DDevice"] = "global.D3DDevice",
    };

    // The pack used at run time: declarations merged with the derived guards
    // and signatures of NativeTargetCatalog.Signatures.g.cs.
    // Built on first use: static field initializers of the two partial files
    // run in an unspecified order.
    public static NativeLayoutPack Pack => pack.Value;
    private static readonly Lazy<NativeLayoutPack> pack = new(() => new(PackName, PreferredBase, KnownCodeSha256,
        Declarations.Select(d => Derived.TryGetValue(d.Id, out var derived) ? d with { Guard = derived.Guard, Signatures = derived.Signatures } : d).ToArray(),
        Capabilities));

    public sealed record DerivedTarget(string? Guard, IReadOnlyList<NativeSignature> Signatures);
}
