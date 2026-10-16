# Inventaire de l’API publique

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> Traduction de la [page originale en anglais](../../../reference/public-api-inventory.md) d’OmsiLaunch 0.1.0-beta4. La page anglaise fait foi : en cas de divergence, la page anglaise et le code prévalent.

Cette page est générée à partir des assemblies compilés par `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`) ; le contrôle de la documentation échoue lorsqu’elle ne correspond plus au code. Elle répertorie chaque type exporté de `OmsiLaunch.Api`, `OmsiLaunch.Core` et `OmsiLaunch.Process`, avec sa stabilité et la signature de chaque membre public. La sémantique, les préconditions, les erreurs et les exemples sont documentés sur la page indiquée dans chaque titre (par défaut : [API publique](public-api.md)). Vocabulaire de stabilité : `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (public pour des raisons techniques, ce n’est pas une surface d’intégration), `UNAVAILABLE` (voir [API publique](public-api.md#stability-vocabulary)). Les membres de record générés par le compilateur (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) sont omis.

## `OmsiLaunch.Api`

### `Capability`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| propriété | `string Name { get; init; }` |
| propriété | `bool Available { get; init; }` |
| propriété | `string EvidenceState { get; init; }` |
| propriété | `string Reason { get; init; }` |

### `ContentIdentity`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| propriété | `string Identity { get; init; }` |
| propriété | `string Kind { get; init; }` |
| propriété | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `Maps = 0` |
| valeur | `Situations = 1` |
| valeur | `Vehicles = 2` |
| valeur | `Repaints = 3` |
| valeur | `Hofs = 4` |
| valeur | `FleetNumbers = 5` |
| valeur | `Registrations = 6` |
| valeur | `Addons = 7` |
| valeur | `Entrypoints = 8` |
| valeur | `SessionProfiles = 9` |
| valeur | `RuntimeModules = 10` |

### `D3DDeviceState`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `NotReady = 0` |
| valeur | `Ready = 1` |
| valeur | `Lost = 2` |
| valeur | `Resetting = 3` |
| valeur | `Stopping = 4` |
| valeur | `Stopped = 5` |

### `D3DDeviceStatus`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| propriété | `bool Available { get; init; }` |
| propriété | `D3DDeviceState State { get; init; }` |
| propriété | `uint Generation { get; init; }` |
| propriété | `uint LiveTextureCount { get; init; }` |
| propriété | `bool ResetHookInstalled { get; init; }` |
| propriété | `uint ExecutionThreadId { get; init; }` |
| propriété | `uint LastResetThreadId { get; init; }` |
| propriété | `int QueryInterfaceHResult { get; init; }` |
| propriété | `int CooperativeLevelHResult { get; init; }` |
| propriété | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode d’extension | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| méthode d’extension | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| méthode d’extension | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| méthode d’extension | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| méthode d’extension | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| propriété | `D3DTextureHandle Handle { get; init; }` |
| propriété | `D3DTextureResourceState State { get; init; }` |
| propriété | `D3DDeviceState DeviceState { get; init; }` |
| propriété | `uint Generation { get; init; }` |
| propriété | `uint Width { get; init; }` |
| propriété | `uint Height { get; init; }` |
| propriété | `D3DTextureFormat Format { get; init; }` |
| propriété | `uint Levels { get; init; }` |
| propriété | `uint Level { get; init; }` |
| propriété | `uint LevelWidth { get; init; }` |
| propriété | `uint LevelHeight { get; init; }` |
| propriété | `int HResult { get; init; }` |
| propriété | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `A8R8G8B8 = 0` |
| valeur | `X8R8G8B8 = 1` |
| valeur | `R5G6B5 = 2` |
| valeur | `X1R5G5B5 = 3` |
| valeur | `A1R5G5B5 = 4` |
| valeur | `A4R4G4B4 = 5` |
| valeur | `A8 = 6` |
| valeur | `L8 = 7` |
| valeur | `A8L8 = 8` |

### `D3DTextureHandle`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `D3DTextureHandle(string Value)` |
| propriété | `string Value { get; init; }` |

### `D3DTextureResourceState`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `Live = 0` |
| valeur | `Released = 1` |
| valeur | `Stale = 2` |

### `D3DTextureUpdate`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| propriété | `uint Level { get; init; }` |
| propriété | `uint X { get; init; }` |
| propriété | `uint Y { get; init; }` |
| propriété | `uint Width { get; init; }` |
| propriété | `uint Height { get; init; }` |
| propriété | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| propriété | `DateTimeMode Mode { get; init; }` |
| propriété | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Unset = 0` |
| valeur | `Explicit = 1` |
| valeur | `System = 2` |

### `DiagnosticsSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| propriété | `bool Log { get; init; }` |
| propriété | `bool Verbose { get; init; }` |
| propriété | `bool OmsiLogAll { get; init; }` |
| propriété | `bool ProcessTrace { get; init; }` |
| propriété | `bool PluginTrace { get; init; }` |
| propriété | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Unset = 0` |
| valeur | `PresentedIndex = 1` |
| valeur | `Identity = 2` |

### `EntrypointSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| propriété | `EntrypointMode Mode { get; init; }` |
| propriété | `OptionalValue<int> PresentedIndex { get; init; }` |
| propriété | `OptionalValue<string> Identity { get; init; }` |
| propriété statique | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| propriété | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `Add = 0` |
| valeur | `Mirror = 1` |

### `HofManagementSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| propriété | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| propriété | `HofSourceKind SourceKind { get; init; }` |
| propriété | `string Source { get; init; }` |
| propriété | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| propriété | `HofApplyMode Mode { get; init; }` |
| propriété | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `Collection = 0` |
| valeur | `Directory = 1` |
| valeur | `Vehicle = 2` |
| valeur | `File = 3` |

### `IOmsiLaunch`

Interface dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| méthode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| méthode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| méthode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| propriété | `OptionalValue<string> KeyboardDocument { get; init; }` |
| propriété | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode statique | `string IdentityKey(string root)` |
| méthode statique | `string NormalizeRoot(string root)` |
| méthode statique | `IReadOnlyList<string> Segments(string relativePath)` |
| méthode statique | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| propriété | `string RootPath { get; init; }` |
| propriété | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Native = 0` |
| valeur | `Disabled = 1` |
| valeur | `Override = 2` |

### `InternetTexturesSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| propriété | `InternetTexturesMode Mode { get; init; }` |
| propriété | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| propriété | `bool RestoreConfiguration { get; init; }` |
| propriété | `bool SuppressStaleClosecheckWarning { get; init; }` |
| propriété | `int StartupTimeoutSeconds { get; init; }` |
| propriété | `int ShutdownTimeoutSeconds { get; init; }` |
| propriété | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| propriété | `string Code { get; init; }` |
| propriété | `string Message { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| propriété | `InstallationSpec Installation { get; init; }` |
| propriété | `WorldSpec World { get; init; }` |
| propriété | `DateSpec Date { get; init; }` |
| propriété | `TimeSpec Time { get; init; }` |
| propriété | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| propriété | `EnvironmentSpec Environment { get; init; }` |
| propriété | `LaunchBehaviorSpec Behavior { get; init; }` |
| propriété | `YearSpec Year { get; init; }` |
| propriété | `WeatherSpec Weather { get; init; }` |
| propriété | `InputSpec Input { get; init; }` |
| propriété | `DiagnosticsSpec Diagnostics { get; init; }` |
| propriété | `SessionPresentationSpec Presentation { get; init; }` |
| propriété | `InternetTexturesSpec InternetTextures { get; init; }` |
| propriété | `SessionProfileMetadata SessionProfile { get; init; }` |
| propriété | `HofManagementSpec HofManagement { get; init; }` |
| propriété | `RuntimeModulesSpec Modules { get; init; }` |
| propriété | `HofManagementSpec EffectiveHofManagement { get; }` |
| propriété | `RuntimeModulesSpec EffectiveModules { get; }` |
| propriété | `YearSpec EffectiveYear { get; }` |
| propriété | `WeatherSpec EffectiveWeather { get; }` |
| propriété | `InputSpec EffectiveInput { get; }` |
| propriété | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| propriété | `SessionPresentationSpec EffectivePresentation { get; }` |
| propriété | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Classe scellée dans `OmsiLaunch.Api`. Stabilité : `EXPERIMENTAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `OmsiRuntimeException(string code, string detail = null)` |
| propriété | `string Code { get; }` |

### `OptionalValue`

Record struct dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `OptionalValue(Presence Presence, T Value)` |
| propriété | `Presence Presence { get; init; }` |
| propriété | `T Value { get; init; }` |
| propriété | `bool IsSet { get; }` |
| propriété statique | `OptionalValue<T> Unset { get; }` |
| méthode statique | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| constante | `string Eligible = "ELIGIBLE"` |
| constante | `string Rejected = "REJECTED"` |
| constante | `string ActivationRequested = "requested"` |
| constante | `string ActivationSettingsFile = "settings-file"` |
| constante | `string ActivationContent = "content"` |
| constante | `string ActivationDefault = "default"` |
| propriété | `string Id { get; init; }` |
| propriété | `string Version { get; init; }` |
| propriété | `string Status { get; init; }` |
| propriété | `bool Requested { get; init; }` |
| propriété | `string FailurePolicy { get; init; }` |
| propriété | `IReadOnlyList<string> Operations { get; init; }` |
| propriété | `string Code { get; init; }` |
| propriété | `string Reason { get; init; }` |
| propriété | `string ManifestSha256 { get; init; }` |
| propriété | `string Activation { get; init; }` |
| propriété | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| propriété | `bool IsEligible { get; }` |
| propriété | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PlannedModuleSetting(string Key, string Value, string Source)` |
| propriété | `string Key { get; init; }` |
| propriété | `string Value { get; init; }` |
| propriété | `string Source { get; init; }` |

### `PlannedMutation`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| propriété | `string RelativePath { get; init; }` |
| propriété | `string SemanticKey { get; init; }` |
| propriété | `string RequestedValue { get; init; }` |
| propriété | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| propriété | `OptionalValue<string> Model { get; init; }` |
| propriété | `OptionalValue<string> Repaint { get; init; }` |
| propriété | `OptionalValue<string> Hof { get; init; }` |
| propriété | `OptionalValue<string> FleetNumber { get; init; }` |
| propriété | `OptionalValue<string> Registration { get; init; }` |
| propriété | `bool Enabled { get; }` |

### `Presence`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Unset = 0` |
| valeur | `Set = 1` |

### `PublicCapabilityClassification`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [capabilities](capabilities.md).

| Membre | Signature |
| --- | --- |
| valeur | `PublicStableBeta = 0` |
| valeur | `PublicExperimental = 1` |
| valeur | `InternalOnly = 2` |
| valeur | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [capabilities](capabilities.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| propriété | `string Id { get; init; }` |
| propriété | `string Family { get; init; }` |
| propriété | `PublicCapabilityClassification Classification { get; init; }` |
| propriété | `PublicCapabilityKind Kind { get; init; }` |
| propriété | `bool RequiresSession { get; init; }` |
| propriété | `bool RequiresExactProfile { get; init; }` |
| propriété | `string ApiRoute { get; init; }` |
| propriété | `string CliRoute { get; init; }` |
| propriété | `string RuntimeValidation { get; init; }` |
| propriété | `string Description { get; init; }` |
| propriété | `IReadOnlyList<string> HandleTypes { get; init; }` |
| propriété | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| propriété | `string NativeCapability { get; init; }` |
| propriété | `string EvidenceNote { get; init; }` |
| propriété | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| propriété | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [capabilities](capabilities.md).

| Membre | Signature |
| --- | --- |
| valeur | `Read = 0` |
| valeur | `Write = 1` |
| valeur | `Action = 2` |
| valeur | `Event = 3` |

### `PublicCapabilityRegistry`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [capabilities](capabilities.md).

| Membre | Signature |
| --- | --- |
| constante | `string ProtocolVersion = "0.1"` |
| champ statique | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| propriété statique | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| propriété statique | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| propriété statique | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| méthode statique | `PublicCapabilityDescriptor Get(string id)` |
| méthode statique | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| méthode statique | `bool IsInternalResultKey(string key)` |
| méthode statique | `bool IsModuleOperation(string operation)` |
| méthode statique | `bool IsPublicRuntimeOperation(string operation)` |
| méthode statique | `string NativeCapabilityOf(string operation)` |
| méthode statique | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| méthode statique | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| méthode statique | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [errors](errors.md).

| Membre | Signature |
| --- | --- |
| constante | `string InvalidArgument = "invalid_argument"` |
| constante | `string UnsupportedProfile = "unsupported_profile"` |
| constante | `string Session = "session"` |
| constante | `string Runtime = "runtime"` |
| constante | `string NotFound = "not_found"` |
| constante | `string Transaction = "transaction"` |
| constante | `string Internal = "internal"` |

### `PublicErrorCodes`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [errors](errors.md).

| Membre | Signature |
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
| champ statique | `IReadOnlyList<PublicErrorDescriptor> All` |
| méthode statique | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [errors](errors.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PublicErrorDescriptor(string Code, string Category)` |
| propriété | `string Code { get; init; }` |
| propriété | `string Category { get; init; }` |

### `PublicEvidence`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| constante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| constante | `string Partial = "PARTIAL"` |
| constante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| constante | `string Unavailable = "UNAVAILABLE"` |
| champ statique | `IReadOnlyList<string> All` |

### `PublicExitCode`

Énumération (`int`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [exit-codes](exit-codes.md).

| Membre | Signature |
| --- | --- |
| valeur | `Success = 0` |
| valeur | `SessionFailed = 1` |
| valeur | `InvalidArguments = 2` |
| valeur | `UnsupportedProfile = 3` |
| valeur | `NoActiveSession = 4` |
| valeur | `RuntimeUnavailable = 5` |
| valeur | `NotFound = 6` |
| valeur | `OperationRejected = 7` |
| valeur | `TransactionRecoveryFailed = 8` |
| valeur | `InternalError = 10` |

### `PublicMutationPolicy`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `string None = "none"` |
| constante | `string OmsiState = "omsi-state"` |
| constante | `string SessionResource = "session-resource"` |
| constante | `string SessionPolicy = "session-policy"` |
| champ statique | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| propriété | `string Name { get; init; }` |
| propriété | `bool Required { get; init; }` |
| propriété | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| propriété | `bool Accepted { get; init; }` |
| propriété | `string ErrorCode { get; init; }` |
| propriété | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| propriété | `string Id { get; init; }` |
| propriété | `string NativeCapability { get; init; }` |
| propriété | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| propriété | `IReadOnlyList<string> CliRoutes { get; init; }` |
| propriété | `bool Paged { get; init; }` |
| propriété | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| propriété | `bool Pending { get; init; }` |
| propriété | `bool Recovered { get; init; }` |
| propriété | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [runtime-control](runtime-control.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| propriété | `Guid SessionId { get; init; }` |
| propriété | `ulong RequestId { get; init; }` |
| propriété | `string Operation { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [runtime-control](runtime-control.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| propriété | `Guid SessionId { get; init; }` |
| propriété | `ulong RequestId { get; init; }` |
| propriété | `bool Succeeded { get; init; }` |
| propriété | `string ErrorCode { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `uint Magic = 1330401859` |
| constante | `ushort Version = 1` |
| constante | `int HeaderSize = 72` |
| méthode statique | `byte[] SerializeRequest(RuntimeCommand command)` |
| méthode statique | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| méthode statique | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| méthode statique | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| méthode statique | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| propriété | `string Type { get; init; }` |
| propriété | `DateTimeOffset TimestampUtc { get; init; }` |
| propriété | `long Sequence { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| propriété | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| propriété | `string Key { get; init; }` |
| propriété | `string Type { get; init; }` |
| propriété | `string Default { get; init; }` |
| propriété | `bool AllowsAuto { get; init; }` |
| propriété | `string Minimum { get; init; }` |
| propriété | `string Maximum { get; init; }` |
| propriété | `IReadOnlyList<string> Choices { get; init; }` |
| propriété | `string Flag { get; init; }` |
| propriété | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| propriété | `string Id { get; init; }` |
| propriété | `string Version { get; init; }` |
| propriété | `string Name { get; init; }` |
| propriété | `string Activation { get; init; }` |
| propriété | `bool IsValid { get; init; }` |
| propriété | `IReadOnlyList<string> Operations { get; init; }` |
| propriété | `string ContentBindingMap { get; init; }` |
| propriété | `string ErrorCode { get; init; }` |
| propriété | `string ErrorMessage { get; init; }` |
| propriété | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| propriété | `IReadOnlyList<string> Enable { get; init; }` |
| propriété | `IReadOnlyList<string> Disable { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propriété | `IReadOnlyList<string> EffectiveDisable { get; }` |
| propriété | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| propriété | `string OsFamily { get; init; }` |
| propriété | `string OsVersion { get; init; }` |
| propriété | `string OsArchitecture { get; init; }` |
| propriété | `string HostArchitecture { get; init; }` |
| propriété | `string OmsiArchitecture { get; init; }` |
| propriété | `string PluginArchitecture { get; init; }` |
| propriété | `bool CurrentPlatformSupported { get; init; }` |
| propriété | `bool LegacyPlatform { get; init; }` |
| propriété | `bool Wow64Available { get; init; }` |
| propriété | `bool InstallationWritable { get; init; }` |
| propriété | `bool ProcessLaunchSupported { get; init; }` |
| propriété | `bool PluginRuntimeSupported { get; init; }` |
| propriété | `bool NativeInteropSupported { get; init; }` |
| propriété | `bool SharedMemorySupported { get; init; }` |
| propriété | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SemanticDate(int Year, int Month, int Day)` |
| propriété | `int Year { get; init; }` |
| propriété | `int Month { get; init; }` |
| propriété | `int Day { get; init; }` |

### `SemanticTime`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SemanticTime(int Hour, int Minute, int Second)` |
| propriété | `int Hour { get; init; }` |
| propriété | `int Minute { get; init; }` |
| propriété | `int Second { get; init; }` |

### `SessionHandle`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionHandle(Guid SessionId)` |
| propriété | `Guid SessionId { get; init; }` |

### `SessionPlan`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| propriété | `Guid SessionId { get; init; }` |
| propriété | `string BuildProfileId { get; init; }` |
| propriété | `LaunchSpec Spec { get; init; }` |
| propriété | `RuntimePlatformInfo Platform { get; init; }` |
| propriété | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| propriété | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| propriété | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| propriété | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| propriété | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| propriété | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| propriété | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propriété | `bool IsRunnable { get; init; }` |
| propriété | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| propriété | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| propriété | `SplashMode Splash { get; init; }` |
| propriété | `OptionalValue<string> Language { get; init; }` |
| propriété | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| propriété | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| propriété | `string Id { get; init; }` |
| propriété | `string Name { get; init; }` |
| propriété | `string Version { get; init; }` |
| propriété | `string Author { get; init; }` |
| propriété | `string PresetId { get; init; }` |
| propriété | `int PresetIndex { get; init; }` |
| propriété | `string PresetName { get; init; }` |
| propriété | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| propriété | `int Index { get; init; }` |
| propriété | `string Id { get; init; }` |
| propriété | `string Name { get; init; }` |

### `SessionProfileSummary`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| propriété | `string Id { get; init; }` |
| propriété | `string Name { get; init; }` |
| propriété | `string Version { get; init; }` |
| propriété | `string Author { get; init; }` |
| propriété | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propriété | `string MapIdentity { get; init; }` |
| propriété | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| propriété | `bool IsValid { get; init; }` |
| propriété | `string ErrorCode { get; init; }` |
| propriété | `string ErrorMessage { get; init; }` |

### `SessionState`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| valeur | `Created = 0` |
| valeur | `ValidatingPlatform = 1` |
| valeur | `Planning = 2` |
| valeur | `AcquiringInstallationLock = 3` |
| valeur | `RecoveringPreviousTransaction = 4` |
| valeur | `Snapshotting = 5` |
| valeur | `ApplyingConfiguration = 6` |
| valeur | `DeployingRuntime = 7` |
| valeur | `CreatingStartupHandoff = 8` |
| valeur | `StartingProcess = 9` |
| valeur | `WaitingForPlugin = 10` |
| valeur | `PluginBootstrap = 11` |
| valeur | `StartingWorld = 12` |
| valeur | `EnteringGameplay = 13` |
| valeur | `Running = 14` |
| valeur | `ProcessExited = 15` |
| valeur | `Restoring = 16` |
| valeur | `CleaningRuntime = 17` |
| valeur | `Completed = 18` |
| valeur | `Failed = 19` |

### `SessionStatus`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| propriété | `Guid SessionId { get; init; }` |
| propriété | `SessionState State { get; init; }` |
| propriété | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| propriété | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Unset = 0` |
| valeur | `Native = 0` |
| valeur | `Managed = 1` |

### `StartDateTime`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| propriété | `int? Year { get; init; }` |
| propriété | `int? DayOfYear { get; init; }` |
| propriété | `int? Hour { get; init; }` |
| propriété | `int? Minute { get; init; }` |
| propriété | `int? Second { get; init; }` |
| propriété | `bool IsEmpty { get; }` |

### `StartWeather`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| propriété | `WeatherMode Mode { get; init; }` |
| propriété | `string Preset { get; init; }` |
| propriété | `string Icao { get; init; }` |

### `StartupHandoff`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| propriété | `Guid SessionId { get; init; }` |
| propriété | `string BuildProfileId { get; init; }` |
| propriété | `WorldMode WorldMode { get; init; }` |
| propriété | `string MapIdentity { get; init; }` |
| propriété | `int PresentedEntrypointIndex { get; init; }` |
| propriété | `bool HeadlessStart { get; init; }` |
| propriété | `bool PlayerVehicleEnabled { get; init; }` |
| propriété | `DateTimeMode DateMode { get; init; }` |
| propriété | `DateTimeMode TimeMode { get; init; }` |
| propriété | `string EntrypointIdentity { get; init; }` |
| propriété | `string SituationIdentity { get; init; }` |
| propriété | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| propriété | `bool PluginTrace { get; init; }` |
| propriété | `bool NativeTrace { get; init; }` |
| propriété | `StartDateTime Start { get; init; }` |
| propriété | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `uint Magic = 1330402120` |
| constante | `ushort Version = 7` |
| constante | `int MaxModules = 64` |
| méthode statique | `byte[] Serialize(StartupHandoff value)` |
| méthode statique | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| propriété | `string RelativeDirectory { get; init; }` |
| propriété | `string ManifestSha256 { get; init; }` |
| propriété | `string Settings { get; init; }` |

### `TelemetryRingWire`

Classe statique dans `OmsiLaunch.Api`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `uint Magic = 1381256271` |
| constante | `int Version = 1` |
| constante | `int SlotCount = 64` |
| constante | `int SlotSize = 2048` |
| constante | `int HeaderSize = 64` |
| constante | `int Capacity = 131136` |
| constante | `int MaxPayload = 2036` |
| méthode statique | `void Initialize(MemoryMappedViewAccessor view)` |
| méthode statique | `bool IsRing(MemoryMappedViewAccessor view)` |
| méthode statique | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| méthode statique | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| propriété | `DateTimeMode Mode { get; init; }` |
| propriété | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Énumération (`byte`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `Unset = 0` |
| valeur | `Preset = 1` |
| valeur | `Icao = 2` |
| valeur | `RealCurrent = 3` |

### `WeatherSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| propriété | `WeatherMode Mode { get; init; }` |
| propriété | `OptionalValue<string> Preset { get; init; }` |
| propriété | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Énumération (`int`) dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| valeur | `NewMap = 0` |
| valeur | `SavedSituation = 1` |
| valeur | `LastMapState = 2` |
| valeur | `LastSituation = 2` |

### `WorldSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `STABLE_BETA`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| propriété | `WorldMode Mode { get; init; }` |
| propriété | `OptionalValue<string> MapIdentity { get; init; }` |
| propriété | `OptionalValue<string> SituationIdentity { get; init; }` |
| propriété | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| propriété | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| propriété | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Record scellé dans `OmsiLaunch.Api`. Stabilité : `PARTIAL`. Sémantique : [launchspec](launchspec.md).

| Membre | Signature |
| --- | --- |
| constructeur | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| propriété | `DateTimeMode Mode { get; init; }` |
| propriété | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Classe scellée dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `HofPlanningException(string code, string message)` |
| propriété | `string Code { get; }` |

### `LaunchValidation`

Classe statique dans `OmsiLaunch.Core`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode statique | `bool IsMapIdentity(string value)` |
| méthode statique | `bool IsSha256(string value)` |
| méthode statique | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Classe statique dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode statique | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Record scellé dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| propriété | `string PluginBuildDirectory { get; init; }` |
| propriété | `string NativeBridgePath { get; init; }` |
| propriété | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Classe scellée dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| méthode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| méthode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| méthode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| méthode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| méthode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Record scellé dans `OmsiLaunch.Core`. Stabilité : `EXPERIMENTAL`. Sémantique : [session-profiles](session-profiles.md).

| Membre | Signature |
| --- | --- |
| constructeur | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| propriété | `string Map { get; init; }` |
| propriété | `int? EntrypointIndex { get; init; }` |
| propriété | `string EntrypointIdentity { get; init; }` |
| propriété | `SemanticDate Date { get; init; }` |
| propriété | `SemanticTime Time { get; init; }` |
| propriété | `int? Year { get; init; }` |
| propriété | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Record scellé dans `OmsiLaunch.Core`. Stabilité : `EXPERIMENTAL`. Sémantique : [session-profiles](session-profiles.md).

| Membre | Signature |
| --- | --- |
| constructeur | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| propriété | `int Index { get; init; }` |
| propriété | `string Id { get; init; }` |
| propriété | `string Name { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| propriété | `SessionPresentationSpec Presentation { get; init; }` |
| propriété | `InternetTexturesSpec InternetTextures { get; init; }` |
| propriété | `LaunchBehaviorSpec Behavior { get; init; }` |
| propriété | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| propriété | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Classe scellée dans `OmsiLaunch.Core`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionPlanner(IRuntimePlatform platform)` |
| méthode | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Classe statique dans `OmsiLaunch.Core`. Stabilité : `EXPERIMENTAL`. Sémantique : [session-profiles](session-profiles.md).

| Membre | Signature |
| --- | --- |
| constante | `string Schema = "omsilaunch.session-profile/v1"` |
| constante | `long MaxBytes = 262144` |
| constante | `int MinPresetIndex = 1` |
| constante | `int MaxPresetIndex = 99` |
| champ statique | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| méthode statique | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| méthode statique | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| méthode statique | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| méthode statique | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Classe scellée dans `OmsiLaunch.Core`. Stabilité : `EXPERIMENTAL`. Sémantique : [session-profiles](session-profiles.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionProfileException(string code, string message)` |
| propriété | `string Code { get; }` |

### `SessionProfilePackage`

Record scellé dans `OmsiLaunch.Core`. Stabilité : `EXPERIMENTAL`. Sémantique : [session-profiles](session-profiles.md).

| Membre | Signature |
| --- | --- |
| constructeur | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| propriété | `string RootPath { get; init; }` |
| propriété | `SessionProfileMetadata Metadata { get; init; }` |
| propriété | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| propriété | `ProfileNew New { get; init; }` |
| propriété | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Classe statique dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `int MinimumYear = 1900` |
| constante | `int MaximumYear = 2200` |
| méthode statique | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Classe statique dans `OmsiLaunch.Core`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode statique | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| propriété | `string Name { get; }` |
| propriété | `Guid SessionId { get; }` |
| méthode statique | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| méthode | `void Dispose()` |
| méthode | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| propriété | `string Name { get; }` |
| propriété | `Guid SessionId { get; }` |
| méthode statique | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| méthode | `void Dispose()` |

### `CurrentTelemetryStore`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| propriété | `string Name { get; }` |
| méthode statique | `CurrentTelemetryStore Create(Guid sessionId)` |
| méthode | `void Dispose()` |
| méthode | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `CurrentWindowsX64Platform()` |
| méthode | `RuntimePlatformInfo Detect(string root)` |
| méthode | `bool HasExited(LaunchedProcess p)` |
| méthode | `bool IsInstallationWritable(string root)` |
| méthode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| méthode | `void Terminate(LaunchedProcess p)` |
| méthode | `void ValidateCurrent(RuntimePlatformInfo p)` |
| méthode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interface dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode | `OmsiProcessState Observe()` |
| méthode | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| méthode | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interface dans `OmsiLaunch.Process`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode | `RuntimePlatformInfo Detect(string installationRoot)` |
| méthode | `bool HasExited(LaunchedProcess process)` |
| méthode | `bool IsInstallationWritable(string installationRoot)` |
| méthode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| méthode | `void Terminate(LaunchedProcess process)` |
| méthode | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| méthode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| méthode statique | `InstallationLease Acquire(string installationRoot)` |
| méthode | `void Dispose()` |
| méthode statique | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| propriété | `int ProcessId { get; }` |
| propriété | `int ThreadId { get; }` |
| propriété | `ProcessIdentity Identity { get; }` |
| méthode | `void Dispose()` |

### `ModuleSessionProcess`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `STABLE_BETA`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `int MaxLineBytes = 65536` |
| propriété | `int ProcessId { get; }` |
| propriété | `bool HasExited { get; }` |
| propriété | `int? ExitCode { get; }` |
| méthode | `void Dispose()` |
| méthode | `void Kill()` |
| méthode statique | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| méthode | `bool TryWriteLine(string line)` |
| méthode | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Record scellé dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| propriété | `string State { get; init; }` |
| propriété | `int? ProcessId { get; init; }` |
| propriété | `bool Responding { get; init; }` |

### `ProcessIdentity`

Record scellé dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| propriété | `int ProcessId { get; init; }` |
| propriété | `DateTimeOffset CreationTimeUtc { get; init; }` |
| propriété | `string ExecutablePath { get; init; }` |
| propriété | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Classe statique dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constante | `string FileName = "release-manifest.json"` |
| méthode statique | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| méthode statique | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Record scellé dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| propriété | `string SourcePath { get; init; }` |
| propriété | `string DestinationRelativePath { get; init; }` |
| propriété | `string Sha256 { get; init; }` |
| propriété | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Classe scellée dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| propriété | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| propriété | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| propriété | `string IntegrityReference { get; }` |
| méthode statique | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| méthode | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Record scellé dans `OmsiLaunch.Process`. Stabilité : `INTERNAL`. Sémantique : [public-api](public-api.md).

| Membre | Signature |
| --- | --- |
| constructeur | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| propriété | `string ExecutablePath { get; init; }` |
| propriété | `string WorkingDirectory { get; init; }` |
| propriété | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
