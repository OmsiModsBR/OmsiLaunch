# 交易與復原

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../concepts/transactions-and-recovery.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

每個會變更 OMSI 檔案的 OmsiLaunch 工作階段，都在一個持久、具日誌的交易中進行：原始位元組在被取代之前先行備份，日誌記錄工作階段進行到哪一步，而還原在寫回每份備份之前都會先驗證它。本頁說明這個交易的實作方式：由 `FileConfigurationTransaction`（`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`）實作，由 `OmsiLaunchService.StartAsync`、`SuperviseAsync` 與 `RecoverPendingAsync`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）驅動，並搭配 `SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）計算的檔案輸入。本頁寫給需要了解工作階段會變更什麼、復原會做什麼的使用者，以及需要確切保證的整合者。

<a id="what-a-session-changes"></a>
## 工作階段會變更什麼

只有**暫時性 overlay（暫時覆蓋）**會進入交易。它們在交易開啟前計算，並在交易關閉時還原。

| 工作階段輸入 | 檔案 | 類型 |
| --- | --- | --- |
| `/set:<key>=<value>`、設定檔 `settings`、`LaunchSpec.Environment.*` | `options.cfg`（語意 token 修補；保留 CP1252 位元組，遵循帶 BOM 標記的 UTF-8/UTF-16） | overlay |
| 受管理的啟動畫面（`SplashMode.Managed`，預設值） | `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<language>.bmp` | overlay（當 OMSI 安裝中沒有在地化檔案時，該檔案由交易建立） |
| Internet Textures（網路材質）`Override` | `Texture\standard.itx` | overlay |
| Internet Textures `Override` | `.itx` 中列出的每個目標，以及 `Texture\standard.ipr` | 工作階段刪除 |
| 一律 | `closecheck`（工作階段之前不存在時） | 工作階段刪除 |

永久產品檔案**不是**交易參與者：`plugins\OmsiLaunch.*` 下的外掛程式封閉集合（僅驗證，請參閱[永久外掛程式](permanent-plugin.md)）、`.omsilaunch\assets\splash\*.bmp`（只複製一次，從不移除）、`.omsilaunch\diagnostics` 下的診斷檔案、工作階段設定檔套件，以及發行文件與範例。第三方外掛程式及其他所有 OMSI 檔案從不會被列舉、複製、移除或還原。

工作階段執行期間，OMSI 本身會如同一般啟動 OMSI 時一樣持續寫入自己的狀態：`options.cfg`（例如工作階段載入不同地圖時的 `[last_map]`，在進入遊戲時重寫）、`Texture\standard.ipr`、時刻表與光照貼圖快取（`Texture\Temp_Schedules\*`、`maps\<map>\*.map.LM.bmp`）、`maps\<map>\laststn.osn`、`Drivers\` 下的駕駛員設定檔，以及其記錄檔。對工作階段擁有之路徑（見上方）的寫入會由還原撤銷；其他所有 OMSI 寫入在工作階段結束後都會保留，就如同直接執行 OMSI 之後一樣。執行階段收尾證據：在另一張地圖上的已儲存情境工作階段因為沒有覆蓋 `options.cfg`，留下了變更後的 `[last_map]`（`CAM01`），而 `/set` 工作階段則精確還原了 `options.cfg`（`S12a`、`S12b`、`C01`）。

<a id="transaction-states"></a>
## 交易狀態

每次狀態轉換後，`TransactionState` 都會保存在日誌中。這些值由 `System.Text.Json` 序列化為整數。

| 值 | 狀態 | 寫入時機 |
| --- | --- | --- |
| 0 | `Prepared` | 已擷取每個 overlay 與刪除路徑的快照，且其備份已排清至磁碟。OMSI 安裝中尚未有任何變更。這是復原義務的起點：自此之後，當機會留下可復原的日誌。 |
| 1 | `Applied` | 每個 overlay 都已以不可分割的方式寫入，每個刪除項目都已移除。 |
| 2 | `RuntimeDeployed` | 已針對此次啟動驗證永久外掛程式的完整性（不部署任何內容；此名稱為歷史沿用）。 |
| 3 | `HandoffCreated` | 啟動交接區、遙測槽位與執行階段信箱（mailbox）已作為具名共用記憶體存在。 |
| 4 | `ProcessStarted` | 已建立 `Omsi.exe`。日誌此時還包含 `ProcessId`、`ProcessStartFileTimeUtc`（建立時間，UTC ticks）與 `ExecutablePath`。 |
| 5 | `ProcessExited` | 監督程式已確認處理程序結束（自然結束或 `TerminateProcess`）。 |
| 6 | `Restoring` | 還原已開始。 |
| 7 | `Restored` | 每個擁有的檔案都已還原並驗證。隨即刪除日誌並移除 `backup\<session>`。 |
| 8 | `Completed` | 已在列舉中宣告，但從未保存；已完成的交易沒有日誌。 |

因此，一般工作階段的生命週期為：快照 -> `Prepared` -> 寫入 overlay／移除刪除項目 -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> 刪除日誌 -> 移除 `backup\<session>`。公開的 `SessionState` 值 `Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`ProcessExited`、`Restoring`、`CleaningRuntime` 與 `Completed` 從外部追蹤相同的進程（請參閱[工作階段生命週期](session-lifecycle.md)）。

沒有任何擁有檔案變更的工作階段仍會為其生命週期寫入日誌；還原它是經過驗證的無作業。

<a id="journal-file"></a>
## 日誌檔案

路徑：`<root>\.omsilaunch\journal.json`。每個 OMSI 安裝最多只有一個日誌；它的存在代表「有交易待處理」。

`TransactionJournal` 欄位：

| 欄位 | 型別 | 意義 |
| --- | --- | --- |
| `SessionId` | GUID | 擁有此日誌的工作階段；也是備份目錄名稱（`N` 格式）。 |
| `State` | 整數 | 上述的 `TransactionState`。 |
| `Files` | `JournalFile` 陣列 | 每個擁有的路徑一筆項目。 |
| `ProcessId` | 整數或 null | OMSI PID，自 `ProcessStarted` 起。 |
| `ProcessStartFileTimeUtc` | long 或 null | OMSI 建立時間（UTC ticks），自 `ProcessStarted` 起。 |
| `ExecutablePath` | 字串或 null | 所啟動 `Omsi.exe` 的完整路徑，自 `ProcessStarted` 起。 |

`JournalFile` 欄位：

| 欄位 | 型別 | 意義 |
| --- | --- | --- |
| `RelativePath` | 字串 | 相對於安裝根目錄的路徑（`options.cfg`、`GUI\NewSplashscreen_ENG.bmp`、...）。 |
| `Existed` | bool | 工作階段之前該檔案是否存在。 |
| `Sha256` | 十六進位字串 | 原始位元組的 SHA-256（`Existed` 為 false 時為空位元組陣列的雜湊）。 |
| `BackupPath` | 字串 | 備份副本的絕對路徑（僅在 `Existed` 時寫入）。 |
| `AppliedSha256` | 十六進位字串或 null | 工作階段寫入此路徑之 overlay 位元組的 SHA-256；工作階段刪除項目為 null。這是原本不存在之檔案的擁有權指紋。 |
| `LastWriteTimeUtcTicks` | long 或 null | 原始的最後寫入時間。 |
| `CreationTimeUtcTicks` | long 或 null | 原始的建立時間。 |
| `Attributes` | 整數或 null | 原始的 `FileAttributes`（包括 `ReadOnly`）。 |
| `SessionDeletion` | bool | 對於工作階段要求保持不存在的路徑（`.itx` 目標、`Texture\standard.ipr`、`closecheck`）為 true。 |

較早組建所寫入、沒有 `AppliedSha256` 與中繼資料欄位的日誌仍可讀取；請參閱[原本不存在的檔案](#originally-absent-files-and-ownership)。

<a id="backup-layout"></a>
## 備份配置

| 路徑 | 內容 |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | 每個工作階段一個目錄，與 `Prepared` 日誌一起建立。 |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | 一個已存在之擁有檔案的原始位元組完整副本。原本不存在的檔案沒有備份。 |

備份與日誌的寫入方式為：先寫入暫存檔（`<path>.omsilaunch.tmp`），使用直接寫入（write-through）並明確呼叫 `Flush(true)`，再以覆寫模式進行不可分割的 `File.Move`。暫存檔一律會被刪除，即使失敗也是如此。overlay 與還原的原始檔案使用相同的寫入路徑，因此已完成的作業不會殘留任何 `*.omsilaunch.tmp` 檔案。

備份只會在參照它們的日誌被刪除之後才移除。無法刪除 `backup\<session>` 只是外觀上的問題，絕不會撤銷已驗證的還原。

<a id="restore"></a>
## 還原

`RestoreAsync` 在 `ProcessExited` 之後（或在復原期間）執行。對於日誌中的每個路徑：

| 原始狀態 | Action |
| --- | --- |
| 原本存在 | 計算備份位元組的雜湊並與 `Sha256` 比較；若不相符，會在寫入任何內容之前以 `OL_E_RECOVERY_BACKUP_CORRUPT` 中止。接著以不可分割的方式寫入位元組（若目前檔案為唯讀，會先清除該屬性），並還原建立時間、最後寫入時間與屬性（`RestoreMetadata`；中繼資料失敗會被忽略，使權限問題無法阻擋逐位元組精確還原）。 |
| 原本不存在、現在存在，已知 `AppliedSha256` | 計算目前位元組的雜湊。若等於 `AppliedSha256`，該檔案就是工作階段自己的 overlay，並會被刪除。否則以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` 中止還原並保留日誌。 |
| 原本不存在、現在存在，工作階段刪除項目，日誌已達 `ProcessStarted` | 該檔案是工作階段的副產物（OMSI 在持有安裝租用的情況下執行，且此路徑被要求保持不存在）。它會被刪除，並以診斷訊息 `restore.session-artifact-removed` 回報，附上被移除內容的 SHA-256。 |
| 原本不存在、現在存在，工作階段刪除項目，處理程序從未啟動 | 該檔案來自工作階段之外。它會被保留，並以 `OL_W_RESTORE_FOREIGN_FILE_RETAINED` 回報其 SHA-256，交易仍會完成。 |
| 原本不存在、現在存在，沒有擁有權證據（指紋前日誌） | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`；保留日誌。 |
| 原本不存在、仍不存在 | 無需處理。 |

所有檔案處理完畢後，`VerifyRestoredSnapshots` 會重新讀取每個路徑：原本存在的檔案雜湊必須等於 `Sha256`，原本不存在的路徑必須不存在，除非它們被明確保留。只有在此之後才會保存 `Restored`、刪除日誌（若日誌仍存在則為 `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）並移除備份目錄。在 `Restored` 與刪除日誌之間發生當機，只會導致一次冪等的重播。

還原附註（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）會以 `LaunchDiagnostic` 項目出現在 `SessionStatus.Diagnostics`（`Data` 中含 `sha256`）與 `RecoveryStatus.Diagnostics` 中，因此不會有任何檔案在無聲無息中被移除或保留。

<a id="originally-absent-files-and-ownership"></a>
### 原本不存在的檔案與擁有權

寫入原本不存在之路徑的 overlay，**只有在其內容仍與工作階段所套用的內容相符時**（`AppliedSha256`）才會在還原時移除。若工作階段期間有其他東西取代了它，還原會以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` 失敗，並保留日誌以供檢查。

對於原本不存在、但之後存在的工作階段刪除路徑（`.itx` 目標、`Texture\standard.ipr`、`closecheck`），會依據 OMSI 是否在此交易下執行來判斷：若日誌已達 `ProcessStarted`，該檔案是工作階段副產物並會被移除（`restore.session-artifact-removed`）；若處理程序從未啟動，該檔案會被保留並回報為 `OL_W_RESTORE_FOREIGN_FILE_RETAINED`，日誌仍會完成。

### `closecheck`

`closecheck` 是 OMSI 自己的當機標記（當 OMSI 未正常關閉時存在）。適用兩條規則：

- 若它在工作階段**之前**就存在，且 `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` 為 `true`（預設值），它會在交易開啟前被永久移除，並記錄為診斷訊息 `closecheck.stale-removed`，附上其 SHA-256（若刪除失敗則為 `OL_E_CLOSECHECK_REMOVE_FAILED`）。這是有文件說明的永久變更，不是交易參與者。旗標為 `false` 時，標記會保留，OMSI 會顯示其警告。
- 若它在工作階段之前**不**存在，`closecheck` 會被加入為工作階段刪除項目。由於工作階段以 `TerminateProcess` 結束（OMSI 的關閉常式不會執行），OMSI 在啟動時建立的標記事後一定仍然存在；它會在還原時作為工作階段產生的檔案被移除。

<a id="early-recovery-order"></a>
## 早期復原順序

每次 `StartSessionAsync` 時，在取得安裝租用之後、任何程式讀取實際 OMSI 安裝之前：

1. 一個僅供復原的交易會檢查 `journal.json`。若存在，會立即執行 `RestorePendingAsync`，使新工作階段的 overlay 與啟動畫面語言衍生自**原始**檔案，絕不會衍生自前一個工作階段的殘留物。
2. 若該復原以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` 失敗（指紋前日誌），復原會被**延後**：新工作階段建立其 overlay，而新交易會以自己規劃的位元組作為擁有權證據重試復原（內容等於新 overlay 的原本不存在檔案，會被視為 OmsiLaunch 所擁有）。任何其他復原失敗都會使啟動失敗。
3. 只有在此之後，才會驗證外掛程式封閉集合、計算 `Omsi.exe` 的雜湊、處理 `closecheck`，並準備與套用新交易。

主機追蹤記錄會記錄 `PENDING_JOURNAL_RECOVERED` 或 `PENDING_JOURNAL_RECOVERY_DEFERRED`。

<a id="crash-recovery-and-owner-liveness"></a>
## 當機復原與擁有者存活判定

復原絕不會在執行中的 OMSI 底下取代檔案。當日誌所記錄的擁有者仍存活時，`RestorePendingAsync` 會以 `OL_E_INSTALLATION_BUSY` 拒絕：

| 日誌內容 | 存活測試 |
| --- | --- |
| 已記錄 `ProcessId` 與 `ProcessStartFileTimeUtc` | 具有該 PID 的處理程序必須正在執行、其啟動時間必須相符（排除 PID 重複使用），且在已記錄 `ExecutablePath` 時，其主模組必須是該路徑（無關的存活處理程序無法保留該交易）。 |
| 沒有 PID，狀態介於 `HandoffCreated`（含）與 `ProcessExited`（不含）之間 | 主機在 `CreateProcess` 與寫入日誌之間終止。任何主模組為 `<root>\Omsi.exe` 的 `Omsi.exe` 都會被視為擁有者。 |
| 沒有 PID，其他狀態 | 未存活；繼續復原。 |

明確復原以 `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)` 公開，回傳 `RecoveryStatus(Pending, Recovered, Diagnostics)`；它會先取得安裝租用（當另一個擁有者持有時為 `OL_E_INSTALLATION_BUSY`）。在 CLI 上，`/recovery-status` 只回報而不還原，`/recover` 則執行還原；當要求還原但之後日誌仍待處理時，回傳結束代碼 8（`TransactionRecoveryFailed`）。請參閱 [CLI](../reference/cli.md) 與[公開 API](../reference/public-api.md)。

<a id="deferred-restore-at-session-end"></a>
### 工作階段結束時的延後還原

若監督程式無法確認 OMSI 已結束（`OL_E_PROCESS_TERMINATE_FAILED`、`OL_E_PROCESS_WAIT_FAILED`，或回報為 `OL_E_PROCESS_CLEANUP_FAILED` 的清理錯誤），工作階段會以 `OL_E_RESTORE_DEFERRED` 失敗，且日誌會被刻意保留：在 OMSI 可能仍在讀取安裝檔案時取代它們並不安全。下一次啟動（或 `/recover`）會在處理程序消失後進行還原。因其他任何原因而失敗的還原，會以 `OL_E_RESTORE_FAILED` 結束工作階段；日誌會保留，直到每個擁有的原始檔案都已還原並驗證為止。

<a id="the-installation-lease"></a>
## 安裝租用

租用是一個計數為 1 的具名旗號（semaphore）`Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>`。根目錄由 `InstallationLease.NormalizeRoot` 正規化（完整路徑，移除結尾分隔符號，磁碟機根目錄除外），因此 `C:\OMSI`、`C:\OMSI\` 與 `c:\omsi\sub\..` 共用同一個租用；本機控制管道名稱使用相同的正規化。它由 `StartSessionAsync`（狀態 `AcquiringInstallationLock`）與 `RecoverPendingAsync` 取得，並在工作階段的生命週期工作完成或復原呼叫返回時釋放。無法取得時會立即引發 `OL_E_INSTALLATION_BUSY`（不等待）。

已接受的限制（已記錄於文件，未排定變更）：

- `Local\` 範圍：**每個登入工作階段**中每個 OMSI 安裝只有一個擁有者。同一部電腦上的兩位互動式使用者不會互斥。
- 只要還有其他處理程序持有旗號的 handle，當機並不會釋放該旗號；與被遺棄的 mutex 不同，旗號沒有擁有者。過時的持有者會使該 OMSI 安裝維持 `OL_E_INSTALLATION_BUSY`，直到該 handle 關閉為止。
- 同一 Windows 使用者的任何處理程序都可以搶先建立該名稱並持有它。

<a id="omsilaunch-directory"></a>
## `.omsilaunch` 目錄

| 項目 | 存續期間 | 擁有者 |
| --- | --- | --- |
| `journal.json` | 暫時；僅在有交易待處理時存在 | 交易 |
| `backup\<sessionId>\*.bin` | 暫時；在日誌之後移除 | 交易 |
| `diagnostics\<sessionId>-host.log` | 永久；保留最新的 50 個工作階段（新工作階段啟動時會刪除較舊的工作階段前綴檔案） | 主機追蹤記錄 |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | 永久（相同保留規則） | CLI |
| `diagnostics\tray-host.log` | 永久 | Windows 系統匣主機 |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | 永久產品資產；從套件複製一次，從不覆寫或移除 | 工作階段視覺資產 |
| `session-profiles\<id>\` | 永久；由使用者或內容作者安裝 | 使用者 |
| `profiles\` | 目前的程式碼不會建立或讀取；保留 | 無 |
| `docs\`、`examples\` | 永久；隨發行套件提供 | 套件 |

不會有任何資料離開本機；診斷訊息僅為本機檔案。另請參閱 [`.omsilaunch` 目錄](../../../concepts/omsilaunch-directory.md)。

<a id="runtime-mutations-are-not-journaled"></a>
## 執行階段變更不會記入日誌

執行階段控制操作（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random`、D3D 材質）只會變更 OMSI 的記憶體內狀態。它們不會記錄在日誌中，也不會被還原；它們會隨處理程序消失。請參閱[執行階段控制](../reference/runtime-control.md)。

<a id="failure-modes-and-error-codes"></a>
## 失敗模式與錯誤碼

| 錯誤碼 | 意義 | 之後的日誌 |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | 租用由另一個擁有者持有，或日誌中記錄的 OMSI 處理程序仍存活 | 保留 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | 對一個有快照但磁碟上沒有日誌的交易要求還原 | 不適用 |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | 備份的雜湊與快照指紋不同；未寫入任何內容 | 保留 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | 原本不存在的 overlay 路徑現在含有不是工作階段寫入的內容 | 保留 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | 指紋前日誌中有一個原本不存在、但現在存在的路徑；只有規劃位元組完全相同的新工作階段才能結束它 | 保留（延後） |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | 已驗證的還原之後無法刪除 `journal.json` | 保留（重播為冪等） |
| `OL_E_RESTORE_DEFERRED` | 未確認 OMSI 已結束；還原延後至下次啟動 | 保留 |
| `OL_E_RESTORE_FAILED` | 其他任何還原失敗（還原後存在與否或雜湊不符、I/O 錯誤） | 保留 |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | 交易之前無法刪除過時的 `closecheck` | 尚無 |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | 警告：工作階段刪除路徑上的外來檔案被保留 | 已完成 |
| `OL_E_PLAN_NOT_RUNNABLE` | 啟動時重新規劃發現 spec 已不再可執行（例如 `Omsi.exe` 已變更）；不會開啟任何交易 | 無 |

CLI 將 `OL_E_RECOVERY_*` 與 `OL_E_RESTORE_FAILED` 對應為結束代碼 8，將 `OL_E_INSTALLATION_BUSY` 對應為結束代碼 7；請參閱[結束代碼](../reference/exit-codes.md)。

<a id="evidence"></a>
## 證據

`tools/OmsiLaunch.TestHost` 中的離線測試涵蓋交易路徑：`transaction.restore`、`transaction.options-overlay-restore`、`transaction.absent-overlay-restore`、`transaction.absent-file-ownership`、`transaction.absent-file-recovery`、`transaction.session-delete-restore`、`transaction.deletion-created-during-session`、`transaction.deletion-foreign-file-retained`、`transaction.deletion-recovery-after-crash`、`transaction.backup-corrupt-rejected`、`transaction.metadata-and-backup-cleanup`、`transaction.legacy-journal-ownership-migration`、`transaction.recovery-pre-pid-window`、`transaction.recovery-then-apply-ownership`、`transaction.restore-failure-recovery`、`transaction.failure-boundaries`、`transaction.empty-journal-restore`、`api.recover-requires-lease`、`lease.cross-thread-release`。

執行階段證據（驗證矩陣）：RV-005 與 RV-006（套用 overlay 與逐位元組精確還原，工作階段 `1e8e0548-...` 與展示批次）、RV-008 提前結束通過（工作階段 `0dc40570-...`）。

執行階段證據（執行階段收尾輪次，2026-09-23，`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：
- 使用 OMSI 真實下載器移除 `.itx` 目標這類工作階段產生的檔案，涵蓋一般停止，以及擁有者中斷後再執行 `/recover` 的情況（S-01、`I01`、`I02`）；
- 在建立 overlay 之前進行早期復原（S-05、`S05`）；
- 中繼資料與唯讀屬性還原，以及備份清理（S-12、`S12a`、`S12b`）；
- 在租用下執行 `/recover`，包括存在孤立 OMSI 與處於 PID 記錄前時間窗的情況（S-04、`S04`、`S04b`）；
- 啟動失敗，以及還原失敗後再執行 `/recover`（RV-008 其餘部分、`SF01`、`SF02`、`F01`）；
- CP1252 保留（S-07、`C01`）。

延後的指紋前復原分支仍僅有離線測試。請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)。
