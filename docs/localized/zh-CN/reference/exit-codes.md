# 退出码

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/exit-codes.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页列出 `OmsiLaunch.exe` 和 `OmsiLaunchW.exe` 可能返回的每一个进程退出码：公共托管契约 `PublicExitCode`（`src\OmsiLaunch.Api\PublicControlContract.cs`）、原生 shim 代码 `100`..`106`（`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` 和 `OmsiLaunch.WindowsHost.cpp`），以及 `CliProgram.Classify` 对任何逃逸异常所应用的分类规则（`tools\OmsiLaunch.Cli\Program.cs`）。调用方必须根据退出码和结构化错误信封（envelope）推断语义，绝不能根据消息文本推断。错误码在[错误](errors.md)中列出；产生各个退出码的命令见 [CLI 参考](cli.md)。

<a id="public-exit-codes-publicexitcode"></a>
## 公共退出码（`PublicExitCode`）

| 代码 | 枚举名 | 含义 | 何时出现 |
|---|---|---|---|
| 0 | `Success` | 命令已完成。 | `/version`、`capabilities`、`help`、`profiles`、`detect`、`/list`、`/recovery-status`；计划可运行时的 `/plan`/`/validate`；以 `Completed` 结束的会话；`OmsiLaunchW.exe` 启动后的 `/silent`；所有者以 `Ok=true` 应答的转发客户端命令；没有待处理内容或还原已完成时的 `/recover`。 |
| 1 | `SessionFailed` | 计划不可运行，或所拥有的会话以 `Failed` 结束。 | 报告 `NOT RUNNABLE` 的 `/plan`；计划不可运行的启动（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_PERMANENT_PLUGIN_*` 等；`OmsiLaunchW.exe` 还会在消息框中显示最后一条 `OL_E_` 诊断信息）；由 `StartSessionAsync` 引发的 `OL_E_PLAN_NOT_RUNNABLE`（启动时重新规划）；会话未在启动超时内到达 `Running`；会话以 `Failed` 终止。 |
| 2 | `InvalidArguments` | 命令行、spec、配置档或运行时参数在分派之前或分派期间被拒绝。 | 未知参数或路由、缺少值、值超出范围；`SessionProfileException`（`OL_E_SESSION_PROFILE_*`）；`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`；`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`；`OL_E_ITX_PROFILE_REQUIRED`；`OL_E_RUNTIME_OPERATION_UNKNOWN` 和 `OL_E_RUNTIME_ARGUMENT_REQUIRED`（本地产生或由所有者返回）；为无法分派的命令打印了用法说明；任何 `ArgumentException`、`FormatException`、`InvalidDataException` 或 `OverflowException`。 |
| 3 | `UnsupportedProfile` | 平台或 OMSI 版本（build）不受支持。 | 代码以 `OL_E_UNSUPPORTED_` 开头的逃逸异常（`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_OS_ARCHITECTURE`）。注意，在规划期间发现相同条件时，计划会变为不可运行，并改为返回 `1`。 |
| 4 | `NoActiveSession` | 客户端命令找不到所有者。 | 该安装实例的本地控制端点没有应答时的 `session status`、`session stop`、`events read`、`events watch` 或转发的运行时操作（`OL_E_NO_ACTIVE_SESSION`）。 |
| 5 | `RuntimeUnavailable` | 超时以异常形式逃逸。 | 任何 `TimeoutException`（消息中不含代码时为 `OL_E_TIMEOUT`，否则为其中嵌入的代码，例如 `OL_E_RUNTIME_REQUEST_TIMEOUT`）。转发的客户端超时由所有者以 `Ok=false` 应答，返回 `7`，而不是 `5`。 |
| 6 | `NotFound` | 找不到文件或目录。 | `FileNotFoundException` / `DirectoryNotFoundException`（默认 `OL_E_NOT_FOUND`），例如 `OL_E_SPEC_NOT_FOUND`、以异常形式引发的 `OL_E_ITX_PROFILE_MISSING`、`/list` 期间缺失的安装目录。 |
| 7 | `OperationRejected` | 命令有效但被拒绝，或转发的命令在所有者处失败。 | `OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`、`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED`、`OL_E_CANCELLED`；除两个参数类代码以外的每个 `Ok=false` 控制回复（`OL_E_CONTROL_*`、`OL_E_RUNTIME_*`、`OL_E_SESSION_NOT_RUNNING`）；任何其他携带 `OL_E_` 代码且未在别处分类的逃逸异常（`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_PROCESS_*` 等）。 |
| 8 | `TransactionRecoveryFailed` | 持久事务无法还原。 | 事务日志原本处于待处理状态且仍处于待处理状态时的 `/recover`；任何代码以 `OL_E_RECOVERY_` 开头或为 `OL_E_RESTORE_FAILED` 的逃逸异常（例如会话自身还原期间的 `OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`）。 |
| 10 | `InternalError` | 不带 `OL_E_` 代码的意外异常。 | 报告为 `OL_E_INTERNAL`，类别为 `internal`；消息为异常文本。 |

代码 `9` 未分配。

<a id="native-shim-exit-codes"></a>
## 原生 shim 退出码

由 `OmsiLaunch.exe` / `OmsiLaunchW.exe` 在托管控制器运行之前返回。它们与 `PublicExitCode` 互不重叠，因此调用方可以区分宿主启动失败和控制器结果。`OmsiLaunchW.exe` 还会在消息框中显示 `OmsiLaunch could not start the .NET host (code N).`。

| 代码 | 含义 | 原因 |
|---|---|---|
| 100 | 无法解析可执行文件路径 | `GetModuleFileNameW` 失败。 |
| 101 | 无法对命令行进行分词 | `CommandLineToArgvW` 返回 null。 |
| 102 | `hostfxr` 位置探测失败 | `get_hostfxr_path` 大小查询失败：未安装匹配的 .NET 运行时（需要 x64 .NET 6 运行时）。 |
| 103 | 无法获取 `hostfxr` 路径 | 第二次 `get_hostfxr_path` 调用失败。 |
| 104 | 无法加载 `hostfxr` 库 | 对解析得到的 `hostfxr.dll` 执行 `LoadLibraryW` 失败。 |
| 105 | 缺少必需的 `hostfxr` 导出 | 找不到 `hostfxr_initialize_for_dotnet_command_line`、`hostfxr_run_app` 或 `hostfxr_close`。 |
| 106 | 无法初始化托管宿主 | 针对 `OmsiLaunch.Controller.dll` 的 `hostfxr_initialize_for_dotnet_command_line` 失败（缺少 `OmsiLaunch.Controller.runtimeconfig.json`、缺少 x64 版 `Microsoft.WindowsDesktop.App` 6.0，或包已损坏）。 |

<a id="classification-rules-cliprogramclassify"></a>
## 分类规则（`CliProgram.Classify`）

从 `CliProgram.RunAsync` 逃逸的每个异常都由 `CliProgram.ReportFailure` 转换为错误信封（`CliInput.WriteError`）和退出码，后者会调用 `Classify`。解析失败在分派之前以同样方式处理（退出码 `2`）。规则按以下顺序应用：

1. 从异常消息中提取第一个 `OL_E_` 标记（`ExtractCode`）：代码是从 `OL_E_` 开始、由 ASCII 字母、数字和 `_` 组成的最长连续串。代码原样呈现在 `error.code` 中。
2. `SessionProfileException` → 其自身的 `Code`，类别 `invalid_argument`，退出码 `2`。
3. `ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException` → 提取到的代码或 `OL_E_INVALID_ARGUMENT`，类别 `invalid_argument`，退出码 `2`。
4. `FileNotFoundException`、`DirectoryNotFoundException` → 提取到的代码或 `OL_E_NOT_FOUND`，类别 `not_found`，退出码 `6`。
5. `TimeoutException` → 提取到的代码或 `OL_E_TIMEOUT`，类别 `runtime`，退出码 `5`。
6. `OperationCanceledException` → `OL_E_CANCELLED`，类别 `session`，退出码 `7`。
7. 否则，如果提取到了代码：
   - 以 `OL_E_RECOVERY_` 开头或等于 `OL_E_RESTORE_FAILED` → 类别 `transaction`，退出码 `8`；
   - `OL_E_INSTALLATION_BUSY`、`OL_E_SESSION_ALREADY_ACTIVE` → 类别 `session`，退出码 `7`；
   - `OL_E_PLAN_NOT_RUNNABLE` → 类别 `session`，退出码 `1`；
   - `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_ITX_PROFILE_REQUIRED` → 类别 `invalid_argument`，退出码 `2`；
   - 以 `OL_E_UNSUPPORTED_` 开头 → 类别 `unsupported_profile`，退出码 `3`；
   - 任何其他代码 → 对于 `InvalidOperationException` 和 `IOException` 类别为 `runtime`，否则为 `internal`；退出码 `7`。
8. 完全没有代码 → `OL_E_INTERNAL`，类别 `internal`，退出码 `10`。

转发的客户端回复不经过 `Classify`：`CliProgram.ReportForwarded` 在没有端点时返回 `4`，对 `OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED` 返回 `2`，对任何其他 `Ok=false` 回复返回 `7`，对 `Ok=true` 返回 `0`。

<a id="scripting-guidance"></a>
## 脚本编写指南

- 将 `0` 视为成功，其他一切视为失败；先根据数字代码分支，再根据 `--json` 信封中的 `error.code` 分支。
- 会话启动只有在会话结束且其文件已还原后才会返回；`1` 表示事务已执行但 OMSI 失败或计划被拒绝，而不表示文件仍处于被修改状态（残留的事务日志由 `/recovery-status` 报告）。
- `100`..`106` 表示包或 .NET 运行时已损坏；见[安装](../getting-started/installation.md)和[打包](packaging.md)。
