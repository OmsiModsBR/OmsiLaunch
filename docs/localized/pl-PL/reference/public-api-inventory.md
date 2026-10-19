# Inwentarz publicznego API

<!-- l10n: source=reference/public-api-inventory.md source-sha256=1038165496fa7e22c0b936388cb2dde7b90ed33b193b8f77d462d10d9eff149a -->
> Tłumaczenie [oryginalnej strony w języku angielskim](../../../reference/public-api-inventory.md) dla OmsiLaunch 0.1.0-beta4. Wiążąca jest strona angielska: w razie rozbieżności obowiązują strona angielska i kod.

Ta strona jest generowana z skompilowanych assembly przez `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); kontrola dokumentacji kończy się błędem, gdy strona przestaje odpowiadać kodowi. Wymienia każdy eksportowany typ z `OmsiLaunch.Api`, `OmsiLaunch.Core` i `OmsiLaunch.Process` wraz z jego stabilnością i sygnaturą każdej publicznej składowej. Semantyka, warunki wstępne, błędy i przykłady są opisane na stronie wskazanej w każdym nagłówku (domyślnie: [publiczne API](public-api.md)). Słownik stabilności: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (publiczne z przyczyn technicznych, nie jest powierzchnią integracji), `UNAVAILABLE` (zob. [publiczne API](public-api.md#stability-vocabulary)). Składowe rekordów generowane przez kompilator (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) zostały pominięte.

## `OmsiLaunch.Api`

### `Capability`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| właściwość | `string Name { get; init; }` |
| właściwość | `bool Available { get; init; }` |
| właściwość | `string EvidenceState { get; init; }` |
| właściwość | `string Reason { get; init; }` |

### `ContentIdentity`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| właściwość | `string Identity { get; init; }` |
| właściwość | `string Kind { get; init; }` |
| właściwość | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Maps = 0` |
| wartość | `Situations = 1` |
| wartość | `Vehicles = 2` |
| wartość | `Repaints = 3` |
| wartość | `Hofs = 4` |
| wartość | `FleetNumbers = 5` |
| wartość | `Registrations = 6` |
| wartość | `Addons = 7` |
| wartość | `Entrypoints = 8` |
| wartość | `SessionProfiles = 9` |
| wartość | `RuntimeModules = 10` |

### `D3DDeviceState`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `NotReady = 0` |
| wartość | `Ready = 1` |
| wartość | `Lost = 2` |
| wartość | `Resetting = 3` |
| wartość | `Stopping = 4` |
| wartość | `Stopped = 5` |

### `D3DDeviceStatus`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| właściwość | `bool Available { get; init; }` |
| właściwość | `D3DDeviceState State { get; init; }` |
| właściwość | `uint Generation { get; init; }` |
| właściwość | `uint LiveTextureCount { get; init; }` |
| właściwość | `bool ResetHookInstalled { get; init; }` |
| właściwość | `uint ExecutionThreadId { get; init; }` |
| właściwość | `uint LastResetThreadId { get; init; }` |
| właściwość | `int QueryInterfaceHResult { get; init; }` |
| właściwość | `int CooperativeLevelHResult { get; init; }` |
| właściwość | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda rozszerzająca | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metoda rozszerzająca | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metoda rozszerzająca | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metoda rozszerzająca | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| metoda rozszerzająca | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| właściwość | `D3DTextureHandle Handle { get; init; }` |
| właściwość | `D3DTextureResourceState State { get; init; }` |
| właściwość | `D3DDeviceState DeviceState { get; init; }` |
| właściwość | `uint Generation { get; init; }` |
| właściwość | `uint Width { get; init; }` |
| właściwość | `uint Height { get; init; }` |
| właściwość | `D3DTextureFormat Format { get; init; }` |
| właściwość | `uint Levels { get; init; }` |
| właściwość | `uint Level { get; init; }` |
| właściwość | `uint LevelWidth { get; init; }` |
| właściwość | `uint LevelHeight { get; init; }` |
| właściwość | `int HResult { get; init; }` |
| właściwość | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `A8R8G8B8 = 0` |
| wartość | `X8R8G8B8 = 1` |
| wartość | `R5G6B5 = 2` |
| wartość | `X1R5G5B5 = 3` |
| wartość | `A1R5G5B5 = 4` |
| wartość | `A4R4G4B4 = 5` |
| wartość | `A8 = 6` |
| wartość | `L8 = 7` |
| wartość | `A8L8 = 8` |

### `D3DTextureHandle`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `D3DTextureHandle(string Value)` |
| właściwość | `string Value { get; init; }` |

### `D3DTextureResourceState`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Live = 0` |
| wartość | `Released = 1` |
| wartość | `Stale = 2` |

### `D3DTextureUpdate`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| właściwość | `uint Level { get; init; }` |
| właściwość | `uint X { get; init; }` |
| właściwość | `uint Y { get; init; }` |
| właściwość | `uint Width { get; init; }` |
| właściwość | `uint Height { get; init; }` |
| właściwość | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| właściwość | `DateTimeMode Mode { get; init; }` |
| właściwość | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Unset = 0` |
| wartość | `Explicit = 1` |
| wartość | `System = 2` |

### `DiagnosticsSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| właściwość | `bool Log { get; init; }` |
| właściwość | `bool Verbose { get; init; }` |
| właściwość | `bool OmsiLogAll { get; init; }` |
| właściwość | `bool ProcessTrace { get; init; }` |
| właściwość | `bool PluginTrace { get; init; }` |
| właściwość | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Unset = 0` |
| wartość | `PresentedIndex = 1` |
| wartość | `Identity = 2` |

### `EntrypointSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| właściwość | `EntrypointMode Mode { get; init; }` |
| właściwość | `OptionalValue<int> PresentedIndex { get; init; }` |
| właściwość | `OptionalValue<string> Identity { get; init; }` |
| właściwość statyczna | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Add = 0` |
| wartość | `Mirror = 1` |

### `HofManagementSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| właściwość | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| właściwość | `HofSourceKind SourceKind { get; init; }` |
| właściwość | `string Source { get; init; }` |
| właściwość | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| właściwość | `HofApplyMode Mode { get; init; }` |
| właściwość | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Collection = 0` |
| wartość | `Directory = 1` |
| wartość | `Vehicle = 2` |
| wartość | `File = 3` |

### `IOmsiLaunch`

Interfejs w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| metoda | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| metoda | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| metoda | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| właściwość | `OptionalValue<string> KeyboardDocument { get; init; }` |
| właściwość | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda statyczna | `string IdentityKey(string root)` |
| metoda statyczna | `string NormalizeRoot(string root)` |
| metoda statyczna | `IReadOnlyList<string> Segments(string relativePath)` |
| metoda statyczna | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| właściwość | `string RootPath { get; init; }` |
| właściwość | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Native = 0` |
| wartość | `Disabled = 1` |
| wartość | `Override = 2` |

### `InternetTexturesSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| właściwość | `InternetTexturesMode Mode { get; init; }` |
| właściwość | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| właściwość | `bool RestoreConfiguration { get; init; }` |
| właściwość | `bool SuppressStaleClosecheckWarning { get; init; }` |
| właściwość | `int StartupTimeoutSeconds { get; init; }` |
| właściwość | `int ShutdownTimeoutSeconds { get; init; }` |
| właściwość | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| właściwość | `string Code { get; init; }` |
| właściwość | `string Message { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| właściwość | `InstallationSpec Installation { get; init; }` |
| właściwość | `WorldSpec World { get; init; }` |
| właściwość | `DateSpec Date { get; init; }` |
| właściwość | `TimeSpec Time { get; init; }` |
| właściwość | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| właściwość | `EnvironmentSpec Environment { get; init; }` |
| właściwość | `LaunchBehaviorSpec Behavior { get; init; }` |
| właściwość | `YearSpec Year { get; init; }` |
| właściwość | `WeatherSpec Weather { get; init; }` |
| właściwość | `InputSpec Input { get; init; }` |
| właściwość | `DiagnosticsSpec Diagnostics { get; init; }` |
| właściwość | `SessionPresentationSpec Presentation { get; init; }` |
| właściwość | `InternetTexturesSpec InternetTextures { get; init; }` |
| właściwość | `SessionProfileMetadata SessionProfile { get; init; }` |
| właściwość | `HofManagementSpec HofManagement { get; init; }` |
| właściwość | `RuntimeModulesSpec Modules { get; init; }` |
| właściwość | `HofManagementSpec EffectiveHofManagement { get; }` |
| właściwość | `RuntimeModulesSpec EffectiveModules { get; }` |
| właściwość | `YearSpec EffectiveYear { get; }` |
| właściwość | `WeatherSpec EffectiveWeather { get; }` |
| właściwość | `InputSpec EffectiveInput { get; }` |
| właściwość | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| właściwość | `SessionPresentationSpec EffectivePresentation { get; }` |
| właściwość | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Zapieczętowana klasa w `OmsiLaunch.Api`. Stabilność: `EXPERIMENTAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `OmsiRuntimeException(string code, string detail = null)` |
| właściwość | `string Code { get; }` |

### `OptionalValue`

Record struct w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `OptionalValue(Presence Presence, T Value)` |
| właściwość | `Presence Presence { get; init; }` |
| właściwość | `T Value { get; init; }` |
| właściwość | `bool IsSet { get; }` |
| właściwość statyczna | `OptionalValue<T> Unset { get; }` |
| metoda statyczna | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null, IReadOnlyList<string> RuntimeOperations = null, int TickMilliseconds = 0, string SnapshotSha256 = null)` |
| stała | `string Eligible = "ELIGIBLE"` |
| stała | `string Rejected = "REJECTED"` |
| stała | `string ActivationRequested = "requested"` |
| stała | `string ActivationSettingsFile = "settings-file"` |
| stała | `string ActivationContent = "content"` |
| stała | `string ActivationDefault = "default"` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Version { get; init; }` |
| właściwość | `string Status { get; init; }` |
| właściwość | `bool Requested { get; init; }` |
| właściwość | `string FailurePolicy { get; init; }` |
| właściwość | `IReadOnlyList<string> Operations { get; init; }` |
| właściwość | `string Code { get; init; }` |
| właściwość | `string Reason { get; init; }` |
| właściwość | `string ManifestSha256 { get; init; }` |
| właściwość | `string Activation { get; init; }` |
| właściwość | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| właściwość | `IReadOnlyList<string> RuntimeOperations { get; init; }` |
| właściwość | `int TickMilliseconds { get; init; }` |
| właściwość | `string SnapshotSha256 { get; init; }` |
| właściwość | `bool IsEligible { get; }` |
| właściwość | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |
| właściwość | `IReadOnlyList<string> EffectiveRuntimeOperations { get; }` |

### `PlannedModuleSetting`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| właściwość | `string Key { get; init; }` |
| właściwość | `string Value { get; init; }` |
| właściwość | `string Source { get; init; }` |

### `PlannedMutation`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| właściwość | `string RelativePath { get; init; }` |
| właściwość | `string SemanticKey { get; init; }` |
| właściwość | `string RequestedValue { get; init; }` |
| właściwość | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| właściwość | `OptionalValue<string> Model { get; init; }` |
| właściwość | `OptionalValue<string> Repaint { get; init; }` |
| właściwość | `OptionalValue<string> Hof { get; init; }` |
| właściwość | `OptionalValue<string> FleetNumber { get; init; }` |
| właściwość | `OptionalValue<string> Registration { get; init; }` |
| właściwość | `bool Enabled { get; }` |

### `Presence`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Unset = 0` |
| wartość | `Set = 1` |

### `PublicCapabilityClassification`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [capabilities](capabilities.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `PublicStableBeta = 0` |
| wartość | `PublicExperimental = 1` |
| wartość | `InternalOnly = 2` |
| wartość | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [capabilities](capabilities.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Family { get; init; }` |
| właściwość | `PublicCapabilityClassification Classification { get; init; }` |
| właściwość | `PublicCapabilityKind Kind { get; init; }` |
| właściwość | `bool RequiresSession { get; init; }` |
| właściwość | `bool RequiresExactProfile { get; init; }` |
| właściwość | `string ApiRoute { get; init; }` |
| właściwość | `string CliRoute { get; init; }` |
| właściwość | `string RuntimeValidation { get; init; }` |
| właściwość | `string Description { get; init; }` |
| właściwość | `IReadOnlyList<string> HandleTypes { get; init; }` |
| właściwość | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| właściwość | `string NativeCapability { get; init; }` |
| właściwość | `string EvidenceNote { get; init; }` |
| właściwość | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| właściwość | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [capabilities](capabilities.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Read = 0` |
| wartość | `Write = 1` |
| wartość | `Action = 2` |
| wartość | `Event = 3` |

### `PublicCapabilityRegistry`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [capabilities](capabilities.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string ProtocolVersion = "0.1"` |
| pole statyczne | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| właściwość statyczna | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| właściwość statyczna | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| właściwość statyczna | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| metoda statyczna | `PublicCapabilityDescriptor Get(string id)` |
| metoda statyczna | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| metoda statyczna | `bool IsInternalResultKey(string key)` |
| metoda statyczna | `bool IsModuleOperation(string operation)` |
| metoda statyczna | `bool IsPublicRuntimeOperation(string operation)` |
| metoda statyczna | `string NativeCapabilityOf(string operation)` |
| metoda statyczna | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| metoda statyczna | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| metoda statyczna | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [errors](errors.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string InvalidArgument = "invalid_argument"` |
| stała | `string UnsupportedProfile = "unsupported_profile"` |
| stała | `string Session = "session"` |
| stała | `string Runtime = "runtime"` |
| stała | `string NotFound = "not_found"` |
| stała | `string Transaction = "transaction"` |
| stała | `string Internal = "internal"` |

### `PublicErrorCodes`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [errors](errors.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string CANCELLED = "OL_E_CANCELLED"` |
| stała | `string INTERNAL = "OL_E_INTERNAL"` |
| stała | `string TIMEOUT = "OL_E_TIMEOUT"` |
| stała | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| stała | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| stała | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| stała | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| stała | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| stała | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| stała | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| stała | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| stała | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| stała | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| stała | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| stała | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| stała | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| stała | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| stała | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| stała | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| stała | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| stała | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| stała | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| stała | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| stała | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| stała | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| stała | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| stała | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| stała | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| stała | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| stała | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| stała | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| stała | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| stała | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| stała | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| stała | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| stała | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| stała | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| stała | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| stała | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| stała | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| stała | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| stała | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| stała | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| stała | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| stała | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| stała | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| stała | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| stała | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| stała | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| stała | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| stała | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| stała | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| stała | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| stała | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| stała | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| stała | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| stała | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| stała | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| stała | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| stała | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| stała | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| stała | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| stała | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| stała | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| stała | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| stała | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| stała | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| stała | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| stała | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| stała | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| stała | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| stała | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| stała | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| stała | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| stała | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| stała | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| stała | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| stała | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| stała | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| stała | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| stała | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| stała | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| stała | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| stała | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| stała | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| stała | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| stała | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| stała | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| stała | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| stała | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| stała | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| stała | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| stała | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| stała | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| stała | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| stała | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| stała | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| stała | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| stała | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| stała | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| stała | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| stała | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| stała | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| stała | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| stała | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| stała | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| stała | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| stała | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| stała | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| stała | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| stała | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| stała | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| stała | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| stała | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| stała | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| stała | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| stała | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| stała | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| stała | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| stała | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| stała | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| stała | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| stała | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| stała | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| stała | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| stała | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| stała | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| stała | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| stała | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| stała | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| stała | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| stała | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| stała | `string START_SESSION = "OL_E_START_SESSION"` |
| stała | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| stała | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| stała | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| stała | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| stała | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| stała | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| stała | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| stała | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| stała | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| stała | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| stała | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| stała | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| stała | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| stała | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| stała | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| stała | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| stała | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| stała | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| stała | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| stała | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| stała | `string MODULE_OPERATION_NOT_DECLARED = "OL_E_MODULE_OPERATION_NOT_DECLARED"` |
| stała | `string MODULE_CONTENT_INPUT_UNDECLARED = "OL_E_MODULE_CONTENT_INPUT_UNDECLARED"` |
| stała | `string MODULE_CONTENT_INPUT_INVALID = "OL_E_MODULE_CONTENT_INPUT_INVALID"` |
| stała | `string UI_NOT_PAUSED = "OL_E_UI_NOT_PAUSED"` |
| stała | `string UI_BUSY = "OL_E_UI_BUSY"` |
| stała | `string UI_ALREADY_TAKEN_OVER = "OL_E_UI_ALREADY_TAKEN_OVER"` |
| stała | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| stała | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| stała | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| stała | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| stała | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| stała | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| stała | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| stała | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| stała | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| stała | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| stała | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| stała | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| stała | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| stała | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| stała | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| stała | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| stała | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| stała | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| stała | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| stała | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| stała | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| stała | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| stała | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| stała | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| pole statyczne | `IReadOnlyList<PublicErrorDescriptor> All` |
| metoda statyczna | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [errors](errors.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PublicErrorDescriptor(string Code, string Category)` |
| właściwość | `string Code { get; init; }` |
| właściwość | `string Category { get; init; }` |

### `PublicEvidence`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| stała | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| stała | `string Partial = "PARTIAL"` |
| stała | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| stała | `string Unavailable = "UNAVAILABLE"` |
| pole statyczne | `IReadOnlyList<string> All` |

### `PublicExitCode`

Wyliczenie (`int`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [exit-codes](exit-codes.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Success = 0` |
| wartość | `SessionFailed = 1` |
| wartość | `InvalidArguments = 2` |
| wartość | `UnsupportedProfile = 3` |
| wartość | `NoActiveSession = 4` |
| wartość | `RuntimeUnavailable = 5` |
| wartość | `NotFound = 6` |
| wartość | `OperationRejected = 7` |
| wartość | `TransactionRecoveryFailed = 8` |
| wartość | `InternalError = 10` |

### `PublicMutationPolicy`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string None = "none"` |
| stała | `string OmsiState = "omsi-state"` |
| stała | `string SessionResource = "session-resource"` |
| stała | `string SessionPolicy = "session-policy"` |
| pole statyczne | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| właściwość | `string Name { get; init; }` |
| właściwość | `bool Required { get; init; }` |
| właściwość | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| właściwość | `bool Accepted { get; init; }` |
| właściwość | `string ErrorCode { get; init; }` |
| właściwość | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string NativeCapability { get; init; }` |
| właściwość | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| właściwość | `IReadOnlyList<string> CliRoutes { get; init; }` |
| właściwość | `bool Paged { get; init; }` |
| właściwość | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| właściwość | `bool Pending { get; init; }` |
| właściwość | `bool Recovered { get; init; }` |
| właściwość | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [runtime-control](runtime-control.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| właściwość | `Guid SessionId { get; init; }` |
| właściwość | `ulong RequestId { get; init; }` |
| właściwość | `string Operation { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [runtime-control](runtime-control.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| właściwość | `Guid SessionId { get; init; }` |
| właściwość | `ulong RequestId { get; init; }` |
| właściwość | `bool Succeeded { get; init; }` |
| właściwość | `string ErrorCode { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `uint Magic = 1330401859` |
| stała | `ushort Version = 1` |
| stała | `int HeaderSize = 72` |
| metoda statyczna | `byte[] SerializeRequest(RuntimeCommand command)` |
| metoda statyczna | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| metoda statyczna | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| metoda statyczna | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| metoda statyczna | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| właściwość | `string Type { get; init; }` |
| właściwość | `DateTimeOffset TimestampUtc { get; init; }` |
| właściwość | `long Sequence { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| właściwość | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| właściwość | `string Key { get; init; }` |
| właściwość | `string Type { get; init; }` |
| właściwość | `string Default { get; init; }` |
| właściwość | `bool AllowsAuto { get; init; }` |
| właściwość | `string Minimum { get; init; }` |
| właściwość | `string Maximum { get; init; }` |
| właściwość | `IReadOnlyList<string> Choices { get; init; }` |
| właściwość | `string Flag { get; init; }` |
| właściwość | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Version { get; init; }` |
| właściwość | `string Name { get; init; }` |
| właściwość | `string Activation { get; init; }` |
| właściwość | `bool IsValid { get; init; }` |
| właściwość | `IReadOnlyList<string> Operations { get; init; }` |
| właściwość | `string ContentBindingMap { get; init; }` |
| właściwość | `string ErrorCode { get; init; }` |
| właściwość | `string ErrorMessage { get; init; }` |
| właściwość | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| właściwość | `IReadOnlyList<string> Enable { get; init; }` |
| właściwość | `IReadOnlyList<string> Disable { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| właściwość | `IReadOnlyList<string> EffectiveDisable { get; }` |
| właściwość | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| właściwość | `string OsFamily { get; init; }` |
| właściwość | `string OsVersion { get; init; }` |
| właściwość | `string OsArchitecture { get; init; }` |
| właściwość | `string HostArchitecture { get; init; }` |
| właściwość | `string OmsiArchitecture { get; init; }` |
| właściwość | `string PluginArchitecture { get; init; }` |
| właściwość | `bool CurrentPlatformSupported { get; init; }` |
| właściwość | `bool LegacyPlatform { get; init; }` |
| właściwość | `bool Wow64Available { get; init; }` |
| właściwość | `bool InstallationWritable { get; init; }` |
| właściwość | `bool ProcessLaunchSupported { get; init; }` |
| właściwość | `bool PluginRuntimeSupported { get; init; }` |
| właściwość | `bool NativeInteropSupported { get; init; }` |
| właściwość | `bool SharedMemorySupported { get; init; }` |
| właściwość | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SemanticDate(int Year, int Month, int Day)` |
| właściwość | `int Year { get; init; }` |
| właściwość | `int Month { get; init; }` |
| właściwość | `int Day { get; init; }` |

### `SemanticTime`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SemanticTime(int Hour, int Minute, int Second)` |
| właściwość | `int Hour { get; init; }` |
| właściwość | `int Minute { get; init; }` |
| właściwość | `int Second { get; init; }` |

### `SessionHandle`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionHandle(Guid SessionId)` |
| właściwość | `Guid SessionId { get; init; }` |

### `SessionPlan`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| właściwość | `Guid SessionId { get; init; }` |
| właściwość | `string BuildProfileId { get; init; }` |
| właściwość | `LaunchSpec Spec { get; init; }` |
| właściwość | `RuntimePlatformInfo Platform { get; init; }` |
| właściwość | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| właściwość | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| właściwość | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| właściwość | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| właściwość | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| właściwość | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| właściwość | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| właściwość | `bool IsRunnable { get; init; }` |
| właściwość | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| właściwość | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| właściwość | `SplashMode Splash { get; init; }` |
| właściwość | `OptionalValue<string> Language { get; init; }` |
| właściwość | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| właściwość | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Name { get; init; }` |
| właściwość | `string Version { get; init; }` |
| właściwość | `string Author { get; init; }` |
| właściwość | `string PresetId { get; init; }` |
| właściwość | `int PresetIndex { get; init; }` |
| właściwość | `string PresetName { get; init; }` |
| właściwość | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| właściwość | `int Index { get; init; }` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Name { get; init; }` |

### `SessionProfileSummary`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Name { get; init; }` |
| właściwość | `string Version { get; init; }` |
| właściwość | `string Author { get; init; }` |
| właściwość | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| właściwość | `string MapIdentity { get; init; }` |
| właściwość | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| właściwość | `bool IsValid { get; init; }` |
| właściwość | `string ErrorCode { get; init; }` |
| właściwość | `string ErrorMessage { get; init; }` |

### `SessionState`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Created = 0` |
| wartość | `ValidatingPlatform = 1` |
| wartość | `Planning = 2` |
| wartość | `AcquiringInstallationLock = 3` |
| wartość | `RecoveringPreviousTransaction = 4` |
| wartość | `Snapshotting = 5` |
| wartość | `ApplyingConfiguration = 6` |
| wartość | `DeployingRuntime = 7` |
| wartość | `CreatingStartupHandoff = 8` |
| wartość | `StartingProcess = 9` |
| wartość | `WaitingForPlugin = 10` |
| wartość | `PluginBootstrap = 11` |
| wartość | `StartingWorld = 12` |
| wartość | `EnteringGameplay = 13` |
| wartość | `Running = 14` |
| wartość | `ProcessExited = 15` |
| wartość | `Restoring = 16` |
| wartość | `CleaningRuntime = 17` |
| wartość | `Completed = 18` |
| wartość | `Failed = 19` |

### `SessionStatus`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| właściwość | `Guid SessionId { get; init; }` |
| właściwość | `SessionState State { get; init; }` |
| właściwość | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| właściwość | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Unset = 0` |
| wartość | `Native = 0` |
| wartość | `Managed = 1` |

### `StartDateTime`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| właściwość | `int? Year { get; init; }` |
| właściwość | `int? DayOfYear { get; init; }` |
| właściwość | `int? Hour { get; init; }` |
| właściwość | `int? Minute { get; init; }` |
| właściwość | `int? Second { get; init; }` |
| właściwość | `bool IsEmpty { get; }` |

### `StartWeather`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| właściwość | `WeatherMode Mode { get; init; }` |
| właściwość | `string Preset { get; init; }` |
| właściwość | `string Icao { get; init; }` |

### `StartupHandoff`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| właściwość | `Guid SessionId { get; init; }` |
| właściwość | `string BuildProfileId { get; init; }` |
| właściwość | `WorldMode WorldMode { get; init; }` |
| właściwość | `string MapIdentity { get; init; }` |
| właściwość | `int PresentedEntrypointIndex { get; init; }` |
| właściwość | `bool HeadlessStart { get; init; }` |
| właściwość | `bool PlayerVehicleEnabled { get; init; }` |
| właściwość | `DateTimeMode DateMode { get; init; }` |
| właściwość | `DateTimeMode TimeMode { get; init; }` |
| właściwość | `string EntrypointIdentity { get; init; }` |
| właściwość | `string SituationIdentity { get; init; }` |
| właściwość | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| właściwość | `bool PluginTrace { get; init; }` |
| właściwość | `bool NativeTrace { get; init; }` |
| właściwość | `StartDateTime Start { get; init; }` |
| właściwość | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `uint Magic = 1330402120` |
| stała | `ushort Version = 8` |
| stała | `int MaxModules = 64` |
| metoda statyczna | `byte[] Serialize(StartupHandoff value)` |
| metoda statyczna | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "", string SnapshotSha256 = "")` |
| właściwość | `string RelativeDirectory { get; init; }` |
| właściwość | `string ManifestSha256 { get; init; }` |
| właściwość | `string Settings { get; init; }` |
| właściwość | `string SnapshotSha256 { get; init; }` |

### `TelemetryRingWire`

Klasa statyczna w `OmsiLaunch.Api`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `uint Magic = 1381256271` |
| stała | `int Version = 1` |
| stała | `int SlotCount = 64` |
| stała | `int SlotSize = 2048` |
| stała | `int HeaderSize = 64` |
| stała | `int Capacity = 131136` |
| stała | `int MaxPayload = 2036` |
| metoda statyczna | `void Initialize(MemoryMappedViewAccessor view)` |
| metoda statyczna | `bool IsRing(MemoryMappedViewAccessor view)` |
| metoda statyczna | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| metoda statyczna | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| właściwość | `DateTimeMode Mode { get; init; }` |
| właściwość | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Wyliczenie (`byte`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `Unset = 0` |
| wartość | `Preset = 1` |
| wartość | `Icao = 2` |
| wartość | `RealCurrent = 3` |

### `WeatherSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| właściwość | `WeatherMode Mode { get; init; }` |
| właściwość | `OptionalValue<string> Preset { get; init; }` |
| właściwość | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Wyliczenie (`int`) w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| wartość | `NewMap = 0` |
| wartość | `SavedSituation = 1` |
| wartość | `LastMapState = 2` |
| wartość | `LastSituation = 2` |

### `WorldSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `STABLE_BETA`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| właściwość | `WorldMode Mode { get; init; }` |
| właściwość | `OptionalValue<string> MapIdentity { get; init; }` |
| właściwość | `OptionalValue<string> SituationIdentity { get; init; }` |
| właściwość | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| właściwość | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| właściwość | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Zapieczętowany rekord w `OmsiLaunch.Api`. Stabilność: `PARTIAL`. Semantyka: [launchspec](launchspec.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| właściwość | `DateTimeMode Mode { get; init; }` |
| właściwość | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Zapieczętowana klasa w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `HofPlanningException(string code, string message)` |
| właściwość | `string Code { get; }` |

### `LaunchValidation`

Klasa statyczna w `OmsiLaunch.Core`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda statyczna | `bool IsMapIdentity(string value)` |
| metoda statyczna | `bool IsSha256(string value)` |
| metoda statyczna | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Klasa statyczna w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda statyczna | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Zapieczętowany rekord w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| właściwość | `string PluginBuildDirectory { get; init; }` |
| właściwość | `string NativeBridgePath { get; init; }` |
| właściwość | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Zapieczętowana klasa w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| metoda | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| metoda | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| metoda | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| metoda | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| metoda | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Zapieczętowany rekord w `OmsiLaunch.Core`. Stabilność: `EXPERIMENTAL`. Semantyka: [session-profiles](session-profiles.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| właściwość | `string Map { get; init; }` |
| właściwość | `int? EntrypointIndex { get; init; }` |
| właściwość | `string EntrypointIdentity { get; init; }` |
| właściwość | `SemanticDate Date { get; init; }` |
| właściwość | `SemanticTime Time { get; init; }` |
| właściwość | `int? Year { get; init; }` |
| właściwość | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Zapieczętowany rekord w `OmsiLaunch.Core`. Stabilność: `EXPERIMENTAL`. Semantyka: [session-profiles](session-profiles.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| właściwość | `int Index { get; init; }` |
| właściwość | `string Id { get; init; }` |
| właściwość | `string Name { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| właściwość | `SessionPresentationSpec Presentation { get; init; }` |
| właściwość | `InternetTexturesSpec InternetTextures { get; init; }` |
| właściwość | `LaunchBehaviorSpec Behavior { get; init; }` |
| właściwość | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| właściwość | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Zapieczętowana klasa w `OmsiLaunch.Core`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionPlanner(IRuntimePlatform platform)` |
| metoda | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Klasa statyczna w `OmsiLaunch.Core`. Stabilność: `EXPERIMENTAL`. Semantyka: [session-profiles](session-profiles.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string Schema = "omsilaunch.session-profile/v1"` |
| stała | `long MaxBytes = 262144` |
| stała | `int MinPresetIndex = 1` |
| stała | `int MaxPresetIndex = 99` |
| pole statyczne | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| metoda statyczna | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| metoda statyczna | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| metoda statyczna | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| metoda statyczna | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Zapieczętowana klasa w `OmsiLaunch.Core`. Stabilność: `EXPERIMENTAL`. Semantyka: [session-profiles](session-profiles.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionProfileException(string code, string message)` |
| właściwość | `string Code { get; }` |

### `SessionProfilePackage`

Zapieczętowany rekord w `OmsiLaunch.Core`. Stabilność: `EXPERIMENTAL`. Semantyka: [session-profiles](session-profiles.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| właściwość | `string RootPath { get; init; }` |
| właściwość | `SessionProfileMetadata Metadata { get; init; }` |
| właściwość | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| właściwość | `ProfileNew New { get; init; }` |
| właściwość | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Klasa statyczna w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `int MinimumYear = 1900` |
| stała | `int MaximumYear = 2200` |
| metoda statyczna | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Klasa statyczna w `OmsiLaunch.Core`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda statyczna | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| właściwość | `string Name { get; }` |
| właściwość | `Guid SessionId { get; }` |
| metoda statyczna | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| metoda | `void Dispose()` |
| metoda | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| właściwość | `string Name { get; }` |
| właściwość | `Guid SessionId { get; }` |
| metoda statyczna | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| metoda | `void Dispose()` |

### `CurrentTelemetryStore`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| właściwość | `string Name { get; }` |
| metoda statyczna | `CurrentTelemetryStore Create(Guid sessionId)` |
| metoda | `void Dispose()` |
| metoda | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `CurrentWindowsX64Platform()` |
| metoda | `RuntimePlatformInfo Detect(string root)` |
| metoda | `bool HasExited(LaunchedProcess p)` |
| metoda | `bool IsInstallationWritable(string root)` |
| metoda | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| metoda | `void Terminate(LaunchedProcess p)` |
| metoda | `void ValidateCurrent(RuntimePlatformInfo p)` |
| metoda | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interfejs w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda | `OmsiProcessState Observe()` |
| metoda | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| metoda | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interfejs w `OmsiLaunch.Process`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda | `RuntimePlatformInfo Detect(string installationRoot)` |
| metoda | `bool HasExited(LaunchedProcess process)` |
| metoda | `bool IsInstallationWritable(string installationRoot)` |
| metoda | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| metoda | `void Terminate(LaunchedProcess process)` |
| metoda | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| metoda | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| metoda statyczna | `InstallationLease Acquire(string installationRoot)` |
| metoda | `void Dispose()` |
| metoda statyczna | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| właściwość | `int ProcessId { get; }` |
| właściwość | `int ThreadId { get; }` |
| właściwość | `ProcessIdentity Identity { get; }` |
| metoda | `void Dispose()` |

### `ModuleSessionProcess`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `STABLE_BETA`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `int MaxLineBytes = 65536` |
| właściwość | `int ProcessId { get; }` |
| właściwość | `bool HasExited { get; }` |
| właściwość | `int? ExitCode { get; }` |
| metoda | `void Dispose()` |
| metoda | `void Kill()` |
| metoda statyczna | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| metoda | `bool TryWriteLine(string line)` |
| metoda | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Zapieczętowany rekord w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| właściwość | `string State { get; init; }` |
| właściwość | `int? ProcessId { get; init; }` |
| właściwość | `bool Responding { get; init; }` |

### `ProcessIdentity`

Zapieczętowany rekord w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| właściwość | `int ProcessId { get; init; }` |
| właściwość | `DateTimeOffset CreationTimeUtc { get; init; }` |
| właściwość | `string ExecutablePath { get; init; }` |
| właściwość | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Klasa statyczna w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| stała | `string FileName = "release-manifest.json"` |
| metoda statyczna | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| metoda statyczna | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Zapieczętowany rekord w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| właściwość | `string SourcePath { get; init; }` |
| właściwość | `string DestinationRelativePath { get; init; }` |
| właściwość | `string Sha256 { get; init; }` |
| właściwość | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Zapieczętowana klasa w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| właściwość | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| właściwość | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| właściwość | `string IntegrityReference { get; }` |
| metoda statyczna | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| metoda | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Zapieczętowany rekord w `OmsiLaunch.Process`. Stabilność: `INTERNAL`. Semantyka: [public-api](public-api.md).

| Składowa | Sygnatura |
| --- | --- |
| konstruktor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| właściwość | `string ExecutablePath { get; init; }` |
| właściwość | `string WorkingDirectory { get; init; }` |
| właściwość | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
