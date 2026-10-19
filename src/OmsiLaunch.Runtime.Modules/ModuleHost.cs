using System.Globalization;
using OmsiLaunch.Api;

namespace OmsiLaunch.Runtime.Modules;

// What the in-process host needs from the bridge. The plugin implements it
// over OMSI; tests implement it with fakes.
public interface IModuleHostEnvironment
{
    Guid SessionId { get; }
    string InstallationRoot { get; }
    string ProductVersion { get; }
    // The OMSI product version the game reports about itself (2.3.004 on the
    // reference build, never its PE file version); "unknown" when uncertain.
    string OmsiVersion { get; }
    // Runs a catalog operation in process, on the UI thread (the module host
    // has already checked the module's declaration).
    ModuleCallResult ExecuteRuntimeOperation(string operation, IReadOnlyDictionary<string, string>? arguments);
    // Keyboard and mouse capture (the host arbitrates between modules; this is
    // the one hook). SetInputCapture returns false when it cannot start;
    // DrainInput appends the captured events and returns how many.
    bool InputAvailable { get; }
    bool SetInputCapture(bool enabled);
    int DrainInput(List<ModuleInputEvent> events);
    // True when an OMSI plugin DLL with this file name is loaded in the
    // process (a module's conflicts.plugins).
    bool IsPluginLibraryLoaded(string fileName);
    bool IsCapabilityAvailable(string capability);
    // The map OMSI actually loaded (maps\<folder>\global.cfg), when known.
    string? LoadedMapIdentity { get; }
    long TickMilliseconds { get; }
    void Emit(string name, IReadOnlyDictionary<string, string> data);
    IRuntimeModule CreateModule(ModuleManifest manifest, string directory);
    // Loads a native component and calls its start export (hooks may be
    // requested during start and gameplay-ready only). values answers the
    // component's get_value requests (see ModuleHost.NativeValue).
    // runtime runs a catalog operation for the component (run_operation); the
    // host has already checked the module's declaration.
    IModuleNativeComponent StartNativeComponent(ModuleManifest manifest, string directory, ModuleComponent component, Func<string, string?> values, Func<string, IReadOnlyDictionary<string, string>?, ModuleCallResult> runtime);
    // The program is created suspended, placed in the session's job object
    // (kill on close, with the options' priority and memory limit) and only then
    // resumed, so a child it starts at once cannot escape the job.
    IModuleAuxiliaryProcess StartAuxiliaryProcess(string path, string arguments, ModuleAuxiliaryOptions options);
}

// A module the controller planned: its directory, the SHA-256 of the
// module.json it evaluated (re-verified in process) and its effective
// settings (a declared key the request lacks gets the manifest default).
// SnapshotSha256 (bridge protocol 3) is the digest of the module's files taken
// at planning; empty or null when the plan carried none.
public sealed record ModuleLoadRequest(string Directory, string ManifestSha256, IReadOnlyDictionary<string, string>? Settings = null, string? SnapshotSha256 = null);

public enum ModuleState : byte { Loaded, Armed, Ready, Degraded, Disabled, Failed, Stopped }
public sealed record ModuleCallResult(bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string> Values);
public sealed record ModuleStatusSnapshot(string Id, ModuleState State, string? LastErrorCode, int TrackedResources);

// Lifecycle steps 4 to 8, in process. Any failure applies the module's
// failure_policy and releases everything the module holds:
// - fail-session: the module is released and the session is failed (event
//   module.session-failure; the controller stops the session);
// - disable-module: the module is released, the session continues;
// - degrade: a failed operation leaves the module active (reported once per
//   code); a failed lifecycle phase behaves like disable-module.
public sealed class ModuleHost
{
    private readonly IModuleHostEnvironment environment;
    private readonly List<Entry> entries = new();
    public ModuleHost(IModuleHostEnvironment environment) => this.environment = environment;

    public IReadOnlyList<ModuleStatusSnapshot> Status => entries.Select(x => new ModuleStatusSnapshot(x.Manifest.Id, x.State, x.LastErrorCode, x.Ledger.Count)).ToArray();
    public bool SessionFailureRequested { get; private set; }

    // True when an armed module declares the capability (required or optional).
    public bool RequiresCapability(string capability) =>
        entries.Any(x => x.State is ModuleState.Armed or ModuleState.Ready or ModuleState.Degraded && (x.Manifest.RequiredCapabilities.Contains(capability) || x.Manifest.OptionalCapabilities.Contains(capability)));

    public void Load(IEnumerable<ModuleLoadRequest> requests)
    {
        foreach (var request in requests)
        {
            var discovered = ModuleCatalog.InspectPlanned(request.Directory, request.ManifestSha256, request.SnapshotSha256);
            if (discovered.Manifest is not { } manifest) { Emit("module.load.failed", ("module", discovered.DirectoryName), ("code", discovered.Code ?? PublicErrorCodes.MODULE_MANIFEST_INVALID), ("detail", discovered.Message ?? "")); continue; }
            if (discovered.Code is not null) { Reject(manifest, discovered.Code, discovered.Message ?? ""); continue; }

            var missing = manifest.RequiredCapabilities.Where(x => !environment.IsCapabilityAvailable(x)).ToArray();
            if (missing.Length != 0) { Reject(manifest, PublicErrorCodes.MODULE_CAPABILITY_UNRESOLVED, "unresolved in process: " + string.Join(",", missing)); continue; }
            // A conflicting OMSI plugin is loaded in OMSI (the plan checks the
            // .opl files; this also catches one that was started another way).
            var conflicting = manifest.EffectiveConflictingPlugins.Where(environment.IsPluginLibraryLoaded).ToArray();
            if (conflicting.Length != 0) { Reject(manifest, PublicErrorCodes.MODULE_CONFLICT, "loaded next to the conflicting plugin " + string.Join(", ", conflicting)); continue; }
            IRuntimeModule instance;
            try { instance = environment.CreateModule(manifest, request.Directory) ?? throw new InvalidOperationException("the loader returned no module"); }
            catch (Exception exception) { Reject(manifest, PublicErrorCodes.MODULE_LOAD_FAILED, exception.Message); continue; }
            var settings = manifest.EffectiveSettings.ToDictionary(x => x.Key, x => request.Settings is not null && request.Settings.TryGetValue(x.Key, out var value) ? value : x.Default, StringComparer.Ordinal);
            var entry = new Entry(manifest, request.Directory, instance, settings) { SnapshotSha256 = request.SnapshotSha256 };
            entries.Add(entry);
            SetState(entry, ModuleState.Loaded, "load");
        }
    }

    public void Arm()
    {
        foreach (var entry in entries.Where(x => x.State == ModuleState.Loaded).ToArray())
            if (RunPhase(entry, "arm", entry.Manifest.Timeouts.ArmMilliseconds, () => ArmEntry(entry))) SetState(entry, ModuleState.Armed, "arm");
    }

    public void GameplayReady()
    {
        foreach (var entry in entries.Where(x => x.State == ModuleState.Armed).ToArray())
        {
            // In-process content binding: the map OMSI actually loaded.
            if (entry.Manifest.ContentBinding is { } binding && environment.LoadedMapIdentity is { } loaded && !string.Equals(binding.Map, loaded, StringComparison.OrdinalIgnoreCase))
            {
                Fail(entry, "gameplay-ready", PublicErrorCodes.MODULE_CONTENT_BINDING, "the module is bound to " + binding.Map + ", OMSI loaded " + loaded);
                continue;
            }
            if (RunPhase(entry, "gameplay-ready", entry.Manifest.Timeouts.GameplayReadyMilliseconds, () => { entry.Native?.GameplayReady(); entry.Instance.GameplayReady(); })) SetState(entry, ModuleState.Ready, "gameplay-ready");
        }
    }

    // The native component starts first (it is tracked first, so it is
    // released last, after everything the managed component holds).
    private void ArmEntry(Entry entry)
    {
        if (entry.Manifest.Components.FirstOrDefault(x => x.Kind == ModuleComponentKind.Native) is { } native)
        {
            if (ModuleCatalog.VerifyComponent(entry.Directory, entry.Manifest, native, entry.SnapshotSha256) is { } failure) throw new ModuleOperationException(PublicErrorCodes.MODULE_INTEGRITY, failure);
            var component = environment.StartNativeComponent(entry.Manifest, entry.Directory, native, name => NativeValue(entry, name), (operation, arguments) => ExecuteRuntime(entry, operation, arguments));
            entry.Native = component;
            entry.Ledger.Add(("native component", component));
        }
        entry.Instance.Arm(new Context(this, entry));
    }

    // What a native component reads through get_value: "setting.<key>",
    // "data_directory", "module_directory", "session_id", "module_version",
    // "product_version", "omsi_version" and "session_channel" (only for a module
    // with a host program). Null when the name is unknown.
    private string? NativeValue(Entry entry, string name)
    {
        if (name.StartsWith("setting.", StringComparison.Ordinal)) return entry.Settings.TryGetValue(name["setting.".Length..], out var value) ? value : null;
        return name switch
        {
            "data_directory" => DataDirectory(entry),
            "module_directory" => entry.Directory,
            "session_id" => environment.SessionId.ToString("D"),
            "module_version" => entry.Manifest.Version,
            "product_version" => environment.ProductVersion,
            "omsi_version" => environment.OmsiVersion,
            "session_channel" => SessionChannel(entry),
            _ => null
        };
    }

    private string? SessionChannel(Entry entry) => entry.Manifest.HostComponent is null ? null : RuntimeModuleProtocol.SessionChannelName(environment.SessionId, entry.Manifest.Id);

    private string DataDirectory(Entry entry)
    {
        var directory = RuntimeModuleProtocol.DataDirectory(environment.InstallationRoot, entry.Manifest.Id);
        try { System.IO.Directory.CreateDirectory(directory); } catch (Exception exception) when (exception is IOException or UnauthorizedAccessException) { }
        return directory;
    }

    // Calls every module that asked for a tick and whose interval elapsed.
    // The plugin calls it from OMSI's 50 ms UI timer. A tick that throws or
    // overruns its interval is a failure of that module: under degrade it is
    // reported once per code and the module keeps running (the ticks it missed
    // are skipped, never queued); under the other policies the policy applies.
    public void Tick()
    {
        foreach (var entry in entries.Where(x => x.State is ModuleState.Ready or ModuleState.Degraded && x.Manifest.TickMilliseconds > 0).ToArray())
        {
            var interval = entry.Manifest.TickMilliseconds;
            var now = environment.TickMilliseconds;
            if (entry.LastTickMilliseconds is { } previous && now - previous < interval) continue;
            var elapsed = entry.LastTickMilliseconds is { } last ? now - last : interval;
            entry.LastTickMilliseconds = now;
            var instance = entry.Instance as ITickingModule;
            var native = entry.Native is { HasTick: true } component ? component : null;
            if (instance is null && native is null) continue;
            try
            {
                native?.Tick((uint)Math.Min(elapsed, uint.MaxValue));
                instance?.Tick(TimeSpan.FromMilliseconds(elapsed));
            }
            catch (Exception exception) { TickFailed(entry, PublicErrorCodes.MODULE_PHASE_FAILED, exception.Message); continue; }
            var took = environment.TickMilliseconds - now;
            if (took > interval)
            {
                entry.LastTickMilliseconds = environment.TickMilliseconds;
                TickFailed(entry, PublicErrorCodes.MODULE_PHASE_TIMEOUT, "tick took " + took.ToString(CultureInfo.InvariantCulture) + " ms (interval " + interval.ToString(CultureInfo.InvariantCulture) + " ms)");
            }
        }
    }

    private void TickFailed(Entry entry, string code, string detail)
    {
        if (entry.Manifest.FailurePolicy == ModuleFailurePolicy.Degrade) OperationFailed(entry, code, detail, null);
        else Fail(entry, "tick", code, detail);
    }

    // The catalog operations a module declared, executed in process.
    private ModuleCallResult ExecuteRuntime(Entry entry, string operation, IReadOnlyDictionary<string, string>? arguments)
    {
        if (!entry.Manifest.EffectiveRuntimeOperations.Contains(operation, StringComparer.Ordinal))
            return Error(PublicErrorCodes.MODULE_OPERATION_NOT_DECLARED, "module '" + entry.Manifest.Id + "' does not declare the runtime operation '" + operation + "'");
        // A module arms in the Loaded state (its native start and Arm run then), so it may already ask.
        if (entry.State is not (ModuleState.Loaded or ModuleState.Armed or ModuleState.Ready or ModuleState.Degraded) || entry.Stopping.IsCancellationRequested)
            return Error(PublicErrorCodes.MODULE_DISABLED, "module '" + entry.Manifest.Id + "' is " + entry.State.ToString().ToLowerInvariant());
        var validation = PublicCapabilityRegistry.ValidateRuntimeArguments(operation, arguments);
        if (!validation.Accepted) return Error(validation.ErrorCode ?? PublicErrorCodes.RUNTIME_ARGUMENT_REQUIRED, validation.Message ?? "invalid arguments");
        ModuleCallResult result;
        try { result = environment.ExecuteRuntimeOperation(operation, arguments); }
        catch (Exception exception) { return Error(PublicErrorCodes.RUNTIME_OPERATION_FAILED, exception.Message); }
        var values = result.Values.Where(x => !PublicCapabilityRegistry.IsInternalResultKey(x.Key)).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        var size = values.Sum(x => (long)x.Key.Length + x.Value.Length);
        if (size > RuntimeModuleProtocol.MaxRuntimeResultBytes) return Error(PublicErrorCodes.RUNTIME_RESPONSE_TOO_LARGE, "the result of " + operation + " is larger than " + RuntimeModuleProtocol.MaxRuntimeResultBytes.ToString(CultureInfo.InvariantCulture) + " bytes");
        return new(result.Succeeded, result.ErrorCode, values);
    }

    public ModuleCallResult Execute(string operation, IReadOnlyDictionary<string, string>? arguments)
    {
        if (!RuntimeModuleProtocol.TrySplitOperation(operation, out var id, out var name)) return Error(PublicErrorCodes.MODULE_OPERATION_UNKNOWN, "not a module operation");
        var entry = entries.FirstOrDefault(x => x.Manifest.Id == id);
        if (entry is null) return Error(PublicErrorCodes.MODULE_NOT_LOADED, "module '" + id + "' is not loaded in this session");
        if (entry.State is ModuleState.Disabled or ModuleState.Failed or ModuleState.Stopped) return Error(PublicErrorCodes.MODULE_DISABLED, "module '" + id + "' is " + entry.State.ToString().ToLowerInvariant());
        if (entry.State is not ModuleState.Ready and not ModuleState.Degraded) return Error(PublicErrorCodes.MODULE_NOT_LOADED, "module '" + id + "' is not ready");
        var descriptor = entry.Manifest.Operations.FirstOrDefault(x => x.Id == name);
        if (descriptor is null) return Error(PublicErrorCodes.MODULE_OPERATION_UNKNOWN, "module '" + id + "' declares no operation '" + name + "'");
        var values = arguments ?? new Dictionary<string, string>();
        var missing = descriptor.Arguments.Where(x => x.Required && (!values.TryGetValue(x.Name, out var value) || string.IsNullOrWhiteSpace(value))).Select(x => x.Name).ToArray();
        if (missing.Length != 0) return Error(PublicErrorCodes.RUNTIME_ARGUMENT_REQUIRED, "requires: " + string.Join(", ", missing));
        var unknown = values.Keys.Where(key => descriptor.Arguments.All(x => x.Name != key)).ToArray();
        if (unknown.Length != 0) return Error(PublicErrorCodes.RUNTIME_ARGUMENT_REQUIRED, "undeclared arguments: " + string.Join(", ", unknown));

        IReadOnlyDictionary<string, string>? output = null;
        var started = environment.TickMilliseconds;
        try { output = entry.Instance.Execute(name, values); }
        catch (ModuleOperationException exception)
        {
            // A module's typed code crosses the boundary only when it is a
            // catalogued public code.
            var code = PublicErrorCodes.FromMessage(exception.Code) == exception.Code ? exception.Code : PublicErrorCodes.MODULE_OPERATION_FAILED;
            return OperationFailed(entry, code, exception.Message, exception.Code);
        }
        catch (Exception exception) { return OperationFailed(entry, PublicErrorCodes.MODULE_OPERATION_FAILED, exception.Message, null); }
        if (environment.TickMilliseconds - started > descriptor.TimeoutMilliseconds)
            return OperationFailed(entry, PublicErrorCodes.MODULE_PHASE_TIMEOUT, "operation '" + name + "' took longer than " + descriptor.TimeoutMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms", null);
        var result = (output ?? new Dictionary<string, string>()).Where(x => !PublicCapabilityRegistry.IsInternalResultKey(x.Key)).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        return new(true, null, result);
    }

    // Lifecycle steps 7 and 8 for every module still holding anything.
    public void Stop()
    {
        foreach (var entry in Enumerable.Reverse(entries).ToArray())
        {
            if (entry.State is ModuleState.Stopped or ModuleState.Disabled or ModuleState.Failed) continue;
            // A failing Stop is reported; the module still ends stopped and
            // released.
            if (!RunPhase(entry, "stop", entry.Manifest.Timeouts.StopMilliseconds, entry.Instance.Stop, applyPolicy: false)) entry.LastErrorCode ??= PublicErrorCodes.MODULE_PHASE_FAILED;
            Cleanup(entry);
            SetState(entry, ModuleState.Stopped, "stop");
        }
    }

    private bool RunPhase(Entry entry, string phase, int budget, Action action, bool applyPolicy = true)
    {
        var started = environment.TickMilliseconds;
        try { action(); }
        catch (Exception exception)
        {
            if (applyPolicy) Fail(entry, phase, PublicErrorCodes.MODULE_PHASE_FAILED, exception.Message);
            else Emit("module.failed", ("module", entry.Manifest.Id), ("phase", phase), ("code", PublicErrorCodes.MODULE_PHASE_FAILED), ("policy", ModuleEligibility.Policy(entry.Manifest.FailurePolicy)), ("detail", exception.Message));
            return false;
        }
        var elapsed = environment.TickMilliseconds - started;
        if (elapsed <= budget) return true;
        var detail = phase + " took " + elapsed.ToString(CultureInfo.InvariantCulture) + " ms (budget " + budget.ToString(CultureInfo.InvariantCulture) + " ms)";
        if (applyPolicy) Fail(entry, phase, PublicErrorCodes.MODULE_PHASE_TIMEOUT, detail);
        else Emit("module.failed", ("module", entry.Manifest.Id), ("phase", phase), ("code", PublicErrorCodes.MODULE_PHASE_TIMEOUT), ("policy", ModuleEligibility.Policy(entry.Manifest.FailurePolicy)), ("detail", detail));
        return false;
    }

    private ModuleCallResult OperationFailed(Entry entry, string code, string detail, string? moduleCode)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal) { ["detail"] = detail };
        if (moduleCode is not null && moduleCode != code) values["module_code"] = moduleCode;
        if (entry.Manifest.FailurePolicy == ModuleFailurePolicy.Degrade)
        {
            if (entry.DegradedCodes.Add(code)) Emit("module.degraded", ("module", entry.Manifest.Id), ("code", code), ("detail", detail));
            SetState(entry, ModuleState.Degraded, "runtime");
        }
        else Fail(entry, "runtime", code, detail);
        entry.LastErrorCode = code;
        return new(false, code, values);
    }

    private void Fail(Entry entry, string phase, string code, string detail)
    {
        entry.LastErrorCode = code;
        var policy = entry.Manifest.FailurePolicy;
        Emit("module.failed", ("module", entry.Manifest.Id), ("phase", phase), ("code", code), ("policy", ModuleEligibility.Policy(policy)), ("detail", detail));
        // The module gets a chance to stop, then everything tracked is released.
        try { entry.Instance.Stop(); } catch (Exception exception) { Emit("module.stop.failed", ("module", entry.Manifest.Id), ("detail", exception.Message)); }
        Cleanup(entry);
        SetState(entry, policy == ModuleFailurePolicy.FailSession ? ModuleState.Failed : ModuleState.Disabled, phase);
        if (policy == ModuleFailurePolicy.FailSession)
        {
            SessionFailureRequested = true;
            Emit("module.session-failure", ("module", entry.Manifest.Id), ("code", code), ("phase", phase));
        }
    }

    private void Reject(ModuleManifest manifest, string code, string detail)
    {
        Emit("module.load.failed", ("module", manifest.Id), ("code", code), ("policy", ModuleEligibility.Policy(manifest.FailurePolicy)), ("detail", detail));
        if (manifest.FailurePolicy != ModuleFailurePolicy.FailSession) return;
        SessionFailureRequested = true;
        Emit("module.session-failure", ("module", manifest.Id), ("code", code), ("phase", "load"));
    }

    // Releases the module's tracked resources in reverse order and verifies
    // nothing is left (auxiliary processes exited, background threads ended).
    private void Cleanup(Entry entry)
    {
        entry.Stopping.Cancel();
        var incomplete = new List<string>();
        for (var index = entry.Ledger.Count - 1; index >= 0; index--)
        {
            var (kind, resource) = entry.Ledger[index];
            try { resource.Dispose(); }
            catch (Exception exception) { incomplete.Add(kind + ": " + exception.Message); continue; }
            if (resource is IReleaseVerifiable verifiable && !verifiable.Released) incomplete.Add(kind + ": " + verifiable.Description + " is still alive");
        }
        entry.Ledger.Clear();
        if (incomplete.Count == 0) Emit("module.cleanup", ("module", entry.Manifest.Id), ("result", "complete"));
        else Emit("module.cleanup", ("module", entry.Manifest.Id), ("result", "incomplete"), ("code", PublicErrorCodes.MODULE_CLEANUP_INCOMPLETE), ("detail", string.Join("; ", incomplete)));
    }

    private void SetState(Entry entry, ModuleState state, string phase)
    {
        entry.State = state;
        Emit("module.state", ("module", entry.Manifest.Id), ("state", state.ToString().ToLowerInvariant()), ("phase", phase));
    }

    private static ModuleCallResult Error(string code, string detail) => new(false, code, new Dictionary<string, string>(StringComparer.Ordinal) { ["detail"] = detail });
    private void Emit(string name, params (string Key, string Value)[] values) => environment.Emit(name, values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));

    private interface IReleaseVerifiable { bool Released { get; } string Description { get; } }

    private sealed class Entry
    {
        public Entry(ModuleManifest manifest, string directory, IRuntimeModule instance, IReadOnlyDictionary<string, string> settings) { Manifest = manifest; Directory = directory; Instance = instance; Settings = settings; }
        public ModuleManifest Manifest { get; }
        public IReadOnlyDictionary<string, string> Settings { get; }
        public string Directory { get; }
        public IRuntimeModule Instance { get; }
        public ModuleState State { get; set; }
        public string? LastErrorCode { get; set; }
        public List<(string Kind, IDisposable Resource)> Ledger { get; } = new();
        public CancellationTokenSource Stopping { get; } = new();
        public HashSet<string> DegradedCodes { get; } = new(StringComparer.Ordinal);
        public IModuleNativeComponent? Native { get; set; }
        public long? LastTickMilliseconds { get; set; }
        public string? SnapshotSha256 { get; set; }
    }

    private sealed class Context : IRuntimeModuleContext
    {
        private readonly ModuleHost host; private readonly Entry entry;
        public Context(ModuleHost host, Entry entry) { this.host = host; this.entry = entry; Runtime = new RuntimeApi(host, entry); Content = new ContentApi(host, entry); Input = new InputApi(host, entry); }
        public Guid SessionId => host.environment.SessionId;
        public string ModuleId => entry.Manifest.Id;
        public string ModuleDirectory => entry.Directory;
        public IReadOnlyDictionary<string, string> Settings => entry.Settings;
        public string DataDirectory => host.DataDirectory(entry);
        public string ProductVersion => host.environment.ProductVersion;
        public string OmsiVersion => host.environment.OmsiVersion;
        public IModuleRuntime Runtime { get; }
        public IModuleNativeComponent? NativeComponent => entry.Native;
        public IModuleContent Content { get; }
        public IModuleInput Input { get; }
        public string? SessionChannelName => host.SessionChannel(entry);
        public CancellationToken Stopping => entry.Stopping.Token;
        public bool IsCapabilityAvailable(string capability) => host.environment.IsCapabilityAvailable(capability);
        public void Emit(string name, IReadOnlyDictionary<string, string>? data = null)
        {
            if (!RuntimeModuleProtocol.IsOperationId(name)) throw new ArgumentException("event name is not valid", nameof(name));
            var values = data ?? new Dictionary<string, string>();
            host.environment.Emit("module." + entry.Manifest.Id + "." + name, name == ModuleNotices.EventName ? ModuleNotices.Sanitize(values) : values);
        }
        public void Track(string kind, IDisposable resource) => entry.Ledger.Add((kind, resource ?? throw new ArgumentNullException(nameof(resource))));
        public void RunBackground(string name, Action<CancellationToken> work)
        {
            var worker = new BackgroundWork(name, work, entry.Stopping.Token);
            entry.Ledger.Add(("thread", worker));
            worker.Start();
        }
        public IModuleAuxiliaryProcess StartAuxiliaryProcess(string componentPath, string arguments)
        {
            var normalized = componentPath.Replace('/', '\\');
            var component = entry.Manifest.Components.FirstOrDefault(x => x.Kind == ModuleComponentKind.Auxiliary && string.Equals(x.Path, normalized, StringComparison.OrdinalIgnoreCase))
                ?? throw new ModuleOperationException(PublicErrorCodes.MODULE_COMPONENT_UNSUPPORTED, componentPath + " is not an auxiliary component of this module");
            // Re-verify the pinned hash right before starting it.
            if (ModuleCatalog.VerifyComponent(entry.Directory, entry.Manifest, component, entry.SnapshotSha256) is { } failure) throw new ModuleOperationException(PublicErrorCodes.MODULE_INTEGRITY, failure);
            var process = host.environment.StartAuxiliaryProcess(Path.Combine(entry.Directory, component.Path), arguments, new ModuleAuxiliaryOptions(component.Priority, component.MemoryLimitMegabytes));
            entry.Ledger.Add(("process", new AuxiliaryProcessResource(process)));
            return process;
        }
    }

    // At most one module captures. The ledger holds a resource that releases
    // the capture, so a stop or a failure always gives the input back.
    private Entry? inputOwner;

    private sealed class InputApi : IModuleInput
    {
        private readonly ModuleHost host; private readonly Entry entry; private InputRelease? release;
        public InputApi(ModuleHost host, Entry entry) { this.host = host; this.entry = entry; }
        public bool IsAvailable => entry.Manifest.RequiredCapabilities.Concat(entry.Manifest.OptionalCapabilities).Contains("bridge.input") && host.environment.InputAvailable;
        public bool IsCapturing => host.inputOwner == entry;
        public bool Capture(bool enabled)
        {
            if (!enabled)
            {
                if (host.inputOwner != entry) return true;
                release?.Dispose();
                return true;
            }
            if (host.inputOwner == entry) return true;
            if (!IsAvailable || host.inputOwner is not null || entry.Stopping.IsCancellationRequested) return false;
            if (!host.environment.SetInputCapture(true)) return false;
            host.inputOwner = entry;
            release = new InputRelease(host, entry);
            entry.Ledger.Add(("input capture", release));
            return true;
        }
        public IReadOnlyList<ModuleInputEvent> Drain()
        {
            if (host.inputOwner != entry) return Array.Empty<ModuleInputEvent>();
            var events = new List<ModuleInputEvent>();
            host.environment.DrainInput(events);
            return events;
        }
    }

    private sealed class InputRelease : IDisposable
    {
        private readonly ModuleHost host; private readonly Entry entry; private bool released;
        public InputRelease(ModuleHost host, Entry entry) { this.host = host; this.entry = entry; }
        public void Dispose()
        {
            if (released) return;
            released = true;
            if (host.inputOwner == entry) { host.inputOwner = null; host.environment.SetInputCapture(false); }
        }
    }

    private sealed class ContentApi : IModuleContent
    {
        private readonly ModuleHost host; private readonly Entry entry;
        public ContentApi(ModuleHost host, Entry entry) { this.host = host; this.entry = entry; }
        public bool IsDeclared(ModuleContentInputKind kind) => entry.Manifest.EffectiveContentInputs.Any(x => x.Kind == kind);
        public ModuleContentResult LocateRealmapPackage(string mapIdentity) =>
            !IsDeclared(ModuleContentInputKind.RealmapPackage)
                ? new(false, null, PublicErrorCodes.MODULE_CONTENT_INPUT_UNDECLARED, "module '" + entry.Manifest.Id + "' does not declare the content input realmap-package")
                : RealmapPackage.Locate(host.environment.InstallationRoot, mapIdentity);
    }

    private sealed class RuntimeApi : IModuleRuntime
    {
        private readonly ModuleHost host; private readonly Entry entry;
        public RuntimeApi(ModuleHost host, Entry entry) { this.host = host; this.entry = entry; }
        public bool IsDeclared(string operation) => entry.Manifest.EffectiveRuntimeOperations.Contains(operation, StringComparer.Ordinal);
        public ModuleCallResult Execute(string operation, IReadOnlyDictionary<string, string>? arguments = null) => host.ExecuteRuntime(entry, operation, arguments);
    }

    private sealed class BackgroundWork : IDisposable, IReleaseVerifiable
    {
        private readonly Thread thread;
        public BackgroundWork(string name, Action<CancellationToken> work, CancellationToken stopping)
        {
            thread = new Thread(() => { try { work(stopping); } catch (OperationCanceledException) { } catch (Exception) { } }) { IsBackground = true, Name = "OmsiLaunch module: " + name };
            Description = "thread '" + name + "'";
        }
        public string Description { get; }
        public bool Released => !thread.IsAlive;
        public void Start() => thread.Start();
        public void Dispose() => thread.Join(TimeSpan.FromSeconds(2));
    }

    private sealed class AuxiliaryProcessResource : IDisposable, IReleaseVerifiable
    {
        private readonly IModuleAuxiliaryProcess process;
        public AuxiliaryProcessResource(IModuleAuxiliaryProcess process) { this.process = process; Description = "auxiliary process " + process.ProcessId.ToString(CultureInfo.InvariantCulture); }
        public string Description { get; }
        public bool Released => process.HasExited;
        public void Dispose() => process.Dispose();
    }
}
