# 公共 API 清单

<!-- l10n: source=reference/public-api-inventory.md source-sha256=03334a6e3cbfb63c191f563ebd823a41ed0b943fad524df95f4754c7d0820ea5 -->
> 本页是 OmsiLaunch 0.1.0-beta4 [英文原始页面](../../../reference/public-api-inventory.md) 的译文。英文页面为规范文本：如有出入，以英文页面和代码为准。

本页由 `tests/OmsiLaunch.DocumentationTests` 根据编译后的程序集生成（`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`）；当本页与代码不再一致时，文档检查会失败。本页列出 `OmsiLaunch.Api`、`OmsiLaunch.Core` 和 `OmsiLaunch.Process` 导出的每个类型及其稳定性，以及每个公共成员的签名。语义、前置条件、错误和示例记录在各标题所指的页面中（默认：[公共 API](public-api.md)）。稳定性术语：`STABLE_BETA`、`EXPERIMENTAL`、`PARTIAL`、`INTERNAL`（出于技术原因而公开，不属于集成接口）、`UNAVAILABLE`（参见[公共 API](public-api.md#stability-vocabulary)）。编译器生成的 record 成员（`Equals`、`GetHashCode`、`ToString`、`Deconstruct`、`<Clone>$`、`EqualityContract`）不予列出。

## `OmsiLaunch.Api`

### `Capability`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `Capability(string Name, bool Available, string EvidenceState, string Reason = null)` |
| 属性 | `string Name { get; init; }` |
| 属性 | `bool Available { get; init; }` |
| 属性 | `string EvidenceState { get; init; }` |
| 属性 | `string Reason { get; init; }` |

### `ContentIdentity`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `ContentIdentity(string Identity, string Kind, string DisplayName = null)` |
| 属性 | `string Identity { get; init; }` |
| 属性 | `string Kind { get; init; }` |
| 属性 | `string DisplayName { get; init; }` |

### `ContentQueryKind`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Maps = 0` |
| 值 | `Situations = 1` |
| 值 | `Vehicles = 2` |
| 值 | `Repaints = 3` |
| 值 | `Hofs = 4` |
| 值 | `FleetNumbers = 5` |
| 值 | `Registrations = 6` |
| 值 | `Addons = 7` |
| 值 | `Entrypoints = 8` |
| 值 | `SessionProfiles = 9` |
| 值 | `RuntimeModules = 10` |

### `D3DDeviceState`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `NotReady = 0` |
| 值 | `Ready = 1` |
| 值 | `Lost = 2` |
| 值 | `Resetting = 3` |
| 值 | `Stopping = 4` |
| 值 | `Stopped = 5` |

### `D3DDeviceStatus`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `D3DDeviceStatus(bool Available, D3DDeviceState State, uint Generation, uint LiveTextureCount, bool ResetHookInstalled, uint ExecutionThreadId, uint LastResetThreadId, int QueryInterfaceHResult, int CooperativeLevelHResult, uint OwnedDeviceReferences)` |
| 属性 | `bool Available { get; init; }` |
| 属性 | `D3DDeviceState State { get; init; }` |
| 属性 | `uint Generation { get; init; }` |
| 属性 | `uint LiveTextureCount { get; init; }` |
| 属性 | `bool ResetHookInstalled { get; init; }` |
| 属性 | `uint ExecutionThreadId { get; init; }` |
| 属性 | `uint LastResetThreadId { get; init; }` |
| 属性 | `int QueryInterfaceHResult { get; init; }` |
| 属性 | `int CooperativeLevelHResult { get; init; }` |
| 属性 | `uint OwnedDeviceReferences { get; init; }` |

### `D3DRuntimeApi`

静态类，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 扩展方法 | `Task<D3DTextureDescription> CreateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, uint width, uint height, D3DTextureFormat format, uint levels = 1, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 扩展方法 | `Task<D3DTextureDescription> DescribeD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, uint level = 0, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 扩展方法 | `Task<D3DDeviceStatus> GetD3DStatusAsync(this IOmsiLaunch launch, SessionHandle session, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 扩展方法 | `Task<D3DTextureDescription> ReleaseD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |
| 扩展方法 | `Task<D3DTextureDescription> UpdateD3DTextureAsync(this IOmsiLaunch launch, SessionHandle session, D3DTextureHandle handle, D3DTextureUpdate update, TimeSpan? timeout = default, CancellationToken cancellationToken = default)` |

### `D3DTextureDescription`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `D3DTextureDescription(D3DTextureHandle Handle, D3DTextureResourceState State, D3DDeviceState DeviceState, uint Generation, uint Width, uint Height, D3DTextureFormat Format, uint Levels, uint Level, uint LevelWidth, uint LevelHeight, int HResult, uint ExecutionThreadId)` |
| 属性 | `D3DTextureHandle Handle { get; init; }` |
| 属性 | `D3DTextureResourceState State { get; init; }` |
| 属性 | `D3DDeviceState DeviceState { get; init; }` |
| 属性 | `uint Generation { get; init; }` |
| 属性 | `uint Width { get; init; }` |
| 属性 | `uint Height { get; init; }` |
| 属性 | `D3DTextureFormat Format { get; init; }` |
| 属性 | `uint Levels { get; init; }` |
| 属性 | `uint Level { get; init; }` |
| 属性 | `uint LevelWidth { get; init; }` |
| 属性 | `uint LevelHeight { get; init; }` |
| 属性 | `int HResult { get; init; }` |
| 属性 | `uint ExecutionThreadId { get; init; }` |

### `D3DTextureFormat`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `A8R8G8B8 = 0` |
| 值 | `X8R8G8B8 = 1` |
| 值 | `R5G6B5 = 2` |
| 值 | `X1R5G5B5 = 3` |
| 值 | `A1R5G5B5 = 4` |
| 值 | `A4R4G4B4 = 5` |
| 值 | `A8 = 6` |
| 值 | `L8 = 7` |
| 值 | `A8L8 = 8` |

### `D3DTextureHandle`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `D3DTextureHandle(string Value)` |
| 属性 | `string Value { get; init; }` |

### `D3DTextureResourceState`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Live = 0` |
| 值 | `Released = 1` |
| 值 | `Stale = 2` |

### `D3DTextureUpdate`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `D3DTextureUpdate(uint Level, uint X, uint Y, uint Width, uint Height, ReadOnlyMemory<byte> Pixels)` |
| 属性 | `uint Level { get; init; }` |
| 属性 | `uint X { get; init; }` |
| 属性 | `uint Y { get; init; }` |
| 属性 | `uint Width { get; init; }` |
| 属性 | `uint Height { get; init; }` |
| 属性 | `ReadOnlyMemory<byte> Pixels { get; init; }` |

### `DateSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value)` |
| 属性 | `DateTimeMode Mode { get; init; }` |
| 属性 | `OptionalValue<SemanticDate> Value { get; init; }` |

### `DateTimeMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Explicit = 1` |
| 值 | `System = 2` |

### `DiagnosticsSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false)` |
| 属性 | `bool Log { get; init; }` |
| 属性 | `bool Verbose { get; init; }` |
| 属性 | `bool OmsiLogAll { get; init; }` |
| 属性 | `bool ProcessTrace { get; init; }` |
| 属性 | `bool PluginTrace { get; init; }` |
| 属性 | `bool NativeTrace { get; init; }` |

### `EntrypointMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `PresentedIndex = 1` |
| 值 | `Identity = 2` |

### `EntrypointSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)` |
| 属性 | `EntrypointMode Mode { get; init; }` |
| 属性 | `OptionalValue<int> PresentedIndex { get; init; }` |
| 属性 | `OptionalValue<string> Identity { get; init; }` |
| 静态属性 | `EntrypointSpec Unset { get; }` |

### `EnvironmentSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `EnvironmentSpec(IReadOnlyDictionary<string, OptionalValue<string>> General, IReadOnlyDictionary<string, OptionalValue<string>> Advanced, IReadOnlyDictionary<string, OptionalValue<string>> Graphics, IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics, IReadOnlyDictionary<string, OptionalValue<string>> Sound, IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers, IReadOnlyDictionary<string, OptionalValue<string>> Keyboard, IReadOnlyDictionary<string, OptionalValue<string>> Controllers)` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> General { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> Advanced { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> Graphics { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> Sound { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> Keyboard { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, OptionalValue<string>> Controllers { get; init; }` |

### `HofApplyMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Add = 0` |
| 值 | `Mirror = 1` |

### `HofManagementSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations)` |
| 属性 | `IReadOnlyList<HofOperationSpec> Operations { get; init; }` |

### `HofOperationSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = Add, IReadOnlyList<string> Files = null)` |
| 属性 | `HofSourceKind SourceKind { get; init; }` |
| 属性 | `string Source { get; init; }` |
| 属性 | `IReadOnlyList<string> TargetVehicles { get; init; }` |
| 属性 | `HofApplyMode Mode { get; init; }` |
| 属性 | `IReadOnlyList<string> Files { get; init; }` |

### `HofSourceKind`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Collection = 0` |
| 值 | `Directory = 1` |
| 值 | `Vehicle = 2` |
| 值 | `File = 3` |

### `IOmsiLaunch`

接口，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 方法 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `InputSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument)` |
| 属性 | `OptionalValue<string> KeyboardDocument { get; init; }` |
| 属性 | `OptionalValue<string> ControllerDocument { get; init; }` |

### `InstallationPaths`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 静态方法 | `string IdentityKey(string root)` |
| 静态方法 | `string NormalizeRoot(string root)` |
| 静态方法 | `IReadOnlyList<string> Segments(string relativePath)` |
| 静态方法 | `bool TryGetContainedRelativePath(string root, string candidate, out string relativePath)` |

### `InstallationSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `InstallationSpec(string RootPath, string ExpectedExecutableSha256 = null)` |
| 属性 | `string RootPath { get; init; }` |
| 属性 | `string ExpectedExecutableSha256 { get; init; }` |

### `InternetTexturesMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Native = 0` |
| 值 | `Disabled = 1` |
| 值 | `Override = 2` |

### `InternetTexturesSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `InternetTexturesSpec(InternetTexturesMode Mode = Native, OptionalValue<string> OverrideProfilePath = default)` |
| 属性 | `InternetTexturesMode Mode { get; init; }` |
| 属性 | `OptionalValue<string> OverrideProfilePath { get; init; }` |

### `LaunchBehaviorSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false)` |
| 属性 | `bool RestoreConfiguration { get; init; }` |
| 属性 | `bool SuppressStaleClosecheckWarning { get; init; }` |
| 属性 | `int StartupTimeoutSeconds { get; init; }` |
| 属性 | `int ShutdownTimeoutSeconds { get; init; }` |
| 属性 | `bool CooperativeShutdown { get; init; }` |

### `LaunchDiagnostic`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string> Data = null)` |
| 属性 | `string Code { get; init; }` |
| 属性 | `string Message { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Data { get; init; }` |

### `LaunchSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec Year = null, WeatherSpec Weather = null, InputSpec Input = null, DiagnosticsSpec Diagnostics = null, SessionPresentationSpec Presentation = null, InternetTexturesSpec InternetTextures = null, SessionProfileMetadata SessionProfile = null, HofManagementSpec HofManagement = null, RuntimeModulesSpec Modules = null)` |
| 属性 | `InstallationSpec Installation { get; init; }` |
| 属性 | `WorldSpec World { get; init; }` |
| 属性 | `DateSpec Date { get; init; }` |
| 属性 | `TimeSpec Time { get; init; }` |
| 属性 | `OptionalValue<PlayerVehicleSpec> PlayerVehicle { get; init; }` |
| 属性 | `EnvironmentSpec Environment { get; init; }` |
| 属性 | `LaunchBehaviorSpec Behavior { get; init; }` |
| 属性 | `YearSpec Year { get; init; }` |
| 属性 | `WeatherSpec Weather { get; init; }` |
| 属性 | `InputSpec Input { get; init; }` |
| 属性 | `DiagnosticsSpec Diagnostics { get; init; }` |
| 属性 | `SessionPresentationSpec Presentation { get; init; }` |
| 属性 | `InternetTexturesSpec InternetTextures { get; init; }` |
| 属性 | `SessionProfileMetadata SessionProfile { get; init; }` |
| 属性 | `HofManagementSpec HofManagement { get; init; }` |
| 属性 | `RuntimeModulesSpec Modules { get; init; }` |
| 属性 | `HofManagementSpec EffectiveHofManagement { get; }` |
| 属性 | `RuntimeModulesSpec EffectiveModules { get; }` |
| 属性 | `YearSpec EffectiveYear { get; }` |
| 属性 | `WeatherSpec EffectiveWeather { get; }` |
| 属性 | `InputSpec EffectiveInput { get; }` |
| 属性 | `DiagnosticsSpec EffectiveDiagnostics { get; }` |
| 属性 | `SessionPresentationSpec EffectivePresentation { get; }` |
| 属性 | `InternetTexturesSpec EffectiveInternetTextures { get; }` |

### `OmsiRuntimeException`

密封类，位于 `OmsiLaunch.Api`。稳定性：`EXPERIMENTAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `OmsiRuntimeException(string code, string detail = null)` |
| 属性 | `string Code { get; }` |

### `OptionalValue`

record struct，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `OptionalValue(Presence Presence, T Value)` |
| 属性 | `Presence Presence { get; init; }` |
| 属性 | `T Value { get; init; }` |
| 属性 | `bool IsSet { get; }` |
| 静态属性 | `OptionalValue<T> Unset { get; }` |
| 静态方法 | `OptionalValue<T> Set(T value)` |

### `PlannedModule`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string Code = null, string Reason = null, string ManifestSha256 = null, string Activation = "requested", IReadOnlyList<PlannedModuleSetting> Settings = null)` |
| 常量 | `string Eligible = "ELIGIBLE"` |
| 常量 | `string Rejected = "REJECTED"` |
| 常量 | `string ActivationRequested = "requested"` |
| 常量 | `string ActivationSettingsFile = "settings-file"` |
| 常量 | `string ActivationContent = "content"` |
| 常量 | `string ActivationDefault = "default"` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Version { get; init; }` |
| 属性 | `string Status { get; init; }` |
| 属性 | `bool Requested { get; init; }` |
| 属性 | `string FailurePolicy { get; init; }` |
| 属性 | `IReadOnlyList<string> Operations { get; init; }` |
| 属性 | `string Code { get; init; }` |
| 属性 | `string Reason { get; init; }` |
| 属性 | `string ManifestSha256 { get; init; }` |
| 属性 | `string Activation { get; init; }` |
| 属性 | `IReadOnlyList<PlannedModuleSetting> Settings { get; init; }` |
| 属性 | `bool IsEligible { get; }` |
| 属性 | `IReadOnlyList<PlannedModuleSetting> EffectiveSettings { get; }` |

### `PlannedModuleSetting`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PlannedModuleSetting(string Key, string Value, string Source)` |
| 属性 | `string Key { get; init; }` |
| 属性 | `string Value { get; init; }` |
| 属性 | `string Source { get; init; }` |

### `PlannedMutation`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation)` |
| 属性 | `string RelativePath { get; init; }` |
| 属性 | `string SemanticKey { get; init; }` |
| 属性 | `string RequestedValue { get; init; }` |
| 属性 | `string Operation { get; init; }` |

### `PlayerVehicleSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)` |
| 属性 | `OptionalValue<string> Model { get; init; }` |
| 属性 | `OptionalValue<string> Repaint { get; init; }` |
| 属性 | `OptionalValue<string> Hof { get; init; }` |
| 属性 | `OptionalValue<string> FleetNumber { get; init; }` |
| 属性 | `OptionalValue<string> Registration { get; init; }` |
| 属性 | `bool Enabled { get; }` |

### `Presence`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Set = 1` |

### `PublicCapabilityClassification`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [capabilities](capabilities.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `PublicStableBeta = 0` |
| 值 | `PublicExperimental = 1` |
| 值 | `InternalOnly = 2` |
| 值 | `Unsupported = 3` |

### `PublicCapabilityDescriptor`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [capabilities](capabilities.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PublicCapabilityDescriptor(string Id, string Family, PublicCapabilityClassification Classification, PublicCapabilityKind Kind, bool RequiresSession, bool RequiresExactProfile, string ApiRoute, string CliRoute, string RuntimeValidation, string Description, IReadOnlyList<string> HandleTypes = null, IReadOnlyList<PublicRuntimeOperation> Operations = null, string NativeCapability = null, string EvidenceNote = null)` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Family { get; init; }` |
| 属性 | `PublicCapabilityClassification Classification { get; init; }` |
| 属性 | `PublicCapabilityKind Kind { get; init; }` |
| 属性 | `bool RequiresSession { get; init; }` |
| 属性 | `bool RequiresExactProfile { get; init; }` |
| 属性 | `string ApiRoute { get; init; }` |
| 属性 | `string CliRoute { get; init; }` |
| 属性 | `string RuntimeValidation { get; init; }` |
| 属性 | `string Description { get; init; }` |
| 属性 | `IReadOnlyList<string> HandleTypes { get; init; }` |
| 属性 | `IReadOnlyList<PublicRuntimeOperation> Operations { get; init; }` |
| 属性 | `string NativeCapability { get; init; }` |
| 属性 | `string EvidenceNote { get; init; }` |
| 属性 | `IReadOnlyList<PublicRuntimeOperation> EffectiveOperations { get; }` |
| 属性 | `IReadOnlyList<string> NativeCapabilities { get; }` |

### `PublicCapabilityKind`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [capabilities](capabilities.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Read = 0` |
| 值 | `Write = 1` |
| 值 | `Action = 2` |
| 值 | `Event = 3` |

### `PublicCapabilityRegistry`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [capabilities](capabilities.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string ProtocolVersion = "0.1"` |
| 静态字段 | `IReadOnlyList<PublicCapabilityDescriptor> All` |
| 静态属性 | `IReadOnlyCollection<string> PublicRuntimeOperationIds { get; }` |
| 静态属性 | `IReadOnlyList<PublicRuntimeOperation> RuntimeOperations { get; }` |
| 静态属性 | `IReadOnlyDictionary<string, string> CliRoutes { get; }` |
| 静态方法 | `PublicCapabilityDescriptor Get(string id)` |
| 静态方法 | `IReadOnlyList<PublicRuntimeArgumentDescriptor> GetRuntimeArguments(string operation)` |
| 静态方法 | `bool IsInternalResultKey(string key)` |
| 静态方法 | `bool IsModuleOperation(string operation)` |
| 静态方法 | `bool IsPublicRuntimeOperation(string operation)` |
| 静态方法 | `string NativeCapabilityOf(string operation)` |
| 静态方法 | `bool TryGet(string id, out PublicCapabilityDescriptor descriptor)` |
| 静态方法 | `bool TryGetOperation(string operation, out PublicCapabilityDescriptor capability, out PublicRuntimeOperation descriptor)` |
| 静态方法 | `PublicRuntimeArgumentValidation ValidateRuntimeArguments(string operation, IReadOnlyDictionary<string, string> arguments)` |

### `PublicErrorCategory`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [errors](errors.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string InvalidArgument = "invalid_argument"` |
| 常量 | `string UnsupportedProfile = "unsupported_profile"` |
| 常量 | `string Session = "session"` |
| 常量 | `string Runtime = "runtime"` |
| 常量 | `string NotFound = "not_found"` |
| 常量 | `string Transaction = "transaction"` |
| 常量 | `string Internal = "internal"` |

### `PublicErrorCodes`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [errors](errors.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string CANCELLED = "OL_E_CANCELLED"` |
| 常量 | `string INTERNAL = "OL_E_INTERNAL"` |
| 常量 | `string TIMEOUT = "OL_E_TIMEOUT"` |
| 常量 | `string WINDOWS_HOST_MISSING = "OL_E_WINDOWS_HOST_MISSING"` |
| 常量 | `string WINDOWS_HOST_START_FAILED = "OL_E_WINDOWS_HOST_START_FAILED"` |
| 常量 | `string BUILD_VALIDATION_FAILED = "OL_E_BUILD_VALIDATION_FAILED"` |
| 常量 | `string CAPABILITY_UNRESOLVED = "OL_E_CAPABILITY_UNRESOLVED"` |
| 常量 | `string UNSUPPORTED_BUILD = "OL_E_UNSUPPORTED_BUILD"` |
| 常量 | `string UNSUPPORTED_OPERATING_SYSTEM = "OL_E_UNSUPPORTED_OPERATING_SYSTEM"` |
| 常量 | `string UNSUPPORTED_OS_ARCHITECTURE = "OL_E_UNSUPPORTED_OS_ARCHITECTURE"` |
| 常量 | `string ENTRYPOINT_NOT_FOUND = "OL_E_ENTRYPOINT_NOT_FOUND"` |
| 常量 | `string ENTRYPOINT_REQUIRED = "OL_E_ENTRYPOINT_REQUIRED"` |
| 常量 | `string ENTRYPOINT_NOT_PRESENTED = "OL_E_ENTRYPOINT_NOT_PRESENTED"` |
| 常量 | `string HOF_NOT_FOUND = "OL_E_HOF_NOT_FOUND"` |
| 常量 | `string HOF_CONFLICT = "OL_E_HOF_CONFLICT"` |
| 常量 | `string HOF_INVALID = "OL_E_HOF_INVALID"` |
| 常量 | `string HOF_PATH_ESCAPE = "OL_E_HOF_PATH_ESCAPE"` |
| 常量 | `string HOF_SOURCE_NOT_FOUND = "OL_E_HOF_SOURCE_NOT_FOUND"` |
| 常量 | `string HOF_TARGET_NOT_FOUND = "OL_E_HOF_TARGET_NOT_FOUND"` |
| 常量 | `string MAP_NOT_FOUND = "OL_E_MAP_NOT_FOUND"` |
| 常量 | `string NOT_FOUND = "OL_E_NOT_FOUND"` |
| 常量 | `string REPAINT_NOT_FOUND = "OL_E_REPAINT_NOT_FOUND"` |
| 常量 | `string SITUATION_MAP_NOT_FOUND = "OL_E_SITUATION_MAP_NOT_FOUND"` |
| 常量 | `string SITUATION_NOT_FOUND = "OL_E_SITUATION_NOT_FOUND"` |
| 常量 | `string VEHICLE_NOT_FOUND = "OL_E_VEHICLE_NOT_FOUND"` |
| 常量 | `string INSTALLATION_BUSY = "OL_E_INSTALLATION_BUSY"` |
| 常量 | `string INSTALLATION_NOT_FOUND = "OL_E_INSTALLATION_NOT_FOUND"` |
| 常量 | `string INSTALLATION_NOT_WRITABLE = "OL_E_INSTALLATION_NOT_WRITABLE"` |
| 常量 | `string PERMANENT_PLUGIN_HASH_MISMATCH = "OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"` |
| 常量 | `string PERMANENT_PLUGIN_MANIFEST_INCOMPLETE = "OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE"` |
| 常量 | `string PERMANENT_PLUGIN_MISSING = "OL_E_PERMANENT_PLUGIN_MISSING"` |
| 常量 | `string PLATFORM_CAPABILITY_MISSING = "OL_E_PLATFORM_CAPABILITY_MISSING"` |
| 常量 | `string RELEASE_MANIFEST_INVALID = "OL_E_RELEASE_MANIFEST_INVALID"` |
| 常量 | `string INVALID_ARGUMENT = "OL_E_INVALID_ARGUMENT"` |
| 常量 | `string INVALID_SETTING_VALUE = "OL_E_INVALID_SETTING_VALUE"` |
| 常量 | `string SETTING_NOT_WRITABLE = "OL_E_SETTING_NOT_WRITABLE"` |
| 常量 | `string UNKNOWN_SETTING = "OL_E_UNKNOWN_SETTING"` |
| 常量 | `string SPEC_INVALID = "OL_E_SPEC_INVALID"` |
| 常量 | `string SPEC_NOT_FOUND = "OL_E_SPEC_NOT_FOUND"` |
| 常量 | `string SPEC_TOO_LARGE = "OL_E_SPEC_TOO_LARGE"` |
| 常量 | `string SPEC_UNKNOWN_PROPERTY = "OL_E_SPEC_UNKNOWN_PROPERTY"` |
| 常量 | `string CONTROL_COMMAND_UNKNOWN = "OL_E_CONTROL_COMMAND_UNKNOWN"` |
| 常量 | `string CONTROL_FAILED = "OL_E_CONTROL_FAILED"` |
| 常量 | `string CONTROL_HANDLER_FAILED = "OL_E_CONTROL_HANDLER_FAILED"` |
| 常量 | `string CONTROL_MESSAGE_INVALID = "OL_E_CONTROL_MESSAGE_INVALID"` |
| 常量 | `string CONTROL_MESSAGE_TOO_LARGE = "OL_E_CONTROL_MESSAGE_TOO_LARGE"` |
| 常量 | `string CONTROL_PROTOCOL = "OL_E_CONTROL_PROTOCOL"` |
| 常量 | `string CONTROL_RESPONSE_TOO_LARGE = "OL_E_CONTROL_RESPONSE_TOO_LARGE"` |
| 常量 | `string CONTROL_SESSION_MISMATCH = "OL_E_CONTROL_SESSION_MISMATCH"` |
| 常量 | `string PLAN_NOT_RUNNABLE = "OL_E_PLAN_NOT_RUNNABLE"` |
| 常量 | `string ITX_PROFILE_INVALID = "OL_E_ITX_PROFILE_INVALID"` |
| 常量 | `string ITX_PROFILE_MISSING = "OL_E_ITX_PROFILE_MISSING"` |
| 常量 | `string ITX_PROFILE_REQUIRED = "OL_E_ITX_PROFILE_REQUIRED"` |
| 常量 | `string ITX_TARGET_OUTSIDE_TEXTURE_PATH = "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH"` |
| 常量 | `string SPLASH_ASSET_DIRECTORY_MISSING = "OL_E_SPLASH_ASSET_DIRECTORY_MISSING"` |
| 常量 | `string SPLASH_ASSET_MISSING = "OL_E_SPLASH_ASSET_MISSING"` |
| 常量 | `string SPLASH_FORMAT_UNSUPPORTED = "OL_E_SPLASH_FORMAT_UNSUPPORTED"` |
| 常量 | `string PROCESS_CLEANUP_FAILED = "OL_E_PROCESS_CLEANUP_FAILED"` |
| 常量 | `string PROCESS_CREATION_TIME_FAILED = "OL_E_PROCESS_CREATION_TIME_FAILED"` |
| 常量 | `string PROCESS_EXITED_EARLY = "OL_E_PROCESS_EXITED_EARLY"` |
| 常量 | `string PROCESS_START_FAILED = "OL_E_PROCESS_START_FAILED"` |
| 常量 | `string PROCESS_SUPERVISION = "OL_E_PROCESS_SUPERVISION"` |
| 常量 | `string PROCESS_TERMINATE_FAILED = "OL_E_PROCESS_TERMINATE_FAILED"` |
| 常量 | `string PROCESS_WAIT_FAILED = "OL_E_PROCESS_WAIT_FAILED"` |
| 常量 | `string CAMERA_PRESET_FAMILY_UNSUPPORTED = "OL_E_CAMERA_PRESET_FAMILY_UNSUPPORTED"` |
| 常量 | `string DATE_TIME_APPLY_FAILED = "OL_E_DATE_TIME_APPLY_FAILED"` |
| 常量 | `string WEATHER_INVALID = "OL_E_WEATHER_INVALID"` |
| 常量 | `string WEATHER_APPLY_FAILED = "OL_E_WEATHER_APPLY_FAILED"` |
| 常量 | `string MAKEVEHICLE_BUS_NOT_FOUND = "OL_E_MAKEVEHICLE_BUS_NOT_FOUND"` |
| 常量 | `string MAKEVEHICLE_DELTA_MULTIPLE = "OL_E_MAKEVEHICLE_DELTA_MULTIPLE"` |
| 常量 | `string MAKEVEHICLE_DELTA_ZERO = "OL_E_MAKEVEHICLE_DELTA_ZERO"` |
| 常量 | `string MAKEVEHICLE_NATIVE_FAILED = "OL_E_MAKEVEHICLE_NATIVE_FAILED"` |
| 常量 | `string PLACE_RANDOM_BUS_FAILED = "OL_E_PLACE_RANDOM_BUS_FAILED"` |
| 常量 | `string RUNTIME_ARGUMENT_REQUIRED = "OL_E_RUNTIME_ARGUMENT_REQUIRED"` |
| 常量 | `string RUNTIME_ARTIFACT_MISSING = "OL_E_RUNTIME_ARTIFACT_MISSING"` |
| 常量 | `string RUNTIME_BASELINE_UNAVAILABLE = "OL_E_RUNTIME_BASELINE_UNAVAILABLE"` |
| 常量 | `string RUNTIME_BUS_IDENTITY_INVALID = "OL_E_RUNTIME_BUS_IDENTITY_INVALID"` |
| 常量 | `string RUNTIME_CHANNEL_BUSY = "OL_E_RUNTIME_CHANNEL_BUSY"` |
| 常量 | `string RUNTIME_CHANNEL_CLOSED = "OL_E_RUNTIME_CHANNEL_CLOSED"` |
| 常量 | `string RUNTIME_CHANNEL_STATE_INVALID = "OL_E_RUNTIME_CHANNEL_STATE_INVALID"` |
| 常量 | `string RUNTIME_CONSTANTS_UNAVAILABLE = "OL_E_RUNTIME_CONSTANTS_UNAVAILABLE"` |
| 常量 | `string RUNTIME_CONSTANT_NOT_FOUND = "OL_E_RUNTIME_CONSTANT_NOT_FOUND"` |
| 常量 | `string RUNTIME_CREATED_OBJECT_INVALID = "OL_E_RUNTIME_CREATED_OBJECT_INVALID"` |
| 常量 | `string RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION = "OL_E_RUNTIME_CREATED_OBJECT_NOT_IN_COLLECTION"` |
| 常量 | `string RUNTIME_CURVE_DEGENERATE = "OL_E_RUNTIME_CURVE_DEGENERATE"` |
| 常量 | `string RUNTIME_CURVE_EMPTY = "OL_E_RUNTIME_CURVE_EMPTY"` |
| 常量 | `string RUNTIME_CURVE_INVALID = "OL_E_RUNTIME_CURVE_INVALID"` |
| 常量 | `string RUNTIME_CURVE_NOT_FOUND = "OL_E_RUNTIME_CURVE_NOT_FOUND"` |
| 常量 | `string RUNTIME_HOF_UNAVAILABLE = "OL_E_RUNTIME_HOF_UNAVAILABLE"` |
| 常量 | `string RUNTIME_INSTALLATION_INCOMPLETE = "OL_E_RUNTIME_INSTALLATION_INCOMPLETE"` |
| 常量 | `string RUNTIME_OBJECT_HANDLE_REQUIRED = "OL_E_RUNTIME_OBJECT_HANDLE_REQUIRED"` |
| 常量 | `string RUNTIME_OBJECT_HANDLE_STALE = "OL_E_RUNTIME_OBJECT_HANDLE_STALE"` |
| 常量 | `string RUNTIME_OPERATION_FAILED = "OL_E_RUNTIME_OPERATION_FAILED"` |
| 常量 | `string RUNTIME_OPERATION_UNAVAILABLE = "OL_E_RUNTIME_OPERATION_UNAVAILABLE"` |
| 常量 | `string RUNTIME_OPERATION_UNKNOWN = "OL_E_RUNTIME_OPERATION_UNKNOWN"` |
| 常量 | `string RUNTIME_PLAYER_VEHICLE_UNAVAILABLE = "OL_E_RUNTIME_PLAYER_VEHICLE_UNAVAILABLE"` |
| 常量 | `string RUNTIME_PROTOCOL_MISMATCH = "OL_E_RUNTIME_PROTOCOL_MISMATCH"` |
| 常量 | `string RUNTIME_REQUEST_ID_REUSED = "OL_E_RUNTIME_REQUEST_ID_REUSED"` |
| 常量 | `string RUNTIME_REQUEST_TIMEOUT = "OL_E_RUNTIME_REQUEST_TIMEOUT"` |
| 常量 | `string RUNTIME_RESPONSE_INVALID = "OL_E_RUNTIME_RESPONSE_INVALID"` |
| 常量 | `string RUNTIME_RESPONSE_TOO_LARGE = "OL_E_RUNTIME_RESPONSE_TOO_LARGE"` |
| 常量 | `string RUNTIME_SCRIPT_OBJECT_UNAVAILABLE = "OL_E_RUNTIME_SCRIPT_OBJECT_UNAVAILABLE"` |
| 常量 | `string RUNTIME_SESSION_MISMATCH = "OL_E_RUNTIME_SESSION_MISMATCH"` |
| 常量 | `string RUNTIME_SETTING_NOT_PERSISTENT = "OL_E_RUNTIME_SETTING_NOT_PERSISTENT"` |
| 常量 | `string RUNTIME_SETTING_UNAVAILABLE = "OL_E_RUNTIME_SETTING_UNAVAILABLE"` |
| 常量 | `string RUNTIME_STRING_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_STRING_VARIABLE_NOT_FOUND"` |
| 常量 | `string RUNTIME_VALUE_INVALID = "OL_E_RUNTIME_VALUE_INVALID"` |
| 常量 | `string RUNTIME_VALUE_OUT_OF_RANGE = "OL_E_RUNTIME_VALUE_OUT_OF_RANGE"` |
| 常量 | `string RUNTIME_CURSOR_INVALID = "OL_E_RUNTIME_CURSOR_INVALID"` |
| 常量 | `string RUNTIME_VARIABLE_NOT_FOUND = "OL_E_RUNTIME_VARIABLE_NOT_FOUND"` |
| 常量 | `string RUNTIME_VARIABLE_UNAVAILABLE = "OL_E_RUNTIME_VARIABLE_UNAVAILABLE"` |
| 常量 | `string TIME_APPLY_FAILED = "OL_E_TIME_APPLY_FAILED"` |
| 常量 | `string D3D_DEVICE_LOST = "OL_E_D3D_DEVICE_LOST"` |
| 常量 | `string D3D_INVALID_ARGUMENT = "OL_E_D3D_INVALID_ARGUMENT"` |
| 常量 | `string D3D_INVALID_PIXEL_BUFFER = "OL_E_D3D_INVALID_PIXEL_BUFFER"` |
| 常量 | `string D3D_INVALID_TEXTURE_FORMAT = "OL_E_D3D_INVALID_TEXTURE_FORMAT"` |
| 常量 | `string D3D_NATIVE_CALL_FAILED = "OL_E_D3D_NATIVE_CALL_FAILED"` |
| 常量 | `string D3D_NOT_READY = "OL_E_D3D_NOT_READY"` |
| 常量 | `string D3D_RESET_IN_PROGRESS = "OL_E_D3D_RESET_IN_PROGRESS"` |
| 常量 | `string D3D_RESOURCE_RELEASED = "OL_E_D3D_RESOURCE_RELEASED"` |
| 常量 | `string D3D_STALE_RESOURCE_HANDLE = "OL_E_D3D_STALE_RESOURCE_HANDLE"` |
| 常量 | `string CAPABILITY_UNAVAILABLE = "OL_E_CAPABILITY_UNAVAILABLE"` |
| 常量 | `string HEADLESS_ARM_FAILED = "OL_E_HEADLESS_ARM_FAILED"` |
| 常量 | `string NO_ACTIVE_SESSION = "OL_E_NO_ACTIVE_SESSION"` |
| 常量 | `string PLUGIN_NOT_LOADED = "OL_E_PLUGIN_NOT_LOADED"` |
| 常量 | `string PLUGIN_PROTOCOL_MISMATCH = "OL_E_PLUGIN_PROTOCOL_MISMATCH"` |
| 常量 | `string SESSION_ALREADY_ACTIVE = "OL_E_SESSION_ALREADY_ACTIVE"` |
| 常量 | `string SESSION_NOT_RUNNING = "OL_E_SESSION_NOT_RUNNING"` |
| 常量 | `string SESSION_PRESENTATION_INVALID = "OL_E_SESSION_PRESENTATION_INVALID"` |
| 常量 | `string SESSION_START_FAILED = "OL_E_SESSION_START_FAILED"` |
| 常量 | `string SITUATION_LOAD_FAILED = "OL_E_SITUATION_LOAD_FAILED"` |
| 常量 | `string STARTUP_TIMEOUT = "OL_E_STARTUP_TIMEOUT"` |
| 常量 | `string START_SESSION = "OL_E_START_SESSION"` |
| 常量 | `string WORLD_START_FAILED = "OL_E_WORLD_START_FAILED"` |
| 常量 | `string MODULE_MANIFEST_INVALID = "OL_E_MODULE_MANIFEST_INVALID"` |
| 常量 | `string MODULE_NOT_FOUND = "OL_E_MODULE_NOT_FOUND"` |
| 常量 | `string MODULE_INTEGRITY = "OL_E_MODULE_INTEGRITY"` |
| 常量 | `string MODULE_PROTOCOL_UNSUPPORTED = "OL_E_MODULE_PROTOCOL_UNSUPPORTED"` |
| 常量 | `string MODULE_CAPABILITY_UNRESOLVED = "OL_E_MODULE_CAPABILITY_UNRESOLVED"` |
| 常量 | `string MODULE_CONTENT_BINDING = "OL_E_MODULE_CONTENT_BINDING"` |
| 常量 | `string MODULE_COMPONENT_UNSUPPORTED = "OL_E_MODULE_COMPONENT_UNSUPPORTED"` |
| 常量 | `string MODULE_LOAD_FAILED = "OL_E_MODULE_LOAD_FAILED"` |
| 常量 | `string MODULE_PHASE_FAILED = "OL_E_MODULE_PHASE_FAILED"` |
| 常量 | `string MODULE_PHASE_TIMEOUT = "OL_E_MODULE_PHASE_TIMEOUT"` |
| 常量 | `string MODULE_DISABLED = "OL_E_MODULE_DISABLED"` |
| 常量 | `string MODULE_NOT_LOADED = "OL_E_MODULE_NOT_LOADED"` |
| 常量 | `string MODULE_OPERATION_UNKNOWN = "OL_E_MODULE_OPERATION_UNKNOWN"` |
| 常量 | `string MODULE_OPERATION_FAILED = "OL_E_MODULE_OPERATION_FAILED"` |
| 常量 | `string MODULE_CLEANUP_INCOMPLETE = "OL_E_MODULE_CLEANUP_INCOMPLETE"` |
| 常量 | `string MODULE_SESSION_FAILED = "OL_E_MODULE_SESSION_FAILED"` |
| 常量 | `string MODULE_HOOK_UNAVAILABLE = "OL_E_MODULE_HOOK_UNAVAILABLE"` |
| 常量 | `string MODULE_SETTING_INVALID = "OL_E_MODULE_SETTING_INVALID"` |
| 常量 | `string MODULE_CONFLICT = "OL_E_MODULE_CONFLICT"` |
| 常量 | `string SESSION_PROFILE_ASSET_MISSING = "OL_E_SESSION_PROFILE_ASSET_MISSING"` |
| 常量 | `string SESSION_PROFILE_INVALID = "OL_E_SESSION_PROFILE_INVALID"` |
| 常量 | `string SESSION_PROFILE_MAP_MISMATCH = "OL_E_SESSION_PROFILE_MAP_MISMATCH"` |
| 常量 | `string SESSION_PROFILE_NOT_FOUND = "OL_E_SESSION_PROFILE_NOT_FOUND"` |
| 常量 | `string SESSION_PROFILE_OVERRIDE_CONFLICT = "OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT"` |
| 常量 | `string SESSION_PROFILE_PATH_ESCAPE = "OL_E_SESSION_PROFILE_PATH_ESCAPE"` |
| 常量 | `string SESSION_PROFILE_PRESET_NOT_FOUND = "OL_E_SESSION_PROFILE_PRESET_NOT_FOUND"` |
| 常量 | `string SESSION_PROFILE_SCHEMA_UNSUPPORTED = "OL_E_SESSION_PROFILE_SCHEMA_UNSUPPORTED"` |
| 常量 | `string SESSION_PROFILE_SETTING_NOT_WRITABLE = "OL_E_SESSION_PROFILE_SETTING_NOT_WRITABLE"` |
| 常量 | `string SESSION_PROFILE_SETTING_UNKNOWN = "OL_E_SESSION_PROFILE_SETTING_UNKNOWN"` |
| 常量 | `string CLOSECHECK_REMOVE_FAILED = "OL_E_CLOSECHECK_REMOVE_FAILED"` |
| 常量 | `string RECOVERY_ABSENT_OWNERSHIP_MISMATCH = "OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH"` |
| 常量 | `string RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED = "OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED"` |
| 常量 | `string RECOVERY_BACKUP_CORRUPT = "OL_E_RECOVERY_BACKUP_CORRUPT"` |
| 常量 | `string RECOVERY_JOURNAL_MISSING = "OL_E_RECOVERY_JOURNAL_MISSING"` |
| 常量 | `string RECOVERY_JOURNAL_REMOVE_FAILED = "OL_E_RECOVERY_JOURNAL_REMOVE_FAILED"` |
| 常量 | `string RESTORE_DEFERRED = "OL_E_RESTORE_DEFERRED"` |
| 常量 | `string RESTORE_FAILED = "OL_E_RESTORE_FAILED"` |
| 常量 | `string RESTORE_FOREIGN_FILE_RETAINED = "OL_W_RESTORE_FOREIGN_FILE_RETAINED"` |
| 常量 | `string BUILD_UNRECOGNIZED = "OL_W_BUILD_UNRECOGNIZED"` |
| 常量 | `string COOPERATIVE_SHUTDOWN_UNAVAILABLE = "OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE"` |
| 常量 | `string CAPABILITY_PENDING = "OL_W_CAPABILITY_PENDING"` |
| 常量 | `string MODULE_NOT_LOADED_WARNING = "OL_W_MODULE_NOT_LOADED"` |
| 常量 | `string MODULE_SETTING_IGNORED = "OL_W_MODULE_SETTING_IGNORED"` |
| 静态字段 | `IReadOnlyList<PublicErrorDescriptor> All` |
| 静态方法 | `string FromMessage(string message)` |

### `PublicErrorDescriptor`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [errors](errors.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PublicErrorDescriptor(string Code, string Category)` |
| 属性 | `string Code { get; init; }` |
| 属性 | `string Category { get; init; }` |

### `PublicEvidence`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string RuntimeValidated = "RUNTIME_VALIDATED"` |
| 常量 | `string StaticallyValidated = "STATICALLY_VALIDATED"` |
| 常量 | `string Partial = "PARTIAL"` |
| 常量 | `string RuntimeValidationRequired = "RUNTIME_VALIDATION_REQUIRED"` |
| 常量 | `string Unavailable = "UNAVAILABLE"` |
| 静态字段 | `IReadOnlyList<string> All` |

### `PublicExitCode`

枚举 (`int`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [exit-codes](exit-codes.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Success = 0` |
| 值 | `SessionFailed = 1` |
| 值 | `InvalidArguments = 2` |
| 值 | `UnsupportedProfile = 3` |
| 值 | `NoActiveSession = 4` |
| 值 | `RuntimeUnavailable = 5` |
| 值 | `NotFound = 6` |
| 值 | `OperationRejected = 7` |
| 值 | `TransactionRecoveryFailed = 8` |
| 值 | `InternalError = 10` |

### `PublicMutationPolicy`

静态类，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string None = "none"` |
| 常量 | `string OmsiState = "omsi-state"` |
| 常量 | `string SessionResource = "session-resource"` |
| 常量 | `string SessionPolicy = "session-policy"` |
| 静态字段 | `IReadOnlyList<string> All` |

### `PublicRuntimeArgumentDescriptor`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PublicRuntimeArgumentDescriptor(string Name, bool Required, string Description)` |
| 属性 | `string Name { get; init; }` |
| 属性 | `bool Required { get; init; }` |
| 属性 | `string Description { get; init; }` |

### `PublicRuntimeArgumentValidation`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PublicRuntimeArgumentValidation(bool Accepted, string ErrorCode = null, string Message = null)` |
| 属性 | `bool Accepted { get; init; }` |
| 属性 | `string ErrorCode { get; init; }` |
| 属性 | `string Message { get; init; }` |

### `PublicRuntimeOperation`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `PublicRuntimeOperation(string Id, string NativeCapability, IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments, IReadOnlyList<string> CliRoutes, bool Paged = false, string MutationPolicy = "none")` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string NativeCapability { get; init; }` |
| 属性 | `IReadOnlyList<PublicRuntimeArgumentDescriptor> Arguments { get; init; }` |
| 属性 | `IReadOnlyList<string> CliRoutes { get; init; }` |
| 属性 | `bool Paged { get; init; }` |
| 属性 | `string MutationPolicy { get; init; }` |

### `RecoveryStatus`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics)` |
| 属性 | `bool Pending { get; init; }` |
| 属性 | `bool Recovered { get; init; }` |
| 属性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |

### `RuntimeCommand`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [runtime-control](runtime-control.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeCommand(Guid SessionId, ulong RequestId, string Operation, IReadOnlyDictionary<string, string> Arguments = null)` |
| 属性 | `Guid SessionId { get; init; }` |
| 属性 | `ulong RequestId { get; init; }` |
| 属性 | `string Operation { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Arguments { get; init; }` |

### `RuntimeCommandResult`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [runtime-control](runtime-control.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeCommandResult(Guid SessionId, ulong RequestId, bool Succeeded, string ErrorCode = null, IReadOnlyDictionary<string, string> Values = null)` |
| 属性 | `Guid SessionId { get; init; }` |
| 属性 | `ulong RequestId { get; init; }` |
| 属性 | `bool Succeeded { get; init; }` |
| 属性 | `string ErrorCode { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Values { get; init; }` |

### `RuntimeCommandWire`

静态类，位于 `OmsiLaunch.Api`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `uint Magic = 1330401859` |
| 常量 | `ushort Version = 1` |
| 常量 | `int HeaderSize = 72` |
| 静态方法 | `byte[] SerializeRequest(RuntimeCommand command)` |
| 静态方法 | `byte[] SerializeResponse(RuntimeCommandResult result)` |
| 静态方法 | `bool TryDeserializeRequest(ReadOnlySpan<byte> bytes, out RuntimeCommand command)` |
| 静态方法 | `bool TryDeserializeResponse(ReadOnlySpan<byte> bytes, out RuntimeCommandResult result)` |
| 静态方法 | `bool TryReadRequestId(ReadOnlySpan<byte> bytes, out ulong requestId)` |

### `RuntimeEvent`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = default)` |
| 属性 | `string Type { get; init; }` |
| 属性 | `DateTimeOffset TimestampUtc { get; init; }` |
| 属性 | `long Sequence { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Data { get; init; }` |
| 属性 | `long? SessionTickMilliseconds { get; init; }` |

### `RuntimeModuleSettingSummary`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string Minimum = null, string Maximum = null, IReadOnlyList<string> Choices = null, string Flag = null, string Description = null)` |
| 属性 | `string Key { get; init; }` |
| 属性 | `string Type { get; init; }` |
| 属性 | `string Default { get; init; }` |
| 属性 | `bool AllowsAuto { get; init; }` |
| 属性 | `string Minimum { get; init; }` |
| 属性 | `string Maximum { get; init; }` |
| 属性 | `IReadOnlyList<string> Choices { get; init; }` |
| 属性 | `string Flag { get; init; }` |
| 属性 | `string Description { get; init; }` |

### `RuntimeModuleSummary`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeModuleSummary(string Id, string Version, string Name, string Activation, bool IsValid, IReadOnlyList<string> Operations, string ContentBindingMap = null, string ErrorCode = null, string ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary> Settings = null)` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Version { get; init; }` |
| 属性 | `string Name { get; init; }` |
| 属性 | `string Activation { get; init; }` |
| 属性 | `bool IsValid { get; init; }` |
| 属性 | `IReadOnlyList<string> Operations { get; init; }` |
| 属性 | `string ContentBindingMap { get; init; }` |
| 属性 | `string ErrorCode { get; init; }` |
| 属性 | `string ErrorMessage { get; init; }` |
| 属性 | `IReadOnlyList<RuntimeModuleSettingSummary> Settings { get; init; }` |

### `RuntimeModulesSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string> Disable = null, IReadOnlyDictionary<string, string> Settings = null)` |
| 属性 | `IReadOnlyList<string> Enable { get; init; }` |
| 属性 | `IReadOnlyList<string> Disable { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| 属性 | `IReadOnlyList<string> EffectiveDisable { get; }` |
| 属性 | `IReadOnlyDictionary<string, string> EffectiveSettings { get; }` |

### `RuntimePlatformInfo`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported)` |
| 属性 | `string OsFamily { get; init; }` |
| 属性 | `string OsVersion { get; init; }` |
| 属性 | `string OsArchitecture { get; init; }` |
| 属性 | `string HostArchitecture { get; init; }` |
| 属性 | `string OmsiArchitecture { get; init; }` |
| 属性 | `string PluginArchitecture { get; init; }` |
| 属性 | `bool CurrentPlatformSupported { get; init; }` |
| 属性 | `bool LegacyPlatform { get; init; }` |
| 属性 | `bool Wow64Available { get; init; }` |
| 属性 | `bool InstallationWritable { get; init; }` |
| 属性 | `bool ProcessLaunchSupported { get; init; }` |
| 属性 | `bool PluginRuntimeSupported { get; init; }` |
| 属性 | `bool NativeInteropSupported { get; init; }` |
| 属性 | `bool SharedMemorySupported { get; init; }` |
| 属性 | `bool ExactRestoreSupported { get; init; }` |

### `SemanticDate`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SemanticDate(int Year, int Month, int Day)` |
| 属性 | `int Year { get; init; }` |
| 属性 | `int Month { get; init; }` |
| 属性 | `int Day { get; init; }` |

### `SemanticTime`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SemanticTime(int Hour, int Minute, int Second)` |
| 属性 | `int Hour { get; init; }` |
| 属性 | `int Minute { get; init; }` |
| 属性 | `int Second { get; init; }` |

### `SessionHandle`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionHandle(Guid SessionId)` |
| 属性 | `Guid SessionId { get; init; }` |

### `SessionPlan`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule> PlannedModules = null)` |
| 属性 | `Guid SessionId { get; init; }` |
| 属性 | `string BuildProfileId { get; init; }` |
| 属性 | `LaunchSpec Spec { get; init; }` |
| 属性 | `RuntimePlatformInfo Platform { get; init; }` |
| 属性 | `IReadOnlyList<ContentIdentity> ResolvedContent { get; init; }` |
| 属性 | `IReadOnlyList<string> TouchedFiles { get; init; }` |
| 属性 | `IReadOnlyList<string> RuntimeArtifacts { get; init; }` |
| 属性 | `IReadOnlyList<Capability> RequiredCapabilities { get; init; }` |
| 属性 | `IReadOnlyList<Capability> UnsupportedRequestedFeatures { get; init; }` |
| 属性 | `IReadOnlyList<PlannedMutation> PlannedMutations { get; init; }` |
| 属性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| 属性 | `bool IsRunnable { get; init; }` |
| 属性 | `IReadOnlyList<PlannedModule> PlannedModules { get; init; }` |
| 属性 | `IReadOnlyList<PlannedModule> EffectivePlannedModules { get; }` |

### `SessionPresentationSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionPresentationSpec(SplashMode Splash = Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false)` |
| 属性 | `SplashMode Splash { get; init; }` |
| 属性 | `OptionalValue<string> Language { get; init; }` |
| 属性 | `OptionalValue<string> CustomAssetDirectory { get; init; }` |
| 属性 | `bool SuppressTrayIcon { get; init; }` |

### `SessionProfileMetadata`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath)` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Name { get; init; }` |
| 属性 | `string Version { get; init; }` |
| 属性 | `string Author { get; init; }` |
| 属性 | `string PresetId { get; init; }` |
| 属性 | `int PresetIndex { get; init; }` |
| 属性 | `string PresetName { get; init; }` |
| 属性 | `string PackagePath { get; init; }` |

### `SessionProfilePresetSummary`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionProfilePresetSummary(int Index, string Id, string Name)` |
| 属性 | `int Index { get; init; }` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Name { get; init; }` |

### `SessionProfileSummary`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionProfileSummary(string Id, string Name, string Version, string Author, IReadOnlyList<string> CompatibleMaps, string MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string ErrorCode, string ErrorMessage)` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Name { get; init; }` |
| 属性 | `string Version { get; init; }` |
| 属性 | `string Author { get; init; }` |
| 属性 | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| 属性 | `string MapIdentity { get; init; }` |
| 属性 | `IReadOnlyList<SessionProfilePresetSummary> Presets { get; init; }` |
| 属性 | `bool IsValid { get; init; }` |
| 属性 | `string ErrorCode { get; init; }` |
| 属性 | `string ErrorMessage { get; init; }` |

### `SessionState`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Created = 0` |
| 值 | `ValidatingPlatform = 1` |
| 值 | `Planning = 2` |
| 值 | `AcquiringInstallationLock = 3` |
| 值 | `RecoveringPreviousTransaction = 4` |
| 值 | `Snapshotting = 5` |
| 值 | `ApplyingConfiguration = 6` |
| 值 | `DeployingRuntime = 7` |
| 值 | `CreatingStartupHandoff = 8` |
| 值 | `StartingProcess = 9` |
| 值 | `WaitingForPlugin = 10` |
| 值 | `PluginBootstrap = 11` |
| 值 | `StartingWorld = 12` |
| 值 | `EnteringGameplay = 13` |
| 值 | `Running = 14` |
| 值 | `ProcessExited = 15` |
| 值 | `Restoring = 16` |
| 值 | `CleaningRuntime = 17` |
| 值 | `Completed = 18` |
| 值 | `Failed = 19` |

### `SessionStatus`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent> RuntimeEvents = null)` |
| 属性 | `Guid SessionId { get; init; }` |
| 属性 | `SessionState State { get; init; }` |
| 属性 | `IReadOnlyList<LaunchDiagnostic> Diagnostics { get; init; }` |
| 属性 | `IReadOnlyList<RuntimeEvent> RuntimeEvents { get; init; }` |

### `SplashMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Native = 0` |
| 值 | `Managed = 1` |

### `StartDateTime`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `StartDateTime(int? Year, int? DayOfYear, int? Hour, int? Minute, int? Second)` |
| 属性 | `int? Year { get; init; }` |
| 属性 | `int? DayOfYear { get; init; }` |
| 属性 | `int? Hour { get; init; }` |
| 属性 | `int? Minute { get; init; }` |
| 属性 | `int? Second { get; init; }` |
| 属性 | `bool IsEmpty { get; }` |

### `StartWeather`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `StartWeather(WeatherMode Mode, string Preset = "", string Icao = "")` |
| 属性 | `WeatherMode Mode { get; init; }` |
| 属性 | `string Preset { get; init; }` |
| 属性 | `string Icao { get; init; }` |

### `StartupHandoff`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `StartupHandoff(Guid SessionId, string BuildProfileId, WorldMode WorldMode, string MapIdentity, int PresentedEntrypointIndex, bool HeadlessStart, bool PlayerVehicleEnabled, DateTimeMode DateMode, DateTimeMode TimeMode, string EntrypointIdentity = "", string SituationIdentity = "", IReadOnlyList<StartupModule> Modules = null, bool PluginTrace = false, bool NativeTrace = false, StartDateTime Start = null, StartWeather Weather = null)` |
| 属性 | `Guid SessionId { get; init; }` |
| 属性 | `string BuildProfileId { get; init; }` |
| 属性 | `WorldMode WorldMode { get; init; }` |
| 属性 | `string MapIdentity { get; init; }` |
| 属性 | `int PresentedEntrypointIndex { get; init; }` |
| 属性 | `bool HeadlessStart { get; init; }` |
| 属性 | `bool PlayerVehicleEnabled { get; init; }` |
| 属性 | `DateTimeMode DateMode { get; init; }` |
| 属性 | `DateTimeMode TimeMode { get; init; }` |
| 属性 | `string EntrypointIdentity { get; init; }` |
| 属性 | `string SituationIdentity { get; init; }` |
| 属性 | `IReadOnlyList<StartupModule> Modules { get; init; }` |
| 属性 | `bool PluginTrace { get; init; }` |
| 属性 | `bool NativeTrace { get; init; }` |
| 属性 | `StartDateTime Start { get; init; }` |
| 属性 | `StartWeather Weather { get; init; }` |

### `StartupHandoffWire`

静态类，位于 `OmsiLaunch.Api`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `uint Magic = 1330402120` |
| 常量 | `ushort Version = 7` |
| 常量 | `int MaxModules = 64` |
| 静态方法 | `byte[] Serialize(StartupHandoff value)` |
| 静态方法 | `bool TryDeserialize(ReadOnlySpan<byte> bytes, out StartupHandoff value)` |

### `StartupModule`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `StartupModule(string RelativeDirectory, string ManifestSha256, string Settings = "")` |
| 属性 | `string RelativeDirectory { get; init; }` |
| 属性 | `string ManifestSha256 { get; init; }` |
| 属性 | `string Settings { get; init; }` |

### `TelemetryRingWire`

静态类，位于 `OmsiLaunch.Api`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `uint Magic = 1381256271` |
| 常量 | `int Version = 1` |
| 常量 | `int SlotCount = 64` |
| 常量 | `int SlotSize = 2048` |
| 常量 | `int HeaderSize = 64` |
| 常量 | `int Capacity = 131136` |
| 常量 | `int MaxPayload = 2036` |
| 静态方法 | `void Initialize(MemoryMappedViewAccessor view)` |
| 静态方法 | `bool IsRing(MemoryMappedViewAccessor view)` |
| 静态方法 | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long, long> ReadSince(MemoryMappedViewAccessor view, long after)` |
| 静态方法 | `bool Write(MemoryMappedViewAccessor view, byte[] payload)` |

### `TimeSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value)` |
| 属性 | `DateTimeMode Mode { get; init; }` |
| 属性 | `OptionalValue<SemanticTime> Value { get; init; }` |

### `WeatherMode`

枚举 (`byte`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `Unset = 0` |
| 值 | `Preset = 1` |
| 值 | `Icao = 2` |
| 值 | `RealCurrent = 3` |

### `WeatherSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao)` |
| 属性 | `WeatherMode Mode { get; init; }` |
| 属性 | `OptionalValue<string> Preset { get; init; }` |
| 属性 | `OptionalValue<string> Icao { get; init; }` |

### `WorldMode`

枚举 (`int`)，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 值 | `NewMap = 0` |
| 值 | `SavedSituation = 1` |
| 值 | `LastMapState = 2` |
| 值 | `LastSituation = 2` |

### `WorldSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`STABLE_BETA`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)` |
| 属性 | `WorldMode Mode { get; init; }` |
| 属性 | `OptionalValue<string> MapIdentity { get; init; }` |
| 属性 | `OptionalValue<string> SituationIdentity { get; init; }` |
| 属性 | `OptionalValue<int> PresentedEntrypointIndex { get; init; }` |
| 属性 | `OptionalValue<string> EntrypointIdentity { get; init; }` |
| 属性 | `EntrypointSpec Entrypoint { get; }` |

### `YearSpec`

密封 record，位于 `OmsiLaunch.Api`。稳定性：`PARTIAL`。语义： [launchspec](launchspec.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `YearSpec(DateTimeMode Mode, OptionalValue<int> Value)` |
| 属性 | `DateTimeMode Mode { get; init; }` |
| 属性 | `OptionalValue<int> Value { get; init; }` |

## `OmsiLaunch.Core`

### `HofPlanningException`

密封类，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `HofPlanningException(string code, string message)` |
| 属性 | `string Code { get; }` |

### `LaunchValidation`

静态类，位于 `OmsiLaunch.Core`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 静态方法 | `bool IsMapIdentity(string value)` |
| 静态方法 | `bool IsSha256(string value)` |
| 静态方法 | `IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)` |

### `ModuleDataStore`

静态类，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 静态方法 | `bool Clear(string installationRoot, string moduleId)` |

### `OmsiLaunchRuntimePaths`

密封 record，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string ReleaseManifestPath = null)` |
| 属性 | `string PluginBuildDirectory { get; init; }` |
| 属性 | `string NativeBridgePath { get; init; }` |
| 属性 | `string ReleaseManifestPath { get; init; }` |

### `OmsiLaunchService`

密封类，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)` |
| 方法 | `Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |
| 方法 | `Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default)` |
| 方法 | `Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `ProfileNew`

密封 record，位于 `OmsiLaunch.Core`。稳定性：`EXPERIMENTAL`。语义： [session-profiles](session-profiles.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `ProfileNew(string Map = null, int? EntrypointIndex = default, string EntrypointIdentity = null, SemanticDate Date = null, SemanticTime Time = null, int? Year = default, WeatherSpec Weather = null)` |
| 属性 | `string Map { get; init; }` |
| 属性 | `int? EntrypointIndex { get; init; }` |
| 属性 | `string EntrypointIdentity { get; init; }` |
| 属性 | `SemanticDate Date { get; init; }` |
| 属性 | `SemanticTime Time { get; init; }` |
| 属性 | `int? Year { get; init; }` |
| 属性 | `WeatherSpec Weather { get; init; }` |

### `ProfilePreset`

密封 record，位于 `OmsiLaunch.Core`。稳定性：`EXPERIMENTAL`。语义： [session-profiles](session-profiles.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `ProfilePreset(int Index, string Id, string Name, IReadOnlyDictionary<string, string> Settings, SessionPresentationSpec Presentation, InternetTexturesSpec InternetTextures, LaunchBehaviorSpec Behavior, IReadOnlyList<HofOperationSpec> Hofs, RuntimeModulesSpec Modules = null)` |
| 属性 | `int Index { get; init; }` |
| 属性 | `string Id { get; init; }` |
| 属性 | `string Name { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Settings { get; init; }` |
| 属性 | `SessionPresentationSpec Presentation { get; init; }` |
| 属性 | `InternetTexturesSpec InternetTextures { get; init; }` |
| 属性 | `LaunchBehaviorSpec Behavior { get; init; }` |
| 属性 | `IReadOnlyList<HofOperationSpec> Hofs { get; init; }` |
| 属性 | `RuntimeModulesSpec Modules { get; init; }` |

### `SessionPlanner`

密封类，位于 `OmsiLaunch.Core`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionPlanner(IRuntimePlatform platform)` |
| 方法 | `Task<SessionPlan> PlanAsync(LaunchSpec spec, CancellationToken cancellationToken = default)` |

### `SessionProfileCompiler`

静态类，位于 `OmsiLaunch.Core`。稳定性：`EXPERIMENTAL`。语义： [session-profiles](session-profiles.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string Schema = "omsilaunch.session-profile/v1"` |
| 常量 | `long MaxBytes = 262144` |
| 常量 | `int MinPresetIndex = 1` |
| 常量 | `int MaxPresetIndex = 99` |
| 静态字段 | `IReadOnlyDictionary<string, IReadOnlyList<string>> SchemaKeys` |
| 静态方法 | `LaunchSpec Apply(SessionProfilePackage profile, LaunchSpec baseline, WorldMode selectedWorldMode)` |
| 静态方法 | `IReadOnlyList<SessionProfileSummary> Discover(string installationRoot)` |
| 静态方法 | `SessionProfilePackage Load(string installationRoot, string id, int presetIndex)` |
| 静态方法 | `void ValidateCompatibility(SessionProfilePackage profile, string installationRoot, WorldSpec world, WorldMode mode)` |

### `SessionProfileException`

密封类，位于 `OmsiLaunch.Core`。稳定性：`EXPERIMENTAL`。语义： [session-profiles](session-profiles.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionProfileException(string code, string message)` |
| 属性 | `string Code { get; }` |

### `SessionProfilePackage`

密封 record，位于 `OmsiLaunch.Core`。稳定性：`EXPERIMENTAL`。语义： [session-profiles](session-profiles.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `SessionProfilePackage(string RootPath, SessionProfileMetadata Metadata, IReadOnlyList<string> CompatibleMaps, ProfileNew New, ProfilePreset Preset)` |
| 属性 | `string RootPath { get; init; }` |
| 属性 | `SessionProfileMetadata Metadata { get; init; }` |
| 属性 | `IReadOnlyList<string> CompatibleMaps { get; init; }` |
| 属性 | `ProfileNew New { get; init; }` |
| 属性 | `ProfilePreset Preset { get; init; }` |

### `StartDateTimeResolver`

静态类，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `int MinimumYear = 1900` |
| 常量 | `int MaximumYear = 2200` |
| 静态方法 | `StartDateTime Resolve(LaunchSpec spec, DateTime now, out string error)` |

### `StartWeatherResolver`

静态类，位于 `OmsiLaunch.Core`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 静态方法 | `StartWeather Resolve(LaunchSpec spec, out string error)` |

## `OmsiLaunch.Process`

### `CurrentRuntimeCommandStore`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 属性 | `string Name { get; }` |
| 属性 | `Guid SessionId { get; }` |
| 静态方法 | `CurrentRuntimeCommandStore Create(Guid sessionId)` |
| 方法 | `void Dispose()` |
| 方法 | `Task<RuntimeCommandResult> RequestAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)` |

### `CurrentStartupHandoffStore`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 属性 | `string Name { get; }` |
| 属性 | `Guid SessionId { get; }` |
| 静态方法 | `CurrentStartupHandoffStore Create(StartupHandoff handoff)` |
| 方法 | `void Dispose()` |

### `CurrentTelemetryStore`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 属性 | `string Name { get; }` |
| 静态方法 | `CurrentTelemetryStore Create(Guid sessionId)` |
| 方法 | `void Dispose()` |
| 方法 | `ValueTuple<IReadOnlyList<ValueTuple<long, string>>, long> ReadNew()` |

### `CurrentWindowsX64Platform`

密封类，位于 `OmsiLaunch.Process`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `CurrentWindowsX64Platform()` |
| 方法 | `RuntimePlatformInfo Detect(string root)` |
| 方法 | `bool HasExited(LaunchedProcess p)` |
| 方法 | `bool IsInstallationWritable(string root)` |
| 方法 | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| 方法 | `void Terminate(LaunchedProcess p)` |
| 方法 | `void ValidateCurrent(RuntimePlatformInfo p)` |
| 方法 | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken token)` |

### `IOmsiProcessController`

接口，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 方法 | `OmsiProcessState Observe()` |
| 方法 | `Task<int> StartAsync(string installation, CancellationToken cancellationToken = default)` |
| 方法 | `Task StopAsync(CancellationToken cancellationToken = default)` |

### `IRuntimePlatform`

接口，位于 `OmsiLaunch.Process`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 方法 | `RuntimePlatformInfo Detect(string installationRoot)` |
| 方法 | `bool HasExited(LaunchedProcess process)` |
| 方法 | `bool IsInstallationWritable(string installationRoot)` |
| 方法 | `Task<LaunchedProcess> StartAsync(StartupProcessRequest request, string executableSha256, CancellationToken cancellationToken)` |
| 方法 | `void Terminate(LaunchedProcess process)` |
| 方法 | `void ValidateCurrent(RuntimePlatformInfo platform)` |
| 方法 | `Task WaitForExitAsync(LaunchedProcess process, CancellationToken cancellationToken)` |

### `InstallationLease`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 静态方法 | `InstallationLease Acquire(string installationRoot)` |
| 方法 | `void Dispose()` |
| 静态方法 | `string NormalizeRoot(string installationRoot)` |

### `LaunchedProcess`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 属性 | `int ProcessId { get; }` |
| 属性 | `int ThreadId { get; }` |
| 属性 | `ProcessIdentity Identity { get; }` |
| 方法 | `void Dispose()` |

### `ModuleSessionProcess`

密封类，位于 `OmsiLaunch.Process`。稳定性：`STABLE_BETA`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `int MaxLineBytes = 65536` |
| 属性 | `int ProcessId { get; }` |
| 属性 | `bool HasExited { get; }` |
| 属性 | `int? ExitCode { get; }` |
| 方法 | `void Dispose()` |
| 方法 | `void Kill()` |
| 静态方法 | `ModuleSessionProcess Start(string executable, string workingDirectory, Guid sessionId, Action<string> onLine)` |
| 方法 | `bool TryWriteLine(string line)` |
| 方法 | `bool WaitForExit(TimeSpan timeout)` |

### `OmsiProcessState`

密封 record，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `OmsiProcessState(string State, int? ProcessId, bool Responding)` |
| 属性 | `string State { get; init; }` |
| 属性 | `int? ProcessId { get; init; }` |
| 属性 | `bool Responding { get; init; }` |

### `ProcessIdentity`

密封 record，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `ProcessIdentity(int ProcessId, DateTimeOffset CreationTimeUtc, string ExecutablePath, string ExecutableSha256)` |
| 属性 | `int ProcessId { get; init; }` |
| 属性 | `DateTimeOffset CreationTimeUtc { get; init; }` |
| 属性 | `string ExecutablePath { get; init; }` |
| 属性 | `string ExecutableSha256 { get; init; }` |

### `ReleaseManifest`

静态类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 常量 | `string FileName = "release-manifest.json"` |
| 静态方法 | `IReadOnlyDictionary<string, string> ParsePluginHashes(byte[] bytes)` |
| 静态方法 | `IReadOnlyDictionary<string, string> TryReadPluginHashes(string manifestPath)` |

### `RuntimeArtifact`

密封 record，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `RuntimeArtifact(string SourcePath, string DestinationRelativePath, string Sha256, long Size)` |
| 属性 | `string SourcePath { get; init; }` |
| 属性 | `string DestinationRelativePath { get; init; }` |
| 属性 | `string Sha256 { get; init; }` |
| 属性 | `long Size { get; init; }` |

### `RuntimeArtifactSet`

密封类，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 属性 | `IReadOnlyList<RuntimeArtifact> Artifacts { get; }` |
| 属性 | `IReadOnlyDictionary<string, string> ExpectedHashes { get; }` |
| 属性 | `string IntegrityReference { get; }` |
| 静态方法 | `RuntimeArtifactSet Load(string pluginBuildDirectory, string nativeBuildPath, IReadOnlyDictionary<string, string> expectedHashes = null)` |
| 方法 | `void ValidateInstalled(string installationRoot)` |

### `StartupProcessRequest`

密封 record，位于 `OmsiLaunch.Process`。稳定性：`INTERNAL`。语义： [public-api](public-api.md).

| 成员 | 签名 |
| --- | --- |
| 构造函数 | `StartupProcessRequest(string ExecutablePath, string WorkingDirectory, IReadOnlyDictionary<string, string> Environment)` |
| 属性 | `string ExecutablePath { get; init; }` |
| 属性 | `string WorkingDirectory { get; init; }` |
| 属性 | `IReadOnlyDictionary<string, string> Environment { get; init; }` |
