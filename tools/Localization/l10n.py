"""OmsiLaunch documentation localization tool.

The English pages under docs/ are the canonical en-US source. Every locale in
locales.json has a translated copy of each page of the localized set under
docs/localized/<locale>/ with the same relative path.

Commands (run from the repository root):

  python tools/Localization/l10n.py finalize <locale> [page ...] [--source-current]
      Idempotent post-processing of translated pages: inserts the translation
      banner (which records the SHA-256 of the English source the translation
      follows; kept as is unless --source-current is given, which records the
      current English page: use it only after the translation matches it), inserts an explicit <a id="english-slug"></a> anchor before every
      heading (so English anchors keep working after the heading text is
      translated) and rewrites relative links that leave the localized page set
      so they point to the English page.

  python tools/Localization/l10n.py check <locale|all> [page ...] [--allow-pending]
      Structural parity with the English page: same heading levels, identical
      fenced code blocks, same table rows and cell counts, every inline code
      span of the English page present, the same link destinations, every
      relative link resolving, banner present, and the recorded English source
      hash equal to the current English page (a stale translation fails even
      when only prose changed). Exit 1 on any problem.

  python tools/Localization/l10n.py leak <locale|all>
      Heuristic report of untranslated English prose and of cross-locale
      contamination (pt-BR/pt-PT, es-ES/es-LATAM, zh-CN/zh-TW, en-US/en-GB).

  python tools/Localization/l10n.py inventory <locale|all>
      Writes reference/public-api-inventory.md for the locale from the English
      generated inventory, translating only its fixed phrases.

Human prose is never compared textually.
"""
import hashlib
import io
import json
import os
import re
import sys

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".."))
DOCS = os.path.join(ROOT, "docs")
LOCALIZED = os.path.join(DOCS, "localized")
CONFIG = json.load(io.open(os.path.join(os.path.dirname(__file__), "locales.json"), encoding="utf-8"))
PAGES = CONFIG["pages"]
LOCALES = [l["id"] for l in CONFIG["locales"]]
GENERATED = {"reference/public-api-inventory.md"}
BANNER_MARK = "<!-- l10n:"
SOURCE_SHA = re.compile(r"source-sha256=([0-9a-f]{64}|unknown)")
ALLOW_PENDING = False

FENCE = re.compile(r"^(\s*)(```+|~~~+)(.*)$")
HEADING = re.compile(r"^(#{1,6})\s+(.*?)\s*#*\s*$")
ANCHOR_LINE = re.compile(r'^<a id="[^"]*"></a>\s*$')
LINK = re.compile(r"(?<!\!)\[((?:[^\[\]`]|`[^`]*`)*)\]\(([^)\s]+)(\s+\"[^\"]*\")?\)")


def read(path):
    return io.open(path, encoding="utf-8").read()


def write(path, text):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    io.open(path, "w", encoding="utf-8", newline="\n").write(text)


def locale_config(locale):
    for entry in CONFIG["locales"]:
        if entry["id"] == locale:
            return entry
    raise SystemExit("unknown locale " + locale)


def split_blocks(text):
    """Yields (kind, lines) with kind 'code' for fenced blocks, 'text' otherwise."""
    out, buf, code, fence = [], [], None, None
    for line in text.split("\n"):
        m = FENCE.match(line)
        if code is None and m:
            if buf:
                out.append(("text", buf))
            buf, code, fence = [], [line], m.group(2)[0] * 3
            continue
        if code is not None:
            code.append(line)
            if line.strip().startswith(fence) and len(code) > 1 and line.strip().strip(fence[0]) == "":
                out.append(("code", code))
                code = None
            continue
        buf.append(line)
    if code is not None:
        out.append(("code", code))
    if buf:
        out.append(("text", buf))
    return out


def code_blocks(text):
    return ["\n".join(l.strip() for l in lines) for kind, lines in split_blocks(text) if kind == "code"]


def prose_lines(text):
    for kind, lines in split_blocks(text):
        if kind == "text":
            for line in lines:
                yield line


def headings(text):
    result = []
    for line in prose_lines(text):
        m = HEADING.match(line)
        if m:
            result.append((len(m.group(1)), m.group(2)))
    return result


def strip_spans(line):
    line = re.sub(r"``.+?``", "", line)
    return re.sub(r"`[^`]*`", "", line)


def inline_spans(text):
    spans = []
    for line in prose_lines(text):
        for m in re.finditer(r"``(.+?)``|`([^`]+)`", line):
            spans.append(m.group(1) if m.group(1) is not None else m.group(2))
    return spans


def table_shape(text):
    shape = []
    for line in prose_lines(text):
        s = line.strip()
        if s.startswith("|"):
            shape.append(strip_spans(s).replace("\\|", "").count("|"))
    return shape


def links(text):
    result = []
    for line in prose_lines(text):
        for m in LINK.finditer(strip_code_for_links(line)):
            result.append(m.group(2))
    return result


def strip_code_for_links(line):
    # Links inside inline code are not links.
    return re.sub(r"`[^`]*`", lambda m: " " * len(m.group(0)) if "](" in m.group(0) else m.group(0), line)


def slug(heading_text):
    s = heading_text.strip().lower()
    s = re.sub(r"<[^>]+>", "", s)
    s = re.sub(r"[^\w\- ]", "", s, flags=re.UNICODE)
    return s.replace(" ", "-")


def is_external(target):
    return re.match(r"^[a-z][a-z0-9+.-]*:", target, re.I) is not None or target.startswith("#")


def english_path(page):
    return os.path.join(DOCS, *page.split("/"))


def localized_path(locale, page):
    return os.path.join(LOCALIZED, locale, *page.split("/"))


def resolve(from_file, target):
    path = target.split("#", 1)[0]
    if not path:
        return from_file
    return os.path.normpath(os.path.join(os.path.dirname(from_file), path.replace("/", os.sep)))


def logical_target(locale, page, target):
    """Maps a link of a localized page to the English file it stands for."""
    if is_external(target):
        return target
    frag = "#" + target.split("#", 1)[1] if "#" in target else ""
    abs_target = resolve(localized_path(locale, page), target)
    loc_root = os.path.join(LOCALIZED, locale) + os.sep
    if abs_target.startswith(loc_root):
        rel = abs_target[len(loc_root):].replace(os.sep, "/")
        return "docs/" + rel + frag
    return os.path.relpath(abs_target, ROOT).replace(os.sep, "/") + frag


def english_logical(page, target):
    if is_external(target):
        return target
    frag = "#" + target.split("#", 1)[1] if "#" in target else ""
    return os.path.relpath(resolve(english_path(page), target), ROOT).replace(os.sep, "/") + frag


# ---------------------------------------------------------------- finalize
def source_sha(page):
    """SHA-256 of the English page as the documentation gate computes it."""
    text = io.open(english_path(page), encoding="utf-8-sig").read().replace("\r\n", "\n")
    return hashlib.sha256(text.encode("utf-8")).hexdigest()


def recorded_sha(text):
    for line in text.split("\n"):
        if line.startswith(BANNER_MARK):
            m = SOURCE_SHA.search(line)
            return m.group(1) if m else None
    return None


def banner(locale, page, sha):
    cfg = locale_config(locale)
    source = os.path.relpath(english_path(page), os.path.dirname(localized_path(locale, page))).replace(os.sep, "/")
    return BANNER_MARK + " source=" + page + " source-sha256=" + sha + " -->\n> " + cfg["banner"].replace("{source}", source) + "\n"


def finalize(locale, pages, source_current=False):
    loc_file_set = {p for p in PAGES}
    for page in pages:
        path = localized_path(locale, page)
        if not os.path.exists(path):
            print("MISSING", locale, page)
            continue
        text = read(path).replace("\r\n", "\n")
        sha = source_sha(page) if source_current else (recorded_sha(text) or "unknown")
        # 1. remove previous banner and anchors (idempotence)
        lines = text.split("\n")
        while any(l.startswith(BANNER_MARK) for l in lines):
            i = next(i for i, l in enumerate(lines) if l.startswith(BANNER_MARK))
            j = i + 1
            while j < len(lines) and lines[j].startswith(">"):
                j += 1
            del lines[i:j]
        lines = [l for l in lines if not ANCHOR_LINE.match(l)]
        text = "\n".join(lines)
        # 2. rewrite links that leave the localized set
        english_text = read(english_path(page))
        out_lines = []
        for kind, block in split_blocks(text):
            if kind == "code":
                out_lines.extend(block)
                continue
            for line in block:
                def fix(m):
                    target = m.group(2)
                    if is_external(target):
                        return m.group(0)
                    frag = "#" + target.split("#", 1)[1] if "#" in target else ""
                    if not target.split("#", 1)[0]:
                        return m.group(0)
                    # The link as the translator copied it (English-relative) or as a
                    # previous run rewrote it: find the English file it stands for.
                    logical = logical_target(locale, page, target).split("#", 1)[0]
                    abs_en = os.path.join(ROOT, *logical.split("/"))
                    if not os.path.exists(abs_en):
                        abs_en = resolve(english_path(page), target)
                    if not os.path.exists(abs_en):
                        return m.group(0)
                    docs_rel = os.path.relpath(abs_en, DOCS).replace(os.sep, "/")
                    if docs_rel in loc_file_set:
                        dest = localized_path(locale, docs_rel)
                    else:
                        dest = abs_en
                    new = os.path.relpath(dest, os.path.dirname(path)).replace(os.sep, "/") + frag
                    return "[" + m.group(1) + "](" + new + (m.group(3) or "") + ")"
                out_lines.append(LINK.sub(fix, line))
        text = "\n".join(out_lines)
        # 3. anchors: English slug before each heading when heading sequences align
        en_heads = headings(english_text)
        loc_heads = headings(text)
        if [h[0] for h in en_heads] == [h[0] for h in loc_heads]:
            out, i = [], 0
            for kind, block in split_blocks(text):
                if kind == "code":
                    out.extend(block)
                    continue
                for line in block:
                    m = HEADING.match(line)
                    if m and i < len(en_heads):
                        en_slug = slug(en_heads[i][1])
                        if slug(m.group(2)) != en_slug and i > 0:
                            out.append('<a id="' + en_slug + '"></a>')
                        i += 1
                    out.append(line)
            text = "\n".join(out)
        else:
            print("WARN heading structure differs, anchors not inserted:", locale, page)
        # 4. banner after the H1 line
        lines = text.split("\n")
        idx = next((i for i, l in enumerate(lines) if HEADING.match(l)), 0)
        lines[idx + 1:idx + 1] = ["", banner(locale, page, sha).rstrip("\n")]
        text = "\n".join(lines).rstrip("\n") + "\n"
        text = re.sub(r"\n{3,}", "\n\n", text)
        write(path, text)
        print("finalized", locale, page)


# ---------------------------------------------------------------- check
def is_prose(line):
    """A line with real prose: at least four words of three or more letters once
    code spans, link targets, table pipes and ALL_CAPS tokens are removed."""
    s = strip_spans(line)
    s = LINK.sub(lambda m: m.group(1), s)
    s = re.sub(r"[A-Z0-9_]{2,}", " ", s)
    return len(re.findall(r"[^\W\d_]{3,}", s)) >= 4


def strip_l10n(text):
    """The page without the translation banner and the inserted anchors."""
    out, skip = [], False
    for line in text.split("\n"):
        if line.startswith(BANNER_MARK):
            skip = True
            continue
        if skip and line.startswith(">"):
            continue
        skip = False
        if ANCHOR_LINE.match(line):
            continue
        out.append(line)
    return "\n".join(out)


def check_page(locale, page):
    problems = []
    path = localized_path(locale, page)
    if not os.path.exists(path):
        return ["missing file"]
    loc = read(path).replace("\r\n", "\n")
    en = read(english_path(page)).replace("\r\n", "\n")
    if BANNER_MARK not in loc:
        problems.append("translation banner missing (run finalize)")
    elif recorded_sha(loc) != source_sha(page):
        problems.append("stale translation: the English page changed since it was translated (update it, then finalize with --source-current)")
    loc_body = strip_l10n(loc)
    eh, lh = headings(en), headings(loc_body)
    if [h[0] for h in eh] != [h[0] for h in lh]:
        problems.append("heading levels differ: en=%s loc=%s" % ("".join(str(h[0]) for h in eh), "".join(str(h[0]) for h in lh)))
    for (el, et), (ll, lt) in zip(eh, lh):
        if "`" in et and inline_spans(et) != inline_spans(lt):
            problems.append("heading code spans differ: %r vs %r" % (et, lt))
    ec, lc = code_blocks(en), code_blocks(loc_body)
    if len(ec) != len(lc):
        problems.append("code block count differs: en=%d loc=%d" % (len(ec), len(lc)))
    for i, (a, b) in enumerate(zip(ec, lc)):
        if a != b:
            problems.append("code block %d differs from English" % (i + 1))
    et, lt = table_shape(en), table_shape(loc_body)
    if et != lt:
        problems.append("table shape differs (rows/cells): en=%d rows loc=%d rows" % (len(et), len(lt)))
    lspans = set(inline_spans(loc_body))
    missing = [s for s in dict.fromkeys(inline_spans(en)) if s not in lspans]
    if missing:
        problems.append("inline code spans missing: " + ", ".join(repr(s) for s in missing[:12]) + (" ... (+%d)" % (len(missing) - 12) if len(missing) > 12 else ""))
    el_targets = sorted(english_logical(page, t) for t in links(en))
    ll_targets = sorted(logical_target(locale, page, t) for t in links(loc_body))
    if el_targets != ll_targets:
        a, b = set(el_targets), set(ll_targets)
        problems.append("link destinations differ: only-en=%s only-loc=%s" % (sorted(a - b)[:6], sorted(b - a)[:6]))
    for t in links(loc_body):
        target = resolve(path, t) if not is_external(t) else ""
        if target and t.split("#", 1)[0] and os.path.normpath(target).startswith(DOCS + os.sep) and not os.path.normpath(target).startswith(LOCALIZED + os.sep):
            if os.path.relpath(target, DOCS).replace(os.sep, "/") in PAGES:
                problems.append("link to a translated page points to the English page: " + t)
    for t in links(loc):
        if is_external(t):
            continue
        target = resolve(path, t)
        if not os.path.exists(target):
            pending = os.path.normpath(target).startswith(os.path.join(LOCALIZED, locale) + os.sep) and os.path.relpath(target, os.path.join(LOCALIZED, locale)).replace(os.sep, "/") in PAGES
            if not (pending and ALLOW_PENDING):
                problems.append("broken link: " + t)
        elif "#" in t and t.split("#", 1)[1]:
            frag = t.split("#", 1)[1]
            dest = read(target) if target.endswith(".md") else ""
            anchors = {slug(h) for _, h in headings(dest)} | set(re.findall(r'<a id="([^"]+)"></a>', dest))
            if dest and frag not in anchors:
                problems.append("broken anchor: " + t)
    return problems


def check(locales, pages):
    total = 0
    for locale in locales:
        locale_dir = os.path.join(LOCALIZED, locale)
        extra = []
        for dirpath, _, files in os.walk(locale_dir):
            for f in files:
                rel = os.path.relpath(os.path.join(dirpath, f), locale_dir).replace(os.sep, "/")
                if rel not in PAGES:
                    extra.append(rel)
        if extra:
            print("FAIL", locale, "unexpected files:", extra)
            total += len(extra)
        for page in pages:
            probs = check_page(locale, page)
            loc_path = localized_path(locale, page)
            if page not in GENERATED and locale != "en-GB" and os.path.exists(loc_path):
                en_lines = {l.strip() for l in prose_lines(read(english_path(page))) if is_prose(l)}
                loc_lines = [l.strip() for l in prose_lines(strip_l10n(read(loc_path))) if is_prose(l)]
                same = [l for l in loc_lines if l in en_lines]
                if loc_lines and len(same) / len(loc_lines) > 0.2:
                    probs.append("%d of %d prose lines are identical to English (untranslated?)" % (len(same), len(loc_lines)))
            for p in probs:
                print("FAIL", locale, page, "-", p)
            total += len(probs)
    print("problems=%d" % total)
    return total


# ---------------------------------------------------------------- leak
# Distinctive English function words only; words that also exist in the target
# languages (as, an, on, was, will, is, of, be ...) would give false positives.
EN_WORDS = set("the and that this with when which its only every must does has have were should would".split())


def prose_for_leak(text):
    for line in prose_lines(strip_l10n(text)):
        s = strip_spans(line)
        s = LINK.sub(lambda m: m.group(1), s)
        s = re.sub(r"https?://\S+", "", s)
        yield line, s


def leak(locales):
    findings = 0
    for locale in locales:
        cfg = locale_config(locale)
        for page in PAGES:
            path = localized_path(locale, page)
            if not os.path.exists(path) or page in GENERATED:
                continue
            for n, (raw, s) in enumerate(prose_for_leak(read(path)), 1):
                words = re.findall(r"[A-Za-z']+", s)
                if locale not in ("en-GB",) and len(words) >= 8:
                    ratio = sum(1 for w in words if w.lower() in EN_WORDS) / len(words)
                    if ratio > 0.15:
                        print("EN?", locale, page, ":", s.strip()[:140])
                        findings += 1
                for marker in cfg.get("forbidden", []):
                    if re.search(marker, s, re.I if not re.search(r"[㐀-鿿]", marker) else 0):
                        print("XLOC", locale, page, "marker", marker, ":", s.strip()[:120])
                        findings += 1
    print("findings=%d" % findings)
    return findings


# ---------------------------------------------------------------- inventory
def inventory(locales):
    en = read(english_path("reference/public-api-inventory.md"))
    for locale in locales:
        ph = locale_config(locale)["inventory"]
        text = en
        text = text.replace("# Public API Inventory", "# " + ph["title"], 1)
        intro = text.split("\n")[2]
        text = text.replace(intro, ph["intro"], 1)
        text = text.replace("| Member | Signature |", "| " + ph["member"] + " | " + ph["signature"] + " |")
        for k in ["Sealed record", "Sealed class", "Static class", "Record struct", "Interface", "Enum"]:
            text = re.sub(r"^" + re.escape(k) + r"( \(`[a-z]+`\))? in (`[^`]+`)\. Stability: (`[A-Z_]+`)\. Semantics: ",
                          lambda m, k=k: ph["type_line"].replace("{kind}", ph["kinds"][k] + (m.group(1) or "")).replace("{ns}", m.group(2)).replace("{stability}", m.group(3)) + " ",
                          text, flags=re.M)
        for k, v in ph["members"].items():
            text = text.replace("| " + k + " | `", "| " + v + " | `")
        write(localized_path(locale, "reference/public-api-inventory.md"), text)
        # Generated from the current English inventory, so it is current by construction.
        finalize(locale, ["reference/public-api-inventory.md"], source_current=True)
        print("inventory", locale)


def main(argv):
    try:
        sys.stdout.reconfigure(encoding="utf-8")
    except (AttributeError, ValueError):
        pass
    if len(argv) < 2:
        print(__doc__)
        return 2
    global ALLOW_PENDING
    ALLOW_PENDING = "--allow-pending" in argv
    source_current = "--source-current" in argv
    argv = [a for a in argv if a not in ("--allow-pending", "--source-current")]
    cmd, target = argv[0], argv[1]
    locales = LOCALES if target == "all" else [target]
    pages = argv[2:] or PAGES
    if cmd == "finalize":
        for l in locales:
            finalize(l, pages, source_current)
        return 0
    if cmd == "check":
        return 1 if check(locales, pages) else 0
    if cmd == "leak":
        leak(locales)
        return 0
    if cmd == "inventory":
        inventory(locales)
        return 0
    print(__doc__)
    return 2


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
