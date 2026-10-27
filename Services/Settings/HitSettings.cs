using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Themes;

namespace HoldItTogether.Services.Settings;

public sealed record HitSettings
{
    public bool AlwaysOnTop { get; init; } = true;
    public bool ForceAlwaysOnTop { get; init; }
    public int MenuCompactDelayMs { get; init; } = 5000;
    public bool WindowMagnetize { get; init; } = true;
    public bool WindowAllowOtherMonitors { get; init; } = true;
    public int WindowEdgeReachPercent { get; init; } = 98;
    public bool SystemTray { get; init; }
    public bool StartWithWindows { get; init; }
    public bool ShowTooltips { get; init; } = true;
    public ClockPreferences Clock { get; init; } = new();
    public bool AudioArtworkMode { get; init; }
    public int AudioArtworkBlur { get; init; } = 8;
    public bool AudioArtworkGlobalAccent { get; init; }
    public bool AudioArtworkGlobalCover { get; init; } = true;
    public bool AudioArtworkLyricsCover { get; init; }
    public bool AudioLyricsNeutralBackground { get; init; }
    public bool AudioLyricsShowTitle { get; init; } = true;
    public bool AudioLyricsShowSource { get; init; } = true;
    public int AudioArtworkZoom { get; init; } = 100;
    public int AudioArtworkDistortion { get; init; }
    public int AudioArtworkSaturation { get; init; } = 80;
    public bool AudioArtworkColorEnabled { get; init; }
    public bool AudioArtworkDrift { get; init; }
    public bool AudioArtworkRadialBlur { get; init; }
    public int AudioArtworkLens { get; init; }
    public int AudioArtworkChromatic { get; init; }
    public int AudioArtworkVignette { get; init; }
    public int AudioArtworkGrain { get; init; }
    public int AudioArtworkBrightness { get; init; } = 100;
    public int AudioArtworkContrast { get; init; } = 100;
    public int AudioArtworkTemperature { get; init; }
    public int AudioArtworkTint { get; init; }
    public int AudioArtworkHue { get; init; }
    public int AudioArtworkSepia { get; init; }
    public int AudioArtworkGrayscale { get; init; }
    public bool AudioArtworkOnBeat { get; init; }
    public int AudioArtworkBeatStrength { get; init; } = 100;
    public bool AudioVisualizerEnabled { get; init; }
    public bool AudioLyricsTimeline { get; init; }
    public int AudioLyricsTimelineThickness { get; init; } = 2;
    public int AudioScaleX { get; init; } = 100;
    public int AudioScaleY { get; init; } = 100;
    public bool AudioHideControls { get; init; }
    public bool AudioHidePlayerButtons { get; init; } = true;
    public bool AudioHideLyricsButtons { get; init; } = true;
    public bool AudioShowSource { get; init; } = true;
    public bool RemoveAnimations { get; init; }
    public string Theme { get; init; } = "default";
    public bool ScrollCooldownEnabled { get; init; } = true;
    public int ScrollCooldownMs { get; init; } = 350;
    public double ScrollSensitivity { get; init; } = 1;

    public HitSettings Normalize() => this with
    {
        Clock = (Clock ?? new()).Normalize(),
        ForceAlwaysOnTop = AlwaysOnTop && ForceAlwaysOnTop,
        MenuCompactDelayMs = Math.Clamp(MenuCompactDelayMs, 0, 10000),
        WindowEdgeReachPercent = Math.Clamp(WindowEdgeReachPercent,85,100),
        Theme = HitBundledThemes.NormalizeSelection(Theme),
        AudioArtworkBlur = Math.Clamp(AudioArtworkBlur, 0, 40),
        AudioScaleX = Math.Clamp(AudioScaleX, 25, 200),
        AudioScaleY = Math.Clamp(AudioScaleY, 25, 200),
        AudioArtworkZoom = Math.Clamp(AudioArtworkZoom, 100, 180),
        AudioArtworkDistortion = Math.Clamp(AudioArtworkDistortion, 0, 30),
        AudioArtworkSaturation = Math.Clamp(AudioArtworkSaturation, 0, 200),
        AudioArtworkLens = Math.Clamp(AudioArtworkLens, 0, 100),
        AudioArtworkChromatic = Math.Clamp(AudioArtworkChromatic, 0, 20),
        AudioArtworkVignette = Math.Clamp(AudioArtworkVignette, 0, 100),
        AudioArtworkGrain = Math.Clamp(AudioArtworkGrain, 0, 30),
        AudioArtworkBrightness = Math.Clamp(AudioArtworkBrightness, 20, 200),
        AudioArtworkContrast = Math.Clamp(AudioArtworkContrast, 25, 200),
        AudioArtworkTemperature = Math.Clamp(AudioArtworkTemperature, -100, 100),
        AudioArtworkTint = Math.Clamp(AudioArtworkTint, -100, 100),
        AudioArtworkHue = Math.Clamp(AudioArtworkHue, -180, 180),
        AudioArtworkSepia = Math.Clamp(AudioArtworkSepia, 0, 100),
        AudioArtworkGrayscale = Math.Clamp(AudioArtworkGrayscale, 0, 100),
        AudioArtworkBeatStrength = Math.Clamp(AudioArtworkBeatStrength, 0, 200),
        AudioLyricsTimelineThickness = Math.Clamp(AudioLyricsTimelineThickness, 1, 6),
        ScrollCooldownMs = Math.Clamp(ScrollCooldownMs, 0, 2000),
        ScrollSensitivity = double.IsFinite(ScrollSensitivity)
            ? Math.Clamp(ScrollSensitivity, 0.25, 3)
            : 1
    };
}
