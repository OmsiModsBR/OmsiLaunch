# HOF 管理

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/hof-management.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

OMSI 會從每輛車輛的資料夾讀取 HOF 檔案（`*.hof`：車輛的 IBIS／目的地顯示器所顯示的目的地、路線編號與站點清單）。HOF 管理會將 HOF 檔案複製到車輛資料夾中，**僅在單一工作階段期間有效**：地圖或附加元件可以隨附其路線所需的 HOF，使用者也可以將某一輛車輛的 HOF 重複用於其他車輛，而不會永久變更任何車輛。本頁是規範性參考，涵蓋由 `src/OmsiLaunch.Core/HofPlanner.cs` 中的 `HofPlanner` 所實作的內容、[LaunchSpec](launchspec.md) 的 `HofManagement` 欄位、`/hof-source`、`/hof-target`、`/hof-file` 與 `/hof-mode` 等 [CLI](cli.md) 旗標，以及[工作階段設定檔](session-profiles.md)的 `hofs` 區塊。

穩定性：規劃、衝突規則、路徑限制與交易式還原為 `STABLE_BETA`（離線關卡 `hof.plan-sources-and-modes`、`hof.plan-rejects-unsafe-input`、`hof.transaction-exact-restore`、`hof.recovery-after-crash`、`session-profiles.hofs`）。已於執行階段驗證：具備精確還原的鏡像工作階段，以及透過 `/recover` 進行的當機復原（請見[執行階段驗證狀態](../status/runtime-validation-status.md)）。

HOF 管理與 `PlayerVehicle.Hof` / `/hof:<id>` 不同；後者用於在啟動時選取玩家車輛的 HOF，在此組建中為 `UNAVAILABLE`。

<a id="model"></a>
## 模型

一個請求是一份操作清單（`HofManagementSpec.Operations`）。每個 `HofOperationSpec` 具有：

| 欄位 | 意義 |
| --- | --- |
| `SourceKind`, `Source` | HOF 檔案的來源（見下表）。 |
| `TargetVehicles` | 一或多輛已安裝的車輛：`.bus` 或 `.ovh` 檔案的識別（`Vehicles\MAN_SD200\MAN_SD77.bus`），或車輛資料夾（`Vehicles\MAN_SD200`），兩者都位於 `Vehicles\` 之下。同一資料夾中的多個識別只會指向該資料夾一次。 |
| `Mode` | `Add`（預設）：將來源 HOF 複製到每個目標資料夾；該處的其他 HOF 保持不變。`Mirror`：複製來源 HOF，並在工作階段期間移除目標資料夾中所有其他 `.hof`，使該資料夾的 HOF 集合與來源集合相同。 |
| `Files` | 選用的檔案名稱清單，指定要從來源取用的檔案（不區分大小寫）。省略時：取用來源中直接存在的所有 `*.hof`。列出的名稱若不存在於來源中：`OL_E_HOF_SOURCE_NOT_FOUND`。 |

| `SourceKind` | `Source` | 解析為 |
| --- | --- | --- |
| `Collection` | 單純的名稱，例如 `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`：由使用者或工具維護的集合。 |
| `Directory` | 相對於安裝的目錄，例如 `maps\RMG Leste\hof` | 該目錄中的 `*.hof`：隨地圖或附加元件內容一起提供的 HOF。 |
| `Vehicle` | 車輛識別或資料夾，與目標相同 | 該車輛資料夾中的 `*.hof`：將某一輛車輛的 HOF 重複用於其他車輛。 |
| `File` | 相對於安裝的 `.hof` 檔案 | 該單一檔案。 |

只會使用直接位於來源目錄中的檔案（不遞迴）。從不追蹤符號連結與目錄連接（junction）（請見[路徑限制](#confinement)）。

<a id="what-a-session-does"></a>
## 工作階段的執行內容

1. **規劃**（`PlanSessionAsync`、`/plan`）。每個操作都會被解析：讀取來源、檢查目標、偵測衝突。每個將會變更的檔案都會出現在 `SessionPlan.PlannedMutations` 中，其 `SemanticKey` 為 `hof.add` 或 `hof.mirror`，`RequestedValue` = 來源識別，`Operation` 為 `exact-file-overlay`（寫入）或 `session-deletion`（由鏡像移除）；這些路徑屬於 `TouchedFiles`。任何問題都會使計畫不可執行，並附上下列代碼。
2. **啟動**。復原失效的日誌之後，會依據實際檔案再次解析各項操作，並交給工作階段交易：寫入的檔案為 overlay（暫時覆蓋），被鏡像移除的檔案為工作階段刪除。若目標檔案的位元組已與來源相同，則保持不動，也不會成為交易項目。
3. **工作階段結束、失敗、當機。** 交易會精確還原每個目標資料夾：原本存在的檔案取回其原始位元組、時間戳記與屬性；工作階段建立的檔案會被移除；被鏡像移除的檔案會重新出現。擁有者當機後，相同的還原會在下次啟動時或透過 `/recover` 執行（請見[交易與復原](../concepts/transactions-and-recovery.md)）。

目標資料夾以外的任何內容都不會被寫入。來源檔案只會被讀取。工作階段從不修改 `.omsilaunch\hof\`。

<a id="conflict-rules"></a>
## 衝突規則

各項操作依序套用，且彼此必須一致：

- 兩個操作以相同位元組寫入同一目標檔案時會合併；位元組不同時：`OL_E_HOF_CONFLICT`。
- 鏡像將移除某個先前操作所寫入的檔案，或某個操作寫入先前鏡像所移除的檔案：`OL_E_HOF_CONFLICT`。
- 鏡像涵蓋資料夾在工作階段中的 HOF 集合：現有檔案加上先前操作寫入該處的檔案。
- HOF 目標同時也由工作階段呈現（啟動畫面（splash）或 Internet Textures（網路材質））寫入：`OL_E_HOF_CONFLICT`（以目前的呈現路徑不會發生，因為這些路徑位於 `Vehicles\` 之外）。
- 當目標中已有名稱相同但大小寫不同的檔案時，保留現有的拼寫。

<a id="confinement"></a>
## 路徑限制

每個來源與目標都必須位於安裝內：根路徑、`..` 區段以及離開根目錄的路徑皆為 `OL_E_HOF_PATH_ESCAPE`；位於 `Vehicles\` 之外的目標（以及 `Vehicle` 來源）、不是單純目錄名稱的集合名稱，以及透過目錄連接或符號連結抵達的任何來源或目標資料夾，也同樣如此。本身為重新分析點（reparse point）的 `.hof` 檔案會被忽略。

<a id="limits"></a>
## 限制

| 限制 | 值 | 錯誤 |
| --- | --- | --- |
| 單一 HOF 檔案的大小 | 8 MiB | `OL_E_HOF_INVALID` |
| 每個操作的檔案數 | 256 | `OL_E_HOF_INVALID` |
| 每個工作階段寫入的總位元組數 | 256 MiB | `OL_E_HOF_INVALID` |
| 每個操作的目標數 | 至少 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## 錯誤碼

| 代碼 | 原因 |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | 集合、目錄、車輛或檔案不存在、不包含任何 `.hof`，或缺少 `Files` 中指定的檔案。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | 目標車輛檔案或資料夾不存在。 |
| `OL_E_HOF_PATH_ESCAPE` | 路徑離開安裝、目標不在 `Vehicles\` 之下、集合名稱不是單純名稱，或經過了目錄連接／符號連結。 |
| `OL_E_HOF_CONFLICT` | 兩個操作對同一目標檔案的處理不一致（請見[衝突規則](#conflict-rules)）。 |
| `OL_E_HOF_INVALID` | 沒有目標、沒有來源、`File` 來源不是 `.hof`、目標是 `.bus`/`.ovh` 以外的檔案，或超出[限制](#limits)。 |

<a id="using-it"></a>
## 使用方式

命令列（一個操作；多個操作需要 `/spec` 或工作階段設定檔）：

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec（`/spec` JSON，列舉以整數表示）：

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

工作階段設定檔的預設組合（preset）（`package` 來源是設定檔套件內的目錄）：

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API：設定 `LaunchSpec.HofManagement`，並如常呼叫 `PlanSessionAsync` / `StartSessionAsync`；`/list:Hofs`（`ContentQueryKind.Hofs`）會列出已安裝的 HOF 檔案。

相關內容：[LaunchSpec](launchspec.md)、[工作階段設定檔](session-profiles.md)、[CLI](cli.md)、[錯誤](errors.md)、[交易與復原](../concepts/transactions-and-recovery.md)。
