#include <windows.h>
#include <wchar.h>
#include <stdint.h>
#include <new>
#include <initializer_list>
#include <d3d9.h>
#include "NativeHooks.h"

struct CanonicalMapDiagnostics {
    uintptr_t form;
    uintptr_t formVmt;
    uintptr_t stringArray;
    uintptr_t companionArray;
    int stringCount;
    int companionCount;
    int grundorfIndex;
    uintptr_t sourceData;
    int sourceLength;
    uintptr_t destinationSlot;
    uintptr_t destinationVariable;
    uintptr_t destinationBeforeData;
    int destinationBeforeLength;
    uintptr_t destinationAfterData;
    int destinationAfterLength;
    int destinationCodePage;
    int destinationElementSize;
    int helperCalled;
    int readbackValid;
    wchar_t sourceText[512];
    wchar_t destinationBeforeText[512];
    wchar_t destinationAfterText[512];
    int scanStage;
    int failedIndex;
    int readableEntryCount;
    wchar_t entrySummary[4096];
    unsigned int sourceHeader[4];
    unsigned char sourceBytes[64];
    unsigned int destinationBeforeHeader[4];
    unsigned char destinationBeforeBytes[64];
    unsigned int destinationAfterHeader[4];
    unsigned char destinationAfterBytes[64];
};

struct EntrypointDiagnostics {
    int status;
    int preflightStatus;
    int newSituationFingerprintOk;
    int setposGlobalReadable;
    int setposSlotReadable;
    int setposObjectReadable;
    int invocationCount;
    int originalInvoked;
    int formShowCalled;
    int buttonCalled;
    int presentedCount;
    int presentedIndex;
    int requestedPresentedIndex;
    int confirmedPresentedIndex;
    int itemIndexBefore;
    int itemIndexAfter;
    int identityDecodeStatus;
    int rawCount;
    int rawIndex;
    int rawDiagnosticStatus;
    int slotRestored;
    uintptr_t setposSelf;
    uintptr_t setposVmt;
    uintptr_t slotAddress;
    uintptr_t originalTarget;
    uintptr_t listBox;
    uintptr_t items;
    uintptr_t rawArrayHolder;
    uintptr_t rawArray;
    float map124;
    float map128;
    float map12C;
    float map130;
    float map134;
    float map138;
    float map13C;
    float map140;
    wchar_t presentedName[512];
    wchar_t rawName[512];
};

// Internal-only diagnostic contract for the profiled MakeVehicle primitive.
// It is intentionally pointer-bearing because it never crosses the public API.
struct NativeMakeVehicleDiagnostics {
    int status;
    int rawReturn;
    int beforeCount;
    int afterCount;
    int deltaCount;
    int copyReturn;
    uintptr_t progMan;
    uintptr_t roadVehicles;
    uintptr_t roadVehicleTypes;
    uintptr_t criticalSection;
    uintptr_t temporaryList;
    uintptr_t createdVehicle;
    uintptr_t createdVehicleVmt;
};

struct NativeD3DStatus {
    int status;
    uintptr_t slot;
    uintptr_t candidate;
    long queryInterfaceResult;
    long cooperativeLevelResult;
    unsigned long executionThreadId;
    unsigned long ownedDeviceReferences;
    unsigned long lifecycleState;
    unsigned long lifecycleTransition;
    unsigned long deviceGeneration;
    unsigned long liveTextureCount;
    unsigned long resetHookInstalled;
    unsigned long lastResetThreadId;
};

struct NativeD3DTextureResult {
    int status;
    long operationResult;
    unsigned __int64 handle;
    unsigned long lifecycleState;
    unsigned long deviceGeneration;
    unsigned long textureState;
    unsigned long width;
    unsigned long height;
    unsigned long format;
    unsigned long levels;
    unsigned long level;
    unsigned long levelWidth;
    unsigned long levelHeight;
    unsigned long executionThreadId;
};

// Native target table. Every OMSI address, VMT slot and build-specific field
// offset this boundary uses comes from here; none is compiled in. The managed
// plugin resolves and verifies each target (OmsiLaunch.NativeResolution and
// the build catalog) before any hook and passes the table once. An entry of 0
// is unresolved: every operation that needs it fails closed.
struct NativeTargetTable {
    uint32_t version, size, imageBase, imageEnd;
    uint32_t startFormShow, startFormHide, startButton1Click, startSituationSelectionChanged, startButtonDriverNewClick;
    uint32_t setposFormShow, setposButton1Click;
    uint32_t showModal, setVisible, radioSetChecked, uStrToLStr, getMem;
    uint32_t closeMap, newSituation, calcAiSum, setAiVehicles, setTime, initializeScheduledAi;
    uint32_t makeVehicle, placeRandomBus, copyTempList, tempRvListCreate, downloadInternetTextures;
    uint32_t siteShowModalSetVisible, siteNewSituationAskForPosition;
    uint32_t vmtStart, vmtSetpos, vmtTempRvList, vmtListBox;
    uint32_t slotStartShowModal, slotSetposShowModal, slotListBoxGetItemIndex, slotListBoxSetItemIndex;
    uint32_t gStartForm, gSetposForm, gSelectedMap, gProgMan, gProgManLive, gMap, gRawEntrypoints, gRawEntrypointIndex;
    uint32_t gStartTransition, gAiConfig, gRoadVehicles, gRoadVehicleTypes, gD3DDevice;
    uint32_t oSituationControl, oMapStrings, oMapCompanions, oSituationCollection, oModeNewMap, oModeSelectedMap, oModeSavedSituation;
    uint32_t oSetposListBox, oSetposButton, oSetposAskForPosition, oSetposSelectedText, oModalResult, oListBoxItems;
    uint32_t oProgManCriticalSection, oRawEntrypointStride, oRawEntrypointName, oRoadVehicleItems, oMemArrayData, oMemArrayCount;
    // Version 2: the start clock (start date/time of a NEW_MAP session).
    uint32_t gStartYear, gStartDayOfYear, gStartHour, gStartMinute, gStartSecond, gTimeDayOfYear, gTimeYear, gTimeHour, gTimeMinute;
    uint32_t gFixedTimeFlag, gFixedTimeMode, gTimeHold, siteNewSituationStartTimeCopy;
    // The last map state (/last).
    uint32_t startRadioButton1Click, startComboBox1Change, oModeLastState, oMapCombo;
    // Cooperative exit.
    uint32_t mainCloseOmsi, gMainForm, gExitDone;
    // Weather strings.
    uint32_t lStrAsg, refreshWeather, gWeather, gActualWeather, vmtWeather, vmtActuWeather, oWeatherCloudType, oWeatherCloudTypeNum, oActuWeatherIcao;
    // Vehicle scripts.
    uint32_t vmtRoadVehicleInst, setStringVariable, triggerXml, soundTrigger, slotTriggerXml, slotSoundTrigger;
    // Start weather.
    uint32_t loadWeather, oActuWeatherActive, oActuWeatherLastDownloaded, oActuWeatherInvalidIcao, oActuWeatherProcess;
    // Version 3: the native pause menu (take-over, panel handlers).
    uint32_t gMenu2Form, vmtMenu2, vmtMainForm, tCustomFormShow;
    uint32_t menu2Panel1Click, menu2Panel2Click, menu2Panel3Click, menu2Panel4Click, menu2Panel5Click;
    uint32_t oFormVisible, oMainInFrame, oWinControlHandle, oMenu2Panel1, oMenu2Panel2, oMenu2Panel3, oMenu2Panel4, oMenu2Panel5;
};
constexpr uint32_t NativeTargetTableVersion = 4;
NativeTargetTable gNative{};
volatile LONG gTargetsConfigured = 0;

// Accepted once, before any hook is installed. Returns 1 when accepted.
extern "C" __declspec(dllexport) int __cdecl NativeConfigureTargets(const NativeTargetTable* table) {
    if (table == nullptr || table->version != NativeTargetTableVersion || table->size != sizeof(NativeTargetTable) ||
        table->imageBase == 0 || table->imageEnd <= table->imageBase) return 0;
    if (InterlockedCompareExchange(&gTargetsConfigured, 1, 0) != 0) return 0;
    gNative = *table;
    return 1;
}

// True when every listed table entry is resolved.
bool Have(std::initializer_list<uint32_t> entries) {
    if (gTargetsConfigured == 0) return false;
    for (auto entry : entries) if (entry == 0) return false;
    return true;
}

// Delphi RTL ABI (Classes.TStrings virtual method table of the compiler that
// built OMSI), not an OMSI build layout: TStrings.Get and TStrings.GetCount.
constexpr uint32_t TStringsGetVmtOffset = 0x0C;
constexpr uint32_t TStringsGetCountVmtOffset = 0x14;

namespace {

bool Readable(const void* pointer, size_t bytes);
bool PlausibleOmsiObject(void* object);
bool ReadUnicodeString(wchar_t* value, wchar_t* output, int capacity);
void DirectSetPosStubBody(void* self);
void HeadlessStartShowModalBody(void* self);
void HeadlessStartShowThunkBody(void* self, unsigned char visibility);
void HeadlessStartShowModalStub();
void HeadlessStartShowThunk();

// Copied in behavior, not architecture, from the pinned OmsiHook invoker's
// BorlandFastCall helper. The MakeVehicle ABI was runtime-reconciled for this
// exact executable in MakeVehicle-Reconciliation-002.
__declspec(naked) int __cdecl BorlandFastCall(int function, int registerArguments, int totalArguments, ...) {
    __asm {
        push ebp
        mov ebp, esp
        sub esp, 10h
        push ebx
        push esi
        push edi
        mov ebx, [ebp+10h]
        sub ebx, [ebp+0Ch]
        mov esi, [ebp+10h]
        lea esi, [ebp+10h+esi*4]
    copy_stack_arguments:
        cmp ebx, 0
        jz load_register_arguments
        push [esi]
        sub esi, 4
        dec ebx
        jmp copy_stack_arguments
    load_register_arguments:
        mov ebx, [ebp+0Ch]
        cmp ebx, 1
        jl invoke_target
        mov eax, [ebp+14h]
        cmp ebx, 2
        jl invoke_target
        mov edx, [ebp+18h]
        cmp ebx, 3
        jl invoke_target
        mov ecx, [ebp+1Ch]
    invoke_target:
        call dword ptr [ebp+8]
        pop edi
        pop esi
        pop ebx
        add esp, 10h
        mov esp, ebp
        pop ebp
        ret
    }
}

bool Readable(const void* pointer, size_t bytes) {
    if (pointer == nullptr || bytes == 0) return false;
    MEMORY_BASIC_INFORMATION memory{};
    if (VirtualQuery(pointer, &memory, sizeof(memory)) != sizeof(memory)) return false;
    if (memory.State != MEM_COMMIT || (memory.Protect & (PAGE_GUARD | PAGE_NOACCESS)) != 0) return false;
    const auto start = reinterpret_cast<uintptr_t>(pointer);
    const auto end = start + bytes;
    const auto regionEnd = reinterpret_cast<uintptr_t>(memory.BaseAddress) + memory.RegionSize;
    return end >= start && end <= regionEnd;
}

volatile uintptr_t gSetposSlot = 0;
volatile uintptr_t gSetposOriginal = 0;
volatile EntrypointDiagnostics* gEntrypointDiagnostics = nullptr;
volatile int gEntrypointActive = 0;
volatile int gEntrypointInvocationCount = 0;
volatile int gEntrypointSuccess = 0;
volatile int gRequestedPresentedIndex = 1;
const wchar_t* gRequestedEntrypointIdentity = nullptr;
EntrypointDiagnostics gLastEntrypointDiagnostics{};

volatile int gHeadlessStartPending = 0;
volatile int gHeadlessStartConsumed = 0;
volatile int gHeadlessFormShowExecuted = 0;
volatile int gHeadlessStartInstalled = 0;
volatile int gHeadlessModalResult = 0;

bool ReadStartForm(void** output) {
    if (output == nullptr) return false;
    *output = nullptr;
    if (!Have({ gNative.gStartForm, gNative.vmtStart })) return false;
    auto slot = reinterpret_cast<void**>(gNative.gStartForm);
    if (!Readable(slot, sizeof(void*)) || *slot == nullptr || !Readable(*slot, sizeof(void*))) return false;
    auto form = *slot;
    if (*reinterpret_cast<uintptr_t*>(form) != gNative.vmtStart) return false;
    *output = form;
    return true;
}

bool ResolveSituationIndex(void* form, const wchar_t* requestedSituation, int* indexOutput) {
    if (form == nullptr || requestedSituation == nullptr || *requestedSituation == L'\0' || indexOutput == nullptr) return false;
    *indexOutput = -1;
    if (!Have({ gNative.oSituationCollection })) return false;
    auto situations = *reinterpret_cast<wchar_t***>(reinterpret_cast<unsigned char*>(form) + gNative.oSituationCollection);
    if (situations == nullptr || !Readable(situations - 1, sizeof(int))) return false;
    const int count = *(reinterpret_cast<int*>(situations) - 1);
    if (count <= 0 || count > 4096 || !Readable(situations, static_cast<size_t>(count) * sizeof(wchar_t*))) return false;
    for (int index = 0; index < count; ++index) {
        wchar_t decoded[512]{};
        if (ReadUnicodeString(situations[index], decoded, static_cast<int>(_countof(decoded))) && _wcsicmp(decoded, requestedSituation) == 0) {
            *indexOutput = index;
            return true;
        }
    }
    return false;
}

bool SetRadioChecked(void* radio, bool checked) {
    if (radio == nullptr || !Readable(radio, sizeof(uintptr_t)) || !Have({ gNative.radioSetChecked })) return false;
    const uintptr_t target = gNative.radioSetChecked;
    __asm {
        mov eax, radio
        mov dl, checked
        mov ecx, target
        call ecx
    }
    return true;
}

bool SetSituationItemIndex(void* selector, int index) {
    if (selector == nullptr || index < 0 || !Readable(selector, sizeof(uintptr_t))) return false;
    if (!Have({ gNative.vmtListBox, gNative.slotListBoxSetItemIndex })) return false;
    const auto vmt = *reinterpret_cast<uintptr_t*>(selector);
    const uintptr_t setterOffset = gNative.slotListBoxSetItemIndex - gNative.vmtListBox;
    if (!Readable(reinterpret_cast<void*>(vmt + setterOffset), sizeof(uintptr_t))) return false;
    const auto setter = *reinterpret_cast<uintptr_t*>(vmt + setterOffset);
    if (setter < gNative.imageBase || setter >= gNative.imageEnd) return false;
    __asm {
        mov eax, selector
        mov edx, index
        mov ecx, setter
        call ecx
    }
    return true;
}

void InvokeStartModeChanged(void* form) {
    __asm {
        mov eax, form
        mov ecx, gNative.startButtonDriverNewClick
        call ecx
    }
}

void InvokeSituationSelectionChanged(void* form) {
    __asm {
        mov eax, form
        mov edx, form
        mov ecx, gNative.startSituationSelectionChanged
        call ecx
    }
}

void InvokeStartButton1Click(void* form) {
    __asm {
        mov eax, form
        mov ecx, gNative.startButton1Click
        call ecx
    }
}

// The askForPosition branch of TProgMan.NewSituation; its guard bytes were
// verified by the resolver before the table was accepted.
bool IsKnownNewSituation() {
    return Have({ gNative.siteNewSituationAskForPosition, gNative.newSituation });
}

bool SetVmtSlot(uintptr_t slot, uintptr_t value) {
    DWORD oldProtection = 0;
    if (!VirtualProtect(reinterpret_cast<void*>(slot), sizeof(uintptr_t), PAGE_READWRITE, &oldProtection)) return false;
    *reinterpret_cast<volatile uintptr_t*>(slot) = value;
    FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(slot), sizeof(uintptr_t));
    DWORD ignored = 0;
    VirtualProtect(reinterpret_cast<void*>(slot), sizeof(uintptr_t), oldProtection, &ignored);
    return true;
}

bool SetCodeBytes(uintptr_t address, const unsigned char* bytes, size_t size) {
    if (bytes == nullptr || size == 0 || !Readable(reinterpret_cast<void*>(address), size)) return false;
    DWORD oldProtection = 0;
    if (!VirtualProtect(reinterpret_cast<void*>(address), size, PAGE_EXECUTE_READWRITE, &oldProtection)) return false;
    memcpy(reinterpret_cast<void*>(address), bytes, size);
    FlushInstructionCache(GetCurrentProcess(), reinterpret_cast<void*>(address), size);
    DWORD ignored = 0;
    VirtualProtect(reinterpret_cast<void*>(address), size, oldProtection, &ignored);
    return true;
}

// True when a 5-byte call at site reaches target (E8 rel32).
bool CallReaches(uintptr_t site, uintptr_t target) {
    if (site == 0 || target == 0 || !Readable(reinterpret_cast<void*>(site), 5) || *reinterpret_cast<unsigned char*>(site) != 0xE8) return false;
    return site + 5 + *reinterpret_cast<LONG*>(site + 1) == target;
}

// The original call bytes are captured when the patch is installed.
unsigned char gVisualCallOriginal[5]{};
volatile LONG gVisualCallSaved = 0;

bool RestoreHeadlessVisualCall() {
    if (gVisualCallSaved == 0) return true;
    if (!SetCodeBytes(gNative.siteShowModalSetVisible, gVisualCallOriginal, sizeof(gVisualCallOriginal))) return false;
    gVisualCallSaved = 0;
    return true;
}

unsigned char gInternetTexturesOriginal[5]{};
static volatile LONG gInternetTexturesSuppressed = 0;

bool InstallInternetTexturesSuppression() {
    const uintptr_t target = gNative.downloadInternetTextures;
    if (!Have({ target }) || gInternetTexturesSuppressed != 0 || !Readable(reinterpret_cast<void*>(target), sizeof(gInternetTexturesOriginal))) return false;
    memcpy(gInternetTexturesOriginal, reinterpret_cast<const void*>(target), sizeof(gInternetTexturesOriginal));
    const unsigned char replacement[] = { 0xC3, 0x90, 0x90, 0x90, 0x90 };
    if (!SetCodeBytes(target, replacement, sizeof(replacement))) return false;
    gInternetTexturesSuppressed = 1;
    return true;
}

void RestoreInternetTexturesSuppression() {
    if (InterlockedExchange(&gInternetTexturesSuppressed, 0) != 0)
        SetCodeBytes(gNative.downloadInternetTextures, gInternetTexturesOriginal, sizeof(gInternetTexturesOriginal));
}

bool InstallHeadlessVisualCall() {
    const uintptr_t site = gNative.siteShowModalSetVisible;
    if (!CallReaches(site, gNative.setVisible)) return false;
    memcpy(gVisualCallOriginal, reinterpret_cast<const void*>(site), sizeof(gVisualCallOriginal));
    gVisualCallSaved = 1;
    unsigned char replacement[sizeof(gVisualCallOriginal)]{};
    replacement[0] = 0xE8;
    const auto next = site + sizeof(replacement);
    const auto target = reinterpret_cast<uintptr_t>(&HeadlessStartShowThunk);
    const LONG relative = static_cast<LONG>(target - next);
    memcpy(replacement + 1, &relative, sizeof(relative));
    if (!SetCodeBytes(site, replacement, sizeof(replacement))) { gVisualCallSaved = 0; return false; }
    return true;
}

void RestoreHeadlessStartHooks() {
    SetVmtSlot(gNative.slotStartShowModal, gNative.showModal);
    RestoreHeadlessVisualCall();
    gHeadlessStartPending = 0;
    gHeadlessStartConsumed = 0;
    gHeadlessStartInstalled = 0;
    gHeadlessFormShowExecuted = 0;
}

// Live-state check of the headless-start targets (the table was verified
// statically; this confirms the live form and the unpatched slot and call).
bool HeadlessFingerprint(void** form) {
    if (!Have({ gNative.slotStartShowModal, gNative.showModal, gNative.siteShowModalSetVisible, gNative.setVisible, gNative.startFormShow, gNative.startFormHide })) return false;
    if (!ReadStartForm(form)) return false;
    if (!Readable(reinterpret_cast<void*>(gNative.slotStartShowModal), sizeof(uintptr_t)) ||
        *reinterpret_cast<uintptr_t*>(gNative.slotStartShowModal) != gNative.showModal) return false;
    return CallReaches(gNative.siteShowModalSetVisible, gNative.setVisible);
}

// Headless-start readiness of the live Start form (capability boot.headless-start).
extern "C" __declspec(dllexport) int __cdecl NativeValidateBuild() {
    void* form = nullptr;
    return HeadlessFingerprint(&form) ? 1 : 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeSuppressInternetTextures() {
    return InstallInternetTexturesSuppression() ? 1 : 0;
}

extern "C" __declspec(dllexport) void __cdecl NativeRestoreInternetTextures() { RestoreInternetTexturesSuppression(); }

extern "C" __declspec(dllexport) void __cdecl NativeInstallMainThreadGateway() {
    // PluginRuntime owns scheduling. The native layer has no generic callback
    // registry and performs no work until a profile-owned semantic operation is queued.
}

extern "C" __declspec(dllexport) int __cdecl NativeArmHeadlessStart() {
    void* form = nullptr;
    if (!HeadlessFingerprint(&form)) return 0;
    if (!SetVmtSlot(gNative.slotStartShowModal, reinterpret_cast<uintptr_t>(&HeadlessStartShowModalStub))) return 0;
    gHeadlessStartPending = 1;
    gHeadlessStartConsumed = 0;
    gHeadlessFormShowExecuted = 0;
    gHeadlessStartInstalled = 1;
    return 1;
}

void __declspec(naked) HeadlessStartShowModalStub() {
    __asm {
        pushad
        push eax
        call HeadlessStartShowModalBody
        add esp, 4
        popad
        mov eax, gHeadlessModalResult
        ret
    }
}

void __declspec(naked) HeadlessStartShowThunk() {
    __asm {
        pushad
        push edx
        push eax
        call HeadlessStartShowThunkBody
        add esp, 8
        popad
        ret
    }
}

void HeadlessStartShowThunkBody(void* self, unsigned char visibility) {
    void* current = nullptr;
    if (!gHeadlessStartConsumed || !gHeadlessStartPending || visibility != 1 || !ReadStartForm(&current) || current != self) {
        const uintptr_t setVisible = gNative.setVisible;
        __asm {
            mov eax, self
            mov dl, visibility
            mov ecx, setVisible
            call ecx
        }
        return;
    }

    // Restore only after the live Tform_start has consumed the one-shot hook.
    RestoreHeadlessVisualCall();
    gHeadlessStartPending = 0;
    const uintptr_t formShow = gNative.startFormShow;
    __asm {
        mov eax, self
        mov edx, self
        mov ecx, formShow
        call ecx
    }
    gHeadlessFormShowExecuted = 1;
}

void HeadlessStartShowModalBody(void* self) {
    void* current = nullptr;
    if (!gHeadlessStartInstalled || !ReadStartForm(&current) || current != self) {
        gHeadlessModalResult = 2;
        RestoreHeadlessStartHooks();
        return;
    }

    // The VMT hook is one-shot; arm the visibility call only inside this
    // exact Tform_start ShowModal invocation.
    SetVmtSlot(gNative.slotStartShowModal, gNative.showModal);
    gHeadlessStartConsumed = 1;
    if (!InstallHeadlessVisualCall()) {
        gHeadlessStartConsumed = 0;
        gHeadlessStartInstalled = 0;
        gHeadlessStartPending = 0;
        gHeadlessModalResult = 2;
        return;
    }
    const uintptr_t showModal = gNative.showModal;
    __asm {
        mov eax, self
        mov ecx, showModal
        call ecx
        mov gHeadlessModalResult, eax
    }

    if (gHeadlessFormShowExecuted) {
        const uintptr_t formHide = gNative.startFormHide;
        __asm {
            mov eax, self
            mov edx, self
            mov ecx, formHide
            call ecx
        }
        gHeadlessFormShowExecuted = 0;
    }
    RestoreHeadlessStartHooks();
}

void RestoreSetposSlot() {
    if (gSetposSlot != 0 && gSetposOriginal != 0)
        SetVmtSlot(gSetposSlot, gSetposOriginal);
}

void CallFormShow(void* self) {
    const uintptr_t formShow = gNative.setposFormShow;
    __asm {
        mov eax, self
        mov edx, self
        mov ecx, formShow
        call ecx
    }
}

int ListBoxItemsCount(void* listBox, void** itemsOut) {
    int result = -1;
    if (itemsOut == nullptr) return result;
    *itemsOut = nullptr;
    if (listBox == nullptr || !Have({ gNative.oListBoxItems }) || !Readable(listBox, gNative.oListBoxItems + sizeof(void*))) return result;
    auto items = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(listBox) + gNative.oListBoxItems);
    if (!PlausibleOmsiObject(items)) return result;
    *itemsOut = items;
    const uint32_t getCount = TStringsGetCountVmtOffset;
    __asm {
        mov eax, items
        mov edx, [eax]
        add edx, getCount
        call dword ptr [edx]
        mov result, eax
    }
    return result;
}

int ListBoxItemIndex(void* listBox) {
    int result = -1;
    if (!Have({ gNative.vmtListBox, gNative.slotListBoxGetItemIndex })) return result;
    const uint32_t getter = gNative.slotListBoxGetItemIndex - gNative.vmtListBox;
    __asm {
        mov eax, listBox
        mov edx, [eax]
        add edx, getter
        call dword ptr [edx]
        mov result, eax
    }
    return result;
}

bool SetListBoxItemIndex(void* listBox, int index) {
    if (!Have({ gNative.vmtListBox, gNative.slotListBoxSetItemIndex })) return false;
    const uint32_t setter = gNative.slotListBoxSetItemIndex - gNative.vmtListBox;
    __asm {
        mov eax, listBox
        mov edx, index
        mov ecx, [eax]
        add ecx, setter
        call dword ptr [ecx]
    }
    return true;
}

bool GetListBoxItemText(void* listBox, int index, wchar_t** text) {
    if (text == nullptr) return false;
    *text = nullptr;
    if (!Have({ gNative.oListBoxItems })) return false;
    auto items = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(listBox) + gNative.oListBoxItems);
    if (!PlausibleOmsiObject(items)) return false;
    const uint32_t get = TStringsGetVmtOffset;
    __asm {
        mov eax, items
        mov edx, index
        lea ecx, text
        mov edi, [eax]
        add edi, get
        call dword ptr [edi]
    }
    return *text != nullptr;
}

bool ApplySelectedEntrypoint(void* setpos, EntrypointDiagnostics* diagnostics, int requestedIndex) {
    if (setpos == nullptr || diagnostics == nullptr) return false;
    diagnostics->setposSelf = reinterpret_cast<uintptr_t>(setpos);
    diagnostics->setposVmt = *reinterpret_cast<uintptr_t*>(setpos);
    if (!Have({ gNative.vmtSetpos, gNative.oSetposAskForPosition, gNative.oSetposListBox, gNative.oSetposButton, gNative.oSetposSelectedText, gNative.setposButton1Click }) ||
        diagnostics->setposVmt != gNative.vmtSetpos || *reinterpret_cast<unsigned char*>(reinterpret_cast<unsigned char*>(setpos) + gNative.oSetposAskForPosition) != 1) {
        diagnostics->status = 2;
        return false;
    }

    CallFormShow(setpos);
    diagnostics->formShowCalled = 1;
    auto listBox = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(setpos) + gNative.oSetposListBox);
    diagnostics->listBox = reinterpret_cast<uintptr_t>(listBox);
    if (!PlausibleOmsiObject(listBox)) {
        diagnostics->status = 3;
        return false;
    }
    void* items = nullptr;
    const int presentedCount = ListBoxItemsCount(listBox, &items);
    diagnostics->items = reinterpret_cast<uintptr_t>(items);
    if (items == nullptr) {
        diagnostics->status = 4;
        return false;
    }
    diagnostics->presentedCount = presentedCount;
    diagnostics->presentedIndex = requestedIndex;
    diagnostics->requestedPresentedIndex = requestedIndex;
    if (gRequestedEntrypointIdentity != nullptr && *gRequestedEntrypointIdentity != L'\0') {
        int resolvedIndex = -1;
        for (int index = 0; index < presentedCount; ++index) {
            wchar_t* text = nullptr;
            if (!GetListBoxItemText(listBox, index, &text) || text == nullptr) continue;
            // Exact and case-sensitive, as OMSI's own de-duplication (UStrEqual)
            // builds this list: two labels differing only by case are both presented.
            if (wcscmp(text, gRequestedEntrypointIdentity) != 0) continue;
            if (resolvedIndex >= 0) {
                diagnostics->status = 19; // Identity is ambiguous in this map's presented list.
                return false;
            }
            resolvedIndex = index;
        }
        if (resolvedIndex < 0) {
            diagnostics->status = 20; // Identity is absent from the presented list.
            return false;
        }
        requestedIndex = resolvedIndex;
        diagnostics->presentedIndex = resolvedIndex;
        diagnostics->requestedPresentedIndex = resolvedIndex;
    }
    if (requestedIndex < 0 || requestedIndex >= presentedCount) {
        diagnostics->status = 4;
        return false;
    }

    wchar_t* presentedName = nullptr;
    if (GetListBoxItemText(listBox, requestedIndex, &presentedName) && presentedName != nullptr)
        wcsncpy_s(diagnostics->presentedName, _countof(diagnostics->presentedName), presentedName, _TRUNCATE);

    diagnostics->itemIndexBefore = ListBoxItemIndex(listBox);
    SetListBoxItemIndex(listBox, requestedIndex);
    diagnostics->itemIndexAfter = ListBoxItemIndex(listBox);
    diagnostics->confirmedPresentedIndex = diagnostics->itemIndexAfter;
    if (diagnostics->itemIndexAfter != requestedIndex) {
        diagnostics->status = 15;
        return false;
    }
    // The VMT item-text getter is not stable across all VCL listbox backing
    // implementations. This profiled form field is the selected Unicode text.
    if (diagnostics->presentedName[0] == L'\0') {
        auto selectedText = *reinterpret_cast<wchar_t**>(reinterpret_cast<unsigned char*>(setpos) + gNative.oSetposSelectedText);
        ReadUnicodeString(selectedText, diagnostics->presentedName, _countof(diagnostics->presentedName));
    }

    auto button = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(setpos) + gNative.oSetposButton);
    if (!PlausibleOmsiObject(button)) {
        diagnostics->status = 11;
        return false;
    }
    const uintptr_t buttonClick = gNative.setposButton1Click;
    __asm {
        mov eax, setpos
        mov edx, button
        mov ecx, buttonClick
        call ecx
    }
    diagnostics->buttonCalled = 1;
    if (!Have({ gNative.gRawEntrypointIndex })) {
        diagnostics->status = 12;
        return false;
    }
    auto rawIndexGlobal = reinterpret_cast<int**>(gNative.gRawEntrypointIndex);
    if (!Readable(rawIndexGlobal, sizeof(void*)) || *rawIndexGlobal == nullptr || !Readable(*rawIndexGlobal, sizeof(int))) {
        diagnostics->status = 12;
        return false;
    }
    diagnostics->rawIndex = **rawIndexGlobal;
    // Raw entrypoint identity decoding is diagnostic only; it is skipped when
    // its record layout is not verified for this build.
    auto rawGlobal = reinterpret_cast<void**>(gNative.gRawEntrypoints);
    auto rawHolder = (Have({ gNative.gRawEntrypoints, gNative.oRawEntrypointStride, gNative.oRawEntrypointName }) && Readable(rawGlobal, sizeof(void*)) ? *rawGlobal : nullptr);
    auto raw = (rawHolder != nullptr && Readable(rawHolder, sizeof(void*)))
        ? *reinterpret_cast<void**>(rawHolder)
        : nullptr;
    diagnostics->rawArrayHolder = reinterpret_cast<uintptr_t>(rawHolder);
    if (raw == nullptr || !Readable(reinterpret_cast<unsigned char*>(raw) - 4, sizeof(int))) {
        diagnostics->rawDiagnosticStatus = 1;
    } else {
        const int rawCount = *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(raw) - 4);
        diagnostics->rawArray = reinterpret_cast<uintptr_t>(raw);
        diagnostics->rawCount = rawCount;
        if (diagnostics->rawIndex < 0 || diagnostics->rawIndex >= rawCount || diagnostics->rawIndex >= 4096) {
            diagnostics->rawDiagnosticStatus = 2;
        } else {
            auto rawRecord = reinterpret_cast<unsigned char*>(raw) + diagnostics->rawIndex * gNative.oRawEntrypointStride;
            auto rawName = *reinterpret_cast<wchar_t**>(rawRecord + gNative.oRawEntrypointName);
            diagnostics->identityDecodeStatus = ReadUnicodeString(rawName, diagnostics->rawName, _countof(diagnostics->rawName)) ? 1 : 0;
        }
    }
    diagnostics->status = 0;
    return true;
}

void __declspec(naked) DirectSetPosStub() {
    __asm {
        pushad
        push eax
        call DirectSetPosStubBody
        add esp, 4
        popad
        mov eax, 1
        ret
    }
}

void DirectSetPosStubBody(void* self) {
    if (!gEntrypointActive || ++gEntrypointInvocationCount != 1) {
        if (gEntrypointDiagnostics != nullptr) gEntrypointDiagnostics->status = 16;
        RestoreSetposSlot();
        return;
    }
    RestoreSetposSlot();
    gEntrypointSuccess = ApplySelectedEntrypoint(self, const_cast<EntrypointDiagnostics*>(gEntrypointDiagnostics), gRequestedPresentedIndex) ? 1 : 0;
}

bool PlausibleOmsiObject(void* object) {
    if (!Readable(object, sizeof(void*))) return false;
    const auto vmt = *reinterpret_cast<uintptr_t*>(object);
    return vmt >= gNative.imageBase && vmt < gNative.imageEnd && Readable(reinterpret_cast<void*>(vmt), sizeof(void*));
}

void* ResolveObject(uintptr_t globalAddress) {
    if (globalAddress == 0) return nullptr;
    auto global = reinterpret_cast<void***>(globalAddress);
    if (!Readable(global, sizeof(void**)) || *global == nullptr || !Readable(*global, sizeof(void*))) return nullptr;
    return **global;
}

namespace {
constexpr int MaximumRoadVehicles = 10000;

bool SnapshotRoadVehicles(void* roadVehicles, uintptr_t** items, int* count) {
    if (items == nullptr || count == nullptr || !PlausibleOmsiObject(roadVehicles)) return false;
    *items = nullptr;
    *count = 0;
    if (!Have({ gNative.oRoadVehicleItems, gNative.oMemArrayData, gNative.oMemArrayCount })) return false;
    auto wrapper = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(roadVehicles) + gNative.oRoadVehicleItems);
    if (wrapper == nullptr || !Readable(wrapper, (gNative.oMemArrayData > gNative.oMemArrayCount ? gNative.oMemArrayData : gNative.oMemArrayCount) + 4)) return false;
    const int itemCount = *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(wrapper) + gNative.oMemArrayCount);
    auto data = *reinterpret_cast<uintptr_t**>(reinterpret_cast<unsigned char*>(wrapper) + gNative.oMemArrayData);
    if (itemCount < 0 || itemCount > MaximumRoadVehicles || (itemCount > 0 && (data == nullptr || !Readable(data, static_cast<size_t>(itemCount) * sizeof(uintptr_t))))) return false;
    auto copy = itemCount == 0 ? nullptr : new (std::nothrow) uintptr_t[itemCount];
    if (itemCount > 0 && copy == nullptr) return false;
    for (int index = 0; index < itemCount; ++index) copy[index] = data[index];
    *items = copy;
    *count = itemCount;
    return true;
}

bool IsBeforeVehicle(uintptr_t candidate, const uintptr_t* before, int beforeCount) {
    for (int index = 0; index < beforeCount; ++index) if (before[index] == candidate) return true;
    return false;
}

// This mirrors OmsiHook's AllocateString(false): a Delphi AnsiString data
// pointer backed by the OMSI allocator. The path is passed to MakeVehicle;
// ownership is deliberately not reclaimed here because the reference call
// leaves its path allocation with the native operation.
char* AllocateMakeVehiclePath(const wchar_t* value) {
    if (value == nullptr || *value == L'\0') return nullptr;
    const int bytes = WideCharToMultiByte(1252, WC_NO_BEST_FIT_CHARS, value, -1, nullptr, 0, nullptr, nullptr);
    if (bytes <= 1) return nullptr;
    const int allocation = bytes + 12;
    if (!Have({ gNative.getMem })) return nullptr;
    const auto header = reinterpret_cast<unsigned char*>(static_cast<uintptr_t>(BorlandFastCall(static_cast<int>(gNative.getMem), 1, 1, allocation)));
    if (header == nullptr || !Readable(header, static_cast<size_t>(allocation))) return nullptr;
    BOOL usedDefault = FALSE;
    if (WideCharToMultiByte(1252, WC_NO_BEST_FIT_CHARS, value, -1, reinterpret_cast<char*>(header + 12), bytes, nullptr, &usedDefault) == 0 || usedDefault != FALSE) return nullptr;
    *reinterpret_cast<short*>(header) = 1252;
    *reinterpret_cast<short*>(header + 2) = 1;
    *reinterpret_cast<int*>(header + 4) = 1;
    *reinterpret_cast<int*>(header + 8) = bytes - 1;
    return reinterpret_cast<char*>(header + 12);
}
}

// Executes only the basic, reconciled MakeVehicle sequence. It accepts no
// player assignment, paint, HOF, fleet or registration behavior. Temp list
// cleanup is performed by CopyTempListIntoMainList for this build, as proven
// by MakeVehicle-Reconciliation-002.
extern "C" __declspec(dllexport) int __cdecl NativeMakeVehicleBasic(
    const wchar_t* busIdentity, NativeMakeVehicleDiagnostics* diagnostics) {
    if (diagnostics == nullptr) return 1;
    ZeroMemory(diagnostics, sizeof(*diagnostics));
    diagnostics->status = 1;
    if (busIdentity == nullptr || *busIdentity == L'\0' ||
        !Have({ gNative.gProgManLive, gNative.gRoadVehicles, gNative.gRoadVehicleTypes, gNative.vmtTempRvList, gNative.tempRvListCreate, gNative.makeVehicle, gNative.copyTempList, gNative.getMem, gNative.oProgManCriticalSection })) return diagnostics->status;

    auto progMan = *reinterpret_cast<void**>(gNative.gProgManLive);
    auto roadVehicles = *reinterpret_cast<void**>(gNative.gRoadVehicles);
    auto roadVehicleTypes = *reinterpret_cast<void**>(gNative.gRoadVehicleTypes);
    diagnostics->progMan = reinterpret_cast<uintptr_t>(progMan);
    diagnostics->roadVehicles = reinterpret_cast<uintptr_t>(roadVehicles);
    diagnostics->roadVehicleTypes = reinterpret_cast<uintptr_t>(roadVehicleTypes);
    if (!PlausibleOmsiObject(progMan)) { diagnostics->status = 2; return diagnostics->status; }
    if (!PlausibleOmsiObject(roadVehicles) || roadVehicleTypes == nullptr || !Readable(roadVehicleTypes, sizeof(void*))) { diagnostics->status = 3; return diagnostics->status; }

    uintptr_t* before = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &before, &diagnostics->beforeCount)) { diagnostics->status = 4; return diagnostics->status; }
    char* path = AllocateMakeVehiclePath(busIdentity);
    if (path == nullptr) { delete[] before; diagnostics->status = 5; return diagnostics->status; }

    auto criticalSection = reinterpret_cast<CRITICAL_SECTION*>(reinterpret_cast<unsigned char*>(progMan) + gNative.oProgManCriticalSection);
    if (!Readable(criticalSection, sizeof(CRITICAL_SECTION))) { delete[] before; diagnostics->status = 6; return diagnostics->status; }
    diagnostics->criticalSection = reinterpret_cast<uintptr_t>(criticalSection);
    int temporaryList = 0;
    bool lockHeld = false;
    __try {
        EnterCriticalSection(criticalSection);
        lockHeld = true;
        temporaryList = BorlandFastCall(static_cast<int>(gNative.tempRvListCreate), 2, 2, gNative.vmtTempRvList, 1);
        diagnostics->temporaryList = static_cast<uintptr_t>(temporaryList);
        if (temporaryList == 0 || !PlausibleOmsiObject(reinterpret_cast<void*>(static_cast<uintptr_t>(temporaryList)))) { diagnostics->status = 7; __leave; }
        diagnostics->rawReturn = BorlandFastCall(static_cast<int>(gNative.makeVehicle), 3, 25,
            progMan, temporaryList, roadVehicleTypes,
            0, 0, 0, 0, 0, 0, 0,
            -1, 1, 0, 2, 0,
            0, 0, 0, 0, -1, 0,
            0, 0, 0, path);
        // CopyTempListIntoMainList owns the temp-list finalization for this
        // exact build. Do not add a destructor or FreeMem call here.
        diagnostics->copyReturn = BorlandFastCall(static_cast<int>(gNative.copyTempList), 2, 2, roadVehicles, temporaryList);
        temporaryList = 0;
    } __finally {
        if (lockHeld) LeaveCriticalSection(criticalSection);
    }

    uintptr_t* after = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &after, &diagnostics->afterCount)) { delete[] before; diagnostics->status = 8; return diagnostics->status; }
    uintptr_t created = 0;
    int delta = 0;
    for (int index = 0; index < diagnostics->afterCount; ++index) {
        if (!IsBeforeVehicle(after[index], before, diagnostics->beforeCount)) { created = after[index]; ++delta; }
    }
    delete[] before;
    delete[] after;
    diagnostics->deltaCount = delta;
    diagnostics->createdVehicle = created;
    if (delta == 0) { diagnostics->status = 9; return diagnostics->status; }
    if (delta != 1) { diagnostics->status = 10; return diagnostics->status; }
    if (!PlausibleOmsiObject(reinterpret_cast<void*>(created))) { diagnostics->status = 11; return diagnostics->status; }
    diagnostics->createdVehicleVmt = *reinterpret_cast<uintptr_t*>(created);
    diagnostics->status = 0;
    return 0;
}

// Distinct from MakeVehicle: OMSI owns the AI/random placement policy and the
// returned integer is preserved as diagnostics only, never a public identity.
extern "C" __declspec(dllexport) int __cdecl NativePlaceRandomBus(
    int aiType, int group, int type, int scheduled, int tour, int line,
    int* rawReturn, int* beforeCount, int* afterCount) {
    if (rawReturn == nullptr || beforeCount == nullptr || afterCount == nullptr || !Have({ gNative.gProgManLive, gNative.gRoadVehicles, gNative.placeRandomBus })) return 1;
    *rawReturn = 0; *beforeCount = 0; *afterCount = 0;
    auto progMan = *reinterpret_cast<void**>(gNative.gProgManLive);
    auto roadVehicles = *reinterpret_cast<void**>(gNative.gRoadVehicles);
    if (!PlausibleOmsiObject(progMan) || !PlausibleOmsiObject(roadVehicles)) return 2;
    uintptr_t* before = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &before, beforeCount)) return 3;
    const int result = BorlandFastCall(static_cast<int>(gNative.placeRandomBus), 3, 11,
        progMan, aiType, group, 0, 0, 1, type, scheduled != 0, 0, tour, line);
    delete[] before;
    uintptr_t* after = nullptr;
    if (!SnapshotRoadVehicles(roadVehicles, &after, afterCount)) return 4;
    delete[] after;
    *rawReturn = result;
    return 0;
}

bool ReadUnicodeString(wchar_t* value, wchar_t* output, int capacity) {
    if (value == nullptr || output == nullptr || capacity < 2 ||
        !Readable(value - 2, sizeof(int))) return false;
    const int length = *(reinterpret_cast<int*>(value) - 1);
    if (length <= 0 || length >= capacity ||
        !Readable(value, static_cast<size_t>(length) * sizeof(wchar_t))) return false;
    wmemcpy_s(output, capacity, value, length);
    output[length] = L'\0';
    return true;
}

// Tform_start is an auto-created form in this known build. The slot is
// validated at runtime; no heap pointer is persisted.
void* ResolveStartForm() {
    void* form = nullptr;
    return ReadStartForm(&form) ? form : nullptr;
}

bool ResolveMapSource(const wchar_t* requestedMap, void** formOutput, wchar_t** sourceOutput, wchar_t* decoded, int capacity,
    CanonicalMapDiagnostics* diagnostics = nullptr) {
    if (requestedMap == nullptr || formOutput == nullptr || sourceOutput == nullptr || decoded == nullptr || capacity < 2) return false;
    *formOutput = nullptr;
    *sourceOutput = nullptr;
    decoded[0] = L'\0';

    auto form = ResolveStartForm();
    if (diagnostics != nullptr) {
        diagnostics->form = reinterpret_cast<uintptr_t>(form);
        diagnostics->formVmt = form != nullptr ? *reinterpret_cast<uintptr_t*>(form) : 0;
        diagnostics->scanStage = form == nullptr ? 1 : 2;
        diagnostics->entrySummary[0] = L'\0';
        diagnostics->failedIndex = -1;
    }
    if (form == nullptr || !Have({ gNative.oMapStrings, gNative.oMapCompanions })) return false;
    auto strings = *reinterpret_cast<wchar_t***>(reinterpret_cast<unsigned char*>(form) + gNative.oMapStrings);
    auto companions = *reinterpret_cast<int**>(reinterpret_cast<unsigned char*>(form) + gNative.oMapCompanions);
    if (diagnostics != nullptr) {
        diagnostics->stringArray = reinterpret_cast<uintptr_t>(strings);
        diagnostics->companionArray = reinterpret_cast<uintptr_t>(companions);
        diagnostics->scanStage = (strings == nullptr || companions == nullptr) ? 3 : 4;
    }
    if (strings == nullptr || companions == nullptr ||
        !Readable(strings - 1, sizeof(int)) || !Readable(companions - 1, sizeof(int))) return false;
    const int stringCount = *(reinterpret_cast<int*>(strings) - 1);
    const int companionCount = *(reinterpret_cast<int*>(companions) - 1);
    if (diagnostics != nullptr) {
        diagnostics->stringCount = stringCount;
        diagnostics->companionCount = companionCount;
        diagnostics->scanStage = 5;
    }
    if (stringCount <= 0 || stringCount > 4096 || companionCount != stringCount ||
        !Readable(strings, static_cast<size_t>(stringCount) * sizeof(wchar_t*)) ||
        !Readable(companions, static_cast<size_t>(companionCount) * sizeof(int))) return false;

    size_t summaryLength = 0;
    for (int i = 0; i < stringCount; ++i) {
        wchar_t* candidate = strings[i];
        wchar_t path[512]{};
        const bool readable = ReadUnicodeString(candidate, path, static_cast<int>(_countof(path)));
        if (readable && diagnostics != nullptr) {
            ++diagnostics->readableEntryCount;
            const auto remaining = _countof(diagnostics->entrySummary) - summaryLength;
            if (remaining > 1) {
                const int written = _snwprintf_s(diagnostics->entrySummary + summaryLength, remaining,
                    _TRUNCATE, L"[%d]=%ls;", i, path);
                if (written > 0) summaryLength += static_cast<size_t>(written);
            }
        }
        if (!readable && diagnostics != nullptr) diagnostics->failedIndex = i;
        if (readable && _wcsicmp(path, requestedMap) == 0) {
            wcsncpy_s(decoded, capacity, path, _TRUNCATE);
            *formOutput = form;
            *sourceOutput = candidate;
            if (diagnostics != nullptr) {
                diagnostics->grundorfIndex = i;
                diagnostics->sourceData = reinterpret_cast<uintptr_t>(candidate);
                diagnostics->sourceLength = *(reinterpret_cast<int*>(candidate) - 1);
                wcsncpy_s(diagnostics->sourceText, _countof(diagnostics->sourceText), path, _TRUNCATE);
                diagnostics->scanStage = 6;
            }
            return true;
        }
    }
    if (diagnostics != nullptr) diagnostics->scanStage = 7;
    return false;
}

}

void ClearCanonicalMapDiagnostics(CanonicalMapDiagnostics* diagnostics) {
    if (diagnostics != nullptr) {
        ZeroMemory(diagnostics, sizeof(*diagnostics));
    }
}

bool ReadUnicodeStringBounded(wchar_t* value, wchar_t* output, int capacity, int* lengthOutput) {
    if (lengthOutput != nullptr) *lengthOutput = 0;
    if (!ReadUnicodeString(value, output, capacity)) return false;
    if (lengthOutput != nullptr) *lengthOutput = static_cast<int>(wcslen(output));
    return true;
}

bool ReadAnsiStringBounded(unsigned char* value, wchar_t* output, int capacity, int* lengthOutput,
    int* codePageOutput, int* elementSizeOutput) {
    if (lengthOutput != nullptr) *lengthOutput = 0;
    if (codePageOutput != nullptr) *codePageOutput = 0;
    if (elementSizeOutput != nullptr) *elementSizeOutput = 0;
    if (value == nullptr || output == nullptr || capacity < 2 ||
        !Readable(value - 12, 12)) return false;
    const unsigned int metadata = *(reinterpret_cast<unsigned int*>(value) - 3);
    const unsigned int codePage = metadata & 0xffff;
    const unsigned int elementSize = metadata >> 16;
    const int length = *reinterpret_cast<int*>(value - 4);
    if (codePageOutput != nullptr) *codePageOutput = static_cast<int>(codePage);
    if (elementSizeOutput != nullptr) *elementSizeOutput = static_cast<int>(elementSize);
    if (elementSize != 1 || codePage != 0x04e4 || length <= 0 || length >= capacity ||
        !Readable(value, static_cast<size_t>(length))) return false;
    const int converted = MultiByteToWideChar(1252, 0, reinterpret_cast<const char*>(value), length,
        output, capacity - 1);
    if (converted <= 0) return false;
    output[converted] = L'\0';
    if (lengthOutput != nullptr) *lengthOutput = length;
    return true;
}

void CaptureRawString(void* value, unsigned int* header, unsigned char* bytes) {
    if (header != nullptr) ZeroMemory(header, 4 * sizeof(unsigned int));
    if (bytes != nullptr) ZeroMemory(bytes, 64);
    if (value == nullptr) return;
    auto data = reinterpret_cast<unsigned char*>(value);
    if (header != nullptr && Readable(data - 16, 16))
        memcpy(header, data - 16, 16);
    if (bytes != nullptr && Readable(data, 64))
        memcpy(bytes, data, 64);
}

extern "C" __declspec(dllexport) int __cdecl NativeAssignMapDetailed(
    const wchar_t* requestedMap, wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics);

extern "C" __declspec(dllexport) int __cdecl NativeAssignMapDetailed(
    const wchar_t* requestedMap, wchar_t* selectedMap, int selectedMapCapacity, CanonicalMapDiagnostics* diagnostics) {
    ClearCanonicalMapDiagnostics(diagnostics);
    void* form = nullptr;
    wchar_t* source = nullptr;
    if (requestedMap == nullptr || selectedMap == nullptr || selectedMapCapacity < 2) return 1;
    wchar_t sourcePath[512]{};
    if (!ResolveMapSource(requestedMap, &form, &source, sourcePath, static_cast<int>(_countof(sourcePath)), diagnostics)) return 2;

    if (diagnostics != nullptr) {
        diagnostics->form = reinterpret_cast<uintptr_t>(form);
        diagnostics->formVmt = *reinterpret_cast<uintptr_t*>(form);
        diagnostics->sourceData = reinterpret_cast<uintptr_t>(source);
        diagnostics->sourceLength = *(reinterpret_cast<int*>(source) - 1);
        wcsncpy_s(diagnostics->sourceText, _countof(diagnostics->sourceText), sourcePath, _TRUNCATE);
        CaptureRawString(source, diagnostics->sourceHeader, diagnostics->sourceBytes);
    }

    if (!Have({ gNative.gSelectedMap, gNative.uStrToLStr })) return 3;
    auto destinationSlot = reinterpret_cast<wchar_t***>(gNative.gSelectedMap);
    if (!Readable(destinationSlot, sizeof(wchar_t**)) || *destinationSlot == nullptr) return 3;
    const uintptr_t assignAnsi = gNative.uStrToLStr;
    if (diagnostics != nullptr) {
        diagnostics->destinationSlot = reinterpret_cast<uintptr_t>(destinationSlot);
        diagnostics->destinationVariable = reinterpret_cast<uintptr_t>(*destinationSlot);
        diagnostics->destinationBeforeData = reinterpret_cast<uintptr_t>(**destinationSlot);
        ReadUnicodeStringBounded(**destinationSlot, diagnostics->destinationBeforeText,
            static_cast<int>(_countof(diagnostics->destinationBeforeText)),
            &diagnostics->destinationBeforeLength);
        CaptureRawString(**destinationSlot, diagnostics->destinationBeforeHeader,
            diagnostics->destinationBeforeBytes);
    }
    __asm {
        mov eax, destinationSlot
        mov eax, [eax]
        mov edx, source
        xor ecx, ecx
        call assignAnsi
    }
    if (diagnostics != nullptr) diagnostics->helperCalled = 1;

    wchar_t* assigned = **destinationSlot;
    if (diagnostics != nullptr) {
        diagnostics->destinationAfterData = reinterpret_cast<uintptr_t>(assigned);
        CaptureRawString(assigned, diagnostics->destinationAfterHeader,
            diagnostics->destinationAfterBytes);
        diagnostics->readbackValid = ReadAnsiStringBounded(reinterpret_cast<unsigned char*>(assigned),
            diagnostics->destinationAfterText, static_cast<int>(_countof(diagnostics->destinationAfterText)),
            &diagnostics->destinationAfterLength, &diagnostics->destinationCodePage,
            &diagnostics->destinationElementSize) ? 1 : 0;
    }
    if (!ReadAnsiStringBounded(reinterpret_cast<unsigned char*>(assigned), selectedMap, selectedMapCapacity,
        nullptr, nullptr, nullptr) ||
        _wcsicmp(selectedMap, requestedMap) != 0) return 4;
    return 0;
}

// 0=ready, 1=ProgMan unresolved, 2=selected map unresolved, 3=map mismatch,
// 4=world already active, 5=AI config owner unresolved.
extern "C" __declspec(dllexport) int __cdecl NativeResolveWorld(
    void** progMan, void** aiConfig, wchar_t* selectedMap, int selectedMapCapacity, const wchar_t* expectedMap) {
    if (progMan == nullptr || aiConfig == nullptr || selectedMap == nullptr || selectedMapCapacity < 2) return 1;
    *progMan = nullptr;
    *aiConfig = nullptr;
    selectedMap[0] = L'\0';

    if (!Have({ gNative.gProgMan, gNative.gSelectedMap, gNative.gMap, gNative.gAiConfig })) return 1;
    void* resolvedProgMan = ResolveObject(gNative.gProgMan);
    if (!PlausibleOmsiObject(resolvedProgMan)) return 1;

    auto mapSlotGlobal = reinterpret_cast<wchar_t***>(gNative.gSelectedMap);
    if (!Readable(mapSlotGlobal, sizeof(wchar_t**)) || *mapSlotGlobal == nullptr || !Readable(*mapSlotGlobal, sizeof(wchar_t*))) return 2;
    auto mapData = reinterpret_cast<unsigned char*>(**mapSlotGlobal);
    if (!ReadAnsiStringBounded(mapData, selectedMap, selectedMapCapacity, nullptr, nullptr, nullptr) ||
        expectedMap == nullptr || _wcsicmp(selectedMap, expectedMap) != 0) return 3;

    if (ResolveObject(gNative.gMap) != nullptr) return 4;

    void* resolvedAiConfig = ResolveObject(gNative.gAiConfig);
    if (!PlausibleOmsiObject(resolvedAiConfig)) return 5;

    *progMan = resolvedProgMan;
    *aiConfig = resolvedAiConfig;
    return 0;
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldCloseMap(void* progMan) {
    const uintptr_t target = gNative.closeMap;
    if (target == 0) return;
    __asm {
        mov eax, progMan
        mov ecx, target
        call ecx
    }
}

extern "C" __declspec(dllexport) int __cdecl NativeWorldNewSituationDirect(
    void* progMan, int presentedIndex, EntrypointDiagnostics* diagnostics) {
    if (diagnostics == nullptr) return 0;
    ZeroMemory(diagnostics, sizeof(*diagnostics));
    gRequestedPresentedIndex = presentedIndex;
    if (!Have({ gNative.slotSetposShowModal, gNative.gSetposForm, gNative.vmtSetpos, gNative.showModal, gNative.newSituation })) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 1;
        return 0;
    }
    const uintptr_t slotAddress = gNative.slotSetposShowModal;
    auto setposGlobal = reinterpret_cast<void**>(gNative.gSetposForm);
    diagnostics->newSituationFingerprintOk = IsKnownNewSituation() ? 1 : 0;
    diagnostics->setposGlobalReadable = Readable(setposGlobal, sizeof(void*)) ? 1 : 0;
    diagnostics->setposSlotReadable = diagnostics->setposGlobalReadable && *setposGlobal != nullptr &&
        Readable(*setposGlobal, sizeof(void*)) ? 1 : 0;
    if (!diagnostics->newSituationFingerprintOk) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 1;
        return 0;
    }
    if (!diagnostics->setposGlobalReadable || !diagnostics->setposSlotReadable) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 2;
        return 0;
    }
    // NewSituation resolves the SetPos global to the form slot, then reads the live form object.
    auto setposSlot = *setposGlobal;
    auto setpos = *reinterpret_cast<void**>(setposSlot);
    diagnostics->setposObjectReadable = setpos != nullptr && Readable(setpos, sizeof(void*)) ? 1 : 0;
    if (setpos == nullptr || !Readable(setpos, sizeof(void*))) {
        diagnostics->status = 1;
        diagnostics->preflightStatus = 3;
        return 0;
    }
    const uintptr_t vmt = *reinterpret_cast<uintptr_t*>(setpos);
    diagnostics->setposSelf = reinterpret_cast<uintptr_t>(setpos);
    diagnostics->setposVmt = vmt;
    diagnostics->slotAddress = slotAddress;
    if (vmt != gNative.vmtSetpos ||
        !Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t)) ||
        *reinterpret_cast<uintptr_t*>(slotAddress) != gNative.showModal) {
        diagnostics->status = 2;
        diagnostics->originalTarget = Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t))
            ? *reinterpret_cast<uintptr_t*>(slotAddress) : 0;
        return 0;
    }
    diagnostics->originalTarget = gNative.showModal;
    gSetposSlot = slotAddress;
    gSetposOriginal = gNative.showModal;
    gEntrypointDiagnostics = diagnostics;
    gEntrypointInvocationCount = 0;
    gEntrypointSuccess = 0;
    gEntrypointActive = 1;
    if (!SetVmtSlot(slotAddress, reinterpret_cast<uintptr_t>(&DirectSetPosStub))) {
        gEntrypointActive = 0;
        gEntrypointDiagnostics = nullptr;
        diagnostics->status = 3;
        return 0;
    }
    unsigned char success = 0;
    const uintptr_t newSituation = gNative.newSituation;
    __try {
        __asm {
            push 1
            mov eax, progMan
            xor edx, edx
            mov cl, 1
            call newSituation
            mov success, al
        }
    } __finally {
        RestoreSetposSlot();
        gEntrypointActive = 0;
        gEntrypointDiagnostics = nullptr;
    }
    diagnostics->invocationCount = gEntrypointInvocationCount;
    diagnostics->slotRestored = Readable(reinterpret_cast<void*>(slotAddress), sizeof(uintptr_t)) &&
        *reinterpret_cast<uintptr_t*>(slotAddress) == gNative.showModal ? 1 : 0;
    if (!diagnostics->slotRestored) {
        diagnostics->status = 17;
        return 0;
    }
    if (gEntrypointInvocationCount != 1 || !gEntrypointSuccess) return 0;
    diagnostics->status = success != 0 ? 0 : 18;
    return success != 0 ? 1 : 0;
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldCalcAiSum(void* aiConfig) {
    const uintptr_t target = gNative.calcAiSum;
    if (target == 0) return;
    __asm {
        mov eax, aiConfig
        mov ecx, target
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldSetAiVehicles(void* progMan) {
    const uintptr_t target = gNative.setAiVehicles;
    if (target == 0) return;
    __asm {
        mov eax, progMan
        mov ecx, target
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldSetTime(void* progMan) {
    const uintptr_t target = gNative.setTime;
    if (target == 0) return;
    __asm {
        mov eax, progMan
        mov dl, 1
        mov ecx, target
        call ecx
    }
}

// Applies OMSI's profiled TProgMan.SetTime after the in-process adapter
// updates validated clock scalars, preserving dependent time invariants.
extern "C" __declspec(dllexport) int __cdecl NativeRuntimeApplyTime() {
    if (!Have({ gNative.gProgMan, gNative.setTime })) return 0;
    void* progMan = ResolveObject(gNative.gProgMan);
    if (!PlausibleOmsiObject(progMan)) return 0;
    NativeWorldSetTime(progMan);
    return 1;
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldInitializeScheduledAi(void* progMan) {
    const uintptr_t target = gNative.initializeScheduledAi;
    if (target == 0) return;
    __asm {
        mov eax, progMan
        mov ecx, target
        call ecx
    }
}

extern "C" __declspec(dllexport) void __cdecl NativeWorldFinalizeStart() {
    if (!Have({ gNative.gStartTransition, gNative.oModalResult })) return;
    auto transitionSlot = reinterpret_cast<unsigned char**>(gNative.gStartTransition);
    if (Readable(transitionSlot, sizeof(unsigned char*)) && *transitionSlot != nullptr && Readable(*transitionSlot, 1)) {
        **transitionSlot = 0;
    }
    auto start = ResolveStartForm();
    if (start != nullptr && Readable(reinterpret_cast<unsigned char*>(start) + gNative.oModalResult, sizeof(unsigned long))) {
        *reinterpret_cast<unsigned long*>(reinterpret_cast<unsigned char*>(start) + gNative.oModalResult) = 1;
    }
}

// ---- start date/time (research report beta4-static start-date-time.md).
// The values a NEW_MAP start asks for, set by the plugin before
// NativeStartNewMap. NewSituation copies OMSI's start clock into its running
// clock; writing the start clock right before it lets OMSI derive the season
// and the map timeline from the requested date before the map loads.
struct PendingStartTime { int present; int year, dayOfYear, hour, minute; float second; };
PendingStartTime gPendingStartTime{};

// present: 0x01 year, 0x02 day of year, 0x04 hour, 0x08 minute, 0x10 second.
// Returns 1 when every target the request needs is resolved.
extern "C" __declspec(dllexport) int __cdecl NativeSetStartDateTime(int present, int year, int dayOfYear, int hour, int minute, int second) {
    if ((present & ~0x1F) != 0 || (present & 0x01 && (year < 1 || year > 9999)) || (present & 0x02 && (dayOfYear < 1 || dayOfYear > 365)) ||
        (present & 0x04 && (hour < 0 || hour > 23)) || (present & 0x08 && (minute < 0 || minute > 59)) || (present & 0x10 && (second < 0 || second > 59))) return 0;
    if (present != 0 && !Have({ gNative.gStartYear, gNative.gStartDayOfYear, gNative.gStartHour, gNative.gStartMinute, gNative.gStartSecond, gNative.gTimeDayOfYear,
        gNative.gTimeYear, gNative.gTimeHour, gNative.gTimeMinute, gNative.gFixedTimeFlag, gNative.gFixedTimeMode, gNative.gTimeHold, gNative.siteNewSituationStartTimeCopy })) return 0;
    gPendingStartTime = PendingStartTime{ present, year, dayOfYear, hour, minute, static_cast<float>(second) };
    return 1;
}

namespace {

// 0 written; 6 OMSI would not use the start clock (fixed-time flags or a held
// clock): the request cannot apply.
int ApplyStartDateTime() {
    const auto& pending = gPendingStartTime;
    if (pending.present == 0) return 0;
    auto* fixedFlag = reinterpret_cast<unsigned char*>(gNative.gFixedTimeFlag);
    auto* fixedMode = reinterpret_cast<uint32_t*>(gNative.gFixedTimeMode);
    auto* hold = reinterpret_cast<unsigned char*>(gNative.gTimeHold);
    if (!Readable(fixedFlag, 1) || !Readable(fixedMode, 4) || !Readable(hold, 1) || *fixedFlag != 0 || *fixedMode != 0 || *hold != 0) return 6;
    auto write = [](uint32_t address, int value) { auto* target = reinterpret_cast<int*>(address); if (Readable(target, 4)) *target = value; };
    // Year first, as OMSI's own preparation does.
    if (pending.present & 0x01) write(gNative.gStartYear, pending.year);
    if (pending.present & 0x02) write(gNative.gStartDayOfYear, pending.dayOfYear);
    if (pending.present & 0x04) write(gNative.gStartHour, pending.hour);
    if (pending.present & 0x08) write(gNative.gStartMinute, pending.minute);
    if (pending.present & 0x10) { auto* second = reinterpret_cast<float*>(gNative.gStartSecond); if (Readable(second, 4)) *second = pending.second; }
    return 0;
}

// After NewSituation and SetTime: the running clock holds the request (the
// hour or minute may have advanced with the time that passed). 7 on mismatch.
int VerifyStartDateTime() {
    const auto& pending = gPendingStartTime;
    if (pending.present == 0) return 0;
    auto read = [](uint32_t address) { auto* source = reinterpret_cast<int*>(address); return Readable(source, 4) ? *source : -1; };
    if ((pending.present & 0x01) && read(gNative.gTimeYear) != pending.year) return 7;
    if ((pending.present & 0x02) && read(gNative.gTimeDayOfYear) != pending.dayOfYear) return 7;
    auto* hour = reinterpret_cast<unsigned char*>(gNative.gTimeHour);
    auto* minute = reinterpret_cast<unsigned char*>(gNative.gTimeMinute);
    if ((pending.present & 0x04) && (!Readable(hour, 1) || (*hour != pending.hour && *hour != (pending.hour + 1) % 24))) return 7;
    if ((pending.present & 0x08) && (!Readable(minute, 1) || (*minute != pending.minute && *minute != (pending.minute + 1) % 60))) return 7;
    return 0;
}

} // namespace

// Closed semantic operation used by PluginRuntime. No address, VMT slot, or
// raw OMSI object is exposed outside this build-profile-gated native boundary.
// Result: 0=success; non-zero values identify the first failed native stage
// (6 and 7: the requested start date/time could not be applied).
extern "C" __declspec(dllexport) int __cdecl NativeStartNewMap(
    const wchar_t* mapIdentity, int presentedIndex, const wchar_t* entrypointIdentity) {
    void* startForm = nullptr;
    if (!Have({ gNative.closeMap, gNative.calcAiSum, gNative.setAiVehicles, gNative.setTime, gNative.initializeScheduledAi, gNative.gStartTransition, gNative.oModalResult }) ||
        !ReadStartForm(&startForm) || !IsKnownNewSituation()) return 1;
    if (mapIdentity == nullptr || (presentedIndex < 0 && (entrypointIdentity == nullptr || *entrypointIdentity == L'\0'))) return 2;

    wchar_t selected[512]{};
    if (NativeAssignMapDetailed(mapIdentity, selected, static_cast<int>(_countof(selected)), nullptr) != 0) return 3;

    void* progMan = nullptr;
    void* aiConfig = nullptr;
    if (NativeResolveWorld(&progMan, &aiConfig, selected, static_cast<int>(_countof(selected)), mapIdentity) != 0) return 4;
    NativeWorldCloseMap(progMan);
    const int startTime = ApplyStartDateTime();
    if (startTime != 0) return startTime;

    EntrypointDiagnostics diagnostics{};
    gRequestedEntrypointIdentity = entrypointIdentity;
    const int accepted = NativeWorldNewSituationDirect(progMan, presentedIndex, &diagnostics);
    gRequestedEntrypointIdentity = nullptr;
    gLastEntrypointDiagnostics = diagnostics;
    if (accepted == 0) return diagnostics.status == 0 ? 5 : 100 + diagnostics.status;
    NativeWorldCalcAiSum(aiConfig);
    NativeWorldSetAiVehicles(progMan);
    NativeWorldSetTime(progMan);
    NativeWorldInitializeScheduledAi(progMan);
    const int verified = VerifyStartDateTime();
    gPendingStartTime = PendingStartTime{};
    if (verified != 0) return verified;
    NativeWorldFinalizeStart();
    return 0;
}

// This reproduces the Start form's own control sequence. The direct internal
// loader call is deliberately not used: it bypasses selection state and was
// observed to fail OMSI's runtime checks.
extern "C" __declspec(dllexport) int __cdecl NativeStartSavedSituation(const wchar_t* situationIdentity) {
    void* form = nullptr;
    int situationIndex = -1;
    if (!Have({ gNative.oModeNewMap, gNative.oModeSelectedMap, gNative.oModeSavedSituation, gNative.oSituationControl, gNative.oModalResult,
               gNative.startSituationSelectionChanged, gNative.startButtonDriverNewClick, gNative.startButton1Click, gNative.radioSetChecked }) ||
        !ReadStartForm(&form) || !ResolveSituationIndex(form, situationIdentity, &situationIndex)) return 1;

    auto newMapMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + gNative.oModeNewMap);
    auto selectedMapMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + gNative.oModeSelectedMap);
    auto savedSituationMode = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + gNative.oModeSavedSituation);
    auto selector = *reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(form) + gNative.oSituationControl);
    if (!SetRadioChecked(newMapMode, false) || !SetRadioChecked(selectedMapMode, false) || !SetRadioChecked(savedSituationMode, true) ||
        !SetSituationItemIndex(selector, situationIndex)) return 2;

    InvokeSituationSelectionChanged(form);
    InvokeStartModeChanged(form);
    InvokeStartButton1Click(form);
    return *reinterpret_cast<int*>(reinterpret_cast<unsigned char*>(form) + gNative.oModalResult) == 1 ? 0 : 3;
}

// LAST MAP STATE (/last), driven like a user would through the Start form's
// own handlers (research report beta4-static entrypoint-last-player-vehicle.md
// section 2): the last-state radio's click handler switches the mode and
// rebuilds the map list (only maps with a laststn.osn), the map list selects
// the requested map, and Button1Click loads <map folder>\laststn.osn.
// 1 not available, 2 the map is not in the last-state list, 3 OMSI did not
// accept the start.
extern "C" __declspec(dllexport) int __cdecl NativeStartLastMapState(const wchar_t* mapIdentity) {
    void* form = nullptr;
    if (mapIdentity == nullptr || !Have({ gNative.startRadioButton1Click, gNative.startComboBox1Change, gNative.startButton1Click, gNative.oModeLastState,
            gNative.oMapCombo, gNative.oMapStrings, gNative.oMapCompanions, gNative.oModalResult, gNative.vmtListBox, gNative.slotListBoxSetItemIndex }) ||
        !ReadStartForm(&form)) return 1;
    auto* base = reinterpret_cast<unsigned char*>(form);
    auto radio = *reinterpret_cast<void**>(base + gNative.oModeLastState);
    const uintptr_t radioClick = gNative.startRadioButton1Click;
    if (radio == nullptr) return 1;
    __asm {
        mov eax, form
        mov edx, radio
        mov ecx, radioClick
        call ecx
    }
    // The list now holds only the maps that have a last state.
    auto strings = *reinterpret_cast<wchar_t***>(base + gNative.oMapStrings);
    auto companions = *reinterpret_cast<int**>(base + gNative.oMapCompanions);
    if (strings == nullptr || companions == nullptr || !Readable(strings - 1, sizeof(int)) || !Readable(companions - 1, sizeof(int))) return 2;
    const int stringCount = *(reinterpret_cast<int*>(strings) - 1);
    const int presentedCount = *(reinterpret_cast<int*>(companions) - 1);
    if (stringCount <= 0 || stringCount > 4096 || presentedCount < 0 || presentedCount > stringCount ||
        !Readable(strings, static_cast<size_t>(stringCount) * sizeof(wchar_t*)) || !Readable(companions, static_cast<size_t>(presentedCount) * sizeof(int))) return 2;
    int presented = -1;
    for (int p = 0; p < presentedCount && presented < 0; ++p) {
        const int index = companions[p];
        wchar_t path[512]{};
        if (index >= 0 && index < stringCount && ReadUnicodeString(strings[index], path, static_cast<int>(_countof(path))) && _wcsicmp(path, mapIdentity) == 0) presented = p;
    }
    if (presented < 0) return 2;
    auto combo = *reinterpret_cast<void**>(base + gNative.oMapCombo);
    if (!SetSituationItemIndex(combo, presented)) return 2;
    const uintptr_t comboChange = gNative.startComboBox1Change;
    __asm {
        mov eax, form
        mov edx, combo
        mov ecx, comboChange
        call ecx
    }
    InvokeStartButton1Click(form);
    return *reinterpret_cast<int*>(base + gNative.oModalResult) == 1 ? 0 : 3;
}

// ---- cooperative exit (research report beta4-static runtime-map-data-shutdown.md
// section 5). OMSI's own TForm_main.CloseOMSI is what runs after a user
// confirms quitting: it saves options.cfg and the map's laststn.osn, calls
// every plugin's PluginFinalize, unloads the plugins and posts WM_QUIT. Every
// route through Form_main.Close asks the user first, so none is used. The
// call runs from a one-shot UI-thread timer of this (pinned) module, after the
// runtime command that asked for it has answered, outside RenderFrame.
namespace {
UINT_PTR gExitTimer = 0;

bool CooperativeExitAllowed() {
    if (!Have({ gNative.mainCloseOmsi, gNative.gMainForm, gNative.gExitDone })) return false;
    auto* mainForm = reinterpret_cast<void**>(gNative.gMainForm);
    auto* done = reinterpret_cast<unsigned char*>(gNative.gExitDone);
    return Readable(mainForm, sizeof(void*)) && *mainForm != nullptr && Readable(done, 1) && *done == 0;
}

void CALLBACK CooperativeExitTimer(HWND, UINT, UINT_PTR id, DWORD) {
    KillTimer(nullptr, id);
    gExitTimer = 0;
    if (!CooperativeExitAllowed()) return;
    void* mainForm = *reinterpret_cast<void**>(gNative.gMainForm);
    const uintptr_t closeOmsi = gNative.mainCloseOmsi;
    __asm {
        mov eax, mainForm
        mov ecx, closeOmsi
        call ecx
    }
}
} // namespace

// ---- native pause menu (research report beta4-static pause-menu-and-input.md
// sections 3 to 5 and 8). OMSI's pause is its own byte (non-zero: the
// simulation, the rendering, the 3D clicks and DirectInput are held) set by
// Tform_menu2.FormShow. A module that draws its own menu hides the native form
// through the VCL (the pause stays) and, for the menu's actions, calls the
// panel handlers OMSI's own clicks call. Nothing here clicks, posts a
// message or moves a window. Every call runs on the UI thread at message
// dispatch level (the plugin's UI timer), never inside the frame: the panel
// handlers can enter nested modal loops, so they run from a one-shot timer
// after the runtime command that asked has answered.
namespace {
constexpr unsigned long DelphiExceptionCode = 0x0EEDFADE;
enum UiAction : int { UiActionResume = 1, UiActionNewGame = 2, UiActionSave = 3, UiActionOptions = 4, UiActionQuit = 5 };
enum UiResult : int { UiResultNone = 0, UiResultResumed = 1, UiResultGameStarted = 2, UiResultReturned = 3, UiResultQuitRequested = 4, UiResultException = 5, UiResultRefused = 6 };

UINT_PTR gUiTimer = 0;
volatile LONG gUiBusy = 0;
volatile LONG gUiTakenOver = 0;
int gUiPendingAction = 0, gUiLastAction = 0, gUiLastResult = UiResultNone;
bool gUiPinned = false;

bool UiTargets() {
    return Have({ gNative.gMenu2Form, gNative.gMainForm, gNative.gStartTransition, gNative.vmtMenu2, gNative.vmtMainForm, gNative.setVisible, gNative.tCustomFormShow,
                  gNative.menu2Panel1Click, gNative.menu2Panel2Click, gNative.menu2Panel3Click, gNative.menu2Panel4Click, gNative.menu2Panel5Click,
                  gNative.oFormVisible, gNative.oMainInFrame, gNative.oMenu2Panel1, gNative.oMenu2Panel2, gNative.oMenu2Panel3, gNative.oMenu2Panel4, gNative.oMenu2Panel5 });
}

// The form behind a global variable, only when its class is the expected one
// (the first dword of a Delphi object is its VMT); null otherwise.
void* UiForm(uint32_t variable, uint32_t vmt) {
    auto* slot = reinterpret_cast<void**>(variable);
    if (!Readable(slot, sizeof(void*)) || *slot == nullptr) return nullptr;
    auto* form = *slot;
    if (!Readable(form, sizeof(uint32_t)) || *reinterpret_cast<uint32_t*>(form) != vmt) return nullptr;
    return form;
}

// OMSI's pause byte through its pointer cell; -1 when unreadable.
int UiPauseByte() {
    auto* cell = reinterpret_cast<unsigned char**>(gNative.gStartTransition);
    if (!Readable(cell, sizeof(void*)) || *cell == nullptr || !Readable(*cell, 1)) return -1;
    return **cell;
}

int UiByteAt(void* form, uint32_t offset) {
    auto* address = reinterpret_cast<unsigned char*>(form) + offset;
    return Readable(address, 1) ? *address : -1;
}

// TControl.SetVisible(Self = EAX, Value = DL); a Delphi exception (the VCL
// raises one for a bad state) is caught, never let into OMSI's loop.
bool UiSetVisible(void* form, unsigned char visible) {
    const uintptr_t setVisible = gNative.setVisible;
    __try {
        __asm {
            mov eax, form
            mov dl, visible
            mov ecx, setVisible
            call ecx
        }
        return true;
    } __except (GetExceptionCode() == DelphiExceptionCode ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH) { return false; }
}

// TCustomForm.Show(Self = EAX).
bool UiShow(void* form) {
    const uintptr_t show = gNative.tCustomFormShow;
    __try {
        __asm {
            mov eax, form
            mov ecx, show
            call ecx
        }
        return true;
    } __except (GetExceptionCode() == DelphiExceptionCode ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH) { return false; }
}

// A published handler of Tform_menu2: Self = EAX (the form), Sender = EDX (the
// panel; the five handlers do not read it).
bool UiCallHandler(uintptr_t handler, void* form, void* sender) {
    __try {
        __asm {
            mov eax, form
            mov edx, sender
            mov ecx, handler
            call ecx
        }
        return true;
    } __except (GetExceptionCode() == DelphiExceptionCode ? EXCEPTION_EXECUTE_HANDLER : EXCEPTION_CONTINUE_SEARCH) { return false; }
}

uintptr_t UiHandler(int action) {
    switch (action) {
    case UiActionResume: return gNative.menu2Panel1Click;
    case UiActionNewGame: return gNative.menu2Panel2Click;
    case UiActionSave: return gNative.menu2Panel3Click;
    case UiActionOptions: return gNative.menu2Panel4Click;
    case UiActionQuit: return gNative.menu2Panel5Click;
    }
    return 0;
}

uint32_t UiPanelOffset(int action) {
    switch (action) {
    case UiActionResume: return gNative.oMenu2Panel1;
    case UiActionNewGame: return gNative.oMenu2Panel2;
    case UiActionSave: return gNative.oMenu2Panel3;
    case UiActionOptions: return gNative.oMenu2Panel4;
    case UiActionQuit: return gNative.oMenu2Panel5;
    }
    return 0;
}

void CALLBACK UiActionTimer(HWND, UINT, UINT_PTR id, DWORD) {
    KillTimer(nullptr, id);
    gUiTimer = 0;
    const int action = gUiPendingAction;
    gUiPendingAction = 0;
    int result = UiResultRefused;
    if (UiTargets() && action >= UiActionResume && action <= UiActionQuit) {
        void* menu = UiForm(gNative.gMenu2Form, gNative.vmtMenu2);
        void* mainForm = UiForm(gNative.gMainForm, gNative.vmtMainForm);
        // Not inside the frame (a modal loop would re-enter it) and only while OMSI is paused.
        if (menu != nullptr && mainForm != nullptr && UiPauseByte() > 0 && UiByteAt(mainForm, gNative.oMainInFrame) == 0) {
            InterlockedExchange(&gUiBusy, 1);
            void* sender = nullptr;
            auto* panel = reinterpret_cast<void**>(reinterpret_cast<unsigned char*>(menu) + UiPanelOffset(action));
            if (Readable(panel, sizeof(void*))) sender = *panel;
            const bool completed = UiCallHandler(UiHandler(action), menu, sender);
            InterlockedExchange(&gUiBusy, 0);
            if (!completed) result = UiResultException;
            else if (action == UiActionResume) result = UiPauseByte() == 0 ? UiResultResumed : UiResultReturned;
            else if (action == UiActionNewGame) result = UiPauseByte() == 0 ? UiResultGameStarted : UiResultReturned;
            else if (action == UiActionQuit) { MSG quit; result = PeekMessageW(&quit, nullptr, WM_QUIT, WM_QUIT, PM_NOREMOVE) ? UiResultQuitRequested : UiResultReturned; }
            else result = UiResultReturned;
            // The game runs again: whatever the module had taken over is OMSI's again.
            if (UiPauseByte() == 0) InterlockedExchange(&gUiTakenOver, 0);
        }
    }
    gUiLastResult = result;
}
} // namespace

struct NativeUiState {
    int status;
    unsigned long paused, nativeMenuVisible, mainFormReady, inFrame, takenOver, busy, pendingAction, lastAction, lastResult;
};

// status 0: the fields are valid; 1: not resolved on this build or the forms
// are not there.
extern "C" __declspec(dllexport) int __cdecl NativeUiPauseState(NativeUiState* out) {
    if (out == nullptr) return 1;
    *out = {};
    out->status = 1;
    if (!UiTargets()) return 1;
    void* menu = UiForm(gNative.gMenu2Form, gNative.vmtMenu2);
    void* mainForm = UiForm(gNative.gMainForm, gNative.vmtMainForm);
    if (menu == nullptr || mainForm == nullptr) return 1;
    out->status = 0;
    out->paused = UiPauseByte() > 0 ? 1 : 0;
    out->nativeMenuVisible = UiByteAt(menu, gNative.oFormVisible) == 1 ? 1 : 0;
    out->mainFormReady = UiByteAt(mainForm, gNative.oFormVisible) == 1 ? 1 : 0;
    out->inFrame = UiByteAt(mainForm, gNative.oMainInFrame) == 1 ? 1 : 0;
    out->takenOver = gUiTakenOver != 0 ? 1 : 0;
    out->busy = (gUiBusy != 0 || gUiTimer != 0) ? 1 : 0;
    out->pendingAction = static_cast<unsigned long>(gUiPendingAction);
    out->lastAction = static_cast<unsigned long>(gUiLastAction);
    out->lastResult = static_cast<unsigned long>(gUiLastResult);
    return 0;
}

// Hides the native menu through the VCL; OMSI stays paused. 0 done; 1 not
// available; 2 not paused or the menu is not shown; 3 an action is running or
// the call is inside the frame; 4 already taken over; 5 the VCL raised.
extern "C" __declspec(dllexport) int __cdecl NativeUiPauseTakeOver() {
    if (!UiTargets()) return 1;
    void* menu = UiForm(gNative.gMenu2Form, gNative.vmtMenu2);
    void* mainForm = UiForm(gNative.gMainForm, gNative.vmtMainForm);
    if (menu == nullptr || mainForm == nullptr) return 1;
    if (gUiBusy != 0 || gUiTimer != 0 || UiByteAt(mainForm, gNative.oMainInFrame) != 0) return 3;
    if (gUiTakenOver != 0) return 4;
    if (UiPauseByte() <= 0 || UiByteAt(menu, gNative.oFormVisible) != 1) return 2;
    if (!UiSetVisible(menu, 0)) return 5;
    InterlockedExchange(&gUiTakenOver, 1);
    return 0;
}

// Gives the native menu back: Show again, which re-runs the native FormShow
// (pause stays set, the first key is swallowed again). 0 done (also when the
// menu was already shown); 1 not available; 2 OMSI is not paused (nothing to
// restore); 3 an action is running; 5 the VCL raised.
extern "C" __declspec(dllexport) int __cdecl NativeUiPauseRestoreNative() {
    if (!UiTargets()) return 1;
    void* menu = UiForm(gNative.gMenu2Form, gNative.vmtMenu2);
    if (menu == nullptr) return 1;
    if (gUiBusy != 0 || gUiTimer != 0) return 3;
    if (UiPauseByte() <= 0) { InterlockedExchange(&gUiTakenOver, 0); return 2; }
    if (UiByteAt(menu, gNative.oFormVisible) == 1) { InterlockedExchange(&gUiTakenOver, 0); return 0; }
    if (!UiShow(menu)) return 5;
    InterlockedExchange(&gUiTakenOver, 0);
    return 0;
}

// Schedules a panel handler (1 resume, 2 new game, 3 save, 4 options, 5 quit)
// from a one-shot UI-thread timer. 0 scheduled; 1 not available; 2 OMSI is not
// paused; 3 an action is already scheduled or running, or inside the frame; 4
// not an action; 5 the timer could not be created. The outcome is in
// NativeUiPauseState (lastAction, lastResult) once busy is 0.
extern "C" __declspec(dllexport) int __cdecl NativeUiPauseAction(int action) {
    if (action < UiActionResume || action > UiActionQuit) return 4;
    if (!UiTargets()) return 1;
    void* menu = UiForm(gNative.gMenu2Form, gNative.vmtMenu2);
    void* mainForm = UiForm(gNative.gMainForm, gNative.vmtMainForm);
    if (menu == nullptr || mainForm == nullptr) return 1;
    if (gUiBusy != 0 || gUiTimer != 0 || UiByteAt(mainForm, gNative.oMainInFrame) != 0) return 3;
    if (UiPauseByte() <= 0) return 2;
    if (!gUiPinned) {
        // Quitting unloads plugins; this module must stay mapped until the handler returns.
        HMODULE self = nullptr;
        if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN, reinterpret_cast<LPCWSTR>(&NativeUiPauseAction), &self)) return 5;
        gUiPinned = true;
    }
    gUiPendingAction = action;
    gUiLastAction = action;
    gUiLastResult = UiResultNone;
    gUiTimer = SetTimer(nullptr, 0, 0, UiActionTimer);
    if (gUiTimer == 0) { gUiPendingAction = 0; return 5; }
    return 0;
}

// ---- input capture (pause-menu-and-input.md section 6.5). OMSI reads its
// simulation keys through DirectInput and its mouse through VCL messages to
// the main form's view window; the native pause byte already holds the
// simulation. For a module's own menu the bridge installs ONE thread-local
// WH_GETMESSAGE hook on the UI thread, only while a module captures: the
// keyboard and mouse messages that belong to the game's window tree are
// recorded as plain events and removed (converted to WM_NULL) so the VCL does
// not also act on them. No window is subclassed, no user32 key-state function
// is hooked, nothing is sent. Native modal dialogs get real input because the
// module releases the capture first.
namespace {
struct NativeInputEvent { unsigned long kind; int key, x, y; unsigned long button; int delta, modifiers; };
constexpr int InputCapacity = 256;
NativeInputEvent gInputRing[InputCapacity];
int gInputHead = 0, gInputCount = 0;
HHOOK gInputHook = nullptr;
DWORD gInputThread = 0;
volatile LONG gInputCapturing = 0;
// Test seam: the self-check stands in for the game's window.
HWND gInputTestRoot = nullptr;

HWND InputRootWindow() {
    if (gInputTestRoot != nullptr) return gInputTestRoot;
    if (!Have({ gNative.gMainForm, gNative.vmtMainForm, gNative.oWinControlHandle })) return nullptr;
    auto* slot = reinterpret_cast<void**>(gNative.gMainForm);
    if (!Readable(slot, sizeof(void*)) || *slot == nullptr) return nullptr;
    auto* form = reinterpret_cast<unsigned char*>(*slot);
    if (!Readable(form, sizeof(uint32_t)) || *reinterpret_cast<uint32_t*>(form) != gNative.vmtMainForm) return nullptr;
    auto* handle = reinterpret_cast<HWND*>(form + gNative.oWinControlHandle);
    return Readable(handle, sizeof(HWND)) ? *handle : nullptr;
}

HWND InputMenuWindow() {
    if (gInputTestRoot != nullptr || !Have({ gNative.gMenu2Form, gNative.vmtMenu2, gNative.oWinControlHandle })) return nullptr;
    auto* slot = reinterpret_cast<void**>(gNative.gMenu2Form);
    if (!Readable(slot, sizeof(void*)) || *slot == nullptr) return nullptr;
    auto* form = reinterpret_cast<unsigned char*>(*slot);
    if (!Readable(form, sizeof(uint32_t)) || *reinterpret_cast<uint32_t*>(form) != gNative.vmtMenu2) return nullptr;
    auto* handle = reinterpret_cast<HWND*>(form + gNative.oWinControlHandle);
    return Readable(handle, sizeof(HWND)) ? *handle : nullptr;
}

void InputPush(const NativeInputEvent& event) {
    const int tail = (gInputHead + gInputCount) % InputCapacity;
    gInputRing[tail] = event;
    if (gInputCount < InputCapacity) ++gInputCount;
    else gInputHead = (gInputHead + 1) % InputCapacity;
}

int InputModifiers(WPARAM wParam, bool mouse) {
    int modifiers = 0;
    if (mouse) { if (wParam & MK_SHIFT) modifiers |= 1; if (wParam & MK_CONTROL) modifiers |= 2; }
    else { if (GetKeyState(VK_SHIFT) < 0) modifiers |= 1; if (GetKeyState(VK_CONTROL) < 0) modifiers |= 2; }
    if (GetKeyState(VK_MENU) < 0) modifiers |= 4;
    return modifiers;
}

// Records the event for a message of the game's window tree; false for a message that is not one.
bool InputRecord(const MSG& msg) {
    NativeInputEvent event{};
    switch (msg.message) {
    case WM_KEYDOWN: case WM_SYSKEYDOWN: event.kind = 1; event.key = static_cast<int>(msg.wParam); event.modifiers = InputModifiers(0, false); break;
    case WM_KEYUP: case WM_SYSKEYUP: event.kind = 2; event.key = static_cast<int>(msg.wParam); event.modifiers = InputModifiers(0, false); break;
    case WM_CHAR: case WM_SYSCHAR: event.kind = 3; event.key = static_cast<int>(msg.wParam); event.modifiers = InputModifiers(0, false); break;
    case WM_MOUSEMOVE: event.kind = 4; break;
    case WM_LBUTTONDOWN: event.kind = 5; event.button = 1; break;
    case WM_RBUTTONDOWN: event.kind = 5; event.button = 2; break;
    case WM_MBUTTONDOWN: event.kind = 5; event.button = 3; break;
    case WM_LBUTTONUP: event.kind = 6; event.button = 1; break;
    case WM_RBUTTONUP: event.kind = 6; event.button = 2; break;
    case WM_MBUTTONUP: event.kind = 6; event.button = 3; break;
    case WM_LBUTTONDBLCLK: event.kind = 8; event.button = 1; break;
    case WM_RBUTTONDBLCLK: event.kind = 8; event.button = 2; break;
    case WM_MBUTTONDBLCLK: event.kind = 8; event.button = 3; break;
    case WM_MOUSEWHEEL: event.kind = 7; event.delta = static_cast<short>(HIWORD(msg.wParam)); break;
    default: return false;
    }
    if (event.kind >= 4) {
        event.modifiers = InputModifiers(msg.wParam, true);
        POINT point{ static_cast<short>(LOWORD(msg.lParam)), static_cast<short>(HIWORD(msg.lParam)) };
        // The wheel reports screen coordinates; every other mouse message client ones.
        if (msg.message == WM_MOUSEWHEEL) ScreenToClient(msg.hwnd, &point);
        event.x = point.x; event.y = point.y;
    }
    InputPush(event);
    return true;
}

LRESULT CALLBACK InputHook(int code, WPARAM wParam, LPARAM lParam) {
    // Only a message that is being removed from the queue, only while captured.
    if (code == HC_ACTION && wParam == PM_REMOVE && gInputCapturing != 0 && lParam != 0) {
        auto* msg = reinterpret_cast<MSG*>(lParam);
        const bool keyboard = msg->message >= WM_KEYFIRST && msg->message <= WM_KEYLAST;
        const bool mouse = msg->message >= WM_MOUSEFIRST && msg->message <= WM_MOUSELAST;
        if ((keyboard || mouse) && msg->hwnd != nullptr) {
            const HWND root = InputRootWindow();
            const HWND menu = InputMenuWindow();
            if ((root != nullptr && (msg->hwnd == root || IsChild(root, msg->hwnd))) || (menu != nullptr && msg->hwnd == menu)) {
                if (InputRecord(*msg)) { msg->message = WM_NULL; msg->wParam = 0; msg->lParam = 0; }
            }
        }
    }
    return CallNextHookEx(nullptr, code, wParam, lParam);
}

HMODULE InputModule() {
    HMODULE self = nullptr;
    GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_UNCHANGED_REFCOUNT, reinterpret_cast<LPCWSTR>(&InputHook), &self);
    return self;
}
} // namespace

// Starts (1) or ends (0) the capture. 0 done; 1 not available (targets
// unresolved or no game window yet); 2 ending from another thread than the
// one that started; 3 starting while it is already on. Always on the UI thread.
extern "C" __declspec(dllexport) int __cdecl NativeInputCapture(int enable) {
    if (enable != 0) {
        if (gInputHook != nullptr) return 3;
        if (InputRootWindow() == nullptr) return 1;
        gInputThread = GetCurrentThreadId();
        gInputHook = SetWindowsHookExW(WH_GETMESSAGE, InputHook, InputModule(), gInputThread);
        if (gInputHook == nullptr) return 1;
        gInputHead = gInputCount = 0;
        InterlockedExchange(&gInputCapturing, 1);
        return 0;
    }
    if (gInputHook == nullptr) return 0;
    if (gInputThread != GetCurrentThreadId()) return 2;
    InterlockedExchange(&gInputCapturing, 0);
    UnhookWindowsHookEx(gInputHook);
    gInputHook = nullptr;
    gInputHead = gInputCount = 0;
    return 0;
}

// Copies and clears the captured events; returns how many were copied.
extern "C" __declspec(dllexport) int __cdecl NativeInputDrain(NativeInputEvent* events, int capacity) {
    if (events == nullptr || capacity <= 0) return 0;
    int copied = 0;
    while (gInputCount > 0 && copied < capacity) {
        events[copied++] = gInputRing[gInputHead];
        gInputHead = (gInputHead + 1) % InputCapacity;
        --gInputCount;
    }
    return copied;
}

// Offline check of the hook mechanics with a private window standing in for
// the game's: keyboard and mouse messages to it are recorded and removed,
// a message to another window is left alone, and ending the capture stops it.
// Bits: 0x01 keys recorded and swallowed, 0x02 mouse recorded and swallowed,
// 0x04 an unrelated window's message untouched, 0x08 nothing recorded after
// the capture ended, 0x10 the ring keeps the newest 256.
namespace {
int gInputProbeSeen = 0;
LRESULT CALLBACK InputProbeProc(HWND window, UINT message, WPARAM wParam, LPARAM lParam) {
    // TranslateMessage turns a WM_KEYDOWN into a WM_CHAR; only the key and the click are counted.
    if (message == WM_KEYDOWN || message == WM_LBUTTONDOWN) ++gInputProbeSeen;
    return DefWindowProcW(window, message, wParam, lParam);
}
void InputPump() { MSG msg; while (PeekMessageW(&msg, nullptr, 0, 0, PM_REMOVE)) { TranslateMessage(&msg); DispatchMessageW(&msg); } }
} // namespace

extern "C" __declspec(dllexport) int __cdecl NativeInputSelfCheck() {
    int passed = 0;
    WNDCLASSW cls{};
    cls.lpfnWndProc = InputProbeProc; cls.hInstance = InputModule(); cls.lpszClassName = L"OmsiLaunchInputProbe";
    RegisterClassW(&cls);
    HWND game = CreateWindowExW(0, cls.lpszClassName, L"game", WS_POPUP, 0, 0, 10, 10, nullptr, nullptr, cls.hInstance, nullptr);
    HWND other = CreateWindowExW(0, cls.lpszClassName, L"other", WS_POPUP, 0, 0, 10, 10, nullptr, nullptr, cls.hInstance, nullptr);
    if (game == nullptr || other == nullptr) return 0;
    gInputTestRoot = game;
    InputPump();
    if (NativeInputCapture(1) == 0) {
        gInputProbeSeen = 0;
        PostMessageW(game, WM_KEYDOWN, 'A', 0); PostMessageW(game, WM_CHAR, 'a', 0); PostMessageW(game, WM_KEYUP, 'A', 0);
        InputPump();
        NativeInputEvent events[16]{};
        int count = NativeInputDrain(events, 16);
        if (count == 3 && events[0].kind == 1 && events[0].key == 'A' && events[1].kind == 3 && events[1].key == 'a' && events[2].kind == 2 && gInputProbeSeen == 0) passed |= 0x01;
        PostMessageW(game, WM_LBUTTONDOWN, MK_LBUTTON, MAKELPARAM(12, 34)); PostMessageW(game, WM_MOUSEMOVE, 0, MAKELPARAM(5, 6));
        InputPump();
        count = NativeInputDrain(events, 16);
        if (count == 2 && events[0].kind == 5 && events[0].button == 1 && events[0].x == 12 && events[0].y == 34 && events[1].kind == 4 && events[1].x == 5 && gInputProbeSeen == 0) passed |= 0x02;
        gInputProbeSeen = 0;
        PostMessageW(other, WM_KEYDOWN, 'B', 0);
        InputPump();
        if (gInputProbeSeen == 1 && NativeInputDrain(events, 16) == 0) passed |= 0x04;
        for (int index = 0; index < 300; index++) PostMessageW(game, WM_KEYDOWN, 'A' + (index % 26), 0);
        InputPump();
        NativeInputEvent big[300]{};
        if (NativeInputDrain(big, 300) == InputCapacity) passed |= 0x10;
        NativeInputCapture(0);
        gInputProbeSeen = 0;
        PostMessageW(game, WM_KEYDOWN, 'C', 0);
        InputPump();
        if (gInputProbeSeen == 1 && NativeInputDrain(events, 16) == 0 && NativeInputCapture(0) == 0) passed |= 0x08;
    }
    NativeInputCapture(0);
    gInputTestRoot = nullptr;
    DestroyWindow(game); DestroyWindow(other);
    UnregisterClassW(cls.lpszClassName, cls.hInstance);
    return passed;
}

// ---- weather strings (research report beta4-static weather-and-delphi-strings.md
// sections 2.3, 3 and 5.2). Numeric source fields are plain stores done by the
// managed writer; the two AnsiString fields need OMSI's own @LStrAsg. The
// source is a constant-shaped string (refCnt -1) on this stack: @LStrAsg
// copies it into OMSI's heap and releases the old value, so OmsiLaunch never
// allocates, frees or aliases an OMSI string. UI thread only.
namespace {
struct AnsiConstant {
    uint16_t codePage;
    uint16_t elementSize;
    int32_t referenceCount;
    int32_t length;
    char chars[264];
};

// The UnicodeString counterpart (code page 1200, two-byte elements).
struct UnicodeConstant {
    uint16_t codePage;
    uint16_t elementSize;
    int32_t referenceCount;
    int32_t length;
    wchar_t chars[1028];
};

bool MakeUnicodeConstant(const wchar_t* text, int maxLength, UnicodeConstant& out) {
    if (text == nullptr || maxLength <= 0 || maxLength >= static_cast<int>(sizeof(out.chars) / sizeof(wchar_t))) return false;
    const int length = lstrlenW(text);
    if (length <= 0 || length > maxLength) return false;
    for (int index = 0; index < length; index++) out.chars[index] = text[index];
    out.chars[length] = 0;
    out.codePage = 1200;
    out.elementSize = 2;
    out.referenceCount = -1;
    out.length = length;
    return true;
}

// Code page 1252, as OMSI's own literals. False when empty, longer than
// maxLength or not representable.
bool MakeAnsiConstant(const wchar_t* text, int maxLength, AnsiConstant& out) {
    if (text == nullptr || maxLength <= 0 || maxLength >= static_cast<int>(sizeof(out.chars))) return false;
    const int wide = lstrlenW(text);
    if (wide <= 0 || wide > maxLength) return false;
    BOOL lossy = FALSE;
    const int bytes = WideCharToMultiByte(1252, WC_NO_BEST_FIT_CHARS, text, wide, out.chars, static_cast<int>(sizeof(out.chars)) - 1, nullptr, &lossy);
    if (bytes != wide || lossy) return false;
    out.chars[bytes] = 0;
    out.codePage = 1252;
    out.elementSize = 1;
    out.referenceCount = -1;
    out.length = bytes;
    return true;
}

void AssignAnsi(void* field, const AnsiConstant& source) {
    const char* chars = source.chars;
    const uintptr_t assign = gNative.lStrAsg;
    __asm {
        mov eax, field
        mov edx, chars
        mov ecx, assign
        call ecx
    }
}

// The instance a profiled global holds, when its class is exactly vmt.
unsigned char* LiveInstance(uint32_t global, uint32_t vmt) {
    auto* cell = reinterpret_cast<unsigned char**>(global);
    if (!Readable(cell, sizeof(void*)) || *cell == nullptr || !Readable(*cell, sizeof(uint32_t))) return nullptr;
    return *reinterpret_cast<uint32_t*>(*cell) == vmt ? *cell : nullptr;
}
} // namespace

// Sets actWeather.CloudType and lets OMSI resolve it (CloudTypeNum := -1, then
// RefreshWeather, as every OMSI writer does). 1 resolved; 2 OMSI did not find
// the cloud type (it logs it and draws none; "-1" is OMSI's own "no clouds");
// 3 invalid name; 0 unavailable.
extern "C" __declspec(dllexport) int __cdecl NativeSetWeatherCloudType(const wchar_t* name) {
    if (!Have({ gNative.lStrAsg, gNative.refreshWeather, gNative.gWeather, gNative.vmtWeather, gNative.oWeatherCloudType, gNative.oWeatherCloudTypeNum })) return 0;
    AnsiConstant source{};
    if (!MakeAnsiConstant(name, 64, source)) return 3;
    unsigned char* weather = LiveInstance(gNative.gWeather, gNative.vmtWeather);
    if (weather == nullptr || !Readable(weather + gNative.oWeatherCloudTypeNum, sizeof(int32_t))) return 0;
    AssignAnsi(weather + gNative.oWeatherCloudType, source);
    auto* cloudTypeNum = reinterpret_cast<int32_t*>(weather + gNative.oWeatherCloudTypeNum);
    *cloudTypeNum = -1;
    const uintptr_t refresh = gNative.refreshWeather;
    __asm {
        mov eax, weather
        mov ecx, refresh
        call ecx
    }
    return *cloudTypeNum >= 0 ? 1 : 2;
}

// Sets ActuWeather.ICAO. The managed caller then sets the METAR flags OMSI's
// weather dialog sets (active, invalid_icao, lastdownloaded, prozess).
// 1 assigned; 3 invalid; 0 unavailable.
extern "C" __declspec(dllexport) int __cdecl NativeSetMetarIcao(const wchar_t* icao) {
    if (!Have({ gNative.lStrAsg, gNative.gActualWeather, gNative.vmtActuWeather, gNative.oActuWeatherIcao })) return 0;
    AnsiConstant source{};
    if (!MakeAnsiConstant(icao, 4, source)) return 3;
    unsigned char* actual = LiveInstance(gNative.gActualWeather, gNative.vmtActuWeather);
    if (actual == nullptr || !Readable(actual + gNative.oActuWeatherIcao, sizeof(void*))) return 0;
    AssignAnsi(actual + gNative.oActuWeatherIcao, source);
    return 1;
}

// ---- start weather (same report, section 5.2 "Session-start weather"). Runs
// on the UI thread right after the native start path returned, before the
// first frame's Translate. mode: 1 preset (relative .owt path), 2 live METAR
// for icao, 3 live weather for the ICAO OMSI has configured.
// 0 applied; 1 unavailable; 2 OMSI has no ICAO configured; 3 invalid request.
extern "C" __declspec(dllexport) int __cdecl NativeApplyStartWeather(int mode, const wchar_t* preset, const wchar_t* icao) {
    if (!Have({ gNative.gWeather, gNative.vmtWeather, gNative.gActualWeather, gNative.vmtActuWeather, gNative.loadWeather, gNative.lStrAsg,
                gNative.oActuWeatherIcao, gNative.oActuWeatherActive, gNative.oActuWeatherLastDownloaded, gNative.oActuWeatherInvalidIcao, gNative.oActuWeatherProcess })) return 1;
    unsigned char* actual = LiveInstance(gNative.gActualWeather, gNative.vmtActuWeather);
    if (actual == nullptr || !Readable(actual + gNative.oActuWeatherProcess, 1)) return 1;
    if (mode == 1) {
        UnicodeConstant file{};
        if (!MakeUnicodeConstant(preset, 260, file)) return 3;
        unsigned char* weather = LiveInstance(gNative.gWeather, gNative.vmtWeather);
        if (weather == nullptr) return 1;
        // A live download would replace the preset.
        actual[gNative.oActuWeatherActive] = 0;
        const wchar_t* chars = file.chars;
        const uintptr_t target = gNative.loadWeather;
        __asm {
            mov eax, weather
            mov edx, chars
            xor ecx, ecx
            call target
        }
        return 0;
    }
    if (mode == 2) {
        AnsiConstant code{};
        if (!MakeAnsiConstant(icao, 4, code)) return 3;
        AssignAnsi(actual + gNative.oActuWeatherIcao, code);
    } else if (mode == 3) {
        const char* configured = *reinterpret_cast<char**>(actual + gNative.oActuWeatherIcao);
        if (configured == nullptr || !Readable(configured - 4, 4) || *reinterpret_cast<const int32_t*>(configured - 4) <= 0) return 2;
    } else {
        return 3;
    }
    // As OMSI's weather dialog requests live weather: the next update downloads.
    actual[gNative.oActuWeatherInvalidIcao] = 0;
    *reinterpret_cast<double*>(actual + gNative.oActuWeatherLastDownloaded) = 0.0;
    actual[gNative.oActuWeatherProcess] = 0;
    actual[gNative.oActuWeatherActive] = 1;
    return 0;
}

// ---- vehicle scripts (same report, section 4). OMSI's own setters run with
// constant-shaped sources; the managed caller has resolved the handle to a
// live road vehicle and checked that its script declares the string variable
// (SetStringVariable ignores unknown names). UI thread, at message level, where
// the calc thread is idle.
namespace {
bool LiveRoadVehicle(void* instance) {
    return instance != nullptr && Readable(instance, sizeof(uint32_t)) && *static_cast<uint32_t*>(instance) == gNative.vmtRoadVehicleInst;
}

// The virtual method in the profiled slot, when it is the expected one
// (an unexpected override is never called).
uintptr_t VirtualTarget(uint32_t slot, uint32_t expected) {
    auto* entry = reinterpret_cast<uintptr_t*>(slot);
    return Readable(entry, sizeof(uintptr_t)) && *entry == expected ? *entry : 0;
}
} // namespace

// 1 set; 3 invalid name or value; 0 unavailable. An empty value clears it.
extern "C" __declspec(dllexport) int __cdecl NativeSetScriptStringVariable(void* instance, const wchar_t* name, const wchar_t* value) {
    if (!Have({ gNative.vmtRoadVehicleInst, gNative.setStringVariable })) return 0;
    UnicodeConstant nameSource{};
    UnicodeConstant valueSource{};
    if (!MakeUnicodeConstant(name, 128, nameSource) || value == nullptr) return 3;
    const bool empty = value[0] == 0;
    if (!empty && !MakeUnicodeConstant(value, 1024, valueSource)) return 3;
    if (!LiveRoadVehicle(instance)) return 0;
    const wchar_t* nameChars = nameSource.chars;
    const wchar_t* valueChars = empty ? nullptr : valueSource.chars;
    const uintptr_t target = gNative.setStringVariable;
    __asm {
        mov eax, instance
        mov edx, nameChars
        mov ecx, valueChars
        call target
    }
    return 1;
}

// TriggerXML(name, value): 1 = press (name), 2 = drag (name_drag), 0 = release
// (name_off), as OMSI's KeyCommand and mouse handlers call it; the vehicle
// script entry runs synchronously. 1 dispatched; 3 invalid; 4 unexpected
// virtual method; 0 unavailable.
extern "C" __declspec(dllexport) int __cdecl NativeScriptTrigger(void* instance, const wchar_t* name, int value) {
    if (!Have({ gNative.vmtRoadVehicleInst, gNative.triggerXml, gNative.slotTriggerXml })) return 0;
    AnsiConstant source{};
    if (value < 0 || value > 2 || !MakeAnsiConstant(name, 128, source)) return 3;
    if (!LiveRoadVehicle(instance)) return 0;
    const uintptr_t target = VirtualTarget(gNative.slotTriggerXml, gNative.triggerXml);
    if (target == 0) return 4;
    const char* chars = source.chars;
    float ignored = 0;
    __asm {
        mov eax, instance
        mov edx, chars
        mov ecx, value
        call target
        fstp ignored
    }
    (void)ignored;
    return 1;
}

// SoundTrigger(trigger, filename). 1 dispatched; 3 invalid; 4 unexpected
// virtual method; 0 unavailable. The managed caller checked the sound pack.
extern "C" __declspec(dllexport) int __cdecl NativeScriptSoundTrigger(void* instance, const wchar_t* trigger, const wchar_t* filename) {
    if (!Have({ gNative.vmtRoadVehicleInst, gNative.soundTrigger, gNative.slotSoundTrigger })) return 0;
    AnsiConstant triggerSource{};
    AnsiConstant fileSource{};
    if (!MakeAnsiConstant(trigger, 128, triggerSource) || !MakeAnsiConstant(filename, 260, fileSource)) return 3;
    if (!LiveRoadVehicle(instance)) return 0;
    const uintptr_t target = VirtualTarget(gNative.slotSoundTrigger, gNative.soundTrigger);
    if (target == 0) return 4;
    const char* triggerChars = triggerSource.chars;
    const char* fileChars = fileSource.chars;
    __asm {
        mov eax, instance
        mov edx, triggerChars
        mov ecx, fileChars
        call target
    }
    return 1;
}

// 1 scheduled; 0 not available (unresolved targets, OMSI already closing, or
// called off the UI thread's message loop).
extern "C" __declspec(dllexport) int __cdecl NativeRequestCooperativeExit() {
    if (!CooperativeExitAllowed()) return 0;
    if (gExitTimer != 0) return 1;
    HMODULE self = nullptr;
    // OMSI unloads the plugin while the exit runs; this module stays.
    if (!GetModuleHandleExW(GET_MODULE_HANDLE_EX_FLAG_FROM_ADDRESS | GET_MODULE_HANDLE_EX_FLAG_PIN, reinterpret_cast<LPCWSTR>(&NativeRequestCooperativeExit), &self)) return 0;
    gExitTimer = SetTimer(nullptr, 0, 0, CooperativeExitTimer);
    return gExitTimer != 0 ? 1 : 0;
}

// Copies a completed selection's semantic diagnostics. It never exposes an
// OMSI object pointer or reads live OMSI memory after the selection returned.
extern "C" __declspec(dllexport) int __cdecl NativeGetLastEntrypointSelection(
    int* presentedIndex, int* rawIndex,
    wchar_t* presentedName, int presentedNameCapacity,
    wchar_t* rawName, int rawNameCapacity) {
    if (presentedIndex == nullptr || rawIndex == nullptr || presentedName == nullptr || rawName == nullptr ||
        presentedNameCapacity < 2 || rawNameCapacity < 2 || gLastEntrypointDiagnostics.status != 0) return 0;
    *presentedIndex = gLastEntrypointDiagnostics.confirmedPresentedIndex;
    *rawIndex = gLastEntrypointDiagnostics.rawIndex;
    wcsncpy_s(presentedName, presentedNameCapacity, gLastEntrypointDiagnostics.presentedName, _TRUNCATE);
    wcsncpy_s(rawName, rawNameCapacity, gLastEntrypointDiagnostics.rawName, _TRUNCATE);
    return 1;
}

namespace {
constexpr unsigned long D3DStateNotReady = 0;
constexpr unsigned long D3DStateReady = 1;
constexpr unsigned long D3DStateLost = 2;
constexpr unsigned long D3DStateResetting = 3;
constexpr unsigned long D3DStateStopping = 4;
constexpr unsigned long D3DStateStopped = 5;
constexpr unsigned long D3DTransitionNone = 0;
constexpr unsigned long D3DTransitionReady = 1;
constexpr unsigned long D3DTransitionLost = 2;
constexpr unsigned long D3DTransitionResetting = 3;
constexpr unsigned long D3DTransitionRestored = 4;
constexpr unsigned long D3DTransitionStopped = 5;
constexpr unsigned long TextureUnused = 0;
constexpr unsigned long TextureLive = 1;
constexpr unsigned long TextureReleased = 2;
constexpr unsigned long TextureStale = 3;
constexpr int D3DTextureCapacity = 128;
constexpr unsigned long D3DTransitionCapacity = 32;

struct D3DTextureEntry {
    unsigned __int64 handle;
    IDirect3DTexture9* texture;
    unsigned long state;
    unsigned long generation;
    unsigned long width;
    unsigned long height;
    unsigned long format;
    unsigned long levels;
};

SRWLOCK gD3DLock = SRWLOCK_INIT;
IDirect3DDevice9* gD3DDevice = nullptr;
IUnknown* gD3DCandidate = nullptr;
unsigned long gD3DState = D3DStateNotReady;
unsigned long gD3DGeneration = 0;
unsigned long gD3DNextHandle = 0;
unsigned long gD3DOwnerThread = 0;
unsigned long gD3DResetThread = 0;
unsigned long gD3DTransitions[D3DTransitionCapacity]{};
unsigned long gD3DTransitionRead = 0;
unsigned long gD3DTransitionWrite = 0;
unsigned long gD3DTransitionCount = 0;
D3DTextureEntry gD3DTextures[D3DTextureCapacity]{};
using D3DResetMethod = HRESULT (STDMETHODCALLTYPE*)(IDirect3DDevice9*, D3DPRESENT_PARAMETERS*);
D3DResetMethod gD3DOriginalReset = nullptr;
void** gD3DResetSlot = nullptr;
// The product's Reset hook is recorded in the hook registry so module hooks
// on the device can stack on it.
int gD3DResetHookId = 0;

void QueueTransition(unsigned long transition) {
    if (transition == D3DTransitionNone || transition > D3DTransitionStopped) return;
    if (gD3DTransitionCount == D3DTransitionCapacity) {
        gD3DTransitionRead = (gD3DTransitionRead + 1) % D3DTransitionCapacity;
        --gD3DTransitionCount;
    }
    gD3DTransitions[gD3DTransitionWrite] = transition;
    gD3DTransitionWrite = (gD3DTransitionWrite + 1) % D3DTransitionCapacity;
    ++gD3DTransitionCount;
}

unsigned long PopTransition() {
    if (gD3DTransitionCount == 0) return D3DTransitionNone;
    const auto transition = gD3DTransitions[gD3DTransitionRead];
    gD3DTransitionRead = (gD3DTransitionRead + 1) % D3DTransitionCapacity;
    --gD3DTransitionCount;
    return transition;
}

unsigned long LiveTextureCount() {
    unsigned long count = 0;
    for (const auto& entry : gD3DTextures) if (entry.state == TextureLive && entry.texture != nullptr) ++count;
    return count;
}

void InvalidateTextures(unsigned long state) {
    for (auto& entry : gD3DTextures) {
        if (entry.state != TextureLive || entry.texture == nullptr) continue;
        entry.texture->Release();
        entry.texture = nullptr;
        entry.state = state;
    }
}

void ReleaseDevice() {
    if (gD3DDevice != nullptr) {
        gD3DDevice->Release();
        gD3DDevice = nullptr;
    }
    gD3DCandidate = nullptr;
}

HRESULT STDMETHODCALLTYPE D3DResetHook(IDirect3DDevice9* self, D3DPRESENT_PARAMETERS* parameters) {
    D3DResetMethod original = nullptr;
    AcquireSRWLockExclusive(&gD3DLock);
    original = gD3DOriginalReset;
    gD3DResetThread = GetCurrentThreadId();
    gD3DState = D3DStateResetting;
    InvalidateTextures(TextureStale);
    ++gD3DGeneration;
    QueueTransition(D3DTransitionResetting);
    ReleaseSRWLockExclusive(&gD3DLock);

    if (original == nullptr) return D3DERR_INVALIDCALL;
    const auto result = original(self, parameters);

    AcquireSRWLockExclusive(&gD3DLock);
    if (SUCCEEDED(result)) {
        gD3DState = D3DStateReady;
        QueueTransition(D3DTransitionRestored);
    } else {
        gD3DState = result == D3DERR_DEVICENOTRESET ? D3DStateResetting : D3DStateLost;
        QueueTransition(gD3DState == D3DStateResetting ? D3DTransitionResetting : D3DTransitionLost);
    }
    ReleaseSRWLockExclusive(&gD3DLock);
    return result;
}

bool InstallD3DResetHook(IDirect3DDevice9* device) {
    if (gD3DOriginalReset != nullptr) return true;
    if (device == nullptr || !Readable(device, sizeof(void*))) return false;
    auto** vtable = *reinterpret_cast<void***>(device);
    if (vtable == nullptr || !Readable(vtable + 16, sizeof(void*))) return false;
    auto** slot = vtable + 16;
    auto* original = reinterpret_cast<D3DResetMethod>(*slot);
    if (original == nullptr || original == &D3DResetHook) return false;
    DWORD oldProtection = 0;
    if (!VirtualProtect(slot, sizeof(void*), PAGE_READWRITE, &oldProtection)) return false;
    *slot = reinterpret_cast<void*>(&D3DResetHook);
    DWORD ignored = 0;
    VirtualProtect(slot, sizeof(void*), oldProtection, &ignored);
    gD3DOriginalReset = original;
    gD3DResetSlot = slot;
    HookAdoptSlot("omsilaunch.d3d", slot, reinterpret_cast<void*>(original), reinterpret_cast<void*>(&D3DResetHook), &gD3DResetHookId);
    return true;
}

void RemoveD3DResetHook() {
    // A hook stacked on top of ours (a module that has not released it yet)
    // still calls through D3DResetHook: keep the hook and its original then.
    if (gD3DResetHookId != 0 && HookForget(gD3DResetHookId) != 0) return;
    gD3DResetHookId = 0;
    if (gD3DResetSlot != nullptr && gD3DOriginalReset != nullptr && Readable(gD3DResetSlot, sizeof(void*)) &&
        *gD3DResetSlot == reinterpret_cast<void*>(&D3DResetHook)) {
        DWORD oldProtection = 0;
        if (VirtualProtect(gD3DResetSlot, sizeof(void*), PAGE_READWRITE, &oldProtection)) {
            *gD3DResetSlot = reinterpret_cast<void*>(gD3DOriginalReset);
            DWORD ignored = 0;
            VirtualProtect(gD3DResetSlot, sizeof(void*), oldProtection, &ignored);
        }
    }
    gD3DResetSlot = nullptr;
    gD3DOriginalReset = nullptr;
}


unsigned long MapLifecycleState(long cooperative) {
    if (cooperative == D3D_OK) return D3DStateReady;
    if (cooperative == D3DERR_DEVICELOST) return D3DStateLost;
    if (cooperative == D3DERR_DEVICENOTRESET) return D3DStateResetting;
    return D3DStateNotReady;
}

unsigned long ObserveLifecycle(long* queryInterfaceResult, long* cooperativeResult, uintptr_t* candidateValue) {
    if (queryInterfaceResult != nullptr) *queryInterfaceResult = E_PENDING;
    if (cooperativeResult != nullptr) *cooperativeResult = E_PENDING;
    auto* candidate = gNative.gD3DDevice != 0 && Readable(reinterpret_cast<void*>(gNative.gD3DDevice), sizeof(void*))
        ? *reinterpret_cast<IUnknown**>(gNative.gD3DDevice) : nullptr;
    if (candidateValue != nullptr) *candidateValue = reinterpret_cast<uintptr_t>(candidate);

    if (candidate == nullptr || !Readable(candidate, sizeof(void*))) {
        if (gD3DState == D3DStateReady || gD3DState == D3DStateLost || gD3DState == D3DStateResetting) {
            InvalidateTextures(TextureStale);
            RemoveD3DResetHook();
            ReleaseDevice();
            ++gD3DGeneration;
            gD3DState = D3DStateLost;
            QueueTransition(D3DTransitionLost);
            return PopTransition();
        }
        gD3DState = D3DStateNotReady;
        return PopTransition();
    }

    unsigned long transition = D3DTransitionNone;
    if (gD3DDevice == nullptr || candidate != gD3DCandidate) {
        const bool replacing = gD3DDevice != nullptr;
        if (replacing) {
            InvalidateTextures(TextureStale);
            RemoveD3DResetHook();
            ReleaseDevice();
            ++gD3DGeneration;
        }
        IDirect3DDevice9* acquired = nullptr;
        const auto query = candidate->QueryInterface(__uuidof(IDirect3DDevice9), reinterpret_cast<void**>(&acquired));
        if (queryInterfaceResult != nullptr) *queryInterfaceResult = query;
        if (FAILED(query) || acquired == nullptr) {
            gD3DState = D3DStateNotReady;
            return D3DTransitionNone;
        }
        gD3DDevice = acquired;
        gD3DCandidate = candidate;
        ++gD3DGeneration;
        if (!InstallD3DResetHook(gD3DDevice)) {
            ReleaseDevice();
            gD3DState = D3DStateNotReady;
            return PopTransition();
        }
        transition = replacing ? D3DTransitionRestored : D3DTransitionReady;
    } else if (queryInterfaceResult != nullptr) {
        *queryInterfaceResult = S_OK;
    }

    const auto previous = gD3DState;
    const auto cooperative = gD3DDevice->TestCooperativeLevel();
    if (cooperativeResult != nullptr) *cooperativeResult = cooperative;
    const auto observed = MapLifecycleState(cooperative);
    if (observed == D3DStateLost || observed == D3DStateResetting) {
        if (previous != observed) {
            InvalidateTextures(TextureStale);
            ++gD3DGeneration;
            transition = observed == D3DStateLost ? D3DTransitionLost : D3DTransitionResetting;
        }
    } else if (observed == D3DStateReady && (previous == D3DStateLost || previous == D3DStateResetting)) {
        ++gD3DGeneration;
        transition = D3DTransitionRestored;
    }
    gD3DState = observed;
    gD3DOwnerThread = GetCurrentThreadId();
    QueueTransition(transition);
    return PopTransition();
}

D3DTextureEntry* FindTexture(unsigned __int64 handle) {
    for (auto& entry : gD3DTextures) if (entry.handle == handle && entry.state != TextureUnused) return &entry;
    return nullptr;
}

D3DTextureEntry* FindTextureSlot() {
    for (auto& entry : gD3DTextures) if (entry.state != TextureLive) return &entry;
    return nullptr;
}

bool SupportedTextureFormat(unsigned long format, unsigned long* bytesPerPixel) {
    unsigned long bytes = 0;
    switch (static_cast<D3DFORMAT>(format)) {
        case D3DFMT_A8R8G8B8: case D3DFMT_X8R8G8B8: bytes = 4; break;
        case D3DFMT_R5G6B5: case D3DFMT_X1R5G5B5: case D3DFMT_A1R5G5B5:
        case D3DFMT_A4R4G4B4: case D3DFMT_A8L8: bytes = 2; break;
        case D3DFMT_A8: case D3DFMT_L8: bytes = 1; break;
        default: return false;
    }
    if (bytesPerPixel != nullptr) *bytesPerPixel = bytes;
    return true;
}

void FillTextureResult(NativeD3DTextureResult* result, D3DTextureEntry* entry) {
    result->executionThreadId = GetCurrentThreadId();
    result->lifecycleState = gD3DState;
    result->deviceGeneration = gD3DGeneration;
    if (entry == nullptr) return;
    result->handle = entry->handle;
    result->textureState = entry->state;
    result->width = entry->width;
    result->height = entry->height;
    result->format = entry->format;
    result->levels = entry->levels;
    if (entry->texture == nullptr || entry->state != TextureLive) return;
    D3DSURFACE_DESC desc{};
    const auto hr = entry->texture->GetLevelDesc(result->level, &desc);
    result->operationResult = hr;
    if (SUCCEEDED(hr)) {
        result->levelWidth = desc.Width;
        result->levelHeight = desc.Height;
        result->format = desc.Format;
        if (result->level == 0) { result->width = desc.Width; result->height = desc.Height; }
        else {
            D3DSURFACE_DESC root{};
            if (SUCCEEDED(entry->texture->GetLevelDesc(0, &root))) { result->width = root.Width; result->height = root.Height; }
        }
    }
}
}

// The device the bridge acquired (after a D3D probe), for device hooks.
void* HookCurrentD3DDevice() {
    AcquireSRWLockShared(&gD3DLock);
    void* device = gD3DDevice;
    ReleaseSRWLockShared(&gD3DLock);
    return device;
}

// Acquires and retains exactly one QI reference while initialized. The raw
// OMSI slot is borrowed and is never AddRef'd or released directly.
extern "C" __declspec(dllexport) int __cdecl NativeD3DProbe(NativeD3DStatus* result) {
    if (result == nullptr) return 1;
    *result = {};
    result->slot = gNative.gD3DDevice;
    result->executionThreadId = GetCurrentThreadId();
    if (!Have({ gNative.gD3DDevice })) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    result->lifecycleTransition = ObserveLifecycle(&result->queryInterfaceResult, &result->cooperativeLevelResult, &result->candidate);
    result->ownedDeviceReferences = gD3DDevice == nullptr ? 0 : 1;
    result->lifecycleState = gD3DState;
    result->deviceGeneration = gD3DGeneration;
    result->liveTextureCount = LiveTextureCount();
    result->resetHookInstalled = gD3DOriginalReset != nullptr && gD3DResetSlot != nullptr ? 1 : 0;
    result->lastResetThreadId = gD3DResetThread;
    result->status = gD3DDevice == nullptr ? 5 : 0;
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureCreate(
    unsigned long width, unsigned long height, unsigned long format, unsigned long levels, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {};
    result->operationResult = E_INVALIDARG;
    if (!Have({ gNative.gD3DDevice })) { result->status = 2; return result->status; }
    if (width == 0 || height == 0 || width > 4096 || height > 4096 || levels > 16 || !SupportedTextureFormat(format, nullptr)) {
        result->status = 6; return result->status;
    }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, &result->operationResult, nullptr);
    if (gD3DState != D3DStateReady || gD3DDevice == nullptr) {
        result->status = gD3DState == D3DStateLost ? 8 : (gD3DState == D3DStateResetting ? 9 : 7);
        FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status;
    }
    auto* entry = FindTextureSlot();
    if (entry == nullptr) { result->status = 10; FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status; }
    IDirect3DTexture9* texture = nullptr;
    const auto hr = gD3DDevice->CreateTexture(width, height, levels, D3DUSAGE_DYNAMIC, static_cast<D3DFORMAT>(format), D3DPOOL_DEFAULT, &texture, nullptr);
    result->operationResult = hr;
    if (FAILED(hr) || texture == nullptr) { result->status = 11; FillTextureResult(result, nullptr); ReleaseSRWLockExclusive(&gD3DLock); return result->status; }
    const auto token = ++gD3DNextHandle == 0 ? ++gD3DNextHandle : gD3DNextHandle;
    entry->handle = (static_cast<unsigned __int64>(gD3DGeneration) << 32) | token;
    entry->texture = texture;
    entry->state = TextureLive;
    entry->generation = gD3DGeneration;
    entry->width = width;
    entry->height = height;
    entry->format = format;
    entry->levels = texture->GetLevelCount();
    result->level = 0;
    result->status = 0;
    FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureDescribe(
    unsigned __int64 handle, unsigned long level, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle; result->level = level;
    if (!Have({ gNative.gD3DDevice })) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, nullptr, nullptr);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->generation != static_cast<unsigned long>(handle >> 32)) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (entry->state != TextureLive || entry->generation != gD3DGeneration) result->status = 12;
    else if (level >= entry->texture->GetLevelCount()) result->status = 14;
    else { result->status = 0; FillTextureResult(result, entry); }
    if (result->status != 0) FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureUpdate(
    unsigned __int64 handle, unsigned long level, unsigned long x, unsigned long y,
    unsigned long width, unsigned long height, const unsigned char* pixels, unsigned long pixelBytes,
    NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle; result->level = level;
    if (!Have({ gNative.gD3DDevice })) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    ObserveLifecycle(nullptr, nullptr, nullptr);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->state == TextureStale || entry->generation != static_cast<unsigned long>(handle >> 32) || entry->generation != gD3DGeneration) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (gD3DState == D3DStateLost) result->status = 8;
    else if (gD3DState == D3DStateResetting) result->status = 9;
    else if (gD3DState != D3DStateReady) result->status = 7;
    else if (level >= entry->texture->GetLevelCount() || width == 0 || height == 0 || pixels == nullptr) result->status = 14;
    else {
        D3DSURFACE_DESC desc{};
        auto hr = entry->texture->GetLevelDesc(level, &desc);
        unsigned long bytesPerPixel = 0;
        if (FAILED(hr) || !SupportedTextureFormat(desc.Format, &bytesPerPixel) || x > desc.Width || y > desc.Height || width > desc.Width - x || height > desc.Height - y || pixelBytes != width * height * bytesPerPixel) {
            result->status = 14; result->operationResult = FAILED(hr) ? hr : E_INVALIDARG;
        } else {
            RECT rect{ static_cast<LONG>(x), static_cast<LONG>(y), static_cast<LONG>(x + width), static_cast<LONG>(y + height) };
            D3DLOCKED_RECT locked{};
            hr = entry->texture->LockRect(level, &locked, &rect, 0);
            result->operationResult = hr;
            if (FAILED(hr)) result->status = 15;
            else {
                const auto rowBytes = width * bytesPerPixel;
                for (unsigned long row = 0; row < height; ++row)
                    memcpy(static_cast<unsigned char*>(locked.pBits) + row * locked.Pitch, pixels + row * rowBytes, rowBytes);
                hr = entry->texture->UnlockRect(level);
                result->operationResult = hr;
                result->status = FAILED(hr) ? 16 : 0;
            }
        }
    }
    const auto operationResult = result->operationResult;
    FillTextureResult(result, entry);
    result->operationResult = operationResult;
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) int __cdecl NativeD3DTextureRelease(unsigned __int64 handle, NativeD3DTextureResult* result) {
    if (result == nullptr) return 1;
    *result = {}; result->handle = handle;
    if (!Have({ gNative.gD3DDevice })) { result->status = 2; return result->status; }
    AcquireSRWLockExclusive(&gD3DLock);
    auto* entry = FindTexture(handle);
    if (entry == nullptr || entry->generation != static_cast<unsigned long>(handle >> 32)) result->status = 12;
    else if (entry->state == TextureReleased) result->status = 13;
    else if (entry->state == TextureStale) result->status = 12;
    else {
        entry->texture->Release(); entry->texture = nullptr; entry->state = TextureReleased;
        result->operationResult = S_OK; result->status = 0;
    }
    FillTextureResult(result, entry);
    ReleaseSRWLockExclusive(&gD3DLock);
    return result->status;
}

extern "C" __declspec(dllexport) void __cdecl NativeD3DShutdown() {
    AcquireSRWLockExclusive(&gD3DLock);
    gD3DState = D3DStateStopping;
    InvalidateTextures(TextureStale);
    RemoveD3DResetHook();
    ReleaseDevice();
    ++gD3DGeneration;
    gD3DState = D3DStateStopped;
    QueueTransition(D3DTransitionStopped);
    gD3DOwnerThread = GetCurrentThreadId();
    ReleaseSRWLockExclusive(&gD3DLock);
}
