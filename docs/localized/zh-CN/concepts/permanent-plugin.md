# 永久插件

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../concepts/permanent-plugin.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

OmsiLaunch 通过一个插件从 OMSI 进程内部控制 OMSI。该插件作为产品的一部分，只在 `plugins\OmsiLaunch.*` 下安装一次。会话从不对它进行暂存、复制、快照、还原或删除。本页说明插件闭包包含哪些文件、OMSI 如何加载它、主机如何在每次启动前校验它、主机与插件如何通信（交接、遥测、运行时邮箱），以及在未通过 OmsiLaunch 启动 OMSI 时插件会做什么。来源：`src/OmsiLaunch.Process/RuntimeDeployment.cs`（`RuntimeArtifactSet`、`ReleaseManifest`、三个共享内存存储）、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`、`src/OmsiLaunch.Plugin/PluginRuntime.cs`、`src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`、`src/OmsiLaunch.Api/StartupHandoff.cs` 和 `src/OmsiLaunch.Api/RuntimeControlProtocol.cs`。

<a id="the-closure-9-files"></a>
## 插件闭包（9 个文件）

| `plugins\` 下的文件 | 作用 |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | OMSI 插件描述文件。其内容为 `[dll]`，后跟 `OmsiLaunch.PluginNE.dll`。 |
| `OmsiLaunch.PluginNE.dll` | 由 DNNE 2.0.6 生成的原生 x86 导出 shim。导出 OMSI 的插件 ABI（`PluginStart`、`PluginFinalize`、`AccessVariable`、`AccessTrigger`、`AccessStringVariable`、`AccessSystemVariable`）并承载 .NET 运行时。带有产品版本资源。 |
| `OmsiLaunch.Plugin.dll` | 托管插件（`net6.0-windows`，x86）：`CurrentDnneAdapter`、`PluginRuntime`、`CurrentRuntimeControl`、`CurrentTelemetrySink`。 |
| `OmsiLaunch.Plugin.deps.json` | 插件的 .NET 依赖清单。 |
| `OmsiLaunch.Plugin.runtimeconfig.json` | .NET 运行时配置（框架 `Microsoft.NETCore.App` 6.0，`win-x86`）。 |
| `OmsiLaunch.Api.dll` | 与主机共享的传输格式和公共记录类型。 |
| `OmsiLaunch.Builds.Omsi23004.dll` | 构建配置：可执行文件指纹、全局变量、对象布局、方法地址。 |
| `OmsiLaunch.Interop.dll` | 基于构建配置实现的进程内内存读取器和写入器。 |
| `OmsiLaunch.Native.x86.dll` | 原生桥（C++）：版本（build）验证、无头启动 hook、时间应用、MakeVehicle、PlaceRandomBus、网络纹理抑制、D3D9 设备访问。 |

`RuntimeArtifactSet.Load` 从控制器自身的 `plugins\` 目录推导出此列表：四个具名文件（`.opl`、`PluginNE.dll`、`deps.json`、`runtimeconfig.json`）、该目录中除 `OmsiLaunch.PluginNE.dll` 以外的每个 `OmsiLaunch.*.dll`，以及原生桥。`OmsiLaunch.Plugin.dll` 必须在其中。任意 DLL 从不会被带入 OMSI。发行打包脚本（`tools/New-ReleasePackage.ps1`）恰好写入上述九个文件。

<a id="how-omsi-loads-it"></a>
## OMSI 如何加载插件

1. OMSI 枚举 `plugins\*.opl`，并加载 `OmsiLaunch.Plugin.opl` 中指定的 DLL：`OmsiLaunch.PluginNE.dll`。
2. DNNE shim 在 OMSI 进程内启动由 `OmsiLaunch.Plugin.runtimeconfig.json` 描述的 x86 .NET 6 运行时，并解析 `OmsiLaunch.Plugin.dll` 中的托管导出。
3. OMSI 调用 `PluginStart`。OMSI 可能在启动期间多次调用它；只有第一次调用生效（`Interlocked.Exchange` 保护），因为一次成功的启动会占用一个一次性的原生 hook。之后的调用会立即返回。
4. `PluginStart` 首先检查 `OMSILAUNCH_INTERNET_TEXTURES_MODE`：当其为 `Disabled` 时，应用原生下载器抑制（`internet-textures.suppressed` 或 `internet-textures.suppression.failed`）。
5. `PluginRuntime.Start` 读取交接数据（见下文），在进程内验证版本（build），挂载无头启动 hook，打开运行时邮箱，并通过 `SetTimer` 回调在 OMSI 的 UI 线程上安排世界启动。运行时命令从不在 IPC 工作线程上执行；所有操作都在定时器回调中、在 OMSI 原有的 UI 线程上运行。
6. `PluginFinalize` 销毁定时器，关闭运行时（`NativeD3DShutdown`），并还原网络纹理补丁。

`AccessVariable`、`AccessTrigger`、`AccessStringVariable` 和 `AccessSystemVariable` 导出为空实现；OmsiLaunch 不使用 OMSI 的脚本变量插件通道。

<a id="integrity-validation-before-every-start"></a>
## 每次启动前的完整性校验

`RuntimeArtifactSet.ValidateInstalled` 在 `StartSessionAsync` 期间（早期恢复之后、准备事务之前）以及 `PlanSessionAsync` 期间（仅检查存在性，通过 `LoadArtifacts`）运行。计划诊断信息 `plugin.integrity.reference` 报告所使用的参照。

| 情形 | 参照 | 逐文件检查 | 错误 |
| --- | --- | --- | --- |
| `OmsiLaunch.exe` 旁存在 `release-manifest.json`（已安装的包） | `manifest` | 已安装的文件必须存在，且其 SHA-256 必须等于清单中 `plugins/<name>` 的条目 | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（清单中没有某个必需文件的条目）、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| 无清单（开发布局） | `self` | 存在性加自一致性：已安装文件的哈希必须与控制器自身 `plugins\` 目录中的文件一致 | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| 清单不可读或格式错误（缺少 `files` 数组，条目缺少 `path`/`sha256`） | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| 控制器目录中的闭包不完整 | | | `OL_E_RUNTIME_ARTIFACT_MISSING`（计划不可运行）；当缺少 `plugins\OmsiLaunch.Plugin.opl` 或 `OmsiLaunch.Native.x86.dll` 时，CLI 还会报告 `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` |

只使用清单中的 `plugins/` 条目；清单是数据，从不是策略。清单由发行打包脚本生成，包含每个暂存文件的 SHA-256。当控制器从 OMSI 根目录运行时（发行布局），源目录和目标目录是同一个目录；因此在没有清单的情况下，检查会退化为存在性和自一致性检查，这就是发行包始终带有 `release-manifest.json` 的原因。不匹配时的修复方法：重新安装该包，使 `plugins\` 与 `release-manifest.json` 一致。

在打补丁之前，插件会针对已加载的映像解析并校验版本构建目录中的每个原生目标，并将校验后的表交给不含任何 OMSI 地址的 `OmsiLaunch.Native.x86.dll`（遥测 `native.capabilities`）。`boot.headless-start` 或世界模式能力不可用的启动会以 `plugin.capability.unresolved`（`OL_E_CAPABILITY_UNRESOLVED`）失败；处于意外状态的实时 Start 窗体会以 `plugin.build.invalid`（`OL_E_BUILD_VALIDATION_FAILED`）失败。参见[兼容性](../reference/compatibility.md)。

<a id="host-to-plugin-environment-variables"></a>
## 主机到插件：环境变量

`CreateProcessW` 以父进程环境加上以下变量启动 `Omsi.exe`：

| 变量 | 值 | 使用方 |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | 会话 GUID（`D` 格式） | `CurrentRuntimeControl` 用它标记 D3D 句柄（`N` 格式） |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` 以只读方式打开此映射 |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`、`Disabled` 或 `Override` | `PluginStart`（只有 `Disabled` 有进程内效果） |

这三个映射由主机在进程启动之前创建（`CurrentStartupHandoffStore`、`CurrentTelemetryStore`、`CurrentRuntimeCommandStore`），并在会话的生命周期任务结束时释放。它们是具名内核对象，使用发起启动的用户的默认 DACL；同一用户的任何进程都可以打开它们（已接受的同用户信任模型，参见[已知限制](../reference/known-limitations.md)）。

<a id="the-startup-handoff"></a>
## 启动交接

一个内存映射、只读、固定布局的记录（`StartupHandoffWire`，魔数 `OLSH`，版本 4；读取器仍接受版本 3）。64 字节的头部包含魔数、版本、头部大小、总大小、会话 GUID、载荷大小以及载荷的 SHA-256。载荷包含 `BuildProfileId`、`MapIdentity`、`EntrypointIdentity`、`SituationIdentity`（带长度前缀的 UTF-8）、`PresentedEntrypointIndex`、`WorldMode`、标志位（`HeadlessStart`、`PlayerVehicleEnabled`）、`DateMode` 和 `TimeMode`。插件会重新计算载荷的哈希，并拒绝任何不匹配（`plugin.handoff.invalid`，主机错误 `OL_E_PLUGIN_PROTOCOL_MISMATCH`）。大于 1 MiB 或大小不一致的映射也会以同样方式被拒绝。

只有当 `WorldMode` 为 `NewMap` 或 `SavedSituation`、`HeadlessStart` 已设置、`PlayerVehicleEnabled` 未设置、日期和时间模式均为 `Unset`，且已保存情景指定了其 `.osn` 时，插件才接受交接数据。其他任何情况均为 `plugin.request.unsupported`（主机错误 `OL_E_CAPABILITY_UNAVAILABLE`）。主机始终设置 `HeadlessStart`。

<a id="telemetry-slot"></a>
## 遥测槽

`OmsiLaunch.Telemetry.<session>` 是一个事件环（Beta 4，`TelemetryRingWire`）：64 字节头部（魔数 `OLTR`、版本 1、槽数、槽大小、已提交序号）和 64 个带编号的 2048 字节槽，每个槽保存一个 UTF-8 JSON 事件 `{ "name": ..., "data": { ... }, "tick": <ms> }`（`tick` 是插件的单调会话时钟）。插件是唯一的写入方：先使某个槽失效，写入该槽，提交该槽的序号，然后发布环的序号。主机每 100 ms 按顺序读取自上次读取以来提交的每个事件；复制期间被覆盖的槽会被计为丢失，绝不会以撕裂状态交付。落后超过 64 个事件的主机会收到运行时事件 `telemetry.lost`，并带有确切的计数。大于一个槽的事件会被指明它的 `telemetry.oversized` 替换。事件被追加到 `SessionStatus.RuntimeEvents`（仅保留最近 256 个），并驱动语义生命周期（`plugin.started`、`world.starting`、`gameplay.entered`、模块事件和失败事件）。

<a id="runtime-command-mailbox"></a>
## 运行时命令邮箱

`OmsiLaunch.Runtime.<session>` 是一个 64 KiB 的单次在途（single-flight）邮箱：`state`（偏移 0 处的 int32：0 空闲，1 已请求，2 已响应）、`length`（偏移 4 处的 int32）、偏移 8 处的信封（envelope）。信封是 `RuntimeCommandWire` 记录（魔数 `OLRC`，版本 1，72 字节的头部，包含种类、总长度、会话 GUID、请求 ID、载荷长度以及 UTF-8 JSON 载荷的 SHA-256）。插件从其 UI 线程定时器轮询邮箱（世界加载后为 50 ms），在该线程上执行命令，并且只有当槽中仍是同一个请求 ID 时才发布响应；主机因超时而放弃的请求永远不会得到应答。过大的响应会被替换为类型化的 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 错误。详细信息和超时设置参见[运行时控制](../reference/runtime-control.md)。

<a id="dll-search-policy"></a>
## DLL 搜索策略

`OmsiLaunch.Plugin`、`OmsiLaunch.Interop`、`OmsiLaunch.Process` 和 CLI 程序集声明了 `[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]`。原生导入（`OmsiLaunch.Native.x86.dll`、`user32.dll`、`kernel32.dll`）只从程序集自身所在目录（`plugins\`）或 Windows 系统目录解析；从不探测 OMSI 根目录和 `PATH`。因此 `OmsiLaunch.Native.x86.dll` 只从 `plugins\` 加载，不会从其他任何位置加载。

<a id="when-omsi-is-started-without-omsilaunch"></a>
## 未通过 OmsiLaunch 启动 OMSI 时

由于闭包是永久性的，OMSI 在每次启动时都会加载 `OmsiLaunch.PluginNE.dll`，包括从 Steam 或桌面启动。在这种情况下：

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` 不存在，因此不会应用下载器补丁。
- `OMSILAUNCH_HANDOFF_NAME` 不存在，因此 `PluginRuntime.Start` 发出 `plugin.handoff.invalid` 并返回 `false`。未设置 `OMSILAUNCH_TELEMETRY_NAME` 时，`CurrentTelemetrySink.Emit` 会立即返回，因此不会向任何地方写入内容。
- 没有版本（build）验证，没有原生 hook，没有邮箱，没有定时器。插件保持加载但处于惰性状态；OMSI 的行为就像该插件不存在一样。
- OMSI 退出时，`PluginFinalize` 会调用原生还原例程和 `NativeD3DShutdown`，在未安装任何内容时两者都是空操作。

因此，正常运行 OMSI 时无需删除 `.opl`。

<a id="non-interference-with-third-party-plugins"></a>
## 不干扰第三方插件

会话从不枚举、计算哈希、复制、删除或还原 `plugins\` 中的其他文件。插件不使用 OMSI 的 `AccessVariable` 通道，也不触及其他插件的状态。仅有的进程内补丁是：经过构建配置分析的无头启动 hook（为会话挂载的一次性 VMT 重定向）、可选的网络纹理抑制（在 `PluginFinalize` 中还原），以及用于纹理生命周期跟踪的 D3D9 设备 `Reset` 拦截。

<a id="difference-from-omsihook"></a>
## 与 OmsiHook 的区别

OmsiLaunch 对 OmsiHook 或任何 OmsiHook 二进制文件**没有运行时依赖**：唯一引用的包是 `DNNE` 2.0.6 和 `YamlDotNet` 15.1.2，产品中任何地方都没有 `using OmsiHook`，也没有对 OmsiHook DLL 的 P/Invoke。OmsiLaunch 与 OmsiHook 共享的是派生知识：对象布局和若干读取包装器是根据固定版本的 OmsiHook 检出（`space928/Omsi-Extensions`，提交 `7687b6623f5f74b4419695257bd2a4eef54dd93e`，LGPL-3.0-only），针对确切的 `Omsi23004_692EBFBF` 可执行文件进行核对得到的。署名和许可条款见 `THIRD-PARTY-NOTICES.md`，逐文件复用矩阵见 `third_party/OMSIHOOK-REUSE-MATRIX.md`。OmsiHook 注入一个独立进程并暴露原始指针；OmsiLaunch 在进程内运行，只暴露不透明的、会话作用域的句柄，并从公共结果中剔除所有原生地址（参见[能力](../reference/capabilities.md)）。
