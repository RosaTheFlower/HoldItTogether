using HoldItTogether.Services.Themes;

namespace HoldItTogether.Services.Settings;

public static class HitSphereSettings
{
    public static HitSettings RestoreDefaults(HitSettings current, HitAppearancePreferences appearance)
    {
        var defaults = new HitSettings();
        return current with
        {
            ShrinkInactiveSphere = defaults.ShrinkInactiveSphere,
            SphereShrinkDelayMs = defaults.SphereShrinkDelayMs,
            MenuCompactDelayMs = defaults.MenuCompactDelayMs,
            AlwaysOnTop = defaults.AlwaysOnTop,
            ForceAlwaysOnTop = defaults.ForceAlwaysOnTop,
            WindowMagnetize = defaults.WindowMagnetize,
            WindowMagnetizeCenter = defaults.WindowMagnetizeCenter,
            WindowAllowOtherMonitors = defaults.WindowAllowOtherMonitors,
            WindowEdgeReachPercent = defaults.WindowEdgeReachPercent,
            IdleOpacityEnabled = defaults.IdleOpacityEnabled,
            IdleOpacitySphere = defaults.IdleOpacitySphere,
            IdleOpacityCompact = defaults.IdleOpacityCompact,
            IdleOpacityOpen = defaults.IdleOpacityOpen,
            IdleOpacityDelayMs = defaults.IdleOpacityDelayMs,
            IdleOpacityPercent = defaults.IdleOpacityPercent,
            IdleOpacityTransitionMs = defaults.IdleOpacityTransitionMs,
            Appearance = appearance.RestoreSphereDefaults()
        };
    }
}
