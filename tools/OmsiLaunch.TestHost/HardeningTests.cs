using System.IO.MemoryMappedFiles;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using OmsiLaunch.Api;
using OmsiLaunch.Configuration;
using OmsiLaunch.Core;
using OmsiLaunch.Process;

// Behavioural tests for the hardening round (OMSILAUNCH-SECURITY-ROBUSTNESS-REVIEW-001).
// Each case names the finding it locks down.
internal static class HardeningTests
{
    // S-07: options.cfg is Windows-1252. A patch to one token must not corrupt
    // accented bytes elsewhere in the file.
    public static void OptionsCp1252RoundTrip()
    {
        var original = Encoding.Latin1.GetBytes("[language]\r\nPTB\r\n[last_map]\r\nmaps\\Braço Norte\\global.cfg\r\n[maxFPS]\r\n30\r\n");
        Require(original.Contains((byte)0xE7), "fixture must contain a CP1252 byte");
        var patched = ConfigurationCatalog.CreatePatch("graphics.maxFPS", "60").Apply(original);
        var text = Encoding.Latin1.GetString(patched);
        Require(text.Contains("[maxFPS]\r\n60", StringComparison.Ordinal), "patch was not applied");
        Require(patched.Contains((byte)0xE7), "CP1252 byte was lost");
        Require(!ContainsSequence(patched, new byte[] { 0xEF, 0xBF, 0xBD }), "replacement character U+FFFD was written");
        Require(text.Contains("maps\\Braço Norte\\global.cfg", StringComparison.Ordinal), "unrelated token changed");
    }

    // S-01: a path the session asked to keep absent, created by OMSI during the
    // session, is a session artifact. Restore removes it and records the hash.
    public static void DeletionCreatedDuringSession()
    {
        var root = Fixture(); try
        {
            const string target = "Texture\\standard.ipr";
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["Texture\\standard.itx"] = Encoding.ASCII.GetBytes("override") }, new[] { target });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            transaction.RecordProcessAsync(2_000_000_000, DateTimeOffset.UtcNow, Path.Combine(root, "Omsi.exe")).GetAwaiter().GetResult();
            WriteText(root, target, "written-by-omsi");
            transaction.RestoreAsync().GetAwaiter().GetResult();
            Require(!File.Exists(Path.Combine(root, target)), "session artifact on a deletion path was not removed");
            Require(!File.Exists(Path.Combine(root, "Texture", "standard.itx")), "absent overlay was not removed");
            Require(!transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal remained after artifact removal");
            var note = transaction.RestoreNotes.Single();
            Require(note.Code == "restore.session-artifact-removed" && note.RelativePath == target && note.Sha256 == Hash(Encoding.UTF8.GetBytes("written-by-omsi")), "removed artifact was not recorded with its hash");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-01 (conservative branch): without a started process nothing OmsiLaunch
    // launched could have written the file. It is retained, reported, and the
    // journal still completes instead of wedging every later start.
    public static void DeletionForeignFileRetained()
    {
        var root = Fixture(); try
        {
            const string target = "Texture\\standard.ipr";
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>(), new[] { target });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            WriteText(root, target, "third-party");
            transaction.RestoreAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(Path.Combine(root, target)) == "third-party", "foreign file was removed");
            Require(!transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal wedged on a foreign file");
            Require(transaction.RestoreNotes.Single().Code == "OL_W_RESTORE_FOREIGN_FILE_RETAINED", "retained file was not reported");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-01 through crash recovery: the journal carries the deletion flag and
    // the process state, so a later recovery reaches the same decision.
    public static void DeletionRecoveryAfterCrash()
    {
        var root = Fixture(); try
        {
            const string target = "Texture\\standard.ipr";
            var crashed = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>(), new[] { target });
            crashed.ApplyAsync().GetAwaiter().GetResult();
            crashed.RecordProcessAsync(2_000_000_000, DateTimeOffset.UtcNow, Path.Combine(root, "Omsi.exe")).GetAwaiter().GetResult();
            WriteText(root, target, "written-by-omsi");
            var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            recovery.RestorePendingAsync().GetAwaiter().GetResult();
            Require(!File.Exists(Path.Combine(root, target)), "recovery did not remove the session artifact");
            Require(!recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal remained after recovery");
            Require(recovery.RestoreNotes.Single().Code == "restore.session-artifact-removed", "recovery did not record the removal");
            Require(!Directory.Exists(Path.Combine(root, ".omsilaunch", "backup")) || !Directory.EnumerateDirectories(Path.Combine(root, ".omsilaunch", "backup")).Any(), "backup directories were not cleaned after recovery");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-12: timestamps and attributes come back, read-only originals do not
    // break apply, backups are removed after a verified restore, no temp files.
    public static void MetadataAndBackupCleanup()
    {
        var root = Fixture(); try
        {
            var path = Path.Combine(root, "options.cfg"); WriteText(root, "options.cfg", "original");
            var stamp = new DateTime(2021, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(path, stamp); File.SetAttributes(path, File.GetAttributes(path) | FileAttributes.ReadOnly);
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = Encoding.ASCII.GetBytes("temporary") });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(path) == "temporary", "read-only original blocked apply");
            transaction.RestoreAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(path) == "original", "original bytes were not restored");
            Require(File.GetLastWriteTimeUtc(path) == stamp, "last write time was not restored");
            Require(File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly), "read-only attribute was not restored");
            Require(!Directory.Exists(Path.Combine(root, ".omsilaunch", "backup")) || !Directory.EnumerateFileSystemEntries(Path.Combine(root, ".omsilaunch", "backup")).Any(), "backup was not cleaned after restore");
            Require(!Directory.EnumerateFiles(root, "*.omsilaunch.tmp", SearchOption.AllDirectories).Any(), "temporary files were left behind");
            File.SetAttributes(path, FileAttributes.Normal);
        }
        finally { Directory.Delete(root, true); }
    }

    // S-12: a corrupted backup must never overwrite the live file.
    public static void BackupCorruptRejected()
    {
        var root = Fixture(); try
        {
            var path = Path.Combine(root, "options.cfg"); WriteText(root, "options.cfg", "original");
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = Encoding.ASCII.GetBytes("temporary") });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            transaction.RecordProcessAsync(2_000_000_000, DateTimeOffset.UtcNow, Path.Combine(root, "Omsi.exe")).GetAwaiter().GetResult();
            foreach (var backup in Directory.EnumerateFiles(Path.Combine(root, ".omsilaunch", "backup"), "*.bin", SearchOption.AllDirectories)) File.WriteAllText(backup, "corrupted");
            var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            var rejected = false;
            try { recovery.RestorePendingAsync().GetAwaiter().GetResult(); }
            catch (IOException error) when (error.Message.StartsWith("OL_E_RECOVERY_BACKUP_CORRUPT", StringComparison.Ordinal)) { rejected = true; }
            Require(rejected, "corrupt backup was accepted");
            Require(File.ReadAllText(path) == "temporary", "corrupt backup overwrote the live file");
            Require(recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal was removed although recovery failed");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-04: a journal past the handoff but without a PID belongs to a host that
    // died around CreateProcess. While an OMSI from this root is running the
    // journal is busy; once it is gone, recovery proceeds normally.
    public static void RecoveryPrePidWindow()
    {
        var root = Fixture(); System.Diagnostics.Process? omsi = null; try
        {
            const string target = "Texture\\standard.ipr";
            var crashed = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = Encoding.ASCII.GetBytes("temporary") }, new[] { target });
            WriteText(root, "options.cfg", "original");
            crashed.ApplyAsync().GetAwaiter().GetResult();
            crashed.MarkStateAsync(TransactionState.HandoffCreated).GetAwaiter().GetResult();
            var executable = Path.Combine(root, "Omsi.exe"); File.Copy(Path.Combine(Environment.SystemDirectory, "ping.exe"), executable);
            omsi = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(executable, "-n 30 127.0.0.1") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true });
            Require(omsi is not null, "fixture process did not start");
            var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            var busy = false;
            try { recovery.RestorePendingAsync().GetAwaiter().GetResult(); }
            catch (IOException error) when (error.Message.StartsWith("OL_E_INSTALLATION_BUSY", StringComparison.Ordinal)) { busy = true; }
            Require(busy, "recovery ran underneath a running OMSI from this root");
            Require(File.ReadAllText(Path.Combine(root, "options.cfg")) == "temporary", "busy recovery changed files");
            omsi!.Kill(); omsi.WaitForExit();
            recovery.RestorePendingAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(Path.Combine(root, "options.cfg")) == "original", "recovery did not restore after the process exited");
            Require(!recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal remained");
        }
        finally { try { if (omsi is { HasExited: false }) omsi.Kill(); } catch (InvalidOperationException) { } omsi?.Dispose(); Directory.Delete(root, true); }
    }

    // S-08: a response that arrives after its request timed out must not wedge
    // the channel; the next request is staged and answered normally.
    public static void LateResponseIgnored()
    {
        var session = Guid.NewGuid(); using var store = CurrentRuntimeCommandStore.Create(session);
        var timedOut = false;
        try { store.RequestAsync(new RuntimeCommand(session, 1, "time.read"), TimeSpan.FromMilliseconds(200)).GetAwaiter().GetResult(); }
        catch (TimeoutException) { timedOut = true; }
        Require(timedOut, "unanswered request did not time out");
        using var mapping = MemoryMappedFile.OpenExisting(store.Name, MemoryMappedFileRights.ReadWrite);
        using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite))
        {
            var late = RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, 1, true));
            view.Write(4, late.Length); view.WriteArray(8, late, 0, late.Length); view.Write(0, 2); view.Flush();
        }
        var pending = store.RequestAsync(new RuntimeCommand(session, 2, "time.read"), TimeSpan.FromSeconds(3));
        using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite))
        {
            var until = DateTimeOffset.UtcNow.AddSeconds(2); while (view.ReadInt32(0) != 1 && DateTimeOffset.UtcNow < until) Thread.Sleep(10);
            Require(view.ReadInt32(0) == 1, "second request was not staged after a stale response (channel wedged as BUSY)");
            var length = view.ReadInt32(4); var staged = new byte[length]; view.ReadArray(8, staged, 0, length);
            Require(RuntimeCommandWire.TryReadRequestId(staged, out var stagedId) && stagedId == 2, "staged request id is not the new request");
            var response = RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, 2, true, Values: new Dictionary<string, string> { ["ok"] = "true" }));
            view.Write(4, response.Length); view.WriteArray(8, response, 0, response.Length); view.Write(0, 2); view.Flush();
        }
        var result = pending.GetAwaiter().GetResult();
        Require(result.Succeeded && result.RequestId == 2, "second request did not receive its own response");
    }

    // S-09: with a packaged manifest, installed plugin files must match the
    // packaged hashes; a stale or tampered file is rejected, not self-validated.
    public static void PermanentPluginManifestIntegrity()
    {
        var root = Fixture(); try
        {
            var source = Path.Combine(root, "source"); Directory.CreateDirectory(source); Directory.CreateDirectory(Path.Combine(root, "plugins"));
            var names = new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json", "OmsiLaunch.Plugin.dll", "OmsiLaunch.Api.dll" };
            foreach (var file in names) File.WriteAllText(Path.Combine(source, file), file + "-content");
            var native = Path.Combine(root, "OmsiLaunch.Native.x86.dll"); File.WriteAllText(native, "native-content");
            var entries = names.Select(file => new { path = "plugins/" + file, sha256 = Hash(File.ReadAllBytes(Path.Combine(source, file))) }).Append(new { path = "plugins/OmsiLaunch.Native.x86.dll", sha256 = Hash(File.ReadAllBytes(native)) }).ToList();
            var manifest = Path.Combine(root, "release-manifest.json");
            File.WriteAllText(manifest, JsonSerializer.Serialize(new { files = entries.Append(new { path = "LICENSE", sha256 = new string('0', 64) }) }));
            var expected = ReleaseManifest.TryReadPluginHashes(manifest);
            Require(expected is not null && expected.Count == 7 && !expected.ContainsKey("LICENSE"), "manifest plugin hashes were not read");
            var deployment = RuntimeArtifactSet.Load(source, native, expected);
            Require(deployment.IntegrityReference == "manifest", "integrity reference must be the manifest");
            foreach (var artifact in deployment.Artifacts) File.Copy(artifact.SourcePath, Path.Combine(root, artifact.DestinationRelativePath), true);
            deployment.ValidateInstalled(root);

            // Self-referential layout: the reference closure IS the installed
            // directory. Only the manifest can detect a stale file there.
            File.WriteAllText(Path.Combine(root, "plugins", "OmsiLaunch.Plugin.dll"), "stale-beta2-copy");
            var selfValidated = RuntimeArtifactSet.Load(Path.Combine(root, "plugins"), Path.Combine(root, "plugins", "OmsiLaunch.Native.x86.dll"));
            selfValidated.ValidateInstalled(root);
            var withManifest = RuntimeArtifactSet.Load(Path.Combine(root, "plugins"), Path.Combine(root, "plugins", "OmsiLaunch.Native.x86.dll"), expected);
            Expect("OL_E_PERMANENT_PLUGIN_HASH_MISMATCH", () => withManifest.ValidateInstalled(root));

            var incomplete = expected!.Where(pair => !pair.Key.EndsWith("OmsiLaunch.Api.dll", StringComparison.OrdinalIgnoreCase)).ToDictionary(pair => pair.Key, pair => pair.Value);
            File.Copy(Path.Combine(source, "OmsiLaunch.Plugin.dll"), Path.Combine(root, "plugins", "OmsiLaunch.Plugin.dll"), true);
            Expect("OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE", () => RuntimeArtifactSet.Load(Path.Combine(root, "plugins"), Path.Combine(root, "plugins", "OmsiLaunch.Native.x86.dll"), incomplete).ValidateInstalled(root));
        }
        finally { Directory.Delete(root, true); }
    }

    // Runtime closure BUG-01: planning validates the INSTALLED closure against
    // the manifest, so a manifest-listed plugin missing from plugins\ (or an
    // altered one) makes the plan non-runnable with the specific code, and the
    // re-plan in StartSessionAsync rejects before any session exists.
    public static void PlanValidatesInstalledPluginClosure()
    {
        var root = Fixture(); try
        {
            var plugins = Path.Combine(root, "plugins"); Directory.CreateDirectory(plugins);
            foreach (var file in new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json", "OmsiLaunch.Plugin.dll", "OmsiLaunch.Api.dll", "OmsiLaunch.Native.x86.dll", "OmsiLaunch.Interop.dll" })
                File.WriteAllText(Path.Combine(plugins, file), file + "-content");
            var manifest = Path.Combine(root, "release-manifest.json");
            File.WriteAllText(manifest, JsonSerializer.Serialize(new { files = Directory.EnumerateFiles(plugins).Select(file => new { path = "plugins/" + Path.GetFileName(file), sha256 = Hash(File.ReadAllBytes(file)) }) }));
            var service = new OmsiLaunchService(new CurrentWindowsX64Platform(), new OmsiLaunchRuntimePaths(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), manifest));
            var none = new Dictionary<string, OptionalValue<string>>();
            var spec = new LaunchSpec(new InstallationSpec(root), new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new EnvironmentSpec(none, none, none, none, none, none, none, none), new LaunchBehaviorSpec());
            IEnumerable<string> Codes(SessionPlan plan) => plan.Diagnostics.Select(d => d.Code);
            var intact = service.PlanSessionAsync(spec).GetAwaiter().GetResult();
            Require(!Codes(intact).Any(code => code.StartsWith("OL_E_PERMANENT_PLUGIN_", StringComparison.Ordinal)) && Codes(intact).Contains("plugin.integrity.reference"), "an intact installed closure was reported as damaged");
            // The installed closure is the plugin build directory here, so a
            // parked file disappears from both: only the manifest can tell.
            File.Move(Path.Combine(plugins, "OmsiLaunch.Interop.dll"), Path.Combine(root, "OmsiLaunch.Interop.dll.parked"));
            var missing = service.PlanSessionAsync(spec).GetAwaiter().GetResult();
            Require(!missing.IsRunnable && Codes(missing).Contains("OL_E_PERMANENT_PLUGIN_MISSING"), "a manifest-listed plugin missing from plugins\\ did not make the plan non-runnable");
            File.Move(Path.Combine(root, "OmsiLaunch.Interop.dll.parked"), Path.Combine(plugins, "OmsiLaunch.Interop.dll"));
            File.AppendAllText(Path.Combine(plugins, "OmsiLaunch.Api.dll"), "!");
            var altered = service.PlanSessionAsync(spec).GetAwaiter().GetResult();
            Require(!altered.IsRunnable && Codes(altered).Contains("OL_E_PERMANENT_PLUGIN_HASH_MISMATCH"), "an altered installed plugin did not make the plan non-runnable");
            Expect("OL_E_PLAN_NOT_RUNNABLE", () => service.StartSessionAsync(intact with { IsRunnable = true }).GetAwaiter().GetResult());
        }
        finally { Directory.Delete(root, true); }
    }

    // S-25 / Beta 4 telemetry ring: every event keeps its own numbered slot.
    // A burst arrives complete and in order, identical consecutive events stay
    // distinct, a torn slot is never delivered, and a reader that falls behind
    // by more than the ring reports exactly how many events it lost.
    public static void TelemetrySequenceSamples()
    {
        using var store = CurrentTelemetryStore.Create(Guid.NewGuid());
        using var mapping = MemoryMappedFile.OpenExisting(store.Name, MemoryMappedFileRights.ReadWrite);
        using var view = mapping.CreateViewAccessor(0, TelemetryRingWire.Capacity, MemoryMappedFileAccess.ReadWrite);
        var empty = store.ReadNew(); Require(empty.Events.Count == 0 && empty.Lost == 0, "empty ring produced events");
        byte[] Event(string name) => Encoding.UTF8.GetBytes("{\"name\":\"" + name + "\",\"data\":{}}");
        // A PluginStart-like burst written before the host polls once.
        foreach (var name in new[] { "native.capabilities", "internet-textures.suppressed", "plugin.started", "plugin.started" }) Require(TelemetryRingWire.Write(view, Event(name)), "ring write failed");
        var burst = store.ReadNew();
        Require(burst.Lost == 0 && burst.Events.Select(e => e.Sequence).SequenceEqual(new long[] { 1, 2, 3, 4 }), "a burst was not delivered complete and in order");
        Require(burst.Events[0].Payload.Contains("native.capabilities", StringComparison.Ordinal) && burst.Events[3].Payload.Contains("plugin.started", StringComparison.Ordinal), "burst payloads differ");
        Require(store.ReadNew().Events.Count == 0, "events were delivered twice");
        // Falling behind by more than the ring.
        for (var i = 0; i < TelemetryRingWire.SlotCount + 10; i++) TelemetryRingWire.Write(view, Event("tick." + i));
        var behind = store.ReadNew();
        Require(behind.Lost == 10 && behind.Events.Count == TelemetryRingWire.SlotCount && behind.Events[0].Payload.Contains("tick.10", StringComparison.Ordinal), "lost events were not counted exactly");
        // A slot being rewritten (sequence invalidated) is lost, not torn.
        TelemetryRingWire.Write(view, Event("last"));
        var committed = view.ReadInt64(16);
        view.Write(TelemetryRingWire.HeaderSize + ((committed - 1) % TelemetryRingWire.SlotCount) * TelemetryRingWire.SlotSize, 0L);
        var torn = store.ReadNew(); Require(torn.Events.Count == 0 && torn.Lost == 1, "an invalidated slot was delivered");
        Require(!TelemetryRingWire.Write(view, new byte[TelemetryRingWire.MaxPayload + 1]), "an oversized payload was written");
    }

    // S-04: recovery through the API takes the installation lease.
    public static void RecoverRequiresLease()
    {
        var root = Fixture(); try
        {
            var service = new OmsiLaunchService(new CurrentWindowsX64Platform(), new OmsiLaunchRuntimePaths(root, Path.Combine(root, "native.dll")));
            using (var held = InstallationLease.Acquire(root))
            {
                Expect("OL_E_INSTALLATION_BUSY", () => service.RecoverPendingAsync(new InstallationSpec(root), true).GetAwaiter().GetResult());
            }
            var idle = service.RecoverPendingAsync(new InstallationSpec(root), true).GetAwaiter().GetResult();
            Require(!idle.Pending && !idle.Recovered, "an installation without a journal reported recovery");
            WriteText(root, "options.cfg", "original");
            var crashed = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = Encoding.ASCII.GetBytes("temporary") });
            crashed.ApplyAsync().GetAwaiter().GetResult();
            crashed.RecordProcessAsync(2_000_000_000, DateTimeOffset.UtcNow, Path.Combine(root, "Omsi.exe")).GetAwaiter().GetResult();
            var status = service.RecoverPendingAsync(new InstallationSpec(root), false).GetAwaiter().GetResult();
            Require(status.Pending && !status.Recovered && File.ReadAllText(Path.Combine(root, "options.cfg")) == "temporary", "status-only recovery changed files");
            var recovered = service.RecoverPendingAsync(new InstallationSpec(root), true).GetAwaiter().GetResult();
            Require(recovered.Pending && recovered.Recovered && File.ReadAllText(Path.Combine(root, "options.cfg")) == "original", "recovery did not restore");
            // The lease is released again after recovery.
            using var reacquired = InstallationLease.Acquire(root);
        }
        finally { Directory.Delete(root, true); }
    }

    // S-02: the public API boundary rejects internal.* and unknown operations
    // before any session state is consulted, and strips internal result keys.
    public static void RuntimeRejectsInternalOperations()
    {
        var root = Fixture(); try
        {
            var service = new OmsiLaunchService(new CurrentWindowsX64Platform(), new OmsiLaunchRuntimePaths(root, Path.Combine(root, "native.dll")));
            var handle = new SessionHandle(Guid.NewGuid());
            var rejected = service.ExecuteRuntimeAsync(handle, new RuntimeCommand(handle.SessionId, 1, "internal.road-vehicles.make-basic", new Dictionary<string, string> { ["model"] = "Vehicles\\x\\x.bus" }), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
            Require(!rejected.Succeeded && rejected.ErrorCode == "OL_E_RUNTIME_OPERATION_UNKNOWN", "internal operation crossed the public boundary");
            var unknown = service.ExecuteRuntimeAsync(handle, new RuntimeCommand(handle.SessionId, 2, "not.an.operation"), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
            Require(!unknown.Succeeded && unknown.ErrorCode == "OL_E_RUNTIME_OPERATION_UNKNOWN", "unknown operation was not rejected");
            var missing = service.ExecuteRuntimeAsync(handle, new RuntimeCommand(handle.SessionId, 3, "road-vehicles.spawn"), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult();
            Require(!missing.Succeeded && missing.ErrorCode == "OL_E_RUNTIME_ARGUMENT_REQUIRED", "missing required argument was not rejected");
            Require(!PublicCapabilityRegistry.IsPublicRuntimeOperation("internal.road-vehicles.make-basic") && PublicCapabilityRegistry.IsPublicRuntimeOperation("road-vehicles.read"), "public operation set regression");
            Require(PublicCapabilityRegistry.IsInternalResultKey("internal_created_native_address") && PublicCapabilityRegistry.IsInternalResultKey("object_vmt") && !PublicCapabilityRegistry.IsInternalResultKey("handle"), "internal result key convention regression");
            Require(PublicCapabilityRegistry.All.All(descriptor => descriptor.Classification != PublicCapabilityClassification.InternalOnly || !PublicCapabilityRegistry.IsPublicRuntimeOperation(descriptor.ApiRoute)), "an InternalOnly descriptor is routable through the public operation set");
        }
        finally { Directory.Delete(root, true); }
    }

    // S-26: only the newest sessions keep their diagnostics; unrelated files stay.
    public static void DiagnosticsRetention()
    {
        var root = Fixture(); try
        {
            var directory = Path.Combine(root, ".omsilaunch", "diagnostics"); Directory.CreateDirectory(directory);
            var sessions = Enumerable.Range(0, HostTrace.RetainedSessions + 5).Select(_ => Guid.NewGuid().ToString("N")).ToArray();
            var stamp = DateTime.UtcNow.AddHours(-sessions.Length);
            foreach (var session in sessions)
            {
                foreach (var suffix in new[] { "-host.log", "-runtime-operation.json" }) { var file = Path.Combine(directory, session + suffix); File.WriteAllText(file, session); File.SetLastWriteTimeUtc(file, stamp); }
                stamp = stamp.AddHours(1);
            }
            File.WriteAllText(Path.Combine(directory, "release-presentation-validation.json"), "{}"); File.WriteAllText(Path.Combine(directory, "tray-host.log"), "x");
            HostTrace.Prune(directory, HostTrace.RetainedSessions);
            var remaining = sessions.Count(session => File.Exists(Path.Combine(directory, session + "-host.log")));
            Require(remaining == HostTrace.RetainedSessions, "retention kept " + remaining + " session logs");
            foreach (var oldest in sessions.Take(5)) Require(!Directory.EnumerateFiles(directory, oldest + "-*").Any(), "companion files of a pruned session remained");
            foreach (var newest in sessions.Skip(5)) Require(File.Exists(Path.Combine(directory, newest + "-runtime-operation.json")), "a retained session lost its companion file");
            Require(File.Exists(Path.Combine(directory, "release-presentation-validation.json")) && File.Exists(Path.Combine(directory, "tray-host.log")), "retention removed a non-session file");
        }
        finally { Directory.Delete(root, true); }
    }

    // Correction pass (packaging): Windows PowerShell 5.1 writes the release
    // manifest as UTF-8 with a BOM. The reader must accept both encodings with
    // identical results, and still reject a malformed manifest.
    public static void ReleaseManifestWithBom()
    {
        var root = Fixture(); try
        {
            var json = "{\"files\":[{\"path\":\"plugins/OmsiLaunch.Plugin.dll\",\"sha256\":\"" + new string('A', 64) + "\"},{\"path\":\"LICENSE\",\"sha256\":\"" + new string('B', 64) + "\"}]}";
            var plain = Path.Combine(root, "plain.json"); File.WriteAllText(plain, json, new UTF8Encoding(false));
            var bom = Path.Combine(root, "bom.json"); File.WriteAllText(bom, json, new UTF8Encoding(true));
            Require(File.ReadAllBytes(bom)[0] == 0xEF, "fixture must carry a BOM");
            var withoutBom = ReleaseManifest.TryReadPluginHashes(plain); var withBom = ReleaseManifest.TryReadPluginHashes(bom);
            Require(withoutBom is { Count: 1 } && withBom is { Count: 1 } && withBom["plugins/OmsiLaunch.Plugin.dll"] == new string('A', 64), "BOM changed manifest parsing");
            var broken = Path.Combine(root, "broken.json"); File.WriteAllText(broken, "{\"files\":[{\"path\":\"plugins/x.dll\"}]}");
            Expect("OL_E_RELEASE_MANIFEST_INVALID", () => ReleaseManifest.TryReadPluginHashes(broken));
            Require(ReleaseManifest.TryReadPluginHashes(Path.Combine(root, "absent.json")) is null, "absent manifest must mean development layout");
        }
        finally { Directory.Delete(root, true); }
    }

    // Correction pass: a destination briefly held by another process (antivirus,
    // indexer) is retried; a destination that stays locked still fails.
    public static void TransientLockRetried()
    {
        var root = Fixture(); try
        {
            var path = Path.Combine(root, "options.cfg"); WriteText(root, "options.cfg", "original");
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = Encoding.ASCII.GetBytes("temporary") });
            var holder = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            var release = Task.Run(async () => { await Task.Delay(250); holder.Dispose(); });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            release.GetAwaiter().GetResult();
            Require(File.ReadAllText(path) == "temporary", "a briefly held destination was not written after the lock was released");
            transaction.RestoreAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(path) == "original", "restore after a transient lock failed");
        }
        finally { Directory.Delete(root, true); }
    }

    // Addendum item 5 / invariant F: the manifest is read strictly, and the
    // installed closure is validated against it in both directions.
    public static void ManifestParserStrictness()
    {
        string Entry(string path, string? hash = null) => "{\"path\":" + JsonSerializer.Serialize(path) + ",\"sha256\":\"" + (hash ?? new string('A', 64)) + "\"}";
        string Manifest(params string[] entries) => "{\"files\":[" + string.Join(",", entries) + "]}";
        var accepted = new (string Name, string Json)[]
        {
            ("utf-8 plugin and root entries", Manifest(Entry("plugins/OmsiLaunch.Plugin.dll"), Entry("OmsiLaunch.exe", new string('b', 64)))),
            ("backslash spelling", Manifest(Entry("plugins\\OmsiLaunch.Api.dll")))
        };
        foreach (var (name, text) in accepted) ReleaseManifest.ParsePluginHashes(Encoding.UTF8.GetBytes(text));
        Require(ReleaseManifest.ParsePluginHashes(new UTF8Encoding(true).GetPreamble().Concat(Encoding.UTF8.GetBytes(accepted[0].Json)).ToArray()).Count == 1, "BOM manifest rejected");
        var rejected = new (string Name, string Json)[]
        {
            ("empty", ""), ("whitespace", "   "), ("invalid json", "{files:["), ("root not object", "[]"), ("no files", "{}"),
            ("entry not object", "{\"files\":[1]}"), ("missing hash", "{\"files\":[{\"path\":\"plugins/x.dll\"}]}"),
            ("hash too short", Manifest(Entry("plugins/x.dll", "ABCD"))), ("hash too long", Manifest(Entry("plugins/x.dll", new string('A', 65)))), ("hash not hex", Manifest(Entry("plugins/x.dll", new string('Z', 64)))),
            ("duplicate path", Manifest(Entry("plugins/x.dll"), Entry("plugins/x.dll"))),
            ("duplicate by case", Manifest(Entry("plugins/OmsiLaunch.Api.dll"), Entry("PLUGINS/omsilaunch.api.DLL"))),
            ("duplicate by separator", Manifest(Entry("plugins/x.dll"), Entry("plugins\\x.dll"))),
            ("parent segment", Manifest(Entry("plugins/../x.dll"))), ("leading parent", Manifest(Entry("../x.dll"))),
            ("absolute drive", Manifest(Entry("C:/x.dll"))), ("rooted", Manifest(Entry("/plugins/x.dll"))), ("stream syntax", Manifest(Entry("plugins/x.dll:stream"))),
            ("empty segment", Manifest(Entry("plugins//x.dll"))), ("dot segment", Manifest(Entry("plugins/./x.dll")))
        };
        foreach (var (name, text) in rejected) Expect("OL_E_RELEASE_MANIFEST_INVALID", () => ReleaseManifest.ParsePluginHashes(Encoding.UTF8.GetBytes(text)));

        // Closure against manifest: listed-but-missing, present-but-unlisted, wrong hash.
        var root = Fixture(); try
        {
            var plugins = Path.Combine(root, "plugins"); Directory.CreateDirectory(plugins);
            foreach (var file in new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json", "OmsiLaunch.Plugin.dll", "OmsiLaunch.Api.dll", "OmsiLaunch.Native.x86.dll" }) File.WriteAllText(Path.Combine(plugins, file), file);
            Dictionary<string, string> Expected() => Directory.EnumerateFiles(plugins).ToDictionary(file => "plugins/" + Path.GetFileName(file), file => Hash(File.ReadAllBytes(file)), StringComparer.OrdinalIgnoreCase);
            RuntimeArtifactSet Load(IReadOnlyDictionary<string, string> expected) => RuntimeArtifactSet.Load(plugins, Path.Combine(plugins, "OmsiLaunch.Native.x86.dll"), expected);
            Load(Expected()).ValidateInstalled(root);
            var withExtraListed = Expected(); withExtraListed["plugins/OmsiLaunch.Interop.dll"] = new string('C', 64);
            Expect("OL_E_PERMANENT_PLUGIN_MISSING", () => Load(withExtraListed).ValidateInstalled(root));
            var coherent = Expected();
            File.WriteAllText(Path.Combine(plugins, "OmsiLaunch.Unexpected.dll"), "stale");
            Expect("OL_E_PERMANENT_PLUGIN_MANIFEST_INCOMPLETE", () => Load(coherent).ValidateInstalled(root));
            File.Delete(Path.Combine(plugins, "OmsiLaunch.Unexpected.dll"));
            File.AppendAllText(Path.Combine(plugins, "OmsiLaunch.Api.dll"), "!");
            Expect("OL_E_PERMANENT_PLUGIN_HASH_MISMATCH", () => Load(coherent).ValidateInstalled(root));
        }
        finally { Directory.Delete(root, true); }
    }

    // Correction pass (oversized responses): a response larger than the slot
    // is rejected by the host with a typed error instead of being read past the
    // mapping, and the channel remains usable for the next request.
    public static void OversizedResponseRejected()
    {
        var session = Guid.NewGuid(); using var store = CurrentRuntimeCommandStore.Create(session);
        using var mapping = MemoryMappedFile.OpenExisting(store.Name, MemoryMappedFileRights.ReadWrite);
        var pending = store.RequestAsync(new RuntimeCommand(session, 1, "time.read"), TimeSpan.FromSeconds(3));
        using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite))
        {
            var until = DateTimeOffset.UtcNow.AddSeconds(2); while (view.ReadInt32(0) != 1 && DateTimeOffset.UtcNow < until) Thread.Sleep(10);
            view.Write(4, 70_000); view.Write(0, 2); view.Flush();
        }
        Expect("OL_E_RUNTIME_RESPONSE_INVALID", () => pending.GetAwaiter().GetResult());
        var next = store.RequestAsync(new RuntimeCommand(session, 2, "time.read"), TimeSpan.FromSeconds(3));
        using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite))
        {
            var until = DateTimeOffset.UtcNow.AddSeconds(2); while (view.ReadInt32(0) != 1 && DateTimeOffset.UtcNow < until) Thread.Sleep(10);
            Require(view.ReadInt32(0) == 1, "channel did not accept a request after an oversized response");
            var response = RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, 2, true));
            view.Write(4, response.Length); view.WriteArray(8, response, 0, response.Length); view.Write(0, 2); view.Flush();
        }
        Require(next.GetAwaiter().GetResult() is { Succeeded: true, RequestId: 2 }, "next request after an oversized response failed");
    }

    // Addendum invariant C: every terminal path of a runtime request leaves the
    // channel reusable. Each scenario is followed by a valid request that must
    // be staged cleanly (no stale state, length or envelope in the slot) and
    // answered normally.
    public static void RuntimeChannelTerminalPaths()
    {
        var session = Guid.NewGuid(); using var store = CurrentRuntimeCommandStore.Create(session);
        using var mapping = MemoryMappedFile.OpenExisting(store.Name, MemoryMappedFileRights.ReadWrite);
        ulong id = 100;
        // plugin: runs on a worker, waits for a staged request, then applies the scenario.
        Task Plugin(Action<MemoryMappedViewAccessor, RuntimeCommand> respond) => Task.Run(() =>
        {
            using var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite);
            var until = DateTimeOffset.UtcNow.AddSeconds(3); while (view.ReadInt32(0) != 1 && DateTimeOffset.UtcNow < until) Thread.Sleep(5);
            if (view.ReadInt32(0) != 1) throw new InvalidOperationException("request was not staged");
            var bytes = new byte[view.ReadInt32(4)]; view.ReadArray(8, bytes, 0, bytes.Length);
            Require(RuntimeCommandWire.TryDeserializeRequest(bytes, out var request) && request is not null, "staged request could not be decoded (stale bytes in the slot?)");
            respond(view, request!);
        });
        void Respond(MemoryMappedViewAccessor view, byte[] response) { view.Write(4, response.Length); view.WriteArray(8, response, 0, response.Length); view.Write(0, 2); view.Flush(); }
        void AssertSlotClean(string scenario)
        {
            using var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.Read);
            var header = new byte[RuntimeCommandWire.HeaderSize]; view.ReadArray(8, header, 0, header.Length);
            Require(view.ReadInt32(0) == 0 && view.ReadInt32(4) == 0 && header.All(b => b == 0), scenario + ": slot was not cleaned (state/length/envelope left behind)");
        }
        void NextRequestWorks(string scenario)
        {
            AssertSlotClean(scenario);
            var next = ++id;
            var plugin = Plugin((view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId, true, Values: new Dictionary<string, string> { ["ok"] = "true" }))));
            var result = store.RequestAsync(new RuntimeCommand(session, next, "time.read"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            plugin.GetAwaiter().GetResult();
            Require(result.Succeeded && result.RequestId == next, scenario + ": next valid request did not succeed");
            AssertSlotClean(scenario + " (after next)");
        }
        void Fails(string scenario, string expected, Action<MemoryMappedViewAccessor, RuntimeCommand>? respond, TimeSpan timeout, CancellationToken token = default)
        {
            var plugin = respond is null ? Task.CompletedTask : Plugin(respond);
            var current = ++id;
            try { store.RequestAsync(new RuntimeCommand(session, current, "time.read"), timeout, token).GetAwaiter().GetResult(); throw new InvalidOperationException(scenario + ": expected failure " + expected); }
            catch (Exception error) when (error.Message.Contains(expected, StringComparison.Ordinal) || (expected == "cancel" && error is OperationCanceledException)) { }
            plugin.GetAwaiter().GetResult();
            NextRequestWorks(scenario);
        }

        // success
        NextRequestWorks("initial");
        // typed plugin rejection (error received as data, not an exception)
        {
            var plugin = Plugin((view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId, false, "OL_E_RUNTIME_ARGUMENT_REQUIRED"))));
            var rejected = store.RequestAsync(new RuntimeCommand(session, ++id, "time.read"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            plugin.GetAwaiter().GetResult();
            Require(!rejected.Succeeded && rejected.ErrorCode == "OL_E_RUNTIME_ARGUMENT_REQUIRED", "typed rejection was not returned");
            NextRequestWorks("typed rejection");
        }
        Fails("malformed response", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => Respond(view, Encoding.ASCII.GetBytes("not an envelope at all, but a valid length")), TimeSpan.FromSeconds(3));
        Fails("oversized response length", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => { view.Write(4, 70_000); view.Write(0, 2); view.Flush(); }, TimeSpan.FromSeconds(3));
        Fails("negative response length", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => { view.Write(4, -5); view.Write(0, 2); view.Flush(); }, TimeSpan.FromSeconds(3));
        Fails("wrong request id", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId + 7, true))), TimeSpan.FromSeconds(3));
        Fails("wrong session id", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(Guid.NewGuid(), request.RequestId, true))), TimeSpan.FromSeconds(3));
        Fails("corrupt response hash", "OL_E_RUNTIME_RESPONSE_INVALID", (view, request) => { var bytes = RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId, true, Values: new Dictionary<string, string> { ["k"] = "v" })); bytes[^1] ^= 1; Respond(view, bytes); }, TimeSpan.FromSeconds(3));
        Fails("plugin reset without answer", "OL_E_RUNTIME_REQUEST_TIMEOUT", (view, request) => { view.Write(0, 0); view.Flush(); }, TimeSpan.FromMilliseconds(300));
        Fails("timeout, no plugin", "OL_E_RUNTIME_REQUEST_TIMEOUT", null, TimeSpan.FromMilliseconds(200));
        using (var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(150)))
            Fails("caller cancellation", "cancel", null, TimeSpan.FromSeconds(10), cancellation.Token);
        // stale response: arrives after the host abandoned the request
        {
            var current = ++id;
            try { store.RequestAsync(new RuntimeCommand(session, current, "time.read"), TimeSpan.FromMilliseconds(150)).GetAwaiter().GetResult(); } catch (TimeoutException) { }
            using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, current, true)));
            var plugin = Plugin((view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId, true))));
            var next = ++id; var result = store.RequestAsync(new RuntimeCommand(session, next, "time.read"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            plugin.GetAwaiter().GetResult();
            Require(result.Succeeded && result.RequestId == next, "stale response was read as the answer to a later request");
            AssertSlotClean("stale response");
        }
        // stale response whose id collides with the new request: rejected, then usable
        {
            var reused = ++id;
            using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, reused, true)));
            Expect("OL_E_RUNTIME_REQUEST_ID_REUSED", () => store.RequestAsync(new RuntimeCommand(session, reused, "time.read"), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult());
            NextRequestWorks("request id reused");
        }
        // orphaned request left in the slot (never answered): cleared, then usable
        {
            var orphan = RuntimeCommandWire.SerializeRequest(new RuntimeCommand(session, 1, "time.read"));
            using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(4, orphan.Length); view.WriteArray(8, orphan, 0, orphan.Length); view.Write(0, 1); view.Flush(); }
            var plugin = Plugin((view, request) => Respond(view, RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, request.RequestId, true))));
            var next = ++id; var result = store.RequestAsync(new RuntimeCommand(session, next, "time.read"), TimeSpan.FromSeconds(3)).GetAwaiter().GetResult();
            plugin.GetAwaiter().GetResult();
            Require(result.Succeeded && result.RequestId == next, "orphaned request blocked the channel");
        }
        // unknown state value: typed error, then usable
        {
            using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite)) { view.Write(0, 9); view.Flush(); }
            Expect("OL_E_RUNTIME_CHANNEL_STATE_INVALID", () => store.RequestAsync(new RuntimeCommand(session, ++id, "time.read"), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult());
            NextRequestWorks("unknown state");
        }
        // disposed channel (owner/session shutdown): deterministic failure, no hang
        store.Dispose();
        try { store.RequestAsync(new RuntimeCommand(session, ++id, "time.read"), TimeSpan.FromSeconds(1)).GetAwaiter().GetResult(); throw new InvalidOperationException("a disposed channel accepted a request"); }
        catch (ObjectDisposedException) { }
    }

    private static bool ContainsSequence(byte[] haystack, byte[] needle)
    {
        for (var index = 0; index + needle.Length <= haystack.Length; index++) if (haystack.AsSpan(index, needle.Length).SequenceEqual(needle)) return true;
        return false;
    }
    private static string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
    private static string Fixture() { var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Hardening-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); return root; }
    private static void WriteText(string root, string relative, string text) { var path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text); }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Expect(string code, Action action)
    {
        try { action(); }
        catch (Exception error) when (error.Message.Contains(code, StringComparison.Ordinal)) { return; }
        catch (Exception error) { throw new InvalidOperationException("expected " + code + " but got " + error.GetType().Name + ": " + error.Message); }
        throw new InvalidOperationException("expected " + code);
    }
}
