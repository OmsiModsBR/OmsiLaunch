using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using OmsiLaunch.Api;
using OmsiLaunch.Configuration;
using OmsiLaunch.Core;
using OmsiLaunch.Process;
using OmsiLaunch.Runtime.Modules;

var jsonRequested = args.Any(argument => argument.Equals("--json", StringComparison.OrdinalIgnoreCase) || argument.Equals("/json", StringComparison.OrdinalIgnoreCase));
CliInput input;
try { input = CliInput.Parse(args); }
catch (SessionProfileException exception)
{
    CliInput.WriteError("session profile", exception.Code, exception.Message, jsonRequested, PublicErrorCategory.InvalidArgument);
    return (int)PublicExitCode.InvalidArguments;
}
catch (Exception exception) when (exception is ArgumentException or FormatException or InvalidDataException or OverflowException)
{
    CliInput.WriteError("cli", "OL_E_INVALID_ARGUMENT", exception.Message, jsonRequested, PublicErrorCategory.InvalidArgument);
    return (int)PublicExitCode.InvalidArguments;
}
// Every failure below this line maps to a documented exit code and a typed
// error envelope; no path ends in an unhandled exception.
try { return await CliProgram.RunAsync(input, args); }
catch (Exception exception) { return CliProgram.ReportFailure(exception, input.JsonOutput); }

internal static class CliProgram
{
    // Identity comes from the assembly the build stamped from OmsiLaunch.Version.props.
    public static string ProductVersion { get; } = typeof(CliProgram).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
    public const string SupportedFamily = "OMSI_2";

    // The delegated host must not inherit this process's handles: with
    // CreateProcess inheritance it kept the caller's stdout pipe open for the
    // whole session, so a caller capturing /silent output blocked until the
    // session ended (runtime closure BUG-03). ShellExecute never inherits.
    internal static ProcessStartInfo SilentDelegation(string windowsHost, IEnumerable<string> args)
    {
        var delegated = new ProcessStartInfo(windowsHost) { UseShellExecute = true, WorkingDirectory = Environment.CurrentDirectory };
        foreach (var argument in args.Where(argument => !argument.Equals("/silent", StringComparison.OrdinalIgnoreCase) && !argument.Equals("--silent", StringComparison.OrdinalIgnoreCase))) delegated.ArgumentList.Add(argument);
        return delegated;
    }

    public static async Task<int> RunAsync(CliInput input, string[] args)
    {
        if (input.Silent && !WindowsHost.IsActive)
        {
            var windowsHost = Path.Combine(AppContext.BaseDirectory, "OmsiLaunchW.exe");
            if (!File.Exists(windowsHost)) { CliInput.WriteError("silent", "OL_E_WINDOWS_HOST_MISSING", "OmsiLaunchW.exe is required for /silent execution.", input.JsonOutput); return (int)PublicExitCode.OperationRejected; }
            // /silent detaches: success means the Windows host was started, not
            // that the session succeeded. The host reports through .omsilaunch.
            using var host = Process.Start(SilentDelegation(windowsHost, args));
            if (host is null) { CliInput.WriteError("silent", "OL_E_WINDOWS_HOST_START_FAILED", "OmsiLaunchW.exe could not be started.", input.JsonOutput); return (int)PublicExitCode.OperationRejected; }
            CliInput.WriteEnvelope("silent", new { delegated = true, host_process_id = host.Id }, input.JsonOutput);
            return (int)PublicExitCode.Success;
        }
        // A flag only an installed module declares is valid where a session is planned.
        input.RejectUnresolvedFlags();
        // /quiet: only JSON output and errors reach the console.
        CliInput.QuietOutput = input.Quiet;
        if (input.Serve) { CliInput.WriteError("serve", PublicErrorCodes.INVALID_ARGUMENT, "/serve was removed in 0.1.0-beta4: the session owner always serves the local control endpoint.", input.JsonOutput, PublicErrorCategory.InvalidArgument); return (int)PublicExitCode.InvalidArguments; }
        if (input.Version) { CliInput.WriteEnvelope("version", new { product = "OmsiLaunch", version = ProductVersion, protocol_version = PublicCapabilityRegistry.ProtocolVersion, supported_family = SupportedFamily }, input.JsonOutput); return (int)PublicExitCode.Success; }
        if (input.Command is "capabilities") { CliInput.WriteEnvelope("capabilities", PublicCapabilityRegistry.All.Where(x => x.Classification is PublicCapabilityClassification.PublicStableBeta or PublicCapabilityClassification.PublicExperimental), input.JsonOutput); return (int)PublicExitCode.Success; }
        if (input.Command is "help") { CliInput.WriteEnvelope("help", CliInput.HelpFor(input.CommandWords.SingleOrDefault()), input.JsonOutput); return (int)PublicExitCode.Success; }
        // Authoring helpers for community modules (bridge protocol 3). They read
        // and write only the module directory they are given, never an installation.
        if (input.PackModule is not null || input.VerifyModule is not null)
        {
            var packing = input.PackModule is not null;
            var moduleDirectory = Path.GetFullPath(input.PackModule ?? input.VerifyModule!);
            try
            {
                var packed = packing ? ModulePacker.Pack(moduleDirectory) : ModulePacker.Verify(moduleDirectory);
                CliInput.WriteEnvelope(packing ? "module.pack" : "module.verify", new
                {
                    module = packed.ModuleId,
                    directory = moduleDirectory,
                    written = packed.Written,
                    intact = packed.Intact,
                    files = packed.Files.Select(x => new { path = x.Path, sha256 = x.Current, pinned = x.Previous is not null, mismatch = x.Mismatch, written = packing && x.NeedsWrite }).ToArray()
                }, input.JsonOutput);
                return packing || packed.Intact ? (int)PublicExitCode.Success : (int)PublicExitCode.OperationRejected;
            }
            catch (ModuleException exception)
            {
                CliInput.WriteError(packing ? "module.pack" : "module.verify", exception.Code, exception.Message, input.JsonOutput, PublicErrorCategory.InvalidArgument);
                return (int)PublicExitCode.InvalidArguments;
            }
        }
        if (input.Command is "profiles")
        {
            // Compatibility is "OMSI 2 installed"; hashes are diagnostics and
            // select a known layout. Capabilities are predicted from the file
            // and decided in process at start.
            var build = OmsiLaunch.Builds.Omsi23004.OmsiBuildDetection.Detect(CliInput.ResolveInstallationRoot(input.Installation));
            CliInput.WriteEnvelope("profiles", new
            {
                requirement = "OMSI 2",
                installation = new { is_omsi2 = build.IsOmsi2, reason = build.Reason, build_id = build.BuildId, identity = build.KnownIdentity ? "known" : "unknown", file_sha256 = build.FileSha256, file_version = build.FileVersion, code_sha256 = build.CodeSha256, file_variant = build.KnownFileVariant },
                known_layouts = OmsiLaunch.Builds.Omsi23004.NativeTargetCatalog.KnownCodeSha256.Select(code => new { build_id = OmsiLaunch.Builds.Omsi23004.NativeTargetCatalog.PackName, code_sha256 = code, runtime_validated = true }),
                predicted_capabilities = build.StaticCapabilities
            }, input.JsonOutput);
            return (int)PublicExitCode.Success;
        }

        // Client mode: no installation argument, an owner is addressed through
        // the local control plane of the installation this executable lives in.
        var clientRoot = input.Installation ?? AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(input.Installation) && input.RuntimeOperation is not null)
        {
            var arguments = new Dictionary<string, string>(input.RuntimeArguments, StringComparer.Ordinal);
            var validation = PublicCapabilityRegistry.ValidateRuntimeArguments(input.RuntimeOperation, arguments);
            if (!validation.Accepted)
            {
                CliInput.WriteError(input.RuntimeOperation, validation.ErrorCode!, validation.Message!, input.JsonOutput, PublicErrorCategory.InvalidArgument);
                return (int)PublicExitCode.InvalidArguments;
            }
            arguments["operation"] = input.RuntimeOperation;
            var controlTimeout = input.RuntimeOperation is "road-vehicles.spawn" ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(8);
            var forwarded = await LocalControlPlane.TryRequestBoundAsync(clientRoot, "runtime.execute", arguments, controlTimeout);
            return ReportForwarded(input.RuntimeOperation, forwarded, input.JsonOutput);
        }
        if (string.IsNullOrWhiteSpace(input.Installation) && input.Command is "session" && input.CommandWords.Count == 1 && input.CommandWords[0].Equals("status", StringComparison.OrdinalIgnoreCase))
            return ReportForwarded("session.status", await LocalControlPlane.TryRequestAsync(clientRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(750)), input.JsonOutput);
        if (string.IsNullOrWhiteSpace(input.Installation) && input.Command is "session" && input.CommandWords.Count == 1 && input.CommandWords[0].Equals("stop", StringComparison.OrdinalIgnoreCase))
            return ReportForwarded("session.stop", await LocalControlPlane.TryRequestBoundAsync(clientRoot, "session.stop", null, TimeSpan.FromMilliseconds(750)), input.JsonOutput);
        if (string.IsNullOrWhiteSpace(input.Installation) && input.Command is "events" && input.CommandWords.Count == 1 && input.CommandWords[0].Equals("read", StringComparison.OrdinalIgnoreCase))
            return ReportForwarded("events.read", await LocalControlPlane.TryRequestAsync(clientRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.events"), TimeSpan.FromMilliseconds(750)), input.JsonOutput);
        if (string.IsNullOrWhiteSpace(input.Installation) && input.Command is "events" && input.CommandWords.Count == 1 && input.CommandWords[0].Equals("watch", StringComparison.OrdinalIgnoreCase))
            return await CliEventWatch.RunAsync(input, clientRoot);
        if (input.Command is "detect" || (string.IsNullOrWhiteSpace(input.Installation) && input.Command is null && !input.Help && input.SpecFile is null && !input.LaunchRequested && !input.Recovery && input.List is null))
        {
            var discovered = Process.GetProcessesByName("Omsi").Select(process =>
            {
                try { return new { state = "OMSI_FOUND_UNMANAGED", process_id = process.Id, executable_path = process.MainModule?.FileName, responsive = process.Responding }; }
                catch { return new { state = "UNKNOWN_BINARY_FOUND", process_id = process.Id, executable_path = (string?)null, responsive = false }; }
            }).ToArray();
            var active = await LocalControlPlane.TryRequestAsync(clientRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(250));
            CliInput.WriteEnvelope("detect", new { state = discovered.Length == 0 ? "NO_OMSI_FOUND" : "OMSI_FOUND_UNMANAGED", processes = discovered, active_omsilaunch_instance = active?.Ok == true, managed_session = active?.Result }, input.JsonOutput);
            return (int)PublicExitCode.Success;
        }
        if (input.Help || (string.IsNullOrWhiteSpace(input.Installation) && input.SpecFile is null && !input.LaunchRequested && !input.Recovery && input.List is null))
        {
            Console.WriteLine(CliInput.Usage);
            return input.Help ? (int)PublicExitCode.Success : (int)PublicExitCode.InvalidArguments;
        }

        // Owner mode. The controller runs from the installed package root; the
        // permanent plugin closure and its packaged manifest live beside it.
        var packagedRuntime = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (!File.Exists(Path.Combine(packagedRuntime, "OmsiLaunch.Plugin.opl")))
        {
            CliInput.WriteError("installation", "OL_E_RUNTIME_INSTALLATION_INCOMPLETE", "The installed OmsiLaunch plugin is missing from plugins\\OmsiLaunch.Plugin.opl.", input.JsonOutput, PublicErrorCategory.Runtime);
            return (int)PublicExitCode.OperationRejected;
        }
        var nativeRuntime = Path.Combine(packagedRuntime, "OmsiLaunch.Native.x86.dll");
        if (!File.Exists(nativeRuntime))
        {
            CliInput.WriteError("installation", "OL_E_RUNTIME_INSTALLATION_INCOMPLETE", "The installed OmsiLaunch native bridge is missing from plugins\\OmsiLaunch.Native.x86.dll.", input.JsonOutput, PublicErrorCategory.Runtime);
            return (int)PublicExitCode.OperationRejected;
        }
        var manifest = Path.Combine(AppContext.BaseDirectory, ReleaseManifest.FileName);
        IOmsiLaunch launch = new OmsiLaunchService(new CurrentWindowsX64Platform(), new OmsiLaunchRuntimePaths(packagedRuntime, nativeRuntime, File.Exists(manifest) ? manifest : null));

        var installationRoot = CliInput.ResolveInstallationRoot(input.Installation);
        if (input.Recovery)
        {
            var recovery = await launch.RecoverPendingAsync(new InstallationSpec(installationRoot), input.Recover);
            CliInput.WriteEnvelope("recover", new { pending = recovery.Pending, recovered = recovery.Recovered, diagnostics = recovery.Diagnostics }, input.JsonOutput);
            return recovery.Pending && input.Recover && !recovery.Recovered ? (int)PublicExitCode.TransactionRecoveryFailed : (int)PublicExitCode.Success;
        }

        if (input.ClearModuleData is not null)
        {
            // A module's data (.omsilaunch\module-data\<id>) belongs to the module;
            // only this explicit request deletes it, and never during a session.
            var cleared = ModuleDataStore.Clear(installationRoot, input.ClearModuleData);
            CliInput.WriteEnvelope("module-data.clear", new { module = input.ClearModuleData, cleared }, input.JsonOutput);
            return (int)PublicExitCode.Success;
        }

        if (input.List is not null)
        {
            if (!Enum.TryParse<ContentQueryKind>(input.List, true, out var kind)) throw new ArgumentException("Unknown discovery category: " + input.List);
            if (kind == ContentQueryKind.RuntimeModules)
            {
                CliInput.WriteEnvelope("runtime-modules.list", await launch.ListRuntimeModulesAsync(new InstallationSpec(installationRoot)), input.JsonOutput);
                return (int)PublicExitCode.Success;
            }
            if (kind == ContentQueryKind.SessionProfiles)
            {
                CliInput.WriteEnvelope("session-profiles.list", await launch.ListSessionProfilesAsync(new InstallationSpec(installationRoot)), input.JsonOutput);
                return (int)PublicExitCode.Success;
            }
            var scope = kind == ContentQueryKind.Entrypoints ? input.Map : input.VehicleScope;
            var result = await launch.DiscoverAsync(new InstallationSpec(installationRoot), kind, scope is null ? OptionalValue<string>.Unset : OptionalValue<string>.Set(scope));
            CliInput.WriteEnvelope("content.list", result, input.JsonOutput);
            return (int)PublicExitCode.Success;
        }

        var spec = await input.BuildSpecAsync();
        var plan = await launch.PlanSessionAsync(spec);
        CliInput.Write(plan, input.JsonOutput);
        if (input.PlanOnly || input.ValidateOnly) return plan.IsRunnable ? (int)PublicExitCode.Success : (int)PublicExitCode.SessionFailed;
        // Under OmsiLaunchW.exe a launch that cannot start must not end
        // silently (documentation audit BUG-06): report the plan's last error.
        if (!plan.IsRunnable) { WindowsHost.ShowFailure(plan.Diagnostics, "The session plan is not runnable."); return (int)PublicExitCode.SessionFailed; }

        var existingOwner = await LocalControlPlane.TryRequestAsync(spec.Installation.RootPath, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(250));
        // Any reply, including a typed error, proves an owner holds the endpoint.
        if (existingOwner is not null)
        {
            CliInput.WriteError("session", "OL_E_SESSION_ALREADY_ACTIVE", "An OmsiLaunch owner is already managing this installation. Use session status, session stop, events, or runtime commands as a client.", input.JsonOutput);
            return (int)PublicExitCode.OperationRejected;
        }
        return await OwnerSession.RunAsync(launch, input, spec, plan);
    }

    internal static int ReportForwarded(string command, LocalControlResponse? forwarded, bool json)
    {
        if (forwarded is null) { CliInput.WriteError(command, "OL_E_NO_ACTIVE_SESSION", "No active OmsiLaunch control instance was found for this installation.", json); return (int)PublicExitCode.NoActiveSession; }
        if (!forwarded.Ok) { CliInput.WriteError(command, forwarded.ErrorCode ?? "OL_E_CONTROL_FAILED", forwarded.Message ?? "Control request failed.", json); return forwarded.ErrorCode is "OL_E_RUNTIME_OPERATION_UNKNOWN" or "OL_E_RUNTIME_ARGUMENT_REQUIRED" ? (int)PublicExitCode.InvalidArguments : (int)PublicExitCode.OperationRejected; }
        CliInput.WriteEnvelope(command, forwarded.Result!, json, forwarded.Metadata);
        return (int)PublicExitCode.Success;
    }

    // Maps any escaped exception to the public exit-code contract. Codes that
    // start with OL_E_ are surfaced verbatim; everything else is classified.
    public static int ReportFailure(Exception exception, bool json)
    {
        var (code, category, exit) = Classify(exception);
        CliInput.WriteError("cli", code, exception.Message, json, category);
        return (int)exit;
    }

    internal static (string Code, string Category, PublicExitCode Exit) Classify(Exception exception)
    {
        var code = ExtractCode(exception.Message);
        switch (exception)
        {
            case SessionProfileException profile: return (profile.Code, PublicErrorCategory.InvalidArgument, PublicExitCode.InvalidArguments);
            case ArgumentException or FormatException or InvalidDataException or OverflowException: return (code ?? "OL_E_INVALID_ARGUMENT", PublicErrorCategory.InvalidArgument, PublicExitCode.InvalidArguments);
            case FileNotFoundException or DirectoryNotFoundException: return (code ?? "OL_E_NOT_FOUND", PublicErrorCategory.NotFound, PublicExitCode.NotFound);
            case TimeoutException: return (code ?? "OL_E_TIMEOUT", PublicErrorCategory.Runtime, PublicExitCode.RuntimeUnavailable);
            case OperationCanceledException: return ("OL_E_CANCELLED", PublicErrorCategory.Session, PublicExitCode.OperationRejected);
        }
        if (code is not null)
        {
            if (code.StartsWith("OL_E_RECOVERY_", StringComparison.Ordinal) || code is "OL_E_RESTORE_FAILED") return (code, PublicErrorCategory.Transaction, PublicExitCode.TransactionRecoveryFailed);
            if (code is "OL_E_INSTALLATION_BUSY" or "OL_E_SESSION_ALREADY_ACTIVE") return (code, PublicErrorCategory.Session, PublicExitCode.OperationRejected);
            if (code is "OL_E_PLAN_NOT_RUNNABLE") return (code, PublicErrorCategory.Session, PublicExitCode.SessionFailed);
            if (code is "OL_E_UNKNOWN_SETTING" or "OL_E_SETTING_NOT_WRITABLE" or "OL_E_ITX_PROFILE_REQUIRED") return (code, PublicErrorCategory.InvalidArgument, PublicExitCode.InvalidArguments);
            if (code.StartsWith("OL_E_UNSUPPORTED_", StringComparison.Ordinal)) return (code, PublicErrorCategory.UnsupportedProfile, PublicExitCode.UnsupportedProfile);
            return (code, exception is InvalidOperationException or IOException ? PublicErrorCategory.Runtime : PublicErrorCategory.Internal, PublicExitCode.OperationRejected);
        }
        return ("OL_E_INTERNAL", PublicErrorCategory.Internal, PublicExitCode.InternalError);
    }

    private static string? ExtractCode(string message)
    {
        var start = message.IndexOf("OL_E_", StringComparison.Ordinal); if (start < 0) return null;
        var end = start; while (end < message.Length && ((char.IsLetterOrDigit(message[end]) && message[end] < 128) || message[end] == '_')) end++;
        return message[start..end];
    }
}

// The owner lifecycle. From StartSessionAsync onwards this process holds a
// transaction; every exit path, including console signals and unexpected
// exceptions, ends in CloseAsync so restore and lease release are never skipped.
internal static class OwnerSession
{
    public static async Task<int> RunAsync(IOmsiLaunch launch, CliInput input, LaunchSpec spec, SessionPlan plan, bool consoleSignals = true)
    {
        var root = spec.Installation.RootPath;
        var controlStopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var session = await launch.StartSessionAsync(plan);
        ConsoleCancelEventHandler? cancel = null; EventHandler? exit = null;
        if (consoleSignals)
        {
            cancel = (_, eventArgs) => { eventArgs.Cancel = true; controlStopped.TrySetResult(); };
            Console.CancelKeyPress += cancel;
            // Console close and logoff give the runtime a few seconds. Request the
            // canonical stop and wait for restore within that budget; anything
            // left over is recovered by the durable journal on the next start.
            exit = (_, _) => { try { launch.StopAsync(session).GetAwaiter().GetResult(); launch.WaitForAsync(session, SessionState.Completed, TimeSpan.FromSeconds(4)).GetAwaiter().GetResult(); } catch (Exception) { } };
            AppDomain.CurrentDomain.ProcessExit += exit;
        }
        SessionTrayIndicator? tray = null; LocalControlPlane? control = null;
        try
        {
            tray = spec.EffectivePresentation.SuppressTrayIcon
                ? null
                : SessionTrayIndicator.CreateIfAvailable(root, plan, () => launch.GetStatusAsync(session).GetAwaiter().GetResult(), () => controlStopped.TrySetResult());
            var running = await launch.WaitForAsync(session, SessionState.Running, TimeSpan.FromSeconds(spec.Behavior.StartupTimeoutSeconds + 5));
            CliInput.Write(running, input.JsonOutput);
            if (running.State != SessionState.Running)
            {
                WindowsHost.ShowFailure(running.Diagnostics, "The OMSI session did not reach gameplay.");
                return (int)PublicExitCode.SessionFailed;
            }
            var runtimeResults = input.D3DBatch
                ? await D3DValidationBatch.ExecuteAsync(launch, session, plan.BuildProfileId)
                : input.RuntimeBatch || input.RuntimeWriteBatch ? await RuntimeBatch.ExecuteAsync(launch, session, plan.BuildProfileId, input.RuntimeWriteBatch) : Array.Empty<RuntimeBatchResult>();
            if (input.RuntimeBatch || input.RuntimeWriteBatch) RuntimeBatch.WriteArtifact(root, session.SessionId, plan.BuildProfileId, runtimeResults, input.RuntimeWriteBatch);
            if (input.D3DBatch) D3DValidationBatch.WriteArtifact(root, session.SessionId, plan.BuildProfileId, runtimeResults);
            foreach (var result in runtimeResults) CliInput.Write(result, input.JsonOutput);
            long forwardedRequestId = 50_000;
            control = new LocalControlPlane(root, async request =>
            {
                bool Bound() => request.Arguments is not null && request.Arguments.TryGetValue("session_id", out var id) && Guid.TryParse(id, out var parsed) && parsed == session.SessionId;
                if (request.Command == "session.status") { var fitted = LocalControlPlane.FitStatus(await launch.GetStatusAsync(session), out var dropped); return new(true, fitted, Metadata: LocalControlPlane.TruncationMetadata(fitted.RuntimeEvents, dropped)); }
                if (request.Command == "session.events") { var events = LocalControlPlane.FitEvents((await launch.GetStatusAsync(session)).RuntimeEvents ?? Array.Empty<RuntimeEvent>(), 256, out var dropped); return new(true, events, Metadata: LocalControlPlane.TruncationMetadata(events, dropped)); }
                // Mutating commands are bound to this session id so a client
                // cannot act on a session it has not identified first.
                if (request.Command == "session.stop")
                {
                    if (!Bound()) return new(false, ErrorCode: "OL_E_CONTROL_SESSION_MISMATCH", Message: "session.stop requires the active session_id.");
                    controlStopped.TrySetResult(); return new(true, new { accepted = true, session_id = session.SessionId });
                }
                if (request.Command == "runtime.execute" && request.Arguments is not null && request.Arguments.TryGetValue("operation", out var operation))
                {
                    if (!Bound()) return new(false, ErrorCode: "OL_E_CONTROL_SESSION_MISMATCH", Message: "runtime.execute requires the active session_id.");
                    var arguments = request.Arguments.Where(x => x.Key is not ("operation" or "session_id")).ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
                    var validation = PublicCapabilityRegistry.ValidateRuntimeArguments(operation, arguments);
                    if (!validation.Accepted) return new(false, ErrorCode: validation.ErrorCode, Message: validation.Message);
                    var runtimeTimeout = operation is "road-vehicles.spawn" ? TimeSpan.FromSeconds(30) : TimeSpan.FromSeconds(8);
                    var requestId = unchecked((ulong)Interlocked.Increment(ref forwardedRequestId));
                    var result = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId, operation, arguments), runtimeTimeout);
                    return result.Succeeded ? new(true, result) : new(false, result, result.ErrorCode, "Runtime operation was rejected.");
                }
                return new(false, ErrorCode: "OL_E_CONTROL_COMMAND_UNKNOWN", Message: "Unsupported local control command.");
            });
            control.Start();
            if (input.RuntimeOperation is not null)
            {
                try
                {
                    var runtimeTimeout = input.RuntimeOperation is "road-vehicles.spawn" ? TimeSpan.FromSeconds(15) : TimeSpan.FromSeconds(5);
                    var runtimeResult = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, 10_001, input.RuntimeOperation, input.RuntimeArguments), runtimeTimeout);
                    var diagnosticDirectory = Path.Combine(root, ".omsilaunch", "diagnostics"); Directory.CreateDirectory(diagnosticDirectory);
                    File.WriteAllText(Path.Combine(diagnosticDirectory, session.SessionId.ToString("N") + "-runtime-operation.json"), JsonSerializer.Serialize(new { session_id = session.SessionId, operation = input.RuntimeOperation, result = runtimeResult }, CliInput.Json));
                    CliInput.Write(runtimeResult, input.JsonOutput);
                }
                catch (Exception exception)
                {
                    // An isolated runtime-command failure is not permission to terminate a
                    // healthy, transaction-owning session.
                    CliInput.Write(new { runtime_error = exception.Message }, input.JsonOutput);
                }
            }
            var completion = launch.WaitForAsync(session, SessionState.Completed, Timeout.InfiniteTimeSpan);
            if (input.ObserveSeconds is { } observeSeconds)
            {
                // An observation window is an upper bound; tray and control-plane
                // stop requests still end the session early.
                await Task.WhenAny(Task.Delay(TimeSpan.FromSeconds(observeSeconds)), controlStopped.Task, completion);
                await launch.StopAsync(session);
            }
            else
            {
                var signal = await Task.WhenAny(controlStopped.Task, completion);
                if (signal == controlStopped.Task) await launch.StopAsync(session);
            }
            var completed = await completion;
            CliInput.Write(completed, input.JsonOutput);
            return completed.State == SessionState.Completed ? (int)PublicExitCode.Success : (int)PublicExitCode.SessionFailed;
        }
        finally
        {
            if (control is not null) { try { await control.DisposeAsync(); } catch (Exception) { } }
            tray?.Dispose();
            await launch.CloseAsync(session);
            if (cancel is not null) Console.CancelKeyPress -= cancel;
            if (exit is not null) AppDomain.CurrentDomain.ProcessExit -= exit;
        }
    }
}

internal sealed class CliInput
{
    internal const string Usage = "OmsiLaunch.exe [detect|capabilities|profiles|help [family]] | [session status|stop] | [events read|watch] | [<family> <verb> [--key=value ...]]\n           [<installation>] [/silent] [/predefined-profile:<id> /predefined-profile-index:<1..99>] [/hof-source:<collection|directory|vehicle|file>:<value> /hof-target:<vehicle> ... [/hof-file:<name.hof> ...] [/hof-mode:add|mirror]] [/module:<id> ... /no-module:<id> ... /module-setting:<id>.<key>=<value> ... /<module flag>[:<value>]] [/new|/saved:<file.osn>|/last] [/map:<identity>] [/entrypoint:<identity>|/entrypoint-index:<n>] [/date:<yyyy-mm-dd|system> /time:<hh:mm[:ss]|system> /year:<n|system>] [/weather:<preset>|/weather-icao:<code>|/weather-real] [/vehicle:<identity> /repaint:<id> /hof:<id> /fleet:<n> /registration:<r>|/no-vehicle] [/set:<key>=<value> ...] [/splash:Managed|Native|Unset /splash-language:PTB|ENG|FRA|DEU /splash-assets:<directory>] [/internet-textures:Native|Disabled|Override /internet-textures-profile:<file.itx>] [/spec:<path-to-json>] [/plan|/validate|/runtime:<operation> /runtime-arg:<key=value>] [/startup-timeout:<1..600>] [/observe-seconds:<n>] [/list:<category> [/vehicle-scope:<bus>]] [/recovery-status|/recover] [/clear-module-data:<id>] [/version] [--json]\n\nExamples: OmsiLaunch.exe /predefined-profile:rmg-leste /predefined-profile-index:1 /new; OmsiLaunch.exe /silent /predefined-profile:rmg-leste /predefined-profile-index:1 /new; OmsiLaunch.exe session status; OmsiLaunch.exe time get. A normal launch remains active until OMSI exits, the tray ends the session, or a client sends session stop. Full reference: .omsilaunch\\docs\\reference\\cli.md";
    internal static readonly JsonSerializerOptions Json = new() { WriteIndented = true };
    public string? Installation { get; private set; } public string? Command { get; private set; } public List<string> CommandWords { get; } = new(); public bool Help { get; private set; } public bool Version { get; private set; } public bool JsonOutput { get; private set; } public bool Quiet { get; private set; } public bool Verbose { get; private set; } public bool Log { get; private set; } public bool LogAll { get; private set; } public bool OmsiLogAll { get; private set; } public bool TraceProcess { get; private set; } public bool TracePlugin { get; private set; } public bool TraceNative { get; private set; } public bool PlanOnly { get; private set; } public bool ValidateOnly { get; private set; } public bool Serve { get; private set; } public bool LaunchRequested { get; private set; } public bool Silent { get; private set; }
    public SplashMode Splash { get; private set; } = SplashMode.Managed; public bool SplashSpecified { get; private set; } public string? SplashLanguage { get; private set; } public string? SplashAssets { get; private set; } public InternetTexturesMode InternetTextures { get; private set; } = InternetTexturesMode.Native; public bool InternetTexturesSpecified { get; private set; } public string? InternetTexturesProfile { get; private set; } public string? PredefinedProfile { get; private set; } public int? PredefinedProfileIndex { get; private set; }
    public bool Recovery { get; private set; } public bool Recover { get; private set; } public bool RuntimeBatch { get; private set; } public bool RuntimeWriteBatch { get; private set; } public bool D3DBatch { get; private set; } public string? RuntimeOperation { get; private set; } public Dictionary<string, string> RuntimeArguments { get; } = new(StringComparer.Ordinal); public string? List { get; private set; } public string? VehicleScope { get; private set; } public string? SpecFile { get; private set; }
    public WorldMode WorldMode { get; private set; } = WorldMode.NewMap; public string? Map { get; private set; } public string? Situation { get; private set; } public int? EntrypointIndex { get; private set; } public string? EntrypointIdentity { get; private set; }
    public string? Date { get; private set; } public string? Time { get; private set; } public string? Year { get; private set; } public WeatherMode WeatherMode { get; private set; } public string? Weather { get; private set; } public string? Icao { get; private set; }
    public bool NoVehicle { get; private set; } public string? Vehicle { get; private set; } public string? Repaint { get; private set; } public string? Hof { get; private set; } public string? Fleet { get; private set; } public string? Registration { get; private set; }
    // Timeouts stay unset unless given on the command line so that a /spec or
    // session-profile value is not silently overridden by a CLI default.
    public int? StartupTimeout { get; private set; } public int? ShutdownTimeout { get; private set; } public bool CooperativeShutdown { get; private set; } public int? ObserveSeconds { get; private set; } public Dictionary<string, string> Settings { get; } = new(StringComparer.OrdinalIgnoreCase);
    // One HOF operation from the command line; several operations need /spec
    // or a session profile.
    public HofSourceKind? HofSourceKind { get; private set; } public string? HofSource { get; private set; } public List<string> HofTargets { get; } = new(); public List<string> Modules { get; } = new(); public List<string> HofFiles { get; } = new(); public HofApplyMode? HofMode { get; private set; }
    // Runtime modules: /no-module, /module-setting and /clear-module-data. A
    // flag no built-in knows is kept here and resolved against the flags the
    // installed modules declare when a session is planned.
    public List<string> DisabledModules { get; } = new(); public Dictionary<string, string> ModuleSettings { get; } = new(StringComparer.Ordinal); public string? ClearModuleData { get; private set; } public string? PackModule { get; private set; } public string? VerifyModule { get; private set; } public List<string> UnresolvedFlags { get; } = new();
    public bool HasAutomaticStopPolicy => ObserveSeconds.HasValue;

    // Every accepted hierarchical command route and the runtime operation it
    // maps to. The documentation gate reads this table.
    // Command route ("time get") -> runtime operation, from the canonical catalog.
    internal static readonly IReadOnlyDictionary<string, string> HierarchicalRoutes = PublicCapabilityRegistry.CliRoutes;
    // Every command word the parser accepts in the first position.
    internal static readonly IReadOnlyList<string> CommandWordsAccepted = new[] { "capabilities", "profiles", "detect", "help", "session", "events", "time", "weather", "map", "camera", "vehicles", "player", "humans", "timetable", "scripts", "constants", "curves", "hof", "drivers", "tickets", "d3d", "ui" };
    // Every slash/dash flag the parser accepts (without value suffix), including
    // the compatibility flags that currently have no effect.
    internal static readonly IReadOnlyList<string> KnownFlags = new[] { "?", "help", "version", "json", "quiet", "silent", "verbose", "serve", "log", "logall", "omsi-logall", "trace", "trace-process", "trace-plugin", "trace-native", "plan", "validate", "runtime-batch", "runtime-write-batch", "splash", "splash-language", "splash-assets", "internet-textures", "internet-textures-profile", "d3d-batch", "runtime", "runtime-arg", "new", "saved", "last", "map", "entrypoint", "entrypoint-index", "date", "time", "year", "weather", "weather-icao", "weather-real", "vehicle", "repaint", "hof", "fleet", "registration", "no-vehicle", "set", "predefined-profile", "predefined-profile-index", "hof-source", "hof-target", "hof-file", "hof-mode", "module", "no-module", "module-setting", "clear-module-data", "pack-module", "verify-module", "spec", "list", "vehicle-scope", "startup-timeout", "shutdown-timeout", "cooperative-shutdown", "observe-seconds", "recovery-status", "recover" };
    // Flags that are parsed for compatibility but have no effect in this build.
    internal static readonly IReadOnlyList<string> AcceptedNoEffectFlags = Array.Empty<string>();

    public static CliInput Parse(string[] args)
    {
        var output = new CliInput();
        foreach (var raw in args)
        {
            if (raw.StartsWith("--", StringComparison.Ordinal) && raw.Contains('='))
            {
                var argument = raw[2..].Split('=', 2);
                output.RuntimeArguments[argument[0]] = argument[1];
                continue;
            }
            if (!raw.StartsWith('/') && !raw.StartsWith('-'))
            {
                if (output.Command is null && raw is "capabilities" or "profiles" or "detect" or "help" or "session" or "events" or "time" or "weather" or "map" or "camera" or "vehicles" or "player" or "humans" or "timetable" or "scripts" or "constants" or "curves" or "hof" or "drivers" or "tickets" or "d3d" or "ui") { output.Command = raw; continue; }
                if (output.Command is not null && output.Installation is null) output.CommandWords.Add(raw);
                else if (output.Installation is null) output.Installation = raw;
                else output.CommandWords.Add(raw);
                continue;
            }
            var pair = raw.TrimStart('/', '-').Split(':', 2); var key = pair[0].ToLowerInvariant(); var value = pair.Length == 2 ? pair[1] : null;
            switch (key)
            {
                case "?": case "help": output.Help = true; break; case "version": output.Version = true; break; case "json": output.JsonOutput = true; break; case "quiet": output.Quiet = true; break; case "silent": output.Silent = true; break; case "verbose": output.Verbose = true; break; case "serve": output.Serve = true; break; case "log": output.Log = true; break; case "logall": output.LogAll = true; break; case "omsi-logall": output.OmsiLogAll = true; break; case "trace": case "trace-process": output.TraceProcess = true; break; case "trace-plugin": output.TracePlugin = true; break; case "trace-native": output.TraceNative = true; break; case "plan": output.PlanOnly = true; break; case "validate": output.ValidateOnly = true; break; case "runtime-batch": output.RuntimeBatch = true; break;
                case "runtime-write-batch": output.RuntimeWriteBatch = true; break;
                case "splash": output.Splash = Enum.Parse<SplashMode>(value ?? throw new ArgumentException("/splash requires Unset, Native, or Managed"), true); output.SplashSpecified = true; break;
                case "splash-language": output.SplashLanguage = value ?? throw new ArgumentException("/splash-language requires a locale"); break;
                case "splash-assets": output.SplashAssets = value ?? throw new ArgumentException("/splash-assets requires a directory"); break;
                case "internet-textures": output.InternetTextures = Enum.Parse<InternetTexturesMode>(value ?? throw new ArgumentException("/internet-textures requires Native, Disabled, or Override"), true); output.InternetTexturesSpecified = true; break;
                case "internet-textures-profile": output.InternetTexturesProfile = value ?? throw new ArgumentException("/internet-textures-profile requires an .itx path"); break;
                case "d3d-batch": output.D3DBatch = true; break;
                case "runtime": output.RuntimeOperation = value ?? throw new ArgumentException("/runtime requires an operation"); break;
                case "runtime-arg": var runtimeArgument = RequireValue(raw, value).Split('=', 2); if (runtimeArgument.Length != 2) throw new ArgumentException("/runtime-arg requires key=value"); output.RuntimeArguments[runtimeArgument[0]] = runtimeArgument[1]; break;
                case "new": output.LaunchRequested = true; output.WorldMode = WorldMode.NewMap; break; case "saved": output.LaunchRequested = true; output.WorldMode = WorldMode.SavedSituation; output.Situation = RequireValue(raw, value); break; case "last": output.LaunchRequested = true; output.WorldMode = WorldMode.LastMapState; break;
                case "map": output.Map = RequireValue(raw, value); break; case "entrypoint": output.EntrypointIdentity = RequireValue(raw, value); break; case "entrypoint-index": output.EntrypointIndex = ParseInt(raw, value, 0, int.MaxValue); break;
                case "date": output.Date = value; break; case "time": output.Time = value; break; case "year": output.Year = value; break;
                case "weather": output.WeatherMode = WeatherMode.Preset; output.Weather = value; break; case "weather-icao": output.WeatherMode = WeatherMode.Icao; output.Icao = value; break; case "weather-real": output.WeatherMode = WeatherMode.RealCurrent; break;
                case "vehicle": output.Vehicle = value; break; case "repaint": output.Repaint = value; break; case "hof": output.Hof = value; break; case "fleet": output.Fleet = value; break; case "registration": output.Registration = value; break; case "no-vehicle": output.NoVehicle = true; break;
                case "set": var setting = RequireValue(raw, value).Split('=', 2); if (setting.Length != 2) throw new ArgumentException("/set requires key=value"); output.Settings[setting[0]] = setting[1]; break;
                case "predefined-profile": output.PredefinedProfile = value ?? throw new ArgumentException("/predefined-profile requires an id"); break; case "predefined-profile-index": output.PredefinedProfileIndex = ParseInt(raw, value, SessionProfileCompiler.MinPresetIndex, SessionProfileCompiler.MaxPresetIndex); break;
                case "hof-source":
                    var hofSource = RequireValue(raw, value).Split(':', 2);
                    if (hofSource.Length != 2 || string.IsNullOrWhiteSpace(hofSource[1]) || !Enum.TryParse<HofSourceKind>(hofSource[0], true, out var hofKind) || !Enum.IsDefined(hofKind) || int.TryParse(hofSource[0], out _)) throw new ArgumentException("/hof-source requires collection:<name>, directory:<path>, vehicle:<identity> or file:<path.hof>");
                    output.HofSourceKind = hofKind; output.HofSource = hofSource[1]; break;
                case "hof-target": output.HofTargets.Add(RequireValue(raw, value)); break;
                case "module": output.Modules.Add(RequireValue(raw, value)); break;
                case "no-module": output.DisabledModules.Add(RequireValue(raw, value)); break;
                case "module-setting":
                    var moduleSetting = RequireValue(raw, value).Split('=', 2);
                    if (moduleSetting.Length != 2 || moduleSetting[0].IndexOf('.') <= 0) throw new ArgumentException("/module-setting requires <module-id>.<key>=<value>");
                    output.ModuleSettings[moduleSetting[0]] = moduleSetting[1]; break;
                case "clear-module-data": output.ClearModuleData = RequireValue(raw, value); break;
                case "pack-module": output.PackModule = RequireValue(raw, value); break;
                case "verify-module": output.VerifyModule = RequireValue(raw, value); break;
                case "hof-file": output.HofFiles.Add(RequireValue(raw, value)); break;
                case "hof-mode": output.HofMode = RequireValue(raw, value).ToLowerInvariant() switch { "add" => HofApplyMode.Add, "mirror" => HofApplyMode.Mirror, _ => throw new ArgumentException("/hof-mode requires add or mirror") }; break;
                case "spec": output.SpecFile = RequireValue(raw, value); output.LaunchRequested = true; break; case "list": output.List = RequireValue(raw, value); break; case "vehicle-scope": output.VehicleScope = RequireValue(raw, value); break;
                case "startup-timeout": output.StartupTimeout = ParseInt(raw, value, 1, 600); break; case "shutdown-timeout": output.ShutdownTimeout = ParseInt(raw, value, 1, 600); break; case "cooperative-shutdown": output.CooperativeShutdown = true; break; case "observe-seconds": output.ObserveSeconds = ParseInt(raw, value, 0, int.MaxValue); break;
                case "recovery-status": output.Recovery = true; break; case "recover": output.Recovery = true; output.Recover = true; break;
                // A flag an installed module may declare (resolved in BuildSpecAsync).
                default: if (RuntimeModuleProtocol.IsFlagName(key) && raw.StartsWith('/')) { output.UnresolvedFlags.Add(raw); break; } throw new ArgumentException("Unknown argument: " + raw);
            }
        }
        output.ResolveHierarchicalCommand();
        return output;
    }

    private static string RequireValue(string raw, string? value) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException(raw.Split(':', 2)[0] + " requires a value") : value;
    private static int ParseInt(string raw, string? value, int minimum, int maximum)
    {
        var name = raw.Split(':', 2)[0];
        if (!int.TryParse(RequireValue(raw, value), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out var parsed)) throw new ArgumentException(name + " requires an integer");
        if (parsed < minimum || parsed > maximum) throw new ArgumentException(name + " must be between " + minimum + " and " + (maximum == int.MaxValue ? "2147483647" : maximum.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        return parsed;
    }

    // "." means the directory containing OmsiLaunch.exe, never the caller's
    // working folder; a portable Release example relies on that.
    public static string ResolveInstallationRoot(string? installation)
    {
        var root = string.IsNullOrWhiteSpace(installation) || installation == "." ? AppContext.BaseDirectory : installation;
        return Path.GetFullPath(root);
    }

    private void ResolveHierarchicalCommand()
    {
        if (RuntimeOperation is not null || CommandWords.Count == 0) return;
        if (Command is "session" or "events" or "help") return;
        var words = Command is null ? CommandWords : new[] { Command }.Concat(CommandWords).ToList();
        var route = string.Join(' ', words.Select(x => x.ToLowerInvariant()));
        RuntimeOperation = HierarchicalRoutes.TryGetValue(route, out var operation) ? operation : throw new ArgumentException("Unknown public command route: " + route);
    }

    // True when the command plans a session, the only place module flags are
    // resolved; anywhere else an unknown flag is an error.
    public bool PlansSession => !Version && !Help && Command is null && RuntimeOperation is null && !Recovery && List is null && ClearModuleData is null && (LaunchRequested || SpecFile is not null || !string.IsNullOrWhiteSpace(Installation));

    public void RejectUnresolvedFlags()
    {
        if (UnresolvedFlags.Count != 0 && !PlansSession) throw new ArgumentException("Unknown argument: " + UnresolvedFlags[0]);
    }

    // /<flag>[:<value>] -> "<module-id>.<key>" for a flag a module declares.
    // A boolean setting's flag without a value means true.
    private Dictionary<string, string> ResolveModuleFlags(string installationRoot)
    {
        var settings = new Dictionary<string, string>(ModuleSettings, StringComparer.Ordinal);
        if (UnresolvedFlags.Count == 0) return settings;
        var declared = ModuleCatalog.ReadDeclarations(installationRoot)
            .SelectMany(module => module.EffectiveSettings.Where(setting => setting.Flag is not null).Select(setting => (Module: module, Setting: setting))).ToArray();
        foreach (var raw in UnresolvedFlags)
        {
            var pair = raw.TrimStart('/').Split(':', 2);
            var flag = pair[0].ToLowerInvariant();
            var matches = declared.Where(x => x.Setting.Flag == flag).ToArray();
            if (matches.Length == 0) throw new ArgumentException("Unknown argument: " + raw);
            if (matches.Length > 1) throw new ArgumentException(PublicErrorCodes.MODULE_SETTING_INVALID + ": /" + flag + " is declared by several modules: " + string.Join(", ", matches.Select(x => x.Module.Id)));
            var (module, setting) = matches[0];
            var value = pair.Length == 2 ? pair[1] : setting.Type == ModuleSettingType.Boolean ? "true" : throw new ArgumentException("/" + flag + " requires a value");
            settings[module.Id + "." + setting.Key] = value;
        }
        return settings;
    }

    public async Task<LaunchSpec> BuildSpecAsync()
    {
        LaunchSpec seed = Defaults(Installation ?? AppContext.BaseDirectory);
        if (SpecFile is not null) seed = await LaunchSpecJson.LoadAsync(SpecFile);
        // An explicit installation argument wins over the spec's RootPath.
        var installationRoot = ResolveInstallationRoot(Installation ?? seed.Installation.RootPath);
        var moduleSettings = ResolveModuleFlags(installationRoot);
        SessionProfilePackage? selectedProfile = null;
        if (PredefinedProfile is not null)
        {
            if (PredefinedProfileIndex is null) throw new ArgumentException("OL_E_SESSION_PROFILE_PRESET_NOT_FOUND: /predefined-profile-index is required.");
            var package = SessionProfileCompiler.Load(installationRoot, PredefinedProfile, PredefinedProfileIndex.Value);
            RejectProfileConflicts(package, moduleSettings);
            selectedProfile = package;
            seed = seed with { Installation = new InstallationSpec(installationRoot), World = new WorldSpec(WorldMode, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset, OptionalValue<string>.Unset) };
            seed = SessionProfileCompiler.Apply(package, seed, WorldMode);
        }
        var vehicle = NoVehicle ? OptionalValue<PlayerVehicleSpec>.Unset : VehicleSpec(seed.PlayerVehicle);
        var settings = new Dictionary<string, OptionalValue<string>>(seed.Environment.General, StringComparer.OrdinalIgnoreCase);
        foreach (var pair in Settings)
        {
            if (!ConfigurationCatalog.TryGet(pair.Key, out var setting)) throw new ArgumentException("OL_E_UNKNOWN_SETTING: " + pair.Key);
            if (!setting.Writable) throw new InvalidOperationException("OL_E_SETTING_NOT_WRITABLE: " + pair.Key);
            settings[pair.Key] = OptionalValue<string>.Set(pair.Value);
        }
        var entrypointIdentity = Text(EntrypointIdentity, seed.World.EntrypointIdentity);
        var entrypointIndex = EntrypointIdentity is not null
            ? OptionalValue<int>.Unset
            : EntrypointIndex is { } index ? OptionalValue<int>.Set(index) : seed.World.PresentedEntrypointIndex;
        if (WorldMode == WorldMode.SavedSituation && (Map is not null || EntrypointIndex is not null || EntrypointIdentity is not null)) throw new ArgumentException("SAVED_SITUATION derives map and position from the selected .osn; /map and /entrypoint are not valid with /saved.");
        var world = WorldMode == WorldMode.SavedSituation
            ? new WorldSpec(WorldMode.SavedSituation, OptionalValue<string>.Unset, Text(Situation, seed.World.SituationIdentity), OptionalValue<int>.Unset, OptionalValue<string>.Unset)
            : new WorldSpec(WorldMode, Text(Map, seed.World.MapIdentity), Text(Situation, seed.World.SituationIdentity), entrypointIndex, entrypointIdentity);
        var result = seed with { Installation = new InstallationSpec(installationRoot), World = world, Date = ParseDate(Date, seed.Date), Time = ParseTime(Time, seed.Time), Year = ParseYear(Year, seed.EffectiveYear), Weather = new(WeatherMode == WeatherMode.Unset ? seed.EffectiveWeather.Mode : WeatherMode, Text(Weather, seed.EffectiveWeather.Preset), Text(Icao, seed.EffectiveWeather.Icao)), PlayerVehicle = vehicle, Environment = seed.Environment with { General = settings }, Behavior = seed.Behavior with { StartupTimeoutSeconds = StartupTimeout ?? seed.Behavior.StartupTimeoutSeconds, ShutdownTimeoutSeconds = ShutdownTimeout ?? seed.Behavior.ShutdownTimeoutSeconds, CooperativeShutdown = CooperativeShutdown || seed.Behavior.CooperativeShutdown }, Presentation = new SessionPresentationSpec(SplashSpecified ? Splash : seed.EffectivePresentation.Splash, Text(SplashLanguage, seed.EffectivePresentation.Language), Text(SplashAssets, seed.EffectivePresentation.CustomAssetDirectory), seed.EffectivePresentation.SuppressTrayIcon), InternetTextures = new InternetTexturesSpec(InternetTexturesSpecified ? InternetTextures : seed.EffectiveInternetTextures.Mode, Text(InternetTexturesProfile, seed.EffectiveInternetTextures.OverrideProfilePath)), Diagnostics = new DiagnosticsSpec(Log || seed.EffectiveDiagnostics.Log, Verbose || LogAll || seed.EffectiveDiagnostics.Verbose, OmsiLogAll || seed.EffectiveDiagnostics.OmsiLogAll, TraceProcess || LogAll || seed.EffectiveDiagnostics.ProcessTrace, TracePlugin || LogAll || seed.EffectiveDiagnostics.PluginTrace, TraceNative || LogAll || seed.EffectiveDiagnostics.NativeTrace) };
        // /module and /no-module add to what a /spec file or profile asked
        // for; a module setting from the command line wins over the /spec file.
        if (Modules.Count != 0 || DisabledModules.Count != 0 || moduleSettings.Count != 0)
        {
            var current = result.EffectiveModules;
            var mergedSettings = new Dictionary<string, string>(current.EffectiveSettings, StringComparer.Ordinal);
            foreach (var pair in moduleSettings) mergedSettings[pair.Key] = pair.Value;
            result = result with { Modules = new(current.Enable.Concat(Modules).Distinct(StringComparer.Ordinal).ToArray(), current.EffectiveDisable.Concat(DisabledModules).Distinct(StringComparer.Ordinal).ToArray(), mergedSettings) };
        }
        if (HofSource is null && (HofTargets.Count != 0 || HofFiles.Count != 0 || HofMode is not null)) throw new ArgumentException("/hof-target, /hof-file and /hof-mode require /hof-source.");
        if (HofSource is not null)
        {
            if (HofTargets.Count == 0) throw new ArgumentException("/hof-source requires at least one /hof-target.");
            // Command-line HOF flags replace the operations of a /spec file.
            result = result with { HofManagement = new(new[] { new HofOperationSpec(HofSourceKind!.Value, HofSource, HofTargets.ToArray(), HofMode ?? HofApplyMode.Add, HofFiles.Count == 0 ? null : HofFiles.ToArray()) }) };
        }
        if (selectedProfile is not null && WorldMode != WorldMode.NewMap) SessionProfileCompiler.ValidateCompatibility(selectedProfile, installationRoot, result.World, WorldMode);
        return result;
    }

    // Set from /quiet: human-readable success output is suppressed; JSON and errors are not.
    public static bool QuietOutput { get; set; }
    public static void Write<T>(T value, bool json) { if ((!WindowsHost.SuppressConsole && !QuietOutput) || json) Console.WriteLine(json ? JsonSerializer.Serialize(value, Json) : value is SessionPlan plan ? $"Plan: {(plan.IsRunnable ? "READY" : "NOT RUNNABLE")} profile={plan.BuildProfileId}" : JsonSerializer.Serialize(value, Json)); }
    public static void WriteEnvelope(string command, object result, bool json, IReadOnlyDictionary<string, string>? metadata = null)
    {
        if ((WindowsHost.SuppressConsole || QuietOutput) && !json) return;
        if (json) { Console.WriteLine(metadata is null ? JsonSerializer.Serialize(new { ok = true, command, protocol_version = PublicCapabilityRegistry.ProtocolVersion, result }, Json) : JsonSerializer.Serialize(new { ok = true, command, protocol_version = PublicCapabilityRegistry.ProtocolVersion, result, metadata }, Json)); return; }
        Console.WriteLine(JsonSerializer.Serialize(result, Json));
        if (metadata is not null && metadata.TryGetValue("events_dropped_count", out var dropped)) Console.WriteLine("Note: " + dropped + " older events were omitted to fit the control frame.");
    }
    public static void WriteError(string command, string code, string message, bool json, string category = PublicErrorCategory.Runtime) { if (!WindowsHost.SuppressConsole || json) Console.WriteLine(json ? JsonSerializer.Serialize(new { ok = false, command, protocol_version = PublicCapabilityRegistry.ProtocolVersion, error = new { code, category, message } }, Json) : $"{code}: {message}"); WindowsHost.ShowFailure(code, message); }
    public static object HelpFor(string? family) => new
    {
        usage = Usage,
        product_version = CliProgram.ProductVersion,
        protocol_version = PublicCapabilityRegistry.ProtocolVersion,
        family = string.IsNullOrWhiteSpace(family) ? null : family,
        commands = PublicCapabilityRegistry.All
            .Where(x => x.Classification is PublicCapabilityClassification.PublicStableBeta or PublicCapabilityClassification.PublicExperimental)
            .Where(x => x.CliRoute.Length > 0 && (string.IsNullOrWhiteSpace(family) || x.Family.Equals(family, StringComparison.OrdinalIgnoreCase)))
            .Select(x => new { x.CliRoute, x.Description, x.Classification, x.RuntimeValidation })
    };
    private static LaunchSpec Defaults(string root) { var none = new Dictionary<string, OptionalValue<string>>(); return new(new(root), new(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new()); }
    private void RejectProfileConflicts(SessionProfilePackage profile, IReadOnlyDictionary<string, string> moduleSettings)
    {
        var ownsWorld = WorldMode == WorldMode.NewMap;
        var worldConflict = ownsWorld &&
            ((Map is not null && profile.New.Map is not null) ||
             ((EntrypointIndex is not null || EntrypointIdentity is not null) && (profile.New.EntrypointIndex is not null || profile.New.EntrypointIdentity is not null)) ||
             (Date is not null && profile.New.Date is not null) ||
             (Time is not null && profile.New.Time is not null) ||
             (Year is not null && profile.New.Year is not null) ||
             ((WeatherMode != WeatherMode.Unset || Weather is not null || Icao is not null) && profile.New.Weather is not null));
        var settingConflict = Settings.Keys.Any(profile.Preset.Settings.ContainsKey);
        var presentationConflict = profile.Preset.Presentation is not null && (SplashSpecified || SplashLanguage is not null || SplashAssets is not null);
        var internetConflict = profile.Preset.InternetTextures is not null && (InternetTexturesSpecified || InternetTexturesProfile is not null);
        var behaviorConflict = profile.Preset.Behavior is not null && (StartupTimeout is not null || ShutdownTimeout is not null);
        var hofConflict = profile.Preset.Hofs.Count != 0 && HofSource is not null;
        // A module the preset switches on or off, or a module setting it sets.
        var presetModules = profile.Preset.Modules;
        var moduleConflict = presetModules is not null && (
            Modules.Any(presetModules.EffectiveDisable.Contains) || DisabledModules.Any(presetModules.Enable.Contains) ||
            moduleSettings.Keys.Any(presetModules.EffectiveSettings.ContainsKey));
        if (worldConflict || settingConflict || presentationConflict || internetConflict || behaviorConflict || hofConflict || moduleConflict)
            throw new ArgumentException("OL_E_SESSION_PROFILE_OVERRIDE_CONFLICT: An explicit argument attempts to override a field owned by the selected session profile.");
    }
    private OptionalValue<PlayerVehicleSpec> VehicleSpec(OptionalValue<PlayerVehicleSpec> old) { if (Vehicle is null && Repaint is null && Hof is null && Fleet is null && Registration is null) return old; var current = old.IsSet ? old.Value! : new(OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<string>.Unset); return OptionalValue<PlayerVehicleSpec>.Set(new(Text(Vehicle, current.Model), Text(Repaint, current.Repaint), Text(Hof, current.Hof), Text(Fleet, current.FleetNumber), Text(Registration, current.Registration))); }
    private static OptionalValue<string> Text(string? value, OptionalValue<string> fallback) => value is null ? fallback : OptionalValue<string>.Set(value);
    private static DateSpec ParseDate(string? value, DateSpec fallback) { if (value is null) return fallback; if (value.Equals("system", StringComparison.OrdinalIgnoreCase)) return new(DateTimeMode.System, OptionalValue<SemanticDate>.Unset); var parsed = System.DateOnly.Parse(value); return new(DateTimeMode.Explicit, OptionalValue<SemanticDate>.Set(new(parsed.Year, parsed.Month, parsed.Day))); }
    private static TimeSpec ParseTime(string? value, TimeSpec fallback) { if (value is null) return fallback; if (value.Equals("system", StringComparison.OrdinalIgnoreCase)) return new(DateTimeMode.System, OptionalValue<SemanticTime>.Unset); var parsed = System.TimeOnly.Parse(value); return new(DateTimeMode.Explicit, OptionalValue<SemanticTime>.Set(new(parsed.Hour, parsed.Minute, parsed.Second))); }
    private static YearSpec ParseYear(string? value, YearSpec fallback) => value is null ? fallback : value.Equals("system", StringComparison.OrdinalIgnoreCase) ? new(DateTimeMode.System, OptionalValue<int>.Unset) : new(DateTimeMode.Explicit, OptionalValue<int>.Set(int.Parse(value)));
}

internal static class CliEventWatch
{
    public static async Task<int> RunAsync(CliInput input, string installationRoot)
    {
        using var cancellation = new CancellationTokenSource();
        Console.CancelKeyPress += (_, eventArgs) => { eventArgs.Cancel = true; cancellation.Cancel(); };
        long lastSequence = 0;
        try
        {
            while (!cancellation.IsCancellationRequested)
            {
                var response = await LocalControlPlane.TryRequestAsync(installationRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.events"), TimeSpan.FromMilliseconds(750));
                if (response is null)
                {
                    CliInput.WriteError("events watch", "OL_E_NO_ACTIVE_SESSION", "No active OmsiLaunch control instance was found.", input.JsonOutput);
                    return (int)PublicExitCode.NoActiveSession;
                }
                if (!response.Ok)
                {
                    CliInput.WriteError("events watch", response.ErrorCode ?? "OL_E_CONTROL_FAILED", response.Message ?? "Control request failed.", input.JsonOutput);
                    return (int)PublicExitCode.OperationRejected;
                }
                if (response.Result is JsonElement events && events.ValueKind == JsonValueKind.Array)
                {
                    foreach (var runtimeEvent in events.EnumerateArray())
                    {
                        if (!runtimeEvent.TryGetProperty("Sequence", out var sequence) || sequence.GetInt64() <= lastSequence) continue;
                        lastSequence = sequence.GetInt64();
                        CliInput.WriteEnvelope("events.watch", runtimeEvent, input.JsonOutput);
                    }
                }
                await Task.Delay(250, cancellation.Token).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) { }
        return (int)PublicExitCode.Success;
    }
}

internal sealed record RuntimeBatchResult(string Operation, Guid SessionId, ulong RequestId, string Profile, bool Succeeded, string? ErrorCode, IReadOnlyDictionary<string, string>? Values, long LatencyMilliseconds, string ExecutionPath);
internal static class RuntimeBatch
{
    public static async Task<IReadOnlyList<RuntimeBatchResult>> ExecuteAsync(IOmsiLaunch launch, SessionHandle session, string profile, bool includeTimeWrite)
    {
        var results = new List<RuntimeBatchResult>(); ulong requestId = 1;
        foreach (var operation in new[] { "time.read", "map.read", "weather.read", "weather.actual.read", "camera.read", "road-vehicles.read", "player-vehicle.read", "humans.read", "timetable.read", "timetable.tracks.list", "timetable.trips.list", "timetable.lines.list", "timetable.rv-files.list", "timetable.track-entries.list", "timetable.bus-stops.list", "timetable.station-links.list", "timetable.tours.list", "timetable.profiles.list", "timetable.tour-entries.list", "drivers.read", "tickets.read", "timetable.logs.read" })
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, operation), TimeSpan.FromSeconds(5));
                results.Add(new RuntimeBatchResult(operation, session.SessionId, result.RequestId, profile, result.Succeeded, result.ErrorCode, result.Values, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(operation, session.SessionId, requestId - 1, profile, false, exception.Message, null, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
        }
        var vehicleHandle = await ExecuteEntitySnapshotAsync("road-vehicles.list", "vehicle.0.handle", "road-vehicle.read", "handle");
        if (vehicleHandle is not null)
        {
            await ExecuteNamedValueAsync("vehicle.variables.list", "vehicle.variable.get", vehicleHandle);
            await ExecuteNamedValueAsync("vehicle.string-variables.list", "vehicle.string-variable.get", vehicleHandle);
            await ExecuteNamedValueAsync("vehicle.constants.list", "vehicle.constant.get", vehicleHandle);
            await ExecuteFirstCurveAsync(vehicleHandle);
            await ExecuteAsync("vehicle.hofs.read", new Dictionary<string, string> { ["handle"] = vehicleHandle }, "vehicle.hofs.read");
            if (includeTimeWrite)
            {
                await ExecuteAsync("vehicle.variable.set", new Dictionary<string, string> { ["handle"] = vehicleHandle, ["name"] = "Refresh_Strings", ["value"] = "1" }, "vehicle.variable.set");
                await ExecuteAsync("vehicle.variable.set.restore", new Dictionary<string, string> { ["handle"] = vehicleHandle, ["name"] = "Refresh_Strings", ["value"] = "0" }, "vehicle.variable.set");
            }
        }
        await ExecuteEntitySnapshotAsync("humans.list", "human.0.handle", "human.read", "handle");
        if (includeTimeWrite)
        {
            var baseline = results.SingleOrDefault(result => result.Operation == "time.read" && result.Succeeded)?.Values;
            if (baseline is null || !baseline.TryGetValue("minute", out var text) || !byte.TryParse(text, out var minute))
            {
                results.Add(new RuntimeBatchResult("time.set", session.SessionId, requestId++, profile, false, "OL_E_RUNTIME_BASELINE_UNAVAILABLE", null, 0, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop -> Native.x86"));
            }
            else
            {
                var changedMinute = (byte)((minute + 1) % 60);
                await ExecuteAsync("time.set", new Dictionary<string, string> { ["minute"] = changedMinute.ToString(System.Globalization.CultureInfo.InvariantCulture) });
                await ExecuteAsync("time.set.restore", new Dictionary<string, string> { ["minute"] = minute.ToString(System.Globalization.CultureInfo.InvariantCulture) }, "time.set");
            }

            var camera = results.SingleOrDefault(result => result.Operation == "camera.read" && result.Succeeded)?.Values;
            if (camera is null || !camera.TryGetValue("field_of_view", out var fovText) || !float.TryParse(fovText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var fov))
            {
                results.Add(new RuntimeBatchResult("camera.set", session.SessionId, requestId++, profile, false, "OL_E_RUNTIME_BASELINE_UNAVAILABLE", null, 0, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            else
            {
                var changedFov = Math.Abs(fov - 46f) < 0.001f ? 45f : 46f;
                await ExecuteAsync("camera.set", new Dictionary<string, string> { ["field_of_view"] = changedFov.ToString(System.Globalization.CultureInfo.InvariantCulture) }, "camera.set");
                await ExecuteAsync("camera.set.restore", new Dictionary<string, string> { ["field_of_view"] = fov.ToString(System.Globalization.CultureInfo.InvariantCulture) }, "camera.set");
            }
        }
        return results;

        async Task ExecuteAsync(string label, IReadOnlyDictionary<string, string> arguments, string operation = "time.set")
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, operation, arguments), TimeSpan.FromSeconds(5));
                results.Add(new RuntimeBatchResult(label, session.SessionId, result.RequestId, profile, result.Succeeded, result.ErrorCode, result.Values, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop -> Native.x86"));
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(label, session.SessionId, requestId - 1, profile, false, exception.Message, null, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop -> Native.x86"));
            }
        }

        async Task<string?> ExecuteEntitySnapshotAsync(string listOperation, string handleKey, string readOperation, string argumentName)
        {
            var started = System.Diagnostics.Stopwatch.StartNew(); RuntimeCommandResult? list = null;
            try
            {
                list = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, listOperation), TimeSpan.FromSeconds(5));
                results.Add(new RuntimeBatchResult(listOperation, session.SessionId, list.RequestId, profile, list.Succeeded, list.ErrorCode, list.Values, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(listOperation, session.SessionId, requestId - 1, profile, false, exception.Message, null, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            if (list?.Succeeded != true || list.Values is null || !list.Values.TryGetValue(handleKey, out var handle)) return null;
            var readStarted = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var read = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, readOperation, new Dictionary<string, string> { [argumentName] = handle }), TimeSpan.FromSeconds(5));
                results.Add(new RuntimeBatchResult(readOperation, session.SessionId, read.RequestId, profile, read.Succeeded, read.ErrorCode, read.Values, readStarted.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(readOperation, session.SessionId, requestId - 1, profile, false, exception.Message, null, readStarted.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            return handle;
        }

        async Task ExecuteNamedValueAsync(string listOperation, string readOperation, string handle)
        {
            var started = System.Diagnostics.Stopwatch.StartNew(); RuntimeCommandResult? list = null;
            try
            {
                list = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, listOperation, new Dictionary<string, string> { ["handle"] = handle }), TimeSpan.FromSeconds(5));
                results.Add(new RuntimeBatchResult(listOperation, session.SessionId, list.RequestId, profile, list.Succeeded, list.ErrorCode, list.Values, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(listOperation, session.SessionId, requestId - 1, profile, false, exception.Message, null, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            }
            if (list?.Succeeded != true || list.Values is null || !list.Values.TryGetValue("name.0", out var name)) return;
            await ExecuteAsync(readOperation, new Dictionary<string, string> { ["handle"] = handle, ["name"] = name }, readOperation);
        }

        async Task ExecuteFirstCurveAsync(string handle)
        {
            var list = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, "vehicle.curves.list", new Dictionary<string, string> { ["handle"] = handle }), TimeSpan.FromSeconds(5));
            results.Add(new RuntimeBatchResult("vehicle.curves.list", session.SessionId, list.RequestId, profile, list.Succeeded, list.ErrorCode, list.Values, 0, "host -> session mailbox -> PluginRuntime UI timer -> profiled Interop"));
            if (list.Succeeded && list.Values is not null && list.Values.TryGetValue("name.0", out var name)) await ExecuteAsync("vehicle.curve.evaluate", new Dictionary<string, string> { ["handle"] = handle, ["name"] = name, ["x"] = "0" }, "vehicle.curve.evaluate");
        }
    }

    public static void WriteArtifact(string root, Guid sessionId, string profile, IReadOnlyList<RuntimeBatchResult> results, bool includesWrite)
    {
        var directory = Path.Combine(root, ".omsilaunch", "diagnostics"); Directory.CreateDirectory(directory);
        var suffix = includesWrite ? "runtime-write-batch" : "runtime-read-batch";
        var path = Path.Combine(directory, sessionId.ToString("N") + "-" + suffix + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(new { session_id = sessionId, build_profile = profile, utc = DateTimeOffset.UtcNow, operations = results }, CliInput.Json));
    }
}

internal static class D3DValidationBatch
{
    public static async Task<IReadOnlyList<RuntimeBatchResult>> ExecuteAsync(IOmsiLaunch launch, SessionHandle session, string profile)
    {
        var results = new List<RuntimeBatchResult>();
        ulong requestId = 20_000;

        async Task<RuntimeCommandResult> ExecuteAsync(string operation, IReadOnlyDictionary<string, string>? arguments = null)
        {
            var started = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                var result = await launch.ExecuteRuntimeAsync(session, new RuntimeCommand(session.SessionId, requestId++, operation, arguments), TimeSpan.FromSeconds(8));
                results.Add(new RuntimeBatchResult(operation, session.SessionId, result.RequestId, profile, result.Succeeded, result.ErrorCode, result.Values, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime main/render thread -> Native.x86 D3D9"));
                return result;
            }
            catch (Exception exception)
            {
                results.Add(new RuntimeBatchResult(operation, session.SessionId, requestId - 1, profile, false, exception.Message, null, started.ElapsedMilliseconds, "host -> session mailbox -> PluginRuntime main/render thread -> Native.x86 D3D9"));
                return new RuntimeCommandResult(session.SessionId, requestId - 1, false, exception.Message);
            }
        }

        async Task ExpectReleasedAsync(string operation, IReadOnlyDictionary<string, string> arguments)
        {
            var result = await ExecuteAsync(operation, arguments);
            if (result.Succeeded || !string.Equals(result.ErrorCode, "OL_E_D3D_RESOURCE_RELEASED", StringComparison.Ordinal))
                return;

            // A released opaque handle must be rejected; record this as a passed
            // validation assertion rather than treating the intended rejection as a batch failure.
            results[^1] = new RuntimeBatchResult(
                operation + ".released-rejection",
                session.SessionId,
                result.RequestId,
                profile,
                true,
                null,
                new Dictionary<string, string> { ["expected_error"] = result.ErrorCode! },
                results[^1].LatencyMilliseconds,
                "host -> session mailbox -> PluginRuntime main/render thread -> Native.x86 D3D9");
        }

        await ExecuteAsync("d3d.status");
        var first = await ExecuteAsync("d3d.texture.create", new Dictionary<string, string> { ["width"] = "8", ["height"] = "8", ["format"] = "A8R8G8B8", ["levels"] = "1" });
        if (first.Succeeded && first.Values is not null && first.Values.TryGetValue("handle", out var firstHandle))
        {
            await ExecuteAsync("d3d.texture.describe", new Dictionary<string, string> { ["handle"] = firstHandle, ["level"] = "0" });
            var fullPixels = Enumerable.Range(0, 64).SelectMany(index => new byte[] { (byte)index, (byte)(255 - index), 0x55, 0xFF }).ToArray();
            await ExecuteAsync("d3d.texture.update", new Dictionary<string, string> { ["handle"] = firstHandle, ["level"] = "0", ["x"] = "0", ["y"] = "0", ["width"] = "8", ["height"] = "8", ["pixels_base64"] = Convert.ToBase64String(fullPixels) });
            var rectPixels = Enumerable.Repeat(new byte[] { 0x10, 0x20, 0x30, 0xFF }, 4).SelectMany(value => value).ToArray();
            await ExecuteAsync("d3d.texture.update", new Dictionary<string, string> { ["handle"] = firstHandle, ["level"] = "0", ["x"] = "2", ["y"] = "3", ["width"] = "2", ["height"] = "2", ["pixels_base64"] = Convert.ToBase64String(rectPixels) });

            var second = await ExecuteAsync("d3d.texture.create", new Dictionary<string, string> { ["width"] = "4", ["height"] = "4", ["format"] = "X8R8G8B8", ["levels"] = "1" });
            await ExecuteAsync("d3d.texture.release", new Dictionary<string, string> { ["handle"] = firstHandle });
            await ExpectReleasedAsync("d3d.texture.describe", new Dictionary<string, string> { ["handle"] = firstHandle, ["level"] = "0" });
            await ExpectReleasedAsync("d3d.texture.release", new Dictionary<string, string> { ["handle"] = firstHandle });
            if (second.Succeeded && second.Values is not null && second.Values.TryGetValue("handle", out var secondHandle))
                await ExecuteAsync("d3d.texture.release", new Dictionary<string, string> { ["handle"] = secondHandle });
        }
        for (var cycle = 0; cycle < 3; cycle++)
        {
            var created = await ExecuteAsync("d3d.texture.create", new Dictionary<string, string> { ["width"] = "2", ["height"] = "2", ["format"] = "A8R8G8B8", ["levels"] = "1" });
            if (created.Succeeded && created.Values is not null && created.Values.TryGetValue("handle", out var handle))
                await ExecuteAsync("d3d.texture.release", new Dictionary<string, string> { ["handle"] = handle });
        }
        await ExecuteAsync("d3d.status");
        return results;
    }

    public static void WriteArtifact(string root, Guid sessionId, string profile, IReadOnlyList<RuntimeBatchResult> results)
    {
        var directory = Path.Combine(root, ".omsilaunch", "diagnostics");
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path.Combine(directory, sessionId.ToString("N") + "-d3d-wave-d-batch.json"), JsonSerializer.Serialize(new
        {
            session_id = sessionId,
            build_profile = profile,
            utc = DateTimeOffset.UtcNow,
            operations = results
        }, CliInput.Json));
    }
}
