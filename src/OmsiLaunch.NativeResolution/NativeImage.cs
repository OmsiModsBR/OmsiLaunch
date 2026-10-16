using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace OmsiLaunch.NativeResolution;

public sealed record NativeSection(string Name, uint VirtualAddress, uint VirtualSize, bool Executable, bool Writable);

// A read-only snapshot of a 32-bit PE image laid out at its virtual
// addresses: index 0 of Bytes is ImageBase. Built either from the executable
// file (tools, tests) or from the loaded module of the current process (plugin).
public sealed class NativeImage
{
    private const uint ScnExecute = 0x20000000, ScnWrite = 0x80000000, ScnCode = 0x00000020;

    private NativeImage(uint imageBase, byte[] bytes, IReadOnlyList<NativeSection> sections, IReadOnlyList<uint> relocations)
    {
        ImageBase = imageBase; Bytes = bytes; Sections = sections; Relocations = relocations;
        var code = sections.FirstOrDefault(s => s.Executable && (s.Name is ".text" or "CODE")) ?? sections.FirstOrDefault(s => s.Executable)
            ?? throw new InvalidDataException("The image has no executable section.");
        CodeSection = code;
        CodeSha256 = Convert.ToHexString(SHA256.HashData(Span(code.VirtualAddress + imageBase, (int)code.VirtualSize))).ToLowerInvariant();
    }

    public uint ImageBase { get; }
    public uint SizeOfImage => (uint)Bytes.Length;
    public byte[] Bytes { get; }
    public IReadOnlyList<NativeSection> Sections { get; }
    // Main code section and the SHA-256 of its bytes: the build identity used
    // only as a fast path to a known layout, never as a compatibility gate.
    public NativeSection CodeSection { get; }
    public string CodeSha256 { get; }
    // Virtual addresses of HIGHLOW base relocations (file images only; empty
    // for a process snapshot). Signature derivation masks these operands.
    public IReadOnlyList<uint> Relocations { get; }

    public bool Contains(uint va, int length = 1) => va >= ImageBase && length >= 0 && (ulong)va - ImageBase + (ulong)length <= (ulong)Bytes.Length;
    public ReadOnlySpan<byte> Span(uint va, int length) => Contains(va, length) ? Bytes.AsSpan((int)(va - ImageBase), length) : ReadOnlySpan<byte>.Empty;
    public bool TryReadUInt32(uint va, out uint value)
    {
        value = 0;
        if (!Contains(va, 4)) return false;
        value = BinaryPrimitives.ReadUInt32LittleEndian(Bytes.AsSpan((int)(va - ImageBase), 4));
        return true;
    }
    public bool TryReadUInt16(uint va, out ushort value)
    {
        value = 0;
        if (!Contains(va, 2)) return false;
        value = BinaryPrimitives.ReadUInt16LittleEndian(Bytes.AsSpan((int)(va - ImageBase), 2));
        return true;
    }
    // Delphi ShortString: length byte followed by that many Latin-1 bytes.
    public bool TryReadShortString(uint va, out string value)
    {
        value = string.Empty;
        if (!Contains(va, 1)) return false;
        var length = Bytes[va - ImageBase];
        if (!Contains(va + 1, length)) return false;
        value = System.Text.Encoding.Latin1.GetString(Bytes, (int)(va - ImageBase + 1), length);
        return true;
    }
    public NativeSection? SectionOf(uint va) => Sections.FirstOrDefault(s => va >= ImageBase + s.VirtualAddress && va < ImageBase + s.VirtualAddress + Math.Max(s.VirtualSize, 1));
    public bool IsExecutable(uint va) => SectionOf(va)?.Executable == true;
    public bool IsWritableData(uint va) => SectionOf(va) is { Writable: true, Executable: false };

    public static NativeImage FromFile(byte[] file)
    {
        var headers = ParseHeaders(file, 0);
        var image = new byte[headers.SizeOfImage];
        Array.Copy(file, 0, image, 0, Math.Min(headers.SizeOfHeaders, (uint)file.Length));
        var sections = new List<NativeSection>();
        foreach (var raw in headers.Sections)
        {
            var copy = raw.VirtualSize == 0 ? raw.RawSize : Math.Min(raw.RawSize, raw.VirtualSize);
            if (raw.RawPointer < file.Length && raw.VirtualAddress < image.Length)
                Array.Copy(file, raw.RawPointer, image, raw.VirtualAddress, (int)Math.Min(Math.Min(copy, (uint)file.Length - raw.RawPointer), (uint)image.Length - raw.VirtualAddress));
            sections.Add(raw.ToSection());
        }
        return new NativeImage(headers.ImageBase, image, sections, ReadRelocations(image, headers));
    }

    // Snapshot of the main module of the current process. Only committed,
    // readable pages are copied; everything else reads as zero. Must run
    // before this process patches the image, so the code identity is pristine.
    public static unsafe NativeImage FromCurrentProcess()
    {
        var module = GetModuleHandleW(null);
        if (module == 0) throw new InvalidOperationException("The main module handle is unavailable.");
        var headerSpan = new ReadOnlySpan<byte>((void*)module, 0x1000);
        var headers = ParseHeaders(headerSpan.ToArray(), 0);
        var image = new byte[headers.SizeOfImage];
        var basePtr = (byte*)module;
        for (uint offset = 0; offset < headers.SizeOfImage;)
        {
            if (VirtualQuery((nint)(basePtr + offset), out var info, (nuint)sizeof(MemoryBasicInformation)) == 0) break;
            var regionEnd = (uint)((ulong)info.BaseAddress + info.RegionSize - (ulong)module);
            var end = Math.Min(regionEnd, headers.SizeOfImage);
            if (info.State == 0x1000 && (info.Protect & 0x101) == 0 && (info.Protect & 0xEE) != 0)
                new ReadOnlySpan<byte>(basePtr + offset, (int)(end - offset)).CopyTo(image.AsSpan((int)offset));
            if (end <= offset) break;
            offset = end;
        }
        return new NativeImage((uint)module, image, headers.Sections.Select(s => s.ToSection()).ToArray(), Array.Empty<uint>());
    }

    private sealed record RawSection(string Name, uint VirtualAddress, uint VirtualSize, uint RawPointer, uint RawSize, uint Characteristics)
    {
        public NativeSection ToSection() => new(Name, VirtualAddress, VirtualSize == 0 ? RawSize : VirtualSize, (Characteristics & (ScnExecute | ScnCode)) != 0, (Characteristics & ScnWrite) != 0);
    }
    private sealed record Headers(uint ImageBase, uint SizeOfImage, uint SizeOfHeaders, uint RelocationRva, uint RelocationSize, IReadOnlyList<RawSection> Sections);

    private static Headers ParseHeaders(byte[] data, int start)
    {
        uint U32(int at) => BinaryPrimitives.ReadUInt32LittleEndian(data.AsSpan(at, 4));
        ushort U16(int at) => BinaryPrimitives.ReadUInt16LittleEndian(data.AsSpan(at, 2));
        if (data.Length < 0x40 || U16(start) != 0x5A4D) throw new InvalidDataException("Not a PE image (MZ).");
        var pe = start + (int)U32(start + 0x3C);
        if (pe + 24 > data.Length || U32(pe) != 0x00004550) throw new InvalidDataException("Not a PE image (PE signature).");
        if (U16(pe + 4) != 0x014C) throw new InvalidDataException("Not a 32-bit x86 image.");
        var count = U16(pe + 6); var optionalSize = U16(pe + 20); var optional = pe + 24;
        if (U16(optional) != 0x10B) throw new InvalidDataException("Not a PE32 optional header.");
        var imageBase = U32(optional + 28); var sizeOfImage = U32(optional + 56); var sizeOfHeaders = U32(optional + 60);
        var directories = U32(optional + 92);
        uint relocRva = 0, relocSize = 0;
        if (directories > 5) { relocRva = U32(optional + 96 + 5 * 8); relocSize = U32(optional + 96 + 5 * 8 + 4); }
        var sections = new List<RawSection>();
        for (var i = 0; i < count; i++)
        {
            var at = optional + optionalSize + i * 40;
            if (at + 40 > data.Length) break;
            var name = System.Text.Encoding.ASCII.GetString(data, at, 8).TrimEnd('\0');
            sections.Add(new(name, U32(at + 12), U32(at + 8), U32(at + 20), U32(at + 16), U32(at + 36)));
        }
        if (sizeOfImage == 0 || sizeOfImage > 256u * 1024 * 1024) throw new InvalidDataException("Implausible SizeOfImage.");
        return new(imageBase, sizeOfImage, sizeOfHeaders, relocRva, relocSize, sections);
    }

    private static IReadOnlyList<uint> ReadRelocations(byte[] image, Headers headers)
    {
        var output = new List<uint>();
        if (headers.RelocationRva == 0 || headers.RelocationSize == 0) return output;
        var at = headers.RelocationRva; var end = Math.Min(headers.RelocationRva + headers.RelocationSize, (uint)image.Length);
        while (at + 8 <= end)
        {
            var page = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan((int)at, 4));
            var size = BinaryPrimitives.ReadUInt32LittleEndian(image.AsSpan((int)at + 4, 4));
            if (size < 8) break;
            for (var entry = at + 8; entry + 2 <= at + size && entry + 2 <= end; entry += 2)
            {
                var value = BinaryPrimitives.ReadUInt16LittleEndian(image.AsSpan((int)entry, 2));
                if (value >> 12 == 3) output.Add(headers.ImageBase + page + (uint)(value & 0xFFF));
            }
            at += size;
        }
        output.Sort();
        return output;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandleW(string? name);
    [DllImport("kernel32.dll")] private static extern nuint VirtualQuery(nint address, out MemoryBasicInformation buffer, nuint length);
    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation { public nint BaseAddress; public nint AllocationBase; public uint AllocationProtect; public nuint RegionSize; public uint State; public uint Protect; public uint Type; }
}
