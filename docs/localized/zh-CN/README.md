# OmsiLaunch 文档

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../README.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

这是 OmsiLaunch `0.1.0-beta4`（加固后基线）的规范性英文文档。OmsiLaunch 为已安装的 OMSI 2 提供可编程的启动、会话所有权和运行时控制；每项原生能力都按版本（build）分别解析并校验（参见[兼容性](reference/compatibility.md)）。`docs/` 下的每个页面都描述当前代码的实际行为；页面与代码不一致时，以代码为准，页面视为缺陷。

全文使用的稳定性词汇：`STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`、`UNAVAILABLE`。已解析但不起任何作用的参数标记为 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`。除非[运行时验证状态](status/runtime-validation-status.md)中如此说明，否则任何内容都不称为经运行时验证。

<a id="who-reads-what"></a>
## 读者指引

| 读者 | 从这里开始 | 然后阅读 |
| --- | --- | --- |
| 用户（CLI、快捷方式、会话配置档） | [安装](getting-started/installation.md)、[第一个会话](getting-started/first-session.md) | [CLI 参考](reference/cli.md)、[CLI 示例](reference/cli-examples.md)、[会话配置档](reference/session-profiles.md)、[Windows 托盘](reference/windows-tray.md)、[退出码](reference/exit-codes.md) |
| 集成方（`OmsiLaunch.Api`、本地 IPC） | [公共 API 快速入门](getting-started/api-quick-start.md)、[公共 API 参考](reference/public-api.md)、[LaunchSpec 参考](reference/launchspec.md) | [会话生命周期](concepts/session-lifecycle.md)、[运行时控制](reference/runtime-control.md)、[能力](reference/capabilities.md)、[本地控制 / IPC](reference/local-control.md)、[错误参考](reference/errors.md) |
| 维护者（发布、验证、边界） | [打包](reference/packaging.md)、[永久插件模型](concepts/permanent-plugin.md) | [事务与恢复](concepts/transactions-and-recovery.md)、[兼容性](reference/compatibility.md)、[已知限制](reference/known-limitations.md)、[运行时验证状态](status/runtime-validation-status.md) |

<a id="navigation"></a>
## 导航

| 页面 | 用途 |
| --- | --- |
| [入门](getting-started/first-session.md) | 从 OMSI 根目录规划、启动、观察并停止一个会话。 |
| [安装](getting-started/installation.md) | 前提条件、将包解压到 OMSI 根目录、用 `/version` 校验、彻底卸载。 |
| [公共 API 快速入门](getting-started/api-quick-start.md) | 一个完整的 .NET 程序，用于规划、启动、读取并停止一个会话。 |
| [CLI 参考](reference/cli.md) | `OmsiLaunch.exe` / `OmsiLaunchW.exe` 的每个参数、命令词和层级路由。 |
| [CLI 示例](reference/cli-examples.md) | 常见任务的可直接复制粘贴的命令行。 |
| [LaunchSpec 参考](reference/launchspec.md) | 每个 `LaunchSpec` 属性和枚举值，以及 `/spec` 的 JSON 加载规则。 |
| [HOF 管理](reference/hof-management.md) | 会话范围内的 HOF 复制与镜像到车辆文件夹：来源、目标、冲突、还原。 |
| [运行时模块](../../reference/runtime-modules.md) | 随会话在 OMSI 内运行的模块：清单、信任、生命周期、失败策略、操作。 |
| [会话配置档参考](reference/session-profiles.md) | `profile.yaml` 架构 `omsilaunch.session-profile/v1`、键、限制、优先级。 |
| [公共 API 参考](reference/public-api.md) | `IOmsiLaunch`、公共记录和枚举，以及每个成员的稳定性。 |
| [公共 API 清单](reference/public-api-inventory.md) | 自动生成的列表，包含每个公共类型和成员的签名与稳定性。 |
| [运行时控制](reference/runtime-control.md) | 运行时命令通道、超时、句柄、停止语义。 |
| [能力参考](reference/capabilities.md) | 能力目录，以及每个公共运行时操作 ID 及其分类。 |
| [会话生命周期](concepts/session-lifecycle.md) | `SessionState` 状态转换、`StartSessionAsync` 的承诺、会话如何结束。 |
| [事务与恢复](concepts/transactions-and-recovery.md) | 事务日志（journal）状态、备份、还原校验、会话删除、崩溃恢复。 |
| [永久插件模型](concepts/permanent-plugin.md) | `plugins\OmsiLaunch.*` 闭包、基于清单（manifest）的完整性校验、会话从不触及的内容。 |
| [本地控制 / IPC](reference/local-control.md) | 命名管道协议 `0.1`、按安装实例划分的端点、`session_id` 绑定、信任模型。 |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Windows（无控制台）主机：与 `OmsiLaunch.exe` 的差异、`/silent`、对话框、退出码。 |
| [Windows 托盘](reference/windows-tray.md) | 通知区域指示器：图标、菜单、逐字段说明的状态窗口、End session、资源管理器重启。 |
| [错误参考](reference/errors.md) | 每个 `OL_E_*` / `OL_W_*` 代码及其类别和含义。 |
| [退出码](reference/exit-codes.md) | `PublicExitCode` 值 0 到 10，以及引导程序 shim 代码 100 到 106。 |
| [打包 / 安装布局](reference/packaging.md) | 发行 ZIP 中的文件、`release-manifest.json`、`.omsilaunch\` 布局。 |
| [兼容性 / 支持的 OMSI 版本](reference/compatibility.md) | 唯一受支持的 `Omsi.exe` 哈希、已接受的 Steam LAA 哈希、平台要求。 |
| [已知限制](reference/known-limitations.md) | 本 Beta 中不受支持、部分实现或属于已接受的风险的内容。 |
| [运行时验证状态](status/runtime-validation-status.md) | 哪些已在 OMSI 下运行、哪些仅离线运行、哪些仍需要真实会话。 |

对维护者仍具有规范性的页面：仓库的
[`README.md`](../../../README.md)、`docs/specs` 下的规范
（[`PUBLIC-API.md`](../../specs/PUBLIC-API.md)、
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md)、
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md)、
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)）以及 `docs/project` 下的项目页面
（[`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md)、
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md)、
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)）。它们只是概要；上面的页面才是详细参考。历史页面列在[文档清单](../../DOCUMENTATION-MANIFEST.md)中。

<a id="how-this-documentation-is-kept-in-sync"></a>
## 本文档如何保持同步

文档门禁 `tests\OmsiLaunch.DocumentationTests` 针对 `OmsiLaunch.Api` 和 `OmsiLaunch.Core` 进行编译，并将上述页面与定义公共接口的代码进行比较：

| 门禁 | 检查内容 |
| --- | --- |
| `docs.cli-flags` | 每个 `CliInput.KnownFlags` 条目都以 `` `/flag` `` 或 `` `/flag:` `` 的形式出现在 CLI 参考中；每个 `CliInput.AcceptedNoEffectFlags` 条目在其所在行标记为 `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT`；每个命令词和每条 `CliInput.HierarchicalRoutes` 路由都与其运行时操作一起出现；每个 `PublicExitCode` 值在退出码表中都有一个 `| n |` 行。 |
| `docs.capabilities` | 每个 `PublicCapabilityRegistry.All` ID、每个 `PublicCapabilityRegistry.PublicRuntimeOperationIds` 条目和每个 `PublicCapabilityClassification` 名称都出现在能力参考中。 |
| `docs.errors` | 每个 `PublicErrorCodes.All` 代码都出现在错误参考中，且 `src\` 或 `tools\OmsiLaunch.Cli\` 中没有任何 `OL_E_*` / `OL_W_*` 字面量缺失于 `PublicErrorCodes`。 |
| `docs.public-api` | `OmsiLaunch.Api` 的每个导出类型、每个枚举值和每个 `IOmsiLaunch` 成员都出现在公共 API 参考中，并且五个稳定性词汇全部被使用。 |
| `docs.launchspec` | 从 `LaunchSpec` 可达的每个公共属性及其枚举的每个值都出现在 LaunchSpec 参考中。 |
| `docs.session-profiles` | 每个 `SessionProfileCompiler.SchemaKeys` 键、架构标识符和 `256 KiB` 限制都出现在会话配置档参考中。 |
| `docs.structure` | 导航表中的每个页面都存在。 |
| `docs.links` | `docs\**\*.md`（不含 `docs\localized\`）以及根目录 `*.md` 文件中的每个相对链接都能解析到某个文件或目录。 |
| `docs.localization` | `docs\localized\LOCALIZATION-MANIFEST.md` 中列出的每个语言区域都具备本地化页面集的全部页面；每个页面都保留英文页面的标题、表格和代码块、每个行内代码片段（参数、能力和操作 ID、错误码、键、标识符）以及每个链接，且其相对链接能够解析。 |

该门禁是 `tools\Invoke-OfflineValidation.ps1` 运行的测试套件之一（可用 `-SkipDocs` 跳过）。它离线运行，从不启动 OMSI；当某个参数、路由、能力、错误码、枚举值或公共类型未被记录，或某个链接失效时，门禁会使构建失败。它不检查正文，因此页面在行为描述上仍可能有误；遇到这种情况，请将其作为针对该页面的缺陷报告。

<a id="translations"></a>
## 翻译

`docs\localized\<locale>\` 包含本 `0.1.0-beta4` 文档的完整译文，语言区域为 `pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW` 和 `ja-JP`。页面集、各语言区域根目录以及有意不翻译的页面列在 [`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md) 中。译文保留英文页面中的每个命令、参数、标识符、错误码和示例不变，`docs.localization` 门禁会对此进行检查。英文页面仍是规范性来源：译文与英文页面不一致时，以英文页面和代码为准，译文视为缺陷。
