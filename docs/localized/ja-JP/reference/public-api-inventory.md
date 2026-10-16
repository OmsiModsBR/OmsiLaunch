# 公開 API インベントリ

<!-- l10n: source=reference/public-api-inventory.md source-sha256=3c373c0c630e07ca0b28dc908aae11d05b87f4e5716c1442a868f3980637e134 -->
> このページは OmsiLaunch 0.1.0-beta4 の[英語版の原文ページ](../../../reference/public-api-inventory.md)の翻訳です。規範となるのは英語版です。内容が異なる場合は、英語版のページとコードが優先されます。

このページは、コンパイル済みアセンブリから `tests/OmsiLaunch.DocumentationTests` によって生成されます（`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`）。コードと一致しなくなると、ドキュメントのチェックが失敗します。`OmsiLaunch.Api`、`OmsiLaunch.Core`、`OmsiLaunch.Process` がエクスポートするすべての型について、その安定性とすべての public メンバーのシグネチャを記載しています。セマンティクス、前提条件、エラー、使用例は、各見出しに示したページに記載されています（既定: [公開 API](public-api.md)）。安定性の区分: `STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`（技術的な理由で public になっているもので、統合用のインターフェイスではありません）、`UNAVAILABLE`（[公開 API](public-api.md#stability-vocabulary) を参照）。コンパイラが生成する record のメンバー（`Equals`、`GetHashCode`、`ToString`、`Deconstruct`、`<Clone>$`、`EqualityContract`）は省略しています。

## `OmsiLaunch.Api`

### `Capability`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `bool Available { get; init; }` |
| プロパティ | `string EvidenceState { get; init; }` |
| プロパティ | `string Reason { get; init; }` |

### `ContentIdentity`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| プロパティ | `string Identity { get; init; }` |
| プロパティ | `string Kind { get; init; }` |
| プロパティ | `string DisplayName { get; init; }` |

### `ContentQueryKind`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Maps = 0` |
| 値 | `Situations = 1` |
| 値 | `Vehicles = 2` |
| 値 | `Repaints = 3` |
| 値 | `Hofs = 4` |
| 値 | `FleetNumbers = 5` |
| 値 | `Registrations = 6` |
| 値 | `Addons = 7` |
| 値 | `Entrypoints = 8` |
| 値 | `SessionProfiles = 9` |
| 値 | `RuntimeModules = 10` |

### `D3DDeviceState`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `NotReady = 0` |
| 値 | `Ready = 1` |
| 値 | `Lost = 2` |
| 値 | `Resetting = 3` |
| 値 | `Stopping = 4` |
| 値 | `Stopped = 5` |

### `D3DDeviceStatus`

sealed record（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| プロパティ | `bool Available { get; init; }` |
| プロパティ | `D3DDeviceState State { get; init; }` |
| プロパティ | `uint Generation { get; init; }` |
| プロパティ | `uint LiveTextureCount { get; init; }` |
| プロパティ | `bool ResetHookInstalled { get; init; }` |
| プロパティ | `uint ExecutionThreadId { get; init; }` |
| プロパティ | `uint LastResetThreadId { get; init; }` |
| プロパティ | `int QueryInterfaceHResult { get; init; }` |
| プロパティ | `int CooperativeLevelHResult { get; init; }` |
| プロパティ | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

static クラス（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 拡張メソッド | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 拡張メソッド | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 拡張メソッド | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 拡張メソッド | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 拡張メソッド | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

sealed record（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| プロパティ | `D3DTextureHandle Handle { get; init; }` |
| プロパティ | `D3DTextureResourceState State { get; init; }` |
| プロパティ | `D3DDeviceState DeviceState { get; init; }` |
| プロパティ | `uint Generation { get; init; }` |
| プロパティ | `uint Width { get; init; }` |
| プロパティ | `uint Height { get; init; }` |
| プロパティ | `D3DTextureFormat Format { get; init; }` |
| プロパティ | `uint Levels { get; init; }` |
| プロパティ | `uint Level { get; init; }` |
| プロパティ | `uint LevelWidth { get; init; }` |
| プロパティ | `uint LevelHeight { get; init; }` |
| プロパティ | `int HResult { get; init; }` |
| プロパティ | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `A8R8G8B8 = 0` |
| 値 | `X8R8G8B8 = 1` |
| 値 | `R5G6B5 = 2` |
| 値 | `X1R5G5B5 = 3` |
| 値 | `A1R5G5B5 = 4` |
| 値 | `A4R4G4B4 = 5` |
| 値 | `A8 = 6` |
| 値 | `L8 = 7` |
| 値 | `A8L8 = 8` |

### `D3DTextureHandle`

sealed record（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `D3DTextureHandle(string Value)` |
| プロパティ | `string Value { get; init; }` |

### `D3DTextureResourceState`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Live = 0` |
| 値 | `Released = 1` |
| 値 | `Stale = 2` |

### `D3DTextureUpdate`

sealed record（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| プロパティ | `uint Level { get; init; }` |
| プロパティ | `uint X { get; init; }` |
| プロパティ | `uint Y { get; init; }` |
| プロパティ | `uint Width { get; init; }` |
| プロパティ | `uint Height { get; init; }` |
| プロパティ | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| プロパティ | `DateTimeMode Mode { get; init; }` |
| プロパティ | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Unset = 0` |
| 値 | `Explicit = 1` |
| 値 | `System = 2` |

### `DiagnosticsSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| プロパティ | `bool Log { get; init; }` |
| プロパティ | `bool Verbose { get; init; }` |
| プロパティ | `bool OmsiLogAll { get; init; }` |
| プロパティ | `bool ProcessTrace { get; init; }` |
| プロパティ | `bool PluginTrace { get; init; }` |
| プロパティ | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Unset = 0` |
| 値 | `PresentedIndex = 1` |
| 値 | `Identity = 2` |

### `EntrypointSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| プロパティ | `EntrypointMode Mode { get; init; }` |
| プロパティ | `OptionalValue<int> PresentedIndex { get; init; }` |
| プロパティ | `OptionalValue<string> Identity { get; init; }` |
| static プロパティ | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Add = 0` |
| 値 | `Mirror = 1` |

### `HofManagementSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| プロパティ | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| プロパティ | `HofSourceKind SourceKind { get; init; }` |
| プロパティ | `string Source { get; init; }` |
| プロパティ | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| プロパティ | `HofApplyMode Mode { get; init; }` |
| プロパティ | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Collection = 0` |
| 値 | `Directory = 1` |
| 値 | `Vehicle = 2` |
| 値 | `File = 3` |

### `IOmsiLaunch`

インターフェイス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| メソッド | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| メソッド | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| メソッド | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| メソッド | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| プロパティ | `OptionalValue<string> KeyboardDocument { get; init; }` |
| プロパティ | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| static メソッド | `string IdentityKey(string root)` |
| static メソッド | `string NormalizeRoot(string root)` |
| static メソッド | `IReadOnlyList<string> Segments(string relativePath)` |
| static メソッド | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| プロパティ | `string RootPath { get; init; }` |
| プロパティ | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Native = 0` |
| 値 | `Disabled = 1` |
| 値 | `Override = 2` |

### `InternetTexturesSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| プロパティ | `InternetTexturesMode Mode { get; init; }` |
| プロパティ | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| プロパティ | `bool RestoreConfiguration { get; init; }` |
| プロパティ | `bool SuppressStaleClosecheckWarning { get; init; }` |
| プロパティ | `int StartupTimeoutSeconds { get; init; }` |
| プロパティ | `int ShutdownTimeoutSeconds { get; init; }` |
| プロパティ | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| プロパティ | `string Code { get; init; }` |
| プロパティ | `string Message { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| プロパティ | `InstallationSpec Installation { get; init; }` |
| プロパティ | `WorldSpec World { get; init; }` |
| プロパティ | `DateSpec Date { get; init; }` |
| プロパティ | `TimeSpec Time { get; init; }` |
| プロパティ | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| プロパティ | `EnvironmentSpec Environment { get; init; }` |
| プロパティ | `LaunchBehaviorSpec Behavior { get; init; }` |
| プロパティ | `YearSpec Year { get; init; }` |
| プロパティ | `WeatherSpec Weather { get; init; }` |
| プロパティ | `InputSpec Input { get; init; }` |
| プロパティ | `DiagnosticsSpec Diagnostics { get; init; }` |
| プロパティ | `SessionPresentationSpec Presentation { get; init; }` |
| プロパティ | `InternetTexturesSpec InternetTextures { get; init; }` |
| プロパティ | `SessionProfileMetadata SessionProfile { get; init; }` |
| プロパティ | `HofManagementSpec HofManagement { get; init; }` |
| プロパティ | `RuntimeModulesSpec Modules { get; init; }` |
| プロパティ | `HofManagementSpec EffectiveHofManagement { get; }` |
| プロパティ | `RuntimeModulesSpec EffectiveModules { get; }` |
| プロパティ | `YearSpec EffectiveYear { get; }` |
| プロパティ | `WeatherSpec EffectiveWeather { get; }` |
| プロパティ | `InputSpec EffectiveInput { get; }` |
| プロパティ | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| プロパティ | `SessionPresentationSpec EffectivePresentation { get; }` |
| プロパティ | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

sealed クラス（`OmsiLaunch.Api`）。安定性: `EXPERIMENTAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `OmsiRuntimeException(string code, string detail = null)` |
| プロパティ | `string Code { get; }` |

### `OptionalValue`

record struct（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `OptionalValue(Presence Presence, T Value)` |
| プロパティ | `Presence Presence { get; init; }` |
| プロパティ | `T Value { get; init; }` |
| プロパティ | `bool IsSet { get; }` |
| static プロパティ | `OptionalValue<T> Unset { get; }` |
| static メソッド | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| 定数 | `string Eligible = "ELIGIBLE"` |
| 定数 | `string Rejected = "REJECTED"` |
| 定数 | `string ActivationRequested = "requested"` |
| 定数 | `string ActivationSettingsFile = "settings-file"` |
| 定数 | `string ActivationContent = "content"` |
| 定数 | `string ActivationDefault = "default"` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Version { get; init; }` |
| プロパティ | `string Status { get; init; }` |
| プロパティ | `bool Requested { get; init; }` |
| プロパティ | `string FailurePolicy { get; init; }` |
| プロパティ | `IReadOnlyList<string> Operations { get; init; }` |
| プロパティ | `string Code { get; init; }` |
| プロパティ | `string Reason { get; init; }` |
| プロパティ | `string ManifestSha256 { get; init; }` |
| プロパティ | `string Activation { get; init; }` |
| プロパティ | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| プロパティ | `bool IsEligible { get; }` |
| プロパティ | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PlannedModuleSetting(string Key, string Value, string Source)` |
| プロパティ | `string Key { get; init; }` |
| プロパティ | `string Value { get; init; }` |
| プロパティ | `string Source { get; init; }` |

### `PlannedMutation`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| プロパティ | `string RelativePath { get; init; }` |
| プロパティ | `string SemanticKey { get; init; }` |
| プロパティ | `string RequestedValue { get; init; }` |
| プロパティ | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| プロパティ | `OptionalValue<string> Model { get; init; }` |
| プロパティ | `OptionalValue<string> Repaint { get; init; }` |
| プロパティ | `OptionalValue<string> Hof { get; init; }` |
| プロパティ | `OptionalValue<string> FleetNumber { get; init; }` |
| プロパティ | `OptionalValue<string> Registration { get; init; }` |
| プロパティ | `bool Enabled { get; }` |

### `Presence`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Unset = 0` |
| 値 | `Set = 1` |

### `PublicCapabilityClassification`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [capabilities](capabilities.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `PublicStableBeta = 0` |
| 値 | `PublicExperimental = 1` |
| 値 | `InternalOnly = 2` |
| 値 | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [capabilities](capabilities.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Family { get; init; }` |
| プロパティ | `PublicCapabilityClassification Classification { get; init; }` |
| プロパティ | `PublicCapabilityKind Kind { get; init; }` |
| プロパティ | `bool RequiresSession { get; init; }` |
| プロパティ | `bool RequiresExactProfile { get; init; }` |
| プロパティ | `string ApiRoute { get; init; }` |
| プロパティ | `string CliRoute { get; init; }` |
| プロパティ | `string RuntimeValidation { get; init; }` |
| プロパティ | `string Description { get; init; }` |
| プロパティ | `IReadOnlyList<string> HandleTypes { get; init; }` |
| プロパティ | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| プロパティ | `string NativeCapability { get; init; }` |
| プロパティ | `string EvidenceNote { get; init; }` |
| プロパティ | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| プロパティ | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [capabilities](capabilities.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Read = 0` |
| 値 | `Write = 1` |
| 値 | `Action = 2` |
| 値 | `Event = 3` |

### `PublicCapabilityRegistry`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [capabilities](capabilities.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string ProtocolVersion = "0.1"` |
| static フィールド | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| static プロパティ | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| static プロパティ | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| static プロパティ | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| static メソッド | `PublicCapabilityDescriptor Get(string id)` |
| static メソッド | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| static メソッド | `bool IsInternalResultKey(string key)` |
| static メソッド | `bool IsModuleOperation(string operation)` |
| static メソッド | `bool IsPublicRuntimeOperation(string operation)` |
| static メソッド | `string NativeCapabilityOf(string operation)` |
| static メソッド | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| static メソッド | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| static メソッド | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [errors](errors.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string InvalidArgument = "invalid_argument"` |
| 定数 | `string UnsupportedProfile = "unsupported_profile"` |
| 定数 | `string Session = "session"` |
| 定数 | `string Runtime = "runtime"` |
| 定数 | `string NotFound = "not_found"` |
| 定数 | `string Transaction = "transaction"` |
| 定数 | `string Internal = "internal"` |

### `PublicErrorCodes`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [errors](errors.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string CANCELLED = "OL_E_CANCELLED"` |
| 定数 | `string INTERNAL = "OL_E_INTERNAL"` |
| 定数 | `string TIMEOUT = "OL_E_TIMEOUT"` |
| 定数 | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| 定数 | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| 定数 | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| 定数 | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| 定数 | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| 定数 | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| 定数 | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| 定数 | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| 定数 | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| 定数 | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| 定数 | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| 定数 | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| 定数 | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| 定数 | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| 定数 | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| 定数 | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| 定数 | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| 定数 | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| 定数 | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| 定数 | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| 定数 | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| 定数 | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| 定数 | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| 定数 | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| 定数 | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| 定数 | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| 定数 | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| 定数 | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| 定数 | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| 定数 | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| 定数 | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| 定数 | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| 定数 | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| 定数 | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| 定数 | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| 定数 | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| 定数 | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| 定数 | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| 定数 | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| 定数 | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| 定数 | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| 定数 | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| 定数 | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| 定数 | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| 定数 | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| 定数 | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| 定数 | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| 定数 | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| 定数 | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| 定数 | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| 定数 | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| 定数 | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| 定数 | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| 定数 | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| 定数 | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| 定数 | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| 定数 | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| 定数 | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| 定数 | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| 定数 | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| 定数 | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| 定数 | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| 定数 | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| 定数 | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| 定数 | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| 定数 | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| 定数 | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| 定数 | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| 定数 | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| 定数 | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| 定数 | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| 定数 | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| 定数 | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| 定数 | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| 定数 | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| 定数 | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| 定数 | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| 定数 | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| 定数 | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| 定数 | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| 定数 | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| 定数 | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| 定数 | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| 定数 | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| 定数 | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| 定数 | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| 定数 | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| 定数 | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| 定数 | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| 定数 | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| 定数 | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| 定数 | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| 定数 | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| 定数 | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| 定数 | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| 定数 | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| 定数 | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| 定数 | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| 定数 | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| 定数 | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| 定数 | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| 定数 | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| 定数 | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| 定数 | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| 定数 | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| 定数 | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| 定数 | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| 定数 | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| 定数 | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| 定数 | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| 定数 | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| 定数 | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| 定数 | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| 定数 | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| 定数 | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| 定数 | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| 定数 | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| 定数 | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| 定数 | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| 定数 | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| 定数 | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| 定数 | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| 定数 | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| 定数 | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| 定数 | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| 定数 | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| 定数 | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| 定数 | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| 定数 | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| 定数 | `string START_SESSION = "OL_E_START_SESSION"` |
| 定数 | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| 定数 | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| 定数 | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| 定数 | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| 定数 | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| 定数 | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| 定数 | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| 定数 | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| 定数 | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| 定数 | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| 定数 | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| 定数 | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| 定数 | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| 定数 | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| 定数 | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| 定数 | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| 定数 | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| 定数 | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| 定数 | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| 定数 | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| 定数 | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| 定数 | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| 定数 | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| 定数 | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| 定数 | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| 定数 | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| 定数 | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| 定数 | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| 定数 | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| 定数 | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| 定数 | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| 定数 | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| 定数 | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| 定数 | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| 定数 | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| 定数 | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| 定数 | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| 定数 | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| 定数 | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| 定数 | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| 定数 | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| 定数 | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| 定数 | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| 定数 | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| static フィールド | `IReadOnlyList<PublicErrorDescriptor> All` |
| static メソッド | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [errors](errors.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PublicErrorDescriptor(string Code, string Category)` |
| プロパティ | `string Code { get; init; }` |
| プロパティ | `string Category { get; init; }` |

### `PublicEvidence`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| 定数 | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| 定数 | `string Partial = "PARTIAL"` |
| 定数 | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| 定数 | `string Unavailable = "UNAVAILABLE"` |
| static フィールド | `IReadOnlyList<string> All` |

### `PublicExitCode`

列挙型 (`int`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [exit-codes](exit-codes.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Success = 0` |
| 値 | `SessionFailed = 1` |
| 値 | `InvalidArguments = 2` |
| 値 | `UnsupportedProfile = 3` |
| 値 | `NoActiveSession = 4` |
| 値 | `RuntimeUnavailable = 5` |
| 値 | `NotFound = 6` |
| 値 | `OperationRejected = 7` |
| 値 | `TransactionRecoveryFailed = 8` |
| 値 | `InternalError = 10` |

### `PublicMutationPolicy`

static クラス（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string None = "none"` |
| 定数 | `string OmsiState = "omsi-state"` |
| 定数 | `string SessionResource = "session-resource"` |
| 定数 | `string SessionPolicy = "session-policy"` |
| static フィールド | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `bool Required { get; init; }` |
| プロパティ | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| プロパティ | `bool Accepted { get; init; }` |
| プロパティ | `string ErrorCode { get; init; }` |
| プロパティ | `string Message { get; init; }` |

### `PublicRuntimeOperation`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string NativeCapability { get; init; }` |
| プロパティ | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| プロパティ | `IReadOnlyList<string> CliRoutes { get; init; }` |
| プロパティ | `bool Paged { get; init; }` |
| プロパティ | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| プロパティ | `bool Pending { get; init; }` |
| プロパティ | `bool Recovered { get; init; }` |
| プロパティ | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [runtime-control](runtime-control.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| プロパティ | `Guid SessionId { get; init; }` |
| プロパティ | `ulong RequestId { get; init; }` |
| プロパティ | `string Operation { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [runtime-control](runtime-control.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| プロパティ | `Guid SessionId { get; init; }` |
| プロパティ | `ulong RequestId { get; init; }` |
| プロパティ | `bool Succeeded { get; init; }` |
| プロパティ | `string ErrorCode { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

static クラス（`OmsiLaunch.Api`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `uint Magic = 1330401859` |
| 定数 | `ushort Version = 1` |
| 定数 | `int HeaderSize = 72` |
| static メソッド | `byte[] SerializeRequest(RuntimeCommand command)` |
| static メソッド | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| static メソッド | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| static メソッド | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| static メソッド | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| プロパティ | `string Type { get; init; }` |
| プロパティ | `DateTimeOffset TimestampUtc { get; init; }` |
| プロパティ | `long Sequence { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| プロパティ | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| プロパティ | `string Key { get; init; }` |
| プロパティ | `string Type { get; init; }` |
| プロパティ | `string Default { get; init; }` |
| プロパティ | `bool AllowsAuto { get; init; }` |
| プロパティ | `string Minimum { get; init; }` |
| プロパティ | `string Maximum { get; init; }` |
| プロパティ | `IReadOnlyList<string> Choices { get; init; }` |
| プロパティ | `string Flag { get; init; }` |
| プロパティ | `string Description { get; init; }` |

### `RuntimeModuleSummary`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Version { get; init; }` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `string Activation { get; init; }` |
| プロパティ | `bool IsValid { get; init; }` |
| プロパティ | `IReadOnlyList<string> Operations { get; init; }` |
| プロパティ | `string ContentBindingMap { get; init; }` |
| プロパティ | `string ErrorCode { get; init; }` |
| プロパティ | `string ErrorMessage { get; init; }` |
| プロパティ | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| プロパティ | `IReadOnlyList<string> Enable { get; init; }` |
| プロパティ | `IReadOnlyList<string> Disable { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| プロパティ | `IReadOnlyList<string> EffectiveDisable { get; }` |
| プロパティ | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| プロパティ | `string OsFamily { get; init; }` |
| プロパティ | `string OsVersion { get; init; }` |
| プロパティ | `string OsArchitecture { get; init; }` |
| プロパティ | `string HostArchitecture { get; init; }` |
| プロパティ | `string OmsiArchitecture { get; init; }` |
| プロパティ | `string PluginArchitecture { get; init; }` |
| プロパティ | `bool CurrentPlatformSupported { get; init; }` |
| プロパティ | `bool LegacyPlatform { get; init; }` |
| プロパティ | `bool Wow64Available { get; init; }` |
| プロパティ | `bool InstallationWritable { get; init; }` |
| プロパティ | `bool ProcessLaunchSupported { get; init; }` |
| プロパティ | `bool PluginRuntimeSupported { get; init; }` |
| プロパティ | `bool NativeInteropSupported { get; init; }` |
| プロパティ | `bool SharedMemorySupported { get; init; }` |
| プロパティ | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SemanticDate(int Year, int Month, int Day)` |
| プロパティ | `int Year { get; init; }` |
| プロパティ | `int Month { get; init; }` |
| プロパティ | `int Day { get; init; }` |

### `SemanticTime`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SemanticTime(int Hour, int Minute, int Second)` |
| プロパティ | `int Hour { get; init; }` |
| プロパティ | `int Minute { get; init; }` |
| プロパティ | `int Second { get; init; }` |

### `SessionHandle`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionHandle(Guid SessionId)` |
| プロパティ | `Guid SessionId { get; init; }` |

### `SessionPlan`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| プロパティ | `Guid SessionId { get; init; }` |
| プロパティ | `string BuildProfileId { get; init; }` |
| プロパティ | `LaunchSpec Spec { get; init; }` |
| プロパティ | `RuntimePlatformInfo Platform { get; init; }` |
| プロパティ | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| プロパティ | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| プロパティ | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| プロパティ | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| プロパティ | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| プロパティ | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| プロパティ | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| プロパティ | `bool IsRunnable { get; init; }` |
| プロパティ | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| プロパティ | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| プロパティ | `SplashMode Splash { get; init; }` |
| プロパティ | `OptionalValue<string> Language { get; init; }` |
| プロパティ | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| プロパティ | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `string Version { get; init; }` |
| プロパティ | `string Author { get; init; }` |
| プロパティ | `string PresetId { get; init; }` |
| プロパティ | `int PresetIndex { get; init; }` |
| プロパティ | `string PresetName { get; init; }` |
| プロパティ | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| プロパティ | `int Index { get; init; }` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Name { get; init; }` |

### `SessionProfileSummary`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `string Version { get; init; }` |
| プロパティ | `string Author { get; init; }` |
| プロパティ | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| プロパティ | `string MapIdentity { get; init; }` |
| プロパティ | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| プロパティ | `bool IsValid { get; init; }` |
| プロパティ | `string ErrorCode { get; init; }` |
| プロパティ | `string ErrorMessage { get; init; }` |

### `SessionState`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Created = 0` |
| 値 | `ValidatingPlatform = 1` |
| 値 | `Planning = 2` |
| 値 | `AcquiringInstallationLock = 3` |
| 値 | `RecoveringPreviousTransaction = 4` |
| 値 | `Snapshotting = 5` |
| 値 | `ApplyingConfiguration = 6` |
| 値 | `DeployingRuntime = 7` |
| 値 | `CreatingStartupHandoff = 8` |
| 値 | `StartingProcess = 9` |
| 値 | `WaitingForPlugin = 10` |
| 値 | `PluginBootstrap = 11` |
| 値 | `StartingWorld = 12` |
| 値 | `EnteringGameplay = 13` |
| 値 | `Running = 14` |
| 値 | `ProcessExited = 15` |
| 値 | `Restoring = 16` |
| 値 | `CleaningRuntime = 17` |
| 値 | `Completed = 18` |
| 値 | `Failed = 19` |

### `SessionStatus`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| プロパティ | `Guid SessionId { get; init; }` |
| プロパティ | `SessionState State { get; init; }` |
| プロパティ | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| プロパティ | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Unset = 0` |
| 値 | `Native = 0` |
| 値 | `Managed = 1` |

### `StartDateTime`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| プロパティ | `int? Year { get; init; }` |
| プロパティ | `int? DayOfYear { get; init; }` |
| プロパティ | `int? Hour { get; init; }` |
| プロパティ | `int? Minute { get; init; }` |
| プロパティ | `int? Second { get; init; }` |
| プロパティ | `bool IsEmpty { get; }` |

### `StartWeather`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| プロパティ | `WeatherMode Mode { get; init; }` |
| プロパティ | `string Preset { get; init; }` |
| プロパティ | `string Icao { get; init; }` |

### `StartupHandoff`

sealed record（`OmsiLaunch.Api`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| プロパティ | `Guid SessionId { get; init; }` |
| プロパティ | `string BuildProfileId { get; init; }` |
| プロパティ | `WorldMode WorldMode { get; init; }` |
| プロパティ | `string MapIdentity { get; init; }` |
| プロパティ | `int PresentedEntrypointIndex { get; init; }` |
| プロパティ | `bool HeadlessStart { get; init; }` |
| プロパティ | `bool PlayerVehicleEnabled { get; init; }` |
| プロパティ | `DateTimeMode DateMode { get; init; }` |
| プロパティ | `DateTimeMode TimeMode { get; init; }` |
| プロパティ | `string EntrypointIdentity { get; init; }` |
| プロパティ | `string SituationIdentity { get; init; }` |
| プロパティ | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| プロパティ | `bool PluginTrace { get; init; }` |
| プロパティ | `bool NativeTrace { get; init; }` |
| プロパティ | `StartDateTime Start { get; init; }` |
| プロパティ | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

static クラス（`OmsiLaunch.Api`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `uint Magic = 1330402120` |
| 定数 | `ushort Version = 7` |
| static メソッド | `byte[] Serialize(StartupHandoff value)` |
| static メソッド | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| プロパティ | `string RelativeDirectory { get; init; }` |
| プロパティ | `string ManifestSha256 { get; init; }` |
| プロパティ | `string Settings { get; init; }` |

### `TelemetryRingWire`

static クラス（`OmsiLaunch.Api`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `uint Magic = 1381256271` |
| 定数 | `int Version = 1` |
| 定数 | `int SlotCount = 64` |
| 定数 | `int SlotSize = 2048` |
| 定数 | `int HeaderSize = 64` |
| 定数 | `int Capacity = 131136` |
| 定数 | `int MaxPayload = 2036` |
| static メソッド | `void Initialize(MemoryMappedViewAccessor view)` |
| static メソッド | `bool IsRing(MemoryMappedViewAccessor view)` |
| static メソッド | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| static メソッド | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| プロパティ | `DateTimeMode Mode { get; init; }` |
| プロパティ | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

列挙型 (`byte`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `Unset = 0` |
| 値 | `Preset = 1` |
| 値 | `Icao = 2` |
| 値 | `RealCurrent = 3` |

### `WeatherSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| プロパティ | `WeatherMode Mode { get; init; }` |
| プロパティ | `OptionalValue<string> Preset { get; init; }` |
| プロパティ | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

列挙型 (`int`)（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| 値 | `NewMap = 0` |
| 値 | `SavedSituation = 1` |
| 値 | `LastMapState = 2` |
| 値 | `LastSituation = 2` |

### `WorldSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `STABLE_BETA`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| プロパティ | `WorldMode Mode { get; init; }` |
| プロパティ | `OptionalValue<string> MapIdentity { get; init; }` |
| プロパティ | `OptionalValue<string> SituationIdentity { get; init; }` |
| プロパティ | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| プロパティ | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| プロパティ | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

sealed record（`OmsiLaunch.Api`）。安定性: `PARTIAL`。セマンティクス: [launchspec](launchspec.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| プロパティ | `DateTimeMode Mode { get; init; }` |
| プロパティ | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

sealed クラス（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `HofPlanningException(string code, string message)` |
| プロパティ | `string Code { get; }` |

### `LaunchValidation`

static クラス（`OmsiLaunch.Core`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| static メソッド | `bool IsMapIdentity(string value)` |
| static メソッド | `bool IsSha256(string value)` |
| static メソッド | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

static クラス（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| static メソッド | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

sealed record（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| プロパティ | `string PluginBuildDirectory { get; init; }` |
| プロパティ | `string NativeBridgePath { get; init; }` |
| プロパティ | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

sealed クラス（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| メソッド | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| メソッド | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| メソッド | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| メソッド | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| メソッド | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

sealed record（`OmsiLaunch.Core`）。安定性: `EXPERIMENTAL`。セマンティクス: [session-profiles](session-profiles.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| プロパティ | `string Map { get; init; }` |
| プロパティ | `int? EntrypointIndex { get; init; }` |
| プロパティ | `string EntrypointIdentity { get; init; }` |
| プロパティ | `SemanticDate Date { get; init; }` |
| プロパティ | `SemanticTime Time { get; init; }` |
| プロパティ | `int? Year { get; init; }` |
| プロパティ | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

sealed record（`OmsiLaunch.Core`）。安定性: `EXPERIMENTAL`。セマンティクス: [session-profiles](session-profiles.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| プロパティ | `int Index { get; init; }` |
| プロパティ | `string Id { get; init; }` |
| プロパティ | `string Name { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| プロパティ | `SessionPresentationSpec Presentation { get; init; }` |
| プロパティ | `InternetTexturesSpec InternetTextures { get; init; }` |
| プロパティ | `LaunchBehaviorSpec Behavior { get; init; }` |
| プロパティ | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| プロパティ | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

sealed クラス（`OmsiLaunch.Core`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionPlanner(IRuntimePlatform platform)` |
| メソッド | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

static クラス（`OmsiLaunch.Core`）。安定性: `EXPERIMENTAL`。セマンティクス: [session-profiles](session-profiles.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string Schema = "omsilaunch.session-profile/v1"` |
| 定数 | `long MaxBytes = 262144` |
| 定数 | `int MinPresetIndex = 1` |
| 定数 | `int MaxPresetIndex = 99` |
| static フィールド | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| static メソッド | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| static メソッド | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| static メソッド | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| static メソッド | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

sealed クラス（`OmsiLaunch.Core`）。安定性: `EXPERIMENTAL`。セマンティクス: [session-profiles](session-profiles.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionProfileException(string code, string message)` |
| プロパティ | `string Code { get; }` |

### `SessionProfilePackage`

sealed record（`OmsiLaunch.Core`）。安定性: `EXPERIMENTAL`。セマンティクス: [session-profiles](session-profiles.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| プロパティ | `string RootPath { get; init; }` |
| プロパティ | `SessionProfileMetadata Metadata { get; init; }` |
| プロパティ | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| プロパティ | `ProfileNew New { get; init; }` |
| プロパティ | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

static クラス（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `int MinimumYear = 1900` |
| 定数 | `int MaximumYear = 2200` |
| static メソッド | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

static クラス（`OmsiLaunch.Core`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| static メソッド | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| プロパティ | `string Name { get; }` |
| プロパティ | `Guid SessionId { get; }` |
| static メソッド | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| メソッド | `void Dispose()` |
| メソッド | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| プロパティ | `string Name { get; }` |
| プロパティ | `Guid SessionId { get; }` |
| static メソッド | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| メソッド | `void Dispose()` |

### `CurrentTelemetryStore`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| プロパティ | `string Name { get; }` |
| static メソッド | `CurrentTelemetryStore Create(Guid sessionId)` |
| メソッド | `void Dispose()` |
| メソッド | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

sealed クラス（`OmsiLaunch.Process`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `CurrentWindowsX64Platform()` |
| メソッド | `RuntimePlatformInfo Detect(string root)` |
| メソッド | `bool HasExited(LaunchedProcess p)` |
| メソッド | `bool IsInstallationWritable(string root)` |
| メソッド | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| メソッド | `void Terminate(LaunchedProcess p)` |
| メソッド | `void ValidateCurrent(RuntimePlatformInfo p)` |
| メソッド | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

インターフェイス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| メソッド | `OmsiProcessState Observe()` |
| メソッド | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| メソッド | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

インターフェイス（`OmsiLaunch.Process`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| メソッド | `RuntimePlatformInfo Detect(string installationRoot)` |
| メソッド | `bool HasExited(LaunchedProcess process)` |
| メソッド | `bool IsInstallationWritable(string installationRoot)` |
| メソッド | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| メソッド | `void Terminate(LaunchedProcess process)` |
| メソッド | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| メソッド | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| static メソッド | `InstallationLease Acquire(string installationRoot)` |
| メソッド | `void Dispose()` |
| static メソッド | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| プロパティ | `int ProcessId { get; }` |
| プロパティ | `int ThreadId { get; }` |
| プロパティ | `ProcessIdentity Identity { get; }` |
| メソッド | `void Dispose()` |

### `ModuleSessionProcess`

sealed クラス（`OmsiLaunch.Process`）。安定性: `STABLE_BETA`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `int MaxLineBytes = 65536` |
| プロパティ | `int ProcessId { get; }` |
| プロパティ | `bool HasExited { get; }` |
| プロパティ | `int? ExitCode { get; }` |
| メソッド | `void Dispose()` |
| メソッド | `void Kill()` |
| static メソッド | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| メソッド | `bool TryWriteLine(string line)` |
| メソッド | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

sealed record（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| プロパティ | `string State { get; init; }` |
| プロパティ | `int? ProcessId { get; init; }` |
| プロパティ | `bool Responding { get; init; }` |

### `ProcessIdentity`

sealed record（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| プロパティ | `int ProcessId { get; init; }` |
| プロパティ | `DateTimeOffset CreationTimeUtc { get; init; }` |
| プロパティ | `string ExecutablePath { get; init; }` |
| プロパティ | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

static クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| 定数 | `string FileName = "release-manifest.json"` |
| static メソッド | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| static メソッド | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

sealed record（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| プロパティ | `string SourcePath { get; init; }` |
| プロパティ | `string DestinationRelativePath { get; init; }` |
| プロパティ | `string Sha256 { get; init; }` |
| プロパティ | `long Size { get; init; }` |

### `RuntimeArtifactSet`

sealed クラス（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| プロパティ | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| プロパティ | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| プロパティ | `string IntegrityReference { get; }` |
| static メソッド | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| メソッド | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

sealed record（`OmsiLaunch.Process`）。安定性: `INTERNAL`。セマンティクス: [public-api](public-api.md).

| メンバー | シグネチャ |
| --- | --- |
| コンストラクター | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| プロパティ | `string ExecutablePath { get; init; }` |
| プロパティ | `string WorkingDirectory { get; init; }` |
| プロパティ | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
