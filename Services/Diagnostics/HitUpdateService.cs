using System.Net;
using System.Text.Json;
using System.Globalization;
using System.Reflection;
using System.Text.RegularExpressions;

namespace HoldItTogether.Services.Diagnostics;

public sealed record HitUpdateResult(string Message, string? Url = null);
public sealed record HitReleaseNotes(string Version,string Name,string Body);
public sealed class HitUpdateService
{
    private readonly HttpClient? _http;
    public HitUpdateService() { }
    public HitUpdateService(HttpClient http) => _http=http;
    public async Task<HitReleaseNotes> ReadChangelogAsync(CancellationToken token)
    {
        using var owned=_http is null?new HttpClient{Timeout=TimeSpan.FromSeconds(8)}:null;
        var client=_http??owned!;
        using var request=new HttpRequestMessage(HttpMethod.Get,"https://api.github.com/repos/RosaTheFlower/HoldItTogether/releases/latest");
        request.Headers.UserAgent.ParseAdd("HoldItTogether/"+VersionLabel);
        using var response=await client.SendAsync(request,HttpCompletionOption.ResponseHeadersRead,token);
        response.EnsureSuccessStatusCode();
        await using var source=await response.Content.ReadAsStreamAsync(token);
        using var buffer=new MemoryStream();var block=new byte[8192];int count;
        while((count=await source.ReadAsync(block,token))>0){if(buffer.Length+count>1024*1024)throw new InvalidDataException("Release notes response exceeds 1 MB.");await buffer.WriteAsync(block.AsMemory(0,count),token);}
        using var json=JsonDocument.Parse(buffer.ToArray());var root=json.RootElement;
        string Read(string key)=>root.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.String?value.GetString()??"":"";
        return new(Read("tag_name"),Read("name"),Read("body"));
    }
    public const string RepositoryUrl = "https://github.com/RosaTheFlower/HoldItTogether";
    public static string VersionLabel => typeof(HitUpdateService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "2v2910";
    public static int ReleaseGeneration => int.Parse(typeof(HitUpdateService).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(value=>value.Key=="HitReleaseGeneration").Value!,CultureInfo.InvariantCulture);
    public static DateOnly ReleaseDate => DateOnly.ParseExact(typeof(HitUpdateService).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(value=>value.Key=="HitReleaseDate").Value!,"yyyy-MM-dd",CultureInfo.InvariantCulture);

    internal static bool IsNewerRelease(string? tag,DateTimeOffset? publishedAt,DateOnly current,int currentGeneration=1)
    {
        if(tag is null||tag.Length>32)return false;
        var match=Regex.Match(tag.Trim(),@"^(?:(?<generation>[1-9][0-9]*)?[vV])(?<date>[0-9]{4})$",RegexOptions.CultureInvariant);
        if(!match.Success)return false;
        var generation=1;
        if(match.Groups["generation"].Success&&!int.TryParse(match.Groups["generation"].Value,out generation))return false;
        var value=match.Groups["date"].Value;
        var day=int.Parse(value[..2],CultureInfo.InvariantCulture);var month=int.Parse(value[2..],CultureInfo.InvariantCulture);
        var year=publishedAt?.Year??current.Year;
        return month is >=1 and <=12 && day>=1 && day<=DateTime.DaysInMonth(year,month) &&
            (generation>currentGeneration || generation==currentGeneration && new DateOnly(year,month,day)>current);
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
        return IsNewerRelease(tag,publishedAt,ReleaseDate,ReleaseGeneration)
            ? new($"HIT {tag} is available.", RepositoryUrl + "/releases/latest")
            : new("You are using the latest published version.");
    }
}
