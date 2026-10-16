# 安装

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../getting-started/installation.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页说明 OmsiLaunch `0.1.0-beta4` 的运行要求、它支持哪个 OMSI 版本（build）、如何将发行包安装到 OMSI 安装根目录，以及如何在启动会话之前用 `/version` 和 `/plan` 校验安装。包的内容在[打包](../reference/packaging.md)中规定；首次启动在[第一个会话](first-session.md)中说明。

<a id="requirements"></a>
## 要求

| 要求 | 详细说明 | 缺失时的失败表现 |
|---|---|---|
| Windows 10 或更高版本，64 位 | 控制器检查 `Environment.OSVersion.Version.Major >= 10`、x64 操作系统以及 x64 进程。 | 计划不可运行，报告 `OL_E_UNSUPPORTED_OPERATING_SYSTEM`（或 `OL_E_UNSUPPORTED_OS_ARCHITECTURE`），退出码 `1`/`3`。 |
| .NET 6 Desktop Runtime，**x64** | `OmsiLaunch.Controller.runtimeconfig.json` 需要 `Microsoft.NETCore.App` 6.0 和 `Microsoft.WindowsDesktop.App` 6.0（托盘指示器使用 Windows Forms）。shim 通过 `nethost.dll` 定位它。 | `OmsiLaunch.exe` 在产生任何输出之前以 shim 代码 `102`..`106` 退出；`OmsiLaunchW.exe` 显示 `OmsiLaunch could not start the .NET host (code N).`。 |
| .NET 6 Runtime，**x86** | `plugins\OmsiLaunch.Plugin.runtimeconfig.json` 需要 x86 版 `Microsoft.NETCore.App` 6.0，因为插件运行在 32 位的 `Omsi.exe` 内部。x86 .NET 6 Desktop Runtime 安装包同样满足此要求。 | 插件无法在 OMSI 内启动；会话无法到达 `Running`（`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`），退出码 `1`，文件已还原。 |
| 已安装 OMSI 2 | 32 位 `Omsi.exe`，版本为 OMSI 2（2.x），且根目录中有 `maps\` 和 `Vehicles\`。不要求特定的可执行文件哈希；OMSI 2.3.004 是已知且经运行时验证的布局，在其他版本（build）上，每项能力都在进程内解析并校验。 | `OL_E_UNSUPPORTED_BUILD`；计划不可运行，退出码 `1`。在该版本上不可用的能力会以 `OL_E_CAPABILITY_UNRESOLVED` 失败。见[兼容性](../reference/compatibility.md)。 |
| 可写的安装根目录 | 事务会写入 `.omsilaunch\`、`GUI\` 和 `Texture\` 下的覆盖层（overlay）以及 `options.cfg`，并将它们还原；根目录必须对当前用户可写（在没有相应权限时应避免使用 `Program Files`）。 | `OL_E_INSTALLATION_NOT_WRITABLE`，退出码 `1`。 |
| 每个安装实例一个用户、一个所有者 | 安装租约 `Local\OmsiLaunch.Installation.<sha256(root)>` 和控制管道都按登录会话划分。 | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`，退出码 `7`。 |

两个运行时都需要从 Microsoft 单独下载；请为 .NET 6 安装 x64 Desktop Runtime 和 x86 Runtime（或 x86 Desktop Runtime）。不需要其他组件。任何数据都不会离开本机。

<a id="confirm-the-omsi-build"></a>
## 确认 OMSI 版本

将 `<OMSI_PATH>` 替换为您的 OMSI 2 目录（例如 `C:\OMSI 2`）。

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

哈希必须是上面列出的两个之一。安装后，`OmsiLaunch.exe profiles` 会打印同样的列表及其验证状态。

<a id="install-the-package"></a>
## 安装包

1. 下载 `OmsiLaunch-0.1.0-beta4.zip` 和 `OmsiLaunch-0.1.0-beta4.zip.sha256`；校验校验和（`Get-FileHash` 的结果必须等于 `.sha256` 文件中的值）。
2. 将压缩包**直接解压到 OMSI 安装根目录**（包含 `Omsi.exe` 的文件夹）。压缩包的布局正是针对该根目录的：
   - 根目录下的 `OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.Controller.dll` 及其他 `OmsiLaunch.*.dll` 控制器程序集、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md`；
   - 永久插件闭包 `plugins\OmsiLaunch.*`（9 个文件），与您现有的插件并列放置，现有插件从不会被触及；
   - `.omsilaunch\`，包含启动画面资源、离线文档和示例。
3. 将 `release-manifest.json` 保留在 `OmsiLaunch.exe` 旁边。正是它使每次启动都能按 SHA-256 校验已安装的插件文件（`plugin.integrity.reference = manifest`）；没有它时只检查文件是否存在及其自洽性（`plugin.integrity.reference = self`）。
4. 不要移动或重命名 `plugins\OmsiLaunch.*` 下的任何内容，也不要将插件二进制文件放入 `.omsilaunch\`。

升级是同样的操作：在没有会话运行且没有待处理的恢复（`OmsiLaunch.exe /recovery-status`）时，将新包解压覆盖旧文件。插件哈希和清单必须始终来自同一个包（否则报告 `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`）。

<a id="verify"></a>
## 校验

在 OMSI 根目录下运行（安装参数默认为包含 `OmsiLaunch.exe` 的目录）：

```text
OmsiLaunch.exe /version
```
预期结果：`"version": "0.1.0-beta4"`、`"protocol_version": "0.1"`、`"supported_family": "OMSI_2"`；退出码 `0`。退出码 `102`..`106` 表示缺少 x64 .NET 6 运行时或包不完整。

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
预期结果：先是受支持的哈希，然后是在此安装实例中发现的地图；退出码 `0`。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
预期结果：`Plan: READY profile=Omsi23004`（在已知布局上；否则为 `unrecognized`），退出码 `0`（可以使用 `/list:Maps` 中的任意地图标识；入口点索引必须是该地图所呈现的索引之一，见 `/list:Entrypoints /map:<identity>`）。使用 `--json` 时，计划会列出 `TouchedFiles`（启动画面覆盖层）、`PlannedMutations`、`RequiredCapabilities`（`STATICALLY_VALIDATED`，在已知布局上原生启动能力为 `RESOLVED_KNOWN_LAYOUT`）、`Diagnostics`（包括 `plugin.integrity.reference`）和 `IsRunnable`。`Plan: NOT RUNNABLE` 且退出码为 `1` 时，原因会在 `Diagnostics` 中给出（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_RUNTIME_ARTIFACT_MISSING`……）。规划从不启动 OMSI，也从不写入安装目录。

<a id="where-things-live-afterwards"></a>
## 安装后的文件位置

| 路径 | 内容 |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | 每个会话的主机跟踪记录（保留最新的 50 个会话） |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | 托盘指示器日志文件 |
| `<root>\.omsilaunch\journal.json`、`backup\<sessionId>\` | 仅在存在待处理的事务时存在；见[事务与恢复](../concepts/transactions-and-recovery.md) |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | 您预定义的会话配置档；见[会话配置档](../reference/session-profiles.md) |
| `<root>\.omsilaunch\assets\splash\` | 托管启动画面资源 |
| `<root>\.omsilaunch\docs\` | 本文档的离线版本（从 `README.md` 开始阅读；CLI 参考为 `reference\cli.md`） |
| `<root>\.omsilaunch\examples\` | LaunchSpec 和会话配置档示例 |

<a id="uninstall"></a>
## 卸载

停止所有会话，运行 `OmsiLaunch.exe /recovery-status`（如有待处理的事务，再运行 `/recover`），然后删除根目录下的产品文件、`plugins\OmsiLaunch.*` 和 `.omsilaunch\`。详见[打包](../reference/packaging.md)。

<a id="next"></a>
## 下一步

[第一个会话](first-session.md) · [CLI 参考](../reference/cli.md) · [已知限制](../reference/known-limitations.md)
