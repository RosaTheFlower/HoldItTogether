using HoldItTogether.Services.Diagnostics;
using System.Diagnostics;
using System.Text.Json;

internal static class DiagnosticChecks
{
    public static async Task RunAutomaticAsync()
    {
        var directory=Path.GetFullPath("obj/automatic-report-tests/"+Guid.NewGuid().ToString("N"));
        var log=new HitDiagnosticLog();var resources=new HitResourcePolicy();
        using var spectrum=new HoldItTogether.Services.Audio.AudioSpectrumService();
        await using(var reporter=new HitAutomaticReporter(log,resources,spectrum,TimeSpan.FromMilliseconds(40),directory))
        {
            var sample=new HitPerformanceSampler().Sample();
            for(var i=0;i<35;i++)reporter.WriteReport(sample with{At=sample.At.AddMinutes(i)});
            Check(Directory.GetFiles(directory,"auto-*.json").Length==30,"Automatic reports must rotate within 30 fixed slots.");
            Check(Directory.GetFiles(directory,"*.tmp").Length==0,"Completed automatic writes must leave no partial files.");
            resources.Update(true,true,false,false,0);resources.Update(true,true,false,false,5000);
            reporter.Start();reporter.Start(); // Idempotent startup.
            await Task.Delay(200);
            await reporter.DisposeAsync();
            Check(resources.IsSleeping,"Automatic reporting must not wake the interface.");
            var paths=Directory.GetFiles(directory,"auto-*.json");
            Check(paths.Any(path=>{using var json=JsonDocument.Parse(File.ReadAllText(path));return json.RootElement.GetProperty("sleeping").GetBoolean();}),"Scheduled reports must capture actual sleep state.");
            var writes=paths.Select(File.GetLastWriteTimeUtc).ToArray();await Task.Delay(120);
            Check(paths.Select(File.GetLastWriteTimeUtc).SequenceEqual(writes),"Disposal must stop automatic writes.");
        }
        var blocked=Path.Combine(directory,"blocked");File.WriteAllText(blocked,"not a directory");
        await using(var failing=new HitAutomaticReporter(log,resources,spectrum,TimeSpan.FromMilliseconds(40),blocked))
        {failing.Start();await Task.Delay(200);}
        Check(log.Snapshot().Any(entry=>entry.Code=="automatic.write.failed"),"Automatic I/O failures must be recorded without crashing the application.");
        Console.WriteLine("PASS: scheduled automatic reports, 30-file rotation, atomic completion, sleeping preservation, shutdown and I/O failure handling.");
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var tree=new HitPerformanceSampler.ProcessNode[]{new(1,0,"HoldItTogether.exe"),new(2,1,"msedgewebview2.exe"),
            new(3,2,"msedgewebview2.exe"),new(4,0,"msedgewebview2.exe"),new(5,1,"helper.exe"),new(6,5,"msedgewebview2.exe")};
        Check(HitPerformanceSampler.SelectProcesses(1,tree).Order().SequenceEqual(new[]{1,2,3,6}),"Select only host and descendant WebView2 processes, never another application's browser.");
        var host=new HitProcessSample(1,10,true,100,60,1,10);
        var web=new HitProcessSample(2,11,false,200,90,2,5);
        var sample=new HitPerformanceSample(DateTimeOffset.UtcNow,[host,web],false,null);
        Check(sample.TotalBytes==150&&sample.HostBytes+sample.WebViewBytes==sample.TotalBytes&&sample.IsPrivateWorkingSet,"Private totals must include host and WebView2 consistently.");
        var fallback=sample with{Processes=[host,web with{PrivateWorkingSetBytes=null}]};
        Check(fallback.TotalBytes==300&&fallback.HostBytes==100&&!fallback.IsPrivateWorkingSet,"Fallback must use one working-set definition for all processes.");
        Check(HitPerformanceSampler.CpuDelta([host,web],[host with{CpuSeconds=2},web with{CpuSeconds=3}],1,4)==50,"CPU must be normalized across logical processors.");
        Check(HitPerformanceSampler.CpuDelta([host],[host with{StartedUtcTicks=12}],1,4) is null,"PID reuse must not reuse old CPU time.");
        var log=new HitDiagnosticLog();
        for(var i=0;i<500;i++)log.Record("audio","poll.failed");
        Check(log.Snapshot() is [{Count:500}],"Repeated failures must coalesce.");
        for(var i=0;i<200;i++)log.Record("window","event."+i);
        Check(log.Snapshot().Length==HitDiagnosticLog.Capacity,"Diagnostics must be bounded.");
        var directory=Path.GetFullPath("obj/diagnostic-tests/"+Guid.NewGuid().ToString("N"));
        var path=log.Export(sample,false,0,directory);
        using(var json=JsonDocument.Parse(File.ReadAllText(path)))Check(json.RootElement.GetProperty("Events").GetArrayLength()==128,"Export must include only the bounded snapshot.");
        var sampler=new HitPerformanceSampler();
        var native=sampler.Sample();
        Check(native.Processes.Any(p=>p.Id==Environment.ProcessId&&p.IsHost)&&native.TotalBytes>0,"Real native sampling must include its host.");
        var handles=Process.GetCurrentProcess().HandleCount;
        for(var i=0;i<25;i++)sampler.Sample();
        Check(Process.GetCurrentProcess().HandleCount<=handles+8,"Sampling must release process and snapshot handles.");
        Check(sampler.Sample(int.MaxValue).Partial,"Exited/inaccessible roots must yield partial data rather than crash.");
        Console.WriteLine("PASS: process ancestry isolation, memory definitions, CPU deltas/PID reuse, bounded diagnostics/export and native sampling handle ownership.");
    }
}
