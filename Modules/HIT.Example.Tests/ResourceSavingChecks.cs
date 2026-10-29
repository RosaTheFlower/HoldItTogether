using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Settings;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using HoldItTogether.Components.HitMenus;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;

internal static class ResourceSavingChecks
{
    public static async Task RunAudioAsync(IServiceProvider services)
    {
        var settings=services.GetRequiredService<HitSettingsService>();
        var saved=settings.Current;
        settings.Update(_=>new HitSettings()); // No real loopback capture in this test.
        var policy=services.GetRequiredService<HitResourcePolicy>();
        var media=(FakeMedia)services.GetRequiredService<IHitMediaService>();
        var clock=services.GetRequiredService<HitClockService>();
        clock.StartTimer(TimeSpan.FromMinutes(2));
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{await renderer.MountAsync<HitMenuAudio>();});
            await Task.Delay(100);
            var releases=media.Releases;
            await renderer.Dispatcher.InvokeAsync(()=>{policy.Update(true,true,false,false,0);policy.Update(true,true,false,false,5000);});
            await Task.Delay(100); // Let cancellation finish before observing the quiet period.
            var reads=media.Reads;
            await Task.Delay(650);
            Check(media.Reads==reads&&media.Releases>releases,"Sleeping Audio must stop actual media polling and release retained artwork.");
            Check(clock.Read().Timer.Running,"A sleeping interface must not stop the timer domain worker.");
            await renderer.Dispatcher.InvokeAsync(policy.Wake);
            await Task.Delay(650);
            Check(media.Reads>reads,"Waking must restart actual Audio polling.");
            for(var cycle=0;cycle<12;cycle++)
            {
                await renderer.Dispatcher.InvokeAsync(()=>{policy.Update(true,true,false,false,0);policy.Update(true,true,false,false,5000);});
                await Task.Delay(15);
                await renderer.Dispatcher.InvokeAsync(policy.Wake);
                await Task.Delay(15);
            }
            Check(renderer.Errors.Count==0,"Sleep/wake must not produce renderer/lifecycle errors.");
        }
        await Task.Delay(100);
        var disposedReads=media.Reads;
        policy.Update(true,true,false,false,0);policy.Update(true,true,false,false,5000);policy.Wake();
        await Task.Delay(650);
        Check(media.Reads==disposedReads,"Disposed Audio must not restart workers after later resource transitions.");
        clock.ResetTimer();
        settings.Update(_=>saved);
        Console.WriteLine("PASS: live component sleep cancels media polling/releases artwork, preserves timer state and resumes polling on wake.");
    }
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        var policy=new HitResourcePolicy();
        var changes=0;
        policy.Changed+=()=>changes++;
        policy.Update(true,true,false,false,100);
        policy.Update(true,true,false,false,5099);
        Check(!policy.IsSleeping,"The entire five-second grace period must elapse.");
        policy.Update(true,true,false,false,5100);
        Check(policy.IsSleeping&&changes==1,"The collapsed sphere must enter sleep after five seconds.");
        for(var tick=5101;tick<6000;tick++)policy.Update(true,true,false,false,tick);
        Check(changes==1,"Sleeping ticks must not emit repeated resource transitions.");
        policy.Update(true,true,false,true,6000);
        Check(!policy.IsSleeping&&changes==2,"Interaction must immediately wake the sphere.");
        policy.Update(true,false,false,false,7000);
        policy.Update(true,false,false,false,20000);
        Check(!policy.IsSleeping,"An open menu, including a compact menu, must not sleep merely from inactivity.");
        policy.Update(true,false,true,false,21000);
        policy.Update(true,false,true,false,26000);
        Check(policy.IsSleeping,"A completely covered host must sleep even when a menu is open.");
        policy.Update(true,false,false,false,26001);
        Check(!policy.IsSleeping,"Removing fullscreen coverage must wake an open menu immediately.");
        policy.Update(true,true,false,false,30000);
        policy.Update(true,true,false,false,35000);
        policy.Update(false,true,true,false,35001);
        Check(!policy.IsSleeping,"Disabling the option must immediately wake the host.");
        policy.Update(false,true,true,false,100000);
        Check(!policy.IsSleeping,"Disabled saving must not sleep under either condition.");
        policy.Update(true,true,false,false,100001);
        policy.Update(true,true,false,true,104999);
        policy.Update(true,true,false,false,105000);
        policy.Update(true,true,false,false,109999);
        Check(!policy.IsSleeping,"Interaction during the grace period must restart the delay.");
        policy.Update(true,true,false,false,110000);
        policy.Wake();
        Check(!policy.IsSleeping,"Direct pointer/focus events must wake without waiting for a timer.");
        Check(new HitSettings().AutomaticResourceSaving&&JsonSerializer.Deserialize<HitSettings>("{}")!.AutomaticResourceSaving,
            "New and existing profiles without the field must default to enabled.");
        var path=Path.GetFullPath("obj/resource-tests/"+Guid.NewGuid().ToString("N")+"/settings.json");
        var settings=new HitSettingsService(path);
        settings.Update(value=>value with {AutomaticResourceSaving=false});
        Check(!new HitSettingsService(path).Current.AutomaticResourceSaving,"OFF must persist across restart.");
        Console.WriteLine("PASS: resource-saving grace, sphere versus compact menu, coverage, immediate wake, cancellation, defaults and persistence.");
    }
}
