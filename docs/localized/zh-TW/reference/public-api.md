# 公開 API 參考（`OmsiLaunch.Api`）

<!-- l10n: source=reference/public-api.md source-sha256=84642bb43094a73ea13c435992e09e49f1a9328928880336e171182bb1ad6851 -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/public-api.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁是 OmsiLaunch 0.1.0-beta4 受控（managed）公開 API 的規範性參考：涵蓋 `OmsiLaunch.Api` 組件（合約）以及 `OmsiLaunch.Core` 中供整合者使用的進入點 `OmsiLaunchService`。本頁只記載目前程式碼實際的行為。整合者可以呼叫、接收或觀察的一切，皆連同其穩定性等級列於此處；未列出者即不屬於整合介面。

自動產生的[公開 API 清單](public-api-inventory.md)列出 `OmsiLaunch.Api`、`OmsiLaunch.Core` 與 `OmsiLaunch.Process` 的每個公開型別與成員，並附上其簽章與穩定性；當清單與組件不一致時，文件檢查關卡會失敗。本頁則說明其語意。

相關頁面：[LaunchSpec 參考](launchspec.md)、[錯誤碼](errors.md)、[工作階段生命週期](../concepts/session-lifecycle.md)、[交易與復原](../concepts/transactions-and-recovery.md)、[執行階段控制](runtime-control.md)、[功能](capabilities.md)、[本機控制平面](local-control.md)、[結束代碼](exit-codes.md)、[執行階段驗證狀態](../status/runtime-validation-status.md)。

<a id="stability-vocabulary"></a>
## 穩定性用語

| 等級 | 在本頁的意義 |
| --- | --- |
| `STABLE_BETA` | 合約在 0.1 協定系列中已凍結，且該路徑已在 `research/reports/OMSILAUNCH-RUNTIME-VALIDATION-MATRIX.md` 中於執行階段驗證。 |
| `EXPERIMENTAL` | 可呼叫且已測試，但在成為穩定版之前，合約或執行階段證據仍可能變更。 |
| `PARTIAL` | 存在於合約中；僅實作或驗證了部分行為（內文會說明是哪一部分）。 |
| `INTERNAL` | 基於技術原因在組件中為公開（橋接層共用該型別），但不屬於整合介面；可能在未通知的情況下變更。 |
| `UNAVAILABLE` | 存在於合約中，但目前的組建會拒絕。 |

<a id="assembly-overview"></a>
## 組件概觀

| 組件 | 對整合者的角色 |
| --- | --- |
| `OmsiLaunch.Api` | 純合約：record、列舉、`IOmsiLaunch`、功能登錄、錯誤目錄、傳輸格式、D3D 輔助方法。不包含任何 `IntPtr`、`nint`、Win32 handle、原生位址或處理程序物件。 |
| `OmsiLaunch.Core` | `OmsiLaunchService`（`IOmsiLaunch` 的實作）、`OmsiLaunchRuntimePaths`、`SessionPlanner`、`LaunchValidation`、`SessionProfileCompiler`。 |
| `OmsiLaunch.Process` | `IRuntimePlatform` 與 `CurrentWindowsX64Platform`（唯一的平台配接器）、`InstallationLease`。建構服務時需要。 |
| `OmsiLaunch.Configuration`、`OmsiLaunch.Content`、`OmsiLaunch.Interop`、`OmsiLaunch.Plugin`、`OmsiLaunch.Builds.Omsi23004` | 實作組件。其公開型別對整合者而言為 `INTERNAL`。 |

<a id="entry-point-omsilaunchservice-and-omsilaunchruntimepaths"></a>
## 進入點：`OmsiLaunchService` 與 `OmsiLaunchRuntimePaths`

```csharp
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);
public sealed class OmsiLaunchService : IOmsiLaunch
{
    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths);
}
```

| 參數 | 有效值 | 無效值／預設值 |
| --- | --- | --- |
| `platform` | `new CurrentWindowsX64Platform()`（命名空間 `OmsiLaunch.Process`）。它會偵測平台、以 `CreateProcessW` 建立 OMSI 處理程序、等待並終止該處理程序。 | 未隨附其他實作。自訂的 `IRuntimePlatform` 屬於 `INTERNAL`。 |
| `PluginBuildDirectory` | 包含永久外掛程式封閉集合（closure）參考檔案的目錄：`OmsiLaunch.Plugin.opl`、`OmsiLaunch.PluginNE.dll`、`OmsiLaunch.Plugin.deps.json`、`OmsiLaunch.Plugin.runtimeconfig.json`，以及受控封閉集合中的每個 `OmsiLaunch.*.dll`（必須包含 `OmsiLaunch.Plugin.dll`）。在已安裝的套件中，此目錄為 `<package>\plugins`。 | 目錄或檔案不存在：`PlanSessionAsync` 回傳附有 `OL_E_RUNTIME_ARTIFACT_MISSING` 的不可執行計畫。 |
| `NativeBridgePath` | `OmsiLaunch.Native.x86.dll` 的路徑（在套件中為 `<package>\plugins\OmsiLaunch.Native.x86.dll`）。 | 同上。 |
| `ReleaseManifestPath` | 若存在，為 `OmsiLaunch.exe` 旁的 `release-manifest.json`。提供每個 `plugins/` 檔案的預期 SHA-256（`plugin.integrity.reference = manifest`）。 | `null`（開發配置）：已安裝的檔案僅依參考封閉集合檢查是否存在及自身一致性（`plugin.integrity.reference = self`）。資訊清單（manifest）格式錯誤：`OL_E_RELEASE_MANIFEST_INVALID`。 |

服務在每次 `PlanSessionAsync` 與 `StartSessionAsync` 時讀取這些路徑；它從不複製、暫存或移除外掛程式檔案（請參閱[永久外掛程式](../concepts/permanent-plugin.md)）。CLI 正是以如下方式建構服務（`tools/OmsiLaunch.Cli/Program.cs`）：

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

每個處理程序建立一個服務並共用它。穩定性：`STABLE_BETA`。

<a id="session-ownership-rules"></a>
## 工作階段擁有權規則

| 規則 | 詳細說明 |
| --- | --- |
| 每個 OMSI 安裝只有一個擁有者 | `StartSessionAsync` 會取得安裝租用（lease），即具名號誌 `Local\OmsiLaunch.Installation.<SHA-256 of the upper-cased full root path>`，並持有至監督程式（supervisor）還原該安裝為止。同一登入工作階段中的任何處理程序若對相同根目錄再次啟動，會以 `OL_E_INSTALLATION_BUSY` 失敗（以 `Failed` 工作階段回報，請參閱 `StartSessionAsync`）。此租用以登入工作階段為範圍，不跨登入工作階段；且只要另一個處理程序仍持有其 handle 就不會釋放（已接受的風險）。 |
| handle 僅限處理程序本機 | `SessionHandle` 包裝工作階段的 `Guid`。它只對回傳它的那個 `OmsiLaunchService` 執行個體有意義。在其他處理程序（或其他服務執行個體）中以已知 `Guid` 建構的 handle 會產生 `KeyNotFoundException`。跨處理程序控制須透過[本機控制平面](local-control.md)，而非透過 handle。 |
| 務必呼叫 `CloseAsync` | 自 `StartSessionAsync` 起，處理程序即擁有一個持久交易。`CloseAsync` 會在需要時要求標準停止，等待監督程式（處理程序結束、精確還原、租用釋放）後遺忘該工作階段。每條結束路徑都必須呼叫它，包括進入 `Failed` 狀態之後。若未呼叫，工作階段項目會留在記憶體中；還原本身無論如何都會由監督程式執行。 |
| 失敗的工作階段仍是工作階段 | 在 `StartSessionAsync` 回傳之後才失敗的啟動會回報 `SessionState.Failed`；在 `CloseAsync` 之前，該 handle 對 `GetStatusAsync`／`WaitForAsync` 仍然有效。 |
| 計畫會被重新檢查 | `StartSessionAsync` 會重新計算 `Omsi.exe` 的雜湊並重新規劃 spec；已不再可執行的計畫會以 `OL_E_PLAN_NOT_RUNNABLE` 被拒絕。 |

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

所有方法的共通事項：

- 未知或已關閉的 handle 會擲回 `KeyNotFoundException`（"Unknown OmsiLaunch session."）。
- 除 `ExecuteRuntimeAsync` 外，沒有任何方法需要處於 Running 狀態的工作階段。
- 帶有 OmsiLaunch 代碼的例外會將代碼置於 `Exception.Message` 的開頭（`"OL_E_PLAN_NOT_RUNNABLE: ..."`）。CLI 以相同方式從訊息中擷取代碼（`CliProgram.Classify`）。
- 支援的安裝：OMSI 2。原生功能會依組建在處理程序內解析並驗證；在某個組建上無法使用的功能會以 `OL_E_CAPABILITY_UNRESOLVED` 失敗。請參閱[相容性](compatibility.md)。

<a id="complete-minimal-example"></a>
### 完整的最小範例

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

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 用途 | 在不啟動 OMSI 的情況下，將 `LaunchSpec` 編譯為 `SessionPlan`：驗證 spec、偵測平台、為 `Omsi.exe` 計算指紋、解析內容識別、計算預定的檔案變更、列出所需與不支援的功能，並決定 `IsRunnable`。公開功能 `session.plan`。 |
| 參數 | `spec`：完整填入的 `LaunchSpec`（請參閱 [LaunchSpec 參考](launchspec.md)）。`Installation`、`World`、`Date`、`Time`、`Environment`（全部八個字典）與 `Behavior` 不得為 null；選用成員可為 `null`。`RootPath` 應為絕對目錄路徑；空的根目錄會被記錄為 `OL_E_INSTALLATION_NOT_FOUND`，但對空路徑進行平台探測會在計畫回傳前擲回 `ArgumentException`，因此切勿傳入空的根目錄。 |
| 回傳 | `SessionPlan`，包含新的 `SessionId`、`BuildProfileId` = 已知配置（`"Omsi23004"`），對沒有已知配置的 OMSI 2 組建則為 `"unrecognized"`、輸入的 `Spec`、`Platform`、`ResolvedContent`、`TouchedFiles`、`RuntimeArtifacts`（`plugins\OmsiLaunch.*` 目的地路徑，加上 `"OmsiLaunch startup handoff v6"`）、`RequiredCapabilities`、`UnsupportedRequestedFeatures`、`PlannedMutations`、`Diagnostics`、`IsRunnable`、`PlannedModules`（每個被要求或由內容啟用的[執行階段模組](../../../reference/runtime-modules.md)一個 `PlannedModule`；存取子 `EffectivePlannedModules`）。當且僅當沒有任何診斷訊息代碼以 `OL_E_` 開頭時，`IsRunnable` 為 `true`。資訊性診斷訊息（訊息為 `self` 或 `manifest` 的 `plugin.integrity.reference`、`session_profile.selected`）絕不會使計畫變成不可執行。 |
| 由結果攜帶的錯誤 | 每個規劃錯誤都是診斷訊息，而非例外：`OL_E_INSTALLATION_NOT_FOUND`、`OL_E_INSTALLATION_NOT_WRITABLE`、`OL_E_UNSUPPORTED_OPERATING_SYSTEM`、`OL_E_UNSUPPORTED_BUILD`、`OL_E_MAP_NOT_FOUND`、`OL_E_ENTRYPOINT_NOT_FOUND`、`OL_E_ENTRYPOINT_REQUIRED`、`OL_E_SITUATION_NOT_FOUND`、`OL_E_SITUATION_MAP_NOT_FOUND`、`OL_E_VEHICLE_NOT_FOUND`、`OL_E_REPAINT_NOT_FOUND`、`OL_E_HOF_NOT_FOUND`、`OL_E_DATE_TIME_APPLY_FAILED`、`OL_E_INVALID_ARGUMENT`、`OL_E_CAPABILITY_UNAVAILABLE`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_SESSION_PRESENTATION_INVALID`（訊息中帶有 splash／ITX 代碼）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`（`plugins\` 中已安裝的外掛程式封閉集合會在規劃時依發行資訊清單驗證）、`OL_E_RUNTIME_ARTIFACT_MISSING`（訊息中可能帶有 `OL_E_RELEASE_MANIFEST_INVALID`）。完整條件：[LaunchSpec 驗證規則](launchspec.md#validation-rules-and-non-runnable-diagnostics)。 |
| 擲回 | 若 token 在進入時已取消，擲回 `OperationCanceledException`（唯一的檢查點）；語法無效的根路徑擲回 `ArgumentException`／`NotSupportedException`；必要成員為 null 時擲回 `NullReferenceException`／`ArgumentNullException`；語法無效的發行資訊清單擲回 `System.Text.Json.JsonException`。 |
| 取消 | 僅在進入時檢查一次。之後的規劃為同步的檔案系統作業。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI 狀態 | 否。 |
| 變更檔案系統 | 否（會讀取 `Omsi.exe`、內容檔案、外掛程式封閉集合、資訊清單）。此處不驗證設定值（只檢查鍵是否存在及是否可寫入）；無效的值會在啟動時以 `OL_E_INVALID_SETTING_VALUE` 失敗。 |
| 交易／還原 | 無。 |
| 限制 | 在此組建上，要求 `Date`／`Time`／`Year` 中任一個採用 `Unset` 以外的模式、`Unset` 以外的任何 `Weather` 模式、任何 `PlayerVehicle` 欄位、`Input` 文件、`EntrypointIdentity` 或 `WorldMode.LastMapState`，都會產生 `OL_E_CAPABILITY_UNAVAILABLE` 以及不可執行的計畫（`UnsupportedRequestedFeatures` 中的項目附帶其目錄證據，`UNAVAILABLE` 或 `PARTIAL`）。 |
| 穩定性 | `STABLE_BETA`。 |
| 範例 | `var plan = await launch.PlanSessionAsync(spec); Console.WriteLine(plan.IsRunnable ? "READY" : string.Join(", ", plan.Diagnostics.Where(d => d.Code.StartsWith("OL_E_")).Select(d => d.Code)));` |

### `StartSessionAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 用途 | 從可執行的計畫啟動一個交易式的受管 OMSI 工作階段：取得安裝租用、復原殘留的日誌、驗證永久外掛程式封閉集合、為工作階段檔案建立快照並套用 overlay（暫時覆蓋）、建立啟動交接（handoff）、遙測槽位與執行階段信箱（mailbox）、啟動 `Omsi.exe`、將處理程序記錄於日誌中，並將工作階段交給背景監督程式。公開功能 `session.start`。 |
| 參數 | `plan`：`IsRunnable == true` 的 `SessionPlan`。計畫中的 spec 會被重新規劃；呼叫端計畫中只保留 `plan.SessionId`。`plan.Spec.Behavior.StartupTimeoutSeconds` 必須為 1..600。 |
| 回傳 | 一旦 `Omsi.exe` 已建立並記錄（狀態 `WaitingForPlugin`），或一旦啟動路徑失敗（狀態 `Failed`），即回傳 `SessionHandle(plan.SessionId)`。它不會等待遊戲開始；請使用 `WaitForAsync(session, SessionState.Running, ...)`。 |
| 擲回 | 當 `plan.IsRunnable` 為 false 時擲回 `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE")`；重新規劃結果不可執行時（例如 `Omsi.exe` 已變更、內容已移除、外掛程式封閉集合遺失）擲回 `InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: <codes>")`；相同 id 的工作階段仍在登錄中時擲回 `InvalidOperationException("Duplicate session id.")`（請先呼叫 `CloseAsync`）；`StartupTimeoutSeconds` 超出 1..600 時擲回 `ArgumentOutOfRangeException`；在重新規劃之前或期間取消時擲回 `OperationCanceledException`；另加 `PlanSessionAsync` 會擲回的所有例外。在所有擲回的情況下，都不會登錄任何工作階段。 |
| 由結果攜帶的錯誤 | 重新規劃之後的任何失敗都會在啟動路徑內部被攔截：工作階段會被登錄，其狀態為 `Failed`，且其診斷訊息包含 `OL_E_START_SESSION`，其訊息為內部訊息（若有內部代碼，則以該代碼開頭）：`OL_E_INSTALLATION_BUSY`（租用被持有，或日誌中記錄的 OMSI 處理程序仍存活）、`OL_E_PERMANENT_PLUGIN_MISSING`、`OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE`、`OL_E_PERMANENT_PLUGIN_HASH_MISMATCH`、`OL_E_RELEASE_MANIFEST_INVALID`、`OL_E_SPLASH_ASSET_MISSING`、`OL_E_SPLASH_ASSET_DIRECTORY_MISSING`、`OL_E_SPLASH_FORMAT_UNSUPPORTED`、`OL_E_ITX_PROFILE_REQUIRED`、`OL_E_ITX_PROFILE_MISSING`、`OL_E_ITX_PROFILE_INVALID`、`OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH`、`OL_E_UNKNOWN_SETTING`、`OL_E_SETTING_NOT_WRITABLE`、`OL_E_INVALID_SETTING_VALUE`、`OL_E_CLOSECHECK_REMOVE_FAILED`、`OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`（僅在以本工作階段的 overlay 延後重試後仍無法證明擁有權時）、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED`、`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`。清理時可能加上 `OL_E_PROCESS_CLEANUP_FAILED`、`OL_E_RESTORE_DEFERRED`（未確認 OMSI 已結束；保留日誌）或 `OL_E_RESTORE_FAILED`。之後的失敗由監督程式回報（請參閱[工作階段生命週期](../concepts/session-lifecycle.md)）。 |
| 取消 | 在重新規劃之前／期間：擲回例外。之後 token 會傳給交易與處理程序建立；在該處發生的取消會視同任何啟動失敗處理（`Failed` + `OL_E_START_SESSION: The operation was canceled.`），處理程序（若已建立）會被終止，安裝也會被還原。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI 狀態 | 是：以環境變數 `OMSILAUNCH_SESSION_ID`、`OMSILAUNCH_HANDOFF_NAME`、`OMSILAUNCH_TELEMETRY_NAME`、`OMSILAUNCH_RUNTIME_CHANNEL`、`OMSILAUNCH_INTERNET_TEXTURES_MODE` 建立 OMSI 處理程序。 |
| 變更檔案系統 | 是，在安裝根目錄內：`.omsilaunch\diagnostics\<sessionId>-host.log`（保留：最新的 50 個工作階段）、`.omsilaunch\journal.json`、`.omsilaunch\backup\<sessionId>\*.bin`、`.omsilaunch\assets\splash\*.bmp`（為受管啟動畫面複製一次）、工作階段 overlay（`options.cfg` 修補、`GUI\NewSplashscreen_*.bmp`、`Texture\standard.itx`）、工作階段刪除（ITX 目標、`Texture\standard.ipr`、`closecheck`），以及在 `SuppressStaleClosecheckWarning` 為 true 時永久移除事先存在的殘留 `closecheck`（診斷訊息 `closecheck.stale-removed`）。 |
| 交易／還原 | 開啟交易（`Prepared` → `Applied` → `RuntimeDeployed` → `HandoffCreated` → `ProcessStarted`）。離開工作階段的每條路徑都以還原作結。請參閱[交易與復原](../concepts/transactions-and-recovery.md)。 |
| 限制 | 只有搭配 `PresentedEntrypointIndex` 的 `WorldMode.NewMap` 與 `WorldMode.SavedSituation` 能進入遊戲。`WorldMode.LastMapState` 為 `UNAVAILABLE`。玩家車輛的要求永遠不會到達此方法，因為它們在規劃時即為不可執行。 |
| 穩定性 | `STABLE_BETA`（NEW_MAP 與 SAVED_SITUATION 生命週期已於執行階段驗證）。 |
| 範例 | `var session = await launch.StartSessionAsync(plan); var s = await launch.GetStatusAsync(session); if (s.State == SessionState.Failed) Console.WriteLine(s.Diagnostics.Last(d => d.Code.StartsWith("OL_E_")).Message);` |

### `GetStatusAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 讀取語意生命週期狀態、目前為止收集到的診斷訊息，以及有界的執行階段事件清單。公開功能 `session.status`。 |
| 參數 | `session`：由 `StartSessionAsync` 回傳且尚未關閉的 handle。 |
| 回傳 | `SessionStatus(SessionId, State, Diagnostics, RuntimeEvents)`：不可變的快照（陣列在工作階段鎖定下複製）。對存活中的工作階段，`RuntimeEvents` 永遠不會是 `null`。 |
| 擲回 | 未知／已關閉的 handle 擲回 `KeyNotFoundException`。除此之外絕不擲回。 |
| 取消 | 忽略 token（呼叫會同步完成）。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI／檔案系統／交易 | 否／否／無。 |
| 穩定性 | `STABLE_BETA`。 |
| 範例 | `var status = await launch.GetStatusAsync(session); Console.WriteLine($"{status.State} events={status.RuntimeEvents!.Count}");` |

### `WaitForAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 用途 | 輪詢（每 100 ms 一次），直到工作階段處於 `state`、處於終止狀態（`Completed`、`Failed`），或逾時為止；然後回傳目前的狀態。 |
| 參數 | `state`：任何 `SessionState`。等待一個已經經過的暫時狀態（或從未被設定的狀態，請參閱[工作階段生命週期](../concepts/session-lifecycle.md)）會一直等到終止狀態或逾時。`timeout`：任何非負的 `TimeSpan` 或 `Timeout.InfiniteTimeSpan`。 |
| 回傳 | 等待結束當下的狀態。逾時時回傳狀態而非擲回例外：請自行檢查 `State`。等待 `Running` 而結束於 `Failed` 時，會立即連同失敗診斷訊息回傳。 |
| 擲回 | `KeyNotFoundException`；呼叫端的 token 被取消時擲回 `OperationCanceledException`（只有呼叫端的取消會傳遞出來；內部逾時不會）。 |
| 取消 | 在每個 100 ms 週期檢查呼叫端的 token。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI／檔案系統／交易 | 否／否／無。 |
| 穩定性 | `STABLE_BETA`。 |
| 範例 | `var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(185)); if (running.State != SessionState.Running) { /* timed out or Failed */ }` |

### `StopAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 要求標準停止。它會設定停止旗標並立即返回；監督程式會在其 100 ms 迴圈內偵測到該旗標，對 `Omsi.exe` 呼叫 `TerminateProcess`、等待其結束、將日誌標記為 `ProcessExited`、還原每個由工作階段擁有的檔案，並釋放租用。這是強制終止：OMSI 本身的關閉程序不會執行，OMSI 也不會在結束時重寫 `options.cfg`（刻意如此，以保護交易）。未實作以 `WM_CLOSE` 進行的協同關閉（產品決策；在執行階段驗證收尾中，OMSI 未在 `WM_CLOSE` 後 30 s 內關閉，`L05b`）。公開功能 `session.stop`。 |
| 參數 | `session`。 |
| 回傳 | 已完成的 task；不會等待終止或還原。請使用 `WaitForAsync(session, SessionState.Completed, ...)` 觀察完成。 |
| 擲回 | `KeyNotFoundException`。 |
| 取消 | 忽略 token。 |
| 需要 Running 工作階段 | 否。具等冪性；在監督程式啟動前要求的停止，會在其啟動後立即生效；對已終止的工作階段要求停止則不執行任何動作。 |
| 變更 OMSI 狀態 | 是：終止 OMSI 處理程序（結束代碼 1）。 |
| 變更檔案系統 | 間接：觸發監督程式執行還原、刪除日誌及移除備份。 |
| 交易／還原 | 觸發 `ProcessExited` → `Restoring` → `Restored`。透過 `ExecuteRuntimeAsync` 在執行階段所做的變更（時鐘寫入、生成的車輛、指令碼變數、D3D 材質）不會被還原；它們會隨處理程序一併消失。 |
| 穩定性 | `STABLE_BETA`。 |
| 範例 | `await launch.StopAsync(session); var done = await launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromMinutes(1));` |

### `CloseAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 用途 | 釋放使用端的 handle，同時不讓交易懸而未決：若工作階段尚未終止，則要求標準停止；接著等待監督程式的生命週期 task（處理程序結束、還原、租用釋放）；然後遺忘該工作階段。 |
| 參數 | `session`。 |
| 回傳 | 在工作階段已終止並被移除時完成。回傳之後，該 handle 即為未知（之後的任何呼叫，包括第二次 `CloseAsync`，都會擲回 `KeyNotFoundException`）。 |
| 擲回 | `KeyNotFoundException`；若呼叫端在等待監督程式時取消，擲回 `OperationCanceledException`。此時工作階段不會被移除，監督程式會繼續執行；請再次呼叫 `CloseAsync`。 |
| 取消 | 只套用於等待；絕不會取消還原。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI 狀態 | 當工作階段仍存活時為是（與 `StopAsync` 相同）。 |
| 變更檔案系統 | 間接（由監督程式還原）。 |
| 交易／還原 | 保證在釋放 handle 之前將交易推進至完成（當監督程式已啟動時）。對於在監督程式啟動前即已失敗的工作階段，啟動路徑已完成還原或回報了 `OL_E_RESTORE_DEFERRED`。 |
| 穩定性 | `STABLE_BETA`。 |
| 範例 | `try { ... } finally { await launch.CloseAsync(session); }` |

### `ExecuteRuntimeAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 用途 | 透過單一進行中（single-flight）的工作階段信箱（記憶體對應、64 KiB、要求繫結至工作階段 id 與要求 id），在執行中的 OMSI 處理程序內執行一個公開的執行階段操作。外掛程式會在 OMSI 的 UI 執行緒上執行該操作。操作目錄：[執行階段控制](runtime-control.md)與[功能](capabilities.md)。 |
| 參數 | `command.SessionId` 必須等於 `session.SessionId`。`command.RequestId`：由呼叫端選擇的 `ulong`；請在每個處理程序中使用嚴格遞增的計數器（D3D 輔助方法從 30 000 開始，CLI 擁有者從 10 001／50 000 開始）。`command.Operation`：來自 `PublicCapabilityRegistry.PublicRuntimeOperationIds` 的公開操作 id（例如 `time.read`、`road-vehicle.read`、`d3d.texture.create`）。`command.Arguments`：以序數名稱為鍵的字串值；各操作的必要名稱來自 `PublicCapabilityRegistry.GetRuntimeArguments`。`timeout`：從要求放入信箱的那一刻開始計算（排在另一個進行中命令之後等待的時間不計入）。CLI 作為擁有者時使用 5 s（`road-vehicles.spawn` 為 15 s），作為用戶端時使用 8 s／30 s。 |
| 檢查順序 | 1. 登錄驗證（在查找工作階段之前）：未知或 `internal.*` 操作 → 結果 `Succeeded=false, ErrorCode=OL_E_RUNTIME_OPERATION_UNKNOWN`；缺少必要引數（不存在或僅含空白）→ `OL_E_RUNTIME_ARGUMENT_REQUIRED`。2. 查找工作階段 → `KeyNotFoundException`。3. `command.SessionId != session.SessionId` → `InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH")`。4. 狀態不是 `Running` → `InvalidOperationException("OL_E_SESSION_NOT_RUNNING")`。5. 信箱要求。6. 鍵以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的結果值會被移除。 |
| 回傳 | `RuntimeCommandResult(SessionId, RequestId, Succeeded, ErrorCode, Values)`。成功時，`Values` 保存該操作的語意字串（各操作的說明見[執行階段控制](runtime-control.md)）。 |
| 由結果攜帶的錯誤 | `OL_E_RUNTIME_OPERATION_UNKNOWN`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（登錄）；`OL_E_RUNTIME_RESPONSE_TOO_LARGE`（外掛程式結果超出信箱；有界清單結果則改以 `truncated=true` 縮短）；`OL_E_RUNTIME_SETTING_NOT_PERSISTENT`（帶有衍生索引鍵的 `weather.set`）；每個 `OL_E_D3D_*` 代碼（附 `Values["detail"]` 與 `Values["native_status"]`）；`OL_E_CAPABILITY_UNRESOLVED`（該操作的原生功能在執行中的組建上未解析）；以及每個外掛程式端的具型別失敗，其專屬代碼位於 `ErrorCode`，訊息位於 `Values["detail"]`（Beta 4；只有沒有已登錄代碼的失敗才是 `OL_E_RUNTIME_OPERATION_FAILED`）。以此方式傳回的代碼：`OL_E_RUNTIME_OPERATION_UNAVAILABLE`、`OL_E_RUNTIME_ARGUMENT_REQUIRED`（外掛程式端檢查）、`OL_E_RUNTIME_VALUE_OUT_OF_RANGE`、`OL_E_RUNTIME_VALUE_INVALID`、`OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED`、`OL_E_RUNTIME_OBJECT_HANDLE_STALE`、`OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE`、`OL_E_RUNTIME_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_VARIABLE_UNAVAILABLE`、`OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND`、`OL_E_RUNTIME_CONSTANT_NOT_FOUND`、`OL_E_RUNTIME_CONSTANTS_UNAVAILABLE`、`OL_E_RUNTIME_CURVE_NOT_FOUND`、`OL_E_RUNTIME_CURVE_EMPTY`、`OL_E_RUNTIME_CURVE_DEGENERATE`、`OL_E_RUNTIME_CURVE_INVALID`、`OL_E_RUNTIME_HOF_UNAVAILABLE`、`OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE`、`OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED`、`OL_E_TIME_APPLY_FAILED`、`OL_E_RUNTIME_BUS_IDENTITY_INVALID`、`OL_E_MAKEVEHICLE_BUS_NOT_FOUND`、`OL_E_MAKEVEHICLE_DELTA_ZERO`、`OL_E_MAKEVEHICLE_DELTA_MULTIPLE`、`OL_E_MAKEVEHICLE_NATIVE_FAILED`、`OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION`、`OL_E_RUNTIME_CREATED_OBJECT_INVALID`、`OL_E_PLACE_RANDOM_BUS_FAILED`、`OL_E_RUNTIME_SETTING_UNAVAILABLE`。請參閱[錯誤碼](errors.md)。 |
| 擲回 | `KeyNotFoundException`；附 `OL_E_RUNTIME_SESSION_MISMATCH`、`OL_E_SESSION_NOT_RUNNING`、`OL_E_RUNTIME_CHANNEL_CLOSED`（信箱已被監督程式處置）、`OL_E_RUNTIME_CHANNEL_BUSY`（槽位仍保有被放棄的要求）、`OL_E_RUNTIME_REQUEST_ID_REUSED`（槽位中仍有相同要求 id 的過時回應）的 `InvalidOperationException`；`TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")`；`InvalidDataException("OL_E_RUNTIME_RESPONSE_INVALID")`（回應損毀、來源不符或不相符）；序列化後的要求超出信箱時擲回 `ArgumentOutOfRangeException`；`OperationCanceledException`。 |
| 取消 | 在等待每個工作階段的閘道（gate）時，以及輪詢回應期間每 20 ms 檢查一次。在進行途中取消不會重設槽位：該工作階段的下一個呼叫可能以 `OL_E_RUNTIME_CHANNEL_BUSY` 失敗，直到外掛程式發佈其回應為止（該回應接著會被視為過時而捨棄）。建議改用逾時；逾時會重設槽位，而延遲抵達的回應會被偵測並捨棄。 |
| 需要 Running 工作階段 | 是（`SessionState.Running`）；否則會擲回 `OL_E_SESSION_NOT_RUNNING`。信箱會一直存在，直到監督程式在還原期間將其處置。 |
| 變更 OMSI 狀態 | 取決於操作：`Read` 操作不會；`Write`／`Action` 操作（`time.set`、`camera.set`、`camera.lock`、`camera.unlock`、`road-vehicles.spawn`、`road-vehicles.place-random`、`vehicle.variable.set`、`d3d.texture.*`）會變更處理程序內的狀態，且這些狀態不會被還原。 |
| 變更檔案系統 | 主機不寫入任何檔案。OMSI 可能因此寫入其自身的檔案（不追蹤）。 |
| 交易／還原 | 無。 |
| 限制 | 每個工作階段同時只有一個進行中的命令（對同一工作階段的呼叫會序列化）。要求與回應各自限制為 64 KiB 減 8 位元組；D3D 像素承載限制為 48 KiB。`internal.road-vehicles.make-basic` 為 `INTERNAL` 且無法觸及。`weather.set` 與 `weather.metar.set` 為 `EXPERIMENTAL`。`timetable.logs.read` 不是有界的，在大型時刻表上可能回傳 `OL_E_RUNTIME_RESPONSE_TOO_LARGE`。`camera.lock` 為 `EXPERIMENTAL`；它需要玩家車輛，且已於執行階段驗證（`CAM01`），儘管登錄中的 `RuntimeValidation` 字串仍顯示 `STATICALLY_VALIDATED`。handle（`rv-NNNNNN`、`hb-NNNNNN`、`d3dtex-<session>-<hex>`）的範圍限於工作階段。 |
| 穩定性 | 傳輸與合約為 `STABLE_BETA`；各操作的穩定性依循 `PublicCapabilityRegistry`（`PublicStableBeta` → `STABLE_BETA`、`PublicExperimental` → `EXPERIMENTAL`），上述例外除外。 |
| 範例 | `var r = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 42, "road-vehicle.read", new Dictionary<string, string> { ["handle"] = "rv-000001" }), TimeSpan.FromSeconds(5)); if (!r.Succeeded) Console.WriteLine($"{r.ErrorCode} {r.Values?["detail"]}");` |

### `GetCapabilitiesAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 回傳針對某個安裝評估的功能目錄：每個非 `InternalOnly` 的 `PublicCapabilityRegistry` 項目對應一個 `Capability(Name, Available, EvidenceState, Reason)`。`Name` 為目錄 id，`EvidenceState` 為目錄證據（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`）。`Available` 依平台、OMSI 2 檢查、分類以及該項目所需的原生功能計算得出（請參閱[功能](capabilities.md#getcapabilitiesasync)）。 |
| 參數 | `installation.RootPath`：被探測的安裝（平台、`Omsi.exe`）。`ExpectedExecutableSha256` 會被忽略。 |
| 回傳 | 每個非內部目錄項目一個項目，例如 `time.read`（`RUNTIME_VALIDATED`，在已知配置上 `Reason` 為 `native: RESOLVED_KNOWN_LAYOUT`）、`world.last-map-state`（`false`、`UNAVAILABLE`）。 |
| 與 `PublicCapabilityRegistry` 的關係 | 由其衍生：登錄宣告功能、其操作與其證據；此呼叫則針對單一安裝評估它們。 |
| 擲回 | 進入時擲回 `OperationCanceledException`；根路徑為空時擲回 `ArgumentException`。 |
| 取消 | 僅在進入時檢查一次。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI／檔案系統／交易 | 否／否／無。 |
| 穩定性 | 呼叫合約為 `STABLE_BETA`；清單內容是人工維護的清單：`PARTIAL`。 |
| 範例 | `foreach (var c in await launch.GetCapabilitiesAsync(new InstallationSpec(root))) Console.WriteLine($"{c.Name} {c.Available} {c.EvidenceState} {c.Reason}");` |

### `DiscoverAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 用途 | 列舉已安裝的內容，並回傳可用於 `LaunchSpec` 的標準識別。探索會略過重新剖析點（junction 迴圈不會使其停滯）、以 Windows-1252 讀取 OMSI 檔案（遵循帶 BOM 標記的 UTF-8／UTF-16），且絕不跟隨符號連結。 |
| 參數 | `query` 與 `scope` 依下表。`Entrypoints`（地圖識別）、`Repaints`、`FleetNumbers`、`Registrations`（車輛識別）需要 `scope`。 |
| 回傳 | 已排序的 `ContentIdentity(Identity, Kind, DisplayName)` 清單。識別為以反斜線表示、相對於安裝的路徑；比對時不區分大小寫。 |
| 擲回 | 進入時擲回 `OperationCanceledException`；查詢 `Entrypoints` 未提供範圍或根目錄為空時擲回 `ArgumentException`；範圍所指的地圖或車輛未安裝時擲回 `FileNotFoundException`（無 `OL_E_` 代碼；CLI 會將其對應為 `OL_E_NOT_FOUND`）。根目錄或內容目錄不存在時會得到空清單，而非錯誤。 |
| 取消 | 僅在進入時檢查一次。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI／檔案系統／交易 | 否／否／無。 |
| 穩定性 | `Maps`、`Situations`、`Vehicles`：`STABLE_BETA`（每個已於執行階段驗證的計畫都透過它們解析）。`Entrypoints`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`：`EXPERIMENTAL`（僅有靜態證據）。 |
| 範例 | `var maps = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Maps); var entries = await launch.DiscoverAsync(new InstallationSpec(root), ContentQueryKind.Entrypoints, OptionalValue<string>.Set(maps[0].Identity));` |

`ContentQueryKind` 的值與結果：

| 值 | 範圍 | `Identity` | `Kind` | `DisplayName` |
| --- | --- | --- | --- | --- |
| `Maps` | 無 | `maps\<dir>\global.cfg` | `map` | 地圖目錄名稱 |
| `Situations` | 無 | `situations\...\<file>.osn` | `situation` | `.osn` 所參照的地圖識別（可能為 `null`） |
| `Vehicles` | 無 | `Vehicles\...\<file>.bus` | `vehicle` | `[friendlyname]` 或檔案名稱 |
| `Repaints` | 車輛識別（必要；缺少時：空清單） | `<cti path>#item:<ordinal>` | `repaint` | `[item]` 名稱 |
| `Hofs` | 無 | `Vehicles\...\<file>.hof` | `hof` | `null` |
| `FleetNumbers` | 車輛識別（必要；缺少時：空清單） | 相對於車輛的 `[number]` 來源路徑 | `fleet-number` | `null` |
| `Registrations` | 車輛識別（必要；缺少時：空清單） | `registration_automatic` / `registration_list` / `registration_free` | `registration` | 第一個值所在的行（free 時為 `null`） |
| `Addons` | 無 | `Addons\<dir>` | `addon` | `directory-only` |
| `Entrypoints` | 地圖識別（必要；缺少時：`ArgumentException`） | `<map identity>#entrypoint:<SHA-256 of the 12-line record>` | `entrypoint` | 進入點標籤 |
| `SessionProfiles` | 無 | 套件目錄名稱（即 `/predefined-profile` 的 id） | `session-profile`，套件無法編譯時為 `session-profile-invalid` | 設定檔的 `name`，或無效套件的 `OL_E_SESSION_PROFILE_*` 代碼 |
| `RuntimeModules` | 無 | 模組 id | `runtime-module`，無法載入時為 `runtime-module-invalid` | 模組的 `name`，或 `OL_E_MODULE_*` 代碼 |

進入點可由 `PresentedEntrypointIndex` 或 `EntrypointIdentity`（探索識別或精確標籤）選擇。規劃器會將識別解析為 OMSI 所呈現的標籤（OMSI 只呈現每個精確標籤的第一筆記錄），計畫會將其以種類 `entrypoint` 列在 `ResolvedContent` 之下；在完成執行階段驗證之前，`world.entrypoint-identity` 為 `EXPERIMENTAL`。

### `ListSessionProfilesAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 列出 `<root>\.omsilaunch\session-profiles\` 之下的每個工作階段設定檔套件及其預設組合，讓前端不必解析 YAML 即可提供它們。CLI `/list:SessionProfiles`。 |
| 回傳 | 每個套件目錄一個 `SessionProfileSummary(Id, Name, Version, Author, CompatibleMaps, MapIdentity, Presets, IsValid, ErrorCode, ErrorMessage)`，依 `Id` 排序。`Presets` 為依索引順序排列的 `SessionProfilePresetSummary(Index, Id, Name)`。每個套件都以與啟動相同的嚴格規則編譯（`SessionProfileCompiler`）；無法編譯的套件會以 `IsValid = false`、其 `OL_E_SESSION_PROFILE_*` 代碼與訊息列出，且沒有預設組合。沒有設定檔目錄：空清單。 |
| 擲回 | 進入時擲回 `OperationCanceledException`。編譯錯誤會在結果中回報，不會擲回。 |
| 需要 Running 工作階段 | 否。 |
| 變更 OMSI／檔案系統／交易 | 否／否／無。 |
| 穩定性 | `STABLE_BETA`（離線關卡 `session-profiles.discovery`）。 |
| 範例 | `foreach (var p in await launch.ListSessionProfilesAsync(new InstallationSpec(root))) Console.WriteLine($"{p.Id}: {string.Join(", ", p.Presets.Select(x => x.Index + "=" + x.Name))}");` |

### `ListRuntimeModulesAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 用途 | 探索安裝在 `plugins\OmsiLaunch.Modules` 之下的[執行階段模組](../../../reference/runtime-modules.md)及其完整性與設定。唯讀。載入是開放的：不會依清單或簽章檢查任何模組。 |
| 回傳 | 每個模組目錄一個 `RuntimeModuleSummary(Id, Version, Name, Activation, IsValid, Operations, ContentBindingMap, ErrorCode, ErrorMessage, Settings)`，依目錄名稱排序。`Activation` 為 `requested`、`content` 或 `default`；模組無法載入時 `IsValid` 為 false 並帶有 `OL_E_MODULE_*` 代碼；`Settings` 列出模組所宣告的設定，為 `RuntimeModuleSettingSummary(Key, Type, Default, AllowsAuto, Minimum, Maximum, Choices, Flag, Description)`。 |
| 擲回 | 進入時擲回 `OperationCanceledException`。 |
| 需要 Running 工作階段 | 否。CLI：`/list:RuntimeModules`（`runtime-modules.list` 封套）。 |

### `RecoverPendingAsync`

| 面向 | 詳細說明 |
| --- | --- |
| 簽章 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 用途 | 回報或完成由當機的擁有者遺留下來的殘留持久交易（`<root>\.omsilaunch\journal.json`）。在呼叫期間取得安裝租用，因此絕不會在正在啟動的工作階段底下進行還原。公開功能 `session.recover`；CLI `/recovery-status` 與 `/recover`。 |
| 參數 | `installation.RootPath`：安裝根目錄（以 `Path.GetFullPath` 正規化）。`restore`：`false` = 僅回報；`true` = 還原、驗證、刪除日誌與備份。 |
| 回傳 | `RecoveryStatus(Pending, Recovered, Diagnostics)`：`Pending` = 呼叫開始時存在日誌；`Recovered` = 已要求還原、已執行，且不再有日誌；`Diagnostics` = 還原附註（附 `Data["sha256"]` 的 `restore.session-artifact-removed`、`OL_W_RESTORE_FOREIGN_FILE_RETAINED`），未還原任何內容時為空。 |
| 擲回 | 租用被持有時擲回 `InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation.")`；當日誌的 PID + 建立時間 + 執行檔路徑仍與某個存活的處理程序相符，或（日誌已超過 `HandoffCreated` 但沒有 PID 時）來自該根目錄的任何 `Omsi.exe` 正在執行時，擲回 `IOException("OL_E_INSTALLATION_BUSY: a journaled OMSI process is still alive.")`；附 `OL_E_RECOVERY_BACKUP_CORRUPT`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH`、`OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED`、`OL_E_RECOVERY_JOURNAL_REMOVE_FAILED` 或驗證訊息（"Restore hash mismatch: ..."、"Restore presence mismatch: ..."）的 `IOException`；日誌損毀時擲回 `InvalidDataException("Invalid OmsiLaunch journal.")`／`JsonException`；根目錄為空時擲回 `ArgumentException`；`OperationCanceledException`。只要在還原開始後擲回例外，日誌都會被保留，下一次呼叫會以等冪方式重播。 |
| 取消 | 會傳給日誌／備份的寫入；在還原途中取消會使日誌維持待處理狀態。 |
| 需要 Running 工作階段 | 否（有擁有者在作用中時會拒絕）。 |
| 變更 OMSI 狀態 | 否。 |
| 變更檔案系統 | 僅在 `restore == true` 時：從已驗證的備份重寫原始檔案（位元組、上次寫入時間、建立時間、屬性；可處理唯讀的原始檔案；write-through + flush；不留下 `*.omsilaunch.tmp`）、移除工作階段產物、刪除 `journal.json` 與 `backup\<sessionId>`。 |
| 交易／還原 | 完成待處理的交易（`Restoring` → `Restored` → 移除日誌）。 |
| 穩定性 | `STABLE_BETA`：回報路徑與提前結束後的復原（矩陣 RV-008）、還原失敗後的復原（執行階段驗證收尾 `F01`）、在存活擁有者之下及 PID 記錄前時間窗內的拒絕（`S04`），以及啟動時延後進行的指紋前復原（`S05`）；請參閱[執行階段驗證狀態](../status/runtime-validation-status.md)。 |
| 範例 | `var r = await launch.RecoverPendingAsync(new InstallationSpec(root), restore: true); Console.WriteLine($"pending={r.Pending} recovered={r.Recovered}");` |

<a id="contract-types"></a>
## 合約型別

<a id="optional-values-and-semantic-primitives"></a>
### 選用值與語意基本型別

| 型別 | 定義 | 附註 |
| --- | --- | --- |
| `OptionalValue<T>` | `readonly record struct OptionalValue<T>(Presence Presence, T? Value)`；`IsSet`、靜態 `Unset`、靜態 `Set(T)` | 區分「未要求」與「已要求並附帶值」。JSON 形式記載於 [LaunchSpec 參考](launchspec.md)。 |
| `Presence` | `Unset` = 0、`Set` = 1 | Byte 列舉。 |
| `SemanticDate` | `(int Year, int Month, int Day)` | 僅在 `DateTimeMode.Explicit` 時驗證（月 1..12、日 1..31）。 |
| `SemanticTime` | `(int Hour, int Minute, int Second)` | 僅在 `DateTimeMode.Explicit` 時驗證（0..23、0..59、0..59）。 |

<a id="launchspec-family"></a>
### LaunchSpec 型別家族

以下所有 record 都在 [LaunchSpec 參考](launchspec.md)中逐一說明其屬性；本表確立型別清單。

| 型別 | 用途 | 穩定性 |
| --- | --- | --- |
| `LaunchSpec` | 根要求 record，具有 `EffectiveYear`、`EffectiveWeather`、`EffectiveInput`、`EffectiveDiagnostics`、`EffectivePresentation`、`EffectiveInternetTextures` 存取子，會以預設值取代為 `null` 的選用成員。 | `STABLE_BETA` |
| `InstallationSpec` | `RootPath`、`ExpectedExecutableSha256`（`Omsi.exe` 的選用固定值，於規劃時檢查）。 | `STABLE_BETA` |
| `WorldSpec`、`WorldMode`、`EntrypointSpec`、`EntrypointMode` | 世界選擇。`WorldMode`：`NewMap` = 0、`SavedSituation` = 1、`LastMapState` = 2、`LastSituation` = 2（`LastMapState` 的已淘汰別名；絕不表示「最新的 .osn」）。`EntrypointMode`：`Unset`、`PresentedIndex`、`Identity`（由 `WorldSpec.Entrypoint` 計算）。 | `NewMap`、`SavedSituation`：`STABLE_BETA`；`LastMapState`：`UNAVAILABLE`；`EntrypointMode.Identity`：`PARTIAL` |
| `DateSpec`、`TimeSpec`、`YearSpec`、`DateTimeMode` | `DateTimeMode`：`Unset`、`Explicit`、`System`。`Unset` 以外的任何模式都會使計畫變成不可執行。 | `PARTIAL`（計畫證據 `UNAVAILABLE`） |
| `WeatherSpec`、`WeatherMode` | `WeatherMode`：`Unset`、`Preset`、`Icao`、`RealCurrent`。`Unset` 以外的任何模式都會使計畫變成不可執行。 | `PARTIAL` |
| `PlayerVehicleSpec` | `Model`、`Repaint`、`Hof`、`FleetNumber`、`Registration`、`Enabled`。設定任何欄位都會使計畫變成不可執行。 | `PARTIAL` |
| `EnvironmentSpec` | 八組 `IReadOnlyDictionary<string, OptionalValue<string>>`，內容為語意化的 `options.cfg` 設定。 | `STABLE_BETA` |
| `InputSpec` | `KeyboardDocument`、`ControllerDocument`；設定任何值都會使計畫變成不可執行。 | `PARTIAL` |
| `DiagnosticsSpec` | 六個布林值；會攜帶，但不會使用。 | `PARTIAL` |
| `SessionPresentationSpec`、`SplashMode` | `SplashMode`：`Unset` = 0、`Native` = 0（別名）、`Managed` = 1。 | `STABLE_BETA` |
| `InternetTexturesSpec`、`InternetTexturesMode` | `InternetTexturesMode`：`Native`、`Disabled`、`Override`。 | `STABLE_BETA`（`Native`）、`EXPERIMENTAL`（`Disabled`、`Override`） |
| `SessionProfileMetadata` | 已編譯工作階段設定檔的來源資訊（`Id`、`Name`、`Version`、`Author`、`PresetId`、`PresetIndex`、`PresetName`、`PackagePath`）。 | `STABLE_BETA` |
| `HofManagementSpec` | 工作階段 [HOF 管理](hof-management.md)：`Operations`（`HofOperationSpec` 清單）。欄位 `LaunchSpec.HofManagement`；存取子 `EffectiveHofManagement`。 | `STABLE_BETA` |
| `RuntimeModulesSpec` | 工作階段的[執行階段模組](../../../reference/runtime-modules.md)：`Enable`（模組 id）、`Disable`（被排除的模組 id）、`Settings`（`"<module id>.<key>"` 對應值）；存取子 `EffectiveDisable`、`EffectiveSettings`。欄位 `LaunchSpec.Modules`；存取子 `EffectiveModules`。 | `EXPERIMENTAL` |
| `PlannedModule` | 模組的計畫評估：`Id`、`Version`、`Status`（`ELIGIBLE` 或 `REJECTED`）、`Requested`、`FailurePolicy`、`Operations`（`module.<id>.<operation>`）、`Code`、`Reason`、`ManifestSha256`、`Activation`（`requested`、`settings-file`、`content` 或 `default`）、`Settings`；`IsEligible`、`EffectiveSettings`。常數 `Eligible`、`Rejected`、`ActivationRequested`、`ActivationSettingsFile`、`ActivationContent`、`ActivationDefault`。 | `EXPERIMENTAL` |
| `PlannedModuleSetting` | 一個有效的模組設定：`Key`、`Value`、`Source`（`request`、`settings-file` 或 `default`）。 | `EXPERIMENTAL` |
| `RuntimeModuleSummary` | 已安裝模組的探索檢視；請參閱 `ListRuntimeModulesAsync`。 | `EXPERIMENTAL` |
| `RuntimeModuleSettingSummary` | 模組所宣告的設定：`Key`、`Type`、`Default`、`AllowsAuto`、`Minimum`、`Maximum`、`Choices`、`Flag`、`Description`。 | `EXPERIMENTAL` |
| `HofOperationSpec` | 一項 HOF 操作（`SourceKind`、`Source`、`TargetVehicles`、`Mode`、`Files`）。 | `STABLE_BETA` |
| `HofSourceKind` | `Collection`、`Directory`、`Vehicle`、`File`。 | `STABLE_BETA` |
| `HofApplyMode` | `Add`、`Mirror`。 | `STABLE_BETA` |
| `HofPlanningException` | 由 HOF 規劃擲回（`OmsiLaunch.Core`）；`Code` 為 `OL_E_HOF_*` 代碼。公開 API 會將其回報為計畫診斷訊息。 | `STABLE_BETA` |
| `SessionProfileSummary` | 單一套件的探索檢視（`Id`、`Name`、`Version`、`Author`、`CompatibleMaps`、`MapIdentity`、`Presets`、`IsValid`、`ErrorCode`、`ErrorMessage`）。請參閱 `ListSessionProfilesAsync`。 | `STABLE_BETA` |
| `SessionProfilePresetSummary` | 摘要中的一個預設組合（`Index`、`Id`、`Name`）。 | `STABLE_BETA` |
| `LaunchBehaviorSpec` | `RestoreConfiguration`（會攜帶，還原一律會發生）、`SuppressStaleClosecheckWarning`、`StartupTimeoutSeconds`（1..600，預設 180）、`ShutdownTimeoutSeconds`（會攜帶，但不會使用）。 | `STABLE_BETA` / `PARTIAL` |

<a id="plan-and-status-types"></a>
### 計畫與狀態型別

| 型別 | 欄位 | 附註 |
| --- | --- | --- |
| `SessionPlan` | `SessionId`（每個計畫一個新的 `Guid`）、`BuildProfileId`（`"Omsi23004"` 或 `"unrecognized"`）、`Spec`、`Platform`（`RuntimePlatformInfo`）、`ResolvedContent`（`ContentIdentity` 清單：`map`、`vehicle`、`repaint`、`hof`、`situation`、`situation-map`）、`TouchedFiles`（`PlannedMutations` 中不重複的相對路徑）、`RuntimeArtifacts`、`RequiredCapabilities`（`STATICALLY_VALIDATED` 或 `UNAVAILABLE` 的 `Capability`；原生啟動功能為 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`）、`UnsupportedRequestedFeatures`（針對已要求但不支援之功能的 `Capability` 項目）、`PlannedMutations`、`Diagnostics`、`IsRunnable`。 | 這是公開的 record：它可能被修改或過時，這正是 `StartSessionAsync` 會重新規劃的原因。 |
| `RuntimePlatformInfo` | `OsFamily`、`OsVersion`、`OsArchitecture`、`HostArchitecture`、`OmsiArchitecture`（`X86`）、`PluginArchitecture`（`X86`）、`CurrentPlatformSupported`（Windows 10 以上、x64 作業系統與 x64 主機）、`LegacyPlatform`（一律為 `false`）、`Wow64Available`、`InstallationWritable`、`ProcessLaunchSupported`、`PluginRuntimeSupported`、`NativeInteropSupported`、`SharedMemorySupported`、`ExactRestoreSupported`（全部等於 `CurrentPlatformSupported`）。 | |
| `Capability` | `Name`、`Available`、`EvidenceState`、`Reason`。 | `EvidenceState` 為 `PublicEvidence` 值（`RUNTIME_VALIDATED`、`STATICALLY_VALIDATED`、`PARTIAL`、`RUNTIME_VALIDATION_REQUIRED`、`UNAVAILABLE`），但 `SessionPlan.RequiredCapabilities` 中除外：平台或內容需求為 `STATICALLY_VALIDATED` 或 `UNAVAILABLE`，原生功能為 `RESOLVED_KNOWN_LAYOUT`、`STATICALLY_RESOLVED` 或 `PENDING_IN_PROCESS_RESOLUTION`。 |
| `PlannedMutation` | `RelativePath`、`SemanticKey`、`RequestedValue`、`Operation`（`token-patch`、`vector-component-patch`、`exact-file-overlay`）。 | 呈現相關的變更使用鍵 `session-presentation.splash`、`internet-textures.override`、`internet-textures.cache`、`internet-textures.target`。 |
| `LaunchDiagnostic` | `Code`、`Message`、`Data`（選用的字串對應）。 | 以 `OL_E_` 開頭的代碼為錯誤，`OL_W_` 為警告，其餘為資訊。 |
| `SessionStatus` | `SessionId`、`State`（`SessionState`）、`Diagnostics`、`RuntimeEvents`。 | 工作階段診斷訊息不包含計畫診斷訊息。 |
| `RuntimeEvent` | `Type`、`TimestampUtc`（主機接收時間）、`Sequence`（從 1 起算，每個工作階段各自計數）、`Data`、`SessionTickMilliseconds`（外掛程式發出事件時其單調遞增的工作階段時鐘；主機事件為 null）。 | 上限為最近的 256 個事件（捨棄最舊的）。外掛程式事件會依序經由遙測環狀緩衝區抵達；因環狀緩衝區溢位而遺失的事件由 `telemetry.lost` 計數。 |
| `SessionHandle` | `SessionId`。 | 僅限處理程序本機。 |
| `RecoveryStatus` | `Pending`、`Recovered`、`Diagnostics`。 | 請參閱 `RecoverPendingAsync`。 |
| `ContentIdentity` | `Identity`、`Kind`、`DisplayName`。 | 請參閱 `DiscoverAsync`。 |
| `ContentQueryKind` | `Maps`、`Situations`、`Vehicles`、`Repaints`、`Hofs`、`FleetNumbers`、`Registrations`、`Addons`、`Entrypoints`、`SessionProfiles`、`RuntimeModules`。 | |

### `SessionState`

依宣告順序排列的 Byte 列舉：`Created`、`ValidatingPlatform`、`Planning`、`AcquiringInstallationLock`、`RecoveringPreviousTransaction`、`Snapshotting`、`ApplyingConfiguration`、`DeployingRuntime`、`CreatingStartupHandoff`、`StartingProcess`、`WaitingForPlugin`、`PluginBootstrap`、`StartingWorld`、`EnteringGameplay`、`Running`、`ProcessExited`、`Restoring`、`CleaningRuntime`、`Completed`、`Failed`。目前的服務從不設定 `ValidatingPlatform`、`Planning` 與 `EnteringGameplay`；`Snapshotting` 是暫時狀態，實務上無法觀察到。終止狀態：`Completed`、`Failed`。完整語意：[工作階段生命週期](../concepts/session-lifecycle.md)。

<a id="runtime-control-types"></a>
### 執行階段控制型別

| 型別 | 定義 | 穩定性 |
| --- | --- | --- |
| `RuntimeCommand` | `(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string>? Arguments)` | `STABLE_BETA` |
| `RuntimeCommandResult` | `(Guid SessionId, ulong RequestId, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values)` | `STABLE_BETA` |
| `RuntimeCommandWire` | 主機與外掛程式用於信箱封套的靜態編解碼器：magic `0x4F4C5243`（"OLRC"）、版本 1、72 位元組 little-endian 標頭（magic、版本、種類 1 = 要求／2 = 回應、總長度、工作階段 `Guid`、要求 id、承載長度、承載的 SHA-256），後接 UTF-8 JSON 承載。`SerializeRequest`、`SerializeResponse`、`TryDeserializeRequest`、`TryDeserializeResponse`、`TryReadRequestId`。 | `INTERNAL`：因橋接兩端共用而為公開；不屬於整合介面；格式可能隨協定版本變更。 |
| `StartupHandoff` | `(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity, string SituationIdentity, IReadOnlyList<StartupModule>? Modules, bool PluginTrace, bool NativeTrace, StartDateTime? Start)`——主機在記憶體對應檔案 `OmsiLaunch.Handoff.<sessionId>` 中發佈給外掛程式的內容。 | `INTERNAL` |
| `StartupModule` | `(string RelativeDirectory, string ManifestSha256)`：由 handoff 攜帶的已規劃模組（`StartupHandoff.Modules`）。 | `INTERNAL` |
| `StartDateTime` | `(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)`、`IsEmpty`：由 handoff 攜帶的 NEW_MAP 工作階段已解析的起始時鐘（`StartupHandoff.Start`）；欄位為 null 時保留 OMSI 自己的值。 | `INTERNAL` |
| `StartupHandoffWire` | 編解碼器：magic `0x4F4C5348`、版本 7（可讀取 3 至 7；版本 5 附加已規劃的模組，版本 6 附加每個模組的有效設定，版本 7 附加起始日期／時間；旗標位元 0x04 與 0x08 攜帶 `PluginTrace` 與 `NativeTrace`）、具 SHA-256 承載完整性的 64 位元組標頭。 | `INTERNAL` |
| `TelemetryRingWire` | 外掛程式到主機的事件環狀緩衝區，位於對應 `OMSILAUNCH_TELEMETRY_NAME`：magic `0x52544C4F`（"OLTR"）、版本 1、64 個編號槽位，每個 2048 位元組。`Initialize`、`Write`、`IsRing`、`ReadSince`（回傳某個序號之後的事件以及遺失了多少）。 | `INTERNAL` |

除非 `WorldMode` 為 `NewMap` 或 `SavedSituation`、`HeadlessStart` 為 true、`PlayerVehicleEnabled` 為 false、日期／時間兩種模式皆為 `Unset`，且已儲存情境具有非空的識別，否則外掛程式會拒絕交接（`plugin.request.unsupported` → `OL_E_CAPABILITY_UNAVAILABLE`）。規劃器會更早強制執行相同的限制，因此可執行的計畫永遠不會觸發此情況。

<a id="capability-registry-types"></a>
### 功能登錄型別

| 型別 | 用途 |
| --- | --- |
| `PublicCapabilityRegistry` | 標準功能目錄。`ProtocolVersion`（`"0.1"`）、`All`（每個 `PublicCapabilityDescriptor`）、`Get`、`TryGet`、`PublicRuntimeOperationIds`（前端可轉送的 48 個具體操作 id）、`RuntimeOperations`、`TryGetOperation`、`NativeCapabilityOf`、`CliRoutes`（命令路由對應操作 id）、`IsPublicRuntimeOperation`、`GetRuntimeArguments`、`ValidateRuntimeArguments`（回傳 `PublicRuntimeArgumentValidation`）、`IsInternalResultKey`。由 `ExecuteRuntimeAsync`、CLI、本機控制平面與外掛程式強制執行。 |
| `PublicCapabilityDescriptor` | `Id`、`Family`、`Classification`、`Kind`、`RequiresSession`、`RequiresExactProfile`、`ApiRoute`、`CliRoute`、`RuntimeValidation`（`PublicEvidence` 值）、`Description`、`HandleTypes`、`Operations`、`NativeCapability`、`EvidenceNote`；衍生的 `EffectiveOperations`、`NativeCapabilities`。 |
| `PublicRuntimeOperation` | `Id`、`NativeCapability`、`Arguments`、`CliRoutes`、`Paged`（接受 `cursor`/`limit`，請參閱[分頁](capabilities.md#pagination)）、`MutationPolicy`。 |
| `PublicMutationPolicy` | 執行階段操作所變更的內容：`None`、`OmsiState`、`SessionResource`、`SessionPolicy`、`All`。 |
| `PublicEvidence` | 證據詞彙：`RuntimeValidated`、`StaticallyValidated`、`Partial`、`RuntimeValidationRequired`、`Unavailable`、`All`。 |
| `PublicCapabilityClassification` | `PublicStableBeta`、`PublicExperimental`、`InternalOnly`、`Unsupported`。 |
| `PublicCapabilityKind` | `Read`、`Write`、`Action`、`Event`。 |
| `PublicRuntimeArgumentDescriptor` | `Name`、`Required`、`Description`。 |
| `PublicRuntimeArgumentValidation` | `Accepted`、`ErrorCode`、`Message`。 |

完整目錄：[功能](capabilities.md)。

<a id="d3druntimeapi-extension-methods"></a>
### `D3DRuntimeApi` 擴充方法

針對 `d3d.*` 操作、建構於 `ExecuteRuntimeAsync` 之上的型別化包裝（`EXPERIMENTAL`，`PublicExperimental` 功能 `d3d.texture`）。它們從一個處理程序範圍、自 30 000 起算的計數器配置要求 id，預設逾時為 5 s（`GetD3DStatusAsync` 除外，它必須指定逾時）。

| 方法 | 操作 | 引數與限制 |
| --- | --- | --- |
| `GetD3DStatusAsync(IOmsiLaunch, SessionHandle, TimeSpan timeout, CancellationToken)` → `D3DDeviceStatus` | `d3d.status` | 無 |
| `CreateD3DTextureAsync(..., uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout, ...)` → `D3DTextureDescription` | `d3d.texture.create` | width／height 1..4096、levels 0..16 |
| `DescribeD3DTextureAsync(..., D3DTextureHandle handle, uint level = 0, ...)` | `d3d.texture.describe` | level 0..15 |
| `UpdateD3DTextureAsync(..., D3DTextureHandle handle, D3DTextureUpdate update, ...)` | `d3d.texture.update` | `D3DTextureUpdate(Level, X, Y, Width, Height, Pixels)`：x／y 0..4095、width／height 1..4096、pixels ≤ 48 KiB（傳輸時以 Base64 編碼） |
| `ReleaseD3DTextureAsync(..., D3DTextureHandle handle, ...)` | `d3d.texture.release` | 重複釋放會以 `OL_E_D3D_RESOURCE_RELEASED` 被拒絕 |

型別：`D3DDeviceStatus(Available, State, Generation, LiveTextureCount, ResetHookInstalled, ExecutionThreadId, LastResetThreadId, QueryInterfaceHResult, CooperativeLevelHResult, OwnedDeviceReferences)`；`D3DDeviceState`：`NotReady`、`Ready`、`Lost`、`Resetting`、`Stopping`、`Stopped`；`D3DTextureHandle(Value)`，其中 `Value = "d3dtex-<session id N>-<16 hex>"`；`D3DTextureDescription(Handle, State, DeviceState, Generation, Width, Height, Format, Levels, Level, LevelWidth, LevelHeight, HResult, ExecutionThreadId)`；`D3DTextureResourceState`：`Live`、`Released`、`Stale`；`D3DTextureFormat`：`A8R8G8B8`、`X8R8G8B8`、`R5G6B5`、`X1R5G5B5`、`A1R5G5B5`、`A4R4G4B4`、`A8`、`L8`、`A8L8`。

錯誤：失敗的結果會以 `OmsiRuntimeException(Code, detail)` 重新擲回，其中 `Code` 為結果的 `ErrorCode`（若不存在則為 `OL_E_RUNTIME_OPERATION_FAILED`），訊息為 `"<code>: <Values["detail"]>"`；成功但沒有值的結果，或未知的裝置狀態字串，會擲回 `OmsiRuntimeException("OL_E_RUNTIME_PROTOCOL_MISMATCH", ...)`。`ExecuteRuntimeAsync` 擲回的所有例外都會原封不動地傳遞出來。裝置重設的處理已於執行階段驗證：重設會使裝置經由 `Resetting` 回到 `Ready`，並使每個存活的材質失效（`OL_E_D3D_STALE_RESOURCE_HANDLE`，執行階段驗證收尾 `D01`）；目錄項目 `d3d.texture` 記錄了此事。`Lost` 轉換無法從產品外部產生，僅以離線方式涵蓋。

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
### 處理程序合約型別

| 型別 | 內容 |
| --- | --- |
| `PublicExitCode` | `Success` = 0、`SessionFailed` = 1、`InvalidArguments` = 2、`UnsupportedProfile` = 3、`NoActiveSession` = 4、`RuntimeUnavailable` = 5、`NotFound` = 6、`OperationRejected` = 7、`TransactionRecoveryFailed` = 8、`InternalError` = 10。僅由 CLI 使用（[結束代碼](exit-codes.md)）；API 從不結束處理程序。 |
| `PublicErrorCategory` | 用於 CLI／控制錯誤封套的字串常數：`invalid_argument`、`unsupported_profile`、`session`、`runtime`、`not_found`、`transaction`、`internal`。 |
| `PublicErrorCodes` | 每個代碼一個 `const string`（143 個：142 個 `OL_E_` 錯誤與 1 個 `OL_W_` 警告），以及 `All`，即 `PublicErrorDescriptor(Code, Category)` 目錄。類別：`Cli`、`Compatibility`、`Content`、`Installation`、`InvalidArgument`、`LaunchSpec`、`LocalControl`、`Other`、`Presentation`、`Process`、`Runtime`、`RuntimeD3D`、`Session`、`SessionProfile`、`Transaction`、`Warning`。參考：[錯誤碼](errors.md)。 |
| `PublicErrorDescriptor` | `(string Code, string Category)`。 |
| `OmsiRuntimeException` | `Code` 屬性加上訊息；僅由 `D3DRuntimeApi` 擲回。 |

<a id="installationpaths-installation-identity-and-path-containment"></a>
### `InstallationPaths`（安裝識別與路徑限制）

穩定性：`STABLE_BETA`（純函式，無 I/O、不涉及 OMSI 狀態、不變更檔案系統、不參與交易、不需要 Running 工作階段）。它是安裝租用、本機控制管道名稱、工作階段設定檔資產限制、Internet Textures（網路材質）目標驗證以及執行階段生成模型路徑所共用的唯一定義。

| 成員 | 行為 |
| --- | --- |
| `string NormalizeRoot(string root)` | 不含結尾分隔字元的 `Path.GetFullPath(root)`，但磁碟機根目錄（`C:\`）會保留。會解析 `.` 與 `..` 區段、將 `/` 與 `\` 視為相同，並合併重複的分隔字元。**不會**解析 junction 或符號連結。root 為 null 或空白時擲回 `ArgumentException`。 |
| `string IdentityKey(string root)` | 轉為大寫的 `NormalizeRoot(root)`。同一根目錄在字面上等價的各種寫法（`C:\OMSI`、`C:\OMSI\`、`C:\OMSI\.`、`C:\foo\..\OMSI`、`c:\omsi`）共用同一個鍵；不同的根目錄（`C:\OMSI-A`、`C:\OMSI-B`）則絕不會共用。 |
| `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` | 解析 `candidate`（相對於 `root`，或為絕對路徑），只有在它嚴格位於 `root` 之下時才回傳 `true`；`relativePath` 是以 `\` 表示的標準寫法。使用 `Path.GetRelativePath` 區段，因此像 `C:\OMSI-A\x` 這樣的同層目錄永遠不會被視為在 `C:\OMSI` 之內；根目錄本身、其他磁碟區以及 `..` 跳脫都會回傳 `false`。 |
| `IReadOnlyList<string> Segments(string relativePath)` | 以 `/` 與 `\` 分割，並捨棄空的區段。 |

```csharp
var same = InstallationPaths.IdentityKey(@"C:\OMSI") == InstallationPaths.IdentityKey(@"c:\foo\..\OMSI\"); // true
InstallationPaths.TryGetContainedRelativePath(@"C:\OMSI", @"Sceneryobjects\\x\texture\a.tga", out var relative); // true, "Sceneryobjects\x\texture\a.tga"
```

<a id="session-profiles-omsilaunchcore"></a>
### 工作階段設定檔（`OmsiLaunch.Core`）

穩定性：`EXPERIMENTAL`。這些型別會將 YAML [工作階段設定檔](session-profiles.md)（`<root>\.omsilaunch\session-profiles\<id>\profile.yaml`，結構描述 `omsilaunch.session-profile/v1`）編譯為 `LaunchSpec`。CLI 的 `/predefined-profile:<id> /predefined-profile-index:<n>` 正是使用這些呼叫；整合者可以用它們透過 API 啟動設定檔。

| 成員 | 行為 |
| --- | --- |
| `SessionProfileCompiler.Load(string installationRoot, string id, int presetIndex)` → `SessionProfilePackage` | 讀取並驗證套件。`id` 必須是單純的目錄名稱（否則為 `OL_E_SESSION_PROFILE_PATH_ESCAPE`）；`presetIndex` 為 1..99（`MinPresetIndex`..`MaxPresetIndex`；否則為 `OL_E_SESSION_PROFILE_PRESET_NOT_FOUND`）。檔案不存在：`OL_E_SESSION_PROFILE_NOT_FOUND`；大於 `MaxBytes`（256 KiB）、無效的 YAML、錨點、未知的鍵，或 `id` 與目錄名稱不同：`OL_E_SESSION_PROFILE_INVALID`；其他 `schema`：`OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED`。資產路徑受限於套件內（`OL_E_SESSION_PROFILE_PATH_ESCAPE`、`OL_E_SESSION_PROFILE_ASSET_MISSING`）。每種失敗都是 `SessionProfileException`。 |
| `SessionProfileCompiler.Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` → `LaunchSpec` | 回傳套用設定檔後的 `baseline`：對 `NewMap` 套用設定檔的 `new` 區塊（地圖、進入點，以及任何日期／時間／年份／天氣）；將預設組合（preset）的 `settings` 合併覆蓋到 `Environment.General`；若存在則套用預設組合的 `Presentation`、`InternetTextures` 與 `Behavior`；並設定 `SessionProfile` = `profile.Metadata`。對 `NewMap` 還會依設定檔的 `compatibility` 清單檢查地圖（`OL_E_SESSION_PROFILE_MAP_MISMATCH`）。 |
| `SessionProfileCompiler.ValidateCompatibility(SessionProfilePackage, string installationRoot, WorldSpec world, WorldMode mode)` | 其他模式的相容性檢查（對 `SavedSituation`，地圖是從 `.osn` 讀取）。CLI 在建構最終的 `WorldSpec` 之後呼叫它。 |
| `SessionProfileCompiler.Schema`、`MaxBytes`、`SchemaKeys` | `"omsilaunch.session-profile/v1"`、`262144`，以及每個 YAML 對應中接受的鍵。 |
| `SessionProfilePackage(RootPath, Metadata, CompatibleMaps, New, Preset)`、`ProfileNew`、`ProfilePreset` | 已載入的套件；`Preset` 僅為所選的預設組合。 |
| `SessionProfileException(string code, string message)` | 具有 `Code`（`OL_E_SESSION_PROFILE_*` 代碼之一）的 `IOException`；訊息為 `"<code>: <message>"`。 |

CLI 另外會拒絕與設定檔衝突的命令列旗標（`OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT`）；該檢查不屬於編譯器。完整的合併順序請參閱[工作階段設定檔](session-profiles.md#precedence-and-override-conflicts)。

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
### 平台型別（`OmsiLaunch.Process`）

| 型別 | 穩定性 | 用途 |
| --- | --- | --- |
| `IRuntimePlatform` | 作為 `OmsiLaunchService` 建構函式的參數型別時為 `STABLE_BETA` | 偵測平台、檢查可寫入性，並啟動、觀察、終止及等待 `Omsi.exe`。請傳入 `new CurrentWindowsX64Platform()`；不支援自行實作。 |
| `CurrentWindowsX64Platform` | `STABLE_BETA` | 唯一的實作：Windows x64 主機，以 `CreateProcessW` 啟動 `Omsi.exe`，以 `TerminateProcess` 進行標準停止。其方法由服務呼叫；整合者只需建構它。成員（與 `IRuntimePlatform` 共用）：`Detect(root)` 回傳計畫的 `RuntimePlatformInfo`；當主機無法執行工作階段時，`ValidateCurrent(info)` 擲回 `OL_E_UNSUPPORTED_OPERATING_SYSTEM`／`OL_E_UNSUPPORTED_OS_ARCHITECTURE`／`OL_E_PLATFORM_CAPABILITY_MISSING`；`IsInstallationWritable(root)` 支撐 `OL_E_INSTALLATION_NOT_WRITABLE`；`StartAsync(request, sha256)` 建立 `Omsi.exe` 並記錄處理程序識別（PID、建立時間、路徑及服務計算出的雜湊；`OL_E_PROCESS_START_FAILED`、`OL_E_PROCESS_CREATION_TIME_FAILED`）；`HasExited`、`WaitForExitAsync` 與 `Terminate` 用於觀察並結束該處理程序。 |
| `InstallationLease`、`LaunchedProcess`、`ProcessIdentity`、`ReleaseManifest`、`RuntimeArtifact`、`RuntimeArtifactSet`、`StartupProcessRequest`、`CurrentRuntimeCommandStore`、`IOmsiProcessController`、`OmsiProcessState` | `INTERNAL` | 因服務與測試共用而在組件中為公開。不屬於整合介面；`LaunchedProcess` 在內部包裝 OMSI 的處理程序與執行緒 handle（它們不是公開成員），且永遠不會由 `IOmsiLaunch` 回傳。 |

<a id="thread-safety"></a>
## 執行緒安全

- `OmsiLaunchService` 可安全地對不同工作階段進行並行呼叫：工作階段存放於 `ConcurrentDictionary` 中，每個針對單一工作階段的變更都在該工作階段的私有鎖定下進行。
- 對同一工作階段的並行呼叫是安全的，但在必要之處會序列化：`ExecuteRuntimeAsync` 會取得每個工作階段的閘道，因此第二個命令會等待第一個命令（其逾時從放入信箱時開始計算）。
- 從 `StartSessionAsync` 回傳的那一刻起，直到工作階段終止為止，監督程式都在執行緒集區 task（`Task.Run`）上執行；它每 100 ms 輪詢一次遙測與處理程序。呼叫端永遠不會執行監督程式的程式碼。
- `StopAsync` 與 `GetStatusAsync` 會同步完成，可從任何執行緒呼叫，包括在 `ProcessExit` 處理常式內（CLI 以 4 s 的時間上限這麼做）。
- 沒有任何 API 呼叫具有執行緒親和性；也沒有任何呼叫需要同步處理內容（synchronization context）。

<a id="what-is-not-in-the-api"></a>
## API 不包含的內容

- 沒有 `IntPtr`、`nint`、Win32 handle、原生位址、VMT 指標或處理程序物件。鍵以 `internal_` 開頭或以 `_address`、`_pointer`、`_vmt` 結尾的結果值，會在結果離開 `ExecuteRuntimeAsync` 之前被移除。
- 沒有 `internal.*` 執行階段操作：`internal.road-vehicles.make-basic` 在登錄中為 `InternalOnly`，並會從 API 與 CLI 回傳 `OL_E_RUNTIME_OPERATION_UNKNOWN`。
- 沒有原始的 OMSI 記憶體讀寫，除 `LaunchSpec` 宣告的內容外，也沒有對安裝的檔案層級存取。
- 沒有跨處理程序的 handle：[本機控制平面](local-control.md)是唯一的跨處理程序途徑，且只接受 `session.status`、`session.events`、`session.stop` 與 `runtime.execute`。
- 啟動時不套用玩家車輛。協同式關閉、`LAST_MAP_STATE`、起始日期／時間／天氣，以及鍵盤／控制器文件 overlay 皆為 `EXPERIMENTAL`。

<a id="stability-summary"></a>
## 穩定性摘要

| 介面 | 穩定性 |
| --- | --- |
| `OmsiLaunchService` 建構函式、`OmsiLaunchRuntimePaths` | `STABLE_BETA` |
| `PlanSessionAsync`、`StartSessionAsync`（NEW_MAP、SAVED_SITUATION）、`GetStatusAsync`、`WaitForAsync`、`StopAsync`、`CloseAsync` | `STABLE_BETA` |
| `ExecuteRuntimeAsync` 傳輸；`PublicStableBeta` 操作 | `STABLE_BETA` |
| `PublicExperimental` 操作、`D3DRuntimeApi`、`camera.lock` | `EXPERIMENTAL` |
| 日期／時間／天氣／輸入的 spec 成員、`DiagnosticsSpec`、`ExpectedExecutableSha256`、`RestoreConfiguration`、`ShutdownTimeoutSeconds`、`CooperativeShutdown` | `EXPERIMENTAL` |
| 玩家車輛的 spec 成員 | `PARTIAL` |
| `RuntimeCommandWire`、`StartupHandoff`、`StartupHandoffWire`、`StartupModule`、`StartDateTime`、`StartWeather`、`TelemetryRingWire`、`IRuntimePlatform` 實作、所有實作組件 | `INTERNAL` |
| `WorldMode.LastSituation`、`internal.*` 操作 | `UNAVAILABLE` |
