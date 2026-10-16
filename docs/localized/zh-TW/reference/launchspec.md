# LaunchSpec 參考

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/launchspec.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 `LaunchSpec` 的規範性參考。`LaunchSpec` 是描述單一 OmsiLaunch 工作階段的請求記錄，本頁涵蓋：其 C# 結構（`OmsiLaunch.Api`）、由 CLI 載入的 JSON 檔案形式（`/spec:<path>`、`tools/OmsiLaunch.Cli/LaunchSpecJson.cs`）、每個屬性的型別、預設值、驗證規則與目前效果、使計畫不可執行的驗證規則，以及 CLI 旗標、spec 檔案與工作階段設定檔之間的優先順序。本頁只記載目前程式碼實際的行為。

相關頁面：[公開 API](public-api.md)、[CLI 參考](cli.md)、[工作階段設定檔](session-profiles.md)、[錯誤碼](errors.md)、[工作階段生命週期](../concepts/session-lifecycle.md)、[功能](capabilities.md)。

<a id="where-a-launchspec-comes-from"></a>
## LaunchSpec 的來源

| 來源 | 如何成為 `LaunchSpec` |
| --- | --- |
| API | 整合者建構此記錄並將其傳給 `PlanSessionAsync`。 |
| CLI 旗標 | `CliInput.BuildSpecAsync` 從內建預設值（NEW_MAP、所有項目皆未設定、`Behavior` 預設值）開始，再套用旗標。 |
| `/spec:<path>` JSON 檔案 | 由 `LaunchSpecJson.LoadAsync` 載入，接著作為種子，由 CLI 旗標覆寫（請見[優先順序](#precedence-cli-flags-vs-spec-file-vs-session-profile)）。 |
| 工作階段設定檔（`/predefined-profile:<id> /predefined-profile-index:<n>`） | `SessionProfileCompiler.Apply` 將設定檔的世界、設定、呈現、Internet Textures（網路材質）與行為寫入種子，並記錄 `SessionProfile` 中繼資料。 |

下方的[完整範例](#complete-example)已對照記錄結構驗證。最小範例的副本以 `examples/release-session.example.json` 隨附（在發行套件中為 `.omsilaunch\examples\release-session.example.json`）。

<a id="json-form"></a>
## JSON 形式

| 規則 | 詳細說明 |
| --- | --- |
| 序列化程式 | `System.Text.Json`，搭配 `PropertyNameCaseInsensitive = true`、`ReadCommentHandling = Skip`、`AllowTrailingCommas = true`；未註冊任何轉換器。 |
| 屬性名稱 | 即 C# 屬性名稱（`Installation`、`RootPath`……）。載入時比對不區分大小寫；CLI 以 PascalCase 寫出。 |
| 列舉 | 整數（沒有字串列舉轉換器）。`"Mode": 0` 有效；`"Mode": "NewMap"` 會被視為格式錯誤的 JSON 而遭拒絕。各值列於[列舉](#enumerations)。 |
| `OptionalValue<T>` | 一個物件 `{ "Presence": 0 | 1, "Value": <T or null> }`。`Presence` 0 = `Unset`（值會被忽略），1 = `Set`（值必須存在且不可為 null；值為 null 的 `Set` 不會被驗證，其行為等同無效值）。省略的 `OptionalValue` 成員即為 `Unset`。唯讀成員 `IsSet` 會出現在 CLI 寫出的輸出中，載入時接受但忽略。 |
| 選用記錄 | `Year`、`Weather`、`Input`、`Diagnostics`、`Presentation`、`InternetTextures`、`SessionProfile`、`HofManagement`、`Modules` 可為 `null` 或省略；`Effective*` 存取子會代入預設值。 |
| 必要記錄 | `Installation`、`World`、`Date`、`Time`、`Environment`（須含全部八個字典，空的請用 `{}`）、`Behavior` 必須是存在的物件。它們不會被驗證：若為 `null` 或缺少，稍後會以 null 參考失敗，CLI 會回報為 `OL_E_INTERNAL`（結束代碼 10）或 `OL_E_INVALID_ARGUMENT`（結束代碼 2）。 |
| 未知屬性 | 在繫結前即遭拒絕：`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`（路徑使用檔案中所寫的成員名稱）。字典內容（`Environment.*`）不會被當作屬性檢查。 |
| 根節點 | 必須是 JSON 物件：`OL_E_SPEC_INVALID`。最大巢狀深度為 32。 |
| 檔案大小 | 最多 1 MiB（1 048 576 位元組）：`OL_E_SPEC_TOO_LARGE`。檔案不存在：`OL_E_SPEC_NOT_FOUND`。 |
| 註解與結尾逗號 | 接受 `//` 與 `/* */` 註解，以及結尾逗號。 |
| 格式錯誤的 JSON | 剖析器例外不會被轉譯：CLI 回報 `OL_E_INTERNAL`，結束代碼為 10。 |
| 編碼 | UTF-8（讀取器可容許 BOM）。識別碼中的反斜線必須跳脫（`"maps\\Grundorf\\global.cfg"`）；地圖、情境、車輛與 HOF 識別碼可接受正斜線。 |

<a id="complete-example"></a>
## 完整範例

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // optional pin of Omsi.exe
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // must be true
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30,             // with CooperativeShutdown
    "CooperativeShutdown": false              // opt-in
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

明確日期（在組建支援時）寫成 `"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }`，時間則寫成 `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }`。兩者只適用於 NEW_MAP 工作階段（`EXPERIMENTAL`；見下文）。

<a id="property-reference"></a>
## 屬性參考

「使用情形」欄說明目前程式碼如何處理該值。穩定性使用[公開 API 頁面](public-api.md#stability-vocabulary)的用語。

<a id="launchspec-root"></a>
### `LaunchSpec`（根）

| 屬性 | JSON 型別 | 必要 | 省略時的預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` 物件 | 是 | 無 | 是 | `STABLE_BETA` |
| `World` | `WorldSpec` 物件 | 是 | 無 | 是 | `STABLE_BETA` |
| `Date` | `DateSpec` 物件 | 是 | 無 | 是，僅限 NEW_MAP（請見 [`DateSpec`](#datespec-timespec-yearspec)） | `EXPERIMENTAL` |
| `Time` | `TimeSpec` 物件 | 是 | 無 | 是，僅限 NEW_MAP | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | 否 | `Unset` | 為診斷訊息而解析；設定任何欄位皆不可執行 | `PARTIAL` |
| `Environment` | `EnvironmentSpec` 物件 | 是 | 無 | 是（語意 `options.cfg` overlay（暫時覆蓋）） | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` 物件 | 是 | 無 | 部分（見該記錄） | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` 物件或 null | 否 | `null` → `EffectiveYear` = 模式 `Unset` | 是，僅限 NEW_MAP | `EXPERIMENTAL` |
| `Weather` | `WeatherSpec` 物件或 null | 否 | `null` → `EffectiveWeather` = 模式 `Unset` | 是，所有世界模式，於世界啟動後立即套用（請見 [`WeatherSpec`](#weatherspec)） | `EXPERIMENTAL` |
| `Input` | `InputSpec` 物件或 null | 否 | `null` → `EffectiveInput` = 兩者皆未設定 | 是（請見 [`InputSpec`](#inputspec)） | `EXPERIMENTAL` |
| `Diagnostics` | `DiagnosticsSpec` 物件或 null | 否 | `null` → `EffectiveDiagnostics` = 預設值 | 僅攜帶 | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` 物件或 null | 否 | `null` → `EffectivePresentation` = 受控啟動畫面、無語言、無自訂目錄、顯示系統匣 | 是 | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` 物件或 null | 否 | `null` → `EffectiveInternetTextures` = `Native` | 是 | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` 物件或 null | 否 | `null` | 僅供來源追溯（`session_profile.selected` 計畫診斷訊息） | `STABLE_BETA` |
| `HofManagement` | `HofManagementSpec` 物件或 null | 否 | `null` → `EffectiveHofManagement` = 沒有任何操作 | 是（工作階段 HOF overlay，請見 [HOF 管理](hof-management.md)） | `STABLE_BETA` |
| `Modules` | `RuntimeModulesSpec` 物件（`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`）或 null | 否 | `null` → `EffectiveModules` = 沒有任何被要求、停用或設定的項目 | 否（模組絕不會透過 OmsiLaunch 觸及安裝檔案；請見[執行階段模組](runtime-modules.md)） | `EXPERIMENTAL` |

唯讀存取子（出現在 CLI JSON 輸出中，載入時忽略）：`EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures`、`EffectiveHofManagement`、`EffectiveModules`。

### `InstallationSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | 字串 | 必要 | 包含 `Omsi.exe` 與 `plugins\` 的目錄。請見[路徑規則](#path-rules)。空白或僅含空白字元 → `OL_E_INSTALLATION_NOT_FOUND`。 | 是 | `STABLE_BETA` |
| `ExpectedExecutableSha256` | 字串或 null | `null` | `null`，或 64 個十六進位數字（否則為 `OL_E_INVALID_ARGUMENT`）。 | 選用的固定值：設定時，除非 `Omsi.exe` 的 SHA-256 與其相等，否則計畫不可執行（`OL_E_UNSUPPORTED_BUILD`，資料 `expected_sha256` 與 `file_sha256`）。 | `STABLE_BETA` |

### `WorldSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` 整數 | 必要 | `0` `NewMap`、`1` `SavedSituation`、`2` `LastMapState`（`LastSituation` 是值同為 2 的已淘汰別名）。 | 是；`LastMapState` 需要 `MapIdentity` 與該地圖的 `laststn.osn`（`OL_E_MAP_NOT_FOUND`、`OL_E_SITUATION_NOT_FOUND`） | `NewMap`、`SavedSituation`：`STABLE_BETA`；`LastMapState`：`EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | 用於 `NewMap`：必要，形式為 `maps\<dir>\global.cfg`（不區分大小寫、接受 `/`、不可含 `..`）且必須已安裝。用於 `SavedSituation` 時會被忽略（地圖由 `.osn` 提供）。 | 是（交接） | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | 用於 `SavedSituation`：必要，為已安裝的 `situations\...\<file>.osn` 識別碼（如 `DiscoverAsync(Situations)` / `/list:situations` 所回傳）。 | 是（交接） | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | 用於未設定 `EntrypointIdentity` 的 `NewMap`：必要，`>= 0`，為該地圖在 OMSI 所呈現之進入點清單中的索引。未設定時以 `-1` 傳給外掛程式。 | 是（交接） | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | 精確（區分大小寫）的進入點標籤，或探索識別碼。規劃器會將其解析為 OMSI 所呈現的標籤，啟動時選取該標籤（`world.entrypoint-identity`）；OMSI 不呈現的記錄會被拒絕（`OL_E_ENTRYPOINT_NOT_PRESENTED`），未知者亦同（`OL_E_ENTRYPOINT_NOT_FOUND`）。 | 會攜帶 | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec`（唯讀） | 計算得出 | 設定 `EntrypointIdentity` 時 `Mode` = `Identity`；否則設定索引時為 `PresentedIndex`；否則為 `Unset`。`PresentedIndex`、`Identity` 反映輸入值。 | 衍生 | `STABLE_BETA` |

<a id="datespec-timespec-yearspec"></a>
### `DateSpec`、`TimeSpec`、`YearSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` 整數 | 必要（`Year`：記錄為 null 時為 `0`） | `0` `Unset`、`1` `Explicit`、`2` `System`。 | NEW_MAP：`Explicit` 使用 `Value`，`System` 在規劃工作階段時使用電腦時鐘；`Unset` 保留 OMSI 自己的值（其選項與啟動表單）。控制器會將要求解析為 OMSI 的起始時鐘（年、一年中的第幾天、時、分、秒；功能 `world.explicit-date`、`world.explicit-time`、`world.explicit-year`），啟動交接資料（版本 7）攜帶它，外掛程式會在 OMSI 的 `NewSituation` 之前寫入，因此 OMSI 在地圖載入前即依要求的日期推導季節與日曆。啟動後會讀回執行中的時鐘；不一致，或 OMSI 使用固定的起始時間，都會使啟動以 `OL_E_DATE_TIME_APPLY_FAILED` 失敗。`SavedSituation`：`Unset` 以外的模式為 `OL_E_CAPABILITY_UNAVAILABLE`（情境本身帶有時間）。 | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`：真實的日曆日期（`Day` 對該月與該年有效）；`SemanticTime`：`Hour` 0..23、`Minute` 0..59、`Second` 0..59；年份 1900..2200。`YearSpec` 設定年份（兩者皆給定時，也設定 `Date` 的年份）。閏年的 12 月 31 日會被拒絕：OMSI 的日曆無法表示第 366 天。無效值：`OL_E_DATE_TIME_APPLY_FAILED`。`Mode` 為 `Explicit` 時必須設定，否則必須未設定（`OL_E_INVALID_ARGUMENT`）。 | 是 | `EXPERIMENTAL` |

### `WeatherSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` 整數 | 記錄為 null 時為 `0` | `0` `Unset`、`1` `Preset`、`2` `Icao`、`3` `RealCurrent`。 | `Unset` 以外的任何模式都需要 `world.weather`；外掛程式在世界啟動後立即套用（所有世界模式；它會覆寫已儲存情境的天氣）。失敗會使啟動以 `OL_E_WEATHER_APPLY_FAILED` 失敗。 | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | 安裝的 `weather` 資料夾中的檔案，以名稱或 `weather\<name>.owt` 指定；於規劃時檢查（`OL_E_WEATHER_INVALID`）。 | 交接 | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | 四個字母或數字（否則為 `OL_E_WEATHER_INVALID`）；由 `Icao` 模式使用。 | 交接 | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec`（位於 `PlayerVehicle` 內）

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | 已安裝的 `Vehicles\...\<file>.bus` 識別碼，否則為 `OL_E_VEHICLE_NOT_FOUND`。 | 解析至 `ResolvedContent`；接著 `player-vehicle.model` 不支援 → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | `Model` 的塗裝（repaint）識別碼（`<cti>#item:<n>`），否則為 `OL_E_REPAINT_NOT_FOUND`；僅在設定 `Model` 時檢查。 | 同上 | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | 已安裝的 `Vehicles\...\<file>.hof`，否則為 `OL_E_HOF_NOT_FOUND`。 | 同上 | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | 任何字串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | 任何字串。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool（唯讀） | 計算得出 | 設定 `Model` 時為 `true`。交接資料以 `PlayerVehicleEnabled` 攜帶的是 `PlayerVehicle.IsSet`。 | 衍生 | `PARTIAL` |

`Presence` 為 1 且所有欄位皆未設定的 `PlayerVehicle` 會被接受，且沒有任何效果。在此組建中，設定任何欄位都會使計畫不可執行（`UNAVAILABLE`）。

### `EnvironmentSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `General`, `Advanced`, `Graphics`, `AdvancedGraphics`, `Sound`, `AiPassengers`, `Keyboard`, `Controllers` | 各為 `IReadOnlyDictionary<string, OptionalValue<string>>`，必要（空的時候為 `{}`） | 無 | 是 | `STABLE_BETA` |

這八個群組會被串接在一起；鍵放在哪個群組中並無影響。每個 `Presence` 為 1 的項目都是來自 `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）的語意設定；鍵決定目標檔案（目前所有鍵皆為 `options.cfg`）與 token。規劃時會檢查鍵是否存在（`OL_E_UNKNOWN_SETTING`）以及是否可寫入（`OL_E_SETTING_NOT_WRITABLE`）；值只在啟動時驗證（`OL_E_INVALID_SETTING_VALUE`，回報為帶有 `OL_E_START_SESSION` 的 `Failed` 工作階段）。鍵不區分大小寫。未設定的項目會被忽略。CLI 的 `/set:<key>=<value>` 旗標會寫入 `General`；工作階段設定檔的 `settings` 也會合併到 `General`。

| 鍵 | 值 | 備註 |
| --- | --- | --- |
| `general.language` | 字串 | `[language]` token |
| `general.radio` | 字串 | |
| `general.alternateView`, `general.showOwnDriver`, `general.showErrorMessages`, `general.autoSave`, `general.currentTime`, `general.currentDate`, `general.currentYear` | `true` / `false` | 存在型 token（`autoSave` 是 `noAutoSave` 的反向） |
| `graphics.screenRatio` | 字串 | |
| `graphics.maxFPS` | 整數 10..200 | |
| `graphics.tileDistance` | 整數 1..20 | |
| `graphics.maxObjectDistanceMeters` | 數值 20..5000 | |
| `graphics.minObjectScreenPercent` | 數值 0..10 | 除以 100 後儲存 |
| `graphics.minReflectionObjectScreenPercent` | 數值 0..50 | 除以 100 後儲存 |
| `graphics.maxObjectComplexity` | 整數 0..3 | |
| `graphics.maxMapComplexity` | 整數 0..2 | |
| `graphics.sunGlow`, `graphics.loadAllTiles`, `graphics.stencilBuffer`, `graphics.rainReflections`, `graphics.humansInRainReflections` | `true` / `false` | 存在型 token |
| `graphics.stencilShadows` | `true` / `false` | 寫成 `on` / `off` |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections` (bool,int>=0,bool,bool) | 一個 `smokesystems` 區塊 |
| `simulation.collision`, `simulation.collisionTerrain`, `simulation.collisionVehicles`, `simulation.collisionPedestrians`, `simulation.disableAutomaticScheduleAnalysisPopup`, `simulation.ticketInfo`, `simulation.automaticClutch` | `true` / `false` | 存在型 token |
| `simulation.ticketSelling` | 整數 0..2 | |
| `simulation.maintenance` | 整數 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | 同時寫入兩個 OMSI token（`RUNTIME_PROVEN`） |
| `view.driverSmooth`, `view.driverMoving`, `controls.autoCenter`, `controls.reducedSteeringSpeed` | `true` / `false` | 存在型 token |
| `traffic.randomVehicles` | 整數 0..1000 | 多行 `AIMaxCountRandom` 區塊的第 0 個分量（已於執行階段驗證，矩陣 RV-005） |
| `traffic.humans` | 整數 0..1000 | `AIMaxCountRandom` 的第 1 個分量 |
| `traffic.factorPercent` | 數值 1..300 | |
| `traffic.parkedVehiclesPercent` | 數值 0..100 | |
| `traffic.scheduledVehicles` | 數值 0..1000 | |
| `traffic.scheduledLinePriority` | 數值 1..4 | |
| `traffic.passengerFactorPercent` | 數值 0..200 | |
| `sound.stereo` | 數值 0..100 | |
| `sound.maxSimultaneousSounds` | 數值 5..1000 | |
| `sound.masterVolume` | 數值 0..1 | |
| `advanced.multithreadingCalculate`, `advanced.multithreadingTextureLoad`, `graphics.texture`, `graphics.textureFilter` | 拒絕 | 已知但不可寫入 → `OL_E_SETTING_NOT_WRITABLE` |

修補後的檔案會保留其編碼（保留 Windows-1252 位元組；遵循帶 BOM 的 UTF-8/UTF-16）與行尾字元。

### `LaunchBehaviorSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | 僅 `true` | 精確還原是強制的：`false` 會以 `OL_E_INVALID_ARGUMENT` 拒絕。 | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | 任意 | `true`：工作階段開始前已存在的 `closecheck` 檔案會在啟動時被永久移除（診斷訊息 `closecheck.stale-removed` 附其 SHA-256；失敗時為 `OL_E_CLOSECHECK_REMOVE_FAILED`）。`false`：現有的 `closecheck` 保持不動，且不屬於工作階段刪除項目。OMSI 在工作階段期間寫入的 `closecheck` 一律在還原時移除。 | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600（否則 `StartSessionAsync` 會擲回 `ArgumentOutOfRangeException`；CLI `/startup-timeout` 強制 1..600；設定檔要求 > 0）。從監督程式啟動到 `Running` 的時間預算；逾時則工作階段以 `OL_E_STARTUP_TIMEOUT`（外掛程式已啟動）或 `OL_E_PLUGIN_NOT_LOADED` 失敗。 | 是 | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600（CLI `/shutdown-timeout`） | 搭配 `CooperativeShutdown`：OMSI 在被終止前可自行結束的時間。未搭配時 OMSI 會立即被終止。 | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | 任意（CLI `/cooperative-shutdown`） | `true`：結束執行中的工作階段時，會要求 OMSI 透過其自己的結束常式（`TForm_main.CloseOMSI`：儲存選項與地圖的 `laststn.osn`，並結束外掛程式）結束，而不是終止它；經過 `ShutdownTimeoutSeconds` 後會被終止（`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`）。還原在結束之後執行，因此工作階段擁有的檔案仍會回到其原始位元組。 | `EXPERIMENTAL` |

### `InputSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | OMSI 格式的鍵盤檔案路徑（絕對路徑，或相對於安裝）。工作階段會用它取代 `Inputs\keyboard.cfg`（交易 overlay：安裝中的檔案在結束時逐位元組還原）。該檔案必須存在、至多 1 MiB，且至少包含一個 `[entry]` 綁定；否則為 `OL_E_INVALID_ARGUMENT`。 | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | OMSI 格式的控制器檔案路徑，以相同方式在工作階段中取代 `Inputs\gamectrler.cfg`。它必須至少包含一個 `[ctrl]` 裝置；否則為 `OL_E_INVALID_ARGUMENT`。 | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| 屬性 | 型別 | 預設值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | `src/` 中沒有取用端。主機追蹤記錄 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` 一律會寫入。 | `PARTIAL`（會攜帶，目前無效果） |
| `Verbose` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `ProcessTrace` | bool | `false` | 沒有取用端。 | `PARTIAL` |
| `PluginTrace` | bool | `false` | 啟動交接旗標：外掛程式會為每個執行階段操作寫入 `plugin.trace.operation` 事件（操作、結果、代碼、以微秒計的耗時）；它們會經由遙測環狀緩衝區抵達 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`。 | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | 啟動交接旗標：原生層會將每次 hook 的安裝與移除（`native.trace`：邊界、擁有者、目標、結果）回報到同一份主機追蹤中。 | `EXPERIMENTAL` |

CLI 旗標 `/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native` 會填入這些布林值（`/logall` 會設定 `Verbose`、`ProcessTrace`、`PluginTrace`、`NativeTrace`）；它們會與 spec 中的值做 OR 運算。

### `SessionPresentationSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` 整數 | `1`（`Managed`） | `0` `Unset`（別名 `Native`）：不改動 OMSI 自己的啟動畫面檔案。`1` `Managed`：OmsiLaunch 在工作階段期間以 overlay 覆蓋 `GUI\NewSplashscreen_ENG.bmp` 與 `GUI\NewSplashscreen_<LANG>.bmp`（事後精確還原）。 | 是 | `STABLE_BETA`（矩陣 RV-006） |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`、`ENG`/`EN`、`DEU`/`DE`、`FRA`/`FR`（不區分大小寫）；其他任何值都會正規化為 `ENG`。未設定時，會讀取 `options.cfg` 的 `[language]` 值並以相同方式正規化。 | 是（僅受控啟動畫面） | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | 包含 `ENG.bmp` 與 `<LANG>.bmp`（640×480、24 位元 BMP）的目錄。請見[路徑規則](#path-rules)。目錄不存在：`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`；檔案不存在：`OL_E_SPLASH_ASSET_MISSING`；格式錯誤：`OL_E_SPLASH_FORMAT_UNSUPPORTED`。未設定時使用 `<root>\.omsilaunch\assets\splash`（由套件初始化一次），否則使用套件隨附的 `assets\splash`。 | 是（僅受控啟動畫面） | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` 會隱藏 CLI 擁有者獨立的 Windows 系統匣指示器。 | 僅 CLI 擁有者；API 沒有系統匣。沒有對應的 CLI 旗標；只能來自 spec 檔案。 | `STABLE_BETA` |

### `InternetTexturesSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` 整數 | `0`（`Native`） | `0` `Native`：不做任何變更。`1` `Disabled`：外掛程式抑制 OMSI 處理程序內的下載器（遙測 `internet-textures.suppressed` / `internet-textures.suppression.failed`）。`2` `Override`：`.itx` 設定檔以 overlay 覆蓋為 `Texture\standard.itx`；其目標檔案與 `Texture\standard.ipr` 成為工作階段刪除項目。 | 是 | `Native`：`STABLE_BETA`；`Disabled`、`Override`：`EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | `Override` 時為必要（`OL_E_ITX_PROFILE_REQUIRED`）。一個由成對行組成的文字檔：先是絕對 `http`/`https` URL，接著是相對於安裝根目錄的目標路徑；該路徑必須包含 `Texture\` 元件、不是根路徑、不含 `..`、不以 `\` 開頭，且不經過任何 junction/symlink（`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。請見[路徑規則](#path-rules)。 | 是 | `EXPERIMENTAL` |

### `HofManagementSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Operations` | `HofOperationSpec` 陣列 | 無 | 依序套用；解析、衝突規則與限制請見 [HOF 管理](hof-management.md)。 | 是 | `STABLE_BETA` |

`HofOperationSpec` 成員：`SourceKind`（`HofSourceKind` 整數）、`Source`（字串）、`TargetVehicles`（`Vehicles\` 之下的車輛識別碼或資料夾陣列，至少一個）、`Mode`（`HofApplyMode` 整數，預設 `0` `Add`）、`Files`（`.hof` 檔名陣列，null 表示全部）。問題以計畫診斷訊息 `OL_E_HOF_*` 回報。

### `RuntimeModulesSpec`

| 屬性 | 型別 | 預設值 | 有效值 | 使用情形 | 穩定性 |
| --- | --- | --- | --- | --- | --- |
| `Enable` | 模組 id 陣列 | 無 | 安裝在 `plugins\OmsiLaunch.Modules` 之下的模組 id；請見[執行階段模組](runtime-modules.md)。無法載入的被要求模組會是計畫診斷訊息 `OL_E_MODULE_*`（計畫不可執行）。 | 是 | `EXPERIMENTAL` |
| `Disable` | 模組 id 陣列或 null | 無 | 讓預設、內容或設定檔所載入的模組不加入工作階段（計畫診斷訊息 `module.disabled`）。同時啟用與停用的模組為 `OL_E_MODULE_SETTING_INVALID`。唯讀存取子 `EffectiveDisable`。 | 是 | `EXPERIMENTAL` |
| `Settings` | 字串物件或 null | 無 | `"<module id>.<key>": "<value>"`。優先於設定檔 `.omsilaunch\modules.ini` 與資訊清單預設值。無效值，或模組未宣告的索引鍵，為 `OL_E_MODULE_SETTING_INVALID`（計畫不可執行）；針對工作階段未載入之模組的設定為 `OL_W_MODULE_SETTING_IGNORED`。有效值位於 `PlannedModule.Settings`。唯讀存取子 `EffectiveSettings`。 | 是 | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| 屬性 | 型別 | 使用情形 | 穩定性 |
| --- | --- | --- | --- |
| `Id`, `Name`, `Version`, `Author`, `PresetId`, `PresetIndex`, `PresetName`, `PackagePath` | 字串／整數 | 記錄在計畫診斷訊息 `session_profile.selected`（`Data["session_profile.*"]`）中。除此之外不會被使用；通常由工作階段設定檔編譯器填入，而非手動填寫。 | `STABLE_BETA` |

<a id="enumerations"></a>
## 列舉

| 列舉 | 值（JSON 整數） |
| --- | --- |
| `WorldMode` | `NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（已淘汰的別名；它是 OMSI 原生的最後地圖狀態分支，絕不是最新的 `.osn`） |
| `DateTimeMode` | `Unset` = 0、`Explicit` = 1、`System` = 2 |
| `WeatherMode` | `Unset` = 0、`Preset` = 1、`Icao` = 2、`RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0、`Native` = 0（別名）、`Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0、`Disabled` = 1、`Override` = 2 |
| `HofSourceKind` | `Collection` = 0、`Directory` = 1、`Vehicle` = 2、`File` = 3 |
| `HofApplyMode` | `Add` = 0、`Mirror` = 1 |
| `Presence` | `Unset` = 0、`Set` = 1 |
| `EntrypointMode`（唯讀 `Entrypoint.Mode`） | `Unset` = 0、`PresentedIndex` = 1、`Identity` = 2 |

超出宣告範圍的整數會由序列化程式原樣儲存，其行為等同未知值（例如未知的 `WorldMode` 既不是 NEW_MAP 也不是 SAVED_SITUATION，會產生沒有世界功能的計畫；外掛程式會拒絕它，但 CLI 無論如何都會取代該模式，請見優先順序）。

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## 驗證規則與不可執行的診斷訊息

`PlanSessionAsync` 先執行 `LaunchValidation.Validate`，再執行 `SessionPlanner.PlanAsync`。若且唯若沒有任何診斷代碼以 `OL_E_` 開頭，計畫才可執行。完整清單如下：

| 診斷訊息 | 條件 | 來源 |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` 為空或僅含空白字元 | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` 但未設定值，或月／日超出範圍；`Time.Mode` = `Explicit` 但未設定值，或時／分／秒超出範圍 | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | 模式不是 `Explicit` 時卻設定了 `Date.Value` 或 `Time.Value` | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` 且 `MapIdentity` 未設定或不是 `maps\...\global.cfg` 形式（驗證）；`NewMap` 且識別碼未安裝（規劃器） | 兩者 |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` 未設定 `EntrypointIdentity`，且 `PresentedEntrypointIndex` 未設定或為負數 | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`、地圖已安裝、沒有 `EntrypointIdentity`、`PresentedEntrypointIndex` 未設定（`world.presented-entrypoint` 無法使用） | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` 沒有 `SituationIdentity`（驗證），或識別碼未安裝（規劃器） | 兩者 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`：`.osn` 內指定的地圖未安裝 | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | 不是在 x64 作業系統上以 x64 主機處理程序執行的 Windows 10 以上版本（`runtime.current-windows-x64`） | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | 根目錄不存在、設有唯讀屬性，或沒有 `plugins\` 子目錄（`transaction.exact-restore`） | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | 不是 OMSI 2 安裝：`Omsi.exe` 不存在、不是 32 位元 x86 PE 映像、不是 OMSI 2（2.x）版本，或沒有 `maps\` / `Vehicles\` 目錄（`omsi.omsi2`）。雜湊不是判斷條件。 | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | 設定了 `EntrypointIdentity`；`SavedSituation` 搭配不是 `Unset` 的 `Date`/`Time`/`Year` 模式；`Weather` 模式不是 `Unset`；設定了任何 `PlayerVehicle` 欄位 | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`, `OL_E_REPAINT_NOT_FOUND`, `OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` 未安裝（另外也會有 `OL_E_CAPABILITY_UNAVAILABLE`） | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`, `OL_E_SETTING_NOT_WRITABLE` | `Environment` 鍵不在目錄中／不可寫入 | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | 建立啟動畫面／ITX 計畫時擲回例外；訊息中帶有 `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID` 或 `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | 無法載入外掛程式封閉集合參考（`OmsiLaunchRuntimePaths`）或發行資訊清單（manifest）（訊息可能帶有 `OL_E_RELEASE_MANIFEST_INVALID`） | `OmsiLaunchService.PlanSessionAsync` |

規劃時不會驗證（在啟動時以帶有 `OL_E_START_SESSION` 的 `Failed` 工作階段失敗）：設定值（`OL_E_INVALID_SETTING_VALUE`）、永久外掛程式完整性（`OL_E_PERMANENT_PLUGIN_*`）、租用可用性（`OL_E_INSTALLATION_BUSY`）、`StartupTimeoutSeconds` 範圍（由 `StartSessionAsync` 擲回）。

資訊性計畫診斷訊息：`plugin.integrity.reference`（訊息為 `manifest` 或 `self`）、`session_profile.selected`。

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## 優先順序：CLI 旗標、spec 檔案與工作階段設定檔

`CliInput.BuildSpecAsync`（`tools/OmsiLaunch.Cli/Program.cs`）依下列順序建立有效的 spec：

1. 種子 = 內建預設值；若有指定 `/spec` 檔案則為該檔案。
2. 安裝根目錄 = 若有指定明確的安裝引數則使用之，否則使用種子的 `RootPath`；接著 `.`／空白 → 可執行檔所在目錄，再套用 `Path.GetFullPath`。明確的安裝引數一律優先於 spec 的 `RootPath`。
3. 工作階段設定檔（`/predefined-profile` + `/predefined-profile-index`）：觸及設定檔所擁有欄位的明確 CLI 引數會以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 拒絕（世界欄位僅限 NEW_MAP 模式；預設組合中存在的 `/set` 鍵；預設組合含 `presentation` 時的啟動畫面旗標；含 `internet-textures` 時的 Internet Textures 旗標；含 `behavior` 時的逾時旗標）。種子的 `World` 會被全新的一個取代（只保留 CLI 世界模式），接著套用設定檔的 `new:` 區塊（僅 NEW_MAP）、`settings`（併入 `General`）、`presentation`、`internet-textures`、`behavior` 以及 `SessionProfile` 中繼資料。`compatibility.maps` 會對 NEW_MAP 與 SAVED_SITUATION 強制執行。
4. 世界：CLI 世界模式一律優先（`/new` 為預設、`/saved:<osn>`、`/last`）；spec 檔案的 `World.Mode` 會被取代。若要從 spec 執行已儲存情境，請傳入 `/saved:`。`/map` 與 `/entrypoint`/`/entrypoint-index` 會覆寫種子；CLI 的 `/entrypoint` 識別碼會清除索引；`/saved` 搭配 `/map` 或進入點旗標為 `OL_E_INVALID_ARGUMENT`。
5. 有指定 `/date`、`/time`、`/year`、`/weather*` 時會覆寫種子（`system` 選取 `DateTimeMode.System`）。
6. `/no-vehicle` 會清除 `PlayerVehicle`；個別的 `/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration` 會覆寫種子玩家車輛的個別欄位。
7. `/set:<key>=<value>` 項目會加入 `Environment.General`（會檢查鍵，不檢查值）；其他七個群組原封不動地來自種子。
8. `/startup-timeout` 與 `/shutdown-timeout` 只在有指定時覆寫種子；否則依序套用 spec、設定檔，最後是預設值 180 s / 30 s。`ShutdownTimeoutSeconds` 在搭配 `/cooperative-shutdown` 時生效（它會與 spec 的值做 OR 運算）。
9. 有指定 `/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` 時會覆寫種子；`SuppressTrayIcon` 只來自種子。
10. 診斷旗標會與種子做 OR 運算。

結果：明確的 CLI 旗標 > 工作階段設定檔 > spec 檔案 > 內建預設值；但與設定檔所擁有欄位衝突的 CLI 旗標會被視為錯誤，而不是覆寫。

<a id="path-rules"></a>
## 路徑規則

| 路徑 | API 行為 | CLI 行為 |
| --- | --- | --- |
| `Installation.RootPath` | 依原樣使用：在檔案操作中，相對路徑會以處理程序的工作目錄為基準解析。請傳入絕對路徑。租用、日誌與內容目錄會以 `Path.GetFullPath` 將其正規化。 | `.` 或空白 = 包含 `OmsiLaunch.exe` 的目錄，絕不是呼叫端的工作資料夾；明確的安裝引數優先於 spec；結果會轉為絕對路徑。 |
| `Presentation.CustomAssetDirectory` | 絕對路徑，或相對於 `Installation.RootPath`。必須存在。 | 相同（`/splash-assets`）。工作階段設定檔的 `assets` 路徑受限於設定檔套件內，並以絕對路徑儲存。 |
| `InternetTextures.OverrideProfilePath` | 以 `Path.GetFullPath` 解析，亦即相對於處理程序的工作目錄，而非安裝根目錄。必須存在。 | 相同（`/internet-textures-profile`）。工作階段設定檔的 `profile` 路徑受限於套件內，並以絕對路徑儲存。 |
| ITX 目標行 | 相對於安裝根目錄；必須包含 `Texture\` 元件；不可為根路徑、不可含 `..`、不可以 `\` 開頭、不可含 junction/symlink 元件。 | 相同。 |
| 內容識別碼（`MapIdentity`、`SituationIdentity`、`PlayerVehicle.*`） | 相對於安裝、不區分大小寫、接受 `/`；絕不是絕對路徑。 | 相同。 |

<a id="carried-but-not-applied"></a>
## 會攜帶但不套用

| 欄位 | 目前效果 | 穩定性 |
| --- | --- | --- |
| `Diagnostics.Log`、`Verbose`、`OmsiLogAll`、`ProcessTrace` | 無（主機追蹤記錄一律寫入） | `PARTIAL` |
| `PlayerVehicle.*`（設定任何欄位） | 為診斷訊息解析內容，計畫不可執行（`UNAVAILABLE`） | `PARTIAL` |
| `SessionProfile` | 僅供來源追溯的診斷訊息 | `STABLE_BETA` |
