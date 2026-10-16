# Known Limitations

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> British English edition of the [canonical page](../../../reference/known-limitations.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page lists, from the code, everything in OmsiLaunch 0.1.0-beta4 that is `UNAVAILABLE`, `PARTIAL`, or an accepted risk, so that users and integrators do not build on behaviour the product does not provide. Each row names the limitation, its stability, why it exists, and where it is documented in detail. The English documentation is normative; localised copies under `docs/localized/` are not maintained at the same level and may lag behind (see the last section).

## Compatibility

| Limitation | Stability | Detail |
| --- | --- | --- |
| Capabilities that read object layouts (vehicles, humans, weather, map, camera, timetable, scripts, drivers, tickets) are verified only on a known layout (`Omsi23004`); on another OMSI 2 build they are unavailable (`OL_E_CAPABILITY_UNRESOLVED`) until structural validators exist | `PARTIAL` on unknown builds | [compatibility](compatibility.md) |
| The genuine Steam executable (DRM-protected) has not been runtime validated; a controlled LAA copy was | `PARTIAL` for Steam | [compatibility](compatibility.md) |
| Only the known layout has been runtime validated; the build-independent resolution is verified offline against the reference executable with its identity withheld | `EXPERIMENTAL` on unknown builds | [runtime validation status](../status/runtime-validation-status.md) |
| Windows 10+ x64 only; no Windows 7/8, no XP, no ARM64 | `UNAVAILABLE` | [compatibility](compatibility.md) |
| The plugin needs the **x86** .NET 6 runtime in addition to the x64 runtime used by the controller | | [compatibility](compatibility.md) |

## World start and launch options

| Limitation | Stability | Detail |
| --- | --- | --- |
| `LAST_MAP_STATE` (`/last /map:<identity>`) is implemented from static analysis of OMSI's last-state branch and validated offline only | `EXPERIMENTAL` | [CLI](cli.md) |
| Explicit or system date, time and year (`/date`, `/time`, `/year`, profile `new.date`/`new.time`/`new.year`) apply to NEW_MAP starts only and are validated offline; runtime validation is pending. 31 December of a leap year cannot be requested (OMSI's calendar wraps day 366) | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| Weather preset, ICAO and real-current at start (`/weather*`, `new.weather`) are applied right after the world starts through OMSI's own weather loading and METAR flags (closed statically, BI-003); runtime validation is pending. The weather OMSI shows on the very first frame may still be the map's default | `EXPERIMENTAL` | [CLI](cli.md) |
| Player vehicle model, repaint, HOF, fleet number, registration at start (`/vehicle` family, `PlayerVehicleSpec`) are resolved against the content catalogue but not applied; requesting them makes the plan not runnable. The native assignment sequence (placement through OMSI's position dialogue, `MakeVehicle`, the per-unit finalise steps, `CopyTempListIntoMainList`, the player index) is closed statically, but it creates and registers a live vehicle and is not implemented before it can be validated at runtime; it is planned for a later beta. A saved situation or `/last` restores its own player vehicle | `UNAVAILABLE` (BI-007) | `player.assign-headless` in [capabilities](capabilities.md) |
| Entrypoint by identity (`/entrypoint:<identity>`) selects the exact label OMSI presents (closed statically, BI-001); a later record sharing a label cannot be selected, as in OMSI's own dialogue. Runtime validation pending | `EXPERIMENTAL` | plan capability `world.entrypoint-identity` |
| Keyboard and controller settings by key (`Environment.Keyboard`, `Environment.Controllers`) have no catalogued keys: whole files are replaced with `InputSpec` instead | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| Cooperative shutdown (`/cooperative-shutdown`, `Behavior.CooperativeShutdown`) is opt-in and validated offline only; by default OMSI is terminated when a session ends and `ShutdownTimeoutSeconds` has no effect | `EXPERIMENTAL` | [CLI](cli.md) |
| Diagnostics flags `/log`, `/omsi-logall`, `/verbose`, `/trace`, `/trace-process` populate `DiagnosticsSpec` without a further effect (the host trace under `.omsilaunch\diagnostics` is always written); `/trace-plugin` and `/trace-native` add plugin and native trace events to it | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`, `/runtime-write-batch`, `/d3d-batch` are validation harnesses | `INTERNAL` | [CLI](cli.md) |

## Session end and process control

| Limitation | Stability | Detail |
| --- | --- | --- |
| Session stop is a forced termination: `session.stop`, tray "End session", Ctrl+C and `CloseAsync` all lead to `TerminateProcess`. OMSI's shutdown routine does not run, OMSI does not rewrite `options.cfg` or its logs on exit, and any unsaved OMSI state is lost. This is deliberate: it keeps OMSI from writing over restored files. | by design | [session lifecycle](../concepts/session-lifecycle.md) |
| Cooperative WM_CLOSE shutdown with a timeout and terminate fallback is not implemented | `UNAVAILABLE` (product decision, S-11; OMSI ignored `WM_CLOSE` to its main window in the runtime closure round) | [runtime validation status](../status/runtime-validation-status.md) |
| On console close or logoff the owner has a 4 s budget to stop and restore; anything left is recovered by the journal on the next start | console close runtime validated; logoff not exercised | [transactions and recovery](../concepts/transactions-and-recovery.md) |

## Transaction, recovery and lease

| Limitation | Stability | Detail |
| --- | --- | --- |
| Installation lease is a `Local\` semaphore: one owner per installation **per logon session**; not enforced across users; not released while another process holds a handle; any same-user process can hold the name | accepted risk (S-18) | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Recovery is refused (`OL_E_INSTALLATION_BUSY`) while the journalled OMSI process, or for a journal without PID any `Omsi.exe` from that root, is running | by design | as above |
| An originally-absent overlay path whose content changed during the session blocks restore (`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`) until inspected | by design | as above |
| Journals from before ownership fingerprints can only be closed by a session with identical planned bytes (`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`) | `PARTIAL` | as above |
| Only session-owned paths are restored. OMSI's own writes during a session (`options.cfg` `[last_map]` when no setting overlays `options.cfg`, `Texture\standard.ipr`, caches, `laststn.osn`, driver profile, logs) persist, as after a direct OMSI start | by design | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Stale `closecheck` removal before a session is permanent (recorded, not restored) when `SuppressStaleClosecheckWarning` is true | by design | as above |

## Runtime control

| Limitation | Stability | Detail |
| --- | --- | --- |
| `weather.set` and `weather.metar.set` write OMSI's source weather record (closed statically); persistence over the weather tick, cloud-type resolution and the METAR download await runtime validation. Derived values (humidity, light factors, actual wind) cannot be set; while OMSI is paused they update only after unpausing | `EXPERIMENTAL` | [capabilities](capabilities.md) |
| `SetActualDateTime` only copies the PC clock into OMSI's start clock (static analysis); OmsiLaunch does not call it and sets the start clock itself | by design | `calendar.set-actual-date-time` |
| String-variable writes, named triggers and sound triggers go through OMSI's own methods (closed statically, BI-004); their effect on vehicle scripts awaits runtime validation. Only road vehicles are targets | `EXPERIMENTAL` | `scripts.string.write`, `scripts.trigger` |
| No vehicle relocation, no cross-tile spatial rebinding, no ODE-safe transform authority; position fields are read-only | `UNAVAILABLE` (BI-008) | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` need a PlayerVehicle; the headless NEW_MAP start has none (a saved situation provides one) | by design (BI-007) | RV-004 |
| Runtime mutations (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, spawn, place-random, D3D textures) are not journalled and not restored | by design | [runtime control](runtime-control.md) |
| Handle staleness is decided from OMSI's own never-reused instance number (`IDCode`) and, for humans, from despawn and respawn; it is validated offline. Natural removal (RV-002: AI cars leaving the loaded path network) is still to be observed at runtime, and no `entity.removed` event is emitted yet | `PARTIAL` | [runtime control](runtime-control.md) |
| Results are bounded by the 64 KiB mailbox: long lists are truncated (`truncated=true`); pixel payloads are limited to 48 KiB per `d3d.texture.update` | by design | [capabilities](capabilities.md) |
| Single-flight channel: one request at a time per session; a busy slot is `OL_E_RUNTIME_CHANNEL_BUSY`; request ids must not be reused | by design | [runtime control](runtime-control.md) |
| Telemetry is a 64-event ring: a host that falls more than 64 events behind loses the oldest ones and is told how many (`telemetry.lost`) | `EXPERIMENTAL` | [permanent plugin](../concepts/permanent-plugin.md) |
| A module's Direct3D device hooks are vtable hooks: on an installation where another component writes the device's original methods back (observed on the regression installation shortly after gameplay starts), they stop being called and cannot be removed at cleanup (`OL_E_MODULE_CLEANUP_INCOMPLETE`; the component stays loaded) | `PARTIAL` | [runtime modules](../../../reference/runtime-modules.md#native-components-and-hooks) |
| Runtime module loading is open: OmsiLaunch does not check who made a module; it guarantees only that modules never run inside the OmsiLaunch process and that a session program is confined to its job (no second process, ended with the session) | by design (E21) | [runtime modules](../../../reference/runtime-modules.md#security-boundary) |
| Module protocol 2 (settings, default activation, the settings file, session programs, plugin conflicts, notices, `get_value`) is validated offline only | `EXPERIMENTAL` | [runtime modules](../../../reference/runtime-modules.md) |
| D3D device reset was observed at runtime (`resetting`, `restored`, generation invalidation); a distinct `lost` transition was not produced because OMSI's device went straight to `DEVICENOTRESET` | `PARTIAL` (RV-007) | [runtime validation status](../status/runtime-validation-status.md) |
| Runtime lists are paged (`cursor`, `limit`, `next_cursor`): a page holds at most the list's maximum rows and what fits the 64 KiB runtime slot | `EXPERIMENTAL` | [capabilities](capabilities.md#pagination) |
| Some advanced map/tile/path/object graph fields are not exposed; runtime readers are profile-gated typed snapshots, never arbitrary memory access | by design | [capabilities](capabilities.md) |
| In-process memory reads are check-then-use against a live OMSI; a concurrent OMSI mutation between the check and the read can yield an inconsistent snapshot (`OL_E_RUNTIME_OPERATION_FAILED`) | accepted risk (S-33) | |

## Local control plane and trust model

| Limitation | Stability | Detail |
| --- | --- | --- |
| Same-user trust model: the named pipe (`CurrentUserOnly`), the handoff/telemetry/runtime memory mappings and the lease semaphore are accessible to any process of the same Windows user. Such a process can read status, stop the session or execute runtime operations once it has read the `session_id`. | accepted risk (S-06, S-30) | [local control](local-control.md) |
| The control endpoint exists only while the owner is `Running`; a client sees `OL_E_NO_ACTIVE_SESSION` (exit 4) during startup and after the session ends | by design | [local control](local-control.md) |
| If another process already owns the pipe name, the owner keeps running without an endpoint (`ListenFault`), and a second launch may misreport `OL_E_SESSION_ALREADY_ACTIVE` | accepted risk | [local control](local-control.md) |
| `.omsilaunch\` inherits the ACL of the OMSI root; no explicit access control is applied | accepted risk (S-31) | [transactions and recovery](../concepts/transactions-and-recovery.md) |

## Diagnostics and output

| Limitation | Stability | Detail |
| --- | --- | --- |
| Diagnostics are local files only (`.omsilaunch\diagnostics`); nothing is uploaded, and there is no remote reporting | by design | [transactions and recovery](../concepts/transactions-and-recovery.md) |
| Retention keeps the 50 newest sessions; older session-prefixed diagnostics are deleted when a new session starts | by design | as above |
| JSON output and diagnostics include installation paths (`RootPath`, asset directories, `.itx` paths) | by design (local data) | |
| The session-failure dialogue of `OmsiLaunchW.exe` shows the plugin's failure payload (for example `{"name":"world.failed",...}`) as its message rather than a sentence; the `Code:` line is correct | cosmetic | [windows tray](windows-tray.md) |
| A D3D request rejected by the native bridge before any Direct3D call reports `native_status` correctly but its `detail` text says `HRESULT 0x00000000` | cosmetic | [capabilities](capabilities.md) |
| The tray status window is a snapshot of the planned session taken when it opens; it does not refresh and shows no live OMSI values | by design | [windows tray](windows-tray.md) |

## Documentation

The English pages under `docs/` are the normative documentation for this release. `docs/localized/<locale>/` contains translations of the same `0.1.0-beta4` pages (see [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)); where a translation differs from the English text, the English text and the code are authoritative. The historical and legacy pages listed there are available in English only.

Related: [capabilities](capabilities.md), [runtime validation status](../status/runtime-validation-status.md), [errors](errors.md).
