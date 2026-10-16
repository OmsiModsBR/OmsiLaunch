# 运行时控制

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/runtime-control.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

运行时控制是 OmsiLaunch 在正在运行的 OMSI 会话内部执行的一组读取、写入和动作操作。本页说明访问它的三种途径（所有者 API、CLI 客户端、本地控制平面），请求如何从调用方传到插件再返回，句柄和请求标识如何工作，适用哪些超时，哪些内容有意不记入事务日志，以及参数与结果的约定，并为每个系列给出一个完整示例。操作清单本身（含参数、结果键和错误）见[能力](capabilities.md)。来源：`OmsiLaunchService.ExecuteRuntimeAsync`、`CurrentRuntimeCommandStore`（`src/OmsiLaunch.Process/RuntimeDeployment.cs`）、`CurrentRuntimeCommandMailbox` 和 `CurrentRuntimeControl`（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`）、`LocalControlPlane` 和 `CliInput`（`tools/OmsiLaunch.Cli/`）。

<a id="three-entry-points"></a>
## 三个入口点

| 入口点 | 使用者 | 路径 | 超时 |
| --- | --- | --- | --- |
| 所有者 API | 通过 `IOmsiLaunch.StartSessionAsync` 在进程内启动会话的集成方 | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> 注册表校验 -> 会话查找 -> 邮箱 | 调用方提供的 `TimeSpan` |
| 所有者 CLI（`/runtime:<op>`） | 拥有该会话的 `OmsiLaunch.exe` 进程 | 在进入 `Running` 后立即执行一个操作，结果写入 `.omsilaunch\diagnostics\<session>-runtime-operation.json` 并输出到控制台；会话继续运行 | 5 s（`road-vehicles.spawn` 为 15 s） |
| CLI 客户端 | 任何**不带**安装参数的 `OmsiLaunch.exe` 调用，例如 `OmsiLaunch.exe time get` | 通过命名管道发送 `runtime.execute` 给可执行文件所在安装实例的所有者 -> 所有者调用 `ExecuteRuntimeAsync` | 客户端和所有者两侧均为 8 s（`road-vehicles.spawn` 为 30 s） |
| 本地控制平面 | 同一 Windows 用户的任何进程 | 与 CLI 客户端相同的管道协议；见[本地控制](local-control.md) | 同上 |

所有路径最终都进入 `ExecuteRuntimeAsync`，它按以下顺序强制执行公开边界：

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`：不在 `PublicRuntimeOperationIds` 中的操作（包括所有 `internal.*` 操作）返回 `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必需参数或必需参数为空白时返回 `OL_E_RUNTIME_ARGUMENT_REQUIRED`。二者都不会触及会话。
2. 会话查找（句柄未知时抛出 `KeyNotFoundException`）；`RuntimeCommand.SessionId` 与句柄不一致时返回 `OL_E_RUNTIME_SESSION_MISMATCH`；状态不是 `Running` 时返回 `OL_E_SESSION_NOT_RUNNING`。
3. 邮箱请求（见下文），随后 `ScrubInternalValues` 移除所有以 `internal_` 开头或以 `_address`、`_pointer`、`_vmt` 结尾的结果键。

CLI 客户端和控制平面在联系所有者之前会自行执行第 1 步，因此即使没有活动会话，无效的命令名也会报告为退出码 2（`OL_E_RUNTIME_OPERATION_UNKNOWN` 和 `OL_E_RUNTIME_ARGUMENT_REQUIRED` 映射为 `InvalidArguments`；其他拒绝映射为 7；没有所有者映射为 4）。

控制平面端点仅在所有者处于 `Running` 状态时存在，即启动完成、且任何 `/runtime-batch`、`/runtime-write-batch` 或 `/d3d-batch` 测试批次执行完毕之后。会产生修改的命令（`runtime.execute`、`session.stop`）必须携带当前活动的 `session_id`；CLI 会自动从 `session.status` 获取它（否则返回 `OL_E_CONTROL_SESSION_MISMATCH`）。

<a id="request-identity-and-the-mailbox"></a>
## 请求标识与邮箱

`RuntimeCommand(SessionId, RequestId, Operation, Arguments)` 会被序列化为 `RuntimeCommandWire` 信封（envelope）（魔数 `OLRC`，版本 1，72 字节头部，JSON 负载的 SHA-256），并暂存到该会话 64 KiB 的单飞（single-flight）邮箱（`OmsiLaunch.Runtime.<sessionId>`）中。插件在 OMSI 的 UI 线程上每 50 ms 轮询一次邮箱，在该线程上执行操作并写入响应。

使通道稳健的规则：

| 规则 | 效果 |
| --- | --- |
| 单飞 | 每个会话同一时间只处理一个请求；宿主用信号量串行化调用方。新请求到达时若槽仍处于 `requested` 状态，则返回 `OL_E_RUNTIME_CHANNEL_BUSY`。 |
| 会话绑定 | 插件会忽略（并清除）会话 GUID 不属于自身的请求；宿主会拒绝会话 ID 或请求 ID 不匹配的响应（`OL_E_RUNTIME_RESPONSE_INVALID`）。 |
| 超时 | 截止时间过后，宿主将槽重置为空闲，并抛出 `TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`。 |
| 迟到的响应 | 插件仅在槽中仍保存着其请求 ID 时才发布响应；对已放弃请求的响应会被丢弃。如果仍有这样的响应落入槽中，宿主的下一个请求会发现一个过时的 `responded` 槽，将其丢弃后继续；如果该过时响应携带的请求 ID 与新请求**相同**，宿主会抛出 `OL_E_RUNTIME_REQUEST_ID_REUSED`。因此，调用方绝不能在同一会话内重复使用请求 ID。 |
| 大小 | 大于槽的请求会在暂存之前被拒绝（`ArgumentOutOfRangeException`）。无法放入的列表页会由插件缩短（丢弃末尾的行，`truncated=true`，`returned_count` 变小，`next_cursor` 指向第一个被丢弃的行）；其他任何超大响应都会被替换为带类型的 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 错误。 |
| 通道关闭 | 会话结束后返回 `OL_E_RUNTIME_CHANNEL_CLOSED`。 |

请求 ID 是由调用方选择的 `ulong`。CLI 使用固定区间：`/runtime:` 使用 `10_001`，转发的控制平面请求使用 `50_001+`，读取批次使用 `1+`，D3D 批次使用 `20_000+`，`D3DRuntimeApi` 使用 `30_000+`。集成方应在每个会话内使用单调递增的计数器。

<a id="handles-and-stale-detection"></a>
## 句柄与失效检测

| 前缀 | 类型 | 签发者 | 格式 |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`、`road-vehicles.spawn`（`created_handle`）、`player-vehicle.read` | `rv-` + 六位十进制数字（`rv-000003`） |
| `hb-` | Human | `humans.list` | `hb-` + 六位十进制数字 |
| `d3dtex-` | D3D 纹理 | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

句柄是不透明的，作用域为会话：它们由插件生成，从不编码地址，在其他会话中没有意义。解析句柄时，插件会检查其映射到的地址是否仍在 OMSI 的活动集合中（以最近一次列表读取为准；`road-vehicles.list` 和 `humans.list` 会刷新该视图），并重新读取对象指纹（Delphi VMT 加车辆定义指针，或 human 模型索引）。不匹配意味着原生对象已被销毁且其地址被复用：`OL_E_RUNTIME_OBJECT_HANDLE_STALE`。在重复的列表读取中，同一个活动对象会再次返回同一个标记。残余盲区：在两次列表读取之间，于同一地址重新创建的同类、同定义对象无法被区分。D3D 句柄由原生桥校验：未知句柄或属于其他会话的句柄返回 `OL_E_D3D_STALE_RESOURCE_HANDLE`，已释放的句柄返回 `OL_E_D3D_RESOURCE_RELEASED`，设备代次变化会将纹理标记为 `STALE`。

句柄从来不是原生地址，绝不能被解析、按数值比较、跨会话持久保存或传给另一个会话：应将其视为仅在签发它的会话中有效的不透明字符串。

运行时证据：已释放的 D3D 句柄在创建新纹理后仍被拒绝，上一个会话的句柄会被下一个会话拒绝（运行时收尾 `H02`）；D3D 设备重置会使所有活动纹理失效（`OL_E_D3D_STALE_RESOURCE_HANDLE`，`D01`）。RoadVehicle 和 Human 在自然移除后的失效检测（RV-002）没有安全的运行时触发方式（在观察时间窗口内 OMSI 没有移除对象，也没有公开操作能够移除对象）；该项由离线测试覆盖。

<a id="what-is-not-journaled"></a>
## 不记入事务日志的内容

运行时修改只改变 OMSI 的内存。**它们不会被记录到事务日志（journal）中，会话结束时也不会被还原**：`time.set`、`camera.set`、`camera.lock`（插件关闭时该策略停止）、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random` 以及所有 `d3d.texture.*` 资源。它们会随 OMSI 进程一同消失；OmsiLaunch 在会话结束时终止该进程，不让 OMSI 持久保存任何内容（见[事务与恢复](../concepts/transactions-and-recovery.md)）。运行时系列中的任何操作都不会触及文件系统。

<a id="argument-and-result-conventions"></a>
## 参数与结果约定

- 参数是字符串键/值对。在 CLI 中，命令词之后的 `--key=value` 会成为运行时参数（`OmsiLaunch.exe vehicles get --handle=rv-000001`）；对于 `/runtime:<op>`，等效写法为 `/runtime-arg:key=value`。数字使用固定区域性（invariant culture，小数分隔符为 `.`）；布尔值为 `true`/`false`。
- 命令词通过 `CliInput.HierarchicalRoutes` 映射到操作 ID（例如 `time get` -> `time.read`、`vehicles summary` -> `road-vehicles.read`、`scripts variable set` -> `vehicle.variable.set`）。完整的路由表见 [CLI 参考](cli.md)。D3D 操作没有命令词路由；请使用 `/runtime:d3d.status` 或 `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`。
- 结果是扁平的字符串字典。列表使用 `<row>.<n>.<field>` 形式的键（`vehicle.0.handle`、`track.3.filename`、`name.12`），并带有页字段 `count`、`returned_count`、`truncated` 和 `next_cursor`（参见[分页](capabilities.md#pagination)）。
- 失败结果带有 `Succeeded=false`、一个 `OL_E_` 形式的 `ErrorCode`，对于插件端失败还带有 `detail`（D3D 另有 `native_status`，意外故障另有 `exception`）。

<a id="cli-envelope---json"></a>
### CLI 信封（`--json`）

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

错误信封为 `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }`。不带 `--json` 时，CLI 将结果对象输出为缩进的 JSON，或输出 `CODE: message`。

<a id="api-envelope"></a>
### API 信封

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

对于注册表校验之后的边界违规（`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_*`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_RESPONSE_INVALID`），`ExecuteRuntimeAsync` 会抛出异常；对于注册表拒绝和插件端错误，则返回 `Succeeded=false`。

<a id="worked-examples"></a>
## 完整示例

所有 CLI 示例都假定包含 `OmsiLaunch.exe` 的安装实例已有一个所有者在运行（例如用 `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` 启动；示例需要玩家车辆时用 `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` 启动），并在同一安装实例的第二个控制台中运行。

| 系列 | 命令 | 作用 |
| --- | --- | --- |
| 会话 | `OmsiLaunch.exe session status --json` | 读取 `SessionId`、`State`、诊断信息以及有界的运行时事件列表。 |
| 时间 | `OmsiLaunch.exe time get`，然后 `OmsiLaunch.exe time set --hour=7 --minute=30` | 读取时钟；通过配置化的 `SetTime` 设置时钟并返回回读值。 |
| 天气 | `OmsiLaunch.exe weather get` 和 `OmsiLaunch.exe weather actual get` | 读取当前天气状态和实际/ICAO 天气状态。`OmsiLaunch.exe weather set --wind_speed=5` 设置手动天气（OMSI 的源记录；实时 METAR 停止），`OmsiLaunch.exe weather metar set --icao=EDDF` 切换到实时 METAR 天气。二者在经运行时验证之前均为 EXPERIMENTAL。 |
| 地图 | `OmsiLaunch.exe map get` | 地图标识、名称、描述、图块数、年份范围和行车方向。 |
| 摄像机 | `OmsiLaunch.exe camera set --field_of_view=50`，然后 `OmsiLaunch.exe camera lock --family=0 --preset=1` 和 `OmsiLaunch.exe camera unlock` | 写入 FOV；以预设 1 固定司机视角 family（需要玩家车辆，例如已保存情景）；解除该策略。 |
| 车辆 | `OmsiLaunch.exe vehicles summary`、`OmsiLaunch.exe vehicles list`、`OmsiLaunch.exe vehicles get --handle=rv-000001` | 计数；句柄；单个快照。 |
| 生成 | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | 创建一辆 AI RoadVehicle（30 s 超时）；返回 `created_handle`。`OmsiLaunch.exe vehicles place-random --group=1` 调用 `PlaceRandomBus`。 |
| 玩家 | `OmsiLaunch.exe player get` | headless 启动时为 `present=false`，否则返回玩家快照。 |
| 人物对象 | `OmsiLaunch.exe humans summary`、`OmsiLaunch.exe humans list`、`OmsiLaunch.exe humans get --handle=hb-000001` | 计数；句柄；单个快照。 |
| 时刻表 | `OmsiLaunch.exe timetable get`、`OmsiLaunch.exe timetable tracks list`、`OmsiLaunch.exe timetable logs list` | 管理器计数；有界的线路轨迹行；时刻表日志。 |
| 脚本 | `OmsiLaunch.exe scripts variable list --handle=rv-000001`、`OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`、`OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`、`OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`、`OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`、`OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | 数值变量的列出/读取/写入；字符串变量读取/写入；脚本和声音触发器（字符串写入和触发器在经运行时验证之前均为 EXPERIMENTAL）。 |
| 常量与曲线 | `OmsiLaunch.exe constants list --handle=rv-000001`、`OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`、`OmsiLaunch.exe curves list --handle=rv-000001`、`OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | 车辆常量与曲线求值。名称与车型相关：请使用 `list` 命令返回的名称（这些名称是针对 `situations\Linie 5.osn` 的玩家巴士列出的）。 |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | 车辆定义的 HOF 元数据。 |
| 司机与车票 | `OmsiLaunch.exe drivers list`、`OmsiLaunch.exe tickets get` | 司机记录；车票包。 |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8`，然后 `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` 和 `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | 在渲染线程上的纹理生命周期；`<HANDLE>` 是 `create` 输出的 `handle`；`--pixels_base64` 解码后必须为 `width * height * 4` 字节（32 位格式），且最多 48 KiB。 |
| 事件 | `OmsiLaunch.exe events read`、`OmsiLaunch.exe events watch` | 有界事件列表；轮询式监视（250 ms），直至按下 Ctrl+C。 |
| 停止 | `OmsiLaunch.exe session stop` | 请求规范停止：由所有者终止 OMSI 并还原事务。 |

<a id="stability"></a>
## 稳定性

通道本身（`runtime.command-channel`）为 `STABLE_BETA`：传输完整性、会话绑定、迟到响应的丢弃以及带类型的超大拒绝，均由离线测试 `runtime-command.wire-guard`、`runtime-command.session-binding` 和 `runtime-command.late-response-ignored` 覆盖；在运行时由 RV-003（会话 `5f641c8d`）、Round A RA-019（一个客户端在 250 ms 后放弃了 `road-vehicles.spawn`；下一个请求成功）以及运行时收尾的通道测试组 `R03`（超时、取消、重复使用的请求 ID、插件拒绝、内部操作、错误会话和缺少参数，每项之后都跟随一个成功的请求）覆盖。各操作的稳定性见[能力](capabilities.md)。

<a id="channel-reuse-after-failures"></a>
## 失败后的通道复用

运行时请求的每一条终止路径都会使邮箱保持可复用：成功、带类型的错误、格式错误、超大、属于其他会话或请求 ID 错误的响应、超时、调用方取消以及解码失败，最终都会进入同一个清理过程，将槽恢复为空闲并清除长度和信封头部，因此下一个请求无法读到先前请求的任何内容。新请求开始时若发现残留的请求或响应，会先将其清除（若其携带新请求的 ID，则返回 `OL_E_RUNTIME_REQUEST_ID_REUSED`）。在插件端，操作内部的异常以 `OL_E_RUNTIME_OPERATION_FAILED` 应答，大于槽的结果以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（一个固定的小信封）应答，针对其他会话的请求以 `OL_E_RUNTIME_SESSION_MISMATCH` 应答，而宿主已放弃的请求永远不会得到应答。这些规则由离线测试覆盖（`runtime-command.terminal-paths-leave-channel-usable`、`plugin-runtime.oversized-and-abandoned-responses`、`plugin-runtime.bounded-list-fits-slot`）；调用方能够触发的路径（超时、取消、重复使用的 ID、带类型的拒绝、迟到的响应）也有运行时证据（RA-019、`R03`）。损坏的、属于其他会话的或超大的响应无法从产品外部触发，仍然仅离线验证。
