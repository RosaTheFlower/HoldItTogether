using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HoldItTogether.Services.Audio;

public sealed record LyricLine(double Time, string Text);
public readonly record struct LyricSeekRequest(long Generation, double Position);
public sealed record SyncedLyrics(IReadOnlyList<LyricLine> Lines, string Source = "LRCLIB", string? SourceUrl = null)
{
    public int CurrentIndex(double position)
    {
        var low = 0; var high = Lines.Count - 1; var result = -1;
        while (low <= high)
        {
            var middle = (low + high) / 2;
            if (Lines[middle].Time <= position) { result = middle; low = middle + 1; }
            else high = middle - 1;
        }
        return result;
    }
}

/// <summary>Sequential timed-lyrics providers, bounded responses and cache. Never fabricates timestamps.</summary>
public sealed class LyricsService : IDisposable
{
    public const int MaxCacheEntries = 40;
    public const long MaxCacheBytes = 4 * 1024 * 1024;
    private readonly HttpClient _http;
    private readonly Dictionary<string, CacheEntry> _cache = new();
    private sealed record CacheEntry(DateTimeOffset At, SyncedLyrics? Lyrics, long Bytes);
    private long _cacheBytes;
    private readonly Queue<DateTimeOffset> _syncLrcRequests = new();
    private readonly object _sync = new();
    private static readonly Regex Timestamps = new(@"\[(?<m>\d{1,4}):(?<s>\d{2}(?:\.\d{1,3})?)\]", RegexOptions.NonBacktracking);
    public LyricsService() : this(new HttpClient()) { }
    public LyricsService(HttpClient http)
    {
        _http = http;
        _http.Timeout = Timeout.InfiniteTimeSpan;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("HoldItTogether/1.0 (+https://github.com/RosaTheFlower/HoldItTogether)");
    }

    public async Task<SyncedLyrics?> FindAsync(MediaSnapshot media, CancellationToken token)
    {
        if (!media.HasMedia || media.Duration <= 0 || media.Artist is "" or "Mídia do sistema" || media.Title == "Áudio em reprodução") return null;
        var title = CleanTitle(media.Title, media.Artist);
        var key = title + "\n" + media.Artist + "\n" + Math.Round(media.Duration);
        lock (_sync)
        {
            PruneCache(DateTimeOffset.UtcNow);
            if (_cache.TryGetValue(key, out var cached) && DateTimeOffset.UtcNow - cached.At < TimeSpan.FromMinutes(cached.Lyrics is null ? 2 : 60))
                return cached.Lyrics;
        }
        var query = "track_name=" + Uri.EscapeDataString(title) + "&artist_name=" + Uri.EscapeDataString(media.Artist);
        var exactQuery = query + "&duration=" + Math.Round(media.Duration).ToString(CultureInfo.InvariantCulture);
        if (!string.IsNullOrWhiteSpace(media.Album)) exactQuery += "&album_name=" + Uri.EscapeDataString(media.Album);
        SyncedLyrics? found;
        var exactUrl = "https://lrclib.net/api/get?" + exactQuery;
        using (var exact = await GetJsonAsync(exactUrl, token))
            found = Timed(exact?.RootElement, "syncedLyrics", "LRCLIB", LrcLibUrl(exact?.RootElement, exactUrl));

        if (found is null)
        {
            using var search = await GetJsonAsync("https://lrclib.net/api/search?" + query, token);
            if (search?.RootElement.ValueKind == JsonValueKind.Array)
                foreach (var result in search.RootElement.EnumerateArray()
                    .Where(item => Matches(media, title, Text(item,"trackName"), Text(item,"artistName"), Numeric(item,"duration")))
                    .OrderBy(item => Math.Abs(Numeric(item,"duration") - media.Duration)))
                {
                    found = Timed(result, "syncedLyrics", "LRCLIB", LrcLibUrl(result, "https://lrclib.net/api/search?" + query));
                    if (found is not null) break;
                }
        }
        if (found is null && AllowSyncLrc())
        {
            var url = "https://api.synclrc.dev/lyrics?track=" + Uri.EscapeDataString(title) + "&artist=" +
                Uri.EscapeDataString(media.Artist) + "&type=synced&format=lrc&duration=" +
                Math.Round(media.Duration).ToString(CultureInfo.InvariantCulture);
            using var result = await GetJsonAsync(url, token);
            if (result is not null && Matches(media, title, Text(result.RootElement,"track"), Text(result.RootElement,"artist"), Numeric(result.RootElement,"duration")))
                found = Timed(result.RootElement, "lyrics", "SyncLRC", url) ?? Timed(result.RootElement, "synced", "SyncLRC", url);
        }
        if (found is null)
        {
            using var search = await GetJsonAsync("https://music.163.com/api/search/get?type=1&limit=8&s=" +
                Uri.EscapeDataString(media.Artist + " " + title), token);
            if (search is not null && search.RootElement.TryGetProperty("result", out var result) &&
                result.ValueKind == JsonValueKind.Object && result.TryGetProperty("songs", out var songs) && songs.ValueKind == JsonValueKind.Array)
            {
                var candidates = songs.EnumerateArray().Where(song =>
                {
                    var artists = song.TryGetProperty("artists", out var array) && array.ValueKind == JsonValueKind.Array
                        ? string.Join(", ", array.EnumerateArray().Select(artist => Text(artist,"name"))) : "";
                    return Matches(media, title, Text(song,"name"), artists, Numeric(song,"duration") / 1000);
                }).OrderBy(song => Math.Abs(Numeric(song,"duration") / 1000 - media.Duration)).Take(2);
                foreach (var song in candidates)
                {
                    if (!song.TryGetProperty("id", out var id) || !id.TryGetInt64(out var songId)) continue;
                    using var response = await GetJsonAsync("https://music.163.com/api/song/lyric?id=" + songId + "&lv=1&kv=1&tv=-1", token);
                    if (response is not null && response.RootElement.TryGetProperty("lrc", out var lrc))
                        found = Timed(lrc, "lyric", "NetEase Music", "https://music.163.com/#/song?id=" + songId);
                    if (found is not null) break;
                }
            }
        }
        token.ThrowIfCancellationRequested();
        lock (_sync)
        {
            PruneCache(DateTimeOffset.UtcNow);
            RemoveCached(key);
            var bytes = EstimateBytes(key, found);
            if (bytes <= MaxCacheBytes)
            {
                while (_cache.Count >= MaxCacheEntries || _cacheBytes + bytes > MaxCacheBytes)
                    RemoveCached(_cache.Keys.First());
                _cache[key] = new(DateTimeOffset.UtcNow, found, bytes);
                _cacheBytes += bytes;
            }
        }
        return found;
    }

    private static long EstimateBytes(string key, SyncedLyrics? lyrics) =>
        160L + key.Length * 2L + (lyrics is null ? 0 :
            (lyrics.Source.Length + (lyrics.SourceUrl?.Length ?? 0)) * 2L +
            lyrics.Lines.Sum(line => 80L + line.Text.Length * 2L));

    private void RemoveCached(string key)
    {
        if (_cache.Remove(key, out var entry)) _cacheBytes -= entry.Bytes;
    }

    private void PruneCache(DateTimeOffset now)
    {
        foreach (var key in _cache.Where(item => now - item.Value.At >= TimeSpan.FromMinutes(item.Value.Lyrics is null ? 2 : 60))
                     .Select(item => item.Key).ToArray()) RemoveCached(key);
    }

    private bool AllowSyncLrc()
    {
        lock (_sync)
        {
            while (_syncLrcRequests.TryPeek(out var at) && DateTimeOffset.UtcNow - at > TimeSpan.FromMinutes(1)) _syncLrcRequests.Dequeue();
            if (_syncLrcRequests.Count >= 25) return false;
            _syncLrcRequests.Enqueue(DateTimeOffset.UtcNow);
            return true;
        }
    }

    private async Task<JsonDocument?> GetJsonAsync(string url, CancellationToken token)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(token);
        deadline.CancelAfter(TimeSpan.FromSeconds(6));
        try
        {
            using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, deadline.Token);
            if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > 1024 * 1024) return null;
            await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token);
            using var buffer = new MemoryStream();
            var bytes = new byte[8192]; int count;
            while ((count = await stream.ReadAsync(bytes, deadline.Token)) > 0)
            {
                if (buffer.Length + count > 1024 * 1024) return null;
                buffer.Write(bytes, 0, count);
            }
            return JsonDocument.Parse(buffer.GetBuffer().AsMemory(0, checked((int)buffer.Length)));
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException or IOException) { return null; }
    }

    private static string LrcLibUrl(JsonElement? record, string fallback) => record is { ValueKind: JsonValueKind.Object } element &&
        element.TryGetProperty("id", out var id) && id.ValueKind == JsonValueKind.Number && id.TryGetInt64(out var value) && value > 0
            ? "https://lrclib.net/api/get/" + value : fallback;

    private static SyncedLyrics? Timed(JsonElement? record, string property, string source, string sourceUrl)
    {
        if (record is not { ValueKind: JsonValueKind.Object } element) return null;
        var parsed = Parse(Text(element, property));
        return parsed is null ? null : parsed with { Source = source, SourceUrl = sourceUrl };
    }
    private static string Text(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() ?? "" : "";
    private static double Numeric(JsonElement element, string property) =>
        element.ValueKind == JsonValueKind.Object && element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetDouble(out var number) ? number : 0;
    private static bool Matches(MediaSnapshot expected, string title, string candidate, string artist, double duration)
    {
        var left = Normalize(title);
        var right = Normalize(CleanTitle(candidate, expected.Artist));
        var artistWords = " " + Normalize(artist) + " ";
        var expectedArtist = Normalize(Regex.Split(expected.Artist, @"[,;&]| feat\.? | featuring ", RegexOptions.IgnoreCase)[0]);
        return left.Length > 0 && left == right && expectedArtist.Length > 0 && artistWords.Contains(" " + expectedArtist + " ", StringComparison.Ordinal) &&
            duration > 0 && Math.Abs(duration - expected.Duration) <= Math.Max(3, Math.Min(5, expected.Duration * .01));
    }
    private static string Normalize(string value)
    {
        var builder = new StringBuilder();
        foreach (var character in value.Normalize(NormalizationForm.FormD))
            if (char.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                builder.Append(char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : ' ');
        return Regex.Replace(builder.ToString(), @"\s+", " ").Trim();
    }
    private static string CleanTitle(string title, string artist)
    {
        var value = Regex.Replace(title, @"\s*[\[(](?:official(?: music)? video|official audio|lyrics?|lyric video|visuali[sz]er|audio|hd|4k)[^\])]*[\])]", "", RegexOptions.IgnoreCase);
        foreach (var separator in new[] { " - ", " — ", " – " })
            if (value.StartsWith(artist + separator, StringComparison.OrdinalIgnoreCase)) value = value[(artist.Length + separator.Length)..];
        return value.Trim();
    }
    public static SyncedLyrics? Parse(string text)
    {
        var lines = new List<LyricLine>();
        var offsetMatch = Regex.Match(text, @"\[offset:([+-]?\d+)\]", RegexOptions.IgnoreCase | RegexOptions.NonBacktracking);
        var offset = offsetMatch.Success && int.TryParse(offsetMatch.Groups[1].Value, out var milliseconds) ? milliseconds / 1000d : 0;
        using var rows = new StringReader(text);
        for (var rowIndex = 0; rowIndex < 3000 && rows.ReadLine() is { } row; rowIndex++)
        {
            if (lines.Count >= 5000) break;
            var timestamps = Timestamps.Matches(row);
            if (timestamps.Count == 0) continue;
            var content = Timestamps.Replace(row, "").Trim();
            foreach (Match timestamp in timestamps)
            {
                if (lines.Count >= 5000) break;
                var seconds = double.Parse(timestamp.Groups["s"].Value, CultureInfo.InvariantCulture);
                if (seconds >= 60) continue;
                var time = int.Parse(timestamp.Groups["m"].Value, CultureInfo.InvariantCulture) * 60 + seconds - offset;
                lines.Add(new(Math.Max(0, time), content));
            }
        }
        return lines.Any(line => !string.IsNullOrWhiteSpace(line.Text))
            ? new(lines.OrderBy(line => line.Time).Distinct().ToArray()) : null;
    }

    public void Dispose()
    {
        lock (_sync) { _cache.Clear(); _cacheBytes = 0; _syncLrcRequests.Clear(); }
        _http.Dispose();
    }
}
