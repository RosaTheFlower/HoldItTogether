namespace HoldItTogether.Services.Clock;

public sealed record ClockTimeZoneOption(string Id, string Label);

/// <summary>Uses Windows time zone rules, including daylight saving at the displayed instant.</summary>
public static class ClockTimeZones
{
    public const string LocalId = "local";
    public const int MaxClocks = 12;
    private static readonly object Sync = new();
    private static readonly Dictionary<string, TimeZoneInfo> Zones = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Lazy<IReadOnlyList<ClockTimeZoneOption>> Catalog = new(() => Array.AsReadOnly(
        new[] { new ClockTimeZoneOption(LocalId, "Horário local do sistema") }
            .Concat(TimeZoneInfo.GetSystemTimeZones().Select(zone => new ClockTimeZoneOption(zone.Id, zone.DisplayName))).ToArray()));
    public static IReadOnlyList<ClockTimeZoneOption> Options => Catalog.Value;

    public static TimeZoneInfo? Resolve(string? id)
    {
        if (string.IsNullOrWhiteSpace(id)) return null;
        if (id == LocalId) return TimeZoneInfo.Local;
        lock (Sync) if (Zones.TryGetValue(id, out var cached)) return cached;
        try
        {
            var zone = TimeZoneInfo.FindSystemTimeZoneById(id);
            lock (Sync)
            {
                if (Zones.Count >= MaxClocks) Zones.Clear();
                Zones[id] = zone;
            }
            return zone;
        }
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
