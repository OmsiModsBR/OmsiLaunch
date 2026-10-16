# 工作階段生命週期

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../concepts/session-lifecycle.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁說明 OmsiLaunch 工作階段如何在 `SessionState` 中從 `Created` 前進到 `Completed` 或 `Failed`：每個狀態由哪個元件設定、哪些外掛程式遙測事件驅動狀態轉換、啟動逾時如何運作、停止代表什麼（強制終止）、`WaitForAsync` 回傳什麼、哪些狀態是終止狀態、哪些狀態從未或幾乎無法觀察到，以及 CLI 擁有者提供哪些保證。本頁內容全部取自 `OmsiLaunchService.StartAsync`、`SuperviseAsync` 與 `ApplyTelemetry`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）、`PluginRuntime`（`src/OmsiLaunch.Plugin/PluginRuntime.cs`）以及 `OwnerSession`（`tools/OmsiLaunch.Cli/Program.cs`）。

相關頁面：[公開 API](../reference/public-api.md)、[錯誤碼](../reference/errors.md)、[交易與復原](transactions-and-recovery.md)、[永久外掛程式](permanent-plugin.md)、[執行階段控制](../reference/runtime-control.md)、[本機控制平面](../reference/local-control.md)、[Windows 系統匣](../reference/windows-tray.md)、[CLI 參考](../reference/cli.md)、[執行階段驗證狀態](../status/runtime-validation-status.md)、[`.omsilaunch` 目錄](../../../concepts/omsilaunch-directory.md)。

<a id="overview"></a>
## 概觀

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## `SessionState` 參考

各值依宣告順序列出。「設定者」指出呼叫 `Move`/`Fail` 的程式碼；「可觀察」說明 `GetStatusAsync`/`WaitForAsync` 實際上能否看到該狀態。

| # | 狀態 | 設定者 | 可觀察 | 意義 |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync`（即時工作階段的初始值） | 短暫 | 工作階段已註冊；尚未發生任何事。 |
| 1 | `ValidatingPlatform` | 無 | 否 | 已宣告，但目前的服務從未設定（平台驗證發生在 `PlanSessionAsync`，該處沒有工作階段狀態）。 |
| 2 | `Planning` | 無 | 否 | 已宣告，從未設定（規劃發生在工作階段存在之前；`StartSessionAsync` 中的重新規劃也早於註冊）。 |
| 3 | `AcquiringInstallationLock` | `StartAsync` | 是 | 正在取得安裝租用（lease）。失敗：`OL_E_INSTALLATION_BUSY`。 |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | 是 | 在讀取實際 OMSI 安裝之前，還原待處理的 `journal.json`；驗證永久外掛程式封閉集合（`plugin.integrity.reference`）、計算 `Omsi.exe` 的雜湊、預先放置啟動畫面資產、移除過時的 `closecheck`。失敗：`OL_E_PERMANENT_PLUGIN_*`、`OL_E_SPLASH_*`、`OL_E_ITX_*`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_*`、`OL_E_INSTALLATION_BUSY`（日誌中記錄的處理程序仍存活）。 |
| 5 | `Snapshotting` | `StartAsync` | 實際上否 | 緊接在 `ApplyingConfiguration` 之前設定，中間沒有任何 await；快照本身是在 `ApplyAsync` 內擷取。屬於暫態，無法觀察。 |
| 6 | `ApplyingConfiguration` | `StartAsync` | 是 | 寫入 `journal.json`（`Prepared`）、備份原始檔案、寫入 overlay、移除工作階段刪除項目（`Applied`）。失敗：`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、I/O 錯誤。 |
| 7 | `DeployingRuntime` | `StartAsync` | 是 | 日誌狀態 `RuntimeDeployed`。不部署任何檔案：外掛程式封閉集合是永久安裝的。 |
| 8 | `CreatingStartupHandoff` | `StartAsync` | 是 | 交接區（`OmsiLaunch.Handoff.<id>`）、遙測槽位（`OmsiLaunch.Telemetry.<id>`）與執行階段信箱（`OmsiLaunch.Runtime.<id>`）已存在；日誌狀態 `HandoffCreated`。 |
| 9 | `StartingProcess` | `StartAsync` | 是 | 以工作目錄 `<root>` 對 `<root>\Omsi.exe` 呼叫 `CreateProcessW`。失敗：`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。 |
| 10 | `WaitingForPlugin` | `StartAsync` | 是 | 處理程序已存在，已記錄 `process.started`，日誌為 `ProcessStarted`，監督程式已啟動，且 `StartSessionAsync` 回傳。 |
| 11 | `PluginBootstrap` | `ApplyTelemetry`，於 `plugin.started` 時 | 是 | 永久外掛程式已讀取此工作階段的有效交接資料。自此之後，逾時為 `OL_E_STARTUP_TIMEOUT`，而非 `OL_E_PLUGIN_NOT_LOADED`。 |
| 12 | `StartingWorld` | `ApplyTelemetry`，於 `world.starting` 時 | 是（僅 NEW_MAP） | 外掛程式已在 OMSI 的 UI 執行緒上呼叫原生 NEW_MAP 啟動。已儲存情境會發出 `world.situation.starting`，該事件未對應到狀態，因此 SAVED_SITUATION 工作階段會從 `PluginBootstrap` 直接進入 `Running`。 |
| 13 | `EnteringGameplay` | 無 | 否 | 已宣告，從未設定：`gameplay.entered` 會讓工作階段直接進入 `Running`。 |
| 14 | `Running` | `ApplyTelemetry`，於 `gameplay.entered` 時 | 是 | 已進入遊戲。允許呼叫 `ExecuteRuntimeAsync`；CLI 擁有者開啟本機控制平面；系統匣顯示執行中的工作階段。 |
| 15 | `ProcessExited` | `SuperviseAsync` | 是（僅成功的工作階段） | OMSI 已結束（自然結束或遭終止），日誌為 `ProcessExited`。 |
| 16 | `Restoring` | `SuperviseAsync`（以及啟動失敗路徑） | 是（僅成功的工作階段） | 每個工作階段擁有的檔案都從其已驗證的備份還原；移除工作階段產生的檔案。 |
| 17 | `CleaningRuntime` | `SuperviseAsync` | 是（僅成功的工作階段） | 還原已驗證，日誌與備份已移除；即將處置 runtime 儲存區。 |
| 18 | `Completed` | `SuperviseAsync`（`finally`） | 是，終止狀態 | 儲存區已處置、信箱已關閉、租用已釋放，未記錄任何失敗。 |
| 19 | `Failed` | 由 `StartAsync`、`SuperviseAsync`、`ApplyTelemetry` 呼叫 `LiveSession.Fail` | 是，終止狀態 | 已記錄失敗診斷訊息。此狀態具黏滯性：之後的 `Move` 呼叫會被忽略，因此失敗的工作階段永遠不會顯示 `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`，即使終止與還原仍會執行。 |

終止狀態：`Completed` 與 `Failed`。進入其中任一狀態後，`WaitForAsync` 會立即回傳，`CloseAsync` 會直接回傳而不要求停止。

「從未設定」的驗證方式：在程式碼庫中搜尋 `SessionState.ValidatingPlatform`、`SessionState.Planning` 與 `SessionState.EnteringGameplay`，只會找到列舉宣告；`SessionState.Snapshotting` 只出現一次，且緊接著 `Move(SessionState.ApplyingConfiguration)`。

<a id="start-phase-startsessionasync"></a>
## 啟動階段（`StartSessionAsync`）

1. 拒絕不可執行的計畫（`OL_E_PLAN_NOT_RUNNABLE`），重新規劃 spec（重新計算 `Omsi.exe` 的雜湊、重新解析內容、重新檢查外掛程式封閉集合），若已不再可執行則再次拒絕。註冊即時工作階段（`Created`）。
2. 建立主機追蹤記錄 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`（超出最新 50 個工作階段的較舊工作階段前綴檔案會被清除）。拒絕超出 1..600 範圍的 `StartupTimeoutSeconds`（`ArgumentOutOfRangeException`；該工作階段會被取消註冊）。
3. `AcquiringInstallationLock` → 租用。`RecoveringPreviousTransaction` → 復原待處理的日誌（無法證明擁有權的指紋前日誌會延後處理，待此工作階段的 overlay 產生後再重試一次）、驗證永久外掛程式封閉集合、計算執行檔雜湊、移除過時的 `closecheck`、建立交易（overlay：`options.cfg` 修補、受管理的啟動畫面 BMP、`Texture\standard.itx`；刪除項目：ITX 目標、`Texture\standard.ipr`、不存在時的 `closecheck`）。
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`，接著啟動監督程式工作並回傳 handle。
5. 步驟 3–4 中的任何例外都會被攔截：工作階段以 `OL_E_START_SESSION`（附內部訊息）進入 `Failed`，已建立的處理程序會被終止並等待其結束，儲存區會被處置，交易會被還原（失敗時為 `OL_E_RESTORE_FAILED`），或在無法確認 OMSI 已結束時以 `OL_E_RESTORE_DEFERRED` 保持待處理；租用會被釋放。此情況下 `StartSessionAsync` 仍會回傳 handle；請讀取 `GetStatusAsync`。

重新規劃會保留呼叫端的 `SessionId`，因此 handle 中的 id 等於 `plan.SessionId`。

<a id="supervision-superviseasync"></a>
## 監督（`SuperviseAsync`）

監督程式在執行緒集區工作上執行，每 100 ms 循環一次，直到 OMSI 結束或收到停止要求：

1. 依序讀取外掛程式自上次讀取以來提交的每個遙測事件（事件環狀緩衝區；落後的讀取端會收到附帶計數的 `telemetry.lost`）。每個事件都會附加到 `RuntimeEvents`，並由 `ApplyTelemetry` 對應處理。
2. 若工作階段為 `Failed`，離開迴圈。
3. 若工作階段尚未 `Running` 且期限（監督程式進入後 `StartupTimeoutSeconds`）已過：若已達 `PluginBootstrap`，以 `OL_E_STARTUP_TIMEOUT` 呼叫 `Fail`，否則以 `OL_E_PLUGIN_NOT_LOADED`；離開迴圈。

迴圈結束後：若 OMSI 在 `Running` 之前結束且未記錄任何失敗，以 `OL_E_PROCESS_EXITED_EARLY` 呼叫 `Fail`。接著，無論工作階段是否失敗：若 OMSI 仍存活則將其終止、等待結束、將日誌標記為 `ProcessExited`、進入 `ProcessExited`、還原（`Restoring` → `CleaningRuntime`）或記錄 `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`、處置處理程序 handle、交接區、遙測槽位與執行階段信箱、釋放租用，並在狀態不是 `Failed` 時進入 `Completed`。監督程式本身內部的錯誤會記錄為 `OL_E_PROCESS_SUPERVISION`（清理問題記錄為 `OL_E_PROCESS_CLEANUP_FAILED`），並執行相同的終止／還原路徑。

由於 `Failed` 具黏滯性，失敗的工作階段已被還原的唯一證據，是其診斷訊息中沒有 `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`（以及 `journal.json` 不存在）；還原附註（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）在兩種情況下都會出現。

<a id="telemetry-events"></a>
## 遙測事件

外掛程式將 JSON `{ "name": ..., "data": {...} }` 樣本發布到遙測槽位；主機將每個新樣本記錄為 `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`。

| Event | 發出者 | 主機動作 |
| --- | --- | --- |
| `plugin.started`（`session_id`） | `PluginRuntime.Start`，於讀取有效交接資料後 | `Move(PluginBootstrap)`；`PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`：沒有 `OMSILAUNCH_HANDOFF_NAME`，或交接資料無法讀取或無法驗證 | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`：交接資料要求 NEW_MAP/SAVED_SITUATION 以外的世界模式、非 headless 啟動、玩家車輛、日期／時間模式，或空的情境識別 | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`：處理程序內組建驗證失敗 | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | 僅記錄 |
| `headless.arm.failed` | `PluginRuntime.Start`：無法設置（arm）原生 headless 啟動 hook | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | 僅記錄 |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart`，當 `InternetTextures.Mode` 為 `Disabled` 時 | 僅記錄 |
| `world.starting`（`map`、`presented_index`、`entrypoint_identity`） | `PluginRuntime.ConsumePendingWorld`（NEW_MAP） | `Move(StartingWorld)` |
| `world.waiting-native-ready`（`native_status` 3 或 4） | NEW_MAP：OMSI 尚未就緒；在下一次 UI 計時器觸發時重試啟動 | 僅記錄 |
| `world.loaded`、`world.entrypoint.selected`（`presented_index`、`raw_index`、`presented_label`、`raw_label`） | NEW_MAP 成功路徑 | 僅記錄 |
| `world.failed`（`native_status`） | NEW_MAP：原生啟動回傳失敗 | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`、`world.situation.loaded`（`situation`） | SAVED_SITUATION 路徑 | 僅記錄（不改變狀態） |
| `world.situation.failed`（`native_status`、`situation`） | SAVED_SITUATION：原生啟動回傳失敗 | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered`（NEW_MAP：進入點選取欄位或 `entrypoint_diagnostics = unavailable`；SAVED_SITUATION：`situation`） | 世界啟動結束時 | `Move(Running)` |
| `d3d.ready`、`d3d.lost`、`d3d.resetting`、`d3d.restored`、`d3d.stopped`（`state`、`generation`、`execution_thread_id`、`live_textures`） | `CurrentRuntimeControl.PollLifecycle`，在任何 `d3d.*` 操作啟用探測之後 | 僅記錄 |
| `camera.lock.degraded`（`code`） | `CurrentRuntimeControl.PollLifecycle`，當重新套用作用中的 `camera.lock` 擲回例外時（每種不同錯誤只回報一次） | 僅記錄 |
| 無效的 JSON | 任何來源 | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

注意事項：槽位只保存一個樣本，因此在同一次 100 ms 主機輪詢內發出的多個事件可能遺失（外掛程式在 `gameplay.entered` 之後 2 s 內會抑制生命週期事件，且絕不在發布 `gameplay.entered` 的同一次觸發中發布 D3D 事件，因此不會錯過 `Running` 邊界）。`RuntimeEvents` 保留最近的 256 個事件；較舊的會被捨棄。它不是無遺失的記錄。請透過 `GetStatusAsync`、控制平面上的 `session.events`，或 CLI 上的 `events read|watch` 讀取事件。

<a id="startup-timeout"></a>
## 啟動逾時

| 項目 | 值 |
| --- | --- |
| 來源 | `LaunchSpec.Behavior.StartupTimeoutSeconds`（預設 180；1..600；CLI `/startup-timeout`，設定檔 `behavior.startup-timeout`）。 |
| 開始計時 | 監督程式工作進入其迴圈時（handle 回傳之後）。 |
| 在 `PluginBootstrap` 之前到期 | 以 `OL_E_PLUGIN_NOT_LOADED` 進入 `Failed`。 |
| 在 `PluginBootstrap` 之後、`Running` 之前到期 | 以 `OL_E_STARTUP_TIMEOUT` 進入 `Failed`。 |
| `Running` 之後 | 不套用逾時；工作階段持續到 OMSI 結束或收到停止要求為止。 |
| CLI 擁有者 | 等待 `StartupTimeoutSeconds + 5` 秒以進入 `Running`；失敗時輸出狀態，在 `OmsiLaunchW.exe` 下顯示含最後一個 `OL_E_` 診斷訊息的對話方塊（後備錯誤碼 `OL_E_SESSION_START_FAILED`），並在 `CloseAsync` 之後以 1 結束。 |

`ShutdownTimeoutSeconds` 會隨 spec 傳遞但不會被使用：沒有關閉等待。

<a id="stop-semantics"></a>
## 停止語意

每個停止要求都是相同的標準停止要求：API 的 `StopAsync(handle)`、對非終止狀態工作階段呼叫 `CloseAsync`、本機控制平面上的 `session.stop`（繫結至作用中的工作階段 id）、系統匣中的「End session」、CLI 擁有者中的 Ctrl+C 或關閉主控台，以及 `/observe-seconds` 到期。

| 步驟 | 細節 |
| --- | --- |
| 1 | 在即時工作階段上設定 `StopRequested`；呼叫端立即返回。 |
| 2 | 在 100 ms 內，監督程式離開其迴圈並呼叫 `TerminateProcess(Omsi.exe, 1)`。這是強制終止：OMSI 的關閉常式不會執行，OMSI 不會重寫 `options.cfg`，也不會出現儲存對話方塊。這是刻意的設計，使 OMSI 無法覆寫交易即將還原的檔案。 |
| 3 | 監督程式等待處理程序結束、記錄 `ProcessExited`、精確還原每個工作階段擁有的檔案（包括 OMSI 在工作階段期間寫入的 `closecheck` 標記，它會成為 `restore.session-artifact-removed` 附註）、移除日誌與備份、處置 runtime 儲存區（之後的 `ExecuteRuntimeAsync` 呼叫會擲回 `OL_E_RUNTIME_CHANNEL_CLOSED` 或 `OL_E_SESSION_NOT_RUNNING`）、釋放租用，並進入 `Completed`。 |
| 自然結束 | 若 OMSI 在 `Running` 之後自行結束（使用者關閉 OMSI），會執行相同路徑但不進行終止，工作階段正常完成。若在 `Running` 之前結束，則為 `OL_E_PROCESS_EXITED_EARLY`。 |
| 協作式關閉 | 未實作。傳送 `WM_CLOSE` 並等待 `ShutdownTimeoutSeconds` 未實作（產品決策；在執行階段收尾輪次中，OMSI 忽略了送往其主視窗的 `WM_CLOSE`，`L05b`）（[執行階段驗證狀態](../status/runtime-validation-status.md)）。 |
| 執行階段端狀態 | 透過執行階段操作變更的任何內容（時鐘、攝影機、生成的車輛、腳本變數、D3D 材質）都是處理程序內狀態，會隨處理程序消失；絕不會被還原或保存。 |

<a id="waitforasync-semantics"></a>
## `WaitForAsync` 語意

| 情況 | 結果 |
| --- | --- |
| 工作階段到達所要求的狀態 | 回傳 `State == requested` 的狀態。 |
| 工作階段先到達終止狀態 | 立即以 `Completed` 或 `Failed` 回傳（請檢查 `Diagnostics`）。 |
| 逾時到期 | 回傳目前狀態（不擲回例外）。請將 `State` 與您要求的狀態比較。 |
| 所要求的狀態已經過去（或從未設定：`ValidatingPlatform`、`Planning`、`EnteringGameplay`，實際上還有 `Snapshotting`） | 等到終止狀態或逾時為止。 |
| 呼叫端取消 | `OperationCanceledException`。 |
| 未知或已關閉的 handle | `KeyNotFoundException`。 |

輪詢間隔為 100 ms，因此觀察到的狀態轉換最多會比實際轉換晚 100 ms。

<a id="owner-lifecycle-guarantees-cli"></a>
## 擁有者生命週期保證（CLI）

`tools/OmsiLaunch.Cli/Program.cs` 中的 `OwnerSession.RunAsync` 是參考擁有者實作。

| 保證 | 細節 |
| --- | --- |
| 單一擁有者 | 啟動前，CLI 會探測控制管道；若有擁有者回應，則以 `OL_E_SESSION_ALREADY_ACTIVE` 拒絕（結束代碼 7）。租用在處理程序之間強制執行相同規則。 |
| 每條結束路徑都會到達 `CloseAsync` | 從 `StartSessionAsync` 開始，例外、Ctrl+C（`CancelKeyPress`）、關閉主控台／登出（`ProcessExit`：要求停止，擁有者最多等待 4 s 以進入 `Completed`；剩餘的任何內容會在下次啟動時由日誌復原）、系統匣停止、控制平面 `session.stop`、`/observe-seconds` 到期與自然完成，最後都會進入 `finally` 區塊，該區塊處置控制平面與系統匣並等待 `CloseAsync`。 |
| `/observe-seconds` 是上限 | 系統匣或控制平面的停止要求仍會讓工作階段提早結束。 |
| 控制平面僅在 Running 期間存在 | 具名管道端點在 `Running` 之後（以及任何 `INTERNAL` 驗證批次之後）建立，並在 `CloseAsync` 之前處置；其他時間用戶端會收到 `OL_E_NO_ACTIVE_SESSION`。 |
| 結束代碼 | 最終狀態為 `Completed` 時為 0，為 `Failed` 或未進入遊戲時為 1，所要求的復原未完成時為 8（[結束代碼](../reference/exit-codes.md)）。 |
| 診斷訊息 | 主機追蹤記錄與執行階段操作產生的檔案位於 `<root>\.omsilaunch\diagnostics` 下，系統匣記錄檔為 `tray-host.log`；不會有任何資料離開本機。 |

撰寫自有擁有者的整合者必須重現前兩項保證：每個 OMSI 安裝同一時間只能有一個 `StartSessionAsync`，且每條路徑都必須呼叫 `CloseAsync`。

<a id="failure-map"></a>
## 失敗對照

| 階段 | 失敗時的狀態 | 您會看到的診斷訊息 |
| --- | --- | --- |
| 計畫 | 無（沒有工作階段） | 由 `StartSessionAsync` 擲回的 `OL_E_PLAN_NOT_RUNNABLE`；計畫本身的 `OL_E_` 錯誤碼（[LaunchSpec 驗證](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)）。 |
| 啟動（從租用到建立處理程序） | `Failed` | 附內部錯誤碼的 `OL_E_START_SESSION`；可能還有 `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_RESTORE_FAILED`。 |
| 外掛程式啟動程序 | `Failed` | `OL_E_PLUGIN_NOT_LOADED`、`OL_E_PLUGIN_PROTOCOL_MISMATCH`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_BUILD_VALIDATION_FAILED`、`OL_E_HEADLESS_ARM_FAILED`。 |
| 世界啟動 | `Failed` | `OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_STARTUP_TIMEOUT`、`OL_E_PROCESS_EXITED_EARLY`。 |
| 執行中 | 僅在監督程式錯誤時為 `Failed` | `OL_E_PROCESS_SUPERVISION`；執行階段操作錯誤絕不會使工作階段失敗。 |
| 終止與還原 | `Failed` | `OL_E_RESTORE_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_PROCESS_CLEANUP_FAILED`。 |

每條失敗路徑仍會嘗試終止與還原；殘留的日誌會在下次啟動時，或由 `RecoverPendingAsync` / `/recover` 復原（[交易與復原](transactions-and-recovery.md)）。
