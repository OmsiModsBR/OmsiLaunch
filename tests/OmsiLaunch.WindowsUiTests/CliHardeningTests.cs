using System.Text;
using System.Text.Json;
using OmsiLaunch.Api;

// CLI behaviour locked down by the hardening round (findings S-03, S-06, S-14, S-20).
internal static class CliHardeningTests
{
    public static void Run()
    {
        ParseErrors(); Console.WriteLine("PASS cli.parse-errors");
        ModuleFlags(); Console.WriteLine("PASS cli.module-flags");
        SpecTimeoutsAndUnknownProperties(); Console.WriteLine("PASS cli.spec-strict-loading");
        ExitCodeClassification(); Console.WriteLine("PASS cli.exit-code-classification");
        ControlPlane(); Console.WriteLine("PASS control-plane.binding-and-typed-errors");
        ControlPlaneNeverSilent(); Console.WriteLine("PASS control-plane.errors-never-silent");
        OwnerLifecycle(); Console.WriteLine("PASS owner.close-on-every-exit-path");
        SilentDelegationDoesNotInheritHandles(); Console.WriteLine("PASS cli.silent-delegation-no-handle-inheritance");
        Console.WriteLine(TrayMenuTakesForeground() ? "PASS tray.menu-takes-foreground" : "SKIP tray.menu-takes-foreground (no interactive desktop)");
        WindowsHostReportsNonRunnablePlan(); Console.WriteLine("PASS windows-host.non-runnable-plan-is-reported");
    }

    // Documentation audit BUG-06: under OmsiLaunchW.exe a launch whose plan is
    // not runnable must show the failure dialog instead of ending silently.
    private static void WindowsHostReportsNonRunnablePlan()
    {
        var plugins = Path.Combine(AppContext.BaseDirectory, "plugins");
        var createdPlugins = !Directory.Exists(plugins);
        var stubs = new[] { Path.Combine(plugins, "OmsiLaunch.Plugin.opl"), Path.Combine(plugins, "OmsiLaunch.Native.x86.dll") }.Where(path => !File.Exists(path)).ToArray();
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-WHost-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        var previous = Environment.GetEnvironmentVariable("OMSILAUNCH_WINDOWS_HOST");
        var reported = new List<(string Code, string Message)>();
        try
        {
            Directory.CreateDirectory(plugins); foreach (var stub in stubs) File.WriteAllText(stub, "stub");
            Environment.SetEnvironmentVariable("OMSILAUNCH_WINDOWS_HOST", "1");
            WindowsHost.FailureObserver = (code, message) => reported.Add((code, message));
            var args = new[] { root, "/new", "/map:maps\\Grundorf\\global.cfg", "/entrypoint-index:1" };
            var exit = CliProgram.RunAsync(CliInput.Parse(args), args).GetAwaiter().GetResult();
            Assert(exit == (int)PublicExitCode.SessionFailed, "a non-runnable launch must exit 1, got " + exit);
            Assert(reported.Count == 1 && reported[0].Code.StartsWith("OL_E_", StringComparison.Ordinal), "the non-runnable launch was not reported by the Windows host");
        }
        finally
        {
            WindowsHost.FailureObserver = null;
            Environment.SetEnvironmentVariable("OMSILAUNCH_WINDOWS_HOST", previous);
            foreach (var stub in stubs) File.Delete(stub);
            if (createdPlugins) Directory.Delete(plugins, true);
            Directory.Delete(root, true);
        }
    }

    // Runtime closure BUG-04: a notification-icon menu must be shown with its
    // owner as the foreground window, or it ignores clicks and never closes
    // while another application is in front. The right-click is delivered as
    // the NotifyIcon callback message the shell sends.
    private static bool TrayMenuTakesForeground()
    {
        if (!Environment.UserInteractive) return false;
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Tray-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var spec = new LaunchSpec(new InstallationSpec(root), new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set("maps\\Grundorf\\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)), new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new EnvironmentSpec(new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>()), new LaunchBehaviorSpec());
            var plan = new SessionPlan(Guid.NewGuid(), "Omsi23004_692EBFBF", spec, new RuntimePlatformInfo("Windows", "", "X64", "X64", "X86", "X86", true, false, true, true, true, true, true, true, true), Array.Empty<ContentIdentity>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<Capability>(), Array.Empty<Capability>(), Array.Empty<PlannedMutation>(), Array.Empty<LaunchDiagnostic>(), true);
            using var indicator = SessionTrayIndicator.CreateIfAvailable(root, plan, () => new SessionStatus(plan.SessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>()), () => { });
            if (indicator is null) return false;
            var self = (uint)Environment.ProcessId;
            var candidates = new List<IntPtr>();
            EnumWindows((window, _) => { GetWindowThreadProcessId(window, out var pid); var name = new StringBuilder(256); GetClassName(window, name, 256); if (pid == self && name.ToString().StartsWith("WindowsForms10.Window", StringComparison.Ordinal) && GetWindowTextLength(window) == 0) candidates.Add(window); return true; }, IntPtr.Zero);
            foreach (var window in candidates) { PostMessage(window, 0x800, IntPtr.Zero, (IntPtr)0x204); PostMessage(window, 0x800, IntPtr.Zero, (IntPtr)0x205); }
            var deadline = DateTime.UtcNow.AddSeconds(3); var foregroundIsOwn = false;
            while (DateTime.UtcNow < deadline && !foregroundIsOwn) { Thread.Sleep(100); GetWindowThreadProcessId(GetForegroundWindow(), out var pid); foregroundIsOwn = pid == self; }
            Assert(foregroundIsOwn, "the tray menu was shown without making its owner the foreground window");
            return true;
        }
        finally { try { Directory.Delete(root, true); } catch (IOException) { } }
    }
    private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
    [System.Runtime.InteropServices.DllImport("user32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode)] private static extern int GetClassName(IntPtr window, StringBuilder name, int capacity);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern int GetWindowTextLength(IntPtr window);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    [System.Runtime.InteropServices.DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

    // Runtime closure BUG-03: the /silent host outlives the launcher, so it must
    // not inherit the launcher's handles (a caller capturing the launcher's
    // output would otherwise block until the whole session ended).
    private static void SilentDelegationDoesNotInheritHandles()
    {
        var cmd = Path.Combine(Environment.SystemDirectory, "cmd.exe");
        var info = CliProgram.SilentDelegation(cmd, new[] { "/c", "exit", "7", "/silent", "--SILENT" });
        Assert(info.UseShellExecute && !info.RedirectStandardInput && !info.RedirectStandardOutput && !info.RedirectStandardError, "delegation must use ShellExecute without redirection");
        Assert(info.ArgumentList.SequenceEqual(new[] { "/c", "exit", "7" }), "/silent was not stripped from the delegated arguments");
        info.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        using (var exited = System.Diagnostics.Process.Start(info)!) { exited.WaitForExit(10_000); Assert(exited.ExitCode == 7, "delegated arguments were not passed through ShellExecute"); }
        using var pipe = new System.IO.Pipes.AnonymousPipeServerStream(System.IO.Pipes.PipeDirection.In, HandleInheritability.Inheritable);
        var running = CliProgram.SilentDelegation(cmd, new[] { "/c", "ping", "-n", "5", "127.0.0.1", ">nul" }); running.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
        using var child = System.Diagnostics.Process.Start(running)!;
        pipe.DisposeLocalCopyOfClientHandle();
        var read = pipe.ReadAsync(new byte[1]).AsTask();
        var eofWhileChildRuns = read.Wait(TimeSpan.FromSeconds(2)) && read.Result == 0 && !child.HasExited;
        child.WaitForExit(15_000);
        Assert(eofWhileChildRuns, "the delegated process inherited an inheritable handle of the launcher");
    }

    // S-14: malformed values are argument errors, never null-reference or
    // overflow crashes, and ranges are enforced at parse time.
    private static void ParseErrors()
    {
        foreach (var arguments in new[] { new[] { "/set" }, new[] { "/runtime-arg" }, new[] { "/entrypoint-index:abc" }, new[] { "/entrypoint-index:99999999999" }, new[] { "/predefined-profile-index:0" }, new[] { "/predefined-profile-index:100" }, new[] { "/observe-seconds:-1" }, new[] { "/startup-timeout:0" }, new[] { "/map" }, new[] { "/saved" }, new[] { "/module-setting:noid=1" }, new[] { "/no-module" }, new[] { "/unknown flag" } })
        {
            try { CliInput.Parse(arguments); throw new InvalidOperationException("accepted " + string.Join(' ', arguments)); }
            catch (ArgumentException) { }
        }
        var parsed = CliInput.Parse(new[] { "C:\\OMSI", "/new", "/map:maps\\Grundorf\\global.cfg", "/entrypoint-index:1", "/startup-timeout:120", "--json", "vehicles", "summary" });
        Assert(parsed.StartupTimeout == 120 && parsed.ShutdownTimeout is null && parsed.JsonOutput && parsed.Installation == "C:\\OMSI", "parse regression");
        Assert(CliInput.Parse(new[] { "vehicles", "summary" }).RuntimeOperation == "road-vehicles.read" && CliInput.Parse(new[] { "humans", "summary" }).RuntimeOperation == "humans.read", "summary routes regression");
        Assert(CliInput.Parse(Array.Empty<string>()).StartupTimeout is null, "timeouts must stay unset by default");
        Assert(CliInput.Parse(new[] { "/predefined-profile:x", "/predefined-profile-index:99" }).PredefinedProfileIndex == 99 && CliInput.Parse(new[] { "/predefined-profile:x", "/predefined-profile-index:1" }).PredefinedProfileIndex == 1, "preset indexes 1 and 99 must parse");
    }

    // Runtime module flags: /no-module, /module-setting and the flags installed
    // modules declare. A flag no built-in knows is kept until a session is
    // planned, resolved against the installed modules there, and refused
    // anywhere else.
    private static void ModuleFlags()
    {
        var unknown = CliInput.Parse(new[] { "/unknown-flag" });
        Assert(unknown.UnresolvedFlags.SequenceEqual(new[] { "/unknown-flag" }), "an unknown flag is kept for module resolution");
        try { unknown.RejectUnresolvedFlags(); throw new InvalidOperationException("accepted /unknown-flag outside a session"); } catch (ArgumentException) { }
        var install = Path.Combine(Path.GetTempPath(), "OmsiLaunch-CliModules-" + Guid.NewGuid().ToString("N"));
        try
        {
            var module = Path.Combine(install, "plugins", "OmsiLaunch.Modules", "flagged");
            Directory.CreateDirectory(module);
            File.WriteAllText(Path.Combine(module, "module.json"), "{ \"schema\": \"omsilaunch.runtime-module/v1\", \"id\": \"flagged\", \"version\": \"1.0.0\", \"bridge_protocol\": { \"min\": 2, \"max\": 2 }, " +
                "\"components\": [ { \"kind\": \"managed\", \"path\": \"flagged.dll\", \"sha256\": \"" + new string('A', 64) + "\", \"entry\": \"Flagged.Module\" } ], \"failure_policy\": \"degrade\", " +
                "\"settings\": [ { \"key\": \"ram_mb\", \"type\": \"integer\", \"default\": \"auto\", \"auto\": true, \"flag\": \"flagged-ram\" }, { \"key\": \"monitor\", \"type\": \"boolean\", \"default\": false, \"flag\": \"flagged-monitor\" } ] }");
            var spec = CliInput.Parse(new[] { install, "/new", "/map:maps\\X\\global.cfg", "/flagged-ram:4096", "/flagged-monitor", "/no-module:other", "/module-setting:flagged.mode=fast", "/module:flagged" }).BuildSpecAsync().GetAwaiter().GetResult();
            var modules = spec.EffectiveModules;
            Assert(modules.EffectiveSettings["flagged.ram_mb"] == "4096" && modules.EffectiveSettings["flagged.monitor"] == "true" && modules.EffectiveSettings["flagged.mode"] == "fast", "module flags and /module-setting reach LaunchSpec.Modules.Settings");
            Assert(modules.Enable.SequenceEqual(new[] { "flagged" }) && modules.EffectiveDisable.SequenceEqual(new[] { "other" }), "/module and /no-module");
            try { CliInput.Parse(new[] { install, "/new", "/no-such-flag:1" }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("accepted a flag no module declares"); } catch (ArgumentException) { }
            try { CliInput.Parse(new[] { install, "/new", "/flagged-ram" }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("accepted an integer module flag without a value"); } catch (ArgumentException) { }
        }
        finally { try { Directory.Delete(install, recursive: true); } catch (IOException) { } }
    }

    // S-20: /spec values are honoured unless the command line overrides them,
    // unknown properties are rejected, and oversized files are refused.
    private static void SpecTimeoutsAndUnknownProperties()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-CliTests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var spec = Path.Combine(root, "spec.json");
            File.WriteAllText(spec, "{ \"Installation\": { \"RootPath\": \"" + root.Replace("\\", "\\\\") + "\" }, \"World\": { \"Mode\": 0, \"MapIdentity\": { \"Presence\": 1, \"Value\": \"maps\\\\Grundorf\\\\global.cfg\" }, \"SituationIdentity\": { \"Presence\": 0 }, \"PresentedEntrypointIndex\": { \"Presence\": 1, \"Value\": 1 } }, \"Date\": { \"Mode\": 0 }, \"Time\": { \"Mode\": 0 }, \"PlayerVehicle\": { \"Presence\": 0 }, \"Environment\": { \"General\": { \"graphics.maxFPS\": { \"Presence\": 1, \"Value\": \"40\" } }, \"Advanced\": {}, \"Graphics\": {}, \"AdvancedGraphics\": {}, \"Sound\": {}, \"AiPassengers\": {}, \"Keyboard\": {}, \"Controllers\": {} }, \"Behavior\": { \"StartupTimeoutSeconds\": 400, \"ShutdownTimeoutSeconds\": 45 }, \"Presentation\": { \"Splash\": 1, \"SuppressTrayIcon\": true } }");
            var fromSpec = CliInput.Parse(new[] { "/spec:" + spec }).BuildSpecAsync().GetAwaiter().GetResult();
            Assert(fromSpec.Behavior.StartupTimeoutSeconds == 400 && fromSpec.Behavior.ShutdownTimeoutSeconds == 45, "spec timeouts were overridden by CLI defaults");
            Assert(fromSpec.EffectivePresentation.SuppressTrayIcon && fromSpec.Environment.General["graphics.maxFPS"].Value == "40", "spec presentation/settings were not honoured");
            Assert(string.Equals(fromSpec.Installation.RootPath, root, StringComparison.OrdinalIgnoreCase), "spec root was not honoured");
            var overridden = CliInput.Parse(new[] { "/spec:" + spec, "/startup-timeout:120" }).BuildSpecAsync().GetAwaiter().GetResult();
            Assert(overridden.Behavior.StartupTimeoutSeconds == 120 && overridden.Behavior.ShutdownTimeoutSeconds == 45, "explicit CLI timeout must win over the spec");

            var typo = Path.Combine(root, "typo.json");
            File.WriteAllText(typo, File.ReadAllText(spec).Replace("\"Presentation\"", "\"Presentacion\""));
            try { CliInput.Parse(new[] { "/spec:" + typo }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("unknown spec property was accepted"); }
            catch (InvalidDataException error) when (error.Message.Contains("OL_E_SPEC_UNKNOWN_PROPERTY: $.Presentacion", StringComparison.Ordinal)) { }
            var nested = Path.Combine(root, "nested.json");
            File.WriteAllText(nested, File.ReadAllText(spec).Replace("\"SuppressTrayIcon\"", "\"SupressTrayIcon\""));
            try { CliInput.Parse(new[] { "/spec:" + nested }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("unknown nested spec property was accepted"); }
            catch (InvalidDataException error) when (error.Message.Contains("$.Presentation.SupressTrayIcon", StringComparison.Ordinal)) { }

            var huge = Path.Combine(root, "huge.json");
            using (var stream = File.Create(huge)) stream.SetLength(LaunchSpecJson.MaxBytes + 1);
            try { CliInput.Parse(new[] { "/spec:" + huge }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("oversized spec was accepted"); }
            catch (InvalidDataException error) when (error.Message.StartsWith("OL_E_SPEC_TOO_LARGE", StringComparison.Ordinal)) { }
            try { CliInput.Parse(new[] { "/spec:" + Path.Combine(root, "missing.json") }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("missing spec was accepted"); }
            catch (FileNotFoundException) { }

            // Beta 4 HOF management: /spec arrays of records are strict, and
            // command-line HOF flags replace the spec's operations.
            var hofSpec = Path.Combine(root, "hof.json");
            File.WriteAllText(hofSpec, File.ReadAllText(spec).TrimEnd().TrimEnd('}') + ", \"HofManagement\": { \"Operations\": [ { \"SourceKind\": 0, \"Source\": \"berlin\", \"TargetVehicles\": [ \"Vehicles\\\\B\\\\b.bus\" ], \"Mode\": 1 } ] } }");
            var withHof = CliInput.Parse(new[] { "/spec:" + hofSpec }).BuildSpecAsync().GetAwaiter().GetResult();
            Assert(withHof.EffectiveHofManagement.Operations.Single() is { SourceKind: HofSourceKind.Collection, Source: "berlin", Mode: HofApplyMode.Mirror } op && op.TargetVehicles.Single() == "Vehicles\\B\\b.bus", "spec HOF operation was not loaded");
            var hofTypo = Path.Combine(root, "hof-typo.json");
            File.WriteAllText(hofTypo, File.ReadAllText(hofSpec).Replace("\"TargetVehicles\"", "\"TargetVehicle\""));
            try { CliInput.Parse(new[] { "/spec:" + hofTypo }).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("unknown property inside a spec array was accepted"); }
            catch (InvalidDataException error) when (error.Message.Contains("$.HofManagement.Operations[0].TargetVehicle", StringComparison.Ordinal)) { }
            var fromFlags = CliInput.Parse(new[] { "/spec:" + hofSpec, "/hof-source:vehicle:Vehicles\\A\\a.bus", "/hof-target:Vehicles\\C", "/hof-target:Vehicles\\D\\d.ovh", "/hof-file:x.hof" }).BuildSpecAsync().GetAwaiter().GetResult();
            Assert(fromFlags.EffectiveHofManagement.Operations.Single() is { SourceKind: HofSourceKind.Vehicle, Source: "Vehicles\\A\\a.bus", Mode: HofApplyMode.Add } flagOp && flagOp.TargetVehicles.Count == 2 && flagOp.Files!.Single() == "x.hof", "HOF flags did not replace the spec operations");
            foreach (var arguments in new[] { new[] { "/hof-target:Vehicles\\C" }, new[] { "/hof-source:collection:berlin" }, new[] { "/hof-mode:mirror" } })
            {
                try { CliInput.Parse(arguments).BuildSpecAsync().GetAwaiter().GetResult(); throw new InvalidOperationException("incomplete HOF flags were accepted: " + string.Join(' ', arguments)); }
                catch (ArgumentException) { }
            }
            foreach (var bad in new[] { "/hof-source:berlin", "/hof-source:unknown:x", "/hof-source:0:x", "/hof-source:collection:", "/hof-mode:replace" })
            {
                try { CliInput.Parse(new[] { bad }); throw new InvalidOperationException("malformed HOF flag was accepted: " + bad); }
                catch (ArgumentException) { }
            }

            // Beta 4 Runtime Modules: /module adds to the spec's modules; a
            // module operation passes the frontend check (the session decides).
            var moduleSpec = Path.Combine(root, "modules.json");
            File.WriteAllText(moduleSpec, File.ReadAllText(spec).TrimEnd().TrimEnd('}') + ", \"Modules\": { \"Enable\": [ \"preloader\" ] } }");
            var withModules = CliInput.Parse(new[] { "/spec:" + moduleSpec, "/module:null-test", "/module:preloader" }).BuildSpecAsync().GetAwaiter().GetResult();
            Assert(withModules.EffectiveModules.Enable.SequenceEqual(new[] { "preloader", "null-test" }), "/module did not add to the spec's modules: " + string.Join(",", withModules.EffectiveModules.Enable));
            try { CliInput.Parse(new[] { "/module" }); throw new InvalidOperationException("/module without a value was accepted"); }
            catch (ArgumentException) { }
            Assert(CliInput.Parse(new[] { "/runtime:module.null-test.ping" }).RuntimeOperation == "module.null-test.ping" && PublicCapabilityRegistry.ValidateRuntimeArguments("module.null-test.ping", null).Accepted, "a module operation was not accepted by the frontend");
            Assert(!PublicCapabilityRegistry.ValidateRuntimeArguments("module.Bad.ping", null).Accepted, "a malformed module operation was accepted");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-14: every escaped exception maps to the public exit-code contract.
    private static void ExitCodeClassification()
    {
        Assert(CliProgram.Classify(new InvalidOperationException("OL_E_INSTALLATION_BUSY: held")) is ("OL_E_INSTALLATION_BUSY", _, PublicExitCode.OperationRejected), "busy classification");
        Assert(CliProgram.Classify(new IOException("OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH: x")) is (_, _, PublicExitCode.TransactionRecoveryFailed), "recovery classification");
        Assert(CliProgram.Classify(new InvalidOperationException("OL_E_PLAN_NOT_RUNNABLE: y")) is (_, _, PublicExitCode.SessionFailed), "plan classification");
        Assert(CliProgram.Classify(new ArgumentException("bad")) is ("OL_E_INVALID_ARGUMENT", _, PublicExitCode.InvalidArguments), "argument classification");
        Assert(CliProgram.Classify(new FileNotFoundException("gone")) is (_, _, PublicExitCode.NotFound), "not-found classification");
        Assert(CliProgram.Classify(new TimeoutException("OL_E_RUNTIME_REQUEST_TIMEOUT")) is ("OL_E_RUNTIME_REQUEST_TIMEOUT", _, PublicExitCode.RuntimeUnavailable), "timeout classification");
        Assert(CliProgram.Classify(new NullReferenceException()) is ("OL_E_INTERNAL", _, PublicExitCode.InternalError), "internal classification");
        Assert(CliProgram.Classify(new InvalidOperationException("OL_E_UNSUPPORTED_OPERATING_SYSTEM")) is (_, _, PublicExitCode.UnsupportedProfile), "unsupported classification");
        Assert((int)PublicExitCode.SessionFailed == 1, "exit code 1 must be part of the public contract");
    }

    // S-06: one endpoint per installation, mutating commands bound to the
    // session id, handler failures answered as typed errors, oversize refused.
    private static void ControlPlane()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Control-" + Guid.NewGuid().ToString("N"));
        var other = root + "-other";
        var sessionId = Guid.NewGuid();
        string? boundSeen = null;
        var plane = new LocalControlPlane(root, request =>
        {
            if (request.Command == "session.status") return Task.FromResult(new LocalControlResponse(true, new SessionStatus(sessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>())));
            if (request.Command == "session.stop") { boundSeen = request.Arguments?.GetValueOrDefault("session_id"); return Task.FromResult(new LocalControlResponse(true, new { accepted = true })); }
            if (request.Command == "boom") throw new InvalidOperationException("OL_E_SESSION_NOT_RUNNING");
            if (request.Command == "crash") throw new NullReferenceException("handler bug");
            if (request.Command == "huge") return Task.FromResult(new LocalControlResponse(true, new string('x', 70_000)));
            return Task.FromResult(new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_COMMAND_UNKNOWN"));
        });
        plane.Start();
        try
        {
            Assert(LocalControlPlane.PipeNameFor(root) != LocalControlPlane.PipeNameFor(other) && LocalControlPlane.PipeNameFor(root) == LocalControlPlane.PipeNameFor(root + "\\"), "pipe name must be per installation and normalised");
            var status = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(status is { Ok: true } && status.Result is JsonElement element && element.GetProperty("SessionId").GetGuid() == sessionId, "status round-trip failed");
            Assert(LocalControlPlane.TryRequestAsync(other, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(500)).GetAwaiter().GetResult() is null, "another installation reached this owner's endpoint");
            var stop = LocalControlPlane.TryRequestBoundAsync(root, "session.stop", null, TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(stop is { Ok: true } && boundSeen == sessionId.ToString("D"), "bound request did not carry the session id");
            var typed = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "boom"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(typed is { Ok: false, ErrorCode: "OL_E_SESSION_NOT_RUNNING" }, "handler exception was not answered as a typed error");
            var crashed = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "crash"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(crashed is { Ok: false, ErrorCode: "OL_E_CONTROL_HANDLER_FAILED" }, "handler bug was not answered as a typed error");
            var protocol = LocalControlPlane.TryRequestAsync(root, new("9.9", "session.status"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(protocol is { Ok: false, ErrorCode: "OL_E_CONTROL_PROTOCOL" }, "protocol mismatch was not rejected");
            var oversized = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "session.status", new Dictionary<string, string> { ["pad"] = new string('x', 70_000) }), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(oversized is { Ok: false, ErrorCode: "OL_E_CONTROL_MESSAGE_TOO_LARGE" }, "oversized request was not reported as a typed client error");
            // Correction pass: an oversized reply is a typed error, not silence
            // that the client would read as "no active session".
            var huge = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "huge"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(huge is { Ok: false, ErrorCode: "OL_E_CONTROL_RESPONSE_TOO_LARGE" }, "oversized reply was not reported as a typed error");
            // Status and events carry up to 256 events of up to 4 KiB; they are
            // trimmed oldest-first to fit one frame, never dropped entirely.
            var bulky = Enumerable.Range(1, 256).Select(index => new RuntimeEvent("telemetry.sample", DateTimeOffset.UtcNow, index, new Dictionary<string, string> { ["payload"] = new string('y', 1024) })).ToArray();
            var fitted = LocalControlPlane.FitStatus(new SessionStatus(sessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>(), bulky), out var omitted);
            Assert(omitted > 0 && fitted.RuntimeEvents!.Count > 0 && fitted.RuntimeEvents[^1].Sequence == 256 && JsonSerializer.SerializeToUtf8Bytes(new LocalControlResponse(true, fitted)).Length < 65_536, "status was not trimmed to one frame keeping the newest events");
            var small = LocalControlPlane.FitStatus(new SessionStatus(sessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>(), bulky.Take(3).ToArray()), out var none);
            Assert(none == 0 && small.RuntimeEvents!.Count == 3, "small status must not be trimmed");
            var still = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(still is { Ok: true }, "endpoint died after faulted clients");
        }
        finally { plane.DisposeAsync().AsTask().GetAwaiter().GetResult(); }
        Assert(plane.ListenFault is null, "listener faulted: " + plane.ListenFault);
    }

    // S-03: whatever happens after StartSessionAsync, CloseAsync runs.
    private static void OwnerLifecycle()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Owner-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root);
        try
        {
            var spec = new LaunchSpec(new InstallationSpec(root), new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Set("maps\\Grundorf\\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(1)), new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new EnvironmentSpec(new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>(), new Dictionary<string, OptionalValue<string>>()), new LaunchBehaviorSpec(StartupTimeoutSeconds: 1), Presentation: new SessionPresentationSpec(SuppressTrayIcon: true));
            var plan = new SessionPlan(Guid.NewGuid(), "Omsi23004_692EBFBF", spec, new RuntimePlatformInfo("Windows", "", "X64", "X64", "X86", "X86", true, false, true, true, true, true, true, true, true), Array.Empty<ContentIdentity>(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<Capability>(), Array.Empty<Capability>(), Array.Empty<PlannedMutation>(), Array.Empty<LaunchDiagnostic>(), true);
            var input = CliInput.Parse(new[] { root, "/new", "--json" });

            var faulting = new FakeLaunch { CompletionFault = new InvalidOperationException("OL_E_PROCESS_SUPERVISION: simulated") };
            try { OwnerSession.RunAsync(faulting, input, spec, plan, consoleSignals: false).GetAwaiter().GetResult(); throw new InvalidOperationException("fault was swallowed"); }
            catch (InvalidOperationException error) when (error.Message.Contains("simulated", StringComparison.Ordinal)) { }
            Assert(faulting.Closed, "CloseAsync was skipped after an exception");

            var failed = new FakeLaunch { RunningState = SessionState.Failed };
            Assert(OwnerSession.RunAsync(failed, input, spec, plan, consoleSignals: false).GetAwaiter().GetResult() == (int)PublicExitCode.SessionFailed && failed.Closed, "failed startup did not close the session");

            var normal = new FakeLaunch();
            var observed = CliInput.Parse(new[] { root, "/new", "--json", "/observe-seconds:0" });
            Assert(OwnerSession.RunAsync(normal, observed, spec, plan, consoleSignals: false).GetAwaiter().GetResult() == (int)PublicExitCode.Success && normal.Closed && normal.StopRequested, "normal observation did not stop and close");
        }
        finally { Directory.Delete(root, true); }
    }

    // Addendum invariant D and item 2: every known server-side failure reaches
    // the client as a typed error; "no reply"/"no active session" is reserved
    // for the case where no owner endpoint exists at all.
    private static void ControlPlaneNeverSilent()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Silent-" + Guid.NewGuid().ToString("N"));
        var sessionId = Guid.NewGuid();
        var bulky = Enumerable.Range(1, 256).Select(index => new RuntimeEvent("telemetry.sample", DateTimeOffset.UtcNow, index, new Dictionary<string, string> { ["payload"] = new string('y', 1024) })).ToArray();
        var plane = new LocalControlPlane(root, request => request.Command switch
        {
            "unserializable" => Task.FromResult(new LocalControlResponse(true, new Cycle())),
            "bulky-status" => Task.FromResult(Fitted(new SessionStatus(sessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>(), bulky))),
            _ => Task.FromResult(new LocalControlResponse(true, new SessionStatus(sessionId, SessionState.Running, Array.Empty<LaunchDiagnostic>())))
        });
        plane.Start();
        try
        {
            var pipe = LocalControlPlane.PipeNameFor(root);
            var frames = new (string Name, byte[] Frame, string Expected)[]
            {
                ("json null", Frame(Encoding.UTF8.GetBytes("null")), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("empty frame", Frame(Array.Empty<byte>()), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("malformed json", Frame(Encoding.UTF8.GetBytes("{not json")), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("oversized request header", BitConverter.GetBytes(70_000), "OL_E_CONTROL_MESSAGE_INVALID"),
                // Runtime closure R04: an oversized header followed by body bytes
                // the owner never reads must still be answered, not dropped.
                ("oversized request header with trailing body", BitConverter.GetBytes(70_000).Concat(new byte[16]).ToArray(), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("negative length header with trailing body", BitConverter.GetBytes(-1).Concat(new byte[16]).ToArray(), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("negative length header", BitConverter.GetBytes(-1), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("missing command", Frame(JsonSerializer.SerializeToUtf8Bytes(new { ProtocolVersion = PublicCapabilityRegistry.ProtocolVersion })), "OL_E_CONTROL_MESSAGE_INVALID"),
                ("unsupported protocol", Frame(JsonSerializer.SerializeToUtf8Bytes(new LocalControlRequest("0.0", "session.status"))), "OL_E_CONTROL_PROTOCOL")
            };
            foreach (var (name, frame, expected) in frames)
            {
                var reply = RawExchange(pipe, frame);
                Assert(reply is not null && reply.Value.GetProperty("Ok").GetBoolean() == false && reply.Value.GetProperty("ErrorCode").GetString() == expected, name + ": expected typed " + expected + " but got " + (reply?.ToString() ?? "no reply"));
            }
            var unserializable = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "unserializable"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(unserializable is { Ok: false, ErrorCode: "OL_E_CONTROL_HANDLER_FAILED" }, "unserializable reply was not answered as a typed error");
            var trimmed = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "bulky-status"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(trimmed is { Ok: true } && trimmed.Metadata is { } metadata && metadata["events_truncated"] == "true" && int.Parse(metadata["events_dropped_count"]) > 0 && int.Parse(metadata["events_returned_count"]) + int.Parse(metadata["events_dropped_count"]) == 256, "truncated status did not carry truncation metadata");
            var untouched = LocalControlPlane.TryRequestAsync(root, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            Assert(untouched is { Ok: true, Metadata: null }, "an untrimmed reply must not claim truncation");
            // CLI mapping: typed errors keep their code and never become "no active session".
            Assert(CliProgram.ReportForwarded("session.status", new LocalControlResponse(false, ErrorCode: "OL_E_CONTROL_RESPONSE_TOO_LARGE"), true) == (int)PublicExitCode.OperationRejected, "typed control error was mapped to another exit code");
            Assert(CliProgram.ReportForwarded("session.status", null, true) == (int)PublicExitCode.NoActiveSession, "absent owner must map to NoActiveSession");
        }
        finally { plane.DisposeAsync().AsTask().GetAwaiter().GetResult(); }

        // An owner that accepts the connection and closes without a reply, and
        // one that never answers: both are typed, because the owner exists.
        foreach (var (behaviour, expected) in new[] { ("close", "OL_E_CONTROL_PROTOCOL"), ("hang", "OL_E_TIMEOUT") })
        {
            var silentRoot = Path.Combine(Path.GetTempPath(), "OmsiLaunch-SilentOwner-" + Guid.NewGuid().ToString("N"));
            using var server = new System.IO.Pipes.NamedPipeServerStream(LocalControlPlane.PipeNameFor(silentRoot), System.IO.Pipes.PipeDirection.InOut, 1, System.IO.Pipes.PipeTransmissionMode.Byte, System.IO.Pipes.PipeOptions.Asynchronous | System.IO.Pipes.PipeOptions.CurrentUserOnly);
            var owner = Task.Run(async () => { await server.WaitForConnectionAsync(); var header = new byte[4]; await server.ReadAsync(header); var body = new byte[BitConverter.ToInt32(header, 0)]; await server.ReadAsync(body); if (behaviour == "close") server.Disconnect(); else await Task.Delay(2_000); });
            var reply = LocalControlPlane.TryRequestAsync(silentRoot, new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(800)).GetAwaiter().GetResult();
            Assert(reply is { Ok: false } && reply.ErrorCode == expected, behaviour + ": expected typed " + expected + " but got " + (reply?.ErrorCode ?? "no reply (null)"));
            owner.Wait(TimeSpan.FromSeconds(5));
        }
        // No endpoint at all: the only case that is "no active session".
        Assert(LocalControlPlane.TryRequestAsync(Path.Combine(Path.GetTempPath(), "OmsiLaunch-NoOwner-" + Guid.NewGuid().ToString("N")), new(PublicCapabilityRegistry.ProtocolVersion, "session.status"), TimeSpan.FromMilliseconds(300)).GetAwaiter().GetResult() is null, "absent endpoint must be reported as null (no active session)");
    }

    private static LocalControlResponse Fitted(SessionStatus status) { var fitted = LocalControlPlane.FitStatus(status, out var dropped); return new LocalControlResponse(true, fitted, Metadata: LocalControlPlane.TruncationMetadata(fitted.RuntimeEvents, dropped)); }
    private static byte[] Frame(byte[] payload) => BitConverter.GetBytes(payload.Length).Concat(payload).ToArray();
    // Bounded: an owner that never answers fails the test instead of hanging it.
    private static JsonElement? RawExchange(string pipeName, byte[] frame)
    {
        var exchange = Task.Run(() => RawExchangeCore(pipeName, frame));
        return exchange.Wait(TimeSpan.FromSeconds(10)) ? exchange.Result : null;
    }
    private static JsonElement? RawExchangeCore(string pipeName, byte[] frame)
    {
        using var client = new System.IO.Pipes.NamedPipeClientStream(".", pipeName, System.IO.Pipes.PipeDirection.InOut, System.IO.Pipes.PipeOptions.CurrentUserOnly);
        client.Connect(3_000); client.Write(frame); client.Flush();
        var header = new byte[4]; if (!ReadAll(client, header)) return null;
        var body = new byte[BitConverter.ToInt32(header, 0)]; if (!ReadAll(client, body)) return null;
        return JsonDocument.Parse(body).RootElement.Clone();
    }
    private static bool ReadAll(Stream stream, byte[] buffer) { var read = 0; while (read < buffer.Length) { var count = stream.Read(buffer, read, buffer.Length - read); if (count == 0) return false; read += count; } return true; }
    private sealed class Cycle { public Cycle Self => this; }

    private static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }

    private sealed class FakeLaunch : IOmsiLaunch
    {
        public SessionState RunningState { get; init; } = SessionState.Running;
        public Exception? CompletionFault { get; init; }
        public bool Closed { get; private set; }
        public bool StopRequested { get; private set; }
        private SessionState state = SessionState.Created;
        public Task<SessionPlan> PlanSessionAsync(LaunchSpec spec, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<SessionProfileSummary>> ListSessionProfilesAsync(InstallationSpec installation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<RuntimeModuleSummary>> ListRuntimeModulesAsync(InstallationSpec installation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<SessionHandle> StartSessionAsync(SessionPlan plan, CancellationToken cancellationToken = default) { state = RunningState; return Task.FromResult(new SessionHandle(plan.SessionId)); }
        public Task<SessionStatus> GetStatusAsync(SessionHandle session, CancellationToken cancellationToken = default) => Task.FromResult(new SessionStatus(session.SessionId, state, Array.Empty<LaunchDiagnostic>()));
        public async Task<SessionStatus> WaitForAsync(SessionHandle session, SessionState target, TimeSpan timeout, CancellationToken cancellationToken = default)
        {
            if (target == SessionState.Completed)
            {
                // A supervisor fault surfaces through the completion wait
                // without any stop having been requested.
                if (CompletionFault is not null) { await Task.Delay(50, cancellationToken); throw CompletionFault; }
                while (!StopRequested) await Task.Delay(10, cancellationToken);
                state = SessionState.Completed;
            }
            return new SessionStatus(session.SessionId, state, Array.Empty<LaunchDiagnostic>());
        }
        public Task StopAsync(SessionHandle session, CancellationToken cancellationToken = default) { StopRequested = true; return Task.CompletedTask; }
        public Task CloseAsync(SessionHandle session, CancellationToken cancellationToken = default) { Closed = true; StopRequested = true; return Task.CompletedTask; }
        public Task<RuntimeCommandResult> ExecuteRuntimeAsync(SessionHandle session, RuntimeCommand command, TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(new RuntimeCommandResult(session.SessionId, command.RequestId, true));
        public Task<IReadOnlyList<Capability>> GetCapabilitiesAsync(InstallationSpec installation, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Capability>>(Array.Empty<Capability>());
        public Task<IReadOnlyList<ContentIdentity>> DiscoverAsync(InstallationSpec installation, ContentQueryKind query, OptionalValue<string> scope = default, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ContentIdentity>>(Array.Empty<ContentIdentity>());
        public Task<RecoveryStatus> RecoverPendingAsync(InstallationSpec installation, bool restore, CancellationToken cancellationToken = default) => Task.FromResult(new RecoveryStatus(false, false, Array.Empty<LaunchDiagnostic>()));
    }
}
