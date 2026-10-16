# Inventário da API pública

<!-- l10n: source=reference/public-api-inventory.md source-sha256=3c373c0c630e07ca0b28dc908aae11d05b87f4e5716c1442a868f3980637e134 -->
> Tradução da [página original em inglês](../../../reference/public-api-inventory.md) do OmsiLaunch 0.1.0-beta4. A página em inglês é a referência normativa: se as duas divergirem, valem a página em inglês e o código.

Esta página é gerada a partir dos assemblies compilados por `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); o gate de documentação falha quando ela deixa de corresponder ao código. Ela lista todos os tipos exportados de `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process`, com a estabilidade de cada um e a assinatura de cada membro público. A semântica, as pré-condições, os erros e os exemplos estão na página indicada em cada título (padrão: [API pública](public-api.md)). Vocabulário de estabilidade: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (público por motivos técnicos, não é uma superfície de integração), `UNAVAILABLE` (veja [API pública](public-api.md#stability-vocabulary)). Os membros de record gerados pelo compilador (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) são omitidos.

## `OmsiLaunch.Api`

### `Capability`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| propriedade | `string Name { get; init; }` |
| propriedade | `bool Available { get; init; }` |
| propriedade | `string EvidenceState { get; init; }` |
| propriedade | `string Reason { get; init; }` |

### `ContentIdentity`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| propriedade | `string Identity { get; init; }` |
| propriedade | `string Kind { get; init; }` |
| propriedade | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Maps = 0` |
| valor | `Situations = 1` |
| valor | `Vehicles = 2` |
| valor | `Repaints = 3` |
| valor | `Hofs = 4` |
| valor | `FleetNumbers = 5` |
| valor | `Registrations = 6` |
| valor | `Addons = 7` |
| valor | `Entrypoints = 8` |
| valor | `SessionProfiles = 9` |
| valor | `RuntimeModules = 10` |

### `D3DDeviceState`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `NotReady = 0` |
| valor | `Ready = 1` |
| valor | `Lost = 2` |
| valor | `Resetting = 3` |
| valor | `Stopping = 4` |
| valor | `Stopped = 5` |

### `D3DDeviceStatus`

Record selado em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| propriedade | `bool Available { get; init; }` |
| propriedade | `D3DDeviceState State { get; init; }` |
| propriedade | `uint Generation { get; init; }` |
| propriedade | `uint LiveTextureCount { get; init; }` |
| propriedade | `bool ResetHookInstalled { get; init; }` |
| propriedade | `uint ExecutionThreadId { get; init; }` |
| propriedade | `uint LastResetThreadId { get; init; }` |
| propriedade | `int QueryInterfaceHResult { get; init; }` |
| propriedade | `int CooperativeLevelHResult { get; init; }` |
| propriedade | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método de extensão | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensão | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensão | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| método de extensão | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensão | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Record selado em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| propriedade | `D3DTextureHandle Handle { get; init; }` |
| propriedade | `D3DTextureResourceState State { get; init; }` |
| propriedade | `D3DDeviceState DeviceState { get; init; }` |
| propriedade | `uint Generation { get; init; }` |
| propriedade | `uint Width { get; init; }` |
| propriedade | `uint Height { get; init; }` |
| propriedade | `D3DTextureFormat Format { get; init; }` |
| propriedade | `uint Levels { get; init; }` |
| propriedade | `uint Level { get; init; }` |
| propriedade | `uint LevelWidth { get; init; }` |
| propriedade | `uint LevelHeight { get; init; }` |
| propriedade | `int HResult { get; init; }` |
| propriedade | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `A8R8G8B8 = 0` |
| valor | `X8R8G8B8 = 1` |
| valor | `R5G6B5 = 2` |
| valor | `X1R5G5B5 = 3` |
| valor | `A1R5G5B5 = 4` |
| valor | `A4R4G4B4 = 5` |
| valor | `A8 = 6` |
| valor | `L8 = 7` |
| valor | `A8L8 = 8` |

### `D3DTextureHandle`

Record selado em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `D3DTextureHandle(string Value)` |
| propriedade | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Live = 0` |
| valor | `Released = 1` |
| valor | `Stale = 2` |

### `D3DTextureUpdate`

Record selado em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| propriedade | `uint Level { get; init; }` |
| propriedade | `uint X { get; init; }` |
| propriedade | `uint Y { get; init; }` |
| propriedade | `uint Width { get; init; }` |
| propriedade | `uint Height { get; init; }` |
| propriedade | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| propriedade | `DateTimeMode Mode { get; init; }` |
| propriedade | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Explicit = 1` |
| valor | `System = 2` |

### `DiagnosticsSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| propriedade | `bool Log { get; init; }` |
| propriedade | `bool Verbose { get; init; }` |
| propriedade | `bool OmsiLogAll { get; init; }` |
| propriedade | `bool ProcessTrace { get; init; }` |
| propriedade | `bool PluginTrace { get; init; }` |
| propriedade | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Unset = 0` |
| valor | `PresentedIndex = 1` |
| valor | `Identity = 2` |

### `EntrypointSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| propriedade | `EntrypointMode Mode { get; init; }` |
| propriedade | `OptionalValue<int> PresentedIndex { get; init; }` |
| propriedade | `OptionalValue<string> Identity { get; init; }` |
| propriedade estática | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Add = 0` |
| valor | `Mirror = 1` |

### `HofManagementSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| propriedade | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| propriedade | `HofSourceKind SourceKind { get; init; }` |
| propriedade | `string Source { get; init; }` |
| propriedade | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| propriedade | `HofApplyMode Mode { get; init; }` |
| propriedade | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Collection = 0` |
| valor | `Directory = 1` |
| valor | `Vehicle = 2` |
| valor | `File = 3` |

### `IOmsiLaunch`

Interface em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| método | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| método | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| método | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| método | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| propriedade | `OptionalValue<string> KeyboardDocument { get; init; }` |
| propriedade | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método estático | `string IdentityKey(string root)` |
| método estático | `string NormalizeRoot(string root)` |
| método estático | `IReadOnlyList<string> Segments(string relativePath)` |
| método estático | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| propriedade | `string RootPath { get; init; }` |
| propriedade | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Native = 0` |
| valor | `Disabled = 1` |
| valor | `Override = 2` |

### `InternetTexturesSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| propriedade | `InternetTexturesMode Mode { get; init; }` |
| propriedade | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| propriedade | `bool RestoreConfiguration { get; init; }` |
| propriedade | `bool SuppressStaleClosecheckWarning { get; init; }` |
| propriedade | `int StartupTimeoutSeconds { get; init; }` |
| propriedade | `int ShutdownTimeoutSeconds { get; init; }` |
| propriedade | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| propriedade | `string Code { get; init; }` |
| propriedade | `string Message { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| propriedade | `InstallationSpec Installation { get; init; }` |
| propriedade | `WorldSpec World { get; init; }` |
| propriedade | `DateSpec Date { get; init; }` |
| propriedade | `TimeSpec Time { get; init; }` |
| propriedade | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| propriedade | `EnvironmentSpec Environment { get; init; }` |
| propriedade | `LaunchBehaviorSpec Behavior { get; init; }` |
| propriedade | `YearSpec Year { get; init; }` |
| propriedade | `WeatherSpec Weather { get; init; }` |
| propriedade | `InputSpec Input { get; init; }` |
| propriedade | `DiagnosticsSpec Diagnostics { get; init; }` |
| propriedade | `SessionPresentationSpec Presentation { get; init; }` |
| propriedade | `InternetTexturesSpec InternetTextures { get; init; }` |
| propriedade | `SessionProfileMetadata SessionProfile { get; init; }` |
| propriedade | `HofManagementSpec HofManagement { get; init; }` |
| propriedade | `RuntimeModulesSpec Modules { get; init; }` |
| propriedade | `HofManagementSpec EffectiveHofManagement { get; }` |
| propriedade | `RuntimeModulesSpec EffectiveModules { get; }` |
| propriedade | `YearSpec EffectiveYear { get; }` |
| propriedade | `WeatherSpec EffectiveWeather { get; }` |
| propriedade | `InputSpec EffectiveInput { get; }` |
| propriedade | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| propriedade | `SessionPresentationSpec EffectivePresentation { get; }` |
| propriedade | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Classe selada em `OmsiLaunch.Api`. Estabilidade: `EXPERIMENTAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `OmsiRuntimeException(string code, string detail = null)` |
| propriedade | `string Code { get; }` |

### `OptionalValue`

Record struct em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `OptionalValue(Presence Presence, T Value)` |
| propriedade | `Presence Presence { get; init; }` |
| propriedade | `T Value { get; init; }` |
| propriedade | `bool IsSet { get; }` |
| propriedade estática | `OptionalValue<T> Unset { get; }` |
| método estático | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| constante | `string Eligible = "ELIGIBLE"` |
| constante | `string Rejected = "REJECTED"` |
| constante | `string ActivationRequested = "requested"` |
| constante | `string ActivationSettingsFile = "settings-file"` |
| constante | `string ActivationContent = "content"` |
| constante | `string ActivationDefault = "default"` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Version { get; init; }` |
| propriedade | `string Status { get; init; }` |
| propriedade | `bool Requested { get; init; }` |
| propriedade | `string FailurePolicy { get; init; }` |
| propriedade | `IReadOnlyList<string> Operations { get; init; }` |
| propriedade | `string Code { get; init; }` |
| propriedade | `string Reason { get; init; }` |
| propriedade | `string ManifestSha256 { get; init; }` |
| propriedade | `string Activation { get; init; }` |
| propriedade | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| propriedade | `bool IsEligible { get; }` |
| propriedade | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| propriedade | `string Key { get; init; }` |
| propriedade | `string Value { get; init; }` |
| propriedade | `string Source { get; init; }` |

### `PlannedMutation`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| propriedade | `string RelativePath { get; init; }` |
| propriedade | `string SemanticKey { get; init; }` |
| propriedade | `string RequestedValue { get; init; }` |
| propriedade | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| propriedade | `OptionalValue<string> Model { get; init; }` |
| propriedade | `OptionalValue<string> Repaint { get; init; }` |
| propriedade | `OptionalValue<string> Hof { get; init; }` |
| propriedade | `OptionalValue<string> FleetNumber { get; init; }` |
| propriedade | `OptionalValue<string> Registration { get; init; }` |
| propriedade | `bool Enabled { get; }` |

### `Presence`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Set = 1` |

### `PublicCapabilityClassification`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [capabilities](capabilities.md).

| Membro | Assinatura |
| --- | --- |
| valor | `PublicStableBeta = 0` |
| valor | `PublicExperimental = 1` |
| valor | `InternalOnly = 2` |
| valor | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [capabilities](capabilities.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Family { get; init; }` |
| propriedade | `PublicCapabilityClassification Classification { get; init; }` |
| propriedade | `PublicCapabilityKind Kind { get; init; }` |
| propriedade | `bool RequiresSession { get; init; }` |
| propriedade | `bool RequiresExactProfile { get; init; }` |
| propriedade | `string ApiRoute { get; init; }` |
| propriedade | `string CliRoute { get; init; }` |
| propriedade | `string RuntimeValidation { get; init; }` |
| propriedade | `string Description { get; init; }` |
| propriedade | `IReadOnlyList<string> HandleTypes { get; init; }` |
| propriedade | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| propriedade | `string NativeCapability { get; init; }` |
| propriedade | `string EvidenceNote { get; init; }` |
| propriedade | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| propriedade | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [capabilities](capabilities.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Read = 0` |
| valor | `Write = 1` |
| valor | `Action = 2` |
| valor | `Event = 3` |

### `PublicCapabilityRegistry`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [capabilities](capabilities.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string ProtocolVersion = "0.1"` |
| campo estático | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| propriedade estática | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| propriedade estática | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| propriedade estática | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| método estático | `PublicCapabilityDescriptor Get(string id)` |
| método estático | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| método estático | `bool IsInternalResultKey(string key)` |
| método estático | `bool IsModuleOperation(string operation)` |
| método estático | `bool IsPublicRuntimeOperation(string operation)` |
| método estático | `string NativeCapabilityOf(string operation)` |
| método estático | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| método estático | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| método estático | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [errors](errors.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string InvalidArgument = "invalid_argument"` |
| constante | `string UnsupportedProfile = "unsupported_profile"` |
| constante | `string Session = "session"` |
| constante | `string Runtime = "runtime"` |
| constante | `string NotFound = "not_found"` |
| constante | `string Transaction = "transaction"` |
| constante | `string Internal = "internal"` |

### `PublicErrorCodes`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [errors](errors.md).

| Membro | Assinatura |
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
| campo estático | `IReadOnlyList<PublicErrorDescriptor> All` |
| método estático | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [errors](errors.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PublicErrorDescriptor(string Code, string Category)` |
| propriedade | `string Code { get; init; }` |
| propriedade | `string Category { get; init; }` |

### `PublicEvidence`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| constante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| constante | `string Partial = "PARTIAL"` |
| constante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| constante | `string Unavailable = "UNAVAILABLE"` |
| campo estático | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enum (`int`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [exit-codes](exit-codes.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Success = 0` |
| valor | `SessionFailed = 1` |
| valor | `InvalidArguments = 2` |
| valor | `UnsupportedProfile = 3` |
| valor | `NoActiveSession = 4` |
| valor | `RuntimeUnavailable = 5` |
| valor | `NotFound = 6` |
| valor | `OperationRejected = 7` |
| valor | `TransactionRecoveryFailed = 8` |
| valor | `InternalError = 10` |

### `PublicMutationPolicy`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string None = "none"` |
| constante | `string OmsiState = "omsi-state"` |
| constante | `string SessionResource = "session-resource"` |
| constante | `string SessionPolicy = "session-policy"` |
| campo estático | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| propriedade | `string Name { get; init; }` |
| propriedade | `bool Required { get; init; }` |
| propriedade | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| propriedade | `bool Accepted { get; init; }` |
| propriedade | `string ErrorCode { get; init; }` |
| propriedade | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string NativeCapability { get; init; }` |
| propriedade | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| propriedade | `IReadOnlyList<string> CliRoutes { get; init; }` |
| propriedade | `bool Paged { get; init; }` |
| propriedade | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| propriedade | `bool Pending { get; init; }` |
| propriedade | `bool Recovered { get; init; }` |
| propriedade | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [runtime-control](runtime-control.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| propriedade | `Guid SessionId { get; init; }` |
| propriedade | `ulong RequestId { get; init; }` |
| propriedade | `string Operation { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [runtime-control](runtime-control.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| propriedade | `Guid SessionId { get; init; }` |
| propriedade | `ulong RequestId { get; init; }` |
| propriedade | `bool Succeeded { get; init; }` |
| propriedade | `string ErrorCode { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `uint Magic = 1330401859` |
| constante | `ushort Version = 1` |
| constante | `int HeaderSize = 72` |
| método estático | `byte[] SerializeRequest(RuntimeCommand command)` |
| método estático | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| método estático | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| método estático | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| método estático | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| propriedade | `string Type { get; init; }` |
| propriedade | `DateTimeOffset TimestampUtc { get; init; }` |
| propriedade | `long Sequence { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| propriedade | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| propriedade | `string Key { get; init; }` |
| propriedade | `string Type { get; init; }` |
| propriedade | `string Default { get; init; }` |
| propriedade | `bool AllowsAuto { get; init; }` |
| propriedade | `string Minimum { get; init; }` |
| propriedade | `string Maximum { get; init; }` |
| propriedade | `IReadOnlyList<string> Choices { get; init; }` |
| propriedade | `string Flag { get; init; }` |
| propriedade | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Version { get; init; }` |
| propriedade | `string Name { get; init; }` |
| propriedade | `string Activation { get; init; }` |
| propriedade | `bool IsValid { get; init; }` |
| propriedade | `IReadOnlyList<string> Operations { get; init; }` |
| propriedade | `string ContentBindingMap { get; init; }` |
| propriedade | `string ErrorCode { get; init; }` |
| propriedade | `string ErrorMessage { get; init; }` |
| propriedade | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| propriedade | `IReadOnlyList<string> Enable { get; init; }` |
| propriedade | `IReadOnlyList<string> Disable { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propriedade | `IReadOnlyList<string> EffectiveDisable { get; }` |
| propriedade | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| propriedade | `string OsFamily { get; init; }` |
| propriedade | `string OsVersion { get; init; }` |
| propriedade | `string OsArchitecture { get; init; }` |
| propriedade | `string HostArchitecture { get; init; }` |
| propriedade | `string OmsiArchitecture { get; init; }` |
| propriedade | `string PluginArchitecture { get; init; }` |
| propriedade | `bool CurrentPlatformSupported { get; init; }` |
| propriedade | `bool LegacyPlatform { get; init; }` |
| propriedade | `bool Wow64Available { get; init; }` |
| propriedade | `bool InstallationWritable { get; init; }` |
| propriedade | `bool ProcessLaunchSupported { get; init; }` |
| propriedade | `bool PluginRuntimeSupported { get; init; }` |
| propriedade | `bool NativeInteropSupported { get; init; }` |
| propriedade | `bool SharedMemorySupported { get; init; }` |
| propriedade | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SemanticDate(int Year, int Month, int Day)` |
| propriedade | `int Year { get; init; }` |
| propriedade | `int Month { get; init; }` |
| propriedade | `int Day { get; init; }` |

### `SemanticTime`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SemanticTime(int Hour, int Minute, int Second)` |
| propriedade | `int Hour { get; init; }` |
| propriedade | `int Minute { get; init; }` |
| propriedade | `int Second { get; init; }` |

### `SessionHandle`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionHandle(Guid SessionId)` |
| propriedade | `Guid SessionId { get; init; }` |

### `SessionPlan`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| propriedade | `Guid SessionId { get; init; }` |
| propriedade | `string BuildProfileId { get; init; }` |
| propriedade | `LaunchSpec Spec { get; init; }` |
| propriedade | `RuntimePlatformInfo Platform { get; init; }` |
| propriedade | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| propriedade | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| propriedade | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| propriedade | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| propriedade | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| propriedade | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| propriedade | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propriedade | `bool IsRunnable { get; init; }` |
| propriedade | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| propriedade | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| propriedade | `SplashMode Splash { get; init; }` |
| propriedade | `OptionalValue<string> Language { get; init; }` |
| propriedade | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| propriedade | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Name { get; init; }` |
| propriedade | `string Version { get; init; }` |
| propriedade | `string Author { get; init; }` |
| propriedade | `string PresetId { get; init; }` |
| propriedade | `int PresetIndex { get; init; }` |
| propriedade | `string PresetName { get; init; }` |
| propriedade | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| propriedade | `int Index { get; init; }` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Name { get; init; }` |

### `SessionProfileSummary`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Name { get; init; }` |
| propriedade | `string Version { get; init; }` |
| propriedade | `string Author { get; init; }` |
| propriedade | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propriedade | `string MapIdentity { get; init; }` |
| propriedade | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| propriedade | `bool IsValid { get; init; }` |
| propriedade | `string ErrorCode { get; init; }` |
| propriedade | `string ErrorMessage { get; init; }` |

### `SessionState`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Created = 0` |
| valor | `ValidatingPlatform = 1` |
| valor | `Planning = 2` |
| valor | `AcquiringInstallationLock = 3` |
| valor | `RecoveringPreviousTransaction = 4` |
| valor | `Snapshotting = 5` |
| valor | `ApplyingConfiguration = 6` |
| valor | `DeployingRuntime = 7` |
| valor | `CreatingStartupHandoff = 8` |
| valor | `StartingProcess = 9` |
| valor | `WaitingForPlugin = 10` |
| valor | `PluginBootstrap = 11` |
| valor | `StartingWorld = 12` |
| valor | `EnteringGameplay = 13` |
| valor | `Running = 14` |
| valor | `ProcessExited = 15` |
| valor | `Restoring = 16` |
| valor | `CleaningRuntime = 17` |
| valor | `Completed = 18` |
| valor | `Failed = 19` |

### `SessionStatus`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| propriedade | `Guid SessionId { get; init; }` |
| propriedade | `SessionState State { get; init; }` |
| propriedade | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propriedade | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Native = 0` |
| valor | `Managed = 1` |

### `StartDateTime`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| propriedade | `int? Year { get; init; }` |
| propriedade | `int? DayOfYear { get; init; }` |
| propriedade | `int? Hour { get; init; }` |
| propriedade | `int? Minute { get; init; }` |
| propriedade | `int? Second { get; init; }` |
| propriedade | `bool IsEmpty { get; }` |

### `StartWeather`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| propriedade | `WeatherMode Mode { get; init; }` |
| propriedade | `string Preset { get; init; }` |
| propriedade | `string Icao { get; init; }` |

### `StartupHandoff`

Record selado em `OmsiLaunch.Api`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| propriedade | `Guid SessionId { get; init; }` |
| propriedade | `string BuildProfileId { get; init; }` |
| propriedade | `WorldMode WorldMode { get; init; }` |
| propriedade | `string MapIdentity { get; init; }` |
| propriedade | `int PresentedEntrypointIndex { get; init; }` |
| propriedade | `bool HeadlessStart { get; init; }` |
| propriedade | `bool PlayerVehicleEnabled { get; init; }` |
| propriedade | `DateTimeMode DateMode { get; init; }` |
| propriedade | `DateTimeMode TimeMode { get; init; }` |
| propriedade | `string EntrypointIdentity { get; init; }` |
| propriedade | `string SituationIdentity { get; init; }` |
| propriedade | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| propriedade | `bool PluginTrace { get; init; }` |
| propriedade | `bool NativeTrace { get; init; }` |
| propriedade | `StartDateTime Start { get; init; }` |
| propriedade | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `uint Magic = 1330402120` |
| constante | `ushort Version = 7` |
| método estático | `byte[] Serialize(StartupHandoff value)` |
| método estático | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| propriedade | `string RelativeDirectory { get; init; }` |
| propriedade | `string ManifestSha256 { get; init; }` |
| propriedade | `string Settings { get; init; }` |

### `TelemetryRingWire`

Classe estática em `OmsiLaunch.Api`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `uint Magic = 1381256271` |
| constante | `int Version = 1` |
| constante | `int SlotCount = 64` |
| constante | `int SlotSize = 2048` |
| constante | `int HeaderSize = 64` |
| constante | `int Capacity = 131136` |
| constante | `int MaxPayload = 2036` |
| método estático | `void Initialize(MemoryMappedViewAccessor view)` |
| método estático | `bool IsRing(MemoryMappedViewAccessor view)` |
| método estático | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| método estático | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| propriedade | `DateTimeMode Mode { get; init; }` |
| propriedade | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enum (`byte`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Preset = 1` |
| valor | `Icao = 2` |
| valor | `RealCurrent = 3` |

### `WeatherSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| propriedade | `WeatherMode Mode { get; init; }` |
| propriedade | `OptionalValue<string> Preset { get; init; }` |
| propriedade | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enum (`int`) em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| valor | `NewMap = 0` |
| valor | `SavedSituation = 1` |
| valor | `LastMapState = 2` |
| valor | `LastSituation = 2` |

### `WorldSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `STABLE_BETA`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| propriedade | `WorldMode Mode { get; init; }` |
| propriedade | `OptionalValue<string> MapIdentity { get; init; }` |
| propriedade | `OptionalValue<string> SituationIdentity { get; init; }` |
| propriedade | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| propriedade | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| propriedade | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Record selado em `OmsiLaunch.Api`. Estabilidade: `PARTIAL`. Semântica: [launchspec](launchspec.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| propriedade | `DateTimeMode Mode { get; init; }` |
| propriedade | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Classe selada em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `HofPlanningException(string code, string message)` |
| propriedade | `string Code { get; }` |

### `LaunchValidation`

Classe estática em `OmsiLaunch.Core`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método estático | `bool IsMapIdentity(string value)` |
| método estático | `bool IsSha256(string value)` |
| método estático | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Classe estática em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método estático | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Record selado em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| propriedade | `string PluginBuildDirectory { get; init; }` |
| propriedade | `string NativeBridgePath { get; init; }` |
| propriedade | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Classe selada em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| método | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| método | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| método | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| método | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| método | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| método | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| método | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Record selado em `OmsiLaunch.Core`. Estabilidade: `EXPERIMENTAL`. Semântica: [session-profiles](session-profiles.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| propriedade | `string Map { get; init; }` |
| propriedade | `int? EntrypointIndex { get; init; }` |
| propriedade | `string EntrypointIdentity { get; init; }` |
| propriedade | `SemanticDate Date { get; init; }` |
| propriedade | `SemanticTime Time { get; init; }` |
| propriedade | `int? Year { get; init; }` |
| propriedade | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Record selado em `OmsiLaunch.Core`. Estabilidade: `EXPERIMENTAL`. Semântica: [session-profiles](session-profiles.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| propriedade | `int Index { get; init; }` |
| propriedade | `string Id { get; init; }` |
| propriedade | `string Name { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propriedade | `SessionPresentationSpec Presentation { get; init; }` |
| propriedade | `InternetTexturesSpec InternetTextures { get; init; }` |
| propriedade | `LaunchBehaviorSpec Behavior { get; init; }` |
| propriedade | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| propriedade | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Classe selada em `OmsiLaunch.Core`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionPlanner(IRuntimePlatform platform)` |
| método | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Classe estática em `OmsiLaunch.Core`. Estabilidade: `EXPERIMENTAL`. Semântica: [session-profiles](session-profiles.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string Schema = "omsilaunch.session-profile/v1"` |
| constante | `long MaxBytes = 262144` |
| constante | `int MinPresetIndex = 1` |
| constante | `int MaxPresetIndex = 99` |
| campo estático | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| método estático | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| método estático | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| método estático | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| método estático | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Classe selada em `OmsiLaunch.Core`. Estabilidade: `EXPERIMENTAL`. Semântica: [session-profiles](session-profiles.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionProfileException(string code, string message)` |
| propriedade | `string Code { get; }` |

### `SessionProfilePackage`

Record selado em `OmsiLaunch.Core`. Estabilidade: `EXPERIMENTAL`. Semântica: [session-profiles](session-profiles.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| propriedade | `string RootPath { get; init; }` |
| propriedade | `SessionProfileMetadata Metadata { get; init; }` |
| propriedade | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propriedade | `ProfileNew New { get; init; }` |
| propriedade | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Classe estática em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `int MinimumYear = 1900` |
| constante | `int MaximumYear = 2200` |
| método estático | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Classe estática em `OmsiLaunch.Core`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método estático | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| propriedade | `string Name { get; }` |
| propriedade | `Guid SessionId { get; }` |
| método estático | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| método | `void Dispose()` |
| método | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| propriedade | `string Name { get; }` |
| propriedade | `Guid SessionId { get; }` |
| método estático | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| método | `void Dispose()` |

### `CurrentTelemetryStore`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| propriedade | `string Name { get; }` |
| método estático | `CurrentTelemetryStore Create(Guid sessionId)` |
| método | `void Dispose()` |
| método | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `CurrentWindowsX64Platform()` |
| método | `RuntimePlatformInfo Detect(string root)` |
| método | `bool HasExited(LaunchedProcess p)` |
| método | `bool IsInstallationWritable(string root)` |
| método | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| método | `void Terminate(LaunchedProcess p)` |
| método | `void ValidateCurrent(RuntimePlatformInfo p)` |
| método | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interface em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método | `OmsiProcessState Observe()` |
| método | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| método | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interface em `OmsiLaunch.Process`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método | `RuntimePlatformInfo Detect(string installationRoot)` |
| método | `bool HasExited(LaunchedProcess process)` |
| método | `bool IsInstallationWritable(string installationRoot)` |
| método | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| método | `void Terminate(LaunchedProcess process)` |
| método | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| método | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| método estático | `InstallationLease Acquire(string installationRoot)` |
| método | `void Dispose()` |
| método estático | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| propriedade | `int ProcessId { get; }` |
| propriedade | `int ThreadId { get; }` |
| propriedade | `ProcessIdentity Identity { get; }` |
| método | `void Dispose()` |

### `ModuleSessionProcess`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `STABLE_BETA`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `int MaxLineBytes = 65536` |
| propriedade | `int ProcessId { get; }` |
| propriedade | `bool HasExited { get; }` |
| propriedade | `int? ExitCode { get; }` |
| método | `void Dispose()` |
| método | `void Kill()` |
| método estático | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| método | `bool TryWriteLine(string line)` |
| método | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Record selado em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| propriedade | `string State { get; init; }` |
| propriedade | `int? ProcessId { get; init; }` |
| propriedade | `bool Responding { get; init; }` |

### `ProcessIdentity`

Record selado em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| propriedade | `int ProcessId { get; init; }` |
| propriedade | `DateTimeOffset CreationTimeUtc { get; init; }` |
| propriedade | `string ExecutablePath { get; init; }` |
| propriedade | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Classe estática em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| constante | `string FileName = "release-manifest.json"` |
| método estático | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| método estático | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Record selado em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| propriedade | `string SourcePath { get; init; }` |
| propriedade | `string DestinationRelativePath { get; init; }` |
| propriedade | `string Sha256 { get; init; }` |
| propriedade | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Classe selada em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| propriedade | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| propriedade | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| propriedade | `string IntegrityReference { get; }` |
| método estático | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| método | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Record selado em `OmsiLaunch.Process`. Estabilidade: `INTERNAL`. Semântica: [public-api](public-api.md).

| Membro | Assinatura |
| --- | --- |
| construtor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| propriedade | `string ExecutablePath { get; init; }` |
| propriedade | `string WorkingDirectory { get; init; }` |
| propriedade | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
