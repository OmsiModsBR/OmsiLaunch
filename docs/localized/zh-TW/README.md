# OmsiLaunch 文件

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../README.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

這是 OmsiLaunch `0.1.0-beta4`（強化後基準版本）的規範性英文文件。OmsiLaunch 針對已安裝的 OMSI 2 提供可程式化的啟動、工作階段擁有權與執行階段控制；每項原生功能都依組建逐一解析並驗證（請參閱[相容性](reference/compatibility.md)）。`docs/` 下的每一頁都描述目前程式碼的實際行為；當頁面與程式碼不一致時，以程式碼為準，該頁面即視為錯誤。

全文使用的穩定性詞彙：`STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`、`UNAVAILABLE`。會被解析但不產生任何作用的旗標標示為 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`。除非[執行階段驗證狀態](status/runtime-validation-status.md)如此記載，否則任何項目都不稱為已於執行階段驗證。

<a id="who-reads-what"></a>
## 讀者導覽

| 讀者 | 從這裡開始 | 接著閱讀 |
| --- | --- | --- |
| 使用者（CLI、捷徑、工作階段設定檔） | [安裝](getting-started/installation.md)、[第一個工作階段](getting-started/first-session.md) | [CLI 參考](reference/cli.md)、[CLI 範例](reference/cli-examples.md)、[工作階段設定檔](reference/session-profiles.md)、[Windows 系統匣](reference/windows-tray.md)、[結束代碼](reference/exit-codes.md) |
| 整合者（`OmsiLaunch.Api`、本機 IPC） | [公開 API 快速入門](getting-started/api-quick-start.md)、[公開 API 參考](reference/public-api.md)、[LaunchSpec 參考](reference/launchspec.md) | [工作階段生命週期](concepts/session-lifecycle.md)、[執行階段控制](reference/runtime-control.md)、[功能](reference/capabilities.md)、[本機控制／IPC](reference/local-control.md)、[錯誤參考](reference/errors.md) |
| 維護者（發行、驗證、邊界） | [封裝](reference/packaging.md)、[永久外掛程式模型](concepts/permanent-plugin.md) | [交易與復原](concepts/transactions-and-recovery.md)、[相容性](reference/compatibility.md)、[已知限制](reference/known-limitations.md)、[執行階段驗證狀態](status/runtime-validation-status.md) |

<a id="navigation"></a>
## 導覽

| 頁面 | 用途 |
| --- | --- |
| [快速上手](getting-started/first-session.md) | 從 OMSI 根目錄規劃、啟動、觀察並停止一個工作階段。 |
| [安裝](getting-started/installation.md) | 先決條件、將套件解壓縮到 OMSI 根目錄、以 `/version` 驗證、完整移除。 |
| [公開 API 快速入門](getting-started/api-quick-start.md) | 一個完整的 .NET 程式，用來規劃、啟動、讀取並停止一個工作階段。 |
| [CLI 參考](reference/cli.md) | `OmsiLaunch.exe` / `OmsiLaunchW.exe` 的所有旗標、命令字與階層式路由。 |
| [CLI 範例](reference/cli-examples.md) | 常見工作可直接複製貼上的命令列。 |
| [LaunchSpec 參考](reference/launchspec.md) | 所有 `LaunchSpec` 屬性與列舉值，以及 `/spec` 的 JSON 載入規則。 |
| [HOF 管理](reference/hof-management.md) | 以工作階段為範圍，將 HOF 複製並鏡像到車輛資料夾：來源、目標、衝突與還原。 |
| [執行階段模組](reference/runtime-modules.md) | 隨工作階段在 OMSI 內執行的模組：資訊清單、信任、生命週期、失敗原則與操作。 |
| [工作階段設定檔參考](reference/session-profiles.md) | `profile.yaml` 結構描述 `omsilaunch.session-profile/v1`、索引鍵、限制與優先順序。 |
| [公開 API 參考](reference/public-api.md) | `IOmsiLaunch`、公開 record 與列舉，以及每個成員的穩定性。 |
| [公開 API 清單](reference/public-api-inventory.md) | 自動產生的清單，列出每個公開型別與成員及其簽章與穩定性。 |
| [執行階段控制](reference/runtime-control.md) | 執行階段命令通道、逾時、handle、停止語意。 |
| [功能參考](reference/capabilities.md) | 功能目錄，以及每個公開執行階段操作 id 及其分類。 |
| [工作階段生命週期](concepts/session-lifecycle.md) | `SessionState` 狀態轉換、`StartSessionAsync` 保證的事項、工作階段如何結束。 |
| [交易與復原](concepts/transactions-and-recovery.md) | 日誌狀態、備份、還原驗證、工作階段刪除、當機復原。 |
| [永久外掛程式模型](concepts/permanent-plugin.md) | `plugins\OmsiLaunch.*` 檔案集合、以資訊清單（manifest）為基礎的完整性檢查、工作階段絕不觸碰的項目。 |
| [本機控制／IPC](reference/local-control.md) | 具名管道（named pipe）協定 `0.1`、每個安裝各自的端點、`session_id` 繫結、信任模型。 |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Windows（無主控台）主機：與 `OmsiLaunch.exe` 的差異、`/silent`、對話方塊、結束代碼。 |
| [Windows 系統匣](reference/windows-tray.md) | 通知區域指示器：圖示、選單、狀態視窗的逐欄說明、End session、檔案總管重新啟動。 |
| [錯誤參考](reference/errors.md) | 每個 `OL_E_*` / `OL_W_*` 代碼及其類別與意義。 |
| [結束代碼](reference/exit-codes.md) | `PublicExitCode` 值 0 到 10，以及啟動程式 shim 代碼 100 到 106。 |
| [封裝／安裝配置](reference/packaging.md) | 發行 ZIP 中的檔案、`release-manifest.json`、`.omsilaunch\` 的配置。 |
| [相容性／支援的 OMSI 組建](reference/compatibility.md) | 唯一支援的 `Omsi.exe` hash、接受的 Steam LAA hash、平台需求。 |
| [已知限制](reference/known-limitations.md) | 此 Beta 版中不支援、部分支援或屬於已接受風險的項目。 |
| [執行階段驗證狀態](status/runtime-validation-status.md) | 哪些已在 OMSI 下執行過、哪些僅離線執行過、哪些仍需要實際工作階段。 |

對維護者而言仍具規範性的頁面：儲存庫的
[`README.md`](../../../README.md)、`docs/specs` 之下的規格
（[`PUBLIC-API.md`](../../specs/PUBLIC-API.md)、
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md)、
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md)、
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)）以及 `docs/project` 之下的專案頁面
（[`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md)、
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md)、
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)）。這些頁面只是摘要；上方各頁才是詳細參考。歷史頁面列於[文件資訊清單](../../DOCUMENTATION-MANIFEST.md)。

<a id="how-this-documentation-is-kept-in-sync"></a>
## 本文件如何保持同步

文件檢查關卡 `tests\OmsiLaunch.DocumentationTests` 會針對 `OmsiLaunch.Api` 與 `OmsiLaunch.Core` 進行編譯，並將上述頁面與定義公開介面的程式碼相互比對：

| 關卡 | 檢查內容 |
| --- | --- |
| `docs.cli-flags` | 每個 `CliInput.KnownFlags` 項目都以 `` `/flag` `` 或 `` `/flag:` `` 形式出現在 CLI 參考中；每個 `CliInput.AcceptedNoEffectFlags` 項目在其所在行都標示為 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`；每個命令字與每個 `CliInput.HierarchicalRoutes` 路由都與其執行階段操作一同出現；每個 `PublicExitCode` 值在結束代碼表中都有一個 `| n |` 資料列。 |
| `docs.capabilities` | 每個 `PublicCapabilityRegistry.All` id、每個 `PublicCapabilityRegistry.PublicRuntimeOperationIds` 項目與每個 `PublicCapabilityClassification` 名稱都出現在功能參考中。 |
| `docs.errors` | 每個 `PublicErrorCodes.All` 代碼都出現在錯誤參考中，且 `src\` 或 `tools\OmsiLaunch.Cli\` 中沒有任何 `OL_E_*` / `OL_W_*` 常值缺漏於 `PublicErrorCodes`。 |
| `docs.public-api` | `OmsiLaunch.Api` 的每個匯出型別、每個列舉值與每個 `IOmsiLaunch` 成員都出現在公開 API 參考中，且五個穩定性詞彙全部都有使用。 |
| `docs.launchspec` | 可由 `LaunchSpec` 存取的每個公開屬性及其列舉的每個值都出現在 LaunchSpec 參考中。 |
| `docs.session-profiles` | 每個 `SessionProfileCompiler.SchemaKeys` 索引鍵、結構描述識別碼與 `256 KiB` 限制都出現在工作階段設定檔參考中。 |
| `docs.structure` | 導覽表中的每個頁面都存在。 |
| `docs.links` | `docs\**\*.md`（不含 `docs\localized\`）與根層級 `*.md` 檔案中的每個相對連結都能解析為一個檔案或目錄。 |
| `docs.localization` | `docs\localized\LOCALIZATION-MANIFEST.md` 所列的每個地區設定都具備本地化頁面集的所有頁面；每個頁面都保留英文頁面的標題、表格與程式碼區塊、每個行內程式碼片段（旗標、功能與操作 id、錯誤碼、索引鍵、識別碼）與每個連結，且其相對連結皆可解析。 |

此關卡是 `tools\Invoke-OfflineValidation.ps1` 執行的測試套件之一（可用 `-SkipDocs` 略過）。它以離線方式執行，絕不會啟動 OMSI；當旗標、路由、功能、錯誤碼、列舉值或公開型別未經記載，或連結失效時，會使組建失敗。它不檢查文字敘述，因此頁面對行為的描述仍可能有誤；遇到這種情況，請針對該頁面回報錯誤。

<a id="translations"></a>
## 翻譯

`docs\localized\<locale>\` 存放此 `0.1.0-beta4` 文件的完整翻譯，涵蓋 `pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW` 與 `ja-JP`。頁面集、各地區設定的根目錄以及刻意不翻譯的頁面列於 [`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md)。翻譯會保留英文頁面中的每個命令、旗標、識別碼、錯誤碼與範例不變，並由 `docs.localization` 關卡進行檢查。英文頁面仍是規範性的來源：翻譯與英文頁面不一致時，以英文頁面與程式碼為準，該翻譯即視為錯誤。
