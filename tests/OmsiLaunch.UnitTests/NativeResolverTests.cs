using System.Buffers.Binary;
using System.Text;
using OmsiLaunch.Builds.Omsi23004;
using OmsiLaunch.NativeResolution;

// Beta 4 build-independent native resolution (R1). Every case runs on
// synthetic PE images, so no OMSI installation is needed:
//  - a known code identity uses the reference address but still checks guards;
//  - an unknown identity resolves each target by RTTI, signature or VMT slot;
//  - an unresolvable target disables only the capabilities that need it;
//  - nothing ever falls back to a fixed address.
internal static class NativeResolverTests
{
    private const uint Base = 0x00400000, Text = Base + 0x1000, Data = Base + 0x3000;
    private static readonly byte[] Prologue = { 0x55, 0x8B, 0xEC, 0x83, 0xC4, 0xF0, 0x53, 0x56, 0x57, 0x8B, 0xDA, 0x89, 0x45, 0xFC, 0xB8, 0x11, 0x22, 0x33, 0x44, 0x5F, 0x5E, 0x5B, 0xC9, 0xC3 };

    public static void Run()
    {
        KnownIdentityFastPathStillGuarded();
        UnknownIdentityResolvesByStrategy();
        FailureIsPerCapability();
        AmbiguityAndWrongClassAreRejected();
        LayoutNeedsKnownIdentity();
        CatalogIsConsistent();
        Console.WriteLine("PASS native-resolution.per-capability");
    }

    // Reference image: function at +0x100, a global cell and a Delphi class.
    private static byte[] Image(int functionShift = 0, bool corruptPrologue = false, bool duplicate = false, string className = "TSample")
    {
        var file = PeFile();
        var function = Text + 0x100 + (uint)functionShift;
        Write(file, function, Prologue);
        if (corruptPrologue) Write(file, function, new byte[] { 0xE9, 0x00, 0x00, 0x00, 0x00 });
        if (duplicate) Write(file, Text + 0x600, Prologue);
        // Code that reads the global through a pointer cell: mov eax,[cell]; mov eax,[eax]; ret
        var cell = Data + 0x40; var global = Data + 0x80;
        Write(file, Text + 0x400, new byte[] { 0x90, 0x90, 0x53, 0x8B, 0xD8, 0xA1 }); WriteU32(file, Text + 0x406, cell); Write(file, Text + 0x40A, new byte[] { 0x8B, 0x00, 0x5B, 0xC3, 0xCC, 0xCC, 0x31, 0x77, 0x42, 0x19 });
        WriteU32(file, cell, global);
        // Delphi class: name, VMT with self pointer and class-name pointer, one method slot.
        var name = Data + 0x200; var vmt = Data + 0x300;
        Write(file, name, new[] { (byte)className.Length }.Concat(Encoding.Latin1.GetBytes(className)).ToArray());
        WriteU32(file, vmt - NativeResolver.VmtSelfPtr, vmt);
        WriteU32(file, vmt - NativeResolver.VmtClassName, name);
        WriteU32(file, vmt + 0x20, function);
        return file;
    }

    private static NativeLayoutPack Pack(string? knownIdentity) => new("Sample", Base, knownIdentity is null ? Array.Empty<string>() : new[] { knownIdentity }, new[]
    {
        new NativeTargetSpec("fn", NativeTargetKind.Function, Text + 0x100, Guard: "55 8B EC 83 C4 F0", Signatures: new[] { new NativeSignature("55 8B EC 83 C4 F0 53 56 57 8B DA 89 45 FC B8 ?? ?? ?? ?? 5F 5E 5B C9 C3") }),
        new NativeTargetSpec("global", NativeTargetKind.Global, Data + 0x80, Signatures: new[] { new NativeSignature("53 8B D8 A1 ?? ?? ?? ?? 8B 00 5B C3 CC CC 31 77", 4, CaptureKind.Indirect32) }),
        new NativeTargetSpec("vmt", NativeTargetKind.Vmt, Data + 0x300, ClassName: "TSample"),
        new NativeTargetSpec("slot", NativeTargetKind.VmtSlot, Data + 0x320, SlotOf: "vmt", SlotValue: "fn"),
        new NativeTargetSpec("layout", NativeTargetKind.Layout, 0),
    }, new[]
    {
        new NativeCapabilitySpec("cap.call", new[] { "fn", "slot", "vmt" }),
        new NativeCapabilitySpec("cap.read", new[] { "global" }),
        new NativeCapabilitySpec("cap.layout", new[] { "global", "layout" }),
    });

    private static void KnownIdentityFastPathStillGuarded()
    {
        var image = NativeImage.FromFile(Image());
        var resolution = NativeResolver.Resolve(image, Pack(image.CodeSha256));
        Require(resolution.KnownIdentity && resolution.Targets.Values.All(t => t.Verified && t.Method == ResolutionMethod.KnownLayout), "known identity must resolve every target through the verified fast path");
        Require(resolution.TryGet("fn", out var fn) && fn == Text + 0x100 && resolution.IsAvailable("cap.call") && resolution.IsAvailable("cap.layout"), "known identity values");
        // The same identity claimed for an image whose guard fails: the fast
        // path is rejected and the signature is not found either.
        var patched = NativeImage.FromFile(Image(corruptPrologue: true));
        var rejected = NativeResolver.Resolve(patched, Pack(patched.CodeSha256));
        Require(!rejected.Targets["fn"].Verified && rejected.Targets["fn"].Failure!.Contains("guard bytes differ"), "a guard failure on the known identity must not be accepted");
        Require(!rejected.IsAvailable("cap.call") && rejected.IsAvailable("cap.read"), "only the capability that needs the failed target is unavailable");
    }

    private static void UnknownIdentityResolvesByStrategy()
    {
        var shifted = NativeImage.FromFile(Image(functionShift: 0x40));
        var resolution = NativeResolver.Resolve(shifted, Pack("0000"));
        Require(!resolution.KnownIdentity, "identity must be unknown");
        Require(resolution.Targets["fn"] is { Verified: true, Method: ResolutionMethod.Signature } fn && fn.Value == Text + 0x140, "function must move with the code (signature)");
        Require(resolution.Targets["global"] is { Verified: true, Method: ResolutionMethod.Signature } g && g.Value == Data + 0x80, "global must resolve through the pointer cell");
        Require(resolution.Targets["vmt"] is { Verified: true, Method: ResolutionMethod.RttiClass } v && v.Value == Data + 0x300, "VMT must resolve through RTTI");
        Require(resolution.Targets["slot"] is { Verified: true, Method: ResolutionMethod.VmtSlotValue } s && s.Value == Data + 0x320, "slot must resolve by the method it holds");
        Require(resolution.IsAvailable("cap.call") && resolution.IsAvailable("cap.read"), "resolved capabilities must be available");
        Require(resolution.Targets.Values.All(t => t.Method != ResolutionMethod.KnownLayout), "no target may use the reference address without the known identity");
    }

    private static void FailureIsPerCapability()
    {
        var broken = NativeImage.FromFile(Image(corruptPrologue: true));
        var resolution = NativeResolver.Resolve(broken, Pack(null));
        Require(!resolution.Targets["fn"].Verified && resolution.Targets["fn"].Value == 0, "an unresolved function must have no address");
        Require(!resolution.Targets["slot"].Verified && resolution.Targets["slot"].Failure!.Contains("dependencies unresolved"), "a dependent slot must stay unresolved");
        Require(!resolution.IsAvailable("cap.call") && resolution.Capabilities["cap.call"].MissingTargets.Contains("fn"), "the capability must name its missing target");
        Require(resolution.IsAvailable("cap.read") && resolution.Targets["vmt"].Verified, "unrelated targets and capabilities must stay available");
    }

    private static void AmbiguityAndWrongClassAreRejected()
    {
        var duplicated = NativeResolver.Resolve(NativeImage.FromFile(Image(duplicate: true)), Pack(null));
        Require(!duplicated.Targets["fn"].Verified && duplicated.Targets["fn"].Failure!.Contains("ambiguous"), "an ambiguous signature must not resolve");
        var renamed = NativeResolver.Resolve(NativeImage.FromFile(Image(className: "TOther")), Pack(null));
        Require(!renamed.Targets["vmt"].Verified && !renamed.IsAvailable("cap.call"), "a VMT with another class name must not resolve");
    }

    private static void LayoutNeedsKnownIdentity()
    {
        var resolution = NativeResolver.Resolve(NativeImage.FromFile(Image()), Pack(null));
        Require(!resolution.Targets["layout"].Verified && resolution.Targets["layout"].Failure!.Contains("known code identity"), "object layouts are verified only for a known identity");
        Require(!resolution.IsAvailable("cap.layout") && resolution.IsAvailable("cap.read"), "layout-dependent capabilities are unavailable on an unknown build");
    }

    // The production catalog: every capability names declared targets, every
    // derived entry belongs to a declaration, ids are unique.
    private static void CatalogIsConsistent()
    {
        var pack = NativeTargetCatalog.Pack;
        var ids = pack.Targets.Select(t => t.Id).ToArray();
        Require(ids.Distinct(StringComparer.Ordinal).Count() == ids.Length, "native target ids must be unique");
        foreach (var capability in pack.Capabilities)
            foreach (var target in capability.Targets)
                Require(ids.Contains(target), "capability " + capability.Id + " names an undeclared target " + target);
        foreach (var target in pack.Targets.Where(t => t.Kind is NativeTargetKind.Function or NativeTargetKind.CallSite))
            Require(target.Guard is not null || target.RttiProperty is not null, "code target without a guard: " + target.Id);
    }

    private static byte[] PeFile()
    {
        var file = new byte[0x4400];
        file[0] = (byte)'M'; file[1] = (byte)'Z'; BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(0x3C), 0x40);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(0x40), 0x00004550);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(0x44), 0x014C); BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(0x46), 2);
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(0x54), 0xE0);
        var optional = 0x58;
        BinaryPrimitives.WriteUInt16LittleEndian(file.AsSpan(optional), 0x10B);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 28), Base);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 32), 0x1000); BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 36), 0x200);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 56), 0x5000); BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 60), 0x400);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(optional + 92), 16);
        Section(file, optional + 0xE0, ".text", 0x1000, 0x2000, 0x400, 0x60000020);
        Section(file, optional + 0xE0 + 40, ".data", 0x3000, 0x2000, 0x2400, 0xC0000040);
        return file;
    }

    private static void Section(byte[] file, int at, string name, uint rva, uint size, uint raw, uint characteristics)
    {
        Encoding.ASCII.GetBytes(name).CopyTo(file, at);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 8), size); BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 12), rva);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 16), size); BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 20), raw);
        BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(at + 36), characteristics);
    }

    private static int FileOffset(uint va) => va >= Data ? (int)(va - Data + 0x2400) : (int)(va - Text + 0x400);
    private static void Write(byte[] file, uint va, byte[] bytes) => bytes.CopyTo(file, FileOffset(va));
    private static void WriteU32(byte[] file, uint va, uint value) => BinaryPrimitives.WriteUInt32LittleEndian(file.AsSpan(FileOffset(va)), value);
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("native-resolution: " + message); }
}
