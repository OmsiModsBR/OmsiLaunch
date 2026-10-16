# 最初のセッション

<!-- l10n: source=getting-started/first-session.md source-sha256=3985b8db122dfbbfeabd69692a21620d4e3d381f398197b61be2c500be1f3170 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../getting-started/first-session.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch `0.1.0-beta4` で管理される最初の OMSI セッションを順を追って説明します。OMSI を起動せずにプランを作成する方法、明示的なフラグで開始する方法、事前定義したセッションプロファイルで開始する方法、セッションの制御と停止、そして終了後に診断情報を見つける方法です。パッケージが[インストール](installation.md)の説明どおりにインストールされていることを前提とします。すべてのフラグは [CLI リファレンス](../reference/cli.md)で規定されています。その他の呼び出し例は [CLI の使用例](../reference/cli-examples.md)にあります。

<a id="what-a-session-does"></a>
## セッションが行うこと

セッションは 1 つの OMSI プロセスを包むトランザクションです。OmsiLaunch は、変更するファイル（既定では `GUI\` 配下の 2 つのスプラッシュ用ビットマップ、`/set` オーバーレイが要求された場合はさらに `options.cfg`）のスナップショットを取得し、`.omsilaunch\` 配下に永続的なジャーナルを書き込み、オーバーレイを適用し、常駐プラグインとともに `Omsi.exe` を起動し、ゲームプレイに入る（`Running`）まで待機し、セッションを制御可能な状態に保ち、最後に OMSI を終了させて、変更したすべてのファイルをバイト単位で正確に復元します。`/new` がマップやエントリポイントを暗黙に選択することはありません。両方を指定するか、`/spec` ファイルまたはセッションプロファイルから与える必要があります。

<a id="1-plan-nothing-is-started"></a>
## 1. プランを作成する（何も開始されません）

OMSI ルートから実行します。インストール環境は、既定では `OmsiLaunch.exe` を含むディレクトリになります。

```text
OmsiLaunch.exe /list:Maps
OmsiLaunch.exe /list:Entrypoints /map:maps\Grundorf\global.cfg
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan --json
```

プランには `"IsRunnable": true` と表示されなければなりません（終了コード `0`）。プランには `TouchedFiles` と `PlannedMutations` が一覧されるため、セッションが何をオーバーレイするのかを正確に確認できます。先に進む前に、`OL_E_` 診断があればすべて解消してください。この時点では何も書き込まれていません。

パッケージに含まれるサンプルの spec は、ファイルを使って同じことを行います。

```text
OmsiLaunch.exe /spec:.omsilaunch\examples\release-session.example.json /plan
```

<a id="2-start-with-explicit-flags"></a>
## 2. 明示的なフラグで開始する

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

実行される処理（順番どおり）:

1. プランが出力されます（`Plan: READY profile=Omsi23004`（既知のレイアウトの場合））。
2. 古い保留中のジャーナルがあればそのリカバリ、リースの取得、スナップショット、ジャーナル、オーバーレイ、プラグインの整合性検査、`Omsi.exe` の起動。
3. トレイアイコンが表示されます（`OmsiLaunch is running`、OmsiLaunch が実行中であることを示すツールチップ）。[Windows トレイ](../reference/windows-tray.md)を参照してください。
4. ゲームプレイに入ると、`Running` ステータスが JSON で出力されます（`"State": 14`）。既定の起動タイムアウトは 180 s です（変更するには `/startup-timeout:<1..600>` を使用します）。
5. コンソールはセッションが終了するまでアタッチされたままです。停止するためにコンソールウィンドウを閉じないでください。以下の停止方法のいずれかを使用してください。

最初の実行で任意に追加できるもの:

- `/set:graphics.maxFPS=60`（`options.cfg` のオーバーレイで、終了時に復元されます）
- `/splash:Unset` で OMSI のスプラッシュ画面をそのままにする、または `/splash-language:DEU` でローカライズされた管理対象スプラッシュを選択する
- `/observe-seconds:30` で `Running` の 30 s 後に自動停止する（スモークテストに便利です）
- `--json` で構造化された出力を得る

日付、時刻、年、天候、プレイヤー車両を要求するフラグ（`/date`、`/time`、`/year`、`/weather*`、`/vehicle` など）は受け付けられますが、このビルドでは適用できません。プランは `OL_E_CAPABILITY_UNAVAILABLE` により `NOT RUNNABLE` になります。これらは指定しないでください。

<a id="3-start-with-a-predefined-session-profile"></a>
## 3. 事前定義したセッションプロファイルで開始する

セッションプロファイルは `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` に置く YAML ファイルで、マップ、エントリポイント、および最大 99 個の設定プリセットを固定します（スキーマ `omsilaunch.session-profile/v1`。完全なリファレンスは[セッションプロファイル](../reference/session-profiles.md)を参照してください）。`D:\OMSI 2\.omsilaunch\session-profiles\grundorf-quick\profile.yaml` を作成します。

```yaml
schema: omsilaunch.session-profile/v1
id: grundorf-quick
name: Grundorf quick start
author: You
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
  - index: 2
    id: high
    name: High detail
    settings:
      graphics.maxFPS: 60
      graphics.tileDistance: 8
```

続いて次を実行します。

```text
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new /plan
OmsiLaunch.exe /predefined-profile:grundorf-quick /predefined-profile-index:2 /new
```

覚えておくべき規則: `id` はディレクトリ名と一致しなければなりません。インデックスは `1..99` です。プロファイルが所有するフィールドを上書きすることになる明示的なフラグ（`/map`、`/entrypoint-index`、プリセットが所有する `/set` キー、プリセットに `presentation` がある場合のスプラッシュ関連フラグ）は `OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT` で拒否されます（終了コード `2`）。`new:` ブロックは `/new` の場合にのみ適用されます。`/saved:<file.osn>` の場合は、そのシチュエーションのマップが `compatibility.maps` に記載されていなければなりません。パッケージに含まれる `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` はスキーマ全体を示していますが、その `new:` ブロックはこのビルドでは適用できない `date`、`time`、`weather` を設定しているため、コピーする場合はそれらのキーを削除してからにしてください。

<a id="4-control-the-running-session"></a>
## 4. 実行中のセッションを制御する

同じディレクトリの 2 つ目のコンソールから実行します（インストール環境の引数は不要です）。

```text
OmsiLaunch.exe session status --json
OmsiLaunch.exe events watch
OmsiLaunch.exe time get
OmsiLaunch.exe vehicles list
OmsiLaunch.exe vehicles get --handle=rv-000001
```

これらのコマンドは、このインストール環境のローカル制御パイプを経由します（[ローカル制御](../reference/local-control.md)）。終了コード `4` は、ここでオーナーが実行されていないことを意味します。

<a id="5-stop"></a>
## 5. 停止する

以下のいずれの方法でも、セッションは同じように終了します（OMSI が終了させられ、次に変更されたすべてのファイルが復元され、その後ジャーナルとバックアップが削除されます）。

| 方法 | 注記 |
|---|---|
| トレイアイコン → `End session`（セッションを終了する項目）→ 確認 | `OmsiLaunch.exe` と `OmsiLaunchW.exe` のどちらのセッションでも利用できます。 |
| `OmsiLaunch.exe session stop` | 別のコンソールから実行します。すぐに戻り、復元はオーナーが完了させます。 |
| オーナーのコンソールで Ctrl+C | 停止を要求します。オーナーは復元を待ってから終了します。 |
| `/observe-seconds:<n>` | `Running` の `n` 秒後に自動停止します。 |
| OMSI が自ら終了する | オーナーが `ProcessExited` を検出して復元します。 |

OMSI 自身のシャットダウン処理は実行されないため、OMSI は終了時に `options.cfg` を書き換えません。これは復元を正確にするための意図的な動作です。オーナーのコンソールウィンドウを X ボタンで閉じた場合、復元に与えられる時間は 4 s だけです。完了しなかった場合は、次回の開始時（または `OmsiLaunch.exe /recover`）にジャーナルから復元が完了されます。セッションが `Completed` で終了した場合、オーナーの終了コードは `0` です。

<a id="6-where-to-look-afterwards"></a>
## 6. 終了後に確認する場所

| 場所 | 内容 |
|---|---|
| コンソール / `--json` 出力 | プラン、`Running` ステータス、最終ステータス（`"State": 18` = `Completed`）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | セッションのホストトレース（トランザクションの境界、プロセスの起動、プラグインへのハンドオフ、ゲームプレイへの移行、復元）。 |
| `<root>\.omsilaunch\diagnostics\<sessionId>-runtime-operation.json` | オーナーが実行した `/runtime:` 操作の結果。 |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | トレイインジケーターのイベント。 |
| `OmsiLaunch.exe /recovery-status` | 正常に終了した後は `"pending": false`。`true` はジャーナルが残っていることを意味します。`OmsiLaunch.exe /recover` を実行してください。 |

セッションがゲームプレイに到達しなかった場合、最終ステータスには失敗の原因となった `OL_E_` 診断（例: `OL_E_STARTUP_TIMEOUT`、`OL_E_PLUGIN_NOT_LOADED`、`OL_E_PROCESS_EXITED_EARLY`）が含まれ、終了コードは `1` となり、それでもファイルは復元されています。[終了コード](../reference/exit-codes.md)、[エラー](../reference/errors.md)、[既知の制限事項](../reference/known-limitations.md)を参照してください。

<a id="running-without-a-console"></a>
## コンソールなしで実行する

```text
OmsiLaunch.exe /silent /new /map:maps\Grundorf\global.cfg /entrypoint-index:1
```

これは `OmsiLaunchW.exe` に処理を委譲し、ただちに `0` を返します。セッションにはコンソールがなく、失敗はメッセージボックスとして表示され、目に見える表示はトレイアイコンだけです。セッションを追跡するには、`session status`、`events watch`、および診断ディレクトリを使用してください。Windows ホストの完全な動作は [OmsiLaunchW.exe リファレンス](../reference/omsilaunchw.md)にあります。
