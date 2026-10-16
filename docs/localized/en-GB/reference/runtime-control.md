# Runtime Control

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> British English edition of the [canonical page](../../../reference/runtime-control.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

Runtime control is the set of read, write and action operations that OmsiLaunch executes inside a running OMSI session. This page explains the three ways to reach it (owner API, CLI client, local control plane), how requests travel from the caller to the plugin and back, how handles and request identities work, what timeouts apply, what is deliberately not journalled, and the argument and result conventions, with a worked example for each family. The operation inventory itself, with arguments, result keys and errors, is in [capabilities](capabilities.md). Sources: `OmsiLaunchService.ExecuteRuntimeAsync`, `CurrentRuntimeCommandStore` (`src/OmsiLaunch.Process/RuntimeDeployment.cs`), `CurrentRuntimeCommandMailbox` and `CurrentRuntimeControl` (`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`), `LocalControlPlane` and `CliInput` (`tools/OmsiLaunch.Cli/`).

## Three entry points

| Entry point | Who | Path | Timeout |
| --- | --- | --- | --- |
| Owner API | An integrator that started the session in-process with `IOmsiLaunch.StartSessionAsync` | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> registry validation -> session lookup -> mailbox | caller-supplied `TimeSpan` |
| Owner CLI (`/runtime:<op>`) | The `OmsiLaunch.exe` process that owns the session | one operation executed right after `Running`, result written to `.omsilaunch\diagnostics\<session>-runtime-operation.json` and to the console; the session continues | 5 s (15 s for `road-vehicles.spawn`) |
| CLI client | Any `OmsiLaunch.exe` invocation **without** an installation argument, for example `OmsiLaunch.exe time get` | named pipe `runtime.execute` to the owner of the installation the executable lives in -> owner calls `ExecuteRuntimeAsync` | 8 s (30 s for `road-vehicles.spawn`) on both the client and the owner side |
| Local control plane | Any process of the same Windows user | same pipe protocol as the CLI client; see [local control](local-control.md) | as above |

Every path ends in `ExecuteRuntimeAsync`, which enforces the public boundary in this order:

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: an operation not in `PublicRuntimeOperationIds` (including every `internal.*` operation) returns `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`; a missing or blank required argument returns `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Neither touches the session.
2. Session lookup (`KeyNotFoundException` for an unknown handle), `OL_E_RUNTIME_SESSION_MISMATCH` when `RuntimeCommand.SessionId` differs from the handle, `OL_E_SESSION_NOT_RUNNING` unless the state is `Running`.
3. Mailbox request (below), then `ScrubInternalValues` removes any result key starting with `internal_` or ending with `_address`, `_pointer`, `_vmt`.

The CLI client and the control plane run step 1 themselves before contacting the owner, so an invalid command name is reported as exit code 2 even when no session is active (`OL_E_RUNTIME_OPERATION_UNKNOWN` and `OL_E_RUNTIME_ARGUMENT_REQUIRED` map to `InvalidArguments`; other rejections to 7; no owner to 4).

The control-plane endpoint exists only while the owner is `Running`, after startup and after any `/runtime-batch`, `/runtime-write-batch` or `/d3d-batch` harness has completed. Mutating commands (`runtime.execute`, `session.stop`) must carry the active `session_id`; the CLI obtains it from `session.status` automatically (`OL_E_CONTROL_SESSION_MISMATCH` otherwise).

## Request identity and the mailbox

A `RuntimeCommand(SessionId, RequestId, Operation, Arguments)` is serialised into a `RuntimeCommandWire` envelope (magic `OLRC`, version 1, 72-byte header, SHA-256 of the JSON payload) and staged in the session's 64 KiB single-flight mailbox (`OmsiLaunch.Runtime.<sessionId>`). The plugin polls the mailbox every 50 ms on OMSI's UI thread, executes the operation there, and writes the response.

Rules that make the channel robust:

| Rule | Effect |
| --- | --- |
| Single flight | One request at a time per session; the host serialises callers with a semaphore. A slot that is still `requested` when a new request arrives is `OL_E_RUNTIME_CHANNEL_BUSY`. |
| Session binding | The plugin ignores (and clears) a request whose session GUID is not its own; the host rejects a response whose session or request id does not match (`OL_E_RUNTIME_RESPONSE_INVALID`). |
| Timeout | When the deadline passes the host resets the slot to idle and throws `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`. |
| Late response | The plugin publishes a response only if the slot still holds its request id; a response to an abandoned request is dropped. If one nevertheless lands, the next host request finds a stale `responded` slot, discards it, and proceeds; if that stale response carries the **same** request id as the new request, the host throws `OL_E_RUNTIME_REQUEST_ID_REUSED`. Callers must therefore never reuse a request id within a session. |
| Size | A request larger than the slot is rejected before staging (`ArgumentOutOfRangeException`). A list page that does not fit is shortened by the plugin (last rows dropped, `truncated=true`, smaller `returned_count`, `next_cursor` at the first dropped row); any other oversized response is replaced by a typed `OL_E_RUNTIME_RESPONSE_TOO_LARGE` error. |
| Channel closed | After the session ends, `OL_E_RUNTIME_CHANNEL_CLOSED`. |

Request ids are `ulong` chosen by the caller. The CLI uses fixed ranges: `10_001` for `/runtime:`, `50_001+` for forwarded control-plane requests, `1+` for the read batch, `20_000+` for the D3D batch, `30_000+` for `D3DRuntimeApi`. Integrators should use a monotonically increasing counter per session.

## Handles and stale detection

| Prefix | Type | Issued by | Format |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`, `road-vehicles.spawn` (`created_handle`), `player-vehicle.read` | `rv-` + six decimal digits (`rv-000003`) |
| `hb-` | Human | `humans.list` | `hb-` + six decimal digits |
| `d3dtex-` | D3D texture | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Handles are opaque and session-scoped: they are minted by the plugin, never encode an address, and are meaningless in another session. When a handle is resolved, the plugin checks that the address it maps to is still in the live OMSI collection (as of the last list read; `road-vehicles.list` and `humans.list` refresh the view) and re-reads an object fingerprint (the Delphi VMT plus the vehicle definition pointer, or the human model index). A mismatch means the native object was destroyed and its address reused: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`. The same token is returned again for the same live object across repeated list reads. Residual blind spot: an object of the same class and definition recreated at the same address between two list reads cannot be distinguished. D3D handles are validated by the native bridge: an unknown or foreign-session handle is `OL_E_D3D_STALE_RESOURCE_HANDLE`, a released one `OL_E_D3D_RESOURCE_RELEASED`, and a device generation change marks textures `STALE`.

Handles are never native addresses and must never be parsed, compared numerically, persisted across sessions or passed to another session: treat them as opaque strings valid for the session that issued them.

Runtime evidence: a released D3D handle stays rejected after new textures are created and a handle from a previous session is rejected by the next one (runtime closure `H02`); a D3D device reset invalidates every live texture (`OL_E_D3D_STALE_RESOURCE_HANDLE`, `D01`). RoadVehicle and Human stale detection after natural removal (RV-002) has no safe runtime producer (OMSI did not remove objects in the observation windows and no public operation removes one); it is covered offline.

<a id="what-is-not-journaled"></a>
## What is not journalled

Runtime mutations change OMSI's memory only. **They are not recorded in the transaction journal and are not restored** when the session ends: `time.set`, `camera.set`, `camera.lock` (the policy stops when the plugin shuts down), `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random` and every `d3d.texture.*` resource. They disappear with the OMSI process, which OmsiLaunch terminates at the end of the session without letting OMSI persist anything (see [transactions and recovery](../concepts/transactions-and-recovery.md)). Nothing in the runtime family touches the filesystem.

## Argument and result conventions

- Arguments are string key/value pairs. On the CLI, `--key=value` after the command words becomes a runtime argument (`OmsiLaunch.exe vehicles get --handle=rv-000001`); `/runtime-arg:key=value` is the equivalent for `/runtime:<op>`. Numbers use the invariant culture (`.` decimal separator); booleans are `true`/`false`.
- Command words map to operation ids through `CliInput.HierarchicalRoutes` (for example `time get` -> `time.read`, `vehicles summary` -> `road-vehicles.read`, `scripts variable set` -> `vehicle.variable.set`). The full route table is in the [CLI reference](cli.md). D3D operations have no command-word route; use `/runtime:d3d.status` or `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`.
- Results are flat string dictionaries. Lists use `<row>.<n>.<field>` keys (`vehicle.0.handle`, `track.3.filename`, `name.12`) with the page fields `count`, `returned_count`, `truncated` and `next_cursor` (see [pagination](capabilities.md#pagination)).
- Failures carry `Succeeded=false`, an `OL_E_` `ErrorCode` and, for plugin-side failures, `detail` (and `native_status` for D3D, `exception` for unexpected faults).

### CLI envelope (`--json`)

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

An error envelope is `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`. Without `--json` the CLI prints the result object as indented JSON or `CODE: message`.

### API envelope

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` throws for boundary violations after registry validation (`OL_E_RUNTIME_SESSION_MISMATCH`, `OL_E_SESSION_NOT_RUNNING`, `OL_E_RUNTIME_CHANNEL_*`, `OL_E_RUNTIME_REQUEST_TIMEOUT`, `OL_E_RUNTIME_RESPONSE_INVALID`) and returns `Succeeded=false` for registry rejections and plugin-side errors.

## Worked examples

All CLI examples assume an owner is running for the installation that contains `OmsiLaunch.exe` (started with, for example, `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1`, or `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` when an example needs a player vehicle) and are run from a second console in the same installation.

| Family | Command | What it does |
| --- | --- | --- |
| Session | `OmsiLaunch.exe session status --json` | Reads `SessionId`, `State`, diagnostics and the bounded runtime event list. |
| Time | `OmsiLaunch.exe time get` then `OmsiLaunch.exe time set --hour=7 --minute=30` | Reads the clock; sets it through the profiled `SetTime` and returns the read-back. |
| Weather | `OmsiLaunch.exe weather get` and `OmsiLaunch.exe weather actual get` | Reads current and actual/ICAO weather state. `OmsiLaunch.exe weather set --wind_speed=5` sets manual weather (OMSI's source record; live METAR stops) and `OmsiLaunch.exe weather metar set --icao=EDDF` switches to live METAR weather. Both are EXPERIMENTAL until runtime validated. |
| Map | `OmsiLaunch.exe map get` | Map identity, name, description, tile count, year range and traffic side. |
| Camera | `OmsiLaunch.exe camera set --field_of_view=50` then `OmsiLaunch.exe camera lock --family=0 --preset=1` and `OmsiLaunch.exe camera unlock` | Writes FOV; pins the driver family with preset 1 (requires a player vehicle, for example a saved situation); releases the policy. |
| Vehicles | `OmsiLaunch.exe vehicles summary`, `OmsiLaunch.exe vehicles list`, `OmsiLaunch.exe vehicles get --handle=rv-000001` | Counts; handles; one snapshot. |
| Spawn | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | Creates one AI RoadVehicle (30 s timeout); returns `created_handle`. `OmsiLaunch.exe vehicles place-random --group=1` invokes `PlaceRandomBus`. |
| Player | `OmsiLaunch.exe player get` | `present=false` in a headless start, otherwise the player snapshot. |
| Humans | `OmsiLaunch.exe humans summary`, `OmsiLaunch.exe humans list`, `OmsiLaunch.exe humans get --handle=hb-000001` | Counts; handles; one snapshot. |
| Timetable | `OmsiLaunch.exe timetable get`, `OmsiLaunch.exe timetable tracks list`, `OmsiLaunch.exe timetable logs list` | Manager counts; bounded track rows; timetable logs. |
| Scripts | `OmsiLaunch.exe scripts variable list --handle=rv-000001`, `OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`, `OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`, `OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`, `OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`, `OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | Numeric variable list/read/write; string variable read/write; script and sound triggers (string writes and triggers are EXPERIMENTAL until runtime validated). |
| Constants and curves | `OmsiLaunch.exe constants list --handle=rv-000001`, `OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`, `OmsiLaunch.exe curves list --handle=rv-000001`, `OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | Vehicle constants and curve evaluation. Names are model-specific: use the names returned by the `list` command (these were listed for the player bus of `situations\Linie 5.osn`). |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | HOF metadata of the vehicle definition. |
| Drivers and tickets | `OmsiLaunch.exe drivers list`, `OmsiLaunch.exe tickets get` | Driver records; ticket pack. |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` then `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` and `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | Texture lifecycle on the render thread; `<HANDLE>` is the `handle` printed by `create`; `--pixels_base64` must decode to `width * height * 4` bytes (32-bit formats) and at most 48 KiB. |
| Events | `OmsiLaunch.exe events read`, `OmsiLaunch.exe events watch` | Bounded event list; polling watch (250 ms) until Ctrl+C. |
| Stop | `OmsiLaunch.exe session stop` | Requests the canonical stop: OMSI is terminated and the transaction restored by the owner. |

## Stability

The channel itself (`runtime.command-channel`) is `STABLE_BETA`: wire integrity, session binding, late-response discard and typed oversize rejection are covered offline by `runtime-command.wire-guard`, `runtime-command.session-binding` and `runtime-command.late-response-ignored`, and at runtime by RV-003 (session `5f641c8d`), Round A RA-019 (a client abandoned `road-vehicles.spawn` after 250 ms; the next request succeeded) and the runtime closure channel battery `R03` (timeout, cancellation, reused request id, plugin rejection, internal operation, wrong session and missing argument, each followed by a successful request). Per-operation stability is in [capabilities](capabilities.md).

## Channel reuse after failures

Every terminal path of a runtime request leaves the mailbox reusable: success, a typed error, a malformed, oversized, foreign-session or wrong-request-id response, timeout, caller cancellation and decode failures all end in one cleanup that returns the slot to idle and clears the length and envelope header, so nothing from an earlier request can be read by the next one. A leftover request or response found when a new request starts is cleared first (`OL_E_RUNTIME_REQUEST_ID_REUSED` if it carries the new request's id). On the plugin side, an exception inside an operation is answered with `OL_E_RUNTIME_OPERATION_FAILED`, a result larger than the slot with `OL_E_RUNTIME_RESPONSE_TOO_LARGE` (a fixed, small envelope), a request for another session with `OL_E_RUNTIME_SESSION_MISMATCH`, and a request the host has abandoned is never answered. These rules are covered offline (`runtime-command.terminal-paths-leave-channel-usable`, `plugin-runtime.oversized-and-abandoned-responses`, `plugin-runtime.bounded-list-fits-slot`); the paths a caller can produce (timeout, cancellation, reused id, typed rejections, late response) also have runtime evidence (RA-019, `R03`). Corrupt, foreign or oversized responses cannot be produced from outside the product and remain offline-only.
