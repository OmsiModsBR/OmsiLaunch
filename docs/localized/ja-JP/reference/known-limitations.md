# 既知の制限事項

<!-- l10n: source=reference/known-limitations.md source-sha256=005074ec3caf17e2c206f7d659c5a2cf975a19e91d01ba4a6c6bd197777e658b -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/known-limitations.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch 0.1.0-beta4 において `UNAVAILABLE`、`PARTIAL`、または受容済みリスクであるものをすべて、コードに基づいて列挙します。これは、利用者やインテグレーターが、製品が提供しない動作を前提に構築してしまうことを防ぐためです。各行には、制限事項、その安定性、それが存在する理由、および詳細が記載されている場所を示します。英語のドキュメントが規範です。`docs/localized/` 配下のローカライズ版は同じ水準では保守されておらず、内容が遅れている場合があります（最後のセクションを参照）。

<a id="compatibility"></a>
## 互換性

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| オブジェクトレイアウトを読み取るケイパビリティ（車両、人物、天候、マップ、カメラ、時刻表、スクリプト、運転士、乗車券）は、既知のレイアウト（`Omsi23004`）でのみ検証されます。別の OMSI 2 ビルドでは、構造的なバリデーターが用意されるまで利用できません（`OL_E_CAPABILITY_UNRESOLVED`） | 不明なビルドでは `PARTIAL` | [互換性](compatibility.md) |
| 正規の Steam 実行ファイル（DRM で保護）はランタイム検証されていません。管理された LAA コピーは検証されました | Steam については `PARTIAL` | [互換性](compatibility.md) |
| ランタイム検証されたのは既知のレイアウトのみです。ビルド非依存の解決は、ID を伏せた参照実行ファイルに対してオフラインで検証されています | 不明なビルドでは `EXPERIMENTAL` | [ランタイム検証ステータス](../status/runtime-validation-status.md) |
| Windows 10 以降の x64 のみ。Windows 7/8、XP、ARM64 には対応していません | `UNAVAILABLE` | [互換性](compatibility.md) |
| プラグインには、コントローラーが使用する x64 ランタイムに加えて **x86** の .NET 6 ランタイムが必要です | | [互換性](compatibility.md) |

<a id="world-start-and-launch-options"></a>
## ワールドの開始と起動オプション

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| `LAST_MAP_STATE`（`/last /map:<identity>`）は、OMSI の last-state 分岐の静的解析から実装されており、オフラインでのみ検証されています | `EXPERIMENTAL` | [CLI](cli.md) |
| 明示的な日付・時刻・年、またはシステムの日付・時刻・年（`/date`、`/time`、`/year`、プロファイルの `new.date`/`new.time`/`new.year`）は NEW_MAP の開始にのみ適用され、オフラインで検証されています。ランタイム検証は保留中です。うるう年の 12 月 31 日は要求できません（OMSI のカレンダーは 366 日目で折り返します） | `EXPERIMENTAL` | [launchspec](launchspec.md#datespec-timespec-yearspec) |
| 開始時の天候プリセット、ICAO、現在の実天候（`/weather*`、`new.weather`）は、ワールドの開始直後に OMSI 自身の天候読み込みと METAR フラグを通じて適用されます（静的に確定済み、BI-003）。ランタイム検証は保留中です。最初のフレームで OMSI が表示する天候は、マップの既定のままである可能性があります | `EXPERIMENTAL` | [CLI](cli.md) |
| 開始時のプレイヤー車両のモデル、リペイント、HOF、車両番号、ナンバープレート（`/vehicle` 系、`PlayerVehicleSpec`）はコンテンツカタログに照らして解決されますが、適用はされません。これらを要求するとプランは実行不可になります。ネイティブの割り当て手順（OMSI の位置ダイアログによる配置、`MakeVehicle`、ユニットごとの終了処理ステップ、`CopyTempListIntoMainList`、プレイヤーのインデックス）は静的に確定していますが、稼働中の車両を作成して登録するものであり、ランタイムで検証できるようになるまでは実装されません。後のベータで計画しています。保存済みシチュエーションまたは `/last` は、それ自身のプレイヤー車両を復元します | `UNAVAILABLE`（BI-007） | [ケイパビリティ](capabilities.md) の `player.assign-headless` |
| ID によるエントリポイント指定（`/entrypoint:<identity>`）は、OMSI が提示する正確なラベルを選択します（静的に確定済み、BI-001）。ラベルを共有する後続のレコードは、OMSI 自身のダイアログと同様に選択できません。ランタイム検証は保留中です | `EXPERIMENTAL` | プランのケイパビリティ `world.entrypoint-identity` |
| キーによるキーボードおよびコントローラーの設定（`Environment.Keyboard`、`Environment.Controllers`）にはカタログ化されたキーがありません。代わりに `InputSpec` でファイル全体が置き換えられます | `UNAVAILABLE` | [launchspec](launchspec.md#inputspec) |

| 協調的なシャットダウン（`/cooperative-shutdown`、`Behavior.CooperativeShutdown`）はオプトインで、オフラインでのみ検証されています。既定では、セッション終了時に OMSI は強制終了され、`ShutdownTimeoutSeconds` は効果を持ちません | `EXPERIMENTAL` | [CLI](cli.md) |

| 診断フラグ `/log`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process` は `DiagnosticsSpec` に値を設定するだけで、それ以上の効果はありません（`.omsilaunch\diagnostics` 配下のホストトレースは常に書き込まれます）。`/trace-plugin` と `/trace-native` は、プラグインとネイティブのトレースイベントをそこに追加します | `PARTIAL` | [CLI](cli.md) |
| `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` は検証ハーネスです | `INTERNAL` | [CLI](cli.md) |

<a id="session-end-and-process-control"></a>
## セッションの終了とプロセス制御

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| セッションの停止は強制終了です。`session.stop`、トレイの "End session"、Ctrl+C、`CloseAsync` はいずれも `TerminateProcess` に至ります。OMSI のシャットダウン処理は実行されず、OMSI は終了時に `options.cfg` やログを書き直さず、OMSI の未保存の状態はすべて失われます。これは意図的なもので、復元されたファイルを OMSI が上書きすることを防ぎます。 | 設計上の仕様 | [セッションライフサイクル](../concepts/session-lifecycle.md) |
| タイムアウトと強制終了へのフォールバックを備えた、WM_CLOSE による協調的なシャットダウンは未実装です | `UNAVAILABLE`（製品としての判断、S-11。ランタイムクロージャーラウンドでは、OMSI はメインウィンドウへの `WM_CLOSE` を無視しました） | [ランタイム検証ステータス](../status/runtime-validation-status.md) |
| コンソールのクローズまたはログオフ時、オーナーには停止と復元のために 4 s の猶予があります。残ったものは次回の開始時にジャーナルによってリカバリされます | コンソールのクローズはランタイム検証済み。ログオフは未実施 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |

<a id="transaction-recovery-and-lease"></a>
## トランザクション、リカバリ、リース

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| インストールリースは `Local\` セマフォです。オーナーはインストール環境ごと、**ログオンセッションごと**に 1 つです。ユーザーをまたいでは強制されず、別のプロセスがハンドルを保持している間は解放されず、同じユーザーの任意のプロセスがその名前を保持できます | 受容済みリスク（S-18） | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| ジャーナルに記録された OMSI プロセス、または PID のないジャーナルの場合はそのルートの任意の `Omsi.exe` が実行中の間、リカバリは拒否されます（`OL_E_INSTALLATION_BUSY`） | 設計上の仕様 | 同上 |
| 元々存在しなかったオーバーレイパスの内容がセッション中に変更された場合、確認されるまで復元はブロックされます（`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`） | 設計上の仕様 | 同上 |
| 所有権フィンガープリント導入以前のジャーナルは、計画されたバイト列が同一のセッションによってのみクローズできます（`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`） | `PARTIAL` | 同上 |
| 復元されるのはセッションが所有するパスのみです。セッション中の OMSI 自身の書き込み（`options.cfg` をオーバーレイする設定がない場合の `options.cfg` の `[last_map]`、`Texture\standard.ipr`、キャッシュ、`laststn.osn`、運転士プロファイル、ログ）は、OMSI を直接起動した場合と同様に残ります | 設計上の仕様 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| `SuppressStaleClosecheckWarning` が true の場合、セッション前に行われる古い `closecheck` の削除は恒久的です（記録はされますが、復元されません） | 設計上の仕様 | 同上 |

<a id="runtime-control"></a>
## ランタイム制御

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| `weather.set` と `weather.metar.set` は OMSI のソース天候レコードに書き込みます（静的に確定済み）。天候ティックをまたぐ永続性、雲の種類の解決、METAR のダウンロードは、ランタイム検証を待っています。導出される値（湿度、光の係数、実際の風）は設定できません。OMSI が一時停止している間は、一時停止を解除した後にのみ更新されます | `EXPERIMENTAL` | [ケイパビリティ](capabilities.md) |
| `SetActualDateTime` は PC のクロックを OMSI の開始クロックにコピーするだけです（静的解析）。OmsiLaunch はこれを呼び出さず、開始クロックを自ら設定します | 設計上の仕様 | `calendar.set-actual-date-time` |
| 文字列変数の書き込み、名前付きトリガー、サウンドトリガーは OMSI 自身のメソッドを通じて行われます（静的に確定済み、BI-004）。車両スクリプトへの影響は、ランタイム検証を待っています。対象は道路車両のみです | `EXPERIMENTAL` | `scripts.string.write`、`scripts.trigger` |
| 車両の再配置、タイルをまたいだ空間的な再バインド、ODE に対して安全な変換権限はいずれもありません。位置フィールドは読み取り専用です | `UNAVAILABLE`（BI-008） | `road-vehicle.read` |
| `camera.lock` / `camera.unlock` には PlayerVehicle が必要です。ヘッドレスの NEW_MAP 開始には PlayerVehicle がありません（保存済みシチュエーションでは提供されます） | 設計上の仕様（BI-007） | RV-004 |
| ランタイムでの変更（`time.set`、`camera.set`、`camera.lock`、`vehicle.variable.set`、スポーン、ランダム配置、D3D テクスチャ）はジャーナルに記録されず、復元もされません | 設計上の仕様 | [ランタイム制御](runtime-control.md) |
| ハンドルの失効は、OMSI 自身の再利用されないインスタンス番号（`IDCode`）と、人物については消滅と再出現から判定され、オフラインで検証されています。自然な削除（RV-002: ロードされたパスネットワークから出る AI 車両）は、まだランタイムで観測されておらず、`entity.removed` イベントもまだ発行されません | `PARTIAL` | [ランタイム制御](runtime-control.md) |
| 結果は 64 KiB のメールボックスによって上限が設けられます。長いリストは切り詰められます（`truncated=true`）。ピクセルペイロードは `d3d.texture.update` 1 回あたり 48 KiB に制限されます | 設計上の仕様 | [ケイパビリティ](capabilities.md) |
| シングルフライトのチャネル: セッションごとに一度に 1 リクエストのみです。スロットがビジーの場合は `OL_E_RUNTIME_CHANNEL_BUSY` になります。リクエスト ID を再利用してはなりません | 設計上の仕様 | [ランタイム制御](runtime-control.md) |
| テレメトリは 64 イベントのリングです。64 イベントを超えて遅れたホストは最も古いものを失い、その件数が通知されます（`telemetry.lost`） | `EXPERIMENTAL` | [常駐プラグイン](../concepts/permanent-plugin.md) |
| モジュールの Direct3D デバイスフックは vtable フックです。別のコンポーネントがデバイスの元のメソッドを書き戻すインストール環境（リグレッション用のインストール環境で、ゲームプレイの開始直後に観測されました）では、呼び出されなくなり、クリーンアップ時にも取り除けません（`OL_E_MODULE_CLEANUP_INCOMPLETE`。コンポーネントは読み込まれたままです） | `PARTIAL` | [ランタイムモジュール](runtime-modules.md#native-components-and-hooks) |
| ランタイムモジュールの読み込みはオープンです。OmsiLaunch はモジュールを誰が作ったかを確認しません。保証するのは、モジュールが OmsiLaunch プロセス内で決して実行されないこと、およびセッションプログラムがそのジョブに閉じ込められること（2 つ目のプロセスなし、セッションとともに終了）だけです | 設計上の仕様（E21） | [ランタイムモジュール](runtime-modules.md#security-boundary) |
| モジュールプロトコル 2（設定、既定のアクティベーション、設定ファイル、セッションプログラム、プラグインの競合、通知、`get_value`）は、オフラインでのみ検証されています | `EXPERIMENTAL` | [ランタイムモジュール](runtime-modules.md) |
| D3D のデバイスリセットはランタイムで観測されました（`resetting`、`restored`、世代の無効化）。OMSI のデバイスが直接 `DEVICENOTRESET` に移行したため、独立した `lost` 遷移は発生しませんでした | `PARTIAL`（RV-007） | [ランタイム検証ステータス](../status/runtime-validation-status.md) |
| ランタイムのリストはページングされます（`cursor`、`limit`、`next_cursor`）。1 ページが保持するのは、リストの最大行数と、64 KiB のランタイムスロットに収まる分までです | `EXPERIMENTAL` | [ケイパビリティ](capabilities.md#pagination) |

| 高度なマップ/タイル/パス/オブジェクトグラフのフィールドの一部は公開されていません。ランタイムリーダーはプロファイルで制限された型付きスナップショットであり、任意のメモリアクセスではありません | 設計上の仕様 | [ケイパビリティ](capabilities.md) |
| プロセス内のメモリ読み取りは、稼働中の OMSI に対してチェックしてから使用する方式です。チェックと読み取りの間に OMSI が並行して変更を行うと、一貫性のないスナップショットになる可能性があります（`OL_E_RUNTIME_OPERATION_FAILED`） | 受容済みリスク（S-33） | |

<a id="local-control-plane-and-trust-model"></a>
## ローカルコントロールプレーンと信頼モデル

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| 同一ユーザー信頼モデル: 名前付きパイプ（`CurrentUserOnly`）、ハンドオフ/テレメトリ/ランタイムのメモリマッピング、およびリースのセマフォには、同じ Windows ユーザーの任意のプロセスがアクセスできます。そのようなプロセスは、`session_id` を読み取ると、ステータスの読み取り、セッションの停止、ランタイム操作の実行が可能になります。 | 受容済みリスク（S-06、S-30） | [ローカル制御](local-control.md) |
| コントロールエンドポイントはオーナーが `Running` の間のみ存在します。起動中およびセッション終了後、クライアントには `OL_E_NO_ACTIVE_SESSION`（終了コード 4）が返されます | 設計上の仕様 | [ローカル制御](local-control.md) |
| 別のプロセスがすでにパイプ名を所有している場合、オーナーはエンドポイントなしで実行を続け（`ListenFault`）、2 回目の起動で `OL_E_SESSION_ALREADY_ACTIVE` が誤って報告される可能性があります | 受容済みリスク | [ローカル制御](local-control.md) |
| `.omsilaunch\` は OMSI ルートの ACL を継承します。明示的なアクセス制御は適用されません | 受容済みリスク（S-31） | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |

<a id="diagnostics-and-output"></a>
## 診断と出力

| 制限事項 | 安定性 | 詳細 |
| --- | --- | --- |
| 診断情報はローカルファイル（`.omsilaunch\diagnostics`）のみです。何もアップロードされず、リモートへのレポート送信もありません | 設計上の仕様 | [トランザクションとリカバリ](../concepts/transactions-and-recovery.md) |
| 保持されるのは最新 50 セッション分です。新しいセッションの開始時に、それより古いセッション接頭辞付きの診断情報は削除されます | 設計上の仕様 | 同上 |
| JSON 出力と診断情報にはインストールパス（`RootPath`、アセットディレクトリ、`.itx` パス）が含まれます | 設計上の仕様（ローカルデータ） | |
| `OmsiLaunchW.exe` のセッション失敗ダイアログは、メッセージとして文章ではなくプラグインの失敗ペイロード（たとえば `{"name":"world.failed",...}`）を表示します。`Code:` 行は正しい内容です | 表示上の問題 | [Windows トレイ](windows-tray.md) |
| Direct3D の呼び出しより前にネイティブブリッジによって拒否された D3D リクエストは、`native_status` を正しく報告しますが、その `detail` テキストは `HRESULT 0x00000000` となります | 表示上の問題 | [ケイパビリティ](capabilities.md) |
| トレイのステータスウィンドウは、開いた時点で取得された、プランニングされたセッションのスナップショットです。更新されず、OMSI のライブ値は表示されません | 設計上の仕様 | [Windows トレイ](windows-tray.md) |

<a id="documentation"></a>
## ドキュメント

`docs/` 配下の英語ページが、このリリースの規範となるドキュメントです。`docs/localized/<locale>/` には同じ `0.1.0-beta4` ページの翻訳が含まれています（[`LOCALIZATION-MANIFEST.md`](../../LOCALIZATION-MANIFEST.md) を参照）。翻訳と英語のテキストが異なる場合は、英語のテキストとコードが正となります。そこに記載されている履歴ページおよびレガシーページは英語でのみ提供されています。

関連項目: [ケイパビリティ](capabilities.md)、[ランタイム検証ステータス](../status/runtime-validation-status.md)、[エラー](errors.md)。
