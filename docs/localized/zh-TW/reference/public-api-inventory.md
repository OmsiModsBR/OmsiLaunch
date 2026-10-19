# 公開 API 清單

<!-- l10n: source=reference/public-api-inventory.md source-sha256=1038165496fa7e22c0b936388cb2dde7b90ed33b193b8f77d462d10d9eff149a -->
> 本頁為 OmsiLaunch 0.1.0-beta4 [英文原始頁面](../../../reference/public-api-inventory.md) 的翻譯。英文頁面為規範版本：若有出入，以英文頁面與程式碼為準。

本頁由 `tests/OmsiLaunch.DocumentationTests` 依據編譯後的組件產生（`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`）；當本頁與程式碼不再一致時，文件檢查便會失敗。本頁列出 `OmsiLaunch.Api`、`OmsiLaunch.Core` 與 `OmsiLaunch.Process` 匯出的每個型別及其穩定性，以及每個公開成員的簽章。語意、前置條件、錯誤與範例記載於各標題所指的頁面（預設：[公開 API](public-api.md)）。穩定性用語：`STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`（基於技術原因而公開，並非整合介面）、`UNAVAILABLE`（請參閱[公開 API](public-api.md#stability-vocabulary)）。編譯器產生的 record 成員（`Equals`、`GetHashCode`、`ToString`、`Deconstruct`、`<Clone>$`、`EqualityContract`）不予列出。

## `OmsiLaunch.Api`

### `Capability`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `bool Available { get; init; }` |
| 屬性 | `string EvidenceState { get; init; }` |
| 屬性 | `string Reason { get; init; }` |

### `ContentIdentity`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| 屬性 | `string Identity { get; init; }` |
| 屬性 | `string Kind { get; init; }` |
| 屬性 | `string DisplayName { get; init; }` |

### `ContentQueryKind`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Maps = 0` |
| 值 | `Situations = 1` |
| 值 | `Vehicles = 2` |
| 值 | `Repaints = 3` |
| 值 | `Hofs = 4` |
| 值 | `FleetNumbers = 5` |
| 值 | `Registrations = 6` |
| 值 | `Addons = 7` |
| 值 | `Entrypoints = 8` |
| 值 | `SessionProfiles = 9` |
| 值 | `RuntimeModules = 10` |

### `D3DDeviceState`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `NotReady = 0` |
| 值 | `Ready = 1` |
| 值 | `Lost = 2` |
| 值 | `Resetting = 3` |
| 值 | `Stopping = 4` |
| 值 | `Stopped = 5` |

### `D3DDeviceStatus`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| 屬性 | `bool Available { get; init; }` |
| 屬性 | `D3DDeviceState State { get; init; }` |
| 屬性 | `uint Generation { get; init; }` |
| 屬性 | `uint LiveTextureCount { get; init; }` |
| 屬性 | `bool ResetHookInstalled { get; init; }` |
| 屬性 | `uint ExecutionThreadId { get; init; }` |
| 屬性 | `uint LastResetThreadId { get; init; }` |
| 屬性 | `int QueryInterfaceHResult { get; init; }` |
| 屬性 | `int CooperativeLevelHResult { get; init; }` |
| 屬性 | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 擴充方法 | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 擴充方法 | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 擴充方法 | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 擴充方法 | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 擴充方法 | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| 屬性 | `D3DTextureHandle Handle { get; init; }` |
| 屬性 | `D3DTextureResourceState State { get; init; }` |
| 屬性 | `D3DDeviceState DeviceState { get; init; }` |
| 屬性 | `uint Generation { get; init; }` |
| 屬性 | `uint Width { get; init; }` |
| 屬性 | `uint Height { get; init; }` |
| 屬性 | `D3DTextureFormat Format { get; init; }` |
| 屬性 | `uint Levels { get; init; }` |
| 屬性 | `uint Level { get; init; }` |
| 屬性 | `uint LevelWidth { get; init; }` |
| 屬性 | `uint LevelHeight { get; init; }` |
| 屬性 | `int HResult { get; init; }` |
| 屬性 | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `A8R8G8B8 = 0` |
| 值 | `X8R8G8B8 = 1` |
| 值 | `R5G6B5 = 2` |
| 值 | `X1R5G5B5 = 3` |
| 值 | `A1R5G5B5 = 4` |
| 值 | `A4R4G4B4 = 5` |
| 值 | `A8 = 6` |
| 值 | `L8 = 7` |
| 值 | `A8L8 = 8` |

### `D3DTextureHandle`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `D3DTextureHandle(string Value)` |
| 屬性 | `string Value { get; init; }` |

### `D3DTextureResourceState`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Live = 0` |
| 值 | `Released = 1` |
| 值 | `Stale = 2` |

### `D3DTextureUpdate`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| 屬性 | `uint Level { get; init; }` |
| 屬性 | `uint X { get; init; }` |
| 屬性 | `uint Y { get; init; }` |
| 屬性 | `uint Width { get; init; }` |
| 屬性 | `uint Height { get; init; }` |
| 屬性 | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| 屬性 | `DateTimeMode Mode { get; init; }` |
| 屬性 | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Explicit = 1` |
| 值 | `System = 2` |

### `DiagnosticsSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| 屬性 | `bool Log { get; init; }` |
| 屬性 | `bool Verbose { get; init; }` |
| 屬性 | `bool OmsiLogAll { get; init; }` |
| 屬性 | `bool ProcessTrace { get; init; }` |
| 屬性 | `bool PluginTrace { get; init; }` |
| 屬性 | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `PresentedIndex = 1` |
| 值 | `Identity = 2` |

### `EntrypointSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| 屬性 | `EntrypointMode Mode { get; init; }` |
| 屬性 | `OptionalValue<int> PresentedIndex { get; init; }` |
| 屬性 | `OptionalValue<string> Identity { get; init; }` |
| 靜態屬性 | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Add = 0` |
| 值 | `Mirror = 1` |

### `HofManagementSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| 屬性 | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| 屬性 | `HofSourceKind SourceKind { get; init; }` |
| 屬性 | `string Source { get; init; }` |
| 屬性 | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| 屬性 | `HofApplyMode Mode { get; init; }` |
| 屬性 | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Collection = 0` |
| 值 | `Directory = 1` |
| 值 | `Vehicle = 2` |
| 值 | `File = 3` |

### `IOmsiLaunch`

介面，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 方法 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| 屬性 | `OptionalValue<string> KeyboardDocument { get; init; }` |
| 屬性 | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 靜態方法 | `string IdentityKey(string root)` |
| 靜態方法 | `string NormalizeRoot(string root)` |
| 靜態方法 | `IReadOnlyList<string> Segments(string relativePath)` |
| 靜態方法 | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| 屬性 | `string RootPath { get; init; }` |
| 屬性 | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Native = 0` |
| 值 | `Disabled = 1` |
| 值 | `Override = 2` |

### `InternetTexturesSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| 屬性 | `InternetTexturesMode Mode { get; init; }` |
| 屬性 | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| 屬性 | `bool RestoreConfiguration { get; init; }` |
| 屬性 | `bool SuppressStaleClosecheckWarning { get; init; }` |
| 屬性 | `int StartupTimeoutSeconds { get; init; }` |
| 屬性 | `int ShutdownTimeoutSeconds { get; init; }` |
| 屬性 | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| 屬性 | `string Code { get; init; }` |
| 屬性 | `string Message { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| 屬性 | `InstallationSpec Installation { get; init; }` |
| 屬性 | `WorldSpec World { get; init; }` |
| 屬性 | `DateSpec Date { get; init; }` |
| 屬性 | `TimeSpec Time { get; init; }` |
| 屬性 | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| 屬性 | `EnvironmentSpec Environment { get; init; }` |
| 屬性 | `LaunchBehaviorSpec Behavior { get; init; }` |
| 屬性 | `YearSpec Year { get; init; }` |
| 屬性 | `WeatherSpec Weather { get; init; }` |
| 屬性 | `InputSpec Input { get; init; }` |
| 屬性 | `DiagnosticsSpec Diagnostics { get; init; }` |
| 屬性 | `SessionPresentationSpec Presentation { get; init; }` |
| 屬性 | `InternetTexturesSpec InternetTextures { get; init; }` |
| 屬性 | `SessionProfileMetadata SessionProfile { get; init; }` |
| 屬性 | `HofManagementSpec HofManagement { get; init; }` |
| 屬性 | `RuntimeModulesSpec Modules { get; init; }` |
| 屬性 | `HofManagementSpec EffectiveHofManagement { get; }` |
| 屬性 | `RuntimeModulesSpec EffectiveModules { get; }` |
| 屬性 | `YearSpec EffectiveYear { get; }` |
| 屬性 | `WeatherSpec EffectiveWeather { get; }` |
| 屬性 | `InputSpec EffectiveInput { get; }` |
| 屬性 | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| 屬性 | `SessionPresentationSpec EffectivePresentation { get; }` |
| 屬性 | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

密封類別，位於 `OmsiLaunch.Api`。穩定性：`EXPERIMENTAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `OmsiRuntimeException(string code, string detail = null)` |
| 屬性 | `string Code { get; }` |

### `OptionalValue`

record struct，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `OptionalValue(Presence Presence, T Value)` |
| 屬性 | `Presence Presence { get; init; }` |
| 屬性 | `T Value { get; init; }` |
| 屬性 | `bool IsSet { get; }` |
| 靜態屬性 | `OptionalValue<T> Unset { get; }` |
| 靜態方法 | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null, IReadOnlyList<string> RuntimeOperations = null, int TickMilliseconds = 0, string SnapshotSha256 = null)` |
| 常數 | `string Eligible = "ELIGIBLE"` |
| 常數 | `string Rejected = "REJECTED"` |
| 常數 | `string ActivationRequested = "requested"` |
| 常數 | `string ActivationSettingsFile = "settings-file"` |
| 常數 | `string ActivationContent = "content"` |
| 常數 | `string ActivationDefault = "default"` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Version { get; init; }` |
| 屬性 | `string Status { get; init; }` |
| 屬性 | `bool Requested { get; init; }` |
| 屬性 | `string FailurePolicy { get; init; }` |
| 屬性 | `IReadOnlyList<string> Operations { get; init; }` |
| 屬性 | `string Code { get; init; }` |
| 屬性 | `string Reason { get; init; }` |
| 屬性 | `string ManifestSha256 { get; init; }` |
| 屬性 | `string Activation { get; init; }` |
| 屬性 | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| 屬性 | `IReadOnlyList<string> RuntimeOperations { get; init; }` |
| 屬性 | `int TickMilliseconds { get; init; }` |
| 屬性 | `string SnapshotSha256 { get; init; }` |
| 屬性 | `bool IsEligible { get; }` |
| 屬性 | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |
| 屬性 | `IReadOnlyList<string> EffectiveRuntimeOperations { get; }` |

### `PlannedModuleSetting`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PlannedModuleSetting(string Key, string Value, string Source)` |
| 屬性 | `string Key { get; init; }` |
| 屬性 | `string Value { get; init; }` |
| 屬性 | `string Source { get; init; }` |

### `PlannedMutation`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| 屬性 | `string RelativePath { get; init; }` |
| 屬性 | `string SemanticKey { get; init; }` |
| 屬性 | `string RequestedValue { get; init; }` |
| 屬性 | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| 屬性 | `OptionalValue<string> Model { get; init; }` |
| 屬性 | `OptionalValue<string> Repaint { get; init; }` |
| 屬性 | `OptionalValue<string> Hof { get; init; }` |
| 屬性 | `OptionalValue<string> FleetNumber { get; init; }` |
| 屬性 | `OptionalValue<string> Registration { get; init; }` |
| 屬性 | `bool Enabled { get; }` |

### `Presence`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Set = 1` |

### `PublicCapabilityClassification`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [capabilities](capabilities.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `PublicStableBeta = 0` |
| 值 | `PublicExperimental = 1` |
| 值 | `InternalOnly = 2` |
| 值 | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [capabilities](capabilities.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Family { get; init; }` |
| 屬性 | `PublicCapabilityClassification Classification { get; init; }` |
| 屬性 | `PublicCapabilityKind Kind { get; init; }` |
| 屬性 | `bool RequiresSession { get; init; }` |
| 屬性 | `bool RequiresExactProfile { get; init; }` |
| 屬性 | `string ApiRoute { get; init; }` |
| 屬性 | `string CliRoute { get; init; }` |
| 屬性 | `string RuntimeValidation { get; init; }` |
| 屬性 | `string Description { get; init; }` |
| 屬性 | `IReadOnlyList<string> HandleTypes { get; init; }` |
| 屬性 | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| 屬性 | `string NativeCapability { get; init; }` |
| 屬性 | `string EvidenceNote { get; init; }` |
| 屬性 | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| 屬性 | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [capabilities](capabilities.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Read = 0` |
| 值 | `Write = 1` |
| 值 | `Action = 2` |
| 值 | `Event = 3` |

### `PublicCapabilityRegistry`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [capabilities](capabilities.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string ProtocolVersion = "0.1"` |
| 靜態欄位 | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| 靜態屬性 | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| 靜態屬性 | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| 靜態屬性 | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| 靜態方法 | `PublicCapabilityDescriptor Get(string id)` |
| 靜態方法 | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| 靜態方法 | `bool IsInternalResultKey(string key)` |
| 靜態方法 | `bool IsModuleOperation(string operation)` |
| 靜態方法 | `bool IsPublicRuntimeOperation(string operation)` |
| 靜態方法 | `string NativeCapabilityOf(string operation)` |
| 靜態方法 | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| 靜態方法 | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| 靜態方法 | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [errors](errors.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string InvalidArgument = "invalid_argument"` |
| 常數 | `string UnsupportedProfile = "unsupported_profile"` |
| 常數 | `string Session = "session"` |
| 常數 | `string Runtime = "runtime"` |
| 常數 | `string NotFound = "not_found"` |
| 常數 | `string Transaction = "transaction"` |
| 常數 | `string Internal = "internal"` |

### `PublicErrorCodes`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [errors](errors.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string CANCELLED = "OL_E_CANCELLED"` |
| 常數 | `string INTERNAL = "OL_E_INTERNAL"` |
| 常數 | `string TIMEOUT = "OL_E_TIMEOUT"` |
| 常數 | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| 常數 | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| 常數 | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| 常數 | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| 常數 | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| 常數 | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| 常數 | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| 常數 | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| 常數 | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| 常數 | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| 常數 | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| 常數 | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| 常數 | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| 常數 | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| 常數 | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| 常數 | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| 常數 | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| 常數 | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| 常數 | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| 常數 | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| 常數 | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| 常數 | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| 常數 | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| 常數 | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| 常數 | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| 常數 | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| 常數 | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| 常數 | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| 常數 | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| 常數 | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| 常數 | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| 常數 | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| 常數 | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| 常數 | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| 常數 | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| 常數 | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| 常數 | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| 常數 | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| 常數 | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| 常數 | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| 常數 | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| 常數 | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| 常數 | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| 常數 | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| 常數 | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| 常數 | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| 常數 | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| 常數 | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| 常數 | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| 常數 | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| 常數 | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| 常數 | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| 常數 | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| 常數 | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| 常數 | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| 常數 | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| 常數 | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| 常數 | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| 常數 | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| 常數 | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| 常數 | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| 常數 | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| 常數 | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| 常數 | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| 常數 | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| 常數 | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| 常數 | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| 常數 | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| 常數 | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| 常數 | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| 常數 | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| 常數 | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| 常數 | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| 常數 | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| 常數 | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| 常數 | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| 常數 | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| 常數 | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| 常數 | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| 常數 | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| 常數 | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| 常數 | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| 常數 | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| 常數 | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| 常數 | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| 常數 | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| 常數 | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| 常數 | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| 常數 | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| 常數 | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| 常數 | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| 常數 | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| 常數 | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| 常數 | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| 常數 | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| 常數 | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| 常數 | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| 常數 | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| 常數 | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| 常數 | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| 常數 | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| 常數 | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| 常數 | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| 常數 | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| 常數 | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| 常數 | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| 常數 | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| 常數 | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| 常數 | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| 常數 | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| 常數 | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| 常數 | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| 常數 | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| 常數 | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| 常數 | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| 常數 | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| 常數 | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| 常數 | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| 常數 | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| 常數 | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| 常數 | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| 常數 | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| 常數 | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| 常數 | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| 常數 | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| 常數 | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| 常數 | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| 常數 | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| 常數 | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| 常數 | `string START_SESSION = "OL_E_START_SESSION"` |
| 常數 | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| 常數 | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| 常數 | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| 常數 | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| 常數 | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| 常數 | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| 常數 | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| 常數 | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| 常數 | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| 常數 | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| 常數 | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| 常數 | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| 常數 | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| 常數 | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| 常數 | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| 常數 | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| 常數 | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| 常數 | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| 常數 | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| 常數 | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| 常數 | `string MODULE_OPERATION_NOT_DECLARED = "OL_E_MODULE_OPERATION_NOT_DECLARED"` |
| 常數 | `string MODULE_CONTENT_INPUT_UNDECLARED = "OL_E_MODULE_CONTENT_INPUT_UNDECLARED"` |
| 常數 | `string MODULE_CONTENT_INPUT_INVALID = "OL_E_MODULE_CONTENT_INPUT_INVALID"` |
| 常數 | `string UI_NOT_PAUSED = "OL_E_UI_NOT_PAUSED"` |
| 常數 | `string UI_BUSY = "OL_E_UI_BUSY"` |
| 常數 | `string UI_ALREADY_TAKEN_OVER = "OL_E_UI_ALREADY_TAKEN_OVER"` |
| 常數 | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| 常數 | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| 常數 | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| 常數 | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| 常數 | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| 常數 | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| 常數 | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| 常數 | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| 常數 | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| 常數 | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| 常數 | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| 常數 | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| 常數 | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| 常數 | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| 常數 | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| 常數 | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| 常數 | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| 常數 | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| 常數 | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| 常數 | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| 常數 | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| 常數 | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| 常數 | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| 常數 | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| 靜態欄位 | `IReadOnlyList<PublicErrorDescriptor> All` |
| 靜態方法 | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [errors](errors.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PublicErrorDescriptor(string Code, string Category)` |
| 屬性 | `string Code { get; init; }` |
| 屬性 | `string Category { get; init; }` |

### `PublicEvidence`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| 常數 | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| 常數 | `string Partial = "PARTIAL"` |
| 常數 | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| 常數 | `string Unavailable = "UNAVAILABLE"` |
| 靜態欄位 | `IReadOnlyList<string> All` |

### `PublicExitCode`

列舉 (`int`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [exit-codes](exit-codes.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Success = 0` |
| 值 | `SessionFailed = 1` |
| 值 | `InvalidArguments = 2` |
| 值 | `UnsupportedProfile = 3` |
| 值 | `NoActiveSession = 4` |
| 值 | `RuntimeUnavailable = 5` |
| 值 | `NotFound = 6` |
| 值 | `OperationRejected = 7` |
| 值 | `TransactionRecoveryFailed = 8` |
| 值 | `InternalError = 10` |

### `PublicMutationPolicy`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string None = "none"` |
| 常數 | `string OmsiState = "omsi-state"` |
| 常數 | `string SessionResource = "session-resource"` |
| 常數 | `string SessionPolicy = "session-policy"` |
| 靜態欄位 | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `bool Required { get; init; }` |
| 屬性 | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| 屬性 | `bool Accepted { get; init; }` |
| 屬性 | `string ErrorCode { get; init; }` |
| 屬性 | `string Message { get; init; }` |

### `PublicRuntimeOperation`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string NativeCapability { get; init; }` |
| 屬性 | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| 屬性 | `IReadOnlyList<string> CliRoutes { get; init; }` |
| 屬性 | `bool Paged { get; init; }` |
| 屬性 | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| 屬性 | `bool Pending { get; init; }` |
| 屬性 | `bool Recovered { get; init; }` |
| 屬性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [runtime-control](runtime-control.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| 屬性 | `Guid SessionId { get; init; }` |
| 屬性 | `ulong RequestId { get; init; }` |
| 屬性 | `string Operation { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [runtime-control](runtime-control.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| 屬性 | `Guid SessionId { get; init; }` |
| 屬性 | `ulong RequestId { get; init; }` |
| 屬性 | `bool Succeeded { get; init; }` |
| 屬性 | `string ErrorCode { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `uint Magic = 1330401859` |
| 常數 | `ushort Version = 1` |
| 常數 | `int HeaderSize = 72` |
| 靜態方法 | `byte[] SerializeRequest(RuntimeCommand command)` |
| 靜態方法 | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| 靜態方法 | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| 靜態方法 | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| 靜態方法 | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| 屬性 | `string Type { get; init; }` |
| 屬性 | `DateTimeOffset TimestampUtc { get; init; }` |
| 屬性 | `long Sequence { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| 屬性 | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| 屬性 | `string Key { get; init; }` |
| 屬性 | `string Type { get; init; }` |
| 屬性 | `string Default { get; init; }` |
| 屬性 | `bool AllowsAuto { get; init; }` |
| 屬性 | `string Minimum { get; init; }` |
| 屬性 | `string Maximum { get; init; }` |
| 屬性 | `IReadOnlyList<string> Choices { get; init; }` |
| 屬性 | `string Flag { get; init; }` |
| 屬性 | `string Description { get; init; }` |

### `RuntimeModuleSummary`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Version { get; init; }` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `string Activation { get; init; }` |
| 屬性 | `bool IsValid { get; init; }` |
| 屬性 | `IReadOnlyList<string> Operations { get; init; }` |
| 屬性 | `string ContentBindingMap { get; init; }` |
| 屬性 | `string ErrorCode { get; init; }` |
| 屬性 | `string ErrorMessage { get; init; }` |
| 屬性 | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| 屬性 | `IReadOnlyList<string> Enable { get; init; }` |
| 屬性 | `IReadOnlyList<string> Disable { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| 屬性 | `IReadOnlyList<string> EffectiveDisable { get; }` |
| 屬性 | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| 屬性 | `string OsFamily { get; init; }` |
| 屬性 | `string OsVersion { get; init; }` |
| 屬性 | `string OsArchitecture { get; init; }` |
| 屬性 | `string HostArchitecture { get; init; }` |
| 屬性 | `string OmsiArchitecture { get; init; }` |
| 屬性 | `string PluginArchitecture { get; init; }` |
| 屬性 | `bool CurrentPlatformSupported { get; init; }` |
| 屬性 | `bool LegacyPlatform { get; init; }` |
| 屬性 | `bool Wow64Available { get; init; }` |
| 屬性 | `bool InstallationWritable { get; init; }` |
| 屬性 | `bool ProcessLaunchSupported { get; init; }` |
| 屬性 | `bool PluginRuntimeSupported { get; init; }` |
| 屬性 | `bool NativeInteropSupported { get; init; }` |
| 屬性 | `bool SharedMemorySupported { get; init; }` |
| 屬性 | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SemanticDate(int Year, int Month, int Day)` |
| 屬性 | `int Year { get; init; }` |
| 屬性 | `int Month { get; init; }` |
| 屬性 | `int Day { get; init; }` |

### `SemanticTime`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SemanticTime(int Hour, int Minute, int Second)` |
| 屬性 | `int Hour { get; init; }` |
| 屬性 | `int Minute { get; init; }` |
| 屬性 | `int Second { get; init; }` |

### `SessionHandle`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionHandle(Guid SessionId)` |
| 屬性 | `Guid SessionId { get; init; }` |

### `SessionPlan`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| 屬性 | `Guid SessionId { get; init; }` |
| 屬性 | `string BuildProfileId { get; init; }` |
| 屬性 | `LaunchSpec Spec { get; init; }` |
| 屬性 | `RuntimePlatformInfo Platform { get; init; }` |
| 屬性 | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| 屬性 | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| 屬性 | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| 屬性 | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| 屬性 | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| 屬性 | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| 屬性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| 屬性 | `bool IsRunnable { get; init; }` |
| 屬性 | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| 屬性 | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| 屬性 | `SplashMode Splash { get; init; }` |
| 屬性 | `OptionalValue<string> Language { get; init; }` |
| 屬性 | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| 屬性 | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `string Version { get; init; }` |
| 屬性 | `string Author { get; init; }` |
| 屬性 | `string PresetId { get; init; }` |
| 屬性 | `int PresetIndex { get; init; }` |
| 屬性 | `string PresetName { get; init; }` |
| 屬性 | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| 屬性 | `int Index { get; init; }` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Name { get; init; }` |

### `SessionProfileSummary`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `string Version { get; init; }` |
| 屬性 | `string Author { get; init; }` |
| 屬性 | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| 屬性 | `string MapIdentity { get; init; }` |
| 屬性 | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| 屬性 | `bool IsValid { get; init; }` |
| 屬性 | `string ErrorCode { get; init; }` |
| 屬性 | `string ErrorMessage { get; init; }` |

### `SessionState`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Created = 0` |
| 值 | `ValidatingPlatform = 1` |
| 值 | `Planning = 2` |
| 值 | `AcquiringInstallationLock = 3` |
| 值 | `RecoveringPreviousTransaction = 4` |
| 值 | `Snapshotting = 5` |
| 值 | `ApplyingConfiguration = 6` |
| 值 | `DeployingRuntime = 7` |
| 值 | `CreatingStartupHandoff = 8` |
| 值 | `StartingProcess = 9` |
| 值 | `WaitingForPlugin = 10` |
| 值 | `PluginBootstrap = 11` |
| 值 | `StartingWorld = 12` |
| 值 | `EnteringGameplay = 13` |
| 值 | `Running = 14` |
| 值 | `ProcessExited = 15` |
| 值 | `Restoring = 16` |
| 值 | `CleaningRuntime = 17` |
| 值 | `Completed = 18` |
| 值 | `Failed = 19` |

### `SessionStatus`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| 屬性 | `Guid SessionId { get; init; }` |
| 屬性 | `SessionState State { get; init; }` |
| 屬性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| 屬性 | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Native = 0` |
| 值 | `Managed = 1` |

### `StartDateTime`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| 屬性 | `int? Year { get; init; }` |
| 屬性 | `int? DayOfYear { get; init; }` |
| 屬性 | `int? Hour { get; init; }` |
| 屬性 | `int? Minute { get; init; }` |
| 屬性 | `int? Second { get; init; }` |
| 屬性 | `bool IsEmpty { get; }` |

### `StartWeather`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| 屬性 | `WeatherMode Mode { get; init; }` |
| 屬性 | `string Preset { get; init; }` |
| 屬性 | `string Icao { get; init; }` |

### `StartupHandoff`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| 屬性 | `Guid SessionId { get; init; }` |
| 屬性 | `string BuildProfileId { get; init; }` |
| 屬性 | `WorldMode WorldMode { get; init; }` |
| 屬性 | `string MapIdentity { get; init; }` |
| 屬性 | `int PresentedEntrypointIndex { get; init; }` |
| 屬性 | `bool HeadlessStart { get; init; }` |
| 屬性 | `bool PlayerVehicleEnabled { get; init; }` |
| 屬性 | `DateTimeMode DateMode { get; init; }` |
| 屬性 | `DateTimeMode TimeMode { get; init; }` |
| 屬性 | `string EntrypointIdentity { get; init; }` |
| 屬性 | `string SituationIdentity { get; init; }` |
| 屬性 | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| 屬性 | `bool PluginTrace { get; init; }` |
| 屬性 | `bool NativeTrace { get; init; }` |
| 屬性 | `StartDateTime Start { get; init; }` |
| 屬性 | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `uint Magic = 1330402120` |
| 常數 | `ushort Version = 8` |
| 常數 | `int MaxModules = 64` |
| 靜態方法 | `byte[] Serialize(StartupHandoff value)` |
| 靜態方法 | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "", string SnapshotSha256 = "")` |
| 屬性 | `string RelativeDirectory { get; init; }` |
| 屬性 | `string ManifestSha256 { get; init; }` |
| 屬性 | `string Settings { get; init; }` |
| 屬性 | `string SnapshotSha256 { get; init; }` |

### `TelemetryRingWire`

靜態類別，位於 `OmsiLaunch.Api`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `uint Magic = 1381256271` |
| 常數 | `int Version = 1` |
| 常數 | `int SlotCount = 64` |
| 常數 | `int SlotSize = 2048` |
| 常數 | `int HeaderSize = 64` |
| 常數 | `int Capacity = 131136` |
| 常數 | `int MaxPayload = 2036` |
| 靜態方法 | `void Initialize(MemoryMappedViewAccessor view)` |
| 靜態方法 | `bool IsRing(MemoryMappedViewAccessor view)` |
| 靜態方法 | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| 靜態方法 | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| 屬性 | `DateTimeMode Mode { get; init; }` |
| 屬性 | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

列舉 (`byte`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Preset = 1` |
| 值 | `Icao = 2` |
| 值 | `RealCurrent = 3` |

### `WeatherSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| 屬性 | `WeatherMode Mode { get; init; }` |
| 屬性 | `OptionalValue<string> Preset { get; init; }` |
| 屬性 | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

列舉 (`int`)，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 值 | `NewMap = 0` |
| 值 | `SavedSituation = 1` |
| 值 | `LastMapState = 2` |
| 值 | `LastSituation = 2` |

### `WorldSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`STABLE_BETA`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| 屬性 | `WorldMode Mode { get; init; }` |
| 屬性 | `OptionalValue<string> MapIdentity { get; init; }` |
| 屬性 | `OptionalValue<string> SituationIdentity { get; init; }` |
| 屬性 | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| 屬性 | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| 屬性 | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

密封 record，位於 `OmsiLaunch.Api`。穩定性：`PARTIAL`。語意： [launchspec](launchspec.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| 屬性 | `DateTimeMode Mode { get; init; }` |
| 屬性 | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

密封類別，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `HofPlanningException(string code, string message)` |
| 屬性 | `string Code { get; }` |

### `LaunchValidation`

靜態類別，位於 `OmsiLaunch.Core`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 靜態方法 | `bool IsMapIdentity(string value)` |
| 靜態方法 | `bool IsSha256(string value)` |
| 靜態方法 | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

靜態類別，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 靜態方法 | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

密封 record，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| 屬性 | `string PluginBuildDirectory { get; init; }` |
| 屬性 | `string NativeBridgePath { get; init; }` |
| 屬性 | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

密封類別，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| 方法 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

密封 record，位於 `OmsiLaunch.Core`。穩定性：`EXPERIMENTAL`。語意： [session-profiles](session-profiles.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| 屬性 | `string Map { get; init; }` |
| 屬性 | `int? EntrypointIndex { get; init; }` |
| 屬性 | `string EntrypointIdentity { get; init; }` |
| 屬性 | `SemanticDate Date { get; init; }` |
| 屬性 | `SemanticTime Time { get; init; }` |
| 屬性 | `int? Year { get; init; }` |
| 屬性 | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

密封 record，位於 `OmsiLaunch.Core`。穩定性：`EXPERIMENTAL`。語意： [session-profiles](session-profiles.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| 屬性 | `int Index { get; init; }` |
| 屬性 | `string Id { get; init; }` |
| 屬性 | `string Name { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| 屬性 | `SessionPresentationSpec Presentation { get; init; }` |
| 屬性 | `InternetTexturesSpec InternetTextures { get; init; }` |
| 屬性 | `LaunchBehaviorSpec Behavior { get; init; }` |
| 屬性 | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| 屬性 | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

密封類別，位於 `OmsiLaunch.Core`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionPlanner(IRuntimePlatform platform)` |
| 方法 | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

靜態類別，位於 `OmsiLaunch.Core`。穩定性：`EXPERIMENTAL`。語意： [session-profiles](session-profiles.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string Schema = "omsilaunch.session-profile/v1"` |
| 常數 | `long MaxBytes = 262144` |
| 常數 | `int MinPresetIndex = 1` |
| 常數 | `int MaxPresetIndex = 99` |
| 靜態欄位 | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| 靜態方法 | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| 靜態方法 | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| 靜態方法 | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| 靜態方法 | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

密封類別，位於 `OmsiLaunch.Core`。穩定性：`EXPERIMENTAL`。語意： [session-profiles](session-profiles.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionProfileException(string code, string message)` |
| 屬性 | `string Code { get; }` |

### `SessionProfilePackage`

密封 record，位於 `OmsiLaunch.Core`。穩定性：`EXPERIMENTAL`。語意： [session-profiles](session-profiles.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| 屬性 | `string RootPath { get; init; }` |
| 屬性 | `SessionProfileMetadata Metadata { get; init; }` |
| 屬性 | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| 屬性 | `ProfileNew New { get; init; }` |
| 屬性 | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

靜態類別，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `int MinimumYear = 1900` |
| 常數 | `int MaximumYear = 2200` |
| 靜態方法 | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

靜態類別，位於 `OmsiLaunch.Core`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 靜態方法 | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 屬性 | `string Name { get; }` |
| 屬性 | `Guid SessionId { get; }` |
| 靜態方法 | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| 方法 | `void Dispose()` |
| 方法 | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 屬性 | `string Name { get; }` |
| 屬性 | `Guid SessionId { get; }` |
| 靜態方法 | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| 方法 | `void Dispose()` |

### `CurrentTelemetryStore`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 屬性 | `string Name { get; }` |
| 靜態方法 | `CurrentTelemetryStore Create(Guid sessionId)` |
| 方法 | `void Dispose()` |
| 方法 | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `CurrentWindowsX64Platform()` |
| 方法 | `RuntimePlatformInfo Detect(string root)` |
| 方法 | `bool HasExited(LaunchedProcess p)` |
| 方法 | `bool IsInstallationWritable(string root)` |
| 方法 | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| 方法 | `void Terminate(LaunchedProcess p)` |
| 方法 | `void ValidateCurrent(RuntimePlatformInfo p)` |
| 方法 | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

介面，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 方法 | `OmsiProcessState Observe()` |
| 方法 | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

介面，位於 `OmsiLaunch.Process`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 方法 | `RuntimePlatformInfo Detect(string installationRoot)` |
| 方法 | `bool HasExited(LaunchedProcess process)` |
| 方法 | `bool IsInstallationWritable(string installationRoot)` |
| 方法 | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| 方法 | `void Terminate(LaunchedProcess process)` |
| 方法 | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| 方法 | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 靜態方法 | `InstallationLease Acquire(string installationRoot)` |
| 方法 | `void Dispose()` |
| 靜態方法 | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 屬性 | `int ProcessId { get; }` |
| 屬性 | `int ThreadId { get; }` |
| 屬性 | `ProcessIdentity Identity { get; }` |
| 方法 | `void Dispose()` |

### `ModuleSessionProcess`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`STABLE_BETA`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `int MaxLineBytes = 65536` |
| 屬性 | `int ProcessId { get; }` |
| 屬性 | `bool HasExited { get; }` |
| 屬性 | `int? ExitCode { get; }` |
| 方法 | `void Dispose()` |
| 方法 | `void Kill()` |
| 靜態方法 | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| 方法 | `bool TryWriteLine(string line)` |
| 方法 | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

密封 record，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| 屬性 | `string State { get; init; }` |
| 屬性 | `int? ProcessId { get; init; }` |
| 屬性 | `bool Responding { get; init; }` |

### `ProcessIdentity`

密封 record，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| 屬性 | `int ProcessId { get; init; }` |
| 屬性 | `DateTimeOffset CreationTimeUtc { get; init; }` |
| 屬性 | `string ExecutablePath { get; init; }` |
| 屬性 | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

靜態類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 常數 | `string FileName = "release-manifest.json"` |
| 靜態方法 | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| 靜態方法 | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

密封 record，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| 屬性 | `string SourcePath { get; init; }` |
| 屬性 | `string DestinationRelativePath { get; init; }` |
| 屬性 | `string Sha256 { get; init; }` |
| 屬性 | `long Size { get; init; }` |

### `RuntimeArtifactSet`

密封類別，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 屬性 | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| 屬性 | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| 屬性 | `string IntegrityReference { get; }` |
| 靜態方法 | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| 方法 | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

密封 record，位於 `OmsiLaunch.Process`。穩定性：`INTERNAL`。語意： [public-api](public-api.md).

| 成員 | 簽章 |
| --- | --- |
| 建構函式 | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| 屬性 | `string ExecutablePath { get; init; }` |
| 屬性 | `string WorkingDirectory { get; init; }` |
| 屬性 | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
