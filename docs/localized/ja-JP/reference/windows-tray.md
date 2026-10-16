# Windows トレイインジケーター

<!-- l10n: source=reference/windows-tray.md source-sha256=d38220bd1069b7c27c4e384abad4bd439883255437821375a4118a1750d01b4a -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/windows-tray.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

スタンドアロンのオーナーセッション（`OmsiLaunch.exe` または `OmsiLaunchW.exe` で開始されたもの）はすべて、セッションの状態を示し、利用者がセッションを終了できる通知領域アイコンを表示します。このページでは、`tools\OmsiLaunch.Cli\WindowsHost.cs` 内の `SessionTrayIndicator`、`StatusWindow`、`StopConfirmationWindow`、読み取り専用のプレゼンター `SessionStatusPresenter`（`tools\OmsiLaunch.Cli\SessionStatusPresenter.cs`）、文字列レジストリ `WindowsUiStrings`（`tools\OmsiLaunch.Cli\WindowsUiStrings.cs`）、および `WindowsHost` の `OmsiLaunchW.exe` エラーダイアログによって実装されているインジケーターの仕様を示します。トレイは表示用のアダプターにすぎません。OMSI もリカバリも所有せず、その停止操作は `session stop` と同じオーナーの正規停止経路に通知します（[CLI リファレンス](cli.md)と[ローカル制御](local-control.md)を参照）。

<a id="when-the-icon-exists"></a>
## アイコンが存在する期間

| 段階 | 動作 |
|---|---|
| 作成 | `SuppressTrayIcon` が設定されていない限り、`StartSessionAsync` が戻った直後、セッションが `Running` に到達する前に作成されます。したがって、アイコンは `StartingProcess`、`WaitingForPlugin`、`StartingWorld`、`EnteringGameplay` の間も存在します。 |
| 起動時間の上限 | UI スレッド（`STA`、バックグラウンド、名前は `OmsiLaunch tray`）は 2 s 以内にアイコンを公開しなければなりません。間に合わない場合、または作成中に何らかの例外が発生した場合、インジケーターは破棄され、セッションはアイコン**なし**で続行します。`startup-timeout` または例外がログに記録されます。起動が遅くても、表示されたアイコンが取り残されることはありません。 |
| 削除 | オーナーの `finally` ブロック内で、セッションが完了または失敗した後、`CloseAsync` の前に行われます。破棄処理は UI スレッドにシャットダウンをポストし（メニュー、確認ダイアログ、ステータスウィンドウを閉じてからメッセージループを終了）、最大 2 s スレッドの終了を待ち（超過時は `dispose-timeout` をログに記録）、`NotifyIcon` を非表示にして破棄します。 |
| `SuppressTrayIcon` | `SessionPresentationSpec.SuppressTrayIcon`（`LaunchSpec` のフィールド `Presentation.SuppressTrayIcon`、既定値 `false`）です。`/spec` と API で設定できますが、CLI フラグはありません。独自のセッション表示 UI を提供するインテグレーターは `true` に設定します。セッションのそれ以外の動作は何も変わりません。 |
| ホスト | `OmsiLaunch.exe`（コンソール）と `OmsiLaunchW.exe`（Windows サブシステム）の両方がアイコンを表示します。`/silent` で開始された `OmsiLaunchW.exe` のセッションには、これ以外に目に見える表示はありません。 |

<a id="icon-and-tooltip"></a>
## アイコンとツールチップ

- アイコン: 実行中の実行ファイルに関連付けられたアイコン（`Icon.ExtractAssociatedIcon(Application.ExecutablePath)`、埋め込まれた OmsiLaunch アイコン）です。取得できない場合は `SystemIcons.Application` にフォールバックします。
- ツールチップのテキスト: `Tray.Running`（`OmsiLaunch is running`、OmsiLaunch が実行中であることを示す文字列）。停止中もテキストは変わりません（「停止中」を示す文字列はまだなく、オーナーがアイコンを削除するまでアイコンはそのままです）。
- ビジュアルスタイルが有効になっています（`Application.EnableVisualStyles`）。

<a id="interaction"></a>
## 操作

| Action | 結果 |
|---|---|
| 右クリック | トレイウィンドウをフォアグラウンドウィンドウにし（通知アイコンのメニューに必要です。これを行わないと、OMSI が前面にある間、メニューがクリックを無視して閉じなくなることがあります）、実際のカーソル位置（`Cursor.Position`。シェルがホストするイベントでは `NotifyIcon` が `(0,0)` を報告することがあるため、イベントの座標は使いません）にコンテキストメニューを開きます。メニューは、カーソルのある画面の作業領域内に収まるよう調整されます。 |
| ダブルクリック | ステータスウィンドウを開きます（`Status` メニュー項目と同じ）。 |
| 左クリック | 何も行いません。 |
| メニュー項目 `Status`（`Tray.Status`） | 読み取り専用のステータスウィンドウを開きます（すでに開いている場合はアクティブにします）。 |
| 区切り線 | |
| メニュー項目 `End session`（`Tray.EndSession`、アクセシビリティ用の説明 `Tray.EndSessionDescription`） | 確認ダイアログを開きます。 |

<a id="status-window-read-only-snapshot"></a>
### ステータスウィンドウ（読み取り専用のスナップショット）

`Status` メニュー項目、またはアイコンのダブルクリックで開きます（`SessionTrayIndicator.ShowStatus`）。固定サイズ・画面中央配置・自動サイズ調整のダイアログで、タスクバーには表示されず、ボタンは `Close`（`Status.Close`、ウィンドウを閉じるボタン。`Escape` でも閉じます）の 1 つだけです。ウィンドウがすでに開いているときに `Status` を選ぶと、ウィンドウを再構築せずにアクティブにします（最初に開いたときのスナップショットが保持されます）。ウィンドウの構築に失敗した場合は `tray-host.log` に書き込まれますが、セッションには影響しません。

**これはスナップショットであり、ライブビューではありません。** `SessionStatusPresenter.Create(plan, status, ui)` はウィンドウを開いたときに 1 回だけ実行されます。セッションの解決済み `SessionPlan`（実際にプランされた spec）と、1 つの `SessionStatus`（その `State` のみ）を読み取ります。ウィンドウを開いている間は何も更新されず、OMSI に問い合わせることもありません（ランタイム操作もテレメトリ値も使いません）。新しい状態を確認するには、いったん閉じてから開き直してください。

ウィンドウのタイトルと見出し: 同じテキストで、`SessionStatus.State` が `Running` のときは `Status.SessionRunning`（`Session is running`、セッションが実行中であることを示す文字列）、それ以外のときは `SessionState` の名前そのものです（たとえば、アイコンは `Running` より前から存在するため、起動中に開くと `WaitingForPlugin` になります）。`Status.Title`（`OmsiLaunch session status`）は文字列テーブルに定義されていますが、このリリースでは使用されていません。

セクションは次の順序で表示され、フィールドが 1 つもないセクションは省略されます。すべての値はプランされた `LaunchSpec`/`SessionPlan` から取得され、OMSI から取得されることはありません。

| セクション（英語ラベル） | フィールド（英語ラベル） | 表示条件 | 値 | 取得元（対応する公開情報） |
| --- | --- | --- | --- | --- |
| `Session` | `Mode` | 常に表示 | `New session`（`WorldMode.NewMap`）、`Saved situation`（`WorldMode.SavedSituation`）、`Last map state`（その他のモード。`LastMapState` は実行不可のため、実際には到達しません） | `SessionPlan.Spec.World.Mode` |
| `Session` | `Map` | プランが `map` のコンテンツ識別子を解決した場合 | マップの `DisplayName`（マップのディレクトリ名。たとえば `Grundorf`）。ない場合は識別子のファイル名から拡張子を除いたもの | `Kind = "map"` を持つ `SessionPlan.ResolvedContent` のエントリ（NEW_MAP）。`DiscoverAsync(Maps)` も同じ `DisplayName` を返します |
| `Session` | `Situation` | シチュエーション識別子を持つ `SavedSituation` の場合 | 拡張子を除いたファイル名（`situations\Linie 5.osn` → `Linie 5`） | `Spec.World.SituationIdentity` |
| `Session` | `Entry point` | エントリポイントが要求された場合 | エントリポイント識別子が設定されていればその識別子（このリリースでは常に実行不可）、そうでなければ整数としての提示インデックス（`1`） | `Spec.World.EntrypointIdentity` / `PresentedEntrypointIndex` |
| `Session profile` | `Profile` | セッションプロファイルが使用された場合（`/predefined-profile`） | プロファイルの `name` | `Spec.SessionProfile.Name`（`SessionProfileMetadata`） |
| `Session profile` | `Preset` | 上記に加え、プリセットに名前がある場合 | プリセットの `name` | `Spec.SessionProfile.PresetName` |
| `Environment` | `Date`、`Time`、`Weather` | 明示的またはシステムの日付・時刻・天候が要求された場合 | `DD/MM/YYYY` または `System`、`HH:MM:SS` または `System`、ICAO コード・プリセット名・`Real/current` | `Spec.Date`、`Spec.Time`、`Spec.EffectiveWeather` |
| `Vehicle` | `Vehicle`、`Repaint`、`HOF`、`Fleet number`、`Registration` | プレイヤー車両のフィールドが要求された場合 | 要求された識別子/値 | `Spec.PlayerVehicle` |
| `Configuration` | セマンティック設定ごとに 1 フィールド | 設定が指定され（`/set`、プロファイルの `settings`、`LaunchSpec.Environment.*`）、かつ `ConfigurationCatalog` が認識している場合 | 要求された値。`Percent` で終わるキーには `%`、`DistanceMeters` で終わるキーには ` m` が付加されます。ラベルは、設定キーをドットで区切った各部分の先頭を大文字にしたものです（`graphics.maxFPS` → `Graphics MaxFPS`） | `Spec.Environment.*`。同じ値がプランの `PlannedMutations` になります |
| `Presentation` | `Splash` | 常に表示 | `Managed` または `Original OMSI` | `Spec.EffectivePresentation.Splash` |
| `Presentation` | `Internet textures` | 常に表示 | `Original OMSI`（`Native`）、`Disabled`、`Override` | `Spec.EffectiveInternetTextures.Mode` |

`Environment` セクションは、日付、時刻、年、または天候が要求された場合に、実行中の Beta 4 セッションに表示されます（それらの要求は試験的です。[既知の制限事項](known-limitations.md)を参照）。`Vehicle` セクションが実行中のセッションに表示されることはありません。いずれかのプレイヤー車両フィールドを要求するとプランが実行不可になり、そのようなセッションは開始されないためです。これは将来のビルドのために存在し、オフラインのプレゼンターテストでカバーされています。

ランタイムエビデンス（pt-BR の UI、ランタイムクロージャ `T01`）: NEW_MAP の Grundorf セッションでは `Sessão em execução` が表示されました。`Sessão`: `Modo: Nova sessão`、`Mapa: Grundorf`、`Ponto de entrada: 1`。`Apresentação`: `Splash: Gerenciado`、`Texturas da internet: OMSI original`。

同じデータは、トレイを使わずにツールからも取得できます。[ローカルコントロールプレーン](local-control.md)経由の `session status` は `SessionId`、`State`、`Diagnostics`、`RuntimeEvents`（ライブ）を返し、オーナーの `/plan --json` 出力（または `PlanSessionAsync`）は、ウィンドウが要約しているプラン済みの spec、解決済みコンテンツ、プラン済みの変更を返します。

<a id="end-session-with-confirmation"></a>
### セッションの終了（確認あり）

1. `StopConfirmationWindow`: タイトル `End session?`、メッセージ `OMSI 2 will be closed and the OmsiLaunch managed session will end.`（OMSI 2 が閉じられ、OmsiLaunch が管理するセッションが終了することを伝える文）、ボタンは `End session`（既定、`DialogResult.OK`）と `Cancel`（`Escape`）です。ダイアログが開いている間に 2 回目の要求があった場合は、別のダイアログを重ねて開かず、既存のダイアログをアクティブにします。
2. `OK` の場合、トレイは `requestCanonicalStop` を呼び出し、これがオーナーの `controlStopped` シグナルを完了させます。続いてオーナーが `StopAsync` を呼び出し、OMSI は `TerminateProcess` で終了され、セッションが所有するすべてのファイルが復元されます。トレイ自身が OMSI を終了させることはありません。
3. 要求が例外をスローした場合、エラーはログに記録され、`Stop.Failed`（`The session could not be ended. OMSI and its managed session remain active.`、セッションを終了できず、OMSI と管理下のセッションが引き続き動作していることを伝える文）が表示されます。
4. トレイは成功を確認表示しません。オーナーが復元を終えてインジケーターを破棄した時点でアイコンが消えます（ランタイムクロージャ `T01`: `End session` の確認から 607 ms 後にオーナーが終了しました）。
5. `Cancel`（またはダイアログを閉じること）では何も起こらず、セッションは実行を続けます（`T01`）。
6. ステータスウィンドウまたは確認ダイアログが開いている間に、別の経路（`session stop`、Ctrl+C、`/observe-seconds`、OMSI の終了）から停止が届いた場合、インジケーターの破棄処理の一部としてそれらは閉じられます。オーナーは利用者の操作を待ちません（`T02`: 両方のウィンドウが開いた状態で、パイプ経由の停止から 725 ms 後にオーナーが終了しました）。

<a id="explorer-restart"></a>
## エクスプローラーの再起動

`TrayWindow` は、`TaskbarCreated` ウィンドウメッセージを登録する非表示のネイティブウィンドウです。エクスプローラー（シェル）が再起動すると、このメッセージがブロードキャストされ、インジケーターはアイコンを再追加します（`Visible = false; Visible = true`）。ランタイムクロージャ `T01`: `explorer.exe` を終了させ、Windows によって再起動された後、アイコンは `Shell_TrayWnd` に戻り、メニューとステータスウィンドウは引き続き動作しました。

<a id="localization"></a>
## ローカライズ

`WindowsUiStrings.Resolve` は **Windows の UI カルチャ**（`CultureInfo.CurrentUICulture`）に従い、OMSI のコンテンツ言語やセッションプロファイルの言語には従いません。解決順序は、完全一致するカルチャ名、次に 2 文字の言語コード、最後に英語です。翻訳に欠けているキーは、すべて英語にフォールバックします。

| カルチャキー | 言語 |
|---|---|
| `en`, `en-US`, `en-GB` | 英語（既定およびフォールバック） |
| `pt-BR` | ブラジルポルトガル語。`pt-PT`（および単独の `pt`）は意図的に英語にフォールバックします。 |
| `de`, `de-DE` | ドイツ語 |
| `fr`, `fr-FR` | フランス語 |
| `pl`, `pl-PL` | ポーランド語 |

ローカライズされる文字列は、ツールチップ、2 つのメニュー項目、ステータスウィンドウ（見出し、セクションタイトル、フィールドラベル、`Close`）、モードとプレゼンテーションの値、そして確認ダイアログです。用語集は `docs\windows-ui-localization.md` で管理されており、オフラインテスト `windows-ui.localization-and-status`（`tests\OmsiLaunch.WindowsUiTests`）が解決処理とプレゼンターを検証します。

<a id="omsilaunchwexe-failure-dialogs"></a>
## `OmsiLaunchW.exe` エラーダイアログ

`OMSILAUNCH_WINDOWS_HOST=1`（`OmsiLaunchW.exe` が設定）の場合、`WindowsHost.ShowFailure` はコンソールへのエラー出力の代わりに、タイトルが `OmsiLaunch` のモーダルなメッセージボックス（エラーアイコン付き）を表示します。内容は `<message>`、空行、`Code: OL_E_...`、空行、`See .omsilaunch\diagnostics for details.`（詳細は診断ディレクトリを参照するよう促す文）です。このメッセージボックスは、すべての `CliInput.WriteError`（引数エラー、`OL_E_NO_ACTIVE_SESSION`、`OL_E_SESSION_ALREADY_ACTIVE`、分類済みの例外）で表示されるほか、起動プランが実行不可の場合（フォールバックメッセージ `The session plan is not runnable.`。ドキュメント監査 BUG-06）と、セッションが `Running` に到達しなかった場合（`The OMSI session did not reach gameplay.` と最後の `OL_E_` 診断。診断がない場合は `OL_E_SESSION_START_FAILED`）にも表示されます。`OmsiLaunchW.exe` の動作の詳細は [OmsiLaunchW.exe リファレンス](omsilaunchw.md)にあります。`OmsiLaunch.exe` の下では、同じ関数は何も行いません。.NET ホストの起動失敗（shim のコード `100`..`106`）は、ネイティブ shim 自身が表示します。[終了コード](exit-codes.md)を参照してください。

<a id="log-location"></a>
## ログの場所

`<root>\.omsilaunch\diagnostics\tray-host.log` に、1 エントリにつき 1 行で記録されます。形式は ISO-8601 の UTC タイムスタンプ、タブ、エントリの順です。エントリは `created`、`removed`、`startup-timeout`、`startup-cancelled`（破棄が起動処理と競合し、ループがスキップされた）、`dispose-timeout`、および UI の障害に関する例外の全文です。ログ記録はベストエフォートであり、例外をスローすることはありません。セッションのホストログ（`<sessionId>-host.log`）は、オーナーによって同じディレクトリに書き込まれます。トレイのログにはセッションのプレフィックスが付かず、50 セッション分の保持期間による削除の対象にもなりません。

<a id="lifecycle-guarantees"></a>
## ライフサイクルの保証

- トレイがセッションを所有することはありません。OMSI を開始することも、ファイルを復元することも、オーナーの停止経路を迂回することもできません。
- オーナーのすべての終了経路（正常完了、OMSI の終了、Ctrl+C、コンソールのクローズ、パイプ経由の停止、例外、`/observe-seconds`）は、`CloseAsync` の前にインジケーターを破棄します。そのため、オーナープロセスが強制終了された場合を除き、アイコンがセッションより長く残ることはありません（その場合も、取り残されたアイコンは次にマウスを重ねたときに Windows が削除します）。
- 作成と破棄はロックによって直列化されています。競合で破棄が先に勝った場合、UI スレッドはメッセージループをスキップして直ちにクリーンアップします。
- Windows Forms の処理はすべて専用の STA スレッド上で行われます。他のスレッドは、非表示のマーシャリング用コントロールを通じてポストするだけです。

<a id="residual-caveats-from-the-code-comments"></a>
## 残存する注意点（コードのコメントより）

- 「停止中」を示すツールチップのテキストはありません。アイコンは削除されるまで `OmsiLaunch is running` と表示します。
- シェルがホストするイベントでは、`NotifyIcon` が `(0,0)` のマウス座標を報告することがあります。そのため、代わりにカーソル位置を読み取ります。
- 確認ダイアログがモーダル表示中でもトレイのメッセージは届きます。2 つ目の確認ダイアログが重ねて開かれることはありません。
- UI スレッドが 2 s の破棄時間の上限内に終了しない場合、オーナーは待たずに処理を続行します（`dispose-timeout`）。
- ステータスウィンドウは、開いた時点のプラン済みの値のスナップショットです。更新されることはなく、OMSI を読み取ることもありません。

<a id="runtime-evidence"></a>
## ランタイムエビデンス

ランタイムクロージャのラウンドで、承認済みのインストール環境上の実際のセッションにおいて確認された内容です（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`、pt-BR の Windows UI。[ランタイム検証ステータス](../status/runtime-validation-status.md)を参照）。

- アイコンは `OmsiLaunchW.exe` の下で実際の通知領域（`Shell_TrayWnd`）に登録され、復元後に削除されます（`T01`..`T04`）。
- 確認付きの「End session」は正規停止と完全な復元を実行します。`Cancel` ではセッションが実行を続けます（`T01`、`T03`）。
- エクスプローラーの再起動後、アイコンは再作成されます（`TaskbarCreated`、`T01`）。
- ステータスウィンドウと確認ウィンドウが開いている間に停止が届くと、オーナーがそれらを閉じます（`T02`）。
- 引数エラー（`OL_E_INVALID_ARGUMENT`）、アクティブなセッションがない場合（`OL_E_NO_ACTIVE_SESSION`）、ゲームプレイ前に失敗したセッション（`OL_E_WORLD_START_FAILED`）に対する `OmsiLaunchW.exe` のエラーダイアログ（`T04`）。最後のケースでは、ダイアログはプラグインの失敗ペイロードをメッセージとして表示します。
- `/silent` はデタッチします。Windows ホストがセッションを保持したまま、ランチャーは制御を返します（`T04`）。
- コンソールのクローズ時の 4 s の `ProcessExit` 時間の上限（`L04`、コンソールオーナー）。

未検証: ブートストラッパー shim の終了コードダイアログ（`100`..`106`）。
