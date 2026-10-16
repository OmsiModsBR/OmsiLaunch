<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>OMSI 2 的会话控制。</strong></p>
<p align="center">开源 · 可编程 · 社区驱动</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <strong>简体中文</strong> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <a href="README.ja-JP.md">日本語</a>
</p>

---

> 本文是规范性 [English (US) README](../../README.md) 的译文。两者内容不一致时，以 English (US) README 为准。

# OmsiLaunch

**OmsiLaunch** 是一个开源层，用于以编程方式启动 OMSI 2、管理会话并控制正在运行的模拟。它根据声明式描述规划会话，在带事务日志（journal）的事务中应用每一项临时配置更改，启动 OMSI 并持续观察直至进入游戏，允许工具通过公共 API 读取和更改正在运行的模拟，并在会话结束时还原它所改动的每一个文件。

它是面向启动器、工具、自动化和社区集成的基础设施，而不是图形化启动器。

> **定义会话，而不是点击。**

## 状态：0.1.0-beta4

当前公开 Beta 版本为 **`0.1.0-beta4`**。Beta 4 新增了按能力兼容性、运行时模块（Runtime Modules）、1..99 号会话配置档，以及一组启动和运行时功能。兼容性、能力目录、分页、类型化错误和模块桥已在实际 OMSI 会话中观察验证（验证轮次 A 和 B）。这些轮次之后新增的功能为 `EXPERIMENTAL`：已通过静态分析关闭，仅经离线测试，运行时验证待进行。部分功能仍为 `STATICALLY_VALIDATED`、`PARTIAL` 或 `UNAVAILABLE`。有两个运行时事项仍未关闭，因为无法安全地产生相应场景：Steam LAA 的游戏过程，以及道路车辆和人物对象的自然移除（RV-002）。[运行时验证状态](../localized/zh-CN/status/runtime-validation-status.md)页面是权威记录，说明哪些内容在 OMSI 下运行过、哪些仅离线运行过。

这是 Beta 版本：公共 API、CLI 和文件格式按成员分别标记为 `STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL` 或 `UNAVAILABLE`，在 1.0 之前仍可能变化。

## 支持的 OMSI 范围

OmsiLaunch 需要已安装的 OMSI 2。它不依赖某一个特定的 `Omsi.exe` 文件：每项原生能力在使用前都会在 OMSI 内部单独解析并校验，在某个版本（build）上无法校验的能力，在该版本上不可用。

| 项目 | 范围 |
| --- | --- |
| 要求 | 已安装 OMSI 2：32 位 `Omsi.exe`，其版本为 OMSI 2（2.x），并带有 `maps\` 和 `Vehicles\` 目录。其他情况一律以 `OL_E_UNSUPPORTED_BUILD` 拒绝。 |
| 已知布局 | OMSI 2.3.004（`Omsi23004`）。所有能力均可解析，所有运行时验证均在此布局上进行。哈希用于将其识别为快速路径；并非必要条件。 |
| 其他 OMSI 2 版本（build） | 每项能力通过与版本无关的策略（Delphi RTTI、唯一代码签名、VMT 槽）解析并校验。读取对象布局的能力需要已知布局。 |
| Steam 可执行文件 | 在进程内解析；正版 Steam 会话**未经**运行时验证。 |
| 操作系统 | Windows 10 或更高版本，x64。 |
| 运行时 | .NET 6 Desktop Runtime **x64**（控制器）和 .NET 6 Runtime **x86**（插件运行在 32 位 `Omsi.exe` 内部）。 |

详情：[兼容性](../localized/zh-CN/reference/compatibility.md)和[安装](../localized/zh-CN/getting-started/installation.md)。

## 提供的功能

**会话。** 会话根据 `LaunchSpec`（CLI 参数、JSON 文件、会话配置档或 API）进行规划，在无副作用的情况下完成验证（`/plan`），然后启动，通过 `SessionState` 状态转换进行观察，最后结束。停止会话会强制终止 OMSI，从而使 OMSI 无法覆盖即将被还原的文件。参见[会话生命周期](../localized/zh-CN/concepts/session-lifecycle.md)。

**事务与恢复。** 每一项配置覆盖都限定在会话范围内。OmsiLaunch 会对它更改的每个文件进行快照、记录事务日志、应用、校验和还原，崩溃后也是如此（`/recovery-status`、`/recover`、`RecoverPendingAsync`）。它不提供永久性的配置编辑。参见[事务与恢复](../localized/zh-CN/concepts/transactions-and-recovery.md)。

**CLI（`OmsiLaunch.exe`）。** 基于同一公共 API 的参考前端，自身不包含任何 OMSI 逻辑：提供发现、规划、启动会话，以及针对正在运行的会话的客户端命令。参见 [CLI 参考](../localized/zh-CN/reference/cli.md)和 [CLI 示例](../localized/zh-CN/reference/cli-examples.md)。

**`OmsiLaunchW.exe`。** 用于快捷方式的 Windows 子系统宿主程序。它接受相同的命令行，不显示控制台窗口，并通过消息框报告失败。参见 [OmsiLaunchW.exe](../localized/zh-CN/reference/omsilaunchw.md)。

**Windows 托盘。** 每个所有者会话都会显示一个通知区域图标，附带状态窗口和“结束会话”操作。该操作与 `session stop` 走同一条停止路径。参见 [Windows 托盘](../localized/zh-CN/reference/windows-tray.md)。

**会话配置档。** 声明式 `profile.yaml` 包（`omsilaunch.session-profile/v1`），位于 `.omsilaunch\session-profiles\<id>\` 下，内容作者可以借此发布可复现、一条命令即可启动的会话。参见[会话配置档](../localized/zh-CN/reference/session-profiles.md)。

**公共 API 与运行时控制。** `OmsiLaunch.Api`（`IOmsiLaunch`）是首选的产品接口。时间、天气、地图、摄像机、时刻表、车辆、人物对象、脚本变量和 D3D 纹理等运行时操作均限定在会话范围内，根据构建配置进行验证，并通过不透明的语义句柄（而非原生指针）寻址。结果受 64 KiB 运行时槽的大小限制。参见[公共 API 参考](../localized/zh-CN/reference/public-api.md)和[运行时控制](../localized/zh-CN/reference/runtime-control.md)。

**本地控制。** 每个安装实例对应一个绑定到当前活动 `session_id` 的命名管道，允许同一用户的其他进程读取状态和事件、停止会话以及执行公共运行时操作。参见[本地控制 / IPC](../localized/zh-CN/reference/local-control.md)。

**能力。** 每项能力和公共运行时操作都连同其稳定性一起登记在目录中。实验性和不可用的能力会被列出，而不是隐藏（`OmsiLaunch.exe capabilities`）。参见[能力](../localized/zh-CN/reference/capabilities.md)。

**永久插件。** 进程内插件闭包只需在 `plugins\OmsiLaunch.*` 下安装一次。每次启动前，都会根据 `release-manifest.json` 中的 SHA-256 条目对其进行校验。第三方插件绝不会被改动。参见[永久插件模型](../localized/zh-CN/concepts/permanent-plugin.md)。

## 快速入门

将发行包解压到 OMSI 2 安装根目录，然后在该目录中运行以下命令：

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

该会话运行期间，可以在同一目录中打开第二个控制台来查询或结束它：

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

分步指南：[第一个会话](../localized/zh-CN/getting-started/first-session.md)。面向 .NET 集成方：[公共 API 快速入门](../localized/zh-CN/getting-started/api-quick-start.md)。

## 文档

完整文档集的索引位于 [`docs/localized/zh-CN/README.md`](../localized/zh-CN/README.md)。`docs/` 下的 English (US) 页面是规范性的权威版本。

| 主题 | 页面 |
| --- | --- |
| 公共 API | [docs/localized/zh-CN/reference/public-api.md](../localized/zh-CN/reference/public-api.md) |
| CLI | [docs/localized/zh-CN/reference/cli.md](../localized/zh-CN/reference/cli.md) |
| CLI 示例 | [docs/localized/zh-CN/reference/cli-examples.md](../localized/zh-CN/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/zh-CN/reference/omsilaunchw.md](../localized/zh-CN/reference/omsilaunchw.md) |
| Windows 托盘 | [docs/localized/zh-CN/reference/windows-tray.md](../localized/zh-CN/reference/windows-tray.md) |
| 会话配置档 | [docs/localized/zh-CN/reference/session-profiles.md](../localized/zh-CN/reference/session-profiles.md) |
| 运行时控制 | [docs/localized/zh-CN/reference/runtime-control.md](../localized/zh-CN/reference/runtime-control.md) |
| 本地控制 / IPC | [docs/localized/zh-CN/reference/local-control.md](../localized/zh-CN/reference/local-control.md) |
| 能力 | [docs/localized/zh-CN/reference/capabilities.md](../localized/zh-CN/reference/capabilities.md) |
| 错误与退出码 | [docs/localized/zh-CN/reference/errors.md](../localized/zh-CN/reference/errors.md), [docs/localized/zh-CN/reference/exit-codes.md](../localized/zh-CN/reference/exit-codes.md) |
| 打包 | [docs/localized/zh-CN/reference/packaging.md](../localized/zh-CN/reference/packaging.md) |
| 已知限制 | [docs/localized/zh-CN/reference/known-limitations.md](../localized/zh-CN/reference/known-limitations.md) |
| 运行时验证状态 | [docs/localized/zh-CN/status/runtime-validation-status.md](../localized/zh-CN/status/runtime-validation-status.md) |

### 其他语言的文档

文档已翻译为 14 种语言区域，位于 [`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md) 下。这些译文依据规范性的 English (US) 页面生成，并已对照原文进行机械校验。母语人士的编辑审校不属于 Beta 4 发布的一部分，可能会在发布后进行。当译文与英文页面不一致时，以英文页面为准。

## 限制与未关闭事项

以下是最重要的限制。完整列表见[已知限制](../localized/zh-CN/reference/known-limitations.md)。

- **Steam。** Steam LAA 为 `PARTIAL`：游戏过程、读取、命令和停止都需要正版 Steam 安装实例，且未经运行时验证。
- **本 Beta 版本中为实验性：** 从上次地图状态启动（`/last`），以及启动时指定日期、时间、年份或天气，已通过静态分析关闭，等待运行时验证。**不可用：** 启动时分配玩家车辆；请求该项会使计划不可运行，而不是被静默忽略。
- **运行时写入受限。** `weather.set`、`weather.metar.set`、字符串变量写入和脚本触发器在经运行时验证之前均为实验性；日历写入和车辆重新定位不可用。运行时更改不会记录到事务日志中，也不会被还原。
- **句柄生命周期。** 对于自然消失的道路车辆和人物对象（RV-002），失效检测没有安全的运行时产生途径，仅经过离线验证。
- **分页结果。** 运行时列表通过 `cursor` 和 `limit` 分页，并从 `next_cursor` 继续；放不进 64 KiB 邮箱的页面会被缩短，并标记为 `truncated=true`。
- **默认强制停止。** OMSI 会被终止，其自身的关闭流程不会运行，未保存的 OMSI 状态会丢失。可选启用的协作式关闭（`/cooperative-shutdown`，实验性）会先运行 OMSI 的退出例程。
- **同一用户信任模型。** 同一 Windows 用户的任何进程都可以访问本地控制平面。

## 下载

从 [Releases 页面](https://github.com/lmonteirotech/OmsiLaunch/releases)下载 **`OmsiLaunch-0.1.0-beta4.zip`** 及其 `.sha256` 文件，并将其直接解压到受支持的 OMSI 根目录中。该包包含控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe`）、其依赖项、附带 `release-manifest.json` 的永久插件闭包、启动画面资源、一个会话示例，以及位于 `.omsilaunch\docs\` 下的离线文档。包布局和彻底移除方法：[打包](../localized/zh-CN/reference/packaging.md)。

## 从源代码构建

要求：

- Windows 10 或更高版本，x64。
- .NET 6 SDK，以及用于运行测试的 x64 和 x86 .NET 6 运行时。
- 安装了 MSBuild C++ 工作负载（平台工具集 `v145`）的 Visual Studio 和 Windows 10 SDK，用于构建三个原生项目：`OmsiLaunch.Native.x86`（Win32），以及 `OmsiLaunch.Bootstrapper` 和 `OmsiLaunch.WindowsHost`（x64）。

`OmsiLaunch.sln` 包含托管项目和测试套件。唯一的离线入口点会构建全部内容并运行所有离线测试套件；它从不启动 OMSI：

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` 跳过原生项目和打包回归测试。`-SkipDocs` 跳过文档门禁。构建输出位于 `artifacts\`，该目录不纳入版本跟踪。`tools\New-ReleasePackage.ps1` 基于现有构建对发行包进行暂存、计算哈希、验证和压缩。参见[打包](../localized/zh-CN/reference/packaging.md)和[测试与验证](../project/TESTING-AND-VALIDATION.md)。

| 路径 | 内容 |
| --- | --- |
| `src/` | 产品库：API、核心、配置、内容、互操作、进程、插件、构建配置、原生 x86 边界 |
| `tools/` | CLI 和 Windows 宿主程序（`OmsiLaunch.Cli`）、原生 shim（`OmsiLaunch.Bootstrapper`）、离线测试宿主程序、打包与验证脚本、本地化工具 |
| `tests/` | 单元、集成、配置、Windows UI 和文档测试套件 |
| `docs/` | 规范性文档及其位于 `docs/localized/` 下的译文 |
| `examples/` | LaunchSpec 和会话示例 |
| `assets/` | 品牌素材、图标和包资源 |
| `third_party/` | 上游来源说明 |

维护者摘要：[PUBLIC-API.md](../specs/PUBLIC-API.md)、[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md)、[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md)、[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md)、[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md)、[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md)。

## 社区与许可证

OmsiLaunch 是一个面向社区的开源项目。它独立于其他 OMSI 启动器，兼容的社区工具可以在其基础上构建。

OmsiLaunch 采用 [LGPL-3.0-only](../../LICENSE) 许可证。有关所含源代码的来源及适用声明，请参见[第三方声明](../../THIRD-PARTY-NOTICES.md)。
