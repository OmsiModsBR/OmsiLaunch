# Runtime Modules

A runtime module adds behavior to a managed OMSI session: a feature that runs with the session (for example a map's HUD, or loading optimizations), without becoming part of OmsiLaunch itself. The bridge loads it, gives it only what it declares, and removes everything it holds when the session ends or the module fails. This page is the normative reference for Runtime Modules as implemented by `src/OmsiLaunch.Runtime.Modules` (manifest, settings, eligibility, lifecycle host), the planner, the controller's session programs (`ModuleSessionPrograms`, `ModuleSessionProcess`), the plugin's module environment and the module flags of the [CLI](cli.md).

Stability: `EXPERIMENTAL`. Bridge protocol 1 was runtime validated in round B (managed modules, content activation, operations, `disable-module` and `fail-session`; native components with IAT hooks; see [runtime validation status](../status/runtime-validation-status.md)). Bridge protocol 2 (settings, default activation, the settings file, session programs, plugin conflicts, notices, `get_value`) is validated offline only. Offline gates: the `module.*` gates of `tests/OmsiLaunch.ModuleTests` (manifest, integrity, selection and activation, settings, the settings file, plugin conflicts, session programs started through their job, a failure injected in every phase under every failure policy, native component lifecycle, cleanup, handoff), `plugin-runtime.modules-lifecycle` (a real managed module loaded by the plugin's environment), `runtime-modules.native-component` (a real native component hooking an import and reading values through the bridge), `runtime-modules.plugin-conflict` and `native-hooks.self-check` (the hook engine). Bridge protocol 3 (Beta 5: catalog operations in process, the tick, input capture, community content inputs, optional pins with the plan-time snapshot, `module pack`, auxiliary job options, the OMSI version) is validated offline only: the `module.*` gates of `tests/OmsiLaunch.ModuleTests` (`manifest.protocol-3`, `manifest.optional-pins`, `catalog.snapshot`, `content.realmap-package`, `packer.pins`, `handoff.v8-snapshot`, `lifecycle.runtime-operations`, `lifecycle.tick.*`, `lifecycle.input-capture`, `lifecycle.auxiliary-options`, `lifecycle.snapshot-before-components`), `runtime-modules.protocol-3`, `runtime-modules.auxiliary-job`, `ui-pause.fails-closed` and `ui-input.hook-self-check` of the plugin integration tests, and `omsi-version.literal` of the unit tests. The pause menu take-over and the input capture have not been observed in a real OMSI session.

Loading is open: any module installed below `plugins\OmsiLaunch.Modules` can be loaded, whoever made it. OmsiLaunch does not vouch for what a module does inside OMSI. What it guarantees is the [boundary](#security-boundary): modules never run inside the OmsiLaunch process, and nothing OmsiLaunch offers lets a module reach beyond OMSI's own rights.

## Where modules live

Each module is a directory below the permanent plugin folder:

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

The directory name must equal the module `id`. Modules are installed by whoever distributes them; a session never writes, snapshots or restores files there, and modules never change installation files through OmsiLaunch (a manifest has no way to declare an installation overlay). A module's writable data lives in its [data directory](#data-directory).

## Manifest (`module.json`)

Schema `omsilaunch.runtime-module/v1`. Unknown keys are rejected, so a manifest written for a later schema is refused instead of being half understood. All paths are relative to the module directory (bound content paths are relative to the OMSI root), use `\` or `/`, and may not contain `..` or a drive.

| Key | Required | Meaning |
| --- | --- | --- |
| `schema` | yes | `omsilaunch.runtime-module/v1`. |
| `id` | yes | Module id: lowercase letters, digits and `-`, 2 to 48 characters, starting with a letter or digit. Also its directory name and its operation namespace. |
| `version` | yes | `major.minor.patch`, optionally `-suffix`. |
| `name` | no | Display name (at most 128 characters). |
| `bridge_protocol` | yes | `{ "min": n, "max": n }`: the bridge protocols the module works with. This build serves protocols `1`, `2` and `3`; a module is accepted when its range includes one of them. |
| `activation` | no | `requested` (default): loaded when the session asks for it. `content`: also loaded when the session runs the bound map (needs `content_binding`). `default` (protocol 2): loaded in every session unless it is disabled (see [choosing modules](#choosing-modules-for-a-session)); it cannot have a `content_binding`. |
| `required_capabilities` | no | Capabilities the module cannot work without (see [capabilities](#capabilities)). |
| `optional_capabilities` | no | Capabilities the module uses when available; it checks them with `IsCapabilityAvailable`. |
| `content_binding` | no | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: the module only runs on that map, with those files unchanged. |
| `assets` | no | Files the module reads, each `{ "path", "sha256" }`. From protocol 3 the `sha256` is optional (see [integrity](#integrity)). |
| `components` | yes | Exactly one `managed` component `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`; optionally one `native` component (`.dll`), `auxiliary` components (`.exe`) and, with protocol 2, one `host` component (`.exe`, the module's [session program](#session-programs)). From protocol 3 the `sha256` of a component is optional, and an `auxiliary` component may carry `priority` (`normal`, `below-normal` or `idle`) and `memory_limit_mb` (64 to 65536): the job object of the program applies them from its first instruction (see [auxiliary programs](#auxiliary-programs)). |
| `failure_policy` | yes | `fail-session`, `disable-module` or `degrade` (see [failures](#failures)). |
| `operations` | no | Operations the module exposes while the session runs: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`. |
| `settings` | no | Protocol 2. The settings the module accepts (see [settings](#settings)). |
| `conflicts` | no | Protocol 2. `{ "plugins": [ "<file>.dll" ] }`: OMSI plugin DLLs that must not run next to the module (see [conflicts](#conflicts-with-omsi-plugins)). |
| `runtime_operations` | no | Protocol 3. Operations of the public capability catalog the module runs in process, for example `["time.read", "camera.set"]` (see [operations in process](#operations-in-process)). Needs the `bridge.runtime` capability. |
| `tick_ms` | no | Protocol 3. The module wants a periodic callback every `tick_ms` milliseconds: a multiple of 50, 50 to 1000 (see [tick](#tick)). Needs the `bridge.tick` capability. |
| `content_inputs` | no | Protocol 3. Community content the module reads, located by the bridge: `[ { "kind": "realmap-package", "required": false } ]` (see [community content](#community-content)). |
| `timeouts` | no | Budgets in milliseconds: `arm_ms`, `gameplay_ready_ms`, `stop_ms` (default 5000 each, at most 30000); with protocol 2 also `prepare_ms` (default 120000, at most 1800000) and `host_stop_ms` (default 10000, at most 60000) for the session program. Operations default to 2000 ms. |
| `diagnostics` | no | `{ "trace": true\|false }`. |

A manifest that uses a protocol 2 feature (`settings`, `conflicts`, activation `default`, a `host` component, `prepare_ms` or `host_stop_ms`) must declare `bridge_protocol.min` 2. A manifest that uses a protocol 3 feature (`runtime_operations`, `tick_ms`, `content_inputs`, a `bridge.runtime`, `bridge.tick` or `bridge.input` capability, an auxiliary `priority` or `memory_limit_mb`, or a component or asset without a `sha256`) must declare `bridge_protocol.min` 3.

Every file named by `components` and `assets` may be pinned by its SHA-256 (required before protocol 3, optional from it: see [integrity](#integrity)). `module.json` cannot pin itself.

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

## Integrity

Every file the manifest names must exist and must not be reached through a junction or symbolic link, and every file the author pinned must have its pinned SHA-256; otherwise the module is refused with `OL_E_MODULE_INTEGRITY`. This catches a module that is corrupted or half updated. It is not a trust decision: a module is not checked against any list, signature or publisher.

From protocol 3 a pin is optional, so an author never has to maintain hashes by hand, and the plan always takes a **snapshot**: one SHA-256 over every file the manifest names and its content, recorded in the plan (`PlannedModule.SnapshotSha256`) and carried to the plugin in the startup handoff (wire version 8). The snapshot is what guarantees that the files the plan evaluated are the files that run: the plugin loads a module only when its `module.json` has the SHA-256 the plan evaluated and its files still have the snapshot's content, and the controller and the bridge compare the snapshot again right before a session program, a native component or an auxiliary program starts. A module whose files changed after planning is not loaded (`OL_E_MODULE_INTEGRITY`, "the module's files changed after planning"). The snapshot covers every module, pinned or not; a pin additionally lets whoever publishes a module detect a corrupted or half-updated copy as soon as it is discovered.

The check is repeated where the module's code starts: the plugin loads a module only if its `module.json` still has the SHA-256 the plan evaluated and every pin and the snapshot still hold, and the controller starts a session program only after verifying that program's pin and the snapshot.

### Packing a module

`OmsiLaunch.exe /pack-module:<module directory>` writes the pins for the files a `module.json` names (its components and assets), so an author never edits a SHA-256 by hand. It rewrites only the `sha256` values (the rest of the manifest is kept; the file is re-indented), re-reads the result with the manifest parser before writing anything, and reports each file (`module.pack` envelope: `module`, `directory`, `written`, `intact`, `files` with `path`, `sha256`, `pinned`, `mismatch`, `written`). It works on the directory it is given and never touches an installation. `/verify-module:<module directory>` reports the same without writing (`module.verify`); it exits `7` when a pinned file does not match its pin (a file without a pin is not a mismatch). Both exit `2` for a manifest that does not parse or a file that is missing.

## Security boundary

OmsiLaunch does not control what a module does inside OMSI: its managed and native components run in the OMSI process with the same rights as any OMSI plugin. OmsiLaunch guarantees the following:

1. The OmsiLaunch process never loads module code. Module code runs inside OMSI, in an auxiliary program started from inside OMSI, or in the module's session program.
2. The programs OmsiLaunch starts for modules are files of the module's own directory, verified against their pin, started with a command line OmsiLaunch fixes. A session program runs in a job object that ends it with the session, allows no second process (it cannot start other programs) and refuses changes to system parameters, display settings, the desktop and logoff or shutdown. It inherits only its two pipe handles.
3. Everything a module declares stays inside its directory, and its writable data inside `.omsilaunch\module-data\<id>`.
4. The OmsiLaunch channels (local control, startup handoff, telemetry, runtime commands) stay restricted to the current user and bound to the session; none of them lets a caller start a program, read or write files outside the installation, or change system settings.
5. OmsiLaunch never elevates. A module runs with the rights of the user who started the session.
6. Text that comes from a module (notices, events, values) is treated as data: bounded, without control characters, never run, opened or followed as a link.

## Capabilities

`required_capabilities` and `optional_capabilities` name either a native capability of the running build (the native capability column of the [capability catalog](capabilities.md#capability-catalog), for example `time.read` or `d3d.texture`) or a bridge feature:

| Bridge feature | What the module gets |
| --- | --- |
| `bridge.events` | `Emit`: runtime events `module.<id>.<name>`. |
| `bridge.background-threads` | `RunBackground`: tracked background threads. |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: auxiliary processes bound to the session. |
| `bridge.hooks.iat` | Native component: hooks on imports of `Omsi.exe` (`iat:<dll>!<function>`). |
| `bridge.hooks.d3d9` | Native component: hooks on methods of the live Direct3D 9 device (`d3d9:device.<method>`). Needs the native capability `d3d.texture`. |
| `bridge.runtime` | Protocol 3. `IRuntimeModuleContext.Runtime` and the native `run_operation`: the catalog operations the manifest lists in `runtime_operations`. |
| `bridge.tick` | Protocol 3. A periodic callback: `ITickingModule.Tick` and the native `omsilaunch_module_native_v1_tick` export. |
| `bridge.input` | Protocol 3. `IRuntimeModuleContext.Input`: capture of the game's keyboard and mouse messages for the module's own menu. Needs the native capability `ui.input.capture`. |

A name that is neither is refused at planning (`OL_E_MODULE_CAPABILITY_UNRESOLVED`). On an OMSI 2 build without a known layout, a native capability the executable file does not resolve is decided in process; if it is not resolved there, the module is not loaded.

## Choosing modules for a session

A module joins a session in one of four ways; `PlannedModule.Activation` says which:

| Activation | When |
| --- | --- |
| `requested` | The session asks for it: `LaunchSpec.Modules.Enable`, `/module:<id>`, or `enabled: true` in the selected session-profile preset. |
| `settings-file` | `enabled=true` in its section of the [settings file](#the-settings-file). |
| `content` | Its activation is `content` and the session runs its bound map. |
| `default` | Its activation is `default`. |

A module is kept out of the session by `LaunchSpec.Modules.Disable`, `/no-module:<id>`, `enabled: false` in the preset, or `enabled=false` in the settings file (plan diagnostic `module.disabled`). An explicit request wins over the settings file. Enabling and disabling the same module in one request is `OL_E_MODULE_SETTING_INVALID`.

Planning rules: a requested module that cannot be loaded makes the plan not runnable with its `OL_E_MODULE_*` code. A module that joined by itself (settings file, content, default) and cannot be loaded does not stop the session: the plan reports `OL_W_MODULE_NOT_LOADED`.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

## Settings

A module declares its settings in `settings`; each entry:

| Key | Required | Meaning |
| --- | --- | --- |
| `key` | yes | `[a-z][a-z0-9_]{0,31}`, not `enabled`. |
| `type` | yes | `boolean`, `integer`, `choice` or `text`. |
| `default` | yes | The value when nothing else sets it: a boolean, an integer, a string, or `"auto"` when `auto` is true. |
| `auto` | no | `true`: the value `auto` is accepted and passed to the module, which decides at run time. |
| `min`, `max` | no | Integer bounds. |
| `choices` | for `choice` | The accepted values (`[a-z0-9][a-z0-9_.-]{0,31}`, not `auto`). |
| `max_length` | no | Text length limit (1..4096, default 256). Control characters are refused. |
| `flag` | no | A command-line flag for this setting, `[a-z][a-z0-9-]{2,31}`: `/<flag>:<value>`, or `/<flag>` alone for `true` on a boolean. |
| `description` | no | At most 256 characters. |

Values are normalized: booleans accept `true`/`false`, `yes`/`no`, `on`/`off`, `1`/`0`; choices are matched without case; integers are checked against their bounds.

The effective value of each setting is, in order:

1. the request: `LaunchSpec.Modules.Settings` (`"<module-id>.<key>": "<value>"`), `/module-setting:<module-id>.<key>=<value>`, a module's own flag, or `modules.<id>.settings` in the selected session-profile preset. As for every other preset field, the command line cannot also set a key the preset sets (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`);
2. the [settings file](#the-settings-file);
3. the manifest default.

An invalid requested value, or a requested key the module does not declare, makes the plan not runnable (`OL_E_MODULE_SETTING_INVALID`). A settings-file entry that cannot be used is ignored with `OL_W_MODULE_SETTING_IGNORED`, as are requested settings for a module the session does not load. The plan lists every effective value with its source (`request`, `settings-file` or `default`) in `PlannedModule.Settings`.

The module receives every declared key with its effective value: `IRuntimeModuleContext.Settings`, `get_value("setting.<key>")` in a native component, and the `settings` object of a session program's context.

### The settings file

`<OMSI root>\.omsilaunch\modules.ini`, next to the session profiles. OmsiLaunch only reads it.

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

Sections are module ids; `key=value` lines set settings, `enabled` switches the module on or off for every session; lines starting with `;` or `#` are comments. The file may be at most 256 KiB. Anything it cannot use (a section that is not a module id, a line outside a section, an unknown key, an invalid value) is reported with `OL_W_MODULE_SETTING_IGNORED` and ignored.

## Session programs

A module may declare one `host` component: its session program, an executable from its own directory. When the module is in the plan, the controller:

1. verifies the program's pin and creates the module's data directory;
2. starts it before OMSI, from the module directory, with the command line `"<program>" --omsilaunch-session <session id>`, suspended until it is in its job (see the [security boundary](#security-boundary));
3. writes the context as the first line on its standard input;
4. waits until the program writes `{"type":"ready"}`, at most `prepare_ms`; only then does it start OMSI;
5. after OMSI was created, writes `{"type":"omsi-started","pid":<process id>}`;
6. at the end of the session, after OMSI has exited and before any file is restored, writes `{"type":"stop"}`, waits at most `host_stop_ms`, then ends the program.

Both directions use UTF-8 JSON, one object per line. The context:

| Field | Meaning |
| --- | --- |
| `type` | `context`. |
| `schema` | `omsilaunch.module-session/v1`. |
| `session_id`, `module_id`, `module_version`, `product_version` | Identity. |
| `installation_root`, `module_directory`, `data_directory` | Full paths. |
| `channel` | The named pipe the program may serve for the module's components in OMSI: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`. The program creates it, restricted to the current user, and checks that the client is OMSI's process. Components read it with `get_value("session_channel")` or `IRuntimeModuleContext.SessionChannelName`. |
| `settings` | The module's effective settings. |
| `world` | `mode`, `map`, `situation`, `entrypoint_index`, `entrypoint` of the session. |
| `session_files` | Installation-relative files the session transaction owns. The program must not keep them open or mapped: they are restored when the session ends. |

What the program may write besides `ready`:

| Line | Effect |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | Runtime event `module.<id>.<name>`. |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | Runtime event `module.<id>.notice` (see [notices](#notices)). |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | Session diagnostic with that catalogued code (`OL_E_MODULE_OPERATION_FAILED` otherwise). |

Other lines, lines longer than 64 KiB and anything after 10,000 lines are ignored. The controller records `module.<id>.host.started`, `host.ready` (with `prepare_ms`), `host.failed`, `host.exited` and `host.killed`.

A program that is not ready within `prepare_ms`, exits before it is ready, or whose pin does not hold, fails its module's preparation: under `fail-session` the session does not start (`OL_E_MODULE_SESSION_FAILED`); under `disable-module` the program is ended and the module is left out of OMSI; under `degrade` the module stays (and the program keeps running if it still can). The session's `StartupTimeoutSeconds` counts from when OMSI is started, so preparation does not consume it.

## Conflicts with OMSI plugins

`conflicts.plugins` names OMSI plugin DLLs that must not run next to the module, for example an older plugin that installs the same hooks. At planning, OmsiLaunch reads the `[dll]` section of every `plugins\*.opl`; if one loads a conflicting DLL, the module is refused with `OL_E_MODULE_CONFLICT` and the reason names the `.opl`. Inside OMSI, a module whose conflicting DLL is loaded anyway is not loaded. OmsiLaunch never changes another plugin's files; switching it off is the user's decision.

## Notices

A module event named `notice` (from `Emit("notice", ...)`, a native component's `emit`, or a session program's notice line) is shown by the OmsiLaunch tray as a balloon whose title names the module. Only `title` (at most 63 characters) and `text` (at most 255 characters) are kept, control characters are removed, and nothing in it is opened or run. A session program that must inform the user before OMSI starts (when the tray is not shown yet) uses its own window.

## Data directory

`<OMSI root>\.omsilaunch\module-data\<id>\` is the module's writable directory for this installation: `IRuntimeModuleContext.DataDirectory`, `get_value("data_directory")`, and the session program's `data_directory`. It is created on first use. It is not part of the session transaction: what a module writes there stays after the session. `/clear-module-data:<id>` deletes it (and nothing else); it is refused while a session owns the installation, and a directory reached through a link is removed as a link, never followed.

## Lifecycle

| Step | Where | What happens |
| --- | --- | --- |
| 1. Discovery | controller | `plugins\OmsiLaunch.Modules\*` is read: manifest and integrity (`ListRuntimeModulesAsync`, `/list:RuntimeModules`). |
| 2. Eligibility | controller, at planning | The modules that [join the session](#choosing-modules-for-a-session) are evaluated: usable, bridge protocol, capabilities, content binding, plugin conflicts, settings. The result is `SessionPlan.PlannedModules`. |
| 3. Prepare | controller, at start | The modules' [session programs](#session-programs) are started and awaited. Then the eligible modules travel to the plugin in the startup handoff (version 6): their directories, the SHA-256 of each evaluated `module.json`, and their effective settings. |
| 4. Arm | plugin, before the world starts | Each module is re-verified and loaded (managed component in its own load context). Its native component, if any, is loaded and started first, then `Arm(context)` is called. |
| 5. Gameplay ready | plugin | After `gameplay.entered`, when a loaded module declares `bridge.hooks.d3d9`, the bridge starts observing the Direct3D device (`d3d.ready`); then it calls the native component's `gameplay_ready` and `GameplayReady()`. For a NEW_MAP session the bound map is checked again against the map the start loaded. |
| 6. Runtime | plugin | Operations `module.<id>.<operation>` reach `Execute`. A module with `tick_ms` is ticked (see [tick](#tick)); a module with `runtime_operations` runs them in process (see [operations in process](#operations-in-process)). |
| 7. Stop | plugin, then controller | When the session ends (or the module fails), `Stop()` is called. After OMSI has exited, the session programs are stopped. |
| 8. Cleanup | plugin | Everything the module tracked is released in reverse order and verified gone; the native component is stopped last and its hooks are removed; its DLL stays loaded, inert, until OMSI exits (a thread may still be returning through a detour). |

Every in-process callback runs on OMSI's UI thread, like the product's own runtime operations. The bridge measures each callback against its budget: a callback that returns late fails its phase (`OL_E_MODULE_PHASE_TIMEOUT`). The bridge cannot interrupt a callback that never returns; such a module stops OMSI, and the session ends at its startup timeout or by `session stop` like any other session.

## Failures

A failure is an exception or an overrun in any phase or operation, a capability unresolved in process, a content binding that does not hold in process, a conflicting plugin loaded in OMSI, or a session program that does not become ready. The module's `failure_policy` decides what follows. In every case the module's resources are released. A tick that throws, returns non-zero or overruns its interval is a failure like an operation's.

| Policy | Failure in prepare, load, arm or gameplay-ready | Failure in an operation | Failure in stop |
| --- | --- | --- | --- |
| `fail-session` | Module released; `module.session-failure`; the session fails with `OL_E_MODULE_SESSION_FAILED`, OMSI is stopped and files are restored. A failure before the world starts prevents the start. | Same. | Reported (`module.failed`); the session continues to its end. |
| `disable-module` | Module released and disabled; the session continues. | Same. | Reported; module released. |
| `degrade` | As `disable-module`, except that a session program's failure leaves the module in the session. | The failing call returns its code; the module stays active; `module.degraded` is reported once per code. | Reported; module released. |

A disabled or failed module answers its operations with `OL_E_MODULE_DISABLED`. Module failures appear as session diagnostics (`SessionStatus.Diagnostics`) with their `OL_E_MODULE_*` code and as runtime events.

## Cleanup

What a module acquires through its context is tracked: resources registered with `Track`, threads started with `RunBackground`, processes started with `StartAuxiliaryProcess`. At stop or failure the bridge cancels `Stopping`, then releases them in reverse order. Auxiliary processes are also placed in a kill-on-close job object, so they end with OMSI even if OMSI is killed; a session program's job ends it with the OmsiLaunch session, even if OmsiLaunch itself is killed. A resource that is still alive after release (a process that did not exit, a thread that did not end) is reported as `OL_E_MODULE_CLEANUP_INCOMPLETE`. Threads or processes a module creates without its context are not tracked.

After a crash there is nothing to recover for modules: they hold no installation files, and their memory, threads and job-bound processes end with the OMSI process or the OmsiLaunch process.

## Native components and hooks

A native component is a 32-bit DLL implementing the C interface in `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h`:

| Export | Called |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | At arm, before the managed `Arm`. Non-zero fails the phase. |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | Optional; after gameplay was entered. |
| `void omsilaunch_module_native_v1_stop(void)` | At stop or failure, before its hooks are removed. Afterwards the bridge table refuses every call and the DLL stays loaded, inert, until OMSI exits. |

The bridge table gives the component `request_hook`, `emit` (runtime events `module.<id>.<name>` with a JSON object of strings) and, from protocol 2, `get_value`. A component reads the table's `size` before using `get_value` (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`). `get_value(context, name, buffer, capacity, length)` copies a value as UTF-8 with a terminating zero and sets `*length` to the bytes needed without it; it returns `0`, `1` (argument), `2` (unknown name), `11` (after stop) or `12` (buffer too small). From protocol 3 the table also holds `run_operation` (see [operations in process](#operations-in-process)) and `d3d_is_game_device(context, device)`: whether a device pointer a Direct3D hook received is the game's own device (`1`), another one (`0`; the vehicle selection form creates a second device with the same method table, so a `Present` detour is called for both and should draw only for the game's), `10` when no device is observed yet, `11` when `bridge.hooks.d3d9` is not declared or after stop. The pointer is only compared, never used, and the bridge never hands one out. A module that keeps Direct3D resources in the default pool hooks `d3d9:device.Reset` itself (what it does before calling the original is "device lost", after it returns success is "device restored"). A component checks the table's `size` before using a protocol 3 member (`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`, `OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`). Two optional exports complete the component: `int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)` (see [tick](#tick)) and `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`, the way the managed component calls the native one through `IRuntimeModuleContext.NativeComponent.Execute` (JSON in, JSON out, the same buffer convention as `run_operation`). The names `get_value` answers:

| Name | Value |
| --- | --- |
| `setting.<key>` | The effective value of a declared setting. |
| `data_directory` | The module's [data directory](#data-directory). |
| `module_directory` | The module's own directory. |
| `session_id` | The session id. |
| `module_version` | The manifest version. |
| `product_version` | The OmsiLaunch product version. |
| `session_channel` | The [session program's](#session-programs) pipe; unknown for a module without one. |
| `omsi_version` | The OMSI product version the game reports about itself (protocol 3; see [the OMSI version](#the-omsi-version)). |

Hooks are requested by symbolic target only, during `start` or `gameplay_ready`, for a target whose capability the manifest declares:

| Target | Hooks | Capability |
| --- | --- | --- |
| `iat:<dll>!<function>` | An import of `Omsi.exe`, for example `iat:kernel32.dll!ReadFile`. | `bridge.hooks.iat` |
| `d3d9:device.<method>` | `Reset`, `Present`, `BeginScene` or `EndScene` of the live Direct3D 9 device (request it in `gameplay_ready`, when the device exists). | `bridge.hooks.d3d9` |

`request_hook` returns the function to continue the chain in `*original` (a detour must call it). Its result: `0` success, `1` argument, `2` unknown target, `3` the site does not hold what it should, `4` the site is hooked by someone outside the bridge, `5` memory protection, `6` registry full, `7` order, `8` memory, `9` a thread was executing the bytes to patch, `10` no Direct3D device yet, `11` not allowed now or capability not declared.

Every hook the bridge installs, for the product or a module, is recorded in one registry with its owner (`module:<id>` for a module):

- A hook is installed only over the value the site should hold (the imported function; a function of a module named `d3d9.dll`, the system runtime or a wrapper), or stacked over the registry's own top hook on that site. A site already changed by someone else is refused (`4`), never overwritten.
- Hooks on one site are removed last-in first-out; a hook with another on top of it stays until that one is removed.
- The product's own Direct3D `Reset` hook is in the registry, so module device hooks stack on it.
- At cleanup the bridge removes every hook of the module after `stop` returns. If one cannot be removed (someone else changed the site), cleanup reports `OL_E_MODULE_CLEANUP_INCOMPLETE`. A native component's DLL is never unloaded while OMSI runs, so a detour that is still being returned through stays valid.

The engine also supports hooks on function prologues with a trampoline (other threads are suspended while the bytes change, and the exact expected bytes are checked first), but no prologue target is offered to modules yet: such targets come from the build's target catalog with verified bytes.

## Operations

A module operation is `module.<id>.<operation>`. It exists only for a session whose plan loaded the module (`PlannedModule.Operations`); otherwise `ExecuteRuntimeAsync` answers `OL_E_MODULE_NOT_LOADED` or `OL_E_MODULE_OPERATION_UNKNOWN`. Required arguments are checked, and undeclared arguments are refused (`OL_E_RUNTIME_ARGUMENT_REQUIRED`). A module's typed failure keeps its code when it is a catalogued public code; any other code becomes `OL_E_MODULE_OPERATION_FAILED` with the module's code in `Values["module_code"]`. Result keys that follow the internal naming convention are removed, as for every runtime operation.

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

## Auxiliary programs

A module starts one of its `auxiliary` components (a pinned `.exe` of its own directory) with `StartAuxiliaryProcess`. From protocol 3 the bridge creates the program **suspended**, places it in the session's job object and only then resumes it, so a child it starts at once cannot run outside the job. The job ends the program and everything it started when the module stops or OMSI exits (kill on close) and carries the manifest's `priority` (`below-normal`, `idle`) and `memory_limit_mb` from the program's first instruction. Unlike a [session program](#session-programs), an auxiliary program's job allows child processes (a program that embeds a web view needs them); every one of them is in the job. Before it starts, the program is verified again: its pin when it has one, and always the plan's snapshot.

## Operations in process

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)` (managed) and `run_operation` (native) run an operation of the [public capability catalog](capabilities.md#public-runtime-operations) in process, on OMSI's UI thread, with the same registry, the same typed `OL_E_*` results, the same argument checks and the same mutation policy as the runtime control channel, and without the channel. Only the operations the manifest lists in `runtime_operations` are allowed: any other is answered with `OL_E_MODULE_OPERATION_NOT_DECLARED` before it reaches OMSI, and `internal.*` and `module.*` operations can never be listed. An operation whose native capability the build does not resolve answers `OL_E_CAPABILITY_UNRESOLVED`, as everywhere else.

The list is transparency and planning information, not a security boundary: a module already runs inside OMSI with the rights of the user. The plan shows it (`PlannedModule.RuntimeOperations`), and an operation that changes OMSI's state carries its mutation policy there, for example `camera.set (omsi-state)`. A module can run operations from `Arm` on (the world may not exist yet; the operation then answers its own typed error). Results are bounded (1 MiB); a larger result is `OL_E_RUNTIME_RESPONSE_TOO_LARGE`. After the module stopped, every call answers `OL_E_MODULE_DISABLED`.

The native `run_operation(context, operation, arguments_json, buffer, capacity, length)` takes the catalog id and a JSON object of strings (or null) and writes `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`. It returns `0` when it answered (the operation's own failure is inside the JSON), `1` for an argument, `11` after stop and `12` when the buffer is too small (`*length` is the size needed).

## Tick

A module that declares `tick_ms` and the `bridge.tick` capability is called about every `tick_ms` milliseconds, on OMSI's UI thread, from the plugin's 50 ms UI timer: `ITickingModule.Tick(TimeSpan elapsed)` on the managed component and the optional native export `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`. The interval is a multiple of 50 between 50 and 1000. A tick is not per frame: a module that draws hooks the Direct3D `Present` method and uses the tick to read data. Ticks start after `gameplay.entered` and stop with the module. A tick that throws, returns non-zero (native) or takes longer than its interval is a failure of that module: under `degrade` it is reported once per code (`module.degraded`) and the module keeps running (the ticks it missed are skipped, never queued); under `disable-module` and `fail-session` the policy applies. A nested modal loop of OMSI keeps the UI timer firing, so a tick never re-enters itself.

## Community content

`content_inputs` declares content the module reads that its author cannot know when the module is built, because the community makes it, for a map, after the module. There is no SHA-256 to pin; the bridge only locates it and checks that reading it is safe. The one kind today is `realmap-package`: the `openomsi-realmap` folder of a map, `<OMSI root>\maps\<folder>\openomsi-realmap`, written by the openOMSI map package tooling.

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` answers whether the map has a package (not having one is not an error) and, when it has, its directory, file count, total size and a `Fingerprint`. The fingerprint is a change detector over every file's relative path, size and write time: use it as a cache key; it is never an integrity check or a trust decision. The package must be inside the installation, no part of it may be a reparse point, and it is bounded (50,000 files, 4 GiB); otherwise the answer is `OL_E_MODULE_CONTENT_INPUT_INVALID`. A module that did not declare the kind gets `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`. The package is data made by third parties: read it in managed code, treat every value as untrusted and degrade by resource. A `required: true` input makes the plan refuse the module (`OL_E_MODULE_CONTENT_BINDING`) when the session's map has no package.

## Input capture

A module that replaces OMSI's pause menu with its own needs the game's keyboard and mouse. OMSI reads its simulation keys through DirectInput and its mouse through window messages to the main form's view window; while its own pause is on (the native pause byte), the simulation, the rendering, 3D clicks and DirectInput commands are held by OMSI itself. `IRuntimeModuleContext.Input` (capability `bridge.input`, native capability `ui.input.capture`) adds the rest: `Capture(true)` installs one thread-local `WH_GETMESSAGE` hook on the UI thread, and the keyboard and mouse messages that belong to the game's window tree (the main form, its children and the hidden native pause form) are recorded as plain `ModuleInputEvent` values (kind, virtual key, client coordinates of the view window, button, wheel delta, modifiers) and removed from the queue, so neither OMSI nor the VCL acts on them. `Drain()` returns the events (at most 256 are kept). Nothing is subclassed, no user32 key-state function is hooked and nothing is sent.

One module captures at a time. The capture is released when the module stops or fails, whatever `Stop` did. Native modal dialogs (save, options, new game) need real input: release the capture before asking for one. The hook mechanics are validated offline with a stand-in window (`ui-input.hook-self-check`); their behaviour against the real game window is not yet observed.

## The OMSI version

`IRuntimeModuleContext.OmsiVersion` and `get_value("omsi_version")` give the OMSI product version the game reports about itself, for example `2.3.004`: the text OMSI writes after `Version:` on line 7 of its `logfile.txt`, read from the running image. It is **never** the executable's PE file version (the reference build carries `FileVersion` `2.2.032`). When the image has no single such text, the version of the build profile is used when the code identity is known, otherwise `unknown`. The executable's hash, its code size, addresses and the profile name are not exposed. Use the version for text and for choosing behavior; capabilities (`IsCapabilityAvailable`) remain the source of truth for what works.

## Writing a module (contracts)

The managed component references `OmsiLaunch.Runtime.Modules` and `OmsiLaunch.Api` and implements `IRuntimeModule`. The bridge binds these two references to its own loaded assemblies (whatever load context the plugin runs in), so a copy shipped with the module is ignored:

| Member | Called |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | Step 4. Acquire resources through the context. |
| `void GameplayReady()` | Step 5. |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | Step 6, for each declared operation (without the `module.<id>.` prefix). Throw `ModuleOperationException(code, message)` for a typed failure. |
| `void Stop()` | Step 7. |

`IRuntimeModuleContext` offers `SessionId`, `ModuleId`, `ModuleDirectory`, `Settings`, `DataDirectory`, `ProductVersion`, `OmsiVersion`, `SessionChannelName`, `Stopping` (a cancellation token), `IsCapabilityAvailable`, `Emit`, `Track`, `RunBackground`, `StartAuxiliaryProcess`, and from protocol 3 `Runtime` (see [operations in process](#operations-in-process)), `Content` (see [community content](#community-content)), `Input` (see [input capture](#input-capture)) and `NativeComponent` (the loaded native component, or null). A managed component that wants ticks also implements `ITickingModule`. Nothing in the context exposes addresses, the executable's hash or its code layout. The component must be loadable by the 32-bit plugin (AnyCPU or x86, .NET 6); a copy of `OmsiLaunch.Runtime.Modules` or `OmsiLaunch.Api` next to it is ignored.

## Known limitations

- Native components can hook `Omsi.exe` imports and Direct3D device methods; hooks on OMSI function prologues and call sites are not offered to modules yet.
- The pause menu take-over (`ui.pause.*`) and the input capture are statically designed and validated offline; they have not been observed in a real OMSI session, and `ui.pause-menu.replace` is resolved only on a known code identity (some of its fields have no build-independent derivation).
- The in-process content-binding check uses the map the start requested (NEW_MAP); for a saved situation the binding is checked at planning against the situation's map.
- Device hooks are vtable hooks. Where another component writes the device's original methods back (observed on the regression installation shortly after gameplay starts), a device hook stops being called and cannot be removed; cleanup then reports `OL_E_MODULE_CLEANUP_INCOMPLETE` and the component stays loaded. A device method already hooked by something outside the registry is refused (4); the refusal event names the module that owns the method.
- Modules stop on a normal OMSI exit; a session stop terminates OMSI, so module cleanup inside a session happens only through a failure policy.
- Notices are shown once the tray exists, after OMSI was started; a session program shows its own window for anything it must say before that.
