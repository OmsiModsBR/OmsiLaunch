# Inventario dell’API pubblica

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> Traduzione della [pagina originale in inglese](../../../reference/public-api-inventory.md) di OmsiLaunch 0.1.0-beta4. La pagina inglese è normativa: in caso di differenze prevalgono la pagina inglese e il codice.

Questa pagina è generata dagli assembly compilati tramite `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); il controllo della documentazione fallisce quando non corrisponde più al codice. Elenca ogni tipo esportato da `OmsiLaunch.Api`, `OmsiLaunch.Core` e `OmsiLaunch.Process`, con la sua stabilità e la firma di ogni membro pubblico. Semantica, precondizioni, errori ed esempi sono documentati nella pagina indicata in ciascun titolo (predefinita: [API pubblica](public-api.md)). Vocabolario di stabilità: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (pubblico per motivi tecnici, non è una superficie di integrazione), `UNAVAILABLE` (vedere [API pubblica](public-api.md#stability-vocabulary)). I membri dei record generati dal compilatore (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) sono omessi.

## `OmsiLaunch.Api`

### `Capability`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| proprietà | `string Name { get; init; }` |
| proprietà | `bool Available { get; init; }` |
| proprietà | `string EvidenceState { get; init; }` |
| proprietà | `string Reason { get; init; }` |

### `ContentIdentity`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| proprietà | `string Identity { get; init; }` |
| proprietà | `string Kind { get; init; }` |
| proprietà | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `Maps = 0` |
| valore | `Situations = 1` |
| valore | `Vehicles = 2` |
| valore | `Repaints = 3` |
| valore | `Hofs = 4` |
| valore | `FleetNumbers = 5` |
| valore | `Registrations = 6` |
| valore | `Addons = 7` |
| valore | `Entrypoints = 8` |
| valore | `SessionProfiles = 9` |
| valore | `RuntimeModules = 10` |

### `D3DDeviceState`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `NotReady = 0` |
| valore | `Ready = 1` |
| valore | `Lost = 2` |
| valore | `Resetting = 3` |
| valore | `Stopping = 4` |
| valore | `Stopped = 5` |

### `D3DDeviceStatus`

Record sealed in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| proprietà | `bool Available { get; init; }` |
| proprietà | `D3DDeviceState State { get; init; }` |
| proprietà | `uint Generation { get; init; }` |
| proprietà | `uint LiveTextureCount { get; init; }` |
| proprietà | `bool ResetHookInstalled { get; init; }` |
| proprietà | `uint ExecutionThreadId { get; init; }` |
| proprietà | `uint LastResetThreadId { get; init; }` |
| proprietà | `int QueryInterfaceHResult { get; init; }` |
| proprietà | `int CooperativeLevelHResult { get; init; }` |
| proprietà | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Classe statica in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo di estensione | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metodo di estensione | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metodo di estensione | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metodo di estensione | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metodo di estensione | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Record sealed in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| proprietà | `D3DTextureHandle Handle { get; init; }` |
| proprietà | `D3DTextureResourceState State { get; init; }` |
| proprietà | `D3DDeviceState DeviceState { get; init; }` |
| proprietà | `uint Generation { get; init; }` |
| proprietà | `uint Width { get; init; }` |
| proprietà | `uint Height { get; init; }` |
| proprietà | `D3DTextureFormat Format { get; init; }` |
| proprietà | `uint Levels { get; init; }` |
| proprietà | `uint Level { get; init; }` |
| proprietà | `uint LevelWidth { get; init; }` |
| proprietà | `uint LevelHeight { get; init; }` |
| proprietà | `int HResult { get; init; }` |
| proprietà | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `A8R8G8B8 = 0` |
| valore | `X8R8G8B8 = 1` |
| valore | `R5G6B5 = 2` |
| valore | `X1R5G5B5 = 3` |
| valore | `A1R5G5B5 = 4` |
| valore | `A4R4G4B4 = 5` |
| valore | `A8 = 6` |
| valore | `L8 = 7` |
| valore | `A8L8 = 8` |

### `D3DTextureHandle`

Record sealed in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `D3DTextureHandle(string Value)` |
| proprietà | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `Live = 0` |
| valore | `Released = 1` |
| valore | `Stale = 2` |

### `D3DTextureUpdate`

Record sealed in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| proprietà | `uint Level { get; init; }` |
| proprietà | `uint X { get; init; }` |
| proprietà | `uint Y { get; init; }` |
| proprietà | `uint Width { get; init; }` |
| proprietà | `uint Height { get; init; }` |
| proprietà | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| proprietà | `DateTimeMode Mode { get; init; }` |
| proprietà | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Unset = 0` |
| valore | `Explicit = 1` |
| valore | `System = 2` |

### `DiagnosticsSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| proprietà | `bool Log { get; init; }` |
| proprietà | `bool Verbose { get; init; }` |
| proprietà | `bool OmsiLogAll { get; init; }` |
| proprietà | `bool ProcessTrace { get; init; }` |
| proprietà | `bool PluginTrace { get; init; }` |
| proprietà | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Unset = 0` |
| valore | `PresentedIndex = 1` |
| valore | `Identity = 2` |

### `EntrypointSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| proprietà | `EntrypointMode Mode { get; init; }` |
| proprietà | `OptionalValue<int> PresentedIndex { get; init; }` |
| proprietà | `OptionalValue<string> Identity { get; init; }` |
| proprietà statica | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `Add = 0` |
| valore | `Mirror = 1` |

### `HofManagementSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| proprietà | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| proprietà | `HofSourceKind SourceKind { get; init; }` |
| proprietà | `string Source { get; init; }` |
| proprietà | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| proprietà | `HofApplyMode Mode { get; init; }` |
| proprietà | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `Collection = 0` |
| valore | `Directory = 1` |
| valore | `Vehicle = 2` |
| valore | `File = 3` |

### `IOmsiLaunch`

Interfaccia in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| metodo | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| metodo | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| metodo | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| proprietà | `OptionalValue<string> KeyboardDocument { get; init; }` |
| proprietà | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo statico | `string IdentityKey(string root)` |
| metodo statico | `string NormalizeRoot(string root)` |
| metodo statico | `IReadOnlyList<string> Segments(string relativePath)` |
| metodo statico | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| proprietà | `string RootPath { get; init; }` |
| proprietà | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Native = 0` |
| valore | `Disabled = 1` |
| valore | `Override = 2` |

### `InternetTexturesSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| proprietà | `InternetTexturesMode Mode { get; init; }` |
| proprietà | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| proprietà | `bool RestoreConfiguration { get; init; }` |
| proprietà | `bool SuppressStaleClosecheckWarning { get; init; }` |
| proprietà | `int StartupTimeoutSeconds { get; init; }` |
| proprietà | `int ShutdownTimeoutSeconds { get; init; }` |
| proprietà | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| proprietà | `string Code { get; init; }` |
| proprietà | `string Message { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| proprietà | `InstallationSpec Installation { get; init; }` |
| proprietà | `WorldSpec World { get; init; }` |
| proprietà | `DateSpec Date { get; init; }` |
| proprietà | `TimeSpec Time { get; init; }` |
| proprietà | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| proprietà | `EnvironmentSpec Environment { get; init; }` |
| proprietà | `LaunchBehaviorSpec Behavior { get; init; }` |
| proprietà | `YearSpec Year { get; init; }` |
| proprietà | `WeatherSpec Weather { get; init; }` |
| proprietà | `InputSpec Input { get; init; }` |
| proprietà | `DiagnosticsSpec Diagnostics { get; init; }` |
| proprietà | `SessionPresentationSpec Presentation { get; init; }` |
| proprietà | `InternetTexturesSpec InternetTextures { get; init; }` |
| proprietà | `SessionProfileMetadata SessionProfile { get; init; }` |
| proprietà | `HofManagementSpec HofManagement { get; init; }` |
| proprietà | `RuntimeModulesSpec Modules { get; init; }` |
| proprietà | `HofManagementSpec EffectiveHofManagement { get; }` |
| proprietà | `RuntimeModulesSpec EffectiveModules { get; }` |
| proprietà | `YearSpec EffectiveYear { get; }` |
| proprietà | `WeatherSpec EffectiveWeather { get; }` |
| proprietà | `InputSpec EffectiveInput { get; }` |
| proprietà | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| proprietà | `SessionPresentationSpec EffectivePresentation { get; }` |
| proprietà | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Classe sealed in `OmsiLaunch.Api`. Stabilità: `EXPERIMENTAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `OmsiRuntimeException(string code, string detail = null)` |
| proprietà | `string Code { get; }` |

### `OptionalValue`

Record struct in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `OptionalValue(Presence Presence, T Value)` |
| proprietà | `Presence Presence { get; init; }` |
| proprietà | `T Value { get; init; }` |
| proprietà | `bool IsSet { get; }` |
| proprietà statica | `OptionalValue<T> Unset { get; }` |
| metodo statico | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| costante | `string Eligible = "ELIGIBLE"` |
| costante | `string Rejected = "REJECTED"` |
| costante | `string ActivationRequested = "requested"` |
| costante | `string ActivationSettingsFile = "settings-file"` |
| costante | `string ActivationContent = "content"` |
| costante | `string ActivationDefault = "default"` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Version { get; init; }` |
| proprietà | `string Status { get; init; }` |
| proprietà | `bool Requested { get; init; }` |
| proprietà | `string FailurePolicy { get; init; }` |
| proprietà | `IReadOnlyList<string> Operations { get; init; }` |
| proprietà | `string Code { get; init; }` |
| proprietà | `string Reason { get; init; }` |
| proprietà | `string ManifestSha256 { get; init; }` |
| proprietà | `string Activation { get; init; }` |
| proprietà | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| proprietà | `bool IsEligible { get; }` |
| proprietà | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PlannedModuleSetting(string Key, string Value, string Source)` |
| proprietà | `string Key { get; init; }` |
| proprietà | `string Value { get; init; }` |
| proprietà | `string Source { get; init; }` |

### `PlannedMutation`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| proprietà | `string RelativePath { get; init; }` |
| proprietà | `string SemanticKey { get; init; }` |
| proprietà | `string RequestedValue { get; init; }` |
| proprietà | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| proprietà | `OptionalValue<string> Model { get; init; }` |
| proprietà | `OptionalValue<string> Repaint { get; init; }` |
| proprietà | `OptionalValue<string> Hof { get; init; }` |
| proprietà | `OptionalValue<string> FleetNumber { get; init; }` |
| proprietà | `OptionalValue<string> Registration { get; init; }` |
| proprietà | `bool Enabled { get; }` |

### `Presence`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Unset = 0` |
| valore | `Set = 1` |

### `PublicCapabilityClassification`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [capabilities](capabilities.md).

| Membro | Firma |
| --- | --- |
| valore | `PublicStableBeta = 0` |
| valore | `PublicExperimental = 1` |
| valore | `InternalOnly = 2` |
| valore | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [capabilities](capabilities.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Family { get; init; }` |
| proprietà | `PublicCapabilityClassification Classification { get; init; }` |
| proprietà | `PublicCapabilityKind Kind { get; init; }` |
| proprietà | `bool RequiresSession { get; init; }` |
| proprietà | `bool RequiresExactProfile { get; init; }` |
| proprietà | `string ApiRoute { get; init; }` |
| proprietà | `string CliRoute { get; init; }` |
| proprietà | `string RuntimeValidation { get; init; }` |
| proprietà | `string Description { get; init; }` |
| proprietà | `IReadOnlyList<string> HandleTypes { get; init; }` |
| proprietà | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| proprietà | `string NativeCapability { get; init; }` |
| proprietà | `string EvidenceNote { get; init; }` |
| proprietà | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| proprietà | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [capabilities](capabilities.md).

| Membro | Firma |
| --- | --- |
| valore | `Read = 0` |
| valore | `Write = 1` |
| valore | `Action = 2` |
| valore | `Event = 3` |

### `PublicCapabilityRegistry`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [capabilities](capabilities.md).

| Membro | Firma |
| --- | --- |
| costante | `string ProtocolVersion = "0.1"` |
| campo statico | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| proprietà statica | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| proprietà statica | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| proprietà statica | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| metodo statico | `PublicCapabilityDescriptor Get(string id)` |
| metodo statico | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| metodo statico | `bool IsInternalResultKey(string key)` |
| metodo statico | `bool IsModuleOperation(string operation)` |
| metodo statico | `bool IsPublicRuntimeOperation(string operation)` |
| metodo statico | `string NativeCapabilityOf(string operation)` |
| metodo statico | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| metodo statico | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| metodo statico | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [errors](errors.md).

| Membro | Firma |
| --- | --- |
| costante | `string InvalidArgument = "invalid_argument"` |
| costante | `string UnsupportedProfile = "unsupported_profile"` |
| costante | `string Session = "session"` |
| costante | `string Runtime = "runtime"` |
| costante | `string NotFound = "not_found"` |
| costante | `string Transaction = "transaction"` |
| costante | `string Internal = "internal"` |

### `PublicErrorCodes`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [errors](errors.md).

| Membro | Firma |
| --- | --- |
| costante | `string CANCELLED = "OL_E_CANCELLED"` |
| costante | `string INTERNAL = "OL_E_INTERNAL"` |
| costante | `string TIMEOUT = "OL_E_TIMEOUT"` |
| costante | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| costante | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| costante | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| costante | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| costante | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| costante | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| costante | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| costante | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| costante | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| costante | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| costante | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| costante | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| costante | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| costante | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| costante | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| costante | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| costante | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| costante | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| costante | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| costante | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| costante | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| costante | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| costante | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| costante | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| costante | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| costante | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| costante | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| costante | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| costante | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| costante | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| costante | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| costante | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| costante | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| costante | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| costante | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| costante | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| costante | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| costante | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| costante | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| costante | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| costante | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| costante | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| costante | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| costante | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| costante | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| costante | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| costante | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| costante | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| costante | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| costante | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| costante | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| costante | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| costante | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| costante | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| costante | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| costante | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| costante | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| costante | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| costante | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| costante | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| costante | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| costante | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| costante | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| costante | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| costante | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| costante | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| costante | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| costante | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| costante | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| costante | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| costante | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| costante | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| costante | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| costante | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| costante | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| costante | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| costante | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| costante | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| costante | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| costante | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| costante | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| costante | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| costante | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| costante | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| costante | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| costante | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| costante | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| costante | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| costante | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| costante | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| costante | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| costante | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| costante | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| costante | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| costante | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| costante | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| costante | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| costante | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| costante | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| costante | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| costante | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| costante | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| costante | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| costante | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| costante | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| costante | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| costante | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| costante | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| costante | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| costante | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| costante | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| costante | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| costante | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| costante | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| costante | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| costante | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| costante | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| costante | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| costante | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| costante | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| costante | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| costante | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| costante | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| costante | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| costante | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| costante | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| costante | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| costante | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| costante | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| costante | `string START_SESSION = "OL_E_START_SESSION"` |
| costante | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| costante | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| costante | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| costante | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| costante | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| costante | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| costante | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| costante | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| costante | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| costante | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| costante | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| costante | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| costante | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| costante | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| costante | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| costante | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| costante | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| costante | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| costante | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| costante | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| costante | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| costante | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| costante | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| costante | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| costante | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| costante | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| costante | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| costante | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| costante | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| costante | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| costante | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| costante | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| costante | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| costante | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| costante | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| costante | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| costante | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| costante | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| costante | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| costante | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| costante | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| costante | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| costante | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| costante | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| campo statico | `IReadOnlyList<PublicErrorDescriptor> All` |
| metodo statico | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [errors](errors.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PublicErrorDescriptor(string Code, string Category)` |
| proprietà | `string Code { get; init; }` |
| proprietà | `string Category { get; init; }` |

### `PublicEvidence`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| costante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| costante | `string Partial = "PARTIAL"` |
| costante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| costante | `string Unavailable = "UNAVAILABLE"` |
| campo statico | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enumerazione (`int`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [exit-codes](exit-codes.md).

| Membro | Firma |
| --- | --- |
| valore | `Success = 0` |
| valore | `SessionFailed = 1` |
| valore | `InvalidArguments = 2` |
| valore | `UnsupportedProfile = 3` |
| valore | `NoActiveSession = 4` |
| valore | `RuntimeUnavailable = 5` |
| valore | `NotFound = 6` |
| valore | `OperationRejected = 7` |
| valore | `TransactionRecoveryFailed = 8` |
| valore | `InternalError = 10` |

### `PublicMutationPolicy`

Classe statica in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `string None = "none"` |
| costante | `string OmsiState = "omsi-state"` |
| costante | `string SessionResource = "session-resource"` |
| costante | `string SessionPolicy = "session-policy"` |
| campo statico | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| proprietà | `string Name { get; init; }` |
| proprietà | `bool Required { get; init; }` |
| proprietà | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| proprietà | `bool Accepted { get; init; }` |
| proprietà | `string ErrorCode { get; init; }` |
| proprietà | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string NativeCapability { get; init; }` |
| proprietà | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| proprietà | `IReadOnlyList<string> CliRoutes { get; init; }` |
| proprietà | `bool Paged { get; init; }` |
| proprietà | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| proprietà | `bool Pending { get; init; }` |
| proprietà | `bool Recovered { get; init; }` |
| proprietà | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [runtime-control](runtime-control.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| proprietà | `Guid SessionId { get; init; }` |
| proprietà | `ulong RequestId { get; init; }` |
| proprietà | `string Operation { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [runtime-control](runtime-control.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| proprietà | `Guid SessionId { get; init; }` |
| proprietà | `ulong RequestId { get; init; }` |
| proprietà | `bool Succeeded { get; init; }` |
| proprietà | `string ErrorCode { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Classe statica in `OmsiLaunch.Api`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `uint Magic = 1330401859` |
| costante | `ushort Version = 1` |
| costante | `int HeaderSize = 72` |
| metodo statico | `byte[] SerializeRequest(RuntimeCommand command)` |
| metodo statico | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| metodo statico | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| metodo statico | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| metodo statico | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| proprietà | `string Type { get; init; }` |
| proprietà | `DateTimeOffset TimestampUtc { get; init; }` |
| proprietà | `long Sequence { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| proprietà | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| proprietà | `string Key { get; init; }` |
| proprietà | `string Type { get; init; }` |
| proprietà | `string Default { get; init; }` |
| proprietà | `bool AllowsAuto { get; init; }` |
| proprietà | `string Minimum { get; init; }` |
| proprietà | `string Maximum { get; init; }` |
| proprietà | `IReadOnlyList<string> Choices { get; init; }` |
| proprietà | `string Flag { get; init; }` |
| proprietà | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Version { get; init; }` |
| proprietà | `string Name { get; init; }` |
| proprietà | `string Activation { get; init; }` |
| proprietà | `bool IsValid { get; init; }` |
| proprietà | `IReadOnlyList<string> Operations { get; init; }` |
| proprietà | `string ContentBindingMap { get; init; }` |
| proprietà | `string ErrorCode { get; init; }` |
| proprietà | `string ErrorMessage { get; init; }` |
| proprietà | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| proprietà | `IReadOnlyList<string> Enable { get; init; }` |
| proprietà | `IReadOnlyList<string> Disable { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| proprietà | `IReadOnlyList<string> EffectiveDisable { get; }` |
| proprietà | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| proprietà | `string OsFamily { get; init; }` |
| proprietà | `string OsVersion { get; init; }` |
| proprietà | `string OsArchitecture { get; init; }` |
| proprietà | `string HostArchitecture { get; init; }` |
| proprietà | `string OmsiArchitecture { get; init; }` |
| proprietà | `string PluginArchitecture { get; init; }` |
| proprietà | `bool CurrentPlatformSupported { get; init; }` |
| proprietà | `bool LegacyPlatform { get; init; }` |
| proprietà | `bool Wow64Available { get; init; }` |
| proprietà | `bool InstallationWritable { get; init; }` |
| proprietà | `bool ProcessLaunchSupported { get; init; }` |
| proprietà | `bool PluginRuntimeSupported { get; init; }` |
| proprietà | `bool NativeInteropSupported { get; init; }` |
| proprietà | `bool SharedMemorySupported { get; init; }` |
| proprietà | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SemanticDate(int Year, int Month, int Day)` |
| proprietà | `int Year { get; init; }` |
| proprietà | `int Month { get; init; }` |
| proprietà | `int Day { get; init; }` |

### `SemanticTime`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SemanticTime(int Hour, int Minute, int Second)` |
| proprietà | `int Hour { get; init; }` |
| proprietà | `int Minute { get; init; }` |
| proprietà | `int Second { get; init; }` |

### `SessionHandle`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionHandle(Guid SessionId)` |
| proprietà | `Guid SessionId { get; init; }` |

### `SessionPlan`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| proprietà | `Guid SessionId { get; init; }` |
| proprietà | `string BuildProfileId { get; init; }` |
| proprietà | `LaunchSpec Spec { get; init; }` |
| proprietà | `RuntimePlatformInfo Platform { get; init; }` |
| proprietà | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| proprietà | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| proprietà | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| proprietà | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| proprietà | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| proprietà | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| proprietà | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| proprietà | `bool IsRunnable { get; init; }` |
| proprietà | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| proprietà | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| proprietà | `SplashMode Splash { get; init; }` |
| proprietà | `OptionalValue<string> Language { get; init; }` |
| proprietà | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| proprietà | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Name { get; init; }` |
| proprietà | `string Version { get; init; }` |
| proprietà | `string Author { get; init; }` |
| proprietà | `string PresetId { get; init; }` |
| proprietà | `int PresetIndex { get; init; }` |
| proprietà | `string PresetName { get; init; }` |
| proprietà | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| proprietà | `int Index { get; init; }` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Name { get; init; }` |

### `SessionProfileSummary`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Name { get; init; }` |
| proprietà | `string Version { get; init; }` |
| proprietà | `string Author { get; init; }` |
| proprietà | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| proprietà | `string MapIdentity { get; init; }` |
| proprietà | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| proprietà | `bool IsValid { get; init; }` |
| proprietà | `string ErrorCode { get; init; }` |
| proprietà | `string ErrorMessage { get; init; }` |

### `SessionState`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| valore | `Created = 0` |
| valore | `ValidatingPlatform = 1` |
| valore | `Planning = 2` |
| valore | `AcquiringInstallationLock = 3` |
| valore | `RecoveringPreviousTransaction = 4` |
| valore | `Snapshotting = 5` |
| valore | `ApplyingConfiguration = 6` |
| valore | `DeployingRuntime = 7` |
| valore | `CreatingStartupHandoff = 8` |
| valore | `StartingProcess = 9` |
| valore | `WaitingForPlugin = 10` |
| valore | `PluginBootstrap = 11` |
| valore | `StartingWorld = 12` |
| valore | `EnteringGameplay = 13` |
| valore | `Running = 14` |
| valore | `ProcessExited = 15` |
| valore | `Restoring = 16` |
| valore | `CleaningRuntime = 17` |
| valore | `Completed = 18` |
| valore | `Failed = 19` |

### `SessionStatus`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| proprietà | `Guid SessionId { get; init; }` |
| proprietà | `SessionState State { get; init; }` |
| proprietà | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| proprietà | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Unset = 0` |
| valore | `Native = 0` |
| valore | `Managed = 1` |

### `StartDateTime`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| proprietà | `int? Year { get; init; }` |
| proprietà | `int? DayOfYear { get; init; }` |
| proprietà | `int? Hour { get; init; }` |
| proprietà | `int? Minute { get; init; }` |
| proprietà | `int? Second { get; init; }` |
| proprietà | `bool IsEmpty { get; }` |

### `StartWeather`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| proprietà | `WeatherMode Mode { get; init; }` |
| proprietà | `string Preset { get; init; }` |
| proprietà | `string Icao { get; init; }` |

### `StartupHandoff`

Record sealed in `OmsiLaunch.Api`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| proprietà | `Guid SessionId { get; init; }` |
| proprietà | `string BuildProfileId { get; init; }` |
| proprietà | `WorldMode WorldMode { get; init; }` |
| proprietà | `string MapIdentity { get; init; }` |
| proprietà | `int PresentedEntrypointIndex { get; init; }` |
| proprietà | `bool HeadlessStart { get; init; }` |
| proprietà | `bool PlayerVehicleEnabled { get; init; }` |
| proprietà | `DateTimeMode DateMode { get; init; }` |
| proprietà | `DateTimeMode TimeMode { get; init; }` |
| proprietà | `string EntrypointIdentity { get; init; }` |
| proprietà | `string SituationIdentity { get; init; }` |
| proprietà | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| proprietà | `bool PluginTrace { get; init; }` |
| proprietà | `bool NativeTrace { get; init; }` |
| proprietà | `StartDateTime Start { get; init; }` |
| proprietà | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Classe statica in `OmsiLaunch.Api`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `uint Magic = 1330402120` |
| costante | `ushort Version = 7` |
| costante | `int MaxModules = 64` |
| metodo statico | `byte[] Serialize(StartupHandoff value)` |
| metodo statico | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| proprietà | `string RelativeDirectory { get; init; }` |
| proprietà | `string ManifestSha256 { get; init; }` |
| proprietà | `string Settings { get; init; }` |

### `TelemetryRingWire`

Classe statica in `OmsiLaunch.Api`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `uint Magic = 1381256271` |
| costante | `int Version = 1` |
| costante | `int SlotCount = 64` |
| costante | `int SlotSize = 2048` |
| costante | `int HeaderSize = 64` |
| costante | `int Capacity = 131136` |
| costante | `int MaxPayload = 2036` |
| metodo statico | `void Initialize(MemoryMappedViewAccessor view)` |
| metodo statico | `bool IsRing(MemoryMappedViewAccessor view)` |
| metodo statico | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| metodo statico | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| proprietà | `DateTimeMode Mode { get; init; }` |
| proprietà | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enumerazione (`byte`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `Unset = 0` |
| valore | `Preset = 1` |
| valore | `Icao = 2` |
| valore | `RealCurrent = 3` |

### `WeatherSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| proprietà | `WeatherMode Mode { get; init; }` |
| proprietà | `OptionalValue<string> Preset { get; init; }` |
| proprietà | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enumerazione (`int`) in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| valore | `NewMap = 0` |
| valore | `SavedSituation = 1` |
| valore | `LastMapState = 2` |
| valore | `LastSituation = 2` |

### `WorldSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `STABLE_BETA`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| proprietà | `WorldMode Mode { get; init; }` |
| proprietà | `OptionalValue<string> MapIdentity { get; init; }` |
| proprietà | `OptionalValue<string> SituationIdentity { get; init; }` |
| proprietà | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| proprietà | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| proprietà | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Record sealed in `OmsiLaunch.Api`. Stabilità: `PARTIAL`. Semantica: [launchspec](launchspec.md).

| Membro | Firma |
| --- | --- |
| costruttore | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| proprietà | `DateTimeMode Mode { get; init; }` |
| proprietà | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Classe sealed in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `HofPlanningException(string code, string message)` |
| proprietà | `string Code { get; }` |

### `LaunchValidation`

Classe statica in `OmsiLaunch.Core`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo statico | `bool IsMapIdentity(string value)` |
| metodo statico | `bool IsSha256(string value)` |
| metodo statico | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Classe statica in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo statico | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Record sealed in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| proprietà | `string PluginBuildDirectory { get; init; }` |
| proprietà | `string NativeBridgePath { get; init; }` |
| proprietà | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Classe sealed in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| metodo | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| metodo | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| metodo | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| metodo | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metodo | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Record sealed in `OmsiLaunch.Core`. Stabilità: `EXPERIMENTAL`. Semantica: [session-profiles](session-profiles.md).

| Membro | Firma |
| --- | --- |
| costruttore | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| proprietà | `string Map { get; init; }` |
| proprietà | `int? EntrypointIndex { get; init; }` |
| proprietà | `string EntrypointIdentity { get; init; }` |
| proprietà | `SemanticDate Date { get; init; }` |
| proprietà | `SemanticTime Time { get; init; }` |
| proprietà | `int? Year { get; init; }` |
| proprietà | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Record sealed in `OmsiLaunch.Core`. Stabilità: `EXPERIMENTAL`. Semantica: [session-profiles](session-profiles.md).

| Membro | Firma |
| --- | --- |
| costruttore | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| proprietà | `int Index { get; init; }` |
| proprietà | `string Id { get; init; }` |
| proprietà | `string Name { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| proprietà | `SessionPresentationSpec Presentation { get; init; }` |
| proprietà | `InternetTexturesSpec InternetTextures { get; init; }` |
| proprietà | `LaunchBehaviorSpec Behavior { get; init; }` |
| proprietà | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| proprietà | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Classe sealed in `OmsiLaunch.Core`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionPlanner(IRuntimePlatform platform)` |
| metodo | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Classe statica in `OmsiLaunch.Core`. Stabilità: `EXPERIMENTAL`. Semantica: [session-profiles](session-profiles.md).

| Membro | Firma |
| --- | --- |
| costante | `string Schema = "omsilaunch.session-profile/v1"` |
| costante | `long MaxBytes = 262144` |
| costante | `int MinPresetIndex = 1` |
| costante | `int MaxPresetIndex = 99` |
| campo statico | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| metodo statico | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| metodo statico | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| metodo statico | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| metodo statico | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Classe sealed in `OmsiLaunch.Core`. Stabilità: `EXPERIMENTAL`. Semantica: [session-profiles](session-profiles.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionProfileException(string code, string message)` |
| proprietà | `string Code { get; }` |

### `SessionProfilePackage`

Record sealed in `OmsiLaunch.Core`. Stabilità: `EXPERIMENTAL`. Semantica: [session-profiles](session-profiles.md).

| Membro | Firma |
| --- | --- |
| costruttore | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| proprietà | `string RootPath { get; init; }` |
| proprietà | `SessionProfileMetadata Metadata { get; init; }` |
| proprietà | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| proprietà | `ProfileNew New { get; init; }` |
| proprietà | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Classe statica in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `int MinimumYear = 1900` |
| costante | `int MaximumYear = 2200` |
| metodo statico | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Classe statica in `OmsiLaunch.Core`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo statico | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| proprietà | `string Name { get; }` |
| proprietà | `Guid SessionId { get; }` |
| metodo statico | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| metodo | `void Dispose()` |
| metodo | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| proprietà | `string Name { get; }` |
| proprietà | `Guid SessionId { get; }` |
| metodo statico | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| metodo | `void Dispose()` |

### `CurrentTelemetryStore`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| proprietà | `string Name { get; }` |
| metodo statico | `CurrentTelemetryStore Create(Guid sessionId)` |
| metodo | `void Dispose()` |
| metodo | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `CurrentWindowsX64Platform()` |
| metodo | `RuntimePlatformInfo Detect(string root)` |
| metodo | `bool HasExited(LaunchedProcess p)` |
| metodo | `bool IsInstallationWritable(string root)` |
| metodo | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| metodo | `void Terminate(LaunchedProcess p)` |
| metodo | `void ValidateCurrent(RuntimePlatformInfo p)` |
| metodo | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interfaccia in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo | `OmsiProcessState Observe()` |
| metodo | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| metodo | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interfaccia in `OmsiLaunch.Process`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo | `RuntimePlatformInfo Detect(string installationRoot)` |
| metodo | `bool HasExited(LaunchedProcess process)` |
| metodo | `bool IsInstallationWritable(string installationRoot)` |
| metodo | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| metodo | `void Terminate(LaunchedProcess process)` |
| metodo | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| metodo | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| metodo statico | `InstallationLease Acquire(string installationRoot)` |
| metodo | `void Dispose()` |
| metodo statico | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| proprietà | `int ProcessId { get; }` |
| proprietà | `int ThreadId { get; }` |
| proprietà | `ProcessIdentity Identity { get; }` |
| metodo | `void Dispose()` |

### `ModuleSessionProcess`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `STABLE_BETA`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `int MaxLineBytes = 65536` |
| proprietà | `int ProcessId { get; }` |
| proprietà | `bool HasExited { get; }` |
| proprietà | `int? ExitCode { get; }` |
| metodo | `void Dispose()` |
| metodo | `void Kill()` |
| metodo statico | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| metodo | `bool TryWriteLine(string line)` |
| metodo | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Record sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| proprietà | `string State { get; init; }` |
| proprietà | `int? ProcessId { get; init; }` |
| proprietà | `bool Responding { get; init; }` |

### `ProcessIdentity`

Record sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| proprietà | `int ProcessId { get; init; }` |
| proprietà | `DateTimeOffset CreationTimeUtc { get; init; }` |
| proprietà | `string ExecutablePath { get; init; }` |
| proprietà | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Classe statica in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costante | `string FileName = "release-manifest.json"` |
| metodo statico | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| metodo statico | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Record sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| proprietà | `string SourcePath { get; init; }` |
| proprietà | `string DestinationRelativePath { get; init; }` |
| proprietà | `string Sha256 { get; init; }` |
| proprietà | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Classe sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| proprietà | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| proprietà | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| proprietà | `string IntegrityReference { get; }` |
| metodo statico | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| metodo | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Record sealed in `OmsiLaunch.Process`. Stabilità: `INTERNAL`. Semantica: [public-api](public-api.md).

| Membro | Firma |
| --- | --- |
| costruttore | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| proprietà | `string ExecutablePath { get; init; }` |
| proprietà | `string WorkingDirectory { get; init; }` |
| proprietà | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
