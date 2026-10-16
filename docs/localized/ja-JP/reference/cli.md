# CLI リファレンス

<!-- l10n: source=reference/cli.md source-sha256=a4f9167107ac29bcb98c94e8b0fe813fd55b891f9c2d8800d22a08d1d57e9f58 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/cli.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、OmsiLaunch `0.1.0-beta4` のコマンドラインに関する完全かつ規範的なリファレンスです。3 つの実行ファイル、引数の文法、ディスパッチ順序、すべてのコマンドワード、すべての階層ルート、すべてのフラグ、出力エンベロープ、および各コマンドのエラー時の動作を扱います。本ページは `tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Parse`、`CliInput.KnownFlags`、`CliInput.AcceptedNoEffectFlags`、`CliInput.CommandWordsAccepted`、`CliInput.HierarchicalRoutes`、`CliInput.BuildSpecAsync`、`CliEventWatch`）、`tools\OmsiLaunch.Cli\LaunchSpecJson.cs`、および `tools\OmsiLaunch.Bootstrapper` 配下の 2 つのネイティブシムから生成されています。プロセスの結果は[終了コード](exit-codes.md)に、エラーコードは[エラー](errors.md)に、実際の呼び出し例は [CLI の使用例](cli-examples.md)に記載しています。

<a id="executables"></a>
## 実行ファイル

| ファイル | サブシステム | 役割 | 相違点 |
|---|---|---|---|
| `OmsiLaunch.exe` | Console | ネイティブブートストラッパー（`OmsiLaunch.Bootstrapper.cpp`）です。自身のディレクトリを解決し、`CommandLineToArgvW` でコマンドラインをトークン化し、`nethost.dll` を介して `hostfxr` を探し、同じ引数で `OmsiLaunch.Controller.dll` を実行します。 | コンソール出力が書き込まれます。プロセスの終了コードはマネージドコントローラーの終了コードです。.NET ホストを開始できなかった場合はシムのコード `100`..`106` になります。 |
| `OmsiLaunchW.exe` | Windows (GUI) | Windows サブシステム向けにビルドされた同じシム（`OmsiLaunch.WindowsHost.cpp`）です。コントローラーを開始する前に環境変数 `OMSILAUNCH_WINDOWS_HOST=1` を設定します。 | コンソールはありません。`--json` が指定されない限りコンソール出力は抑止され（`WindowsHost.SuppressConsole`）、失敗はメッセージボックスで表示されます（`WindowsHost.ShowFailure`: メッセージ、`Code: OL_E_...`、およびヒント `See .omsilaunch\diagnostics for details.`）。シムの失敗 `100`..`106` は `OmsiLaunch could not start the .NET host (code N).` と表示されます。動作の全容: [OmsiLaunchW.exe リファレンス](omsilaunchw.md)。 |
| `OmsiLaunch.Controller.dll` | マネージド（x64、`net6.0-windows`、Windows Forms） | コントローラー本体です。利用者が直接呼び出すことはありません。両方のシムがコントローラーのパスを最初のホスト引数として渡すため、公開された引数リストには現れません。 | `Microsoft.WindowsDesktop.App` を含む x64 .NET 6 ランタイムが必要です。[インストール](../getting-started/installation.md)を参照してください。 |

`nethost.dll` はシムと同じ場所に置く必要があります。シム自身は引数を読み取りません。すべての引数は変更されずに `CliInput.Parse` に届くため、`OmsiLaunch.exe` と `OmsiLaunchW.exe` はまったく同じ構文を受け付けます。

<a id="invocation-model"></a>
## 呼び出しモデル

<a id="argument-grammar-cliinputparse"></a>
### 引数の文法（`CliInput.Parse`）

| 形式 | 意味 |
|---|---|
| `/key:value`、`/key`、`-key:value`、`-key` | フラグです。キーは大文字と小文字を区別しません。値は最初の `:` 以降のすべてです。未知のキーは `OL_E_INVALID_ARGUMENT`（`Unknown argument: ...`）で失敗し、終了コードは `2` です。 |
| `--key=value` | 選択されたランタイム操作に対するランタイム引数です（例: `--handle=rv-000001`）。`=` を含む `--` トークンはすべてランタイム引数であり、フラグになることはありません。 |
| `--json`、`/json` | 構造化出力です（[出力形式](#output-formats)を参照）。`=` を含まない `--` トークンで意味を持つのは `--json` だけで、`/json` フラグとして解析されます。 |
| 裸の単語 | まだコマンドワードが現れておらず、その単語が[コマンドワード](#command-words)のいずれかであれば、それがコマンドになります。コマンドワードが存在した後は、それ以降の裸の単語はすべてコマンドワード（ルート）になります。それ以外の場合、最初の裸の単語はインストールルートとなり、それ以降の裸の単語はルートに追加されます。 |

結果として、階層ルート（`time get`）の後ろにインストール引数を置いて組み合わせることはできません（`time get D:\OMSI` は未知のルート `time get d:\omsi` となり、終了コード `2`）。`D:\OMSI time get` は受け付けられますが、これは **オーナーモード** です（新しいセッションが開始され、その中で操作が 1 回実行されます）。解析エラー（`ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException`）とセッションプロファイルのエラー（`SessionProfileException`）は、何かが実行される前に報告され、終了コードは常に `2` です。

<a id="installation-root"></a>
### インストールルート

- 明示的な裸の単語によるインストール引数は、`/spec` ファイル内の `RootPath` より優先されます（`CliInput.BuildSpecAsync`）。
- `.` は実行ファイルを含むディレクトリ（`AppContext.BaseDirectory`）を意味し、呼び出し元の作業ディレクトリを意味することはありません（`CliInput.ResolveInstallationRoot`）。ポータブルパッケージはこの動作に依存しています。
- 引数が省略された場合、オーナーモードの操作（`/new`、`/saved`、`/spec`、`/list`、`/recovery-status`、`/recover`）も実行ファイルのディレクトリを使用します。パスは `Path.GetFullPath` で正規化されます。
- クライアントモードのコマンドはインストール引数を取りません。実行ファイルが置かれているインストール環境（`AppContext.BaseDirectory`）のローカル制御エンドポイントを対象とします。[ローカル制御](local-control.md)を参照してください。

<a id="owner-and-client"></a>
### オーナーとクライアント

- **オーナー**: セッションのプランを作成し、開始し、監視し、復元するプロセスです（`OwnerSession.RunAsync`）。インストールリース（`Local\OmsiLaunch.Installation.<sha256(root)>`）と設定トランザクションを保持し、セッションが生きている間はローカル制御エンドポイントを公開し、[トレイアイコン](windows-tray.md)を表示します。オーナーはインストール環境ごとに厳密に 1 つです。制御エンドポイントで既にオーナーが `session.status` に応答している場合、2 回目の起動は `OL_E_SESSION_ALREADY_ACTIVE`（終了コード `7`）で失敗します。
- **クライアント**: インストール引数なしで `session status`、`session stop`、`events read`、`events watch`、またはランタイム操作を送信する呼び出しです。ローカル制御パイプ経由で転送されます。オーナーが存在しない場合は `OL_E_NO_ACTIVE_SESSION`（終了コード `4`）で失敗します。

<a id="dispatch-order-cliprogramrunasync"></a>
### ディスパッチ順序（`CliProgram.RunAsync`）

1. `/silent`（`OmsiLaunchW.exe` の下で既に実行中でない場合）: 実行ファイルのディレクトリから `ShellExecute` を通じて（ハンドル継承なしで）`OmsiLaunchW.exe` を開始し、`/silent`/`--silent` を除いた同じ引数を渡し、`silent` エンベロープ（`delegated`、`host_process_id`）を書き込んで `0` を返します。コンソールプロセスはセッションを待機しません。[OmsiLaunchW.exe](omsilaunchw.md#silent-delegation) を参照してください。`OL_E_WINDOWS_HOST_MISSING` / `OL_E_WINDOWS_HOST_START_FAILED` は `7` を返します。
2. `/version`: `product`、`version`（アセンブリの informational version。`OmsiLaunch.Version.props` から刻印され、`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family`（`OMSI_2_3_004_COMMON`）を含むエンベロープ `version`。終了コード `0`。
3. `capabilities`: 正規カタログ `PublicCapabilityRegistry` のすべての `PublicStableBeta` または `PublicExperimental` 記述子（その操作、ネイティブケイパビリティ、エビデンス、注記を含む）を含むエンベロープ。終了コード `0`。
4. `help [family]`: `usage`、`product_version`、`protocol_version`、`family`、および公開 `commands`（`CliRoute`、`Description`、`Classification`、`RuntimeValidation`）を含むエンベロープ `help`。family で絞り込むこともできます。終了コード `0`。
5. `profiles`: `requirement`（`OMSI 2`）、対象ルートの `installation` レポート（`is_omsi2`、`reason`、`build_id`、`identity` の `known`/`unknown`、`file_sha256`、`file_version`、`code_sha256`、`file_variant`）、`known_layouts`、および実行ファイルから解決された `predicted_capabilities`（開始時のプロセス内解決が正となります）を含むエンベロープ。終了コード `0`。
6. クライアントのランタイム操作（インストール引数がなく、ルートまたは `/runtime:` がある場合）: 引数は `PublicCapabilityRegistry.ValidateRuntimeArguments` に照らして検証され（`OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`、終了コード `2`）、その後 `runtime.execute` が 8 s のタイムアウト（`road-vehicles.spawn` では 30 s）で転送されます。
7. クライアントの `session status`（750 ms）、`session stop`（アクティブなセッション id に束縛、750 ms）、`events read`（750 ms）、`events watch`（Ctrl+C まで 250 ms ごとにポーリング）。
8. `detect`、または **引数がまったくない場合**（インストール、コマンド、`/?`、`/spec`、起動フラグ、リカバリフラグ、`/list` のいずれもない）: `Omsi` プロセスを列挙し、制御エンドポイントを調べます（250 ms）。エンベロープ `detect`。終了コード `0`。
9. `/?` または `/help`: 使用方法のテキストを出力し、終了コード `0`。コマンドワードはあるがディスパッチ可能なルートがないその他の呼び出し（例: `d3d` 単独や `session status D:\OMSI`）は、使用方法のテキストを出力して終了コード `2` で終了します。
10. オーナーモード。前提条件: 実行ファイルと同じ場所に `plugins\OmsiLaunch.Plugin.opl` と `plugins\OmsiLaunch.Native.x86.dll` が存在する必要があります（`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、終了コード `7`）。実行ファイルと同じ場所に `release-manifest.json` がある場合は、それが期待されるプラグインハッシュを提供します。
11. `/recovery-status` / `/recover`: `RecoverPendingAsync`。`pending`、`recovered`、`diagnostics` を含むエンベロープ `recover`。復元が要求されたのに完了しなかった場合にのみ終了コード `8`、それ以外は `0`。
12. `/list:<category>`: `DiscoverAsync`。エンベロープ `content.list`。終了コード `0`。`/list:SessionProfiles` は代わりに `ListSessionProfilesAsync` を使用し、エンベロープは `session-profiles.list` です。
13. `LaunchSpec` を構築し（`BuildSpecAsync`）、プランを作成し（`PlanSessionAsync`）、プランを出力します。`/plan` または `/validate`: `IsRunnable` なら終了コード `0`、そうでなければ `1`。実行不可のプランが OMSI を起動することはありません（終了コード `1`）。`OmsiLaunchW.exe` の下では、実行不可のプランでの起動は、その最後の `OL_E_` 診断をメッセージボックスで表示します（ドキュメント監査 BUG-06）。プランニングでは、インストール済みの常駐プラグイン一式を `release-manifest.json` と照合する検証も行うため、プラグインが欠落または改変されているとプランは実行不可になります（`OL_E_PERMANENT_PLUGIN_*`）。
14. 既存のオーナーの有無を調べ（`OL_E_SESSION_ALREADY_ACTIVE`、終了コード `7`）、その後 `OwnerSession.RunAsync` を実行します。

<a id="owner-lifecycle-ownersessionrunasync"></a>
### オーナーのライフサイクル（`OwnerSession.RunAsync`）

1. `StartSessionAsync(plan)`。ここから先は、すべての終了経路が `finally` ブロック内の `CloseAsync` に到達します。例外、Ctrl+C（`Console.CancelKeyPress`）、コンソールのクローズ / ログオフ（停止 + 復元に 4 s の猶予を持つ `AppDomain.ProcessExit`。残ったものは次回の開始時にジャーナルによってリカバリされます）、トレイの "End session"、パイプの `session.stop`、および `/observe-seconds` がこれに含まれます。
2. spec で `Presentation.SuppressTrayIcon` が設定されていない限り、トレイアイコンが作成されます。
3. `StartupTimeoutSeconds + 5` 秒間 `Running` を待機します。ステータスが出力されます。状態が `Running` でない場合は終了コード `1` です（`OmsiLaunchW.exe` は、最後の `OL_E_` 診断または `OL_E_SESSION_START_FAILED` とともに `The OMSI session did not reach gameplay.` を表示します）。
4. 検証バッチ（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`）が実行され、成果物を書き込みます。
5. ローカル制御エンドポイントが開始されます。
6. `/runtime:<operation>` が 1 回実行されます（5 s、`road-vehicles.spawn` では 15 s）。結果は `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` に書き込まれ、出力されます。ランタイムコマンドが失敗してもセッションが終了することはありません（代わりに `runtime_error` が出力されます）。
7. 待機: `/observe-seconds:n` を指定した場合、セッションは `n` 秒後に停止されます。**または**、それより前にトレイ/パイプからの停止要求があった場合、あるいは OMSI が終了した場合に停止されます。指定しない場合、オーナーは OMSI が終了するか停止が要求されるまで待機します。
8. 完了時のステータスが出力されます。`Completed` なら終了コード `0`、それ以外は `1` です。

`session.stop`、トレイの "End session"、Ctrl+C、および `CloseAsync` はすべて正規の停止を要求します。OMSI は `TerminateProcess` で終了させられ（OMSI 自身のシャットダウン処理は実行されず、`options.cfg` が OMSI によって書き換えられることはありません）、その後セッションが所有するすべてのファイルが復元されます。[セッションのライフサイクル](../concepts/session-lifecycle.md)と[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照してください。

<a id="command-words"></a>
## コマンドワード

先頭の位置で受け付けられるすべての単語（`CliInput.CommandWordsAccepted`）:

| 単語 | 目的 | モード | 注記 |
|---|---|---|---|
| `capabilities` | 公開ケイパビリティを一覧表示 | ローカル、セッション不要 | エンベロープ `capabilities`。 |
| `profiles` | サポートされる `Omsi.exe` バリアントを一覧表示 | ローカル、セッション不要 | エンベロープ `profiles`。 |
| `detect` | `Omsi.exe` プロセスとアクティブなオーナーを報告 | ローカル、セッション不要 | 引数が指定されない場合のデフォルトでもあります。状態: `NO_OMSI_FOUND`、`OMSI_FOUND_UNMANAGED`、バイナリを検査できない場合のプロセスごとの `UNKNOWN_BINARY_FOUND`、`active_omsilaunch_instance`、`managed_session`。 |
| `help` | 使用方法と公開コマンドカタログ | ローカル、セッション不要 | `help <family>` はケイパビリティファミリー（`session`、`time`、`weather`、`map`、`camera`、`vehicles`、`player`、`humans`、`timetable`、`scripts`、`constants`、`curves`、`hof`、`drivers`、`tickets`、`d3d`、`events`）で絞り込みます。 |
| `session` | `session status`、`session stop` | クライアント | 後続の単語はちょうど 1 つです。それ以外は使用方法を出力し、終了コード `2`。`session plan`/`session start` は API のルート名であり、CLI の単語ではありません。`/plan` と `/new` を使用してください。 |
| `events` | `events read`、`events watch` | クライアント | `read` は上限付きのイベントリストを 1 回返します。`watch` は新しいイベントごとに（`Sequence` に基づき）`events.watch` エンベロープとして 250 ms ごとに Ctrl+C まで出力し（終了コード `0`）、オーナーが応答しない場合は `4`、制御エラーの場合は `7` です。 |
| `time` | `time get`、`time set` | クライアントルート | |
| `weather` | `weather get`、`weather set`、`weather metar set`、`weather actual get` | クライアントルート | |
| `map` | `map get` | クライアントルート | |
| `camera` | `camera get`、`camera set`、`camera lock`、`camera unlock` | クライアントルート | |
| `vehicles` | `vehicles list`、`vehicles get`、`vehicles summary`、`vehicles spawn`、`vehicles place-random` | クライアントルート | |
| `player` | `player get` | クライアントルート | |
| `humans` | `humans list`、`humans get`、`humans summary` | クライアントルート | |
| `timetable` | `timetable get`、`timetable <table> list`、`timetable logs list` | クライアントルート | |
| `scripts` | `scripts variable list|get|set`、`scripts string list|get|set`、`scripts trigger`、`scripts sound-trigger` | クライアントルート | |
| `constants` | `constants list`、`constants get` | クライアントルート | |
| `curves` | `curves list`、`curves evaluate` | クライアントルート | |
| `hof` | `hof get` | クライアントルート | |
| `drivers` | `drivers list` | クライアントルート | |
| `tickets` | `tickets get` | クライアントルート | |
| `d3d` | 予約済みのファミリーワード | なし | `d3d` には **階層ルートがありません**。`d3d texture ...` は未知のルート（終了コード `2`）であり、`d3d` 単独では使用方法を出力します（終了コード `2`）。D3D 操作には `/runtime:d3d.status`、`/runtime:d3d.texture.create` などでアクセスします（[ルートのない操作](#operations-without-a-route)を参照）。 |

<a id="hierarchical-routes"></a>
## 階層ルート

`CliInput.HierarchicalRoutes` は、小文字化されたルートをランタイム操作 id に対応付けます。すべてのルートは `Running` セッションを必要とし、ランタイムメールボックス（`ExecuteRuntimeAsync`）を通じて実行されます。ランタイムの書き込みは OMSI のメモリ上の状態のみを変更します。ファイルには一切触れず、設定トランザクションの一部でもなく、停止時に元に戻されることは **ありません**（OMSI は強制終了されます）。安定性は `PublicCapabilityRegistry` と[検証マトリクス](../status/runtime-validation-status.md)に従います。詳細と結果フィールドは[ランタイム制御](runtime-control.md)にあります。

| ルート | ランタイム操作 | 種別 | Running が必要 | OMSI を変更 | 復元への関与 | 安定性 | 注記 |
|---|---|---|---|---|---|---|---|
| `time get` | `time.read` | Read | はい | いいえ | なし | STABLE_BETA | 時計とカレンダーのフィールド。 |
| `time set` | `time.set` | Write | はい | はい（メモリ上の時計） | なし、元に戻されない | EXPERIMENTAL | 例: `--minute=<0..59>`。書き込み、読み戻し、復元は 2026-09-20 に検証済み。 |
| `weather get` | `weather.read` | Read | はい | いいえ | なし | STABLE_BETA | |
| `weather set` | `weather.set` | Write | はい | はい（メモリ内の天候） | なし、元に戻されない | EXPERIMENTAL | 例: `--wind_speed=5 --cloud_type="Cumulus 1"`。手動天候: ライブ METAR の更新は停止します。ランタイム検証は保留中です。 |
| `weather metar set` | `weather.metar.set` | Write | はい | はい（メモリ内の天候。OMSI は ICAO を `options.cfg` に保存し、セッションがそれを復元します） | なし、元に戻されない | EXPERIMENTAL | `--icao=<ICAO>`。OMSI の天候ダイアログが設定するのと同じライブ METAR 天候です。ランタイム検証は保留中です。 |
| `weather actual get` | `weather.actual.read` | Read | はい | いいえ | なし | EXPERIMENTAL | 実天候/ICAO コントローラーの状態。 |
| `map get` | `map.read` | Read | はい | いいえ | なし | STABLE_BETA | マップ名、ファイル、説明、タイル数、年の範囲、通行区分。修正後のマップスロットでランタイム再検証済み。 |
| `camera get` | `camera.read` | Read | はい | いいえ | なし | STABLE_BETA | |
| `camera set` | `camera.set` | Write | はい | はい（カメラのスカラー値。例: `--field_of_view=`） | なし、元に戻されない | EXPERIMENTAL | FOV の書き込み/読み戻しは検証済み。 |
| `camera lock` | `camera.lock` | Action | はい | はい（セッションスコープのポリシー） | なし | EXPERIMENTAL | `--family=<0..3>`（運転席=0、乗客=1、外部=2、マップ=3）が必須、`--preset=<n>` は任意（family 0 または 1）。プレイヤー車両が必要です（例: 保存済みシチュエーション）。ランタイムクロージャ（`CAM01`）でランタイム検証済みですが、レジストリの `RuntimeValidation` 文字列は依然として `STATICALLY_VALIDATED` のままです（[ケイパビリティ](capabilities.md)を参照）。 |
| `camera unlock` | `camera.unlock` | Action | はい | はい | なし | EXPERIMENTAL | `camera lock` で設定したポリシーを解除します（`CAM01`）。 |
| `vehicles list` | `road-vehicles.list` | Read | はい | いいえ | なし | STABLE_BETA | セッションスコープの `rv-NNNNNN` ハンドルを返します。 |
| `vehicles get` | `road-vehicle.read` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=` が必須。失効したハンドル: `OL_E_RUNTIME_OBJECT_HANDLE_STALE`。 |
| `vehicles summary` | `road-vehicles.read` | Read | はい | いいえ | なし | STABLE_BETA | 件数とプレイヤーの状態。ハンドルは含みません。 |
| `vehicles spawn` | `road-vehicles.spawn` | Action | はい | はい（RoadVehicle を 1 台追加） | なし、削除されない | EXPERIMENTAL | `--model=Vehicles\...\*.bus` が必須。クライアントのタイムアウト 30 s、オーナーのタイムアウト 15 s。プレイヤー車両は割り当てません。RV-003 `RUNTIME_VALIDATED`。 |
| `vehicles place-random` | `road-vehicles.place-random` | Action | はい | はい | なし | EXPERIMENTAL | プロファイル済みの `PlaceRandomBus`。 |
| `player get` | `player-vehicle.read` | Read | はい | いいえ | なし | STABLE_BETA | プレイヤー車両がない場合はセマンティックな null。 |
| `humans list` | `humans.list` | Read | はい | いいえ | なし | EXPERIMENTAL | `hb-NNNNNN` ハンドルを返します。 |
| `humans get` | `human.read` | Read | はい | いいえ | なし | EXPERIMENTAL | `--handle=` が必須。 |
| `humans summary` | `humans.read` | Read | はい | いいえ | なし | EXPERIMENTAL | 件数のみ。 |
| `timetable get` | `timetable.read` | Read | はい | いいえ | なし | STABLE_BETA | 時刻表マネージャーの状態。 |
| `timetable tracks list` | `timetable.tracks.list` | Read | はい | いいえ | なし | STABLE_BETA | `timetable.read` ケイパビリティの一部。2026-09-20 のバッチ読み取りエビデンス。 |
| `timetable trips list` | `timetable.trips.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable lines list` | `timetable.lines.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable tours list` | `timetable.tours.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable profiles list` | `timetable.profiles.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable bus-stops list` | `timetable.bus-stops.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable station-links list` | `timetable.station-links.list` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable logs list` | `timetable.logs.read` | Read | はい | いいえ | なし | STABLE_BETA | 同上。 |
| `timetable no-rv-numbers list` | `timetable.no-rv-numbers.list` | Read | はい | いいえ | なし | EXPERIMENTAL | OMSI が自由な AI の使用から除外している車両番号（時刻表ツアーによって予約されている、または固定の AI リストにあるもの）。ページング対象（`cursor`、`limit`、1 ページあたり最大 256）。 |
| `map tiles list` | `map.tiles.list` | Read | はい | いいえ | なし | EXPERIMENTAL | マップのタイル: グリッド位置、自身のインデックス、ファイルとロード状態、オブジェクト・パス・スプラインの数。ページング対象（`cursor`、`limit`、1 ページあたり最大 256）。 |
| `drivers list` | `drivers.read` | Read | はい | いいえ | なし | EXPERIMENTAL | 運転士のレコード。 |
| `tickets get` | `tickets.read` | Read | はい | いいえ | なし | EXPERIMENTAL | 乗車券パックのレコード。 |
| `hof get` | `vehicle.hofs.read` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=` が必須。 |
| `constants list` | `vehicle.constants.list` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=` が必須。 |
| `constants get` | `vehicle.constant.get` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=`、`--name=` が必須。 |
| `curves list` | `vehicle.curves.list` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=` が必須。 |
| `curves evaluate` | `vehicle.curve.evaluate` | Read | はい | いいえ | なし | STABLE_BETA | `--handle=`、`--name=`、`--x=` が必須。 |
| `scripts variable list` | `vehicle.variables.list` | Read | はい | いいえ | なし | EXPERIMENTAL | `--handle=` が必須。 |
| `scripts variable get` | `vehicle.variable.get` | Read | はい | いいえ | なし | EXPERIMENTAL | `--handle=`、`--name=` が必須。 |
| `scripts variable set` | `vehicle.variable.set` | Write | はい | はい（スクリプト変数） | なし、元に戻されない | EXPERIMENTAL | `--handle=`、`--name=`、`--value=`（有限の数値）が必須。 |
| `scripts string list` | `vehicle.string-variables.list` | Read | はい | いいえ | なし | EXPERIMENTAL | `--handle=` が必須。 |
| `scripts string get` | `vehicle.string-variable.get` | Read | はい | いいえ | なし | EXPERIMENTAL | `--handle=`、`--name=` が必須。 |
| `scripts string set` | `vehicle.string-variable.set` | Write | はい | はい（メモリ内のスクリプト状態） | なし、元に戻されない | EXPERIMENTAL | `--handle=`、`--name=`、`--value=` が必須。OMSI 自身の `SetStringVariable`。ランタイム検証は保留中です。 |
| `scripts trigger` | `vehicle.trigger` | Action | はい | はい（車両スクリプトのコードを実行） | なし、元に戻されない | EXPERIMENTAL | `--handle=`、`--name=`、`--value=`（`1` 押下、`0` 解放、`2` ドラッグ）が必須。ランタイム検証は保留中です。 |
| `scripts sound-trigger` | `vehicle.sound-trigger` | Action | はい | はい（車両のサウンドを再生） | なし、元に戻されない | EXPERIMENTAL | `--handle=`、`--trigger=` が必須。`--file=` は任意。ランタイム検証は保留中です。 |

<a id="operations-without-a-route"></a>
### ルートのない操作

以下の公開操作 id（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）には階層ルートがなく、`/runtime:<operation>` に `--key=value` または `/runtime-arg:key=value` を付けて呼び出します: `timetable.rv-files.list`、`timetable.track-entries.list`、`timetable.tour-entries.list`、`d3d.status`、`d3d.texture.create`（`width`、`height`、`format` が必須、`levels` は任意）、`d3d.texture.describe`（`handle`、`level` は任意）、`d3d.texture.update`（`handle`、`width`、`height`、`pixels_base64` が必須、`level`、`x`、`y` は任意）、`d3d.texture.release`（`handle`）。D3D 操作は EXPERIMENTAL です。テクスチャのライフサイクルとデバイスリセットによる無効化はランタイム検証済みです（ランタイムクロージャ `H02`、`D01`。[ケイパビリティ](capabilities.md)を参照）。`timetable.track-entries.list` と `timetable.tour-entries.list` は上限付きリストです。ランタイムスロットに収まらない結果は短縮されます（`truncated=true`）。`internal.road-vehicles.make-basic` は INTERNAL であり、CLI と API の両方で `OL_E_RUNTIME_OPERATION_UNKNOWN` により拒否されます。

<a id="flags"></a>
## フラグ

`CliInput.KnownFlags` のすべてのフラグです。「フェーズ」は *launch-time*（新しいセッションの `LaunchSpec`/プランを形作る）、*runtime*（実行中のセッションに作用する）、*control*（CLI 自体の動作を変える）のいずれかです。互換性のためだけに解析されるフラグ（`CliInput.AcceptedNoEffectFlags`）は、その行に明記しています。

<a id="control-and-output"></a>
### 制御と出力

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/?` | `/?` | オフ | control | STABLE_BETA | 使用方法のテキストを出力し、終了コード `0`。 |
| `/help` | `/help` | オフ | control | STABLE_BETA | `/?` と同じです。（裸の単語 `help` は代わりに構造化されたカタログを返します。） |
| `/version` | `/version` | オフ | control | STABLE_BETA | `version` エンベロープ、終了コード `0`。`/silent` を除く他のすべてのコマンドより先に評価されます。 |
| `/json` | `/json` または `--json` | オフ | control | STABLE_BETA | JSON エンベロープを出力します。`OmsiLaunchW.exe` の下でもコンソール出力を強制します。 |
| `/quiet` | `/quiet` | オフ | control | STABLE_BETA | 人間が読む成功時の出力を抑制します。JSON 出力（`/json`）とエラーは引き続き書き出され、終了コードは変わりません。 |
| `/silent` | `/silent`（`--silent` も可） | オフ | control | EXPERIMENTAL | コマンドライン全体を `OmsiLaunchW.exe` に委譲し、ホストプロセスが開始された時点で `0` を返します。セッションの結果は `OmsiLaunchW.exe`（メッセージボックス、トレイアイコン）、`.omsilaunch\diagnostics`、およびローカル制御エンドポイントによって報告されます。委譲とエラーダイアログはランタイム検証済みです（ランタイムクロージャ `T04`）。[OmsiLaunchW.exe](omsilaunchw.md) を参照してください。 |
| `/serve` | `/serve` | — | control | REMOVED | 0.1.0-beta4 で削除されました。`OL_E_INVALID_ARGUMENT`（終了コード `2`）で拒否されます。セッションオーナーは常にローカル制御エンドポイントを提供します。 |
| `/verbose` | `/verbose` | オフ | launch-time | PARTIAL | `DiagnosticsSpec.Verbose`。値は spec に保持されますが、その効果は `.omsilaunch\diagnostics` 配下のホストトレースに限られます。 |
| `/log` | `/log` | オン（`DiagnosticsSpec.Log` のデフォルトは `true`） | launch-time | PARTIAL | `DiagnosticsSpec.Log`。事実上常にオンです。 |
| `/logall` | `/logall` | オフ | launch-time | PARTIAL | `Verbose`、`ProcessTrace`、`PluginTrace`、`NativeTrace` をまとめて設定します。 |
| `/omsi-logall` | `/omsi-logall` | オフ | launch-time | PARTIAL | `DiagnosticsSpec.OmsiLogAll`。 |
| `/trace` | `/trace` | オフ | launch-time | PARTIAL | `/trace-process` の別名です。 |
| `/trace-process` | `/trace-process` | オフ | launch-time | PARTIAL | `DiagnosticsSpec.ProcessTrace`。 |
| `/trace-plugin` | `/trace-plugin` | オフ | launch-time | EXPERIMENTAL | `DiagnosticsSpec.PluginTrace`: プラグインはすべてのランタイム操作（`plugin.trace.operation`: 操作、結果、コード、所要時間）をセッションのホストトレースに報告します。 |
| `/trace-native` | `/trace-native` | オフ | launch-time | EXPERIMENTAL | `DiagnosticsSpec.NativeTrace`: ネイティブレイヤーはすべてのフックの設置と解除（`native.trace`）をセッションのホストトレースに報告します。 |

<a id="planning-validation-and-harnesses"></a>
### プランニング、検証、ハーネス

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/plan` | `/plan` | オフ | launch-time | STABLE_BETA | `SessionPlan` を構築して出力し、OMSI は起動しません。`IsRunnable` なら終了コード `0`、それ以外は `1`。起動対象の選択（`/new`、`/saved`、`/spec`、またはインストール引数）が必要です。他に何も指定しない `/plan` 単独では `detect` が実行されます。 |
| `/validate` | `/validate` | オフ | launch-time | STABLE_BETA | このビルドでは `/plan` と同一です。 |
| `/runtime-batch` | `/runtime-batch` | オフ | runtime（オーナー） | INTERNAL | 検証ハーネス: `Running` の後、読み取り操作のセットを実行し、`<sessionId>-runtime-read-batch.json` を書き込みます。 |
| `/runtime-write-batch` | `/runtime-write-batch` | オフ | runtime（オーナー） | INTERNAL | 検証ハーネス: 読み取りに加えて、復元付きで `time.set`、`camera.set`、`vehicle.variable.set` を実行し、`<sessionId>-runtime-write-batch.json` を書き込みます。 |
| `/d3d-batch` | `/d3d-batch` | オフ | runtime（オーナー） | INTERNAL | D3D テクスチャのライフサイクル用の検証ハーネスです。`<sessionId>-d3d-wave-d-batch.json` を書き込みます。 |
| `/runtime` | `/runtime:<operation>` | なし | runtime | STABLE_BETA（ディスパッチ） | 公開ランタイム操作を id で選択します。クライアントモード（インストール引数なし）: オーナーに転送されます。オーナーモード: `Running` の後に 1 回実行されます。未知の id: `OL_E_RUNTIME_OPERATION_UNKNOWN`、終了コード `2`。 |
| `/runtime-arg` | `/runtime-arg:<key>=<value>`（繰り返し可） | なし | runtime | STABLE_BETA（ディスパッチ） | ランタイム引数です。`--key=value` と同等です。`=` がない場合: `/runtime-arg requires key=value`、終了コード `2`。 |

<a id="world-selection"></a>
### ワールドの選択

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/new` | `/new` | `WorldMode.NewMap` がデフォルトのモードですが、起動が要求されるのは `/new`、`/saved`、`/last`、`/spec` のいずれかが指定された場合のみです | launch-time | STABLE_BETA | NEW_MAP。`/map` と `/entrypoint-index` が必須です（表示上のエントリポイントインデックスがないプランは `OL_E_ENTRYPOINT_REQUIRED` を報告し、`/map` がない場合はマップが解決されません）。`/new` が暗黙のうちにマップを選択することはありません。 |
| `/saved` | `/saved:<file.osn>` | なし | launch-time | STABLE_BETA | SAVED_SITUATION。マップと位置は `.osn` から取得されます。`/saved` と一緒に指定した `/map`、`/entrypoint`、`/entrypoint-index` は拒否されます（終了コード `2`）。シチュエーションがない場合: `OL_E_SITUATION_NOT_FOUND`。そのマップがない場合: `OL_E_SITUATION_MAP_NOT_FOUND`。 |
| `/last` | `/last /map:<identity>` | なし | launch-time | EXPERIMENTAL | LAST_MAP_STATE: マップ自身の `laststn.osn`（OMSI がマップを閉じたときに書き出すもの）を、Start フォームの last-state 分岐を通じて読み込みます。`/map` が必須で、`laststn.osn` のないマップは `OL_E_SITUATION_NOT_FOUND` です。最新の `.osn` を探すヒューリスティックは決して使いません。ランタイム検証は保留中です。 |
| `/map` | `/map:<identity>`（例: `maps\Grundorf\global.cfg`） | なし | launch-time | STABLE_BETA | `/new` 用のマップ識別子、または `/list:Entrypoints` のスコープです。未知の場合: `OL_E_MAP_NOT_FOUND`。 |
| `/entrypoint` | `/entrypoint:<identity>` | なし | launch-time | EXPERIMENTAL | 正確な（大文字と小文字を区別する）ラベル、または `/list:entrypoints` の識別子によるエントリポイント指定です。OMSI は各ラベルの最初のレコードだけを提示します。同じラベルを共有する後続のレコードは拒否され（`OL_E_ENTRYPOINT_NOT_PRESENTED`）、未知のものも拒否されます（`OL_E_ENTRYPOINT_NOT_FOUND`）。ランタイム検証は保留中です。`/entrypoint-index` とは排他です（識別子が優先され、インデックスはクリアされます）。 |
| `/entrypoint-index` | `/entrypoint-index:<n>`、`0..2147483647` | なし | launch-time | STABLE_BETA | エントリポイントの表示リスト上のインデックスです（OMSI が表示するとおり 1 始まり）。実行可能な NEW_MAP プランには必須です。 |

<a id="date-time-and-weather"></a>
### 日付、時刻、天候

これらのフラグは受け付けられ、`LaunchSpec` に保持されます。開始日・時刻・年は NEW_MAP セッションにのみ適用されます。`/saved` または `/last` の場合、プランナーはこれらを `UNAVAILABLE` として記録し、**さらに `OL_E_CAPABILITY_UNAVAILABLE` を追加するため、プランは NOT RUNNABLE（実行不可）になります（終了コード `1`）**。保存済みシチュエーションは独自の時計を保持するためです。天候はすべてのワールドモードに適用されます。これらはすべて `EXPERIMENTAL` です。ネイティブの開始経路は静的に確定済みで、ランタイム検証は保留中です。これらを設定する `/spec` ファイルやセッションプロファイルも同じ結果になります。

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/date` | `/date:<yyyy-mm-dd>` または `/date:system` | 未設定 | launch-time | EXPERIMENTAL | `DateSpec` の明示指定/システム: NEW_MAP セッションの開始日です（[launchspec](launchspec.md#datespec-timespec-yearspec)を参照）。解析できない値: `OL_E_INVALID_ARGUMENT`、終了コード `2`。不可能な日付: `OL_E_DATE_TIME_APPLY_FAILED`。 |
| `/time` | `/time:<hh:mm[:ss]>` または `/time:system` | 未設定 | launch-time | EXPERIMENTAL | `TimeSpec` の明示指定/システム: NEW_MAP セッションの開始時刻です。 |
| `/year` | `/year:<n>` または `/year:system` | 未設定 | launch-time | EXPERIMENTAL | `YearSpec`: NEW_MAP セッションの開始年です（1900..2200）。 |
| `/weather` | `/weather:<preset>` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.Preset`: インストール環境の `weather` フォルダー内のファイルを、名前（`Regen`）またはパス（`weather\Regen.owt`）で指定します。存在しないものはプラン作成時に `OL_E_WEATHER_INVALID` になります。ワールドの開始直後に OMSI 自身の `loadWeather` で読み込まれ、ライブ METAR はオフのままです。ランタイム検証は保留中です。 |
| `/weather-icao` | `/weather-icao:<code>` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.Icao`: 4 文字の ICAO 観測所のライブ METAR 天候です（それ以外は `OL_E_WEATHER_INVALID`）。OMSI は観測所を `options.cfg` に保存し、セッションがそれを復元します。ランタイム検証は保留中です。 |
| `/weather-real` | `/weather-real` | 未設定 | launch-time | EXPERIMENTAL | `WeatherMode.RealCurrent`: OMSI にすでに設定されている観測所のライブ METAR 天候です。設定がない場合、開始は `OL_E_WEATHER_APPLY_FAILED` で失敗します。`/weather`、`/weather-icao`、`/weather-real` のうち最後に指定したものが優先されます。開始時の天候は、保存済みシチュエーションの天候より優先されます。 |

<a id="player-vehicle"></a>
### プレイヤー車両

受け付けられ、インストール環境に照らして解決されますが、ランタイムでは適用されません。設定された各フィールドは `UNAVAILABLE` となり、`OL_E_CAPABILITY_UNAVAILABLE` を追加します（プランは NOT RUNNABLE、終了コード `1`）。

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/vehicle` | `/vehicle:<identity>`（`Vehicles\...\*.bus`） | 未設定 | launch-time | UNAVAILABLE | 最初に解決されます（未知の場合は `OL_E_VEHICLE_NOT_FOUND`）。 |
| `/repaint` | `/repaint:<id>` | 未設定 | launch-time | UNAVAILABLE | `/vehicle` と一緒に指定した場合にのみ解決されます（`OL_E_REPAINT_NOT_FOUND`）。 |
| `/hof` | `/hof:<id>` | 未設定 | launch-time | UNAVAILABLE | 未知の場合は `OL_E_HOF_NOT_FOUND`。 |
| `/hof-source` | `/hof-source:<collection\|directory\|vehicle\|file>:<value>` | なし | launch-time | STABLE_BETA | セッションの [HOF 管理](hof-management.md)操作 1 件: `HofSourceKind` と `Source`。少なくとも 1 つの `/hof-target` が必須です。`/spec` の `HofManagement` を置き換えます。`hofs` を持つセッションプロファイルのプリセットとの併用: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。不正な形式: 終了コード `2`。 |
| `/hof-target` | `/hof-target:<vehicle identity or folder>`（繰り返し可） | なし | launch-time | STABLE_BETA | `Vehicles\` 配下のターゲット車両です。`/hof-source` がない場合: 終了コード `2`。 |
| `/hof-file` | `/hof-file:<name.hof>`（繰り返し可） | すべてのファイル | launch-time | STABLE_BETA | ソースをこれらのファイル名に限定します。`/hof-source` がない場合: 終了コード `2`。 |
| `/hof-mode` | `/hof-mode:add\|mirror` | `add` | launch-time | STABLE_BETA | `HofApplyMode`。`/hof-source` がない場合: 終了コード `2`。 |
| `/module` | `/module:<id>`（繰り返し可） | なし | launch-time | EXPERIMENTAL | セッションに[ランタイムモジュール](../../../reference/runtime-modules.md)を要求します（`LaunchSpec.Modules.Enable`。`/spec` ファイルのモジュールに追加されます）。読み込めない要求済みモジュールがあると、プランは実行不可となり、その `OL_E_MODULE_*` コードが返ります（終了コード `1`）。モジュールの操作は `/runtime:module.<id>.<operation>` と `/runtime-arg` で呼び出します。当該モジュールに `enabled: false` を設定するセッションプロファイルのプリセットとの併用: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |
| `/no-module` | `/no-module:<id>`（繰り返し可） | なし | launch-time | EXPERIMENTAL | 既定、そのマップ、または設定ファイルによって読み込まれるモジュールを含め、モジュールをセッションから除外します（`LaunchSpec.Modules.Disable`）。当該モジュールに `enabled: true` を設定するプリセットとの併用: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。値がない場合: 終了コード `2`。 |
| `/module-setting` | `/module-setting:<module id>.<key>=<value>`（繰り返し可） | なし | launch-time | EXPERIMENTAL | モジュールの設定を指定します（`LaunchSpec.Modules.Settings`）。`/spec` ファイル、設定ファイル、既定値より優先されます。選択したプリセットも設定しているキー: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。`<id>.<key>=<value>` の形式でない場合: 終了コード `2`。無効な値または宣言されていないキー: プラン診断 `OL_E_MODULE_SETTING_INVALID`（終了コード `1`）。 |
| `/<module flag>` | `/<flag>:<value>`。ブール設定では `/<flag>` のみで `true` を意味します | なし | launch-time | EXPERIMENTAL | インストール済みモジュールが自身の設定の 1 つに対して宣言するフラグです（マニフェストの `flag`。`/list:RuntimeModules` で一覧されます）。セッションのプラン作成時に解決され、`/module-setting:<id>.<key>=<value>` と同じ意味になります。どのインストール済みモジュールも宣言していないフラグ、または起動以外で使われたフラグ: `Unknown argument`、終了コード `2`。2 つのモジュールが宣言したフラグ: `OL_E_MODULE_SETTING_INVALID`、終了コード `2`。モジュールのフラグが組み込みフラグを置き換えることはありません。 |
| `/fleet` | `/fleet:<n>` | 未設定 | launch-time | UNAVAILABLE | 車両番号。 |
| `/registration` | `/registration:<text>` | 未設定 | launch-time | UNAVAILABLE | 登録番号。 |
| `/no-vehicle` | `/no-vehicle` | オフ | launch-time | STABLE_BETA | シード（`/spec` またはプロファイル）からプレイヤー車両をすべてクリアします。無害です。 |

<a id="configuration-overlays"></a>
### 設定オーバーレイ

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/set` | `/set:<key>=<value>`（繰り返し可、キーは大文字と小文字を区別しない） | なし | launch-time | STABLE_BETA | `ConfigurationCatalog` に基づく、セマンティックな `options.cfg` オーバーレイです（例: `graphics.maxFPS=60`、`traffic.randomVehicles=150`）。未知のキー: `OL_E_UNKNOWN_SETTING`（終了コード `2`）。読み取り専用のキー（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）: `OL_E_SETTING_NOT_WRITABLE`（終了コード `2`）。範囲外または不正な形式の値: オーバーレイの構築時に `OL_E_INVALID_SETTING_VALUE`。オーバーレイはセッションによる変更です。スナップショットが取られ、OMSI の起動前に適用され、停止時にバイト単位で正確に復元されます（RV-005 `RUNTIME_VALIDATED`）。選択したプロファイルのプリセットが所有するキーと競合する場合: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。 |

<a id="splash-presentation"></a>
### スプラッシュの表示

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/splash` | `/splash:Managed`、`/splash:Native`、`/splash:Unset`（大文字と小文字を区別しない） | `Managed` | launch-time | STABLE_BETA | `Managed`: パッケージに同梱された 640x480 24 ビットの BMP が `<root>\.omsilaunch\assets\splash` に一度だけコピーされ、`GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_<lang>.bmp` がトランザクションとしてオーバーレイされ、正確に復元されます（RV-006 `RUNTIME_VALIDATED`）。`Native`/`Unset`（別名）: OMSI のファイルには触れません。値がない場合: `/splash requires Unset, Native, or Managed`、終了コード `2`。 |
| `/splash-language` | `/splash-language:PTB|ENG|DEU|FRA`（`pt-BR`、`de`、`fr`、`en` も可。それ以外はすべて `ENG` にフォールバック） | `options.cfg` の `[language]`、なければ `ENG` | launch-time | STABLE_BETA | ローカライズされた対象ファイルを選択します。 |
| `/splash-assets` | `/splash-assets:<directory>`（相対パスはインストールルート配下として解決） | `<root>\.omsilaunch\assets\splash`、なければパッケージ同梱のセット | launch-time | STABLE_BETA | カスタムのアセットディレクトリです。`ENG.bmp` と、英語以外の言語の場合は `<lang>.bmp` を含む必要があります。エラー: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`（プランでは `OL_E_SESSION_PRESENTATION_INVALID` として表面化し、実行不可）。 |

### Internet Textures

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/internet-textures` | `/internet-textures:Native|Disabled|Override` | `Native` | launch-time | EXPERIMENTAL | `Native`: 変更しません。`Disabled`: プロファイル済みのプロセス内ダウンローダーが抑止されます。`Override`: 指定した `.itx` プロファイルが `Texture\standard.itx` としてオーバーレイされます。そこに列挙されたすべての HTTP(S) ターゲットと `Texture\standard.ipr` は、セッション削除（セッション中は削除され、停止時に復元）になります。値がない場合: 終了コード `2`。 |
| `/internet-textures-profile` | `/internet-textures-profile:<file.itx>` | なし | launch-time | EXPERIMENTAL | `Override` の場合は必須です（`OL_E_ITX_PROFILE_REQUIRED`、終了コード `2`）。`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`（`http`/`https` URL を持つ URL/ターゲット行のペアである必要があります）、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`（ターゲットは `Texture\` 配下に解決される必要があり、ルート付きパス、`..`、リパースポイントは不可）。 |

<a id="session-profiles"></a>
### セッションプロファイル

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/predefined-profile` | `/predefined-profile:<id>` | なし | launch-time | STABLE_BETA（コンパイル。オフラインの `OmsiLaunch.ProfileTests`） | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` を読み込みます（[セッションプロファイル](session-profiles.md)を参照）。`/predefined-profile-index` が必須です（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`、終了コード `2`）。`new:` ブロックは `/new` の場合にのみ適用されます。`compatibility.maps` は `/new` と `/saved` に対して強制されます（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。プロファイルが所有するフィールドと衝突する明示的なフラグは `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` で拒否されます（`CliInput.RejectProfileConflicts`）。対象は、`new:` ブロックが所有する場合の map/entrypoint/date/time/year/weather、プリセットが所有する `/set` キー、プリセットに `presentation` がある場合のスプラッシュフラグ、`internet-textures` がある場合の Internet Textures フラグ、`behavior` がある場合のタイムアウトです。 |
| `/predefined-profile-index` | `/predefined-profile-index:<1..99>` | なし | launch-time | STABLE_BETA | `index` によってプリセットを選択します。範囲外: 終了コード `2`。 |

<a id="launchspec-file"></a>
### LaunchSpec ファイル

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/spec` | `/spec:<path.json>` | なし | launch-time | STABLE_BETA（ローダーはオフラインテスト済み。セッションのセマンティクスはフラグと同一） | `LaunchSpec` JSON ファイルをシードとして読み込み（[LaunchSpec](launchspec.md) を参照）、起動が要求されたものとして扱います。規則（`LaunchSpecJson`）: ファイルが存在すること（`OL_E_SPEC_NOT_FOUND`、終了コード `6`）。最大 1 MiB（`OL_E_SPEC_TOO_LARGE`、終了コード `2`）。ルートはオブジェクトであること（`OL_E_SPEC_INVALID`）。プロパティ名は大文字と小文字を区別しない。`//` コメントと末尾のカンマは許可。深さは最大 32。未知のプロパティはすべて、その JSON パスとともに拒否されます（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentation.Foo`、終了コード `2`）。 |

**優先順位**（`CliInput.BuildSpecAsync`）: デフォルト → `/spec` ファイル → `/predefined-profile`（`Installation` と `World` を置き換えてから、プロファイルを適用） → 明示的なフラグ。明示的なインストール引数は spec 内の `RootPath` より優先されます。`/no-vehicle` は spec のプレイヤー車両をクリアします。`/vehicle` とその関連フラグはフィールド単位で spec にマージされます。`/set` キーは `Environment.General` にマージされます。`/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` は指定された場合にのみ上書きします。`/startup-timeout` と `/shutdown-timeout` は指定された場合にのみ上書きします。`Presentation.SuppressTrayIcon` は spec からのみ取得されます（フラグはありません）。診断フラグは spec の `Diagnostics` と OR で結合されます。

<a id="content-discovery"></a>
### コンテンツの検出

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/list` | `/list:<category>`。カテゴリは `ContentQueryKind` の値 `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`（大文字と小文字を区別しない） | なし | ローカル、セッション不要 | STABLE_BETA | インストール環境に対する `DiscoverAsync` です。`Identity`、`Kind`、`DisplayName` のエントリを含むエンベロープ `content.list`。終了コード `0`。`SessionProfiles` は `ListSessionProfilesAsync`（エンベロープ `session-profiles.list`）で、`RuntimeModules` は `ListRuntimeModulesAsync`（エンベロープ `runtime-modules.list`: 各モジュールの ID、バージョン、アクティベーション、操作、有効性、および型・既定値・フラグを含む設定）で応答します。未知のカテゴリ: `Unknown discovery category`、終了コード `2`。リパースポイント（ジャンクション/シンボリックリンク）はスキップされ、OMSI のファイルは Windows-1252 として読み取られます。 |
| `/vehicle-scope` | `/vehicle-scope:<vehicle identity>` | なし | ローカル | STABLE_BETA | `Entrypoints` を除くすべてのカテゴリに転送されるスコープです。`Entrypoints` は `/map` をスコープとして使用します。 |

<a id="timeouts-and-observation"></a>
### タイムアウトと観察

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/startup-timeout` | `/startup-timeout:<1..600>` 秒 | spec/プロファイルの値、なければ `180` | launch-time | STABLE_BETA | `Behavior.StartupTimeoutSeconds`。オーナーはこの値に 5 s を加えた時間だけ `Running` を待機します。`OL_E_STARTUP_TIMEOUT` はセッションを終了させ、終了コードは `1` です。 |
| `/shutdown-timeout` | `/shutdown-timeout:<1..600>` 秒 | spec/プロファイルの値、なければ `30` | launch-time | EXPERIMENTAL | `Behavior.ShutdownTimeoutSeconds`: `/cooperative-shutdown` を併用した場合、スーパーバイザーが OMSI を強制終了する前に OMSI の終了を待つ時間です。併用しない場合、OMSI は直ちに強制終了され、この値は効果を持ちません。 |
| `/cooperative-shutdown` | `/cooperative-shutdown` | オフ | launch-time | EXPERIMENTAL | `Behavior.CooperativeShutdown`: 実行中のセッションを終了するとき、OMSI 自身の終了ルーチン（ユーザーが終了を確認した後に実行されるもので、`options.cfg` とマップの `laststn.osn` を保存してから閉じます）で OMSI に終了を依頼し、強制終了する前に `ShutdownTimeoutSeconds` だけ待機します。ランタイム検証済みになるまではオプトインです。OMSI を強制終了せざるを得なかった場合は `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`。セッションのファイルは、どちらの場合も終了後に復元されます。 |
| `/observe-seconds` | `/observe-seconds:<0..2147483647>` | なし（OMSI が終了するか停止が要求されるまで実行） | runtime（オーナー） | STABLE_BETA | 実行フェーズの上限です。`Running` になってから `n` 秒後に正規の停止が要求されます。トレイやパイプからの停止、または OMSI の終了があれば、それより早く終わります。`0` は `Running` の直後に停止します。 |

<a id="recovery"></a>
### リカバリ

| フラグ | 構文と値 | デフォルト | フェーズ | 安定性 | 動作 |
|---|---|---|---|---|---|
| `/recovery-status` | `/recovery-status` | オフ | ローカル | STABLE_BETA | `<root>\.omsilaunch\journal.json` が保留中かどうか（`pending`）を報告し、復元は決して行いません。終了コード `0`。インストールリースを取得します。オーナーがリースを保持している間は `OL_E_INSTALLATION_BUSY`（終了コード `7`）です。 |
| `/recover` | `/recover` | オフ | ローカル | STABLE_BETA | 保留中のジャーナルを復元します（まずバックアップをスナップショットの SHA-256 と照合して検証します。`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED` は `diagnostics` に報告されます）。何も保留されていなかった場合、または復元が完了した場合は終了コード `0`。ジャーナルが保留中で、そのまま残っている場合は `8`。ジャーナルに記録された OMSI プロセス（PID、作成時刻、exe パス）、または PID を持たない `HandoffCreated` 以降のジャーナルの場合はそのルートの任意の `Omsi.exe` が生存している間は、`OL_E_INSTALLATION_BUSY` で拒否されます。すべてのセッション開始時には、インストール環境を読み取る前に同じリカバリが自動的に実行されます。 |
| `/clear-module-data` | `/clear-module-data:<module id>` | なし | ローカル、セッション不要 | EXPERIMENTAL | モジュールのデータディレクトリ `<root>\.omsilaunch\module-data\<id>` だけを削除します。エンベロープ `module-data.clear` に `module` と `cleared`（削除対象がなかった場合は `false`）を含み、終了コード `0`。インストールリースを取得します。セッションがインストール環境を所有している間は `OL_E_INSTALLATION_BUSY`（終了コード `7`）。リンク経由で到達したモジュールデータフォルダーは拒否されます（`OL_E_MODULE_INTEGRITY`）。その内部のリンクはリンクとして削除されます。 |

<a id="output-formats"></a>
## 出力形式

- **成功エンベロープ**（`CliInput.WriteEnvelope`、`--json` 指定時）: `{"ok": true, "command": "<name>", "protocol_version": "0.1", "result": <object>}`（インデント付き）。転送された `session status` および `events read` の応答では、制御フレームに収めるために古いイベントが除外された場合、`metadata` メンバーが追加されます（`events_dropped_count`、[ローカル制御](local-control.md)を参照）。`--json` がない場合は `<object>` のみがインデント付き JSON として出力され、イベントが除外された場合はその後に `Note: <n> older events were omitted to fit the control frame.` が続きます。
- **エラーエンベロープ**（`CliInput.WriteError`、`--json` 指定時）: `{"ok": false, "command": "<name>", "protocol_version": "0.1", "error": {"code": "OL_E_...", "category": "<category>", "message": "..."}}`。`--json` がない場合: 1 行の `OL_E_<CODE>: message`。カテゴリ: `invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。`OmsiLaunchW.exe` の下では、同じコードとメッセージがメッセージボックスで表示されます。
- **プランとステータス**（`CliInput.Write`）: `SessionPlan`、`SessionStatus`、`RuntimeCommandResult` の各レコードは、エンベロープ **なし** でインデント付き JSON として出力されます。`--json` がない場合、プランは `Plan: READY profile=Omsi23004_692EBFBF` または `Plan: NOT RUNNABLE profile=...` と要約されます。その他のレコードは引き続き JSON として出力されます。列挙値は整数としてシリアル化されます（`SessionState.Running` は `14`、`Completed` は `18`、`Failed` は `19`）。
- エンベロープで使用されるコマンド名: `silent`、`version`、`capabilities`、`help`、`profiles`、`detect`、`recover`、`content.list`、`session`、`session.status`、`session.stop`、`events.read`、`events.watch`、`events watch`、`installation`、`cli`、`session profile`、および転送されたランタイムコマンドのランタイム操作 id。
- `OmsiLaunchW.exe`（`OMSILAUNCH_WINDOWS_HOST=1`）の下では、`--json` が指定されない限りコンソールには何も書き込まれません。

<a id="errors-per-command"></a>
## コマンドごとのエラー

| コマンド | 代表的なエラーコード | 終了コード |
|---|---|---|
| 任意の解析失敗 | `OL_E_INVALID_ARGUMENT`、セッションプロファイルのコード（`OL_E_SESSION_PROFILE_*`） | `2` |
| `/silent` | `OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED` | `7` |
| クライアントルート、`/runtime`（クライアント） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（`2`）。`OL_E_NO_ACTIVE_SESSION`（`4`）。オーナーが返す `OL_E_CONTROL_*`、`OL_E_RUNTIME_*`。例: `OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_SESSION_NOT_RUNNING`（`7`） | `2`、`4`、`7` |
| `session status`、`session stop`、`events read`、`events watch` | `OL_E_NO_ACTIVE_SESSION`（`4`）。`OL_E_CONTROL_SESSION_MISMATCH`、`OL_E_CONTROL_PROTOCOL`、`OL_E_CONTROL_FAILED`（`7`） | `4`、`7` |
| オーナーの事前チェック | `OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_SESSION_ALREADY_ACTIVE` | `7` |
| `/recovery-status`、`/recover` | `OL_E_INSTALLATION_BUSY`（`7`）。`OL_E_RECOVERY_*`、`OL_E_RESTORE_FAILED`（`8`）。保留中だがリカバリされなかった場合（`8`） | `7`、`8` |
| `/list` | 未知のカテゴリ（`2`）。`OL_E_INSTALLATION_NOT_FOUND`/ディレクトリの欠落（`6`） | `2`、`6` |
| `/spec` | `OL_E_SPEC_NOT_FOUND`（`6`）。`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`（`2`） | `2`、`6` |
| `/set` | `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE` | `2` |
| `/plan`、`/validate`、起動 | プラン診断: `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（インストール済みのプラグイン一式）、`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_SESSION_PRESENTATION_INVALID`、`OL_E_RUNTIME_ARTIFACT_MISSING`、`plugin.integrity.reference`（情報提供） | `1` |
| セッションの開始 | `OL_E_PLAN_NOT_RUNNABLE`（開始時の再プランニング、`1`）。`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_RELEASE_MANIFEST_INVALID`（通常はプランニングがプラン診断として報告し、終了コード `1`。プランニングから開始までの間にプラグインファイルが変化した場合にのみ `7`）、`OL_E_INSTALLATION_BUSY`（`7`）。`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_EXITED_EARLY`、`OL_E_STARTUP_TIMEOUT`、`OL_E_WORLD_START_FAILED`、`OL_E_SITUATION_LOAD_FAILED`、`OL_E_PLUGIN_NOT_LOADED`（セッションは `Failed`、`1`） | `1`、`7` |
| 任意の場所での未処理例外 | `CliProgram.Classify` によって分類されます（[終了コード](exit-codes.md)を参照） | `2`..`10` |

<a id="environment"></a>
## 環境

| 変数 | 設定元 | 効果 |
|---|---|---|
| `OMSILAUNCH_WINDOWS_HOST=1` | `OmsiLaunchW.exe` | `WindowsHost.IsActive`: コンソール出力は抑止され、失敗はメッセージボックスで表示され、`/silent` は再委譲されません。 |

<a id="see-also"></a>
## 関連項目

[CLI の使用例](cli-examples.md) · [OmsiLaunchW.exe](omsilaunchw.md) · [終了コード](exit-codes.md) · [エラー](errors.md) · [ローカル制御](local-control.md) · [Windows トレイ](windows-tray.md) · [ランタイム制御](runtime-control.md) · [ケイパビリティ](capabilities.md) · [LaunchSpec](launchspec.md) · [セッションプロファイル](session-profiles.md) · [パッケージング](packaging.md) · [互換性](compatibility.md) · [既知の制限事項](known-limitations.md) · [公開 API](public-api.md)
