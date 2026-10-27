using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Reflection;

namespace HoldItTogether.Services.Diagnostics;

public sealed record HitUpdateResult(string Message, string? Url = null);
public sealed class HitUpdateService
{
    public const string RepositoryUrl = "https://github.com/RosaTheFlower/HoldItTogether";
    public static string VersionLabel => typeof(HitUpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "v2710";
    public static DateOnly ReleaseDate => DateOnly.ParseExact(typeof(HitUpdateService).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(value=>value.Key=="HitReleaseDate").Value!,"yyyy-MM-dd",CultureInfo.InvariantCulture);

    internal static bool IsNewerRelease(string? tag,DateTimeOffset? publishedAt,DateOnly current)
    {
        if(tag is null)return false;
        var value=tag.Trim().TrimStart('v','V');
        if(value.Length!=4||!value.All(char.IsAsciiDigit)||!int.TryParse(value[..2],out var day)||!int.TryParse(value[2..],out var month))return false;
        var year=publishedAt?.Year??current.Year;
        return month is >=1 and <=12 && day>=1 && day<=DateTime.DaysInMonth(year,month) && new DateOnly(year,month,day)>current;
    }
    public async Task<HitUpdateResult> CheckAsync(CancellationToken token)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(8) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HoldItTogether/" + VersionLabel);
        using var response = await client.GetAsync("https://api.github.com/repos/RosaTheFlower/HoldItTogether/releases/latest", HttpCompletionOption.ResponseHeadersRead, token);
        if (response.StatusCode == HttpStatusCode.NotFound) return new("No published release is available yet.");
        response.EnsureSuccessStatusCode();
        await using var source = await response.Content.ReadAsStreamAsync(token);
        using var buffer = new MemoryStream();
        var bytes = new byte[8192];
        int read;
        while ((read = await source.ReadAsync(bytes, token)) > 0)
        {
            if (buffer.Length + read > 1024 * 1024) throw new IOException("The update response is too large.");
            buffer.Write(bytes, 0, read);
        }
        using var json = JsonDocument.Parse(buffer.ToArray());
        var tag = json.RootElement.GetProperty("tag_name").GetString();
        var publishedAt=json.RootElement.TryGetProperty("published_at",out var published)&&published.ValueKind==JsonValueKind.String&&
            DateTimeOffset.TryParse(published.GetString(),CultureInfo.InvariantCulture,DateTimeStyles.AssumeUniversal,out var date)?date:(DateTimeOffset?)null;
        return IsNewerRelease(tag,publishedAt,ReleaseDate)
            ? new($"HIT {tag} is available.", RepositoryUrl + "/releases/latest")
            : new("You are using the latest published version.");
    }
}
