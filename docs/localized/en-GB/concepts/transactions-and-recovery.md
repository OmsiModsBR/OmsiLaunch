# Transactions and Recovery

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> British English edition of the [canonical page](../../../concepts/transactions-and-recovery.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

Every OmsiLaunch session that touches an OMSI file does so inside a durable, journaled transaction: the original bytes are backed up before they are replaced, the journal records how far the session got, and restore verifies each backup before writing it back. This page describes that transaction as implemented by `FileConfigurationTransaction` (`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`) and driven by `OmsiLaunchService.StartAsync`, `SuperviseAsync` and `RecoverPendingAsync` (`src/OmsiLaunch.Core/OmsiLaunchService.cs`), together with the file inputs computed by `SessionVisualAssets` (`src/OmsiLaunch.Core/SessionVisualAssets.cs`). It is written for users who need to know what a session changes and what recovery does, and for integrators who need the exact guarantees.

## What a session changes

Only **temporary overlays** enter the transaction. They are computed before the transaction is opened and restored when it closes.

| Session input | File(s) | Kind |
| --- | --- | --- |
| `/set:<key>=<value>`, profile `settings`, `LaunchSpec.Environment.*` | `options.cfg` (semantic token patches; CP1252 bytes preserved, BOM-marked UTF-8/UTF-16 respected) | overlay |
| Managed splash (`SplashMode.Managed`, the default) | `GUI\NewSplashscreen_ENG.bmp` and `GUI\NewSplashscreen_<language>.bmp` | overlay (the localised file is transaction-created when the installation has none) |
| Internet textures `Override` | `Texture\standard.itx` | overlay |
| Internet textures `Override` | every target listed in the `.itx`, plus `Texture\standard.ipr` | session deletion |
| Always | `closecheck` (when absent before the session) | session deletion |

Permanent product files are **not** transaction participants: the plugin closure under `plugins\OmsiLaunch.*` (validated only, see [permanent plugin](permanent-plugin.md)), `.omsilaunch\assets\splash\*.bmp` (copied once, never removed), diagnostics under `.omsilaunch\diagnostics`, session-profile packages, and the release documentation and examples. Third-party plugins and every other OMSI file are never enumerated, copied, removed or restored.

OMSI itself keeps writing its own state while a session runs, exactly as in a normal OMSI start: `options.cfg` (for example `[last_map]` when the session loads a different map, rewritten at gameplay entry), `Texture\standard.ipr`, schedule and lightmap caches (`Texture\Temp_Schedules\*`, `maps\<map>\*.map.LM.bmp`), `maps\<map>\laststn.osn`, the driver profile under `Drivers\`, and its logs. A write to a path the session owns (above) is undone by the restore; every other OMSI write persists after the session, just as it would after running OMSI directly. Runtime closure evidence: a saved-situation session on another map left `[last_map]` changed because it did not overlay `options.cfg` (`CAM01`), while `/set` sessions restored `options.cfg` exactly (`S12a`, `S12b`, `C01`).

## Transaction states

`TransactionState` is persisted in the journal after every transition. The values are serialised as integers by `System.Text.Json`.

| Value | State | Written when |
| --- | --- | --- |
| 0 | `Prepared` | Snapshots of every overlay and deletion path have been taken and their backups flushed to disk. Nothing in the installation has changed yet. This is the recovery obligation: from here on, a crash leaves a recoverable journal. |
| 1 | `Applied` | Every overlay has been atomically written and every deletion removed. |
| 2 | `RuntimeDeployed` | Permanent plugin integrity has been validated for this start (nothing is deployed; the name is historical). |
| 3 | `HandoffCreated` | The startup handoff, telemetry slot and runtime mailbox exist as named shared memory. |
| 4 | `ProcessStarted` | `Omsi.exe` was created. The journal now also carries `ProcessId`, `ProcessStartFileTimeUtc` (creation time, UTC ticks) and `ExecutablePath`. |
| 5 | `ProcessExited` | The supervisor confirmed process exit (natural exit or `TerminateProcess`). |
| 6 | `Restoring` | Restore has begun. |
| 7 | `Restored` | Every owned file has been restored and verified. Immediately afterwards the journal is deleted and `backup\<session>` removed. |
| 8 | `Completed` | Declared in the enum but never persisted; a completed transaction has no journal. |

The lifecycle of a normal session is therefore: snapshot -> `Prepared` -> overlays written / deletions removed -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> journal deleted -> `backup\<session>` removed. The public `SessionState` values `Snapshotting`, `ApplyingConfiguration`, `DeployingRuntime`, `CreatingStartupHandoff`, `StartingProcess`, `ProcessExited`, `Restoring`, `CleaningRuntime` and `Completed` track the same progression from the outside (see [session lifecycle](session-lifecycle.md)).

A session with no owned file mutations still writes a journal for its lifecycle; restoring it is a verified no-op.

## Journal file

Path: `<root>\.omsilaunch\journal.json`. There is at most one journal per installation; its presence means "a transaction is pending".

`TransactionJournal` fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `SessionId` | GUID | Session that owns the journal; also the backup directory name (`N` format). |
| `State` | integer | `TransactionState` above. |
| `Files` | array of `JournalFile` | One entry per owned path. |
| `ProcessId` | integer or null | OMSI PID, from `ProcessStarted` on. |
| `ProcessStartFileTimeUtc` | long or null | OMSI creation time (UTC ticks), from `ProcessStarted` on. |
| `ExecutablePath` | string or null | Full path of the launched `Omsi.exe`, from `ProcessStarted` on. |

`JournalFile` fields:

| Field | Type | Meaning |
| --- | --- | --- |
| `RelativePath` | string | Path relative to the installation root (`options.cfg`, `GUI\NewSplashscreen_ENG.bmp`, ...). |
| `Existed` | bool | Whether the file existed before the session. |
| `Sha256` | hex string | SHA-256 of the original bytes (of an empty byte array when `Existed` is false). |
| `BackupPath` | string | Absolute path of the backup copy (only written when `Existed`). |
| `AppliedSha256` | hex string or null | SHA-256 of the overlay bytes the session wrote to this path; null for session deletions. This is the ownership fingerprint for originally-absent files. |
| `LastWriteTimeUtcTicks` | long or null | Original last-write time. |
| `CreationTimeUtcTicks` | long or null | Original creation time. |
| `Attributes` | integer or null | Original `FileAttributes` (including `ReadOnly`). |
| `SessionDeletion` | bool | True for paths the session asked to keep absent (`.itx` targets, `Texture\standard.ipr`, `closecheck`). |

Journals written by earlier builds without `AppliedSha256` and the metadata fields are still readable; see [Originally-absent files](#originally-absent-files-and-ownership).

## Backup layout

| Path | Content |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | One directory per session, created with the `Prepared` journal. |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | Exact original bytes of one existing owned file. Originally-absent files have no backup. |

Backups and the journal are written with a temporary file (`<path>.omsilaunch.tmp`), write-through plus an explicit `Flush(true)`, then an atomic `File.Move` with overwrite. The temporary file is always deleted, even on failure. The same write path is used for overlays and for restored originals, so no `*.omsilaunch.tmp` file survives a completed operation.

Backups are only removed after the journal that referenced them has been deleted. A failure to delete `backup\<session>` is cosmetic and never undoes a verified restore.

## Restore

`RestoreAsync` runs after `ProcessExited` (or during recovery). For every journaled path:

| Original state | Action |
| --- | --- |
| Existed | The backup bytes are hashed and compared with `Sha256`; a mismatch aborts with `OL_E_RECOVERY_BACKUP_CORRUPT` before anything is written. The bytes are then written atomically (a read-only current file has its attribute cleared first), and creation time, last-write time and attributes are restored (`RestoreMetadata`; metadata failures are ignored so a permission problem cannot block a byte-exact restore). |
| Absent, now present, `AppliedSha256` known | The current bytes are hashed. If they equal `AppliedSha256` the file is the session's own overlay and is deleted. Otherwise `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` aborts the restore and the journal is kept. |
| Absent, now present, session deletion, journal reached `ProcessStarted` | The file is a session by-product (OMSI ran with the installation lease held and this path was asked to stay absent). It is deleted and reported as diagnostic `restore.session-artifact-removed` with the SHA-256 of the removed content. |
| Absent, now present, session deletion, process never started | The file came from outside the session. It is retained, reported as `OL_W_RESTORE_FOREIGN_FILE_RETAINED` with its SHA-256, and the transaction still completes. |
| Absent, now present, no ownership evidence (pre-fingerprint journal) | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`; the journal is kept. |
| Absent, still absent | Nothing to do. |

After all files are processed, `VerifyRestoredSnapshots` re-reads every path: existing originals must hash to `Sha256`, originally-absent paths must be absent unless they were explicitly retained. Only then is `Restored` persisted, the journal deleted (`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` if it survives) and the backup directory removed. A crash between `Restored` and the journal deletion only causes an idempotent replay.

Restore notes (`restore.session-artifact-removed`, `OL_W_RESTORE_FOREIGN_FILE_RETAINED`) surface as `LaunchDiagnostic` entries in `SessionStatus.Diagnostics` (with `sha256` in `Data`) and in `RecoveryStatus.Diagnostics`, so nothing is removed or retained silently.

### Originally-absent files and ownership

An overlay written to a path that did not exist is removed at restore **only if its content still matches what the session applied** (`AppliedSha256`). If something else replaced it during the session, restore fails with `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` and the journal is kept for inspection.

A session-deletion path (`.itx` targets, `Texture\standard.ipr`, `closecheck`) that did not exist before but exists after is judged by whether OMSI ran under this transaction: if the journal reached `ProcessStarted`, the file is a session by-product and is removed (`restore.session-artifact-removed`); if the process never started, the file is retained and reported as `OL_W_RESTORE_FOREIGN_FILE_RETAINED`, and the journal still completes.

### `closecheck`

`closecheck` is OMSI's own crash marker (present when OMSI did not shut down cleanly). Two rules apply:

- If it exists **before** the session and `LaunchBehaviorSpec.SuppressStaleClosecheckWarning` is `true` (the default), it is permanently removed before the transaction opens, recorded as diagnostic `closecheck.stale-removed` with its SHA-256 (`OL_E_CLOSECHECK_REMOVE_FAILED` if the delete fails). This is a documented permanent change, not a transaction participant. With the flag `false` the marker stays and OMSI shows its warning.
- If it does **not** exist before the session, `closecheck` is added as a session deletion. Because the session ends with `TerminateProcess` (OMSI's shutdown routine does not run), OMSI's start-time marker is always still there afterwards; it is removed at restore as a session artefact.

## Early recovery order

On every `StartSessionAsync`, after the installation lease is taken and before anything reads the live installation:

1. A recovery-only transaction checks for `journal.json`. If present, `RestorePendingAsync` runs immediately, so overlays and the splash language for the new session derive from the **original** files, never from a previous session's leftovers.
2. If that recovery fails with `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` (a pre-fingerprint journal), recovery is **deferred**: the new session builds its overlays, and the new transaction retries recovery with its own planned bytes as ownership evidence (an originally-absent file whose content equals the new overlay is accepted as OmsiLaunch-owned). Any other recovery failure fails the start.
3. Only then are the plugin closure validated, `Omsi.exe` hashed, `closecheck` handled, and the new transaction prepared and applied.

The host trace records `PENDING_JOURNAL_RECOVERED` or `PENDING_JOURNAL_RECOVERY_DEFERRED`.

## Crash recovery and owner liveness

Recovery never replaces files underneath a running OMSI. `RestorePendingAsync` refuses with `OL_E_INSTALLATION_BUSY` while the journaled owner is alive:

| Journal content | Liveness test |
| --- | --- |
| `ProcessId` and `ProcessStartFileTimeUtc` recorded | The process with that PID must be running, its start time must match (rejects PID reuse), and, when `ExecutablePath` is recorded, its main module must be that path (a live unrelated process cannot retain the transaction). |
| No PID, state between `HandoffCreated` (inclusive) and `ProcessExited` (exclusive) | The host died between `CreateProcess` and the journal write. Any `Omsi.exe` whose main module is `<root>\Omsi.exe` is treated as the owner. |
| No PID, other states | Not alive; recovery proceeds. |

Explicit recovery is exposed as `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)` returning `RecoveryStatus(Pending, Recovered, Diagnostics)`; it takes the installation lease first (`OL_E_INSTALLATION_BUSY` when another owner holds it). On the CLI, `/recovery-status` reports without restoring and `/recover` restores; exit code 8 (`TransactionRecoveryFailed`) is returned when a restore was requested and the journal is still pending afterwards. See [CLI](../reference/cli.md) and [public API](../reference/public-api.md).

### Deferred restore at session end

If the supervisor cannot confirm that OMSI exited (`OL_E_PROCESS_TERMINATE_FAILED`, `OL_E_PROCESS_WAIT_FAILED`, or a cleanup fault reported as `OL_E_PROCESS_CLEANUP_FAILED`), the session fails with `OL_E_RESTORE_DEFERRED` and the journal is deliberately retained: replacing installation files while OMSI may still read them is unsafe. The next start (or `/recover`) restores once the process is gone. A restore that fails for any other reason ends the session with `OL_E_RESTORE_FAILED`; the journal remains until every owned original is restored and verified.

## The installation lease

The lease is a named semaphore `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` with count 1. The root is normalised by `InstallationLease.NormalizeRoot` (full path, trailing separators removed except for a drive root), so `C:\OMSI`, `C:\OMSI\` and `c:\omsi\sub\..` share one lease; the local control pipe name uses the same normalisation. It is taken by `StartSessionAsync` (state `AcquiringInstallationLock`) and by `RecoverPendingAsync`, and released when the session's lifecycle task completes or the recovery call returns. `OL_E_INSTALLATION_BUSY` is raised immediately when it cannot be taken (no waiting).

Accepted limitations (documented, not scheduled for change):

- `Local\` scope: one owner per installation **per logon session**. Two interactive users on the same machine are not mutually excluded.
- A semaphore is not released by a crash while any other process still holds a handle to it; unlike an abandoned mutex it has no owner. A stale holder leaves the installation `OL_E_INSTALLATION_BUSY` until that handle closes.
- Any process of the same Windows user can create the name first and hold it.

## `.omsilaunch` directory

| Entry | Lifetime | Owner |
| --- | --- | --- |
| `journal.json` | Temporary; exists only while a transaction is pending | Transaction |
| `backup\<sessionId>\*.bin` | Temporary; removed after the journal | Transaction |
| `diagnostics\<sessionId>-host.log` | Permanent; retention keeps the 50 newest sessions (older session-prefixed files are deleted when a new session starts) | Host trace |
| `diagnostics\<sessionId>-runtime-operation.json`, `-runtime-read-batch.json`, `-runtime-write-batch.json`, `-d3d-wave-d-batch.json` | Permanent (same retention) | CLI |
| `diagnostics\tray-host.log` | Permanent | Windows tray host |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | Permanent product assets; copied once from the package, never overwritten or removed | Session visual assets |
| `session-profiles\<id>\` | Permanent; installed by the user or content author | User |
| `profiles\` | Not created or read by the current code; reserved | none |
| `docs\`, `examples\` | Permanent; shipped by the release package | Package |

No data leaves the machine; diagnostics are local files only. See also [`.omsilaunch` directory](../../../concepts/omsilaunch-directory.md).

## Runtime mutations are not journaled

Runtime control operations (`time.set`, `camera.set`, `camera.lock`, `vehicle.variable.set`, `road-vehicles.spawn`, `road-vehicles.place-random`, D3D textures) change OMSI's in-memory state only. They are not recorded in the journal and are not restored; they vanish with the process. See [runtime control](../reference/runtime-control.md).

## Failure modes and error codes

| Code | Meaning | Journal afterwards |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | Lease held by another owner, or the journaled OMSI process is still alive | kept |
| `OL_E_RECOVERY_JOURNAL_MISSING` | Restore requested for a transaction with snapshots but no journal on disk | n/a |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | A backup's hash differs from the snapshot fingerprint; nothing was written | kept |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | An originally-absent overlay path now holds content the session did not write | kept |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | Pre-fingerprint journal with an originally-absent path that now exists; only a new session with identical planned bytes can close it | kept (deferred) |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `journal.json` could not be deleted after a verified restore | kept (replay is idempotent) |
| `OL_E_RESTORE_DEFERRED` | OMSI exit not confirmed; restore postponed to the next start | kept |
| `OL_E_RESTORE_FAILED` | Any other restore failure (presence or hash mismatch after restore, I/O error) | kept |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | Stale `closecheck` could not be deleted before the transaction | none yet |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | Warning: a foreign file on a session-deletion path was kept | completed |
| `OL_E_PLAN_NOT_RUNNABLE` | Re-planning at start found the spec no longer runnable (for example a changed `Omsi.exe`); no transaction is opened | none |

The CLI maps `OL_E_RECOVERY_*` and `OL_E_RESTORE_FAILED` to exit code 8 and `OL_E_INSTALLATION_BUSY` to exit code 7; see [exit codes](../reference/exit-codes.md).

## Evidence

Offline tests in `tools/OmsiLaunch.TestHost` cover the transaction paths: `transaction.restore`, `transaction.options-overlay-restore`, `transaction.absent-overlay-restore`, `transaction.absent-file-ownership`, `transaction.absent-file-recovery`, `transaction.session-delete-restore`, `transaction.deletion-created-during-session`, `transaction.deletion-foreign-file-retained`, `transaction.deletion-recovery-after-crash`, `transaction.backup-corrupt-rejected`, `transaction.metadata-and-backup-cleanup`, `transaction.legacy-journal-ownership-migration`, `transaction.recovery-pre-pid-window`, `transaction.recovery-then-apply-ownership`, `transaction.restore-failure-recovery`, `transaction.failure-boundaries`, `transaction.empty-journal-restore`, `api.recover-requires-lease`, `lease.cross-thread-release`.

Runtime evidence (validation matrix): RV-005 and RV-006 (overlay applied and byte-exact restore, sessions `1e8e0548-...` and the presentation batch), RV-008 early-exit pass (session `0dc40570-...`).

Runtime evidence (runtime closure round, 2026-09-23, `research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`):
- session-artefact removal of `.itx` targets with OMSI's real downloader, on normal stop and after owner interruption plus `/recover` (S-01, `I01`, `I02`);
- early recovery before overlay build (S-05, `S05`);
- metadata and read-only restore plus backup cleanup (S-12, `S12a`, `S12b`);
- `/recover` under the lease, with an orphaned OMSI and in the pre-PID window (S-04, `S04`, `S04b`);
- startup failures and a failed restore followed by `/recover` (RV-008 remainder, `SF01`, `SF02`, `F01`);
- CP1252 preservation (S-07, `C01`).

The deferred pre-fingerprint recovery branch remains offline-only. See [runtime validation status](../status/runtime-validation-status.md).
