#include <windows.h>
#include <stdio.h>
#include <string.h>
#include "omsilaunch_module_native_v1.h"

// Native test component: hooks an import of the host executable through the
// bridge, checks that unknown and undeclared targets are refused, reads values
// through get_value (bridge protocol 2), hooks the Direct3D device (Present
// counts frames, Reset stacks on the bridge's own Reset hook) once gameplay is
// ready, and reports everything as module events.
namespace {
const omsilaunch_bridge_v1* gBridge = nullptr;
volatile LONG gTicks = 0;

using IsDebuggerPresentFn = BOOL (WINAPI*)(void);
IsDebuggerPresentFn gOriginal = nullptr;
BOOL WINAPI IsDebuggerPresentDetour(void) { return gOriginal(); }

// IDirect3DDevice9::Present and ::Reset without the d3d9 headers.
using PresentFn = long (__stdcall*)(void* device, const void* source, const void* destination, void* window, const void* dirty);
using ResetFn = long (__stdcall*)(void* device, void* parameters);
PresentFn gPresent = nullptr;
ResetFn gReset = nullptr;
volatile LONG gFrames = 0;

long __stdcall PresentDetour(void* device, const void* source, const void* destination, void* window, const void* dirty) {
    const auto frames = InterlockedIncrement(&gFrames);
    if (frames == 120 && gBridge != nullptr) gBridge->emit(gBridge->context, "present.observed", "{\"frames\":\"120\"}");
    return gPresent(device, source, destination, window, dirty);
}

long __stdcall ResetDetour(void* device, void* parameters) { return gReset(device, parameters); }
}

extern "C" __declspec(dllexport) int __cdecl omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1* bridge) {
    if (bridge == nullptr || bridge->size < sizeof(omsilaunch_bridge_v1) || bridge->version != OMSILAUNCH_MODULE_NATIVE_V1) return 1;
    gBridge = bridge;
    void* unused = nullptr;
    const int iat = bridge->request_hook(bridge->context, "iat:kernel32.dll!IsDebuggerPresent", reinterpret_cast<void*>(&IsDebuggerPresentDetour), reinterpret_cast<void**>(&gOriginal));
    const int missing = bridge->request_hook(bridge->context, "iat:kernel32.dll!NoSuchFunctionAnywhere", reinterpret_cast<void*>(&IsDebuggerPresentDetour), &unused);
    // Before the world starts: refused when the manifest does not declare
    // bridge.hooks.d3d9 (11), otherwise no device yet (10).
    const int early = bridge->request_hook(bridge->context, "d3d9:device.Present", reinterpret_cast<void*>(&PresentDetour), &unused);
    // Values: a declared setting, a buffer that is too small (12, with the
    // size needed), an unknown name (2). -1 when the bridge has no get_value.
    char value[64] = {};
    char tiny[1] = {};
    uint32_t length = 0, needed = 0, ignored = 0;
    int setting = -1, tooSmall = -1, unknown = -1;
    if (bridge->size >= OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE && bridge->get_value != nullptr) {
        setting = bridge->get_value(bridge->context, "setting.sample", value, sizeof(value), &length);
        tooSmall = bridge->get_value(bridge->context, "product_version", tiny, sizeof(tiny), &needed);
        unknown = bridge->get_value(bridge->context, "no.such.value", tiny, sizeof(tiny), &ignored);
    }
    char json[384];
    sprintf_s(json, "{\"iat\":\"%d\",\"missing\":\"%d\",\"undeclared\":\"%d\",\"chain\":\"%d\",\"setting\":\"%d\",\"value\":\"%s\",\"small\":\"%d\",\"needed\":\"%u\",\"unknown\":\"%d\"}",
        iat, missing, early, gOriginal != nullptr && IsDebuggerPresentDetour() == IsDebuggerPresent() ? 1 : 0, setting, setting == 0 ? value : "", tooSmall, needed, unknown);
    bridge->emit(bridge->context, "started", json);
    // Bridge protocol 3: the OMSI version, and a catalog operation in process.
    if (bridge->size >= OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION && bridge->get_value != nullptr && bridge->run_operation != nullptr) {
        char omsi[32] = {};
        uint32_t omsiLength = 0;
        const int version = bridge->get_value(bridge->context, "omsi_version", omsi, sizeof(omsi), &omsiLength);
        char answer[512] = {};
        uint32_t answerLength = 0;
        const int run = bridge->run_operation(bridge->context, "time.read", "{}", answer, sizeof(answer), &answerLength);
        char undeclared[512] = {};
        uint32_t undeclaredLength = 0;
        const int refused = bridge->run_operation(bridge->context, "weather.read", nullptr, undeclared, sizeof(undeclared), &undeclaredLength);
        char tinyAnswer[1] = {};
        uint32_t needed2 = 0;
        const int tooSmall2 = bridge->run_operation(bridge->context, "time.read", nullptr, tinyAnswer, sizeof(tinyAnswer), &needed2);
        // The game's device is not observed offline (10), and a module that does
        // not declare bridge.hooks.d3d9 is refused (11).
        int device = -1;
        if (bridge->size >= OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE && bridge->d3d_is_game_device != nullptr) device = bridge->d3d_is_game_device(bridge->context, const_cast<LONG*>(&gTicks));
        char v3[1100];
        // The answers are JSON; they are reported escaped as plain strings.
        sprintf_s(v3, "{\"version\":\"%d\",\"omsi\":\"%s\",\"run\":\"%d\",\"succeeded\":\"%d\",\"refused\":\"%d\",\"declared\":\"%d\",\"small\":\"%d\",\"needed\":\"%u\",\"device\":\"%d\"}",
            version, version == 0 ? omsi : "", run, strstr(answer, "\"succeeded\":true") != nullptr ? 1 : 0, refused,
            strstr(undeclared, "OL_E_MODULE_OPERATION_NOT_DECLARED") != nullptr ? 0 : 1, tooSmall2, needed2, device);
        bridge->emit(bridge->context, "started3", v3);
    }
    return iat == 0 ? 0 : 2;
}

// Bridge protocol 3 optional exports: a tick counter and an echo for the
// managed component.
extern "C" __declspec(dllexport) int __cdecl omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1* bridge, uint32_t elapsedMilliseconds) {
    const auto ticks = InterlockedIncrement(&gTicks);
    if (ticks == 3 && bridge != nullptr) {
        char json[64];
        sprintf_s(json, "{\"ticks\":\"3\",\"elapsed\":\"%u\"}", elapsedMilliseconds);
        bridge->emit(bridge->context, "ticked", json);
    }
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* argumentsJson, char* buffer, uint32_t capacity, uint32_t* length) {
    char json[256];
    const int written = sprintf_s(json, "{\"op\":\"%s\",\"args\":%s}", operation == nullptr ? "" : operation, argumentsJson == nullptr ? "null" : argumentsJson);
    if (written < 0 || length == nullptr) return 1;
    *length = static_cast<uint32_t>(written);
    if (buffer == nullptr || capacity < static_cast<uint32_t>(written) + 1) return OMSILAUNCH_BRIDGE_BUFFER_TOO_SMALL;
    memcpy(buffer, json, static_cast<size_t>(written) + 1);
    return 0;
}

extern "C" __declspec(dllexport) int __cdecl omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1* bridge) {
    const int present = bridge->request_hook(bridge->context, "d3d9:device.Present", reinterpret_cast<void*>(&PresentDetour), reinterpret_cast<void**>(&gPresent));
    const int reset = bridge->request_hook(bridge->context, "d3d9:device.Reset", reinterpret_cast<void*>(&ResetDetour), reinterpret_cast<void**>(&gReset));
    char json[96];
    sprintf_s(json, "{\"present\":\"%d\",\"reset\":\"%d\"}", present, reset);
    return bridge->emit(bridge->context, "ready", json);
}

extern "C" __declspec(dllexport) void __cdecl omsilaunch_module_native_v1_stop(void) {
    // A hook request outside start and gameplay_ready is refused (11).
    void* unused = nullptr;
    const int late = gBridge->request_hook(gBridge->context, "iat:kernel32.dll!IsDebuggerPresent", reinterpret_cast<void*>(&IsDebuggerPresentDetour), &unused);
    char json[96];
    sprintf_s(json, "{\"late\":\"%d\",\"frames\":\"%ld\"}", late, gFrames);
    gBridge->emit(gBridge->context, "stopped", json);
}
