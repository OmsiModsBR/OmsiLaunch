using OmsiLaunch.Api;
using OmsiLaunch.Configuration;
using OmsiLaunch.Content;
using OmsiLaunch.Core;
using OmsiLaunch.Process;
using System.IO.MemoryMappedFiles;

var tests = new List<(string Name, Action Run)>
{
    ("handoff.roundtrip", HandoffRoundTrip),
    ("handoff.hash-guard", HandoffHashGuard),
    ("runtime-command.wire-guard", RuntimeCommandWireGuard),
    ("runtime-command.session-binding", RuntimeCommandSessionBinding),
    (@"options.noop", OptionsNoOp),
    (@"options.vector-block", OptionsVectorBlock),
    ("discovery.fixture", DiscoveryFixture),
    ("transaction.restore", TransactionRestore),
    ("transaction.options-overlay-restore", TransactionOptionsOverlayRestore),
    ("transaction.absent-overlay-restore", TransactionAbsentOverlayRestore),
    ("transaction.session-delete-restore", TransactionDeleteRestore),
    ("transaction.failure-boundaries", TransactionFailureBoundaries),
    ("transaction.restore-failure-recovery", TransactionRestoreFailureRecovery),
    ("transaction.absent-file-recovery", TransactionAbsentFileRecovery),
    ("transaction.absent-file-ownership", TransactionAbsentFileOwnership),
    ("transaction.empty-journal-restore", TransactionEmptyJournalRestore),
    ("transaction.recovery-then-apply-ownership", TransactionRecoveryThenApplyOwnership),
    ("transaction.legacy-journal-ownership-migration", TransactionLegacyJournalOwnershipMigration),
    ("configuration.catalog", ConfigurationCatalogTest)
    ,("configuration.ranges-transforms", ConfigurationRanges)
    ,("configuration.compound-patches", ConfigurationCompoundPatches)
    ,("keyboard.patch", KeyboardPatch)
    ,("controller.patch", ControllerPatch)
    ,("runtime.manifest", RuntimeManifest),
    ("process.createprocess.fast-exit", ProcessProviderFastExit),
    ("lease.cross-thread-release", LeaseCrossThreadRelease),
    ("runtime.permanent-plugin-installation", RuntimePermanentPluginInstallation),
    ("content.discovery-junction-cycle", ContentReparseTests.DiscoveryJunctionCycle),
    ("content.readlines-cp1252", ContentReparseTests.ReadLinesCp1252),
    (@"options.cp1252-roundtrip", HardeningTests.OptionsCp1252RoundTrip),
    ("transaction.deletion-created-during-session", HardeningTests.DeletionCreatedDuringSession),
    ("build-detection.rejects-non-omsi2", BuildDetectionTests.RejectsWhatIsNotOmsi2),
    ("build-detection.reference-executable-when-available", BuildDetectionTests.ReferenceExecutable),
    ("input-documents.planned-as-overlays", InputDocumentTests.PlannedAsOverlays),
    ("start-date-time.resolution", StartDateTimeTests.Resolution),
    ("last-map-state.planned", LastMapStateTests.Planned),
    ("entrypoint-identity.resolved", EntrypointIdentityTests.Resolved),
    ("start-weather.resolved", StartWeatherTests.Resolved),
    ("hof.plan-sources-and-modes", HofManagementTests.PlanSourcesAndModes),
    ("hof.plan-rejects-unsafe-input", HofManagementTests.PlanRejectsUnsafeInput),
    ("hof.transaction-exact-restore", HofManagementTests.TransactionExactRestore),
    ("hof.recovery-after-crash", HofManagementTests.RecoveryAfterCrash),
    ("transaction.deletion-foreign-file-retained", HardeningTests.DeletionForeignFileRetained),
    ("transaction.deletion-recovery-after-crash", HardeningTests.DeletionRecoveryAfterCrash),
    ("transaction.metadata-and-backup-cleanup", HardeningTests.MetadataAndBackupCleanup),
    ("transaction.backup-corrupt-rejected", HardeningTests.BackupCorruptRejected),
    ("transaction.recovery-pre-pid-window", HardeningTests.RecoveryPrePidWindow),
    ("runtime-command.late-response-ignored", HardeningTests.LateResponseIgnored),
    ("runtime.permanent-plugin-manifest-integrity", HardeningTests.PermanentPluginManifestIntegrity),
    ("telemetry.sequence-samples", HardeningTests.TelemetrySequenceSamples),
    ("api.recover-requires-lease", HardeningTests.RecoverRequiresLease),
    ("api.runtime-rejects-internal-operations", HardeningTests.RuntimeRejectsInternalOperations),
    ("diagnostics.retention", HardeningTests.DiagnosticsRetention)
    ,("presentation.itx-target-root-normalization", ItxTargetRootNormalization)
    ,("presentation.itx-target-guard-rejections", ItxTargetGuardRejections)
    ,("lease.root-normalization", LeaseRootNormalization)
    ,("paths.installation-identity-and-containment", InstallationIdentity)
    ,("runtime.release-manifest-bom", HardeningTests.ReleaseManifestWithBom)
    ,("runtime.release-manifest-strict-parser", HardeningTests.ManifestParserStrictness)
    ,("plan.validates-installed-plugin-closure", HardeningTests.PlanValidatesInstalledPluginClosure)
    ,("transaction.transient-lock-retried", HardeningTests.TransientLockRetried)
    ,("runtime-command.oversized-response-rejected", HardeningTests.OversizedResponseRejected)
    ,("runtime-command.terminal-paths-leave-channel-usable", HardeningTests.RuntimeChannelTerminalPaths)
};
var failures = new List<string>();
foreach (var test in tests)
{
    try { test.Run(); Console.WriteLine("PASS " + test.Name); }
    catch (Exception error) { failures.Add(test.Name + ": " + error.Message); Console.Error.WriteLine("FAIL " + test.Name + " " + error); }
}
if (failures.Count != 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures)); Environment.ExitCode = 1; }

static void HandoffRoundTrip()
{
    var expected = new StartupHandoff(Guid.NewGuid(), "Omsi23004_692EBFBF", WorldMode.NewMap, "maps\\Grundorf\\global.cfg", -1, true, false, DateTimeMode.Unset, DateTimeMode.Unset, "Nordspitze Bauernhof");
    Assert(StartupHandoffWire.TryDeserialize(StartupHandoffWire.Serialize(expected), out var actual) && actual == expected, "portable handoff did not round-trip");
}
static void HandoffHashGuard()
{
    var data = StartupHandoffWire.Serialize(new StartupHandoff(Guid.NewGuid(), "profile", WorldMode.NewMap, "maps\\Grundorf\\global.cfg", 1, true, false, DateTimeMode.Unset, DateTimeMode.Unset)); data[^1] ^= 0x01;
    Assert(!StartupHandoffWire.TryDeserialize(data, out _), "corrupt handoff was accepted");
}

static void ItxTargetRootNormalization()
{
    var root = Path.Combine(Path.GetTempPath(), "omsilaunch-itx-" + Guid.NewGuid().ToString("N"));
    try
    {
        var target = "Sceneryobjects\\Ruede\\texture\\werbungen\\fromInternet_0.tga";
        Directory.CreateDirectory(Path.Combine(root, "Sceneryobjects", "Ruede", "texture", "werbungen"));
        var profile = Path.Combine(root, "fixture.itx");
        File.WriteAllText(profile, "https://example.invalid/fixture.tga\n" + target + "\n");
        var empty = new Dictionary<string, OptionalValue<string>>();
        foreach (var installationRoot in new[] { root, root + Path.DirectorySeparatorChar })
        {
            var spec = new LaunchSpec(
                new InstallationSpec(installationRoot),
                new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset),
                new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset),
                new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset),
                OptionalValue<PlayerVehicleSpec>.Unset,
                new EnvironmentSpec(empty, empty, empty, empty, empty, empty, empty, empty),
                new LaunchBehaviorSpec(),
                Presentation: new SessionPresentationSpec(SplashMode.Unset),
                InternetTextures: new InternetTexturesSpec(InternetTexturesMode.Override, OptionalValue<string>.Set(profile)));
            var plan = SessionVisualAssets.Build(spec);
            Assert(plan.Deletions.Contains(target, StringComparer.OrdinalIgnoreCase), "valid .itx target was rejected for root: " + installationRoot);
        }
    }
    finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
// Correction pass: every rejection rule of the .itx target guard, for roots
// with and without a trailing separator, including a root that itself lives
// below a folder named "texture".
static void ItxTargetGuardRejections()
{
    var parent = Path.Combine(Path.GetTempPath(), "omsilaunch-itx-guard-" + Guid.NewGuid().ToString("N"));
    var root = Path.Combine(parent, "texture", "OMSI");
    var outside = Path.Combine(parent, "outside");
    try
    {
        Directory.CreateDirectory(Path.Combine(root, "Sceneryobjects", "Demo", "texture"));
        Directory.CreateDirectory(outside);
        var link = Path.Combine(root, "Sceneryobjects", "Linked");
        var mklink = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + outside + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        mklink.WaitForExit(); Assert(mklink.ExitCode == 0 && Directory.Exists(link), "junction fixture could not be created");
        Directory.CreateDirectory(Path.Combine(outside, "texture"));
        var cases = new (string Target, bool Accepted, string Why)[]
        {
            (@"Sceneryobjects\Demo\texture\ok.tga", true, "valid target"),
            (@"Sceneryobjects\Demo\TEXTURE\ok.tga", true, "case-insensitive texture folder"),
            (@"options.cfg", false, "target outside any texture folder while the root is below 'texture'"),
            (@"Sceneryobjects\Demo\ok.tga", false, "no texture folder below the root"),
            (@"Sceneryobjects\Demo\texture", false, "texture folder itself is not a file target"),
            (@"..\outside\texture\x.tga", false, "parent traversal"),
            (@"Sceneryobjects\..\..\texture\x.tga", false, "nested traversal"),
            (Path.Combine(outside, "texture", "x.tga"), false, "absolute external path"),
            (@"\texture\x.tga", false, "root-relative path"),
            (@"Sceneryobjects\Linked\texture\x.tga", false, "junction to a folder outside the installation"),
            (@"Sceneryobjects\mytexture\x.tga", false, "segment containing 'texture' as a substring"),
            (@"Sceneryobjects\texture_backup\x.tga", false, "segment 'texture_backup'"),
            (@"Sceneryobjects\texture2\x.tga", false, "segment 'texture2'"),
            (@"Sceneryobjects\Texture\x.tga", true, "Windows casing 'Texture'"),
            (@"Sceneryobjects\Demo\\texture\\ok.tga", true, "duplicate separators"),
            ("Sceneryobjects/Demo/texture/ok.tga", true, "forward slashes"),
            (@"Sceneryobjects\Demo\texture\.\ok.tga", true, "current-directory segment")
        };
        var empty = new Dictionary<string, OptionalValue<string>>();
        foreach (var installationRoot in new[] { root, root + Path.DirectorySeparatorChar })
            foreach (var (target, accepted, why) in cases)
            {
                var profile = Path.Combine(parent, "fixture.itx");
                File.WriteAllText(profile, "https://example.invalid/fixture.tga\n" + target + "\n");
                var spec = new LaunchSpec(new InstallationSpec(installationRoot), new WorldSpec(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new EnvironmentSpec(empty, empty, empty, empty, empty, empty, empty, empty), new LaunchBehaviorSpec(), Presentation: new SessionPresentationSpec(SplashMode.Unset), InternetTextures: new InternetTexturesSpec(InternetTexturesMode.Override, OptionalValue<string>.Set(profile)));
                var wasAccepted = true;
                try { SessionVisualAssets.Build(spec); }
                catch (InvalidDataException error) when (error.Message == "OL_E_ITX_TARGET_OUTSIDE_TEXTURE_PATH") { wasAccepted = false; }
                Assert(wasAccepted == accepted, (accepted ? "rejected " : "accepted ") + why + " (" + target + ") for root " + installationRoot);
            }
        // Spelling variants of one target collapse to one canonical transaction path.
        foreach (var spelling in new[] { @"Sceneryobjects\Demo\\texture\\ok.tga", "Sceneryobjects/Demo/texture/ok.tga", @"Sceneryobjects\Demo\texture\.\ok.tga" })
            Assert(SessionVisualAssets.ValidateTextureTarget(root + Path.DirectorySeparatorChar, spelling) == @"Sceneryobjects\Demo\texture\ok.tga", "target was not canonicalized: " + spelling);
        Directory.Delete(link);
    }
    finally { if (Directory.Exists(parent)) Directory.Delete(parent, true); }
}
// Correction pass: the installation lease has one name however the root is
// spelled (trailing separator, case, relative segments).
static void LeaseRootNormalization()
{
    var root = Fixture(); try
    {
        using var first = InstallationLease.Acquire(root);
        AssertThrows(() => InstallationLease.Acquire(root + Path.DirectorySeparatorChar), "trailing separator produced a second lease");
        AssertThrows(() => InstallationLease.Acquire(root.ToUpperInvariant()), "different case produced a second lease");
        AssertThrows(() => InstallationLease.Acquire(Path.Combine(root, "sub", "..")), "relative segments produced a second lease");
        Assert(InstallationLease.NormalizeRoot("C:\\") == "C:\\" && InstallationLease.NormalizeRoot(root + "\\") == root, "root normalization regression");
    }
    finally { Directory.Delete(root, true); }
}
// Addendum invariants A and B: lexically equivalent spellings of one
// installation share one identity (and therefore one lease); different
// installations never do, including a sibling whose name extends the root.
static void InstallationIdentity()
{
    var parent = Path.Combine(Path.GetTempPath(), "omsilaunch-identity-" + Guid.NewGuid().ToString("N"));
    var omsi = Path.Combine(parent, "OMSI"); var a = Path.Combine(parent, "OMSI-A"); var b = Path.Combine(parent, "OMSI-B");
    Directory.CreateDirectory(omsi); Directory.CreateDirectory(a); Directory.CreateDirectory(b); Directory.CreateDirectory(Path.Combine(parent, "foo"));
    try
    {
        var spellings = new[] { omsi, omsi + "\\", omsi + "\\.", Path.Combine(parent, "foo", "..", "OMSI"), omsi.ToUpperInvariant(), omsi.ToLowerInvariant(), omsi.Replace('\\', '/'), omsi + "\\\\", omsi + "\\sub\\.." };
        var key = InstallationPaths.IdentityKey(omsi);
        foreach (var spelling in spellings) Assert(InstallationPaths.IdentityKey(spelling) == key, "same installation produced a different identity: " + spelling);
        using (var lease = InstallationLease.Acquire(omsi))
            foreach (var spelling in spellings) AssertThrows(() => InstallationLease.Acquire(spelling).Dispose(), "a second lease was granted for spelling " + spelling);
        Assert(InstallationPaths.IdentityKey(a) != InstallationPaths.IdentityKey(b) && InstallationPaths.IdentityKey(a) != key, "different installations share an identity");
        using (var leaseA = InstallationLease.Acquire(a)) using (var leaseB = InstallationLease.Acquire(b)) using (var leaseRoot = InstallationLease.Acquire(omsi)) { }
        Assert(InstallationPaths.NormalizeRoot("C:\\") == "C:\\" && InstallationPaths.NormalizeRoot("C:\\.") == "C:\\", "drive root normalization regression");
        // Containment uses the same definition and is segment based.
        Assert(!InstallationPaths.TryGetContainedRelativePath(omsi, Path.Combine(a, "x.tga"), out _), "sibling OMSI-A was treated as inside OMSI (prefix match)");
        Assert(!InstallationPaths.TryGetContainedRelativePath(omsi, "..\\OMSI-A\\x.tga", out _), "traversal to a sibling was contained");
        Assert(!InstallationPaths.TryGetContainedRelativePath(omsi, omsi, out _) && !InstallationPaths.TryGetContainedRelativePath(omsi, ".", out _), "the root itself is not a contained path");
        Assert(!InstallationPaths.TryGetContainedRelativePath(omsi, "Z:\\elsewhere\\x.tga", out _), "another volume was contained");
        Assert(InstallationPaths.TryGetContainedRelativePath(omsi + "\\", "a//b\\\\c.txt", out var canonical) && canonical == "a\\b\\c.txt", "canonical relative spelling regression: " + canonical);
        Assert(InstallationPaths.TryGetContainedRelativePath(omsi, "..foo\\x.txt", out var dotted) && dotted == "..foo\\x.txt", "a name starting with two dots is not traversal");
    }
    finally { Directory.Delete(parent, true); }
}
static void RuntimeCommandWireGuard()
{
    var expected = new RuntimeCommand(Guid.NewGuid(), 42, "time.read", new Dictionary<string, string> { ["scope"] = "session" });
    var bytes = RuntimeCommandWire.SerializeRequest(expected);
    Assert(RuntimeCommandWire.TryDeserializeRequest(bytes, out var decoded) && decoded is not null && decoded.SessionId == expected.SessionId && decoded.RequestId == expected.RequestId && decoded.Operation == expected.Operation && decoded.Arguments is not null && decoded.Arguments["scope"] == "session", "runtime command did not round-trip");
    bytes[^1] ^= 1; Assert(!RuntimeCommandWire.TryDeserializeRequest(bytes, out _), "corrupt runtime request was accepted");
}
static void RuntimeCommandSessionBinding()
{
    var session = Guid.NewGuid(); using var store = CurrentRuntimeCommandStore.Create(session);
    var request = new RuntimeCommand(session, 9, "time.read");
    var pending = store.RequestAsync(request, TimeSpan.FromSeconds(2));
    using (var mapping = MemoryMappedFile.OpenExisting(store.Name, MemoryMappedFileRights.ReadWrite))
    using (var view = mapping.CreateViewAccessor(0, 65_536, MemoryMappedFileAccess.ReadWrite))
    {
        var until = DateTimeOffset.UtcNow.AddSeconds(1); while (view.ReadInt32(0) != 1 && DateTimeOffset.UtcNow < until) Thread.Sleep(10);
        Assert(view.ReadInt32(0) == 1, "runtime request was not staged");
        var response = RuntimeCommandWire.SerializeResponse(new RuntimeCommandResult(session, 9, true, Values: new Dictionary<string, string> { ["ok"] = "true" }));
        view.Write(4, response.Length); view.WriteArray(8, response, 0, response.Length); view.Write(0, 2); view.Flush();
    }
    Assert(pending.GetAwaiter().GetResult().Succeeded, "runtime response was not consumed");
}
static void RuntimePermanentPluginInstallation()
{
    var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Orphan-Test-" + Guid.NewGuid().ToString("N"));
    var source = Path.Combine(root, "source"); Directory.CreateDirectory(source); Directory.CreateDirectory(Path.Combine(root, "plugins"));
    try
    {
        foreach (var file in new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json", "OmsiLaunch.Plugin.dll", "OmsiLaunch.Api.dll" })
            File.WriteAllText(Path.Combine(source, file), file + "-source");
        var native = Path.Combine(root, "OmsiLaunch.Native.x86.dll"); File.WriteAllText(native, "native-source");
        var deployment = RuntimeArtifactSet.Load(source, native);
        foreach (var artifact in deployment.Artifacts) File.Copy(artifact.SourcePath, Path.Combine(root, artifact.DestinationRelativePath), true);
        deployment.ValidateInstalled(root);
        var collision = deployment.Artifacts.Single(x => x.DestinationRelativePath.EndsWith("OmsiLaunch.Api.dll", StringComparison.OrdinalIgnoreCase));
        File.WriteAllText(Path.Combine(root, collision.DestinationRelativePath), "partial-product-update");
        AssertThrows(() => deployment.ValidateInstalled(root), "permanent plugin hash mismatch was accepted");
        Assert(File.Exists(Path.Combine(root, collision.DestinationRelativePath)), "plugin validation removed a permanent product file");
    }
    finally { if (Directory.Exists(root)) Directory.Delete(root, true); }
}
static void OptionsNoOp()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[unknown]\r\nvalue\r\n[useActTime]\r\n\r\n");
    Assert(input.SequenceEqual(new OptionsDocument(input).Serialize()), "no-op options round-trip changed bytes");
}
static void OptionsVectorBlock()
{
    const string original = "[AIMaxCountRandom]\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n[unknown]\r\nkeep\r\n";
    var input = System.Text.Encoding.ASCII.GetBytes(original);

    var random = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("traffic.randomVehicles", "150").Apply(input));
    Assert(random.Contains("[AIMaxCountRandom]\r\n150\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n", StringComparison.Ordinal), "randomVehicles did not preserve the multiline block tail");
    Assert(!random.Contains("150 2 3", StringComparison.Ordinal), "AIMaxCountRandom was collapsed into one line");

    var humans = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("traffic.humans", "200").Apply(input));
    Assert(humans.Contains("[AIMaxCountRandom]\r\n1\r\n200\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n", StringComparison.Ordinal), "humans did not preserve the multiline block tail");

    var cumulative = ConfigurationCatalog.CreatePatch("traffic.randomVehicles", "150").Apply(input);
    cumulative = ConfigurationCatalog.CreatePatch("traffic.humans", "200").Apply(cumulative);
    var cumulativeText = System.Text.Encoding.ASCII.GetString(cumulative);
    Assert(cumulativeText.Contains("[AIMaxCountRandom]\r\n150\r\n200\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n", StringComparison.Ordinal), "consecutive AIMaxCountRandom patches were not cumulative");
    Assert(cumulativeText.Contains("[unknown]\r\nkeep\r\n", StringComparison.Ordinal), "AIMaxCountRandom patch changed unrelated options content");

    var shortBlock = System.Text.Encoding.ASCII.GetBytes("[AIMaxCountRandom]\r\n7\r\n");
    var expanded = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("traffic.humans", "200").Apply(shortBlock));
    Assert(expanded.Contains("[AIMaxCountRandom]\r\n7\r\n200\r\n0\r\n0\r\n0\r\n0\r\n0\r\n0\r\n0", StringComparison.Ordinal), "AIMaxCountRandom did not add only missing components as OMSI lines");
}
static void DiscoveryFixture()
{
    var root = Fixture(); try
    {
        Write(root, "maps\\Grundorf\\global.cfg", "[entrypoints]\r\n1\r\n4\r\n103\r\n0\r\n202.000\r\n9.000\r\n136.000\r\n0.000\r\n0.173\r\n0.000\r\n0.985\r\n4\r\nNordspitze Bauernhof\r\n"); Write(root, "situations\\23A 2005.osn", "maps\\Grundorf\\global.cfg"); Write(root, "Vehicles\\Demo\\demo.bus", "[friendlyname]\r\nDemo\r\nBus\r\n[number]\r\nnumbers.org\r\n[registration_automatic]\r\nD-A\r\n[registration_free]\r\n"); Write(root, "Vehicles\\Demo\\demo.cti", "[item]\r\nBlue\r\nvar\r\nblue.png\r\n"); Write(root, "Vehicles\\Demo\\demo.hof", "");
        var catalog = new FileSystemContentCatalog(root); Assert(catalog.ResolveMap("maps\\Grundorf\\global.cfg").IsReadable, "map was not discovered"); Assert(catalog.EnumerateEntrypoints("maps\\Grundorf\\global.cfg").Single().Identity.StartsWith("maps\\Grundorf\\global.cfg#entrypoint:", StringComparison.Ordinal) && catalog.EnumerateEntrypoints("maps\\Grundorf\\global.cfg").Single().DisplayName == "Nordspitze Bauernhof", "stable opaque entrypoint identity was not discovered"); Assert(catalog.EnumerateSituations().Single().MapIdentity == "maps\\Grundorf\\global.cfg", "situation map was not parsed"); Assert(catalog.EnumerateVehicles().Single().DisplayName == "Demo Bus", "vehicle friendly name was not parsed"); Assert(catalog.EnumerateRepaints("Vehicles\\Demo\\demo.bus").Single().Identity.EndsWith("#item:1", StringComparison.Ordinal) && catalog.EnumerateRepaints("Vehicles\\Demo\\demo.bus").Single().DisplayName == "Blue", "stable repaint identity was not discovered"); Assert(catalog.EnumerateFleetNumbers("Vehicles\\Demo\\demo.bus").Single().SourceIdentity == "numbers.org", "fleet number source was not discovered"); Assert(catalog.EnumerateRegistrations("Vehicles\\Demo\\demo.bus").Select(x => x.Mode).OrderBy(x => x).SequenceEqual(new[] { "registration_automatic", "registration_free" }), "registration modes were not discovered");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionRestore()
{
    var root = Fixture(); try
    {
        Write(root, "options.cfg", "original"); var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = System.Text.Encoding.ASCII.GetBytes("temporary") }); transaction.ApplyAsync().GetAwaiter().GetResult(); Assert(File.ReadAllText(Path.Combine(root, "options.cfg")) == "temporary", "transaction did not apply"); transaction.RestoreAsync().GetAwaiter().GetResult(); Assert(File.ReadAllText(Path.Combine(root, "options.cfg")) == "original", "transaction did not restore exact original");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionOptionsOverlayRestore()
{
    var root = Fixture(); try
    {
        var original = System.Text.Encoding.ASCII.GetBytes("[AIMaxCountRandom]\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n");
        Write(root, "options.cfg", System.Text.Encoding.ASCII.GetString(original));
        var overlay = ConfigurationCatalog.CreatePatch("traffic.humans", "200").Apply(ConfigurationCatalog.CreatePatch("traffic.randomVehicles", "150").Apply(original));
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["options.cfg"] = overlay });
        transaction.ApplyAsync().GetAwaiter().GetResult();
        Assert(File.ReadAllBytes(Path.Combine(root, "options.cfg")).SequenceEqual(overlay), "options overlay did not apply its multiline bytes");
        transaction.RestoreAsync().GetAwaiter().GetResult();
        Assert(File.ReadAllBytes(Path.Combine(root, "options.cfg")).SequenceEqual(original), "options.cfg did not restore byte-for-byte after overlay rollback");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionAbsentOverlayRestore()
{
    var root = Fixture(); try
    {
        const string overlay = "GUI\\NewSplashscreen_PTB.bmp";
        var path = Path.Combine(root, overlay);
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [overlay] = System.Text.Encoding.ASCII.GetBytes("session-splash") });
        transaction.ApplyAsync().GetAwaiter().GetResult();
        Assert(File.Exists(path), "session overlay was not created for an absent destination");
        transaction.RestoreAsync().GetAwaiter().GetResult();
        Assert(!File.Exists(path), "restore did not remove a session overlay whose original destination was absent");
        Assert(!File.Exists(Path.Combine(root, ".omsilaunch", "journal.json")), "journal remained after absent-overlay restore");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionDeleteRestore()
{
    var root = Fixture(); try
    {
        Write(root, "Texture\\standard.ipr", "original-cache"); Write(root, "Sceneryobjects\\Demo\\texture\\target.tga", "original-target");
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { ["Texture\\standard.itx"] = System.Text.Encoding.ASCII.GetBytes("override") }, new[] { "Texture\\standard.ipr", "Sceneryobjects\\Demo\\texture\\target.tga" });
        transaction.ApplyAsync().GetAwaiter().GetResult(); Assert(!File.Exists(Path.Combine(root, "Texture\\standard.ipr")) && !File.Exists(Path.Combine(root, "Sceneryobjects\\Demo\\texture\\target.tga")), "session deletion did not apply");
        transaction.RestoreAsync().GetAwaiter().GetResult(); Assert(File.ReadAllText(Path.Combine(root, "Texture\\standard.ipr")) == "original-cache" && File.ReadAllText(Path.Combine(root, "Sceneryobjects\\Demo\\texture\\target.tga")) == "original-target", "session deletion did not restore exact originals");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionFailureBoundaries()
{
    // These are the session ownership boundaries. The production service must
    // call the same restore path whether launch fails before or after process
    // ownership, during startup, or after an early process exit.
    foreach (var boundary in new[] { "before-process-start", "after-process-start", "startup-failure", "early-process-exit" })
    {
        var root = Fixture();
        try
        {
            var original = System.Text.Encoding.UTF8.GetBytes("original-" + boundary);
            Write(root, "options.cfg", System.Text.Encoding.UTF8.GetString(original));
            var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>
            {
                ["options.cfg"] = System.Text.Encoding.UTF8.GetBytes("temporary-" + boundary),
                ["GUI\\NewSplashscreen_ENG.bmp"] = System.Text.Encoding.UTF8.GetBytes("session-only")
            });
            transaction.ApplyAsync().GetAwaiter().GetResult();
            Assert(transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), boundary + " did not retain recovery after apply");

            try { throw new InvalidOperationException(boundary); }
            catch (InvalidOperationException) { transaction.RestoreAsync().GetAwaiter().GetResult(); }

            Assert(File.ReadAllBytes(Path.Combine(root, "options.cfg")).SequenceEqual(original), boundary + " did not restore original bytes");
            Assert(!File.Exists(Path.Combine(root, "GUI", "NewSplashscreen_ENG.bmp")), boundary + " did not remove originally absent file");
            Assert(!transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), boundary + " cleared completion before verified restore");
        }
        finally { Directory.Delete(root, true); }
    }
}
static void TransactionRestoreFailureRecovery()
{
    var root = Fixture();
    try
    {
        const string relative = "options.cfg";
        var path = Path.Combine(root, relative);
        var original = System.Text.Encoding.UTF8.GetBytes("original-options");
        Write(root, relative, System.Text.Encoding.UTF8.GetString(original));
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [relative] = System.Text.Encoding.UTF8.GetBytes("temporary-options") });
        transaction.ApplyAsync().GetAwaiter().GetResult();

        using (var heldOpen = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var failed = false;
            try { transaction.RestoreAsync().GetAwaiter().GetResult(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failed = true; }
            Assert(failed, "locked original did not force a restore failure");
            Assert(transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "applied and unrestored transaction lost its recovery journal");
            Assert(File.ReadAllBytes(path).SequenceEqual(System.Text.Encoding.UTF8.GetBytes("temporary-options")), "failed restore unexpectedly changed the locked file");
        }

        var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
        recovery.RestorePendingAsync().GetAwaiter().GetResult();
        Assert(File.ReadAllBytes(path).SequenceEqual(original), "subsequent recovery did not restore original bytes");
        Assert(!recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal remained after fully verified recovery");
        recovery.RestorePendingAsync().GetAwaiter().GetResult();
        Assert(File.ReadAllBytes(path).SequenceEqual(original), "idempotent recovery changed restored bytes");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionAbsentFileRecovery()
{
    var root = Fixture();
    try
    {
        const string existing = "options.cfg";
        const string absentA = "GUI\\NewSplashscreen_PTB.bmp";
        const string absentB = "GUI\\NewSplashscreen_ENG.bmp";
        const string foreign = "GUI\\third-party.bmp";
        var existingPath = Path.Combine(root, existing);
        var original = System.Text.Encoding.UTF8.GetBytes("original-options");
        Write(root, existing, System.Text.Encoding.UTF8.GetString(original));
        Write(root, foreign, "third-party-original");

        // Order is intentional: the first absent file is removed before the
        // locked existing file causes a partial rollback failure.
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>
        {
            [absentA] = System.Text.Encoding.UTF8.GetBytes("session-splash-ptb"),
            [existing] = System.Text.Encoding.UTF8.GetBytes("temporary-options"),
            [absentB] = System.Text.Encoding.UTF8.GetBytes("session-splash-eng")
        });
        transaction.ApplyAsync().GetAwaiter().GetResult();
        Assert(File.Exists(Path.Combine(root, absentA)) && File.Exists(Path.Combine(root, absentB)), "transaction did not create absent owned files");

        using (var heldOpen = new FileStream(existingPath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            var failed = false;
            try { transaction.RestoreAsync().GetAwaiter().GetResult(); }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException) { failed = true; }
            Assert(failed, "partial rollback fixture did not fail on the locked original");
            Assert(!File.Exists(Path.Combine(root, absentA)), "partial rollback did not remove the first absent-owned file");
            Assert(transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "partial rollback removed its recovery journal");
        }

        var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
        recovery.RestorePendingAsync().GetAwaiter().GetResult();
        Assert(File.ReadAllBytes(existingPath).SequenceEqual(original), "recovery did not restore the existing original bytes");
        Assert(!File.Exists(Path.Combine(root, absentA)) && !File.Exists(Path.Combine(root, absentB)), "recovery did not restore original absence for owned temporary files");
        Assert(File.ReadAllText(Path.Combine(root, foreign)) == "third-party-original", "recovery removed a file outside the transaction");
        Assert(!recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "journal remained after mixed-file recovery verified absence");

        recovery.RestorePendingAsync().GetAwaiter().GetResult();
        Assert(!File.Exists(Path.Combine(root, absentA)) && !File.Exists(Path.Combine(root, absentB)), "repeated recovery recreated an absent original");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionAbsentFileOwnership()
{
    var root = Fixture();
    try
    {
        const string relative = "GUI\\NewSplashscreen_DEU.bmp";
        var path = Path.Combine(root, relative);
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [relative] = System.Text.Encoding.UTF8.GetBytes("managed-splash") });
        transaction.ApplyAsync().GetAwaiter().GetResult();
        File.WriteAllText(path, "third-party-replacement");

        var failed = false;
        try { transaction.RestoreAsync().GetAwaiter().GetResult(); }
        catch (IOException error) when (error.Message.StartsWith("OL_E_RECOVERY_ABSENT_OWNERSHIP_MISMATCH", StringComparison.Ordinal)) { failed = true; }
        Assert(failed, "restore accepted a non-owned replacement for an originally absent file");
        Assert(File.ReadAllText(path) == "third-party-replacement", "restore removed a non-owned replacement file");
        Assert(transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "ownership mismatch cleared the recovery journal");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionEmptyJournalRestore()
{
    var root = Fixture();
    try
    {
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
        transaction.ApplyAsync().GetAwaiter().GetResult();
        Assert(transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "empty transaction did not retain its lifecycle journal after apply");
        transaction.RestoreAsync().GetAwaiter().GetResult();
        Assert(!transaction.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "empty transaction did not complete its verified no-op restore");
        transaction.RestorePendingAsync().GetAwaiter().GetResult();
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionRecoveryThenApplyOwnership()
{
    var root = Fixture();
    try
    {
        new FileConfigurationTransaction(root, new Dictionary<string, byte[]>()).ApplyAsync().GetAwaiter().GetResult();
        const string relative = "GUI\\NewSplashscreen_FRA.bmp";
        var transaction = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [relative] = System.Text.Encoding.UTF8.GetBytes("managed-splash") });
        transaction.RestorePendingAsync().GetAwaiter().GetResult();
        transaction.ApplyAsync().GetAwaiter().GetResult();
        var journal = File.ReadAllText(Path.Combine(root, ".omsilaunch", "journal.json"));
        Assert(journal.Contains("\"AppliedSha256\":\"", StringComparison.Ordinal), "recovery cleared ownership evidence for the subsequent transaction");
        transaction.RestoreAsync().GetAwaiter().GetResult();
        Assert(!File.Exists(Path.Combine(root, relative)), "subsequent transaction did not restore original absence");
    }
    finally { Directory.Delete(root, true); }
}
static void TransactionLegacyJournalOwnershipMigration()
{
    var root = Fixture();
    try
    {
        const string relative = "GUI\\NewSplashscreen_ENG.bmp";
        var bytes = System.Text.Encoding.UTF8.GetBytes("managed-splash");
        new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [relative] = bytes }).ApplyAsync().GetAwaiter().GetResult();
        var journalPath = Path.Combine(root, ".omsilaunch", "journal.json");
        var legacy = System.Text.RegularExpressions.Regex.Replace(File.ReadAllText(journalPath), "\\\"AppliedSha256\\\":\\\"[A-F0-9]+\\\"", "\"AppliedSha256\":null");
        File.WriteAllText(journalPath, legacy);

        var compatibleRecovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]> { [relative] = bytes });
        compatibleRecovery.RestorePendingAsync().GetAwaiter().GetResult();
        Assert(!File.Exists(Path.Combine(root, relative)), "compatible legacy recovery did not restore original absence");
        Assert(!compatibleRecovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "compatible legacy recovery retained its journal");
    }
    finally { Directory.Delete(root, true); }
}
static void ConfigurationCatalogTest()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[unknown]\r\nvalue\r\n[no_collision_terrain]\r\n");
    var patch = ConfigurationCatalog.CreatePatch("simulation.collisionTerrain", "true");
    var enabled = System.Text.Encoding.ASCII.GetString(patch.Apply(input)); Assert(!enabled.Contains("[no_collision_terrain]", StringComparison.Ordinal) && enabled.Contains("[unknown]", StringComparison.Ordinal), "positive collision semantic did not remove the inverted token");
    var disabled = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("simulation.collisionTerrain", "false").Apply(input)); Assert(disabled.Contains("[no_collision_terrain]", StringComparison.Ordinal), "false collision semantic did not preserve the inverted token");
    var vectorInput = System.Text.Encoding.ASCII.GetBytes("[AIMaxCountRandom]\r\n1\r\n2\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9\r\n");
    var vector = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("traffic.humans", "12").Apply(vectorInput)); Assert(vector.Contains("1\r\n12\r\n3\r\n4\r\n5\r\n6\r\n7\r\n8\r\n9", StringComparison.Ordinal), "semantic traffic patch did not preserve the multiline vector tail");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("unknown.key", "true"), "unknown semantic setting was accepted");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("graphics.texture", "1"), "non-writable semantic setting was accepted");
}
static void ConfigurationRanges()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[performance_minObjSize]\r\n0.010\r\n[maxFPS]\r\n60\r\n[noAutoSave]\r\n");
    var percent = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("graphics.minObjectScreenPercent", "1.50").Apply(input)); Assert(percent.Contains("[performance_minObjSize]\r\n0.015", StringComparison.Ordinal), "percent transform was not applied");
    var fps = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("graphics.maxFPS", "200").Apply(input)); Assert(fps.Contains("[maxFPS]\r\n200", StringComparison.Ordinal), "maxFPS range endpoint was rejected");
    var autosave = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("general.autoSave", "true").Apply(input)); Assert(!autosave.Contains("[noAutoSave]", StringComparison.Ordinal), "positive autoSave did not remove negative raw token");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("graphics.tileDistance", "21"), "out of range tileDistance was accepted");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("sound.masterVolume", "1.1"), "out of range volume was accepted");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("traffic.humans", "1001"), "out of range human count was accepted");
}
static void ConfigurationCompoundPatches()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[no_multithreading_calculate]\r\n[unknown]\r\nkeep\r\n[smokesystems]\r\n1\r\n200\r\n0\r\n1\r\n");
    var reduced = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("advanced.reducedMultithreading", "true").Apply(input));
    Assert(reduced.Contains("[no_multithreading_calculate]", StringComparison.Ordinal) && reduced.Contains("[no_multithreading_texload]", StringComparison.Ordinal), "reduced multithreading did not synchronize both native flags");
    var particles = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("graphics.particles", "true,800,true,true").Apply(input));
    Assert(particles.Contains("[smokesystems]\r\n1\r\n800\r\n1\r\n0", StringComparison.Ordinal) && particles.Contains("[unknown]\r\nkeep", StringComparison.Ordinal), "particle semantic patch changed unrelated tokens or inverted reflection state");
    var stencil = System.Text.Encoding.ASCII.GetString(ConfigurationCatalog.CreatePatch("graphics.stencilShadows", "false").Apply(input));
    Assert(stencil.Contains("[shadow_stencil]\r\noff", StringComparison.Ordinal), "stencil shadow boolean was not translated to native on/off");
    AssertThrows(() => ConfigurationCatalog.CreatePatch("graphics.realTimeReflections", "disabled"), "unproven reflection disabled value was accepted");
}
static void KeyboardPatch()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[entry]\r\nquicksave\r\n31\r\n4\r\n[unknown]\r\nvalue\r\n"); var document = new KeyboardDocument(input);
    Assert(document.Bindings.Single() == new KeyboardBinding("quicksave", 31, true, false), "keyboard binding was not parsed");
    document.SetBinding(new KeyboardBinding("quicksave", 30, false, true)); var output = System.Text.Encoding.ASCII.GetString(document.Serialize());
    Assert(output.Contains("quicksave\r\n30\r\n2", StringComparison.Ordinal) && output.Contains("[unknown]\r\nvalue", StringComparison.Ordinal), "keyboard patch changed unknown data or modifiers incorrectly");
}
static void ControllerPatch()
{
    var input = System.Text.Encoding.ASCII.GetBytes("[ctrl]\r\nWheel\r\n0\r\n[axis]\r\n0\r\n[buttons]\r\n0\r\n[FFScale]\r\n1.000\r\n1.000\r\n[unknown]\r\nvalue\r\n"); var document = new GameControllerDocument(input);
    Assert(document.Devices.Single() == new ControllerDevice("Wheel", false, "1.000", "1.000"), "controller block was not parsed"); document.SetActive("Wheel", true); document.SetForceFeedback("Wheel", "0.750", "0.500"); var output = System.Text.Encoding.ASCII.GetString(document.Serialize());
    Assert(output.Contains("[ctrl]\r\nWheel\r\n1", StringComparison.Ordinal) && output.Contains("[FFScale]\r\n0.750\r\n0.500", StringComparison.Ordinal) && output.Contains("[unknown]\r\nvalue", StringComparison.Ordinal), "controller patch changed unrelated data");
}
static void RuntimeManifest()
{
    var root = Fixture(); try
    {
        var plugin = Path.Combine(root, "plugin"); Directory.CreateDirectory(plugin);
        foreach (var file in new[] { "OmsiLaunch.Plugin.opl", "OmsiLaunch.PluginNE.dll", "OmsiLaunch.Plugin.dll", "OmsiLaunch.Api.dll", "OmsiLaunch.Interop.dll", "OmsiLaunch.Builds.Omsi23004.dll", "OmsiLaunch.Plugin.deps.json", "OmsiLaunch.Plugin.runtimeconfig.json" }) File.WriteAllText(Path.Combine(plugin, file), file);
        var native = Path.Combine(root, "OmsiLaunch.Native.x86.dll"); File.WriteAllText(native, "native");
        var artifacts = RuntimeArtifactSet.Load(plugin, native); Assert(artifacts.Artifacts.Count == 9, "runtime manifest omitted an artifact"); Assert(artifacts.Artifacts.Any(x => x.DestinationRelativePath.EndsWith(Path.Combine("plugins", "OmsiLaunch.Interop.dll"), StringComparison.OrdinalIgnoreCase)) && artifacts.Artifacts.Any(x => x.DestinationRelativePath.EndsWith(Path.Combine("plugins", "OmsiLaunch.Builds.Omsi23004.dll"), StringComparison.OrdinalIgnoreCase)), "managed dependency closure was not delivered to permanent plugins"); Assert(artifacts.Artifacts.All(x => x.DestinationRelativePath.StartsWith("plugins" + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)), "runtime destination escaped plugins");
    }
    finally { Directory.Delete(root, true); }
}
static void ProcessProviderFastExit()
{
    var platform = new CurrentWindowsX64Platform(); var executable = Path.Combine(Environment.SystemDirectory, "where.exe");
    using var process = platform.StartAsync(new StartupProcessRequest(executable, Environment.SystemDirectory, new Dictionary<string, string> { ["OMSILAUNCH_PROCESS_TEST"] = "1" }), "test", CancellationToken.None).GetAwaiter().GetResult();
    Assert(process.ProcessId > 0 && process.ThreadId > 0, "CreateProcessW did not return raw PROCESS_INFORMATION"); Assert(process.Identity.CreationTimeUtc.Year > 2000, "GetProcessTimes did not produce a creation timestamp");
    platform.WaitForExitAsync(process, CancellationToken.None).GetAwaiter().GetResult(); Assert(platform.HasExited(process), "owned process handle did not observe exit");
}
static void LeaseCrossThreadRelease()
{
    var root = Fixture(); try { using var first = InstallationLease.Acquire(root); AssertThrows(() => InstallationLease.Acquire(root), "second lease holder was accepted"); Task.Run(first.Dispose).GetAwaiter().GetResult(); using var second = InstallationLease.Acquire(root); }
    finally { Directory.Delete(root, true); }
}
static string Fixture() { var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Test-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(root); return root; }
static void Write(string root, string relative, string text) { var path = Path.Combine(root, relative); Directory.CreateDirectory(Path.GetDirectoryName(path)!); File.WriteAllText(path, text); }
static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
static void AssertThrows(Action action, string message) { try { action(); } catch (ArgumentException) { return; } catch (InvalidOperationException) { return; } catch (InvalidDataException) { return; } throw new InvalidOperationException(message); }
