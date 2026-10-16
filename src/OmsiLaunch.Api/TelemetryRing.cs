using System.IO.MemoryMappedFiles;
using System.Text;

namespace OmsiLaunch.Api;

// Plugin -> host telemetry: a ring of numbered event slots in one shared
// mapping (Beta 4). It replaces the latest-value slot, whose events were lost
// when the plugin emitted faster than the host polled. One writer (the plugin,
// serialized), one reader (the owning host). A reader that falls more than
// SlotCount events behind reports exactly how many it lost; an event is never
// silently skipped or delivered twice.
//
// Layout: header (64 bytes: magic, version, slot count, slot size, committed
// sequence as int64), then SlotCount slots of SlotSize bytes (commit sequence
// int64, payload length int32, UTF-8 payload).
public static class TelemetryRingWire
{
    public const uint Magic = 0x52544C4F; // "OLTR"
    public const int Version = 1;
    public const int SlotCount = 64;
    public const int SlotSize = 2048;
    public const int HeaderSize = 64;
    public const int Capacity = HeaderSize + SlotCount * SlotSize;
    public const int MaxPayload = SlotSize - 12;
    private const int SequenceOffset = 16;

    public static void Initialize(MemoryMappedViewAccessor view)
    {
        view.Write(0, Magic); view.Write(4, Version); view.Write(8, SlotCount); view.Write(12, SlotSize); view.Write(SequenceOffset, 0L);
    }

    // Writes one event; returns false when the payload does not fit a slot or
    // the mapping is not a ring of this version.
    public static bool Write(MemoryMappedViewAccessor view, byte[] payload)
    {
        if (payload.Length > MaxPayload || !IsRing(view)) return false;
        var next = view.ReadInt64(SequenceOffset) + 1;
        var slot = HeaderSize + (long)((next - 1) % SlotCount) * SlotSize;
        // Invalidate, write, commit the slot, then publish the sequence.
        view.Write(slot, 0L);
        view.Write(slot + 8, payload.Length);
        view.WriteArray(slot + 12, payload, 0, payload.Length);
        view.Write(slot, next);
        view.Write(SequenceOffset, next);
        return true;
    }

    public static bool IsRing(MemoryMappedViewAccessor view) =>
        view.ReadUInt32(0) == Magic && view.ReadInt32(4) == Version && view.ReadInt32(8) == SlotCount && view.ReadInt32(12) == SlotSize;

    // Reads every event committed after `after`, oldest first. Lost counts
    // events that were overwritten before this read.
    public static (IReadOnlyList<(long Sequence, string Payload)> Events, long Last, long Lost) ReadSince(MemoryMappedViewAccessor view, long after)
    {
        var events = new List<(long, string)>();
        if (!IsRing(view)) return (events, after, 0);
        var committed = view.ReadInt64(SequenceOffset);
        if (committed <= after) return (events, after, 0);
        long lost = 0;
        var first = after + 1;
        if (committed - after > SlotCount) { first = committed - SlotCount + 1; lost = first - after - 1; }
        for (var sequence = first; sequence <= committed; sequence++)
        {
            var slot = HeaderSize + ((sequence - 1) % SlotCount) * SlotSize;
            if (view.ReadInt64(slot) != sequence) { lost++; continue; }
            var length = view.ReadInt32(slot + 8);
            if (length <= 0 || length > MaxPayload) { lost++; continue; }
            var bytes = new byte[length];
            view.ReadArray(slot + 12, bytes, 0, length);
            // Overwritten while copying: the event is lost, not torn.
            if (view.ReadInt64(slot) != sequence) { lost++; continue; }
            events.Add((sequence, Encoding.UTF8.GetString(bytes)));
        }
        return (events, committed, lost);
    }
}
