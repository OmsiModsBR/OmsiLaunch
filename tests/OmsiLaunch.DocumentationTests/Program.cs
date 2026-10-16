using System.Reflection;
using System.Text.RegularExpressions;
using OmsiLaunch.Api;
using OmsiLaunch.Core;

// Documentation quality gate. It compares the normative English documentation
// under docs/ with the code that defines the public surface, so that a new
// flag, route, capability, error code, enum value or public type cannot ship
// undocumented, and no relative link in the documentation is broken.
var repository = FindRepositoryRoot();
var docs = Path.Combine(repository, "docs");
var failures = new List<string>();
var inventoryPath = Path.Combine(docs, "reference", "public-api-inventory.md");
var capabilitiesPath = Path.Combine(docs, "reference", "capabilities.md");
if (args.Contains("--write-inventory"))
{
    File.WriteAllText(inventoryPath, PublicApiInventory.Generate().Replace("\r\n", "\n")); Console.WriteLine("written " + inventoryPath);
    File.WriteAllText(capabilitiesPath, CapabilityCatalogDocument.Splice(File.ReadAllText(capabilitiesPath))); Console.WriteLine("written " + capabilitiesPath);
    return;
}
// Normative English pages; release notes of earlier betas and localized copies are historical.
IEnumerable<string> NormativePages() => Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories)
    .Where(file => !file.Contains(Path.DirectorySeparatorChar + "localized" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !Path.GetFileName(file).StartsWith("release-0.1.0-beta", StringComparison.Ordinal) && !file.Contains(Path.DirectorySeparatorChar + "adr" + Path.DirectorySeparatorChar, StringComparison.Ordinal))
    .Append(Path.Combine(repository, "README.md"));
IEnumerable<(string Page, string Language, string Body)> CodeBlocks() => NormativePages().SelectMany(page =>
    Regex.Matches(File.ReadAllText(page).Replace("\r\n", "\n"), @"^```([\w-]*)\n(.*?)^```", RegexOptions.Multiline | RegexOptions.Singleline).Select(m => (Path.GetRelativePath(repository, page), m.Groups[1].Value, m.Groups[2].Value)));

string Doc(string relative) { var path = Path.Combine(docs, relative); return File.Exists(path) ? File.ReadAllText(path) : throw new FileNotFoundException("Missing normative documentation page", path); }
void Check(bool condition, string message) { if (!condition) failures.Add(message); }
void Gate(string name, Action action)
{
    try { action(); Console.WriteLine("PASS docs." + name); }
    catch (Exception error) { failures.Add(name + ": " + error.Message); Console.Error.WriteLine("FAIL docs." + name + " " + error.Message); }
}

Gate("cli-flags", () =>
{
    var cli = Doc("reference/cli.md");
    foreach (var flag in CliInput.KnownFlags) Check(cli.Contains("`/" + flag + (flag == "?" ? "`" : "") , StringComparison.Ordinal) || cli.Contains("`/" + flag + ":", StringComparison.Ordinal), "CLI flag not documented: /" + flag);
    foreach (var flag in CliInput.AcceptedNoEffectFlags) Check(Regex.IsMatch(cli, "/" + Regex.Escape(flag) + @"[^\n]*ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT"), "no-effect flag not marked ACCEPTED_FOR_COMPATIBILITY / CURRENTLY_NO_EFFECT: /" + flag);
    foreach (var word in CliInput.CommandWordsAccepted) Check(Regex.IsMatch(cli, @"(^|[^\w-])" + Regex.Escape(word) + @"([^\w-]|$)", RegexOptions.Multiline), "CLI command word not documented: " + word);
    foreach (var route in CliInput.HierarchicalRoutes) Check(cli.Contains("`" + route.Key + "`", StringComparison.Ordinal) && cli.Contains(route.Value, StringComparison.Ordinal), "hierarchical route not documented with its operation: " + route.Key + " -> " + route.Value);
    foreach (var code in Enum.GetValues<PublicExitCode>()) Check(Doc("reference/exit-codes.md").Contains("| " + (int)code + " |", StringComparison.Ordinal), "exit code not documented: " + code + " = " + (int)code);
});

Gate("capabilities", () =>
{
    var page = Doc("reference/capabilities.md");
    foreach (var descriptor in PublicCapabilityRegistry.All) Check(page.Contains("`" + descriptor.Id + "`", StringComparison.Ordinal), "capability not documented: " + descriptor.Id);
    foreach (var operation in PublicCapabilityRegistry.PublicRuntimeOperationIds) Check(page.Contains("`" + operation + "`", StringComparison.Ordinal), "public runtime operation not documented: " + operation);
    foreach (var classification in Enum.GetNames<PublicCapabilityClassification>()) Check(page.Contains(classification, StringComparison.Ordinal), "classification not explained: " + classification);
});

Gate("errors", () =>
{
    var page = Doc("reference/errors.md");
    foreach (var error in PublicErrorCodes.All) Check(page.Contains("`" + error.Code + "`", StringComparison.Ordinal), "error code not documented: " + error.Code);
    var pattern = new Regex(@"OL_[EW]_[A-Z0-9_]+");
    var sources = Directory.EnumerateFiles(Path.Combine(repository, "src"), "*.cs", SearchOption.AllDirectories)
        .Concat(Directory.EnumerateFiles(Path.Combine(repository, "src"), "*.cpp", SearchOption.AllDirectories))
        .Concat(Directory.EnumerateFiles(Path.Combine(repository, "tools", "OmsiLaunch.Cli"), "*.cs", SearchOption.AllDirectories))
        .Where(file => !file.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !file.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar, StringComparison.Ordinal));
    var known = PublicErrorCodes.All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
    foreach (var file in sources)
        foreach (Match match in pattern.Matches(File.ReadAllText(file)))
            if (!match.Value.EndsWith('_')) Check(known.Contains(match.Value), "error code in source but not in PublicErrorCodes: " + match.Value + " (" + Path.GetRelativePath(repository, file) + ")");
});

Gate("public-api", () =>
{
    var page = Doc("reference/public-api.md");
    var api = typeof(IOmsiLaunch).Assembly;
    foreach (var type in api.GetExportedTypes().Where(type => !type.IsNested))
    {
        var name = type.IsGenericType ? type.Name[..type.Name.IndexOf('`')] : type.Name;
        Check(page.Contains("`" + name, StringComparison.Ordinal), "public type not documented: " + type.FullName);
        if (type.IsEnum) foreach (var value in Enum.GetNames(type)) Check(page.Contains("`" + value + "`", StringComparison.Ordinal) || page.Contains(name + "." + value, StringComparison.Ordinal), "enum value not documented: " + name + "." + value);
    }
    foreach (var method in typeof(IOmsiLaunch).GetMethods()) Check(page.Contains("`" + method.Name + "`", StringComparison.Ordinal) || page.Contains(method.Name + "(", StringComparison.Ordinal), "IOmsiLaunch member not documented: " + method.Name);
    foreach (var stability in new[] { "STABLE_BETA", "EXPERIMENTAL", "PARTIAL", "INTERNAL", "UNAVAILABLE" }) Check(page.Contains(stability, StringComparison.Ordinal), "stability level not used in API docs: " + stability);
});

Gate("launchspec", () =>
{
    var page = Doc("reference/launchspec.md");
    void Walk(Type type, string path, HashSet<Type> seen)
    {
        if (!seen.Add(type)) return;
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            Check(page.Contains("`" + property.Name + "`", StringComparison.Ordinal), "LaunchSpec property not documented: " + path + "." + property.Name);
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            if (propertyType.Namespace == typeof(LaunchSpec).Namespace && !propertyType.IsEnum && propertyType != typeof(string) && !propertyType.IsGenericType) Walk(propertyType, path + "." + property.Name, seen);
        }
    }
    Walk(typeof(LaunchSpec), "LaunchSpec", new HashSet<Type>());
    foreach (var type in new[] { typeof(WorldMode), typeof(DateTimeMode), typeof(WeatherMode), typeof(SplashMode), typeof(InternetTexturesMode), typeof(Presence) })
        foreach (var value in Enum.GetNames(type)) Check(page.Contains("`" + value + "`", StringComparison.Ordinal), "LaunchSpec enum value not documented: " + type.Name + "." + value);
});

Gate("session-profiles", () =>
{
    var page = Doc("reference/session-profiles.md");
    foreach (var context in SessionProfileCompiler.SchemaKeys) foreach (var key in context.Value) Check(page.Contains("`" + key + "`", StringComparison.Ordinal), "session profile key not documented: " + context.Key + "." + key);
    Check(page.Contains(SessionProfileCompiler.Schema, StringComparison.Ordinal), "schema identifier not documented");
    Check(page.Contains("256 KiB", StringComparison.Ordinal), "file size limit not documented");
});

// Addendum invariant I: documentation never promotes runtime evidence that was
// not observed. A RUNTIME_VALIDATED row must cite runtime evidence (a session
// id, a matrix/Round id); offline test names alone are not runtime evidence;
// rows describing offline correction-pass work cannot be RUNTIME_VALIDATED.
Gate("runtime-status-evidence", () =>
{
    var page = Doc("status/runtime-validation-status.md");
    var runtimeEvidence = new Regex(@"\b[0-9a-f]{8}(-[0-9a-f]{4}){0,4}\b|\bRV-\d{3}\b|\bRA-\d{3}\b|\b[Mm]atrix\b|\bsession\b|\bsessions\b", RegexOptions.CultureInvariant);
    foreach (var line in page.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.StartsWith("| ", StringComparison.Ordinal)))
    {
        var cells = line.Split('|').Select(cell => cell.Trim()).ToArray();
        if (cells.Length < 4) continue;
        var feature = cells[1]; var state = cells[2]; var evidence = string.Join(" | ", cells.Skip(3));
        if (!state.StartsWith("RUNTIME_VALIDATED", StringComparison.Ordinal)) continue;
        Check(runtimeEvidence.IsMatch(evidence), "RUNTIME_VALIDATED without runtime evidence: " + feature);
        Check(!feature.Contains("correction pass", StringComparison.OrdinalIgnoreCase) && !evidence.Contains("correction pass", StringComparison.OrdinalIgnoreCase), "offline correction-pass work promoted to RUNTIME_VALIDATED: " + feature);
    }
});

Gate("structure", () =>
{
    foreach (var required in new[] { "README.md", "getting-started/installation.md", "getting-started/first-session.md", "reference/cli.md", "reference/cli-examples.md", "reference/launchspec.md", "reference/session-profiles.md", "reference/public-api.md", "reference/runtime-control.md", "reference/capabilities.md", "concepts/session-lifecycle.md", "concepts/transactions-and-recovery.md", "concepts/permanent-plugin.md", "reference/local-control.md", "reference/windows-tray.md", "reference/errors.md", "reference/exit-codes.md", "reference/packaging.md", "reference/compatibility.md", "reference/known-limitations.md", "status/runtime-validation-status.md" })
        Check(File.Exists(Path.Combine(docs, required)), "required documentation page missing: docs/" + required);
});

Gate("links", () =>
{
    var pages = Directory.EnumerateFiles(docs, "*.md", SearchOption.AllDirectories).Where(file => !file.Contains(Path.DirectorySeparatorChar + "localized" + Path.DirectorySeparatorChar, StringComparison.Ordinal)).Concat(Directory.EnumerateFiles(repository, "*.md", SearchOption.TopDirectoryOnly));
    var link = new Regex(@"\]\(([^)\s#]+)(#[^)]*)?\)");
    foreach (var page in pages)
        foreach (Match match in link.Matches(File.ReadAllText(page)))
        {
            var target = match.Groups[1].Value;
            if (target.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || target.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)) continue;
            var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, target.Replace('/', Path.DirectorySeparatorChar)));
            Check(File.Exists(resolved) || Directory.Exists(resolved), "broken link in " + Path.GetRelativePath(repository, page) + ": " + target);
        }
    // Anchors: a #fragment must name a heading (GitHub slug) or an explicit <a id> of the target page.
    var anchored = new Regex(@"\]\(([^)\s#]*)#([^)\s]+)\)");
    foreach (var page in pages)
        foreach (Match match in anchored.Matches(File.ReadAllText(page)))
        {
            var target = match.Groups[1].Value;
            if (target.Contains("://", StringComparison.Ordinal)) continue;
            var resolved = target.Length == 0 ? page : Path.GetFullPath(Path.Combine(Path.GetDirectoryName(page)!, target.Replace('/', Path.DirectorySeparatorChar)));
            if (!resolved.EndsWith(".md", StringComparison.OrdinalIgnoreCase) || !File.Exists(resolved)) continue;
            Check(LocalizationGate.Anchors(File.ReadAllText(resolved)).Contains(match.Groups[2].Value), "broken anchor in " + Path.GetRelativePath(repository, page) + ": " + target + "#" + match.Groups[2].Value);
        }
});

// Documentation closure gates (Beta 3): every direction, not only code -> docs.
Gate("public-api-inventory", () =>
{
    var expected = PublicApiInventory.Generate().Replace("\r\n", "\n");
    var actual = File.Exists(inventoryPath) ? File.ReadAllText(inventoryPath).Replace("\r\n", "\n") : "";
    Check(expected == actual, "docs/reference/public-api-inventory.md is stale; regenerate with --write-inventory");
});

Gate("capability-catalog", () =>
{
    var actual = File.ReadAllText(capabilitiesPath).Replace("\r\n", "\n");
    Check(CapabilityCatalogDocument.Splice(actual) == actual, "the generated capability catalog in docs/reference/capabilities.md is stale; regenerate with --write-inventory");
});

Gate("registry-cli-routes", () =>
{
    // Every CLI route the product advertises (help, capabilities) must be accepted by the parser.
    foreach (var descriptor in PublicCapabilityRegistry.All.Where(d => d.Classification is PublicCapabilityClassification.PublicStableBeta or PublicCapabilityClassification.PublicExperimental or PublicCapabilityClassification.Unsupported && d.CliRoute.Length > 0))
        foreach (var alternative in descriptor.CliRoute.Split(" | "))
        {
            var sample = Regex.Replace(Regex.Replace(alternative, @"\[[^\]]*\]", ""), @"<[^>]*>", "1").Trim();
            try
            {
                var input = CliInput.Parse(sample.Split(' ', StringSplitOptions.RemoveEmptyEntries));
                if (descriptor.ApiRoute.StartsWith("runtime.", StringComparison.Ordinal)) Check(input.RuntimeOperation is not null && PublicCapabilityRegistry.IsPublicRuntimeOperation(input.RuntimeOperation) || descriptor.Classification == PublicCapabilityClassification.Unsupported, "advertised route does not reach a public operation: " + descriptor.Id + " -> " + alternative);
            }
            catch (Exception error) { Check(false, "advertised CLI route is rejected by the parser: " + descriptor.Id + " -> '" + alternative + "' (" + error.Message + ")"); }
        }
});

Gate("cli-flags-reverse", () =>
{
    // A flag written in the documentation must be one the parser accepts.
    var known = CliInput.KnownFlags.ToHashSet(StringComparer.OrdinalIgnoreCase);
    // Only a backtick that opens a code span (start of line, space or punctuation before it).
    var inline = new Regex(@"(?<=^|[\s(|,;:])`(/[A-Za-z?][A-Za-z0-9\-]*)", RegexOptions.Multiline);
    var grammar = new HashSet<string>(StringComparer.Ordinal) { "key", "flag" };
    foreach (var page in NormativePages())
        foreach (Match match in inline.Matches(File.ReadAllText(page)))
        {
            var flag = match.Groups[1].Value[1..];
            if (flag.Length == 0 || grammar.Contains(flag)) continue;
            Check(known.Contains(flag), "documented flag unknown to the parser: /" + flag + " (" + Path.GetRelativePath(repository, page) + ")");
        }
});

Gate("errors-reverse", () =>
{
    // Every error or warning code written in the documentation must exist in the catalog (a trailing * marks a family).
    var known = PublicErrorCodes.All.Select(x => x.Code).ToHashSet(StringComparer.Ordinal);
    var code = new Regex(@"OL_[EW]_[A-Z0-9_]+(\*)?");
    foreach (var page in NormativePages())
        foreach (Match match in code.Matches(File.ReadAllText(page)))
        {
            if (match.Groups[1].Success || match.Value.EndsWith('_')) { Check(known.Any(k => k.StartsWith(match.Value.TrimEnd('*'), StringComparison.Ordinal)), "documented error family matches no code: " + match.Value); continue; }
            Check(known.Contains(match.Value), "documented error code not in PublicErrorCodes: " + match.Value + " (" + Path.GetRelativePath(repository, page) + ")");
        }
});

Gate("capabilities-reverse", () =>
{
    var page = Doc("reference/capabilities.md");
    var registryIds = PublicCapabilityRegistry.All.Select(d => d.Id).ToHashSet(StringComparer.Ordinal);
    var operations = PublicCapabilityRegistry.PublicRuntimeOperationIds.ToHashSet(StringComparer.Ordinal);
    var inRuntimeOperations = false;
    foreach (var line in page.Replace("\r\n", "\n").Split('\n'))
    {
        if (line.StartsWith("## ", StringComparison.Ordinal)) inRuntimeOperations = line.Contains("Public runtime operations", StringComparison.Ordinal);
        var first = Regex.Match(line, @"^\| `([a-z0-9.\-]+)` \|");
        if (!first.Success) continue;
        if (inRuntimeOperations) Check(operations.Contains(first.Groups[1].Value), "operation row for an id that is not a public runtime operation: " + first.Groups[1].Value);
        else if (line.Contains("| `Public", StringComparison.Ordinal) || line.Contains("| `InternalOnly`", StringComparison.Ordinal) || line.Contains("| `Unsupported`", StringComparison.Ordinal)) Check(registryIds.Contains(first.Groups[1].Value), "capability row for an id that is not in the registry: " + first.Groups[1].Value);
    }
    foreach (var operation in operations) Check(Regex.IsMatch(page, @"^\| `" + Regex.Escape(operation) + @"` \| (Read|Write|Action) \|", RegexOptions.Multiline), "public runtime operation without its own contract row: " + operation);
});

Gate("examples", () =>
{
    var checkedLines = 0; var checkedJson = 0; var checkedSpecs = 0; var checkedProfiles = 0;
    foreach (var (page, language, body) in CodeBlocks())
    {
        if (language is "text" or "powershell" or "")
            foreach (var raw in body.Split('\n'))
            {
                var line = Regex.Replace(raw.Trim(), @"^(&\s+)?(\.\\)?", "");
                var command = Regex.Match(line, @"^(OmsiLaunchW?\.exe)(\s+(?<args>.*))?$");
                if (!command.Success) continue;
                var arguments = command.Groups["args"].Value;
                arguments = Regex.Replace(arguments, @"\s+[|#].*$", "").Trim();
                checkedLines++;
                try
                {
                    var parsed = CliInput.Parse(SplitCommandLine(arguments));
                    // A map identity that planning would reject (for example one that
                    // lost its backslashes) still parses; check it the way planning does.
                    if (parsed.Map is { } map && !map.Contains('<')) Check(LaunchValidation.IsMapIdentity(map), "CLI example has an invalid map identity (" + page + "): " + line);
                }
                catch (Exception error) { Check(false, "CLI example does not parse (" + page + "): " + line + " -> " + error.Message); }
            }
        if (language == "json") { checkedJson++; try { System.Text.Json.JsonDocument.Parse(body); } catch (Exception error) { Check(false, "invalid JSON example (" + page + "): " + error.Message); } }
        if (language == "jsonc") { checkedSpecs++; try { LaunchSpecJson.Parse(System.Text.Encoding.UTF8.GetBytes(body)); } catch (Exception error) { Check(false, "LaunchSpec example rejected (" + page + "): " + error.Message); } }
        if (language == "yaml" && body.Contains("schema: omsilaunch.session-profile/v1", StringComparison.Ordinal)) { checkedProfiles++; CompileProfile(page, body); }
    }
    foreach (var file in Directory.EnumerateFiles(Path.Combine(repository, "examples"), "*.json", SearchOption.AllDirectories))
    { checkedSpecs++; try { LaunchSpecJson.Parse(File.ReadAllBytes(file)); } catch (Exception error) { Check(false, "example spec rejected: " + Path.GetRelativePath(repository, file) + " -> " + error.Message); } }
    foreach (var file in Directory.EnumerateFiles(Path.Combine(docs, "examples"), "profile.yaml", SearchOption.AllDirectories))
    { checkedProfiles++; CompileProfile(Path.GetRelativePath(repository, file), File.ReadAllText(file)); }
    Console.WriteLine($"  examples: cli={checkedLines} json={checkedJson} specs={checkedSpecs} profiles={checkedProfiles}");
    Check(checkedLines > 0 && checkedSpecs > 0 && checkedProfiles > 0, "no examples were found to check");
});

Gate("localization", () => LocalizationGate.Run(repository, Check));

void CompileProfile(string page, string yaml)
{
    // A profile example must compile for every preset it declares (the schema,
    // not the presence of a particular installation, is under test).
    var id = Regex.Match(yaml, @"^id:\s*(\S+)", RegexOptions.Multiline).Groups[1].Value.Trim('"');
    var root = Path.Combine(Path.GetTempPath(), "OmsiLaunch-DocProfile-" + Guid.NewGuid().ToString("N"));
    try
    {
        var folder = Path.Combine(root, ".omsilaunch", "session-profiles", id); Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "profile.yaml"), yaml);
        // Referenced package files exist in a real package; the example is checked for schema, not content.
        static string Local(string value) => value.Trim('"', '\'').Replace(@"\\", @"\").Replace('/', Path.DirectorySeparatorChar);
        foreach (Match asset in Regex.Matches(yaml, @"^\s*assets:\s*(\S+)", RegexOptions.Multiline)) Directory.CreateDirectory(Path.Combine(folder, Local(asset.Groups[1].Value)));
        foreach (Match itx in Regex.Matches(yaml, @"^\s*profile:\s*(\S+\.itx)", RegexOptions.Multiline))
        {
            var path = Path.Combine(folder, Local(itx.Groups[1].Value)); Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "http://127.0.0.1/example.tga\r\nSceneryobjects\\Example\\texture\\example.tga\r\n");
        }
        foreach (Match index in Regex.Matches(yaml, @"^\s*-\s*index:\s*(\d+)", RegexOptions.Multiline))
            try
            {
                var package = SessionProfileCompiler.Load(root, id, int.Parse(index.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture));
                // Plain YAML scalars keep a doubled backslash, which no map identity
                // resolves to. Only the packaged rmg-leste asset is documented as
                // shipping that way ("The packaged example").
                if (!page.EndsWith("profile.yaml", StringComparison.OrdinalIgnoreCase))
                    Check(!(package.New.Map ?? "").Contains(@"\\", StringComparison.Ordinal) && package.CompatibleMaps.All(map => !map.Contains(@"\\", StringComparison.Ordinal)),
                        "session profile example has a doubled backslash in a map identity (" + page + ")");
            }
            catch (Exception error) { Check(false, "session profile example rejected (" + page + ", preset " + index.Groups[1].Value + "): " + error.Message); }
    }
    finally { try { Directory.Delete(root, true); } catch (IOException) { } }
}

if (failures.Count != 0) { Console.Error.WriteLine(string.Join(Environment.NewLine, failures.Take(80))); if (failures.Count > 80) Console.Error.WriteLine("... " + (failures.Count - 80) + " more"); Environment.ExitCode = 1; }
else Console.WriteLine("PASS docs.all");

// The exact tokenization the shims use (CommandLineToArgvW), so quoting in the
// examples is checked the way a user's shell hands it to OmsiLaunch.
static string[] SplitCommandLine(string arguments)
{
    if (string.IsNullOrWhiteSpace(arguments)) return Array.Empty<string>();
    var pointer = CommandLineToArgvW("OmsiLaunch.exe " + arguments, out var count);
    try { return Enumerable.Range(1, count - 1).Select(i => System.Runtime.InteropServices.Marshal.PtrToStringUni(System.Runtime.InteropServices.Marshal.ReadIntPtr(pointer, i * IntPtr.Size))!).ToArray(); }
    finally { LocalFree(pointer); }
}
[System.Runtime.InteropServices.DllImport("shell32.dll", CharSet = System.Runtime.InteropServices.CharSet.Unicode, SetLastError = true)] static extern IntPtr CommandLineToArgvW(string commandLine, out int count);
[System.Runtime.InteropServices.DllImport("kernel32.dll")] static extern IntPtr LocalFree(IntPtr memory);

static string FindRepositoryRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "OmsiLaunch.sln"))) directory = directory.Parent;
    return directory?.FullName ?? throw new DirectoryNotFoundException("OmsiLaunch.sln was not found above " + AppContext.BaseDirectory);
}
