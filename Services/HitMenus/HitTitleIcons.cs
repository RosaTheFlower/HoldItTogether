using HoldItTogether.Services.Localization;

namespace HoldItTogether.Services.HitMenus;

/// <summary>Presentation-only icons for built-in page titles, including translated titles.</summary>
internal static class HitTitleIcons
{
    private const string Audio = "M5 9v6m5-11v16m5-13v10m5-7v4";
    private const string Clock = "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18m0 4v5l4 2";
    private const string Palette = "M12 3a9 9 0 1 0 0 18h2a2 2 0 0 0 0-4h-1a2 2 0 0 1 0-4h4a4 4 0 0 0 4-4c0-4-5-6-9-6ZM7 9h.01M10 6h.01M15 6h.01M6 13h.01";
    private const string Image = "M4 4h16v16H4ZM4 16l5-5 4 4 3-3 4 4M16 8h.01";
    private const string Sphere = "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18M9 10v4m6-4v4";
    private const string Type = "M4 5h16M12 5v15M8 20h8";
    private static readonly IReadOnlyDictionary<string, string> Paths = new Dictionary<string, string>
    {
        ["Audio"] = Audio, ["Audio settings"] = Audio, ["Visualizer"] = Audio,
        ["Size"] = "M4 9V4h5m6 0h5v5m0 6v5h-5M9 20H4v-5",
        ["Artwork"] = Image, ["Background effects"] = Image, ["Color adjustments"] = Palette,
        ["Hide buttons"] = "M4 8h16v8H4ZM8 12h.01M12 12h.01M16 12h.01",
        ["Idle transparency"] = "M12 3C9 7 5 11 5 15a7 7 0 0 0 14 0c0-4-4-8-7-12Zm0 7v10",
        ["Notification sounds"] = "M6 16V9a6 6 0 0 1 12 0v7l2 2H4Zm4 5h4",
        ["Lyrics"] = "M5 5h14M5 10h14M5 15h10M5 20h7",
        ["Clock"] = Clock, ["Clock settings"] = Clock, ["World clocks"] = Clock,
        ["Timer"] = "M9 2h6M12 2v3M12 5a8 8 0 1 0 0 16 8 8 0 0 0 0-16m0 4v5l3 2", ["Pomodoro"] = Clock,
        ["Chess"] = "M5 21h14v-3H7c0-4 4-5 7-8l-4 1-3 3-3-3 2-5 4-3 1-2 3 2c6 0 7 6 5 11l-1 4M12 7h.01",
        ["Info"] = "M12 3a9 9 0 1 0 0 18 9 9 0 0 0 0-18m0 7v7m0-10h.01",
        ["Languages"] = "M3 5h12M9 2v3M5 5c0 5 4 9 9 11M13 5c0 5-4 9-10 11m11 5 4-12 4 12m-6-4h4",
        ["Changelog"] = "M6 3h12v18H6ZM9 7h6M9 11h6M9 15h4",
        ["Performance"] = "M3 18h18M5 15v-4m5 4V6m5 9V9m5 6V3",
        ["Manage your data"] = "M4 5h16M9 3h6M6 5l1 16h10l1-16M10 9v8m4-8v8",
        ["Themes"] = Palette, ["Personalization"] = Palette, ["Colors"] = Palette,
        ["Appearance"] = "M4 4h16v12H4ZM8 20h8m-4-4v4",
        ["Background media"] = Image, ["Background"] = Image, ["Visual profiles"] = Image,
        ["Interaction"] = "M12 3c-4 0-6 3-6 6v6a6 6 0 0 0 12 0V9c0-3-2-6-6-6Zm0 0v7M6 10h12",
        ["Scroll"] = "m8 7 4-4 4 4M12 3v18m-4-4 4 4 4-4",
        ["Accessibility"] = "M12 3a2 2 0 1 0 0 4 2 2 0 0 0 0-4M4 9h16m-8 0v6m0 0-5 6m5-6 5 6",
        ["Extensions"] = "M4 4h6V2h4v2h6v6h2v4h-2v6h-6v-2h-4v2H4v-6H2v-4h2Z",
        ["Window placement"] = "M4 9V4h5m6 0h5v5m0 6v5h-5M9 20H4v-5M8 8h8v8H8Z",
        ["HIT Sphere"] = Sphere, ["Collapsed sphere"] = Sphere, ["Inactive sphere"] = Sphere,
        ["Eyes"] = Sphere, ["Behavior"] = Sphere, ["Size and shape"] = Sphere,
        ["Shape and typography"] = Type, ["Installed fonts"] = Type, ["Custom font"] = Type
    };

    public static string? ForTitle(string title)
    {
        if (Paths.TryGetValue(title, out var path)) return path;
        foreach (var entry in Paths)
            if (HitText.Get(entry.Key) == title) return entry.Value;
        return null;
    }
}
