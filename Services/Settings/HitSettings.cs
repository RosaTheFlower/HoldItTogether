using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Themes;

namespace HoldItTogether.Services.Settings;

public sealed record HitSettings
{
    public bool AlwaysOnTop { get; init; } = true;
    public bool SystemTray { get; init; }
    public bool ShowTooltips { get; init; } = true;
    public ClockPreferences Clock { get; init; } = new();
    public bool AudioArtworkMode { get; init; }
    public int AudioArtworkBlur { get; init; } = 8;
    public bool AudioArtworkGlobalAccent { get; init; }
    public bool AudioArtworkGlobalCover { get; init; } = true;
    public bool AudioArtworkLyricsCover { get; init; }
    public int AudioScaleX { get; init; } = 100;
    public int AudioScaleY { get; init; } = 100;
    public bool AudioHideControls { get; init; }
    public bool AudioShowSource { get; init; } = true;
    public bool RemoveAnimations { get; init; }
    public string Theme { get; init; } = "default";
    public bool ScrollCooldownEnabled { get; init; } = true;
    public int ScrollCooldownMs { get; init; } = 350;
    public double ScrollSensitivity { get; init; } = 1;

    public HitSettings Normalize() => this with
    {
        Clock = (Clock ?? new()).Normalize(),
        Theme = HitBundledThemes.NormalizeSelection(Theme),
        AudioArtworkBlur = Math.Clamp(AudioArtworkBlur, 0, 40),
        AudioScaleX = Math.Clamp(AudioScaleX, 75, 200),
        AudioScaleY = Math.Clamp(AudioScaleY, 75, 200),
        ScrollCooldownMs = Math.Clamp(ScrollCooldownMs, 0, 2000),
        ScrollSensitivity = double.IsFinite(ScrollSensitivity)
            ? Math.Clamp(ScrollSensitivity, 0.25, 3)
            : 1
    };
}
