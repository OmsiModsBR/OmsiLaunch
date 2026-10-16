# パッケージングとリリースレイアウト

<!-- l10n: source=reference/packaging.md source-sha256=8d5d5b27225188339094529a2435a976635969dd53d614c53d768169087e7ffa -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/packaging.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページでは、OmsiLaunch `0.1.0-beta4` のリリースパッケージについて説明します。`tools\New-ReleasePackage.ps1` が生成するもの、`release-manifest.json` のフィールド、コントローラーが実行時にマニフェストを使って常駐プラグインのクロージャーを検証する方法、OMSI のインストールルートへのパッケージのインストールと削除の方法、使用後に `.omsilaunch` ディレクトリに含まれるもの、および検証スクリプト（`tools\Test-ReleaseIdentity.ps1`、`tools\Test-ReleasePresentation.ps1`、`tools\Invoke-OfflineValidation.ps1`）を扱います。製品のアイデンティティは `OmsiLaunch.Version.props` から取得されます。利用者向けのインストール手順は [インストール](../getting-started/installation.md) を、プラグインクロージャーの実行時の役割は [常駐プラグイン](../concepts/permanent-plugin.md) を参照してください。

<a id="product-identity-omsilaunchversionprops"></a>
## 製品アイデンティティ（`OmsiLaunch.Version.props`）

| プロパティ | 値 | 用途 |
|---|---|---|
| `OmsiLaunchProductName` | `OmsiLaunch` | マニフェストの `product`、Windows の `ProductName` |
| `OmsiLaunchCompanyName` | `LMonteiro` | Windows の `CompanyName` |
| `OmsiLaunchLegalCopyright` | `Copyright © 2026 LMonteiro - https://github.com/lmonteirotech/OmsiLaunch` | Windows の `LegalCopyright` |
| `OmsiLaunchProductVersion` | `0.1.0-beta4` | マニフェストの `product_version`、Windows の `ProductVersion`、アセンブリの情報バージョン（`/version`）、公開 ZIP の名前 |
| `OmsiLaunchManagedVersion` | `0.1.0` | マネージドアセンブリのバージョンの基準 |
| `OmsiLaunchAssemblyVersion` / `OmsiLaunchFileVersion` | `0.1.0.0` | アセンブリバージョンと Windows のファイルバージョン |
| `OmsiLaunchPackageAlias` | `current` | マニフェストの `package_alias`、ステージングフォルダー名とエイリアス ZIP の名前 |

`Directory.Build.props` は `InformationalVersion` をソースリビジョンなしで `OmsiLaunchProductVersion` に設定するため、`OmsiLaunch.exe /version` は正確に `0.1.0-beta4` を出力します。

<a id="build-toolsnew-releasepackageps1"></a>
## ビルド（`tools\New-ReleasePackage.ps1`）

`New-ReleasePackage.ps1 [-Configuration Release|Debug] [-OutputDirectory <dir>] [-AllowOverwritePublished]`（既定の出力先は `artifacts\release`）は、ビルド済みの成果物からパッケージをステージングします。`OmsiLaunch-<product_version>.zip` がすでに存在する場合、`-AllowOverwritePublished` を指定しない限り `artifacts\release` への書き込みは拒否されます。候補パッケージは別のディレクトリに出力します（オフライン検証では `artifacts\candidate\post-round-a` を使用します）。

ステージングの前に、すべてのビルド出力が古くなっていないかチェックされます。

- **`OmsiLaunch.Native.x86.dll`: タイムスタンプではなく内容で判定します。** ネイティブビルドは `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.build-receipt.txt`（`.vcxproj` 内のターゲット `WriteOmsiLaunchNativeBuildReceipt`）を書き出します。これには、生成した DLL の SHA-256（`output=`）と、ビルド元となったすべてのソース（`source=<sha256>|<path>`: `.cpp`、`.rc`、`.vcxproj` および `OmsiLaunch.Version.props`）の SHA-256 が記録されます。パッケージングは、DLL のハッシュが記録された出力と一致しない場合（`does not match its build receipt`: タイムスタンプにかかわらず、古いコピーまたは別のビルドのコピー）、記録されたソースが変更された場合（`Native source changed after the recorded build`）、レシートに含まれないネイティブソースがある場合、またはレシートが存在しない場合に、その DLL を拒否します。
- **シムとマネージドアセンブリ: タイムスタンプで判定します。** それぞれ、自身のプロジェクトのソース（各シムの `.cpp`/`.rc`/`.vcxproj`、各マネージドアセンブリ自身のプロジェクト）より古くてはなりません。古い入力があると、`Stale build artifact` でパッケージングが中止されます。

その後、パッケージは一意の名前を持つ新規のステージングディレクトリ（出力ディレクトリ内の `.staging-<guid>`）で組み立てられるため、以前の実行で残ったファイルがクロージャーに入り込むことはありません。以前の `OmsiLaunch-current` フォルダーとアーカイブは、以下のすべてのチェックに合格した後にのみ置き換えられます。

| ソース | パッケージ内の配置先 |
|---|---|
| `artifacts\bin\OmsiLaunch.Bootstrapper\<cfg>\OmsiLaunch.exe`、`nethost.dll` | `OmsiLaunch.exe`、`nethost.dll` |
| `artifacts\bin\OmsiLaunch.WindowsHost\<cfg>\OmsiLaunchW.exe` | `OmsiLaunchW.exe` |
| `artifacts\bin\OmsiLaunch.Cli\<cfg>\net6.0-windows\`（x64）: `OmsiLaunch.Controller.dll`、`.deps.json`、`.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Configuration.dll`、`OmsiLaunch.Content.dll`、`OmsiLaunch.Core.dll`、`OmsiLaunch.Process.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`YamlDotNet.dll` | ルート |
| `artifacts\bin\OmsiLaunch.Plugin\x86\<cfg>\net6.0-windows\`: `OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json`、`OmsiLaunch.Api.dll`、`OmsiLaunch.Builds.Omsi23004.dll`、`OmsiLaunch.Interop.dll` | `plugins\` |
| `artifacts\x86\<cfg>\OmsiLaunch.Native.x86.dll` | `plugins\OmsiLaunch.Native.x86.dll` |
| CLI の `assets\splash\*.bmp`（`PTB`、`ENG`、`DEU`、`FRA`） | `.omsilaunch\assets\splash\` |
| `examples\release-session.example.json` | `.omsilaunch\examples\release-session.example.json` |
| `docs\examples\session-profiles\rmg-leste\profile.yaml` | `.omsilaunch\examples\session-profiles\rmg-leste\profile.yaml` |
| `LICENSE`、`THIRD-PARTY-NOTICES.md` | ルート |
| `docs\localized\` を除く `docs\` 配下のすべてのファイル（英語ドキュメント、同じディレクトリ構造） | `.omsilaunch\docs\`（これにより、CLI の使用方法テキストに表示されるパス `.omsilaunch\docs\reference\cli.md` が存在します。ドキュメント監査 BUG-08）。`docs\README.md` からリポジトリルートの概要（`PUBLIC-API.md` など）へのリンクは、ソースリポジトリ内でのみ解決されます。 |
| `docs\localized\LOCALIZATION-MANIFEST.md` と、そのマニフェストに記載された各ロケール（`pt-BR`、`pt-PT`、`en-GB`、`fr-FR`、`de-DE`、`es-ES`、`es-LATAM`、`it-IT`、`pl-PL`、`nl-NL`、`ru-RU`、`zh-CN`、`zh-TW` および `ja-JP`）の `docs\localized\<locale>\**` | `.omsilaunch\docs\localized\`（同じ構造）。記載されたロケールが欠けている場合、スクリプトは中止されます |

続いてスクリプトは、ステージングされたすべてのファイルのハッシュを計算し、パッケージルートに `release-manifest.json` を BOM **なし**の UTF-8 で書き出し（結果は PowerShell のエディションに依存しなくなりました）、ステージに対して `Test-ReleasePackageIntegrity.ps1` を実行し、ステージングされた各プラグインファイルと `OmsiLaunch.Native.x86.dll` をビルド出力と再比較し、ステージを圧縮し、**アーカイブを新しい一時ディレクトリに展開して、展開されたクロージャーを同じマニフェストに照らして検証します**（アーカイブ内のマニフェストは検証済みのマニフェストとバイト単位で一致している必要があります）。その後、ステージを `OmsiLaunch-current` として、アーカイブを `OmsiLaunch-current.zip` として公開し、それを `OmsiLaunch-<product_version>.zip`（`OmsiLaunch-0.1.0-beta4.zip`）にコピーして、`<SHA-256>  <file name>` を含む `OmsiLaunch-0.1.0-beta4.zip.sha256` を書き出します。成果物が 1 つでも欠けているとスクリプトは中止されます。このスクリプトはビルドを行いません。先に `Invoke-OfflineValidation.ps1`（または個別の `dotnet build` / MSBuild の手順）を実行してください。

<a id="package-layout"></a>
## パッケージレイアウト

```plaintext
OmsiLaunch.exe                       console shim (x64 native)
OmsiLaunchW.exe                      Windows-subsystem shim (x64 native)
nethost.dll                          .NET host locator used by both shims
OmsiLaunch.Controller.dll            managed controller (x64, net6.0-windows)
OmsiLaunch.Controller.deps.json
OmsiLaunch.Controller.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 + Microsoft.WindowsDesktop.App 6.0
OmsiLaunch.Api.dll  OmsiLaunch.Core.dll  OmsiLaunch.Process.dll  OmsiLaunch.Configuration.dll
OmsiLaunch.Content.dll  OmsiLaunch.Builds.Omsi23004.dll  YamlDotNet.dll
LICENSE  THIRD-PARTY-NOTICES.md
release-manifest.json                package inventory and expected plugin hashes
plugins\                             the permanent plugin closure (9 files, all named OmsiLaunch.*)
  OmsiLaunch.Plugin.opl              OMSI plugin descriptor
  OmsiLaunch.PluginNE.dll            native export shim loaded by OMSI (x86)
  OmsiLaunch.Plugin.dll              managed plugin (x86, net6.0-windows)
  OmsiLaunch.Plugin.deps.json  OmsiLaunch.Plugin.runtimeconfig.json   requires Microsoft.NETCore.App 6.0 (x86)
  OmsiLaunch.Api.dll  OmsiLaunch.Builds.Omsi23004.dll  OmsiLaunch.Interop.dll   x86 copies
  OmsiLaunch.Native.x86.dll          native bridge (loaded from plugins\ only)
.omsilaunch\
  assets\splash\{PTB,ENG,DEU,FRA}.bmp   640x480 24-bit managed splash assets
  docs\                               English documentation (README.md, getting-started\, reference\, concepts\, status\, ...)
  docs\localized\<locale>\             translations of the 0.1.0-beta4 pages (not normative)
  examples\release-session.example.json
  examples\session-profiles\rmg-leste\profile.yaml
```

製品が所有するのは、ルートの製品ファイル、`plugins\OmsiLaunch.*` および `.omsilaunch\` のみです。`plugins\` 配下のサードパーティ製プラグインを、OmsiLaunch が列挙、コピー、ハッシュ計算、削除、復元することは一切ありません。

## `release-manifest.json`

| フィールド | 型 | 意味 |
|---|---|---|
| `product` | string | `OmsiLaunch` |
| `product_version` | string | `0.1.0-beta4` |
| `package_alias` | string | `current` |
| `control_protocol` | string | `0.1`。`PublicCapabilityRegistry.ProtocolVersion` と一致している必要があります |
| `requirement` | string | `OMSI 2` |
| `known_layouts` | object[] | 既知の各レイアウトの `build_id` と `code_sha256`（`Omsi23004`、`13ec110a...c028`）。高速パスであり、要件ではありません |
| `configuration` | string | `Release` または `Debug` |
| `generated_utc` | string | ISO-8601 形式のビルド時刻 |
| `files[]` | object[] | パッケージ内のすべてのファイルについての `path`（スラッシュ区切り、パッケージルートからの相対パス）、`bytes`、`sha256`（大文字の 16 進数） |

マニフェストはデータであり、実行可能なポリシーではありません。コントローラーが読み取るのは `plugins/` のエントリのみです。リーダーは UTF-8 BOM の有無にかかわらずファイルを受け付けます（この修正以前に Windows PowerShell 5.1 で書き出されたマニフェストには BOM が付いています）。

<a id="runtime-use-of-the-manifest-plugin-integrity"></a>
## マニフェストの実行時の利用（プラグインの整合性）

プランの作成と開始のたびに、`OmsiLaunchService.LoadArtifacts` は想定されるプラグインクロージャーを構築します（`RuntimeArtifactSet.Load`、`src\OmsiLaunch.Process\RuntimeDeployment.cs`）。

1. コントローラーは `OmsiLaunch.exe` と同じ場所（`AppContext.BaseDirectory`）で `release-manifest.json` を探します。存在する場合、`ReleaseManifest.TryReadPluginHashes` が `plugins/*` のハッシュを取り出します（ファイルをマニフェストとして読み取れない場合は `OL_E_RELEASE_MANIFEST_INVALID`）。
2. インストールされている各ファイル `<root>\plugins\OmsiLaunch.*` のハッシュ（SHA-256）を計算し、次のように比較します。
   - マニフェストがある場合: マニフェストのハッシュと比較し、プラン診断 `plugin.integrity.reference = manifest` を記録します。ファイルがない → `OL_E_PERMANENT_PLUGIN_MISSING`、ファイルはあるがマニフェストに記載されていない → `OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、ハッシュが異なる → `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`（`reinstall the OmsiLaunch package so plugins\ and release-manifest.json agree`）。
   - マニフェストがない場合（開発用レイアウト、またはマニフェストを省いたインストール環境）: 存在と、コントローラーと同じ場所にあるパッケージ内のコピーとの自己整合性のみをチェックできます。`plugin.integrity.reference = self` となります。
3. 失敗すると、プランは実行不可とマークされる（詳細付きの `OL_E_RUNTIME_ARTIFACT_MISSING`）か、開始が拒否されます（終了コード `7`）。

プラグインファイルがセッションによってステージング、スナップショット取得、復元、削除されることはありません。クロージャーはインストール環境の恒久的な一部です。x86 の `OmsiLaunch.Native.x86.dll` は `plugins\` からのみ読み込まれ、マネージドアセンブリは `DefaultDllImportSearchPaths(AssemblyDirectory | System32)` を宣言しています。

<a id="installation-into-the-omsi-root"></a>
## OMSI ルートへのインストール

1. アーカイブを検証します。`OmsiLaunch-0.1.0-beta4.zip` を `OmsiLaunch-0.1.0-beta4.zip.sha256` と照合してください。
2. アーカイブを **OMSI のインストールルート（`Omsi.exe` があるディレクトリ）に直接**展開します。これにより、ルートのファイル、`plugins\OmsiLaunch.*`（サードパーティ製プラグインと並んで配置）および `.omsilaunch\` が配置されます。
3. `release-manifest.json` は `OmsiLaunch.exe` と同じ場所に置いたままにしてください。これによりマニフェストベースのプラグイン整合性チェックが有効になります。マニフェストとバイナリは同じパッケージのものでなければなりません。古いマニフェストの上に新しいバイナリを置く（またはその逆の）場合、すべての開始が `OL_E_PERMANENT_PLUGIN_HASH_MISMATCH` で失敗します。`Test-ReleasePresentation.ps1 -InstallPackage` は現在、製品ファイルと一緒にマニフェストもコピーします。以前はマニフェストをスキップしていたため、新しいバイナリの横に古いマニフェストが残っていました（Round A RA-007）。
4. `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <zip> -InstallationRoot <root>` で整合性を読み取り専用でチェックします。`installation_comparison.coherent_with_package` が `true` でなければなりません。
5. プラグインのバイナリを `.omsilaunch\` に移動したり、`plugins\OmsiLaunch.*` の名前を変更したりしないでください。
6. `OmsiLaunch.exe /version`、`OmsiLaunch.exe profiles` および `/plan` で確認します（[最初のセッション](../getting-started/first-session.md) を参照）。

既存の `.omsilaunch\assets\splash\*.bmp` ファイルがセッションによって上書きされることはありません（明示的に管理されたアセットセットは保持されます）。新しいパッケージを展開してこれらを上書きすることは、利用者による意図的な操作です。

<a id="the-omsilaunch-directory-after-use"></a>
## 使用後の `.omsilaunch` ディレクトリ

| パス | 作成元 | 存続期間 |
|---|---|---|
| `assets\splash\{PTB,ENG,DEU,FRA}.bmp` | パッケージ、または最初のマネージドスプラッシュセッションでコピー | 恒久的 |
| `docs\`、`examples\` | パッケージ | 恒久的 |
| `session-profiles\<id>\profile.yaml` | 利用者 | 恒久的。[セッションプロファイル](session-profiles.md) を参照 |
| `diagnostics\<sessionId>-host.log` | すべてのセッション | 最新 50 セッション分を保持。新しいセッションの開始時に、それより古いセッション接頭辞付きファイルは削除されます |
| `diagnostics\<sessionId>-runtime-operation.json`、`-runtime-read-batch.json`、`-runtime-write-batch.json`、`-d3d-wave-d-batch.json` | `/runtime`、検証ハーネス | 同じ保持方針（セッション接頭辞） |
| `diagnostics\tray-host.log` | トレイインジケーター | 恒久的、追記 |
| `diagnostics\release-presentation-*.out`、`release-presentation-validation.json` | `Test-ReleasePresentation.ps1` | 恒久的（セッション接頭辞なし） |
| `journal.json` | トランザクション | `Prepared` から `Restored` まで存在します。残っている場合はリカバリが保留中であることを意味します（`/recovery-status`） |
| `backup\<sessionId>\<sha256(path)>.bin` | トランザクション | 変更対象ファイルのスナップショット。復元後に削除されます |

データがマシンの外に送信されることはありません。[トランザクションとリカバリ](../concepts/transactions-and-recovery.md) を参照してください。

<a id="uninstall"></a>
## アンインストール

1. 実行中のセッションがないこと（`OmsiLaunch.exe detect`、`OmsiLaunch.exe session status`）と、保留中のリカバリがないこと（`OmsiLaunch.exe /recovery-status`。`pending` が `true` の場合は `/recover` を実行）を確認し、OMSI のファイルがすでに復元された状態にします。
2. `plugins\OmsiLaunch.Plugin.opl`、`plugins\OmsiLaunch.PluginNE.dll`、`plugins\OmsiLaunch.Plugin.dll`、`plugins\OmsiLaunch.Plugin.deps.json`、`plugins\OmsiLaunch.Plugin.runtimeconfig.json`、`plugins\OmsiLaunch.Api.dll`、`plugins\OmsiLaunch.Builds.Omsi23004.dll`、`plugins\OmsiLaunch.Interop.dll`、`plugins\OmsiLaunch.Native.x86.dll` を削除します。その他のプラグインには手を触れないでください。
3. 上記レイアウトに記載されたルートの製品ファイル（`OmsiLaunch.exe`、`OmsiLaunchW.exe`、`nethost.dll`、`OmsiLaunch.*.dll`、`OmsiLaunch.Controller.*.json`、`YamlDotNet.dll`、`release-manifest.json`、`LICENSE`、`THIRD-PARTY-NOTICES.md`）を削除します。
4. `.omsilaunch\` を削除します（これにより、セッションプロファイルと診断情報も削除されます）。`journal.json` が存在する間は、決して削除しないでください。

完了したセッションは、自身が所有していたすべてのファイルを復元するため、それ以上のクリーンアップは不要です。OMSI 自身が実行中に書き込むファイル（たとえば `options.cfg` 内の `[last_map]`、キャッシュ、`laststn.osn`、ログ）は OMSI の通常の状態であり、元に戻されません。[トランザクションとリカバリ](../concepts/transactions-and-recovery.md) を参照してください。

<a id="validation-scripts"></a>
## 検証スクリプト

| スクリプト | 目的 | OMSI への影響 |
|---|---|---|
| `tools\Invoke-OfflineValidation.ps1 [-Configuration] [-SkipNative] [-SkipDocs]` | 警告をエラーとして扱って `OmsiLaunch.sln` をビルドし、3 つのネイティブプロジェクト（`OmsiLaunch.Native.x86` Win32、`OmsiLaunch.Bootstrapper` x64、`OmsiLaunch.WindowsHost` x64）を MSBuild でビルドした後、すべてのオフラインスイートを実行します: `OmsiLaunch.TestHost`、`OmsiLaunch.UnitTests`、`OmsiLaunch.IntegrationTests`、`OmsiLaunch.ProfileTests`、`OmsiLaunch.WindowsUiTests`、およびスキップされない限り `OmsiLaunch.DocumentationTests`。その後、パッケージングの回帰テスト `Test-PackagingPipeline.ps1` を実行します（`-SkipNative` 指定時はスキップ）。`OFFLINE VALIDATION PASSED`/`FAILED` を出力します。 | なし |
| `tools\New-ReleasePackage.ps1` | 古いビルドの検出、ステージング、マニフェスト、整合性の自己チェック、ZIP、チェックサム（上記参照）。 | なし |
| `tools\Test-ReleasePackageIntegrity.ps1 -PackagePath <dir or zip> [-InstallationRoot <root>]` | マニフェストがパッケージ内のファイルを過不足なく、サイズと SHA-256 が一致する形で列挙していること、必須のクロージャー（3 つの実行ファイル、コントローラー、`OmsiLaunch.Native.x86.dll` を含む 9 つの常駐プラグインファイル）が存在すること、および構成が `Release` であることを検証します。`-InstallationRoot` を指定すると、インストール環境の製品ファイルをパッケージと**読み取り専用で**比較します。終了コード `0` = 整合。 | なし（読み取り専用） |
| `tools\Test-PackagingPipeline.ps1 [-OutputDirectory]` | `artifacts\candidate\post-round-a` 配下のクリーンなステージングから候補パッケージを生成し、ステージングされたクロージャーと再展開されたアーカイブの両方が整合性チェックに合格することを要求します。整合性ゲートが、改ざんされた DLL、改ざんされたまたは古い `Native.x86`、古いプラグインのコピー、記載済みファイルの削除、想定外のファイル、変更されたまたは不正な形式のマニフェストハッシュ、重複エントリ（完全一致、大文字小文字違い、区切り文字違い）、親パスおよび絶対パス、不正な JSON を拒否すること、パッケージャーがビルド出力に置かれた古い `Native.x86`（タイムスタンプが新しくても）とソースが変更されたレシートを拒否すること、そして公開済みアーカイブが決して上書きされないことを証明します。ビルド出力はバイト単位で復元されます。`Invoke-OfflineValidation.ps1` から実行されます。 | なし |
| `tools\Test-ReleaseIdentity.ps1 [-PackagePath]` | ZIP を `artifacts\release\identity-verification` に展開し、`product`/`product_version`/`package_alias` をチェックします。さらに、`nethost.dll` と `YamlDotNet.dll` を除くすべての `.exe`/`.dll` の `ProductName`、`CompanyName`、`LegalCopyright`、`FileVersion`、`ProductVersion`、両シムの `InternalName`/`OriginalFilename`、および `OmsiLaunch.exe` にアイコンが埋め込まれていることを検証します。 | なし |
| `tools\Test-ReleasePresentation.ps1 -InstallationRoot <root> [-PackageDirectory] [-ObserveSeconds 5..60] [-InstallPackage] [-RunOmsi]` | パッケージ化された Release 実行ファイルを実際のインストール環境に対して検証します。マニフェストは `Release` であり、`Debug` や `runtime/plugin/` のパスを含まず、`plugins/OmsiLaunch.*` をインストールするものでなければなりません。4 つのスプラッシュアセットが存在しなければなりません。3 つの `/plan` ケース（マネージドの既定、マネージドのカスタムアセット、`/splash:Unset`）を実行します。`-RunOmsi`（`-InstallPackage` が必要）を指定すると、各ケースを `/observe-seconds` 付きで起動し、セッション中に `GUI\NewSplashscreen_ENG.bmp` と `GUI\NewSplashscreen_PTB.bmp` を監視して、正確な復元、`Omsi.exe` が残っていないこと、`journal.json` がないこと、終了コード `0`、サードパーティ製プラグインのハッシュが変化していないこと、常駐プラグインのセットが変化していないことを確認します。`.omsilaunch\diagnostics\release-presentation-validation.json` を書き出します。 | `-RunOmsi` 指定時はあり（セッション単位、復元済み） |

どちらの `Test-*` スクリプトも、想定バージョンを知るために `OmsiLaunch.Version.props` を読み取ります。
