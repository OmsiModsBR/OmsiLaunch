# 工作階段設定檔

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/session-profiles.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

工作階段設定檔是一種宣告式 YAML 套件，由內容作者隨地圖或附加元件一起發佈，讓終端使用者只需一個命令（`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`）就能啟動可重現的 OmsiLaunch 工作階段。本頁是下列內容的規範性參考：由 `src/OmsiLaunch.Core/SessionProfiles.cs` 中的 `SessionProfileCompiler` 所實作的 `omsilaunch.session-profile/v1` 格式、CLI 套用的優先順序規則（`tools/OmsiLaunch.Cli/Program.cs` 中的 `CliInput.BuildSpecAsync` 與 `RejectProfileConflicts`），以及設定檔可寫入的設定目錄（`ConfigurationCatalog`）。設定檔能做的一切，CLI 旗標與 [LaunchSpec](launchspec.md) 也都能做；設定檔只是將這些選擇打包起來。

穩定性：剖析、驗證、衝突偵測以及 `settings` / `presentation` / `internet-textures` / `behavior` 區塊為 `STABLE_BETA`（離線測試 `session-profiles.strict-compiler`；overlay（暫時覆蓋）與還原路徑已由 RV-005 與 RV-006 於執行階段驗證，請見[執行階段驗證狀態](../status/runtime-validation-status.md)）。`new.date`、`new.time`、`new.year` 與 `new.weather` 鍵在此組建中為 `UNAVAILABLE`（請見 [`new` 區塊](#new)）。

<a id="package-location-and-naming"></a>
## 套件位置與命名

| 項目 | 規則 |
| --- | --- |
| 套件目錄 | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| 設定檔檔案 | `<package>\profile.yaml`（名稱須完全相同，僅一個檔案） |
| 資產 | 套件目錄內的任何檔案或目錄，由 `presentation.splash.assets` 與 `internet-textures.profile` 以相對路徑參考 |
| `id` | 必須是單純的目錄名稱：不可為空或僅含空白字元、不可包含 `\`、`/` 或 `:`，且不可包含序列 `..`。違反時為 `OL_E_SESSION_PROFILE_PATH_ESCAPE`。`profile.yaml` 內宣告的 `id` 值必須與目錄名稱逐位元組相同（區分大小寫）；否則為 `OL_E_SESSION_PROFILE_INVALID`。 |
| 選取 | `/predefined-profile:<id>` 搭配 `/predefined-profile-index:<n>`。索引為必要：只有 `/predefined-profile` 而沒有 `/predefined-profile-index` 時會以 `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` 失敗。 |
| 套件不存在 | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| 發行配置 | 發行套件在 `.omsilaunch\examples\session-profiles\rmg-leste\` 下隨附一個範例（請見[封裝](packaging.md)）。範例不是設定檔：請將套件複製到 `.omsilaunch\session-profiles\<id>\` 才能選取。 |

設定檔由使用者或內容作者安裝與移除。OmsiLaunch 從不寫入套件、從不複製套件，也從不刪除套件。套件目錄不屬於任何交易。

<a id="parsing-rules"></a>
## 剖析規則

| 規則 | 行為 | 錯誤 |
| --- | --- | --- |
| 大小限制 | `profile.yaml` 不可超過 256 KiB（262,144 位元組） | `OL_E_SESSION_PROFILE_INVALID` |
| 文件結構 | 恰好一個 YAML 文件，其根節點為對應（mapping） | `OL_E_SESSION_PROFILE_INVALID` |
| 結構描述 | `schema` 必須完全等於 `omsilaunch.session-profile/v1`（區分大小寫） | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| 錨點與別名 | 文件中任何位置帶有 YAML 錨點（`&name`）的節點都會在驗證前遭拒絕；因此不可能出現別名（`*name`） | `OL_E_SESSION_PROFILE_INVALID`（"YAML anchors are not supported."） |
| 未知鍵 | 每個對應都是封閉的：未列在下表中對應情境的鍵會遭拒絕（"Unknown property in `<context>`: `<key>`"）。鍵的比對區分大小寫（`Schema:` 是未知鍵）。唯一開放的對應是 `settings`，其鍵改為依設定目錄驗證。 | `OL_E_SESSION_PROFILE_INVALID` |
| 純量 | 每個葉節點值都必須是純量；預期純量之處若為序列或對應會遭拒絕（"`<field>` must be a scalar."） | `OL_E_SESSION_PROFILE_INVALID` |
| 數字 | 整數以不變文化特性剖析（`1`、`30`）；`settings` 中的小數以 `.` 作為分隔符號 | `OL_E_SESSION_PROFILE_INVALID` |
| 日期與時間 | `new.date.value` 由 `DateOnly.Parse` 剖析，`new.time.value` 由 `TimeOnly.Parse` 剖析，兩者皆使用不變文化特性；請使用 ISO 格式 `yyyy-MM-dd` 與 `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| YAML 語法錯誤 | 以剖析器訊息回報 | `OL_E_SESSION_PROFILE_INVALID`（"Invalid YAML: ..."） |
| 可執行內容 | YAML 以 `YamlDotNet` 僅剖析為表示樹；不支援標籤、自訂型別或程式碼執行 |

純量（未加引號）中的反斜線是字面字元。Windows 路徑請以單一反斜線書寫（`maps\Grundorf\global.cfg`）。純量中的雙反斜線在值中仍會維持雙反斜線；請見[隨附的範例](#the-packaged-example)。

<a id="key-reference"></a>
## 鍵參考

情境名稱與編譯器所使用的名稱完全相同。此處列出的每個鍵都會被接受；除此之外一律不接受。

<a id="profile-root-mapping"></a>
### `profile`（根對應）

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `schema` | 字串 | 是 | 字面值 `omsilaunch.session-profile/v1`。 |
| `id` | 字串 | 是 | 套件識別碼；必須等於目錄名稱。 |
| `name` | 字串 | 是 | 顯示名稱；回報於 `SessionProfileMetadata.Name`。 |
| `author` | 字串 | 是 | 作者；回報於 `SessionProfileMetadata.Author`。 |
| `version` | 字串 | 是 | 套件版本字串（格式自由，請加上引號：`"1.0"`）；回報於 `SessionProfileMetadata.Version`。 |
| `compatibility` | 對應 | 否 | 請見 `compatibility`。 |
| `new` | 對應 | 否 | NEW_MAP 預設值。請見 `new`。 |
| `presets` | 對應的序列 | 是 | 1 到 99 個預設組合（preset）項目。零個、超過 99 個或非序列的值為 `OL_E_SESSION_PROFILE_INVALID`。 |

### `compatibility`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `maps` | 字串的序列 | 否 | 此設定檔適用的地圖識別碼（`maps\<Map>\global.cfg`）。`/` 會正規化為 `\`；比對不區分大小寫。清單不存在或為空表示「任何地圖」。清單非空時，會對 `WorldMode.NewMap`（對照有效的 `new.map` 或 `/map`）以及 `WorldMode.SavedSituation`（對照所選 `.osn` 參考的地圖，透過內容目錄解析）強制執行。對 `WorldMode.LastMapState` 無法推導出地圖，因此非空清單一律失敗。失敗：`OL_E_SESSION_PROFILE_MAP_MISMATCH`。 |

### `new`

只要此區塊存在就會被讀取與驗證，但只有在所選世界模式為 NEW_MAP（`/new`，CLI 預設值）時才會套用到 spec。在 `/saved:<file.osn>` 下此區塊會被忽略。

| 鍵 | 型別 | 必要 | 是否套用 | 說明 |
| --- | --- | --- | --- | --- |
| `map` | 字串 | 否 | 是 | 正規化形式 `maps\<Map>\global.cfg` 的地圖識別碼（規劃時要求完全符合此形狀：以 `maps\` 開頭、以 `\global.cfg` 結尾、不含 `..`）。設定 `WorldSpec.MapIdentity`。 |
| `entrypoint-index` | 整數 | 否 | 是 | 所呈現的進入點索引（在 OMSI 進入點清單中以 0 起算的位置）。設定 `PresentedEntrypointIndex` 並清除任何進入點識別碼。 |
| `entrypoint` | 字串 | 否 | 是 | 原始進入點識別碼。設定 `EntrypointIdentity` 並清除所呈現的索引。若同時有 `entrypoint-index` 與 `entrypoint`，由於 `entrypoint` 最後套用，因此以它為準。該值是精確（區分大小寫）的標籤或探索識別碼；OMSI 只會呈現每個標籤的第一筆記錄（`world.entrypoint-identity`，`EXPERIMENTAL`）。 |
| `date` | 對應 | 否 | 否（`UNAVAILABLE`） | 請見 `new.date`。 |
| `time` | 對應 | 否 | 否（`UNAVAILABLE`） | 請見 `new.time`。 |
| `year` | 整數 | 否 | 否（`UNAVAILABLE`） | 明確年份。 |
| `weather` | 對應 | 否 | 否（`UNAVAILABLE`） | 請見 `new.weather`。 |

`date`、`time`、`year` 與 `weather` 會被編譯為 `DateSpec`、`TimeSpec`、`YearSpec` 與 `WeatherSpec`，其中 `DateTimeMode.Explicit`／所選的 `WeatherMode`。工作階段規劃器（`src/OmsiLaunch.Core/SessionPlanner.cs`）需要 `world.explicit-date`、`world.explicit-time`、`world.explicit-year` 與 `world.weather`；這四者皆為 `EXPERIMENTAL`（已靜態收尾，執行階段驗證待完成）。起始日期／時間僅適用於 NEW_MAP；天氣適用於每種世界模式。

#### `new.date`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `mode` | 字串 | 是 | 必須為 `explicit`（不區分大小寫）。其他任何值皆為 `OL_E_SESSION_PROFILE_INVALID`（"date must use explicit mode."）。 |
| `value` | 字串 | 是 | `yyyy-MM-dd`。 |

#### `new.time`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `mode` | 字串 | 是 | 必須為 `explicit`。 |
| `value` | 字串 | 是 | `HH:mm` 或 `HH:mm:ss`。 |

#### `new.weather`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `mode` | 字串 | 是 | `preset`、`icao` 或 `real`（不區分大小寫）。其他任何值：`OL_E_SESSION_PROFILE_INVALID`（"Unsupported weather mode"）。 |
| `preset` | 字串 | `mode: preset` 時 | 天氣預設組合名稱。 |
| `icao` | 字串 | `mode: icao` 時 | ICAO 氣象站代碼。 |

<a id="preset-each-entry-of-presets"></a>
### `preset`（`presets` 的每個項目）

| 鍵 | 型別 | 必要 | 預設值 | 說明 |
| --- | --- | --- | --- | --- |
| `index` | 整數 | 是 | | 1 到 99，在設定檔內唯一；索引不必連續。以 `/predefined-profile-index` 選取。重複或超出範圍：`OL_E_SESSION_PROFILE_INVALID`；設定檔中任何地方都不存在的索引：`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`。 |
| `id` | 字串 | 是 | | 預設組合識別碼；回報為 `SessionProfileMetadata.PresetId`。 |
| `name` | 字串 | 是 | | 預設組合顯示名稱；回報為 `SessionProfileMetadata.PresetName`。 |
| `settings` | 對應 | 否 | 無 | 語意 `options.cfg` 設定，請見[設定](#settings)。鍵以不區分大小寫的方式與設定目錄比對。 |
| `presentation` | 對應 | 否 | 繼承 | 啟動畫面（splash）呈現，請見 `presentation`。不存在時，預設組合會繼承基準值（`/spec` 的值或 CLI 預設值 `Managed`）。 |
| `internet-textures` | 對應 | 否 | 繼承 | 請見 `internet-textures`。 |
| `behavior` | 對應 | 否 | 繼承 | 逾時，請見 `behavior`。 |
| `hofs` | 對應的序列 | 否 | 繼承 | 工作階段 HOF 操作，請見 `hofs`。存在時會取代基準的 `HofManagement`。 |
| `modules` | 對應 | 否 | 繼承 | 執行階段模組，請見 `modules`。會與 `/spec` 檔案所要求的合併。 |

只會套用所選的預設組合。但每個預設組合仍會被剖析與驗證，因此預設組合 3 中的錯誤也會使對預設組合 1 的請求失敗。

### `presentation`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `splash` | 對應 | 是 | `presentation` 存在時為必要（"Presentation requires splash."）。請見 `presentation.splash`。 |

#### `presentation.splash`

| 鍵 | 型別 | 必要 | 預設值 | 說明 |
| --- | --- | --- | --- | --- |
| `mode` | 字串 | 是 | | `managed` 會在工作階段期間安裝 OmsiLaunch 的啟動畫面點陣圖（`SplashMode.Managed`）。`unset` 或 `native` 會保留 OMSI 自己的啟動畫面檔案（`SplashMode.Unset`；`Native` 為別名）。不區分大小寫。其他任何值：`OL_E_SESSION_PROFILE_INVALID`。 |
| `language` | 字串 | 否 | `ENG` | 第二個啟動畫面目標的地區語言：`PTB`、`ENG`、`DEU`、`FRA`（別名 `PT-BR`、`EN`、`DE`、`FR`；任何未知值在建立工作階段時會解析為 `ENG`）。搭配 `mode: managed` 時，工作階段會以 overlay 覆蓋 `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<language>.bmp`。 |
| `assets` | 字串 | 否 | 套件隨附的資產 | **相對於套件**的目錄，其中包含 `ENG.bmp`，以及（非英文 `language` 時）`<language>.bmp`；每個檔案都必須是 640x480、24 位元的 BMP。載入設定檔時目錄必須存在（`OL_E_SESSION_PROFILE_ASSET_MISSING`）；檔案在工作階段啟動時驗證（`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`）。適用路徑限制規則。省略時使用安裝中的 `.omsilaunch\assets\splash`（或隨附的預設資產）。 |

設定檔無法設定 `SessionPresentationSpec.SuppressTrayIcon`；除非由 `/spec` 設定，否則它維持為 `false`。

### `internet-textures`

| 鍵 | 型別 | 必要 | 說明 |
| --- | --- | --- | --- |
| `mode` | 字串 | 是 | `native`（`InternetTexturesMode.Native`，OMSI 正常運作）、`disabled`（`Disabled`，工作階段期間抑制組建設定檔所涵蓋的處理程序內下載器）、`override`（`Override`，將工作階段範圍的 `.itx` 設定檔安裝為 `Texture\standard.itx`）。不區分大小寫；其他任何值：`OL_E_SESSION_PROFILE_INVALID`。 |
| `profile` | 字串 | `override` 時為必要 | `.itx` 檔案**相對於套件**的路徑。`override` 時缺少此鍵：`OL_E_SESSION_PROFILE_INVALID`；檔案不存在：`OL_E_SESSION_PROFILE_ASSET_MISSING`。適用路徑限制規則。檔案必須由 `URL` / `target` 行對組成，且 URL 為 `http://` 或 `https://`（否則為 `OL_E_ITX_PROFILE_INVALID`），而每個目標都必須解析到安裝的 `Texture\` 目錄之下，且不可經過重新分析點（`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。列出的目標與 `Texture\standard.ipr` 會成為工作階段刪除項目（請見[交易與復原](../concepts/transactions-and-recovery.md)）。 |

### `behavior`

| 鍵 | 型別 | 必要 | 預設值 | 說明 |
| --- | --- | --- | --- | --- |
| `startup-timeout` | 整數（秒） | 否 | 180 | 從處理程序啟動到 `Running` 的允許時間。載入設定檔時必須為正數；工作階段在啟動時另外要求 1 到 600（否則為 `OL_E_START_SESSION`）。對應至 `LaunchBehaviorSpec.StartupTimeoutSeconds`。 |
| `shutdown-timeout` | 整數（秒） | 否 | 30 | 對應至 `LaunchBehaviorSpec.ShutdownTimeoutSeconds`。ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT：監督程式會直接終止 OMSI，從不讀取此值。 |

`behavior` 區塊存在時，兩個逾時都會被設定（指定值或預設值），並完全取代基準的 `LaunchBehaviorSpec`，包括 `RestoreConfiguration` 與 `SuppressStaleClosecheckWarning`，這兩者會恢復為其預設值（`true`、`true`）。

### `hofs`

每個項目是一項 [HOF 管理](hof-management.md)操作，且恰好指名一個來源鍵（`package`、`collection`、`directory`、`vehicle` 或 `file`）；零個或多個：`OL_E_SESSION_PROFILE_INVALID`。來源與目標是否存在會在規劃工作階段時檢查（`OL_E_HOF_*`），`package` 除外。

| 鍵 | 型別 | 必要 | 預設值 | 說明 |
| --- | --- | --- | --- | --- |
| `package` | 字串 | 其中一個來源鍵 | | **相對於套件**的目錄，存放設定檔隨附的 `.hof` 檔案。必須在載入設定檔時存在（`OL_E_SESSION_PROFILE_ASSET_MISSING`）；適用路徑限制規則。成為 `Directory` 來源。 |
| `collection` | 字串 | 其中一個來源鍵 | | `.omsilaunch\hof\` 之下的集合名稱。 |
| `directory` | 字串 | 其中一個來源鍵 | | 相對於安裝的目錄，例如地圖內容旁。 |
| `vehicle` | 字串 | 其中一個來源鍵 | | 要複製其 HOF 的車輛識別或資料夾。 |
| `file` | 字串 | 其中一個來源鍵 | | 相對於安裝的 `.hof` 檔案。 |
| `targets` | 字串序列 | 是 | | 目標車輛（`Vehicles\` 之下的識別或資料夾）。缺少或為空：`OL_E_SESSION_PROFILE_INVALID`。 |
| `files` | 字串序列 | 否 | 全部 | 要從來源取用的檔名。 |
| `mode` | 字串 | 否 | `add` | `add` 或 `mirror`（不區分大小寫）；其他值：`OL_E_SESSION_PROFILE_INVALID`。 |

含有 `hofs` 的預設組合與 `/hof-source` 旗標並用：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。

### `modules`

每個[執行階段模組](runtime-modules.md) id 一個項目（`EXPERIMENTAL`）。每個項目是帶有下列鍵的對應（內容 `module`）：

| 鍵 | 型別 | 必要 | 預設值 | 說明 |
| --- | --- | --- | --- | --- |
| `enabled` | `true` 或 `false` | 否 | 未設定 | `true` 要求該模組（`LaunchSpec.Modules.Enable`）；`false` 讓它不加入工作階段，包括預設載入、由其地圖載入或由設定檔載入的模組（`LaunchSpec.Modules.Disable`）。 |
| `settings` | 對應 | 否 | 無 | 依鍵指定的模組設定（`LaunchSpec.Modules.Settings`，形式為 `<id>.<key>`）。其值會在規劃工作階段時依模組所宣告的設定檢查（`OL_E_MODULE_SETTING_INVALID`）。 |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

不符合 `[a-z0-9][a-z0-9-]{1,47}` 的模組 id、不是 `true` 或 `false` 的 `enabled`，或不符合 `[a-z][a-z0-9_]{0,31}` 的設定鍵：`OL_E_SESSION_PROFILE_INVALID`。未安裝的模組會在規劃工作階段時回報（`enabled: true` 為 `OL_E_MODULE_NOT_FOUND`，僅有設定時為 `OL_W_MODULE_SETTING_IGNORED`）。

<a id="settings"></a>
## 設定

`settings` 的鍵是 `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）的語意名稱。編譯器只接受存在（`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`）且可寫入（`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`）的鍵。值以字串儲存，並在工作階段建立其 overlay 時轉換為 `options.cfg` 修補；因此無效值會在 `StartSessionAsync` 時才被偵測到，而非在載入設定檔時，並使工作階段以 `OL_E_START_SESSION` 失敗，其訊息帶有 `OL_E_INVALID_SETTING_VALUE: <key>`。下列每個設定都會寫入 `options.cfg`；它們全都限於工作階段範圍，並在工作階段結束後精確還原。

值的形式：

- **bool** 為 `true` 或 `false`（不區分大小寫）。對於存在型 token，會新增或移除該 token；對於反向 token（`no_*`），`true` 會移除否定 token。
- **int** / **decimal** 會依範圍驗證；帶有除數的值會以除過的值儲存（例如 `graphics.minObjectScreenPercent: 5` 會寫入 `0.05`）。
- **string** 依原樣寫入。

| 設定鍵 | `options.cfg` token | 型別 | 範圍／值 | 證據 |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | 任意 | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | 任意 | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool（存在型） | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool（存在型） | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool（存在型） | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool（存在型） | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool（存在型） | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool（存在型） | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | 任意 | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10，儲存時 /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50，儲存時 /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool（存在型） | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool（存在型） | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool，寫成 `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` 或 `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems`（4 行區塊） | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool（存在型） | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool（反向存在型） | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool（兩個存在型 token） | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool（存在型） | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool（存在型） | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool（存在型） | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool（存在型） | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` 第 0 個分量 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 執行階段） |
| `traffic.humans` | `AIMaxCountRandom` 第 1 個分量 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 執行階段） |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

存在於目錄中但**不可寫入**的項目（以 `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` 拒絕）：`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`（已由 `advanced.reducedMultithreading` 取代）、`graphics.texture`、`graphics.textureFilter`。

<a id="path-confinement"></a>
## 路徑限制

`presentation.splash.assets` 與 `internet-textures.profile` 由 `Confined(package root, value)` 解析：

1. 根路徑（`C:\...`）、以 `\` 開頭的路徑，以及任何等於 `..` 的路徑元件都會遭拒絕。
2. 計算完整路徑，且該路徑必須以套件目錄開頭。
3. 從套件根目錄以下直到最終路徑（含）的每個現有元件，都會檢查是否帶有 `ReparsePoint` 屬性。該路徑上任何位置的 junction、目錄符號連結或檔案符號連結都會遭拒絕；無法檢查的元件（`IOException` / `UnauthorizedAccessException`）也會遭拒絕。

這三種失敗都是 `OL_E_SESSION_PROFILE_PATH_ESCAPE`。建立工作階段時，同樣的重新分析點規則也會套用到 `Texture\` 下的 `.itx` 目標。

<a id="precedence-and-override-conflicts"></a>
## 優先順序與覆寫衝突

`CliInput.BuildSpecAsync` 依下列順序組合 spec：

1. **預設值**（NEW_MAP、所有項目皆未設定、逾時 180 s / 30 s）。
2. **`/spec:<file.json>`**：若有指定，會完全取代預設值。
3. **安裝根目錄**：明確的安裝引數優先於 spec 的 `RootPath`；`.` 表示包含可執行檔的目錄。
4. **設定檔**（`/predefined-profile` + `/predefined-profile-index`）：載入套件，並在合併任何內容**之前**，對原始 CLI 引數執行 `RejectProfileConflicts`。接著種子的世界區塊會重設為所選模式的空 `WorldSpec`（使用設定檔時，`/spec` 的世界會被捨棄），再由 `SessionProfileCompiler.Apply` 將設定檔疊加到種子上：`new`（僅 NEW_MAP）、`settings`（合併到種子的 `Environment.General` 之上，逐鍵以設定檔為準），以及 `presentation`、`internet-textures`、`behavior`（各自只在預設組合有定義時取代種子的對應區塊）。
5. **其餘 CLI 引數**疊加在最上層：`/map`、`/entrypoint`、`/entrypoint-index`、`/date`、`/time`、`/year`、天氣旗標、車輛旗標、`/set`、啟動畫面旗標、Internet Textures 旗標、`/startup-timeout`、`/shutdown-timeout`。CLI 的逾時只在有指定時套用；否則維持 spec／設定檔／預設值。
6. 對非 NEW_MAP 模式進行**相容性檢查**（`ValidateCompatibility`）。

以所選設定檔所擁有之欄位為目標的 CLI 引數即為衝突，會以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 拒絕（結束代碼 2，類別 `invalid_argument`）。檢查是以欄位為單位，而非以值為單位：即使重複指定設定檔自己的值，仍然算是衝突。

| CLI 引數 | 設定檔定義下列項目時衝突 | 僅限模式 |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` 或 `/entrypoint-index` | `new.entrypoint` 或 `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`, `/weather-icao`, `/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | 預設組合 `settings` 中相同的 `<key>`（不區分大小寫） | 任意 |
| `/splash`, `/splash-language`, `/splash-assets` | `presentation`（任何內容） | 任意 |
| `/internet-textures`, `/internet-textures-profile` | `internet-textures`（任何內容） | 任意 |
| `/startup-timeout`, `/shutdown-timeout` | `behavior`（任何內容） | 任意 |
| `/module:<id>` | `modules.<id>.enabled: false` | 任意 |
| `/no-module:<id>` | `modules.<id>.enabled: true` | 任意 |
| `/module-setting:<id>.<key>=...` 或模組旗標 | `modules.<id>.settings.<key>` | 任意 |

不屬於衝突的情況：預設組合未定義的 `/set` 鍵（會被加入）、車輛旗標（`/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration`、`/no-vehicle`；設定檔無法定義玩家車輛），以及 `/saved` 下的任何世界引數（`new` 區塊在該情況下不會套用）。無論是否使用設定檔，`/map`、`/entrypoint` 與 `/entrypoint-index` 搭配 `/saved` 一律無效（`OL_E_INVALID_ARGUMENT`）。

<a id="error-codes"></a>
## 錯誤碼

| 代碼 | 發生時機 | CLI 結束代碼 |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 不存在 | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` 不是單純的目錄名稱；`assets` / `profile` 離開套件或經過重新分析點 | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` 不是 `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | 大小限制、文件結構、錨點、未知鍵、缺少必要鍵、非純量值、錯誤的數字／日期／時間、`id` 不符、預設組合數量／索引規則、不支援的模式字詞、非正數逾時、`presentation` 缺少 `splash`、`override` 缺少 `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | 缺少 `/predefined-profile-index`、超出 1..99，或沒有具該 `index` 的預設組合 | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | `settings` 鍵不在設定目錄中 | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | `settings` 鍵已列入目錄但為唯讀 | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | 套件內不存在 `assets` 目錄或 `profile` 檔案 | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` 非空，且有效地圖未列於其中（或無法推導） | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 明確的 CLI 引數以設定檔所擁有的欄位為目標 | 2 |

這些錯誤全都在編譯命令列時、規劃之前發生。它們是 `SessionProfileException`（衝突則為 `ArgumentException`），且絕不會啟動工作階段。完整目錄請見[錯誤](errors.md)；結束代碼請見[結束代碼](exit-codes.md)。

<a id="discovering-installed-profiles"></a>
## 探索已安裝的設定檔

`OmsiLaunch.exe /list:SessionProfiles`（API `ListSessionProfilesAsync`）會編譯 `.omsilaunch\session-profiles\` 之下的每個套件，並為每個套件印出一個項目：`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity`，以及依索引順序排列的 `Presets`（`Index`、`Id`、`Name`）。無法編譯的套件仍會列出，並帶有 `IsValid: false` 與啟動時會回報的相同 `OL_E_SESSION_PROFILE_*` 代碼。不會寫入任何內容。

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## 設定檔在 API 中的呈現方式

成功載入後，spec 會在 `LaunchSpec.SessionProfile` 中攜帶一筆 `SessionProfileMetadata` 記錄：

| 欄位 | 來源 |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | 所選預設組合的 `id` |
| `PresetIndex` | 所選預設組合的 `index` |
| `PresetName` | 所選預設組合的 `name` |
| `PackagePath` | 套件目錄的絕對路徑 |

規劃器會在由此類 spec 建立的每個 `SessionPlan` 中加入資訊性診斷訊息 `session_profile.selected`，其資料鍵為 `session_profile.id`、`session_profile.name`、`session_profile.version`、`session_profile.author`、`session_profile.preset_id`、`session_profile.preset_index`、`session_profile.preset_name` 與 `session_profile.path`。它不影響是否可執行。直接使用[公開 API](public-api.md) 的整合者可以呼叫 `OmsiLaunch.Core` 中的 `SessionProfileCompiler.Load` 與 `SessionProfileCompiler.Apply`；YAML 表示法絕不會進入 `OmsiLaunch.Api`。

<a id="examples"></a>
## 範例

<a id="example-1-settings-only-profile-one-preset"></a>
### 範例 1：僅含設定、單一預設組合的設定檔

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

執行：`OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`。由於設定檔未定義 `new` 區塊，地圖與進入點來自命令列；加上 `/set:graphics.maxFPS=60` 是允許的，加上 `/set:traffic.humans=10` 則為衝突。

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### 範例 2：綁定地圖、含三個預設組合與隨附資產的設定檔

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`，套件內含 `assets\splash\ENG.bmp`、`assets\splash\DEU.bmp` 與 `textures\offline.itx`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

執行：`OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`。若使用 `/saved:situations\mytrip.osn`，則會略過 `new` 區塊，且該 `.osn` 必須參考 `maps\Grundorf\global.cfg`。

<a id="the-packaged-example"></a>
### 隨附的範例

發行版本隨附 `docs/examples/session-profiles/rmg-leste/profile.yaml`（[檢視](../../../examples/session-profiles/rmg-leste/profile.yaml)）。它語法有效、符合結構描述，且載入時不會發生錯誤。但有兩個特性使它在此組建中無法不經修改就啟動工作階段：

1. 它設定了 `new.date`、`new.time` 與 `new.weather`，這會使計畫不可執行（請見 [`new` 區塊](#new)）。
2. 它的路徑值是含雙反斜線的純量（`maps\\RMG Leste\\global.cfg`）。YAML 會保留雙反斜線，而地圖識別碼是以文字比對（僅做 `/` 到 `\` 的正規化），因此 `new.map` 與 `compatibility.maps` 不會符合目錄識別碼 `maps\RMG Leste\global.cfg`（規劃時為 `OL_E_MAP_NOT_FOUND`）。`assets` 值仍可解析，因為 Windows 路徑正規化會合併重複的分隔符號。

此組建中可執行的形式如下：

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## 相關頁面

- [CLI 參考](cli.md)：`/predefined-profile`、`/predefined-profile-index`、`/set` 與世界旗標
- [LaunchSpec](launchspec.md)：設定檔所編譯成的記錄
- [交易與復原](../concepts/transactions-and-recovery.md)：`settings`、啟動畫面與 `.itx` overlay 如何套用與還原
- [功能](capabilities.md)與[執行階段驗證狀態](../status/runtime-validation-status.md)
