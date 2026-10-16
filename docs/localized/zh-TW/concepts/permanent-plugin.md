# 永久外掛程式

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../concepts/permanent-plugin.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

OmsiLaunch 透過一個外掛程式從 OMSI 處理程序內部控制 OMSI；該外掛程式作為產品的一部分，只安裝一次，位於 `plugins\OmsiLaunch.*` 之下。工作階段從不會暫存、複製、建立快照、還原或移除它。本頁說明封閉集合包含哪些檔案、OMSI 如何載入它、主機如何在每次啟動前驗證它、主機與外掛程式如何溝通（交接區、遙測、執行階段信箱），以及在未透過 OmsiLaunch 啟動 OMSI 時外掛程式會做什麼。來源：`src/OmsiLaunch.Process/RuntimeDeployment.cs`（`RuntimeArtifactSet`、`ReleaseManifest`、三個共用記憶體儲存區）、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`、`src/OmsiLaunch.Plugin/PluginRuntime.cs`、`src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`、`src/OmsiLaunch.Api/StartupHandoff.cs` 與 `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`。

<a id="the-closure-9-files"></a>
## 封閉集合（9 個檔案）

| `plugins\` 下的檔案 | 角色 |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | OMSI 外掛程式描述檔。其內容為 `[dll]`，後接 `OmsiLaunch.PluginNE.dll`。 |
| `OmsiLaunch.PluginNE.dll` | 由 DNNE 2.0.6 產生的原生 x86 匯出 shim。匯出 OMSI 的外掛程式 ABI（`PluginStart`、`PluginFinalize`、`AccessVariable`、`AccessTrigger`、`AccessStringVariable`、`AccessSystemVariable`）並裝載 .NET runtime。帶有產品版本資源。 |
| `OmsiLaunch.Plugin.dll` | 受控外掛程式（`net6.0-windows`，x86）：`CurrentDnneAdapter`、`PluginRuntime`、`CurrentRuntimeControl`、`CurrentTelemetrySink`。 |
| `OmsiLaunch.Plugin.deps.json` | 外掛程式的 .NET 相依性資訊清單（manifest）。 |
| `OmsiLaunch.Plugin.runtimeconfig.json` | .NET runtime 設定（架構 `Microsoft.NETCore.App` 6.0，`win-x86`）。 |
| `OmsiLaunch.Api.dll` | 與主機共用的傳輸格式與公開記錄。 |
| `OmsiLaunch.Builds.Omsi23004.dll` | 組建設定檔：執行檔指紋、全域變數、物件配置、方法位址。 |
| `OmsiLaunch.Interop.dll` | 建立在設定檔之上的處理程序內記憶體讀取器與寫入器。 |
| `OmsiLaunch.Native.x86.dll` | 原生橋接層（C++）：組建驗證、headless 啟動 hook、時間套用、MakeVehicle、PlaceRandomBus、網路材質下載抑制、D3D9 裝置存取。 |

`RuntimeArtifactSet.Load` 從控制器自己的 `plugins\` 目錄推導出這份清單：四個具名檔案（`.opl`、`PluginNE.dll`、`deps.json`、`runtimeconfig.json`）、該目錄中除 `OmsiLaunch.PluginNE.dll` 以外的每個 `OmsiLaunch.*.dll`，以及原生橋接層。`OmsiLaunch.Plugin.dll` 必須在其中。任意 DLL 絕不會被一併帶入 OMSI。發行打包工具（`tools/New-ReleasePackage.ps1`）只寫入上述九個檔案，不多也不少。

<a id="how-omsi-loads-it"></a>
## OMSI 如何載入它

1. OMSI 列舉 `plugins\*.opl`，並載入 `OmsiLaunch.Plugin.opl` 中指定的 DLL：`OmsiLaunch.PluginNE.dll`。
2. DNNE shim 在 OMSI 處理程序內啟動 `OmsiLaunch.Plugin.runtimeconfig.json` 所描述的 x86 .NET 6 runtime，並解析 `OmsiLaunch.Plugin.dll` 中的受控匯出。
3. OMSI 呼叫 `PluginStart`。OMSI 在啟動期間可能不只一次呼叫它；只有第一次呼叫會被採用（`Interlocked.Exchange` 防護），因為成功的啟動會擁有一個只能使用一次的原生 hook。之後的呼叫會立即返回。
4. `PluginStart` 首先檢查 `OMSILAUNCH_INTERNET_TEXTURES_MODE`：當其為 `Disabled` 時，套用原生下載器抑制（`internet-textures.suppressed`，或 `internet-textures.suppression.failed`）。
5. `PluginRuntime.Start` 讀取交接資料（見下文）、在處理程序內驗證組建、設置（arm）headless 啟動 hook、開啟執行階段信箱，並透過 `SetTimer` 回呼將世界啟動排程到 OMSI 的 UI 執行緒上。執行階段命令絕不會在 IPC 背景工作執行緒上執行；所有工作都在計時器回呼中、於 OMSI 原本的 UI 執行緒上執行。
6. `PluginFinalize` 停止計時器、關閉 runtime（`NativeD3DShutdown`），並還原網路材質修補。

`AccessVariable`、`AccessTrigger`、`AccessStringVariable` 與 `AccessSystemVariable` 匯出是空的；OmsiLaunch 不使用 OMSI 的腳本變數外掛程式通道。

<a id="integrity-validation-before-every-start"></a>
## 每次啟動前的完整性驗證

`RuntimeArtifactSet.ValidateInstalled` 在 `StartSessionAsync` 期間（早期復原之後、準備交易之前）以及 `PlanSessionAsync` 期間（僅檢查存在與否，透過 `LoadArtifacts`）執行。計畫診斷訊息 `plugin.integrity.reference` 會回報所使用的參考來源。

| 情況 | 參考 | 每個檔案的檢查 | 錯誤 |
| --- | --- | --- | --- |
| `OmsiLaunch.exe` 旁有 `release-manifest.json`（已安裝的套件） | `manifest` | 已安裝的檔案必須存在，且其 SHA-256 必須等於資訊清單中 `plugins/<name>` 的項目 | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（資訊清單中沒有某個必要檔案的項目）、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| 沒有資訊清單（開發配置） | `self` | 存在性加上自我一致性：已安裝檔案的雜湊必須等於控制器自己 `plugins\` 目錄中的檔案 | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| 資訊清單無法讀取或格式錯誤（缺少 `files` 陣列、項目缺少 `path`/`sha256`） | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| 控制器目錄中的封閉集合不完整 | | | `OL_E_RUNTIME_ARTIFACT_MISSING`（計畫不可執行）；當 `plugins\OmsiLaunch.Plugin.opl` 或 `OmsiLaunch.Native.x86.dll` 不存在時，CLI 還會回報 `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` |

只會使用資訊清單中的 `plugins/` 項目；資訊清單是資料，絕非政策。資訊清單由發行打包工具產生，含有每個暫存檔案的 SHA-256。當控制器從 OMSI 根目錄執行時（發行配置），來源與目的地是同一個目錄；因此在沒有資訊清單時，檢查會降級為存在性與自我一致性，這就是發行套件一律附帶 `release-manifest.json` 的原因。不相符時的修正方式：重新安裝套件，使 `plugins\` 與 `release-manifest.json` 一致。

在修補任何內容之前，外掛程式會針對載入的映像解析並驗證組建目錄中的每個原生目標，並將已驗證的表格交給 `OmsiLaunch.Native.x86.dll`（後者不含任何 OMSI 位址）（遙測 `native.capabilities`）。`boot.headless-start` 或世界模式功能無法使用的啟動，會以 `plugin.capability.unresolved`（`OL_E_CAPABILITY_UNRESOLVED`）失敗；處於非預期狀態的即時啟動表單，則會以 `plugin.build.invalid`（`OL_E_BUILD_VALIDATION_FAILED`）失敗。請參閱[相容性](../reference/compatibility.md)。

<a id="host-to-plugin-environment-variables"></a>
## 主機到外掛程式：環境變數

`CreateProcessW` 以父處理程序的環境加上下列變數啟動 `Omsi.exe`：

| 變數 | 值 | 使用者 |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | 工作階段 GUID（`D` 格式） | `CurrentRuntimeControl` 以它標記 D3D handle（`N` 格式） |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` 以唯讀方式開啟此對應 |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`、`Disabled` 或 `Override` | `PluginStart`（只有 `Disabled` 在處理程序內有作用） |

這三個對應由主機在處理程序啟動前建立（`CurrentStartupHandoffStore`、`CurrentTelemetryStore`、`CurrentRuntimeCommandStore`），並在工作階段的生命週期工作結束時處置。它們是具名核心物件，使用啟動者的預設 DACL；同一使用者的任何處理程序都可以開啟它們（已接受的同使用者信任模型，請參閱[已知限制](../reference/known-limitations.md)）。

<a id="the-startup-handoff"></a>
## 啟動交接

一個記憶體對應、唯讀、固定配置的記錄（`StartupHandoffWire`，magic `OLSH`，版本 4；讀取器仍接受版本 3）。64 位元組的標頭包含 magic、版本、標頭大小、總大小、工作階段 GUID、承載大小，以及承載的 SHA-256。承載包含 `BuildProfileId`、`MapIdentity`、`EntrypointIdentity`、`SituationIdentity`（長度前綴的 UTF-8）、`PresentedEntrypointIndex`、`WorldMode`、旗標（`HeadlessStart`、`PlayerVehicleEnabled`）、`DateMode` 與 `TimeMode`。外掛程式會重新計算承載的雜湊並拒絕任何不符（`plugin.handoff.invalid`，主機錯誤 `OL_E_PLUGIN_PROTOCOL_MISMATCH`）。大於 1 MiB 或大小不一致的對應也會以相同方式被拒絕。

只有當 `WorldMode` 為 `NewMap` 或 `SavedSituation`、已設定 `HeadlessStart`、未設定 `PlayerVehicleEnabled`、日期與時間模式皆為 `Unset`，且已儲存情境指明其 `.osn` 時，外掛程式才接受交接資料。其他任何情況都是 `plugin.request.unsupported`（主機錯誤 `OL_E_CAPABILITY_UNAVAILABLE`）。主機一律設定 `HeadlessStart`。

<a id="telemetry-slot"></a>
## 遙測槽位

`OmsiLaunch.Telemetry.<session>` 是一個事件環狀緩衝區（Beta 4，`TelemetryRingWire`）：64 位元組的標頭（magic `OLTR`、版本 1、槽位數、槽位大小、已提交的序號）與 64 個編號的 2048 位元組槽位，每個槽位存放一個 UTF-8 JSON 事件 `{ "name": ..., "data": { ... }, "tick": <ms> }`（`tick` 是外掛程式單調遞增的工作階段時鐘）。外掛程式是唯一的寫入者：它先使槽位失效、寫入、提交該槽位的序號，然後發布環狀緩衝區的序號。主機每 100 ms 依序讀取自上次讀取以來提交的每個事件；複製期間被覆寫的槽位會被計為遺失，絕不會以撕裂的形式交付。落後超過 64 個事件的主機會收到帶有確切計數的執行階段事件 `telemetry.lost`。大於槽位的事件會被指名它的 `telemetry.oversized` 取代。事件會附加到 `SessionStatus.RuntimeEvents`（上限為最後 256 個），並驅動語意生命週期（`plugin.started`、`world.starting`、`gameplay.entered`、模組與失敗事件）。

<a id="runtime-command-mailbox"></a>
## 執行階段命令信箱

`OmsiLaunch.Runtime.<session>` 是一個 64 KiB 的單一飛行中請求信箱（mailbox）：`state`（位移 0 的 int32：0 閒置、1 已請求、2 已回應）、`length`（位移 4 的 int32）、位移 8 的封套。封套是 `RuntimeCommandWire` 記錄（magic `OLRC`，版本 1，72 位元組標頭，含種類、總長度、工作階段 GUID、請求 id、承載長度，以及 UTF-8 JSON 承載的 SHA-256）。外掛程式從其 UI 執行緒計時器輪詢信箱（世界載入後為 50 ms）、在該執行緒上執行命令，並且只在槽位仍保有相同請求 id 時才發布回應；主機因逾時而放棄的請求永遠不會得到回應。過大的回應會被替換為具型別的 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 錯誤。細節與逾時請參閱[執行階段控制](../reference/runtime-control.md)。

<a id="dll-search-policy"></a>
## DLL 搜尋原則

`OmsiLaunch.Plugin`、`OmsiLaunch.Interop`、`OmsiLaunch.Process` 與 CLI 組件都宣告 `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`。原生匯入（`OmsiLaunch.Native.x86.dll`、`user32.dll`、`kernel32.dll`）只會從組件本身的目錄（`plugins\`）或 Windows 系統目錄解析；絕不會探測 OMSI 根目錄與 `PATH`。因此 `OmsiLaunch.Native.x86.dll` 只會從 `plugins\` 載入，不會從其他任何地方載入。

<a id="when-omsi-is-started-without-omsilaunch"></a>
## 未透過 OmsiLaunch 啟動 OMSI 時

由於封閉集合是永久安裝的，OMSI 每次啟動都會載入 `OmsiLaunch.PluginNE.dll`，包括從 Steam 或桌面啟動。在這種情況下：

- 沒有 `OMSILAUNCH_INTERNET_TEXTURES_MODE`，因此不會套用下載器修補。
- 沒有 `OMSILAUNCH_HANDOFF_NAME`，因此 `PluginRuntime.Start` 發出 `plugin.handoff.invalid` 並回傳 `false`。當 `OMSILAUNCH_TELEMETRY_NAME` 未設定時，`CurrentTelemetrySink.Emit` 會立即返回，因此不會寫入任何地方。
- 沒有組建驗證、沒有原生 hook、沒有信箱、沒有計時器。外掛程式保持載入但不作用；OMSI 的行為就如同外掛程式不存在一樣。
- OMSI 結束時的 `PluginFinalize` 會呼叫原生還原常式與 `NativeD3DShutdown`，在沒有安裝任何東西時兩者皆為無作業。

因此，若要正常執行 OMSI，不需要移除 `.opl`。

<a id="non-interference-with-third-party-plugins"></a>
## 不干擾第三方外掛程式

工作階段從不列舉、計算雜湊、複製、移除或還原 `plugins\` 中的其他檔案。外掛程式不使用 OMSI 的 `AccessVariable` 通道，也不觸碰其他外掛程式的狀態。唯一的處理程序內修補是：已建立設定檔的 headless 啟動 hook（為工作階段設置的一次性 VMT 重新導向）、選用的網路材質下載抑制（於 `PluginFinalize` 中還原），以及用於材質生命週期追蹤的 D3D9 裝置 `Reset` 攔截。

<a id="difference-from-omsihook"></a>
## 與 OmsiHook 的差異

OmsiLaunch 對 OmsiHook 或任何 OmsiHook 二進位檔**沒有 runtime 相依性**：唯一參考的套件是 `DNNE` 2.0.6 與 `YamlDotNet` 15.1.2，產品中任何地方都沒有 `using OmsiHook`，也沒有對 OmsiHook DLL 的 P/Invoke。OmsiLaunch 與 OmsiHook 共有的是衍生知識：物件配置與數個讀取包裝器，是從固定版本的 OmsiHook checkout（`space928/Omsi-Extensions`，commit `7687b6623f5f74b4419695257bd2a4eef54dd93e`，LGPL-3.0-only）對照確切的 `Omsi23004_692EBFBF` 執行檔核對而來。署名與授權條款位於 `THIRD-PARTY-NOTICES.md`，逐檔重用矩陣位於 `third_party/OMSIHOOK-REUSE-MATRIX.md`。OmsiHook 注入一個獨立處理程序並公開原始指標；OmsiLaunch 在處理程序內執行，只公開不透明、以工作階段為範圍的 handle，並從公開結果中移除任何原生位址（請參閱[功能](../reference/capabilities.md)）。
