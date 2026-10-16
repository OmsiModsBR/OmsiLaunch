# インストール

<!-- l10n: source=getting-started/installation.md source-sha256=a547bd23c9342584c75eb8f54ddbc7a43478458dd0863949b72893826df2e136 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../getting-started/installation.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch `0.1.0-beta4` の要件、対応する OMSI ビルド、リリースパッケージを OMSI のインストールルートにインストールする方法、そしてセッションを開始する前に `/version` と `/plan` でインストールを確認する方法を説明します。パッケージの内容は[パッケージング](../reference/packaging.md)で規定されています。最初の起動については[最初のセッション](first-session.md)で説明します。

<a id="requirements"></a>
## 要件

| 要件 | 詳細 | 満たされない場合の失敗 |
|---|---|---|
| Windows 10 以降、64 ビット | コントローラーは `Environment.OSVersion.Version.Major >= 10`、x64 オペレーティングシステム、x64 プロセスであることを検査します。 | `OL_E_UNSUPPORTED_OPERATING_SYSTEM`（または `OL_E_UNSUPPORTED_OS_ARCHITECTURE`）によりプランが実行不可となり、終了コードは `1`/`3` です。 |
| .NET 6 Desktop Runtime、**x64** | `OmsiLaunch.Controller.runtimeconfig.json` は `Microsoft.NETCore.App` 6.0 と `Microsoft.WindowsDesktop.App` 6.0 を必要とします（トレイインジケーターが Windows Forms を使用するため）。シムは `nethost.dll` を使ってこれを探します。 | `OmsiLaunch.exe` は何も出力する前にシムコード `102`..`106` で終了します。`OmsiLaunchW.exe` は `OmsiLaunch could not start the .NET host (code N).` を表示します。 |
| .NET 6 Runtime、**x86** | プラグインは 32 ビットの `Omsi.exe` 内で実行されるため、`plugins\OmsiLaunch.Plugin.runtimeconfig.json` は x86 用の `Microsoft.NETCore.App` 6.0 を必要とします。x86 の .NET 6 Desktop Runtime バンドルでもこの要件を満たします。 | プラグインが OMSI 内で起動せず、セッションは `Running` に到達できません（`OL_E_PLUGIN_NOT_LOADED` / `OL_E_STARTUP_TIMEOUT`）。終了コードは `1` で、ファイルは復元されます。 |
| インストールされた OMSI 2 | 32 ビットの `Omsi.exe`（OMSI 2（2.x）のバージョン）があり、ルートに `maps\` と `Vehicles\` が存在すること。特定の実行ファイルのハッシュは要求されません。OMSI 2.3.004 が既知でランタイム検証済みのレイアウトであり、それ以外のビルドでは各ケイパビリティがプロセス内で解決され検証されます。 | `OL_E_UNSUPPORTED_BUILD`。プランは実行不可となり、終了コードは `1` です。そのビルドで利用できないケイパビリティは `OL_E_CAPABILITY_UNRESOLVED` で失敗します。[互換性](../reference/compatibility.md)を参照してください。 |
| 書き込み可能なインストールルート | トランザクションは `.omsilaunch\`、`GUI\` と `Texture\` 配下のオーバーレイ、および `options.cfg` に書き込み、それらを復元します。ルートは現在のユーザーが書き込み可能でなければなりません（適切な権限なしで `Program Files` を使用することは避けてください）。 | `OL_E_INSTALLATION_NOT_WRITABLE`、終了コード `1`。 |
| インストール環境ごとに 1 ユーザー、1 オーナー | インストールリース `Local\OmsiLaunch.Installation.<sha256(root)>` と制御パイプはログオンセッション単位です。 | `OL_E_INSTALLATION_BUSY` / `OL_E_SESSION_ALREADY_ACTIVE`、終了コード `7`。 |

どちらのランタイムも Microsoft から個別にダウンロードします。.NET 6 の x64 Desktop Runtime と x86 Runtime（または x86 Desktop Runtime）をインストールしてください。これ以外のコンポーネントは必要ありません。データがマシンの外に送信されることはありません。

<a id="confirm-the-omsi-build"></a>
## OMSI ビルドを確認する

`<OMSI_PATH>` を OMSI 2 のディレクトリ（例: `C:\OMSI 2`）に置き換えてください。

```powershell
Get-FileHash '<OMSI_PATH>\Omsi.exe' -Algorithm SHA256
(Get-Item '<OMSI_PATH>\Omsi.exe').Length
```

ハッシュは上記の 2 つのいずれかでなければなりません。インストール後は、`OmsiLaunch.exe profiles` が同じ一覧を検証ステータス付きで出力します。

<a id="install-the-package"></a>
## パッケージをインストールする

1. `OmsiLaunch-0.1.0-beta4.zip` と `OmsiLaunch-0.1.0-beta4.zip.sha256` をダウンロードし、チェックサムを確認します（`Get-FileHash` の結果が `.sha256` ファイル内の値と一致しなければなりません）。
2. アーカイブを **OMSI のインストールルート（`Omsi.exe` を含むフォルダー）に直接**展開します。アーカイブはそのルートに合わせた構成になっています。
   - ルートには `OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.Controller.dll` およびその他の `OmsiLaunch.*.dll` コントローラーアセンブリ、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md` が配置されます。
   - 常駐プラグインのクロージャ `plugins\OmsiLaunch.*`（9 ファイル）は既存のプラグインの隣に配置され、既存のプラグインには一切触れません。
   - `.omsilaunch\` にはスプラッシュ用アセット、オフラインドキュメント、サンプルが含まれます。
3. `release-manifest.json` は `OmsiLaunch.exe` の隣に置いたままにしてください。これにより、開始のたびにインストール済みのプラグインファイルが SHA-256 で検証されます（`plugin.integrity.reference = manifest`）。これがない場合は、存在と自己整合性のみが検査されます（`plugin.integrity.reference = self`）。
4. `plugins\OmsiLaunch.*` 配下のものを移動したり名前を変更したりしないでください。また、プラグインのバイナリを `.omsilaunch\` に置かないでください。

アップグレードも同じ操作です。セッションが実行されておらず、保留中のリカバリもない状態（`OmsiLaunch.exe /recovery-status`）で、新しいパッケージを古いファイルの上に展開します。プラグインのハッシュとマニフェストは常に同じパッケージのものでなければなりません（そうでない場合は `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`）。

<a id="verify"></a>
## 確認する

OMSI ルートから実行します（インストール環境の引数は、既定では `OmsiLaunch.exe` を含むディレクトリになります）。

```text
OmsiLaunch.exe /version
```
期待される結果: `"version": "0.1.0-beta4"`、`"protocol_version": "0.1"`、`"supported_family": "OMSI_2"`、終了コード `0`。終了コード `102`..`106` は、x64 の .NET 6 ランタイムが存在しないか、パッケージが不完全であることを意味します。

```text
OmsiLaunch.exe profiles
OmsiLaunch.exe /list:Maps
```
期待される結果: 対応するハッシュ、続いてこのインストール環境で検出されたマップ。終了コード `0`。

```text
OmsiLaunch.exe /new /map:maps\Grundorf\global.cfg /entrypoint-index:1 /plan
```
期待される結果: `Plan: READY profile=Omsi23004`（既知のレイアウトの場合。それ以外では `unrecognized`）と終了コード `0`（`/list:Maps` の任意のマップ識別子を使用できます。エントリポイントのインデックスはそのマップで提示されたインデックスでなければなりません。`/list:Entrypoints /map:<identity>` を参照してください）。`--json` を付けると、プランには `TouchedFiles`（スプラッシュのオーバーレイ）、`PlannedMutations`、`RequiredCapabilities`（`STATICALLY_VALIDATED`。既知のレイアウトのネイティブ開始ケイパビリティは `RESOLVED_KNOWN_LAYOUT`）、`Diagnostics`（`plugin.integrity.reference` を含む）、`IsRunnable` が一覧されます。終了コード `1` を伴う `Plan: NOT RUNNABLE` の場合は、`Diagnostics` にその理由が示されます（`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_RUNTIME_ARTIFACT_MISSING` など）。プランの作成で OMSI が起動されることはなく、インストール環境に書き込まれることもありません。

<a id="where-things-live-afterwards"></a>
## インストール後のファイルの場所

| パス | 内容 |
|---|---|
| `<root>\.omsilaunch\diagnostics\<sessionId>-host.log` | 各セッションのホストトレース（新しい 50 セッション分を保持） |
| `<root>\.omsilaunch\diagnostics\tray-host.log` | トレイインジケーターのログ |
| `<root>\.omsilaunch\journal.json`、`backup\<sessionId>\` | トランザクションが保留中の間のみ存在します。[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)を参照してください |
| `<root>\.omsilaunch\session-profiles\<id>\profile.yaml` | 利用者が事前定義したセッションプロファイル。[セッションプロファイル](../reference/session-profiles.md)を参照してください |
| `<root>\.omsilaunch\assets\splash\` | 管理対象のスプラッシュ用アセット |
| `<root>\.omsilaunch\docs\` | このドキュメントのオフライン版（`README.md` から読み始めてください。CLI リファレンスは `reference\cli.md` です） |
| `<root>\.omsilaunch\examples\` | LaunchSpec とセッションプロファイルのサンプル |

<a id="uninstall"></a>
## アンインストール

セッションをすべて停止し、`OmsiLaunch.exe /recovery-status` を実行し（保留中であれば `/recover` も実行し）、その後ルートの製品ファイル、`plugins\OmsiLaunch.*`、`.omsilaunch\` を削除します。詳細は[パッケージング](../reference/packaging.md)を参照してください。

<a id="next"></a>
## 次のステップ

[最初のセッション](first-session.md) · [CLI リファレンス](../reference/cli.md) · [既知の制限事項](../reference/known-limitations.md)
