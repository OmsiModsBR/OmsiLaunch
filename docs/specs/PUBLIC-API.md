# Public API

Status: NORMATIVE (summary; the full member-by-member reference is
[`docs/reference/public-api.md`](../reference/public-api.md))

`OmsiLaunch.Api` is the product boundary. Consumers construct a semantic
`LaunchSpec`, call `PlanSessionAsync`, then call `StartSessionAsync` only for a
runnable plan. The CLI is a reference consumer of this API and contains no
separate OMSI implementation.

`IOmsiLaunch` members: `PlanSessionAsync`, `StartSessionAsync`,
`GetStatusAsync`, `WaitForAsync`, `StopAsync`, `CloseAsync`,
`ExecuteRuntimeAsync`, `GetCapabilitiesAsync`, `DiscoverAsync`, and
`RecoverPendingAsync`.

## Boundary Rule

`ExecuteRuntimeAsync` validates the operation against
`PublicCapabilityRegistry` before it looks up the session. An unknown or
`internal.*` operation returns `Succeeded=false` with
`OL_E_RUNTIME_OPERATION_UNKNOWN`; a missing required argument returns
`OL_E_RUNTIME_ARGUMENT_REQUIRED`. Result values whose key starts with
`internal_` or ends with `_address`, `_pointer` or `_vmt` are stripped before
the result is returned. `internal.road-vehicles.make-basic` is INTERNAL and is
unreachable through the API or the CLI. `StartSessionAsync` re-hashes
`Omsi.exe` and re-plans the spec; a plan that is no longer runnable is rejected
with `OL_E_PLAN_NOT_RUNNABLE`.

## Recovery

`RecoverPendingAsync(InstallationSpec installation, bool restore)` returns
`RecoveryStatus(Pending, Recovered, Diagnostics)`. `Pending` means a durable
journal exists under `<root>\.omsilaunch\journal.json`; `Recovered` means this
call restored, verified and removed it. With `restore=false` the call only
reports. It takes the installation lease and returns `OL_E_INSTALLATION_BUSY`
while another owner holds it or while the journaled OMSI process is alive. The
CLI exposes it as `/recovery-status` (report only) and `/recover` (restore);
exit code `8` (`TransactionRecoveryFailed`) is returned when a restore was
requested and did not complete. Every start also recovers a pending journal
before it reads the live installation.

## Exit Codes

`PublicExitCode`: `0 = Success`, `1 = SessionFailed` (a plan was not runnable
or an owned session ended `Failed`), `2 = InvalidArguments`,
`3 = UnsupportedProfile`, `4 = NoActiveSession`, `5 = RuntimeUnavailable`,
`6 = NotFound`, `7 = OperationRejected`, `8 = TransactionRecoveryFailed`,
`10 = InternalError`. See [`docs/reference/exit-codes.md`](../reference/exit-codes.md).

## Stop Semantics

`StopAsync`, `CloseAsync`, CLI `session stop`, the tray "End session" action and
Ctrl+C all request the same canonical stop. The supervisor then terminates OMSI
by force (`TerminateProcess`); OMSI's shutdown routine does not run and
`options.cfg` is not rewritten by OMSI on exit. This protects the transaction
from OMSI writing over restored files. `LaunchBehaviorSpec.ShutdownTimeoutSeconds`
is carried in the spec but is currently not consumed by the supervisor.

Public contracts never contain OMSI pointers, Win32 handles, DNNE types, CLR
objects, shared-memory details, or pointer-width-dependent values. Content
identities are canonical OMSI-relative paths. `GetCapabilitiesAsync` and a
`SessionPlan` expose the availability of build-profile-specific operations.

`StartSessionAsync` returns after session ownership, transactional staging and
process supervision are established. It does not promise gameplay. Consumers
wait for `SessionState.Running`; for headless launch requests this means the
host observed `gameplay.entered` from the in-process runtime.

`World.EntrypointIdentity` remains capability-gated until its structured raw
entrypoint-to-presented-list mapping is closed for the active profile. The
native adapter already rejects absent or ambiguous presented labels, but callers
use `PresentedEntrypointIndex` as the supported low-level selector meanwhile.

`DiscoverAsync` is read-only. Add-on discovery is inventory only: OmsiLaunch
does not activate, deactivate, modify entitlement, or alter Steam add-ons.
Discovery skips reparse points, so junction cycles cannot hang planning.

The CLI hierarchical routes `vehicles summary` and `humans summary` map to the
runtime operations `road-vehicles.read` and `humans.read` (collection counts
without handles); `vehicles list` / `humans list` return opaque `rv-NNNNNN` /
`hb-NNNNNN` handles that are session-scoped and rejected as
`OL_E_RUNTIME_OBJECT_HANDLE_STALE` when the native object was replaced.

All configuration in a `LaunchSpec` is temporary session state. The public API
does not expose permanent configuration editing: every touched configuration
file is transaction-snapshotted, journaled, verified, and restored byte-for-byte.

## Session Presentation

`SessionPresentationSpec.Splash` is `Managed` by default. It selects the
OmsiLaunch default splash from `<installation>/.omsilaunch/assets/splash` and
temporarily overlays OMSI GUI splash files. `CustomAssetDirectory` selects a
session/project asset directory; relative paths resolve below the installation.
`Unset` (and the compatibility alias `Native`) explicitly preserves the native
OMSI splash and adds no GUI splash mutation. This presentation policy remains
session-scoped; `.omsilaunch` is product state, not a persistent edit to OMSI
configuration.

### Windows session indicator

`SessionPresentationSpec.SuppressTrayIcon` is an API-only Boolean and defaults
to `false`. Standalone session owners show a localized Windows Notification
Area indicator with read-only Status and canonical End session actions. An
embedding application can set it to `true` when it provides its own session
indicator and stop control; diagnostics, recovery, lifecycle and structured
errors remain active.

```csharp
var presentation = new SessionPresentationSpec(SuppressTrayIcon: true);
```

## D3D9 Advanced API

`D3DRuntimeApi` provides typed session extensions for device status and texture
create, describe, update and release. Public models are `D3DDeviceStatus`,
`D3DTextureHandle`, `D3DTextureDescription`, `D3DTextureUpdate`,
`D3DDeviceState`, `D3DTextureFormat`, and `D3DTextureResourceState`.

Handles are opaque, session-scoped values. Consumers cannot obtain a COM
pointer, OMSI address or Win32 handle. Released, stale-generation and
cross-session values fail with structured `OmsiRuntimeException` codes. Public
session status also exposes the bounded ordered `RuntimeEvents` collection,
which carries D3D lifecycle events with timestamp, sequence and semantic data.

The typed methods are wrappers over the same public runtime command channel:

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
var texture = await launch.CreateD3DTextureAsync(
    session, 64, 64, D3DTextureFormat.A8R8G8B8);
await launch.UpdateD3DTextureAsync(session, texture.Handle,
    new D3DTextureUpdate(0, 0, 0, 64, 16, pixels));
await launch.ReleaseD3DTextureAsync(session, texture.Handle);
```

Calls are valid only while the owning session is `RUNNING` and the matching
BuildProfile is active. Reset does not recreate or retarget a handle.
