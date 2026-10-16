# CLI 示例

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/cli-examples.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

以下是 OmsiLaunch `0.1.0-beta4` 中 `OmsiLaunch.exe` 的最简且正确的调用方式，每个示例都附有预期的进程退出码，并说明会修改和还原哪些内容。除非另有说明，每个示例都从 OMSI 安装根目录（`<OMSI_PATH>`）运行；语法定义见 [CLI 参考](cli.md)，退出码见[退出码](exit-codes.md)。任何命令都可以加上 `--json` 以获取结构化信封（envelope）。

<a id="conventions"></a>
## 约定

- **修改**：命令改变的文件或 OMSI 状态。“会话覆盖层”指一个文件先被快照纳入事务，在 OMSI 启动前应用，并在会话结束时逐字节还原。
- **还原**：会话结束时（正常停止、Ctrl+C、托盘、`session stop`、`/observe-seconds`）或恢复时撤销的内容。
- 运行时写入（`time set`、`camera set`、`scripts variable set`、`vehicles spawn`）只改变 OMSI 的内存；由于 OMSI 在停止时被终止，它们从不被撤销。
- 占位符：`<OMSI_PATH>` 是包含 OmsiLaunch 包的 OMSI 2 安装（例如 `C:\OMSI 2`）；`<OTHER_OMSI_PATH>` 是另一个安装实例；`<SPEC_PATH>` 和 `<ITX_PATH>` 分别是您自己的 LaunchSpec 文件和网络纹理（Internet Textures）配置文件；`<HANDLE>` 是前面的 `list` 或 `create` 命令打印的句柄，`<BASE64>` 是 Base64 编码的像素数据。其他所有值都是字面量，可在标准 OMSI 2 安装上使用（`grundorf-quick` 是本页定义的示例配置档）。
- 包含空格的路径需要加引号，且带引号的路径不要以 `\` 结尾（Windows 参数解析会把 `\"` 转换为字面引号）：应写 `"C:\OMSI 2"`，而不是 `"C:\OMSI 2\"`。
- 本页的每条命令行都由文档门禁（`tests/OmsiLaunch.DocumentationTests`，门禁 `examples`）解析；标识、发现、规划和客户端示例还在真实安装上执行过（`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`）。

<a id="identity-and-discovery-no-session"></a>
## 标识与发现（无会话）

```text
OmsiLaunch.exe /version
```
退出码 `0`。打印 `product`、`version`（`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family`。不修改任何内容。

```text
OmsiLaunch.exe profiles --json
```
退出码 `0`。列出支持的 `Omsi.exe` 哈希及其验证状态。不修改任何内容。

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
退出码 `0`。公开能力目录；`help <family>` 对其进行过滤。不修改任何内容。

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
退出码 `0`（两种形式完全相同）。报告正在运行的 `Omsi.exe` 进程，以及是否有 OmsiLaunch 所有者为此安装实例作出响应。不修改任何内容。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
退出码 `0`（未知类别时为 `2`）。只读发现；目录联接循环会被跳过。不修改任何内容。此处打印的 `Identity` 值正是 `/map`、`/saved`、`/vehicle-scope` 和 `LaunchSpec` 所期望的确切字符串（例如 `maps\Grundorf\global.cfg`、`situations\Linie 5.osn`）。

<a id="planning-and-validation"></a>
## 规划与验证

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
计划为 `READY` 时退出码 `0`，为 `NOT RUNNABLE` 时为 `1`（例如 `OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`）。不修改任何内容；不启动 OMSI。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
退出码 `0`/`1`，同上；`/validate` 是 `/plan` 的别名。JSON 为原始的 `SessionPlan`（`TouchedFiles`、`PlannedMutations`、`Diagnostics`、`IsRunnable`）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
退出码 `1`。`/date`、`/time`、`/year`、`/weather*` 和玩家车辆参数会被接受，但此构建不会应用它们；计划带有 `OL_E_CAPABILITY_UNAVAILABLE`，不可运行。

```text
OmsiLaunch.exe /last /plan
```
退出码 `1`。`LAST_MAP_STATE` 对此构建配置不可用（`OL_E_CAPABILITY_UNAVAILABLE`）。

<a id="starting-sessions-owner-mode"></a>
## 启动会话（所有者模式）

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
会话以 `Completed` 结束时退出码 `0`，`Failed` 或计划不可运行时为 `1`。修改：会话覆盖层 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_<lang>.bmp`（托管启动画面为默认设置）、`closecheck` 处理、启动交接（handoff）。还原：会话结束时逐字节还原每个覆盖层。控制台会一直保持附着，直到 OMSI 退出、托盘“End session”被确认、客户端发送 `session stop`，或按下 Ctrl+C。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
退出码 `0`。与上例相同，但会话在进入 `Running` 8 s 后停止（托盘/管道停止时更早）。供验证脚本使用。

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
退出码 `0`/`1`。SAVED_SITUATION：地图、时间和位置来自 `.osn`（`situations\Linie 5.osn` 随 OMSI 2 提供，在 Berlin-Spandau 上以一辆玩家巴士开始）。该值是 `/list:Situations` 打印的情景标识（相对于安装目录，不区分大小写）；`Linie 5.osn` 这样的纯文件名无法解析（`OL_E_SITUATION_NOT_FOUND`，退出码 `1`）。`/map` 或 `/entrypoint-index` 与 `/saved` 一起使用会被拒绝，退出码 `2`。修改和还原与 NEW_MAP 相同。OMSI 自身会将情景的地图记录到 `options.cfg` 的 `[last_map]` 中；除非有 `/set` 覆盖 `options.cfg`，否则 OMSI 的这一写入不会被撤销（见[事务与恢复](../concepts/transactions-and-recovery.md)）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
退出码 `0`。修改：`options.cfg`（会话覆盖层，语义化的标记/向量补丁；保留 CP1252 字节）以及启动画面覆盖层。还原：精确还原 `options.cfg` 和启动画面文件（RV-005、RV-006）。`/set:graphics.texture=...` 以 `2` 退出（`OL_E_SETTING_NOT_WRITABLE`）；`/set:foo=1` 以 `2` 退出（`OL_E_UNKNOWN_SETTING`）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
退出码 `0`。修改：没有启动画面覆盖层；只有启动交接和 `closecheck` 处理。还原：启动画面没有需要还原的内容。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
退出码 `0`（目录或 BMP 缺失，或不是 640x480 24 位时为 `1`，附 `OL_E_SESSION_PRESENTATION_INVALID`）。修改：来自自定义目录的 `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_PTB.bmp`（会话覆盖层）。还原：精确还原这两个文件。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
退出码 `0`。修改：除启动画面覆盖层外，不修改磁盘上的任何内容；进程内下载器在会话期间被抑制。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
退出码 `0`（省略配置文件时为 `2`，附 `OL_E_ITX_PROFILE_REQUIRED`；配置文件无效或目标位于 `Texture\` 之外时为 `1`）。修改：`Texture\standard.itx`（会话覆盖层）；配置文件中列出的每个目标和 `Texture\standard.ipr` 都是会话删除项。还原：移除覆盖层，还原被删除的原始文件；会话期间 OMSI 在这些路径上创建的文件作为会话副产物被移除。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
退出码 `0`。等待 `Running` 最多 300 s（+5 s），而不是默认的 180 s。`/shutdown-timeout:60` 会被接受，但在此构建中没有效果。

<a id="predefined-session-profile"></a>
### 预定义会话配置档

配置档文件 `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`：

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
退出码 `0`。修改：`options.cfg`（预设中的设置项，会话覆盖层）以及启动画面覆盖层。还原：全部还原。添加 `/map:...` 或 `/set:graphics.maxFPS=60` 会以 `2` 退出（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；省略 `/predefined-profile-index` 会以 `2` 退出（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）。打包的示例 `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` 展示了完整的架构，但按发行时的内容，其 `new:` 块请求了 `date`、`time` 和 `weather`，而此构建无法应用这些项：用 `/new` 规划它会得到 `NOT RUNNABLE`（`OL_E_CAPABILITY_UNAVAILABLE`）；使用前请删除这些键。

<a id="launchspec-file"></a>
### LaunchSpec 文件

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
退出码 `0`/`1`。打包的示例选择 Grundorf、入口点索引 `1`、托管启动画面和原生网络纹理；`RootPath: "."` 解析为可执行文件所在目录。修改内容与显式 NEW_MAP 示例相同。含有未知属性的 spec 以 `2` 退出（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`）；文件缺失以 `6` 退出（`OL_E_SPEC_NOT_FOUND`）；超过 1 MiB 的文件以 `2` 退出（`OL_E_SPEC_TOO_LARGE`）。

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
退出码 `0`/`1`。显式安装实例 `<OTHER_OMSI_PATH>` 优先于 spec 的 `RootPath`；`/startup-timeout` 覆盖 spec 的 `Behavior.StartupTimeoutSeconds`，仅仅因为给出了该参数。

<a id="silent-detached-start"></a>
### 静默（分离）启动

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
`OmsiLaunchW.exe` 一经启动即以 `0` 退出（`{"delegated": true, "host_process_id": <pid>}`）；如果 `OmsiLaunchW.exe` 缺失（`OL_E_WINDOWS_HOST_MISSING`）或无法启动，则为 `7`。启动器立即返回，不会保持调用方的控制台或管道打开：捕获其输出的脚本会立刻收到文件结束（运行时闭包 `T04`）。会话本身在 `OmsiLaunchW.exe` 中运行：没有控制台输出，失败以消息框显示，托盘图标可用。可以用 `session status`、`events watch` 和 `.omsilaunch\diagnostics\<sessionId>-host.log` 查看进度。完整参考：[OmsiLaunchW.exe](omsilaunchw.md)。

<a id="controlling-a-running-session-client-mode"></a>
## 控制正在运行的会话（客户端模式）

在所有者运行期间，从同一安装目录运行以下命令。没有所有者响应时，每个命令以 `4`（`OL_E_NO_ACTIVE_SESSION`）退出；发生控制错误时以 `7` 退出。

```text
OmsiLaunch.exe session status --json
```
退出码 `0`。返回 `SessionId`、`State`（`14` = `Running`）、`Diagnostics`、`RuntimeEvents`。不修改任何内容。

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
退出码 `0`（`events watch` 一直运行到 Ctrl+C）。有界的运行时事件（`gameplay.entered`、D3D 生命周期事件……）。不修改任何内容。

```text
OmsiLaunch.exe session stop
```
退出码 `0`（`{"accepted": true, "session_id": "..."}`）。请求规范停止：OMSI 被终止，所有者还原覆盖层，事务日志被删除。客户端立即返回；所有者进程在还原后退出。

<a id="runtime-reads"></a>
## 运行时读取

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
退出码 `0`，信封中包含 `RuntimeCommandResult`（`Succeeded`、`Values`）。不修改任何内容。超时 8 s（`OL_E_RUNTIME_REQUEST_TIMEOUT`，退出码 `7`）。

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
退出码 `0`；缺少必需参数时为 `2`（`OL_E_RUNTIME_ARGUMENT_REQUIRED`，在发送请求之前报告）；插件拒绝请求时为 `7`：`OL_E_RUNTIME_OPERATION_FAILED`，具体原因在 `Values.detail` 中，例如句柄不再标识同一对象时为 `OL_E_RUNTIME_OBJECT_HANDLE_STALE`，或者 `OL_E_RUNTIME_CONSTANT_NOT_FOUND`。句柄是会话范围的，来自前面的 `list`。变量、常量和曲线名称由各车辆模型定义：请从 `list` 结果中获取。上面的名称是针对 `rv-000001`（一个 `situations\Linie 5.osn` 会话中的玩家巴士）列出的。不修改任何内容。

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
退出码 `0`。没有层级路由的操作，或任何有路由的操作，都可以通过操作 ID 调用。`timetable.track-entries.list` 是有界列表：在 `situations\Linie 5.osn` 上，它返回了 825 个条目中的 137 个，并带有 `truncated=true`（文档审计运行时复测）。不修改任何内容。

<a id="runtime-writes"></a>
## 运行时写入

```text
OmsiLaunch.exe time set --minute=30
```
退出码 `0`。修改 OMSI 内存中的时钟（已验证：写入、回读、通过第二次 `time set` 还原）。停止时不撤销。

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
退出码 `0`（`camera lock` 缺少 `--family` 时为 `2`）。在会话期间修改摄像机状态。`camera lock` 需要一个 PlayerVehicle（例如 `/saved` 会话）；在没有玩家车辆的 `/new` 会话中它会失败（`Values.detail` 中为 `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`）。锁定和解锁已使用已保存情景经运行时验证（族 0、2 和 1，并进行了摄像机回读）。停止时不撤销；锁定策略随会话结束而结束。

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
退出码 `0`（缺少 `handle`、`name` 或 `value` 时为 `2`）。修改该车辆的一个数值脚本变量。不撤销。

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
退出码 `0`。写入 OMSI 的源天气记录并停止实时 METAR 更新；OMSI 会在下一次未暂停的天气刻中据此推导雾、光照、实际风和湿度。在写入任何内容之前会检查每个值（`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`，退出码 `7`）。诸如 `relative_humidity` 之类的派生键返回 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`，并指明应改为设置的键。不撤销。EXPERIMENTAL：运行时验证待完成。

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
退出码 `0`。切换为该站点的实时 METAR 天气；OMSI 会在下一次天气更新时下载它。EXPERIMENTAL：运行时验证待完成。

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
退出码 `0`。`scripts string set` 通过 OMSI 自身的 `SetStringVariable` 执行并返回回读的值；车辆脚本未声明的名称返回 `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（退出码 `7`）。`scripts trigger` 像 OMSI 的输入处理程序一样触发一个触发器（`1` 按下，`0` 释放）；车辆脚本会立即运行它。触发器和变量的名称由各车型定义；上面的名称仅为示例。不撤销。EXPERIMENTAL：运行时验证待完成。

<a id="spawn"></a>
## 生成

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
退出码 `0`，`Values` 中包含新的 `rv-NNNNNN` 句柄（缺少 `--model` 时为 `2`；出现 `OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID` 时为 `7`）。客户端超时 30 s。修改道路车辆集合（添加一辆车）；不会指派玩家车辆。不撤销；该车辆在停止时随 OMSI 一起消失。

<a id="d3d-textures"></a>
## D3D 纹理

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` 是 `create` 打印的 `handle` 值（`d3dtex-<session id>-<16 hex digits>`）。对于 32 位格式，`<BASE64>` 解码后必须为 `width * height * 4` 字节（8 x 8 x 4 = 256 字节），且最多 48 KiB。退出码 `0`；缺少必需参数时为 `2`；出现 `OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_INVALID_PIXEL_BUFFER`、`OL_E_D3D_RESOURCE_RELEASED`（第二次释放，或释放后再 describe）、`OL_E_D3D_STALE_RESOURCE_HANDLE`（设备重置之前的句柄，或来自另一个会话的句柄）、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST` 时为 `7`。创建由会话拥有的 GPU 资源；这些资源被显式释放，或在 OMSI 结束时释放。不触及任何文件。

<a id="owner-side-single-runtime-operation"></a>
## 所有者端的单次运行时操作

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
退出码 `0`。启动一个会话，在进入 `Running` 后执行一次 `time.read`（超时 5 s），写出 `.omsilaunch\diagnostics\<sessionId>-runtime-operation.json`，继续运行 5 s，然后停止并还原。运行时失败会打印为 `runtime_error`，不会结束会话。

<a id="recovery"></a>
## 恢复

```text
OmsiLaunch.exe /recovery-status --json
```
退出码 `0`：不存在事务日志时为 `{"pending": false, ...}`，存在时为 `{"pending": true, "recovered": false}`。所有者持有该安装实例时退出码为 `7`（`OL_E_INSTALLATION_BUSY`）。不修改任何内容。

```text
OmsiLaunch.exe /recover --json
```
没有待处理内容或还原完成时退出码 `0`（`recovered: true`；`diagnostics` 中可能包含 `restore.session-artifact-removed` 和 `OL_W_RESTORE_FOREIGN_FILE_RETAINED`）；事务日志原本待处理且仍待处理时退出码 `8`（`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）；所有者或事务日志中记录的 OMSI 持有该安装实例时退出码 `7`（`OL_E_INSTALLATION_BUSY`）；还原后的字节未通过校验时退出码 `10`（`OL_E_INTERNAL`）（`Restore hash mismatch` 或 `Restore presence mismatch`；事务日志保持待处理）。修改：在校验 SHA-256 之后，从 `.omsilaunch\backup\<sessionId>\` 还原事务日志中记录的每个文件，然后删除事务日志和备份目录。事务日志中记录的 `Omsi.exe` 仍存活时，以 `OL_E_INSTALLATION_BUSY` 拒绝（运行时闭包 `S04`、`S04b`、`F01`）。

<a id="exit-code-quick-check-powershell"></a>
## 退出码快速检查（PowerShell）

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
