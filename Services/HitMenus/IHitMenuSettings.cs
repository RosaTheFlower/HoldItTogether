namespace HoldItTogether.Services.HitMenus;

/// <summary>Optional per-menu settings, opened by right-clicking the shared title.</summary>
public interface IHitMenuSettings
{
    bool IsSettingsOpen { get; }
    Task OpenSettingsAsync();
}
