# トランザクションとリカバリ

<!-- l10n: source=concepts/transactions-and-recovery.md source-sha256=cc2e7f493ee6d112a3d5326be020b81beb67a1002aa25b6042e982b0b3d9bb9c -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../concepts/transactions-and-recovery.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OMSI のファイルに手を加える OmsiLaunch のセッションは、すべて永続的でジャーナル化されたトランザクションの内部でその変更を行います。元のバイト列は置き換えられる前にバックアップされ、ジャーナルはセッションがどこまで進んだかを記録し、復元時には各バックアップを検証してから書き戻します。このページでは、`FileConfigurationTransaction`（`src/OmsiLaunch.Configuration/ConfigurationTransaction.cs`）として実装され、`OmsiLaunchService.StartAsync`、`SuperviseAsync`、`RecoverPendingAsync`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）によって駆動されるトランザクションを、`SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）が算出するファイル入力とあわせて説明します。セッションが何を変更し、リカバリが何を行うかを知る必要があるユーザーと、正確な保証内容を知る必要があるインテグレーターを対象としています。

<a id="what-a-session-changes"></a>
## セッションが変更するもの

トランザクションに含まれるのは**一時的なオーバーレイ**だけです。オーバーレイはトランザクションを開く前に算出され、トランザクションを閉じるときに復元されます。

| セッション入力 | ファイル | 種類 |
| --- | --- | --- |
| `/set:<key>=<value>`、プロファイルの `settings`、`LaunchSpec.Environment.*` | `options.cfg`（意味単位でのトークンパッチ。CP1252 のバイト列は保持され、BOM 付きの UTF-8/UTF-16 も尊重されます） | オーバーレイ |
| 管理対象のスプラッシュ（`SplashMode.Managed`、既定値） | `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_<language>.bmp` | オーバーレイ（インストール環境にローカライズ版のファイルがない場合は、トランザクションが作成します） |
| Internet Textures の `Override` | `Texture\standard.itx` | オーバーレイ |
| Internet Textures の `Override` | `.itx` に列挙されたすべてのターゲット、および `Texture\standard.ipr` | セッション削除 |
| 常に | `closecheck`（セッション前に存在しない場合） | セッション削除 |

製品の常設ファイルはトランザクションの対象では**ありません**。対象外となるのは、`plugins\OmsiLaunch.*` 以下のプラグインのクロージャー（検証のみ。[常駐プラグイン](permanent-plugin.md)を参照）、`.omsilaunch\assets\splash\*.bmp`（一度だけコピーされ、削除されることはありません）、`.omsilaunch\diagnostics` 以下の診断情報、セッションプロファイルのパッケージ、およびリリースに含まれるドキュメントとサンプルです。サードパーティ製プラグインやその他の OMSI ファイルが列挙、コピー、削除、復元されることは一切ありません。

OMSI 自身は、通常の OMSI の起動とまったく同じように、セッションの実行中も自らの状態を書き込み続けます。具体的には `options.cfg`（たとえばセッションが別のマップを読み込んだ場合の `[last_map]`。ゲームプレイ開始時に書き換えられます）、`Texture\standard.ipr`、時刻表とライトマップのキャッシュ（`Texture\Temp_Schedules\*`、`maps\<map>\*.map.LM.bmp`）、`maps\<map>\laststn.osn`、`Drivers\` 以下の運転士プロファイル、およびログです。セッションが所有するパス（上記）への書き込みは復元によって元に戻されますが、それ以外の OMSI による書き込みは、OMSI を直接実行した場合と同様にセッション後も残ります。ランタイムクロージャーのエビデンス: 別のマップ上の保存済みシチュエーションセッションでは、`options.cfg` をオーバーレイしていなかったため `[last_map]` が変更されたまま残りました（`CAM01`）。一方、`/set` を使ったセッションでは `options.cfg` が正確に復元されました（`S12a`、`S12b`、`C01`）。

<a id="transaction-states"></a>
## トランザクションの状態

`TransactionState` は遷移のたびにジャーナルに永続化されます。値は `System.Text.Json` によって整数としてシリアル化されます。

| 値 | 状態 | 書き込まれるタイミング |
| --- | --- | --- |
| 0 | `Prepared` | すべてのオーバーレイと削除対象パスのスナップショットが取得され、そのバックアップがディスクにフラッシュされた時点です。インストール環境にはまだ何の変更も加えられていません。これがリカバリ義務の起点であり、ここから先はクラッシュしてもリカバリ可能なジャーナルが残ります。 |
| 1 | `Applied` | すべてのオーバーレイがアトミックに書き込まれ、すべての削除対象が削除された時点です。 |
| 2 | `RuntimeDeployed` | 今回の開始に対して常駐プラグインの整合性が検証された時点です（何も配置されません。この名前は歴史的な経緯によるものです）。 |
| 3 | `HandoffCreated` | 起動ハンドオフ、テレメトリスロット、ランタイムメールボックスが名前付き共有メモリとして作成された時点です。 |
| 4 | `ProcessStarted` | `Omsi.exe` が作成された時点です。この時点からジャーナルには `ProcessId`、`ProcessStartFileTimeUtc`（作成時刻、UTC ティック）、`ExecutablePath` も含まれます。 |
| 5 | `ProcessExited` | スーパーバイザーがプロセスの終了（自然終了または `TerminateProcess`）を確認した時点です。 |
| 6 | `Restoring` | 復元が開始された時点です。 |
| 7 | `Restored` | 所有するすべてのファイルが復元され、検証された時点です。その直後にジャーナルが削除され、`backup\<session>` も削除されます。 |
| 8 | `Completed` | 列挙型で宣言されていますが、永続化されることはありません。完了したトランザクションにはジャーナルが存在しません。 |

したがって、通常のセッションのライフサイクルは次のとおりです: スナップショット -> `Prepared` -> オーバーレイ書き込み / 削除対象の削除 -> `Applied` -> `RuntimeDeployed` -> `HandoffCreated` -> `ProcessStarted` -> `ProcessExited` -> `Restoring` -> `Restored` -> ジャーナル削除 -> `backup\<session>` の削除。公開されている `SessionState` の値 `Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`ProcessExited`、`Restoring`、`CleaningRuntime`、`Completed` は、同じ進行を外部から追跡するものです（[セッションのライフサイクル](session-lifecycle.md)を参照）。

所有するファイルを変更しないセッションでも、ライフサイクルのためにジャーナルは書き込まれます。その復元は検証付きの何もしない処理（no-op）になります。

<a id="journal-file"></a>
## ジャーナルファイル

パス: `<root>\.omsilaunch\journal.json`。ジャーナルはインストール環境ごとに最大 1 つであり、その存在は「トランザクションが保留中である」ことを意味します。

`TransactionJournal` のフィールド:

| フィールド | 型 | 意味 |
| --- | --- | --- |
| `SessionId` | GUID | ジャーナルを所有するセッションです。バックアップディレクトリ名（`N` 形式）にもなります。 |
| `State` | 整数 | 上記の `TransactionState` です。 |
| `Files` | `JournalFile` の配列 | 所有するパスごとに 1 エントリです。 |
| `ProcessId` | 整数または null | OMSI の PID です。`ProcessStarted` 以降に記録されます。 |
| `ProcessStartFileTimeUtc` | long または null | OMSI の作成時刻（UTC ティック）です。`ProcessStarted` 以降に記録されます。 |
| `ExecutablePath` | 文字列または null | 起動された `Omsi.exe` のフルパスです。`ProcessStarted` 以降に記録されます。 |

`JournalFile` のフィールド:

| フィールド | 型 | 意味 |
| --- | --- | --- |
| `RelativePath` | 文字列 | インストールルートからの相対パスです（`options.cfg`、`GUI\NewSplashscreen_ENG.bmp` など）。 |
| `Existed` | bool | セッション前にファイルが存在していたかどうかです。 |
| `Sha256` | 16 進文字列 | 元のバイト列の SHA-256 です（`Existed` が false の場合は空のバイト配列の SHA-256）。 |
| `BackupPath` | 文字列 | バックアップコピーの絶対パスです（`Existed` の場合にのみ書き込まれます）。 |
| `AppliedSha256` | 16 進文字列または null | セッションがこのパスに書き込んだオーバーレイのバイト列の SHA-256 です。セッション削除の場合は null です。これは元々存在しなかったファイルに対する所有権のフィンガープリントです。 |
| `LastWriteTimeUtcTicks` | long または null | 元の最終書き込み時刻です。 |
| `CreationTimeUtcTicks` | long または null | 元の作成時刻です。 |
| `Attributes` | 整数または null | 元の `FileAttributes`（`ReadOnly` を含む）です。 |
| `SessionDeletion` | bool | セッションが存在しない状態を維持するよう求めたパス（`.itx` のターゲット、`Texture\standard.ipr`、`closecheck`）の場合に true です。 |

`AppliedSha256` やメタデータのフィールドを持たない以前のビルドで書き込まれたジャーナルも引き続き読み取れます。[元々存在しなかったファイル](#originally-absent-files-and-ownership)を参照してください。

<a id="backup-layout"></a>
## バックアップの構成

| パス | 内容 |
| --- | --- |
| `<root>\.omsilaunch\backup\<sessionId N-format>\` | セッションごとに 1 つのディレクトリで、`Prepared` のジャーナルとともに作成されます。 |
| `<backup dir>\<SHA-256 of the UTF-8 relative path, hex>.bin` | 存在していた所有ファイル 1 つ分の元のバイト列そのものです。元々存在しなかったファイルにはバックアップがありません。 |

バックアップとジャーナルは、一時ファイル（`<path>.omsilaunch.tmp`）を使い、ライトスルーと明示的な `Flush(true)` の後、上書き指定のアトミックな `File.Move` によって書き込まれます。一時ファイルは失敗時も含めて必ず削除されます。オーバーレイと復元される元のファイルにも同じ書き込み経路が使われるため、完了した操作の後に `*.omsilaunch.tmp` ファイルが残ることはありません。

バックアップは、それを参照していたジャーナルが削除された後にのみ削除されます。`backup\<session>` の削除に失敗しても見た目上の問題にすぎず、検証済みの復元が取り消されることはありません。

<a id="restore"></a>
## 復元

`RestoreAsync` は `ProcessExited` の後（またはリカバリ中）に実行されます。ジャーナルに記録された各パスについて、次のように処理します。

| 元の状態 | Action |
| --- | --- |
| 存在していた | バックアップのバイト列のハッシュを計算して `Sha256` と比較します。一致しない場合は、何も書き込む前に `OL_E_RECOVERY_BACKUP_CORRUPT` で中止します。その後、バイト列をアトミックに書き込み（現在のファイルが読み取り専用の場合は、先にその属性を解除します）、作成時刻、最終書き込み時刻、属性を復元します（`RestoreMetadata`。権限の問題によってバイト単位で正確な復元が妨げられないよう、メタデータの失敗は無視されます）。 |
| 存在しなかった、現在は存在する、`AppliedSha256` が既知 | 現在のバイト列のハッシュを計算します。`AppliedSha256` と一致すれば、そのファイルはセッション自身のオーバーレイであるため削除します。一致しなければ `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` で復元を中止し、ジャーナルを保持します。 |
| 存在しなかった、現在は存在する、セッション削除、ジャーナルが `ProcessStarted` に到達している | そのファイルはセッションの副産物です（OMSI はインストールリースを保持した状態で実行され、このパスは存在しない状態を維持するよう求められていました）。ファイルを削除し、削除した内容の SHA-256 とともに診断 `restore.session-artifact-removed` として報告します。 |
| 存在しなかった、現在は存在する、セッション削除、プロセスが一度も開始されていない | そのファイルはセッション外から来たものです。ファイルは保持され、その SHA-256 とともに `OL_W_RESTORE_FOREIGN_FILE_RETAINED` として報告され、トランザクションはそのまま完了します。 |
| 存在しなかった、現在は存在する、所有権の根拠がない（フィンガープリント導入前のジャーナル） | `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`。ジャーナルは保持されます。 |
| 存在しなかった、現在も存在しない | 何もしません。 |

すべてのファイルを処理した後、`VerifyRestoredSnapshots` がすべてのパスを再度読み取ります。存在していた元のファイルは `Sha256` とハッシュが一致しなければならず、元々存在しなかったパスは、明示的に保持された場合を除いて存在してはなりません。これを満たした場合にのみ `Restored` が永続化され、ジャーナルが削除され（削除できずに残った場合は `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）、バックアップディレクトリが削除されます。`Restored` とジャーナル削除の間でクラッシュしても、べき等な再実行が発生するだけです。

復元に関する注記（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）は、`SessionStatus.Diagnostics` 内（`Data` に `sha256` を含む）と `RecoveryStatus.Diagnostics` 内に `LaunchDiagnostic` エントリとして表示されるため、何かが黙って削除されたり保持されたりすることはありません。

<a id="originally-absent-files-and-ownership"></a>
### 元々存在しなかったファイルと所有権

存在しなかったパスに書き込まれたオーバーレイは、**その内容がセッションの適用した内容（`AppliedSha256`）とまだ一致している場合に限り**、復元時に削除されます。セッション中に別の何かがそれを置き換えた場合、復元は `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` で失敗し、調査のためにジャーナルが保持されます。

以前は存在しなかったが事後に存在するセッション削除パス（`.itx` のターゲット、`Texture\standard.ipr`、`closecheck`）は、このトランザクションの下で OMSI が実行されたかどうかによって判断されます。ジャーナルが `ProcessStarted` に到達していれば、そのファイルはセッションの副産物として削除されます（`restore.session-artifact-removed`）。プロセスが一度も開始されていなければ、ファイルは保持されて `OL_W_RESTORE_FOREIGN_FILE_RETAINED` として報告され、ジャーナルはそのまま完了します。

### `closecheck`

`closecheck` は OMSI 自身のクラッシュマーカーです（OMSI が正常にシャットダウンしなかった場合に存在します）。次の 2 つの規則が適用されます。

- セッションの**前に**存在し、`LaunchBehaviorSpec.SuppressStaleClosecheckWarning` が `true`（既定値）の場合、トランザクションを開く前に完全に削除され、その SHA-256 とともに診断 `closecheck.stale-removed` として記録されます（削除に失敗した場合は `OL_E_CLOSECHECK_REMOVE_FAILED`）。これはドキュメント化された恒久的な変更であり、トランザクションの対象ではありません。フラグが `false` の場合、マーカーは残り、OMSI が警告を表示します。
- セッションの前に存在**しない**場合、`closecheck` はセッション削除として追加されます。セッションは `TerminateProcess` で終了する（OMSI のシャットダウン処理は実行されない）ため、OMSI が起動時に作成するマーカーは終了後も必ず残っています。これは復元時にセッションの生成物として削除されます。

<a id="early-recovery-order"></a>
## 早期リカバリの順序

`StartSessionAsync` のたびに、インストールリースを取得した後、ライブのインストール環境を何かが読み取る前に、次の処理が行われます。

1. リカバリ専用のトランザクションが `journal.json` の有無を確認します。存在する場合は直ちに `RestorePendingAsync` が実行されるため、新しいセッションのオーバーレイとスプラッシュの言語は、以前のセッションの残骸ではなく、必ず**元の**ファイルから導出されます。
2. そのリカバリが `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`（フィンガープリント導入前のジャーナル）で失敗した場合、リカバリは**後回し**にされます。新しいセッションがオーバーレイを構築し、新しいトランザクションが自身の予定バイト列を所有権の根拠としてリカバリを再試行します（内容が新しいオーバーレイと等しい、元々存在しなかったファイルは OmsiLaunch の所有物として受け入れられます）。それ以外のリカバリ失敗では、開始は失敗します。
3. その後に初めて、プラグインのクロージャーが検証され、`Omsi.exe` のハッシュが計算され、`closecheck` が処理され、新しいトランザクションが準備・適用されます。

ホストトレースには `PENDING_JOURNAL_RECOVERED` または `PENDING_JOURNAL_RECOVERY_DEFERRED` が記録されます。

<a id="crash-recovery-and-owner-liveness"></a>
## クラッシュからのリカバリとオーナーの生存確認

リカバリが実行中の OMSI の下でファイルを置き換えることは決してありません。ジャーナルに記録されたオーナーが生存している間、`RestorePendingAsync` は `OL_E_INSTALLATION_BUSY` で拒否します。

| ジャーナルの内容 | 生存確認の方法 |
| --- | --- |
| `ProcessId` と `ProcessStartFileTimeUtc` が記録されている | その PID のプロセスが実行中であり、開始時刻が一致し（PID の再利用を排除します）、`ExecutablePath` が記録されている場合はメインモジュールがそのパスである必要があります（無関係な生存プロセスがトランザクションを保持し続けることはできません）。 |
| PID なし、状態が `HandoffCreated`（含む）から `ProcessExited`（含まない）の間 | ホストが `CreateProcess` とジャーナル書き込みの間で停止しています。メインモジュールが `<root>\Omsi.exe` である `Omsi.exe` はすべてオーナーとして扱われます。 |
| PID なし、その他の状態 | 生存していないと判断し、リカバリを続行します。 |

明示的なリカバリは `IOmsiLaunch.RecoverPendingAsync(InstallationSpec, bool restore)` として公開されており、`RecoveryStatus(Pending, Recovered, Diagnostics)` を返します。最初にインストールリースを取得します（別のオーナーが保持している場合は `OL_E_INSTALLATION_BUSY`）。CLI では、`/recovery-status` は復元せずに状態を報告し、`/recover` は復元を行います。復元が要求されたにもかかわらず、その後もジャーナルが保留中のままである場合は終了コード 8（`TransactionRecoveryFailed`）が返されます。[CLI](../reference/cli.md) と [公開 API](../reference/public-api.md) を参照してください。

<a id="deferred-restore-at-session-end"></a>
### セッション終了時の復元の延期

OMSI が終了したことをスーパーバイザーが確認できない場合（`OL_E_PROCESS_TERMINATE_FAILED`、`OL_E_PROCESS_WAIT_FAILED`、または `OL_E_PROCESS_CLEANUP_FAILED` として報告されるクリーンアップの障害）、セッションは `OL_E_RESTORE_DEFERRED` で失敗し、ジャーナルは意図的に保持されます。OMSI がまだファイルを読み取っている可能性がある間にインストール環境のファイルを置き換えるのは安全ではないためです。次回の開始時（または `/recover`）に、プロセスがなくなった時点で復元されます。それ以外の理由で復元が失敗した場合、セッションは `OL_E_RESTORE_FAILED` で終了し、所有するすべての元のファイルが復元・検証されるまでジャーナルは残ります。

<a id="the-installation-lease"></a>
## インストールリース

リースは、カウント 1 の名前付きセマフォ `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased, normalized installation root>` です。ルートは `InstallationLease.NormalizeRoot` によって正規化される（フルパス化し、ドライブルートを除いて末尾の区切り文字を除去する）ため、`C:\OMSI`、`C:\OMSI\`、`c:\omsi\sub\..` は 1 つのリースを共有します。ローカル制御のパイプ名にも同じ正規化が使われます。リースは `StartSessionAsync`（状態 `AcquiringInstallationLock`）と `RecoverPendingAsync` によって取得され、セッションのライフサイクルタスクが完了したとき、またはリカバリ呼び出しが戻ったときに解放されます。取得できない場合は、待機せずに直ちに `OL_E_INSTALLATION_BUSY` が発生します。

受容済みの制限事項（ドキュメント化済みで、変更の予定はありません）:

- `Local\` スコープ: オーナーはインストール環境ごと**ログオンセッションごと**に 1 つです。同じマシン上の 2 人の対話ユーザーは相互に排他されません。
- セマフォは、他のプロセスがまだそのハンドルを保持している間はクラッシュによって解放されません。放棄されたミューテックスとは異なり、セマフォにはオーナーがありません。古い保持者が残っていると、そのハンドルが閉じられるまでインストール環境は `OL_E_INSTALLATION_BUSY` のままになります。
- 同じ Windows ユーザーのプロセスであれば、どのプロセスでも先にその名前を作成して保持できます。

<a id="omsilaunch-directory"></a>
## `.omsilaunch` ディレクトリ

| エントリ | 存続期間 | 所有者 |
| --- | --- | --- |
| `journal.json` | 一時的。トランザクションが保留中の間のみ存在 | トランザクション |
| `backup\<sessionId>\*.bin` | 一時的。ジャーナルの後に削除 | トランザクション |
| `diagnostics\<sessionId>-host.log` | 永続的。最新 50 セッション分を保持（新しいセッションの開始時に、セッション ID を接頭辞とする古いファイルが削除される） | ホストトレース |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | 永続的（同じ保持ルール） | CLI |
| `diagnostics\tray-host.log` | 永続的 | Windows トレイホスト |
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | 永続的な製品アセット。パッケージから一度だけコピーされ、上書きも削除もされない | セッションのビジュアルアセット |
| `session-profiles\<id>\` | 永続的。ユーザーまたはコンテンツ作成者がインストール | ユーザー |
| `profiles\` | 現在のコードでは作成も読み取りもされない。予約済み | なし |
| `docs\`、`examples\` | 永続的。リリースパッケージに同梱 | パッケージ |

データがマシン外に送信されることはありません。診断情報はローカルファイルのみです。[`.omsilaunch` ディレクトリ](../../../concepts/omsilaunch-directory.md)も参照してください。

<a id="runtime-mutations-are-not-journaled"></a>
## ランタイムでの変更はジャーナル化されない

ランタイム制御操作（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random`、D3D テクスチャ）は、OMSI のメモリ上の状態のみを変更します。これらはジャーナルに記録されず、復元もされません。プロセスとともに消えます。[ランタイム制御](../reference/runtime-control.md)を参照してください。

<a id="failure-modes-and-error-codes"></a>
## 失敗モードとエラーコード

| コード | 意味 | その後のジャーナル |
| --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | リースを別のオーナーが保持している、またはジャーナルに記録された OMSI プロセスがまだ生存している | 保持 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | スナップショットはあるがディスク上にジャーナルがないトランザクションに対して復元が要求された | 該当なし |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | バックアップのハッシュがスナップショットのフィンガープリントと異なる。何も書き込まれていない | 保持 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | 元々存在しなかったオーバーレイパスに、セッションが書き込んでいない内容が現在存在する | 保持 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | フィンガープリント導入前のジャーナルで、元々存在しなかったパスが現在存在する。予定バイト列が同一の新しいセッションのみがこれを完了できる | 保持（延期） |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | 検証済みの復元後に `journal.json` を削除できなかった | 保持（再実行はべき等） |
| `OL_E_RESTORE_DEFERRED` | OMSI の終了が確認されていない。復元は次回の開始まで延期 | 保持 |
| `OL_E_RESTORE_FAILED` | その他の復元失敗（復元後の存在またはハッシュの不一致、I/O エラー） | 保持 |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | トランザクション前に古い `closecheck` を削除できなかった | まだ存在しない |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | 警告: セッション削除パス上の外部ファイルが保持された | 完了 |
| `OL_E_PLAN_NOT_RUNNABLE` | 開始時の再プランニングで、スペックが実行不可になっていることが判明した（たとえば `Omsi.exe` が変更された）。トランザクションは開かれない | なし |

CLI は `OL_E_RECOVERY_*` と `OL_E_RESTORE_FAILED` を終了コード 8 に、`OL_E_INSTALLATION_BUSY` を終了コード 7 にマッピングします。[終了コード](../reference/exit-codes.md)を参照してください。

<a id="evidence"></a>
## エビデンス

`tools/OmsiLaunch.TestHost` のオフラインテストがトランザクションの経路をカバーしています: `transaction.restore`、`transaction.options-overlay-restore`、`transaction.absent-overlay-restore`、`transaction.absent-file-ownership`、`transaction.absent-file-recovery`、`transaction.session-delete-restore`、`transaction.deletion-created-during-session`、`transaction.deletion-foreign-file-retained`、`transaction.deletion-recovery-after-crash`、`transaction.backup-corrupt-rejected`、`transaction.metadata-and-backup-cleanup`、`transaction.legacy-journal-ownership-migration`、`transaction.recovery-pre-pid-window`、`transaction.recovery-then-apply-ownership`、`transaction.restore-failure-recovery`、`transaction.failure-boundaries`、`transaction.empty-journal-restore`、`api.recover-requires-lease`、`lease.cross-thread-release`。

ランタイムのエビデンス（検証マトリクス）: RV-005 と RV-006（オーバーレイの適用とバイト単位で正確な復元。セッション `1e8e0548-...` およびプレゼンテーションバッチ）、RV-008 の早期終了のパス（セッション `0dc40570-...`）。

ランタイムのエビデンス（ランタイムクロージャーラウンド、2026-09-23、`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）:
- OMSI の実際のダウンローダーを使った `.itx` ターゲットのセッション生成物の削除。通常の停止時と、オーナーの中断後に `/recover` を実行した場合（S-01、`I01`、`I02`）
- オーバーレイ構築前の早期リカバリ（S-05、`S05`）
- メタデータと読み取り専用属性の復元、およびバックアップのクリーンアップ（S-12、`S12a`、`S12b`）
- リース下での `/recover`。孤立した OMSI がある場合と、PID 記録前の時間帯の場合（S-04、`S04`、`S04b`）
- 起動失敗、および復元失敗後の `/recover`（RV-008 の残り、`SF01`、`SF02`、`F01`）
- CP1252 の保持（S-07、`C01`）

フィンガープリント導入前のジャーナルに対する延期リカバリの分岐は、引き続きオフラインのみの検証です。[ランタイム検証ステータス](../status/runtime-validation-status.md)を参照してください。
