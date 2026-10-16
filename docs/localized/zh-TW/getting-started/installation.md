# 安裝

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../getting-started/installation.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁說明 OmsiLaunch `0.1.0-beta4` 的需求、支援的 OMSI 組建、如何將發行套件安裝到 OMSI 安裝根目錄，以及在啟動工作階段之前如何以 `/version` 和 `/plan` 驗證安裝。套件內容規範於[封裝](../reference/packaging.md)；首次啟動說明於[第一個工作階段](first-session.md)。

<a id="requirements"></a>
## 需求

| 需求 | 說明 | 缺少時的失敗結果 |
|---|---|---|
| Windows 10 或更新版本，64 位元 | 控制器會檢查 `Environment.OSVersion.Version.Major >= 10`、x64 作業系統以及 x64 處理程序。 | 計畫不可執行，錯誤為 `OL_E_UNSUPPORTED_OPERATING_SYSTEM`（或 `OL_E_UNSUPPORTED_OS_ARCHITECTURE`），結束代碼 `1`/`3`。 |
| .NET 6 Desktop Runtime，**x64** | `OmsiLaunch.Controller.runtimeconfig.json` 需要 `Microsoft.NETCore.App` 6.0 與 `Microsoft.WindowsDesktop.App` 6.0（系統匣指示器使用 Windows Forms）。shim 透過 `nethost.dll` 尋找 runtime。 | `OmsiLaunch.exe` 在產生任何輸出之前即以 shim 代碼 `102`..`106` 結束；`OmsiLaunchW.exe` 會顯示 `OmsiLaunch could not start the .NET host (code N).`。 |
| .NET 6 Runtime，**x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` 需要 x86 版的 `Microsoft.NETCore.App` 6.0，因為外掛程式在 32 位元的 `Omsi.exe` 內執行。x86 .NET 6 Desktop Runtime 套組同樣可滿足此需求。 | 外掛程式無法在 OMSI 內啟動；工作階段無法進入 `Running`（`OL_E_PLUGIN_NOT_LOADED`／`OL_E_STARTUP_TIMEOUT`），結束代碼 `1`，檔案已還原。 |
| 已安裝 OMSI 2 | 32 位元的 `Omsi.exe`，版本為 OMSI 2（2.x），且根目錄中有 `maps\` 與 `Vehicles\`。不要求特定的執行檔雜湊；OMSI 2.3.004 是已知且已於執行階段驗證的配置，在其他組建上，每項功能都會在處理程序內解析並驗證。 | `OL_E_UNSUPPORTED_BUILD`；計畫不可執行，結束代碼 `1`。組建上無法使用的功能會以 `OL_E_CAPABILITY_UNRESOLVED` 失敗。請參閱[相容性](../reference/compatibility.md)。 |
| 可寫入的安裝根目錄 | 交易會寫入 `.omsilaunch\`、`GUI\` 與 `Texture\` 下的 overlay（暫時覆蓋）以及 `options.cfg`，之後再將其還原；目前使用者必須對根目錄具有寫入權限（若無適當權限，請避免使用 `Program Files`）。 | `OL_E_INSTALLATION_NOT_WRITABLE`，結束代碼 `1`。 |
| 每個安裝一位使用者、一個擁有者 | 安裝租用 `Local\OmsiLaunch.Installation.<sha256(root)>` 與控制管道都以登入工作階段為範圍。 | `OL_E_INSTALLATION_BUSY`／`OL_E_SESSION_ALREADY_ACTIVE`，結束代碼 `7`。 |

這兩個 runtime 需分別從 Microsoft 下載；請安裝 .NET 6 的 x64 Desktop Runtime 與 x86 Runtime（或 x86 Desktop Runtime）。不需要其他元件。任何資料都不會離開本機。

<a id="confirm-the-omsi-build"></a>
## 確認 OMSI 組建

將 `<OMSI_PATH>` 替換為您的 OMSI 2 目錄（例如 `C:\OMSI 2`）。

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

雜湊必須是上方列出的兩者之一。安裝完成後，`OmsiLaunch.exe profiles` 會列出相同的清單及其驗證狀態。

<a id="install-the-package"></a>
## 安裝套件

1. 下載 `OmsiLaunch-0.1.0-beta4.zip` 與 `OmsiLaunch-0.1.0-beta4.zip.sha256`，並驗證總和檢查碼（`Get-FileHash` 必須等於 `.sha256` 檔中的值）。
2. 將封存檔**直接解壓縮到 OMSI 安裝根目錄**（包含 `Omsi.exe` 的資料夾）。封存檔的配置即對應該根目錄：
   - 根目錄下的 `OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.Controller.dll` 與其他 `OmsiLaunch.*.dll` 控制器組件、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md`；
   - 永久外掛程式的完整檔案集 `plugins\OmsiLaunch.*`（9 個檔案），放在您既有的外掛程式旁，既有外掛程式絕不會被變更；
   - `.omsilaunch\`，內含啟動畫面（splash）資源、離線文件與範例。
3. 請將 `release-manifest.json` 保留在 `OmsiLaunch.exe` 旁。每次啟動時正是依靠它以 SHA-256 驗證已安裝的外掛程式檔案（`plugin.integrity.reference = manifest`）；若缺少此檔，則只會檢查檔案是否存在及其自身一致性（`plugin.integrity.reference = self`）。
4. 請勿移動或重新命名 `plugins\OmsiLaunch.*` 下的任何項目，也不要將外掛程式二進位檔放入 `.omsilaunch\`。

升級也是相同的操作：在沒有工作階段執行、也沒有待處理的復原時（`OmsiLaunch.exe /recovery-status`），將新套件解壓縮並覆蓋舊檔案。外掛程式的雜湊與資訊清單（manifest）必須一律來自同一個套件（否則會出現 `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`）。

<a id="verify"></a>
## 驗證

請在 OMSI 根目錄下執行（安裝引數預設為包含 `OmsiLaunch.exe` 的目錄）：

```text
OmsiLaunch.exe /version
```
預期結果：`"version": "0.1.0-beta4"`、`"protocol_version": "0.1"`、`"supported_family": "OMSI_2"`；結束代碼 `0`。結束代碼 `102`..`106` 表示缺少 x64 .NET 6 runtime，或套件不完整。

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
預期結果：先列出受支援的雜湊，接著列出在此安裝中找到的地圖；結束代碼 `0`。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
預期結果：`Plan: READY profile=Omsi23004`（在已知配置上；其他情況為 `unrecognized`）且結束代碼 `0`（可使用 `/list:Maps` 中的任何地圖識別；進入點索引必須是該地圖所呈現的索引，請參閱 `/list:Entrypoints /map:<identity>`）。加上 `--json` 時，計畫會列出 `TouchedFiles`（啟動畫面 overlay）、`PlannedMutations`、`RequiredCapabilities`（`STATICALLY_VALIDATED`；在已知配置上，原生啟動功能為 `RESOLVED_KNOWN_LAYOUT`）、`Diagnostics`（包含 `plugin.integrity.reference`）以及 `IsRunnable`。若為 `Plan: NOT RUNNABLE` 並以結束代碼 `1` 結束，原因會列在 `Diagnostics` 中（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_RUNTIME_ARTIFACT_MISSING`……）。規劃絕不會啟動 OMSI，也絕不會寫入安裝目錄。

<a id="where-things-live-afterwards"></a>
## 安裝後各項目的位置

| 路徑 | 內容 |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | 每個工作階段的主機追蹤記錄（保留最新的 50 個工作階段） |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | 系統匣指示器記錄檔 |
| `<root>\.omsilaunch\journal.json`、`backup\<sessionId>\` | 僅在有交易待處理時存在；請參閱[交易與復原](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | 您預先定義的工作階段設定檔；請參閱[工作階段設定檔](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | 受管理的啟動畫面資源 |
| `<root>\.omsilaunch\docs\` | 本文件的離線版本（從 `README.md` 開始閱讀；CLI 參考為 `reference\cli.md`） |
| `<root>\.omsilaunch\examples\` | LaunchSpec 與工作階段設定檔範例 |

<a id="uninstall"></a>
## 解除安裝

停止所有工作階段，執行 `OmsiLaunch.exe /recovery-status`（若有待處理項目則執行 `/recover`），然後刪除根目錄下的產品檔案、`plugins\OmsiLaunch.*` 與 `.omsilaunch\`。詳情請參閱[封裝](../reference/packaging.md)。

<a id="next"></a>
## 下一步

[第一個工作階段](first-session.md) · [CLI 參考](../reference/cli.md) · [已知限制](../reference/known-limitations.md)
