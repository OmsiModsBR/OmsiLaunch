using OmsiLaunch.Api;
using OmsiLaunch.Core;

// Start date/time of a NEW_MAP session: resolved into OMSI's start clock
// (year, day of the year, hour, minute, second) before the handoff.
internal static class StartDateTimeTests
{
    public static void Resolution()
    {
        var unsetDate = new DateSpec(DateTimeMode.Unset, OptionalValue<SemanticDate>.Unset);
        var unsetTime = new TimeSpec(DateTimeMode.Unset, OptionalValue<SemanticTime>.Unset);
        var now = new DateTime(2026, 10, 12, 21, 5, 9);
        var start = StartDateTimeResolver.Resolve(Spec(Date(2024, 3, 1), new(DateTimeMode.Explicit, OptionalValue<SemanticTime>.Set(new(8, 15, 30)))), now, out var error);
        Require(error is null && start == new StartDateTime(2024, 61, 8, 15, 30), "explicit date and time: " + start + " " + error);
        var system = StartDateTimeResolver.Resolve(Spec(new(DateTimeMode.System, OptionalValue<SemanticDate>.Unset), unsetTime, new(DateTimeMode.Explicit, OptionalValue<int>.Set(1999))), now, out error);
        Require(error is null && system == new StartDateTime(1999, now.DayOfYear, null, null, null), "system date with an explicit year: " + system + " " + error);
        var systemTime = StartDateTimeResolver.Resolve(Spec(unsetDate, new(DateTimeMode.System, OptionalValue<SemanticTime>.Unset)), now, out error);
        Require(error is null && systemTime == new StartDateTime(null, null, 21, 5, 9), "system time");
        Require(StartDateTimeResolver.Resolve(Spec(unsetDate, unsetTime), now, out error) is null && error is null, "unset modes keep OMSI's own start time");
        StartDateTimeResolver.Resolve(Spec(Date(2024, 12, 31), unsetTime), now, out error);
        Require(error is not null && error.Contains("leap"), "31 December of a leap year must be refused");
        StartDateTimeResolver.Resolve(Spec(Date(2023, 2, 29), unsetTime), now, out error);
        Require(error is not null, "29 February of a common year must be refused");
        StartDateTimeResolver.Resolve(Spec(Date(1850, 6, 1), unsetTime), now, out error);
        Require(error is not null, "a year outside 1900..2200 must be refused");
        Console.WriteLine("PASS start-date-time.resolution");
    }

    private static DateSpec Date(int year, int month, int day) => new(DateTimeMode.Explicit, OptionalValue<SemanticDate>.Set(new(year, month, day)));

    private static LaunchSpec Spec(DateSpec date, TimeSpec time, YearSpec? year = null)
    {
        var none = new Dictionary<string, OptionalValue<string>>();
        return new LaunchSpec(new(@"C:\omsi"), new(WorldMode.NewMap, OptionalValue<string>.Set(@"maps\Grundorf\global.cfg"), OptionalValue<string>.Unset, OptionalValue<int>.Set(0)), date, time, OptionalValue<PlayerVehicleSpec>.Unset, new(none, none, none, none, none, none, none, none), new(), Year: year);
    }

    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
