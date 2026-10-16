# 互換性

<!-- l10n: source=reference/compatibility.md source-sha256=bd2e58f38c45a35819751b568f389c9b567f45ecef4a5a5f5a1b68e2f1160907 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/compatibility.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

OmsiLaunch は、インストールされた **OMSI 2** で動作します。特定の `Omsi.exe` ファイルを必要とはしません。OMSI 内部に対して行うすべてのネイティブな前提（関数、パッチサイト、クラス、VMT スロット、グローバル変数、フィールドオフセット）は、使用される前にプロセス内で個別に解決され検証され、ケイパビリティはその前提がすべて検証された場合にのみ利用可能になります。このページでは、「OMSI 2 がインストールされている」とは何を意味するか、ビルドがどのように識別されるか、ケイパビリティがどのように解決されるか、そしてオペレーティングシステムとランタイムの要件を示します。ソース: `src/OmsiLaunch.Builds.Omsi23004/OmsiBuildDetection.cs`、`src/OmsiLaunch.Builds.Omsi23004/NativeTargetCatalog.cs`、`src/OmsiLaunch.NativeResolution/`、`src/OmsiLaunch.Plugin/PluginNativeTargets.cs`、`src/OmsiLaunch.Core/SessionPlanner.cs`、`src/OmsiLaunch.Process/RuntimePlatform.cs`。

<a id="requirement-omsi-2-installed"></a>
## 要件: OMSI 2 がインストールされていること

プランが `OL_E_UNSUPPORTED_BUILD` で拒否されるのは、インストール環境が OMSI 2 のインストール環境ではない場合のみです。

| 検査 | 拒否される条件 |
| --- | --- |
| `Omsi.exe` | インストールルートにない |
| イメージ | 32 ビット x86 の PE イメージではない |
| バージョン | バージョンリソースが OMSI 2 のバージョン（2.x）を持たない |
| インストール環境 | ルートに `maps\` または `Vehicles\` ディレクトリがない |

実行ファイルの SHA-256、サイズ、および正確なバージョンは、互換性の基準では**ありません**。これらは診断（`omsi.build.detected`、`OmsiLaunch.exe profiles`）として記録され、エビデンスとして使われます。

<a id="build-identity-and-capability-resolution"></a>
## ビルドの識別とケイパビリティの解決

| 概念 | 意味 |
| --- | --- |
| コード ID | 実行ファイルのメインコードセクションの SHA-256。既知の ID は、高速パスとして既知のレイアウトを選択します。ヘッダーのみが異なるバリアント（たとえば Large Address Aware のコピー）は、コード ID を共有します。 |
| 既知のレイアウト | `Omsi23004`: OMSI 2.3.004（ファイルバージョン 2.2.032）、コード ID `13ec110abdccc09d6d85875a017d5e75e45514283d5a4558f49a7c282dfdc028`。[ステータスページ](../status/runtime-validation-status.md)のすべてのランタイム検証は、このレイアウトで実行されました。 |
| ネイティブターゲット | ネイティブ境界の前提の 1 つで、そのガードとともにビルドカタログに宣言されています。 |
| ケイパビリティ | ターゲットのグループ。たとえば `world.new-map` や `time.read`。すべてのターゲットが解決され、**かつ**検証されている場合にのみ利用可能です。 |

ターゲットは、OmsiLaunch が何かにパッチを当てる前に、プラグイン内で次のように解決されます。

1. **既知の ID:** 既知のレイアウトの基準アドレスが最初に試され、それがターゲットのガードをなお通過しなければなりません。
2. **それ以外の場合、またはガードが失敗した場合**、ビルド非依存の方法が試されます。
   - Delphi RTTI のクラス名（`vmtSelfPtr` を検査）。
   - クラスの published メソッド、フィールド、プロパティ（RTTI テーブル）。
   - 解決済みのメソッドを保持する VMT スロット。
   - ちょうど 1 回だけ一致しなければならないマスク付きのコードシグネチャ（レイアウトに依存するオペランドはワイルドカード）。これにより、アドレス、グローバル変数（直接、またはそのポインターセル経由）、またはフィールド自身のクラスのメソッド内部からフィールドの変位が取得されます。
3. **一意に一致してガードを通過するものがない場合:** ターゲットは未解決のままです。**固定アドレスへのフォールバックはありません。**

ガード: 関数とパッチサイトにはマスク付きのエントリバイト、パッチされた呼び出しには呼び出し先、クラスには `vmtSelfPtr` と RTTI のクラス名、VMT スロットには保持されたメソッド、グローバル変数には書き込み可能なデータセクション。ランタイムリーダーが読み取るオブジェクトレイアウト（車両、人物、天候、時刻表のレコードなど）には、ビルド非依存の構造的な検証がまだありません。これらは既知のコード ID に対してのみ検証されるため、それらを読み取るケイパビリティは、不明な ID のビルドでは利用できません。

既知のレイアウトでは、すべてのケイパビリティが解決されます。不明な ID のビルドでは、そのターゲットが上記の方法で解決されるケイパビリティ（ヘッドレス開始、NEW_MAP、保存済みシチュエーション、Internet Textures の抑制、時刻の読み取りと設定、D3D）は利用可能になり得て、それぞれが個別に検証されます。レイアウトに依存する読み取りは利用できません。

<a id="where-compatibility-is-decided"></a>
## 互換性が決まる場所

| 段階 | 何が行われるか |
| --- | --- |
| プランの作成（`PlanSessionAsync`、`/plan`） | OMSI 2 の確認（`omsi.omsi2`。そうでなければ `OL_E_UNSUPPORTED_BUILD`）。ファイルイメージが**予測**として解決されます。必須のネイティブケイパビリティは `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED`、または警告 `OL_W_CAPABILITY_PENDING` を伴う `PENDING_IN_PROCESS_RESOLUTION` を持ちます。不明なコード ID は警告 `OL_W_BUILD_UNRECOGNIZED` を追加します。警告によってプランが実行不可になることはありません。 |
| 開始（`StartSessionAsync`） | spec のプランが再作成されます。もはや実行可能でなくなったプランは `OL_E_PLAN_NOT_RUNNABLE` で拒否されます。 |
| プロセス内（プラグインの開始） | 読み込まれたイメージが解決されます（これが正です。DRM 保護された実行ファイルは、そのファイルと異なる場合があります）。レポートはテレメトリ `native.capabilities` として送信されます。`boot.headless-start` とワールドモードのケイパビリティ（`world.new-map` または `world.saved-situation`）が利用できない場合、セッションはフックの前に `OL_E_CAPABILITY_UNRESOLVED` で失敗し、ファイルは復元されます。それらが利用可能でも、実際の Start フォームが期待された状態にない場合（たとえば別のプラグインがパッチを当てた場合）、セッションは `OL_E_BUILD_VALIDATION_FAILED` で失敗します。 |
| ランタイム操作 | ケイパビリティが利用できない操作は、OMSI のメモリに触れることなく `OL_E_CAPABILITY_UNRESOLVED`（`Data["capability"]`）で拒否されます。 |

`OmsiLaunch.exe profiles --json` は、インストール環境（`is_omsi2`、`build_id`、`identity`、ファイルとコードのハッシュ、`file_variant`）、既知のレイアウト、およびファイルから予測されたケイパビリティを報告します。

<a id="operating-system-and-architecture"></a>
## オペレーティングシステムとアーキテクチャ

`CurrentWindowsX64Platform.Detect` が `RuntimePlatformInfo` を算出します。現在のプラットフォームは、次のすべてが満たされる場合にのみ対応しています。

| 要件 | 検査 | 違反時のエラー |
| --- | --- | --- |
| Windows | `OperatingSystem.IsWindows()` | `OL_E_UNSUPPORTED_OPERATING_SYSTEM` |
| Windows 10 以降 | `Environment.OSVersion.Version.Major >= 10`（Windows 10、Windows 11、Server 2016 以降） | `OL_E_PLATFORM_CAPABILITY_MISSING` |
| 64 ビット Windows かつ 64 ビットのホストプロセス | `OSArchitecture == X64` かつ `ProcessArchitecture == X64` | `OL_E_UNSUPPORTED_OS_ARCHITECTURE` |
| 書き込み可能なインストール環境 | ルートディレクトリが存在し、読み取り専用ではなく、`plugins\` を含むこと | `OL_E_INSTALLATION_NOT_WRITABLE` |

`RuntimePlatformInfo` はさらに、`OmsiArchitecture` と `PluginArchitecture` を `X86`（OMSI は 32 ビットプロセスであり、プラグインのクロージャは x86 で WOW64 上で実行されます）、`LegacyPlatform=false`、および `Wow64Available` として報告します。ARM64 版 Windows は、x64 エミュレーションが存在する場合でも非対応です。ホストプロセス自体が x64 でなければならないためです。

<a id="net-requirements"></a>
## .NET の要件

| コンポーネント | ランタイム | 注記 |
| --- | --- | --- |
| コントローラー（`OmsiLaunch.exe`、`OmsiLaunchW.exe` -> `OmsiLaunch.Controller.dll`） | .NET 6、x64 | ネイティブブートストラッパーは、パッケージに含まれる `nethost.dll` を介して `hostfxr` 経由でランタイムを探します。ランタイムがない場合はシムが報告します（終了コード 100-106。[CLI](cli.md)と[終了コード](exit-codes.md)を参照）。 |
| プラグインのクロージャ（`OmsiLaunch.PluginNE.dll` 経由の `plugins\OmsiLaunch.Plugin.dll`） | .NET 6、**x86**（`net6.0-windows`、`win-x86`）、`Omsi.exe` 内で DNNE 2.0.6 によりホスト | マシンに x86 の .NET 6 Desktop/Core ランタイムがインストールされている必要があります。プラグインには 64 ビットのランタイムだけでは不十分です。 |
| ネイティブブリッジ（`plugins\OmsiLaunch.Native.x86.dll`） | ネイティブ x86 | `plugins\` からのみ読み込まれます（[常駐プラグイン](../concepts/permanent-plugin.md)を参照）。OMSI のアドレスは一切含まれません。プラグインから検証済みのターゲットテーブルを受け取ります。 |

<a id="legacy-platforms"></a>
## レガシープラットフォーム

Windows 7、Windows 8.x、Windows XP、およびその他の NT 6 以前のシステムは、現在のサポート範囲外です。`RuntimePlatformInfo.LegacyPlatform` は常に `false` であり、レガシーアダプターは存在しません。このフィールドと `IPluginNativeServices` の接合点は、将来レガシーアダプターを公開 API を変更せずに追加できるようにするためだけに存在します（`docs/adr/ADR-0010-Legacy-Portability-Boundary.md` を参照）。このリリースには、これらのシステムで動作するものは何もありません。

<a id="steam-and-large-address-aware-notes"></a>
## Steam と Large Address Aware に関する注記

- ヘッダーのみが異なる Large Address Aware のコピーは、元のものと同じコード ID を持ち、同じ既知のレイアウトを使用します。
- Steam の実行ファイルはディスク上で DRM により保護されているため、そのファイルは読み込まれたイメージと異なる場合があります。プランは `OL_W_CAPABILITY_PENDING` を報告することがあり、プロセス内の解決が決定します。正規の Steam 実行ファイルによるセッションはランタイム検証されていません（[ランタイム検証ステータス](../status/runtime-validation-status.md)を参照）。
- Steam は OMSI を自ら起動します。ハンドオフが存在するように、セッションは `OmsiLaunch.exe` を通じて開始する必要があります。Steam から起動された場合、常駐プラグインは何も動作しません（ハンドオフなし、フックなし）。

<a id="related-pages"></a>
## 関連ページ

- [既知の制限事項](known-limitations.md)
- [ランタイム検証ステータス](../status/runtime-validation-status.md)
- [インストール](../getting-started/installation.md)
