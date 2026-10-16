# 終了コード

<!-- l10n: source=reference/exit-codes.md source-sha256=e4c8d0f699c39379c1bb3c3194c29b92bf3ca4b830a086b6b3c2a104b8422cfa -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/exit-codes.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、`OmsiLaunch.exe` と `OmsiLaunchW.exe` が返しうるすべてのプロセス終了コードを一覧します。対象は、公開マネージド契約 `PublicExitCode`（`src\OmsiLaunch.Api\PublicControlContract.cs`）、ネイティブシムのコード `100`..`106`（`tools\OmsiLaunch.Bootstrapper\OmsiLaunch.Bootstrapper.cpp` と `OmsiLaunch.WindowsHost.cpp`）、および捕捉されずに伝播した例外に `CliProgram.Classify` が適用する分類規則（`tools\OmsiLaunch.Cli\Program.cs`）です。呼び出し元は、意味をコードと構造化されたエラーエンベロープから判断しなければならず、メッセージテキストから判断してはいけません。エラーコードは[エラー](errors.md)に一覧があり、各コードを生成するコマンドは [CLI リファレンス](cli.md)にあります。

<a id="public-exit-codes-publicexitcode"></a>
## 公開終了コード（`PublicExitCode`）

| コード | 列挙名 | 意味 | 発生条件 |
|---|---|---|---|
| 0 | `Success` | コマンドが完了しました。 | `/version`、`capabilities`、`help`、`profiles`、`detect`、`/list`、`/recovery-status`。実行可能なプランでの `/plan`/`/validate`。`Completed` で終了したセッション。`OmsiLaunchW.exe` が起動した時点の `/silent`。オーナーが `Ok=true` で応答した転送クライアントコマンド。保留中のものがなかった、または復元が完了した場合の `/recover`。 |
| 1 | `SessionFailed` | プランが実行不可だったか、所有するセッションが `Failed` で終了しました。 | `NOT RUNNABLE` を報告する `/plan`。プランが実行不可である起動（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_PERMANENT_PLUGIN_*` など。`OmsiLaunchW.exe` はさらに最後の `OL_E_` 診断をメッセージボックスに表示します）。`StartSessionAsync` が送出する `OL_E_PLAN_NOT_RUNNABLE`（開始時の再プラン）。起動タイムアウト内にセッションが `Running` に到達しなかった場合。セッションが `Failed` で終了した場合。 |
| 2 | `InvalidArguments` | コマンドライン、spec、プロファイル、またはランタイム引数が、ディスパッチの前または途中で拒否されました。 | 不明なフラグまたはルート、値の欠落、範囲外の値。`SessionProfileException`（`OL_E_SESSION_PROFILE_*`）。`OL_E_SPEC_TOO_LARGE`、`OL_E_SPEC_INVALID`、`OL_E_SPEC_UNKNOWN_PROPERTY`。`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`。`OL_E_ITX_PROFILE_REQUIRED`。`OL_E_RUNTIME_OPERATION_UNKNOWN` と `OL_E_RUNTIME_ARGUMENT_REQUIRED`（ローカルで検出された場合、またはオーナーから返された場合）。ディスパッチできないコマンドに対して使用法が出力された場合。あらゆる `ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException`。 |
| 3 | `UnsupportedProfile` | プラットフォームまたは OMSI ビルドが対応していません。 | コードが `OL_E_UNSUPPORTED_` で始まる、捕捉されずに伝播した例外（`OL_E_UNSUPPORTED_BUILD`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_OS_ARCHITECTURE`）。なお、同じ条件がプランの作成中に見つかった場合は、プランが実行不可となり、代わりに `1` が返されます。 |
| 4 | `NoActiveSession` | クライアントコマンドがオーナーを見つけられませんでした。 | このインストール環境のローカル制御エンドポイントが応答しない場合の `session status`、`session stop`、`events read`、`events watch`、または転送されたランタイム操作（`OL_E_NO_ACTIVE_SESSION`）。 |
| 5 | `RuntimeUnavailable` | タイムアウトが例外として伝播しました。 | あらゆる `TimeoutException`（メッセージにコードが含まれない場合は `OL_E_TIMEOUT`、含まれる場合は `OL_E_RUNTIME_REQUEST_TIMEOUT` などの埋め込まれたコード）。転送されたクライアントのタイムアウトは、オーナーによって `Ok=false` として応答され、`5` ではなく `7` を返します。 |
| 6 | `NotFound` | ファイルまたはディレクトリが見つかりませんでした。 | `FileNotFoundException` / `DirectoryNotFoundException`（既定は `OL_E_NOT_FOUND`）。例として、`OL_E_SPEC_NOT_FOUND`、例外として送出された場合の `OL_E_ITX_PROFILE_MISSING`、`/list` 実行中にインストールディレクトリが存在しない場合。 |
| 7 | `OperationRejected` | コマンドは有効でしたが拒否されたか、転送されたコマンドがオーナー側で失敗しました。 | `OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`、`OL_E_RUNTIME_INSTALLATION_INCOMPLETE`、`OL_E_WINDOWS_HOST_MISSING`、`OL_E_WINDOWS_HOST_START_FAILED`、`OL_E_CANCELLED`。2 つの引数コード以外のすべての `Ok=false` 制御応答（`OL_E_CONTROL_*`、`OL_E_RUNTIME_*`、`OL_E_SESSION_NOT_RUNNING`）。他のどこにも分類されない `OL_E_` コードを持つ、捕捉されずに伝播したその他の例外（`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_PROCESS_*` など）。 |
| 8 | `TransactionRecoveryFailed` | 永続的なトランザクションを復元できませんでした。 | ジャーナルが保留中で、実行後も保留中のままである場合の `/recover`。コードが `OL_E_RECOVERY_` で始まるか `OL_E_RESTORE_FAILED` である、捕捉されずに伝播した例外（例: セッション自身の復元中の `OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`）。 |
| 10 | `InternalError` | `OL_E_` コードを持たない予期しない例外。 | カテゴリ `internal` の `OL_E_INTERNAL` として報告されます。メッセージは例外のテキストです。 |

コード `9` は割り当てられていません。

<a id="native-shim-exit-codes"></a>
## ネイティブシムの終了コード

マネージドコントローラーが実行される前に `OmsiLaunch.exe` / `OmsiLaunchW.exe` が返すコードです。これらは `PublicExitCode` と重複しないため、呼び出し元はホスト起動時の失敗とコントローラーの結果を区別できます。`OmsiLaunchW.exe` はさらに `OmsiLaunch could not start the .NET host (code N).` をメッセージボックスに表示します。

| コード | 意味 | 原因 |
|---|---|---|
| 100 | 実行ファイルのパスを解決できませんでした | `GetModuleFileNameW` が失敗しました。 |
| 101 | コマンドラインをトークン化できませんでした | `CommandLineToArgvW` が null を返しました。 |
| 102 | `hostfxr` の場所の探索に失敗しました | `get_hostfxr_path` のサイズ照会が失敗しました。一致する .NET ランタイムがインストールされていません（x64 の .NET 6 ランタイムが必要です）。 |
| 103 | `hostfxr` のパスを取得できませんでした | 2 回目の `get_hostfxr_path` 呼び出しが失敗しました。 |
| 104 | `hostfxr` ライブラリを読み込めませんでした | 解決された `hostfxr.dll` に対する `LoadLibraryW` が失敗しました。 |
| 105 | 必要な `hostfxr` エクスポートがありません | `hostfxr_initialize_for_dotnet_command_line`、`hostfxr_run_app`、または `hostfxr_close` が見つかりません。 |
| 106 | マネージドホストを初期化できませんでした | `OmsiLaunch.Controller.dll` に対する `hostfxr_initialize_for_dotnet_command_line` が失敗しました（`OmsiLaunch.Controller.runtimeconfig.json` がない、`Microsoft.WindowsDesktop.App` 6.0 x64 がない、またはパッケージが破損している）。 |

<a id="classification-rules-cliprogramclassify"></a>
## 分類規則（`CliProgram.Classify`）

`CliProgram.RunAsync` から伝播したすべての例外は、`Classify` を呼び出す `CliProgram.ReportFailure` によって、エラーエンベロープ（`CliInput.WriteError`）と終了コードに変換されます。解析の失敗も、ディスパッチの前に同じ方法で処理されます（終了コード `2`）。規則は次の順序で適用されます。

1. 例外メッセージ内の最初の `OL_E_` トークンが抽出されます（`ExtractCode`）。コードは、`OL_E_` から始まる ASCII 英字、数字、`_` の最長の連続です。コードはそのまま `error.code` に出力されます。
2. `SessionProfileException` → その例外自身の `Code`、カテゴリ `invalid_argument`、終了コード `2`。
3. `ArgumentException`、`FormatException`、`InvalidDataException`、`OverflowException` → 抽出されたコードまたは `OL_E_INVALID_ARGUMENT`、カテゴリ `invalid_argument`、終了コード `2`。
4. `FileNotFoundException`、`DirectoryNotFoundException` → 抽出されたコードまたは `OL_E_NOT_FOUND`、カテゴリ `not_found`、終了コード `6`。
5. `TimeoutException` → 抽出されたコードまたは `OL_E_TIMEOUT`、カテゴリ `runtime`、終了コード `5`。
6. `OperationCanceledException` → `OL_E_CANCELLED`、カテゴリ `session`、終了コード `7`。
7. それ以外で、コードが抽出された場合:
   - `OL_E_RECOVERY_` で始まるか `OL_E_RESTORE_FAILED` と等しい → カテゴリ `transaction`、終了コード `8`
   - `OL_E_INSTALLATION_BUSY`、`OL_E_SESSION_ALREADY_ACTIVE` → カテゴリ `session`、終了コード `7`
   - `OL_E_PLAN_NOT_RUNNABLE` → カテゴリ `session`、終了コード `1`
   - `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_ITX_PROFILE_REQUIRED` → カテゴリ `invalid_argument`、終了コード `2`
   - `OL_E_UNSUPPORTED_` で始まる → カテゴリ `unsupported_profile`、終了コード `3`
   - その他のコード → `InvalidOperationException` と `IOException` の場合はカテゴリ `runtime`、それ以外は `internal`。終了コード `7`。
8. コードがまったくない → `OL_E_INTERNAL`、カテゴリ `internal`、終了コード `10`。

転送されたクライアントの応答は `Classify` を経由しません。`CliProgram.ReportForwarded` は、エンドポイントがない場合は `4`、`OL_E_RUNTIME_OPERATION_UNKNOWN` / `OL_E_RUNTIME_ARGUMENT_REQUIRED` の場合は `2`、その他の `Ok=false` 応答の場合は `7`、`Ok=true` の場合は `0` を返します。

<a id="scripting-guidance"></a>
## スクリプトでの扱い方

- `0` を成功、それ以外をすべて失敗として扱ってください。数値コードで分岐し、次に `--json` エンベロープの `error.code` で分岐します。
- セッションの起動は、セッションが終了してそのファイルが復元された後にのみ戻ります。`1` は、トランザクションは実行されたが OMSI が失敗したかプランが拒否されたことを意味し、ファイルが変更されたまま残っていることを意味するものではありません（残存するジャーナルは `/recovery-status` で報告されます）。
- `100`..`106` は、パッケージまたは .NET ランタイムが壊れていることを意味します。[インストール](../getting-started/installation.md)と[パッケージング](packaging.md)を参照してください。
