# 运行时验证状态

<!-- l10n: source=status/runtime-validation-status.md source-sha256=fe88f9c85e5cd7a0aa631f130cdbfa8a372574da34981c410f63e36322f3d7a3 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../status/runtime-validation-status.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页是 OmsiLaunch 0.1.0-beta4 唯一的证据表。每一行列出一项能力或功能及其证据状态。`RUNTIME_VALIDATED` 可以引用 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`（矩阵编号 RV-nnn、阻塞项 BI-nnn，以及其 2026-09-20 执行表中的会话 ID），也可以引用 Fable 之后的 Round A 报告 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-ROUND-A-001.md`（RA-nnn）；其余所有内容均由代码和离线测试套件推导得出。2026-09-21 的加固轮次（`research/reports/OMSILAUNCH-SECURITY-ROBUSTNESS-REVIEW-001.md`，发现项 S-01 至 S-34）改变了还原、恢复、控制平面和边界行为；这些更改当时只有离线证据。2026-09-23 的最终运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`，场景编号如 `F01`、`S05`、`T01`，候选版本 2 和 3）在已授权的安装实例上以真实会话运行了这些更改；由该轮次提升状态的行会引用其场景编号和会话 ID。因此，`RUNTIME_VALIDATED` 也可以引用该报告。

证据状态：

| 状态 | 含义 |
| --- | --- |
| RUNTIME_VALIDATED | 已在矩阵记录的规范会话中观察到（引用编号和会话）。 |
| STATICALLY_VALIDATED | 已实现并由离线测试（`tools/OmsiLaunch.TestHost`、`tests/*`）覆盖，自上次更改以来未在实际会话中观察到。 |
| PARTIAL | 已实现，但证据不完整、存在矛盾，或已知该行为受限。 |
| RUNTIME_VALIDATION_REQUIRED | 行为已更改或从未被观察到；必须先在真实会话中验证，才能提升状态。 |
| UNAVAILABLE | 未实现或被有意拒绝。 |

<a id="session-lifecycle-and-transaction"></a>
## 会话生命周期与事务

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| NEW_MAP 无界面启动（`world.new-map`、`world.presented-entrypoint`、`boot.headless-start`） | RUNTIME_VALIDATED | 矩阵“Existing Runtime Evidence”一节；会话 `e5454061`、`1e8e0548`、`5f641c8d`、`50a1f1ec`（Grundorf） |
| SAVED_SITUATION 启动（`world.saved-situation`） | RUNTIME_VALIDATED | `GetCapabilitiesAsync` 说明及矩阵（Berlin-Spandau，Start 窗体与 `Button1Click`） |
| `session.stop` 强制终止与精确还原 | RUNTIME_VALIDATED | 会话 `50a1f1ec`：正常停止后未残留 OMSI 进程，也未残留事务日志 |
| 启动幂等性（重复 `PluginStart`） | RUNTIME_VALIDATED | 会话 `50a1f1ec` |
| `options.cfg` 语义覆盖层与逐字节精确还原（RV-005） | RUNTIME_VALIDATED | 会话 `1e8e0548`：`AIMaxCountRandom` 150/200，并保留文件尾部；还原后的哈希为 `A12982C3...` |
| 托管启动画面与覆盖层事务（RV-006） | RUNTIME_VALIDATED | 矩阵 RV-006 PASS（展示批次：托管默认、托管自定义、`Unset`）；Release Presentation 001 会话 `388cf5d2-7310-4301-9313-e62c4c08a346`、`794cae34-a2ac-4fcf-8981-f142ac14c308`、`12a74b24-6c78-4863-8ffe-947651212eeb` |
| 提前退出恢复（RV-008，强制终止 OMSI） | RUNTIME_VALIDATED | Fable 之后的 Round A 会话 `9e7cad79-9c35-4f93-bfc8-71f455513f82`（进入游戏之后）和 `dcac497e-ff35-4c97-b5e4-98b660f88032`（`world.starting` 之后、进入游戏之前）：所有者正常完成，事务日志/租约已清除，`options.cfg` 及原本不存在的启动画面状态与快照一致。原生启动失败注入和还原失败注入仍单独处理。 |
| 启动失败与有意制造的还原失败边界（RV-008 其余部分） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：启动超时 `SF01` 会话 `0b21c664-b0b2-46b2-9b6b-def536765fff` 和插件报告的世界失败 `SF02` 会话 `53358c75-469b-41d5-9ddb-cdb1d1ff828d` 均以 `Failed` 结束，带有类型化错误码并完成精确还原；还原失败 `F01` 会话 `d4194a69-d858-42fe-97c7-ce93ccbdc3f6`（停止期间一个会话所属文件被仅用于测试的进程锁定）返回 `OL_E_RESTORE_FAILED`，保留了事务日志和备份，随后 `/recover` 将状态精确还原到会话之前。这些失败的制造者仅为测试基础设施。 |
| `.itx` 目标及 `Texture\standard.ipr` 的会话产物还原（S-01） | RUNTIME_VALIDATED | Round A 在 `47efd323-4ce1-400a-ac83-74893cf82f26` 中还原了已存在的路径；在 `8d9bdedf-d7de-4c64-a8b1-627ae4deff7b` 中于正常停止时创建并移除了一个已登记、原本不存在的目标；并在 `aa9a45d4-302e-4cf5-8490-f5a1c9e44b79` 中通过所有者中断加 `/recover` 重复了该清理。 |
| `closecheck` 作为会话删除项及失效标记移除（S-13） | RUNTIME_VALIDATED | Round A 会话（包括 `47efd323-4ce1-400a-ac83-74893cf82f26`）记录了 `closecheck.stale-removed`，随后是 `restore.session-artifact-removed`；未残留事务日志。 |
| 覆盖层构建之前的提前恢复、延迟的预指纹恢复（S-05） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`S05`：会话 `f755ed51-b85c-4df2-b062-5bf65c768b32` 的所有者在存在待处理事务日志时被终止（`maxFPS=77` 仍生效）；下一次启动即会话 `291ea22b-f9b3-42a4-be07-7f0c055c7113` 在 `TRANSACTION_STARTED` 之前记录了 `PENDING_JOURNAL_RECOVERED`，以自己的覆盖层（`88`）运行，结束时与第一个会话之前的状态逐字节一致。延迟的预指纹分支仍仅离线（`transaction.legacy-journal-ownership-migration`）。 |
| 元数据还原（时间戳、属性、只读）、直写刷新、备份清理（S-12） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`S12a` 会话 `40de172b-e144-4f0a-9207-09c80b5d7df7`（已知的 `options.cfg` 时间戳）和 `S12b` 会话 `d9e2534b-3cb3-47ef-9cbd-1b92c62a35e7`（时间戳加 ReadOnly）：会话期间覆盖层生效（为 OMSI 清除了 ReadOnly），字节、最后写入时间、创建时间和属性均被精确还原；未残留备份目录。 |
| 安装租约下的 `/recover`；PID 之前窗口期的拒绝（S-04） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`S04` 会话 `1fb2aad5-2985-4fa4-9c5a-6186f51d37b8`：所有者存活时，`/recovery-status` 和 `/recover` 返回 `OL_E_INSTALLATION_BUSY`，第二次启动被拒绝；终止所有者并让 OMSI 继续运行时，`/recover` 和新的启动均被拒绝，直到 OMSI 退出，随后 `/recover` 完成精确还原。`S04b` 会话 `5e57ca51-ab5e-4e94-a0d0-531735ee4057`：只要该根目录下有任何 `Omsi.exe` 在运行，PID 之前的事务日志（测试工具故障注入）就会被拒绝，退出后即被恢复；由该 OMSI 写入的 `closecheck` 会保留并给出 `OL_W_RESTORE_FOREIGN_FILE_RETAINED`（已记录在文档中，属保守处理）。 |
| 在 `StartSessionAsync` 时重新规划，对过时的计划返回 `OL_E_PLAN_NOT_RUNNABLE`（S-15） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R02`：规划之后将一个清单中列出的插件移走，`StartSessionAsync` 在任何会话存在之前抛出 `OL_E_PLAN_NOT_RUNNABLE: OL_E_PERMANENT_PLUGIN_MISSING`（计划 `4797c9ce-6eb3-47b1-b09d-03562dd3bd44`，候选版本 2，修复 BUG-01 之后） |
| 所有者生命周期：管道停止、Ctrl+C、托盘停止、OMSI 退出、所有者被终止、`/observe-seconds` | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`/observe-seconds` 期间的管道停止 `L06` 会话 `300a563e-7b0e-4f01-afbd-9e15df32c7e6`；Ctrl+C `L03` 会话 `5e2aca54-bae2-4fc6-a106-93699e755bfc`（所有者在收到信号后 577 ms 结束）；OMSI 被终止 `L05a` 会话 `d69658f9-16f6-43e9-8e36-682114b97e14`；关闭 OMSI 主窗口 `L05b` 会话 `76b11ba4-8ddb-4611-9a1c-8be317cd0337`（OMSI 忽略 `WM_CLOSE`；最终通过终止进程结束）；托盘停止 `T01`/`T03` 会话 `6d827c13-caf8-4221-832a-c91d26f130e0`、`7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`；所有者被终止 `S05`。每条路径均以精确还原结束。所有者被直接终止时，`Omsi.exe` 会继续运行（设计如此）；在其退出之前恢复会被拒绝（`S04`）。 |
| 控制台关闭或注销时 ProcessExit 的 4 s 时间预算 | RUNTIME_VALIDATED（控制台关闭） | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`L04` 会话 `559d0f1d-7d5b-4b6b-b70c-0acb272d53db`：关闭控制台窗口；还原在预算内完成，会话进入 `Completed` 且未残留事务日志；Windows 在关闭后约 5 s 结束了所有者（退出码 `0xC000013A`）。未测试注销场景。 |
| 协作式 WM_CLOSE 关闭 | UNAVAILABLE | 未实现（产品决定，S-11）。运行时收尾 `L05b`：向 OMSI 主窗口发送 `WM_CLOSE` 后，OMSI 在 30 s 内没有关闭。`ShutdownTimeoutSeconds` 未被使用。 |
| 安装租约语义（`Local\` 信号量） | STATICALLY_VALIDATED | `lease.cross-thread-release`；限制已作为已接受的风险记录在文档中（S-18） |
| 修补 OMSI 文件时保留 CP1252 编码（S-07） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`C01` 会话 `84a71235-d618-4917-9af4-c2a6025742f4`：通过 `/set` 修补了带有 CP1252 重音字符值（包括 0x80 和 0x96）的 `options.cfg`；实际文件保留了 CP1252 字节，没有出现 `EF BF BD`，会话结束后文件逐字节一致 |
| 内容发现跳过重解析点（S-23） | STATICALLY_VALIDATED | `discovery.*` 测试 |
| 诊断信息保留（最新的 50 个会话）（S-26） | STATICALLY_VALIDATED | `HostTrace.Prune` |
| 事务写入和删除时遇到的临时文件锁（杀毒软件、索引器）会重试约 1.5 s；持续存在的锁仍会导致失败（修正轮次 CP-10） | STATICALLY_VALIDATED | `transaction.transient-lock-retried`、`transaction.restore-failure-recovery`（持续的锁仍会失败）；修复之前观察到两次间歇性离线失败，修复之后 16 次运行中一次也没有 |

<a id="permanent-plugin-and-platform"></a>
## 永久插件与平台

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 构建配置 `Omsi23004_692EBFBF`（`692EBFBF...6243`，8,503,440 字节） | RUNTIME_VALIDATED | 所有矩阵会话 |
| 安装实例标识：租约和控制管道名称源自同一个定义 `InstallationPaths.IdentityKey`（`C:\OMSI` = `C:\OMSI\` = `C:\OMSI\.` = `C:\foo\..\OMSI` = 大小写变体；`C:\OMSI-A` 与 `C:\OMSI-B` 不同）（修正轮次 CP-02，附录 A/B） | STATICALLY_VALIDATED；已在运行时观察到 | `lease.root-normalization`、`paths.installation-identity-and-containment`。运行时收尾 `ID01`：会话 `01bfc780-10ef-4390-b700-c08bf809a42f` 运行期间，以 `\.`、`..` 路径段、小写和大写形式书写根目录的第二次启动和 `/recovery-status` 均被拒绝（`OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`）；带尾部分隔符的写法同样被拒绝（`ID01b`，会话 `ba3ea7c9-9826-453d-b447-81c48763e18f`）。 |
| 发行包一致性：干净的暂存、`Native.x86` 构建回执（基于内容而非时间戳）、无 BOM 的清单、暂存闭包及重新解压的归档的完整性、安装程序复制清单（修正轮次 CP-01、Round A RA-007、附录 F/G/H） | STATICALLY_VALIDATED；已在安装中观察到 | `Test-PackagingPipeline.ps1`（候选版本一致，另加 16 个反例）、`runtime.release-manifest-bom`、`runtime.release-manifest-strict-parser`。运行时收尾：安装到已授权安装实例中的包与其清单一致，且每个会话都以 `plugin.integrity.reference = manifest` 进行规划（候选版本记录 `research/reports/runtime-closure/CANDIDATES.md`）。 |
| 依据 `release-manifest.json` 的插件闭包完整性（S-09） | RUNTIME_VALIDATED | Round A 会话 `47efd323-4ce1-400a-ac83-74893cf82f26`；在已安装发行包上的运行时收尾会话（例如 `R01` 会话 `878b5e3c-2f9d-46bf-a918-11bb2e227e55`，候选版本 3）以 `plugin.integrity.reference = manifest` 进行规划；`R02` 表明缺少清单中列出的插件会阻止启动。 |
| 进程内构建校验（`plugin.build.invalid` -> `OL_E_BUILD_VALIDATION_FAILED`） | RUNTIME_VALIDATED | 矩阵会话；此前两个基于过时插件的 `plugin.build.invalid` 会话 |
| 带 SHA-256 的 Handoff v4 | RUNTIME_VALIDATED | 集成测试 `handoff.*`；每个会话 |
| 没有 OmsiLaunch 时插件保持惰性（无交接） | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`P01`（直接启动 OMSI，没有 OmsiLaunch 会话；OMSI pid 35564）：插件闭包和 .NET 宿主已加载，`OmsiLaunch.Native.x86.dll` 未加载，三个原生修补点保持未修补的 `Omsi.exe` 字节，且没有出现控制管道、事务日志或诊断信息 |
| DLL 搜索策略（`AssemblyDirectory | System32`）（S-24） | STATICALLY_VALIDATED | 程序集特性 |
| Windows 10+ x64 检测及拒绝其他平台 | STATICALLY_VALIDATED | `CurrentWindowsX64Platform.Detect` |

<a id="runtime-control-channel-and-control-plane"></a>
## 运行时控制通道与控制平面

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 邮箱传输完整性、会话绑定 | RUNTIME_VALIDATED | 所有运行时会话；`runtime-command.wire-guard`、`runtime-command.session-binding` |
| 丢弃迟到的响应，超时后通道不会卡死（S-08） | RUNTIME_VALIDATED | Round A 会话 `173414a5-ee53-4b10-aeb7-046f40b851e9`：外部客户端在 250 ms 后放弃了 `road-vehicles.spawn`；随后的 `map.read` 成功，规范停止清理了会话。超大响应的拒绝仍仅离线。 |
| 本地控制平面：按安装实例区分的管道名称、`session_id` 绑定、类型化处理程序错误、64 KiB 限制（S-06） | RUNTIME_VALIDATED | Round A 会话 `a00c6ff8-9cc9-4bce-ae44-c4314daad7bf`：无效协议、未绑定的停止、错误会话的运行时请求以及超大帧均被拒绝；随后有效的状态请求和已绑定的停止均成功。超大响应的拒绝仍仅离线。 |
| 遥测序列槽、跳过撕裂的样本、相同的连续事件（S-25） | STATICALLY_VALIDATED | `telemetry.sequence-samples` |
| API 边界拒绝 `internal.*` 并剥离内部结果键（S-02） | STATICALLY_VALIDATED | `OmsiLaunchService.ExecuteRuntimeAsync`；注册表校验 |
| 无效或超大的邮箱响应会释放槽；下一个请求成功（修正轮次 CP-03） | STATICALLY_VALIDATED | `runtime-command.oversized-response-rejected`；插件侧 `plugin-runtime.oversized-and-abandoned-responses`。无法在运行时制造：没有任何公开操作返回超过一个槽的数据（最大约 37.8 KB，运行时收尾 `R03`）。 |
| 运行时请求的每条终止路径都使邮箱保持可复用：类型化拒绝、超时、调用方取消、重复使用请求 ID、内部操作、错误会话、缺少参数（运行时）；格式错误、超大、损坏、过时和孤立的响应（离线） | RUNTIME_VALIDATED（可达路径） | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R03` 会话 `1134fdc3-a469-4ff5-a511-9964ed65e7cd`：每次失败都是类型化的，且下一个有效请求均成功。响应损坏路径无法从外部制造，仍仅离线（`runtime-command.terminal-paths-leave-channel-usable`）。 |
| 超大的控制平面回复以 `OL_E_CONTROL_RESPONSE_TOO_LARGE` 应答；状态/事件从最旧的开始裁剪至一帧（修正轮次 CP-04） | STATICALLY_VALIDATED | `control-plane.binding-and-typed-errors`。无法在运行时制造：运行时收尾轮次中观察到的最大公开回复约为 37.8 KB（`R03`），而一个会话的事件历史远小于一帧。 |
| 连接建立后控制平面绝不静默：格式错误、超大、负长度、空、JSON `null`、缺少命令、非字符串参数、无效协议、未知命令、未绑定和错误会话的帧；被截断的客户端帧；停滞的客户端；在回复之前离开的客户端 | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R04` 会话 `faaaf8a8-126d-4a23-a8f3-381e47c58de6`（候选版本 2，修复 BUG-02 之后）：每个帧都得到了其类型化错误，且之后的 `session.status` 均成功；停滞的客户端没有阻塞其他客户端；已绑定的停止与正在进行的 `road-vehicles.spawn` 竞争时，会话结束，spawn 的调用方得到干净的流结束。`OL_E_TIMEOUT` 和截断元数据仍仅离线（`control-plane.errors-never-silent`）。 |
| 运行时批处理测试工具（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`） | INTERNAL（STATICALLY_VALIDATED） | 仅为验证测试工具（S-27） |

<a id="runtime-operations"></a>
## 运行时操作

| 操作 | 状态 | 证据 |
| --- | --- | --- |
| `time.read`、`time.set` | RUNTIME_VALIDATED | 会话 `e5454061`（写入、`SetTime`、回读、还原） |
| `weather.read`、`weather.actual.read` | RUNTIME_VALIDATED | 会话 `e5454061` |
| `weather.set` | UNAVAILABLE | 以 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` 拒绝；会话 `e5454061` 和 `5f641c8d` 中持久性 FAIL；`50a1f1ec` 中拒绝 PASS |
| `map.read` | RUNTIME_VALIDATED | 在 Map 全局槽修正之后于会话 `9dd62626-94c6-4cd7-bb6f-0288327696f4` 中重新验证：`loaded=true`，19 个地块，Grundorf 标识、文件名、描述和年份范围均一致。 |
| `camera.read`、`camera.set` | RUNTIME_VALIDATED | 会话 `e5454061`（FOV 写入、回读、还原） |
| `camera.lock`、`camera.unlock` | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`CAM01` 会话 `5dfd9b96-fa94-4060-85af-0c9d19823a73`：已保存情景 `Linie 5` 提供了一个 PlayerVehicle（`rv-000001`）；对类别 0、2 和 1 的锁定均通过摄像机回读确认；解锁释放了该策略 |
| `road-vehicles.read`、`road-vehicles.list`、`road-vehicle.read` | RUNTIME_VALIDATED | 会话 `e5454061`、`5f641c8d` |
| RoadVehicle 句柄生命周期与失效检测（RV-002） | PARTIAL | 没有安全的移除制造方式：在三个运行时收尾窗口（`H01`、`H01b`、`H01c`，最长 180 s，并伴有时钟跳变）中，Grundorf 的 RoadVehicle 和 Human 数量只增不减；没有任何公开操作会移除对象。失效检测仍仅离线（S-19）。代次/ABA 规则已针对 D3D 纹理经运行时验证（`H02`）。 |
| `road-vehicles.spawn`（RV-003） | RUNTIME_VALIDATED | 会话 `5f641c8d`：`2 -> 3`，`rv-000003` 已解析 |
| `road-vehicles.place-random` | RUNTIME_VALIDATED | 矩阵：集合 `2 -> 4` |
| `player-vehicle.read` | RUNTIME_VALIDATED | Round A RA-002 会话 `9dd62626-94c6-4cd7-bb6f-0288327696f4`（`present=false`）；运行时收尾 `CAM01` 会话 `5dfd9b96-fa94-4060-85af-0c9d19823a73` 从已保存情景中读取到了存在的 PlayerVehicle |
| `humans.read`、`humans.list`、`human.read` | RUNTIME_VALIDATED | 会话 `e5454061` |
| `timetable.read`、`timetable.*.list`、`timetable.logs.read` | RUNTIME_VALIDATED | 会话 `e5454061`；线路轨迹条目 91 条记录 |
| 有界列表截断，取代 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（文档审计 BUG-05） | RUNTIME_VALIDATED | 在包 `9F8CC87B...`、已保存情景 `situations\Linie 5.osn`（Berlin-Spandau）上进行的文档审计复测：`timetable.track-entries.list` 返回了 825 个条目中的 137 个，`timetable.tour-entries.list` 返回了 4747 个中的 224 个，并带有 `truncated=true`（会话 `08df97b8-06cc-4faa-be8b-f49ca1538dce` 及文档截取复测）；修复之前两者均以 `OL_E_RUNTIME_RESPONSE_TOO_LARGE` 失败（会话 `f51dcb59-a723-4570-b6c5-b61e628a7993`）。证据：`research/reports/documentation-audit/runtime/`。 |
| `drivers.read`、`tickets.read` | RUNTIME_VALIDATED | 会话 `e5454061` |
| `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RUNTIME_VALIDATED | 在 `research/reports/OmsiHook-Parity-Wave-A-Closure-001.md` 中记录为 Runtime-Proven 的运行时会话里，`Refresh_Strings` 0 -> 1 -> 0（枚举了 1,025 个名称，依据 `OMSILAUNCH-AUTONOMOUS-BUILD-001.md`）；该报告未记录会话 ID |
| `vehicle.string-variables.list`、`vehicle.string-variable.get` | RUNTIME_VALIDATED（仅读取） | 会话 `e5454061`；写入为 BI-004 UNAVAILABLE |
| `vehicle.constants.list`、`vehicle.constant.get`、`vehicle.curves.list`、`vehicle.curve.evaluate`、`vehicle.hofs.read` | RUNTIME_VALIDATED | 会话 `e5454061` |
| `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release`、拒绝已释放的句柄 | RUNTIME_VALIDATED | 矩阵“basic D3D texture lifecycle”；`runtime.d3d.*` 能力说明（设备槽见会话 `4378899d`） |
| D3D 设备丢失/重置/恢复事件、代次失效（RV-007） | RUNTIME_VALIDATED（resetting/restored） | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`D01` 会话 `fc07946e-58dd-4ddc-af7e-10c7e61a899a`：一次临时的显示模式更改（测试制造方式，从不公开，无合成的重置调用）使 OMSI 两次重置其设备。发出了 `d3d.resetting`/`d3d.restored`，每个周期代次都会前进，重置期间的请求得到 `OL_E_D3D_RESET_IN_PROGRESS`，丢失前的纹理以 `OL_E_D3D_STALE_RESOURCE_HANDLE` 被拒绝，新纹理正常工作。OMSI 直接进入 `DEVICENOTRESET`，因此未产生单独的 `lost` 状态转换。有一次尝试使 OMSI 在 `DSound.dll` 中崩溃（显示器音频端点变化；所有者完成了精确还原）。 |
| `events.read` / `events watch` | RUNTIME_VALIDATED | 每个会话（遥测驱动的生命周期） |
| `internal.road-vehicles.make-basic` | INTERNAL | 无法通过公开接口访问 |

<a id="launch-features"></a>
## 启动功能

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| `/set` 及会话配置档 `settings`（`configuration.options.semantic`） | RUNTIME_VALIDATED | RV-005 |
| 会话配置档（严格编译器、冲突、路径限制，包括重解析点 S-17） | RUNTIME_VALIDATED（配置档启动范围） | Round A RA-009，`Test-Beta3SessionProfile.ps1`，2026-09-22：托管启动画面、禁用网络纹理（Internet Textures）以及 `graphics.maxFPS=30` 预设；`options.cfg` 已还原，测试夹具完好，无事务日志/租约/进程残留。 |
| `/spec` 加载（1 MiB 限制，拒绝未知属性，遵守超时设置）（S-20） | STATICALLY_VALIDATED | `cli.*` 测试 |
| 托管启动画面 `PTB`/`ENG`/`DEU`/`FRA` | RUNTIME_VALIDATED | RV-006 |
| 网络纹理 `Disabled`（进程内下载器抑制） | STATICALLY_VALIDATED | `internet-textures.suppressed` 遥测路径；矩阵中未引用 |
| 网络纹理 `.itx` 目标防护（Round A RA-011/RA-012，修正轮次 CP-05，附录 E）：规范根目录、规范目标、相对路径、路径段规则；只有根目录之下名为 `texture` 的目录段才符合条件（`mytexture`、`texture_backup`、`texture2` 以及位于绝对根路径中的 `texture` 均不符合）；遍历、绝对、相对根和联接点目标均被拒绝；不同写法会归并为同一个规范事务路径 | STATICALLY_VALIDATED | `presentation.itx-target-root-normalization`、`presentation.itx-target-guard-rejections` |
| 网络纹理 `Override` | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`I01` 会话 `6eac2dac-c7e9-49a1-acbf-2a5d59c1c0d3`：OMSI 的下载器针对一个已存在的目标和一个原本不存在的目标向环回服务器发送了 `HEAD` 和 `GET`，写入了两者；规范停止之后，原本不存在的目标被移除，已存在的目标被逐字节精确还原；`I02` 会话 `a8f2b720-e41a-4e34-86bf-125bf519a5bd`：在所有者和 OMSI 都被终止之后，通过 `/recover` 得到相同结果。 |
| `/date`、`/time`、`/year`、天气参数、玩家车辆参数 | UNAVAILABLE | 规划器将计划标记为不可运行（`OL_E_CAPABILITY_UNAVAILABLE`）；插件拒绝非 `Unset` 的日期/时间模式 |
| `/entrypoint:<identity>`（BI-001） | PARTIAL | 计划不可运行（计划证据 `PARTIAL`） |
| `/last`（`LastMapState`，BI-006） | UNAVAILABLE | 计划不可运行；没有 `.osn` 回退 |
| `InputSpec` 键盘/控制器覆盖层（BI-005） | UNAVAILABLE | 解析器已存在；但不会应用 |
| 无界面 PlayerVehicle 分配（BI-007）、`SetActualDateTime`（BI-002）、ICAO 天气控制（BI-003）、字符串变量写入（BI-004）、跨地块重新定位（BI-008） | UNAVAILABLE | 在实现完成之前处于阻塞状态 |
| `/list` 发现（`content.*`） | STATICALLY_VALIDATED；已在运行时观察到 | `discovery.fixture`；运行时收尾对已授权安装实例使用了 `/list:situations`（标识中包括非 ASCII 的 `situations\Nur für Fortgeschrittene.osn`） |
| `/quiet`、`/serve` | UNAVAILABLE | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT |
| 诊断参数（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace*`） | PARTIAL | 由 `DiagnosticsSpec` 携带；效果仅限于宿主跟踪 |

<a id="windows-host-and-tray"></a>
## Windows 宿主与托盘

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 托盘指示器（`OmsiLaunchW.exe`）：通知区域中的图标、状态、带确认和 Cancel 的“End session”菜单项（用于结束会话）、资源管理器重启、停止到达时对话框处于打开状态、`/observe-seconds` 期间的停止、失败对话框（S-16） | RUNTIME_VALIDATED | 在候选版本 3（修复 BUG-04 之后）上进行的运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：`T01` 会话 `6d827c13-caf8-4221-832a-c91d26f130e0`（在 `Shell_TrayWnd` 中找到图标，pt-BR 状态窗口，资源管理器重启后重新添加了图标，Cancel 保留了会话，End session 在 607 ms 内结束会话并移除了图标）；`T02` 会话 `0939a151-bf03-4ca9-8294-8b0088df7d30`（管道停止期间状态窗口和确认对话框处于打开状态）；`T03` 会话 `7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`；`T04` 会话 `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（参数错误、无活动会话以及带错误码的会话失败对话框）。未产生 shim 退出码 100-106。 |
| 宿主启动后 `/silent` 重新启动返回 0 | RUNTIME_VALIDATED | 运行时收尾轮次（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`T04` 会话 `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（候选版本 3，修复 BUG-03 之后）：启动器返回 0，捕获其输出的调用方在 0.4 s 后得到流结束，而 OmsiLaunchW 会话在没有控制台窗口的情况下运行；会话停止时完成了精确还原 |
| 引导程序 shim 退出码 100-106、动态路径缓冲区（S-21） | STATICALLY_VALIDATED | `Test-ReleaseIdentity.ps1` |

<a id="documentation-audit-retest-010-beta3-documentation-closure"></a>
## 文档审计复测（0.1.0-beta3 文档收尾）

在已授权安装实例上的包 `9F8CC87B238A677F7238C262E8FE0496832263B6C0A58083409EB485C03C6F17`，其中 BUG-05、BUG-06、BUG-07 及冒烟测试在最终包 `B8462ED6C596843E778C33DF4E93EBA3ACC875B7E43B011D60E5A21A16032E35` 上重复进行（R01 会话 `56cdee4b-9160-49fc-b0fd-5134ff9fe2d0`，文档截取会话 `40046805-5337-48fc-90dd-e7d3500ed3b4`）；证据位于 `research/reports/documentation-audit/runtime/` 和 `research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`。

| 项目 | 状态 | 证据 |
| --- | --- | --- |
| `OmsiLaunchW.exe` 报告不可运行的启动（BUG-06） | RUNTIME_VALIDATED | 在 `OmsiLaunchW.exe` 下执行 `/new ... /date:2026-09-20`：对话框显示 `Requested capability unavailable: world.explicit-date` / `Code: OL_E_CAPABILITY_UNAVAILABLE`，退出码 1，无 OMSI 进程（`doc-bug06-dialog.json`；已安装包 `9f8cc87b...`，按设计不会创建会话） |
| 公布的 CLI 路由是真实语法（BUG-07） | RUNTIME_VALIDATED | 已安装的 `help --json` 和 `capabilities --json` 输出的每个客户端路由备选写法（32 个描述符）都被已安装的解析器接受；没有任何一个输出用法说明（`doc-bug07-routes.json`；已安装包 `9f8cc87b...`） |
| CLI 示例页面中的每一条命令行 | RUNTIME_VALIDATED | 针对所有者会话 `08df97b8-06cc-4faa-be8b-f49ca1538dce`（已保存情景；跳过了 `events watch`，因为它会一直运行直到 Ctrl+C）执行了 43 条客户端命令行，最后执行该页面自己的 `session stop`（所有者退出码 0）。除 `vehicle.constant.get` 使用 `name=max_speed` 外，所有结果都与页面一致；该常量在那辆巴士上并不存在，页面现已改用 `antrieb_getr_version`。`grundorf-quick` 配置档示例未能完成规划（普通 YAML 标量中的反斜杠被重复书写），已予以更正；所有无会话的命令行均从安装根目录运行，启动命令行使用 `/plan`（`doc-client-examples.json`、`doc-examples-no-session.json`、`doc-profile-example.json`） |
| 在最终包上使用 `/observe-seconds` 的 NEW_MAP 冒烟测试 | RUNTIME_VALIDATED | `R01` 会话 `08570134-cfd8-4726-a472-5f6dd2f04c8f`，结束状态干净（`research/reports/runtime-closure/doc-R01-*`） |

<a id="beta-4-development-round-a-2026-09-28"></a>
## Beta 4 开发（A 轮，2026-09-28）

已知布局 `Omsi23004` 上的开发候选版本 `4AF6A180…`（`research/reports/beta4-runtime/ROUND-A-REPORT.md`）。

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 进程内按能力的原生解析、无地址的原生边界（已知布局） | RUNTIME_VALIDATED | A02 会话 `088f0491-114a-4841-afc0-e5ba483364b5`：headless 启动、NEW_MAP、43 个客户端示例中的 40 个（2 个失败是有记录的 `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` 和 `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`）、通过原生 `SetTime` 执行 `time set`、通过原生 `MakeVehicle` 执行 `vehicles spawn`；A03 会话 `cf49d2c1-ca69-41e2-af0c-c4b08fa083c3`：已保存情景 `Linie 5`。会话拥有的文件被精确还原。 |
| 在没有已知布局的 OMSI 2 版本上进行与版本无关的解析 | STATICALLY_VALIDATED | 离线：隐去其代码标识的参考可执行文件将 82 个非布局目标中的 81 个解析为参考值（`build-detection.reference-executable`、`native-resolution.per-capability`）。没有其他 OMSI 2 版本可用于会话。 |
| OMSI 2 检测、计划证据、`profiles` 报告 | RUNTIME_VALIDATED | 已安装候选版本 `4af6a180` 上的 A01：`is_omsi2`，标识 `known`，`build_id` 为 `Omsi23004`；会话 `088f0491` 和 `cf49d2c1` 的计划接受了该安装。 |
| 带类型的运行时错误码保留至 CLI | RUNTIME_VALIDATED | A02 会话 `088f0491-114a-4841-afc0-e5ba483364b5`（上述错误码原样到达 CLI）。 |
| HOF 管理（镜像、精确还原） | RUNTIME_VALIDATED | C01 会话 `e37318a2-73b3-4fb3-be11-b0225f15226d`：会话期间 `Vehicles\MB_O305` 恰好包含 `Vehicles\MAN_SD202` 的 14 个 HOF，并按字节还原，包括时间戳和属性。 |
| HOF 管理（崩溃恢复） | RUNTIME_VALIDATED | C02 会话 `02568113-3f28-4e31-958b-23eac83d7b0b`：在 `Running` 时终止了所有者和 OMSI；`/recover` 精确还原了 `Vehicles\MAN_SD200` 并删除了事务日志。 |
| 会话配置档 1..99 与发现 | STATICALLY_VALIDATED | 离线门禁 `session-profiles.preset-range-1-99`、`session-profiles.discovery`、`session-profiles.hofs`。 |
| 网络纹理（Internet Textures）`Disabled` | STATICALLY_VALIDATED | A04 在该模式下正常启动并结束；Beta 3 的最新值槽无法观察到 `PluginStart` 遥测。已被 B 轮取代（见下文）。 |

<a id="beta-4-development-round-b-2026-09-29"></a>
## Beta 4 开发（B 轮，2026-09-29）

已知布局 `Omsi23004` 上的开发候选版本 `beta4-b2` 至 `beta4-b5`（`research/reports/beta4-runtime/ROUND-B-REPORT.md`）。

| 功能 | 状态 | 证据 |
| --- | --- | --- |
| 遥测事件环（插件的每个事件按顺序送达；单调的会话 tick） | RUNTIME_VALIDATED | B03 会话 `0c5ae237-c587-47b4-a409-a55989e7e38b`：按顺序收到全部 `PluginStart` 事件，包括 `native.capabilities` 和 `internet-textures.suppressed`，没有 `telemetry.lost`；B06 会话 `f231ab2a-5b0d-40e6-a21d-085e061f1de8`：事件 tick 单调。 |
| 规范能力目录与安装上的 `GetCapabilitiesAsync` | RUNTIME_VALIDATED | B 轮，已安装候选版本 `1355c928` 上的 B01：53 个目录条目，带有操作、原生能力、分页和修改策略；`GetCapabilitiesAsync` 评估了 68 个条目（`time.read`：`native: RESOLVED_KNOWN_LAYOUT`）；B02 会话 `d875a24e-8154-4774-93b3-83567a90dbf1` 通过派生的 CLI 路由运行了每个客户端示例。 |
| 运行时模块 v1：信任、规划、内容激活、生命周期、操作、`disable-module` | RUNTIME_VALIDATED | B05（安装上的信任与计划）；B06 会话 `f231ab2a-5b0d-40e6-a21d-085e061f1de8` 和 `227d35a3-7e3d-4fcb-9013-f129840c49a7`：三个模块已加载、已就绪并处于待命状态（其中一个通过内容激活），通过 CLI 执行操作并返回带类型的错误，一个失败的模块被禁用并释放，而 OMSI 继续运行。 |
| 运行时模块 v1：`fail-session` | RUNTIME_VALIDATED | B07 会话 `59324c59-c3bc-4118-9fe9-57fbdc26f353`：一次操作失败使会话在 185 ms 后以 `OL_E_MODULE_SESSION_FAILED` 结束，OMSI 停止且文件被还原；加载时的情形见会话 `8b1d392f-4453-4e88-9231-d41a8fff1829`。 |
| 原生模块组件与钩子注册表 | PARTIAL | 会话 `f231ab2a-5b0d-40e6-a21d-085e061f1de8`、`227d35a3-7e3d-4fcb-9013-f129840c49a7` 和 `dfbd70a5-11a9-401d-967c-a9d154e15129`：已安装、链接并移除对 `Omsi.exe` 某个导入的 IAT 钩子；未知目标被拒绝；已安装设备的 `Present` 和 `Reset` 钩子（`Reset` 叠加在产品自己的钩子之上）。未决：在该安装上，注册表之外的某个组件在游戏开始后不久把运行时原始的 `Present` 和 `Reset` 写回了设备的 vtable，因此设备钩子没有持续生效（该模块的 `Present` 绕行从未运行）；注册表报告这两处均已被更改（`OL_E_MODULE_CLEANUP_INCOMPLETE`），且没有覆盖它们。属于 E 阶段事项。 |
| 运行时列表的统一分页 | RUNTIME_VALIDATED | B04 会话 `8a45e79c-37c2-4f66-a3a3-b536a00a2421`（Berlin-Spandau）：20 个列表被分页到恰好等于其计数，没有重复行，其中包括按邮箱容量分页的 4747 个班次条目和 825 个线路轨迹条目；游标和限制错误均带类型。 |
| 网络纹理（Internet Textures）`Disabled` | RUNTIME_VALIDATED | B03 会话 `0c5ae237-c587-47b4-a409-a55989e7e38b`：已应用原生抑制（`internet-textures.suppressed`，没有 `internet-textures.suppression.failed`），会话正常运行并结束。 |

<a id="consolidated-runtime_validation_required-list"></a>
## 汇总的 RUNTIME_VALIDATION_REQUIRED 列表

2026-09-23 的运行时收尾轮次清空了之前的队列。除无法在此安装实例上安全制造的项目外，该队列保持为空：

1. RV-002 RoadVehicle 和 Human 的移除：没有安全的移除制造方式（上方对应行为 `PARTIAL`）。

因无法从产品外部制造而保持仅离线的项目已在各自行中标注，不属于该队列：邮箱响应损坏、超大回复、事件历史截断、D3D 的 `lost` 状态转换、注销、shim 退出码 100-106，以及延迟的预指纹恢复分支。

相关页面：[能力](../reference/capabilities.md)、[已知限制](../reference/known-limitations.md)、[事务与恢复](../concepts/transactions-and-recovery.md)、[兼容性](../reference/compatibility.md)。
