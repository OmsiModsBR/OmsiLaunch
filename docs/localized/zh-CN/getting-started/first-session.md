# 第一个会话

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../getting-started/first-session.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页逐步介绍使用 OmsiLaunch `0.1.0-beta4` 进行的第一个受管 OMSI 会话：在不启动 OMSI 的情况下进行规划、使用显式参数启动、使用预定义的会话配置档启动、控制和停止会话，以及事后查找诊断信息。本页假定已按照[安装](installation.md)中的说明安装了包。每个参数都在 [CLI 参考](../reference/cli.md)中规定；更多调用示例见 [CLI 示例](../reference/cli-examples.md)。

<a id="what-a-session-does"></a>
## 会话的作用

会话是围绕一个 OMSI 进程的事务：OmsiLaunch 会为它将要触及的文件创建快照（默认是 `GUI\` 下的两个启动画面位图，在请求 `/set` 覆盖层（overlay）时还包括 `options.cfg`），在 `.omsilaunch\` 下写入持久的事务日志（journal），应用覆盖层，启动带有永久插件的 `Omsi.exe`，等待进入游戏（`Running`），在此期间保持会话可控，并在结束时终止 OMSI 并逐字节还原每个被触及的文件。`/new` 从不静默选择地图或入口点：二者都必须给出，或者来自 `/spec` 文件或会话配置档。

<a id="1-plan-nothing-is-started"></a>
## 1. 规划（不启动任何内容）

在 OMSI 根目录下运行；安装实例默认为包含 `OmsiLaunch.exe` 的目录。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

计划必须显示 `"IsRunnable": true`（退出码 `0`）。它会列出 `TouchedFiles` 和 `PlannedMutations`，以便您准确了解会话将覆盖哪些内容。继续之前，请修复所有 `OL_E_` 诊断信息；此时尚未写入任何内容。

随包提供的示例 spec 通过文件完成同样的操作：

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. 使用显式参数启动

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

依次发生的事情：

1. 打印计划（`Plan: READY profile=Omsi23004`，在已知布局上）。
2. 恢复任何较早的待处理事务日志、获取租约、创建快照、写入事务日志、应用覆盖层、检查插件完整性、启动 `Omsi.exe`。
3. 出现托盘图标（`OmsiLaunch is running`）；见 [Windows 托盘](../reference/windows-tray.md)。
4. 进入游戏后，`Running` 状态以 JSON 形式打印（`"State": 14`）。默认启动超时为 180 s（可用 `/startup-timeout:<1..600>` 修改）。
5. 控制台会一直保持连接，直到会话结束。不要通过关闭控制台窗口来停止会话：请使用下面的某种停止方法。

首次运行时可选的附加参数：

- `/set:graphics.maxFPS=60`（一个 `options.cfg` 覆盖层，在结束时还原）；
- `/splash:Unset` 保持 OMSI 启动画面不变，或用 `/splash-language:DEU` 选择本地化的托管启动画面；
- `/observe-seconds:30` 在 `Running` 之后 30 s 自动停止（适用于冒烟测试）；
- `--json` 输出结构化结果。

请求日期、时间、年份、天气或玩家车辆的参数（`/date`、`/time`、`/year`、`/weather*`、`/vehicle` 等）会被接受，但本版本（build）无法应用它们：计划会变为 `NOT RUNNABLE`，并报告 `OL_E_CAPABILITY_UNAVAILABLE`。请不要使用这些参数。

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. 使用预定义的会话配置档启动

会话配置档是位于 `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 的 YAML 文件，它固定地图、入口点以及最多 99 个设置项预设（schema 为 `omsilaunch.session-profile/v1`；完整参考见[会话配置档](../reference/session-profiles.md)）。创建 `D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

然后：

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

需要记住的规则：`id` 必须与目录名相同；索引为 `1..99`；会覆盖配置档所拥有字段的显式参数（`/map`、`/entrypoint-index`、预设所拥有的 `/set` 键、预设含有 `presentation` 时的启动画面参数）会以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 被拒绝（退出码 `2`）；`new:` 块仅在使用 `/new` 时生效；使用 `/saved:<file.osn>` 时，情景的地图必须列在 `compatibility.maps` 下。随包提供的 `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` 展示了完整的 schema，但其 `new:` 块设置了 `date`、`time` 和 `weather`，而本版本（build）无法应用这些键，因此只有在删除这些键之后才能复制使用。

<a id="4-control-the-running-session"></a>
## 4. 控制正在运行的会话

在同一目录下的第二个控制台中运行（不带安装实例参数）：

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

这些命令通过该安装实例的本地控制管道执行（[本地控制](../reference/local-control.md)）；退出码 `4` 表示此处没有正在运行的所有者。

<a id="5-stop"></a>
## 5. 停止

以下任何一种方式都以相同的方式结束会话（先终止 OMSI，然后还原每个被触及的文件，再删除事务日志和备份）：

| 方法 | 说明 |
|---|---|
| 托盘图标 → `End session` → 确认 | 在 `OmsiLaunch.exe` 和 `OmsiLaunchW.exe` 会话中均可用。 |
| `OmsiLaunch.exe session stop` | 从另一个控制台执行；立即返回，由所有者完成还原。 |
| 在所有者控制台中按 Ctrl+C | 发出停止请求；所有者在退出前等待还原完成。 |
| `/observe-seconds:<n>` | 在 `Running` 之后 `n` 秒自动停止。 |
| OMSI 自行退出 | 所有者检测到 `ProcessExited` 并进行还原。 |

OMSI 自身的关闭例程不会运行，因此 OMSI 不会在退出时重写 `options.cfg`；这是有意为之，以便实现精确还原。用 X 按钮关闭所有者控制台窗口时，还原只有 4 s 时间；如果还原未完成，下一次启动（或 `OmsiLaunch.exe /recover`）会根据事务日志完成还原。会话以 `Completed` 结束时，所有者的退出码为 `0`。

<a id="6-where-to-look-afterwards"></a>
## 6. 事后查看的位置

| 位置 | 内容 |
|---|---|
| 控制台 / `--json` 输出 | 计划、`Running` 状态、最终状态（`"State": 18` = `Completed`）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | 会话的宿主跟踪记录（事务边界、进程启动、插件交接、进入游戏、还原）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | 由所有者运行的 `/runtime:` 操作的结果。 |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | 托盘指示器事件。 |
| `OmsiLaunch.exe /recovery-status` | 干净结束后为 `"pending": false`。`true` 表示残留了事务日志；请运行 `OmsiLaunch.exe /recover`。 |

如果会话未能进入游戏，最终状态会带有导致失败的 `OL_E_` 诊断信息（例如 `OL_E_STARTUP_TIMEOUT`、`OL_E_PLUGIN_NOT_LOADED`、`OL_E_PROCESS_EXITED_EARLY`），退出码为 `1`，并且文件仍然已被还原。见[退出码](../reference/exit-codes.md)、[错误](../reference/errors.md)和[已知限制](../reference/known-limitations.md)。

<a id="running-without-a-console"></a>
## 不使用控制台运行

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

上述命令会委托给 `OmsiLaunchW.exe`，并立即返回 `0`。该会话没有控制台；失败会以消息框形式显示，托盘图标是唯一可见的界面。可使用 `session status`、`events watch` 和诊断目录来跟踪会话。Windows 宿主的完整行为见 [OmsiLaunchW.exe 参考](../reference/omsilaunchw.md)。
