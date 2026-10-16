# 執行階段驗證狀態

<!-- l10n: source=status/runtime-validation-status.md source-sha256=87cfe5eca3645bba4beefa62ea72d3bda5ef7a9f355158a7ea38eb1b61d8376d -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../status/runtime-validation-status.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 OmsiLaunch 0.1.0-beta4 唯一的證據總表。每一列列出一項功能（capability）或特性及其證據狀態。`RUNTIME_VALIDATED` 可引用 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`（矩陣 id RV-nnn、阻礙項 BI-nnn，以及其 2026-09-20 執行表中的工作階段 id），或引用 post-Fable Round A 報告 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-ROUND-A-001.md`（RA-nnn）；其餘一切皆由程式碼與離線測試套件推導而來。2026-09-21 的強化回合（`research/reports/OMSILAUNCH-SECURITY-ROBUSTNESS-REVIEW-001.md`，發現項目 S-01 至 S-34）變更了還原、復原、控制平面與邊界行為；這些變更當時只有離線證據。2026-09-23 的最終執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`，情境 id 例如 `F01`、`S05`、`T01`，候選版本 2 與 3）在經授權的安裝上以實際工作階段執行了這些變更；由該回合提升狀態的列會引用其情境 id 與工作階段 id。因此 `RUNTIME_VALIDATED` 也可以引用該報告。

證據狀態：

| 狀態 | 意義 |
| --- | --- |
| RUNTIME_VALIDATED | 已在矩陣所記錄的標準工作階段中觀察到（引用 id 與工作階段）。 |
| STATICALLY_VALIDATED | 已實作並由離線測試涵蓋（`tools/OmsiLaunch.TestHost`、`tests/*`），自上次變更以來未在實際工作階段中觀察到。 |
| PARTIAL | 已實作，但證據不完整、相互矛盾，或已知其行為有所限制。 |
| RUNTIME_VALIDATION_REQUIRED | 行為已變更或從未被觀察到；必須先在實際工作階段中驗證，才能提升狀態。 |
| UNAVAILABLE | 未實作或刻意拒絕。 |

<a id="session-lifecycle-and-transaction"></a>
## 工作階段生命週期與交易

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| NEW_MAP 無頭啟動（`world.new-map`、`world.presented-entrypoint`、`boot.headless-start`） | RUNTIME_VALIDATED | 矩陣「Existing Runtime Evidence」；工作階段 `e5454061`、`1e8e0548`、`5f641c8d`、`50a1f1ec`（Grundorf） |
| SAVED_SITUATION 啟動（`world.saved-situation`） | RUNTIME_VALIDATED | `GetCapabilitiesAsync` 附註與矩陣（Berlin-Spandau，Start 表單與 `Button1Click`） |
| `session.stop` 強制終止與精確還原 | RUNTIME_VALIDATED | 工作階段 `50a1f1ec`：正常停止後沒有殘留 OMSI 處理程序，也沒有日誌 |
| 啟動冪等性（重複的 `PluginStart`） | RUNTIME_VALIDATED | 工作階段 `50a1f1ec` |
| `options.cfg` 語意 overlay（暫時覆蓋）與逐位元組精確還原（RV-005） | RUNTIME_VALIDATED | 工作階段 `1e8e0548`：`AIMaxCountRandom` 150/200，並保留尾端內容；還原後雜湊為 `A12982C3...` |
| Managed 啟動畫面加上 overlay 交易（RV-006） | RUNTIME_VALIDATED | 矩陣 RV-006 PASS（呈現批次：managed 預設、managed 自訂、`Unset`）；Release Presentation 001 工作階段 `388cf5d2-7310-4301-9313-e62c4c08a346`、`794cae34-a2ac-4fcf-8981-f142ac14c308`、`12a74b24-6c78-4863-8ffe-947651212eeb` |
| 提前結束的復原（RV-008，強制終止 OMSI） | RUNTIME_VALIDATED | Post-Fable Round A 工作階段 `9e7cad79-9c35-4f93-bfc8-71f455513f82`（遊戲進行後）與 `dcac497e-ff35-4c97-b5e4-98b660f88032`（`world.starting` 之後、遊戲進行前）：擁有者完成、日誌與租約已清除，`options.cfg` 及原本不存在的啟動畫面狀態皆與快照相符。原生啟動失敗與還原失敗的注入測試另行處理。 |
| 啟動失敗與刻意造成還原失敗的邊界（RV-008 其餘部分） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：啟動逾時 `SF01` 工作階段 `0b21c664-b0b2-46b2-9b6b-def536765fff` 與外掛程式回報的世界失敗 `SF02` 工作階段 `53358c75-469b-41d5-9ddb-cdb1d1ff828d` 皆以 `Failed` 結束，附帶具型別的錯誤碼並精確還原；還原失敗 `F01` 工作階段 `d4194a69-d858-42fe-97c7-ce93ccbdc3f6`（停止期間，一個工作階段擁有的檔案被僅供測試的處理程序鎖定）產生 `OL_E_RESTORE_FAILED`，保留了日誌與備份，而 `/recover` 精確還原了工作階段前的狀態。這些失敗的產生方式僅屬測試基礎設施。 |
| `.itx` 目標與 `Texture\standard.ipr` 的工作階段產出物還原（S-01） | RUNTIME_VALIDATED | Round A 在 `47efd323-4ce1-400a-ac83-74893cf82f26` 中還原既有路徑；在 `8d9bdedf-d7de-4c64-a8b1-627ae4deff7b` 中於正常停止時建立並移除一個已登記、原本不存在的目標；並在 `aa9a45d4-302e-4cf5-8490-f5a1c9e44b79` 中透過擁有者中斷加上 `/recover` 重複了該清理。 |
| `closecheck` 視為工作階段刪除與過時標記移除（S-13） | RUNTIME_VALIDATED | Round A 工作階段（包括 `47efd323-4ce1-400a-ac83-74893cf82f26`）記錄了 `closecheck.stale-removed`，接著是 `restore.session-artifact-removed`；沒有殘留日誌。 |
| 在建構 overlay 之前提早復原、延後的 pre-fingerprint 復原（S-05） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`S05`：工作階段 `f755ed51-b85c-4df2-b062-5bf65c768b32` 的擁有者在有待處理日誌時被終止（`maxFPS=77` 生效中）；下一次啟動，即工作階段 `291ea22b-f9b3-42a4-be07-7f0c055c7113`，在 `TRANSACTION_STARTED` 之前記錄了 `PENDING_JOURNAL_RECOVERED`，以其自身的 overlay（`88`）執行，結束時與第一個工作階段之前的狀態逐位元組相同。延後的 pre-fingerprint 分支仍僅有離線證據（`transaction.legacy-journal-ownership-migration`）。 |
| 中繼資料還原（時間戳記、屬性、唯讀）、write-through 排清、備份清理（S-12） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`S12a` 工作階段 `40de172b-e144-4f0a-9207-09c80b5d7df7`（已知的 `options.cfg` 時間戳記）與 `S12b` 工作階段 `d9e2534b-3cb3-47ef-9cbd-1b92c62a35e7`（時間戳記加上 ReadOnly）：工作階段期間 overlay 生效（為 OMSI 清除 ReadOnly），位元組、最後寫入時間、建立時間與屬性皆精確還原；沒有殘留備份目錄。 |
| 在安裝租約下執行 `/recover`；pre-PID 時間窗內的拒絕（S-04） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`S04` 工作階段 `1fb2aad5-2985-4fa4-9c5a-6186f51d37b8`：擁有者存活時，`/recovery-status` 與 `/recover` 回傳 `OL_E_INSTALLATION_BUSY`，第二次啟動被拒絕；擁有者被終止而 OMSI 仍在執行時，`/recover` 與新的啟動都被拒絕，直到 OMSI 結束，之後 `/recover` 精確還原。`S04b` 工作階段 `5e57ca51-ab5e-4e94-a0d0-531735ee4057`：只要有任何 `Omsi.exe` 從該根目錄執行，pre-PID 日誌（測試工具的故障注入）就會被拒絕，並在其結束後復原；由該 OMSI 寫入的 `closecheck` 以 `OL_W_RESTORE_FOREIGN_FILE_RETAINED` 保留（已記載，屬保守作法）。 |
| 在 `StartSessionAsync` 重新規劃，過時計畫回傳 `OL_E_PLAN_NOT_RUNNABLE`（S-15） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R02`：規劃後將一個資訊清單所列的外掛程式移開，`StartSessionAsync` 在任何工作階段存在之前就擲出 `OL_E_PLAN_NOT_RUNNABLE: OL_E_PERMANENT_PLUGIN_MISSING`（計畫 `4797c9ce-6eb3-47b1-b09d-03562dd3bd44`，候選版本 2，修正 BUG-01 之後） |
| 擁有者生命週期：管道停止、Ctrl+C、系統匣停止、OMSI 結束、擁有者被終止、`/observe-seconds` | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：在 `/observe-seconds` 期間以管道停止 `L06` 工作階段 `300a563e-7b0e-4f01-afbd-9e15df32c7e6`；Ctrl+C `L03` 工作階段 `5e2aca54-bae2-4fc6-a106-93699e755bfc`（擁有者在訊號後 577 ms 結束）；OMSI 被終止 `L05a` 工作階段 `d69658f9-16f6-43e9-8e36-682114b97e14`；關閉 OMSI 主視窗 `L05b` 工作階段 `76b11ba4-8ddb-4611-9a1c-8be317cd0337`（OMSI 忽略 `WM_CLOSE`；以終止結束）；系統匣停止 `T01`/`T03` 工作階段 `6d827c13-caf8-4221-832a-c91d26f130e0`、`7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`；擁有者被終止 `S05`。每條路徑都以精確還原結束。擁有者被直接終止時，`Omsi.exe` 會繼續執行（依設計）；在其結束前復原會被拒絕（`S04`）。 |
| 主控台關閉或登出時 ProcessExit 的 4 s 時間上限 | RUNTIME_VALIDATED（主控台關閉） | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`L04` 工作階段 `559d0f1d-7d5b-4b6b-b70c-0acb272d53db`：關閉主控台視窗；還原在時間上限內完成，工作階段達到 `Completed` 且無日誌；Windows 在關閉後約 5 s 結束擁有者（結束代碼 `0xC000013A`）。登出未經實測。 |
| 協同式 WM_CLOSE 關閉 | UNAVAILABLE | 未實作（產品決策，S-11）。執行階段收尾 `L05b`：對 OMSI 主視窗送出 `WM_CLOSE` 後，OMSI 在 30 s 內未關閉。`ShutdownTimeoutSeconds` 未被使用。 |
| 安裝租約語意（`Local\` 號誌） | STATICALLY_VALIDATED | `lease.cross-thread-release`；限制已記載為已接受的風險（S-18） |
| 修補 OMSI 檔案時保留 CP1252（S-07） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`C01` 工作階段 `84a71235-d618-4917-9af4-c2a6025742f4`：以 `/set` 修補含有重音 CP1252 值（包括 0x80 與 0x96）的 `options.cfg`；執行中的檔案保留 CP1252 位元組，沒有出現 `EF BF BD`，且工作階段結束後檔案逐位元組相同 |
| 內容探索略過重新剖析點（S-23） | STATICALLY_VALIDATED | `discovery.*` 測試 |
| 診斷資料保留（最新 50 個工作階段）（S-26） | STATICALLY_VALIDATED | `HostTrace.Prune` |
| 交易寫入與刪除時遇到的暫時性檔案鎖定（防毒軟體、索引服務）會重試約 1.5 s；持續性鎖定仍會失敗（修正回合 CP-10） | STATICALLY_VALIDATED | `transaction.transient-lock-retried`、`transaction.restore-failure-recovery`（持續性鎖定仍會失敗）；修正前觀察到兩次間歇性離線失敗，修正後 16 次執行中皆未發生 |

<a id="permanent-plugin-and-platform"></a>
## 永久外掛程式與平台

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| 設定檔 `Omsi23004_692EBFBF`（`692EBFBF...6243`，8,503,440 位元組） | RUNTIME_VALIDATED | 矩陣中的每個工作階段 |
| Steam LAA 執行檔（`7DAB063D...D759`） | PARTIAL | 執行階段收尾 `LAA01` 工作階段 `9ec975ca-7820-47be-b806-5f5a94aad4ab`：一份受控副本（由該安裝未修補的原始檔衍生，設定 `IMAGE_FILE_LARGE_ADDRESS_AWARE` 並重新計算 PE 總和檢查碼；8,503,440 位元組，characteristics 為 `0x81AE`）與雜湊相符，指紋被接受，計畫可執行。此映像是原始、受 DRM 保護的 Steam 執行檔：在真正的 Steam 安裝之外，它會交由 Steam 用戶端處理並結束，因此工作階段以 `OL_E_PROCESS_EXITED_EARLY` 失敗並精確還原。遊戲進行、讀取、命令與停止都需要真正的 Steam 安裝（ENVIRONMENT）。 |
| 安裝識別：租約與控制管道名稱皆衍生自同一個定義 `InstallationPaths.IdentityKey`（`C:\OMSI` = `C:\OMSI\` = `C:\OMSI\.` = `C:\foo\..\OMSI` = 大小寫變化；`C:\OMSI-A` 與 `C:\OMSI-B` 不同）（修正回合 CP-02，附錄 A/B） | STATICALLY_VALIDATED；已於執行階段觀察到 | `lease.root-normalization`、`paths.installation-identity-and-containment`。執行階段收尾 `ID01`：工作階段 `01bfc780-10ef-4390-b700-c08bf809a42f` 執行期間，以含 `\.`、含 `..` 區段、小寫及大寫拼寫根目錄的第二次啟動與 `/recovery-status` 皆被拒絕（`OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`）；結尾帶分隔符號的拼寫亦同（`ID01b`，工作階段 `ba3ea7c9-9826-453d-b447-81c48763e18f`）。 |
| 發行套件一致性：乾淨的暫存區、`Native.x86` 組建收據（依內容而非時間戳記）、不含 BOM 的資訊清單、已暫存封閉集與重新解壓縮封存檔的完整性、安裝程式會複製資訊清單（修正回合 CP-01、Round A RA-007、附錄 F/G/H） | STATICALLY_VALIDATED；已於安裝中觀察到 | `Test-PackagingPipeline.ps1`（候選套件一致，外加 16 個負面案例）、`runtime.release-manifest-bom`、`runtime.release-manifest-strict-parser`。執行階段收尾：安裝到經授權安裝中的套件都與其資訊清單一致，且每個工作階段都以 `plugin.integrity.reference = manifest` 進行規劃（候選版本台帳 `research/reports/runtime-closure/CANDIDATES.md`）。 |
| 外掛程式封閉集對照 `release-manifest.json` 的完整性（S-09） | RUNTIME_VALIDATED | Round A 工作階段 `47efd323-4ce1-400a-ac83-74893cf82f26`；在已安裝發行套件上的執行階段收尾工作階段（例如 `R01` 工作階段 `878b5e3c-2f9d-46bf-a918-11bb2e227e55`，候選版本 3）以 `plugin.integrity.reference = manifest` 進行規劃；`R02` 顯示缺少一個列出的外掛程式會阻擋啟動。 |
| 處理程序內組建驗證（`plugin.build.invalid` -> `OL_E_BUILD_VALIDATION_FAILED`） | RUNTIME_VALIDATED | 矩陣工作階段；先前兩個在過時外掛程式上發生 `plugin.build.invalid` 的工作階段 |
| 使用 SHA-256 的 Handoff v4 | RUNTIME_VALIDATED | 整合測試 `handoff.*`；每個工作階段 |
| 沒有 OmsiLaunch 時外掛程式不作用（無 handoff） | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`P01`（單純啟動 OMSI，沒有 OmsiLaunch 工作階段；OMSI pid 35564）：外掛程式封閉集與 .NET 主機已載入，`OmsiLaunch.Native.x86.dll` 未載入，三個原生修補位置保持未修補的 `Omsi.exe` 位元組，且沒有出現控制管道、日誌或診斷資料 |
| DLL 搜尋原則（`AssemblyDirectory | System32`）（S-24） | STATICALLY_VALIDATED | 組件屬性 |
| Windows 10+ x64 偵測以及拒絕其他平台 | STATICALLY_VALIDATED | `CurrentWindowsX64Platform.Detect` |

<a id="runtime-control-channel-and-control-plane"></a>
## 執行階段控制通道與控制平面

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| 信箱（mailbox）傳輸完整性、工作階段繫結 | RUNTIME_VALIDATED | 所有執行階段工作階段；`runtime-command.wire-guard`、`runtime-command.session-binding` |
| 捨棄延遲回應，且逾時後通道不會卡住（S-08） | RUNTIME_VALIDATED | Round A 工作階段 `173414a5-ee53-4b10-aeb7-046f40b851e9`：外部用戶端在 250 ms 後放棄 `road-vehicles.spawn`；之後的 `map.read` 成功，標準停止清理了工作階段。超大回應的拒絕仍僅有離線證據。 |
| 本機控制平面：每個安裝各自的管道名稱、`session_id` 繫結、具型別的處理常式錯誤、64 KiB 上限（S-06） | RUNTIME_VALIDATED | Round A 工作階段 `a00c6ff8-9cc9-4bce-ae44-c4314daad7bf`：無效協定、未繫結的停止、錯誤工作階段的執行階段要求與超大訊框皆被拒絕；隨後有效的狀態要求與已繫結的停止皆成功。超大回應的拒絕仍僅有離線證據。 |
| 遙測序號槽位、略過撕裂取樣、相同的連續事件（S-25） | STATICALLY_VALIDATED | `telemetry.sequence-samples` |
| API 邊界拒絕 `internal.*` 並移除內部結果鍵（S-02） | STATICALLY_VALIDATED | `OmsiLaunchService.ExecuteRuntimeAsync`；登錄驗證 |
| 無效或超大的信箱回應會釋放槽位；下一個要求會成功（修正回合 CP-03） | STATICALLY_VALIDATED | `runtime-command.oversized-response-rejected`；外掛程式端 `plugin-runtime.oversized-and-abandoned-responses`。無法在執行階段產生：沒有任何公開操作會回傳超過一個槽位的內容（最大約 37.8 KB，執行階段收尾 `R03`）。 |
| 執行階段要求的每條終止路徑都讓信箱可再次使用：具型別的拒絕、逾時、呼叫端取消、重複使用的要求 id、內部操作、錯誤的工作階段、缺少引數（執行階段）；格式錯誤、超大、損毀、過時及孤立的回應（離線） | RUNTIME_VALIDATED（可觸及的路徑） | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R03` 工作階段 `1134fdc3-a469-4ff5-a511-9964ed65e7cd`：每個失敗都具有型別，且下一個有效要求皆成功。回應損毀的路徑無法從外部產生，仍僅有離線證據（`runtime-command.terminal-paths-leave-channel-usable`）。 |
| 超大的控制平面回覆以 `OL_E_CONTROL_RESPONSE_TOO_LARGE` 回應；status／events 由最舊的開始修剪至一個訊框（修正回合 CP-04） | STATICALLY_VALIDATED | `control-plane.binding-and-typed-errors`。無法在執行階段產生：執行階段收尾回合中觀察到的最大公開回覆約為 37.8 KB（`R03`），而工作階段的事件歷程遠低於一個訊框。 |
| 連線後控制平面絕不沉默：格式錯誤、超大、負長度、空白、JSON `null`、缺少命令、非字串引數、無效協定、未知命令、未繫結及錯誤工作階段的訊框；被截斷的用戶端訊框；停滯的用戶端；在回覆前離開的用戶端 | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R04` 工作階段 `faaaf8a8-126d-4a23-a8f3-381e47c58de6`（候選版本 2，修正 BUG-02 之後）：每個訊框都得到其具型別的錯誤，且每次之後的 `session.status` 都成功；停滯的用戶端不會阻擋其他用戶端；已繫結的停止與進行中的 `road-vehicles.spawn` 競爭時，工作階段結束，spawn 的呼叫端得到乾淨的串流結尾。`OL_E_TIMEOUT` 與截斷中繼資料仍僅有離線證據（`control-plane.errors-never-silent`）。 |
| 執行階段批次測試工具（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`） | INTERNAL (STATICALLY_VALIDATED) | 僅為驗證測試工具（S-27） |

<a id="runtime-operations"></a>
## 執行階段操作

| 操作 | 狀態 | 證據 |
| --- | --- | --- |
| `time.read`、`time.set` | RUNTIME_VALIDATED | 工作階段 `e5454061`（寫入、`SetTime`、讀回、還原） |
| `weather.read`、`weather.actual.read` | RUNTIME_VALIDATED | 工作階段 `e5454061` |
| `weather.set` | UNAVAILABLE | 以 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` 拒絕；在工作階段 `e5454061` 與 `5f641c8d` 中持續性檢查 FAIL；在 `50a1f1ec` 中拒絕檢查 PASS |
| `map.read` | RUNTIME_VALIDATED | 在修正 Map 全域槽位之後，於工作階段 `9dd62626-94c6-4cd7-bb6f-0288327696f4` 中重新驗證：`loaded=true`、19 個圖塊，Grundorf 的識別、檔名、描述與年份範圍一致。 |
| `camera.read`、`camera.set` | RUNTIME_VALIDATED | 工作階段 `e5454061`（FOV 寫入、讀回、還原） |
| `camera.lock`、`camera.unlock` | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`CAM01` 工作階段 `5dfd9b96-fa94-4060-85af-0c9d19823a73`：已儲存情境 `Linie 5` 提供了 PlayerVehicle（`rv-000001`）；對家族 0、2 與 1 的鎖定皆經攝影機讀回確認；解除鎖定釋放了該原則 |
| `road-vehicles.read`、`road-vehicles.list`、`road-vehicle.read` | RUNTIME_VALIDATED | 工作階段 `e5454061`、`5f641c8d` |
| RoadVehicle handle 存續期間與失效偵測（RV-002） | PARTIAL | 沒有安全的移除產生方式：在三個執行階段收尾時間窗（`H01`、`H01b`、`H01c`，最長 180 s，並包含時鐘跳躍）中，Grundorf 的 RoadVehicle 與 Human 數量只增不減；沒有任何公開操作會移除物件。失效偵測仍以離線方式涵蓋（S-19）。generation／ABA 規則已針對 D3D 材質於執行階段驗證（`H02`）。 |
| `road-vehicles.spawn`（RV-003） | RUNTIME_VALIDATED | 工作階段 `5f641c8d`：`2 -> 3`，`rv-000003` 已解析 |
| `road-vehicles.place-random` | RUNTIME_VALIDATED | 矩陣：集合 `2 -> 4` |
| `player-vehicle.read` | RUNTIME_VALIDATED | Round A RA-002 工作階段 `9dd62626-94c6-4cd7-bb6f-0288327696f4`（`present=false`）；執行階段收尾 `CAM01` 工作階段 `5dfd9b96-fa94-4060-85af-0c9d19823a73` 從已儲存情境讀取到存在的 PlayerVehicle |
| `humans.read`、`humans.list`、`human.read` | RUNTIME_VALIDATED | 工作階段 `e5454061` |
| `timetable.read`、`timetable.*.list`、`timetable.logs.read` | RUNTIME_VALIDATED | 工作階段 `e5454061`；路線軌跡項目（track entry）91 筆記錄 |
| 以有界清單截斷取代 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（文件稽核 BUG-05） | RUNTIME_VALIDATED | 在套件 `9F8CC87B...`、已儲存情境 `situations\Linie 5.osn`（Berlin-Spandau）上的文件稽核重測：`timetable.track-entries.list` 回傳 825 筆中的 137 筆，`timetable.tour-entries.list` 回傳 4747 筆中的 224 筆，且皆為 `truncated=true`（工作階段 `08df97b8-06cc-4faa-be8b-f49ca1538dce` 與文件擷取重測）；修正前兩者皆以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失敗（工作階段 `f51dcb59-a723-4570-b6c5-b61e628a7993`）。證據：`research/reports/documentation-audit/runtime/`。 |
| `drivers.read`、`tickets.read` | RUNTIME_VALIDATED | 工作階段 `e5454061` |
| `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RUNTIME_VALIDATED | `Refresh_Strings` 0 -> 1 -> 0，發生於 `research/reports/OmsiHook-Parity-Wave-A-Closure-001.md` 中記錄為 Runtime-Proven 的執行階段工作階段（依 `OMSILAUNCH-AUTONOMOUS-BUILD-001.md`，列舉了 1,025 個名稱）；該報告未記錄工作階段 id |
| `vehicle.string-variables.list`、`vehicle.string-variable.get` | RUNTIME_VALIDATED（僅讀取） | 工作階段 `e5454061`；寫入為 BI-004 UNAVAILABLE |
| `vehicle.constants.list`、`vehicle.constant.get`、`vehicle.curves.list`、`vehicle.curve.evaluate`、`vehicle.hofs.read` | RUNTIME_VALIDATED | 工作階段 `e5454061` |
| `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release`、拒絕已釋放的 handle | RUNTIME_VALIDATED | 矩陣「basic D3D texture lifecycle」；`runtime.d3d.*` 功能附註（裝置槽位為工作階段 `4378899d`） |
| D3D 裝置遺失／重設／復原事件、generation 失效（RV-007） | RUNTIME_VALIDATED（resetting/restored） | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`D01` 工作階段 `fc07946e-58dd-4ddc-af7e-10c7e61a899a`：一次暫時的顯示模式變更（測試用產生方式，絕不公開，沒有合成的重設呼叫）使 OMSI 重設其裝置兩次。發出了 `d3d.resetting`/`d3d.restored`，每個循環 generation 都遞增，重設期間的要求得到 `OL_E_D3D_RESET_IN_PROGRESS`，遺失前的材質以 `OL_E_D3D_STALE_RESOURCE_HANDLE` 被拒絕，新材質可正常運作。OMSI 直接進入 `DEVICENOTRESET`，因此未產生獨立的 `lost` 轉換。有一次嘗試使 OMSI 在 `DSound.dll` 中當機（顯示器音訊端點變更；擁有者精確還原）。 |
| `events.read` / `events watch` | RUNTIME_VALIDATED | 每個工作階段（遙測驅動的生命週期） |
| `internal.road-vehicles.make-basic` | INTERNAL | 無法透過公開介面觸及 |

<a id="launch-features"></a>
## 啟動特性

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| `/set` 與設定檔 `settings`（`configuration.options.semantic`） | RUNTIME_VALIDATED | RV-005 |
| 工作階段設定檔（嚴格編譯器、衝突、路徑限制，包括重新剖析點 S-17） | RUNTIME_VALIDATED（設定檔啟動範圍） | Round A RA-009，`Test-Beta3SessionProfile.ps1`，2026-09-22：managed 啟動畫面、停用 Internet Textures（網路材質）以及 `graphics.maxFPS=30` 預設組合（preset）；`options.cfg` 已還原，測試固件完好，無日誌／租約／處理程序殘留。 |
| `/spec` 載入（1 MiB 上限、拒絕未知屬性、遵守逾時）（S-20） | STATICALLY_VALIDATED | `cli.*` 測試 |
| Managed 啟動畫面 `PTB`/`ENG`/`DEU`/`FRA` | RUNTIME_VALIDATED | RV-006 |
| Internet Textures `Disabled`（處理程序內下載器抑制） | STATICALLY_VALIDATED | `internet-textures.suppressed` 遙測路徑；矩陣中未引用 |
| Internet Textures `.itx` 目標防護（Round A RA-011/RA-012、修正回合 CP-05、附錄 E）：標準根目錄、標準目標、相對路徑、區段規則；只有根目錄之下名為 `texture` 的目錄區段才符合（`mytexture`、`texture_backup`、`texture2` 以及絕對根目錄中的 `texture` 皆不符合）；拒絕周遊、絕對、相對於根目錄及連接點（junction）目標；各種拼寫會收斂為同一個標準交易路徑 | STATICALLY_VALIDATED | `presentation.itx-target-root-normalization`、`presentation.itx-target-guard-rejections` |
| Internet Textures `Override` | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`I01` 工作階段 `6eac2dac-c7e9-49a1-acbf-2a5d59c1c0d3`：OMSI 的下載器針對一個既有目標與一個原本不存在的目標，向回送（loopback）伺服器送出 `HEAD` 與 `GET`，並寫入兩者；標準停止後，原本不存在的目標被移除，既有目標逐位元組精確還原；`I02` 工作階段 `a8f2b720-e41a-4e34-86bf-125bf519a5bd`：在擁有者與 OMSI 都被終止後，透過 `/recover` 得到相同結果。 |
| `/date`、`/time`、`/year`、天氣旗標、玩家車輛旗標 | UNAVAILABLE | 規劃器將計畫標示為不可執行（`OL_E_CAPABILITY_UNAVAILABLE`）；外掛程式拒絕非 `Unset` 的日期／時間模式 |
| `/entrypoint:<identity>`（BI-001） | PARTIAL | 計畫不可執行（計畫證據 `PARTIAL`） |
| `/last`（`LastMapState`，BI-006） | UNAVAILABLE | 計畫不可執行；沒有 `.osn` 退路 |
| `InputSpec` 鍵盤／控制器 overlay（BI-005） | UNAVAILABLE | 剖析器存在；未套用 |
| 無頭 PlayerVehicle 指派（BI-007）、`SetActualDateTime`（BI-002）、ICAO 天氣控制（BI-003）、字串變數寫入（BI-004）、跨圖塊重新定位（BI-008） | UNAVAILABLE | 在實作完成前受阻 |
| `/list` 探索（`content.*`） | STATICALLY_VALIDATED；已於執行階段觀察到 | `discovery.fixture`；執行階段收尾對經授權的安裝使用了 `/list:situations`（識別包括非 ASCII 的 `situations\Nur für Fortgeschrittene.osn`） |
| `/quiet`、`/serve` | UNAVAILABLE | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT |
| 診斷旗標（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace*`） | PARTIAL | 攜帶於 `DiagnosticsSpec` 中；效果僅限於主機追蹤記錄 |

<a id="windows-host-and-tray"></a>
## Windows 主機與系統匣

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| 系統匣指示器（`OmsiLaunchW.exe`）：通知區域中的圖示、狀態、附確認與「Cancel」的「End session」、檔案總管重新啟動、停止到達時仍開啟的對話方塊、在 `/observe-seconds` 期間停止、失敗對話方塊（S-16） | RUNTIME_VALIDATED | 在候選版本 3（修正 BUG-04 之後）上的執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`T01` 工作階段 `6d827c13-caf8-4221-832a-c91d26f130e0`（在 `Shell_TrayWnd` 中找到圖示、pt-BR 狀態視窗、檔案總管重新啟動後重新加入圖示、「Cancel」保留工作階段、「End session」在 607 ms 內結束工作階段並移除圖示）；`T02` 工作階段 `0939a151-bf03-4ca9-8294-8b0088df7d30`（管道停止期間狀態視窗與確認對話方塊為開啟狀態）；`T03` 工作階段 `7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`；`T04` 工作階段 `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（引數錯誤、無作用中工作階段及工作階段失敗的對話方塊，皆附錯誤碼）。未產生 shim 結束代碼 100-106。 |
| `/silent` 重新啟動在主機已啟動時回傳 0 | RUNTIME_VALIDATED | 執行階段收尾回合（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`T04` 工作階段 `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（候選版本 3，修正 BUG-03 之後）：啟動器回傳 0，擷取其輸出的呼叫端在 0.4 s 後得到串流結尾，而 OmsiLaunchW 工作階段在沒有主控台視窗的情況下執行；工作階段停止並精確還原 |
| Bootstrapper shim 結束代碼 100-106、動態路徑緩衝區（S-21） | STATICALLY_VALIDATED | `Test-ReleaseIdentity.ps1` |

<a id="documentation-audit-retest-010-beta3-documentation-closure"></a>
## 文件稽核重測（0.1.0-beta3 文件收尾）

在經授權的安裝上使用套件 `9F8CC87B238A677F7238C262E8FE0496832263B6C0A58083409EB485C03C6F17`，並在最終套件 `B8462ED6C596843E778C33DF4E93EBA3ACC875B7E43B011D60E5A21A16032E35` 上重複 BUG-05、BUG-06、BUG-07 與冒煙測試（R01 工作階段 `56cdee4b-9160-49fc-b0fd-5134ff9fe2d0`，文件擷取工作階段 `40046805-5337-48fc-90dd-e7d3500ed3b4`）；證據位於 `research/reports/documentation-audit/runtime/` 與 `research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`。

| 項目 | 狀態 | 證據 |
| --- | --- | --- |
| `OmsiLaunchW.exe` 回報不可執行的啟動（BUG-06） | RUNTIME_VALIDATED | 在 `OmsiLaunchW.exe` 下執行 `/new ... /date:2026-09-20`：對話方塊顯示 `Requested capability unavailable: world.explicit-date` / `Code: OL_E_CAPABILITY_UNAVAILABLE`，結束代碼 1，沒有 OMSI 處理程序（`doc-bug06-dialog.json`；已安裝套件 `9f8cc87b...`，依設計不會建立工作階段） |
| 公告的 CLI 路由皆為實際可用的語法（BUG-07） | RUNTIME_VALIDATED | 已安裝的 `help --json` 與 `capabilities --json`（32 個描述項）所列印的每個用戶端路由替代寫法，都被已安裝的剖析器接受；沒有任何一個列印出用法說明（`doc-bug07-routes.json`；已安裝套件 `9f8cc87b...`） |
| CLI 範例頁面中的每一行命令 | RUNTIME_VALIDATED | 對擁有者工作階段 `08df97b8-06cc-4faa-be8b-f49ca1538dce`（已儲存情境；`events watch` 因會執行到 Ctrl+C 為止而略過）執行了 43 行用戶端命令，最後以該頁面本身的 `session stop` 結束（擁有者結束代碼 0）。除了 `vehicle.constant.get` 搭配 `name=max_speed` 以外，所有結果都與頁面相符；該巴士沒有這個常數，頁面現改用 `antrieb_getr_version`。`grundorf-quick` 設定檔範例無法規劃（純 YAML 純量中的反斜線被重複）且已修正；所有不需工作階段的命令列都從安裝根目錄執行，啟動命令列則搭配 `/plan` 執行（`doc-client-examples.json`、`doc-examples-no-session.json`、`doc-profile-example.json`） |
| 在最終套件上以 `/observe-seconds` 進行 NEW_MAP 冒煙測試 | RUNTIME_VALIDATED | `R01` 工作階段 `08570134-cfd8-4726-a472-5f6dd2f04c8f`，結束狀態乾淨（`research/reports/runtime-closure/doc-R01-*`） |

<a id="beta-4-development-round-a-2026-09-28"></a>
## Beta 4 開發（第 A 輪，2026-09-28）

已知配置 `Omsi23004` 上的開發候選版 `4AF6A180…`（`research/reports/beta4-runtime/ROUND-A-REPORT.md`）。

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| 處理程序內依功能的原生解析、無位址的原生邊界（已知配置） | RUNTIME_VALIDATED | A02 工作階段 `088f0491-114a-4841-afc0-e5ba483364b5`：無介面啟動、NEW_MAP、43 個用戶端範例中的 40 個（2 個失敗是已記載的 `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` 與 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`），透過原生 `SetTime` 的 `time set`，透過原生 `MakeVehicle` 的 `vehicles spawn`；A03 工作階段 `cf49d2c1-ca69-41e2-af0c-c4b08fa083c3`：已儲存情境 `Linie 5`。工作階段擁有的檔案已精確還原。 |
| 在沒有已知配置的 OMSI 2 組建上，不依賴組建的解析 | STATICALLY_VALIDATED | 離線：隱藏其程式碼識別的參考執行檔，在 82 個非配置目標中有 81 個解析為參考值（`build-detection.reference-executable`、`native-resolution.per-capability`）。沒有其他 OMSI 2 組建可供執行工作階段。 |
| OMSI 2 偵測、計畫證據、`profiles` 報告 | RUNTIME_VALIDATED | 已安裝候選版 `4af6a180` 上的 A01：`is_omsi2`，識別 `known`，`build_id` 為 `Omsi23004`；工作階段 `088f0491` 與 `cf49d2c1` 的計畫都接受了該安裝。 |
| 具型別的執行階段錯誤碼一路保留到 CLI | RUNTIME_VALIDATED | A02 工作階段 `088f0491-114a-4841-afc0-e5ba483364b5`（上述代碼原封不動地到達 CLI）。 |
| HOF 管理（鏡像、精確還原） | RUNTIME_VALIDATED | C01 工作階段 `e37318a2-73b3-4fb3-be11-b0225f15226d`：`Vehicles\MB_O305` 在工作階段期間恰好持有 `Vehicles\MAN_SD202` 的 14 個 HOF，並逐位元組還原，包括時間戳記與屬性。 |
| HOF 管理（當機復原） | RUNTIME_VALIDATED | C02 工作階段 `02568113-3f28-4e31-958b-23eac83d7b0b`：在 `Running` 時終止擁有者與 OMSI；`/recover` 精確還原了 `Vehicles\MAN_SD200` 並移除日誌。 |
| 工作階段設定檔 1..99 與探索 | STATICALLY_VALIDATED | 離線關卡 `session-profiles.preset-range-1-99`、`session-profiles.discovery`、`session-profiles.hofs`。 |
| Internet Textures `Disabled` | STATICALLY_VALIDATED | A04 以該模式正常啟動並結束；`PluginStart` 遙測無法透過 Beta 3 的最新值槽位觀察到。已由第 B 輪取代（見下）。 |

<a id="beta-4-development-round-b-2026-09-29"></a>
## Beta 4 開發（第 B 輪，2026-09-29）

已知配置 `Omsi23004` 上的開發候選版 `beta4-b2` 至 `beta4-b5`（`research/reports/beta4-runtime/ROUND-B-REPORT.md`）。

| 特性 | 狀態 | 證據 |
| --- | --- | --- |
| 遙測事件環狀緩衝區（每個外掛程式事件依序送達；單調遞增的工作階段 tick） | RUNTIME_VALIDATED | B03 工作階段 `0c5ae237-c587-47b4-a409-a55989e7e38b`：依序收到所有 `PluginStart` 事件，包括 `native.capabilities` 與 `internet-textures.suppressed`，沒有 `telemetry.lost`；B06 工作階段 `f231ab2a-5b0d-40e6-a21d-085e061f1de8`：事件 tick 單調遞增。 |
| 標準功能目錄與針對某個安裝的 `GetCapabilitiesAsync` | RUNTIME_VALIDATED | 第 B 輪，已安裝候選版 `1355c928` 上的 B01：53 個目錄項目，含操作、原生功能、分頁與變更原則；`GetCapabilitiesAsync` 評估了 68 個項目（`time.read`：`native: RESOLVED_KNOWN_LAYOUT`）；B02 工作階段 `d875a24e-8154-4774-93b3-83567a90dbf1` 透過衍生的 CLI 路由執行了每個用戶端範例。 |
| 執行階段模組 v1：信任、規劃、內容啟用、生命週期、操作、`disable-module` | RUNTIME_VALIDATED | B05（安裝上的信任與計畫）；B06 工作階段 `f231ab2a-5b0d-40e6-a21d-085e061f1de8` 與 `227d35a3-7e3d-4fcb-9013-f129840c49a7`：三個模組已載入、已啟動並就緒（其中一個由內容啟用），操作與具型別錯誤經由 CLI 傳達，失敗的模組被停用並釋放，而 OMSI 持續執行。 |
| 執行階段模組 v1：`fail-session` | RUNTIME_VALIDATED | B07 工作階段 `59324c59-c3bc-4118-9fe9-57fbdc26f353`：某個操作失敗後，工作階段在 185 ms 後以 `OL_E_MODULE_SESSION_FAILED` 結束，OMSI 停止且檔案已還原；載入階段的情形見工作階段 `8b1d392f-4453-4e88-9231-d41a8fff1829`。 |
| 原生模組元件與 hook 登錄 | PARTIAL | 工作階段 `f231ab2a-5b0d-40e6-a21d-085e061f1de8`、`227d35a3-7e3d-4fcb-9013-f129840c49a7` 與 `dfbd70a5-11a9-401d-967c-a9d154e15129`：`Omsi.exe` 匯入的 IAT hook 已安裝、串接並移除；未知目標被拒絕；裝置 `Present` 與 `Reset` hook 已安裝（`Reset` 疊加在產品自己的 hook 之上）。未解決：在此安裝上，登錄之外的某個元件在遊戲過程開始後不久，把 runtime 原本的 `Present` 與 `Reset` 寫回裝置的 vtable，因此裝置 hook 並未持續存在（模組的 `Present` detour 從未執行）；登錄將兩個位置都回報為已變更（`OL_E_MODULE_CLEANUP_INCOMPLETE`）且未覆寫它們。屬於 Stage E 的項目。 |
| 執行階段清單的統一分頁 | RUNTIME_VALIDATED | B04 工作階段 `8a45e79c-37c2-4f66-a3a3-b536a00a2421`（Berlin-Spandau）：20 個清單分頁後恰好等於其數量，沒有任何資料列重複，包括以符合信箱大小的頁面取得的 4747 個班次項目與 825 個路線軌跡項目；cursor 與 limit 錯誤皆為具型別。 |
| Internet Textures `Disabled` | RUNTIME_VALIDATED | B03 工作階段 `0c5ae237-c587-47b4-a409-a55989e7e38b`：原生抑制已套用（`internet-textures.suppressed`，沒有 `internet-textures.suppression.failed`），工作階段正常執行並結束。 |

<a id="consolidated-runtime_validation_required-list"></a>
## 彙整的 RUNTIME_VALIDATION_REQUIRED 清單

2026-09-23 的執行階段收尾回合結清了先前的待辦佇列。除了在此安裝上無法安全產生的項目之外，佇列維持為空：

1. Steam LAA 的遊戲進行、讀取、命令與停止：需要真正的 Steam 安裝（環境因素；上方對應列為 `PARTIAL`）。
2. RV-002 RoadVehicle 與 Human 的移除：沒有安全的移除產生方式（上方對應列為 `PARTIAL`）。

因無法從產品外部產生而維持離線的項目，已在各自的列中標註，不屬於此佇列：信箱回應損毀、超大回覆、事件歷程截斷、D3D 的 `lost` 轉換、登出、shim 結束代碼 100-106，以及延後的 pre-fingerprint 復原分支。

相關頁面：[功能](../reference/capabilities.md)、[已知限制](../reference/known-limitations.md)、[交易與復原](../concepts/transactions-and-recovery.md)、[相容性](../reference/compatibility.md)。
