# 相容性

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/compatibility.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

OmsiLaunch 可與已安裝的 **OMSI 2** 搭配運作。它不要求某個特定的 `Omsi.exe` 檔案：它在 OMSI 內所做的每一項原生假設（函式、修補位置、類別、VMT 槽位、全域變數、欄位位移）在使用之前都會在處理程序內單獨解析並驗證，只有在其所有假設都已驗證時，功能才可用。本頁說明「已安裝 OMSI 2」的意義、如何識別組建、如何解析功能，以及作業系統與 runtime 需求。來源：`src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`、`src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`、`src/OmsiLaunch.NativeResolution/`、`src/OmsiLaunch.Plugin/PluginNativeTargets.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`。

<a id="requirement-omsi-2-installed"></a>
## 需求：已安裝 OMSI 2

只有當安裝不是 OMSI 2 安裝時，計畫才會以 `OL_E_UNSUPPORTED_BUILD` 被拒絕：

| 檢查 | 在下列情況被拒絕 |
| --- | --- |
| `Omsi.exe` | 安裝根目錄中不存在 |
| 映像 | 不是 32 位元 x86 PE 映像 |
| 版本 | 其版本資源不帶有 OMSI 2 版本（2.x） |
| 安裝 | 根目錄沒有 `maps\` 或 `Vehicles\` 目錄 |

執行檔的 SHA-256、大小與確切版本**不是**相容性條件。它們會記錄為診斷訊息（`omsi.build.detected`、`OmsiLaunch.exe profiles`），並作為證據。

<a id="build-identity-and-capability-resolution"></a>
## 組建識別與功能解析

| 概念 | 意義 |
| --- | --- |
| 程式碼識別 | 執行檔主要程式碼區段的 SHA-256。已知的識別會選取已知配置，作為快速路徑。僅標頭不同的變體（例如 Large Address Aware 副本）共用同一個程式碼識別。 |
| 已知配置 | `Omsi23004`：OMSI 2.3.004（檔案版本 2.2.032），程式碼識別 `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`。[狀態頁面](../status/runtime-validation-status.md)中的每一項執行階段驗證都是在此配置上進行的。 |
| 原生目標 | 原生邊界的一項假設，連同其防護宣告於組建目錄中。 |
| 功能 | 一組目標，例如 `world.new-map` 或 `time.read`。只有在每個目標都已解析**且**已驗證時才可用。 |

在 OmsiLaunch 修補任何內容之前，目標在外掛程式中如何被解析：

1. **已知識別：**先嘗試已知配置的參考位址，且它仍必須通過該目標的防護。
2. **否則，或防護失敗時**，改試與組建無關的策略：
   - Delphi RTTI 類別名稱（檢查 `vmtSelfPtr`）；
   - 類別的已發佈方法、欄位與屬性（RTTI 表）；
   - 持有已解析方法的 VMT 槽位；
   - 必須恰好符合一次的遮罩程式碼簽章（依賴配置的運算元為萬用字元），可擷取位址、全域變數（直接或經由其指標儲存格），或從欄位所屬類別的某個方法內擷取欄位位移。
3. **沒有任何項目唯一符合並通過防護：**該目標維持未解析。**不會退回到固定位址。**

防護：函式與修補位置為遮罩的進入位元組，被修補的呼叫為呼叫目的地，類別為 `vmtSelfPtr` 與 RTTI 類別名稱，VMT 槽位為所持有的方法，全域變數為可寫入的資料區段。執行階段讀取器讀取的物件配置（車輛、人物、天氣、時刻表記錄等）目前還沒有與組建無關的結構驗證。它們只在已知程式碼識別下才會被驗證，因此讀取它們的功能在識別未知的組建上無法使用。

在已知配置上，每項功能都會解析。在識別未知的組建上，其目標可透過上述策略解析的功能（無介面啟動、NEW_MAP、已儲存情境、Internet Textures 抑制、時間讀取與設定、D3D）可能可用，且每項都會單獨驗證；依賴配置的讀取則無法使用。

<a id="where-compatibility-is-decided"></a>
## 相容性在何處決定

| 階段 | 發生的事 |
| --- | --- |
| 規劃（`PlanSessionAsync`、`/plan`） | OMSI 2 檢查（`omsi.omsi2`，否則為 `OL_E_UNSUPPORTED_BUILD`）。檔案映像會作為**預測**被解析：必要的原生功能帶有 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED`，或帶有警告 `OL_W_CAPABILITY_PENDING` 的 `PENDING_IN_PROCESS_RESOLUTION`。未知的程式碼識別會加上警告 `OL_W_BUILD_UNRECOGNIZED`。警告不會使計畫不可執行。 |
| 啟動（`StartSessionAsync`） | 重新規劃規格；不再可執行的計畫會以 `OL_E_PLAN_NOT_RUNNABLE` 拒絕。 |
| 處理程序內（外掛程式啟動） | 解析載入的映像（具權威性；受 DRM 保護的執行檔可能與其檔案不同）。報告會以遙測 `native.capabilities` 送出。若 `boot.headless-start` 與世界模式功能（`world.new-map` 或 `world.saved-situation`）無法使用，工作階段會在任何 hook 之前以 `OL_E_CAPABILITY_UNRESOLVED` 失敗，並還原檔案。若它們可用，但即時的啟動表單不在預期狀態（例如另一個外掛程式修補過它），工作階段會以 `OL_E_BUILD_VALIDATION_FAILED` 失敗。 |
| 執行階段操作 | 功能無法使用的操作會以 `OL_E_CAPABILITY_UNRESOLVED`（`Data["capability"]`）拒絕，且不會觸及 OMSI 記憶體。 |

`OmsiLaunch.exe profiles --json` 會回報安裝（`is_omsi2`、`build_id`、`identity`、檔案與程式碼雜湊、`file_variant`）、已知配置，以及由檔案預測的功能。

<a id="operating-system-and-architecture"></a>
## 作業系統與架構

`CurrentWindowsX64Platform.Detect` 會計算 `RuntimePlatformInfo`。只有在下列條件全部成立時，目前平台才受支援：

| 需求 | 檢查 | 違反時的錯誤 |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 或更新版本 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016+） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 位元 Windows 與 64 位元主機處理程序 | `OSArchitecture == X64` 且 `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 可寫入的安裝 | 根目錄存在、不是唯讀，且包含 `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` 也會回報 `OmsiArchitecture` 與 `PluginArchitecture` 為 `X86`（OMSI 是 32 位元處理程序；外掛程式檔案集為 x86，在 WOW64 下執行）、`LegacyPlatform=false` 以及 `Wow64Available`。ARM64 Windows 即使具備 x64 模擬也不受支援，因為主機處理程序本身必須是 x64。

<a id="net-requirements"></a>
## .NET 需求

| 元件 | Runtime | 備註 |
| --- | --- | --- |
| 控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6，x64 | 原生啟動載入器透過套件隨附的 `nethost.dll` 經由 `hostfxr` 尋找 runtime。缺少 runtime 時由 shim 回報（結束代碼 100-106；請參閱 [CLI](cli.md) 與[結束代碼](exit-codes.md)）。 |
| 外掛程式檔案集（`plugins\OmsiLaunch.Plugin.dll`，經由 `OmsiLaunch.PluginNE.dll`） | .NET 6，**x86**（`net6.0-windows`、`win-x86`），由 DNNE 2.0.6 在 `Omsi.exe` 內部裝載 | 電腦上必須安裝 x86 的 .NET 6 Desktop／Core runtime；僅有 64 位元 runtime 並不足以執行外掛程式。 |
| 原生橋接層（`plugins\OmsiLaunch.Native.x86.dll`） | 原生 x86 | 只會從 `plugins\` 載入（請參閱[永久外掛程式](../concepts/permanent-plugin.md)）。它不含任何 OMSI 位址：它從外掛程式接收已驗證的目標表。 |

<a id="legacy-platforms"></a>
## 舊版平台

Windows 7、Windows 8.x、Windows XP 以及其他 NT 6 及更早的系統不在目前的支援範圍內。`RuntimePlatformInfo.LegacyPlatform` 一律為 `false`，也不存在任何舊版轉接器；此欄位與 `IPluginNativeServices` 接縫之所以存在，只是為了讓未來能在不變更公開 API 的情況下加入舊版轉接器（請參閱 `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`）。此版本中沒有任何部分能在這些系統上執行。

<a id="steam-and-large-address-aware-notes"></a>
## Steam 與 Large Address Aware 注意事項

- 僅標頭不同的 Large Address Aware 副本與其原始檔案具有相同的程式碼識別，並使用相同的已知配置。
- Steam 執行檔在磁碟上受 DRM 保護，因此其檔案可能與載入的映像不同：計畫可能回報 `OL_W_CAPABILITY_PENDING`，由處理程序內的解析決定。使用真正 Steam 執行檔的工作階段尚未經過執行階段驗證（請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)）。
- Steam 會自行啟動 OMSI；工作階段必須透過 `OmsiLaunch.exe` 啟動，才會有交接資料。若從 Steam 啟動，永久外掛程式會保持不作用（沒有交接資料，也沒有 hook）。

<a id="related-pages"></a>
## 相關頁面

- [已知限制](known-limitations.md)
- [執行階段驗證狀態](../status/runtime-validation-status.md)
- [安裝](../getting-started/installation.md)
