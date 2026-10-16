# Windows 系統匣指示器

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/windows-tray.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

每個獨立的擁有者工作階段（以 `OmsiLaunch.exe` 或 `OmsiLaunchW.exe` 啟動）都會在通知區域顯示一個圖示，用來回報工作階段狀態，並讓使用者結束工作階段。本頁規範此指示器的實作，涵蓋 `tools\OmsiLaunch.Cli\WindowsHost.cs` 中的 `SessionTrayIndicator`、`StatusWindow` 與 `StopConfirmationWindow`、唯讀呈現器 `SessionStatusPresenter`（`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`）與字串登錄 `WindowsUiStrings`（`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`），以及 `WindowsHost` 的 `OmsiLaunchW.exe` 失敗對話方塊。系統匣只是一個呈現配接器：它既不擁有 OMSI，也不負責復原，其停止動作所觸發的是與 `session stop` 相同的擁有者標準停止路徑（請參閱 [CLI 參考](cli.md)與[本機控制](local-control.md)）。

<a id="when-the-icon-exists"></a>
## 圖示何時存在

| 步驟 | 行為 |
|---|---|
| 建立 | 在 `StartSessionAsync` 回傳後、工作階段達到 `Running` 之前立即建立，除非已設定 `SuppressTrayIcon`。因此圖示在 `StartingProcess`、`WaitingForPlugin`、`StartingWorld` 與 `EnteringGameplay` 期間都存在。 |
| 啟動時間上限 | UI 執行緒（`STA`、背景執行緒、名稱為 `OmsiLaunch tray`）必須在 2 s 內發布圖示。否則，或在建立期間發生任何例外時，指示器會被處置，工作階段會在**沒有**圖示的情況下繼續；並記錄 `startup-timeout` 或該例外。啟動緩慢絕不會留下孤立的可見圖示。 |
| 移除 | 在擁有者的 `finally` 區塊中、工作階段完成或失敗之後、`CloseAsync` 之前移除。處置動作會將關閉作業投遞到 UI 執行緒（關閉選單、確認對話方塊與狀態視窗，然後結束訊息迴圈），最多等待該執行緒 2 s 完成聯結（超過時記錄 `dispose-timeout`），接著隱藏並處置 `NotifyIcon`。 |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon`（一個 `LaunchSpec` 欄位，`Presentation.SuppressTrayIcon`，預設為 `false`）。可透過 `/spec` 與 API 設定；沒有對應的 CLI 旗標。自行呈現工作階段操作介面的整合者會將其設為 `true`；工作階段的其他部分都不會改變。 |
| 主機 | `OmsiLaunch.exe`（主控台）與 `OmsiLaunchW.exe`（Windows 子系統）都會顯示圖示；以 `/silent` 啟動的 `OmsiLaunchW.exe` 工作階段沒有其他可見的介面。 |

<a id="icon-and-tooltip"></a>
## 圖示與工具提示

- 圖示：與執行中執行檔相關聯的圖示（`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`，即內嵌的 OmsiLaunch 圖示），備援為 `SystemIcons.Application`。
- 工具提示文字：`Tray.Running`（`OmsiLaunch is running`，意為「OmsiLaunch 正在執行」）。停止期間文字不會改變（目前還沒有表示「正在停止」的字串；圖示會維持原樣，直到擁有者將其移除）。
- 已啟用視覺化樣式（`Application.EnableVisualStyles`）。

<a id="interaction"></a>
## 互動

| Action | 結果 |
|---|---|
| 按一下右鍵 | 將系統匣視窗設為前景視窗（通知圖示選單的必要條件；若不這樣做，當 OMSI 位於前景時，選單可能忽略點按且永遠不會關閉），然後在實際游標位置（`Cursor.Position`，而非事件座標，因為 `NotifyIcon` 對 shell 所承載的事件可能回報 `(0,0)`）開啟右鍵選單。選單會被限制在游標所在螢幕的工作區域內。 |
| 按兩下 | 開啟狀態視窗（與 `Status` 選單項目相同）。 |
| 按一下左鍵 | 沒有動作。 |
| 選單項目 `Status`（`Tray.Status`） | 開啟（若已開啟則啟用）唯讀狀態視窗。 |
| 分隔線 | |
| 選單項目 `End session`（`Tray.EndSession`，協助工具描述為 `Tray.EndSessionDescription`） | 開啟確認對話方塊。 |

<a id="status-window-read-only-snapshot"></a>
### 狀態視窗（唯讀快照）

由 `Status` 選單項目或在圖示上按兩下開啟（`SessionTrayIndicator.ShowStatus`）。它是一個固定大小、置中、自動調整尺寸的對話方塊，不會出現在工作列上，只有一個 `Close` 按鈕（`Status.Close`；按 `Escape` 也可關閉）。當視窗已開啟時再次選擇 `Status`，只會啟用該視窗而不會重建它（它保留第一次開啟時的快照）。建立視窗失敗時會寫入 `tray-host.log`；不會影響工作階段。

**它是快照，而非即時檢視。** `SessionStatusPresenter.Create(plan, status, ui)` 只在視窗開啟時執行一次：它讀取工作階段已解析的 `SessionPlan`（實際規劃的 spec）以及一個 `SessionStatus`（僅其 `State`）。視窗保持開啟期間不會重新整理任何內容，也從不查詢 OMSI（沒有執行階段操作，也沒有遙測值）。若要查看較新的狀態，請關閉後重新開啟。

視窗標題與標題列文字相同：當 `SessionStatus.State` 為 `Running` 時為 `Status.SessionRunning`（`Session is running`，意為「工作階段正在執行」），否則為原始的 `SessionState` 名稱（例如在啟動期間開啟時為 `WaitingForPlugin`，因為圖示在 `Running` 之前就已存在）。`Status.Title`（`OmsiLaunch session status`）雖在字串表中定義，但此版本並未使用。

各區段依下列順序出現，沒有欄位的區段會省略。所有值都來自已規劃的 `LaunchSpec`／`SessionPlan`，絕不來自 OMSI。

| 區段（英文標籤） | 欄位（英文標籤） | 顯示時機 | 值 | 來源（公開對應項） |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | 一律顯示 | `New session`（`WorldMode.NewMap`）、`Saved situation`（`WorldMode.SavedSituation`）、`Last map state`（任何其他模式；由於 `LastMapState` 不可執行，實際上不會出現） | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | 計畫已解析出 `map` 內容識別 | 地圖的 `DisplayName`（地圖目錄名稱，例如 `Grundorf`），否則為識別的檔名（不含副檔名） | `SessionPlan.ResolvedContent` 中 `Kind = "map"` 的項目（NEW_MAP）；`DiscoverAsync(Maps)` 提供相同的 `DisplayName` |
| `Session` | `Situation` | `SavedSituation` 且具有情境識別 | 檔名（不含副檔名）（`situations\Linie 5.osn` → `Linie 5`） | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | 要求了進入點 | 若已設定則為進入點識別（此版本中從不可執行），否則為以整數表示的呈現索引（`1`） | `Spec.World.EntrypointIdentity`／`PresentedEntrypointIndex` |
| `Session profile` | `Profile` | 使用了工作階段設定檔（`/predefined-profile`） | 設定檔的 `name` | `Spec.SessionProfile.Name`（`SessionProfileMetadata`） |
| `Session profile` | `Preset` | 同上，且預設組合具有名稱時 | 預設組合的 `name` | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`、`Time`、`Weather` | 要求了明確／系統的日期、時間或天氣 | `DD/MM/YYYY` 或 `System`；`HH:MM:SS` 或 `System`；ICAO 代碼、預設組合名稱或 `Real/current` | `Spec.Date`、`Spec.Time`、`Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`、`Repaint`、`HOF`、`Fleet number`、`Registration` | 要求了玩家車輛欄位 | 所要求的識別／值 | `Spec.PlayerVehicle` |
| `Configuration` | 每個語意設定一個欄位 | 已設定某項設定（`/set`、設定檔 `settings`、`LaunchSpec.Environment.*`），且 `ConfigurationCatalog` 認得該設定 | 所要求的值；以 `Percent` 結尾的鍵會附加 `%`，以 `DistanceMeters` 結尾的鍵會附加 ` m`。標籤為設定鍵中以點分隔的各部分首字母大寫後的結果（`graphics.maxFPS` → `Graphics MaxFPS`） | `Spec.Environment.*`；相同的值即為計畫的 `PlannedMutations` |
| `Presentation` | `Splash` | 一律顯示 | `Managed` 或 `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | 一律顯示 | `Original OMSI`（`Native`）、`Disabled`、`Override` | `Spec.EffectiveInternetTextures.Mode` |

`Environment` 區段會在要求了日期、時間、年份或天氣的執行中 Beta 4 工作階段出現（這些要求屬於實驗性，請參閱[已知限制](known-limitations.md)）。`Vehicle` 區段絕不會出現在執行中的工作階段：要求任何玩家車輛欄位都會使計畫不可執行，因此不會啟動此類工作階段。它是為未來組建而存在，並由離線呈現器測試涵蓋。

執行階段證據（pt-BR UI，執行階段收尾 `T01`）：一個 NEW_MAP Grundorf 工作階段顯示 `Sessão em execução`；`Sessão`：`Modo: Nova sessão`、`Mapa: Grundorf`、`Ponto de entrada: 1`；`Apresentação`：`Splash: Gerenciado`、`Texturas da internet: OMSI original`。

不透過系統匣，工具也能取得相同的資料：透過[本機控制平面](local-control.md)執行 `session status` 可取得 `SessionId`、`State`、`Diagnostics` 與 `RuntimeEvents`（即時），而擁有者的 `/plan --json` 輸出（或 `PlanSessionAsync`）則提供視窗所摘要的已規劃 spec、已解析內容與已規劃的變更。

<a id="end-session-with-confirmation"></a>
### 結束工作階段（需確認）

1. `StopConfirmationWindow`：標題為 `End session?`（意為「要結束工作階段嗎？」），訊息為 `OMSI 2 will be closed and the OmsiLaunch managed session will end.`（意為 OMSI 2 將被關閉，OmsiLaunch 所管理的工作階段將結束），按鈕為 `End session`（預設按鈕，`DialogResult.OK`）與 `Cancel`（`Escape`）。對話方塊開啟期間若再次要求，會啟用現有對話方塊，而不會再疊加一個。
2. 按下 `OK` 時，系統匣會呼叫 `requestCanonicalStop`，完成擁有者的 `controlStopped` 訊號；擁有者接著呼叫 `StopAsync`：以 `TerminateProcess` 終止 OMSI，並還原工作階段擁有的每一個檔案。系統匣本身絕不會終止 OMSI。
3. 若該要求擲回例外，錯誤會被記錄，並顯示 `Stop.Failed`（`The session could not be ended. OMSI and its managed session remain active.`，意為無法結束工作階段，OMSI 及其受管理的工作階段仍在運作）。
4. 系統匣不會確認成功；當擁有者完成還原並處置指示器時，圖示便會消失（執行階段收尾 `T01`：擁有者在確認 `End session` 後 607 ms 結束）。
5. `Cancel`（或關閉對話方塊）不會執行任何動作：工作階段繼續執行（`T01`）。
6. 若在狀態視窗或確認對話方塊開啟時，有來自其他來源的停止（`session stop`、Ctrl+C、`/observe-seconds`、OMSI 結束），這些視窗會作為指示器處置的一部分而被關閉；擁有者不會等待使用者（`T02`：在兩個視窗都開啟的情況下，擁有者於管道停止後 725 ms 結束）。

<a id="explorer-restart"></a>
## 檔案總管重新啟動

`TrayWindow` 是一個隱藏的原生視窗，會註冊 `TaskbarCreated` 視窗訊息。當檔案總管（shell）重新啟動時，它會廣播該訊息，指示器便會重新加入圖示（`Visible = false; Visible = true`）。執行階段收尾 `T01`：在 `explorer.exe` 被終止並由 Windows 重新啟動後，圖示回到了 `Shell_TrayWnd` 中，選單與狀態視窗也持續正常運作。

<a id="localization"></a>
## 當地語系化

`WindowsUiStrings.Resolve` 依循 **Windows UI 文化特性**（`CultureInfo.CurrentUICulture`），絕不依循 OMSI 內容語言或工作階段設定檔的語言。解析順序：完整文化特性名稱，其次為兩個字母的語言代碼，最後為英文。若某個翻譯缺少某個鍵，該鍵一律退回英文。

| 文化特性鍵 | 語言 |
|---|---|
| `en`、`en-US`、`en-GB` | 英文（預設與備援） |
| `pt-BR` | 巴西葡萄牙文。`pt-PT`（以及單獨的 `pt`）刻意退回英文。 |
| `de`、`de-DE` | 德文 |
| `fr`、`fr-FR` | 法文 |
| `pl`、`pl-PL` | 波蘭文 |

當地語系化字串涵蓋工具提示、兩個選單項目、狀態視窗（標題、區段標題、欄位標籤、`Close`）、模式與呈現相關的值，以及確認對話方塊。詞彙表維護於 `docs\windows-ui-localization.md`；離線測試 `windows-ui.localization-and-status`（`tests\OmsiLaunch.WindowsUiTests`）會驗證解析與呈現器。

<a id="omsilaunchwexe-failure-dialogs"></a>
## `OmsiLaunchW.exe` 失敗對話方塊

當 `OMSILAUNCH_WINDOWS_HOST=1`（由 `OmsiLaunchW.exe` 設定）時，`WindowsHost.ShowFailure` 會以一個標題為 `OmsiLaunch`（錯誤圖示）的強制回應訊息方塊取代主控台錯誤輸出，內容依序為：`<message>`、一個空白行、`Code: OL_E_...`、一個空白行、`See .omsilaunch\diagnostics for details.`。每次 `CliInput.WriteError`（參數錯誤、`OL_E_NO_ACTIVE_SESSION`、`OL_E_SESSION_ALREADY_ACTIVE`、已分類的例外）都會顯示它；當啟動計畫不可執行時（備援訊息 `The session plan is not runnable.`；文件稽核 BUG-06），以及當工作階段未能達到 `Running` 時（`The OMSI session did not reach gameplay.` 加上最後一個 `OL_E_` 診斷訊息，若沒有則為 `OL_E_SESSION_START_FAILED`）也會顯示。`OmsiLaunchW.exe` 的完整行為請參閱 [OmsiLaunchW.exe 參考](omsilaunchw.md)。在 `OmsiLaunch.exe` 下，同一個函式不執行任何動作。.NET 主機啟動失敗（shim 代碼 `100`..`106`）由原生 shim 本身顯示；請參閱[結束代碼](exit-codes.md)。

<a id="log-location"></a>
## 記錄檔位置

`<root>\.omsilaunch\diagnostics\tray-host.log`，每個項目一行：ISO-8601 UTC 時間戳記、一個 Tab 字元，然後是項目內容。項目包括：`created`、`removed`、`startup-timeout`、`startup-cancelled`（處置動作與啟動發生競爭，因而略過迴圈）、`dispose-timeout`，以及 UI 失敗的完整例外文字。記錄為盡力而為，絕不擲回例外。工作階段主機記錄檔（`<sessionId>-host.log`）由擁有者寫入同一目錄；系統匣記錄檔不以工作階段為前綴，也不受 50 個工作階段保留上限的清除影響。

<a id="lifecycle-guarantees"></a>
## 生命週期保證

- 系統匣絕不擁有工作階段：它無法啟動 OMSI、無法還原檔案，也無法繞過擁有者的停止路徑。
- 每一條擁有者結束路徑（正常完成、OMSI 結束、Ctrl+C、關閉主控台、管道停止、例外、`/observe-seconds`）都會在 `CloseAsync` 之前處置指示器，因此除非擁有者處理程序被直接強制終止，否則不會有圖示比其工作階段存在得更久（Windows 會在下一次滑鼠停留時移除孤立的圖示）。
- 建立與處置會在鎖定下序列化：在競爭中勝出的處置動作會使 UI 執行緒略過其訊息迴圈並立即清理。
- 所有 Windows Forms 工作都在專用的 STA 執行緒上進行；其他執行緒只能透過一個隱藏的封送處理控制項向其投遞工作。

<a id="residual-caveats-from-the-code-comments"></a>
## 殘餘注意事項（來自程式碼註解）

- 沒有表示「正在停止」的工具提示文字；在圖示被移除之前，它一直顯示 `OmsiLaunch is running`。
- `NotifyIcon` 對 shell 所承載的事件可能回報 `(0,0)` 滑鼠座標；因此改為讀取游標位置。
- 確認對話方塊為強制回應時，系統匣訊息仍會到達；不會疊加第二個確認對話方塊。
- 若 UI 執行緒未在 2 s 的處置時間上限內完成，擁有者會不等待而繼續（`dispose-timeout`）。
- 狀態視窗是開啟時所取得之已規劃值的快照；它不會重新整理，也從不讀取 OMSI。

<a id="runtime-evidence"></a>
## 執行階段證據

於執行階段收尾回合中，在授權的安裝上以實際工作階段觀察到（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`，pt-BR Windows UI；請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)）：

- 在 `OmsiLaunchW.exe` 下，圖示註冊於實際的通知區域（`Shell_TrayWnd`），並在還原後移除（`T01`..`T04`）。
- 經確認的「End session」會驅動標準停止與精確還原；`Cancel` 會讓工作階段繼續執行（`T01`、`T03`）。
- 檔案總管重新啟動後圖示會重新建立（`TaskbarCreated`、`T01`）。
- 當停止在狀態視窗與確認視窗開啟時到達，擁有者會關閉這些視窗（`T02`）。
- `OmsiLaunchW.exe` 針對參數錯誤（`OL_E_INVALID_ARGUMENT`）、沒有作用中工作階段（`OL_E_NO_ACTIVE_SESSION`）以及在進入遊戲前失敗的工作階段（`OL_E_WORLD_START_FAILED`）顯示的失敗對話方塊（`T04`）。對於最後一種情況，對話方塊會以外掛程式的失敗酬載作為其訊息。
- `/silent` 會分離執行：啟動器會返回，而 Windows 主機則持續維持工作階段（`T04`）。
- 關閉主控台時的 4 s `ProcessExit` 時間上限（`L04`，主控台擁有者）。

未產生：啟動程式 shim 的結束代碼對話方塊（`100`..`106`）。
