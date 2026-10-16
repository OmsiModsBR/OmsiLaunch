# 错误码与诊断码

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/errors.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 `PublicErrorCodes`（`src/OmsiLaunch.Api/PublicErrorCodes.cs`）中每个代码的规范性参考：共 167 个 `OL_E_` 错误码和 4 个 `OL_W_` 警告，按目录类别分组，另附不属于错误的信息性诊断码。对于每个代码，本页说明当前代码在何处引发它、它的含义、它以何种方式到达您（抛出的异常、结果字段、诊断信息、控制平面回复或 CLI 信封）以及应如何处理。含义取自各引发位置；如果某个代码已定义但当前没有引发路径，本页会明确说明。

相关页面：[公共 API](public-api.md)、[退出码](exit-codes.md)、[LaunchSpec](launchspec.md)、[会话生命周期](../concepts/session-lifecycle.md)、[事务与恢复](../concepts/transactions-and-recovery.md)、[本地控制平面](local-control.md)、[运行时控制](runtime-control.md)、[会话配置档](session-profiles.md)、[永久插件](../concepts/permanent-plugin.md)。

<a id="how-codes-reach-you"></a>
## 代码如何到达您

| 途径 | 含义 |
| --- | --- |
| 抛出 | 一个 `Message` 以该代码开头的异常（`InvalidOperationException`、`IOException`、`TimeoutException`、`InvalidDataException`、`FileNotFoundException`、`ArgumentException`、`SessionProfileException`）。CLI 从消息中提取代码并将其映射为退出码（`CliProgram.Classify`）。 |
| 计划诊断信息 | `SessionPlan.Diagnostics` 中的一个 `LaunchDiagnostic`；任何 `OL_E_` 代码都会使 `IsRunnable` 为 false（CLI：计划 `NOT RUNNABLE`，退出码 1）。 |
| 会话诊断信息 | `SessionStatus.Diagnostics` 中的一个 `LaunchDiagnostic`；会话状态为 `Failed`（CLI 退出码 1）。`OL_E_START_SESSION`、`OL_E_PROCESS_SUPERVISION` 和 `OL_E_RESTORE_FAILED` 会在其消息中包装一个内部代码。 |
| 运行时结果 | `RuntimeCommandResult.ErrorCode`，同时 `Succeeded = false`。 |
| 运行时详情 | `RuntimeCommandResult.ErrorCode` 是具体的代码，`Values["detail"]` 是消息（另有 `Values["exception"]`，即 .NET 类型）。Beta 4：插件侧带类型的代码会原样到达 API 和 CLI；只有没有已编入目录代码的失败才会变成 `OL_E_RUNTIME_OPERATION_FAILED`。 |
| 控制回复 | 本地控制平面回复（`LocalControlResponse`）的 `ErrorCode`。 |
| CLI 信封 | `--json` 信封（envelope）中的 `error.code`，或控制台上的 `<code>: <message>`；表中给出了退出码。 |
| 遥测 | 由主机映射为会话诊断信息的运行时事件名称。 |

## Cli

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | 有 `OperationCanceledException` 逸出（Ctrl+C 或客户端等待被取消）。 | CLI 信封，退出码 7 | 重试该命令。 |
| `OL_E_INTERNAL` | `CliProgram.Classify` | 有不带 `OL_E_` 代码的异常逸出：`/spec` JSON 格式错误、必需的 spec 成员为 null、意外故障。 | CLI 信封，退出码 10 | 阅读消息和 `<root>\.omsilaunch\diagnostics\<sessionId>-host.log`；修正输入；如果无法解释，请报告。 |
| `OL_E_TIMEOUT` | `CliProgram.Classify`；`LocalControlPlane.TryRequestAsync` | 有不带代码的 `TimeoutException` 逸出；或者本地控制客户端已连接到所有者，但所有者未在超时内回复（所有者存在，因此不会报告为 `OL_E_NO_ACTIVE_SESSION`）。（邮箱超时则携带 `OL_E_RUNTIME_REQUEST_TIMEOUT`。） | CLI 信封、控制回复；来自 `Classify` 时退出码 5，控制回复时退出码 7 | 重试；检查 OMSI 和所有者是否有响应。 |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync`（`/silent`） | `OmsiLaunchW.exe` 不在 `OmsiLaunch.exe` 旁边。 | CLI 信封，退出码 7 | 重新安装该包。 |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync`（`/silent`） | 对 `OmsiLaunchW.exe` 调用 `Process.Start` 未返回进程。 | CLI 信封，退出码 7 | 检查包文件和权限；不带 `/silent` 运行以查看失败原因。 |

## Compatibility

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `OmsiLaunchService.ApplyTelemetry`，收到 `plugin.build.invalid` 时 | 启动能力已解析，但在插件启用 headless 启动时，实时 Start 窗体或其某个钩子位置不处于预期状态（例如另一个插件已对其打过补丁）。 | 会话诊断信息（`Failed`） | 移除冲突的插件；参见[兼容性](compatibility.md)。 |
| `OL_E_CAPABILITY_UNRESOLVED` | 插件（启动时的 `plugin.capability.unresolved`；运行时操作） | 请求所需的某项原生能力无法在此 OMSI 版本（build）上解析并校验，因此不可用。启动时，会话在任何钩子之前失败并还原文件；运行时操作会在不触及 OMSI 内存的情况下被拒绝。`Data["capability"]`（运行时）或遥测的 `capabilities` 值会指明该能力。 | 会话诊断信息（`Failed`）；运行时结果 | 参见[兼容性](compatibility.md)：该能力在此版本上不可用。 |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner`（`omsi.omsi2` 不可用） | 该安装不是 OMSI 2 安装：`Omsi.exe` 缺失、不是 32 位 x86 PE 映像、没有 OMSI 2（2.x）版本，或根目录中没有 `maps\` 或 `Vehicles\` 目录。可执行文件的哈希不是判断标准。 | 计划诊断信息 | 将 OmsiLaunch 指向 OMSI 2 的安装根目录。 |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner`（`runtime.current-windows-x64` 不可用）；也由 `CurrentWindowsX64Platform.ValidateCurrent` 抛出，但服务不会调用该方法 | 不是 Windows 10 或更高版本，或者操作系统或主机进程不是 x64。 | 计划诊断信息（抛出时 CLI 退出码 3） | 在 64 位 Windows 10 或更高版本上运行。 |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | 仅 `CurrentWindowsX64Platform.ValidateCurrent` | 操作系统或主机架构不是 x64。服务不会调用 `ValidateCurrent`；当前没有引发路径。 | 仅由该方法抛出（`PlatformNotSupportedException`） | 参见源代码 `src/OmsiLaunch.Process/RuntimePlatform.cs`。 |

## Content

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | 验证：NEW_MAP 未设置 `EntrypointIdentity`，且 `PresentedEntrypointIndex` 未设置或为负。规划器：地图中没有具有所请求标识或精确（区分大小写）标签的入口点。 | 计划诊断信息 | 设置 `PresentedEntrypointIndex`（`/entrypoint-index:<n>`），或使用来自 `/list:entrypoints /map:<id>` 的标识。 |
| `OL_E_WEATHER_INVALID` | `SessionPlanner`（`StartWeatherResolver`） | 无法应用启动天气：预设不是安装的 `weather` 文件夹中的文件，或 ICAO 不是四个字母或数字。 | 计划诊断信息 | 使用已安装的预设或有效的站点代码。 |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | 所请求的入口点与地图中较早的一条记录共用同一标签；OMSI 只呈现每个标签的第一条记录，因此无法在启动时选择它。 | 计划诊断信息 | 选择具有该标签的第一条记录，或另一个入口点。 |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner`（`world.presented-entrypoint` 不可用） | NEW_MAP 的地图已解析，但既没有呈现索引也没有标识。总是与 `OL_E_ENTRYPOINT_NOT_FOUND` 一同出现。 | 计划诊断信息 | 同上。 |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` 不是已安装的 `Vehicles\...\*.hof`。 | 计划诊断信息 | 使用 `/list:hofs` 中的标识。（在此版本上，玩家车辆字段无论如何都不可运行。） |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | [HOF 管理](hof-management.md)的某个来源（集合、目录、车辆或文件）不存在、没有 `.hof`，或缺少 `Files` 中指定的文件。 | 计划诊断信息；启动失败 | 修正来源或文件列表。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | HOF 目标车辆文件或文件夹未安装。 | 计划诊断信息；启动失败 | 使用来自 `/list:vehicles` 的车辆标识，或 `Vehicles\` 之下已存在的文件夹。 |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | HOF 路径离开了安装目录，目标不在 `Vehicles\` 之下，集合名称不是普通名称，或经过了联接点/符号链接。 | 计划诊断信息；启动失败 | 使用不含 `..` 且不含链接的相对于安装目录的路径。 |
| `OL_E_HOF_CONFLICT` | `HofPlanner`、`OmsiLaunchService` | 两个 HOF 操作向同一文件写入不同的字节，或某个镜像操作删除了另一个操作要写入的文件。 | 计划诊断信息；启动失败 | 重新排序、拆分或移除相冲突的操作。 |
| `OL_E_HOF_INVALID` | `HofPlanner` | HOF 操作没有目标或来源、文件来源不是 `.hof`、目标文件不是 `.bus`/`.ovh`，或违反大小/数量限制（每个文件 8 MiB、每个操作 256 个文件、每个会话 256 MiB）。 | 计划诊断信息；启动失败 | 修正该操作。 |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | 校验：NEW_MAP 的 `MapIdentity` 未设置，或其形式不是 `maps\<dir>\global.cfg`。规划器：该地图未安装。 | 计划诊断信息 | 使用 `/list:maps` 中的标识。 |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | 有不带代码的 `FileNotFoundException`/`DirectoryNotFoundException` 逸出，例如带未知 `/vehicle-scope` 的 `/list:repaints`，或带未知 `/map` 的 `/list:entrypoints`。 | CLI 信封，退出码 6 | 更正标识。 |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` 不是该车型的 `.cti` 条目（仅在设置了 `Model` 时检查）。 | 计划诊断信息 | 使用 `/list:repaints /vehicle-scope:<bus>` 中的标识。 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | 所选 `.osn` 中引用的地图未安装。 | 计划诊断信息 | 安装该地图或选择其他情景。 |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`；`SessionPlanner` | SAVED_SITUATION 未设置 `SituationIdentity`，或该 `.osn` 未安装。 | 计划诊断信息 | 使用 `/list:situations` 中的标识。 |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` 不是已安装的 `Vehicles\...\*.bus`。 | 计划诊断信息 | 使用 `/list:vehicles` 中的标识。 |

## Installation

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`；`OmsiLaunchService.RecoverPendingAsync`；`FileConfigurationTransaction.RestorePendingAsync` | 安装租约（`Local\OmsiLaunch.Installation.<hash>`）被此登录会话中的另一个所有者持有，或者日志中记录的 OMSI 进程（PID + 创建时间 + 可执行文件路径；对于已过 `HandoffCreated` 但没有 PID 的日志，则为根目录下的任何 `Omsi.exe`）仍然存活。 | 启动：通过 `OL_E_START_SESSION` 作为会话诊断信息。恢复：抛出（`InvalidOperationException` / `IOException`）。CLI 退出码 7。 | 停止另一个所有者（`session stop`）或等待 OMSI 退出，然后重试或执行 `/recover`。 |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` 为空。 | 计划诊断信息 | 传入安装目录。 |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner`（`transaction.exact-restore` 不可用）；也包括 `ValidateCurrent` | 根目录不存在、具有只读属性，或没有 `plugins\` 目录。 | 计划诊断信息 | 指向一个真实且可写的 OMSI 安装。 |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled`（规划和启动时） | 已安装的某个 `plugins\OmsiLaunch.*` 文件与 `release-manifest.json` 中的哈希不一致（没有清单时则与参考闭包不一致）。 | 计划诊断信息（计划不可运行，CLI 退出码 1）；仅当文件在规划与启动之间发生变化时，才通过 `OL_E_START_SESSION` 作为会话诊断信息 | 重新安装 OmsiLaunch 包，使 `plugins\` 与清单一致。 |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled`（规划和启动时） | 清单中缺少某个必需插件文件的条目。 | 计划诊断信息；在与上文相同的竞态下，通过 `OL_E_START_SESSION` 作为会话诊断信息 | 重新安装该包。 |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled`（规划和启动时） | OMSI 安装中缺少某个必需的 `plugins\OmsiLaunch.*` 文件，或 `release-manifest.json` 中列出的某个 `plugins/` 文件未安装。 | 计划诊断信息；在与上文相同的竞态下，通过 `OL_E_START_SESSION` 作为会话诊断信息 | 安装永久插件闭包（[安装](../getting-started/installation.md)）。 |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | 仅 `CurrentWindowsX64Platform.ValidateCurrent` | `CurrentPlatformSupported` 为 false。服务不会调用该方法；当前没有引发路径。 | 仅由该方法抛出 | 参见源代码。 |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` 为空、不是 JSON、没有 `files` 数组，或者含有以下条目：缺少 `path`/`sha256`、哈希不是 64 位十六进制数字、路径是带根路径、包含 `:`、包含空段、`.` 段或 `..` 段，或同一路径列出两次（比较时不区分大小写，`/` 与 `\` 视为等同）。接受 UTF-8 BOM。 | 规划：包装在 `OL_E_RUNTIME_ARTIFACT_MISSING` 中；启动：通过 `OL_E_START_SESSION` | 重新安装该包。 |

## InvalidArgument

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`；`CliInput.Parse`/`Classify` | 校验：在模式不是 `Explicit` 时设置了 `Date.Value`/`Time.Value`。CLI：未知参数、缺少值、整数或范围错误、`/saved` 与 `/map`/`/entrypoint` 组合使用、未知命令路由，以及任何不带代码的 `ArgumentException`/`FormatException`。 | 计划诊断信息；CLI 信封，退出码 2 | 修正参数。 |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch`（启动时） | 某个语义设置项的值超出范围、不是布尔值、不在允许的集合中，或格式错误（`graphics.particles` 需要四个字段）。规划时不会校验这些值。 | 通过 `OL_E_START_SESSION` 作为会话诊断信息 | 使用[设置项表](launchspec.md#environmentspec)中的值。 |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`；`CliInput.BuildSpecAsync`；`BuildTransactionalOverlays` | 该键存在但不可写（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）。 | 计划诊断信息；CLI 退出码 2 | 删除该键。 |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`；`CliInput.BuildSpecAsync`；`BuildTransactionalOverlays` | 该键不在 `ConfigurationCatalog` 中。 | 计划诊断信息；CLI 退出码 2 | 使用目录中的键。 |

## LaunchSpec

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | 根不是 JSON 对象，或反序列化未产生记录。 | 抛出（`InvalidDataException`），CLI 退出码 2 | 修正该文件（[LaunchSpec](launchspec.md)）。 |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | `/spec` 文件不存在。 | 抛出（`FileNotFoundException`），CLI 退出码 6 | 检查路径。 |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | 文件超过 1 MiB。 | 抛出（`InvalidDataException`），CLI 退出码 2 | 缩小该文件。 |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | 某个成员不是该位置记录的公共属性；消息为 `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`。 | 抛出（`InvalidDataException`），CLI 退出码 2 | 删除或重命名该成员。 |

## LocalControl

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | 所有者处理程序（`OwnerSession`） | 该命令不是 `session.status`、`session.events`、`session.stop`，也不是带 `operation` 参数的 `runtime.execute`。 | 控制回复；CLI 退出码 7 | 使用受支持的命令。 |
| `OL_E_CONTROL_FAILED` | CLI 客户端（`ReportForwarded`、`CliEventWatch`） | 所有者回复了 `Ok = false`，但没有错误码。 | CLI 信封，退出码 7 | 阅读消息；检查所有者的控制台/诊断信息。 |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | 所有者的处理程序抛出了一个消息中不含 `OL_E_` 代码的异常（例如会话已关闭），或处理程序的回复无法序列化。 | 控制回复 | 读取 `session status`；如果所有者已不存在，请重新启动所有者。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane`（两端） | 长度前缀为负数或超过 64 KiB（包括过大的请求帧）、空帧、JSON `null`、没有 `Command` 的请求，或无法解码的 JSON。 | 控制回复 / CLI 信封 | 使用文档所述的协议（[本地控制平面](local-control.md)）。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync`（客户端） | 客户端自身序列化后的请求超过 64 KiB。错误报告给调用方；不会发送任何内容。 | 控制回复 / CLI 信封 | 缩小请求。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync`（所有者） | 所有者的回复无法装入 64 KiB 的帧。所有者以这个类型化错误作答，而不是丢弃回复。`session.status` 和 `session.events` 永远不会触发它：它们的事件历史会从最旧的开始裁剪以适应大小。 | 控制回复 / CLI 信封 | 重试；对于事件，请更频繁地读取。 |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`、`TryRequestBoundAsync` | 请求的 `ProtocolVersion` 不是 `0.1`；所有者回复无法解码或为空；所有者未回复就关闭了连接，或连接建立后中断；所有者未报告 `SessionId`。 | 控制回复 / CLI 信封 | 使客户端与所有者版本一致；读取 `session status`。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | 所有者处理程序 | `session.stop` 或 `runtime.execute` 没有携带与活动会话相同的 `session_id`。 | 控制回复；CLI 退出码 7 | 先读取 `session.status` 并绑定请求（CLI 会自动完成此操作）。 |

## Other

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | 传入的计划 `IsRunnable = false`，或启动时的重新规划不可运行（`Omsi.exe` 已更改、内容被移除、插件闭包缺失）；消息列出当前的 `OL_E_` 代码。 | 抛出（`InvalidOperationException`）；CLI 退出码 1 | 重新规划并修正列出的诊断信息。 |

## Presentation

均由 `SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）引发。在规划时，它们被包装在 `OL_E_SESSION_PRESENTATION_INVALID` 中（消息携带该代码）；在启动时，它们通过 `OL_E_START_SESSION` 呈现。

| 代码 | 含义 / 典型原因 | 处理方法 |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | `.itx` 文件为空、非空白行数为奇数，或某个 URL 行不是绝对的 `http`/`https` URL。 | 使用 URL/目标成对的行。 |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath`（相对于进程工作目录解析）不存在。以 `FileNotFoundException` 抛出。 | 传入一个存在的 `.itx` 路径。 |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` 为 `Override`，但没有 `OverrideProfilePath`。抛出时 CLI 退出码 2。 | 提供 `/internet-textures-profile:<file.itx>`。 |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | 某个目标行是带根路径、包含 `..`、以 `\` 开头、解析到安装之外、缺少 `Texture\` 路径组成部分，或穿越了联接点/符号链接。 | 使用 `Texture\...` 相对目标。 |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` 不存在。 | 修正该目录。 |
| `OL_E_SPLASH_ASSET_MISSING` | 资源目录中缺少 `ENG.bmp` 或 `<LANG>.bmp`，或者在初始化 `.omsilaunch\assets\splash` 时缺少包内的 `assets\splash\<LANG>.bmp`。 | 提供这些 BMP / 重新安装该包。 |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | 某个启动画面 BMP 不是 640×480 的 24 位 `BM` 位图。 | 转换该图像。 |

## Process

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService`（启动失败和监管器故障路径） | 在错误清理期间终止或等待 OMSI 时抛出异常；其后附有内部消息。 | 会话诊断信息（添加到 `Failed` 会话） | 确保没有残留的 `Omsi.exe`，如果存在待处理的日志，则执行 `/recover`。 |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | 在 `CreateProcessW` 之后立即调用的 `GetProcessTimes` 失败（`Win32=<code>`）；该进程被终止。 | 通过 `OL_E_START_SESSION` 作为会话诊断信息 | 重试；检查杀毒软件/权限。 |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | OMSI 在 `gameplay.entered` 之前退出（崩溃、OMSI 错误对话框被关闭、窗口被关闭）。 | 会话诊断信息（`Failed`）；执行还原 | 检查 OMSI 自身的日志和 `logfile.txt`；查看 `RuntimeEvents` 中的最后一个插件事件。 |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` 失败（消息中含 `Win32=<code>`）。 | 通过 `OL_E_START_SESSION` 作为会话诊断信息 | 解决该 Win32 错误（文件缺失、访问被拒绝、策略）。 |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | 监管器循环抛出异常（遥测读取、进程等待/终止、日志写入）；OMSI 被终止并尝试还原。 | 会话诊断信息（`Failed`） | 阅读内部消息和主机日志。 |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` 失败（`Win32=<code>`）。 | 出现在 `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` 消息中 | 手动终止 OMSI，然后执行 `/recover`。 |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | 对进程句柄调用 `WaitForSingleObject` 失败。 | 出现在 `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` 消息中 | 同上。 |

## Runtime

“运行时详情”是指具体代码位于 `ErrorCode` 中，消息位于 `Values["detail"]`，异常类型位于 `Values["exception"]`。

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `camera.lock` 带有 `preset`，而 `family` 为 2（外部）或 3（地图）；预设仅适用于司机（0）和乘客（1）。 | 运行时详情 | 省略 `preset` 或使用 family 0/1。 |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`/`Time` 模式为 `Explicit` 但没有值，或各组成部分超出范围。该名称是历史遗留；它是一个规划时的校验错误。 | 计划诊断信息 | 修正该值（并注意在此版本上显式日期/时间不可运行）。 |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`：`.bus` 路径在 OMSI 工作目录下不存在（在原生调用之前检查，以免 OMSI 替换为后备车辆）。 | 运行时详情 | 使用 `vehicles list`/`/list:vehicles` 中的标识。 |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | 同上 | 原生 MakeVehicle 使道路车辆集合的变化超过一个对象。 | 运行时详情（`native_status`，消息中含计数） | 报告；已创建的对象会保留到会话结束。 |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | 同上 | 集合没有变化；OMSI 静默拒绝了该车辆。 | 运行时详情 | 检查 `.bus` 文件；尝试其他车型。 |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | 同上 | 任何其他非零原生状态。 | 运行时详情 | 附上消息中的计数进行报告。 |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | 已建立构建配置的 PlaceRandomBus 调用返回了失败状态。 | 运行时详情 | 待游戏过程稳定后重试；报告。 |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`；插件侧检查（`time.set` 没有 `hour`/`minute`/`second`；`camera.set` 没有 `family`/`field_of_view`；`camera.lock` 没有可解析的 `family`；车辆/曲线操作） | 某个必需参数缺失或为空。 | 运行时结果（注册表；CLI 退出码 2）或运行时详情（插件） | 提供该参数（[运行时控制](runtime-control.md)）。 |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | 无法加载 `OmsiLaunchRuntimePaths` 中给出的插件闭包参考目录/文件或原生桥（可能包装 `OL_E_RELEASE_MANIFEST_INVALID`）。 | 计划诊断信息 | 从完整的包中运行。 |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch`（`/runtime-write-batch`，INTERNAL 测试工具） | 基线 `time.read`/`camera.read` 失败，因此跳过了写入测试。 | 仅批处理产物 | 不面向用户。 |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` 为空、长度超过 240 个字符、包含 NUL 或 `..`、不以 `Vehicles\` 开头，或不以 `.bus` 结尾。 | 运行时详情 | 传入 `Vehicles\<dir>\<file>.bus`。 |
| `OL_E_RUNTIME_CHANNEL_BUSY` | 无（为兼容性而保留） | 不再发出。早期版本在已取消的请求仍留在槽中时会引发它；现在请求的每条终止路径都会重置该槽，并且在新请求开始时发现的残留请求或响应会被清除。 | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | 由于会话正在结束，邮箱已被释放。 | 抛出（`InvalidOperationException`） | 无需处理；会话已结束。 |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | 邮箱槽中的状态值不是空闲、已请求或已响应（数据损坏）。该槽被重置并引发此错误；下一个请求可正常工作。 | 抛出（`InvalidDataException`） | 重试；如果持续出现，请报告。 |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | 车辆的常量块指针为 null。 | 运行时详情 | 该车辆没有常量；无需处理。 |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在车辆的常量表中。 | 运行时详情 | 先列出常量。 |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | 生成操作创建的对象的 VMT 位于 OMSI 映像范围之外。 | 运行时详情 | 报告。 |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | 同上 | 创建的对象不在道路车辆集合中。 | 运行时详情 | 报告。 |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | 两个相邻的曲线点具有相同的 X。 | 运行时详情 | 车辆曲线的内容问题。 |
| `OL_E_RUNTIME_CURVE_EMPTY` | 同上 | 曲线没有点。 | 运行时详情 | 同上。 |
| `OL_E_RUNTIME_CURVE_INVALID` | 同上 | 曲线中没有任何分段包含 `x`。 | 运行时详情 | 在曲线的定义域内求值。 |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | 同上 | `name` 未知，或其函数指针为 null。 | 运行时详情 | 先列出曲线。 |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | 车辆定义指针为 null。 | 运行时详情 | 句柄指向一个没有定义数据的车辆。 |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync`（所有者模式） | 可执行文件旁缺少 `plugins\OmsiLaunch.Plugin.opl` 或 `plugins\OmsiLaunch.Native.x86.dll`。 | CLI 信封，退出码 7 | 重新安装该包。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `road-vehicle.read`、`human.read`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list`、`vehicle.curves.list` 的 `handle` 缺失或为空（注册表通常会先以 `OL_E_RUNTIME_ARGUMENT_REQUIRED` 拒绝这些请求）。 | 运行时详情 | 提供句柄。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | 句柄未知、对象已离开集合、地址代次已推进，或者由于地址被重用，对象指纹（VMT + 定义/车型标识）已改变。残留盲区：在两次列表读取之间，同一类别和车型的对象在同一地址被重新创建。 | 运行时详情 | 再次运行 `road-vehicles.list`/`humans.list` 并使用新句柄。 |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`；`CurrentRuntimeCommandMailbox.TryDispatch`；`D3DRuntimeApi` 后备路径 | 通用的插件侧失败包装；`Values["detail"]` 保存消息（通常是更具体的代码），`Values["exception"]` 保存异常类型。在邮箱调度器内从某个操作中逸出的异常也会以此代码（不带值）作答，而不是让请求得不到回复。 | 运行时结果 | 阅读 `detail`。 |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | 插件没有注册表所允许的某个操作的实现（注册表/插件版本漂移）。 | 运行时详情 | 重新安装版本一致的包。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | 该操作不在 `PublicRuntimeOperationIds` 中，包括所有 `internal.*` 操作。在查找会话之前检查。 | 运行时结果；控制回复；CLI 退出码 2 | 使用公共操作 ID。 |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | `camera.lock` 带有 `preset`，但没有玩家车辆（无头会话没有玩家车辆）。重新应用失败时，也会作为 `camera.lock.degraded` 事件的 `code` 报告。 | 运行时详情 / 运行时事件 | 不带预设进行锁定，或使用有玩家车辆的会话。 |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | 某个成功的 D3D 结果没有值，或设备状态字符串未知。 | 抛出（`OmsiRuntimeException`） | 使主机与插件版本一致。 |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | 槽中存有一个过期响应，其请求 ID 与新请求相同。该过期响应在引发错误之前被清除。 | 抛出（`InvalidOperationException`） | 使用严格递增的请求 ID。 |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | 在 `timeout` 内没有响应；该槽被重置，迟到的响应会被丢弃。 | 抛出（`TimeoutException`）；CLI 退出码 5 | 使用更长的超时重试；检查 OMSI 是否被阻塞（模态对话框、加载中）。 |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | 响应信封已损坏、长度错误（负数、零或大于槽）、会话 ID 不属于本会话，或请求 ID 不同。该槽在引发错误之前被重置，因此下一个请求可正常工作。 | 抛出（`InvalidDataException`） | 重试；如果持续出现，请报告。 |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage`（插件） | 列表操作的 `cursor` 格式错误，或属于另一种列表。 | 运行时详情 | 传入同一操作上一页的 `next_cursor`，或省略它。 |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | 序列化后的结果超过 64 KiB 邮箱。分页的列表结果会被缩短以适应邮箱，并通过 `next_cursor` 继续；没有任何列表结果会触发该代码。 | 运行时结果 | 使用范围更窄的操作或更小的 `limit`。 |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | 车辆的脚本定义或状态指针为 null。 | 运行时详情 | 该车辆没有脚本对象。 |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`；`CurrentRuntimeCommandStore`；插件邮箱 | `RuntimeCommand.SessionId` 与句柄的会话 ID 不同（由主机抛出），或者某个请求到达了绑定到另一会话的插件（由插件作为类型化结果返回）。 | 抛出（`InvalidOperationException`）/ 运行时结果 | 使用 `session.SessionId` 构建命令。 |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime`（`world.failed` 事件的 `code`） | 世界启动后无法应用启动天气：OMSI 没有为 `/weather-real` 配置 ICAO 站点，或其天气对象不可用。会话失败。 | 会话失败 | 在 OMSI 中配置一个站点，或使用 `/weather-icao`。 |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | `weather.set` 的某个键所指的值是 OMSI 在每个天气刻都会推导的值（湿度、光照系数、云透明度、降水标志、潮湿地面）；不会写入任何内容。 | 运行时结果 | 设置 `detail` 中指明的源键（例如用 `dew_point` 代替 `relative_humidity`）。 |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | 未知的 `weather.set` 键；不会写入任何内容。 | 运行时结果 | 使用[能力](capabilities.md#weather)中列出的键。 |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在字符串变量表中。 | 运行时详情 | 先列出字符串变量。 |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | 某个天气布尔值不是 `true`/`false`/`1`/`0`。目前无法触发（见上文）。 | 运行时详情（已定义） | 参见源代码。 |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`、`OmsiCameraWriter`、`OmsiCameraLockWriter`、`OmsiRuntimeReaders`、`OmsiWeatherWriter` | `time.set`：`hour` 0..23、`minute` 0..59、`second` 0..59.999；`camera.set`：`family` 0..3、`field_of_view` 10..170；`camera.lock`：`preset` 不是整数、`family` 0..3、`preset` 0..255；`road-vehicles.place-random`：`ai_type` 0..255、`group`/`type`/`tour`/`line` 0..65535（`type` 可以为 -1）、`scheduled` 0..1；`vehicle.variable.set`：`value` 不是有限值；`vehicle.curve.evaluate`：`x` 不是有限值。 | 运行时详情 | 使用范围内的值。 |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` 不在数值变量表中。 | 运行时详情 | 先列出变量。 |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | 变量槽或值地址为 null。 | 运行时详情 | 该变量未在此车辆上实例化。 |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | 时钟标量已写入，但已建立构建配置的原生 SetTime 调用返回失败。 | 运行时详情 | 重试；用 `time.read` 读回。 |

## RuntimeD3D

均由 `CurrentRuntimeControl` 引发（原生状态的 `ThrowD3D` 映射；`detail` 携带操作和 HRESULT，`native_status` 携带数值状态）。途径：运行时结果，代码位于 `ErrorCode` 中；`D3DRuntimeApi` 将其作为 `OmsiRuntimeException` 重新抛出。

| 代码 | 原生状态 / 原因 | 处理方法 |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8：Direct3D 设备丢失。 | 等待 `d3d.restored`；重新创建纹理（代次已改变）。 |
| `OL_E_D3D_INVALID_ARGUMENT` | 14：`width`、`height`、`level`、`x`、`y`、`format` 或 `handle` 缺失或无效（范围：width/height 1..4096，levels 0..16，level 0..15，x/y 0..4095）。 | 修正参数。 |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` 不是有效的 Base64，或超过 48 KiB。 | 发送更小的矩形。 |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6，或未知的 `format` 名称（有效值：`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）。 | 使用列出的格式。 |
| `OL_E_D3D_NATIVE_CALL_FAILED` | 任何其他状态；HRESULT 位于 `detail` 中。 | 附上 HRESULT 进行报告。 |
| `OL_E_D3D_NOT_READY` | 7：设备尚未就绪（第一帧之前或正在停止时）。 | 在 `d3d.ready` 之后重试。 |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9：设备重置正在进行。 | 在 `d3d.restored` 之后重试。 |
| `OL_E_D3D_RESOURCE_RELEASED` | 13：纹理句柄已被释放。 | 不要重用已释放的句柄。 |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12：句柄属于之前的设备代次；或句柄字符串不是 `d3dtex-<session>-<hex>` / 为零。 | 重新创建纹理。 |

## Session

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`；`ApplyTelemetry`，收到 `plugin.request.unsupported` 时 | 规划：请求了 `LastMapState`、`EntrypointIdentity`、日期/时间/年份模式、天气模式、玩家车辆字段或输入文档（`Requested capability unavailable: <name>`）。遥测：插件拒绝了交接（对于可运行的计划不会发生）。 | 计划诊断信息；会话诊断信息 | 移除不受支持的请求（[已知限制](known-limitations.md)）。 |
| `OL_E_HEADLESS_ARM_FAILED` | `ApplyTelemetry`，收到 `headless.arm.failed` 时 | 插件无法在 OMSI 中挂载一次性的无头启动钩子。 | 会话诊断信息（`Failed`） | 验证版本；报告。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI 客户端（`ReportForwarded`、`CliEventWatch`） | 该安装的控制管道上没有所有者应答（没有会话，或所有者仍在启动/校验中）。 | CLI 信封，退出码 4 | 启动一个会话，或等待其进入 `Running`。 |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | 在 `plugin.started` 之前 `StartupTimeoutSeconds` 已耗尽（OMSI 未加载 `plugins\OmsiLaunch.Plugin.opl`，或在插件初始化之前卡住）。 | 会话诊断信息（`Failed`） | 检查插件闭包、`plugins\OmsiLaunch.Plugin.opl` 和 OMSI 的 `logfile.txt`。 |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `ApplyTelemetry`，收到 `plugin.handoff.invalid` 或无法解析的遥测 JSON 时 | 插件无法读取/验证启动交接（版本 3 至 6，SHA-256），或发送了无效的遥测。 | 会话诊断信息（`Failed`） | 使主机与插件版本一致（重新安装该包）。 |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | 已有一个所有者为此安装应答 `session.status`。 | CLI 信封，退出码 7 | 使用客户端命令（`session status`、`session stop`、运行时命令）。 |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | 会话状态不是 `Running`。 | 抛出（`InvalidOperationException`） | 先调用 `WaitForAsync(session, SessionState.Running, ...)`。 |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | 无法构建启动画面/ITX 计划；消息携带呈现类代码。 | 计划诊断信息 | 参见 [Presentation](#presentation)。 |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure`（OmsiLaunchW 对话框） | 当启动计划不可运行或会话未进入游戏过程，且不存在 `OL_E_` 诊断信息时显示的后备代码。 | 仅消息框 | 阅读 `.omsilaunch\diagnostics`。 |
| `OL_E_SITUATION_LOAD_FAILED` | `ApplyTelemetry`，收到 `world.situation.failed` 时 | 原生的已保存情景启动返回失败（事件中含 `native_status`）。 | 会话诊断信息（`Failed`） | 检查该 `.osn` 及其地图。 |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | 插件已启动，但未在 `StartupTimeoutSeconds` 内进入 `Running`。 | 会话诊断信息（`Failed`） | 对于大型地图，增大 `/startup-timeout`；检查 `RuntimeEvents` 中的最后一个世界事件。 |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | 启动路径中的任何异常；消息即内部消息（通常以内部代码开头）。 | 会话诊断信息（`Failed`） | 根据内部代码处理。 |
| `OL_E_WORLD_START_FAILED` | `ApplyTelemetry`，收到 `world.failed` 时 | 原生 NEW_MAP 启动返回失败（事件中含 `native_status`）。 | 会话诊断信息（`Failed`） | 检查地图、入口点索引和 OMSI 的日志。 |

## SessionProfile

均由 `SessionProfileCompiler`（`src/OmsiLaunch.Core/SessionProfiles.cs`）或 `CliInput` 引发，以 `SessionProfileException`（带有 `Code` 的 `IOException`）抛出，CLI 退出码 2。参见[会话配置档](session-profiles.md)。

| 代码 | 含义 / 典型原因 | 处理方法 |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | 预设的启动画面 `assets` 目录或网络纹理 `profile` 文件在包内不存在。 | 添加该资源。 |
| `OL_E_SESSION_PROFILE_INVALID` | 结构或限制违规：超过 256 KiB、根映射不是恰好一个、使用了 YAML 锚点、未知键、缺少必需键、需要标量处为非标量、`id` 与目录名不同、预设不在 1..5 范围内或 `index` 重复、超时不是正数、不受支持的天气/启动画面/网络纹理模式、日期/时间不是 `explicit`、无效的 YAML、数字/日期解析错误。 | 根据消息修正 YAML。 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` 非空，且不包含所选地图（NEW_MAP）或所选情景的地图（SAVED_SITUATION）。 | 选择兼容的世界。 |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 不存在。 | 检查 ID。 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 某个显式 CLI 参数指向了所选配置档/预设所拥有的字段。 | 去掉该参数或选择其他预设。 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | ID 包含 `\`、`/`、`:` 或 `..`；或者某个资源路径是带根路径、逃逸出包，或穿越了联接点/符号链接。 | 使路径保持在包内。 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` 缺失、超出 1..99，或未由该配置档声明。 | 使用已声明的预设索引。 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` 不是 `omsilaunch.session-profile/v1`。 | 使用受支持的 schema。 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | 某个预设设置项键已知但不可写。 | 删除该键。 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | 某个预设设置项键不在目录中。 | 使用目录中的键。 |

## Transaction

参见[事务与恢复](../concepts/transactions-and-recovery.md)。

| 代码 | 引发位置 | 含义 / 典型原因 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | 执行 `File.Delete` 之后，过期的 `closecheck` 仍然存在。 | 通过 `OL_E_START_SESSION` 作为会话诊断信息 | 手动删除 `<root>\closecheck`（权限问题）。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | 某个在会话之前不存在的路径现在含有与会话所应用内容不同的内容；它不会被删除，事务日志会被保留。 | 抛出（`IOException`）；出现在 `OL_E_RESTORE_FAILED` / `OL_E_START_SESSION` 中；CLI 退出码 8 | 检查该文件；删除或移走它，然后执行 `/recover`。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync`（指纹机制之前的事务日志） | 对于一个原本不存在且非删除类的路径，事务日志中没有已应用内容的指纹，因此无法证明所有权。在会话启动时，恢复会被推迟，并使用本次会话计划的字节重试；通过 `RecoverPendingAsync` 调用时则会抛出。 | 抛出（`IOException`）；CLI 退出码 8 | 使用相同的 spec 启动会话（以提供这些字节），或检查并删除该文件，然后执行 `/recover`。 |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | 某个备份的 SHA-256 与事务日志中记录的快照不一致；不会写入任何内容。 | 抛出；出现在 `OL_E_RESTORE_FAILED` 中；CLI 退出码 8 | 从您自己的备份中还原该文件；只有在确定无误时才删除事务日志。 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | 内存中存在快照，但 `journal.json` 已不存在（在会话期间被删除）。 | 抛出；出现在 `OL_E_RESTORE_FAILED` 中 | 手动核实会话文件。 |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | 删除后 `journal.json` 仍然存在（还原本身已成功并经过校验）。 | 抛出；出现在 `OL_E_RESTORE_FAILED` 中；CLI 退出码 8 | 删除 `<root>\.omsilaunch\journal.json`（权限问题），或再次运行 `/recover`（幂等）。 |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService`（启动失败 / 监管器） | 无法确认 OMSI 已退出，因此在它可能仍在使用这些文件时没有替换它们；事务日志被保留。 | 会话诊断信息（`Failed`） | 在 `Omsi.exe` 退出后运行 `/recover`（或由下一次启动自动恢复）。 |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService`（启动失败 / 监管器） | `RestoreAsync` 抛出异常；消息携带内部代码；事务日志被保留。 | 会话诊断信息（`Failed`）；从 `/recover` 抛出时 CLI 退出码 8 | 根据内部代码处理，然后执行 `/recover`。 |

## Warning

| 代码 | 引发位置 | 含义 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | 该 OMSI 2 可执行文件没有已知布局（其代码标识不在版本目录中）。计划仍可运行：每项原生能力都在进程内解析并校验，无法校验的能力不可用。 | 计划诊断信息（不影响可运行性） | 无需处理；请查看 `/plan` 和 `profiles` 了解预测的能力。 |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService`（监管器） | 请求了 `Behavior.CooperativeShutdown`，但无法让 OMSI 退出（其退出例程在此版本上未解析，或 OMSI 已在关闭），或它没有在 `ShutdownTimeoutSeconds` 内退出：改为终止了 OMSI。还原照常执行。 | 会话诊断信息 | 无需处理：会话以强制终止的方式结束。 |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | 启动所需的某项能力无法从可执行文件中解析（例如受 DRM 保护的可执行文件，其已加载映像与文件不同）。它会在启动时于进程内解析。 | 计划诊断信息（不影响可运行性） | 无需处理；若在进程内也无法解析，启动会以 `OL_E_CAPABILITY_UNRESOLVED` 失败。 |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | 自行加入会话的模块（设置文件、内容或默认激活）无法加载（`Data["code"]` 说明原因，`Data["activation"]` 说明其加入方式）。 | 计划诊断信息（不影响可运行性） | 修复、禁用或移除该模块。 |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`、`ModuleSettingsFile`、`ModuleSettings` | `.omsilaunch\modules.ini` 的某个条目无法使用（不是模块段、位于段之外、未知键、值无效；将使用默认值），或为会话未加载的模块请求了设置。 | 计划诊断信息（不影响可运行性） | 更正该条目或请求。 |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | 某个会话删除类路径（ITX 目标、`Texture\standard.ipr`、`closecheck`）在会话之前不存在、现在存在，但在此事务日志下从未启动过 OMSI 进程，因此该文件不可能是会话的副产物。它会被保留并报告（消息 = 相对路径，`Data["sha256"]`）；事务仍会完成。 | 会话诊断信息 / `RecoveryStatus.Diagnostics`（不影响状态） | 检查该文件；如果不需要，请自行删除。 |

## Module

运行时模块（参见[运行时模块](runtime-modules.md)）。规划阶段，代码是计划诊断信息：对于被请求的模块，它会使计划不可运行；对于自行加入的模块（设置文件、内容、默认），计划会改为报告 `OL_W_MODULE_NOT_LOADED`。在会话中，代码是会话诊断信息（除非代码为 `OL_E_MODULE_SESSION_FAILED`，否则会话继续），对于模块操作则是运行时结果。

| 代码 | 引发位置 | 含义 | 途径 | 处理方法 |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`、`ModuleCatalog` | `module.json` 缺失、不是有效的 `omsilaunch.runtime-module/v1`，或其 `id` 与目录名不同。消息会指明字段。 | 计划诊断信息；`ListRuntimeModulesAsync` | 修正清单。 |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | 所请求的模块 ID 未安装在 `plugins\OmsiLaunch.Modules` 之下。 | 计划诊断信息 | 检查 `/list:RuntimeModules`。 |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`、`ModuleHost` | 某个固定的文件缺失、与其固定的 SHA-256 不同，或是通过重解析点访问的；或在进程内，`module.json` 在规划之后发生了变化。 | 计划诊断信息；会话诊断信息 | 重新安装该模块。 |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | 模块的 `bridge_protocol` 范围不包含此版本所提供的任何协议（1 和 2）。 | 计划诊断信息 | 使用为此 OmsiLaunch 制作的模块版本。 |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`、`ModuleHost` | 某项必需的能力对此版本未知、在此安装上不可用，或未在进程内解析。 | 计划诊断信息；会话诊断信息 | 参见[能力](capabilities.md)和 `profiles`。 |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`、`ModuleHost` | 会话没有运行模块所绑定的地图，或某个绑定的文件与其固定的 SHA-256 不同。 | 计划诊断信息；会话诊断信息 | 使用绑定的内容启动绑定的地图。 |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | 模块请求启动的文件不是它的辅助组件之一。 | 模块内部的运行时结果 | 模块缺陷。 |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`、`ModuleSessionPrograms` | 无法加载托管组件或创建其入口类型；或模块的会话程序无法启动，或在就绪之前就退出了。 | 会话诊断信息 | 检查模块的构建（与 x86 兼容，入口类型实现了 `IRuntimeModule`）。 |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`、`GameplayReady` 或 `Stop` 抛出了异常。 | 会话诊断信息 | 模块缺陷；已应用其 `failure_policy`。 |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`、`ModuleSessionPrograms` | 某个阶段或某个操作在其预算（`timeouts`、`timeout_ms`）之后才返回，或会话程序没有在 `prepare_ms` 内就绪。 | 会话诊断信息；运行时结果 | 模块缺陷；已应用其 `failure_policy`。 |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | 该模块已被禁用，或在会话中较早时失败。 | 运行时结果 | 第一次失败请参见会话诊断信息。 |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | 会话没有加载该模块，或它尚未就绪（在进入游戏之前）。 | 运行时结果 | 在启动时请求该模块；等待 `Running`。 |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | 该模块没有声明这样的操作。 | 运行时结果 | 检查模块的 `operations`。 |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | 操作失败，且没有已编入目录的代码；模块自己的代码位于 `Values["module_code"]`。 | 运行时结果 | 视模块而定。 |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | 停止或失败之后，某个被跟踪的资源仍然存活（未退出的辅助进程、未结束的后台线程），或其释放过程抛出了异常。 | 会话诊断信息 | 向模块方报告；无论如何，该进程都会随 OMSI 结束。 |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | `failure_policy` 为 `fail-session` 的模块失败；会话被停止并还原。 | 会话诊断信息（状态 `Failed`） | 参见前面的模块代码。 |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`、`ModuleSettings`、CLI | 请求的模块设置无效或未由该模块声明，某个模块同时被启用和禁用，设置名称不是 `<module-id>.<key>`，或某个模块参数由多个模块声明。 | 计划诊断信息（不可运行）；CLI 错误 | 更正请求；`/list:RuntimeModules` 会显示每个模块的设置。 |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`、`ModuleHost` | 模块在 `conflicts.plugins` 下列出的某个 OMSI 插件已被启用（`plugins\*.opl`），或已在 OMSI 中加载。原因会指明 `.opl`。 | 计划诊断信息；会话诊断信息 | 关闭或移除该插件。OmsiLaunch 绝不会更改它。 |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | 保留 | 保留；原生组件会改为收到[运行时模块](runtime-modules.md#native-components-and-hooks)中描述的数值钩子结果。 | 无 | 无 |

<a id="non-error-diagnostic-codes"></a>
## 非错误诊断码

| 代码 | 发出者 | 消息 / 数据 | 含义 |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | 消息 = PID；`Data["thread_id"]`、`Data["creation_utc"]`（ISO 8601） | `Omsi.exe` 已创建，其标识已记录。会话诊断信息。 |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | 消息 = 被删除文件的 SHA-256 | 会话之前已存在的 `closecheck` 被永久删除（`SuppressStaleClosecheckWarning = true`）。会话诊断信息。 |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | 消息 = 相对路径；`Data["sha256"]` | 在进程已启动的会话期间，某个会话删除类路径被 OMSI 重新创建；为还原其原本不存在的状态，它已被删除。会话诊断信息 / `RecoveryStatus.Diagnostics`。 |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | 消息 = `manifest` 或 `self` | 永久插件校验所使用的参考。计划诊断信息。 |
| `module.planned` | `SessionPlanner` | 消息 = 模块 ID；`Data["module"]`、`Data["status"]`、`Data["activation"]` | 该模块在计划中。计划诊断信息。 |
| `module.disabled` | `SessionPlanner` | 消息 = 模块 ID | 某个内容模块或默认模块因被禁用（请求或设置文件）而被排除在外。计划诊断信息。 |
| `session_profile.selected` | `SessionPlanner` | 消息 = 配置档 ID；`Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | 由会话配置档编译而成的会话的来源信息。计划诊断信息。 |

运行时事件名称（`RuntimeEvent.Type`，不属于诊断信息）列于[会话生命周期](../concepts/session-lifecycle.md#telemetry-events)中。
