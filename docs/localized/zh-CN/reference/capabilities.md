# 能力

<!-- l10n: source=reference/capabilities.md source-sha256=e6d9d063c2365c007efc79c0057a4843d1f91b88ba68448cc8b8d7579308de23 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/capabilities.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页记录 OmsiLaunch 能做什么。其来源是规范能力目录 `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）：每项能力、每个公开的运行时操作（连同其参数、CLI 路由和原生能力），以及每项能力的一个证据状态，都只在那里声明一次。运行时操作集、参数校验、CLI 命令路由、插件按操作设置的原生门禁、`GetCapabilitiesAsync` 以及下面的[目录表](#capability-catalog)都由它派生。对每个运行时操作，本页给出参数、结果键和错误。如何调用某个操作（CLI 路由、API 信封、超时、句柄）见[运行时控制](runtime-control.md)；各会话的证据见[运行时验证状态](../status/runtime-validation-status.md)。

<a id="classification-and-stability"></a>
## 分类与稳定性

`PublicCapabilityClassification` 有四个取值。它们与稳定性术语的对应关系如下，证据不完整之处会降级：

| 分类 | 含义 | 稳定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公开，属于 Beta 契约，经运行时验证 | `STABLE_BETA`（注明之处降级为 `PARTIAL`） |
| `PublicExperimental` | 公开，可能变更，经运行时验证或经静态验证 | `EXPERIMENTAL`（注明之处降级为 `PARTIAL`） |
| `InternalOnly` | 研究用原语；无法通过公共 API 或 CLI 访问 | `INTERNAL` |
| `Unsupported` | 可被识别以用于诊断，但会被拒绝或不存在 | `UNAVAILABLE` |

`PublicCapabilityKind` 区分 `Read`、`Write`、`Action` 和 `Event`。每项运行时能力都需要处于 `Running` 状态的会话（注册表中的 `RequiresSession`），以及其原生能力在正在运行的版本上已解析并校验；其原生能力不可用的操作会以 `OL_E_CAPABILITY_UNRESOLVED` 被拒绝。`RequiresExactProfile` 标记依赖原生目标的能力（参见[兼容性](compatibility.md)）。

运行时结果是字符串字典。以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的键会在 API 边界处被移除（`ScrubInternalValues`），本页不予列出。下文各操作表中的结果键是产品实际返回的精确键集合：它们是在真实会话中执行每个公开操作后采集的（运行时收尾文档采集，会话 `f51dcb59-a723-4570-b6c5-b61e628a7993`，`situations\Linie 5.osn`；列表行写作 `<row>.<n>.<field>`）。

操作失败的报告方式（`CurrentRuntimeControl.Execute`）：

| 失败 | `ErrorCode` | `Values` |
| --- | --- | --- |
| 注册表拒绝（未知操作或 `internal.*` 操作、缺少必需参数） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | 无 |
| 产品有意的拒绝（`weather.set` 中的派生天气键） | 具体错误码（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（一句说明） |
| D3D 失败 | 具体的 `OL_E_D3D_*` 错误码 | `detail`、`native_status` |
| 插件端带类型的失败（失效句柄、未知名称、值超出范围、无玩家车辆……） | 具体错误码，例如 `OL_E_RUNTIME_OBJECT_HANDLE_STALE` 或 `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail`（消息）、`exception`（.NET 类型名称） |
| 其原生能力在正在运行的版本上未解析的操作 | `OL_E_CAPABILITY_UNRESOLVED` | `detail`、`capability`（该原生能力） |
| 没有错误码的意外插件端异常 | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`、`exception` |
| 结果无法放入 64 KiB 邮箱 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（分页列表则改为缩短，并通过 `next_cursor` 继续，见[分页](#pagination)） | 无 |

各表的 `Errors` 列列出具体错误码；它们经由插件、桥、控制平面、API 和 CLI 原样到达 `ErrorCode` 中。

<a id="capability-catalog"></a>
## 能力目录

每个目录条目一行，由 `PublicCapabilityRegistry.All` 生成（该表过期时，文档门禁会使构建失败）。`Session` 对需要运行中会话的能力为 `Running`。`Operations` 是具体的运行时操作 id（契约行见[公开运行时操作](#public-runtime-operations)）。`Native capability` 列出原生目标目录（参见[兼容性](compatibility.md)）中必须在正在运行的版本上已解析并校验的条目。`Evidence` 使用同一套词汇：

| 证据 | 含义 |
| --- | --- |
| RUNTIME_VALIDATED | 已在真实的 OMSI 会话中观察到；会话 ID 见[运行时验证状态](../status/runtime-validation-status.md)。 |
| STATICALLY_VALIDATED | 已实现并经离线验证；其运行时效果尚未被观察到。 |
| PARTIAL | 其契约的一部分可用；说明列指出未完成的部分。 |
| RUNTIME_VALIDATION_REQUIRED | 已实现，等待其运行时验证。 |
| UNAVAILABLE | 为诊断信息而被识别并被拒绝，或尚未实现；在启动时请求它会使计划不可运行。 |

会更改某些内容的操作会在其 id 之后标出其修改策略（`PublicRuntimeOperation.MutationPolicy`）；没有任何运行时操作会记入事务日志：

| 修改策略 | 含义 |
| --- | --- |
| `none` | 仅读取（不显示）。 |
| `omsi-state` | 更改 OMSI 的内存状态；没有任何机制会撤销它；它随 OMSI 进程一同结束。 |
| `session-resource` | 创建或更改会话拥有的资源，显式释放，或在会话结束时释放。 |
| `session-policy` | 安装一个策略，插件会反复应用它，直到该策略被移除或会话结束。 |

<!-- generated:capability-catalog:begin -->
| 能力 | 分类 | 类型 | 会话 | 操作 | 原生能力 | 证据 | CLI | 说明 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4：插件事件通过带编号的环传递；落后的读取方会收到带有计数的 telemetry.lost。 |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 A 轮：已知布局。 |
| `boot.headless-start` | `PublicStableBeta` | Action | 否 |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | 否 |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4（BI-001）：OMSI 只呈现每个精确标签的第一条记录；启动时按该标签选择它。已通过静态分析确定；运行时验证待完成。 |
| `world.saved-situation` | `PublicStableBeta` | Action | 否 |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | 否 |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4：原生分支已通过静态分析确定（BI-006）；运行时验证待完成。绝不是“最新 .osn”的启发式方法。 |
| `world.explicit-date` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4：在 NewSituation 之前写入 OMSI 的启动时钟（静态分析）；运行时验证待完成。 |
| `world.explicit-time` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4：在 NewSituation 之前写入 OMSI 的启动时钟（静态分析）；运行时验证待完成。 |
| `world.explicit-year` | `PublicExperimental` | Action | 否 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4：在 NewSituation 之前写入 OMSI 的启动时钟（静态分析）；运行时验证待完成。 |
| `world.weather` | `PublicExperimental` | Action | 否 |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4（BI-003）：在世界启动后立即通过 OMSI 自身的 loadWeather 或其 METAR 标志应用；它会覆盖已保存情景的天气。已通过静态分析确定；运行时验证待完成。 |
| `player-vehicle.model` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007；为诊断信息而解析，不会应用。 |
| `player-vehicle.repaint` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007。 |
| `player-vehicle.hof` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/hof:<id>` | BI-007。 |
| `player-vehicle.fleet-number` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007。 |
| `player-vehicle.registration` | `Unsupported` | Action | 否 |  |  | UNAVAILABLE | `/registration:<r>` | BI-007。 |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | headless 启动时 present=false；使用已保存情景时返回完整快照。 |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007：原生分配序列已通过静态分析确定（research/reports/beta4-static/entrypoint-last-player-vehicle.md 第 3 节），但在运行时验证之前不会实现；计划在后续的 Beta 中提供。 |
| `configuration.options.semantic` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005。 |
| `input.keyboard` | `PublicExperimental` | Write | 否 |  |  | STATICALLY_VALIDATED |  | Beta 4：事务覆盖层，经离线验证。 |
| `input.controller` | `PublicExperimental` | Write | 否 |  |  | STATICALLY_VALIDATED |  | Beta 4：事务覆盖层，经离线验证。 |
| `presentation.splash` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | 否 |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 B 轮：抑制已应用，并通过遥测环报告（internet-textures.suppressed）。 |
| `internet-textures.override` | `PublicExperimental` | Write | 否 |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | 运行时收尾验证 I01。 |
| `session-profiles` | `PublicStableBeta` | Action | 否 |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 配置档启动；1..99 和发现功能经离线验证。 |
| `hof.management` | `PublicStableBeta` | Write | 否 |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 A 轮 C01、C02。 |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` （分页） | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | 否 |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | 否 |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 已通过静态分析确定：SetActualDateTime 只是把电脑时钟复制到启动时钟中；启动日期使用 world.explicit-date。已被取代：原生 ABI 和后置条件并未确定。 |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4：写入 actWeather（Weather+0x08），即 OMSI 的天气刻据以推导的记录；已通过静态分析确定。派生值（湿度、光照系数、实际风）保持只读（OL_E_RUNTIME_SETTING_NOT_PERSISTENT）。运行时验证待完成。 |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` （分页） | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | 需要玩家车辆（已保存情景会提供一辆）；在 headless 的 NEW_MAP 上为 OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE。 |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5：经静态验证的设计（研究 beta4-static pause-menu-and-input.md）；这些原生调用尚未在真实会话中观察到。 |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` （分页） | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | 自然移除后的失效检测（RV-002）没有安全的运行时触发方式，经离线验证。 |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` （分页） | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` （分页）, `timetable.trips.list` （分页）, `timetable.lines.list` （分页）, `timetable.rv-files.list` （分页）, `timetable.track-entries.list` （分页）, `timetable.bus-stops.list` （分页）, `timetable.station-links.list` （分页）, `timetable.tours.list` （分页）, `timetable.profiles.list` （分页）, `timetable.tour-entries.list` （分页）, `timetable.logs.read` （分页）, `timetable.no-rv-numbers.list` （分页） | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` （分页）, `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` （分页）, `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | 写入：scripts.string.write。 |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4（BI-004）：OMSI 复制一个常量形态的字符串；已通过静态分析确定。运行时验证待完成。 |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4（BI-004）：通过已配置化的 VMT 槽调用虚方法 TriggerXML/SoundTrigger；已通过静态分析确定。运行时验证待完成。 |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` （分页）, `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` （分页）, `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` （分页） | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` （分页） | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | 已观察到设备重置（resetting、restored、代次失效）；未产生单独的 lost 状态转换（RV-007）。 |
<!-- generated:capability-catalog:end -->

`internal.make-basic` 为 `InternalOnly`：它存在于插件中供研究使用（会返回一个原生地址），在 API 边界处以及本地控制平面中都会被拒绝，二者都会先依据 `PublicRuntimeOperationIds` 校验操作名称；CLI 没有指向它的路由。`session.stop` 会终止 OMSI；协作式关闭未实现。

<a id="pagination"></a>
## 分页

每个列表操作（在[能力目录](#capability-catalog)中标有“（分页）”）接受两个可选参数，并返回四个页字段：

| 参数 | 含义 |
| --- | --- |
| `cursor` | 页的起点：上一页的 `next_cursor`。省略：第一页。来自另一个列表的游标或格式错误的游标为 `OL_E_RUNTIME_CURSOR_INVALID`。 |
| `limit` | 需要的行数（正整数）。大于该列表的最大页大小：使用最大值。不是正整数：`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`。 |

| 结果键 | 含义 |
| --- | --- |
| `count` | OMSI 持有的全部记录。 |
| `returned_count` | 本次回复中的行数。 |
| `truncated` | 本页之后还有记录时为 `true`。 |
| `next_cursor` | 仅在之后还有记录时存在：将其作为下一页的 `cursor` 传入。 |

游标是稳定的键，而不是位置。对于记录数组（时刻表、脚本名称、常量、曲线、HOF、司机、车票、日志），行以记录的绝对索引为键，形式为 `<row>.<n>.<field>`（或 `name.<n>`），游标指明下一个索引，因此第一页等同于 Beta 3 的回复。对于句柄集合（`road-vehicles.list`、`humans.list`），行按句柄顺序返回，游标指明最后返回的句柄：在两页之间出现的对象会得到更靠后的句柄，并出现在更靠后的页中；没有任何对象会重复出现。请将游标值视为不透明的。

最大页大小：`road-vehicles.list` 和 `humans.list` 为 1000；`timetable.track-entries.list`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list` 和 `vehicle.curves.list` 为 512；`timetable.tour-entries.list`、`timetable.logs.read` 和 `vehicle.hofs.read` 为 256；其他时刻表列表、`drivers.read` 和 `tickets.read` 为 128。当一页的行仍然超过 64 KiB 邮箱时，插件会丢弃最后的行，直到回复能够放入，`next_cursor` 从第一个被丢弃的行继续，因此没有任何列表会因其大小而失败。

<a id="public-runtime-operations"></a>
## 公开运行时操作

以下是公开前端可以转发的操作 ID 的精确列表（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。每个操作都要求 `SessionState.Running`；均不记入事务日志，也不会被还原。必需参数由注册表在使用邮箱之前强制检查（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）；可选参数由插件校验。所有操作通用的失败码：`OL_E_RUNTIME_OPERATION_UNKNOWN`（不在此列表中）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（插件没有实现）、`OL_E_RUNTIME_OPERATION_FAILED`（进程内意外异常；带有 `detail` 和 `exception` 值）。

<a id="time"></a>
### 时间

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `time.read` | Read | 无 | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 可选 `hour`（0..23）、`minute`（0..59）、`second`（0..59.999，小数）；至少一个 | 经过配置化的 `SetTime` 之后的 `time.read` 键 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天气

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | 无 | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`，以及源值 `set.fog_density`、`set.lightness`、`cloud_type_name`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads`。`fog_density`、`lightness`、`wind_speed`、`wind_direction`、光照系数和湿度是 OMSI 每个天气刻的值；`cloud_type` 是解析后的云类型编号 | |
| `weather.set` | Write | 以下至少其中之一：`fog_density`、`lightness`、`wind_speed`、`wind_direction`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`、`cloud_type`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads`。范围：雾 0..100000，lightness 和地面值 0..1，风速 0..200，风向 0..360，温度和露点 -60..60，气压 850..1100，降水 0..2，rate 0..255，云高 0..20000，`cloud_type` 为代码页 1252 的 1..64 个字符（`-1` = 无云），布尔值 `true`/`false`/`1`/`0` | `weather.read` 的键；带有 `cloud_type` 时还有 `cloud_type_resolved`（当 OMSI 不认识该云类型时为 `false`：它不绘制云） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（派生键：`relative_humidity`、`absolute_humidity`、光照系数、`cloud_transparency`、`precipitation_set`、`wet_ground`；detail 会指明应设置的键）、`OL_E_RUNTIME_SETTING_UNAVAILABLE`（未知键）、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao`（4 个字母或数字） | `weather.actual.read` 的键 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | 无 | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### 地图

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `map.read` | Read | 无 | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### 摄像机

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | 无 | `family`（0 司机、1 乘客、2 外部、3 地图）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 可选 `family`（0..3）、`field_of_view`（10..170）；至少一个 | `camera.read` 的键 | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必需 `family`（0..3）；可选 `preset`（0..255，仅用于 family 0 或 1） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。该策略每 100 ms 重新应用一次，直至 `camera.unlock`；重新应用失败时，每种不同的错误发出一次 `camera.lock.degraded` 事件。 |
| `camera.unlock` | Action | 无 | `locked`（`false`） | |

<a id="user-interface-pause-menu"></a>
### 用户界面（暂停菜单）

接管 OMSI 的原生暂停菜单，以便模块使用自己的菜单（能力 `ui.pause-menu.replace`，仅在已知代码标识上解析）。OMSI 自身的暂停字节保持置位，因此模拟、渲染、3D 点击和 DirectInput 命令都保持暂停；原生窗体通过 VCL 隐藏，其操作被直接调用（没有点击、没有投递消息、没有移动窗口）。`ui.pause.action` 在发送应答之后，从一次性的 UI 线程定时器中运行原生处理程序，因为该处理程序可能进入原生模态对话框：请通过 `ui.pause.state` 读取结果（`busy` 为 `false`，然后是 `last_result`）。

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | 无 | `paused`、`native_menu_visible`、`main_form_ready`、`in_frame`、`taken_over`、`busy`、`pending_action`、`last_action`、`last_result`（`none`、`resumed`、`game-started`、`returned`、`quit-requested`、`exception`、`refused`） | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | 无 | 接管之后的 `ui.pause.state` 的键 | `OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_UI_ALREADY_TAKEN_OVER`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | 无 | `ui.pause.state` 的键；再次显示原生菜单（其自身的 `FormShow` 会运行：OMSI 保持暂停） | `OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | 必需 `action`：`resume`、`new-game`、`save`、`options` 或 `quit` | `ui.pause.state` 的键，加上 `scheduled`（`true`）和 `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |

<a id="road-vehicles-and-player"></a>
### 道路车辆与玩家

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | 无 | `count`、`ai_collection`、`player_index`（OMSI 原始的玩家车辆索引，按读取值报告；要判断是否存在玩家车辆，请使用 `player-vehicle.read` 的 `present`） | |
| `road-vehicles.list` | Read | 可选 `cursor`、`limit` | 页字段；`vehicle.<n>.handle`（`rv-NNNNNN`；`<n>` 是 OMSI 列表中的位置） | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | 必需 `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | 无 | `present`（OMSI 没有玩家车辆时为 `false`）；为 `true` 时：`player_index` 加上 `road-vehicle.read` 的全部键 | |
| `road-vehicles.spawn` | Action | 必需 `model`（规范形式 `Vehicles\...\*.bus`，最多 240 个字符，不含 `..`，必须存在于安装目录之下） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。不会分配 PlayerVehicle。耗时数秒；请使用 30 s 的客户端超时。 |
| `road-vehicles.place-random` | Action | 可选 `ai_type`（0..255，默认 0）、`group`（0..65535，默认 1）、`type`（-1..65535，默认 -1）、`scheduled`（0..1，默认 0）、`tour`（0..65535，默认 0）、`line`（0..65535，默认 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物对象

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | 无 | `count` | |
| `humans.list` | Read | 可选 `cursor`、`limit` | 页字段；`human.<n>.handle`（`hb-NNNNNN`；`<n>` 是 OMSI 列表中的位置） | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | 必需 `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 时刻表、司机、车票

时刻表列表、`drivers.read` 和 `tickets.read` 是分页的（参见[分页](#pagination)）；下文的“页字段”指 `count`、`returned_count`、`truncated` 和 `next_cursor`，每个列表都接受可选的 `cursor` 和 `limit`。

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | 无 | `invalid`（`true`/`false`），以及记录数 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | 可选 `cursor`、`limit` | 页字段；`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | 可选 `cursor`、`limit` | 页字段；`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | 可选 `cursor`、`limit` | 页字段；`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | 可选 `cursor`、`limit` | 页字段；`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | 可选 `cursor`、`limit` | 页字段；`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | 可选 `cursor`、`limit` | 页字段；`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | 可选 `cursor`、`limit` | 页字段；`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | 可选 `cursor`、`limit` | 页字段；`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | 可选 `cursor`、`limit` | 页字段；`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | 可选 `cursor`、`limit` | 页字段；`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | 可选 `cursor`、`limit` | 页字段；`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | 可选 `cursor`、`limit` | 页字段；`no_rv.<n>.group`、`no_rv.<n>.number`：OMSI 不让其自由用于 AI 的车辆编号（OMSI 的保留编号字符串列表，通过其自身的布局读取；当该列表不具有预期形状时为 `OL_E_RUNTIME_VALUE_INVALID`）。每页最多 256 个。静态分析；运行时验证待完成。 |
| `map.tiles.list` | Read | 可选 `cursor`、`limit` | 页字段；`tile.<n>.x`、`tile.<n>.y`（网格位置）、`tile.<n>.tile_index`（该图块在地图图块列表中的自身索引）、`tile.<n>.filename`、`tile.<n>.version`、`tile.<n>.unsaved`、`tile.<n>.loaded`、`tile.<n>.load_requested`、`tile.<n>.failed`、`tile.<n>.thread_loading`、`tile.<n>.prepared_for_ode`、`tile.<n>.width_south`、`tile.<n>.width_north`、`tile.<n>.objects`、`tile.<n>.path_groups`、`tile.<n>.path_segments`、`tile.<n>.surface_objects`、`tile.<n>.spline_segments`；对无法读取的图块，`tile.<n>.state` = `unavailable` 并带有 `tile.<n>.error`。每页最多 256 个。位置和索引来自静态分析；运行时验证待完成。 |
| `drivers.read` | Read | 可选 `cursor`、`limit` | `count`、`selected_index`；`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | 可选 `cursor`、`limit` | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`；`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 车辆脚本、常量、曲线、HOF（按句柄）

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必需 `handle`；可选 `cursor`、`limit` | `handle`、页字段、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必需 `handle`、`name`、`value`（有限浮点数） | `handle`、`name`、`requested_value`、`value`（回读值） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必需 `handle`；可选 `cursor`、`limit` | `handle`、页字段、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | 必需 `handle`、`name`（区分大小写，与脚本声明的完全一致，最多 128 个字符）、`value`（1..1024 个字符） | `handle`、`name`、写入后回读的 `value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（OMSI 自身会静默忽略未知名称）、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | 必需 `handle`、`name`（代码页 1252，最多 128 个字符）、`value`（`1` 运行 `name`，`0` 运行 `name_off`，`2` 运行 `name_drag`） | `handle`、`name`、`value`、`dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`。脚本入口同步运行；脚本未定义的名称不会产生任何效果。按下之后通常应跟随一次释放 |
| `vehicle.sound-trigger` | Action | 必需 `handle`、`trigger`（代码页 1252，最多 128 个字符）；可选 `file`（最多 260 个字符） | `handle`、`trigger`、`file`、`dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`（也包括：该车辆没有声音包）、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | 必需 `handle`；可选 `cursor`、`limit` | `handle`、页字段、`name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必需 `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必需 `handle`；可选 `cursor`、`limit` | `handle`、页字段、`name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必需 `handle`、`name`、`x`（有限浮点数；缺少 `x` 或 `x` 非数值时为 `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（线性插值） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必需 `handle`；可选 `cursor`、`limit` | `handle`、页字段、`hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作通过原生桥在 OMSI 的主线程/渲染线程上执行。纹理句柄的格式为 `d3dtex-<sessionId N>-<16 hex digits>`。

| 操作 | 类型 | 参数 | 结果键 | 错误 |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | 无 | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必需 `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）；可选 `levels`（0..16，默认 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必需 `handle`；可选 `level`（0..15，默认 0） | `d3d.texture.create` 的 13 个键，对应所请求的 level | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`，以及 create 的错误 |
| `d3d.texture.update` | Action | 必需 `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（解码后最多 48 KiB）；可选 `level`（0..15，默认 0）、`x`（0..4095，默认 0）、`y`（0..4095，默认 0） | 更新之后的 `d3d.texture.create` 的 13 个键 | `OL_E_D3D_INVALID_PIXEL_BUFFER`，以及 describe 的错误 |
| `d3d.texture.release` | Action | 必需 `handle` | `d3d.texture.create` 的 13 个键，其中 `state` = `RELEASED` | 重复释放时为 `OL_E_D3D_RESOURCE_RELEASED`，`OL_E_D3D_STALE_RESOURCE_HANDLE` |

失败的 D3D 操作会返回 `detail` 和 `native_status` 值。`D3DRuntimeApi` 扩展方法（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）为 API 使用方封装了这些操作。

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` 为每个不是 `InternalOnly` 的目录条目返回一个 `Capability(Name, Available, EvidenceState, Reason)`。`Name` 是目录 id，`EvidenceState` 是其目录证据。`Available` 针对该安装实例评估：

- `Unsupported` 条目永远不可用；`Reason` 携带目录中的说明。
- 每个条目都需要当前平台，除 `runtime.current-windows-x64` 之外的每个条目都需要 OMSI 2 安装。
- 在已知布局上具有原生能力的条目会报告 `native: RESOLVED_KNOWN_LAYOUT`。在另一个 OMSI 2 版本上，会对可执行文件进行静态解析：`native: STATICALLY_RESOLVED`，或指明文件未能解析哪些内容的 `native: PENDING_IN_PROCESS_RESOLUTION`。处于待定状态的条目仍会被报告为可用，因为由进程内解析决定；若其失败，该操作或启动会以 `OL_E_CAPABILITY_UNRESOLVED` 失败。

规划器在 `SessionPlan.RequiredCapabilities` 和 `SessionPlan.UnsupportedRequestedFeatures` 中以相同的目录 id 报告每个计划的能力（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.omsi2`、`world.*`、`boot.headless-start`、`internet-textures.disabled`、`player-vehicle.*`、`input.*`、`content.*`）。被请求但不受支持的功能带有其目录证据；所需的原生能力带有 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`。

<a id="known-limitations"></a>
## 已知限制

- 句柄的作用域为会话，其对象消失后会报告为 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`。车辆句柄绑定到其 VMT、其定义以及 OMSI 的 `IDCode`（每个地图对象在构造时获得、进程内唯一且从不复用的编号），因此在同一地址重新创建的对象总能被区分开来。人物对象是在整个地图会话期间存在的池化槽位：当人物对象消失（`RenderMe` 为 0）或该槽位被重新生成（新的目标车站或出发时间）时，其句柄不再能被解析。
- 结果受 64 KiB 邮箱限制；每个列表都是分页的（参见[分页](#pagination)）。
- 不支持跨图块的车辆重新定位，不支持日历写入，不支持 headless 模式下的 PlayerVehicle 分配。字符串变量写入、脚本触发器和天气写入为 `EXPERIMENTAL`（运行时验证待完成）。见[已知限制](known-limitations.md)。
