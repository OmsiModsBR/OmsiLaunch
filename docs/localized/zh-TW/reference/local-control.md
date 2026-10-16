# 本機控制平面

<!-- l10n: source=reference/local-control.md source-sha256=9bb135c4d6ffae86153c3c841d540c56f4d4304c6727f1ad142c02d2b30a3f86 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/local-control.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本機控制平面是一個具名管道（named pipe）端點，執行中的 OmsiLaunch 擁有者（啟動工作階段的處理程序）透過它接受同一部電腦上其他處理程序傳來的語意化工作階段命令：狀態、事件、停止，以及公開的執行階段操作。本頁依 `tools\OmsiLaunch.Cli\LocalControlPlane.cs` 中的實作以及 `OwnerSession.RunAsync`（`tools\OmsiLaunch.Cli\Program.cs`）中的要求處理常式，規範此端點：管道命名、訊框（frame）格式、協定版本、命令、工作階段繫結、錯誤碼、信任模型、可用時段，以及如何從其他工具與它溝通。CLI 用戶端命令（`session status`、`session stop`、`events read`、`events watch`，以及 `time get` 等路由）是此協定的薄包裝層；請參閱 [CLI 參考](cli.md)。執行階段操作本身規範於[執行階段控制](runtime-control.md)；供整合者使用的處理程序內替代方案是[公開 API](public-api.md)。

<a id="summary"></a>
## 摘要

| 屬性 | 值 |
|---|---|
| 傳輸 | Windows 具名管道，`PipeDirection.InOut`、位元組模式、`PipeOptions.Asynchronous | PipeOptions.CurrentUserOnly`、`MaxAllowedServerInstances`，輸入與輸出緩衝區各為一個訊框（65,540 位元組） |
| 管道名稱 | `OmsiLaunch.Control.0.1.<key>`，其中 `<key>` 是對安裝根目錄完整路徑（移除結尾的 `\` 並轉為大寫）之 UTF-8 位元組計算 SHA-256 後的前 16 個十六進位字元（`LocalControlPlane.PipeNameFor`） |
| 訊框格式 | 4 位元組 little-endian `int32` 長度前綴，後接該長度的 UTF-8 JSON 位元組；每個連線一個要求與一個回應 |
| 訊息上限 | 要求與回應皆為 65,536 位元組（`MaxMessageBytes`） |
| 協定版本 | `"0.1"`（`PublicCapabilityRegistry.ProtocolVersion`）；版本不符時以 `OL_E_CONTROL_PROTOCOL` 回應 |
| 命令 | `session.status`、`session.events`、`session.stop`、`runtime.execute` |
| 工作階段繫結 | `session.stop` 與 `runtime.execute` 要求 `Arguments.session_id` 等於使用中的工作階段 id |
| 可用性 | 從工作階段進入 `Running`（在任何驗證批次之後）的那一刻起，直到工作階段進入 `Completed` 或 `Failed` 且擁有者處置該端點為止 |
| 範圍 | 每個安裝根目錄一個端點；同一使用者的兩個 OMSI 安裝絕不共用管道 |
| 穩定性 | STABLE_BETA（離線測試位於 `OmsiLaunch.WindowsUiTests`；執行階段證據：RV-003 在長達數秒的原生呼叫期間同時查詢狀態、Round A RA-008，以及 runtime closure 原始訊框測試組 `R04`：17 個格式錯誤、過大、協定錯誤、未知命令、未繫結與工作階段錯誤的訊框，各自以其具型別的錯誤回應，之後皆接著一個成功的 `session.status`；停滯的用戶端不會阻擋其他用戶端；在生成進行中送出已繫結的停止會結束工作階段） |

<a id="pipe-name-derivation"></a>
## 管道名稱推導

```text
normalized = InstallationPaths.IdentityKey(root)   // upper-cased InstallationPaths.NormalizeRoot(root)
key        = HEX(SHA256(UTF8(normalized)))[0..16)
pipe       = "OmsiLaunch.Control.0.1." + key
full path  = \\.\pipe\OmsiLaunch.Control.0.1.<key>
```

`InstallationPaths.NormalizeRoot` 是安裝識別的唯一定義，安裝租約也使用它：它會解析完整路徑（包括 `.` 與 `..` 區段、`/` 或 `\`、重複的分隔符號），並移除結尾的分隔符號（磁碟機根目錄除外）。`D:\OMSI 2`、`D:\OMSI 2\`、`D:\OMSI 2\.`、`D:\x\..\OMSI 2` 與 `d:\omsi 2` 會產生相同的名稱；`D:\OMSI 2` 與 `E:\OMSI 2` 會產生不同的名稱。除非 API 呼叫端傳入明確的根目錄，否則用戶端一律從其所執行之執行檔所在的 OMSI 安裝（`AppContext.BaseDirectory`）推導名稱。名稱中的 `0.1` 是協定版本，因此未來的協定可以在同一部電腦上並存。

<a id="framing-and-encoding"></a>
## 訊框格式與編碼

- 寫入端：以 `System.Text.Json` 預設選項序列化記錄，檢查 `length <= 65536`（否則為 `OL_E_CONTROL_MESSAGE_TOO_LARGE`），寫入 `BitConverter.GetBytes((int)length)`（4 位元組，在 Windows 上為 little-endian），寫入酬載，然後排清。
- 讀取端：精確讀取 4 位元組；以 `OL_E_CONTROL_MESSAGE_INVALID` 拒絕 `length < 0` 或 `length > 65536`；精確讀取 `length` 位元組；還原序列化。在讀完完整訊框之前就關閉的連線，會被擁有者靜默捨棄（不回覆）。即使用戶端寫入的位元組已多於擁有者讀取的量，被拒絕的訊框仍會得到回應：管道緩衝區可容納整個訊框，因此用戶端的寫入會完成並能讀取回覆（runtime closure BUG-02；修正之前，此類用戶端與擁有者都會在各自的寫入中阻塞）。
- 擁有者在 5 s 期限內寫入每個回覆：連線、送出要求卻從不讀取回覆的用戶端，佔用擁有者工作的時間不會超過此期限。
- 要求中的屬性名稱為 **PascalCase 且區分大小寫**（`ProtocolVersion`、`Command`、`Arguments`），因為擁有者以預設選項還原序列化。回應使用 `Ok`、`Result`、`ErrorCode`、`Message`。列舉值序列化為整數；`Guid` 序列化為字串。
- 每個連線一個要求：擁有者讀取一個要求、寫入一個回應，然後關閉管道。每個要求都要開啟新的連線。

<a id="request"></a>
### 要求

```json
{"ProtocolVersion": "0.1", "Command": "runtime.execute", "Arguments": {"operation": "time.read", "session_id": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da"}}
```

`session_id` 必須是 `session.status` 針對執行中工作階段所回傳的 id（上述值取自實際工作階段）。

對 `session.status` 與 `session.events` 而言，`Arguments` 為選用（`null` 或省略）。參數鍵以序數方式比較（區分大小寫）。

<a id="response"></a>
### 回應

以下訊框擷取自實際的擁有者（runtime closure `R04`，最終套件）。成功的 `runtime.execute`：

```json
{"Ok": true, "Result": {"SessionId": "9700ba92-d6ef-4b94-acec-d2b0b5aec1da", "RequestId": 50001, "Succeeded": true, "ErrorCode": null, "Values": {"hour": "14", "minute": "58", "second": "18.921843", "day": "28", "month": "9", "year": "2000"}}, "ErrorCode": null, "Message": null, "Metadata": null}
```

未帶使用中 `session_id` 而被拒絕的 `session.stop`：

```json
{"Ok": false, "Result": null, "ErrorCode": "OL_E_CONTROL_SESSION_MISMATCH", "Message": "session.stop requires the active session_id.", "Metadata": null}
```

<a id="commands"></a>
## 命令

| 命令 | 參數 | `Ok=true` 時的結果 | 附註 |
|---|---|---|---|
| `session.status` | 無 | `SessionStatus`：`SessionId`（字串 GUID）、`State`（整數 `SessionState`；`14` = `Running`、`15` = `ProcessExited`、`16` = `Restoring`、`18` = `Completed`、`19` = `Failed`）、`Diagnostics`（`{Code, Message, Data}` 陣列）、`RuntimeEvents`（陣列） | 唯讀。用戶端會先讀取此命令以取得 `SessionId`。 |
| `session.events` | 無 | `RuntimeEvent` 陣列：`Type`、`TimestampUtc`、`Sequence`（單調遞增的 `int64`）、`Data`（字串對應） | 唯讀，有界清單；`events watch` 會輪詢它，並輸出 `Sequence` 大於上次所見值的項目。 |
| `session.stop` | `session_id`（必要） | `{"accepted": true, "session_id": "..."}` | 要求標準停止（對 OMSI 執行 `TerminateProcess`，然後還原）。會立即回傳；在端點消失之前，可透過 `session.status` 觀察工作階段狀態。 |
| `runtime.execute` | `operation`（必要）、`session_id`（必要），以及該操作本身的參數（`handle`、`name`、`value`、`model`、`family`……） | `RuntimeCommandResult`：`SessionId`、`RequestId`（由擁有者指派，從 `50001` 開始）、`Succeeded`、`ErrorCode`、`Values`（字串對應） | 執行前以 `PublicCapabilityRegistry.ValidateRuntimeArguments` 驗證：未知或內部操作 → `OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必要值 → `OL_E_RUNTIME_ARGUMENT_REQUIRED`。逾時 8 s，`road-vehicles.spawn` 為 30 s。執行階段失敗會以 `Ok=false` 回應，其中 `Result` 設為該 `RuntimeCommandResult`、`ErrorCode` 設為其錯誤碼，`Message` 為 `Runtime operation was rejected.`。以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的結果鍵，會在到達管道之前由 API 移除。 |
| 其他任何命令 | | `Ok=false`、`OL_E_CONTROL_COMMAND_UNKNOWN` | |

擁有者會並行處理連線（每個被接受的用戶端都在各自的工作上處理），因此在 `road-vehicles.spawn` 這類長達數秒的原生呼叫進行期間，`session.status` 仍會持續回應。

<a id="session-id-binding"></a>
## 工作階段 id 繫結

會變更狀態的命令必須指明其作用的工作階段。CLI 實作了 `TryRequestBoundAsync`：它送出 `session.status`（750 ms），取得 `Result.SessionId`，然後在 `Arguments` 中加入 `session_id` 重送要求。id 缺少、無法剖析或不同時，會以 `OL_E_CONTROL_SESSION_MISMATCH` 回應。若擁有者未回報工作階段 id，用戶端會回報 `OL_E_CONTROL_PROTOCOL`。

<a id="reply-metadata-and-truncated-event-history"></a>
### 回覆中繼資料與截斷的事件歷程

除非回覆帶有關於其自身的事實，否則 `Metadata` 為 `null`。`session.status` 與 `session.events` 會回傳擁有者的事件歷程（最多 256 個事件，最新的在最後）。當該歷程無法放入一個 64 KiB 訊框時，會移除最舊的事件直到能夠容納，並由 `Metadata` 說明：

| 鍵 | 意義 |
|---|---|
| `events_truncated` | `true` |
| `events_returned_count` | 此回覆中的事件數 |
| `events_dropped_count` | 為容納於訊框而移除的最舊事件數 |
| `events_available_count` | 擁有者為此回覆所保有的事件數 |
| `events_first_returned_sequence` | 第一個回傳事件的 `Sequence` |

沒有 `Metadata` 時，表示回覆未被控制平面截斷。兩種情況下 `Sequence` 都是單調遞增的，因此用戶端也能偵測到擁有者本身因超出 256 個事件歷程而逐出的事件。CLI 會在 `--json` 封套中輸出此中繼資料，在文字模式中則輸出一則附註。

<a id="error-codes"></a>
## 錯誤碼

| 錯誤碼 | 來源 | 意義 |
|---|---|---|
| `OL_E_CONTROL_PROTOCOL` | 擁有者／用戶端 | 要求的 `ProtocolVersion` 不是 `0.1`；擁有者的回覆無法解碼或為空；擁有者未回覆即關閉連線；連線建立後中斷；或擁有者未回報工作階段 id。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | 擁有者／用戶端 | 長度前綴超出範圍（過大的要求訊框會以此方式在讀取其酬載之前被拒絕）、空訊框、JSON `null`、沒有 `Command` 的要求，或無法解碼的 JSON。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | 用戶端寫入端 | 用戶端本身的要求超過 65,536 位元組；此錯誤回報給用戶端，且不會送出任何內容。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | 擁有者寫入端 | 擁有者的回覆超過 65,536 位元組；擁有者以此具型別的錯誤回應。`session.status` 與 `session.events` 會修剪其事件歷程（從最舊的開始）以容納，因此不會產生此錯誤。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | 擁有者 | `session.stop`／`runtime.execute` 未帶使用中的 `session_id`。 |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | 擁有者 | 不支援的命令名稱。 |
| `OL_E_CONTROL_HANDLER_FAILED` | 擁有者 | 處理常式的回覆無法序列化，或處理常式擲回的例外訊息中沒有 `OL_E_` 錯誤碼；若有錯誤碼，則改為回傳該錯誤碼（例如 OMSI 結束後由 `ExecuteRuntimeAsync` 產生的 `OL_E_SESSION_NOT_RUNNING`）。 |
| `OL_E_CONTROL_FAILED` | CLI 用戶端 | 回覆為 `Ok=false` 但沒有 `ErrorCode` 時的預設錯誤碼。 |
| `OL_E_TIMEOUT` | 用戶端 | 用戶端已連線到擁有者，但擁有者未在逾時內回覆。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI 用戶端 | 無法連到任何擁有者端點（`TryRequestAsync` 僅在連線本身失敗時回傳 `null`）。連線之後，每一種失敗都是上述具型別錯誤碼之一。結束代碼 `4`。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 用戶端與擁有者 | `runtime.execute` 的公開介面驗證（CLI 中結束代碼為 `2`）。 |

每一種已知的失敗都會以具型別的錯誤回應，因此用戶端絕不會把故障誤認為擁有者不存在。有兩種情況會在沒有回覆訊框的情形下結束：用戶端在送出完整要求之前就中斷連線（沒有可回應的對象），以及工作階段結束時仍在處理中的要求（擁有者在關閉端點時會取消待處理的回覆；用戶端會看到資料流結束，CLI 用戶端將其回報為 `OL_E_CONTROL_PROTOCOL`，訊息為 "The owner closed the connection without a reply"（擁有者未回覆即關閉了連線））。送出要求後、讀取回覆前就中斷連線的用戶端不會影響擁有者。只要該 OMSI 安裝的端點傳回任何回覆（包括具型別的錯誤），擁有者的啟動就會拒絕繼續，因為有回覆即證明已有擁有者存在。CLI 將回覆對應到結束代碼的方式請參閱[結束代碼](exit-codes.md)。

<a id="trust-model-accepted-risk"></a>
## 信任模型（已接受的風險）

- `PipeOptions.CurrentUserOnly` 將管道限制於擁有該工作階段的 Windows 使用者（及完整性等級）；用戶端的同一選項則會驗證伺服器是否由同一使用者擁有。
- **以同一 Windows 使用者身分執行的任何處理程序，都能讀取狀態、停止工作階段並執行公開的執行階段操作。**沒有額外的驗證、權杖或個別用戶端授權。這是本 Beta 版已記載並已接受的風險；請勿在與不受信任軟體共用的帳戶下執行 OmsiLaunch 工作階段。
- 管道絕不公開原生位址、原生 handle 或原始記憶體操作；只接受 `PublicCapabilityRegistry` 的公開操作 id，內部研究用基本操作（`internal.*`）會在任何工作階段查詢之前被拒絕。
- 訊息有大小上限（64 KiB）且以訊框封裝；格式錯誤或過大的訊框會被回應或捨棄，而不影響工作階段。
- 若擁有者開始接聽時管道名稱已被其他處理程序占用（建立時發生 `IOException`／`UnauthorizedAccessException`），擁有者會在沒有端點的情況下繼續執行工作階段，並將此故障記錄於 `LocalControlPlane.ListenFault`；用戶端此時會看到 `OL_E_NO_ACTIVE_SESSION`。請使用系統匣圖示或 Ctrl+C 結束此類工作階段。

<a id="availability-window"></a>
## 可用時段

1. `StartSessionAsync` 執行中；端點尚不存在。針對同一根目錄的第二次 `OmsiLaunch.exe` 啟動會在開始前以 250 ms 探測 `session.status`，只有在已有擁有者回應時才以 `OL_E_SESSION_ALREADY_ACTIVE` 失敗；啟動期間競爭的兩個擁有者由安裝租約序列化（`OL_E_INSTALLATION_BUSY`）。
2. 工作階段進入 `Running`；若有要求驗證批次（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`），則這些批次完成。
3. `LocalControlPlane.Start()`：端點開始接受連線。擁有者自身的 `/runtime:` 操作（若有）在此時間點之後執行。
4. 端點在 `ProcessExited`、`Restoring` 與 `CleaningRuntime` 期間持續存在（狀態會回報這些狀態），直到工作階段為 `Completed` 或 `Failed`。
5. `DisposeAsync` 會取消接聽程式並等待處理中的用戶端工作，然後擁有者呼叫 `CloseAsync`。之後管道名稱即不再存在。

用戶端應使用較短的連線逾時（CLI 對 status／stop／events 使用 750 ms），並將「沒有端點」視為「此 OMSI 安裝沒有使用中的工作階段」。

<a id="using-the-protocol-from-other-tooling"></a>
## 從其他工具使用此協定

<a id="c-net-6-or-later"></a>
### C#（.NET 6 或更新版本）

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

匿名物件會序列化為 `ProtocolVersion`／`Command`／`Arguments`，完全符合擁有者的預期。`ConnectAsync` 發生 `TimeoutException`／`OperationCanceledException` 表示該根目錄沒有擁有者。

<a id="powershell-7-pwsh"></a>
### PowerShell 7（`pwsh`）

`PipeOptions.CurrentUserOnly`、`SHA256.HashData` 與 `Convert.ToHexString` 需要 .NET 5 或更新版本，因此此範例需要 PowerShell 7；Windows PowerShell 5.1（.NET Framework）無法設定 `CurrentUserOnly`。

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

`ConvertTo-Json` 會保留雜湊表鍵的大小寫，因此 `ProtocolVersion`、`Command` 與 `Arguments` 會依原本寫法輸出。

<a id="using-the-cli-as-the-client"></a>
### 以 CLI 作為用戶端

不需要自訂用戶端時，請從安裝目錄呼叫 `OmsiLaunch.exe`：`session status --json`、`session stop`、`events read --json`、`events watch`、`time get --json`、`/runtime:d3d.status --json`。CLI 會執行狀態／繫結交握，並將回覆對應到[結束代碼](exit-codes.md)（`0` 成功、`2` 參數驗證、`4` 沒有擁有者、`7` 遭拒絕）。

<a id="relationship-to-other-channels"></a>
## 與其他通道的關係

- 擁有者與處理程序內外掛程式之間的執行階段信箱（記憶體對應、64 KiB、單一飛行、繫結工作階段）是獨立的私有通道；控制平面僅透過 `IOmsiLaunch.ExecuteRuntimeAsync` 將要求轉送給它。
- 安裝租約（`Local\OmsiLaunch.Installation.<sha256(root)>`）是具名旗號（named semaphore），不屬於此協定；它保證每個登入工作階段中每個 OMSI 安裝只有一個擁有者。
- 系統匣圖示的「End session」與控制平面的 `session.stop` 會發出同一個擁有者端停止要求；請參閱 [Windows 系統匣](windows-tray.md)。
