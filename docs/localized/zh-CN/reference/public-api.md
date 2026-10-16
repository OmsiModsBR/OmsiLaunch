# 公共 API 参考（`OmsiLaunch.Api`）

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/public-api.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 OmsiLaunch 0.1.0-beta4 托管公共 API 的规范性参考：包括 `OmsiLaunch.Api` 程序集（契约）以及 `OmsiLaunch.Core` 中面向集成方的入口点 `OmsiLaunchService`。本页只记录当前代码的实际行为。集成方可以调用、接收或观察到的一切内容都连同其稳定性级别列于此处；未列出的内容均不属于集成接口。

自动生成的[公共 API 清单](public-api-inventory.md)列出了 `OmsiLaunch.Api`、`OmsiLaunch.Core` 和 `OmsiLaunch.Process` 的每个公共类型和成员及其签名与稳定性；当清单与程序集不一致时，文档门禁会失败。本页负责解释语义。

相关页面：[LaunchSpec 参考](launchspec.md)、[错误码](errors.md)、[会话生命周期](../concepts/session-lifecycle.md)、[事务与恢复](../concepts/transactions-and-recovery.md)、[运行时控制](runtime-control.md)、[能力](capabilities.md)、[本地控制平面](local-control.md)、[退出码](exit-codes.md)、[运行时验证状态](../status/runtime-validation-status.md)。

<a id="stability-vocabulary"></a>
## 稳定性术语

| 级别 | 在本页中的含义 |
| --- | --- |
| `STABLE_BETA` | 契约在 0.1 协议线内冻结，且该路径已在 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md` 中经运行时验证。 |
| `EXPERIMENTAL` | 可调用且经过测试，但在成为稳定版之前，契约或运行时证据可能发生变化。 |
| `PARTIAL` | 存在于契约中；只有部分行为已实现或已验证（正文会说明是哪一部分）。 |
| `INTERNAL` | 出于技术原因在程序集中为 public（桥两端共享该类型），但不是集成接口；可能在不另行通知的情况下变更。 |
| `UNAVAILABLE` | 存在于契约中，但被当前版本（build）拒绝。 |

<a id="assembly-overview"></a>
## 程序集概览

| 程序集 | 对集成方的作用 |
| --- | --- |
| `OmsiLaunch.Api` | 纯契约：记录（record）、枚举、`IOmsiLaunch`、能力注册表、错误目录、线路格式（wire format）、D3D 辅助方法。不包含任何 `IntPtr`、`nint`、Win32 句柄、原生地址或进程对象。 |
| `OmsiLaunch.Core` | `OmsiLaunchService`（`IOmsiLaunch` 的实现）、`OmsiLaunchRuntimePaths`、`SessionPlanner`、`LaunchValidation`、`SessionProfileCompiler`。 |
| `OmsiLaunch.Process` | `IRuntimePlatform` 和 `CurrentWindowsX64Platform`（唯一的平台适配器）、`InstallationLease`。构造服务时需要。 |
| `OmsiLaunch.Configuration`、`OmsiLaunch.Content`、`OmsiLaunch.Interop`、`OmsiLaunch.Plugin`、`OmsiLaunch.Builds.Omsi23004` | 实现程序集。其公共类型对集成方而言属于 `INTERNAL`。 |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## 入口点：`OmsiLaunchService` 和 `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| 参数 | 有效值 | 无效值 / 默认值 |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()`（命名空间 `OmsiLaunch.Process`）。它负责检测平台、使用 `CreateProcessW` 创建 OMSI 进程、等待并终止该进程。 | 不随附其他实现。自定义 `IRuntimePlatform` 属于 `INTERNAL`。 |
| `PluginBuildDirectory` | 包含永久插件闭包参考文件的目录：`OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json` 以及托管闭包中的每个 `OmsiLaunch.*.dll`（必须包含 `OmsiLaunch.Plugin.dll`）。在已安装的包中，该目录为 `<package>\plugins`。 | 目录或文件缺失：`PlanSessionAsync` 返回带有 `OL_E_RUNTIME_ARTIFACT_MISSING` 的不可运行计划。 |
| `NativeBridgePath` | `OmsiLaunch.Native.x86.dll` 的路径（在包中为 `<package>\plugins\OmsiLaunch.Native.x86.dll`）。 | 同上。 |
| `ReleaseManifestPath` | 存在时为 `OmsiLaunch.exe` 旁边的 `release-manifest.json`。它提供每个 `plugins/` 文件的预期 SHA-256（`plugin.integrity.reference = manifest`）。 | `null`（开发布局）：仅检查已安装文件是否存在，以及相对于参考闭包是否自洽（`plugin.integrity.reference = self`）。清单格式错误：`OL_E_RELEASE_MANIFEST_INVALID`。 |

服务在每次 `PlanSessionAsync` 和 `StartSessionAsync` 时读取这些路径；它从不复制、暂存或删除插件文件（参见[永久插件](../concepts/permanent-plugin.md)）。CLI 构造服务的方式与此完全相同（`tools/OmsiLaunch.Cli/Program.cs`）：

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

每个进程创建一个服务并共享使用。稳定性：`STABLE_BETA`。

<a id="session-ownership-rules"></a>
## 会话所有权规则

| 规则 | 详情 |
| --- | --- |
| 每个安装实例只有一个所有者 | `StartSessionAsync` 获取安装租约——一个命名信号量 `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`（名称后缀为转为大写的完整根路径的 SHA-256），并一直持有，直到监管器还原安装实例为止。同一登录会话中的任何进程对同一根目录发起第二次启动，都会以 `OL_E_INSTALLATION_BUSY` 失败（以 `Failed` 会话的形式报告，参见 `StartSessionAsync`）。租约按登录会话划分，不跨登录会话生效；并且在另一个进程持有其句柄期间不会被释放（已接受的风险）。 |
| 句柄是进程本地的 | `SessionHandle` 封装会话 `Guid`。它只对返回它的那个 `OmsiLaunchService` 实例有意义。在另一个进程（或另一个服务实例）中用已知的 `Guid` 构造的句柄会产生 `KeyNotFoundException`。跨进程控制通过[本地控制平面](local-control.md)进行，而不是通过句柄。 |
| 始终调用 `CloseAsync` | 从 `StartSessionAsync` 开始，进程就拥有一个持久事务。`CloseAsync` 在需要时请求规范停止，等待监管器完成（进程退出、精确还原、租约释放），然后移除该会话。必须在每条退出路径上调用它，包括进入 `Failed` 状态之后。若不调用，会话条目会一直留在内存中；但无论如何，还原本身都由监管器执行。 |
| 失败的会话仍然是会话 | 在 `StartSessionAsync` 返回之后才失败的启动会报告 `SessionState.Failed`；在调用 `CloseAsync` 之前，该句柄对 `GetStatusAsync`/`WaitForAsync` 仍然有效。 |
| 计划会被重新检查 | `StartSessionAsync` 会重新计算 `Omsi.exe` 的哈希并重新规划 spec；已不再可运行的计划会以 `OL_E_PLAN_NOT_RUNNABLE` 被拒绝。 |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

适用于所有方法的共同事实：

- 未知或已关闭的句柄会抛出 `KeyNotFoundException`（“Unknown OmsiLaunch session.”）。
- 除 `ExecuteRuntimeAsync` 外，任何方法都不要求会话处于 Running 状态。
- 携带 OmsiLaunch 代码的异常会将该代码放在 `Exception.Message` 的开头（`"OL_E_PLAN_NOT_RUNNABLE: ..."`）。CLI 以同样的方式从消息中提取代码（`CliProgram.Classify`）。
- 支持的安装：OMSI 2。原生能力在进程内按版本（build）分别解析并校验；在某个版本上不可用的能力会以 `OL_E_CAPABILITY_UNRESOLVED` 失败。参见[兼容性](compatibility.md)。

<a id="complete-minimal-example"></a>
### 完整的最小示例

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 用途 | 在不启动 OMSI 的情况下将 `LaunchSpec` 编译为 `SessionPlan`：校验 spec、检测平台、计算 `Omsi.exe` 的指纹、解析内容标识、计算计划中的文件变更、列出所需能力和不受支持的能力，并确定 `IsRunnable`。公共能力 `session.plan`。 |
| 参数 | `spec`：完整填充的 `LaunchSpec`（参见 [LaunchSpec 参考](launchspec.md)）。`Installation`、`World`、`Date`、`Time`、`Environment`（全部八个字典）和 `Behavior` 必须非 null；可选成员可以为 `null`。`RootPath` 应为绝对目录；空根目录会被记录为 `OL_E_INSTALLATION_NOT_FOUND`，但对空路径进行的平台探测会在计划返回之前抛出 `ArgumentException`，因此切勿传入空根目录。 |
| 返回值 | `SessionPlan`，包含新的 `SessionId`、`BuildProfileId` = 已知布局（`"Omsi23004"`），对于没有已知布局的 OMSI 2 版本（build）则为 `"unrecognized"`、输入的 `Spec`、`Platform`、`ResolvedContent`、`TouchedFiles`、`RuntimeArtifacts`（`plugins\OmsiLaunch.*` 目标路径以及 `"OmsiLaunch startup handoff v6"`）、`RequiredCapabilities`、`UnsupportedRequestedFeatures`、`PlannedMutations`、`Diagnostics`、`IsRunnable`、`PlannedModules`（每个被请求或由内容激活的[运行时模块](runtime-modules.md)对应一个 `PlannedModule`；访问器 `EffectivePlannedModules`）。当且仅当没有任何诊断信息代码以 `OL_E_` 开头时，`IsRunnable` 为 `true`。信息性诊断信息（消息为 `self` 或 `manifest` 的 `plugin.integrity.reference`、`session_profile.selected`）永远不会使计划变为不可运行。 |
| 结果携带的错误 | 每个规划错误都是诊断信息，而不是异常：`OL_E_INSTALLATION_NOT_FOUND`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_DATE_TIME_APPLY_FAILED`、`OL_E_INVALID_ARGUMENT`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_SESSION_PRESENTATION_INVALID`（消息中携带启动画面/ITX 代码）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（在规划时，会根据发行清单校验 `plugins\` 中已安装的插件闭包）、`OL_E_RUNTIME_ARTIFACT_MISSING`（消息中可能携带 `OL_E_RELEASE_MANIFEST_INVALID`）。完整条件：[LaunchSpec 校验规则](launchspec.md#validation-rules-and-non-runnable-diagnostics)。 |
| 抛出的异常 | 若令牌在进入时已被取消，则抛出 `OperationCanceledException`（唯一的检查点）；语法无效的根路径抛出 `ArgumentException`/`NotSupportedException`；必需成员为 null 时抛出 `NullReferenceException`/`ArgumentNullException`；语法无效的发行清单抛出 `System.Text.Json.JsonException`。 |
| 取消 | 仅在进入时检查一次。之后的规划是同步的文件系统操作。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI 状态 | 否。 |
| 是否修改文件系统 | 否（读取 `Omsi.exe`、内容文件、插件闭包、清单）。此处不校验设置项的值（只校验键是否存在以及是否可写）；无效值会在启动时以 `OL_E_INVALID_SETTING_VALUE` 失败。 |
| 事务 / 还原 | 无。 |
| 限制 | 在此版本（build）上，请求 `Date`/`Time`/`Year` 中任意一个为 `Unset` 以外的模式、`Weather` 为 `Unset` 以外的任何模式、任何 `PlayerVehicle` 字段、`Input` 文档、`EntrypointIdentity` 或 `WorldMode.LastMapState`，都会产生 `OL_E_CAPABILITY_UNAVAILABLE` 和不可运行的计划（`UnsupportedRequestedFeatures` 中带有其目录证据 `UNAVAILABLE` 或 `PARTIAL` 的条目）。 |
| 稳定性 | `STABLE_BETA`。 |
| 示例 | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 用途 | 根据可运行的计划启动一个事务性托管 OMSI 会话：获取安装租约、恢复失效的事务日志（journal）、校验永久插件闭包、为会话文件创建快照并应用覆盖层（overlay）、创建启动交接（handoff）、遥测槽和运行时邮箱（mailbox）、启动 `Omsi.exe`、将进程记录到事务日志中，并将会话移交给后台监管器。公共能力 `session.start`。 |
| 参数 | `plan`：满足 `IsRunnable == true` 的 `SessionPlan`。计划中的 spec 会被重新规划；调用方计划中只有 `plan.SessionId` 会被保留。`plan.Spec.Behavior.StartupTimeoutSeconds` 必须在 1..600 范围内。 |
| 返回值 | 一旦 `Omsi.exe` 已创建并记录（状态 `WaitingForPlugin`），或一旦启动路径失败（状态 `Failed`），即返回 `SessionHandle(plan.SessionId)`。它不会等待进入游戏；请使用 `WaitForAsync(session, SessionState.Running, ...)`。 |
| 抛出的异常 | `plan.IsRunnable` 为 false 时抛出 `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`；重新规划的结果不可运行时（例如 `Omsi.exe` 已更改、内容已删除、插件闭包缺失）抛出 `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`；具有相同 id 的会话仍处于注册状态时抛出 `InvalidOperationException("Duplicate session id.")`（请先调用 `CloseAsync`）；`StartupTimeoutSeconds` 超出 1..600 时抛出 `ArgumentOutOfRangeException`；在重新规划之前或期间被取消时抛出 `OperationCanceledException`；另外还包括 `PlanSessionAsync` 可能抛出的所有异常。在所有抛出异常的情况下，都不会注册会话。 |
| 结果携带的错误 | 重新规划之后的任何失败都会在启动路径内部被捕获：会话被注册，其状态为 `Failed`，其诊断信息包含 `OL_E_START_SESSION`，其消息为内部消息（存在内部代码时以该代码开头）：`OL_E_INSTALLATION_BUSY`（租约被占用，或事务日志中记录的 OMSI 进程仍然存活）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`（仅当使用本会话覆盖层进行的延迟重试仍无法证明所有权时）、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`、`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。清理过程可能追加 `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`（未确认 OMSI 已退出；保留事务日志）或 `OL_E_RESTORE_FAILED`。之后发生的失败由监管器报告（参见[会话生命周期](../concepts/session-lifecycle.md)）。 |
| 取消 | 在重新规划之前或期间：抛出异常。此后令牌会传递给事务和进程创建；在那里发生的取消与任何启动失败的处理方式相同（`Failed` + `OL_E_START_SESSION: The operation was canceled.`），进程（如已创建）会被终止，安装实例会被还原。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI 状态 | 是：使用环境变量 `OMSILAUNCH_SESSION_ID`、`OMSILAUNCH_HANDOFF_NAME`、`OMSILAUNCH_TELEMETRY_NAME`、`OMSILAUNCH_RUNTIME_CHANNEL`、`OMSILAUNCH_INTERNET_TEXTURES_MODE` 创建 OMSI 进程。 |
| 是否修改文件系统 | 是，在安装根目录内：`.omsilaunch\diagnostics\<sessionId>-host.log`（保留最新的 50 个会话）、`.omsilaunch\journal.json`、`.omsilaunch\backup\<sessionId>\*.bin`、`.omsilaunch\assets\splash\*.bmp`（为托管启动画面复制一次）、会话覆盖层（`options.cfg` 补丁、`GUI\NewSplashscreen_*.bmp`、`Texture\standard.itx`）、会话删除项（ITX 目标、`Texture\standard.ipr`、`closecheck`），以及当 `SuppressStaleClosecheckWarning` 为 true 时永久删除预先存在的失效 `closecheck`（诊断信息 `closecheck.stale-removed`）。 |
| 事务 / 还原 | 开启事务（`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`）。离开会话的每条路径都以还原结束。参见[事务与恢复](../concepts/transactions-and-recovery.md)。 |
| 限制 | 只有带 `PresentedEntrypointIndex` 的 `WorldMode.NewMap` 和 `WorldMode.SavedSituation` 能够进入游戏。`WorldMode.LastMapState` 为 `UNAVAILABLE`。玩家车辆请求永远不会到达此方法，因为它们在规划时就是不可运行的。 |
| 稳定性 | `STABLE_BETA`（NEW_MAP 和 SAVED_SITUATION 生命周期已经运行时验证）。 |
| 示例 | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 读取语义生命周期状态、迄今收集的诊断信息以及有界的运行时事件列表。公共能力 `session.status`。 |
| 参数 | `session`：由 `StartSessionAsync` 返回且尚未关闭的句柄。 |
| 返回值 | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`：一个不可变快照（数组在会话锁内复制）。对于存活的会话，`RuntimeEvents` 永远不为 `null`。 |
| 抛出的异常 | 未知/已关闭的句柄抛出 `KeyNotFoundException`。除此之外从不抛出异常。 |
| 取消 | 忽略令牌（调用同步完成）。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI / 文件系统 / 事务 | 否 / 否 / 无。 |
| 稳定性 | `STABLE_BETA`。 |
| 示例 | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 用途 | 轮询（每 100 ms 一次），直到会话处于 `state`、处于终止状态（`Completed`、`Failed`）或超时为止；然后返回当前状态。 |
| 参数 | `state`：任意 `SessionState`。等待一个已经经过的瞬时状态（或从不设置的状态，参见[会话生命周期](../concepts/session-lifecycle.md)）时，会一直等到终止状态或超时。`timeout`：任意非负的 `TimeSpan` 或 `Timeout.InfiniteTimeSpan`。 |
| 返回值 | 等待结束那一刻的状态。超时时返回状态而不是抛出异常：请自行检查 `State`。等待 `Running` 但以 `Failed` 结束时，会立即返回并附带失败诊断信息。 |
| 抛出的异常 | `KeyNotFoundException`；调用方的令牌被取消时抛出 `OperationCanceledException`（只有调用方的取消会传播；内部超时不会）。 |
| 取消 | 在每个 100 ms 周期检查调用方令牌。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI / 文件系统 / 事务 | 否 / 否 / 无。 |
| 稳定性 | `STABLE_BETA`。 |
| 示例 | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 请求规范停止。它设置停止标志后立即返回；监管器在其 100 ms 循环内观察到该标志，对 `Omsi.exe` 调用 `TerminateProcess`，等待其退出，将事务日志标记为 `ProcessExited`，还原每个会话所拥有的文件并释放租约。这是一次强制终止：OMSI 自身的关闭例程不会运行，OMSI 也不会在退出时重写 `options.cfg`（有意为之，用于保护事务）。基于 `WM_CLOSE` 的协作式关闭未实现（产品决策；在运行时收尾验证中，OMSI 在收到 `WM_CLOSE` 后 30 s 内未关闭，`L05b`）。公共能力 `session.stop`。 |
| 参数 | `session`。 |
| 返回值 | 已完成的任务；不等待终止或还原。请使用 `WaitForAsync(session, SessionState.Completed, ...)` 观察完成情况。 |
| 抛出的异常 | `KeyNotFoundException`。 |
| 取消 | 忽略令牌。 |
| 是否需要运行中的会话 | 否。幂等；在监管器启动之前请求的停止会在其启动后立即得到执行；对处于终止状态的会话发出停止请求不执行任何操作。 |
| 是否修改 OMSI 状态 | 是：终止 OMSI 进程（退出码 1）。 |
| 是否修改文件系统 | 间接修改：触发监管器执行还原、删除事务日志和删除备份。 |
| 事务 / 还原 | 触发 `ProcessExited` → `Restoring` → `Restored`。通过 `ExecuteRuntimeAsync` 在运行时一侧所做的更改（时钟写入、生成的车辆、脚本变量、D3D 纹理）不会被还原；它们随进程一同消失。 |
| 稳定性 | `STABLE_BETA`。 |
| 示例 | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 释放使用方句柄，同时不会让事务处于悬空状态：如果会话尚未终止，则请求规范停止；然后等待监管器的生命周期任务（进程退出、还原、租约释放）；最后移除该会话。 |
| 参数 | `session`。 |
| 返回值 | 当会话已终止并被移除时完成。返回后，该句柄即为未知句柄（之后的任何调用都会抛出 `KeyNotFoundException`，包括第二次 `CloseAsync`）。 |
| 抛出的异常 | `KeyNotFoundException`；调用方在等待监管器期间取消时抛出 `OperationCanceledException`。这种情况下会话不会被移除，监管器继续运行；请再次调用 `CloseAsync`。 |
| 取消 | 只作用于等待过程；绝不会取消还原。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI 状态 | 会话仍存活时为是（与 `StopAsync` 相同）。 |
| 是否修改文件系统 | 间接修改（由监管器还原）。 |
| 事务 / 还原 | 保证在释放句柄之前将事务推进到完成（在监管器已启动的情况下）。对于在监管器启动之前就已失败的会话，启动路径已经完成了还原，或已报告 `OL_E_RESTORE_DEFERRED`。 |
| 稳定性 | `STABLE_BETA`。 |
| 示例 | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 用途 | 通过单飞（single-flight）会话邮箱（内存映射，64 KiB，请求与会话 id 和请求 id 绑定），在运行中的 OMSI 进程内执行一个公共运行时操作。插件在 OMSI 的 UI 线程上执行该操作。操作目录：[运行时控制](runtime-control.md)和[能力](capabilities.md)。 |
| 参数 | `command.SessionId` 必须等于 `session.SessionId`。`command.RequestId`：由调用方选择的 `ulong`；每个进程请使用严格递增的计数器（D3D 辅助方法从 30 000 开始，CLI 所有者从 10 001/50 000 开始）。`command.Operation`：来自 `PublicCapabilityRegistry.PublicRuntimeOperationIds` 的公共操作 id（例如 `time.read`、`road-vehicle.read`、`d3d.texture.create`）。`command.Arguments`：以序数名称为键的字符串值；每个操作所需的名称来自 `PublicCapabilityRegistry.GetRuntimeArguments`。`timeout`：从请求被暂存到邮箱的那一刻开始计时（排在另一个进行中的命令之后等待的时间不计入）。CLI 作为所有者时使用 5 s（`road-vehicles.spawn` 为 15 s），作为客户端时使用 8 s / 30 s。 |
| 检查顺序 | 1. 注册表校验（在查找会话之前）：未知操作或 `internal.*` 操作 → 结果 `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必需参数（不存在或仅含空白）→ `OL_E_RUNTIME_ARGUMENT_REQUIRED`。2. 查找会话 → `KeyNotFoundException`。3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`。4. 状态不是 `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`。5. 邮箱请求。6. 剥离键以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的结果值。 |
| 返回值 | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`。成功时 `Values` 包含该操作的语义字符串（每个操作的说明见[运行时控制](runtime-control.md)）。 |
| 结果携带的错误 | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（注册表）；`OL_E_RUNTIME_RESPONSE_TOO_LARGE`（插件结果超出邮箱容量；有界列表结果则改为以 `truncated=true` 缩短返回）；`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（带有派生键的 `weather.set`）；所有 `OL_E_D3D_*` 代码（附带 `Values["detail"]` 和 `Values["native_status"]`）；`OL_E_CAPABILITY_UNRESOLVED`（该操作的原生能力在正在运行的版本上未解析）；以及插件端每一种带类型的失败，其自身的代码位于 `ErrorCode` 中，消息位于 `Values["detail"]`（Beta 4；只有没有已编入目录代码的失败才是 `OL_E_RUNTIME_OPERATION_FAILED`）。以这种方式传递的代码：`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（插件端检查）、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE`、`OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_CONSTANT_NOT_FOUND`、`OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`、`OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_DEGENERATE`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_HOF_UNAVAILABLE`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_TIME_APPLY_FAILED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_PLACE_RANDOM_BUS_FAILED`、`OL_E_RUNTIME_SETTING_UNAVAILABLE`。参见[错误码](errors.md)。 |
| 抛出的异常 | `KeyNotFoundException`；附带 `OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_CLOSED`（邮箱已被监管器释放）、`OL_E_RUNTIME_CHANNEL_BUSY`（槽中仍有一个被放弃的请求）、`OL_E_RUNTIME_REQUEST_ID_REUSED`（槽中仍有同一请求 id 的过期响应）的 `InvalidOperationException`；`TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`；`InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")`（响应损坏、来源不明或不匹配）；序列化后的请求超出邮箱容量时抛出 `ArgumentOutOfRangeException`；`OperationCanceledException`。 |
| 取消 | 在等待每会话门控期间，以及轮询响应时每 20 ms 检查一次。在请求进行中取消不会重置槽：该会话上的下一次调用可能以 `OL_E_RUNTIME_CHANNEL_BUSY` 失败，直到插件发布其响应（该响应随后作为过期响应被丢弃）。建议优先使用超时；超时会重置槽，迟到的响应会被检测到并丢弃。 |
| 是否需要运行中的会话 | 是（`SessionState.Running`）；否则抛出 `OL_E_SESSION_NOT_RUNNING`。邮箱一直存在，直到监管器在还原期间将其释放。 |
| 是否修改 OMSI 状态 | 取决于操作：`Read` 操作不修改；`Write`/`Action` 操作（`time.set`、`camera.set`、`camera.lock`、`camera.unlock`、`road-vehicles.spawn`、`road-vehicles.place-random`、`vehicle.variable.set`、`d3d.texture.*`）会修改进程内状态，且这些状态不会被还原。 |
| 是否修改文件系统 | 宿主不进行写入。OMSI 可能因此写入其自身的文件（不跟踪）。 |
| 事务 / 还原 | 无。 |
| 限制 | 每个会话同时只有一个进行中的命令（同一会话上的调用会被串行化）。请求和响应各自限制为 64 KiB 减去 8 字节；D3D 像素负载限制为 48 KiB。`internal.road-vehicles.make-basic` 为 `INTERNAL` 且无法访问。`weather.set` 和 `weather.metar.set` 为 `EXPERIMENTAL`。`timetable.logs.read` 不是有界的，在大型时刻表上可能返回 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`。`camera.lock` 为 `EXPERIMENTAL`；它需要玩家车辆，并且已经运行时验证（`CAM01`），尽管注册表中的 `RuntimeValidation` 字符串仍为 `STATICALLY_VALIDATED`。句柄（`rv-NNNNNN`、`hb-NNNNNN`、`d3dtex-<session>-<hex>`）的作用域为会话。 |
| 稳定性 | 传输和契约为 `STABLE_BETA`；各操作的稳定性遵循 `PublicCapabilityRegistry`（`PublicStableBeta` → `STABLE_BETA`，`PublicExperimental` → `EXPERIMENTAL`），上述例外情况除外。 |
| 示例 | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 返回针对某个安装实例评估后的能力目录：`PublicCapabilityRegistry` 中每个不是 `InternalOnly` 的条目对应一个 `Capability(Name, Available, EvidenceState, Reason)`。`Name` 是目录 id，`EvidenceState` 是目录证据（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`）。`Available` 依据平台、OMSI 2 检查、分类以及该条目所需的原生能力计算得出（参见[能力](capabilities.md#getcapabilitiesasync)）。 |
| 参数 | `installation.RootPath`：被探测的安装实例（平台、`Omsi.exe`）。`ExpectedExecutableSha256` 被忽略。 |
| 返回值 | 每个非内部目录条目一个条目，例如 `time.read`（`RUNTIME_VALIDATED`，在已知布局上 `Reason` 为 `native: RESOLVED_KNOWN_LAYOUT`）、`world.last-map-state`（`false`、`UNAVAILABLE`）。 |
| 与 `PublicCapabilityRegistry` 的关系 | 由它派生：注册表声明能力、其操作及其证据；此调用针对某个安装实例对它们进行评估。 |
| 抛出的异常 | 进入时抛出 `OperationCanceledException`；根路径为空时抛出 `ArgumentException`。 |
| 取消 | 仅在进入时检查一次。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI / 文件系统 / 事务 | 否 / 否 / 无。 |
| 稳定性 | 调用契约为 `STABLE_BETA`；列表内容是手工维护的清单：`PARTIAL`。 |
| 示例 | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 用途 | 枚举已安装的内容，并返回可在 `LaunchSpec` 中使用的规范标识。发现过程会跳过重解析点（目录联接形成的循环不会使其挂起），以 Windows-1252 读取 OMSI 文件（遵循带 BOM 标记的 UTF-8/UTF-16），并且从不跟随符号链接。 |
| 参数 | `query` 和 `scope` 见下表。对于 `Entrypoints`（地图标识）以及 `Repaints`、`FleetNumbers`、`Registrations`（车辆标识），`scope` 是必需的。 |
| 返回值 | 已排序的 `ContentIdentity(Identity, Kind, DisplayName)` 列表。标识是使用反斜杠的相对于安装实例的路径；比较不区分大小写。 |
| 抛出的异常 | 进入时抛出 `OperationCanceledException`；查询 `Entrypoints` 时未提供作用域或根目录为空时抛出 `ArgumentException`；作用域中的地图或车辆未安装时抛出 `FileNotFoundException`（不带 `OL_E_` 代码；CLI 将其映射为 `OL_E_NOT_FOUND`）。根目录或内容目录缺失时返回空列表，而不是错误。 |
| 取消 | 仅在进入时检查一次。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI / 文件系统 / 事务 | 否 / 否 / 无。 |
| 稳定性 | `Maps`、`Situations`、`Vehicles`：`STABLE_BETA`（每个经运行时验证的计划都通过它们进行解析）。`Entrypoints`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`：`EXPERIMENTAL`（仅有静态证据）。 |
| 示例 | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

`ContentQueryKind` 的取值及结果：

| 值 | 作用域 | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | 无 | `maps\<dir>\global.cfg` | `map` | 地图目录名 |
| `Situations` | 无 | `situations\...\<file>.osn` | `situation` | `.osn` 引用的地图标识（可能为 `null`） |
| `Vehicles` | 无 | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` 或文件名 |
| `Repaints` | 车辆标识（必需；缺少时返回空列表） | `<cti path>#item:<ordinal>` | `repaint` | `[item]` 名称 |
| `Hofs` | 无 | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | 车辆标识（必需；缺少时返回空列表） | 相对于车辆的 `[number]` 源路径 | `fleet-number` | `null` |
| `Registrations` | 车辆标识（必需；缺少时返回空列表） | `registration_automatic` / `registration_list` / `registration_free` | `registration` | 第一个值行（free 时为 `null`） |
| `Addons` | 无 | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | 地图标识（必需；缺少时抛出 `ArgumentException`） | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | 入口点标签 |
| `SessionProfiles` | 无 | 包目录名（即 `/predefined-profile` 的 id） | `session-profile`，包无法编译时为 `session-profile-invalid` | 配置档 `name`，或无效包的 `OL_E_SESSION_PROFILE_*` 代码 |
| `RuntimeModules` | 无 | 模块 id | `runtime-module`，无法加载时为 `runtime-module-invalid` | 模块 `name`，或 `OL_E_MODULE_*` 代码 |

入口点通过 `PresentedEntrypointIndex` 或 `EntrypointIdentity`（发现标识或精确标签）选择。规划器会把标识解析为 OMSI 所呈现的标签（OMSI 只呈现每个精确标签的第一条记录），计划会在 `ResolvedContent` 下以 `entrypoint` 类型列出它；在经运行时验证之前，`world.entrypoint-identity` 为 `EXPERIMENTAL`。

### `ListSessionProfilesAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 列出 `<root>\.omsilaunch\session-profiles\` 下的每个会话配置档包及其预设，使前端无需解析 YAML 即可提供它们。CLI 为 `/list:SessionProfiles`。 |
| 返回值 | 每个包目录一个 `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)`，按 `Id` 排序。`Presets` 是按索引顺序排列的 `SessionProfilePresetSummary(Index, Id, Name)`。每个包都按与启动相同的严格规则编译（`SessionProfileCompiler`）；无法编译的包会被列出，`IsValid = false`，带有其 `OL_E_SESSION_PROFILE_*` 代码和消息，且没有预设。没有配置档目录：返回空列表。 |
| 抛出的异常 | 进入时抛出 `OperationCanceledException`。编译错误会在结果中报告，而不是抛出。 |
| 是否需要运行中的会话 | 否。 |
| 是否修改 OMSI / 文件系统 / 事务 | 否 / 否 / 无。 |
| 稳定性 | `STABLE_BETA`（离线门禁 `session-profiles.discovery`）。 |
| 示例 | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 发现安装在 `plugins\OmsiLaunch.Modules` 之下的[运行时模块](runtime-modules.md)及其完整性和设置项。只读。模块加载是开放的：不会对照任何列表或签名检查模块。 |
| 返回值 | 每个模块目录一个 `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)`，按目录名排序。`Activation` 为 `requested`、`content` 或 `default`；当模块无法加载时 `IsValid` 为 false 并带有 `OL_E_MODULE_*` 代码；`Settings` 列出模块声明的设置项，形式为 `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`。 |
| 抛出的异常 | 进入时抛出 `OperationCanceledException`。 |
| 是否需要运行中的会话 | 否。CLI：`/list:RuntimeModules`（信封 `runtime-modules.list`）。 |

### `RecoverPendingAsync`

| 方面 | 详情 |
| --- | --- |
| 签名 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 用途 | 报告或完成由崩溃的所有者遗留的失效持久事务（`<root>\.omsilaunch\journal.json`）。在调用期间持有安装租约，因此绝不会在一个正在启动的会话底下执行还原。公共能力 `session.recover`；CLI `/recovery-status` 和 `/recover`。 |
| 参数 | `installation.RootPath`：安装根目录（使用 `Path.GetFullPath` 规范化）。`restore`：`false` = 仅报告；`true` = 还原、校验、删除事务日志和备份。 |
| 返回值 | `RecoveryStatus(Pending, Recovered, Diagnostics)`：`Pending` = 调用开始时存在事务日志；`Recovered` = 请求了还原、还原已执行且不再有事务日志；`Diagnostics` = 还原说明（带 `Data["sha256"]` 的 `restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`），未执行还原时为空。 |
| 抛出的异常 | 租约被占用时抛出 `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`；当事务日志中的 PID + 创建时间 + 可执行文件路径仍与某个存活进程相符，或（事务日志已越过 `HandoffCreated` 但没有 PID 时）来自该根目录的任何 `Omsi.exe` 正在运行时，抛出 `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`；附带 `OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` 或校验消息（“Restore hash mismatch: ...”、“Restore presence mismatch: ...”）的 `IOException`；事务日志损坏时抛出 `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException`；根目录为空时抛出 `ArgumentException`；`OperationCanceledException`。只要在还原开始之后抛出异常，事务日志都会被保留，下一次调用会以幂等方式重放。 |
| 取消 | 传递给事务日志/备份的写入操作；在还原中途取消会使事务日志保持待处理状态。 |
| 是否需要运行中的会话 | 否（在有所有者处于活动状态时会拒绝执行）。 |
| 是否修改 OMSI 状态 | 否。 |
| 是否修改文件系统 | 仅当 `restore == true` 时：从已校验的备份重写原始文件（字节、最后写入时间、创建时间、属性；可处理只读原始文件；直写 + 刷新；不留下 `*.omsilaunch.tmp`），删除会话产物，删除 `journal.json` 和 `backup\<sessionId>`。 |
| 事务 / 还原 | 完成待处理的事务（`Restoring` → `Restored` → 删除事务日志）。 |
| 稳定性 | `STABLE_BETA`：报告路径和提前退出后的恢复（矩阵 RV-008）、还原失败后的恢复（运行时收尾验证 `F01`）、在存活所有者下以及 PID 记录之前的窗口期内拒绝执行（`S04`），以及启动时延迟的指纹前恢复（`S05`）；参见[运行时验证状态](../status/runtime-validation-status.md)。 |
| 示例 | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## 契约类型

<a id="optional-values-and-semantic-primitives"></a>
### 可选值与语义基元

| 类型 | 定义 | 说明 |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`；`IsSet`、静态 `Unset`、静态 `Set(T)` | 区分“未请求”与“已请求且带有值”。JSON 结构见 [LaunchSpec 参考](launchspec.md)。 |
| `Presence` | `Unset` = 0，`Set` = 1 | 字节枚举。 |
| `SemanticDate` | `(int Year, int Month, int Day)` | 仅在 `DateTimeMode.Explicit` 时校验（月 1..12，日 1..31）。 |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | 仅在 `DateTimeMode.Explicit` 时校验（0..23、0..59、0..59）。 |

<a id="launchspec-family"></a>
### LaunchSpec 系列

以下所有记录均在 [LaunchSpec 参考](launchspec.md)中逐个属性进行了说明；本表确定类型清单。

| 类型 | 用途 | 稳定性 |
| --- | --- | --- |
| `LaunchSpec` | 根请求记录，带有 `EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures` 访问器，它们会为值为 `null` 的可选成员代入默认值。 | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`、`ExpectedExecutableSha256`（对 `Omsi.exe` 的可选固定，规划时检查）。 | `STABLE_BETA` |
| `WorldSpec`、`WorldMode`、`EntrypointSpec`、`EntrypointMode` | 世界选择。`WorldMode`：`NewMap` = 0，`SavedSituation` = 1，`LastMapState` = 2，`LastSituation` = 2（`LastMapState` 的已过时别名；绝不表示“最新的 .osn”）。`EntrypointMode`：`Unset`、`PresentedIndex`、`Identity`（由 `WorldSpec.Entrypoint` 计算得出）。 | `NewMap`、`SavedSituation`：`STABLE_BETA`；`LastMapState`：`UNAVAILABLE`；`EntrypointMode.Identity`：`PARTIAL` |
| `DateSpec`、`TimeSpec`、`YearSpec`、`DateTimeMode` | `DateTimeMode`：`Unset`、`Explicit`、`System`。`Unset` 以外的任何模式都会使计划不可运行。 | `PARTIAL`（计划证据 `UNAVAILABLE`） |
| `WeatherSpec`、`WeatherMode` | `WeatherMode`：`Unset`、`Preset`、`Icao`、`RealCurrent`。`Unset` 以外的任何模式都会使计划不可运行。 | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`、`Repaint`、`Hof`、`FleetNumber`、`Registration`、`Enabled`。设置任何字段都会使计划不可运行。 | `PARTIAL` |
| `EnvironmentSpec` | 八组 `IReadOnlyDictionary<string, OptionalValue<string>>`，包含语义化的 `options.cfg` 设置项。 | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`、`ControllerDocument`；设置任何值都会使计划不可运行。 | `PARTIAL` |
| `DiagnosticsSpec` | 六个布尔值；会被携带，但不被使用。 | `PARTIAL` |
| `SessionPresentationSpec`、`SplashMode` | `SplashMode`：`Unset` = 0，`Native` = 0（别名），`Managed` = 1。 | `STABLE_BETA` |
| `InternetTexturesSpec`、`InternetTexturesMode` | `InternetTexturesMode`：`Native`、`Disabled`、`Override`。 | `STABLE_BETA`（`Native`），`EXPERIMENTAL`（`Disabled`、`Override`） |
| `SessionProfileMetadata` | 已编译会话配置档的来源信息（`Id`、`Name`、`Version`、`Author`、`PresetId`、`PresetIndex`、`PresetName`、`PackagePath`）。 | `STABLE_BETA` |
| `HofManagementSpec` | 会话 [HOF 管理](hof-management.md)：`Operations`（`HofOperationSpec` 列表）。字段 `LaunchSpec.HofManagement`；访问器 `EffectiveHofManagement`。 | `STABLE_BETA` |
| `RuntimeModulesSpec` | 会话的[运行时模块](runtime-modules.md)：`Enable`（模块 id）、`Disable`（被排除的模块 id）、`Settings`（`"<module id>.<key>"` 到值）；访问器 `EffectiveDisable`、`EffectiveSettings`。字段 `LaunchSpec.Modules`；访问器 `EffectiveModules`。 | `EXPERIMENTAL` |
| `PlannedModule` | 模块的计划评估：`Id`、`Version`、`Status`（`ELIGIBLE` 或 `REJECTED`）、`Requested`、`FailurePolicy`、`Operations`（`module.<id>.<operation>`）、`Code`、`Reason`、`ManifestSha256`、`Activation`（`requested`、`settings-file`、`content` 或 `default`）、`Settings`；`IsEligible`、`EffectiveSettings`。常量 `Eligible`、`Rejected`、`ActivationRequested`、`ActivationSettingsFile`、`ActivationContent`、`ActivationDefault`。 | `EXPERIMENTAL` |
| `PlannedModuleSetting` | 一个生效的模块设置项：`Key`、`Value`、`Source`（`request`、`settings-file` 或 `default`）。 | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | 已安装模块的发现视图；参见 `ListRuntimeModulesAsync`。 | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | 模块声明的一个设置项：`Key`、`Type`、`Default`、`AllowsAuto`、`Minimum`、`Maximum`、`Choices`、`Flag`、`Description`。 | `EXPERIMENTAL` |
| `HofOperationSpec` | 一个 HOF 操作（`SourceKind`、`Source`、`TargetVehicles`、`Mode`、`Files`）。 | `STABLE_BETA` |
| `HofSourceKind` | `Collection`、`Directory`、`Vehicle`、`File`。 | `STABLE_BETA` |
| `HofApplyMode` | `Add`、`Mirror`。 | `STABLE_BETA` |
| `HofPlanningException` | 由 HOF 规划抛出（`OmsiLaunch.Core`）；`Code` 是 `OL_E_HOF_*` 代码。公共 API 将其作为计划诊断信息报告。 | `STABLE_BETA` |
| `SessionProfileSummary` | 一个包的发现视图（`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity`、`Presets`、`IsValid`、`ErrorCode`、`ErrorMessage`）。参见 `ListSessionProfilesAsync`。 | `STABLE_BETA` |
| `SessionProfilePresetSummary` | 摘要中的一个预设（`Index`、`Id`、`Name`）。 | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration`（会被携带，还原始终会发生）、`SuppressStaleClosecheckWarning`、`StartupTimeoutSeconds`（1..600，默认 180）、`ShutdownTimeoutSeconds`（会被携带，但不被使用）。 | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### 计划与状态类型

| 类型 | 字段 | 说明 |
| --- | --- | --- |
| `SessionPlan` | `SessionId`（每个计划一个新的 `Guid`）、`BuildProfileId`（`"Omsi23004"` 或 `"unrecognized"`）、`Spec`、`Platform`（`RuntimePlatformInfo`）、`ResolvedContent`（`ContentIdentity` 列表：`map`、`vehicle`、`repaint`、`hof`、`situation`、`situation-map`）、`TouchedFiles`（`PlannedMutations` 中去重后的相对路径）、`RuntimeArtifacts`、`RequiredCapabilities`（带有 `STATICALLY_VALIDATED` 或 `UNAVAILABLE` 的 `Capability`；原生启动能力带有 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`）、`UnsupportedRequestedFeatures`（针对已请求但不受支持的功能的 `Capability` 条目）、`PlannedMutations`、`Diagnostics`、`IsRunnable`。 | 它是公共记录：可能被修改或过期，这正是 `StartSessionAsync` 会重新规划的原因。 |
| `RuntimePlatformInfo` | `OsFamily`、`OsVersion`、`OsArchitecture`、`HostArchitecture`、`OmsiArchitecture`（`X86`）、`PluginArchitecture`（`X86`）、`CurrentPlatformSupported`（Windows 10+、x64 操作系统和 x64 宿主）、`LegacyPlatform`（始终为 `false`）、`Wow64Available`、`InstallationWritable`、`ProcessLaunchSupported`、`PluginRuntimeSupported`、`NativeInteropSupported`、`SharedMemorySupported`、`ExactRestoreSupported`（均等于 `CurrentPlatformSupported`）。 | |
| `Capability` | `Name`、`Available`、`EvidenceState`、`Reason`。 | `EvidenceState` 是 `PublicEvidence` 的值（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`），`SessionPlan.RequiredCapabilities` 除外：平台或内容要求为 `STATICALLY_VALIDATED` 或 `UNAVAILABLE`，原生能力为 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`。 |
| `PlannedMutation` | `RelativePath`、`SemanticKey`、`RequestedValue`、`Operation`（`token-patch`、`vector-component-patch`、`exact-file-overlay`）。 | 展示相关的变更使用键 `session-presentation.splash`、`internet-textures.override`、`internet-textures.cache`、`internet-textures.target`。 |
| `LaunchDiagnostic` | `Code`、`Message`、`Data`（可选的字符串映射）。 | 以 `OL_E_` 开头的代码是错误，以 `OL_W_` 开头的是警告，其他均为信息性代码。 |
| `SessionStatus` | `SessionId`、`State`（`SessionState`）、`Diagnostics`、`RuntimeEvents`。 | 会话诊断信息不包含计划诊断信息。 |
| `RuntimeEvent` | `Type`、`TimestampUtc`（宿主接收时间）、`Sequence`（从 1 开始，按会话计）、`Data`、`SessionTickMilliseconds`（插件发出该事件时其单调的会话时钟；宿主事件为 null）。 | 限定为最近的 256 个事件（丢弃最旧的）。插件事件通过遥测环按顺序到达；因环被覆盖而丢失的事件由 `telemetry.lost` 计数。 |
| `SessionHandle` | `SessionId`。 | 进程本地。 |
| `RecoveryStatus` | `Pending`、`Recovered`、`Diagnostics`。 | 参见 `RecoverPendingAsync`。 |
| `ContentIdentity` | `Identity`、`Kind`、`DisplayName`。 | 参见 `DiscoverAsync`。 |
| `ContentQueryKind` | `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`。 | |

### `SessionState`

按声明顺序排列的字节枚举：`Created`、`ValidatingPlatform`、`Planning`、`AcquiringInstallationLock`、`RecoveringPreviousTransaction`、`Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`WaitingForPlugin`、`PluginBootstrap`、`StartingWorld`、`EnteringGameplay`、`Running`、`ProcessExited`、`Restoring`、`CleaningRuntime`、`Completed`、`Failed`。当前服务从不设置 `ValidatingPlatform`、`Planning` 和 `EnteringGameplay`；`Snapshotting` 是瞬时状态，实际上无法观察到。终止状态：`Completed`、`Failed`。完整语义：[会话生命周期](../concepts/session-lifecycle.md)。

<a id="runtime-control-types"></a>
### 运行时控制类型

| 类型 | 定义 | 稳定性 |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | 宿主和插件用于邮箱信封（envelope）的静态编解码器：魔数 `0x4F4C5243`（“OLRC”），版本 1，72 字节小端序头部（魔数、版本、种类 1 = 请求 / 2 = 响应、总长度、会话 `Guid`、请求 id、负载长度、负载的 SHA-256），后跟 UTF-8 JSON 负载。`SerializeRequest`、`SerializeResponse`、`TryDeserializeRequest`、`TryDeserializeResponse`、`TryReadRequestId`。 | `INTERNAL`：因桥的两端共享而为 public；不是集成接口；格式可能随协议版本变化。 |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)`——宿主在内存映射文件 `OmsiLaunch.Handoff.<sessionId>` 中发布给插件的内容。 | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`：由交接（`StartupHandoff.Modules`）携带的已计划模块。 | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`，`IsEmpty`：由交接（`StartupHandoff.Start`）携带的 NEW_MAP 会话已解析的启动时钟；字段为 null 时保留 OMSI 自身的值。 | `INTERNAL` |
| `StartupHandoffWire` | 编解码器：魔数 `0x4F4C5348`，版本 7（可读取 3 至 7；版本 5 追加已计划的模块，版本 6 追加每个模块的生效设置，版本 7 追加启动日期/时间；标志位 0x04 和 0x08 分别携带 `PluginTrace` 和 `NativeTrace`），64 字节头部，使用 SHA-256 保证负载完整性。 | `INTERNAL` |
| `TelemetryRingWire` | 插件到宿主的事件环，位于映射 `OMSILAUNCH_TELEMETRY_NAME` 中：魔数 `0x52544C4F`（"OLTR"），版本 1，64 个带编号的 2048 字节槽。`Initialize`、`Write`、`IsRing`、`ReadSince`（返回某个序号之后的事件以及丢失了多少个）。 | `INTERNAL` |

除非 `WorldMode` 为 `NewMap` 或 `SavedSituation`、`HeadlessStart` 为 true、`PlayerVehicleEnabled` 为 false、日期/时间模式均为 `Unset`，并且已保存情景具有非空标识，否则插件会拒绝该交接（`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`）。规划器会更早地强制执行相同的约束，因此可运行的计划永远不会触发这种情况。

<a id="capability-registry-types"></a>
### 能力注册表类型

| 类型 | 用途 |
| --- | --- |
| `PublicCapabilityRegistry` | 规范能力目录。`ProtocolVersion`（`"0.1"`）、`All`（每个 `PublicCapabilityDescriptor`）、`Get`、`TryGet`、`PublicRuntimeOperationIds`（前端可以转发的 48 个具体操作 id）、`RuntimeOperations`、`TryGetOperation`、`NativeCapabilityOf`、`CliRoutes`（命令路由到操作 id）、`IsPublicRuntimeOperation`、`GetRuntimeArguments`、`ValidateRuntimeArguments`（返回 `PublicRuntimeArgumentValidation`）、`IsInternalResultKey`。由 `ExecuteRuntimeAsync`、CLI、本地控制平面和插件强制执行。 |
| `PublicCapabilityDescriptor` | `Id`、`Family`、`Classification`、`Kind`、`RequiresSession`、`RequiresExactProfile`、`ApiRoute`、`CliRoute`、`RuntimeValidation`（`PublicEvidence` 的值）、`Description`、`HandleTypes`、`Operations`、`NativeCapability`、`EvidenceNote`；派生的 `EffectiveOperations`、`NativeCapabilities`。 |
| `PublicRuntimeOperation` | `Id`、`NativeCapability`、`Arguments`、`CliRoutes`、`Paged`（接受 `cursor`/`limit`，参见[分页](capabilities.md#pagination)）、`MutationPolicy`。 |
| `PublicMutationPolicy` | 运行时操作会更改什么：`None`、`OmsiState`、`SessionResource`、`SessionPolicy`、`All`。 |
| `PublicEvidence` | 证据词汇：`RuntimeValidated`、`StaticallyValidated`、`Partial`、`RuntimeValidationRequired`、`Unavailable`、`All`。 |
| `PublicCapabilityClassification` | `PublicStableBeta`、`PublicExperimental`、`InternalOnly`、`Unsupported`。 |
| `PublicCapabilityKind` | `Read`、`Write`、`Action`、`Event`。 |
| `PublicRuntimeArgumentDescriptor` | `Name`、`Required`、`Description`。 |
| `PublicRuntimeArgumentValidation` | `Accepted`、`ErrorCode`、`Message`。 |

完整目录：[能力](capabilities.md)。

<a id="d3druntimeapi-extension-methods"></a>
### `D3DRuntimeApi` 扩展方法

针对 `d3d.*` 操作的 `ExecuteRuntimeAsync` 类型化封装（`EXPERIMENTAL`，`PublicExperimental` 能力 `d3d.texture`）。它们从一个从 30 000 开始的进程级计数器分配请求 id，默认超时为 5 s（`GetD3DStatusAsync` 除外，它必须显式提供超时）。

| 方法 | 操作 | 参数与限制 |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | 无 |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | width/height 1..4096，levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`：x/y 0..4095，width/height 1..4096，pixels ≤ 48 KiB（在线路上以 Base64 编码） |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | 重复释放会以 `OL_E_D3D_RESOURCE_RELEASED` 被拒绝 |

类型：`D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`；`D3DDeviceState`：`NotReady`、`Ready`、`Lost`、`Resetting`、`Stopping`、`Stopped`；`D3DTextureHandle(Value)`，其中 `Value = "d3dtex-<session id N>-<16 hex>"`；`D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`；`D3DTextureResourceState`：`Live`、`Released`、`Stale`；`D3DTextureFormat`：`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`。

错误：失败的结果会作为 `OmsiRuntimeException(Code, detail)` 重新抛出，其中 `Code` 是结果的 `ErrorCode`（缺失时为 `OL_E_RUNTIME_OPERATION_FAILED`），消息为 `"<code>: <Values["detail"]>"`；不带值的成功结果或未知的设备状态字符串会抛出 `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`。`ExecuteRuntimeAsync` 抛出的所有异常都会原样传播。设备重置处理已经运行时验证：重置会使设备经过 `Resetting` 回到 `Ready`，并使每个存活的纹理失效（`OL_E_D3D_STALE_RESOURCE_HANDLE`，运行时收尾验证 `D01`）；目录条目 `d3d.texture` 记录了这一点。`Lost` 转换无法从产品外部触发，仅由离线测试覆盖。

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### 进程契约类型

| 类型 | 内容 |
| --- | --- |
| `PublicExitCode` | `Success` = 0，`SessionFailed` = 1，`InvalidArguments` = 2，`UnsupportedProfile` = 3，`NoActiveSession` = 4，`RuntimeUnavailable` = 5，`NotFound` = 6，`OperationRejected` = 7，`TransactionRecoveryFailed` = 8，`InternalError` = 10。仅供 CLI 使用（[退出码](exit-codes.md)）；API 从不退出进程。 |
| `PublicErrorCategory` | CLI/控制错误信封中使用的字符串常量：`invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。 |
| `PublicErrorCodes` | 每个代码一个 `const string`（共 143 个：142 个 `OL_E_` 错误和 1 个 `OL_W_` 警告），以及 `All`，即 `PublicErrorDescriptor(Code, Category)` 目录。类别：`Cli`、`Compatibility`、`Content`、`Installation`、`InvalidArgument`、`LaunchSpec`、`LocalControl`、`Other`、`Presentation`、`Process`、`Runtime`、`RuntimeD3D`、`Session`、`SessionProfile`、`Transaction`、`Warning`。参考：[错误码](errors.md)。 |
| `PublicErrorDescriptor` | `(string Code, string Category)`。 |
| `OmsiRuntimeException` | `Code` 属性加消息；仅由 `D3DRuntimeApi` 抛出。 |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths`（安装实例标识与路径包含）

稳定性：`STABLE_BETA`（纯函数，无 I/O，不涉及 OMSI 状态，不修改文件系统，不参与事务，不需要运行中的会话）。它是安装租约、本地控制管道名称、会话配置档资源限定、网络纹理（Internet Textures）目标校验以及运行时生成模型路径所使用的唯一定义。

| 成员 | 行为 |
| --- | --- |
| `string NormalizeRoot(string root)` | 去掉末尾分隔符的 `Path.GetFullPath(root)`，但驱动器根目录（`C:\`）除外，予以保留。解析 `.` 和 `..` 段，将 `/` 与 `\` 同等对待，并合并重复的分隔符。**不**解析目录联接或符号链接。根目录为 null 或空白时抛出 `ArgumentException`。 |
| `string IdentityKey(string root)` | 转为大写的 `NormalizeRoot(root)`。同一根目录在词法上等价的各种写法（`C:\OMSI`、`C:\OMSI\`、`C:\OMSI\.`、`C:\foo\..\OMSI`、`c:\omsi`）共享同一个键；不同的根目录（`C:\OMSI-A`、`C:\OMSI-B`）绝不会共享。 |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | 解析 `candidate`（相对于 `root` 或绝对路径），仅当其严格位于 `root` 之下时返回 `true`；`relativePath` 是使用 `\` 的规范写法。它使用 `Path.GetRelativePath` 的路径段，因此像 `C:\OMSI-A\x` 这样的同级路径绝不会被视为位于 `C:\OMSI` 之内；根目录本身、其他卷以及 `..` 逃逸均返回 `false`。 |
| `IReadOnlyList<string> Segments(string relativePath)` | 按 `/` 和 `\` 拆分，丢弃空段。 |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### 会话配置档（`OmsiLaunch.Core`）

稳定性：`EXPERIMENTAL`。这些类型将 YAML [会话配置档](session-profiles.md)（`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`，schema `omsilaunch.session-profile/v1`）编译为 `LaunchSpec`。CLI 的 `/predefined-profile:<id> /predefined-profile-index:<n>` 正是使用这些调用；集成方可以用它们通过 API 启动某个配置档。

| 成员 | 行为 |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | 读取并校验包。`id` 必须是一个普通的目录名（否则为 `OL_E_SESSION_PROFILE_PATH_ESCAPE`）；`presetIndex` 为 1..99（`MinPresetIndex`..`MaxPresetIndex`；否则为 `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）。文件缺失：`OL_E_SESSION_PROFILE_NOT_FOUND`；大于 `MaxBytes`（256 KiB）、YAML 无效、使用锚点、存在未知键或 `id` 与目录名不同：`OL_E_SESSION_PROFILE_INVALID`；其他 `schema`：`OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`。资源路径被限定在包内（`OL_E_SESSION_PROFILE_PATH_ESCAPE`、`OL_E_SESSION_PROFILE_ASSET_MISSING`）。每种失败都是 `SessionProfileException`。 |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | 返回应用了配置档的 `baseline`：对于 `NewMap`，应用配置档的 `new` 块（地图、入口点，以及任何日期/时间/年份/天气）；将预设的 `settings` 合并到 `Environment.General` 之上；存在时应用预设的 `Presentation`、`InternetTextures` 和 `Behavior`；并设置 `SessionProfile` = `profile.Metadata`。对于 `NewMap`，它还会根据配置档的 `compatibility` 列表检查地图（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。 |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | 针对其他模式的兼容性检查（对于 `SavedSituation`，地图从 `.osn` 中读取）。CLI 在构建出最终的 `WorldSpec` 之后调用它。 |
| `SessionProfileCompiler.Schema`、`MaxBytes`、`SchemaKeys` | `"omsilaunch.session-profile/v1"`、`262144`，以及每个 YAML 映射所接受的键。 |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`、`ProfileNew`、`ProfilePreset` | 已加载的包；`Preset` 仅为所选的那个预设。 |
| `SessionProfileException(string code, string message)` | 带有 `Code`（`OL_E_SESSION_PROFILE_*` 代码之一）的 `IOException`；消息为 `"<code>: <message>"`。 |

CLI 还会拒绝与配置档冲突的命令行参数（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；该检查不属于编译器。完整的合并顺序参见[会话配置档](session-profiles.md#precedence-and-override-conflicts)。

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### 平台类型（`OmsiLaunch.Process`）

| 类型 | 稳定性 | 用途 |
| --- | --- | --- |
| `IRuntimePlatform` | 作为 `OmsiLaunchService` 构造函数参数类型时为 `STABLE_BETA` | 检测平台、检查可写性，以及启动、观察、终止和等待 `Omsi.exe`。请传入 `new CurrentWindowsX64Platform()`；不支持自行实现该接口。 |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | 唯一的实现：Windows x64 宿主，对 `Omsi.exe` 使用 `CreateProcessW`，对规范停止使用 `TerminateProcess`。其方法由服务调用；集成方只需构造它。成员（与 `IRuntimePlatform` 共享）：`Detect(root)` 返回计划中的 `RuntimePlatformInfo`；当宿主无法运行会话时，`ValidateCurrent(info)` 抛出 `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING`；`IsInstallationWritable(root)` 是 `OL_E_INSTALLATION_NOT_WRITABLE` 的判定依据；`StartAsync(request, sha256)` 创建 `Omsi.exe` 并记录进程标识（PID、创建时间、路径以及由服务计算的哈希；`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`）；`HasExited`、`WaitForExitAsync` 和 `Terminate` 负责观察和结束该进程。 |
| `InstallationLease`、`LaunchedProcess`、`ProcessIdentity`、`ReleaseManifest`、`RuntimeArtifact`、`RuntimeArtifactSet`、`StartupProcessRequest`、`CurrentRuntimeCommandStore`、`IOmsiProcessController`、`OmsiProcessState` | `INTERNAL` | 由于服务和测试共享这些类型，它们在程序集中为 public。不是集成接口；`LaunchedProcess` 在内部封装 OMSI 进程句柄和线程句柄（它们不是公共成员），并且永远不会由 `IOmsiLaunch` 返回。 |

<a id="thread-safety"></a>
## 线程安全

- `OmsiLaunchService` 可以安全地对不同会话进行并发调用：会话保存在 `ConcurrentDictionary` 中，每个会话的所有修改都在该会话的私有锁内进行。
- 对同一会话的并发调用是安全的，但在关键之处会被串行化：`ExecuteRuntimeAsync` 使用每会话门控，因此第二个命令会等待第一个命令（其超时从被暂存时开始计算）。
- 监管器从 `StartSessionAsync` 返回的那一刻起，在线程池任务（`Task.Run`）上运行，直到会话终止；它每 100 ms 轮询一次遥测和进程。调用方永远不会运行监管器代码。
- `StopAsync` 和 `GetStatusAsync` 同步完成，可以从任何线程调用，包括在 `ProcessExit` 处理程序内调用（CLI 就是这样做的，时间预算为 4 s）。
- 没有任何 API 调用具有线程亲和性；也没有任何调用需要同步上下文。

<a id="what-is-not-in-the-api"></a>
## API 中不包含的内容

- 不包含 `IntPtr`、`nint`、Win32 句柄、原生地址、VMT 指针或进程对象。键以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的结果值会在结果离开 `ExecuteRuntimeAsync` 之前被移除。
- 不包含 `internal.*` 运行时操作：`internal.road-vehicles.make-basic` 在注册表中为 `InternalOnly`，从 API 和 CLI 调用都会返回 `OL_E_RUNTIME_OPERATION_UNKNOWN`。
- 不提供对 OMSI 内存的原始读写，除 `LaunchSpec` 所声明的内容外，不提供对安装实例的文件级访问。
- 不提供跨进程句柄：[本地控制平面](local-control.md)是唯一的跨进程途径，并且只接受 `session.status`、`session.events`、`session.stop` 和 `runtime.execute`。
- 不在启动时应用玩家车辆。协作式关闭、`LAST_MAP_STATE`、启动日期/时间/天气以及键盘/控制器文档覆盖层均为 `EXPERIMENTAL`。

<a id="stability-summary"></a>
## 稳定性汇总

| 接口 | 稳定性 |
| --- | --- |
| `OmsiLaunchService` 构造函数、`OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`、`StartSessionAsync`（NEW_MAP、SAVED_SITUATION）、`GetStatusAsync`、`WaitForAsync`、`StopAsync`、`CloseAsync` | `STABLE_BETA` |
| `ExecuteRuntimeAsync` 传输；`PublicStableBeta` 操作 | `STABLE_BETA` |
| `PublicExperimental` 操作、`D3DRuntimeApi`、`camera.lock` | `EXPERIMENTAL` |
| 日期/时间/天气/输入 spec 成员、`DiagnosticsSpec`、`ExpectedExecutableSha256`、`RestoreConfiguration`、`ShutdownTimeoutSeconds`、`CooperativeShutdown` | `EXPERIMENTAL` |
| 玩家车辆 spec 成员 | `PARTIAL` |
| `RuntimeCommandWire`、`StartupHandoff`、`StartupHandoffWire`、`StartupModule`、`StartDateTime`、`StartWeather`、`TelemetryRingWire`、`IRuntimePlatform` 的实现、所有实现程序集 | `INTERNAL` |
| `WorldMode.LastSituation`、`internal.*` 操作 | `UNAVAILABLE` |
