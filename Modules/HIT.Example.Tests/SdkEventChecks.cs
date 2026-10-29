using HIT.Sdk;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;

internal static class SdkEventChecks
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task RunAsync(string repo)
    {
        var root=Path.GetFullPath("obj/sdk-event-tests/"+Guid.NewGuid().ToString("N"));
        var store=new ExtensionStore(Path.Combine(root,"Modules"));
        await using var archive=File.OpenRead(Path.Combine(repo,"artifacts/packages/dev.hit.example-1.0.2.hitmenu"));
        var installed=await store.InstallAsync(archive,"test.hitmenu");
        var settings=new HitSettingsService(Path.Combine(root,"settings.json"));
        var themes=new HitThemeService(settings,Path.Combine(root,"Themes"),Path.Combine(repo,"wwwroot/themes"));
        await using var clock=new HitClockService(settings,Path.Combine(root,"clock.json"),TimeProvider.System,new SilentSound(),false);
        using var spectrum=new AudioSpectrumService();var resources=new HitResourcePolicy();var log=new HitDiagnosticLog();var media=new FakeMedia();
        await using var hub=new HitExtensionHub(media,spectrum,settings,resources,store,log);
        hub.PublishHost(HitSurfaceState.Open,true,false);
        using var lifetime=new CancellationTokenSource();
        var manifest=installed.Manifest with{Capabilities=["events.subscribe","host.read","appearance.read","media.read","actions.register","actions.execute"]};
        var api=new ExtensionApi(manifest,store,media,clock,settings,themes,new NoLinks(),new ExtensionUi(),lifetime.Token,hub);
        Check(api is IHitApiV2,"New host exposes the additive interface while preserving IHitApi.");
        var hostEvents=0;var mediaEvents=0;
        using var view=new CancellationTokenSource();
        using var host=api.Events.SubscribeHost(_=>Interlocked.Increment(ref hostEvents),view.Token);
        using var faulty=api.Events.SubscribeHost(_=>throw new InvalidOperationException("observer regression"),view.Token);
        using var first=api.Events.SubscribeMedia(_=>Interlocked.Increment(ref mediaEvents),view.Token);
        using var second=api.Events.SubscribeMedia(_=>Interlocked.Increment(ref mediaEvents),view.Token);
        await Task.Delay(650);
        Check(hostEvents>0&&mediaEvents>=2&&media.Reads<=3,"Subscribers must receive snapshots through one shared media poller.");
        Check(log.Snapshot().Any(entry=>entry.Code=="observer.failed"),"One failing observer must be isolated without blocking healthy observers.");
        hub.PublishHost(HitSurfaceState.Compact,true,true);
        Check(api.Events.ReadHost() is{Surface:HitSurfaceState.Compact,Pinned:true},"Host read must reflect actual published state.");
        hub.PublishTokens(new(){["--hit-accent"]="#123456",["bad"]="discard"});
        Check(api.Events.ReadAppearance().Tokens.Count==1&&api.Events.ReadAppearance().Tokens["--hit-accent"]=="#123456","Appearance exposes a validated read-only token snapshot.");
        var runs=0;
        using var action=api.Actions.Register("test-action","Test action",_=>{runs++;return Task.CompletedTask;});
        var id=manifest.Id+":test-action";
        Check(api.Actions.List().Any(a=>a.Id==id)&&await api.Actions.ExecuteAsync(id)&&runs==1,"Registered actions must list and execute through the same registry.");
        try{api.Actions.Register("test-action","Duplicate",_=>Task.CompletedTask);throw new Exception("Duplicate action accepted");}catch(InvalidOperationException){}
        try{api.Events.SubscribeSpectrum(_=>{});throw new Exception("Unauthorized spectrum accepted");}catch(InvalidOperationException){}
        store.SetEnabled(manifest.Id+":"+manifest.Menus[0].Id,false);
        Check(!await api.Actions.ExecuteAsync(id)&&api.Actions.List().Count==0,"Disabled package actions must not execute or appear in discovery.");
        await Task.Delay(150);var quietReads=media.Reads;await Task.Delay(600);Check(media.Reads==quietReads,"Disabled packages must not keep media polling active.");
        store.SetEnabled(manifest.Id+":"+manifest.Menus[0].Id,true);
        Check(await api.Actions.ExecuteAsync(id),"Re-enabled package actions remain usable.");
        resources.Update(true,true,false,false,0);resources.Update(true,true,false,false,5000);
        await Task.Delay(150);quietReads=media.Reads;await Task.Delay(600);Check(media.Reads==quietReads,"Resource sleep pauses SDK media work too.");
        view.Cancel();await Task.Delay(100);var quietHost=hostEvents;hub.PublishHost(HitSurfaceState.Open,true,false);await Task.Delay(100);Check(hostEvents==quietHost,"View cancellation must release event delivery.");
        lifetime.Cancel();Check(api.Actions.List().Count==0,"Module cancellation unregisters owned actions.");
        Check(spectrum.ActiveCaptures==0,"Unauthorized/subscription-free checks must not create a capture.");
        Console.WriteLine("PASS: additive SDK discovery, permissions, shared event polling, tokens/state, actions, package disable/re-enable, sleep and cancellation cleanup.");
    }
}
