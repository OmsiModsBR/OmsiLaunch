# Public API Inventory

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> British English edition of the [canonical page](../../../reference/public-api-inventory.md) for OmsiLaunch 0.1.0-beta4. The US English page is normative: where the two differ, it and the code take precedence.

This page is generated from the compiled assemblies by `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); the documentation gate fails when it no longer matches the code. It lists every exported type of `OmsiLaunch.Api`, `OmsiLaunch.Core` and `OmsiLaunch.Process` with its stability and the signature of every public member. Semantics, preconditions, errors and examples are documented on the page named in each heading (default: [public API](public-api.md)). Stability vocabulary: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (public for technical reasons, not an integration surface), `UNAVAILABLE` (see [public API](public-api.md#stability-vocabulary)). Compiler-generated record members (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) are omitted.

## `OmsiLaunch.Api`

### `Capability`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| property | `string Name { get; init; }` |
| property | `bool Available { get; init; }` |
| property | `string EvidenceState { get; init; }` |
| property | `string Reason { get; init; }` |

### `ContentIdentity`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| property | `string Identity { get; init; }` |
| property | `string Kind { get; init; }` |
| property | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `Maps = 0` |
| value | `Situations = 1` |
| value | `Vehicles = 2` |
| value | `Repaints = 3` |
| value | `Hofs = 4` |
| value | `FleetNumbers = 5` |
| value | `Registrations = 6` |
| value | `Addons = 7` |
| value | `Entrypoints = 8` |
| value | `SessionProfiles = 9` |
| value | `RuntimeModules = 10` |

### `D3DDeviceState`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `NotReady = 0` |
| value | `Ready = 1` |
| value | `Lost = 2` |
| value | `Resetting = 3` |
| value | `Stopping = 4` |
| value | `Stopped = 5` |

### `D3DDeviceStatus`

Sealed record in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| property | `bool Available { get; init; }` |
| property | `D3DDeviceState State { get; init; }` |
| property | `uint Generation { get; init; }` |
| property | `uint LiveTextureCount { get; init; }` |
| property | `bool ResetHookInstalled { get; init; }` |
| property | `uint ExecutionThreadId { get; init; }` |
| property | `uint LastResetThreadId { get; init; }` |
| property | `int QueryInterfaceHResult { get; init; }` |
| property | `int CooperativeLevelHResult { get; init; }` |
| property | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Static class in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| extension method | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extension method | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extension method | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| extension method | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extension method | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Sealed record in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| property | `D3DTextureHandle Handle { get; init; }` |
| property | `D3DTextureResourceState State { get; init; }` |
| property | `D3DDeviceState DeviceState { get; init; }` |
| property | `uint Generation { get; init; }` |
| property | `uint Width { get; init; }` |
| property | `uint Height { get; init; }` |
| property | `D3DTextureFormat Format { get; init; }` |
| property | `uint Levels { get; init; }` |
| property | `uint Level { get; init; }` |
| property | `uint LevelWidth { get; init; }` |
| property | `uint LevelHeight { get; init; }` |
| property | `int HResult { get; init; }` |
| property | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `A8R8G8B8 = 0` |
| value | `X8R8G8B8 = 1` |
| value | `R5G6B5 = 2` |
| value | `X1R5G5B5 = 3` |
| value | `A1R5G5B5 = 4` |
| value | `A4R4G4B4 = 5` |
| value | `A8 = 6` |
| value | `L8 = 7` |
| value | `A8L8 = 8` |

### `D3DTextureHandle`

Sealed record in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `D3DTextureHandle(string Value)` |
| property | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `Live = 0` |
| value | `Released = 1` |
| value | `Stale = 2` |

### `D3DTextureUpdate`

Sealed record in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| property | `uint Level { get; init; }` |
| property | `uint X { get; init; }` |
| property | `uint Y { get; init; }` |
| property | `uint Width { get; init; }` |
| property | `uint Height { get; init; }` |
| property | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| property | `DateTimeMode Mode { get; init; }` |
| property | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Unset = 0` |
| value | `Explicit = 1` |
| value | `System = 2` |

### `DiagnosticsSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| property | `bool Log { get; init; }` |
| property | `bool Verbose { get; init; }` |
| property | `bool OmsiLogAll { get; init; }` |
| property | `bool ProcessTrace { get; init; }` |
| property | `bool PluginTrace { get; init; }` |
| property | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Unset = 0` |
| value | `PresentedIndex = 1` |
| value | `Identity = 2` |

### `EntrypointSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| property | `EntrypointMode Mode { get; init; }` |
| property | `OptionalValue<int> PresentedIndex { get; init; }` |
| property | `OptionalValue<string> Identity { get; init; }` |
| static property | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| property | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `Add = 0` |
| value | `Mirror = 1` |

### `HofManagementSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| property | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| property | `HofSourceKind SourceKind { get; init; }` |
| property | `string Source { get; init; }` |
| property | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| property | `HofApplyMode Mode { get; init; }` |
| property | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `Collection = 0` |
| value | `Directory = 1` |
| value | `Vehicle = 2` |
| value | `File = 3` |

### `IOmsiLaunch`

Interface in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| method | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| method | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| method | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| method | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| method | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| property | `OptionalValue<string> KeyboardDocument { get; init; }` |
| property | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| static method | `string IdentityKey(string root)` |
| static method | `string NormalizeRoot(string root)` |
| static method | `IReadOnlyList<string> Segments(string relativePath)` |
| static method | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| property | `string RootPath { get; init; }` |
| property | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Native = 0` |
| value | `Disabled = 1` |
| value | `Override = 2` |

### `InternetTexturesSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| property | `InternetTexturesMode Mode { get; init; }` |
| property | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| property | `bool RestoreConfiguration { get; init; }` |
| property | `bool SuppressStaleClosecheckWarning { get; init; }` |
| property | `int StartupTimeoutSeconds { get; init; }` |
| property | `int ShutdownTimeoutSeconds { get; init; }` |
| property | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| property | `string Code { get; init; }` |
| property | `string Message { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| property | `InstallationSpec Installation { get; init; }` |
| property | `WorldSpec World { get; init; }` |
| property | `DateSpec Date { get; init; }` |
| property | `TimeSpec Time { get; init; }` |
| property | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| property | `EnvironmentSpec Environment { get; init; }` |
| property | `LaunchBehaviorSpec Behavior { get; init; }` |
| property | `YearSpec Year { get; init; }` |
| property | `WeatherSpec Weather { get; init; }` |
| property | `InputSpec Input { get; init; }` |
| property | `DiagnosticsSpec Diagnostics { get; init; }` |
| property | `SessionPresentationSpec Presentation { get; init; }` |
| property | `InternetTexturesSpec InternetTextures { get; init; }` |
| property | `SessionProfileMetadata SessionProfile { get; init; }` |
| property | `HofManagementSpec HofManagement { get; init; }` |
| property | `RuntimeModulesSpec Modules { get; init; }` |
| property | `HofManagementSpec EffectiveHofManagement { get; }` |
| property | `RuntimeModulesSpec EffectiveModules { get; }` |
| property | `YearSpec EffectiveYear { get; }` |
| property | `WeatherSpec EffectiveWeather { get; }` |
| property | `InputSpec EffectiveInput { get; }` |
| property | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| property | `SessionPresentationSpec EffectivePresentation { get; }` |
| property | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Sealed class in `OmsiLaunch.Api`. Stability: `EXPERIMENTAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `OmsiRuntimeException(string code, string detail = null)` |
| property | `string Code { get; }` |

### `OptionalValue`

Record struct in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `OptionalValue(Presence Presence, T Value)` |
| property | `Presence Presence { get; init; }` |
| property | `T Value { get; init; }` |
| property | `bool IsSet { get; }` |
| static property | `OptionalValue<T> Unset { get; }` |
| static method | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| constant | `string Eligible = "ELIGIBLE"` |
| constant | `string Rejected = "REJECTED"` |
| constant | `string ActivationRequested = "requested"` |
| constant | `string ActivationSettingsFile = "settings-file"` |
| constant | `string ActivationContent = "content"` |
| constant | `string ActivationDefault = "default"` |
| property | `string Id { get; init; }` |
| property | `string Version { get; init; }` |
| property | `string Status { get; init; }` |
| property | `bool Requested { get; init; }` |
| property | `string FailurePolicy { get; init; }` |
| property | `IReadOnlyList<string> Operations { get; init; }` |
| property | `string Code { get; init; }` |
| property | `string Reason { get; init; }` |
| property | `string ManifestSha256 { get; init; }` |
| property | `string Activation { get; init; }` |
| property | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| property | `bool IsEligible { get; }` |
| property | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| property | `string Key { get; init; }` |
| property | `string Value { get; init; }` |
| property | `string Source { get; init; }` |

### `PlannedMutation`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| property | `string RelativePath { get; init; }` |
| property | `string SemanticKey { get; init; }` |
| property | `string RequestedValue { get; init; }` |
| property | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| property | `OptionalValue<string> Model { get; init; }` |
| property | `OptionalValue<string> Repaint { get; init; }` |
| property | `OptionalValue<string> Hof { get; init; }` |
| property | `OptionalValue<string> FleetNumber { get; init; }` |
| property | `OptionalValue<string> Registration { get; init; }` |
| property | `bool Enabled { get; }` |

### `Presence`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Unset = 0` |
| value | `Set = 1` |

### `PublicCapabilityClassification`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [capabilities](capabilities.md).

| Member | Signature |
| --- | --- |
| value | `PublicStableBeta = 0` |
| value | `PublicExperimental = 1` |
| value | `InternalOnly = 2` |
| value | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [capabilities](capabilities.md).

| Member | Signature |
| --- | --- |
| constructor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| property | `string Id { get; init; }` |
| property | `string Family { get; init; }` |
| property | `PublicCapabilityClassification Classification { get; init; }` |
| property | `PublicCapabilityKind Kind { get; init; }` |
| property | `bool RequiresSession { get; init; }` |
| property | `bool RequiresExactProfile { get; init; }` |
| property | `string ApiRoute { get; init; }` |
| property | `string CliRoute { get; init; }` |
| property | `string RuntimeValidation { get; init; }` |
| property | `string Description { get; init; }` |
| property | `IReadOnlyList<string> HandleTypes { get; init; }` |
| property | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| property | `string NativeCapability { get; init; }` |
| property | `string EvidenceNote { get; init; }` |
| property | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| property | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [capabilities](capabilities.md).

| Member | Signature |
| --- | --- |
| value | `Read = 0` |
| value | `Write = 1` |
| value | `Action = 2` |
| value | `Event = 3` |

### `PublicCapabilityRegistry`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [capabilities](capabilities.md).

| Member | Signature |
| --- | --- |
| constant | `string ProtocolVersion = "0.1"` |
| static field | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| static property | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| static property | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| static property | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| static method | `PublicCapabilityDescriptor Get(string id)` |
| static method | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| static method | `bool IsInternalResultKey(string key)` |
| static method | `bool IsModuleOperation(string operation)` |
| static method | `bool IsPublicRuntimeOperation(string operation)` |
| static method | `string NativeCapabilityOf(string operation)` |
| static method | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| static method | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| static method | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [errors](errors.md).

| Member | Signature |
| --- | --- |
| constant | `string InvalidArgument = "invalid_argument"` |
| constant | `string UnsupportedProfile = "unsupported_profile"` |
| constant | `string Session = "session"` |
| constant | `string Runtime = "runtime"` |
| constant | `string NotFound = "not_found"` |
| constant | `string Transaction = "transaction"` |
| constant | `string Internal = "internal"` |

### `PublicErrorCodes`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [errors](errors.md).

| Member | Signature |
| --- | --- |
| constant | `string CANCELLED = "OL_E_CANCELLED"` |
| constant | `string INTERNAL = "OL_E_INTERNAL"` |
| constant | `string TIMEOUT = "OL_E_TIMEOUT"` |
| constant | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| constant | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| constant | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| constant | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| constant | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| constant | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| constant | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| constant | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| constant | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| constant | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| constant | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| constant | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| constant | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| constant | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| constant | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| constant | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| constant | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| constant | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| constant | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| constant | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| constant | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| constant | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| constant | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| constant | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| constant | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| constant | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| constant | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| constant | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| constant | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| constant | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| constant | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| constant | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| constant | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| constant | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| constant | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| constant | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| constant | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| constant | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| constant | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| constant | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| constant | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| constant | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| constant | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| constant | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| constant | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| constant | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| constant | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| constant | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| constant | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| constant | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| constant | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| constant | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| constant | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| constant | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| constant | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| constant | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| constant | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| constant | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| constant | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| constant | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| constant | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| constant | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| constant | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| constant | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| constant | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| constant | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| constant | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| constant | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| constant | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| constant | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| constant | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| constant | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| constant | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| constant | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| constant | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| constant | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| constant | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| constant | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| constant | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| constant | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| constant | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| constant | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| constant | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| constant | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| constant | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| constant | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| constant | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| constant | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| constant | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| constant | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| constant | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| constant | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| constant | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| constant | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| constant | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| constant | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| constant | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| constant | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| constant | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| constant | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| constant | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| constant | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| constant | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| constant | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| constant | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| constant | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| constant | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| constant | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| constant | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| constant | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| constant | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| constant | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| constant | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| constant | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| constant | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| constant | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| constant | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| constant | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| constant | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| constant | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| constant | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| constant | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| constant | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| constant | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| constant | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| constant | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| constant | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| constant | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| constant | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| constant | `string START_SESSION = "OL_E_START_SESSION"` |
| constant | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| constant | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| constant | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| constant | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| constant | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| constant | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| constant | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| constant | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| constant | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| constant | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| constant | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| constant | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| constant | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| constant | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| constant | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| constant | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| constant | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| constant | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| constant | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| constant | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| constant | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| constant | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| constant | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| constant | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| constant | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| constant | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| constant | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| constant | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| constant | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| constant | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| constant | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| constant | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| constant | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| constant | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| constant | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| constant | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| constant | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| constant | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| constant | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| constant | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| constant | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| constant | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| constant | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| constant | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| static field | `IReadOnlyList<PublicErrorDescriptor> All` |
| static method | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [errors](errors.md).

| Member | Signature |
| --- | --- |
| constructor | `PublicErrorDescriptor(string Code, string Category)` |
| property | `string Code { get; init; }` |
| property | `string Category { get; init; }` |

### `PublicEvidence`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| constant | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| constant | `string Partial = "PARTIAL"` |
| constant | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| constant | `string Unavailable = "UNAVAILABLE"` |
| static field | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enum (`int`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [exit-codes](exit-codes.md).

| Member | Signature |
| --- | --- |
| value | `Success = 0` |
| value | `SessionFailed = 1` |
| value | `InvalidArguments = 2` |
| value | `UnsupportedProfile = 3` |
| value | `NoActiveSession = 4` |
| value | `RuntimeUnavailable = 5` |
| value | `NotFound = 6` |
| value | `OperationRejected = 7` |
| value | `TransactionRecoveryFailed = 8` |
| value | `InternalError = 10` |

### `PublicMutationPolicy`

Static class in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `string None = "none"` |
| constant | `string OmsiState = "omsi-state"` |
| constant | `string SessionResource = "session-resource"` |
| constant | `string SessionPolicy = "session-policy"` |
| static field | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| property | `string Name { get; init; }` |
| property | `bool Required { get; init; }` |
| property | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| property | `bool Accepted { get; init; }` |
| property | `string ErrorCode { get; init; }` |
| property | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| property | `string Id { get; init; }` |
| property | `string NativeCapability { get; init; }` |
| property | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| property | `IReadOnlyList<string> CliRoutes { get; init; }` |
| property | `bool Paged { get; init; }` |
| property | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| property | `bool Pending { get; init; }` |
| property | `bool Recovered { get; init; }` |
| property | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [runtime-control](runtime-control.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| property | `Guid SessionId { get; init; }` |
| property | `ulong RequestId { get; init; }` |
| property | `string Operation { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [runtime-control](runtime-control.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| property | `Guid SessionId { get; init; }` |
| property | `ulong RequestId { get; init; }` |
| property | `bool Succeeded { get; init; }` |
| property | `string ErrorCode { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Static class in `OmsiLaunch.Api`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `uint Magic = 1330401859` |
| constant | `ushort Version = 1` |
| constant | `int HeaderSize = 72` |
| static method | `byte[] SerializeRequest(RuntimeCommand command)` |
| static method | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| static method | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| static method | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| static method | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| property | `string Type { get; init; }` |
| property | `DateTimeOffset TimestampUtc { get; init; }` |
| property | `long Sequence { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| property | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| property | `string Key { get; init; }` |
| property | `string Type { get; init; }` |
| property | `string Default { get; init; }` |
| property | `bool AllowsAuto { get; init; }` |
| property | `string Minimum { get; init; }` |
| property | `string Maximum { get; init; }` |
| property | `IReadOnlyList<string> Choices { get; init; }` |
| property | `string Flag { get; init; }` |
| property | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| property | `string Id { get; init; }` |
| property | `string Version { get; init; }` |
| property | `string Name { get; init; }` |
| property | `string Activation { get; init; }` |
| property | `bool IsValid { get; init; }` |
| property | `IReadOnlyList<string> Operations { get; init; }` |
| property | `string ContentBindingMap { get; init; }` |
| property | `string ErrorCode { get; init; }` |
| property | `string ErrorMessage { get; init; }` |
| property | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| property | `IReadOnlyList<string> Enable { get; init; }` |
| property | `IReadOnlyList<string> Disable { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| property | `IReadOnlyList<string> EffectiveDisable { get; }` |
| property | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| property | `string OsFamily { get; init; }` |
| property | `string OsVersion { get; init; }` |
| property | `string OsArchitecture { get; init; }` |
| property | `string HostArchitecture { get; init; }` |
| property | `string OmsiArchitecture { get; init; }` |
| property | `string PluginArchitecture { get; init; }` |
| property | `bool CurrentPlatformSupported { get; init; }` |
| property | `bool LegacyPlatform { get; init; }` |
| property | `bool Wow64Available { get; init; }` |
| property | `bool InstallationWritable { get; init; }` |
| property | `bool ProcessLaunchSupported { get; init; }` |
| property | `bool PluginRuntimeSupported { get; init; }` |
| property | `bool NativeInteropSupported { get; init; }` |
| property | `bool SharedMemorySupported { get; init; }` |
| property | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `SemanticDate(int Year, int Month, int Day)` |
| property | `int Year { get; init; }` |
| property | `int Month { get; init; }` |
| property | `int Day { get; init; }` |

### `SemanticTime`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `SemanticTime(int Hour, int Minute, int Second)` |
| property | `int Hour { get; init; }` |
| property | `int Minute { get; init; }` |
| property | `int Second { get; init; }` |

### `SessionHandle`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionHandle(Guid SessionId)` |
| property | `Guid SessionId { get; init; }` |

### `SessionPlan`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| property | `Guid SessionId { get; init; }` |
| property | `string BuildProfileId { get; init; }` |
| property | `LaunchSpec Spec { get; init; }` |
| property | `RuntimePlatformInfo Platform { get; init; }` |
| property | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| property | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| property | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| property | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| property | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| property | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| property | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| property | `bool IsRunnable { get; init; }` |
| property | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| property | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| property | `SplashMode Splash { get; init; }` |
| property | `OptionalValue<string> Language { get; init; }` |
| property | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| property | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| property | `string Id { get; init; }` |
| property | `string Name { get; init; }` |
| property | `string Version { get; init; }` |
| property | `string Author { get; init; }` |
| property | `string PresetId { get; init; }` |
| property | `int PresetIndex { get; init; }` |
| property | `string PresetName { get; init; }` |
| property | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| property | `int Index { get; init; }` |
| property | `string Id { get; init; }` |
| property | `string Name { get; init; }` |

### `SessionProfileSummary`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| property | `string Id { get; init; }` |
| property | `string Name { get; init; }` |
| property | `string Version { get; init; }` |
| property | `string Author { get; init; }` |
| property | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| property | `string MapIdentity { get; init; }` |
| property | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| property | `bool IsValid { get; init; }` |
| property | `string ErrorCode { get; init; }` |
| property | `string ErrorMessage { get; init; }` |

### `SessionState`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| value | `Created = 0` |
| value | `ValidatingPlatform = 1` |
| value | `Planning = 2` |
| value | `AcquiringInstallationLock = 3` |
| value | `RecoveringPreviousTransaction = 4` |
| value | `Snapshotting = 5` |
| value | `ApplyingConfiguration = 6` |
| value | `DeployingRuntime = 7` |
| value | `CreatingStartupHandoff = 8` |
| value | `StartingProcess = 9` |
| value | `WaitingForPlugin = 10` |
| value | `PluginBootstrap = 11` |
| value | `StartingWorld = 12` |
| value | `EnteringGameplay = 13` |
| value | `Running = 14` |
| value | `ProcessExited = 15` |
| value | `Restoring = 16` |
| value | `CleaningRuntime = 17` |
| value | `Completed = 18` |
| value | `Failed = 19` |

### `SessionStatus`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| property | `Guid SessionId { get; init; }` |
| property | `SessionState State { get; init; }` |
| property | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| property | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Unset = 0` |
| value | `Native = 0` |
| value | `Managed = 1` |

### `StartDateTime`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| property | `int? Year { get; init; }` |
| property | `int? DayOfYear { get; init; }` |
| property | `int? Hour { get; init; }` |
| property | `int? Minute { get; init; }` |
| property | `int? Second { get; init; }` |
| property | `bool IsEmpty { get; }` |

### `StartWeather`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| property | `WeatherMode Mode { get; init; }` |
| property | `string Preset { get; init; }` |
| property | `string Icao { get; init; }` |

### `StartupHandoff`

Sealed record in `OmsiLaunch.Api`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| property | `Guid SessionId { get; init; }` |
| property | `string BuildProfileId { get; init; }` |
| property | `WorldMode WorldMode { get; init; }` |
| property | `string MapIdentity { get; init; }` |
| property | `int PresentedEntrypointIndex { get; init; }` |
| property | `bool HeadlessStart { get; init; }` |
| property | `bool PlayerVehicleEnabled { get; init; }` |
| property | `DateTimeMode DateMode { get; init; }` |
| property | `DateTimeMode TimeMode { get; init; }` |
| property | `string EntrypointIdentity { get; init; }` |
| property | `string SituationIdentity { get; init; }` |
| property | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| property | `bool PluginTrace { get; init; }` |
| property | `bool NativeTrace { get; init; }` |
| property | `StartDateTime Start { get; init; }` |
| property | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Static class in `OmsiLaunch.Api`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `uint Magic = 1330402120` |
| constant | `ushort Version = 7` |
| constant | `int MaxModules = 64` |
| static method | `byte[] Serialize(StartupHandoff value)` |
| static method | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| property | `string RelativeDirectory { get; init; }` |
| property | `string ManifestSha256 { get; init; }` |
| property | `string Settings { get; init; }` |

### `TelemetryRingWire`

Static class in `OmsiLaunch.Api`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `uint Magic = 1381256271` |
| constant | `int Version = 1` |
| constant | `int SlotCount = 64` |
| constant | `int SlotSize = 2048` |
| constant | `int HeaderSize = 64` |
| constant | `int Capacity = 131136` |
| constant | `int MaxPayload = 2036` |
| static method | `void Initialize(MemoryMappedViewAccessor view)` |
| static method | `bool IsRing(MemoryMappedViewAccessor view)` |
| static method | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| static method | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| property | `DateTimeMode Mode { get; init; }` |
| property | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enum (`byte`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `Unset = 0` |
| value | `Preset = 1` |
| value | `Icao = 2` |
| value | `RealCurrent = 3` |

### `WeatherSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| property | `WeatherMode Mode { get; init; }` |
| property | `OptionalValue<string> Preset { get; init; }` |
| property | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enum (`int`) in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| value | `NewMap = 0` |
| value | `SavedSituation = 1` |
| value | `LastMapState = 2` |
| value | `LastSituation = 2` |

### `WorldSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `STABLE_BETA`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| property | `WorldMode Mode { get; init; }` |
| property | `OptionalValue<string> MapIdentity { get; init; }` |
| property | `OptionalValue<string> SituationIdentity { get; init; }` |
| property | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| property | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| property | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Sealed record in `OmsiLaunch.Api`. Stability: `PARTIAL`. Semantics: [launchspec](launchspec.md).

| Member | Signature |
| --- | --- |
| constructor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| property | `DateTimeMode Mode { get; init; }` |
| property | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Sealed class in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `HofPlanningException(string code, string message)` |
| property | `string Code { get; }` |

### `LaunchValidation`

Static class in `OmsiLaunch.Core`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| static method | `bool IsMapIdentity(string value)` |
| static method | `bool IsSha256(string value)` |
| static method | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Static class in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| static method | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Sealed record in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| property | `string PluginBuildDirectory { get; init; }` |
| property | `string NativeBridgePath { get; init; }` |
| property | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Sealed class in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| method | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| method | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| method | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| method | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| method | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| method | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| method | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Sealed record in `OmsiLaunch.Core`. Stability: `EXPERIMENTAL`. Semantics: [session-profiles](session-profiles.md).

| Member | Signature |
| --- | --- |
| constructor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| property | `string Map { get; init; }` |
| property | `int? EntrypointIndex { get; init; }` |
| property | `string EntrypointIdentity { get; init; }` |
| property | `SemanticDate Date { get; init; }` |
| property | `SemanticTime Time { get; init; }` |
| property | `int? Year { get; init; }` |
| property | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Sealed record in `OmsiLaunch.Core`. Stability: `EXPERIMENTAL`. Semantics: [session-profiles](session-profiles.md).

| Member | Signature |
| --- | --- |
| constructor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| property | `int Index { get; init; }` |
| property | `string Id { get; init; }` |
| property | `string Name { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| property | `SessionPresentationSpec Presentation { get; init; }` |
| property | `InternetTexturesSpec InternetTextures { get; init; }` |
| property | `LaunchBehaviorSpec Behavior { get; init; }` |
| property | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| property | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Sealed class in `OmsiLaunch.Core`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionPlanner(IRuntimePlatform platform)` |
| method | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Static class in `OmsiLaunch.Core`. Stability: `EXPERIMENTAL`. Semantics: [session-profiles](session-profiles.md).

| Member | Signature |
| --- | --- |
| constant | `string Schema = "omsilaunch.session-profile/v1"` |
| constant | `long MaxBytes = 262144` |
| constant | `int MinPresetIndex = 1` |
| constant | `int MaxPresetIndex = 99` |
| static field | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| static method | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| static method | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| static method | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| static method | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Sealed class in `OmsiLaunch.Core`. Stability: `EXPERIMENTAL`. Semantics: [session-profiles](session-profiles.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionProfileException(string code, string message)` |
| property | `string Code { get; }` |

### `SessionProfilePackage`

Sealed record in `OmsiLaunch.Core`. Stability: `EXPERIMENTAL`. Semantics: [session-profiles](session-profiles.md).

| Member | Signature |
| --- | --- |
| constructor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| property | `string RootPath { get; init; }` |
| property | `SessionProfileMetadata Metadata { get; init; }` |
| property | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| property | `ProfileNew New { get; init; }` |
| property | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Static class in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `int MinimumYear = 1900` |
| constant | `int MaximumYear = 2200` |
| static method | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Static class in `OmsiLaunch.Core`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| static method | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| property | `string Name { get; }` |
| property | `Guid SessionId { get; }` |
| static method | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| method | `void Dispose()` |
| method | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| property | `string Name { get; }` |
| property | `Guid SessionId { get; }` |
| static method | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| method | `void Dispose()` |

### `CurrentTelemetryStore`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| property | `string Name { get; }` |
| static method | `CurrentTelemetryStore Create(Guid sessionId)` |
| method | `void Dispose()` |
| method | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Sealed class in `OmsiLaunch.Process`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `CurrentWindowsX64Platform()` |
| method | `RuntimePlatformInfo Detect(string root)` |
| method | `bool HasExited(LaunchedProcess p)` |
| method | `bool IsInstallationWritable(string root)` |
| method | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| method | `void Terminate(LaunchedProcess p)` |
| method | `void ValidateCurrent(RuntimePlatformInfo p)` |
| method | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interface in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| method | `OmsiProcessState Observe()` |
| method | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| method | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interface in `OmsiLaunch.Process`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| method | `RuntimePlatformInfo Detect(string installationRoot)` |
| method | `bool HasExited(LaunchedProcess process)` |
| method | `bool IsInstallationWritable(string installationRoot)` |
| method | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| method | `void Terminate(LaunchedProcess process)` |
| method | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| method | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| static method | `InstallationLease Acquire(string installationRoot)` |
| method | `void Dispose()` |
| static method | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| property | `int ProcessId { get; }` |
| property | `int ThreadId { get; }` |
| property | `ProcessIdentity Identity { get; }` |
| method | `void Dispose()` |

### `ModuleSessionProcess`

Sealed class in `OmsiLaunch.Process`. Stability: `STABLE_BETA`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `int MaxLineBytes = 65536` |
| property | `int ProcessId { get; }` |
| property | `bool HasExited { get; }` |
| property | `int? ExitCode { get; }` |
| method | `void Dispose()` |
| method | `void Kill()` |
| static method | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| method | `bool TryWriteLine(string line)` |
| method | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Sealed record in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| property | `string State { get; init; }` |
| property | `int? ProcessId { get; init; }` |
| property | `bool Responding { get; init; }` |

### `ProcessIdentity`

Sealed record in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| property | `int ProcessId { get; init; }` |
| property | `DateTimeOffset CreationTimeUtc { get; init; }` |
| property | `string ExecutablePath { get; init; }` |
| property | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Static class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constant | `string FileName = "release-manifest.json"` |
| static method | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| static method | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Sealed record in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| property | `string SourcePath { get; init; }` |
| property | `string DestinationRelativePath { get; init; }` |
| property | `string Sha256 { get; init; }` |
| property | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Sealed class in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| property | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| property | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| property | `string IntegrityReference { get; }` |
| static method | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| method | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Sealed record in `OmsiLaunch.Process`. Stability: `INTERNAL`. Semantics: [public-api](public-api.md).

| Member | Signature |
| --- | --- |
| constructor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| property | `string ExecutablePath { get; init; }` |
| property | `string WorkingDirectory { get; init; }` |
| property | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
