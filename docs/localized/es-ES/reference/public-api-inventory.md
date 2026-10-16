# Inventario de la API pública

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> Traducción de la [página original en inglés](../../../reference/public-api-inventory.md) de OmsiLaunch 0.1.0-beta4. La página en inglés es la referencia normativa: si difieren, prevalecen la página en inglés y el código.

Esta página se genera a partir de los ensamblados compilados mediante `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); la comprobación de la documentación falla cuando deja de coincidir con el código. Enumera cada tipo exportado de `OmsiLaunch.Api`, `OmsiLaunch.Core` y `OmsiLaunch.Process`, con su estabilidad y la firma de cada miembro público. La semántica, las precondiciones, los errores y los ejemplos se documentan en la página indicada en cada encabezado (por defecto: [API pública](public-api.md)). Vocabulario de estabilidad: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (público por motivos técnicos, no es una superficie de integración), `UNAVAILABLE` (véase [API pública](public-api.md#stability-vocabulary)). Se omiten los miembros de record generados por el compilador (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`).

## `OmsiLaunch.Api`

### `Capability`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| propiedad | `string Name { get; init; }` |
| propiedad | `bool Available { get; init; }` |
| propiedad | `string EvidenceState { get; init; }` |
| propiedad | `string Reason { get; init; }` |

### `ContentIdentity`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| propiedad | `string Identity { get; init; }` |
| propiedad | `string Kind { get; init; }` |
| propiedad | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| valor | `NotReady = 0` |
| valor | `Ready = 1` |
| valor | `Lost = 2` |
| valor | `Resetting = 3` |
| valor | `Stopping = 4` |
| valor | `Stopped = 5` |

### `D3DDeviceStatus`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| propiedad | `bool Available { get; init; }` |
| propiedad | `D3DDeviceState State { get; init; }` |
| propiedad | `uint Generation { get; init; }` |
| propiedad | `uint LiveTextureCount { get; init; }` |
| propiedad | `bool ResetHookInstalled { get; init; }` |
| propiedad | `uint ExecutionThreadId { get; init; }` |
| propiedad | `uint LastResetThreadId { get; init; }` |
| propiedad | `int QueryInterfaceHResult { get; init; }` |
| propiedad | `int CooperativeLevelHResult { get; init; }` |
| propiedad | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método de extensión | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensión | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensión | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| método de extensión | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| método de extensión | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| propiedad | `D3DTextureHandle Handle { get; init; }` |
| propiedad | `D3DTextureResourceState State { get; init; }` |
| propiedad | `D3DDeviceState DeviceState { get; init; }` |
| propiedad | `uint Generation { get; init; }` |
| propiedad | `uint Width { get; init; }` |
| propiedad | `uint Height { get; init; }` |
| propiedad | `D3DTextureFormat Format { get; init; }` |
| propiedad | `uint Levels { get; init; }` |
| propiedad | `uint Level { get; init; }` |
| propiedad | `uint LevelWidth { get; init; }` |
| propiedad | `uint LevelHeight { get; init; }` |
| propiedad | `int HResult { get; init; }` |
| propiedad | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `D3DTextureHandle(string Value)` |
| propiedad | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| valor | `Live = 0` |
| valor | `Released = 1` |
| valor | `Stale = 2` |

### `D3DTextureUpdate`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| propiedad | `uint Level { get; init; }` |
| propiedad | `uint X { get; init; }` |
| propiedad | `uint Y { get; init; }` |
| propiedad | `uint Width { get; init; }` |
| propiedad | `uint Height { get; init; }` |
| propiedad | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| propiedad | `DateTimeMode Mode { get; init; }` |
| propiedad | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Explicit = 1` |
| valor | `System = 2` |

### `DiagnosticsSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| propiedad | `bool Log { get; init; }` |
| propiedad | `bool Verbose { get; init; }` |
| propiedad | `bool OmsiLogAll { get; init; }` |
| propiedad | `bool ProcessTrace { get; init; }` |
| propiedad | `bool PluginTrace { get; init; }` |
| propiedad | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Unset = 0` |
| valor | `PresentedIndex = 1` |
| valor | `Identity = 2` |

### `EntrypointSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| propiedad | `EntrypointMode Mode { get; init; }` |
| propiedad | `OptionalValue<int> PresentedIndex { get; init; }` |
| propiedad | `OptionalValue<string> Identity { get; init; }` |
| propiedad estática | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| valor | `Add = 0` |
| valor | `Mirror = 1` |

### `HofManagementSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| propiedad | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| propiedad | `HofSourceKind SourceKind { get; init; }` |
| propiedad | `string Source { get; init; }` |
| propiedad | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| propiedad | `HofApplyMode Mode { get; init; }` |
| propiedad | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| valor | `Collection = 0` |
| valor | `Directory = 1` |
| valor | `Vehicle = 2` |
| valor | `File = 3` |

### `IOmsiLaunch`

Interfaz en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| propiedad | `OptionalValue<string> KeyboardDocument { get; init; }` |
| propiedad | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método estático | `string IdentityKey(string root)` |
| método estático | `string NormalizeRoot(string root)` |
| método estático | `IReadOnlyList<string> Segments(string relativePath)` |
| método estático | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| propiedad | `string RootPath { get; init; }` |
| propiedad | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Native = 0` |
| valor | `Disabled = 1` |
| valor | `Override = 2` |

### `InternetTexturesSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| propiedad | `InternetTexturesMode Mode { get; init; }` |
| propiedad | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| propiedad | `bool RestoreConfiguration { get; init; }` |
| propiedad | `bool SuppressStaleClosecheckWarning { get; init; }` |
| propiedad | `int StartupTimeoutSeconds { get; init; }` |
| propiedad | `int ShutdownTimeoutSeconds { get; init; }` |
| propiedad | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| propiedad | `string Code { get; init; }` |
| propiedad | `string Message { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| propiedad | `InstallationSpec Installation { get; init; }` |
| propiedad | `WorldSpec World { get; init; }` |
| propiedad | `DateSpec Date { get; init; }` |
| propiedad | `TimeSpec Time { get; init; }` |
| propiedad | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| propiedad | `EnvironmentSpec Environment { get; init; }` |
| propiedad | `LaunchBehaviorSpec Behavior { get; init; }` |
| propiedad | `YearSpec Year { get; init; }` |
| propiedad | `WeatherSpec Weather { get; init; }` |
| propiedad | `InputSpec Input { get; init; }` |
| propiedad | `DiagnosticsSpec Diagnostics { get; init; }` |
| propiedad | `SessionPresentationSpec Presentation { get; init; }` |
| propiedad | `InternetTexturesSpec InternetTextures { get; init; }` |
| propiedad | `SessionProfileMetadata SessionProfile { get; init; }` |
| propiedad | `HofManagementSpec HofManagement { get; init; }` |
| propiedad | `RuntimeModulesSpec Modules { get; init; }` |
| propiedad | `HofManagementSpec EffectiveHofManagement { get; }` |
| propiedad | `RuntimeModulesSpec EffectiveModules { get; }` |
| propiedad | `YearSpec EffectiveYear { get; }` |
| propiedad | `WeatherSpec EffectiveWeather { get; }` |
| propiedad | `InputSpec EffectiveInput { get; }` |
| propiedad | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| propiedad | `SessionPresentationSpec EffectivePresentation { get; }` |
| propiedad | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Clase sellada en `OmsiLaunch.Api`. Estabilidad: `EXPERIMENTAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `OmsiRuntimeException(string code, string detail = null)` |
| propiedad | `string Code { get; }` |

### `OptionalValue`

Record struct en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `OptionalValue(Presence Presence, T Value)` |
| propiedad | `Presence Presence { get; init; }` |
| propiedad | `T Value { get; init; }` |
| propiedad | `bool IsSet { get; }` |
| propiedad estática | `OptionalValue<T> Unset { get; }` |
| método estático | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| constante | `string Eligible = "ELIGIBLE"` |
| constante | `string Rejected = "REJECTED"` |
| constante | `string ActivationRequested = "requested"` |
| constante | `string ActivationSettingsFile = "settings-file"` |
| constante | `string ActivationContent = "content"` |
| constante | `string ActivationDefault = "default"` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Version { get; init; }` |
| propiedad | `string Status { get; init; }` |
| propiedad | `bool Requested { get; init; }` |
| propiedad | `string FailurePolicy { get; init; }` |
| propiedad | `IReadOnlyList<string> Operations { get; init; }` |
| propiedad | `string Code { get; init; }` |
| propiedad | `string Reason { get; init; }` |
| propiedad | `string ManifestSha256 { get; init; }` |
| propiedad | `string Activation { get; init; }` |
| propiedad | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| propiedad | `bool IsEligible { get; }` |
| propiedad | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| propiedad | `string Key { get; init; }` |
| propiedad | `string Value { get; init; }` |
| propiedad | `string Source { get; init; }` |

### `PlannedMutation`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| propiedad | `string RelativePath { get; init; }` |
| propiedad | `string SemanticKey { get; init; }` |
| propiedad | `string RequestedValue { get; init; }` |
| propiedad | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| propiedad | `OptionalValue<string> Model { get; init; }` |
| propiedad | `OptionalValue<string> Repaint { get; init; }` |
| propiedad | `OptionalValue<string> Hof { get; init; }` |
| propiedad | `OptionalValue<string> FleetNumber { get; init; }` |
| propiedad | `OptionalValue<string> Registration { get; init; }` |
| propiedad | `bool Enabled { get; }` |

### `Presence`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Set = 1` |

### `PublicCapabilityClassification`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [capabilities](capabilities.md).

| Miembro | Firma |
| --- | --- |
| valor | `PublicStableBeta = 0` |
| valor | `PublicExperimental = 1` |
| valor | `InternalOnly = 2` |
| valor | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [capabilities](capabilities.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Family { get; init; }` |
| propiedad | `PublicCapabilityClassification Classification { get; init; }` |
| propiedad | `PublicCapabilityKind Kind { get; init; }` |
| propiedad | `bool RequiresSession { get; init; }` |
| propiedad | `bool RequiresExactProfile { get; init; }` |
| propiedad | `string ApiRoute { get; init; }` |
| propiedad | `string CliRoute { get; init; }` |
| propiedad | `string RuntimeValidation { get; init; }` |
| propiedad | `string Description { get; init; }` |
| propiedad | `IReadOnlyList<string> HandleTypes { get; init; }` |
| propiedad | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| propiedad | `string NativeCapability { get; init; }` |
| propiedad | `string EvidenceNote { get; init; }` |
| propiedad | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| propiedad | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [capabilities](capabilities.md).

| Miembro | Firma |
| --- | --- |
| valor | `Read = 0` |
| valor | `Write = 1` |
| valor | `Action = 2` |
| valor | `Event = 3` |

### `PublicCapabilityRegistry`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [capabilities](capabilities.md).

| Miembro | Firma |
| --- | --- |
| constante | `string ProtocolVersion = "0.1"` |
| campo estático | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| propiedad estática | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| propiedad estática | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| propiedad estática | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
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

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [errors](errors.md).

| Miembro | Firma |
| --- | --- |
| constante | `string InvalidArgument = "invalid_argument"` |
| constante | `string UnsupportedProfile = "unsupported_profile"` |
| constante | `string Session = "session"` |
| constante | `string Runtime = "runtime"` |
| constante | `string NotFound = "not_found"` |
| constante | `string Transaction = "transaction"` |
| constante | `string Internal = "internal"` |

### `PublicErrorCodes`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [errors](errors.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [errors](errors.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PublicErrorDescriptor(string Code, string Category)` |
| propiedad | `string Code { get; init; }` |
| propiedad | `string Category { get; init; }` |

### `PublicEvidence`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| constante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| constante | `string Partial = "PARTIAL"` |
| constante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| constante | `string Unavailable = "UNAVAILABLE"` |
| campo estático | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enumeración (`int`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [exit-codes](exit-codes.md).

| Miembro | Firma |
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

Clase estática en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `string None = "none"` |
| constante | `string OmsiState = "omsi-state"` |
| constante | `string SessionResource = "session-resource"` |
| constante | `string SessionPolicy = "session-policy"` |
| campo estático | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| propiedad | `string Name { get; init; }` |
| propiedad | `bool Required { get; init; }` |
| propiedad | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| propiedad | `bool Accepted { get; init; }` |
| propiedad | `string ErrorCode { get; init; }` |
| propiedad | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string NativeCapability { get; init; }` |
| propiedad | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| propiedad | `IReadOnlyList<string> CliRoutes { get; init; }` |
| propiedad | `bool Paged { get; init; }` |
| propiedad | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| propiedad | `bool Pending { get; init; }` |
| propiedad | `bool Recovered { get; init; }` |
| propiedad | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [runtime-control](runtime-control.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| propiedad | `Guid SessionId { get; init; }` |
| propiedad | `ulong RequestId { get; init; }` |
| propiedad | `string Operation { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [runtime-control](runtime-control.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| propiedad | `Guid SessionId { get; init; }` |
| propiedad | `ulong RequestId { get; init; }` |
| propiedad | `bool Succeeded { get; init; }` |
| propiedad | `string ErrorCode { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| propiedad | `string Type { get; init; }` |
| propiedad | `DateTimeOffset TimestampUtc { get; init; }` |
| propiedad | `long Sequence { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| propiedad | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| propiedad | `string Key { get; init; }` |
| propiedad | `string Type { get; init; }` |
| propiedad | `string Default { get; init; }` |
| propiedad | `bool AllowsAuto { get; init; }` |
| propiedad | `string Minimum { get; init; }` |
| propiedad | `string Maximum { get; init; }` |
| propiedad | `IReadOnlyList<string> Choices { get; init; }` |
| propiedad | `string Flag { get; init; }` |
| propiedad | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Version { get; init; }` |
| propiedad | `string Name { get; init; }` |
| propiedad | `string Activation { get; init; }` |
| propiedad | `bool IsValid { get; init; }` |
| propiedad | `IReadOnlyList<string> Operations { get; init; }` |
| propiedad | `string ContentBindingMap { get; init; }` |
| propiedad | `string ErrorCode { get; init; }` |
| propiedad | `string ErrorMessage { get; init; }` |
| propiedad | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| propiedad | `IReadOnlyList<string> Enable { get; init; }` |
| propiedad | `IReadOnlyList<string> Disable { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propiedad | `IReadOnlyList<string> EffectiveDisable { get; }` |
| propiedad | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| propiedad | `string OsFamily { get; init; }` |
| propiedad | `string OsVersion { get; init; }` |
| propiedad | `string OsArchitecture { get; init; }` |
| propiedad | `string HostArchitecture { get; init; }` |
| propiedad | `string OmsiArchitecture { get; init; }` |
| propiedad | `string PluginArchitecture { get; init; }` |
| propiedad | `bool CurrentPlatformSupported { get; init; }` |
| propiedad | `bool LegacyPlatform { get; init; }` |
| propiedad | `bool Wow64Available { get; init; }` |
| propiedad | `bool InstallationWritable { get; init; }` |
| propiedad | `bool ProcessLaunchSupported { get; init; }` |
| propiedad | `bool PluginRuntimeSupported { get; init; }` |
| propiedad | `bool NativeInteropSupported { get; init; }` |
| propiedad | `bool SharedMemorySupported { get; init; }` |
| propiedad | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SemanticDate(int Year, int Month, int Day)` |
| propiedad | `int Year { get; init; }` |
| propiedad | `int Month { get; init; }` |
| propiedad | `int Day { get; init; }` |

### `SemanticTime`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SemanticTime(int Hour, int Minute, int Second)` |
| propiedad | `int Hour { get; init; }` |
| propiedad | `int Minute { get; init; }` |
| propiedad | `int Second { get; init; }` |

### `SessionHandle`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionHandle(Guid SessionId)` |
| propiedad | `Guid SessionId { get; init; }` |

### `SessionPlan`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| propiedad | `Guid SessionId { get; init; }` |
| propiedad | `string BuildProfileId { get; init; }` |
| propiedad | `LaunchSpec Spec { get; init; }` |
| propiedad | `RuntimePlatformInfo Platform { get; init; }` |
| propiedad | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| propiedad | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| propiedad | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| propiedad | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| propiedad | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| propiedad | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| propiedad | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propiedad | `bool IsRunnable { get; init; }` |
| propiedad | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| propiedad | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| propiedad | `SplashMode Splash { get; init; }` |
| propiedad | `OptionalValue<string> Language { get; init; }` |
| propiedad | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| propiedad | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Name { get; init; }` |
| propiedad | `string Version { get; init; }` |
| propiedad | `string Author { get; init; }` |
| propiedad | `string PresetId { get; init; }` |
| propiedad | `int PresetIndex { get; init; }` |
| propiedad | `string PresetName { get; init; }` |
| propiedad | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| propiedad | `int Index { get; init; }` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Name { get; init; }` |

### `SessionProfileSummary`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Name { get; init; }` |
| propiedad | `string Version { get; init; }` |
| propiedad | `string Author { get; init; }` |
| propiedad | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propiedad | `string MapIdentity { get; init; }` |
| propiedad | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| propiedad | `bool IsValid { get; init; }` |
| propiedad | `string ErrorCode { get; init; }` |
| propiedad | `string ErrorMessage { get; init; }` |

### `SessionState`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| propiedad | `Guid SessionId { get; init; }` |
| propiedad | `SessionState State { get; init; }` |
| propiedad | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propiedad | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Native = 0` |
| valor | `Managed = 1` |

### `StartDateTime`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| propiedad | `int? Year { get; init; }` |
| propiedad | `int? DayOfYear { get; init; }` |
| propiedad | `int? Hour { get; init; }` |
| propiedad | `int? Minute { get; init; }` |
| propiedad | `int? Second { get; init; }` |
| propiedad | `bool IsEmpty { get; }` |

### `StartWeather`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| propiedad | `WeatherMode Mode { get; init; }` |
| propiedad | `string Preset { get; init; }` |
| propiedad | `string Icao { get; init; }` |

### `StartupHandoff`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| propiedad | `Guid SessionId { get; init; }` |
| propiedad | `string BuildProfileId { get; init; }` |
| propiedad | `WorldMode WorldMode { get; init; }` |
| propiedad | `string MapIdentity { get; init; }` |
| propiedad | `int PresentedEntrypointIndex { get; init; }` |
| propiedad | `bool HeadlessStart { get; init; }` |
| propiedad | `bool PlayerVehicleEnabled { get; init; }` |
| propiedad | `DateTimeMode DateMode { get; init; }` |
| propiedad | `DateTimeMode TimeMode { get; init; }` |
| propiedad | `string EntrypointIdentity { get; init; }` |
| propiedad | `string SituationIdentity { get; init; }` |
| propiedad | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| propiedad | `bool PluginTrace { get; init; }` |
| propiedad | `bool NativeTrace { get; init; }` |
| propiedad | `StartDateTime Start { get; init; }` |
| propiedad | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `uint Magic = 1330402120` |
| constante | `ushort Version = 7` |
| constante | `int MaxModules = 64` |
| método estático | `byte[] Serialize(StartupHandoff value)` |
| método estático | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| propiedad | `string RelativeDirectory { get; init; }` |
| propiedad | `string ManifestSha256 { get; init; }` |
| propiedad | `string Settings { get; init; }` |

### `TelemetryRingWire`

Clase estática en `OmsiLaunch.Api`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
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

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| propiedad | `DateTimeMode Mode { get; init; }` |
| propiedad | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enumeración (`byte`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `Unset = 0` |
| valor | `Preset = 1` |
| valor | `Icao = 2` |
| valor | `RealCurrent = 3` |

### `WeatherSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| propiedad | `WeatherMode Mode { get; init; }` |
| propiedad | `OptionalValue<string> Preset { get; init; }` |
| propiedad | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enumeración (`int`) en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| valor | `NewMap = 0` |
| valor | `SavedSituation = 1` |
| valor | `LastMapState = 2` |
| valor | `LastSituation = 2` |

### `WorldSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `STABLE_BETA`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| propiedad | `WorldMode Mode { get; init; }` |
| propiedad | `OptionalValue<string> MapIdentity { get; init; }` |
| propiedad | `OptionalValue<string> SituationIdentity { get; init; }` |
| propiedad | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| propiedad | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| propiedad | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Record sellado en `OmsiLaunch.Api`. Estabilidad: `PARTIAL`. Semántica: [launchspec](launchspec.md).

| Miembro | Firma |
| --- | --- |
| constructor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| propiedad | `DateTimeMode Mode { get; init; }` |
| propiedad | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Clase sellada en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `HofPlanningException(string code, string message)` |
| propiedad | `string Code { get; }` |

### `LaunchValidation`

Clase estática en `OmsiLaunch.Core`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método estático | `bool IsMapIdentity(string value)` |
| método estático | `bool IsSha256(string value)` |
| método estático | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Clase estática en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método estático | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Record sellado en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| propiedad | `string PluginBuildDirectory { get; init; }` |
| propiedad | `string NativeBridgePath { get; init; }` |
| propiedad | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Clase sellada en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
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

Record sellado en `OmsiLaunch.Core`. Estabilidad: `EXPERIMENTAL`. Semántica: [session-profiles](session-profiles.md).

| Miembro | Firma |
| --- | --- |
| constructor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| propiedad | `string Map { get; init; }` |
| propiedad | `int? EntrypointIndex { get; init; }` |
| propiedad | `string EntrypointIdentity { get; init; }` |
| propiedad | `SemanticDate Date { get; init; }` |
| propiedad | `SemanticTime Time { get; init; }` |
| propiedad | `int? Year { get; init; }` |
| propiedad | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Record sellado en `OmsiLaunch.Core`. Estabilidad: `EXPERIMENTAL`. Semántica: [session-profiles](session-profiles.md).

| Miembro | Firma |
| --- | --- |
| constructor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| propiedad | `int Index { get; init; }` |
| propiedad | `string Id { get; init; }` |
| propiedad | `string Name { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propiedad | `SessionPresentationSpec Presentation { get; init; }` |
| propiedad | `InternetTexturesSpec InternetTextures { get; init; }` |
| propiedad | `LaunchBehaviorSpec Behavior { get; init; }` |
| propiedad | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| propiedad | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Clase sellada en `OmsiLaunch.Core`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionPlanner(IRuntimePlatform platform)` |
| método | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Clase estática en `OmsiLaunch.Core`. Estabilidad: `EXPERIMENTAL`. Semántica: [session-profiles](session-profiles.md).

| Miembro | Firma |
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

Clase sellada en `OmsiLaunch.Core`. Estabilidad: `EXPERIMENTAL`. Semántica: [session-profiles](session-profiles.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionProfileException(string code, string message)` |
| propiedad | `string Code { get; }` |

### `SessionProfilePackage`

Record sellado en `OmsiLaunch.Core`. Estabilidad: `EXPERIMENTAL`. Semántica: [session-profiles](session-profiles.md).

| Miembro | Firma |
| --- | --- |
| constructor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| propiedad | `string RootPath { get; init; }` |
| propiedad | `SessionProfileMetadata Metadata { get; init; }` |
| propiedad | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propiedad | `ProfileNew New { get; init; }` |
| propiedad | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Clase estática en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `int MinimumYear = 1900` |
| constante | `int MaximumYear = 2200` |
| método estático | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Clase estática en `OmsiLaunch.Core`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método estático | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| propiedad | `string Name { get; }` |
| propiedad | `Guid SessionId { get; }` |
| método estático | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| método | `void Dispose()` |
| método | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| propiedad | `string Name { get; }` |
| propiedad | `Guid SessionId { get; }` |
| método estático | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| método | `void Dispose()` |

### `CurrentTelemetryStore`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| propiedad | `string Name { get; }` |
| método estático | `CurrentTelemetryStore Create(Guid sessionId)` |
| método | `void Dispose()` |
| método | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `CurrentWindowsX64Platform()` |
| método | `RuntimePlatformInfo Detect(string root)` |
| método | `bool HasExited(LaunchedProcess p)` |
| método | `bool IsInstallationWritable(string root)` |
| método | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| método | `void Terminate(LaunchedProcess p)` |
| método | `void ValidateCurrent(RuntimePlatformInfo p)` |
| método | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interfaz en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método | `OmsiProcessState Observe()` |
| método | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| método | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interfaz en `OmsiLaunch.Process`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método | `RuntimePlatformInfo Detect(string installationRoot)` |
| método | `bool HasExited(LaunchedProcess process)` |
| método | `bool IsInstallationWritable(string installationRoot)` |
| método | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| método | `void Terminate(LaunchedProcess process)` |
| método | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| método | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| método estático | `InstallationLease Acquire(string installationRoot)` |
| método | `void Dispose()` |
| método estático | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| propiedad | `int ProcessId { get; }` |
| propiedad | `int ThreadId { get; }` |
| propiedad | `ProcessIdentity Identity { get; }` |
| método | `void Dispose()` |

### `ModuleSessionProcess`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `STABLE_BETA`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `int MaxLineBytes = 65536` |
| propiedad | `int ProcessId { get; }` |
| propiedad | `bool HasExited { get; }` |
| propiedad | `int? ExitCode { get; }` |
| método | `void Dispose()` |
| método | `void Kill()` |
| método estático | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| método | `bool TryWriteLine(string line)` |
| método | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Record sellado en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| propiedad | `string State { get; init; }` |
| propiedad | `int? ProcessId { get; init; }` |
| propiedad | `bool Responding { get; init; }` |

### `ProcessIdentity`

Record sellado en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| propiedad | `int ProcessId { get; init; }` |
| propiedad | `DateTimeOffset CreationTimeUtc { get; init; }` |
| propiedad | `string ExecutablePath { get; init; }` |
| propiedad | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Clase estática en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constante | `string FileName = "release-manifest.json"` |
| método estático | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| método estático | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Record sellado en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| propiedad | `string SourcePath { get; init; }` |
| propiedad | `string DestinationRelativePath { get; init; }` |
| propiedad | `string Sha256 { get; init; }` |
| propiedad | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Clase sellada en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| propiedad | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| propiedad | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| propiedad | `string IntegrityReference { get; }` |
| método estático | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| método | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Record sellado en `OmsiLaunch.Process`. Estabilidad: `INTERNAL`. Semántica: [public-api](public-api.md).

| Miembro | Firma |
| --- | --- |
| constructor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| propiedad | `string ExecutablePath { get; init; }` |
| propiedad | `string WorkingDirectory { get; init; }` |
| propiedad | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
