using System.Buffers.Binary;

namespace OmsiLaunch.NativeResolution;

// Resolves and verifies every native target of a layout pack against one
// image, independently per target. The code identity only selects a fast path
// (the reference address); it never bypasses a guard, and a target that no
// strategy can resolve and verify stays unresolved: there is no fallback to a
// fixed address. A capability is available only when all its targets are.
public static class NativeResolver
{
    // Delphi (2009+) 32-bit VMT layout, negative offsets from the VMT pointer.
    public const uint VmtSelfPtr = 0x58, VmtFieldTable = 0x44, VmtMethodTable = 0x40, VmtClassName = 0x38;
    private const uint MaximumVmtScan = 0x800;

    public static NativeResolution Resolve(NativeImage image, NativeLayoutPack pack)
    {
        var context = new Context(image, pack);
        // Targets may depend on other targets (VMT slots, call targets,
        // signatures searched inside a function). Resolve to a fixpoint.
        var pending = pack.Targets.ToList();
        while (pending.Count > 0)
        {
            var progress = false;
            foreach (var spec in pending.ToArray())
            {
                if (!context.DependenciesReady(spec)) continue;
                context.Results[spec.Id] = context.ResolveOne(spec);
                pending.Remove(spec); progress = true;
            }
            if (progress) continue;
            foreach (var spec in pending) context.Results[spec.Id] = new(spec.Id, spec.Kind, 0, ResolutionMethod.Unresolved, false, "dependency unresolved: " + string.Join(", ", context.Dependencies(spec).Where(d => !context.Results.ContainsKey(d) || !context.Results[d].Verified)));
            break;
        }
        var capabilities = pack.Capabilities.ToDictionary(c => c.Id, c =>
        {
            var missing = c.Targets.Where(t => !context.Results.TryGetValue(t, out var r) || !r.Verified).ToArray();
            return new CapabilityState(c.Id, missing.Length == 0, missing);
        }, StringComparer.Ordinal);
        return new NativeResolution(image.CodeSha256, context.KnownIdentity, image.ImageBase, image.SizeOfImage, context.Results, capabilities);
    }

    // Finds the VMTs whose RTTI class name equals className (vmtSelfPtr checked).
    public static IReadOnlyList<uint> FindVmts(NativeImage image, string className)
    {
        var name = new byte[className.Length + 1];
        name[0] = (byte)className.Length;
        System.Text.Encoding.Latin1.GetBytes(className, 0, className.Length, name, 1);
        var output = new List<uint>();
        var bytes = image.Bytes.AsSpan();
        Span<byte> pointer = stackalloc byte[4];
        for (var at = bytes.IndexOf(name); at >= 0;)
        {
            var nameVa = image.ImageBase + (uint)at;
            BinaryPrimitives.WriteUInt32LittleEndian(pointer, nameVa);
            for (var p = bytes.IndexOf(pointer); p >= 0;)
            {
                var vmt = image.ImageBase + (uint)p + VmtClassName;
                if ((p & 3) == 0 && image.TryReadUInt32(vmt - VmtSelfPtr, out var self) && self == vmt && !output.Contains(vmt)) output.Add(vmt);
                var next = bytes[(p + 1)..].IndexOf(pointer); p = next < 0 ? -1 : p + 1 + next;
            }
            var again = bytes[(at + 1)..].IndexOf(name); at = again < 0 ? -1 : at + 1 + again;
        }
        return output;
    }

    // Published methods (name, address) of one class, from its RTTI method table.
    public static IReadOnlyList<(string Name, uint Address)> PublishedMethods(NativeImage image, uint vmt)
    {
        var output = new List<(string, uint)>();
        if (!image.TryReadUInt32(vmt - VmtMethodTable, out var table) || table == 0 || !image.TryReadUInt16(table, out var count)) return output;
        var entry = table + 2;
        for (var i = 0; i < count; i++)
        {
            if (!image.TryReadUInt16(entry, out var size) || size < 7 || !image.TryReadUInt32(entry + 2, out var address) || !image.TryReadShortString(entry + 6, out var name)) break;
            output.Add((name, address)); entry += size;
        }
        return output;
    }

    // Published properties (declaring class, name, GetProc, SetProc) of one
    // class and its ancestors, from the class type information. A GetProc or
    // SetProc of $FF00xxxx is a field offset, $FE00xxxx a virtual method slot
    // offset, anything else a static method address.
    public static IReadOnlyList<(string Owner, string Name, uint Get, uint Set)> PublishedProperties(NativeImage image, uint vmt)
    {
        var output = new List<(string, string, uint, uint)>();
        if (!image.TryReadUInt32(vmt - 0x48, out var typeInfo)) return output;
        for (var depth = 0; typeInfo != 0 && depth < 32; depth++)
        {
            if (!image.TryReadShortString(typeInfo + 1, out var owner)) break;
            var typeData = typeInfo + 2 + (uint)owner.Length;
            if (!image.TryReadUInt32(typeData + 4, out var parent) || !image.TryReadShortString(typeData + 10, out var unit) || !image.TryReadUInt16(typeData + 11 + (uint)unit.Length, out var count)) break;
            var entry = typeData + 13 + (uint)unit.Length;
            for (var i = 0; i < count; i++)
            {
                if (!image.TryReadUInt32(entry + 4, out var get) || !image.TryReadUInt32(entry + 8, out var set) || !image.TryReadShortString(entry + 26, out var name)) break;
                output.Add((owner, name, get, set)); entry += 27u + (uint)name.Length;
            }
            if (parent == 0 || !image.TryReadUInt32(parent, out typeInfo)) break;
        }
        return output;
    }

    // Published fields (name, offset) of one class, from its RTTI field table.
    public static IReadOnlyList<(string Name, uint Offset)> PublishedFields(NativeImage image, uint vmt)
    {
        var output = new List<(string, uint)>();
        if (!image.TryReadUInt32(vmt - VmtFieldTable, out var table) || table == 0 || !image.TryReadUInt16(table, out var count)) return output;
        var entry = table + 6;
        for (var i = 0; i < count; i++)
        {
            if (!image.TryReadUInt32(entry, out var offset) || !image.TryReadShortString(entry + 6, out var name)) break;
            output.Add((name, offset)); entry += 7u + (uint)name.Length;
        }
        return output;
    }

    // Every address in the executable sections (or inside one window) where
    // the pattern matches; stops after `limit` matches.
    public static IReadOnlyList<uint> Find(NativeImage image, string pattern, uint windowStart = 0, int windowLength = 0, int limit = 2)
    {
        var masked = new MaskedPattern(pattern);
        var output = new List<uint>();
        var ranges = windowLength > 0
            ? new[] { (windowStart, windowLength) }
            : image.Sections.Where(s => s.Executable).Select(s => (image.ImageBase + s.VirtualAddress, (int)s.VirtualSize)).ToArray();
        // Anchor on the longest run of fixed bytes, then verify the whole mask.
        var (anchorStart, anchorLength) = LongestFixedRun(masked);
        var anchor = masked.Bytes.AsSpan(anchorStart, anchorLength).ToArray();
        foreach (var (start, length) in ranges)
        {
            var data = image.Span(start, length);
            for (var at = data.IndexOf(anchor); at >= 0 && output.Count < limit;)
            {
                var candidate = at - anchorStart;
                if (candidate >= 0 && masked.Matches(data[candidate..])) output.Add(start + (uint)candidate);
                var next = data[(at + 1)..].IndexOf(anchor); at = next < 0 ? -1 : at + 1 + next;
            }
        }
        return output;
    }

    private static (int Start, int Length) LongestFixedRun(MaskedPattern pattern)
    {
        int bestStart = 0, bestLength = 0;
        for (var i = 0; i < pattern.Length;)
        {
            if (!pattern.Mask[i]) { i++; continue; }
            var j = i; while (j < pattern.Length && pattern.Mask[j]) j++;
            if (j - i > bestLength) { bestStart = i; bestLength = j - i; }
            i = j;
        }
        return (bestStart, bestLength);
    }

    private sealed class Context
    {
        private readonly NativeImage image; private readonly NativeLayoutPack pack; private readonly long delta;
        private readonly Dictionary<string, IReadOnlyList<uint>> vmtCache = new(StringComparer.Ordinal);
        public Context(NativeImage image, NativeLayoutPack pack)
        {
            this.image = image; this.pack = pack;
            KnownIdentity = pack.KnownCodeSha256.Contains(image.CodeSha256, StringComparer.OrdinalIgnoreCase);
            delta = (long)image.ImageBase - pack.PreferredBase;
        }
        public bool KnownIdentity { get; }
        public Dictionary<string, ResolvedTarget> Results { get; } = new(StringComparer.Ordinal);

        public IEnumerable<string> Dependencies(NativeTargetSpec spec)
        {
            if (spec.SlotOf is not null) yield return spec.SlotOf;
            if (spec.SlotValue is not null) yield return spec.SlotValue;
            if (spec.CallTarget is not null) yield return spec.CallTarget;
            foreach (var signature in spec.Signatures ?? Array.Empty<NativeSignature>()) if (signature.Within is not null) yield return signature.Within;
        }
        public bool DependenciesReady(NativeTargetSpec spec) => Dependencies(spec).All(Results.ContainsKey);

        public ResolvedTarget ResolveOne(NativeTargetSpec spec)
        {
            var failures = new List<string>();
            foreach (var (value, method) in Candidates(spec, failures))
            {
                var guard = Guard(spec, value);
                if (guard is null) return new(spec.Id, spec.Kind, value, method, true, null);
                failures.Add(method + " 0x" + value.ToString("X8") + ": " + guard);
            }
            return new(spec.Id, spec.Kind, 0, ResolutionMethod.Unresolved, false, failures.Count == 0 ? "no resolution strategy" : string.Join("; ", failures));
        }

        private IEnumerable<(uint, ResolutionMethod)> Candidates(NativeTargetSpec spec, List<string> failures)
        {
            if (KnownIdentity)
                yield return (spec.Kind is NativeTargetKind.FieldOffset or NativeTargetKind.Layout ? spec.KnownValue : (uint)(spec.KnownValue + delta), ResolutionMethod.KnownLayout);
            else if (spec.Kind == NativeTargetKind.Layout)
                failures.Add("object layout is validated only for a known code identity");
            if (spec.Kind == NativeTargetKind.Vmt && spec.ClassName is not null)
            {
                var vmts = Vmts(spec.ClassName);
                if (vmts.Count == 1) yield return (vmts[0], ResolutionMethod.RttiClass);
                else failures.Add("RTTI class " + spec.ClassName + ": " + vmts.Count + " candidates");
            }
            if (spec.RttiName is not null && spec.ClassName is not null && spec.Kind is NativeTargetKind.Function or NativeTargetKind.FieldOffset)
            {
                var vmts = Vmts(spec.ClassName);
                if (vmts.Count != 1) failures.Add("RTTI class " + spec.ClassName + ": " + vmts.Count + " candidates");
                else if (spec.Kind == NativeTargetKind.Function)
                {
                    var methods = PublishedMethods(image, vmts[0]).Where(m => m.Name == spec.RttiName).ToArray();
                    if (methods.Length == 1) yield return (methods[0].Address, ResolutionMethod.RttiMethod); else failures.Add("RTTI method " + spec.RttiName + ": " + methods.Length + " candidates");
                }
                else
                {
                    var fields = PublishedFields(image, vmts[0]).Where(f => f.Name == spec.RttiName).ToArray();
                    if (fields.Length == 1) yield return (fields[0].Offset, ResolutionMethod.RttiField); else failures.Add("RTTI field " + spec.RttiName + ": " + fields.Length + " candidates");
                }
            }
            if (spec.RttiProperty is not null && spec.ClassName is not null)
            {
                var vmts = Vmts(spec.ClassName);
                var parts = spec.RttiProperty.Split('.');
                var property = vmts.Count == 1 ? PublishedProperties(image, vmts[0]).Where(p => p.Name == parts[0]).ToArray() : Array.Empty<(string, string, uint, uint)>();
                uint? resolved = null;
                if (property.Length >= 1 && parts.Length == 2)
                {
                    var accessor = parts[1] == "get" ? property[0].Item3 : property[0].Item4;
                    if (spec.Kind == NativeTargetKind.FieldOffset && (accessor & 0xFF000000) == 0xFF000000) resolved = accessor & 0x00FFFFFF;
                    else if (spec.Kind == NativeTargetKind.Function && (accessor & 0xFF000000) == 0xFE000000 && image.TryReadUInt32(vmts[0] + (accessor & 0xFFFF), out var virtualTarget)) resolved = virtualTarget;
                    else if (spec.Kind == NativeTargetKind.Function && (accessor & 0xFF000000) is not (0xFF000000 or 0xFE000000)) resolved = accessor;
                }
                if (resolved is { } value) yield return (value, ResolutionMethod.RttiProperty);
                else failures.Add("RTTI property " + spec.ClassName + "." + spec.RttiProperty + " unresolved");
            }
            if (spec.Kind == NativeTargetKind.VmtSlot && spec.SlotOf is not null && spec.SlotValue is not null)
            {
                if (Verified(spec.SlotOf, out var vmt) && Verified(spec.SlotValue, out var method))
                {
                    var slots = new List<uint>();
                    for (var slot = vmt; slot < vmt + MaximumVmtScan; slot += 4)
                        if (image.TryReadUInt32(slot, out var held) && held == method) slots.Add(slot);
                    if (slots.Count == 1) yield return (slots[0], ResolutionMethod.VmtSlotValue); else failures.Add("VMT slot holding " + spec.SlotValue + ": " + slots.Count + " candidates");
                }
                else failures.Add("VMT slot dependencies unresolved");
            }
            foreach (var signature in spec.Signatures ?? Array.Empty<NativeSignature>())
            {
                uint windowStart = 0;
                if (signature.Within is not null && !Verified(signature.Within, out windowStart)) { failures.Add("signature window " + signature.Within + " unresolved"); continue; }
                var matches = Find(image, signature.Pattern, windowStart, signature.Within is null ? 0 : signature.WithinLength);
                if (matches.Count != 1) { failures.Add("signature: " + (matches.Count == 0 ? "no match" : "ambiguous")); continue; }
                if (Capture(matches[0], signature) is { } captured) yield return (captured, ResolutionMethod.Signature);
                else failures.Add("signature capture unreadable");
            }
        }

        private uint? Capture(uint match, NativeSignature signature)
        {
            var at = (uint)(match + signature.Offset);
            switch (signature.Capture)
            {
                case CaptureKind.Match: return at;
                case CaptureKind.Absolute32: return image.TryReadUInt32(at, out var absolute) ? absolute : null;
                case CaptureKind.Displacement32: return image.TryReadUInt32(at, out var displacement) ? displacement : null;
                case CaptureKind.Displacement8: return image.Contains(at) ? image.Bytes[at - image.ImageBase] : null;
                case CaptureKind.Relative32: return image.TryReadUInt32(at, out var relative) ? unchecked(at + 4 + relative) : null;
                case CaptureKind.Indirect32: return image.TryReadUInt32(at, out var cell) && !image.IsExecutable(cell) && image.TryReadUInt32(cell, out var target) ? target : null;
                default: return null;
            }
        }

        // null when the value passes the target's guard, otherwise the reason.
        private string? Guard(NativeTargetSpec spec, uint value)
        {
            switch (spec.Kind)
            {
                case NativeTargetKind.Function:
                case NativeTargetKind.CallSite:
                    if (!image.IsExecutable(value)) return "not in an executable section";
                    if (spec.Guard is not null && !new MaskedPattern(spec.Guard).Matches(image.Span(value, new MaskedPattern(spec.Guard).Length))) return "guard bytes differ";
                    if (spec.CallTarget is not null)
                    {
                        if (!Verified(spec.CallTarget, out var callee)) return "call target unresolved";
                        if (image.Span(value, 1).Length != 1 || image.Span(value, 1)[0] != 0xE8 || !image.TryReadUInt32(value + 1, out var relative) || unchecked(value + 5 + relative) != callee) return "call does not reach " + spec.CallTarget;
                    }
                    return null;
                case NativeTargetKind.Global:
                    return image.IsWritableData(value) ? null : "not in a writable data section";
                case NativeTargetKind.Vmt:
                    if (!image.TryReadUInt32(value - VmtSelfPtr, out var self) || self != value) return "vmtSelfPtr mismatch";
                    if (spec.ClassName is not null && (!image.TryReadUInt32(value - VmtClassName, out var namePointer) || !image.TryReadShortString(namePointer, out var name) || name != spec.ClassName)) return "RTTI class name differs";
                    return null;
                case NativeTargetKind.VmtSlot:
                    if (spec.SlotOf is not null && (!Verified(spec.SlotOf, out var vmt) || value < vmt || value >= vmt + MaximumVmtScan)) return "slot outside its VMT";
                    if (spec.SlotValue is not null && (!Verified(spec.SlotValue, out var method) || !image.TryReadUInt32(value, out var held) || held != method)) return "slot does not hold " + spec.SlotValue;
                    return null;
                case NativeTargetKind.FieldOffset:
                    return value is > 0 and < 0x10000 ? null : "implausible field offset";
                case NativeTargetKind.Layout:
                    return KnownIdentity ? null : "object layout is validated only for a known code identity";
                default:
                    return "unknown target kind";
            }
        }

        private bool Verified(string id, out uint value)
        {
            value = 0;
            if (!Results.TryGetValue(id, out var result) || !result.Verified) return false;
            value = result.Value; return true;
        }
        private IReadOnlyList<uint> Vmts(string className) => vmtCache.TryGetValue(className, out var cached) ? cached : vmtCache[className] = FindVmts(image, className);
    }
}
