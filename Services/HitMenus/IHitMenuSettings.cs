namespace HoldItTogether.Services.HitMenus;

/// <summary>Optional menu settings. M2 on the shared title opens them; M2 inside returns to content.</summary>
public interface IHitMenuSettings : HIT.Sdk.IHitMenuSettings
{
    /// <summary>Whether the shared header can return one level within this menu.</summary>
    bool CanGoBack => IsSettingsOpen;
}
