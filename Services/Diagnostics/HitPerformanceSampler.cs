using System.Diagnostics;
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HoldItTogether.Services.Diagnostics;

public sealed record HitProcessSample(int Id, long StartedUtcTicks, bool IsHost, long WorkingSetBytes,
    long? PrivateWorkingSetBytes, double CpuSeconds, int Handles);
public sealed record HitPerformanceSample(DateTimeOffset At, HitProcessSample[] Processes, bool Partial,
    double? CpuPercent)
{
    public long HostBytes => Processes.Where(p=>p.IsHost).Sum(p=>IsPrivateWorkingSet?p.PrivateWorkingSetBytes!.Value:p.WorkingSetBytes);
    public bool IsPrivateWorkingSet => Processes.Length>0&&Processes.All(p=>p.PrivateWorkingSetBytes.HasValue);
    public long TotalBytes => Processes.Sum(p=>IsPrivateWorkingSet?p.PrivateWorkingSetBytes!.Value:p.WorkingSetBytes);
    public long WebViewBytes => Processes.Where(p=>!p.IsHost).Sum(p=>IsPrivateWorkingSet?p.PrivateWorkingSetBytes!.Value:p.WorkingSetBytes);
}

/// <summary>On-demand host/WebView2 sampling. No resident polling worker.</summary>
public sealed class HitPerformanceSampler
{
    private HitPerformanceSample? _previous;
    private long _previousTimestamp;
    private readonly object _sync=new();

    public HitPerformanceSample Sample(int? hostId=null,bool allowLongInterval=false)
    {
        lock(_sync)
        {
            var root=hostId??Environment.ProcessId;
            var partial=false;
            var tree=ReadTree(ref partial);
            var ids=SelectProcesses(root,tree);
            var samples=new List<HitProcessSample>();
            foreach(var id in ids)
            {
                try
                {
                    using var process=Process.GetProcessById(id);
                    process.Refresh();
                    var counters=new Counters{Size=(uint)Marshal.SizeOf<Counters>()};
                    long? resident=OperatingSystem.IsWindows()&&GetProcessMemoryInfo(process.Handle,ref counters,counters.Size)&&counters.PrivateWorkingSet>0
                        ? (long)counters.PrivateWorkingSet:null;
                    samples.Add(new(id,process.StartTime.ToUniversalTime().Ticks,id==root,process.WorkingSet64,
                        resident,process.TotalProcessorTime.TotalSeconds,process.HandleCount));
                }
                catch(Exception error)when(error is ArgumentException or InvalidOperationException or Win32Exception or NotSupportedException)
                {partial=true;}
            }
            var host=samples.FirstOrDefault(p=>p.IsHost);
            // Reject stale descendants left over from a reused root PID.
            if(host is not null)samples.RemoveAll(p=>!p.IsHost&&p.StartedUtcTicks<host.StartedUtcTicks);
            var now=Stopwatch.GetTimestamp();
            var result=new HitPerformanceSample(DateTimeOffset.UtcNow,samples.ToArray(),partial,null);
            if(_previous is { } previous&&now>_previousTimestamp&&(allowLongInterval||Stopwatch.GetElapsedTime(_previousTimestamp,now).TotalSeconds<=5))
                result=result with{CpuPercent=CpuDelta(previous.Processes,result.Processes,
                    Stopwatch.GetElapsedTime(_previousTimestamp,now).TotalSeconds,Environment.ProcessorCount)};
            _previous=result;_previousTimestamp=now;
            return result;
        }
    }

    internal static double? CpuDelta(HitProcessSample[] previous,HitProcessSample[] current,double seconds,int processors)
    {
        if(seconds<=0||processors<=0)return null;
        var matched=current.Select(p=>(Current:p,Before:previous.FirstOrDefault(old=>old.Id==p.Id&&old.StartedUtcTicks==p.StartedUtcTicks)))
            .Where(pair=>pair.Before is not null).ToArray();
        return matched.Length==0?null:Math.Clamp(matched.Sum(pair=>Math.Max(0,pair.Current.CpuSeconds-pair.Before!.CpuSeconds))/seconds/processors*100,0,100);
    }

    internal sealed record ProcessNode(int Id,int Parent,string Name);
    internal static int[] SelectProcesses(int root,IReadOnlyList<ProcessNode> tree)
    {
        var descendants=new HashSet<int>{root};
        bool changed;
        do{changed=false;foreach(var entry in tree)if(descendants.Contains(entry.Parent))changed|=descendants.Add(entry.Id);}while(changed);
        return tree.Where(p=>p.Id!=root&&descendants.Contains(p.Id)&&p.Name.Equals("msedgewebview2.exe",StringComparison.OrdinalIgnoreCase))
            .Select(p=>p.Id).Prepend(root).Distinct().ToArray();
    }
    private static List<ProcessNode> ReadTree(ref bool partial)
    {
        var list=new List<ProcessNode>();
        if(!OperatingSystem.IsWindows()){partial=true;return list;}
        var snapshot=CreateToolhelp32Snapshot(2,0);
        if(snapshot==new nint(-1)){partial=true;return list;}
        try
        {
            var entry=new ProcessEntry{Size=(uint)Marshal.SizeOf<ProcessEntry>(),Name=""};
            if(!Process32First(snapshot,ref entry)){partial=true;return list;}
            do{list.Add(new((int)entry.Id,(int)entry.Parent,entry.Name));}while(Process32Next(snapshot,ref entry));
            if(Marshal.GetLastWin32Error()!=18)partial=true; // ERROR_NO_MORE_FILES
        }
        finally{CloseHandle(snapshot);}
        return list;
    }
    [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)]
    private struct ProcessEntry{public uint Size,Usage,Id;public nuint Heap;public uint Module,Threads,Parent;public int Priority;public uint Flags;[MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)]public string Name;}
    [StructLayout(LayoutKind.Sequential)]
    private struct Counters{public uint Size,Faults;public nuint PeakWorkingSet,WorkingSet,PeakPaged,Paged,PeakNonPaged,NonPaged,Pagefile,PeakPagefile,PrivateUsage,PrivateWorkingSet;public ulong SharedCommit;}
    [DllImport("kernel32.dll",SetLastError=true)]private static extern nint CreateToolhelp32Snapshot(uint flags,uint process);
    [DllImport("kernel32.dll",EntryPoint="Process32FirstW",SetLastError=true)]private static extern bool Process32First(nint snapshot,ref ProcessEntry entry);
    [DllImport("kernel32.dll",EntryPoint="Process32NextW",SetLastError=true)]private static extern bool Process32Next(nint snapshot,ref ProcessEntry entry);
    [DllImport("kernel32.dll")]private static extern bool CloseHandle(nint handle);
    [DllImport("psapi.dll",SetLastError=true)]private static extern bool GetProcessMemoryInfo(nint process,ref Counters counters,uint size);
}
