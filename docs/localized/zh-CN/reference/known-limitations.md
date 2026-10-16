# 已知限制

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/known-limitations.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页依据代码列出 OmsiLaunch 0.1.0-beta4 中所有 `UNAVAILABLE`、`PARTIAL` 或属于已接受的风险的内容，以免用户和集成方依赖产品并未提供的行为。每一行给出限制内容、其稳定性、存在的原因，以及详细说明所在的位置。英文文档具有规范性；`docs/localized/` 下的本地化副本的维护程度与英文不同，可能会滞后（见最后一节）。

<a id="compatibility"></a>
## 兼容性

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 读取对象布局的能力（车辆、人物对象、天气、地图、摄像机、时刻表、脚本、司机、车票）仅在已知布局（`Omsi23004`）上经过校验；在其他 OMSI 2 版本上，在结构校验器出现之前它们不可用（`OL_E_CAPABILITY_UNRESOLVED`） | 未知版本上为 `PARTIAL` | [兼容性](compatibility.md) |
| 正版 Steam 可执行文件（受 DRM 保护）尚未经过运行时验证；受控的 LAA 副本已经过验证 | Steam 为 `PARTIAL` | [兼容性](compatibility.md) |
| 只有已知布局经过运行时验证；与版本无关的解析是在隐去其标识的参考可执行文件上离线验证的 | 未知版本上为 `EXPERIMENTAL` | [运行时验证状态](../status/runtime-validation-status.md) |
| 仅支持 Windows 10+ x64；不支持 Windows 7/8、XP 和 ARM64 | `UNAVAILABLE` | [兼容性](compatibility.md) |
| 除控制器使用的 x64 运行时之外，插件还需要 **x86** .NET 6 运行时 | | [兼容性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## 世界启动与启动选项

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last /map:<identity>`）依据对 OMSI 上次状态分支的静态分析实现，且仅经过离线验证 | `EXPERIMENTAL` | [CLI](cli.md) |
| 显式或系统的日期、时间和年份（`/date`、`/time`、`/year`，配置档 `new.date`/`new.time`/`new.year`）仅适用于 NEW_MAP 启动，并经过离线验证；运行时验证待完成。无法请求闰年的 12 月 31 日（OMSI 的日历会在第 366 天回绕） | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| 启动时的天气预设、ICAO 天气和实时天气（`/weather*`、`new.weather`）会在世界启动后立即通过 OMSI 自身的天气加载和 METAR 标志应用（已通过静态分析确定，BI-003）；运行时验证待完成。OMSI 在最初一帧显示的天气可能仍是地图的默认天气 | `EXPERIMENTAL` | [CLI](cli.md) |
| 启动时的玩家车辆型号、涂装、HOF、车队编号、车牌号（`/vehicle` 系列、`PlayerVehicleSpec`）会依据内容目录进行解析，但不会应用；请求这些内容会使计划不可运行。原生分配序列（通过 OMSI 的位置对话框放置、`MakeVehicle`、各单元的收尾步骤、`CopyTempListIntoMainList`、玩家索引）已通过静态分析确定，但它会创建并注册一辆实时车辆，在能够于运行时验证之前不会实现；计划在后续的 Beta 中提供。已保存情景或 `/last` 会还原其自身的玩家车辆 | `UNAVAILABLE`（BI-007） | [能力](capabilities.md) 中的 `player.assign-headless` |
| 按标识指定入口点（`/entrypoint:<identity>`）会选择 OMSI 所展示的确切标签（已通过静态分析确定，BI-001）；与 OMSI 自身的对话框一样，无法选择共用同一标签的后续记录。运行时验证待完成 | `EXPERIMENTAL` | 计划能力 `world.entrypoint-identity` |
| 按键设置键盘和控制器（`Environment.Keyboard`、`Environment.Controllers`）没有已编入目录的键：请改用 `InputSpec` 替换整个文件 | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |
| 协作式关闭（`/cooperative-shutdown`、`Behavior.CooperativeShutdown`）为可选启用，且仅经过离线验证；默认情况下会话结束时会终止 OMSI，`ShutdownTimeoutSeconds` 不起作用 | `EXPERIMENTAL` | [CLI](cli.md) |
| 诊断参数 `/log`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process` 会填充 `DiagnosticsSpec`，没有进一步的效果（`.omsilaunch\diagnostics` 下的宿主跟踪始终会写入）；`/trace-plugin` 和 `/trace-native` 会向其中添加插件和原生跟踪事件 | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` 是验证测试工具 | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## 会话结束与进程控制

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 会话停止是强制终止：`session.stop`、托盘的“End session”菜单项（结束会话）、Ctrl+C 和 `CloseAsync` 最终都会调用 `TerminateProcess`。OMSI 的关闭例程不会运行，OMSI 退出时不会重写 `options.cfg` 或其日志文件，所有未保存的 OMSI 状态都会丢失。这是有意为之：可以防止 OMSI 覆盖已还原的文件。 | 设计如此 | [会话生命周期](../concepts/session-lifecycle.md) |
| 带超时和终止回退的协作式 WM_CLOSE 关闭未实现 | `UNAVAILABLE`（产品决定，S-11；在运行时收尾轮次中，OMSI 忽略了发送到其主窗口的 `WM_CLOSE`） | [运行时验证状态](../status/runtime-validation-status.md) |
| 控制台关闭或注销时，所有者有 4 s 的时间预算来停止并还原；未完成的部分会在下一次启动时通过事务日志恢复 | 控制台关闭已经过运行时验证；注销未测试 | [事务与恢复](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## 事务、恢复与租约

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 安装租约是一个 `Local\` 信号量：每个安装实例**在每个登录会话中**只有一个所有者；不跨用户强制执行；只要有其他进程持有句柄就不会释放；同一用户的任何进程都可以持有该名称 | 已接受的风险（S-18） | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 当事务日志中记录的 OMSI 进程仍在运行时（对于没有 PID 的事务日志，则是该根目录下的任何 `Omsi.exe` 在运行时），恢复会被拒绝（`OL_E_INSTALLATION_BUSY`） | 设计如此 | 同上 |
| 原本不存在的覆盖层路径若在会话期间内容发生变化，会阻止还原（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`），直到经过检查为止 | 设计如此 | 同上 |
| 所有权指纹引入之前的事务日志只能由计划字节完全相同的会话关闭（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 只还原会话所属的路径。OMSI 自身在会话期间的写入（当没有设置项覆盖 `options.cfg` 时的 `options.cfg` `[last_map]`、`Texture\standard.ipr`、缓存、`laststn.osn`、司机档案、日志文件）会保留下来，与直接启动 OMSI 后的情况相同 | 设计如此 | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 当 `SuppressStaleClosecheckWarning` 为 true 时，会话之前对失效 `closecheck` 的移除是永久性的（会被记录，但不会还原） | 设计如此 | 同上 |

<a id="runtime-control"></a>
## 运行时控制

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| `weather.set` 和 `weather.metar.set` 会写入 OMSI 的源天气记录（已通过静态分析确定）；其在天气刻中的持久性、云类型解析和 METAR 下载有待运行时验证。派生值（湿度、光照系数、实际风）无法设置；OMSI 暂停期间，它们只会在取消暂停后更新 | `EXPERIMENTAL` | [能力](capabilities.md) |
| `SetActualDateTime` 只是把电脑时钟复制到 OMSI 的启动时钟中（静态分析）；OmsiLaunch 不调用它，而是自行设置启动时钟 | 设计如此 | `calendar.set-actual-date-time` |
| 字符串变量写入、命名触发器和声音触发器通过 OMSI 自身的方法执行（已通过静态分析确定，BI-004）；它们对车辆脚本的影响有待运行时验证。目标只能是道路车辆 | `EXPERIMENTAL` | `scripts.string.write`、`scripts.trigger` |
| 不支持车辆重新定位、跨地块空间重新绑定，也没有 ODE 安全的变换控制权；位置字段为只读 | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` 需要 PlayerVehicle；无界面的 NEW_MAP 启动没有 PlayerVehicle（已保存情景会提供一个） | 设计如此（BI-007） | RV-004 |
| 运行时变更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、生成、随机放置、D3D 纹理）不会记入事务日志，也不会被还原 | 设计如此 | [运行时控制](runtime-control.md) |
| 句柄是否失效依据 OMSI 自身从不复用的实例编号（`IDCode`）判断，对人物对象则依据消失与重新出现；该机制经过离线验证。自然移除（RV-002：AI 汽车离开已加载的路径网络）仍有待在运行时观察，目前尚未发出 `entity.removed` 事件 | `PARTIAL` | [运行时控制](runtime-control.md) |
| 结果受 64 KiB 邮箱限制：长列表会被截断（`truncated=true`）；每次 `d3d.texture.update` 的像素负载限制为 48 KiB | 设计如此 | [能力](capabilities.md) |
| 单飞行通道：每个会话同一时间只处理一个请求；槽忙时返回 `OL_E_RUNTIME_CHANNEL_BUSY`；请求 ID 不得重复使用 | 设计如此 | [运行时控制](runtime-control.md) |
| 遥测是一个 64 事件的环：落后超过 64 个事件的宿主会丢失最旧的事件，并会被告知丢失了多少（`telemetry.lost`） | `EXPERIMENTAL` | [永久插件](../concepts/permanent-plugin.md) |
| 模块的 Direct3D 设备钩子是 vtable 钩子：在另一个组件会把设备的原始方法写回的安装上（在回归测试安装上于游戏开始后不久观察到），它们不再被调用，并且在清理时无法移除（`OL_E_MODULE_CLEANUP_INCOMPLETE`；该组件保持加载） | `PARTIAL` | [运行时模块](runtime-modules.md#native-components-and-hooks) |
| 运行时模块的加载是开放的：OmsiLaunch 不检查模块由谁制作；它只保证模块绝不会在 OmsiLaunch 进程内运行，并且会话程序被限制在其任务范围内（不会有第二个进程，随会话结束而结束） | 设计如此（E21） | [运行时模块](runtime-modules.md#security-boundary) |
| 模块协议 2（设置、默认激活、设置文件、会话程序、插件冲突、通知、`get_value`）仅经过离线验证 | `EXPERIMENTAL` | [运行时模块](runtime-modules.md) |
| 已在运行时观察到 D3D 设备重置（`resetting`、`restored`、代次失效）；由于 OMSI 的设备直接进入 `DEVICENOTRESET`，未产生单独的 `lost` 状态转换 | `PARTIAL`（RV-007） | [运行时验证状态](../status/runtime-validation-status.md) |
| 运行时列表是分页的（`cursor`、`limit`、`next_cursor`）：一页最多包含该列表的最大行数，以及 64 KiB 运行时槽所能容纳的内容 | `EXPERIMENTAL` | [能力](capabilities.md#pagination) |
| 部分高级的地图/地块/路径/对象图字段未公开；运行时读取器是受构建配置限制的类型化快照，绝不提供任意内存访问 | 设计如此 | [能力](capabilities.md) |
| 进程内内存读取针对正在运行的 OMSI 采用“先检查后使用”的方式；在检查与读取之间，OMSI 的并发修改可能导致快照不一致（`OL_E_RUNTIME_OPERATION_FAILED`） | 已接受的风险（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## 本地控制平面与信任模型

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 同一用户信任模型：命名管道（`CurrentUserOnly`）、交接/遥测/运行时内存映射以及租约信号量均可被同一 Windows 用户的任何进程访问。此类进程一旦读取到 `session_id`，就可以读取状态、停止会话或执行运行时操作。 | 已接受的风险（S-06、S-30） | [本地控制](local-control.md) |
| 控制端点仅在所有者处于 `Running` 状态时存在；在启动期间和会话结束之后，客户端会看到 `OL_E_NO_ACTIVE_SESSION`（退出码 4） | 设计如此 | [本地控制](local-control.md) |
| 如果另一个进程已占用该管道名称，所有者会在没有端点的情况下继续运行（`ListenFault`），而第二次启动可能会错误地报告 `OL_E_SESSION_ALREADY_ACTIVE` | 已接受的风险 | [本地控制](local-control.md) |
| `.omsilaunch\` 继承 OMSI 根目录的 ACL；不会应用显式的访问控制 | 已接受的风险（S-31） | [事务与恢复](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 诊断信息与输出

| 限制 | 稳定性 | 详细说明 |
| --- | --- | --- |
| 诊断信息仅为本地文件（`.omsilaunch\diagnostics`）；不会上传任何内容，也没有远程报告 | 设计如此 | [事务与恢复](../concepts/transactions-and-recovery.md) |
| 保留策略保留最新的 50 个会话；新会话启动时，会删除较旧的、以会话为前缀的诊断信息 | 设计如此 | 同上 |
| JSON 输出和诊断信息包含安装路径（`RootPath`、资源目录、`.itx` 路径） | 设计如此（本地数据） | |
| `OmsiLaunchW.exe` 的会话失败对话框将插件的失败负载（例如 `{"name":"world.failed",...}`）作为消息显示，而不是一句说明文字；`Code:` 行是正确的 | 外观问题 | [Windows 托盘](windows-tray.md) |
| 在任何 Direct3D 调用之前就被原生桥拒绝的 D3D 请求会正确报告 `native_status`，但其 `detail` 文本显示 `HRESULT 0x00000000` | 外观问题 | [能力](capabilities.md) |
| 托盘状态窗口是打开时对已计划会话拍摄的快照；它不会刷新，也不显示 OMSI 的实时值 | 设计如此 | [Windows 托盘](windows-tray.md) |

<a id="documentation"></a>
## 文档

`docs/` 下的英文页面是此版本的规范性文档。`docs/localized/<locale>/` 包含相同 `0.1.0-beta4` 页面的翻译（参见 [`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md)）；译文与英文文本不一致时，以英文文本和代码为准。其中列出的历史页面和旧版页面仅提供英文版本。

相关页面：[能力](capabilities.md)、[运行时验证状态](../status/runtime-validation-status.md)、[错误](errors.md)。
