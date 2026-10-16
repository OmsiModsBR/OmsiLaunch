# 執行階段控制

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/runtime-control.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

執行階段控制是 OmsiLaunch 在執行中的 OMSI 工作階段內所執行之讀取、寫入與動作操作的集合。本頁說明存取它的三種方式（擁有者 API、CLI 用戶端、本機控制平面）、要求如何從呼叫端傳送到外掛程式再傳回、handle 與要求識別如何運作、適用哪些逾時、哪些內容刻意不記入日誌，以及參數與結果的慣例，並為每個系列提供實作範例。操作清單本身（含參數、結果鍵與錯誤）請參閱[功能](capabilities.md)。來源：`OmsiLaunchService.ExecuteRuntimeAsync`、`CurrentRuntimeCommandStore`（`src/OmsiLaunch.Process/RuntimeDeployment.cs`）、`CurrentRuntimeCommandMailbox` 與 `CurrentRuntimeControl`（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`）、`LocalControlPlane` 與 `CliInput`（`tools/OmsiLaunch.Cli/`）。

<a id="three-entry-points"></a>
## 三個進入點

| 進入點 | 使用者 | 路徑 | 逾時 |
| --- | --- | --- | --- |
| 擁有者 API | 以 `IOmsiLaunch.StartSessionAsync` 在處理程序內啟動工作階段的整合者 | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> 登錄驗證 -> 工作階段查詢 -> 信箱 | 由呼叫端提供的 `TimeSpan` |
| 擁有者 CLI（`/runtime:<op>`） | 擁有該工作階段的 `OmsiLaunch.exe` 處理程序 | 在進入 `Running` 後立即執行一項操作，結果寫入 `.omsilaunch\diagnostics\<session>-runtime-operation.json` 並輸出到主控台；工作階段繼續執行 | 5 s（`road-vehicles.spawn` 為 15 s） |
| CLI 用戶端 | **不帶**安裝參數的任何 `OmsiLaunch.exe` 呼叫，例如 `OmsiLaunch.exe time get` | 透過具名管道（named pipe）`runtime.execute` 傳送給執行檔所在 OMSI 安裝的擁有者 -> 擁有者呼叫 `ExecuteRuntimeAsync` | 用戶端與擁有者端皆為 8 s（`road-vehicles.spawn` 為 30 s） |
| 本機控制平面 | 同一 Windows 使用者的任何處理程序 | 與 CLI 用戶端相同的管道協定；請參閱[本機控制](local-control.md) | 同上 |

每條路徑最後都會進入 `ExecuteRuntimeAsync`，它依下列順序強制執行公開邊界：

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`：不在 `PublicRuntimeOperationIds` 中的操作（包括所有 `internal.*` 操作）會回傳 `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必要參數或必要參數為空白時回傳 `OL_E_RUNTIME_ARGUMENT_REQUIRED`。兩者都不會觸及工作階段。
2. 工作階段查詢（未知的 handle 會引發 `KeyNotFoundException`）；`RuntimeCommand.SessionId` 與 handle 不同時為 `OL_E_RUNTIME_SESSION_MISMATCH`；狀態不是 `Running` 時為 `OL_E_SESSION_NOT_RUNNING`。
3. 信箱要求（見下文），之後 `ScrubInternalValues` 會移除任何以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的結果鍵。

CLI 用戶端與控制平面會在聯絡擁有者之前自行執行步驟 1，因此即使沒有使用中的工作階段，無效的命令名稱也會回報為結束代碼 2（`OL_E_RUNTIME_OPERATION_UNKNOWN` 與 `OL_E_RUNTIME_ARGUMENT_REQUIRED` 對應至 `InvalidArguments`；其他拒絕對應至 7；沒有擁有者對應至 4）。

控制平面端點只在擁有者處於 `Running` 時存在，也就是啟動完成且任何 `/runtime-batch`、`/runtime-write-batch` 或 `/d3d-batch` 測試批次都已完成之後。會變更狀態的命令（`runtime.execute`、`session.stop`）必須帶有使用中的 `session_id`；CLI 會自動從 `session.status` 取得它（否則為 `OL_E_CONTROL_SESSION_MISMATCH`）。

<a id="request-identity-and-the-mailbox"></a>
## 要求識別與信箱

`RuntimeCommand(SessionId, RequestId, Operation, Arguments)` 會序列化成 `RuntimeCommandWire` 封套（magic `OLRC`、版本 1、72 位元組標頭、JSON 酬載的 SHA-256），並放入該工作階段的 64 KiB 單一飛行（single-flight）信箱（`OmsiLaunch.Runtime.<sessionId>`）。外掛程式每 50 ms 在 OMSI 的 UI 執行緒上輪詢信箱，在該執行緒上執行操作並寫入回應。

讓通道穩健的規則：

| 規則 | 效果 |
| --- | --- |
| 單一飛行 | 每個工作階段一次只處理一個要求；主機以旗號（semaphore）將呼叫端序列化。新要求抵達時，若槽位仍為 `requested`，則為 `OL_E_RUNTIME_CHANNEL_BUSY`。 |
| 工作階段繫結 | 外掛程式會忽略（並清除）工作階段 GUID 不是自己的要求；主機會拒絕工作階段或要求 id 不相符的回應（`OL_E_RUNTIME_RESPONSE_INVALID`）。 |
| 逾時 | 超過期限時，主機會將槽位重設為閒置並擲回 `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`。 |
| 延遲回應 | 外掛程式只有在槽位仍保有其要求 id 時才會發布回應；對已放棄之要求的回應會被捨棄。若仍有此類回應寫入，下一個主機要求會發現過時的 `responded` 槽位，將其捨棄後繼續；若該過時回應帶有與新要求**相同**的要求 id，主機會擲回 `OL_E_RUNTIME_REQUEST_ID_REUSED`。因此呼叫端絕不可在同一工作階段內重複使用要求 id。 |
| 大小 | 大於槽位的要求會在放入之前被拒絕（`ArgumentOutOfRangeException`）。無法容納的清單頁面會由外掛程式縮短（捨棄最後的資料列、`truncated=true`、較小的 `returned_count`、`next_cursor` 指向第一個被捨棄的資料列）；其他任何過大的回應則會被替換為具型別的 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 錯誤。 |
| 通道已關閉 | 工作階段結束後為 `OL_E_RUNTIME_CHANNEL_CLOSED`。 |

要求 id 是由呼叫端選擇的 `ulong`。CLI 使用固定範圍：`/runtime:` 為 `10_001`，轉送的控制平面要求為 `50_001+`，讀取批次為 `1+`，D3D 批次為 `20_000+`，`D3DRuntimeApi` 為 `30_000+`。整合者應在每個工作階段使用單調遞增的計數器。

<a id="handles-and-stale-detection"></a>
## Handle 與失效偵測

| 前綴 | 型別 | 發出者 | 格式 |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`、`road-vehicles.spawn`（`created_handle`）、`player-vehicle.read` | `rv-` + 六位十進位數字（`rv-000003`） |
| `hb-` | Human | `humans.list` | `hb-` + 六位十進位數字 |
| `d3dtex-` | D3D 材質 | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

Handle（控制代碼）是不透明且以工作階段為範圍的：它們由外掛程式產生，從不編碼位址，在其他工作階段中沒有意義。解析 handle 時，外掛程式會檢查其對應的位址是否仍在 OMSI 的即時集合中（以最近一次清單讀取為準；`road-vehicles.list` 與 `humans.list` 會重新整理此檢視），並重新讀取物件指紋（Delphi VMT 加上車輛定義指標，或人物模型索引）。不相符表示原生物件已被銷毀且其位址被重複使用：`OL_E_RUNTIME_OBJECT_HANDLE_STALE`。對同一個存活中的物件，重複讀取清單時會再次回傳相同的語彙單元。殘留盲點：在兩次清單讀取之間，於同一位址重新建立之相同類別與定義的物件無法被區分。D3D handle 由原生橋接層驗證：未知或屬於其他工作階段的 handle 為 `OL_E_D3D_STALE_RESOURCE_HANDLE`，已釋放的為 `OL_E_D3D_RESOURCE_RELEASED`，裝置世代變更則會將材質標示為 `STALE`。

Handle 絕不是原生位址，絕不可剖析、以數值比較、跨工作階段保存或傳給另一個工作階段：請將它們視為僅在發出它們的工作階段內有效的不透明字串。

執行階段證據：已釋放的 D3D handle 在建立新材質之後仍會被拒絕，而前一個工作階段的 handle 會被下一個工作階段拒絕（runtime closure `H02`）；D3D 裝置重設會使所有存活中的材質失效（`OL_E_D3D_STALE_RESOURCE_HANDLE`、`D01`）。RoadVehicle 與 Human 在自然移除後的失效偵測（RV-002）沒有安全的執行階段產生方式（OMSI 在觀察時段內沒有移除物件，也沒有任何公開操作能移除物件）；它以離線測試涵蓋。

<a id="what-is-not-journaled"></a>
## 哪些內容不記入日誌

執行階段變更只會改變 OMSI 的記憶體。**它們不會記錄在交易日誌中，也不會被還原**（在工作階段結束時）：`time.set`、`camera.set`、`camera.lock`（外掛程式關閉時原則即停止）、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random` 以及所有 `d3d.texture.*` 資源。它們會隨 OMSI 處理程序一起消失；OmsiLaunch 在工作階段結束時會終止該處理程序，不讓 OMSI 保存任何內容（請參閱[交易與復原](../concepts/transactions-and-recovery.md)）。執行階段系列中沒有任何操作會觸及檔案系統。

<a id="argument-and-result-conventions"></a>
## 參數與結果慣例

- 參數是字串鍵／值組。在 CLI 上，命令字之後的 `--key=value` 會成為執行階段參數（`OmsiLaunch.exe vehicles get --handle=rv-000001`）；`/runtime-arg:key=value` 是 `/runtime:<op>` 的對等寫法。數字使用不變文化特性（小數分隔符號為 `.`）；布林值為 `true`/`false`。
- 命令字透過 `CliInput.HierarchicalRoutes` 對應到操作 id（例如 `time get` -> `time.read`、`vehicles summary` -> `road-vehicles.read`、`scripts variable set` -> `vehicle.variable.set`）。完整路由表請參閱 [CLI 參考](cli.md)。D3D 操作沒有命令字路由；請使用 `/runtime:d3d.status` 或 `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`。
- 結果是扁平的字串字典。清單使用 `<row>.<n>.<field>` 鍵（`vehicle.0.handle`、`track.3.filename`、`name.12`）並附頁面欄位 `count`、`returned_count`、`truncated` 與 `next_cursor`（請參閱[分頁](capabilities.md#pagination)）。
- 失敗會帶有 `Succeeded=false`、`OL_E_` 形式的 `ErrorCode`，外掛程式端失敗還會帶有 `detail`（D3D 另有 `native_status`，非預期錯誤另有 `exception`）。

<a id="cli-envelope---json"></a>
### CLI 封套（`--json`）

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

錯誤封套為 `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`。不使用 `--json` 時，CLI 會將結果物件以縮排 JSON 或 `CODE: message` 形式輸出。

<a id="api-envelope"></a>
### API 封套

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` 對登錄驗證之後的邊界違規會擲回例外（`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_*`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_RESPONSE_INVALID`），對登錄拒絕與外掛程式端錯誤則回傳 `Succeeded=false`。

<a id="worked-examples"></a>
## 實作範例

所有 CLI 範例都假設包含 `OmsiLaunch.exe` 的 OMSI 安裝已有擁有者在執行（例如以 `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` 啟動，範例需要玩家車輛時則以 `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` 啟動），並從同一安裝中的第二個主控台執行。

| 系列 | 命令 | 作用 |
| --- | --- | --- |
| 工作階段 | `OmsiLaunch.exe session status --json` | 讀取 `SessionId`、`State`、診斷訊息與有界的執行階段事件清單。 |
| 時間 | `OmsiLaunch.exe time get`，接著 `OmsiLaunch.exe time set --hour=7 --minute=30` | 讀取時鐘；透過經設定檔定義的 `SetTime` 設定時間並回傳回讀值。 |
| 天氣 | `OmsiLaunch.exe weather get` 與 `OmsiLaunch.exe weather actual get` | 讀取目前天氣與實際／ICAO 天氣狀態。`OmsiLaunch.exe weather set --wind_speed=5` 會設定手動天氣（OMSI 的來源記錄；即時 METAR 停止），`OmsiLaunch.exe weather metar set --icao=EDDF` 則切換為即時 METAR 天氣。兩者在完成執行階段驗證之前皆為 EXPERIMENTAL。 |
| 地圖 | `OmsiLaunch.exe map get` | 地圖識別、名稱、說明、圖塊數、年份範圍與行車方向。 |
| 攝影機 | `OmsiLaunch.exe camera set --field_of_view=50`，接著 `OmsiLaunch.exe camera lock --family=0 --preset=1` 與 `OmsiLaunch.exe camera unlock` | 寫入 FOV；以預設組合（preset）1 固定駕駛員 family（需要玩家車輛，例如已儲存情境）；解除原則。 |
| 車輛 | `OmsiLaunch.exe vehicles summary`、`OmsiLaunch.exe vehicles list`、`OmsiLaunch.exe vehicles get --handle=rv-000001` | 數量；handle；單一快照。 |
| 生成 | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | 建立一輛 AI RoadVehicle（30 s 逾時）；回傳 `created_handle`。`OmsiLaunch.exe vehicles place-random --group=1` 會呼叫 `PlaceRandomBus`。 |
| 玩家 | `OmsiLaunch.exe player get` | 無頭啟動時為 `present=false`，否則為玩家快照。 |
| 人物 | `OmsiLaunch.exe humans summary`、`OmsiLaunch.exe humans list`、`OmsiLaunch.exe humans get --handle=hb-000001` | 數量；handle；單一快照。 |
| 時刻表 | `OmsiLaunch.exe timetable get`、`OmsiLaunch.exe timetable tracks list`、`OmsiLaunch.exe timetable logs list` | 管理器計數；有界的路線資料列；時刻表記錄。 |
| 腳本 | `OmsiLaunch.exe scripts variable list --handle=rv-000001`、`OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`、`OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`、`OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`、`OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`、`OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | 數值變數的列出／讀取／寫入；字串變數讀取／寫入；指令碼與音效 trigger（字串寫入與 trigger 在完成執行階段驗證之前為 EXPERIMENTAL）。 |
| 常數與曲線 | `OmsiLaunch.exe constants list --handle=rv-000001`、`OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`、`OmsiLaunch.exe curves list --handle=rv-000001`、`OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | 車輛常數與曲線求值。名稱因車型而異：請使用 `list` 命令回傳的名稱（這些名稱是針對 `situations\Linie 5.osn` 的玩家巴士列出的）。 |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | 車輛定義的 HOF 中繼資料。 |
| 駕駛員與車票 | `OmsiLaunch.exe drivers list`、`OmsiLaunch.exe tickets get` | 駕駛員記錄；車票套組。 |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`，接著 `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` 與 `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | 在轉譯執行緒上的材質生命週期；`<HANDLE>` 是 `create` 輸出的 `handle`；`--pixels_base64` 解碼後必須為 `width * height * 4` 位元組（32 位元格式），且最多 48 KiB。 |
| 事件 | `OmsiLaunch.exe events read`、`OmsiLaunch.exe events watch` | 有界事件清單；輪詢式監看（250 ms），直到按下 Ctrl+C。 |
| 停止 | `OmsiLaunch.exe session stop` | 要求標準停止：由擁有者終止 OMSI 並還原交易。 |

<a id="stability"></a>
## 穩定性

通道本身（`runtime.command-channel`）為 `STABLE_BETA`：傳輸完整性、工作階段繫結、延遲回應捨棄與具型別的過大拒絕，已由 `runtime-command.wire-guard`、`runtime-command.session-binding` 與 `runtime-command.late-response-ignored` 以離線方式涵蓋，並在執行階段由 RV-003（工作階段 `5f641c8d`）、Round A RA-019（用戶端在 250 ms 後放棄 `road-vehicles.spawn`；下一個要求成功）以及 runtime closure 通道測試組 `R03`（逾時、取消、重複使用的要求 id、外掛程式拒絕、內部操作、錯誤的工作階段與缺少參數，每一項之後都接著一個成功的要求）涵蓋。各操作的穩定性請參閱[功能](capabilities.md)。

<a id="channel-reuse-after-failures"></a>
## 失敗後的通道重複使用

執行階段要求的每一條終止路徑都會讓信箱保持可重複使用：成功、具型別的錯誤、格式錯誤、過大、屬於其他工作階段或要求 id 錯誤的回應、逾時、呼叫端取消與解碼失敗，最後都會進入同一段清理程序，將槽位恢復為閒置並清除長度與封套標頭，因此先前要求的任何內容都不會被下一個要求讀到。新要求開始時若發現殘留的要求或回應，會先將其清除（若它帶有新要求的 id，則為 `OL_E_RUNTIME_REQUEST_ID_REUSED`）。在外掛程式端，操作內發生的例外會以 `OL_E_RUNTIME_OPERATION_FAILED` 回應，大於槽位的結果以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（固定的小型封套）回應，屬於其他工作階段的要求以 `OL_E_RUNTIME_SESSION_MISMATCH` 回應，而主機已放棄的要求則永遠不會被回應。這些規則已以離線方式涵蓋（`runtime-command.terminal-paths-leave-channel-usable`、`plugin-runtime.oversized-and-abandoned-responses`、`plugin-runtime.bounded-list-fits-slot`）；呼叫端能夠產生的路徑（逾時、取消、重複使用的 id、具型別的拒絕、延遲回應）也有執行階段證據（RA-019、`R03`）。損毀、屬於其他工作階段或過大的回應無法從產品外部產生，因此仍僅以離線方式驗證。
