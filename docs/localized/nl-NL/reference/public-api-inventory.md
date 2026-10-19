# Inventaris van de publieke API

<!-- l10n: source=reference/public-api-inventory.md source-sha256=1038165496fa7e22c0b936388cb2dde7b90ed33b193b8f77d462d10d9eff149a -->
> Vertaling van de [oorspronkelijke Engelse pagina](../../../reference/public-api-inventory.md) voor OmsiLaunch 0.1.0-beta4. De Engelse pagina is normatief: bij verschillen gelden de Engelse pagina en de code.

Deze pagina wordt door `tests/OmsiLaunch.DocumentationTests` gegenereerd uit de gecompileerde assemblies (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); de documentatiecontrole faalt zodra ze niet meer overeenkomt met de code. Ze vermeldt elk geëxporteerd type van `OmsiLaunch.Api`, `OmsiLaunch.Core` en `OmsiLaunch.Process` met de stabiliteit en de signatuur van elk publiek lid. Semantiek, voorwaarden, fouten en voorbeelden staan op de pagina die in elke kop wordt genoemd (standaard: [publieke API](public-api.md)). Stabiliteitsniveaus: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (om technische redenen publiek, geen integratie-oppervlak), `UNAVAILABLE` (zie [publieke API](public-api.md#stability-vocabulary)). Door de compiler gegenereerde recordleden (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) zijn weggelaten.

## `OmsiLaunch.Api`

### `Capability`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `bool Available { get; init; }` |
| eigenschap | `string EvidenceState { get; init; }` |
| eigenschap | `string Reason { get; init; }` |

### `ContentIdentity`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| eigenschap | `string Identity { get; init; }` |
| eigenschap | `string Kind { get; init; }` |
| eigenschap | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Maps = 0` |
| waarde | `Situations = 1` |
| waarde | `Vehicles = 2` |
| waarde | `Repaints = 3` |
| waarde | `Hofs = 4` |
| waarde | `FleetNumbers = 5` |
| waarde | `Registrations = 6` |
| waarde | `Addons = 7` |
| waarde | `Entrypoints = 8` |
| waarde | `SessionProfiles = 9` |
| waarde | `RuntimeModules = 10` |

### `D3DDeviceState`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `NotReady = 0` |
| waarde | `Ready = 1` |
| waarde | `Lost = 2` |
| waarde | `Resetting = 3` |
| waarde | `Stopping = 4` |
| waarde | `Stopped = 5` |

### `D3DDeviceStatus`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| eigenschap | `bool Available { get; init; }` |
| eigenschap | `D3DDeviceState State { get; init; }` |
| eigenschap | `uint Generation { get; init; }` |
| eigenschap | `uint LiveTextureCount { get; init; }` |
| eigenschap | `bool ResetHookInstalled { get; init; }` |
| eigenschap | `uint ExecutionThreadId { get; init; }` |
| eigenschap | `uint LastResetThreadId { get; init; }` |
| eigenschap | `int QueryInterfaceHResult { get; init; }` |
| eigenschap | `int CooperativeLevelHResult { get; init; }` |
| eigenschap | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| extensiemethode | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extensiemethode | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extensiemethode | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| extensiemethode | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| extensiemethode | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| eigenschap | `D3DTextureHandle Handle { get; init; }` |
| eigenschap | `D3DTextureResourceState State { get; init; }` |
| eigenschap | `D3DDeviceState DeviceState { get; init; }` |
| eigenschap | `uint Generation { get; init; }` |
| eigenschap | `uint Width { get; init; }` |
| eigenschap | `uint Height { get; init; }` |
| eigenschap | `D3DTextureFormat Format { get; init; }` |
| eigenschap | `uint Levels { get; init; }` |
| eigenschap | `uint Level { get; init; }` |
| eigenschap | `uint LevelWidth { get; init; }` |
| eigenschap | `uint LevelHeight { get; init; }` |
| eigenschap | `int HResult { get; init; }` |
| eigenschap | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `A8R8G8B8 = 0` |
| waarde | `X8R8G8B8 = 1` |
| waarde | `R5G6B5 = 2` |
| waarde | `X1R5G5B5 = 3` |
| waarde | `A1R5G5B5 = 4` |
| waarde | `A4R4G4B4 = 5` |
| waarde | `A8 = 6` |
| waarde | `L8 = 7` |
| waarde | `A8L8 = 8` |

### `D3DTextureHandle`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `D3DTextureHandle(string Value)` |
| eigenschap | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Live = 0` |
| waarde | `Released = 1` |
| waarde | `Stale = 2` |

### `D3DTextureUpdate`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| eigenschap | `uint Level { get; init; }` |
| eigenschap | `uint X { get; init; }` |
| eigenschap | `uint Y { get; init; }` |
| eigenschap | `uint Width { get; init; }` |
| eigenschap | `uint Height { get; init; }` |
| eigenschap | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| eigenschap | `DateTimeMode Mode { get; init; }` |
| eigenschap | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Unset = 0` |
| waarde | `Explicit = 1` |
| waarde | `System = 2` |

### `DiagnosticsSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| eigenschap | `bool Log { get; init; }` |
| eigenschap | `bool Verbose { get; init; }` |
| eigenschap | `bool OmsiLogAll { get; init; }` |
| eigenschap | `bool ProcessTrace { get; init; }` |
| eigenschap | `bool PluginTrace { get; init; }` |
| eigenschap | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Unset = 0` |
| waarde | `PresentedIndex = 1` |
| waarde | `Identity = 2` |

### `EntrypointSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| eigenschap | `EntrypointMode Mode { get; init; }` |
| eigenschap | `OptionalValue<int> PresentedIndex { get; init; }` |
| eigenschap | `OptionalValue<string> Identity { get; init; }` |
| statische eigenschap | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Add = 0` |
| waarde | `Mirror = 1` |

### `HofManagementSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| eigenschap | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| eigenschap | `HofSourceKind SourceKind { get; init; }` |
| eigenschap | `string Source { get; init; }` |
| eigenschap | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| eigenschap | `HofApplyMode Mode { get; init; }` |
| eigenschap | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Collection = 0` |
| waarde | `Directory = 1` |
| waarde | `Vehicle = 2` |
| waarde | `File = 3` |

### `IOmsiLaunch`

Interface in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| methode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| methode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| methode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| methode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| eigenschap | `OptionalValue<string> KeyboardDocument { get; init; }` |
| eigenschap | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| statische methode | `string IdentityKey(string root)` |
| statische methode | `string NormalizeRoot(string root)` |
| statische methode | `IReadOnlyList<string> Segments(string relativePath)` |
| statische methode | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| eigenschap | `string RootPath { get; init; }` |
| eigenschap | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Native = 0` |
| waarde | `Disabled = 1` |
| waarde | `Override = 2` |

### `InternetTexturesSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| eigenschap | `InternetTexturesMode Mode { get; init; }` |
| eigenschap | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| eigenschap | `bool RestoreConfiguration { get; init; }` |
| eigenschap | `bool SuppressStaleClosecheckWarning { get; init; }` |
| eigenschap | `int StartupTimeoutSeconds { get; init; }` |
| eigenschap | `int ShutdownTimeoutSeconds { get; init; }` |
| eigenschap | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| eigenschap | `string Code { get; init; }` |
| eigenschap | `string Message { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| eigenschap | `InstallationSpec Installation { get; init; }` |
| eigenschap | `WorldSpec World { get; init; }` |
| eigenschap | `DateSpec Date { get; init; }` |
| eigenschap | `TimeSpec Time { get; init; }` |
| eigenschap | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| eigenschap | `EnvironmentSpec Environment { get; init; }` |
| eigenschap | `LaunchBehaviorSpec Behavior { get; init; }` |
| eigenschap | `YearSpec Year { get; init; }` |
| eigenschap | `WeatherSpec Weather { get; init; }` |
| eigenschap | `InputSpec Input { get; init; }` |
| eigenschap | `DiagnosticsSpec Diagnostics { get; init; }` |
| eigenschap | `SessionPresentationSpec Presentation { get; init; }` |
| eigenschap | `InternetTexturesSpec InternetTextures { get; init; }` |
| eigenschap | `SessionProfileMetadata SessionProfile { get; init; }` |
| eigenschap | `HofManagementSpec HofManagement { get; init; }` |
| eigenschap | `RuntimeModulesSpec Modules { get; init; }` |
| eigenschap | `HofManagementSpec EffectiveHofManagement { get; }` |
| eigenschap | `RuntimeModulesSpec EffectiveModules { get; }` |
| eigenschap | `YearSpec EffectiveYear { get; }` |
| eigenschap | `WeatherSpec EffectiveWeather { get; }` |
| eigenschap | `InputSpec EffectiveInput { get; }` |
| eigenschap | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| eigenschap | `SessionPresentationSpec EffectivePresentation { get; }` |
| eigenschap | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Sealed klasse in `OmsiLaunch.Api`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `OmsiRuntimeException(string code, string detail = null)` |
| eigenschap | `string Code { get; }` |

### `OptionalValue`

Record struct in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `OptionalValue(Presence Presence, T Value)` |
| eigenschap | `Presence Presence { get; init; }` |
| eigenschap | `T Value { get; init; }` |
| eigenschap | `bool IsSet { get; }` |
| statische eigenschap | `OptionalValue<T> Unset { get; }` |
| statische methode | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null, IReadOnlyList<string> RuntimeOperations = null, int TickMilliseconds = 0, string SnapshotSha256 = null)` |
| constante | `string Eligible = "ELIGIBLE"` |
| constante | `string Rejected = "REJECTED"` |
| constante | `string ActivationRequested = "requested"` |
| constante | `string ActivationSettingsFile = "settings-file"` |
| constante | `string ActivationContent = "content"` |
| constante | `string ActivationDefault = "default"` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Version { get; init; }` |
| eigenschap | `string Status { get; init; }` |
| eigenschap | `bool Requested { get; init; }` |
| eigenschap | `string FailurePolicy { get; init; }` |
| eigenschap | `IReadOnlyList<string> Operations { get; init; }` |
| eigenschap | `string Code { get; init; }` |
| eigenschap | `string Reason { get; init; }` |
| eigenschap | `string ManifestSha256 { get; init; }` |
| eigenschap | `string Activation { get; init; }` |
| eigenschap | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| eigenschap | `IReadOnlyList<string> RuntimeOperations { get; init; }` |
| eigenschap | `int TickMilliseconds { get; init; }` |
| eigenschap | `string SnapshotSha256 { get; init; }` |
| eigenschap | `bool IsEligible { get; }` |
| eigenschap | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |
| eigenschap | `IReadOnlyList<string> EffectiveRuntimeOperations { get; }` |

### `PlannedModuleSetting`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| eigenschap | `string Key { get; init; }` |
| eigenschap | `string Value { get; init; }` |
| eigenschap | `string Source { get; init; }` |

### `PlannedMutation`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| eigenschap | `string RelativePath { get; init; }` |
| eigenschap | `string SemanticKey { get; init; }` |
| eigenschap | `string RequestedValue { get; init; }` |
| eigenschap | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| eigenschap | `OptionalValue<string> Model { get; init; }` |
| eigenschap | `OptionalValue<string> Repaint { get; init; }` |
| eigenschap | `OptionalValue<string> Hof { get; init; }` |
| eigenschap | `OptionalValue<string> FleetNumber { get; init; }` |
| eigenschap | `OptionalValue<string> Registration { get; init; }` |
| eigenschap | `bool Enabled { get; }` |

### `Presence`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Unset = 0` |
| waarde | `Set = 1` |

### `PublicCapabilityClassification`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [capabilities](capabilities.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `PublicStableBeta = 0` |
| waarde | `PublicExperimental = 1` |
| waarde | `InternalOnly = 2` |
| waarde | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [capabilities](capabilities.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Family { get; init; }` |
| eigenschap | `PublicCapabilityClassification Classification { get; init; }` |
| eigenschap | `PublicCapabilityKind Kind { get; init; }` |
| eigenschap | `bool RequiresSession { get; init; }` |
| eigenschap | `bool RequiresExactProfile { get; init; }` |
| eigenschap | `string ApiRoute { get; init; }` |
| eigenschap | `string CliRoute { get; init; }` |
| eigenschap | `string RuntimeValidation { get; init; }` |
| eigenschap | `string Description { get; init; }` |
| eigenschap | `IReadOnlyList<string> HandleTypes { get; init; }` |
| eigenschap | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| eigenschap | `string NativeCapability { get; init; }` |
| eigenschap | `string EvidenceNote { get; init; }` |
| eigenschap | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| eigenschap | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [capabilities](capabilities.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Read = 0` |
| waarde | `Write = 1` |
| waarde | `Action = 2` |
| waarde | `Event = 3` |

### `PublicCapabilityRegistry`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [capabilities](capabilities.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string ProtocolVersion = "0.1"` |
| statisch veld | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| statische eigenschap | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| statische eigenschap | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| statische eigenschap | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| statische methode | `PublicCapabilityDescriptor Get(string id)` |
| statische methode | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| statische methode | `bool IsInternalResultKey(string key)` |
| statische methode | `bool IsModuleOperation(string operation)` |
| statische methode | `bool IsPublicRuntimeOperation(string operation)` |
| statische methode | `string NativeCapabilityOf(string operation)` |
| statische methode | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| statische methode | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| statische methode | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [errors](errors.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string InvalidArgument = "invalid_argument"` |
| constante | `string UnsupportedProfile = "unsupported_profile"` |
| constante | `string Session = "session"` |
| constante | `string Runtime = "runtime"` |
| constante | `string NotFound = "not_found"` |
| constante | `string Transaction = "transaction"` |
| constante | `string Internal = "internal"` |

### `PublicErrorCodes`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [errors](errors.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string CANCELLED = "OL_E_CANCELLED"` |
| constante | `string INTERNAL = "OL_E_INTERNAL"` |
| constante | `string TIMEOUT = "OL_E_TIMEOUT"` |
| constante | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| constante | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| constante | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| constante | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| constante | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| constante | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| constante | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| constante | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| constante | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| constante | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| constante | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| constante | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| constante | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| constante | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| constante | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| constante | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| constante | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| constante | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| constante | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| constante | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| constante | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| constante | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| constante | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| constante | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| constante | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| constante | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| constante | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| constante | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| constante | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| constante | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| constante | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| constante | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| constante | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| constante | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| constante | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| constante | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| constante | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| constante | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| constante | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| constante | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| constante | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| constante | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| constante | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| constante | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| constante | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| constante | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| constante | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| constante | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| constante | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| constante | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| constante | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| constante | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| constante | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| constante | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| constante | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| constante | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| constante | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| constante | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| constante | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| constante | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| constante | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| constante | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| constante | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| constante | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| constante | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| constante | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| constante | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| constante | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| constante | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| constante | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| constante | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| constante | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| constante | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| constante | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| constante | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| constante | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| constante | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| constante | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| constante | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| constante | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| constante | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| constante | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| constante | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| constante | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| constante | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| constante | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| constante | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| constante | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| constante | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| constante | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| constante | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| constante | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| constante | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| constante | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| constante | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| constante | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| constante | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| constante | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| constante | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| constante | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| constante | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| constante | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| constante | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| constante | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| constante | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| constante | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| constante | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| constante | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| constante | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| constante | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| constante | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| constante | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| constante | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| constante | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| constante | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| constante | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| constante | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| constante | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| constante | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| constante | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| constante | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| constante | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| constante | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| constante | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| constante | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| constante | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| constante | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| constante | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| constante | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| constante | `string START_SESSION = "OL_E_START_SESSION"` |
| constante | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| constante | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| constante | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| constante | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| constante | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| constante | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| constante | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| constante | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| constante | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| constante | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| constante | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| constante | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| constante | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| constante | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| constante | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| constante | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| constante | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| constante | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| constante | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| constante | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| constante | `string MODULE_OPERATION_NOT_DECLARED = "OL_E_MODULE_OPERATION_NOT_DECLARED"` |
| constante | `string MODULE_CONTENT_INPUT_UNDECLARED = "OL_E_MODULE_CONTENT_INPUT_UNDECLARED"` |
| constante | `string MODULE_CONTENT_INPUT_INVALID = "OL_E_MODULE_CONTENT_INPUT_INVALID"` |
| constante | `string UI_NOT_PAUSED = "OL_E_UI_NOT_PAUSED"` |
| constante | `string UI_BUSY = "OL_E_UI_BUSY"` |
| constante | `string UI_ALREADY_TAKEN_OVER = "OL_E_UI_ALREADY_TAKEN_OVER"` |
| constante | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| constante | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| constante | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| constante | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| constante | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| constante | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| constante | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| constante | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| constante | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| constante | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| constante | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| constante | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| constante | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| constante | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| constante | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| constante | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| constante | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| constante | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| constante | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| constante | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| constante | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| constante | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| constante | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| constante | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| statisch veld | `IReadOnlyList<PublicErrorDescriptor> All` |
| statische methode | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [errors](errors.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PublicErrorDescriptor(string Code, string Category)` |
| eigenschap | `string Code { get; init; }` |
| eigenschap | `string Category { get; init; }` |

### `PublicEvidence`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| constante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| constante | `string Partial = "PARTIAL"` |
| constante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| constante | `string Unavailable = "UNAVAILABLE"` |
| statisch veld | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enumeratie (`int`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [exit-codes](exit-codes.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Success = 0` |
| waarde | `SessionFailed = 1` |
| waarde | `InvalidArguments = 2` |
| waarde | `UnsupportedProfile = 3` |
| waarde | `NoActiveSession = 4` |
| waarde | `RuntimeUnavailable = 5` |
| waarde | `NotFound = 6` |
| waarde | `OperationRejected = 7` |
| waarde | `TransactionRecoveryFailed = 8` |
| waarde | `InternalError = 10` |

### `PublicMutationPolicy`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string None = "none"` |
| constante | `string OmsiState = "omsi-state"` |
| constante | `string SessionResource = "session-resource"` |
| constante | `string SessionPolicy = "session-policy"` |
| statisch veld | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `bool Required { get; init; }` |
| eigenschap | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| eigenschap | `bool Accepted { get; init; }` |
| eigenschap | `string ErrorCode { get; init; }` |
| eigenschap | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string NativeCapability { get; init; }` |
| eigenschap | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| eigenschap | `IReadOnlyList<string> CliRoutes { get; init; }` |
| eigenschap | `bool Paged { get; init; }` |
| eigenschap | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| eigenschap | `bool Pending { get; init; }` |
| eigenschap | `bool Recovered { get; init; }` |
| eigenschap | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [runtime-control](runtime-control.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| eigenschap | `Guid SessionId { get; init; }` |
| eigenschap | `ulong RequestId { get; init; }` |
| eigenschap | `string Operation { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [runtime-control](runtime-control.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| eigenschap | `Guid SessionId { get; init; }` |
| eigenschap | `ulong RequestId { get; init; }` |
| eigenschap | `bool Succeeded { get; init; }` |
| eigenschap | `string ErrorCode { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `uint Magic = 1330401859` |
| constante | `ushort Version = 1` |
| constante | `int HeaderSize = 72` |
| statische methode | `byte[] SerializeRequest(RuntimeCommand command)` |
| statische methode | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| statische methode | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| statische methode | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| statische methode | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| eigenschap | `string Type { get; init; }` |
| eigenschap | `DateTimeOffset TimestampUtc { get; init; }` |
| eigenschap | `long Sequence { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| eigenschap | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| eigenschap | `string Key { get; init; }` |
| eigenschap | `string Type { get; init; }` |
| eigenschap | `string Default { get; init; }` |
| eigenschap | `bool AllowsAuto { get; init; }` |
| eigenschap | `string Minimum { get; init; }` |
| eigenschap | `string Maximum { get; init; }` |
| eigenschap | `IReadOnlyList<string> Choices { get; init; }` |
| eigenschap | `string Flag { get; init; }` |
| eigenschap | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Version { get; init; }` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `string Activation { get; init; }` |
| eigenschap | `bool IsValid { get; init; }` |
| eigenschap | `IReadOnlyList<string> Operations { get; init; }` |
| eigenschap | `string ContentBindingMap { get; init; }` |
| eigenschap | `string ErrorCode { get; init; }` |
| eigenschap | `string ErrorMessage { get; init; }` |
| eigenschap | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| eigenschap | `IReadOnlyList<string> Enable { get; init; }` |
| eigenschap | `IReadOnlyList<string> Disable { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| eigenschap | `IReadOnlyList<string> EffectiveDisable { get; }` |
| eigenschap | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| eigenschap | `string OsFamily { get; init; }` |
| eigenschap | `string OsVersion { get; init; }` |
| eigenschap | `string OsArchitecture { get; init; }` |
| eigenschap | `string HostArchitecture { get; init; }` |
| eigenschap | `string OmsiArchitecture { get; init; }` |
| eigenschap | `string PluginArchitecture { get; init; }` |
| eigenschap | `bool CurrentPlatformSupported { get; init; }` |
| eigenschap | `bool LegacyPlatform { get; init; }` |
| eigenschap | `bool Wow64Available { get; init; }` |
| eigenschap | `bool InstallationWritable { get; init; }` |
| eigenschap | `bool ProcessLaunchSupported { get; init; }` |
| eigenschap | `bool PluginRuntimeSupported { get; init; }` |
| eigenschap | `bool NativeInteropSupported { get; init; }` |
| eigenschap | `bool SharedMemorySupported { get; init; }` |
| eigenschap | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SemanticDate(int Year, int Month, int Day)` |
| eigenschap | `int Year { get; init; }` |
| eigenschap | `int Month { get; init; }` |
| eigenschap | `int Day { get; init; }` |

### `SemanticTime`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SemanticTime(int Hour, int Minute, int Second)` |
| eigenschap | `int Hour { get; init; }` |
| eigenschap | `int Minute { get; init; }` |
| eigenschap | `int Second { get; init; }` |

### `SessionHandle`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionHandle(Guid SessionId)` |
| eigenschap | `Guid SessionId { get; init; }` |

### `SessionPlan`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| eigenschap | `Guid SessionId { get; init; }` |
| eigenschap | `string BuildProfileId { get; init; }` |
| eigenschap | `LaunchSpec Spec { get; init; }` |
| eigenschap | `RuntimePlatformInfo Platform { get; init; }` |
| eigenschap | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| eigenschap | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| eigenschap | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| eigenschap | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| eigenschap | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| eigenschap | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| eigenschap | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| eigenschap | `bool IsRunnable { get; init; }` |
| eigenschap | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| eigenschap | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| eigenschap | `SplashMode Splash { get; init; }` |
| eigenschap | `OptionalValue<string> Language { get; init; }` |
| eigenschap | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| eigenschap | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `string Version { get; init; }` |
| eigenschap | `string Author { get; init; }` |
| eigenschap | `string PresetId { get; init; }` |
| eigenschap | `int PresetIndex { get; init; }` |
| eigenschap | `string PresetName { get; init; }` |
| eigenschap | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| eigenschap | `int Index { get; init; }` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Name { get; init; }` |

### `SessionProfileSummary`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `string Version { get; init; }` |
| eigenschap | `string Author { get; init; }` |
| eigenschap | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| eigenschap | `string MapIdentity { get; init; }` |
| eigenschap | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| eigenschap | `bool IsValid { get; init; }` |
| eigenschap | `string ErrorCode { get; init; }` |
| eigenschap | `string ErrorMessage { get; init; }` |

### `SessionState`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Created = 0` |
| waarde | `ValidatingPlatform = 1` |
| waarde | `Planning = 2` |
| waarde | `AcquiringInstallationLock = 3` |
| waarde | `RecoveringPreviousTransaction = 4` |
| waarde | `Snapshotting = 5` |
| waarde | `ApplyingConfiguration = 6` |
| waarde | `DeployingRuntime = 7` |
| waarde | `CreatingStartupHandoff = 8` |
| waarde | `StartingProcess = 9` |
| waarde | `WaitingForPlugin = 10` |
| waarde | `PluginBootstrap = 11` |
| waarde | `StartingWorld = 12` |
| waarde | `EnteringGameplay = 13` |
| waarde | `Running = 14` |
| waarde | `ProcessExited = 15` |
| waarde | `Restoring = 16` |
| waarde | `CleaningRuntime = 17` |
| waarde | `Completed = 18` |
| waarde | `Failed = 19` |

### `SessionStatus`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| eigenschap | `Guid SessionId { get; init; }` |
| eigenschap | `SessionState State { get; init; }` |
| eigenschap | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| eigenschap | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Unset = 0` |
| waarde | `Native = 0` |
| waarde | `Managed = 1` |

### `StartDateTime`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| eigenschap | `int? Year { get; init; }` |
| eigenschap | `int? DayOfYear { get; init; }` |
| eigenschap | `int? Hour { get; init; }` |
| eigenschap | `int? Minute { get; init; }` |
| eigenschap | `int? Second { get; init; }` |
| eigenschap | `bool IsEmpty { get; }` |

### `StartWeather`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| eigenschap | `WeatherMode Mode { get; init; }` |
| eigenschap | `string Preset { get; init; }` |
| eigenschap | `string Icao { get; init; }` |

### `StartupHandoff`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| eigenschap | `Guid SessionId { get; init; }` |
| eigenschap | `string BuildProfileId { get; init; }` |
| eigenschap | `WorldMode WorldMode { get; init; }` |
| eigenschap | `string MapIdentity { get; init; }` |
| eigenschap | `int PresentedEntrypointIndex { get; init; }` |
| eigenschap | `bool HeadlessStart { get; init; }` |
| eigenschap | `bool PlayerVehicleEnabled { get; init; }` |
| eigenschap | `DateTimeMode DateMode { get; init; }` |
| eigenschap | `DateTimeMode TimeMode { get; init; }` |
| eigenschap | `string EntrypointIdentity { get; init; }` |
| eigenschap | `string SituationIdentity { get; init; }` |
| eigenschap | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| eigenschap | `bool PluginTrace { get; init; }` |
| eigenschap | `bool NativeTrace { get; init; }` |
| eigenschap | `StartDateTime Start { get; init; }` |
| eigenschap | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `uint Magic = 1330402120` |
| constante | `ushort Version = 8` |
| constante | `int MaxModules = 64` |
| statische methode | `byte[] Serialize(StartupHandoff value)` |
| statische methode | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "", string SnapshotSha256 = "")` |
| eigenschap | `string RelativeDirectory { get; init; }` |
| eigenschap | `string ManifestSha256 { get; init; }` |
| eigenschap | `string Settings { get; init; }` |
| eigenschap | `string SnapshotSha256 { get; init; }` |

### `TelemetryRingWire`

Statische klasse in `OmsiLaunch.Api`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `uint Magic = 1381256271` |
| constante | `int Version = 1` |
| constante | `int SlotCount = 64` |
| constante | `int SlotSize = 2048` |
| constante | `int HeaderSize = 64` |
| constante | `int Capacity = 131136` |
| constante | `int MaxPayload = 2036` |
| statische methode | `void Initialize(MemoryMappedViewAccessor view)` |
| statische methode | `bool IsRing(MemoryMappedViewAccessor view)` |
| statische methode | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| statische methode | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| eigenschap | `DateTimeMode Mode { get; init; }` |
| eigenschap | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enumeratie (`byte`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `Unset = 0` |
| waarde | `Preset = 1` |
| waarde | `Icao = 2` |
| waarde | `RealCurrent = 3` |

### `WeatherSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| eigenschap | `WeatherMode Mode { get; init; }` |
| eigenschap | `OptionalValue<string> Preset { get; init; }` |
| eigenschap | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enumeratie (`int`) in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| waarde | `NewMap = 0` |
| waarde | `SavedSituation = 1` |
| waarde | `LastMapState = 2` |
| waarde | `LastSituation = 2` |

### `WorldSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `STABLE_BETA`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| eigenschap | `WorldMode Mode { get; init; }` |
| eigenschap | `OptionalValue<string> MapIdentity { get; init; }` |
| eigenschap | `OptionalValue<string> SituationIdentity { get; init; }` |
| eigenschap | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| eigenschap | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| eigenschap | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Sealed record in `OmsiLaunch.Api`. Stabiliteit: `PARTIAL`. Semantiek: [launchspec](launchspec.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| eigenschap | `DateTimeMode Mode { get; init; }` |
| eigenschap | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Sealed klasse in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `HofPlanningException(string code, string message)` |
| eigenschap | `string Code { get; }` |

### `LaunchValidation`

Statische klasse in `OmsiLaunch.Core`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| statische methode | `bool IsMapIdentity(string value)` |
| statische methode | `bool IsSha256(string value)` |
| statische methode | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Statische klasse in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| statische methode | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Sealed record in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| eigenschap | `string PluginBuildDirectory { get; init; }` |
| eigenschap | `string NativeBridgePath { get; init; }` |
| eigenschap | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Sealed klasse in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| methode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| methode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| methode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| methode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| methode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Sealed record in `OmsiLaunch.Core`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [session-profiles](session-profiles.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| eigenschap | `string Map { get; init; }` |
| eigenschap | `int? EntrypointIndex { get; init; }` |
| eigenschap | `string EntrypointIdentity { get; init; }` |
| eigenschap | `SemanticDate Date { get; init; }` |
| eigenschap | `SemanticTime Time { get; init; }` |
| eigenschap | `int? Year { get; init; }` |
| eigenschap | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Sealed record in `OmsiLaunch.Core`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [session-profiles](session-profiles.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| eigenschap | `int Index { get; init; }` |
| eigenschap | `string Id { get; init; }` |
| eigenschap | `string Name { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| eigenschap | `SessionPresentationSpec Presentation { get; init; }` |
| eigenschap | `InternetTexturesSpec InternetTextures { get; init; }` |
| eigenschap | `LaunchBehaviorSpec Behavior { get; init; }` |
| eigenschap | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| eigenschap | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Sealed klasse in `OmsiLaunch.Core`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionPlanner(IRuntimePlatform platform)` |
| methode | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Statische klasse in `OmsiLaunch.Core`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [session-profiles](session-profiles.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string Schema = "omsilaunch.session-profile/v1"` |
| constante | `long MaxBytes = 262144` |
| constante | `int MinPresetIndex = 1` |
| constante | `int MaxPresetIndex = 99` |
| statisch veld | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| statische methode | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| statische methode | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| statische methode | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| statische methode | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Sealed klasse in `OmsiLaunch.Core`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [session-profiles](session-profiles.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionProfileException(string code, string message)` |
| eigenschap | `string Code { get; }` |

### `SessionProfilePackage`

Sealed record in `OmsiLaunch.Core`. Stabiliteit: `EXPERIMENTAL`. Semantiek: [session-profiles](session-profiles.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| eigenschap | `string RootPath { get; init; }` |
| eigenschap | `SessionProfileMetadata Metadata { get; init; }` |
| eigenschap | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| eigenschap | `ProfileNew New { get; init; }` |
| eigenschap | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Statische klasse in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `int MinimumYear = 1900` |
| constante | `int MaximumYear = 2200` |
| statische methode | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Statische klasse in `OmsiLaunch.Core`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| statische methode | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| eigenschap | `string Name { get; }` |
| eigenschap | `Guid SessionId { get; }` |
| statische methode | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| methode | `void Dispose()` |
| methode | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| eigenschap | `string Name { get; }` |
| eigenschap | `Guid SessionId { get; }` |
| statische methode | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| methode | `void Dispose()` |

### `CurrentTelemetryStore`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| eigenschap | `string Name { get; }` |
| statische methode | `CurrentTelemetryStore Create(Guid sessionId)` |
| methode | `void Dispose()` |
| methode | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `CurrentWindowsX64Platform()` |
| methode | `RuntimePlatformInfo Detect(string root)` |
| methode | `bool HasExited(LaunchedProcess p)` |
| methode | `bool IsInstallationWritable(string root)` |
| methode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| methode | `void Terminate(LaunchedProcess p)` |
| methode | `void ValidateCurrent(RuntimePlatformInfo p)` |
| methode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interface in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| methode | `OmsiProcessState Observe()` |
| methode | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| methode | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interface in `OmsiLaunch.Process`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| methode | `RuntimePlatformInfo Detect(string installationRoot)` |
| methode | `bool HasExited(LaunchedProcess process)` |
| methode | `bool IsInstallationWritable(string installationRoot)` |
| methode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| methode | `void Terminate(LaunchedProcess process)` |
| methode | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| methode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| statische methode | `InstallationLease Acquire(string installationRoot)` |
| methode | `void Dispose()` |
| statische methode | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| eigenschap | `int ProcessId { get; }` |
| eigenschap | `int ThreadId { get; }` |
| eigenschap | `ProcessIdentity Identity { get; }` |
| methode | `void Dispose()` |

### `ModuleSessionProcess`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `STABLE_BETA`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `int MaxLineBytes = 65536` |
| eigenschap | `int ProcessId { get; }` |
| eigenschap | `bool HasExited { get; }` |
| eigenschap | `int? ExitCode { get; }` |
| methode | `void Dispose()` |
| methode | `void Kill()` |
| statische methode | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| methode | `bool TryWriteLine(string line)` |
| methode | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Sealed record in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| eigenschap | `string State { get; init; }` |
| eigenschap | `int? ProcessId { get; init; }` |
| eigenschap | `bool Responding { get; init; }` |

### `ProcessIdentity`

Sealed record in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| eigenschap | `int ProcessId { get; init; }` |
| eigenschap | `DateTimeOffset CreationTimeUtc { get; init; }` |
| eigenschap | `string ExecutablePath { get; init; }` |
| eigenschap | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Statische klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constante | `string FileName = "release-manifest.json"` |
| statische methode | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| statische methode | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Sealed record in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| eigenschap | `string SourcePath { get; init; }` |
| eigenschap | `string DestinationRelativePath { get; init; }` |
| eigenschap | `string Sha256 { get; init; }` |
| eigenschap | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Sealed klasse in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| eigenschap | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| eigenschap | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| eigenschap | `string IntegrityReference { get; }` |
| statische methode | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| methode | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Sealed record in `OmsiLaunch.Process`. Stabiliteit: `INTERNAL`. Semantiek: [public-api](public-api.md).

| Lid | Signatuur |
| --- | --- |
| constructor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| eigenschap | `string ExecutablePath { get; init; }` |
| eigenschap | `string WorkingDirectory { get; init; }` |
| eigenschap | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
