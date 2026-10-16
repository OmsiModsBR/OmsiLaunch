using System.Reflection;
using System.Text;
using OmsiLaunch.Api;
using OmsiLaunch.Core;

// Generates docs/reference/public-api-inventory.md from the compiled public
// surface: every exported type of OmsiLaunch.Api, OmsiLaunch.Core and
// OmsiLaunch.Process with its stability and every public member signature.
// The documentation gate regenerates it and fails when the page is stale; run
// the test project with --write-inventory to refresh it.
internal static class PublicApiInventory
{
    // Stability per type; anything not listed is STABLE_BETA.
    private static readonly Dictionary<string, string> Stability = new(StringComparer.Ordinal)
    {
        ["RuntimeCommandWire"] = "INTERNAL", ["TelemetryRingWire"] = "INTERNAL", ["StartupHandoff"] = "INTERNAL", ["StartupHandoffWire"] = "INTERNAL",
        ["D3DRuntimeApi"] = "EXPERIMENTAL", ["D3DDeviceStatus"] = "EXPERIMENTAL", ["D3DDeviceState"] = "EXPERIMENTAL", ["D3DTextureDescription"] = "EXPERIMENTAL",
        ["D3DTextureFormat"] = "EXPERIMENTAL", ["D3DTextureHandle"] = "EXPERIMENTAL", ["D3DTextureResourceState"] = "EXPERIMENTAL", ["D3DTextureUpdate"] = "EXPERIMENTAL", ["OmsiRuntimeException"] = "EXPERIMENTAL",
        ["DateSpec"] = "PARTIAL", ["TimeSpec"] = "PARTIAL", ["YearSpec"] = "PARTIAL", ["WeatherSpec"] = "PARTIAL", ["PlayerVehicleSpec"] = "PARTIAL", ["InputSpec"] = "PARTIAL", ["DiagnosticsSpec"] = "PARTIAL",
        ["SessionProfileCompiler"] = "EXPERIMENTAL", ["SessionProfilePackage"] = "EXPERIMENTAL", ["ProfileNew"] = "EXPERIMENTAL", ["ProfilePreset"] = "EXPERIMENTAL", ["SessionProfileException"] = "EXPERIMENTAL",
        ["SessionPlanner"] = "INTERNAL", ["LaunchValidation"] = "INTERNAL",
        ["CurrentRuntimeCommandStore"] = "INTERNAL", ["CurrentStartupHandoffStore"] = "INTERNAL", ["CurrentTelemetryStore"] = "INTERNAL", ["InstallationLease"] = "INTERNAL",
        ["IOmsiProcessController"] = "INTERNAL", ["LaunchedProcess"] = "INTERNAL", ["OmsiProcessState"] = "INTERNAL", ["ProcessIdentity"] = "INTERNAL", ["ReleaseManifest"] = "INTERNAL",
        ["RuntimeArtifact"] = "INTERNAL", ["RuntimeArtifactSet"] = "INTERNAL", ["StartupProcessRequest"] = "INTERNAL",
    };
    // Where the semantics of a type are documented.
    private static readonly Dictionary<string, string> Home = new(StringComparer.Ordinal)
    {
        ["LaunchSpec"] = "launchspec.md", ["InstallationSpec"] = "launchspec.md", ["WorldSpec"] = "launchspec.md", ["EntrypointSpec"] = "launchspec.md", ["DateSpec"] = "launchspec.md", ["TimeSpec"] = "launchspec.md", ["YearSpec"] = "launchspec.md",
        ["WeatherSpec"] = "launchspec.md", ["PlayerVehicleSpec"] = "launchspec.md", ["EnvironmentSpec"] = "launchspec.md", ["InputSpec"] = "launchspec.md", ["DiagnosticsSpec"] = "launchspec.md", ["SessionPresentationSpec"] = "launchspec.md",
        ["InternetTexturesSpec"] = "launchspec.md", ["LaunchBehaviorSpec"] = "launchspec.md", ["SessionProfileMetadata"] = "launchspec.md", ["OptionalValue"] = "launchspec.md", ["Presence"] = "launchspec.md", ["SemanticDate"] = "launchspec.md", ["SemanticTime"] = "launchspec.md",
        ["WorldMode"] = "launchspec.md", ["EntrypointMode"] = "launchspec.md", ["DateTimeMode"] = "launchspec.md", ["WeatherMode"] = "launchspec.md", ["SplashMode"] = "launchspec.md", ["InternetTexturesMode"] = "launchspec.md",
        ["RuntimeCommand"] = "runtime-control.md", ["RuntimeCommandResult"] = "runtime-control.md",
        ["PublicCapabilityRegistry"] = "capabilities.md", ["PublicCapabilityDescriptor"] = "capabilities.md", ["PublicCapabilityClassification"] = "capabilities.md", ["PublicCapabilityKind"] = "capabilities.md",
        ["PublicErrorCodes"] = "errors.md", ["PublicErrorDescriptor"] = "errors.md", ["PublicErrorCategory"] = "errors.md", ["PublicExitCode"] = "exit-codes.md",
        ["SessionProfileCompiler"] = "session-profiles.md", ["SessionProfilePackage"] = "session-profiles.md", ["ProfileNew"] = "session-profiles.md", ["ProfilePreset"] = "session-profiles.md", ["SessionProfileException"] = "session-profiles.md",
    };

    public static string Generate()
    {
        var text = new StringBuilder();
        text.AppendLine("# Public API Inventory");
        text.AppendLine();
        text.AppendLine("This page is generated from the compiled assemblies by `tests/OmsiLaunch.DocumentationTests` (`dotnet run --project tests/OmsiLaunch.DocumentationTests -- --write-inventory`); the documentation gate fails when it no longer matches the code. It lists every exported type of `OmsiLaunch.Api`, `OmsiLaunch.Core` and `OmsiLaunch.Process` with its stability and the signature of every public member. Semantics, preconditions, errors and examples are documented on the page named in each heading (default: [public API](public-api.md)). Stability vocabulary: `STABLE_BETA`, `EXPERIMENTAL`, `PARTIAL`, `INTERNAL` (public for technical reasons, not an integration surface), `UNAVAILABLE` (see [public API](public-api.md#stability-vocabulary)). Compiler-generated record members (`Equals`, `GetHashCode`, `ToString`, `Deconstruct`, `<Clone>$`, `EqualityContract`) are omitted.");
        foreach (var assembly in new[] { typeof(IOmsiLaunch).Assembly, typeof(OmsiLaunchService).Assembly, typeof(OmsiLaunch.Process.CurrentWindowsX64Platform).Assembly })
        {
            text.AppendLine();
            text.AppendLine("## `" + assembly.GetName().Name + "`");
            foreach (var type in assembly.GetExportedTypes().Where(t => !t.IsNested).OrderBy(t => t.Name, StringComparer.Ordinal))
            {
                var name = Short(type);
                var stability = Stability.GetValueOrDefault(name, "STABLE_BETA");
                var home = Home.GetValueOrDefault(name, "public-api.md");
                text.AppendLine();
                text.AppendLine("### `" + name + "`");
                text.AppendLine();
                text.AppendLine($"{Kind(type)} in `{type.Namespace}`. Stability: `{stability}`. Semantics: [{Path.GetFileNameWithoutExtension(home)}]({home}).");
                var members = Members(type).ToList();
                if (members.Count == 0) continue;
                text.AppendLine();
                text.AppendLine("| Member | Signature |");
                text.AppendLine("| --- | --- |");
                foreach (var (kind, signature) in members) text.AppendLine("| " + kind + " | `" + signature.Replace("|", "\\|") + "` |");
            }
        }
        return text.ToString();
    }

    private static string Short(Type t) => t.IsGenericType ? t.Name[..t.Name.IndexOf('`')] : t.Name;
    private static string Kind(Type type)
    {
        if (type.IsEnum) return "Enum (`" + TypeName(Enum.GetUnderlyingType(type)) + "`)";
        if (type.IsInterface) return "Interface";
        var record = type.GetMethod("<Clone>$") is not null || type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance).Any(m => m.Name == "PrintMembers");
        if (type.IsValueType) return record ? "Record struct" : "Struct";
        if (type.IsAbstract && type.IsSealed) return "Static class";
        return record ? (type.IsSealed ? "Sealed record" : "Record") : (type.IsSealed ? "Sealed class" : "Class");
    }
    private static IEnumerable<(string Kind, string Signature)> Members(Type type)
    {
        const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
        if (type.IsEnum) { foreach (var name in Enum.GetNames(type)) yield return ("value", name + " = " + Convert.ToInt64(Enum.Parse(type, name), System.Globalization.CultureInfo.InvariantCulture)); yield break; }
        foreach (var c in type.GetConstructors(flags).OrderBy(c => c.GetParameters().Length)) if (!(c.GetParameters().Length == 1 && c.GetParameters()[0].ParameterType == type)) yield return ("constructor", Short(type) + "(" + Params(c) + ")");
        foreach (var f in type.GetFields(flags).OrderBy(f => f.MetadataToken)) yield return (f.IsLiteral ? "constant" : f.IsStatic ? "static field" : "field", TypeName(f.FieldType) + " " + f.Name + (f.IsLiteral ? " = " + Literal(f.GetRawConstantValue()) : ""));
        foreach (var p in type.GetProperties(flags).Where(p => p.Name != "EqualityContract").OrderBy(p => p.MetadataToken))
        {
            var setter = p.SetMethod is { IsPublic: true } set ? (set.ReturnParameter.GetRequiredCustomModifiers().Any(x => x.Name == "IsExternalInit") ? " init;" : " set;") : "";
            yield return (p.GetMethod!.IsStatic ? "static property" : "property", TypeName(p.PropertyType) + " " + p.Name + " { get;" + setter + " }");
        }
        foreach (var e in type.GetEvents(flags)) yield return ("event", TypeName(e.EventHandlerType!) + " " + e.Name);
        foreach (var m in type.GetMethods(flags).Where(m => !m.IsSpecialName && m.Name is not ("Equals" or "GetHashCode" or "ToString" or "Deconstruct" or "PrintMembers" or "<Clone>$")).OrderBy(m => m.Name, StringComparer.Ordinal).ThenBy(m => m.GetParameters().Length))
            yield return (m.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) ? "extension method" : m.IsStatic ? "static method" : "method", TypeName(m.ReturnType) + " " + m.Name + "(" + Params(m) + ")");
    }
    private static string Literal(object? value) => value switch { null => "null", string s => "\"" + s + "\"", bool b => b ? "true" : "false", IFormattable f => f.ToString(null, System.Globalization.CultureInfo.InvariantCulture), _ => value.ToString()! };
    private static string Params(MethodBase m) => string.Join(", ", m.GetParameters().Select(p =>
        (p.Member.IsDefined(typeof(System.Runtime.CompilerServices.ExtensionAttribute)) && p.Position == 0 ? "this " : "") + (p.IsOut ? "out " : "") + TypeName(p.ParameterType) + " " + p.Name + (p.HasDefaultValue ? " = " + (p.DefaultValue is null ? (p.ParameterType.IsValueType ? "default" : "null") : Literal(p.DefaultValue)) : "")));
    private static string TypeName(Type t)
    {
        if (t.IsByRef) return TypeName(t.GetElementType()!);
        if (Nullable.GetUnderlyingType(t) is { } inner) return TypeName(inner) + "?";
        if (t.IsArray) return TypeName(t.GetElementType()!) + "[]";
        if (t.IsGenericParameter) return t.Name;
        if (!t.IsGenericType) return t.Name switch { "String" => "string", "Int32" => "int", "Int64" => "long", "UInt32" => "uint", "UInt64" => "ulong", "Boolean" => "bool", "Byte" => "byte", "Void" => "void", "Object" => "object", "Double" => "double", "Single" => "float", "UInt16" => "ushort", "Int16" => "short", _ => t.Name };
        return Short(t) + "<" + string.Join(", ", t.GetGenericArguments().Select(TypeName)) + ">";
    }
}
