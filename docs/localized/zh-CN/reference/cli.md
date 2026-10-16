# CLI 参考

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/cli.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 OmsiLaunch `0.1.0-beta4` 命令行完整的规范性参考：三个可执行文件、参数语法、分派顺序、每个命令词、每条层级路由、每个参数、输出信封（envelope）以及每个命令的错误行为。本页依据 `tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Parse`、`CliInput.KnownFlags`、`CliInput.AcceptedNoEffectFlags`、`CliInput.CommandWordsAccepted`、`CliInput.HierarchicalRoutes`、`CliInput.BuildSpecAsync`、`CliEventWatch`）、`tools\OmsiLaunch.Cli\LaunchSpecJson.cs` 以及 `tools\OmsiLaunch.Bootstrapper` 下的两个原生 shim 生成。进程结果列于[退出码](exit-codes.md)；错误码见[错误](errors.md)；完整调用示例见 [CLI 示例](cli-examples.md)。

<a id="executables"></a>
## 可执行文件

| 文件 | 子系统 | 作用 | 差异 |
|---|---|---|---|
| `OmsiLaunch.exe` | 控制台 | 原生引导程序（`OmsiLaunch.Bootstrapper.cpp`）：解析自身所在目录，用 `CommandLineToArgvW` 拆分命令行，通过 `nethost.dll` 定位 `hostfxr`，并以相同参数运行 `OmsiLaunch.Controller.dll`。 | 写出控制台输出；进程退出码为托管控制器的退出码，若无法启动 .NET 主机，则为 shim 退出码 `100`..`106`。 |
| `OmsiLaunchW.exe` | Windows（GUI） | 同一个 shim（`OmsiLaunch.WindowsHost.cpp`），针对 Windows 子系统构建。它在启动控制器之前设置环境变量 `OMSILAUNCH_WINDOWS_HOST=1`。 | 没有控制台：除非指定 `--json`，否则控制台输出被抑制（`WindowsHost.SuppressConsole`）；失败以消息框显示（`WindowsHost.ShowFailure`：消息、`Code: OL_E_...` 以及提示 `See .omsilaunch\diagnostics for details.`）；shim 失败 `100`..`106` 显示为 `OmsiLaunch could not start the .NET host (code N).`。完整行为见 [OmsiLaunchW.exe 参考](omsilaunchw.md)。 |
| `OmsiLaunch.Controller.dll` | 托管（x64、`net6.0-windows`、Windows Forms） | 控制器本身。用户从不直接调用它；两个 shim 都把控制器路径作为第一个主机参数传入，因此它不会出现在公开的参数列表中。 | 需要带有 `Microsoft.WindowsDesktop.App` 的 x64 .NET 6 运行时；见[安装](../getting-started/installation.md)。 |

`nethost.dll` 必须与 shim 位于同一目录。shim 本身不读取任何参数；每个参数都原样到达 `CliInput.Parse`，因此 `OmsiLaunch.exe` 和 `OmsiLaunchW.exe` 接受完全相同的语法。

<a id="invocation-model"></a>
## 调用模型

<a id="argument-grammar-cliinputparse"></a>
### 参数语法（`CliInput.Parse`）

| 形式 | 含义 |
|---|---|
| `/key:value`、`/key`、`-key:value`、`-key` | 参数。键不区分大小写；值为第一个 `:` 之后的全部内容。未知键以 `OL_E_INVALID_ARGUMENT`（`Unknown argument: ...`）失败，退出码 `2`。 |
| `--key=value` | 所选运行时操作的运行时参数（例如 `--handle=rv-000001`）。任何包含 `=` 的 `--` 标记都是运行时参数，而不是命令行参数。 |
| `--json`、`/json` | 结构化输出（见[输出格式](#output-formats)）。`--json` 是唯一有意义的不含 `=` 的 `--` 标记；它被解析为 `/json` 参数。 |
| 裸词 | 如果尚未出现命令词，且该词属于[命令词](#command-words)之一，则它成为命令。一旦存在命令词，之后的每个裸词都是命令词（路由）。否则，第一个裸词是安装根目录，之后的裸词追加到路由中。 |

由此可知：层级路由（`time get`）不能与放在其后的安装参数组合（`time get D:\OMSI` 是未知路由 `time get d:\omsi`，退出码 `2`）。`D:\OMSI time get` 可以被接受，但属于**所有者模式**（启动一个新会话，并在其中执行一次该操作）。解析错误（`ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException`）和会话配置档错误（`SessionProfileException`）会在任何操作执行之前报告，退出码始终为 `2`。

<a id="installation-root"></a>
### 安装根目录

- 显式的裸词安装参数优先于 `/spec` 文件中的 `RootPath`（`CliInput.BuildSpecAsync`）。
- `.` 表示包含可执行文件的目录（`AppContext.BaseDirectory`），而不是调用方的工作目录（`CliInput.ResolveInstallationRoot`）。便携包依赖这一点。
- 省略该参数时，所有者模式的操作（`/new`、`/saved`、`/spec`、`/list`、`/recovery-status`、`/recover`）同样使用可执行文件所在目录。路径通过 `Path.GetFullPath` 规范化。
- 客户端模式的命令从不接受安装参数：它们访问可执行文件所在安装实例（`AppContext.BaseDirectory`）的本地控制端点。见[本地控制](local-control.md)。

<a id="owner-and-client"></a>
### 所有者与客户端

- **所有者**：负责规划、启动、监管和还原会话的进程（`OwnerSession.RunAsync`）。它持有安装租约（`Local\OmsiLaunch.Installation.<sha256(root)>`）和配置事务，在会话存活期间公开本地控制端点，并显示[托盘图标](windows-tray.md)。每个安装实例恰好一个所有者：如果控制端点上已有所有者响应 `session.status`，则第二次启动以 `OL_E_SESSION_ALREADY_ACTIVE`（退出码 `7`）失败。
- **客户端**：任何不带安装参数、发送 `session status`、`session stop`、`events read`、`events watch` 或运行时操作的调用。它通过本地控制管道转发；没有所有者时以 `OL_E_NO_ACTIVE_SESSION`（退出码 `4`）失败。

<a id="dispatch-order-cliprogramrunasync"></a>
### 分派顺序（`CliProgram.RunAsync`）

1. `/silent`（当前未在 `OmsiLaunchW.exe` 下运行时）：通过 `ShellExecute`（不继承句柄）从可执行文件所在目录启动 `OmsiLaunchW.exe`，传入去掉 `/silent`/`--silent` 后的相同参数，写出 `silent` 信封（`delegated`、`host_process_id`）并返回 `0`。控制台进程不等待会话；见 [OmsiLaunchW.exe](omsilaunchw.md#silent-delegation)。`OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` 返回 `7`。
2. `/version`：信封 `version`，包含 `product`、`version`（程序集信息版本，取自 `OmsiLaunch.Version.props`，`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family`（`OMSI_2_3_004_COMMON`）；退出码 `0`。
3. `capabilities`：信封中包含规范目录 `PublicCapabilityRegistry` 的每个 `PublicStableBeta` 或 `PublicExperimental` 描述符（含其操作、原生能力、证据和说明）；退出码 `0`。
4. `help [family]`：信封 `help`，包含 `usage`、`product_version`、`protocol_version`、`family` 以及公开的 `commands`（`CliRoute`、`Description`、`Classification`、`RuntimeValidation`），可按能力族过滤；退出码 `0`。
5. `profiles`：信封中包含 `requirement`（`OMSI 2`）、目标根目录的 `installation` 报告（`is_omsi2`、`reason`、`build_id`、`identity` 取 `known`/`unknown`、`file_sha256`、`file_version`、`code_sha256`、`file_variant`）、`known_layouts`，以及从可执行文件解析出的 `predicted_capabilities`（启动时的进程内解析具有权威性）；退出码 `0`。
6. 客户端运行时操作（无安装参数，且带有路由或 `/runtime:`）：参数先由 `PublicCapabilityRegistry.ValidateRuntimeArguments` 校验（`OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`，退出码 `2`），然后以 8 s 超时转发 `runtime.execute`（`road-vehicles.spawn` 为 30 s）。
7. 客户端 `session status`（750 ms）、`session stop`（绑定到活动会话 ID，750 ms）、`events read`（750 ms）、`events watch`（每 250 ms 轮询一次，直到 Ctrl+C）。
8. `detect`，或者**完全没有参数**（无安装、无命令、无 `/?`、无 `/spec`、无启动参数、无恢复参数、无 `/list`）：枚举 `Omsi` 进程并探测控制端点（250 ms）；信封 `detect`；退出码 `0`。
9. `/?` 或 `/help`：打印用法文本，退出码 `0`。任何带有命令词但没有可分派路由的其他调用（例如单独的 `d3d` 或 `session status D:\OMSI`）会打印用法文本并以 `2` 退出。
10. 所有者模式。前提条件：`plugins\OmsiLaunch.Plugin.opl` 和 `plugins\OmsiLaunch.Native.x86.dll` 必须存在于可执行文件旁边（`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`，退出码 `7`）。可执行文件旁边的 `release-manifest.json`（如果存在）提供预期的插件哈希。
11. `/recovery-status` / `/recover`：`RecoverPendingAsync`；信封 `recover`，包含 `pending`、`recovered`、`diagnostics`；仅当请求了还原但还原未完成时退出码为 `8`，否则为 `0`。
12. `/list:<category>`：`DiscoverAsync`；信封 `content.list`；退出码 `0`。`/list:SessionProfiles` 改用 `ListSessionProfilesAsync`；信封 `session-profiles.list`。
13. 构建 `LaunchSpec`（`BuildSpecAsync`），对其进行规划（`PlanSessionAsync`），打印计划。`/plan` 或 `/validate`：`IsRunnable` 时退出码 `0`，否则为 `1`。不可运行的计划从不启动 OMSI（退出码 `1`）；在 `OmsiLaunchW.exe` 下，使用不可运行计划的启动会在消息框中显示其最后一个 `OL_E_` 诊断信息（文档审计 BUG-06）。规划时还会依据 `release-manifest.json` 校验已安装的永久插件闭包，因此插件缺失或被改动会使计划不可运行（`OL_E_PERMANENT_PLUGIN_*`）。
14. 探测是否已有所有者（`OL_E_SESSION_ALREADY_ACTIVE`，退出码 `7`），然后执行 `OwnerSession.RunAsync`。

<a id="owner-lifecycle-ownersessionrunasync"></a>
### 所有者生命周期（`OwnerSession.RunAsync`）

1. `StartSessionAsync(plan)`。从此处开始，每条退出路径都会在 `finally` 块中到达 `CloseAsync`：异常、Ctrl+C（`Console.CancelKeyPress`）、关闭控制台 / 注销（`AppDomain.ProcessExit`，停止 + 还原的时间预算为 4 s；剩余部分在下次启动时由事务日志恢复）、托盘“End session”、管道 `session.stop` 以及 `/observe-seconds`。
2. 除非 spec 中设置了 `Presentation.SuppressTrayIcon`，否则创建托盘图标。
3. 等待 `Running`，时长为 `StartupTimeoutSeconds + 5` 秒。打印状态。如果状态不是 `Running`，退出码为 `1`（`OmsiLaunchW.exe` 显示 `The OMSI session did not reach gameplay.`，并附上最后一个 `OL_E_` 诊断信息或 `OL_E_SESSION_START_FAILED`）。
4. 运行验证批处理（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`）并写出其产物。
5. 启动本地控制端点。
6. `/runtime:<operation>` 执行一次（5 s，`road-vehicles.spawn` 为 15 s）；结果写入 `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` 并打印。失败的运行时命令从不终止会话（改为打印 `runtime_error`）。
7. 等待：使用 `/observe-seconds:n` 时，会话在 `n` 秒后停止，**或者**在托盘/管道停止时、或 OMSI 退出时提前停止；不使用该参数时，所有者一直等待，直到 OMSI 退出或收到停止请求。
8. 打印完成状态；若为 `Completed` 则退出码 `0`，否则为 `1`。

`session.stop`、托盘“End session”、Ctrl+C 和 `CloseAsync` 都会请求规范停止：用 `TerminateProcess` 终止 OMSI（OMSI 自身的关闭例程不会运行，OMSI 也不会重写 `options.cfg`），然后还原每个会话拥有的文件。见[会话生命周期](../concepts/session-lifecycle.md)和[事务与恢复](../concepts/transactions-and-recovery.md)。

<a id="command-words"></a>
## 命令词

第一个位置上可接受的所有词（`CliInput.CommandWordsAccepted`）：

| 词 | 用途 | 模式 | 说明 |
|---|---|---|---|
| `capabilities` | 列出公开能力 | 本地，无会话 | 信封 `capabilities`。 |
| `profiles` | 列出支持的 `Omsi.exe` 变体 | 本地，无会话 | 信封 `profiles`。 |
| `detect` | 报告 `Omsi.exe` 进程和活动的所有者 | 本地，无会话 | 也是不带任何参数时的默认行为。状态：`NO_OMSI_FOUND`、`OMSI_FOUND_UNMANAGED`，无法检查二进制文件时按进程报告 `UNKNOWN_BINARY_FOUND`；`active_omsilaunch_instance`、`managed_session`。 |
| `help` | 用法与公开命令目录 | 本地，无会话 | `help <family>` 按能力族过滤（`session`、`time`、`weather`、`map`、`camera`、`vehicles`、`player`、`humans`、`timetable`、`scripts`、`constants`、`curves`、`hof`、`drivers`、`tickets`、`d3d`、`events`）。 |
| `session` | `session status`、`session stop` | 客户端 | 后面恰好跟一个词；其他任何情况打印用法，退出码 `2`。`session plan`/`session start` 是 API 路由名，不是 CLI 词：请使用 `/plan` 和 `/new`。 |
| `events` | `events read`、`events watch` | 客户端 | `read` 一次性返回有界事件列表；`watch` 每 250 ms 将每个新事件（按 `Sequence`）作为 `events.watch` 信封打印，直到 Ctrl+C（退出码 `0`）；没有所有者响应时为 `4`，控制错误时为 `7`。 |
| `time` | `time get`、`time set` | 客户端路由 | |
| `weather` | `weather get`、`weather set`、`weather metar set`、`weather actual get` | 客户端路由 | |
| `map` | `map get` | 客户端路由 | |
| `camera` | `camera get`、`camera set`、`camera lock`、`camera unlock` | 客户端路由 | |
| `vehicles` | `vehicles list`、`vehicles get`、`vehicles summary`、`vehicles spawn`、`vehicles place-random` | 客户端路由 | |
| `player` | `player get` | 客户端路由 | |
| `humans` | `humans list`、`humans get`、`humans summary` | 客户端路由 | |
| `timetable` | `timetable get`、`timetable <table> list`、`timetable logs list` | 客户端路由 | |
| `scripts` | `scripts variable list|get|set`、`scripts string list|get|set`、`scripts trigger`、`scripts sound-trigger` | 客户端路由 | |
| `constants` | `constants list`、`constants get` | 客户端路由 | |
| `curves` | `curves list`、`curves evaluate` | 客户端路由 | |
| `hof` | `hof get` | 客户端路由 | |
| `drivers` | `drivers list` | 客户端路由 | |
| `tickets` | `tickets get` | 客户端路由 | |
| `d3d` | 保留的能力族词 | 无 | `d3d` **没有层级路由**：`d3d texture ...` 是未知路由（退出码 `2`），单独的 `d3d` 打印用法（退出码 `2`）。D3D 操作通过 `/runtime:d3d.status`、`/runtime:d3d.texture.create` 等方式调用（见[没有路由的操作](#operations-without-a-route)）。 |

<a id="hierarchical-routes"></a>
## 层级路由

`CliInput.HierarchicalRoutes` 将小写化的路由映射到运行时操作 ID。所有路由都需要处于 `Running` 状态的会话，并通过运行时邮箱（`ExecuteRuntimeAsync`）执行。运行时写入只改变 OMSI 的内存状态：它们从不触及文件，不属于配置事务，并且在停止时**不会**被撤销（OMSI 会被终止）。稳定性以 `PublicCapabilityRegistry` 和[验证矩阵](../status/runtime-validation-status.md)为准；详细信息和结果字段见[运行时控制](runtime-control.md)。

| 路由 | 运行时操作 | 类型 | 需要 Running | 修改 OMSI | 参与还原 | 稳定性 | 说明 |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | 是 | 否 | 无 | STABLE_BETA | 时钟和日历字段。 |
| `time set` | `time.set` | Write | 是 | 是（内存中的时钟） | 无，不撤销 | EXPERIMENTAL | 例如 `--minute=<0..59>`；写入、回读和还原已于 2026-09-20 验证。 |
| `weather get` | `weather.read` | Read | 是 | 否 | 无 | STABLE_BETA | |
| `weather set` | `weather.set` | Write | 是 | 是（内存中的天气） | 无，不撤销 | EXPERIMENTAL | 例如 `--wind_speed=5 --cloud_type="Cumulus 1"`。手动天气：实时 METAR 更新停止。运行时验证待完成。 |
| `weather metar set` | `weather.metar.set` | Write | 是 | 是（内存中的天气；OMSI 会把 ICAO 保存在 `options.cfg` 中，会话会将其还原） | 无，不撤销 | EXPERIMENTAL | `--icao=<ICAO>`。实时 METAR 天气，与 OMSI 的天气对话框所设置的一致。运行时验证待完成。 |
| `weather actual get` | `weather.actual.read` | Read | 是 | 否 | 无 | EXPERIMENTAL | 实际/ICAO 控制器状态。 |
| `map get` | `map.read` | Read | 是 | 否 | 无 | STABLE_BETA | 地图名称、文件、描述、地块数量、年份范围和通行方向；已在修正后的地图槽上重新经运行时验证。 |
| `camera get` | `camera.read` | Read | 是 | 否 | 无 | STABLE_BETA | |
| `camera set` | `camera.set` | Write | 是 | 是（摄像机标量，例如 `--field_of_view=`） | 无，不撤销 | EXPERIMENTAL | FOV 写入/回读已验证。 |
| `camera lock` | `camera.lock` | Action | 是 | 是（会话范围的策略） | 无 | EXPERIMENTAL | 需要 `--family=<0..3>`（driver=0、passenger=1、external=2、map=3），可选 `--preset=<n>`（族 0 或 1）。需要玩家车辆（例如已保存情景）。已在运行时闭包中经运行时验证（`CAM01`）；注册表的 `RuntimeValidation` 字符串仍为 `STATICALLY_VALIDATED`（见[能力](capabilities.md)）。 |
| `camera unlock` | `camera.unlock` | Action | 是 | 是 | 无 | EXPERIMENTAL | 解除 `camera lock` 设置的策略（`CAM01`）。 |
| `vehicles list` | `road-vehicles.list` | Read | 是 | 否 | 无 | STABLE_BETA | 返回会话范围的 `rv-NNNNNN` 句柄。 |
| `vehicles get` | `road-vehicle.read` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`。失效句柄：`OL_E_RUNTIME_OBJECT_HANDLE_STALE`。 |
| `vehicles summary` | `road-vehicles.read` | Read | 是 | 否 | 无 | STABLE_BETA | 计数和玩家状态，不含句柄。 |
| `vehicles spawn` | `road-vehicles.spawn` | Action | 是 | 是（添加一辆 RoadVehicle） | 无，不移除 | EXPERIMENTAL | 需要 `--model=Vehicles\...\*.bus`。客户端超时 30 s，所有者超时 15 s。不会指派玩家车辆。RV-003 `RUNTIME_VALIDATED`。 |
| `vehicles place-random` | `road-vehicles.place-random` | Action | 是 | 是 | 无 | EXPERIMENTAL | 经构建配置分析的 `PlaceRandomBus`。 |
| `player get` | `player-vehicle.read` | Read | 是 | 否 | 无 | STABLE_BETA | 没有玩家车辆时返回语义上的 null。 |
| `humans list` | `humans.list` | Read | 是 | 否 | 无 | EXPERIMENTAL | 返回 `hb-NNNNNN` 句柄。 |
| `humans get` | `human.read` | Read | 是 | 否 | 无 | EXPERIMENTAL | 需要 `--handle=`。 |
| `humans summary` | `humans.read` | Read | 是 | 否 | 无 | EXPERIMENTAL | 仅计数。 |
| `timetable get` | `timetable.read` | Read | 是 | 否 | 无 | STABLE_BETA | 时刻表管理器状态。 |
| `timetable tracks list` | `timetable.tracks.list` | Read | 是 | 否 | 无 | STABLE_BETA | 属于 `timetable.read` 能力；批量读取证据 2026-09-20。 |
| `timetable trips list` | `timetable.trips.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable lines list` | `timetable.lines.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable tours list` | `timetable.tours.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable profiles list` | `timetable.profiles.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable station-links list` | `timetable.station-links.list` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable logs list` | `timetable.logs.read` | Read | 是 | 否 | 无 | STABLE_BETA | 同上。 |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | 是 | 否 | 无 | EXPERIMENTAL | OMSI 不让其自由用于 AI 的车辆编号（被时刻表班次预留，或属于固定 AI 列表）。分页（`cursor`、`limit`，每页最多 256 个）。 |
| `map tiles list` | `map.tiles.list` | Read | 是 | 否 | 无 | EXPERIMENTAL | 地图的图块：网格位置、自身索引、文件和加载状态，以及对象、路径和样条的数量。分页（`cursor`、`limit`，每页最多 256 个）。 |
| `drivers list` | `drivers.read` | Read | 是 | 否 | 无 | EXPERIMENTAL | 司机记录。 |
| `tickets get` | `tickets.read` | Read | 是 | 否 | 无 | EXPERIMENTAL | 车票包记录。 |
| `hof get` | `vehicle.hofs.read` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`。 |
| `constants list` | `vehicle.constants.list` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`。 |
| `constants get` | `vehicle.constant.get` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`、`--name=`。 |
| `curves list` | `vehicle.curves.list` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`。 |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | 是 | 否 | 无 | STABLE_BETA | 需要 `--handle=`、`--name=`、`--x=`。 |
| `scripts variable list` | `vehicle.variables.list` | Read | 是 | 否 | 无 | EXPERIMENTAL | 需要 `--handle=`。 |
| `scripts variable get` | `vehicle.variable.get` | Read | 是 | 否 | 无 | EXPERIMENTAL | 需要 `--handle=`、`--name=`。 |
| `scripts variable set` | `vehicle.variable.set` | Write | 是 | 是（脚本变量） | 无，不撤销 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`（有限数值）。 |
| `scripts string list` | `vehicle.string-variables.list` | Read | 是 | 否 | 无 | EXPERIMENTAL | 需要 `--handle=`。 |
| `scripts string get` | `vehicle.string-variable.get` | Read | 是 | 否 | 无 | EXPERIMENTAL | 需要 `--handle=`、`--name=`。 |
| `scripts string set` | `vehicle.string-variable.set` | Write | 是 | 是（内存中的脚本状态） | 无，不撤销 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`。OMSI 自身的 `SetStringVariable`。运行时验证待完成。 |
| `scripts trigger` | `vehicle.trigger` | Action | 是 | 是（运行车辆脚本代码） | 无，不撤销 | EXPERIMENTAL | 需要 `--handle=`、`--name=`、`--value=`（`1` 按下，`0` 释放，`2` 拖动）。运行时验证待完成。 |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | 是 | 是（播放车辆声音） | 无，不撤销 | EXPERIMENTAL | 需要 `--handle=`、`--trigger=`；可选 `--file=`。运行时验证待完成。 |

<a id="operations-without-a-route"></a>
### 没有路由的操作

以下公开操作 ID（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）没有层级路由，需要用 `/runtime:<operation>` 加上 `--key=value` 或 `/runtime-arg:key=value` 调用：`timetable.rv-files.list`、`timetable.track-entries.list`、`timetable.tour-entries.list`、`d3d.status`、`d3d.texture.create`（必需 `width`、`height`、`format`；可选 `levels`）、`d3d.texture.describe`（`handle`；可选 `level`）、`d3d.texture.update`（必需 `handle`、`width`、`height`、`pixels_base64`；可选 `level`、`x`、`y`）、`d3d.texture.release`（`handle`）。D3D 操作为 EXPERIMENTAL；纹理生命周期和设备重置导致的失效已经运行时验证（运行时闭包 `H02`、`D01`；见[能力](capabilities.md)）。`timetable.track-entries.list` 和 `timetable.tour-entries.list` 是有界列表：放不进运行时槽的结果会被缩短（`truncated=true`）。`internal.road-vehicles.make-basic` 为 INTERNAL，CLI 和 API 都会以 `OL_E_RUNTIME_OPERATION_UNKNOWN` 拒绝它。

<a id="flags"></a>
## 参数

`CliInput.KnownFlags` 中的每个参数。“阶段”分为*启动时*（决定新会话的 `LaunchSpec`/计划）、*运行时*（作用于正在运行的会话）或*控制*（改变 CLI 自身的行为）。仅为兼容性而解析的参数（`CliInput.AcceptedNoEffectFlags`）会在其所在行标明。

<a id="control-and-output"></a>
### 控制与输出

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/?` | `/?` | 关 | 控制 | STABLE_BETA | 打印用法文本，退出码 `0`。 |
| `/help` | `/help` | 关 | 控制 | STABLE_BETA | 与 `/?` 相同。（裸词 `help` 则返回结构化目录。） |
| `/version` | `/version` | 关 | 控制 | STABLE_BETA | `version` 信封，退出码 `0`。在除 `/silent` 之外的所有其他命令之前求值。 |
| `/json` | `/json` 或 `--json` | 关 | 控制 | STABLE_BETA | 输出 JSON 信封；即使在 `OmsiLaunchW.exe` 下也强制输出到控制台。 |
| `/quiet` | `/quiet` | 关 | 控制 | STABLE_BETA | 抑制面向人的成功输出。JSON 输出（`/json`）和错误仍会写出；退出码不变。 |
| `/silent` | `/silent`（也可用 `--silent`） | 关 | 控制 | EXPERIMENTAL | 将整个命令行委托给 `OmsiLaunchW.exe`，主机进程一启动即返回 `0`。会话结果由 `OmsiLaunchW.exe`（消息框、托盘图标）、`.omsilaunch\diagnostics` 和本地控制端点报告。委托和失败对话框已经运行时验证（运行时闭包 `T04`）；见 [OmsiLaunchW.exe](omsilaunchw.md)。 |
| `/serve` | `/serve` | — | 控制 | REMOVED | 在 0.1.0-beta4 中移除：以 `OL_E_INVALID_ARGUMENT` 被拒绝（退出码 `2`）。会话所有者始终提供本地控制端点。 |
| `/verbose` | `/verbose` | 关 | 启动时 | PARTIAL | `DiagnosticsSpec.Verbose`。其值随 spec 传递；效果仅限于 `.omsilaunch\diagnostics` 下的主机跟踪。 |
| `/log` | `/log` | 开（`DiagnosticsSpec.Log` 默认为 `true`） | 启动时 | PARTIAL | `DiagnosticsSpec.Log`。实际上始终开启。 |
| `/logall` | `/logall` | 关 | 启动时 | PARTIAL | 同时设置 `Verbose`、`ProcessTrace`、`PluginTrace` 和 `NativeTrace`。 |
| `/omsi-logall` | `/omsi-logall` | 关 | 启动时 | PARTIAL | `DiagnosticsSpec.OmsiLogAll`。 |
| `/trace` | `/trace` | 关 | 启动时 | PARTIAL | `/trace-process` 的别名。 |
| `/trace-process` | `/trace-process` | 关 | 启动时 | PARTIAL | `DiagnosticsSpec.ProcessTrace`。 |
| `/trace-plugin` | `/trace-plugin` | 关 | 启动时 | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`：插件会把每个运行时操作（`plugin.trace.operation`：操作、结果、代码、耗时）报告到会话的宿主跟踪中。 |
| `/trace-native` | `/trace-native` | 关 | 启动时 | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`：原生层会把每次钩子的安装和移除（`native.trace`）报告到会话的宿主跟踪中。 |

<a id="planning-validation-and-harnesses"></a>
### 规划、验证与测试工具

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/plan` | `/plan` | 关 | 启动时 | STABLE_BETA | 构建并打印 `SessionPlan`，不启动 OMSI。`IsRunnable` 时退出码 `0`，否则为 `1`。需要一个启动选择（`/new`、`/saved`、`/spec` 或安装参数）；单独使用 `/plan` 而没有其他内容时会执行 `detect`。 |
| `/validate` | `/validate` | 关 | 启动时 | STABLE_BETA | 在此构建中与 `/plan` 完全相同。 |
| `/runtime-batch` | `/runtime-batch` | 关 | 运行时（所有者） | INTERNAL | 验证测试工具：进入 `Running` 后执行读取操作集，并写出 `<sessionId>-runtime-read-batch.json`。 |
| `/runtime-write-batch` | `/runtime-write-batch` | 关 | 运行时（所有者） | INTERNAL | 验证测试工具：读取操作，外加带还原的 `time.set`、`camera.set` 和 `vehicle.variable.set`；写出 `<sessionId>-runtime-write-batch.json`。 |
| `/d3d-batch` | `/d3d-batch` | 关 | 运行时（所有者） | INTERNAL | D3D 纹理生命周期的验证测试工具；写出 `<sessionId>-d3d-wave-d-batch.json`。 |
| `/runtime` | `/runtime:<operation>` | 无 | 运行时 | STABLE_BETA（分派） | 按 ID 选择一个公开运行时操作。客户端模式（无安装参数）：转发给所有者。所有者模式：在 `Running` 之后执行一次。未知 ID：`OL_E_RUNTIME_OPERATION_UNKNOWN`，退出码 `2`。 |
| `/runtime-arg` | `/runtime-arg:<key>=<value>`（可重复） | 无 | 运行时 | STABLE_BETA（分派） | 运行时参数；等同于 `--key=value`。缺少 `=`：`/runtime-arg requires key=value`，退出码 `2`。 |

<a id="world-selection"></a>
### 世界选择

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` 是默认模式，但只有存在 `/new`、`/saved`、`/last`、`/spec` 之一时才会请求启动 | 启动时 | STABLE_BETA | NEW_MAP。需要 `/map` 和 `/entrypoint-index`（没有给出所呈现入口点索引的计划会报告 `OL_E_ENTRYPOINT_REQUIRED`；没有 `/map` 则不会解析任何地图）。`/new` 从不静默选择地图。 |
| `/saved` | `/saved:<file.osn>` | 无 | 启动时 | STABLE_BETA | SAVED_SITUATION。地图和位置来自 `.osn`；`/map`、`/entrypoint`、`/entrypoint-index` 与 `/saved` 一起使用时会被拒绝（退出码 `2`）。情景缺失：`OL_E_SITUATION_NOT_FOUND`；其地图缺失：`OL_E_SITUATION_MAP_NOT_FOUND`。 |
| `/last` | `/last /map:<identity>` | 无 | 启动时 | EXPERIMENTAL | LAST_MAP_STATE：通过 Start 窗体的上次状态分支加载该地图自身的 `laststn.osn`（OMSI 在关闭地图时写入）。需要 `/map`；没有 `laststn.osn` 的地图为 `OL_E_SITUATION_NOT_FOUND`。绝不是“最新 `.osn`”的启发式方法。运行时验证待完成。 |
| `/map` | `/map:<identity>`（例如 `maps\Grundorf\global.cfg`） | 无 | 启动时 | STABLE_BETA | `/new` 的地图标识，或 `/list:Entrypoints` 的范围。未知：`OL_E_MAP_NOT_FOUND`。 |
| `/entrypoint` | `/entrypoint:<identity>` | 无 | 启动时 | EXPERIMENTAL | 按其精确（区分大小写）的标签，或按来自 `/list:entrypoints` 的标识指定入口点。OMSI 只呈现每个标签的第一条记录；共用该标签的后续记录会被拒绝（`OL_E_ENTRYPOINT_NOT_PRESENTED`），未知的也会被拒绝（`OL_E_ENTRYPOINT_NOT_FOUND`）。运行时验证待完成。与 `/entrypoint-index` 互斥（标识优先并清除索引）。 |
| `/entrypoint-index` | `/entrypoint-index:<n>`，`0..2147483647` | 无 | 启动时 | STABLE_BETA | 入口点在所呈现列表中的索引（与 OMSI 呈现的一样从 1 开始）。可运行的 NEW_MAP 计划必需此参数。 |

<a id="date-time-and-weather"></a>
### 日期、时间与天气

这些参数会被接受并带入 `LaunchSpec`。开始日期、时间和年份仅适用于 NEW_MAP 会话：与 `/saved` 或 `/last` 一起使用时，规划器将它们记录为 `UNAVAILABLE`，**并添加 `OL_E_CAPABILITY_UNAVAILABLE`，因此计划不可运行（NOT RUNNABLE，退出码 `1`）**，因为已保存情景自带其时钟。天气适用于每种世界模式。它们全部为 `EXPERIMENTAL`：原生启动路径已通过静态分析确定，运行时验证待完成。设置了这些项的 `/spec` 文件或会话配置档具有相同的效果。

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` 或 `/date:system` | 未设置 | 启动时 | EXPERIMENTAL | `DateSpec` 显式值/系统值：NEW_MAP 会话的启动日期（参见 [launchspec](launchspec.md#datespec-timespec-yearspec)）。无法解析的值：`OL_E_INVALID_ARGUMENT`，退出码 `2`；不可能存在的日期：`OL_E_DATE_TIME_APPLY_FAILED`。 |
| `/time` | `/time:<hh:mm[:ss]>` 或 `/time:system` | 未设置 | 启动时 | EXPERIMENTAL | `TimeSpec` 显式值/系统值：NEW_MAP 会话的启动时间。 |
| `/year` | `/year:<n>` 或 `/year:system` | 未设置 | 启动时 | EXPERIMENTAL | `YearSpec`：NEW_MAP 会话的启动年份（1900..2200）。 |
| `/weather` | `/weather:<preset>` | 未设置 | 启动时 | EXPERIMENTAL | `WeatherMode.Preset`：安装的 `weather` 文件夹中的某个文件，按名称（`Regen`）或路径（`weather\Regen.owt`）指定；不存在的在规划时为 `OL_E_WEATHER_INVALID`。在世界启动后立即通过 OMSI 自身的 `loadWeather` 加载；实时 METAR 保持关闭。运行时验证待完成。 |
| `/weather-icao` | `/weather-icao:<code>` | 未设置 | 启动时 | EXPERIMENTAL | `WeatherMode.Icao`：四字符 ICAO 站点的实时 METAR 天气（否则为 `OL_E_WEATHER_INVALID`）。OMSI 会把该站点保存在 `options.cfg` 中，会话会将其还原。运行时验证待完成。 |
| `/weather-real` | `/weather-real` | 未设置 | 启动时 | EXPERIMENTAL | `WeatherMode.RealCurrent`：OMSI 已配置的站点的实时 METAR 天气；没有配置站点时，启动以 `OL_E_WEATHER_APPLY_FAILED` 失败。`/weather`、`/weather-icao`、`/weather-real` 中最后出现的生效。任何启动天气都会覆盖已保存情景的天气。 |

<a id="player-vehicle"></a>
### 玩家车辆

这些参数会被接受并依据安装实例解析，但运行时不会应用它们：每个已设置的字段都是 `UNAVAILABLE`，并添加 `OL_E_CAPABILITY_UNAVAILABLE`（计划不可运行，NOT RUNNABLE，退出码 `1`）。

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>`（`Vehicles\...\*.bus`） | 未设置 | 启动时 | UNAVAILABLE | 最先解析（未知时为 `OL_E_VEHICLE_NOT_FOUND`）。 |
| `/repaint` | `/repaint:<id>` | 未设置 | 启动时 | UNAVAILABLE | 仅与 `/vehicle` 一起解析（`OL_E_REPAINT_NOT_FOUND`）。 |
| `/hof` | `/hof:<id>` | 未设置 | 启动时 | UNAVAILABLE | 未知时为 `OL_E_HOF_NOT_FOUND`。 |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | 无 | 启动时 | STABLE_BETA | 一个会话 [HOF 管理](hof-management.md)操作：`HofSourceKind` 和 `Source`。至少需要一个 `/hof-target`。会替换 `/spec` 的 `HofManagement`；与带有 `hofs` 的会话配置档预设一起使用时：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。格式错误：退出码 `2`。 |
| `/hof-target` | `/hof-target:<vehicle identity or folder>`（可重复） | 无 | 启动时 | STABLE_BETA | `Vehicles\` 之下的一个目标车辆。没有 `/hof-source`：退出码 `2`。 |
| `/hof-file` | `/hof-file:<name.hof>`（可重复） | 全部文件 | 启动时 | STABLE_BETA | 将来源限制为这些文件名。没有 `/hof-source`：退出码 `2`。 |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | 启动时 | STABLE_BETA | `HofApplyMode`。没有 `/hof-source`：退出码 `2`。 |
| `/module` | `/module:<id>`（可重复） | 无 | 启动时 | EXPERIMENTAL | 为会话请求一个[运行时模块](runtime-modules.md)（`LaunchSpec.Modules.Enable`，会加入 `/spec` 文件中的模块）。无法加载的被请求模块会使计划不可运行，并给出其 `OL_E_MODULE_*` 代码（退出码 `1`）。其操作通过 `/runtime:module.<id>.<operation>` 和 `/runtime-arg` 调用。与为其设置了 `enabled: false` 的会话配置档预设一起使用时：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |
| `/no-module` | `/no-module:<id>`（可重复） | 无 | 启动时 | EXPERIMENTAL | 使模块不进入会话，包括默认加载、由其地图加载或由设置文件加载的模块（`LaunchSpec.Modules.Disable`）。与为其设置了 `enabled: true` 的预设一起使用时：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。没有值：退出码 `2`。 |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>`（可重复） | 无 | 启动时 | EXPERIMENTAL | 设置模块设置项（`LaunchSpec.Modules.Settings`）；优先于 `/spec` 文件、设置文件和默认值。所选预设也设置了的键：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。不是 `<id>.<key>=<value>`：退出码 `2`。无效的值或未声明的键：计划诊断信息 `OL_E_MODULE_SETTING_INVALID`（退出码 `1`）。 |
| `/<module flag>` | `/<flag>:<value>`；对布尔设置项，单独的 `/<flag>` 表示 `true` | 无 | 启动时 | EXPERIMENTAL | 已安装模块为其某个设置项声明的参数（清单中的 `flag`；由 `/list:RuntimeModules` 列出）。它在规划会话时解析，与 `/module-setting:<id>.<key>=<value>` 相同。没有任何已安装模块声明的参数，或在启动之外使用的参数：`Unknown argument`，退出码 `2`。由两个模块声明的参数：`OL_E_MODULE_SETTING_INVALID`，退出码 `2`。模块参数绝不会取代内置参数。 |
| `/fleet` | `/fleet:<n>` | 未设置 | 启动时 | UNAVAILABLE | 车队编号。 |
| `/registration` | `/registration:<text>` | 未设置 | 启动时 | UNAVAILABLE | 车牌号。 |
| `/no-vehicle` | `/no-vehicle` | 关 | 启动时 | STABLE_BETA | 清除种子（`/spec` 或配置档）中的任何玩家车辆。无副作用。 |

<a id="configuration-overlays"></a>
### 配置覆盖层

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>`（可重复；键不区分大小写） | 无 | 启动时 | STABLE_BETA | 来自 `ConfigurationCatalog` 的语义 `options.cfg` 覆盖层（overlay）（例如 `graphics.maxFPS=60`、`traffic.randomVehicles=150`）。未知键：`OL_E_UNKNOWN_SETTING`（退出码 `2`）；只读键（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）：`OL_E_SETTING_NOT_WRITABLE`（退出码 `2`）；超出范围或格式错误的值：构建覆盖层时报告 `OL_E_INVALID_SETTING_VALUE`。覆盖层属于会话变更：先做快照，在 OMSI 启动前应用，停止时逐字节还原（RV-005 `RUNTIME_VALIDATED`）。与所选配置档中预设拥有的键冲突：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |

<a id="splash-presentation"></a>
### 启动画面呈现

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`、`/splash:Native`、`/splash:Unset`（不区分大小写） | `Managed` | 启动时 | STABLE_BETA | `Managed`：打包的 640x480 24 位 BMP 会一次性复制到 `<root>\.omsilaunch\assets\splash`，并以事务方式覆盖 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_<lang>.bmp`，随后精确还原（RV-006 `RUNTIME_VALIDATED`）。`Native`/`Unset`（别名）：不触及 OMSI 文件。缺少值：`/splash requires Unset, Native, or Managed`，退出码 `2`。 |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA`（也可用 `pt-BR`、`de`、`fr`、`en`；其他任何值回退为 `ENG`） | 来自 `options.cfg` 的 `[language]`，否则为 `ENG` | 启动时 | STABLE_BETA | 选择本地化的目标文件。 |
| `/splash-assets` | `/splash-assets:<directory>`（相对路径在安装根目录下解析） | `<root>\.omsilaunch\assets\splash`，否则为打包的资源集 | 启动时 | STABLE_BETA | 自定义资源目录；必须包含 `ENG.bmp`，对于非英语语言还须包含 `<lang>.bmp`。错误：`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`（在计划中表现为 `OL_E_SESSION_PRESENTATION_INVALID`；不可运行）。 |

<a id="internet-textures"></a>
### 网络纹理

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | 启动时 | EXPERIMENTAL | `Native`：不做改动。`Disabled`：经构建配置分析的进程内下载器被抑制。`Override`：将给定的 `.itx` 配置文件覆盖为 `Texture\standard.itx`；其中列出的每个 HTTP(S) 目标以及 `Texture\standard.ipr` 都成为会话删除项（在会话期间移除，停止时还原）。缺少值：退出码 `2`。 |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | 无 | 启动时 | EXPERIMENTAL | 使用 `Override` 时必需（`OL_E_ITX_PROFILE_REQUIRED`，退出码 `2`）。`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`（必须是 URL/目标行对，且 URL 为 `http`/`https`）、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`（目标必须解析到 `Texture\` 之下，不得包含带根路径、`..` 或重解析点）。 |

<a id="session-profiles"></a>
### 会话配置档

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | 无 | 启动时 | STABLE_BETA（编译；离线 `OmsiLaunch.ProfileTests`） | 加载 `<root>\.omsilaunch\session-profiles\<id>\profile.yaml`（见[会话配置档](session-profiles.md)）。需要 `/predefined-profile-index`（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`，退出码 `2`）。`new:` 块仅在使用 `/new` 时生效；`compatibility.maps` 对 `/new` 和 `/saved` 强制执行（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。与配置档拥有的字段冲突的显式参数会以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 被拒绝（`CliInput.RejectProfileConflicts`）：`new:` 块拥有地图/入口点/日期/时间/年份/天气时的相应参数、预设拥有的 `/set` 键、预设含 `presentation` 时的启动画面参数、预设含 `internet-textures` 时的网络纹理参数、预设含 `behavior` 时的超时参数。 |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | 无 | 启动时 | STABLE_BETA | 按 `index` 选择预设。超出范围：退出码 `2`。 |

<a id="launchspec-file"></a>
### LaunchSpec 文件

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | 无 | 启动时 | STABLE_BETA（加载器经离线测试；会话语义与参数相同） | 加载 `LaunchSpec` JSON 文件作为种子（见 [LaunchSpec](launchspec.md)），并标记为已请求启动。规则（`LaunchSpecJson`）：文件必须存在（`OL_E_SPEC_NOT_FOUND`，退出码 `6`）；最大 1 MiB（`OL_E_SPEC_TOO_LARGE`，退出码 `2`）；根必须是对象（`OL_E_SPEC_INVALID`）；属性名不区分大小写；允许 `//` 注释和尾随逗号；深度最多 32；每个未知属性都会连同其 JSON 路径被拒绝（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`，退出码 `2`）。 |

**优先级**（`CliInput.BuildSpecAsync`）：默认值 → `/spec` 文件 → `/predefined-profile`（替换 `Installation` 和 `World`，然后应用配置档）→ 显式参数。显式安装参数优先于 spec 中的 `RootPath`。`/no-vehicle` 清除 spec 中的玩家车辆；`/vehicle` 及相关参数逐字段合并到其中。`/set` 键合并到 `Environment.General`。`/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` 仅在给出时才覆盖。`/startup-timeout` 和 `/shutdown-timeout` 仅在给出时才覆盖；`Presentation.SuppressTrayIcon` 只能来自 spec（没有对应参数）。诊断参数与 spec 的 `Diagnostics` 按“或”合并。

<a id="content-discovery"></a>
### 内容发现

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`；类别为 `ContentQueryKind` 值 `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`（不区分大小写） | 无 | 本地，无会话 | STABLE_BETA | 对安装实例执行 `DiscoverAsync`；信封 `content.list`，包含 `Identity`、`Kind`、`DisplayName` 条目；退出码 `0`。`SessionProfiles` 由 `ListSessionProfilesAsync` 应答（信封 `session-profiles.list`），`RuntimeModules` 由 `ListRuntimeModulesAsync` 应答（信封 `runtime-modules.list`：每个模块的 id、版本、激活方式、操作、有效性，以及其设置项的类型、默认值和参数）。未知类别：`Unknown discovery category`，退出码 `2`。重解析点（联接点/符号链接）会被跳过，OMSI 文件按 Windows-1252 读取。 |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | 无 | 本地 | STABLE_BETA | 对除 `Entrypoints` 之外的每个类别转发的范围；`Entrypoints` 使用 `/map` 作为其范围。 |

<a id="timeouts-and-observation"></a>
### 超时与观察

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` 秒 | spec/配置档中的值，否则为 `180` | 启动时 | STABLE_BETA | `Behavior.StartupTimeoutSeconds`。所有者等待 `Running` 的时长为该值加 5 s；`OL_E_STARTUP_TIMEOUT` 以退出码 `1` 结束会话。 |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` 秒 | spec/配置档中的值，否则为 `30` | 启动时 | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`：配合 `/cooperative-shutdown`，指监管器在终止 OMSI 之前等待其退出的时长；没有它时，OMSI 会被立即终止，该值不起作用。 |
| `/cooperative-shutdown` | `/cooperative-shutdown` | 关 | 启动时 | EXPERIMENTAL | `Behavior.CooperativeShutdown`：结束正在运行的会话时，通过 OMSI 自身的退出例程（用户确认退出后运行的那个：它会保存 `options.cfg` 和地图的 `laststn.osn`，然后关闭）请求 OMSI 退出，并在终止它之前等待 `ShutdownTimeoutSeconds`。在经运行时验证之前为可选启用；当不得不终止 OMSI 时给出 `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`。无论哪种情况，会话的文件都会在退出后被还原。 |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | 无（一直运行，直到 OMSI 退出或收到停止请求） | 运行时（所有者） | STABLE_BETA | 运行阶段的上限：处于 `Running` 达 `n` 秒后请求规范停止。托盘或管道停止、或 OMSI 退出会使其提前结束。`0` 在进入 `Running` 后立即停止。 |

<a id="recovery"></a>
### 恢复

| 参数 | 语法与取值 | 默认值 | 阶段 | 稳定性 | 行为 |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | 关 | 本地 | STABLE_BETA | 报告 `<root>\.omsilaunch\journal.json` 是否处于待处理状态（`pending`），从不还原；退出码 `0`。会获取安装租约：所有者持有租约时返回 `OL_E_INSTALLATION_BUSY`（退出码 `7`）。 |
| `/recover` | `/recover` | 关 | 本地 | STABLE_BETA | 还原待处理的事务日志（journal）（先依据快照 SHA-256 校验备份；`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED` 在 `diagnostics` 中报告）。没有待处理内容或还原完成时退出码 `0`；存在待处理事务日志且仍未处理时为 `8`。当事务日志中记录的 OMSI 进程（PID、创建时间、exe 路径）仍存活时，或者对于已越过 `HandoffCreated` 但没有 PID 的事务日志、该根目录下的任何 `Omsi.exe` 仍存活时，以 `OL_E_INSTALLATION_BUSY` 拒绝。每次会话启动都会在读取安装实例之前自动执行相同的恢复。 |
| `/clear-module-data` | `/clear-module-data:<module id>` | 无 | 本地，无会话 | EXPERIMENTAL | 删除模块的数据目录 `<root>\.omsilaunch\module-data\<id>`，且不删除其他任何内容；信封 `module-data.clear`，包含 `module` 和 `cleared`（没有可删除内容时为 `false`）；退出码 `0`。会获取安装租约：会话拥有该安装实例期间为 `OL_E_INSTALLATION_BUSY`（退出码 `7`）。通过链接访问的模块数据文件夹会被拒绝（`OL_E_MODULE_INTEGRITY`）；其中的链接会按链接被移除。 |

<a id="output-formats"></a>
## 输出格式

- **信封成功**（`CliInput.WriteEnvelope`，使用 `--json`）：`{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`，带缩进。当为了适应控制帧而省略了较早的事件时，转发的 `session status` 和 `events read` 回复会添加 `metadata` 成员（`events_dropped_count`，见[本地控制](local-control.md)）。不使用 `--json` 时只打印 `<object>`（缩进的 JSON），如果有事件被丢弃，后面再跟 `Note: <n> older events were omitted to fit the control frame.`。
- **信封错误**（`CliInput.WriteError`，使用 `--json`）：`{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`。不使用 `--json` 时：单行 `OL_E_<CODE>: message`。类别：`invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。在 `OmsiLaunchW.exe` 下，相同的错误码和消息显示在消息框中。
- **计划与状态**（`CliInput.Write`）：`SessionPlan`、`SessionStatus` 和 `RuntimeCommandResult` 记录以缩进 JSON 打印，**不带**信封。不使用 `--json` 时，计划被概括为 `Plan: READY profile=Omsi23004_692EBFBF` 或 `Plan: NOT RUNNABLE profile=...`；其他记录仍以 JSON 打印。枚举值序列化为整数（`SessionState.Running` 为 `14`，`Completed` 为 `18`，`Failed` 为 `19`）。
- 信封中使用的命令名：`silent`、`version`、`capabilities`、`help`、`profiles`、`detect`、`recover`、`content.list`、`session`、`session.status`、`session.stop`、`events.read`、`events.watch`、`events watch`、`installation`、`cli`、`session profile`，以及转发的运行时命令所用的运行时操作 ID。
- 在 `OmsiLaunchW.exe`（`OMSILAUNCH_WINDOWS_HOST=1`）下，除非指定 `--json`，否则不向控制台写出任何内容。

<a id="errors-per-command"></a>
## 各命令的错误

| 命令 | 典型错误码 | 退出码 |
|---|---|---|
| 任何解析失败 | `OL_E_INVALID_ARGUMENT`、会话配置档错误码（`OL_E_SESSION_PROFILE_*`） | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| 客户端路由、`/runtime`（客户端） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（`2`）；`OL_E_NO_ACTIVE_SESSION`（`4`）；所有者返回的 `OL_E_CONTROL_*`、`OL_E_RUNTIME_*`，例如 `OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_SESSION_NOT_RUNNING`（`7`） | `2`、`4`、`7` |
| `session status`、`session stop`、`events read`、`events watch` | `OL_E_NO_ACTIVE_SESSION`（`4`）；`OL_E_CONTROL_SESSION_MISMATCH`、`OL_E_CONTROL_PROTOCOL`、`OL_E_CONTROL_FAILED`（`7`） | `4`、`7` |
| 所有者预检 | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`、`/recover` | `OL_E_INSTALLATION_BUSY`（`7`）；`OL_E_RECOVERY_*`、`OL_E_RESTORE_FAILED`（`8`）；待处理但未恢复（`8`） | `7`、`8` |
| `/list` | 未知类别（`2`）；`OL_E_INSTALLATION_NOT_FOUND`/目录缺失（`6`） | `2`、`6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND`（`6`）；`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`（`2`） | `2`、`6` |
| `/set` | `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`、`/validate`、启动 | 计划诊断信息：`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（已安装的插件闭包）、`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_SESSION_PRESENTATION_INVALID`、`OL_E_RUNTIME_ARTIFACT_MISSING`、`plugin.integrity.reference`（仅供参考） | `1` |
| 会话启动 | `OL_E_PLAN_NOT_RUNNABLE`（启动时重新规划，`1`）；`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_RELEASE_MANIFEST_INVALID`（通常由规划作为计划诊断信息报告，退出码 `1`；仅当插件文件在规划与启动之间发生变化时为 `7`）、`OL_E_INSTALLATION_BUSY`（`7`）；`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_EXITED_EARLY`、`OL_E_STARTUP_TIMEOUT`、`OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_PLUGIN_NOT_LOADED`（会话 `Failed`，`1`） | `1`、`7` |
| 任何位置的未处理异常 | 由 `CliProgram.Classify` 分类（见[退出码](exit-codes.md)） | `2`..`10` |

<a id="environment"></a>
## 环境

| 变量 | 设置者 | 效果 |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`：控制台输出被抑制，失败以消息框显示，`/silent` 不再重新委托。 |

<a id="see-also"></a>
## 另请参阅

[CLI 示例](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [退出码](exit-codes.md) · [错误](errors.md) · [本地控制](local-control.md) · [Windows 托盘](windows-tray.md) · [运行时控制](runtime-control.md) · [能力](capabilities.md) · [LaunchSpec](launchspec.md) · [会话配置档](session-profiles.md) · [打包](packaging.md) · [兼容性](compatibility.md) · [已知限制](known-limitations.md) · [公开 API](public-api.md)
