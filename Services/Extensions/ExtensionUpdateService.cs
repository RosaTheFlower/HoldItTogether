using System.IO.Compression;
using System.Text.Json;

namespace HoldItTogether.Services.Extensions;

public sealed record ExtensionUpdate(string PackageId, string InstalledVersion, string Version, string AssetName, Uri Download, ExtensionUpdateSource Source);

/// <summary>User-requested GitHub discovery and download; installation remains owned by ExtensionStore.</summary>
public sealed class ExtensionUpdateService
{
    private readonly ExtensionStore _store;
    private readonly HttpClient? _http;
    public ExtensionUpdateService(ExtensionStore store) => _store = store;
    public ExtensionUpdateService(ExtensionStore store, HttpClient http) { _store = store; _http = http; }

    public static ExtensionUpdateSource? SourceFor(ExtensionManifest manifest) => manifest.Updates ??
        (manifest.Id == "dev.hit.chess" ? new("RosaTheFlower/HoldItTogether", "chess-v") : null);

    public async Task<ExtensionUpdate?> CheckAsync(InstalledExtension package, CancellationToken token)
    {
        var source = SourceFor(package.Manifest);
        if (source is null || package.PendingDelete) return null;
        source.Validate();
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        using var owned = _http is null ? new HttpClient() : null;
        var client = _http ?? owned!;
        using var request = new HttpRequestMessage(HttpMethod.Get, $"https://api.github.com/repos/{source.Repository}/releases?per_page=20");
        request.Headers.UserAgent.ParseAdd("HoldItTogether/2");
        using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
        using var buffer = new MemoryStream();
        await CopyLimitedAsync(stream, buffer, 2 * 1024 * 1024, timeout.Token);
        using var json = JsonDocument.Parse(buffer.ToArray());
        ExtensionUpdate? best = null;
        foreach (var release in json.RootElement.EnumerateArray().Take(20))
        {
            if (release.GetProperty("draft").GetBoolean() || release.GetProperty("prerelease").GetBoolean()) continue;
            foreach (var asset in release.GetProperty("assets").EnumerateArray().Take(100))
            {
                var name = asset.GetProperty("name").GetString() ?? "";
                if (!name.StartsWith(source.AssetPrefix, StringComparison.Ordinal) || !name.EndsWith(".hitmenu", StringComparison.Ordinal)) continue;
                var version = name[source.AssetPrefix.Length..^8];
                if (!System.Version.TryParse(version, out var parsed) || parsed.Build < 0 || parsed <= System.Version.Parse(package.Manifest.Version) ||
                    (best is not null && parsed <= System.Version.Parse(best.Version))) continue;
                if (!Uri.TryCreate(asset.GetProperty("browser_download_url").GetString(), UriKind.Absolute, out var uri) || !ValidDownload(uri, source)) continue;
                if (asset.GetProperty("size").GetInt64() is <= 0 or > ExtensionStore.MaxPackageBytes) continue;
                best = new(package.Manifest.Id, package.Manifest.Version, version, name, uri, source);
            }
        }
        return best;
    }

    public async Task<InstalledExtension> InstallAsync(ExtensionUpdate update, CancellationToken token)
    {
        update.Source.Validate();
        var current = _store.State.Packages.FirstOrDefault(p => p.Manifest.Id == update.PackageId && !p.PendingDelete);
        if (current is null || current.Manifest.Version != update.InstalledVersion || SourceFor(current.Manifest) != update.Source || !ValidDownload(update.Download, update.Source))
            throw new InvalidOperationException("The extension changed. Check for updates again.");
        if (!System.Version.TryParse(update.Version, out var targetVersion) || targetVersion <= System.Version.Parse(current.Manifest.Version) ||
            update.AssetName != update.Source.AssetPrefix + update.Version + ".hitmenu")
            throw new InvalidDataException("Invalid extension update version.");
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(token);
        timeout.CancelAfter(TimeSpan.FromMinutes(2));
        using var owned = _http is null ? new HttpClient() : null;
        using var request = new HttpRequestMessage(HttpMethod.Get, update.Download);
        request.Headers.UserAgent.ParseAdd("HoldItTogether/2");
        using var response = await (_http ?? owned!).SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        response.EnsureSuccessStatusCode();
        var temporary = Path.GetTempFileName();
        try
        {
            await using var file = new FileStream(temporary, FileMode.Open, FileAccess.ReadWrite, FileShare.None, 8192, FileOptions.Asynchronous);
            await using (var input = await response.Content.ReadAsStreamAsync(timeout.Token))
                await CopyLimitedAsync(input, file, ExtensionStore.MaxPackageBytes, timeout.Token);
            file.Position = 0;
            using (var archive = new ZipArchive(file, ZipArchiveMode.Read, true))
            {
                var entry = archive.GetEntry("manifest.json") ?? throw new InvalidDataException("Missing update manifest.");
                if (entry.Length > 64 * 1024) throw new InvalidDataException("Update manifest is too large.");
                await using var input = entry.Open();
                var manifest = await JsonSerializer.DeserializeAsync<ExtensionManifest>(input, ExtensionStore.Json, timeout.Token) ?? throw new InvalidDataException("Invalid update manifest.");
                manifest.Validate();
                if (manifest.Id != update.PackageId || manifest.Version != update.Version || manifest.Capabilities.Except(current.Manifest.Capabilities).Any() ||
                    (manifest.Updates is not null && manifest.Updates != update.Source))
                    throw new InvalidDataException("Update identity, permissions or source changed. Import this package manually to review it.");
            }
            file.Position = 0;
            return await _store.InstallAsync(file, update.AssetName, timeout.Token);
        }
        finally { File.Delete(temporary); }
    }

    private static bool ValidDownload(Uri uri, ExtensionUpdateSource source) => uri.Scheme == "https" && uri.Host == "github.com" && uri.IsDefaultPort &&
        string.IsNullOrEmpty(uri.UserInfo) && uri.AbsolutePath.StartsWith($"/{source.Repository}/releases/download/", StringComparison.Ordinal);

    private static async Task CopyLimitedAsync(Stream input, Stream output, long limit, CancellationToken token)
    {
        var bytes = new byte[8192]; long total = 0; int count;
        while ((count = await input.ReadAsync(bytes, token)) > 0)
        {
            total += count;
            if (total > limit) throw new InvalidDataException("The update download exceeds its size limit.");
            await output.WriteAsync(bytes.AsMemory(0, count), token);
        }
    }
}
