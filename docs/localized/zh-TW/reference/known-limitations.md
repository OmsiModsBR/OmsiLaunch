# 已知限制

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/known-limitations.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁根據程式碼，列出 OmsiLaunch 0.1.0-beta4 中所有屬於 `UNAVAILABLE`、`PARTIAL` 或已接受風險的項目，讓使用者與整合者不會建立在產品並未提供的行為之上。每一列說明該限制、其穩定性、存在原因，以及詳細說明所在的位置。英文文件為規範版本；`docs/localized/` 底下的在地化副本並未以相同程度維護，內容可能落後（請見最後一節）。

<a id="compatibility"></a>
## 相容性

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 讀取物件配置的功能（車輛、人物、天氣、地圖、攝影機、時刻表、指令碼、駕駛員、車票）只在已知配置（`Omsi23004`）上驗證；在另一個 OMSI 2 組建上，在結構驗證器出現之前它們無法使用（`OL_E_CAPABILITY_UNRESOLVED`） | 未知組建上為 `PARTIAL` | [相容性](compatibility.md) |
| 真正的 Steam 執行檔（受 DRM 保護）尚未經過執行階段驗證；受控的 LAA 副本則已驗證 | Steam 為 `PARTIAL` | [相容性](compatibility.md) |
| 只有已知配置經過執行階段驗證；與組建無關的解析是針對隱藏其識別的參考執行檔離線驗證 | 未知組建上為 `EXPERIMENTAL` | [執行階段驗證狀態](../status/runtime-validation-status.md) |
| 僅支援 Windows 10+ x64；不支援 Windows 7/8、XP 或 ARM64 | `UNAVAILABLE` | [相容性](compatibility.md) |
| 除了控制器使用的 x64 runtime 之外，外掛程式還需要 **x86** .NET 6 runtime | | [相容性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## 世界啟動與啟動選項

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last /map:<identity>`）依據對 OMSI 最後狀態分支的靜態分析實作，且僅離線驗證 | `EXPERIMENTAL` | [CLI](cli.md) |
| 明確指定或系統的日期、時間與年份（`/date`、`/time`、`/year`、設定檔 `new.date`/`new.time`/`new.year`）僅適用於 NEW_MAP 啟動，並已離線驗證；執行階段驗證待完成。閏年的 12 月 31 日無法要求（OMSI 的日曆會在第 366 天繞回） | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| 啟動時的天氣預設組合（preset）、ICAO 與即時實際天氣（`/weather*`、`new.weather`）會在世界啟動後立即透過 OMSI 自己的天氣載入與 METAR 旗標套用（已靜態收尾，BI-003）；執行階段驗證待完成。OMSI 在最初一格畫面顯示的天氣仍可能是地圖的預設值 | `EXPERIMENTAL` | [CLI](cli.md) |
| 啟動時的玩家車輛型號、塗裝（repaint）、HOF、車隊編號、車牌號碼（`/vehicle` 系列、`PlayerVehicleSpec`）會依內容目錄解析但不會套用；要求它們會使計畫不可執行。原生指派序列（透過 OMSI 的位置對話方塊置放、`MakeVehicle`、每個單元的收尾步驟、`CopyTempListIntoMainList`、玩家索引）已靜態收尾，但它會建立並登錄一輛即時車輛，在能於執行階段驗證之前不予實作；預計於後續 Beta 提供。已儲存情境或 `/last` 會還原其自身的玩家車輛 | `UNAVAILABLE`（BI-007） | [功能](capabilities.md)中的 `player.assign-headless` |
| 依識別指定進入點（`/entrypoint:<identity>`）會選取 OMSI 所呈現的精確標籤（已靜態收尾，BI-001）；與 OMSI 自己的對話方塊相同，共用標籤的後續記錄無法被選取。執行階段驗證待完成 | `EXPERIMENTAL` | 計畫功能 `world.entrypoint-identity` |
| 依索引鍵設定的鍵盤與控制器設定（`Environment.Keyboard`、`Environment.Controllers`）沒有已編入目錄的索引鍵：請改用 `InputSpec` 取代整個檔案 | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| 協同式關閉（`/cooperative-shutdown`、`Behavior.CooperativeShutdown`）為選用，且僅離線驗證；預設情況下，工作階段結束時 OMSI 會被終止，`ShutdownTimeoutSeconds` 沒有作用 | `EXPERIMENTAL` | [CLI](cli.md) |
| 診斷旗標 `/log`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process` 會填入 `DiagnosticsSpec`，沒有進一步的效果（`.omsilaunch\diagnostics` 下的主機追蹤一律會寫入）；`/trace-plugin` 與 `/trace-native` 會將外掛程式與原生追蹤事件加入其中 | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` 是驗證測試工具 | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## 工作階段結束與處理程序控制

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 工作階段停止是強制終止：`session.stop`、系統匣的「End session」、Ctrl+C 與 `CloseAsync` 最終都會呼叫 `TerminateProcess`。OMSI 的關閉程序不會執行，OMSI 在結束時不會重寫 `options.cfg` 或其記錄檔，任何未儲存的 OMSI 狀態都會遺失。這是刻意的設計：可避免 OMSI 覆寫已還原的檔案。 | 依設計 | [工作階段生命週期](../concepts/session-lifecycle.md) |
| 具逾時與終止備援機制的協同式 WM_CLOSE 關閉未實作 | `UNAVAILABLE`（產品決策，S-11；在執行階段收尾回合中，OMSI 忽略了送往其主視窗的 `WM_CLOSE`） | [執行階段驗證狀態](../status/runtime-validation-status.md) |
| 主控台關閉或登出時，擁有者有 4 s 的時間上限來停止並還原；未完成的部分會在下一次啟動時由日誌復原 | 主控台關閉已於執行階段驗證；登出未經實測 | [交易與復原](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## 交易、復原與租約

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 安裝租約是一個 `Local\` 號誌：**每個登入工作階段**中每個安裝只能有一個擁有者；不跨使用者強制執行；在其他處理程序持有 handle（控制代碼）時不會釋放；任何同一使用者的處理程序都能持有該名稱 | 已接受的風險（S-18） | [交易與復原](../concepts/transactions-and-recovery.md) |
| 當日誌記錄的 OMSI 處理程序（或對於沒有 PID 的日誌，該根目錄中的任何 `Omsi.exe`）正在執行時，復原會被拒絕（`OL_E_INSTALLATION_BUSY`） | 依設計 | 同上 |
| 原本不存在、但內容在工作階段期間被變更的 overlay 路徑，會阻擋還原（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`），直到經過檢查為止 | 依設計 | 同上 |
| 擁有權指紋出現之前的日誌，只能由規劃位元組完全相同的工作階段結清（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 只會還原工作階段擁有的路徑。OMSI 在工作階段期間的自身寫入（未有設定 overlay `options.cfg` 時的 `options.cfg` `[last_map]`、`Texture\standard.ipr`、快取、`laststn.osn`、駕駛員設定檔、記錄檔）會保留下來，與直接啟動 OMSI 後的情況相同 | 依設計 | [交易與復原](../concepts/transactions-and-recovery.md) |
| 當 `SuppressStaleClosecheckWarning` 為 true 時，工作階段開始前對過時 `closecheck` 的移除是永久的（會記錄，但不還原） | 依設計 | 同上 |

<a id="runtime-control"></a>
## 執行階段控制

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| `weather.set` 與 `weather.metar.set` 會寫入 OMSI 的來源天氣記錄（已靜態收尾）；在天氣更新週期上的持續性、雲型解析與 METAR 下載有待執行階段驗證。衍生值（濕度、光線係數、實際風）無法設定；OMSI 暫停期間，它們只會在取消暫停後才更新 | `EXPERIMENTAL` | [功能](capabilities.md) |
| `SetActualDateTime` 只會把電腦時鐘複製到 OMSI 的起始時鐘（靜態分析）；OmsiLaunch 不會呼叫它，而是自行設定起始時鐘 | 依設計 | `calendar.set-actual-date-time` |
| 字串變數寫入、具名觸發器與音效觸發器會經由 OMSI 自己的方法執行（已靜態收尾，BI-004）；它們對車輛指令碼的影響有待執行階段驗證。目標僅限道路車輛 | `EXPERIMENTAL` | `scripts.string.write`、`scripts.trigger` |
| 無車輛重新定位、無跨圖塊空間重新繫結、無 ODE 安全的變換權限；位置欄位為唯讀 | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` 需要 PlayerVehicle；無頭 NEW_MAP 啟動沒有 PlayerVehicle（已儲存情境會提供一個） | 依設計（BI-007） | RV-004 |
| 執行階段變更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、生成、隨機放置、D3D 材質）不會記入日誌，也不會還原 | 依設計 | [執行階段控制](runtime-control.md) |
| Handle 的失效是依據 OMSI 自己從不重複使用的實例編號（`IDCode`）判定，人物則依據消失與重新生成判定；已離線驗證。自然移除（RV-002：AI 車輛離開已載入的路徑網路）仍有待在執行階段觀察，且尚未發出 `entity.removed` 事件 | `PARTIAL` | [執行階段控制](runtime-control.md) |
| 結果受限於 64 KiB 信箱（mailbox）：長清單會被截斷（`truncated=true`）；每次 `d3d.texture.update` 的像素酬載上限為 48 KiB | 依設計 | [功能](capabilities.md) |
| 單一飛行（single-flight）通道：每個工作階段一次只能有一個要求；槽位忙碌時為 `OL_E_RUNTIME_CHANNEL_BUSY`；要求 id 不得重複使用 | 依設計 | [執行階段控制](runtime-control.md) |
| 遙測是 64 個事件的環狀緩衝區：落後超過 64 個事件的主機會失去最舊的事件，並被告知失去多少（`telemetry.lost`） | `EXPERIMENTAL` | [永久外掛程式](../concepts/permanent-plugin.md) |
| 模組的 Direct3D 裝置 hook 是 vtable hook：在另一個元件會把裝置的原始方法寫回的安裝上（在迴歸測試安裝上，於遊戲過程開始後不久觀察到），它們會停止被呼叫，且在清理時無法移除（`OL_E_MODULE_CLEANUP_INCOMPLETE`；該元件仍保持載入） | `PARTIAL` | [執行階段模組](../../../reference/runtime-modules.md#native-components-and-hooks) |
| 執行階段模組的載入是開放的：OmsiLaunch 不檢查模組是誰製作的；它只保證模組絕不會在 OmsiLaunch 處理程序內執行，且工作階段程式被限制在其工作範圍內（沒有第二個處理程序、隨工作階段結束） | 依設計（E21） | [執行階段模組](../../../reference/runtime-modules.md#security-boundary) |
| 模組協定 2（設定、預設啟用、設定檔、工作階段程式、外掛程式衝突、通知、`get_value`）僅離線驗證 | `EXPERIMENTAL` | [執行階段模組](../../../reference/runtime-modules.md) |
| 已在執行階段觀察到 D3D 裝置重設（`resetting`、`restored`、generation 失效）；由於 OMSI 的裝置直接進入 `DEVICENOTRESET`，未產生獨立的 `lost` 轉換 | `PARTIAL`（RV-007） | [執行階段驗證狀態](../status/runtime-validation-status.md) |
| 執行階段清單是分頁的（`cursor`、`limit`、`next_cursor`）：一頁最多包含該清單的最大資料列數，以及可容納於 64 KiB 執行階段槽位的資料列 | `EXPERIMENTAL` | [功能](capabilities.md#pagination) |
| 部分進階的地圖／圖塊／路徑／物件圖欄位未公開；執行階段讀取器是受設定檔限制的具型別快照，絕非任意記憶體存取 | 依設計 | [功能](capabilities.md) |
| 處理程序內記憶體讀取是對執行中 OMSI 的「先檢查後使用」；在檢查與讀取之間若 OMSI 同時發生變更，可能產生不一致的快照（`OL_E_RUNTIME_OPERATION_FAILED`） | 已接受的風險（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## 本機控制平面與信任模型

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 同一使用者信任模型：具名管道（named pipe）（`CurrentUserOnly`）、handoff／遙測／執行階段記憶體對應以及租約號誌，同一 Windows 使用者的任何處理程序都能存取。這類處理程序一旦讀取到 `session_id`，就能讀取狀態、停止工作階段或執行執行階段操作。 | 已接受的風險（S-06、S-30） | [本機控制](local-control.md) |
| 控制端點僅在擁有者處於 `Running` 時存在；用戶端在啟動期間及工作階段結束後會看到 `OL_E_NO_ACTIVE_SESSION`（結束代碼 4） | 依設計 | [本機控制](local-control.md) |
| 若另一個處理程序已擁有該管道名稱，擁有者會在沒有端點的情況下繼續執行（`ListenFault`），而第二次啟動可能誤報 `OL_E_SESSION_ALREADY_ACTIVE` | 已接受的風險 | [本機控制](local-control.md) |
| `.omsilaunch\` 繼承 OMSI 根目錄的 ACL；未套用明確的存取控制 | 已接受的風險（S-31） | [交易與復原](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 診斷與輸出

| 限制 | 穩定性 | 詳細資訊 |
| --- | --- | --- |
| 診斷資料僅為本機檔案（`.omsilaunch\diagnostics`）；不會上傳任何內容，也沒有遠端回報 | 依設計 | [交易與復原](../concepts/transactions-and-recovery.md) |
| 保留最新的 50 個工作階段；新工作階段啟動時，會刪除較舊的、以工作階段為前綴的診斷資料 | 依設計 | 同上 |
| JSON 輸出與診斷資料包含安裝路徑（`RootPath`、素材目錄、`.itx` 路徑） | 依設計（本機資料） | |
| `OmsiLaunchW.exe` 的工作階段失敗對話方塊會以外掛程式的失敗酬載（例如 `{"name":"world.failed",...}`）作為訊息，而不是一個句子；`Code:` 那一行是正確的 | 外觀問題 | [Windows 系統匣](windows-tray.md) |
| 在任何 Direct3D 呼叫之前就被原生橋接層拒絕的 D3D 要求，會正確回報 `native_status`，但其 `detail` 文字卻顯示 `HRESULT 0x00000000` | 外觀問題 | [功能](capabilities.md) |
| 系統匣狀態視窗是開啟時所擷取的已規劃工作階段快照；它不會重新整理，也不顯示即時的 OMSI 數值 | 依設計 | [Windows 系統匣](windows-tray.md) |

<a id="documentation"></a>
## 文件

`docs/` 底下的英文頁面是此版本的規範文件。`docs/localized/<locale>/` 包含相同 `0.1.0-beta4` 頁面的翻譯（請見 [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)）；若翻譯與英文內容有出入，以英文內容與程式碼為準。其中列出的歷史與舊版頁面僅提供英文版。

相關頁面：[功能](capabilities.md)、[執行階段驗證狀態](../status/runtime-validation-status.md)、[錯誤](errors.md)。
