# 本地控制平面

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/local-control.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本地控制平面是一个命名管道端点：正在运行的 OmsiLaunch 所有者（启动会话的进程）通过它接受同一台计算机上其他进程发来的语义化会话命令，包括状态、事件、停止以及公开运行时操作。本页按照 `tools\OmsiLaunch.Cli\LocalControlPlane.cs` 中的实现以及 `OwnerSession.RunAsync`（`tools\OmsiLaunch.Cli\Program.cs`）中的请求处理程序来规定该端点：管道命名、分帧、协议版本、命令、会话绑定、错误码、信任模型、可用时间窗口，以及如何从其他工具与之通信。CLI 客户端命令（`session status`、`session stop`、`events read`、`events watch`，以及 `time get` 等路由）只是该协议的薄封装；见 [CLI 参考](cli.md)。运行时操作本身在[运行时控制](runtime-control.md)中规定；面向集成方的进程内替代方案是[公共 API](public-api.md)。

<a id="summary"></a>
## 概要

| 属性 | 值 |
|---|---|
| 传输 | Windows 命名管道，`PipeDirection.InOut`，字节模式，`PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`，`MaxAllowedServerInstances`，输入和输出缓冲区均为一帧大小（65,540 字节） |
| 管道名称 | `OmsiLaunch.Control.0.1.<key>`，其中 `<key>` 是对安装根目录完整路径（去掉末尾的 `\`、转为大写）的 UTF-8 字节计算 SHA-256 后取前 16 个十六进制字符（`LocalControlPlane.PipeNameFor`） |
| 分帧 | 4 字节小端序 `int32` 长度前缀，后跟相应字节数的 UTF-8 JSON；每个连接一个请求和一个响应 |
| 最大消息 | 请求和响应均为 65,536 字节（`MaxMessageBytes`） |
| 协议版本 | `"0.1"`（`PublicCapabilityRegistry.ProtocolVersion`）；版本不匹配时以 `OL_E_CONTROL_PROTOCOL` 应答 |
| 命令 | `session.status`、`session.events`、`session.stop`、`runtime.execute` |
| 会话绑定 | `session.stop` 和 `runtime.execute` 要求 `Arguments.session_id` 等于当前活动的会话 ID |
| 可用性 | 从会话进入 `Running`（在任何验证批次之后）开始，直到会话进入 `Completed` 或 `Failed` 且所有者释放该端点为止 |
| 作用范围 | 每个安装根目录一个端点；同一用户的两个安装实例永远不会共用一个管道 |
| 稳定性 | STABLE_BETA（离线测试位于 `OmsiLaunch.WindowsUiTests`；运行时证据：RV-003 在持续数秒的原生调用期间并发读取状态、Round A RA-008，以及运行时收尾的原始帧测试组 `R04`：17 个格式错误、超大、协议错误、未知命令、未绑定和会话错误的帧，每个都以其带类型的错误应答，随后跟随一次成功的 `session.status`；停滞的客户端不会阻塞其他客户端；在生成过程中发送的已绑定停止请求会结束会话） |

<a id="pipe-name-derivation"></a>
## 管道名称的推导

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` 是安装实例标识的唯一定义，安装租约也使用它：它解析完整路径（包括 `.` 和 `..` 段、`/` 或 `\`、重复的分隔符），并移除末尾的分隔符（驱动器根目录除外）。`D:\OMSI 2`、`D:\OMSI 2\`、`D:\OMSI 2\.`、`D:\x\..\OMSI 2` 和 `d:\omsi 2` 生成相同的名称；`D:\OMSI 2` 和 `E:\OMSI 2` 生成不同的名称。除非 API 调用方显式传入根目录，客户端总是根据其所运行的可执行文件所在的安装实例（`AppContext.BaseDirectory`）推导名称。名称中的 `0.1` 是协议版本，因此未来的协议可以在同一台计算机上共存。

<a id="framing-and-encoding"></a>
## 分帧与编码

- 写入方：使用 `System.Text.Json` 默认选项序列化记录，检查 `length <= 65536`（否则返回 `OL_E_CONTROL_MESSAGE_TOO_LARGE`），写入 `BitConverter.GetBytes((int)length)`（4 字节，在 Windows 上为小端序），写入负载，刷新。
- 读取方：恰好读取 4 字节；以 `OL_E_CONTROL_MESSAGE_INVALID` 拒绝 `length < 0` 或 `length > 65536`；恰好读取 `length` 字节；反序列化。在读完一整帧之前就关闭的连接会被所有者静默丢弃（不回复）。即使客户端写入的字节已多于所有者读取的字节，被拒绝的帧也会得到应答：管道缓冲区能容纳一整帧，因此客户端的写入可以完成，随后它就能读取回复（运行时收尾 BUG-02；修复之前，这样的客户端和所有者都会阻塞在各自的写入上）。
- 所有者在 5 s 的截止时间内写入每个回复：一个连接后发送请求却从不读取回复的客户端，占用所有者任务的时间不会超过这一时限。
- 请求中的属性名是 **PascalCase 且区分大小写**的（`ProtocolVersion`、`Command`、`Arguments`），因为所有者使用默认选项进行反序列化。响应使用 `Ok`、`Result`、`ErrorCode`、`Message`。枚举值序列化为整数；`Guid` 序列化为字符串。
- 每个连接一个请求：所有者读取一个请求，写入一个响应，然后关闭管道。每个请求都要打开一个新连接。

<a id="request"></a>
### 请求

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` 必须是 `session.status` 为正在运行的会话返回的 ID（上面的值来自一个真实会话）。

对于 `session.status` 和 `session.events`，`Arguments` 是可选的（`null` 或省略）。参数键按序数比较（区分大小写）。

<a id="response"></a>
### 响应

以下帧采集自一个真实的所有者（运行时收尾 `R04`，最终包）。一次成功的 `runtime.execute`：

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

一次未携带当前活动 `session_id` 而被拒绝的 `session.stop`：

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## 命令

| 命令 | 参数 | `Ok=true` 时的结果 | 说明 |
|---|---|---|---|
| `session.status` | 无 | `SessionStatus`：`SessionId`（字符串形式的 GUID）、`State`（整数 `SessionState`；`14` = `Running`，`15` = `ProcessExited`，`16` = `Restoring`，`18` = `Completed`，`19` = `Failed`）、`Diagnostics`（`{Code, Message, Data}` 数组）、`RuntimeEvents`（数组） | 只读。客户端首先读取它以获得 `SessionId`。 |
| `session.events` | 无 | `RuntimeEvent` 数组：`Type`、`TimestampUtc`、`Sequence`（单调递增的 `int64`）、`Data`（字符串映射） | 只读，有界列表；`events watch` 轮询它，并输出 `Sequence` 大于上次所见值的条目。 |
| `session.stop` | `session_id`（必需） | `{"accepted": true, "session_id": "..."}` | 请求规范停止（对 OMSI 执行 `TerminateProcess`，然后还原）。立即返回；在端点消失之前，可以通过 `session.status` 观察会话状态。 |
| `runtime.execute` | `operation`（必需）、`session_id`（必需），以及该操作自身的参数（`handle`、`name`、`value`、`model`、`family`……） | `RuntimeCommandResult`：`SessionId`、`RequestId`（由所有者分配，从 `50001` 开始）、`Succeeded`、`ErrorCode`、`Values`（字符串映射） | 执行前使用 `PublicCapabilityRegistry.ValidateRuntimeArguments` 校验：未知操作或内部操作 → `OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必需值 → `OL_E_RUNTIME_ARGUMENT_REQUIRED`。超时为 8 s，`road-vehicles.spawn` 为 30 s。运行时失败以 `Ok=false` 应答，其中 `Result` 设为该 `RuntimeCommandResult`，`ErrorCode` 设为其错误码，`Message` 为 `Runtime operation was rejected.`。以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的结果键在到达管道之前就会被 API 剥离。 |
| 其他任何命令 | | `Ok=false`，`OL_E_CONTROL_COMMAND_UNKNOWN` | |

所有者并发处理连接（每个被接受的客户端都在各自的任务上处理），因此在 `road-vehicles.spawn` 这类持续数秒的原生调用进行期间，`session.status` 仍能持续应答。

<a id="session-id-binding"></a>
## 会话 ID 绑定

会产生修改的命令必须指明其作用的会话。CLI 实现了 `TryRequestBoundAsync`：它发送 `session.status`（750 ms），取得 `Result.SessionId`，然后在 `Arguments` 中加入 `session_id` 重新发送请求。缺失、无法解析或不一致的 ID 以 `OL_E_CONTROL_SESSION_MISMATCH` 应答。如果所有者没有报告会话 ID，客户端报告 `OL_E_CONTROL_PROTOCOL`。

<a id="reply-metadata-and-truncated-event-history"></a>
### 回复元数据与被截断的事件历史

除非回复携带了关于自身的某项事实，否则 `Metadata` 为 `null`。`session.status` 和 `session.events` 返回所有者的事件历史（最多 256 个事件，最新的在最后）。当该历史无法放入一个 64 KiB 的帧时，会从最旧的事件开始移除，直至能够放入，并由 `Metadata` 说明这一情况：

| 键 | 含义 |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | 本次回复中的事件数 |
| `events_dropped_count` | 为放入帧而移除的最旧事件数 |
| `events_available_count` | 所有者为本次回复持有的事件数 |
| `events_first_returned_sequence` | 第一个返回事件的 `Sequence` |

没有 `Metadata` 时，回复未被控制平面截断。两种情况下 `Sequence` 都是单调递增的，因此客户端也能检测到所有者自身因超出 256 个事件的历史上限而淘汰的事件。CLI 会在 `--json` 信封中输出这些元数据，在文本模式下输出一条提示。

<a id="error-codes"></a>
## 错误码

| 错误码 | 来源 | 含义 |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | 所有者 / 客户端 | 请求的 `ProtocolVersion` 不是 `0.1`；所有者的回复无法解码或为空；所有者未回复就关闭了连接；连接建立后中断；或所有者未报告会话 ID。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | 所有者 / 客户端 | 长度前缀超出范围（超大的请求帧以此方式在读取其负载之前被拒绝）、空帧、JSON `null`、缺少 `Command` 的请求，或无法解码的 JSON。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | 客户端写入方 | 客户端自身的请求超过 65,536 字节；该错误报告给客户端，不发送任何内容。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | 所有者写入方 | 所有者的回复超过 65,536 字节；所有者以此带类型的错误应答。`session.status` 和 `session.events` 会裁剪其事件历史（从最旧的开始）以放入帧中，因此不会产生此错误。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | 所有者 | `session.stop` / `runtime.execute` 未携带当前活动的 `session_id`。 |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | 所有者 | 不受支持的命令名。 |
| `OL_E_CONTROL_HANDLER_FAILED` | 所有者 | 处理程序的回复无法序列化，或处理程序抛出的异常消息中不含 `OL_E_` 错误码；若含有错误码，则改为返回该错误码（例如 OMSI 退出后 `ExecuteRuntimeAsync` 抛出的 `OL_E_SESSION_NOT_RUNNING`）。 |
| `OL_E_CONTROL_FAILED` | CLI 客户端 | 回复为 `Ok=false` 且没有 `ErrorCode` 时的默认错误码。 |
| `OL_E_TIMEOUT` | 客户端 | 客户端已连接到所有者，但所有者未在超时时间内回复。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI 客户端 | 无法连接到任何所有者端点（仅当连接本身失败时 `TryRequestAsync` 才返回 `null`）。一旦连接成功，每种失败都属于上述带类型的错误码之一。退出码 `4`。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 客户端和所有者 | 对 `runtime.execute` 的公开接口校验（在 CLI 中退出码为 `2`）。 |

每种已知失败都以带类型的错误应答，因此客户端绝不会把故障误认为所有者不存在。有两种情况不会有回复帧：客户端在发送完整请求之前断开连接（没有应答对象）；以及会话结束时仍在处理中的请求（所有者在关闭端点时会取消待发送的回复；客户端看到流结束，CLI 客户端将其报告为 `OL_E_CONTROL_PROTOCOL`，消息为 "The owner closed the connection without a reply"（所有者未回复就关闭了连接））。客户端发送请求后在读取回复之前断开连接，不会影响所有者。只要该安装实例的端点返回了任何回复（包括带类型的错误），所有者启动就会拒绝继续，因为回复证明已有所有者存在。CLI 如何将回复映射为退出码，见[退出码](exit-codes.md)。

<a id="trust-model-accepted-risk"></a>
## 信任模型（已接受的风险）

- `PipeOptions.CurrentUserOnly` 将管道限制为拥有该会话的 Windows 用户（及完整性级别）；客户端一侧的同一选项会验证服务器由同一用户拥有。
- **以同一 Windows 用户身份运行的任何进程都可以读取状态、停止会话并执行公开运行时操作。** 没有额外的身份验证、令牌或按客户端的授权。这是 Beta 版中已记录的、已接受的风险；不要在与不受信任软件共用的账户下运行 OmsiLaunch 会话。
- 管道从不暴露原生地址、句柄或原始内存操作；只接受 `PublicCapabilityRegistry` 中的公开操作 ID，内部研究原语（`internal.*`）会在任何会话查找之前被拒绝。
- 消息有大小上限（64 KiB）并经过分帧；格式错误或超大的帧会被应答或丢弃，而不会影响会话。
- 如果所有者开始监听时管道名称已被另一个进程占用（创建时出现 `IOException`/`UnauthorizedAccessException`），所有者会在没有端点的情况下继续运行会话，并将该故障记录在 `LocalControlPlane.ListenFault` 中；此时客户端看到的是 `OL_E_NO_ACTIVE_SESSION`。可使用托盘图标或 Ctrl+C 结束这样的会话。

<a id="availability-window"></a>
## 可用时间窗口

1. `StartSessionAsync` 运行；此时端点尚不存在。针对同一根目录的第二次 `OmsiLaunch.exe` 启动会在开始前以 250 ms 探测 `session.status`，仅当已有所有者应答时才以 `OL_E_SESSION_ALREADY_ACTIVE` 失败；启动期间相互竞争的两个所有者由安装租约串行化（`OL_E_INSTALLATION_BUSY`）。
2. 会话进入 `Running`；如有请求，验证批次（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`）执行完毕。
3. `LocalControlPlane.Start()`：端点开始接受连接。所有者自身的 `/runtime:` 操作（如有）在此之后运行。
4. 端点在 `ProcessExited`、`Restoring` 和 `CleaningRuntime` 期间保持可用（状态会报告这些状态），直至会话变为 `Completed` 或 `Failed`。
5. `DisposeAsync` 取消监听器，等待处理中的客户端任务完成，然后所有者调用 `CloseAsync`。此后该管道名称不再存在。

客户端应使用较短的连接超时（CLI 对 status/stop/events 使用 750 ms），并将“没有端点”视为“该安装实例没有活动会话”。

<a id="using-the-protocol-from-other-tooling"></a>
## 从其他工具使用该协议

<a id="c-net-6-or-later"></a>
### C#（.NET 6 或更高版本）

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

匿名对象会序列化为 `ProtocolVersion`/`Command`/`Arguments`，与所有者的期望完全一致。`ConnectAsync` 上出现 `TimeoutException`/`OperationCanceledException` 表示该根目录没有所有者。

<a id="powershell-7-pwsh"></a>
### PowerShell 7（`pwsh`）

`PipeOptions.CurrentUserOnly`、`SHA256.HashData` 和 `Convert.ToHexString` 需要 .NET 5 或更高版本，因此本示例需要 PowerShell 7；Windows PowerShell 5.1（.NET Framework）无法设置 `CurrentUserOnly`。

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

`ConvertTo-Json` 会保留哈希表键的大小写，因此 `ProtocolVersion`、`Command` 和 `Arguments` 会按原样输出。

<a id="using-the-cli-as-the-client"></a>
### 将 CLI 用作客户端

不需要自定义客户端时，可从安装目录调用 `OmsiLaunch.exe`：`session status --json`、`session stop`、`events read --json`、`events watch`、`time get --json`、`/runtime:d3d.status --json`。CLI 会执行状态查询/绑定握手，并将回复映射为[退出码](exit-codes.md)（`0` 成功，`2` 参数校验失败，`4` 没有所有者，`7` 被拒绝）。

<a id="relationship-to-other-channels"></a>
## 与其他通道的关系

- 所有者与进程内插件之间的运行时邮箱（内存映射、64 KiB、单飞、绑定会话）是一个独立的私有通道；控制平面只通过 `IOmsiLaunch.ExecuteRuntimeAsync` 向其转发。
- 安装租约（`Local\OmsiLaunch.Installation.<sha256(root)>`）是一个命名信号量，不属于本协议；它保证每个登录会话中每个安装实例只有一个所有者。
- 托盘图标的“End session”（结束会话）菜单项与控制平面的 `session.stop` 发出的是同一个所有者端停止请求；见 [Windows 托盘](windows-tray.md)。
