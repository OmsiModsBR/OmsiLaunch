# LaunchSpec リファレンス

<!-- l10n: source=reference/launchspec.md source-sha256=fc6d2fffb75e37ffbd49ece3a88aa84e08c55696a149f74e35f98b492b79caef -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/launchspec.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、1 つの OmsiLaunch セッションを記述する要求レコードである `LaunchSpec` の規範的なリファレンスです。対象は、C# での形（`OmsiLaunch.Api`）、CLI が読み込む JSON ファイル形式（`/spec:<path>`、`tools/OmsiLaunch.Cli/LaunchSpecJson.cs`）、各プロパティの型・既定値・検証ルール・現在の効果、プランを実行不可にする検証ルール、そして CLI フラグ、spec ファイル、セッションプロファイルの間の優先順位です。記載しているのは現在のコードが実際に行うことだけです。

関連ページ: [公開 API](public-api.md)、[CLI リファレンス](cli.md)、[セッションプロファイル](session-profiles.md)、[エラーコード](errors.md)、[セッションのライフサイクル](../concepts/session-lifecycle.md)、[ケイパビリティ](capabilities.md)。

<a id="where-a-launchspec-comes-from"></a>
## LaunchSpec の出どころ

| 出どころ | `LaunchSpec` になるまで |
| --- | --- |
| API | インテグレーターがレコードを構築し、`PlanSessionAsync` に渡します。 |
| CLI フラグ | `CliInput.BuildSpecAsync` が組み込みの既定値（NEW_MAP、すべて未設定、`Behavior` は既定値）から開始し、フラグを適用します。 |
| `/spec:<path>` JSON ファイル | `LaunchSpecJson.LoadAsync` で読み込まれ、CLI フラグが上書きする基点（シード）として使われます（[優先順位](#precedence-cli-flags-vs-spec-file-vs-session-profile)を参照）。 |
| セッションプロファイル（`/predefined-profile:<id> /predefined-profile-index:<n>`） | `SessionProfileCompiler.Apply` がプロファイルの world、settings、presentation、internet-textures、behaviour をシードに書き込み、`SessionProfile` メタデータを記録します。 |

以下の[完全な例](#complete-example)はレコードの形に照らして検証済みです。最小構成の例のコピーが `examples/release-session.example.json`（リリースパッケージでは `.omsilaunch\examples\release-session.example.json`）として同梱されています。

<a id="json-form"></a>
## JSON 形式

| ルール | 詳細 |
| --- | --- |
| シリアライザー | `System.Text.Json` を `PropertyNameCaseInsensitive = true`、`ReadCommentHandling = Skip`、`AllowTrailingCommas = true` で使用します。コンバーターは登録されていません。 |
| プロパティ名 | C# のプロパティ名（`Installation`、`RootPath`、...）です。読み込み時の照合は大文字と小文字を区別しません。CLI は PascalCase で書き出します。 |
| 列挙型 | 整数です（文字列列挙型のコンバーターはありません）。`"Mode": 0` は有効で、`"Mode": "NewMap"` は不正な JSON として拒否されます。値は[列挙型](#enumerations)に記載しています。 |
| `OptionalValue<T>` | オブジェクト `{ "Presence": 0 | 1, "Value": <T or null> }` です。`Presence` 0 = `Unset`（値は無視されます）、1 = `Set`（値が存在し、null でない必要があります。値が null の `Set` は検証されず、無効な値として振る舞います）。省略された `OptionalValue` メンバーは `Unset` です。読み取り専用の `IsSet` メンバーは CLI が書き出す出力に含まれ、読み込み時には受け付けられて無視されます。 |
| 任意のレコード | `Year`、`Weather`、`Input`、`Diagnostics`、`Presentation`、`InternetTextures`、`SessionProfile`、`HofManagement`、`Modules` は `null` にするか省略できます。`Effective*` アクセサーが既定値で補います。 |
| 必須のレコード | `Installation`、`World`、`Date`、`Time`、`Environment`（8 つの辞書すべてを含む。空なら `{}` を使用）、`Behavior` はオブジェクトとして存在する必要があります。これらは検証されません。`null` または欠落している場合は後で null 参照により失敗し、CLI はそれを `OL_E_INTERNAL`（終了コード 10）または `OL_E_INVALID_ARGUMENT`（終了コード 2）として報告します。 |
| 未知のプロパティ | バインドの前に拒否されます: `OL_E_SPEC_UNKNOWN_PROPERTY: $.Path.Name`（パスにはファイルに書かれたとおりのメンバー名が使われます）。辞書の内容（`Environment.*`）はプロパティとしては検査されません。 |
| ルート | JSON オブジェクトである必要があります: `OL_E_SPEC_INVALID`。最大ネスト深さは 32 です。 |
| ファイルサイズ | 最大 1 MiB（1 048 576 バイト）: `OL_E_SPEC_TOO_LARGE`。ファイルが存在しない場合: `OL_E_SPEC_NOT_FOUND`。 |
| コメントと末尾のカンマ | `//` および `/* */` のコメントと末尾のカンマは受け付けられます。 |
| 不正な JSON | パーサーの例外は変換されません。CLI は `OL_E_INTERNAL` を終了コード 10 で報告します。 |
| エンコーディング | UTF-8（リーダーは BOM を許容します）。識別子内のバックスラッシュはエスケープする必要があります（`"maps\\Grundorf\\global.cfg"`）。マップ、シチュエーション、車両、HOF の識別子ではスラッシュも受け付けられます。 |

<a id="complete-example"></a>
## 完全な例

```jsonc
{
  // Comments and trailing commas are accepted. Enums are integers.
  "Installation": {
    "RootPath": ".",                          // "." = directory that contains OmsiLaunch.exe (CLI only)
    "ExpectedExecutableSha256": null          // optional pin of Omsi.exe
  },
  "World": {
    "Mode": 0,                                // 0 NewMap, 1 SavedSituation, 2 LastMapState
    "MapIdentity": { "Presence": 1, "Value": "maps\\Grundorf\\global.cfg" },
    "SituationIdentity": { "Presence": 0, "Value": null },
    "PresentedEntrypointIndex": { "Presence": 1, "Value": 1 },
    "EntrypointIdentity": { "Presence": 0, "Value": null }
  },
  "Date": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Time": { "Mode": 0, "Value": { "Presence": 0, "Value": null } },
  "Year": null,
  "Weather": null,
  "PlayerVehicle": { "Presence": 0, "Value": null },
  "Environment": {
    "General": {
      "traffic.randomVehicles": { "Presence": 1, "Value": "150" },
      "graphics.maxFPS": { "Presence": 1, "Value": "60" }
    },
    "Advanced": {}, "Graphics": {}, "AdvancedGraphics": {},
    "Sound": {}, "AiPassengers": {}, "Keyboard": {}, "Controllers": {}
  },
  "Behavior": {
    "RestoreConfiguration": true,             // must be true
    "SuppressStaleClosecheckWarning": true,
    "StartupTimeoutSeconds": 180,             // 1..600
    "ShutdownTimeoutSeconds": 30,             // with CooperativeShutdown
    "CooperativeShutdown": false              // opt-in
  },
  "Input": null,
  "Diagnostics": null,
  "Presentation": {
    "Splash": 1,                              // 0 Unset/Native (keep OMSI files), 1 Managed
    "Language": { "Presence": 0, "Value": null },
    "CustomAssetDirectory": { "Presence": 0, "Value": null },
    "SuppressTrayIcon": false
  },
  "InternetTextures": {
    "Mode": 0,                                // 0 Native, 1 Disabled, 2 Override
    "OverrideProfilePath": { "Presence": 0, "Value": null }
  },
  "SessionProfile": null
}
```

明示的な日付は、ビルドが対応している場合、`"Date": { "Mode": 1, "Value": { "Presence": 1, "Value": { "Year": 2024, "Month": 5, "Day": 1 } } }` のように書き、時刻は `{ "Mode": 1, "Value": { "Presence": 1, "Value": { "Hour": 7, "Minute": 30, "Second": 0 } } }` のように書きます。どちらも NEW_MAP セッションにのみ適用されます（`EXPERIMENTAL`。下記参照）。

<a id="property-reference"></a>
## プロパティリファレンス

「使用」列は、現在のコードがその値をどう扱うかを示します。安定性の表記は[公開 API ページ](public-api.md#stability-vocabulary)の用語に従います。

<a id="launchspec-root"></a>
### `LaunchSpec`（ルート）

| プロパティ | JSON 型 | 必須 | 省略時の既定値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Installation` | `InstallationSpec` オブジェクト | はい | なし | はい | `STABLE_BETA` |
| `World` | `WorldSpec` オブジェクト | はい | なし | はい | `STABLE_BETA` |
| `Date` | `DateSpec` オブジェクト | はい | なし | はい。NEW_MAP のみ（[`DateSpec`](#datespec-timespec-yearspec)を参照） | `EXPERIMENTAL` |
| `Time` | `TimeSpec` オブジェクト | はい | なし | はい。NEW_MAP のみ | `EXPERIMENTAL` |
| `PlayerVehicle` | `OptionalValue<PlayerVehicleSpec>` | いいえ | `Unset` | 診断のために解決されます。いずれかのフィールドが設定されていると実行不可 | `PARTIAL` |
| `Environment` | `EnvironmentSpec` オブジェクト | はい | なし | はい（セマンティックな `options.cfg` オーバーレイ） | `STABLE_BETA` |
| `Behavior` | `LaunchBehaviorSpec` オブジェクト | はい | なし | 一部（レコードを参照） | `STABLE_BETA` / `PARTIAL` |
| `Year` | `YearSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveYear` = モード `Unset` | はい。NEW_MAP のみ | `EXPERIMENTAL` |
| `Weather` | `WeatherSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveWeather` = モード `Unset` | はい。すべてのワールドモードで、ワールドの開始直後に適用されます（[`WeatherSpec`](#weatherspec) を参照） | `EXPERIMENTAL` |
| `Input` | `InputSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveInput` = 両方とも未設定 | はい（[`InputSpec`](#inputspec)を参照） | `EXPERIMENTAL` |
| `Diagnostics` | `DiagnosticsSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveDiagnostics` = 既定値 | 保持されるのみ | `PARTIAL` |
| `Presentation` | `SessionPresentationSpec` オブジェクトまたは null | いいえ | `null` → `EffectivePresentation` = 管理されたスプラッシュ、言語なし、カスタムディレクトリなし、トレイ表示 | はい | `STABLE_BETA` |
| `InternetTextures` | `InternetTexturesSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveInternetTextures` = `Native` | はい | `STABLE_BETA` / `EXPERIMENTAL` |
| `SessionProfile` | `SessionProfileMetadata` オブジェクトまたは null | いいえ | `null` | 出自の記録のみ（`session_profile.selected` プラン診断） | `STABLE_BETA` |
| `HofManagement` | `HofManagementSpec` オブジェクトまたは null | いいえ | `null` → `EffectiveHofManagement` = 操作なし | はい（セッションの HOF オーバーレイ。[HOF 管理](hof-management.md)を参照） | `STABLE_BETA` |
| `Modules` | `RuntimeModulesSpec` オブジェクト（`{ "Enable": [...], "Disable": [...], "Settings": { "<module id>.<key>": "<value>" } }`）または null | いいえ | `null` → `EffectiveModules` = 要求・無効化・設定されたものなし | いいえ（モジュールが OmsiLaunch を通じてインストール環境のファイルに触れることはありません。[ランタイムモジュール](../../../reference/runtime-modules.md)を参照） | `EXPERIMENTAL` |

読み取り専用アクセサー（CLI の JSON 出力に含まれ、読み込み時には無視されます）: `EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures`、`EffectiveHofManagement`、`EffectiveModules`。

### `InstallationSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `RootPath` | string | 必須 | `Omsi.exe` と `plugins\` を含むディレクトリ。[パスのルール](#path-rules)を参照してください。空または空白のみ → `OL_E_INSTALLATION_NOT_FOUND`。 | はい | `STABLE_BETA` |
| `ExpectedExecutableSha256` | string または null | `null` | `null`、または 16 進数 64 桁（それ以外は `OL_E_INVALID_ARGUMENT`）。 | 任意の固定指定: 設定されている場合、`Omsi.exe` の SHA-256 と一致しなければプランは実行不可になります（`OL_E_UNSUPPORTED_BUILD`、データ `expected_sha256` と `file_sha256`）。 | `STABLE_BETA` |

### `WorldSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WorldMode` int | 必須 | `0` `NewMap`、`1` `SavedSituation`、`2` `LastMapState`（`LastSituation` は同じ値 2 を持つ廃止済みの別名です）。 | はい。`LastMapState` には `MapIdentity` とそのマップの `laststn.osn` が必要です（`OL_E_MAP_NOT_FOUND`、`OL_E_SITUATION_NOT_FOUND`） | `NewMap`、`SavedSituation`: `STABLE_BETA`。`LastMapState`: `EXPERIMENTAL` |
| `MapIdentity` | `OptionalValue<string>` | `Unset` | `NewMap` の場合は必須で、`maps\<dir>\global.cfg` の形式（大文字と小文字を区別しない、`/` 可、`..` 不可）であり、インストールされている必要があります。`SavedSituation` では無視されます（マップは `.osn` が提供します）。 | はい（ハンドオフ） | `STABLE_BETA` |
| `SituationIdentity` | `OptionalValue<string>` | `Unset` | `SavedSituation` の場合は必須で、インストール済みの `situations\...\<file>.osn` 識別子（`DiscoverAsync(Situations)` / `/list:situations` が返すもの）です。 | はい（ハンドオフ） | `STABLE_BETA` |
| `PresentedEntrypointIndex` | `OptionalValue<int>` | `Unset` | `EntrypointIdentity` のない `NewMap` の場合は必須で、`>= 0` であり、そのマップについて OMSI が提示するエントリポイント一覧へのインデックスです。未設定の場合はプラグインに `-1` として送られます。 | はい（ハンドオフ） | `STABLE_BETA` |
| `EntrypointIdentity` | `OptionalValue<string>` | `Unset` | 正確な（大文字と小文字を区別する）エントリポイントのラベル、またはディスカバリ識別子。プランナーはそれを OMSI が提示するラベルに解決し、起動はそのラベルを選択します（`world.entrypoint-identity`）。OMSI が提示しないレコードは拒否され（`OL_E_ENTRYPOINT_NOT_PRESENTED`）、未知のものも拒否されます（`OL_E_ENTRYPOINT_NOT_FOUND`）。 | 保持される | `RUNTIME_VALIDATION_REQUIRED` |
| `Entrypoint` | `EntrypointSpec`（読み取り専用） | 算出 | `EntrypointIdentity` が設定されていれば `Mode` = `Identity`、そうでなくインデックスが設定されていれば `PresentedIndex`、それ以外は `Unset`。`PresentedIndex`、`Identity` は入力をそのまま反映します。 | 派生 | `STABLE_BETA` |

<a id="datespec-timespec-yearspec"></a>
### `DateSpec`、`TimeSpec`、`YearSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `DateTimeMode` int | 必須（`Year`: レコードが null の場合は `0`） | `0` `Unset`、`1` `Explicit`、`2` `System`。 | NEW_MAP: `Explicit` は `Value` を使用し、`System` はセッションのプラン作成時の PC のクロックを使用します。`Unset` は OMSI 自身の値（そのオプションと Start フォーム）を保持します。コントローラーは要求を OMSI の開始クロック（年、年内の日、時、分、秒。ケイパビリティ `world.explicit-date`、`world.explicit-time`、`world.explicit-year`）に解決し、スタートアップハンドオフ（バージョン 7）がそれを運び、プラグインが OMSI の `NewSituation` の直前にそれを書き込みます。これにより、OMSI はマップが読み込まれる前に、要求された日付から季節とカレンダーを導出します。開始後に実行中のクロックが読み戻され、不一致の場合、または OMSI が固定の開始時刻を使用している場合は、開始が `OL_E_DATE_TIME_APPLY_FAILED` で失敗します。`SavedSituation`: `Unset` 以外のモードは `OL_E_CAPABILITY_UNAVAILABLE` です（シチュエーションは自身の時刻を持つため）。 | `EXPERIMENTAL` |
| `Value` | `OptionalValue<SemanticDate>` / `OptionalValue<SemanticTime>` / `OptionalValue<int>` | `Unset` | `SemanticDate`: 実在するカレンダー日付（`Day` がその月と年で有効であること）。`SemanticTime`: `Hour` 0..23、`Minute` 0..59、`Second` 0..59。年は 1900..2200。`YearSpec` は年を設定します（両方が指定された場合は `Date` の年も設定します）。うるう年の 12 月 31 日は拒否されます。OMSI のカレンダーは 366 日目を表現できないためです。無効な値: `OL_E_DATE_TIME_APPLY_FAILED`。`Mode` が `Explicit` の場合は設定されていなければならず、それ以外の場合は未設定でなければなりません（`OL_E_INVALID_ARGUMENT`）。 | はい | `EXPERIMENTAL` |

### `WeatherSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `WeatherMode` int | レコードが null の場合は `0` | `0` `Unset`、`1` `Preset`、`2` `Icao`、`3` `RealCurrent`。 | `Unset` 以外のモードには `world.weather` が必要です。プラグインはワールドの開始直後にそれを適用します（すべてのワールドモード。保存済みシチュエーションの天候より優先されます）。失敗した場合、開始は `OL_E_WEATHER_APPLY_FAILED` で失敗します。 | `EXPERIMENTAL` |
| `Preset` | `OptionalValue<string>` | `Unset` | インストール環境の `weather` フォルダー内のファイルを、名前または `weather\<name>.owt` で指定します。プラン作成時に検査されます（`OL_E_WEATHER_INVALID`）。 | ハンドオフ | `EXPERIMENTAL` |
| `Icao` | `OptionalValue<string>` | `Unset` | 4 文字の英字または数字（それ以外は `OL_E_WEATHER_INVALID`）。`Icao` モードで使用されます。 | ハンドオフ | `EXPERIMENTAL` |

<a id="playervehiclespec-inside-playervehicle"></a>
### `PlayerVehicleSpec`（`PlayerVehicle` の内部）

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Model` | `OptionalValue<string>` | `Unset` | インストール済みの `Vehicles\...\<file>.bus` 識別子。そうでなければ `OL_E_VEHICLE_NOT_FOUND`。 | `ResolvedContent` に解決され、その後 `player-vehicle.model` が unsupported → `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Repaint` | `OptionalValue<string>` | `Unset` | `Model` のリペイント識別子（`<cti>#item:<n>`）。そうでなければ `OL_E_REPAINT_NOT_FOUND`。`Model` が設定されている場合にのみ検査されます。 | 同上 | `PARTIAL` |
| `Hof` | `OptionalValue<string>` | `Unset` | インストール済みの `Vehicles\...\<file>.hof`。そうでなければ `OL_E_HOF_NOT_FOUND`。 | 同上 | `PARTIAL` |
| `FleetNumber` | `OptionalValue<string>` | `Unset` | 任意の文字列。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Registration` | `OptionalValue<string>` | `Unset` | 任意の文字列。 | `OL_E_CAPABILITY_UNAVAILABLE` | `PARTIAL` |
| `Enabled` | bool（読み取り専用） | 算出 | `Model` が設定されていれば `true`。ハンドオフが `PlayerVehicleEnabled` として運ぶのは `PlayerVehicle.IsSet` です。 | 派生 | `PARTIAL` |

`Presence` が 1 で、すべてのフィールドが未設定の `PlayerVehicle` は受け付けられ、効果はありません。このビルドでは、いずれかのフィールドが設定されているとプランは実行不可になります（`STATICALLY_PARTIAL`）。

### `EnvironmentSpec`

| プロパティ | 型 | 既定値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- |
| `General`、`Advanced`、`Graphics`、`AdvancedGraphics`、`Sound`、`AiPassengers`、`Keyboard`、`Controllers` | それぞれ `IReadOnlyDictionary<string, OptionalValue<string>>`、必須（空の場合は `{}`） | なし | はい | `STABLE_BETA` |

8 つのグループは連結されます。キーをどのグループに置いても効果は変わりません。`Presence` が 1 の各エントリは `ConfigurationCatalog`（`src/OmsiLaunch.Configuration/ConfigurationCatalog.cs`）のセマンティックな設定です。キーによって対象ファイル（現在のすべてのキーで `options.cfg`）とトークンが決まります。プランニングではキーが存在すること（`OL_E_UNKNOWN_SETTING`）と書き込み可能であること（`OL_E_SETTING_NOT_WRITABLE`）を検査します。値は開始時にのみ検証されます（`OL_E_INVALID_SETTING_VALUE`。`OL_E_START_SESSION` を伴う `Failed` セッションとして報告されます）。キーは大文字と小文字を区別しません。未設定のエントリは無視されます。CLI の `/set:<key>=<value>` フラグは `General` に書き込みます。セッションプロファイルの `settings` も `General` にマージされます。

| キー | 値 | 備考 |
| --- | --- | --- |
| `general.language` | string | `[language]` トークン |
| `general.radio` | string | |
| `general.alternateView`、`general.showOwnDriver`、`general.showErrorMessages`、`general.autoSave`、`general.currentTime`、`general.currentDate`、`general.currentYear` | `true` / `false` | 存在トークン（`autoSave` は `noAutoSave` の逆） |
| `graphics.screenRatio` | string | |
| `graphics.maxFPS` | 整数 10..200 | |
| `graphics.tileDistance` | 整数 1..20 | |
| `graphics.maxObjectDistanceMeters` | 数値 20..5000 | |
| `graphics.minObjectScreenPercent` | 数値 0..10 | 100 で割って格納 |
| `graphics.minReflectionObjectScreenPercent` | 数値 0..50 | 100 で割って格納 |
| `graphics.maxObjectComplexity` | 整数 0..3 | |
| `graphics.maxMapComplexity` | 整数 0..2 | |
| `graphics.sunGlow`、`graphics.loadAllTiles`、`graphics.stencilBuffer`、`graphics.rainReflections`、`graphics.humansInRainReflections` | `true` / `false` | 存在トークン |
| `graphics.stencilShadows` | `true` / `false` | `on` / `off` として書き込み |
| `graphics.realTimeReflections` | `economy` / `full` | `STATICALLY_PARTIAL` |
| `graphics.particles` | `enabled,maxPerEmitter,playerVehicleOnly,inReflections`（bool,int>=0,bool,bool） | `smokesystems` ブロック 1 つ |
| `simulation.collision`、`simulation.collisionTerrain`、`simulation.collisionVehicles`、`simulation.collisionPedestrians`、`simulation.disableAutomaticScheduleAnalysisPopup`、`simulation.ticketInfo`、`simulation.automaticClutch` | `true` / `false` | 存在トークン |
| `simulation.ticketSelling` | 整数 0..2 | |
| `simulation.maintenance` | 整数 0..4 | |
| `advanced.reducedMultithreading` | `true` / `false` | 2 つの OMSI トークンを同時に操作（`RUNTIME_PROVEN`） |
| `view.driverSmooth`、`view.driverMoving`、`controls.autoCenter`、`controls.reducedSteeringSpeed` | `true` / `false` | 存在トークン |
| `traffic.randomVehicles` | 整数 0..1000 | 複数行の `AIMaxCountRandom` ブロックの成分 0（ランタイム検証済み、マトリクス RV-005） |
| `traffic.humans` | 整数 0..1000 | `AIMaxCountRandom` の成分 1 |
| `traffic.factorPercent` | 数値 1..300 | |
| `traffic.parkedVehiclesPercent` | 数値 0..100 | |
| `traffic.scheduledVehicles` | 数値 0..1000 | |
| `traffic.scheduledLinePriority` | 数値 1..4 | |
| `traffic.passengerFactorPercent` | 数値 0..200 | |
| `sound.stereo` | 数値 0..100 | |
| `sound.maxSimultaneousSounds` | 数値 5..1000 | |
| `sound.masterVolume` | 数値 0..1 | |
| `advanced.multithreadingCalculate`、`advanced.multithreadingTextureLoad`、`graphics.texture`、`graphics.textureFilter` | 拒否 | 既知だが書き込み不可 → `OL_E_SETTING_NOT_WRITABLE` |

パッチが適用されたファイルは、エンコーディング（Windows-1252 のバイトはそのまま保持し、BOM 付きの UTF-8/UTF-16 は尊重します）と改行コードを維持します。

### `LaunchBehaviorSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `RestoreConfiguration` | bool | `true` | `true` のみ | 正確な復元は必須です。`false` は `OL_E_INVALID_ARGUMENT` で拒否されます。 | `STABLE_BETA` |
| `SuppressStaleClosecheckWarning` | bool | `true` | 任意 | `true`: セッション前から存在する `closecheck` ファイルは開始時に完全に削除されます（SHA-256 を伴う診断 `closecheck.stale-removed`、失敗時は `OL_E_CLOSECHECK_REMOVE_FAILED`）。`false`: 既存の `closecheck` はそのまま残され、セッションによる削除対象にはなりません。セッション中に OMSI が書き込む `closecheck` は、復元時に常に削除されます。 | `STABLE_BETA` |
| `StartupTimeoutSeconds` | int | `180` | 1..600（範囲外の場合は `StartSessionAsync` から `ArgumentOutOfRangeException`。CLI の `/startup-timeout` は 1..600 を強制し、プロファイルでは > 0 が必要）。スーパーバイザーの開始から `Running` までの時間枠です。期限を過ぎると、セッションは `OL_E_STARTUP_TIMEOUT`（プラグインが開始済みの場合）または `OL_E_PLUGIN_NOT_LOADED` で失敗します。 | はい | `STABLE_BETA` |
| `ShutdownTimeoutSeconds` | int | `30` | 1..600（CLI の `/shutdown-timeout`） | `CooperativeShutdown` を併用した場合: OMSI が強制終了される前に自ら終了するために与えられる時間です。併用しない場合、OMSI は直ちに強制終了されます。 | `EXPERIMENTAL` |
| `CooperativeShutdown` | bool | `false` | 任意（CLI の `/cooperative-shutdown`） | `true`: 実行中のセッションを終了するとき、OMSI を強制終了する代わりに、OMSI 自身の終了ルーチン（`TForm_main.CloseOMSI`: オプションとマップの `laststn.osn` が保存され、プラグインが終了処理されます）で終了するよう依頼します。`ShutdownTimeoutSeconds` の後は強制終了されます（`OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE`）。復元は終了後に実行されるため、セッションが所有するファイルは引き続き元のバイト列に戻ります。 | `EXPERIMENTAL` |

### `InputSpec`

| プロパティ | 型 | 既定値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- |
| `KeyboardDocument` | `OptionalValue<string>` | `Unset` | OMSI 形式のキーボードファイルのパス（絶対パス、またはインストール環境からの相対パス）。セッションは `Inputs\keyboard.cfg` の代わりにこれを使用します（トランザクションのオーバーレイ: インストール環境のファイルは最後にバイト単位で復元されます）。ファイルは存在し、1 MiB 以下で、`[entry]` のバインドを少なくとも 1 つ含んでいなければなりません。そうでなければ `OL_E_INVALID_ARGUMENT`。 | `EXPERIMENTAL` |
| `ControllerDocument` | `OptionalValue<string>` | `Unset` | OMSI 形式のコントローラーファイルのパス。セッションは同様に `Inputs\gamectrler.cfg` の代わりにこれを使用します。`[ctrl]` デバイスを少なくとも 1 つ含んでいなければなりません。そうでなければ `OL_E_INVALID_ARGUMENT`。 | `EXPERIMENTAL` |

### `DiagnosticsSpec`

| プロパティ | 型 | 既定値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- |
| `Log` | bool | `true` | `src/` に使用箇所はありません。ホストトレース `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` は常に書き込まれます。 | `PARTIAL`（保持されるが、現在は効果なし） |
| `Verbose` | bool | `false` | 使用箇所なし。 | `PARTIAL` |
| `OmsiLogAll` | bool | `false` | 使用箇所なし。 | `PARTIAL` |
| `ProcessTrace` | bool | `false` | 使用箇所なし。 | `PARTIAL` |
| `PluginTrace` | bool | `false` | スタートアップハンドオフのフラグ: プラグインはすべてのランタイム操作について `plugin.trace.operation` イベント（操作、結果、コード、マイクロ秒単位の所要時間）を書き出します。これらはテレメトリリングを通じて `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` に届きます。 | `EXPERIMENTAL` |
| `NativeTrace` | bool | `false` | スタートアップハンドオフのフラグ: ネイティブレイヤーはすべてのフックの設置と解除（`native.trace`: 境界、オーナー、ターゲット、結果）を同じホストトレースに報告します。 | `EXPERIMENTAL` |

CLI フラグ `/log`、`/logall`、`/omsi-logall`、`/verbose`、`/trace`、`/trace-process`、`/trace-plugin`、`/trace-native` がこれらのブール値を設定します（`/logall` は `Verbose`、`ProcessTrace`、`PluginTrace`、`NativeTrace` を設定します）。これらは spec の値と OR で結合されます。

### `SessionPresentationSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Splash` | `SplashMode` int | `1`（`Managed`） | `0` `Unset`（別名 `Native`）: OMSI 自身のスプラッシュファイルには手を加えません。`1` `Managed`: OmsiLaunch がセッションの間 `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_<LANG>.bmp` をオーバーレイします（終了後に正確に復元されます）。 | はい | `STABLE_BETA`（マトリクス RV-006） |
| `Language` | `OptionalValue<string>` | `Unset` | `PTB`/`PT-BR`、`ENG`/`EN`、`DEU`/`DE`、`FRA`/`FR`（大文字と小文字を区別しない）。それ以外の値はすべて `ENG` に正規化されます。未設定の場合は、`options.cfg` の `[language]` の値を読み取り、同じ方法で正規化します。 | はい（管理されたスプラッシュの場合のみ） | `STABLE_BETA` |
| `CustomAssetDirectory` | `OptionalValue<string>` | `Unset` | `ENG.bmp` と `<LANG>.bmp`（640×480、24 ビット BMP）を含むディレクトリ。[パスのルール](#path-rules)を参照してください。ディレクトリがない場合: `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`。ファイルがない場合: `OL_E_SPLASH_ASSET_MISSING`。形式が不正な場合: `OL_E_SPLASH_FORMAT_UNSUPPORTED`。未設定の場合は `<root>\.omsilaunch\assets\splash`（パッケージから一度だけ初期配置されます）が使われ、それがなければパッケージ同梱の `assets\splash` が使われます。 | はい（管理されたスプラッシュの場合のみ） | `STABLE_BETA` |
| `SuppressTrayIcon` | bool | `false` | `true` の場合、CLI オーナーの単独の Windows トレイ表示を抑止します。 | CLI オーナーのみ。API にはトレイがありません。CLI フラグはなく、spec ファイルからのみ指定できます。 | `STABLE_BETA` |

### `InternetTexturesSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Mode` | `InternetTexturesMode` int | `0`（`Native`） | `0` `Native`: 何も変更しません。`1` `Disabled`: プラグインが OMSI のプロセス内ダウンローダーを抑止します（テレメトリ `internet-textures.suppressed` / `internet-textures.suppression.failed`）。`2` `Override`: `.itx` プロファイルが `Texture\standard.itx` としてオーバーレイされ、そのターゲットファイルと `Texture\standard.ipr` はセッションによる削除対象になります。 | はい | `Native`: `STABLE_BETA`、`Disabled`、`Override`: `EXPERIMENTAL` |
| `OverrideProfilePath` | `OptionalValue<string>` | `Unset` | `Override` の場合は必須です（`OL_E_ITX_PROFILE_REQUIRED`）。2 行 1 組で構成されるテキストファイルで、1 行目は絶対 `http`/`https` URL、2 行目はインストールルートからの相対ターゲットパスです。ターゲットパスは `Texture\` コンポーネントを含み、ルート付きでなく、`..` を含まず、`\` で始まらず、ジャンクション/シンボリックリンクを経由しない必要があります（`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`）。[パスのルール](#path-rules)を参照してください。 | はい | `EXPERIMENTAL` |

### `HofManagementSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Operations` | `HofOperationSpec` の配列 | なし | 順番に適用されます。解決、競合の規則、上限については [HOF 管理](hof-management.md)を参照してください。 | はい | `STABLE_BETA` |

`HofOperationSpec` のメンバー: `SourceKind`（`HofSourceKind` の int）、`Source`（文字列）、`TargetVehicles`（`Vehicles\` 配下の車両 ID またはフォルダーの配列。少なくとも 1 つ）、`Mode`（`HofApplyMode` の int。既定値は `0` の `Add`）、`Files`（`.hof` ファイル名の配列。すべてを対象にする場合は null）。問題はプラン診断 `OL_E_HOF_*` になります。

### `RuntimeModulesSpec`

| プロパティ | 型 | 既定値 | 有効な値 | 使用 | 安定性 |
| --- | --- | --- | --- | --- | --- |
| `Enable` | モジュール ID の配列 | なし | `plugins\OmsiLaunch.Modules` 配下にインストールされたモジュールの ID。[ランタイムモジュール](../../../reference/runtime-modules.md)を参照してください。読み込めない要求済みモジュールは、プラン診断 `OL_E_MODULE_*` になります（プランは実行不可）。 | はい | `EXPERIMENTAL` |
| `Disable` | モジュール ID の配列または null | なし | 既定、コンテンツ、または設定ファイルによるモジュールをセッションから除外します（プラン診断 `module.disabled`）。有効化と無効化の両方が指定されたモジュールは `OL_E_MODULE_SETTING_INVALID` です。読み取り専用アクセサー `EffectiveDisable`。 | はい | `EXPERIMENTAL` |
| `Settings` | 文字列のオブジェクトまたは null | なし | `"<module id>.<key>": "<value>"`。設定ファイル `.omsilaunch\modules.ini` とマニフェストの既定値より優先されます。無効な値、またはモジュールが宣言していないキーは `OL_E_MODULE_SETTING_INVALID`（プランは実行不可）です。セッションが読み込まないモジュールの設定は `OL_W_MODULE_SETTING_IGNORED` です。有効な値は `PlannedModule.Settings` にあります。読み取り専用アクセサー `EffectiveSettings`。 | はい | `EXPERIMENTAL` |

### `SessionProfileMetadata`

| プロパティ | 型 | 使用 | 安定性 |
| --- | --- | --- | --- |
| `Id`、`Name`、`Version`、`Author`、`PresetId`、`PresetIndex`、`PresetName`、`PackagePath` | 文字列 / int | プラン診断 `session_profile.selected`（`Data["session_profile.*"]`）に記録されます。それ以外では使用されません。通常は手作業ではなくセッションプロファイルコンパイラーが設定します。 | `STABLE_BETA` |

<a id="enumerations"></a>
## 列挙型

| 列挙型 | 値（JSON 整数） |
| --- | --- |
| `WorldMode` | `NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（廃止済みの別名。OMSI ネイティブの「前回のマップ状態」分岐であり、最新の `.osn` を意味することはありません） |
| `DateTimeMode` | `Unset` = 0、`Explicit` = 1、`System` = 2 |
| `WeatherMode` | `Unset` = 0、`Preset` = 1、`Icao` = 2、`RealCurrent` = 3 |
| `SplashMode` | `Unset` = 0、`Native` = 0（別名）、`Managed` = 1 |
| `InternetTexturesMode` | `Native` = 0、`Disabled` = 1、`Override` = 2 |
| `HofSourceKind` | `Collection` = 0、`Directory` = 1、`Vehicle` = 2、`File` = 3 |
| `HofApplyMode` | `Add` = 0、`Mirror` = 1 |
| `Presence` | `Unset` = 0、`Set` = 1 |
| `EntrypointMode`（読み取り専用の `Entrypoint.Mode`） | `Unset` = 0、`PresentedIndex` = 1、`Identity` = 2 |

宣言された範囲外の整数はシリアライザーによってそのまま格納され、未知の値として振る舞います（たとえば未知の `WorldMode` は NEW_MAP でも SAVED_SITUATION でもなく、world ケイパビリティを持たないプランになります。プラグインはこれを拒否しますが、CLI はいずれにせよモードを置き換えます。優先順位を参照してください）。

<a id="validation-rules-and-non-runnable-diagnostics"></a>
## 検証ルールと実行不可の診断

`PlanSessionAsync` は `LaunchValidation.Validate` を実行し、続いて `SessionPlanner.PlanAsync` を実行します。プランが実行可能であるのは、`OL_E_` で始まる診断コードが 1 つもない場合に限ります。完全な一覧は次のとおりです。

| 診断 | 条件 | 発生元 |
| --- | --- | --- |
| `OL_E_INSTALLATION_NOT_FOUND` | `Installation.RootPath` が空または空白のみ | `LaunchValidation` |
| `OL_E_DATE_TIME_APPLY_FAILED` | `Date.Mode` = `Explicit` で値が設定されていない、または月/日が範囲外。`Time.Mode` = `Explicit` で値が設定されていない、または時/分/秒が範囲外 | `LaunchValidation` |
| `OL_E_INVALID_ARGUMENT` | モードが `Explicit` でないのに `Date.Value` または `Time.Value` が設定されている | `LaunchValidation` |
| `OL_E_MAP_NOT_FOUND` | `NewMap` で `MapIdentity` が未設定、または `maps\...\global.cfg` の形式でない（検証）。`NewMap` で識別子がインストールされていない（プランナー） | 両方 |
| `OL_E_ENTRYPOINT_NOT_FOUND` | `NewMap` で `EntrypointIdentity` がなく、`PresentedEntrypointIndex` が未設定または負 | `LaunchValidation` |
| `OL_E_ENTRYPOINT_REQUIRED` | `NewMap`、マップはインストール済み、`EntrypointIdentity` なし、`PresentedEntrypointIndex` 未設定（`world.presented-entrypoint` が利用不可） | `SessionPlanner` |
| `OL_E_SITUATION_NOT_FOUND` | `SavedSituation` で `SituationIdentity` がない（検証）、または識別子がインストールされていない（プランナー） | 両方 |
| `OL_E_SITUATION_MAP_NOT_FOUND` | `SavedSituation`: `.osn` 内で指定されたマップがインストールされていない | `SessionPlanner` |
| `OL_E_UNSUPPORTED_OPERATING_SYSTEM` | 「x64 OS 上の Windows 10 以降で、x64 のホストプロセスとして動作している」という条件を満たさない（`runtime.current-windows-x64`） | `SessionPlanner` |
| `OL_E_INSTALLATION_NOT_WRITABLE` | ルートディレクトリが存在しない、読み取り専用属性が設定されている、または `plugins\` サブディレクトリがない（`transaction.exact-restore`） | `SessionPlanner` |
| `OL_E_UNSUPPORTED_BUILD` | OMSI 2 のインストール環境ではない: `Omsi.exe` が存在しない、32 ビット x86 の PE イメージではない、OMSI 2（2.x）のバージョンがない、または `maps\` / `Vehicles\` ディレクトリがない（`omsi.omsi2`）。ハッシュは基準ではありません。 | `SessionPlanner` |
| `OL_E_CAPABILITY_UNAVAILABLE` | `EntrypointIdentity` が設定されている。`SavedSituation` で `Date`/`Time`/`Year` のモードが `Unset` でない。`Weather` のモードが `Unset` でない。`PlayerVehicle` のいずれかのフィールドが設定されている | `SessionPlanner` |
| `OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND` | `PlayerVehicle.Model` / `Repaint` / `Hof` がインストールされていない（`OL_E_CAPABILITY_UNAVAILABLE` に加えて） | `SessionPlanner` |
| `OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE` | カタログにない / 書き込み不可の `Environment` キー | `SessionPlanner` |
| `OL_E_SESSION_PRESENTATION_INVALID` | スプラッシュ/ITX プランの構築中に例外が発生した。メッセージには `OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH` のいずれかが含まれる | `SessionPlanner` |
| `OL_E_RUNTIME_ARTIFACT_MISSING` | プラグインクロージャーの参照（`OmsiLaunchRuntimePaths`）またはリリースマニフェストを読み込めない（メッセージに `OL_E_RELEASE_MANIFEST_INVALID` が含まれる場合がある） | `OmsiLaunchService.PlanSessionAsync` |

プラン時には検証されないもの（開始時に `OL_E_START_SESSION` を伴う `Failed` セッションとして失敗します）: 設定値（`OL_E_INVALID_SETTING_VALUE`）、常駐プラグインの整合性（`OL_E_PERMANENT_PLUGIN_*`）、リースの取得可否（`OL_E_INSTALLATION_BUSY`）、`StartupTimeoutSeconds` の範囲（`StartSessionAsync` がスローします）。

情報提供用のプラン診断: `plugin.integrity.reference`（メッセージは `manifest` または `self`）、`session_profile.selected`。

<a id="precedence-cli-flags-vs-spec-file-vs-session-profile"></a>
## 優先順位: CLI フラグ、spec ファイル、セッションプロファイル

`CliInput.BuildSpecAsync`（`tools/OmsiLaunch.Cli/Program.cs`）は、次の順序で有効な spec を構築します。

1. シード = 組み込みの既定値、または指定されている場合は `/spec` ファイル。
2. インストールルート = 明示的なインストール引数が指定されていればそれ、なければシードの `RootPath`。その後、`.`/空 → 実行ファイルのディレクトリとし、`Path.GetFullPath` を適用します。明示的なインストール引数は常に spec の `RootPath` より優先されます。
3. セッションプロファイル（`/predefined-profile` + `/predefined-profile-index`）: プロファイルが所有するフィールドに触れる明示的な CLI 引数は `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` で拒否されます（world フィールドは NEW_MAP モードの場合のみ、`/set` のキーはプリセットに存在する場合、スプラッシュのフラグはプリセットに `presentation` がある場合、internet-textures のフラグは `internet-textures` がある場合、タイムアウトは `behavior` がある場合）。シードの `World` は新しいものに置き換えられ（CLI の world モードだけが残ります）、その後、プロファイルの `new:` ブロック（NEW_MAP の場合のみ）、`settings`（`General` へ）、`presentation`、`internet-textures`、`behavior`、および `SessionProfile` メタデータが適用されます。`compatibility.maps` は NEW_MAP と SAVED_SITUATION に対して強制されます。
4. World: CLI の world モードが常に優先されます（既定の `/new`、`/saved:<osn>`、`/last`）。spec ファイルの `World.Mode` は置き換えられます。spec から保存済みシチュエーションを実行するには `/saved:` を渡してください。`/map` と `/entrypoint`/`/entrypoint-index` はシードを上書きします。CLI の `/entrypoint` 識別子はインデックスをクリアします。`/saved` を `/map` またはエントリポイントのフラグと併用すると `OL_E_INVALID_ARGUMENT` になります。
5. `/date`、`/time`、`/year`、`/weather*` は、指定された場合にシードを上書きします（`system` は `DateTimeMode.System` を選択します）。
6. `/no-vehicle` は `PlayerVehicle` をクリアします。個々の `/vehicle`、`/repaint`、`/hof`、`/fleet`、`/registration` は、シードのプレイヤー車両の個々のフィールドを上書きします。
7. `/set:<key>=<value>` のエントリは `Environment.General` に追加されます（キーは検査され、値は検査されません）。残りの 7 つのグループはシードから変更されずに引き継がれます。
8. `/startup-timeout` と `/shutdown-timeout` は、指定された場合にのみシードを上書きします。指定されない場合は spec、次にプロファイル、最後に既定値 180 s / 30 s が適用されます。`ShutdownTimeoutSeconds` は `/cooperative-shutdown` を指定した場合に有効になります（これは spec の値と OR されます）。
9. `/splash`、`/splash-language`、`/splash-assets`、`/internet-textures`、`/internet-textures-profile` は、指定された場合にシードを上書きします。`SuppressTrayIcon` はシードからのみ取得されます。
10. 診断フラグはシードと OR で結合されます。

結果: 明示的な CLI フラグ > セッションプロファイル > spec ファイル > 組み込みの既定値。ただし、プロファイルが所有するフィールドと競合する CLI フラグは、上書きではなくエラーになります。

<a id="path-rules"></a>
## パスのルール

| パス | API での動作 | CLI での動作 |
| --- | --- | --- |
| `Installation.RootPath` | 指定されたとおりに使われます。相対パスはファイル操作においてプロセスの作業ディレクトリを基準に解決されます。絶対パスを渡してください。リース、ジャーナル、コンテンツカタログは `Path.GetFullPath` で正規化します。 | `.` または空 = `OmsiLaunch.exe` を含むディレクトリであり、呼び出し元の作業フォルダーになることはありません。明示的なインストール引数は spec より優先されます。結果は絶対パスに変換されます。 |
| `Presentation.CustomAssetDirectory` | 絶対パス、または `Installation.RootPath` からの相対パス。存在している必要があります。 | 同じです（`/splash-assets`）。セッションプロファイルの `assets` パスはプロファイルパッケージ内に制限され、絶対パスとして格納されます。 |
| `InternetTextures.OverrideProfilePath` | `Path.GetFullPath` で解決されます。つまり、インストールルートではなくプロセスの作業ディレクトリが基準です。存在している必要があります。 | 同じです（`/internet-textures-profile`）。セッションプロファイルの `profile` パスはパッケージ内に制限され、絶対パスとして格納されます。 |
| ITX のターゲット行 | インストールルートからの相対パス。`Texture\` コンポーネントを含む必要があります。ルートなし、`..` なし、先頭の `\` なし、ジャンクション/シンボリックリンクのコンポーネントなし。 | 同じです。 |
| コンテンツ識別子（`MapIdentity`、`SituationIdentity`、`PlayerVehicle.*`） | インストール環境からの相対パスで、大文字と小文字を区別せず、`/` を受け付けます。絶対パスにはなりません。 | 同じです。 |

<a id="carried-but-not-applied"></a>
## 保持されるが適用されないもの

| フィールド | 現在の効果 | 安定性 |
| --- | --- | --- |
| `Diagnostics.Log`、`Verbose`、`OmsiLogAll`、`ProcessTrace` | なし（ホストトレースは常に書き込まれます） | `PARTIAL` |
| `PlayerVehicle.*`（いずれかのフィールドが設定） | 診断のためにコンテンツは解決されるが、プランは実行不可（`UNAVAILABLE`） | `PARTIAL` |
| `SessionProfile` | 出自の診断のみ | `STABLE_BETA` |
