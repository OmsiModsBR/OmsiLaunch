<p align="center">
  <img src="../../assets/branding/omsilaunch-logo-en-preto.png" alt="OmsiLaunch" width="620">
</p>

<p align="center"><strong>OMSI 2 のためのセッション制御。</strong></p>
<p align="center">オープンソース · プログラム可能 · コミュニティ主導</p>

<p align="center">
  <a href="../../README.md">English (US)</a> ·
  <a href="README.en-GB.md">English (UK)</a> ·
  <a href="README.pt-BR.md">Português (Brasil)</a> ·
  <a href="README.pt-PT.md">Português (Portugal)</a> ·
  <a href="README.fr-FR.md">Français</a> ·
  <a href="README.de-DE.md">Deutsch</a> ·
  <a href="README.es-ES.md">Español (España)</a> ·
  <a href="README.es-LATAM.md">Español (Latinoamérica)</a> ·
  <a href="README.it-IT.md">Italiano</a> ·
  <a href="README.pl-PL.md">Polski</a> ·
  <a href="README.nl-NL.md">Nederlands</a> ·
  <a href="README.ru-RU.md">Русский</a> ·
  <a href="README.zh-CN.md">简体中文</a> ·
  <a href="README.zh-TW.md">繁體中文</a> ·
  <strong>日本語</strong>
</p>

---

> これは規範となる [英語版（US）README](../../README.md) の翻訳です。両者の内容が異なる場合は、英語版（US）README が優先されます。

# OmsiLaunch

**OmsiLaunch** は、OMSI 2 をプログラムから起動し、セッションを管理し、実行中の
シミュレーションを制御するためのオープンソースのレイヤーです。宣言的な記述から
セッションのプランを作成し、一時的な設定変更をすべてジャーナル付きトランザクションの
中で適用し、OMSI を起動してゲームプレイに入るまで監視します。さらに、公開 API を通じて
ツールが実行中のシミュレーションを読み取り・変更できるようにし、セッション終了時には
変更したすべてのファイルを復元します。

これはランチャー、ツール、自動化、コミュニティによる連携のためのインフラストラクチャです。
グラフィカルなランチャーではありません。

> **クリックではなく、セッションを定義する。**

## ステータス: 0.1.0-beta4

現在の公開ベータは **`0.1.0-beta4`** です。Beta 4 では、ケイパビリティごとの互換性、
ランタイムモジュール、1〜99 のセッションプロファイル、および開始機能とランタイム機能の一式が
追加されました。互換性、ケイパビリティカタログ、ページネーション、型付きエラー、モジュール
ブリッジは、実際の OMSI セッション（検証ラウンド A と B）で動作が確認されています。
これらのラウンドより後に追加された機能は `EXPERIMENTAL` です。静的解析で
クローズされ、オフラインでのみテストされており、ランタイム検証は保留中です。一部は引き続き
`STATICALLY_VALIDATED`、`PARTIAL`、または `UNAVAILABLE` です。
安全に再現できないため、2 つのランタイム項目が未解決のまま残っています。Steam
LAA でのゲームプレイと、道路車両および人物オブジェクトの自然な消滅（RV-002）です。
[ランタイム検証ステータス](../localized/ja-JP/status/runtime-validation-status.md)のページが、
OMSI 上で実行されたものとオフラインでのみ実行されたものを示す、規範となる記録です。

これはベータ版です。公開 API、CLI、ファイル形式はメンバーごとに `STABLE_BETA`、
`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`、`UNAVAILABLE` のいずれかに分類されており、
1.0 までに変更される可能性があります。

## 対応する OMSI の範囲

OmsiLaunch にはインストール済みの OMSI 2 が必要です。特定の `Omsi.exe` ファイルには
依存しません。すべてのネイティブケイパビリティは、使用される前に OMSI 内で個別に解決・検証され、
あるビルドで検証できないケイパビリティは、そのビルドでは利用不可になります。

| 項目 | 範囲 |
| --- | --- |
| 要件 | OMSI 2 がインストールされていること: OMSI 2（2.x）のバージョンを持つ 32 ビットの `Omsi.exe`、および `maps\` と `Vehicles\` ディレクトリ。それ以外は `OL_E_UNSUPPORTED_BUILD` で拒否されます。 |
| 既知のレイアウト | OMSI 2.3.004（`Omsi23004`）。すべてのケイパビリティが解決され、すべてのランタイム検証はこのレイアウトで実行されました。ハッシュは高速パスとしてこれを識別しますが、要件ではありません。 |
| その他の OMSI 2 ビルド | 各ケイパビリティは、ビルドに依存しない方式（Delphi RTTI、一意のコードシグネチャ、VMT スロット）で解決され、検証されます。オブジェクトのレイアウトを読み取るケイパビリティには、既知のレイアウトが必要です。 |
| Steam 実行ファイル | プロセス内で解決されます。正規の Steam セッションはランタイム検証されて**いません**。 |
| オペレーティングシステム | Windows 10 以降、x64。 |
| ランタイム | .NET 6 Desktop Runtime **x64**（コントローラー）と .NET 6 Runtime **x86**（プラグインは 32 ビットの `Omsi.exe` 内で動作します）。 |

詳細: [互換性](../localized/ja-JP/reference/compatibility.md)と
[インストール](../localized/ja-JP/getting-started/installation.md)。

## 提供するもの

**セッション。** セッションは `LaunchSpec`（CLI フラグ、JSON ファイル、
セッションプロファイル、または API）からプランが作成され、副作用なしで検証され（`/plan`）、
その後開始され、`SessionState` の遷移を通じて観察され、終了されます。セッションを停止すると
OMSI は強制終了されるため、OMSI が復元予定のファイルを上書きすることはありません。
[セッションのライフサイクル](../localized/ja-JP/concepts/session-lifecycle.md)を参照してください。

**トランザクションとリカバリ。** すべての設定オーバーライドはセッションに限定されます。
OmsiLaunch は変更する各ファイルについて、スナップショットの取得、ジャーナルへの記録、適用、
検証、復元を行います。これはクラッシュ後も同様です（`/recovery-status`、`/recover`、
`RecoverPendingAsync`）。恒久的な設定編集の機能は提供しません。
[トランザクションとリカバリ](../localized/ja-JP/concepts/transactions-and-recovery.md)を参照してください。

**CLI（`OmsiLaunch.exe`）。** 同じ公開 API の上に構築されたリファレンスフロントエンドで、
独自の OMSI ロジックは持ちません。探索、プランニング、セッションの開始、および実行中の
セッションに対するクライアントコマンドを提供します。[CLI リファレンス](../localized/ja-JP/reference/cli.md)
と [CLI の使用例](../localized/ja-JP/reference/cli-examples.md)を参照してください。

**`OmsiLaunchW.exe`。** ショートカット用の Windows サブシステムホストです。コンソール
ウィンドウなしで同じコマンドラインを受け付け、失敗はメッセージボックスで通知します。
[OmsiLaunchW.exe](../localized/ja-JP/reference/omsilaunchw.md)を参照してください。

**Windows トレイ。** すべてのオーナーセッションは、ステータスウィンドウとセッションを終了する
操作を備えた通知領域のアイコンを表示します。この操作は `session stop` と同じ停止経路を
たどります。[Windows トレイ](../localized/ja-JP/reference/windows-tray.md)を参照してください。

**セッションプロファイル。** 宣言的な `profile.yaml` パッケージ
（`omsilaunch.session-profile/v1`）を
`.omsilaunch\session-profiles\<id>\` 配下に置くことで、コンテンツ作成者は 1 つのコマンドで
開始できる再現可能なセッションを配布できます。
[セッションプロファイル](../localized/ja-JP/reference/session-profiles.md)を参照してください。

**公開 API とランタイム制御。** `OmsiLaunch.Api`（`IOmsiLaunch`）が推奨される製品
サーフェスです。時刻、天候、マップ、カメラ、時刻表、車両、人物オブジェクト、スクリプト変数、
D3D テクスチャなどのランタイム操作は、セッションに限定され、ビルドプロファイルに照らして
検証され、ネイティブポインターではなく不透明なセマンティックハンドルを通じて指定されます。
結果は 64 KiB のランタイムスロットによって上限が設けられています。
[公開 API リファレンス](../localized/ja-JP/reference/public-api.md)と
[ランタイム制御](../localized/ja-JP/reference/runtime-control.md)を参照してください。

**ローカル制御。** アクティブな `session_id` にバインドされた、インストール環境ごとの
名前付きパイプにより、同じユーザーの他のプロセスがステータスとイベントを読み取り、
セッションを停止し、公開ランタイム操作を実行できます。
[ローカル制御 / IPC](../localized/ja-JP/reference/local-control.md)を参照してください。

**ケイパビリティ。** すべてのケイパビリティと公開ランタイム操作は、その安定性とともに
カタログ化されています。試験的なケイパビリティや利用不可のケイパビリティも隠されずに
一覧に表示されます（`OmsiLaunch.exe capabilities`）。
[ケイパビリティ](../localized/ja-JP/reference/capabilities.md)を参照してください。

**常駐プラグイン。** インプロセスのプラグインクロージャは `plugins\OmsiLaunch.*` 配下に
一度だけインストールされます。開始のたびに、`release-manifest.json` の SHA-256
エントリと照合されます。サードパーティのプラグインには一切触れません。
[常駐プラグインモデル](../localized/ja-JP/concepts/permanent-plugin.md)を参照してください。

## クイックスタート

リリースパッケージを OMSI 2 のインストールルートに展開し、そのディレクトリで
次のコマンドを実行します。

```text
OmsiLaunch.exe /version
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

そのセッションの実行中は、同じディレクトリで開いた 2 つ目のコンソールから
セッションを照会したり終了したりできます。

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe time get
OmsiLaunch.exe session stop
```

手順の解説: [最初のセッション](../localized/ja-JP/getting-started/first-session.md)。.NET
インテグレーター向け: [公開 API クイックスタート](../localized/ja-JP/getting-started/api-quick-start.md)。

## ドキュメント

ドキュメント一式の索引は [`docs/localized/ja-JP/README.md`](../localized/ja-JP/README.md) にあります。
`docs/` 配下の英語版（US）ページが原文であり、規範です。

| トピック | ページ |
| --- | --- |
| 公開 API | [docs/localized/ja-JP/reference/public-api.md](../localized/ja-JP/reference/public-api.md) |
| CLI | [docs/localized/ja-JP/reference/cli.md](../localized/ja-JP/reference/cli.md) |
| CLI の使用例 | [docs/localized/ja-JP/reference/cli-examples.md](../localized/ja-JP/reference/cli-examples.md) |
| OmsiLaunchW.exe | [docs/localized/ja-JP/reference/omsilaunchw.md](../localized/ja-JP/reference/omsilaunchw.md) |
| Windows トレイ | [docs/localized/ja-JP/reference/windows-tray.md](../localized/ja-JP/reference/windows-tray.md) |
| セッションプロファイル | [docs/localized/ja-JP/reference/session-profiles.md](../localized/ja-JP/reference/session-profiles.md) |
| ランタイム制御 | [docs/localized/ja-JP/reference/runtime-control.md](../localized/ja-JP/reference/runtime-control.md) |
| ローカル制御 / IPC | [docs/localized/ja-JP/reference/local-control.md](../localized/ja-JP/reference/local-control.md) |
| ケイパビリティ | [docs/localized/ja-JP/reference/capabilities.md](../localized/ja-JP/reference/capabilities.md) |
| エラーと終了コード | [docs/localized/ja-JP/reference/errors.md](../localized/ja-JP/reference/errors.md), [docs/localized/ja-JP/reference/exit-codes.md](../localized/ja-JP/reference/exit-codes.md) |
| パッケージング | [docs/localized/ja-JP/reference/packaging.md](../localized/ja-JP/reference/packaging.md) |
| 既知の制限事項 | [docs/localized/ja-JP/reference/known-limitations.md](../localized/ja-JP/reference/known-limitations.md) |
| ランタイム検証ステータス | [docs/localized/ja-JP/status/runtime-validation-status.md](../localized/ja-JP/status/runtime-validation-status.md) |

### 他の言語のドキュメント

ドキュメントは 14 のロケールに翻訳されており、
[`docs/localized/`](../localized/LOCALIZATION-MANIFEST.md) 配下にあります。翻訳は規範となる
英語版（US）ページから作成され、原文と機械的に照合して検証されています。ネイティブ
スピーカーによる編集レビューは Beta 4 リリースには含まれておらず、公開後に実施される
可能性があります。翻訳と英語版のページが食い違う場合は、英語版のページが優先されます。

## 制限事項と未解決の項目

以下は最も重要な制限事項です。完全な一覧は
[既知の制限事項](../localized/ja-JP/reference/known-limitations.md)にあります。

- **Steam。** Steam LAA は `PARTIAL` です。ゲームプレイ、読み取り、コマンド、停止には
  正規の Steam インストール環境が必要であり、ランタイム検証されていません。
- **このベータでは試験的:** 最後のマップ状態からの開始（`/last`）と、開始時の日付・時刻・年・
  天候の明示指定は、静的にクローズされており、ランタイム検証を待っています。
  **利用不可:** 開始時のプレイヤー車両の割り当て。これを要求すると、黙って無視されるのではなく、
  プランが実行不可になります。
- **ランタイムでの書き込みは限定的。** `weather.set`、`weather.metar.set`、文字列変数の書き込み、
  スクリプトトリガーは、ランタイム検証されるまで試験的です。カレンダーの書き込みと車両の移動は
  利用できません。ランタイムでの変更はジャーナルに記録されず、復元もされません。
- **ハンドルの有効期間。** 自然に消滅する道路車両と人物オブジェクトに対する失効の検出（RV-002）には
  安全なランタイム上の再現手段がなく、オフラインでのみ検証されています。
- **ページングされた結果。** ランタイムのリストは `cursor` と `limit` でページングされ、
  `next_cursor` から続きを取得します。64 KiB のメールボックスに収まらないページは切り詰められ、
  `truncated=true` が付きます。
- **停止はデフォルトで強制的。** OMSI は強制終了され、OMSI 自身のシャットダウン処理は実行されず、
  保存されていない OMSI の状態は失われます。オプトインの協調シャットダウン
  （`/cooperative-shutdown`、試験的）では、先に OMSI の終了ルーチンが実行されます。
- **同一ユーザーの信頼モデル。** 同じ Windows ユーザーのプロセスであれば、どのプロセスでも
  ローカルコントロールプレーンにアクセスできます。

## ダウンロード

[リリースページ](https://github.com/lmonteirotech/OmsiLaunch/releases)から
**`OmsiLaunch-0.1.0-beta4.zip`** とその `.sha256` ファイルをダウンロードします。
対応する OMSI ルートに直接展開してください。パッケージには、コントローラー
（`OmsiLaunch.exe`、`OmsiLaunchW.exe`）、その依存関係、`release-manifest.json` を含む
常駐プラグインのクロージャ、スプラッシュ用アセット、セッションの例、および
`.omsilaunch\docs\` 配下のオフラインドキュメントが含まれています。パッケージのレイアウトと
クリーンな削除方法: [パッケージング](../localized/ja-JP/reference/packaging.md)。

## ソースからのビルド

要件:

- Windows 10 以降、x64。
- .NET 6 SDK、およびテスト実行用の x64 と x86 の .NET 6 ランタイム。
- 3 つのネイティブプロジェクト `OmsiLaunch.Native.x86`（Win32）、
  `OmsiLaunch.Bootstrapper` と `OmsiLaunch.WindowsHost`（x64）のための、MSBuild C++ ワークロード
  （プラットフォームツールセット `v145`）と Windows 10 SDK を備えた Visual Studio。

`OmsiLaunch.sln` にはマネージドプロジェクトとテストスイートが含まれています。唯一の
オフライン用エントリポイントがすべてをビルドし、すべてのオフラインスイートを実行します。
OMSI を起動することはありません。

```powershell
powershell -ExecutionPolicy Bypass -File tools\Invoke-OfflineValidation.ps1
```

`-SkipNative` はネイティブプロジェクトとパッケージングの回帰テストをスキップします。
`-SkipDocs` はドキュメントゲートをスキップします。ビルド出力は `artifacts\` に置かれ、
バージョン管理の対象外です。`tools\New-ReleasePackage.ps1` は既存のビルドからリリース
パッケージのステージング、ハッシュ計算、検証、圧縮を行います。
[パッケージング](../localized/ja-JP/reference/packaging.md)と
[テストと検証](../project/TESTING-AND-VALIDATION.md)を参照してください。

| パス | 内容 |
| --- | --- |
| `src/` | 製品ライブラリ: API、コア、構成、コンテンツ、相互運用、プロセス、プラグイン、ビルドプロファイル、ネイティブ x86 境界 |
| `tools/` | CLI と Windows ホスト（`OmsiLaunch.Cli`）、ネイティブシム（`OmsiLaunch.Bootstrapper`）、オフラインテストホスト、パッケージングおよび検証スクリプト、ローカライズ用ツール |
| `tests/` | ユニット、統合、プロファイル、Windows UI、ドキュメントの各テストスイート |
| `docs/` | 規範となるドキュメントと、`docs/localized/` 配下のその翻訳 |
| `examples/` | LaunchSpec とセッションの例 |
| `assets/` | ブランディング、アイコン、パッケージ用アセット |
| `third_party/` | アップストリームの出所に関するメモ |

メンテナー向けの要約: [PUBLIC-API.md](../specs/PUBLIC-API.md)、
[RUNTIME-CONTROL.md](../specs/RUNTIME-CONTROL.md)、
[RUNTIME-CAPABILITIES.md](../specs/RUNTIME-CAPABILITIES.md)、
[BUILD-PROFILES.md](../specs/BUILD-PROFILES.md)、
[IMPLEMENTATION-STATUS.md](../project/IMPLEMENTATION-STATUS.md)、
[POST-RELEASE-BACKLOG.md](../project/POST-RELEASE-BACKLOG.md)。

## コミュニティとライセンス

OmsiLaunch はコミュニティ志向のオープンソースプロジェクトです。他の OMSI ランチャーとは
独立しており、互換性のあるコミュニティツールはこれを基盤として構築できます。

OmsiLaunch は [LGPL-3.0-only](../../LICENSE) の下でライセンスされています。組み込まれたソースの
出所と適用される通知については、[サードパーティ通知](../../THIRD-PARTY-NOTICES.md)を
参照してください。
