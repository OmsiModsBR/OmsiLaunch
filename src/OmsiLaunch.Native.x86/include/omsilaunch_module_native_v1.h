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
 *   "omsi_version"      the OMSI product version the game reports about
 *                       itself (for example "2.3.004"), never the executable's
 *                       PE file version; "unknown" when it cannot be determined
 *                       (bridge protocol 3)
 * It returns 0, 1 (argument), 2 (unknown name), 11 (after stop) or 12 (the
 * buffer is too small; *length says how much is needed).
 *
 * Bridge protocol 3 appends run_operation to the table and adds two optional
 * exports. A component checks the table's size before using run_operation:
 *   if (bridge->size >= OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION) ...
 * run_operation runs an operation of the public capability catalog that the
 * manifest lists in runtime_operations (capability bridge.runtime), in
 * process on OMSI's UI thread. operation is the catalog id (for example
 * "time.read"), arguments_json a JSON object of strings or null. The answer is
 * JSON, UTF-8 with a terminating zero:
 *   {"succeeded":true|false,"error_code":null|"OL_E_...","values":{...}}
 * run_operation returns 0 when it answered (the operation's own failure,
 * including OL_E_MODULE_OPERATION_NOT_DECLARED, is inside the JSON), 1
 * (argument), 11 (after stop) or 12 (the buffer is too small; *length says how
 * much is needed).
 *
 * d3d_is_game_device (bridge protocol 3, capability bridge.hooks.d3d9) answers
 * whether a device pointer a Direct3D hook received is the game's own device.
 * The vehicle selection form creates a second Direct3D device with the same
 * method table, so a Present/EndScene detour is called for both: draw only
 * when it returns 1. It returns 1 (the game's device), 0 (another device), 10
 * (the bridge does not observe a device yet), 11 (not declared, or after stop).
 * The pointer is only compared; it is never used, and the bridge never hands
 * one out. A module that keeps Direct3D resources in the default pool also
 * hooks d3d9:device.Reset itself (what it does before calling the original is
 * "device lost", after it returns success is "device restored"); hooks stack
 * on the bridge's own Reset hook. Optional exports of the component:
 *   omsilaunch_module_native_v1_tick      (capability bridge.tick, manifest tick_ms)
 *       called on the UI thread about every tick_ms with the milliseconds since
 *       the previous call; not per frame (draw from the Direct3D Present hook).
 *       A non-zero result fails the tick under the module's failure policy.
 *   omsilaunch_module_native_v1_execute
 *       the managed component's way to call this one: operation and JSON in,
 *       JSON out in the same buffer convention as run_operation.
 */
#include <stddef.h>
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
    /* Bridge protocol 3: runs a declared catalog operation (see above). */
    int (__cdecl* run_operation)(void* context, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length);
    /* Bridge protocol 3: is this Direct3D device pointer the game's own device? (see above) */
    int (__cdecl* d3d_is_game_device)(void* context, const void* device);
} omsilaunch_bridge_v1;

/* Table sizes: the size a bridge of that protocol reports in size. */
#define OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE ((uint32_t)offsetof(omsilaunch_bridge_v1, run_operation))
#define OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION ((uint32_t)offsetof(omsilaunch_bridge_v1, d3d_is_game_device))
#define OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE ((uint32_t)sizeof(omsilaunch_bridge_v1))
#define OMSILAUNCH_BRIDGE_BUFFER_TOO_SMALL 12

/* Exported by the module. Return 0 on success. */
typedef int (__cdecl* omsilaunch_module_native_v1_start_fn)(const omsilaunch_bridge_v1* bridge);
typedef void (__cdecl* omsilaunch_module_native_v1_stop_fn)(void);
/* Optional export: called after gameplay was entered (the Direct3D device
 * exists by then). Return 0 on success. */
typedef int (__cdecl* omsilaunch_module_native_v1_gameplay_ready_fn)(const omsilaunch_bridge_v1* bridge);
/* Optional exports (bridge protocol 3), see the notes at the top of this file. */
typedef int (__cdecl* omsilaunch_module_native_v1_tick_fn)(const omsilaunch_bridge_v1* bridge, uint32_t elapsed_ms);
typedef int (__cdecl* omsilaunch_module_native_v1_execute_fn)(const omsilaunch_bridge_v1* bridge, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length);

#ifdef __cplusplus
}
#endif
