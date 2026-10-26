namespace HoldItTogether.Services.Themes;

public sealed record HitBundledTheme(string Id, string Name, string FileName);

/// <summary>Explicit catalog of shipped themes. Unregistered build remnants are ignored.</summary>
public static class HitBundledThemes
{
    public const string DefaultId = "default";
    public const string DefaultFileName = "HIT-OBSIDIAN-EYES.css";
    public const string OledFileName = "HIT-OLED-OFF-WHITE.css";
    public const string OledId = "bundled:" + OledFileName;
    public const string LightFileName = "HIT-LIGHT.css";
    public const string LightId = "bundled:" + LightFileName;

    public static IReadOnlyList<HitBundledTheme> All { get; } = Array.AsReadOnly(new[]
    {
        new HitBundledTheme(DefaultId, "HIT Dark Theme", DefaultFileName),
        new HitBundledTheme(OledId, "HIT OLED", OledFileName),
        new HitBundledTheme(LightId, "HIT Light Theme", LightFileName)
    });

    public static string NormalizeSelection(string? id)
    {
        id = id?.Trim();
        if (string.IsNullOrEmpty(id) || id.StartsWith("bundled:", StringComparison.Ordinal) && !All.Any(theme => theme.Id == id))
            return DefaultId;
        return id;
    }
}
