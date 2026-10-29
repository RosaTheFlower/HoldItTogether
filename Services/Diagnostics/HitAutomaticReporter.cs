using System.Text.Json;
using HoldItTogether.Services.Audio;

namespace HoldItTogether.Services.Diagnostics;

/// <summary>Low-frequency local reports with bounded retention and no renderer dependency.</summary>
public sealed class HitAutomaticReporter : IAsyncDisposable
{
    public const int RetainedReports=30;
    private readonly HitDiagnosticLog _log;
    private readonly HitResourcePolicy _resources;
    private readonly AudioSpectrumService _spectrum;
    private readonly HitPerformanceSampler _sampler=new(); // Independent CPU baseline from Info.
    private readonly string _directory;
    private readonly TimeSpan _interval;
    private readonly CancellationTokenSource _lifetime=new();
    private Task? _worker;
    private bool _disposed;

    public HitAutomaticReporter(HitDiagnosticLog log,HitResourcePolicy resources,AudioSpectrumService spectrum)
        :this(log,resources,spectrum,TimeSpan.FromMinutes(1),Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HoldItTogether","Diagnostics","Automatic")){}

    internal HitAutomaticReporter(HitDiagnosticLog log,HitResourcePolicy resources,AudioSpectrumService spectrum,TimeSpan interval,string directory)
    {_log=log;_resources=resources;_spectrum=spectrum;_interval=interval;_directory=directory;}

    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed,this);
        _worker??=Task.Run(()=>RunAsync(_lifetime.Token));
    }

    private async Task RunAsync(CancellationToken token)
    {
        _sampler.Sample(allowLongInterval:true); // CPU baseline only; first file is written after one interval.
        using var timer=new PeriodicTimer(_interval);
        try
        {
            while(await timer.WaitForNextTickAsync(token))
            {
                try{WriteReport(_sampler.Sample(allowLongInterval:true));}
                catch(Exception error)when(error is IOException or UnauthorizedAccessException)
                {_log.Record("diagnostics","automatic.write.failed");}
            }
        }
        catch(OperationCanceledException)when(token.IsCancellationRequested){}
    }

    internal string WriteReport(HitPerformanceSample sample)
    {
        Directory.CreateDirectory(_directory);
        var slots=Enumerable.Range(0,RetainedReports).Select(i=>Path.Combine(_directory,$"auto-{i:00}.json")).ToArray();
        var target=slots.FirstOrDefault(path=>!File.Exists(path))??slots.MinBy(File.GetLastWriteTimeUtc)!;
        var report=new{Version=HitUpdateService.VersionLabel,ReportType="automatic",IntervalSeconds=_interval.TotalSeconds,
            CpuMeasurement="Average over the interval between automatic samples",performance=sample,
            sleeping=_resources.IsSleeping,activeCaptures=_spectrum.ActiveCaptures,Events=_log.Snapshot()};
        File.WriteAllText(target+".tmp",JsonSerializer.Serialize(report,new JsonSerializerOptions{WriteIndented=true}));
        File.Move(target+".tmp",target,true);
        return target;
    }

    public async ValueTask DisposeAsync()
    {
        if(_disposed)return;
        _disposed=true;_lifetime.Cancel();
        try{if(_worker is not null)await _worker;}
        finally{_lifetime.Dispose();}
    }
}
