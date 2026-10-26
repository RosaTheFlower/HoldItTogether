namespace HoldItTogether.Services.Clock;

public sealed record ClockTimeZoneOption(string Id, string Label);

/// <summary>Uses Windows time zone rules, including daylight saving at the displayed instant.</summary>
public static class ClockTimeZones
{
    public const string LocalId = "local";
    public const int MaxClocks = 12;
    private static readonly IReadOnlyDictionary<string, TimeZoneInfo> Zones = TimeZoneInfo.GetSystemTimeZones()
        .ToDictionary(zone => zone.Id, StringComparer.OrdinalIgnoreCase);
    public static IReadOnlyList<ClockTimeZoneOption> Options { get; } = Array.AsReadOnly(
        new[] { new ClockTimeZoneOption(LocalId, "Horário local do sistema") }
            .Concat(Zones.Values.Select(zone => new ClockTimeZoneOption(zone.Id, zone.DisplayName))).ToArray());

    public static TimeZoneInfo? Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (id == LocalId) return TimeZoneInfo.Local;
        if (Zones.TryGetValue(id, out var zone)) return zone;
        try { return TimeZoneInfo.FindSystemTimeZoneById(id); }
        catch (Exception error) when (error is TimeZoneNotFoundException or InvalidTimeZoneException) { return null; }
    }

    public static DateTimeOffset At(DateTimeOffset now, SystemClock clock) => clock.TimeZoneId == LocalId
        ? now : TimeZoneInfo.ConvertTime(now, Resolve(clock.TimeZoneId) ?? TimeZoneInfo.Local);

    public static SystemClock[] Normalize(SystemClock[]? clocks)
    {
        var clean = (clocks ?? []).Where(clock => clock is not null && Resolve(clock.TimeZoneId) is not null)
            .Take(MaxClocks).Select(clock => clock with
            {
                Name = string.IsNullOrWhiteSpace(clock.Name) ? "Relógio" : clock.Name.Trim()[..Math.Min(clock.Name.Trim().Length, 40)],
                TimeZoneId = clock.TimeZoneId == LocalId ? LocalId : Resolve(clock.TimeZoneId)!.Id
            }).ToArray();
        if (clean.Length == 0) clean = [new("Sistema", LocalId)];
        return clocks is not null && clocks.SequenceEqual(clean) ? clocks : clean;
    }
}
