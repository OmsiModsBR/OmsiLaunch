using System.Text;
using OmsiLaunch.Api;

// Generates the capability catalog section of docs/reference/capabilities.md
// from PublicCapabilityRegistry (R3): the page cannot drift from the catalog
// the API, CLI, plugin gate and GetCapabilitiesAsync are derived from. The
// section sits between the two markers; run the test project with
// --write-inventory to refresh it.
internal static class CapabilityCatalogDocument
{
    public const string Begin = "<!-- generated:capability-catalog:begin -->";
    public const string End = "<!-- generated:capability-catalog:end -->";

    public static string Generate()
    {
        var text = new StringBuilder();
        text.Append(Begin).Append('\n');
        text.Append("| Capability | Classification | Kind | Session | Operations | Native capability | Evidence | CLI | Note |\n");
        text.Append("| --- | --- | --- | --- | --- | --- | --- | --- | --- |\n");
        foreach (var family in PublicCapabilityRegistry.All.GroupBy(x => x.Family))
            foreach (var capability in family)
            {
                text.Append("| `").Append(capability.Id).Append("` | `").Append(capability.Classification).Append("` | ").Append(capability.Kind)
                    .Append(" | ").Append(capability.RequiresSession ? "Running" : "no")
                    .Append(" | ").Append(string.Join(", ", capability.EffectiveOperations.Select(x => "`" + x.Id + "`" + Notes(x))))
                    .Append(" | ").Append(Codes(capability.NativeCapabilities))
                    .Append(" | ").Append(capability.RuntimeValidation)
                    .Append(" | ").Append(Codes(capability.CliRoute.Split(" | ", StringSplitOptions.RemoveEmptyEntries)))
                    .Append(" | ").Append(Cell(capability.EvidenceNote ?? "")).Append(" |\n");
            }
        text.Append(End).Append('\n');
        return text.ToString();
    }

    // The page with its generated section replaced.
    public static string Splice(string page)
    {
        page = page.Replace("\r\n", "\n");
        var start = page.IndexOf(Begin, StringComparison.Ordinal);
        var end = page.IndexOf(End, StringComparison.Ordinal);
        if (start < 0 || end < start) throw new InvalidOperationException("capabilities.md has no generated capability catalog markers.");
        return page[..start] + Generate() + page[(end + End.Length + 1)..];
    }

    private static string Notes(PublicRuntimeOperation operation)
    {
        var notes = new List<string>();
        if (operation.Paged) notes.Add("paged");
        if (operation.MutationPolicy != PublicMutationPolicy.None) notes.Add(operation.MutationPolicy);
        return notes.Count == 0 ? "" : " (" + string.Join(", ", notes) + ")";
    }

    private static string Codes(IEnumerable<string> values) => string.Join(", ", values.Select(x => "`" + x + "`"));
    private static string Cell(string value) => value.Replace("|", "\\|", StringComparison.Ordinal);
}
