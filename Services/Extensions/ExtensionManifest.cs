using System.Text.RegularExpressions;

namespace HoldItTogether.Services.Extensions;

public sealed record ExtensionMenu
{
    public string Id { get; init; } = "";
    public string Title { get; init; } = "";
    public string ComponentAssembly { get; init; } = "";
    public string ComponentType { get; init; } = "";
    public string[] Styles { get; init; } = [];
}

public sealed record ExtensionManifest
{
    public int FormatVersion { get; init; }
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Author { get; init; } = "";
    public string Version { get; init; } = "";
    public string HitContractVersion { get; init; } = "";
    public string EntryAssembly { get; init; } = "";
    public string EntryType { get; init; } = "";
    public string[] Capabilities { get; init; } = [];
    public ExtensionMenu[] Menus { get; init; } = [];
    public ExtensionUpdateSource? Updates { get; init; }

    public static readonly HashSet<string> KnownCapabilities = new(StringComparer.Ordinal)
    { "media.read", "media.control", "clock.read", "clock.control", "window.control", "theme.control", "links.open", "notifications", "events.subscribe", "host.read", "appearance.read", "media.spectrum", "actions.register", "actions.execute" };

    public void Validate()
    {
        Updates?.Validate();
        if (FormatVersion != 1 || HitContractVersion != "1") throw new InvalidDataException("This extension requires an unsupported HIT SDK version.");
        if (!ValidId(Id) || string.IsNullOrWhiteSpace(Name) || Name.Length > 80 || Author is null || Author.Length > 120)
            throw new InvalidDataException("Invalid extension identity.");
        if (!System.Version.TryParse(Version, out var version) || version.Major < 0 || version.Build < 0 || Version.Length > 32 ||
            !Regex.IsMatch(Version, @"^\d+\.\d+\.\d+(\.\d+)?$")) throw new InvalidDataException("Use a numeric version such as 1.0.0.");
        if (string.IsNullOrWhiteSpace(EntryType) || EntryType.Length > 250 || string.IsNullOrWhiteSpace(EntryAssembly) || !EntryAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("An extension must declare its module assembly and type.");
        SafePath("validation", EntryAssembly);
        if (Capabilities is null || Capabilities.Length > KnownCapabilities.Count || Capabilities.Any(item => !KnownCapabilities.Contains(item)))
            throw new InvalidDataException("Unknown extension capability.");
        if (Menus is null || Menus.Length is < 1 or > 20 || Menus.Select(item => item?.Id).Distinct(StringComparer.OrdinalIgnoreCase).Count() != Menus.Length)
            throw new InvalidDataException("An extension must declare 1–20 menus with unique IDs.");
        foreach (var menu in Menus)
        {
            if (menu is null || !ValidId(menu.Id) || string.IsNullOrWhiteSpace(menu.Title) || menu.Title.Length > 80 ||
                string.IsNullOrWhiteSpace(menu.ComponentType) || menu.ComponentType.Length > 250 ||
                string.IsNullOrWhiteSpace(menu.ComponentAssembly) || !menu.ComponentAssembly.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || menu.Styles is null || menu.Styles.Length > 10)
                throw new InvalidDataException("Invalid menu definition.");
            SafePath("validation", menu.ComponentAssembly);
            foreach (var style in menu.Styles)
            {
                SafePath("validation", style);
                if (!style.EndsWith(".css", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Menu styles must be CSS files.");
            }
        }
    }

    public static bool ValidId(string? id) => id is { Length: >= 1 and <= 100 } && Regex.IsMatch(id, "^[a-z0-9]+([.-][a-z0-9]+)*$");

    public static string SafePath(string root, string relative)
    {
        if (string.IsNullOrWhiteSpace(relative) || relative.Length > 220 || relative.Contains('\\') || relative.Contains(':') || relative.StartsWith('/'))
            throw new InvalidDataException("Invalid package path.");
        var parts = relative.Split('/');
        if (parts.Any(part => part.Length == 0 || part is "." or ".." || part.EndsWith('.') || part.EndsWith(' ') ||
            part.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || Regex.IsMatch(part, @"^(CON|PRN|AUX|NUL|COM[0-9]|LPT[0-9])(\.|$)", RegexOptions.IgnoreCase)))
            throw new InvalidDataException("A package contains an unsafe path.");
        var parent = Path.GetFullPath(root) + Path.DirectorySeparatorChar;
        var result = Path.GetFullPath(Path.Combine(root, relative));
        if (!result.StartsWith(parent, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("A package path escapes its directory.");
        return result;
    }
}

public sealed record ExtensionUpdateSource(string Repository, string AssetPrefix)
{
    public void Validate()
    {
        if (Repository is null || Repository.Length > 201 || !Regex.IsMatch(Repository, @"^[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+$") ||
            AssetPrefix is null || AssetPrefix.Length > 80 || !Regex.IsMatch(AssetPrefix, @"^[a-zA-Z0-9][a-zA-Z0-9._-]*$"))
            throw new InvalidDataException("Invalid extension update source.");
    }
}

public sealed record InstalledExtension(ExtensionManifest Manifest, string Hash, bool PendingDelete = false, string? PreviousVersion = null);
public sealed record ExtensionCleanupResult(int Removed, int Skipped);
public sealed record ExtensionRegistry
{
    public InstalledExtension[] Packages { get; init; } = [];
    public string[] Order { get; init; } = [];
    public string[] DisabledMenus { get; init; } = [];
}
