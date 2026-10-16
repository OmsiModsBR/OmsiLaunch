# Session lifecycle

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> British English edition of the [canonical page](../../../concepts/session-lifecycle.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page describes how an OmsiLaunch session moves through `SessionState` from `Created` to `Completed` or `Failed`: which component sets each state, which plugin telemetry events drive the transitions, how the startup timeout works, what stopping means (forced termination), what `WaitForAsync` returns, which states are terminal, which states are never or barely observable, and which guarantees the CLI owner gives. Everything here is taken from `OmsiLaunchService.StartAsync`, `SuperviseAsync` and `ApplyTelemetry` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), `PluginRuntime` (`src/OmsiLaunch.Plugin/PluginRuntime.cs`) and `OwnerSession` (`tools/OmsiLaunch.Cli/Program.cs`).

Related pages: [public API](../reference/public-api.md), [error codes](../reference/errors.md), [transactions and recovery](transactions-and-recovery.md), [permanent plugin](permanent-plugin.md), [runtime control](../reference/runtime-control.md), [local control plane](../reference/local-control.md), [Windows tray](../reference/windows-tray.md), [CLI reference](../reference/cli.md), [runtime validation status](../status/runtime-validation-status.md), [the `.omsilaunch` directory](../../../concepts/omsilaunch-directory.md).

## Overview

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

## `SessionState` reference

Values in declaration order. "Set by" names the code that calls `Move`/`Fail`; "Observable" says whether `GetStatusAsync`/`WaitForAsync` can see it in practice.

| # | State | Set by | Observable | Meaning |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync` (initial value of the live session) | Briefly | The session is registered; nothing has happened yet. |
| 1 | `ValidatingPlatform` | nobody | No | Declared, never set by the current service (platform validation happens in `PlanSessionAsync`, which has no session state). |
| 2 | `Planning` | nobody | No | Declared, never set (planning happens before a session exists; the re-plan in `StartSessionAsync` also precedes registration). |
| 3 | `AcquiringInstallationLock` | `StartAsync` | Yes | The installation lease is being acquired. Failure: `OL_E_INSTALLATION_BUSY`. |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | Yes | A pending `journal.json` is restored before the live installation is read; the permanent plugin closure is validated (`plugin.integrity.reference`), `Omsi.exe` is hashed, splash assets are seeded, a stale `closecheck` is removed. Failures: `OL_E_PERMANENT_PLUGIN_*`, `OL_E_SPLASH_*`, `OL_E_ITX_*`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_*`, `OL_E_INSTALLATION_BUSY` (journalled process alive). |
| 5 | `Snapshotting` | `StartAsync` | Practically no | Set immediately before `ApplyingConfiguration` with no await in between; the snapshot itself is taken inside `ApplyAsync`. Transient and unobservable. |
| 6 | `ApplyingConfiguration` | `StartAsync` | Yes | `journal.json` is written (`Prepared`), originals are backed up, overlays written, session deletions removed (`Applied`). Failures: `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, I/O errors. |
| 7 | `DeployingRuntime` | `StartAsync` | Yes | Journal state `RuntimeDeployed`. No file is deployed: the plugin closure is permanent. |
| 8 | `CreatingStartupHandoff` | `StartAsync` | Yes | The handoff (`OmsiLaunch.Handoff.<id>`), telemetry slot (`OmsiLaunch.Telemetry.<id>`) and runtime mailbox (`OmsiLaunch.Runtime.<id>`) exist; journal state `HandoffCreated`. |
| 9 | `StartingProcess` | `StartAsync` | Yes | `CreateProcessW` for `<root>\Omsi.exe` with working directory `<root>`. Failures: `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. |
| 10 | `WaitingForPlugin` | `StartAsync` | Yes | The process exists, `process.started` is recorded, the journal is `ProcessStarted`, the supervisor is started, and `StartSessionAsync` returns. |
| 11 | `PluginBootstrap` | `ApplyTelemetry` on `plugin.started` | Yes | The permanent plugin read a valid handoff for this session. From here a timeout is `OL_E_STARTUP_TIMEOUT` instead of `OL_E_PLUGIN_NOT_LOADED`. |
| 12 | `StartingWorld` | `ApplyTelemetry` on `world.starting` | Yes (NEW_MAP only) | The plugin invoked the native NEW_MAP start on OMSI's UI thread. Saved situations emit `world.situation.starting`, which is not mapped, so a SAVED_SITUATION session goes from `PluginBootstrap` directly to `Running`. |
| 13 | `EnteringGameplay` | nobody | No | Declared, never set: `gameplay.entered` moves the session straight to `Running`. |
| 14 | `Running` | `ApplyTelemetry` on `gameplay.entered` | Yes | Gameplay reached. `ExecuteRuntimeAsync` is allowed; the CLI owner opens the local control plane; the tray shows a running session. |
| 15 | `ProcessExited` | `SuperviseAsync` | Yes (successful sessions only) | OMSI has exited (naturally or by termination), the journal is `ProcessExited`. |
| 16 | `Restoring` | `SuperviseAsync` (and the start-failure path) | Yes (successful sessions only) | Every session-owned file is restored from its verified backup; session artefacts are removed. |
| 17 | `CleaningRuntime` | `SuperviseAsync` | Yes (successful sessions only) | Restore verified, journal and backups removed; runtime stores are about to be disposed. |
| 18 | `Completed` | `SuperviseAsync` (`finally`) | Yes, terminal | Stores disposed, mailbox closed, lease released, no failure recorded. |
| 19 | `Failed` | `LiveSession.Fail` from `StartAsync`, `SuperviseAsync`, `ApplyTelemetry` | Yes, terminal | A failure diagnostic was recorded. The state is sticky: later `Move` calls are ignored, so a failed session never shows `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed` even though termination and restore still run. |

Terminal states: `Completed` and `Failed`. After either, `WaitForAsync` returns immediately and `CloseAsync` returns without requesting a stop.

Verification of "never set": a search of the code base for `SessionState.ValidatingPlatform`, `SessionState.Planning` and `SessionState.EnteringGameplay` finds only the enum declaration; `SessionState.Snapshotting` appears once, immediately followed by `Move(SessionState.ApplyingConfiguration)`.

## Start phase (`StartSessionAsync`)

1. Reject a non-runnable plan (`OL_E_PLAN_NOT_RUNNABLE`), re-plan the spec (re-hashing `Omsi.exe`, re-resolving content, re-checking the plugin closure) and reject again if it is no longer runnable. Register the live session (`Created`).
2. Create the host trace `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` (older session-prefixed files beyond the 50 newest sessions are pruned). Reject `StartupTimeoutSeconds` outside 1..600 (`ArgumentOutOfRangeException`; the session is unregistered).
3. `AcquiringInstallationLock` → lease. `RecoveringPreviousTransaction` → recover a pending journal (a pre-fingerprint journal that cannot prove ownership is deferred and retried once this session's overlays exist), validate the permanent plugin closure, hash the executable, remove a stale `closecheck`, build the transaction (overlays: `options.cfg` patches, managed splash BMPs, `Texture\standard.itx`; deletions: ITX targets, `Texture\standard.ipr`, `closecheck` when it does not exist).
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`, then the supervisor task starts and the handle is returned.
5. Any exception in steps 3–4 is caught: the session is `Failed` with `OL_E_START_SESSION` (inner message), a created process is terminated and awaited, stores are disposed, the transaction is restored (`OL_E_RESTORE_FAILED` on failure) or, if OMSI's exit could not be confirmed, left pending with `OL_E_RESTORE_DEFERRED`; the lease is released. `StartSessionAsync` still returns the handle in this case; read `GetStatusAsync`.

The re-plan keeps the caller's `SessionId`, so the id in the handle equals `plan.SessionId`.

## Supervision (`SuperviseAsync`)

The supervisor runs on a thread-pool task and loops every 100 ms until OMSI has exited or a stop was requested:

1. Read every telemetry event the plugin committed since the last read, in order (an event ring; a reader that fell behind gets `telemetry.lost` with the count). Every event is appended to `RuntimeEvents` and mapped by `ApplyTelemetry`.
2. If the session is `Failed`, leave the loop.
3. If the session is not yet `Running` and the deadline (`StartupTimeoutSeconds` after supervisor entry) has passed: `Fail` with `OL_E_STARTUP_TIMEOUT` when `PluginBootstrap` was reached, otherwise `OL_E_PLUGIN_NOT_LOADED`; leave the loop.

After the loop: if OMSI exited before `Running` and no failure was recorded, `Fail` with `OL_E_PROCESS_EXITED_EARLY`. Then, whether or not the session failed: terminate OMSI if it is still alive, wait for exit, mark the journal `ProcessExited`, move to `ProcessExited`, restore (`Restoring` → `CleaningRuntime`) or record `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`, dispose the process handle, handoff, telemetry slot and runtime mailbox, release the lease, and move to `Completed` unless the state is `Failed`. A fault inside the supervisor itself is recorded as `OL_E_PROCESS_SUPERVISION` (cleanup problems as `OL_E_PROCESS_CLEANUP_FAILED`) and the same termination/restore path runs.

Because `Failed` is sticky, the only evidence that a failed session was restored is the absence of `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` in its diagnostics (and the absence of `journal.json`); restore notes (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) appear in either case.

## Telemetry events

The plugin publishes JSON `{ "name": ..., "data": {...} }` samples into the telemetry slot; the host records each new sample as a `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`.

| Event | Emitted by | Host action |
| --- | --- | --- |
| `plugin.started` (`session_id`) | `PluginRuntime.Start` after reading a valid handoff | `Move(PluginBootstrap)`; `PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: no `OMSILAUNCH_HANDOFF_NAME`, unreadable or unverifiable handoff | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: handoff asks for a world mode other than NEW_MAP/SAVED_SITUATION, non-headless start, player vehicle, date/time modes, or an empty situation identity | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: in-process build validation failed | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | recorded only |
| `headless.arm.failed` | `PluginRuntime.Start`: the native headless start hook could not be armed | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | recorded only |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart` when `InternetTextures.Mode` is `Disabled` | recorded only |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld` (NEW_MAP) | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 or 4) | NEW_MAP: OMSI is not ready yet; the start is retried on the next UI-timer tick | recorded only |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | NEW_MAP success path | recorded only |
| `world.failed` (`native_status`) | NEW_MAP: native start returned a failure | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | SAVED_SITUATION path | recorded only (no state change) |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: native start returned a failure | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered` (NEW_MAP: entrypoint selection fields or `entrypoint_diagnostics = unavailable`; SAVED_SITUATION: `situation`) | end of the world start | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | `CurrentRuntimeControl.PollLifecycle` once any `d3d.*` operation activated the probe | recorded only |
| `camera.lock.degraded` (`code`) | `CurrentRuntimeControl.PollLifecycle` when re-applying an active `camera.lock` throws (reported once per distinct error) | recorded only |
| Invalid JSON | any | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

Caveats: the slot holds one sample, so events emitted within one 100 ms host poll can be lost (the plugin suppresses lifecycle events for 2 s after `gameplay.entered` and never publishes a D3D event in the tick that publishes `gameplay.entered`, so the `Running` boundary is not missed). `RuntimeEvents` keeps the 256 most recent events; older ones are dropped. It is not a lossless log. Read events through `GetStatusAsync`, `session.events` on the control plane, or `events read|watch` on the CLI.

## Startup timeout

| Item | Value |
| --- | --- |
| Source | `LaunchSpec.Behavior.StartupTimeoutSeconds` (default 180; 1..600; CLI `/startup-timeout`, profile `behavior.startup-timeout`). |
| Clock starts | When the supervisor task enters its loop (after the handle was returned). |
| Expiry before `PluginBootstrap` | `Failed` with `OL_E_PLUGIN_NOT_LOADED`. |
| Expiry after `PluginBootstrap`, before `Running` | `Failed` with `OL_E_STARTUP_TIMEOUT`. |
| After `Running` | No timeout applies; the session lasts until OMSI exits or a stop is requested. |
| CLI owner | Waits `StartupTimeoutSeconds + 5` seconds for `Running`; on failure prints the status, under `OmsiLaunchW.exe` shows a dialogue with the last `OL_E_` diagnostic (`OL_E_SESSION_START_FAILED` fallback code), and exits 1 after `CloseAsync`. |

`ShutdownTimeoutSeconds` is carried in the spec but not consumed: there is no shutdown wait.

## Stop semantics

Every stop request is the same canonical request: `StopAsync(handle)` from the API, `CloseAsync` on a non-terminal session, `session.stop` on the local control plane (bound to the active session id), "End session" in the tray, Ctrl+C or console close in the CLI owner, and the end of `/observe-seconds`.

| Step | Detail |
| --- | --- |
| 1 | `StopRequested` is set on the live session; the caller returns immediately. |
| 2 | Within 100 ms the supervisor leaves its loop and calls `TerminateProcess(Omsi.exe, 1)`. This is a forced termination: OMSI's shutdown routine does not run, OMSI does not rewrite `options.cfg`, no save dialogue appears. This is deliberate so OMSI cannot write over files the transaction is about to restore. |
| 3 | The supervisor waits for the process to exit, records `ProcessExited`, restores every session-owned file exactly (including the `closecheck` marker OMSI wrote during the session, which becomes a `restore.session-artifact-removed` note), removes the journal and backups, disposes the runtime stores (later `ExecuteRuntimeAsync` calls throw `OL_E_RUNTIME_CHANNEL_CLOSED` or `OL_E_SESSION_NOT_RUNNING`), releases the lease, and moves to `Completed`. |
| Natural exit | If OMSI exits on its own after `Running` (user closes OMSI), the same path runs without termination and the session completes normally. Before `Running` it is `OL_E_PROCESS_EXITED_EARLY`. |
| Cooperative shutdown | Not implemented. Sending `WM_CLOSE` and waiting `ShutdownTimeoutSeconds` is not implemented (product decision; OMSI ignored `WM_CLOSE` to its main window in the runtime closure round, `L05b`) ([runtime validation status](../status/runtime-validation-status.md)). |
| Runtime-side state | Anything changed through runtime operations (clock, camera, spawned vehicles, script variables, D3D textures) is in-process state and disappears with the process; it is never restored or persisted. |

## `WaitForAsync` semantics

| Situation | Result |
| --- | --- |
| Session reaches the requested state | Returns the status with `State == requested`. |
| Session reaches a terminal state first | Returns immediately with `Completed` or `Failed` (check `Diagnostics`). |
| Timeout elapses | Returns the current status (no exception). Compare `State` with what you asked for. |
| Requested state already passed (or never set: `ValidatingPlatform`, `Planning`, `EnteringGameplay`, effectively `Snapshotting`) | Waits until a terminal state or the timeout. |
| Caller cancels | `OperationCanceledException`. |
| Unknown or closed handle | `KeyNotFoundException`. |

The poll interval is 100 ms, so observed transitions lag the real ones by up to 100 ms.

## Owner lifecycle guarantees (CLI)

`OwnerSession.RunAsync` in `tools/OmsiLaunch.Cli/Program.cs` is the reference owner.

| Guarantee | Detail |
| --- | --- |
| Single owner | Before starting, the CLI probes the control pipe; if an owner answers, it refuses with `OL_E_SESSION_ALREADY_ACTIVE` (exit 7). The lease enforces the same rule across processes. |
| Every exit path reaches `CloseAsync` | From `StartSessionAsync` on, exceptions, Ctrl+C (`CancelKeyPress`), console close/logoff (`ProcessExit`: stop is requested and the owner waits up to 4 s for `Completed`; anything left is recovered by the journal on the next start), tray stop, control-plane `session.stop`, `/observe-seconds` expiry and natural completion all end in the `finally` block that disposes the control plane and tray and awaits `CloseAsync`. |
| `/observe-seconds` is an upper bound | Tray or control-plane stop requests still end the session earlier. |
| Control plane only while Running | The named pipe endpoint is created after `Running` (and after any `INTERNAL` validation batches) and disposed before `CloseAsync`; clients get `OL_E_NO_ACTIVE_SESSION` at other times. |
| Exit code | 0 when the final state is `Completed`, 1 when it is `Failed` or gameplay was not reached, 8 when a requested recovery did not complete ([exit codes](../reference/exit-codes.md)). |
| Diagnostics | Host trace and runtime-operation artefacts under `<root>\.omsilaunch\diagnostics`, tray log `tray-host.log`; no data leaves the machine. |

Integrators writing their own owner must reproduce the first two guarantees: one `StartSessionAsync` per installation at a time, and `CloseAsync` on every path.

## Failure map

| Phase | State when failing | Diagnostics you will see |
| --- | --- | --- |
| Plan | none (no session) | `OL_E_PLAN_NOT_RUNNABLE` thrown by `StartSessionAsync`; the plan's own `OL_E_` codes ([LaunchSpec validation](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)). |
| Start (lease to process creation) | `Failed` | `OL_E_START_SESSION` with the inner code; possibly `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_RESTORE_FAILED`. |
| Plugin bootstrap | `Failed` | `OL_E_PLUGIN_NOT_LOADED`, `OL_E_PLUGIN_PROTOCOL_MISMATCH`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_BUILD_VALIDATION_FAILED`, `OL_E_HEADLESS_ARM_FAILED`. |
| World start | `Failed` | `OL_E_WORLD_START_FAILED`, `OL_E_SITUATION_LOAD_FAILED`, `OL_E_STARTUP_TIMEOUT`, `OL_E_PROCESS_EXITED_EARLY`. |
| Running | `Failed` only on supervisor faults | `OL_E_PROCESS_SUPERVISION`; runtime operation errors never fail the session. |
| Termination and restore | `Failed` | `OL_E_RESTORE_FAILED`, `OL_E_RESTORE_DEFERRED`, `OL_E_PROCESS_CLEANUP_FAILED`. |

Every failure path still attempts termination and restore; a journal that remains is recovered on the next start or by `RecoverPendingAsync` / `/recover` ([transactions and recovery](transactions-and-recovery.md)).
