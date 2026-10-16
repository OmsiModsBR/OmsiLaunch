#pragma once
/*
 * OmsiLaunch Runtime Modules v1: native component ABI (x86, cdecl).
 *
 * A module's native component is a 32-bit DLL named by the manifest
 * (components[].kind = "native"). The bridge loads it when the module is
 * armed and calls omsilaunch_module_native_v1_start on OMSI's UI thread with
 * a bridge table. Hooks are requested by symbolic target name only; the
 * bridge resolves, verifies and installs them, and removes every hook the
 * module owns after omsilaunch_module_native_v1_stop returns. The DLL then
 * stays loaded, inert, until OMSI exits (a thread may still be returning
 * through a detour); after stop the bridge table refuses every call. The
 * module never receives the executable, its identity or any OMSI address.
 *
 * Hook targets (v1):
 *   "iat:<dll>!<function>"   an import of Omsi.exe, for example
 *                            "iat:kernel32.dll!ReadFile"   (capability bridge.hooks.iat)
 *   "d3d9:device.<method>"   a method of the live Direct3D 9 device:
 *                            Reset, Present, BeginScene, EndScene (capability bridge.hooks.d3d9)
 *
 * A detour must call *original to continue the chain. Hooks may only be
 * requested during start and gameplay_ready, for a target whose capability
 * the manifest declares. Return codes: 0 success, 1 argument, 2 target not
 * found, 3 unexpected site content, 4 site hooked by someone else, 5 memory
 * protection, 6 registry full, 7 order, 8 memory, 9 busy, 10 no Direct3D
 * device yet, 11 not allowed now or capability not declared.
 *
 * Bridge protocol 2 appends get_value to the table. A component reads the
 * table's size before using a member a protocol 1 bridge does not have:
 *   if (bridge->size >= OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE) ...
 * get_value copies a value as UTF-8 with a terminating zero and stores in
 * *length the bytes it needs without the zero. Names:
 *   "setting.<key>"     the effective value of a setting the manifest declares
 *   "data_directory"    the module's writable data directory
 *                       (<installation>\.omsilaunch\module-data\<id>)
 *   "module_directory"  the module's own directory
 *   "session_id"        the session id (GUID, "D" format)
 *   "module_version"    the module's manifest version
 *   "product_version"   the OmsiLaunch product version
 *   "session_channel"   the pipe the module's session program serves
 *                       (only for a module with a host component)
 * It returns 0, 1 (argument), 2 (unknown name), 11 (after stop) or 12 (the
 * buffer is too small; *length says how much is needed).
 */
#include <stdint.h>

#ifdef __cplusplus
extern "C" {
#endif

#define OMSILAUNCH_MODULE_NATIVE_V1 1u

typedef struct omsilaunch_bridge_v1 {
    uint32_t size;      /* sizeof(omsilaunch_bridge_v1) */
    uint32_t version;   /* OMSILAUNCH_MODULE_NATIVE_V1 */
    void* context;      /* pass back unchanged */
    /* Installs a hook on a symbolic target; *original receives the function to call. */
    int (__cdecl* request_hook)(void* context, const char* target, void* detour, void** original);
    /* Emits the runtime event module.<id>.<name>; data is a JSON object of strings or null.
     * The event "notice" with "title" and "text" is shown to the user by the OmsiLaunch tray. */
    int (__cdecl* emit)(void* context, const char* name, const char* data_json);
    /* Bridge protocol 2: reads a value (see above). */
    int (__cdecl* get_value)(void* context, const char* name, char* buffer, uint32_t capacity, uint32_t* length);
} omsilaunch_bridge_v1;

#define OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE ((uint32_t)sizeof(omsilaunch_bridge_v1))
#define OMSILAUNCH_BRIDGE_BUFFER_TOO_SMALL 12

/* Exported by the module. Return 0 on success. */
typedef int (__cdecl* omsilaunch_module_native_v1_start_fn)(const omsilaunch_bridge_v1* bridge);
typedef void (__cdecl* omsilaunch_module_native_v1_stop_fn)(void);
/* Optional export: called after gameplay was entered (the Direct3D device
 * exists by then). Return 0 on success. */
typedef int (__cdecl* omsilaunch_module_native_v1_gameplay_ready_fn)(const omsilaunch_bridge_v1* bridge);

#ifdef __cplusplus
}
#endif
