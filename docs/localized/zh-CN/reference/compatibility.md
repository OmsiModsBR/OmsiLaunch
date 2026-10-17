# 兼容性

<!-- l10n: source=reference/compatibility.md source-sha256=9f3a7654d57410845e20c92eccc746868cbac665b4d937581f16c3818d7b7b88 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/compatibility.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

OmsiLaunch 适用于已安装的 **OMSI 2**。它不要求某一个特定的 `Omsi.exe` 文件：它在 OMSI 内部作出的每一个原生假设（函数、补丁位置、类、VMT 槽、全局变量、字段偏移）在使用之前都会在进程内被单独解析并校验，只有当某项能力的所有假设都已校验时，该能力才可用。本页说明“已安装 OMSI 2”的含义、如何识别版本（build）、如何解析能力，以及对操作系统和运行时的要求。来源：`src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`、`src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`、`src/OmsiLaunch.NativeResolution/`、`src/OmsiLaunch.Plugin/PluginNativeTargets.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`。

<a id="requirement-omsi-2-installed"></a>
## 要求：已安装 OMSI 2

只有当安装不是 OMSI 2 安装时，计划才会以 `OL_E_UNSUPPORTED_BUILD` 被拒绝：

| 检查 | 在以下情况被拒绝 |
| --- | --- |
| `Omsi.exe` | 安装根目录中缺失 |
| 映像 | 不是 32 位 x86 PE 映像 |
| 版本 | 其版本资源不带有 OMSI 2 版本（2.x） |
| 安装 | 根目录中没有 `maps\` 或 `Vehicles\` 目录 |

可执行文件的 SHA-256、大小和确切版本**不是**兼容性判断标准。它们会作为诊断信息（`omsi.build.detected`、`OmsiLaunch.exe profiles`）记录，并作为证据使用。

<a id="build-identity-and-capability-resolution"></a>
## 版本标识与能力解析

| 概念 | 含义 |
| --- | --- |
| 代码标识 | 可执行文件主代码节的 SHA-256。已知的标识会选择已知布局，作为快速路径。仅文件头不同的变体（例如 Large Address Aware 副本）共用同一个代码标识。 |
| 已知布局 | `Omsi23004`：OMSI 2.3.004（文件版本 2.2.032），代码标识 `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`。[状态页面](../status/runtime-validation-status.md)中的每一项运行时验证都是在该布局上进行的。 |
| 原生目标 | 原生边界的一个假设，连同其守卫一起声明在版本目录中。 |
| 能力 | 一组目标，例如 `world.new-map` 或 `time.read`。仅当每个目标都已解析**并**经过校验时才可用。 |

在 OmsiLaunch 修补任何内容之前，插件按如下方式解析一个目标：

1. **已知标识：**首先尝试已知布局的参考地址，并且它必须仍然通过该目标的守卫。
2. **否则，或守卫失败时**，尝试与版本无关的策略：
   - Delphi RTTI 类名（并检查 `vmtSelfPtr`）；
   - 类的已发布方法、字段和属性（RTTI 表）；
   - 持有已解析方法的 VMT 槽；
   - 带掩码的代码签名，必须恰好匹配一次（依赖布局的操作数为通配符），并从字段所属类的某个方法内部捕获地址、全局变量（直接或通过其指针单元）或字段位移。
3. **没有唯一匹配且通过守卫的结果：**该目标保持未解析。**不会回退到固定地址。**

守卫：函数和补丁位置使用带掩码的入口字节，被修补的调用使用调用目标，类使用 `vmtSelfPtr` 和 RTTI 类名，VMT 槽使用其持有的方法，全局变量使用可写数据节。运行时读取器所读取的对象布局（车辆、人物对象、天气、时刻表记录等）目前还没有与版本无关的结构校验。它们只针对已知的代码标识进行校验，因此读取它们的能力在代码标识未知的版本上不可用。

在已知布局上，每项能力都能解析。在代码标识未知的版本上，其目标可通过上述策略解析的能力（headless 启动、NEW_MAP、已保存情景、网络纹理（Internet Textures）抑制、时间的读取和设置、D3D）可以可用，并且每一项都单独校验；依赖布局的读取不可用。

<a id="where-compatibility-is-decided"></a>
## 在何处判定兼容性

| 阶段 | 发生的事情 |
| --- | --- |
| 规划（`PlanSessionAsync`、`/plan`） | OMSI 2 检查（`omsi.omsi2`，否则为 `OL_E_UNSUPPORTED_BUILD`）。文件映像作为**预测**被解析：所需的原生能力带有 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`，并带有警告 `OL_W_CAPABILITY_PENDING`。未知的代码标识会添加警告 `OL_W_BUILD_UNRECOGNIZED`。警告不会使计划变为不可运行。 |
| 启动（`StartSessionAsync`） | 重新规划该 spec；不再可运行的计划会以 `OL_E_PLAN_NOT_RUNNABLE` 被拒绝。 |
| 进程内（插件启动） | 解析已加载的映像（具有权威性；受 DRM 保护的可执行文件可能与其文件不同）。报告以遥测 `native.capabilities` 发送。如果 `boot.headless-start` 和世界模式能力（`world.new-map` 或 `world.saved-situation`）不可用，会话会在任何钩子之前以 `OL_E_CAPABILITY_UNRESOLVED` 失败，并还原文件。如果它们可用，但实时 Start 窗体不处于预期状态（例如另一个插件对其打了补丁），会话会以 `OL_E_BUILD_VALIDATION_FAILED` 失败。 |
| 运行时操作 | 其能力不可用的操作会以 `OL_E_CAPABILITY_UNRESOLVED`（`Data["capability"]`）被拒绝，且不会触及 OMSI 内存。 |

`OmsiLaunch.exe profiles --json` 会报告安装（`is_omsi2`、`build_id`、`identity`、文件和代码哈希、`file_variant`）、已知布局，以及依据文件预测的能力。

<a id="operating-system-and-architecture"></a>
## 操作系统与架构

`CurrentWindowsX64Platform.Detect` 计算 `RuntimePlatformInfo`。只有在以下条件全部满足时，当前平台才受支持：

| 要求 | 检查 | 不满足时的错误 |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 或更高版本 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016+） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 位 Windows 和 64 位宿主进程 | `OSArchitecture == X64` 且 `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 可写的安装实例 | 根目录存在、不是只读，并且包含 `plugins\` | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` 还将 `OmsiArchitecture` 和 `PluginArchitecture` 报告为 `X86`（OMSI 是 32 位进程；插件闭包为 x86，在 WOW64 下运行），并报告 `LegacyPlatform=false` 和 `Wow64Available`。即使存在 x64 仿真，也不支持 ARM64 Windows，因为宿主进程本身必须是 x64。

<a id="net-requirements"></a>
## .NET 要求

| 组件 | 运行时 | 说明 |
| --- | --- | --- |
| 控制器（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6，x64 | 原生引导程序通过随包提供的 `nethost.dll` 借助 `hostfxr` 定位运行时。缺少运行时的情况由 shim 报告（退出码 100-106；见 [CLI](cli.md) 和[退出码](exit-codes.md)）。 |
| 插件闭包（通过 `OmsiLaunch.PluginNE.dll` 加载的 `plugins\OmsiLaunch.Plugin.dll`） | .NET 6，**x86**（`net6.0-windows`、`win-x86`），由 DNNE 2.0.6 在 `Omsi.exe` 内部托管 | 需要在计算机上安装 x86 .NET 6 Desktop/Core 运行时；仅有 64 位运行时不足以运行插件。 |
| 原生桥（`plugins\OmsiLaunch.Native.x86.dll`） | 原生 x86 | 仅从 `plugins\` 加载（见[永久插件](../concepts/permanent-plugin.md)）。它不含任何 OMSI 地址：它从插件接收经过校验的目标表。 |

<a id="legacy-platforms"></a>
## 旧版平台

Windows 7、Windows 8.x、Windows XP 以及其他 NT 6 及更早的系统不在当前支持范围内。`RuntimePlatformInfo.LegacyPlatform` 始终为 `false`，也不存在旧版适配器；该字段和 `IPluginNativeServices` 接缝的存在，只是为了将来可以在不更改公共 API 的情况下添加旧版适配器（见 `docs/adr/ADR-0010-Legacy-Portability-Boundary.md`）。本发行版中没有任何内容能在这些系统上运行。

<a id="steam-and-large-address-aware-notes"></a>
## Steam 与 Large Address Aware 说明

- 仅文件头不同的 Large Address Aware 副本与其原件具有相同的代码标识，并使用同一个已知布局。
- Steam 可执行文件在磁盘上受 DRM 保护，因此其文件可能与其已加载的映像不同：计划可能会报告 `OL_W_CAPABILITY_PENDING`，由进程内解析决定。
- Steam 会自行启动 OMSI；会话必须通过 `OmsiLaunch.exe` 启动，才会存在交接数据。从 Steam 启动时，永久插件保持非活动状态（没有交接，没有钩子）。

<a id="related-pages"></a>
## 相关页面

- [已知限制](known-limitations.md)
- [运行时验证状态](../status/runtime-validation-status.md)
- [安装](../getting-started/installation.md)
