using OmsiLaunch.Api;

namespace OmsiLaunch.Core;

// Turns LaunchSpec.Date/Time/Year into OMSI's start clock: year, 1-based day
// of the year, hour, minute, second (research report beta4-static
// start-date-time.md). "System" is the PC clock at planning time. A mode left
// Unset keeps OMSI's own value (its options and Start-form preparation).
public static class StartDateTimeResolver
{
    public const int MinimumYear = 1900, MaximumYear = 2200;

    public static StartDateTime? Resolve(LaunchSpec spec, DateTime now, out string? error)
    {
        error = null;
        int? year = null, dayOfYear = null, hour = null, minute = null, second = null;
        var yearSpec = spec.EffectiveYear;
        if (yearSpec.Mode == DateTimeMode.Explicit && yearSpec.Value.IsSet) year = yearSpec.Value.Value;
        else if (yearSpec.Mode == DateTimeMode.System) year = now.Year;

        if (spec.Date.Mode is DateTimeMode.Explicit or DateTimeMode.System)
        {
            var date = spec.Date.Mode == DateTimeMode.System ? new SemanticDate(now.Year, now.Month, now.Day) : spec.Date.Value.IsSet ? spec.Date.Value.Value : null;
            if (date is null) { error = "an explicit date needs a value"; return null; }
            year ??= date.Year;
            if (date.Month is < 1 or > 12 || date.Day < 1 || date.Day > DateTime.DaysInMonth(Math.Clamp(year.Value, 1, 9999), date.Month))
            {
                error = $"{year:D4}-{date.Month:D2}-{date.Day:D2} is not a valid date";
                return null;
            }
            dayOfYear = new DateTime(year.Value, date.Month, date.Day).DayOfYear;
            // OMSI's calendar wraps day 366 to 1 January, so the last day of a
            // leap year cannot be represented.
            if (dayOfYear == 366) { error = $"31 December of the leap year {year} cannot be represented by OMSI's calendar"; return null; }
        }
        if (spec.Time.Mode is DateTimeMode.Explicit or DateTimeMode.System)
        {
            var time = spec.Time.Mode == DateTimeMode.System ? new SemanticTime(now.Hour, now.Minute, now.Second) : spec.Time.Value.IsSet ? spec.Time.Value.Value : null;
            if (time is null) { error = "an explicit time needs a value"; return null; }
            if (time.Hour is < 0 or > 23 || time.Minute is < 0 or > 59 || time.Second is < 0 or > 59) { error = $"{time.Hour:D2}:{time.Minute:D2}:{time.Second:D2} is not a valid time"; return null; }
            hour = time.Hour; minute = time.Minute; second = time.Second;
        }
        if (year is < MinimumYear or > MaximumYear) { error = $"the year {year} is outside {MinimumYear}..{MaximumYear}"; return null; }
        var result = new StartDateTime(year, dayOfYear, hour, minute, second);
        return result.IsEmpty ? null : result;
    }
}
