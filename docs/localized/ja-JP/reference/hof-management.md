# HOF 管理

<!-- l10n: source=reference/hof-management.md source-sha256=3127b5f644162d8ac6258531f4b9dd03f4a2985d047eca1609b10fade5432462 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/hof-management.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OMSI は、各車両のフォルダーから HOF ファイル（`*.hof`: 車両の IBIS／行先表示器に表示される行先、系統番号、停留所リスト）を読み込みます。HOF 管理は、HOF ファイルを **1 つのセッションの期間中だけ** 車両フォルダーにコピーします。これにより、マップやアドオンはその路線に必要な HOF を同梱でき、ユーザーはある車両の HOF を他の車両で再利用できます。どの車両も恒久的には変更されません。このページは規範的なリファレンスであり、`src/OmsiLaunch.Core/HofPlanner.cs` の `HofPlanner`、[LaunchSpec](launchspec.md) の `HofManagement` フィールド、[CLI](cli.md) の `/hof-source`、`/hof-target`、`/hof-file`、`/hof-mode` フラグ、および[セッションプロファイル](session-profiles.md)の `hofs` ブロックによる実装に対応しています。

安定性: プランニング、競合ルール、閉じ込め（confinement）、およびトランザクションによる復元は `STABLE_BETA` です（オフラインゲート `hof.plan-sources-and-modes`、`hof.plan-rejects-unsafe-input`、`hof.transaction-exact-restore`、`hof.recovery-after-crash`、`session-profiles.hofs`）。ランタイム検証済み: ミラーセッションの正確な復元と、`/recover` によるクラッシュ後のリカバリ（[ランタイム検証の状況](../status/runtime-validation-status.md)を参照してください）。

HOF 管理は `PlayerVehicle.Hof` / `/hof:<id>` とは別のものです。後者は開始時にプレイヤー車両の HOF を選択するもので、このビルドでは `UNAVAILABLE` です。

<a id="model"></a>
## モデル

リクエストは操作のリスト（`HofManagementSpec.Operations`）です。各 `HofOperationSpec` は次の項目を持ちます。

| フィールド | 意味 |
| --- | --- |
| `SourceKind`, `Source` | HOF ファイルの取得元（下表を参照）。 |
| `TargetVehicles` | インストール済みの 1 台以上の車両。`.bus` または `.ovh` ファイルの識別子（`Vehicles\MAN_SD200\MAN_SD77.bus`）または車両フォルダー（`Vehicles\MAN_SD200`）で指定し、いずれも `Vehicles\` の配下にある必要があります。同じフォルダー内の複数の識別子は、そのフォルダーを 1 回だけ対象とします。 |
| `Mode` | `Add`（既定）: ソースの HOF を各ターゲットフォルダーにコピーします。フォルダー内の他の HOF はそのまま残ります。`Mirror`: ソースの HOF をコピーし、セッションの間ターゲットフォルダーのその他すべての `.hof` を削除します。これにより、フォルダーの HOF セットがソースのセットと一致します。 |
| `Files` | ソースから取得するファイル名の任意のリスト（大文字と小文字を区別しません）。省略した場合: ソース直下のすべての `*.hof`。ソースに含まれない名前を指定した場合: `OL_E_HOF_SOURCE_NOT_FOUND`。 |

| `SourceKind` | `Source` | 解決先 |
| --- | --- | --- |
| `Collection` | 単純な名前（例: `berlin`） | `<root>\.omsilaunch\hof\<name>\*.hof`: ユーザーまたはツールが管理するコレクション。 |
| `Directory` | インストールルートからの相対ディレクトリ（例: `maps\RMG Leste\hof`） | そのディレクトリ内の `*.hof`: マップやアドオンのコンテンツと一緒に配布される HOF。 |
| `Vehicle` | ターゲットと同様の車両識別子またはフォルダー | その車両フォルダー内の `*.hof`: ある車両の HOF を他の車両で再利用します。 |
| `File` | インストールルートからの相対パスで指定する `.hof` ファイル | その 1 つのファイル。 |

使用されるのはソースディレクトリ直下のファイルのみです（再帰はしません）。シンボリックリンクとジャンクションはたどりません（[閉じ込め](#confinement)を参照してください）。

<a id="what-a-session-does"></a>
## セッションで行われること

1. **プランニング**（`PlanSessionAsync`、`/plan`）。すべての操作が解決されます。ソースが読み込まれ、ターゲットが確認され、競合が検出されます。変更される各ファイルは `SessionPlan.PlannedMutations` に、`SemanticKey` が `hof.add` または `hof.mirror`、`RequestedValue` がソースの識別子、`Operation` が `exact-file-overlay`（書き込み）または `session-deletion`（ミラーによる削除）として現れます。それらのパスは `TouchedFiles` に含まれます。問題がある場合、プランは下記のコードで実行不可になります。
2. **開始**。失効したジャーナルがリカバリされた後、操作は実際のファイルから改めて解決され、セッショントランザクションに渡されます。書き込まれるファイルはオーバーレイ、ミラーによって取り除かれるファイルはセッション削除になります。バイト内容がすでにソースと等しいターゲットファイルは変更されず、トランザクションのエントリにもなりません。
3. **セッションの終了、失敗、クラッシュ。** トランザクションはすべてのターゲットフォルダーを正確に復元します。既存だったファイルは元のバイト内容、タイムスタンプ、属性に戻り、セッションが作成したファイルは削除され、ミラーによって取り除かれたファイルは元に戻ります。オーナーがクラッシュした場合も、次回の開始時または `/recover` によって同じ復元が実行されます（[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照してください）。

ターゲットフォルダーの外には何も書き込まれません。ソースファイルは読み込まれるだけです。`.omsilaunch\hof\` がセッションによって変更されることはありません。

<a id="conflict-rules"></a>
## 競合ルール

操作は順番に適用され、互いに矛盾してはなりません。

- 同じターゲットファイルに同じバイト内容を書き込む 2 つの操作はマージされます。バイト内容が異なる場合: `OL_E_HOF_CONFLICT`。
- 先行する操作が書き込むファイルを削除するミラー、または先行するミラーが削除するファイルに書き込む操作: `OL_E_HOF_CONFLICT`。
- ミラーはフォルダーのセッション HOF セット、つまり既存のファイルに先行する操作がそこに書き込むファイルを加えたものを対象とします。
- セッションのプレゼンテーション（スプラッシュ画面または Internet Textures）によっても書き込まれる HOF ターゲット: `OL_E_HOF_CONFLICT`（現在のプレゼンテーションのパスは `Vehicles\` の外にあるため、実際には発生しません）。
- ターゲットに大文字と小文字だけが異なる同名のファイルがすでにある場合は、既存の表記が維持されます。

<a id="confinement"></a>
## 閉じ込め

すべてのソースとターゲットはインストール環境の内部にとどまる必要があります。ルート付きパス、`..` セグメント、およびルートの外に出るパスは `OL_E_HOF_PATH_ESCAPE` です。`Vehicles\` の外にあるターゲット（および `Vehicle` ソース）、単純なディレクトリ名ではないコレクション名、ジャンクションまたはシンボリックリンクを経由して到達するソースまたはターゲットのフォルダーも同様です。それ自体がリパースポイントである `.hof` ファイルは無視されます。

<a id="limits"></a>
## 上限

| 上限 | 値 | エラー |
| --- | --- | --- |
| 1 つの HOF ファイルのサイズ | 8 MiB | `OL_E_HOF_INVALID` |
| 操作あたりのファイル数 | 256 | `OL_E_HOF_INVALID` |
| セッションあたりの書き込み総バイト数 | 256 MiB | `OL_E_HOF_INVALID` |
| 操作あたりのターゲット数 | 1 以上 | `OL_E_HOF_INVALID` |

<a id="error-codes"></a>
## エラーコード

| コード | 原因 |
| --- | --- |
| `OL_E_HOF_SOURCE_NOT_FOUND` | コレクション、ディレクトリ、車両、またはファイルが存在しない、`.hof` を含まない、または `Files` で指定されたファイルがない。 |
| `OL_E_HOF_TARGET_NOT_FOUND` | ターゲットの車両ファイルまたはフォルダーが存在しない。 |
| `OL_E_HOF_PATH_ESCAPE` | パスがインストール環境の外に出る、ターゲットが `Vehicles\` の配下にない、コレクション名が単純な名前ではない、またはジャンクション／シンボリックリンクを経由している。 |
| `OL_E_HOF_CONFLICT` | 2 つの操作が 1 つのターゲットファイルについて矛盾している（[競合ルール](#conflict-rules)を参照）。 |
| `OL_E_HOF_INVALID` | ターゲットがない、ソースがない、`.hof` ではない `File` ソース、`.bus`/`.ovh` 以外のファイルであるターゲット、または[上限](#limits)の超過。 |

<a id="using-it"></a>
## 使い方

コマンドライン（1 つの操作。複数の操作には `/spec` またはセッションプロファイルが必要です）:

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:collection:berlin /hof-target:Vehicles\MAN_SD200\MAN_SD77.bus /plan
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /hof-source:vehicle:Vehicles\MAN_SD200\MAN_SD77.bus /hof-target:Vehicles\MAN_SD202 /hof-mode:mirror
```

LaunchSpec（`/spec` の JSON、列挙型は整数）:

```json
{
  "HofManagement": {
    "Operations": [
      { "SourceKind": 1, "Source": "maps\\Grundorf\\hof", "TargetVehicles": [ "Vehicles\\MAN_SD200\\MAN_SD77.bus" ], "Mode": 0 },
      { "SourceKind": 0, "Source": "berlin", "TargetVehicles": [ "Vehicles\\MAN_SD202" ], "Mode": 1, "Files": [ "Linie5.hof" ] }
    ]
  }
}
```

セッションプロファイルのプリセット（`package` ソースはプロファイルパッケージ内のディレクトリです）:

```yaml
hofs:
  - package: hof
    targets:
      - Vehicles\MAN_SD200\MAN_SD77.bus
    mode: mirror
```

API: `LaunchSpec.HofManagement` を設定し、通常どおり `PlanSessionAsync` / `StartSessionAsync` を呼び出します。`/list:Hofs`（`ContentQueryKind.Hofs`）はインストール済みの HOF ファイルを一覧表示します。

関連項目: [LaunchSpec](launchspec.md)、[セッションプロファイル](session-profiles.md)、[CLI](cli.md)、[エラー](errors.md)、[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)。
