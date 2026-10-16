# 会话生命周期

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../concepts/session-lifecycle.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页说明 OmsiLaunch 会话如何在 `SessionState` 中从 `Created` 推进到 `Completed` 或 `Failed`：哪个组件设置每个状态、哪些插件遥测事件驱动状态转换、启动超时如何工作、停止意味着什么（强制终止）、`WaitForAsync` 返回什么、哪些状态是终止状态、哪些状态从不可见或几乎不可见，以及 CLI 所有者提供哪些保证。本页所有内容均取自 `OmsiLaunchService.StartAsync`、`SuperviseAsync` 和 `ApplyTelemetry`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）、`PluginRuntime`（`src/OmsiLaunch.Plugin/PluginRuntime.cs`）以及 `OwnerSession`（`tools/OmsiLaunch.Cli/Program.cs`）。

相关页面：[公共 API](../reference/public-api.md)、[错误码](../reference/errors.md)、[事务与恢复](transactions-and-recovery.md)、[永久插件](permanent-plugin.md)、[运行时控制](../reference/runtime-control.md)、[本地控制平面](../reference/local-control.md)、[Windows 托盘](../reference/windows-tray.md)、[CLI 参考](../reference/cli.md)、[运行时验证状态](../status/runtime-validation-status.md)、[`.omsilaunch` 目录](../../../concepts/omsilaunch-directory.md)。

<a id="overview"></a>
## 概览

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## `SessionState` 参考

各值按声明顺序列出。“设置者”列出调用 `Move`/`Fail` 的代码；“可观察”说明 `GetStatusAsync`/`WaitForAsync` 在实际中能否看到该状态。

| # | 状态 | 设置者 | 可观察 | 含义 |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync`（活动会话的初始值） | 短暂可见 | 会话已注册，尚未发生任何操作。 |
| 1 | `ValidatingPlatform` | 无 | 否 | 已声明，但当前服务从不设置（平台验证在 `PlanSessionAsync` 中进行，而该阶段没有会话状态）。 |
| 2 | `Planning` | 无 | 否 | 已声明，从不设置（规划发生在会话存在之前；`StartSessionAsync` 中的重新规划也在注册之前进行）。 |
| 3 | `AcquiringInstallationLock` | `StartAsync` | 是 | 正在获取安装租约。失败：`OL_E_INSTALLATION_BUSY`。 |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | 是 | 在读取当前安装之前还原待处理的 `journal.json`；校验永久插件闭包（`plugin.integrity.reference`），计算 `Omsi.exe` 的哈希，准备启动画面资源，删除遗留的 `closecheck`。失败：`OL_E_PERMANENT_PLUGIN_*`、`OL_E_SPLASH_*`、`OL_E_ITX_*`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_*`、`OL_E_INSTALLATION_BUSY`（事务日志中记录的进程仍存活）。 |
| 5 | `Snapshotting` | `StartAsync` | 实际上不可见 | 紧接在 `ApplyingConfiguration` 之前设置，中间没有 await；快照本身在 `ApplyAsync` 内部生成。属于瞬态，不可观察。 |
| 6 | `ApplyingConfiguration` | `StartAsync` | 是 | 写入 `journal.json`（`Prepared`），备份原始文件，写入覆盖层，删除会话删除项（`Applied`）。失败：`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、I/O 错误。 |
| 7 | `DeployingRuntime` | `StartAsync` | 是 | 事务日志状态为 `RuntimeDeployed`。不部署任何文件：插件闭包是永久性的。 |
| 8 | `CreatingStartupHandoff` | `StartAsync` | 是 | 交接（`OmsiLaunch.Handoff.<id>`）、遥测槽（`OmsiLaunch.Telemetry.<id>`）和运行时邮箱（`OmsiLaunch.Runtime.<id>`）已存在；事务日志状态为 `HandoffCreated`。 |
| 9 | `StartingProcess` | `StartAsync` | 是 | 以工作目录 `<root>` 对 `<root>\Omsi.exe` 调用 `CreateProcessW`。失败：`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。 |
| 10 | `WaitingForPlugin` | `StartAsync` | 是 | 进程已存在，`process.started` 已记录，事务日志为 `ProcessStarted`，监管器已启动，`StartSessionAsync` 返回。 |
| 11 | `PluginBootstrap` | `ApplyTelemetry`（收到 `plugin.started` 时） | 是 | 永久插件已读取到本会话的有效交接数据。从此处起，超时报告为 `OL_E_STARTUP_TIMEOUT`，而不是 `OL_E_PLUGIN_NOT_LOADED`。 |
| 12 | `StartingWorld` | `ApplyTelemetry`（收到 `world.starting` 时） | 是（仅 NEW_MAP） | 插件已在 OMSI 的 UI 线程上调用原生 NEW_MAP 启动。已保存情景发出的是 `world.situation.starting`，该事件没有映射，因此 SAVED_SITUATION 会话会从 `PluginBootstrap` 直接进入 `Running`。 |
| 13 | `EnteringGameplay` | 无 | 否 | 已声明，从不设置：`gameplay.entered` 会让会话直接进入 `Running`。 |
| 14 | `Running` | `ApplyTelemetry`（收到 `gameplay.entered` 时） | 是 | 已进入游戏。允许调用 `ExecuteRuntimeAsync`；CLI 所有者打开本地控制平面；托盘显示正在运行的会话。 |
| 15 | `ProcessExited` | `SuperviseAsync` | 是（仅成功的会话） | OMSI 已退出（自然退出或被终止），事务日志为 `ProcessExited`。 |
| 16 | `Restoring` | `SuperviseAsync`（以及启动失败路径） | 是（仅成功的会话） | 每个会话所有的文件都从其经过校验的备份还原；删除会话产物。 |
| 17 | `CleaningRuntime` | `SuperviseAsync` | 是（仅成功的会话） | 还原已校验，事务日志和备份已删除；运行时存储即将释放。 |
| 18 | `Completed` | `SuperviseAsync`（`finally`） | 是，终止状态 | 存储已释放，邮箱已关闭，租约已释放，未记录任何失败。 |
| 19 | `Failed` | `LiveSession.Fail`，来自 `StartAsync`、`SuperviseAsync`、`ApplyTelemetry` | 是，终止状态 | 已记录一条失败诊断信息。该状态具有粘滞性：之后的 `Move` 调用会被忽略，因此失败的会话永远不会显示 `ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed`，尽管终止和还原仍会执行。 |

终止状态：`Completed` 和 `Failed`。进入其中任一状态后，`WaitForAsync` 会立即返回，`CloseAsync` 会直接返回而不发出停止请求。

关于“从不设置”的验证：在代码库中搜索 `SessionState.ValidatingPlatform`、`SessionState.Planning` 和 `SessionState.EnteringGameplay`，只能找到枚举声明；`SessionState.Snapshotting` 只出现一次，且紧跟着 `Move(SessionState.ApplyingConfiguration)`。

<a id="start-phase-startsessionasync"></a>
## 启动阶段（`StartSessionAsync`）

1. 拒绝不可运行的计划（`OL_E_PLAN_NOT_RUNNABLE`），重新规划该规格（重新计算 `Omsi.exe` 的哈希、重新解析内容、重新检查插件闭包），如果已不再可运行则再次拒绝。注册活动会话（`Created`）。
2. 创建主机跟踪文件 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`（超出最新 50 个会话的较旧会话前缀文件会被清理）。拒绝超出 1..600 范围的 `StartupTimeoutSeconds`（`ArgumentOutOfRangeException`；会话被注销）。
3. `AcquiringInstallationLock` → 租约。`RecoveringPreviousTransaction` → 恢复待处理的事务日志（无法证明所有权的旧版无指纹事务日志会被推迟，待本会话的覆盖层生成后再重试一次）、校验永久插件闭包、计算可执行文件的哈希、删除遗留的 `closecheck`、构建事务（覆盖层：`options.cfg` 补丁、托管启动画面 BMP、`Texture\standard.itx`；删除项：ITX 目标、`Texture\standard.ipr`，以及不存在时的 `closecheck`）。
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin`，然后启动监管器任务并返回句柄。
5. 步骤 3–4 中的任何异常都会被捕获：会话以 `OL_E_START_SESSION`（附内部消息）进入 `Failed`，已创建的进程被终止并等待其退出，存储被释放，事务被还原（失败时为 `OL_E_RESTORE_FAILED`），或者在无法确认 OMSI 已退出时保持待处理状态并记录 `OL_E_RESTORE_DEFERRED`；租约被释放。这种情况下 `StartSessionAsync` 仍会返回句柄；请读取 `GetStatusAsync`。

重新规划会保留调用方的 `SessionId`，因此句柄中的 ID 等于 `plan.SessionId`。

<a id="supervision-superviseasync"></a>
## 监管（`SuperviseAsync`）

监管器在线程池任务上运行，每 100 ms 循环一次，直到 OMSI 退出或收到停止请求：

1. 按顺序读取自上次读取以来插件提交的每个遥测事件（一个事件环；落后的读取方会收到带有计数的 `telemetry.lost`）。每个事件都会追加到 `RuntimeEvents`，并由 `ApplyTelemetry` 进行映射。
2. 如果会话为 `Failed`，退出循环。
3. 如果会话尚未进入 `Running` 且截止时间（监管器进入后 `StartupTimeoutSeconds`）已过：若已到达 `PluginBootstrap`，则以 `OL_E_STARTUP_TIMEOUT` 执行 `Fail`，否则以 `OL_E_PLUGIN_NOT_LOADED` 执行；退出循环。

循环结束后：如果 OMSI 在 `Running` 之前退出且未记录任何失败，则以 `OL_E_PROCESS_EXITED_EARLY` 执行 `Fail`。然后，无论会话是否失败：如果 OMSI 仍存活则终止它，等待其退出，将事务日志标记为 `ProcessExited`，进入 `ProcessExited`，执行还原（`Restoring` → `CleaningRuntime`）或记录 `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`，释放进程句柄、交接、遥测槽和运行时邮箱，释放租约，并在状态不是 `Failed` 时进入 `Completed`。监管器自身内部的故障记录为 `OL_E_PROCESS_SUPERVISION`（清理问题记录为 `OL_E_PROCESS_CLEANUP_FAILED`），并执行同样的终止/还原路径。

由于 `Failed` 具有粘滞性，失败的会话已被还原的唯一证据是其诊断信息中没有 `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED`（并且不存在 `journal.json`）；还原说明（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）在两种情况下都会出现。

<a id="telemetry-events"></a>
## 遥测事件

插件将 JSON `{ "name": ..., "data": {...} }` 样本发布到遥测槽中；主机将每个新样本记录为 `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)`。

| Event | 发出方 | 主机操作 |
| --- | --- | --- |
| `plugin.started`（`session_id`） | `PluginRuntime.Start`，在读取到有效交接数据后 | `Move(PluginBootstrap)`；`PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`：没有 `OMSILAUNCH_HANDOFF_NAME`，或交接数据不可读、无法校验 | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`：交接数据请求了 NEW_MAP/SAVED_SITUATION 以外的世界模式、非无头启动、玩家车辆、日期/时间模式，或情景标识为空 | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`：进程内版本（build）验证失败 | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | 仅记录 |
| `headless.arm.failed` | `PluginRuntime.Start`：无法挂载原生无头启动 hook | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | 仅记录 |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `CurrentDnneAdapter.PluginStart`，当 `InternetTextures.Mode` 为 `Disabled` 时 | 仅记录 |
| `world.starting`（`map`、`presented_index`、`entrypoint_identity`） | `PluginRuntime.ConsumePendingWorld`（NEW_MAP） | `Move(StartingWorld)` |
| `world.waiting-native-ready`（`native_status` 为 3 或 4） | NEW_MAP：OMSI 尚未就绪；在下一个 UI 定时器节拍重试启动 | 仅记录 |
| `world.loaded`、`world.entrypoint.selected`（`presented_index`、`raw_index`、`presented_label`、`raw_label`） | NEW_MAP 成功路径 | 仅记录 |
| `world.failed`（`native_status`） | NEW_MAP：原生启动返回失败 | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`、`world.situation.loaded`（`situation`） | SAVED_SITUATION 路径 | 仅记录（无状态变化） |
| `world.situation.failed`（`native_status`、`situation`） | SAVED_SITUATION：原生启动返回失败 | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered`（NEW_MAP：入口点选择字段或 `entrypoint_diagnostics = unavailable`；SAVED_SITUATION：`situation`） | 世界启动结束时 | `Move(Running)` |
| `d3d.ready`、`d3d.lost`、`d3d.resetting`、`d3d.restored`、`d3d.stopped`（`state`、`generation`、`execution_thread_id`、`live_textures`） | `CurrentRuntimeControl.PollLifecycle`，在任一 `d3d.*` 操作激活探针之后 | 仅记录 |
| `camera.lock.degraded`（`code`） | `CurrentRuntimeControl.PollLifecycle`，当重新应用处于活动状态的 `camera.lock` 抛出异常时（每种不同错误只报告一次） | 仅记录 |
| 无效 JSON | 任意 | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

注意事项：该槽只保存一个样本，因此在一次 100 ms 主机轮询内发出的多个事件可能会丢失（插件在 `gameplay.entered` 之后的 2 s 内抑制生命周期事件，并且从不在发布 `gameplay.entered` 的同一节拍中发布 D3D 事件，因此不会错过 `Running` 边界）。`RuntimeEvents` 保留最近的 256 个事件；更早的事件会被丢弃。它不是无损日志。请通过 `GetStatusAsync`、控制平面上的 `session.events` 或 CLI 中的 `events read|watch` 读取事件。

<a id="startup-timeout"></a>
## 启动超时

| 项目 | 值 |
| --- | --- |
| 来源 | `LaunchSpec.Behavior.StartupTimeoutSeconds`（默认 180；1..600；CLI `/startup-timeout`，配置档 `behavior.startup-timeout`）。 |
| 开始计时 | 监管器任务进入其循环时（句柄返回之后）。 |
| 在 `PluginBootstrap` 之前到期 | 以 `OL_E_PLUGIN_NOT_LOADED` 进入 `Failed`。 |
| 在 `PluginBootstrap` 之后、`Running` 之前到期 | 以 `OL_E_STARTUP_TIMEOUT` 进入 `Failed`。 |
| `Running` 之后 | 不再适用超时；会话持续到 OMSI 退出或收到停止请求。 |
| CLI 所有者 | 等待 `StartupTimeoutSeconds + 5` 秒以进入 `Running`；失败时输出状态，在 `OmsiLaunchW.exe` 下显示一个包含最后一条 `OL_E_` 诊断信息的对话框（后备错误码为 `OL_E_SESSION_START_FAILED`），并在 `CloseAsync` 之后以 1 退出。 |

`ShutdownTimeoutSeconds` 虽然包含在规格中，但不会被使用：不存在关闭等待。

<a id="stop-semantics"></a>
## 停止语义

所有停止请求都是同一个规范停止请求：API 中的 `StopAsync(handle)`、对非终止状态会话调用的 `CloseAsync`、本地控制平面上的 `session.stop`（绑定到当前活动会话 ID）、托盘中的“End session”（结束会话）、CLI 所有者中的 Ctrl+C 或关闭控制台，以及 `/observe-seconds` 到期。

| 步骤 | 详情 |
| --- | --- |
| 1 | 在活动会话上设置 `StopRequested`；调用方立即返回。 |
| 2 | 在 100 ms 内，监管器退出循环并调用 `TerminateProcess(Omsi.exe, 1)`。这是强制终止：OMSI 的关闭例程不会运行，OMSI 不会重写 `options.cfg`，也不会出现保存对话框。这是有意为之，目的是让 OMSI 无法覆盖事务即将还原的文件。 |
| 3 | 监管器等待进程退出，记录 `ProcessExited`，精确还原每个会话所有的文件（包括 OMSI 在会话期间写入的 `closecheck` 标记，它会成为一条 `restore.session-artifact-removed` 说明），删除事务日志和备份，释放运行时存储（之后的 `ExecuteRuntimeAsync` 调用会抛出 `OL_E_RUNTIME_CHANNEL_CLOSED` 或 `OL_E_SESSION_NOT_RUNNING`），释放租约，并进入 `Completed`。 |
| 自然退出 | 如果 OMSI 在 `Running` 之后自行退出（用户关闭 OMSI），则执行同一路径但不进行终止，会话正常完成。在 `Running` 之前退出则为 `OL_E_PROCESS_EXITED_EARLY`。 |
| 协作式关闭 | 未实现。发送 `WM_CLOSE` 并等待 `ShutdownTimeoutSeconds` 未实现（产品决策；在运行时收尾轮次 `L05b` 中，OMSI 忽略了发送到其主窗口的 `WM_CLOSE`）（[运行时验证状态](../status/runtime-validation-status.md)）。 |
| 运行时侧状态 | 通过运行时操作更改的任何内容（时钟、摄像机、生成的车辆、脚本变量、D3D 纹理）都是进程内状态，随进程一起消失；它们从不被还原或持久化。 |

<a id="waitforasync-semantics"></a>
## `WaitForAsync` 语义

| 情形 | 结果 |
| --- | --- |
| 会话到达所请求的状态 | 返回 `State == requested` 的状态。 |
| 会话先到达终止状态 | 立即返回 `Completed` 或 `Failed`（请检查 `Diagnostics`）。 |
| 超时到期 | 返回当前状态（不抛出异常）。请将 `State` 与您请求的状态进行比较。 |
| 所请求的状态已经过去（或从不设置：`ValidatingPlatform`、`Planning`、`EnteringGameplay`，实际上还有 `Snapshotting`） | 一直等待，直到进入终止状态或超时。 |
| 调用方取消 | `OperationCanceledException`。 |
| 未知或已关闭的句柄 | `KeyNotFoundException`。 |

轮询间隔为 100 ms，因此观察到的状态转换比实际转换最多滞后 100 ms。

<a id="owner-lifecycle-guarantees-cli"></a>
## 所有者生命周期保证（CLI）

`tools/OmsiLaunch.Cli/Program.cs` 中的 `OwnerSession.RunAsync` 是参考所有者实现。

| 保证 | 详情 |
| --- | --- |
| 单一所有者 | 启动前，CLI 会探测控制管道；如果已有所有者响应，则以 `OL_E_SESSION_ALREADY_ACTIVE` 拒绝（退出码 7）。租约在进程之间强制执行同样的规则。 |
| 每条退出路径都会到达 `CloseAsync` | 从 `StartSessionAsync` 开始，异常、Ctrl+C（`CancelKeyPress`）、关闭控制台/注销（`ProcessExit`：发出停止请求，所有者最多等待 4 s 直到 `Completed`；剩余部分会在下次启动时通过事务日志恢复）、托盘停止、控制平面 `session.stop`、`/observe-seconds` 到期以及自然完成，都会结束于 `finally` 块，该块会释放控制平面和托盘并等待 `CloseAsync`。 |
| `/observe-seconds` 是上限 | 托盘或控制平面的停止请求仍可使会话提前结束。 |
| 控制平面仅在 Running 期间存在 | 命名管道端点在 `Running` 之后（以及在任何 `INTERNAL` 验证批次之后）创建，并在 `CloseAsync` 之前释放；在其他时间客户端会收到 `OL_E_NO_ACTIVE_SESSION`。 |
| 退出码 | 最终状态为 `Completed` 时为 0；为 `Failed` 或未进入游戏时为 1；所请求的恢复未完成时为 8（[退出码](../reference/exit-codes.md)）。 |
| 诊断信息 | 主机跟踪和运行时操作产物位于 `<root>\.omsilaunch\diagnostics` 下，托盘日志文件为 `tray-host.log`；没有数据离开本机。 |

编写自有所有者的集成方必须实现前两项保证：每个安装实例同一时间只能有一个 `StartSessionAsync`，并且每条路径都要调用 `CloseAsync`。

<a id="failure-map"></a>
## 失败对照

| 阶段 | 失败时的状态 | 您将看到的诊断信息 |
| --- | --- | --- |
| 计划 | 无（没有会话） | 由 `StartSessionAsync` 抛出的 `OL_E_PLAN_NOT_RUNNABLE`；计划自身的 `OL_E_` 错误码（[LaunchSpec 验证](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)）。 |
| 启动（从租约到进程创建） | `Failed` | 带内部错误码的 `OL_E_START_SESSION`；可能还有 `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_RESTORE_FAILED`。 |
| 插件引导 | `Failed` | `OL_E_PLUGIN_NOT_LOADED`、`OL_E_PLUGIN_PROTOCOL_MISMATCH`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_BUILD_VALIDATION_FAILED`、`OL_E_HEADLESS_ARM_FAILED`。 |
| 世界启动 | `Failed` | `OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_STARTUP_TIMEOUT`、`OL_E_PROCESS_EXITED_EARLY`。 |
| 运行中 | 仅在监管器故障时为 `Failed` | `OL_E_PROCESS_SUPERVISION`；运行时操作错误从不会使会话失败。 |
| 终止与还原 | `Failed` | `OL_E_RESTORE_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_PROCESS_CLEANUP_FAILED`。 |

每条失败路径仍会尝试终止和还原；遗留的事务日志会在下次启动时，或通过 `RecoverPendingAsync` / `/recover` 恢复（[事务与恢复](transactions-and-recovery.md)）。
