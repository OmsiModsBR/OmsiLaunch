# OmsiLaunch ドキュメント

<!-- l10n: source=README.md source-sha256=09275bd568ca2ebaee648db8894d7773a8b94bafdbb324b3d8b53d897597b0cb -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../README.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

これは、ハードニング後のベースラインである OmsiLaunch `0.1.0-beta4` の規範となる英語ドキュメントです。OmsiLaunch は、インストールされた OMSI 2 に対して、プログラム可能な起動、セッションの所有、およびランタイム制御を提供します。各ネイティブケイパビリティはビルドごとに解決され検証されます（[互換性](reference/compatibility.md)を参照）。`docs/` 配下のすべてのページは現在のコードの動作を記述しています。ページとコードが食い違う場合はコードが優先され、そのページがバグとなります。

全体を通じて使用する安定性の用語: `STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`、`UNAVAILABLE`。解析はされるものの何の効果もないフラグは `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` と表記されます。[ランタイム検証ステータス](status/runtime-validation-status.md)にそう記載されていない限り、ランタイム検証済みとは呼びません。

<a id="who-reads-what"></a>
## 対象読者別の読み方

| 対象読者 | 最初に読むページ | 次に読むページ |
| --- | --- | --- |
| 利用者（CLI、ショートカット、セッションプロファイル） | [インストール](getting-started/installation.md)、[最初のセッション](getting-started/first-session.md) | [CLI リファレンス](reference/cli.md)、[CLI の使用例](reference/cli-examples.md)、[セッションプロファイル](reference/session-profiles.md)、[Windows トレイ](reference/windows-tray.md)、[終了コード](reference/exit-codes.md) |
| インテグレーター（`OmsiLaunch.Api`、ローカル IPC） | [公開 API クイックスタート](getting-started/api-quick-start.md)、[公開 API リファレンス](reference/public-api.md)、[LaunchSpec リファレンス](reference/launchspec.md) | [セッションのライフサイクル](concepts/session-lifecycle.md)、[ランタイム制御](reference/runtime-control.md)、[ケイパビリティ](reference/capabilities.md)、[ローカル制御 / IPC](reference/local-control.md)、[エラーリファレンス](reference/errors.md) |
| メンテナー（リリース、検証、境界） | [パッケージング](reference/packaging.md)、[常駐プラグインモデル](concepts/permanent-plugin.md) | [トランザクションとリカバリ](concepts/transactions-and-recovery.md)、[互換性](reference/compatibility.md)、[既知の制限事項](reference/known-limitations.md)、[ランタイム検証ステータス](status/runtime-validation-status.md) |

<a id="navigation"></a>
## ナビゲーション

| ページ | 目的 |
| --- | --- |
| [はじめに](getting-started/first-session.md) | OMSI ルートから 1 つのセッションのプランを作成し、開始し、観察し、停止します。 |
| [インストール](getting-started/installation.md) | 前提条件、OMSI ルートへのパッケージの展開、`/version` による確認、クリーンな削除。 |
| [公開 API クイックスタート](getting-started/api-quick-start.md) | 1 つのセッションのプランを作成し、開始し、読み取り、停止する完全な .NET プログラム。 |
| [CLI リファレンス](reference/cli.md) | `OmsiLaunch.exe` / `OmsiLaunchW.exe` のすべてのフラグ、コマンドワード、階層ルート。 |
| [CLI の使用例](reference/cli-examples.md) | よくある作業のための、コピー＆ペーストで使えるコマンドライン。 |
| [LaunchSpec リファレンス](reference/launchspec.md) | `LaunchSpec` のすべてのプロパティと列挙値、`/spec` の JSON 読み込み規則。 |
| [HOF 管理](reference/hof-management.md) | セッションスコープの HOF のコピーと車両フォルダーへのミラーリング: ソース、ターゲット、競合、復元。 |
| [ランタイムモジュール](../../reference/runtime-modules.md) | セッションとともに OMSI 内で動作するモジュール: マニフェスト、信頼、ライフサイクル、失敗ポリシー、操作。 |
| [セッションプロファイルリファレンス](reference/session-profiles.md) | `profile.yaml` のスキーマ `omsilaunch.session-profile/v1`、キー、上限、優先順位。 |
| [公開 API リファレンス](reference/public-api.md) | `IOmsiLaunch`、公開レコードと列挙型、メンバーごとの安定性。 |
| [公開 API インベントリ](reference/public-api-inventory.md) | すべての公開型とメンバーをシグネチャと安定性付きで列挙した自動生成リスト。 |
| [ランタイム制御](reference/runtime-control.md) | ランタイムコマンドチャネル、タイムアウト、ハンドル、停止のセマンティクス。 |
| [ケイパビリティリファレンス](reference/capabilities.md) | ケイパビリティカタログと、すべての公開ランタイム操作 ID およびその分類。 |
| [セッションのライフサイクル](concepts/session-lifecycle.md) | `SessionState` の遷移、`StartSessionAsync` が保証する内容、セッションの終了方法。 |
| [トランザクションとリカバリ](concepts/transactions-and-recovery.md) | ジャーナルの状態、バックアップ、復元の検証、セッションによる削除、クラッシュからのリカバリ。 |
| [常駐プラグインモデル](concepts/permanent-plugin.md) | `plugins\OmsiLaunch.*` のクロージャ、マニフェストに基づく整合性、セッションが決して触れないもの。 |
| [ローカル制御 / IPC](reference/local-control.md) | 名前付きパイププロトコル `0.1`、インストール環境ごとのエンドポイント、`session_id` のバインド、信頼モデル。 |
| [OmsiLaunchW.exe](reference/omsilaunchw.md) | Windows（コンソールなし）ホスト: `OmsiLaunch.exe` との違い、`/silent`、ダイアログ、終了コード。 |
| [Windows トレイ](reference/windows-tray.md) | 通知領域のインジケーター: アイコン、メニュー、ステータスウィンドウの各フィールド、End session、エクスプローラーの再起動。 |
| [エラーリファレンス](reference/errors.md) | すべての `OL_E_*` / `OL_W_*` コードとそのカテゴリおよび意味。 |
| [終了コード](reference/exit-codes.md) | `PublicExitCode` の値 0 から 10 と、ブートストラッパーシムのコード 100 から 106。 |
| [パッケージング / インストールレイアウト](reference/packaging.md) | リリース ZIP 内のファイル、`release-manifest.json`、`.omsilaunch\` のレイアウト。 |
| [互換性 / 対応 OMSI ビルド](reference/compatibility.md) | 唯一の対応 `Omsi.exe` ハッシュ、受け入れられる Steam LAA ハッシュ、プラットフォーム要件。 |
| [既知の制限事項](reference/known-limitations.md) | このベータで非対応、部分的、または受容済みリスクであるもの。 |
| [ランタイム検証ステータス](status/runtime-validation-status.md) | OMSI 上で実行されたもの、オフラインでのみ実行されたもの、まだ実際のセッションを必要とするもの。 |

メンテナー向けに引き続き規範となるページ: リポジトリの
[`README.md`](../../../README.md)、`docs/specs` 配下の仕様
（[`PUBLIC-API.md`](../../specs/PUBLIC-API.md)、
[`RUNTIME-CONTROL.md`](../../specs/RUNTIME-CONTROL.md)、
[`RUNTIME-CAPABILITIES.md`](../../specs/RUNTIME-CAPABILITIES.md)、
[`BUILD-PROFILES.md`](../../specs/BUILD-PROFILES.md)）、および
`docs/project` 配下のプロジェクトページ（[`IMPLEMENTATION-STATUS.md`](../../project/IMPLEMENTATION-STATUS.md)、
[`TESTING-AND-VALIDATION.md`](../../project/TESTING-AND-VALIDATION.md)、
[`POST-RELEASE-BACKLOG.md`](../../project/POST-RELEASE-BACKLOG.md)）。これらは要約であり、詳細なリファレンスは上記のページです。過去のページは[ドキュメントマニフェスト](../../DOCUMENTATION-MANIFEST.md)に一覧があります。

<a id="how-this-documentation-is-kept-in-sync"></a>
## このドキュメントを同期させる仕組み

ドキュメントゲート `tests\OmsiLaunch.DocumentationTests` は、`OmsiLaunch.Api` と `OmsiLaunch.Core` に対してコンパイルされ、上記のページと公開サーフェスを定義するコードとを照合します。

| ゲート | 検査内容 |
| --- | --- |
| `docs.cli-flags` | `CliInput.KnownFlags` のすべてのエントリが CLI リファレンスに `` `/flag` `` または `` `/flag:` `` として記載されていること。`CliInput.AcceptedNoEffectFlags` のすべてのエントリが、その行で `ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT` と表記されていること。すべてのコマンドワードとすべての `CliInput.HierarchicalRoutes` ルートが、そのランタイム操作とともに記載されていること。すべての `PublicExitCode` 値が終了コード表に `| n |` 行を持つこと。 |
| `docs.capabilities` | すべての `PublicCapabilityRegistry.All` の ID、すべての `PublicCapabilityRegistry.PublicRuntimeOperationIds` のエントリ、すべての `PublicCapabilityClassification` の名前がケイパビリティリファレンスに記載されていること。 |
| `docs.errors` | すべての `PublicErrorCodes.All` のコードがエラーリファレンスに記載されていること、および `src\` または `tools\OmsiLaunch.Cli\` 内のどの `OL_E_*` / `OL_W_*` リテラルも `PublicErrorCodes` から漏れていないこと。 |
| `docs.public-api` | `OmsiLaunch.Api` のすべてのエクスポートされた型、すべての列挙値、すべての `IOmsiLaunch` メンバーが公開 API リファレンスに記載され、5 つの安定性の用語がすべて使用されていること。 |
| `docs.launchspec` | `LaunchSpec` から到達可能なすべての公開プロパティと、その列挙型のすべての値が LaunchSpec リファレンスに記載されていること。 |
| `docs.session-profiles` | すべての `SessionProfileCompiler.SchemaKeys` のキー、スキーマ識別子、`256 KiB` の上限がセッションプロファイルリファレンスに記載されていること。 |
| `docs.structure` | ナビゲーション表のすべてのページが存在すること。 |
| `docs.links` | `docs\**\*.md`（`docs\localized\` を除く）およびルートの `*.md` ファイル内のすべての相対リンクが、ファイルまたはディレクトリに解決されること。 |
| `docs.localization` | `docs\localized\LOCALIZATION-MANIFEST.md` に記載されたすべてのロケールが、ローカライズ対象のページ一式をすべて備えていること。各ページが英語版ページの見出し、表、コードブロック、すべてのインラインコードスパン（フラグ、ケイパビリティ ID と操作 ID、エラーコード、キー、識別子）およびすべてのリンクを保持し、その相対リンクが解決されること。 |

このゲートは `tools\Invoke-OfflineValidation.ps1` が実行するスイートの 1 つです（`-SkipDocs` でスキップできます）。オフラインで実行され、OMSI を起動することはなく、フラグ、ルート、ケイパビリティ、エラーコード、列挙値、公開型のいずれかが文書化されていない場合やリンクが壊れている場合にビルドを失敗させます。本文の記述内容は検査しないため、ページが動作について誤っている可能性は残ります。その場合はそのページに対するバグとして報告してください。

<a id="translations"></a>
## 翻訳

`docs\localized\<locale>\` には、この `0.1.0-beta4` ドキュメントの `pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW`、`ja-JP` 向けの完全な翻訳が収められています。ページ一式、ロケールのルート、および意図的に翻訳しないページは [`localized/LOCALIZATION-MANIFEST.md`](../LOCALIZATION-MANIFEST.md) に一覧があります。翻訳では英語版ページのすべてのコマンド、フラグ、識別子、エラーコード、例を変更せずに保持し、`docs.localization` ゲートがそれを検査します。規範となる情報源は引き続き英語版ページです。翻訳が英語版と食い違う場合は、英語版ページとコードが正であり、その翻訳がバグとなります。
