# パブリック API リファレンス（`OmsiLaunch.Api`）

<!-- l10n: source=reference/public-api.md source-sha256=03d2b00af2df06cdc28d5fbbec294f9f480584c4817b1dfe8b805a914621f734 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/public-api.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、OmsiLaunch 0.1.0-beta4 のマネージドパブリック API に関する規範的なリファレンスです。対象は `OmsiLaunch.Api` アセンブリ（コントラクト）と、`OmsiLaunch.Core` に含まれるインテグレーター向けエントリポイント `OmsiLaunchService` です。記載しているのは現在のコードの動作のみです。インテグレーターが呼び出し、受け取り、観測できるものはすべて安定性レベルとともにここに列挙しています。ここに記載されていないものは統合用のサーフェスではありません。

自動生成される [パブリック API インベントリ](public-api-inventory.md) には、`OmsiLaunch.Api`、`OmsiLaunch.Core`、`OmsiLaunch.Process` のすべてのパブリック型とメンバーが、シグネチャと安定性とともに列挙されています。インベントリとアセンブリが一致しない場合はドキュメントゲートが失敗します。このページではそのセマンティクスを説明します。

関連ページ: [LaunchSpec リファレンス](launchspec.md)、[エラーコード](errors.md)、[セッションライフサイクル](../concepts/session-lifecycle.md)、[トランザクションとリカバリ](../concepts/transactions-and-recovery.md)、[ランタイム制御](runtime-control.md)、[ケイパビリティ](capabilities.md)、[ローカルコントロールプレーン](local-control.md)、[終了コード](exit-codes.md)、[ランタイム検証ステータス](../status/runtime-validation-status.md)。

<a id="stability-vocabulary"></a>
## 安定性の用語

| レベル | このページでの意味 |
| --- | --- |
| `STABLE_BETA` | コントラクトは 0.1 プロトコル系列で凍結されており、その経路は `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md` でランタイム検証済みです。 |
| `EXPERIMENTAL` | 呼び出し可能でテスト済みですが、安定版になる前にコントラクトまたはランタイムのエビデンスが変わる可能性があります。 |
| `PARTIAL` | コントラクトには存在しますが、動作の一部だけが実装または検証されています（どの部分かは本文に記載しています）。 |
| `INTERNAL` | 技術的な理由（ブリッジが型を共有している）でアセンブリ上はパブリックですが、統合用のサーフェスではありません。予告なく変更される可能性があります。 |
| `UNAVAILABLE` | コントラクトには存在しますが、現在のビルドでは拒否されます。 |

<a id="assembly-overview"></a>
## アセンブリの概要

| アセンブリ | インテグレーターにとっての役割 |
| --- | --- |
| `OmsiLaunch.Api` | 純粋なコントラクト: レコード、列挙型、`IOmsiLaunch`、ケイパビリティレジストリ、エラーカタログ、ワイヤーフォーマット、D3D ヘルパー。`IntPtr`、`nint`、Win32 ハンドル、ネイティブアドレス、プロセスオブジェクトは一切含みません。 |
| `OmsiLaunch.Core` | `OmsiLaunchService`（`IOmsiLaunch` の実装）、`OmsiLaunchRuntimePaths`、`SessionPlanner`、`LaunchValidation`、`SessionProfileCompiler`。 |
| `OmsiLaunch.Process` | `IRuntimePlatform` と `CurrentWindowsX64Platform`（唯一のプラットフォームアダプター）、`InstallationLease`。サービスの構築に必要です。 |
| `OmsiLaunch.Configuration`、`OmsiLaunch.Content`、`OmsiLaunch.Interop`、`OmsiLaunch.Plugin`、`OmsiLaunch.Builds.Omsi23004` | 実装アセンブリです。これらのパブリック型はインテグレーターにとって `INTERNAL` です。 |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## エントリポイント: `OmsiLaunchService` と `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| パラメーター | 有効な値 | 無効な場合 / 既定値 |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()`（名前空間 `OmsiLaunch.Process`）。プラットフォームを検出し、`CreateProcessW` で OMSI プロセスを作成し、その終了を待機し、強制終了します。 | ほかの実装は同梱されていません。独自の `IRuntimePlatform` は `INTERNAL` です。 |
| `PluginBuildDirectory` | 常駐プラグインのクロージャ参照ファイルを含むディレクトリ: `OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json`、およびマネージドクロージャのすべての `OmsiLaunch.*.dll`（`OmsiLaunch.Plugin.dll` を含む必要があります）。インストール済みパッケージでは `<package>\plugins` です。 | ディレクトリまたはファイルが存在しない場合: `PlanSessionAsync` は `OL_E_RUNTIME_ARTIFACT_MISSING` を含む実行不可のプランを返します。 |
| `NativeBridgePath` | `OmsiLaunch.Native.x86.dll` のパス（パッケージ内では `<package>\plugins\OmsiLaunch.Native.x86.dll`）。 | 上と同じです。 |
| `ReleaseManifestPath` | `OmsiLaunch.exe` の隣に存在する場合の `release-manifest.json`。各 `plugins/` ファイルの期待される SHA-256 を提供します（`plugin.integrity.reference = manifest`）。 | `null`（開発用レイアウト）: インストール済みファイルは、存在と参照クロージャに対する自己整合性のみが検査されます（`plugin.integrity.reference = self`）。不正な形式のマニフェスト: `OL_E_RELEASE_MANIFEST_INVALID`。 |

サービスはこれらのパスを `PlanSessionAsync` と `StartSessionAsync` のたびに読み取ります。プラグインファイルをコピー、ステージング、削除することは一切ありません（[常駐プラグイン](../concepts/permanent-plugin.md) を参照）。CLI はまさにこのとおりにサービスを構築します（`tools/OmsiLaunch.Cli/Program.cs`）。

```csharp
using OmsiLaunch.Api;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

var package = AppContext.BaseDirectory;                       // directory that contains OmsiLaunch.exe
var plugins = Path.Combine(package, "plugins");
var manifest = Path.Combine(package, "release-manifest.json");
IOmsiLaunch launch = new OmsiLaunchService(
    new CurrentWindowsX64Platform(),
    new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), File.Exists(manifest) ? manifest : null));
```

サービスはプロセスごとに 1 つ作成し、共有してください。安定性: `STABLE_BETA`。

<a id="session-ownership-rules"></a>
## セッションの所有に関するルール

| ルール | 詳細 |
| --- | --- |
| インストール環境ごとにオーナーは 1 つ | `StartSessionAsync` はインストールリース、すなわち名前付きセマフォ `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>` を取得し、スーパーバイザーがインストール環境を復元するまで保持します。同じログオンセッション内の任意のプロセスから同じルートに対して 2 回目の開始を行うと、`OL_E_INSTALLATION_BUSY` で失敗します（`Failed` セッションとして報告されます。`StartSessionAsync` を参照）。リースはログオンセッション単位であり、ログオンをまたぐものではありません。また、別のプロセスがそのハンドルを保持している間は解放されません（受容済みリスク）。 |
| ハンドルはプロセスローカル | `SessionHandle` はセッションの `Guid` をラップします。意味を持つのは、それを返した `OmsiLaunchService` インスタンスに対してのみです。既知の `Guid` から別のプロセス（または別のサービスインスタンス）で構築したハンドルは `KeyNotFoundException` になります。プロセス間の制御はハンドルではなく [ローカルコントロールプレーン](local-control.md) を経由します。 |
| 必ず `CloseAsync` を呼び出す | `StartSessionAsync` 以降、プロセスは永続的なトランザクションを所有します。`CloseAsync` は必要に応じて正規の停止を要求し、スーパーバイザー（プロセスの終了、厳密な復元、リースの解放）を待機してから、セッションを破棄します。`Failed` 状態の後も含め、すべての終了経路で呼び出す必要があります。呼び出さない場合、セッションエントリはメモリに残ります。ただし復元そのものは、いずれにせよスーパーバイザーが実行します。 |
| 失敗したセッションもセッションである | `StartSessionAsync` が戻った後に失敗した開始は `SessionState.Failed` を報告します。ハンドルは `CloseAsync` まで `GetStatusAsync`/`WaitForAsync` に対して有効なままです。 |
| プランは再検査される | `StartSessionAsync` は `Omsi.exe` のハッシュを再計算し、spec のプランを再作成します。もはや実行可能でないプランは `OL_E_PLAN_NOT_RUNNABLE` で拒否されます。 |

## `IOmsiLaunch`

```csharp
public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
```

すべてのメソッドに共通する事項:

- 不明なハンドル、またはすでにクローズされたハンドルは `KeyNotFoundException`（"Unknown OmsiLaunch session."）をスローします。
- `ExecuteRuntimeAsync` 以外のメソッドは、Running 状態のセッションを必要としません。
- OmsiLaunch コードを伴う例外は、そのコードを `Exception.Message` の先頭に置きます（`"OL_E_PLAN_NOT_RUNNABLE: ..."`）。CLI も同じ方法でメッセージからコードを抽出します（`CliProgram.Classify`）。
- 対応インストール: OMSI 2。ネイティブケイパビリティはビルドごとにプロセス内で解決され検証されます。そのビルドで利用できないケイパビリティは `OL_E_CAPABILITY_UNRESOLVED` で失敗します。[互換性](compatibility.md) を参照してください。

<a id="complete-minimal-example"></a>
### 最小限の完全な例

```csharp
var none = new Dictionary<string, OptionalValue<string>>();
var spec = new LaunchSpec(
    Installation: new InstallationSpec(@"C:\OMSI 2"),
    World: new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)),
    Date: new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
    Time: new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
    PlayerVehicle: OptionalValue<PlayerVehicleSpec>.Unset,
    Environment: new EnvironmentSpec(none, none, none, none, none, none, none, none),
    Behavior: new LaunchBehaviorSpec());

var plan = await launch.PlanSessionAsync(spec);
if (!plan.IsRunnable) { foreach (var d in plan.Diagnostics) Console.WriteLine($"{d.Code}: {d.Message}"); return; }

var session = await launch.StartSessionAsync(plan);
try
{
    var status = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(plan.Spec.Behavior.StartupTimeoutSeconds + 5));
    if (status.State == SessionState.Running)
    {
        var time = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 1, "time.read"), TimeSpan.FromSeconds(5));
        Console.WriteLine(time.Succeeded ? $"{time.Values!["hour"]}:{time.Values["minute"]}" : time.ErrorCode);
        await launch.StopAsync(session);
    }
    var final = await launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
    Console.WriteLine(final.State);                      // Completed, or Failed with diagnostics
}
finally
{
    await launch.CloseAsync(session);                    // always
}
```

### `PlanSessionAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 目的 | OMSI を起動せずに `LaunchSpec` を `SessionPlan` にコンパイルします。spec の検証、プラットフォームの検出、`Omsi.exe` のフィンガープリント取得、コンテンツ ID の解決、予定されるファイル変更の算出、必要なケイパビリティと非対応のケイパビリティの列挙、`IsRunnable` の決定を行います。パブリックケイパビリティ `session.plan`。 |
| パラメーター | `spec`: すべての値が設定された `LaunchSpec`（[LaunchSpec リファレンス](launchspec.md) を参照）。`Installation`、`World`、`Date`、`Time`、`Environment`（8 つのディクショナリすべて）、`Behavior` は null 以外である必要があります。省略可能なメンバーは `null` でもかまいません。`RootPath` は絶対パスのディレクトリにしてください。空のルートは `OL_E_INSTALLATION_NOT_FOUND` として記録されますが、空のパスに対するプラットフォームプローブがプランを返す前に `ArgumentException` をスローするため、空のルートは決して渡さないでください。 |
| 戻り値 | 新しい `SessionId`、`BuildProfileId`（既知のレイアウトでは `"Omsi23004"`、既知のレイアウトを持たない OMSI 2 ビルドでは `"unrecognized"`）、入力の `Spec`、`Platform`、`ResolvedContent`、`TouchedFiles`、`RuntimeArtifacts`（`plugins\OmsiLaunch.*` の配置先パスと `"OmsiLaunch startup handoff v6"`）、`RequiredCapabilities`、`UnsupportedRequestedFeatures`、`PlannedMutations`、`Diagnostics`、`IsRunnable`、`PlannedModules`（要求された、またはコンテンツによって有効化された[ランタイムモジュール](runtime-modules.md)ごとに 1 つの `PlannedModule`。アクセサー `EffectivePlannedModules`）を持つ `SessionPlan`。`IsRunnable` は、`OL_E_` で始まる診断コードが 1 つもない場合に限り `true` です。情報提供用の診断（メッセージ `self` または `manifest` を持つ `plugin.integrity.reference`、`session_profile.selected`）によってプランが実行不可になることはありません。 |
| 結果で返されるエラー | プランニングのエラーはすべて例外ではなく診断として返されます: `OL_E_INSTALLATION_NOT_FOUND`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_DATE_TIME_APPLY_FAILED`、`OL_E_INVALID_ARGUMENT`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_SESSION_PRESENTATION_INVALID`（メッセージにスプラッシュ/ITX のコードが含まれます）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（`plugins\` 内のインストール済みプラグインクロージャは、プランニング時にリリースマニフェストと照合されます）、`OL_E_RUNTIME_ARTIFACT_MISSING`（メッセージに `OL_E_RELEASE_MANIFEST_INVALID` が含まれる場合があります）。条件の詳細: [LaunchSpec の検証ルール](launchspec.md#validation-rules-and-non-runnable-diagnostics)。 |
| スローされる例外 | 開始時点でトークンがすでにキャンセルされている場合は `OperationCanceledException`（唯一のチェックポイント）。構文的に無効なルートパスには `ArgumentException`/`NotSupportedException`。必須メンバーが null の場合は `NullReferenceException`/`ArgumentNullException`。構文的に無効なリリースマニフェストには `System.Text.Json.JsonException`。 |
| キャンセル | 開始時に 1 回だけ検査されます。その後のプランニングは同期的なファイルシステム処理です。 |
| Running セッションが必要か | いいえ。 |
| OMSI の状態を変更するか | いいえ。 |
| ファイルシステムを変更するか | いいえ（`Omsi.exe`、コンテンツファイル、プラグインクロージャ、マニフェストを読み取ります）。設定値はここでは検証されません（キーの存在と書き込み可否のみ）。無効な値は開始時に `OL_E_INVALID_SETTING_VALUE` で失敗します。 |
| トランザクション / 復元 | なし。 |
| 制限事項 | `Date`/`Time`/`Year` のいずれかで `Unset` 以外のモードを要求すること、`Weather` で `Unset` 以外のモードを要求すること、`PlayerVehicle` のいずれかのフィールド、`Input` ドキュメント、`EntrypointIdentity`、または `WorldMode.LastMapState` を要求すると、このビルドでは `OL_E_CAPABILITY_UNAVAILABLE` が発生し、プランは実行不可になります（`UnsupportedRequestedFeatures` に、カタログのエビデンス `UNAVAILABLE` または `PARTIAL` を持つエントリが追加されます）。 |
| 安定性 | `STABLE_BETA`。 |
| 例 | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 目的 | 実行可能なプランから、トランザクション管理された OMSI セッションを開始します。インストールリースの取得、失効したジャーナルのリカバリ、常駐プラグインクロージャの検証、セッションファイルのスナップショット取得とオーバーレイ、起動ハンドオフ・テレメトリスロット・ランタイムメールボックスの作成、`Omsi.exe` の起動、ジャーナルへのプロセスの記録を行い、セッションをバックグラウンドのスーパーバイザーに引き渡します。パブリックケイパビリティ `session.start`。 |
| パラメーター | `plan`: `IsRunnable == true` である `SessionPlan`。プラン内の spec はプランが再作成され、呼び出し元のプランから保持されるのは `plan.SessionId` だけです。`plan.Spec.Behavior.StartupTimeoutSeconds` は 1..600 である必要があります。 |
| 戻り値 | `Omsi.exe` が作成されて記録された時点（状態 `WaitingForPlugin`）、または開始経路が失敗した時点（状態 `Failed`）で、ただちに `SessionHandle(plan.SessionId)` を返します。ゲームプレイの開始は待ちません。`WaitForAsync(session, SessionState.Running, ...)` を使用してください。 |
| スローされる例外 | `plan.IsRunnable` が false の場合は `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`。再プランが実行不可の場合（たとえば `Omsi.exe` が変更された、コンテンツが削除された、プラグインクロージャが見つからない）は `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`。同じ ID のセッションがまだ登録されている場合は `InvalidOperationException("Duplicate session id.")`（先に `CloseAsync` を呼び出してください）。`StartupTimeoutSeconds` が 1..600 の範囲外の場合は `ArgumentOutOfRangeException`。再プランの前または最中にキャンセルされた場合は `OperationCanceledException`。加えて、`PlanSessionAsync` がスローするすべての例外。例外がスローされた場合はいずれも、セッションは登録されません。 |
| 結果で返されるエラー | 再プラン以降の失敗はすべて開始経路の内部で捕捉されます。セッションは登録され、状態は `Failed` になり、その診断には `OL_E_START_SESSION` が含まれます。そのメッセージは内部のメッセージです（内部のコードがある場合はそのコードで始まります）: `OL_E_INSTALLATION_BUSY`（リースが保持されている、またはジャーナルに記録された OMSI プロセスがまだ動作している）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`（このセッションのオーバーレイを用いた遅延再試行でもなお所有権を証明できない場合のみ）、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`、`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。クリーンアップによって `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`（OMSI の終了が確認できず、ジャーナルを保持）、または `OL_E_RESTORE_FAILED` が追加される場合があります。それ以降の失敗はスーパーバイザーが報告します（[セッションライフサイクル](../concepts/session-lifecycle.md) を参照）。 |
| キャンセル | 再プランの前または最中: 例外をスローします。それ以降はトークンがトランザクションとプロセス作成に渡されます。そこでのキャンセルはほかの開始失敗と同様に扱われ（`Failed` + `OL_E_START_SESSION: The operation was canceled.`）、プロセスが作成されていれば強制終了され、インストール環境は復元されます。 |
| Running セッションが必要か | いいえ。 |
| OMSI の状態を変更するか | はい: 環境変数 `OMSILAUNCH_SESSION_ID`、`OMSILAUNCH_HANDOFF_NAME`、`OMSILAUNCH_TELEMETRY_NAME`、`OMSILAUNCH_RUNTIME_CHANNEL`、`OMSILAUNCH_INTERNET_TEXTURES_MODE` を設定して OMSI プロセスを作成します。 |
| ファイルシステムを変更するか | はい。インストールルート内で次の変更を行います: `.omsilaunch\diagnostics\<sessionId>-host.log`（保持: 最新 50 セッション）、`.omsilaunch\journal.json`、`.omsilaunch\backup\<sessionId>\*.bin`、`.omsilaunch\assets\splash\*.bmp`（マネージドスプラッシュ用に 1 回だけコピー）、セッションオーバーレイ（`options.cfg` のパッチ、`GUI\NewSplashscreen_*.bmp`、`Texture\standard.itx`）、セッション中の削除（ITX のターゲット、`Texture\standard.ipr`、`closecheck`）、および `SuppressStaleClosecheckWarning` が true の場合は、以前から存在する失効した `closecheck` の恒久的な削除（診断 `closecheck.stale-removed`）。 |
| トランザクション / 復元 | トランザクションを開きます（`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`）。セッションから抜けるすべての経路は復元で終わります。[トランザクションとリカバリ](../concepts/transactions-and-recovery.md) を参照してください。 |
| 制限事項 | ゲームプレイに到達するのは、`PresentedEntrypointIndex` を指定した `WorldMode.NewMap` と `WorldMode.SavedSituation` のみです。`WorldMode.LastMapState` は `UNAVAILABLE` です。プレイヤー車両の要求は、プランニング時点で実行不可となるため、このメソッドに到達することはありません。 |
| 安定性 | `STABLE_BETA`（NEW_MAP と SAVED_SITUATION のライフサイクルはランタイム検証済み）。 |
| 例 | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 目的 | セマンティックなライフサイクル状態、これまでに収集された診断、および上限付きのランタイムイベントリストを読み取ります。パブリックケイパビリティ `session.status`。 |
| パラメーター | `session`: `StartSessionAsync` が返し、まだクローズされていないハンドル。 |
| 戻り値 | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`: 不変のスナップショットです（配列はセッションロックの下でコピーされます）。有効なセッションでは `RuntimeEvents` が `null` になることはありません。 |
| スローされる例外 | 不明なハンドル/クローズ済みのハンドルに対しては `KeyNotFoundException`。それ以外では例外をスローしません。 |
| キャンセル | トークンは無視されます（呼び出しは同期的に完了します）。 |
| Running セッションが必要か | いいえ。 |
| OMSI / ファイルシステム / トランザクションを変更するか | いいえ / いいえ / なし。 |
| 安定性 | `STABLE_BETA`。 |
| 例 | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 目的 | セッションが `state` になるか、終了状態（`Completed`、`Failed`）になるか、タイムアウトが経過するまで（100 ms ごとに）ポーリングし、その後で現在のステータスを返します。 |
| パラメーター | `state`: 任意の `SessionState`。すでに通過した一時的な状態（または設定されることのない状態。[セッションライフサイクル](../concepts/session-lifecycle.md) を参照）を待機すると、終了状態になるかタイムアウトするまで待機します。`timeout`: 負でない任意の `TimeSpan` または `Timeout.InfiniteTimeSpan`。 |
| 戻り値 | 待機が終了した時点のステータス。タイムアウト時は例外ではなくステータスが返されるため、`State` は呼び出し側で確認してください。`Running` の待機が `Failed` で終わった場合は、失敗の診断とともにただちに戻ります。 |
| スローされる例外 | `KeyNotFoundException`。呼び出し元のトークンがキャンセルされた場合は `OperationCanceledException`（伝播するのは呼び出し元によるキャンセルのみで、内部のタイムアウトは伝播しません）。 |
| キャンセル | 呼び出し元のトークンは 100 ms ごとのティックで毎回確認されます。 |
| Running セッションが必要か | いいえ。 |
| OMSI / ファイルシステム / トランザクションを変更するか | いいえ / いいえ / なし。 |
| 安定性 | `STABLE_BETA`。 |
| 例 | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 目的 | 正規の停止を要求します。停止フラグを設定してただちに戻ります。スーパーバイザーは 100 ms のループ内でフラグを検出し、`Omsi.exe` に対して `TerminateProcess` を呼び出し、終了を待機し、ジャーナルを `ProcessExited` とマークし、セッションが所有するすべてのファイルを復元してリースを解放します。これは強制終了です。OMSI 自身のシャットダウン処理は実行されず、OMSI は終了時に `options.cfg` を書き換えません（トランザクションを保護するための意図的な動作です）。`WM_CLOSE` による協調的なシャットダウンは実装されていません（製品上の判断です。ランタイムクロージャでは、OMSI は `WM_CLOSE` から 30 s 以内に終了しませんでした。`L05b`）。パブリックケイパビリティ `session.stop`。 |
| パラメーター | `session`。 |
| 戻り値 | 完了済みのタスク。強制終了や復元は待ちません。完了を確認するには `WaitForAsync(session, SessionState.Completed, ...)` を使用してください。 |
| スローされる例外 | `KeyNotFoundException`。 |
| キャンセル | トークンは無視されます。 |
| Running セッションが必要か | いいえ。べき等です。スーパーバイザーの開始前に要求された停止は、スーパーバイザーが開始されしだい実行されます。終了状態のセッションに対する停止は何もしません。 |
| OMSI の状態を変更するか | はい: OMSI プロセスを強制終了します（終了コード 1）。 |
| ファイルシステムを変更するか | 間接的に変更します: スーパーバイザーによる復元、ジャーナルの削除、バックアップの削除を引き起こします。 |
| トランザクション / 復元 | `ProcessExited` → `Restoring` → `Restored` を引き起こします。`ExecuteRuntimeAsync` を通じて行われたランタイム側の変更（時計の書き込み、スポーンした車両、スクリプト変数、D3D テクスチャ）は復元されません。それらはプロセスとともに消えます。 |
| 安定性 | `STABLE_BETA`。 |
| 例 | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 目的 | トランザクションを置き去りにせずに利用側のハンドルを解放します。セッションが終了状態でなければ正規の停止を要求し、次にスーパーバイザーのライフサイクルタスク（プロセスの終了、復元、リースの解放）を待機し、最後にセッションを破棄します。 |
| パラメーター | `session`。 |
| 戻り値 | セッションが終了状態になり、削除された時点で完了します。戻った後、そのハンドルは不明なハンドルになります（2 回目の `CloseAsync` を含め、以降のすべての呼び出しで `KeyNotFoundException`）。 |
| スローされる例外 | `KeyNotFoundException`。スーパーバイザーの待機中に呼び出し元がキャンセルした場合は `OperationCanceledException`。その場合、セッションは削除されず、スーパーバイザーは動作を続けます。もう一度 `CloseAsync` を呼び出してください。 |
| キャンセル | 待機にのみ適用され、復元がキャンセルされることはありません。 |
| Running セッションが必要か | いいえ。 |
| OMSI の状態を変更するか | セッションがまだ有効な場合ははい（`StopAsync` と同じ）。 |
| ファイルシステムを変更するか | 間接的に変更します（スーパーバイザーによる復元）。 |
| トランザクション / 復元 | ハンドルが解放される前に、トランザクションが完了まで進められることを保証します（スーパーバイザーが開始されていた場合）。スーパーバイザーの開始前に失敗したセッションについては、開始経路がすでに復元を行っているか、`OL_E_RESTORE_DEFERRED` を報告しています。 |
| 安定性 | `STABLE_BETA`。 |
| 例 | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 目的 | 同時に 1 件のみ処理するセッションメールボックス（メモリマップト、64 KiB、要求はセッション ID と要求 ID に紐付け）を通じて、実行中の OMSI プロセス内で 1 つのパブリックランタイム操作を実行します。プラグインは OMSI の UI スレッド上で操作を実行します。操作カタログ: [ランタイム制御](runtime-control.md) と [ケイパビリティ](capabilities.md)。 |
| パラメーター | `command.SessionId` は `session.SessionId` と等しい必要があります。`command.RequestId`: 呼び出し元が選ぶ `ulong`。プロセスごとに厳密に増加するカウンターを使用してください（D3D ヘルパーは 30 000 から、CLI のオーナーは 10 001/50 000 から開始します）。`command.Operation`: `PublicCapabilityRegistry.PublicRuntimeOperationIds` に含まれるパブリック操作 ID（例: `time.read`、`road-vehicle.read`、`d3d.texture.create`）。`command.Arguments`: 序数比較の名前をキーとする文字列値。操作ごとの必須の名前は `PublicCapabilityRegistry.GetRuntimeArguments` から取得します。`timeout`: 要求がメールボックスにステージングされた時点から計測されます（実行中の別コマンドの後ろで待機している時間は含まれません）。CLI はオーナーとしては 5 s（`road-vehicles.spawn` では 15 s）、クライアントとしては 8 s / 30 s を使用します。 |
| 検査の順序 | 1. レジストリによる検証（セッションの検索より前）: 不明な操作または `internal.*` 操作 → 結果 `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`。必須引数の欠落（存在しない、または空白のみ）→ `OL_E_RUNTIME_ARGUMENT_REQUIRED`。2. セッションの検索 → `KeyNotFoundException`。3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`。4. 状態が `Running` でない → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`。5. メールボックスへの要求。6. キーが `internal_` で始まる、または `_address`、`_pointer`、`_vmt` で終わる結果値は除去されます。 |
| 戻り値 | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`。成功時、`Values` には操作のセマンティックな文字列が格納されます（操作ごとに [ランタイム制御](runtime-control.md) に記載）。 |
| 結果で返されるエラー | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（レジストリ）。`OL_E_RUNTIME_RESPONSE_TOO_LARGE`（プラグインの結果がメールボックスを超えた場合。上限付きリストの結果は、これの代わりに `truncated=true` を付けて短縮されます）。`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（導出キーを指定した `weather.set`）。すべての `OL_E_D3D_*` コード（`Values["detail"]` と `Values["native_status"]` 付き）。`OL_E_CAPABILITY_UNRESOLVED`（その操作のネイティブケイパビリティが、実行中のビルドで解決されていない場合）。そして、プラグイン側の型付きの失敗はすべて、`ErrorCode` に固有のコードを持ち、メッセージは `Values["detail"]` に入ります（Beta 4。カタログに載っているコードを持たない失敗のみが `OL_E_RUNTIME_OPERATION_FAILED` です）。この形で届くコード: `OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（プラグイン側の検査）、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE`、`OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_CONSTANT_NOT_FOUND`、`OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`、`OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_DEGENERATE`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_HOF_UNAVAILABLE`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_TIME_APPLY_FAILED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_PLACE_RANDOM_BUS_FAILED`、`OL_E_RUNTIME_SETTING_UNAVAILABLE`。[エラーコード](errors.md) を参照してください。 |
| スローされる例外 | `KeyNotFoundException`。`OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_CLOSED`（メールボックスがスーパーバイザーによってすでに破棄されている）、`OL_E_RUNTIME_CHANNEL_BUSY`（スロットに放棄された要求がまだ残っている）、`OL_E_RUNTIME_REQUEST_ID_REUSED`（同じ要求 ID の失効した応答がまだスロットに残っている）を伴う `InvalidOperationException`。`TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`。`InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")`（破損した応答、別のセッションの応答、または一致しない応答）。シリアル化された要求がメールボックスを超える場合は `ArgumentOutOfRangeException`。`OperationCanceledException`。 |
| キャンセル | セッションごとのゲートを待機している間、および応答のポーリング中は 20 ms ごとに確認されます。処理中にキャンセルしてもスロットはリセットされません。そのセッションでの次の呼び出しは、プラグインが応答を公開する（その応答は失効したものとして破棄されます）まで `OL_E_RUNTIME_CHANNEL_BUSY` で失敗する可能性があります。タイムアウトの使用を推奨します。タイムアウトはスロットをリセットし、遅れて届いた応答は検出されて破棄されます。 |
| Running セッションが必要か | はい（`SessionState.Running`）。それ以外の場合は `OL_E_SESSION_NOT_RUNNING` がスローされます。メールボックスは、スーパーバイザーが復元中に破棄するまで存在します。 |
| OMSI の状態を変更するか | 操作によって異なります: `Read` 操作は変更しません。`Write`/`Action` 操作（`time.set`、`camera.set`、`camera.lock`、`camera.unlock`、`road-vehicles.spawn`、`road-vehicles.place-random`、`vehicle.variable.set`、`d3d.texture.*`）はプロセス内の状態を変更し、その変更は復元されません。 |
| ファイルシステムを変更するか | ホストによる書き込みはありません。その結果として OMSI が自身のファイルを書き込む場合があります（追跡されません）。 |
| トランザクション / 復元 | なし。 |
| 制限事項 | 処理中のコマンドはセッションごとに 1 つです（同じセッションへの呼び出しは直列化されます）。要求と応答はそれぞれ 64 KiB から 8 bytes を引いたサイズに制限され、D3D のピクセルペイロードは 48 KiB に制限されます。`internal.road-vehicles.make-basic` は `INTERNAL` であり、到達できません。`weather.set` と `weather.metar.set` は `EXPERIMENTAL` です。`timetable.logs.read` は上限付きではなく、大きな時刻表では `OL_E_RUNTIME_RESPONSE_TOO_LARGE` を返す可能性があります。`camera.lock` は `EXPERIMENTAL` です。プレイヤー車両が必要で、ランタイム検証済み（`CAM01`）ですが、レジストリの `RuntimeValidation` 文字列はまだ `STATICALLY_VALIDATED` のままです。ハンドル（`rv-NNNNNN`、`hb-NNNNNN`、`d3dtex-<session>-<hex>`）はセッションスコープです。 |
| 安定性 | トランスポートとコントラクトは `STABLE_BETA`。操作ごとの安定性は `PublicCapabilityRegistry` に従います（`PublicStableBeta` → `STABLE_BETA`、`PublicExperimental` → `EXPERIMENTAL`）。ただし上記の例外があります。 |
| 例 | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 目的 | インストール環境に対して評価されたケイパビリティカタログを返します。`InternalOnly` ではない `PublicCapabilityRegistry` のエントリごとに 1 つの `Capability(Name, Available, EvidenceState, Reason)` です。`Name` はカタログ ID、`EvidenceState` はカタログのエビデンス（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`）です。`Available` は、プラットフォーム、OMSI 2 の確認、分類、そのエントリが必要とするネイティブケイパビリティから算出されます（[ケイパビリティ](capabilities.md#getcapabilitiesasync)を参照）。 |
| パラメーター | `installation.RootPath`: プローブ対象のインストール環境（プラットフォーム、`Omsi.exe`）。`ExpectedExecutableSha256` は無視されます。 |
| 戻り値 | 内部向けでないカタログエントリごとに 1 つのエントリ。例: `time.read`（`RUNTIME_VALIDATED`。既知のレイアウトでは `Reason` が `native: RESOLVED_KNOWN_LAYOUT`）、`world.last-map-state`（`false`、`UNAVAILABLE`）。 |
| `PublicCapabilityRegistry` との関係 | これから導出されます。レジストリがケイパビリティ、その操作、およびそのエビデンスを宣言し、この呼び出しはそれらを 1 つのインストール環境に対して評価します。 |
| スローされる例外 | 開始時の `OperationCanceledException`。空のルートパスに対する `ArgumentException`。 |
| キャンセル | 開始時に 1 回だけ検査されます。 |
| Running セッションが必要か | いいえ。 |
| OMSI / ファイルシステム / トランザクションを変更するか | いいえ / いいえ / なし。 |
| 安定性 | 呼び出しのコントラクトは `STABLE_BETA`。リストの内容は手作業で管理されているインベントリであり、`PARTIAL` です。 |
| 例 | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 目的 | インストール済みのコンテンツを列挙し、`LaunchSpec` で使用できる正規の ID を返します。探索では再解析ポイントをスキップし（ジャンクションの循環でハングすることはありません）、OMSI ファイルを Windows-1252 として読み取り（BOM 付きの UTF-8/UTF-16 は尊重されます）、シンボリックリンクをたどることは一切ありません。 |
| パラメーター | `query` と `scope` は下表のとおりです。`scope` は `Entrypoints`（マップ ID）、`Repaints`、`FleetNumbers`、`Registrations`（車両 ID）で必須です。 |
| 戻り値 | ソート済みの `ContentIdentity(Identity, Kind, DisplayName)` リスト。ID はバックスラッシュ区切りのインストール相対パスで、比較は大文字と小文字を区別しません。 |
| スローされる例外 | 開始時の `OperationCanceledException`。スコープなしで `Entrypoints` を照会した場合、またはルートが空の場合は `ArgumentException`。スコープのマップまたは車両がインストールされていない場合は `FileNotFoundException`（`OL_E_` コードはありません。CLI はこれを `OL_E_NOT_FOUND` に対応付けます）。ルートやコンテンツディレクトリが存在しない場合は、エラーではなく空のリストになります。 |
| キャンセル | 開始時に 1 回だけ検査されます。 |
| Running セッションが必要か | いいえ。 |
| OMSI / ファイルシステム / トランザクションを変更するか | いいえ / いいえ / なし。 |
| 安定性 | `Maps`、`Situations`、`Vehicles`: `STABLE_BETA`（ランタイム検証済みのすべてのプランがこれらを通じて解決されます）。`Entrypoints`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`: `EXPERIMENTAL`（静的なエビデンスのみ）。 |
| 例 | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

`ContentQueryKind` の値と結果:

| 値 | スコープ | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | なし | `maps\<dir>\global.cfg` | `map` | マップディレクトリ名 |
| `Situations` | なし | `situations\...\<file>.osn` | `situation` | `.osn` が参照するマップ ID（`null` の場合があります） |
| `Vehicles` | なし | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` またはファイル名 |
| `Repaints` | 車両 ID（必須。省略時: 空のリスト） | `<cti path>#item:<ordinal>` | `repaint` | `[item]` の名前 |
| `Hofs` | なし | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | 車両 ID（必須。省略時: 空のリスト） | 車両相対の `[number]` ソースパス | `fleet-number` | `null` |
| `Registrations` | 車両 ID（必須。省略時: 空のリスト） | `registration_automatic` / `registration_list` / `registration_free` | `registration` | 最初の値の行（free の場合は `null`） |
| `Addons` | なし | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | マップ ID（必須。省略時: `ArgumentException`） | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | エントリポイントのラベル |
| `SessionProfiles` | なし | パッケージのディレクトリ名（`/predefined-profile` の ID） | `session-profile`。パッケージがコンパイルできない場合は `session-profile-invalid` | プロファイルの `name`。無効なパッケージの場合は `OL_E_SESSION_PROFILE_*` コード |
| `RuntimeModules` | なし | モジュール ID | `runtime-module`。読み込めない場合は `runtime-module-invalid` | モジュールの `name`、または `OL_E_MODULE_*` コード |

エントリポイントは `PresentedEntrypointIndex` または `EntrypointIdentity`（探索 ID または正確なラベル）で選択します。プランナーは ID を OMSI が提示するラベルに解決し（OMSI は各正確なラベルの最初のレコードだけを提示します）、プランはそれを種類 `entrypoint` で `ResolvedContent` に一覧します。`world.entrypoint-identity` はランタイム検証されるまで `EXPERIMENTAL` です。

### `ListSessionProfilesAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 目的 | `<root>\.omsilaunch\session-profiles\` 配下のすべてのセッションプロファイルパッケージを、そのプリセットとともに一覧します。フロントエンドは YAML を解析せずにそれらを提示できます。CLI の `/list:SessionProfiles`。 |
| 戻り値 | パッケージのディレクトリごとに 1 つの `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)`（`Id` でソート）。`Presets` はインデックス順の `SessionProfilePresetSummary(Index, Id, Name)` です。各パッケージは起動時と同じ厳格な規則（`SessionProfileCompiler`）でコンパイルされます。コンパイルできないパッケージは `IsValid = false`、その `OL_E_SESSION_PROFILE_*` コードとメッセージとともに一覧され、プリセットは持ちません。プロファイルのディレクトリがない場合: 空のリスト。 |
| スローされる例外 | 開始時の `OperationCanceledException`。コンパイルエラーは結果に報告され、スローされません。 |
| Running セッションが必要か | いいえ。 |
| OMSI / ファイルシステム / トランザクションを変更するか | いいえ / いいえ / なし。 |
| 安定性 | `STABLE_BETA`（オフラインゲート `session-profiles.discovery`）。 |
| 例 | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 目的 | `plugins\OmsiLaunch.Modules` 配下にインストールされた[ランタイムモジュール](runtime-modules.md)を、その整合性と設定とともに探索します。読み取り専用です。読み込みはオープンです。モジュールがリストや署名と照合されることはありません。 |
| 戻り値 | モジュールのディレクトリごとに 1 つの `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)`（ディレクトリ名でソート）。`Activation` は `requested`、`content` または `default` です。モジュールを読み込めない場合、`IsValid` は false で `OL_E_MODULE_*` コードが付きます。`Settings` は、モジュールが宣言する設定を `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)` として一覧します。 |
| スローされる例外 | 開始時の `OperationCanceledException`。 |
| Running セッションが必要か | いいえ。CLI: `/list:RuntimeModules`（エンベロープ `runtime-modules.list`）。 |

### `RecoverPendingAsync`

| 項目 | 詳細 |
| --- | --- |
| シグネチャ | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 目的 | クラッシュしたオーナーが残した失効済みの永続トランザクション（`<root>\.omsilaunch\journal.json`）を報告または完了させます。呼び出しの間はインストールリースを取得するため、開始中のセッションの下で復元を行うことはありません。パブリックケイパビリティ `session.recover`。CLI では `/recovery-status` と `/recover`。 |
| パラメーター | `installation.RootPath`: インストールルート（`Path.GetFullPath` で正規化されます）。`restore`: `false` = 報告のみ。`true` = 復元、検証、ジャーナルとバックアップの削除。 |
| 戻り値 | `RecoveryStatus(Pending, Recovered, Diagnostics)`: `Pending` = 呼び出し開始時にジャーナルが存在した。`Recovered` = 復元が要求されて実行され、ジャーナルが残っていない。`Diagnostics` = 復元に関する記録（`Data["sha256"]` を伴う `restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`）。何も復元されなかった場合は空です。 |
| スローされる例外 | リースが保持されている場合は `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`。ジャーナルの PID + 作成時刻 + 実行ファイルパスが動作中のプロセスとまだ一致する場合、または（ジャーナルが `HandoffCreated` を過ぎていて PID がない場合）そのルートの `Omsi.exe` のいずれかが実行中の場合は `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`。`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`、または検証メッセージ（"Restore hash mismatch: ..."、"Restore presence mismatch: ..."）を伴う `IOException`。破損したジャーナルに対しては `InvalidDataException("Invalid OmsiLaunch journal.")` / `JsonException`。空のルートに対しては `ArgumentException`。`OperationCanceledException`。復元の開始後に例外がスローされた場合は常にジャーナルが保持され、次回の呼び出しでべき等に再実行されます。 |
| キャンセル | ジャーナル/バックアップの書き込みに渡されます。復元の途中でキャンセルすると、ジャーナルは保留中のまま残ります。 |
| Running セッションが必要か | いいえ（オーナーが動作中の間は拒否します）。 |
| OMSI の状態を変更するか | いいえ。 |
| ファイルシステムを変更するか | `restore == true` の場合のみ: 検証済みのバックアップから元のファイルを書き戻し（バイト列、最終書き込み時刻、作成時刻、属性。読み取り専用の元ファイルも処理。ライトスルー + フラッシュ。`*.omsilaunch.tmp` は残しません）、セッションの成果物を削除し、`journal.json` と `backup\<sessionId>` を削除します。 |
| トランザクション / 復元 | 保留中のトランザクションを完了させます（`Restoring` → `Restored` → ジャーナル削除）。 |
| 安定性 | `STABLE_BETA`: 報告経路と早期終了時のリカバリ（マトリクス RV-008）、復元失敗後のリカバリ（ランタイムクロージャ `F01`）、オーナー動作中および PID 記録前の時間帯における拒否（`S04`）、開始時のフィンガープリント取得前の遅延リカバリ（`S05`）。[ランタイム検証ステータス](../status/runtime-validation-status.md) を参照してください。 |
| 例 | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## コントラクト型

<a id="optional-values-and-semantic-primitives"></a>
### 省略可能な値とセマンティックなプリミティブ

| 型 | 定義 | 備考 |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`。`IsSet`、static `Unset`、static `Set(T)` | 「要求されていない」と「値を指定して要求された」を区別します。JSON の形式は [LaunchSpec リファレンス](launchspec.md) に記載しています。 |
| `Presence` | `Unset` = 0、`Set` = 1 | byte 列挙型です。 |
| `SemanticDate` | `(int Year, int Month, int Day)` | `DateTimeMode.Explicit` の場合のみ検証されます（月 1..12、日 1..31）。 |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | `DateTimeMode.Explicit` の場合のみ検証されます（0..23、0..59、0..59）。 |

<a id="launchspec-family"></a>
### LaunchSpec ファミリー

以下のレコードはすべて、[LaunchSpec リファレンス](launchspec.md) でプロパティごとに説明しています。この表は型の一覧を確定するものです。

| 型 | 目的 | 安定性 |
| --- | --- | --- |
| `LaunchSpec` | ルートの要求レコード。`null` の省略可能メンバーを既定値で置き換えるアクセサー `EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures` を持ちます。 | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`、`ExpectedExecutableSha256`（`Omsi.exe` の任意の固定指定。プラン作成時に検査されます）。 | `STABLE_BETA` |
| `WorldSpec`、`WorldMode`、`EntrypointSpec`、`EntrypointMode` | ワールドの選択。`WorldMode`: `NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（`LastMapState` の旧エイリアス。「最新の .osn」という意味では決してありません）。`EntrypointMode`: `Unset`、`PresentedIndex`、`Identity`（`WorldSpec.Entrypoint` から算出）。 | `NewMap`、`SavedSituation`: `STABLE_BETA`。`LastMapState`: `UNAVAILABLE`。`EntrypointMode.Identity`: `PARTIAL` |
| `DateSpec`、`TimeSpec`、`YearSpec`、`DateTimeMode` | `DateTimeMode`: `Unset`、`Explicit`、`System`。`Unset` 以外のモードを指定するとプランは実行不可になります。 | `PARTIAL`（プランのエビデンスは `UNAVAILABLE`） |
| `WeatherSpec`、`WeatherMode` | `WeatherMode`: `Unset`、`Preset`、`Icao`、`RealCurrent`。`Unset` 以外のモードを指定するとプランは実行不可になります。 | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`、`Repaint`、`Hof`、`FleetNumber`、`Registration`、`Enabled`。いずれかのフィールドを設定するとプランは実行不可になります。 | `PARTIAL` |
| `EnvironmentSpec` | セマンティックな `options.cfg` 設定をまとめた 8 つの `IReadOnlyDictionary<string, OptionalValue<string>>` グループ。 | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`、`ControllerDocument`。いずれかの値を設定するとプランは実行不可になります。 | `PARTIAL` |
| `DiagnosticsSpec` | 6 つのブール値。保持されますが、使用されません。 | `PARTIAL` |
| `SessionPresentationSpec`、`SplashMode` | `SplashMode`: `Unset` = 0、`Native` = 0（エイリアス）、`Managed` = 1。 | `STABLE_BETA` |
| `InternetTexturesSpec`、`InternetTexturesMode` | `InternetTexturesMode`: `Native`、`Disabled`、`Override`。 | `STABLE_BETA`（`Native`）、`EXPERIMENTAL`（`Disabled`、`Override`） |
| `SessionProfileMetadata` | コンパイル済みセッションプロファイルの由来情報（`Id`、`Name`、`Version`、`Author`、`PresetId`、`PresetIndex`、`PresetName`、`PackagePath`）。 | `STABLE_BETA` |
| `HofManagementSpec` | セッションの [HOF 管理](hof-management.md): `Operations`（`HofOperationSpec` のリスト）。フィールド `LaunchSpec.HofManagement`、アクセサー `EffectiveHofManagement`。 | `STABLE_BETA` |
| `RuntimeModulesSpec` | セッションの[ランタイムモジュール](runtime-modules.md): `Enable`（モジュール ID）、`Disable`（除外するモジュール ID）、`Settings`（`"<module id>.<key>"` から値へ）。アクセサー `EffectiveDisable`、`EffectiveSettings`。フィールド `LaunchSpec.Modules`、アクセサー `EffectiveModules`。 | `EXPERIMENTAL` |
| `PlannedModule` | モジュールのプラン評価: `Id`、`Version`、`Status`（`ELIGIBLE` または `REJECTED`）、`Requested`、`FailurePolicy`、`Operations`（`module.<id>.<operation>`）、`Code`、`Reason`、`ManifestSha256`、`Activation`（`requested`、`settings-file`、`content` または `default`）、`Settings`、`RuntimeOperations`（モジュールがプロセス内で実行するカタログ操作。OMSI を変更する場合は、それぞれの変更ポリシーを併記します。例: `camera.set (omsi-state)`）、`TickMilliseconds`、`SnapshotSha256`（プラン作成時点のモジュールのファイルのダイジェスト。[ランタイムモジュール](runtime-modules.md#integrity)を参照）。`IsEligible`、`EffectiveSettings`、`EffectiveRuntimeOperations`。定数 `Eligible`、`Rejected`、`ActivationRequested`、`ActivationSettingsFile`、`ActivationContent`、`ActivationDefault`。 | `EXPERIMENTAL` |
| `PlannedModuleSetting` | モジュールの有効な設定 1 件: `Key`、`Value`、`Source`（`request`、`settings-file` または `default`）。 | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | インストール済みモジュールの探索ビュー。`ListRuntimeModulesAsync` を参照。 | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | モジュールが宣言する設定: `Key`、`Type`、`Default`、`AllowsAuto`、`Minimum`、`Maximum`、`Choices`、`Flag`、`Description`。 | `EXPERIMENTAL` |
| `HofOperationSpec` | HOF 操作 1 件（`SourceKind`、`Source`、`TargetVehicles`、`Mode`、`Files`）。 | `STABLE_BETA` |
| `HofSourceKind` | `Collection`、`Directory`、`Vehicle`、`File`。 | `STABLE_BETA` |
| `HofApplyMode` | `Add`、`Mirror`。 | `STABLE_BETA` |
| `HofPlanningException` | HOF のプランニング（`OmsiLaunch.Core`）がスローします。`Code` は `OL_E_HOF_*` コードです。パブリック API はこれをプラン診断として報告します。 | `STABLE_BETA` |
| `SessionProfileSummary` | 1 つのパッケージの探索ビュー（`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity`、`Presets`、`IsValid`、`ErrorCode`、`ErrorMessage`）。`ListSessionProfilesAsync` を参照。 | `STABLE_BETA` |
| `SessionProfilePresetSummary` | サマリー内のプリセット 1 件（`Index`、`Id`、`Name`）。 | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration`（保持されますが、復元は常に行われます）、`SuppressStaleClosecheckWarning`、`StartupTimeoutSeconds`（1..600、既定値 180）、`ShutdownTimeoutSeconds`（保持されますが、使用されません）。 | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### プラン型とステータス型

| 型 | フィールド | 備考 |
| --- | --- | --- |
| `SessionPlan` | `SessionId`（プランごとに新しい `Guid`）、`BuildProfileId`（`"Omsi23004"` または `"unrecognized"`）、`Spec`、`Platform`（`RuntimePlatformInfo`）、`ResolvedContent`（`ContentIdentity` のリスト: `map`、`vehicle`、`repaint`、`hof`、`situation`、`situation-map`）、`TouchedFiles`（`PlannedMutations` の重複を除いた相対パス）、`RuntimeArtifacts`、`RequiredCapabilities`（`STATICALLY_VALIDATED` または `UNAVAILABLE` の `Capability`。ネイティブ開始ケイパビリティは `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` または `PENDING_IN_PROCESS_RESOLUTION`）、`UnsupportedRequestedFeatures`（要求されたが非対応の機能に対する `Capability` エントリ）、`PlannedMutations`、`Diagnostics`、`IsRunnable`。 | パブリックなレコードであり、編集されたり失効したりする可能性があります。そのため `StartSessionAsync` はプランを再作成します。 |
| `RuntimePlatformInfo` | `OsFamily`、`OsVersion`、`OsArchitecture`、`HostArchitecture`、`OmsiArchitecture`（`X86`）、`PluginArchitecture`（`X86`）、`CurrentPlatformSupported`（Windows 10 以降、x64 OS かつ x64 ホスト）、`LegacyPlatform`（常に `false`）、`Wow64Available`、`InstallationWritable`、`ProcessLaunchSupported`、`PluginRuntimeSupported`、`NativeInteropSupported`、`SharedMemorySupported`、`ExactRestoreSupported`（いずれも `CurrentPlatformSupported` と等しい）。 | |
| `Capability` | `Name`、`Available`、`EvidenceState`、`Reason`。 | `EvidenceState` は `PublicEvidence` の値（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`）です。ただし `SessionPlan.RequiredCapabilities` では、プラットフォームまたはコンテンツの要件は `STATICALLY_VALIDATED` または `UNAVAILABLE`、ネイティブケイパビリティは `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` または `PENDING_IN_PROCESS_RESOLUTION` です。 |
| `PlannedMutation` | `RelativePath`、`SemanticKey`、`RequestedValue`、`Operation`（`token-patch`、`vector-component-patch`、`exact-file-overlay`）。 | 表示関連の変更にはキー `session-presentation.splash`、`internet-textures.override`、`internet-textures.cache`、`internet-textures.target` が使われます。 |
| `LaunchDiagnostic` | `Code`、`Message`、`Data`（省略可能な文字列マップ）。 | `OL_E_` で始まるコードはエラー、`OL_W_` は警告、それ以外は情報です。 |
| `SessionStatus` | `SessionId`、`State`（`SessionState`）、`Diagnostics`、`RuntimeEvents`。 | セッションの診断にプランの診断は含まれません。 |
| `RuntimeEvent` | `Type`、`TimestampUtc`（ホストの受信時刻）、`Sequence`（1 から始まる、セッションごと）、`Data`、`SessionTickMilliseconds`（イベントを発行したときのプラグインの単調増加するセッションクロック。ホストのイベントでは null）。 | 最新の 256 件のイベントに制限されます（古いものから破棄）。プラグインのイベントはテレメトリリングを通じて順序どおりに届きます。リングのオーバーランで失われたイベントは `telemetry.lost` で数えられます。 |
| `SessionHandle` | `SessionId`。 | プロセスローカルです。 |
| `RecoveryStatus` | `Pending`、`Recovered`、`Diagnostics`。 | `RecoverPendingAsync` を参照してください。 |
| `ContentIdentity` | `Identity`、`Kind`、`DisplayName`。 | `DiscoverAsync` を参照してください。 |
| `ContentQueryKind` | `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`。 | |

### `SessionState`

宣言順の byte 列挙型: `Created`、`ValidatingPlatform`、`Planning`、`AcquiringInstallationLock`、`RecoveringPreviousTransaction`、`Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`WaitingForPlugin`、`PluginBootstrap`、`StartingWorld`、`EnteringGameplay`、`Running`、`ProcessExited`、`Restoring`、`CleaningRuntime`、`Completed`、`Failed`。`ValidatingPlatform`、`Planning`、`EnteringGameplay` は現在のサービスでは一度も設定されません。`Snapshotting` は一時的な状態で、実際にはほぼ観測できません。終了状態: `Completed`、`Failed`。セマンティクスの詳細: [セッションライフサイクル](../concepts/session-lifecycle.md)。

<a id="runtime-control-types"></a>
### ランタイム制御の型

| 型 | 定義 | 安定性 |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | ホストとプラグインがメールボックスのエンベロープに使用する静的コーデック: マジック `0x4F4C5243`（"OLRC"）、バージョン 1、72 バイトのリトルエンディアンヘッダー（マジック、バージョン、種別 1 = 要求 / 2 = 応答、全体長、セッションの `Guid`、要求 ID、ペイロード長、ペイロードの SHA-256）に続いて UTF-8 の JSON ペイロード。`SerializeRequest`、`SerializeResponse`、`TryDeserializeRequest`、`TryDeserializeResponse`、`TryReadRequestId`。 | `INTERNAL`: ブリッジの両端が共有するためパブリックになっていますが、統合用のサーフェスではありません。フォーマットはプロトコルバージョンとともに変わる可能性があります。 |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)` — ホストがメモリマップトファイル `OmsiLaunch.Handoff.<sessionId>` でプラグインに公開する内容です。 | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256, string Settings, string SnapshotSha256)`: ハンドオフ（`StartupHandoff.Modules`）が運ぶ、計画されたモジュール。`SnapshotSha256`（ワイヤーバージョン 8）は、プラン作成時に取得したモジュールのファイルのダイジェストで、プランに含まれていなければ空です。 | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`、`IsEmpty`: ハンドオフ（`StartupHandoff.Start`）が運ぶ、NEW_MAP セッションの解決済みの開始クロック。null のフィールドは OMSI 自身の値を保持します。 | `INTERNAL` |
| `StartupHandoffWire` | コーデック: マジック `0x4F4C5348`、バージョン 8（3 から 8 を読み取り可能。バージョン 5 は計画されたモジュールを、バージョン 6 は各モジュールの有効な設定を、バージョン 7 は開始日時を、バージョン 8 は各モジュールのプラン時ファイルスナップショットを追加します。フラグビット 0x04 と 0x08 は `PluginTrace` と `NativeTrace` を運びます）、SHA-256 によるペイロード整合性を持つ 64 バイトのヘッダー。 | `INTERNAL` |
| `TelemetryRingWire` | マッピング `OMSILAUNCH_TELEMETRY_NAME` 内の、プラグインからホストへのイベントリング: マジック `0x52544C4F`（"OLTR"）、バージョン 1、2048 バイトの番号付きスロット 64 個。`Initialize`、`Write`、`IsRing`、`ReadSince`（シーケンス番号より後のイベントと、失われた数を返します）。 | `INTERNAL` |

プラグインは、`WorldMode` が `NewMap` または `SavedSituation` であり、`HeadlessStart` が true、`PlayerVehicleEnabled` が false、日付/時刻の両モードが `Unset` で、保存済みシチュエーションの場合は ID が空でない、という条件をすべて満たさない限り、ハンドオフを拒否します（`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`）。プランナーが同じ制約をより早い段階で強制するため、実行可能なプランがこれを引き起こすことはありません。

<a id="capability-registry-types"></a>
### ケイパビリティレジストリの型

| 型 | 目的 |
| --- | --- |
| `PublicCapabilityRegistry` | 正規のケイパビリティカタログ。`ProtocolVersion`（`"0.1"`）、`All`（すべての `PublicCapabilityDescriptor`）、`Get`、`TryGet`、`PublicRuntimeOperationIds`（フロントエンドが転送してよい 48 個の具体的な操作 ID）、`RuntimeOperations`、`TryGetOperation`、`NativeCapabilityOf`、`CliRoutes`（コマンドルートから操作 ID）、`IsPublicRuntimeOperation`、`GetRuntimeArguments`、`ValidateRuntimeArguments`（`PublicRuntimeArgumentValidation` を返します）、`IsInternalResultKey`。`ExecuteRuntimeAsync`、CLI、ローカルコントロールプレーン、およびプラグインによって強制されます。 |
| `PublicCapabilityDescriptor` | `Id`、`Family`、`Classification`、`Kind`、`RequiresSession`、`RequiresExactProfile`、`ApiRoute`、`CliRoute`、`RuntimeValidation`（`PublicEvidence` の値）、`Description`、`HandleTypes`、`Operations`、`NativeCapability`、`EvidenceNote`。導出されるもの: `EffectiveOperations`、`NativeCapabilities`。 |
| `PublicRuntimeOperation` | `Id`、`NativeCapability`、`Arguments`、`CliRoutes`、`Paged`（`cursor`/`limit` を受け取ります。[ページネーション](capabilities.md#pagination)を参照）、`MutationPolicy`。 |
| `PublicMutationPolicy` | ランタイム操作が何を変更するか: `None`、`OmsiState`、`SessionResource`、`SessionPolicy`、`All`。 |
| `PublicEvidence` | エビデンスの語彙: `RuntimeValidated`、`StaticallyValidated`、`Partial`、`RuntimeValidationRequired`、`Unavailable`、`All`。 |
| `PublicCapabilityClassification` | `PublicStableBeta`、`PublicExperimental`、`InternalOnly`、`Unsupported`。 |
| `PublicCapabilityKind` | `Read`、`Write`、`Action`、`Event`。 |
| `PublicRuntimeArgumentDescriptor` | `Name`、`Required`、`Description`。 |
| `PublicRuntimeArgumentValidation` | `Accepted`、`ErrorCode`、`Message`。 |

完全なカタログ: [ケイパビリティ](capabilities.md)。

<a id="d3druntimeapi-extension-methods"></a>
### `D3DRuntimeApi` 拡張メソッド

`d3d.*` 操作（`EXPERIMENTAL`、`PublicExperimental` ケイパビリティ `d3d.texture`）用に `ExecuteRuntimeAsync` をラップした型付きラッパーです。要求 ID は 30 000 から始まるプロセス全体のカウンターから割り当てられ、タイムアウトの既定値は 5 s です（ただし `GetD3DStatusAsync` はタイムアウトの指定が必須です）。

| メソッド | 操作 | 引数と制限 |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | なし |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | width/height 1..4096、levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`: x/y 0..4095、width/height 1..4096、pixels ≤ 48 KiB（ワイヤー上では Base64 エンコード） |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | 繰り返しの解放は `OL_E_D3D_RESOURCE_RELEASED` で拒否されます |

型: `D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`。`D3DDeviceState`: `NotReady`、`Ready`、`Lost`、`Resetting`、`Stopping`、`Stopped`。`D3DTextureHandle(Value)`（`Value = "d3dtex-<session id N>-<16 hex>"`）。`D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`。`D3DTextureResourceState`: `Live`、`Released`、`Stale`。`D3DTextureFormat`: `A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`。

エラー: 失敗した結果は `OmsiRuntimeException(Code, detail)` として再スローされます。`Code` は結果の `ErrorCode`（存在しない場合は `OL_E_RUNTIME_OPERATION_FAILED`）で、メッセージは `"<code>: <Values["detail"]>"` です。値を持たない成功結果、または不明なデバイス状態の文字列は `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)` をスローします。`ExecuteRuntimeAsync` がスローする例外はすべてそのまま伝播します。デバイスリセットの処理はランタイム検証済みです。リセットによってデバイスは `Resetting` を経て `Ready` に戻り、有効なテクスチャはすべて無効化されます（`OL_E_D3D_STALE_RESOURCE_HANDLE`、ランタイムクロージャ `D01`）。カタログエントリ `d3d.texture` がこれを記録しています。`Lost` への遷移は製品の外部から発生させることができず、オフラインでのみカバーされています。

```csharp
var status = await launch.GetD3DStatusAsync(session, TimeSpan.FromSeconds(5));
if (status.State == D3DDeviceState.Ready)
{
    var texture = await launch.CreateD3DTextureAsync(session, 8, 8, D3DTextureFormat.A8R8G8B8);
    var pixels = new byte[8 * 8 * 4];
    await launch.UpdateD3DTextureAsync(session, texture.Handle, new D3DTextureUpdate(0, 0, 0, 8, 8, pixels));
    await launch.ReleaseD3DTextureAsync(session, texture.Handle);
}
```

<a id="process-contract-types"></a>
### プロセスコントラクトの型

| 型 | 内容 |
| --- | --- |
| `PublicExitCode` | `Success` = 0、`SessionFailed` = 1、`InvalidArguments` = 2、`UnsupportedProfile` = 3、`NoActiveSession` = 4、`RuntimeUnavailable` = 5、`NotFound` = 6、`OperationRejected` = 7、`TransactionRecoveryFailed` = 8、`InternalError` = 10。CLI のみが使用します（[終了コード](exit-codes.md)）。API がプロセスを終了させることはありません。 |
| `PublicErrorCategory` | CLI/制御のエラーエンベロープで使用される文字列定数: `invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。 |
| `PublicErrorCodes` | コードごとに 1 つの `const string`（143 個: `OL_E_` エラー 142 個と `OL_W_` 警告 1 個）と、`PublicErrorDescriptor(Code, Category)` のカタログである `All`。カテゴリ: `Cli`、`Compatibility`、`Content`、`Installation`、`InvalidArgument`、`LaunchSpec`、`LocalControl`、`Other`、`Presentation`、`Process`、`Runtime`、`RuntimeD3D`、`Session`、`SessionProfile`、`Transaction`、`Warning`。リファレンス: [エラーコード](errors.md)。 |
| `PublicErrorDescriptor` | `(string Code, string Category)`。 |
| `OmsiRuntimeException` | `Code` プロパティとメッセージ。`D3DRuntimeApi` からのみスローされます。 |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths`（インストール環境の ID とパスの包含判定）

安定性: `STABLE_BETA`（純粋関数であり、I/O なし、OMSI の状態なし、ファイルシステムの変更なし、トランザクションへの関与なし、Running セッション不要）。インストールリース、ローカル制御のパイプ名、セッションプロファイルのアセットの閉じ込め、Internet Textures のターゲット検証、ランタイムのスポーン用モデルパスで使用される唯一の定義です。

| メンバー | 動作 |
| --- | --- |
| `string NormalizeRoot(string root)` | 末尾の区切り文字を除いた `Path.GetFullPath(root)`。ただしドライブルート（`C:\`）はそのまま保持されます。`.` と `..` のセグメントを解決し、`/` と `\` を同一に扱い、連続する区切り文字を 1 つにまとめます。ジャンクションやシンボリックリンクは解決**しません**。ルートが null または空白の場合は `ArgumentException` をスローします。 |
| `string IdentityKey(string root)` | 大文字化した `NormalizeRoot(root)`。字句上同等な同一ルートの表記（`C:\OMSI`、`C:\OMSI\`、`C:\OMSI\.`、`C:\foo\..\OMSI`、`c:\omsi`）は 1 つのキーを共有し、異なるルート（`C:\OMSI-A`、`C:\OMSI-B`）がキーを共有することはありません。 |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | `candidate`（`root` からの相対パス、または絶対パス）を解決し、それが `root` の厳密に下位にある場合にのみ `true` を返します。`relativePath` は `\` を使った正規の表記です。`Path.GetRelativePath` のセグメントを使用するため、`C:\OMSI-A\x` のような兄弟パスが `C:\OMSI` の内側と判定されることはありません。ルート自体、別のボリューム、`..` による脱出は `false` を返します。 |
| `IReadOnlyList<string> Segments(string relativePath)` | `/` と `\` で分割し、空のセグメントを除外します。 |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### セッションプロファイル（`OmsiLaunch.Core`）

安定性: `EXPERIMENTAL`。これらの型は、YAML の [セッションプロファイル](session-profiles.md)（`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`、スキーマ `omsilaunch.session-profile/v1`）を `LaunchSpec` にコンパイルします。CLI の `/predefined-profile:<id> /predefined-profile-index:<n>` はまさにこれらの呼び出しを使用しています。インテグレーターはこれらを使って API 経由でプロファイルを開始できます。

| メンバー | 動作 |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | パッケージを読み取って検証します。`id` は単純なディレクトリ名である必要があります（そうでない場合は `OL_E_SESSION_PROFILE_PATH_ESCAPE`）。`presetIndex` は 1..99 です（`MinPresetIndex`..`MaxPresetIndex`。それ以外は `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）。ファイルが存在しない場合: `OL_E_SESSION_PROFILE_NOT_FOUND`。`MaxBytes`（256 KiB）より大きい、YAML が無効、アンカーがある、不明なキーがある、または `id` がディレクトリ名と異なる場合: `OL_E_SESSION_PROFILE_INVALID`。それ以外の `schema`: `OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`。アセットのパスはパッケージ内に閉じ込められます（`OL_E_SESSION_PROFILE_PATH_ESCAPE`、`OL_E_SESSION_PROFILE_ASSET_MISSING`）。すべての失敗は `SessionProfileException` になります。 |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | プロファイルを適用した `baseline` を返します。`NewMap` の場合はプロファイルの `new` ブロック（マップ、エントリポイント、および日付/時刻/年/天候）、プリセットの `settings` を `Environment.General` の上にマージしたもの、存在する場合はプリセットの `Presentation`、`InternetTextures`、`Behavior`、そして `SessionProfile` = `profile.Metadata` が適用されます。`NewMap` の場合は、マップをプロファイルの `compatibility` リストとも照合します（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。 |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | ほかのモード向けの互換性チェックです（`SavedSituation` の場合、マップは `.osn` から読み取られます）。CLI は最終的な `WorldSpec` を構築した後にこれを呼び出します。 |
| `SessionProfileCompiler.Schema`、`MaxBytes`、`SchemaKeys` | `"omsilaunch.session-profile/v1"`、`262144`、および YAML マッピングごとの受け付けるキー。 |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`、`ProfileNew`、`ProfilePreset` | 読み込まれたパッケージ。`Preset` は選択されたプリセットのみです。 |
| `SessionProfileException(string code, string message)` | `Code`（`OL_E_SESSION_PROFILE_*` コードのいずれか）を持つ `IOException`。メッセージは `"<code>: <message>"` です。 |

CLI はさらに、プロファイルと矛盾するコマンドラインフラグを拒否します（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）。このチェックはコンパイラーには含まれていません。マージ順序の詳細は [セッションプロファイル](session-profiles.md#precedence-and-override-conflicts) を参照してください。

```csharp
static async Task<SessionPlan> PlanProfileAsync(IOmsiLaunch launch, LaunchSpec baseline, string installationRoot, string profileId)
{
    // baseline: a LaunchSpec for installationRoot with World.Mode = WorldMode.NewMap (see the complete example).
    var profile = SessionProfileCompiler.Load(installationRoot, profileId, presetIndex: 1);
    var spec = SessionProfileCompiler.Apply(profile, baseline, WorldMode.NewMap);
    return await launch.PlanSessionAsync(spec); // plan.Spec.SessionProfile carries the provenance
}
```

<a id="platform-types-omsilaunchprocess"></a>
### プラットフォームの型（`OmsiLaunch.Process`）

| 型 | 安定性 | 用途 |
| --- | --- | --- |
| `IRuntimePlatform` | `OmsiLaunchService` のコンストラクターのパラメーター型としては `STABLE_BETA` | プラットフォームの検出、書き込み可否の検査、`Omsi.exe` の起動・監視・強制終了・終了待機を行います。`new CurrentWindowsX64Platform()` を渡してください。独自に実装することはサポートされていません。 |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | 唯一の実装です: Windows x64 ホスト、`Omsi.exe` には `CreateProcessW`、正規の停止には `TerminateProcess` を使用します。メソッドはサービスから呼び出され、インテグレーターは構築するだけです。メンバー（`IRuntimePlatform` と共通）: `Detect(root)` はプランの `RuntimePlatformInfo` を返します。`ValidateCurrent(info)` は、ホストでセッションを実行できない場合に `OL_E_UNSUPPORTED_OPERATING_SYSTEM` / `OL_E_UNSUPPORTED_OS_ARCHITECTURE` / `OL_E_PLATFORM_CAPABILITY_MISSING` をスローします。`IsInstallationWritable(root)` は `OL_E_INSTALLATION_NOT_WRITABLE` の判定の根拠になります。`StartAsync(request, sha256)` は `Omsi.exe` を作成し、プロセスの識別情報（PID、作成時刻、パス、およびサービスが計算したハッシュ）を記録します（`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`）。`HasExited`、`WaitForExitAsync`、`Terminate` はプロセスを監視し、終了させます。 |
| `InstallationLease`、`LaunchedProcess`、`ProcessIdentity`、`ReleaseManifest`、`RuntimeArtifact`、`RuntimeArtifactSet`、`StartupProcessRequest`、`CurrentRuntimeCommandStore`、`IOmsiProcessController`、`OmsiProcessState` | `INTERNAL` | サービスとテストが共有するため、アセンブリ上はパブリックです。統合用のサーフェスではありません。`LaunchedProcess` は OMSI のプロセスハンドルとスレッドハンドルを内部でラップしており（パブリックメンバーではありません）、`IOmsiLaunch` から返されることはありません。 |

<a id="thread-safety"></a>
## スレッドセーフ

- `OmsiLaunchService` は、異なるセッションに対する同時呼び出しに対して安全です。セッションは `ConcurrentDictionary` に格納され、セッションごとの変更はすべてそのセッション専用のロックの下で行われます。
- 同じセッションに対する同時呼び出しも安全ですが、必要な箇所では直列化されます。`ExecuteRuntimeAsync` はセッションごとのゲートを取得するため、2 つ目のコマンドは 1 つ目の完了を待ちます（そのタイムアウトはステージングされた時点から始まります）。
- スーパーバイザーは、`StartSessionAsync` が戻った時点からセッションが終了状態になるまで、スレッドプールのタスク（`Task.Run`）上で動作し、100 ms ごとにテレメトリとプロセスをポーリングします。呼び出し元がスーパーバイザーのコードを実行することはありません。
- `StopAsync` と `GetStatusAsync` は同期的に完了し、`ProcessExit` ハンドラーの内部を含め、任意のスレッドから呼び出せます（CLI は 4 s の猶予でこれを行っています）。
- スレッドアフィニティを持つ API 呼び出しはなく、同期コンテキストを必要とするものもありません。

<a id="what-is-not-in-the-api"></a>
## API に含まれないもの

- `IntPtr`、`nint`、Win32 ハンドル、ネイティブアドレス、VMT ポインター、プロセスオブジェクトは含まれません。キーが `internal_` で始まる、または `_address`、`_pointer`、`_vmt` で終わる結果値は、結果が `ExecuteRuntimeAsync` から返される前に除去されます。
- `internal.*` ランタイム操作は含まれません。`internal.road-vehicles.make-basic` はレジストリ上 `InternalOnly` であり、API と CLI からは `OL_E_RUNTIME_OPERATION_UNKNOWN` を返します。
- OMSI メモリの生の読み書きはなく、`LaunchSpec` が宣言するもの以外にインストール環境へのファイルレベルのアクセスもありません。
- プロセス間ハンドルはありません。[ローカルコントロールプレーン](local-control.md) が唯一のプロセス間経路であり、受け付けるのは `session.status`、`session.events`、`session.stop`、`runtime.execute` のみです。
- 開始時のプレイヤー車両の適用はありません。協調的シャットダウン、`LAST_MAP_STATE`、開始日時/天候、キーボード/コントローラードキュメントのオーバーレイは `EXPERIMENTAL` です。

<a id="stability-summary"></a>
## 安定性のまとめ

| サーフェス | 安定性 |
| --- | --- |
| `OmsiLaunchService` コンストラクター、`OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`、`StartSessionAsync`（NEW_MAP、SAVED_SITUATION）、`GetStatusAsync`、`WaitForAsync`、`StopAsync`、`CloseAsync` | `STABLE_BETA` |
| `ExecuteRuntimeAsync` のトランスポート、`PublicStableBeta` の操作 | `STABLE_BETA` |
| `PublicExperimental` の操作、`D3DRuntimeApi`、`camera.lock` | `EXPERIMENTAL` |
| 日付/時刻/天候/入力の spec メンバー、`DiagnosticsSpec`、`ExpectedExecutableSha256`、`RestoreConfiguration`、`ShutdownTimeoutSeconds`、`CooperativeShutdown` | `EXPERIMENTAL` |
| プレイヤー車両の spec メンバー | `PARTIAL` |
| `RuntimeCommandWire`、`StartupHandoff`、`StartupHandoffWire`、`StartupModule`、`StartDateTime`、`StartWeather`、`TelemetryRingWire`、`IRuntimePlatform` の実装、すべての実装アセンブリ | `INTERNAL` |
| `WorldMode.LastSituation`、`internal.*` 操作 | `UNAVAILABLE` |
