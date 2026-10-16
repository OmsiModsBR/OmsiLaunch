# 常駐プラグイン

<!-- l10n: source=concepts/permanent-plugin.md source-sha256=f18a0812b1fa4194aa62d0fa6366ba35b3140f830d4c9b12f57c2f82e2cfb8c3 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../concepts/permanent-plugin.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OmsiLaunch は、製品の一部として `plugins\OmsiLaunch.*` 以下に一度だけインストールされるプラグインを通じて、OMSI プロセスの内部から OMSI を制御します。このプラグインがセッションによってステージング、コピー、スナップショット取得、復元、削除されることは決してありません。このページでは、クロージャーに何が含まれるか、OMSI がそれをどのように読み込むか、ホストが毎回の開始前にそれをどのように検証するか、ホストとプラグインがどのように通信するか（ハンドオフ、テレメトリ、ランタイムメールボックス）、そして OmsiLaunch を介さずに OMSI が起動された場合にプラグインが何をするかを説明します。出典: `src/OmsiLaunch.Process/RuntimeDeployment.cs`（`RuntimeArtifactSet`、`ReleaseManifest`、3 つの共有メモリストア）、`src/OmsiLaunch.Plugin/CurrentDnneAdapter.cs`、`src/OmsiLaunch.Plugin/PluginRuntime.cs`、`src/OmsiLaunch.Plugin/OmsiLaunch.Plugin.opl`、`src/OmsiLaunch.Api/StartupHandoff.cs`、`src/OmsiLaunch.Api/RuntimeControlProtocol.cs`。

<a id="the-closure-9-files"></a>
## クロージャー（9 ファイル）

| `plugins\` 以下のファイル | 役割 |
| --- | --- |
| `OmsiLaunch.Plugin.opl` | OMSI のプラグイン記述子です。内容は `[dll]` の後に `OmsiLaunch.PluginNE.dll` が続くものです。 |
| `OmsiLaunch.PluginNE.dll` | DNNE 2.0.6 によって生成されたネイティブ x86 のエクスポートシムです。OMSI のプラグイン ABI（`PluginStart`、`PluginFinalize`、`AccessVariable`、`AccessTrigger`、`AccessStringVariable`、`AccessSystemVariable`）をエクスポートし、.NET ランタイムをホストします。製品バージョンリソースを持ちます。 |
| `OmsiLaunch.Plugin.dll` | マネージドプラグイン（`net6.0-windows`、x86）です: `CurrentDnneAdapter`、`PluginRuntime`、`CurrentRuntimeControl`、`CurrentTelemetrySink`。 |
| `OmsiLaunch.Plugin.deps.json` | プラグイン用の .NET 依存関係マニフェストです。 |
| `OmsiLaunch.Plugin.runtimeconfig.json` | .NET ランタイムの構成です（フレームワーク `Microsoft.NETCore.App` 6.0、`win-x86`）。 |
| `OmsiLaunch.Api.dll` | ホストと共有するワイヤーフォーマットと公開レコードです。 |
| `OmsiLaunch.Builds.Omsi23004.dll` | ビルドプロファイルです: 実行ファイルのフィンガープリント、グローバル変数、オブジェクトレイアウト、メソッドアドレス。 |
| `OmsiLaunch.Interop.dll` | プロファイルに基づいて構築された、プロセス内のメモリリーダーとライターです。 |
| `OmsiLaunch.Native.x86.dll` | ネイティブブリッジ（C++）です: ビルド検証、ヘッドレス開始フック、時刻の適用、MakeVehicle、PlaceRandomBus、Internet Textures の抑制、D3D9 デバイスへのアクセス。 |

`RuntimeArtifactSet.Load` は、コントローラー自身の `plugins\` ディレクトリからこの一覧を導出します。対象は、名前で指定された 4 つのファイル（`.opl`、`PluginNE.dll`、`deps.json`、`runtimeconfig.json`）、そのディレクトリ内の `OmsiLaunch.PluginNE.dll` を除くすべての `OmsiLaunch.*.dll`、およびネイティブブリッジです。この中に `OmsiLaunch.Plugin.dll` が含まれている必要があります。任意の DLL が OMSI に取り込まれることは決してありません。リリースパッケージャー（`tools/New-ReleasePackage.ps1`）は、上記の 9 ファイルを正確に書き出します。

<a id="how-omsi-loads-it"></a>
## OMSI による読み込み

1. OMSI は `plugins\*.opl` を列挙し、`OmsiLaunch.Plugin.opl` で指定された DLL である `OmsiLaunch.PluginNE.dll` を読み込みます。
2. DNNE シムが、`OmsiLaunch.Plugin.runtimeconfig.json` に記述された x86 の .NET 6 ランタイムを OMSI プロセス内で起動し、`OmsiLaunch.Plugin.dll` のマネージドエクスポートを解決します。
3. OMSI が `PluginStart` を呼び出します。OMSI は起動中にこれを複数回呼び出す場合がありますが、有効なのは最初の呼び出しだけです（`Interlocked.Exchange` によるガード）。開始に成功すると、1 回限りのネイティブフックを所有するためです。以降の呼び出しは即座に戻ります。
4. `PluginStart` はまず `OMSILAUNCH_INTERNET_TEXTURES_MODE` を確認します。これが `Disabled` の場合、ネイティブのダウンローダー抑制が適用されます（`internet-textures.suppressed`、または `internet-textures.suppression.failed`）。
5. `PluginRuntime.Start` がハンドオフ（後述）を読み取り、プロセス内でビルドを検証し、ヘッドレス開始フックを有効化し、ランタイムメールボックスを開き、`SetTimer` のコールバックを通じて OMSI の UI スレッド上でワールドの開始をスケジュールします。ランタイムコマンドが IPC ワーカースレッド上で実行されることは決してありません。すべての処理はタイマーコールバック上、すなわち OMSI の本来の UI スレッド上で実行されます。
6. `PluginFinalize` がタイマーを停止し、ランタイムをシャットダウンし（`NativeD3DShutdown`）、Internet Textures のパッチを元に戻します。

`AccessVariable`、`AccessTrigger`、`AccessStringVariable`、`AccessSystemVariable` の各エクスポートは空です。OmsiLaunch は OMSI のスクリプト変数用プラグインチャネルを使用しません。

<a id="integrity-validation-before-every-start"></a>
## 毎回の開始前の整合性検証

`RuntimeArtifactSet.ValidateInstalled` は、`StartSessionAsync` の間（早期リカバリの後、トランザクションの準備の前）と、`PlanSessionAsync` の間（`LoadArtifacts` を介した存在確認のみ）に実行されます。プラン診断 `plugin.integrity.reference` は、どの参照が使われたかを報告します。

| 状況 | 参照 | ファイルごとのチェック | エラー |
| --- | --- | --- | --- |
| `OmsiLaunch.exe` と同じ場所に `release-manifest.json` がある（インストール済みパッケージ） | `manifest` | インストールされたファイルが存在し、その SHA-256 が `plugins/<name>` のマニフェストエントリと等しい必要があります | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（必要なファイルのエントリがマニフェストにない）、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| マニフェストがない（開発用レイアウト） | `self` | 存在確認と自己整合性: インストールされたファイルのハッシュが、コントローラー自身の `plugins\` ディレクトリ内のファイルと一致する必要があります | `OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` |
| マニフェストが読み取れない、または不正な形式（`files` 配列がない、`path`/`sha256` のないエントリがある） | | | `OL_E_RELEASE_MANIFEST_INVALID` |
| コントローラーのディレクトリでクロージャーが不完全 | | | `OL_E_RUNTIME_ARTIFACT_MISSING`（プランは実行不可）。`plugins\OmsiLaunch.Plugin.opl` または `OmsiLaunch.Native.x86.dll` がない場合、CLI はさらに `OL_E_RUNTIME_INSTALLATION_INCOMPLETE` を報告します |

使用されるのはマニフェストの `plugins/` エントリだけです。マニフェストはデータであり、ポリシーではありません。マニフェストはリリースパッケージャーによって、ステージングされたすべてのファイルの SHA-256 とともに生成されます。コントローラーが OMSI ルートから実行される場合（リリースのレイアウト）、コピー元とコピー先は同じディレクトリになります。したがってマニフェストがない場合、チェックは存在確認と自己整合性にまで弱まります。リリースパッケージに必ず `release-manifest.json` が含まれているのはこのためです。不一致の解消方法: `plugins\` と `release-manifest.json` が一致するよう、パッケージを再インストールしてください。

何かにパッチを当てる前に、プラグインはビルドカタログのすべてのネイティブターゲットを、読み込まれたイメージに対して解決して検証し、検証済みのテーブルを OMSI のアドレスを一切含まない `OmsiLaunch.Native.x86.dll` に渡します（テレメトリ `native.capabilities`）。`boot.headless-start` またはワールドモードのケイパビリティが利用できない開始は `plugin.capability.unresolved`（`OL_E_CAPABILITY_UNRESOLVED`）で失敗し、想定外の状態にある実際の Start フォームは `plugin.build.invalid`（`OL_E_BUILD_VALIDATION_FAILED`）で失敗します。[互換性](../reference/compatibility.md)を参照してください。

<a id="host-to-plugin-environment-variables"></a>
## ホストからプラグインへ: 環境変数

`CreateProcessW` は、親の環境変数に以下を加えて `Omsi.exe` を起動します。

| 変数 | 値 | 利用側 |
| --- | --- | --- |
| `OMSILAUNCH_SESSION_ID` | セッション GUID（`D` 形式） | `CurrentRuntimeControl` がこれを使って D3D ハンドルにタグを付けます（`N` 形式） |
| `OMSILAUNCH_HANDOFF_NAME` | `OmsiLaunch.Handoff.<sessionId N>` | `PluginRuntime.Start` がこのマッピングを読み取り専用で開きます |
| `OMSILAUNCH_TELEMETRY_NAME` | `OmsiLaunch.Telemetry.<sessionId N>` | `CurrentTelemetrySink.Emit` |
| `OMSILAUNCH_RUNTIME_CHANNEL` | `OmsiLaunch.Runtime.<sessionId N>` | `CurrentRuntimeCommandMailbox` |
| `OMSILAUNCH_INTERNET_TEXTURES_MODE` | `Native`、`Disabled`、または `Override` | `PluginStart`（プロセス内で効果があるのは `Disabled` のみ） |

3 つのマッピングは、プロセスの開始前にホストによって作成され（`CurrentStartupHandoffStore`、`CurrentTelemetryStore`、`CurrentRuntimeCommandStore`）、セッションのライフサイクルタスクが終了したときに破棄されます。これらは起動したユーザーの既定の DACL を持つ名前付きカーネルオブジェクトであり、同じユーザーのプロセスであればどれでも開くことができます（受容済みの同一ユーザー信頼モデルです。[既知の制限事項](../reference/known-limitations.md)を参照）。

<a id="the-startup-handoff"></a>
## 起動ハンドオフ

メモリマップされた、読み取り専用の固定レイアウトのレコードです（`StartupHandoffWire`、マジック `OLSH`、バージョン 4。バージョン 3 もリーダーは引き続き受け入れます）。64 バイトのヘッダーには、マジック、バージョン、ヘッダーサイズ、全体サイズ、セッション GUID、ペイロードサイズ、ペイロードの SHA-256 が含まれます。ペイロードには、`BuildProfileId`、`MapIdentity`、`EntrypointIdentity`、`SituationIdentity`（長さプレフィックス付き UTF-8）、`PresentedEntrypointIndex`、`WorldMode`、フラグ（`HeadlessStart`、`PlayerVehicleEnabled`）、`DateMode`、`TimeMode` が含まれます。プラグインはペイロードのハッシュを再計算し、不一致があれば拒否します（`plugin.handoff.invalid`、ホスト側のエラーは `OL_E_PLUGIN_PROTOCOL_MISMATCH`）。1 MiB を超えるマッピングやサイズに矛盾のあるマッピングも同様に拒否されます。

プラグインがハンドオフを受け入れるのは、`WorldMode` が `NewMap` または `SavedSituation` であり、`HeadlessStart` がセットされ、`PlayerVehicleEnabled` がクリアされ、日付と時刻のモードがどちらも `Unset` であり、保存済みシチュエーションの場合はその `.osn` が指定されているときだけです。それ以外はすべて `plugin.request.unsupported`（ホスト側のエラーは `OL_E_CAPABILITY_UNAVAILABLE`）になります。ホストは常に `HeadlessStart` をセットします。

<a id="telemetry-slot"></a>
## テレメトリスロット

`OmsiLaunch.Telemetry.<session>` はイベントリング（Beta 4、`TelemetryRingWire`）です。64 バイトのヘッダー（マジック `OLTR`、バージョン 1、スロット数、スロットサイズ、コミット済みシーケンス）と、2048 バイトの番号付きスロット 64 個からなり、各スロットは UTF-8 JSON イベント 1 件 `{ "name": ..., "data": { ... }, "tick": <ms> }` を保持します（`tick` はプラグインの単調増加するセッションクロックです）。書き込み側はプラグインのみで、スロットを無効化し、書き込み、そのスロットのシーケンスをコミットし、続いてリングのシーケンスを公開します。ホストは 100 ms ごとに、前回の読み取り以降にコミットされたすべてのイベントを順番に読み取ります。コピー中に上書きされたスロットは失われたものとして数えられ、書き込み途中のまま渡されることはありません。64 イベントを超えて遅れたホストには、正確な件数を伴うランタイムイベント `telemetry.lost` が渡されます。スロットより大きいイベントは、それを示す `telemetry.oversized` に置き換えられます。イベントは `SessionStatus.RuntimeEvents`（最新 256 件に制限）に追加され、セマンティックなライフサイクル（`plugin.started`、`world.starting`、`gameplay.entered`、モジュールおよび失敗のイベント）を駆動します。

<a id="runtime-command-mailbox"></a>
## ランタイムコマンドメールボックス

`OmsiLaunch.Runtime.<session>` は 64 KiB のシングルフライトのメールボックスです: `state`（オフセット 0 の int32。0 はアイドル、1 は要求済み、2 は応答済み）、`length`（オフセット 4 の int32）、オフセット 8 からのエンベロープ。エンベロープは `RuntimeCommandWire` レコードです（マジック `OLRC`、バージョン 1、種別、全体長、セッション GUID、リクエスト ID、ペイロード長、UTF-8 JSON ペイロードの SHA-256 を含む 72 バイトのヘッダー）。プラグインは UI スレッドのタイマー（ワールドの読み込み後は 50 ms）からメールボックスをポーリングし、そのスレッド上でコマンドを実行し、スロットがまだ同じリクエスト ID を保持している場合にのみ応答を公開します。ホストがタイムアウトで放棄したリクエストに応答が返されることはありません。サイズ超過の応答は、型付きの `OL_E_RUNTIME_RESPONSE_TOO_LARGE` エラーに置き換えられます。詳細とタイムアウトについては[ランタイム制御](../reference/runtime-control.md)を参照してください。

<a id="dll-search-policy"></a>
## DLL の検索ポリシー

`OmsiLaunch.Plugin`、`OmsiLaunch.Interop`、`OmsiLaunch.Process` および CLI のアセンブリは、`[assembly: DefaultDllImportSearchPaths(DllImportSearchPath.AssemblyDirectory | DllImportSearchPath.System32)]` を宣言しています。ネイティブのインポート（`OmsiLaunch.Native.x86.dll`、`user32.dll`、`kernel32.dll`）は、アセンブリ自身のディレクトリ（`plugins\`）または Windows のシステムディレクトリからのみ解決され、OMSI ルートや `PATH` が探索されることはありません。したがって、`OmsiLaunch.Native.x86.dll` は `plugins\` からのみ読み込まれ、それ以外の場所から読み込まれることはありません。

<a id="when-omsi-is-started-without-omsilaunch"></a>
## OmsiLaunch を介さずに OMSI が起動された場合

クロージャーは常駐しているため、OMSI は Steam やデスクトップからの起動を含め、起動のたびに `OmsiLaunch.PluginNE.dll` を読み込みます。その場合は次のようになります。

- `OMSILAUNCH_INTERNET_TEXTURES_MODE` が存在しないため、ダウンローダーのパッチは適用されません。
- `OMSILAUNCH_HANDOFF_NAME` が存在しないため、`PluginRuntime.Start` は `plugin.handoff.invalid` を発行して `false` を返します。`CurrentTelemetrySink.Emit` は `OMSILAUNCH_TELEMETRY_NAME` が設定されていない場合は即座に戻るため、どこにも何も書き込まれません。
- ビルド検証、ネイティブフック、メールボックス、タイマーはいずれも行われません。プラグインは読み込まれたままですが何も動作せず、OMSI はプラグインが存在しないかのように動作します。
- OMSI の終了時に `PluginFinalize` がネイティブの復元ルーチンと `NativeD3DShutdown` を呼び出しますが、何もインストールされていなければどちらも何もしません。

したがって、OMSI を通常どおり実行するために `.opl` を削除する必要はありません。

<a id="non-interference-with-third-party-plugins"></a>
## サードパーティ製プラグインへの非干渉

セッションが `plugins\` 内の他のファイルを列挙、ハッシュ計算、コピー、削除、復元することは決してありません。プラグインは OMSI の `AccessVariable` チャネルを使用せず、他のプラグインの状態にも触れません。プロセス内で行うパッチは、プロファイルに基づくヘッドレス開始フック（セッションのために有効化される 1 回限りの VMT リダイレクト）、オプションの Internet Textures の抑制（`PluginFinalize` で元に戻されます）、そしてテクスチャのライフサイクル追跡に使われる D3D9 デバイスの `Reset` のインターセプトだけです。

<a id="difference-from-omsihook"></a>
## OmsiHook との違い

OmsiLaunch は、OmsiHook やその任意のバイナリに対して**ランタイム依存関係を一切持ちません**。参照しているパッケージは `DNNE` 2.0.6 と `YamlDotNet` 15.1.2 のみであり、製品内のどこにも `using OmsiHook` や OmsiHook の DLL への P/Invoke はありません。OmsiLaunch が OmsiHook と共有しているのは、そこから得られた知見です。オブジェクトレイアウトといくつかの読み取りラッパーは、固定された OmsiHook のチェックアウト（`space928/Omsi-Extensions`、コミット `7687b6623f5f74b4419695257bd2a4eef54dd93e`、LGPL-3.0-only）から、正確な `Omsi23004_692EBFBF` 実行ファイルと照合して整合させたものです。帰属表示とライセンス条項は `THIRD-PARTY-NOTICES.md` に、ファイルごとの再利用マトリクスは `third_party/OMSIHOOK-REUSE-MATRIX.md` にあります。OmsiHook は別プロセスを注入して生のポインターを公開しますが、OmsiLaunch はプロセス内で動作し、セッションスコープの不透明なハンドルのみを公開し、公開される結果からはネイティブアドレスをすべて取り除きます（[ケイパビリティ](../reference/capabilities.md)を参照）。
