using System.IO.Compression;
using System.Text.Json.Nodes;
using HoldItTogether.Services.Extensions;

internal static class PackageLifecycleChecks
{
    public static async Task RunAsync()
    {
        var fixture = File.ReadAllBytes("artifacts/packages/dev.hit.example-1.0.2.hitmenu");
        var root = Path.GetFullPath(Path.Combine("obj", "package-lifecycle", Guid.NewGuid().ToString("N")));
        var store = new ExtensionStore(root);
        async Task<InstalledExtension> Install(string version)
        {
            using var bytes = new MemoryStream();
            using (var output = new ZipArchive(bytes, ZipArchiveMode.Create, true))
            using (var input = new ZipArchive(new MemoryStream(fixture)))
                foreach (var entry in input.Entries)
                {
                    using var target = output.CreateEntry(entry.FullName).Open();
                    using var source = entry.Open();
                    if (entry.FullName == "manifest.json")
                    {
                        var json = JsonNode.Parse(new StreamReader(source).ReadToEnd())!;
                        json["version"] = version;
                        using var writer = new StreamWriter(target); writer.Write(json.ToJsonString());
                    }
                    else source.CopyTo(target);
                }
            bytes.Position = 0;
            return await store.InstallAsync(bytes, "example.hitmenu");
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        var original = await Install("1.0.0");
        await Install("1.1.0");
        store.ConfirmDowngradeAsync = (_, _, _) => Task.FromResult(false);
        try { await Install("1.0.0"); throw new Exception("Canceled downgrade installed."); }
        catch (OperationCanceledException) { }
        Check(store.State.Packages.Single().Manifest.Version == "1.1.0", "Cancel preserves selected version.");
        store.ConfirmDowngradeAsync = (_, _, _) => Task.FromResult(true);
        await Install("1.0.0");
        Check(store.State.Packages.Single().Manifest.Version == "1.0.0", "Confirmed retained downgrade selects older version.");
        var data = Path.Combine(root, "State", "keep.txt"); File.WriteAllText(data, "saved");
        store.Remove(original.Manifest.Id);
        var loaded = new Dictionary<string, string> { [original.Manifest.Id] = "1.1.0" };
        var cleanup = await store.ClearOldVersionsAsync(loaded);
        Check(cleanup.Removed == 1 && cleanup.Skipped == 1, "Removed package cleanup preserves loaded code only.");
        Check(Directory.Exists(Path.Combine(root, "Installed", original.Manifest.Id, "1.1.0")), "Loaded directory remains.");
        await store.ClearOldVersionsAsync(new Dictionary<string, string>());
        Check(store.State.Packages.Length == 0 && !Directory.Exists(Path.Combine(root, "Installed", original.Manifest.Id)), "Unloaded removed package is deleted.");
        Check(File.ReadAllText(data) == "saved", "Cleanup preserves saved data.");
        Console.WriteLine("PASS: canceled/confirmed retained downgrade, removed-package cleanup, loaded-code protection and saved data.");
    }
}
