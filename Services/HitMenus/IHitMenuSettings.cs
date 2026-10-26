namespace HoldItTogether.Services.HitMenus;

/// <summary>Optional menu settings. M2 on the shared title opens them; M2 inside returns to content.</summary>
public interface IHitMenuSettings
{
    bool IsSettingsOpen { get; }
    Task OpenSettingsAsync();
    Task CloseSettingsAsync();
}
