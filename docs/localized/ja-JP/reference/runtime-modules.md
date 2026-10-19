# ランタイムモジュール

<!-- l10n: source=reference/runtime-modules.md source-sha256=242dc9ea597f243b633862d5e39528d52f684968ee8c4f94f8e2cd965ab36323 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/runtime-modules.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

ランタイムモジュールは、管理対象の OMSI セッションに動作を追加します。たとえばマップの HUD や読み込みの最適化のように、セッションと一緒に動作する機能であり、OmsiLaunch 自体の一部にはなりません。ブリッジはモジュールを読み込み、モジュールが宣言したものだけを渡し、セッションが終了したとき、またはモジュールが失敗したときに、モジュールが保持しているものをすべて解放します。このページは、`src/OmsiLaunch.Runtime.Modules`（マニフェスト、設定、適格性判定、ライフサイクルホスト）、プランナー、コントローラーのセッションプログラム（`ModuleSessionPrograms`、`ModuleSessionProcess`）、プラグインのモジュール環境、および [CLI](cli.md) のモジュールフラグが実装するランタイムモジュールの規範的なリファレンスです。

安定性: `EXPERIMENTAL`。ブリッジプロトコル 1 は B ラウンドでランタイム検証済みです（マネージドモジュール、コンテンツアクティベーション、操作、`disable-module` と `fail-session`、IAT フック付きのネイティブコンポーネント。[ランタイム検証の状況](../status/runtime-validation-status.md)を参照してください）。ブリッジプロトコル 2（設定、既定のアクティベーション、設定ファイル、セッションプログラム、プラグインの競合、通知、`get_value`）はオフラインのみで検証されています。オフラインゲートは次のとおりです。`tests/OmsiLaunch.ModuleTests` の `module.*` ゲート（マニフェスト、整合性、選択とアクティベーション、設定、設定ファイル、プラグインの競合、ジョブ経由で開始されるセッションプログラム、失敗ポリシーごとの全フェーズへの障害注入、ネイティブコンポーネントのライフサイクル、クリーンアップ、ハンドオフ）、`plugin-runtime.modules-lifecycle`（プラグインの環境が読み込む実際のマネージドモジュール）、`runtime-modules.native-component`（インポートをフックし、ブリッジ経由で値を読み取る実際のネイティブコンポーネント）、`runtime-modules.plugin-conflict`、`native-hooks.self-check`（フックエンジン）。ブリッジプロトコル 3（Beta 5: プロセス内のカタログ操作、ティック、入力キャプチャ、コミュニティコンテンツ入力、プラン時スナップショットを伴う任意のピン、`module pack`、補助ジョブオプション、OMSI バージョン）はオフラインでのみ検証されています。`tests/OmsiLaunch.ModuleTests` の `module.*` ゲート（`manifest.protocol-3`、`manifest.optional-pins`、`catalog.snapshot`、`content.realmap-package`、`packer.pins`、`handoff.v8-snapshot`、`lifecycle.runtime-operations`、`lifecycle.tick.*`、`lifecycle.input-capture`、`lifecycle.auxiliary-options`、`lifecycle.snapshot-before-components`）、プラグイン統合テストの `runtime-modules.protocol-3`、`runtime-modules.auxiliary-job`、`ui-pause.fails-closed`、`ui-input.hook-self-check`、およびユニットテストの `omsi-version.literal` です。ポーズメニューの引き継ぎと入力キャプチャは、実際の OMSI セッションではまだ観測されていません。

読み込みはオープンです。`plugins\OmsiLaunch.Modules` 以下にインストールされたモジュールであれば、作成者を問わず読み込めます。OmsiLaunch は、モジュールが OMSI の内部で行うことを保証しません。OmsiLaunch が保証するのは[境界](#security-boundary)です。モジュールが OmsiLaunch プロセスの内部で実行されることは決してなく、OmsiLaunch が提供するものを使って、モジュールが OMSI 自身の権限を超えて到達することもできません。

<a id="where-modules-live"></a>
## モジュールの配置場所

各モジュールは、常駐プラグインフォルダー以下のディレクトリに置かれます。

```text
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\module.json
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<managed component>.dll
<OMSI root>\plugins\OmsiLaunch.Modules\<id>\<assets and other components>
```

ディレクトリ名はモジュールの `id` と一致している必要があります。モジュールは配布者がインストールします。セッションがそこにあるファイルを書き込んだり、スナップショットを取ったり、復元したりすることはなく、モジュールが OmsiLaunch 経由でインストール環境のファイルを変更することもありません（マニフェストにはインストールオーバーレイを宣言する手段がありません）。モジュールの書き込み可能なデータは、[データディレクトリ](#data-directory)に置かれます。

<a id="manifest-modulejson"></a>
## マニフェスト（`module.json`）

スキーマは `omsilaunch.runtime-module/v1` です。未知のキーは拒否されるため、より新しいスキーマ向けに書かれたマニフェストは、中途半端に解釈されることなく拒否されます。パスはすべてモジュールのディレクトリからの相対パスです（バインドされたコンテンツのパスは OMSI ルートからの相対パスです）。区切りには `\` または `/` を使い、`..` やドライブを含めることはできません。

| キー | 必須 | 意味 |
| --- | --- | --- |
| `schema` | 必須 | `omsilaunch.runtime-module/v1`。 |
| `id` | 必須 | モジュール ID。小文字の英字、数字、`-` で構成される 2～48 文字で、英字または数字で始まります。ディレクトリ名および操作の名前空間も兼ねます。 |
| `version` | 必須 | `major.minor.patch`。必要に応じて `-suffix` を付けられます。 |
| `name` | 任意 | 表示名（128 文字以内）。 |
| `bridge_protocol` | 必須 | `{ "min": n, "max": n }`: モジュールが動作するブリッジプロトコル。このビルドはプロトコル `1`、`2`、`3` に対応しており、範囲にそのいずれかが含まれるモジュールが受け入れられます。 |
| `activation` | 任意 | `requested`（既定）: セッションが要求したときに読み込まれます。`content`: セッションがバインドされたマップを実行するときにも読み込まれます（`content_binding` が必要）。`default`（プロトコル 2）: 無効化されていない限り、すべてのセッションで読み込まれます（[セッションで使用するモジュールの選択](#choosing-modules-for-a-session)を参照）。`content_binding` を持つことはできません。 |
| `required_capabilities` | 任意 | モジュールがなければ動作できないケイパビリティ（[ケイパビリティ](#capabilities)を参照）。 |
| `optional_capabilities` | 任意 | 利用可能な場合にモジュールが使用するケイパビリティ。モジュールは `IsCapabilityAvailable` で確認します。 |
| `content_binding` | 任意 | `{ "map": "maps\\<folder>\\global.cfg", "files": [ { "path", "sha256" } ] }`: モジュールは、そのマップで、かつそれらのファイルが変更されていない場合にのみ実行されます。 |
| `assets` | 任意 | モジュールが読み取るファイル。それぞれ `{ "path", "sha256" }` で指定します。プロトコル 3 以降は `sha256` は任意です（[整合性](#integrity)を参照）。 |
| `components` | 必須 | `managed` コンポーネントをちょうど 1 つ `{ "kind": "managed", "path": "<file>.dll", "sha256", "entry": "<Namespace.Type>" }` で指定します。任意で `native` コンポーネント（`.dll`）を 1 つ、`auxiliary` コンポーネント（`.exe`）を複数、さらにプロトコル 2 では `host` コンポーネント（`.exe`、モジュールの[セッションプログラム](#session-programs)）を 1 つ指定できます。プロトコル 3 以降はコンポーネントの `sha256` は任意で、`auxiliary` コンポーネントには `priority`（`normal`、`below-normal`、`idle`）と `memory_limit_mb`（64～65536）を指定できます。プログラムのジョブオブジェクトが、最初の命令からこれらを適用します（[補助プログラム](#auxiliary-programs)を参照）。 |
| `failure_policy` | 必須 | `fail-session`、`disable-module`、`degrade` のいずれか（[失敗](#failures)を参照）。 |
| `operations` | 任意 | セッションの実行中にモジュールが公開する操作: `{ "id", "kind": "read"\|"write"\|"action", "arguments": [ { "name", "required" } ], "timeout_ms" }`。 |
| `settings` | 任意 | プロトコル 2。モジュールが受け付ける設定（[設定](#settings)を参照）。 |
| `conflicts` | 任意 | プロトコル 2。`{ "plugins": [ "<file>.dll" ] }`: モジュールと同時に実行してはならない OMSI プラグイン DLL（[OMSI プラグインとの競合](#conflicts-with-omsi-plugins)を参照）。 |
| `runtime_operations` | 任意 | プロトコル 3。モジュールがプロセス内で実行する公開ケイパビリティカタログの操作。たとえば `["time.read", "camera.set"]`（[プロセス内の操作](#operations-in-process)を参照）。`bridge.runtime` ケイパビリティが必要です。 |
| `tick_ms` | 任意 | プロトコル 3。モジュールが `tick_ms` ミリ秒ごとの定期コールバックを要求します。50 の倍数で、50～1000（[ティック](#tick)を参照）。`bridge.tick` ケイパビリティが必要です。 |
| `content_inputs` | 任意 | プロトコル 3。モジュールが読み取り、ブリッジが検索するコミュニティコンテンツ: `[ { "kind": "realmap-package", "required": false } ]`（[コミュニティコンテンツ](#community-content)を参照）。 |
| `timeouts` | 任意 | ミリ秒単位の予算: `arm_ms`、`gameplay_ready_ms`、`stop_ms`（いずれも既定 5000、最大 30000）。プロトコル 2 では、セッションプログラム用に `prepare_ms`（既定 120000、最大 1800000）と `host_stop_ms`（既定 10000、最大 60000）も指定できます。操作の既定値は 2000 ms です。 |
| `diagnostics` | 任意 | `{ "trace": true\|false }`。 |

プロトコル 2 の機能（`settings`、`conflicts`、アクティベーション `default`、`host` コンポーネント、`prepare_ms`、`host_stop_ms`）を使用するマニフェストは、`bridge_protocol.min` を 2 と宣言する必要があります。プロトコル 3 の機能（`runtime_operations`、`tick_ms`、`content_inputs`、`bridge.runtime`・`bridge.tick`・`bridge.input` ケイパビリティ、補助コンポーネントの `priority` または `memory_limit_mb`、`sha256` のないコンポーネントまたはアセット）を使用するマニフェストは、`bridge_protocol.min` を 3 と宣言する必要があります。

`components` と `assets` で指定されたすべてのファイルは、SHA-256 で固定できます（プロトコル 3 より前は必須、プロトコル 3 以降は任意です。[整合性](#integrity)を参照）。`module.json` 自身を固定することはできません。

```json
{
  "schema": "omsilaunch.runtime-module/v1",
  "id": "example-preload",
  "version": "1.0.0",
  "bridge_protocol": { "min": 2, "max": 2 },
  "activation": "default",
  "required_capabilities": ["bridge.events", "bridge.hooks.iat"],
  "components": [
    { "kind": "managed", "path": "Example.Preload.dll", "sha256": "<64 hex digits>", "entry": "Example.Preload.PreloadModule" },
    { "kind": "native", "path": "example-preload.dll", "sha256": "<64 hex digits>" },
    { "kind": "host", "path": "ExamplePreloader.exe", "sha256": "<64 hex digits>" }
  ],
  "settings": [
    { "key": "ram_budget_mb", "type": "integer", "default": "auto", "auto": true, "min": 256, "max": 12288, "flag": "example-ram" },
    { "key": "monitor", "type": "boolean", "default": false, "flag": "example-monitor" }
  ],
  "conflicts": { "plugins": ["OldExamplePlugin.dll"] },
  "failure_policy": "degrade",
  "timeouts": { "prepare_ms": 300000 }
}
```

<a id="integrity"></a>
## 整合性

マニフェストが指定するすべてのファイルは、存在し、ジャンクションやシンボリックリンクを経由せずに到達できる必要があり、作成者が固定したすべてのファイルは、固定された SHA-256 を持っている必要があります。そうでない場合、モジュールは `OL_E_MODULE_INTEGRITY` で拒否されます。これにより、破損したモジュールや更新が中途半端なモジュールを検出できます。これは信頼の判断ではありません。モジュールが、いかなるリスト、署名、発行元とも照合されることはありません。

プロトコル 3 以降は固定が任意なので、作成者がハッシュを手作業で管理する必要はなく、プランは常に**スナップショット**を取得します。スナップショットとは、マニフェストが指定するすべてのファイルとその内容にわたる 1 つの SHA-256 であり、プランに記録され（`PlannedModule.SnapshotSha256`）、スタートアップハンドオフ（ワイヤーバージョン 8）でプラグインに運ばれます。スナップショットは、プランが評価したファイルが、実際に実行されるファイルであることを保証するものです。プラグインは、モジュールの `module.json` がプランで評価された SHA-256 を持ち、そのファイルがスナップショットの内容のままである場合にのみモジュールを読み込み、コントローラーとブリッジは、セッションプログラム、ネイティブコンポーネント、補助プログラムが開始される直前にもう一度スナップショットを比較します。プラン作成後にファイルが変更されたモジュールは読み込まれません（`OL_E_MODULE_INTEGRITY`、「the module's files changed after planning」）。スナップショットは、固定の有無にかかわらずすべてのモジュールを対象とします。固定は、モジュールを公開する側が、破損したコピーや更新が中途半端なコピーを検出されたらすぐに検出できるようにするためのものです。

このチェックは、モジュールのコードが開始される箇所でも繰り返されます。プラグインは、モジュールの `module.json` がプランで評価された SHA-256 のままであり、すべての固定とスナップショットが引き続き成立している場合にのみモジュールを読み込みます。コントローラーは、セッションプログラムの固定とスナップショットを検証した後にのみ、そのプログラムを開始します。

<a id="packing-a-module"></a>
### モジュールのパッキング

`OmsiLaunch.exe /pack-module:<module directory>` は、`module.json` が指定するファイル（コンポーネントとアセット）の固定を書き込むため、作成者が SHA-256 を手作業で編集する必要はありません。書き換えるのは `sha256` の値だけです（マニフェストのそれ以外の部分は保持されますが、ファイルは再インデントされます）。何かを書き込む前に結果をマニフェストパーサーで読み直し、各ファイルを報告します（`module.pack` エンベロープ: `module`、`directory`、`written`、`intact`、`files`。`files` の各要素は `path`、`sha256`、`pinned`、`mismatch`、`written`）。指定されたディレクトリに対してのみ動作し、インストール環境には一切触れません。`/verify-module:<module directory>` は、書き込まずに同じ内容を報告します（`module.verify`）。固定されたファイルがその固定と一致しない場合は終了コード `7` で終了します（ピンのないファイルは不一致ではありません）。どちらも、解析できないマニフェストまたは存在しないファイルでは終了コード `2` で終了します。

<a id="security-boundary"></a>
## セキュリティ境界

OmsiLaunch は、モジュールが OMSI の内部で行うことを制御しません。マネージドコンポーネントとネイティブコンポーネントは、他の OMSI プラグインと同じ権限で OMSI プロセス内で実行されます。OmsiLaunch が保証するのは次の点です。

1. OmsiLaunch プロセスがモジュールのコードを読み込むことは決してありません。モジュールのコードは、OMSI の内部、OMSI の内部から起動された補助プログラム、またはモジュールのセッションプログラムの中で実行されます。
2. OmsiLaunch がモジュール用に起動するプログラムは、モジュール自身のディレクトリにあるファイルであり、固定に対して検証され、OmsiLaunch が定めたコマンドラインで起動されます。セッションプログラムはジョブオブジェクト内で実行され、セッションとともに終了します。2 つ目のプロセスは許可されず（他のプログラムを起動できません）、システムパラメーター、ディスプレイ設定、デスクトップ、ログオフまたはシャットダウンの変更は拒否されます。継承するのは 2 つのパイプハンドルだけです。
3. モジュールが宣言するものはすべてそのディレクトリ内に収まり、書き込み可能なデータは `.omsilaunch\module-data\<id>` 内に収まります。
4. OmsiLaunch のチャネル（ローカル制御、起動ハンドオフ、テレメトリ、ランタイムコマンド）は現在のユーザーに限定され、セッションにバインドされたままです。いずれも、呼び出し元がプログラムを起動したり、インストール環境の外にあるファイルを読み書きしたり、システム設定を変更したりすることを許しません。
5. OmsiLaunch が昇格することは決してありません。モジュールは、セッションを開始したユーザーの権限で実行されます。
6. モジュールから届くテキスト（通知、イベント、値）はデータとして扱われます。長さは制限され、制御文字は含まれず、実行されることも、開かれることも、リンクとして辿られることもありません。

<a id="capabilities"></a>
## ケイパビリティ

`required_capabilities` と `optional_capabilities` には、実行中のビルドのネイティブケイパビリティ（[ケイパビリティカタログ](capabilities.md#capability-catalog)のネイティブケイパビリティ列。たとえば `time.read` や `d3d.texture`）か、ブリッジ機能のいずれかを指定します。

| ブリッジ機能 | モジュールが得るもの |
| --- | --- |
| `bridge.events` | `Emit`: ランタイムイベント `module.<id>.<name>`。 |
| `bridge.background-threads` | `RunBackground`: 追跡されるバックグラウンドスレッド。 |
| `bridge.auxiliary-processes` | `StartAuxiliaryProcess`: セッションにバインドされた補助プロセス。 |
| `bridge.hooks.iat` | ネイティブコンポーネント: `Omsi.exe` のインポートに対するフック（`iat:<dll>!<function>`）。 |
| `bridge.hooks.d3d9` | ネイティブコンポーネント: 動作中の Direct3D 9 デバイスのメソッドに対するフック（`d3d9:device.<method>`）。ネイティブケイパビリティ `d3d.texture` が必要です。 |
| `bridge.runtime` | プロトコル 3。`IRuntimeModuleContext.Runtime` とネイティブの `run_operation`: マニフェストが `runtime_operations` に挙げるカタログ操作。 |
| `bridge.tick` | プロトコル 3。定期コールバック: `ITickingModule.Tick` とネイティブの `omsilaunch_module_native_v1_tick` エクスポート。 |
| `bridge.input` | プロトコル 3。`IRuntimeModuleContext.Input`: モジュール独自のメニューのための、ゲームのキーボードおよびマウスメッセージのキャプチャ。ネイティブケイパビリティ `ui.input.capture` が必要です。 |

どちらにも該当しない名前は、プランニングの時点で拒否されます（`OL_E_MODULE_CAPABILITY_UNRESOLVED`）。レイアウトが既知でない OMSI 2 ビルドでは、実行ファイルから解決できないネイティブケイパビリティはプロセス内で判定されます。そこでも解決されない場合、モジュールは読み込まれません。

<a id="choosing-modules-for-a-session"></a>
## セッションで使用するモジュールの選択

モジュールがセッションに参加する方法は 4 つあります。どれであるかは `PlannedModule.Activation` が示します。

| アクティベーション | 条件 |
| --- | --- |
| `requested` | セッションが要求したとき: `LaunchSpec.Modules.Enable`、`/module:<id>`、または選択したセッションプロファイルのプリセットでの `enabled: true`。 |
| `settings-file` | [設定ファイル](#the-settings-file)のモジュールのセクションで `enabled=true` が指定されているとき。 |
| `content` | アクティベーションが `content` で、セッションがバインドされたマップを実行するとき。 |
| `default` | アクティベーションが `default` のとき。 |

モジュールは、`LaunchSpec.Modules.Disable`、`/no-module:<id>`、プリセットでの `enabled: false`、または設定ファイルでの `enabled=false` によってセッションから除外されます（プラン診断 `module.disabled`）。明示的な要求は設定ファイルより優先されます。同じモジュールを 1 つの要求で有効にして無効にすることは、`OL_E_MODULE_SETTING_INVALID` になります。

プランニングのルール: 要求されたモジュールを読み込めない場合、プランは実行不可となり、`OL_E_MODULE_*` コードが付きます。モジュールが自動的に参加した場合（設定ファイル、コンテンツ、既定）に読み込めなくても、セッションは停止しません。プランは `OL_W_MODULE_NOT_LOADED` を報告します。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /no-module:example-preload
```

<a id="settings"></a>
## 設定

モジュールは `settings` で自身の設定を宣言します。各エントリは次のとおりです。

| キー | 必須 | 意味 |
| --- | --- | --- |
| `key` | 必須 | `[a-z][a-z0-9_]{0,31}`。`enabled` は不可。 |
| `type` | 必須 | `boolean`、`integer`、`choice`、`text` のいずれか。 |
| `default` | 必須 | 他に何も値を設定しないときの値: 真偽値、整数、文字列、または `auto` が true の場合は `"auto"`。 |
| `auto` | 任意 | `true`: 値 `auto` が受け付けられてモジュールに渡され、モジュールが実行時に判断します。 |
| `min`、`max` | 任意 | 整数の範囲。 |
| `choices` | `choice` の場合 | 受け付ける値（`[a-z0-9][a-z0-9_.-]{0,31}`、`auto` は不可）。 |
| `max_length` | 任意 | テキストの長さの上限（1..4096、既定 256）。制御文字は拒否されます。 |
| `flag` | 任意 | この設定用のコマンドラインフラグ。`[a-z][a-z0-9-]{2,31}` で、`/<flag>:<value>` の形式です。boolean では `/<flag>` だけで `true` を表します。 |
| `description` | 任意 | 256 文字以内。 |

値は正規化されます。boolean は `true`/`false`、`yes`/`no`、`on`/`off`、`1`/`0` を受け付けます。choice は大文字小文字を区別せずに照合されます。integer は範囲に照らして検査されます。

各設定の有効な値は、次の順序で決まります。

1. 要求: `LaunchSpec.Modules.Settings`（`"<module-id>.<key>": "<value>"`）、`/module-setting:<module-id>.<key>=<value>`、モジュール自身のフラグ、または選択したセッションプロファイルのプリセットでの `modules.<id>.settings`。他のプリセットフィールドと同様に、プリセットが設定しているキーをコマンドラインで重ねて設定することはできません（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）。
2. [設定ファイル](#the-settings-file)。
3. マニフェストの既定値。

要求された値が無効な場合、またはモジュールが宣言していないキーが要求された場合、プランは実行不可になります（`OL_E_MODULE_SETTING_INVALID`）。使用できない設定ファイルのエントリは、セッションが読み込まないモジュールに対して要求された設定と同様に、`OL_W_MODULE_SETTING_IGNORED` を伴って無視されます。プランは、すべての有効な値を、その由来（`request`、`settings-file`、`default`）とともに `PlannedModule.Settings` に列挙します。

モジュールは、宣言したすべてのキーを有効な値とともに受け取ります。`IRuntimeModuleContext.Settings`、ネイティブコンポーネントでの `get_value("setting.<key>")`、およびセッションプログラムのコンテキストの `settings` オブジェクトを通じて受け取ります。

<a id="the-settings-file"></a>
### 設定ファイル

`<OMSI root>\.omsilaunch\modules.ini` で、セッションプロファイルと同じ場所にあります。OmsiLaunch は読み取るだけです。

```ini
; one section per module id
[example-preload]
enabled=true
ram_budget_mb=8192
monitor=false
```

セクションはモジュール ID です。`key=value` 行は設定を指定し、`enabled` はすべてのセッションでモジュールのオン/オフを切り替えます。`;` または `#` で始まる行はコメントです。ファイルは最大 256 KiB です。使用できないもの（モジュール ID ではないセクション、セクションの外にある行、未知のキー、無効な値）は `OL_W_MODULE_SETTING_IGNORED` として報告され、無視されます。

<a id="session-programs"></a>
## セッションプログラム

モジュールは `host` コンポーネントを 1 つ宣言できます。これはモジュール自身のセッションプログラムで、モジュール自身のディレクトリにある実行ファイルです。モジュールがプランに含まれる場合、コントローラーは次の処理を行います。

1. プログラムの固定を検証し、モジュールのデータディレクトリを作成します。
2. OMSI より先に、モジュールのディレクトリから、コマンドライン `"<program>" --omsilaunch-session <session id>` で起動します。ジョブに入るまでは一時停止状態です（[セキュリティ境界](#security-boundary)を参照）。
3. コンテキストを標準入力の最初の行として書き込みます。
4. プログラムが `{"type":"ready"}` を書き込むまで、最長 `prepare_ms` の間待機します。OMSI を起動するのはその後です。
5. OMSI が作成された後、`{"type":"omsi-started","pid":<process id>}` を書き込みます。
6. セッションの終了時、OMSI が終了した後、かつファイルが復元される前に、`{"type":"stop"}` を書き込み、最長 `host_stop_ms` 待機してから、プログラムを終了させます。

どちらの方向も UTF-8 の JSON で、1 行に 1 オブジェクトです。コンテキストは次のとおりです。

| フィールド | 意味 |
| --- | --- |
| `type` | `context`。 |
| `schema` | `omsilaunch.module-session/v1`。 |
| `session_id`、`module_id`、`module_version`、`product_version` | 識別情報。 |
| `installation_root`、`module_directory`、`data_directory` | フルパス。 |
| `channel` | プログラムが OMSI 内のモジュールのコンポーネントのために提供できる名前付きパイプ: `\\.\pipe\OmsiLaunch.Module.<session id without dashes>.<module id>`。プログラムがこれを作成し、現在のユーザーに限定して、クライアントが OMSI のプロセスであることを確認します。コンポーネントは `get_value("session_channel")` または `IRuntimeModuleContext.SessionChannelName` で読み取ります。 |
| `settings` | モジュールの有効な設定。 |
| `world` | セッションの `mode`、`map`、`situation`、`entrypoint_index`、`entrypoint`。 |
| `session_files` | セッショントランザクションが所有する、インストール環境からの相対パスのファイル。プログラムはこれらを開いたままにしたり、マップしたままにしたりしてはいけません。セッションの終了時に復元されるためです。 |

プログラムが `ready` 以外に書き込める内容は次のとおりです。

| 行 | 効果 |
| --- | --- |
| `{"type":"event","name":"<name>","data":{...}}` | ランタイムイベント `module.<id>.<name>`。 |
| `{"type":"notice","data":{"title":"...","text":"..."}}` | ランタイムイベント `module.<id>.notice`（[通知](#notices)を参照）。 |
| `{"type":"error","data":{"code":"OL_E_...","detail":"..."}}` | そのカタログ登録済みコードを持つセッション診断（それ以外は `OL_E_MODULE_OPERATION_FAILED`）。 |

それ以外の行、64 KiB を超える行、および 10,000 行目以降のすべては無視されます。コントローラーは `module.<id>.host.started`、`host.ready`（`prepare_ms` 付き）、`host.failed`、`host.exited`、`host.killed` を記録します。

`prepare_ms` 以内に準備完了にならないプログラム、準備完了の前に終了するプログラム、または固定が成立しないプログラムは、そのモジュールの準備を失敗させます。`fail-session` ではセッションが開始されません（`OL_E_MODULE_SESSION_FAILED`）。`disable-module` ではプログラムが終了され、モジュールは OMSI から除外されます。`degrade` ではモジュールが残ります（プログラムも、可能であれば実行を続けます）。セッションの `StartupTimeoutSeconds` は OMSI が起動された時点から数えるため、準備にかかる時間はこれを消費しません。

<a id="conflicts-with-omsi-plugins"></a>
## OMSI プラグインとの競合

`conflicts.plugins` には、モジュールと同時に実行してはならない OMSI プラグイン DLL を指定します。たとえば、同じフックをインストールする古いプラグインです。プランニング時に、OmsiLaunch はすべての `plugins\*.opl` の `[dll]` セクションを読み取ります。競合する DLL を読み込むものがあれば、モジュールは `OL_E_MODULE_CONFLICT` で拒否され、理由にはその `.opl` が示されます。OMSI の内部では、競合する DLL がそれでも読み込まれていた場合、そのモジュールは読み込まれません。OmsiLaunch が他のプラグインのファイルを変更することは決してありません。それを無効にするかどうかは利用者が決めます。

<a id="notices"></a>
## 通知

`notice` という名前のモジュールイベント（`Emit("notice", ...)`、ネイティブコンポーネントの `emit`、またはセッションプログラムの通知行によるもの）は、OmsiLaunch のトレイにバルーンとして表示され、そのタイトルにはモジュール名が示されます。保持されるのは `title`（63 文字以内）と `text`（255 文字以内）だけで、制御文字は取り除かれ、その内容が開かれたり実行されたりすることはありません。OMSI が起動する前（トレイがまだ表示されていないとき）に利用者へ知らせる必要があるセッションプログラムは、自前のウィンドウを使用します。

<a id="data-directory"></a>
## データディレクトリ

`<OMSI root>\.omsilaunch\module-data\<id>\` は、このインストール環境におけるモジュールの書き込み可能なディレクトリです。`IRuntimeModuleContext.DataDirectory`、`get_value("data_directory")`、およびセッションプログラムの `data_directory` で参照できます。最初の使用時に作成されます。セッショントランザクションの対象ではありません。モジュールがそこに書き込んだものは、セッションの後も残ります。`/clear-module-data:<id>` はそれを削除します（それ以外は削除しません）。セッションがインストール環境を所有している間は拒否されます。また、リンク経由で到達したディレクトリはリンクとして削除され、辿られることはありません。

<a id="lifecycle"></a>
## ライフサイクル

| ステップ | 場所 | 処理内容 |
| --- | --- | --- |
| 1. 検出 | コントローラー | `plugins\OmsiLaunch.Modules\*` を読み取ります。マニフェストと整合性が対象です（`ListRuntimeModulesAsync`、`/list:RuntimeModules`）。 |
| 2. 適格性判定 | コントローラー、プランニング時 | [セッションに参加する](#choosing-modules-for-a-session)モジュールを評価します。使用可能か、ブリッジプロトコル、ケイパビリティ、コンテンツバインディング、プラグインの競合、設定です。結果は `SessionPlan.PlannedModules` になります。 |
| 3. 準備 | コントローラー、開始時 | モジュールの[セッションプログラム](#session-programs)を起動し、待機します。その後、適格なモジュールが起動ハンドオフ（バージョン 6）でプラグインに渡されます。渡されるのは、モジュールのディレクトリ、評価済みの各 `module.json` の SHA-256、および有効な設定です。 |
| 4. アーム | プラグイン、ワールドの開始前 | 各モジュールを再検証して読み込みます（マネージドコンポーネントは専用のロードコンテキストで読み込みます）。ネイティブコンポーネントがあれば先に読み込んで開始し、その後に `Arm(context)` が呼び出されます。 |
| 5. ゲームプレイ準備完了 | プラグイン | `gameplay.entered` の後、読み込まれたモジュールが `bridge.hooks.d3d9` を宣言していると、ブリッジは Direct3D デバイスの監視を開始します（`d3d.ready`）。その後、ネイティブコンポーネントの `gameplay_ready` と `GameplayReady()` を呼び出します。NEW_MAP セッションでは、バインドされたマップが、開始時に読み込まれたマップと再度照合されます。 |
| 6. ランタイム | プラグイン | 操作 `module.<id>.<operation>` が `Execute` に到達します。`tick_ms` を持つモジュールにはティックが与えられ（[ティック](#tick)を参照）、`runtime_operations` を持つモジュールはそれらをプロセス内で実行します（[プロセス内の操作](#operations-in-process)を参照）。 |
| 7. 停止 | プラグイン、続いてコントローラー | セッションが終了したとき（またはモジュールが失敗したとき）、`Stop()` が呼び出されます。OMSI が終了した後、セッションプログラムが停止されます。 |
| 8. クリーンアップ | プラグイン | モジュールが追跡していたものすべてを逆順に解放し、なくなったことを確認します。ネイティブコンポーネントは最後に停止され、そのフックが取り除かれます。DLL は、OMSI が終了するまで、何もしない状態で読み込まれたままになります（スレッドがデトゥアーを経由してまだ戻っている可能性があるためです）。 |

プロセス内のコールバックはすべて、製品自身のランタイム操作と同様に、OMSI の UI スレッドで実行されます。ブリッジは各コールバックを予算と照らして計測します。遅れて戻ったコールバックは、そのフェーズを失敗させます（`OL_E_MODULE_PHASE_TIMEOUT`）。ブリッジは、戻ってこないコールバックを中断できません。そのようなモジュールは OMSI を停止させ、セッションは、他のセッションと同様に、起動タイムアウトで、または `session stop` によって終了します。

<a id="failures"></a>
## 失敗

失敗とは、いずれかのフェーズまたは操作での例外や超過、プロセス内で解決されなかったケイパビリティ、プロセス内で成立しなかったコンテンツバインディング、OMSI で読み込まれた競合プラグイン、または準備完了にならなかったセッションプログラムを指します。その後の動作は、モジュールの `failure_policy` によって決まります。どの場合でも、モジュールのリソースは解放されます。例外をスローする、0 以外を返す、または間隔を超過したティックは、操作の場合と同様に失敗です。

| ポリシー | 準備、読み込み、アーム、ゲームプレイ準備完了での失敗 | 操作での失敗 | 停止での失敗 |
| --- | --- | --- | --- |
| `fail-session` | モジュールは解放され、`module.session-failure` が発生し、セッションは `OL_E_MODULE_SESSION_FAILED` で失敗し、OMSI は停止され、ファイルは復元されます。ワールドの開始前の失敗は、開始を妨げます。 | 同じ。 | 報告されます（`module.failed`）。セッションは最後まで継続します。 |
| `disable-module` | モジュールは解放されて無効化され、セッションは継続します。 | 同じ。 | 報告され、モジュールは解放されます。 |
| `degrade` | `disable-module` と同じですが、セッションプログラムの失敗ではモジュールがセッションに残ります。 | 失敗した呼び出しがそのコードを返し、モジュールはアクティブなままで、`module.degraded` がコードごとに 1 回報告されます。 | 報告され、モジュールは解放されます。 |

無効化された、または失敗したモジュールは、その操作に `OL_E_MODULE_DISABLED` で応答します。モジュールの失敗は、`OL_E_MODULE_*` コードを持つセッション診断（`SessionStatus.Diagnostics`）として、またランタイムイベントとして現れます。

<a id="cleanup"></a>
## クリーンアップ

モジュールがそのコンテキストを通じて取得したものは追跡されます。`Track` で登録したリソース、`RunBackground` で開始したスレッド、`StartAuxiliaryProcess` で開始したプロセスです。停止時または失敗時に、ブリッジは `Stopping` をキャンセルし、それらを逆順に解放します。補助プロセスは、クローズ時に終了するジョブオブジェクトにも入れられるため、OMSI が強制終了されても OMSI とともに終了します。セッションプログラムのジョブは、OmsiLaunch 自体が強制終了されても、OmsiLaunch のセッションとともにそれを終了させます。解放後もまだ生きているリソース（終了しなかったプロセス、終了しなかったスレッド）は、`OL_E_MODULE_CLEANUP_INCOMPLETE` として報告されます。モジュールがコンテキストを使わずに作成したスレッドやプロセスは追跡されません。

クラッシュ後に、モジュールについてリカバリするものはありません。モジュールはインストール環境のファイルを保持しておらず、そのメモリ、スレッド、ジョブにバインドされたプロセスは、OMSI プロセスまたは OmsiLaunch プロセスとともに終了するためです。

<a id="native-components-and-hooks"></a>
## ネイティブコンポーネントとフック

ネイティブコンポーネントは、`src/OmsiLaunch.Native.x86/include/omsilaunch_module_native_v1.h` の C インターフェイスを実装する 32 ビット DLL です。

| エクスポート | 呼び出されるタイミング |
| --- | --- |
| `int omsilaunch_module_native_v1_start(const omsilaunch_bridge_v1*)` | アーム時、マネージドの `Arm` の前。0 以外を返すとフェーズが失敗します。 |
| `int omsilaunch_module_native_v1_gameplay_ready(const omsilaunch_bridge_v1*)` | 任意。ゲームプレイに入った後。 |
| `void omsilaunch_module_native_v1_stop(void)` | 停止時または失敗時、フックが取り除かれる前。その後、ブリッジテーブルはすべての呼び出しを拒否し、DLL は OMSI が終了するまで、何もしない状態で読み込まれたままになります。 |

ブリッジテーブルは、コンポーネントに `request_hook`、`emit`（文字列の JSON オブジェクトを伴うランタイムイベント `module.<id>.<name>`）、およびプロトコル 2 以降では `get_value` を提供します。コンポーネントは、`get_value` を使用する前にテーブルの `size` を読み取ります（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_GET_VALUE`）。`get_value(context, name, buffer, capacity, length)` は、値を終端のゼロ付きの UTF-8 としてコピーし、`*length` に終端を除いて必要なバイト数を設定します。戻り値は `0`、`1`（引数）、`2`（未知の名前）、`11`（停止後）、`12`（バッファーが小さすぎる）のいずれかです。プロトコル 3 以降、テーブルには `run_operation`（[プロセス内の操作](#operations-in-process)を参照）と `d3d_is_game_device(context, device)` も含まれます。後者は、Direct3D フックが受け取ったデバイスポインターがゲーム自身のデバイスか（`1`）、別のデバイスか（`0`。車両選択フォームは同じメソッドテーブルを持つ 2 つ目のデバイスを作成するため、`Present` のデトゥアーは両方に対して呼び出され、ゲーム自身のデバイスに対してのみ描画すべきです）を返します。まだデバイスが観測されていない場合は `10`、`bridge.hooks.d3d9` が宣言されていない場合または停止後は `11` です。ポインターは比較されるだけで使用されることはなく、ブリッジがポインターを渡すことも決してありません。Direct3D リソースをデフォルトプールに保持するモジュールは、`d3d9:device.Reset` を自分でフックします（元の関数を呼び出す前に行うことが「デバイスロスト」、戻って成功した後に行うことが「デバイス復元」です）。コンポーネントは、プロトコル 3 のメンバーを使用する前にテーブルの `size` を確認します（`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_RUN_OPERATION`、`OMSILAUNCH_BRIDGE_V1_SIZE_WITH_D3D_IS_GAME_DEVICE`）。2 つの任意のエクスポートでコンポーネントは完成します。`int omsilaunch_module_native_v1_tick(const omsilaunch_bridge_v1*, uint32_t elapsed_ms)`（[ティック](#tick)を参照）と、`int omsilaunch_module_native_v1_execute(const omsilaunch_bridge_v1*, const char* operation, const char* arguments_json, char* buffer, uint32_t capacity, uint32_t* length)` です。後者は、マネージドコンポーネントが `IRuntimeModuleContext.NativeComponent.Execute` を通じてネイティブコンポーネントを呼び出す方法です（JSON を入力して JSON を出力し、`run_operation` と同じバッファー規約です）。`get_value` が返す名前は次のとおりです。

| 名前 | 値 |
| --- | --- |
| `setting.<key>` | 宣言された設定の有効な値。 |
| `data_directory` | モジュールの[データディレクトリ](#data-directory)。 |
| `module_directory` | モジュール自身のディレクトリ。 |
| `session_id` | セッション ID。 |
| `module_version` | マニフェストのバージョン。 |
| `product_version` | OmsiLaunch の製品バージョン。 |
| `session_channel` | [セッションプログラム](#session-programs)のパイプ。セッションプログラムを持たないモジュールでは不明です。 |
| `omsi_version` | ゲームが自身について報告する OMSI の製品バージョン（プロトコル 3。[OMSI バージョン](#the-omsi-version)を参照）。 |

フックは、シンボリックなターゲットでのみ、`start` または `gameplay_ready` の間に、マニフェストがそのケイパビリティを宣言しているターゲットに対して要求されます。

| ターゲット | フック対象 | ケイパビリティ |
| --- | --- | --- |
| `iat:<dll>!<function>` | `Omsi.exe` のインポート。たとえば `iat:kernel32.dll!ReadFile`。 | `bridge.hooks.iat` |
| `d3d9:device.<method>` | 動作中の Direct3D 9 デバイスの `Reset`、`Present`、`BeginScene`、`EndScene`（デバイスが存在する `gameplay_ready` で要求します）。 | `bridge.hooks.d3d9` |

`request_hook` は、チェーンを継続するための関数を `*original` に返します（デトゥアーはこれを呼び出す必要があります）。結果は次のとおりです。`0` 成功、`1` 引数、`2` 未知のターゲット、`3` その箇所が期待された内容を保持していない、`4` その箇所がブリッジ外の何かによってフックされている、`5` メモリ保護、`6` レジストリが満杯、`7` 順序、`8` メモリ、`9` パッチ対象のバイトをスレッドが実行していた、`10` Direct3D デバイスがまだない、`11` 現時点では許可されていない、またはケイパビリティが宣言されていない。

ブリッジがインストールするすべてのフックは、製品用であってもモジュール用であっても、所有者（モジュールの場合は `module:<id>`）とともに 1 つのレジストリに記録されます。

- フックは、その箇所が保持しているべき値（インポートされた関数、`d3d9.dll` という名前のモジュール・システムランタイム・ラッパーのいずれかの関数）の上にのみ、またはその箇所のレジストリ自身の最上位のフックの上に積み重ねる形でのみインストールされます。すでに他者によって変更された箇所は拒否され（`4`）、上書きされることは決してありません。
- 同じ箇所のフックは後入れ先出しで取り除かれます。上に別のフックが載っているフックは、そのフックが取り除かれるまで残ります。
- 製品自身の Direct3D `Reset` フックはレジストリに含まれているため、モジュールのデバイスフックはその上に積み重ねられます。
- クリーンアップ時に、ブリッジは `stop` が戻った後でモジュールのすべてのフックを取り除きます。取り除けないフックがある場合（他者がその箇所を変更した場合）、クリーンアップは `OL_E_MODULE_CLEANUP_INCOMPLETE` を報告します。ネイティブコンポーネントの DLL は OMSI の実行中にアンロードされることがないため、まだ戻りの途中にあるデトゥアーは有効なままです。

エンジンは、トランポリンを使った関数プロローグへのフックにも対応しています（バイトの変更中は他のスレッドが一時停止され、事前に期待されるバイト列が厳密に検査されます）。ただし、プロローグのターゲットはまだモジュールには提供されていません。そのようなターゲットは、検証済みのバイト列を持つビルドのターゲットカタログから得られます。

<a id="operations"></a>
## 操作

モジュールの操作は `module.<id>.<operation>` です。これは、プランがそのモジュールを読み込んだセッション（`PlannedModule.Operations`）にのみ存在します。それ以外の場合、`ExecuteRuntimeAsync` は `OL_E_MODULE_NOT_LOADED` または `OL_E_MODULE_OPERATION_UNKNOWN` で応答します。必須引数は検査され、宣言されていない引数は拒否されます（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。モジュールの型付き失敗は、それがカタログ登録済みの公開コードであればそのコードを維持します。それ以外のコードは `OL_E_MODULE_OPERATION_FAILED` となり、モジュールのコードは `Values["module_code"]` に入ります。内部の命名規則に従う結果キーは、すべてのランタイム操作と同様に取り除かれます。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /module:null-test
OmsiLaunch.exe /runtime:module.null-test.ping /runtime-arg:text=hello
```

<a id="auxiliary-programs"></a>
## 補助プログラム

モジュールは、`StartAuxiliaryProcess` で、自身の `auxiliary` コンポーネント（自身のディレクトリにある固定された `.exe`）の 1 つを開始します。プロトコル 3 以降、ブリッジはそのプログラムを**中断状態**で作成し、セッションのジョブオブジェクトに入れてから再開するため、プログラムがすぐに開始する子プロセスがジョブの外で実行されることはありません。ジョブは、モジュールが停止したとき、または OMSI が終了したときに、プログラムとそれが開始したすべてのものを終了させ（クローズ時に強制終了）、マニフェストの `priority`（`below-normal`、`idle`）と `memory_limit_mb` を、プログラムの最初の命令から適用します。[セッションプログラム](#session-programs)とは異なり、補助プログラムのジョブは子プロセスを許可します（Web ビューを組み込むプログラムには必要です）。そのすべてがジョブに含まれます。開始する前に、プログラムは再度検証されます。固定がある場合はその固定が、そして常にプランのスナップショットが検証されます。

<a id="operations-in-process"></a>
## プロセス内の操作

`IRuntimeModuleContext.Runtime.Execute(operation, arguments)`（マネージド）と `run_operation`（ネイティブ）は、[公開ケイパビリティカタログ](capabilities.md#public-runtime-operations)の操作を、OMSI の UI スレッドでプロセス内で実行します。同じレジストリ、同じ型付き `OL_E_*` 結果、同じ引数チェック、同じ変更ポリシーをランタイム制御チャネルと共有しますが、チャネルは使用しません。許可されるのは、マニフェストが `runtime_operations` に挙げた操作だけです。それ以外の操作は、OMSI に到達する前に `OL_E_MODULE_OPERATION_NOT_DECLARED` で応答され、`internal.*` と `module.*` の操作を挙げることはできません。ネイティブケイパビリティがビルドで解決されない操作は、他の場所と同様に `OL_E_CAPABILITY_UNRESOLVED` で応答します。

このリストは透明性とプランニングのための情報であり、セキュリティ境界ではありません。モジュールはすでにユーザーの権限で OMSI の内部で動作しています。プランにはこのリストが表示され（`PlannedModule.RuntimeOperations`）、OMSI の状態を変更する操作には、そこで変更ポリシーが併記されます。たとえば `camera.set (omsi-state)` です。モジュールは `Arm` から操作を実行できます（ワールドがまだ存在しないことがあります。その場合、操作は独自の型付きエラーで応答します）。結果には上限があり（1 MiB）、それより大きい結果は `OL_E_RUNTIME_RESPONSE_TOO_LARGE` になります。モジュールが停止した後は、すべての呼び出しが `OL_E_MODULE_DISABLED` で応答します。

ネイティブの `run_operation(context, operation, arguments_json, buffer, capacity, length)` は、カタログ ID と文字列の JSON オブジェクト（または null）を受け取り、`{"succeeded":bool,"error_code":null|"OL_E_...","values":{...}}` を書き込みます。応答した場合は `0` を返し（操作自身の失敗は JSON の内側にあります）、引数の問題では `1`、停止後は `11`、バッファーが小さすぎる場合は `12` を返します（`*length` は必要なサイズです）。

<a id="tick"></a>
## ティック

`tick_ms` と `bridge.tick` ケイパビリティを宣言したモジュールは、プラグインの 50 ms の UI タイマーから、約 `tick_ms` ミリ秒ごとに、OMSI の UI スレッドで呼び出されます。マネージドコンポーネントでは `ITickingModule.Tick(TimeSpan elapsed)`、ネイティブでは任意のエクスポート `omsilaunch_module_native_v1_tick(bridge, elapsed_ms)` です。間隔は 50 の倍数で、50 から 1000 の間です。ティックはフレームごとではありません。描画するモジュールは Direct3D の `Present` メソッドをフックし、ティックはデータの読み取りに使います。ティックは `gameplay.entered` の後に始まり、モジュールとともに停止します。例外をスローする、0 以外を返す（ネイティブ）、または間隔より長くかかったティックは、そのモジュールの失敗です。`degrade` ではコードごとに 1 回報告され（`module.degraded`）、モジュールは実行を続けます（取り逃したティックはスキップされ、キューには積まれません）。`disable-module` と `fail-session` では、そのポリシーが適用されます。OMSI のネストしたモーダルループ中も UI タイマーは発火し続けるため、ティックが自分自身に再入することはありません。

<a id="community-content"></a>
## コミュニティコンテンツ

`content_inputs` は、モジュールが読み取るコンテンツのうち、モジュールのビルド時に作成者が知り得ないものを宣言します。コミュニティが、マップ向けに、モジュールの後で作るためです。固定する SHA-256 はなく、ブリッジはそれを検索し、読み取りが安全であることを確認するだけです。現時点で唯一の種類は `realmap-package` です。マップの `openomsi-realmap` フォルダー `<OMSI root>\maps\<folder>\openomsi-realmap` で、openOMSI のマップパッケージツールによって書き込まれます。

`IRuntimeModuleContext.Content.LocateRealmapPackage("maps\\<folder>\\global.cfg")` は、マップにパッケージがあるかどうかを答え（ない場合はエラーではありません）、ある場合はそのディレクトリ、ファイル数、合計サイズ、`Fingerprint` を返します。フィンガープリントは、すべてのファイルの相対パス、サイズ、書き込み時刻に対する変更検出子です。キャッシュキーとして使用してください。整合性チェックや信頼の判断に使うものでは決してありません。パッケージはインストール環境の内側にある必要があり、その一部にもリパースポイントがあってはならず、上限があります（50,000 ファイル、4 GiB）。そうでない場合の応答は `OL_E_MODULE_CONTENT_INPUT_INVALID` です。その種類を宣言しなかったモジュールには `OL_E_MODULE_CONTENT_INPUT_UNDECLARED` が返ります。パッケージはサードパーティが作ったデータです。マネージドコードで読み取り、すべての値を信頼できないものとして扱い、リソース単位で縮退してください。`required: true` の入力は、セッションのマップにパッケージがない場合に、プランにそのモジュールを拒否させます（`OL_E_MODULE_CONTENT_BINDING`）。

<a id="input-capture"></a>
## 入力キャプチャ

OMSI のポーズメニューを独自のものに置き換えるモジュールには、ゲームのキーボードとマウスが必要です。OMSI はシミュレーションキーを DirectInput で、マウスをメインフォームのビューウィンドウへのウィンドウメッセージで読み取ります。OMSI 自身のポーズ（ネイティブのポーズバイト）がオンの間は、シミュレーション、レンダリング、3D クリック、DirectInput コマンドは OMSI 自身によって保持されます。`IRuntimeModuleContext.Input`（ケイパビリティ `bridge.input`、ネイティブケイパビリティ `ui.input.capture`）が残りを補います。`Capture(true)` は UI スレッドにスレッドローカルな `WH_GETMESSAGE` フックを 1 つインストールし、ゲームのウィンドウツリー（メインフォーム、その子、非表示のネイティブポーズフォーム）に属するキーボードとマウスのメッセージは、単純な `ModuleInputEvent` 値（種類、仮想キー、ビューウィンドウのクライアント座標、ボタン、ホイールのデルタ、修飾キー）として記録され、キューから取り除かれます。そのため、OMSI も VCL もそれらに反応しません。`Drain()` はイベントを返します（保持されるのは最大 256 件です）。何もサブクラス化されず、user32 のキー状態関数はフックされず、何も送信されません。

同時にキャプチャできるモジュールは 1 つです。キャプチャは、`Stop` が何をしたかにかかわらず、モジュールが停止または失敗したときに解放されます。ネイティブのモーダルダイアログ（保存、オプション、新規ゲーム）には実際の入力が必要です。ダイアログを求める前にキャプチャを解放してください。フックの仕組みはスタンドインウィンドウを使ってオフラインで検証されています（`ui-input.hook-self-check`）。実際のゲームウィンドウに対する動作は、まだ観測されていません。

<a id="the-omsi-version"></a>
## OMSI バージョン

`IRuntimeModuleContext.OmsiVersion` と `get_value("omsi_version")` は、ゲームが自身について報告する OMSI の製品バージョンを返します。たとえば `2.3.004` です。これは、OMSI が `logfile.txt` の 7 行目の `Version:` の後に書き込むテキストで、実行中のイメージから読み取ります。実行ファイルの PE ファイルバージョンでは**決して**ありません（リファレンスビルドの `FileVersion` は `2.2.032` です）。イメージにそのようなテキストが 1 つだけ存在しない場合は、コード ID が既知であればビルドプロファイルのバージョンが使用され、そうでなければ `unknown` になります。実行ファイルのハッシュ、そのコードサイズ、アドレス、プロファイル名は公開されません。バージョンは、テキスト表示や動作の選択に使用してください。何が動作するかについての唯一の正とする情報源は、引き続きケイパビリティ（`IsCapabilityAvailable`）です。

<a id="writing-a-module-contracts"></a>
## モジュールの作成（コントラクト）

マネージドコンポーネントは `OmsiLaunch.Runtime.Modules` と `OmsiLaunch.Api` を参照し、`IRuntimeModule` を実装します。ブリッジはこれら 2 つの参照を、（プラグインがどのロードコンテキストで動作していても）自身が読み込んだアセンブリにバインドするため、モジュールに同梱されたコピーは無視されます。

| メンバー | 呼び出されるタイミング |
| --- | --- |
| `void Arm(IRuntimeModuleContext context)` | ステップ 4。コンテキストを通じてリソースを取得します。 |
| `void GameplayReady()` | ステップ 5。 |
| `IReadOnlyDictionary<string, string> Execute(string operation, IReadOnlyDictionary<string, string> arguments)` | ステップ 6。宣言された各操作に対して呼び出されます（`module.<id>.` プレフィックスなし）。型付き失敗には `ModuleOperationException(code, message)` をスローします。 |
| `void Stop()` | ステップ 7。 |

`IRuntimeModuleContext` は、`SessionId`、`ModuleId`、`ModuleDirectory`、`Settings`、`DataDirectory`、`ProductVersion`、`OmsiVersion`、`SessionChannelName`、`Stopping`（キャンセルトークン）、`IsCapabilityAvailable`、`Emit`、`Track`、`RunBackground`、`StartAuxiliaryProcess` を提供し、プロトコル 3 以降は `Runtime`（[プロセス内の操作](#operations-in-process)を参照）、`Content`（[コミュニティコンテンツ](#community-content)を参照）、`Input`（[入力キャプチャ](#input-capture)を参照）、`NativeComponent`（読み込まれたネイティブコンポーネント、または null）も提供します。ティックを必要とするマネージドコンポーネントは、`ITickingModule` も実装します。コンテキストを通じて、アドレス、実行ファイルのハッシュ、そのコードレイアウトが公開されることはありません。コンポーネントは 32 ビットのプラグインで読み込める必要があります（AnyCPU または x86、.NET 6）。`OmsiLaunch.Runtime.Modules` や `OmsiLaunch.Api` のコピーが隣にあっても無視されます。整合性のチェックがカバーされるよう、モジュールのプログラムが読み込むすべてのファイル（.NET プログラムの場合は、`.exe` の隣にある `.dll`）を固定してください。

<a id="known-limitations"></a>
## 既知の制限事項

- ネイティブコンポーネントは、`Omsi.exe` のインポートと Direct3D デバイスのメソッドをフックできます。OMSI の関数プロローグや呼び出し箇所へのフックは、まだモジュールには提供されていません。
- ポーズメニューの引き継ぎ（`ui.pause.*`）と入力キャプチャは、静的に設計されオフラインで検証されていますが、実際の OMSI セッションではまだ観測されていません。また `ui.pause-menu.replace` は、既知のコード ID でのみ解決されます（一部のフィールドにはビルドに依存しない導出方法がありません）。
- プロセス内のコンテンツバインディングのチェックは、開始時に要求されたマップ（NEW_MAP）を使用します。保存済みシチュエーションでは、バインディングはプランニング時にシチュエーションのマップに対して検査されます。
- デバイスフックは vtable フックです。他のコンポーネントがデバイスの元のメソッドを書き戻す場合（回帰テスト用インストール環境で、ゲームプレイ開始の直後に確認されています）、デバイスフックは呼び出されなくなり、取り除くこともできません。その場合、クリーンアップは `OL_E_MODULE_CLEANUP_INCOMPLETE` を報告し、コンポーネントは読み込まれたままになります。レジストリ外の何かによってすでにフックされているデバイスメソッドは拒否され（4）、拒否イベントにはそのメソッドを所有するモジュールが示されます。
- モジュールは OMSI の通常終了時に停止します。セッションの停止は OMSI を強制終了させるため、セッション内でのモジュールのクリーンアップは、失敗ポリシーを通じてのみ行われます。
- 通知は、OMSI の起動後にトレイが存在してから表示されます。それより前に伝える必要があることについては、セッションプログラムが自前のウィンドウを表示します。
