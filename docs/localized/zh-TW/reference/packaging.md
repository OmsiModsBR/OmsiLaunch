# 封裝與發行配置

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/packaging.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁說明 OmsiLaunch `0.1.0-beta4` 的發行套件：`tools\New-ReleasePackage.ps1` 產生哪些內容、`release-manifest.json` 的各個欄位、控制器在執行階段如何利用資訊清單（manifest）驗證永久外掛程式封閉集（closure）、套件如何安裝到 OMSI 安裝根目錄以及如何從中移除、使用後 `.omsilaunch` 目錄包含哪些內容，以及各驗證指令碼（`tools\Test-ReleaseIdentity.ps1`、`tools\Test-ReleasePresentation.ps1`、`tools\Invoke-OfflineValidation.ps1`）。產品識別資訊來自 `OmsiLaunch.Version.props`。給使用者的安裝步驟請見[安裝](../getting-started/installation.md)；外掛程式封閉集在執行階段扮演的角色請見[永久外掛程式](../concepts/permanent-plugin.md)。

<a id="product-identity-omsilaunchversionprops"></a>
## 產品識別資訊（`OmsiLaunch.Version.props`）

| 屬性 | 值 | 用途 |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | 資訊清單的 `product`、Windows 的 `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows 的 `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | Windows 的 `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | 資訊清單的 `product_version`、Windows 的 `ProductVersion`、組件資訊版本（`/version`）、公開 ZIP 檔名 |
| `OmsiLaunchManagedVersion` | `0.1.0` | Managed 組件版本的基準 |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | 組件版本與 Windows 檔案版本 |
| `OmsiLaunchPackageAlias` | `current` | 資訊清單的 `package_alias`、暫存資料夾與別名 ZIP 檔名 |

`Directory.Build.props` 將 `InformationalVersion` 設為 `OmsiLaunchProductVersion`，且不附加原始碼修訂版本，因此 `OmsiLaunch.exe /version` 會精確輸出 `0.1.0-beta4`。

<a id="build-toolsnew-releasepackageps1"></a>
## 組建（`tools\New-ReleasePackage.ps1`）

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]`（預設輸出位置為 `artifacts\release`）會以已組建完成的產出物暫存出一個套件。若 `OmsiLaunch-<product_version>.zip` 已存在，除非指定 `-AllowOverwritePublished`，否則會拒絕寫入 `artifacts\release`；候選套件則輸出到其他目錄（離線驗證使用 `artifacts\candidate\post-round-a`）。

暫存之前，會檢查每一項組建輸出是否已過時。

- **`OmsiLaunch.Native.x86.dll`：依內容判斷，而非依時間戳記。** 原生組建會寫入 `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt`（`.vcxproj` 中的目標 `WriteOmsiLaunchNativeBuildReceipt`）：其中記錄所產生 DLL 的 SHA-256（`output=`），以及組建時使用的每個原始檔的 SHA-256（`source=<sha256>|<path>`：`.cpp`、`.rc`、`.vcxproj` 與 `OmsiLaunch.Version.props`）。當 DLL 的雜湊不是所記錄的輸出時（`does not match its build receipt`：過時或來源不明的副本，不論其時間戳記為何）、當某個已記錄的原始檔有所變更時（`Native source changed after the recorded build`）、當某個原生原始檔未被收據涵蓋時，或當收據遺失時，封裝都會拒絕該 DLL。
- **Shim 與 managed 組件：依時間戳記判斷。** 每一項都不得比其所屬專案的原始檔更舊（各 shim 的 `.cpp`／`.rc`／`.vcxproj`；各 managed 組件自身的專案）。過時的輸入會以 `Stale build artifact` 中止封裝。

接著，套件會在一個全新且名稱唯一的暫存目錄（輸出目錄中的 `.staging-<guid>`）中組裝，因此先前執行留下的任何檔案都不可能混入封閉集。只有在下列所有檢查都通過後，才會取代先前的 `OmsiLaunch-current` 資料夾與封存檔。

| 來源 | 套件中的目的地 |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`、`nethost.dll` | `OmsiLaunch.exe`、`nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\`（x64）：`OmsiLaunch.Controller.dll`、`.deps.json`、`.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Configuration.dll`、`OmsiLaunch.Content.dll`、`OmsiLaunch.Core.dll`、`OmsiLaunch.Process.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`YamlDotNet.dll` | 根目錄 |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`：`OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI 的 `assets\splash\*.bmp`（`PTB`、`ENG`、`DEU`、`FRA`） | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`、`THIRD-PARTY-NOTICES.md` | 根目錄 |
| `docs\` 底下除 `docs\localized\` 以外的所有檔案（英文文件，目錄結構相同） | `.omsilaunch\docs\`（因此 CLI 用法說明中列印的路徑 `.omsilaunch\docs\reference\cli.md` 確實存在；文件稽核 BUG-08）。從 `docs\README.md` 連到儲存庫根目錄摘要（`PUBLIC-API.md` 等）的連結，只有在原始碼儲存庫中才能解析。 |
| `docs\localized\LOCALIZATION-MANIFEST.md`，以及該資訊清單所列每個地區設定的 `docs\localized\<locale>\**`（`pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW` 與 `ja-JP`） | `.omsilaunch\docs\localized\`（結構相同）；若所列的某個地區設定不存在，指令碼會中止 |

接著，指令碼會計算每個已暫存檔案的雜湊，在套件根目錄以 UTF-8 **不含** BOM 的格式寫入 `release-manifest.json`（結果不再取決於 PowerShell 版本），對暫存區執行 `Test-ReleasePackageIntegrity.ps1`，再將每個已暫存的外掛程式檔案與 `OmsiLaunch.Native.x86.dll` 重新與其組建輸出比對，壓縮暫存區，**將封存檔解壓縮到一個全新的暫存目錄，並以同一份資訊清單驗證解壓縮後的封閉集**（封存檔中的資訊清單必須與已驗證的資訊清單逐位元組相同），然後將暫存區發布為 `OmsiLaunch-current`、將封存檔發布為 `OmsiLaunch-current.zip`，並複製為 `OmsiLaunch-<product_version>.zip`（`OmsiLaunch-0.1.0-beta4.zip`），最後寫入內容為 `<SHA-256>  <file name>` 的 `OmsiLaunch-0.1.0-beta4.zip.sha256`。任何產出物遺失都會使指令碼中止。此指令碼不會進行組建；請先執行 `Invoke-OfflineValidation.ps1`（或個別的 `dotnet build`／MSBuild 步驟）。

<a id="package-layout"></a>
## 套件配置

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

只有根目錄的產品檔案、`plugins\OmsiLaunch.*` 與 `.omsilaunch\` 屬於產品所有。`plugins\` 底下的第三方外掛程式，OmsiLaunch 絕不會列舉、複製、計算雜湊、移除或還原。

## `release-manifest.json`

| 欄位 | 型別 | 意義 |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`；必須與 `PublicCapabilityRegistry.ProtocolVersion` 相符 |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | 每個已知配置的 `build_id` 與 `code_sha256`（`Omsi23004`、`13ec110a...c028`）；僅為快速路徑，並非必要條件 |
| `configuration` | string | `Release` 或 `Debug` |
| `generated_utc` | string | ISO-8601 格式的組建時間 |
| `files[]` | object[] | 每個封裝檔案的 `path`（使用正斜線，相對於套件根目錄）、`bytes`、`sha256`（大寫十六進位） |

資訊清單只是資料，絕不是可執行的原則：控制器只讀取其中的 `plugins/` 項目。讀取器接受含或不含 UTF-8 BOM 的檔案（在此修正之前由 Windows PowerShell 5.1 寫入的資訊清單帶有 BOM）。

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## 執行階段對資訊清單的使用（外掛程式完整性）

在每次規劃與啟動之前，`OmsiLaunchService.LoadArtifacts` 會建立預期的外掛程式封閉集（`RuntimeArtifactSet.Load`，`src\OmsiLaunch.Process\RuntimeDeployment.cs`）：

1. 控制器會在 `OmsiLaunch.exe` 旁（`AppContext.BaseDirectory`）尋找 `release-manifest.json`。若存在，`ReleaseManifest.TryReadPluginHashes` 會擷取 `plugins/*` 的雜湊（若無法將該檔案讀取為資訊清單，則為 `OL_E_RELEASE_MANIFEST_INVALID`）。
2. 每個已安裝的檔案 `<root>\plugins\OmsiLaunch.*` 都會計算雜湊（SHA-256）並進行比對：
   - 有資訊清單時：與資訊清單中的雜湊比對；並記錄計畫診斷訊息 `plugin.integrity.reference = manifest`。檔案遺失 → `OL_E_PERMANENT_PLUGIN_MISSING`；檔案存在但未列於清單 → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`；雜湊不同 → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`（`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`）。
   - 沒有資訊清單時（開發配置，或省略了資訊清單的安裝）：只能檢查檔案是否存在，以及與控制器旁封裝副本之間的自我一致性；`plugin.integrity.reference = self`。
3. 任何失敗都會將計畫標示為不可執行（附帶詳細資訊的 `OL_E_RUNTIME_ARTIFACT_MISSING`），或拒絕啟動（結束代碼 `7`）。

工作階段絕不會暫存、建立快照、還原或移除外掛程式檔案；封閉集是安裝中永久存在的一部分。x86 的 `OmsiLaunch.Native.x86.dll` 只會從 `plugins\` 載入；managed 組件宣告了 `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`。

<a id="installation-into-the-omsi-root"></a>
## 安裝到 OMSI 根目錄

1. 驗證封存檔：將 `OmsiLaunch-0.1.0-beta4.zip` 與 `OmsiLaunch-0.1.0-beta4.zip.sha256` 比對。
2. 將封存檔**直接解壓縮到 OMSI 安裝根目錄**（包含 `Omsi.exe` 的目錄）。這會放置根目錄檔案、`plugins\OmsiLaunch.*`（與任何第三方外掛程式並存）以及 `.omsilaunch\`。
3. 將 `release-manifest.json` 保留在 `OmsiLaunch.exe` 旁：它能啟用以資訊清單為基礎的外掛程式完整性檢查。資訊清單與二進位檔必須來自同一個套件：若新的二進位檔搭配較舊的資訊清單（或反之），每次啟動都會以 `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` 失敗。`Test-ReleasePresentation.ps1 -InstallPackage` 現在會將資訊清單連同產品檔案一起複製；它過去會略過資訊清單，導致較舊的資訊清單留在較新的二進位檔旁（Round A RA-007）。
4. 以 `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>` 進行唯讀的一致性檢查：`installation_comparison.coherent_with_package` 必須為 `true`。
5. 不要將外掛程式二進位檔移入 `.omsilaunch\`，也不要重新命名 `plugins\OmsiLaunch.*`。
6. 以 `OmsiLaunch.exe /version`、`OmsiLaunch.exe profiles` 及一次 `/plan` 進行驗證（請見[第一個工作階段](../getting-started/first-session.md)）。

既有的 `.omsilaunch\assets\splash\*.bmp` 檔案絕不會被工作階段覆寫（明確受管理的素材集會持續保留）；藉由解壓縮新套件來覆寫這些檔案，屬於使用者刻意的操作。

<a id="the-omsilaunch-directory-after-use"></a>
## 使用後的 `.omsilaunch` 目錄

| 路徑 | 建立者 | 存續期間 |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | 套件，或在第一次使用 managed 啟動畫面的工作階段時複製 | 永久 |
| `docs\`、`examples\` | 套件 | 永久 |
| `session-profiles\<id>\profile.yaml` | 使用者 | 永久；請見[工作階段設定檔](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | 每個工作階段 | 保留最新的 50 個工作階段；新工作階段啟動時，會刪除較舊的、以工作階段為前綴的檔案 |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | `/runtime`、驗證測試工具 | 相同的保留原則（工作階段前綴） |
| `diagnostics\tray-host.log` | 系統匣指示器 | 永久，以附加方式寫入 |
| `diagnostics\release-presentation-*.out`、`release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | 永久（不以工作階段為前綴） |
| `journal.json` | 交易 | 從 `Prepared` 存在至 `Restored`；若有殘留，表示有待處理的復原（`/recovery-status`） |
| `backup\<sessionId>\<sha256(path)>.bin` | 交易 | 被修改檔案的快照；還原後移除 |

任何資料都不會離開本機。請見[交易與復原](../concepts/transactions-and-recovery.md)。

<a id="uninstall"></a>
## 解除安裝

1. 確認沒有工作階段正在執行（`OmsiLaunch.exe detect`、`OmsiLaunch.exe session status`），且沒有待處理的復原（`OmsiLaunch.exe /recovery-status`；若 `pending` 為 `true`，請執行 `/recover`），使 OMSI 檔案都已還原。
2. 刪除 `plugins\OmsiLaunch.Plugin.opl`、`plugins\OmsiLaunch.PluginNE.dll`、`plugins\OmsiLaunch.Plugin.dll`、`plugins\OmsiLaunch.Plugin.deps.json`、`plugins\OmsiLaunch.Plugin.runtimeconfig.json`、`plugins\OmsiLaunch.Api.dll`、`plugins\OmsiLaunch.Builds.Omsi23004.dll`、`plugins\OmsiLaunch.Interop.dll`、`plugins\OmsiLaunch.Native.x86.dll`。其他外掛程式請保持不動。
3. 刪除上方配置中列出的根目錄產品檔案（`OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.*.dll`、`OmsiLaunch.Controller.*.json`、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md`）。
4. 刪除 `.omsilaunch\`（這會移除您的工作階段設定檔與診斷資料）。`journal.json` 存在時，絕對不要刪除它。

已完成的工作階段會還原它所擁有的每個檔案，因此不需要額外清理。OMSI 本身在執行期間寫入的檔案（例如 `options.cfg` 中的 `[last_map]`、快取、`laststn.osn`、記錄檔）屬於 OMSI 的正常狀態，不會被還原；請見[交易與復原](../concepts/transactions-and-recovery.md)。

<a id="validation-scripts"></a>
## 驗證指令碼

| 指令碼 | 用途 | 是否觸及 OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | 以「警告視為錯誤」組建 `OmsiLaunch.sln`，並透過 MSBuild 組建三個原生專案（`OmsiLaunch.Native.x86` Win32、`OmsiLaunch.Bootstrapper` x64、`OmsiLaunch.WindowsHost` x64），接著執行所有離線測試套件：`OmsiLaunch.TestHost`、`OmsiLaunch.UnitTests`、`OmsiLaunch.IntegrationTests`、`OmsiLaunch.ProfileTests`、`OmsiLaunch.WindowsUiTests`，以及（未略過時的）`OmsiLaunch.DocumentationTests`，最後執行封裝迴歸測試 `Test-PackagingPipeline.ps1`（使用 `-SkipNative` 時略過）。輸出 `OFFLINE VALIDATION PASSED`/`FAILED`。 | 否 |
| `tools\New-ReleasePackage.ps1` | 過時檢查、暫存、資訊清單、完整性自我檢查、ZIP、總和檢查碼（如上所述）。 | 否 |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | 驗證資訊清單恰好列出所有封裝檔案，且大小與 SHA-256 相符；驗證必要的封閉集（三個執行檔、控制器、包含 `OmsiLaunch.Native.x86.dll` 在內的九個永久外掛程式檔案）存在，且組態為 `Release`。搭配 `-InstallationRoot` 時，會以**唯讀**方式比對安裝中的產品檔案與套件。結束代碼 `0` = 一致。 | 否（唯讀） |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | 在 `artifacts\candidate\post-round-a` 下以乾淨的暫存區產生候選套件，並要求已暫存的封閉集與重新解壓縮的封存檔都通過完整性檢查。證明完整性關卡會拒絕：遭竄改的 DLL、遭竄改或舊版的 `Native.x86`、過時的外掛程式副本、已刪除的列出檔案、非預期的檔案、被變更或格式錯誤的資訊清單雜湊、重複項目（完全相同、大小寫不同、分隔符號不同）、上層路徑與絕對路徑，以及無效的 JSON；證明封裝程式會拒絕放在組建輸出中的舊版 `Native.x86`（即使其時間戳記較新）以及原始檔已變更的收據；並證明已發布的封存檔絕不會被覆寫。組建輸出會逐位元組還原。由 `Invoke-OfflineValidation.ps1` 執行。 | 否 |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | 將 ZIP 解壓縮到 `artifacts\release\identity-verification`，檢查 `product`/`product_version`/`package_alias`，並驗證除 `nethost.dll` 與 `YamlDotNet.dll` 以外每個 `.exe`/`.dll` 的 `ProductName`、`CompanyName`、`LegalCopyright`、`FileVersion`、`ProductVersion`，兩個 shim 的 `InternalName`/`OriginalFilename`，以及 `OmsiLaunch.exe` 是否內嵌圖示。 | 否 |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | 針對實際安裝驗證封裝的 Release 執行檔：資訊清單必須為 `Release`、不得包含 `Debug` 或 `runtime/plugin/` 路徑，並須安裝 `plugins/OmsiLaunch.*`；四個啟動畫面素材必須存在。執行三個 `/plan` 案例（managed 預設、managed 自訂素材、`/splash:Unset`）。搭配 `-RunOmsi`（需要 `-InstallPackage`）時，會以 `/observe-seconds` 啟動每個案例，在工作階段期間監看 `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_PTB.bmp`，並斷言精確還原、沒有 `Omsi.exe`、沒有 `journal.json`、結束代碼 `0`、第三方外掛程式雜湊未變，以及永久外掛程式集合未變。寫入 `.omsilaunch\diagnostics\release-presentation-validation.json`。 | 使用 `-RunOmsi` 時為是（限於工作階段範圍，會還原） |

兩個 `Test-*` 指令碼都會讀取 `OmsiLaunch.Version.props` 以得知預期的版本。
