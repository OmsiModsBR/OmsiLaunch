# セッションのライフサイクル

<!-- l10n: source=concepts/session-lifecycle.md source-sha256=17700c9e44da7c288003c88f490d16d8ac191c8a4a16db97cb4cfd74d6f5930b -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../concepts/session-lifecycle.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch のセッションが `SessionState` 上で `Created` から `Completed` または `Failed` へどのように遷移するかを説明します。具体的には、各状態をどのコンポーネントが設定するか、どのプラグインテレメトリイベントが遷移を駆動するか、起動タイムアウトの仕組み、停止が何を意味するか（強制終了）、`WaitForAsync` が何を返すか、どの状態が終端状態か、どの状態がまったく（またはほとんど）観測できないか、そして CLI オーナーがどのような保証を提供するかを扱います。ここに記載する内容はすべて `OmsiLaunchService.StartAsync`、`SuperviseAsync` および `ApplyTelemetry`（`src/OmsiLaunch.Core/OmsiLaunchService.cs`）、`PluginRuntime`（`src/OmsiLaunch.Plugin/PluginRuntime.cs`）、`OwnerSession`（`tools/OmsiLaunch.Cli/Program.cs`）に基づいています。

関連ページ: [公開 API](../reference/public-api.md)、[エラーコード](../reference/errors.md)、[トランザクションとリカバリ](transactions-and-recovery.md)、[常駐プラグイン](permanent-plugin.md)、[ランタイム制御](../reference/runtime-control.md)、[ローカルコントロールプレーン](../reference/local-control.md)、[Windows トレイ](../reference/windows-tray.md)、[CLI リファレンス](../reference/cli.md)、[ランタイム検証ステータス](../status/runtime-validation-status.md)、[`.omsilaunch` ディレクトリ](../../../concepts/omsilaunch-directory.md)。

<a id="overview"></a>
## 概要

```
PlanSessionAsync                       (no state; returns a SessionPlan)
StartSessionAsync ─ caller thread ─────────────────────────────────────────────
  Created
  AcquiringInstallationLock            lease Local\OmsiLaunch.Installation.<hash>
  RecoveringPreviousTransaction        stale journal restored before anything is read
  Snapshotting → ApplyingConfiguration journal Prepared, overlays written, deletions removed, Applied
  DeployingRuntime                     journal RuntimeDeployed (plugin is permanent; nothing copied)
  CreatingStartupHandoff               handoff, telemetry slot, runtime mailbox; journal HandoffCreated
  StartingProcess                      CreateProcessW Omsi.exe
  WaitingForPlugin                     journal ProcessStarted (PID, creation time, exe path) → handle returned
SuperviseAsync ─ background task ──────────────────────────────────────────────
  PluginBootstrap                      telemetry plugin.started
  StartingWorld                        telemetry world.starting (NEW_MAP only)
  Running                              telemetry gameplay.entered
  ProcessExited                        OMSI exited or was terminated; journal ProcessExited
  Restoring                            exact restore of every session-owned file
  CleaningRuntime                      restore verified; journal removed; backups removed
  Completed                            stores disposed, lease released
  Failed                               from any point above; restore still runs
```

<a id="sessionstate-reference"></a>
## `SessionState` リファレンス

値は宣言順に並んでいます。「設定元」は `Move`/`Fail` を呼び出すコードを示し、「観測可能」は `GetStatusAsync`/`WaitForAsync` から実際にその状態が見えるかどうかを示します。

| # | 状態 | 設定元 | 観測可能 | 意味 |
| --- | --- | --- | --- | --- |
| 0 | `Created` | `StartSessionAsync`（ライブセッションの初期値） | ごく短時間 | セッションが登録された状態です。まだ何も行われていません。 |
| 1 | `ValidatingPlatform` | なし | いいえ | 宣言されていますが、現在のサービスが設定することはありません（プラットフォーム検証は `PlanSessionAsync` で行われ、そこにはセッション状態がありません）。 |
| 2 | `Planning` | なし | いいえ | 宣言されていますが、設定されることはありません（プランニングはセッションが存在する前に行われ、`StartSessionAsync` 内の再プランニングも登録より前に行われます）。 |
| 3 | `AcquiringInstallationLock` | `StartAsync` | はい | インストールリースを取得中です。失敗時: `OL_E_INSTALLATION_BUSY`。 |
| 4 | `RecoveringPreviousTransaction` | `StartAsync` | はい | ライブのインストール環境を読み取る前に、保留中の `journal.json` を復元します。さらに常駐プラグインのクロージャーを検証し（`plugin.integrity.reference`）、`Omsi.exe` のハッシュを計算し、スプラッシュアセットを配置し、古い `closecheck` を削除します。失敗時: `OL_E_PERMANENT_PLUGIN_*`、`OL_E_SPLASH_*`、`OL_E_ITX_*`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_*`、`OL_E_INSTALLATION_BUSY`（ジャーナルに記録されたプロセスが生存中）。 |
| 5 | `Snapshotting` | `StartAsync` | 実質的に不可 | `ApplyingConfiguration` の直前に設定され、その間に await はありません。スナップショット自体は `ApplyAsync` の内部で取得されます。一時的で観測できない状態です。 |
| 6 | `ApplyingConfiguration` | `StartAsync` | はい | `journal.json` が書き込まれ（`Prepared`）、元のファイルがバックアップされ、オーバーレイが書き込まれ、セッション削除対象が削除されます（`Applied`）。失敗時: `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、I/O エラー。 |
| 7 | `DeployingRuntime` | `StartAsync` | はい | ジャーナル状態は `RuntimeDeployed` です。ファイルは一切配置されません。プラグインのクロージャーは常駐しているためです。 |
| 8 | `CreatingStartupHandoff` | `StartAsync` | はい | ハンドオフ（`OmsiLaunch.Handoff.<id>`）、テレメトリスロット（`OmsiLaunch.Telemetry.<id>`）、ランタイムメールボックス（`OmsiLaunch.Runtime.<id>`）が作成済みです。ジャーナル状態は `HandoffCreated` です。 |
| 9 | `StartingProcess` | `StartAsync` | はい | 作業ディレクトリを `<root>` として `<root>\Omsi.exe` に対して `CreateProcessW` を実行します。失敗時: `OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。 |
| 10 | `WaitingForPlugin` | `StartAsync` | はい | プロセスが存在し、`process.started` が記録され、ジャーナルは `ProcessStarted` となり、スーパーバイザーが開始され、`StartSessionAsync` が戻ります。 |
| 11 | `PluginBootstrap` | `plugin.started` を受けた `ApplyTelemetry` | はい | 常駐プラグインがこのセッション用の有効なハンドオフを読み取りました。ここから先のタイムアウトは `OL_E_PLUGIN_NOT_LOADED` ではなく `OL_E_STARTUP_TIMEOUT` になります。 |
| 12 | `StartingWorld` | `world.starting` を受けた `ApplyTelemetry` | はい（NEW_MAP のみ） | プラグインが OMSI の UI スレッド上でネイティブの NEW_MAP 開始処理を呼び出しました。保存済みシチュエーションは `world.situation.starting` を発行しますが、これは状態にマッピングされていないため、SAVED_SITUATION セッションは `PluginBootstrap` から直接 `Running` に遷移します。 |
| 13 | `EnteringGameplay` | なし | いいえ | 宣言されていますが、設定されることはありません。`gameplay.entered` によってセッションは直接 `Running` に遷移します。 |
| 14 | `Running` | `gameplay.entered` を受けた `ApplyTelemetry` | はい | ゲームプレイに到達しました。`ExecuteRuntimeAsync` が許可され、CLI オーナーはローカルコントロールプレーンを開き、トレイは実行中のセッションを表示します。 |
| 15 | `ProcessExited` | `SuperviseAsync` | はい（成功したセッションのみ） | OMSI が終了しており（自然終了または強制終了）、ジャーナルは `ProcessExited` です。 |
| 16 | `Restoring` | `SuperviseAsync`（および開始失敗時の経路） | はい（成功したセッションのみ） | セッションが所有するすべてのファイルを、検証済みのバックアップから復元します。セッションの生成物は削除されます。 |
| 17 | `CleaningRuntime` | `SuperviseAsync` | はい（成功したセッションのみ） | 復元が検証され、ジャーナルとバックアップが削除されました。これからランタイムストアが破棄されます。 |
| 18 | `Completed` | `SuperviseAsync`（`finally`） | はい、終端 | ストアが破棄され、メールボックスが閉じられ、リースが解放され、失敗は記録されていません。 |
| 19 | `Failed` | `StartAsync`、`SuperviseAsync`、`ApplyTelemetry` からの `LiveSession.Fail` | はい、終端 | 失敗の診断が記録されました。この状態は固定されます。以降の `Move` 呼び出しは無視されるため、失敗したセッションでは終了処理と復元が引き続き実行されても、`ProcessExited`/`Restoring`/`CleaningRuntime`/`Completed` が表示されることはありません。 |

終端状態は `Completed` と `Failed` です。いずれかに達した後は、`WaitForAsync` は即座に戻り、`CloseAsync` は停止を要求せずに戻ります。

「設定されることはない」ことの確認: コードベースで `SessionState.ValidatingPlatform`、`SessionState.Planning`、`SessionState.EnteringGameplay` を検索しても、見つかるのは列挙型の宣言だけです。`SessionState.Snapshotting` は 1 回だけ出現し、その直後に `Move(SessionState.ApplyingConfiguration)` が続きます。

<a id="start-phase-startsessionasync"></a>
## 開始フェーズ（`StartSessionAsync`）

1. 実行不可のプランを拒否し（`OL_E_PLAN_NOT_RUNNABLE`）、スペックを再プランニングし（`Omsi.exe` のハッシュを再計算し、コンテンツを再解決し、プラグインのクロージャーを再チェックします）、実行可能でなくなっていれば再度拒否します。ライブセッションを登録します（`Created`）。
2. ホストトレース `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` を作成します（最新 50 セッションより古い、セッション ID を接頭辞とするファイルは削除されます）。1..600 の範囲外の `StartupTimeoutSeconds` を拒否します（`ArgumentOutOfRangeException`。セッションの登録は解除されます）。
3. `AcquiringInstallationLock` → リースを取得します。`RecoveringPreviousTransaction` → 保留中のジャーナルをリカバリし（所有権を証明できないフィンガープリント導入前のジャーナルは後回しにされ、このセッションのオーバーレイが作成された時点で再試行されます）、常駐プラグインのクロージャーを検証し、実行ファイルのハッシュを計算し、古い `closecheck` を削除し、トランザクションを構築します（オーバーレイ: `options.cfg` のパッチ、管理対象のスプラッシュ BMP、`Texture\standard.itx`。削除対象: ITX のターゲット、`Texture\standard.ipr`、存在しない場合の `closecheck`）。
4. `Snapshotting` → `ApplyingConfiguration` → `DeployingRuntime` → `CreatingStartupHandoff` → `StartingProcess` → `WaitingForPlugin` と進み、その後スーパーバイザータスクが開始され、ハンドルが返されます。
5. 手順 3–4 で発生した例外はすべて捕捉されます。セッションは `OL_E_START_SESSION`（内部メッセージ付き）で `Failed` となり、作成済みのプロセスは強制終了されてその終了が待機され、ストアは破棄され、トランザクションは復元されます（失敗時は `OL_E_RESTORE_FAILED`）。OMSI の終了を確認できなかった場合は、`OL_E_RESTORE_DEFERRED` とともに保留のまま残されます。リースは解放されます。この場合も `StartSessionAsync` はハンドルを返すため、`GetStatusAsync` を参照してください。

再プランニングでは呼び出し元の `SessionId` が維持されるため、ハンドル内の ID は `plan.SessionId` と一致します。

<a id="supervision-superviseasync"></a>
## 監視（`SuperviseAsync`）

スーパーバイザーはスレッドプールのタスク上で動作し、OMSI が終了するか停止が要求されるまで 100 ms ごとにループします。

1. プラグインが前回の読み取り以降にコミットしたテレメトリイベントをすべて順番に読み取ります（イベントリングです。遅れた読み取り側には、件数を伴う `telemetry.lost` が渡されます）。イベントはすべて `RuntimeEvents` に追加され、`ApplyTelemetry` によってマッピングされます。
2. セッションが `Failed` であれば、ループを抜けます。
3. セッションがまだ `Running` ではなく、期限（スーパーバイザー開始から `StartupTimeoutSeconds` 後）を過ぎている場合: `PluginBootstrap` に到達していれば `OL_E_STARTUP_TIMEOUT`、そうでなければ `OL_E_PLUGIN_NOT_LOADED` で `Fail` し、ループを抜けます。

ループ終了後: `Running` より前に OMSI が終了し、失敗が記録されていなかった場合は、`OL_E_PROCESS_EXITED_EARLY` で `Fail` します。その後、セッションが失敗したかどうかにかかわらず、OMSI がまだ生存していれば強制終了し、終了を待機し、ジャーナルを `ProcessExited` とし、`ProcessExited` に遷移し、復元を行う（`Restoring` → `CleaningRuntime`）か `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` を記録し、プロセスハンドル、ハンドオフ、テレメトリスロット、ランタイムメールボックスを破棄し、リースを解放し、状態が `Failed` でなければ `Completed` に遷移します。スーパーバイザー自体の内部で障害が発生した場合は `OL_E_PROCESS_SUPERVISION`（クリーンアップの問題は `OL_E_PROCESS_CLEANUP_FAILED`）として記録され、同じ終了・復元の経路が実行されます。

`Failed` は固定されるため、失敗したセッションが復元されたことを示す唯一の根拠は、その診断に `OL_E_RESTORE_FAILED`/`OL_E_RESTORE_DEFERRED` が存在しないこと（および `journal.json` が存在しないこと）です。復元に関する注記（`restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）はどちらの場合にも表示されます。

<a id="telemetry-events"></a>
## テレメトリイベント

プラグインは JSON の `{ "name": ..., "data": {...} }` サンプルをテレメトリスロットに公開します。ホストは新しいサンプルをそれぞれ `RuntimeEvent(Type = name, TimestampUtc = host receipt time, Sequence, Data)` として記録します。

| Event | 発行元 | ホストの動作 |
| --- | --- | --- |
| `plugin.started` (`session_id`) | 有効なハンドオフを読み取った後の `PluginRuntime.Start` | `Move(PluginBootstrap)`、`PluginStarted = true` |
| `plugin.handoff.invalid` | `PluginRuntime.Start`: `OMSILAUNCH_HANDOFF_NAME` がない、またはハンドオフが読み取れない・検証できない | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |
| `plugin.request.unsupported` | `PluginRuntime.Start`: ハンドオフが NEW_MAP/SAVED_SITUATION 以外のワールドモード、ヘッドレスでない開始、プレイヤー車両、日付・時刻モード、または空のシチュエーション識別子を要求している | `Fail(OL_E_CAPABILITY_UNAVAILABLE)` |
| `plugin.build.invalid` | `PluginRuntime.Start`: プロセス内でのビルド検証に失敗した | `Fail(OL_E_BUILD_VALIDATION_FAILED)` |
| `plugin.build.validated` | `PluginRuntime.Start` | 記録のみ |
| `headless.arm.failed` | `PluginRuntime.Start`: ネイティブのヘッドレス開始フックを有効化できなかった | `Fail(OL_E_HEADLESS_ARM_FAILED)` |
| `headless.armed` | `PluginRuntime.Start` | 記録のみ |
| `internet-textures.suppressed` / `internet-textures.suppression.failed` | `InternetTextures.Mode` が `Disabled` のときの `CurrentDnneAdapter.PluginStart` | 記録のみ |
| `world.starting` (`map`, `presented_index`, `entrypoint_identity`) | `PluginRuntime.ConsumePendingWorld`（NEW_MAP） | `Move(StartingWorld)` |
| `world.waiting-native-ready` (`native_status` 3 or 4) | NEW_MAP: OMSI の準備がまだ整っておらず、次の UI タイマーのティックで開始が再試行される | 記録のみ |
| `world.loaded`, `world.entrypoint.selected` (`presented_index`, `raw_index`, `presented_label`, `raw_label`) | NEW_MAP の成功経路 | 記録のみ |
| `world.failed` (`native_status`) | NEW_MAP: ネイティブの開始処理が失敗を返した | `Fail(OL_E_WORLD_START_FAILED)` |
| `world.situation.starting`, `world.situation.loaded` (`situation`) | SAVED_SITUATION の経路 | 記録のみ（状態は変化しない） |
| `world.situation.failed` (`native_status`, `situation`) | SAVED_SITUATION: ネイティブの開始処理が失敗を返した | `Fail(OL_E_SITUATION_LOAD_FAILED)` |
| `gameplay.entered`（NEW_MAP: エントリポイント選択のフィールドまたは `entrypoint_diagnostics = unavailable`、SAVED_SITUATION: `situation`） | ワールド開始処理の終わり | `Move(Running)` |
| `d3d.ready`, `d3d.lost`, `d3d.resetting`, `d3d.restored`, `d3d.stopped` (`state`, `generation`, `execution_thread_id`, `live_textures`) | いずれかの `d3d.*` 操作がプローブを有効化した後の `CurrentRuntimeControl.PollLifecycle` | 記録のみ |
| `camera.lock.degraded` (`code`) | 有効な `camera.lock` の再適用が例外をスローしたときの `CurrentRuntimeControl.PollLifecycle`（異なるエラーごとに 1 回報告） | 記録のみ |
| 不正な JSON | 任意 | `Fail(OL_E_PLUGIN_PROTOCOL_MISMATCH)` |

注意事項: スロットは 1 つのサンプルしか保持しないため、ホストの 1 回の 100 ms ポーリング内に発行された複数のイベントは失われる可能性があります（プラグインは `gameplay.entered` の後 2 s の間ライフサイクルイベントを抑制し、`gameplay.entered` を公開したティックでは D3D イベントを決して公開しないため、`Running` の境界が見落とされることはありません）。`RuntimeEvents` は最新 256 件のイベントを保持し、それより古いものは破棄されます。これは欠落のないログではありません。イベントは `GetStatusAsync`、コントロールプレーンの `session.events`、または CLI の `events read|watch` で読み取ってください。

<a id="startup-timeout"></a>
## 起動タイムアウト

| 項目 | 値 |
| --- | --- |
| 設定元 | `LaunchSpec.Behavior.StartupTimeoutSeconds`（既定値 180、1..600。CLI では `/startup-timeout`、プロファイルでは `behavior.startup-timeout`）。 |
| 計測開始 | スーパーバイザータスクがループに入った時点（ハンドルが返された後）。 |
| `PluginBootstrap` 前の期限切れ | `OL_E_PLUGIN_NOT_LOADED` で `Failed`。 |
| `PluginBootstrap` 後、`Running` 前の期限切れ | `OL_E_STARTUP_TIMEOUT` で `Failed`。 |
| `Running` 以降 | タイムアウトは適用されません。セッションは OMSI が終了するか停止が要求されるまで続きます。 |
| CLI オーナー | `Running` になるまで `StartupTimeoutSeconds + 5` 秒待機します。失敗時はステータスを出力し、`OmsiLaunchW.exe` の下では最後の `OL_E_` 診断（フォールバックコードは `OL_E_SESSION_START_FAILED`）を示すダイアログを表示し、`CloseAsync` の後に終了コード 1 で終了します。 |

`ShutdownTimeoutSeconds` はスペックに含まれていますが、使用されません。シャットダウンの待機は存在しません。

<a id="stop-semantics"></a>
## 停止のセマンティクス

停止要求はすべて同一の正規の要求です。API からの `StopAsync(handle)`、終端状態でないセッションに対する `CloseAsync`、ローカルコントロールプレーンの `session.stop`（アクティブなセッション ID に紐付く）、トレイの "End session"、CLI オーナーでの Ctrl+C またはコンソールのクローズ、そして `/observe-seconds` の終了がこれに当たります。

| 手順 | 詳細 |
| --- | --- |
| 1 | ライブセッションに `StopRequested` が設定され、呼び出し元は即座に戻ります。 |
| 2 | 100 ms 以内にスーパーバイザーがループを抜け、`TerminateProcess(Omsi.exe, 1)` を呼び出します。これは強制終了です。OMSI のシャットダウン処理は実行されず、OMSI は `options.cfg` を書き換えず、保存ダイアログも表示されません。これは、トランザクションがこれから復元するファイルを OMSI が上書きできないようにするための意図的な設計です。 |
| 3 | スーパーバイザーはプロセスの終了を待機し、`ProcessExited` を記録し、セッションが所有するすべてのファイルを正確に復元し（セッション中に OMSI が書き込んだ `closecheck` マーカーも含みます。これは `restore.session-artifact-removed` の注記になります）、ジャーナルとバックアップを削除し、ランタイムストアを破棄し（以降の `ExecuteRuntimeAsync` 呼び出しは `OL_E_RUNTIME_CHANNEL_CLOSED` または `OL_E_SESSION_NOT_RUNNING` をスローします）、リースを解放し、`Completed` に遷移します。 |
| 自然終了 | `Running` の後に OMSI が自ら終了した場合（ユーザーが OMSI を閉じた場合）は、強制終了なしで同じ経路が実行され、セッションは正常に完了します。`Running` より前であれば `OL_E_PROCESS_EXITED_EARLY` になります。 |
| 協調的シャットダウン | 実装されていません。`WM_CLOSE` を送信して `ShutdownTimeoutSeconds` の間待機する処理は実装されていません（製品上の判断です。ランタイムクロージャーラウンドでは、OMSI はメインウィンドウへの `WM_CLOSE` を無視しました。`L05b`）（[ランタイム検証ステータス](../status/runtime-validation-status.md)）。 |
| ランタイム側の状態 | ランタイム操作によって変更されたもの（時計、カメラ、スポーンした車両、スクリプト変数、D3D テクスチャ）はプロセス内の状態であり、プロセスとともに消えます。復元も永続化もされません。 |

<a id="waitforasync-semantics"></a>
## `WaitForAsync` のセマンティクス

| 状況 | 結果 |
| --- | --- |
| セッションが要求された状態に到達した | `State == requested` のステータスを返します。 |
| セッションが先に終端状態に到達した | `Completed` または `Failed` で即座に戻ります（`Diagnostics` を確認してください）。 |
| タイムアウトが経過した | 現在のステータスを返します（例外は発生しません）。`State` を要求した状態と比較してください。 |
| 要求した状態をすでに通過している（または設定されない状態: `ValidatingPlatform`、`Planning`、`EnteringGameplay`、実質的に `Snapshotting`） | 終端状態かタイムアウトまで待機します。 |
| 呼び出し元がキャンセルした | `OperationCanceledException`。 |
| 不明なハンドルまたはクローズ済みのハンドル | `KeyNotFoundException`。 |

ポーリング間隔は 100 ms であるため、観測される遷移は実際の遷移より最大 100 ms 遅れます。

<a id="owner-lifecycle-guarantees-cli"></a>
## オーナーのライフサイクル保証（CLI）

`tools/OmsiLaunch.Cli/Program.cs` の `OwnerSession.RunAsync` がリファレンスとなるオーナー実装です。

| 保証 | 詳細 |
| --- | --- |
| 単一のオーナー | 開始前に CLI はコントロールパイプを調べ、オーナーが応答した場合は `OL_E_SESSION_ALREADY_ACTIVE`（終了コード 7）で拒否します。リースはプロセスをまたいで同じ規則を強制します。 |
| すべての終了経路が `CloseAsync` に到達する | `StartSessionAsync` 以降は、例外、Ctrl+C（`CancelKeyPress`）、コンソールのクローズ・ログオフ（`ProcessExit`: 停止が要求され、オーナーは `Completed` になるまで最大 4 s 待機します。残ったものは次回の開始時にジャーナルによってリカバリされます）、トレイからの停止、コントロールプレーンの `session.stop`、`/observe-seconds` の期限切れ、自然な完了のいずれも、コントロールプレーンとトレイを破棄して `CloseAsync` を待機する `finally` ブロックで終わります。 |
| `/observe-seconds` は上限値である | トレイやコントロールプレーンからの停止要求により、セッションはそれより早く終了することがあります。 |
| コントロールプレーンは Running の間のみ | 名前付きパイプのエンドポイントは `Running` の後（および `INTERNAL` の検証バッチの後）に作成され、`CloseAsync` の前に破棄されます。それ以外のタイミングでは、クライアントは `OL_E_NO_ACTIVE_SESSION` を受け取ります。 |
| 終了コード | 最終状態が `Completed` の場合は 0、`Failed` の場合またはゲームプレイに到達しなかった場合は 1、要求したリカバリが完了しなかった場合は 8 です（[終了コード](../reference/exit-codes.md)）。 |
| 診断 | ホストトレースとランタイム操作の生成物は `<root>\.omsilaunch\diagnostics` 以下に、トレイのログは `tray-host.log` に出力されます。データがマシン外に送信されることはありません。 |

独自のオーナーを実装するインテグレーターは、最初の 2 つの保証を再現する必要があります。すなわち、インストール環境ごとに同時に 1 つの `StartSessionAsync` のみとすること、そしてすべての経路で `CloseAsync` を呼び出すことです。

<a id="failure-map"></a>
## 失敗の対応表

| フェーズ | 失敗時の状態 | 表示される診断 |
| --- | --- | --- |
| プラン | なし（セッションなし） | `StartSessionAsync` がスローする `OL_E_PLAN_NOT_RUNNABLE`、およびプラン自身の `OL_E_` コード（[LaunchSpec の検証](../reference/launchspec.md#validation-rules-and-non-runnable-diagnostics)）。 |
| 開始（リース取得からプロセス作成まで） | `Failed` | 内部コード付きの `OL_E_START_SESSION`。場合により `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_RESTORE_FAILED`。 |
| プラグインのブートストラップ | `Failed` | `OL_E_PLUGIN_NOT_LOADED`、`OL_E_PLUGIN_PROTOCOL_MISMATCH`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_BUILD_VALIDATION_FAILED`、`OL_E_HEADLESS_ARM_FAILED`。 |
| ワールドの開始 | `Failed` | `OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_STARTUP_TIMEOUT`、`OL_E_PROCESS_EXITED_EARLY`。 |
| 実行中 | スーパーバイザーの障害時のみ `Failed` | `OL_E_PROCESS_SUPERVISION`。ランタイム操作のエラーによってセッションが失敗することはありません。 |
| 終了処理と復元 | `Failed` | `OL_E_RESTORE_FAILED`、`OL_E_RESTORE_DEFERRED`、`OL_E_PROCESS_CLEANUP_FAILED`。 |

どの失敗経路でも終了処理と復元は試行されます。残ったジャーナルは、次回の開始時、または `RecoverPendingAsync` / `/recover` によってリカバリされます（[トランザクションとリカバリ](transactions-and-recovery.md)）。
