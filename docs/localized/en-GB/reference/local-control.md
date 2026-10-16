# Local Control Plane

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> British English edition of the [canonical page](../../../reference/local-control.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

The local control plane is the named-pipe endpoint through which a running OmsiLaunch owner (the process that started a session) accepts semantic session commands from other processes on the same machine: status, events, stop, and public runtime operations. This page specifies the endpoint as implemented in `tools\OmsiLaunch.Cli\LocalControlPlane.cs` and the request handler in `OwnerSession.RunAsync` (`tools\OmsiLaunch.Cli\Program.cs`): pipe naming, framing, protocol version, commands, session binding, error codes, trust model, availability window and how to talk to it from other tooling. The CLI client commands (`session status`, `session stop`, `events read`, `events watch`, routes such as `time get`) are thin wrappers over this protocol; see the [CLI reference](cli.md). The runtime operations themselves are specified in [runtime control](runtime-control.md); the in-process alternative for integrators is the [public API](public-api.md).

## Summary

| Property | Value |
|---|---|
| Transport | Windows named pipe, `PipeDirection.InOut`, byte mode, `PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`, `MaxAllowedServerInstances`, in and out buffers of one frame (65,540 bytes) |
| Pipe name | `OmsiLaunch.Control.0.1.<key>` where `<key>` is the first 16 hex characters of SHA-256 over the UTF-8 bytes of the installation root's full path, with a trailing `\` removed, upper-cased (`LocalControlPlane.PipeNameFor`) |
| Framing | 4-byte little-endian `int32` length prefix followed by that many bytes of UTF-8 JSON; one request and one response per connection |
| Maximum message | 65,536 bytes for a request and for a response (`MaxMessageBytes`) |
| Protocol version | `"0.1"` (`PublicCapabilityRegistry.ProtocolVersion`); mismatch is answered with `OL_E_CONTROL_PROTOCOL` |
| Commands | `session.status`, `session.events`, `session.stop`, `runtime.execute` |
| Session binding | `session.stop` and `runtime.execute` require `Arguments.session_id` equal to the active session id |
| Availability | From the moment the session reached `Running` (after any validation batch) until the session reaches `Completed` or `Failed` and the owner disposes the endpoint |
| Scope | One endpoint per installation root; two installations of the same user never share a pipe |
| Stability | STABLE_BETA (offline tests in `OmsiLaunch.WindowsUiTests`; runtime evidence: RV-003 concurrent status during a multi-second native call, Round A RA-008, and the runtime closure raw-frame battery `R04`: 17 malformed, oversized, wrong-protocol, unknown-command, unbound and wrong-session frames each answered with its typed error and followed by a successful `session.status`; a stalled client does not block others; a bound stop during an in-flight spawn ends the session) |

## Pipe name derivation

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` is the single definition of installation identity, also used by the installation lease: it resolves the full path (including `.` and `..` segments, `/` or `\`, repeated separators) and removes trailing separators except for a drive root. `D:\OMSI 2`, `D:\OMSI 2\`, `D:\OMSI 2\.`, `D:\x\..\OMSI 2` and `d:\omsi 2` produce the same name; `D:\OMSI 2` and `E:\OMSI 2` produce different names. The client always derives the name from the installation that contains the executable it runs (`AppContext.BaseDirectory`) unless the API caller passes an explicit root. The `0.1` inside the name is the protocol version, so a future protocol can coexist on the same machine.

## Framing and encoding

- Writer: serialise the record with `System.Text.Json` default options, check `length <= 65536` (otherwise `OL_E_CONTROL_MESSAGE_TOO_LARGE`), write `BitConverter.GetBytes((int)length)` (4 bytes, little-endian on Windows), write the payload, flush.
- Reader: read exactly 4 bytes; reject `length < 0` or `length > 65536` with `OL_E_CONTROL_MESSAGE_INVALID`; read exactly `length` bytes; deserialise. A connection that closes before a full frame was read is dropped silently by the owner (no reply). A rejected frame is answered even when the client has already written more bytes than the owner reads: the pipe buffers hold a whole frame, so the client's write completes and it can read the reply (runtime closure BUG-02; before the fix such a client and the owner both blocked in their writes).
- The owner writes each reply under a 5 s deadline: a client that connects, sends a request and never reads the reply cannot hold an owner task for longer.
- Property names are **PascalCase and case-sensitive** on the request (`ProtocolVersion`, `Command`, `Arguments`), because the owner deserialises with default options. Responses use `Ok`, `Result`, `ErrorCode`, `Message`. Enum values are serialised as integers; `Guid`s as strings.
- One request per connection: the owner reads one request, writes one response and closes the pipe. Open a new connection for every request.

### Request

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` must be the id returned by `session.status` for the running session (the value above is from a real session).

`Arguments` is optional (`null` or omitted) for `session.status` and `session.events`. Argument keys are compared ordinally (case-sensitive).

### Response

Frames captured from a real owner (runtime closure `R04`, final package). A successful `runtime.execute`:

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

A rejected `session.stop` without the active `session_id`:

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

## Commands

| Command | Arguments | Result on `Ok=true` | Notes |
|---|---|---|---|
| `session.status` | none | `SessionStatus`: `SessionId` (string GUID), `State` (integer `SessionState`; `14` = `Running`, `15` = `ProcessExited`, `16` = `Restoring`, `18` = `Completed`, `19` = `Failed`), `Diagnostics` (array of `{Code, Message, Data}`), `RuntimeEvents` (array) | Read-only. Clients read this first to obtain the `SessionId`. |
| `session.events` | none | Array of `RuntimeEvent`: `Type`, `TimestampUtc`, `Sequence` (monotonic `int64`), `Data` (string map) | Read-only, bounded list; `events watch` polls it and prints entries with a `Sequence` above the last seen. |
| `session.stop` | `session_id` (required) | `{"accepted": true, "session_id": "..."}` | Requests the canonical stop (`TerminateProcess` on OMSI, then restore). Returns immediately; the session state is observable through `session.status` until the endpoint disappears. |
| `runtime.execute` | `operation` (required), `session_id` (required), plus the operation's own arguments (`handle`, `name`, `value`, `model`, `family`, ...) | `RuntimeCommandResult`: `SessionId`, `RequestId` (owner-assigned, starting at `50001`), `Succeeded`, `ErrorCode`, `Values` (string map) | Validated with `PublicCapabilityRegistry.ValidateRuntimeArguments` before execution: unknown or internal operation → `OL_E_RUNTIME_OPERATION_UNKNOWN`; missing required value → `OL_E_RUNTIME_ARGUMENT_REQUIRED`. Timeout 8 s, 30 s for `road-vehicles.spawn`. A runtime failure is answered as `Ok=false` with `Result` set to the `RuntimeCommandResult`, `ErrorCode` to its code and `Message` `Runtime operation was rejected.` Result keys starting with `internal_` or ending with `_address`, `_pointer`, `_vmt` are stripped by the API before they reach the pipe. |
| anything else | | `Ok=false`, `OL_E_CONTROL_COMMAND_UNKNOWN` | |

The owner serves connections concurrently (each accepted client is served on its own task), so `session.status` keeps answering while a multi-second native call such as `road-vehicles.spawn` is in progress.

## Session id binding

Mutating commands must name the session they act on. The CLI implements `TryRequestBoundAsync`: it sends `session.status` (750 ms), takes `Result.SessionId`, and repeats the request with `session_id` added to `Arguments`. A missing, unparsable or different id is answered with `OL_E_CONTROL_SESSION_MISMATCH`. If the owner does not report a session id, the client reports `OL_E_CONTROL_PROTOCOL`.

### Reply metadata and truncated event history

`Metadata` is `null` unless the reply carries a fact about itself. `session.status` and `session.events` return the owner's event history (at most 256 events, newest last). When that history does not fit one 64 KiB frame, the oldest events are removed until it fits and `Metadata` states it:

| Key | Meaning |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | Events in this reply |
| `events_dropped_count` | Oldest events removed to fit the frame |
| `events_available_count` | Events the owner held for this reply |
| `events_first_returned_sequence` | `Sequence` of the first returned event |

Without `Metadata`, the reply is not truncated by the control plane. `Sequence` is monotonic in both cases, so a client can also detect events that the owner itself evicted beyond its 256-event history. The CLI prints this metadata in the `--json` envelope and a note in text mode.

## Error codes

| Code | Origin | Meaning |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | owner / client | Request `ProtocolVersion` is not `0.1`; the owner's reply could not be decoded or was empty; the owner closed the connection without replying; the connection broke after it was established; or the owner reported no session id. |
| `OL_E_CONTROL_MESSAGE_INVALID` | owner / client | Length prefix out of range (an oversized request frame is refused this way before its payload is read), empty frame, JSON `null`, request without `Command`, or JSON that could not be decoded. |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | client writer | The client's own request exceeds 65,536 bytes; it is reported to the client and nothing is sent. |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | owner writer | The owner's reply exceeds 65,536 bytes; the owner answers with this typed error. `session.status` and `session.events` trim their event history (oldest first) to fit, so they do not produce it. |
| `OL_E_CONTROL_SESSION_MISMATCH` | owner | `session.stop` / `runtime.execute` without the active `session_id`. |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | owner | Unsupported command name. |
| `OL_E_CONTROL_HANDLER_FAILED` | owner | The handler's reply could not be serialised, or the handler threw an exception without an `OL_E_` code in its message; with a code, that code is returned instead (for example `OL_E_SESSION_NOT_RUNNING` from `ExecuteRuntimeAsync` after OMSI exited). |
| `OL_E_CONTROL_FAILED` | CLI client | Default code when a reply is `Ok=false` without `ErrorCode`. |
| `OL_E_TIMEOUT` | client | The client was connected to an owner that did not reply within the timeout. |
| `OL_E_NO_ACTIVE_SESSION` | CLI client | No owner endpoint could be reached (`TryRequestAsync` returns `null` only when the connection itself fails). Once connected, every failure is one of the typed codes above. Exit `4`. |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`, `OL_E_RUNTIME_ARGUMENT_REQUIRED` | client and owner | Public-surface validation of `runtime.execute` (exit `2` in the CLI). |

Every known failure is answered with a typed error so a client never mistakes a fault for an absent owner. Two cases end without a reply frame: a client that disconnects before sending a full request (nobody to answer), and a request still in flight when the session ends (the owner cancels pending replies while it shuts the endpoint down; the client sees end of stream, which the CLI client reports as `OL_E_CONTROL_PROTOCOL` "The owner closed the connection without a reply"). A client that sent a request and disconnects before reading the reply does not affect the owner. An owner start refuses to proceed when any reply (including a typed error) comes from the installation's endpoint, because a reply proves an owner is present. The CLI maps replies to exit codes as described in [exit codes](exit-codes.md).

## Trust model (accepted risk)

- `PipeOptions.CurrentUserOnly` restricts the pipe to the Windows user (and integrity level) that owns the session; the client side of the same option verifies that the server is owned by the same user.
- **Any process running as the same Windows user can read status, stop the session and execute public runtime operations.** There is no additional authentication, token or per-client authorisation. This is a documented, accepted risk for the beta; do not run OmsiLaunch sessions under an account shared with untrusted software.
- The pipe never exposes native addresses, handles or raw memory operations; only the public operation ids of `PublicCapabilityRegistry` are accepted, and internal research primitives (`internal.*`) are rejected before any session lookup.
- Messages are bounded (64 KiB) and framed; a malformed or oversized frame is answered or dropped without affecting the session.
- If the pipe name is already owned by another process when the owner starts listening (`IOException`/`UnauthorizedAccessException` on creation), the owner keeps running the session without an endpoint and records the fault in `LocalControlPlane.ListenFault`; clients then see `OL_E_NO_ACTIVE_SESSION`. Use the tray icon or Ctrl+C to end such a session.

## Availability window

1. `StartSessionAsync` runs; the endpoint does not exist yet. A second `OmsiLaunch.exe` launch for the same root probes `session.status` for 250 ms before starting and fails with `OL_E_SESSION_ALREADY_ACTIVE` only when an owner already answers; two owners racing during startup are serialised by the installation lease (`OL_E_INSTALLATION_BUSY`).
2. The session reaches `Running`; validation batches (`/runtime-batch`, `/runtime-write-batch`, `/d3d-batch`), when requested, complete.
3. `LocalControlPlane.Start()`: the endpoint accepts connections. The owner's own `/runtime:` operation, if any, runs after this point.
4. The endpoint stays up through `ProcessExited`, `Restoring` and `CleaningRuntime` (status reports those states), until the session is `Completed` or `Failed`.
5. `DisposeAsync` cancels the listener, awaits in-flight client tasks, and the owner calls `CloseAsync`. After that the pipe name no longer exists.

Clients should use short connect timeouts (the CLI uses 750 ms for status/stop/events) and treat "no endpoint" as "no active session for this installation".

## Using the protocol from other tooling

### C# (.NET 6 or later)

```csharp
using System.IO.Pipes;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

// Same result as OmsiLaunch.Api.InstallationPaths.IdentityKey for any root that is not a drive root;
// reference OmsiLaunch.Api and call InstallationPaths.IdentityKey(root) to cover drive roots as well.
static string PipeNameFor(string installationRoot)
{
    var normalized = Path.GetFullPath(installationRoot).TrimEnd(Path.DirectorySeparatorChar).ToUpperInvariant();
    var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    return "OmsiLaunch.Control.0.1." + key[..16];
}

static async Task<JsonDocument> SendAsync(string installationRoot, object request, TimeSpan timeout)
{
    using var cancellation = new CancellationTokenSource(timeout);
    await using var pipe = new NamedPipeClientStream(".", PipeNameFor(installationRoot), PipeDirection.InOut,
        PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly);
    await pipe.ConnectAsync(cancellation.Token);
    var payload = JsonSerializer.SerializeToUtf8Bytes(request);          // must be <= 65,536 bytes
    await pipe.WriteAsync(BitConverter.GetBytes(payload.Length), cancellation.Token);
    await pipe.WriteAsync(payload, cancellation.Token);
    await pipe.FlushAsync(cancellation.Token);
    var length = new byte[4];
    await ReadExactlyAsync(pipe, length, cancellation.Token);
    var reply = new byte[BitConverter.ToInt32(length)];
    await ReadExactlyAsync(pipe, reply, cancellation.Token);
    return JsonDocument.Parse(reply);
}

static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken token)
{
    var read = 0;
    while (read < buffer.Length)
    {
        var count = await stream.ReadAsync(buffer.AsMemory(read), token);
        if (count == 0) throw new EndOfStreamException("The owner closed the pipe before a full frame was received.");
        read += count;
    }
}

var root = @"C:\OMSI 2";                                        // <OMSI_PATH>
using var status = await SendAsync(root, new { ProtocolVersion = "0.1", Command = "session.status" }, TimeSpan.FromMilliseconds(750));
var sessionId = status.RootElement.GetProperty("Result").GetProperty("SessionId").GetString()!;
using var time = await SendAsync(root,
    new { ProtocolVersion = "0.1", Command = "runtime.execute",
          Arguments = new Dictionary<string, string> { ["operation"] = "time.read", ["session_id"] = sessionId } },
    TimeSpan.FromSeconds(8));
Console.WriteLine(time.RootElement.GetProperty("Result").GetProperty("Values"));
```

The anonymous objects serialise to `ProtocolVersion`/`Command`/`Arguments` exactly as the owner expects. A `TimeoutException`/`OperationCanceledException` on `ConnectAsync` means there is no owner for that root.

### PowerShell 7 (`pwsh`)

`PipeOptions.CurrentUserOnly`, `SHA256.HashData` and `Convert.ToHexString` require .NET 5 or later, so this example needs PowerShell 7; Windows PowerShell 5.1 (.NET Framework) cannot set `CurrentUserOnly`.

```powershell
$root = 'D:\OMSI 2'
$normalized = [IO.Path]::GetFullPath($root).TrimEnd('\').ToUpperInvariant()
$key = [Convert]::ToHexString([Security.Cryptography.SHA256]::HashData([Text.Encoding]::UTF8.GetBytes($normalized)))
$pipeName = 'OmsiLaunch.Control.0.1.' + $key.Substring(0, 16)

function Send-OmsiLaunchControl([string] $Json, [int] $TimeoutMilliseconds = 750) {
    $pipe = [IO.Pipes.NamedPipeClientStream]::new('.', $pipeName, [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::CurrentUserOnly)
    try {
        $pipe.Connect($TimeoutMilliseconds)
        $bytes = [Text.Encoding]::UTF8.GetBytes($Json)
        if ($bytes.Length -gt 65536) { throw 'OL_E_CONTROL_MESSAGE_TOO_LARGE' }
        $pipe.Write([BitConverter]::GetBytes([int] $bytes.Length), 0, 4)
        $pipe.Write($bytes, 0, $bytes.Length)
        $pipe.Flush()
        $lengthBytes = [byte[]]::new(4); $read = 0
        while ($read -lt 4) { $n = $pipe.Read($lengthBytes, $read, 4 - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: the owner closed the connection without a reply' }; $read += $n }
        $length = [BitConverter]::ToInt32($lengthBytes, 0)
        $buffer = [byte[]]::new($length); $read = 0
        while ($read -lt $length) { $n = $pipe.Read($buffer, $read, $length - $read); if ($n -eq 0) { throw 'OL_E_CONTROL_PROTOCOL: truncated reply' }; $read += $n }
        [Text.Encoding]::UTF8.GetString($buffer) | ConvertFrom-Json
    }
    finally { $pipe.Dispose() }
}

$status = Send-OmsiLaunchControl '{"ProtocolVersion":"0.1","Command":"session.status"}'
$sessionId = $status.Result.SessionId
$request = @{ ProtocolVersion = '0.1'; Command = 'runtime.execute'; Arguments = @{ operation = 'time.read'; session_id = $sessionId } } | ConvertTo-Json -Compress
Send-OmsiLaunchControl $request 8000
```

`ConvertTo-Json` keeps the hashtable key casing, so `ProtocolVersion`, `Command` and `Arguments` are emitted as written.

### Using the CLI as the client

When no custom client is needed, invoke `OmsiLaunch.exe` from the installation directory: `session status --json`, `session stop`, `events read --json`, `events watch`, `time get --json`, `/runtime:d3d.status --json`. The CLI performs the status/bind handshake and maps replies to [exit codes](exit-codes.md) (`0` ok, `2` argument validation, `4` no owner, `7` rejected).

## Relationship to other channels

- The runtime mailbox between the owner and the in-process plugin (memory-mapped, 64 KiB, single-flight, session-bound) is a separate, private channel; the control plane only forwards to it through `IOmsiLaunch.ExecuteRuntimeAsync`.
- The installation lease (`Local\OmsiLaunch.Installation.<sha256(root)>`) is a named semaphore, not part of this protocol; it guarantees a single owner per installation per logon session.
- The tray icon's "End session" and the control plane's `session.stop` signal the same owner-side stop request; see [Windows tray](windows-tray.md).
