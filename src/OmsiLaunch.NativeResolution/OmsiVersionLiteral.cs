using System.Text;

namespace OmsiLaunch.NativeResolution;

// The OMSI product version the game reports about itself ("Version: 2.3.004"
// on line 7 of its logfile.txt), read from the executable image. The PE file
// version resource says something else on the reference build (2.2.032, with
// product version "2.2") and is never used for this.
//
// OMSI is a Delphi program: the text it writes into its log is a UnicodeString
// literal in the image, laid out as { codepage:u16 = 1200, elemsize:u16 = 2,
// refcount:i32 = -1, length:i32, UTF-16 characters, 0 }. The scan accepts only
// that exact layout for the literal "Version: " followed by n.n.nnn and
// answers only when every match carries the same version; otherwise null
// (the caller reports "unknown", it does not guess).
public static class OmsiVersionLiteral
{
    private static readonly byte[] Prefix = Encoding.Unicode.GetBytes("Version: ");

    public static string? Find(ReadOnlySpan<byte> image)
    {
        string? found = null;
        var from = 0;
        while (from <= image.Length - Prefix.Length)
        {
            var index = image[from..].IndexOf(Prefix);
            if (index < 0) break;
            var start = from + index;
            from = start + 2;
            var version = ReadLiteral(image, start);
            if (version is null) continue;
            if (found is not null && found != version) return null;
            found = version;
        }
        return found;
    }

    private static string? ReadLiteral(ReadOnlySpan<byte> image, int start)
    {
        // The header sits right before the first character.
        if (start < 12) return null;
        var header = image.Slice(start - 12, 12);
        if (header[0] != 0xB0 || header[1] != 0x04 || header[2] != 0x02 || header[3] != 0x00) return null;
        if (header[4] != 0xFF || header[5] != 0xFF || header[6] != 0xFF || header[7] != 0xFF) return null;
        var length = BitConverter.ToInt32(header[8..]);
        // "Version: " is 9 characters; n.n.nnn is 5 to 7 (1-2 digits, dot, 1-2 digits, dot, 3 digits).
        if (length < 9 + 7 - 2 || length > 9 + 7) return null;
        var bytes = length * 2;
        if (start + bytes + 2 > image.Length) return null;
        // The terminating zero must follow the declared length exactly.
        if (image[start + bytes] != 0 || image[start + bytes + 1] != 0) return null;
        var text = Encoding.Unicode.GetString(image.Slice(start, bytes));
        var version = text["Version: ".Length..];
        return IsProductVersion(version) ? version : null;
    }

    // n.n.nnn: major.minor with a three-digit build, as OMSI numbers its releases.
    private static bool IsProductVersion(string value)
    {
        var parts = value.Split('.');
        return parts.Length == 3
            && parts[0].Length is >= 1 and <= 2 && parts[1].Length is >= 1 and <= 2 && parts[2].Length == 3
            && parts.All(part => part.All(c => c is >= '0' and <= '9'));
    }
}
