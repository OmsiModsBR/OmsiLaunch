# CLI Reference

This page is the complete, normative reference for the OmsiLaunch `0.1.0-beta4` command line: the three executables, the argument grammar, the dispatch order, every command word, every hierarchical route, every flag, the output envelopes and the error behaviour of each command. It is generated from `tools\OmsiLaunch.Cli\Program.cs` (`CliProgram.RunAsync`, `OwnerSession.RunAsync`, `CliInput.Parse`, `CliInput.KnownFlags`, `CliInput.AcceptedNoEffectFlags`, `CliInput.CommandWordsAccepted`, `CliInput.HierarchicalRoutes`, `CliInput.BuildSpecAsync`, `CliEventWatch`), `tools\OmsiLaunch.Cli\LaunchSpecJson.cs` and the two native shims under `tools\OmsiLaunch.Bootstrapper`. Process results are listed in [exit codes](exit-codes.md); error codes in [errors](errors.md); worked invocations in [CLI examples](cli-examples.md).

## Executables

| File | Subsystem | Role | Differences |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | Native bootstrapper (`OmsiLaunch.Bootstrapper.cpp`): resolves its own directory, tokenizes the command line with `CommandLineToArgvW`, locates `hostfxr` through `nethost.dll`, and runs `OmsiLaunch.Controller.dll` with the same arguments. | Console output is written; the process exit code is the managed controller's exit code, or a shim code `100`..`106` if the .NET host could not be started. |
| `OmsiLaunchW.exe` | Windows (GUI) | Same shim (`OmsiLaunch.WindowsHost.cpp`) built for the Windows subsystem. It sets the environment variable `OMSILAUNCH_WINDOWS_HOST=1` before starting the controller. | No console: console output is suppressed unless `--json` is given (`WindowsHost.SuppressConsole`), failures are shown as message boxes (`WindowsHost.ShowFailure`: message, `Code: OL_E_...`, and the hint `See .omsilaunch\diagnostics for details.`), and a shim failure `100`..`106` is shown as `OmsiLaunch could not start the .NET host (code N).` Complete behaviour: [OmsiLaunchW.exe reference](omsilaunchw.md). |
| `OmsiLaunch.Controller.dll` | Managed (x64, `net6.0-windows`, Windows Forms) | The controller itself. It is never invoked directly by users; both shims pass the controller path as the first host argument so it never appears in the public argument list. | Requires the x64 .NET 6 runtime with `Microsoft.WindowsDesktop.App`; see [installation](../getting-started/installation.md). |

`nethost.dll` must sit beside the shims. The shims read no arguments themselves; every argument reaches `CliInput.Parse` unchanged, so `OmsiLaunch.exe` and `OmsiLaunchW.exe` accept exactly the same syntax.

## Invocation model

### Argument grammar (`CliInput.Parse`)

| Form | Meaning |
|---|---|
| `/key:value`, `/key`, `-key:value`, `-key` | A flag. The key is case-insensitive; the value is everything after the first `:`. Unknown keys fail with `OL_E_INVALID_ARGUMENT` (`Unknown argument: ...`), exit `2`. |
| `--key=value` | A runtime argument for the selected runtime operation (for example `--handle=rv-000001`). Any `--` token containing `=` is a runtime argument, never a flag. |
| `--json`, `/json` | Structured output (see [Output formats](#output-formats)). `--json` is the only `--` token without `=` that is meaningful; it is parsed as the `/json` flag. |
| bare word | If no command word has been seen yet and the word is one of the [command words](#command-words), it becomes the command. Once a command word is present, every later bare word is a command word (the route). Otherwise the first bare word is the installation root and any later bare word is appended to the route. |

Consequences: a hierarchical route (`time get`) cannot be combined with an installation argument placed after it (`time get D:\OMSI` is the unknown route `time get d:\omsi`, exit `2`). `D:\OMSI time get` is accepted but is **owner mode** (a new session is started and the operation runs once inside it). Parsing errors (`ArgumentException`, `FormatException`, `InvalidDataException`, `OverflowException`) and session-profile errors (`SessionProfileException`) are reported before anything runs, always with exit `2`.

### Installation root

- An explicit bare-word installation argument wins over `RootPath` in a `/spec` file (`CliInput.BuildSpecAsync`).
- `.` means the directory that contains the executable (`AppContext.BaseDirectory`), never the caller's working directory (`CliInput.ResolveInstallationRoot`). A portable package relies on this.
- When the argument is omitted, owner-mode operations (`/new`, `/saved`, `/spec`, `/list`, `/recovery-status`, `/recover`) also use the executable's directory. The path is normalised with `Path.GetFullPath`.
- Client-mode commands never take an installation argument: they address the local control endpoint of the installation the executable lives in (`AppContext.BaseDirectory`). See [local control](local-control.md).

### Owner and client

- **Owner**: the process that plans, starts, supervises and restores a session (`OwnerSession.RunAsync`). It holds the installation lease (`Local\OmsiLaunch.Installation.<sha256(root)>`) and the configuration transaction, exposes the local control endpoint while the session is alive, and shows the [tray icon](windows-tray.md). Exactly one owner per installation: if an owner already answers `session.status` on the control endpoint, a second launch fails with `OL_E_SESSION_ALREADY_ACTIVE` (exit `7`).
- **Client**: any invocation with no installation argument that sends `session status`, `session stop`, `events read`, `events watch` or a runtime operation. It is forwarded over the local control pipe; without an owner it fails with `OL_E_NO_ACTIVE_SESSION` (exit `4`).

### Dispatch order (`CliProgram.RunAsync`)

1. `/silent` (when not already running under `OmsiLaunchW.exe`): start `OmsiLaunchW.exe` from the executable's directory through `ShellExecute` (no handle inheritance) with the same arguments minus `/silent`/`--silent`, write the `silent` envelope (`delegated`, `host_process_id`) and return `0`. The console process does not wait for the session; see [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation). `OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` return `7`.
2. `/version`: envelope `version` with `product`, `version` (assembly informational version, stamped from `OmsiLaunch.Version.props`, `0.1.0-beta4`), `protocol_version` (`0.1`), `supported_family` (`OMSI_2_3_004_COMMON`); exit `0`.
3. `capabilities`: envelope with every `PublicStableBeta` or `PublicExperimental` descriptor of the canonical catalog `PublicCapabilityRegistry` (with its operations, native capabilities, evidence and note); exit `0`.
4. `help [family]`: envelope `help` with `usage`, `product_version`, `protocol_version`, `family` and the public `commands` (`CliRoute`, `Description`, `Classification`, `RuntimeValidation`), optionally filtered by family; exit `0`.
5. `profiles`: envelope with `requirement` (`OMSI 2`), the `installation` report of the target root (`is_omsi2`, `reason`, `build_id`, `identity` `known`/`unknown`, `file_sha256`, `file_version`, `code_sha256`, `file_variant`), the `known_layouts`, and the `predicted_capabilities` resolved from the executable file (the in-process resolution at start is authoritative); exit `0`.
6. Client runtime operation (no installation argument and a route or `/runtime:`): arguments validated against `PublicCapabilityRegistry.ValidateRuntimeArguments` (`OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED`, exit `2`), then `runtime.execute` is forwarded with an 8 s timeout (30 s for `road-vehicles.spawn`).
7. Client `session status` (750 ms), `session stop` (bound to the active session id, 750 ms), `events read` (750 ms), `events watch` (polls every 250 ms until Ctrl+C).
8. `detect`, or **no arguments at all** (no installation, no command, no `/?`, no `/spec`, no launch flag, no recovery flag, no `/list`): enumerate `Omsi` processes and probe the control endpoint (250 ms); envelope `detect`; exit `0`.
9. `/?` or `/help`: print the usage text, exit `0`. Any other invocation that has a command word but no dispatchable route (for example `d3d` alone or `session status D:\OMSI`) prints the usage text and exits `2`.
10. Owner mode. Preconditions: `plugins\OmsiLaunch.Plugin.opl` and `plugins\OmsiLaunch.Native.x86.dll` must exist beside the executable (`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, exit `7`). `release-manifest.json` beside the executable, when present, supplies the expected plugin hashes.
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`; envelope `recover` with `pending`, `recovered`, `diagnostics`; exit `8` only when a restore was requested and did not complete, otherwise `0`.
12. `/list:<category>`: `DiscoverAsync`; envelope `content.list`; exit `0`. `/list:SessionProfiles` uses `ListSessionProfilesAsync` instead; envelope `session-profiles.list`.
13. Build the `LaunchSpec` (`BuildSpecAsync`), plan it (`PlanSessionAsync`), print the plan. `/plan` or `/validate`: exit `0` if `IsRunnable`, else `1`. A non-runnable plan never starts OMSI (exit `1`); under `OmsiLaunchW.exe` a launch with a non-runnable plan shows its last `OL_E_` diagnostic in a message box (documentation audit BUG-06). Planning also verifies the installed permanent plugin closure against `release-manifest.json`, so a missing or altered plugin makes the plan not runnable (`OL_E_PERMANENT_PLUGIN_*`).
14. Probe for an existing owner (`OL_E_SESSION_ALREADY_ACTIVE`, exit `7`), then `OwnerSession.RunAsync`.

### Owner lifecycle (`OwnerSession.RunAsync`)

1. `StartSessionAsync(plan)`. From here on every exit path reaches `CloseAsync` in a `finally` block: exceptions, Ctrl+C (`Console.CancelKeyPress`), console close / logoff (`AppDomain.ProcessExit` with a 4 s budget for stop + restore; anything left is recovered by the journal on the next start), tray "End session", pipe `session.stop`, and `/observe-seconds`.
2. The tray icon is created unless `Presentation.SuppressTrayIcon` is set in the spec.
3. Wait for `Running` for `StartupTimeoutSeconds + 5` seconds. The status is printed. If the state is not `Running`, exit `1` (`OmsiLaunchW.exe` shows `The OMSI session did not reach gameplay.` with the last `OL_E_` diagnostic or `OL_E_SESSION_START_FAILED`).
4. Validation batches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`) run and write their artifacts.
5. The local control endpoint starts.
6. `/runtime:<operation>` runs once (5 s, 15 s for `road-vehicles.spawn`); the result is written to `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` and printed. A failing runtime command never terminates the session (`runtime_error` is printed instead).
7. Wait: with `/observe-seconds:n` the session is stopped after `n` seconds **or** earlier on a tray/pipe stop, or when OMSI exits; without it the owner waits until OMSI exits or a stop is requested.
8. The completed status is printed; exit `0` if `Completed`, `1` otherwise.

`session.stop`, tray "End session", Ctrl+C and `CloseAsync` all request the canonical stop: OMSI is terminated with `TerminateProcess` (OMSI's own shutdown routine does not run and `options.cfg` is not rewritten by OMSI), then every session-owned file is restored. See [session lifecycle](../concepts/session-lifecycle.md) and [transactions and recovery](../concepts/transactions-and-recovery.md).

## Command words

Every word accepted in the first position (`CliInput.CommandWordsAccepted`):

| Word | Purpose | Mode | Notes |
|---|---|---|---|
| `capabilities` | List public capabilities | Local, no session | Envelope `capabilities`. |
| `profiles` | List supported `Omsi.exe` variants | Local, no session | Envelope `profiles`. |
| `detect` | Report `Omsi.exe` processes and an active owner | Local, no session | Also the default when no arguments are given. States: `NO_OMSI_FOUND`, `OMSI_FOUND_UNMANAGED`, per-process `UNKNOWN_BINARY_FOUND` when the binary cannot be inspected; `active_omsilaunch_instance`, `managed_session`. |
| `help` | Usage and public command catalogue | Local, no session | `help <family>` filters by capability family (`session`, `time`, `weather`, `map`, `camera`, `vehicles`, `player`, `humans`, `timetable`, `scripts`, `constants`, `curves`, `hof`, `drivers`, `tickets`, `d3d`, `events`). |
| `session` | `session status`, `session stop` | Client | Exactly one following word; anything else prints usage, exit `2`. `session plan`/`session start` are API route names, not CLI words: use `/plan` and `/new`. |
| `events` | `events read`, `events watch` | Client | `read` returns the bounded event list once; `watch` prints each new event (by `Sequence`) as an `events.watch` envelope every 250 ms until Ctrl+C (exit `0`), `4` when no owner answers, `7` on a control error. |
| `time` | `time get`, `time set` | Client route | |
| `weather` | `weather get`, `weather set`, `weather metar set`, `weather actual get` | Client route | |
| `map` | `map get` | Client route | |
| `camera` | `camera get`, `camera set`, `camera lock`, `camera unlock` | Client route | |
| `ui` | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action` | Client route | |
| `vehicles` | `vehicles list`, `vehicles get`, `vehicles summary`, `vehicles spawn`, `vehicles place-random` | Client route | |
| `player` | `player get` | Client route | |
| `humans` | `humans list`, `humans get`, `humans summary` | Client route | |
| `timetable` | `timetable get`, `timetable <table> list`, `timetable logs list` | Client route | |
| `scripts` | `scripts variable list|get|set`, `scripts string list|get|set`, `scripts trigger`, `scripts sound-trigger` | Client route | |
| `constants` | `constants list`, `constants get` | Client route | |
| `curves` | `curves list`, `curves evaluate` | Client route | |
| `hof` | `hof get` | Client route | |
| `drivers` | `drivers list` | Client route | |
| `tickets` | `tickets get` | Client route | |
| `d3d` | Reserved family word | None | `d3d` has **no hierarchical route**: `d3d texture ...` is an unknown route (exit `2`) and `d3d` alone prints usage (exit `2`). D3D operations are reached with `/runtime:d3d.status`, `/runtime:d3d.texture.create` and so on (see [Operations without a route](#operations-without-a-route)). |

## Hierarchical routes

`CliInput.HierarchicalRoutes` maps a lower-cased route to a runtime operation id. All routes require a `Running` session and are executed through the runtime mailbox (`ExecuteRuntimeAsync`). Runtime writes change OMSI's in-memory state only: they never touch files, they are not part of the configuration transaction and they are **not** reverted at stop (OMSI is terminated). Stability follows `PublicCapabilityRegistry` and the [validation matrix](../status/runtime-validation-status.md); details and result fields are in [runtime control](runtime-control.md).

| Route | Runtime operation | Kind | Requires Running | Mutates OMSI | Restore participation | Stability | Notes |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | Yes | No | None | STABLE_BETA | Clock and calendar fields. |
| `time set` | `time.set` | Write | Yes | Yes (in-memory clock) | None, not reverted | EXPERIMENTAL | For example `--minute=<0..59>`; write, read-back and restore validated 2026-09-20. |
| `weather get` | `weather.read` | Read | Yes | No | None | STABLE_BETA | |
| `weather set` | `weather.set` | Write | Yes | Yes (in-memory weather) | None, not reverted | EXPERIMENTAL | For example `--wind_speed=5 --cloud_type="Cumulus 1"`. Manual weather: live METAR updates stop. Runtime validation pending. |
| `weather metar set` | `weather.metar.set` | Write | Yes | Yes (in-memory weather; OMSI saves the ICAO in `options.cfg`, which the session restores) | None, not reverted | EXPERIMENTAL | `--icao=<ICAO>`. Live METAR weather, as OMSI's weather dialog sets it. Runtime validation pending. |
| `weather actual get` | `weather.actual.read` | Read | Yes | No | None | EXPERIMENTAL | Actual/ICAO controller state. |
| `map get` | `map.read` | Read | Yes | No | None | STABLE_BETA | Map name, file, description, tile count, year range and traffic side; runtime-revalidated on the corrected map slot. |
| `camera get` | `camera.read` | Read | Yes | No | None | STABLE_BETA | |
| `camera set` | `camera.set` | Write | Yes | Yes (camera scalars, e.g. `--field_of_view=`) | None, not reverted | EXPERIMENTAL | FOV write/read-back validated. |
| `camera lock` | `camera.lock` | Action | Yes | Yes (session-scoped policy) | None | EXPERIMENTAL | Requires `--family=<0..3>` (driver=0, passenger=1, external=2, map=3), optional `--preset=<n>` (family 0 or 1). Needs a player vehicle (for example a saved situation). Runtime-validated in the runtime closure (`CAM01`); the registry's `RuntimeValidation` string still says `STATICALLY_VALIDATED` (see [capabilities](capabilities.md)). |
| `camera unlock` | `camera.unlock` | Action | Yes | Yes | None | EXPERIMENTAL | Releases the policy set by `camera lock` (`CAM01`). |
| `ui pause-state` | `ui.pause.state` | Read | Yes | No | None | EXPERIMENTAL | OMSI's pause byte and the native pause menu's state. |
| `ui pause-take-over` | `ui.pause.take-over` | Action | Yes | Yes (hides the native pause menu through the VCL; OMSI stays paused) | None, not reverted | EXPERIMENTAL | Needs OMSI paused with the menu shown. Not observed in a real session yet. |
| `ui pause-restore` | `ui.pause.restore-native` | Action | Yes | Yes (shows the native pause menu again) | None | EXPERIMENTAL | Gives the native menu back. |
| `ui pause-action` | `ui.pause.action` | Action | Yes | Yes (the native menu's own action) | None, not reverted | EXPERIMENTAL | Requires `--action=<resume|new-game|save|options|quit>`; runs after the answer, read the outcome from `ui pause-state`. |
| `vehicles list` | `road-vehicles.list` | Read | Yes | No | None | STABLE_BETA | Returns session-scoped `rv-NNNNNN` handles. |
| `vehicles get` | `road-vehicle.read` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`. Stale handle: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. |
| `vehicles summary` | `road-vehicles.read` | Read | Yes | No | None | STABLE_BETA | Counts and player state, no handles. |
| `vehicles spawn` | `road-vehicles.spawn` | Action | Yes | Yes (adds one RoadVehicle) | None, not removed | EXPERIMENTAL | Requires `--model=Vehicles\...\*.bus`. 30 s client timeout, 15 s owner timeout. Does not assign the player vehicle. RV-003 `RUNTIME_VALIDATED`. |
| `vehicles place-random` | `road-vehicles.place-random` | Action | Yes | Yes | None | EXPERIMENTAL | Profiled `PlaceRandomBus`. |
| `player get` | `player-vehicle.read` | Read | Yes | No | None | STABLE_BETA | Semantic null when there is no player vehicle. |
| `humans list` | `humans.list` | Read | Yes | No | None | EXPERIMENTAL | Returns `hb-NNNNNN` handles. |
| `humans get` | `human.read` | Read | Yes | No | None | EXPERIMENTAL | Requires `--handle=`. |
| `humans summary` | `humans.read` | Read | Yes | No | None | EXPERIMENTAL | Counts only. |
| `timetable get` | `timetable.read` | Read | Yes | No | None | STABLE_BETA | Timetable manager state. |
| `timetable tracks list` | `timetable.tracks.list` | Read | Yes | No | None | STABLE_BETA | Part of the `timetable.read` capability; batch-read evidence 2026-09-20. |
| `timetable trips list` | `timetable.trips.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable lines list` | `timetable.lines.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable tours list` | `timetable.tours.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable profiles list` | `timetable.profiles.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable station-links list` | `timetable.station-links.list` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable logs list` | `timetable.logs.read` | Read | Yes | No | None | STABLE_BETA | As above. |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | Yes | No | None | EXPERIMENTAL | Vehicle numbers OMSI keeps out of free AI use (reserved by timetable tours or fixed AI lists). Paged (`cursor`, `limit`, at most 256 per page). |
| `map tiles list` | `map.tiles.list` | Read | Yes | No | None | EXPERIMENTAL | The map's tiles: grid position, own index, file and load state, counts of objects, paths and splines. Paged (`cursor`, `limit`, at most 256 per page). |
| `drivers list` | `drivers.read` | Read | Yes | No | None | EXPERIMENTAL | Driver records. |
| `tickets get` | `tickets.read` | Read | Yes | No | None | EXPERIMENTAL | Ticket-pack records. |
| `hof get` | `vehicle.hofs.read` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`. |
| `constants list` | `vehicle.constants.list` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`. |
| `constants get` | `vehicle.constant.get` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`, `--name=`. |
| `curves list` | `vehicle.curves.list` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`. |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | Yes | No | None | STABLE_BETA | Requires `--handle=`, `--name=`, `--x=`. |
| `scripts variable list` | `vehicle.variables.list` | Read | Yes | No | None | EXPERIMENTAL | Requires `--handle=`. |
| `scripts variable get` | `vehicle.variable.get` | Read | Yes | No | None | EXPERIMENTAL | Requires `--handle=`, `--name=`. |
| `scripts variable set` | `vehicle.variable.set` | Write | Yes | Yes (script variable) | None, not reverted | EXPERIMENTAL | Requires `--handle=`, `--name=`, `--value=` (finite number). |
| `scripts string list` | `vehicle.string-variables.list` | Read | Yes | No | None | EXPERIMENTAL | Requires `--handle=`. |
| `scripts string get` | `vehicle.string-variable.get` | Read | Yes | No | None | EXPERIMENTAL | Requires `--handle=`, `--name=`. |
| `scripts string set` | `vehicle.string-variable.set` | Write | Yes | Yes (in-memory script state) | None, not reverted | EXPERIMENTAL | Requires `--handle=`, `--name=`, `--value=`. OMSI's own `SetStringVariable`. Runtime validation pending. |
| `scripts trigger` | `vehicle.trigger` | Action | Yes | Yes (runs vehicle script code) | None, not reverted | EXPERIMENTAL | Requires `--handle=`, `--name=`, `--value=` (`1` press, `0` release, `2` drag). Runtime validation pending. |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | Yes | Yes (plays vehicle sounds) | None, not reverted | EXPERIMENTAL | Requires `--handle=`, `--trigger=`; optional `--file=`. Runtime validation pending. |

### Operations without a route

These public operation ids (`PublicCapabilityRegistry.PublicRuntimeOperationIds`) have no hierarchical route and are invoked with `/runtime:<operation>` plus `--key=value` or `/runtime-arg:key=value`: `timetable.rv-files.list`, `timetable.track-entries.list`, `timetable.tour-entries.list`, `d3d.status`, `d3d.texture.create` (`width`, `height`, `format` required; `levels` optional), `d3d.texture.describe` (`handle`; `level` optional), `d3d.texture.update` (`handle`, `width`, `height`, `pixels_base64` required; `level`, `x`, `y` optional), `d3d.texture.release` (`handle`). D3D operations are EXPERIMENTAL; the texture lifecycle and device-reset invalidation are runtime-validated (runtime closure `H02`, `D01`; see [capabilities](capabilities.md)). `timetable.track-entries.list` and `timetable.tour-entries.list` are bounded lists: a result that does not fit the runtime slot is shortened (`truncated=true`). `internal.road-vehicles.make-basic` is INTERNAL and is rejected with `OL_E_RUNTIME_OPERATION_UNKNOWN` by both the CLI and the API.

## Flags

Every flag of `CliInput.KnownFlags`. "Phase" is *launch-time* (shapes the `LaunchSpec`/plan of a new session), *runtime* (acts on a running session) or *control* (changes how the CLI itself behaves). Flags parsed for compatibility only (`CliInput.AcceptedNoEffectFlags`) are marked on their line.

### Control and output

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/?` | `/?` | off | control | STABLE_BETA | Print the usage text, exit `0`. |
| `/help` | `/help` | off | control | STABLE_BETA | Same as `/?`. (The bare word `help` returns the structured catalogue instead.) |
| `/version` | `/version` | off | control | STABLE_BETA | `version` envelope, exit `0`. Evaluated before every other command except `/silent`. |
| `/json` | `/json` or `--json` | off | control | STABLE_BETA | Emit JSON envelopes; also forces console output even under `OmsiLaunchW.exe`. |
| `/quiet` | `/quiet` | off | control | STABLE_BETA | Suppresses the human-readable success output. JSON output (`/json`) and errors are still written; the exit code is unchanged. |
| `/silent` | `/silent` (also `--silent`) | off | control | EXPERIMENTAL | Delegate the whole command line to `OmsiLaunchW.exe`, return `0` as soon as the host process started. The session outcome is reported by `OmsiLaunchW.exe` (message boxes, tray icon), `.omsilaunch\diagnostics` and the local control endpoint. Delegation and the failure dialogs are runtime-validated (runtime closure `T04`); see [OmsiLaunchW.exe](omsilaunchw.md). |
| `/serve` | `/serve` | — | control | REMOVED | Removed in 0.1.0-beta4: rejected with `OL_E_INVALID_ARGUMENT` (exit code `2`). The session owner always serves the local control endpoint. |
| `/verbose` | `/verbose` | off | launch-time | PARTIAL | `DiagnosticsSpec.Verbose`. Values are carried in the spec; their effect is limited to the host trace under `.omsilaunch\diagnostics`. |
| `/log` | `/log` | on (`DiagnosticsSpec.Log` defaults to `true`) | launch-time | PARTIAL | `DiagnosticsSpec.Log`. Effectively always on. |
| `/logall` | `/logall` | off | launch-time | PARTIAL | Sets `Verbose`, `ProcessTrace`, `PluginTrace` and `NativeTrace` together. |
| `/omsi-logall` | `/omsi-logall` | off | launch-time | PARTIAL | `DiagnosticsSpec.OmsiLogAll`. |
| `/trace` | `/trace` | off | launch-time | PARTIAL | Alias of `/trace-process`. |
| `/trace-process` | `/trace-process` | off | launch-time | PARTIAL | `DiagnosticsSpec.ProcessTrace`. |
| `/trace-plugin` | `/trace-plugin` | off | launch-time | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: the plugin reports every runtime operation (`plugin.trace.operation`: operation, outcome, code, duration) to the session's host trace. |
| `/trace-native` | `/trace-native` | off | launch-time | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: the native layer reports every hook installation and removal (`native.trace`) to the session's host trace. |

### Planning, validation and harnesses

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/plan` | `/plan` | off | launch-time | STABLE_BETA | Build and print the `SessionPlan`, do not start OMSI. Exit `0` when `IsRunnable`, `1` otherwise. Requires a launch selection (`/new`, `/saved`, `/spec`, or an installation argument); `/plan` alone with nothing else runs `detect`. |
| `/validate` | `/validate` | off | launch-time | STABLE_BETA | Identical to `/plan` in this build. |
| `/runtime-batch` | `/runtime-batch` | off | runtime (owner) | INTERNAL | Validation harness: after `Running`, executes the read operation set and writes `<sessionId>-runtime-read-batch.json`. |
| `/runtime-write-batch` | `/runtime-write-batch` | off | runtime (owner) | INTERNAL | Validation harness: reads plus `time.set`, `camera.set` and `vehicle.variable.set` with restore; writes `<sessionId>-runtime-write-batch.json`. |
| `/d3d-batch` | `/d3d-batch` | off | runtime (owner) | INTERNAL | Validation harness for the D3D texture lifecycle; writes `<sessionId>-d3d-wave-d-batch.json`. |
| `/runtime` | `/runtime:<operation>` | none | runtime | STABLE_BETA (dispatch) | Select a public runtime operation by id. Client mode (no installation argument): forwarded to the owner. Owner mode: executed once after `Running`. Unknown ids: `OL_E_RUNTIME_OPERATION_UNKNOWN`, exit `2`. |
| `/runtime-arg` | `/runtime-arg:<key>=<value>` (repeatable) | none | runtime | STABLE_BETA (dispatch) | Runtime argument; equivalent to `--key=value`. Missing `=`: `/runtime-arg requires key=value`, exit `2`. |

### World selection

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` is the default mode, but a launch is only requested when one of `/new`, `/saved`, `/last`, `/spec` is present | launch-time | STABLE_BETA | NEW_MAP. Requires `/map` and `/entrypoint-index` (a plan without a presented entrypoint index reports `OL_E_ENTRYPOINT_REQUIRED`; without `/map` no map is resolved). `/new` never selects a map silently. |
| `/saved` | `/saved:<file.osn>` | none | launch-time | STABLE_BETA | SAVED_SITUATION. Map and position come from the `.osn`; `/map`, `/entrypoint`, `/entrypoint-index` are rejected with `/saved` (exit `2`). Missing situation: `OL_E_SITUATION_NOT_FOUND`; its map missing: `OL_E_SITUATION_MAP_NOT_FOUND`. |
| `/last` | `/last /map:<identity>` | none | launch-time | EXPERIMENTAL | LAST_MAP_STATE: loads the map's own `laststn.osn` (written by OMSI when the map was closed) through the Start form's last-state branch. Requires `/map`; a map without `laststn.osn` is `OL_E_SITUATION_NOT_FOUND`. Never a newest-`.osn` heuristic. Runtime validation pending. |
| `/map` | `/map:<identity>` (for example `maps\Grundorf\global.cfg`) | none | launch-time | STABLE_BETA | Map identity for `/new`, or the scope for `/list:Entrypoints`. Unknown: `OL_E_MAP_NOT_FOUND`. |
| `/entrypoint` | `/entrypoint:<identity>` | none | launch-time | EXPERIMENTAL | Entrypoint by its exact (case-sensitive) label or by an identity from `/list:entrypoints`. OMSI presents only the first record of each label; a later record sharing it is refused (`OL_E_ENTRYPOINT_NOT_PRESENTED`), an unknown one too (`OL_E_ENTRYPOINT_NOT_FOUND`). Runtime validation pending. Mutually exclusive with `/entrypoint-index` (the identity wins and clears the index). |
| `/entrypoint-index` | `/entrypoint-index:<n>`, `0..2147483647` | none | launch-time | STABLE_BETA | Presented-list index of the entrypoint (1-based as OMSI presents it). Required for a runnable NEW_MAP plan. |

### Date, time and weather

These flags are accepted and carried into the `LaunchSpec`. The start date, time and year apply to NEW_MAP sessions only: with `/saved` or `/last` the planner records them as `UNAVAILABLE` **and adds `OL_E_CAPABILITY_UNAVAILABLE`, so the plan is NOT RUNNABLE (exit `1`)**, because a saved situation keeps its own clock. The weather applies to every world mode. All of them are `EXPERIMENTAL`: the native start path is closed statically and runtime validation is pending. A `/spec` file or a session profile that sets them has the same effect.

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` or `/date:system` | unset | launch-time | EXPERIMENTAL | `DateSpec` explicit/system: the start date of a NEW_MAP session (see [launchspec](launchspec.md#datespec-timespec-yearspec)). Unparseable value: `OL_E_INVALID_ARGUMENT`, exit `2`; an impossible date: `OL_E_DATE_TIME_APPLY_FAILED`. |
| `/time` | `/time:<hh:mm[:ss]>` or `/time:system` | unset | launch-time | EXPERIMENTAL | `TimeSpec` explicit/system: the start time of a NEW_MAP session. |
| `/year` | `/year:<n>` or `/year:system` | unset | launch-time | EXPERIMENTAL | `YearSpec`: the start year of a NEW_MAP session (1900..2200). |
| `/weather` | `/weather:<preset>` | unset | launch-time | EXPERIMENTAL | `WeatherMode.Preset`: a file of the installation's `weather` folder, by name (`Regen`) or path (`weather\Regen.owt`); a missing one is `OL_E_WEATHER_INVALID` at plan time. Loaded through OMSI's own `loadWeather` right after the world starts; live METAR stays off. Runtime validation pending. |
| `/weather-icao` | `/weather-icao:<code>` | unset | launch-time | EXPERIMENTAL | `WeatherMode.Icao`: live METAR weather for a four-character ICAO station (`OL_E_WEATHER_INVALID` otherwise). OMSI saves the station in `options.cfg`, which the session restores. Runtime validation pending. |
| `/weather-real` | `/weather-real` | unset | launch-time | EXPERIMENTAL | `WeatherMode.RealCurrent`: live METAR weather for the station OMSI already has configured; without one the start fails with `OL_E_WEATHER_APPLY_FAILED`. The last of `/weather`, `/weather-icao`, `/weather-real` wins. Any start weather overrides a saved situation's weather. |

### Player vehicle

Accepted and resolved against the installation, but not applied by the runtime: each set field is `UNAVAILABLE` and adds `OL_E_CAPABILITY_UNAVAILABLE` (plan NOT RUNNABLE, exit `1`).

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>` (`Vehicles\...\*.bus`) | unset | launch-time | UNAVAILABLE | Resolved first (`OL_E_VEHICLE_NOT_FOUND` if unknown). |
| `/repaint` | `/repaint:<id>` | unset | launch-time | UNAVAILABLE | Resolved only together with `/vehicle` (`OL_E_REPAINT_NOT_FOUND`). |
| `/hof` | `/hof:<id>` | unset | launch-time | UNAVAILABLE | `OL_E_HOF_NOT_FOUND` if unknown. |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | none | launch-time | STABLE_BETA | One session [HOF management](hof-management.md) operation: `HofSourceKind` and `Source`. Requires at least one `/hof-target`. Replaces the `HofManagement` of a `/spec`; with a session-profile preset that has `hofs`: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Malformed: exit `2`. |
| `/hof-target` | `/hof-target:<vehicle identity or folder>` (repeatable) | none | launch-time | STABLE_BETA | A target vehicle below `Vehicles\`. Without `/hof-source`: exit `2`. |
| `/hof-file` | `/hof-file:<name.hof>` (repeatable) | all files | launch-time | STABLE_BETA | Restricts the source to these file names. Without `/hof-source`: exit `2`. |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | launch-time | STABLE_BETA | `HofApplyMode`. Without `/hof-source`: exit `2`. |
| `/module` | `/module:<id>` (repeatable) | none | launch-time | EXPERIMENTAL | Requests a [runtime module](runtime-modules.md) for the session (`LaunchSpec.Modules.Enable`, added to the modules of a `/spec` file). A requested module that cannot be loaded makes the plan not runnable with its `OL_E_MODULE_*` code (exit `1`). Its operations are reached with `/runtime:module.<id>.<operation>` and `/runtime-arg`. With a session-profile preset that sets `enabled: false` for it: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |
| `/no-module` | `/no-module:<id>` (repeatable) | none | launch-time | EXPERIMENTAL | Keeps a module out of the session, including a module loaded by default, by its map or by the settings file (`LaunchSpec.Modules.Disable`). With a preset that sets `enabled: true` for it: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Without a value: exit `2`. |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>` (repeatable) | none | launch-time | EXPERIMENTAL | Sets a module setting (`LaunchSpec.Modules.Settings`); wins over the `/spec` file, the settings file and the default. A key the selected preset also sets: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. Not `<id>.<key>=<value>`: exit `2`. Invalid value or undeclared key: plan diagnostic `OL_E_MODULE_SETTING_INVALID` (exit `1`). |
| `/<module flag>` | `/<flag>:<value>`; `/<flag>` alone means `true` for a boolean setting | none | launch-time | EXPERIMENTAL | A flag an installed module declares for one of its settings (`flag` in its manifest; listed by `/list:RuntimeModules`). It is resolved when a session is planned and is the same as `/module-setting:<id>.<key>=<value>`. A flag no installed module declares, or one used outside a launch: `Unknown argument`, exit `2`. A flag declared by two modules: `OL_E_MODULE_SETTING_INVALID`, exit `2`. A module flag never replaces a built-in flag. |
| `/fleet` | `/fleet:<n>` | unset | launch-time | UNAVAILABLE | Fleet number. |
| `/registration` | `/registration:<text>` | unset | launch-time | UNAVAILABLE | Registration. |
| `/no-vehicle` | `/no-vehicle` | off | launch-time | STABLE_BETA | Clears any player vehicle from the seed (`/spec` or profile). Harmless. |

### Configuration overlays

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>` (repeatable; keys case-insensitive) | none | launch-time | STABLE_BETA | Semantic `options.cfg` overlay from `ConfigurationCatalog` (for example `graphics.maxFPS=60`, `traffic.randomVehicles=150`). Unknown key: `OL_E_UNKNOWN_SETTING` (exit `2`); read-only key (`advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter`): `OL_E_SETTING_NOT_WRITABLE` (exit `2`); out-of-range or malformed value: `OL_E_INVALID_SETTING_VALUE` when the overlay is built. The overlay is a session mutation: snapshotted, applied before OMSI starts, restored byte-for-byte at stop (RV-005 `RUNTIME_VALIDATED`). Conflicts with a preset-owned key of a selected profile: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`. |

### Splash presentation

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`, `/splash:Native`, `/splash:Unset` (case-insensitive) | `Managed` | launch-time | STABLE_BETA | `Managed`: the packaged 640x480 24-bit BMPs are copied once to `<root>\.omsilaunch\assets\splash` and `GUI\NewSplashscreen_ENG.bmp` plus `GUI\NewSplashscreen_<lang>.bmp` are overlaid transactionally and restored exactly (RV-006 `RUNTIME_VALIDATED`). `Native`/`Unset` (aliases): OMSI files untouched. Missing value: `/splash requires Unset, Native, or Managed`, exit `2`. |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA` (also `pt-BR`, `de`, `fr`, `en`; anything else falls back to `ENG`) | `[language]` from `options.cfg`, else `ENG` | launch-time | STABLE_BETA | Selects the localized target file. |
| `/splash-assets` | `/splash-assets:<directory>` (relative paths resolve below the installation root) | `<root>\.omsilaunch\assets\splash`, else the packaged set | launch-time | STABLE_BETA | Custom asset directory; must contain `ENG.bmp` and, for a non-English language, `<lang>.bmp`. Errors: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED` (surfaced as `OL_E_SESSION_PRESENTATION_INVALID` in the plan; not runnable). |

### Internet textures

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | launch-time | EXPERIMENTAL | `Native`: untouched. `Disabled`: the profiled in-process downloader is suppressed. `Override`: the given `.itx` profile is overlaid as `Texture\standard.itx`; every HTTP(S) target listed in it plus `Texture\standard.ipr` become session deletions (removed for the session, restored at stop). Missing value: exit `2`. |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | none | launch-time | EXPERIMENTAL | Required with `Override` (`OL_E_ITX_PROFILE_REQUIRED`, exit `2`). `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID` (must be URL/target line pairs with `http`/`https` URLs), `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` (targets must resolve under `Texture\` with no rooted paths, `..`, or reparse points). |

### Session profiles

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | none | launch-time | STABLE_BETA (compilation; offline `OmsiLaunch.ProfileTests`) | Loads `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` (see [session profiles](session-profiles.md)). Requires `/predefined-profile-index` (`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`, exit `2`). The `new:` block applies only with `/new`; `compatibility.maps` is enforced for `/new` and `/saved` (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). Explicit flags that collide with a profile-owned field are rejected with `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` (`CliInput.RejectProfileConflicts`): map/entrypoint/date/time/year/weather when the `new:` block owns them, `/set` keys owned by the preset, splash flags when the preset has `presentation`, internet-texture flags when it has `internet-textures`, timeouts when it has `behavior`. |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | none | launch-time | STABLE_BETA | Selects the preset by `index`. Out of range: exit `2`. |

### LaunchSpec file

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | none | launch-time | STABLE_BETA (loader offline-tested; session semantics identical to flags) | Loads a `LaunchSpec` JSON file as the seed (see [LaunchSpec](launchspec.md)) and marks a launch as requested. Rules (`LaunchSpecJson`): file must exist (`OL_E_SPEC_NOT_FOUND`, exit `6`); at most 1 MiB (`OL_E_SPEC_TOO_LARGE`, exit `2`); root must be an object (`OL_E_SPEC_INVALID`); property names case-insensitive; `//` comments and trailing commas allowed; depth at most 32; every unknown property is rejected with its JSON path (`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`, exit `2`). |

**Precedence** (`CliInput.BuildSpecAsync`): defaults → `/spec` file → `/predefined-profile` (replaces `Installation` and `World`, then applies the profile) → explicit flags. An explicit installation argument beats `RootPath` in the spec. `/no-vehicle` clears the spec's player vehicle; `/vehicle` and friends merge into it field by field. `/set` keys merge into `Environment.General`. `/splash`, `/splash-language`, `/splash-assets`, `/internet-textures`, `/internet-textures-profile` override only when given. `/startup-timeout` and `/shutdown-timeout` override only when given; `Presentation.SuppressTrayIcon` comes from the spec only (no flag). Diagnostics flags are OR-ed with the spec's `Diagnostics`.

### Content discovery

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`; categories are the `ContentQueryKind` values `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules` (case-insensitive) | none | local, no session | STABLE_BETA | `DiscoverAsync` over the installation; envelope `content.list` with `Identity`, `Kind`, `DisplayName` entries; exit `0`. `SessionProfiles` answers with `ListSessionProfilesAsync` (envelope `session-profiles.list`) and `RuntimeModules` with `ListRuntimeModulesAsync` (envelope `runtime-modules.list`: each module's id, version, activation, operations, validity and its settings with type, default and flag). Unknown category: `Unknown discovery category`, exit `2`. Reparse points (junctions/symlinks) are skipped, OMSI files are read as Windows-1252. |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | none | local | STABLE_BETA | Scope forwarded for every category except `Entrypoints`, which uses `/map` as its scope. |

### Timeouts and observation

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` seconds | spec/profile value, else `180` | launch-time | STABLE_BETA | `Behavior.StartupTimeoutSeconds`. The owner waits for `Running` for this value plus 5 s; `OL_E_STARTUP_TIMEOUT` ends the session with exit `1`. |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` seconds | spec/profile value, else `30` | launch-time | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: with `/cooperative-shutdown`, how long the supervisor waits for OMSI to quit before terminating it; without it, OMSI is terminated at once and the value has no effect. |
| `/cooperative-shutdown` | `/cooperative-shutdown` | off | launch-time | EXPERIMENTAL | `Behavior.CooperativeShutdown`: ending a running session asks OMSI to quit through its own exit routine (the one that runs after a user confirms quitting: it saves `options.cfg` and the map's `laststn.osn`, then closes) and waits `ShutdownTimeoutSeconds` before terminating it. Opt-in until runtime validated; `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` when OMSI had to be terminated. The session's files are restored after the exit either way. |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | none (run until OMSI exits or a stop is requested) | runtime (owner) | STABLE_BETA | Upper bound on the running phase: after `n` seconds of `Running` the canonical stop is requested. A tray or pipe stop, or OMSI exiting, ends it earlier. `0` stops immediately after `Running`. |

### Recovery

| Flag | Syntax and values | Default | Phase | Stability | Behaviour |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | off | local | STABLE_BETA | Report whether `<root>\.omsilaunch\journal.json` is pending (`pending`), never restores; exit `0`. Takes the installation lease: `OL_E_INSTALLATION_BUSY` (exit `7`) while an owner holds it. |
| `/recover` | `/recover` | off | local | STABLE_BETA | Restore a pending journal (backups verified against the snapshot SHA-256 first; `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED` reported in `diagnostics`). Exit `0` when nothing was pending or the restore completed; `8` when a journal was pending and remains. Refused with `OL_E_INSTALLATION_BUSY` while the journaled OMSI process (PID, creation time, exe path) or, for a journal past `HandoffCreated` without a PID, any `Omsi.exe` from that root is alive. Every session start performs the same recovery automatically before reading the installation. |
| `/clear-module-data` | `/clear-module-data:<module id>` | none | local, no session | EXPERIMENTAL | Deletes the module's data directory `<root>\.omsilaunch\module-data\<id>` and nothing else; envelope `module-data.clear` with `module` and `cleared` (`false` when there was nothing to delete); exit `0`. Takes the installation lease: `OL_E_INSTALLATION_BUSY` (exit `7`) while a session owns the installation. A module-data folder reached through a link is refused (`OL_E_MODULE_INTEGRITY`); a link inside it is removed as a link. |
| `/pack-module` | `/pack-module:<module directory>` | none | local, no session | EXPERIMENTAL | Writes the SHA-256 pins of the files the directory's `module.json` names (components and assets) and rewrites only those values; envelope `module.pack` (`module`, `directory`, `written`, `intact`, `files`); exit `0`, `2` for a manifest that does not parse or a missing file. Never touches an installation. See [packing a module](runtime-modules.md#packing-a-module). |
| `/verify-module` | `/verify-module:<module directory>` | none | local, no session | EXPERIMENTAL | Reports the same without writing (`module.verify`); exit `0` when every pinned file matches (a file without a pin is not a mismatch), `7` when a pin does not match, `2` for a manifest that does not parse or a missing file. |

## Output formats

- **Envelope success** (`CliInput.WriteEnvelope`, with `--json`): `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`, indented. Forwarded `session status` and `events read` replies add a `metadata` member when older events were left out to fit the control frame (`events_dropped_count`, see [local control](local-control.md)). Without `--json` only `<object>` is printed as indented JSON, followed by `Note: <n> older events were omitted to fit the control frame.` when events were dropped.
- **Envelope error** (`CliInput.WriteError`, with `--json`): `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`. Without `--json`: `OL_E_<CODE>: message` on one line. Categories: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. Under `OmsiLaunchW.exe` the same code and message are shown in a message box.
- **Plan and status** (`CliInput.Write`): the `SessionPlan`, `SessionStatus` and `RuntimeCommandResult` records are printed as indented JSON **without** an envelope. Without `--json` a plan is summarised as `Plan: READY profile=Omsi23004_692EBFBF` or `Plan: NOT RUNNABLE profile=...`; other records are still printed as JSON. Enum values serialize as integers (`SessionState.Running` is `14`, `Completed` is `18`, `Failed` is `19`).
- Command names used in envelopes: `silent`, `version`, `capabilities`, `help`, `profiles`, `detect`, `recover`, `content.list`, `session`, `session.status`, `session.stop`, `events.read`, `events.watch`, `events watch`, `installation`, `cli`, `session profile`, and the runtime operation id for forwarded runtime commands.
- Under `OmsiLaunchW.exe` (`OMSILAUNCH_WINDOWS_HOST=1`) nothing is written to the console unless `--json` is given.

## Errors per command

| Command | Typical error codes | Exit |
|---|---|---|
| Any parse failure | `OL_E_INVALID_ARGUMENT`, session-profile codes (`OL_E_SESSION_PROFILE_*`) | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`, `OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| Client route, `/runtime` (client) | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (`2`); `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_*`, `OL_E_RUNTIME_*` returned by the owner, e.g. `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`, `OL_E_RUNTIME_RESPONSE_TOO_LARGE`, `OL_E_SESSION_NOT_RUNNING` (`7`) | `2`, `4`, `7` |
| `session status`, `session stop`, `events read`, `events watch` | `OL_E_NO_ACTIVE_SESSION` (`4`); `OL_E_CONTROL_SESSION_MISMATCH`, `OL_E_CONTROL_PROTOCOL`, `OL_E_CONTROL_FAILED` (`7`) | `4`, `7` |
| Owner preflight | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`, `OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`, `/recover` | `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_RECOVERY_*`, `OL_E_RESTORE_FAILED` (`8`); pending-but-not-recovered (`8`) | `7`, `8` |
| `/list` | unknown category (`2`); `OL_E_INSTALLATION_NOT_FOUND`/missing directories (`6`) | `2`, `6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND` (`6`); `OL_E_SPEC_TOO_LARGE`, `OL_E_SPEC_INVALID`, `OL_E_SPEC_UNKNOWN_PROPERTY` (`2`) | `2`, `6` |
| `/set` | `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`, `/validate`, launch | plan diagnostics: `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (installed plugin closure), `OL_E_UNSUPPORTED_BUILD`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_SESSION_PRESENTATION_INVALID`, `OL_E_RUNTIME_ARTIFACT_MISSING`, `plugin.integrity.reference` (informational) | `1` |
| Session start | `OL_E_PLAN_NOT_RUNNABLE` (re-plan at start, `1`); `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_RELEASE_MANIFEST_INVALID` (normally reported by planning as a plan diagnostic, exit `1`; `7` only if the plugin files change between planning and start), `OL_E_INSTALLATION_BUSY` (`7`); `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_EXITED_EARLY`, `OL_E_STARTUP_TIMEOUT`, `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_PLUGIN_NOT_LOADED` (session `Failed`, `1`) | `1`, `7` |
| Unhandled exception anywhere | classified by `CliProgram.Classify` (see [exit codes](exit-codes.md)) | `2`..`10` |

## Environment

| Variable | Set by | Effect |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: console output suppressed, failures as message boxes, `/silent` not re-delegated. |

## See also

[CLI examples](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [exit codes](exit-codes.md) · [errors](errors.md) · [local control](local-control.md) · [Windows tray](windows-tray.md) · [runtime control](runtime-control.md) · [capabilities](capabilities.md) · [LaunchSpec](launchspec.md) · [session profiles](session-profiles.md) · [packaging](packaging.md) · [compatibility](compatibility.md) · [known limitations](known-limitations.md) · [public API](public-api.md)
