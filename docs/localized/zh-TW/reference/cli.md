# CLI 參考

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/cli.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 OmsiLaunch `0.1.0-beta4` 命令列完整的規範性參考：涵蓋三個可執行檔、引數文法、分派順序、每個命令字、每個階層式路由、每個旗標、輸出封套，以及每個命令的錯誤行為。本頁內容產生自 `tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Parse`、`CliInput.KnownFlags`、`CliInput.AcceptedNoEffectFlags`、`CliInput.CommandWordsAccepted`、`CliInput.HierarchicalRoutes`、`CliInput.BuildSpecAsync`、`CliEventWatch`）、`tools\OmsiLaunch.Cli\LaunchSpecJson.cs`，以及 `tools\OmsiLaunch.Bootstrapper` 下的兩個原生 shim。處理程序結果列於[結束代碼](exit-codes.md)；錯誤碼列於[錯誤](errors.md)；實際呼叫範例請見 [CLI 範例](cli-examples.md)。

<a id="executables"></a>
## 可執行檔

| 檔案 | 子系統 | 角色 | 差異 |
|---|---|---|---|
| `OmsiLaunch.exe` | 主控台 | 原生啟動程式（`OmsiLaunch.Bootstrapper.cpp`）：解析自身所在目錄，以 `CommandLineToArgvW` 將命令列切分為 token，透過 `nethost.dll` 找到 `hostfxr`，並以相同引數執行 `OmsiLaunch.Controller.dll`。 | 會寫入主控台輸出；處理程序結束代碼即為受控控制器的結束代碼；若無法啟動 .NET 主機，則為 shim 代碼 `100`..`106`。 |
| `OmsiLaunchW.exe` | Windows（GUI） | 相同的 shim（`OmsiLaunch.WindowsHost.cpp`），針對 Windows 子系統組建。它會在啟動控制器之前設定環境變數 `OMSILAUNCH_WINDOWS_HOST=1`。 | 沒有主控台：除非指定 `--json`，否則主控台輸出會被抑制（`WindowsHost.SuppressConsole`）；失敗以訊息方塊顯示（`WindowsHost.ShowFailure`：訊息、`Code: OL_E_...`，以及提示 `See .omsilaunch\diagnostics for details.`）；shim 失敗 `100`..`106` 會顯示為 `OmsiLaunch could not start the .NET host (code N).`。完整行為請見 [OmsiLaunchW.exe 參考](omsilaunchw.md)。 |
| `OmsiLaunch.Controller.dll` | 受控（x64、`net6.0-windows`、Windows Forms） | 控制器本身。使用者永遠不會直接呼叫它；兩個 shim 都會將控制器路徑作為第一個主機引數傳入，因此它永遠不會出現在公開引數清單中。 | 需要含 `Microsoft.WindowsDesktop.App` 的 x64 .NET 6 runtime；請見[安裝](../getting-started/installation.md)。 |

`nethost.dll` 必須與 shim 放在同一目錄。shim 本身不讀取任何引數；每個引數都會原封不動地傳到 `CliInput.Parse`，因此 `OmsiLaunch.exe` 與 `OmsiLaunchW.exe` 接受完全相同的語法。

<a id="invocation-model"></a>
## 呼叫模型

<a id="argument-grammar-cliinputparse"></a>
### 引數文法（`CliInput.Parse`）

| 形式 | 意義 |
|---|---|
| `/key:value`、`/key`、`-key:value`、`-key` | 旗標。key 不區分大小寫；value 為第一個 `:` 之後的所有內容。未知的 key 會以 `OL_E_INVALID_ARGUMENT`（`Unknown argument: ...`）失敗，結束代碼 `2`。 |
| `--key=value` | 所選執行階段操作的執行階段引數（例如 `--handle=rv-000001`）。任何包含 `=` 的 `--` token 都是執行階段引數，絕不是旗標。 |
| `--json`、`/json` | 結構化輸出（請見[輸出格式](#output-formats)）。`--json` 是唯一有意義且不含 `=` 的 `--` token；它會被解析為 `/json` 旗標。 |
| 單純字詞 | 若尚未出現命令字，且該字詞是[命令字](#command-words)之一，它就成為命令。一旦出現命令字，之後的每個單純字詞都是命令字（即路由）。否則，第一個單純字詞是安裝根目錄，之後的單純字詞則附加到路由。 |

結果：階層式路由（`time get`）不能與放在其後的安裝引數組合（`time get D:\OMSI` 會被視為未知路由 `time get d:\omsi`，結束代碼 `2`）。`D:\OMSI time get` 可被接受，但屬於**擁有者模式**（會啟動新的工作階段，並在其中執行一次該操作）。解析錯誤（`ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException`）與工作階段設定檔錯誤（`SessionProfileException`）會在任何動作執行前回報，結束代碼一律為 `2`。

<a id="installation-root"></a>
### 安裝根目錄

- 明確的單純字詞安裝引數優先於 `/spec` 檔案中的 `RootPath`（`CliInput.BuildSpecAsync`）。
- `.` 表示包含可執行檔的目錄（`AppContext.BaseDirectory`），絕不是呼叫端的工作目錄（`CliInput.ResolveInstallationRoot`）。可攜式套件依賴此行為。
- 省略此引數時，擁有者模式的操作（`/new`、`/saved`、`/spec`、`/list`、`/recovery-status`、`/recover`）同樣使用可執行檔所在目錄。路徑會以 `Path.GetFullPath` 正規化。
- 用戶端模式的命令永遠不接受安裝引數：它們會連線到可執行檔所在安裝（`AppContext.BaseDirectory`）的本機控制端點。請見[本機控制](local-control.md)。

<a id="owner-and-client"></a>
### 擁有者與用戶端

- **擁有者**：規劃、啟動、監督並還原工作階段的處理程序（`OwnerSession.RunAsync`）。它持有安裝租用（`Local\OmsiLaunch.Installation.<sha256(root)>`）與設定交易，在工作階段存續期間公開本機控制端點，並顯示[系統匣圖示](windows-tray.md)。每個安裝只能有一個擁有者：若已有擁有者在控制端點上回應 `session.status`，第二次啟動會以 `OL_E_SESSION_ALREADY_ACTIVE` 失敗（結束代碼 `7`）。
- **用戶端**：任何不帶安裝引數、並傳送 `session status`、`session stop`、`events read`、`events watch` 或執行階段操作的呼叫。它會透過本機控制管道轉送；若沒有擁有者，則以 `OL_E_NO_ACTIVE_SESSION` 失敗（結束代碼 `4`）。

<a id="dispatch-order-cliprogramrunasync"></a>
### 分派順序（`CliProgram.RunAsync`）

1. `/silent`（尚未在 `OmsiLaunchW.exe` 下執行時）：透過 `ShellExecute`（不繼承 handle）從可執行檔所在目錄啟動 `OmsiLaunchW.exe`，使用相同引數但去除 `/silent`／`--silent`，寫出 `silent` 封套（`delegated`、`host_process_id`）並回傳 `0`。主控台處理程序不會等待工作階段；請見 [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation)。`OL_E_WINDOWS_HOST_MISSING`／`OL_E_WINDOWS_HOST_START_FAILED` 回傳 `7`。
2. `/version`：`version` 封套，包含 `product`、`version`（組件資訊版本，由 `OmsiLaunch.Version.props` 標記，`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family`（`OMSI_2_3_004_COMMON`）；結束代碼 `0`。
3. `capabilities`：包含標準目錄 `PublicCapabilityRegistry` 中每個 `PublicStableBeta` 或 `PublicExperimental` 描述元（含其操作、原生功能、證據與備註）的封套；結束代碼 `0`。
4. `help [family]`：`help` 封套，包含 `usage`、`product_version`、`protocol_version`、`family` 以及公開的 `commands`（`CliRoute`、`Description`、`Classification`、`RuntimeValidation`），可選擇依 family 篩選；結束代碼 `0`。
5. `profiles`：包含 `requirement`（`OMSI 2`）、目標根目錄的 `installation` 報告（`is_omsi2`、`reason`、`build_id`、`identity` 的 `known`／`unknown`、`file_sha256`、`file_version`、`code_sha256`、`file_variant`）、`known_layouts`，以及由可執行檔解析出的 `predicted_capabilities` 的封套（啟動時於處理程序內的解析才是權威結果）；結束代碼 `0`。
6. 用戶端執行階段操作（沒有安裝引數，且有路由或 `/runtime:`）：引數會依 `PublicCapabilityRegistry.ValidateRuntimeArguments` 驗證（`OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`，結束代碼 `2`），接著以 8 s 逾時轉送 `runtime.execute`（`road-vehicles.spawn` 為 30 s）。
7. 用戶端 `session status`（750 ms）、`session stop`（綁定至作用中的工作階段 ID，750 ms）、`events read`（750 ms）、`events watch`（每 250 ms 輪詢一次，直到按下 Ctrl+C）。
8. `detect`，或**完全沒有引數**（沒有安裝、沒有命令、沒有 `/?`、沒有 `/spec`、沒有啟動旗標、沒有復原旗標、沒有 `/list`）：列舉 `Omsi` 處理程序並探測控制端點（250 ms）；`detect` 封套；結束代碼 `0`。
9. `/?` 或 `/help`：印出用法文字，結束代碼 `0`。任何其他具有命令字但沒有可分派路由的呼叫（例如單獨的 `d3d` 或 `session status D:\OMSI`）會印出用法文字並以 `2` 結束。
10. 擁有者模式。前置條件：`plugins\OmsiLaunch.Plugin.opl` 與 `plugins\OmsiLaunch.Native.x86.dll` 必須存在於可執行檔旁（`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`，結束代碼 `7`）。可執行檔旁若有 `release-manifest.json`，則由它提供預期的外掛程式雜湊。
11. `/recovery-status`／`/recover`：`RecoverPendingAsync`；`recover` 封套，包含 `pending`、`recovered`、`diagnostics`；僅在要求還原但未完成時結束代碼為 `8`，否則為 `0`。
12. `/list:<category>`：`DiscoverAsync`；`content.list` 封套；結束代碼 `0`。`/list:SessionProfiles` 改用 `ListSessionProfilesAsync`；封套為 `session-profiles.list`。
13. 建立 `LaunchSpec`（`BuildSpecAsync`）、規劃它（`PlanSessionAsync`）並印出計畫。`/plan` 或 `/validate`：若 `IsRunnable` 則結束代碼 `0`，否則為 `1`。不可執行的計畫永遠不會啟動 OMSI（結束代碼 `1`）；在 `OmsiLaunchW.exe` 下，以不可執行計畫啟動時，會在訊息方塊中顯示其最後一筆 `OL_E_` 診斷訊息（文件稽核 BUG-06）。規劃時也會依 `release-manifest.json` 驗證已安裝的永久外掛程式封閉集合，因此外掛程式遺失或遭修改會使計畫不可執行（`OL_E_PERMANENT_PLUGIN_*`）。
14. 探測是否已有擁有者（`OL_E_SESSION_ALREADY_ACTIVE`，結束代碼 `7`），然後執行 `OwnerSession.RunAsync`。

<a id="owner-lifecycle-ownersessionrunasync"></a>
### 擁有者生命週期（`OwnerSession.RunAsync`）

1. `StartSessionAsync(plan)`。自此之後，每條結束路徑都會在 `finally` 區塊中到達 `CloseAsync`：例外狀況、Ctrl+C（`Console.CancelKeyPress`）、主控台關閉／登出（`AppDomain.ProcessExit`，停止＋還原的時間預算為 4 s；剩餘部分會在下次啟動時由日誌復原）、系統匣「End session」、管道 `session.stop`，以及 `/observe-seconds`。
2. 除非規格中設定了 `Presentation.SuppressTrayIcon`，否則會建立系統匣圖示。
3. 等待 `Running` 狀態，最長 `StartupTimeoutSeconds + 5` 秒。會印出狀態。若狀態不是 `Running`，結束代碼 `1`（`OmsiLaunchW.exe` 會顯示 `The OMSI session did not reach gameplay.`，並附上最後一筆 `OL_E_` 診斷訊息或 `OL_E_SESSION_START_FAILED`）。
4. 執行驗證批次（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`）並寫出其產出檔案。
5. 啟動本機控制端點。
6. `/runtime:<operation>` 執行一次（5 s，`road-vehicles.spawn` 為 15 s）；結果會寫入 `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` 並印出。失敗的執行階段命令永遠不會終止工作階段（改為印出 `runtime_error`）。
7. 等待：使用 `/observe-seconds:n` 時，工作階段會在 `n` 秒後停止，**或**在系統匣／管道要求停止、或 OMSI 結束時提早停止；未使用時，擁有者會等到 OMSI 結束或收到停止要求。
8. 印出完成狀態；若為 `Completed` 則結束代碼 `0`，否則為 `1`。

`session.stop`、系統匣「End session」、Ctrl+C 與 `CloseAsync` 都會要求標準停止：以 `TerminateProcess` 終止 OMSI（OMSI 自身的關閉程序不會執行，`options.cfg` 也不會被 OMSI 重寫），然後還原每個由工作階段擁有的檔案。請見[工作階段生命週期](../concepts/session-lifecycle.md)與[交易與復原](../concepts/transactions-and-recovery.md)。

<a id="command-words"></a>
## 命令字

第一個位置可接受的每個字詞（`CliInput.CommandWordsAccepted`）：

| 字詞 | 用途 | 模式 | 備註 |
|---|---|---|---|
| `capabilities` | 列出公開功能 | 本機，不需工作階段 | `capabilities` 封套。 |
| `profiles` | 列出支援的 `Omsi.exe` 變體 | 本機，不需工作階段 | `profiles` 封套。 |
| `detect` | 回報 `Omsi.exe` 處理程序與作用中的擁有者 | 本機，不需工作階段 | 也是未提供任何引數時的預設行為。狀態：`NO_OMSI_FOUND`、`OMSI_FOUND_UNMANAGED`，無法檢查二進位檔時為各處理程序的 `UNKNOWN_BINARY_FOUND`；`active_omsilaunch_instance`、`managed_session`。 |
| `help` | 用法與公開命令目錄 | 本機，不需工作階段 | `help <family>` 依功能 family 篩選（`session`、`time`、`weather`、`map`、`camera`、`vehicles`、`player`、`humans`、`timetable`、`scripts`、`constants`、`curves`、`hof`、`drivers`、`tickets`、`d3d`、`events`）。 |
| `session` | `session status`、`session stop` | 用戶端 | 其後必須恰好一個字詞；其他任何情況都會印出用法，結束代碼 `2`。`session plan`／`session start` 是 API 路由名稱，不是 CLI 字詞：請使用 `/plan` 與 `/new`。 |
| `events` | `events read`、`events watch` | 用戶端 | `read` 一次性回傳有界事件清單；`watch` 每 250 ms 將每個新事件（依 `Sequence`）以 `events.watch` 封套印出，直到按下 Ctrl+C（結束代碼 `0`），沒有擁有者回應時為 `4`，控制錯誤時為 `7`。 |
| `time` | `time get`、`time set` | 用戶端路由 | |
| `weather` | `weather get`、`weather set`、`weather metar set`、`weather actual get` | 用戶端路由 | |
| `map` | `map get` | 用戶端路由 | |
| `camera` | `camera get`、`camera set`、`camera lock`、`camera unlock` | 用戶端路由 | |
| `vehicles` | `vehicles list`、`vehicles get`、`vehicles summary`、`vehicles spawn`、`vehicles place-random` | 用戶端路由 | |
| `player` | `player get` | 用戶端路由 | |
| `humans` | `humans list`、`humans get`、`humans summary` | 用戶端路由 | |
| `timetable` | `timetable get`、`timetable <table> list`、`timetable logs list` | 用戶端路由 | |
| `scripts` | `scripts variable list|get|set`、`scripts string list|get|set`、`scripts trigger`、`scripts sound-trigger` | 用戶端路由 | |
| `constants` | `constants list`、`constants get` | 用戶端路由 | |
| `curves` | `curves list`、`curves evaluate` | 用戶端路由 | |
| `hof` | `hof get` | 用戶端路由 | |
| `drivers` | `drivers list` | 用戶端路由 | |
| `tickets` | `tickets get` | 用戶端路由 | |
| `d3d` | 保留的 family 字詞 | 無 | `d3d` **沒有階層式路由**：`d3d texture ...` 是未知路由（結束代碼 `2`），單獨的 `d3d` 會印出用法（結束代碼 `2`）。D3D 操作透過 `/runtime:d3d.status`、`/runtime:d3d.texture.create` 等方式呼叫（請見[沒有路由的操作](#operations-without-a-route)）。 |

<a id="hierarchical-routes"></a>
## 階層式路由

`CliInput.HierarchicalRoutes` 將小寫化的路由對應到執行階段操作 ID。所有路由都需要 `Running` 工作階段，並透過執行階段信箱（mailbox）執行（`ExecuteRuntimeAsync`）。執行階段寫入只會變更 OMSI 的記憶體內狀態：它們永遠不會動到檔案，不屬於設定交易，停止時也**不會**被還原（OMSI 會被終止）。穩定性依 `PublicCapabilityRegistry` 與[驗證矩陣](../status/runtime-validation-status.md)而定；詳細說明與結果欄位請見[執行階段控制](runtime-control.md)。

| 路由 | 執行階段操作 | 種類 | 需要 Running | 變更 OMSI | 參與還原 | 穩定性 | 備註 |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | 是 | 否 | 無 | STABLE_BETA | 時鐘與日曆欄位。 |
| `time set` | `time.set` | Write | 是 | 是（記憶體內時鐘） | 無，不會還原 | EXPERIMENTAL | 例如 `--minute=<0..59>`；寫入、讀回與還原已於 2026-09-20 驗證。 |
| `weather get` | `weather.read` | Read | 是 | 否 | 無 | STABLE_BETA | |
| `weather set` | `weather.set` | Write | 是 | 是（記憶體內的天氣） | 無，不會還原 | EXPERIMENTAL | 例如 `--wind_speed=5 --cloud_type="Cumulus 1"`。手動天氣：即時 METAR 更新會停止。執行階段驗證待完成。 |
| `weather metar set` | `weather.metar.set` | Write | 是 | 是（記憶體內的天氣；OMSI 會把 ICAO 儲存到 `options.cfg`，工作階段會將其還原） | 無，不會還原 | EXPERIMENTAL | `--icao=<ICAO>`。即時 METAR 天氣，與 OMSI 的天氣對話方塊所設定的相同。執行階段驗證待完成。 |
| `weather actual get` | `weather.actual.read` | Read | 是 | 否 | 無 | EXPERIMENTAL | 實際天氣／ICAO 控制器狀態。 |
| `map get` | `map.read` | Read | 是 | 否 | 無 | STABLE_BETA | 地圖名稱、檔案、說明、圖塊數、年份範圍與行車方向；已在修正後的地圖槽位上重新於執行階段驗證。 |
| `camera get` | `camera.read` | Read | 是 | 否 | 無 | STABLE_BETA | |
| `camera set` | `camera.set` | Write | 是 | 是（攝影機純量，例如 `--field_of_view=`） | 無，不會還原 | EXPERIMENTAL | FOV 寫入／讀回已驗證。 |
| `camera lock` | `camera.lock` | Action | 是 | 是（工作階段範圍的原則） | 無 | EXPERIMENTAL | 需要 `--family=<0..3>`（駕駛員=0、乘客=1、外部=2、地圖=3），可選 `--preset=<n>`（family 0 或 1）。需要玩家車輛（例如已儲存情境）。已在執行階段封閉驗證中於執行階段驗證（`CAM01`）；功能登錄的 `RuntimeValidation` 字串仍顯示 `STATICALLY_VALIDATED`（請見[功能](capabilities.md)）。 |
| `camera unlock` | `camera.unlock` | Action | 是 | 是 | 無 | EXPERIMENTAL | 解除由 `camera lock` 設定的原則（`CAM01`）。 |
| `vehicles list` | `road-vehicles.list` | Read | 是 | 否 | 無 | STABLE_BETA | 回傳工作階段範圍的 `rv-NNNNNN` handle。 |
| `vehicles get` | `road-vehicle.read` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`。失效的 handle：`OL_E_RUNTIME_OBJECT_HANDLE_STALE`。 |
| `vehicles summary` | `road-vehicles.read` | Read | 是 | 否 | 無 | STABLE_BETA | 計數與玩家狀態，不含 handle。 |
| `vehicles spawn` | `road-vehicles.spawn` | Action | 是 | 是（新增一輛 RoadVehicle） | 無，不會移除 | EXPERIMENTAL | 需要 `--model=Vehicles\...\*.bus`。用戶端逾時 30 s，擁有者逾時 15 s。不會指派玩家車輛。RV-003 `RUNTIME_VALIDATED`。 |
| `vehicles place-random` | `road-vehicles.place-random` | Action | 是 | 是 | 無 | EXPERIMENTAL | 已建立設定檔的 `PlaceRandomBus`。 |
| `player get` | `player-vehicle.read` | Read | 是 | 否 | 無 | STABLE_BETA | 沒有玩家車輛時為語意上的 null。 |
| `humans list` | `humans.list` | Read | 是 | 否 | 無 | EXPERIMENTAL | 回傳 `hb-NNNNNN` handle。 |
| `humans get` | `human.read` | Read | 是 | 否 | 無 | EXPERIMENTAL | 需要 `--handle=`。 |
| `humans summary` | `humans.read` | Read | 是 | 否 | 無 | EXPERIMENTAL | 僅計數。 |
| `timetable get` | `timetable.read` | Read | 是 | 否 | 無 | STABLE_BETA | 時刻表管理器狀態。 |
| `timetable tracks list` | `timetable.tracks.list` | Read | 是 | 否 | 無 | STABLE_BETA | 屬於 `timetable.read` 功能；批次讀取證據 2026-09-20。 |
| `timetable trips list` | `timetable.trips.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable lines list` | `timetable.lines.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable tours list` | `timetable.tours.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable profiles list` | `timetable.profiles.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable station-links list` | `timetable.station-links.list` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable logs list` | `timetable.logs.read` | Read | 是 | 否 | 無 | STABLE_BETA | 同上。 |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | 是 | 否 | 無 | EXPERIMENTAL | OMSI 不讓其自由用於 AI 的車輛編號（由時刻表班次或固定 AI 清單保留）。分頁（`cursor`、`limit`，每頁最多 256 筆）。 |
| `map tiles list` | `map.tiles.list` | Read | 是 | 否 | 無 | EXPERIMENTAL | 地圖的圖磚：格線位置、自身索引、檔案與載入狀態，以及物件、路徑與曲線的數量。分頁（`cursor`、`limit`，每頁最多 256 筆）。 |
| `drivers list` | `drivers.read` | Read | 是 | 否 | 無 | EXPERIMENTAL | 駕駛員記錄。 |
| `tickets get` | `tickets.read` | Read | 是 | 否 | 無 | EXPERIMENTAL | 車票套組記錄。 |
| `hof get` | `vehicle.hofs.read` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`。 |
| `constants list` | `vehicle.constants.list` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`。 |
| `constants get` | `vehicle.constant.get` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`、`--name=`。 |
| `curves list` | `vehicle.curves.list` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`。 |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | 是 | 否 | 無 | STABLE_BETA | 需要 `--handle=`、`--name=`、`--x=`。 |
| `scripts variable list` | `vehicle.variables.list` | Read | 是 | 否 | 無 | EXPERIMENTAL | 需要 `--handle=`。 |
| `scripts variable get` | `vehicle.variable.get` | Read | 是 | 否 | 無 | EXPERIMENTAL | 需要 `--handle=`、`--name=`。 |
| `scripts variable set` | `vehicle.variable.set` | Write | 是 | 是（指令碼變數） | 無，不會還原 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`（有限數值）。 |
| `scripts string list` | `vehicle.string-variables.list` | Read | 是 | 否 | 無 | EXPERIMENTAL | 需要 `--handle=`。 |
| `scripts string get` | `vehicle.string-variable.get` | Read | 是 | 否 | 無 | EXPERIMENTAL | 需要 `--handle=`、`--name=`。 |
| `scripts string set` | `vehicle.string-variable.set` | Write | 是 | 是（記憶體內的指令碼狀態） | 無，不會還原 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`。OMSI 自己的 `SetStringVariable`。執行階段驗證待完成。 |
| `scripts trigger` | `vehicle.trigger` | Action | 是 | 是（執行車輛指令碼程式碼） | 無，不會還原 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`（`1` 按下、`0` 放開、`2` 拖曳）。執行階段驗證待完成。 |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | 是 | 是（播放車輛音效） | 無，不會還原 | EXPERIMENTAL | 需要 `--handle=`、`--trigger=`；可選 `--file=`。執行階段驗證待完成。 |

<a id="operations-without-a-route"></a>
### 沒有路由的操作

下列公開操作 ID（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）沒有階層式路由，需以 `/runtime:<operation>` 搭配 `--key=value` 或 `/runtime-arg:key=value` 呼叫：`timetable.rv-files.list`、`timetable.track-entries.list`、`timetable.tour-entries.list`、`d3d.status`、`d3d.texture.create`（必要：`width`、`height`、`format`；選用：`levels`）、`d3d.texture.describe`（`handle`；選用：`level`）、`d3d.texture.update`（必要：`handle`、`width`、`height`、`pixels_base64`；選用：`level`、`x`、`y`）、`d3d.texture.release`（`handle`）。D3D 操作為 EXPERIMENTAL；材質生命週期與裝置重設造成的失效已於執行階段驗證（執行階段封閉驗證 `H02`、`D01`；請見[功能](capabilities.md)）。`timetable.track-entries.list` 與 `timetable.tour-entries.list` 是有界清單：無法放入執行階段槽位的結果會被縮短（`truncated=true`）。`internal.road-vehicles.make-basic` 為 INTERNAL，CLI 與 API 都會以 `OL_E_RUNTIME_OPERATION_UNKNOWN` 拒絕它。

<a id="flags"></a>
## 旗標

`CliInput.KnownFlags` 中的每個旗標。「階段」分為 *launch-time*（形塑新工作階段的 `LaunchSpec`／計畫）、*runtime*（作用於執行中的工作階段）或 *control*（變更 CLI 本身的行為）。僅為相容性而解析的旗標（`CliInput.AcceptedNoEffectFlags`）會在其所在列標示。

<a id="control-and-output"></a>
### 控制與輸出

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/?` | `/?` | 關閉 | control | STABLE_BETA | 印出用法文字，結束代碼 `0`。 |
| `/help` | `/help` | 關閉 | control | STABLE_BETA | 與 `/?` 相同。（單純字詞 `help` 則回傳結構化目錄。） |
| `/version` | `/version` | 關閉 | control | STABLE_BETA | `version` 封套，結束代碼 `0`。除 `/silent` 外，在所有其他命令之前評估。 |
| `/json` | `/json` 或 `--json` | 關閉 | control | STABLE_BETA | 輸出 JSON 封套；即使在 `OmsiLaunchW.exe` 下也會強制主控台輸出。 |
| `/quiet` | `/quiet` | 關閉 | control | STABLE_BETA | 抑制供人閱讀的成功輸出。JSON 輸出（`/json`）與錯誤仍會寫出；結束代碼不變。 |
| `/silent` | `/silent`（亦可 `--silent`） | 關閉 | control | EXPERIMENTAL | 將整個命令列委派給 `OmsiLaunchW.exe`，主機處理程序一啟動即回傳 `0`。工作階段結果由 `OmsiLaunchW.exe`（訊息方塊、系統匣圖示）、`.omsilaunch\diagnostics` 與本機控制端點回報。委派與失敗對話方塊已於執行階段驗證（執行階段封閉驗證 `T04`）；請見 [OmsiLaunchW.exe](omsilaunchw.md)。 |
| `/serve` | `/serve` | — | control | REMOVED | 已於 0.1.0-beta4 移除：以 `OL_E_INVALID_ARGUMENT` 拒絕（結束代碼 `2`）。工作階段擁有者一律提供本機控制端點。 |
| `/verbose` | `/verbose` | 關閉 | launch-time | PARTIAL | `DiagnosticsSpec.Verbose`。值會帶入規格中；其效果僅限於 `.omsilaunch\diagnostics` 下的主機追蹤。 |
| `/log` | `/log` | 開啟（`DiagnosticsSpec.Log` 預設為 `true`） | launch-time | PARTIAL | `DiagnosticsSpec.Log`。實際上一律開啟。 |
| `/logall` | `/logall` | 關閉 | launch-time | PARTIAL | 同時設定 `Verbose`、`ProcessTrace`、`PluginTrace` 與 `NativeTrace`。 |
| `/omsi-logall` | `/omsi-logall` | 關閉 | launch-time | PARTIAL | `DiagnosticsSpec.OmsiLogAll`。 |
| `/trace` | `/trace` | 關閉 | launch-time | PARTIAL | `/trace-process` 的別名。 |
| `/trace-process` | `/trace-process` | 關閉 | launch-time | PARTIAL | `DiagnosticsSpec.ProcessTrace`。 |
| `/trace-plugin` | `/trace-plugin` | 關閉 | launch-time | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`：外掛程式會將每個執行階段操作（`plugin.trace.operation`：操作、結果、代碼、耗時）回報到工作階段的主機追蹤。 |
| `/trace-native` | `/trace-native` | 關閉 | launch-time | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`：原生層會將每次 hook 的安裝與移除（`native.trace`）回報到工作階段的主機追蹤。 |

<a id="planning-validation-and-harnesses"></a>
### 規劃、驗證與測試工具

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/plan` | `/plan` | 關閉 | launch-time | STABLE_BETA | 建立並印出 `SessionPlan`，不啟動 OMSI。`IsRunnable` 時結束代碼 `0`，否則為 `1`。需要啟動選擇（`/new`、`/saved`、`/spec` 或安裝引數）；單獨使用 `/plan` 而沒有其他引數時會執行 `detect`。 |
| `/validate` | `/validate` | 關閉 | launch-time | STABLE_BETA | 在此組建中與 `/plan` 完全相同。 |
| `/runtime-batch` | `/runtime-batch` | 關閉 | runtime（擁有者） | INTERNAL | 驗證測試工具：到達 `Running` 後執行讀取操作集，並寫出 `<sessionId>-runtime-read-batch.json`。 |
| `/runtime-write-batch` | `/runtime-write-batch` | 關閉 | runtime（擁有者） | INTERNAL | 驗證測試工具：讀取，再加上帶還原的 `time.set`、`camera.set` 與 `vehicle.variable.set`；寫出 `<sessionId>-runtime-write-batch.json`。 |
| `/d3d-batch` | `/d3d-batch` | 關閉 | runtime（擁有者） | INTERNAL | D3D 材質生命週期的驗證測試工具；寫出 `<sessionId>-d3d-wave-d-batch.json`。 |
| `/runtime` | `/runtime:<operation>` | 無 | runtime | STABLE_BETA（分派） | 依 ID 選擇公開執行階段操作。用戶端模式（沒有安裝引數）：轉送給擁有者。擁有者模式：在 `Running` 後執行一次。未知 ID：`OL_E_RUNTIME_OPERATION_UNKNOWN`，結束代碼 `2`。 |
| `/runtime-arg` | `/runtime-arg:<key>=<value>`（可重複） | 無 | runtime | STABLE_BETA（分派） | 執行階段引數；等同 `--key=value`。缺少 `=`：`/runtime-arg requires key=value`，結束代碼 `2`。 |

<a id="world-selection"></a>
### 世界選擇

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` 是預設模式，但只有在出現 `/new`、`/saved`、`/last`、`/spec` 其中之一時才會要求啟動 | launch-time | STABLE_BETA | NEW_MAP。需要 `/map` 與 `/entrypoint-index`（沒有呈現的進入點索引的計畫會回報 `OL_E_ENTRYPOINT_REQUIRED`；沒有 `/map` 時不會解析任何地圖）。`/new` 永遠不會自動選擇地圖。 |
| `/saved` | `/saved:<file.osn>` | 無 | launch-time | STABLE_BETA | SAVED_SITUATION。地圖與位置來自 `.osn`；`/map`、`/entrypoint`、`/entrypoint-index` 與 `/saved` 併用時會被拒絕（結束代碼 `2`）。情境遺失：`OL_E_SITUATION_NOT_FOUND`；其地圖遺失：`OL_E_SITUATION_MAP_NOT_FOUND`。 |
| `/last` | `/last /map:<identity>` | 無 | launch-time | EXPERIMENTAL | LAST_MAP_STATE：透過啟動表單的最後狀態分支，載入該地圖自己的 `laststn.osn`（OMSI 關閉地圖時寫入）。需要 `/map`；沒有 `laststn.osn` 的地圖為 `OL_E_SITUATION_NOT_FOUND`。絕不使用「最新 `.osn`」的啟發式做法。執行階段驗證待完成。 |
| `/map` | `/map:<identity>`（例如 `maps\Grundorf\global.cfg`） | 無 | launch-time | STABLE_BETA | `/new` 的地圖識別，或 `/list:Entrypoints` 的範圍。未知：`OL_E_MAP_NOT_FOUND`。 |
| `/entrypoint` | `/entrypoint:<identity>` | 無 | launch-time | EXPERIMENTAL | 以精確（區分大小寫）的標籤，或 `/list:entrypoints` 的識別指定進入點。OMSI 只會呈現每個標籤的第一筆記錄；共用該標籤的後續記錄會被拒絕（`OL_E_ENTRYPOINT_NOT_PRESENTED`），未知者亦同（`OL_E_ENTRYPOINT_NOT_FOUND`）。執行階段驗證待完成。與 `/entrypoint-index` 互斥（識別優先並清除索引）。 |
| `/entrypoint-index` | `/entrypoint-index:<n>`，`0..2147483647` | 無 | launch-time | STABLE_BETA | 進入點在呈現清單中的索引（依 OMSI 呈現方式從 1 起算）。可執行的 NEW_MAP 計畫必須提供。 |

<a id="date-time-and-weather"></a>
### 日期、時間與天氣

這些旗標會被接受並帶入 `LaunchSpec`。開始日期、時間與年份只適用於 NEW_MAP 工作階段：搭配 `/saved` 或 `/last` 時，規劃器會將它們記錄為 `UNAVAILABLE`，**並加入 `OL_E_CAPABILITY_UNAVAILABLE`，因此計畫為 NOT RUNNABLE（結束代碼 `1`）**，因為已儲存情境保有自己的時鐘。天氣則適用於所有世界模式。它們全部都是 `EXPERIMENTAL`：原生啟動路徑已靜態收尾，執行階段驗證待完成。設定這些值的 `/spec` 檔案或工作階段設定檔也有相同效果。

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` 或 `/date:system` | 未設定 | launch-time | EXPERIMENTAL | `DateSpec` 明確值／系統：NEW_MAP 工作階段的起始日期（請參閱 [launchspec](launchspec.md#datespec-timespec-yearspec)）。無法解析的值：`OL_E_INVALID_ARGUMENT`，結束代碼 `2`；不可能的日期：`OL_E_DATE_TIME_APPLY_FAILED`。 |
| `/time` | `/time:<hh:mm[:ss]>` 或 `/time:system` | 未設定 | launch-time | EXPERIMENTAL | `TimeSpec` 明確值／系統：NEW_MAP 工作階段的起始時間。 |
| `/year` | `/year:<n>` 或 `/year:system` | 未設定 | launch-time | EXPERIMENTAL | `YearSpec`：NEW_MAP 工作階段的起始年份（1900..2200）。 |
| `/weather` | `/weather:<preset>` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.Preset`：安裝的 `weather` 資料夾中的檔案，以名稱（`Regen`）或路徑（`weather\Regen.owt`）指定；不存在者會在規劃時產生 `OL_E_WEATHER_INVALID`。世界啟動後立即透過 OMSI 自己的 `loadWeather` 載入；即時 METAR 保持關閉。執行階段驗證待完成。 |
| `/weather-icao` | `/weather-icao:<code>` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.Icao`：四字元 ICAO 測站的即時 METAR 天氣（否則為 `OL_E_WEATHER_INVALID`）。OMSI 會把測站儲存到 `options.cfg`，工作階段會將其還原。執行階段驗證待完成。 |
| `/weather-real` | `/weather-real` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.RealCurrent`：OMSI 已設定之測站的即時 METAR 天氣；若尚未設定測站，啟動會以 `OL_E_WEATHER_APPLY_FAILED` 失敗。`/weather`、`/weather-icao`、`/weather-real` 以最後出現者為準。任何起始天氣都會覆寫已儲存情境的天氣。 |

<a id="player-vehicle"></a>
### 玩家車輛

會被接受並依安裝內容解析，但 runtime 不會套用：每個已設定的欄位都為 `UNAVAILABLE`，並加入 `OL_E_CAPABILITY_UNAVAILABLE`（計畫 NOT RUNNABLE，結束代碼 `1`）。

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>`（`Vehicles\...\*.bus`） | 未設定 | launch-time | UNAVAILABLE | 最先解析（未知時為 `OL_E_VEHICLE_NOT_FOUND`）。 |
| `/repaint` | `/repaint:<id>` | 未設定 | launch-time | UNAVAILABLE | 只有與 `/vehicle` 一起時才會解析（`OL_E_REPAINT_NOT_FOUND`）。 |
| `/hof` | `/hof:<id>` | 未設定 | launch-time | UNAVAILABLE | 未知時為 `OL_E_HOF_NOT_FOUND`。 |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | 無 | launch-time | STABLE_BETA | 一項工作階段 [HOF 管理](hof-management.md)操作：`HofSourceKind` 與 `Source`。至少需要一個 `/hof-target`。會取代 `/spec` 的 `HofManagement`；與含有 `hofs` 的工作階段設定檔預設組合並用時：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。格式錯誤：結束代碼 `2`。 |
| `/hof-target` | `/hof-target:<vehicle identity or folder>`（可重複） | 無 | launch-time | STABLE_BETA | `Vehicles\` 之下的目標車輛。沒有 `/hof-source`：結束代碼 `2`。 |
| `/hof-file` | `/hof-file:<name.hof>`（可重複） | 所有檔案 | launch-time | STABLE_BETA | 將來源限制為這些檔名。沒有 `/hof-source`：結束代碼 `2`。 |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | launch-time | STABLE_BETA | `HofApplyMode`。沒有 `/hof-source`：結束代碼 `2`。 |
| `/module` | `/module:<id>`（可重複） | 無 | launch-time | EXPERIMENTAL | 為工作階段要求一個[執行階段模組](../../../reference/runtime-modules.md)（`LaunchSpec.Modules.Enable`，會加到 `/spec` 檔案的模組中）。無法載入的被要求模組會使計畫不可執行並帶有其 `OL_E_MODULE_*` 代碼（結束代碼 `1`）。其操作以 `/runtime:module.<id>.<operation>` 與 `/runtime-arg` 存取。若工作階段設定檔預設組合對它設定了 `enabled: false`：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |
| `/no-module` | `/no-module:<id>`（可重複） | 無 | launch-time | EXPERIMENTAL | 讓某個模組不加入工作階段，包括預設載入、由其地圖載入或由設定檔載入的模組（`LaunchSpec.Modules.Disable`）。若預設組合對它設定了 `enabled: true`：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。沒有值：結束代碼 `2`。 |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>`（可重複） | 無 | launch-time | EXPERIMENTAL | 設定模組設定（`LaunchSpec.Modules.Settings`）；優先於 `/spec` 檔案、設定檔與預設值。所選預設組合也設定的索引鍵：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。不是 `<id>.<key>=<value>`：結束代碼 `2`。無效值或未宣告的索引鍵：計畫診斷 `OL_E_MODULE_SETTING_INVALID`（結束代碼 `1`）。 |
| `/<module flag>` | `/<flag>:<value>`；布林設定單獨使用 `/<flag>` 表示 `true` | 無 | launch-time | EXPERIMENTAL | 已安裝模組為其某項設定所宣告的旗標（其資訊清單中的 `flag`；由 `/list:RuntimeModules` 列出）。在規劃工作階段時解析，等同 `/module-setting:<id>.<key>=<value>`。沒有任何已安裝模組宣告的旗標，或在啟動之外使用的旗標：`Unknown argument`，結束代碼 `2`。由兩個模組宣告的旗標：`OL_E_MODULE_SETTING_INVALID`，結束代碼 `2`。模組旗標絕不會取代內建旗標。 |
| `/fleet` | `/fleet:<n>` | 未設定 | launch-time | UNAVAILABLE | 車隊編號。 |
| `/registration` | `/registration:<text>` | 未設定 | launch-time | UNAVAILABLE | 車牌號碼。 |
| `/no-vehicle` | `/no-vehicle` | 關閉 | launch-time | STABLE_BETA | 清除種子（`/spec` 或設定檔）中的任何玩家車輛。無副作用。 |

<a id="configuration-overlays"></a>
### 設定 overlay

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>`（可重複；key 不區分大小寫） | 無 | launch-time | STABLE_BETA | 來自 `ConfigurationCatalog` 的語意 `options.cfg` overlay（暫時覆蓋）（例如 `graphics.maxFPS=60`、`traffic.randomVehicles=150`）。未知 key：`OL_E_UNKNOWN_SETTING`（結束代碼 `2`）；唯讀 key（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）：`OL_E_SETTING_NOT_WRITABLE`（結束代碼 `2`）；超出範圍或格式錯誤的值：建立 overlay 時為 `OL_E_INVALID_SETTING_VALUE`。overlay 是一種工作階段變更：會建立快照、在 OMSI 啟動前套用，並在停止時逐位元組還原（RV-005 `RUNTIME_VALIDATED`）。與所選設定檔中由預設組合（preset）擁有的 key 衝突：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |

<a id="splash-presentation"></a>
### 啟動畫面呈現

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`、`/splash:Native`、`/splash:Unset`（不區分大小寫） | `Managed` | launch-time | STABLE_BETA | `Managed`：套件內附的 640x480 24 位元 BMP 會複製一次到 `<root>\.omsilaunch\assets\splash`，並以交易方式 overlay `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<lang>.bmp`，之後精確還原（RV-006 `RUNTIME_VALIDATED`）。`Native`／`Unset`（別名）：不動 OMSI 檔案。缺少值：`/splash requires Unset, Native, or Managed`，結束代碼 `2`。 |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA`（亦可 `pt-BR`、`de`、`fr`、`en`；其他任何值都會退回 `ENG`） | `options.cfg` 中的 `[language]`，否則為 `ENG` | launch-time | STABLE_BETA | 選擇在地化的目標檔案。 |
| `/splash-assets` | `/splash-assets:<directory>`（相對路徑會解析到安裝根目錄之下） | `<root>\.omsilaunch\assets\splash`，否則為套件內附的組合 | launch-time | STABLE_BETA | 自訂資產目錄；必須包含 `ENG.bmp`，非英文語言時還需 `<lang>.bmp`。錯誤：`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`（在計畫中呈現為 `OL_E_SESSION_PRESENTATION_INVALID`；不可執行）。 |

### Internet Textures

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | launch-time | EXPERIMENTAL | `Native`：不變動。`Disabled`：抑制已建立設定檔的處理程序內下載器。`Override`：將指定的 `.itx` 設定檔 overlay 為 `Texture\standard.itx`；其中列出的每個 HTTP(S) 目標加上 `Texture\standard.ipr` 都會成為工作階段刪除項目（在工作階段期間移除，停止時還原）。缺少值：結束代碼 `2`。 |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | 無 | launch-time | EXPERIMENTAL | 使用 `Override` 時為必要（`OL_E_ITX_PROFILE_REQUIRED`，結束代碼 `2`）。`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`（必須是 URL／目標的成對行，且 URL 為 `http`／`https`）、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`（目標必須解析到 `Texture\` 之下，不得為根路徑、`..` 或重新剖析點）。 |

<a id="session-profiles"></a>
### 工作階段設定檔

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | 無 | launch-time | STABLE_BETA（編譯；離線 `OmsiLaunch.ProfileTests`） | 載入 `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`（請見[工作階段設定檔](session-profiles.md)）。需要 `/predefined-profile-index`（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`，結束代碼 `2`）。`new:` 區塊僅在 `/new` 時套用；`compatibility.maps` 對 `/new` 與 `/saved` 強制執行（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。與設定檔擁有之欄位衝突的明確旗標會以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 拒絕（`CliInput.RejectProfileConflicts`）：`new:` 區塊擁有地圖／進入點／日期／時間／年份／天氣時的對應旗標、預設組合擁有的 `/set` key、預設組合含 `presentation` 時的啟動畫面旗標、含 `internet-textures` 時的 Internet Textures 旗標，以及含 `behavior` 時的逾時旗標。 |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | 無 | launch-time | STABLE_BETA | 依 `index` 選擇預設組合。超出範圍：結束代碼 `2`。 |

<a id="launchspec-file"></a>
### LaunchSpec 檔案

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | 無 | launch-time | STABLE_BETA（載入器經離線測試；工作階段語意與旗標相同） | 載入 `LaunchSpec` JSON 檔案作為種子（請見 [LaunchSpec](launchspec.md)），並標記為要求啟動。規則（`LaunchSpecJson`）：檔案必須存在（`OL_E_SPEC_NOT_FOUND`，結束代碼 `6`）；最大 1 MiB（`OL_E_SPEC_TOO_LARGE`，結束代碼 `2`）；根節點必須是物件（`OL_E_SPEC_INVALID`）；屬性名稱不區分大小寫；允許 `//` 註解與結尾逗號；深度最多 32；每個未知屬性都會連同其 JSON 路徑被拒絕（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`，結束代碼 `2`）。 |

**優先順序**（`CliInput.BuildSpecAsync`）：預設值 → `/spec` 檔案 → `/predefined-profile`（取代 `Installation` 與 `World`，然後套用設定檔）→ 明確旗標。明確的安裝引數優先於規格中的 `RootPath`。`/no-vehicle` 會清除規格中的玩家車輛；`/vehicle` 及相關旗標則逐欄位合併到其中。`/set` key 合併到 `Environment.General`。`/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` 僅在有指定時才覆寫。`/startup-timeout` 與 `/shutdown-timeout` 僅在有指定時才覆寫；`Presentation.SuppressTrayIcon` 僅來自規格（沒有對應旗標）。診斷旗標會與規格中的 `Diagnostics` 進行 OR 合併。

<a id="content-discovery"></a>
### 內容探索

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`；類別為 `ContentQueryKind` 值 `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`（不區分大小寫） | 無 | 本機，不需工作階段 | STABLE_BETA | 對安裝執行 `DiscoverAsync`；`content.list` 封套，項目包含 `Identity`、`Kind`、`DisplayName`；結束代碼 `0`。`SessionProfiles` 以 `ListSessionProfilesAsync` 回應（`session-profiles.list` 封套），`RuntimeModules` 以 `ListRuntimeModulesAsync` 回應（`runtime-modules.list` 封套：每個模組的 id、版本、啟用方式、操作、有效性，以及其設定的型別、預設值與旗標）。未知類別：`Unknown discovery category`，結束代碼 `2`。會略過重新剖析點（連接點／符號連結），OMSI 檔案以 Windows-1252 讀取。 |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | 無 | 本機 | STABLE_BETA | 轉送給 `Entrypoints` 以外每個類別的範圍；`Entrypoints` 使用 `/map` 作為範圍。 |

<a id="timeouts-and-observation"></a>
### 逾時與觀察

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` 秒 | 規格／設定檔的值，否則為 `180` | launch-time | STABLE_BETA | `Behavior.StartupTimeoutSeconds`。擁有者等待 `Running` 的時間為此值加 5 s；`OL_E_STARTUP_TIMEOUT` 會以結束代碼 `1` 結束工作階段。 |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` 秒 | 規格／設定檔的值，否則為 `30` | launch-time | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`：搭配 `/cooperative-shutdown` 時，監督程式在終止 OMSI 前等待其結束的時間；未搭配時，OMSI 會立即被終止，此值沒有作用。 |
| `/cooperative-shutdown` | `/cooperative-shutdown` | 關閉 | launch-time | EXPERIMENTAL | `Behavior.CooperativeShutdown`：結束執行中的工作階段時，會要求 OMSI 透過其自己的結束常式（使用者確認離開後執行的那一個：儲存 `options.cfg` 與地圖的 `laststn.osn`，然後關閉）結束，並在終止它之前等待 `ShutdownTimeoutSeconds`。在完成執行階段驗證之前為選用；當 OMSI 必須被終止時為 `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`。無論哪種情況，工作階段的檔案都會在結束後還原。 |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | 無（執行到 OMSI 結束或收到停止要求為止） | runtime（擁有者） | STABLE_BETA | 執行階段的上限：處於 `Running` 達 `n` 秒後即要求標準停止。系統匣或管道停止、或 OMSI 結束，會使其提早結束。`0` 會在 `Running` 後立即停止。 |

<a id="recovery"></a>
### 復原

| 旗標 | 語法與值 | 預設 | 階段 | 穩定性 | 行為 |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | 關閉 | 本機 | STABLE_BETA | 回報 `<root>\.omsilaunch\journal.json` 是否待處理（`pending`），永不還原；結束代碼 `0`。會取得安裝租用：擁有者持有時為 `OL_E_INSTALLATION_BUSY`（結束代碼 `7`）。 |
| `/recover` | `/recover` | 關閉 | 本機 | STABLE_BETA | 還原待處理的日誌（備份會先依快照 SHA-256 驗證；`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED` 會在 `diagnostics` 中回報）。沒有待處理項目或還原完成時結束代碼 `0`；日誌原本待處理且仍然存在時為 `8`。當日誌記錄的 OMSI 處理程序（PID、建立時間、exe 路徑）仍存活，或對於已超過 `HandoffCreated` 但沒有 PID 的日誌、該根目錄中有任何 `Omsi.exe` 仍存活時，會以 `OL_E_INSTALLATION_BUSY` 拒絕。每次工作階段啟動在讀取安裝前都會自動執行相同的復原。 |
| `/clear-module-data` | `/clear-module-data:<module id>` | 無 | 本機，不需工作階段 | EXPERIMENTAL | 刪除模組的資料目錄 `<root>\.omsilaunch\module-data\<id>`，不刪除其他任何內容；`module-data.clear` 封套，包含 `module` 與 `cleared`（沒有可刪除的內容時為 `false`）；結束代碼 `0`。會取得安裝租約：當工作階段擁有該安裝時為 `OL_E_INSTALLATION_BUSY`（結束代碼 `7`）。透過連結到達的 module-data 資料夾會被拒絕（`OL_E_MODULE_INTEGRITY`）；其內的連結會當作連結移除。 |

<a id="output-formats"></a>
## 輸出格式

- **成功封套**（`CliInput.WriteEnvelope`，搭配 `--json`）：`{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`，含縮排。轉送的 `session status` 與 `events read` 回覆，若為了放入控制訊框（frame）而省略了較舊的事件，會加上 `metadata` 成員（`events_dropped_count`，請見[本機控制](local-control.md)）。未使用 `--json` 時只會以縮排 JSON 印出 `<object>`，若有事件被捨棄，其後接著 `Note: <n> older events were omitted to fit the control frame.`。
- **錯誤封套**（`CliInput.WriteError`，搭配 `--json`）：`{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`。未使用 `--json` 時：單行 `OL_E_<CODE>: message`。類別：`invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。在 `OmsiLaunchW.exe` 下，相同的代碼與訊息會顯示在訊息方塊中。
- **計畫與狀態**（`CliInput.Write`）：`SessionPlan`、`SessionStatus` 與 `RuntimeCommandResult` 記錄會以縮排 JSON 印出，**不含**封套。未使用 `--json` 時，計畫會摘要為 `Plan: READY profile=Omsi23004_692EBFBF` 或 `Plan: NOT RUNNABLE profile=...`；其他記錄仍以 JSON 印出。列舉值序列化為整數（`SessionState.Running` 為 `14`、`Completed` 為 `18`、`Failed` 為 `19`）。
- 封套中使用的命令名稱：`silent`、`version`、`capabilities`、`help`、`profiles`、`detect`、`recover`、`content.list`、`session`、`session.status`、`session.stop`、`events.read`、`events.watch`、`events watch`、`installation`、`cli`、`session profile`，以及轉送之執行階段命令的執行階段操作 ID。
- 在 `OmsiLaunchW.exe`（`OMSILAUNCH_WINDOWS_HOST=1`）下，除非指定 `--json`，否則不會寫入任何主控台輸出。

<a id="errors-per-command"></a>
## 各命令的錯誤

| 命令 | 常見錯誤碼 | 結束代碼 |
|---|---|---|
| 任何解析失敗 | `OL_E_INVALID_ARGUMENT`、工作階段設定檔代碼（`OL_E_SESSION_PROFILE_*`） | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| 用戶端路由、`/runtime`（用戶端） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（`2`）；`OL_E_NO_ACTIVE_SESSION`（`4`）；擁有者回傳的 `OL_E_CONTROL_*`、`OL_E_RUNTIME_*`，例如 `OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_SESSION_NOT_RUNNING`（`7`） | `2`、`4`、`7` |
| `session status`、`session stop`、`events read`、`events watch` | `OL_E_NO_ACTIVE_SESSION`（`4`）；`OL_E_CONTROL_SESSION_MISMATCH`、`OL_E_CONTROL_PROTOCOL`、`OL_E_CONTROL_FAILED`（`7`） | `4`、`7` |
| 擁有者預檢 | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`、`/recover` | `OL_E_INSTALLATION_BUSY`（`7`）；`OL_E_RECOVERY_*`、`OL_E_RESTORE_FAILED`（`8`）；待處理但未復原（`8`） | `7`、`8` |
| `/list` | 未知類別（`2`）；`OL_E_INSTALLATION_NOT_FOUND`／缺少目錄（`6`） | `2`、`6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND`（`6`）；`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`（`2`） | `2`、`6` |
| `/set` | `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`、`/validate`、啟動 | 計畫診斷訊息：`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（已安裝的外掛程式封閉集合）、`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_SESSION_PRESENTATION_INVALID`、`OL_E_RUNTIME_ARTIFACT_MISSING`、`plugin.integrity.reference`（資訊性） | `1` |
| 工作階段啟動 | `OL_E_PLAN_NOT_RUNNABLE`（啟動時重新規劃，`1`）；`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_RELEASE_MANIFEST_INVALID`（通常由規劃以計畫診斷訊息回報，結束代碼 `1`；僅在外掛程式檔案於規劃與啟動之間變更時為 `7`）、`OL_E_INSTALLATION_BUSY`（`7`）；`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_EXITED_EARLY`、`OL_E_STARTUP_TIMEOUT`、`OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_PLUGIN_NOT_LOADED`（工作階段 `Failed`，`1`） | `1`、`7` |
| 任何地方的未處理例外狀況 | 由 `CliProgram.Classify` 分類（請見[結束代碼](exit-codes.md)） | `2`..`10` |

<a id="environment"></a>
## 環境

| 變數 | 設定者 | 效果 |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`：抑制主控台輸出、失敗以訊息方塊顯示、`/silent` 不會再次委派。 |

<a id="see-also"></a>
## 另請參閱

[CLI 範例](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [結束代碼](exit-codes.md) · [錯誤](errors.md) · [本機控制](local-control.md) · [Windows 系統匣](windows-tray.md) · [執行階段控制](runtime-control.md) · [功能](capabilities.md) · [LaunchSpec](launchspec.md) · [工作階段設定檔](session-profiles.md) · [封裝](packaging.md) · [相容性](compatibility.md) · [已知限制](known-limitations.md) · [公開 API](public-api.md)
