# Перечень публичного API

<!-- l10n: source=reference/public-api-inventory.md source-sha256=1038165496fa7e22c0b936388cb2dde7b90ed33b193b8f77d462d10d9eff149a -->
> Перевод [исходной страницы на английском языке](../../../reference/public-api-inventory.md) для OmsiLaunch 0.1.0-beta4. Нормативной является английская страница: при расхождениях приоритет имеют английская страница и код.

Эта страница генерируется из скомпилированных сборок с помощью `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); проверка документации завершается ошибкой, если страница перестаёт соответствовать коду. Здесь перечислены все экспортируемые типы `OmsiLaunch.Api`, `OmsiLaunch.Core` и `OmsiLaunch.Process` с уровнем стабильности и сигнатурой каждого публичного члена. Семантика, предусловия, ошибки и примеры описаны на странице, указанной в каждом заголовке (по умолчанию: [публичный API](public-api.md)). Уровни стабильности: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (публичный по техническим причинам, не является интерфейсом интеграции), `UNAVAILABLE` (см. [публичный API](public-api.md#stability-vocabulary)). Члены записей, сгенерированные компилятором (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`), не приводятся.

## `OmsiLaunch.Api`

### `Capability`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| свойство | `string Name { get; init; }` |
| свойство | `bool Available { get; init; }` |
| свойство | `string EvidenceState { get; init; }` |
| свойство | `string Reason { get; init; }` |

### `ContentIdentity`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| свойство | `string Identity { get; init; }` |
| свойство | `string Kind { get; init; }` |
| свойство | `string DisplayName { get; init; }` |

### `ContentQueryKind`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Maps = 0` |
| значение | `Situations = 1` |
| значение | `Vehicles = 2` |
| значение | `Repaints = 3` |
| значение | `Hofs = 4` |
| значение | `FleetNumbers = 5` |
| значение | `Registrations = 6` |
| значение | `Addons = 7` |
| значение | `Entrypoints = 8` |
| значение | `SessionProfiles = 9` |
| значение | `RuntimeModules = 10` |

### `D3DDeviceState`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `NotReady = 0` |
| значение | `Ready = 1` |
| значение | `Lost = 2` |
| значение | `Resetting = 3` |
| значение | `Stopping = 4` |
| значение | `Stopped = 5` |

### `D3DDeviceStatus`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| свойство | `bool Available { get; init; }` |
| свойство | `D3DDeviceState State { get; init; }` |
| свойство | `uint Generation { get; init; }` |
| свойство | `uint LiveTextureCount { get; init; }` |
| свойство | `bool ResetHookInstalled { get; init; }` |
| свойство | `uint ExecutionThreadId { get; init; }` |
| свойство | `uint LastResetThreadId { get; init; }` |
| свойство | `int QueryInterfaceHResult { get; init; }` |
| свойство | `int CooperativeLevelHResult { get; init; }` |
| свойство | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

Статический класс в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| метод расширения | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| метод расширения | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| метод расширения | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| метод расширения | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| метод расширения | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| свойство | `D3DTextureHandle Handle { get; init; }` |
| свойство | `D3DTextureResourceState State { get; init; }` |
| свойство | `D3DDeviceState DeviceState { get; init; }` |
| свойство | `uint Generation { get; init; }` |
| свойство | `uint Width { get; init; }` |
| свойство | `uint Height { get; init; }` |
| свойство | `D3DTextureFormat Format { get; init; }` |
| свойство | `uint Levels { get; init; }` |
| свойство | `uint Level { get; init; }` |
| свойство | `uint LevelWidth { get; init; }` |
| свойство | `uint LevelHeight { get; init; }` |
| свойство | `int HResult { get; init; }` |
| свойство | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `A8R8G8B8 = 0` |
| значение | `X8R8G8B8 = 1` |
| значение | `R5G6B5 = 2` |
| значение | `X1R5G5B5 = 3` |
| значение | `A1R5G5B5 = 4` |
| значение | `A4R4G4B4 = 5` |
| значение | `A8 = 6` |
| значение | `L8 = 7` |
| значение | `A8L8 = 8` |

### `D3DTextureHandle`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `D3DTextureHandle(string Value)` |
| свойство | `string Value { get; init; }` |

### `D3DTextureResourceState`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Live = 0` |
| значение | `Released = 1` |
| значение | `Stale = 2` |

### `D3DTextureUpdate`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| свойство | `uint Level { get; init; }` |
| свойство | `uint X { get; init; }` |
| свойство | `uint Y { get; init; }` |
| свойство | `uint Width { get; init; }` |
| свойство | `uint Height { get; init; }` |
| свойство | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| свойство | `DateTimeMode Mode { get; init; }` |
| свойство | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Unset = 0` |
| значение | `Explicit = 1` |
| значение | `System = 2` |

### `DiagnosticsSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| свойство | `bool Log { get; init; }` |
| свойство | `bool Verbose { get; init; }` |
| свойство | `bool OmsiLogAll { get; init; }` |
| свойство | `bool ProcessTrace { get; init; }` |
| свойство | `bool PluginTrace { get; init; }` |
| свойство | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Unset = 0` |
| значение | `PresentedIndex = 1` |
| значение | `Identity = 2` |

### `EntrypointSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| свойство | `EntrypointMode Mode { get; init; }` |
| свойство | `OptionalValue<int> PresentedIndex { get; init; }` |
| свойство | `OptionalValue<string> Identity { get; init; }` |
| статическое свойство | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| свойство | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Add = 0` |
| значение | `Mirror = 1` |

### `HofManagementSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| свойство | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| свойство | `HofSourceKind SourceKind { get; init; }` |
| свойство | `string Source { get; init; }` |
| свойство | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| свойство | `HofApplyMode Mode { get; init; }` |
| свойство | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Collection = 0` |
| значение | `Directory = 1` |
| значение | `Vehicle = 2` |
| значение | `File = 3` |

### `IOmsiLaunch`

Интерфейс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| метод | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| метод | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| метод | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| метод | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| свойство | `OptionalValue<string> KeyboardDocument { get; init; }` |
| свойство | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| статический метод | `string IdentityKey(string root)` |
| статический метод | `string NormalizeRoot(string root)` |
| статический метод | `IReadOnlyList<string> Segments(string relativePath)` |
| статический метод | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| свойство | `string RootPath { get; init; }` |
| свойство | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Native = 0` |
| значение | `Disabled = 1` |
| значение | `Override = 2` |

### `InternetTexturesSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| свойство | `InternetTexturesMode Mode { get; init; }` |
| свойство | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| свойство | `bool RestoreConfiguration { get; init; }` |
| свойство | `bool SuppressStaleClosecheckWarning { get; init; }` |
| свойство | `int StartupTimeoutSeconds { get; init; }` |
| свойство | `int ShutdownTimeoutSeconds { get; init; }` |
| свойство | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| свойство | `string Code { get; init; }` |
| свойство | `string Message { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| свойство | `InstallationSpec Installation { get; init; }` |
| свойство | `WorldSpec World { get; init; }` |
| свойство | `DateSpec Date { get; init; }` |
| свойство | `TimeSpec Time { get; init; }` |
| свойство | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| свойство | `EnvironmentSpec Environment { get; init; }` |
| свойство | `LaunchBehaviorSpec Behavior { get; init; }` |
| свойство | `YearSpec Year { get; init; }` |
| свойство | `WeatherSpec Weather { get; init; }` |
| свойство | `InputSpec Input { get; init; }` |
| свойство | `DiagnosticsSpec Diagnostics { get; init; }` |
| свойство | `SessionPresentationSpec Presentation { get; init; }` |
| свойство | `InternetTexturesSpec InternetTextures { get; init; }` |
| свойство | `SessionProfileMetadata SessionProfile { get; init; }` |
| свойство | `HofManagementSpec HofManagement { get; init; }` |
| свойство | `RuntimeModulesSpec Modules { get; init; }` |
| свойство | `HofManagementSpec EffectiveHofManagement { get; }` |
| свойство | `RuntimeModulesSpec EffectiveModules { get; }` |
| свойство | `YearSpec EffectiveYear { get; }` |
| свойство | `WeatherSpec EffectiveWeather { get; }` |
| свойство | `InputSpec EffectiveInput { get; }` |
| свойство | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| свойство | `SessionPresentationSpec EffectivePresentation { get; }` |
| свойство | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

Запечатанный класс в `OmsiLaunch.Api`. Стабильность: `EXPERIMENTAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `OmsiRuntimeException(string code, string detail = null)` |
| свойство | `string Code { get; }` |

### `OptionalValue`

Record struct в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `OptionalValue(Presence Presence, T Value)` |
| свойство | `Presence Presence { get; init; }` |
| свойство | `T Value { get; init; }` |
| свойство | `bool IsSet { get; }` |
| статическое свойство | `OptionalValue<T> Unset { get; }` |
| статический метод | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null, IReadOnlyList<string> RuntimeOperations = null, int TickMilliseconds = 0, string SnapshotSha256 = null)` |
| константа | `string Eligible = "ELIGIBLE"` |
| константа | `string Rejected = "REJECTED"` |
| константа | `string ActivationRequested = "requested"` |
| константа | `string ActivationSettingsFile = "settings-file"` |
| константа | `string ActivationContent = "content"` |
| константа | `string ActivationDefault = "default"` |
| свойство | `string Id { get; init; }` |
| свойство | `string Version { get; init; }` |
| свойство | `string Status { get; init; }` |
| свойство | `bool Requested { get; init; }` |
| свойство | `string FailurePolicy { get; init; }` |
| свойство | `IReadOnlyList<string> Operations { get; init; }` |
| свойство | `string Code { get; init; }` |
| свойство | `string Reason { get; init; }` |
| свойство | `string ManifestSha256 { get; init; }` |
| свойство | `string Activation { get; init; }` |
| свойство | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| свойство | `IReadOnlyList<string> RuntimeOperations { get; init; }` |
| свойство | `int TickMilliseconds { get; init; }` |
| свойство | `string SnapshotSha256 { get; init; }` |
| свойство | `bool IsEligible { get; }` |
| свойство | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |
| свойство | `IReadOnlyList<string> EffectiveRuntimeOperations { get; }` |

### `PlannedModuleSetting`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PlannedModuleSetting(string Key, string Value, string Source)` |
| свойство | `string Key { get; init; }` |
| свойство | `string Value { get; init; }` |
| свойство | `string Source { get; init; }` |

### `PlannedMutation`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| свойство | `string RelativePath { get; init; }` |
| свойство | `string SemanticKey { get; init; }` |
| свойство | `string RequestedValue { get; init; }` |
| свойство | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| свойство | `OptionalValue<string> Model { get; init; }` |
| свойство | `OptionalValue<string> Repaint { get; init; }` |
| свойство | `OptionalValue<string> Hof { get; init; }` |
| свойство | `OptionalValue<string> FleetNumber { get; init; }` |
| свойство | `OptionalValue<string> Registration { get; init; }` |
| свойство | `bool Enabled { get; }` |

### `Presence`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Unset = 0` |
| значение | `Set = 1` |

### `PublicCapabilityClassification`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [capabilities](capabilities.md).

| Член | Сигнатура |
| --- | --- |
| значение | `PublicStableBeta = 0` |
| значение | `PublicExperimental = 1` |
| значение | `InternalOnly = 2` |
| значение | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [capabilities](capabilities.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| свойство | `string Id { get; init; }` |
| свойство | `string Family { get; init; }` |
| свойство | `PublicCapabilityClassification Classification { get; init; }` |
| свойство | `PublicCapabilityKind Kind { get; init; }` |
| свойство | `bool RequiresSession { get; init; }` |
| свойство | `bool RequiresExactProfile { get; init; }` |
| свойство | `string ApiRoute { get; init; }` |
| свойство | `string CliRoute { get; init; }` |
| свойство | `string RuntimeValidation { get; init; }` |
| свойство | `string Description { get; init; }` |
| свойство | `IReadOnlyList<string> HandleTypes { get; init; }` |
| свойство | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| свойство | `string NativeCapability { get; init; }` |
| свойство | `string EvidenceNote { get; init; }` |
| свойство | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| свойство | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [capabilities](capabilities.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Read = 0` |
| значение | `Write = 1` |
| значение | `Action = 2` |
| значение | `Event = 3` |

### `PublicCapabilityRegistry`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [capabilities](capabilities.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string ProtocolVersion = "0.1"` |
| статическое поле | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| статическое свойство | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| статическое свойство | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| статическое свойство | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| статический метод | `PublicCapabilityDescriptor Get(string id)` |
| статический метод | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| статический метод | `bool IsInternalResultKey(string key)` |
| статический метод | `bool IsModuleOperation(string operation)` |
| статический метод | `bool IsPublicRuntimeOperation(string operation)` |
| статический метод | `string NativeCapabilityOf(string operation)` |
| статический метод | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| статический метод | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| статический метод | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [errors](errors.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string InvalidArgument = "invalid_argument"` |
| константа | `string UnsupportedProfile = "unsupported_profile"` |
| константа | `string Session = "session"` |
| константа | `string Runtime = "runtime"` |
| константа | `string NotFound = "not_found"` |
| константа | `string Transaction = "transaction"` |
| константа | `string Internal = "internal"` |

### `PublicErrorCodes`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [errors](errors.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string CANCELLED = "OL_E_CANCELLED"` |
| константа | `string INTERNAL = "OL_E_INTERNAL"` |
| константа | `string TIMEOUT = "OL_E_TIMEOUT"` |
| константа | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| константа | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| константа | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| константа | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| константа | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| константа | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| константа | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| константа | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| константа | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| константа | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| константа | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| константа | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| константа | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| константа | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| константа | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| константа | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| константа | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| константа | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| константа | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| константа | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| константа | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| константа | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| константа | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| константа | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| константа | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| константа | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| константа | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| константа | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| константа | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| константа | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| константа | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| константа | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| константа | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| константа | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| константа | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| константа | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| константа | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| константа | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| константа | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| константа | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| константа | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| константа | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| константа | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| константа | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| константа | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| константа | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| константа | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| константа | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| константа | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| константа | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| константа | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| константа | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| константа | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| константа | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| константа | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| константа | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| константа | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| константа | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| константа | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| константа | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| константа | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| константа | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| константа | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| константа | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| константа | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| константа | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| константа | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| константа | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| константа | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| константа | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| константа | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| константа | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| константа | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| константа | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| константа | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| константа | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| константа | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| константа | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| константа | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| константа | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| константа | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| константа | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| константа | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| константа | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| константа | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| константа | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| константа | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| константа | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| константа | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| константа | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| константа | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| константа | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| константа | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| константа | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| константа | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| константа | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| константа | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| константа | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| константа | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| константа | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| константа | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| константа | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| константа | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| константа | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| константа | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| константа | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| константа | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| константа | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| константа | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| константа | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| константа | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| константа | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| константа | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| константа | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| константа | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| константа | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| константа | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| константа | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| константа | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| константа | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| константа | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| константа | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| константа | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| константа | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| константа | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| константа | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| константа | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| константа | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| константа | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| константа | `string START_SESSION = "OL_E_START_SESSION"` |
| константа | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| константа | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| константа | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| константа | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| константа | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| константа | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| константа | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| константа | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| константа | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| константа | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| константа | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| константа | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| константа | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| константа | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| константа | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| константа | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| константа | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| константа | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| константа | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| константа | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| константа | `string MODULE_OPERATION_NOT_DECLARED = "OL_E_MODULE_OPERATION_NOT_DECLARED"` |
| константа | `string MODULE_CONTENT_INPUT_UNDECLARED = "OL_E_MODULE_CONTENT_INPUT_UNDECLARED"` |
| константа | `string MODULE_CONTENT_INPUT_INVALID = "OL_E_MODULE_CONTENT_INPUT_INVALID"` |
| константа | `string UI_NOT_PAUSED = "OL_E_UI_NOT_PAUSED"` |
| константа | `string UI_BUSY = "OL_E_UI_BUSY"` |
| константа | `string UI_ALREADY_TAKEN_OVER = "OL_E_UI_ALREADY_TAKEN_OVER"` |
| константа | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| константа | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| константа | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| константа | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| константа | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| константа | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| константа | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| константа | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| константа | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| константа | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| константа | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| константа | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| константа | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| константа | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| константа | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| константа | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| константа | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| константа | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| константа | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| константа | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| константа | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| константа | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| константа | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| константа | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| статическое поле | `IReadOnlyList<PublicErrorDescriptor> All` |
| статический метод | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [errors](errors.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PublicErrorDescriptor(string Code, string Category)` |
| свойство | `string Code { get; init; }` |
| свойство | `string Category { get; init; }` |

### `PublicEvidence`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| константа | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| константа | `string Partial = "PARTIAL"` |
| константа | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| константа | `string Unavailable = "UNAVAILABLE"` |
| статическое поле | `IReadOnlyList<string> All` |

### `PublicExitCode`

Перечисление (`int`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [exit-codes](exit-codes.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Success = 0` |
| значение | `SessionFailed = 1` |
| значение | `InvalidArguments = 2` |
| значение | `UnsupportedProfile = 3` |
| значение | `NoActiveSession = 4` |
| значение | `RuntimeUnavailable = 5` |
| значение | `NotFound = 6` |
| значение | `OperationRejected = 7` |
| значение | `TransactionRecoveryFailed = 8` |
| значение | `InternalError = 10` |

### `PublicMutationPolicy`

Статический класс в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string None = "none"` |
| константа | `string OmsiState = "omsi-state"` |
| константа | `string SessionResource = "session-resource"` |
| константа | `string SessionPolicy = "session-policy"` |
| статическое поле | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| свойство | `string Name { get; init; }` |
| свойство | `bool Required { get; init; }` |
| свойство | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| свойство | `bool Accepted { get; init; }` |
| свойство | `string ErrorCode { get; init; }` |
| свойство | `string Message { get; init; }` |

### `PublicRuntimeOperation`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| свойство | `string Id { get; init; }` |
| свойство | `string NativeCapability { get; init; }` |
| свойство | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| свойство | `IReadOnlyList<string> CliRoutes { get; init; }` |
| свойство | `bool Paged { get; init; }` |
| свойство | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| свойство | `bool Pending { get; init; }` |
| свойство | `bool Recovered { get; init; }` |
| свойство | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [runtime-control](runtime-control.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| свойство | `Guid SessionId { get; init; }` |
| свойство | `ulong RequestId { get; init; }` |
| свойство | `string Operation { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [runtime-control](runtime-control.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| свойство | `Guid SessionId { get; init; }` |
| свойство | `ulong RequestId { get; init; }` |
| свойство | `bool Succeeded { get; init; }` |
| свойство | `string ErrorCode { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

Статический класс в `OmsiLaunch.Api`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `uint Magic = 1330401859` |
| константа | `ushort Version = 1` |
| константа | `int HeaderSize = 72` |
| статический метод | `byte[] SerializeRequest(RuntimeCommand command)` |
| статический метод | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| статический метод | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| статический метод | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| статический метод | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| свойство | `string Type { get; init; }` |
| свойство | `DateTimeOffset TimestampUtc { get; init; }` |
| свойство | `long Sequence { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| свойство | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| свойство | `string Key { get; init; }` |
| свойство | `string Type { get; init; }` |
| свойство | `string Default { get; init; }` |
| свойство | `bool AllowsAuto { get; init; }` |
| свойство | `string Minimum { get; init; }` |
| свойство | `string Maximum { get; init; }` |
| свойство | `IReadOnlyList<string> Choices { get; init; }` |
| свойство | `string Flag { get; init; }` |
| свойство | `string Description { get; init; }` |

### `RuntimeModuleSummary`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| свойство | `string Id { get; init; }` |
| свойство | `string Version { get; init; }` |
| свойство | `string Name { get; init; }` |
| свойство | `string Activation { get; init; }` |
| свойство | `bool IsValid { get; init; }` |
| свойство | `IReadOnlyList<string> Operations { get; init; }` |
| свойство | `string ContentBindingMap { get; init; }` |
| свойство | `string ErrorCode { get; init; }` |
| свойство | `string ErrorMessage { get; init; }` |
| свойство | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| свойство | `IReadOnlyList<string> Enable { get; init; }` |
| свойство | `IReadOnlyList<string> Disable { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| свойство | `IReadOnlyList<string> EffectiveDisable { get; }` |
| свойство | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| свойство | `string OsFamily { get; init; }` |
| свойство | `string OsVersion { get; init; }` |
| свойство | `string OsArchitecture { get; init; }` |
| свойство | `string HostArchitecture { get; init; }` |
| свойство | `string OmsiArchitecture { get; init; }` |
| свойство | `string PluginArchitecture { get; init; }` |
| свойство | `bool CurrentPlatformSupported { get; init; }` |
| свойство | `bool LegacyPlatform { get; init; }` |
| свойство | `bool Wow64Available { get; init; }` |
| свойство | `bool InstallationWritable { get; init; }` |
| свойство | `bool ProcessLaunchSupported { get; init; }` |
| свойство | `bool PluginRuntimeSupported { get; init; }` |
| свойство | `bool NativeInteropSupported { get; init; }` |
| свойство | `bool SharedMemorySupported { get; init; }` |
| свойство | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SemanticDate(int Year, int Month, int Day)` |
| свойство | `int Year { get; init; }` |
| свойство | `int Month { get; init; }` |
| свойство | `int Day { get; init; }` |

### `SemanticTime`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SemanticTime(int Hour, int Minute, int Second)` |
| свойство | `int Hour { get; init; }` |
| свойство | `int Minute { get; init; }` |
| свойство | `int Second { get; init; }` |

### `SessionHandle`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionHandle(Guid SessionId)` |
| свойство | `Guid SessionId { get; init; }` |

### `SessionPlan`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| свойство | `Guid SessionId { get; init; }` |
| свойство | `string BuildProfileId { get; init; }` |
| свойство | `LaunchSpec Spec { get; init; }` |
| свойство | `RuntimePlatformInfo Platform { get; init; }` |
| свойство | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| свойство | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| свойство | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| свойство | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| свойство | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| свойство | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| свойство | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| свойство | `bool IsRunnable { get; init; }` |
| свойство | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| свойство | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| свойство | `SplashMode Splash { get; init; }` |
| свойство | `OptionalValue<string> Language { get; init; }` |
| свойство | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| свойство | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| свойство | `string Id { get; init; }` |
| свойство | `string Name { get; init; }` |
| свойство | `string Version { get; init; }` |
| свойство | `string Author { get; init; }` |
| свойство | `string PresetId { get; init; }` |
| свойство | `int PresetIndex { get; init; }` |
| свойство | `string PresetName { get; init; }` |
| свойство | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| свойство | `int Index { get; init; }` |
| свойство | `string Id { get; init; }` |
| свойство | `string Name { get; init; }` |

### `SessionProfileSummary`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| свойство | `string Id { get; init; }` |
| свойство | `string Name { get; init; }` |
| свойство | `string Version { get; init; }` |
| свойство | `string Author { get; init; }` |
| свойство | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| свойство | `string MapIdentity { get; init; }` |
| свойство | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| свойство | `bool IsValid { get; init; }` |
| свойство | `string ErrorCode { get; init; }` |
| свойство | `string ErrorMessage { get; init; }` |

### `SessionState`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Created = 0` |
| значение | `ValidatingPlatform = 1` |
| значение | `Planning = 2` |
| значение | `AcquiringInstallationLock = 3` |
| значение | `RecoveringPreviousTransaction = 4` |
| значение | `Snapshotting = 5` |
| значение | `ApplyingConfiguration = 6` |
| значение | `DeployingRuntime = 7` |
| значение | `CreatingStartupHandoff = 8` |
| значение | `StartingProcess = 9` |
| значение | `WaitingForPlugin = 10` |
| значение | `PluginBootstrap = 11` |
| значение | `StartingWorld = 12` |
| значение | `EnteringGameplay = 13` |
| значение | `Running = 14` |
| значение | `ProcessExited = 15` |
| значение | `Restoring = 16` |
| значение | `CleaningRuntime = 17` |
| значение | `Completed = 18` |
| значение | `Failed = 19` |

### `SessionStatus`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| свойство | `Guid SessionId { get; init; }` |
| свойство | `SessionState State { get; init; }` |
| свойство | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| свойство | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Unset = 0` |
| значение | `Native = 0` |
| значение | `Managed = 1` |

### `StartDateTime`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| свойство | `int? Year { get; init; }` |
| свойство | `int? DayOfYear { get; init; }` |
| свойство | `int? Hour { get; init; }` |
| свойство | `int? Minute { get; init; }` |
| свойство | `int? Second { get; init; }` |
| свойство | `bool IsEmpty { get; }` |

### `StartWeather`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| свойство | `WeatherMode Mode { get; init; }` |
| свойство | `string Preset { get; init; }` |
| свойство | `string Icao { get; init; }` |

### `StartupHandoff`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| свойство | `Guid SessionId { get; init; }` |
| свойство | `string BuildProfileId { get; init; }` |
| свойство | `WorldMode WorldMode { get; init; }` |
| свойство | `string MapIdentity { get; init; }` |
| свойство | `int PresentedEntrypointIndex { get; init; }` |
| свойство | `bool HeadlessStart { get; init; }` |
| свойство | `bool PlayerVehicleEnabled { get; init; }` |
| свойство | `DateTimeMode DateMode { get; init; }` |
| свойство | `DateTimeMode TimeMode { get; init; }` |
| свойство | `string EntrypointIdentity { get; init; }` |
| свойство | `string SituationIdentity { get; init; }` |
| свойство | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| свойство | `bool PluginTrace { get; init; }` |
| свойство | `bool NativeTrace { get; init; }` |
| свойство | `StartDateTime Start { get; init; }` |
| свойство | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

Статический класс в `OmsiLaunch.Api`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `uint Magic = 1330402120` |
| константа | `ushort Version = 8` |
| константа | `int MaxModules = 64` |
| статический метод | `byte[] Serialize(StartupHandoff value)` |
| статический метод | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "", string SnapshotSha256 = "")` |
| свойство | `string RelativeDirectory { get; init; }` |
| свойство | `string ManifestSha256 { get; init; }` |
| свойство | `string Settings { get; init; }` |
| свойство | `string SnapshotSha256 { get; init; }` |

### `TelemetryRingWire`

Статический класс в `OmsiLaunch.Api`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `uint Magic = 1381256271` |
| константа | `int Version = 1` |
| константа | `int SlotCount = 64` |
| константа | `int SlotSize = 2048` |
| константа | `int HeaderSize = 64` |
| константа | `int Capacity = 131136` |
| константа | `int MaxPayload = 2036` |
| статический метод | `void Initialize(MemoryMappedViewAccessor view)` |
| статический метод | `bool IsRing(MemoryMappedViewAccessor view)` |
| статический метод | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| статический метод | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| свойство | `DateTimeMode Mode { get; init; }` |
| свойство | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

Перечисление (`byte`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `Unset = 0` |
| значение | `Preset = 1` |
| значение | `Icao = 2` |
| значение | `RealCurrent = 3` |

### `WeatherSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| свойство | `WeatherMode Mode { get; init; }` |
| свойство | `OptionalValue<string> Preset { get; init; }` |
| свойство | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

Перечисление (`int`) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| значение | `NewMap = 0` |
| значение | `SavedSituation = 1` |
| значение | `LastMapState = 2` |
| значение | `LastSituation = 2` |

### `WorldSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `STABLE_BETA`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| свойство | `WorldMode Mode { get; init; }` |
| свойство | `OptionalValue<string> MapIdentity { get; init; }` |
| свойство | `OptionalValue<string> SituationIdentity { get; init; }` |
| свойство | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| свойство | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| свойство | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

Запечатанная запись (sealed record) в `OmsiLaunch.Api`. Стабильность: `PARTIAL`. Семантика: [launchspec](launchspec.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| свойство | `DateTimeMode Mode { get; init; }` |
| свойство | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

Запечатанный класс в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `HofPlanningException(string code, string message)` |
| свойство | `string Code { get; }` |

### `LaunchValidation`

Статический класс в `OmsiLaunch.Core`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| статический метод | `bool IsMapIdentity(string value)` |
| статический метод | `bool IsSha256(string value)` |
| статический метод | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

Статический класс в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| статический метод | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

Запечатанная запись (sealed record) в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| свойство | `string PluginBuildDirectory { get; init; }` |
| свойство | `string NativeBridgePath { get; init; }` |
| свойство | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

Запечатанный класс в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| метод | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| метод | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| метод | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| метод | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| метод | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

Запечатанная запись (sealed record) в `OmsiLaunch.Core`. Стабильность: `EXPERIMENTAL`. Семантика: [session-profiles](session-profiles.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| свойство | `string Map { get; init; }` |
| свойство | `int? EntrypointIndex { get; init; }` |
| свойство | `string EntrypointIdentity { get; init; }` |
| свойство | `SemanticDate Date { get; init; }` |
| свойство | `SemanticTime Time { get; init; }` |
| свойство | `int? Year { get; init; }` |
| свойство | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

Запечатанная запись (sealed record) в `OmsiLaunch.Core`. Стабильность: `EXPERIMENTAL`. Семантика: [session-profiles](session-profiles.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| свойство | `int Index { get; init; }` |
| свойство | `string Id { get; init; }` |
| свойство | `string Name { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| свойство | `SessionPresentationSpec Presentation { get; init; }` |
| свойство | `InternetTexturesSpec InternetTextures { get; init; }` |
| свойство | `LaunchBehaviorSpec Behavior { get; init; }` |
| свойство | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| свойство | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

Запечатанный класс в `OmsiLaunch.Core`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionPlanner(IRuntimePlatform platform)` |
| метод | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

Статический класс в `OmsiLaunch.Core`. Стабильность: `EXPERIMENTAL`. Семантика: [session-profiles](session-profiles.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string Schema = "omsilaunch.session-profile/v1"` |
| константа | `long MaxBytes = 262144` |
| константа | `int MinPresetIndex = 1` |
| константа | `int MaxPresetIndex = 99` |
| статическое поле | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| статический метод | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| статический метод | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| статический метод | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| статический метод | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

Запечатанный класс в `OmsiLaunch.Core`. Стабильность: `EXPERIMENTAL`. Семантика: [session-profiles](session-profiles.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionProfileException(string code, string message)` |
| свойство | `string Code { get; }` |

### `SessionProfilePackage`

Запечатанная запись (sealed record) в `OmsiLaunch.Core`. Стабильность: `EXPERIMENTAL`. Семантика: [session-profiles](session-profiles.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| свойство | `string RootPath { get; init; }` |
| свойство | `SessionProfileMetadata Metadata { get; init; }` |
| свойство | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| свойство | `ProfileNew New { get; init; }` |
| свойство | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

Статический класс в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `int MinimumYear = 1900` |
| константа | `int MaximumYear = 2200` |
| статический метод | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

Статический класс в `OmsiLaunch.Core`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| статический метод | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| свойство | `string Name { get; }` |
| свойство | `Guid SessionId { get; }` |
| статический метод | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| метод | `void Dispose()` |
| метод | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| свойство | `string Name { get; }` |
| свойство | `Guid SessionId { get; }` |
| статический метод | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| метод | `void Dispose()` |

### `CurrentTelemetryStore`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| свойство | `string Name { get; }` |
| статический метод | `CurrentTelemetryStore Create(Guid sessionId)` |
| метод | `void Dispose()` |
| метод | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `CurrentWindowsX64Platform()` |
| метод | `RuntimePlatformInfo Detect(string root)` |
| метод | `bool HasExited(LaunchedProcess p)` |
| метод | `bool IsInstallationWritable(string root)` |
| метод | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| метод | `void Terminate(LaunchedProcess p)` |
| метод | `void ValidateCurrent(RuntimePlatformInfo p)` |
| метод | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

Интерфейс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| метод | `OmsiProcessState Observe()` |
| метод | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| метод | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

Интерфейс в `OmsiLaunch.Process`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| метод | `RuntimePlatformInfo Detect(string installationRoot)` |
| метод | `bool HasExited(LaunchedProcess process)` |
| метод | `bool IsInstallationWritable(string installationRoot)` |
| метод | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| метод | `void Terminate(LaunchedProcess process)` |
| метод | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| метод | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| статический метод | `InstallationLease Acquire(string installationRoot)` |
| метод | `void Dispose()` |
| статический метод | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| свойство | `int ProcessId { get; }` |
| свойство | `int ThreadId { get; }` |
| свойство | `ProcessIdentity Identity { get; }` |
| метод | `void Dispose()` |

### `ModuleSessionProcess`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `STABLE_BETA`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `int MaxLineBytes = 65536` |
| свойство | `int ProcessId { get; }` |
| свойство | `bool HasExited { get; }` |
| свойство | `int? ExitCode { get; }` |
| метод | `void Dispose()` |
| метод | `void Kill()` |
| статический метод | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| метод | `bool TryWriteLine(string line)` |
| метод | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

Запечатанная запись (sealed record) в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| свойство | `string State { get; init; }` |
| свойство | `int? ProcessId { get; init; }` |
| свойство | `bool Responding { get; init; }` |

### `ProcessIdentity`

Запечатанная запись (sealed record) в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| свойство | `int ProcessId { get; init; }` |
| свойство | `DateTimeOffset CreationTimeUtc { get; init; }` |
| свойство | `string ExecutablePath { get; init; }` |
| свойство | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

Статический класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| константа | `string FileName = "release-manifest.json"` |
| статический метод | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| статический метод | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

Запечатанная запись (sealed record) в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| свойство | `string SourcePath { get; init; }` |
| свойство | `string DestinationRelativePath { get; init; }` |
| свойство | `string Sha256 { get; init; }` |
| свойство | `long Size { get; init; }` |

### `RuntimeArtifactSet`

Запечатанный класс в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| свойство | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| свойство | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| свойство | `string IntegrityReference { get; }` |
| статический метод | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| метод | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

Запечатанная запись (sealed record) в `OmsiLaunch.Process`. Стабильность: `INTERNAL`. Семантика: [public-api](public-api.md).

| Член | Сигнатура |
| --- | --- |
| конструктор | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| свойство | `string ExecutablePath { get; init; }` |
| свойство | `string WorkingDirectory { get; init; }` |
| свойство | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
