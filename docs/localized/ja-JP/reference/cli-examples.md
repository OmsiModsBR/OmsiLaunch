# CLI の使用例

<!-- l10n: source=reference/cli-examples.md source-sha256=0b65dcecbe06ebef6e07dfc05ef1c1b5b95788ef6a892ebb05b51509ebb86733 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/cli-examples.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OmsiLaunch `0.1.0-beta4` 向けの、最小限かつ正しい `OmsiLaunch.exe` の呼び出し例です。各例には、期待されるプロセスの終了コードと、何が変更され何が復元されるかの注記を付けています。特に断りがない限り、すべての例は OMSI のインストールルート（`<OMSI_PATH>`）から実行します。構文は [CLI リファレンス](cli.md)で、終了コードは[終了コード](exit-codes.md)で定義されています。構造化されたエンベロープを得るには、任意のコマンドに `--json` を追加できます。

<a id="conventions"></a>
## 表記規則

- **変更対象**: コマンドによって変更されるファイルまたは OMSI の状態です。「セッションオーバーレイ」とは、トランザクションにスナップショットが取られ、OMSI の起動前に適用され、セッション終了時にバイト単位で正確に復元されるファイルを意味します。
- **復元対象**: セッション終了時（通常の停止、Ctrl+C、トレイ、`session stop`、`/observe-seconds`）またはリカバリによって元に戻されるものです。
- ランタイムの書き込み（`time set`、`camera set`、`scripts variable set`、`vehicles spawn`）は OMSI のメモリのみを変更します。停止時に OMSI は強制終了されるため、これらが元に戻されることはありません。
- プレースホルダー: `<OMSI_PATH>` は OmsiLaunch パッケージを含む OMSI 2 のインストール環境です（例: `C:\OMSI 2`）。`<OTHER_OMSI_PATH>` は別のインストール環境、`<SPEC_PATH>` と `<ITX_PATH>` は利用者自身の LaunchSpec ファイルと Internet Textures プロファイルです。`<HANDLE>` は直前の `list` または `create` コマンドが出力したハンドル、`<BASE64>` は Base64 のピクセルデータです。それ以外の値はすべて、標準的な OMSI 2 インストール環境でそのまま動作するリテラルです（`grundorf-quick` はこのページで定義している例のプロファイルです）。
- スペースを含むパスは引用符で囲み、引用符で囲んだパスを `\` で終わらせないでください（Windows の引数解析は `\"` をリテラルの引用符として扱います）。`"C:\OMSI 2"` とし、`"C:\OMSI 2\"` とはしないでください。
- このページのすべてのコマンドラインは、ドキュメントゲート（`tests/OmsiLaunch.DocumentationTests`、ゲート `examples`）によって解析されます。識別、検出、プランニング、クライアントの各例は、実際のインストール環境に対しても実行されています（`research/reports/OMSILAUNCH-BETA3-FINAL-DOCUMENTATION-AUDIT.md`）。

<a id="identity-and-discovery-no-session"></a>
## 識別と検出（セッションなし）

```text
OmsiLaunch.exe /version
```
終了コード `0`。`product`、`version`（`0.1.0-beta4`）、`protocol_version`（`0.1`）、`supported_family` を出力します。何も変更しません。

```text
OmsiLaunch.exe profiles --json
```
終了コード `0`。サポートされる `Omsi.exe` のハッシュとその検証ステータスを一覧表示します。何も変更しません。

```text
OmsiLaunch.exe capabilities --json
OmsiLaunch.exe help time
```
終了コード `0`。公開ケイパビリティのカタログです。`help <family>` で絞り込みます。何も変更しません。

```text
OmsiLaunch.exe detect
OmsiLaunch.exe
```
終了コード `0`（2 つの形式は同一です）。実行中の `Omsi.exe` プロセスと、このインストール環境に対して OmsiLaunch のオーナーが応答するかどうかを報告します。何も変更しません。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /list:Repaints /vehicle-scope:Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe "<OMSI_PATH>" /list:Situations
```
終了コード `0`（未知のカテゴリの場合は `2`）。読み取り専用の検出です。ジャンクションの循環はスキップされます。何も変更しません。ここで出力される `Identity` の値は、`/map`、`/saved`、`/vehicle-scope`、および `LaunchSpec` が期待する正確な文字列です（例: `maps\Grundorf\global.cfg`、`situations\Linie 5.osn`）。

<a id="planning-and-validation"></a>
## プランニングと検証

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
プランが `READY` の場合は終了コード `0`、`NOT RUNNABLE` の場合は `1` です（例: `OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`）。何も変更しません。OMSI は起動されません。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /validate --json
```
終了コードは上記と同じく `0`/`1` です。`/validate` は `/plan` の別名です。JSON は生の `SessionPlan`（`TouchedFiles`、`PlannedMutations`、`Diagnostics`、`IsRunnable`）です。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /date:2026-09-20 /plan
```
終了コード `1`。`/date`、`/time`、`/year`、`/weather*`、およびプレイヤー車両のフラグは受け付けられますが、このビルドでは適用されません。プランは `OL_E_CAPABILITY_UNAVAILABLE` を含み、実行不可です。

```text
OmsiLaunch.exe /last /plan
```
終了コード `1`。`LAST_MAP_STATE` はこのプロファイルでは利用不可です（`OL_E_CAPABILITY_UNAVAILABLE`）。

<a id="starting-sessions-owner-mode"></a>
## セッションの開始（オーナーモード）

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
セッションが `Completed` で終了した場合は終了コード `0`、`Failed` または実行不可のプランの場合は `1` です。変更対象: セッションオーバーレイ `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_<lang>.bmp`（マネージドスプラッシュがデフォルト）、`closecheck` の処理、起動時のハンドオフ。復元対象: セッション終了時に、すべてのオーバーレイがバイト単位で正確に復元されます。OMSI が終了するか、トレイの "End session" が確認されるか、クライアントが `session stop` を送信するか、Ctrl+C が押されるまで、コンソールは接続されたままです。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /observe-seconds:8
```
終了コード `0`。上記と同じですが、セッションは `Running` に到達してから 8 s 後に停止されます（トレイ/パイプからの停止があればそれより早く停止）。検証スクリプトで使用されます。

```text
OmsiLaunch.exe "/saved:situations\Linie 5.osn"
```
終了コード `0`/`1`。SAVED_SITUATION: マップ、時刻、位置は `.osn` から取得されます（`situations\Linie 5.osn` は OMSI 2 に同梱されており、Berlin-Spandau でプレイヤーバスとともに開始します）。値は `/list:Situations` が出力するシチュエーション識別子です（インストール環境からの相対パスで、大文字と小文字を区別しません）。`Linie 5.osn` のようなファイル名だけでは解決されません（`OL_E_SITUATION_NOT_FOUND`、終了コード `1`）。`/saved` と一緒に `/map` または `/entrypoint-index` を指定すると、終了コード `2` で拒否されます。変更と復元は NEW_MAP と同様です。OMSI 自身がシチュエーションのマップを `options.cfg` の `[last_map]` に記録します。この OMSI による書き込みは、`/set` が `options.cfg` をオーバーレイしない限り元に戻されません（[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照）。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /set:traffic.randomVehicles=150 /set:traffic.humans=200 /set:graphics.maxFPS=60
```
終了コード `0`。変更対象: `options.cfg`（セッションオーバーレイ、セマンティックなトークン/ベクターのパッチ。CP1252 のバイトは保持されます）とスプラッシュのオーバーレイ。復元対象: `options.cfg` とスプラッシュのファイルが正確に復元されます（RV-005、RV-006）。`/set:graphics.texture=...` は終了コード `2`（`OL_E_SETTING_NOT_WRITABLE`）、`/set:foo=1` は終了コード `2`（`OL_E_UNKNOWN_SETTING`）で終了します。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Unset
```
終了コード `0`。変更対象: スプラッシュのオーバーレイはなく、起動時のハンドオフと `closecheck` の処理のみです。復元対象: スプラッシュについて復元するものはありません。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /splash:Managed /splash-language:PTB /splash-assets:.omsilaunch\assets\my-splash
```
終了コード `0`（ディレクトリまたは BMP が存在しないか、640x480 24 ビットでない場合は `OL_E_SESSION_PRESENTATION_INVALID` で `1`）。変更対象: カスタムディレクトリからの `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_PTB.bmp`（セッションオーバーレイ）。復元対象: 両方のファイルが正確に復元されます。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Disabled
```
終了コード `0`。変更対象: スプラッシュのオーバーレイ以外にディスク上の変更はありません。セッション中はプロセス内ダウンローダーが抑止されます。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /internet-textures:Override /internet-textures-profile:<ITX_PATH>
```
終了コード `0`（プロファイルを省略した場合は `OL_E_ITX_PROFILE_REQUIRED` で `2`、無効なプロファイルまたは `Texture\` 外のターゲットの場合は `1`）。変更対象: `Texture\standard.itx`（セッションオーバーレイ）。プロファイルに列挙されたすべてのターゲットと `Texture\standard.ipr` はセッション削除になります。復元対象: オーバーレイは除去され、削除されたオリジナルは復元されます。セッション中に OMSI がそれらのパスに作成したファイルは、セッションの副産物として削除されます。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /startup-timeout:300
```
終了コード `0`。デフォルトの 180 s の代わりに、最大 300 s（+5 s）`Running` を待機します。`/shutdown-timeout:60` は受け付けられますが、このビルドでは効果がありません。

<a id="predefined-session-profile"></a>
### 定義済みセッションプロファイル

プロファイルファイル `<OMSI_PATH>\.omsilaunch\session-profiles\grundorf-quick\profile.yaml`:

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: Example
version: "1.0"
compatibility:
  maps:
    - maps\Grundorf\global.cfg
new:
  map: maps\Grundorf\global.cfg
  entrypoint-index: 1
presets:
  - index: 1
    id: low
    name: Low detail
    settings:
      graphics.maxFPS: 30
      graphics.tileDistance: 3
```

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:1 /new
```
終了コード `0`。変更対象: `options.cfg`（プリセットの設定、セッションオーバーレイ）とスプラッシュのオーバーレイ。復元対象: そのすべて。`/map:...` または `/set:graphics.maxFPS=60` を追加すると終了コード `2`（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）、`/predefined-profile-index` を省略すると終了コード `2`（`OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）で終了します。パッケージ同梱の例 `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` は完全なスキーマを示していますが、同梱されたままの状態ではその `new:` ブロックが `date`、`time`、`weather` を要求しており、これらはこのビルドでは適用できません。`/new` でそのプランを作成すると `NOT RUNNABLE`（`OL_E_CAPABILITY_UNAVAILABLE`）になります。使用する前にそれらのキーを削除してください。

<a id="launchspec-file"></a>
### LaunchSpec ファイル

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan --json
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json
```
終了コード `0`/`1`。パッケージ同梱の例は、Grundorf、エントリポイントインデックス `1`、マネージドスプラッシュ、ネイティブの Internet Textures を選択します。`RootPath: "."` は実行ファイルのディレクトリに解決されます。変更は明示的な NEW_MAP の例と同様です。未知のプロパティを含む spec は終了コード `2`（`OL_E_SPEC_UNKNOWN_PROPERTY: $.Path`）、ファイルが存在しない場合は終了コード `6`（`OL_E_SPEC_NOT_FOUND`）、1 MiB を超えるファイルは終了コード `2`（`OL_E_SPEC_TOO_LARGE`）で終了します。

```text
OmsiLaunch.exe "<OTHER_OMSI_PATH>" /spec:<SPEC_PATH> /startup-timeout:120
```
終了コード `0`/`1`。明示的なインストール環境 `<OTHER_OMSI_PATH>` が spec の `RootPath` より優先されます。`/startup-timeout` が spec の `Behavior.StartupTimeoutSeconds` を上書きするのは、それが指定されたためです。

<a id="silent-detached-start"></a>
### サイレント（デタッチ）開始

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```
`OmsiLaunchW.exe` が開始された時点で終了コード `0`（`{"delegated": true, "host_process_id": <pid>}`）。`OmsiLaunchW.exe` が存在しない（`OL_E_WINDOWS_HOST_MISSING`）か開始できなかった場合は `7` です。ランチャーは即座に戻り、呼び出し元のコンソールやパイプを開いたままにしません。出力をキャプチャするスクリプトは直ちに end-of-file を受け取ります（ランタイムクロージャ `T04`）。セッション自体は `OmsiLaunchW.exe` 内で実行されます。コンソール出力はなく、失敗はメッセージボックスで表示され、トレイアイコンが利用できます。進行状況は `session status`、`events watch`、および `.omsilaunch\diagnostics\<sessionId>-host.log` で確認してください。完全なリファレンス: [OmsiLaunchW.exe](omsilaunchw.md)。

<a id="controlling-a-running-session-client-mode"></a>
## 実行中のセッションの制御（クライアントモード）

オーナーが実行中の間に、同じインストールディレクトリからこれらを実行します。いずれも、オーナーが応答しない場合は終了コード `4`（`OL_E_NO_ACTIVE_SESSION`）、制御エラーの場合は `7` で終了します。

```text
OmsiLaunch.exe session status --json
```
終了コード `0`。`SessionId`、`State`（`14` = `Running`）、`Diagnostics`、`RuntimeEvents` を返します。何も変更しません。

```text
OmsiLaunch.exe events read --json
OmsiLaunch.exe events watch
```
終了コード `0`（`events watch` は Ctrl+C まで実行されます）。上限付きのランタイムイベント（`gameplay.entered`、D3D ライフサイクルイベントなど）。何も変更しません。

```text
OmsiLaunch.exe session stop
```
終了コード `0`（`{"accepted": true, "session_id": "..."}`）。正規の停止を要求します。OMSI は強制終了され、オーバーレイはオーナーによって復元され、ジャーナルは削除されます。クライアントは即座に戻り、オーナープロセスは復元の後に終了します。

<a id="runtime-reads"></a>
## ランタイムの読み取り

```text
OmsiLaunch.exe time get
OmsiLaunch.exe weather get
OmsiLaunch.exe weather actual get
OmsiLaunch.exe map get
OmsiLaunch.exe camera get
OmsiLaunch.exe player get
OmsiLaunch.exe timetable get
OmsiLaunch.exe timetable lines list
OmsiLaunch.exe drivers list
OmsiLaunch.exe tickets get
OmsiLaunch.exe vehicles summary
OmsiLaunch.exe humans summary
```
終了コード `0`。エンベロープに `RuntimeCommandResult`（`Succeeded`、`Values`）が含まれます。何も変更しません。タイムアウトは 8 s です（`OL_E_RUNTIME_REQUEST_TIMEOUT`、終了コード `7`）。

```text
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
OmsiLaunch.exe hof get --handle=rv-000001
OmsiLaunch.exe constants list --handle=rv-000001
OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version
OmsiLaunch.exe curves list --handle=rv-000001
OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0
OmsiLaunch.exe scripts variable list --handle=rv-000001
OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings
OmsiLaunch.exe scripts string list --handle=rv-000001
OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route
OmsiLaunch.exe humans list
OmsiLaunch.exe humans get --handle=hb-000001
```
終了コード `0`。必須の引数がない場合は `2`（`OL_E_RUNTIME_ARGUMENT_REQUIRED`。要求の送信前に報告されます）。プラグインが要求を拒否した場合は `7` で、`Values.detail` に具体的な理由を含む `OL_E_RUNTIME_OPERATION_FAILED` になります。例として、同じオブジェクトをもはや識別しないハンドルに対する `OL_E_RUNTIME_OBJECT_HANDLE_STALE` や、`OL_E_RUNTIME_CONSTANT_NOT_FOUND` があります。ハンドルはセッションスコープであり、直前の `list` から取得します。変数、定数、カーブの名前は各車両モデルによって定義されるため、`list` の結果から取得してください。上記の名前は、`situations\Linie 5.osn` セッションのプレイヤーバスである `rv-000001` について一覧表示されたものです。何も変更しません。

```text
OmsiLaunch.exe /runtime:timetable.track-entries.list
OmsiLaunch.exe /runtime:vehicle.constant.get /runtime-arg:handle=rv-000001 /runtime-arg:name=antrieb_getr_version
```
終了コード `0`。階層ルートのない操作も、ルートのある操作も、操作 id で指定できます。`timetable.track-entries.list` は上限付きリストです。`situations\Linie 5.osn` では、825 件中 137 件のエントリを `truncated=true` とともに返しました（ドキュメント監査のランタイム再テスト）。何も変更しません。

<a id="runtime-writes"></a>
## ランタイムの書き込み

```text
OmsiLaunch.exe time set --minute=30
```
終了コード `0`。OMSI のメモリ上の時計を変更します（検証済み: 書き込み、読み戻し、2 回目の `time set` による復元）。停止時に元に戻されることはありません。

```text
OmsiLaunch.exe camera set --field_of_view=50
OmsiLaunch.exe camera lock --family=0 --preset=1
OmsiLaunch.exe camera unlock
```
終了コード `0`（`camera lock` で `--family` がない場合は `2`）。セッションのカメラ状態を変更します。`camera lock` には PlayerVehicle が必要です（例: `/saved` セッション）。ヘッドレスの `/new` セッションでは失敗します（`Values.detail` に `OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`）。ロックとアンロックは、保存済みシチュエーションを使用してランタイム検証済みです（ファミリー 0、2、1 とカメラの読み戻し）。停止時に元に戻されることはありません。ロックポリシーはセッションとともに終了します。

```text
OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1
```
終了コード `0`（`handle`、`name`、`value` のいずれかがない場合は `2`）。その車両の数値スクリプト変数を 1 つ変更します。元に戻されることはありません。

```text
OmsiLaunch.exe weather set --wind_speed=5 --temperature=12 --cloud_type="Cumulus 1"
```
終了コード `0`。OMSI のソース天候レコードを書き込み、ライブ METAR の更新を停止します。OMSI は次の（一時停止されていない）天候ティックで、霧、光、実際の風、湿度をそこから導出します。何かを書き込む前にすべての値が検査されます（`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、終了コード `7`）。`relative_humidity` のような導出キーは `OL_E_RUNTIME_SETTING_NOT_PERSISTENT` を返し、代わりに設定すべきキーが示されます。元に戻されません。EXPERIMENTAL: ランタイム検証は保留中です。

```text
OmsiLaunch.exe weather metar set --icao=EDDF
```
終了コード `0`。その観測所のライブ METAR 天候に切り替えます。OMSI は次の天候更新でそれをダウンロードします。EXPERIMENTAL: ランタイム検証は保留中です。

```text
OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1
OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=0
OmsiLaunch.exe scripts sound-trigger --handle=rv-000001 --trigger=ticket_print
```
終了コード `0`。`scripts string set` は OMSI 自身の `SetStringVariable` を通じて書き込み、読み戻した値を返します。車両スクリプトが宣言していない名前は `OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`（終了コード `7`）を返します。`scripts trigger` は OMSI の入力ハンドラーと同じようにトリガーを発生させ（`1` 押下、`0` 解放）、車両スクリプトは直ちにそれを実行します。トリガー名と変数名は各車両モデルが定義するもので、上記は例です。元に戻されません。EXPERIMENTAL: ランタイム検証は保留中です。

<a id="spawn"></a>
## スポーン

```text
OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus
OmsiLaunch.exe vehicles place-random
```
終了コード `0`。`Values` に新しい `rv-NNNNNN` ハンドルが含まれます（`--model` がない場合は `2`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID` の場合は `7`）。クライアントのタイムアウトは 30 s です。道路車両のコレクションを変更します（車両が 1 台追加されます）。プレイヤー車両は割り当てません。元に戻されることはありません。車両は停止時に OMSI とともに消えます。

<a id="d3d-textures"></a>
## D3D テクスチャ

```text
OmsiLaunch.exe /runtime:d3d.status
OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8 --levels=1
OmsiLaunch.exe /runtime:d3d.texture.describe --handle=<HANDLE> --level=0
OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --level=0 --x=0 --y=0 --width=8 --height=8 --pixels_base64=<BASE64>
OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>
```
`<HANDLE>` は `create` が出力する `handle` の値です（`d3dtex-<session id>-<16 hex digits>`）。`<BASE64>` は、32 ビット形式の場合 `width * height * 4` バイト（8 x 8 x 4 = 256 バイト）にデコードされ、かつ最大 48 KiB である必要があります。終了コード `0`。必須の引数がない場合は `2`。`OL_E_D3D_INVALID_TEXTURE_FORMAT`、`OL_E_D3D_INVALID_PIXEL_BUFFER`、`OL_E_D3D_RESOURCE_RELEASED`（2 回目の解放、または解放後の describe）、`OL_E_D3D_STALE_RESOURCE_HANDLE`（デバイスリセット前のハンドル、または別のセッションのハンドル）、`OL_E_D3D_RESET_IN_PROGRESS`、`OL_E_D3D_NOT_READY`、`OL_E_D3D_DEVICE_LOST` の場合は `7` です。セッションが所有する GPU リソースを作成します。これらは明示的に、または OMSI の終了時に解放されます。ファイルには一切触れません。

<a id="owner-side-single-runtime-operation"></a>
## オーナー側での単一のランタイム操作

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /runtime:time.read /observe-seconds:5
```
終了コード `0`。セッションを開始し、`Running` の後に `time.read` を 1 回実行し（タイムアウト 5 s）、`.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` を書き込み、5 s 間実行を続けてから停止し、復元します。ランタイムの失敗は `runtime_error` として出力され、セッションを終了させることはありません。

<a id="recovery"></a>
## リカバリ

```text
OmsiLaunch.exe /recovery-status --json
```
終了コード `0`: ジャーナルが存在しない場合は `{"pending": false, ...}`、存在する場合は `{"pending": true, "recovered": false}`。オーナーがインストール環境を保持している間は終了コード `7`（`OL_E_INSTALLATION_BUSY`）です。何も変更しません。

```text
OmsiLaunch.exe /recover --json
```
何も保留されていなかった場合、または復元が完了した場合は終了コード `0`（`recovered: true`。`diagnostics` には `restore.session-artifact-removed` と `OL_W_RESTORE_FOREIGN_FILE_RETAINED` が含まれることがあります）。ジャーナルが保留中で、依然として保留中のままの場合は終了コード `8`（`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`）。オーナーまたはジャーナルに記録された OMSI がインストール環境を保持している間は終了コード `7`（`OL_E_INSTALLATION_BUSY`）。復元されたバイトの検証に失敗した場合は終了コード `10`（`OL_E_INTERNAL`）です（`Restore hash mismatch` または `Restore presence mismatch`。ジャーナルは保留中のまま残ります）。変更対象: ジャーナルに記録されたすべてのファイルを、SHA-256 を検証したうえで `.omsilaunch\backup\<sessionId>\` から復元し、その後ジャーナルとバックアップディレクトリを削除します。ジャーナルに記録された `Omsi.exe` がまだ生存している間は、`OL_E_INSTALLATION_BUSY` で拒否されます（ランタイムクロージャ `S04`、`S04b`、`F01`）。

<a id="exit-code-quick-check-powershell"></a>
## 終了コードの簡易確認（PowerShell）

```powershell
& .\OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json | Out-Null
$LASTEXITCODE   # 0 = READY, 1 = NOT RUNNABLE, 2 = bad arguments
```
