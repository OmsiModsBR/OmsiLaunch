namespace OmsiLaunch.Api;

public enum WorldMode
{
    NewMap = 0,
    SavedSituation = 1,
    LastMapState = 2,
    // Compatibility alias only: this is OMSI's native last-map-state branch,
    // never a selection of the newest .osn file.
    [Obsolete("Use LastMapState.")]
    LastSituation = LastMapState
}
public enum Presence : byte { Unset = 0, Set = 1 }
public enum DateTimeMode : byte { Unset = 0, Explicit = 1, System = 2 }
public enum WeatherMode : byte { Unset = 0, Preset = 1, Icao = 2, RealCurrent = 3 }
public enum EntrypointMode : byte { Unset = 0, PresentedIndex = 1, Identity = 2 }
public enum SplashMode : byte
{
    // Preserve the original OMSI splash. Native remains a compatibility alias.
    Unset = 0,
    Native = Unset,
    Managed = 1
}
public enum InternetTexturesMode : byte { Native = 0, Disabled = 1, Override = 2 }
public enum SessionState : byte { Created, ValidatingPlatform, Planning, AcquiringInstallationLock, RecoveringPreviousTransaction, Snapshotting, ApplyingConfiguration, DeployingRuntime, CreatingStartupHandoff, StartingProcess, WaitingForPlugin, PluginBootstrap, StartingWorld, EnteringGameplay, Running, ProcessExited, Restoring, CleaningRuntime, Completed, Failed }

public readonly record struct OptionalValue<T>(Presence Presence, T? Value)
{
    public bool IsSet => Presence == Presence.Set;
    public static OptionalValue<T> Unset => new(Presence.Unset, default);
    public static OptionalValue<T> Set(T value) => new(Presence.Set, value);
}

// These records are canonical semantic data. Platform adapters own Process,
// memory mapping, handles, and CLR-specific convenience types.
public sealed record SemanticDate(int Year, int Month, int Day);
public sealed record SemanticTime(int Hour, int Minute, int Second);
public sealed record InstallationSpec(string RootPath, string? ExpectedExecutableSha256 = null);
public sealed record EntrypointSpec(EntrypointMode Mode, OptionalValue<int> PresentedIndex, OptionalValue<string> Identity)
{
    public static EntrypointSpec Unset => new(EntrypointMode.Unset, OptionalValue<int>.Unset, OptionalValue<string>.Unset);
}
public sealed record WorldSpec(WorldMode Mode, OptionalValue<string> MapIdentity, OptionalValue<string> SituationIdentity, OptionalValue<int> PresentedEntrypointIndex, OptionalValue<string> EntrypointIdentity = default)
{
    public EntrypointSpec Entrypoint => EntrypointIdentity.IsSet
        ? new(EntrypointMode.Identity, OptionalValue<int>.Unset, EntrypointIdentity)
        : PresentedEntrypointIndex.IsSet
            ? new(EntrypointMode.PresentedIndex, PresentedEntrypointIndex, OptionalValue<string>.Unset)
            : EntrypointSpec.Unset;
}
public sealed record DateSpec(DateTimeMode Mode, OptionalValue<SemanticDate> Value);
public sealed record TimeSpec(DateTimeMode Mode, OptionalValue<SemanticTime> Value);
public sealed record YearSpec(DateTimeMode Mode, OptionalValue<int> Value);
public sealed record WeatherSpec(WeatherMode Mode, OptionalValue<string> Preset, OptionalValue<string> Icao);
public sealed record PlayerVehicleSpec(OptionalValue<string> Model, OptionalValue<string> Repaint, OptionalValue<string> Hof, OptionalValue<string> FleetNumber, OptionalValue<string> Registration)
{
    public bool Enabled => Model.IsSet;
}
public sealed record EnvironmentSpec(
    IReadOnlyDictionary<string, OptionalValue<string>> General,
    IReadOnlyDictionary<string, OptionalValue<string>> Advanced,
    IReadOnlyDictionary<string, OptionalValue<string>> Graphics,
    IReadOnlyDictionary<string, OptionalValue<string>> AdvancedGraphics,
    IReadOnlyDictionary<string, OptionalValue<string>> Sound,
    IReadOnlyDictionary<string, OptionalValue<string>> AiPassengers,
    IReadOnlyDictionary<string, OptionalValue<string>> Keyboard,
    IReadOnlyDictionary<string, OptionalValue<string>> Controllers);
public sealed record InputSpec(OptionalValue<string> KeyboardDocument, OptionalValue<string> ControllerDocument);
public sealed record DiagnosticsSpec(bool Log = true, bool Verbose = false, bool OmsiLogAll = false, bool ProcessTrace = false, bool PluginTrace = false, bool NativeTrace = false);
// Presentation is intentionally independent from session ownership. Integrators
// that render their own session affordance can suppress only the standalone tray.
public sealed record SessionPresentationSpec(SplashMode Splash = SplashMode.Managed, OptionalValue<string> Language = default, OptionalValue<string> CustomAssetDirectory = default, bool SuppressTrayIcon = false);
public sealed record InternetTexturesSpec(InternetTexturesMode Mode = InternetTexturesMode.Native, OptionalValue<string> OverrideProfilePath = default);
// Identifies declarative third-party session content without leaking its YAML
// representation into the rest of the public session API.
public sealed record SessionProfileMetadata(string Id, string Name, string Version, string Author, string PresetId, int PresetIndex, string PresetName, string PackagePath);
// Discovery view of one session-profile package. A package that does not
// compile is listed with IsValid = false and its OL_E_SESSION_PROFILE_* code.
public sealed record SessionProfileSummary(string Id, string? Name, string? Version, string? Author, IReadOnlyList<string> CompatibleMaps, string? MapIdentity, IReadOnlyList<SessionProfilePresetSummary> Presets, bool IsValid, string? ErrorCode, string? ErrorMessage);
public sealed record SessionProfilePresetSummary(int Index, string Id, string Name);
// Large maps and native saved situations can legitimately require more than one minute
// before they reach gameplay. Callers may still choose a tighter bounded timeout.
// CooperativeShutdown (Beta 4, opt-in until runtime validated): ending a
// running session asks OMSI to quit through its own exit routine and waits
// ShutdownTimeoutSeconds before terminating it.
public sealed record LaunchBehaviorSpec(bool RestoreConfiguration = true, bool SuppressStaleClosecheckWarning = true, int StartupTimeoutSeconds = 180, int ShutdownTimeoutSeconds = 30, bool CooperativeShutdown = false);
// HOF management: copy HOF files (bus-stop/route data read by OMSI from each
// vehicle folder) into vehicle folders for the duration of a session. Every
// written or removed file is a transaction entry and is restored exactly.
public enum HofSourceKind : byte
{
    // <root>\.omsilaunch\hof\<name>\*.hof
    Collection,
    // An installation-relative directory, for example next to map content.
    Directory,
    // The folder of an installed vehicle (identity of its .bus or .ovh file).
    Vehicle,
    // One installation-relative .hof file.
    File
}
public enum HofApplyMode : byte
{
    // Copy the source HOFs into each target folder; other HOFs stay.
    Add,
    // Copy the source HOFs and remove every other .hof from each target
    // folder for the session, so the target set equals the source set.
    Mirror
}
public sealed record HofOperationSpec(HofSourceKind SourceKind, string Source, IReadOnlyList<string> TargetVehicles, HofApplyMode Mode = HofApplyMode.Add, IReadOnlyList<string>? Files = null);
public sealed record HofManagementSpec(IReadOnlyList<HofOperationSpec> Operations);

public sealed record LaunchSpec(InstallationSpec Installation, WorldSpec World, DateSpec Date, TimeSpec Time, OptionalValue<PlayerVehicleSpec> PlayerVehicle, EnvironmentSpec Environment, LaunchBehaviorSpec Behavior, YearSpec? Year = null, WeatherSpec? Weather = null, InputSpec? Input = null, DiagnosticsSpec? Diagnostics = null, SessionPresentationSpec? Presentation = null, InternetTexturesSpec? InternetTextures = null, SessionProfileMetadata? SessionProfile = null, HofManagementSpec? HofManagement = null, RuntimeModulesSpec? Modules = null)
{
    public HofManagementSpec EffectiveHofManagement => HofManagement ?? new(Array.Empty<HofOperationSpec>());
    public RuntimeModulesSpec EffectiveModules => Modules ?? new(Array.Empty<string>());
    public YearSpec EffectiveYear => Year ?? new(DateTimeMode.Unset, OptionalValue<int>.Unset);
    public WeatherSpec EffectiveWeather => Weather ?? new(WeatherMode.Unset, OptionalValue<string>.Unset, OptionalValue<string>.Unset);
    public InputSpec EffectiveInput => Input ?? new(OptionalValue<string>.Unset, OptionalValue<string>.Unset);
    public DiagnosticsSpec EffectiveDiagnostics => Diagnostics ?? new();
    public SessionPresentationSpec EffectivePresentation => Presentation ?? new();
    public InternetTexturesSpec EffectiveInternetTextures => InternetTextures ?? new();
}

public sealed record LaunchDiagnostic(string Code, string Message, IReadOnlyDictionary<string, string>? Data = null);
// SessionTickMilliseconds: the plugin's monotonic session clock when the
// plugin emitted the event (null for host-side events).
public sealed record RuntimeEvent(string Type, DateTimeOffset TimestampUtc, long Sequence, IReadOnlyDictionary<string, string> Data, long? SessionTickMilliseconds = null);
public sealed record RuntimePlatformInfo(string OsFamily, string OsVersion, string OsArchitecture, string HostArchitecture, string OmsiArchitecture, string PluginArchitecture, bool CurrentPlatformSupported, bool LegacyPlatform, bool Wow64Available, bool InstallationWritable, bool ProcessLaunchSupported, bool PluginRuntimeSupported, bool NativeInteropSupported, bool SharedMemorySupported, bool ExactRestoreSupported);
public sealed record Capability(string Name, bool Available, string EvidenceState, string? Reason = null);
public sealed record PlannedMutation(string RelativePath, string SemanticKey, string RequestedValue, string Operation);
public sealed record SessionPlan(Guid SessionId, string BuildProfileId, LaunchSpec Spec, RuntimePlatformInfo Platform, IReadOnlyList<ContentIdentity> ResolvedContent, IReadOnlyList<string> TouchedFiles, IReadOnlyList<string> RuntimeArtifacts, IReadOnlyList<Capability> RequiredCapabilities, IReadOnlyList<Capability> UnsupportedRequestedFeatures, IReadOnlyList<PlannedMutation> PlannedMutations, IReadOnlyList<LaunchDiagnostic> Diagnostics, bool IsRunnable, IReadOnlyList<PlannedModule>? PlannedModules = null)
{
    public IReadOnlyList<PlannedModule> EffectivePlannedModules => PlannedModules ?? Array.Empty<PlannedModule>();
}
// Runtime modules for a session (Beta 4). Enable requests modules by id;
// Disable keeps a default, content or settings-file module out of the
// session; Settings sets module settings by "<module-id>.<key>".
public sealed record RuntimeModulesSpec(IReadOnlyList<string> Enable, IReadOnlyList<string>? Disable = null, IReadOnlyDictionary<string, string>? Settings = null)
{
    private static readonly IReadOnlyDictionary<string, string> NoSettings = new Dictionary<string, string>(StringComparer.Ordinal);
    public IReadOnlyList<string> EffectiveDisable => Disable ?? Array.Empty<string>();
    public IReadOnlyDictionary<string, string> EffectiveSettings => Settings ?? NoSettings;
}
// A module's plan-time evaluation. Status is ELIGIBLE (it is loaded) or
// REJECTED (Code and Reason say why). Operations are the public operation ids
// (module.<id>.<operation>) the module exposes while the session runs.
// Activation says why the module is in the plan: requested (LaunchSpec,
// command line or session profile), settings-file, content or default.
// Settings are the effective values of the settings the module declares.
public sealed record PlannedModule(string Id, string Version, string Status, bool Requested, string FailurePolicy, IReadOnlyList<string> Operations, string? Code = null, string? Reason = null, string? ManifestSha256 = null, string Activation = PlannedModule.ActivationRequested, IReadOnlyList<PlannedModuleSetting>? Settings = null)
{
    public const string Eligible = "ELIGIBLE";
    public const string Rejected = "REJECTED";
    public const string ActivationRequested = "requested";
    public const string ActivationSettingsFile = "settings-file";
    public const string ActivationContent = "content";
    public const string ActivationDefault = "default";
    public bool IsEligible => Status == Eligible;
    public IReadOnlyList<PlannedModuleSetting> EffectiveSettings => Settings ?? Array.Empty<PlannedModuleSetting>();
}
// One effective module setting. Source is request, settings-file or default.
public sealed record PlannedModuleSetting(string Key, string Value, string Source);
// An installed module as discovery sees it. Activation is requested, content
// or default; Settings describe the settings it declares.
public sealed record RuntimeModuleSummary(string Id, string? Version, string? Name, string? Activation, bool IsValid, IReadOnlyList<string> Operations, string? ContentBindingMap = null, string? ErrorCode = null, string? ErrorMessage = null, IReadOnlyList<RuntimeModuleSettingSummary>? Settings = null);
// A setting a module declares. Type is boolean, integer, choice or text;
// Default "auto" means the module decides; Flag is its command-line shortcut.
public sealed record RuntimeModuleSettingSummary(string Key, string Type, string Default, bool AllowsAuto = false, string? Minimum = null, string? Maximum = null, IReadOnlyList<string>? Choices = null, string? Flag = null, string? Description = null);
public sealed record SessionStatus(Guid SessionId, SessionState State, IReadOnlyList<LaunchDiagnostic> Diagnostics, IReadOnlyList<RuntimeEvent>? RuntimeEvents = null);
public sealed record SessionHandle(Guid SessionId);
// Result of an explicit recovery request. Pending means a durable journal
// exists; Recovered means it was restored, verified and removed by this call.
public sealed record RecoveryStatus(bool Pending, bool Recovered, IReadOnlyList<LaunchDiagnostic> Diagnostics);
public sealed record ContentIdentity(string Identity, string Kind, string? DisplayName = null);
public enum ContentQueryKind : byte { Maps, Situations, Vehicles, Repaints, Hofs, FleetNumbers, Registrations, Addons, Entrypoints, SessionProfiles, RuntimeModules }

public interface IOmsiLaunch
{
    Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default);
    Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default);
    Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default);
    Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default);
    // Recovery of a stale durable journal. It takes the installation lease, so
    // it never restores files underneath a session that is still starting.
    Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default);
}
