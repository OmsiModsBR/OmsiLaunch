# ランタイム検証ステータス

<!-- l10n: source=status/runtime-validation-status.md source-sha256=87cfe5eca3645bba4beefa62ea72d3bda5ef7a9f355158a7ea38eb1b61d8376d -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../status/runtime-validation-status.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、OmsiLaunch 0.1.0-beta4 の唯一のエビデンス表です。各行はケイパビリティまたは機能と、そのエビデンスの状態を示します。`RUNTIME_VALIDATED` の根拠として引用できるのは、`research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md`（マトリクス ID RV-nnn、ブロッカー BI-nnn、および 2026-09-20 の実行表に記載されたセッション ID）、または Fable 後の Round A レポート `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-ROUND-A-001.md`（RA-nnn）です。それ以外はすべてコードとオフラインテストスイートから導かれたものです。2026-09-21 の堅牢化ラウンド（`research/reports/OMSILAUNCH-SECURITY-ROBUSTNESS-REVIEW-001.md`、指摘事項 S-01 〜 S-34）では、復元、リカバリ、コントロールプレーン、境界の動作が変更されましたが、これらの変更にはオフラインのエビデンスしかありませんでした。2026-09-23 の最終ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`、`F01`、`S05`、`T01` などのシナリオ ID、候補 2 および 3）では、承認済みのインストール環境上の実セッションでこれらを実行しました。このラウンドで昇格した行には、そのシナリオ ID とセッション ID を記載しています。したがって、`RUNTIME_VALIDATED` はこのレポートを引用することもできます。

エビデンスの状態:

| 状態 | 意味 |
| --- | --- |
| RUNTIME_VALIDATED | マトリクスに記録された正規のセッションで観測済みです（ID とセッションを記載）。 |
| STATICALLY_VALIDATED | 実装済みでオフラインテスト（`tools/OmsiLaunch.TestHost`、`tests/*`）でカバーされていますが、最後の変更以降、ライブセッションでは観測されていません。 |
| PARTIAL | 実装済みですが、エビデンスが不完全であるか、矛盾しているか、動作が限定的であることが判明しています。 |
| RUNTIME_VALIDATION_REQUIRED | 動作が変更されたか、一度も観測されていません。昇格させる前に実セッションで検証する必要があります。 |
| UNAVAILABLE | 未実装、または意図的に拒否されています。 |

<a id="session-lifecycle-and-transaction"></a>
## セッションのライフサイクルとトランザクション

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| NEW_MAP のヘッドレス開始（`world.new-map`、`world.presented-entrypoint`、`boot.headless-start`） | RUNTIME_VALIDATED | マトリクスの「Existing Runtime Evidence」、セッション `e5454061`、`1e8e0548`、`5f641c8d`、`50a1f1ec`（Grundorf） |
| SAVED_SITUATION の開始（`world.saved-situation`） | RUNTIME_VALIDATED | `GetCapabilitiesAsync` の注記とマトリクス（Berlin-Spandau、Start フォームと `Button1Click`） |
| `session.stop` による強制終了と正確な復元 | RUNTIME_VALIDATED | セッション `50a1f1ec`: 通常の停止後、OMSI プロセスもジャーナルも残りませんでした |
| 起動時の冪等性（`PluginStart` の繰り返し） | RUNTIME_VALIDATED | セッション `50a1f1ec` |
| `options.cfg` のセマンティックオーバーレイとバイト単位で正確な復元（RV-005） | RUNTIME_VALIDATED | セッション `1e8e0548`: `AIMaxCountRandom` 150/200 と保持された末尾部分、復元後のハッシュ `A12982C3...` |
| 管理対象スプラッシュとオーバーレイのトランザクション（RV-006） | RUNTIME_VALIDATED | マトリクス RV-006 PASS（プレゼンテーションバッチ: 管理対象の既定、管理対象のカスタム、`Unset`）、Release Presentation 001 のセッション `388cf5d2-7310-4301-9313-e62c4c08a346`、`794cae34-a2ac-4fcf-8981-f142ac14c308`、`12a74b24-6c78-4863-8ffe-947651212eeb` |
| 早期終了からのリカバリ（RV-008、OMSI の強制終了） | RUNTIME_VALIDATED | Fable 後の Round A セッション `9e7cad79-9c35-4f93-bfc8-71f455513f82`（ゲームプレイ後）と `dcac497e-ff35-4c97-b5e4-98b660f88032`（`world.starting` の後、ゲームプレイ前）: オーナーは完了し、ジャーナル/リースは解除され、`options.cfg` と元々存在しなかったスプラッシュの状態はスナップショットと一致しました。ネイティブの起動失敗と、復元失敗の注入は別扱いのままです。 |
| 起動失敗と意図的な復元失敗の境界（RV-008 の残り） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: 起動タイムアウト `SF01` セッション `0b21c664-b0b2-46b2-9b6b-def536765fff` と、プラグインが報告したワールド失敗 `SF02` セッション `53358c75-469b-41d5-9ddb-cdb1d1ff828d` は、型付きコードと正確な復元を伴って `Failed` で終了しました。復元失敗 `F01` セッション `d4194a69-d858-42fe-97c7-ce93ccbdc3f6`（停止中にテスト専用プロセスがセッション所有のファイルをロック）では `OL_E_RESTORE_FAILED` が返され、ジャーナルとバックアップが保持され、`/recover` によってセッション前の状態が正確に復元されました。失敗を発生させる仕組みはテストインフラ専用です。 |
| `.itx` ターゲットと `Texture\standard.ipr` のセッション成果物の復元（S-01） | RUNTIME_VALIDATED | Round A では、`47efd323-4ce1-400a-ac83-74893cf82f26` で既存のパスを復元しました。`8d9bdedf-d7de-4c64-a8b1-627ae4deff7b` では、登録済みで元々存在しなかったターゲットを作成し、通常の停止時に削除しました。さらに `aa9a45d4-302e-4cf5-8490-f5a1c9e44b79` では、オーナーの中断と `/recover` を経て同じクリーンアップを繰り返しました。 |
| セッションの削除と失効マーカーの除去としての `closecheck`（S-13） | RUNTIME_VALIDATED | `47efd323-4ce1-400a-ac83-74893cf82f26` を含む Round A セッションで、`closecheck.stale-removed` に続いて `restore.session-artifact-removed` が記録され、ジャーナルは残りませんでした。 |
| オーバーレイ構築前の早期リカバリ、フィンガープリント前の遅延リカバリ（S-05） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`S05`: セッション `f755ed51-b85c-4df2-b062-5bf65c768b32` のオーナーを保留中のジャーナルがある状態（`maxFPS=77` が有効）で強制終了しました。次の開始であるセッション `291ea22b-f9b3-42a4-be07-7f0c055c7113` は `TRANSACTION_STARTED` の前に `PENDING_JOURNAL_RECOVERED` を記録し、独自のオーバーレイ（`88`）で実行され、最初のセッション前の状態とバイト単位で同一の状態で終了しました。フィンガープリント前の遅延分岐はオフラインのままです（`transaction.legacy-journal-ownership-migration`）。 |
| メタデータの復元（タイムスタンプ、属性、読み取り専用）、ライトスルーのフラッシュ、バックアップのクリーンアップ（S-12） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: `S12a` セッション `40de172b-e144-4f0a-9207-09c80b5d7df7`（既知の `options.cfg` のタイムスタンプ）と `S12b` セッション `d9e2534b-3cb3-47ef-9cbd-1b92c62a35e7`（タイムスタンプと ReadOnly）: セッション中はオーバーレイが有効で（OMSI のために ReadOnly を解除）、バイト内容、最終書き込み日時、作成日時、属性が正確に復元されました。バックアップディレクトリは残りませんでした。 |
| インストールリース下での `/recover`、PID 取得前の期間における拒否（S-04） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: `S04` セッション `1fb2aad5-2985-4fa4-9c5a-6186f51d37b8`: オーナーが稼働中の状態では `/recovery-status` と `/recover` が `OL_E_INSTALLATION_BUSY` を返し、2 回目の開始は拒否されました。オーナーを強制終了して OMSI を実行したままにすると、OMSI が終了するまで `/recover` と新しい開始は拒否され、その後 `/recover` が正確に復元しました。`S04b` セッション `5e57ca51-ab5e-4e94-a0d0-531735ee4057`: PID 取得前のジャーナル（ハーネスによる障害注入）は、ルートから `Omsi.exe` が実行されている間は拒否され、終了後にリカバリされました。その OMSI が書き込んだ `closecheck` は `OL_W_RESTORE_FOREIGN_FILE_RETAINED` とともに保持されます（文書化済み、保守的な動作）。 |
| `StartSessionAsync` での再プランニング、失効したプランに対する `OL_E_PLAN_NOT_RUNNABLE`（S-15） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R02`: プランニング後にマニフェスト記載のプラグインを退避させた状態で、`StartSessionAsync` はセッションが存在する前に `OL_E_PLAN_NOT_RUNNABLE: OL_E_PERMANENT_PLUGIN_MISSING` をスローしました（プラン `4797c9ce-6eb3-47b1-b09d-03562dd3bd44`、候補 2、BUG-01 の修正後） |
| オーナーのライフサイクル: パイプによる停止、Ctrl+C、トレイからの停止、OMSI の終了、オーナーの強制終了、`/observe-seconds` | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: `/observe-seconds` 中のパイプによる停止 `L06` セッション `300a563e-7b0e-4f01-afbd-9e15df32c7e6`、Ctrl+C `L03` セッション `5e2aca54-bae2-4fc6-a106-93699e755bfc`（シグナルから 577 ms 後にオーナーが終了）、OMSI の強制終了 `L05a` セッション `d69658f9-16f6-43e9-8e36-682114b97e14`、OMSI メインウィンドウのクローズ `L05b` セッション `76b11ba4-8ddb-4611-9a1c-8be317cd0337`（OMSI は `WM_CLOSE` を無視するため、強制終了で終了）、トレイからの停止 `T01`/`T03` セッション `6d827c13-caf8-4221-832a-c91d26f130e0`、`7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`、オーナーの強制終了 `S05`。すべての経路が正確な復元で終了しました。オーナーを直接強制終了した場合、`Omsi.exe` は実行を続けます（設計どおり）。リカバリは OMSI が終了するまで拒否されます（`S04`）。 |
| コンソールのクローズまたはログオフ時の ProcessExit 4 s の猶予 | RUNTIME_VALIDATED（コンソールのクローズ） | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`L04` セッション `559d0f1d-7d5b-4b6b-b70c-0acb272d53db`: コンソールウィンドウを閉じました。復元は猶予時間内に完了し、セッションはジャーナルなしで `Completed` に到達しました。Windows はクローズの約 5 s 後にオーナーを終了させました（終了コード `0xC000013A`）。ログオフは検証していません。 |
| 協調的な WM_CLOSE によるシャットダウン | UNAVAILABLE | 未実装（製品上の判断、S-11）。ランタイムクロージャー `L05b`: メインウィンドウへの `WM_CLOSE` から 30 s 以内に OMSI は閉じませんでした。`ShutdownTimeoutSeconds` は使用されません。 |
| インストールリースのセマンティクス（`Local\` セマフォ） | STATICALLY_VALIDATED | `lease.cross-thread-release`。制限は受容済みリスクとして文書化されています（S-18） |
| OMSI ファイルへのパッチ適用時の CP1252 の保持（S-07） | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`C01` セッション `84a71235-d618-4917-9af4-c2a6025742f4`: アクセント付きの CP1252 の値（0x80 と 0x96 を含む）を持つ `options.cfg` に `/set` でパッチを適用しました。ライブのファイルは CP1252 のバイトを保持して `EF BF BD` を含まず、セッション後のファイルはバイト単位で同一でした |
| リパースポイントをスキップするコンテンツ検出（S-23） | STATICALLY_VALIDATED | `discovery.*` テスト |
| 診断情報の保持（新しい順に 50 セッション）（S-26） | STATICALLY_VALIDATED | `HostTrace.Prune` |
| トランザクションの書き込みと削除で発生する一時的なファイルロック（ウイルス対策ソフト、インデクサー）は約 1.5 s の間リトライされます。永続的なロックは引き続き失敗します（修正パス CP-10） | STATICALLY_VALIDATED | `transaction.transient-lock-retried`、`transaction.restore-failure-recovery`（永続的なロックは引き続き失敗）。修正前には断続的なオフライン失敗が 2 回観測されましたが、修正後は 16 回の実行で 1 回も発生していません |

<a id="permanent-plugin-and-platform"></a>
## 常駐プラグインとプラットフォーム

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| プロファイル `Omsi23004_692EBFBF`（`692EBFBF...6243`、8,503,440 バイト） | RUNTIME_VALIDATED | マトリクスのすべてのセッション |
| Steam LAA 実行ファイル（`7DAB063D...D759`） | PARTIAL | ランタイムクロージャー `LAA01` セッション `9ec975ca-7820-47be-b806-5f5a94aad4ab`: 管理下のコピー（インストール環境にあるパッチ未適用のオリジナルから `IMAGE_FILE_LARGE_ADDRESS_AWARE` を付与し、PE チェックサムを再計算して派生させたもの。8,503,440 バイト、characteristics `0x81AE`）はハッシュと一致し、フィンガープリントは受け入れられ、プランは実行可能でした。このイメージは DRM で保護されたオリジナルの Steam 実行ファイルであり、正規の Steam インストール環境の外では Steam クライアントにハンドオフして終了するため、セッションは `OL_E_PROCESS_EXITED_EARLY` で失敗し、正確に復元されました。ゲームプレイ、読み取り、コマンド、停止には正規の Steam インストール環境が必要です（ENVIRONMENT）。 |
| インストール環境の識別: リースとコントロールパイプ名は 1 つの定義 `InstallationPaths.IdentityKey` から導出されます（`C:\OMSI` = `C:\OMSI\` = `C:\OMSI\.` = `C:\foo\..\OMSI` = 大文字小文字の違い。`C:\OMSI-A` と `C:\OMSI-B` は異なる）（修正パス CP-02、補遺 A/B） | STATICALLY_VALIDATED、ランタイムで観測済み | `lease.root-normalization`、`paths.installation-identity-and-containment`。ランタイムクロージャー `ID01`: セッション `01bfc780-10ef-4390-b700-c08bf809a42f` の実行中に、ルートを `\.` 付き、`..` セグメント付き、小文字、大文字で表記した 2 回目の開始と `/recovery-status` は拒否されました（`OL_E_SESSION_ALREADY_ACTIVE`、`OL_E_INSTALLATION_BUSY`）。末尾に区切り文字を付けた表記も同様です（`ID01b`、セッション `ba3ea7c9-9826-453d-b447-81c48763e18f`）。 |
| リリースパッケージの整合性: クリーンなステージング、`Native.x86` のビルドレシート（タイムスタンプではなく内容）、BOM なしのマニフェスト、ステージングしたクロージャーと再展開したアーカイブの完全性、インストーラーによるマニフェストのコピー（修正パス CP-01、Round A RA-007、補遺 F/G/H） | STATICALLY_VALIDATED、インストール環境で観測済み | `Test-PackagingPipeline.ps1`（整合した候補と 16 件のネガティブケース）、`runtime.release-manifest-bom`、`runtime.release-manifest-strict-parser`。ランタイムクロージャー: 承認済みのインストール環境にインストールされたパッケージはマニフェストと整合しており、すべてのセッションが `plugin.integrity.reference = manifest` でプランニングされました（候補台帳 `research/reports/runtime-closure/CANDIDATES.md`）。 |
| `release-manifest.json` に対するプラグインクロージャーの完全性（S-09） | RUNTIME_VALIDATED | Round A セッション `47efd323-4ce1-400a-ac83-74893cf82f26`。インストール済みのリリースパッケージ上のランタイムクロージャーのセッション（例: `R01` セッション `878b5e3c-2f9d-46bf-a918-11bb2e227e55`、候補 3）は `plugin.integrity.reference = manifest` でプランニングされました。`R02` では、記載されたプラグインが欠落していると開始がブロックされることが示されました。 |
| プロセス内のビルド検証（`plugin.build.invalid` -> `OL_E_BUILD_VALIDATION_FAILED`） | RUNTIME_VALIDATED | マトリクスのセッション。古いプラグインでの以前の `plugin.build.invalid` セッション 2 件 |
| SHA-256 付きの Handoff v4 | RUNTIME_VALIDATED | 統合テスト `handoff.*`、すべてのセッション |
| OmsiLaunch なし（ハンドオフなし）ではプラグインが何もしないこと | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`P01`（通常の OMSI 起動、OmsiLaunch セッションなし、OMSI pid 35564）: プラグインクロージャーと .NET ホストは読み込まれましたが、`OmsiLaunch.Native.x86.dll` は読み込まれず、3 か所のネイティブパッチ箇所はパッチ未適用の `Omsi.exe` のバイトのままで、コントロールパイプ、ジャーナル、診断情報はいずれも現れませんでした |
| DLL 検索ポリシー（`AssemblyDirectory | System32`）（S-24） | STATICALLY_VALIDATED | アセンブリ属性 |
| Windows 10 以降の x64 の検出とその他のプラットフォームの拒否 | STATICALLY_VALIDATED | `CurrentWindowsX64Platform.Detect` |

<a id="runtime-control-channel-and-control-plane"></a>
## ランタイム制御チャネルとコントロールプレーン

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| メールボックスのワイヤー完全性、セッションバインディング | RUNTIME_VALIDATED | すべてのランタイムセッション。`runtime-command.wire-guard`、`runtime-command.session-binding` |
| 遅延応答の破棄とタイムアウト後のチャネル停滞の防止（S-08） | RUNTIME_VALIDATED | Round A セッション `173414a5-ee53-4b10-aeb7-046f40b851e9`: 外部クライアントが 250 ms 後に `road-vehicles.spawn` を放棄しました。その後の `map.read` は成功し、正規の停止によってセッションはクリーンアップされました。サイズ超過の応答の拒否はオフラインのみのままです。 |
| ローカルコントロールプレーン: インストール環境ごとのパイプ名、`session_id` のバインディング、型付きハンドラーエラー、64 KiB の上限（S-06） | RUNTIME_VALIDATED | Round A セッション `a00c6ff8-9cc9-4bce-ae44-c4314daad7bf`: 無効なプロトコル、バインドされていない停止、誤ったセッションへのランタイム要求、サイズ超過のフレームはいずれも拒否されました。その後の有効なステータス要求とバインドされた停止は成功しました。サイズ超過の応答の拒否はオフラインのみのままです。 |
| テレメトリのシーケンススロット、不完全なサンプルのスキップ、同一の連続イベント（S-25） | STATICALLY_VALIDATED | `telemetry.sequence-samples` |
| API 境界による `internal.*` の拒否と内部の結果キーの除去（S-02） | STATICALLY_VALIDATED | `OmsiLaunchService.ExecuteRuntimeAsync`、レジストリの検証 |
| 無効またはサイズ超過のメールボックス応答がスロットを解放し、次の要求が成功すること（修正パス CP-03） | STATICALLY_VALIDATED | `runtime-command.oversized-response-rejected`、プラグイン側は `plugin-runtime.oversized-and-abandoned-responses`。ランタイムでは発生させられません。1 スロットを超える結果を返す公開操作はありません（最大は約 37.8 KB、ランタイムクロージャー `R03`）。 |
| ランタイム要求のすべての終端経路でメールボックスが再利用可能なまま残ること: 型付きの拒否、タイムアウト、呼び出し元によるキャンセル、再利用された要求 ID、内部操作、誤ったセッション、引数の欠落（ランタイム）。不正な形式、サイズ超過、破損、失効、孤立した応答（オフライン） | RUNTIME_VALIDATED（到達可能な経路） | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R03` セッション `1134fdc3-a469-4ff5-a511-9964ed65e7cd`: 各失敗は型付きで返され、次の有効な要求は成功しました。応答破損の経路は外部から発生させることができないため、オフラインのままです（`runtime-command.terminal-paths-leave-channel-usable`）。 |
| サイズ超過のコントロールプレーン応答を `OL_E_CONTROL_RESPONSE_TOO_LARGE` として返し、status/events を古い順に 1 フレームに収まるまで切り詰めること（修正パス CP-04） | STATICALLY_VALIDATED | `control-plane.binding-and-typed-errors`。ランタイムでは発生させられません。ランタイムクロージャーラウンドで観測された最大の公開応答は約 37.8 KB（`R03`）であり、セッションのイベント履歴は 1 フレームを大きく下回ります。 |
| 接続後にコントロールプレーンが無応答にならないこと: 不正な形式、サイズ超過、負の長さ、空、JSON `null`、コマンド欠落、文字列でない引数、無効なプロトコル、不明なコマンド、未バインド、誤ったセッションの各フレーム。途中で切れたクライアントフレーム。停滞したクライアント。応答前に切断するクライアント | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`R04` セッション `faaaf8a8-126d-4a23-a8f3-381e47c58de6`（候補 2、BUG-02 の修正後）: すべてのフレームが型付きエラーを受け取り、それぞれの後に `session.status` が成功しました。停滞したクライアントは他のクライアントをブロックしませんでした。実行中の `road-vehicles.spawn` と競合したバインド済みの停止はセッションを終了させ、spawn の呼び出し元はストリームの正常な終端を受け取りました。`OL_E_TIMEOUT` と切り詰めのメタデータはオフラインのままです（`control-plane.errors-never-silent`）。 |
| ランタイムバッチハーネス（`/runtime-batch`、`/runtime-write-batch`、`/d3d-batch`） | INTERNAL（STATICALLY_VALIDATED） | 検証用ハーネスのみ（S-27） |

<a id="runtime-operations"></a>
## ランタイム操作

| 操作 | 状態 | エビデンス |
| --- | --- | --- |
| `time.read`、`time.set` | RUNTIME_VALIDATED | セッション `e5454061`（書き込み、`SetTime`、読み戻し、復元） |
| `weather.read`、`weather.actual.read` | RUNTIME_VALIDATED | セッション `e5454061` |
| `weather.set` | UNAVAILABLE | `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` で拒否されます。セッション `e5454061` と `5f641c8d` で永続性は FAIL、`50a1f1ec` で拒否は PASS |
| `map.read` | RUNTIME_VALIDATED | Map のグローバルスロットの修正後、セッション `9dd62626-94c6-4cd7-bb6f-0288327696f4` で再検証しました: `loaded=true`、19 タイル、Grundorf の識別情報、ファイル名、説明、年の範囲が整合していました。 |
| `camera.read`、`camera.set` | RUNTIME_VALIDATED | セッション `e5454061`（FOV の書き込み、読み戻し、復元） |
| `camera.lock`、`camera.unlock` | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`CAM01` セッション `5dfd9b96-fa94-4060-85af-0c9d19823a73`: 保存済みシチュエーション `Linie 5` が PlayerVehicle（`rv-000001`）を提供しました。ファミリー 0、2、1 のロックはカメラの読み戻しで確認され、アンロックでポリシーが解除されました |
| `road-vehicles.read`、`road-vehicles.list`、`road-vehicle.read` | RUNTIME_VALIDATED | セッション `e5454061`、`5f641c8d` |
| RoadVehicle のハンドルの有効期間と失効の検出（RV-002） | PARTIAL | 安全な削除を発生させる手段がありません。ランタイムクロージャーの 3 つの観測期間（`H01`、`H01b`、`H01c`、最大 180 s、時刻ジャンプあり）で、Grundorf の RoadVehicle と Human の数は増加するだけでした。オブジェクトを削除する公開操作はありません。失効の検出はオフラインのままです（S-19）。世代/ABA ルールは D3D テクスチャについてランタイム検証済みです（`H02`）。 |
| `road-vehicles.spawn`（RV-003） | RUNTIME_VALIDATED | セッション `5f641c8d`: `2 -> 3`、`rv-000003` が解決されました |
| `road-vehicles.place-random` | RUNTIME_VALIDATED | マトリクス: コレクション `2 -> 4` |
| `player-vehicle.read` | RUNTIME_VALIDATED | Round A RA-002 セッション `9dd62626-94c6-4cd7-bb6f-0288327696f4`（`present=false`）。ランタイムクロージャー `CAM01` セッション `5dfd9b96-fa94-4060-85af-0c9d19823a73` では、保存済みシチュエーションから存在する PlayerVehicle を読み取りました |
| `humans.read`、`humans.list`、`human.read` | RUNTIME_VALIDATED | セッション `e5454061` |
| `timetable.read`、`timetable.*.list`、`timetable.logs.read` | RUNTIME_VALIDATED | セッション `e5454061`。路線エントリ（track entry）91 レコード |
| `OL_E_RUNTIME_RESPONSE_TOO_LARGE` の代わりに上限付きリストを切り詰めること（ドキュメント監査 BUG-05） | RUNTIME_VALIDATED | パッケージ `9F8CC87B...`、保存済みシチュエーション `situations\Linie 5.osn`（Berlin-Spandau）でのドキュメント監査の再テスト: `timetable.track-entries.list` は 825 件中 137 件、`timetable.tour-entries.list` は 4747 件中 224 件を `truncated=true` とともに返しました（セッション `08df97b8-06cc-4faa-be8b-f49ca1538dce` とドキュメントキャプチャの再テスト）。修正前は、どちらも `OL_E_RUNTIME_RESPONSE_TOO_LARGE` で失敗していました（セッション `f51dcb59-a723-4570-b6c5-b61e628a7993`）。エビデンス: `research/reports/documentation-audit/runtime/`。 |
| `drivers.read`、`tickets.read` | RUNTIME_VALIDATED | セッション `e5454061` |
| `vehicle.variables.list`、`vehicle.variable.get`、`vehicle.variable.set` | RUNTIME_VALIDATED | `research/reports/OmsiHook-Parity-Wave-A-Closure-001.md` に Runtime-Proven として記録されたランタイムセッションでの `Refresh_Strings` 0 -> 1 -> 0（`OMSILAUNCH-AUTONOMOUS-BUILD-001.md` によれば 1,025 個の名前を列挙）。レポートにはセッション ID が記録されていません |
| `vehicle.string-variables.list`、`vehicle.string-variable.get` | RUNTIME_VALIDATED（読み取りのみ） | セッション `e5454061`。書き込みは BI-004 により UNAVAILABLE |
| `vehicle.constants.list`、`vehicle.constant.get`、`vehicle.curves.list`、`vehicle.curve.evaluate`、`vehicle.hofs.read` | RUNTIME_VALIDATED | セッション `e5454061` |
| `d3d.status`、`d3d.texture.create`、`d3d.texture.describe`、`d3d.texture.update`、`d3d.texture.release`、解放済みハンドルの拒否 | RUNTIME_VALIDATED | マトリクスの「basic D3D texture lifecycle」。`runtime.d3d.*` のケイパビリティ注記（デバイススロットについてはセッション `4378899d`） |
| D3D デバイスの消失/リセット/復元イベント、世代の無効化（RV-007） | RUNTIME_VALIDATED（resetting/restored） | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`D01` セッション `fc07946e-58dd-4ddc-af7e-10c7e61a899a`: 一時的なディスプレイモードの変更（テスト用の発生手段であり、公開されることはなく、合成的なリセット呼び出しも行いません）により、OMSI はデバイスを 2 回リセットしました。`d3d.resetting`/`d3d.restored` が発行され、サイクルごとに世代が進み、リセット中の要求は `OL_E_D3D_RESET_IN_PROGRESS` を受け取り、消失前のテクスチャは `OL_E_D3D_STALE_RESOURCE_HANDLE` で拒否され、新しいテクスチャは機能しました。OMSI は直接 `DEVICENOTRESET` に移行したため、独立した `lost` 遷移は発生しませんでした。1 回の試行では OMSI が `DSound.dll` でクラッシュしました（ディスプレイオーディオのエンドポイント変更。オーナーは正確に復元しました）。 |
| `events.read` / `events watch` | RUNTIME_VALIDATED | すべてのセッション（テレメトリ駆動のライフサイクル） |
| `internal.road-vehicles.make-basic` | INTERNAL | 公開サーフェスからは到達できません |

<a id="launch-features"></a>
## 起動機能

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| `/set` とプロファイルの `settings`（`configuration.options.semantic`） | RUNTIME_VALIDATED | RV-005 |
| セッションプロファイル（厳格なコンパイラー、競合、リパースポイントを含むパスの閉じ込め S-17） | RUNTIME_VALIDATED（プロファイルによる開始の範囲） | Round A RA-009、`Test-Beta3SessionProfile.ps1`、2026-09-22: 管理対象スプラッシュ、無効化された Internet Textures、`graphics.maxFPS=30` プリセット。`options.cfg` は復元され、フィクスチャは無傷で、ジャーナル/リース/プロセスは残りませんでした。 |
| `/spec` の読み込み（1 MiB の上限、不明なプロパティの拒否、タイムアウトの遵守）（S-20） | STATICALLY_VALIDATED | `cli.*` テスト |
| 管理対象スプラッシュ `PTB`/`ENG`/`DEU`/`FRA` | RUNTIME_VALIDATED | RV-006 |
| Internet Textures の `Disabled`（プロセス内ダウンローダーの抑止） | STATICALLY_VALIDATED | `internet-textures.suppressed` テレメトリ経路。マトリクスでは引用されていません |
| Internet Textures の `.itx` ターゲットガード（Round A RA-011/RA-012、修正パス CP-05、補遺 E）: 正規化されたルート、正規化されたターゲット、相対パス、セグメント規則。ルートより下にある `texture` という名前のディレクトリセグメントのみが対象となります（`mytexture`、`texture_backup`、`texture2`、および絶対ルート内の `texture` は対象外）。トラバーサル、絶対パス、ルート相対パス、ジャンクションのターゲットは拒否されます。表記の違いは 1 つの正規化されたトランザクションパスに集約されます | STATICALLY_VALIDATED | `presentation.itx-target-root-normalization`、`presentation.itx-target-guard-rejections` |
| Internet Textures の `Override` | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: `I01` セッション `6eac2dac-c7e9-49a1-acbf-2a5d59c1c0d3`: OMSI のダウンローダーが既存のターゲットと元々存在しなかったターゲットについてループバックサーバーに `HEAD` と `GET` を送信し、両方を書き込みました。正規の停止後、存在しなかったターゲットは削除され、既存のターゲットはバイト単位で正確に復元されました。`I02` セッション `a8f2b720-e41a-4e34-86bf-125bf519a5bd`: オーナーと OMSI を強制終了した後、`/recover` によって同じ結果になりました。 |
| `/date`、`/time`、`/year`、天候フラグ、プレイヤー車両フラグ | UNAVAILABLE | プランナーはプランを実行不可としてマークします（`OL_E_CAPABILITY_UNAVAILABLE`）。プラグインは `Unset` 以外の日付/時刻モードを拒否します |
| `/entrypoint:<identity>`（BI-001） | PARTIAL | プランは実行不可（プランのエビデンスは `PARTIAL`） |
| `/last`（`LastMapState`、BI-006） | UNAVAILABLE | プランは実行不可。`.osn` へのフォールバックはありません |
| `InputSpec` のキーボード/コントローラーのオーバーレイ（BI-005） | UNAVAILABLE | パーサーは存在しますが、適用されません |
| ヘッドレスでの PlayerVehicle の割り当て（BI-007）、`SetActualDateTime`（BI-002）、ICAO 天候制御（BI-003）、文字列変数の書き込み（BI-004）、タイルをまたぐ再配置（BI-008） | UNAVAILABLE | 実装が完了するまでブロックされています |
| `/list` による検出（`content.*`） | STATICALLY_VALIDATED、ランタイムで観測済み | `discovery.fixture`。ランタイムクロージャーでは、承認済みのインストール環境に対して `/list:situations` を使用しました（非 ASCII の `situations\Nur für Fortgeschrittene.osn` を含む識別子） |
| `/quiet`、`/serve` | UNAVAILABLE | ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT |
| 診断フラグ（`/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace*`） | PARTIAL | `DiagnosticsSpec` で伝達されます。効果はホストトレースに限られます |

<a id="windows-host-and-tray"></a>
## Windows ホストとトレイ

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| トレイインジケーター（`OmsiLaunchW.exe`）: 通知領域のアイコン、ステータス、確認とキャンセルを伴う「End session」（セッションを終了する項目）、エクスプローラーの再起動、停止が届いた時点で開いているダイアログ、`/observe-seconds` 中の停止、エラーダイアログ（S-16） | RUNTIME_VALIDATED | 候補 3（BUG-04 の修正後）でのランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）: `T01` セッション `6d827c13-caf8-4221-832a-c91d26f130e0`（`Shell_TrayWnd` でアイコンを確認、pt-BR のステータスウィンドウ、エクスプローラーの再起動でアイコンが再追加され、キャンセルではセッションが維持され、End session で 607 ms 後にセッションが終了してアイコンが削除されました）、`T02` セッション `0939a151-bf03-4ca9-8294-8b0088df7d30`（パイプによる停止中にステータスと確認ダイアログが開いた状態）、`T03` セッション `7e6e805d-69c2-47a3-9fa3-4cdccbe3ccb8`、`T04` セッション `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（引数エラー、アクティブなセッションなし、セッション失敗の各ダイアログがコード付きで表示）。シムの終了コード 100-106 は発生しませんでした。 |
| ホストが起動したときに 0 を返す `/silent` による再起動 | RUNTIME_VALIDATED | ランタイムクロージャーラウンド（`research/reports/runtime-closure/FINAL-RUNTIME-VALIDATION-REPORT.md`）`T04` セッション `8ddc4cc9-3089-4c0c-b062-f0c526ce0e4a`（候補 3、BUG-03 の修正後）: ランチャーは 0 を返し、その出力をキャプチャしていた呼び出し元は 0.4 s 後にストリームの終端を受け取りました。その間、OmsiLaunchW のセッションはコンソールウィンドウなしで実行されました。セッションは正確な復元とともに停止しました |
| ブートストラッパーシムの終了コード 100-106、動的パスバッファー（S-21） | STATICALLY_VALIDATED | `Test-ReleaseIdentity.ps1` |

<a id="documentation-audit-retest-010-beta3-documentation-closure"></a>
## ドキュメント監査の再テスト（0.1.0-beta3 ドキュメントクロージャー）

承認済みのインストール環境上のパッケージ `9F8CC87B238A677F7238C262E8FE0496832263B6C0A58083409EB485C03C6F17` で実施し、BUG-05、BUG-06、BUG-07 とスモークテストは最終パッケージ `B8462ED6C596843E778C33DF4E93EBA3ACC875B7E43B011D60E5A21A16032E35` で繰り返しました（R01 セッション `56cdee4b-9160-49fc-b0fd-5134ff9fe2d0`、ドキュメントキャプチャセッション `40046805-5337-48fc-90dd-e7d3500ed3b4`）。エビデンスは `research/reports/documentation-audit/runtime/` と `research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md` にあります。

| 項目 | 状態 | エビデンス |
| --- | --- | --- |
| `OmsiLaunchW.exe` が実行不可の起動を報告すること（BUG-06） | RUNTIME_VALIDATED | `OmsiLaunchW.exe` での `/new ... /date:2026-09-20`: ダイアログに `Requested capability unavailable: world.explicit-date` / `Code: OL_E_CAPABILITY_UNAVAILABLE` が表示され、終了コード 1、OMSI プロセスなし（`doc-bug06-dialog.json`、インストール済みパッケージ `9f8cc87b...`、設計上セッションは作成されません） |
| 公開されている CLI ルートが実際の構文であること（BUG-07） | RUNTIME_VALIDATED | インストール済みの `help --json` と `capabilities --json` が出力したクライアントルートのすべての代替形（32 個の記述子）が、インストール済みのパーサーに受け入れられ、使用方法を出力したものはありませんでした（`doc-bug07-routes.json`、インストール済みパッケージ `9f8cc87b...`） |
| CLI の例ページのすべてのコマンドライン | RUNTIME_VALIDATED | オーナーセッション `08df97b8-06cc-4faa-be8b-f49ca1538dce`（保存済みシチュエーション。`events watch` は Ctrl+C まで実行し続けるためスキップ）に対して 43 行のクライアントコマンドを実行し、最後にページ自身の `session stop` で終了しました（オーナーの終了コード 0）。`name=max_speed` を指定した `vehicle.constant.get` を除き、すべてページの記載と一致しました。この定数はそのバスには存在しないため、ページでは現在 `antrieb_getr_version` を使用しています。`grundorf-quick` プロファイルの例はプランを作成できなかったため（プレーンな YAML スカラー内のバックスラッシュの二重化）、修正しました。セッションなしの行はすべてインストールルートから実行し、起動の行は `/plan` 付きで実行しました（`doc-client-examples.json`、`doc-examples-no-session.json`、`doc-profile-example.json`） |
| 最終パッケージでの `/observe-seconds` 付き NEW_MAP のスモークテスト | RUNTIME_VALIDATED | `R01` セッション `08570134-cfd8-4726-a472-5f6dd2f04c8f`、クリーンな終了状態（`research/reports/runtime-closure/doc-R01-*`） |

## Beta 4 development (round A, 2026-09-28)

既知のレイアウト `Omsi23004` 上の開発候補 `4AF6A180…`（`research/reports/beta4-runtime/ROUND-A-REPORT.md`）。

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| プロセス内でのケイパビリティごとのネイティブ解決、アドレスを含まないネイティブ境界（既知のレイアウト） | RUNTIME_VALIDATED | A02 セッション `088f0491-114a-4841-afc0-e5ba483364b5`: ヘッドレス開始、NEW_MAP、クライアントの例 43 件中 40 件（失敗した 2 件は、ドキュメントに記載された `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE` と `OL_E_RUNTIME_SETTING_NOT_PERSISTENT`）、ネイティブ `SetTime` による `time set`、ネイティブ `MakeVehicle` による `vehicles spawn`。A03 セッション `cf49d2c1-ca69-41e2-af0c-c4b08fa083c3`: 保存済みシチュエーション `Linie 5`。セッションが所有するファイルは正確に復元されました。 |
| 既知のレイアウトを持たない OMSI 2 ビルドでのビルド非依存の解決 | STATICALLY_VALIDATED | オフライン: コード ID を伏せた参照実行ファイルで、レイアウトに依存しない 82 ターゲットのうち 81 が参照値に解決されます（`build-detection.reference-executable`、`native-resolution.per-capability`）。セッションを実行できる他の OMSI 2 ビルドは入手できませんでした。 |
| OMSI 2 の検出、プランのエビデンス、`profiles` レポート | RUNTIME_VALIDATED | インストールされた候補 `4af6a180` での A01: `is_omsi2`、identity `known`、`build_id` `Omsi23004`。セッション `088f0491` と `cf49d2c1` のプランはインストール環境を受け入れました。 |
| CLI まで保持される型付きランタイムエラーコード | RUNTIME_VALIDATED | A02 セッション `088f0491-114a-4841-afc0-e5ba483364b5`（上記のコードは変更されずに CLI に届きました）。 |
| HOF 管理（ミラー、正確な復元） | RUNTIME_VALIDATED | C01 セッション `e37318a2-73b3-4fb3-be11-b0225f15226d`: `Vehicles\MB_O305` は、セッション中 `Vehicles\MAN_SD202` の HOF 14 件とまったく同じ内容を保持し、タイムスタンプと属性を含めてバイト単位で復元されました。 |
| HOF 管理（クラッシュからのリカバリ） | RUNTIME_VALIDATED | C02 セッション `02568113-3f28-4e31-958b-23eac83d7b0b`: `Running` の間にオーナーと OMSI を強制終了し、`/recover` が `Vehicles\MAN_SD200` を正確に復元してジャーナルを削除しました。 |
| セッションプロファイルの 1..99 とディスカバリ | STATICALLY_VALIDATED | オフラインゲート `session-profiles.preset-range-1-99`、`session-profiles.discovery`、`session-profiles.hofs`。 |
| Internet Textures の `Disabled` | STATICALLY_VALIDATED | A04 はこのモードで開始し、正常に終了しました。`PluginStart` のテレメトリは Beta 3 の最新値スロットでは観測できませんでした。ラウンド B（下記）に置き換えられました。 |

## Beta 4 development (round B, 2026-09-29)

既知のレイアウト `Omsi23004` 上の開発候補 `beta4-b2` から `beta4-b5`（`research/reports/beta4-runtime/ROUND-B-REPORT.md`）。

| 機能 | 状態 | エビデンス |
| --- | --- | --- |
| テレメトリイベントリング（プラグインのすべてのイベントが順番に届く。単調増加するセッションティック） | RUNTIME_VALIDATED | B03 セッション `0c5ae237-c587-47b4-a409-a55989e7e38b`: `native.capabilities` と `internet-textures.suppressed` を含むすべての `PluginStart` イベントが順番に届き、`telemetry.lost` はありませんでした。B06 セッション `f231ab2a-5b0d-40e6-a21d-085e061f1de8`: イベントのティックは単調増加でした。 |
| 正規のケイパビリティカタログと、インストール環境に対する `GetCapabilitiesAsync` | RUNTIME_VALIDATED | ラウンド B、インストールされた候補 `1355c928` での B01: 操作、ネイティブケイパビリティ、ページング、変更ポリシーを持つカタログエントリ 53 件。`GetCapabilitiesAsync` は 68 エントリを評価しました（`time.read`: `native: RESOLVED_KNOWN_LAYOUT`）。B02 セッション `d875a24e-8154-4774-93b3-83567a90dbf1` は、導出された CLI ルートを通じてすべてのクライアントの例を実行しました。 |
| Runtime Modules v1: 信頼、プランニング、コンテンツによるアクティベーション、ライフサイクル、操作、`disable-module` | RUNTIME_VALIDATED | B05（インストール環境での信頼とプラン）。B06 セッション `f231ab2a-5b0d-40e6-a21d-085e061f1de8` と `227d35a3-7e3d-4fcb-9013-f129840c49a7`: 3 つのモジュールが読み込まれ、アームされ、準備完了になりました（1 つはコンテンツによるアクティベーション）。CLI を通じた操作と型付きエラー。失敗したモジュールは、OMSI が動作し続ける間に無効化され解放されました。 |
| Runtime Modules v1: `fail-session` | RUNTIME_VALIDATED | B07 セッション `59324c59-c3bc-4118-9fe9-57fbdc26f353`: 操作の失敗から 185 ms 後にセッションが `OL_E_MODULE_SESSION_FAILED` で終了し、OMSI は停止してファイルは復元されました。読み込み時の失敗はセッション `8b1d392f-4453-4e88-9231-d41a8fff1829` で確認しました。 |
| ネイティブモジュールコンポーネントとフックレジストリ | PARTIAL | セッション `f231ab2a-5b0d-40e6-a21d-085e061f1de8`、`227d35a3-7e3d-4fcb-9013-f129840c49a7`、`dfbd70a5-11a9-401d-967c-a9d154e15129`: `Omsi.exe` のインポートの IAT フックが設置、連鎖、解除されました。未知のターゲットは拒否されました。デバイスの `Present` と `Reset` のフックが設置されました（`Reset` は製品のものの上に積み重ねられました）。未解決: このインストール環境では、レジストリ外のコンポーネントが、ゲームプレイ開始の直後にランタイムの元の `Present` と `Reset` をデバイスの vtable に書き戻したため、デバイスのフックは維持されませんでした（モジュールの `Present` デトゥールは一度も実行されませんでした）。レジストリは両方のサイトが変更されたと報告し（`OL_E_MODULE_CLEANUP_INCOMPLETE`）、上書きしませんでした。ステージ E の項目です。 |
| ランタイムリストの一貫したページネーション | RUNTIME_VALIDATED | B04 セッション `8a45e79c-37c2-4f66-a3a3-b536a00a2421`（Berlin-Spandau）: 20 のリストが、行の重複なく正確にその件数までページングされました。メールボックスに収まるページでの 4747 件のツアーエントリと 825 件のトラックエントリを含みます。カーソルと limit のエラーは型付きでした。 |
| Internet Textures の `Disabled` | RUNTIME_VALIDATED | B03 セッション `0c5ae237-c587-47b4-a409-a55989e7e38b`: ネイティブの抑制が適用され（`internet-textures.suppressed`、`internet-textures.suppression.failed` なし）、セッションは正常に実行され終了しました。 |

<a id="consolidated-runtime_validation_required-list"></a>
## RUNTIME_VALIDATION_REQUIRED の統合リスト

2026-09-23 のランタイムクロージャーラウンドにより、以前のキューは完了しました。このインストール環境で安全に発生させることができない項目を除き、キューは空のままです。

1. Steam LAA のゲームプレイ、読み取り、コマンド、停止: 正規の Steam インストール環境が必要です（環境の問題。上の行は `PARTIAL`）。
2. RV-002 の RoadVehicle と Human の削除: 安全な削除を発生させる手段がありません（上の行は `PARTIAL`）。

製品の外部から発生させることができないためオフラインのままとなる項目は、それぞれの行に記載されており、キューには含まれません: メールボックス応答の破損、サイズ超過の応答、イベント履歴の切り詰め、D3D の `lost` 遷移、ログオフ、シムの終了コード 100-106、フィンガープリント前の遅延リカバリ分岐。

関連項目: [ケイパビリティ](../reference/capabilities.md)、[既知の制限事項](../reference/known-limitations.md)、[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)、[互換性](../reference/compatibility.md)。
