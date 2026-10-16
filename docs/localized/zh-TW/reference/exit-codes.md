# 結束代碼

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/exit-codes.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁列出 `OmsiLaunch.exe` 與 `OmsiLaunchW.exe` 可能回傳的所有處理程序結束代碼：公開的受控合約 `PublicExitCode`（`src\OmsiLaunch.Api\PublicControlContract.cs`）、原生 shim 代碼 `100`..`106`（`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` 與 `OmsiLaunch.WindowsHost.cpp`），以及 `CliProgram.Classify` 對任何未攔截例外套用的分類規則（`tools\OmsiLaunch.Cli\Program.cs`）。呼叫端必須依據代碼與結構化錯誤封套（envelope）判斷語意，絕不可依據訊息文字。錯誤碼列於[錯誤](errors.md)；產生各代碼的命令列於 [CLI 參考](cli.md)。

<a id="public-exit-codes-publicexitcode"></a>
## 公開結束代碼（`PublicExitCode`）

| 代碼 | 列舉名稱 | 意義 | 發生時機 |
|---|---|---|---|
| 0 | `Success` | 命令已完成。 | `/version`、`capabilities`、`help`、`profiles`、`detect`、`/list`、`/recovery-status`；計畫可執行時的 `/plan`／`/validate`；以 `Completed` 結束的工作階段；`OmsiLaunchW.exe` 啟動後的 `/silent`；擁有者以 `Ok=true` 回應的轉送用戶端命令；沒有待處理項目或還原已完成時的 `/recover`。 |
| 1 | `SessionFailed` | 計畫不可執行，或所擁有的工作階段以 `Failed` 結束。 | 回報 `NOT RUNNABLE` 的 `/plan`；計畫不可執行的啟動（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_PERMANENT_PLUGIN_*`……；`OmsiLaunchW.exe` 也會在訊息方塊中顯示最後一個 `OL_E_` 診斷訊息）；由 `StartSessionAsync` 引發的 `OL_E_PLAN_NOT_RUNNABLE`（啟動時重新規劃）；工作階段未在啟動逾時內進入 `Running`；工作階段以 `Failed` 終止。 |
| 2 | `InvalidArguments` | 命令列、spec、設定檔或執行階段引數在分派之前或期間遭到拒絕。 | 未知的旗標或路由、缺少值、值超出範圍；`SessionProfileException`（`OL_E_SESSION_PROFILE_*`）；`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`；`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`；`OL_E_ITX_PROFILE_REQUIRED`；`OL_E_RUNTIME_OPERATION_UNKNOWN` 與 `OL_E_RUNTIME_ARGUMENT_REQUIRED`（於本機產生或由擁有者回傳）；無法分派的命令所印出的用法說明；任何 `ArgumentException`、`FormatException`、`InvalidDataException` 或 `OverflowException`。 |
| 3 | `UnsupportedProfile` | 不支援此平台或 OMSI 組建。 | 代碼以 `OL_E_UNSUPPORTED_` 開頭的未攔截例外（`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_OS_ARCHITECTURE`）。請注意，若在規劃期間發現相同情況，計畫會變為不可執行，並改為回傳 `1`。 |
| 4 | `NoActiveSession` | 用戶端命令找不到擁有者。 | 此安裝的本機控制端點沒有回應時的 `session status`、`session stop`、`events read`、`events watch` 或轉送的執行階段操作（`OL_E_NO_ACTIVE_SESSION`）。 |
| 5 | `RuntimeUnavailable` | 逾時以例外形式未被攔截。 | 任何 `TimeoutException`（訊息未帶代碼時為 `OL_E_TIMEOUT`，否則為內嵌的代碼，例如 `OL_E_RUNTIME_REQUEST_TIMEOUT`）。轉送的用戶端逾時由擁有者以 `Ok=false` 回應，回傳 `7`，而非 `5`。 |
| 6 | `NotFound` | 找不到檔案或目錄。 | `FileNotFoundException`／`DirectoryNotFoundException`（預設為 `OL_E_NOT_FOUND`），例如 `OL_E_SPEC_NOT_FOUND`、以例外形式引發的 `OL_E_ITX_PROFILE_MISSING`、`/list` 期間缺少安裝目錄。 |
| 7 | `OperationRejected` | 命令有效但遭拒絕，或轉送的命令在擁有者端失敗。 | `OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`、`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED`、`OL_E_CANCELLED`；除兩個引數代碼以外的所有 `Ok=false` 控制回應（`OL_E_CONTROL_*`、`OL_E_RUNTIME_*`、`OL_E_SESSION_NOT_RUNNING`）；其他任何帶有未在別處分類之 `OL_E_` 代碼的未攔截例外（`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_PROCESS_*`……）。 |
| 8 | `TransactionRecoveryFailed` | 無法還原持久交易。 | 日誌原本待處理且仍維持待處理時的 `/recover`；代碼以 `OL_E_RECOVERY_` 開頭或為 `OL_E_RESTORE_FAILED` 的任何未攔截例外（例如工作階段自身還原期間的 `OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`）。 |
| 10 | `InternalError` | 沒有 `OL_E_` 代碼的非預期例外。 | 以 `OL_E_INTERNAL` 回報，類別為 `internal`；訊息為例外文字。 |

代碼 `9` 未指派。

<a id="native-shim-exit-codes"></a>
## 原生 shim 結束代碼

由 `OmsiLaunch.exe`／`OmsiLaunchW.exe` 在受控控制器執行之前回傳。這些代碼與 `PublicExitCode` 互不重疊，因此呼叫端可以區分主機啟動失敗與控制器結果。`OmsiLaunchW.exe` 另外會在訊息方塊中顯示 `OmsiLaunch could not start the .NET host (code N).`。

| 代碼 | 意義 | 原因 |
|---|---|---|
| 100 | 無法解析執行檔路徑 | `GetModuleFileNameW` 失敗。 |
| 101 | 無法將命令列切分為權杖 | `CommandLineToArgvW` 回傳 null。 |
| 102 | `hostfxr` 位置探查失敗 | `get_hostfxr_path` 大小查詢失敗：未安裝相符的 .NET runtime（需要 x64 .NET 6 runtime）。 |
| 103 | 無法取得 `hostfxr` 路徑 | 第二次 `get_hostfxr_path` 呼叫失敗。 |
| 104 | 無法載入 `hostfxr` 程式庫 | 對解析出的 `hostfxr.dll` 執行 `LoadLibraryW` 失敗。 |
| 105 | 缺少必要的 `hostfxr` 匯出函式 | 找不到 `hostfxr_initialize_for_dotnet_command_line`、`hostfxr_run_app` 或 `hostfxr_close`。 |
| 106 | 無法初始化受控主機 | `hostfxr_initialize_for_dotnet_command_line` 對 `OmsiLaunch.Controller.dll` 失敗（缺少 `OmsiLaunch.Controller.runtimeconfig.json`、缺少 x64 的 `Microsoft.WindowsDesktop.App` 6.0，或套件損毀）。 |

<a id="classification-rules-cliprogramclassify"></a>
## 分類規則（`CliProgram.Classify`）

每個從 `CliProgram.RunAsync` 逸出的例外，都會由呼叫 `Classify` 的 `CliProgram.ReportFailure` 轉換為錯誤封套（`CliInput.WriteError`）與結束代碼。剖析失敗在分派之前也以相同方式處理（結束代碼 `2`）。規則依下列順序套用：

1. 擷取例外訊息中的第一個 `OL_E_` 權杖（`ExtractCode`）：代碼是從 `OL_E_` 開始、由 ASCII 字母、數字與 `_` 組成的最長連續字元。代碼會原樣呈現在 `error.code` 中。
2. `SessionProfileException` → 其本身的 `Code`，類別 `invalid_argument`，結束代碼 `2`。
3. `ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException` → 擷取出的代碼或 `OL_E_INVALID_ARGUMENT`，類別 `invalid_argument`，結束代碼 `2`。
4. `FileNotFoundException`、`DirectoryNotFoundException` → 擷取出的代碼或 `OL_E_NOT_FOUND`，類別 `not_found`，結束代碼 `6`。
5. `TimeoutException` → 擷取出的代碼或 `OL_E_TIMEOUT`，類別 `runtime`，結束代碼 `5`。
6. `OperationCanceledException` → `OL_E_CANCELLED`，類別 `session`，結束代碼 `7`。
7. 其他情況下，若已擷取出代碼：
   - 以 `OL_E_RECOVERY_` 開頭或等於 `OL_E_RESTORE_FAILED` → 類別 `transaction`，結束代碼 `8`；
   - `OL_E_INSTALLATION_BUSY`、`OL_E_SESSION_ALREADY_ACTIVE` → 類別 `session`，結束代碼 `7`；
   - `OL_E_PLAN_NOT_RUNNABLE` → 類別 `session`，結束代碼 `1`；
   - `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_ITX_PROFILE_REQUIRED` → 類別 `invalid_argument`，結束代碼 `2`；
   - 以 `OL_E_UNSUPPORTED_` 開頭 → 類別 `unsupported_profile`，結束代碼 `3`；
   - 其他任何代碼 → `InvalidOperationException` 與 `IOException` 為類別 `runtime`，否則為 `internal`；結束代碼 `7`。
8. 完全沒有代碼 → `OL_E_INTERNAL`，類別 `internal`，結束代碼 `10`。

轉送的用戶端回應不經過 `Classify`：`CliProgram.ReportForwarded` 在沒有端點時回傳 `4`，對 `OL_E_RUNTIME_OPERATION_UNKNOWN`／`OL_E_RUNTIME_ARGUMENT_REQUIRED` 回傳 `2`，對其他任何 `Ok=false` 回應回傳 `7`，對 `Ok=true` 回傳 `0`。

<a id="scripting-guidance"></a>
## 撰寫指令碼的指引

- 將 `0` 視為成功，其餘皆視為失敗；先依數值代碼分支，再依 `--json` 封套中的 `error.code` 分支。
- 工作階段啟動只會在工作階段結束且其檔案已還原後才返回；`1` 表示交易已執行但 OMSI 失敗或計畫遭拒絕，並不表示檔案仍處於修改狀態（殘留的日誌會由 `/recovery-status` 回報）。
- `100`..`106` 表示套件或 .NET runtime 已損壞；請參閱[安裝](../getting-started/installation.md)與[封裝](packaging.md)。
