# 功能

<!-- l10n: source=reference/capabilities.md source-sha256=e6d9d063c2365c007efc79c0057a4843d1f91b88ba68448cc8b8d7579308de23 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/capabilities.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁記載 OmsiLaunch 能做什麼。其來源是標準功能目錄 `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）：每項功能、每個公開執行階段操作（含其參數、CLI 路由與原生功能），以及每項功能的一個證據狀態，都只在該處宣告一次。執行階段操作集合、參數驗證、CLI 命令路由、外掛程式的各操作原生閘門、`GetCapabilitiesAsync`，以及下方的[功能目錄表](#capability-catalog)，皆由它衍生。對每個執行階段操作，本頁列出參數、結果鍵與錯誤。如何叫用操作（CLI 路由、API 封套、逾時、handle）請見[執行階段控制](runtime-control.md)；各工作階段的證據請見[執行階段驗證狀態](../status/runtime-validation-status.md)。

<a id="classification-and-stability"></a>
## 分類與穩定性

`PublicCapabilityClassification` 有四個值。它們與穩定性用語的對應如下；證據不完整之處會降級：

| 分類 | 意義 | 穩定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公開、屬於 Beta 合約、已於執行階段驗證 | `STABLE_BETA`（註明之處降級為 `PARTIAL`） |
| `PublicExperimental` | 公開、可能變更、已於執行階段驗證或已靜態驗證 | `EXPERIMENTAL`（註明之處降級為 `PARTIAL`） |
| `InternalOnly` | 研究用基本操作；無法透過公開 API 或 CLI 存取 | `INTERNAL` |
| `Unsupported` | 為診斷而辨識，但會被拒絕或不存在 | `UNAVAILABLE` |

`PublicCapabilityKind` 區分 `Read`、`Write`、`Action` 與 `Event`。每一項執行階段功能都需要處於 `Running` 的工作階段（登錄中的 `RequiresSession`），且其原生功能必須在執行中的組建上已解析並驗證；原生功能無法使用的操作會以 `OL_E_CAPABILITY_UNRESOLVED` 拒絕。`RequiresExactProfile` 標示依賴原生目標的功能（請參閱[相容性](compatibility.md)）。

執行階段結果是字串字典。以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的鍵會在 API 邊界被移除（`ScrubInternalValues`），因此不列於此處。下列操作表中的結果鍵即為產品回傳的確切鍵集合：它們是在實際工作階段中執行每一項公開操作所擷取的（runtime closure 文件擷取，工作階段 `f51dcb59-a723-4570-b6c5-b61e628a7993`，`situations\Linie 5.osn`；清單資料列寫作 `<row>.<n>.<field>`）。

操作失敗的回報方式（`CurrentRuntimeControl.Execute`）：

| 失敗 | `ErrorCode` | `Values` |
| --- | --- | --- |
| 登錄拒絕（未知或 `internal.*` 操作、缺少必要參數） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 無 |
| 產品刻意拒絕（`weather.set` 的衍生索引鍵） | 特定錯誤碼（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（一句說明） |
| D3D 失敗 | 特定的 `OL_E_D3D_*` 錯誤碼 | `detail`、`native_status` |
| 外掛程式端具型別的失敗（失效的 handle、未知名稱、值超出範圍、沒有玩家車輛……） | 特定錯誤碼，例如 `OL_E_RUNTIME_OBJECT_HANDLE_STALE` 或 `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail`（訊息）、`exception`（.NET 型別名稱） |
| 原生功能在執行中的組建上未解析的操作 | `OL_E_CAPABILITY_UNRESOLVED` | `detail`、`capability`（該原生功能） |
| 沒有錯誤碼的外掛程式端非預期例外 | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`、`exception` |
| 無法放入 64 KiB 信箱（mailbox）的結果 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（分頁清單則改為縮短並以 `next_cursor` 繼續，請參閱[分頁](#pagination)） | 無 |

各表的 `Errors`（「錯誤」）欄列出特定錯誤碼；它們會經由外掛程式、橋接層、控制平面、API 與 CLI 原封不動地送達 `ErrorCode`。

<a id="capability-catalog"></a>
## 功能目錄

每個目錄項目一列，由 `PublicCapabilityRegistry.All` 產生（此表過期時文件關卡會失敗）。`Session` 為 `Running` 表示該功能需要執行中的工作階段。`Operations` 是具體的執行階段操作 id（合約資料列位於[公開執行階段操作](#public-runtime-operations)）。`Native capability` 指名原生目標目錄的項目（請參閱[相容性](compatibility.md)），它們必須在執行中的組建上已解析並驗證。`Evidence` 使用同一套詞彙：

| 證據 | 意義 |
| --- | --- |
| RUNTIME_VALIDATED | 在真實的 OMSI 工作階段中觀察到；工作階段 id 位於[執行階段驗證狀態](../status/runtime-validation-status.md)。 |
| STATICALLY_VALIDATED | 已實作並離線驗證；尚未觀察到執行階段效果。 |
| PARTIAL | 合約的一部分可運作；備註會指出尚未完成的部分。 |
| RUNTIME_VALIDATION_REQUIRED | 已實作，等待其執行階段驗證。 |
| UNAVAILABLE | 為診斷而辨識並拒絕，或未實作；在啟動時要求它會使計畫不可執行。 |

會變更內容的操作在其 id 之後註明其變更原則（`PublicRuntimeOperation.MutationPolicy`）；沒有任何執行階段操作會被記入日誌：

| 變更原則 | 意義 |
| --- | --- |
| `none` | 僅讀取（不顯示）。 |
| `omsi-state` | 變更 OMSI 的記憶體內狀態；沒有任何機制會復原它；它隨 OMSI 處理程序結束而結束。 |
| `session-resource` | 建立或變更工作階段擁有的資源，明確釋放或在工作階段結束時釋放。 |
| `session-policy` | 安裝一項原則，由外掛程式持續重新套用，直到被移除或工作階段結束。 |

<!-- generated:capability-catalog:begin -->
| 功能 | 分類 | 種類 | 工作階段 | 操作 | 原生功能 | 證據 | CLI | 備註 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4：外掛程式事件經由編號環狀緩衝區傳遞；落後的讀取端會收到附帶計數的 telemetry.lost。 |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 第 A 輪：已知配置。 |
| `boot.headless-start` | `PublicStableBeta` | Action | 否 |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4（BI-001）：OMSI 會呈現每個精確標籤的第一筆記錄；啟動時以該標籤選取它。已靜態收尾；執行階段驗證待完成。 |
| `world.saved-situation` | `PublicStableBeta` | Action | 否 |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | 否 |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4：原生分支已靜態收尾（BI-006）；執行階段驗證待完成。絕不使用「最新 .osn」的啟發式做法。 |
| `world.explicit-date` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4：在 NewSituation 之前寫入 OMSI 的起始時鐘（靜態分析）；執行階段驗證待完成。 |
| `world.explicit-time` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4：在 NewSituation 之前寫入 OMSI 的起始時鐘（靜態分析）；執行階段驗證待完成。 |
| `world.explicit-year` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4：在 NewSituation 之前寫入 OMSI 的起始時鐘（靜態分析）；執行階段驗證待完成。 |
| `world.weather` | `PublicExperimental` | Action | 否 |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4（BI-003）：世界啟動後立即透過 OMSI 自己的 loadWeather 或其 METAR 旗標套用；它會覆寫已儲存情境的天氣。已靜態收尾；執行階段驗證待完成。 |
| `player-vehicle.model` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007；為診斷而解析，不套用。 |
| `player-vehicle.repaint` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007. |
| `player-vehicle.hof` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/hof:<id>` | BI-007. |
| `player-vehicle.fleet-number` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007. |
| `player-vehicle.registration` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/registration:<r>` | BI-007. |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | 無介面啟動時為 present=false；搭配已儲存情境時為完整快照。 |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007：原生指派序列已靜態收尾（research/reports/beta4-static/entrypoint-last-player-vehicle.md 第 3 節），但在執行階段驗證之前不實作；預計於後續 Beta 提供。 |
| `configuration.options.semantic` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005. |
| `input.keyboard` | `PublicExperimental` | Write | 否 |  |  | STATICALLY_VALIDATED |  | Beta 4：交易式 overlay，離線驗證。 |
| `input.controller` | `PublicExperimental` | Write | 否 |  |  | STATICALLY_VALIDATED |  | Beta 4：交易式 overlay，離線驗證。 |
| `presentation.splash` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | 否 |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 第 B 輪：抑制已套用並回報（internet-textures.suppressed），經由遙測環狀緩衝區。 |
| `internet-textures.override` | `PublicExperimental` | Write | 否 |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | 執行階段收尾 I01。 |
| `session-profiles` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 設定檔啟動；1..99 與探索為離線驗證。 |
| `hof.management` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 第 A 輪 C01、C02。 |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read`（分頁） | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 已靜態收尾：SetActualDateTime 只會把電腦時鐘複製到起始時鐘；起始日期請使用 world.explicit-date。已被取代：原生 ABI 與後置條件尚未收尾。 |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4：寫入 actWeather（Weather+0x08），即 OMSI 天氣更新週期所推導的記錄；已靜態收尾。衍生值（濕度、光線係數、實際風）保持唯讀（OL_E_RUNTIME_SETTING_NOT_PERSISTENT）。執行階段驗證待完成。 |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list`（分頁） | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | 需要 PlayerVehicle（已儲存情境會提供一個）；無介面 NEW_MAP 上為 OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE。 |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5：已靜態驗證的設計（研究文件 beta4-static pause-menu-and-input.md）；原生呼叫尚未在真實工作階段中觀察到。 |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list`（分頁） | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | 自然移除後的失效偵測（RV-002）沒有安全的執行階段產生方式，改以離線驗證。 |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list`（分頁） | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list`（分頁）, `timetable.trips.list`（分頁）, `timetable.lines.list`（分頁）, `timetable.rv-files.list`（分頁）, `timetable.track-entries.list`（分頁）, `timetable.bus-stops.list`（分頁）, `timetable.station-links.list`（分頁）, `timetable.tours.list`（分頁）, `timetable.profiles.list`（分頁）, `timetable.tour-entries.list`（分頁）, `timetable.logs.read`（分頁）, `timetable.no-rv-numbers.list`（分頁） | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list`（分頁）, `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list`（分頁）, `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | 寫入：scripts.string.write。 |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4（BI-004）：OMSI 複製常數形狀的字串；已靜態收尾。執行階段驗證待完成。 |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4（BI-004）：經由已剖析的 VMT 槽位呼叫虛擬 TriggerXML/SoundTrigger；已靜態收尾。執行階段驗證待完成。 |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list`（分頁）, `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list`（分頁）, `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read`（分頁） | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read`（分頁） | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | 已觀察到裝置重設（resetting、restored、世代失效）；未產生獨立的 lost 轉換（RV-007）。 |
<!-- generated:capability-catalog:end -->

`internal.make-basic` 為 `InternalOnly`：它存在於外掛程式中供研究使用（會回傳原生位址），並在 API 邊界與本機控制平面被拒絕，兩者都會先依 `PublicRuntimeOperationIds` 驗證操作名稱；CLI 沒有對應的路由。`session.stop` 會終止 OMSI；未實作協同式關閉。

<a id="pagination"></a>
## 分頁

每個清單操作（在[功能目錄](#capability-catalog)中標示「（分頁）」者）接受兩個選用參數，並回應四個頁面欄位：

| 參數 | 意義 |
| --- | --- |
| `cursor` | 頁面的起點：上一頁的 `next_cursor`。省略時：第一頁。來自另一個清單或格式錯誤的 cursor 為 `OL_E_RUNTIME_CURSOR_INVALID`。 |
| `limit` | 所要的資料列數（正整數）。大於該清單的最大頁面大小時：使用最大值。不是正整數：`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`。 |

| 結果鍵 | 意義 |
| --- | --- |
| `count` | OMSI 持有的所有記錄。 |
| `returned_count` | 此回覆中的資料列。 |
| `truncated` | 此頁之後仍有記錄時為 `true`。 |
| `next_cursor` | 僅在後面仍有記錄時存在：將它作為下一頁的 `cursor` 傳入。 |

cursor 是穩定的索引鍵，而非位置。對於記錄陣列（時刻表、指令碼名稱、常數、曲線、HOF、駕駛員、車票、記錄），資料列以 `<row>.<n>.<field>`（或 `name.<n>`）為索引鍵，依記錄的絕對索引編號，cursor 指名下一個索引，因此第一頁等同 Beta 3 的回覆。對於 handle 集合（`road-vehicles.list`、`humans.list`），資料列依 handle 順序傳回，cursor 指名最後傳回的 handle：兩頁之間出現的物件會得到較後的 handle 並出現在較後的頁面；沒有任何物件會重複。請將 cursor 值視為不透明。

最大頁面大小：`road-vehicles.list` 與 `humans.list` 為 1000；`timetable.track-entries.list`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list` 與 `vehicle.curves.list` 為 512；`timetable.tour-entries.list`、`timetable.logs.read` 與 `vehicle.hofs.read` 為 256；其他時刻表清單、`drivers.read` 與 `tickets.read` 為 128。當一頁的資料列仍超過 64 KiB 信箱時，外掛程式會捨棄最後的資料列直到回覆符合大小，且 `next_cursor` 從第一個被捨棄的資料列繼續，因此沒有任何清單會因大小而失敗。

<a id="public-runtime-operations"></a>
## 公開執行階段操作

以下是公開前端可轉送之操作 id 的確切清單（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。每一項都需要 `SessionState.Running`；沒有任何一項會記入日誌或被還原。必要參數在使用信箱之前由登錄強制檢查（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）；選用參數由外掛程式驗證。所有操作的共通失敗錯誤碼：`OL_E_RUNTIME_OPERATION_UNKNOWN`（不在此清單中）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（外掛程式沒有實作）、`OL_E_RUNTIME_OPERATION_FAILED`（非預期的處理程序內例外；附 `detail` 與 `exception` 值）。

<a id="time"></a>
### 時間

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `time.read` | Read | 無 | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 選用 `hour`（0..23）、`minute`（0..59）、`second`（0..59.999，小數）；至少一項 | 經設定檔定義之 `SetTime` 後的 `time.read` 鍵 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天氣

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | 無 | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`，以及來源值 `set.fog_density`、`set.lightness`、`cloud_type_name`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads`。`fog_density`、`lightness`、`wind_speed`、`wind_direction`、光線係數與濕度是 OMSI 每個更新週期的值；`cloud_type` 是解析後的雲型編號 | |
| `weather.set` | Write | 下列至少一項：`fog_density`、`lightness`、`wind_speed`、`wind_direction`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`、`cloud_type`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads`。範圍：霧 0..100000，`lightness` 與地面值 0..1，風速 0..200，風向 0..360，溫度與露點 -60..60，氣壓 850..1100，降水 0..2，速率 0..255，雲高 0..20000，`cloud_type` 為字碼頁 1252 的 1..64 個字元（`-1` = 無雲），布林值 `true`/`false`/`1`/`0` | `weather.read` 的鍵；使用 `cloud_type` 時另有 `cloud_type_resolved`（OMSI 不認得該雲型時為 `false`：它不繪製任何雲） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT` （衍生索引鍵：`relative_humidity`、`absolute_humidity`、光線係數、`cloud_transparency`、`precipitation_set`、`wet_ground`；detail 會指出應設定的索引鍵）, `OL_E_RUNTIME_SETTING_UNAVAILABLE`（未知索引鍵）, `OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao`（4 個字母或數字） | `weather.actual.read` 的鍵 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | 無 | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### 地圖

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `map.read` | Read | 無 | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### 攝影機

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | 無 | `family`（0 駕駛員、1 乘客、2 外部、3 地圖）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 選用 `family`（0..3）、`field_of_view`（10..170）；至少一項 | `camera.read` 的鍵 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必要 `family`（0..3）；選用 `preset`（0..255，僅限 family 0 或 1） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。此原則每 100 ms 重新套用一次，直到 `camera.unlock`；重新套用失敗時，每種不同錯誤只會發出一次 `camera.lock.degraded` 事件。 |
| `camera.unlock` | Action | 無 | `locked`（`false`） | |

### User interface (pause menu)

接管 OMSI 的原生暫停選單，改為模組自己的選單（功能 `ui.pause-menu.replace`，僅在已知程式碼身分上解析）。OMSI 本身的暫停位元組維持設定，因此模擬、轉譯、3D 點擊與 DirectInput 命令都維持被擱置；原生表單透過 VCL 隱藏，並直接呼叫其動作（沒有點擊、沒有投遞訊息、沒有移動視窗）。`ui.pause.action` 會在送出其回應之後，由一次性的 UI 執行緒計時器執行原生處理常式，因為該處理常式可能進入原生強制回應對話方塊：請從 `ui.pause.state` 讀取結果（`busy` 為 `false`，然後是 `last_result`）。

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | 無 | `paused`、`native_menu_visible`、`main_form_ready`、`in_frame`、`taken_over`、`busy`、`pending_action`、`last_action`、`last_result`（`none`、`resumed`、`game-started`、`returned`、`quit-requested`、`exception`、`refused`） | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | 無 | 接管後的 `ui.pause.state` 鍵 | `OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_UI_ALREADY_TAKEN_OVER`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | 無 | `ui.pause.state` 的鍵；再次顯示原生選單（其自身的 `FormShow` 會執行：OMSI 仍維持暫停） | `OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | 必要 `action`：`resume`、`new-game`、`save`、`options` 或 `quit` | `ui.pause.state` 的鍵，加上 `scheduled`（`true`）與 `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |

<a id="road-vehicles-and-player"></a>
### 道路車輛與玩家

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | 無 | `count`、`ai_collection`、`player_index`（OMSI 原始的玩家車輛索引，照讀取值回報；要知道是否存在玩家車輛，請使用 `player-vehicle.read` 的 `present`） | |
| `road-vehicles.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`vehicle.<n>.handle` (`rv-NNNNNN`；`<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | 必要 `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | 無 | `present`（OMSI 沒有玩家車輛時為 `false`）；為 `true` 時：`player_index` 加上所有 `road-vehicle.read` 的鍵 | |
| `road-vehicles.spawn` | Action | 必要 `model`（標準形式 `Vehicles\...\*.bus`，最多 240 個字元，不可含 `..`，必須存在於安裝目錄之下） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。不會指派 PlayerVehicle。需要數秒；請使用 30 s 的用戶端逾時。 |
| `road-vehicles.place-random` | Action | 選用 `ai_type`（0..255，預設 0）、`group`（0..65535，預設 1）、`type`（-1..65535，預設 -1）、`scheduled`（0..1，預設 0）、`tour`（0..65535，預設 0）、`line`（0..65535，預設 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | 無 | `count` | |
| `humans.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`human.<n>.handle` (`hb-NNNNNN`；`<n>` is OMSI's list position) | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | 必要 `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 時刻表、駕駛員、車票

時刻表清單、`drivers.read` 與 `tickets.read` 為分頁（請參閱[分頁](#pagination)）；下文的「頁面欄位」指 `count`、`returned_count`、`truncated` 與 `next_cursor`，且每個清單都接受選用的 `cursor` 與 `limit`。

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | 無 | `invalid`（`true`/`false`），以及記錄數 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | 選用 `cursor`、`limit` | 頁面欄位；`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`no_rv.<n>.group`、`no_rv.<n>.number`：OMSI 不讓其自由用於 AI 的車輛編號（OMSI 保留編號的字串清單，透過其自身配置讀取；清單不具預期形狀時為 `OL_E_RUNTIME_VALUE_INVALID`）。每頁最多 256 筆。靜態分析；執行階段驗證待完成。 |
| `map.tiles.list` | Read | 選用 `cursor`、`limit` | 頁面欄位；`tile.<n>.x`、`tile.<n>.y` （格線位置）, `tile.<n>.tile_index` （圖磚在地圖圖磚清單中的自身索引）, `tile.<n>.filename`、`tile.<n>.version`、`tile.<n>.unsaved`、`tile.<n>.loaded`、`tile.<n>.load_requested`、`tile.<n>.failed`、`tile.<n>.thread_loading`、`tile.<n>.prepared_for_ode`、`tile.<n>.width_south`、`tile.<n>.width_north`、`tile.<n>.objects`、`tile.<n>.path_groups`、`tile.<n>.path_segments`、`tile.<n>.surface_objects`、`tile.<n>.spline_segments`；無法讀取的圖磚為 `tile.<n>.state` = `unavailable` 並附 `tile.<n>.error`。每頁最多 256 筆。位置與索引來自靜態分析；執行階段驗證待完成。 |
| `drivers.read` | Read | 選用 `cursor`、`limit` | `count`、`selected_index`；`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | 選用 `cursor`、`limit` | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`；`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 車輛腳本、常數、曲線、HOF（以 handle 為範圍）

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必要 `handle`；選用 `cursor`、`limit` | `handle`、頁面欄位, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必要 `handle`、`name`、`value`（有限浮點數） | `handle`、`name`、`requested_value`、`value`（回讀值） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必要 `handle`；選用 `cursor`、`limit` | `handle`、頁面欄位, `name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | 必要 `handle`、`name`（區分大小寫，與指令碼的宣告一致，最多 128 個字元）、`value`（1..1024 個字元） | `handle`、`name`、`value` 於寫入後讀回 | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（OMSI 本身會靜默忽略未知名稱）、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | 必要 `handle`、`name`（字碼頁 1252，最多 128 個字元）、`value`（`1` 執行 `name`、`0` 執行 `name_off`、`2` 執行 `name_drag`） | `handle`、`name`、`value`、`dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`。指令碼進入點會同步執行；指令碼未定義的名稱不會有任何作用。按下之後通常會接著放開 |
| `vehicle.sound-trigger` | Action | 必要 `handle`、`trigger`（字碼頁 1252，最多 128 個字元）；選用 `file`（最多 260 個字元） | `handle`、`trigger`、`file`、`dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`（亦包括：車輛沒有音效包）、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | 必要 `handle`；選用 `cursor`、`limit` | `handle`、頁面欄位, `name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必要 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必要 `handle`；選用 `cursor`、`limit` | `handle`、頁面欄位, `name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必要 `handle`、`name`、`x`（有限浮點數；缺少 `x` 或非數值時為 `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（線性內插） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必要 `handle`；選用 `cursor`、`limit` | `handle`、頁面欄位, `hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作透過原生橋接層在 OMSI 的主執行緒／轉譯執行緒上執行。材質 handle 的格式為 `d3dtex-<sessionId N>-<16 hex digits>`。

| 操作 | 種類 | 參數 | 結果鍵 | 錯誤 |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | 無 | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必要 `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）；選用 `levels`（0..16，預設 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必要 `handle`；選用 `level`（0..15，預設 0） | 所要求層級的 13 個 `d3d.texture.create` 鍵 | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`，以及 create 的錯誤 |
| `d3d.texture.update` | Action | 必要 `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（解碼後最多 48 KiB）；選用 `level`（0..15，預設 0）、`x`（0..4095，預設 0）、`y`（0..4095，預設 0） | 更新後的 13 個 `d3d.texture.create` 鍵 | `OL_E_D3D_INVALID_PIXEL_BUFFER`，以及 describe 的錯誤 |
| `d3d.texture.release` | Action | 必要 `handle` | 13 個 `d3d.texture.create` 鍵，其中 `state` = `RELEASED` | 重複釋放時為 `OL_E_D3D_RESOURCE_RELEASED`、`OL_E_D3D_STALE_RESOURCE_HANDLE` |

D3D 操作失敗時會回傳 `detail` 與 `native_status` 值。`D3DRuntimeApi` 擴充方法（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）為 API 使用者包裝了這些操作。

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` 會針對每個非 `InternalOnly` 的目錄項目回傳一個 `Capability(Name, Available, EvidenceState, Reason)`。`Name` 是目錄 id，`EvidenceState` 是其目錄證據。`Available` 依安裝評估：

- `Unsupported` 項目永遠無法使用；`Reason` 帶有目錄備註。
- 每個項目都需要目前的平台，且除 `runtime.current-windows-x64` 之外的每個項目都需要 OMSI 2 安裝。
- 在已知配置上具有原生功能的項目會回報 `native: RESOLVED_KNOWN_LAYOUT`。在另一個 OMSI 2 組建上，執行檔會被靜態解析：`native: STATICALLY_RESOLVED`，或指名檔案未能解析之內容的 `native: PENDING_IN_PROCESS_RESOLUTION`。待處理的項目仍會被回報為可用，因為由處理程序內的解析決定；若失敗，操作或啟動會以 `OL_E_CAPABILITY_UNRESOLVED` 失敗。

規劃器會在 `SessionPlan.RequiredCapabilities` 與 `SessionPlan.UnsupportedRequestedFeatures` 中以相同的目錄 id 回報各計畫的功能（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.omsi2`、`world.*`、`boot.headless-start`、`internet-textures.disabled`、`player-vehicle.*`、`input.*`、`content.*`）。已要求但不支援的功能會帶有其目錄證據；必要的原生功能則帶有 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`。

<a id="known-limitations"></a>
## 已知限制

- Handle 以工作階段為範圍，其物件消失後會回報為 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`。車輛 handle 綁定其 VMT、其定義與 OMSI 的 `IDCode`（每個地圖物件在建構時取得、處理程序內唯一且絕不重複使用的編號），因此在相同位址重新建立的物件一定能被區分。人物是在整個地圖工作階段中存活的集區槽位：當人物消失（`RenderMe` 為 0）或該槽位被再次生成（新的目標站或發車）時，人物 handle 便不再解析。
- 結果受限於 64 KiB 信箱；每個清單都是分頁的（請參閱[分頁](#pagination)）。
- 不支援跨圖塊（tile）移動車輛、行事曆寫入，也不支援無頭模式下的 PlayerVehicle 指派。字串變數寫入、指令碼觸發與天氣寫入為 `EXPERIMENTAL`（執行階段驗證待完成）。請參閱[已知限制](known-limitations.md)。
