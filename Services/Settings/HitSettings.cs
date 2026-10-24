namespace HoldItTogether.Services.Settings;

public sealed record HitSettings
{
    public bool AlwaysOnTop { get; init; } = true;
    public bool SystemTray { get; init; }
    public bool RemoveAnimations { get; init; }
    public string Theme { get; init; } = "default";
    public bool ScrollCooldownEnabled { get; init; } = true;
    public int ScrollCooldownMs { get; init; } = 350;
    public double ScrollSensitivity { get; init; } = 1;

    public HitSettings Normalize() => this with
    {
        Theme = string.IsNullOrWhiteSpace(Theme) || Theme.Trim().StartsWith("bundled:", StringComparison.Ordinal)
            ? "default" : Theme.Trim(),
        ScrollCooldownMs = Math.Clamp(ScrollCooldownMs, 0, 2000),
        ScrollSensitivity = double.IsFinite(ScrollSensitivity)
            ? Math.Clamp(ScrollSensitivity, 0.25, 3)
            : 1
    };
}
