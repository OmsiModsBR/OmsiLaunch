# Runtime Control

Status: NORMATIVE (summary; the detailed references are
[`docs/reference/runtime-control.md`](../reference/runtime-control.md) and
[`docs/reference/capabilities.md`](../reference/capabilities.md))

OmsiLaunch runtime control is session-scoped. It is not OmsiHook RPC
compatibility mode and it is not an external-process attach architecture.

```
OmsiLaunch API/CLI -> live session -> runtime mailbox -> PluginRuntime
    -> OMSI UI-thread gateway -> profiled Interop / Native.x86 -> OMSI
```

The startup handoff remains startup-only. Runtime commands use a separate
versioned mailbox bound to the session GUID and its launched OMSI process.
Every request has a fixed-width request ID, UTF-8 payload, SHA-256 integrity
check, timeout and response identity validation. A mapping is created before
`CreateProcessW`, is supplied only through the child environment, and is
disposed on PluginFinalize, process exit, session cleanup, or failed startup.

`RuntimeCommand` is semantic data: operation name plus UTF-8 key/value
arguments. It contains no OMSI pointers, Win32 handles, DNNE objects, or CLR
object serialization. The plugin dispatches commands from its existing OMSI
UI-thread timer; IPC workers never call Delphi/Borland methods directly.

Runtime control is session-scoped. The CLI is a thin reference frontend over
`IOmsiLaunch.ExecuteRuntimeAsync`; it never attaches independently or embeds
OMSI-specific logic.

## Session Ownership And Duration

Every normal launch (`/new`, `/saved`, `/last`, or `/spec`) creates the
session owner and its local control plane automatically. `/serve` remains a
compatibility spelling only; it is not required to keep a session controllable.

A normal product session has no automatic stop timer. After `RUNNING`, the
owner remains active until OMSI exits naturally or a stop is requested through
`session stop`, the tray "End session" action, Ctrl+C, console close, or
`CloseAsync`. Runtime-command failures are reported to the caller but do not
tear down a healthy owner session.

Every stop path requests the same canonical stop. The supervisor then
terminates OMSI by force (`TerminateProcess`): OMSI's own shutdown routine does
not run and OMSI does not rewrite `options.cfg` on exit. This is deliberate; it
protects the transaction from OMSI writing over files that are about to be
restored. The `closecheck` marker OMSI leaves behind is a session deletion and
is removed at restore. Cooperative `WM_CLOSE` shutdown is not implemented (see
[`POST-RELEASE-BACKLOG.md`](../project/POST-RELEASE-BACKLOG.md)).
`LaunchBehaviorSpec.ShutdownTimeoutSeconds` is carried in the spec but is
currently not consumed by the supervisor.

`/observe-seconds:N` is the only opt-in timed policy. It exists for validation
and automation: after `RUNNING`, it waits at most `N` seconds and then requests
the canonical stop, including process observation and exact transactional
restoration; a tray or pipe stop still ends the session earlier. It is never
implied by `/new`, `/saved`, `/last`, `/spec`, or a runtime command.

After a declarative launch request reaches `RUNNING`, a single semantic command
may be issued with:

```text
OmsiLaunch.Cli <installation> /new /map:maps\Grundorf\global.cfg \
  /entrypoint-index:1 /no-vehicle /runtime:time.read
OmsiLaunch.Cli <installation> /new /map:maps\Grundorf\global.cfg \
  /entrypoint-index:1 /no-vehicle /runtime:time.set /runtime-arg:minute=18
```

`/runtime-arg` repeats for additional semantic fields. Each command travels via
the session-bound request/response mailbox, is executed by `PluginRuntime` on
the OMSI UI timer, and is rejected when the session binding or request identity
does not match. The command is not a persistent configuration editor.

## Public Runtime Operations

The complete set a public frontend may forward is
`PublicCapabilityRegistry.PublicRuntimeOperationIds`. Anything else, including
every `internal.*` operation, is rejected with `OL_E_RUNTIME_OPERATION_UNKNOWN`
before the session is looked up; a missing required argument is rejected with
`OL_E_RUNTIME_ARGUMENT_REQUIRED`. Result keys that start with `internal_` or
end with `_address`, `_pointer` or `_vmt` are stripped.

| Family | Operations | State on the known layout (`Omsi23004`) |
| --- | --- | --- |
| Time | `time.read`, `time.set` | runtime validated |
| Weather | `weather.read`, `weather.actual.read` | runtime validated |
| Weather | `weather.set` | UNAVAILABLE: always rejected with `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` |
| Map | `map.read` | runtime validated (revalidated after the map slot correction) |
| Camera | `camera.read`, `camera.set` | runtime validated |
| Camera | `camera.lock`, `camera.unlock` | runtime validated with a PlayerVehicle (runtime closure `CAM01`) |
| Road vehicles | `road-vehicles.read`, `road-vehicles.list`, `road-vehicle.read`, `player-vehicle.read` | runtime validated |
| Road vehicles | `road-vehicles.spawn`, `road-vehicles.place-random` | runtime validated (native world mutation, EXPERIMENTAL) |
| Humans | `humans.read`, `humans.list`, `human.read` | runtime validated |
| Timetable | `timetable.read`, `timetable.tracks.list`, `timetable.trips.list`, `timetable.lines.list`, `timetable.bus-stops.list`, `timetable.station-links.list`, `timetable.tours.list`, `timetable.profiles.list`, `timetable.tour-entries.list`, `timetable.track-entries.list`, `timetable.rv-files.list`, `timetable.logs.read` | runtime validated; bounded and read-only |
| Vehicle scripts | `vehicle.variables.list`, `vehicle.variable.get`, `vehicle.variable.set`, `vehicle.string-variables.list`, `vehicle.string-variable.get` | runtime validated |
| Vehicle data | `vehicle.constants.list`, `vehicle.constant.get`, `vehicle.curves.list`, `vehicle.curve.evaluate`, `vehicle.hofs.read` | runtime validated |
| Records | `drivers.read`, `tickets.read` | runtime validated |
| D3D | `d3d.status`, `d3d.texture.create`, `d3d.texture.describe`, `d3d.texture.update`, `d3d.texture.release` | runtime validated, including reset invalidation (`D01`); a distinct device-loss transition was not produced |

"Runtime validated" refers to sessions recorded in
`research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md` before the hardening
round; nothing from the hardening round has run under OMSI yet. Arguments for
mutations are deliberately whitelisted by the profile adapter. The CLI routes
`vehicles summary` and `humans summary` map to `road-vehicles.read` and
`humans.read`.

## Command Channel Hardening

The session mailbox is a single-flight memory-mapped region of 64 KiB. Each
request is bound to the session id and a request id. A response that arrives
after the host has timed out is detected and discarded, so a late plugin
response can no longer wedge the channel as busy; the plugin never publishes a
response for an abandoned request. Oversized results are reported as
`OL_E_RUNTIME_RESPONSE_TOO_LARGE`. Default timeouts: CLI client 8 s (30 s for
`road-vehicles.spawn`); owner single operation 5 s / 15 s. Telemetry uses a
latest-value slot with a producer sequence, so identical consecutive events
are distinct and torn samples are skipped.

`weather.set` is recognized but currently rejects scalar writes with
`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`. Both profiled wind candidates were
overwritten by the next normal OMSI weather tick during validation; a native
weather-application lifecycle must be reconciled before mutation is exposed.

`timetable.track-entries.list` is a validated, bounded immutable snapshot of
each track entry's semantic path identity and timing-relevant metadata. The
canonical Grundorf batch returned 91 records through the session channel.

`timetable.rv-files.list` is a validated bounded snapshot. Grundorf's active
runtime returned a valid empty collection. `NoRVNumbers` is intentionally not
published: the pinned upstream `raw:true` interpretation of the candidate
field conflicts with the observed `Omsi23004_692EBFBF` layout, so it remains
profile-unresolved rather than exposing an unsafe read.

`vehicle.variable.set` is runtime-validated through the same profile-scoped channel.
It accepts an opaque `handle`, open-string `name`, and finite float `value`,
resolves the current public-variable slot, then returns an immediate read-back.

Profiled entity snapshots use opaque handles returned by `road-vehicles.list`
(`rv-NNNNNN`) and `humans.list` (`hb-NNNNNN`), then consumed by
`road-vehicle.read` and `human.read` with `/runtime-arg:handle=<handle>`. A
handle is valid only for its owning session and is rejected as
`OL_E_RUNTIME_OBJECT_HANDLE_STALE` if its native object is no longer present or
its address was reused by a different object (fingerprint: VMT plus
definition/model index). Residual blind spot: the same class and model
recreated at the same address between two list reads. It is not a pointer,
list index, or persistent identity.

## D3D9 Advanced Control

The public typed surface in `D3DRuntimeApi` wraps the semantic D3D operations.
`D3DTextureHandle` contains a session tag and an internal device-generation
token; it is neither a COM pointer nor valid after release, Reset, device
replacement, process exit, or another session launch.

The plugin acquires the exact-profile candidate from `D3DDevice`, retains one
QI reference, and dispatches all texture calls from the existing OMSI
UI/main/render-owner timer. Textures are bounded dynamic `D3DPOOL_DEFAULT`
resources. Updates validate level, rectangle and payload, then use
`LockRect`/`UnlockRect` with pitch-aware row copies. The mailbox payload is
bounded to 48 KiB; larger texture uploads are expressed as multiple rectangle
updates.

The lifecycle observer publishes `d3d.ready`, `d3d.lost`, `d3d.resetting` and
`d3d.restored` into `SessionStatus.RuntimeEvents`. Reset invalidates all live
default-pool resources before invoking the original native method and advances
the device generation. `d3d.ready`, normal texture operations, StopSession,
forced exit, relaunch cleanup, released handles and cross-session stale handles
are proven, and runtime closure `D01` proved `d3d.resetting`/`d3d.restored`,
the generation advance and stale-handle rejection across two OMSI-driven
resets. A distinct `d3d.lost` transition was not produced.
