# 运行时模块

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/runtime-modules.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

运行时模块（runtime module）为受管理的 OMSI 会话增加行为：这是一项随会话运行的功能（例如某张地图的 HUD，或加载优化），但不会成为 OmsiLaunch 自身的一部分。桥接层加载模块，只提供模块所声明的内容，并在会话结束或模块失败时移除模块持有的一切。本页是运行时模块的规范性参考，对应 `src/OmsiLaunch.Runtime.Modules`（清单、设置、资格判定、生命周期宿主）、规划器、控制器的会话程序（`ModuleSessionPrograms`、`ModuleSessionProcess`）、插件的模块环境，以及 [CLI](cli.md) 中与模块相关的参数。

稳定性：`EXPERIMENTAL`。桥接协议 1 已在 B 轮中经运行时验证（托管模块、内容激活、操作、`disable-module` 和 `fail-session`；带 IAT 钩子的原生组件；参见[运行时验证状态](../status/runtime-validation-status.md)）。桥接协议 2（设置、默认激活、设置文件、会话程序、插件冲突、通知、`get_value`）仅经离线验证。离线门禁：`tests/OmsiLaunch.ModuleTests` 中的 `module.*` 门禁（清单、完整性、选择与激活、设置、设置文件、插件冲突、通过其作业启动的会话程序、在每个失败策略下于每个阶段注入的失败、原生组件生命周期、清理、交接）、`plugin-runtime.modules-lifecycle`（由插件环境加载的真实托管模块）、`runtime-modules.native-component`（挂钩某个导入并通过桥接层读取值的真实原生组件）、`runtime-modules.plugin-conflict` 以及 `native-hooks.self-check`（钩子引擎）。桥接协议 3（Beta 5：进程内目录操作、tick、输入捕获、社区内容输入、带计划时快照的可选固定项、`module pack`、辅助作业选项、OMSI 版本）仅经过离线验证：`tests/OmsiLaunch.ModuleTests` 的 `module.*` 门禁（`manifest.protocol-3`、`manifest.optional-pins`、`catalog.snapshot`、`content.realmap-package`、`packer.pins`、`handoff.v8-snapshot`、`lifecycle.runtime-operations`、`lifecycle.tick.*`、`lifecycle.input-capture`、`lifecycle.auxiliary-options`、`lifecycle.snapshot-before-components`）、插件集成测试的 `runtime-modules.protocol-3`、`runtime-modules.auxiliary-job`、`ui-pause.fails-closed` 和 `ui-input.hook-self-check`，以及单元测试的 `omsi-version.literal`。暂停菜单接管和输入捕获尚未在真实的 OMSI 会话中观察到。

加载是开放的：安装在 `plugins\OmsiLaunch.Modules` 之下的任何模块都可以被加载，无论由谁制作。OmsiLaunch 不为模块在 OMSI 内部的行为担保。它所保证的是[边界](#security-boundary)：模块绝不在 OmsiLaunch 进程内运行，并且 OmsiLaunch 提供的任何机制都不能让模块超出 OMSI 自身的权限范围。

<a id="where-modules-live"></a>
## 模块的存放位置

每个模块是永久插件文件夹之下的一个目录：

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

目录名必须与模块的 `id` 相同。模块由其分发者安装；会话绝不会在此处写入、做快照或还原文件，模块也绝不会通过 OmsiLaunch 更改安装文件（清单无法声明安装覆盖层）。模块的可写数据位于其[数据目录](#data-directory)。

<a id="manifest-modulejson"></a>
## 清单（`module.json`）

架构（schema）为 `omsilaunch.runtime-module/v1`。未知的键会被拒绝，因此为较新架构编写的清单会被拒绝，而不是只被理解一半。所有路径都相对于模块目录（绑定内容的路径相对于 OMSI 根目录），使用 `\` 或 `/`，不得包含 `..` 或盘符。

| 键 | 是否必需 | 含义 |
| --- | --- | --- |
| `schema` | 是 | `omsilaunch.runtime-module/v1`。 |
| `id` | 是 | 模块 ID：小写字母、数字和 `-`，2 到 48 个字符，以字母或数字开头。同时也是其目录名和操作命名空间。 |
| `version` | 是 | `major.minor.patch`，可选 `-suffix`。 |
| `name` | 否 | 显示名称（最多 128 个字符）。 |
| `bridge_protocol` | 是 | `{ "min": n, "max": n }`：模块可配合使用的桥接协议。此构建提供协议 `1`、`2` 和 `3`；模块的范围包含其中之一即被接受。 |
| `activation` | 否 | `requested`（默认）：会话请求时加载。`content`：会话运行所绑定的地图时也会加载（需要 `content_binding`）。`default`（协议 2）：除非被禁用，否则在每个会话中加载（参见[为会话选择模块](#choosing-modules-for-a-session)）；不能带有 `content_binding`。 |
| `required_capabilities` | 否 | 模块缺少就无法工作的能力（参见[能力](#capabilities)）。 |
| `optional_capabilities` | 否 | 模块在可用时使用的能力；通过 `IsCapabilityAvailable` 检查。 |
| `content_binding` | 否 | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`：模块只在该地图上运行，且这些文件须保持不变。 |
| `assets` | 否 | 模块读取的文件，每项为 `{ "path", "sha256" }`。自协议 3 起，`sha256` 为可选（参见[完整性](#integrity)）。 |
| `components` | 是 | 恰好一个 `managed` 组件 `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }`；可选一个 `native` 组件（`.dll`）、`auxiliary` 组件（`.exe`），以及在协议 2 下的一个 `host` 组件（`.exe`，即模块的[会话程序](#session-programs)）。自协议 3 起，组件的 `sha256` 为可选，且 `auxiliary` 组件可带有 `priority`（`normal`、`below-normal` 或 `idle`）和 `memory_limit_mb`（64 至 65536）：该程序的作业对象从其第一条指令起就应用它们（参见[辅助程序](#auxiliary-programs)）。 |
| `failure_policy` | 是 | `fail-session`、`disable-module` 或 `degrade`（参见[失败](#failures)）。 |
| `operations` | 否 | 模块在会话运行期间公开的操作：`{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`。 |
| `settings` | 否 | 协议 2。模块接受的设置项（参见[设置](#settings)）。 |
| `conflicts` | 否 | 协议 2。`{ "plugins": [ "<file>.dll" ] }`：不得与该模块同时运行的 OMSI 插件 DLL（参见[与 OMSI 插件的冲突](#conflicts-with-omsi-plugins)）。 |
| `runtime_operations` | 否 | 协议 3。模块在进程内运行的公共能力目录中的操作，例如 `["time.read", "camera.set"]`（参见[进程内操作](#operations-in-process)）。需要 `bridge.runtime` 能力。 |
| `tick_ms` | 否 | 协议 3。模块希望每隔 `tick_ms` 毫秒收到一次周期性回调：50 的倍数，范围 50 至 1000（参见 [tick](#tick)）。需要 `bridge.tick` 能力。 |
| `content_inputs` | 否 | 协议 3。模块读取的、由桥接层定位的社区内容：`[ { "kind": "realmap-package", "required": false } ]`（参见[社区内容](#community-content)）。 |
| `timeouts` | 否 | 以毫秒计的预算：`arm_ms`、`gameplay_ready_ms`、`stop_ms`（各默认 5000，最大 30000）；在协议 2 下，会话程序另有 `prepare_ms`（默认 120000，最大 1800000）和 `host_stop_ms`（默认 10000，最大 60000）。操作默认为 2000 ms。 |
| `diagnostics` | 否 | `{ "trace": true\|false }`。 |

使用协议 2 功能（`settings`、`conflicts`、激活方式 `default`、`host` 组件、`prepare_ms` 或 `host_stop_ms`）的清单必须声明 `bridge_protocol.min` 为 2。使用协议 3 功能（`runtime_operations`、`tick_ms`、`content_inputs`、`bridge.runtime`、`bridge.tick` 或 `bridge.input` 能力、辅助程序的 `priority` 或 `memory_limit_mb`，或没有 `sha256` 的组件或资源）的清单必须声明 `bridge_protocol.min` 为 3。

`components` 和 `assets` 所列的每个文件都可以由其 SHA-256 固定（协议 3 之前为必需，自协议 3 起为可选：参见[完整性](#integrity)）。`module.json` 无法固定其自身。

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## 完整性

清单所列的每个文件都必须存在且不得经由联接点（junction）或符号链接到达，作者固定的每个文件都必须具有其固定的 SHA-256；否则模块会被拒绝，错误码为 `OL_E_MODULE_INTEGRITY`。这可以发现已损坏或只更新了一半的模块。这不是信任决策：不会对照任何列表、签名或发布者来检查模块。

自协议 3 起，固定项为可选，因此作者无需手工维护哈希，并且计划始终会获取一个**快照**：对清单所列的每个文件及其内容计算的一个 SHA-256，记录在计划中（`PlannedModule.SnapshotSha256`），并通过启动交接（线路版本 8）传给插件。快照保证计划所评估的文件就是实际运行的文件：只有当模块的 `module.json` 具有计划所评估的 SHA-256 且其文件内容仍与快照一致时，插件才会加载该模块；并且在会话程序、原生组件或辅助程序启动之前，控制器和桥接层会再次比对快照。规划之后文件发生变化的模块不会被加载（`OL_E_MODULE_INTEGRITY`，“模块的文件在规划之后发生了变化”）。快照涵盖每一个模块，无论是否固定；固定项则额外让模块的发布者在发现已损坏或只更新了一半的副本时立即察觉。

在模块代码启动之处会重复该检查：插件只有在模块的 `module.json` 仍具有计划所评估的 SHA-256 且每个固定项和快照仍然成立时才会加载该模块，而控制器只有在验证了会话程序的固定项和快照之后才会启动该程序。

<a id="packing-a-module"></a>
### 打包模块

`OmsiLaunch.exe /pack-module:<module directory>` 为 `module.json` 所列的文件（其组件和资源）写入固定项，因此作者无需手工编辑 SHA-256。它只改写 `sha256` 的值（清单的其余部分予以保留；文件会重新缩进），在写入任何内容之前先用清单解析器重新读取结果，并报告每个文件（`module.pack` 信封：`module`、`directory`、`written`、`intact`，以及带有 `path`、`sha256`、`pinned`、`mismatch`、`written` 的 `files`）。它只处理所给定的目录，从不触及任何安装实例。`/verify-module:<module directory>` 报告相同内容但不写入（`module.verify`）；当某个已固定的文件与其固定项不匹配时，它以退出码 `7` 退出（没有固定项的文件不算不匹配）。对于无法解析的清单或缺失的文件，两者都以退出码 `2` 退出。

<a id="security-boundary"></a>
## 安全边界

OmsiLaunch 不控制模块在 OMSI 内部的行为：其托管组件和原生组件运行在 OMSI 进程中，权限与任何 OMSI 插件相同。OmsiLaunch 保证以下几点：

1. OmsiLaunch 进程绝不加载模块代码。模块代码运行在 OMSI 内部、从 OMSI 内部启动的辅助程序中，或模块的会话程序中。
2. OmsiLaunch 为模块启动的程序都是模块自身目录中的文件，经与其固定项核对后，以 OmsiLaunch 固定的命令行启动。会话程序运行在一个作业对象（job object）中，该作业对象会随会话一同终止它，不允许第二个进程（它不能启动其他程序），并拒绝更改系统参数、显示设置、桌面以及注销或关机。它只继承其两个管道句柄。
3. 模块声明的一切都保持在其目录之内，其可写数据位于 `.omsilaunch\module-data\<id>` 之内。
4. OmsiLaunch 的各通道（本地控制、启动交接、遥测、运行时命令）保持仅限当前用户并绑定到会话；其中没有任何一个允许调用方启动程序、读写安装之外的文件或更改系统设置。
5. OmsiLaunch 从不提升权限。模块以启动会话的用户的权限运行。
6. 来自模块的文本（通知、事件、值）被视为数据：有长度限制，不含控制字符，绝不被运行、打开或作为链接跟随。

<a id="capabilities"></a>
## 能力

`required_capabilities` 和 `optional_capabilities` 所指名称，要么是正在运行的构建的原生能力（[能力目录](capabilities.md#capability-catalog)中的原生能力列，例如 `time.read` 或 `d3d.texture`），要么是桥接功能：

| 桥接功能 | 模块获得的内容 |
| --- | --- |
| `bridge.events` | `Emit`：运行时事件 `module.<id>.<name>`。 |
| `bridge.background-threads` | `RunBackground`：受跟踪的后台线程。 |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`：绑定到会话的辅助进程。 |
| `bridge.hooks.iat` | 原生组件：对 `Omsi.exe` 导入的钩子（`iat:<dll>!<function>`）。 |
| `bridge.hooks.d3d9` | 原生组件：对当前 Direct3D 9 设备方法的钩子（`d3d9:device.<method>`）。需要原生能力 `d3d.texture`。 |
| `bridge.runtime` | 协议 3。`IRuntimeModuleContext.Runtime` 和原生的 `run_operation`：清单在 `runtime_operations` 中列出的目录操作。 |
| `bridge.tick` | 协议 3。周期性回调：`ITickingModule.Tick` 和原生导出 `omsilaunch_module_native_v1_tick`。 |
| `bridge.input` | 协议 3。`IRuntimeModuleContext.Input`：为模块自己的菜单捕获游戏的键盘和鼠标消息。需要原生能力 `ui.input.capture`。 |

两者都不是的名称在规划时即被拒绝（`OL_E_MODULE_CAPABILITY_UNRESOLVED`）。在没有已知布局的 OMSI 2 版本上，可执行文件无法解析的原生能力会在进程内判定；如果在进程内也无法解析，则不加载该模块。

<a id="choosing-modules-for-a-session"></a>
## 为会话选择模块

模块以四种方式之一加入会话；`PlannedModule.Activation` 指明是哪一种：

| 激活方式 | 时机 |
| --- | --- |
| `requested` | 会话请求了它：`LaunchSpec.Modules.Enable`、`/module:<id>`，或所选会话配置档预设中的 `enabled: true`。 |
| `settings-file` | [设置文件](#the-settings-file)中其节内的 `enabled=true`。 |
| `content` | 其激活方式为 `content`，且会话运行其所绑定的地图。 |
| `default` | 其激活方式为 `default`。 |

通过 `LaunchSpec.Modules.Disable`、`/no-module:<id>`、预设中的 `enabled: false` 或设置文件中的 `enabled=false`，可使模块不加入会话（计划诊断信息 `module.disabled`）。显式请求优先于设置文件。在同一请求中同时启用和禁用同一个模块会导致 `OL_E_MODULE_SETTING_INVALID`。

规划规则：被请求但无法加载的模块会使计划不可运行，并带有其 `OL_E_MODULE_*` 错误码。自行加入（设置文件、内容、默认）但无法加载的模块不会使会话停止：计划会报告 `OL_W_MODULE_NOT_LOADED`。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## 设置

模块在 `settings` 中声明其设置项；每个条目：

| 键 | 是否必需 | 含义 |
| --- | --- | --- |
| `key` | 是 | `[a-z][a-z0-9_]{0,31}`，不得为 `enabled`。 |
| `type` | 是 | `boolean`、`integer`、`choice` 或 `text`。 |
| `default` | 是 | 没有其他来源设置时的值：布尔值、整数、字符串，或在 `auto` 为 true 时为 `"auto"`。 |
| `auto` | 否 | `true`：接受值 `auto` 并传递给模块，由模块在运行时决定。 |
| `min`、`max` | 否 | 整数边界。 |
| `choices` | `choice` 类型必需 | 接受的值（`[a-z0-9][a-z0-9_.-]{0,31}`，不得为 `auto`）。 |
| `max_length` | 否 | 文本长度限制（1..4096，默认 256）。控制字符会被拒绝。 |
| `flag` | 否 | 此设置项的命令行参数，`[a-z][a-z0-9-]{2,31}`：`/<flag>:<value>`，对布尔类型单独使用 `/<flag>` 表示 `true`。 |
| `description` | 否 | 最多 256 个字符。 |

值会被规范化：布尔值接受 `true`/`false`、`yes`/`no`、`on`/`off`、`1`/`0`；选项的匹配不区分大小写；整数会按其边界检查。

每个设置项的有效值按以下顺序确定：

1. 请求：`LaunchSpec.Modules.Settings`（`"<module-id>.<key>": "<value>"`）、`/module-setting:<module-id>.<key>=<value>`、模块自己的参数，或所选会话配置档预设中的 `modules.<id>.settings`。与其他所有预设字段一样，命令行不能同时设置预设已设置的键（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；
2. [设置文件](#the-settings-file)；
3. 清单默认值。

无效的请求值，或请求了模块未声明的键，会使计划不可运行（`OL_E_MODULE_SETTING_INVALID`）。无法使用的设置文件条目会被忽略并给出 `OL_W_MODULE_SETTING_IGNORED`，针对会话不加载的模块所请求的设置也是如此。计划在 `PlannedModule.Settings` 中列出每个有效值及其来源（`request`、`settings-file` 或 `default`）。

模块会收到每个已声明键及其有效值：`IRuntimeModuleContext.Settings`、原生组件中的 `get_value("setting.<key>")`，以及会话程序上下文中的 `settings` 对象。

<a id="the-settings-file"></a>
### 设置文件

`<OMSI root>\.omsilaunch\modules.ini`，位于会话配置档旁边。OmsiLaunch 只读取它。

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

节名是模块 ID；`key=value` 行设置设置项，`enabled` 为每个会话开启或关闭该模块；以 `;` 或 `#` 开头的行是注释。该文件最大为 256 KiB。其中无法使用的内容（不是模块 ID 的节、位于任何节之外的行、未知的键、无效的值）会以 `OL_W_MODULE_SETTING_IGNORED` 报告并被忽略。

<a id="session-programs"></a>
## 会话程序

模块可以声明一个 `host` 组件：其会话程序，即来自其自身目录的可执行文件。当模块在计划中时，控制器会：

1. 验证该程序的固定项并创建模块的数据目录；
2. 在 OMSI 之前，从模块目录以命令行 `"<program>" --omsilaunch-session <session id>` 启动它，并保持挂起，直到它进入其作业（参见[安全边界](#security-boundary)）；
3. 将上下文作为第一行写入其标准输入；
4. 等待程序写出 `{"type":"ready"}`，最长 `prepare_ms`；只有此后才启动 OMSI；
5. OMSI 创建之后，写入 `{"type":"omsi-started","pid":<process id>}`；
6. 在会话结束时，OMSI 退出之后、任何文件被还原之前，写入 `{"type":"stop"}`，最多等待 `host_stop_ms`，然后结束该程序。

两个方向都使用 UTF-8 JSON，每行一个对象。上下文：

| 字段 | 含义 |
| --- | --- |
| `type` | `context`。 |
| `schema` | `omsilaunch.module-session/v1`。 |
| `session_id`、`module_id`、`module_version`、`product_version` | 标识信息。 |
| `installation_root`、`module_directory`、`data_directory` | 完整路径。 |
| `channel` | 程序可为模块在 OMSI 中的组件提供服务的命名管道：`\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`。程序创建该管道，将其限制为当前用户，并检查客户端是 OMSI 的进程。组件通过 `get_value("session_channel")` 或 `IRuntimeModuleContext.SessionChannelName` 读取它。 |
| `settings` | 模块的有效设置项。 |
| `world` | 会话的 `mode`、`map`、`situation`、`entrypoint_index`、`entrypoint`。 |
| `session_files` | 会话事务所拥有的、相对于安装根目录的文件。程序不得保持它们打开或映射：会话结束时它们会被还原。 |

除 `ready` 外，程序还可以写出的内容：

| 行 | 效果 |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | 运行时事件 `module.<id>.<name>`。 |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | 运行时事件 `module.<id>.notice`（参见[通知](#notices)）。 |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | 带有该已登记错误码的会话诊断信息（否则为 `OL_E_MODULE_OPERATION_FAILED`）。 |

其他行、长于 64 KiB 的行以及第 10,000 行之后的任何内容都会被忽略。控制器会记录 `module.<id>.host.started`、`host.ready`（带 `prepare_ms`）、`host.failed`、`host.exited` 和 `host.killed`。

在 `prepare_ms` 内未就绪、在就绪前退出，或其固定项不成立的程序，会使其模块的准备失败：在 `fail-session` 下，会话不会启动（`OL_E_MODULE_SESSION_FAILED`）；在 `disable-module` 下，该程序被结束，模块不进入 OMSI；在 `degrade` 下，模块保留（并且如果程序仍能运行，则继续运行）。会话的 `StartupTimeoutSeconds` 从 OMSI 启动时开始计时，因此准备阶段不会消耗它。

<a id="conflicts-with-omsi-plugins"></a>
## 与 OMSI 插件的冲突

`conflicts.plugins` 指明不得与该模块同时运行的 OMSI 插件 DLL，例如安装相同钩子的旧版插件。规划时，OmsiLaunch 会读取每个 `plugins\*.opl` 的 `[dll]` 节；如果其中某个加载了冲突的 DLL，则模块被拒绝，错误码为 `OL_E_MODULE_CONFLICT`，原因中会指明该 `.opl`。在 OMSI 内部，若冲突的 DLL 仍然被加载，则该模块不会被加载。OmsiLaunch 从不更改其他插件的文件；是否关闭它由用户决定。

<a id="notices"></a>
## 通知

名为 `notice` 的模块事件（来自 `Emit("notice", ...)`、原生组件的 `emit` 或会话程序的通知行）会由 OmsiLaunch 托盘以气泡提示的形式显示，标题中会写明模块名称。只保留 `title`（最多 63 个字符）和 `text`（最多 255 个字符），控制字符会被移除，其中的任何内容都不会被打开或运行。必须在 OMSI 启动之前（托盘尚未显示时）告知用户的会话程序，应使用其自己的窗口。

<a id="data-directory"></a>
## 数据目录

`<OMSI root>\.omsilaunch\module-data\<id>\` 是模块针对此安装的可写目录：即 `IRuntimeModuleContext.DataDirectory`、`get_value("data_directory")` 以及会话程序的 `data_directory`。它在首次使用时创建。它不属于会话事务：模块写入其中的内容在会话结束后仍会保留。`/clear-module-data:<id>` 会删除它（且仅删除它）；当会话拥有该安装时会被拒绝，并且经由链接到达的目录会按链接本身移除，绝不跟随。

<a id="lifecycle"></a>
## 生命周期

| 步骤 | 位置 | 发生的事情 |
| --- | --- | --- |
| 1. 发现 | 控制器 | 读取 `plugins\OmsiLaunch.Modules\*`：清单和完整性（`ListRuntimeModulesAsync`、`/list:RuntimeModules`）。 |
| 2. 资格判定 | 控制器，规划时 | 对[加入会话](#choosing-modules-for-a-session)的模块进行评估：可用性、桥接协议、能力、内容绑定、插件冲突、设置。结果为 `SessionPlan.PlannedModules`。 |
| 3. 准备 | 控制器，启动时 | 启动并等待模块的[会话程序](#session-programs)。然后符合资格的模块通过启动交接（版本 6）传递给插件：它们的目录、每个已评估 `module.json` 的 SHA-256，以及它们的有效设置。 |
| 4. 装配 | 插件，世界启动之前 | 每个模块被重新验证并加载（托管组件位于其自己的加载上下文中）。其原生组件（如果有）先被加载并启动，然后调用 `Arm(context)`。 |
| 5. 游戏就绪 | 插件 | 在 `gameplay.entered` 之后，如果某个已加载的模块声明了 `bridge.hooks.d3d9`，桥接层会开始观察 Direct3D 设备（`d3d.ready`）；然后调用原生组件的 `gameplay_ready` 和 `GameplayReady()`。对于 NEW_MAP 会话，会再次核对所绑定的地图与启动时加载的地图是否一致。 |
| 6. 运行时 | 插件 | 操作 `module.<id>.<operation>` 到达 `Execute`。带有 `tick_ms` 的模块会收到 tick（参见 [tick](#tick)）；带有 `runtime_operations` 的模块会在进程内运行这些操作（参见[进程内操作](#operations-in-process)）。 |
| 7. 停止 | 插件，然后控制器 | 会话结束（或模块失败）时调用 `Stop()`。OMSI 退出后，停止会话程序。 |
| 8. 清理 | 插件 | 模块跟踪的一切按相反顺序释放并验证已消失；原生组件最后停止，其钩子被移除；其 DLL 保持加载但处于惰性状态，直到 OMSI 退出（可能仍有线程正通过某个绕行函数返回）。 |

与产品自身的运行时操作一样，每个进程内回调都在 OMSI 的 UI 线程上运行。桥接层会对照预算度量每个回调：迟迟返回的回调会使其阶段失败（`OL_E_MODULE_PHASE_TIMEOUT`）。桥接层无法中断永不返回的回调；这样的模块会使 OMSI 停滞，会话将像其他任何会话一样在启动超时时结束，或通过 `session stop` 结束。

<a id="failures"></a>
## 失败

失败是指任何阶段或操作中的异常或超限、进程内未解析的能力、进程内不成立的内容绑定、在 OMSI 中被加载的冲突插件，或未能就绪的会话程序。随后发生什么由模块的 `failure_policy` 决定。无论哪种情况，模块的资源都会被释放。抛出异常、返回非零值或超出其间隔的 tick 与操作的失败一样，都属于失败。

| 策略 | 在准备、加载、装配或游戏就绪阶段失败 | 在操作中失败 | 在停止阶段失败 |
| --- | --- | --- | --- |
| `fail-session` | 模块被释放；`module.session-failure`；会话以 `OL_E_MODULE_SESSION_FAILED` 失败，OMSI 被停止，文件被还原。世界启动之前的失败会阻止启动。 | 相同。 | 已报告（`module.failed`）；会话继续直至结束。 |
| `disable-module` | 模块被释放并禁用；会话继续。 | 相同。 | 已报告；模块被释放。 |
| `degrade` | 与 `disable-module` 相同，只是会话程序的失败会让模块留在会话中。 | 失败的调用返回其错误码；模块保持活动；每个错误码只报告一次 `module.degraded`。 | 已报告；模块被释放。 |

被禁用或已失败的模块对其操作回复 `OL_E_MODULE_DISABLED`。模块失败会作为会话诊断信息（`SessionStatus.Diagnostics`）连同其 `OL_E_MODULE_*` 错误码出现，也会作为运行时事件出现。

<a id="cleanup"></a>
## 清理

模块通过其上下文获取的内容都会被跟踪：用 `Track` 注册的资源、用 `RunBackground` 启动的线程、用 `StartAuxiliaryProcess` 启动的进程。在停止或失败时，桥接层先取消 `Stopping`，然后按相反顺序释放它们。辅助进程还会被放入关闭即终止（kill-on-close）的作业对象，因此即使 OMSI 被终止，它们也会随 OMSI 一同结束；会话程序的作业会使其随 OmsiLaunch 会话一同结束，即使 OmsiLaunch 自身被终止也是如此。释放之后仍然存活的资源（未退出的进程、未结束的线程）会以 `OL_E_MODULE_CLEANUP_INCOMPLETE` 报告。模块未通过其上下文创建的线程或进程不会被跟踪。

崩溃之后，模块没有任何需要恢复的内容：它们不持有安装文件，其内存、线程和绑定到作业的进程会随 OMSI 进程或 OmsiLaunch 进程一同结束。

<a id="native-components-and-hooks"></a>
## 原生组件与钩子

原生组件是实现 `src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` 中 C 接口的 32 位 DLL：

| 导出 | 调用时机 |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | 装配时，在托管的 `Arm` 之前。返回非零会使该阶段失败。 |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | 可选；在进入游戏之后。 |
| `void omsilaunch_module_native_v1_stop(void)` | 停止或失败时，在其钩子被移除之前。此后桥接表拒绝每一次调用，DLL 保持加载但处于惰性状态，直到 OMSI 退出。 |

桥接表为组件提供 `request_hook`、`emit`（带有字符串 JSON 对象的运行时事件 `module.<id>.<name>`），以及自协议 2 起的 `get_value`。组件在使用 `get_value` 之前须读取该表的 `size`（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`）。`get_value(context, name, buffer, capacity, length)` 将值以带结尾零的 UTF-8 复制出来，并把 `*length` 设为不含结尾零所需的字节数；它返回 `0`、`1`（参数）、`2`（未知名称）、`11`（停止之后）或 `12`（缓冲区过小）。自协议 3 起，该表还包含 `run_operation`（参见[进程内操作](#operations-in-process)）和 `d3d_is_game_device(context, device)`：Direct3D 钩子收到的设备指针是否为游戏自己的设备（`1`），或是另一个设备（`0`；车辆选择窗体会创建第二个具有相同方法表的设备，因此 `Present` 的 detour 会为两者都被调用，应只为游戏自己的设备绘制），尚未观察到设备时为 `10`，未声明 `bridge.hooks.d3d9` 或停止之后为 `11`。该指针仅用于比较，从不被使用，桥接层也从不交出任何指针。在默认池中保留 Direct3D 资源的模块应自行挂钩 `d3d9:device.Reset`（调用原函数之前所做的是“设备丢失”，返回成功之后是“设备已恢复”）。组件在使用协议 3 的成员（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`、`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`）之前须检查该表的 `size`。两个可选导出补全组件：`int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)`（参见 [tick](#tick)）和 `int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)`，即托管组件通过 `IRuntimeModuleContext.NativeComponent.Execute` 调用原生组件的方式（输入 JSON，输出 JSON，缓冲区约定与 `run_operation` 相同）。`get_value` 回答的名称：

| 名称 | 值 |
| --- | --- |
| `setting.<key>` | 已声明设置项的有效值。 |
| `data_directory` | 模块的[数据目录](#data-directory)。 |
| `module_directory` | 模块自己的目录。 |
| `session_id` | 会话 ID。 |
| `module_version` | 清单版本。 |
| `product_version` | OmsiLaunch 产品版本。 |
| `session_channel` | [会话程序](#session-programs)的管道；对没有会话程序的模块为未知。 |
| `omsi_version` | 游戏所报告的自身 OMSI 产品版本（协议 3；参见 [OMSI 版本](#the-omsi-version)）。 |

钩子只能通过符号目标请求，在 `start` 或 `gameplay_ready` 期间，且目标所对应的能力须已在清单中声明：

| 目标 | 钩住的对象 | 能力 |
| --- | --- | --- |
| `iat:<dll>!<function>` | `Omsi.exe` 的一个导入，例如 `iat:kernel32.dll!ReadFile`。 | `bridge.hooks.iat` |
| `d3d9:device.<method>` | 当前 Direct3D 9 设备的 `Reset`、`Present`、`BeginScene` 或 `EndScene`（在设备存在时于 `gameplay_ready` 中请求）。 | `bridge.hooks.d3d9` |

`request_hook` 在 `*original` 中返回用于继续调用链的函数（绕行函数必须调用它）。其结果：`0` 成功，`1` 参数，`2` 未知目标，`3` 该位置的内容与应有内容不符，`4` 该位置被桥接层之外的其他方挂钩，`5` 内存保护，`6` 注册表已满，`7` 顺序，`8` 内存，`9` 有线程正在执行要修补的字节，`10` 尚无 Direct3D 设备，`11` 当前不允许或未声明能力。

桥接层为产品或模块安装的每个钩子，都与其所有者（模块为 `module:<id>`）一起记录在同一个注册表中：

- 钩子只会安装在该位置应有的值之上（被导入的函数；名为 `d3d9.dll` 的模块、系统运行时或包装器的函数），或叠加在注册表自己记录的该位置最上层钩子之上。已被他人更改的位置会被拒绝（`4`），绝不覆盖。
- 同一位置上的钩子按后进先出的顺序移除；上面还有其他钩子的钩子会保留，直到那个钩子被移除。
- 产品自己的 Direct3D `Reset` 钩子在注册表中，因此模块的设备钩子会叠加在它之上。
- 清理时，桥接层在 `stop` 返回之后移除该模块的每个钩子。如果某个无法移除（他人更改了该位置），清理会报告 `OL_E_MODULE_CLEANUP_INCOMPLETE`。OMSI 运行期间，原生组件的 DLL 绝不会被卸载，因此仍在返回途中的绕行函数保持有效。

引擎还支持带跳板（trampoline）的函数序言钩子（字节更改期间其他线程会被挂起，并且会先检查确切的预期字节），但目前尚未向模块提供任何序言目标：此类目标来自构建的目标目录，并带有经过验证的字节。

<a id="operations"></a>
## 操作

模块操作为 `module.<id>.<operation>`。它只对计划加载了该模块的会话存在（`PlannedModule.Operations`）；否则 `ExecuteRuntimeAsync` 回复 `OL_E_MODULE_NOT_LOADED` 或 `OL_E_MODULE_OPERATION_UNKNOWN`。必需参数会被检查，未声明的参数会被拒绝（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。模块的类型化失败在其为已登记的公共错误码时保留该错误码；任何其他错误码都会变为 `OL_E_MODULE_OPERATION_FAILED`，模块的错误码放在 `Values["module_code"]` 中。遵循内部命名约定的结果键会被移除，与每个运行时操作相同。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## 辅助程序

模块通过 `StartAuxiliaryProcess` 启动其某个 `auxiliary` 组件（其自身目录中已固定的 `.exe`）。自协议 3 起，桥接层以**挂起**状态创建该程序，将其放入会话的作业对象，然后才恢复运行，因此它立即启动的子进程不会在作业之外运行。当模块停止或 OMSI 退出时，作业会结束该程序及其启动的一切（关闭时终止），并从程序的第一条指令起就带有清单中的 `priority`（`below-normal`、`idle`）和 `memory_limit_mb`。与[会话程序](#session-programs)不同，辅助程序的作业允许子进程（内嵌 Web 视图的程序需要它们）；每一个子进程都在作业之中。启动之前会再次验证该程序：若有固定项则验证固定项，并始终验证计划的快照。

<a id="operations-in-process"></a>
## 进程内操作

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)`（托管）和 `run_operation`（原生）在进程内、在 OMSI 的 UI 线程上运行[公共能力目录](capabilities.md#public-runtime-operations)中的操作，使用与运行时控制通道相同的注册表、相同的带类型 `OL_E_*` 结果、相同的参数检查和相同的变更策略，但不经过该通道。只允许清单在 `runtime_operations` 中列出的操作：任何其他操作都会在到达 OMSI 之前被回复 `OL_E_MODULE_OPERATION_NOT_DECLARED`，并且 `internal.*` 和 `module.*` 操作永远不能被列出。若该构建未解析某个操作所需的原生能力，则像其他各处一样回复 `OL_E_CAPABILITY_UNRESOLVED`。

该列表是透明度与规划信息，而不是安全边界：模块本来就以用户的权限运行在 OMSI 内部。计划会显示它（`PlannedModule.RuntimeOperations`），改变 OMSI 状态的操作在其中带有其变更策略，例如 `camera.set (omsi-state)`。模块自 `Arm` 起即可运行操作（此时世界可能尚不存在；该操作随后会回复它自己的带类型错误）。结果有界（1 MiB）；更大的结果为 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`。模块停止之后，每次调用都回复 `OL_E_MODULE_DISABLED`。

原生的 `run_operation(context, operation, arguments_json, buffer, capacity, length)` 接收目录 ID 和由字符串组成的 JSON 对象（或 null），并写出 `{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}`。当它已作出回复时返回 `0`（操作自身的失败包含在 JSON 内），参数问题返回 `1`，停止之后返回 `11`，缓冲区过小时返回 `12`（`*length` 为所需大小）。

## tick

声明了 `tick_ms` 和 `bridge.tick` 能力的模块，大约每 `tick_ms` 毫秒会在 OMSI 的 UI 线程上由插件的 50 ms UI 定时器调用一次：托管组件上为 `ITickingModule.Tick(TimeSpan elapsed)`，以及可选的原生导出 `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)`。间隔是介于 50 和 1000 之间的 50 的倍数。tick 并非逐帧：需要绘制的模块应挂钩 Direct3D 的 `Present` 方法，并使用 tick 来读取数据。tick 在 `gameplay.entered` 之后开始，并随模块一同停止。抛出异常、返回非零值（原生）或耗时超过其间隔的 tick，属于该模块的失败：在 `degrade` 下，每种代码只报告一次（`module.degraded`），模块继续运行（错过的 tick 会被跳过，绝不排队）；在 `disable-module` 和 `fail-session` 下则应用相应的策略。OMSI 的嵌套模态循环会使 UI 定时器持续触发，因此 tick 绝不会重入自身。

<a id="community-content"></a>
## 社区内容

`content_inputs` 声明模块所读取的内容，其作者在构建模块时无法得知这些内容，因为它们是社区在模块之后为某张地图制作的。没有可固定的 SHA-256；桥接层只负责定位它，并检查读取它是否安全。目前唯一的种类是 `realmap-package`：地图的 `openomsi-realmap` 文件夹，即 `<OMSI root>\maps\<folder>\openomsi-realmap`，由 openOMSI 地图包工具写出。

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` 会回答该地图是否有包（没有包不是错误），并在有包时给出其目录、文件数、总大小和一个 `Fingerprint`。指纹是针对每个文件的相对路径、大小和写入时间的变更检测器：请把它用作缓存键；它绝不是完整性检查或信任决策。该包必须位于安装目录之内，其任何部分都不得是重解析点，并且有界（50,000 个文件，4 GiB）；否则回答为 `OL_E_MODULE_CONTENT_INPUT_INVALID`。未声明该种类的模块会得到 `OL_E_MODULE_CONTENT_INPUT_UNDECLARED`。该包是第三方制作的数据：请在托管代码中读取，将每个值都视为不受信任，并按资源降级。当会话的地图没有包时，`required: true` 的输入会使计划拒绝该模块（`OL_E_MODULE_CONTENT_BINDING`）。

<a id="input-capture"></a>
## 输入捕获

用自己的菜单替换 OMSI 暂停菜单的模块需要游戏的键盘和鼠标。OMSI 通过 DirectInput 读取其模拟按键，并通过发往主窗体视图窗口的窗口消息读取鼠标；当其自身的暂停开启时（原生暂停字节），模拟、渲染、3D 点击和 DirectInput 命令都由 OMSI 自己保持暂停。`IRuntimeModuleContext.Input`（能力 `bridge.input`，原生能力 `ui.input.capture`）补上其余部分：`Capture(true)` 在 UI 线程上安装一个线程本地的 `WH_GETMESSAGE` 钩子，属于游戏窗口树（主窗体、其子窗口和隐藏的原生暂停窗体）的键盘和鼠标消息会被记录为普通的 `ModuleInputEvent` 值（种类、虚拟键、视图窗口的客户区坐标、按钮、滚轮增量、修饰键）并从队列中移除，因此 OMSI 和 VCL 都不会对其作出响应。`Drain()` 返回这些事件（最多保留 256 个）。不会进行任何子类化，不挂钩任何 user32 按键状态函数，也不发送任何内容。

同一时间只有一个模块可以捕获。无论 `Stop` 做了什么，捕获都会在模块停止或失败时被释放。原生模态对话框（保存、选项、新游戏）需要真实输入：请求其中任何一个之前，先释放捕获。钩子机制已通过一个替身窗口在离线状态下验证（`ui-input.hook-self-check`）；其在真实游戏窗口上的行为尚未被观察到。

<a id="the-omsi-version"></a>
## OMSI 版本

`IRuntimeModuleContext.OmsiVersion` 和 `get_value("omsi_version")` 给出游戏所报告的自身 OMSI 产品版本，例如 `2.3.004`：即 OMSI 在其 `logfile.txt` 第 7 行的 `Version:` 之后写入的文本，从运行中的映像读取。它**绝不是**可执行文件的 PE 文件版本（参考版本的 `FileVersion` 为 `2.2.032`）。当映像中没有唯一的此类文本时，如果代码标识已知则使用构建配置的版本，否则为 `unknown`。可执行文件的哈希、其代码大小、地址和配置名称不会被暴露。请将版本用于文本以及选择行为；对于哪些功能可用，能力（`IsCapabilityAvailable`）仍然是唯一权威来源。

<a id="writing-a-module-contracts"></a>
## 编写模块（契约）

托管组件引用 `OmsiLaunch.Runtime.Modules` 和 `OmsiLaunch.Api`，并实现 `IRuntimeModule`。桥接层将这两个引用绑定到它自己已加载的程序集（无论插件运行在哪个加载上下文中），因此随模块一起分发的副本会被忽略：

| 成员 | 调用时机 |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | 步骤 4。通过上下文获取资源。 |
| `void GameplayReady()` | 步骤 5。 |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | 步骤 6，针对每个已声明的操作（不带 `module.<id>.` 前缀）。类型化失败时抛出 `ModuleOperationException(code, message)`。 |
| `void Stop()` | 步骤 7。 |

`IRuntimeModuleContext` 提供 `SessionId`、`ModuleId`、`ModuleDirectory`、`Settings`、`DataDirectory`、`ProductVersion`、`OmsiVersion`、`SessionChannelName`、`Stopping`（取消令牌）、`IsCapabilityAvailable`、`Emit`、`Track`、`RunBackground`、`StartAuxiliaryProcess`，以及自协议 3 起的 `Runtime`（参见[进程内操作](#operations-in-process)）、`Content`（参见[社区内容](#community-content)）、`Input`（参见[输入捕获](#input-capture)）和 `NativeComponent`（已加载的原生组件，或 null）。需要 tick 的托管组件还要实现 `ITickingModule`。上下文中没有任何内容会暴露地址、可执行文件的哈希或其代码布局。组件必须能被 32 位插件加载（AnyCPU 或 x86，.NET 6）；其旁边的 `OmsiLaunch.Runtime.Modules` 或 `OmsiLaunch.Api` 副本会被忽略。请固定模块的程序所加载的每个文件（对于 .NET 程序：与 `.exe` 相邻的 `.dll`），使完整性检查覆盖它们。

<a id="known-limitations"></a>
## 已知限制

- 原生组件可以挂钩 `Omsi.exe` 的导入和 Direct3D 设备方法；对 OMSI 函数序言和调用点的钩子目前尚未向模块提供。
- 暂停菜单接管（`ui.pause.*`）和输入捕获经过静态设计并在离线状态下验证；它们尚未在真实的 OMSI 会话中观察到，并且 `ui.pause-menu.replace` 仅在已知代码标识上解析（其某些字段没有与构建无关的推导方式）。
- 进程内的内容绑定检查使用启动所请求的地图（NEW_MAP）；对于已保存情景，绑定在规划时对照该情景的地图进行检查。
- 设备钩子是虚表（vtable）钩子。当另一个组件把设备的原始方法写回时（在回归安装实例上于游戏开始后不久观察到），设备钩子会停止被调用且无法移除；此时清理会报告 `OL_E_MODULE_CLEANUP_INCOMPLETE`，且组件保持加载。已被注册表之外的其他方挂钩的设备方法会被拒绝（4）；拒绝事件会指明拥有该方法的模块。
- 模块在 OMSI 正常退出时停止；会话停止会终止 OMSI，因此会话内的模块清理只会通过失败策略发生。
- 通知在托盘存在之后、OMSI 启动之后才显示；会话程序对在此之前必须说明的任何内容，使用其自己的窗口。
