# 会话配置档

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/session-profiles.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

会话配置档是一个声明式的 YAML 包，由内容作者随地图或附加内容一起发布，使最终用户能够用一条命令启动可复现的 OmsiLaunch 会话（`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`）。本页是以下内容的规范性参考：由 `src/OmsiLaunch.Core/SessionProfiles.cs` 中的 `SessionProfileCompiler` 实现的 `omsilaunch.session-profile/v1` 格式；CLI 所应用的优先级规则（`tools/OmsiLaunch.Cli/Program.cs` 中的 `CliInput.BuildSpecAsync` 和 `RejectProfileConflicts`）；以及配置档可写入的设置项目录（`ConfigurationCatalog`）。配置档能做的一切，CLI 参数和 [LaunchSpec](launchspec.md) 同样能做；配置档只是把这些选择打包在一起。

稳定性：解析、校验、冲突检测以及 `settings` / `presentation` / `internet-textures` / `behavior` 块为 `STABLE_BETA`（离线测试 `session-profiles.strict-compiler`；覆盖层和还原路径已由 RV-005 和 RV-006 经运行时验证，参见[运行时验证状态](../status/runtime-validation-status.md)）。`new.date`、`new.time`、`new.year` 和 `new.weather` 键在当前版本（build）中为 `UNAVAILABLE`（参见[`new` 块](#new)）。

<a id="package-location-and-naming"></a>
## 包的位置与命名

| 项目 | 规则 |
| --- | --- |
| 包目录 | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| 配置档文件 | `<package>\profile.yaml`（名称必须完全一致，只有一个文件） |
| 资源 | 包目录内的任意文件或目录，由 `presentation.splash.assets` 和 `internet-textures.profile` 通过相对路径引用 |
| `id` | 必须是普通的目录名：不能为空或仅含空白，不能包含 `\`、`/` 或 `:`，也不能包含序列 `..`。违反时为 `OL_E_SESSION_PROFILE_PATH_ESCAPE`。`profile.yaml` 中声明的 `id` 值必须与目录名逐字节相同（区分大小写）；否则为 `OL_E_SESSION_PROFILE_INVALID`。 |
| 选择 | `/predefined-profile:<id>` 与 `/predefined-profile-index:<n>` 一起使用。索引是必需的：只有 `/predefined-profile` 而没有 `/predefined-profile-index` 会以 `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` 失败。 |
| 包不存在 | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| 发行布局 | 发行包在 `.omsilaunch\examples\session-profiles\rmg-leste\` 下附带一个示例（参见[打包](packaging.md)）。示例不是配置档：需要将包复制到 `.omsilaunch\session-profiles\<id>\` 才能被选择。 |

配置档由用户或内容作者安装和删除。OmsiLaunch 从不向包中写入、从不复制包，也从不删除包。包目录不属于任何事务。

<a id="parsing-rules"></a>
## 解析规则

| 规则 | 行为 | 错误 |
| --- | --- | --- |
| 大小限制 | `profile.yaml` 不得超过 256 KiB（262,144 字节） | `OL_E_SESSION_PROFILE_INVALID` |
| 文档结构 | 恰好一个 YAML 文档，其根节点为映射 | `OL_E_SESSION_PROFILE_INVALID` |
| 模式（schema） | `schema` 必须严格等于 `omsilaunch.session-profile/v1`（区分大小写） | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| 锚点和别名 | 文档中任何位置带有 YAML 锚点（`&name`）的节点都会在校验前被拒绝；因此不可能出现别名（`*name`） | `OL_E_SESSION_PROFILE_INVALID`（“YAML anchors are not supported.”） |
| 未知键 | 每个映射都是封闭的：未在下表中为其上下文列出的键会被拒绝（“Unknown property in `<context>`: `<key>`”）。键按区分大小写的方式匹配（`Schema:` 是未知键）。唯一开放的映射是 `settings`，其键改为依据设置项目录进行校验。 | `OL_E_SESSION_PROFILE_INVALID` |
| 标量 | 每个叶子值都必须是标量；在需要标量的位置出现序列或映射会被拒绝（“`<field>` must be a scalar.”） | `OL_E_SESSION_PROFILE_INVALID` |
| 数字 | 整数以固定区域性（invariant culture）解析（`1`、`30`）；`settings` 中的小数以 `.` 作为分隔符 | `OL_E_SESSION_PROFILE_INVALID` |
| 日期和时间 | `new.date.value` 由 `DateOnly.Parse` 解析，`new.time.value` 由 `TimeOnly.Parse` 解析，均使用固定区域性；请使用 ISO 形式 `yyyy-MM-dd` 和 `HH:mm[:ss]` | `OL_E_SESSION_PROFILE_INVALID` |
| YAML 语法错误 | 连同解析器消息一起报告 | `OL_E_SESSION_PROFILE_INVALID`（“Invalid YAML: ...”） |
| 可执行内容 | YAML 由 `YamlDotNet` 仅解析为表示树；不支持标签、自定义类型或代码执行 |

在普通（不带引号的）标量中，反斜杠是字面字符。Windows 路径请使用单个反斜杠书写（`maps\Grundorf\global.cfg`）。普通标量中的双反斜杠在值中仍保持为双反斜杠；参见[随包示例](#the-packaged-example)。

<a id="key-reference"></a>
## 键参考

上下文的名称与编译器中的命名完全一致。此处列出的每个键都被接受；其他键一概不接受。

<a id="profile-root-mapping"></a>
### `profile`（根映射）

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `schema` | string | 是 | 字面值 `omsilaunch.session-profile/v1`。 |
| `id` | string | 是 | 包标识符；必须与目录名相同。 |
| `name` | string | 是 | 显示名称；在 `SessionProfileMetadata.Name` 中报告。 |
| `author` | string | 是 | 作者；在 `SessionProfileMetadata.Author` 中报告。 |
| `version` | string | 是 | 包版本字符串（自由格式，请加引号：`"1.0"`）；在 `SessionProfileMetadata.Version` 中报告。 |
| `compatibility` | mapping | 否 | 参见 `compatibility`。 |
| `new` | mapping | 否 | NEW_MAP 默认值。参见 `new`。 |
| `presets` | 映射序列 | 是 | 1 到 99 个预设条目。零个、超过 99 个或值不是序列时为 `OL_E_SESSION_PROFILE_INVALID`。 |

### `compatibility`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `maps` | 字符串序列 | 否 | 此配置档适用的地图标识（`maps\<Map>\global.cfg`）。`/` 被规范化为 `\`；比较不区分大小写。列表缺失或为空表示“任何地图”。非空时，对 `WorldMode.NewMap` 强制执行（与生效的 `new.map` 或 `/map` 比较），也对 `WorldMode.SavedSituation` 强制执行（与所选 `.osn` 引用的地图比较，通过内容目录解析）。对于 `WorldMode.LastMapState` 无法推导出地图，因此非空列表总是失败。失败时：`OL_E_SESSION_PROFILE_MAP_MISMATCH`。 |

### `new`

只要该块存在，就会被读取和校验，但仅当所选世界模式为 NEW_MAP（`/new`，CLI 默认值）时才会应用到 spec。在 `/saved:<file.osn>` 下该块被忽略。

| 键 | 类型 | 必需 | 是否应用 | 说明 |
| --- | --- | --- | --- | --- |
| `map` | string | 否 | 是 | 规范化形式 `maps\<Map>\global.cfg` 的地图标识（规划要求严格符合此形式：以 `maps\` 开头，以 `\global.cfg` 结尾，不含 `..`）。设置 `WorldSpec.MapIdentity`。 |
| `entrypoint-index` | integer | 否 | 是 | 所呈现的入口点索引（在 OMSI 入口点列表中从 0 开始的位置）。设置 `PresentedEntrypointIndex` 并清除任何入口点标识。 |
| `entrypoint` | string | 否 | 是 | 原始入口点标识。设置 `EntrypointIdentity` 并清除所呈现的索引。如果 `entrypoint-index` 和 `entrypoint` 同时存在，`entrypoint` 生效，因为它最后应用。该值是精确（区分大小写）的标签或发现标识；OMSI 只呈现每个标签的第一条记录（`world.entrypoint-identity`，`EXPERIMENTAL`）。 |
| `date` | mapping | 否 | 否（`UNAVAILABLE`） | 参见 `new.date`。 |
| `time` | mapping | 否 | 否（`UNAVAILABLE`） | 参见 `new.time`。 |
| `year` | integer | 否 | 否（`UNAVAILABLE`） | 显式年份。 |
| `weather` | mapping | 否 | 否（`UNAVAILABLE`） | 参见 `new.weather`。 |

`date`、`time`、`year` 和 `weather` 会被编译为 `DateSpec`、`TimeSpec`、`YearSpec` 和 `WeatherSpec`，使用 `DateTimeMode.Explicit` / 所选的 `WeatherMode`。随后会话规划器（`src/OmsiLaunch.Core/SessionPlanner.cs`）要求 `world.explicit-date`、`world.explicit-time`、`world.explicit-year` 和 `world.weather`；这四项均为 `EXPERIMENTAL`（已通过静态分析确定，运行时验证待完成）。启动日期/时间仅适用于 NEW_MAP；天气适用于每种世界模式。

#### `new.date`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `mode` | string | 是 | 必须为 `explicit`（不区分大小写）。其他任何值均为 `OL_E_SESSION_PROFILE_INVALID`（“date must use explicit mode.”）。 |
| `value` | string | 是 | `yyyy-MM-dd`。 |

#### `new.time`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `mode` | string | 是 | 必须为 `explicit`。 |
| `value` | string | 是 | `HH:mm` 或 `HH:mm:ss`。 |

#### `new.weather`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `mode` | string | 是 | `preset`、`icao` 或 `real`（不区分大小写）。其他任何值：`OL_E_SESSION_PROFILE_INVALID`（“Unsupported weather mode”）。 |
| `preset` | string | `mode: preset` 时必需 | 天气预设名称。 |
| `icao` | string | `mode: icao` 时必需 | ICAO 气象站代码。 |

<a id="preset-each-entry-of-presets"></a>
### `preset`（`presets` 中的每个条目）

| 键 | 类型 | 必需 | 默认值 | 说明 |
| --- | --- | --- | --- | --- |
| `index` | integer | 是 | | 1 到 99，在配置档内唯一；索引不必连续。通过 `/predefined-profile-index` 选择。重复或超出范围：`OL_E_SESSION_PROFILE_INVALID`；配置档中不存在的索引：`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`。 |
| `id` | string | 是 | | 预设标识符；报告为 `SessionProfileMetadata.PresetId`。 |
| `name` | string | 是 | | 预设显示名称；报告为 `SessionProfileMetadata.PresetName`。 |
| `settings` | mapping | 否 | 无 | 语义化的 `options.cfg` 设置项，参见[设置项](#settings)。键依据目录以不区分大小写的方式匹配。 |
| `presentation` | mapping | 否 | 继承 | 启动画面呈现，参见 `presentation`。缺失时预设继承基线（`/spec` 值或 CLI 默认值 `Managed`）。 |
| `internet-textures` | mapping | 否 | 继承 | 参见 `internet-textures`。 |
| `behavior` | mapping | 否 | 继承 | 超时设置，参见 `behavior`。 |
| `hofs` | sequence of mappings | 否 | 继承 | 会话 HOF 操作，参见 `hofs`。存在时会替换基线 `HofManagement`。 |
| `modules` | mapping | 否 | 继承 | 运行时模块，参见 `modules`。与 `/spec` 文件所请求的内容合并。 |

只有所选的预设会被应用。但每个预设仍会被解析和校验，因此预设 3 中的错误也会导致请求预设 1 失败。

### `presentation`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `splash` | mapping | 是 | 存在 `presentation` 时必需（“Presentation requires splash.”）。参见 `presentation.splash`。 |

#### `presentation.splash`

| 键 | 类型 | 必需 | 默认值 | 说明 |
| --- | --- | --- | --- | --- |
| `mode` | string | 是 | | `managed` 在会话期间安装 OmsiLaunch 启动画面位图（`SplashMode.Managed`）。`unset` 或 `native` 保留 OMSI 自己的启动画面文件（`SplashMode.Unset`；`Native` 是别名）。不区分大小写。其他任何值：`OL_E_SESSION_PROFILE_INVALID`。 |
| `language` | string | 否 | `ENG` | 第二个启动画面目标的语言区域：`PTB`、`ENG`、`DEU`、`FRA`（别名 `PT-BR`、`EN`、`DE`、`FR`；任何未知值在会话构建时解析为 `ENG`）。使用 `mode: managed` 时，会话以覆盖层替换 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_<language>.bmp`。 |
| `assets` | string | 否 | 随包资源 | **相对于包**的目录，包含 `ENG.bmp`，对于非英语的 `language` 还需包含 `<language>.bmp`；每个文件都必须是 640x480、24 位 BMP。该目录在加载配置档时必须存在（`OL_E_SESSION_PROFILE_ASSET_MISSING`）；文件在会话启动时校验（`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`）。适用路径限制规则。省略时使用安装实例的 `.omsilaunch\assets\splash`（或随包默认资源）。 |

配置档无法设置 `SessionPresentationSpec.SuppressTrayIcon`；除非 `/spec` 设置了它，否则保持为 `false`。

### `internet-textures`

| 键 | 类型 | 必需 | 说明 |
| --- | --- | --- | --- |
| `mode` | string | 是 | `native`（`InternetTexturesMode.Native`，OMSI 正常运行）、`disabled`（`Disabled`，在会话期间抑制构建配置中已建档的进程内下载器）、`override`（`Override`，以 `Texture\standard.itx` 的形式安装会话范围的 `.itx` 配置文件）。不区分大小写；其他任何值：`OL_E_SESSION_PROFILE_INVALID`。 |
| `profile` | string | `override` 时必需 | `.itx` 文件**相对于包**的路径。使用 `override` 但缺少该键：`OL_E_SESSION_PROFILE_INVALID`；文件不存在：`OL_E_SESSION_PROFILE_ASSET_MISSING`。适用路径限制规则。该文件必须由 `URL` / `target` 行对组成，URL 为 `http://` 或 `https://`（否则为 `OL_E_ITX_PROFILE_INVALID`），且每个目标都必须解析到安装实例的 `Texture\` 目录之下，且不经过重解析点（`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。列出的目标和 `Texture\standard.ipr` 成为会话删除项（参见[事务与恢复](../concepts/transactions-and-recovery.md)）。 |

### `behavior`

| 键 | 类型 | 必需 | 默认值 | 说明 |
| --- | --- | --- | --- | --- |
| `startup-timeout` | integer（秒） | 否 | 180 | 从进程启动到 `Running` 允许的时间。加载配置档时必须为正数；会话在启动时还要求其在 1 到 600 之间（否则为 `OL_E_START_SESSION`）。映射到 `LaunchBehaviorSpec.StartupTimeoutSeconds`。 |
| `shutdown-timeout` | integer（秒） | 否 | 30 | 映射到 `LaunchBehaviorSpec.ShutdownTimeoutSeconds`。ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT：监管器直接终止 OMSI，从不读取此值。 |

存在 `behavior` 块时，两个超时都会被设置（使用给定值或默认值），并完全替换基线 `LaunchBehaviorSpec`，包括 `RestoreConfiguration` 和 `SuppressStaleClosecheckWarning`，它们会恢复为默认值（`true`、`true`）。

### `hofs`

每个条目是一个 [HOF 管理](hof-management.md)操作，且恰好指定一个来源键（`package`、`collection`、`directory`、`vehicle` 或 `file`）；零个或多个：`OL_E_SESSION_PROFILE_INVALID`。来源和目标是否存在会在规划会话时检查（`OL_E_HOF_*`），`package` 除外。

| 键 | 类型 | 必需 | 默认值 | 说明 |
| --- | --- | --- | --- | --- |
| `package` | string | 其中一个来源键 | | **相对于包**的目录，其中存放配置档随包提供的 `.hof` 文件。必须在加载配置档时存在（`OL_E_SESSION_PROFILE_ASSET_MISSING`）；适用路径限制规则。成为 `Directory` 来源。 |
| `collection` | string | 其中一个来源键 | | `.omsilaunch\hof\` 下某个集合的名称。 |
| `directory` | string | 其中一个来源键 | | 相对于安装目录的目录，例如位于地图内容旁边。 |
| `vehicle` | string | 其中一个来源键 | | 其 HOF 将被复制的车辆标识或文件夹。 |
| `file` | string | 其中一个来源键 | | 相对于安装目录的 `.hof` 文件。 |
| `targets` | sequence of strings | 是 | | 目标车辆（标识或 `Vehicles\` 之下的文件夹）。缺失或为空：`OL_E_SESSION_PROFILE_INVALID`。 |
| `files` | sequence of strings | 否 | 全部 | 要从来源中取用的文件名。 |
| `mode` | string | 否 | `add` | `add` 或 `mirror`（不区分大小写）；其他任何值：`OL_E_SESSION_PROFILE_INVALID`。 |

含有 `hofs` 的预设与 `/hof-source` 参数组合：`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。

### `modules`

每个[运行时模块](../../../reference/runtime-modules.md) ID 一个条目（`EXPERIMENTAL`）。每个条目是带有下列键的映射（上下文 `module`）：

| 键 | 类型 | 必需 | 默认值 | 说明 |
| --- | --- | --- | --- | --- |
| `enabled` | `true` 或 `false` | 否 | 未设置 | `true` 请求该模块（`LaunchSpec.Modules.Enable`）；`false` 使其不进入会话，包括默认加载、由其地图加载或由设置文件加载的模块（`LaunchSpec.Modules.Disable`）。 |
| `settings` | mapping | 否 | 无 | 按键给出的模块设置（`LaunchSpec.Modules.Settings`，形式为 `<id>.<key>`）。这些值会在规划会话时对照模块声明的设置进行检查（`OL_E_MODULE_SETTING_INVALID`）。 |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

模块 ID 不符合 `[a-z0-9][a-z0-9-]{1,47}`、`enabled` 不是 `true` 或 `false`，或设置键不符合 `[a-z][a-z0-9_]{0,31}`：`OL_E_SESSION_PROFILE_INVALID`。未安装的模块会在规划会话时报告（`enabled: true` 为 `OL_E_MODULE_NOT_FOUND`，仅有设置时为 `OL_W_MODULE_SETTING_IGNORED`）。

<a id="settings"></a>
## 设置项

`settings` 的键是 `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）中的语义名称。只有当键存在（`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`）且可写（`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`）时，编译器才会接受。值以字符串形式存储，并在会话构建覆盖层时转换为 `options.cfg` 补丁；因此无效值会在 `StartSessionAsync` 时而不是加载配置档时被检测到，并使会话以 `OL_E_START_SESSION` 失败，其消息中携带 `OL_E_INVALID_SETTING_VALUE: <key>`。下列每个设置项都写入 `options.cfg`；它们全部是会话范围的，并在会话结束后被精确还原。

值的形式：

- **bool** 为 `true` 或 `false`（不区分大小写）。对于存在性令牌，令牌会被添加或删除；对于反向令牌（`no_*`），`true` 会删除该否定令牌。
- **int** / **decimal** 会依据范围校验；带除数的值以除后结果存储（例如 `graphics.minObjectScreenPercent: 5` 写入 `0.05`）。
- **string** 按原文写入。

| 设置项键 | `options.cfg` 令牌 | 类型 | 范围 / 取值 | 证据 |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | 任意 | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | 任意 | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool（存在性） | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool（存在性） | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool（存在性） | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool（存在性） | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool（存在性） | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool（存在性） | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | 任意 | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10，存储为 /100 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50，存储为 /100 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool（存在性） | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool（存在性） | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool，写为 `on` / `off` | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` 或 `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems`（4 行块） | `enabled,maxPerEmitter,playerVehicleOnly,inReflections`（bool,int>=0,bool,bool） | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool（存在性） | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool（反向存在性） | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool（两个存在性令牌） | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool（存在性） | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool（存在性） | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool（存在性） | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool（存在性） | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` 第 0 个分量 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 运行时） |
| `traffic.humans` | `AIMaxCountRandom` 第 1 个分量 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 运行时） |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

存在于目录中但**不可写**的条目（以 `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` 拒绝）：`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`（已被 `advanced.reducedMultithreading` 取代）、`graphics.texture`、`graphics.textureFilter`。

<a id="path-confinement"></a>
## 路径限制

`presentation.splash.assets` 和 `internet-textures.profile` 通过 `Confined(package root, value)` 解析：

1. 拒绝有根路径（`C:\...`）、以 `\` 开头的路径，以及任何等于 `..` 的路径组成部分。
2. 计算完整路径，该路径必须以包目录开头。
3. 检查包根目录之下直至（并包括）最终路径的每个已存在组成部分是否带有 `ReparsePoint` 属性。该路径上任何位置出现 junction、目录符号链接或文件符号链接都会被拒绝；无法检查的组成部分（`IOException` / `UnauthorizedAccessException`）同样会被拒绝。

以上三种失败均为 `OL_E_SESSION_PROFILE_PATH_ESCAPE`。在会话构建时，相同的重解析点规则也适用于 `Texture\` 下的 `.itx` 目标。

<a id="precedence-and-override-conflicts"></a>
## 优先级与覆盖冲突

`CliInput.BuildSpecAsync` 按以下顺序组合 spec：

1. **默认值**（NEW_MAP、所有项均未设置、超时 180 s / 30 s）。
2. **`/spec:<file.json>`**：如果提供，则完全替换默认值。
3. **安装根目录**：显式安装参数优先于 spec 的 `RootPath`；`.` 表示包含可执行文件的目录。
4. **配置档**（`/predefined-profile` + `/predefined-profile-index`）：加载包，并在合并任何内容**之前**针对原始 CLI 参数运行 `RejectProfileConflicts`。随后种子的世界块被重置为所选模式的空 `WorldSpec`（使用配置档时，`/spec` 中的世界会被丢弃），再由 `SessionProfileCompiler.Apply` 将配置档叠加到种子上：`new`（仅 NEW_MAP）、`settings`（合并到种子的 `Environment.General` 之上，按键以配置档为准），以及 `presentation`、`internet-textures`、`behavior`（仅当预设定义了它们时，才各自替换种子中的对应块）。
5. **其余 CLI 参数**叠加在最上层：`/map`、`/entrypoint`、`/entrypoint-index`、`/date`、`/time`、`/year`、天气参数、车辆参数、`/set`、启动画面参数、网络纹理参数、`/startup-timeout`、`/shutdown-timeout`。CLI 中的超时仅在提供时生效；否则保留 spec/配置档/默认值。
6. 对非 NEW_MAP 模式执行**兼容性检查**（`ValidateCompatibility`）。

针对所选配置档所拥有字段的 CLI 参数属于冲突，会以 `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` 被拒绝（退出码 2，类别 `invalid_argument`）。检查按字段而非按值进行：即使重复配置档自身的值也仍属冲突。

| CLI 参数 | 配置档定义了下列内容时冲突 | 仅限模式 |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` 或 `/entrypoint-index` | `new.entrypoint` 或 `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`、`/weather-icao`、`/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | 预设 `settings` 中的相同 `<key>`（不区分大小写） | 任意 |
| `/splash`、`/splash-language`、`/splash-assets` | `presentation`（任意） | 任意 |
| `/internet-textures`、`/internet-textures-profile` | `internet-textures`（任意） | 任意 |
| `/startup-timeout`、`/shutdown-timeout` | `behavior`（任意） | 任意 |
| `/module:<id>` | `modules.<id>.enabled: false` | 任意 |
| `/no-module:<id>` | `modules.<id>.enabled: true` | 任意 |
| `/module-setting:<id>.<key>=...` 或模块参数 | `modules.<id>.settings.<key>` | 任意 |

不属于冲突的情况：预设未定义的 `/set` 键（它们会被添加）、车辆参数（`/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration`、`/no-vehicle`；配置档无法定义玩家车辆），以及 `/saved` 下的任何世界参数（此时不应用 `new` 块）。无论是否使用配置档，`/map`、`/entrypoint` 和 `/entrypoint-index` 与 `/saved` 一起使用时都无效（`OL_E_INVALID_ARGUMENT`）。

<a id="error-codes"></a>
## 错误码

| 代码 | 触发条件 | CLI 退出码 |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` 不存在 | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` 不是普通目录名；`assets` / `profile` 离开了包或经过了重解析点 | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` 不是 `omsilaunch.session-profile/v1` | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | 大小限制、文档结构、锚点、未知键、缺少必需键、非标量值、错误的数字/日期/时间、`id` 不匹配、预设数量/索引规则、不支持的模式词、非正数超时、`presentation` 缺少 `splash`、`override` 缺少 `profile` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | 缺少 `/predefined-profile-index`、超出 1..99，或不存在具有该 `index` 的预设 | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | 某个 `settings` 键不在目录中 | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | 某个 `settings` 键在目录中但为只读 | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | 包内不存在 `assets` 目录或 `profile` 文件 | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` 非空，且生效的地图不在列表中（或无法推导） | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 显式 CLI 参数针对配置档所拥有的字段 | 2 |

以上错误都在编译命令行期间、规划之前触发。它们是 `SessionProfileException`（冲突则为 `ArgumentException`），绝不会启动会话。完整目录见[错误](errors.md)；退出码见[退出码](exit-codes.md)。

<a id="discovering-installed-profiles"></a>
## 发现已安装的配置档

`OmsiLaunch.exe /list:SessionProfiles`（API 为 `ListSessionProfilesAsync`）会编译 `.omsilaunch\session-profiles\` 下的每个包，并为每个包输出一个条目：`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity` 以及按索引顺序排列的 `Presets`（`Index`、`Id`、`Name`）。无法编译的包仍会被列出，带有 `IsValid: false` 以及启动时会报告的同一个 `OL_E_SESSION_PROFILE_*` 代码。不会写入任何内容。

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## 配置档在 API 中的体现

加载成功后，spec 会在 `LaunchSpec.SessionProfile` 中携带一个 `SessionProfileMetadata` 记录：

| 字段 | 来源 |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | 所选预设的 `id` |
| `PresetIndex` | 所选预设的 `index` |
| `PresetName` | 所选预设的 `name` |
| `PackagePath` | 包目录的绝对路径 |

对于由此类 spec 构建的每个 `SessionPlan`，规划器都会添加一条信息性诊断信息 `session_profile.selected`，其数据键为 `session_profile.id`、`session_profile.name`、`session_profile.version`、`session_profile.author`、`session_profile.preset_id`、`session_profile.preset_index`、`session_profile.preset_name` 和 `session_profile.path`。它不影响可运行性。直接使用[公共 API](public-api.md) 的集成方可以调用 `OmsiLaunch.Core` 中的 `SessionProfileCompiler.Load` 和 `SessionProfileCompiler.Apply`；YAML 表示绝不会进入 `OmsiLaunch.Api`。

<a id="examples"></a>
## 示例

<a id="example-1-settings-only-profile-one-preset"></a>
### 示例 1：仅含设置项、一个预设的配置档

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

运行：`OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`。由于配置档没有定义 `new` 块，地图和入口点来自命令行；添加 `/set:graphics.maxFPS=60` 是允许的，添加 `/set:traffic.humans=10` 则属于冲突。

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### 示例 2：绑定地图、含三个预设和随包资源的配置档

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`，包内含有 `assets\splash\ENG.bmp`、`assets\splash\DEU.bmp` 和 `textures\offline.itx`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

运行：`OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`。使用 `/saved:situations\mytrip.osn` 时会跳过 `new` 块，且该 `.osn` 必须引用 `maps\Grundorf\global.cfg`。

<a id="the-packaged-example"></a>
### 随包示例

发行版附带 `docs/examples/session-profiles/rmg-leste/profile.yaml`（[查看](../../../examples/session-profiles/rmg-leste/profile.yaml)）。它在语法上有效、符合模式（schema），并且可以无错误地加载。但有两个特性使其在当前版本（build）中无法不经修改就启动会话：

1. 它设置了 `new.date`、`new.time` 和 `new.weather`，这些键会使计划不可运行（参见[`new` 块](#new)）。
2. 它的路径值是带双反斜杠的普通标量（`maps\\RMG Leste\\global.cfg`）。YAML 会保留双反斜杠，而地图标识按文本比较（仅做 `/` 到 `\` 的规范化），因此 `new.map` 和 `compatibility.maps` 无法与目录标识 `maps\RMG Leste\global.cfg` 匹配（规划时为 `OL_E_MAP_NOT_FOUND`）。`assets` 值仍能解析，因为 Windows 路径规范化会合并重复的分隔符。

当前版本（build）可运行的形式为：

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## 相关页面

- [CLI 参考](cli.md)：`/predefined-profile`、`/predefined-profile-index`、`/set` 以及世界参数
- [LaunchSpec](launchspec.md)：配置档编译生成的记录
- [事务与恢复](../concepts/transactions-and-recovery.md)：`settings`、启动画面和 `.itx` 覆盖层如何应用与还原
- [能力](capabilities.md)与[运行时验证状态](../status/runtime-validation-status.md)
