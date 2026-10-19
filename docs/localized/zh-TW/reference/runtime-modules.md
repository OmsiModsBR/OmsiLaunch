# 執行階段模組

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/runtime-modules.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

執行階段模組（runtime module）為受管理的 OMSI 工作階段增添行為：一項隨工作階段執行的功能（例如地圖的 HUD 或載入最佳化），而不會成為 OmsiLaunch 本身的一部分。橋接層會載入它、只提供它所宣告的內容，並在工作階段結束或模組失敗時移除它持有的一切。本頁是執行階段模組的規範性參考，涵蓋 `src/OmsiLaunch.Runtime.Modules`（資訊清單、設定、資格、生命週期主機）、規劃器、控制器的工作階段程式（`ModuleSessionPrograms`、`ModuleSessionProcess`）、外掛程式的模組環境，以及 [CLI](cli.md) 的模組旗標。

穩定性：`EXPERIMENTAL`。橋接協定 1 已在 B 輪次於執行階段驗證（受管理模組、內容啟用、操作、`disable-module` 與 `fail-session`；含 IAT hook 的原生元件；請見[執行階段驗證狀態](../status/runtime-validation-status.md)）。橋接協定 2（設定、預設啟用、設定檔、工作階段程式、外掛程式衝突、通知、`get_value`）僅經離線驗證。離線關卡：`tests/OmsiLaunch.ModuleTests` 的 `module.*` 關卡（資訊清單、完整性、選取與啟用、設定、設定檔、外掛程式衝突、透過其 job 啟動的工作階段程式、在每個階段於每種失敗原則下注入的失敗、原生元件生命週期、清理、交接）、`plugin-runtime.modules-lifecycle`（由外掛程式環境載入的真實受管理模組）、`runtime-modules.native-component`（掛接匯入並透過橋接層讀取值的真實原生元件）、`runtime-modules.plugin-conflict` 與 `native-hooks.self-check`（hook 引擎）。橋接協定 3（Beta 5：處理程序內的目錄操作、tick、輸入擷取、社群內容輸入、帶有規劃時快照的選用釘選值、`module pack`、輔助工作選項、OMSI 版本）僅經離線驗證：`tests/OmsiLaunch.ModuleTests` 的 `module.*` 關卡（`manifest.protocol-3`、`manifest.optional-pins`、`catalog.snapshot`、`content.realmap-package`、`packer.pins`、`handoff.v8-snapshot`、`lifecycle.runtime-operations`、`lifecycle.tick.*`、`lifecycle.input-capture`、`lifecycle.auxiliary-options`、`lifecycle.snapshot-before-components`）、外掛程式整合測試的 `runtime-modules.protocol-3`、`runtime-modules.auxiliary-job`、`ui-pause.fails-closed` 與 `ui-input.hook-self-check`，以及單元測試的 `omsi-version.literal`。暫停選單接管與輸入擷取尚未在真實的 OMSI 工作階段中觀察到。

載入是開放的：安裝在 `plugins\OmsiLaunch.Modules` 之下的任何模組都可被載入，不論由誰製作。OmsiLaunch 不為模組在 OMSI 內的行為背書。它所保證的是[邊界](#security-boundary)：模組絕不在 OmsiLaunch 處理程序內執行，且 OmsiLaunch 提供的任何功能都不會讓模組超出 OMSI 自身的權限。

<a id="where-modules-live"></a>
## 模組的存放位置

每個模組是永久外掛程式資料夾下的一個目錄：

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

目錄名稱必須等於模組的 `id`。模組由其發行者安裝；工作階段絕不會在該處寫入、建立快照或還原檔案，模組也不會透過 OmsiLaunch 變更安裝檔案（資訊清單無法宣告安裝 overlay）。模組可寫入的資料位於其[資料目錄](#data-directory)。

<a id="manifest-modulejson"></a>
## 資訊清單（`module.json`）

結構描述 `omsilaunch.runtime-module/v1`。未知的鍵會被拒絕，因此為較新結構描述撰寫的資訊清單會被拒絕，而不是被只理解一半。所有路徑都相對於模組目錄（繫結的內容路徑相對於 OMSI 根目錄），使用 `\` 或 `/`，且不得包含 `..` 或磁碟機代號。

| 鍵 | 必要 | 意義 |
| --- | --- | --- |
| `schema` | 是 | `omsilaunch.runtime-module/v1`。 |
| `id` | 是 | 模組 id：小寫字母、數字與 `-`，2 至 48 個字元，以字母或數字開頭。同時也是其目錄名稱與操作命名空間。 |
| `version` | 是 | `major.minor.patch`，可選擇性加上 `-suffix`。 |
| `name` | 否 | 顯示名稱（最多 128 個字元）。 |
| `bridge_protocol` | 是 | `{ "min": n, "max": n }`：模組可搭配運作的橋接協定。此組建提供協定 `1`、`2` 與 `3`；當模組的範圍包含其中之一時即被接受。 |
| `activation` | 否 | `requested`（預設）：在工作階段要求時載入。`content`：在工作階段執行所繫結的地圖時也會載入（需要 `content_binding`）。`default`（協定 2）：除非被停用，否則每個工作階段都會載入（請見[為工作階段選擇模組](#choosing-modules-for-a-session)）；不可有 `content_binding`。 |
| `required_capabilities` | 否 | 模組缺少即無法運作的功能（請見[功能](#capabilities)）。 |
| `optional_capabilities` | 否 | 模組在可用時使用的功能；模組以 `IsCapabilityAvailable` 檢查。 |
| `content_binding` | 否 | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`：模組只在該地圖上、且這些檔案未變更時才執行。 |
| `assets` | 否 | 模組讀取的檔案，每個為 `{ "path", "sha256" }`。從協定 3 起 `sha256` 為選用（請參閱[完整性](#integrity)）。 |
| `components` | 是 | 恰好一個 `managed` 元件 `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`；可選擇性加上一個 `native` 元件（`.dll`）、`auxiliary` 元件（`.exe`），以及在協定 2 下的一個 `host` 元件（`.exe`，即模組的[工作階段程式](#session-programs)）。從協定 3 起，元件的 `sha256` 為選用，且 `auxiliary` 元件可帶有 `priority`（`normal`、`below-normal` 或 `idle`）與 `memory_limit_mb`（64 至 65536）：程式的 job 物件自其第一個指令起就會套用它們（請參閱[輔助程式](#auxiliary-programs)）。 |
| `failure_policy` | 是 | `fail-session`、`disable-module` 或 `degrade`（請見[失敗](#failures)）。 |
| `operations` | 否 | 模組在工作階段執行期間公開的操作：`{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`。 |
| `settings` | 否 | 協定 2。模組接受的設定（請見[設定](#settings)）。 |
| `conflicts` | 否 | 協定 2。`{ "plugins": [ "<file>.dll" ] }`：不得與模組並行執行的 OMSI 外掛程式 DLL（請見[與 OMSI 外掛程式的衝突](#conflicts-with-omsi-plugins)）。 |
| `runtime_operations` | 否 | 協定 3。模組在處理程序內執行的公開功能目錄操作，例如 `["time.read", "camera.set"]`（請參閱[處理程序內的操作](#operations-in-process)）。需要 `bridge.runtime` 功能。 |
| `tick_ms` | 否 | 協定 3。模組希望每 `tick_ms` 毫秒收到一次週期性回呼：50 的倍數，介於 50 至 1000（請參閱 [tick](#tick)）。需要 `bridge.tick` 功能。 |
| `content_inputs` | 否 | 協定 3。模組讀取、由橋接層定位的社群內容：`[ { "kind": "realmap-package", "required": false } ]`（請參閱[社群內容](#community-content)）。 |
| `timeouts` | 否 | 以毫秒為單位的預算：`arm_ms`、`gameplay_ready_ms`、`stop_ms`（各預設 5000，上限 30000）；在協定 2 下，工作階段程式另有 `prepare_ms`（預設 120000，上限 1800000）與 `host_stop_ms`（預設 10000，上限 60000）。操作預設為 2000 ms。 |
| `diagnostics` | 否 | `{ "trace": true\|false }`。 |

使用協定 2 功能（`settings`、`conflicts`、啟用 `default`、`host` 元件、`prepare_ms` 或 `host_stop_ms`）的資訊清單必須宣告 `bridge_protocol.min` 為 2。使用協定 3 功能（`runtime_operations`、`tick_ms`、`content_inputs`、`bridge.runtime`、`bridge.tick` 或 `bridge.input` 功能、輔助的 `priority` 或 `memory_limit_mb`，或沒有 `sha256` 的元件或資產）的資訊清單必須宣告 `bridge_protocol.min` 為 3。

`components` 與 `assets` 所指名的每個檔案都可以其 SHA-256 固定（協定 3 之前為必要，從協定 3 起為選用：請參閱[完整性](#integrity)）。`module.json` 無法固定自身。

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## 完整性

資訊清單所指名的每個檔案都必須存在且不得經由連接點（junction）或符號連結到達，作者所固定的每個檔案都必須符合其固定的 SHA-256；否則模組會以 `OL_E_MODULE_INTEGRITY` 被拒絕。這能攔下損毀或只更新一半的模組。這不是信任決策：模組不會對照任何清單、簽章或發行者進行檢查。

從協定 3 起，固定值為選用，因此作者不必手動維護雜湊，而計畫一律會取得**快照**：對資訊清單所指名的每個檔案及其內容計算的單一 SHA-256，記錄於計畫中（`PlannedModule.SnapshotSha256`），並透過啟動 handoff 傳遞給外掛程式（線路版本 8）。快照保證計畫所評估的檔案就是實際執行的檔案：外掛程式只有在模組的 `module.json` 具有規劃所評估的 SHA-256、且其檔案仍具有快照的內容時才載入該模組；控制器與橋接層會在工作階段程式、原生元件或輔助程式啟動前，再次比對快照。規劃之後檔案已變更的模組不會被載入（`OL_E_MODULE_INTEGRITY`，「模組的檔案在規劃之後已變更」）。快照涵蓋每個模組，無論是否有固定值；固定值則讓發行模組的人在模組被探索到時，就能偵測損毀或只更新一半的副本。

在模組程式碼啟動之處會重複此檢查：外掛程式只有在模組的 `module.json` 仍具有規劃所評估的 SHA-256、且每個固定項與快照仍成立時才載入該模組；控制器只有在驗證該程式的固定項與快照之後，才會啟動工作階段程式。

### Packing a module

`OmsiLaunch.exe /pack-module:<module directory>` 會為 `module.json` 所指名的檔案（其元件與資產）寫入固定值，因此作者不必手動編輯 SHA-256。它只改寫 `sha256` 值（資訊清單其餘部分會保留；檔案會重新縮排），在寫入任何內容之前先以資訊清單剖析器重新讀取結果，並回報每個檔案（`module.pack` 封套：`module`、`directory`、`written`、`intact`、`files`，其中含 `path`、`sha256`、`pinned`、`mismatch`、`written`）。它作用於所給定的目錄，絕不會動到任何安裝。`/verify-module:<module directory>` 回報相同內容但不寫入（`module.verify`）；當已固定的檔案與其固定值不相符時，結束代碼為 `7`（沒有固定值的檔案不算不相符）。對於無法剖析的資訊清單或遺失的檔案，兩者的結束代碼皆為 `2`。

<a id="security-boundary"></a>
## 安全邊界

OmsiLaunch 不控制模組在 OMSI 內做什麼：其受管理與原生元件在 OMSI 處理程序中執行，權限與任何 OMSI 外掛程式相同。OmsiLaunch 保證下列事項：

1. OmsiLaunch 處理程序絕不載入模組程式碼。模組程式碼在 OMSI 內、在由 OMSI 內部啟動的輔助程式中，或在模組的工作階段程式中執行。
2. OmsiLaunch 為模組啟動的程式是模組自己目錄中的檔案，已對照其固定項驗證，並以 OmsiLaunch 固定的命令列啟動。工作階段程式在 job 物件中執行，該物件會讓它隨工作階段結束，不允許第二個處理程序（它無法啟動其他程式），並拒絕變更系統參數、顯示設定、桌面，以及登出或關機。它只繼承其兩個管道 handle。
3. 模組所宣告的一切都留在其目錄內，其可寫入的資料則在 `.omsilaunch\module-data\<id>` 內。
4. OmsiLaunch 的通道（本機控制、啟動交接、遙測、執行階段命令）維持限制於目前使用者並繫結到工作階段；沒有任何一個讓呼叫端能啟動程式、讀寫安裝以外的檔案，或變更系統設定。
5. OmsiLaunch 絕不提升權限。模組以啟動工作階段之使用者的權限執行。
6. 來自模組的文字（通知、事件、值）被視為資料：有長度限制、不含控制字元，絕不執行、開啟或當作連結跟隨。

<a id="capabilities"></a>
## 功能

`required_capabilities` 與 `optional_capabilities` 所指名的，要不是執行中組建的原生功能（[功能目錄](capabilities.md#capability-catalog)的原生功能欄，例如 `time.read` 或 `d3d.texture`），就是橋接功能：

| 橋接功能 | 模組取得的內容 |
| --- | --- |
| `bridge.events` | `Emit`：執行階段事件 `module.<id>.<name>`。 |
| `bridge.background-threads` | `RunBackground`：受追蹤的背景執行緒。 |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`：繫結到工作階段的輔助處理程序。 |
| `bridge.hooks.iat` | 原生元件：對 `Omsi.exe` 匯入的 hook（`iat:<dll>!<function>`）。 |
| `bridge.hooks.d3d9` | 原生元件：對執行中 Direct3D 9 裝置之方法的 hook（`d3d9:device.<method>`）。需要原生功能 `d3d.texture`。 |
| `bridge.runtime` | 協定 3。`IRuntimeModuleContext.Runtime` 與原生 `run_operation`：資訊清單在 `runtime_operations` 中列出的目錄操作。 |
| `bridge.tick` | 協定 3。週期性回呼：`ITickingModule.Tick` 與原生匯出 `omsilaunch_module_native_v1_tick`。 |
| `bridge.input` | 協定 3。`IRuntimeModuleContext.Input`：為模組自己的選單擷取遊戲的鍵盤與滑鼠訊息。需要原生功能 `ui.input.capture`。 |

兩者皆不是的名稱會在規劃時被拒絕（`OL_E_MODULE_CAPABILITY_UNRESOLVED`）。在沒有已知配置的 OMSI 2 組建上，可執行檔無法解析的原生功能會在處理程序內判定；若在那裡也無法解析，則不載入模組。

<a id="choosing-modules-for-a-session"></a>
## 為工作階段選擇模組

模組以四種方式之一加入工作階段；`PlannedModule.Activation` 會說明是哪一種：

| 啟用 | 時機 |
| --- | --- |
| `requested` | 工作階段要求它：`LaunchSpec.Modules.Enable`、`/module:<id>`，或所選工作階段設定檔 preset 中的 `enabled: true`。 |
| `settings-file` | 在[設定檔](#the-settings-file)其區段中的 `enabled=true`。 |
| `content` | 其啟用為 `content`，且工作階段執行其繫結的地圖。 |
| `default` | 其啟用為 `default`。 |

模組會被 `LaunchSpec.Modules.Disable`、`/no-module:<id>`、preset 中的 `enabled: false`，或設定檔中的 `enabled=false` 排除在工作階段之外（計畫診斷訊息 `module.disabled`）。明確的要求優先於設定檔。在同一個要求中啟用又停用同一個模組，會得到 `OL_E_MODULE_SETTING_INVALID`。

規劃規則：被要求但無法載入的模組，會使計畫不可執行並帶有其 `OL_E_MODULE_*` 碼。自行加入（設定檔、內容、預設）而無法載入的模組不會使工作階段停止：計畫會回報 `OL_W_MODULE_NOT_LOADED`。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## 設定

模組在 `settings` 中宣告其設定；每個項目：

| 鍵 | 必要 | 意義 |
| --- | --- | --- |
| `key` | 是 | `[a-z][a-z0-9_]{0,31}`，不可為 `enabled`。 |
| `type` | 是 | `boolean`、`integer`、`choice` 或 `text`。 |
| `default` | 是 | 沒有其他來源設定時的值：布林值、整數、字串，或在 `auto` 為 true 時為 `"auto"`。 |
| `auto` | 否 | `true`：接受值 `auto` 並傳給模組，由模組在執行時決定。 |
| `min`, `max` | 否 | 整數界限。 |
| `choices` | `choice` 時必要 | 接受的值（`[a-z0-9][a-z0-9_.-]{0,31}`，不可為 `auto`）。 |
| `max_length` | 否 | 文字長度上限（1..4096，預設 256）。控制字元會被拒絕。 |
| `flag` | 否 | 此設定的命令列旗標，`[a-z][a-z0-9-]{2,31}`：`/<flag>:<value>`，布林值可單獨使用 `/<flag>` 表示 `true`。 |
| `description` | 否 | 最多 256 個字元。 |

值會經過正規化：布林值接受 `true`/`false`、`yes`/`no`、`on`/`off`、`1`/`0`；選項比對不分大小寫；整數會對照其界限檢查。

每個設定的有效值依序為：

1. 要求：`LaunchSpec.Modules.Settings`（`"<module-id>.<key>": "<value>"`）、`/module-setting:<module-id>.<key>=<value>`、模組自己的旗標，或所選工作階段設定檔 preset 中的 `modules.<id>.settings`。與其他每個 preset 欄位一樣，命令列不能同時設定 preset 已設定的鍵（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；
2. [設定檔](#the-settings-file)；
3. 資訊清單預設值。

無效的被要求值，或模組未宣告的被要求鍵，會使計畫不可執行（`OL_E_MODULE_SETTING_INVALID`）。無法使用的設定檔項目會被忽略並帶有 `OL_W_MODULE_SETTING_IGNORED`，針對工作階段未載入之模組的被要求設定亦同。計畫會在 `PlannedModule.Settings` 中列出每個有效值及其來源（`request`、`settings-file` 或 `default`）。

模組會收到每個已宣告的鍵及其有效值：`IRuntimeModuleContext.Settings`、原生元件中的 `get_value("setting.<key>")`，以及工作階段程式內容的 `settings` 物件。

<a id="the-settings-file"></a>
### 設定檔

`<OMSI root>\.omsilaunch\modules.ini`，位於工作階段設定檔旁。OmsiLaunch 只讀取它。

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

區段是模組 id；`key=value` 行設定各項設定，`enabled` 為每個工作階段開啟或關閉模組；以 `;` 或 `#` 開頭的行是註解。檔案最大為 256 KiB。其無法使用的任何內容（不是模組 id 的區段、位於區段之外的行、未知的鍵、無效的值）都會以 `OL_W_MODULE_SETTING_IGNORED` 回報並被忽略。

<a id="session-programs"></a>
## 工作階段程式

模組可宣告一個 `host` 元件：其工作階段程式，即來自其自身目錄的可執行檔。當模組在計畫中時，控制器會：

1. 驗證程式的固定項並建立模組的資料目錄；
2. 在 OMSI 之前，從模組目錄以命令列 `"<program>" --omsilaunch-session <session id>` 啟動它，並在它進入其 job 之前保持暫停（請見[安全邊界](#security-boundary)）；
3. 將內容寫成其標準輸入的第一行；
4. 等待程式寫出 `{"type":"ready"}`，最多 `prepare_ms`；之後才啟動 OMSI；
5. OMSI 建立之後，寫入 `{"type":"omsi-started","pid":<process id>}`；
6. 在工作階段結束時，於 OMSI 結束之後、任何檔案被還原之前，寫入 `{"type":"stop"}`，最多等待 `host_stop_ms`，然後結束該程式。

兩個方向都使用 UTF-8 JSON，每行一個物件。內容：

| 欄位 | 意義 |
| --- | --- |
| `type` | `context`。 |
| `schema` | `omsilaunch.module-session/v1`。 |
| `session_id`, `module_id`, `module_version`, `product_version` | 身分識別。 |
| `installation_root`, `module_directory`, `data_directory` | 完整路徑。 |
| `channel` | 程式可為 OMSI 中模組元件提供服務的具名管道（named pipe）：`\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`。程式建立它、限制於目前使用者，並檢查用戶端是 OMSI 的處理程序。元件以 `get_value("session_channel")` 或 `IRuntimeModuleContext.SessionChannelName` 讀取它。 |
| `settings` | 模組的有效設定。 |
| `world` | 工作階段的 `mode`、`map`、`situation`、`entrypoint_index`、`entrypoint`。 |
| `session_files` | 工作階段交易所擁有、相對於安裝的檔案。程式不得保持它們開啟或映射：它們會在工作階段結束時還原。 |

程式除 `ready` 之外可以寫入的內容：

| 行 | 效果 |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | 執行階段事件 `module.<id>.<name>`。 |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | 執行階段事件 `module.<id>.notice`（請見[通知](#notices)）。 |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | 帶有該已編目錄碼的工作階段診斷訊息（否則為 `OL_E_MODULE_OPERATION_FAILED`）。 |

其他行、超過 64 KiB 的行，以及 10,000 行之後的任何內容都會被忽略。控制器會記錄 `module.<id>.host.started`、`host.ready`（含 `prepare_ms`）、`host.failed`、`host.exited` 與 `host.killed`。

在 `prepare_ms` 內未就緒、在就緒前結束，或其固定項不成立的程式，會使其模組的準備失敗：在 `fail-session` 下工作階段不會啟動（`OL_E_MODULE_SESSION_FAILED`）；在 `disable-module` 下程式會被結束，模組也不納入 OMSI；在 `degrade` 下模組保留（若程式仍能執行則繼續執行）。工作階段的 `StartupTimeoutSeconds` 從 OMSI 啟動時開始計算，因此準備不會消耗它。

<a id="conflicts-with-omsi-plugins"></a>
## 與 OMSI 外掛程式的衝突

`conflicts.plugins` 指名不得與模組並行執行的 OMSI 外掛程式 DLL，例如安裝相同 hook 的較舊外掛程式。規劃時，OmsiLaunch 會讀取每個 `plugins\*.opl` 的 `[dll]` 區段；若其中之一載入衝突的 DLL，模組會以 `OL_E_MODULE_CONFLICT` 被拒絕，且原因會指出該 `.opl`。在 OMSI 內，若衝突的 DLL 仍被載入，則不載入該模組。OmsiLaunch 絕不變更其他外掛程式的檔案；是否關閉它由使用者決定。

<a id="notices"></a>
## 通知

名為 `notice` 的模組事件（來自 `Emit("notice", ...)`、原生元件的 `emit`，或工作階段程式的通知行）會由 OmsiLaunch 系統匣以氣球提示顯示，其標題會指出模組。只保留 `title`（最多 63 個字元）與 `text`（最多 255 個字元），控制字元會被移除，其中任何內容都不會被開啟或執行。必須在 OMSI 啟動之前（系統匣尚未顯示時）通知使用者的工作階段程式，使用自己的視窗。

<a id="data-directory"></a>
## 資料目錄

`<OMSI root>\.omsilaunch\module-data\<id>\` 是模組在此安裝上可寫入的目錄：`IRuntimeModuleContext.DataDirectory`、`get_value("data_directory")`，以及工作階段程式的 `data_directory`。它在首次使用時建立。它不屬於工作階段交易：模組寫在那裡的內容會在工作階段之後保留。`/clear-module-data:<id>` 會刪除它（且不刪除其他任何內容）；當有工作階段擁有該安裝時會被拒絕，經由連結到達的目錄會以連結的方式移除，絕不跟隨。

<a id="lifecycle"></a>
## 生命週期

| 步驟 | 位置 | 發生的事 |
| --- | --- | --- |
| 1. 探索 | 控制器 | 讀取 `plugins\OmsiLaunch.Modules\*`：資訊清單與完整性（`ListRuntimeModulesAsync`、`/list:RuntimeModules`）。 |
| 2. 資格 | 控制器，規劃時 | 評估[加入工作階段](#choosing-modules-for-a-session)的模組：可用性、橋接協定、功能、內容繫結、外掛程式衝突、設定。結果為 `SessionPlan.PlannedModules`。 |
| 3. 準備 | 控制器，啟動時 | 啟動並等待模組的[工作階段程式](#session-programs)。接著符合資格的模組透過啟動交接（版本 6）傳送給外掛程式：其目錄、每個已評估 `module.json` 的 SHA-256，以及其有效設定。 |
| 4. 裝配（Arm） | 外掛程式，世界啟動之前 | 每個模組都會重新驗證並載入（受管理元件位於其自己的載入內容中）。其原生元件（若有）會先載入並啟動，然後呼叫 `Arm(context)`。 |
| 5. 遊戲就緒 | 外掛程式 | 在 `gameplay.entered` 之後，當已載入的模組宣告 `bridge.hooks.d3d9` 時，橋接層開始觀察 Direct3D 裝置（`d3d.ready`）；然後呼叫原生元件的 `gameplay_ready` 與 `GameplayReady()`。對於 NEW_MAP 工作階段，會再次檢查繫結的地圖與啟動所載入的地圖是否一致。 |
| 6. 執行階段 | 外掛程式 | 操作 `module.<id>.<operation>` 抵達 `Execute`。具有 `tick_ms` 的模組會收到 tick（請參閱 [tick](#tick)）；具有 `runtime_operations` 的模組會在處理程序內執行它們（請參閱[處理程序內的操作](#operations-in-process)）。 |
| 7. 停止 | 外掛程式，然後是控制器 | 工作階段結束時（或模組失敗時），呼叫 `Stop()`。OMSI 結束之後，停止工作階段程式。 |
| 8. 清理 | 外掛程式 | 模組追蹤的一切都以相反順序釋放並驗證已消失；原生元件最後停止並移除其 hook；其 DLL 保持載入、處於惰性狀態，直到 OMSI 結束（執行緒可能仍正從 detour 返回）。 |

每個處理程序內的回呼都在 OMSI 的 UI 執行緒上執行，如同產品自身的執行階段操作。橋接層會對照預算衡量每個回呼：延遲返回的回呼會使其階段失敗（`OL_E_MODULE_PHASE_TIMEOUT`）。橋接層無法中斷永不返回的回呼；這樣的模組會讓 OMSI 停止運作，而工作階段會在其啟動逾時結束，或與任何其他工作階段一樣以 `session stop` 結束。

<a id="failures"></a>
## 失敗

失敗是指任何階段或操作中的例外或超時、在處理程序內未解析的功能、在處理程序內不成立的內容繫結、在 OMSI 中載入的衝突外掛程式，或未就緒的工作階段程式。模組的 `failure_policy` 決定後續處理。在每種情況下，模組的資源都會被釋放。拋出例外、回傳非零或超出其間隔的 tick，與操作一樣屬於失敗。

| 原則 | 在準備、載入、裝配或遊戲就緒中失敗 | 在操作中失敗 | 在停止中失敗 |
| --- | --- | --- | --- |
| `fail-session` | 模組被釋放；`module.session-failure`；工作階段以 `OL_E_MODULE_SESSION_FAILED` 失敗，OMSI 被停止且檔案被還原。世界啟動之前的失敗會阻止啟動。 | 相同。 | 已回報（`module.failed`）；工作階段繼續到其結束。 |
| `disable-module` | 模組被釋放並停用；工作階段繼續。 | 相同。 | 已回報；模組被釋放。 |
| `degrade` | 與 `disable-module` 相同，但工作階段程式的失敗會讓模組留在工作階段中。 | 失敗的呼叫回傳其碼；模組保持啟用；`module.degraded` 每個碼只回報一次。 | 已回報；模組被釋放。 |

已停用或已失敗的模組會以 `OL_E_MODULE_DISABLED` 回應其操作。模組失敗會以其 `OL_E_MODULE_*` 碼顯示為工作階段診斷訊息（`SessionStatus.Diagnostics`），也顯示為執行階段事件。

<a id="cleanup"></a>
## 清理

模組透過其內容取得的內容都會被追蹤：以 `Track` 註冊的資源、以 `RunBackground` 啟動的執行緒、以 `StartAuxiliaryProcess` 啟動的處理程序。在停止或失敗時，橋接層會取消 `Stopping`，然後以相反順序釋放它們。輔助處理程序也會放入關閉即終止（kill-on-close）的 job 物件，因此即使 OMSI 被終止，它們也會隨 OMSI 結束；工作階段程式的 job 會讓它隨 OmsiLaunch 工作階段結束，即使 OmsiLaunch 本身被終止亦然。釋放之後仍存活的資源（未結束的處理程序、未結束的執行緒）會回報為 `OL_E_MODULE_CLEANUP_INCOMPLETE`。模組不透過其內容建立的執行緒或處理程序則不會被追蹤。

當機之後，模組沒有需要復原的內容：它們不持有安裝檔案，其記憶體、執行緒與繫結到 job 的處理程序會隨 OMSI 處理程序或 OmsiLaunch 處理程序結束。

<a id="native-components-and-hooks"></a>
## 原生元件與 hook

原生元件是實作 `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` 中 C 介面的 32 位元 DLL：

| 匯出 | 呼叫時機 |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | 在裝配時，早於受管理的 `Arm`。非零值使該階段失敗。 |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | 選用；在進入遊戲之後。 |
| `void omsilaunch_module_native_v1_stop(void)` | 在停止或失敗時，早於其 hook 被移除。之後橋接表拒絕每個呼叫，而 DLL 保持載入、處於惰性狀態，直到 OMSI 結束。 |

橋接表提供給元件 `request_hook`、`emit`（執行階段事件 `module.<id>.<name>`，帶有字串的 JSON 物件），以及從協定 2 起的 `get_value`。元件在使用 `get_value` 之前會讀取表的 `size`（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`）。`get_value(context, name, buffer, capacity, length)` 將值以 UTF-8 並以結尾零複製，並將 `*length` 設為不含結尾零所需的位元組數；它回傳 `0`、`1`（引數）、`2`（未知名稱）、`11`（停止之後）或 `12`（緩衝區太小）。從協定 3 起，該表還包含 `run_operation`（請參閱[處理程序內的操作](#operations-in-process)）與 `d3d_is_game_device(context, device)`：Direct3D hook 收到的裝置指標是否為遊戲自己的裝置（`1`）或另一個裝置（`0`；車輛選擇表單會以相同的方法表建立第二個裝置，因此 `Present` detour 會為兩者呼叫，且應只為遊戲的裝置繪製），尚未觀察到任何裝置時為 `10`，未宣告 `bridge.hooks.d3d9` 或停止之後為 `11`。該指標只會被比較，絕不會被使用，且橋接層絕不會交出任何指標。在預設集區中保留 Direct3D 資源的模組，需自行 hook `d3d9:device.Reset`（呼叫原始函式之前所做的是「裝置遺失」，回傳成功之後所做的是「裝置已還原」）。元件在使用協定 3 成員之前會檢查表的 `size`（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`、`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`）。兩個選用匯出完成元件：`int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)`（請參閱 [tick](#tick)）與 `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`，這是受管理元件透過 `IRuntimeModuleContext.NativeComponent.Execute` 呼叫原生元件的方式（輸入 JSON、輸出 JSON，緩衝區慣例與 `run_operation` 相同）。`get_value` 回答的名稱：

| 名稱 | 值 |
| --- | --- |
| `setting.<key>` | 已宣告設定的有效值。 |
| `data_directory` | 模組的[資料目錄](#data-directory)。 |
| `module_directory` | 模組自己的目錄。 |
| `session_id` | 工作階段 id。 |
| `module_version` | 資訊清單版本。 |
| `product_version` | OmsiLaunch 產品版本。 |
| `session_channel` | [工作階段程式的](#session-programs)管道；對沒有工作階段程式的模組為未知。 |
| `omsi_version` | 遊戲對自身回報的 OMSI 產品版本（協定 3；請參閱 [OMSI 版本](#the-omsi-version)）。 |

hook 僅以符號目標要求，在 `start` 或 `gameplay_ready` 期間，且目標的功能須為資訊清單所宣告者：

| 目標 | Hook | 功能 |
| --- | --- | --- |
| `iat:<dll>!<function>` | `Omsi.exe` 的一個匯入，例如 `iat:kernel32.dll!ReadFile`。 | `bridge.hooks.iat` |
| `d3d9:device.<method>` | 執行中 Direct3D 9 裝置的 `Reset`、`Present`、`BeginScene` 或 `EndScene`（在裝置存在時於 `gameplay_ready` 中要求）。 | `bridge.hooks.d3d9` |

`request_hook` 在 `*original` 中回傳用於延續鏈的函式（detour 必須呼叫它）。其結果：`0` 成功、`1` 引數、`2` 未知目標、`3` 該位置不含應有內容、`4` 該位置被橋接層以外的對象 hook、`5` 記憶體保護、`6` 登錄已滿、`7` 順序、`8` 記憶體、`9` 有執行緒正在執行要修補的位元組、`10` 尚無 Direct3D 裝置、`11` 目前不允許或未宣告功能。

橋接層安裝的每個 hook，無論為產品或為模組，都連同其擁有者記錄在同一個登錄中（模組為 `module:<id>`）：

- hook 只會安裝在該位置應有的值之上（匯入的函式；名為 `d3d9.dll` 的模組的函式、系統 runtime 或封裝層），或堆疊在登錄自己在該位置的最上層 hook 之上。已被其他對象變更的位置會被拒絕（`4`），絕不覆寫。
- 同一位置上的 hook 以後進先出的順序移除；上方有另一個 hook 的 hook 會保留到那一個被移除為止。
- 產品自己的 Direct3D `Reset` hook 在登錄中，因此模組的裝置 hook 堆疊在它之上。
- 清理時，橋接層會在 `stop` 返回之後移除模組的每個 hook。若有一個無法移除（有其他對象變更了該位置），清理會回報 `OL_E_MODULE_CLEANUP_INCOMPLETE`。原生元件的 DLL 在 OMSI 執行期間絕不卸載，因此仍在返回途中的 detour 保持有效。

引擎也支援以 trampoline 對函式 prologue 進行 hook（位元組變更期間其他執行緒會被暫停，並先檢查確切的預期位元組），但目前尚未向模組提供任何 prologue 目標：這類目標來自組建的目標目錄，並附有已驗證的位元組。

<a id="operations"></a>
## 操作

模組操作為 `module.<id>.<operation>`。它只存在於計畫已載入該模組的工作階段（`PlannedModule.Operations`）；否則 `ExecuteRuntimeAsync` 會回應 `OL_E_MODULE_NOT_LOADED` 或 `OL_E_MODULE_OPERATION_UNKNOWN`。必要引數會被檢查，未宣告的引數會被拒絕（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。模組的具型別失敗在其為已編目錄的公開碼時保留其碼；任何其他碼會變成 `OL_E_MODULE_OPERATION_FAILED`，模組的碼則放在 `Values["module_code"]`。遵循內部命名慣例的結果鍵會被移除，與每個執行階段操作相同。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

## Auxiliary programs

模組以 `StartAuxiliaryProcess` 啟動其某個 `auxiliary` 元件（其自身目錄中已固定的 `.exe`）。從協定 3 起，橋接層會以**暫停**狀態建立該程式，將它放入工作階段的 job 物件，然後才恢復執行，因此它立即啟動的子處理程序不可能在 job 之外執行。當模組停止或 OMSI 結束時，job 會終止該程式及其啟動的所有項目（關閉時終止），並自該程式的第一個指令起套用資訊清單的 `priority`（`below-normal`、`idle`）與 `memory_limit_mb`。與[工作階段程式](#session-programs)不同，輔助程式的 job 允許子處理程序（內嵌 Web 檢視的程式需要它們）；每一個子處理程序都在 job 之中。啟動之前會再次驗證該程式：有固定值時驗證其固定值，並一律驗證計畫的快照。

## Operations in process

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)`（受管理）與 `run_operation`（原生）會在處理程序內、於 OMSI 的 UI 執行緒上，執行[公開功能目錄](capabilities.md#public-runtime-operations)中的操作，使用與執行階段控制通道相同的登錄、相同的具型別 `OL_E_*` 結果、相同的引數檢查與相同的變更原則，且不經過該通道。只有資訊清單在 `runtime_operations` 中列出的操作才被允許：任何其他操作會在抵達 OMSI 之前以 `OL_E_MODULE_OPERATION_NOT_DECLARED` 回應，且 `internal.*` 與 `module.*` 操作絕不能被列出。若組建未解析某操作的原生功能，則與其他各處一樣回應 `OL_E_CAPABILITY_UNRESOLVED`。

此清單是透明度與規劃資訊，而非安全邊界：模組本來就以使用者的權限在 OMSI 內執行。計畫會顯示它（`PlannedModule.RuntimeOperations`），而變更 OMSI 狀態的操作會在其中帶有其變更原則，例如 `camera.set (omsi-state)`。模組自 `Arm` 起即可執行操作（此時世界可能尚不存在；該操作隨後會回應它自己的具型別錯誤）。結果有上限（1 MiB）；更大的結果為 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`。模組停止之後，每次呼叫都回應 `OL_E_MODULE_DISABLED`。

原生 `run_operation(context, operation, arguments_json, buffer, capacity, length)` 接受目錄 id 與字串的 JSON 物件（或 null），並寫入 `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`。當它已回應時回傳 `0`（操作本身的失敗在 JSON 之內），引數問題回傳 `1`，停止之後回傳 `11`，緩衝區太小時回傳 `12`（`*length` 為所需大小）。

## Tick

宣告了 `tick_ms` 與 `bridge.tick` 功能的模組，大約每 `tick_ms` 毫秒會被呼叫一次，在 OMSI 的 UI 執行緒上，由外掛程式的 50 ms UI 計時器觸發：受管理元件上的 `ITickingModule.Tick(TimeSpan elapsed)`，以及選用的原生匯出 `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`。間隔為 50 的倍數，介於 50 與 1000 之間。tick 並非逐訊框：要繪圖的模組應 hook Direct3D 的 `Present` 方法，並以 tick 讀取資料。tick 在 `gameplay.entered` 之後開始，並隨模組一起停止。拋出例外、回傳非零（原生）或耗時超過其間隔的 tick，屬於該模組的失敗：在 `degrade` 下，每個代碼只回報一次（`module.degraded`），模組繼續執行（錯過的 tick 會被略過，絕不會排入佇列）；在 `disable-module` 與 `fail-session` 下則套用該原則。OMSI 的巢狀強制回應迴圈會讓 UI 計時器持續觸發，因此 tick 絕不會重入自身。

## Community content

`content_inputs` 宣告模組所讀取的內容，其作者在建置模組時無法得知，因為這些內容是由社群在模組之後為某張地圖製作的。沒有 SHA-256 可固定；橋接層只負責定位它並檢查讀取是否安全。目前唯一的種類是 `realmap-package`：地圖的 `openomsi-realmap` 資料夾，即 `<OMSI root>\maps\<folder>\openomsi-realmap`，由 openOMSI 地圖套件工具寫入。

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` 回答該地圖是否有套件（沒有套件不是錯誤），有的話則回傳其目錄、檔案數、總大小與 `Fingerprint`。指紋是針對每個檔案的相對路徑、大小與寫入時間的變更偵測器：請將它當作快取鍵；它絕不是完整性檢查或信任決策。套件必須位於安裝之內，其任何部分都不得是重新分析點，且有上限（50,000 個檔案、4 GiB）；否則回應為 `OL_E_MODULE_CONTENT_INPUT_INVALID`。未宣告該種類的模組會收到 `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`。套件是第三方製作的資料：請在受管理程式碼中讀取，將每個值視為不受信任，並依資源降級。當工作階段的地圖沒有套件時，`required: true` 的輸入會使計畫拒絕該模組（`OL_E_MODULE_CONTENT_BINDING`）。

## Input capture

以自己的選單取代 OMSI 暫停選單的模組，需要遊戲的鍵盤與滑鼠。OMSI 透過 DirectInput 讀取模擬按鍵，並透過傳給主表單檢視視窗的視窗訊息讀取滑鼠；當其自身的暫停開啟時（原生暫停位元組），模擬、轉譯、3D 點擊與 DirectInput 命令由 OMSI 自己擱置。`IRuntimeModuleContext.Input`（功能 `bridge.input`，原生功能 `ui.input.capture`）補上其餘部分：`Capture(true)` 會在 UI 執行緒上安裝一個執行緒區域的 `WH_GETMESSAGE` hook，屬於遊戲視窗樹（主表單、其子項與隱藏的原生暫停表單）的鍵盤與滑鼠訊息會被記錄為單純的 `ModuleInputEvent` 值（種類、虛擬鍵、檢視視窗的用戶端座標、按鈕、滾輪差量、修飾鍵）並從佇列中移除，因此 OMSI 與 VCL 都不會對其採取動作。`Drain()` 回傳這些事件（最多保留 256 個）。沒有任何東西被子類別化，沒有 hook 任何 user32 按鍵狀態函式，也沒有送出任何東西。

一次只有一個模組可以擷取。無論 `Stop` 做了什麼，擷取都會在模組停止或失敗時釋放。原生強制回應對話方塊（儲存、選項、新遊戲）需要真實輸入：請在要求之前先釋放擷取。hook 的機制已以替身視窗於離線驗證（`ui-input.hook-self-check`）；它們對真實遊戲視窗的行為尚未被觀察到。

## The OMSI version

`IRuntimeModuleContext.OmsiVersion` 與 `get_value("omsi_version")` 提供遊戲對自身回報的 OMSI 產品版本，例如 `2.3.004`：即 OMSI 寫在其 `logfile.txt` 第 7 行 `Version:` 之後的文字，由執行中的映像讀取。它**絕不是**可執行檔的 PE 檔案版本（參考組建帶有 `FileVersion` `2.2.032`）。當映像沒有唯一的這類文字時，若已知程式碼身分則使用組建設定檔的版本，否則為 `unknown`。可執行檔的雜湊、其程式碼大小、位址與設定檔名稱都不會公開。請將版本用於文字與選擇行為；功能（`IsCapabilityAvailable`）仍是判斷何者可運作的唯一可信來源。

<a id="writing-a-module-contracts"></a>
## 撰寫模組（合約）

受管理元件參考 `OmsiLaunch.Runtime.Modules` 與 `OmsiLaunch.Api` 並實作 `IRuntimeModule`。橋接層將這兩個參考繫結到它自己已載入的組件（不論外掛程式在哪個載入內容中執行），因此隨模組附帶的副本會被忽略：

| 成員 | 呼叫時機 |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | 步驟 4。透過內容取得資源。 |
| `void GameplayReady()` | 步驟 5。 |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | 步驟 6，針對每個已宣告的操作（不含 `module.<id>.` 前綴）。對具型別失敗擲回 `ModuleOperationException(code, message)`。 |
| `void Stop()` | 步驟 7。 |

`IRuntimeModuleContext` 提供 `SessionId`、`ModuleId`、`ModuleDirectory`、`Settings`、`DataDirectory`、`ProductVersion`、`OmsiVersion`、`SessionChannelName`、`Stopping`（取消權杖）、`IsCapabilityAvailable`、`Emit`、`Track`、`RunBackground`、`StartAuxiliaryProcess`，以及從協定 3 起的 `Runtime`（請參閱[處理程序內的操作](#operations-in-process)）、`Content`（請參閱[社群內容](#community-content)）、`Input`（請參閱[輸入擷取](#input-capture)）與 `NativeComponent`（已載入的原生元件，或 null）。想要 tick 的受管理元件還要實作 `ITickingModule`。context 中沒有任何內容公開位址、可執行檔的雜湊或其程式碼配置。元件必須可由 32 位元外掛程式載入（AnyCPU 或 x86，.NET 6）；放在它旁邊的 `OmsiLaunch.Runtime.Modules` 或 `OmsiLaunch.Api` 副本會被忽略。

<a id="known-limitations"></a>
## 已知限制

- 原生元件可以 hook `Omsi.exe` 的匯入與 Direct3D 裝置方法；對 OMSI 函式 prologue 與呼叫位置的 hook 目前尚未向模組提供。
- 暫停選單接管（`ui.pause.*`）與輸入擷取經過靜態設計並於離線驗證；它們尚未在真實的 OMSI 工作階段中觀察到，且 `ui.pause-menu.replace` 僅在已知程式碼身分上解析（其部分欄位沒有與組建無關的推導方式）。
- 處理程序內的內容繫結檢查使用啟動所要求的地圖（NEW_MAP）；對於已儲存情境，繫結會在規劃時對照該情境的地圖檢查。
- 裝置 hook 是 vtable hook。當另一個元件將裝置的原始方法寫回時（在迴歸安裝上於遊戲開始後不久觀察到），裝置 hook 會停止被呼叫且無法移除；清理接著會回報 `OL_E_MODULE_CLEANUP_INCOMPLETE`，且元件保持載入。已被登錄以外對象 hook 的裝置方法會被拒絕（4）；拒絕事件會指出擁有該方法的模組。
- 模組在 OMSI 正常結束時停止；工作階段停止會終止 OMSI，因此工作階段內的模組清理只會透過失敗原則發生。
- 通知在系統匣存在之後（即 OMSI 啟動之後）才顯示；工作階段程式對於在此之前必須說的任何內容，會顯示自己的視窗。
