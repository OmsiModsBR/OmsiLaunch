# セッションプロファイル

<!-- l10n: source=reference/session-profiles.md source-sha256=5bc9beef6e11a8c70d3ea3056678096ccc42321393f0d09fd40217e53c271e4e -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/session-profiles.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

セッションプロファイルは宣言的な YAML パッケージです。コンテンツ作成者がマップやアドオンと一緒に配布し、エンドユーザーが 1 つのコマンド（`OmsiLaunch.exe /predefined-profile:<id> /predefined-profile-index:<1..99> /new`）で再現可能な OmsiLaunch セッションを開始できるようにします。このページは、`src/OmsiLaunch.Core/SessionProfiles.cs` の `SessionProfileCompiler` が実装する `omsilaunch.session-profile/v1` 形式、CLI が適用する優先順位のルール（`tools/OmsiLaunch.Cli/Program.cs` の `CliInput.BuildSpecAsync` と `RejectProfileConflicts`）、およびプロファイルが書き込める設定カタログ（`ConfigurationCatalog`）についての規範的なリファレンスです。プロファイルでできることはすべて、CLI フラグと [LaunchSpec](launchspec.md) でも行えます。プロファイルはそれらの選択をパッケージにまとめるだけです。

安定性: 解析、検証、競合検出、および `settings` / `presentation` / `internet-textures` / `behavior` ブロックは `STABLE_BETA` です（オフラインテスト `session-profiles.strict-compiler`。オーバーレイと復元の経路は RV-005 と RV-006 でランタイム検証済みです。[ランタイム検証の状況](../status/runtime-validation-status.md)を参照してください）。`new.date`、`new.time`、`new.year`、`new.weather` の各キーは、このビルドでは `UNAVAILABLE` です（[`new` ブロック](#new)を参照）。

<a id="package-location-and-naming"></a>
## パッケージの場所と命名

| 項目 | ルール |
| --- | --- |
| パッケージディレクトリ | `<installation root>\.omsilaunch\session-profiles\<id>\` |
| プロファイルファイル | `<package>\profile.yaml`（名前は完全一致、ファイルは 1 つ） |
| アセット | パッケージディレクトリ内の任意のファイルまたはディレクトリで、`presentation.splash.assets` と `internet-textures.profile` から相対パスで参照されます |
| `id` | 単純なディレクトリ名である必要があります。空または空白のみであってはならず、`\`、`/`、`:` を含んではならず、`..` という並びを含んではなりません。違反は `OL_E_SESSION_PROFILE_PATH_ESCAPE` です。`profile.yaml` 内で宣言された `id` の値は、ディレクトリ名とバイト単位で一致する必要があります（大文字と小文字を区別します）。一致しない場合は `OL_E_SESSION_PROFILE_INVALID` です。 |
| 選択 | `/predefined-profile:<id>` を `/predefined-profile-index:<n>` と組み合わせて指定します。インデックスは必須です。`/predefined-profile-index` なしの `/predefined-profile` は `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` で失敗します。 |
| パッケージがない場合 | `OL_E_SESSION_PROFILE_NOT_FOUND` |
| リリースのレイアウト | リリースパッケージには `.omsilaunch\examples\session-profiles\rmg-leste\` に例が同梱されています（[パッケージング](packaging.md)を参照）。例はプロファイルではありません。選択できるようにするには、パッケージを `.omsilaunch\session-profiles\<id>\` にコピーしてください。 |

プロファイルのインストールと削除は、ユーザーまたはコンテンツ作成者が行います。OmsiLaunch がパッケージに書き込んだり、コピーしたり、削除したりすることはありません。パッケージディレクトリはどのトランザクションにも含まれません。

<a id="parsing-rules"></a>
## 解析ルール

| ルール | 動作 | エラー |
| --- | --- | --- |
| サイズ上限 | `profile.yaml` は 256 KiB（262,144 バイト）を超えてはなりません | `OL_E_SESSION_PROFILE_INVALID` |
| ドキュメントの形 | ルートノードがマッピングである YAML ドキュメントがちょうど 1 つ | `OL_E_SESSION_PROFILE_INVALID` |
| スキーマ | `schema` は正確に `omsilaunch.session-profile/v1` である必要があります（大文字と小文字を区別します） | `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` |
| アンカーとエイリアス | ドキュメント内のどこかで YAML アンカー（`&name`）を持つノードは、検証の前に拒否されます。したがってエイリアス（`*name`）が現れることはありません | `OL_E_SESSION_PROFILE_INVALID`（"YAML anchors are not supported."） |
| 未知のキー | すべてのマッピングは閉じています。以下の表でそのコンテキストに対して記載されていないキーは拒否されます（"Unknown property in `<context>`: `<key>`"）。キーは大文字と小文字を区別して照合されます（`Schema:` は未知のキーです）。唯一の開いたマッピングは `settings` で、そのキーは代わりに設定カタログに照らして検証されます。 | `OL_E_SESSION_PROFILE_INVALID` |
| スカラー | すべての末端の値はスカラーである必要があります。スカラーが期待される場所のシーケンスやマッピングは拒否されます（"`<field>` must be a scalar."） | `OL_E_SESSION_PROFILE_INVALID` |
| 数値 | 整数はインバリアントカルチャで解析されます（`1`、`30`）。`settings` 内の小数は区切り文字に `.` を使います | `OL_E_SESSION_PROFILE_INVALID` |
| 日付と時刻 | `new.date.value` は `DateOnly.Parse`、`new.time.value` は `TimeOnly.Parse` で、いずれもインバリアントカルチャで解析されます。ISO 形式 `yyyy-MM-dd` と `HH:mm[:ss]` を使ってください | `OL_E_SESSION_PROFILE_INVALID` |
| YAML の構文エラー | パーサーのメッセージとともに報告されます | `OL_E_SESSION_PROFILE_INVALID`（"Invalid YAML: ..."） |
| 実行可能なコンテンツ | YAML は `YamlDotNet` で表現ツリーとしてのみ解析されます。タグ、カスタム型、コード実行はサポートされていません |

プレーン（引用符なし）スカラー内のバックスラッシュは文字どおりの文字です。Windows のパスはバックスラッシュ 1 つで書いてください（`maps\Grundorf\global.cfg`）。プレーンスカラー内で二重にしたバックスラッシュは、値の中でも二重のまま残ります。[同梱の例](#the-packaged-example)を参照してください。

<a id="key-reference"></a>
## キーリファレンス

コンテキストはコンパイラーでの名前をそのまま使っています。ここに記載されたキーはすべて受け付けられ、それ以外は受け付けられません。

<a id="profile-root-mapping"></a>
### `profile`（ルートマッピング）

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `schema` | string | はい | リテラル `omsilaunch.session-profile/v1`。 |
| `id` | string | はい | パッケージ識別子。ディレクトリ名と一致する必要があります。 |
| `name` | string | はい | 表示名。`SessionProfileMetadata.Name` で報告されます。 |
| `author` | string | はい | 作成者。`SessionProfileMetadata.Author` で報告されます。 |
| `version` | string | はい | パッケージのバージョン文字列（自由形式。引用符で囲んでください: `"1.0"`）。`SessionProfileMetadata.Version` で報告されます。 |
| `compatibility` | mapping | いいえ | `compatibility` を参照してください。 |
| `new` | mapping | いいえ | NEW_MAP の既定値。`new` を参照してください。 |
| `presets` | マッピングのシーケンス | はい | 1 から 99 個のプリセットエントリ。0 個、99 個を超える、またはシーケンスでない値は `OL_E_SESSION_PROFILE_INVALID` です。 |

### `compatibility`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `maps` | 文字列のシーケンス | いいえ | このプロファイルが有効なマップ識別子（`maps\<Map>\global.cfg`）。`/` は `\` に正規化され、比較は大文字と小文字を区別しません。リストがない、または空の場合は「任意のマップ」を意味します。空でない場合、`WorldMode.NewMap` に対して（有効な `new.map` または `/map` と照合）、および `WorldMode.SavedSituation` に対して（選択された `.osn` が参照するマップを、コンテンツカタログを通じて解決したものと照合）強制されます。`WorldMode.LastMapState` ではマップを導出できないため、空でないリストは常に失敗します。失敗: `OL_E_SESSION_PROFILE_MAP_MISMATCH`。 |

### `new`

このブロックは存在する場合は常に読み取られて検証されますが、spec に適用されるのは、選択された world モードが NEW_MAP（`/new`、CLI の既定）の場合だけです。`/saved:<file.osn>` の下では、このブロックは無視されます。

| キー | 型 | 必須 | 適用 | 説明 |
| --- | --- | --- | --- | --- |
| `map` | string | いいえ | はい | 正規化された形式 `maps\<Map>\global.cfg` のマップ識別子（プランニングではこの形が厳密に要求されます: `maps\` で始まり、`\global.cfg` で終わり、`..` を含まない）。`WorldSpec.MapIdentity` を設定します。 |
| `entrypoint-index` | integer | いいえ | はい | 提示されるエントリポイントのインデックス（OMSI のエントリポイント一覧における 0 始まりの位置）。`PresentedEntrypointIndex` を設定し、エントリポイント識別子をクリアします。 |
| `entrypoint` | string | いいえ | はい | 生のエントリポイント識別子。`EntrypointIdentity` を設定し、提示インデックスをクリアします。`entrypoint-index` と `entrypoint` の両方がある場合、最後に適用される `entrypoint` が優先されます。値は正確な（大文字と小文字を区別する）ラベルまたはディスカバリ識別子です。OMSI は各ラベルの最初のレコードだけを提示します（`world.entrypoint-identity`、`EXPERIMENTAL`）。 |
| `date` | mapping | いいえ | いいえ（`UNAVAILABLE`） | `new.date` を参照してください。 |
| `time` | mapping | いいえ | いいえ（`UNAVAILABLE`） | `new.time` を参照してください。 |
| `year` | integer | いいえ | いいえ（`UNAVAILABLE`） | 明示的な年。 |
| `weather` | mapping | いいえ | いいえ（`UNAVAILABLE`） | `new.weather` を参照してください。 |

`date`、`time`、`year`、`weather` は、`DateTimeMode.Explicit` / 選択された `WeatherMode` を持つ `DateSpec`、`TimeSpec`、`YearSpec`、`WeatherSpec` にコンパイルされます。セッションプランナー（`src/OmsiLaunch.Core/SessionPlanner.cs`）は `world.explicit-date`、`world.explicit-time`、`world.explicit-year`、`world.weather` を必要とします。4 つはすべて `EXPERIMENTAL` です（静的に確定済みで、ランタイム検証は保留中）。開始日時は NEW_MAP にのみ適用され、天候はすべてのワールドモードに適用されます。

#### `new.date`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `mode` | string | はい | `explicit` である必要があります（大文字と小文字を区別しません）。それ以外の値は `OL_E_SESSION_PROFILE_INVALID` です（"date must use explicit mode."）。 |
| `value` | string | はい | `yyyy-MM-dd`。 |

#### `new.time`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `mode` | string | はい | `explicit` である必要があります。 |
| `value` | string | はい | `HH:mm` または `HH:mm:ss`。 |

#### `new.weather`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `mode` | string | はい | `preset`、`icao`、`real` のいずれか（大文字と小文字を区別しません）。それ以外: `OL_E_SESSION_PROFILE_INVALID`（"Unsupported weather mode"）。 |
| `preset` | string | `mode: preset` の場合 | 天候プリセット名。 |
| `icao` | string | `mode: icao` の場合 | ICAO 観測所コード。 |

<a id="preset-each-entry-of-presets"></a>
### `preset`（`presets` の各エントリ）

| キー | 型 | 必須 | 既定値 | 説明 |
| --- | --- | --- | --- | --- |
| `index` | integer | はい | | 1 から 99 で、プロファイル内で一意です。インデックスは連続していなくてもかまいません。`/predefined-profile-index` で選択します。重複または範囲外: `OL_E_SESSION_PROFILE_INVALID`。プロファイルのどこにも存在しないインデックス: `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`。 |
| `id` | string | はい | | プリセット識別子。`SessionProfileMetadata.PresetId` として報告されます。 |
| `name` | string | はい | | プリセットの表示名。`SessionProfileMetadata.PresetName` として報告されます。 |
| `settings` | mapping | いいえ | なし | セマンティックな `options.cfg` 設定。[設定](#settings)を参照してください。キーは大文字と小文字を区別せずにカタログと照合されます。 |
| `presentation` | mapping | いいえ | 継承 | スプラッシュの表示。`presentation` を参照してください。ない場合、プリセットはベースライン（`/spec` の値、または CLI の既定値 `Managed`）を継承します。 |
| `internet-textures` | mapping | いいえ | 継承 | `internet-textures` を参照してください。 |
| `behavior` | mapping | いいえ | 継承 | タイムアウト。`behavior` を参照してください。 |
| `hofs` | マッピングのシーケンス | いいえ | 継承 | セッションの HOF 操作。`hofs` を参照してください。指定すると、ベースラインの `HofManagement` を置き換えます。 |
| `modules` | マッピング | いいえ | 継承 | ランタイムモジュール。`modules` を参照してください。`/spec` ファイルが要求するものと結合されます。 |

適用されるのは選択されたプリセットだけです。ただし、すべてのプリセットが解析・検証されるため、プリセット 3 にエラーがあるとプリセット 1 の要求も失敗します。

### `presentation`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `splash` | mapping | はい | `presentation` がある場合は必須です（"Presentation requires splash."）。`presentation.splash` を参照してください。 |

#### `presentation.splash`

| キー | 型 | 必須 | 既定値 | 説明 |
| --- | --- | --- | --- | --- |
| `mode` | string | はい | | `managed` は、セッションの間 OmsiLaunch のスプラッシュビットマップをインストールします（`SplashMode.Managed`）。`unset` または `native` は OMSI 自身のスプラッシュファイルを保持します（`SplashMode.Unset`。`Native` は別名）。大文字と小文字を区別しません。それ以外: `OL_E_SESSION_PROFILE_INVALID`。 |
| `language` | string | いいえ | `ENG` | 2 つ目のスプラッシュターゲットのロケール: `PTB`、`ENG`、`DEU`、`FRA`（別名 `PT-BR`、`EN`、`DE`、`FR`。未知のものはすべてセッション構築時に `ENG` に解決されます）。`mode: managed` の場合、セッションは `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_<language>.bmp` をオーバーレイします。 |
| `assets` | string | いいえ | パッケージ同梱のアセット | **パッケージからの相対**ディレクトリで、`ENG.bmp` と、英語以外の `language` の場合は `<language>.bmp` を含みます。それぞれ 640x480、24 ビット BMP である必要があります。ディレクトリはプロファイル読み込み時に存在している必要があり（`OL_E_SESSION_PROFILE_ASSET_MISSING`）、ファイルはセッション開始時に検証されます（`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`）。パスの制限ルールが適用されます。省略した場合は、インストール環境の `.omsilaunch\assets\splash`（またはパッケージ同梱の既定値）が使われます。 |

プロファイルは `SessionPresentationSpec.SuppressTrayIcon` を設定できません。`/spec` で設定しない限り `false` のままです。

### `internet-textures`

| キー | 型 | 必須 | 説明 |
| --- | --- | --- | --- |
| `mode` | string | はい | `native`（`InternetTexturesMode.Native`、OMSI は通常どおり動作）、`disabled`（`Disabled`、プロファイル済みのプロセス内ダウンローダーをセッションの間抑止）、`override`（`Override`、セッションスコープの `.itx` プロファイルを `Texture\standard.itx` としてインストール）。大文字と小文字を区別しません。それ以外: `OL_E_SESSION_PROFILE_INVALID`。 |
| `profile` | string | `override` の場合は必須 | `.itx` ファイルの**パッケージからの相対**パス。`override` でキーがない場合: `OL_E_SESSION_PROFILE_INVALID`。ファイルがない場合: `OL_E_SESSION_PROFILE_ASSET_MISSING`。パスの制限ルールが適用されます。ファイルは `http://` または `https://` の URL を持つ `URL` / `target` の行ペアで構成されている必要があり（そうでなければ `OL_E_ITX_PROFILE_INVALID`）、すべてのターゲットは再解析ポイントを経由せずにインストール環境の `Texture\` ディレクトリ配下に解決される必要があります（`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。列挙されたターゲットと `Texture\standard.ipr` はセッションによる削除対象になります（[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照）。 |

### `behavior`

| キー | 型 | 必須 | 既定値 | 説明 |
| --- | --- | --- | --- | --- |
| `startup-timeout` | integer（秒） | いいえ | 180 | プロセス開始から `Running` までに許容される時間。プロファイル読み込み時には正の値である必要があり、さらにセッション開始時には 1 から 600 である必要があります（そうでなければ `OL_E_START_SESSION`）。`LaunchBehaviorSpec.StartupTimeoutSeconds` に対応します。 |
| `shutdown-timeout` | integer（秒） | いいえ | 30 | `LaunchBehaviorSpec.ShutdownTimeoutSeconds` に対応します。ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: スーパーバイザーは OMSI を直接終了させ、この値を読み取ることはありません。 |

`behavior` ブロックがある場合、両方のタイムアウトが設定され（指定値または既定値）、ベースラインの `LaunchBehaviorSpec` 全体を置き換えます。これには `RestoreConfiguration` と `SuppressStaleClosecheckWarning` も含まれ、これらは既定値（`true`、`true`）に戻ります。

### `hofs`

各エントリは [HOF 管理](hof-management.md)の操作 1 件で、ソースのキーをちょうど 1 つ（`package`、`collection`、`directory`、`vehicle` または `file`）指定します。0 個または複数: `OL_E_SESSION_PROFILE_INVALID`。ソースとターゲットの存在は、`package` を除き、セッションのプラン作成時に検査されます（`OL_E_HOF_*`）。

| キー | 型 | 必須 | 既定値 | 説明 |
| --- | --- | --- | --- | --- |
| `package` | string | ソースキーのいずれか 1 つ | | プロファイルが同梱する `.hof` ファイルを保持する、**パッケージからの相対**ディレクトリ。プロファイルの読み込み時に存在していなければなりません（`OL_E_SESSION_PROFILE_ASSET_MISSING`）。パスの制限規則が適用されます。`Directory` ソースになります。 |
| `collection` | string | ソースキーのいずれか 1 つ | | `.omsilaunch\hof\` 配下のコレクションの名前。 |
| `directory` | string | ソースキーのいずれか 1 つ | | インストール環境からの相対ディレクトリ。たとえばマップのコンテンツの隣。 |
| `vehicle` | string | ソースキーのいずれか 1 つ | | HOF のコピー元となる車両の識別子またはフォルダー。 |
| `file` | string | ソースキーのいずれか 1 つ | | インストール環境からの相対 `.hof` ファイル。 |
| `targets` | 文字列のシーケンス | はい | | ターゲット車両（`Vehicles\` 配下の識別子またはフォルダー）。ない、または空: `OL_E_SESSION_PROFILE_INVALID`。 |
| `files` | 文字列のシーケンス | いいえ | すべて | ソースから取り込むファイル名。 |
| `mode` | string | いいえ | `add` | `add` または `mirror`（大文字と小文字を区別しない）。それ以外: `OL_E_SESSION_PROFILE_INVALID`。 |

`hofs` を持つプリセットを `/hof-source` フラグと組み合わせた場合: `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`。

### `modules`

[ランタイムモジュール](../../../reference/runtime-modules.md) ID ごとに 1 エントリ（`EXPERIMENTAL`）。各エントリは次のキーを持つマッピングです（コンテキスト `module`）。

| キー | 型 | 必須 | 既定値 | 説明 |
| --- | --- | --- | --- | --- |
| `enabled` | `true` または `false` | いいえ | 未設定 | `true` はモジュールを要求します（`LaunchSpec.Modules.Enable`）。`false` は、既定、そのマップ、または設定ファイルによって読み込まれるモジュールを含め、モジュールをセッションから除外します（`LaunchSpec.Modules.Disable`）。 |
| `settings` | マッピング | いいえ | なし | キーごとのモジュール設定（`<id>.<key>` としての `LaunchSpec.Modules.Settings`）。値は、セッションのプラン作成時にモジュールが宣言した設定と照合されます（`OL_E_MODULE_SETTING_INVALID`）。 |

```yaml
modules:
  example-preload:
    enabled: true
    settings:
      ram_budget_mb: 8192
      monitor: false
  example-hud:
    enabled: false
```

`[a-z0-9][a-z0-9-]{1,47}` でないモジュール ID、`true` または `false` 以外の `enabled`、あるいは `[a-z][a-z0-9_]{0,31}` でない設定キー: `OL_E_SESSION_PROFILE_INVALID`。インストールされていないモジュールは、セッションのプラン作成時に報告されます（`enabled: true` では `OL_E_MODULE_NOT_FOUND`、設定のみでは `OL_W_MODULE_SETTING_IGNORED`）。

<a id="settings"></a>
## 設定

`settings` のキーは `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）のセマンティックな名前です。コンパイラーは、キーが存在し（`OL_E_SESSION_PROFILE_SETTING_UNKNOWN`）、書き込み可能である（`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE`）場合にのみ受け付けます。値は文字列として格納され、セッションがオーバーレイを構築するときに `options.cfg` のパッチに変換されます。そのため、無効な値が検出されるのはプロファイル読み込み時ではなく `StartSessionAsync` の時点であり、メッセージに `OL_E_INVALID_SETTING_VALUE: <key>` を含む `OL_E_START_SESSION` でセッションが失敗します。以下の設定はすべて `options.cfg` に書き込みます。いずれもセッションスコープであり、セッション終了後に正確に復元されます。

値の形式:

- **bool** は `true` または `false` です（大文字と小文字を区別しません）。存在トークンの場合、トークンが追加または削除されます。反転トークン（`no_*`）の場合、`true` は否定トークンを削除します。
- **int** / **decimal** は範囲に照らして検証されます。除数を持つ値は割った値で格納されます（たとえば `graphics.minObjectScreenPercent: 5` は `0.05` を書き込みます）。
- **string** はそのまま書き込まれます。

| 設定キー | `options.cfg` トークン | 型 | 範囲 / 値 | エビデンス |
| --- | --- | --- | --- | --- |
| `general.language` | `language` | string | 任意 | STATICALLY_VALIDATED |
| `general.radio` | `radio` | string | 任意 | STATICALLY_VALIDATED |
| `general.alternateView` | `altView` | bool（存在） | | STATICALLY_VALIDATED |
| `general.showOwnDriver` | `see_own_driver` | bool（存在） | | STATICALLY_VALIDATED |
| `general.showErrorMessages` | `showerrormessages` | bool（存在） | | STATICALLY_VALIDATED |
| `general.autoSave` | `noAutoSave` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `general.currentTime` | `useActTime` | bool（存在） | | STATICALLY_VALIDATED |
| `general.currentDate` | `useActDate` | bool（存在） | | STATICALLY_VALIDATED |
| `general.currentYear` | `useActYear` | bool（存在） | | STATICALLY_VALIDATED |
| `graphics.screenRatio` | `screenratio` | string | 任意 | STATICALLY_VALIDATED |
| `graphics.maxFPS` | `maxFPS` | int | 10..200 | STATICALLY_VALIDATED |
| `graphics.tileDistance` | `performance_tiledistmax` | int | 1..20 | STATICALLY_VALIDATED |
| `graphics.maxObjectDistanceMeters` | `performance_maxObjDist` | int | 20..5000 | STATICALLY_VALIDATED |
| `graphics.minObjectScreenPercent` | `performance_minObjSize` | decimal | 0..10、/100 で格納 | STATICALLY_VALIDATED |
| `graphics.minReflectionObjectScreenPercent` | `performance_minObjSizeRefl` | decimal | 0..50、/100 で格納 | STATICALLY_VALIDATED |
| `graphics.maxObjectComplexity` | `maxcomplexity` | int | 0..3 | STATICALLY_VALIDATED |
| `graphics.maxMapComplexity` | `maxcomplexity_map` | int | 0..2 | STATICALLY_VALIDATED |
| `graphics.sunGlow` | `sunglow` | bool（存在） | | STATICALLY_VALIDATED |
| `graphics.loadAllTiles` | `loadAllTiles` | bool（存在） | | STATICALLY_VALIDATED |
| `graphics.stencilBuffer` | `no_stencilbuffer` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `graphics.stencilShadows` | `shadow_stencil` | bool、`on` / `off` として書き込み | | STATICALLY_VALIDATED |
| `graphics.rainReflections` | `no_rain_refl` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `graphics.humansInRainReflections` | `no_humans_on_rain_refl` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `graphics.realTimeReflections` | `performance_realreflexions` | string | `economy` または `full` | STATICALLY_PARTIAL |
| `graphics.particles` | `smokesystems`（4 行のブロック） | `enabled,maxPerEmitter,playerVehicleOnly,inReflections`（bool,int>=0,bool,bool） | | STATICALLY_VALIDATED |
| `simulation.collision` | `no_collision` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `simulation.collisionTerrain` | `no_collision_terrain` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `simulation.collisionVehicles` | `no_collision_vehToVeh` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `simulation.collisionPedestrians` | `no_collision_pedastrians` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `simulation.ticketSelling` | `ticketselling` | int | 0..2 | STATICALLY_VALIDATED |
| `simulation.maintenance` | `wear_lifespan` | int | 0..4 | STATICALLY_VALIDATED |
| `simulation.disableAutomaticScheduleAnalysisPopup` | `no_schedAnaPopUp` | bool（存在） | | STATICALLY_VALIDATED |
| `simulation.ticketInfo` | `no_ticketinfo_visible` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `simulation.automaticClutch` | `no_automaticClutch` | bool（反転した存在） | | STATICALLY_VALIDATED |
| `advanced.reducedMultithreading` | `no_multithreading_calculate` + `no_multithreading_texload` | bool（両方とも存在トークン） | | RUNTIME_PROVEN |
| `view.driverSmooth` | `driverview_smooth` | bool（存在） | | STATICALLY_VALIDATED |
| `view.driverMoving` | `driverview_moving` | bool（存在） | | STATICALLY_VALIDATED |
| `controls.autoCenter` | `autoCenter` | bool（存在） | | STATICALLY_VALIDATED |
| `controls.reducedSteeringSpeed` | `redSteerSpd` | bool（存在） | | STATICALLY_VALIDATED |
| `traffic.randomVehicles` | `AIMaxCountRandom` の成分 0 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 ランタイム） |
| `traffic.humans` | `AIMaxCountRandom` の成分 1 | int | 0..1000 | STATICALLY_VALIDATED（RV-005 ランタイム） |
| `traffic.factorPercent` | `AIUnschedFactor` | int | 1..300 | STATICALLY_VALIDATED |
| `traffic.parkedVehiclesPercent` | `AIMaxCountParked` | int | 0..100 | STATICALLY_VALIDATED |
| `traffic.scheduledVehicles` | `AIMaxCountScheduled` | int | 0..1000 | STATICALLY_VALIDATED |
| `traffic.scheduledLinePriority` | `AIPriorityScheduled` | int | 1..4 | STATICALLY_VALIDATED |
| `traffic.passengerFactorPercent` | `AIPassFactor` | int | 0..200 | STATICALLY_VALIDATED |
| `sound.stereo` | `sound_stereo` | int | 0..100 | STATICALLY_VALIDATED |
| `sound.maxSimultaneousSounds` | `sound_maxcount` | int | 5..1000 | STATICALLY_VALIDATED |
| `sound.masterVolume` | `sound_vol_master` | decimal | 0..1 | STATICALLY_VALIDATED |

カタログに存在するが**書き込み不可**のエントリ（`OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` で拒否されます）: `advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`（`advanced.reducedMultithreading` に置き換えられています）、`graphics.texture`、`graphics.textureFilter`。

<a id="path-confinement"></a>
## パスの制限

`presentation.splash.assets` と `internet-textures.profile` は `Confined(package root, value)` によって解決されます。

1. ルート付きのパス（`C:\...`）、`\` で始まるパス、および `..` に等しいパスコンポーネントを含むパスは拒否されます。
2. 完全パスが算出され、それはパッケージディレクトリで始まる必要があります。
3. パッケージルートより下にある既存のすべてのコンポーネント（最終パスを含む）について、`ReparsePoint` 属性が検査されます。そのパス上のどこかにジャンクション、ディレクトリのシンボリックリンク、ファイルのシンボリックリンクがあると拒否されます。検査できないコンポーネント（`IOException` / `UnauthorizedAccessException`）も同様に拒否されます。

3 つの失敗はいずれも `OL_E_SESSION_PROFILE_PATH_ESCAPE` です。同じ再解析ポイントのルールが、セッション構築時に `Texture\` 配下の `.itx` ターゲットにも適用されます。

<a id="precedence-and-override-conflicts"></a>
## 優先順位と上書きの競合

`CliInput.BuildSpecAsync` は次の順序で spec を組み立てます。

1. **既定値**（NEW_MAP、すべて未設定、タイムアウト 180 s / 30 s）。
2. **`/spec:<file.json>`** が指定されている場合は、既定値を完全に置き換えます。
3. **インストールルート**: 明示的なインストール引数は spec の `RootPath` より優先されます。`.` は実行ファイルを含むディレクトリを意味します。
4. **プロファイル**（`/predefined-profile` + `/predefined-profile-index`）: パッケージが読み込まれ、何かがマージされる**前に**、生の CLI 引数に対して `RejectProfileConflicts` が実行されます。次に、シードの world ブロックが、選択されたモードの空の `WorldSpec` にリセットされ（プロファイルを使う場合、`/spec` の world は破棄されます）、`SessionProfileCompiler.Apply` がプロファイルをシードに重ねます: `new`（NEW_MAP の場合のみ）、`settings`（シードの `Environment.General` の上にマージされ、キーごとにプロファイルが優先）、および `presentation`、`internet-textures`、`behavior`（それぞれ、プリセットが定義している場合にのみシードのブロックを置き換えます）。
5. **残りの CLI 引数**がその上に重ねられます: `/map`、`/entrypoint`、`/entrypoint-index`、`/date`、`/time`、`/year`、天候フラグ、車両フラグ、`/set`、スプラッシュフラグ、internet-textures フラグ、`/startup-timeout`、`/shutdown-timeout`。CLI のタイムアウトは指定された場合にのみ適用され、指定されない場合は spec/プロファイル/既定値の値がそのまま使われます。
6. NEW_MAP 以外のモードに対する**互換性チェック**（`ValidateCompatibility`）。

選択されたプロファイルが所有するフィールドを対象とする CLI 引数は競合であり、`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`（終了コード 2、カテゴリ `invalid_argument`）で拒否されます。チェックは値単位ではなくフィールド単位です。プロファイル自身と同じ値を繰り返し指定しても競合になります。

| CLI 引数 | プロファイルが次を定義している場合に競合 | 対象モード |
| --- | --- | --- |
| `/map` | `new.map` | NEW_MAP |
| `/entrypoint` または `/entrypoint-index` | `new.entrypoint` または `new.entrypoint-index` | NEW_MAP |
| `/date` | `new.date` | NEW_MAP |
| `/time` | `new.time` | NEW_MAP |
| `/year` | `new.year` | NEW_MAP |
| `/weather`、`/weather-icao`、`/weather-real` | `new.weather` | NEW_MAP |
| `/set:<key>=...` | プリセットの `settings` 内の同じ `<key>`（大文字と小文字を区別しない） | すべて |
| `/splash`、`/splash-language`、`/splash-assets` | `presentation`（内容を問わず） | すべて |
| `/internet-textures`、`/internet-textures-profile` | `internet-textures`（内容を問わず） | すべて |
| `/startup-timeout`、`/shutdown-timeout` | `behavior`（内容を問わず） | すべて |
| `/module:<id>` | `modules.<id>.enabled: false` | すべて |
| `/no-module:<id>` | `modules.<id>.enabled: true` | すべて |
| `/module-setting:<id>.<key>=...` またはモジュールフラグ | `modules.<id>.settings.<key>` | すべて |

競合にならないもの: プリセットが定義していない `/set` のキー（追加されます）、車両フラグ（`/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration`、`/no-vehicle`。プロファイルはプレイヤー車両を定義できません）、および `/saved` の下での world 引数（そこでは `new` ブロックは適用されません）。`/map`、`/entrypoint`、`/entrypoint-index` は、プロファイルの有無にかかわらず `/saved` と併用すると無効です（`OL_E_INVALID_ARGUMENT`）。

<a id="error-codes"></a>
## エラーコード

| コード | 発生条件 | CLI の終了コード |
| --- | --- | --- |
| `OL_E_SESSION_PROFILE_NOT_FOUND` | `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` が存在しない | 2 |
| `OL_E_SESSION_PROFILE_PATH_ESCAPE` | `id` が単純なディレクトリ名でない。`assets` / `profile` がパッケージの外に出る、または再解析ポイントを経由する | 2 |
| `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED` | `schema` が `omsilaunch.session-profile/v1` でない | 2 |
| `OL_E_SESSION_PROFILE_INVALID` | サイズ上限、ドキュメントの形、アンカー、未知のキー、必須キーの欠落、スカラーでない値、不正な数値/日付/時刻、`id` の不一致、プリセットの個数/インデックスのルール、サポートされていないモードの語、正でないタイムアウト、`splash` のない `presentation`、`profile` のない `override` | 2 |
| `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND` | `/predefined-profile-index` がない、1..99 の範囲外、またはその `index` を持つプリセットがない | 2 |
| `OL_E_SESSION_PROFILE_SETTING_UNKNOWN` | `settings` のキーがカタログにない | 2 |
| `OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE` | `settings` のキーがカタログにあるが読み取り専用 | 2 |
| `OL_E_SESSION_PROFILE_ASSET_MISSING` | `assets` ディレクトリまたは `profile` ファイルがパッケージ内に存在しない | 2 |
| `OL_E_SESSION_PROFILE_MAP_MISMATCH` | `compatibility.maps` が空でなく、有効なマップが一覧にない（または導出できない） | 2 |
| `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` | 明示的な CLI 引数がプロファイルの所有するフィールドを対象としている | 2 |

これらはすべて、コマンドラインのコンパイル中、プランニングの前に発生します。いずれも `SessionProfileException`（競合の場合は `ArgumentException`）であり、セッションが開始されることはありません。完全なカタログは[エラー](errors.md)に、終了コードは[終了コード](exit-codes.md)にあります。

## Discovering installed profiles

`OmsiLaunch.exe /list:SessionProfiles`（API `ListSessionProfilesAsync`）は、`.omsilaunch\session-profiles\` 配下のすべてのパッケージをコンパイルし、パッケージごとに 1 エントリを出力します。`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity`、およびインデックス順の `Presets`（`Index`、`Id`、`Name`）です。コンパイルできないパッケージも `IsValid: false` とともに一覧され、起動した場合に報告されるのと同じ `OL_E_SESSION_PROFILE_*` コードが付きます。何も書き込まれません。

```text
OmsiLaunch.exe /list:SessionProfiles --json
```

<a id="how-a-profile-appears-in-the-api"></a>
## API におけるプロファイルの見え方

読み込みに成功すると、spec は `LaunchSpec.SessionProfile` に `SessionProfileMetadata` レコードを持ちます。

| フィールド | 取得元 |
| --- | --- |
| `Id` | `id` |
| `Name` | `name` |
| `Version` | `version` |
| `Author` | `author` |
| `PresetId` | 選択されたプリセットの `id` |
| `PresetIndex` | 選択されたプリセットの `index` |
| `PresetName` | 選択されたプリセットの `name` |
| `PackagePath` | パッケージディレクトリの絶対パス |

プランナーは、このような spec から構築されたすべての `SessionPlan` に、情報提供用の診断 `session_profile.selected` を追加します。データキーは `session_profile.id`、`session_profile.name`、`session_profile.version`、`session_profile.author`、`session_profile.preset_id`、`session_profile.preset_index`、`session_profile.preset_name`、`session_profile.path` です。これは実行可能性には影響しません。[公開 API](public-api.md) を直接使うインテグレーターは、`OmsiLaunch.Core` の `SessionProfileCompiler.Load` と `SessionProfileCompiler.Apply` を呼び出せます。YAML の表現が `OmsiLaunch.Api` に渡ることはありません。

<a id="examples"></a>
## 例

<a id="example-1-settings-only-profile-one-preset"></a>
### 例 1: 設定のみのプロファイル、プリセット 1 つ

`<root>\.omsilaunch\session-profiles\quiet-evening\profile.yaml`

```yaml
schema: omsilaunch.session-profile/v1
id: quiet-evening
name: Quiet evening
author: Example author
version: "1.0"
presets:
  - index: 1
    id: default
    name: Low traffic, no autosave
    settings:
      traffic.randomVehicles: 40
      traffic.humans: 60
      general.autoSave: false
      sound.masterVolume: 0.6
```

実行: `OmsiLaunch.exe /predefined-profile:quiet-evening /predefined-profile-index:1 /new /map:maps\Grundorf\global.cfg /entrypoint-index:0`。プロファイルが `new` ブロックを定義していないため、マップとエントリポイントはコマンドラインから指定します。`/set:graphics.maxFPS=60` を追加することは許可されますが、`/set:traffic.humans=10` を追加すると競合になります。

<a id="example-2-map-bound-profile-with-three-presets-and-packaged-assets"></a>
### 例 2: マップに紐づき、3 つのプリセットと同梱アセットを持つプロファイル

`<root>\.omsilaunch\session-profiles\grundorf-tour\profile.yaml`。パッケージ内に `assets\splash\ENG.bmp`、`assets\splash\DEU.bmp`、`textures\offline.itx` があります。

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-tour
name: Grundorf guided tour
author: Example team
version: "2.1"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 0
presets:
  - index: 1
    id: low
    name: Low-end PC
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
      graphics.rainReflections: false
    presentation:
      splash:
        mode: managed
        language: DEU
        assets: assets\splash
    internet-textures:
      mode: disabled
    behavior:
      startup-timeout: 300
  - index: 2
    id: mid
    name: Mid-range PC
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 6
    internet-textures:
      mode: override
      profile: textures\offline.itx
  - index: 3
    id: high
    name: High-end PC
    settings:
      graphics.maxFPS: 120
      graphics.tileDistance: 10
      advanced.reducedMultithreading: false
    presentation:
      splash:
        mode: native
```

実行: `OmsiLaunch.exe /predefined-profile:grundorf-tour /predefined-profile-index:2 /new`。`/saved:situations\mytrip.osn` を使う場合、`new` ブロックはスキップされ、`.osn` は `maps\Grundorf\global.cfg` を参照している必要があります。

<a id="the-packaged-example"></a>
### 同梱の例

リリースには `docs/examples/session-profiles/rmg-leste/profile.yaml`（[表示](../../../examples/session-profiles/rmg-leste/profile.yaml)）が同梱されています。これは構文的に有効で、スキーマに適合し、エラーなく読み込まれます。ただし、次の 2 つの性質により、このビルドでは変更なしでセッションを開始することはできません。

1. `new.date`、`new.time`、`new.weather` を設定しており、これらはプランを実行不可にします（[`new` ブロック](#new)を参照）。
2. パスの値がバックスラッシュを二重にしたプレーンスカラー（`maps\\RMG Leste\\global.cfg`）です。YAML はそれらを二重のまま保持し、マップ識別子はテキストとして比較される（`/` から `\` への正規化のみ行った後）ため、`new.map` と `compatibility.maps` はカタログの識別子 `maps\RMG Leste\global.cfg` と一致しません（プランニング時に `OL_E_MAP_NOT_FOUND`）。`assets` の値は、Windows のパス正規化が二重の区切り文字を 1 つにまとめるため、それでも解決されます。

このビルドで実行可能な形は次のとおりです。

```yaml
schema: omsilaunch.session-profile/v1
id: rmg-leste
name: RMG Leste
author: Equipe RMG
version: "1.0"
compatibility:
  maps:
    - maps\RMG Leste\global.cfg
new:
  map: maps\RMG Leste\global.cfg
  entrypoint-index: 3
presets:
  - index: 1
    id: weak
    name: PC fraco
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
    presentation:
      splash:
        mode: managed
        language: PTB
        assets: assets\splash
    internet-textures:
      mode: disabled
  - index: 2
    id: medium
    name: PC medio
    settings:
      graphics.maxFPS: 40
      graphics.tileDistance: 5
  - index: 3
    id: strong
    name: PC forte
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

<a id="related-pages"></a>
## 関連ページ

- [CLI リファレンス](cli.md): `/predefined-profile`、`/predefined-profile-index`、`/set`、および world フラグ
- [LaunchSpec](launchspec.md): プロファイルのコンパイル先となるレコード
- [トランザクションとリカバリ](../concepts/transactions-and-recovery.md): `settings`、スプラッシュ、`.itx` のオーバーレイがどのように適用・復元されるか
- [ケイパビリティ](capabilities.md)と[ランタイム検証の状況](../status/runtime-validation-status.md)
