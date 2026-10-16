# ランタイム制御

<!-- l10n: source=reference/runtime-control.md source-sha256=b79a9850e602d765b5b38aa36000259df855a06801f97d18558f1cf962b396d5 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/runtime-control.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

ランタイム制御とは、OmsiLaunch が実行中の OMSI セッション内で実行する読み取り・書き込み・アクション操作の集合です。このページでは、ランタイム制御に到達する 3 つの方法（オーナー API、CLI クライアント、ローカルコントロールプレーン）、要求が呼び出し元からプラグインへ届き、戻ってくるまでの流れ、ハンドルと要求 ID の仕組み、適用されるタイムアウト、意図的にジャーナルに記録しないもの、引数と結果の規約を、ファミリーごとの実例とともに説明します。引数、結果キー、エラーを含む操作の一覧そのものは[ケイパビリティ](capabilities.md)にあります。ソース: `OmsiLaunchService.ExecuteRuntimeAsync`、`CurrentRuntimeCommandStore`（`src/OmsiLaunch.Process/RuntimeDeployment.cs`）、`CurrentRuntimeCommandMailbox` と `CurrentRuntimeControl`（`src/OmsiLaunch.Plugin/CurrentRuntimeControl.cs`）、`LocalControlPlane` と `CliInput`（`tools/OmsiLaunch.Cli/`）。

<a id="three-entry-points"></a>
## 3 つのエントリポイント

| エントリポイント | 利用者 | 経路 | タイムアウト |
| --- | --- | --- | --- |
| オーナー API | `IOmsiLaunch.StartSessionAsync` でプロセス内からセッションを開始したインテグレーター | `ExecuteRuntimeAsync(session, RuntimeCommand, timeout)` -> レジストリによる検証 -> セッションの検索 -> メールボックス | 呼び出し元が指定する `TimeSpan` |
| オーナー CLI（`/runtime:<op>`） | セッションを所有する `OmsiLaunch.exe` プロセス | `Running` の直後に 1 つの操作を実行し、結果を `.omsilaunch\diagnostics\<session>-runtime-operation.json` とコンソールに書き込みます。セッションは継続します | 5 s（`road-vehicles.spawn` は 15 s） |
| CLI クライアント | インストール環境の引数を**付けずに**実行した任意の `OmsiLaunch.exe`（例: `OmsiLaunch.exe time get`） | 実行ファイルが置かれているインストール環境のオーナーへの名前付きパイプ `runtime.execute` -> オーナーが `ExecuteRuntimeAsync` を呼び出す | クライアント側・オーナー側とも 8 s（`road-vehicles.spawn` は 30 s） |
| ローカルコントロールプレーン | 同じ Windows ユーザーの任意のプロセス | CLI クライアントと同じパイププロトコル。[ローカル制御](local-control.md)を参照 | 上記と同じ |

どの経路も最終的に `ExecuteRuntimeAsync` に到達し、次の順序で公開境界を適用します。

1. `PublicCapabilityRegistry.ValidateRuntimeArguments`: `PublicRuntimeOperationIds` にない操作（すべての `internal.*` 操作を含む）は `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN` を返します。必須引数が欠落しているか空白の場合は `OL_E_RUNTIME_ARGUMENT_REQUIRED` を返します。いずれもセッションには触れません。
2. セッションの検索（不明なハンドルでは `KeyNotFoundException`）。`RuntimeCommand.SessionId` がハンドルと異なる場合は `OL_E_RUNTIME_SESSION_MISMATCH`、状態が `Running` でない場合は `OL_E_SESSION_NOT_RUNNING` となります。
3. メールボックスへの要求（後述）。その後 `ScrubInternalValues` が、`internal_` で始まるか `_address`、`_pointer`、`_vmt` で終わる結果キーをすべて削除します。

CLI クライアントとコントロールプレーンは、オーナーに接続する前に自身で手順 1 を実行します。そのため、セッションがアクティブでない場合でも、無効なコマンド名は終了コード 2 として報告されます（`OL_E_RUNTIME_OPERATION_UNKNOWN` と `OL_E_RUNTIME_ARGUMENT_REQUIRED` は `InvalidArguments` に対応し、その他の拒否は 7、オーナーがいない場合は 4 に対応します）。

コントロールプレーンのエンドポイントは、オーナーが `Running` の間だけ、起動後かつ `/runtime-batch`、`/runtime-write-batch`、`/d3d-batch` のハーネスが完了した後に存在します。変更を伴うコマンド（`runtime.execute`、`session.stop`）はアクティブな `session_id` を含める必要があります。CLI はこれを `session.status` から自動的に取得します（取得できない場合は `OL_E_CONTROL_SESSION_MISMATCH`）。

<a id="request-identity-and-the-mailbox"></a>
## 要求 ID とメールボックス

`RuntimeCommand(SessionId, RequestId, Operation, Arguments)` は `RuntimeCommandWire` エンベロープ（マジック `OLRC`、バージョン 1、72 バイトのヘッダー、JSON ペイロードの SHA-256）にシリアル化され、セッションの 64 KiB のシングルフライトのメールボックス（`OmsiLaunch.Runtime.<sessionId>`）に格納されます。プラグインは OMSI の UI スレッド上で 50 ms ごとにメールボックスをポーリングし、そこで操作を実行して応答を書き込みます。

チャネルを堅牢にするルール:

| ルール | 効果 |
| --- | --- |
| シングルフライト | セッションごとに同時に 1 つの要求だけを処理します。ホストはセマフォで呼び出し元を直列化します。新しい要求が到着した時点でスロットがまだ `requested` の場合は `OL_E_RUNTIME_CHANNEL_BUSY` です。 |
| セッションへのバインド | プラグインは、セッション GUID が自身のものでない要求を無視し（そしてクリアし）ます。ホストは、セッションまたは要求 id が一致しない応答を拒否します（`OL_E_RUNTIME_RESPONSE_INVALID`）。 |
| タイムアウト | 期限を過ぎると、ホストはスロットをアイドルに戻し、`TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")` をスローします。 |
| 遅延応答 | プラグインは、スロットがまだ自身の要求 id を保持している場合にのみ応答を公開します。放棄された要求への応答は破棄されます。それでも応答が書き込まれた場合、次のホスト要求は古い `responded` スロットを見つけて破棄し、処理を続行します。その古い応答が新しい要求と**同じ**要求 id を持つ場合、ホストは `OL_E_RUNTIME_REQUEST_ID_REUSED` をスローします。したがって、呼び出し元はセッション内で要求 id を決して再利用してはいけません。 |
| サイズ | スロットより大きい要求は格納前に拒否されます（`ArgumentOutOfRangeException`）。収まらないリストのページはプラグインが短縮します（末尾の行を削除し、`truncated=true`、小さくなった `returned_count`、最初に削除された行を指す `next_cursor`）。それ以外のサイズ超過の応答は、型付きの `OL_E_RUNTIME_RESPONSE_TOO_LARGE` エラーに置き換えられます。 |
| チャネルのクローズ | セッション終了後は `OL_E_RUNTIME_CHANNEL_CLOSED` です。 |

要求 id は呼び出し元が選ぶ `ulong` です。CLI は固定の範囲を使用します。`/runtime:` には `10_001`、転送されるコントロールプレーンの要求には `50_001+`、読み取りバッチには `1+`、D3D バッチには `20_000+`、`D3DRuntimeApi` には `30_000+` を使います。インテグレーターは、セッションごとに単調増加するカウンターを使用してください。

<a id="handles-and-stale-detection"></a>
## ハンドルと失効の検出

| プレフィックス | 型 | 発行元 | 形式 |
| --- | --- | --- | --- |
| `rv-` | RoadVehicle | `road-vehicles.list`、`road-vehicles.spawn`（`created_handle`）、`player-vehicle.read` | `rv-` + 10 進数 6 桁（`rv-000003`） |
| `hb-` | Human | `humans.list` | `hb-` + 10 進数 6 桁 |
| `d3dtex-` | D3D テクスチャ | `d3d.texture.create` | `d3dtex-<sessionId N>-<16 hex digits>` |

ハンドルは不透明でセッション単位です。プラグインが発行し、アドレスを決してエンコードせず、別のセッションでは意味を持ちません。ハンドルを解決するとき、プラグインは対応するアドレスがまだ OMSI の有効なコレクション内にあること（最後のリスト読み取り時点。`road-vehicles.list` と `humans.list` がビューを更新します）を確認し、オブジェクトのフィンガープリント（Delphi の VMT と車両定義ポインター、または人物のモデルインデックス）を読み直します。不一致は、ネイティブオブジェクトが破棄されそのアドレスが再利用されたことを意味し、`OL_E_RUNTIME_OBJECT_HANDLE_STALE` になります。同じ有効なオブジェクトに対しては、リスト読み取りを繰り返しても同じトークンが返されます。残る死角: 2 回のリスト読み取りの間に、同じクラス・同じ定義のオブジェクトが同じアドレスに再作成された場合は区別できません。D3D ハンドルはネイティブブリッジが検証します。不明なハンドルや別セッションのハンドルは `OL_E_D3D_STALE_RESOURCE_HANDLE`、解放済みのハンドルは `OL_E_D3D_RESOURCE_RELEASED` となり、デバイスの世代が変わるとテクスチャは `STALE` になります。

ハンドルはネイティブアドレスではありません。解析したり、数値として比較したり、セッションをまたいで永続化したり、別のセッションに渡したりしてはいけません。発行したセッションでのみ有効な不透明な文字列として扱ってください。

ランタイムのエビデンス: 解放済みの D3D ハンドルは新しいテクスチャが作成された後も拒否され続け、前のセッションのハンドルは次のセッションで拒否されます（ランタイムクロージャー `H02`）。D3D のデバイスリセットは、すべての有効なテクスチャを無効化します（`OL_E_D3D_STALE_RESOURCE_HANDLE`、`D01`）。自然な削除後の RoadVehicle と Human の失効検出（RV-002）には、安全なランタイム上の発生手段がありません（観測期間中に OMSI はオブジェクトを削除せず、オブジェクトを削除する公開操作もありません）。これはオフラインでカバーされています。

<a id="what-is-not-journaled"></a>
## ジャーナルに記録されないもの

ランタイムでの変更は OMSI のメモリだけを変更します。**これらはトランザクションジャーナルに記録されず、セッション終了時に復元されません**: `time.set`、`camera.set`、`camera.lock`（ポリシーはプラグインのシャットダウン時に停止します）、`vehicle.variable.set`、`road-vehicles.spawn`、`road-vehicles.place-random`、およびすべての `d3d.texture.*` リソース。これらは OMSI プロセスとともに消えます。OmsiLaunch はセッション終了時に、OMSI に何も永続化させずにこのプロセスを終了させます（[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照）。ランタイム系の操作はいずれもファイルシステムに触れません。

<a id="argument-and-result-conventions"></a>
## 引数と結果の規約

- 引数は文字列のキーと値のペアです。CLI では、コマンドワードの後の `--key=value` がランタイム引数になります（`OmsiLaunch.exe vehicles get --handle=rv-000001`）。`/runtime:<op>` では `/runtime-arg:key=value` が同等の指定です。数値はインバリアントカルチャ（小数点は `.`）を使用し、ブール値は `true`/`false` です。
- コマンドワードは `CliInput.HierarchicalRoutes` によって操作 id に対応付けられます（例: `time get` -> `time.read`、`vehicles summary` -> `road-vehicles.read`、`scripts variable set` -> `vehicle.variable.set`）。ルートの完全な表は [CLI リファレンス](cli.md)にあります。D3D 操作にはコマンドワードのルートがありません。`/runtime:d3d.status` または `/runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` を使用してください。
- 結果はフラットな文字列の辞書です。リストは `<row>.<n>.<field>` 形式のキー（`vehicle.0.handle`、`track.3.filename`、`name.12`）と、ページのフィールド `count`、`returned_count`、`truncated`、`next_cursor` を使います（[ページネーション](capabilities.md#pagination)を参照）。
- 失敗には `Succeeded=false` と `OL_E_` の `ErrorCode` が含まれ、プラグイン側の失敗では `detail` も含まれます（D3D では `native_status`、予期しない障害では `exception` も含まれます）。

<a id="cli-envelope---json"></a>
### CLI エンベロープ（`--json`）

```json
{
  "ok": true,
  "command": "time.read",
  "protocol_version": "0.1",
  "result": {
    "SessionId": "5f641c8d-5828-42a9-b811-5e45b9d05533",
    "RequestId": 50001,
    "Succeeded": true,
    "ErrorCode": null,
    "Values": { "hour": "6", "minute": "31", "second": "12", "day": "20", "month": "9", "year": "2026" }
  }
}
```

エラーエンベロープは `{ "ok": false, "command": ..., "protocol_version": "0.1", "error": { "code": "OL_E_...", "category": "...", "message": "..." } }` です。`--json` を付けない場合、CLI は結果オブジェクトをインデント付きの JSON、または `CODE: message` として出力します。

<a id="api-envelope"></a>
### API エンベロープ

```csharp
var result = await launch.ExecuteRuntimeAsync(session,
    new RuntimeCommand(session.SessionId, requestId++, "vehicle.variable.set",
        new Dictionary<string, string> { ["handle"] = "rv-000001", ["name"] = "Refresh_Strings", ["value"] = "1" }),
    TimeSpan.FromSeconds(5));
if (!result.Succeeded) Console.WriteLine(result.ErrorCode);
else Console.WriteLine(result.Values!["value"]);
```

`ExecuteRuntimeAsync` は、レジストリによる検証の後の境界違反（`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_*`、`OL_E_RUNTIME_REQUEST_TIMEOUT`、`OL_E_RUNTIME_RESPONSE_INVALID`）では例外をスローし、レジストリによる拒否とプラグイン側のエラーでは `Succeeded=false` を返します。

<a id="worked-examples"></a>
## 実例

すべての CLI の例は、`OmsiLaunch.exe` を含むインストール環境に対してオーナーが実行中であること（例: `OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1` で開始、例でプレイヤー車両が必要な場合は `OmsiLaunch.exe "/saved:situations\Linie 5.osn"` で開始）を前提とし、同じインストール環境の 2 つ目のコンソールから実行します。

| ファミリー | コマンド | 動作 |
| --- | --- | --- |
| セッション | `OmsiLaunch.exe session status --json` | `SessionId`、`State`、診断情報、上限付きのランタイムイベント一覧を読み取ります。 |
| 時刻 | `OmsiLaunch.exe time get` の後に `OmsiLaunch.exe time set --hour=7 --minute=30` | 時計を読み取り、プロファイル済みの `SetTime` で設定して読み戻し値を返します。 |
| 天候 | `OmsiLaunch.exe weather get` と `OmsiLaunch.exe weather actual get` | 現在の天候状態と actual/ICAO の天候状態を読み取ります。`OmsiLaunch.exe weather set --wind_speed=5` は手動の天候（OMSI のソースレコード。ライブ METAR は停止します）を設定し、`OmsiLaunch.exe weather metar set --icao=EDDF` はライブ METAR 天候に切り替えます。どちらもランタイム検証されるまで EXPERIMENTAL です。 |
| マップ | `OmsiLaunch.exe map get` | マップの識別情報、名前、説明、タイル数、年の範囲、通行方向。 |
| カメラ | `OmsiLaunch.exe camera set --field_of_view=50` の後に `OmsiLaunch.exe camera lock --family=0 --preset=1` と `OmsiLaunch.exe camera unlock` | FOV を書き込み、運転士ファミリーをプリセット 1 で固定し（プレイヤー車両が必要です。例: 保存済みシチュエーション）、ポリシーを解除します。 |
| 車両 | `OmsiLaunch.exe vehicles summary`、`OmsiLaunch.exe vehicles list`、`OmsiLaunch.exe vehicles get --handle=rv-000001` | 件数、ハンドル、1 台のスナップショット。 |
| スポーン | `OmsiLaunch.exe vehicles spawn --model=Vehicles\MAN_SD200\MAN_SD77.bus` | AI の RoadVehicle を 1 台作成し（タイムアウト 30 s）、`created_handle` を返します。`OmsiLaunch.exe vehicles place-random --group=1` は `PlaceRandomBus` を呼び出します。 |
| プレイヤー | `OmsiLaunch.exe player get` | ヘッドレス開始では `present=false`、それ以外ではプレイヤーのスナップショット。 |
| 人物オブジェクト | `OmsiLaunch.exe humans summary`、`OmsiLaunch.exe humans list`、`OmsiLaunch.exe humans get --handle=hb-000001` | 件数、ハンドル、1 体のスナップショット。 |
| 時刻表 | `OmsiLaunch.exe timetable get`、`OmsiLaunch.exe timetable tracks list`、`OmsiLaunch.exe timetable logs list` | マネージャーの件数、上限付きの track 行、時刻表のログ。 |
| スクリプト | `OmsiLaunch.exe scripts variable list --handle=rv-000001`、`OmsiLaunch.exe scripts variable get --handle=rv-000001 --name=Refresh_Strings`、`OmsiLaunch.exe scripts variable set --handle=rv-000001 --name=Refresh_Strings --value=1`、`OmsiLaunch.exe scripts string get --handle=rv-000001 --name=act_route`、`OmsiLaunch.exe scripts string set --handle=rv-000001 --name=act_route --value=110`、`OmsiLaunch.exe scripts trigger --handle=rv-000001 --name=bus_doorfront0 --value=1` | 数値変数の一覧/読み取り/書き込み、文字列変数の読み取り/書き込み、スクリプトトリガーとサウンドトリガー（文字列の書き込みとトリガーは、ランタイム検証されるまで EXPERIMENTAL です）。 |
| 定数とカーブ | `OmsiLaunch.exe constants list --handle=rv-000001`、`OmsiLaunch.exe constants get --handle=rv-000001 --name=antrieb_getr_version`、`OmsiLaunch.exe curves list --handle=rv-000001`、`OmsiLaunch.exe curves evaluate --handle=rv-000001 --name=retarder_stufe1 --x=0.5` | 車両の定数とカーブの評価。名前はモデル固有です。`list` コマンドが返す名前を使用してください（これらは `situations\Linie 5.osn` のプレイヤーバスで一覧表示されたものです）。 |
| HOF | `OmsiLaunch.exe hof get --handle=rv-000001` | 車両定義の HOF メタデータ。 |
| 運転士と乗車券 | `OmsiLaunch.exe drivers list`、`OmsiLaunch.exe tickets get` | 運転士のレコード、乗車券パック。 |
| D3D | `OmsiLaunch.exe /runtime:d3d.texture.create --width=8 --height=8 --format=A8R8G8B8` の後に `OmsiLaunch.exe /runtime:d3d.texture.update --handle=<HANDLE> --width=8 --height=8 --pixels_base64=<BASE64>` と `OmsiLaunch.exe /runtime:d3d.texture.release --handle=<HANDLE>` | レンダースレッド上でのテクスチャのライフサイクル。`<HANDLE>` は `create` が出力した `handle` です。`--pixels_base64` はデコード後に `width * height * 4` バイト（32 ビット形式）かつ 48 KiB 以内である必要があります。 |
| イベント | `OmsiLaunch.exe events read`、`OmsiLaunch.exe events watch` | 上限付きのイベント一覧、Ctrl+C を押すまでのポーリングによる監視（250 ms）。 |
| 停止 | `OmsiLaunch.exe session stop` | 正規停止を要求します。OMSI が終了させられ、オーナーがトランザクションを復元します。 |

<a id="stability"></a>
## 安定性

チャネル自体（`runtime.command-channel`）は `STABLE_BETA` です。ワイヤーの整合性、セッションへのバインド、遅延応答の破棄、型付きのサイズ超過拒否は、オフラインでは `runtime-command.wire-guard`、`runtime-command.session-binding`、`runtime-command.late-response-ignored` によってカバーされています。ランタイムでは、RV-003（セッション `5f641c8d`）、Round A RA-019（クライアントが 250 ms 後に `road-vehicles.spawn` を放棄し、次の要求は成功）、およびランタイムクロージャーのチャネル検証セット `R03`（タイムアウト、キャンセル、再利用された要求 id、プラグインによる拒否、内部操作、誤ったセッション、欠落した引数。それぞれの後に要求が成功）によってカバーされています。操作ごとの安定性は[ケイパビリティ](capabilities.md)にあります。

<a id="channel-reuse-after-failures"></a>
## 失敗後のチャネルの再利用

ランタイム要求のどの終端経路でも、メールボックスは再利用可能な状態に戻ります。成功、型付きエラー、不正な形式・サイズ超過・別セッション・誤った要求 id の応答、タイムアウト、呼び出し元によるキャンセル、デコードの失敗は、いずれもスロットをアイドルに戻し長さとエンベロープヘッダーをクリアする 1 つのクリーンアップで終わるため、以前の要求の内容を次の要求が読み取ることはありません。新しい要求の開始時に残っている要求や応答が見つかった場合は、まずそれがクリアされます（新しい要求の id を持っている場合は `OL_E_RUNTIME_REQUEST_ID_REUSED`）。プラグイン側では、操作内の例外には `OL_E_RUNTIME_OPERATION_FAILED`、スロットより大きい結果には `OL_E_RUNTIME_RESPONSE_TOO_LARGE`（固定の小さなエンベロープ）、別セッション向けの要求には `OL_E_RUNTIME_SESSION_MISMATCH` で応答し、ホストが放棄した要求には応答しません。これらのルールはオフラインでカバーされています（`runtime-command.terminal-paths-leave-channel-usable`、`plugin-runtime.oversized-and-abandoned-responses`、`plugin-runtime.bounded-list-fits-slot`）。呼び出し元が発生させられる経路（タイムアウト、キャンセル、再利用された id、型付きの拒否、遅延応答）にはランタイムのエビデンスもあります（RA-019、`R03`）。破損した応答、別セッションの応答、サイズ超過の応答は製品の外部から発生させることができず、オフラインのみの検証にとどまります。
