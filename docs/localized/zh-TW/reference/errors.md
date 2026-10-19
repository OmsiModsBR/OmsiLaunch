# 錯誤碼與診斷碼

<!-- l10n: source=reference/errors.md source-sha256=a3335bf22fcb58419c25d20745f040077113f1db8f973e0cb47d0469e852c5f9 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/errors.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 `PublicErrorCodes`（`src/OmsiLaunch.Api/PublicErrorCodes.cs`）中每個代碼的規範性參考：共 167 個 `OL_E_` 錯誤碼與 4 個 `OL_W_` 警告，依目錄類別分組，另外列出不屬於錯誤的資訊性診斷碼。對每個代碼，本頁說明目前程式碼在何處引發它、其意義、它以何種方式傳達給您（擲回的例外、結果欄位、診斷訊息、控制平面回覆或 CLI 封套），以及應採取的處置。各項意義取自實際引發位置；若某代碼已定義但目前沒有任何引發路徑，本頁會明確註明。

相關頁面：[公開 API](public-api.md)、[結束代碼](exit-codes.md)、[LaunchSpec](launchspec.md)、[工作階段生命週期](../concepts/session-lifecycle.md)、[交易與復原](../concepts/transactions-and-recovery.md)、[本機控制平面](local-control.md)、[執行階段控制](runtime-control.md)、[工作階段設定檔](session-profiles.md)、[永久外掛程式](../concepts/permanent-plugin.md)。

<a id="how-codes-reach-you"></a>
## 代碼如何傳達給您

| 呈現方式 | 意義 |
| --- | --- |
| 擲回 | 一個 `Message` 以該代碼開頭的例外（`InvalidOperationException`、`IOException`、`TimeoutException`、`InvalidDataException`、`FileNotFoundException`、`ArgumentException`、`SessionProfileException`）。CLI 會從訊息中擷取代碼，並將其對應到結束代碼（`CliProgram.Classify`）。 |
| 計畫診斷訊息 | `SessionPlan.Diagnostics` 中的一個 `LaunchDiagnostic`；任何 `OL_E_` 代碼都會使 `IsRunnable` 為 false（CLI：計畫顯示 `NOT RUNNABLE`，結束代碼 1）。 |
| 工作階段診斷訊息 | `SessionStatus.Diagnostics` 中的一個 `LaunchDiagnostic`；工作階段狀態為 `Failed`（CLI 結束代碼 1）。`OL_E_START_SESSION`、`OL_E_PROCESS_SUPERVISION` 與 `OL_E_RESTORE_FAILED` 會在其訊息中包裝一個內部代碼。 |
| 執行階段結果 | `RuntimeCommandResult.ErrorCode`，且 `Succeeded = false`。 |
| 執行階段細節 | `RuntimeCommandResult.ErrorCode` 為具體代碼，`Values["detail"]` 為訊息（並附 `Values["exception"]`，即 .NET 型別）。Beta 4：外掛程式端具型別的代碼會原封不動傳達到 API 與 CLI；只有沒有已登錄代碼的失敗才會變成 `OL_E_RUNTIME_OPERATION_FAILED`。 |
| 控制回覆 | 本機控制平面回覆（`LocalControlResponse`）的 `ErrorCode`。 |
| CLI 封套 | `--json` 封套中的 `error.code`，或主控台上的 `<code>: <message>`；並提供結束代碼。 |
| 遙測 | 由主機對應為工作階段診斷訊息的執行階段事件名稱。 |

## Cli

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | 有 `OperationCanceledException` 未被攔截而傳出（Ctrl+C 或已取消的用戶端等待）。 | CLI 封套，結束代碼 7 | 重試該命令。 |
| `OL_E_INTERNAL` | `CliProgram.Classify` | 有不含 `OL_E_` 代碼的例外傳出：格式錯誤的 `/spec` JSON、必要的 spec 成員為 null、非預期的錯誤。 | CLI 封套，結束代碼 10 | 閱讀訊息與 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`；修正輸入；若無法解釋請回報。 |
| `OL_E_TIMEOUT` | `CliProgram.Classify`；`LocalControlPlane.TryRequestAsync` | 有不含代碼的 `TimeoutException` 傳出；或本機控制用戶端已連線到一個未在逾時時間內回覆的擁有者（擁有者存在，因此不會回報為 `OL_E_NO_ACTIVE_SESSION`）。（信箱逾時則改為帶有 `OL_E_RUNTIME_REQUEST_TIMEOUT`。） | CLI 封套、控制回覆；`Classify` 產生結束代碼 5，控制回覆為結束代碼 7 | 重試；確認 OMSI 與擁有者都有回應。 |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync`（`/silent`） | `OmsiLaunchW.exe` 不在 `OmsiLaunch.exe` 旁邊。 | CLI 封套，結束代碼 7 | 重新安裝套件。 |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync`（`/silent`） | 對 `OmsiLaunchW.exe` 執行 `Process.Start` 未回傳任何處理程序。 | CLI 封套，結束代碼 7 | 檢查套件檔案與權限；不加 `/silent` 執行以查看失敗原因。 |

## Compatibility

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry`（於 `plugin.build.invalid` 時） | 啟動功能已解析，但外掛程式在準備無介面啟動時，即時的啟動表單或其某個 hook 位置不在預期狀態（例如已有另一個外掛程式修補過它）。 | 工作階段診斷訊息（`Failed`） | 移除衝突的外掛程式；請參閱[相容性](compatibility.md)。 |
| `OL_E_CAPABILITY_UNRESOLVED` | 外掛程式（啟動時的 `plugin.capability.unresolved`；執行階段操作） | 要求所需的原生功能無法在此 OMSI 組建上解析並驗證，因此無法使用。啟動時，工作階段會在任何 hook 之前失敗並還原檔案；執行階段操作會在不觸及 OMSI 記憶體的情況下被拒絕。`Data["capability"]`（執行階段）或遙測的 `capabilities` 值會指出它。 | 工作階段診斷訊息（`Failed`）；執行階段結果 | 請參閱[相容性](compatibility.md)：該功能在此組建上無法使用。 |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner`（`omsi.omsi2` 無法使用） | 該安裝不是 OMSI 2 安裝：`Omsi.exe` 不存在、不是 32 位元 x86 PE 映像、沒有 OMSI 2（2.x）版本，或根目錄沒有 `maps\` 或 `Vehicles\` 目錄。執行檔的雜湊不是判斷條件。 | 計畫診斷訊息 | 將 OmsiLaunch 指向 OMSI 2 安裝根目錄。 |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner`（`runtime.current-windows-x64` 無法使用）；`CurrentWindowsX64Platform.ValidateCurrent` 也會擲回，但服務不會呼叫該方法 | 並非 Windows 10 或更新版本，或作業系統或主機處理程序不是 x64。 | 計畫診斷訊息（擲回時 CLI 結束代碼 3） | 在 64 位元 Windows 10 或更新版本上執行。 |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | 僅 `CurrentWindowsX64Platform.ValidateCurrent` | 作業系統或主機架構不是 x64。服務不會呼叫 `ValidateCurrent`；目前沒有引發路徑。 | 僅由該方法擲回（`PlatformNotSupportedException`） | 請參閱原始碼 `src/OmsiLaunch.Process/RuntimePlatform.cs`。 |

## Content

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | 驗證：NEW_MAP 沒有 `EntrypointIdentity`，且 `PresentedEntrypointIndex` 未設定或為負值。規劃器：該地圖沒有具有所要求識別或精確（區分大小寫）標籤的進入點。 | 計畫診斷訊息 | 設定 `PresentedEntrypointIndex`（`/entrypoint-index:<n>`），或使用 `/list:entrypoints /map:<id>` 列出的識別。 |
| `OL_E_WEATHER_INVALID` | `SessionPlanner`（`StartWeatherResolver`） | 起始天氣無法套用：預設組合不是安裝的 `weather` 資料夾中的檔案，或 ICAO 不是四個字母或數字。 | 計畫診斷訊息 | 使用已安裝的預設組合或有效的測站代碼。 |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | 所要求的進入點與該地圖較早的一筆記錄共用標籤；OMSI 只呈現每個標籤的第一筆記錄，因此無法在啟動時選取它。 | 計畫診斷訊息 | 選擇具有該標籤的第一筆記錄，或另一個進入點。 |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner`（`world.presented-entrypoint` 無法使用） | NEW_MAP 地圖已解析，但沒有呈現索引也沒有識別。一定與 `OL_E_ENTRYPOINT_NOT_FOUND` 同時出現。 | 計畫診斷訊息 | 同上。 |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` 不是已安裝的 `Vehicles\...\*.hof`。 | 計畫診斷訊息 | 使用 `/list:hofs` 列出的識別。（無論如何，玩家車輛欄位在此組建上都不可執行。） |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | [HOF 管理](hof-management.md)的來源（集合、目錄、車輛或檔案）不存在、沒有 `.hof`，或缺少 `Files` 中指名的檔案。 | 計畫診斷訊息；啟動失敗 | 修正來源或檔案清單。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | HOF 目標車輛檔案或資料夾未安裝。 | 計畫診斷訊息；啟動失敗 | 使用 `/list:vehicles` 的車輛識別，或 `Vehicles\` 之下已存在的資料夾。 |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | HOF 路徑離開了安裝、目標不在 `Vehicles\` 之下、集合名稱不是單純的名稱，或經過了 junction/symlink。 | 計畫診斷訊息；啟動失敗 | 使用相對於安裝、不含 `..` 且不含連結的路徑。 |
| `OL_E_HOF_CONFLICT` | `HofPlanner`、`OmsiLaunchService` | 兩個 HOF 操作將不同的位元組寫入同一個檔案，或鏡像移除了另一個操作寫入的檔案。 | 計畫診斷訊息；啟動失敗 | 重新排序、拆分或移除衝突的操作。 |
| `OL_E_HOF_INVALID` | `HofPlanner` | HOF 操作沒有目標或來源、檔案來源不是 `.hof`、目標檔案不是 `.bus`/`.ovh`，或超出大小／數量限制（每個檔案 8 MiB、每個操作 256 個檔案、每個工作階段 256 MiB）。 | 計畫診斷訊息；啟動失敗 | 修正該操作。 |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | 驗證：NEW_MAP 的 `MapIdentity` 未設定，或格式不是 `maps\<dir>\global.cfg`。規劃器：該地圖未安裝。 | 計畫診斷訊息 | 使用 `/list:maps` 列出的識別。 |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | 有不含代碼的 `FileNotFoundException`／`DirectoryNotFoundException` 傳出，例如 `/list:repaints` 搭配未知的 `/vehicle-scope`，或 `/list:entrypoints` 搭配未知的 `/map`。 | CLI 封套，結束代碼 6 | 修正識別。 |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` 不是該車型的 `.cti` 項目（僅在設定 `Model` 時檢查）。 | 計畫診斷訊息 | 使用 `/list:repaints /vehicle-scope:<bus>` 列出的識別。 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | 所選 `.osn` 內參照的地圖未安裝。 | 計畫診斷訊息 | 安裝該地圖或選擇其他情境。 |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | SAVED_SITUATION 沒有 `SituationIdentity`，或該 `.osn` 未安裝。 | 計畫診斷訊息 | 使用 `/list:situations` 列出的識別。 |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` 不是已安裝的 `Vehicles\...\*.bus`。 | 計畫診斷訊息 | 使用 `/list:vehicles` 列出的識別。 |

## Installation

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`；`OmsiLaunchService.RecoverPendingAsync`；`FileConfigurationTransaction.RestorePendingAsync` | 安裝租用（`Local\OmsiLaunch.Installation.<hash>`）正由此登入工作階段中的另一個擁有者持有，或日誌記錄的 OMSI 處理程序（PID + 建立時間 + 執行檔路徑；若日誌已超過 `HandoffCreated` 且沒有 PID，則為根目錄中的任何 `Omsi.exe`）仍在執行。 | 啟動：透過 `OL_E_START_SESSION` 的工作階段診斷訊息。復原：擲回（`InvalidOperationException`／`IOException`）。CLI 結束代碼 7。 | 停止另一個擁有者（`session stop`）或等待 OMSI 結束，然後重試或執行 `/recover`。 |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` 為空。 | 計畫診斷訊息 | 傳入安裝目錄。 |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner`（`transaction.exact-restore` 無法使用）；另有 `ValidateCurrent` | 根目錄不存在、具有唯讀屬性，或沒有 `plugins\` 目錄。 | 計畫診斷訊息 | 指向實際存在且可寫入的 OMSI 安裝。 |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled`（規劃與啟動時） | 已安裝的 `plugins\OmsiLaunch.*` 檔案與 `release-manifest.json` 中的 hash 不同（或在沒有資訊清單時與參考封閉集不同）。 | 計畫診斷訊息（計畫不可執行，CLI 結束代碼 1）；僅在規劃與啟動之間檔案變更時，才會是透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 重新安裝 OmsiLaunch 套件，使 `plugins\` 與資訊清單一致。 |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled`（規劃與啟動時） | 資訊清單缺少某個必要外掛程式檔案的項目。 | 計畫診斷訊息；在與上述相同的競爭情況下，為透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 重新安裝套件。 |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled`（規劃與啟動時） | OMSI 安裝中缺少必要的 `plugins\OmsiLaunch.*` 檔案，或 `release-manifest.json` 中列出的某個 `plugins/` 檔案未安裝。 | 計畫診斷訊息；在與上述相同的競爭情況下，為透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 安裝永久外掛程式封閉集（[安裝](../getting-started/installation.md)）。 |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | 僅 `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` 為 false。服務不會呼叫；目前沒有引發路徑。 | 僅由該方法擲回 | 請參閱原始碼。 |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes`／`ParsePluginHashes` | `release-manifest.json` 為空、不是 JSON、沒有 `files` 陣列，或有項目缺少 `path`／`sha256`、hash 不是 64 位十六進位數字、路徑為根路徑、包含 `:`、含有空白、`.` 或 `..` 區段，或同一路徑列出兩次（比較時不區分大小寫，`/` 與 `\` 視為相同）。接受 UTF-8 BOM。 | 規劃：包裝在 `OL_E_RUNTIME_ARTIFACT_MISSING` 中；啟動：透過 `OL_E_START_SESSION` | 重新安裝套件。 |

## InvalidArgument

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`；`CliInput.Parse`／`Classify` | 驗證：模式不是 `Explicit` 時卻設定了 `Date.Value`／`Time.Value`。CLI：未知的旗標、缺少值、錯誤的整數或範圍、`/saved` 與 `/map`／`/entrypoint` 併用、未知的命令路由，以及任何不含代碼的 `ArgumentException`／`FormatException`。 | 計畫診斷訊息；CLI 封套，結束代碼 2 | 修正參數。 |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch`（啟動時） | 語意設定值超出範圍、不是布林值、不在允許的集合中，或格式錯誤（`graphics.particles` 需要四個欄位）。規劃時不會驗證值。 | 透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 使用[設定表](launchspec.md#environmentspec)中的值。 |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`；`CliInput.BuildSpecAsync`；`BuildTransactionalOverlays` | 該鍵存在但不可寫入（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）。 | 計畫診斷訊息；CLI 結束代碼 2 | 移除該鍵。 |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`；`CliInput.BuildSpecAsync`；`BuildTransactionalOverlays` | 該鍵不在 `ConfigurationCatalog` 中。 | 計畫診斷訊息；CLI 結束代碼 2 | 使用目錄中的鍵。 |

## LaunchSpec

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | 根節點不是 JSON 物件，或還原序列化未產生任何記錄。 | 擲回（`InvalidDataException`），CLI 結束代碼 2 | 修正該檔案（[LaunchSpec](launchspec.md)）。 |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | `/spec` 檔案不存在。 | 擲回（`FileNotFoundException`），CLI 結束代碼 6 | 檢查路徑。 |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | 檔案超過 1 MiB。 | 擲回（`InvalidDataException`），CLI 結束代碼 2 | 縮減檔案。 |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | 某個成員不是該位置記錄的公開屬性；訊息為 `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`。 | 擲回（`InvalidDataException`），CLI 結束代碼 2 | 移除或重新命名該成員。 |

## LocalControl

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | 擁有者處理常式（`OwnerSession`） | 命令不是 `session.status`、`session.events`、`session.stop`，也不是帶有 `operation` 參數的 `runtime.execute`。 | 控制回覆；CLI 結束代碼 7 | 使用受支援的命令。 |
| `OL_E_CONTROL_FAILED` | CLI 用戶端（`ReportForwarded`、`CliEventWatch`） | 擁有者回覆 `Ok = false` 但未附錯誤碼。 | CLI 封套，結束代碼 7 | 閱讀訊息；檢查擁有者的主控台／診斷訊息。 |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | 擁有者的處理常式擲回了訊息中不含 `OL_E_` 代碼的例外（例如工作階段已關閉），或處理常式的回覆無法序列化。 | 控制回覆 | 讀取 `session status`；若擁有者已不存在則重新啟動。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane`（兩端） | 長度前綴為負值或超過 64 KiB（包括過大的請求訊框）、空訊框、JSON `null`、沒有 `Command` 的請求，或無法解碼的 JSON。 | 控制回覆／CLI 封套 | 使用文件所述的協定（[本機控制平面](local-control.md)）。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync`（用戶端） | 用戶端本身序列化後的請求超過 64 KiB。會回報給呼叫端；不會送出任何內容。 | 控制回覆／CLI 封套 | 縮減請求。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync`（擁有者） | 擁有者的回覆無法放入 64 KiB 訊框。擁有者會以此具型別錯誤回應，而不是丟棄回覆。`session.status` 與 `session.events` 永遠不會觸及此情況：其事件歷程會從最舊的開始修剪以符合大小。 | 控制回覆／CLI 封套 | 重試；對於事件，請更頻繁地讀取。 |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`、`TryRequestBoundAsync` | 請求的 `ProtocolVersion` 不是 `0.1`；擁有者的回覆無法解碼或為空；擁有者未回覆就關閉連線，或連線建立後中斷；擁有者未回報 `SessionId`。 | 控制回覆／CLI 封套 | 使用相符的用戶端與擁有者版本；讀取 `session status`。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | 擁有者處理常式 | `session.stop` 或 `runtime.execute` 沒有與作用中工作階段相同的 `session_id`。 | 控制回覆；CLI 結束代碼 7 | 先讀取 `session.status`，再繫結請求（CLI 會自動執行）。 |

## Other

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | 傳入的計畫 `IsRunnable = false`，或啟動時重新規劃的結果不可執行（`Omsi.exe` 已變更、內容已移除、外掛程式封閉集缺失）；訊息會列出目前的 `OL_E_` 代碼。 | 擲回（`InvalidOperationException`）；CLI 結束代碼 1 | 重新規劃並修正列出的診斷訊息。 |

## Presentation

全部由 `SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）引發。在規劃時，它們會包裝在 `OL_E_SESSION_PRESENTATION_INVALID` 中（訊息帶有該代碼）；在啟動時則透過 `OL_E_START_SESSION` 呈現。

| 代碼 | 意義／典型原因 | 處置 |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | `.itx` 檔案為空、非空白行數為奇數，或某個 URL 行不是絕對的 `http`／`https` URL。 | 使用 URL／目標成對的行。 |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath`（相對於處理程序工作目錄解析）不存在。以 `FileNotFoundException` 擲回。 | 傳入存在的 `.itx` 路徑。 |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` 為 `Override` 但沒有 `OverrideProfilePath`。擲回時 CLI 結束代碼 2。 | 提供 `/internet-textures-profile:<file.itx>`。 |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | 某個目標行為根路徑、包含 `..`、以 `\` 開頭、解析後位於安裝之外、缺少 `Texture\` 元件，或經過 junction／符號連結。 | 使用 `Texture\...` 相對目標。 |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` 不存在。 | 修正該目錄。 |
| `OL_E_SPLASH_ASSET_MISSING` | 資產目錄中缺少 `ENG.bmp` 或 `<LANG>.bmp`，或在植入 `.omsilaunch\assets\splash` 時缺少套件內的 `assets\splash\<LANG>.bmp`。 | 提供 BMP 檔案／重新安裝套件。 |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | 啟動畫面 BMP 不是 640×480 24 位元的 `BM` 點陣圖。 | 轉換影像。 |

## Process

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService`（啟動失敗與監督程式錯誤路徑） | 在錯誤清理期間終止或等待 OMSI 時擲回例外；後面接著內部訊息。 | 工作階段診斷訊息（加入 `Failed` 工作階段） | 確認沒有殘留的 `Omsi.exe`，若有日誌待處理則執行 `/recover`。 |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | 在 `CreateProcessW` 之後立即呼叫 `GetProcessTimes` 失敗（`Win32=<code>`）；該處理程序會被終止。 | 透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 重試；檢查防毒軟體／權限。 |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI 在 `gameplay.entered` 之前結束（當機、關閉了 OMSI 錯誤對話方塊、關閉了視窗）。 | 工作階段診斷訊息（`Failed`）；會執行還原 | 檢查 OMSI 本身的記錄檔與 `logfile.txt`；查看 `RuntimeEvents` 中最後一個外掛程式事件。 |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` 失敗（訊息中有 `Win32=<code>`）。 | 透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 解決該 Win32 錯誤（檔案遺失、存取遭拒、原則）。 |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | 監督程式迴圈擲回例外（遙測讀取、處理程序等待／終止、日誌寫入）；OMSI 會被終止並嘗試還原。 | 工作階段診斷訊息（`Failed`） | 閱讀內部訊息與主機記錄檔。 |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` 失敗（`Win32=<code>`）。 | 位於 `OL_E_PROCESS_SUPERVISION`／`OL_E_PROCESS_CLEANUP_FAILED` 訊息內 | 手動終止 OMSI，然後執行 `/recover`。 |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | 對處理程序 handle 呼叫 `WaitForSingleObject` 失敗。 | 位於 `OL_E_PROCESS_SUPERVISION`／`OL_E_PROCESS_CLEANUP_FAILED` 訊息內 | 同上。 |

## Runtime

「執行階段細節」表示具體代碼位於 `ErrorCode`，訊息位於 `Values["detail"]`，例外型別位於 `Values["exception"]`。

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` 帶有 `preset`，但 `family` 為 2（外部）或 3（地圖）；預設組合僅適用於駕駛員（0）與乘客（1）。 | 執行階段細節 | 省略 `preset`，或使用 family 0/1。 |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`／`Time` 模式為 `Explicit` 但沒有值，或各部分超出範圍。此名稱是沿用歷史；它其實是規劃時的驗證錯誤。 | 計畫診斷訊息 | 修正該值（並請注意，明確指定的日期／時間在此組建上不可執行）。 |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`：`.bus` 路徑在 OMSI 工作目錄下不存在（在原生呼叫前檢查，使 OMSI 無法以備援車輛替代）。 | 執行階段細節 | 使用 `vehicles list`／`/list:vehicles` 列出的識別。 |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | 同上 | 原生 MakeVehicle 使道路車輛集合的變動超過一個物件。 | 執行階段細節（`native_status`，計數在訊息中） | 回報；已建立的物件會保留到工作階段結束。 |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | 同上 | 集合沒有變化；OMSI 默默拒絕了該車輛。 | 執行階段細節 | 檢查 `.bus` 檔案；嘗試其他車型。 |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | 同上 | 任何其他非零的原生狀態。 | 執行階段細節 | 連同訊息中的計數一併回報。 |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | 已剖析的 PlaceRandomBus 呼叫回傳了失敗狀態。 | 執行階段細節 | 待遊戲進行穩定後重試；回報。 |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`；外掛程式端檢查（`time.set` 沒有 `hour`／`minute`／`second`；`camera.set` 沒有 `family`／`field_of_view`；`camera.lock` 沒有可剖析的 `family`；車輛／曲線操作） | 缺少必要參數或參數為空白。 | 執行階段結果（登錄；CLI 結束代碼 2）或執行階段細節（外掛程式） | 提供該參數（[執行階段控制](runtime-control.md)）。 |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | 無法載入 `OmsiLaunchRuntimePaths` 中指定的外掛程式封閉集參考目錄／檔案或原生橋接層（可能包裝 `OL_E_RELEASE_MANIFEST_INVALID`）。 | 計畫診斷訊息 | 從完整無損的套件執行。 |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch`（`/runtime-write-batch`，INTERNAL 測試工具） | 基準 `time.read`／`camera.read` 失敗，因此略過寫入測試。 | 僅批次產出物 | 不面向使用者。 |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` 為空、長度超過 240 個字元、包含 NUL 或 `..`、不以 `Vehicles\` 開頭，或不以 `.bus` 結尾。 | 執行階段細節 | 傳入 `Vehicles\<dir>\<file>.bus`。 |
| `OL_E_RUNTIME_CHANNEL_BUSY` | 無（為相容性而保留） | 已不再發出。較早的組建在已取消的請求留在槽位中時會引發此代碼；如今請求的每個終止路徑都會重設槽位，而在新請求開始時發現的殘留請求或回應也會被清除。 | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | 由於工作階段即將結束，信箱已被處置。 | 擲回（`InvalidOperationException`） | 無；工作階段已結束。 |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | 信箱槽位保存的狀態值不是閒置、已請求或已回應（毀損）。槽位會被重設並引發此錯誤；下一個請求可正常運作。 | 擲回（`InvalidDataException`） | 重試；若持續發生請回報。 |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | 車輛的常數區塊指標為 null。 | 執行階段細節 | 該車輛沒有常數；無需處理。 |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在車輛的常數表中。 | 執行階段細節 | 先列出常數。 |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | 由 spawn 建立的物件，其 VMT 位於 OMSI 映像範圍之外。 | 執行階段細節 | 回報。 |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | 同上 | 建立的物件不在道路車輛集合中。 | 執行階段細節 | 回報。 |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | 兩個相鄰的曲線點具有相同的 X。 | 執行階段細節 | 車輛曲線的內容問題。 |
| `OL_E_RUNTIME_CURVE_EMPTY` | 同上 | 曲線沒有任何點。 | 執行階段細節 | 同上。 |
| `OL_E_RUNTIME_CURVE_INVALID` | 同上 | 曲線沒有任何區段包含 `x`。 | 執行階段細節 | 在曲線的定義域內求值。 |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | 同上 | `name` 未知，或其函式指標為 null。 | 執行階段細節 | 先列出曲線。 |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | 車輛定義指標為 null。 | 執行階段細節 | Handle 指向沒有定義資料的車輛。 |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync`（擁有者模式） | 執行檔旁缺少 `plugins\OmsiLaunch.Plugin.opl` 或 `plugins\OmsiLaunch.Native.x86.dll`。 | CLI 封套，結束代碼 7 | 重新安裝套件。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `road-vehicle.read`、`human.read`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list`、`vehicle.curves.list` 缺少 `handle` 或為空白（登錄通常會先以 `OL_E_RUNTIME_ARGUMENT_REQUIRED` 拒絕這些請求）。 | 執行階段細節 | 提供該 handle。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | Handle 未知、物件已離開集合、位址世代已推進，或因位址被重複使用而使物件指紋（VMT + 定義／車型識別）改變。殘餘盲點：在兩次清單讀取之間，於同一位址重新建立了相同類別與車型的物件。 | 執行階段細節 | 再次執行 `road-vehicles.list`／`humans.list` 並使用新的 handle。 |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`；`CurrentRuntimeCommandMailbox.TryDispatch`；`D3DRuntimeApi` 備援 | 通用的外掛程式端失敗包裝；`Values["detail"]` 保存訊息（通常是更具體的代碼），`Values["exception"]` 保存例外型別。在信箱分派器內從操作中傳出的例外，也會以此代碼（不含值）回應，而不是讓請求無人回應。 | 執行階段結果 | 閱讀 `detail`。 |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | 登錄允許的某項操作在外掛程式中沒有實作（登錄／外掛程式版本不一致）。 | 執行階段細節 | 重新安裝一致的套件。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | 該操作不在 `PublicRuntimeOperationIds` 中，包括所有 `internal.*` 操作。在查詢工作階段之前檢查。 | 執行階段結果；控制回覆；CLI 結束代碼 2 | 使用公開的 operation id。 |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` 帶有 `preset`，但沒有玩家車輛（無頭工作階段沒有玩家車輛）。重新套用失敗時，也會作為 `camera.lock.degraded` 事件的 `code` 回報。 | 執行階段細節／執行階段事件 | 不使用預設組合進行鎖定，或使用有玩家車輛的工作階段。 |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | 成功的 D3D 結果沒有值，或裝置狀態字串未知。 | 擲回（`OmsiRuntimeException`） | 使用相符的主機與外掛程式版本。 |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | 槽位保存了一個與新請求具有相同 request id 的過時回應。該過時回應會在引發錯誤前被清除。 | 擲回（`InvalidOperationException`） | 使用嚴格遞增的 request id。 |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | 在 `timeout` 內沒有回應；槽位會被重設，延遲到達的回應會被捨棄。 | 擲回（`TimeoutException`）；CLI 結束代碼 5 | 以較長的逾時重試；確認 OMSI 沒有被阻塞（強制回應對話方塊、載入中）。 |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | 回應封套毀損、長度錯誤（負值、零或大於槽位）、屬於其他工作階段的 session id，或不同的 request id。槽位會在引發錯誤前重設，因此下一個請求可正常運作。 | 擲回（`InvalidDataException`） | 重試；若持續發生請回報。 |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage`（外掛程式） | 清單操作的 `cursor` 格式錯誤，或屬於另一種清單。 | 執行階段細節 | 傳入同一操作上一頁的 `next_cursor`，或省略它。 |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | 序列化後的結果超過 64 KiB 信箱。分頁清單結果則會改為縮短以符合大小，並以 `next_cursor` 繼續；沒有任何清單結果會到達此代碼。 | 執行階段結果 | 使用範圍較窄的操作或較小的 `limit`。 |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | 車輛的腳本定義或狀態指標為 null。 | 執行階段細節 | 該車輛沒有腳本物件。 |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`；`CurrentRuntimeCommandStore`；外掛程式信箱 | `RuntimeCommand.SessionId` 與 handle 的 session id 不同（由主機擲回），或請求送達了繫結於另一個工作階段的外掛程式（由外掛程式以具型別結果回傳）。 | 擲回（`InvalidOperationException`）／執行階段結果 | 以 `session.SessionId` 建構命令。 |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime`（`world.failed` 事件的 `code`） | 世界啟動後無法套用起始天氣：OMSI 沒有為 `/weather-real` 設定 ICAO 測站，或其天氣物件無法使用。工作階段失敗。 | 工作階段失敗 | 在 OMSI 中設定測站，或使用 `/weather-icao`。 |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | `weather.set` 的某個索引鍵指名了 OMSI 在每個天氣更新週期都會推導的值（濕度、光線係數、雲透明度、降水旗標、潮濕地面）；不會寫入任何內容。 | 執行階段結果 | 設定 `detail` 中指名的來源索引鍵（例如以 `dew_point` 取代 `relative_humidity`）。 |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | 未知的 `weather.set` 索引鍵；不會寫入任何內容。 | 執行階段結果 | 使用[功能](capabilities.md#weather)中列出的索引鍵。 |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在字串變數表中。 | 執行階段細節 | 先列出字串變數。 |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | 天氣布林值不是 `true`／`false`／`1`／`0`。目前無法觸及（見上文）。 | 執行階段細節（已定義） | 請參閱原始碼。 |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`、`OmsiCameraWriter`、`OmsiCameraLockWriter`、`OmsiRuntimeReaders`、`OmsiWeatherWriter` | `time.set`：`hour` 0..23、`minute` 0..59、`second` 0..59.999；`camera.set`：`family` 0..3、`field_of_view` 10..170；`camera.lock`：`preset` 不是整數、`family` 0..3、`preset` 0..255；`road-vehicles.place-random`：`ai_type` 0..255、`group`／`type`／`tour`／`line` 0..65535（`type` 可為 -1）、`scheduled` 0..1；`vehicle.variable.set`：`value` 不是有限值；`vehicle.curve.evaluate`：`x` 不是有限值。 | 執行階段細節 | 使用範圍內的值。 |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在數值變數表中。 | 執行階段細節 | 先列出變數。 |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | 變數槽位或值位址為 null。 | 執行階段細節 | 此車輛尚未具體化該變數。 |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | 時鐘純量已寫入，但已剖析的原生 SetTime 呼叫回傳失敗。 | 執行階段細節 | 重試；以 `time.read` 讀回確認。 |

## RuntimeD3D

全部由 `CurrentRuntimeControl` 引發（原生狀態的 `ThrowD3D` 對應；`detail` 帶有操作與 HRESULT，`native_status` 帶有數值狀態）。呈現方式：代碼位於 `ErrorCode` 的執行階段結果；`D3DRuntimeApi` 會將其重新擲回為 `OmsiRuntimeException`。

| 代碼 | 原生狀態／原因 | 處置 |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8：Direct3D 裝置已遺失。 | 等待 `d3d.restored`；重新建立材質（世代已變更）。 |
| `OL_E_D3D_INVALID_ARGUMENT` | 14：`width`、`height`、`level`、`x`、`y`、`format` 或 `handle` 缺少或無效（範圍：width/height 1..4096、levels 0..16、level 0..15、x/y 0..4095）。 | 修正參數。 |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` 不是有效的 Base64，或超過 48 KiB。 | 傳送較小的矩形。 |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6，或未知的 `format` 名稱（有效值：`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）。 | 使用列出的格式。 |
| `OL_E_D3D_NATIVE_CALL_FAILED` | 任何其他狀態；HRESULT 位於 `detail` 中。 | 連同 HRESULT 一併回報。 |
| `OL_E_D3D_NOT_READY` | 7：裝置尚未就緒（第一個畫面之前或停止期間）。 | 在 `d3d.ready` 之後重試。 |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9：正在進行裝置重設。 | 在 `d3d.restored` 之後重試。 |
| `OL_E_D3D_RESOURCE_RELEASED` | 13：材質 handle 已被釋放。 | 不要重複使用已釋放的 handle。 |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12：該 handle 屬於先前的裝置世代；或 handle 字串不是 `d3dtex-<session>-<hex>`／為零。 | 重新建立材質。 |

## Session

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`；`ApplyTelemetry`（於 `plugin.request.unsupported` 時） | 規劃：要求了 `LastMapState`、`EntrypointIdentity`、日期／時間／年份模式、天氣模式、玩家車輛欄位或輸入文件（`Requested capability unavailable: <name>`）。遙測：外掛程式拒絕了交接（對可執行的計畫不會發生）。 | 計畫診斷訊息；工作階段診斷訊息 | 移除不受支援的要求（[已知限制](known-limitations.md)）。 |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry`（於 `headless.arm.failed` 時） | 外掛程式無法在 OMSI 中佈署一次性的無頭啟動掛鉤。 | 工作階段診斷訊息（`Failed`） | 驗證組建；回報。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI 用戶端（`ReportForwarded`、`CliEventWatch`） | 該安裝的控制管道上沒有擁有者回應（沒有工作階段，或擁有者仍在啟動／驗證中）。 | CLI 封套，結束代碼 4 | 啟動工作階段，或等到其狀態為 `Running`。 |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | 在 `plugin.started` 之前已超過 `StartupTimeoutSeconds`（OMSI 未載入 `plugins\OmsiLaunch.Plugin.opl`，或卡在外掛程式初始化之前）。 | 工作階段診斷訊息（`Failed`） | 檢查外掛程式封閉集、`plugins\OmsiLaunch.Plugin.opl` 與 OMSI 的 `logfile.txt`。 |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry`（於 `plugin.handoff.invalid` 或無法剖析的遙測 JSON 時） | 外掛程式無法讀取／驗證啟動交接（版本 3 至 6、SHA-256），或送出了無效的遙測。 | 工作階段診斷訊息（`Failed`） | 使用相符的主機與外掛程式版本（重新安裝套件）。 |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | 此安裝已有擁有者回應 `session.status`。 | CLI 封套，結束代碼 7 | 使用用戶端命令（`session status`、`session stop`、執行階段命令）。 |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | 工作階段狀態不是 `Running`。 | 擲回（`InvalidOperationException`） | 先呼叫 `WaitForAsync(session, SessionState.Running, ...)`。 |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | 無法建立啟動畫面／ITX 計畫；訊息帶有呈現相關代碼。 | 計畫診斷訊息 | 請參閱 [Presentation](#presentation)。 |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure`（OmsiLaunchW 對話方塊） | 當啟動計畫不可執行或工作階段未進入遊戲，且不存在任何 `OL_E_` 診斷訊息時所顯示的備援代碼。 | 僅訊息方塊 | 閱讀 `.omsilaunch\diagnostics`。 |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry`（於 `world.situation.failed` 時） | 原生的已儲存情境啟動回傳失敗（事件中有 `native_status`）。 | 工作階段診斷訊息（`Failed`） | 檢查 `.osn` 及其地圖。 |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | 外掛程式已啟動，但未在 `StartupTimeoutSeconds` 內達到 `Running`。 | 工作階段診斷訊息（`Failed`） | 對大型地圖增加 `/startup-timeout`；查看 `RuntimeEvents` 中最後一個世界事件。 |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | 啟動路徑中的任何例外；訊息即為內部訊息（通常以內部代碼開頭）。 | 工作階段診斷訊息（`Failed`） | 依內部代碼處置。 |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry`（於 `world.failed` 時） | 原生的 NEW_MAP 啟動回傳失敗（事件中有 `native_status`）。 | 工作階段診斷訊息（`Failed`） | 檢查地圖、進入點索引與 OMSI 的記錄檔。 |

## SessionProfile

全部由 `SessionProfileCompiler`（`src/OmsiLaunch.Core/SessionProfiles.cs`）或 `CliInput` 引發，以 `SessionProfileException`（帶有 `Code` 的 `IOException`）擲回，CLI 結束代碼 2。請參閱[工作階段設定檔](session-profiles.md)。

| 代碼 | 意義／典型原因 | 處置 |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | 預設組合的啟動畫面 `assets` 目錄或 Internet Textures `profile` 檔案不存在於套件內。 | 加入該資產。 |
| `OL_E_SESSION_PROFILE_INVALID` | 結構或限制違規：超過 256 KiB、根對應不是恰好一個、YAML 錨點、未知的鍵、缺少必要的鍵、需要純量處卻不是純量、`id` 與目錄名稱不同、預設組合不在 1..99 範圍內或 `index` 重複、非正值的逾時、不受支援的天氣／啟動畫面／Internet Textures 模式、日期／時間不是 `explicit`、無效的 YAML、數字／日期剖析錯誤。 | 依訊息修正 YAML。 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` 非空，且不包含所選地圖（NEW_MAP）或所選情境的地圖（SAVED_SITUATION）。 | 選擇相容的世界。 |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 不存在。 | 檢查 id。 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 明確的 CLI 參數指向了所選設定檔／預設組合所擁有的欄位。 | 移除該旗標或選擇其他預設組合。 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | id 包含 `\`、`/`、`:` 或 `..`；或資產路徑為根路徑、跳脫出套件，或經過 junction／符號連結。 | 讓路徑保持在套件內。 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | 缺少 `/predefined-profile-index`、超出 1..99，或該設定檔未宣告此索引。 | 使用已宣告的預設組合索引。 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` 不是 `omsilaunch.session-profile/v1`。 | 使用受支援的 schema。 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | 某個預設組合的設定鍵為已知但不可寫入。 | 移除該鍵。 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | 某個預設組合的設定鍵不在目錄中。 | 使用目錄中的鍵。 |

## Transaction

請參閱[交易與復原](../concepts/transactions-and-recovery.md)。

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | 執行 `File.Delete` 之後，過時的 `closecheck` 仍然存在。 | 透過 `OL_E_START_SESSION` 的工作階段診斷訊息 | 手動移除 `<root>\closecheck`（權限問題）。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | 某個在工作階段之前不存在的路徑，現在包含的內容與工作階段所套用的不同；該路徑不會被移除，日誌也會保留。 | 擲回（`IOException`）；位於 `OL_E_RESTORE_FAILED`／`OL_E_START_SESSION` 內；CLI 結束代碼 8 | 檢查該檔案；將其移除或移走，然後執行 `/recover`。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync`（指紋機制之前的日誌） | 對於原本不存在且非刪除類的路徑，日誌中沒有已套用內容的指紋，因此無法證明擁有權。在工作階段啟動時，復原會延後，並以此工作階段規劃的位元組重試；透過 `RecoverPendingAsync` 時則會擲回。 | 擲回（`IOException`）；CLI 結束代碼 8 | 以相同的 spec 啟動工作階段（以提供位元組），或檢查並移除該檔案，然後執行 `/recover`。 |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | 某個備份的 SHA-256 與日誌中記錄的快照不符；不會寫入任何內容。 | 擲回；位於 `OL_E_RESTORE_FAILED` 內；CLI 結束代碼 8 | 從您自己的備份還原該檔案；只有在確定無誤時才刪除日誌。 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | 記憶體中存在快照，但 `journal.json` 已不見（在工作階段期間被刪除）。 | 擲回；位於 `OL_E_RESTORE_FAILED` 內 | 手動確認工作階段檔案。 |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | 刪除後 `journal.json` 仍然存在（還原本身已成功並經過驗證）。 | 擲回；位於 `OL_E_RESTORE_FAILED` 內；CLI 結束代碼 8 | 刪除 `<root>\.omsilaunch\journal.json`（權限問題），或再次執行 `/recover`（具冪等性）。 |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService`（啟動失敗／監督程式） | 無法確認 OMSI 已結束，因此在它可能仍使用這些檔案時不會取代檔案；日誌會保留。 | 工作階段診斷訊息（`Failed`） | 在 `Omsi.exe` 結束後執行 `/recover`（或由下一次啟動自動復原）。 |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService`（啟動失敗／監督程式） | `RestoreAsync` 擲回例外；訊息帶有內部代碼；日誌會保留。 | 工作階段診斷訊息（`Failed`）；從 `/recover` 擲回時 CLI 結束代碼 8 | 依內部代碼處置，然後執行 `/recover`。 |

## Ui

原生暫停選單操作（`ui.pause.*`）的代碼。

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_UI_NOT_PAUSED` | `CurrentRuntimeControl` | 該操作（接管、動作）需要 OMSI 處於暫停且顯示其原生暫停選單，但目前並非如此。 | 執行階段結果 | 等到使用者暫停 OMSI 為止。 |
| `OL_E_UI_BUSY` | `CurrentRuntimeControl` | 暫停選單動作已排程或正在執行（可能停留在原生強制回應對話方塊中），或呼叫是在 OMSI 的訊框內抵達。 | 執行階段結果 | 輪詢 `ui.pause.state` 直到 `busy` 為 `false`，然後重試。 |
| `OL_E_UI_ALREADY_TAKEN_OVER` | `CurrentRuntimeControl` | 原生暫停選單已被接管時又呼叫 `ui.pause.take-over`。 | 執行階段結果 | 使用 `ui.pause.state`，或先執行 `ui.pause.restore-native`。 |

## Warning

| 代碼 | 引發位置 | 意義 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | 該 OMSI 2 執行檔沒有已知配置（其程式碼識別不在組建目錄中）。計畫仍可執行：每個原生功能都會在處理程序內解析並驗證，無法驗證者即無法使用。 | 計畫診斷訊息（不影響可執行性） | 無需處理；請以 `/plan` 與 `profiles` 查看預測的功能。 |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService`（監督程式） | 已要求 `Behavior.CooperativeShutdown`，但無法要求 OMSI 結束（其結束常式在此組建上未解析，或 OMSI 已在關閉），或它未在 `ShutdownTimeoutSeconds` 內結束：OMSI 改為被終止。還原照常執行。 | 工作階段診斷訊息 | 無：工作階段的結束方式與強制終止相同。 |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | 啟動所需的功能無法由執行檔解析（例如受 DRM 保護、載入映像與檔案不同的執行檔）。它會在啟動時於處理程序內解析。 | 計畫診斷訊息（不影響可執行性） | 無需處理；若在處理程序內也無法解析，啟動會以 `OL_E_CAPABILITY_UNRESOLVED` 失敗。 |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | 自行加入工作階段的模組（設定檔、內容或預設啟用）無法載入（`Data["code"]` 指出原因，`Data["activation"]` 指出加入方式）。 | 計畫診斷訊息（不影響可執行性） | 修正、停用或移除該模組。 |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`、`ModuleSettingsFile`、`ModuleSettings` | `.omsilaunch\modules.ini` 的某個項目無法使用（不是模組區段、位於區段之外、未知的索引鍵、無效值；改用預設值），或為工作階段未載入的模組要求了設定。 | 計畫診斷訊息（不影響可執行性） | 更正該項目或要求。 |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | 某個工作階段刪除路徑（ITX 目標、`Texture\standard.ipr`、`closecheck`）在工作階段之前不存在、現在卻存在，但此日誌下從未啟動過任何 OMSI 處理程序，因此該檔案不可能是工作階段的副產物。它會被保留並回報（訊息 = 相對路徑，`Data["sha256"]`）；交易仍會完成。 | 工作階段診斷訊息／`RecoveryStatus.Diagnostics`（不影響狀態） | 檢查該檔案；若不需要請自行移除。 |

## Module

執行階段模組（請參閱[執行階段模組](runtime-modules.md)）。規劃時，代碼是計畫診斷訊息：對被要求的模組，它會使計畫不可執行；對自行加入的模組（設定檔、內容、預設），計畫改為回報 `OL_W_MODULE_NOT_LOADED`。在工作階段中，代碼是工作階段診斷訊息（除非代碼為 `OL_E_MODULE_SESSION_FAILED`，否則工作階段會繼續），對模組操作而言則是執行階段結果。

| 代碼 | 引發位置 | 意義／典型原因 | 呈現方式 | 處置 |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`、`ModuleCatalog` | `module.json` 不存在、不是有效的 `omsilaunch.runtime-module/v1`，或其 `id` 與目錄名稱不同。訊息會指出欄位。 | 計畫診斷訊息；`ListRuntimeModulesAsync` | 修正資訊清單。 |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | 被要求的模組 id 未安裝在 `plugins\OmsiLaunch.Modules` 之下。 | 計畫診斷訊息 | 檢查 `/list:RuntimeModules`。 |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`、`ModuleHost` | 固定的檔案不存在、與其固定的 SHA-256 不同，或是透過重新剖析點到達；或者在處理程序內，`module.json` 在規劃之後變更。 | 計畫診斷訊息；工作階段診斷訊息 | 重新安裝模組。 |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | 模組的 `bridge_protocol` 範圍不包含此組建所提供的任何協定（1 與 2）。 | 計畫診斷訊息 | 使用為此 OmsiLaunch 製作的模組版本。 |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`、`ModuleHost` | 必要的功能此組建不認得、在此安裝上無法使用，或未在處理程序內解析。 | 計畫診斷訊息；工作階段診斷訊息 | 請參閱[功能](capabilities.md)與 `profiles`。 |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`、`ModuleHost` | 工作階段未執行模組所綁定的地圖，或某個綁定的檔案與其固定的 SHA-256 不同。 | 計畫診斷訊息；工作階段診斷訊息 | 以綁定的內容啟動綁定的地圖。 |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | 模組要求啟動的檔案不是其輔助元件之一。 | 模組內的執行階段結果 | 模組缺陷。 |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`、`ModuleSessionPrograms` | 無法載入受管理元件或建立其進入型別；或模組的工作階段程式無法啟動，或在就緒前就結束。 | 工作階段診斷訊息 | 檢查模組組建（與 x86 相容，進入型別實作 `IRuntimeModule`）。 |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`、`GameplayReady` 或 `Stop` 擲回例外。 | 工作階段診斷訊息 | 模組缺陷；已套用其 `failure_policy`。 |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`、`ModuleSessionPrograms` | 某個階段或操作在其預算（`timeouts`、`timeout_ms`）之後才傳回，或工作階段程式未在 `prepare_ms` 內就緒。 | 工作階段診斷訊息；執行階段結果 | 模組缺陷；已套用其 `failure_policy`。 |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | 該模組已被停用，或在工作階段稍早失敗。 | 執行階段結果 | 請查看工作階段診斷訊息中的第一個失敗。 |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | 工作階段未載入此模組，或它尚未就緒（遊戲過程之前）。 | 執行階段結果 | 在啟動時要求該模組；等待 `Running`。 |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | 該模組沒有宣告這個操作。 | 執行階段結果 | 檢查模組的 `operations`。 |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | 操作失敗且沒有已登錄的代碼；模組自己的代碼位於 `Values["module_code"]`。 | 執行階段結果 | 依模組而定。 |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | 停止或失敗之後，某個被追蹤的資源仍然存活（未結束的輔助處理程序、未結束的背景執行緒），或釋放它時擲回例外。 | 工作階段診斷訊息 | 回報該模組；無論如何，處理程序都會隨 OMSI 結束。 |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | `failure_policy` 為 `fail-session` 的模組失敗；工作階段被停止並還原。 | 工作階段診斷訊息（狀態 `Failed`） | 請參閱前面的模組代碼。 |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`、`ModuleSettings`、CLI | 被要求的模組設定無效或模組未宣告它、某個模組同時被啟用與停用、設定名稱不是 `<module-id>.<key>`，或某個模組旗標由多個模組宣告。 | 計畫診斷訊息（不可執行）；CLI 錯誤 | 修正要求；`/list:RuntimeModules` 會顯示每個模組的設定。 |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`、`ModuleHost` | 模組在 `conflicts.plugins` 下列出的某個 OMSI 外掛程式已啟用（`plugins\*.opl`），或已在 OMSI 中載入。原因會指出該 `.opl`。 | 計畫診斷訊息；工作階段診斷訊息 | 停用或移除該外掛程式。OmsiLaunch 絕不會變更它。 |
| `OL_E_MODULE_OPERATION_NOT_DECLARED` | `ModuleHost` | 模組要求在處理程序內執行某個目錄操作（`IRuntimeModuleContext.Runtime`、原生 `run_operation`），但其資訊清單的 `runtime_operations` 並未列出該操作。此呼叫不會抵達 OMSI。 | 執行階段結果 | 在 `runtime_operations` 中列出該操作（需要 `bridge.runtime` 與 `bridge_protocol.min` 3）。 |
| `OL_E_MODULE_CONTENT_INPUT_UNDECLARED` | `ModuleHost` | 模組要求定位某種社群內容（`IRuntimeModuleContext.Content`），但其資訊清單未在 `content_inputs` 中宣告該種類。 | 內容結果 | 在 `content_inputs` 中宣告該種類。 |
| `OL_E_MODULE_CONTENT_INPUT_INVALID` | `RealmapPackage` | 無法安全讀取該地圖的 `openomsi-realmap` 套件：它是經由重新分析點到達、內含重新分析點、檔案超過 50,000 個或大於 4 GiB，或無法讀取。 | 內容結果 | 修復或重新產生該套件；模組會依資源降級。 |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | 保留 | 保留；原生元件改為接收[執行階段模組](runtime-modules.md#native-components-and-hooks)中所述的數值 hook 結果。 | 無 | 無 |

<a id="non-error-diagnostic-codes"></a>
## 非錯誤的診斷碼

| 代碼 | 發出位置 | 訊息／資料 | 意義 |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | 訊息 = PID；`Data["thread_id"]`、`Data["creation_utc"]`（ISO 8601） | `Omsi.exe` 已建立，且其身分已記錄。工作階段診斷訊息。 |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | 訊息 = 已移除檔案的 SHA-256 | 工作階段之前就已存在的 `closecheck` 已被永久移除（`SuppressStaleClosecheckWarning = true`）。工作階段診斷訊息。 |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | 訊息 = 相對路徑；`Data["sha256"]` | 在處理程序已啟動的工作階段期間，OMSI 重新建立了某個工作階段刪除路徑；為了還原原本不存在的狀態，它已被移除。工作階段診斷訊息／`RecoveryStatus.Diagnostics`。 |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | 訊息 = `manifest` 或 `self` | 永久外掛程式驗證所使用的參考來源。計畫診斷訊息。 |
| `module.planned` | `SessionPlanner` | 訊息 = 模組 id；`Data["module"]`、`Data["status"]`、`Data["activation"]` | 該模組在計畫中。計畫診斷訊息。 |
| `module.disabled` | `SessionPlanner` | 訊息 = 模組 id | 內容或預設模組因停用（要求或設定檔）而被排除。計畫診斷訊息。 |
| `session_profile.selected` | `SessionPlanner` | 訊息 = 設定檔 id；`Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | 由工作階段設定檔編譯而成的工作階段之來源資訊。計畫診斷訊息。 |

執行階段事件名稱（`RuntimeEvent.Type`，並非診斷訊息）列於[工作階段生命週期](../concepts/session-lifecycle.md#telemetry-events)。
