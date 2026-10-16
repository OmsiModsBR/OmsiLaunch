# Inventar der öffentlichen API

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> Übersetzung der [englischen Originalseite](../../../reference/public-api-inventory.md) für OmsiLaunch 0.1.0-beta4. Maßgeblich ist die englische Seite: Bei Abweichungen gelten die englische Seite und der Code.

Diese Seite wird von `tests/OmsiLaunch.DocumentationTests` aus den kompilierten Assemblies erzeugt (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); die Dokumentationsprüfung schlägt fehl, sobald sie nicht mehr zum Code passt. Sie führt jeden exportierten Typ von `OmsiLaunch.Api`, `OmsiLaunch.Core` und `OmsiLaunch.Process` mit seiner Stabilität und der Signatur jedes öffentlichen Members auf. Semantik, Vorbedingungen, Fehler und Beispiele sind auf der Seite dokumentiert, die in der jeweiligen Überschrift genannt ist (Standard: [öffentliche API](public-api.md)). Stabilitätsstufen: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (aus technischen Gründen öffentlich, keine Integrationsschnittstelle), `UNAVAILABLE` (siehe [öffentliche API](public-api.md#stability-vocabulary)). Vom Compiler erzeugte Record-Member (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) werden nicht aufgeführt.

## `OmsiLaunch.Api`

### `Capability`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `bool Available { get; init; }` |
| Eigenschaft | `string EvidenceState { get; init; }` |
| Eigenschaft | `string Reason { get; init; }` |

### `ContentIdentity`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| Eigenschaft | `string Identity { get; init; }` |
| Eigenschaft | `string Kind { get; init; }` |
| Eigenschaft | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `Maps = 0` |
| Wert | `Situations = 1` |
| Wert | `Vehicles = 2` |
| Wert | `Repaints = 3` |
| Wert | `Hofs = 4` |
| Wert | `FleetNumbers = 5` |
| Wert | `Registrations = 6` |
| Wert | `Addons = 7` |
| Wert | `Entrypoints = 8` |
| Wert | `SessionProfiles = 9` |
| Wert | `RuntimeModules = 10` |

### `D3DDeviceState`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `NotReady = 0` |
| Wert | `Ready = 1` |
| Wert | `Lost = 2` |
| Wert | `Resetting = 3` |
| Wert | `Stopping = 4` |
| Wert | `Stopped = 5` |

### `D3DDeviceStatus`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| Eigenschaft | `bool Available { get; init; }` |
| Eigenschaft | `D3DDeviceState State { get; init; }` |
| Eigenschaft | `uint Generation { get; init; }` |
| Eigenschaft | `uint LiveTextureCount { get; init; }` |
| Eigenschaft | `bool ResetHookInstalled { get; init; }` |
| Eigenschaft | `uint ExecutionThreadId { get; init; }` |
| Eigenschaft | `uint LastResetThreadId { get; init; }` |
| Eigenschaft | `int QueryInterfaceHResult { get; init; }` |
| Eigenschaft | `int CooperativeLevelHResult { get; init; }` |
| Eigenschaft | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Erweiterungsmethode | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| Erweiterungsmethode | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| Erweiterungsmethode | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Erweiterungsmethode | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| Erweiterungsmethode | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| Eigenschaft | `D3DTextureHandle Handle { get; init; }` |
| Eigenschaft | `D3DTextureResourceState State { get; init; }` |
| Eigenschaft | `D3DDeviceState DeviceState { get; init; }` |
| Eigenschaft | `uint Generation { get; init; }` |
| Eigenschaft | `uint Width { get; init; }` |
| Eigenschaft | `uint Height { get; init; }` |
| Eigenschaft | `D3DTextureFormat Format { get; init; }` |
| Eigenschaft | `uint Levels { get; init; }` |
| Eigenschaft | `uint Level { get; init; }` |
| Eigenschaft | `uint LevelWidth { get; init; }` |
| Eigenschaft | `uint LevelHeight { get; init; }` |
| Eigenschaft | `int HResult { get; init; }` |
| Eigenschaft | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `A8R8G8B8 = 0` |
| Wert | `X8R8G8B8 = 1` |
| Wert | `R5G6B5 = 2` |
| Wert | `X1R5G5B5 = 3` |
| Wert | `A1R5G5B5 = 4` |
| Wert | `A4R4G4B4 = 5` |
| Wert | `A8 = 6` |
| Wert | `L8 = 7` |
| Wert | `A8L8 = 8` |

### `D3DTextureHandle`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `D3DTextureHandle(string Value)` |
| Eigenschaft | `string Value { get; init; }` |

### `D3DTextureResourceState`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `Live = 0` |
| Wert | `Released = 1` |
| Wert | `Stale = 2` |

### `D3DTextureUpdate`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| Eigenschaft | `uint Level { get; init; }` |
| Eigenschaft | `uint X { get; init; }` |
| Eigenschaft | `uint Y { get; init; }` |
| Eigenschaft | `uint Width { get; init; }` |
| Eigenschaft | `uint Height { get; init; }` |
| Eigenschaft | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| Eigenschaft | `DateTimeMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Unset = 0` |
| Wert | `Explicit = 1` |
| Wert | `System = 2` |

### `DiagnosticsSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| Eigenschaft | `bool Log { get; init; }` |
| Eigenschaft | `bool Verbose { get; init; }` |
| Eigenschaft | `bool OmsiLogAll { get; init; }` |
| Eigenschaft | `bool ProcessTrace { get; init; }` |
| Eigenschaft | `bool PluginTrace { get; init; }` |
| Eigenschaft | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Unset = 0` |
| Wert | `PresentedIndex = 1` |
| Wert | `Identity = 2` |

### `EntrypointSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| Eigenschaft | `EntrypointMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<int> PresentedIndex { get; init; }` |
| Eigenschaft | `OptionalValue<string> Identity { get; init; }` |
| statische Eigenschaft | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `Add = 0` |
| Wert | `Mirror = 1` |

### `HofManagementSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| Eigenschaft | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| Eigenschaft | `HofSourceKind SourceKind { get; init; }` |
| Eigenschaft | `string Source { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| Eigenschaft | `HofApplyMode Mode { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `Collection = 0` |
| Wert | `Directory = 1` |
| Wert | `Vehicle = 2` |
| Wert | `File = 3` |

### `IOmsiLaunch`

Interface in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Methode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Methode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Methode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Methode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| Eigenschaft | `OptionalValue<string> KeyboardDocument { get; init; }` |
| Eigenschaft | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| statische Methode | `string IdentityKey(string root)` |
| statische Methode | `string NormalizeRoot(string root)` |
| statische Methode | `IReadOnlyList<string> Segments(string relativePath)` |
| statische Methode | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| Eigenschaft | `string RootPath { get; init; }` |
| Eigenschaft | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Native = 0` |
| Wert | `Disabled = 1` |
| Wert | `Override = 2` |

### `InternetTexturesSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| Eigenschaft | `InternetTexturesMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| Eigenschaft | `bool RestoreConfiguration { get; init; }` |
| Eigenschaft | `bool SuppressStaleClosecheckWarning { get; init; }` |
| Eigenschaft | `int StartupTimeoutSeconds { get; init; }` |
| Eigenschaft | `int ShutdownTimeoutSeconds { get; init; }` |
| Eigenschaft | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| Eigenschaft | `string Code { get; init; }` |
| Eigenschaft | `string Message { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| Eigenschaft | `InstallationSpec Installation { get; init; }` |
| Eigenschaft | `WorldSpec World { get; init; }` |
| Eigenschaft | `DateSpec Date { get; init; }` |
| Eigenschaft | `TimeSpec Time { get; init; }` |
| Eigenschaft | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| Eigenschaft | `EnvironmentSpec Environment { get; init; }` |
| Eigenschaft | `LaunchBehaviorSpec Behavior { get; init; }` |
| Eigenschaft | `YearSpec Year { get; init; }` |
| Eigenschaft | `WeatherSpec Weather { get; init; }` |
| Eigenschaft | `InputSpec Input { get; init; }` |
| Eigenschaft | `DiagnosticsSpec Diagnostics { get; init; }` |
| Eigenschaft | `SessionPresentationSpec Presentation { get; init; }` |
| Eigenschaft | `InternetTexturesSpec InternetTextures { get; init; }` |
| Eigenschaft | `SessionProfileMetadata SessionProfile { get; init; }` |
| Eigenschaft | `HofManagementSpec HofManagement { get; init; }` |
| Eigenschaft | `RuntimeModulesSpec Modules { get; init; }` |
| Eigenschaft | `HofManagementSpec EffectiveHofManagement { get; }` |
| Eigenschaft | `RuntimeModulesSpec EffectiveModules { get; }` |
| Eigenschaft | `YearSpec EffectiveYear { get; }` |
| Eigenschaft | `WeatherSpec EffectiveWeather { get; }` |
| Eigenschaft | `InputSpec EffectiveInput { get; }` |
| Eigenschaft | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| Eigenschaft | `SessionPresentationSpec EffectivePresentation { get; }` |
| Eigenschaft | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Versiegelte Klasse in `OmsiLaunch.Api`. Stabilität: `EXPERIMENTAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `OmsiRuntimeException(string code, string detail = null)` |
| Eigenschaft | `string Code { get; }` |

### `OptionalValue`

Record struct in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `OptionalValue(Presence Presence, T Value)` |
| Eigenschaft | `Presence Presence { get; init; }` |
| Eigenschaft | `T Value { get; init; }` |
| Eigenschaft | `bool IsSet { get; }` |
| statische Eigenschaft | `OptionalValue<T> Unset { get; }` |
| statische Methode | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| Konstante | `string Eligible = "ELIGIBLE"` |
| Konstante | `string Rejected = "REJECTED"` |
| Konstante | `string ActivationRequested = "requested"` |
| Konstante | `string ActivationSettingsFile = "settings-file"` |
| Konstante | `string ActivationContent = "content"` |
| Konstante | `string ActivationDefault = "default"` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Version { get; init; }` |
| Eigenschaft | `string Status { get; init; }` |
| Eigenschaft | `bool Requested { get; init; }` |
| Eigenschaft | `string FailurePolicy { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> Operations { get; init; }` |
| Eigenschaft | `string Code { get; init; }` |
| Eigenschaft | `string Reason { get; init; }` |
| Eigenschaft | `string ManifestSha256 { get; init; }` |
| Eigenschaft | `string Activation { get; init; }` |
| Eigenschaft | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| Eigenschaft | `bool IsEligible { get; }` |
| Eigenschaft | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PlannedModuleSetting(string Key, string Value, string Source)` |
| Eigenschaft | `string Key { get; init; }` |
| Eigenschaft | `string Value { get; init; }` |
| Eigenschaft | `string Source { get; init; }` |

### `PlannedMutation`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| Eigenschaft | `string RelativePath { get; init; }` |
| Eigenschaft | `string SemanticKey { get; init; }` |
| Eigenschaft | `string RequestedValue { get; init; }` |
| Eigenschaft | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| Eigenschaft | `OptionalValue<string> Model { get; init; }` |
| Eigenschaft | `OptionalValue<string> Repaint { get; init; }` |
| Eigenschaft | `OptionalValue<string> Hof { get; init; }` |
| Eigenschaft | `OptionalValue<string> FleetNumber { get; init; }` |
| Eigenschaft | `OptionalValue<string> Registration { get; init; }` |
| Eigenschaft | `bool Enabled { get; }` |

### `Presence`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Unset = 0` |
| Wert | `Set = 1` |

### `PublicCapabilityClassification`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [capabilities](capabilities.md).

| Member | Signatur |
| --- | --- |
| Wert | `PublicStableBeta = 0` |
| Wert | `PublicExperimental = 1` |
| Wert | `InternalOnly = 2` |
| Wert | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [capabilities](capabilities.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Family { get; init; }` |
| Eigenschaft | `PublicCapabilityClassification Classification { get; init; }` |
| Eigenschaft | `PublicCapabilityKind Kind { get; init; }` |
| Eigenschaft | `bool RequiresSession { get; init; }` |
| Eigenschaft | `bool RequiresExactProfile { get; init; }` |
| Eigenschaft | `string ApiRoute { get; init; }` |
| Eigenschaft | `string CliRoute { get; init; }` |
| Eigenschaft | `string RuntimeValidation { get; init; }` |
| Eigenschaft | `string Description { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> HandleTypes { get; init; }` |
| Eigenschaft | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| Eigenschaft | `string NativeCapability { get; init; }` |
| Eigenschaft | `string EvidenceNote { get; init; }` |
| Eigenschaft | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| Eigenschaft | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [capabilities](capabilities.md).

| Member | Signatur |
| --- | --- |
| Wert | `Read = 0` |
| Wert | `Write = 1` |
| Wert | `Action = 2` |
| Wert | `Event = 3` |

### `PublicCapabilityRegistry`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [capabilities](capabilities.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string ProtocolVersion = "0.1"` |
| statisches Feld | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| statische Eigenschaft | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| statische Eigenschaft | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| statische Eigenschaft | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| statische Methode | `PublicCapabilityDescriptor Get(string id)` |
| statische Methode | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| statische Methode | `bool IsInternalResultKey(string key)` |
| statische Methode | `bool IsModuleOperation(string operation)` |
| statische Methode | `bool IsPublicRuntimeOperation(string operation)` |
| statische Methode | `string NativeCapabilityOf(string operation)` |
| statische Methode | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| statische Methode | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| statische Methode | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [errors](errors.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string InvalidArgument = "invalid_argument"` |
| Konstante | `string UnsupportedProfile = "unsupported_profile"` |
| Konstante | `string Session = "session"` |
| Konstante | `string Runtime = "runtime"` |
| Konstante | `string NotFound = "not_found"` |
| Konstante | `string Transaction = "transaction"` |
| Konstante | `string Internal = "internal"` |

### `PublicErrorCodes`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [errors](errors.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string CANCELLED = "OL_E_CANCELLED"` |
| Konstante | `string INTERNAL = "OL_E_INTERNAL"` |
| Konstante | `string TIMEOUT = "OL_E_TIMEOUT"` |
| Konstante | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| Konstante | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| Konstante | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| Konstante | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| Konstante | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| Konstante | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| Konstante | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| Konstante | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| Konstante | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| Konstante | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| Konstante | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| Konstante | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| Konstante | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| Konstante | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| Konstante | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| Konstante | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| Konstante | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| Konstante | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| Konstante | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| Konstante | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| Konstante | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| Konstante | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| Konstante | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| Konstante | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| Konstante | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| Konstante | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| Konstante | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| Konstante | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| Konstante | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| Konstante | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| Konstante | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| Konstante | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| Konstante | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| Konstante | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| Konstante | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| Konstante | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| Konstante | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| Konstante | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| Konstante | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| Konstante | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| Konstante | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| Konstante | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| Konstante | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| Konstante | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| Konstante | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| Konstante | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| Konstante | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| Konstante | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| Konstante | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| Konstante | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| Konstante | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| Konstante | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| Konstante | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| Konstante | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| Konstante | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| Konstante | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| Konstante | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| Konstante | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| Konstante | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| Konstante | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| Konstante | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| Konstante | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| Konstante | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| Konstante | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| Konstante | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| Konstante | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| Konstante | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| Konstante | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| Konstante | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| Konstante | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| Konstante | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| Konstante | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| Konstante | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| Konstante | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| Konstante | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| Konstante | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| Konstante | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| Konstante | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| Konstante | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| Konstante | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| Konstante | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| Konstante | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| Konstante | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| Konstante | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| Konstante | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| Konstante | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| Konstante | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| Konstante | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| Konstante | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| Konstante | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| Konstante | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| Konstante | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| Konstante | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| Konstante | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| Konstante | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| Konstante | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| Konstante | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| Konstante | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| Konstante | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| Konstante | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| Konstante | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| Konstante | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| Konstante | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| Konstante | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| Konstante | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| Konstante | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| Konstante | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| Konstante | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| Konstante | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| Konstante | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| Konstante | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| Konstante | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| Konstante | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| Konstante | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| Konstante | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| Konstante | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| Konstante | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| Konstante | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| Konstante | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| Konstante | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| Konstante | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| Konstante | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| Konstante | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| Konstante | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| Konstante | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| Konstante | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| Konstante | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| Konstante | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| Konstante | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| Konstante | `string START_SESSION = "OL_E_START_SESSION"` |
| Konstante | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| Konstante | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| Konstante | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| Konstante | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| Konstante | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| Konstante | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| Konstante | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| Konstante | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| Konstante | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| Konstante | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| Konstante | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| Konstante | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| Konstante | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| Konstante | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| Konstante | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| Konstante | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| Konstante | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| Konstante | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| Konstante | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| Konstante | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| Konstante | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| Konstante | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| Konstante | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| Konstante | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| Konstante | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| Konstante | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| Konstante | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| Konstante | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| Konstante | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| Konstante | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| Konstante | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| Konstante | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| Konstante | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| Konstante | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| Konstante | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| Konstante | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| Konstante | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| Konstante | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| Konstante | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| Konstante | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| Konstante | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| Konstante | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| Konstante | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| Konstante | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| statisches Feld | `IReadOnlyList<PublicErrorDescriptor> All` |
| statische Methode | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [errors](errors.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PublicErrorDescriptor(string Code, string Category)` |
| Eigenschaft | `string Code { get; init; }` |
| Eigenschaft | `string Category { get; init; }` |

### `PublicEvidence`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| Konstante | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| Konstante | `string Partial = "PARTIAL"` |
| Konstante | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| Konstante | `string Unavailable = "UNAVAILABLE"` |
| statisches Feld | `IReadOnlyList<string> All` |

### `PublicExitCode`

Enumeration (`int`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [exit-codes](exit-codes.md).

| Member | Signatur |
| --- | --- |
| Wert | `Success = 0` |
| Wert | `SessionFailed = 1` |
| Wert | `InvalidArguments = 2` |
| Wert | `UnsupportedProfile = 3` |
| Wert | `NoActiveSession = 4` |
| Wert | `RuntimeUnavailable = 5` |
| Wert | `NotFound = 6` |
| Wert | `OperationRejected = 7` |
| Wert | `TransactionRecoveryFailed = 8` |
| Wert | `InternalError = 10` |

### `PublicMutationPolicy`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string None = "none"` |
| Konstante | `string OmsiState = "omsi-state"` |
| Konstante | `string SessionResource = "session-resource"` |
| Konstante | `string SessionPolicy = "session-policy"` |
| statisches Feld | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `bool Required { get; init; }` |
| Eigenschaft | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| Eigenschaft | `bool Accepted { get; init; }` |
| Eigenschaft | `string ErrorCode { get; init; }` |
| Eigenschaft | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string NativeCapability { get; init; }` |
| Eigenschaft | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> CliRoutes { get; init; }` |
| Eigenschaft | `bool Paged { get; init; }` |
| Eigenschaft | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| Eigenschaft | `bool Pending { get; init; }` |
| Eigenschaft | `bool Recovered { get; init; }` |
| Eigenschaft | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [runtime-control](runtime-control.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| Eigenschaft | `Guid SessionId { get; init; }` |
| Eigenschaft | `ulong RequestId { get; init; }` |
| Eigenschaft | `string Operation { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [runtime-control](runtime-control.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| Eigenschaft | `Guid SessionId { get; init; }` |
| Eigenschaft | `ulong RequestId { get; init; }` |
| Eigenschaft | `bool Succeeded { get; init; }` |
| Eigenschaft | `string ErrorCode { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `uint Magic = 1330401859` |
| Konstante | `ushort Version = 1` |
| Konstante | `int HeaderSize = 72` |
| statische Methode | `byte[] SerializeRequest(RuntimeCommand command)` |
| statische Methode | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| statische Methode | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| statische Methode | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| statische Methode | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| Eigenschaft | `string Type { get; init; }` |
| Eigenschaft | `DateTimeOffset TimestampUtc { get; init; }` |
| Eigenschaft | `long Sequence { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| Eigenschaft | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| Eigenschaft | `string Key { get; init; }` |
| Eigenschaft | `string Type { get; init; }` |
| Eigenschaft | `string Default { get; init; }` |
| Eigenschaft | `bool AllowsAuto { get; init; }` |
| Eigenschaft | `string Minimum { get; init; }` |
| Eigenschaft | `string Maximum { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> Choices { get; init; }` |
| Eigenschaft | `string Flag { get; init; }` |
| Eigenschaft | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Version { get; init; }` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `string Activation { get; init; }` |
| Eigenschaft | `bool IsValid { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> Operations { get; init; }` |
| Eigenschaft | `string ContentBindingMap { get; init; }` |
| Eigenschaft | `string ErrorCode { get; init; }` |
| Eigenschaft | `string ErrorMessage { get; init; }` |
| Eigenschaft | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| Eigenschaft | `IReadOnlyList<string> Enable { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> Disable { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> EffectiveDisable { get; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| Eigenschaft | `string OsFamily { get; init; }` |
| Eigenschaft | `string OsVersion { get; init; }` |
| Eigenschaft | `string OsArchitecture { get; init; }` |
| Eigenschaft | `string HostArchitecture { get; init; }` |
| Eigenschaft | `string OmsiArchitecture { get; init; }` |
| Eigenschaft | `string PluginArchitecture { get; init; }` |
| Eigenschaft | `bool CurrentPlatformSupported { get; init; }` |
| Eigenschaft | `bool LegacyPlatform { get; init; }` |
| Eigenschaft | `bool Wow64Available { get; init; }` |
| Eigenschaft | `bool InstallationWritable { get; init; }` |
| Eigenschaft | `bool ProcessLaunchSupported { get; init; }` |
| Eigenschaft | `bool PluginRuntimeSupported { get; init; }` |
| Eigenschaft | `bool NativeInteropSupported { get; init; }` |
| Eigenschaft | `bool SharedMemorySupported { get; init; }` |
| Eigenschaft | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SemanticDate(int Year, int Month, int Day)` |
| Eigenschaft | `int Year { get; init; }` |
| Eigenschaft | `int Month { get; init; }` |
| Eigenschaft | `int Day { get; init; }` |

### `SemanticTime`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SemanticTime(int Hour, int Minute, int Second)` |
| Eigenschaft | `int Hour { get; init; }` |
| Eigenschaft | `int Minute { get; init; }` |
| Eigenschaft | `int Second { get; init; }` |

### `SessionHandle`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionHandle(Guid SessionId)` |
| Eigenschaft | `Guid SessionId { get; init; }` |

### `SessionPlan`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| Eigenschaft | `Guid SessionId { get; init; }` |
| Eigenschaft | `string BuildProfileId { get; init; }` |
| Eigenschaft | `LaunchSpec Spec { get; init; }` |
| Eigenschaft | `RuntimePlatformInfo Platform { get; init; }` |
| Eigenschaft | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| Eigenschaft | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| Eigenschaft | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| Eigenschaft | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| Eigenschaft | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| Eigenschaft | `bool IsRunnable { get; init; }` |
| Eigenschaft | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| Eigenschaft | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| Eigenschaft | `SplashMode Splash { get; init; }` |
| Eigenschaft | `OptionalValue<string> Language { get; init; }` |
| Eigenschaft | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| Eigenschaft | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `string Version { get; init; }` |
| Eigenschaft | `string Author { get; init; }` |
| Eigenschaft | `string PresetId { get; init; }` |
| Eigenschaft | `int PresetIndex { get; init; }` |
| Eigenschaft | `string PresetName { get; init; }` |
| Eigenschaft | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| Eigenschaft | `int Index { get; init; }` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Name { get; init; }` |

### `SessionProfileSummary`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `string Version { get; init; }` |
| Eigenschaft | `string Author { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| Eigenschaft | `string MapIdentity { get; init; }` |
| Eigenschaft | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| Eigenschaft | `bool IsValid { get; init; }` |
| Eigenschaft | `string ErrorCode { get; init; }` |
| Eigenschaft | `string ErrorMessage { get; init; }` |

### `SessionState`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Wert | `Created = 0` |
| Wert | `ValidatingPlatform = 1` |
| Wert | `Planning = 2` |
| Wert | `AcquiringInstallationLock = 3` |
| Wert | `RecoveringPreviousTransaction = 4` |
| Wert | `Snapshotting = 5` |
| Wert | `ApplyingConfiguration = 6` |
| Wert | `DeployingRuntime = 7` |
| Wert | `CreatingStartupHandoff = 8` |
| Wert | `StartingProcess = 9` |
| Wert | `WaitingForPlugin = 10` |
| Wert | `PluginBootstrap = 11` |
| Wert | `StartingWorld = 12` |
| Wert | `EnteringGameplay = 13` |
| Wert | `Running = 14` |
| Wert | `ProcessExited = 15` |
| Wert | `Restoring = 16` |
| Wert | `CleaningRuntime = 17` |
| Wert | `Completed = 18` |
| Wert | `Failed = 19` |

### `SessionStatus`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| Eigenschaft | `Guid SessionId { get; init; }` |
| Eigenschaft | `SessionState State { get; init; }` |
| Eigenschaft | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| Eigenschaft | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Unset = 0` |
| Wert | `Native = 0` |
| Wert | `Managed = 1` |

### `StartDateTime`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| Eigenschaft | `int? Year { get; init; }` |
| Eigenschaft | `int? DayOfYear { get; init; }` |
| Eigenschaft | `int? Hour { get; init; }` |
| Eigenschaft | `int? Minute { get; init; }` |
| Eigenschaft | `int? Second { get; init; }` |
| Eigenschaft | `bool IsEmpty { get; }` |

### `StartWeather`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| Eigenschaft | `WeatherMode Mode { get; init; }` |
| Eigenschaft | `string Preset { get; init; }` |
| Eigenschaft | `string Icao { get; init; }` |

### `StartupHandoff`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| Eigenschaft | `Guid SessionId { get; init; }` |
| Eigenschaft | `string BuildProfileId { get; init; }` |
| Eigenschaft | `WorldMode WorldMode { get; init; }` |
| Eigenschaft | `string MapIdentity { get; init; }` |
| Eigenschaft | `int PresentedEntrypointIndex { get; init; }` |
| Eigenschaft | `bool HeadlessStart { get; init; }` |
| Eigenschaft | `bool PlayerVehicleEnabled { get; init; }` |
| Eigenschaft | `DateTimeMode DateMode { get; init; }` |
| Eigenschaft | `DateTimeMode TimeMode { get; init; }` |
| Eigenschaft | `string EntrypointIdentity { get; init; }` |
| Eigenschaft | `string SituationIdentity { get; init; }` |
| Eigenschaft | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| Eigenschaft | `bool PluginTrace { get; init; }` |
| Eigenschaft | `bool NativeTrace { get; init; }` |
| Eigenschaft | `StartDateTime Start { get; init; }` |
| Eigenschaft | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `uint Magic = 1330402120` |
| Konstante | `ushort Version = 7` |
| Konstante | `int MaxModules = 64` |
| statische Methode | `byte[] Serialize(StartupHandoff value)` |
| statische Methode | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| Eigenschaft | `string RelativeDirectory { get; init; }` |
| Eigenschaft | `string ManifestSha256 { get; init; }` |
| Eigenschaft | `string Settings { get; init; }` |

### `TelemetryRingWire`

Statische Klasse in `OmsiLaunch.Api`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `uint Magic = 1381256271` |
| Konstante | `int Version = 1` |
| Konstante | `int SlotCount = 64` |
| Konstante | `int SlotSize = 2048` |
| Konstante | `int HeaderSize = 64` |
| Konstante | `int Capacity = 131136` |
| Konstante | `int MaxPayload = 2036` |
| statische Methode | `void Initialize(MemoryMappedViewAccessor view)` |
| statische Methode | `bool IsRing(MemoryMappedViewAccessor view)` |
| statische Methode | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| statische Methode | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| Eigenschaft | `DateTimeMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Enumeration (`byte`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `Unset = 0` |
| Wert | `Preset = 1` |
| Wert | `Icao = 2` |
| Wert | `RealCurrent = 3` |

### `WeatherSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| Eigenschaft | `WeatherMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<string> Preset { get; init; }` |
| Eigenschaft | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Enumeration (`int`) in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Wert | `NewMap = 0` |
| Wert | `SavedSituation = 1` |
| Wert | `LastMapState = 2` |
| Wert | `LastSituation = 2` |

### `WorldSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `STABLE_BETA`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| Eigenschaft | `WorldMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<string> MapIdentity { get; init; }` |
| Eigenschaft | `OptionalValue<string> SituationIdentity { get; init; }` |
| Eigenschaft | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| Eigenschaft | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| Eigenschaft | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Versiegelter Record in `OmsiLaunch.Api`. Stabilität: `PARTIAL`. Semantik: [launchspec](launchspec.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| Eigenschaft | `DateTimeMode Mode { get; init; }` |
| Eigenschaft | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Versiegelte Klasse in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `HofPlanningException(string code, string message)` |
| Eigenschaft | `string Code { get; }` |

### `LaunchValidation`

Statische Klasse in `OmsiLaunch.Core`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| statische Methode | `bool IsMapIdentity(string value)` |
| statische Methode | `bool IsSha256(string value)` |
| statische Methode | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Statische Klasse in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| statische Methode | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Versiegelter Record in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| Eigenschaft | `string PluginBuildDirectory { get; init; }` |
| Eigenschaft | `string NativeBridgePath { get; init; }` |
| Eigenschaft | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Versiegelte Klasse in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| Methode | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| Methode | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| Methode | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| Methode | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| Methode | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Versiegelter Record in `OmsiLaunch.Core`. Stabilität: `EXPERIMENTAL`. Semantik: [session-profiles](session-profiles.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| Eigenschaft | `string Map { get; init; }` |
| Eigenschaft | `int? EntrypointIndex { get; init; }` |
| Eigenschaft | `string EntrypointIdentity { get; init; }` |
| Eigenschaft | `SemanticDate Date { get; init; }` |
| Eigenschaft | `SemanticTime Time { get; init; }` |
| Eigenschaft | `int? Year { get; init; }` |
| Eigenschaft | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Versiegelter Record in `OmsiLaunch.Core`. Stabilität: `EXPERIMENTAL`. Semantik: [session-profiles](session-profiles.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| Eigenschaft | `int Index { get; init; }` |
| Eigenschaft | `string Id { get; init; }` |
| Eigenschaft | `string Name { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| Eigenschaft | `SessionPresentationSpec Presentation { get; init; }` |
| Eigenschaft | `InternetTexturesSpec InternetTextures { get; init; }` |
| Eigenschaft | `LaunchBehaviorSpec Behavior { get; init; }` |
| Eigenschaft | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| Eigenschaft | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Versiegelte Klasse in `OmsiLaunch.Core`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionPlanner(IRuntimePlatform platform)` |
| Methode | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Statische Klasse in `OmsiLaunch.Core`. Stabilität: `EXPERIMENTAL`. Semantik: [session-profiles](session-profiles.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string Schema = "omsilaunch.session-profile/v1"` |
| Konstante | `long MaxBytes = 262144` |
| Konstante | `int MinPresetIndex = 1` |
| Konstante | `int MaxPresetIndex = 99` |
| statisches Feld | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| statische Methode | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| statische Methode | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| statische Methode | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| statische Methode | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Versiegelte Klasse in `OmsiLaunch.Core`. Stabilität: `EXPERIMENTAL`. Semantik: [session-profiles](session-profiles.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionProfileException(string code, string message)` |
| Eigenschaft | `string Code { get; }` |

### `SessionProfilePackage`

Versiegelter Record in `OmsiLaunch.Core`. Stabilität: `EXPERIMENTAL`. Semantik: [session-profiles](session-profiles.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| Eigenschaft | `string RootPath { get; init; }` |
| Eigenschaft | `SessionProfileMetadata Metadata { get; init; }` |
| Eigenschaft | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| Eigenschaft | `ProfileNew New { get; init; }` |
| Eigenschaft | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Statische Klasse in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `int MinimumYear = 1900` |
| Konstante | `int MaximumYear = 2200` |
| statische Methode | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Statische Klasse in `OmsiLaunch.Core`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| statische Methode | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Eigenschaft | `string Name { get; }` |
| Eigenschaft | `Guid SessionId { get; }` |
| statische Methode | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| Methode | `void Dispose()` |
| Methode | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Eigenschaft | `string Name { get; }` |
| Eigenschaft | `Guid SessionId { get; }` |
| statische Methode | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| Methode | `void Dispose()` |

### `CurrentTelemetryStore`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Eigenschaft | `string Name { get; }` |
| statische Methode | `CurrentTelemetryStore Create(Guid sessionId)` |
| Methode | `void Dispose()` |
| Methode | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `CurrentWindowsX64Platform()` |
| Methode | `RuntimePlatformInfo Detect(string root)` |
| Methode | `bool HasExited(LaunchedProcess p)` |
| Methode | `bool IsInstallationWritable(string root)` |
| Methode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| Methode | `void Terminate(LaunchedProcess p)` |
| Methode | `void ValidateCurrent(RuntimePlatformInfo p)` |
| Methode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Interface in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Methode | `OmsiProcessState Observe()` |
| Methode | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| Methode | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Interface in `OmsiLaunch.Process`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Methode | `RuntimePlatformInfo Detect(string installationRoot)` |
| Methode | `bool HasExited(LaunchedProcess process)` |
| Methode | `bool IsInstallationWritable(string installationRoot)` |
| Methode | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| Methode | `void Terminate(LaunchedProcess process)` |
| Methode | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| Methode | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| statische Methode | `InstallationLease Acquire(string installationRoot)` |
| Methode | `void Dispose()` |
| statische Methode | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Eigenschaft | `int ProcessId { get; }` |
| Eigenschaft | `int ThreadId { get; }` |
| Eigenschaft | `ProcessIdentity Identity { get; }` |
| Methode | `void Dispose()` |

### `ModuleSessionProcess`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `STABLE_BETA`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `int MaxLineBytes = 65536` |
| Eigenschaft | `int ProcessId { get; }` |
| Eigenschaft | `bool HasExited { get; }` |
| Eigenschaft | `int? ExitCode { get; }` |
| Methode | `void Dispose()` |
| Methode | `void Kill()` |
| statische Methode | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| Methode | `bool TryWriteLine(string line)` |
| Methode | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Versiegelter Record in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| Eigenschaft | `string State { get; init; }` |
| Eigenschaft | `int? ProcessId { get; init; }` |
| Eigenschaft | `bool Responding { get; init; }` |

### `ProcessIdentity`

Versiegelter Record in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| Eigenschaft | `int ProcessId { get; init; }` |
| Eigenschaft | `DateTimeOffset CreationTimeUtc { get; init; }` |
| Eigenschaft | `string ExecutablePath { get; init; }` |
| Eigenschaft | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Statische Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstante | `string FileName = "release-manifest.json"` |
| statische Methode | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| statische Methode | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Versiegelter Record in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| Eigenschaft | `string SourcePath { get; init; }` |
| Eigenschaft | `string DestinationRelativePath { get; init; }` |
| Eigenschaft | `string Sha256 { get; init; }` |
| Eigenschaft | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Versiegelte Klasse in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Eigenschaft | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| Eigenschaft | `string IntegrityReference { get; }` |
| statische Methode | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| Methode | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Versiegelter Record in `OmsiLaunch.Process`. Stabilität: `INTERNAL`. Semantik: [public-api](public-api.md).

| Member | Signatur |
| --- | --- |
| Konstruktor | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| Eigenschaft | `string ExecutablePath { get; init; }` |
| Eigenschaft | `string WorkingDirectory { get; init; }` |
| Eigenschaft | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
