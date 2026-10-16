# OmsiLaunchW.exe 参考

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> 本页是 OmsiLaunch 0.1.0-beta3 [英文原始页面](../../../reference/omsilaunchw.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

`OmsiLaunchW.exe` 是 OmsiLaunch 控制器的 Windows 子系统（GUI）宿主。它接受与 `OmsiLaunch.exe` 相同的命令行，并运行相同的控制器代码（`OmsiLaunch.Controller.dll`）。唯一的区别在于报告方式：没有控制台窗口，失败以消息框形式显示，正在运行的会话只能通过其[托盘图标](windows-tray.md)看到。

权威来源：`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp`（原生 shim）、`tools\OmsiLaunch.Cli\WindowsHost.cs`（`WindowsHost`、`SessionTrayIndicator`）和 `tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Write*`）。

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## OmsiLaunch.exe 与 OmsiLaunchW.exe 对比

| 方面 | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| 子系统 | 控制台。从资源管理器启动时会打开控制台窗口。 | Windows（GUI）。没有控制台窗口。 |
| 原生 shim | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| 环境 | 不变 | 在 .NET 启动之前为控制器进程设置 `OMSILAUNCH_WINDOWS_HOST=1` |
| 参数 | 用 `CommandLineToArgvW` 分词后原样传给控制器 | 相同，因此两个宿主接受完全相同的参数和命令（[CLI 参考](cli.md)） |
| 文本输出 | 写入 stdout | 被抑制，除非给出 `--json`（此时 JSON 信封会写入 stdout，调用方可以重定向） |
| 错误 | `OL_E_...: message` 或 JSON 错误信封 | 相同的控制台输出规则，**另外**每个错误都会显示消息框（见[失败对话框](#failure-dialogs)） |
| `/silent` | 以其余参数启动 `OmsiLaunchW.exe` 并返回 `0` | 被忽略：命令已经在 Windows 宿主中运行 |
| 托盘图标 | 所有者会话时显示 | 所有者会话时显示 |
| 停止途径 | 托盘、`session stop`、OMSI 退出、`/observe-seconds`、Ctrl+C、关闭控制台 | 托盘、`session stop`、OMSI 退出、`/observe-seconds`（没有控制台，因此 Ctrl+C 和关闭控制台不适用） |
| 退出码 | [`PublicExitCode`](exit-codes.md) `0`..`10`，shim 代码 `100`..`106` | 相同的代码 |

<a id="how-it-starts"></a>
## 启动过程

1. shim 解析自身路径（`GetModuleFileNameW`），并要求 `OmsiLaunch.Controller.dll` 位于同一目录中。
2. 它对命令行进行分词（`CommandLineToArgvW`），并设置 `OMSILAUNCH_WINDOWS_HOST=1`。
3. 它通过随包提供的 `nethost.dll` 定位 `hostfxr`，加载它，用参数初始化控制器（控制器路径不属于 CLI 解析器看到的参数列表），然后运行控制器。
4. shim 原样返回控制器的退出码。

如果控制器运行之前的任何步骤失败，shim 会显示一个标题为 `OmsiLaunch`、文本为 `OmsiLaunch could not start the .NET host (code N).` 的消息框，并以该代码退出：

| 代码 | 失败的步骤 |
| --- | --- |
| `100` | 无法解析可执行文件路径 |
| `101` | 无法对命令行进行分词 |
| `102` | `hostfxr` 位置探测失败（通常是因为未安装 .NET 6 x64 运行时） |
| `103` | 无法获取 `hostfxr` 路径 |
| `104` | 无法加载 `hostfxr` |
| `105` | 缺少必需的 `hostfxr` 导出 |
| `106` | 无法初始化托管宿主（例如缺少 `OmsiLaunch.Controller.dll` 或其运行时配置，或者没有安装 Windows Desktop 运行时） |

`OmsiLaunch.exe` 使用同一张表，但不打印任何内容。这些对话框尚未在运行时实际产生过（见[运行时验证状态](../status/runtime-validation-status.md)）。

<a id="starting-it"></a>
## 启动方式

直接启动，可通过快捷方式、脚本或其他程序：

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

通过带 `/silent` 的 `OmsiLaunch.exe` 启动：

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

请将可执行文件放在 OMSI 2 安装实例中（包布局，见[打包](packaging.md)）；不带安装实例参数时，安装实例就是包含该可执行文件的目录。显式指定的安装实例作为第一个参数传入，与 `OmsiLaunch.exe` 相同（`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`）。

由于它是 GUI 程序，`cmd.exe` 和资源管理器不会等待它。若要在脚本中等待并读取退出码，请在 `cmd.exe` 中使用 `start /wait OmsiLaunchW.exe ...`，或在 PowerShell 中使用 `Start-Process -Wait -PassThru`：

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## /silent 委托

`OmsiLaunch.exe ... /silent`（或 `--silent`）在并非已经运行于 `OmsiLaunchW.exe` 之下时，会执行以下操作（`CliProgram.RunAsync`、`CliProgram.SilentDelegation`）：

1. 在 `OmsiLaunch.exe` 所在目录中查找 `OmsiLaunchW.exe`。如果缺失：`OL_E_WINDOWS_HOST_MISSING`，退出码 `7`。
2. 通过 `ShellExecute`（`UseShellExecute = true`）启动 `OmsiLaunchW.exe`，使用当前目录，并按原始顺序传递除 `/silent`/`--silent` 以外的所有参数。`ShellExecute` 不会将调用方的句柄传给新进程，因此捕获 `OmsiLaunch.exe /silent` 输出的调用方不会在整个会话期间被阻塞（运行时收尾 BUG-03）。如果没有返回进程：`OL_E_WINDOWS_HOST_START_FAILED`，退出码 `7`。
3. 写入 `silent` 信封并立即以 `0` 退出：

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

退出码 `0` 仅表示 `OmsiLaunchW.exe` 已被启动。会话结果（规划错误、已有活动的所有者、启动失败）由 `OmsiLaunchW.exe` 通过其自身的对话框和诊断信息报告，此后两个进程相互独立：`OmsiLaunch.exe` 已经结束，`OmsiLaunchW.exe` 是会话所有者。可使用 `OmsiLaunch.exe session status` 查看会话。

`/silent` 先于所有其他命令应用，因此 `OmsiLaunch.exe /silent session status` 也会在 `OmsiLaunchW.exe` 内运行 `session status`，其输出在那里被抑制。请仅在启动会话时使用 `/silent`。

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## 各命令在 OmsiLaunchW.exe 下的行为

| 命令行 | 结果 |
| --- | --- |
| 无参数 | 静默运行 `detect` 并以 `0` 退出（不显示任何内容） |
| 计划可运行且没有所有者的启动（`/new`、`/saved:...`、`/spec:...`、会话配置档） | 成为会话所有者：在启动和运行期间显示托盘图标；会话结束时退出（`0` 为已完成，`1` 为失败） |
| 计划不可运行的启动 | 显示带有计划最后一条 `OL_E_` 诊断信息的对话框（回退为 `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`），退出码 `1`（文档审计 BUG-06；修复之前会静默退出） |
| 该安装实例已有活动所有者时的启动 | 显示 `OL_E_SESSION_ALREADY_ACTIVE` 对话框，退出码 `7`；正在运行的会话不受影响 |
| 未能到达 `Running` 的启动 | 显示 `The OMSI session did not reach gameplay.` 对话框，并附带最后一条 `OL_E_` 诊断信息，退出码 `1`。对话框打开期间，会话监管器会终止 OMSI 并还原文件；对话框关闭且 `CloseAsync` 完成后进程退出 |
| 无效参数、未知参数或无效的会话配置档 | 显示带有 `OL_E_INVALID_ARGUMENT` 或 `OL_E_SESSION_PROFILE_*` 代码的对话框，退出码 `2` |
| 没有所有者时的客户端命令（`session status`、`session stop`、`events read`、`time get`、`/runtime:...`） | 显示 `OL_E_NO_ACTIVE_SESSION` 对话框，退出码 `4` |
| 被所有者拒绝的客户端命令 | 显示带有拒绝代码（例如 `OL_E_CONTROL_SESSION_MISMATCH` 或某个运行时错误码）的对话框，退出码 `7`（未知操作或缺少参数时为 `2`） |
| 成功的客户端命令或发现类命令（`session status`、`/list:...`、`help`、`capabilities`、`/version`、`/recovery-status`） | 除非给出 `--json` 且 stdout 被重定向，否则没有可见输出；退出码与 `OmsiLaunch.exe` 相同 |
| `/plan` 或 `/validate` | 不显示对话框，即使计划不可运行也是如此；退出码 `0` 或 `1` |
| 任何其他控制器失败 | 显示带有分类后 `OL_E_` 代码的对话框；退出码见[退出码](exit-codes.md) |

<a id="failure-dialogs"></a>
## 失败对话框

`OmsiLaunch.exe` 会打印的每个错误也都会以模态消息框（`WindowsHost.ShowFailure`）显示，即使给出了 `--json` 也是如此：

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` 是错误消息；对于会话失败，则是最后一条 `OL_E_` 诊断信息的消息。已知限制：对于由插件报告的失败，消息是插件的原始失败负载（例如 `{"name":"world.failed",...}`）；`Code:` 行是正确的（见[已知限制](known-limitations.md)）。该对话框是模态的，关闭后进程退出。运行时证据：参数错误、没有活动会话以及进入游戏之前的失败（运行时收尾 `T04`）。

<a id="session-tray-and-exit"></a>
## 会话、托盘与退出

由 `OmsiLaunchW.exe` 拥有的会话，其行为与由 `OmsiLaunch.exe` 拥有的会话完全相同（见[会话生命周期](../concepts/session-lifecycle.md)）：

- 会话一旦启动，托盘图标即会出现，早于 OMSI 进入游戏，除非在 `/spec` 文件中设置了 `Presentation.SuppressTrayIcon`。使用 `SuppressTrayIcon` 时完全没有可见界面；请通过 `OmsiLaunch.exe session stop` 或关闭 OMSI 来停止会话。
- 会话在以下情况下结束：OMSI 退出、在托盘中确认 `End session`（结束会话）、客户端发送 `session stop`，或 `/observe-seconds` 时间到。随后 OmsiLaunch 会在 OMSI 仍在运行时将其终止，还原它更改过的每个文件，释放安装租约，移除托盘图标并退出。
- 如果 `OmsiLaunchW.exe` 本身被强行结束，该安装实例的下一次 OmsiLaunch 启动会恢复待处理的事务（见[事务与恢复](../concepts/transactions-and-recovery.md)）。

<a id="quick-start"></a>
## 快速入门

1. 将包安装到 OMSI 2 目录中（[安装](../getting-started/installation.md)）。
2. 为 `OmsiLaunchW.exe` 创建一个带有会话参数的快捷方式，例如 `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`。
3. 启动它。OMSI 在没有控制台窗口的情况下启动；OmsiLaunch 图标出现在通知区域中。
4. 右键单击该图标 → `Status` 查看会话（状态窗口），或选择 `End session` → `End session` 结束会话。
5. 如果出现问题，对话框会显示错误码；详细信息位于 `<OMSI_PATH>\.omsilaunch\diagnostics`。
