# 第一個工作階段

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../getting-started/first-session.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁逐步說明如何以 OmsiLaunch `0.1.0-beta4` 執行第一個受管理的 OMSI 工作階段：在不啟動 OMSI 的情況下規劃、以明確旗標啟動、以預先定義的工作階段設定檔啟動、控制與停止工作階段，以及事後查找診斷資訊。本頁假設套件已依[安裝](installation.md)的說明完成安裝。每個旗標的規範都在 [CLI 參考](../reference/cli.md)中；更多呼叫範例請參閱 [CLI 範例](../reference/cli-examples.md)。

<a id="what-a-session-does"></a>
## 工作階段會做什麼

工作階段是圍繞單一 OMSI 處理程序的交易：OmsiLaunch 會為它將變更的檔案建立快照（預設為 `GUI\` 下的兩個啟動畫面點陣圖，要求 `/set` overlay（暫時覆蓋）時再加上 `options.cfg`），在 `.omsilaunch\` 下寫入持久的日誌，套用 overlay，帶著永久外掛程式啟動 `Omsi.exe`，等待進入遊戲（`Running`），在期間保持工作階段可受控制，最後終止 OMSI 並逐位元組還原每個變更過的檔案。`/new` 絕不會默默自行選擇地圖或進入點：兩者都必須明確指定，或來自 `/spec` 檔或工作階段設定檔。

<a id="1-plan-nothing-is-started"></a>
## 1. 規劃（不會啟動任何東西）

請在 OMSI 根目錄下執行；安裝預設為包含 `OmsiLaunch.exe` 的目錄。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

計畫必須顯示 `"IsRunnable": true`（結束代碼 `0`）。它會列出 `TouchedFiles` 與 `PlannedMutations`，讓您確切看到工作階段將覆蓋哪些內容。繼續之前請先修正任何 `OL_E_` 診斷訊息；此時尚未寫入任何內容。

套件隨附的範例 spec 以檔案形式執行相同的操作：

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. 以明確旗標啟動

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

依序會發生下列事情：

1. 印出計畫（在已知配置上為 `Plan: READY profile=Omsi23004`）。
2. 復原任何較舊的待處理日誌、取得租用、建立快照、寫入日誌、套用 overlay、檢查外掛程式完整性、啟動 `Omsi.exe`。
3. 出現系統匣圖示（`OmsiLaunch is running`）；請參閱 [Windows 系統匣](../reference/windows-tray.md)。
4. 進入遊戲後，會以 JSON 印出 `Running` 狀態（`"State": 14`）。預設的啟動逾時為 180 s（可用 `/startup-timeout:<1..600>` 變更）。
5. 主控台會保持連結，直到工作階段結束。請勿以關閉主控台視窗的方式停止：請使用下方的其中一種停止方法。

首次執行時可選擇加入：

- `/set:graphics.maxFPS=60`（`options.cfg` overlay，結束時會還原）；
- `/splash:Unset` 保持 OMSI 啟動畫面不變，或 `/splash-language:DEU` 選擇在地化的受管理啟動畫面；
- `/observe-seconds:30` 在 `Running` 之後 30 s 自動停止（適合冒煙測試）；
- `--json` 取得結構化輸出。

要求日期、時間、年份、天氣或玩家車輛的旗標（`/date`、`/time`、`/year`、`/weather*`、`/vehicle`……）會被接受，但此組建無法套用：計畫會變成 `NOT RUNNABLE` 並帶有 `OL_E_CAPABILITY_UNAVAILABLE`。請勿使用這些旗標。

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. 以預先定義的工作階段設定檔啟動

工作階段設定檔是位於 `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 的 YAML 檔，用來固定地圖、進入點，以及最多 99 組設定的預設組合（preset）（結構描述 `omsilaunch.session-profile/v1`；完整參考請見[工作階段設定檔](../reference/session-profiles.md)）。建立 `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

接著執行：

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

需要記住的規則：`id` 必須與目錄名稱相同；索引為 `1..99`；會覆寫設定檔所擁有欄位的明確旗標（`/map`、`/entrypoint-index`、預設組合所擁有的 `/set` 索引鍵、預設組合含有 `presentation` 時的啟動畫面旗標）會以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 拒絕（結束代碼 `2`）；`new:` 區塊只在使用 `/new` 時套用；使用 `/saved:<file.osn>` 時，情境的地圖必須列在 `compatibility.maps` 下。套件隨附的 `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` 示範了完整的結構描述，但其 `new:` 區塊設定了 `date`、`time` 與 `weather`，而此組建無法套用這些項目，因此請先移除這些索引鍵再複製使用。

<a id="4-control-the-running-session"></a>
## 4. 控制執行中的工作階段

從同一目錄下的第二個主控台執行（不需安裝引數）：

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

這些命令會經由此安裝的本機控制管道傳送（[本機控制](../reference/local-control.md)）；結束代碼 `4` 表示此處沒有正在執行的擁有者。

<a id="5-stop"></a>
## 5. 停止

下列任一方式都會以相同流程結束工作階段（先終止 OMSI，接著還原每個變更過的檔案，再刪除日誌與備份）：

| 方法 | 備註 |
|---|---|
| 系統匣圖示 → `End session` → 確認 | 在 `OmsiLaunch.exe` 與 `OmsiLaunchW.exe` 工作階段中皆可使用。 |
| `OmsiLaunch.exe session stop` | 從另一個主控台執行；會立即返回，由擁有者完成還原。 |
| 在擁有者主控台按 Ctrl+C | 要求停止；擁有者會等待還原完成後才結束。 |
| `/observe-seconds:<n>` | 在 `Running` 之後 `n` 秒自動停止。 |
| OMSI 自行結束 | 擁有者偵測到 `ProcessExited` 後進行還原。 |

OMSI 本身的關閉程序不會執行，因此 OMSI 結束時不會改寫 `options.cfg`；這是刻意的設計，以確保還原精確。以 X 按鈕關閉擁有者主控台視窗時，還原只有 4 s 可完成；若未完成，下一次啟動（或 `OmsiLaunch.exe /recover`）會依據日誌將其完成。當工作階段以 `Completed` 結束時，擁有者的結束代碼為 `0`。

<a id="6-where-to-look-afterwards"></a>
## 6. 事後查看的位置

| 位置 | 內容 |
|---|---|
| 主控台／`--json` 輸出 | 計畫、`Running` 狀態、最終狀態（`"State": 18` = `Completed`）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | 工作階段的主機追蹤記錄（交易邊界、處理程序啟動、外掛程式交接、進入遊戲、還原）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | 由擁有者執行的 `/runtime:` 操作結果。 |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | 系統匣指示器事件。 |
| `OmsiLaunch.exe /recovery-status` | 正常結束後為 `"pending": false`。`true` 表示仍留有日誌；請執行 `OmsiLaunch.exe /recover`。 |

若工作階段未能進入遊戲，最終狀態會帶有導致失敗的 `OL_E_` 診斷訊息（例如 `OL_E_STARTUP_TIMEOUT`、`OL_E_PLUGIN_NOT_LOADED`、`OL_E_PROCESS_EXITED_EARLY`），結束代碼為 `1`，而檔案仍然已經還原。請參閱[結束代碼](../reference/exit-codes.md)、[錯誤](../reference/errors.md)與[已知限制](../reference/known-limitations.md)。

<a id="running-without-a-console"></a>
## 在沒有主控台的情況下執行

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

會委派給 `OmsiLaunchW.exe` 並立即回傳 `0`。此工作階段沒有主控台；失敗會以訊息方塊顯示，系統匣圖示是唯一可見的介面。請使用 `session status`、`events watch` 與診斷目錄追蹤其狀態。Windows 主機的完整行為請參閱 [OmsiLaunchW.exe 參考](../reference/omsilaunchw.md)。
