using System.Text;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.NativeResolution;

// Derives the guards and signatures of the Omsi23004 native target catalog
// from the reference executable and verifies them.
//
//   OmsiLaunch.SignatureTool <reference Omsi.exe> [--write <generated .cs>]
//
// Operands that depend on the layout are masked: every dword listed in the
// executable's base-relocation table (absolute addresses) and every rel32 of a
// call/jmp/jcc whose destination lies outside the pattern window. Windows grow
// until the pattern matches exactly once. Verification resolves the catalog
// twice: with the known code identity (fast path, every guard checked) and with
// the identity withheld, which forces the build-independent strategies (RTTI,
// signatures, VMT slot values) and must reproduce every reference value.
// Exit code 1 when any non-layout target fails either resolution.
if (args.Length < 1) { Console.Error.WriteLine("usage: OmsiLaunch.SignatureTool <Omsi.exe> [--write <file>]"); return 2; }
var image = NativeImage.FromFile(File.ReadAllBytes(args[0]));
var write = args.Length >= 3 && args[1] == "--write" ? args[2] : null;
Console.WriteLine($"image base=0x{image.ImageBase:X8} size=0x{image.SizeOfImage:X} code sha256={image.CodeSha256} relocations={image.Relocations.Count}");
if (!NativeTargetCatalog.KnownCodeSha256.Contains(image.CodeSha256)) { Console.Error.WriteLine("The executable is not the reference build of this catalog."); return 1; }

var relocations = new SortedSet<uint>(image.Relocations);
// Targets without a semantically sound build-independent derivation: they are
// resolved only on a known code identity (reported, not a failure).
var KnownIdentityOnly = new HashSet<string>(StringComparer.Ordinal) { "field.TProgMan.CriticalSection", "field.RawEntrypoint.Stride", "field.RawEntrypoint.Name", "field.RoadVehicleList.Items", "field.MemArrayList.Data", "field.MemArrayList.Count", "field.TWeather.CloudType", "field.TWeather.CloudTypeNum", "field.TActuWeather.Icao", "field.TActuWeather.Active", "field.TActuWeather.LastDownloaded", "field.TActuWeather.InvalidIcao", "field.TActuWeather.Process", "field.Tform_menu2.FirstKeySwallow", "field.TCustomForm.Active", "field.TWinControl.Handle", "field.TForm_main.InIdleFrame", "field.Tform_menu3.Mode" };
var functions = NativeTargetCatalog.Declarations.Where(d => d.Kind == NativeTargetKind.Function).ToArray();
var derived = new SortedDictionary<string, NativeTargetCatalog.DerivedTarget>(StringComparer.Ordinal);
var notes = new List<string>();

foreach (var spec in NativeTargetCatalog.Declarations)
{
    switch (spec.Kind)
    {
        case NativeTargetKind.Function:
        {
            var guard = Pattern(spec.KnownValue, 8, 0);
            var signature = Grow(spec.KnownValue, 0, 16, 192, before: 0);
            derived[spec.Id] = new(guard, signature is null ? Array.Empty<NativeSignature>() : new[] { signature });
            if (signature is null) notes.Add(spec.Id + ": no unique entry signature");
            break;
        }
        case NativeTargetKind.CallSite:
        {
            var length = spec.Id == "site.NewSituation.AskForPosition" ? 22 : 5;
            var guard = Pattern(spec.KnownValue, length, 0, keepCall: spec.CallTarget is null);
            NativeSignature? signature = null;
            for (var before = 8; before <= 96 && signature is null; before += 8) signature = Grow(spec.KnownValue - (uint)before, before, before + length, before + length + 64, before);
            derived[spec.Id] = new(guard, signature is null ? Array.Empty<NativeSignature>() : new[] { signature });
            if (signature is null) notes.Add(spec.Id + ": no unique site signature");
            break;
        }
        case NativeTargetKind.Global:
        {
            var signature = GlobalSignature(spec.KnownValue);
            derived[spec.Id] = new(null, signature is null ? Array.Empty<NativeSignature>() : new[] { signature });
            if (signature is null) notes.Add(spec.Id + ": no unique referencing signature");
            break;
        }
        case NativeTargetKind.FieldOffset:
        {
            var signature = FieldSignature(spec);
            derived[spec.Id] = new(null, signature is null ? Array.Empty<NativeSignature>() : new[] { signature });
            if (signature is null && spec.RttiName is null && spec.RttiProperty is null) notes.Add(spec.Id + ": no class-scoped field-access signature (known identity only)");
            break;
        }
    }
}

var pack = NativeTargetCatalog.Pack with
{
    Targets = NativeTargetCatalog.Declarations.Select(d => derived.TryGetValue(d.Id, out var x) ? d with { Guard = x.Guard, Signatures = x.Signatures } : d).ToArray()
};
var failed = Verify(pack, "known identity", withIdentity: true) | Verify(pack, "identity withheld", withIdentity: false);
foreach (var note in notes) Console.WriteLine("note: " + note);
if (write is not null) { File.WriteAllText(write, Generate(derived), new UTF8Encoding(false)); Console.WriteLine("written " + write); }
return failed ? 1 : 0;

bool Verify(NativeLayoutPack candidate, string label, bool withIdentity)
{
    var resolution = NativeResolver.Resolve(image, withIdentity ? candidate : candidate with { KnownCodeSha256 = Array.Empty<string>() });
    var bad = false; var counts = new SortedDictionary<string, int>();
    foreach (var spec in candidate.Targets)
    {
        var result = resolution.Targets[spec.Id];
        counts[result.Method.ToString()] = counts.GetValueOrDefault(result.Method.ToString()) + 1;
        if (spec.Kind == NativeTargetKind.Layout && !withIdentity) continue;
        if (!withIdentity && KnownIdentityOnly.Contains(spec.Id)) { if (result.Verified) { bad = true; Console.WriteLine($"  FAIL [{label}] {spec.Id}: resolved without identity although declared known-identity only"); } continue; }
        var expected = spec.Kind == NativeTargetKind.Layout ? 0 : spec.KnownValue;
        if (!result.Verified || result.Value != expected)
        {
            bad = true;
            Console.WriteLine($"  FAIL [{label}] {spec.Id}: verified={result.Verified} value=0x{result.Value:X8} expected=0x{expected:X8} {result.Failure}");
        }
    }
    var unavailable = resolution.Capabilities.Values.Where(c => !c.Available).Select(c => c.Id).ToArray();
    Console.WriteLine($"{(bad ? "FAIL" : "PASS")} [{label}] methods: {string.Join(", ", counts.Select(x => x.Key + "=" + x.Value))}; unavailable capabilities: {(unavailable.Length == 0 ? "none" : string.Join(", ", unavailable))}");
    return bad;
}

// A masked pattern of `length` bytes at `start`. Relocated dwords and rel32
// operands leaving the window are wildcards; keepCall keeps a call's rel32
// when the call destination itself is the assumption being guarded.
string Pattern(uint start, int length, int protectStart, bool keepCall = false, int maskAt = -1, int maskLength = 0)
{
    var bytes = image.Span(start, length).ToArray();
    var mask = Enumerable.Repeat(true, length).ToArray();
    foreach (var r in relocations.GetViewBetween(start > 3 ? start - 3 : 0, start + (uint)length))
        for (var k = 0; k < 4; k++) { var i = (long)r + k - start; if (i >= 0 && i < length) mask[i] = false; }
    for (var i = 0; i < length; i++)
    {
        int operand; long destination;
        if ((bytes[i] == 0xE8 || bytes[i] == 0xE9) && i + 5 > length) { for (var k = i + 1; k < length; k++) mask[k] = false; continue; }
        if (bytes[i] == 0x0F && i + 1 < length && bytes[i + 1] is >= 0x80 and <= 0x8F && i + 6 > length) { for (var k = i + 2; k < length; k++) mask[k] = false; continue; }
        if ((bytes[i] == 0xE8 || bytes[i] == 0xE9) && i + 5 <= length) { operand = i + 1; destination = start + i + 5 + BitConverter.ToInt32(bytes, i + 1); }
        else if (bytes[i] == 0x0F && i + 6 <= length && bytes[i + 1] is >= 0x80 and <= 0x8F) { operand = i + 2; destination = start + i + 6 + BitConverter.ToInt32(bytes, i + 2); }
        else continue;
        if (keepCall && i == 0) continue;
        if (destination < start || destination >= start + length) for (var k = 0; k < 4; k++) mask[operand + k] = false;
    }
    for (var k = 0; k < maskLength; k++) if (maskAt + k >= 0 && maskAt + k < length) mask[maskAt + k] = false;
    return string.Join(' ', bytes.Select((b, i) => mask[i] ? b.ToString("X2") : "??"));
}

// Grows a window starting at `start` until its masked pattern is unique.
NativeSignature? Grow(uint start, int minimumFixedOffset, int from, int to, int before, CaptureKind capture = CaptureKind.Match, int maskAt = -1, int maskLength = 0, string? within = null, uint windowStart = 0, int windowLength = 0)
{
    for (var length = from; length <= to; length += 8)
    {
        if (!image.IsExecutable(start) || !image.IsExecutable(start + (uint)length - 1)) return null;
        var pattern = Pattern(start, length, minimumFixedOffset, maskAt: maskAt, maskLength: maskLength);
        if (pattern.Split(' ').Count(x => x != "??") < 8) continue;
        var matches = NativeResolver.Find(image, pattern, windowStart, windowLength);
        if (matches.Count == 1 && matches[0] == start) return new NativeSignature(pattern, before, capture, within, within is null ? 0 : windowLength);
    }
    return null;
}

// A unique code site that references the global through an absolute operand,
// either directly or through a pointer cell in a data section (Indirect32).
NativeSignature? GlobalSignature(uint global)
{
    var cells = relocations.Where(r => !image.IsExecutable(r) && image.TryReadUInt32(r, out var v) && v == global).ToArray();
    var sites = relocations.Where(r => image.IsExecutable(r) && image.TryReadUInt32(r, out var v) && v == global).Select(r => (Site: r, Capture: CaptureKind.Absolute32))
        .Concat(relocations.Where(r => image.IsExecutable(r) && image.TryReadUInt32(r, out var v) && cells.Contains(v)).Select(r => (Site: r, Capture: CaptureKind.Indirect32)));
    NativeSignature? best = null; var bestLength = int.MaxValue; var tried = 0;
    foreach (var (site, capture) in sites)
    {
        if (++tried > 64) break;
        for (var before = 4; before <= 40; before += 4)
        {
            var start = site - (uint)before;
            var signature = Grow(start, 0, before + 12, before + 48, before, capture, maskAt: before, maskLength: 4);
            if (signature is null) continue;
            var length = signature.Pattern.Split(' ').Length;
            if (length < bestLength) { best = signature; bestLength = length; }
            break;
        }
    }
    return best;
}

// A unique [reg+disp] access to the field, searched only inside methods of the
// field's owner class, so the captured displacement means this field and not
// another class's field that happens to share the offset.
NativeSignature? FieldSignature(NativeTargetSpec spec)
{
    const int window = 0x1800;
    var offset = spec.KnownValue;
    var wide = offset >= 0x80;
    var width = wide ? 4 : 1;
    var capture = wide ? CaptureKind.Displacement32 : CaptureKind.Displacement8;
    string[] owners = spec.Id switch
    {
        var id when id.StartsWith("field.Tform_start.") => new[] { "start." },
        var id when id.StartsWith("field.Tform_setpos.") => new[] { "setpos." },
        "field.TCustomForm.ModalResult" or "field.TCustomForm.Visible" => new[] { "vcl.TCustomForm." },
        "field.TProgMan.CriticalSection" => new[] { "progman." },
        var id when id.StartsWith("field.RawEntrypoint.") || id.StartsWith("field.RoadVehicleList.") || id.StartsWith("field.MemArrayList.") => Array.Empty<string>(),
        _ => Array.Empty<string>()
    };
    var virtualCall = false;
    var listBox = NativeTargetCatalog.Declarations.First(d => d.Id == "field.Tform_start.SituationControl").KnownValue;
    foreach (var function in functions.Where(f => owners.Any(o => f.Id.StartsWith(o, StringComparison.Ordinal))))
    {
        var body = image.Span(function.KnownValue, window);
        for (var i = 2; i + width <= body.Length; i++)
        {
            var value = wide ? BitConverter.ToUInt32(body.Slice(i, 4)) : body[i];
            if (value != offset) continue;
            var modrm = body[i - 1];
            if ((modrm & 0xC0) != (wide ? 0x80 : 0x40) || (modrm & 7) == 4) continue; // [reg+disp], no SIB
            // The ModRM must follow a memory-operand opcode; otherwise the
            // bytes may be data embedded in code (strings).
            if (!IsMemoryOpcode(body, i - 2)) continue;
            if (virtualCall)
            {
                // call [reg+disp32] preceded, within 16 bytes, by a load of the list box field.
                if (body[i - 2] != 0xFF || ((modrm >> 3) & 7) != 2) continue;
                var context = body.Slice(Math.Max(0, i - 18), Math.Min(16, i - 2));
                var found = false;
                for (var k = 0; k + 4 <= context.Length && !found; k++) found = BitConverter.ToUInt32(context.Slice(k, 4)) == listBox;
                if (!found) continue;
            }
            for (var before = 4; before <= 24; before += 4)
            {
                var start = function.KnownValue + (uint)i - (uint)before;
                var signature = Grow(start, 0, before + 8, before + 40, before, capture, maskAt: before, maskLength: width, within: function.Id, windowStart: function.KnownValue, windowLength: window);
                if (signature is not null) return signature;
            }
        }
    }
    return null;
}

// mov/movzx/movsx/lea/cmp/test/arith with a ModRM memory operand.
static bool IsMemoryOpcode(ReadOnlySpan<byte> body, int at)
{
    if (at < 0) return false;
    var op = body[at];
    if (op is 0x88 or 0x89 or 0x8A or 0x8B or 0x8D or 0x80 or 0x81 or 0x83 or 0xC6 or 0xC7 or 0x38 or 0x39 or 0x3A or 0x3B or 0x84 or 0x85 or 0xF6 or 0xF7 or 0x01 or 0x03 or 0x29 or 0x2B or 0xFF or 0xD9 or 0xDD) return true;
    return at >= 1 && body[at - 1] == 0x0F && op is 0xB6 or 0xB7 or 0xBE or 0xBF;
}

static string Generate(IDictionary<string, NativeTargetCatalog.DerivedTarget> targets)
{
    var s = new StringBuilder();
    s.AppendLine("// <auto-generated>");
    s.AppendLine("// Generated by tools\\OmsiLaunch.SignatureTool from the reference executable");
    s.AppendLine("// (code SHA-256 " + NativeTargetCatalog.KnownCodeSha256[0] + "). Do not edit.");
    s.AppendLine("// </auto-generated>");
    s.AppendLine("using OmsiLaunch.NativeResolution;");
    s.AppendLine();
    s.AppendLine("namespace OmsiLaunch.Builds.Omsi23004;");
    s.AppendLine();
    s.AppendLine("public static partial class NativeTargetCatalog");
    s.AppendLine("{");
    s.AppendLine("    private static readonly IReadOnlyDictionary<string, DerivedTarget> Derived = new Dictionary<string, DerivedTarget>(StringComparer.Ordinal)");
    s.AppendLine("    {");
    foreach (var (id, target) in targets)
    {
        var guard = target.Guard is null ? "null" : "\"" + target.Guard + "\"";
        var signatures = string.Join(", ", target.Signatures.Select(x => $"new(\"{x.Pattern}\", {x.Offset}, CaptureKind.{x.Capture}{(x.Within is null ? "" : $", \"{x.Within}\", 0x{x.WithinLength:X}")})"));
        s.AppendLine($"        [\"{id}\"] = new({guard}, new NativeSignature[] {{ {signatures} }}),");
    }
    s.AppendLine("    };");
    s.AppendLine("}");
    return s.ToString();
}
