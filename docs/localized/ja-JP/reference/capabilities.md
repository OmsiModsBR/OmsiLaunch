# ケイパビリティ

<!-- l10n: source=reference/capabilities.md source-sha256=e6d9d063c2365c007efc79c0057a4843d1f91b88ba68448cc8b8d7579308de23 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/capabilities.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、OmsiLaunch で何ができるかを示します。その情報源は正規のケイパビリティカタログ `PublicCapabilityRegistry`（`src/OmsiLaunch.Api/PublicCapabilityRegistry.cs`）です。すべてのケイパビリティ、引数・CLI ルート・ネイティブケイパビリティを持つすべての公開ランタイム操作、およびケイパビリティごとの 1 つのエビデンス状態が、そこに一度だけ宣言されています。ランタイム操作の集合、引数の検証、CLI コマンドルート、プラグインの操作ごとのネイティブゲート、`GetCapabilitiesAsync`、および以下の[カタログ表](#capability-catalog)は、そこから導出されます。各ランタイム操作について、このページは引数、結果キー、エラーを示します。操作の呼び出し方法（CLI ルート、API エンベロープ、タイムアウト、ハンドル）は[ランタイム制御](runtime-control.md)に、セッションごとのエビデンスは[ランタイム検証の状況](../status/runtime-validation-status.md)に記載しています。

<a id="classification-and-stability"></a>
## 分類と安定性

`PublicCapabilityClassification` には 4 つの値があります。これらは次のように安定性の用語に対応し、エビデンスが不完全な箇所では格下げされます。

| 分類 | 意味 | 安定性 |
| --- | --- | --- |
| `PublicStableBeta` | 公開、Beta の契約に含まれる、ランタイム検証済み | `STABLE_BETA`（注記のある箇所では `PARTIAL` に格下げ） |
| `PublicExperimental` | 公開、変更される可能性あり、ランタイム検証済みまたは静的検証済み | `EXPERIMENTAL`（注記のある箇所では `PARTIAL` に格下げ） |
| `InternalOnly` | 調査用のプリミティブ。公開 API や CLI からは到達できません | `INTERNAL` |
| `Unsupported` | 診断のために認識されますが、拒否されるか存在しません | `UNAVAILABLE` |

`PublicCapabilityKind` は `Read`、`Write`、`Action`、`Event` を区別します。すべてのランタイムケイパビリティには `Running` セッション（レジストリの `RequiresSession`）が必要で、そのネイティブケイパビリティが実行中のビルドで解決され検証されていなければなりません。ネイティブケイパビリティが利用できない操作は `OL_E_CAPABILITY_UNRESOLVED` で拒否されます。`RequiresExactProfile` は、ネイティブターゲットに依存するケイパビリティを示します（[互換性](compatibility.md)を参照）。

ランタイムの結果は文字列の辞書です。`internal_` で始まるキー、または `_address`、`_pointer`、`_vmt` で終わるキーは API 境界で削除され（`ScrubInternalValues`）、ここには記載していません。以下の操作表の結果キーは、製品が返す正確なキーの集合です。これらは実際のセッションですべての公開操作を実行して取得したものです（ランタイムクロージャーのドキュメント用キャプチャ、セッション `f51dcb59-a723-4570-b6c5-b61e628a7993`、`situations\Linie 5.osn`。リストの行は `<row>.<n>.<field>` と表記します）。

操作の失敗の報告方法（`CurrentRuntimeControl.Execute`）:

| 失敗 | `ErrorCode` | `Values` |
| --- | --- | --- |
| レジストリによる拒否（不明な操作または `internal.*` 操作、必須引数の欠落） | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED` | なし |
| 製品による意図的な拒否（`weather.set` の導出キー） | 固有のコード（`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`） | `detail`（文） |
| D3D の失敗 | 固有の `OL_E_D3D_*` コード | `detail`、`native_status` |
| 型付きのプラグイン側の失敗（失効したハンドル、不明な名前、範囲外の値、プレイヤー車両がない、など） | 固有のコード。例: `OL_E_RUNTIME_OBJECT_HANDLE_STALE` または `OL_E_RUNTIME_VALUE_OUT_OF_RANGE` | `detail`（メッセージ）、`exception`（.NET の型名） |
| 実行中のビルドでネイティブケイパビリティが解決されていない操作 | `OL_E_CAPABILITY_UNRESOLVED` | `detail`、`capability`（ネイティブケイパビリティ） |
| コードを持たない、プラグイン側の予期しない例外 | `OL_E_RUNTIME_OPERATION_FAILED` | `detail`、`exception` |
| 64 KiB のメールボックスに収まらない結果 | `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（ページングされるリストは代わりに短縮され、`next_cursor` で続きを取得します。[ページネーション](#pagination)を参照） | なし |

各表の「エラー」列（`Errors`）には固有のコードを記載しています。これらは、プラグイン、ブリッジ、コントロールプレーン、API、CLI を通じて、変更されずに `ErrorCode` に届きます。

<a id="capability-catalog"></a>
## ケイパビリティカタログ

カタログのエントリごとに 1 行で、`PublicCapabilityRegistry.All` から生成されています（この表が古くなるとドキュメントゲートが失敗します）。`Session`（セッション）は、実行中のセッションを必要とするケイパビリティでは `Running` です。`Operations`（操作）は具体的なランタイム操作 id です（契約の行は[公開ランタイム操作](#public-runtime-operations)にあります）。`Native capability`（ネイティブケイパビリティ）は、実行中のビルドで解決され検証されていなければならない、ネイティブターゲットカタログのエントリ名です（[互換性](compatibility.md)を参照）。`Evidence`（エビデンス）は 1 つの語彙を使います。

| エビデンス | 意味 |
| --- | --- |
| RUNTIME_VALIDATED | 実際の OMSI セッションで観測済み。セッション id は[ランタイム検証の状況](../status/runtime-validation-status.md)にあります。 |
| STATICALLY_VALIDATED | 実装され、オフラインで検証済み。ランタイムでの効果は観測されていません。 |
| PARTIAL | 契約の一部について動作します。注記が未解決の部分を示します。 |
| RUNTIME_VALIDATION_REQUIRED | 実装済みで、ランタイム検証を待っています。 |
| UNAVAILABLE | 診断のために認識されて拒否される、または未実装。開始時に要求するとプランは実行不可になります。 |

何かを変更する操作は、その id の後ろに変更ポリシー（`PublicRuntimeOperation.MutationPolicy`）を示します。ジャーナルに記録されるランタイム操作はありません。

| 変更ポリシー | 意味 |
| --- | --- |
| `none` | 読み取りのみ（表示されません）。 |
| `omsi-state` | OMSI のメモリ内の状態を変更します。それを元に戻すものはなく、OMSI プロセスとともに終わります。 |
| `session-resource` | セッションが所有するリソースを作成または変更します。明示的に、またはセッションの終了時に解放されます。 |
| `session-policy` | 削除されるかセッションが終了するまで、プラグインが再適用し続けるポリシーを設定します。 |

<!-- generated:capability-catalog:begin -->
| ケイパビリティ | 分類 | 種類 | セッション | 操作 | ネイティブケイパビリティ | エビデンス | CLI | 注記 |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `session.plan` | `PublicStableBeta` | Action | 不要 |  |  | RUNTIME_VALIDATED | `/plan` |  |
| `session.start` | `PublicStableBeta` | Action | 不要 |  |  | RUNTIME_VALIDATED | `/new`, `/saved:<situation.osn>`, `/spec:<spec.json>` |  |
| `session.status` | `PublicStableBeta` | Read | Running |  |  | RUNTIME_VALIDATED | `session status` |  |
| `session.stop` | `PublicStableBeta` | Action | Running |  |  | RUNTIME_VALIDATED | `session stop` |  |
| `session.recover` | `PublicStableBeta` | Action | 不要 |  |  | RUNTIME_VALIDATED | `/recovery-status`, `/recover` |  |
| `events.read` | `PublicExperimental` | Event | Running |  |  | RUNTIME_VALIDATED | `events read`, `events watch` | Beta 4: プラグインのイベントは番号付きリングを通じて届きます。遅れた読み取り側は、件数を伴う telemetry.lost を受け取ります。 |
| `runtime.current-windows-x64` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED |  |  |
| `transaction.exact-restore` | `PublicStableBeta` | Action | 不要 |  |  | RUNTIME_VALIDATED |  |  |
| `omsi.omsi2` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED | `profiles` | Beta 4 ラウンド A: 既知のレイアウト。 |
| `boot.headless-start` | `PublicStableBeta` | Action | 不要 |  | `boot.headless-start` | RUNTIME_VALIDATED |  |  |
| `world.new-map` | `PublicStableBeta` | Action | 不要 |  | `world.new-map` | RUNTIME_VALIDATED | `/new /map:<identity> /entrypoint-index:<n>` |  |
| `world.presented-entrypoint` | `PublicStableBeta` | Action | 不要 |  | `world.new-map` | RUNTIME_VALIDATED | `/entrypoint-index:<n>` |  |
| `world.entrypoint-identity` | `PublicExperimental` | Action | 不要 |  | `world.new-map` | RUNTIME_VALIDATION_REQUIRED | `/entrypoint:<identity>` | Beta 4（BI-001）: OMSI は各正確なラベルの最初のレコードを提示し、起動はそのラベルによってそれを選択します。静的に確定済み。ランタイム検証は保留中です。 |
| `world.saved-situation` | `PublicStableBeta` | Action | 不要 |  | `world.saved-situation` | RUNTIME_VALIDATED | `/saved:<situation.osn>` |  |
| `world.last-map-state` | `PublicExperimental` | Action | 不要 |  | `world.last-map-state` | RUNTIME_VALIDATION_REQUIRED | `/last /map:<identity>` | Beta 4: ネイティブの分岐は静的に確定済み（BI-006）。ランタイム検証は保留中です。最新の .osn を探すヒューリスティックは決して使いません。 |
| `world.explicit-date` | `PublicExperimental` | Action | 不要 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/date:<yyyy-mm-dd>` | Beta 4: NewSituation の前に OMSI の開始クロックに書き込まれます（静的解析）。ランタイム検証は保留中です。 |
| `world.explicit-time` | `PublicExperimental` | Action | 不要 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/time:<hh:mm>` | Beta 4: NewSituation の前に OMSI の開始クロックに書き込まれます（静的解析）。ランタイム検証は保留中です。 |
| `world.explicit-year` | `PublicExperimental` | Action | 不要 |  | `world.start-date-time` | RUNTIME_VALIDATION_REQUIRED | `/year:<n>` | Beta 4: NewSituation の前に OMSI の開始クロックに書き込まれます（静的解析）。ランタイム検証は保留中です。 |
| `world.weather` | `PublicExperimental` | Action | 不要 |  | `world.start-weather` | RUNTIME_VALIDATION_REQUIRED | `/weather:<preset>`, `/weather-icao:<code>`, `/weather-real` | Beta 4（BI-003）: ワールドの開始直後に、OMSI 自身の loadWeather または METAR フラグを通じて適用されます。保存済みシチュエーションの天候より優先されます。静的に確定済み。ランタイム検証は保留中です。 |
| `player-vehicle.model` | `Unsupported` | Action | 不要 |  |  | UNAVAILABLE | `/vehicle:<identity>` | BI-007。診断のために解決されますが、適用はされません。 |
| `player-vehicle.repaint` | `Unsupported` | Action | 不要 |  |  | UNAVAILABLE | `/repaint:<id>` | BI-007。 |
| `player-vehicle.hof` | `Unsupported` | Action | 不要 |  |  | UNAVAILABLE | `/hof:<id>` | BI-007。 |
| `player-vehicle.fleet-number` | `Unsupported` | Action | 不要 |  |  | UNAVAILABLE | `/fleet:<n>` | BI-007。 |
| `player-vehicle.registration` | `Unsupported` | Action | 不要 |  |  | UNAVAILABLE | `/registration:<r>` | BI-007。 |
| `player.read` | `PublicStableBeta` | Read | Running | `player-vehicle.read` | `player.read` | RUNTIME_VALIDATED | `player get` | ヘッドレス開始では present=false、保存済みシチュエーションでは完全なスナップショット。 |
| `player.assign-headless` | `Unsupported` | Action | Running |  |  | UNAVAILABLE |  | BI-007: ネイティブの割り当て手順は静的に確定済み（research/reports/beta4-static/entrypoint-last-player-vehicle.md の 3 節）ですが、ランタイム検証の前には実装されません。後のベータで計画しています。 |
| `configuration.options.semantic` | `PublicStableBeta` | Write | 不要 |  |  | RUNTIME_VALIDATED | `/set:<key>=<value>` | RV-005。 |
| `input.keyboard` | `PublicExperimental` | Write | 不要 |  |  | STATICALLY_VALIDATED |  | Beta 4: トランザクションによるオーバーレイ。オフラインで検証済み。 |
| `input.controller` | `PublicExperimental` | Write | 不要 |  |  | STATICALLY_VALIDATED |  | Beta 4: トランザクションによるオーバーレイ。オフラインで検証済み。 |
| `presentation.splash` | `PublicStableBeta` | Write | 不要 |  |  | RUNTIME_VALIDATED | `/splash:Managed`, `/splash:Unset` |  |
| `internet-textures.disabled` | `PublicExperimental` | Action | 不要 |  | `internet-textures.disabled` | RUNTIME_VALIDATED | `/internet-textures:Disabled` | Beta 4 ラウンド B: 抑制が適用され、テレメトリリングを通じて報告されます（internet-textures.suppressed）。 |
| `internet-textures.override` | `PublicExperimental` | Write | 不要 |  |  | RUNTIME_VALIDATED | `/internet-textures:Override` | ランタイムクロージャー I01。 |
| `session-profiles` | `PublicStableBeta` | Action | 不要 |  |  | RUNTIME_VALIDATED | `/predefined-profile:<id> /predefined-profile-index:<n> /new`, `/list:SessionProfiles` | RA-009 プロファイルによる開始。1..99 とディスカバリはオフライン。 |
| `hof.management` | `PublicStableBeta` | Write | 不要 |  |  | RUNTIME_VALIDATED | `/hof-source:collection:<name> /hof-target:<vehicle>` | Beta 4 ラウンド A C01、C02。 |
| `hof.read` | `PublicStableBeta` | Read | Running | `vehicle.hofs.read` （ページング対象） | `hof.read` | RUNTIME_VALIDATED | `hof get` |  |
| `content.discovery` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED | `/list:<category>` |  |
| `content.map` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED |  |  |
| `content.situation-map` | `PublicStableBeta` | Read | 不要 |  |  | RUNTIME_VALIDATED |  |  |
| `content.vehicle` | `PublicStableBeta` | Read | 不要 |  |  | STATICALLY_VALIDATED |  |  |
| `content.repaint` | `PublicStableBeta` | Read | 不要 |  |  | STATICALLY_VALIDATED |  |  |
| `content.hof` | `PublicStableBeta` | Read | 不要 |  |  | STATICALLY_VALIDATED |  |  |
| `time.read` | `PublicStableBeta` | Read | Running | `time.read` | `time.read` | RUNTIME_VALIDATED | `time get` |  |
| `time.set` | `PublicExperimental` | Write | Running | `time.set` (omsi-state) | `time.set` | RUNTIME_VALIDATED | `time set` |  |
| `calendar.set-actual-date-time` | `Unsupported` | Write | Running |  |  | UNAVAILABLE |  | BI-002 は静的に確定済み: SetActualDateTime は PC のクロックを開始クロックにコピーするだけです。開始日は world.explicit-date を使用します。置き換え済み: ネイティブ ABI と事後条件は確定していません。 |
| `weather.read` | `PublicStableBeta` | Read | Running | `weather.read` | `weather.read` | RUNTIME_VALIDATED | `weather get` |  |
| `weather.set` | `PublicExperimental` | Write | Running | `weather.set` (omsi-state), `weather.metar.set` (omsi-state) | `weather.set`, `weather.metar.set` | RUNTIME_VALIDATION_REQUIRED | `weather set` | Beta 4: OMSI の天候ティックが導出元とするレコード actWeather（Weather+0x08）に書き込みます。静的に確定済み。導出される値（湿度、光の係数、実際の風）は読み取り専用のままです（OL_E_RUNTIME_SETTING_NOT_PERSISTENT）。ランタイム検証は保留中です。 |
| `weather.actual.read` | `PublicExperimental` | Read | Running | `weather.actual.read` | `weather.actual.read` | RUNTIME_VALIDATED | `weather actual get` |  |
| `map.read` | `PublicStableBeta` | Read | Running | `map.read`, `map.tiles.list` （ページング対象） | `map.read` | RUNTIME_VALIDATED | `map get` |  |
| `camera.read` | `PublicStableBeta` | Read | Running | `camera.read` | `camera.read` | RUNTIME_VALIDATED | `camera get` |  |
| `camera.set` | `PublicExperimental` | Write | Running | `camera.set` (omsi-state) | `camera.set` | RUNTIME_VALIDATED | `camera set` |  |
| `camera.lock` | `PublicExperimental` | Action | Running | `camera.lock` (session-policy), `camera.unlock` (session-policy) | `camera.lock` | RUNTIME_VALIDATED | `camera lock --family=<0..3> [--preset=<n>]` | PlayerVehicle が必要です（保存済みシチュエーションが提供します）。ヘッドレスの NEW_MAP では OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE。 |
| `ui.pause` | `PublicExperimental` | Action | Running | `ui.pause.state`, `ui.pause.take-over` (omsi-state), `ui.pause.restore-native` (omsi-state), `ui.pause.action` (omsi-state) | `ui.pause-menu.replace` | RUNTIME_VALIDATION_REQUIRED | `ui pause-state`, `ui pause-take-over`, `ui pause-restore`, `ui pause-action --action=<resume|new-game|save|options|quit>` | Beta 5: 静的に検証された設計（research beta4-static pause-menu-and-input.md）。ネイティブ呼び出しは実際のセッションではまだ観測されていません。 |
| `vehicles.list` | `PublicStableBeta` | Read | Running | `road-vehicles.list` （ページング対象） | `vehicles.list` | RUNTIME_VALIDATED | `vehicles list` |  |
| `vehicles.get` | `PublicStableBeta` | Read | Running | `road-vehicle.read` | `vehicles.get` | RUNTIME_VALIDATED | `vehicles get` | 自然な削除後の失効検出（RV-002）には安全なランタイム上の発生手段がなく、オフラインで検証されています。 |
| `vehicles.summary` | `PublicStableBeta` | Read | Running | `road-vehicles.read` | `vehicles.summary` | RUNTIME_VALIDATED | `vehicles summary` |  |
| `vehicles.spawn` | `PublicExperimental` | Action | Running | `road-vehicles.spawn` (omsi-state) | `vehicles.spawn` | RUNTIME_VALIDATED | `vehicles spawn --model=<Vehicles\...\*.bus>` |  |
| `vehicles.place-random` | `PublicExperimental` | Action | Running | `road-vehicles.place-random` (omsi-state) | `vehicles.place-random` | RUNTIME_VALIDATED | `vehicles place-random` |  |
| `internal.make-basic` | `InternalOnly` | Action | Running |  |  | RUNTIME_VALIDATED |  |  |
| `humans.list` | `PublicExperimental` | Read | Running | `humans.list` （ページング対象） | `humans.list` | RUNTIME_VALIDATED | `humans list` |  |
| `humans.get` | `PublicExperimental` | Read | Running | `human.read` | `humans.get` | RUNTIME_VALIDATED | `humans get` |  |
| `humans.summary` | `PublicExperimental` | Read | Running | `humans.read` | `humans.summary` | RUNTIME_VALIDATED | `humans summary` |  |
| `timetable.read` | `PublicStableBeta` | Read | Running | `timetable.read`, `timetable.tracks.list` （ページング対象）, `timetable.trips.list` （ページング対象）, `timetable.lines.list` （ページング対象）, `timetable.rv-files.list` （ページング対象）, `timetable.track-entries.list` （ページング対象）, `timetable.bus-stops.list` （ページング対象）, `timetable.station-links.list` （ページング対象）, `timetable.tours.list` （ページング対象）, `timetable.profiles.list` （ページング対象）, `timetable.tour-entries.list` （ページング対象）, `timetable.logs.read` （ページング対象）, `timetable.no-rv-numbers.list` （ページング対象） | `timetable.read` | RUNTIME_VALIDATED | `timetable get` |  |
| `scripts.numeric` | `PublicExperimental` | Write | Running | `vehicle.variables.list` （ページング対象）, `vehicle.variable.get`, `vehicle.variable.set` (omsi-state) | `scripts.numeric` | RUNTIME_VALIDATED | `scripts variable list`, `scripts variable get`, `scripts variable set` |  |
| `scripts.string.read` | `PublicExperimental` | Read | Running | `vehicle.string-variables.list` （ページング対象）, `vehicle.string-variable.get` | `scripts.string.read` | RUNTIME_VALIDATED | `scripts string list`, `scripts string get` | 書き込み: scripts.string.write。 |
| `scripts.string.write` | `PublicExperimental` | Write | Running | `vehicle.string-variable.set` (omsi-state) | `scripts.string.write` | RUNTIME_VALIDATION_REQUIRED | `scripts string set` | Beta 4（BI-004）: OMSI が定数形式の文字列をコピーします。静的に確定済み。ランタイム検証は保留中です。 |
| `scripts.trigger` | `PublicExperimental` | Action | Running | `vehicle.trigger` (omsi-state), `vehicle.sound-trigger` (omsi-state) | `scripts.trigger` | RUNTIME_VALIDATION_REQUIRED | `scripts trigger`, `scripts sound-trigger` | Beta 4（BI-004）: プロファイル済みの VMT スロットを通じた仮想の TriggerXML/SoundTrigger。静的に確定済み。ランタイム検証は保留中です。 |
| `constants` | `PublicStableBeta` | Read | Running | `vehicle.constants.list` （ページング対象）, `vehicle.constant.get` | `constants` | RUNTIME_VALIDATED | `constants list`, `constants get` |  |
| `curves` | `PublicStableBeta` | Read | Running | `vehicle.curves.list` （ページング対象）, `vehicle.curve.evaluate` | `curves` | RUNTIME_VALIDATED | `curves list`, `curves evaluate` |  |
| `drivers.read` | `PublicExperimental` | Read | Running | `drivers.read` （ページング対象） | `drivers.read` | RUNTIME_VALIDATED | `drivers list` |  |
| `tickets.read` | `PublicExperimental` | Read | Running | `tickets.read` （ページング対象） | `tickets.read` | RUNTIME_VALIDATED | `tickets get` |  |
| `d3d.texture` | `PublicExperimental` | Action | Running | `d3d.status`, `d3d.texture.create` (session-resource), `d3d.texture.describe`, `d3d.texture.update` (session-resource), `d3d.texture.release` (session-resource) | `d3d.texture` | RUNTIME_VALIDATED | `/runtime:d3d.status`, `/runtime:d3d.texture.create`, `/runtime:d3d.texture.describe`, `/runtime:d3d.texture.update`, `/runtime:d3d.texture.release` | デバイスリセットを観測（resetting、restored、世代の無効化）。独立した lost 遷移は発生しませんでした（RV-007）。 |
<!-- generated:capability-catalog:end -->

`internal.make-basic` は `InternalOnly` です。調査用にプラグイン内に存在し（ネイティブアドレスを返します）、API 境界とローカルコントロールプレーンで拒否されます。どちらも最初に操作名を `PublicRuntimeOperationIds` と照合して検証します。CLI にはこの操作のルートがありません。`session.stop` は OMSI を終了させます。協調的なシャットダウンは実装されていません。

<a id="pagination"></a>
## ページネーション

すべてのリスト操作（[ケイパビリティカタログ](#capability-catalog)で「（ページング対象）」と表示されているもの）は、2 つの省略可能な引数を受け取り、4 つのページフィールドを返します。

| 引数 | 意味 |
| --- | --- |
| `cursor` | ページの開始位置: 前のページの `next_cursor`。省略した場合: 最初のページ。別のリストのカーソル、または形式が不正なカーソルは `OL_E_RUNTIME_CURSOR_INVALID` です。 |
| `limit` | 要求する行数（正の整数）。リストの最大ページサイズより大きい場合: 最大値が使用されます。正の整数でない場合: `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`。 |

| 結果キー | 意味 |
| --- | --- |
| `count` | OMSI が保持するすべてのレコード。 |
| `returned_count` | この応答に含まれる行数。 |
| `truncated` | このページの後にレコードが続く場合は `true`。 |
| `next_cursor` | レコードが続く場合にのみ存在します。次のページの `cursor` として渡してください。 |

カーソルは位置ではなく、安定したキーです。レコード配列（時刻表、スクリプト名、定数、カーブ、HOF、運転士、乗車券、ログ）では、行はレコードの絶対インデックスによって `<row>.<n>.<field>`（または `name.<n>`）のキーが付けられ、カーソルは次のインデックスを指します。そのため、最初のページは Beta 3 の応答と同じになります。ハンドルのコレクション（`road-vehicles.list`、`humans.list`）では、行はハンドルの順序で返され、カーソルは最後に返されたハンドルを指します。2 つのページの間に出現したオブジェクトにはより後のハンドルが与えられ、後のページに現れます。繰り返されるものはありません。カーソルの値は不透明なものとして扱ってください。

最大ページサイズ: `road-vehicles.list` と `humans.list` は 1000、`timetable.track-entries.list`、`vehicle.variables.list`、`vehicle.string-variables.list`、`vehicle.constants.list`、`vehicle.curves.list` は 512、`timetable.tour-entries.list`、`timetable.logs.read`、`vehicle.hofs.read` は 256、その他の時刻表リスト、`drivers.read`、`tickets.read` は 128 です。1 ページの行がなお 64 KiB のメールボックスを超える場合、プラグインは応答が収まるまで末尾の行を削除し、`next_cursor` は最初に削除された行から続きます。そのため、サイズが原因で失敗するリストはありません。

<a id="public-runtime-operations"></a>
## 公開ランタイム操作

以下は、公開フロントエンドが転送してよい操作 id の正確な一覧です（`PublicCapabilityRegistry.PublicRuntimeOperationIds`）。いずれも `SessionState.Running` を必要とし、ジャーナルへの記録も復元も行われません。必須引数はメールボックスを使用する前にレジストリによって検査されます（`OL_E_RUNTIME_ARGUMENT_REQUIRED`）。省略可能な引数はプラグインが検証します。すべての操作に共通する失敗コード: `OL_E_RUNTIME_OPERATION_UNKNOWN`（この一覧にない）、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_RUNTIME_CHANNEL_BUSY`、`OL_E_RUNTIME_CHANNEL_CLOSED`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_REQUEST_ID_REUSED`、`OL_E_RUNTIME_RESPONSE_INVALID`、`OL_E_RUNTIME_RESPONSE_TOO_LARGE`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`（プラグインに実装がない）、`OL_E_RUNTIME_OPERATION_FAILED`（プロセス内の予期しない例外。`detail` と `exception` の値を伴います）。

<a id="time"></a>
### 時刻

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `time.read` | Read | なし | `hour`、`minute`、`second`、`day`、`month`、`year` | |
| `time.set` | Write | 省略可能な `hour`（0..23）、`minute`（0..59）、`second`（0..59.999、小数）。少なくとも 1 つ | プロファイル済みの `SetTime` 実行後の `time.read` のキー | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_TIME_APPLY_FAILED` |

<a id="weather"></a>
### 天候

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `weather.read` | Read | なし | `fog_density`、`lightness`、`primary_light_factor`、`secondary_light_factor`、`ambient_light_factor`、`cloud_type`、`cloud_transparency`、`precipitation_set`、`wet_ground`、`wind_speed`、`wind_direction`、`relative_humidity`、`absolute_humidity`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`、およびソース値 `set.fog_density`、`set.lightness`、`cloud_type_name`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads`。`fog_density`、`lightness`、`wind_speed`、`wind_direction`、光の係数、湿度は OMSI のティックごとの値で、`cloud_type` は解決済みの雲の種類の番号です | |
| `weather.set` | Write | `fog_density`、`lightness`、`wind_speed`、`wind_direction`、`temperature`、`dew_point`、`pressure`、`precipitation`、`precipitation_rate`、`cloud_type`、`cloud_height`、`ground_moisture`、`ground_moisture_cover`、`ground_wetness`、`snow`、`snow_on_roads` の少なくとも 1 つ。範囲: 霧 0..100000、lightness と地面の値 0..1、風速 0..200、風向 0..360、気温と露点 -60..60、気圧 850..1100、降水 0..2、レート 0..255、雲の高さ 0..20000、`cloud_type` はコードページ 1252 の 1..64 文字（`-1` = 雲なし）、ブール値は `true`/`false`/`1`/`0` | `weather.read` のキー。`cloud_type` を指定した場合は `cloud_type_resolved` も含まれます（OMSI がその雲の種類を知らない場合は `false`。何も描画されません） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（導出キー: `relative_humidity`、`absolute_humidity`、光の係数、`cloud_transparency`、`precipitation_set`、`wet_ground`。detail が代わりに設定すべきキーを示します）、`OL_E_RUNTIME_SETTING_UNAVAILABLE`（未知のキー）、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.metar.set` | Write | `icao`（4 文字の英字または数字） | `weather.actual.read` のキー | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `weather.actual.read` | Read | なし | `active`、`icao`、`last_downloaded`、`invalid_icao`、`counter`、`process` | |

<a id="map"></a>
### マップ

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `map.read` | Read | なし | `loaded`、`loaded_tiles`、`left_hand_traffic`、`name`、`filename`、`friendly_name`、`description`、`max_speed`、`year_start`、`year_end` | |

<a id="camera"></a>
### カメラ

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `camera.read` | Read | なし | `family`（0 運転士、1 乗客、2 外部、3 マップ）、`name`、`field_of_view`、`normal_field_of_view`、`distance` | |
| `camera.set` | Write | 省略可能な `family`（0..3）、`field_of_view`（10..170）。少なくとも 1 つ | `camera.read` のキー | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE` |
| `camera.lock` | Action | 必須の `family`（0..3）。省略可能な `preset`（0..255、ファミリー 0 または 1 の場合のみ） | `locked`（`true`）、`family`、`preset`、`head_look`（`preserved`）、`field_of_view`（`preserved`） | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`。このポリシーは `camera.unlock` まで 100 ms ごとに再適用されます。再適用が失敗すると、異なるエラーごとに 1 回ずつイベント `camera.lock.degraded` が発行されます。 |
| `camera.unlock` | Action | なし | `locked`（`false`） | |

<a id="user-interface-pause-menu"></a>
### ユーザーインターフェイス（ポーズメニュー）

モジュール独自のメニューのために、OMSI のネイティブポーズメニューを引き継ぎます（ケイパビリティ `ui.pause-menu.replace`。既知のコード ID でのみ解決されます）。OMSI 自身のポーズバイトは設定されたままなので、シミュレーション、レンダリング、3D クリック、DirectInput コマンドは保持されたままです。ネイティブフォームは VCL を通じて非表示になり、そのアクションは直接呼び出されます（クリックなし、投稿メッセージなし、移動したウィンドウなし）。`ui.pause.action` は、ハンドラーがネイティブのモーダルダイアログに入ることがあるため、応答を送信した後に、1 回限りの UI スレッドタイマーからネイティブハンドラーを実行します。結果は `ui.pause.state` から読み取ってください（`busy` が `false` になり、その後 `last_result`）。

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `ui.pause.state` | Read | なし | `paused`、`native_menu_visible`、`main_form_ready`、`in_frame`、`taken_over`、`busy`、`pending_action`、`last_action`、`last_result`（`none`、`resumed`、`game-started`、`returned`、`quit-requested`、`exception`、`refused`） | `OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.take-over` | Action | なし | 引き継ぎ後の `ui.pause.state` のキー | `OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_UI_ALREADY_TAKEN_OVER`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.restore-native` | Action | なし | `ui.pause.state` のキー。ネイティブメニューを再び表示します（その `FormShow` が実行されます。OMSI は一時停止のままです） | `OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |
| `ui.pause.action` | Action | 必須の `action`: `resume`、`new-game`、`save`、`options` または `quit` | `ui.pause.state` のキーに加えて `scheduled`（`true`）と `action` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_UI_NOT_PAUSED`、`OL_E_UI_BUSY`、`OL_E_RUNTIME_OPERATION_FAILED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_CAPABILITY_UNRESOLVED` |

<a id="road-vehicles-and-player"></a>
### 道路車両とプレイヤー

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `road-vehicles.read` | Read | なし | `count`、`ai_collection`、`player_index`（OMSI の生のプレイヤー車両インデックスを読み取ったまま報告します。プレイヤー車両が存在するかどうかは `player-vehicle.read` の `present` で確認してください） | |
| `road-vehicles.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`vehicle.<n>.handle`（`rv-NNNNNN`。`<n>` は OMSI のリスト内の位置） | `OL_E_RUNTIME_CURSOR_INVALID` |
| `road-vehicle.read` | Read | 必須の `handle` | `handle`、`runtime_index`、`tile`、`marked_for_killing`、`traffic_type`、`position_x`、`position_y`、`position_z`、`rotation_x`、`rotation_y`、`rotation_z`、`rotation_w`、`steering`、`tacho`、`ground_speed`、`kilometres`、`throttle`、`brake`、`clutch`、`ai_enabled`、`ai_mode`、`ai_light`、`ai_interior_light`、`ai_indicator_left`、`ai_indicator_right`、`ai_brake_light` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |
| `player-vehicle.read` | Read | なし | `present`（OMSI にプレイヤー車両がない場合は `false`）。`true` の場合: `player_index` とすべての `road-vehicle.read` のキー | |
| `road-vehicles.spawn` | Action | 必須の `model`（正規形の `Vehicles\...\*.bus`、240 文字以内、`..` を含まない、インストール環境の配下に存在すること） | `bus`、`created_handle`、`before_count`、`after_count`、`delta_count`、`raw_native_return`、`identity_validation` | `OL_E_RUNTIME_ARGUMENT_REQUIRED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`。PlayerVehicle は割り当てません。数秒かかるため、クライアントのタイムアウトは 30 s を使用してください。 |
| `road-vehicles.place-random` | Action | 省略可能な `ai_type`（0..255、既定値 0）、`group`（0..65535、既定値 1）、`type`（-1..65535、既定値 -1）、`scheduled`（0..1、既定値 0）、`tour`（0..65535、既定値 0）、`line`（0..65535、既定値 0） | `raw_return`、`before_count`、`after_count`、`delta_count`、`identity_validation`（`native-placement-return-is-diagnostic-only`） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_PLACE_RANDOM_BUS_FAILED` |

<a id="humans"></a>
### 人物オブジェクト

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `humans.read` | Read | なし | `count` | |
| `humans.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`human.<n>.handle`（`hb-NNNNNN`。`<n>` は OMSI のリスト内の位置） | `OL_E_RUNTIME_CURSOR_INVALID` |
| `human.read` | Read | 必須の `handle` | `handle`、`runtime_index`、`human_index`、`tile`、`marked_for_killing`、`collision_type`、`position_x`、`position_y`、`position_z`、`target_x`、`target_y`、`target_z`、`target_station`、`pre_target_station`、`departure`、`enter_bus_at`、`seat_bus`、`seat_station`、`ticket_type`、`ticket_index`、`ticket_ready`、`state`、`speed`、`bus_index`、`station`、`ai_mode`、`ai_mode_ex`、`ai_sub_mode`、`collision_state` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` |

<a id="timetable-drivers-tickets"></a>
### 時刻表、運転士、乗車券

時刻表のリスト、`drivers.read`、`tickets.read` はページングされます（[ページネーション](#pagination)を参照）。以下の「ページフィールド」は `count`、`returned_count`、`truncated`、`next_cursor` を意味し、すべてのリストは省略可能な `cursor` と `limit` を受け取ります。

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `timetable.read` | Read | なし | `invalid`（`true`/`false`）、およびレコード数 `tracks`、`trips`、`bus_stops`、`station_links`、`lines`、`rv_files` | |
| `timetable.tracks.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`track.<n>.filename`、`track.<n>.path`、`track.<n>.entries`、`track.<n>.length` | |
| `timetable.trips.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`trip.<n>.filename`、`trip.<n>.chrono_origin`、`trip.<n>.target`、`trip.<n>.line`、`trip.<n>.track_index`、`trip.<n>.track_name`、`trip.<n>.bus_stops`、`trip.<n>.profiles`、`trip.<n>.train_reverse`、`trip.<n>.invalid` | |
| `timetable.lines.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`line.<n>.name`、`line.<n>.chrono_origin`、`line.<n>.priority`、`line.<n>.tours`、`line.<n>.user_allowed` | |
| `timetable.rv-files.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`rv_file.<n>.line`、`rv_file.<n>.number_tours`、`rv_file.<n>.start_date_rel_2000`、`rv_file.<n>.end_date_rel_2000`、`rv_file.<n>.type_line_files`、`rv_file.<n>.type_line_probability`、`rv_file.<n>.type_tours` | |
| `timetable.track-entries.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`track_entry.<n>.track_index`、`track_entry.<n>.id`、`track_entry.<n>.path_index_on_object`、`track_entry.<n>.path_tile`、`track_entry.<n>.path_index`、`track_entry.<n>.relative_distance`、`track_entry.<n>.distance`、`track_entry.<n>.valid`、`track_entry.<n>.path_order_check`、`track_entry.<n>.allowed_fstrn`、`track_entry.<n>.chrono_origin`、`track_entry.<n>.bad_chronos` | |
| `timetable.bus-stops.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`bus_stop.<n>.name`、`bus_stop.<n>.supplement`、`bus_stop.<n>.tile`、`bus_stop.<n>.id`、`bus_stop.<n>.parent_id`、`bus_stop.<n>.preset_alighting`、`bus_stop.<n>.index`、`bus_stop.<n>.starting_links`、`bus_stop.<n>.ending_links`、`bus_stop.<n>.chrono_origin` | |
| `timetable.station-links.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`station_link.<n>.length`、`station_link.<n>.start_bus_stop_id`、`station_link.<n>.end_bus_stop_id`、`station_link.<n>.start_bus_stop`、`station_link.<n>.end_bus_stop`、`station_link.<n>.chrono_origin`、`station_link.<n>.valid`、`station_link.<n>.visible`、`station_link.<n>.track_entries`、`station_link.<n>.start_track_entry`、`station_link.<n>.end_track_entry` | |
| `timetable.tours.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`tour.<n>.line_index`、`tour.<n>.name`、`tour.<n>.ai_group`、`tour.<n>.ai_group_index`、`tour.<n>.ai_type`、`tour.<n>.completed_day`、`tour.<n>.entries`、`tour.<n>.has_normal_vehicle`、`tour.<n>.invalid`、`tour.<n>.vehicle_indices`、`tour.<n>.vehicle_reservations` | |
| `timetable.profiles.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`profile.<n>.trip_index`、`profile.<n>.name`、`profile.<n>.service_trip`、`profile.<n>.stop_times`、`profile.<n>.total_time`、`profile.<n>.track_entry_times` | |
| `timetable.tour-entries.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`tour_entry.<n>.line_index`、`tour_entry.<n>.tour_index`、`tour_entry.<n>.trip`、`tour_entry.<n>.trip_index`、`tour_entry.<n>.profile_index`、`tour_entry.<n>.start_time`、`tour_entry.<n>.end_time`、`tour_entry.<n>.smooth_transition` | |
| `timetable.logs.read` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`log.<n>.bus_stop`、`log.<n>.estimated_arrival`、`log.<n>.estimated_departure`、`log.<n>.actual_arrival`、`log.<n>.actual_departure`、`log.<n>.arrival_ok`、`log.<n>.departure_ok` | |
| `timetable.no-rv-numbers.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`no_rv.<n>.group`、`no_rv.<n>.number`: OMSI が自由な AI の使用から除外している車両番号（OMSI の予約番号の文字列リスト。OMSI 自身のレイアウトを通じて読み取ります。リストが期待される形でない場合は `OL_E_RUNTIME_VALUE_INVALID`）。1 ページあたり最大 256。静的解析によるもので、ランタイム検証は保留中です。 |
| `map.tiles.list` | Read | 省略可能な `cursor`、`limit` | ページフィールド。`tile.<n>.x`、`tile.<n>.y`（グリッド位置）、`tile.<n>.tile_index`（マップのタイルリスト内でのそのタイル自身のインデックス）、`tile.<n>.filename`、`tile.<n>.version`、`tile.<n>.unsaved`、`tile.<n>.loaded`、`tile.<n>.load_requested`、`tile.<n>.failed`、`tile.<n>.thread_loading`、`tile.<n>.prepared_for_ode`、`tile.<n>.width_south`、`tile.<n>.width_north`、`tile.<n>.objects`、`tile.<n>.path_groups`、`tile.<n>.path_segments`、`tile.<n>.surface_objects`、`tile.<n>.spline_segments`。読み取れないタイルでは `tile.<n>.state` = `unavailable` と `tile.<n>.error`。1 ページあたり最大 256。位置とインデックスは静的解析によるもので、ランタイム検証は保留中です。 |
| `drivers.read` | Read | 省略可能な `cursor`、`limit` | `count`、`selected_index`。`driver.<n>.filename`、`driver.<n>.name`、`driver.<n>.gender`、`driver.<n>.bus_stops`、`driver.<n>.crashes`、`driver.<n>.passengers`、`driver.<n>.tickets`、`driver.<n>.cash` | |
| `tickets.read` | Read | 省略可能な `cursor`、`limit` | `filename`、`voice_path`、`stamper_factor`、`buy_factor`、`chattiness`、`whinge_factor`、`count`。`ticket.<n>.name`、`ticket.<n>.display_name`、`ticket.<n>.value`、`ticket.<n>.maximum_stations`、`ticket.<n>.day_ticket` | |

<a id="vehicle-scripts-constants-curves-hof-handle-scoped"></a>
### 車両スクリプト、定数、カーブ、HOF（ハンドル単位）

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `vehicle.variables.list` | Read | 必須の `handle`。省略可能な `cursor`、`limit` | `handle`、ページフィールド、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.variable.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE` |
| `vehicle.variable.set` | Write | 必須の `handle`、`name`、`value`（有限の浮動小数点数） | `handle`、`name`、`requested_value`、`value`（読み戻し値） | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND` |
| `vehicle.string-variables.list` | Read | 必須の `handle`。省略可能な `cursor`、`limit` | `handle`、ページフィールド、`name.<n>` | `OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE` |
| `vehicle.string-variable.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND` |
| `vehicle.string-variable.set` | Write | 必須の `handle`、`name`（大文字と小文字を区別し、スクリプトが宣言したとおり、128 文字以内）、`value`（1..1024 文字） | `handle`、`name`、`value`（書き込み後に読み戻した値） | `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（OMSI 自身は未知の名前を黙って無視します）、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.trigger` | Action | 必須の `handle`、`name`（コードページ 1252、128 文字以内）、`value`（`1` は `name` を、`0` は `name_off` を、`2` は `name_drag` を実行） | `handle`、`name`、`value`、`dispatched` | `OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE`。スクリプトのエントリは同期的に実行されます。スクリプトが定義していない名前では何も起こりません。押下の後には通常、解放が続きます |
| `vehicle.sound-trigger` | Action | 必須の `handle`、`trigger`（コードページ 1252、128 文字以内）。省略可能な `file`（260 文字以内） | `handle`、`trigger`、`file`、`dispatched` | `OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`（車両にサウンドパックがない場合も）、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_CAPABILITY_UNRESOLVED`、`OL_E_RUNTIME_OPERATION_UNAVAILABLE` |
| `vehicle.constants.list` | Read | 必須の `handle`。省略可能な `cursor`、`limit` | `handle`、ページフィールド、`name.<n>` | `OL_E_RUNTIME_CONSTANTS_UNAVAILABLE` |
| `vehicle.constant.get` | Read | 必須の `handle`、`name` | `handle`、`name`、`value` | `OL_E_RUNTIME_CONSTANT_NOT_FOUND` |
| `vehicle.curves.list` | Read | 必須の `handle`。省略可能な `cursor`、`limit` | `handle`、ページフィールド、`name.<n>` | |
| `vehicle.curve.evaluate` | Read | 必須の `handle`、`name`、`x`（有限の浮動小数点数。`x` が欠落しているか数値でない場合は `OL_E_RUNTIME_ARGUMENT_REQUIRED`） | `handle`、`name`、`x`、`value`（線形補間） | `OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_CURVE_DEGENERATE` |
| `vehicle.hofs.read` | Read | 必須の `handle`。省略可能な `cursor`、`limit` | `handle`、ページフィールド、`hof.<n>.name`、`hof.<n>.service_trip` | `OL_E_RUNTIME_HOF_UNAVAILABLE` |

### Direct3D 9

D3D 操作は、ネイティブブリッジを介して OMSI のメイン/レンダースレッド上で実行されます。テクスチャハンドルは `d3dtex-<sessionId N>-<16 hex digits>` の形式です。

| 操作 | 種類 | 引数 | 結果キー | エラー |
| --- | --- | --- | --- | --- |
| `d3d.status` | Read | なし | `available`、`native_status`、`query_interface_hresult`、`cooperative_level_hresult`、`execution_thread_id`、`owned_device_references`、`state`（`NOT_READY`、`READY`、`LOST`、`RESETTING`、`STOPPING`、`STOPPED`）、`transition`、`generation`、`live_textures`、`reset_hook_installed`、`last_reset_thread_id`、`binding` | |
| `d3d.texture.create` | Action | 必須の `width`（1..4096）、`height`（1..4096）、`format`（`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`）。省略可能な `levels`（0..16、既定値 1） | `handle`、`state`（`LIVE`、`RELEASED`、`STALE`）、`device_state`、`generation`、`width`、`height`、`format`、`levels`、`level`、`level_width`、`level_height`、`hresult`、`execution_thread_id` | `OL_E_D3D_INVALID_ARGUMENT`、`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST`、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NATIVE_CALL_FAILED` |
| `d3d.texture.describe` | Read | 必須の `handle`。省略可能な `level`（0..15、既定値 0） | 要求したレベルについての `d3d.texture.create` の 13 個のキー | `OL_E_D3D_STALE_RESOURCE_HANDLE`、`OL_E_D3D_RESOURCE_RELEASED`、および create のエラー |
| `d3d.texture.update` | Action | 必須の `handle`、`width`（1..4096）、`height`（1..4096）、`pixels_base64`（デコード後 48 KiB 以内）。省略可能な `level`（0..15、既定値 0）、`x`（0..4095、既定値 0）、`y`（0..4095、既定値 0） | 更新後の `d3d.texture.create` の 13 個のキー | `OL_E_D3D_INVALID_PIXEL_BUFFER`、および describe のエラー |
| `d3d.texture.release` | Action | 必須の `handle` | `state` = `RELEASED` となった `d3d.texture.create` の 13 個のキー | 繰り返し解放した場合は `OL_E_D3D_RESOURCE_RELEASED`、`OL_E_D3D_STALE_RESOURCE_HANDLE` |

失敗した D3D 操作は `detail` と `native_status` の値を返します。`D3DRuntimeApi` 拡張メソッド（`GetD3DStatusAsync`、`CreateD3DTextureAsync`、`DescribeD3DTextureAsync`、`UpdateD3DTextureAsync`、`ReleaseD3DTextureAsync`）は、API 利用者向けにこれらの操作をラップしています。

## `GetCapabilitiesAsync`

`IOmsiLaunch.GetCapabilitiesAsync(InstallationSpec)` は、`InternalOnly` ではないカタログエントリごとに 1 つの `Capability(Name, Available, EvidenceState, Reason)` を返します。`Name` はカタログ id、`EvidenceState` はそのカタログのエビデンスです。`Available` はインストール環境に対して評価されます。

- `Unsupported` のエントリは決して利用可能になりません。`Reason` にカタログの注記が入ります。
- すべてのエントリに現在のプラットフォームが必要で、`runtime.current-windows-x64` を除くすべてのエントリに OMSI 2 のインストール環境が必要です。
- ネイティブケイパビリティを持つエントリは、既知のレイアウトでは `native: RESOLVED_KNOWN_LAYOUT` と報告します。別の OMSI 2 ビルドでは実行ファイルが静的に解決され、`native: STATICALLY_RESOLVED`、またはファイルが解決しなかったものを示す `native: PENDING_IN_PROCESS_RESOLUTION` となります。保留中のエントリも、プロセス内の解決が決定するため、利用可能として報告されます。失敗した場合は、操作または開始が `OL_E_CAPABILITY_UNRESOLVED` で失敗します。

プランナーは、プランごとのケイパビリティを、同じカタログ id で `SessionPlan.RequiredCapabilities` と `SessionPlan.UnsupportedRequestedFeatures` に報告します（`runtime.current-windows-x64`、`transaction.exact-restore`、`omsi.omsi2`、`world.*`、`boot.headless-start`、`internet-textures.disabled`、`player-vehicle.*`、`input.*`、`content.*`）。要求されたが非対応の機能は、そのカタログのエビデンスを持ちます。必須のネイティブケイパビリティは `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED`、または `PENDING_IN_PROCESS_RESOLUTION` を持ちます。

<a id="known-limitations"></a>
## 既知の制限事項

- ハンドルはセッション単位で、そのオブジェクトがなくなると `OL_E_RUNTIME_OBJECT_HANDLE_STALE` として報告されます。車両ハンドルは、その VMT、定義、および OMSI の `IDCode`（構築時にすべてのマップオブジェクトに付与される、プロセス内で一意で再利用されない番号）に結び付けられるため、同じアドレスに再作成されたオブジェクトは常に区別されます。人物オブジェクトはマップセッション全体にわたって存在するプール済みのスロットです。人物ハンドルは、その人物が消滅した場合（`RenderMe` 0）、またはスロットが再びスポーンされた場合（新しい目標停留所または出発）に解決されなくなります。
- 結果は 64 KiB のメールボックスに制限されます。すべてのリストはページングされます（[ページネーション](#pagination)を参照）。
- タイルをまたいだ車両の移動、カレンダーの書き込み、ヘッドレスでの PlayerVehicle の割り当てはいずれもできません。文字列変数の書き込み、スクリプトトリガー、天候の書き込みは `EXPERIMENTAL` です（ランタイム検証は保留中）。[既知の制限事項](known-limitations.md)を参照してください。
