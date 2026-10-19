using System.Runtime.InteropServices;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.NativeResolution;
using ResolvedImage = OmsiLaunch.NativeResolution.NativeResolution;

namespace OmsiLaunch.Plugin;

// Resolves every native target of the build catalog against the loaded OMSI
// image, once, before this plugin patches anything, and hands the verified
// table to the native boundary. Capabilities whose targets are not verified
// stay unavailable; nothing falls back to a fixed address.
internal static class PluginNativeTargets
{
    private static ResolvedImage? resolution;
    public static ResolvedImage? Resolution => Volatile.Read(ref resolution);
    public static bool Configured { get; private set; }

    public static ResolvedImage Resolve()
    {
        if (Resolution is { } existing) return existing;
        var resolved = NativeResolver.Resolve(NativeImage.FromCurrentProcess(), NativeTargetCatalog.Pack);
        var table = BuildTable(resolved);
        Configured = NativeConfigureTargets(ref table) == 1;
        Volatile.Write(ref resolution, resolved);
        return resolved;
    }

    public static bool IsAvailable(string capability) => Configured && Resolution?.IsAvailable(capability) == true;

    // Verified globals under the profile names the managed readers use.
    public static IReadOnlyDictionary<string, uint> Globals()
    {
        var output = new Dictionary<string, uint>(StringComparer.Ordinal);
        if (Resolution is not { } resolved) return output;
        foreach (var (name, target) in NativeTargetCatalog.GlobalTargets)
            if (resolved.TryGet(target, out var address)) output[name] = address;
        return output;
    }

    // Object layouts are verified only on a known code identity.
    public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, uint>> Layouts() =>
        Resolution is { KnownIdentity: true } ? Omsi23004.Profile.ObjectLayouts : new Dictionary<string, IReadOnlyDictionary<string, uint>>(StringComparer.Ordinal);

    // Compact report for telemetry (the slot holds 4 KiB).
    public static Dictionary<string, string> Report(ResolvedImage resolved) => new()
    {
        ["identity"] = resolved.KnownIdentity ? "known" : "unknown",
        ["code_sha256"] = resolved.CodeSha256,
        ["configured"] = Configured ? "true" : "false",
        ["targets_verified"] = resolved.Targets.Values.Count(t => t.Verified).ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["targets_total"] = resolved.Targets.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
        ["unavailable"] = string.Join(",", resolved.Capabilities.Values.Where(c => !c.Available).Select(c => c.Id)),
    };

    private static NativeTargetTable BuildTable(ResolvedImage resolved)
    {
        uint T(string id) => resolved.TryGet(id, out var value) ? value : 0;
        return new NativeTargetTable
        {
            Version = 4, Size = (uint)Marshal.SizeOf<NativeTargetTable>(), ImageBase = resolved.ImageBase, ImageEnd = resolved.ImageBase + resolved.ImageSize,
            StartFormShow = T("start.FormShow"), StartFormHide = T("start.FormHide"), StartButton1Click = T("start.Button1Click"),
            StartSituationSelectionChanged = T("start.SituationSelectionChanged"), StartButtonDriverNewClick = T("start.ButtonDriverNewClick"),
            SetposFormShow = T("setpos.FormShow"), SetposButton1Click = T("setpos.Button1Click"),
            ShowModal = T("vcl.TCustomForm.ShowModal"), SetVisible = T("vcl.TControl.SetVisible"), RadioSetChecked = T("vcl.TRadioButton.SetChecked"),
            UStrToLStr = T("rtl.UStrToLStr"), GetMem = T("rtl.GetMem"),
            CloseMap = T("progman.CloseMap"), NewSituation = T("progman.NewSituation"), CalcAiSum = T("progman.CalcAISum"), SetAiVehicles = T("progman.SetAIVehicles"),
            SetTime = T("progman.SetTime"), InitializeScheduledAi = T("progman.InitializeScheduledAI"),
            MakeVehicle = T("progman.MakeVehicle"), PlaceRandomBus = T("progman.PlaceRandomBus"), CopyTempList = T("progman.CopyTempListIntoMainList"),
            TempRvListCreate = T("TTempRVList.Create"), DownloadInternetTextures = T("progman.DownloadInternetTextures"),
            SiteShowModalSetVisible = T("site.ShowModal.SetVisible"), SiteNewSituationAskForPosition = T("site.NewSituation.AskForPosition"),
            VmtStart = T("vmt.Tform_start"), VmtSetpos = T("vmt.Tform_setpos"), VmtTempRvList = T("vmt.TTempRVList"), VmtListBox = T("vmt.TListBox"),
            SlotStartShowModal = T("slot.Tform_start.ShowModal"), SlotSetposShowModal = T("slot.Tform_setpos.ShowModal"),
            SlotListBoxGetItemIndex = T("slot.TListBox.GetItemIndex"), SlotListBoxSetItemIndex = T("slot.TListBox.SetItemIndex"),
            GStartForm = T("global.StartForm"), GSetposForm = T("global.SetPosForm"), GSelectedMap = T("global.SelectedMap"), GProgMan = T("global.ProgMan"),
            GProgManLive = T("global.ProgManLiveObject"), GMap = T("global.Map"), GRawEntrypoints = T("global.RawEntrypoints"), GRawEntrypointIndex = T("global.RawEntrypointIndex"),
            GStartTransition = T("global.StartTransition"), GAiConfig = T("global.AiConfig"), GRoadVehicles = T("global.RoadVehicles"),
            GRoadVehicleTypes = T("global.RoadVehicleTypes"), GD3DDevice = T("global.D3DDevice"),
            OSituationControl = T("field.Tform_start.SituationControl"), OMapStrings = T("field.Tform_start.MapStrings"), OMapCompanions = T("field.Tform_start.MapCompanions"),
            OSituationCollection = T("field.Tform_start.SituationCollection"), OModeNewMap = T("field.Tform_start.ModeNewMap"),
            OModeSelectedMap = T("field.Tform_start.ModeSelectedMap"), OModeSavedSituation = T("field.Tform_start.ModeSavedSituation"),
            OSetposListBox = T("field.Tform_setpos.ListBox"), OSetposButton = T("field.Tform_setpos.Button"), OSetposAskForPosition = T("field.Tform_setpos.AskForPosition"),
            OSetposSelectedText = T("field.Tform_setpos.SelectedText"), OModalResult = T("field.TCustomForm.ModalResult"), OListBoxItems = T("field.TListBox.Items"),
            OProgManCriticalSection = T("field.TProgMan.CriticalSection"), ORawEntrypointStride = T("field.RawEntrypoint.Stride"), ORawEntrypointName = T("field.RawEntrypoint.Name"),
            ORoadVehicleItems = T("field.RoadVehicleList.Items"), OMemArrayData = T("field.MemArrayList.Data"), OMemArrayCount = T("field.MemArrayList.Count"),
            GStartYear = T("global.StartYear"), GStartDayOfYear = T("global.StartDayOfYear"), GStartHour = T("global.StartHour"), GStartMinute = T("global.StartMinute"),
            GStartSecond = T("global.StartSecond"), GTimeDayOfYear = T("global.TimeDayOfYear"), GTimeYear = T("global.TimeYear"), GTimeHour = T("global.TimeHour"),
            GTimeMinute = T("global.TimeMinute"), GFixedTimeFlag = T("global.FixedTimeFlag"), GFixedTimeMode = T("global.FixedTimeMode"), GTimeHold = T("global.TimeHold"),
            SiteNewSituationStartTimeCopy = T("site.NewSituation.StartTimeCopy"),
            StartRadioButton1Click = T("start.RadioButton1Click"), StartComboBox1Change = T("start.ComboBox1Change"),
            OModeLastState = T("field.Tform_start.ModeLastState"), OMapCombo = T("field.Tform_start.MapCombo"),
            MainCloseOmsi = T("main.CloseOMSI"), GMainForm = T("global.MainForm"), GExitDone = T("global.ExitDone"),
            LStrAsg = T("rtl.LStrAsg"), RefreshWeather = T("weather.RefreshWeather"), GWeather = T("global.Weather"), GActualWeather = T("global.ActualWeather"),
            VmtWeather = T("vmt.TWeather"), VmtActuWeather = T("vmt.TActuWeather"),
            OWeatherCloudType = T("field.TWeather.CloudType"), OWeatherCloudTypeNum = T("field.TWeather.CloudTypeNum"), OActuWeatherIcao = T("field.TActuWeather.Icao"),
            VmtRoadVehicleInst = T("vmt.TRoadVehicleInst"), SetStringVariable = T("complmapobjinst.SetStringVariable"),
            TriggerXml = T("roadvehicleinst.TriggerXML"), SoundTrigger = T("roadvehicleinst.SoundTrigger"),
            SlotTriggerXml = T("slot.TRoadVehicleInst.TriggerXML"), SlotSoundTrigger = T("slot.TRoadVehicleInst.SoundTrigger"),
            LoadWeather = T("weather.LoadWeather"), OActuWeatherActive = T("field.TActuWeather.Active"), OActuWeatherLastDownloaded = T("field.TActuWeather.LastDownloaded"),
            OActuWeatherInvalidIcao = T("field.TActuWeather.InvalidIcao"), OActuWeatherProcess = T("field.TActuWeather.Process"),
            GMenu2Form = T("global.Menu2Form"), VmtMenu2 = T("vmt.Tform_menu2"), VmtMainForm = T("vmt.TForm_main"), TCustomFormShow = T("vcl.TCustomForm.Show"),
            Menu2Panel1Click = T("menu2.Panel1Click"), Menu2Panel2Click = T("menu2.Panel2Click"), Menu2Panel3Click = T("menu2.Panel3Click"), Menu2Panel4Click = T("menu2.Panel4Click"), Menu2Panel5Click = T("menu2.Panel5Click"),
            OFormVisible = T("field.TCustomForm.Visible"), OMainInFrame = T("field.TForm_main.InIdleFrame"), OWinControlHandle = T("field.TWinControl.Handle"),
            OMenu2Panel1 = T("field.Tform_menu2.Panel1"), OMenu2Panel2 = T("field.Tform_menu2.Panel2"), OMenu2Panel3 = T("field.Tform_menu2.Panel3"), OMenu2Panel4 = T("field.Tform_menu2.Panel4"), OMenu2Panel5 = T("field.Tform_menu2.Panel5"),
        };
    }

    // Must match struct NativeTargetTable in NativeBoundary.cpp field for field.
    [StructLayout(LayoutKind.Sequential)]
    internal struct NativeTargetTable
    {
        public uint Version, Size, ImageBase, ImageEnd;
        public uint StartFormShow, StartFormHide, StartButton1Click, StartSituationSelectionChanged, StartButtonDriverNewClick;
        public uint SetposFormShow, SetposButton1Click;
        public uint ShowModal, SetVisible, RadioSetChecked, UStrToLStr, GetMem;
        public uint CloseMap, NewSituation, CalcAiSum, SetAiVehicles, SetTime, InitializeScheduledAi;
        public uint MakeVehicle, PlaceRandomBus, CopyTempList, TempRvListCreate, DownloadInternetTextures;
        public uint SiteShowModalSetVisible, SiteNewSituationAskForPosition;
        public uint VmtStart, VmtSetpos, VmtTempRvList, VmtListBox;
        public uint SlotStartShowModal, SlotSetposShowModal, SlotListBoxGetItemIndex, SlotListBoxSetItemIndex;
        public uint GStartForm, GSetposForm, GSelectedMap, GProgMan, GProgManLive, GMap, GRawEntrypoints, GRawEntrypointIndex;
        public uint GStartTransition, GAiConfig, GRoadVehicles, GRoadVehicleTypes, GD3DDevice;
        public uint OSituationControl, OMapStrings, OMapCompanions, OSituationCollection, OModeNewMap, OModeSelectedMap, OModeSavedSituation;
        public uint OSetposListBox, OSetposButton, OSetposAskForPosition, OSetposSelectedText, OModalResult, OListBoxItems;
        public uint OProgManCriticalSection, ORawEntrypointStride, ORawEntrypointName, ORoadVehicleItems, OMemArrayData, OMemArrayCount;
        // Version 2: the start clock (start date/time of a NEW_MAP session).
        public uint GStartYear, GStartDayOfYear, GStartHour, GStartMinute, GStartSecond, GTimeDayOfYear, GTimeYear, GTimeHour, GTimeMinute;
        public uint GFixedTimeFlag, GFixedTimeMode, GTimeHold, SiteNewSituationStartTimeCopy;
        // The last map state (/last).
        public uint StartRadioButton1Click, StartComboBox1Change, OModeLastState, OMapCombo;
        // Cooperative exit.
        public uint MainCloseOmsi, GMainForm, GExitDone;
        // Weather strings.
        public uint LStrAsg, RefreshWeather, GWeather, GActualWeather, VmtWeather, VmtActuWeather, OWeatherCloudType, OWeatherCloudTypeNum, OActuWeatherIcao;
        // Vehicle scripts.
        public uint VmtRoadVehicleInst, SetStringVariable, TriggerXml, SoundTrigger, SlotTriggerXml, SlotSoundTrigger;
        // Start weather.
        public uint LoadWeather, OActuWeatherActive, OActuWeatherLastDownloaded, OActuWeatherInvalidIcao, OActuWeatherProcess;
        // Version 3: the native pause menu (take-over, panel handlers).
        public uint GMenu2Form, VmtMenu2, VmtMainForm, TCustomFormShow;
        public uint Menu2Panel1Click, Menu2Panel2Click, Menu2Panel3Click, Menu2Panel4Click, Menu2Panel5Click;
        public uint OFormVisible, OMainInFrame, OWinControlHandle, OMenu2Panel1, OMenu2Panel2, OMenu2Panel3, OMenu2Panel4, OMenu2Panel5;
    }

    [DllImport("OmsiLaunch.Native.x86.dll", CallingConvention = CallingConvention.Cdecl)] private static extern int NativeConfigureTargets(ref NativeTargetTable table);
}
