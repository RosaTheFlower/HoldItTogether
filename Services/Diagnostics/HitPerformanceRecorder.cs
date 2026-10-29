using System.Text.Json;
namespace HoldItTogether.Services.Diagnostics;

internal static class HitPerformanceRecorder
{
    public static async Task<int> RunAsync(string[] args)
    {
        if(args.Length!=5||!int.TryParse(args[1],out var id)||id<=0||!int.TryParse(args[2],out var seconds)||seconds is <2 or >600)
        {Console.Error.WriteLine("Usage: --hit-profile <PID> <seconds:2-600> <scenario> <output.json>");return 2;}
        var sampler=new HitPerformanceSampler();
        var samples=new List<HitPerformanceSample>();
        long? identity=null;
        for(var tick=0;tick<=seconds;tick++)
        {
            var sample=sampler.Sample(id);
            var host=sample.Processes.FirstOrDefault(p=>p.IsHost);
            if(host is null||(identity is not null&&identity!=host.StartedUtcTicks))break;
            identity??=host.StartedUtcTicks;
            samples.Add(sample);
            if(tick<seconds)await Task.Delay(1000);
        }
        var path=Path.GetFullPath(args[4]);Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,JsonSerializer.Serialize(new{Version=HitUpdateService.VersionLabel,Scenario=args[3][..Math.Min(80,args[3].Length)],
            LogicalProcessors=Environment.ProcessorCount,RequestedSeconds=seconds,Complete=samples.Count==seconds+1,Samples=samples},new JsonSerializerOptions{WriteIndented=true}));
        Console.WriteLine(path);
        return samples.Count==0?1:0;
    }
}
