# Windows 托盘指示器

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/windows-tray.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

每个独立的所有者会话（通过 `OmsiLaunch.exe` 或 `OmsiLaunchW.exe` 启动）都会显示一个通知区域图标，用于报告会话状态并允许用户结束会话。本页规定该指示器的行为，依据是 `tools\OmsiLaunch.Cli\WindowsHost.cs` 中 `SessionTrayIndicator`、`StatusWindow` 和 `StopConfirmationWindow` 的实现、只读呈现器 `SessionStatusPresenter`（`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`）、字符串注册表 `WindowsUiStrings`（`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`），以及 `WindowsHost` 的 `OmsiLaunchW.exe` 失败对话框。托盘只是一个呈现适配器：它既不拥有 OMSI，也不负责恢复，其停止操作所触发的是与 `session stop` 相同的规范所有者停止路径（参见 [CLI 参考](cli.md)和[本地控制](local-control.md)）。

<a id="when-the-icon-exists"></a>
## 图标何时存在

| 步骤 | 行为 |
|---|---|
| 创建 | 在 `StartSessionAsync` 返回后立即创建，此时会话尚未进入 `Running`；设置了 `SuppressTrayIcon` 时除外。因此，在 `StartingProcess`、`WaitingForPlugin`、`StartingWorld` 和 `EnteringGameplay` 期间图标已经存在。 |
| 启动时限 | UI 线程（`STA`、后台线程、名为 `OmsiLaunch tray`）必须在 2 s 内发布图标。否则，或在创建期间发生任何异常时，指示器会被释放，会话**不带**图标继续运行；日志中会记录 `startup-timeout` 或该异常。缓慢的启动永远不会留下孤立的可见图标。 |
| 移除 | 在所有者的 `finally` 块中，于会话完成或失败之后、`CloseAsync` 之前执行。释放操作会将关闭请求投递到 UI 线程（关闭菜单、确认对话框和状态窗口，然后结束消息循环），等待该线程结束最多 2 s（超时则记录 `dispose-timeout`），然后隐藏并释放 `NotifyIcon`。 |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon`（一个 `LaunchSpec` 字段，`Presentation.SuppressTrayIcon`，默认值 `false`）。可以通过 `/spec` 和 API 设置；没有对应的 CLI 参数。自行呈现会话入口的集成方会将其设为 `true`；会话的其他方面不会因此改变。 |
| 主机 | `OmsiLaunch.exe`（控制台）和 `OmsiLaunchW.exe`（Windows 子系统）都会显示该图标；使用 `/silent` 启动的 `OmsiLaunchW.exe` 会话没有其他可见界面。 |

<a id="icon-and-tooltip"></a>
## 图标与工具提示

- 图标：与正在运行的可执行文件关联的图标（`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`，即嵌入的 OmsiLaunch 图标），后备为 `SystemIcons.Application`。
- 工具提示文本：`Tray.Running`（`OmsiLaunch is running`，意为“OmsiLaunch 正在运行”）。停止期间该文本不会改变（目前还没有表示“正在停止”的字符串；在所有者将其移除之前，图标保持原样）。
- 已启用视觉样式（`Application.EnableVisualStyles`）。

<a id="interaction"></a>
## 交互

| Action | 结果 |
|---|---|
| 右键单击 | 使托盘窗口成为前台窗口（通知图标菜单需要这样做；否则在 OMSI 位于前台时，菜单可能忽略单击且永不关闭），然后在真实光标位置打开上下文菜单（使用 `Cursor.Position` 而不是事件坐标，因为对于由外壳承载的事件，`NotifyIcon` 可能报告 `(0,0)`）。菜单会被限制在光标所在屏幕的工作区内。 |
| 双击 | 打开状态窗口（与 `Status` 菜单项相同）。 |
| 左键单击 | 无操作。 |
| 菜单项 `Status`（`Tray.Status`） | 打开（如果已打开则激活）只读状态窗口。 |
| 分隔符 | |
| 菜单项 `End session`（`Tray.EndSession`，无障碍描述为 `Tray.EndSessionDescription`） | 打开确认对话框。 |

<a id="status-window-read-only-snapshot"></a>
### 状态窗口（只读快照）

通过 `Status`（“状态”）菜单项或双击图标打开（`SessionTrayIndicator.ShowStatus`）。它是一个固定大小、居中、自动调整尺寸的对话框，不在任务栏中显示，只有一个 `Close`（“关闭”）按钮（`Status.Close`；按 `Escape` 也可关闭）。如果窗口已经打开时再次选择 `Status`，会激活该窗口而不重新构建它（它保留首次打开时的快照）。构建窗口失败时会写入 `tray-host.log`；不会影响会话。

**它是快照，而不是实时视图。** `SessionStatusPresenter.Create(plan, status, ui)` 在窗口打开时只运行一次：它读取会话已解析的 `SessionPlan`（实际被规划的 spec）以及一个 `SessionStatus`（仅读取其 `State`）。窗口保持打开期间不会刷新任何内容，也从不查询 OMSI（没有运行时操作，没有遥测值）。要查看更新的状态，请关闭后重新打开。

窗口标题和窗口内标题使用相同的文本：当 `SessionStatus.State` 为 `Running` 时为 `Status.SessionRunning`（`Session is running`，意为“会话正在运行”），否则为原始的 `SessionState` 名称（例如在启动期间打开时显示 `WaitingForPlugin`，因为图标在进入 `Running` 之前就已存在）。`Status.Title`（`OmsiLaunch session status`）已在字符串表中定义，但本版本未使用。

各部分按以下顺序显示，没有字段的部分会被省略。所有值都来自规划的 `LaunchSpec`/`SessionPlan`，从不来自 OMSI。

| 部分（英文标签） | 字段（英文标签） | 显示条件 | 值 | 来源（公共等价项） |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | 始终显示 | `New session`（`WorldMode.NewMap`）、`Saved situation`（`WorldMode.SavedSituation`）、`Last map state`（任何其他模式；由于 `LastMapState` 不可运行，实际上不会出现） | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | 计划解析出了 `map` 内容标识 | 地图的 `DisplayName`（地图目录名，例如 `Grundorf`），否则为该标识的不含扩展名的文件名 | `SessionPlan.ResolvedContent` 中 `Kind = "map"` 的条目（NEW_MAP）；`DiscoverAsync(Maps)` 给出相同的 `DisplayName` |
| `Session` | `Situation` | 带情景标识的 `SavedSituation` | 不含扩展名的文件名（`situations\Linie 5.osn` → `Linie 5`） | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | 请求了入口点 | 如果设置了入口点标识则显示该标识（在本版本中永远不可运行），否则显示呈现索引的整数值（`1`） | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | 使用了会话配置档（`/predefined-profile`） | 配置档的 `name` | `Spec.SessionProfile.Name`（`SessionProfileMetadata`） |
| `Session profile` | `Preset` | 同上，且预设有名称时 | 预设的 `name` | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`、`Time`、`Weather` | 请求了显式/系统日期、时间或天气 | `DD/MM/YYYY` 或 `System`；`HH:MM:SS` 或 `System`；ICAO 代码、预设名称或 `Real/current` | `Spec.Date`、`Spec.Time`、`Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`、`Repaint`、`HOF`、`Fleet number`、`Registration` | 请求了玩家车辆字段 | 所请求的标识/值 | `Spec.PlayerVehicle` |
| `Configuration` | 每个语义设置项一个字段 | 设置了某个设置项（`/set`、配置档的 `settings`、`LaunchSpec.Environment.*`），且 `ConfigurationCatalog` 识别该设置项 | 所请求的值；以 `Percent` 结尾的键追加 `%`，以 `DistanceMeters` 结尾的键追加 ` m`。标签为设置项键，其中以点分隔的每一部分首字母大写（`graphics.maxFPS` → `Graphics MaxFPS`） | `Spec.Environment.*`；这些值与计划的 `PlannedMutations` 相同 |
| `Presentation` | `Splash` | 始终显示 | `Managed` 或 `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | 始终显示 | `Original OMSI`（`Native`）、`Disabled`、`Override` | `Spec.EffectiveInternetTextures.Mode` |

`Environment` 部分会在请求了日期、时间、年份或天气时出现在正在运行的 Beta 4 会话中（这些请求为实验性，参见[已知限制](known-limitations.md)）。`Vehicle` 部分永远不会出现在正在运行的会话中：请求任何玩家车辆字段都会使计划不可运行，因此不会启动此类会话。它是为将来的版本准备的，并由离线呈现器测试覆盖。

运行时证据（pt-BR 界面，运行时收尾轮次 `T01`）：一个 NEW_MAP Grundorf 会话显示了 `Sessão em execução`；`Sessão`：`Modo: Nova sessão`、`Mapa: Grundorf`、`Ponto de entrada: 1`；`Apresentação`：`Splash: Gerenciado`、`Texturas da internet: OMSI original`。

不使用托盘的工具也可以获取相同的数据：通过[本地控制平面](local-control.md)执行 `session status` 可获得 `SessionId`、`State`、`Diagnostics` 和 `RuntimeEvents`（实时），而所有者的 `/plan --json` 输出（或 `PlanSessionAsync`）给出窗口所汇总的规划 spec、已解析内容和计划变更。

<a id="end-session-with-confirmation"></a>
### 结束会话（需确认）

1. `StopConfirmationWindow`：标题为 `End session?`（意为“结束会话？”），消息为 `OMSI 2 will be closed and the OmsiLaunch managed session will end.`（意为“OMSI 2 将被关闭，OmsiLaunch 托管的会话将结束。”），按钮为 `End session`（默认按钮，`DialogResult.OK`）和 `Cancel`（`Escape`）。对话框打开期间的第二次请求会激活该对话框，而不是再叠加一个。
2. 选择 `OK` 时，托盘调用 `requestCanonicalStop`，由此完成所有者的 `controlStopped` 信号；随后所有者调用 `StopAsync`：OMSI 被 `TerminateProcess` 终止，每个会话所拥有的文件都会被还原。托盘自身从不终止 OMSI。
3. 如果请求抛出异常，会记录该错误并显示 `Stop.Failed`（`The session could not be ended. OMSI and its managed session remain active.`，意为“无法结束会话。OMSI 及其托管会话仍处于活动状态。”）。
4. 托盘不会确认成功；当所有者完成还原并释放指示器时，图标就会消失（运行时收尾轮次 `T01`：确认 `End session` 后 607 ms，所有者结束）。
5. `Cancel`（或关闭对话框）不执行任何操作：会话继续运行（`T01`）。
6. 如果在状态窗口或确认对话框打开时收到来自其他来源的停止（`session stop`、Ctrl+C、`/observe-seconds`、OMSI 退出），这些窗口会作为指示器释放的一部分被关闭；所有者不会等待用户（`T02`：在两个窗口都打开的情况下，管道停止后 725 ms 所有者结束）。

<a id="explorer-restart"></a>
## 资源管理器重启

`TrayWindow` 是一个隐藏的原生窗口，它注册了 `TaskbarCreated` 窗口消息。当资源管理器（外壳）重启时，会广播该消息，指示器随即重新添加图标（`Visible = false; Visible = true`）。运行时收尾轮次 `T01`：在 `explorer.exe` 被终止并由 Windows 重新启动后，图标重新出现在 `Shell_TrayWnd` 中，菜单和状态窗口也继续正常工作。

<a id="localization"></a>
## 本地化

`WindowsUiStrings.Resolve` 遵循 **Windows UI 区域性**（`CultureInfo.CurrentUICulture`），从不遵循 OMSI 内容语言或会话配置档语言。解析顺序：先精确的区域性名称，再两字母语言代码，最后英语。当某个翻译缺少某个键时，该键回退到英语。

| 区域性键 | 语言 |
|---|---|
| `en`、`en-US`、`en-GB` | 英语（默认和后备） |
| `pt-BR` | 巴西葡萄牙语。`pt-PT`（以及单独的 `pt`）有意回退到英语。 |
| `de`、`de-DE` | 德语 |
| `fr`、`fr-FR` | 法语 |
| `pl`、`pl-PL` | 波兰语 |

本地化字符串涵盖工具提示、两个菜单项、状态窗口（标题、部分标题、字段标签、`Close`）、模式值和呈现值，以及确认对话框。术语表维护在 `docs\windows-ui-localization.md` 中；离线测试 `windows-ui.localization-and-status`（`tests\OmsiLaunch.WindowsUiTests`）验证解析过程和呈现器。

<a id="omsilaunchwexe-failure-dialogs"></a>
## `OmsiLaunchW.exe` 失败对话框

当 `OMSILAUNCH_WINDOWS_HOST=1`（由 `OmsiLaunchW.exe` 设置）时，`WindowsHost.ShowFailure` 会用一个标题为 `OmsiLaunch`（错误图标）的模态消息框取代控制台错误输出，其内容为：`<message>`、空行、`Code: OL_E_...`、空行、`See .omsilaunch\diagnostics for details.`（意为“详情请参见 .omsilaunch\diagnostics”）。每次调用 `CliInput.WriteError`（参数错误、`OL_E_NO_ACTIVE_SESSION`、`OL_E_SESSION_ALREADY_ACTIVE`、已分类的异常）时都会显示该消息框；启动计划不可运行时（后备消息 `The session plan is not runnable.`；文档审计 BUG-06）以及会话未能进入 `Running` 时（`The OMSI session did not reach gameplay.`，附最后一个 `OL_E_` 诊断信息，没有诊断信息时附 `OL_E_SESSION_START_FAILED`）也会显示。`OmsiLaunchW.exe` 的完整行为请参见 [OmsiLaunchW.exe 参考](omsilaunchw.md)。在 `OmsiLaunch.exe` 下，同一函数不执行任何操作。.NET 主机启动失败（shim 代码 `100`..`106`）由原生 shim 自身显示；参见[退出码](exit-codes.md)。

<a id="log-location"></a>
## 日志位置

`<root>\.omsilaunch\diagnostics\tray-host.log`，每个条目一行：ISO-8601 UTC 时间戳、一个制表符，然后是条目内容。条目包括：`created`、`removed`、`startup-timeout`、`startup-cancelled`（释放操作与启动发生竞态，消息循环被跳过）、`dispose-timeout`，以及 UI 故障的完整异常文本。日志记录尽力而为，从不抛出异常。会话主机日志（`<sessionId>-host.log`）由所有者写入同一目录；托盘日志不带会话前缀，也不受 50 个会话的保留策略清理。

<a id="lifecycle-guarantees"></a>
## 生命周期保证

- 托盘从不拥有会话：它不能启动 OMSI，不能还原文件，也不能绕过所有者的停止路径。
- 所有者的每条退出路径（正常完成、OMSI 退出、Ctrl+C、关闭控制台、管道停止、异常、`/observe-seconds`）都会在 `CloseAsync` 之前释放指示器，因此除非所有者进程被直接强制结束，否则不会有图标比其会话存活更久（Windows 会在下一次鼠标悬停时移除孤立图标）。
- 创建和释放在锁的保护下串行执行：在竞态中胜出的释放操作会使 UI 线程跳过其消息循环并立即清理。
- 所有 Windows Forms 工作都在专用的 STA 线程上进行；其他线程只能通过一个隐藏的封送控件向其投递工作。

<a id="residual-caveats-from-the-code-comments"></a>
## 残留注意事项（来自代码注释）

- 不存在表示“正在停止”的工具提示文本；在图标被移除之前，它一直显示 `OmsiLaunch is running`。
- 对于由外壳承载的事件，`NotifyIcon` 可能报告 `(0,0)` 鼠标坐标；因此改为读取光标位置。
- 确认对话框处于模态状态时，托盘消息仍会到达；不会叠加第二个确认对话框。
- 如果 UI 线程未能在 2 s 的释放时限内结束，所有者会继续运行而不等待（`dispose-timeout`）。
- 状态窗口是打开时所取的规划值快照；它不会刷新，也从不读取 OMSI。

<a id="runtime-evidence"></a>
## 运行时证据

在运行时收尾轮次中，于授权安装实例上的真实会话中观察到（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`，pt-BR Windows 界面；参见[运行时验证状态](../status/runtime-validation-status.md)）：

- 图标在 `OmsiLaunchW.exe` 下注册到真实的通知区域（`Shell_TrayWnd`）中，并在还原后被移除（`T01`..`T04`）。
- 经确认的“End session”（结束会话）会驱动规范停止和精确还原；`Cancel` 使会话保持运行（`T01`、`T03`）。
- 资源管理器重启后图标会被重新创建（`TaskbarCreated`、`T01`）。
- 当停止请求在状态窗口和确认对话框打开期间到达时，所有者会关闭它们（`T02`）。
- `OmsiLaunchW.exe` 针对参数错误（`OL_E_INVALID_ARGUMENT`）、无活动会话（`OL_E_NO_ACTIVE_SESSION`）以及在进入游戏过程之前失败的会话（`OL_E_WORLD_START_FAILED`）显示的失败对话框（`T04`）。对于最后一种情况，对话框将插件的失败载荷作为其消息显示。
- `/silent` 会分离：启动器返回，而 Windows 主机继续维持会话（`T04`）。
- 关闭控制台时 4 s 的 `ProcessExit` 时限（`L04`，控制台所有者）。

未产生的证据：引导程序 shim 的退出码对话框（`100`..`106`）。
