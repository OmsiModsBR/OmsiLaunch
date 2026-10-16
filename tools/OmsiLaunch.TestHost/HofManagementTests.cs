using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using OmsiLaunch.Api;
using OmsiLaunch.Configuration;
using OmsiLaunch.Core;

// Beta 4 HOF management: source/target resolution, add and mirror modes,
// conflict and confinement rules, and exact restore through the generic
// transaction (normal end and crash recovery).
internal static class HofManagementTests
{
    public static void PlanSourcesAndModes()
    {
        var root = Fixture(); try
        {
            // Collection -> two vehicles (add): every file becomes an overlay.
            var plan = Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB\\b.bus", "Vehicles\\BusC" }));
            RequireSet(plan.Overlays.Keys, "Vehicles\\BusB\\x.hof", "Vehicles\\BusB\\y.hof", "Vehicles\\BusC\\x.hof", "Vehicles\\BusC\\y.hof");
            Require(plan.Deletions.Count == 0, "add mode removed files");
            Require(plan.Mutations.All(x => x.SemanticKey == "hof.add" && x.RequestedValue == ".omsilaunch\\hof\\berlin" && x.Operation == "exact-file-overlay"), "add mutations are not described");

            // Vehicle -> vehicle (mirror): different same-name file is replaced
            // in its existing spelling, other target HOFs become deletions.
            plan = Build(root, new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusA\\a.bus", new[] { "Vehicles\\BusB\\b.bus" }, HofApplyMode.Mirror));
            RequireSet(plan.Overlays.Keys, "Vehicles\\BusB\\A1.hof", "Vehicles\\BusB\\a2.hof");
            Require(plan.Overlays.Keys.Contains("Vehicles\\BusB\\A1.hof", StringComparer.Ordinal), "the existing target spelling was not preserved");
            RequireSet(plan.Deletions, "Vehicles\\BusB\\b_old.hof");

            // An identical target file needs no transaction entry.
            File.WriteAllText(Path.Combine(root, "Vehicles\\BusC\\x.hof"), "collection x");
            plan = Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusC" }));
            RequireSet(plan.Overlays.Keys, "Vehicles\\BusC\\y.hof");
            // ...and an identical file is not removed by a mirror of the same source.
            plan = Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusC" }, HofApplyMode.Mirror));
            Require(plan.Deletions.Count == 0, "mirror removed a file that equals the source");

            // Directory next to content, file filter, single file.
            RequireSet(Build(root, new HofOperationSpec(HofSourceKind.Directory, "maps\\Map\\hof", new[] { "Vehicles\\BusC" })).Overlays.Keys, "Vehicles\\BusC\\m.hof");
            RequireSet(Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }, Files: new[] { "Y.HOF" })).Overlays.Keys, "Vehicles\\BusB\\y.hof");
            RequireSet(Build(root, new HofOperationSpec(HofSourceKind.File, "maps\\Map\\hof\\m.hof", new[] { "Vehicles\\BusA" })).Overlays.Keys, "Vehicles\\BusA\\m.hof");

            // Two operations writing the same bytes merge; different bytes conflict.
            plan = Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }), new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }));
            RequireSet(plan.Overlays.Keys, "Vehicles\\BusB\\x.hof", "Vehicles\\BusB\\y.hof");
            Expect("OL_E_HOF_CONFLICT", () => Build(root, new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusA", new[] { "Vehicles\\BusC" }), new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusB", new[] { "Vehicles\\BusC" })));
            Expect("OL_E_HOF_CONFLICT", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }, HofApplyMode.Add, new[] { "x.hof" }), new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }, HofApplyMode.Mirror, new[] { "y.hof" })));
        }
        finally { Cleanup(root); }
    }

    public static void PlanRejectsUnsafeInput()
    {
        var root = Fixture(); try
        {
            Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "..\\x", new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Directory, "..\\outside", new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Directory, root, new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "maps\\Map" })));
            Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\..\\..\\Vehicles\\BusB" })));
            Expect("OL_E_HOF_TARGET_NOT_FOUND", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\Missing\\m.bus" })));
            Expect("OL_E_HOF_SOURCE_NOT_FOUND", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "missing", new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_SOURCE_NOT_FOUND", () => Build(root, new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusC", new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_SOURCE_NOT_FOUND", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusB" }, Files: new[] { "absent.hof" })));
            Expect("OL_E_HOF_INVALID", () => Build(root, new HofOperationSpec(HofSourceKind.File, "Vehicles\\BusA\\a.bus", new[] { "Vehicles\\BusB" })));
            Expect("OL_E_HOF_INVALID", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", Array.Empty<string>())));
            Expect("OL_E_HOF_INVALID", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusA\\a1.hof" })));

            // A source or target that traverses a junction is rejected.
            var outside = Path.Combine(root, "outside"); Directory.CreateDirectory(outside); File.WriteAllText(Path.Combine(outside, "o.hof"), "outside");
            var sourceLink = Path.Combine(root, ".omsilaunch", "hof", "linked");
            var targetLink = Path.Combine(root, "Vehicles", "Linked");
            CreateJunction(sourceLink, outside); CreateJunction(targetLink, outside);
            try
            {
                Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "linked", new[] { "Vehicles\\BusB" })));
                Expect("OL_E_HOF_PATH_ESCAPE", () => Build(root, new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\Linked" })));
            }
            finally { Directory.Delete(sourceLink); Directory.Delete(targetLink); }

            // The planner reports HOF problems as typed, non-runnable diagnostics.
            var spec = Spec(root, new HofOperationSpec(HofSourceKind.Collection, "missing", new[] { "Vehicles\\BusB" }));
            var sessionPlan = new SessionPlanner(new OmsiLaunch.Process.CurrentWindowsX64Platform()).PlanAsync(spec).GetAwaiter().GetResult();
            Require(!sessionPlan.IsRunnable && sessionPlan.Diagnostics.Any(x => x.Code == "OL_E_HOF_SOURCE_NOT_FOUND"), "the planner did not report OL_E_HOF_SOURCE_NOT_FOUND");
        }
        finally { Cleanup(root); }
    }

    // Normal session end: the transaction restores every HOF byte-for-byte,
    // removes created files and brings back mirrored-away files.
    public static void TransactionExactRestore()
    {
        var root = Fixture(); try
        {
            var before = Snapshot(root);
            var plan = Build(root, new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusA", new[] { "Vehicles\\BusB" }, HofApplyMode.Mirror), new HofOperationSpec(HofSourceKind.Collection, "berlin", new[] { "Vehicles\\BusC" }));
            var transaction = new FileConfigurationTransaction(root, plan.Overlays, plan.Deletions);
            transaction.ApplyAsync().GetAwaiter().GetResult();
            Require(File.ReadAllText(Path.Combine(root, "Vehicles\\BusB\\A1.hof")) == "bus a one" && !File.Exists(Path.Combine(root, "Vehicles\\BusB\\b_old.hof")) && File.Exists(Path.Combine(root, "Vehicles\\BusC\\y.hof")), "HOF overlays were not applied");
            transaction.RestoreAsync().GetAwaiter().GetResult();
            RequireSame(before, Snapshot(root), "normal restore");
        }
        finally { Cleanup(root); }
    }

    // Owner crash after apply: a later recovery restores the same exact state.
    public static void RecoveryAfterCrash()
    {
        var root = Fixture(); try
        {
            var before = Snapshot(root);
            var plan = Build(root, new HofOperationSpec(HofSourceKind.Vehicle, "Vehicles\\BusA", new[] { "Vehicles\\BusB", "Vehicles\\BusC" }, HofApplyMode.Mirror));
            var crashed = new FileConfigurationTransaction(root, plan.Overlays, plan.Deletions);
            crashed.ApplyAsync().GetAwaiter().GetResult();
            crashed.RecordProcessAsync(2_000_000_000, DateTimeOffset.UtcNow, Path.Combine(root, "Omsi.exe")).GetAwaiter().GetResult();
            var recovery = new FileConfigurationTransaction(root, new Dictionary<string, byte[]>());
            Require(recovery.HasPendingRecoveryAsync().GetAwaiter().GetResult(), "no journal was left by the crashed owner");
            recovery.RestorePendingAsync().GetAwaiter().GetResult();
            RequireSame(before, Snapshot(root), "crash recovery");
        }
        finally { Cleanup(root); }
    }

    private static HofPlanner.Plan Build(string root, params HofOperationSpec[] operations) => HofPlanner.Build(Spec(root, operations));

    private static LaunchSpec Spec(string root, params HofOperationSpec[] operations)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(root), new(WorldMode.NewMap, OptionalValue<string>.Unset, OptionalValue<string>.Unset, OptionalValue<int>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset), new(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset), OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new(), HofManagement: new(operations));
    }

    private static string Fixture()
    {
        var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-Hof-" + Guid.NewGuid().ToString("N"));
        Write(root, "Vehicles\\BusA\\a.bus", "[friendlyname]\r\nBus A\r\n");
        Write(root, "Vehicles\\BusA\\a1.hof", "bus a one");
        Write(root, "Vehicles\\BusA\\a2.hof", "bus a two");
        Write(root, "Vehicles\\BusB\\b.bus", "[friendlyname]\r\nBus B\r\n");
        Write(root, "Vehicles\\BusB\\A1.hof", "bus b own a1");
        Write(root, "Vehicles\\BusB\\b_old.hof", "bus b old");
        Write(root, "Vehicles\\BusC\\c.ovh", "[friendlyname]\r\nCar C\r\n");
        Write(root, ".omsilaunch\\hof\\berlin\\x.hof", "collection x");
        Write(root, ".omsilaunch\\hof\\berlin\\y.hof", "collection y");
        Write(root, "maps\\Map\\hof\\m.hof", "map hof");
        var stamp = new DateTime(2020, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        foreach (var file in Directory.EnumerateFiles(Path.Combine(root, "Vehicles"), "*", SearchOption.AllDirectories)) File.SetLastWriteTimeUtc(file, stamp);
        return root;
    }

    // Every file below Vehicles\ with its content hash and last-write time.
    private static IReadOnlyDictionary<string, string> Snapshot(string root) =>
        Directory.EnumerateFiles(Path.Combine(root, "Vehicles"), "*", SearchOption.AllDirectories).ToDictionary(
            path => Path.GetRelativePath(root, path),
            path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))) + "@" + File.GetLastWriteTimeUtc(path).Ticks,
            StringComparer.Ordinal);

    private static void RequireSame(IReadOnlyDictionary<string, string> expected, IReadOnlyDictionary<string, string> actual, string context)
    {
        var differences = expected.Keys.Union(actual.Keys).Where(key => !expected.TryGetValue(key, out var a) || !actual.TryGetValue(key, out var b) || a != b).ToArray();
        Require(differences.Length == 0, context + " differs from the original Vehicles\\ tree: " + string.Join(", ", differences));
    }

    private static void RequireSet(IEnumerable<string> actual, params string[] expected)
    {
        var set = actual.ToHashSet(StringComparer.OrdinalIgnoreCase);
        Require(set.SetEquals(expected), "expected {" + string.Join(", ", expected) + "} but got {" + string.Join(", ", set) + "}");
    }

    private static void Expect(string code, Action action)
    {
        try { action(); }
        catch (HofPlanningException exception) when (exception.Code == code) { return; }
        catch (HofPlanningException exception) { throw new InvalidOperationException("expected " + code + " but got " + exception.Code + ": " + exception.Message); }
        throw new InvalidOperationException("expected " + code + " but the operation was accepted");
    }

    private static void Write(string root, string relative, string text)
    {
        var path = Path.Combine(root, relative);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllBytes(path, Encoding.ASCII.GetBytes(text));
    }

    private static void CreateJunction(string link, string target)
    {
        using var mklink = Process.Start(new ProcessStartInfo("cmd.exe", "/c mklink /J \"" + link + "\" \"" + target + "\"") { UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true })!;
        mklink.WaitForExit();
        Require(mklink.ExitCode == 0 && Directory.Exists(link), "junction fixture could not be created");
    }

    private static void Cleanup(string root) { try { Directory.Delete(root, true); } catch (IOException) { } }
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
