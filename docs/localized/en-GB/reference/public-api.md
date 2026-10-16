# Public API reference (`OmsiLaunch.Api`)

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> British English edition of the [canonical page](../../../reference/public-api.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page is the normative reference for the managed public API of OmsiLaunch 0.1.0-beta4: the `OmsiLaunch.Api` assembly (contracts) and the integrator entry point `OmsiLaunchService` in `OmsiLaunch.Core`. It documents only what the current code does. Everything an integrator can call, receive, or observe is listed here with its stability level; anything not listed is not an integration surface.

The generated [public API inventory](public-api-inventory.md) lists every public type and member of `OmsiLaunch.Api`, `OmsiLaunch.Core` and `OmsiLaunch.Process` with its signature and stability; a documentation gate fails when the inventory and the assemblies differ. This page explains the semantics.

Related pages: [LaunchSpec reference](launchspec.md), [error codes](errors.md), [session lifecycle](../concepts/session-lifecycle.md), [transactions and recovery](../concepts/transactions-and-recovery.md), [runtime control](runtime-control.md), [capabilities](capabilities.md), [local control plane](local-control.md), [exit codes](exit-codes.md), [runtime validation status](../status/runtime-validation-status.md).

## Stability vocabulary

| Level | Meaning on this page |
| --- | --- |
| `STABLE_BETA` | Contract is frozen for the 0.1 protocol line and the path is runtime validated in `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`. |
| `EXPERIMENTAL` | Callable and tested, but the contract or the runtime evidence may change before it becomes stable. |
| `PARTIAL` | Present in the contract; only part of the behaviour is implemented or validated (the text says which part). |
| `INTERNAL` | Public in the assembly for technical reasons (the bridge shares the type) but not an integration surface; may change without notice. |
| `UNAVAILABLE` | Present in the contract but rejected by the current build. |

## Assembly overview

| Assembly | Role for integrators |
| --- | --- |
| `OmsiLaunch.Api` | Pure contracts: records, enums, `IOmsiLaunch`, capability registry, error catalogue, wire formats, D3D helpers. Contains no `IntPtr`, `nint`, Win32 handle, native address or process object. |
| `OmsiLaunch.Core` | `OmsiLaunchService` (the `IOmsiLaunch` implementation), `OmsiLaunchRuntimePaths`, `SessionPlanner`, `LaunchValidation`, `SessionProfileCompiler`. |
| `OmsiLaunch.Process` | `IRuntimePlatform` and `CurrentWindowsX64Platform` (the only platform adapter), `InstallationLease`. Needed to construct the service. |
| `OmsiLaunch.Configuration`, `OmsiLaunch.Content`, `OmsiLaunch.Interop`, `OmsiLaunch.Plugin`, `OmsiLaunch.Builds.Omsi23004` | Implementation assemblies. Their public types are `INTERNAL` for integrators. |

## Entry point: `OmsiLaunchService` and `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| Parameter | Valid value | Invalid / default |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()` (namespace `OmsiLaunch.Process`). It detects the platform, creates the OMSI process with `CreateProcessW`, waits on and terminates it. | No other implementation ships. A custom `IRuntimePlatform` is `INTERNAL`. |
| `PluginBuildDirectory` | Directory that contains the permanent plugin closure reference files: `OmsiLaunch.Plugin.opl`, `OmsiLaunch.PluginNE.dll`, `OmsiLaunch.Plugin.deps.json`, `OmsiLaunch.Plugin.runtimeconfig.json` and every `OmsiLaunch.*.dll` of the managed closure (must include `OmsiLaunch.Plugin.dll`). In an installed package this is `<package>\plugins`. | Missing directory or file: `PlanSessionAsync` returns a non-runnable plan with `OL_E_RUNTIME_ARTIFACT_MISSING`. |
| `NativeBridgePath` | Path of `OmsiLaunch.Native.x86.dll` (in the package: `<package>\plugins\OmsiLaunch.Native.x86.dll`). | Same as above. |
| `ReleaseManifestPath` | `release-manifest.json` beside `OmsiLaunch.exe` when present. Supplies the expected SHA-256 of each `plugins/` file (`plugin.integrity.reference = manifest`). | `null` (development layout): the installed files are only checked for presence and self-consistency against the reference closure (`plugin.integrity.reference = self`). Malformed manifest: `OL_E_RELEASE_MANIFEST_INVALID`. |

The service reads these paths on every `PlanSessionAsync` and `StartSessionAsync`; it never copies, stages or removes plugin files (see [permanent plugin](../concepts/permanent-plugin.md)). The CLI constructs the service exactly like this (`tools/OmsiLaunch.Cli/Program.cs`):

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

Create one service per process and share it. Stability: `STABLE_BETA`.

## Session ownership rules

| Rule | Detail |
| --- | --- |
| One owner per installation | `StartSessionAsync` acquires the installation lease, a named semaphore `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`, and holds it until the supervisor has restored the installation. A second start on the same root from any process of the same logon session fails with `OL_E_INSTALLATION_BUSY` (reported as a `Failed` session, see `StartSessionAsync`). The lease is per logon session, not cross-logon, and is not released while another process holds a handle to it (accepted risk). |
| Handles are process-local | `SessionHandle` wraps the session `Guid`. It is only meaningful to the `OmsiLaunchService` instance that returned it. A handle built from a known `Guid` in another process (or another service instance) yields `KeyNotFoundException`. Cross-process control goes through the [local control plane](local-control.md), not through handles. |
| Always call `CloseAsync` | From `StartSessionAsync` onwards the process owns a durable transaction. `CloseAsync` requests the canonical stop when needed, waits for the supervisor (process exit, exact restore, lease release) and forgets the session. It must be called on every exit path, including after a `Failed` state. Without it the session entry stays in memory; the restore itself is performed by the supervisor regardless. |
| Failed sessions are still sessions | A start that fails after `StartSessionAsync` returned reports `SessionState.Failed`; the handle stays valid for `GetStatusAsync`/`WaitForAsync` until `CloseAsync`. |
| Plans are re-checked | `StartSessionAsync` re-hashes `Omsi.exe` and re-plans the spec; a plan that is no longer runnable is rejected with `OL_E_PLAN_NOT_RUNNABLE`. |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

Common facts for every method:

- Unknown or already closed handles throw `KeyNotFoundException` ("Unknown OmsiLaunch session.").
- No method requires a Running session except `ExecuteRuntimeAsync`.
- Exceptions that carry an OmsiLaunch code put the code at the start of `Exception.Message` (`"OL_E_PLAN_NOT_RUNNABLE: ..."`). The CLI extracts codes from messages the same way (`CliProgram.Classify`).
- Supported installation: OMSI 2. Native capabilities are resolved and verified per build in process; a capability unavailable on a build fails with `OL_E_CAPABILITY_UNRESOLVED`. See [compatibility](compatibility.md).

### Complete minimal example

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Purpose | Compile a `LaunchSpec` into a `SessionPlan` without starting OMSI: validate the spec, detect the platform, fingerprint `Omsi.exe`, resolve content identities, compute planned file mutations, list required and unsupported capabilities, and decide `IsRunnable`. Public capability `session.plan`. |
| Parameters | `spec`: a fully populated `LaunchSpec` (see [LaunchSpec reference](launchspec.md)). `Installation`, `World`, `Date`, `Time`, `Environment` (all eight dictionaries) and `Behavior` must be non-null; the optional members may be `null`. `RootPath` should be an absolute directory; an empty root is recorded as `OL_E_INSTALLATION_NOT_FOUND` but the platform probe on an empty path throws `ArgumentException` before the plan is returned, so never pass an empty root. |
| Returns | `SessionPlan` with a fresh `SessionId`, `BuildProfileId` = the known layout (`"Omsi23004"`) or `"unrecognized"` for an OMSI 2 build without a known layout, the input `Spec`, `Platform`, `ResolvedContent`, `TouchedFiles`, `RuntimeArtifacts` (`plugins\OmsiLaunch.*` destination paths plus `"OmsiLaunch startup handoff v6"`), `RequiredCapabilities`, `UnsupportedRequestedFeatures`, `PlannedMutations`, `Diagnostics`, `IsRunnable`, `PlannedModules` (one `PlannedModule` per requested or content-activated [runtime module](../../../reference/runtime-modules.md); accessor `EffectivePlannedModules`). `IsRunnable` is `true` exactly when no diagnostic code starts with `OL_E_`. Informational diagnostics (`plugin.integrity.reference` with message `self` or `manifest`, `session_profile.selected`) never make a plan non-runnable. |
| Result-carried errors | Every planning error is a diagnostic, not an exception: `OL_E_INSTALLATION_NOT_FOUND`, `OL_E_INSTALLATION_NOT_WRITABLE`, `OL_E_UNSUPPORTED_OPERATING_SYSTEM`, `OL_E_UNSUPPORTED_BUILD`, `OL_E_MAP_NOT_FOUND`, `OL_E_ENTRYPOINT_NOT_FOUND`, `OL_E_ENTRYPOINT_REQUIRED`, `OL_E_SITUATION_NOT_FOUND`, `OL_E_SITUATION_MAP_NOT_FOUND`, `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND`, `OL_E_DATE_TIME_APPLY_FAILED`, `OL_E_INVALID_ARGUMENT`, `OL_E_CAPABILITY_UNAVAILABLE`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_SESSION_PRESENTATION_INVALID` (message carries the splash/ITX code), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` (the installed plugin closure in `plugins\` is verified against the release manifest at planning time), `OL_E_RUNTIME_ARTIFACT_MISSING` (message may carry `OL_E_RELEASE_MANIFEST_INVALID`). Full conditions: [LaunchSpec validation rules](launchspec.md#validation-rules-and-non-runnable-diagnostics). |
| Thrown | `OperationCanceledException` if the token is already cancelled at entry (the only checkpoint); `ArgumentException`/`NotSupportedException` for syntactically invalid root paths; `NullReferenceException`/`ArgumentNullException` for null required members; `System.Text.Json.JsonException` for a syntactically invalid release manifest. |
| Cancellation | Checked once at entry. Planning is synchronous file-system work afterwards. |
| Running session required | No. |
| Mutates OMSI state | No. |
| Mutates the file system | No (reads `Omsi.exe`, content files, plugin closure, manifest). Setting values are not validated here (only key existence and writability); an invalid value fails at start with `OL_E_INVALID_SETTING_VALUE`. |
| Transaction / restore | None. |
| Limitations | Requesting any of `Date`/`Time`/`Year` mode other than `Unset`, any `Weather` mode other than `Unset`, any `PlayerVehicle` field, `Input` documents, `EntrypointIdentity`, or `WorldMode.LastMapState` produces `OL_E_CAPABILITY_UNAVAILABLE` and a non-runnable plan on this build (entries in `UnsupportedRequestedFeatures` carrying their catalogue evidence, `UNAVAILABLE` or `PARTIAL`). |
| Stability | `STABLE_BETA`. |
| Example | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Purpose | Start a transactional managed OMSI session from a runnable plan: acquire the installation lease, recover a stale journal, validate the permanent plugin closure, snapshot and overlay session files, create the startup handoff, telemetry slot and runtime mailbox, start `Omsi.exe`, record the process in the journal, and hand the session to a background supervisor. Public capability `session.start`. |
| Parameters | `plan`: a `SessionPlan` with `IsRunnable == true`. The spec inside the plan is re-planned; only `plan.SessionId` is kept from the caller's plan. `plan.Spec.Behavior.StartupTimeoutSeconds` must be 1..600. |
| Returns | `SessionHandle(plan.SessionId)` as soon as `Omsi.exe` has been created and recorded (state `WaitingForPlugin`), or as soon as the start path failed (state `Failed`). It does not wait for gameplay; use `WaitForAsync(session, SessionState.Running, ...)`. |
| Thrown | `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")` when `plan.IsRunnable` is false; `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")` when the re-plan is not runnable (for example `Omsi.exe` changed, content removed, plugin closure missing); `InvalidOperationException("Duplicate session id.")` when a session with the same id is still registered (call `CloseAsync` first); `ArgumentOutOfRangeException` when `StartupTimeoutSeconds` is outside 1..600; `OperationCanceledException` when cancelled before or during the re-plan; plus everything `PlanSessionAsync` throws. In all thrown cases no session is registered. |
| Result-carried errors | Any failure after the re-plan is caught inside the start path: the session is registered, its state is `Failed`, and its diagnostics contain `OL_E_START_SESSION` whose message is the inner message (starting with the inner code when there is one): `OL_E_INSTALLATION_BUSY` (lease held or a journalled OMSI process still alive), `OL_E_PERMANENT_PLUGIN_MISSING`, `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`, `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`, `OL_E_RELEASE_MANIFEST_INVALID`, `OL_E_SPLASH_ASSET_MISSING`, `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`, `OL_E_SPLASH_FORMAT_UNSUPPORTED`, `OL_E_ITX_PROFILE_REQUIRED`, `OL_E_ITX_PROFILE_MISSING`, `OL_E_ITX_PROFILE_INVALID`, `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`, `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE`, `OL_E_INVALID_SETTING_VALUE`, `OL_E_CLOSECHECK_REMOVE_FAILED`, `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (only when the deferred retry with this session's overlays still cannot prove ownership), `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`. Cleanup may add `OL_E_PROCESS_CLEANUP_FAILED`, `OL_E_RESTORE_DEFERRED` (OMSI exit not confirmed; journal retained) or `OL_E_RESTORE_FAILED`. Later failures are reported by the supervisor (see [session lifecycle](../concepts/session-lifecycle.md)). |
| Cancellation | Before/during the re-plan: throws. After that the token is passed to the transaction and process creation; a cancellation there is treated like any start failure (`Failed` + `OL_E_START_SESSION: The operation was canceled.`), the process (if created) is terminated and the installation restored. |
| Running session required | No. |
| Mutates OMSI state | Yes: creates the OMSI process with the environment variables `OMSILAUNCH_SESSION_ID`, `OMSILAUNCH_HANDOFF_NAME`, `OMSILAUNCH_TELEMETRY_NAME`, `OMSILAUNCH_RUNTIME_CHANNEL`, `OMSILAUNCH_INTERNET_TEXTURES_MODE`. |
| Mutates the file system | Yes, inside the installation root: `.omsilaunch\diagnostics\<sessionId>-host.log` (retention: 50 newest sessions), `.omsilaunch\journal.json`, `.omsilaunch\backup\<sessionId>\*.bin`, `.omsilaunch\assets\splash\*.bmp` (copied once for managed splash), session overlays (`options.cfg` patches, `GUI\NewSplashscreen_*.bmp`, `Texture\standard.itx`), session deletions (ITX targets, `Texture\standard.ipr`, `closecheck`), and permanent removal of a pre-existing stale `closecheck` when `SuppressStaleClosecheckWarning` is true (diagnostic `closecheck.stale-removed`). |
| Transaction / restore | Opens the transaction (`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`). Every path out of the session ends in restore. See [transactions and recovery](../concepts/transactions-and-recovery.md). |
| Limitations | Only `WorldMode.NewMap` with `PresentedEntrypointIndex` and `WorldMode.SavedSituation` reach gameplay. `WorldMode.LastMapState` is `UNAVAILABLE`. Player-vehicle requests never reach this method because they are non-runnable at plan time. |
| Stability | `STABLE_BETA` (NEW_MAP and SAVED_SITUATION lifecycles are runtime validated). |
| Example | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Purpose | Read the semantic lifecycle state, the diagnostics collected so far and the bounded runtime event list. Public capability `session.status`. |
| Parameters | `session`: a handle returned by `StartSessionAsync` and not yet closed. |
| Returns | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: an immutable snapshot (arrays are copied under the session lock). `RuntimeEvents` is never `null` for a live session. |
| Thrown | `KeyNotFoundException` for unknown/closed handles. Never throws otherwise. |
| Cancellation | The token is ignored (the call completes synchronously). |
| Running session required | No. |
| Mutates OMSI / file system / transaction | No / No / None. |
| Stability | `STABLE_BETA`. |
| Example | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Purpose | Poll (every 100 ms) until the session is in `state`, or in a terminal state (`Completed`, `Failed`), or the timeout elapses; then return the current status. |
| Parameters | `state`: any `SessionState`. Waiting for a transient state that was already passed (or is never set, see [session lifecycle](../concepts/session-lifecycle.md)) waits until a terminal state or the timeout. `timeout`: any non-negative `TimeSpan` or `Timeout.InfiniteTimeSpan`. |
| Returns | The status at the moment the wait ended. On timeout the status is returned, not an exception: check `State` yourself. A wait for `Running` that ends in `Failed` returns immediately with the failure diagnostics. |
| Thrown | `KeyNotFoundException`; `OperationCanceledException` when the caller's token is cancelled (only caller cancellation propagates; the internal timeout does not). |
| Cancellation | Caller token honoured at every 100 ms tick. |
| Running session required | No. |
| Mutates OMSI / file system / transaction | No / No / None. |
| Stability | `STABLE_BETA`. |
| Example | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Purpose | Request the canonical stop. It sets the stop flag and returns immediately; the supervisor observes the flag within its 100 ms loop, calls `TerminateProcess` on `Omsi.exe`, waits for exit, marks the journal `ProcessExited`, restores every session-owned file and releases the lease. This is a forced termination: OMSI's own shutdown routine does not run and OMSI does not rewrite `options.cfg` on exit (deliberate, protects the transaction). Cooperative `WM_CLOSE` shutdown is not implemented (product decision; in the runtime closure OMSI did not close within 30 s of `WM_CLOSE`, `L05b`). Public capability `session.stop`. |
| Parameters | `session`. |
| Returns | Completed task; does not wait for termination or restore. Use `WaitForAsync(session, SessionState.Completed, ...)` to observe completion. |
| Thrown | `KeyNotFoundException`. |
| Cancellation | Token ignored. |
| Running session required | No. Idempotent; a stop requested before the supervisor starts is honoured as soon as it starts; a stop on a terminal session is a no-op. |
| Mutates OMSI state | Yes: terminates the OMSI process (exit code 1). |
| Mutates the file system | Indirectly: triggers restore, journal deletion and backup removal by the supervisor. |
| Transaction / restore | Triggers `ProcessExited` → `Restoring` → `Restored`. Runtime-side changes made through `ExecuteRuntimeAsync` (clock writes, spawned vehicles, script variables, D3D textures) are not restored; they disappear with the process. |
| Stability | `STABLE_BETA`. |
| Example | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Purpose | Release the consumer handle without stranding the transaction: if the session is not terminal, request the canonical stop; then await the supervisor's lifecycle task (process exit, restore, lease release); then forget the session. |
| Parameters | `session`. |
| Returns | Completes when the session is terminal and removed. After it returns, the handle is unknown (`KeyNotFoundException` on any further call, including a second `CloseAsync`). |
| Thrown | `KeyNotFoundException`; `OperationCanceledException` if the caller cancels while waiting for the supervisor. In that case the session is not removed and the supervisor keeps running; call `CloseAsync` again. |
| Cancellation | Applies only to the wait; it never cancels the restore. |
| Running session required | No. |
| Mutates OMSI state | Yes when the session is still live (same as `StopAsync`). |
| Mutates the file system | Indirectly (restore by the supervisor). |
| Transaction / restore | Guarantees the transaction is driven to completion before the handle is released (when the supervisor was started). For a session that failed before the supervisor started, the start path has already restored or reported `OL_E_RESTORE_DEFERRED`. |
| Stability | `STABLE_BETA`. |
| Example | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Purpose | Execute one public runtime operation inside the running OMSI process through the single-flight session mailbox (memory-mapped, 64 KiB, request bound to session id and request id). The plugin executes the operation on OMSI's UI thread. Operation catalogue: [runtime control](runtime-control.md) and [capabilities](capabilities.md). |
| Parameters | `command.SessionId` must equal `session.SessionId`. `command.RequestId`: caller-chosen `ulong`; use a strictly increasing counter per process (the D3D helpers start at 30 000, the CLI owner at 10 001/50 000). `command.Operation`: a public operation id from `PublicCapabilityRegistry.PublicRuntimeOperationIds` (for example `time.read`, `road-vehicle.read`, `d3d.texture.create`). `command.Arguments`: string values keyed by ordinal names; required names per operation come from `PublicCapabilityRegistry.GetRuntimeArguments`. `timeout`: measured from the moment the request is staged in the mailbox (queueing behind another in-flight command is not counted). The CLI uses 5 s (15 s for `road-vehicles.spawn`) as owner and 8 s / 30 s as client. |
| Order of checks | 1. Registry validation (before session lookup): unknown or `internal.*` operation → result `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; missing required argument (absent or whitespace) → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. 2. Session lookup → `KeyNotFoundException`. 3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`. 4. State not `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`. 5. Mailbox request. 6. Result values whose key starts with `internal_` or ends with `_address`, `_pointer`, `_vmt` are stripped. |
| Returns | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`. On success `Values` holds the operation's semantic strings (documented per operation in [runtime control](runtime-control.md)). |
| Result-carried errors | `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (registry); `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (plugin result exceeded the mailbox; bounded list results are shortened with `truncated=true` instead); `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` (`weather.set` with a derived key); every `OL_E_D3D_*` code (with `Values["detail"]` and `Values["native_status"]`); `OL_E_CAPABILITY_UNRESOLVED` (the operation's native capability is not resolved on the running build); and every plugin-side typed failure with its own code in `ErrorCode` and the message in `Values["detail"]` (Beta 4; only a failure without a catalogued code is `OL_E_RUNTIME_OPERATION_FAILED`). Codes that arrive this way: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` (plugin-side checks), `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`, `OL_E_RUNTIME_VALUE_INVALID`, `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`, `OL_E_RUNTIME_OBJECT_HANDLE_STALE`, `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`, `OL_E_RUNTIME_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_VARIABLE_UNAVAILABLE`, `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`, `OL_E_RUNTIME_CONSTANT_NOT_FOUND`, `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`, `OL_E_RUNTIME_CURVE_NOT_FOUND`, `OL_E_RUNTIME_CURVE_EMPTY`, `OL_E_RUNTIME_CURVE_DEGENERATE`, `OL_E_RUNTIME_CURVE_INVALID`, `OL_E_RUNTIME_HOF_UNAVAILABLE`, `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`, `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`, `OL_E_TIME_APPLY_FAILED`, `OL_E_RUNTIME_BUS_IDENTITY_INVALID`, `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`, `OL_E_MAKEVEHICLE_DELTA_ZERO`, `OL_E_MAKEVEHICLE_DELTA_MULTIPLE`, `OL_E_MAKEVEHICLE_NATIVE_FAILED`, `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`, `OL_E_RUNTIME_CREATED_OBJECT_INVALID`, `OL_E_PLACE_RANDOM_BUS_FAILED`, `OL_E_RUNTIME_SETTING_UNAVAILABLE`. See [error codes](errors.md). |
| Thrown | `KeyNotFoundException`; `InvalidOperationException` with `OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_CLOSED` (mailbox already disposed by the supervisor), `OL_E_RUNTIME_CHANNEL_BUSY` (slot still holds an abandoned request), `OL_E_RUNTIME_REQUEST_ID_REUSED` (a stale response for the same request id is still in the slot); `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`; `InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")` (corrupt, foreign or mismatched response); `ArgumentOutOfRangeException` when the serialised request exceeds the mailbox; `OperationCanceledException`. |
| Cancellation | Honoured while waiting for the per-session gate and every 20 ms while polling for the response. Cancelling mid-flight does not reset the slot: the next call on that session may fail with `OL_E_RUNTIME_CHANNEL_BUSY` until the plugin publishes its response (which is then discarded as stale). Prefer the timeout; a timeout resets the slot and a late response is detected and discarded. |
| Running session required | Yes (`SessionState.Running`); otherwise `OL_E_SESSION_NOT_RUNNING` is thrown. The mailbox exists until the supervisor disposes it during restore. |
| Mutates OMSI state | Depends on the operation: `Read` operations do not; `Write`/`Action` operations (`time.set`, `camera.set`, `camera.lock`, `camera.unlock`, `road-vehicles.spawn`, `road-vehicles.place-random`, `vehicle.variable.set`, `d3d.texture.*`) mutate in-process state that is not restored. |
| Mutates the file system | No host writes. OMSI may write its own files as a consequence (not tracked). |
| Transaction / restore | None. |
| Limitations | One in-flight command per session (calls on the same session are serialised). Request and response are each limited to 64 KiB minus 8 bytes; D3D pixel payloads to 48 KiB. `internal.road-vehicles.make-basic` is `INTERNAL` and unreachable. `weather.set` and `weather.metar.set` are `EXPERIMENTAL`. `timetable.logs.read` is not bounded and can return `OL_E_RUNTIME_RESPONSE_TOO_LARGE` on large timetables. `camera.lock` is `EXPERIMENTAL`; it needs a player vehicle and is runtime-validated (`CAM01`), although the registry's `RuntimeValidation` string still reads `STATICALLY_VALIDATED`. Handles (`rv-NNNNNN`, `hb-NNNNNN`, `d3dtex-<session>-<hex>`) are session-scoped. |
| Stability | Transport and contract `STABLE_BETA`; per-operation stability follows `PublicCapabilityRegistry` (`PublicStableBeta` → `STABLE_BETA`, `PublicExperimental` → `EXPERIMENTAL`) with the exceptions above. |
| Example | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Purpose | Return the capability catalogue evaluated for an installation: one `Capability(Name, Available, EvidenceState, Reason)` per `PublicCapabilityRegistry` entry that is not `InternalOnly`. `Name` is the catalogue id, `EvidenceState` the catalogue evidence (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`). `Available` is computed from the platform, the OMSI 2 check, the classification and the native capabilities the entry needs (see [capabilities](capabilities.md#getcapabilitiesasync)). |
| Parameters | `installation.RootPath`: the installation probed (platform, `Omsi.exe`). `ExpectedExecutableSha256` is ignored. |
| Returns | One entry per non-internal catalogue entry, for example `time.read` (`RUNTIME_VALIDATED`, `Reason` `native: RESOLVED_KNOWN_LAYOUT` on the known layout), `world.last-map-state` (`false`, `UNAVAILABLE`). |
| Relation to `PublicCapabilityRegistry` | Derived from it: the registry declares the capabilities, their operations and their evidence; this call evaluates them for one installation. |
| Thrown | `OperationCanceledException` at entry; `ArgumentException` for an empty root path. |
| Cancellation | Checked once at entry. |
| Running session required | No. |
| Mutates OMSI / file system / transaction | No / No / None. |
| Stability | Call contract `STABLE_BETA`; the list content is a hand-maintained inventory: `PARTIAL`. |
| Example | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Purpose | Enumerate installed content and return canonical identities usable in a `LaunchSpec`. Discovery skips reparse points (junction cycles cannot hang it), reads OMSI files as Windows-1252 (BOM-marked UTF-8/UTF-16 respected) and never follows symbolic links. |
| Parameters | `query` and `scope` per the table below. `scope` is required for `Entrypoints` (map identity), `Repaints`, `FleetNumbers`, `Registrations` (vehicle identity). |
| Returns | Sorted `ContentIdentity(Identity, Kind, DisplayName)` list. Identities are installation-relative paths with backslashes; comparisons are case-insensitive. |
| Thrown | `OperationCanceledException` at entry; `ArgumentException` when `Entrypoints` is queried without a scope or the root is empty; `FileNotFoundException` (no `OL_E_` code; the CLI maps it to `OL_E_NOT_FOUND`) when the scope map or vehicle is not installed. A missing root or content directory yields an empty list, not an error. |
| Cancellation | Checked once at entry. |
| Running session required | No. |
| Mutates OMSI / file system / transaction | No / No / None. |
| Stability | `Maps`, `Situations`, `Vehicles`: `STABLE_BETA` (every runtime-validated plan resolves through them). `Entrypoints`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`: `EXPERIMENTAL` (static evidence only). |
| Example | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

`ContentQueryKind` values and results:

| Value | Scope | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | none | `maps\<dir>\global.cfg` | `map` | map directory name |
| `Situations` | none | `situations\...\<file>.osn` | `situation` | map identity referenced by the `.osn` (may be `null`) |
| `Vehicles` | none | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` or file name |
| `Repaints` | vehicle identity (required; without it: empty list) | `<cti path>#item:<ordinal>` | `repaint` | `[item]` name |
| `Hofs` | none | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | vehicle identity (required; without it: empty list) | vehicle-relative `[number]` source path | `fleet-number` | `null` |
| `Registrations` | vehicle identity (required; without it: empty list) | `registration_automatic` / `registration_list` / `registration_free` | `registration` | first value line (`null` for free) |
| `Addons` | none | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | map identity (required; without it: `ArgumentException`) | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | entrypoint label |
| `SessionProfiles` | none | package directory name (the `/predefined-profile` id) | `session-profile`, or `session-profile-invalid` when the package does not compile | profile `name`, or the `OL_E_SESSION_PROFILE_*` code of an invalid package |
| `RuntimeModules` | none | module id | `runtime-module`, or `runtime-module-invalid` when it cannot be loaded | module `name`, or the `OL_E_MODULE_*` code |

An entrypoint is selected by `PresentedEntrypointIndex` or by `EntrypointIdentity` (a discovery identity or an exact label). The planner resolves an identity to the label OMSI presents (OMSI presents only the first record of each exact label) and the plan lists it under `ResolvedContent` with kind `entrypoint`; `world.entrypoint-identity` is `EXPERIMENTAL` until runtime validated.

### `ListSessionProfilesAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Purpose | List every session-profile package under `<root>\.omsilaunch\session-profiles\` with its presets, so a frontend can offer them without parsing YAML. CLI `/list:SessionProfiles`. |
| Returns | One `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)` per package directory, sorted by `Id`. `Presets` are `SessionProfilePresetSummary(Index, Id, Name)` in index order. Each package is compiled with the same strict rules as a launch (`SessionProfileCompiler`); a package that does not compile is listed with `IsValid = false`, its `OL_E_SESSION_PROFILE_*` code and message, and no presets. No profiles directory: empty list. |
| Thrown | `OperationCanceledException` at entry. Compilation errors are reported in the result, not thrown. |
| Running session required | No. |
| Mutates OMSI / file system / transaction | No / No / None. |
| Stability | `STABLE_BETA` (offline gate `session-profiles.discovery`). |
| Example | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Purpose | Discover the [runtime modules](../../../reference/runtime-modules.md) installed below `plugins\OmsiLaunch.Modules` with their integrity and settings. Read only. Loading is open: no module is checked against a list or signature. |
| Returns | One `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)` per module directory, sorted by directory name. `Activation` is `requested`, `content` or `default`; `IsValid` is false with an `OL_E_MODULE_*` code when the module cannot be loaded; `Settings` lists the settings the module declares as `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`. |
| Thrown | `OperationCanceledException` at entry. |
| Running session required | No. CLI: `/list:RuntimeModules` (envelope `runtime-modules.list`). |

### `RecoverPendingAsync`

| Aspect | Detail |
| --- | --- |
| Signature | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Purpose | Report or complete a stale durable transaction (`<root>\.omsilaunch\journal.json`) left by a crashed owner. Takes the installation lease for the duration of the call so it never restores underneath a starting session. Public capability `session.recover`; CLI `/recovery-status` and `/recover`. |
| Parameters | `installation.RootPath`: the installation root (normalised with `Path.GetFullPath`). `restore`: `false` = report only; `true` = restore, verify, delete the journal and backups. |
| Returns | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = a journal existed when the call started; `Recovered` = a restore was requested, ran, and no journal remains; `Diagnostics` = restore notes (`restore.session-artifact-removed` with `Data["sha256"]`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`), empty when nothing was restored. |
| Thrown | `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")` when the lease is held; `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")` when the journal's PID + creation time + executable path still match a live process, or (journal past `HandoffCreated` without a PID) any `Omsi.exe` from that root is running; `IOException` with `OL_E_RECOVERY_BACKUP_CORRUPT`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`, `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`, `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`, or a verification message ("Restore hash mismatch: ...", "Restore presence mismatch: ..."); `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException` for a corrupt journal; `ArgumentException` for an empty root; `OperationCanceledException`. Whenever it throws after a restore started, the journal is retained and the next call replays idempotently. |
| Cancellation | Passed to the journal/backup writes; cancelling mid-restore leaves the journal pending. |
| Running session required | No (it refuses while an owner is active). |
| Mutates OMSI state | No. |
| Mutates the file system | Only with `restore == true`: rewrites originals from verified backups (bytes, last-write time, creation time, attributes; read-only originals handled; write-through + flush; no `*.omsilaunch.tmp` left), removes session artefacts, deletes `journal.json` and `backup\<sessionId>`. |
| Transaction / restore | Completes the pending transaction (`Restoring` → `Restored` → journal removed). |
| Stability | `STABLE_BETA`: report path and early-exit recovery (matrix RV-008), recovery after a failed restore (runtime closure `F01`), refusal under a live owner and in the pre-PID window (`S04`) and deferred pre-fingerprint recovery at start (`S05`); see [runtime validation status](../status/runtime-validation-status.md). |
| Example | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

## Contract types

### Optional values and semantic primitives

| Type | Definition | Notes |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`; `IsSet`, static `Unset`, static `Set(T)` | Distinguishes "not requested" from "requested with a value". JSON shape is documented in the [LaunchSpec reference](launchspec.md). |
| `Presence` | `Unset` = 0, `Set` = 1 | Byte enum. |
| `SemanticDate` | `(int Year, int Month, int Day)` | Validated only when `DateTimeMode.Explicit` (month 1..12, day 1..31). |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | Validated only when `DateTimeMode.Explicit` (0..23, 0..59, 0..59). |

### LaunchSpec family

All records below are documented property by property in the [LaunchSpec reference](launchspec.md); this table fixes the type inventory.

| Type | Purpose | Stability |
| --- | --- | --- |
| `LaunchSpec` | Root request record with `EffectiveYear`, `EffectiveWeather`, `EffectiveInput`, `EffectiveDiagnostics`, `EffectivePresentation`, `EffectiveInternetTextures` accessors that substitute defaults for `null` optional members. | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`, `ExpectedExecutableSha256` (optional pin of `Omsi.exe`, checked when planning). | `STABLE_BETA` |
| `WorldSpec`, `WorldMode`, `EntrypointSpec`, `EntrypointMode` | World selection. `WorldMode`: `NewMap` = 0, `SavedSituation` = 1, `LastMapState` = 2, `LastSituation` = 2 (obsolete alias of `LastMapState`; never "the newest .osn"). `EntrypointMode`: `Unset`, `PresentedIndex`, `Identity` (computed from `WorldSpec.Entrypoint`). | `NewMap`, `SavedSituation`: `STABLE_BETA`; `LastMapState`: `UNAVAILABLE`; `EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`, `TimeSpec`, `YearSpec`, `DateTimeMode` | `DateTimeMode`: `Unset`, `Explicit`, `System`. Any mode other than `Unset` makes the plan non-runnable. | `PARTIAL` (plan evidence `UNAVAILABLE`) |
| `WeatherSpec`, `WeatherMode` | `WeatherMode`: `Unset`, `Preset`, `Icao`, `RealCurrent`. Any mode other than `Unset` makes the plan non-runnable. | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`, `Repaint`, `Hof`, `FleetNumber`, `Registration`, `Enabled`. Any set field makes the plan non-runnable. | `PARTIAL` |
| `EnvironmentSpec` | Eight `IReadOnlyDictionary<string, OptionalValue<string>>` groups of semantic `options.cfg` settings. | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`, `ControllerDocument`; any set value makes the plan non-runnable. | `PARTIAL` |
| `DiagnosticsSpec` | Six booleans; carried, not consumed. | `PARTIAL` |
| `SessionPresentationSpec`, `SplashMode` | `SplashMode`: `Unset` = 0, `Native` = 0 (alias), `Managed` = 1. | `STABLE_BETA` |
| `InternetTexturesSpec`, `InternetTexturesMode` | `InternetTexturesMode`: `Native`, `Disabled`, `Override`. | `STABLE_BETA` (`Native`), `EXPERIMENTAL` (`Disabled`, `Override`) |
| `SessionProfileMetadata` | Provenance of a compiled session profile (`Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath`). | `STABLE_BETA` |
| `HofManagementSpec` | Session [HOF management](hof-management.md): `Operations` (list of `HofOperationSpec`). Field `LaunchSpec.HofManagement`; accessor `EffectiveHofManagement`. | `STABLE_BETA` |
| `RuntimeModulesSpec` | [Runtime modules](../../../reference/runtime-modules.md) for the session: `Enable` (module ids), `Disable` (module ids kept out), `Settings` (`"<module id>.<key>"` to value); accessors `EffectiveDisable`, `EffectiveSettings`. Field `LaunchSpec.Modules`; accessor `EffectiveModules`. | `EXPERIMENTAL` |
| `PlannedModule` | A module's plan evaluation: `Id`, `Version`, `Status` (`ELIGIBLE` or `REJECTED`), `Requested`, `FailurePolicy`, `Operations` (`module.<id>.<operation>`), `Code`, `Reason`, `ManifestSha256`, `Activation` (`requested`, `settings-file`, `content` or `default`), `Settings`; `IsEligible`, `EffectiveSettings`. Constants `Eligible`, `Rejected`, `ActivationRequested`, `ActivationSettingsFile`, `ActivationContent`, `ActivationDefault`. | `EXPERIMENTAL` |
| `PlannedModuleSetting` | One effective module setting: `Key`, `Value`, `Source` (`request`, `settings-file` or `default`). | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | Discovery view of an installed module; see `ListRuntimeModulesAsync`. | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | A setting a module declares: `Key`, `Type`, `Default`, `AllowsAuto`, `Minimum`, `Maximum`, `Choices`, `Flag`, `Description`. | `EXPERIMENTAL` |
| `HofOperationSpec` | One HOF operation (`SourceKind`, `Source`, `TargetVehicles`, `Mode`, `Files`). | `STABLE_BETA` |
| `HofSourceKind` | `Collection`, `Directory`, `Vehicle`, `File`. | `STABLE_BETA` |
| `HofApplyMode` | `Add`, `Mirror`. | `STABLE_BETA` |
| `HofPlanningException` | Thrown by HOF planning (`OmsiLaunch.Core`); `Code` is an `OL_E_HOF_*` code. The public API reports it as plan diagnostics. | `STABLE_BETA` |
| `SessionProfileSummary` | Discovery view of one package (`Id`, `Name`, `Version`, `Author`, `CompatibleMaps`, `MapIdentity`, `Presets`, `IsValid`, `ErrorCode`, `ErrorMessage`). See `ListSessionProfilesAsync`. | `STABLE_BETA` |
| `SessionProfilePresetSummary` | One preset in a summary (`Index`, `Id`, `Name`). | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration` (carried, restore always happens), `SuppressStaleClosecheckWarning`, `StartupTimeoutSeconds` (1..600, default 180), `ShutdownTimeoutSeconds` (carried, not consumed). | `STABLE_BETA` / `PARTIAL` |

### Plan and status types

| Type | Fields | Notes |
| --- | --- | --- |
| `SessionPlan` | `SessionId` (new `Guid` per plan), `BuildProfileId` (`"Omsi23004"` or `"unrecognized"`), `Spec`, `Platform` (`RuntimePlatformInfo`), `ResolvedContent` (`ContentIdentity` list: `map`, `vehicle`, `repaint`, `hof`, `situation`, `situation-map`), `TouchedFiles` (distinct relative paths of `PlannedMutations`), `RuntimeArtifacts`, `RequiredCapabilities` (`Capability` with `STATICALLY_VALIDATED` or `UNAVAILABLE`; native start capabilities with `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` or `PENDING_IN_PROCESS_RESOLUTION`), `UnsupportedRequestedFeatures` (`Capability` entries for requested-but-unsupported features), `PlannedMutations`, `Diagnostics`, `IsRunnable`. | A public record: it can be edited or go stale, which is why `StartSessionAsync` re-plans. |
| `RuntimePlatformInfo` | `OsFamily`, `OsVersion`, `OsArchitecture`, `HostArchitecture`, `OmsiArchitecture` (`X86`), `PluginArchitecture` (`X86`), `CurrentPlatformSupported` (Windows 10+, x64 OS and x64 host), `LegacyPlatform` (always `false`), `Wow64Available`, `InstallationWritable`, `ProcessLaunchSupported`, `PluginRuntimeSupported`, `NativeInteropSupported`, `SharedMemorySupported`, `ExactRestoreSupported` (all equal to `CurrentPlatformSupported`). | |
| `Capability` | `Name`, `Available`, `EvidenceState`, `Reason`. | `EvidenceState` is a `PublicEvidence` value (`RUNTIME_VALIDATED`, `STATICALLY_VALIDATED`, `PARTIAL`, `RUNTIME_VALIDATION_REQUIRED`, `UNAVAILABLE`), except in `SessionPlan.RequiredCapabilities`: a platform or content requirement is `STATICALLY_VALIDATED` or `UNAVAILABLE`, a native capability `RESOLVED_KNOWN_LAYOUT`, `STATICALLY_RESOLVED` or `PENDING_IN_PROCESS_RESOLUTION`. |
| `PlannedMutation` | `RelativePath`, `SemanticKey`, `RequestedValue`, `Operation` (`token-patch`, `vector-component-patch`, `exact-file-overlay`). | Presentation mutations use the keys `session-presentation.splash`, `internet-textures.override`, `internet-textures.cache`, `internet-textures.target`. |
| `LaunchDiagnostic` | `Code`, `Message`, `Data` (optional string map). | Codes starting with `OL_E_` are errors, `OL_W_` warnings, anything else informational. |
| `SessionStatus` | `SessionId`, `State` (`SessionState`), `Diagnostics`, `RuntimeEvents`. | Session diagnostics do not include plan diagnostics. |
| `RuntimeEvent` | `Type`, `TimestampUtc` (host receipt time), `Sequence` (1-based, per session), `Data`, `SessionTickMilliseconds` (the plugin's monotonic session clock when it emitted the event; null for host events). | Bounded to the 256 most recent events (oldest dropped). Plugin events arrive in order through the telemetry ring; events lost to a ring overrun are counted by `telemetry.lost`. |
| `SessionHandle` | `SessionId`. | Process-local. |
| `RecoveryStatus` | `Pending`, `Recovered`, `Diagnostics`. | See `RecoverPendingAsync`. |
| `ContentIdentity` | `Identity`, `Kind`, `DisplayName`. | See `DiscoverAsync`. |
| `ContentQueryKind` | `Maps`, `Situations`, `Vehicles`, `Repaints`, `Hofs`, `FleetNumbers`, `Registrations`, `Addons`, `Entrypoints`, `SessionProfiles`, `RuntimeModules`. | |

### `SessionState`

Byte enum in declaration order: `Created`, `ValidatingPlatform`, `Planning`, `AcquiringInstallationLock`, `RecoveringPreviousTransaction`, `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `WaitingForPlugin`, `PluginBootstrap`, `StartingWorld`, `EnteringGameplay`, `Running`, `ProcessExited`, `Restoring`, `CleaningRuntime`, `Completed`, `Failed`. `ValidatingPlatform`, `Planning` and `EnteringGameplay` are never set by the current service; `Snapshotting` is transient and practically unobservable. Terminal states: `Completed`, `Failed`. Full semantics: [session lifecycle](../concepts/session-lifecycle.md).

### Runtime control types

| Type | Definition | Stability |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | Static codec used by the host and the plugin for the mailbox envelope: magic `0x4F4C5243` ("OLRC"), version 1, 72-byte little-endian header (magic, version, kind 1 = request / 2 = response, total length, session `Guid`, request id, payload length, SHA-256 of the payload) followed by a UTF-8 JSON payload. `SerializeRequest`, `SerializeResponse`, `TryDeserializeRequest`, `TryDeserializeResponse`, `TryReadRequestId`. | `INTERNAL`: public because both bridge ends share it; not an integration surface; the format may change with the protocol version. |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — what the host publishes to the plugin in the memory-mapped file `OmsiLaunch.Handoff.<sessionId>`. | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`: a planned module carried by the handoff (`StartupHandoff.Modules`). | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`, `IsEmpty`: the resolved start clock of a NEW_MAP session carried by the handoff (`StartupHandoff.Start`); a null field keeps OMSI's own value. | `INTERNAL` |
| `StartupHandoffWire` | Codec: magic `0x4F4C5348`, version 7 (reads 3 to 7; version 5 appends the planned modules, version 6 each module's effective settings, version 7 the start date/time; flag bits 0x04 and 0x08 carry `PluginTrace` and `NativeTrace`), 64-byte header with SHA-256 payload integrity. | `INTERNAL` |
| `TelemetryRingWire` | Plugin-to-host event ring in the mapping `OMSILAUNCH_TELEMETRY_NAME`: magic `0x52544C4F` ("OLTR"), version 1, 64 numbered slots of 2048 bytes. `Initialize`, `Write`, `IsRing`, `ReadSince` (returns the events after a sequence number and how many were lost). | `INTERNAL` |

The plugin rejects a handoff (`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`) unless `WorldMode` is `NewMap` or `SavedSituation`, `HeadlessStart` is true, `PlayerVehicleEnabled` is false, both date/time modes are `Unset`, and a saved situation has a non-empty identity. The planner enforces the same constraints earlier, so a runnable plan never triggers this.

### Capability registry types

| Type | Purpose |
| --- | --- |
| `PublicCapabilityRegistry` | The canonical capability catalogue. `ProtocolVersion` (`"0.1"`), `All` (every `PublicCapabilityDescriptor`), `Get`, `TryGet`, `PublicRuntimeOperationIds` (the 48 concrete operation ids a frontend may forward), `RuntimeOperations`, `TryGetOperation`, `NativeCapabilityOf`, `CliRoutes` (command route to operation id), `IsPublicRuntimeOperation`, `GetRuntimeArguments`, `ValidateRuntimeArguments` (returns `PublicRuntimeArgumentValidation`), `IsInternalResultKey`. Enforced by `ExecuteRuntimeAsync`, the CLI, the local control plane and the plugin. |
| `PublicCapabilityDescriptor` | `Id`, `Family`, `Classification`, `Kind`, `RequiresSession`, `RequiresExactProfile`, `ApiRoute`, `CliRoute`, `RuntimeValidation` (a `PublicEvidence` value), `Description`, `HandleTypes`, `Operations`, `NativeCapability`, `EvidenceNote`; derived `EffectiveOperations`, `NativeCapabilities`. |
| `PublicRuntimeOperation` | `Id`, `NativeCapability`, `Arguments`, `CliRoutes`, `Paged` (takes `cursor`/`limit`, see [pagination](capabilities.md#pagination)), `MutationPolicy`. |
| `PublicMutationPolicy` | What a runtime operation changes: `None`, `OmsiState`, `SessionResource`, `SessionPolicy`, `All`. |
| `PublicEvidence` | The evidence vocabulary: `RuntimeValidated`, `StaticallyValidated`, `Partial`, `RuntimeValidationRequired`, `Unavailable`, `All`. |
| `PublicCapabilityClassification` | `PublicStableBeta`, `PublicExperimental`, `InternalOnly`, `Unsupported`. |
| `PublicCapabilityKind` | `Read`, `Write`, `Action`, `Event`. |
| `PublicRuntimeArgumentDescriptor` | `Name`, `Required`, `Description`. |
| `PublicRuntimeArgumentValidation` | `Accepted`, `ErrorCode`, `Message`. |

Full catalogue: [capabilities](capabilities.md).

### `D3DRuntimeApi` extension methods

Typed wrappers over `ExecuteRuntimeAsync` for the `d3d.*` operations (`EXPERIMENTAL`, `PublicExperimental` capability `d3d.texture`). They allocate request ids from a process-wide counter starting at 30 000 and default the timeout to 5 s (except `GetD3DStatusAsync`, which requires one).

| Method | Operation | Arguments and limits |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | none |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | width/height 1..4096, levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095, width/height 1..4096, pixels ≤ 48 KiB (Base64-encoded on the wire) |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | repeated release is rejected with `OL_E_D3D_RESOURCE_RELEASED` |

Types: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`; `D3DDeviceState`: `NotReady`, `Ready`, `Lost`, `Resetting`, `Stopping`, `Stopped`; `D3DTextureHandle(Value)` with `Value = "d3dtex-<session id N>-<16 hex>"`; `D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`; `D3DTextureResourceState`: `Live`, `Released`, `Stale`; `D3DTextureFormat`: `A8R8G8B8`, `X8R8G8B8`, `R5G6B5`, `X1R5G5B5`, `A1R5G5B5`, `A4R4G4B4`, `A8`, `L8`, `A8L8`.

Errors: a failed result is rethrown as `OmsiRuntimeException(Code, detail)` where `Code` is the result's `ErrorCode` (or `OL_E_RUNTIME_OPERATION_FAILED` when absent) and the message is `"<code>: <Values["detail"]>"`; a successful result without values, or an unknown device-state string, throws `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`. Everything `ExecuteRuntimeAsync` throws propagates unchanged. Device reset handling is runtime-validated: a reset moves the device through `Resetting` back to `Ready` and invalidates every live texture (`OL_E_D3D_STALE_RESOURCE_HANDLE`, runtime closure `D01`); the catalogue entry `d3d.texture` records it. The `Lost` transition cannot be produced from outside the product and is covered offline only.

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

### Process contract types

| Type | Content |
| --- | --- |
| `PublicExitCode` | `Success` = 0, `SessionFailed` = 1, `InvalidArguments` = 2, `UnsupportedProfile` = 3, `NoActiveSession` = 4, `RuntimeUnavailable` = 5, `NotFound` = 6, `OperationRejected` = 7, `TransactionRecoveryFailed` = 8, `InternalError` = 10. Used by the CLI only ([exit codes](exit-codes.md)); the API never exits the process. |
| `PublicErrorCategory` | String constants used in CLI/control error envelopes: `invalid_argument`, `unsupported_profile`, `session`, `runtime`, `not_found`, `transaction`, `internal`. |
| `PublicErrorCodes` | One `const string` per code (143: 142 `OL_E_` errors and 1 `OL_W_` warning) and `All`, the `PublicErrorDescriptor(Code, Category)` catalogue. Categories: `Cli`, `Compatibility`, `Content`, `Installation`, `InvalidArgument`, `LaunchSpec`, `LocalControl`, `Other`, `Presentation`, `Process`, `Runtime`, `RuntimeD3D`, `Session`, `SessionProfile`, `Transaction`, `Warning`. Reference: [error codes](errors.md). |
| `PublicErrorDescriptor` | `(string Code, string Category)`. |
| `OmsiRuntimeException` | `Code` property plus message; thrown only by `D3DRuntimeApi`. |

### `InstallationPaths` (installation identity and path containment)

Stability: `STABLE_BETA` (pure functions, no I/O, no OMSI state, no filesystem mutation, no transaction participation, no Running session required). It is the single definition used by the installation lease, the local control pipe name, session-profile asset confinement, Internet Textures target validation and the runtime spawn model path.

| Member | Behaviour |
| --- | --- |
| `string NormalizeRoot(string root)` | `Path.GetFullPath(root)` without trailing separators, except a drive root (`C:\`) which is kept. Resolves `.` and `..` segments, treats `/` and `\` alike and collapses repeated separators. Does **not** resolve junctions or symbolic links. Throws `ArgumentException` for a null or blank root. |
| `string IdentityKey(string root)` | Upper-cased `NormalizeRoot(root)`. Lexically equivalent spellings of one root (`C:\OMSI`, `C:\OMSI\`, `C:\OMSI\.`, `C:\foo\..\OMSI`, `c:\omsi`) share one key; different roots (`C:\OMSI-A`, `C:\OMSI-B`) never do. |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | Resolves `candidate` (relative to `root`, or absolute) and returns `true` only when it lies strictly below `root`; `relativePath` is the canonical spelling with `\`. Uses `Path.GetRelativePath` segments, so a sibling such as `C:\OMSI-A\x` is never inside `C:\OMSI`; the root itself, other volumes and `..` escapes return `false`. |
| `IReadOnlyList<string> Segments(string relativePath)` | Splits on `/` and `\`, dropping empty segments. |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

### Session profiles (`OmsiLaunch.Core`)

Stability: `EXPERIMENTAL`. These types compile a YAML [session profile](session-profiles.md) (`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`, schema `omsilaunch.session-profile/v1`) into a `LaunchSpec`. The CLI `/predefined-profile:<id> /predefined-profile-index:<n>` uses exactly these calls; an integrator can use them to start a profile through the API.

| Member | Behaviour |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | Reads and validates the package. `id` must be a plain directory name (`OL_E_SESSION_PROFILE_PATH_ESCAPE` otherwise); `presetIndex` is 1..99 (`MinPresetIndex`..`MaxPresetIndex`; `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` otherwise). Missing file: `OL_E_SESSION_PROFILE_NOT_FOUND`; larger than `MaxBytes` (256 KiB), invalid YAML, anchors, unknown keys or an `id` that differs from the directory name: `OL_E_SESSION_PROFILE_INVALID`; other `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`. Asset paths are confined to the package (`OL_E_SESSION_PROFILE_PATH_ESCAPE`, `OL_E_SESSION_PROFILE_ASSET_MISSING`). Every failure is a `SessionProfileException`. |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | Returns `baseline` with the profile applied: for `NewMap` the profile's `new` block (map, entrypoint, and any date/time/year/weather); the preset's `settings` merged over `Environment.General`; the preset's `Presentation`, `InternetTextures` and `Behavior` when present; and `SessionProfile` = `profile.Metadata`. For `NewMap` it also checks the map against the profile's `compatibility` list (`OL_E_SESSION_PROFILE_MAP_MISMATCH`). |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | The compatibility check for the other modes (for `SavedSituation` the map is read from the `.osn`). The CLI calls it after the final `WorldSpec` is built. |
| `SessionProfileCompiler.Schema`, `MaxBytes`, `SchemaKeys` | `"omsilaunch.session-profile/v1"`, `262144`, and the accepted keys per YAML mapping. |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`, `ProfileNew`, `ProfilePreset` | The loaded package; `Preset` is the selected preset only. |
| `SessionProfileException(string code, string message)` | `IOException` with `Code` (one of the `OL_E_SESSION_PROFILE_*` codes); the message is `"<code>: <message>"`. |

The CLI additionally rejects command-line flags that conflict with the profile (`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`); that check is not part of the compiler. See [session profiles](session-profiles.md#precedence-and-override-conflicts) for the full merge order.

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

### Platform types (`OmsiLaunch.Process`)

| Type | Stability | Use |
| --- | --- | --- |
| `IRuntimePlatform` | `STABLE_BETA` as the constructor parameter type of `OmsiLaunchService` | Detects the platform, checks writability, starts, observes, terminates and waits for `Omsi.exe`. Pass `new CurrentWindowsX64Platform()`; implementing it yourself is not supported. |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | The only implementation: Windows x64 host, `CreateProcessW` for `Omsi.exe`, `TerminateProcess` for the canonical stop. Its methods are called by the service; integrators only construct it. Members (shared with `IRuntimePlatform`): `Detect(root)` returns the `RuntimePlatformInfo` of the plan; `ValidateCurrent(info)` throws `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` when the host cannot run a session; `IsInstallationWritable(root)` backs `OL_E_INSTALLATION_NOT_WRITABLE`; `StartAsync(request, sha256)` creates `Omsi.exe` and records the process identity (PID, creation time, path and the hash computed by the service; `OL_E_PROCESS_START_FAILED`, `OL_E_PROCESS_CREATION_TIME_FAILED`); `HasExited`, `WaitForExitAsync` and `Terminate` observe and end it. |
| `InstallationLease`, `LaunchedProcess`, `ProcessIdentity`, `ReleaseManifest`, `RuntimeArtifact`, `RuntimeArtifactSet`, `StartupProcessRequest`, `CurrentRuntimeCommandStore`, `IOmsiProcessController`, `OmsiProcessState` | `INTERNAL` | Public in the assembly because the service and the tests share them. Not an integration surface; `LaunchedProcess` wraps the OMSI process and thread handles internally (they are not public members) and is never returned by `IOmsiLaunch`. |

## Thread safety

- `OmsiLaunchService` is safe for concurrent calls on different sessions: sessions live in a `ConcurrentDictionary` and every per-session mutation happens under the session's private lock.
- Concurrent calls on the same session are safe but serialised where it matters: `ExecuteRuntimeAsync` takes a per-session gate, so a second command waits for the first (its timeout starts when it is staged).
- The supervisor runs on a thread-pool task (`Task.Run`) from the moment `StartSessionAsync` returns until the session is terminal; it polls telemetry and the process every 100 ms. Callers never run supervisor code.
- `StopAsync` and `GetStatusAsync` complete synchronously and may be called from any thread, including inside a `ProcessExit` handler (the CLI does this with a 4 s budget).
- No API call is thread-affine; none requires a synchronisation context.

## What is not in the API

- No `IntPtr`, `nint`, Win32 handles, native addresses, VMT pointers or process objects. Result values whose keys start with `internal_` or end with `_address`, `_pointer`, `_vmt` are removed before a result leaves `ExecuteRuntimeAsync`.
- No `internal.*` runtime operations: `internal.road-vehicles.make-basic` is `InternalOnly` in the registry and returns `OL_E_RUNTIME_OPERATION_UNKNOWN` from the API and the CLI.
- No raw OMSI memory reads or writes, no file-level access to the installation other than what a `LaunchSpec` declares.
- No cross-process handles: the [local control plane](local-control.md) is the only cross-process route, and it accepts `session.status`, `session.events`, `session.stop` and `runtime.execute` only.
- No player-vehicle application at start. Cooperative shutdown, `LAST_MAP_STATE`, start date/time/weather and keyboard/controller document overlays are `EXPERIMENTAL`.

## Stability summary

| Surface | Stability |
| --- | --- |
| `OmsiLaunchService` constructor, `OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`, `StartSessionAsync` (NEW_MAP, SAVED_SITUATION), `GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync` | `STABLE_BETA` |
| `ExecuteRuntimeAsync` transport; `PublicStableBeta` operations | `STABLE_BETA` |
| `PublicExperimental` operations, `D3DRuntimeApi`, `camera.lock` | `EXPERIMENTAL` |
| date/time/weather/input spec members, `DiagnosticsSpec`, `ExpectedExecutableSha256`, `RestoreConfiguration`, `ShutdownTimeoutSeconds`, `CooperativeShutdown` | `EXPERIMENTAL` |
| player-vehicle spec members | `PARTIAL` |
| `RuntimeCommandWire`, `StartupHandoff`, `StartupHandoffWire`, `StartupModule`, `StartDateTime`, `StartWeather`, `TelemetryRingWire`, `IRuntimePlatform` implementations, all implementation assemblies | `INTERNAL` |
| `WorldMode.LastSituation`, `internal.*` operations | `UNAVAILABLE` |
