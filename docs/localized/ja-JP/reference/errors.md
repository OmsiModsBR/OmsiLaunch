# エラーコードと診断コード

<!-- l10n: source=reference/errors.md source-sha256=2eb8a7ca44e6cc4c5e7dfa9a3a10ba77477f2cf007719bb7b99c0136ef6339c4 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/errors.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは `PublicErrorCodes`（`src/OmsiLaunch.Api/PublicErrorCodes.cs`）に含まれるすべてのコードの規範的なリファレンスです。対象はカタログのカテゴリ別に分類した 167 個の `OL_E_` エラーコードと 4 個の `OL_W_` 警告、およびエラーではない情報提供用の診断コードです。各コードについて、現在のコードのどこで発生するか、何を意味するか、どのような形で呼び出し元に届くか（スローされる例外、結果フィールド、診断、コントロールプレーンの応答、CLI エンベロープ）、そしてどう対処すべきかを示します。意味は発生箇所から導いています。コードが定義されているものの現在は発生経路がない場合は、その旨を記載しています。

関連ページ: [公開 API](public-api.md)、[終了コード](exit-codes.md)、[LaunchSpec](launchspec.md)、[セッションのライフサイクル](../concepts/session-lifecycle.md)、[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)、[ローカルコントロールプレーン](local-control.md)、[ランタイム制御](runtime-control.md)、[セッションプロファイル](session-profiles.md)、[常駐プラグイン](../concepts/permanent-plugin.md)。

<a id="how-codes-reach-you"></a>
## コードの届き方

| 経路 | 意味 |
| --- | --- |
| スロー | `Message` がコードで始まる例外です（`InvalidOperationException`、`IOException`、`TimeoutException`、`InvalidDataException`、`FileNotFoundException`、`ArgumentException`、`SessionProfileException`）。CLI はメッセージからコードを抽出し、終了コードに対応付けます（`CliProgram.Classify`）。 |
| プラン診断 | `SessionPlan.Diagnostics` 内の `LaunchDiagnostic` です。`OL_E_` コードが 1 つでもあると `IsRunnable` が false になります（CLI: プランは `NOT RUNNABLE`、終了コード 1）。 |
| セッション診断 | `SessionStatus.Diagnostics` 内の `LaunchDiagnostic` です。セッション状態は `Failed` になります（CLI 終了コード 1）。`OL_E_START_SESSION`、`OL_E_PROCESS_SUPERVISION`、`OL_E_RESTORE_FAILED` はメッセージ内に内側のコードを包んでいます。 |
| ランタイム結果 | `Succeeded = false` を伴う `RuntimeCommandResult.ErrorCode` です。 |
| ランタイム詳細 | `RuntimeCommandResult.ErrorCode` が具体的なコードで、`Values["detail"]` がメッセージです（`Values["exception"]` は .NET の型）。Beta 4: プラグイン側の型付きコードは API と CLI にそのまま届きます。カタログに載っているコードを持たない失敗のみが `OL_E_RUNTIME_OPERATION_FAILED` になります。 |
| コントロール応答 | ローカルコントロールプレーンの応答（`LocalControlResponse`）の `ErrorCode` です。 |
| CLI エンベロープ | `--json` エンベロープ内の `error.code`、またはコンソール上の `<code>: <message>` です。終了コードも示されます。 |
| テレメトリ | ホストがセッション診断に対応付けるランタイムイベント名です。 |

## Cli

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_CANCELLED` | `CliProgram.Classify` | `OperationCanceledException` が外部に漏れました（Ctrl+C、またはキャンセルされたクライアントの待機）。 | CLI エンベロープ、終了コード 7 | コマンドを再試行してください。 |
| `OL_E_INTERNAL` | `CliProgram.Classify` | `OL_E_` コードを持たない例外が外部に漏れました。不正な `/spec` JSON、必須の spec メンバーが null、予期しない障害などです。 | CLI エンベロープ、終了コード 10 | メッセージと `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` を確認し、入力を修正してください。原因が不明な場合は報告してください。 |
| `OL_E_TIMEOUT` | `CliProgram.Classify`、`LocalControlPlane.TryRequestAsync` | コードを持たない `TimeoutException` が外部に漏れました。または、ローカル制御クライアントが接続したオーナーがタイムアウト内に応答しませんでした（オーナーは存在するため、`OL_E_NO_ACTIVE_SESSION` としては報告されません）。（メールボックスのタイムアウトは代わりに `OL_E_RUNTIME_REQUEST_TIMEOUT` を伴います。） | CLI エンベロープ、コントロール応答。`Classify` からは終了コード 5、コントロール応答では終了コード 7 | 再試行してください。OMSI とオーナーが応答しているか確認してください。 |
| `OL_E_WINDOWS_HOST_MISSING` | `CliProgram.RunAsync`（`/silent`） | `OmsiLaunchW.exe` が `OmsiLaunch.exe` と同じ場所にありません。 | CLI エンベロープ、終了コード 7 | パッケージを再インストールしてください。 |
| `OL_E_WINDOWS_HOST_START_FAILED` | `CliProgram.RunAsync`（`/silent`） | `OmsiLaunchW.exe` の `Process.Start` がプロセスを返しませんでした。 | CLI エンベロープ、終了コード 7 | パッケージのファイルと権限を確認してください。`/silent` なしで実行すると失敗内容を確認できます。 |

## Compatibility

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_BUILD_VALIDATION_FAILED` | `plugin.build.invalid` 受信時の `OmsiLaunchService.ApplyTelemetry` | 開始ケイパビリティは解決されましたが、プラグインがヘッドレス開始を準備する時点で、実際の Start フォームまたはそのフックサイトの 1 つが期待された状態にありません（たとえば別のプラグインがすでにパッチを当てている場合）。 | セッション診断（`Failed`） | 競合するプラグインを取り除いてください。[互換性](compatibility.md)を参照してください。 |
| `OL_E_CAPABILITY_UNRESOLVED` | プラグイン（開始時の `plugin.capability.unresolved`、ランタイム操作） | リクエストが必要とするネイティブケイパビリティを、この OMSI ビルドで解決して検証できなかったため、利用できません。開始時はフックの前にセッションが失敗し、ファイルは復元されます。ランタイム操作は OMSI のメモリに触れることなく拒否されます。`Data["capability"]`（ランタイム）またはテレメトリの `capabilities` 値がそれを示します。 | セッション診断（`Failed`）、ランタイム結果 | [互換性](compatibility.md)を参照してください。このビルドではそのケイパビリティは利用できません。 |
| `OL_E_UNSUPPORTED_BUILD` | `SessionPlanner`（`omsi.omsi2` が利用不可） | インストール環境が OMSI 2 のものではありません。`Omsi.exe` が存在しない、32 ビット x86 の PE イメージではない、OMSI 2（2.x）のバージョンを持たない、またはルートに `maps\` か `Vehicles\` ディレクトリがありません。実行ファイルのハッシュは基準ではありません。 | プラン診断 | OmsiLaunch を OMSI 2 のインストールルートに向けてください。 |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | `SessionPlanner`（`runtime.current-windows-x64` が利用不可）。`CurrentWindowsX64Platform.ValidateCurrent` からもスローされますが、サービスはこれを呼び出しません | Windows 10 以降ではないか、OS またはホストプロセスが x64 ではありません。 | プラン診断（スローされた場合は CLI 終了コード 3） | 64 ビット版 Windows 10 以降で実行してください。 |
| `OL_E_UNSUPPORTED_OS_ARCHITECTURE` | `CurrentWindowsX64Platform.ValidateCurrent` のみ | OS またはホストのアーキテクチャが x64 ではありません。サービスは `ValidateCurrent` を呼び出さないため、現在の発生経路はありません。 | そのメソッドからのスロー（`PlatformNotSupportedException`）のみ | ソース `src/OmsiLaunch.Process/RuntimePlatform.cs` を参照してください。 |

## Content

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `LaunchValidation`、`SessionPlanner` | 検証: NEW_MAP で `EntrypointIdentity` がなく、`PresentedEntrypointIndex` が未設定または負の値です。プランナー: 要求された識別子または正確な（大文字と小文字を区別する）ラベルを持つエントリポイントがマップにありません。 | プラン診断 | `PresentedEntrypointIndex`（`/entrypoint-index:<n>`）を設定するか、`/list:entrypoints /map:<id>` の識別子を使用してください。 |
| `OL_E_WEATHER_INVALID` | `SessionPlanner`（`StartWeatherResolver`） | 開始時の天候を適用できません。インストール環境の `weather` フォルダーのファイルではないプリセット、または 4 文字の英字か数字ではない ICAO です。 | プラン診断 | インストール済みのプリセットか有効な観測所コードを使用してください。 |
| `OL_E_ENTRYPOINT_NOT_PRESENTED` | `SessionPlanner` | 要求されたエントリポイントが、マップ内の先行するレコードとラベルを共有しています。OMSI は各ラベルの最初のレコードだけを提示するため、開始時に選択できません。 | プラン診断 | そのラベルの最初のレコード、または別のエントリポイントを選択してください。 |
| `OL_E_ENTRYPOINT_REQUIRED` | `SessionPlanner`（`world.presented-entrypoint` が利用不可） | NEW_MAP のマップは解決されましたが、提示インデックスも識別子もありません。常に `OL_E_ENTRYPOINT_NOT_FOUND` と同時に発生します。 | プラン診断 | 同上。 |
| `OL_E_HOF_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Hof` がインストール済みの `Vehicles\...\*.hof` ではありません。 | プラン診断 | `/list:hofs` で得た識別子を使用してください。（いずれにせよ、このビルドではプレイヤー車両のフィールドは実行不可です。） |
| `OL_E_HOF_SOURCE_NOT_FOUND` | `HofPlanner` | [HOF 管理](hof-management.md)のソース（コレクション、ディレクトリ、車両、またはファイル）が存在しない、`.hof` がない、または `Files` で指定されたファイルがありません。 | プラン診断、開始の失敗 | ソースまたはファイルリストを修正してください。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | `HofPlanner` | HOF のターゲットの車両ファイルまたはフォルダーがインストールされていません。 | プラン診断、開始の失敗 | `/list:vehicles` の車両 ID か、`Vehicles\` 配下の既存のフォルダーを使用してください。 |
| `OL_E_HOF_PATH_ESCAPE` | `HofPlanner` | HOF のパスがインストール環境の外に出る、ターゲットが `Vehicles\` 配下にない、コレクション名が単純な名前ではない、またはジャンクション/シンボリックリンクをたどっています。 | プラン診断、開始の失敗 | `..` やリンクを含まない、インストール環境からの相対パスを使用してください。 |
| `OL_E_HOF_CONFLICT` | `HofPlanner`、`OmsiLaunchService` | 2 つの HOF 操作が 1 つのファイルに異なるバイト列を書き込む、またはミラーが別の操作が書き込むファイルを削除します。 | プラン診断、開始の失敗 | 競合する操作の順序を変える、分割する、または削除してください。 |
| `OL_E_HOF_INVALID` | `HofPlanner` | ターゲットまたはソースのない HOF 操作、`.hof` ではないファイルソース、`.bus`/`.ovh` ではないターゲットファイル、またはサイズ/件数の上限（1 ファイルあたり 8 MiB、1 操作あたり 256 ファイル、1 セッションあたり 256 MiB）の超過です。 | プラン診断、開始の失敗 | 操作を修正してください。 |
| `OL_E_MAP_NOT_FOUND` | `LaunchValidation`、`SessionPlanner` | 検証: NEW_MAP で `MapIdentity` が未設定、または `maps\<dir>\global.cfg` の形式ではありません。プランナー: マップがインストールされていません。 | プラン診断 | `/list:maps` で得た識別子を使用してください。 |
| `OL_E_NOT_FOUND` | `CliProgram.Classify` | コードを持たない `FileNotFoundException`/`DirectoryNotFoundException` が外部に漏れました。たとえば、未知の `/vehicle-scope` を指定した `/list:repaints` や、未知の `/map` を指定した `/list:entrypoints` です。 | CLI エンベロープ、終了コード 6 | 識別子を修正してください。 |
| `OL_E_REPAINT_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Repaint` がモデルの `.cti` 項目ではありません（`Model` が設定されている場合のみチェックされます）。 | プラン診断 | `/list:repaints /vehicle-scope:<bus>` で得た識別子を使用してください。 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SessionPlanner` | 選択した `.osn` 内で参照されているマップがインストールされていません。 | プラン診断 | マップをインストールするか、別のシチュエーションを選択してください。 |
| `OL_E_SITUATION_NOT_FOUND` | `LaunchValidation`、`SessionPlanner` | SAVED_SITUATION で `SituationIdentity` がないか、`.osn` がインストールされていません。 | プラン診断 | `/list:situations` で得た識別子を使用してください。 |
| `OL_E_VEHICLE_NOT_FOUND` | `SessionPlanner` | `PlayerVehicle.Model` がインストール済みの `Vehicles\...\*.bus` ではありません。 | プラン診断 | `/list:vehicles` で得た識別子を使用してください。 |

## Installation

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_INSTALLATION_BUSY` | `InstallationLease.Acquire`、`OmsiLaunchService.RecoverPendingAsync`、`FileConfigurationTransaction.RestorePendingAsync` | インストールリース（`Local\OmsiLaunch.Installation.<hash>`）がこのログオンセッション内の別のオーナーに保持されているか、ジャーナルに記録された OMSI プロセス（PID + 作成時刻 + 実行ファイルのパス。PID のない `HandoffCreated` 以降のジャーナルの場合は、ルートにある任意の `Omsi.exe`）がまだ動作しています。 | 開始時: `OL_E_START_SESSION` 経由のセッション診断。リカバリ時: スロー（`InvalidOperationException` / `IOException`）。CLI 終了コード 7。 | 別のオーナーを停止する（`session stop`）か OMSI の終了を待ってから、再試行するか `/recover` を実行してください。 |
| `OL_E_INSTALLATION_NOT_FOUND` | `LaunchValidation` | `Installation.RootPath` が空です。 | プラン診断 | インストールディレクトリを渡してください。 |
| `OL_E_INSTALLATION_NOT_WRITABLE` | `SessionPlanner`（`transaction.exact-restore` が利用不可）。`ValidateCurrent` からも発生 | ルートが存在しない、読み取り専用属性を持つ、または `plugins\` ディレクトリがありません。 | プラン診断 | 実在し、書き込み可能な OMSI インストール環境を指定してください。 |
| `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` | `RuntimeArtifactSet.ValidateInstalled`（プラン時と開始時） | インストール済みの `plugins\OmsiLaunch.*` ファイルが `release-manifest.json` 内のハッシュ（マニフェストがない場合は参照クロージャ）と異なります。 | プラン診断（プランは実行不可、CLI 終了コード 1）。プランニングから開始までの間にファイルが変更された場合のみ、`OL_E_START_SESSION` 経由のセッション診断 | `plugins\` とマニフェストが一致するよう、OmsiLaunch パッケージを再インストールしてください。 |
| `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE` | `RuntimeArtifactSet.ValidateInstalled`（プラン時と開始時） | 必須のプラグインファイルに対するエントリがマニフェストにありません。 | プラン診断。上記と同じ競合時は `OL_E_START_SESSION` 経由のセッション診断 | パッケージを再インストールしてください。 |
| `OL_E_PERMANENT_PLUGIN_MISSING` | `RuntimeArtifactSet.ValidateInstalled`（プラン時と開始時） | 必須の `plugins\OmsiLaunch.*` ファイルが OMSI インストール環境に存在しないか、`release-manifest.json` に記載された `plugins/` のファイルがインストールされていません。 | プラン診断。上記と同じ競合時は `OL_E_START_SESSION` 経由のセッション診断 | 常駐プラグインのクロージャをインストールしてください（[インストール](../getting-started/installation.md)）。 |
| `OL_E_PLATFORM_CAPABILITY_MISSING` | `CurrentWindowsX64Platform.ValidateCurrent` のみ | `CurrentPlatformSupported` が false です。サービスからは呼び出されないため、現在の発生経路はありません。 | そのメソッドからのスローのみ | ソースを参照してください。 |
| `OL_E_RELEASE_MANIFEST_INVALID` | `ReleaseManifest.TryReadPluginHashes` / `ParsePluginHashes` | `release-manifest.json` が空である、JSON ではない、`files` 配列がない、または `path`/`sha256` のないエントリ、16 進 64 桁ではないハッシュ、ルート付きのパス、`:` を含むパス、空・`.`・`..` のセグメントを含むパス、2 回記載されたパス（大文字と小文字を区別せず、`/` と `\` を同一視して比較）を含んでいます。UTF-8 BOM は受け入れられます。 | プラン時: `OL_E_RUNTIME_ARTIFACT_MISSING` に包まれます。開始時: `OL_E_START_SESSION` 経由 | パッケージを再インストールしてください。 |

## InvalidArgument

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_INVALID_ARGUMENT` | `LaunchValidation`、`CliInput.Parse`/`Classify` | 検証: モードが `Explicit` ではないのに `Date.Value`/`Time.Value` が設定されています。CLI: 未知のフラグ、値の欠落、不正な整数または範囲、`/saved` と `/map`/`/entrypoint` の組み合わせ、未知のコマンドルート、コードを持たない `ArgumentException`/`FormatException`。 | プラン診断。CLI エンベロープ、終了コード 2 | 引数を修正してください。 |
| `OL_E_INVALID_SETTING_VALUE` | `ConfigurationCatalog.CreatePatch`（開始時） | セマンティック設定の値が範囲外である、ブール値ではない、許可された集合に含まれない、または形式が不正です（`graphics.particles` には 4 つのフィールドが必要です）。値はプラン時には検証されません。 | `OL_E_START_SESSION` 経由のセッション診断 | [設定表](launchspec.md#environmentspec)にある値を使用してください。 |
| `OL_E_SETTING_NOT_WRITABLE` | `SessionPlanner`、`CliInput.BuildSpecAsync`、`BuildTransactionalOverlays` | キーは存在しますが書き込み不可です（`advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter`）。 | プラン診断。CLI 終了コード 2 | キーを削除してください。 |
| `OL_E_UNKNOWN_SETTING` | `SessionPlanner`、`CliInput.BuildSpecAsync`、`BuildTransactionalOverlays` | キーが `ConfigurationCatalog` にありません。 | プラン診断。CLI 終了コード 2 | カタログにあるキーを使用してください。 |

## LaunchSpec

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_SPEC_INVALID` | `LaunchSpecJson.Parse` | ルートが JSON オブジェクトではないか、デシリアライズの結果レコードが生成されませんでした。 | スロー（`InvalidDataException`）、CLI 終了コード 2 | ファイルを修正してください（[LaunchSpec](launchspec.md)）。 |
| `OL_E_SPEC_NOT_FOUND` | `LaunchSpecJson.LoadAsync` | `/spec` のファイルが存在しません。 | スロー（`FileNotFoundException`）、CLI 終了コード 6 | パスを確認してください。 |
| `OL_E_SPEC_TOO_LARGE` | `LaunchSpecJson.LoadAsync` | ファイルが 1 MiB を超えています。 | スロー（`InvalidDataException`）、CLI 終了コード 2 | ファイルを小さくしてください。 |
| `OL_E_SPEC_UNKNOWN_PROPERTY` | `LaunchSpecJson.Validate` | その位置にあるレコードの公開プロパティではないメンバーがあります。メッセージは `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name` です。 | スロー（`InvalidDataException`）、CLI 終了コード 2 | メンバーを削除するか名前を変更してください。 |

## LocalControl

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_CONTROL_COMMAND_UNKNOWN` | オーナーのハンドラー（`OwnerSession`） | コマンドが `session.status`、`session.events`、`session.stop`、または `operation` 引数付きの `runtime.execute` のいずれでもありません。 | コントロール応答。CLI 終了コード 7 | サポートされているコマンドを使用してください。 |
| `OL_E_CONTROL_FAILED` | CLI クライアント（`ReportForwarded`、`CliEventWatch`） | オーナーがエラーコードなしで `Ok = false` を応答しました。 | CLI エンベロープ、終了コード 7 | メッセージを読み、オーナーのコンソール/診断情報を確認してください。 |
| `OL_E_CONTROL_HANDLER_FAILED` | `LocalControlPlane.ServeAsync` | オーナーのハンドラーが、メッセージに `OL_E_` コードを含まない例外をスローしたか（たとえばセッションがすでに閉じられていた場合）、ハンドラーの応答をシリアライズできませんでした。 | コントロール応答 | `session status` を確認してください。オーナーが存在しない場合は再起動してください。 |
| `OL_E_CONTROL_MESSAGE_INVALID` | `LocalControlPlane`（両端） | 長さプレフィックスが負または 64 KiB 超（サイズ超過のリクエストフレームを含む）、空のフレーム、JSON `null`、`Command` のないリクエスト、またはデコードできない JSON です。 | コントロール応答 / CLI エンベロープ | ドキュメントに記載されたプロトコルを使用してください（[ローカルコントロールプレーン](local-control.md)）。 |
| `OL_E_CONTROL_MESSAGE_TOO_LARGE` | `LocalControlPlane.TryRequestAsync`（クライアント） | クライアント自身のシリアライズ済みリクエストが 64 KiB を超えています。呼び出し元に報告され、何も送信されません。 | コントロール応答 / CLI エンベロープ | リクエストを小さくしてください。 |
| `OL_E_CONTROL_RESPONSE_TOO_LARGE` | `LocalControlPlane.ServeAsync`（オーナー） | オーナーの応答が 64 KiB のフレームに収まりません。オーナーは応答を破棄する代わりに、この型付きエラーで応答します。`session.status` と `session.events` がこのエラーに至ることはありません。これらのイベント履歴は収まるよう古いものから順に削られます。 | コントロール応答 / CLI エンベロープ | 再試行してください。イベントについては、より頻繁に読み取ってください。 |
| `OL_E_CONTROL_PROTOCOL` | `LocalControlPlane`、`TryRequestBoundAsync` | リクエストの `ProtocolVersion` が `0.1` ではない、オーナーの応答をデコードできなかったか空だった、オーナーが応答せずに接続を閉じたか確立後に接続が切れた、オーナーが `SessionId` を報告しなかった、のいずれかです。 | コントロール応答 / CLI エンベロープ | クライアントとオーナーのバージョンを一致させ、`session status` を確認してください。 |
| `OL_E_CONTROL_SESSION_MISMATCH` | オーナーのハンドラー | アクティブなセッションと一致する `session_id` なしで `session.stop` または `runtime.execute` が送られました。 | コントロール応答。CLI 終了コード 7 | 先に `session.status` を読み取り、リクエストをバインドしてください（CLI はこれを自動で行います）。 |

## Other

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_PLAN_NOT_RUNNABLE` | `OmsiLaunchService.StartSessionAsync` | 渡されたプランが `IsRunnable = false` であるか、開始時の再プランニングが実行不可です（`Omsi.exe` の変更、コンテンツの削除、プラグインクロージャの欠落）。メッセージには現在の `OL_E_` コードが列挙されます。 | スロー（`InvalidOperationException`）。CLI 終了コード 1 | 再度プランを作成し、列挙された診断を修正してください。 |

## Presentation

すべて `SessionVisualAssets`（`src/OmsiLaunch.Core/SessionVisualAssets.cs`）から発生します。プラン時には `OL_E_SESSION_PRESENTATION_INVALID` に包まれ（メッセージにコードが含まれます）、開始時には `OL_E_START_SESSION` 経由で表面化します。

| コード | 意味 / 典型的な原因 | 対処 |
| --- | --- | --- |
| `OL_E_ITX_PROFILE_INVALID` | `.itx` ファイルが空である、空白でない行の数が奇数である、または URL 行が絶対 `http`/`https` URL ではありません。 | URL 行とターゲット行のペアを使用してください。 |
| `OL_E_ITX_PROFILE_MISSING` | `OverrideProfilePath`（プロセスの作業ディレクトリを基準に解決）が存在しません。`FileNotFoundException` としてスローされます。 | 存在する `.itx` のパスを渡してください。 |
| `OL_E_ITX_PROFILE_REQUIRED` | `InternetTextures.Mode` が `Override` なのに `OverrideProfilePath` がありません。スローされた場合は CLI 終了コード 2。 | `/internet-textures-profile:<file.itx>` を指定してください。 |
| `OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` | ターゲット行がルート付きである、`..` を含む、`\` で始まる、インストール環境の外に解決される、`Texture\` コンポーネントを含まない、またはジャンクション/シンボリックリンクを経由しています。 | `Texture\...` 形式の相対ターゲットを使用してください。 |
| `OL_E_SPLASH_ASSET_DIRECTORY_MISSING` | `CustomAssetDirectory` が存在しません。 | ディレクトリを修正してください。 |
| `OL_E_SPLASH_ASSET_MISSING` | アセットディレクトリに `ENG.bmp` または `<LANG>.bmp` がないか、`.omsilaunch\assets\splash` の初期配置時にパッケージ内の `assets\splash\<LANG>.bmp` がありません。 | BMP を用意するか、パッケージを再インストールしてください。 |
| `OL_E_SPLASH_FORMAT_UNSUPPORTED` | スプラッシュ用 BMP が 640×480 の 24 ビット `BM` ビットマップではありません。 | 画像を変換してください。 |

## Process

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_PROCESS_CLEANUP_FAILED` | `OmsiLaunchService`（開始失敗時およびスーパーバイザー障害時の経路） | エラー後のクリーンアップ中に、OMSI の終了処理または終了待機が例外をスローしました。内側のメッセージが後に続きます。 | セッション診断（`Failed` のセッションに追加） | `Omsi.exe` が残っていないことを確認し、ジャーナルが保留中であれば `/recover` を実行してください。 |
| `OL_E_PROCESS_CREATION_TIME_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` の直後に `GetProcessTimes` が失敗しました（`Win32=<code>`）。プロセスは終了されます。 | `OL_E_START_SESSION` 経由のセッション診断 | 再試行してください。ウイルス対策ソフト/権限を確認してください。 |
| `OL_E_PROCESS_EXITED_EARLY` | `OmsiLaunchService.SuperviseAsync` | `gameplay.entered` の前に OMSI が終了しました（クラッシュ、OMSI のエラーダイアログが閉じられた、ウィンドウが閉じられた）。 | セッション診断（`Failed`）。復元が実行されます | OMSI 自身のログと `logfile.txt` を確認し、`RuntimeEvents` で最後のプラグインイベントを確認してください。 |
| `OL_E_PROCESS_START_FAILED` | `CurrentWindowsX64Platform.StartAsync` | `CreateProcessW` が失敗しました（メッセージに `Win32=<code>`）。 | `OL_E_START_SESSION` 経由のセッション診断 | Win32 エラー（ファイルの欠落、アクセス拒否、ポリシー）を解消してください。 |
| `OL_E_PROCESS_SUPERVISION` | `OmsiLaunchService.SuperviseAsync` | スーパーバイザーのループが例外をスローしました（テレメトリの読み取り、プロセスの待機/終了、ジャーナルの書き込み）。OMSI は終了され、復元が試みられます。 | セッション診断（`Failed`） | 内側のメッセージとホストログを確認してください。 |
| `OL_E_PROCESS_TERMINATE_FAILED` | `CurrentWindowsX64Platform.Terminate` | `TerminateProcess` が失敗しました（`Win32=<code>`）。 | `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` のメッセージ内 | OMSI を手動で終了してから `/recover` を実行してください。 |
| `OL_E_PROCESS_WAIT_FAILED` | `CurrentWindowsX64Platform.WaitForExitAsync` | プロセスハンドルに対する `WaitForSingleObject` が失敗しました。 | `OL_E_PROCESS_SUPERVISION` / `OL_E_PROCESS_CLEANUP_FAILED` のメッセージ内 | 同上。 |

## Runtime

「ランタイム詳細」とは、具体的なコードが `ErrorCode` に入り、メッセージが `Values["detail"]`、例外の型が `Values["exception"]` に入ることを意味します。

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED` | `OmsiCameraLockWriter` | `family` が 2（外部）または 3（マップ）のときに `preset` 付きで `camera.lock` が呼ばれました。プリセットは運転士（0）と乗客（1）にのみ存在します。 | ランタイム詳細 | `preset` を省略するか、family 0/1 を使用してください。 |
| `OL_E_DATE_TIME_APPLY_FAILED` | `LaunchValidation` | `Date`/`Time` のモードが `Explicit` なのに値がないか、構成要素が範囲外です。名前は歴史的な経緯によるもので、実際はプラン時の検証エラーです。 | プラン診断 | 値を修正してください（なお、このビルドでは明示的な日付/時刻は実行不可です）。 |
| `OL_E_MAKEVEHICLE_BUS_NOT_FOUND` | `CurrentRuntimeControl.MakeBasicRoadVehicle` | `road-vehicles.spawn`: `.bus` のパスが OMSI の作業ディレクトリ配下に存在しません（OMSI が代替車両に置き換えられないよう、ネイティブ呼び出しの前にチェックされます）。 | ランタイム詳細 | `vehicles list`/`/list:vehicles` で得た識別子を使用してください。 |
| `OL_E_MAKEVEHICLE_DELTA_MULTIPLE` | 同上 | ネイティブの MakeVehicle によって、道路車両コレクションが 2 つ以上のオブジェクト分変化しました。 | ランタイム詳細（`native_status`、メッセージ内に件数） | 報告してください。作成されたオブジェクトはセッション終了まで残ります。 |
| `OL_E_MAKEVEHICLE_DELTA_ZERO` | 同上 | コレクションが変化しませんでした。OMSI が車両を黙って拒否しました。 | ランタイム詳細 | `.bus` ファイルを確認し、別のモデルを試してください。 |
| `OL_E_MAKEVEHICLE_NATIVE_FAILED` | 同上 | その他の 0 以外のネイティブステータスです。 | ランタイム詳細 | メッセージ内の件数を添えて報告してください。 |
| `OL_E_PLACE_RANDOM_BUS_FAILED` | `CurrentRuntimeControl.PlaceRandomBus` | プロファイル済みの PlaceRandomBus 呼び出しが失敗ステータスを返しました。 | ランタイム詳細 | ゲームプレイが安定してから再試行し、報告してください。 |
| `OL_E_RUNTIME_ARGUMENT_REQUIRED` | `PublicCapabilityRegistry.ValidateRuntimeArguments`、プラグイン側のチェック（`hour`/`minute`/`second` のない `time.set`、`family`/`field_of_view` のない `camera.set`、解析可能な `family` のない `camera.lock`、車両/カーブ操作） | 必須の引数がないか空です。 | ランタイム結果（レジストリ。CLI 終了コード 2）またはランタイム詳細（プラグイン） | 引数を指定してください（[ランタイム制御](runtime-control.md)）。 |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | `OmsiLaunchService.PlanSessionAsync` | `OmsiLaunchRuntimePaths` で指定されたプラグインクロージャの参照ディレクトリ/ファイル、またはネイティブブリッジを読み込めません（`OL_E_RELEASE_MANIFEST_INVALID` を包むことがあります）。 | プラン診断 | 破損していないパッケージから実行してください。 |
| `OL_E_RUNTIME_BASELINE_UNAVAILABLE` | `RuntimeBatch`（`/runtime-write-batch`、INTERNAL のハーネス） | ベースラインの `time.read`/`camera.read` が失敗したため、書き込みテストがスキップされました。 | バッチの成果物のみ | 利用者向けではありません。 |
| `OL_E_RUNTIME_BUS_IDENTITY_INVALID` | `CurrentRuntimeControl.ValidateBasicBusIdentity` | `model` が空である、240 文字を超える、NUL または `..` を含む、`Vehicles\` で始まらない、または `.bus` で終わりません。 | ランタイム詳細 | `Vehicles\<dir>\<file>.bus` を渡してください。 |
| `OL_E_RUNTIME_CHANNEL_BUSY` | なし（互換性のために保持） | 現在は発行されません。以前のビルドでは、キャンセルされたリクエストがスロットに残った場合に発生していました。現在はリクエストのすべての終端経路でスロットがリセットされ、新しいリクエストの開始時に残っていたリクエストや応答はクリアされます。 | — | — |
| `OL_E_RUNTIME_CHANNEL_CLOSED` | `OmsiLaunchService.LiveSession.RequestRuntimeAsync` | セッションが終了中のため、メールボックスが破棄されました。 | スロー（`InvalidOperationException`） | 対処は不要です。セッションは終了しています。 |
| `OL_E_RUNTIME_CHANNEL_STATE_INVALID` | `CurrentRuntimeCommandStore.RequestAsync` | メールボックススロットが、アイドル・リクエスト済み・応答済みのいずれでもない状態値を保持していました（破損）。スロットはリセットされ、エラーが発生します。次のリクエストは正常に動作します。 | スロー（`InvalidDataException`） | 再試行してください。繰り返し発生する場合は報告してください。 |
| `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` | `OmsiRuntimeReaders` | 車両の定数ブロックへのポインターが null です。 | ランタイム詳細 | 車両に定数がありません。対処は不要です。 |
| `OL_E_RUNTIME_CONSTANT_NOT_FOUND` | `OmsiRuntimeReaders` | `name` が車両の定数テーブルにありません。 | ランタイム詳細 | 先に定数を一覧表示してください。 |
| `OL_E_RUNTIME_CREATED_OBJECT_INVALID` | `OmsiRuntimeReaders.RegisterRoadVehicleHandleAsync` | スポーンで作成されたオブジェクトの VMT が OMSI イメージの範囲外にあります。 | ランタイム詳細 | 報告してください。 |
| `OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION` | 同上 | 作成されたオブジェクトが道路車両コレクションにありません。 | ランタイム詳細 | 報告してください。 |
| `OL_E_RUNTIME_CURVE_DEGENERATE` | `OmsiRuntimeReaders.EvaluateRoadVehicleCurveAsync` | 連続する 2 つのカーブ点が同じ X を持っています。 | ランタイム詳細 | 車両のカーブにあるコンテンツ側の問題です。 |
| `OL_E_RUNTIME_CURVE_EMPTY` | 同上 | カーブに点がありません。 | ランタイム詳細 | 同上。 |
| `OL_E_RUNTIME_CURVE_INVALID` | 同上 | `x` を含むカーブの区間がありません。 | ランタイム詳細 | カーブの定義域内で評価してください。 |
| `OL_E_RUNTIME_CURVE_NOT_FOUND` | 同上 | `name` が未知であるか、その関数ポインターが null です。 | ランタイム詳細 | 先にカーブを一覧表示してください。 |
| `OL_E_RUNTIME_HOF_UNAVAILABLE` | `OmsiRuntimeReaders.ReadRoadVehicleHofsAsync` | 車両定義へのポインターが null です。 | ランタイム詳細 | ハンドルが定義データのない車両を指しています。 |
| `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` | `CliProgram.RunAsync`（オーナーモード） | 実行ファイルと同じ場所に `plugins\OmsiLaunch.Plugin.opl` または `plugins\OmsiLaunch.Native.x86.dll` がありません。 | CLI エンベロープ、終了コード 7 | パッケージを再インストールしてください。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED` | `CurrentRuntimeControl` | `road-vehicle.read`、`human.read`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list`、`vehicle.curves.list` で `handle` がないか空です（通常は、レジストリが先に `OL_E_RUNTIME_ARGUMENT_REQUIRED` で拒否します）。 | ランタイム詳細 | ハンドルを指定してください。 |
| `OL_E_RUNTIME_OBJECT_HANDLE_STALE` | `OmsiRuntimeReaders` | ハンドルが未知である、オブジェクトがコレクションから外れた、アドレスの世代が進んだ、またはアドレスが再利用されたためにオブジェクトのフィンガープリント（VMT + 定義/モデルの識別子）が変化しました。残存する死角: 2 回のリスト読み取りの間に、同じクラスとモデルのオブジェクトが同じアドレスに再作成された場合です。 | ランタイム詳細 | `road-vehicles.list`/`humans.list` を再実行し、新しいハンドルを使用してください。 |
| `OL_E_RUNTIME_OPERATION_FAILED` | `CurrentRuntimeControl.Execute`、`CurrentRuntimeCommandMailbox.TryDispatch`、`D3DRuntimeApi` のフォールバック | プラグイン側の汎用的な失敗ラッパーです。`Values["detail"]` にメッセージ（多くの場合より具体的なコード）、`Values["exception"]` に例外の型が入ります。メールボックスのディスパッチャー内で操作から漏れた例外にも、リクエストを未応答のまま残す代わりに、このコード（値なし）で応答します。 | ランタイム結果 | `detail` を確認してください。 |
| `OL_E_RUNTIME_OPERATION_UNAVAILABLE` | `CurrentRuntimeControl.Execute` | レジストリが許可した操作の実装がプラグインにありません（レジストリとプラグインのバージョンのずれ）。 | ランタイム詳細 | 整合性のあるパッケージを再インストールしてください。 |
| `OL_E_RUNTIME_OPERATION_UNKNOWN` | `PublicCapabilityRegistry.ValidateRuntimeArguments` | 操作が `PublicRuntimeOperationIds` にありません。すべての `internal.*` 操作も該当します。セッションの検索前にチェックされます。 | ランタイム結果、コントロール応答。CLI 終了コード 2 | 公開の operation id を使用してください。 |
| `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` | `OmsiCameraLockWriter` | プレイヤー車両がない状態で `preset` 付きの `camera.lock` が呼ばれました（ヘッドレスセッションにはプレイヤー車両がありません）。再適用が失敗したときの `camera.lock.degraded` イベントの `code` としても報告されます。 | ランタイム詳細 / ランタイムイベント | プリセットなしでロックするか、プレイヤー車両のあるセッションを使用してください。 |
| `OL_E_RUNTIME_PROTOCOL_MISMATCH` | `D3DRuntimeApi` | 成功した D3D の結果に値がないか、未知のデバイス状態文字列が含まれていました。 | スロー（`OmsiRuntimeException`） | ホストとプラグインのバージョンを一致させてください。 |
| `OL_E_RUNTIME_REQUEST_ID_REUSED` | `CurrentRuntimeCommandStore.RequestAsync` | 新しいリクエストと同じリクエスト ID を持つ古い応答がスロットに残っています。エラーの発生前に古い応答はクリアされます。 | スロー（`InvalidOperationException`） | 厳密に増加するリクエスト ID を使用してください。 |
| `OL_E_RUNTIME_REQUEST_TIMEOUT` | `CurrentRuntimeCommandStore.RequestAsync` | `timeout` 内に応答がありませんでした。スロットはリセットされ、遅れて届いた応答は破棄されます。 | スロー（`TimeoutException`）。CLI 終了コード 5 | より長いタイムアウトで再試行してください。OMSI がブロックされていないか（モーダルダイアログ、読み込み中）確認してください。 |
| `OL_E_RUNTIME_RESPONSE_INVALID` | `CurrentRuntimeCommandStore` | 応答エンベロープが破損している、長さが不正（負、0、またはスロットより大きい）、別のセッション ID、または異なるリクエスト ID を持っています。エラーの発生前にスロットがリセットされるため、次のリクエストは正常に動作します。 | スロー（`InvalidDataException`） | 再試行してください。繰り返し発生する場合は報告してください。 |
| `OL_E_RUNTIME_CURSOR_INVALID` | `ListPage`（プラグイン） | リスト操作の `cursor` の形式が不正、または別の種類のリストのものです。 | ランタイム詳細 | 同じ操作の前のページの `next_cursor` を渡すか、省略してください。 |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` | `CurrentRuntimeCommandMailbox.TryDispatch` | シリアライズされた結果が 64 KiB のメールボックスを超えています。ページング対象のリスト結果は、代わりに収まるよう短縮され、`next_cursor` で続きを取得します。どのリスト結果もこのコードには到達しません。 | ランタイム結果 | より範囲の狭い操作か、より小さい `limit` を使用してください。 |
| `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` | `OmsiRuntimeReaders` | 車両のスクリプト定義または状態へのポインターが null です。 | ランタイム詳細 | 車両にスクリプトオブジェクトがありません。 |
| `OL_E_RUNTIME_SESSION_MISMATCH` | `OmsiLaunchService.ExecuteRuntimeAsync`、`CurrentRuntimeCommandStore`、プラグインのメールボックス | `RuntimeCommand.SessionId` がハンドルのセッション ID と異なる（ホストがスロー）か、別のセッションにバインドされたプラグインにリクエストが届きました（プラグインが型付きの結果として返します）。 | スロー（`InvalidOperationException`）/ ランタイム結果 | `session.SessionId` を使ってコマンドを構築してください。 |
| `OL_E_WEATHER_APPLY_FAILED` | `PluginRuntime`（`world.failed` イベントの `code`） | ワールドの開始後に、開始時の天候を適用できませんでした。`/weather-real` に対して OMSI に ICAO 観測所が設定されていない、または天候オブジェクトが利用できません。セッションは失敗します。 | セッションの失敗 | OMSI に観測所を設定するか、`/weather-icao` を使用してください。 |
| `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` | `OmsiWeatherWriter.Prepare` | `weather.set` のキーが、OMSI が天候ティックごとに導出する値（湿度、光の係数、雲の透過度、降水フラグ、濡れた地面）を指しています。何も書き込まれません。 | ランタイム結果 | `detail` に示されたソースのキーを設定してください（たとえば `relative_humidity` の代わりに `dew_point`）。 |
| `OL_E_RUNTIME_SETTING_UNAVAILABLE` | `OmsiWeatherWriter.Prepare` | 未知の `weather.set` キーです。何も書き込まれません。 | ランタイム結果 | [ケイパビリティ](capabilities.md#weather)に一覧されたキーを使用してください。 |
| `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` が文字列変数テーブルにありません。 | ランタイム詳細 | 先に文字列変数を一覧表示してください。 |
| `OL_E_RUNTIME_VALUE_INVALID` | `OmsiWeatherWriter.ParseBoolean` | 天候のブール値が `true`/`false`/`1`/`0` のいずれでもありません。現在は到達不能です（上記参照）。 | ランタイム詳細（定義のみ） | ソースを参照してください。 |
| `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `CurrentRuntimeControl`、`OmsiCameraWriter`、`OmsiCameraLockWriter`、`OmsiRuntimeReaders`、`OmsiWeatherWriter` | `time.set`: `hour` 0..23、`minute` 0..59、`second` 0..59.999。`camera.set`: `family` 0..3、`field_of_view` 10..170。`camera.lock`: `preset` が整数ではない、`family` 0..3、`preset` 0..255。`road-vehicles.place-random`: `ai_type` 0..255、`group`/`type`/`tour`/`line` 0..65535（`type` は -1 も可）、`scheduled` 0..1。`vehicle.variable.set`: `value` が有限値ではない。`vehicle.curve.evaluate`: `x` が有限値ではない。 | ランタイム詳細 | 範囲内の値を使用してください。 |
| `OL_E_RUNTIME_VARIABLE_NOT_FOUND` | `OmsiRuntimeReaders` | `name` が数値変数テーブルにありません。 | ランタイム詳細 | 先に変数を一覧表示してください。 |
| `OL_E_RUNTIME_VARIABLE_UNAVAILABLE` | `OmsiRuntimeReaders` | 変数スロットまたは値のアドレスが null です。 | ランタイム詳細 | この車両では変数が実体化されていません。 |
| `OL_E_TIME_APPLY_FAILED` | `CurrentRuntimeControl.SetTime` | 時計のスカラー値は書き込まれましたが、プロファイル済みのネイティブ SetTime 呼び出しが失敗を返しました。 | ランタイム詳細 | 再試行し、`time.read` で読み戻してください。 |

## RuntimeD3D

すべて `CurrentRuntimeControl` から発生します（ネイティブステータスの `ThrowD3D` による対応付け。`detail` には操作と HRESULT、`native_status` には数値ステータスが入ります）。経路: `ErrorCode` にコードを持つランタイム結果です。`D3DRuntimeApi` は `OmsiRuntimeException` として再スローします。

| コード | ネイティブステータス / 原因 | 対処 |
| --- | --- | --- |
| `OL_E_D3D_DEVICE_LOST` | 8: Direct3D デバイスが失われています。 | `d3d.restored` を待ち、テクスチャを再作成してください（世代が変わっています）。 |
| `OL_E_D3D_INVALID_ARGUMENT` | 14: `width`、`height`、`level`、`x`、`y`、`format` または `handle` がないか不正です（範囲: width/height 1..4096、levels 0..16、level 0..15、x/y 0..4095）。 | 引数を修正してください。 |
| `OL_E_D3D_INVALID_PIXEL_BUFFER` | `pixels_base64` が有効な Base64 ではないか、48 KiB を超えています。 | より小さい矩形を送信してください。 |
| `OL_E_D3D_INVALID_TEXTURE_FORMAT` | 6、または未知の `format` 名（有効な値: `A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）。 | 列挙されたフォーマットを使用してください。 |
| `OL_E_D3D_NATIVE_CALL_FAILED` | その他のステータスです。HRESULT は `detail` に入ります。 | HRESULT を添えて報告してください。 |
| `OL_E_D3D_NOT_READY` | 7: デバイスの準備ができていません（最初のフレームの前、または停止中）。 | `d3d.ready` の後に再試行してください。 |
| `OL_E_D3D_RESET_IN_PROGRESS` | 9: デバイスリセットが進行中です。 | `d3d.restored` の後に再試行してください。 |
| `OL_E_D3D_RESOURCE_RELEASED` | 13: テクスチャハンドルはすでに解放されています。 | 解放済みのハンドルを再利用しないでください。 |
| `OL_E_D3D_STALE_RESOURCE_HANDLE` | 12: ハンドルが以前のデバイス世代に属しています。または、ハンドル文字列が `d3dtex-<session>-<hex>` の形式ではないか 0 です。 | テクスチャを再作成してください。 |

## Session

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_CAPABILITY_UNAVAILABLE` | `SessionPlanner`、`plugin.request.unsupported` 受信時の `ApplyTelemetry` | プラン: `LastMapState`、`EntrypointIdentity`、日付/時刻/年のモード、天候モード、プレイヤー車両のフィールド、または入力ドキュメントが要求されました（`Requested capability unavailable: <name>`）。テレメトリ: プラグインがハンドオフを拒否しました（実行可能なプランでは発生しません）。 | プラン診断、セッション診断 | サポートされていない要求を削除してください（[既知の制限事項](known-limitations.md)）。 |
| `OL_E_HEADLESS_ARM_FAILED` | `headless.arm.failed` 受信時の `ApplyTelemetry` | プラグインが OMSI 内に 1 回限りのヘッドレス開始フックを設定できませんでした。 | セッション診断（`Failed`） | ビルドを確認し、報告してください。 |
| `OL_E_NO_ACTIVE_SESSION` | CLI クライアント（`ReportForwarded`、`CliEventWatch`） | インストール環境のコントロールパイプで応答するオーナーがいません（セッションがない、またはオーナーがまだ起動中/検証中）。 | CLI エンベロープ、終了コード 4 | セッションを開始するか、`Running` になるまで待ってください。 |
| `OL_E_PLUGIN_NOT_LOADED` | `SuperviseAsync` | `plugin.started` の前に `StartupTimeoutSeconds` が経過しました（OMSI が `plugins\OmsiLaunch.Plugin.opl` を読み込まなかったか、プラグイン初期化の前で停止しています）。 | セッション診断（`Failed`） | プラグインクロージャ、`plugins\OmsiLaunch.Plugin.opl`、OMSI の `logfile.txt` を確認してください。 |
| `OL_E_PLUGIN_PROTOCOL_MISMATCH` | `plugin.handoff.invalid` 受信時、または解析できないテレメトリ JSON に対する `ApplyTelemetry` | プラグインが起動時のハンドオフ（バージョン 3 から 6、SHA-256）を読み取り/検証できなかったか、不正なテレメトリを送信しました。 | セッション診断（`Failed`） | ホストとプラグインのバージョンを一致させてください（パッケージを再インストール）。 |
| `OL_E_SESSION_ALREADY_ACTIVE` | `CliProgram.RunAsync` | このインストール環境では、すでにオーナーが `session.status` に応答しています。 | CLI エンベロープ、終了コード 7 | クライアントコマンド（`session status`、`session stop`、ランタイムコマンド）を使用してください。 |
| `OL_E_SESSION_NOT_RUNNING` | `OmsiLaunchService.ExecuteRuntimeAsync` | セッション状態が `Running` ではありません。 | スロー（`InvalidOperationException`） | 先に `WaitForAsync(session, SessionState.Running, ...)` を実行してください。 |
| `OL_E_SESSION_PRESENTATION_INVALID` | `SessionPlanner` | スプラッシュ/ITX のプランを構築できませんでした。メッセージにプレゼンテーションのコードが含まれます。 | プラン診断 | [Presentation](#presentation) を参照してください。 |
| `OL_E_SESSION_START_FAILED` | `WindowsHost.ShowFailure`（OmsiLaunchW のダイアログ） | 起動プランが実行不可であるか、セッションがゲームプレイに到達せず、かつ `OL_E_` の診断が存在しない場合に表示されるフォールバックコードです。 | メッセージボックスのみ | `.omsilaunch\diagnostics` を確認してください。 |
| `OL_E_SITUATION_LOAD_FAILED` | `world.situation.failed` 受信時の `ApplyTelemetry` | ネイティブの保存済みシチュエーション開始処理が失敗を返しました（イベント内に `native_status`）。 | セッション診断（`Failed`） | `.osn` とそのマップを確認してください。 |
| `OL_E_STARTUP_TIMEOUT` | `SuperviseAsync` | プラグインは開始しましたが、`StartupTimeoutSeconds` 内に `Running` に到達しませんでした。 | セッション診断（`Failed`） | 大規模なマップでは `/startup-timeout` を増やしてください。`RuntimeEvents` で最後のワールドイベントを確認してください。 |
| `OL_E_START_SESSION` | `OmsiLaunchService.StartAsync` | 開始経路で発生したあらゆる例外です。メッセージは内側のメッセージです（通常は内側のコードで始まります）。 | セッション診断（`Failed`） | 内側のコードに応じて対処してください。 |
| `OL_E_WORLD_START_FAILED` | `world.failed` 受信時の `ApplyTelemetry` | ネイティブの NEW_MAP 開始処理が失敗を返しました（イベント内に `native_status`）。 | セッション診断（`Failed`） | マップ、エントリポイントのインデックス、OMSI のログを確認してください。 |

## SessionProfile

すべて `SessionProfileCompiler`（`src/OmsiLaunch.Core/SessionProfiles.cs`）または `CliInput` から発生し、`SessionProfileException`（`Code` を持つ `IOException`）としてスローされます。CLI 終了コードは 2 です。[セッションプロファイル](session-profiles.md)を参照してください。

| コード | 意味 / 典型的な原因 | 対処 |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | プリセットのスプラッシュ用 `assets` ディレクトリ、または internet-textures の `profile` ファイルがパッケージ内に存在しません。 | アセットを追加してください。 |
| `OL_E_SESSION_PROFILE_INVALID` | 構造または制限の違反です。256 KiB 超、ルートマッピングがちょうど 1 つではない、YAML アンカー、未知のキー、必須キーの欠落、スカラーが必要な箇所に非スカラー、`id` がディレクトリ名と異なる、プリセットが 1..99 ではないか `index` が重複、正でないタイムアウト、サポートされていない天候/スプラッシュ/internet-textures のモード、日付/時刻が `explicit` ではない、不正な YAML、数値/日付の解析エラー。 | メッセージに従って YAML を修正してください。 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` が空ではなく、選択したマップ（NEW_MAP）または選択したシチュエーションのマップ（SAVED_SITUATION）を含んでいません。 | 互換性のあるワールドを選択してください。 |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` が存在しません。 | id を確認してください。 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 明示的な CLI 引数が、選択したプロファイル/プリセットの管理するフィールドを対象としています。 | フラグを外すか、別のプリセットを選択してください。 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | id に `\`、`/`、`:` または `..` が含まれています。または、アセットのパスがルート付きである、パッケージの外に出る、ジャンクション/シンボリックリンクを経由しています。 | パスをパッケージ内に収めてください。 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` がない、1..99 の範囲外、またはプロファイルで宣言されていません。 | 宣言されたプリセットのインデックスを使用してください。 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` が `omsilaunch.session-profile/v1` ではありません。 | サポートされているスキーマを使用してください。 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | プリセットの設定キーは既知ですが、書き込み不可です。 | キーを削除してください。 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | プリセットの設定キーがカタログにありません。 | カタログにあるキーを使用してください。 |

## Transaction

[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照してください。

| コード | 発生元 | 意味 / 典型的な原因 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_CLOSECHECK_REMOVE_FAILED` | `OmsiLaunchService.RemoveStaleClosecheck` | `File.Delete` の後も古い `closecheck` が残っています。 | `OL_E_START_SESSION` 経由のセッション診断 | `<root>\closecheck` を手動で削除してください（権限）。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH` | `FileConfigurationTransaction.RestoreAsync` | セッション前には存在しなかったパスに、セッションが適用した内容と異なる内容が存在します。そのパスは削除されず、ジャーナルは保持されます。 | スロー（`IOException`）。`OL_E_RESTORE_FAILED` / `OL_E_START_SESSION` の内側。CLI 終了コード 8 | ファイルを調べて削除または移動してから、`/recover` を実行してください。 |
| `OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED` | `RestoreAsync`（フィンガープリント導入前のジャーナル） | もともと存在せず、削除対象でもないパスについて、ジャーナルに適用済み内容のフィンガープリントがないため、所有関係を証明できません。セッション開始時にはリカバリが延期され、このセッションのプラン済みバイト列を使って再試行されます。`RecoverPendingAsync` 経由ではスローされます。 | スロー（`IOException`）。CLI 終了コード 8 | 同じ spec でセッションを開始する（バイト列が提供されます）か、ファイルを調べて削除してから `/recover` を実行してください。 |
| `OL_E_RECOVERY_BACKUP_CORRUPT` | `RestoreAsync` | バックアップの SHA-256 がジャーナルに記録されたスナップショットと一致しません。何も書き込まれません。 | スロー。`OL_E_RESTORE_FAILED` の内側。CLI 終了コード 8 | 利用者自身のバックアップからファイルを復元してください。ジャーナルの削除は確信がある場合にのみ行ってください。 |
| `OL_E_RECOVERY_JOURNAL_MISSING` | `RestoreAsync` | スナップショットはメモリ上に存在しますが、`journal.json` がなくなっています（セッション中に削除されました）。 | スロー。`OL_E_RESTORE_FAILED` の内側 | セッションのファイルを手動で確認してください。 |
| `OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` | `FileConfigurationTransaction.RemoveJournal` | 削除後も `journal.json` が残っています（復元自体は成功し、検証済みです）。 | スロー。`OL_E_RESTORE_FAILED` の内側。CLI 終了コード 8 | `<root>\.omsilaunch\journal.json` を削除する（権限）か、`/recover` を再実行してください（べき等です）。 |
| `OL_E_RESTORE_DEFERRED` | `OmsiLaunchService`（開始失敗時 / スーパーバイザー） | OMSI の終了を確認できなかったため、OMSI がまだ使用している可能性のあるファイルは置き換えられませんでした。ジャーナルは保持されます。 | セッション診断（`Failed`） | `Omsi.exe` が終了した後に `/recover` を実行してください（または次回の開始時に自動的にリカバリされます）。 |
| `OL_E_RESTORE_FAILED` | `OmsiLaunchService`（開始失敗時 / スーパーバイザー） | `RestoreAsync` が例外をスローしました。メッセージに内側のコードが含まれます。ジャーナルは保持されます。 | セッション診断（`Failed`）。`/recover` からスローされた場合は CLI 終了コード 8 | 内側のコードに応じて対処してから、`/recover` を実行してください。 |

## Warning

| コード | 発生元 | 意味 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_W_BUILD_UNRECOGNIZED` | `SessionPlanner` | OMSI 2 の実行ファイルに既知のレイアウトがありません（そのコード ID がビルドカタログにありません）。プランは実行可能のままです。すべてのネイティブケイパビリティはプロセス内で解決され検証され、検証できないものは利用できません。 | プラン診断（実行可否には影響しません） | 対処は不要です。予測されるケイパビリティについては `/plan` と `profiles` を確認してください。 |
| `OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE` | `OmsiLaunchService`（スーパーバイザー） | `Behavior.CooperativeShutdown` が要求されましたが、OMSI に終了を依頼できなかった（このビルドでは終了ルーチンが未解決、または OMSI がすでに終了処理中）か、`ShutdownTimeoutSeconds` 以内に終了しなかったため、OMSI は強制終了されました。復元は通常どおり実行されます。 | セッション診断 | なし: セッションは強制終了の場合と同様に終了します。 |
| `OL_W_CAPABILITY_PENDING` | `SessionPlanner` | 開始に必要なケイパビリティを実行ファイルから解決できませんでした（たとえば、読み込まれたイメージがファイルと異なる DRM 保護の実行ファイル）。開始時にプロセス内で解決されます。 | プラン診断（実行可否には影響しません） | 対処は不要です。プロセス内でも解決できない場合、開始は `OL_E_CAPABILITY_UNRESOLVED` で失敗します。 |
| `OL_W_MODULE_NOT_LOADED` | `SessionPlanner` | 自らセッションに加わったモジュール（設定ファイル、コンテンツ、または既定のアクティベーション）を読み込めません（`Data["code"]` が理由を、`Data["activation"]` が加わった方法を示します）。 | プラン診断（実行可否には影響しません） | モジュールを修正、無効化、または削除してください。 |
| `OL_W_MODULE_SETTING_IGNORED` | `SessionPlanner`、`ModuleSettingsFile`、`ModuleSettings` | `.omsilaunch\modules.ini` のエントリを使用できません（モジュールのセクションではない、セクションの外にある、未知のキー、無効な値。既定値が使用されます）。または、セッションが読み込まないモジュールの設定が要求されました。 | プラン診断（実行可否には影響しません） | エントリまたはリクエストを修正してください。 |
| `OL_W_RESTORE_FOREIGN_FILE_RETAINED` | `FileConfigurationTransaction.RestoreAsync` | セッションによる削除対象のパス（ITX ターゲット、`Texture\standard.ipr`、`closecheck`）がセッション前には存在せず現在は存在しますが、このジャーナルの下で OMSI プロセスが一度も開始されていないため、そのファイルはセッションの副産物ではあり得ません。ファイルは保持されて報告され（メッセージ = 相対パス、`Data["sha256"]`）、トランザクションはそのまま完了します。 | セッション診断 / `RecoveryStatus.Diagnostics`（状態には影響しません） | ファイルを調べ、不要であれば自分で削除してください。 |

## Module

ランタイムモジュール（[ランタイムモジュール](../../../reference/runtime-modules.md)を参照）。プラン作成時、コードはプラン診断です。要求されたモジュールではプランが実行不可になり、自らセッションに加わったモジュール（設定ファイル、コンテンツ、既定）では代わりにプランが `OL_W_MODULE_NOT_LOADED` を報告します。セッション中、コードはセッション診断です（コードが `OL_E_MODULE_SESSION_FAILED` でない限りセッションは継続します）。モジュールの操作ではランタイム結果になります。

| コード | 発生元 | 意味 | 経路 | 対処 |
| --- | --- | --- | --- | --- |
| `OL_E_MODULE_MANIFEST_INVALID` | `ModuleManifestParser`、`ModuleCatalog` | `module.json` がない、有効な `omsilaunch.runtime-module/v1` ではない、または `id` がディレクトリ名と異なります。メッセージがフィールドを示します。 | プラン診断、`ListRuntimeModulesAsync` | マニフェストを修正してください。 |
| `OL_E_MODULE_NOT_FOUND` | `ModuleEligibility` | 要求されたモジュール ID が `plugins\OmsiLaunch.Modules` 配下にインストールされていません。 | プラン診断 | `/list:RuntimeModules` を確認してください。 |
| `OL_E_MODULE_INTEGRITY` | `ModuleCatalog`、`ModuleHost` | 固定されたファイルがない、固定された SHA-256 と異なる、またはリパースポイント経由で到達しています。あるいはプロセス内で、プラン作成後に `module.json` が変更されました。 | プラン診断、セッション診断 | モジュールを再インストールしてください。 |
| `OL_E_MODULE_PROTOCOL_UNSUPPORTED` | `ModuleEligibility` | モジュールの `bridge_protocol` の範囲に、このビルドが提供するプロトコル（1 と 2）が 1 つも含まれていません。 | プラン診断 | この OmsiLaunch 向けに作られたバージョンのモジュールを使用してください。 |
| `OL_E_MODULE_CAPABILITY_UNRESOLVED` | `ModuleEligibility`、`ModuleHost` | 必要なケイパビリティが、このビルドに未知、このインストール環境で利用不可、またはプロセス内で未解決です。 | プラン診断、セッション診断 | [ケイパビリティ](capabilities.md)と `profiles` を参照してください。 |
| `OL_E_MODULE_CONTENT_BINDING` | `ModuleEligibility`、`ModuleHost` | セッションがモジュールのバインド先のマップを実行していない、またはバインドされたファイルが固定された SHA-256 と異なります。 | プラン診断、セッション診断 | バインド先のコンテンツでバインド先のマップを開始してください。 |
| `OL_E_MODULE_COMPONENT_UNSUPPORTED` | `ModuleHost` | モジュールが、自身の補助コンポーネントではないファイルの起動を要求しました。 | モジュール内のランタイム結果 | モジュールの不具合です。 |
| `OL_E_MODULE_LOAD_FAILED` | `ModuleHost`、`ModuleSessionPrograms` | マネージドコンポーネントを読み込めない、またはそのエントリ型を作成できません。あるいはモジュールのセッションプログラムを開始できない、または準備完了前に終了しました。 | セッション診断 | モジュールのビルドを確認してください（x86 互換であること、エントリ型が `IRuntimeModule` を実装していること）。 |
| `OL_E_MODULE_PHASE_FAILED` | `ModuleHost` | `Arm`、`GameplayReady` または `Stop` が例外をスローしました。 | セッション診断 | モジュールの不具合です。その `failure_policy` が適用されました。 |
| `OL_E_MODULE_PHASE_TIMEOUT` | `ModuleHost`、`ModuleSessionPrograms` | フェーズまたは操作が予算（`timeouts`、`timeout_ms`）を超えて戻った、またはセッションプログラムが `prepare_ms` 以内に準備完了になりませんでした。 | セッション診断、ランタイム結果 | モジュールの不具合です。その `failure_policy` が適用されました。 |
| `OL_E_MODULE_DISABLED` | `ModuleHost` | モジュールがセッションの前半で無効化された、または失敗しました。 | ランタイム結果 | 最初の失敗についてはセッション診断を参照してください。 |
| `OL_E_MODULE_NOT_LOADED` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | セッションがこのモジュールを読み込んでいない、またはまだ準備ができていません（ゲームプレイの前）。 | ランタイム結果 | 開始時にモジュールを要求してください。`Running` を待ってください。 |
| `OL_E_MODULE_OPERATION_UNKNOWN` | `OmsiLaunchService.ExecuteRuntimeAsync`、`ModuleHost` | モジュールにそのような操作は宣言されていません。 | ランタイム結果 | モジュールの `operations` を確認してください。 |
| `OL_E_MODULE_OPERATION_FAILED` | `ModuleHost` | カタログに載っているコードなしで操作が失敗しました。モジュール独自のコードは `Values["module_code"]` にあります。 | ランタイム結果 | モジュール固有です。 |
| `OL_E_MODULE_CLEANUP_INCOMPLETE` | `ModuleHost` | 停止または失敗の後も、追跡対象のリソースがまだ生きていた（終了しなかった補助プロセス、終了しなかったバックグラウンドスレッド）か、その解放が例外をスローしました。 | セッション診断 | モジュールを報告してください。プロセスはいずれにせよ OMSI とともに終了します。 |
| `OL_E_MODULE_SESSION_FAILED` | `OmsiLaunchService` | `failure_policy` が `fail-session` のモジュールが失敗しました。セッションは停止され、復元されます。 | セッション診断（状態 `Failed`） | 直前のモジュールコードを参照してください。 |
| `OL_E_MODULE_SETTING_INVALID` | `ModuleEligibility`、`ModuleSettings`、CLI | 要求されたモジュール設定が無効、またはモジュールが宣言していません。あるいはモジュールが有効化と無効化の両方をされている、設定名が `<module-id>.<key>` ではない、または複数のモジュールが同じモジュールフラグを宣言しています。 | プラン診断（実行不可）、CLI エラー | リクエストを修正してください。`/list:RuntimeModules` が各モジュールの設定を示します。 |
| `OL_E_MODULE_CONFLICT` | `ModuleEligibility`、`ModuleHost` | モジュールが `conflicts.plugins` に挙げている OMSI プラグインが有効（`plugins\*.opl`）、または OMSI に読み込まれています。理由に `.opl` が示されます。 | プラン診断、セッション診断 | そのプラグインを無効化または削除してください。OmsiLaunch がそれを変更することはありません。 |
| `OL_E_MODULE_HOOK_UNAVAILABLE` | 予約済み | 予約済みです。ネイティブコンポーネントは代わりに、[ランタイムモジュール](../../../reference/runtime-modules.md#native-components-and-hooks)で説明されている数値のフック結果を受け取ります。 | なし | なし |

<a id="non-error-diagnostic-codes"></a>
## エラーではない診断コード

| コード | 発行元 | メッセージ / データ | 意味 |
| --- | --- | --- | --- |
| `process.started` | `OmsiLaunchService.LiveSession.Attach` | メッセージ = PID。`Data["thread_id"]`、`Data["creation_utc"]`（ISO 8601） | `Omsi.exe` が作成され、その識別情報が記録されました。セッション診断です。 |
| `closecheck.stale-removed` | `OmsiLaunchService.RemoveStaleClosecheck` | メッセージ = 削除されたファイルの SHA-256 | セッション前から存在していた `closecheck` が完全に削除されました（`SuppressStaleClosecheckWarning = true`）。セッション診断です。 |
| `restore.session-artifact-removed` | `FileConfigurationTransaction.RestoreAsync` | メッセージ = 相対パス。`Data["sha256"]` | プロセスが開始されたセッション中に、セッションによる削除対象のパスが OMSI によって再作成されました。元の「存在しない」状態を復元するために削除されました。セッション診断 / `RecoveryStatus.Diagnostics` です。 |
| `plugin.integrity.reference` | `OmsiLaunchService.PlanSessionAsync` | メッセージ = `manifest` または `self` | 常駐プラグインの検証でどの参照を使用するかを示します。プラン診断です。 |
| `module.planned` | `SessionPlanner` | メッセージ = モジュール ID、`Data["module"]`、`Data["status"]`、`Data["activation"]` | モジュールがプランに含まれています。プラン診断。 |
| `module.disabled` | `SessionPlanner` | メッセージ = モジュール ID | コンテンツまたは既定のモジュールが、無効化（リクエストまたは設定ファイル）によって除外されました。プラン診断。 |
| `session_profile.selected` | `SessionPlanner` | メッセージ = プロファイル id。`Data["session_profile.id|name|version|author|preset_id|preset_index|preset_name|path"]` | セッションプロファイルからコンパイルされたセッションの出自を示します。プラン診断です。 |

ランタイムイベント名（`RuntimeEvent.Type`。診断ではありません）は[セッションのライフサイクル](../concepts/session-lifecycle.md#telemetry-events)に一覧があります。
