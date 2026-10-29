using System.IO.Compression;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using HoldItTogether.Services.Extensions;

internal static class ExtensionUpdateChecks
{
    public static async Task RunAsync()
    {
        var fixture = File.ReadAllBytes("artifacts/packages/chess-v1.1.1.hitmenu");
        byte[] Package(string version, string id = "dev.hit.chess", bool extraPermission = false)
        {
            using var bytes = new MemoryStream();
            using (var target = new ZipArchive(bytes, ZipArchiveMode.Create, true))
            using (var source = new ZipArchive(new MemoryStream(fixture)))
                foreach (var entry in source.Entries)
                {
                    using var output = target.CreateEntry(entry.FullName).Open();
                    using var input = entry.Open();
                    if (entry.FullName == "manifest.json")
                    {
                        var json = JsonNode.Parse(new StreamReader(input).ReadToEnd())!;
                        json["version"] = version; json["id"] = id;
                        if (version == "1.0.0") json.AsObject().Remove("updates");
                        if (extraPermission) json["capabilities"] = new JsonArray("links.open");
                        using var writer = new StreamWriter(output); writer.Write(json.ToJsonString());
                    }
                    else input.CopyTo(output);
                }
            return bytes.ToArray();
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        var store = new ExtensionStore(Path.GetFullPath(Path.Combine("obj/update-tests", Guid.NewGuid().ToString("N"))));
        using var original = new MemoryStream(Package("1.0.0"));
        var installed = await store.InstallAsync(original, "old.hitmenu");
        var data = Path.Combine(store.Root, "State/keep.txt"); File.WriteAllText(data, "saved");
        var handler = new Fixture { Package = fixture };
        using var client = new HttpClient(handler);
        var updates = new ExtensionUpdateService(store, client);
        var update = await updates.CheckAsync(installed, CancellationToken.None);
        Check(update?.Version == "1.1.1", "Legacy Chess discovers the highest stable package independently of HIT tags.");
        foreach (var invalid in new[] { Package("1.1.1", "dev.other.chess"), Package("1.1.0"), Package("1.1.1", extraPermission: true), new byte[] { 1, 2, 3 } })
        {
            handler.Package = invalid;
            try { await updates.InstallAsync(update!, CancellationToken.None); throw new Exception("Invalid update installed."); }
            catch (InvalidDataException) { }
            Check(store.State.Packages.Single().Manifest.Version == "1.0.0", "Rejected download preserves registry.");
        }
        handler.Fail = true;
        try { await updates.CheckAsync(installed, CancellationToken.None); throw new Exception("HTTP failure ignored."); } catch (HttpRequestException) { }
        handler.Fail = false;
        try { await updates.CheckAsync(installed, new CancellationToken(true)); throw new Exception("Cancellation ignored."); } catch (OperationCanceledException) { }
        handler.Package = fixture;
        var next = await updates.InstallAsync(update!, CancellationToken.None);
        Check(next.Manifest.Version == "1.1.1" && File.ReadAllText(data) == "saved", "Update installs and preserves saved state.");
        Check(await updates.CheckAsync(next, CancellationToken.None) is null, "Current package has no update.");
        try { await updates.InstallAsync(update!, CancellationToken.None); throw new Exception("Stale update installed."); } catch (InvalidOperationException) { }
        Console.WriteLine("PASS: GitHub extension discovery, legacy Chess, stable version selection, hostile URLs, identity/permission rejection, HTTP failure, cancellation, stale updates and preserved data.");
    }

    private sealed class Fixture : HttpMessageHandler
    {
        public byte[] Package = [];
        public bool Fail;
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            if (Fail) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            if (request.RequestUri!.Host != "api.github.com")
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Package) });
            object Asset(string version, string host = "github.com") => new { name = $"chess-v{version}.hitmenu", size = 5000, browser_download_url = $"https://{host}/RosaTheFlower/HoldItTogether/releases/download/2v2910/chess-v{version}.hitmenu" };
            var json = JsonSerializer.Serialize(new[] {
                new { draft = false, prerelease = false, assets = new[] { Asset("1.0.0"), Asset("1.1.1"), Asset("9.0.0", "example.com") } },
                new { draft = false, prerelease = true, assets = new[] { Asset("8.0.0") } },
                new { draft = true, prerelease = false, assets = new[] { Asset("7.0.0") } }
            });
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) });
        }
    }
}
