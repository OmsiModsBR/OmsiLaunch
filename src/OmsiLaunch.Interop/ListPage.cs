using System.Globalization;

namespace OmsiLaunch.Interop;

// Uniform pagination of runtime lists (Beta 4). A request carries an optional
// opaque cursor and limit; a reply carries count (all records), returned_count,
// truncated (more records follow) and next_cursor (absent on the last page).
// The cursor is a stable key, never a position that shifts when OMSI changes
// a collection: "i:<n>" is the absolute record index of a fixed record array,
// "h:<n>" the last returned handle number of a handle collection. The first
// page (no cursor) of an index list is the Beta 3 reply.
public sealed record ListPage(string? Cursor = null, int? Limit = null)
{
    public static readonly ListPage First = new();

    public static ListPage From(IReadOnlyDictionary<string, string>? arguments)
    {
        string? cursor = null; int? limit = null;
        if (arguments is not null && arguments.TryGetValue("cursor", out var rawCursor) && !string.IsNullOrWhiteSpace(rawCursor)) cursor = rawCursor.Trim();
        if (arguments is not null && arguments.TryGetValue("limit", out var rawLimit) && !string.IsNullOrWhiteSpace(rawLimit))
        {
            if (!int.TryParse(rawLimit, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1) throw new ArgumentOutOfRangeException("limit", "OL_E_RUNTIME_VALUE_OUT_OF_RANGE");
            limit = value;
        }
        return new(cursor, limit);
    }

    // Rows in this page: the requested limit, at most the list's own maximum.
    public int Take(int maximum) => Math.Min(Limit ?? maximum, maximum);

    public int StartIndex() => (int)Parse('i', int.MaxValue);
    public long AfterHandle() => Parse('h', long.MaxValue);

    private long Parse(char kind, long maximum)
    {
        if (Cursor is null) return 0;
        if (Cursor.Length < 3 || Cursor[0] != kind || Cursor[1] != ':' || !long.TryParse(Cursor.AsSpan(2), NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 0 || value > maximum)
            throw new ArgumentException("OL_E_RUNTIME_CURSOR_INVALID: the cursor does not belong to this list");
        return value;
    }

    // Writes the page fields of an index list.
    public static void CompleteIndex(Dictionary<string, string> output, int count, int start, int returned)
    {
        output["count"] = count.ToString(CultureInfo.InvariantCulture);
        output["returned_count"] = returned.ToString(CultureInfo.InvariantCulture);
        var next = (long)start + returned;
        output["truncated"] = (next < count).ToString().ToLowerInvariant();
        if (next < count) output["next_cursor"] = "i:" + next.ToString(CultureInfo.InvariantCulture);
    }

    // Writes the page fields of a handle list.
    public static void CompleteHandles(Dictionary<string, string> output, int count, int returned, long? lastHandle, bool more)
    {
        output["count"] = count.ToString(CultureInfo.InvariantCulture);
        output["returned_count"] = returned.ToString(CultureInfo.InvariantCulture);
        output["truncated"] = more.ToString().ToLowerInvariant();
        if (more && lastHandle is { } last) output["next_cursor"] = "h:" + last.ToString(CultureInfo.InvariantCulture);
    }

    // The number of a session handle ("rv-000123" -> 123).
    public static long HandleNumber(string handle) =>
        long.Parse(handle.AsSpan(handle.IndexOf('-') + 1), NumberStyles.None, CultureInfo.InvariantCulture);
}
