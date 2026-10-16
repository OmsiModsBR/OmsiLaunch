# 事务与恢复

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../concepts/transactions-and-recovery.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

每个涉及 OMSI 文件的 OmsiLaunch 会话都在一个持久化、带事务日志（journal）的事务中进行：原始字节在被替换之前先行备份，事务日志记录会话进行到了哪一步，还原时会先校验每个备份再将其写回。本页说明该事务在 `FileConfigurationTransaction`（`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`）中的实现方式，以及它如何由 `OmsiLaunchService.StartAsync`、`SuperviseAsync` 和 `RecoverPendingAsync`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）驱动，并结合由 `SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）计算的文件输入。本页面向需要了解会话会更改哪些内容以及恢复会执行哪些操作的用户，也面向需要确切保证的集成方。

<a id="what-a-session-changes"></a>
## 会话会更改哪些内容

只有**临时覆盖层（overlay）**会进入事务。它们在事务打开之前计算，在事务关闭时还原。

| 会话输入 | 文件 | 类型 |
| --- | --- | --- |
| `/set:<key>=<value>`、配置档 `settings`、`LaunchSpec.Environment.*` | `options.cfg`（语义化 token 补丁；保留 CP1252 字节，遵循带 BOM 标记的 UTF-8/UTF-16） | 覆盖层 |
| 托管启动画面（`SplashMode.Managed`，默认值） | `GUI\NewSplashscreen_ENG.bmp` 和 `GUI\NewSplashscreen_<language>.bmp` | 覆盖层（安装实例中没有本地化文件时，该文件由事务创建） |
| 网络纹理（Internet Textures）`Override` | `Texture\standard.itx` | 覆盖层 |
| 网络纹理 `Override` | `.itx` 中列出的每个目标，以及 `Texture\standard.ipr` | 会话删除项 |
| 始终 | `closecheck`（会话前不存在时） | 会话删除项 |

永久性产品文件**不**参与事务：`plugins\OmsiLaunch.*` 下的插件闭包（仅做校验，参见[永久插件](permanent-plugin.md)）、`.omsilaunch\assets\splash\*.bmp`（只复制一次，从不删除）、`.omsilaunch\diagnostics` 下的诊断文件、会话配置档包，以及发行版文档和示例。第三方插件和其他所有 OMSI 文件从不被枚举、复制、删除或还原。

会话运行期间，OMSI 本身会像正常启动 OMSI 时一样继续写入自己的状态：`options.cfg`（例如会话加载其他地图时的 `[last_map]`，在进入游戏时重写）、`Texture\standard.ipr`、时刻表和光照贴图缓存（`Texture\Temp_Schedules\*`、`maps\<map>\*.map.LM.bmp`）、`maps\<map>\laststn.osn`、`Drivers\` 下的司机档案，以及它的日志文件。对会话所有路径（见上文）的写入会被还原撤销；其他所有 OMSI 写入在会话结束后保留，与直接运行 OMSI 后的情况完全相同。运行时收尾证据：在另一张地图上运行的已保存情景会话使 `[last_map]` 保持更改，因为它没有覆盖 `options.cfg`（`CAM01`），而 `/set` 会话则精确还原了 `options.cfg`（`S12a`、`S12b`、`C01`）。

<a id="transaction-states"></a>
## 事务状态

每次状态转换后，`TransactionState` 都会持久化到事务日志中。这些值由 `System.Text.Json` 序列化为整数。

| 值 | 状态 | 写入时机 |
| --- | --- | --- |
| 0 | `Prepared` | 已为每个覆盖层路径和删除路径生成快照，并将其备份刷新到磁盘。安装实例中尚未有任何更改。这就是恢复义务的起点：从此处起，崩溃会留下可恢复的事务日志。 |
| 1 | `Applied` | 每个覆盖层都已原子写入，每个删除项都已删除。 |
| 2 | `RuntimeDeployed` | 本次启动已校验永久插件完整性（不部署任何内容；该名称是历史遗留）。 |
| 3 | `HandoffCreated` | 启动交接、遥测槽和运行时邮箱已作为命名共享内存存在。 |
| 4 | `ProcessStarted` | `Omsi.exe` 已创建。事务日志此时还会记录 `ProcessId`、`ProcessStartFileTimeUtc`（创建时间，UTC tick）和 `ExecutablePath`。 |
| 5 | `ProcessExited` | 监管器已确认进程退出（自然退出或 `TerminateProcess`）。 |
| 6 | `Restoring` | 还原已开始。 |
| 7 | `Restored` | 每个所有的文件都已还原并校验。紧接着删除事务日志，并删除 `backup\<session>`。 |
| 8 | `Completed` | 在枚举中声明但从不持久化；已完成的事务没有事务日志。 |

因此，正常会话的生命周期为：快照 -> `Prepared` -> 写入覆盖层 / 删除删除项 -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> 删除事务日志 -> 删除 `backup\<session>`。公共 `SessionState` 值 `Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`ProcessExited`、`Restoring`、`CleaningRuntime` 和 `Completed` 从外部跟踪同一进程（参见[会话生命周期](session-lifecycle.md)）。

没有任何所有文件变更的会话仍会为其生命周期写入事务日志；对它的还原是经过校验的空操作。

<a id="journal-file"></a>
## 事务日志文件

路径：`<root>\.omsilaunch\journal.json`。每个安装实例最多只有一个事务日志；它的存在意味着“存在待处理的事务”。

`TransactionJournal` 字段：

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `SessionId` | GUID | 拥有该事务日志的会话；同时也是备份目录名（`N` 格式）。 |
| `State` | 整数 | 上文的 `TransactionState`。 |
| `Files` | `JournalFile` 数组 | 每个所有路径对应一个条目。 |
| `ProcessId` | 整数或 null | OMSI PID，自 `ProcessStarted` 起记录。 |
| `ProcessStartFileTimeUtc` | long 或 null | OMSI 创建时间（UTC tick），自 `ProcessStarted` 起记录。 |
| `ExecutablePath` | 字符串或 null | 所启动 `Omsi.exe` 的完整路径，自 `ProcessStarted` 起记录。 |

`JournalFile` 字段：

| 字段 | 类型 | 含义 |
| --- | --- | --- |
| `RelativePath` | 字符串 | 相对于安装根目录的路径（`options.cfg`、`GUI\NewSplashscreen_ENG.bmp`……）。 |
| `Existed` | bool | 会话之前该文件是否存在。 |
| `Sha256` | 十六进制字符串 | 原始字节的 SHA-256（`Existed` 为 false 时为空字节数组的 SHA-256）。 |
| `BackupPath` | 字符串 | 备份副本的绝对路径（仅在 `Existed` 时写入）。 |
| `AppliedSha256` | 十六进制字符串或 null | 会话写入该路径的覆盖层字节的 SHA-256；会话删除项为 null。这是原本不存在的文件的所有权指纹。 |
| `LastWriteTimeUtcTicks` | long 或 null | 原始最后写入时间。 |
| `CreationTimeUtcTicks` | long 或 null | 原始创建时间。 |
| `Attributes` | 整数或 null | 原始 `FileAttributes`（包括 `ReadOnly`）。 |
| `SessionDeletion` | bool | 对于会话要求保持不存在的路径（`.itx` 目标、`Texture\standard.ipr`、`closecheck`）为 true。 |

早期构建写入的、不含 `AppliedSha256` 和元数据字段的事务日志仍然可读；参见[原本不存在的文件](#originally-absent-files-and-ownership)。

<a id="backup-layout"></a>
## 备份布局

| 路径 | 内容 |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | 每个会话一个目录，随 `Prepared` 事务日志一同创建。 |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | 一个已存在的所有文件的精确原始字节。原本不存在的文件没有备份。 |

备份和事务日志通过临时文件（`<path>.omsilaunch.tmp`）写入，使用直写（write-through）并显式调用 `Flush(true)`，然后以覆盖方式执行原子 `File.Move`。临时文件始终会被删除，即使失败也是如此。覆盖层和还原的原始文件使用同一写入路径，因此已完成的操作不会留下任何 `*.omsilaunch.tmp` 文件。

只有在引用备份的事务日志被删除之后，备份才会被删除。删除 `backup\<session>` 失败只影响外观，从不会撤销已校验的还原。

<a id="restore"></a>
## 还原

`RestoreAsync` 在 `ProcessExited` 之后（或在恢复期间）运行。对于事务日志中的每个路径：

| 原始状态 | Action |
| --- | --- |
| 已存在 | 计算备份字节的哈希并与 `Sha256` 比较；不匹配时在写入任何内容之前以 `OL_E_RECOVERY_BACKUP_CORRUPT` 中止。然后以原子方式写入这些字节（当前文件为只读时先清除其只读属性），并还原创建时间、最后写入时间和属性（`RestoreMetadata`；元数据失败会被忽略，以免权限问题阻碍逐字节精确还原）。 |
| 原本不存在，现已存在，`AppliedSha256` 已知 | 计算当前字节的哈希。若等于 `AppliedSha256`，则该文件是会话自己的覆盖层，将被删除。否则以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` 中止还原，并保留事务日志。 |
| 原本不存在，现已存在，会话删除项，事务日志已到达 `ProcessStarted` | 该文件是会话的副产物（OMSI 在持有安装租约的情况下运行，且该路径被要求保持不存在）。它会被删除，并以诊断信息 `restore.session-artifact-removed` 报告，附带被删除内容的 SHA-256。 |
| 原本不存在，现已存在，会话删除项，进程从未启动 | 该文件来自会话之外。它会被保留，以 `OL_W_RESTORE_FOREIGN_FILE_RETAINED` 报告并附带其 SHA-256，事务仍会完成。 |
| 原本不存在，现已存在，无所有权证据（旧版无指纹事务日志） | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`；保留事务日志。 |
| 原本不存在，仍不存在 | 无需操作。 |

处理完所有文件后，`VerifyRestoredSnapshots` 会重新读取每个路径：已存在的原始文件的哈希必须等于 `Sha256`，原本不存在的路径必须不存在（除非被明确保留）。只有在此之后才会持久化 `Restored`、删除事务日志（若事务日志仍存在则为 `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）并删除备份目录。在 `Restored` 与删除事务日志之间发生崩溃，只会导致一次幂等的重放。

还原说明（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）会以 `LaunchDiagnostic` 条目的形式出现在 `SessionStatus.Diagnostics`（`Data` 中带 `sha256`）和 `RecoveryStatus.Diagnostics` 中，因此任何删除或保留都不会悄无声息地发生。

<a id="originally-absent-files-and-ownership"></a>
### 原本不存在的文件与所有权

写入原本不存在路径的覆盖层，**只有在其内容仍与会话所应用的内容一致时**（`AppliedSha256`）才会在还原时被删除。如果会话期间有其他内容替换了它，还原会以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` 失败，并保留事务日志以供检查。

对于会话删除路径（`.itx` 目标、`Texture\standard.ipr`、`closecheck`），若会话前不存在而会话后存在，则根据 OMSI 是否在此事务下运行过来判断：如果事务日志已到达 `ProcessStarted`，该文件是会话副产物并被删除（`restore.session-artifact-removed`）；如果进程从未启动，该文件会被保留并以 `OL_W_RESTORE_FOREIGN_FILE_RETAINED` 报告，事务日志仍会完成。

### `closecheck`

`closecheck` 是 OMSI 自身的崩溃标记（OMSI 未正常关闭时存在）。适用两条规则：

- 如果它在会话**之前**就存在，且 `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` 为 `true`（默认值），则在事务打开之前将其永久删除，并以诊断信息 `closecheck.stale-removed` 记录其 SHA-256（删除失败时为 `OL_E_CLOSECHECK_REMOVE_FAILED`）。这是一项有文档说明的永久性更改，不参与事务。该设置为 `false` 时，标记会保留，OMSI 会显示其警告。
- 如果它在会话之前**不**存在，则 `closecheck` 被添加为会话删除项。由于会话以 `TerminateProcess` 结束（OMSI 的关闭例程不会运行），OMSI 在启动时写入的标记之后总是仍然存在；它会作为会话产物在还原时被删除。

<a id="early-recovery-order"></a>
## 早期恢复顺序

每次调用 `StartSessionAsync` 时，在获取安装租约之后、读取当前安装之前：

1. 一个仅用于恢复的事务检查 `journal.json`。如果存在，立即运行 `RestorePendingAsync`，从而使新会话的覆盖层和启动画面语言都基于**原始**文件，而不是上一个会话的残留。
2. 如果该恢复以 `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` 失败（旧版无指纹事务日志），则恢复被**推迟**：新会话构建其覆盖层，新事务以自己计划写入的字节作为所有权证据重试恢复（内容等于新覆盖层的原本不存在的文件会被认定为 OmsiLaunch 所有）。任何其他恢复失败都会使启动失败。
3. 只有在此之后，才会校验插件闭包、计算 `Omsi.exe` 的哈希、处理 `closecheck`，并准备和应用新事务。

主机跟踪会记录 `PENDING_JOURNAL_RECOVERED` 或 `PENDING_JOURNAL_RECOVERY_DEFERRED`。

<a id="crash-recovery-and-owner-liveness"></a>
## 崩溃恢复与所有者存活检测

恢复从不在正在运行的 OMSI 之下替换文件。当事务日志中记录的所有者仍存活时，`RestorePendingAsync` 会以 `OL_E_INSTALLATION_BUSY` 拒绝：

| 事务日志内容 | 存活检测 |
| --- | --- |
| 已记录 `ProcessId` 和 `ProcessStartFileTimeUtc` | 具有该 PID 的进程必须正在运行，其启动时间必须匹配（排除 PID 复用），并且在记录了 `ExecutablePath` 时，其主模块必须是该路径（无关的存活进程无法保留该事务）。 |
| 无 PID，状态介于 `HandoffCreated`（含）与 `ProcessExited`（不含）之间 | 主机在 `CreateProcess` 与事务日志写入之间终止。任何主模块为 `<root>\Omsi.exe` 的 `Omsi.exe` 都被视为所有者。 |
| 无 PID，其他状态 | 未存活；继续恢复。 |

显式恢复通过 `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)` 提供，返回 `RecoveryStatus(Pending, Recovered, Diagnostics)`；它会先获取安装租约（其他所有者持有租约时为 `OL_E_INSTALLATION_BUSY`）。在 CLI 中，`/recovery-status` 只报告而不还原，`/recover` 执行还原；如果请求了还原而之后事务日志仍处于待处理状态，则返回退出码 8（`TransactionRecoveryFailed`）。参见 [CLI](../reference/cli.md) 和[公共 API](../reference/public-api.md)。

<a id="deferred-restore-at-session-end"></a>
### 会话结束时的推迟还原

如果监管器无法确认 OMSI 已退出（`OL_E_PROCESS_TERMINATE_FAILED`、`OL_E_PROCESS_WAIT_FAILED`，或报告为 `OL_E_PROCESS_CLEANUP_FAILED` 的清理故障），会话会以 `OL_E_RESTORE_DEFERRED` 失败，并有意保留事务日志：在 OMSI 可能仍在读取安装文件时替换它们是不安全的。下次启动（或 `/recover`）会在进程消失后执行还原。因其他任何原因失败的还原会以 `OL_E_RESTORE_FAILED` 结束会话；事务日志会一直保留，直到每个所有的原始文件都已还原并校验。

<a id="the-installation-lease"></a>
## 安装租约

租约是一个计数为 1 的命名信号量 `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>`。根目录由 `InstallationLease.NormalizeRoot` 规范化（完整路径，删除末尾分隔符，驱动器根目录除外），因此 `C:\OMSI`、`C:\OMSI\` 和 `c:\omsi\sub\..` 共享同一个租约；本地控制管道名称使用相同的规范化方式。租约由 `StartSessionAsync`（状态 `AcquiringInstallationLock`）和 `RecoverPendingAsync` 获取，并在会话的生命周期任务完成或恢复调用返回时释放。无法获取租约时会立即引发 `OL_E_INSTALLATION_BUSY`（不等待）。

已接受的限制（已记录在文档中，没有更改计划）：

- `Local\` 作用域：**每个登录会话**中每个安装实例只有一个所有者。同一台计算机上的两个交互式用户之间不会互斥。
- 只要任何其他进程仍持有信号量的句柄，崩溃就不会释放该信号量；与被放弃的互斥体不同，它没有所有者。遗留的持有者会使安装实例一直处于 `OL_E_INSTALLATION_BUSY`，直到该句柄关闭。
- 同一 Windows 用户的任何进程都可以抢先创建该名称并持有它。

<a id="omsilaunch-directory"></a>
## `.omsilaunch` 目录

| 条目 | 生命周期 | 所有者 |
| --- | --- | --- |
| `journal.json` | 临时；仅在存在待处理事务时存在 | 事务 |
| `backup\<sessionId>\*.bin` | 临时；在事务日志之后删除 | 事务 |
| `diagnostics\<sessionId>-host.log` | 永久；保留最新 50 个会话（新会话启动时删除较旧的会话前缀文件） | 主机跟踪 |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | 永久（相同的保留策略） | CLI |
| `diagnostics\tray-host.log` | 永久 | Windows 托盘主机 |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | 永久性产品资源；从包中复制一次，从不覆盖或删除 | 会话视觉资源 |
| `session-profiles\<id>\` | 永久；由用户或内容作者安装 | 用户 |
| `profiles\` | 当前代码不创建也不读取；保留 | 无 |
| `docs\`、`examples\` | 永久；随发行包提供 | 包 |

没有数据离开本机；诊断信息仅为本地文件。另请参见 [`.omsilaunch` 目录](../../../concepts/omsilaunch-directory.md)。

<a id="runtime-mutations-are-not-journaled"></a>
## 运行时变更不记入事务日志

运行时控制操作（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random`、D3D 纹理）只更改 OMSI 的内存状态。它们不会记录到事务日志中，也不会被还原；它们随进程一起消失。参见[运行时控制](../reference/runtime-control.md)。

<a id="failure-modes-and-error-codes"></a>
## 失败模式与错误码

| 错误码 | 含义 | 之后的事务日志 |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | 租约由其他所有者持有，或事务日志中记录的 OMSI 进程仍存活 | 保留 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | 对有快照但磁盘上没有事务日志的事务请求了还原 | 不适用 |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | 备份的哈希与快照指纹不同；未写入任何内容 | 保留 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | 原本不存在的覆盖层路径现在包含会话未写入的内容 | 保留 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | 旧版无指纹事务日志中有一个原本不存在、现已存在的路径；只有计划写入字节完全相同的新会话才能关闭它 | 保留（推迟） |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | 已校验的还原完成后无法删除 `journal.json` | 保留（重放是幂等的） |
| `OL_E_RESTORE_DEFERRED` | 未确认 OMSI 已退出；还原推迟到下次启动 | 保留 |
| `OL_E_RESTORE_FAILED` | 其他任何还原失败（还原后存在性或哈希不匹配、I/O 错误） | 保留 |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | 事务开始前无法删除遗留的 `closecheck` | 尚无 |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | 警告：保留了会话删除路径上的外来文件 | 已完成 |
| `OL_E_PLAN_NOT_RUNNABLE` | 启动时重新规划发现规格已不再可运行（例如 `Omsi.exe` 已更改）；不会打开事务 | 无 |

CLI 将 `OL_E_RECOVERY_*` 和 `OL_E_RESTORE_FAILED` 映射为退出码 8，将 `OL_E_INSTALLATION_BUSY` 映射为退出码 7；参见[退出码](../reference/exit-codes.md)。

<a id="evidence"></a>
## 证据

`tools/OmsiLaunch.TestHost` 中的离线测试覆盖了事务路径：`transaction.restore`、`transaction.options-overlay-restore`、`transaction.absent-overlay-restore`、`transaction.absent-file-ownership`、`transaction.absent-file-recovery`、`transaction.session-delete-restore`、`transaction.deletion-created-during-session`、`transaction.deletion-foreign-file-retained`、`transaction.deletion-recovery-after-crash`、`transaction.backup-corrupt-rejected`、`transaction.metadata-and-backup-cleanup`、`transaction.legacy-journal-ownership-migration`、`transaction.recovery-pre-pid-window`、`transaction.recovery-then-apply-ownership`、`transaction.restore-failure-recovery`、`transaction.failure-boundaries`、`transaction.empty-journal-restore`、`api.recover-requires-lease`、`lease.cross-thread-release`。

运行时证据（验证矩阵）：RV-005 和 RV-006（覆盖层已应用且逐字节精确还原，会话 `1e8e0548-...` 和展示批次），RV-008 提前退出通过（会话 `0dc40570-...`）。

运行时证据（运行时收尾轮次，2026-09-23，`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）：
- 使用 OMSI 真实下载器时对 `.itx` 目标的会话产物删除，分别在正常停止时以及所有者中断后加 `/recover` 时（S-01、`I01`、`I02`）；
- 在构建覆盖层之前的早期恢复（S-05、`S05`）；
- 元数据和只读文件还原以及备份清理（S-12、`S12a`、`S12b`）；
- 在租约下执行 `/recover`，包括存在孤立 OMSI 时和在 PID 记录前的时间窗口内（S-04、`S04`、`S04b`）；
- 启动失败，以及还原失败后执行 `/recover`（RV-008 其余部分、`SF01`、`SF02`、`F01`）；
- CP1252 保留（S-07、`C01`）。

推迟的旧版无指纹事务日志恢复分支仍然仅离线验证。参见[运行时验证状态](../status/runtime-validation-status.md)。
