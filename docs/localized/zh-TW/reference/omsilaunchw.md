# OmsiLaunchW.exe 參考

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> 本頁為 OmsiLaunch 0.1.0-beta3 [英文原始頁面](../../../reference/omsilaunchw.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

`OmsiLaunchW.exe` 是 OmsiLaunch 控制器的 Windows 子系統（GUI）主機。它接受與 `OmsiLaunch.exe` 相同的命令列，並執行相同的控制器程式碼（`OmsiLaunch.Controller.dll`）。唯一的差異在於回報方式：沒有主控台視窗，失敗會以訊息方塊顯示，執行中的工作階段只能透過其[系統匣圖示](windows-tray.md)看到。

權威來源：`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp`（原生 shim）、`tools\OmsiLaunch.Cli\WindowsHost.cs`（`WindowsHost`、`SessionTrayIndicator`）以及 `tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Write*`）。

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## OmsiLaunch.exe 與 OmsiLaunchW.exe 比較

| 面向 | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| 子系統 | 主控台。從檔案總管啟動時會開啟主控台視窗。 | Windows（GUI）。沒有主控台視窗。 |
| 原生 shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| 環境 | 不變更 | 在 .NET 啟動前，為控制器處理程序設定 `OMSILAUNCH_WINDOWS_HOST=1` |
| 引數 | 以 `CommandLineToArgvW` 切分為權杖，原封不動傳給控制器 | 相同，因此兩個主機接受完全相同的旗標與命令（[CLI 參考](cli.md)） |
| 文字輸出 | 寫入 stdout | 不輸出，除非指定 `--json`（此時 JSON 封套會寫入 stdout，呼叫端可加以重新導向） |
| 錯誤 | `OL_E_...: message` 或 JSON 錯誤封套 | 相同的主控台輸出規則，**另外**每個錯誤都會顯示訊息方塊（請參閱[失敗對話方塊](#failure-dialogs)） |
| `/silent` | 以其餘引數啟動 `OmsiLaunchW.exe` 並回傳 `0` | 忽略：命令本來就在 Windows 主機中執行 |
| 系統匣圖示 | 擁有者工作階段時顯示 | 擁有者工作階段時顯示 |
| 停止途徑 | 系統匣、`session stop`、OMSI 結束、`/observe-seconds`、Ctrl+C、關閉主控台 | 系統匣、`session stop`、OMSI 結束、`/observe-seconds`（沒有主控台，因此 Ctrl+C 與關閉主控台不適用） |
| 結束代碼 | [`PublicExitCode`](exit-codes.md) `0`..`10`，shim 代碼 `100`..`106` | 相同的代碼 |

<a id="how-it-starts"></a>
## 啟動方式

1. shim 解析自身路徑（`GetModuleFileNameW`），並預期 `OmsiLaunch.Controller.dll` 位於同一目錄。
2. 將命令列切分為權杖（`CommandLineToArgvW`），並設定 `OMSILAUNCH_WINDOWS_HOST=1`。
3. 透過套件隨附的 `nethost.dll` 尋找 `hostfxr`、將其載入、以引數初始化控制器（控制器路徑不屬於 CLI 剖析器看到的引數清單），然後執行控制器。
4. shim 原封不動地回傳控制器的結束代碼。

若控制器執行之前的任何步驟失敗，shim 會顯示標題為 `OmsiLaunch`、文字為 `OmsiLaunch could not start the .NET host (code N).` 的訊息方塊，並以該代碼結束：

| 代碼 | 失敗的步驟 |
| --- | --- |
| `100` | 無法解析執行檔路徑 |
| `101` | 無法將命令列切分為權杖 |
| `102` | `hostfxr` 位置探查失敗（通常是：未安裝 .NET 6 x64 runtime） |
| `103` | 無法取得 `hostfxr` 路徑 |
| `104` | 無法載入 `hostfxr` |
| `105` | 缺少必要的 `hostfxr` 匯出函式 |
| `106` | 無法初始化受控主機（例如缺少 `OmsiLaunch.Controller.dll` 或其 runtime 組態，或未安裝 Windows Desktop runtime） |

`OmsiLaunch.exe` 使用相同的代碼表，但不會印出任何內容。這些對話方塊尚未在執行階段實際產生過（請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)）。

<a id="starting-it"></a>
## 如何啟動

直接啟動，可從捷徑、指令碼或其他程式：

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

透過 `OmsiLaunch.exe` 搭配 `/silent`：

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

請將執行檔放在 OMSI 2 安裝中（即套件配置，請參閱[封裝](packaging.md)）；未提供安裝引數時，安裝即為包含該執行檔的目錄。明確指定的安裝以第一個引數傳入，與 `OmsiLaunch.exe` 相同（`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`）。

由於它是 GUI 程式，`cmd.exe` 與檔案總管不會等待它結束。若要在指令碼中等待並讀取結束代碼，請在 `cmd.exe` 中使用 `start /wait OmsiLaunchW.exe ...`，或在 PowerShell 中使用 `Start-Process -Wait -PassThru`：

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## /silent 委派

`OmsiLaunch.exe ... /silent`（或 `--silent`）在並非已於 `OmsiLaunchW.exe` 下執行時，會執行下列動作（`CliProgram.RunAsync`、`CliProgram.SilentDelegation`）：

1. 在 `OmsiLaunch.exe` 所在目錄中尋找 `OmsiLaunchW.exe`。若找不到：`OL_E_WINDOWS_HOST_MISSING`，結束代碼 `7`。
2. 透過 `ShellExecute`（`UseShellExecute = true`）啟動 `OmsiLaunchW.exe`，使用目前目錄，並依原始順序傳入 `/silent`／`--silent` 以外的所有引數。`ShellExecute` 不會將呼叫端的 handle（控制代碼）傳給新處理程序，因此擷取 `OmsiLaunch.exe /silent` 輸出的呼叫端不會在整個工作階段期間遭到封鎖（執行階段結案項目 BUG-03）。若沒有回傳處理程序：`OL_E_WINDOWS_HOST_START_FAILED`，結束代碼 `7`。
3. 寫入 `silent` 封套並立即以 `0` 結束：

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

結束代碼 `0` 僅表示 `OmsiLaunchW.exe` 已啟動。工作階段的結果（規劃錯誤、已有作用中的擁有者、啟動失敗）由 `OmsiLaunchW.exe` 以其自己的對話方塊與診斷資訊回報，之後兩個處理程序彼此獨立：`OmsiLaunch.exe` 已結束，而 `OmsiLaunchW.exe` 是工作階段擁有者。請使用 `OmsiLaunch.exe session status` 查看工作階段。

`/silent` 會在其他所有命令之前套用，因此 `OmsiLaunch.exe /silent session status` 也會在 `OmsiLaunchW.exe` 內執行 `session status`，而其輸出會被抑制。請只在啟動時使用 `/silent`。

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## 各命令在 OmsiLaunchW.exe 下的行為

| 命令列 | 結果 |
| --- | --- |
| 沒有引數 | 靜默執行 `detect` 並以 `0` 結束（不顯示任何內容） |
| 計畫可執行且沒有擁有者的啟動（`/new`、`/saved:...`、`/spec:...`、工作階段設定檔） | 成為工作階段擁有者：啟動與執行期間顯示系統匣圖示；工作階段結束時結束（完成為 `0`，失敗為 `1`） |
| 計畫不可執行的啟動 | 顯示含有計畫最後一個 `OL_E_` 診斷訊息的對話方塊（備援為 `The session plan is not runnable.`／`OL_E_SESSION_START_FAILED`），結束代碼 `1`（文件稽核 BUG-06；修正前會靜默結束） |
| 該安裝已有作用中的擁有者時的啟動 | `OL_E_SESSION_ALREADY_ACTIVE` 對話方塊，結束代碼 `7`；執行中的工作階段不受影響 |
| 未能進入 `Running` 的啟動 | 顯示含有最後一個 `OL_E_` 診斷訊息的對話方塊 `The OMSI session did not reach gameplay.`，結束代碼 `1`。對話方塊開啟期間，工作階段監督程式會終止 OMSI 並還原檔案；對話方塊關閉且 `CloseAsync` 完成後，處理程序才會結束 |
| 無效引數、未知旗標或無效的工作階段設定檔 | 顯示含有 `OL_E_INVALID_ARGUMENT` 或 `OL_E_SESSION_PROFILE_*` 代碼的對話方塊，結束代碼 `2` |
| 沒有擁有者時的用戶端命令（`session status`、`session stop`、`events read`、`time get`、`/runtime:...`） | `OL_E_NO_ACTIVE_SESSION` 對話方塊，結束代碼 `4` |
| 遭擁有者拒絕的用戶端命令 | 顯示含有拒絕代碼（例如 `OL_E_CONTROL_SESSION_MISMATCH` 或執行階段錯誤碼）的對話方塊，結束代碼 `7`（未知操作或缺少引數時為 `2`） |
| 成功的用戶端或探索命令（`session status`、`/list:...`、`help`、`capabilities`、`/version`、`/recovery-status`） | 除非指定 `--json` 且 stdout 已重新導向，否則沒有可見輸出；結束代碼與 `OmsiLaunch.exe` 相同 |
| `/plan` 或 `/validate` | 不顯示對話方塊，即使計畫不可執行亦然；結束代碼 `0` 或 `1` |
| 其他任何控制器失敗 | 顯示含有分類後 `OL_E_` 代碼的對話方塊；結束代碼依[結束代碼](exit-codes.md)而定 |

<a id="failure-dialogs"></a>
## 失敗對話方塊

`OmsiLaunch.exe` 會印出的每個錯誤，也都會以強制回應訊息方塊（`WindowsHost.ShowFailure`）顯示，即使指定了 `--json` 亦然：

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` 為錯誤訊息；若為工作階段失敗，則為最後一個 `OL_E_` 診斷訊息的訊息內容。已知限制：對於由外掛程式回報的失敗，訊息會是外掛程式的原始失敗承載資料（例如 `{"name":"world.failed",...}`）；`Code:` 那一行是正確的（請參閱[已知限制](known-limitations.md)）。此對話方塊為強制回應，關閉後處理程序才會結束。執行階段證據：引數錯誤、沒有作用中的工作階段，以及進入遊戲前的失敗（執行階段結案項目 `T04`）。

<a id="session-tray-and-exit"></a>
## 工作階段、系統匣與結束

由 `OmsiLaunchW.exe` 擁有的工作階段，其行為與由 `OmsiLaunch.exe` 擁有的工作階段完全相同（請參閱[工作階段生命週期](../concepts/session-lifecycle.md)）：

- 系統匣圖示會在工作階段一啟動、OMSI 尚未進入遊戲之前就出現，除非在 `/spec` 檔中設定了 `Presentation.SuppressTrayIcon`。使用 `SuppressTrayIcon` 時完全沒有可見介面；請以 `OmsiLaunch.exe session stop` 或關閉 OMSI 的方式停止工作階段。
- 當 OMSI 結束、在系統匣中確認 `End session` 以結束工作階段、用戶端傳送 `session stop`，或 `/observe-seconds` 時間到時，工作階段即結束。接著 OmsiLaunch 會在 OMSI 仍在執行時將其終止、還原它變更過的每個檔案、釋放安裝租用、移除系統匣圖示並結束。
- 若 `OmsiLaunchW.exe` 本身遭強制終止，該安裝的下一次 OmsiLaunch 啟動會復原待處理的交易（請參閱[交易與復原](../concepts/transactions-and-recovery.md)）。

<a id="quick-start"></a>
## 快速入門

1. 將套件安裝到 OMSI 2 目錄（[安裝](../getting-started/installation.md)）。
2. 建立指向 `OmsiLaunchW.exe` 的捷徑，並加上工作階段的引數，例如 `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`。
3. 啟動它。OMSI 會在沒有主控台視窗的情況下啟動；OmsiLaunch 圖示會出現在通知區域。
4. 在圖示上按右鍵 → 選擇 `Status` 項目可查看工作階段狀態，或選擇 `End session` → `End session` 以結束工作階段。
5. 若發生問題，對話方塊會顯示錯誤碼；詳細資訊位於 `<OMSI_PATH>\.omsilaunch\diagnostics`。
