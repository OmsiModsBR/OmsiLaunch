using System.Collections.Concurrent;
using System.Text.Json;
using OmsiLaunch.Api;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.Configuration;
using OmsiLaunch.Content;
using OmsiLaunch.Process;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Core;

// ReleaseManifestPath is the packaged release-manifest.json when the controller
// runs from an installed package; it supplies the expected plugin hashes.
public sealed record OmsiLaunchRuntimePaths(string PluginBuildDirectory, string NativeBridgePath, string? ReleaseManifestPath = null);

public sealed class OmsiLaunchService : IOmsiLaunch
{
    private readonly IRuntimePlatform platform;
    private readonly SessionPlanner planner;
    private readonly OmsiLaunchRuntimePaths runtimePaths;
    private readonly ConcurrentDictionary<Guid, LiveSession> sessions = new();

    public OmsiLaunchService(IRuntimePlatform platform, OmsiLaunchRuntimePaths runtimePaths)
    {
        this.platform = platform; this.runtimePaths = runtimePaths; planner = new SessionPlanner(platform);
    }
    public async Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default)
    {
        var plan = await planner.PlanAsync(spec, cancellationToken).ConfigureAwait(false);
        try
        {
            var artifacts = LoadArtifacts();
            // The installed closure is part of runnability: a plugin missing from
            // or altered in plugins\ must make the plan non-runnable, so that the
            // re-plan in StartSessionAsync rejects before any session exists.
            artifacts.ValidateInstalled(spec.Installation.RootPath);
            var diagnostics = plan.Diagnostics.Append(new LaunchDiagnostic("plugin.integrity.reference", artifacts.IntegrityReference)).ToArray();
            return plan with { RuntimeArtifacts = artifacts.Artifacts.Select(x => x.DestinationRelativePath).Append("OmsiLaunch startup handoff v6").ToArray(), Diagnostics = diagnostics };
        }
        catch (Exception exception) when (exception is FileNotFoundException or DirectoryNotFoundException or InvalidDataException)
        {
            var code = exception.Message.StartsWith("OL_E_PERMANENT_PLUGIN_", StringComparison.Ordinal) ? exception.Message.Split(':')[0] : "OL_E_RUNTIME_ARTIFACT_MISSING";
            var detail = exception is FileNotFoundException { FileName: { } file } ? exception.Message + ": " + file : exception.Message;
            var diagnostics = plan.Diagnostics.Append(new LaunchDiagnostic(code, detail)).ToArray();
            return plan with { Diagnostics = diagnostics, IsRunnable = false };
        }
    }
    public async Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default)
    {
        if (!plan.IsRunnable) throw new InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE");
        // A SessionPlan is a public record and can be edited or go stale between
        // planning and start. The spec is re-planned here so that the executable
        // fingerprint, content resolution and runnability are current facts.
        var current = await PlanSessionAsync(plan.Spec, cancellationToken).ConfigureAwait(false);
        if (!current.IsRunnable) throw new InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: " + string.Join("; ", current.Diagnostics.Where(x => x.Code.StartsWith("OL_E_", StringComparison.Ordinal)).Select(x => x.Code)));
        plan = current with { SessionId = plan.SessionId };
        var session = new LiveSession(plan.SessionId); if (!sessions.TryAdd(plan.SessionId, session)) throw new InvalidOperationException("Duplicate session id.");
        try { await StartAsync(session, plan, cancellationToken).ConfigureAwait(false); return new SessionHandle(plan.SessionId); }
        catch { sessions.TryRemove(plan.SessionId, out _); throw; }
    }
    public Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default) => Task.FromResult(Get(session).Status());
    public async Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState state, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var live = Get(session); using var timeoutSource = new CancellationTokenSource(timeout); using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutSource.Token);
        try
        {
            while (live.State != state && live.State != SessionState.Failed && live.State != SessionState.Completed) await Task.Delay(100, linked.Token).ConfigureAwait(false);
        }
        // A timeout is an answer, not a fault: the caller receives the status
        // it can act on. Caller cancellation still propagates.
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested) { }
        return live.Status();
    }
    public async Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default)
    {
        var root = Path.GetFullPath(installation.RootPath);
        InstallationLease lease;
        try { lease = InstallationLease.Acquire(root); }
        catch (InvalidOperationException) { throw new InvalidOperationException("OL_E_INSTALLATION_BUSY: another OmsiLaunch owner holds this installation."); }
        using (lease)
        {
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            var pending = await transaction.HasPendingRecoveryAsync(cancellationToken).ConfigureAwait(false);
            if (!pending || !restore) return new RecoveryStatus(pending, false, Array.Empty<LaunchDiagnostic>());
            await transaction.RestorePendingAsync(cancellationToken).ConfigureAwait(false);
            var recovered = !await transaction.HasPendingRecoveryAsync(cancellationToken).ConfigureAwait(false);
            return new RecoveryStatus(true, recovered, transaction.RestoreNotes.Select(ToDiagnostic).ToArray());
        }
    }
    public Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default) { Get(session).RequestStop(); return Task.CompletedTask; }
    public async Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default)
    {
        var live = Get(session);
        // Closing a consumer handle must never strand a live transaction. A caller that
        // closes early requests the normal stop path, then waits for supervisor-owned
        // process exit, restoration, and lease release before the session is forgotten.
        if (!live.IsTerminal) live.RequestStop();
        if (live.LifecycleTask is { } lifecycle) await lifecycle.WaitAsync(cancellationToken).ConfigureAwait(false);
        sessions.TryRemove(session.SessionId, out _);
    }
    public async Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        // This is the public boundary. Internal research primitives and unknown
        // operations are rejected here, before any frontend, session lookup or
        // the mailbox is involved.
        // A module operation (module.<id>.<operation>) is public only for a
        // module the session loaded; the plugin checks its arguments.
        if (RuntimeModuleProtocol.TrySplitOperation(command.Operation, out var moduleId, out _))
        {
            var moduleSession = Get(session);
            var planned = moduleSession.PlannedModules.FirstOrDefault(x => x.Id == moduleId && x.IsEligible);
            if (planned is null) return new RuntimeCommandResult(session.SessionId, command.RequestId, false, PublicErrorCodes.MODULE_NOT_LOADED);
            if (!planned.Operations.Contains(command.Operation, StringComparer.Ordinal)) return new RuntimeCommandResult(session.SessionId, command.RequestId, false, PublicErrorCodes.MODULE_OPERATION_UNKNOWN);
        }
        else
        {
            var validation = PublicCapabilityRegistry.ValidateRuntimeArguments(command.Operation, command.Arguments);
            if (!validation.Accepted) return new RuntimeCommandResult(session.SessionId, command.RequestId, false, validation.ErrorCode);
        }
        var live = Get(session);
        if (command.SessionId != session.SessionId) throw new InvalidOperationException("OL_E_RUNTIME_SESSION_MISMATCH");
        if (live.State != SessionState.Running) throw new InvalidOperationException("OL_E_SESSION_NOT_RUNNING");
        var result = await live.RequestRuntimeAsync(command, timeout, cancellationToken).ConfigureAwait(false);
        return ScrubInternalValues(result);
    }
    // Defence in depth: whatever the bridge produced, native layout never
    // leaves the API. Keys following the internal naming convention are dropped.
    private static RuntimeCommandResult ScrubInternalValues(RuntimeCommandResult result)
    {
        if (result.Values is null || !result.Values.Keys.Any(PublicCapabilityRegistry.IsInternalResultKey)) return result;
        return result with { Values = result.Values.Where(pair => !PublicCapabilityRegistry.IsInternalResultKey(pair.Key)).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal) };
    }
    private RuntimeArtifactSet LoadArtifacts()
    {
        var expected = runtimePaths.ReleaseManifestPath is { } manifest ? ReleaseManifest.TryReadPluginHashes(manifest) : null;
        return RuntimeArtifactSet.Load(runtimePaths.PluginBuildDirectory, runtimePaths.NativeBridgePath, expected);
    }
    private static LaunchDiagnostic ToDiagnostic(RestoreNote note) => new(note.Code, note.RelativePath, note.Sha256 is null ? null : new Dictionary<string, string> { ["sha256"] = note.Sha256 });
    // Derived from the canonical catalog (R3): one entry per public or
    // unsupported capability, named by its catalog id, carrying its catalog
    // evidence. Availability is evaluated for this installation: platform,
    // OMSI 2, classification, and the native capabilities it depends on.
    public Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var platformInfo = platform.Detect(installation.RootPath);
        var build = OmsiBuildDetection.Detect(installation.RootPath);
        IReadOnlyList<Capability> capabilities = PublicCapabilityRegistry.All
            .Where(descriptor => descriptor.Classification != PublicCapabilityClassification.InternalOnly)
            .Select(descriptor => Evaluate(descriptor, platformInfo, build))
            .ToArray();
        return Task.FromResult(capabilities);
    }

    private static Capability Evaluate(PublicCapabilityDescriptor descriptor, RuntimePlatformInfo platformInfo, OmsiBuildReport build)
    {
        if (descriptor.Classification == PublicCapabilityClassification.Unsupported) return new(descriptor.Id, false, descriptor.RuntimeValidation, descriptor.EvidenceNote ?? descriptor.Description);
        if (!platformInfo.CurrentPlatformSupported) return new(descriptor.Id, false, descriptor.RuntimeValidation, "The current platform is not supported.");
        if (descriptor.Id == "runtime.current-windows-x64") return new(descriptor.Id, true, descriptor.RuntimeValidation, descriptor.EvidenceNote);
        if (!build.IsOmsi2) return new(descriptor.Id, false, descriptor.RuntimeValidation, build.Reason ?? "Not an OMSI 2 installation.");
        var native = descriptor.NativeCapabilities;
        if (native.Count == 0 || build.KnownIdentity) return new(descriptor.Id, true, descriptor.RuntimeValidation, Join(descriptor.EvidenceNote, native.Count == 0 ? null : "native: RESOLVED_KNOWN_LAYOUT"));
        // Unknown build: the file image predicts; the in-process resolution decides.
        var unresolved = native.Where(name => !build.StaticCapabilities.TryGetValue(name, out var available) || !available).ToArray();
        return new(descriptor.Id, true, descriptor.RuntimeValidation, Join(descriptor.EvidenceNote, unresolved.Length == 0
            ? "native: STATICALLY_RESOLVED"
            : "native: PENDING_IN_PROCESS_RESOLUTION (" + string.Join(", ", unresolved) + " not resolved from the executable file; decided in process, OL_E_CAPABILITY_UNRESOLVED if unavailable)"));
    }

    private static string? Join(string? first, string? second) => first is null ? second : second is null ? first : first + " " + second;
    public Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SessionProfileCompiler.Discover(installation.RootPath));
    }
    public Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<RuntimeModuleSummary> modules = ModuleCatalog.Discover(installation.RootPath).Select(Summarize).ToArray();
        return Task.FromResult(modules);
    }
    private static RuntimeModuleSummary Summarize(DiscoveredModule module) => new(
        module.Manifest?.Id ?? module.DirectoryName, module.Manifest?.Version, module.Manifest?.Name,
        module.Manifest is null ? null : ModuleEligibility.ActivationName(module.Manifest.Activation),
        module.IsUsable, module.Manifest?.Operations.Select(x => module.Manifest.OperationName(x.Id)).ToArray() ?? Array.Empty<string>(),
        module.Manifest?.ContentBinding?.Map, module.Code, module.Message,
        module.Manifest?.EffectiveSettings.Select(Summarize).ToArray());
    private static RuntimeModuleSettingSummary Summarize(ModuleSettingDescriptor setting) => new(
        setting.Key, setting.Type.ToString().ToLowerInvariant(), setting.Default, setting.AllowsAuto,
        setting.Type == ModuleSettingType.Integer && setting.Minimum != long.MinValue ? setting.Minimum.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
        setting.Type == ModuleSettingType.Integer && setting.Maximum != long.MaxValue ? setting.Maximum.ToString(System.Globalization.CultureInfo.InvariantCulture) : null,
        setting.Type == ModuleSettingType.Choice ? setting.Choices : null, setting.Flag, setting.Description);
    public Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var catalog = new FileSystemContentCatalog(installation.RootPath);
        IEnumerable<ContentIdentity> results = query switch
        {
            ContentQueryKind.Maps => catalog.EnumerateMaps().Select(x => new ContentIdentity(x.Identity, "map", x.DisplayName)),
            ContentQueryKind.Situations => catalog.EnumerateSituations().Select(x => new ContentIdentity(x.Identity, "situation", x.MapIdentity)),
            ContentQueryKind.Vehicles => catalog.EnumerateVehicles().Select(x => new ContentIdentity(x.Identity, "vehicle", x.DisplayName)),
            ContentQueryKind.Repaints when scope.IsSet => catalog.EnumerateRepaints(scope.Value!).Select(x => new ContentIdentity(x.Identity, "repaint", x.DisplayName)),
            ContentQueryKind.Hofs => catalog.EnumerateHofs().Select(x => new ContentIdentity(x.Identity, "hof")),
            ContentQueryKind.Entrypoints => scope.IsSet
                ? catalog.EnumerateEntrypoints(scope.Value!).Select(x => new ContentIdentity(x.Identity, "entrypoint", x.DisplayName))
                : throw new ArgumentException("Entrypoint discovery requires a map identity scope.", nameof(scope)),
            ContentQueryKind.FleetNumbers when scope.IsSet => catalog.EnumerateFleetNumbers(scope.Value!).Select(x => new ContentIdentity(x.SourceIdentity, "fleet-number")),
            ContentQueryKind.Registrations when scope.IsSet => catalog.EnumerateRegistrations(scope.Value!).Select(x => new ContentIdentity(x.Mode, "registration", x.Value)),
            ContentQueryKind.Addons => catalog.EnumerateAddons().Select(x => new ContentIdentity(x.Identity, "addon", x.DiscoveryConfidence)),
            ContentQueryKind.SessionProfiles => SessionProfileCompiler.Discover(installation.RootPath).Select(x => new ContentIdentity(x.Id, x.IsValid ? "session-profile" : "session-profile-invalid", x.Name ?? x.ErrorCode)),
            ContentQueryKind.RuntimeModules => ListRuntimeModulesAsync(installation, cancellationToken).Result.Select(x => new ContentIdentity(x.Id, x.IsValid ? "runtime-module" : "runtime-module-invalid", x.Name ?? x.ErrorCode)),
            _ => Array.Empty<ContentIdentity>()
        };
        return Task.FromResult<IReadOnlyList<ContentIdentity>>(results.ToArray());
    }

    private async Task StartAsync(LiveSession session, SessionPlan plan, CancellationToken cancellationToken)
    {
        var trace = new HostTrace(plan.Spec.Installation.RootPath, plan.SessionId); trace.Write("STARTSESSION_ENTER");
        if (plan.Spec.Behavior.StartupTimeoutSeconds is < 1 or > 600) throw new ArgumentOutOfRangeException(nameof(plan), "Startup timeout must be between 1 and 600 seconds.");
        InstallationLease? lease = null; FileConfigurationTransaction? transaction = null; CurrentStartupHandoffStore? handoff = null; CurrentTelemetryStore? telemetry = null; CurrentRuntimeCommandStore? runtime = null; LaunchedProcess? process = null; var processExited = false;
        var programs = ModuleSessionPrograms.None;
        try
        {
            var root = plan.Spec.Installation.RootPath;
            trace.Write("PLAN_ACCEPTED"); session.Move(SessionState.AcquiringInstallationLock); lease = InstallationLease.Acquire(root); trace.Write("INSTALLATION_LEASE_ACQUIRED");
            session.Move(SessionState.RecoveringPreviousTransaction);
            // A stale journal is recovered before anything reads the live
            // installation: overlays and the splash language must derive from
            // the original files, not from a previous session's leftovers.
            var deferredRecovery = false;
            var earlyRecovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            if (await earlyRecovery.HasPendingRecoveryAsync(cancellationToken).ConfigureAwait(false))
            {
                try { await earlyRecovery.RestorePendingAsync(cancellationToken).ConfigureAwait(false); AddRestoreNotes(session, earlyRecovery); trace.Write("PENDING_JOURNAL_RECOVERED"); }
                catch (IOException exception) when (exception.Message.StartsWith("OL_E_RECOVERY_ABSENT_OWNERSHIP_UNVERIFIED", StringComparison.Ordinal))
                {
                    // A pre-fingerprint journal can only be closed with this
                    // session's planned bytes; retried once overlays exist.
                    deferredRecovery = true; trace.Write("PENDING_JOURNAL_RECOVERY_DEFERRED", exception.Message);
                }
            }
            var artifacts = LoadArtifacts();
            if (plan.Spec.EffectivePresentation.Splash == SplashMode.Managed) SessionVisualAssets.EnsureInstallationAssets(root);
            var visualPlan = SessionVisualAssets.Build(plan.Spec);
            // HOF sources and targets are read after early recovery, so they
            // derive from original files, never from a previous session's copies.
            var hofPlan = HofPlanner.Build(plan.Spec);
            artifacts.ValidateInstalled(root); trace.Write("PERMANENT_PLUGIN_INSTALLATION_VALIDATED", artifacts.IntegrityReference);
            var executableSha256 = HashFile(Path.Combine(root, "Omsi.exe"));
            // closecheck is OMSI's own crash marker. A stale one is removed when
            // the spec asks for it (a documented permanent removal), and the
            // marker OMSI writes during this session is a session artifact that
            // the transaction restores to absence.
            var deletions = visualPlan.Deletions.Concat(hofPlan.Deletions).ToList();
            var closecheck = Path.Combine(root, "closecheck");
            if (File.Exists(closecheck) && plan.Spec.Behavior.SuppressStaleClosecheckWarning) RemoveStaleClosecheck(root, session);
            if (!File.Exists(closecheck)) deletions.Add("closecheck");
            transaction = new FileConfigurationTransaction(root, BuildTransactionalOverlays(plan.Spec, visualPlan, hofPlan), deletions, plan.SessionId); trace.Write("TRANSACTION_STARTED");
            if (deferredRecovery) { await transaction.RestorePendingAsync(cancellationToken).ConfigureAwait(false); AddRestoreNotes(session, transaction); trace.Write("PENDING_JOURNAL_RECOVERED"); }
            session.Move(SessionState.Snapshotting); session.Move(SessionState.ApplyingConfiguration); await transaction.ApplyAsync(cancellationToken).ConfigureAwait(false);
            session.Move(SessionState.DeployingRuntime); await transaction.MarkStateAsync(TransactionState.RuntimeDeployed, cancellationToken).ConfigureAwait(false); trace.Write("PERMANENT_RUNTIME_READY");
            // Module session programs run before OMSI exists, on the session's
            // own files, and OMSI starts only once they are prepared.
            programs = ModuleSessionPrograms.Start(plan, (name, data) => session.AddRuntimeEvent(name, data), diagnostic => session.AddDiagnostic(diagnostic), (step, detail) => trace.Write(step, detail), cancellationToken);
            var excluded = programs.Excluded;
            handoff = CurrentStartupHandoffStore.Create(new StartupHandoff(plan.SessionId, plan.BuildProfileId, plan.Spec.World.Mode, plan.Spec.World.MapIdentity.Value ?? string.Empty, plan.Spec.World.PresentedEntrypointIndex.IsSet ? plan.Spec.World.PresentedEntrypointIndex.Value : -1, true, plan.Spec.PlayerVehicle.IsSet, plan.Spec.Date.Mode, plan.Spec.Time.Mode, plan.Spec.World.EntrypointIdentity.IsSet ? EntrypointLabel(plan) : string.Empty, plan.Spec.World.SituationIdentity.IsSet ? plan.Spec.World.SituationIdentity.Value! : string.Empty,
                plan.EffectivePlannedModules.Where(x => x.IsEligible && !excluded.Contains(x.Id)).Select(x => new StartupModule(RuntimeModuleProtocol.ModulesDirectory + "\\" + x.Id, x.ManifestSha256!, ModuleSettingsJson(x))).ToArray(),
                plan.Spec.EffectiveDiagnostics.PluginTrace, plan.Spec.EffectiveDiagnostics.NativeTrace,
                plan.Spec.World.Mode == WorldMode.NewMap ? StartDateTimeResolver.Resolve(plan.Spec, DateTime.Now, out _) : null,
                StartWeatherResolver.Resolve(plan.Spec, out _)));
            session.PlannedModules = plan.EffectivePlannedModules.Select(x => excluded.Contains(x.Id) ? x with { Status = PlannedModule.Rejected, Code = PublicErrorCodes.MODULE_LOAD_FAILED, Reason = "its session program failed", Operations = Array.Empty<string>() } : x).ToArray();
            telemetry = CurrentTelemetryStore.Create(plan.SessionId);
            runtime = CurrentRuntimeCommandStore.Create(plan.SessionId);
            session.Move(SessionState.CreatingStartupHandoff); await transaction.MarkStateAsync(TransactionState.HandoffCreated, cancellationToken).ConfigureAwait(false); trace.Write("HANDOFF_CREATED");
            var environment = new Dictionary<string, string> { ["OMSILAUNCH_SESSION_ID"] = plan.SessionId.ToString("D"), ["OMSILAUNCH_HANDOFF_NAME"] = handoff.Name, ["OMSILAUNCH_TELEMETRY_NAME"] = telemetry.Name, ["OMSILAUNCH_RUNTIME_CHANNEL"] = runtime.Name, ["OMSILAUNCH_INTERNET_TEXTURES_MODE"] = plan.Spec.EffectiveInternetTextures.Mode.ToString() };
            session.Move(SessionState.StartingProcess); trace.Write("PROCESS_CREATE_ENTER");
            process = await platform.StartAsync(new StartupProcessRequest(Path.Combine(root, "Omsi.exe"), root, environment), executableSha256, cancellationToken).ConfigureAwait(false);
            trace.Write("PROCESS_CREATE_RETURN", process.ProcessId.ToString()); programs.NotifyOmsiStarted(process.ProcessId); session.Attach(process); trace.Write("PROCESS_OWNERSHIP_REGISTERED"); await transaction.RecordProcessAsync(process.Identity.ProcessId, process.Identity.CreationTimeUtc, process.Identity.ExecutablePath, cancellationToken).ConfigureAwait(false); trace.Write("JOURNAL_PROCESS_STARTED"); session.Move(SessionState.WaitingForPlugin);
            session.AttachRuntime(runtime); runtime = null;
            session.LifecycleTask = Task.Run(() => SuperviseAsync(session, plan, process, lease, transaction, handoff, telemetry, trace, programs)); trace.Write("SUPERVISOR_RUN_TASK_STARTED");
        }
        catch (Exception exception)
        {
            trace.Write("STARTSESSION_CALL_EXCEPTION", exception.ToString());
            session.Fail("OL_E_START_SESSION", exception.Message);
            if (process is not null)
            {
                try
                {
                    if (!platform.HasExited(process)) platform.Terminate(process);
                    await platform.WaitForExitAsync(process, CancellationToken.None).ConfigureAwait(false);
                    processExited = true;
                    if (transaction is not null) await transaction.MarkStateAsync(TransactionState.ProcessExited, CancellationToken.None).ConfigureAwait(false);
                }
                catch (Exception cleanupException)
                {
                    trace.Write("STARTSESSION_PROCESS_CLEANUP_FAILED", cleanupException.ToString());
                    session.AddDiagnostic("OL_E_PROCESS_CLEANUP_FAILED", cleanupException.Message);
                }
                finally { process.Dispose(); }
            }
            runtime?.Dispose(); telemetry?.Dispose(); handoff?.Dispose();
            // Session programs end before any file is restored (they may hold
            // mapped views of installation files).
            StopPrograms(programs, session, trace);
            if (transaction is not null && await transaction.HasPendingRecoveryAsync(CancellationToken.None).ConfigureAwait(false))
            {
                if (process is not null && !processExited)
                {
                    // Replacing installation files while a launched OMSI may
                    // still be using them is unsafe. The prepared journal is
                    // deliberately retained for the next recovery attempt.
                    trace.Write("STARTSESSION_RESTORE_DEFERRED_PROCESS_ALIVE");
                    session.Fail("OL_E_RESTORE_DEFERRED", "OMSI process exit was not confirmed; recovery remains pending.");
                }
                else try
                {
                    session.Move(SessionState.Restoring);
                    await transaction.RestoreAsync(CancellationToken.None).ConfigureAwait(false);
                    AddRestoreNotes(session, transaction);
                    session.Move(SessionState.CleaningRuntime);
                }
                catch (Exception restoreException)
                {
                    // RestoreAsync leaves the journal in place until every owned
                    // original has been restored and verified.
                    trace.Write("STARTSESSION_RESTORE_FAILED", restoreException.ToString());
                    session.Fail("OL_E_RESTORE_FAILED", restoreException.Message);
                }
            }
            lease?.Dispose();
        }
    }

    // The exact label the planner resolved for the requested entrypoint.
    private static string EntrypointLabel(SessionPlan plan) =>
        plan.ResolvedContent.FirstOrDefault(content => content.Kind == "entrypoint")?.DisplayName ?? throw new InvalidOperationException(PublicErrorCodes.ENTRYPOINT_NOT_FOUND);

    // OMSI quits through its own exit routine (it saves options.cfg and the
    // map's laststn.osn first); after ShutdownTimeoutSeconds the caller
    // terminates it. The restore runs after the exit either way.
    private async Task CooperativeExitAsync(LiveSession session, SessionPlan plan, LaunchedProcess process, HostTrace trace)
    {
        try
        {
            var result = await session.RequestRuntimeAsync(new RuntimeCommand(session.Id, ulong.MaxValue - 1, "session.close"), TimeSpan.FromSeconds(5), CancellationToken.None).ConfigureAwait(false);
            trace.Write("COOPERATIVE_EXIT_REQUESTED", result.Succeeded ? "scheduled" : result.ErrorCode);
            if (!result.Succeeded) { session.AddDiagnostic("OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE", "OMSI was terminated: " + result.ErrorCode); return; }
            var until = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(plan.Spec.Behavior.ShutdownTimeoutSeconds, 1, 600));
            while (!platform.HasExited(process) && DateTimeOffset.UtcNow < until) await Task.Delay(100).ConfigureAwait(false);
            trace.Write(platform.HasExited(process) ? "COOPERATIVE_EXIT_COMPLETED" : "COOPERATIVE_EXIT_TIMEOUT");
            if (!platform.HasExited(process)) session.AddDiagnostic("OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE", "OMSI did not exit within ShutdownTimeoutSeconds and was terminated.");
        }
        catch (Exception exception) when (exception is InvalidOperationException or TimeoutException or IOException)
        {
            trace.Write("COOPERATIVE_EXIT_FAILED", exception.Message);
            session.AddDiagnostic("OL_W_COOPERATIVE_SHUTDOWN_UNAVAILABLE", "OMSI was terminated: " + exception.Message);
        }
    }

    private static void StopPrograms(ModuleSessionPrograms programs, LiveSession session, HostTrace trace)
    {
        try { programs.Stop(); trace.Write("MODULE_SESSION_PROGRAMS_STOPPED"); }
        catch (Exception exception) { trace.Write("MODULE_SESSION_PROGRAMS_STOP_FAILED", exception.ToString()); session.AddDiagnostic(PublicErrorCodes.MODULE_CLEANUP_INCOMPLETE, "Module session programs: " + exception.Message); }
    }

    private async Task SuperviseAsync(LiveSession session, SessionPlan plan, LaunchedProcess process, InstallationLease lease, FileConfigurationTransaction transaction, CurrentStartupHandoffStore handoff, CurrentTelemetryStore telemetry, HostTrace trace, ModuleSessionPrograms programs)
    {
        var processExited = false;
        try
        {
            trace.Write("SUPERVISOR_ENTER");
            var deadline = DateTimeOffset.UtcNow.AddSeconds(plan.Spec.Behavior.StartupTimeoutSeconds);
            while (!platform.HasExited(process) && !session.StopRequested)
            {
                var (events, lost) = telemetry.ReadNew();
                if (lost > 0)
                {
                    trace.Write("TELEMETRY_LOST", lost.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    session.AddRuntimeEvent("telemetry.lost", new Dictionary<string, string>(StringComparer.Ordinal) { ["count"] = lost.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                }
                foreach (var (_, payload) in events)
                {
                    if (string.IsNullOrWhiteSpace(payload)) continue;
                    trace.Write("TELEMETRY_RECEIVED", payload);
                    ApplyTelemetry(session, payload);
                    if (session.State == SessionState.Failed) break;
                }
                if (session.State == SessionState.Failed) break;
                if (session.State != SessionState.Running && DateTimeOffset.UtcNow >= deadline)
                {
                    session.Fail(session.PluginStarted ? "OL_E_STARTUP_TIMEOUT" : "OL_E_PLUGIN_NOT_LOADED", "The requested semantic startup state was not reached before the timeout.");
                    break;
                }
                await Task.Delay(100).ConfigureAwait(false);
            }
            if (platform.HasExited(process) && session.State != SessionState.Running && session.State != SessionState.Failed) session.Fail("OL_E_PROCESS_EXITED_EARLY", "OMSI exited before gameplay was entered.");
            if (!platform.HasExited(process) && session.StopRequested && session.State == SessionState.Running && plan.Spec.Behavior.CooperativeShutdown)
                await CooperativeExitAsync(session, plan, process, trace).ConfigureAwait(false);
            if (!platform.HasExited(process)) platform.Terminate(process);
            await platform.WaitForExitAsync(process, CancellationToken.None).ConfigureAwait(false); processExited = true; await transaction.MarkStateAsync(TransactionState.ProcessExited).ConfigureAwait(false); session.Move(SessionState.ProcessExited);
        }
        catch (Exception exception)
        {
            trace.Write("SUPERVISOR_FAULT", exception.ToString()); session.Fail("OL_E_PROCESS_SUPERVISION", exception.Message);
            try
            {
                if (!platform.HasExited(process)) platform.Terminate(process);
                await platform.WaitForExitAsync(process, CancellationToken.None).ConfigureAwait(false);
                processExited = true;
                await transaction.MarkStateAsync(TransactionState.ProcessExited, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception cleanupException)
            {
                trace.Write("SUPERVISOR_PROCESS_CLEANUP_FAILED", cleanupException.ToString());
                session.AddDiagnostic("OL_E_PROCESS_CLEANUP_FAILED", cleanupException.Message);
            }
        }
        finally
        {
            // Session programs end after OMSI and before the restore.
            StopPrograms(programs, session, trace);
            try
            {
                if (!processExited)
                {
                    trace.Write("SUPERVISOR_RESTORE_DEFERRED_PROCESS_ALIVE");
                    session.Fail("OL_E_RESTORE_DEFERRED", "OMSI process exit was not confirmed; recovery remains pending.");
                }
                else { session.Move(SessionState.Restoring); await transaction.RestoreAsync(CancellationToken.None).ConfigureAwait(false); AddRestoreNotes(session, transaction); session.Move(SessionState.CleaningRuntime); }
            }
            catch (Exception exception) { trace.Write("SUPERVISOR_RESTORE_FAILED", exception.ToString()); session.Fail("OL_E_RESTORE_FAILED", exception.Message); }
            finally { trace.Write("PROCESS_HANDLE_DISPOSE"); process.Dispose(); handoff.Dispose(); telemetry.Dispose(); session.DisposeRuntime(); lease.Dispose(); if (session.State != SessionState.Failed) session.Move(SessionState.Completed); trace.Write("SUPERVISOR_COMPLETE", session.State.ToString()); }
        }
    }

    private static void ApplyTelemetry(LiveSession session, string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            var name = root.GetProperty("name").GetString();
            if (!string.IsNullOrWhiteSpace(name))
            {
                var data = root.TryGetProperty("data", out var rawData) && rawData.ValueKind == JsonValueKind.Object
                    ? rawData.EnumerateObject().ToDictionary(property => property.Name, property => property.Value.ToString(), StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
                session.AddRuntimeEvent(name, data, root.TryGetProperty("tick", out var tick) && tick.ValueKind == JsonValueKind.Number && tick.TryGetInt64(out var ticks) ? ticks : null);
            }
            switch (name)
            {
                case "plugin.started": session.Move(SessionState.PluginBootstrap); break;
                case "world.starting": session.Move(SessionState.StartingWorld); break;
                case "gameplay.entered": session.Move(SessionState.Running); break;
                case "plugin.handoff.invalid": session.Fail("OL_E_PLUGIN_PROTOCOL_MISMATCH", json); break;
                case "plugin.build.invalid": session.Fail("OL_E_BUILD_VALIDATION_FAILED", json); break;
                // Beta 4: the requested world mode needs a native capability
                // whose targets did not resolve and verify on this OMSI build.
                case "plugin.capability.unresolved": session.Fail("OL_E_CAPABILITY_UNRESOLVED", json); break;
                case "headless.arm.failed": session.Fail("OL_E_HEADLESS_ARM_FAILED", json); break;
                case "plugin.request.unsupported": session.Fail("OL_E_CAPABILITY_UNAVAILABLE", json); break;
                // A typed code from the plugin (for example OL_E_DATE_TIME_APPLY_FAILED) is kept.
                case "world.failed": session.Fail(root.TryGetProperty("data", out var failure) && failure.TryGetProperty("code", out var failureCode) && failureCode.GetString() is { } typed && PublicErrorCodes.FromMessage(typed) == typed ? typed : "OL_E_WORLD_START_FAILED", json); break;
                case "world.situation.failed": session.Fail("OL_E_SITUATION_LOAD_FAILED", json); break;
                // Runtime modules: a failure is a status diagnostic; a module
                // whose failure_policy is fail-session fails the session.
                case "module.failed" or "module.load.failed": session.AddDiagnostic(ModuleDiagnostic(root, PublicErrorCodes.MODULE_PHASE_FAILED)); break;
                case "module.cleanup" when root.TryGetProperty("data", out var cleanup) && cleanup.TryGetProperty("result", out var result) && result.GetString() == "incomplete": session.AddDiagnostic(ModuleDiagnostic(root, PublicErrorCodes.MODULE_CLEANUP_INCOMPLETE)); break;
                case "module.session-failure": session.Fail(PublicErrorCodes.MODULE_SESSION_FAILED, json); break;
            }
        }
        catch (JsonException) { session.Fail("OL_E_PLUGIN_PROTOCOL_MISMATCH", "Invalid plugin telemetry."); }
    }
    // A planned module's effective settings for the handoff: a JSON object of
    // strings, empty when the module declares none.
    private static string ModuleSettingsJson(PlannedModule module) =>
        module.EffectiveSettings.Count == 0 ? string.Empty : JsonSerializer.Serialize(module.EffectiveSettings.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
    private static LaunchDiagnostic ModuleDiagnostic(JsonElement root, string fallbackCode)
    {
        var data = root.TryGetProperty("data", out var raw) && raw.ValueKind == JsonValueKind.Object
            ? raw.EnumerateObject().ToDictionary(x => x.Name, x => x.Value.ToString(), StringComparer.Ordinal)
            : new Dictionary<string, string>(StringComparer.Ordinal);
        var code = data.TryGetValue("code", out var value) && PublicErrorCodes.FromMessage(value) == value ? value : fallbackCode;
        return new(code, (data.TryGetValue("module", out var module) ? module + ": " : "") + (data.TryGetValue("detail", out var detail) ? detail : ""), data);
    }
    private static IReadOnlyDictionary<string, byte[]> BuildTransactionalOverlays(LaunchSpec spec, SessionVisualAssets.Plan? visualPlan = null, HofPlanner.Plan? hofPlan = null)
    {
        var overlays = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);
        foreach (var visual in (visualPlan ?? SessionVisualAssets.Build(spec)).Overlays) overlays.Add(visual.Key, visual.Value);
        foreach (var hof in (hofPlan ?? HofPlanner.Build(spec)).Overlays)
            if (!overlays.TryAdd(hof.Key, hof.Value)) throw new InvalidOperationException("OL_E_HOF_CONFLICT: " + hof.Key + " is also written by the session presentation.");
        foreach (var document in InputDocuments.Load(spec, out var inputError))
            if (!overlays.TryAdd(document.Target, document.Bytes)) throw new InvalidOperationException("OL_E_INVALID_ARGUMENT: " + document.Target + " is also written by another part of the session.");
        var settings = spec.Environment.General.Concat(spec.Environment.Advanced).Concat(spec.Environment.Graphics)
            .Concat(spec.Environment.AdvancedGraphics).Concat(spec.Environment.Sound).Concat(spec.Environment.AiPassengers)
            .Concat(spec.Environment.Keyboard).Concat(spec.Environment.Controllers).Where(x => x.Value.IsSet).ToArray();
        foreach (var group in settings.GroupBy(x => ConfigurationCatalog.TryGet(x.Key, out var setting) ? setting.FileName : throw new InvalidOperationException("OL_E_UNKNOWN_SETTING: " + x.Key), StringComparer.OrdinalIgnoreCase))
        {
            var path = Path.Combine(spec.Installation.RootPath, group.Key);
            var bytes = overlays.TryGetValue(group.Key, out var staged) ? staged : File.Exists(path) ? File.ReadAllBytes(path) : Array.Empty<byte>();
            foreach (var item in group)
            {
                var setting = ConfigurationCatalog.TryGet(item.Key, out var known) ? known : throw new InvalidOperationException("OL_E_UNKNOWN_SETTING: " + item.Key);
                if (!setting.Writable) throw new InvalidOperationException("OL_E_SETTING_NOT_WRITABLE: " + item.Key);
                bytes = ConfigurationCatalog.CreatePatch(item.Key, item.Value.Value!).Apply(bytes);
            }
            overlays[group.Key] = bytes;
        }
        return overlays;
    }
    private LiveSession Get(SessionHandle handle) => sessions.TryGetValue(handle.SessionId, out var value) ? value : throw new KeyNotFoundException("Unknown OmsiLaunch session.");
    private static void AddRestoreNotes(LiveSession session, FileConfigurationTransaction transaction) { foreach (var note in transaction.RestoreNotes) session.AddDiagnostic(ToDiagnostic(note)); }
    private static string HashFile(string path) { using var stream = File.OpenRead(path); using var algorithm = System.Security.Cryptography.SHA256.Create(); return Convert.ToHexString(algorithm.ComputeHash(stream)); }
    private static void RemoveStaleClosecheck(string root, LiveSession session)
    {
        var path = Path.Combine(root, "closecheck");
        if (!File.Exists(path)) return;
        var bytes = File.ReadAllBytes(path);
        session.AddDiagnostic("closecheck.stale-removed", Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(bytes)));
        File.Delete(path);
        if (File.Exists(path)) throw new IOException("OL_E_CLOSECHECK_REMOVE_FAILED");
    }

    private sealed class LiveSession
    {
        private readonly object gate = new(); private readonly List<LaunchDiagnostic> diagnostics = new(); private readonly List<RuntimeEvent> runtimeEvents = new(); private long eventSequence; private CurrentRuntimeCommandStore? runtime; public Guid Id { get; } public SessionState State { get; private set; } = SessionState.Created; public bool StopRequested { get; private set; } public bool PluginStarted { get; private set; }
        public bool IsTerminal { get { lock (gate) return State is SessionState.Completed or SessionState.Failed; } } public Task? LifecycleTask { get; set; }
        public LiveSession(Guid id) => Id = id;
        public IReadOnlyList<PlannedModule> PlannedModules { get; set; } = Array.Empty<PlannedModule>();
        public void Attach(LaunchedProcess process) { lock (gate) diagnostics.Add(new("process.started", process.ProcessId.ToString(), new Dictionary<string, string> { ["thread_id"] = process.ThreadId.ToString(), ["creation_utc"] = process.Identity.CreationTimeUtc.ToString("O") })); }
        public void AddDiagnostic(string code, string message) { lock (gate) diagnostics.Add(new(code, message)); }
        public void AddDiagnostic(LaunchDiagnostic diagnostic) { lock (gate) diagnostics.Add(diagnostic); }
        public void AddRuntimeEvent(string type, IReadOnlyDictionary<string, string> data, long? tick = null) { lock (gate) { runtimeEvents.Add(new(type, DateTimeOffset.UtcNow, ++eventSequence, data, tick)); if (runtimeEvents.Count > 256) runtimeEvents.RemoveAt(0); } }
        public void Move(SessionState state) { lock (gate) { if (State != SessionState.Failed && State != SessionState.Completed) { State = state; if (state == SessionState.PluginBootstrap) PluginStarted = true; } } }
        public void Fail(string code, string message) { lock (gate) { diagnostics.Add(new(code, message)); State = SessionState.Failed; } }
        public void RequestStop() { lock (gate) StopRequested = true; }
        public void AttachRuntime(CurrentRuntimeCommandStore value) { lock (gate) runtime = value; }
        public Task<RuntimeCommandResult> RequestRuntimeAsync(RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken) { lock (gate) return runtime?.RequestAsync(command, timeout, cancellationToken) ?? Task.FromException<RuntimeCommandResult>(new InvalidOperationException("OL_E_RUNTIME_CHANNEL_CLOSED")); }
        public void DisposeRuntime() { lock (gate) { runtime?.Dispose(); runtime = null; } }
        public SessionStatus Status() { lock (gate) return new SessionStatus(Id, State, diagnostics.ToArray(), runtimeEvents.ToArray()); }
    }
}
