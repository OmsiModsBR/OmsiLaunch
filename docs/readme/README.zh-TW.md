<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>OMSI 2 的工作階段控制。</strong></p>
<p align="center">開放原始碼 · 可程式化 · 社群驅動</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <strong>繁體中文</strong> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> 本文件為規範性[英文（美國）README](../../README.md) 的翻譯。若兩者內容有出入，以英文（美國）README 為準。

# OmsiLaunch

**OmsiLaunch** 是一個開放原始碼層，用於以程式方式啟動 OMSI 2、管理工作階段，並控制執行中的模擬。它依據宣告式描述規劃工作階段，在具有日誌的交易中套用每一項暫時性設定變更，啟動 OMSI 並持續觀察直到進入遊戲畫面，讓工具透過公開 API 讀取及變更執行中的模擬，並在工作階段結束時還原它所修改過的每一個檔案。

它是供啟動器、工具、自動化與社群整合使用的基礎設施，而不是圖形化啟動器。

> **定義工作階段，而非點擊步驟。**

## 狀態：0.1.0-beta4

目前的公開 Beta 版為 **`0.1.0-beta4`**。Beta 4 新增了逐項功能相容性、Runtime Modules、工作階段設定檔 1..99，以及一組啟動與執行階段功能。相容性、功能目錄、分頁、具型別的錯誤與模組橋接層已在實際的 OMSI 工作階段中觀察到（驗證回合 A 與 B）。這些回合之後新增的功能為 `EXPERIMENTAL`：已透過靜態分析結案並僅以離線方式測試，執行階段驗證尚待進行。部分功能仍為 `STATICALLY_VALIDATED`、`PARTIAL` 或 `UNAVAILABLE`。有兩個執行階段項目仍未結案，因為無法安全地產生其情境：Steam LAA 的遊戲過程，以及道路車輛與人物的自然移除（RV-002）。[執行階段驗證狀態](../localized/zh-TW/status/runtime-validation-status.md)頁面是權威紀錄，說明哪些項目曾在 OMSI 下執行、哪些僅以離線方式執行。

本版本為 Beta：公開 API、CLI 與檔案格式依成員分別標示為 `STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL` 或 `UNAVAILABLE`，且在 1.0 之前仍可能變更。

## 支援的 OMSI 範圍

OmsiLaunch 需要已安裝的 OMSI 2。它不依賴某一個特定的 `Omsi.exe` 檔案：每一項原生功能在使用之前，都會在 OMSI 內個別解析並驗證；在某個組建上無法驗證的功能，在該組建上即無法使用。

| 項目 | 範圍 |
| --- | --- |
| 需求 | 已安裝 OMSI 2：32 位元的 `Omsi.exe`，其版本為 OMSI 2（2.x），並具有 `maps\` 與 `Vehicles\` 目錄。其他任何情況都會以 `OL_E_UNSUPPORTED_BUILD` 拒絕。 |
| 已知配置 | OMSI 2.3.004（`Omsi23004`）。每一項功能都能解析，且所有執行階段驗證都在此配置上執行。雜湊僅作為辨識它的快速路徑，並非必要條件。 |
| 其他 OMSI 2 組建 | 每一項功能都透過與組建無關的策略（Delphi RTTI、唯一的程式碼簽章、VMT 槽位）解析並驗證。讀取物件配置的功能需要已知配置。 |
| Steam 執行檔 | 在處理程序內解析；正版 Steam 工作階段**未**經執行階段驗證。 |
| 作業系統 | Windows 10 或更新版本，x64。 |
| Runtime | .NET 6 Desktop Runtime **x64**（控制器）與 .NET 6 Runtime **x86**（外掛程式在 32 位元的 `Omsi.exe` 內執行）。 |

詳細資訊：[相容性](../localized/zh-TW/reference/compatibility.md)與[安裝](../localized/zh-TW/getting-started/installation.md)。

## 提供的功能

**工作階段。** 工作階段依據 `LaunchSpec`（CLI 旗標、JSON 檔案、工作階段設定檔或 API）進行規劃，在不產生副作用的情況下驗證（`/plan`），然後啟動、透過 `SessionState` 狀態轉換加以觀察，最後結束。停止工作階段會強制終止 OMSI，使 OMSI 無法覆寫即將還原的檔案。請參閱[工作階段生命週期](../localized/zh-TW/concepts/session-lifecycle.md)。

**交易與復原。** 每一項設定覆寫的範圍都僅限於該工作階段。OmsiLaunch 會對其變更的每個檔案建立快照、記入日誌、套用、驗證並還原，即使在當機之後也是如此（`/recovery-status`、`/recover`、`RecoverPendingAsync`）。它不提供永久性的設定編輯。請參閱[交易與復原](../localized/zh-TW/concepts/transactions-and-recovery.md)。

**CLI（`OmsiLaunch.exe`）。** 建立在同一套公開 API 之上的參考前端，本身不含任何 OMSI 邏輯：探索、規劃、啟動工作階段，以及對執行中工作階段下達的用戶端命令。請參閱 [CLI 參考](../localized/zh-TW/reference/cli.md)與 [CLI 範例](../localized/zh-TW/reference/cli-examples.md)。

**`OmsiLaunchW.exe`。** 供捷徑使用的 Windows 子系統主機。它接受相同的命令列，不顯示主控台視窗，並以訊息方塊回報失敗。請參閱 [OmsiLaunchW.exe](../localized/zh-TW/reference/omsilaunchw.md)。

**Windows 系統匣。** 每個擁有者工作階段都會在通知區域顯示圖示，並提供狀態視窗與「End session」（結束工作階段）動作。此動作採用與 `session stop` 相同的停止路徑。請參閱 [Windows 系統匣](../localized/zh-TW/reference/windows-tray.md)。

**工作階段設定檔。** 位於 `.omsilaunch\session-profiles\<id>\` 下的宣告式 `profile.yaml` 套件（`omsilaunch.session-profile/v1`），讓內容作者能夠發佈以單一命令即可啟動、且可重現的工作階段。請參閱[工作階段設定檔](../localized/zh-TW/reference/session-profiles.md)。

**公開 API 與執行階段控制。** `OmsiLaunch.Api`（`IOmsiLaunch`）是建議使用的產品介面。時間、天氣、地圖、攝影機、時刻表、車輛、人物、指令碼變數與 D3D 材質等執行階段操作皆以工作階段為範圍，依組建設定檔進行驗證，並透過不透明的語意 handle（控制代碼）定址，絕不使用原生指標。結果受 64 KiB 執行階段槽位限制。請參閱[公開 API 參考](../localized/zh-TW/reference/public-api.md)與[執行階段控制](../localized/zh-TW/reference/runtime-control.md)。

**本機控制。** 每個安裝各自擁有一個繫結至作用中 `session_id` 的具名管道（named pipe），讓同一使用者的其他處理程序能讀取狀態與事件、停止工作階段，以及執行公開的執行階段操作。請參閱[本機控制／IPC](../localized/zh-TW/reference/local-control.md)。

**功能。** 每項功能（capability）與公開執行階段操作都連同其穩定性編入目錄。實驗性與無法使用的功能會列出，而不會隱藏（`OmsiLaunch.exe capabilities`）。請參閱[功能](../localized/zh-TW/reference/capabilities.md)。

**永久外掛程式。** 處理程序內的外掛程式檔案集合只會在 `plugins\OmsiLaunch.*` 下安裝一次。每次啟動前，都會依據 `release-manifest.json` 中的 SHA-256 項目進行檢查。第三方外掛程式絕不會被觸碰。請參閱[永久外掛程式模型](../localized/zh-TW/concepts/permanent-plugin.md)。

## 快速入門

將發行套件解壓縮到 OMSI 2 安裝根目錄，然後在該目錄中執行下列命令：

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

在該工作階段執行期間，可在同一目錄中開啟第二個主控台來查詢或結束它：

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

逐步說明：[第一個工作階段](../localized/zh-TW/getting-started/first-session.md)。.NET 整合者請參閱：[公開 API 快速入門](../localized/zh-TW/getting-started/api-quick-start.md)。

## 文件

完整文件集的索引位於 [`docs/localized/zh-TW/README.md`](../localized/zh-TW/README.md)。`docs/` 下的英文（美國）頁面為規範性的正式版本。

| 主題 | 頁面 |
| --- | --- |
| 公開 API | [docs/localized/zh-TW/reference/public-api.md](../localized/zh-TW/reference/public-api.md) |
| CLI | [docs/localized/zh-TW/reference/cli.md](../localized/zh-TW/reference/cli.md) |
| CLI 範例 | [docs/localized/zh-TW/reference/cli-examples.md](../localized/zh-TW/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/zh-TW/reference/omsilaunchw.md](../localized/zh-TW/reference/omsilaunchw.md) |
| Windows 系統匣 | [docs/localized/zh-TW/reference/windows-tray.md](../localized/zh-TW/reference/windows-tray.md) |
| 工作階段設定檔 | [docs/localized/zh-TW/reference/session-profiles.md](../localized/zh-TW/reference/session-profiles.md) |
| 執行階段控制 | [docs/localized/zh-TW/reference/runtime-control.md](../localized/zh-TW/reference/runtime-control.md) |
| 本機控制／IPC | [docs/localized/zh-TW/reference/local-control.md](../localized/zh-TW/reference/local-control.md) |
| 功能 | [docs/localized/zh-TW/reference/capabilities.md](../localized/zh-TW/reference/capabilities.md) |
| 錯誤與結束代碼 | [docs/localized/zh-TW/reference/errors.md](../localized/zh-TW/reference/errors.md)、[docs/localized/zh-TW/reference/exit-codes.md](../localized/zh-TW/reference/exit-codes.md) |
| 封裝 | [docs/localized/zh-TW/reference/packaging.md](../localized/zh-TW/reference/packaging.md) |
| 已知限制 | [docs/localized/zh-TW/reference/known-limitations.md](../localized/zh-TW/reference/known-limitations.md) |
| 執行階段驗證狀態 | [docs/localized/zh-TW/status/runtime-validation-status.md](../localized/zh-TW/status/runtime-validation-status.md) |

### 其他語言的文件

文件已翻譯為 14 種地區語言，位於 [`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md) 下。這些翻譯以規範性的英文（美國）頁面為來源產生，並已依據英文頁面進行機械式驗證。母語人士的編輯審閱不屬於 Beta 4 發行的一部分，可能於發佈後進行。當翻譯與英文頁面不一致時，以英文頁面為準。

## 限制與未結項目

以下是最重要的限制。完整清單請參閱[已知限制](../localized/zh-TW/reference/known-limitations.md)。

- **Steam。** Steam LAA 為 `PARTIAL`：遊戲過程、讀取、命令與停止都需要正版 Steam 安裝，且未經執行階段驗證。
- **本 Beta 版中屬實驗性：** 從上次的地圖狀態啟動（`/last`），以及啟動時明確指定日期、時間、年份或天氣，已透過靜態分析結案，尚待執行階段驗證。**無法使用：** 啟動時指派玩家車輛；要求此項會使計畫不可執行，而不是被默默忽略。
- **執行階段寫入受到限制。** `weather.set`、`weather.metar.set`、字串變數寫入與指令碼觸發在經過執行階段驗證之前皆為實驗性；行事曆寫入與車輛重新定位則無法使用。執行階段變更不會記入日誌，也不會還原。
- **Handle 生命週期。** 對於自然消失的道路車輛與人物，失效偵測（RV-002）沒有安全的執行階段產生方式，僅以離線方式驗證。
- **分頁結果。** 執行階段清單以 `cursor` 與 `limit` 分頁，並從 `next_cursor` 繼續；放不進 64 KiB 信箱（mailbox）的頁面會被縮短，並標示為 `truncated=true`。
- **預設為強制停止。** OMSI 會被終止，其本身的關閉程序不會執行，未儲存的 OMSI 狀態將會遺失。需主動選用的協作式關閉（`/cooperative-shutdown`，實驗性）會先執行 OMSI 的結束常式。
- **同一使用者信任模型。** 同一 Windows 使用者的任何處理程序都能存取本機控制平面。

## 下載

請從 [Releases 頁面](https://github.com/OmsiModsBR/OmsiLaunch/releases)下載 **`OmsiLaunch-0.1.0-beta4.zip`** 及其 `.sha256` 檔案，並直接解壓縮到支援的 OMSI 根目錄。套件包含控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe`）、其相依元件、附有 `release-manifest.json` 的永久外掛程式檔案集合、啟動畫面資源、一個工作階段範例，以及位於 `.omsilaunch\docs\` 下的離線文件。套件配置與完整移除：[封裝](../localized/zh-TW/reference/packaging.md)。

## 從原始碼建置

需求：

- Windows 10 或更新版本，x64。
- .NET 6 SDK，以及執行測試所需的 x64 與 x86 .NET 6 runtime。
- 安裝 MSBuild C++ 工作負載（平台工具組 `v145`）的 Visual Studio 與 Windows 10 SDK，用於三個原生專案：`OmsiLaunch.Native.x86`（Win32），以及 `OmsiLaunch.Bootstrapper` 與 `OmsiLaunch.WindowsHost`（x64）。

`OmsiLaunch.sln` 包含受控專案與測試套件。單一的離線進入點會建置所有項目並執行每一個離線測試套件；它絕不會啟動 OMSI：

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` 會略過原生專案與封裝回歸測試。`-SkipDocs` 會略過文件檢查關卡。建置輸出位於 `artifacts\`，不納入版本控制。`tools\New-ReleasePackage.ps1` 會從既有的建置結果暫存、計算雜湊、驗證並壓縮發行套件。請參閱[封裝](../localized/zh-TW/reference/packaging.md)與[測試與驗證](../project/TESTING-AND-VALIDATION.md)。

| 路徑 | 內容 |
| --- | --- |
| `src/` | 產品程式庫：API、核心、設定、內容、interop、處理程序、外掛程式、組建設定檔、原生 x86 邊界 |
| `tools/` | CLI 與 Windows 主機（`OmsiLaunch.Cli`）、原生 shim（`OmsiLaunch.Bootstrapper`）、離線測試主機、封裝與驗證指令碼、在地化工具 |
| `tests/` | 單元、整合、設定檔、Windows UI 與文件測試套件 |
| `docs/` | 規範性文件及其位於 `docs/localized/` 下的翻譯 |
| `examples/` | LaunchSpec 與工作階段範例 |
| `assets/` | 品牌識別、圖示與套件資源 |
| `third_party/` | 上游來源出處說明 |

維護者摘要：[PUBLIC-API.md](../specs/PUBLIC-API.md)、[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md)、[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md)、[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md)、[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md)、[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md)。

## 社群與授權

OmsiLaunch 是以社群為導向的開放原始碼專案。它獨立於其他 OMSI 啟動器，相容的社群工具可以在其基礎上進行建構。

OmsiLaunch 以 [LGPL-3.0-only](../../LICENSE) 授權。關於所納入原始碼的出處及適用聲明，請參閱[第三方聲明](../../THIRD-PARTY-NOTICES.md)。
