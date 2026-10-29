using System.Text.Json;
namespace HoldItTogether.Services.Diagnostics;

public sealed record HitDiagnosticEntry(DateTimeOffset At,string Area,string Code,int NativeError,int Count=1);
/// <summary>Bounded structured events; no media titles, URLs, payloads or exception messages.</summary>
public sealed class HitDiagnosticLog
{
    public const int Capacity=128;
    private readonly List<HitDiagnosticEntry> _entries=[];
    private readonly object _sync=new();
    public void Record(string area,string code,int nativeError=0)
    {
        lock(_sync)
        {
            var last=_entries.LastOrDefault();
            if(last is not null&&last.Area==area&&last.Code==code&&last.NativeError==nativeError)
            {_entries[^1]=last with{At=DateTimeOffset.UtcNow,Count=Math.Min(int.MaxValue-1,last.Count)+1};return;}
            if(_entries.Count==Capacity)_entries.RemoveAt(0);
            _entries.Add(new(DateTimeOffset.UtcNow,area[..Math.Min(32,area.Length)],code[..Math.Min(80,code.Length)],nativeError));
        }
    }
    public HitDiagnosticEntry[] Snapshot(){lock(_sync)return _entries.ToArray();}
    public string Export(HitPerformanceSample performance,bool sleeping,int activeCaptures,string? directory=null)
    {
        directory??=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HoldItTogether","Diagnostics");
        Directory.CreateDirectory(directory);
        var path=Path.Combine(directory,$"hit-{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}.json");
        File.WriteAllText(path,JsonSerializer.Serialize(new{Version=HitUpdateService.VersionLabel,performance,sleeping,activeCaptures,Events=Snapshot()},new JsonSerializerOptions{WriteIndented=true}));
        return path;
    }
}
