using System.Text.Json;
using HoldItTogether.Services.Extensions;

namespace HoldItTogether.Services.Settings;

/// <summary>Applies confirmed data changes at startup, before any service can rewrite saved state.</summary>
public sealed class HitDataService
{
    public static string DefaultRoot => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoldItTogether");
    private readonly string _root;
    public HitDataService() : this(DefaultRoot) { }
    public HitDataService(string root) => _root = Path.GetFullPath(root);
    public void Request(bool deleteEverything)
    {
        Directory.CreateDirectory(_root);
        var marker = Path.Combine(_root, "reset-request.json");
        File.WriteAllText(marker + ".tmp", JsonSerializer.Serialize(new ResetRequest(deleteEverything)));
        File.Move(marker + ".tmp", marker, true);
    }
    public void CancelRequest() => File.Delete(Path.Combine(_root, "reset-request.json"));
    public static void ApplyPending(string? directory = null)
    {
        var root = Path.GetFullPath(directory ?? DefaultRoot);
        if (Path.GetPathRoot(root) == root) throw new InvalidOperationException("Invalid application data directory.");
        var marker = Path.Combine(root, "reset-request.json");
        if (!File.Exists(marker)) return;
        if (new FileInfo(marker).Length > 1024) throw new InvalidDataException("Invalid reset request.");
        var request = JsonSerializer.Deserialize<ResetRequest>(File.ReadAllText(marker)) ?? throw new InvalidDataException("Invalid reset request.");
        void RemoveDirectory(string relative)
        {
            var target = Path.GetFullPath(Path.Combine(root, relative));
            if (!target.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("Invalid reset target.");
            if (Directory.Exists(target)) Directory.Delete(target, true);
        }
        // Only application-owned locations are removed; canonical bundled files live elsewhere.
        File.Delete(Path.Combine(root, "settings.json"));
        File.Delete(Path.Combine(root, "window.json"));
        File.Delete(Path.Combine(root, "window-fullscreen.json"));
        RemoveDirectory("Clock");
        RemoveDirectory("Diagnostics");
        if (request.DeleteEverything)
        {
            RemoveDirectory("Themes");
            RemoveDirectory("Appearance");
            RemoveDirectory("Modules");
        }
        else
        {
            RemoveDirectory(Path.Combine("Modules", "State"));
            RemoveDirectory(Path.Combine("Modules", "Logs"));
            var registry = Path.Combine(root, "Modules", "registry.json");
            if (File.Exists(registry))
            {
                var state = JsonSerializer.Deserialize<ExtensionRegistry>(File.ReadAllText(registry), ExtensionStore.Json) ?? new();
                File.WriteAllText(registry + ".tmp", JsonSerializer.Serialize(state with { Order = [], DisabledMenus = [] }, ExtensionStore.Json));
                File.Move(registry + ".tmp", registry, true);
            }
        }
        File.Delete(marker);
    }
    private sealed record ResetRequest(bool DeleteEverything);
}
