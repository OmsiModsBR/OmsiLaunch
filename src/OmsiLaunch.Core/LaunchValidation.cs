using OmsiLaunch.Api;

namespace OmsiLaunch.Core;

public static class LaunchValidation
{
    public static IReadOnlyList<LaunchDiagnostic> Validate(LaunchSpec spec)
    {
        var diagnostics = new List<LaunchDiagnostic>();
        if (string.IsNullOrWhiteSpace(spec.Installation.RootPath)) diagnostics.Add(new("OL_E_INSTALLATION_NOT_FOUND", "Installation root is required."));
        // The exact restore is an invariant of every session: it cannot be switched off.
        if (spec.Behavior is { RestoreConfiguration: false }) diagnostics.Add(new("OL_E_INVALID_ARGUMENT", "RestoreConfiguration=false is not supported: every file a session changes is restored exactly when it ends."));
        if (spec.Installation.ExpectedExecutableSha256 is { } pin && !IsSha256(pin)) diagnostics.Add(new("OL_E_INVALID_ARGUMENT", "ExpectedExecutableSha256 must be 64 hexadecimal digits."));
        ValidateDate(spec.Date, diagnostics); ValidateTime(spec.Time, diagnostics);
        if (spec.World.Mode == WorldMode.NewMap)
        {
            if (!spec.World.MapIdentity.IsSet || !IsMapIdentity(spec.World.MapIdentity.Value)) diagnostics.Add(new("OL_E_MAP_NOT_FOUND", "NEW_MAP requires a normalized maps\\*.cfg identity."));
            if (!spec.World.EntrypointIdentity.IsSet && (!spec.World.PresentedEntrypointIndex.IsSet || spec.World.PresentedEntrypointIndex.Value is < 0)) diagnostics.Add(new("OL_E_ENTRYPOINT_NOT_FOUND", "NEW_MAP requires an entrypoint identity or a non-negative presented entrypoint index."));
        }
        if (spec.World.Mode == WorldMode.LastMapState && (!spec.World.MapIdentity.IsSet || !IsMapIdentity(spec.World.MapIdentity.Value))) diagnostics.Add(new("OL_E_MAP_NOT_FOUND", "LAST_MAP_STATE requires the map whose last state is loaded (maps\\<folder>\\global.cfg)."));
        if (spec.World.Mode == WorldMode.SavedSituation && !spec.World.SituationIdentity.IsSet) diagnostics.Add(new("OL_E_SITUATION_NOT_FOUND", "SAVED_SITUATION requires a canonical situation identity."));
        return diagnostics;
    }

    public static bool IsSha256(string value) => value.Length == 64 && value.All(Uri.IsHexDigit);

    public static bool IsMapIdentity(string? value) => !string.IsNullOrWhiteSpace(value) && value.StartsWith("maps\\", StringComparison.OrdinalIgnoreCase) && value.EndsWith("\\global.cfg", StringComparison.OrdinalIgnoreCase) && !value.Contains("..", StringComparison.Ordinal);
    private static void ValidateDate(DateSpec value, ICollection<LaunchDiagnostic> diagnostics)
    {
        if (value.Mode == DateTimeMode.Explicit && (!value.Value.IsSet || value.Value.Value is not { } date || date.Month is < 1 or > 12 || date.Day is < 1 or > 31)) diagnostics.Add(new("OL_E_DATE_TIME_APPLY_FAILED", "Explicit date requires valid year/month/day values."));
        if (value.Mode != DateTimeMode.Explicit && value.Value.IsSet) diagnostics.Add(new("OL_E_INVALID_ARGUMENT", "Date value requires Explicit date mode."));
    }
    private static void ValidateTime(TimeSpec value, ICollection<LaunchDiagnostic> diagnostics)
    {
        if (value.Mode == DateTimeMode.Explicit && (!value.Value.IsSet || value.Value.Value is not { } time || time.Hour is < 0 or > 23 || time.Minute is < 0 or > 59 || time.Second is < 0 or > 59)) diagnostics.Add(new("OL_E_DATE_TIME_APPLY_FAILED", "Explicit time requires valid hour/minute/second values."));
        if (value.Mode != DateTimeMode.Explicit && value.Value.IsSet) diagnostics.Add(new("OL_E_INVALID_ARGUMENT", "Time value requires Explicit time mode."));
    }
}
