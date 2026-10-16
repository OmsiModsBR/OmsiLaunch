# OmsiLaunchW.exe リファレンス

<!-- l10n: source=reference/omsilaunchw.md source-sha256=1d15bd724a02153b5a30cc0d591754f94a1b1d66e7cba64b62501ac037b54622 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/omsilaunchw.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

`OmsiLaunchW.exe` は、OmsiLaunch コントローラーの Windows サブシステム（GUI）ホストです。`OmsiLaunch.exe` と同じコマンドラインを受け付け、同じコントローラーコード（`OmsiLaunch.Controller.dll`）を実行します。唯一の違いは報告の方法です。コンソールウィンドウはなく、失敗はメッセージボックスとして表示され、実行中のセッションはその[トレイアイコン](windows-tray.md)を通してのみ確認できます。

根拠となるソース: `tools\OmsiLaunch.Bootstrapper\OmsiLaunch.WindowsHost.cpp`（ネイティブシム）、`tools\OmsiLaunch.Cli\WindowsHost.cs`（`WindowsHost`、`SessionTrayIndicator`）、`tools\OmsiLaunch.Cli\Program.cs`（`CliProgram.RunAsync`、`OwnerSession.RunAsync`、`CliInput.Write*`）。

<a id="omsilaunchexe-and-omsilaunchwexe-compared"></a>
## OmsiLaunch.exe と OmsiLaunchW.exe の比較

| 項目 | `OmsiLaunch.exe` | `OmsiLaunchW.exe` |
| --- | --- | --- |
| サブシステム | コンソール。エクスプローラーから起動するとコンソールウィンドウが開きます。 | Windows（GUI）。コンソールウィンドウはありません。 |
| ネイティブシム | `OmsiLaunch.Bootstrapper.cpp` | `OmsiLaunch.WindowsHost.cpp` |
| 環境 | 変更なし | .NET が起動する前に、コントローラープロセスに `OMSILAUNCH_WINDOWS_HOST=1` を設定します |
| 引数 | `CommandLineToArgvW` でトークン化され、変更されずにコントローラーに渡されます | 同じです。そのため、両方のホストはまったく同じフラグとコマンドを受け付けます（[CLI リファレンス](cli.md)） |
| テキスト出力 | stdout に書き込まれます | 抑止されます。ただし `--json` が指定された場合は例外です（その場合は JSON エンベロープが stdout に書き込まれ、呼び出し元はそれをリダイレクトできます） |
| エラー | `OL_E_...: message` または JSON エラーエンベロープ | 同じコンソール出力規則に**加えて**、すべてのエラーでメッセージボックスが表示されます（[エラーダイアログ](#failure-dialogs)を参照） |
| `/silent` | 残りの引数で `OmsiLaunchW.exe` を起動し、`0` を返します | 無視されます。コマンドはすでに Windows ホスト内で実行されています |
| トレイアイコン | オーナーセッションで表示されます | オーナーセッションで表示されます |
| 停止経路 | トレイ、`session stop`、OMSI の終了、`/observe-seconds`、Ctrl+C、コンソールを閉じる | トレイ、`session stop`、OMSI の終了、`/observe-seconds`（コンソールがないため、Ctrl+C とコンソールを閉じる操作は該当しません） |
| 終了コード | [`PublicExitCode`](exit-codes.md) `0`..`10`、シムコード `100`..`106` | 同じコード |

<a id="how-it-starts"></a>
## 起動の仕組み

1. シムは自身のパスを解決し（`GetModuleFileNameW`）、同じディレクトリに `OmsiLaunch.Controller.dll` があることを想定します。
2. コマンドラインをトークン化し（`CommandLineToArgvW`）、`OMSILAUNCH_WINDOWS_HOST=1` を設定します。
3. パッケージに含まれる `nethost.dll` を通じて `hostfxr` を探して読み込み、引数を渡してコントローラーを初期化し（コントローラーのパスは、CLI パーサーが受け取る引数リストには含まれません）、実行します。
4. シムはコントローラーの終了コードを変更せずに返します。

コントローラーが実行される前のいずれかの手順が失敗した場合、シムはタイトルが `OmsiLaunch`、テキストが `OmsiLaunch could not start the .NET host (code N).` のメッセージボックスを表示し、そのコードで終了します。

| コード | 失敗した手順 |
| --- | --- |
| `100` | 実行ファイルのパスを解決できませんでした |
| `101` | コマンドラインをトークン化できませんでした |
| `102` | `hostfxr` の場所の探索に失敗しました（通常は .NET 6 x64 ランタイムがインストールされていません） |
| `103` | `hostfxr` のパスを取得できませんでした |
| `104` | `hostfxr` を読み込めませんでした |
| `105` | 必要な `hostfxr` エクスポートがありません |
| `106` | マネージドホストを初期化できませんでした（例: `OmsiLaunch.Controller.dll` またはそのランタイム構成がない、または Windows Desktop ランタイムが存在しない） |

`OmsiLaunch.exe` も同じ表を使用しますが、何も出力しません。これらのダイアログはランタイムで発生させて確認されていません（[ランタイム検証ステータス](../status/runtime-validation-status.md)を参照）。

<a id="starting-it"></a>
## 起動方法

ショートカット、スクリプト、または別のプログラムから直接起動する場合:

```text
OmsiLaunchW.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

```text
OmsiLaunchW.exe /predefined-profile:<PROFILE_NAME> /predefined-profile-index:1 /new
```

`OmsiLaunch.exe` から `/silent` を使って起動する場合:

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

実行ファイルは OMSI 2 のインストール環境に配置してください（パッケージのレイアウトについては[パッケージング](packaging.md)を参照）。インストール環境の引数がない場合、インストール環境は実行ファイルを含むディレクトリになります。インストール環境を明示する場合は、`OmsiLaunch.exe` と同様に最初の引数として渡します（`OmsiLaunchW.exe "<OMSI_PATH>" /new ...`）。

GUI プログラムであるため、`cmd.exe` とエクスプローラーはその終了を待ちません。スクリプトから終了を待って終了コードを読み取るには、`cmd.exe` では `start /wait OmsiLaunchW.exe ...` を、PowerShell では `Start-Process -Wait -PassThru` を使用してください。

```powershell
$p = Start-Process -FilePath .\OmsiLaunchW.exe -ArgumentList '/new','/map:maps\Grundorf\global.cfg','/entrypoint-index:1' -Wait -PassThru
$p.ExitCode
```

<a id="silent-delegation"></a>
## /silent による委譲

`OmsiLaunch.exe ... /silent`（または `--silent`）は、まだ `OmsiLaunchW.exe` の下で実行されていない場合、次の処理を行います（`CliProgram.RunAsync`、`CliProgram.SilentDelegation`）。

1. `OmsiLaunch.exe` のディレクトリで `OmsiLaunchW.exe` を探します。見つからない場合: `OL_E_WINDOWS_HOST_MISSING`、終了コード `7`。
2. 現在のディレクトリと、`/silent`/`--silent` を除くすべての引数を元の順序のまま使用して、`ShellExecute`（`UseShellExecute = true`）経由で `OmsiLaunchW.exe` を起動します。`ShellExecute` は呼び出し元のハンドルを新しいプロセスに継承させないため、`OmsiLaunch.exe /silent` の出力をキャプチャする呼び出し元がセッションの存続期間中ブロックされることはありません（ランタイムクロージャ BUG-03）。プロセスが返されない場合: `OL_E_WINDOWS_HOST_START_FAILED`、終了コード `7`。
3. `silent` エンベロープを書き込み、ただちに `0` で終了します。

```json
{"ok": true, "command": "silent", "protocol_version": "0.1", "result": {"delegated": true, "host_process_id": 12345}}
```

終了コード `0` は、`OmsiLaunchW.exe` が起動されたことだけを意味します。セッションの結果（プラン作成時のエラー、すでにアクティブなオーナー、開始の失敗）は、`OmsiLaunchW.exe` が独自のダイアログと診断情報で報告します。その後、2 つのプロセスは互いに独立しています。`OmsiLaunch.exe` は終了しており、`OmsiLaunchW.exe` がセッションオーナーです。セッションを確認するには `OmsiLaunch.exe session status` を使用してください。

`/silent` は他のすべてのコマンドより先に適用されるため、`OmsiLaunch.exe /silent session status` も `session status` を `OmsiLaunchW.exe` 内で実行し、その出力は抑止されます。`/silent` は起動にのみ使用してください。

<a id="what-each-command-does-under-omsilaunchwexe"></a>
## OmsiLaunchW.exe における各コマンドの動作

| コマンドライン | 結果 |
| --- | --- |
| 引数なし | `detect` を表示なしで実行し、`0` で終了します（何も表示されません） |
| 実行可能なプランを持ち、オーナーが存在しない起動（`/new`、`/saved:...`、`/spec:...`、セッションプロファイル） | セッションオーナーになります。開始中と実行中はトレイアイコンが表示されます。セッションが終了すると終了します（完了時は `0`、失敗時は `1`） |
| プランが実行不可である起動 | プランの最後の `OL_E_` 診断を示すダイアログ（フォールバックは `The session plan is not runnable.` / `OL_E_SESSION_START_FAILED`）、終了コード `1`（ドキュメント監査 BUG-06。修正前は何も表示せずに終了していました） |
| インストール環境にすでにオーナーがアクティブな状態での起動 | `OL_E_SESSION_ALREADY_ACTIVE` のダイアログ、終了コード `7`。実行中のセッションには影響しません |
| `Running` に到達しない起動 | 最後の `OL_E_` 診断を伴うダイアログ `The OMSI session did not reach gameplay.`、終了コード `1`。ダイアログが開いている間に、セッションスーパーバイザーが OMSI を終了させてファイルを復元します。プロセスは、ダイアログが閉じられ、`CloseAsync` が完了した後に終了します |
| 無効な引数、不明なフラグ、または無効なセッションプロファイル | `OL_E_INVALID_ARGUMENT` または `OL_E_SESSION_PROFILE_*` コードのダイアログ、終了コード `2` |
| オーナーが存在しない状態でのクライアントコマンド（`session status`、`session stop`、`events read`、`time get`、`/runtime:...`） | `OL_E_NO_ACTIVE_SESSION` のダイアログ、終了コード `4` |
| オーナーが拒否したクライアントコマンド | 拒否コード（例: `OL_E_CONTROL_SESSION_MISMATCH` またはランタイムのエラーコード）のダイアログ、終了コード `7`（不明な操作または引数の欠落の場合は `2`） |
| 成功したクライアントコマンドまたは検出系コマンド（`session status`、`/list:...`、`help`、`capabilities`、`/version`、`/recovery-status`） | `--json` が指定され、かつ stdout がリダイレクトされている場合を除き、目に見える出力はありません。終了コードは `OmsiLaunch.exe` と同じです |
| `/plan` または `/validate` | 実行不可のプランであってもダイアログは表示されません。終了コード `0` または `1` |
| その他のコントローラーの失敗 | 分類された `OL_E_` コードのダイアログ。終了コードは[終了コード](exit-codes.md)に従います |

<a id="failure-dialogs"></a>
## エラーダイアログ

`OmsiLaunch.exe` が出力するはずのすべてのエラーは、`--json` が指定された場合でも、モーダルなメッセージボックス（`WindowsHost.ShowFailure`）としても表示されます。

```text
Title:  OmsiLaunch            (error icon)

<message>

Code: OL_E_<CODE>

See .omsilaunch\diagnostics for details.
```

`<message>` はエラーメッセージ、またはセッションの失敗の場合は最後の `OL_E_` 診断のメッセージです。既知の制限事項: プラグインが報告した失敗の場合、メッセージはプラグインの生の失敗ペイロード（例: `{"name":"world.failed",...}`）になります。`Code:` 行は正しい値です（[既知の制限事項](known-limitations.md)を参照）。ダイアログはモーダルであり、閉じられた後にプロセスが終了します。ランタイムでのエビデンス: 引数エラー、アクティブなセッションなし、ゲームプレイ前の失敗（ランタイムクロージャ `T04`）。

<a id="session-tray-and-exit"></a>
## セッション、トレイ、終了

`OmsiLaunchW.exe` が所有するセッションは、`OmsiLaunch.exe` が所有するセッションとまったく同じように動作します（[セッションのライフサイクル](../concepts/session-lifecycle.md)を参照）。

- トレイアイコンは、`/spec` ファイルで `Presentation.SuppressTrayIcon` が設定されていない限り、セッションが開始されるとすぐに、OMSI がゲームプレイに到達する前に表示されます。`SuppressTrayIcon` を設定した場合、目に見える表示はまったくありません。セッションは `OmsiLaunch.exe session stop` または OMSI を閉じることで停止してください。
- セッションは、OMSI が終了したとき、トレイで `End session`（セッションを終了する項目）が確認されたとき、クライアントが `session stop` を送信したとき、または `/observe-seconds` が経過したときに終了します。その後 OmsiLaunch は、OMSI がまだ実行中であれば終了させ、変更したすべてのファイルを復元し、インストールリースを解放し、トレイアイコンを削除して終了します。
- `OmsiLaunchW.exe` 自体が強制終了された場合は、そのインストール環境で次に OmsiLaunch を開始したときに、保留中のトランザクションがリカバリされます（[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照）。

<a id="quick-start"></a>
## クイックスタート

1. パッケージを OMSI 2 のディレクトリにインストールします（[インストール](../getting-started/installation.md)）。
2. セッションの引数（例: `/new /map:maps\Grundorf\global.cfg /entrypoint-index:1`）を指定した `OmsiLaunchW.exe` へのショートカットを作成します。
3. それを起動します。OMSI はコンソールウィンドウなしで起動し、通知領域に OmsiLaunch のアイコンが表示されます。
4. アイコンを右クリック → `Status`（ステータス表示の項目）でセッションを確認するか、`End session` → `End session` でセッションを終了します。
5. 問題が発生した場合は、ダイアログにエラーコードが表示されます。詳細は `<OMSI_PATH>\.omsilaunch\diagnostics` にあります。
