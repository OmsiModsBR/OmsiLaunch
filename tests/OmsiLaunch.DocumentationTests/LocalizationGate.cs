using System.Text.Json;
using System.Text.RegularExpressions;

// Localization gate. The English pages under docs/ are the canonical source;
// every locale listed in tools/Localization/locales.json must have a complete
// translated copy of the localized page set under docs/localized/<locale>/.
// Human prose is never compared; the gate checks what must not change in a
// translation: headings, fenced code blocks, table shape, inline code spans
// (flags, capability and operation ids, error codes, keys, identifiers),
// link destinations, and that every relative link and anchor resolves.
// tools/Localization/l10n.py applies the same rules for translators.
internal static class LocalizationGate
{
    private const string BannerMark = "<!-- l10n:";
    private static readonly Regex Fence = new(@"^\s*(```+|~~~+)");
    private static readonly Regex Heading = new(@"^(#{1,6})\s+(.*?)\s*#*\s*$");
    private static readonly Regex AnchorLine = new(@"^<a id=""[^""]*""></a>\s*$");
    private static readonly Regex Link = new(@"(?<!\!)\[((?:[^\[\]`]|`[^`]*`)*)\]\(([^)\s]+)(\s+""[^""]*"")?\)");
    private static readonly Regex Span = new(@"``(.+?)``|`([^`]+)`");

    public static void Run(string repository, Action<bool, string> check)
    {
        var docs = Path.Combine(repository, "docs");
        var localized = Path.Combine(docs, "localized");
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(repository, "tools", "Localization", "locales.json")));
        var pages = config.RootElement.GetProperty("pages").EnumerateArray().Select(p => p.GetString()!).ToArray();
        var locales = config.RootElement.GetProperty("locales").EnumerateArray().Select(l => l.GetProperty("id").GetString()!).ToArray();
        // Locale identity and public website route are separate fields; the route
        // is metadata for site integration and never a documentation path.
        var routeFormat = new Regex(@"^/([a-z0-9-]+/)?$");
        var routes = config.RootElement.GetProperty("locales").EnumerateArray()
            .Select(l => (Id: l.GetProperty("id").GetString()!, Route: l.TryGetProperty("website_route", out var r) ? r.GetString() : null))
            .Append((Id: config.RootElement.GetProperty("source_locale").GetString()!, Route: config.RootElement.TryGetProperty("source_website_route", out var s) ? s.GetString() : null)).ToArray();
        foreach (var (id, route) in routes) check(route is not null && routeFormat.IsMatch(route), "locale has no valid website_route: " + id);
        foreach (var duplicate in routes.Where(r => r.Route is not null).GroupBy(r => r.Route).Where(g => g.Count() > 1)) check(false, "website_route used by more than one locale: " + duplicate.Key);
        check(File.Exists(Path.Combine(localized, "LOCALIZATION-MANIFEST.md")), "docs/localized/LOCALIZATION-MANIFEST.md is missing");
        var manifest = File.Exists(Path.Combine(localized, "LOCALIZATION-MANIFEST.md")) ? File.ReadAllText(Path.Combine(localized, "LOCALIZATION-MANIFEST.md")) : "";
        foreach (var locale in locales) check(manifest.Contains("`" + locale + "`", StringComparison.Ordinal), "locale not listed in LOCALIZATION-MANIFEST.md: " + locale);
        foreach (var page in pages) check(File.Exists(Path.Combine(docs, page)), "localized page set names a missing English page: " + page);
        var unexpectedDirectories = Directory.Exists(localized) ? Directory.EnumerateDirectories(localized).Select(Path.GetFileName).Where(d => !locales.Contains(d)).ToArray() : Array.Empty<string?>();
        check(unexpectedDirectories.Length == 0, "unlisted locale directories: " + string.Join(", ", unexpectedDirectories));
        var checkedPages = 0;
        foreach (var locale in locales)
        {
            var root = Path.Combine(localized, locale);
            if (!Directory.Exists(root)) { check(false, "locale missing: " + locale); continue; }
            foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
                check(pages.Contains(relative), locale + ": file outside the localized page set: " + relative);
            }
            foreach (var page in pages)
            {
                var path = Path.Combine(root, page);
                if (!File.Exists(path)) { check(false, locale + ": missing translation " + page); continue; }
                checkedPages++;
                foreach (var problem in CheckPage(docs, root, locale, page, pages)) check(false, locale + " " + page + ": " + problem);
            }
        }
        Console.WriteLine($"  localization: locales={locales.Length} pages={pages.Length} checked={checkedPages}");
    }

    private static IEnumerable<string> CheckPage(string docs, string root, string locale, string page, string[] pages)
    {
        var englishPath = Path.Combine(docs, page);
        var path = Path.Combine(root, page);
        var english = File.ReadAllText(englishPath).Replace("\r\n", "\n");
        var translated = File.ReadAllText(path).Replace("\r\n", "\n");
        // The banner records the SHA-256 of the English page the translation
        // follows. Any later change of the English page, prose included, makes
        // the translation stale until it is updated and re-finalized.
        var banner = Regex.Match(translated, "^" + Regex.Escape(BannerMark + " source=" + page) + " source-sha256=([0-9a-f]{64}|unknown) -->$", RegexOptions.Multiline);
        if (!banner.Success) yield return "translation banner missing";
        else if (banner.Groups[1].Value != Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(english))).ToLowerInvariant())
            yield return "stale translation: the English page changed since it was translated";
        var body = StripL10n(translated);
        var (englishProse, englishCode) = Split(english);
        var (prose, code) = Split(body);
        var englishHeadings = Headings(englishProse); var headings = Headings(prose);
        if (!englishHeadings.Select(h => h.Level).SequenceEqual(headings.Select(h => h.Level))) yield return "heading structure differs from the English page";
        else
            foreach (var (en, loc) in englishHeadings.Zip(headings))
                if (en.Text.Contains('`') && !Spans(new[] { en.Text }).SequenceEqual(Spans(new[] { loc.Text }))) yield return "heading code differs: " + en.Text;
        if (englishCode.Count != code.Count) yield return $"code block count differs ({englishCode.Count} vs {code.Count})";
        else for (var i = 0; i < code.Count; i++) if (englishCode[i] != code[i]) yield return "code block " + (i + 1) + " differs from the English page";
        if (!TableShape(englishProse).SequenceEqual(TableShape(prose))) yield return "table rows or cells differ from the English page";
        var spans = Spans(prose).ToHashSet(StringComparer.Ordinal);
        foreach (var span in Spans(englishProse).Distinct()) if (!spans.Contains(span)) yield return "inline code missing: `" + span + "`";
        var englishTargets = Links(englishProse).Select(t => Logical(docs, englishPath, t, null)).OrderBy(t => t, StringComparer.Ordinal);
        var targets = Links(prose).Select(t => Logical(docs, path, t, root)).OrderBy(t => t, StringComparer.Ordinal);
        if (!englishTargets.SequenceEqual(targets)) yield return "link destinations differ from the English page";
        var anchorsHere = Anchors(translated);
        foreach (var target in Links(prose))
        {
            if (IsExternal(target)) continue;
            if (target.StartsWith('#')) { if (!anchorsHere.Contains(target[1..])) yield return "broken anchor: " + target; continue; }
            var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target.Split('#')[0]));
            if (resolved.StartsWith(docs + Path.DirectorySeparatorChar, StringComparison.Ordinal) && !resolved.StartsWith(Path.Combine(docs, "localized") + Path.DirectorySeparatorChar, StringComparison.Ordinal)
                && pages.Contains(Path.GetRelativePath(docs, resolved).Replace('\\', '/'))) yield return "link to a translated page points to the English page: " + target;
            if (!File.Exists(resolved) && !Directory.Exists(resolved)) { yield return "broken link: " + target; continue; }
            if (target.Contains('#') && resolved.EndsWith(".md", StringComparison.Ordinal) && !Anchors(File.ReadAllText(resolved)).Contains(target.Split('#', 2)[1])) yield return "broken anchor: " + target;
        }
        foreach (var target in Links(translated.Split('\n').Where(l => l.StartsWith('>')).ToList()))
            if (!IsExternal(target) && !File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(path)!, target.Split('#')[0])))) yield return "broken banner link: " + target;
        if (locale != "en-GB" && page != "reference/public-api-inventory.md")
        {
            var englishLines = englishProse.Where(IsProse).Select(l => l.Trim()).ToHashSet(StringComparer.Ordinal);
            var lines = prose.Where(IsProse).Select(l => l.Trim()).ToList();
            var same = lines.Count(englishLines.Contains);
            if (lines.Count > 0 && same * 5 > lines.Count) yield return $"{same} of {lines.Count} prose lines are identical to the English page";
        }
    }

    // A line with real prose: at least four words of three or more letters once
    // code spans, link targets, table pipes and ALL_CAPS tokens are removed.
    private static bool IsProse(string line)
    {
        var s = Link.Replace(StripSpans(line), m => m.Groups[1].Value);
        s = Regex.Replace(s, @"[A-Z0-9_]{2,}", " ");
        return Regex.Matches(s, @"[^\W\d_]{3,}").Count >= 4;
    }

    private static string StripL10n(string text)
    {
        var output = new List<string>(); var skip = false;
        foreach (var line in text.Split('\n'))
        {
            if (line.StartsWith(BannerMark, StringComparison.Ordinal)) { skip = true; continue; }
            if (skip && line.StartsWith('>')) continue;
            skip = false;
            if (AnchorLine.IsMatch(line)) continue;
            output.Add(line);
        }
        return string.Join("\n", output);
    }

    private static (List<string> Prose, List<string> Code) Split(string text)
    {
        var prose = new List<string>(); var code = new List<string>(); List<string>? block = null; var fence = "";
        foreach (var line in text.Split('\n'))
        {
            var match = Fence.Match(line);
            if (block is null && match.Success) { block = new() { line.Trim() }; fence = new string(match.Groups[1].Value[0], 3); continue; }
            if (block is not null)
            {
                block.Add(line.Trim());
                if (line.Trim().StartsWith(fence, StringComparison.Ordinal) && line.Trim().Trim(fence[0]).Length == 0) { code.Add(string.Join("\n", block)); block = null; }
                continue;
            }
            prose.Add(line);
        }
        if (block is not null) code.Add(string.Join("\n", block));
        return (prose, code);
    }

    private static List<(int Level, string Text)> Headings(IEnumerable<string> prose) =>
        prose.Select(l => Heading.Match(l)).Where(m => m.Success).Select(m => (m.Groups[1].Value.Length, m.Groups[2].Value)).ToList();

    private static IEnumerable<string> Spans(IEnumerable<string> prose) =>
        prose.SelectMany(l => Span.Matches(l).Select(m => m.Groups[1].Success ? m.Groups[1].Value : m.Groups[2].Value));

    private static string StripSpans(string line) => Regex.Replace(Regex.Replace(line, "``.+?``", ""), "`[^`]*`", "");

    private static IEnumerable<int> TableShape(IEnumerable<string> prose) =>
        prose.Select(l => l.Trim()).Where(l => l.StartsWith('|')).Select(l => StripSpans(l).Replace("\\|", "").Count(c => c == '|'));

    private static IEnumerable<string> Links(IEnumerable<string> prose) =>
        prose.SelectMany(l => Link.Matches(Regex.Replace(l, "`[^`]*`", m => m.Value.Contains("](") ? new string(' ', m.Value.Length) : m.Value)).Select(m => m.Groups[2].Value));

    private static bool IsExternal(string target) => Regex.IsMatch(target, "^[a-z][a-z0-9+.-]*:", RegexOptions.IgnoreCase);

    // The English file a link stands for: a link into the locale tree maps to
    // the English page with the same relative path.
    private static string Logical(string docs, string from, string target, string? localeRoot)
    {
        if (IsExternal(target) || target.StartsWith('#')) return target;
        var parts = target.Split('#', 2);
        var resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(from)!, parts[0]));
        if (localeRoot is not null && resolved.StartsWith(localeRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal))
            resolved = Path.Combine(docs, Path.GetRelativePath(localeRoot, resolved));
        return Path.GetRelativePath(docs, resolved).Replace('\\', '/') + (parts.Length > 1 ? "#" + parts[1] : "");
    }

    internal static HashSet<string> Anchors(string markdown)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        foreach (Match m in Regex.Matches(markdown, @"<a id=""([^""]+)""></a>")) set.Add(m.Groups[1].Value);
        foreach (var (_, text) in Headings(Split(markdown.Replace("\r\n", "\n")).Prose)) set.Add(Slug(text));
        return set;
    }

    private static string Slug(string heading)
    {
        var s = Regex.Replace(heading.Trim().ToLowerInvariant(), "<[^>]+>", "");
        s = Regex.Replace(s, @"[^\w\- ]", "");
        return s.Replace(' ', '-');
    }
}
