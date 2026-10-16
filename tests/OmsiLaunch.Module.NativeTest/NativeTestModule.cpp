#include <windows.h>
#include <stdio.h>
#include "omsilaunch_module_native_v1.h"

// Native test component: hooks an import of the host executable through the
// bridge, checks that unknown and undeclared targets are refused, reads values
// through get_value (bridge protocol 2), hooks the Direct3D device (Present
// counts frames, Reset stacks on the bridge's own Reset hook) once gameplay is
// ready, and reports everything as module events.
namespace {
const omsilaunch_bridge_v1* gBridge = nullptr;

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
    return iat == 0 ? 0 : 2;
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
