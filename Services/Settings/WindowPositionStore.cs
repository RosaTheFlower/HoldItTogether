using System.Text.Json;

namespace HoldItTogether.Services.Settings;

public sealed record SavedWindowPosition(double X,double Y,double PivotX,double PivotY,int WorkLeft,int WorkTop,int WorkWidth,int WorkHeight)
{
    public bool IsValid=>double.IsFinite(X)&&double.IsFinite(Y)&&Math.Abs(X)<1000000&&Math.Abs(Y)<1000000&&
        PivotX is 0 or .5 or 1&&PivotY is 0 or .5 or 1&&WorkWidth is >0 and <100000&&WorkHeight is >0 and <100000&&
        Math.Abs((long)WorkLeft)<1000000&&Math.Abs((long)WorkTop)<1000000;
}
public sealed class WindowPositionStore
{
    private static readonly JsonSerializerOptions Json=new(){PropertyNamingPolicy=JsonNamingPolicy.CamelCase,WriteIndented=true};
    private readonly string _path;
    public WindowPositionStore():this(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HoldItTogether","window.json")){}
    public WindowPositionStore(string path)=>_path=Path.GetFullPath(path);
    private string FullscreenPath=>Path.Combine(Path.GetDirectoryName(_path)!,Path.GetFileNameWithoutExtension(_path)+"-fullscreen.json");
    public SavedWindowPosition[] LoadFullscreen()
    {
        try
        {
            return File.Exists(FullscreenPath)&&new FileInfo(FullscreenPath).Length<16384&&
                JsonSerializer.Deserialize<SavedWindowPosition[]>(File.ReadAllText(FullscreenPath),Json) is {Length:<=16} entries
                ? entries.Where(entry=>entry is {IsValid:true}).ToArray():[];
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or JsonException){return [];}
    }
    public bool SaveFullscreen(SavedWindowPosition value)
    {
        if(!value.IsValid)return false;
        var entries=LoadFullscreen().Where(entry=>entry.WorkLeft!=value.WorkLeft||entry.WorkTop!=value.WorkTop)
            .TakeLast(15).Append(value).ToArray();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            File.WriteAllText(FullscreenPath+".tmp",JsonSerializer.Serialize(entries,Json));
            File.Move(FullscreenPath+".tmp",FullscreenPath,true);return true;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){return false;}
    }
    public SavedWindowPosition? Load()
    {
        try{return File.Exists(_path)&&new FileInfo(_path).Length<16384&&JsonSerializer.Deserialize<SavedWindowPosition>(File.ReadAllText(_path),Json) is {IsValid:true} value?value:null;}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or JsonException){return null;}
    }
    public bool Save(SavedWindowPosition value)
    {
        if(!value.IsValid)return false;
        try{Directory.CreateDirectory(Path.GetDirectoryName(_path)!);File.WriteAllText(_path+".tmp",JsonSerializer.Serialize(value,Json));File.Move(_path+".tmp",_path,true);return true;}
        catch(Exception error)when(error is IOException or UnauthorizedAccessException){return false;}
    }
}
