using System.Globalization;
using System.Reflection;
using System.Text.Json;
using OmsiLaunch.Api;
using OmsiLaunch.Process;
using OmsiLaunch.Runtime.Modules;

namespace OmsiLaunch.Core;

// Session programs of the planned modules (components of kind "host", bridge
// protocol 2). Each starts before OMSI and receives its context as the first
// line on stdin; OMSI is started only after every program reported
// {"type":"ready"} or failed, or its prepare_ms passed. While the session runs
// a program may send {"type":"event"}, {"type":"notice"} and {"type":"error"}
// lines. At the end it receives {"type":"stop"} and has host_stop_ms to exit
// before it is ended. The controller never loads module code itself.
internal sealed class ModuleSessionPrograms
{
    public const string ContextSchema = "omsilaunch.module-session/v1";
    private readonly List<SessionProgram> programs = new();
    private readonly HashSet<string> excluded = new(StringComparer.Ordinal);
    private ModuleSessionPrograms() { }

    public static readonly ModuleSessionPrograms None = new();

    // Modules whose session program failed under disable-module: they are
    // left out of the startup handoff, so nothing of them runs in OMSI.
    public IReadOnlySet<string> Excluded => excluded;

    // Starts every planned module's session program and waits until each one
    // is prepared. A failure follows the module's failure_policy: fail-session
    // throws (the start fails), disable-module ends the program and excludes
    // the module, degrade keeps the module (and the program, if it still runs).
    public static ModuleSessionPrograms Start(SessionPlan plan, Action<string, IReadOnlyDictionary<string, string>> addEvent, Action<LaunchDiagnostic> addDiagnostic, Action<string, string> trace, CancellationToken cancellationToken)
    {
        var root = plan.Spec.Installation.RootPath;
        var result = new ModuleSessionPrograms();
        // A program started but not yet in result.programs when something throws
        // (a cancellation, an unexpected exception) would otherwise outlive the
        // failed start: the caller never receives `result` to stop it.
        SessionProgram? current = null;
        try
        {
        foreach (var planned in plan.EffectivePlannedModules.Where(x => x.IsEligible))
        {
            var directory = Path.Combine(root, RuntimeModuleProtocol.ModulesDirectory, planned.Id);
            var module = ModuleCatalog.InspectPlanned(directory, planned.ManifestSha256!, planned.SnapshotSha256);
            if (module.Manifest?.HostComponent is not { } host) continue;
            var manifest = module.Manifest;
            var program = new SessionProgram(planned, manifest, addEvent, addDiagnostic);
            current = program;
            try
            {
                if (module.Code is not null) throw new ModuleException(module.Code, module.Message ?? "the module is not usable");
                // The executable is started only as the file the plan recorded: its pin
                // when the author wrote one, and always the plan's snapshot.
                if (ModuleCatalog.VerifyComponent(directory, manifest, host, planned.SnapshotSha256) is { } failure) throw new ModuleException(PublicErrorCodes.MODULE_INTEGRITY, failure);
                var data = RuntimeModuleProtocol.DataDirectory(root, manifest.Id);
                Directory.CreateDirectory(data);
                program.Process = ModuleSessionProcess.Start(Path.GetFullPath(Path.Combine(directory, host.Path)), directory, plan.SessionId, program.OnLine);
                trace("MODULE_SESSION_PROGRAM_STARTED", manifest.Id + " pid=" + program.Process.ProcessId.ToString(CultureInfo.InvariantCulture));
                program.Emit("host.started", ("pid", program.Process.ProcessId.ToString(CultureInfo.InvariantCulture)));
                if (!program.Process.TryWriteLine(Context(plan, planned, manifest, directory, data))) throw new ModuleException(PublicErrorCodes.MODULE_LOAD_FAILED, "the session program did not accept its context");
                result.programs.Add(program);
            }
            catch (Exception exception) when (exception is ModuleException or IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
            {
                var code = exception is ModuleException typed ? typed.Code : PublicErrorCodes.MODULE_LOAD_FAILED;
                result.Fail(program, code, "session program: " + exception.Message, trace);
                // Under the degrade policy Fail keeps the module, but a program that was
                // started and never joined the list (its context was refused) has no owner.
                if (!result.programs.Contains(program)) program.End(0);
            }
            current = null;
        }
        result.WaitPrepared(cancellationToken, trace);
        return result;
        }
        catch
        {
            current?.End(0);
            result.Stop();
            throw;
        }
    }

    private void WaitPrepared(CancellationToken cancellationToken, Action<string, string> trace)
    {
        var started = Environment.TickCount64;
        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var waiting = programs.Where(x => !x.Done).ToArray();
            if (waiting.Length == 0) return;
            foreach (var program in waiting)
            {
                var elapsed = Environment.TickCount64 - started;
                if (program.Ready.IsSet)
                {
                    program.Done = true;
                    program.Emit("host.ready", ("prepare_ms", elapsed.ToString(CultureInfo.InvariantCulture)));
                    trace("MODULE_SESSION_PROGRAM_READY", program.Manifest.Id + " " + elapsed.ToString(CultureInfo.InvariantCulture) + "ms");
                }
                else if (program.Process!.HasExited)
                    Fail(program, PublicErrorCodes.MODULE_LOAD_FAILED, "the session program exited (code " + (program.Process.ExitCode?.ToString(CultureInfo.InvariantCulture) ?? "?") + ") before it was prepared", trace);
                else if (elapsed > program.Manifest.Timeouts.PrepareMilliseconds)
                    Fail(program, PublicErrorCodes.MODULE_PHASE_TIMEOUT, "the session program was not prepared within " + program.Manifest.Timeouts.PrepareMilliseconds.ToString(CultureInfo.InvariantCulture) + " ms", trace);
            }
            var pending = programs.Where(x => !x.Done).Select(x => x.Ready.WaitHandle).ToArray();
            if (pending.Length != 0) WaitHandle.WaitAny(pending, 100);
        }
    }

    private void Fail(SessionProgram program, string code, string detail, Action<string, string> trace)
    {
        program.Done = true;
        var policy = program.Manifest.FailurePolicy;
        trace("MODULE_SESSION_PROGRAM_FAILED", program.Manifest.Id + " " + code + " " + detail);
        program.Emit("host.failed", ("code", code), ("policy", ModuleEligibility.Policy(policy)), ("detail", detail));
        program.AddDiagnostic(new(code, program.Manifest.Id + ": " + detail, new Dictionary<string, string>(StringComparer.Ordinal) { ["module"] = program.Manifest.Id, ["phase"] = "prepare", ["policy"] = ModuleEligibility.Policy(policy) }));
        if (policy == ModuleFailurePolicy.Degrade) return;
        program.End(0);
        if (policy == ModuleFailurePolicy.FailSession)
        {
            Stop();
            throw new InvalidOperationException(PublicErrorCodes.MODULE_SESSION_FAILED + ": module " + program.Manifest.Id + ": " + detail);
        }
        excluded.Add(program.Manifest.Id);
    }

    // OMSI is running: the programs learn its process id (to check the
    // client of their channel).
    public void NotifyOmsiStarted(int processId)
    {
        foreach (var program in programs.Where(x => !x.Ended && x.Process is { HasExited: false }))
            program.Process!.TryWriteLine(JsonSerializer.Serialize(new Dictionary<string, object> { ["type"] = "omsi-started", ["pid"] = processId }));
    }

    // End of session (after OMSI exited, before the transaction is restored):
    // each program is asked to stop, gets host_stop_ms, then is ended.
    public void Stop()
    {
        foreach (var program in programs) if (!program.Ended && program.Process is { HasExited: false }) program.Process.TryWriteLine("{\"type\":\"stop\"}");
        foreach (var program in programs) program.End(program.Manifest.Timeouts.HostStopMilliseconds);
    }

    // The first line a session program receives.
    internal static string Context(SessionPlan plan, PlannedModule planned, ModuleManifest manifest, string directory, string data)
    {
        var world = plan.Spec.World;
        // The map of the session, whatever the start: a NEW_MAP and a LAST_MAP_STATE name it, a saved situation carries it (the planner resolved it).
        var map = world.MapIdentity.IsSet ? world.MapIdentity.Value : plan.ResolvedContent.FirstOrDefault(x => x.Kind == "situation-map")?.Identity;
        var context = new Dictionary<string, object?>
        {
            ["type"] = "context",
            ["schema"] = ContextSchema,
            ["session_id"] = plan.SessionId.ToString("D"),
            ["module_id"] = manifest.Id,
            ["module_version"] = manifest.Version,
            ["product_version"] = ProductVersion,
            ["installation_root"] = Path.GetFullPath(plan.Spec.Installation.RootPath),
            ["module_directory"] = Path.GetFullPath(directory),
            ["data_directory"] = Path.GetFullPath(data),
            ["channel"] = RuntimeModuleProtocol.SessionChannelName(plan.SessionId, manifest.Id),
            ["settings"] = planned.EffectiveSettings.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal),
            ["world"] = new Dictionary<string, object?>
            {
                ["mode"] = world.Mode.ToString(),
                ["map"] = map,
                ["situation"] = world.SituationIdentity.IsSet ? world.SituationIdentity.Value : null,
                ["entrypoint_index"] = world.PresentedEntrypointIndex.IsSet ? world.PresentedEntrypointIndex.Value : null,
                ["entrypoint"] = world.EntrypointIdentity.IsSet ? world.EntrypointIdentity.Value : null
            },
            // Files the session transaction owns: a program must not keep them
            // open or mapped, or the restore at the end of the session fails.
            ["session_files"] = plan.TouchedFiles.Append("closecheck").Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
        return JsonSerializer.Serialize(context);
    }

    private static string ProductVersion { get; } = typeof(ModuleSessionPrograms).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";

    private sealed class SessionProgram
    {
        private readonly Action<string, IReadOnlyDictionary<string, string>> addEvent;
        private readonly Action<LaunchDiagnostic> addDiagnostic;
        private int lines;
        public SessionProgram(PlannedModule planned, ModuleManifest manifest, Action<string, IReadOnlyDictionary<string, string>> addEvent, Action<LaunchDiagnostic> addDiagnostic)
        {
            Planned = planned; Manifest = manifest; this.addEvent = addEvent; this.addDiagnostic = addDiagnostic;
        }
        public PlannedModule Planned { get; }
        public ModuleManifest Manifest { get; }
        public ModuleSessionProcess? Process { get; set; }
        public ManualResetEventSlim Ready { get; } = new();
        public bool Done { get; set; }
        public bool Ended => ended;
        private bool ended;

        public void Emit(string name, params (string Key, string Value)[] values) => addEvent("module." + Manifest.Id + "." + name, values.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal));
        public void AddDiagnostic(LaunchDiagnostic diagnostic) => addDiagnostic(diagnostic);

        // A line from the program. Its content is data: bounded, never run.
        public void OnLine(string line)
        {
            // A program cannot flood the session's event list.
            if (Interlocked.Increment(ref lines) > 10_000) return;
            JsonDocument document;
            try { document = JsonDocument.Parse(line); }
            catch (JsonException) { return; }
            using (document)
            {
                var root = document.RootElement;
                if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String) return;
                var data = root.TryGetProperty("data", out var dataElement) && dataElement.ValueKind == JsonValueKind.Object
                    ? dataElement.EnumerateObject().Take(32).ToDictionary(x => x.Name, x => Bounded(x.Value.ValueKind == JsonValueKind.String ? x.Value.GetString()! : x.Value.GetRawText()), StringComparer.Ordinal)
                    : new Dictionary<string, string>(StringComparer.Ordinal);
                switch (typeElement.GetString())
                {
                    case "ready": Ready.Set(); break;
                    case "event" when root.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String && RuntimeModuleProtocol.IsOperationId(name.GetString()!):
                        addEvent("module." + Manifest.Id + "." + name.GetString(), name.GetString() == ModuleNotices.EventName ? ModuleNotices.Sanitize(data) : data);
                        break;
                    case "notice":
                        addEvent("module." + Manifest.Id + "." + ModuleNotices.EventName, ModuleNotices.Sanitize(data));
                        break;
                    case "error":
                        var code = data.TryGetValue("code", out var value) && PublicErrorCodes.FromMessage(value) == value ? value : PublicErrorCodes.MODULE_OPERATION_FAILED;
                        addDiagnostic(new(code, Manifest.Id + ": " + (data.TryGetValue("detail", out var detail) ? detail : "the session program reported an error"), new Dictionary<string, string>(StringComparer.Ordinal) { ["module"] = Manifest.Id }));
                        break;
                }
            }
        }

        // Asks nothing more: waits up to milliseconds for the program to exit,
        // then ends it and releases its job and pipes.
        public void End(int milliseconds)
        {
            if (ended || Process is null) return;
            ended = true;
            var exited = Process.WaitForExit(TimeSpan.FromMilliseconds(milliseconds));
            if (!exited) Process.Kill();
            var code = Process.ExitCode;
            Process.Dispose();
            Emit(exited ? "host.exited" : "host.killed", ("exit_code", code?.ToString(CultureInfo.InvariantCulture) ?? ""));
        }

        private static string Bounded(string value) => value.Length <= 1024 ? value : value[..1024];
    }
}
