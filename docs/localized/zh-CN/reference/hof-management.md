# HOF 管理

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/hof-management.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

OMSI 从每辆车辆的文件夹中读取 HOF 文件（`*.hof`：车辆的 IBIS/终点站显示屏所显示的终点站、线路编号和站点列表）。HOF 管理会**在单个会话期间**把 HOF 文件复制到车辆文件夹中：地图或附加内容可以随附其线路所需的 HOF，用户也可以把一辆车辆的 HOF 复用到其他车辆上，而不会永久更改任何车辆。本页是规范性参考，对应的实现为 `src/OmsiLaunch.Core/HofPlanner.cs` 中的 `HofPlanner`、[LaunchSpec](launchspec.md) 的 `HofManagement` 字段、`/hof-source`、`/hof-target`、`/hof-file` 和 `/hof-mode` 这些 [CLI](cli.md) 参数，以及[会话配置档](session-profiles.md)的 `hofs` 块。

稳定性：规划、冲突规则、路径限制以及事务性还原为 `STABLE_BETA`（离线门禁 `hof.plan-sources-and-modes`、`hof.plan-rejects-unsafe-input`、`hof.transaction-exact-restore`、`hof.recovery-after-crash`、`session-profiles.hofs`）。已经过运行时验证：一次镜像会话的精确还原，以及通过 `/recover` 进行的崩溃恢复（参见[运行时验证状态](../status/runtime-validation-status.md)）。

HOF 管理不同于 `PlayerVehicle.Hof` / `/hof:<id>`；后者在启动时选择玩家车辆的 HOF，在当前版本（build）中为 `UNAVAILABLE`。

<a id="model"></a>
## 模型

一个请求是一个操作列表（`HofManagementSpec.Operations`）。每个 `HofOperationSpec` 包含：

| 字段 | 含义 |
| --- | --- |
| `SourceKind`, `Source` | HOF 文件的来源（见下表）。 |
| `TargetVehicles` | 一辆或多辆已安装的车辆：`.bus` 或 `.ovh` 文件的标识（`Vehicles\MAN_SD200\MAN_SD77.bus`）或车辆文件夹（`Vehicles\MAN_SD200`），两者都必须位于 `Vehicles\` 之下。同一文件夹中的多个标识只会对该文件夹寻址一次。 |
| `Mode` | `Add`（默认）：把来源 HOF 复制到每个目标文件夹；该文件夹中的其他 HOF 保留不变。`Mirror`：复制来源 HOF，并在会话期间删除目标文件夹中所有其他 `.hof`，使该文件夹的 HOF 集合与来源集合相同。 |
| `Files` | 可选，要从来源中获取的文件名列表（不区分大小写）。省略时：来源中直接包含的所有 `*.hof`。列出的名称在来源中不存在时：`OL_E_HOF_SOURCE_NOT_FOUND`。 |

| `SourceKind` | `Source` | 解析为 |
| --- | --- | --- |
| `Collection` | 普通名称，例如 `berlin` | `<root>\.omsilaunch\hof\<name>\*.hof`：由用户或工具维护的集合。 |
| `Directory` | 相对于安装实例的目录，例如 `maps\RMG Leste\hof` | 该目录中的 `*.hof`：随地图或附加内容一起提供的 HOF。 |
| `Vehicle` | 车辆标识或文件夹，与目标的写法相同 | 该车辆文件夹中的 `*.hof`：把一辆车辆的 HOF 复用到其他车辆上。 |
| `File` | 相对于安装实例的 `.hof` 文件 | 仅该文件。 |

只使用直接位于来源目录中的文件（不递归）。从不跟随符号链接和目录联接（junction）（参见[路径限制](#confinement)）。

<a id="what-a-session-does"></a>
## 会话执行的操作

1. **规划**（`PlanSessionAsync`、`/plan`）。每个操作都会被解析：读取来源、检查目标、检测冲突。每个将被更改的文件都会出现在 `SessionPlan.PlannedMutations` 中，其 `SemanticKey` 为 `hof.add` 或 `hof.mirror`，`RequestedValue` 为来源标识，`Operation` 为 `exact-file-overlay`（写入）或 `session-deletion`（由镜像删除）；这些路径属于 `TouchedFiles`。出现问题时，计划会以下列错误码成为不可运行的计划。
2. **启动**。恢复失效的事务日志（journal）之后，会根据当前实际文件重新解析各操作，并将其交给会话事务：写入的文件是覆盖层（overlay），被镜像删除的文件是会话删除项。字节已与来源相同的目标文件保持不动，也不会成为事务条目。
3. **会话结束、失败、崩溃。** 事务会精确还原每个目标文件夹：原先存在的文件恢复其原始字节、时间戳和属性；会话创建的文件被删除；被镜像删除的文件重新出现。所有者崩溃后，同样的还原会在下次启动时或通过 `/recover` 执行（参见[事务与恢复](../concepts/transactions-and-recovery.md)）。

不会向目标文件夹之外写入任何内容。来源文件只会被读取。会话从不修改 `.omsilaunch\hof\`。

<a id="conflict-rules"></a>
## 冲突规则

各操作按顺序应用，并且必须彼此一致：

- 两个操作以相同字节写入同一目标文件时会合并；字节不同时：`OL_E_HOF_CONFLICT`。
- 镜像将删除某个由先前操作写入的文件，或者某个操作写入先前镜像删除的文件：`OL_E_HOF_CONFLICT`。
- 镜像覆盖该文件夹在会话中的 HOF 集合：已存在的文件加上先前操作写入该文件夹的文件。
- HOF 目标同时也被会话呈现（启动画面或网络纹理（Internet Textures））写入：`OL_E_HOF_CONFLICT`（在当前的呈现路径下不会发生，因为这些路径位于 `Vehicles\` 之外）。
- 当目标中已存在仅大小写不同的同名文件时，保留现有的拼写。

<a id="confinement"></a>
## 路径限制

每个来源和目标都必须位于安装实例之内：根路径、`..` 段以及离开根目录的路径都属于 `OL_E_HOF_PATH_ESCAPE`；位于 `Vehicles\` 之外的目标（以及 `Vehicle` 来源）、不是普通目录名的集合名称，以及经由目录联接或符号链接到达的任何来源或目标文件夹同样如此。本身是重解析点（reparse point）的 `.hof` 文件会被忽略。

<a id="limits"></a>
## 限制

| 限制 | 值 | 错误 |
| --- | --- | --- |
| 单个 HOF 文件的大小 | 8 MiB | `OL_E_HOF_INVALID` |
| 每个操作的文件数 | 256 | `OL_E_HOF_INVALID` |
| 每个会话写入的总字节数 | 256 MiB | `OL_E_HOF_INVALID` |
| 每个操作的目标数 | 至少 1 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## 错误码

| 代码 | 原因 |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | 集合、目录、车辆或文件不存在，不包含任何 `.hof`，或缺少 `Files` 中列出的文件。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | 目标车辆文件或文件夹不存在。 |
| `OL_E_HOF_PATH_ESCAPE` | 路径离开了安装实例、目标不在 `Vehicles\` 之下、集合名称不是普通名称，或者经过了目录联接/符号链接。 |
| `OL_E_HOF_CONFLICT` | 两个操作对同一目标文件的要求不一致（参见[冲突规则](#conflict-rules)）。 |
| `OL_E_HOF_INVALID` | 没有目标、没有来源、`File` 来源不是 `.hof`、目标是 `.bus`/`.ovh` 以外的文件，或超出了某项[限制](#limits)。 |

<a id="using-it"></a>
## 使用方法

命令行（一个操作；多个操作需要使用 `/spec` 或会话配置档）：

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec（`/spec` JSON，枚举以整数表示）：

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

会话配置档预设（`package` 来源是配置档包内的一个目录）：

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API：设置 `LaunchSpec.HofManagement`，然后照常调用 `PlanSessionAsync` / `StartSessionAsync`；`/list:Hofs`（`ContentQueryKind.Hofs`）列出已安装的 HOF 文件。

相关内容：[LaunchSpec](launchspec.md)、[会话配置档](session-profiles.md)、[CLI](cli.md)、[错误](errors.md)、[事务与恢复](../concepts/transactions-and-recovery.md)。
