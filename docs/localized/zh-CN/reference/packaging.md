# 打包与发行布局

<!-- l10n: source=reference/packaging.md source-sha256=6fff256ee354401567ab45240b0f155822e6321212dc63ea33ed6bc938b945ad -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/packaging.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页说明 OmsiLaunch `0.1.0-beta4` 发行包：`tools\New-ReleasePackage.ps1` 生成什么、`release-manifest.json` 的字段、控制器在运行时如何使用清单校验永久插件闭包、如何将包安装到 OMSI 安装根目录以及如何从中删除、使用后 `.omsilaunch` 目录包含哪些内容，以及验证脚本（`tools\Test-ReleaseIdentity.ps1`、`tools\Test-ReleasePresentation.ps1`、`tools\Invoke-OfflineValidation.ps1`）。产品标识来自 `OmsiLaunch.Version.props`。面向用户的安装步骤见[安装](../getting-started/installation.md)；插件闭包在运行时的作用见[永久插件](../concepts/permanent-plugin.md)。

<a id="product-identity-omsilaunchversionprops"></a>
## 产品标识（`OmsiLaunch.Version.props`）

| 属性 | 值 | 用途 |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | 清单 `product`、Windows `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/OmsiModsBR/OmsiLaunch` | Windows `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | 清单 `product_version`、Windows `ProductVersion`、程序集信息版本（`/version`）、公开 ZIP 名称 |
| `OmsiLaunchManagedVersion` | `0.1.0` | 托管程序集版本基数 |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | 程序集版本和 Windows 文件版本 |
| `OmsiLaunchPackageAlias` | `current` | 清单 `package_alias`、暂存文件夹和别名 ZIP 名称 |

`Directory.Build.props` 将 `InformationalVersion` 设置为 `OmsiLaunchProductVersion`，不附带源代码修订号，因此 `OmsiLaunch.exe /version` 输出的正是 `0.1.0-beta4`。

<a id="build-toolsnew-releasepackageps1"></a>
## 构建（`tools\New-ReleasePackage.ps1`）

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]`（默认输出目录为 `artifacts\release`）从已构建的产物暂存一个包。当 `OmsiLaunch-<product_version>.zip` 已存在时，除非指定 `-AllowOverwritePublished`，否则拒绝写入 `artifacts\release`；候选包写入其他目录（离线验证使用 `artifacts\candidate\post-round-a`）。

暂存之前，会检查每个构建输出是否过期。

- **`OmsiLaunch.Native.x86.dll`：按内容检查，而非按时间戳。** 原生构建会写入 `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt`（`.vcxproj` 中的目标 `WriteOmsiLaunchNativeBuildReceipt`）：其中记录所生成 DLL 的 SHA-256（`output=`）以及构建所用每个源文件的 SHA-256（`source=<sha256>|<path>`：`.cpp`、`.rc`、`.vcxproj` 和 `OmsiLaunch.Version.props`）。以下情况下打包会拒绝该 DLL：其哈希不是记录的输出（`does not match its build receipt`：过期或外来的副本，无论时间戳如何）、记录的源文件已更改（`Native source changed after the recorded build`）、有原生源文件未被回执覆盖，或回执缺失。
- **Shim 和托管程序集：按时间戳检查。** 每个产物都不得早于其所属项目的源文件（每个 shim 的 `.cpp`/`.rc`/`.vcxproj`；每个托管程序集自身的项目）。过期的输入会以 `Stale build artifact` 中止打包。

随后，包会在一个全新的、名称唯一的暂存目录（输出目录中的 `.staging-<guid>`）中组装，因此之前运行留下的任何文件都无法进入闭包。只有在下文所有检查都通过后，才会替换之前的 `OmsiLaunch-current` 文件夹和归档文件。

| 来源 | 包中的目标位置 |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`、`nethost.dll` | `OmsiLaunch.exe`、`nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\`（x64）：`OmsiLaunch.Controller.dll`、`.deps.json`、`.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Configuration.dll`、`OmsiLaunch.Content.dll`、`OmsiLaunch.Core.dll`、`OmsiLaunch.Process.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`YamlDotNet.dll` | 根目录 |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`：`OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI `assets\splash\*.bmp`（`PTB`、`ENG`、`DEU`、`FRA`） | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`、`THIRD-PARTY-NOTICES.md` | 根目录 |
| `docs\` 下除 `docs\localized\` 以外的每个文件（英文文档，目录结构相同） | `.omsilaunch\docs\`（因此 CLI 用法文本中输出的路径 `.omsilaunch\docs\reference\cli.md` 确实存在；文档审计 BUG-08）。从 `docs\README.md` 指向仓库根目录摘要（`PUBLIC-API.md` 等）的链接只在源代码仓库中有效。 |
| `docs\localized\LOCALIZATION-MANIFEST.md` 以及该清单中列出的每个语言区域的 `docs\localized\<locale>\**`（`pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW` 和 `ja-JP`） | `.omsilaunch\docs\localized\`（结构相同）；缺少任何已列出的语言区域都会使脚本中止 |

然后，脚本计算每个暂存文件的哈希，在包根目录以**不带** BOM 的 UTF-8 写入 `release-manifest.json`（结果不再取决于 PowerShell 版本），在暂存区上运行 `Test-ReleasePackageIntegrity.ps1`，将每个暂存的插件文件和 `OmsiLaunch.Native.x86.dll` 与其构建输出重新比较，压缩暂存区，**将归档解压到一个全新的临时目录，并根据同一清单校验解压出的闭包**（归档中的清单必须与已校验的清单逐字节相同），然后将暂存区发布为 `OmsiLaunch-current`，将归档发布为 `OmsiLaunch-current.zip`，将其复制为 `OmsiLaunch-<product_version>.zip`（`OmsiLaunch-0.1.0-beta4.zip`），并写入内容为 `<SHA-256>  <file name>` 的 `OmsiLaunch-0.1.0-beta4.zip.sha256`。任何产物缺失都会使脚本中止。该脚本不执行构建；请先运行 `Invoke-OfflineValidation.ps1`（或单独的 `dotnet build` / MSBuild 步骤）。

<a id="package-layout"></a>
## 包布局

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

只有根目录下的产品文件、`plugins\OmsiLaunch.*` 和 `.omsilaunch\` 归产品所有。`plugins\` 下的第三方插件从不被 OmsiLaunch 枚举、复制、计算哈希、删除或还原。

## `release-manifest.json`

| 字段 | 类型 | 含义 |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`；必须与 `PublicCapabilityRegistry.ProtocolVersion` 一致 |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | 每个已知布局（`Omsi23004`、`13ec110a...c028`）的 `build_id` 和 `code_sha256`；是一条快速路径，而非要求 |
| `configuration` | string | `Release` 或 `Debug` |
| `generated_utc` | string | ISO-8601 格式的构建时间 |
| `files[]` | object[] | 每个打包文件的 `path`（正斜杠，相对于包根目录）、`bytes`、`sha256`（大写十六进制） |

清单是数据，从不是可执行的策略：控制器只读取 `plugins/` 条目。读取器接受带或不带 UTF-8 BOM 的文件（在此修正之前由 Windows PowerShell 5.1 写入的清单带有 BOM）。

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## 清单的运行时用途（插件完整性）

每次规划和启动之前，`OmsiLaunchService.LoadArtifacts` 都会构建预期的插件闭包（`RuntimeArtifactSet.Load`，`src\OmsiLaunch.Process\RuntimeDeployment.cs`）：

1. 控制器在 `OmsiLaunch.exe` 旁（`AppContext.BaseDirectory`）查找 `release-manifest.json`。存在时，`ReleaseManifest.TryReadPluginHashes` 提取 `plugins/*` 的哈希（无法作为清单读取该文件时为 `OL_E_RELEASE_MANIFEST_INVALID`）。
2. 计算每个已安装文件 `<root>\plugins\OmsiLaunch.*` 的哈希（SHA-256）并进行比较：
   - 有清单时：与清单中的哈希比较；记录计划诊断信息 `plugin.integrity.reference = manifest`。文件缺失 → `OL_E_PERMANENT_PLUGIN_MISSING`；文件存在但未列出 → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`；哈希不同 → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`（`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`）。
   - 无清单时（开发布局，或省略了清单的安装）：只能检查存在性以及与控制器旁打包副本的自一致性；`plugin.integrity.reference = self`。
3. 失败会将计划标记为不可运行（带详细信息的 `OL_E_RUNTIME_ARTIFACT_MISSING`）或拒绝启动（退出码 `7`）。

会话从不对插件文件进行暂存、快照、还原或删除；闭包是安装实例的永久组成部分。x86 的 `OmsiLaunch.Native.x86.dll` 只从 `plugins\` 加载；托管程序集声明了 `DefaultDllImportSearchPaths(AssemblyDirectory | System32)`。

<a id="installation-into-the-omsi-root"></a>
## 安装到 OMSI 根目录

1. 校验归档：将 `OmsiLaunch-0.1.0-beta4.zip` 与 `OmsiLaunch-0.1.0-beta4.zip.sha256` 进行比对。
2. 将归档**直接解压到 OMSI 安装根目录**（包含 `Omsi.exe` 的目录）。这会放置根目录文件、`plugins\OmsiLaunch.*`（与任何第三方插件并列）以及 `.omsilaunch\`。
3. 将 `release-manifest.json` 保留在 `OmsiLaunch.exe` 旁：它用于启用基于清单的插件完整性校验。清单和二进制文件必须来自同一个包：在旧清单上覆盖新二进制文件（或反之）会导致每次启动都以 `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` 失败。`Test-ReleasePresentation.ps1 -InstallPackage` 现在会将清单与产品文件一起复制；它以前会跳过清单，导致较新的二进制文件旁留有旧清单（Round A RA-007）。
4. 使用 `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>` 以只读方式检查一致性：`installation_comparison.coherent_with_package` 必须为 `true`。
5. 不要将插件二进制文件移到 `.omsilaunch\` 中，也不要重命名 `plugins\OmsiLaunch.*`。
6. 使用 `OmsiLaunch.exe /version`、`OmsiLaunch.exe profiles` 和一次 `/plan` 进行验证（参见[第一个会话](../getting-started/first-session.md)）。

会话从不覆盖已有的 `.omsilaunch\assets\splash\*.bmp` 文件（显式托管的资源集会保留）；通过解压新包来覆盖它们属于用户的有意操作。

<a id="the-omsilaunch-directory-after-use"></a>
## 使用后的 `.omsilaunch` 目录

| 路径 | 创建者 | 生命周期 |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | 包，或在第一次托管启动画面会话时复制 | 持久 |
| `docs\`、`examples\` | 包 | 持久 |
| `session-profiles\<id>\profile.yaml` | 用户 | 持久；参见[会话配置档](session-profiles.md) |
| `diagnostics\<sessionId>-host.log` | 每个会话 | 保留最新 50 个会话；新会话启动时删除较旧的会话前缀文件 |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | `/runtime`、验证工具 | 相同的保留策略（会话前缀） |
| `diagnostics\tray-host.log` | 托盘指示器 | 持久，追加写入 |
| `diagnostics\release-presentation-*.out`、`release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | 持久（不带会话前缀） |
| `journal.json` | 事务 | 从 `Prepared` 存在到 `Restored`；遗留该文件意味着存在待处理的恢复（`/recovery-status`） |
| `backup\<sessionId>\<sha256(path)>.bin` | 事务 | 被触及文件的快照；还原后删除 |

没有数据离开本机。参见[事务与恢复](../concepts/transactions-and-recovery.md)。

<a id="uninstall"></a>
## 卸载

1. 确认没有正在运行的会话（`OmsiLaunch.exe detect`、`OmsiLaunch.exe session status`），且没有待处理的恢复（`OmsiLaunch.exe /recovery-status`；如果 `pending` 为 `true`，请运行 `/recover`），以确保 OMSI 文件已被还原。
2. 删除 `plugins\OmsiLaunch.Plugin.opl`、`plugins\OmsiLaunch.PluginNE.dll`、`plugins\OmsiLaunch.Plugin.dll`、`plugins\OmsiLaunch.Plugin.deps.json`、`plugins\OmsiLaunch.Plugin.runtimeconfig.json`、`plugins\OmsiLaunch.Api.dll`、`plugins\OmsiLaunch.Builds.Omsi23004.dll`、`plugins\OmsiLaunch.Interop.dll`、`plugins\OmsiLaunch.Native.x86.dll`。不要改动其他插件。
3. 删除上文布局中列出的根目录产品文件（`OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.*.dll`、`OmsiLaunch.Controller.*.json`、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md`）。
4. 删除 `.omsilaunch\`（这会删除您的会话配置档和诊断信息）。存在 `journal.json` 时切勿删除该目录。

已完成的会话会还原其所有的每个文件，因此无需进一步清理。OMSI 自身在运行时写入的文件（例如 `options.cfg` 中的 `[last_map]`、缓存、`laststn.osn`、日志文件）属于 OMSI 的正常状态，不会被还原；参见[事务与恢复](../concepts/transactions-and-recovery.md)。

<a id="validation-scripts"></a>
## 验证脚本

| 脚本 | 用途 | 是否触及 OMSI |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | 以“警告视为错误”构建 `OmsiLaunch.sln`，通过 MSBuild 构建三个原生项目（`OmsiLaunch.Native.x86` Win32、`OmsiLaunch.Bootstrapper` x64、`OmsiLaunch.WindowsHost` x64），然后运行每个离线测试套件：`OmsiLaunch.TestHost`、`OmsiLaunch.UnitTests`、`OmsiLaunch.IntegrationTests`、`OmsiLaunch.ProfileTests`、`OmsiLaunch.WindowsUiTests`，以及（除非跳过）`OmsiLaunch.DocumentationTests`，最后运行打包回归测试 `Test-PackagingPipeline.ps1`（使用 `-SkipNative` 时跳过）。输出 `OFFLINE VALIDATION PASSED`/`FAILED`。 | 否 |
| `tools\New-ReleasePackage.ps1` | 过期检查、暂存、清单、完整性自检、ZIP、校验和（见上文）。 | 否 |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | 校验清单是否恰好列出了所有打包文件且大小和 SHA-256 均匹配，所需闭包（三个可执行文件、控制器、包括 `OmsiLaunch.Native.x86.dll` 在内的九个永久插件文件）是否存在，以及配置是否为 `Release`。指定 `-InstallationRoot` 时，它会以**只读**方式将安装实例的产品文件与包进行比较。退出码 `0` = 一致。 | 否（只读） |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | 在 `artifacts\candidate\post-round-a` 下从干净的暂存区生成候选包，并要求暂存的闭包和重新解压的归档都通过完整性检查。证明完整性门禁会拒绝：被篡改的 DLL、被篡改或旧版的 `Native.x86`、过期的插件副本、已删除的已列出文件、意外文件、已更改或格式错误的清单哈希、重复条目（完全相同、仅大小写不同、仅分隔符不同）、父级路径和绝对路径以及无效 JSON；证明打包脚本会拒绝放置在构建输出中的旧版 `Native.x86`（即使时间戳更新）以及源文件已更改的回执；并证明已发布的归档永远不会被覆盖。构建输出会被逐字节还原。由 `Invoke-OfflineValidation.ps1` 运行。 | 否 |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | 将 ZIP 解压到 `artifacts\release\identity-verification`，检查 `product`/`product_version`/`package_alias`，并校验除 `nethost.dll` 和 `YamlDotNet.dll` 以外每个 `.exe`/`.dll` 的 `ProductName`、`CompanyName`、`LegalCopyright`、`FileVersion`、`ProductVersion`，两个 shim 的 `InternalName`/`OriginalFilename`，以及 `OmsiLaunch.exe` 是否带有嵌入图标。 | 否 |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | 针对真实的安装实例验证打包的 Release 可执行文件：清单必须为 `Release`，不得包含 `Debug` 或 `runtime/plugin/` 路径，并且必须安装 `plugins/OmsiLaunch.*`；四个启动画面资源必须存在。运行三个 `/plan` 用例（托管默认、托管自定义资源、`/splash:Unset`）。使用 `-RunOmsi`（需要 `-InstallPackage`）时，它会以 `/observe-seconds` 启动每个用例，在会话期间监视 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_PTB.bmp`，并断言：精确还原、没有 `Omsi.exe`、没有 `journal.json`、退出码 `0`、第三方插件哈希未变、永久插件集合未变。写入 `.omsilaunch\diagnostics\release-presentation-validation.json`。 | 使用 `-RunOmsi` 时是（会话作用域，会被还原） |

两个 `Test-*` 脚本都会读取 `OmsiLaunch.Version.props` 以获知预期版本。
