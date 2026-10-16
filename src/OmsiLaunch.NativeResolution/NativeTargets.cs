namespace OmsiLaunch.NativeResolution;

public enum NativeTargetKind : byte
{
    // Entry point of a function; the guard checks its (masked) first bytes.
    Function,
    // An instruction inside a function (a call or a patch site).
    CallSite,
    // A data slot in a writable section.
    Global,
    // A Delphi class VMT; guarded by vmtSelfPtr and the RTTI class name.
    Vmt,
    // A method slot inside a VMT; guarded by the method it must hold.
    VmtSlot,
    // An object field offset (not an address).
    FieldOffset,
    // A whole object layout family (field offsets read by managed readers).
    // No build-independent structural validation exists yet, so a layout is
    // verified only on a known code identity.
    Layout
}

public enum ResolutionMethod : byte { Unresolved, KnownLayout, RttiClass, RttiMethod, RttiField, RttiProperty, Signature, VmtSlotValue }

public enum CaptureKind : byte
{
    // The target is the match address plus Offset.
    Match,
    // The target is the absolute 32-bit value stored at match + Offset.
    Absolute32,
    // The target is a rel32 operand at match + Offset (next instruction + rel32).
    Relative32,
    // The target (a field offset) is the 32-bit displacement at match + Offset.
    Displacement32,
    // The target (a field offset) is the 8-bit displacement at match + Offset.
    Displacement8,
    // The 32-bit value at match + Offset is the address of a pointer cell in a
    // data section; the target is the address stored in that cell (Delphi
    // globals of another unit are reached through such cells).
    Indirect32
}

// A masked byte pattern: hex bytes separated by spaces, "??" for a wildcard.
// Within restricts the search to the first WithinLength bytes of a resolved
// Function target; otherwise every executable section is searched. A
// signature resolves only when it matches exactly once.
public sealed record NativeSignature(string Pattern, int Offset = 0, CaptureKind Capture = CaptureKind.Match, string? Within = null, int WithinLength = 0);

// One native assumption of a build profile. KnownValue is the address (or
// field offset) in the reference layout and is used only as a fast path when
// the code identity matches; every strategy's result is checked by the guard.
// RttiProperty ("Name.get" / "Name.set") resolves through the published
// property of ClassName: a field getter yields a FieldOffset, a static or
// virtual setter yields a Function address.
public sealed record NativeTargetSpec(
    string Id,
    NativeTargetKind Kind,
    uint KnownValue,
    string? Guard = null,
    string? ClassName = null,
    string? RttiName = null,
    string? SlotOf = null,
    string? SlotValue = null,
    string? CallTarget = null,
    IReadOnlyList<NativeSignature>? Signatures = null,
    string? RttiProperty = null);

// A capability is available only when every target it needs is verified.
public sealed record NativeCapabilitySpec(string Id, IReadOnlyList<string> Targets);

public sealed record NativeLayoutPack(string Name, uint PreferredBase, IReadOnlyList<string> KnownCodeSha256, IReadOnlyList<NativeTargetSpec> Targets, IReadOnlyList<NativeCapabilitySpec> Capabilities)
{
    public NativeTargetSpec Target(string id) => Targets.FirstOrDefault(t => t.Id == id) ?? throw new KeyNotFoundException("Unknown native target: " + id);
}

public sealed record ResolvedTarget(string Id, NativeTargetKind Kind, uint Value, ResolutionMethod Method, bool Verified, string? Failure);
public sealed record CapabilityState(string Id, bool Available, IReadOnlyList<string> MissingTargets);

public sealed record NativeResolution(string CodeSha256, bool KnownIdentity, uint ImageBase, uint ImageSize, IReadOnlyDictionary<string, ResolvedTarget> Targets, IReadOnlyDictionary<string, CapabilityState> Capabilities)
{
    public bool IsAvailable(string capability) => Capabilities.TryGetValue(capability, out var state) && state.Available;
    public bool TryGet(string target, out uint value)
    {
        value = 0;
        if (!Targets.TryGetValue(target, out var resolved) || !resolved.Verified) return false;
        value = resolved.Value; return true;
    }
}

internal readonly struct MaskedPattern
{
    public MaskedPattern(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        Bytes = new byte[parts.Length]; Mask = new bool[parts.Length];
        for (var i = 0; i < parts.Length; i++)
        {
            if (parts[i] is "??" or "?") continue;
            Bytes[i] = Convert.ToByte(parts[i], 16); Mask[i] = true;
        }
        if (parts.Length == 0 || !Mask.Any(x => x)) throw new FormatException("A pattern needs at least one fixed byte.");
    }
    public byte[] Bytes { get; }
    public bool[] Mask { get; }
    public int Length => Bytes.Length;
    public bool Matches(ReadOnlySpan<byte> data)
    {
        if (data.Length < Length) return false;
        for (var i = 0; i < Length; i++) if (Mask[i] && data[i] != Bytes[i]) return false;
        return true;
    }
}
