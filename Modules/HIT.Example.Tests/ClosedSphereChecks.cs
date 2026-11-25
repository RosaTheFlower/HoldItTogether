using System.Text.Json;
using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class ClosedSphereChecks
{
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task RunAsync(string repo)
    {
        var root=Path.GetFullPath("obj/closed-sphere-tests/"+Guid.NewGuid().ToString("N"));
        var settings=new HitSettingsService(Path.Combine(root,"settings.json"));
        var themes=new HitThemeService(settings,Path.Combine(root,"Themes"),Path.Combine(repo,"wwwroot/themes"));
        var appearance=new HitAppearanceService(settings,themes,Path.Combine(root,"Appearance"));
        var resources=new HitResourcePolicy();
        var legacy=new HitAppearancePreferences{Enabled=true,SphereSize=80,InactiveSphereSize=40,EyeHeight=12,CollapsedBackgroundOpacity=70,Radius=17,BorderWidth=3};
        Check(legacy.SphereState(false).Size==80&&legacy.SphereState(true).Size==40&&legacy.SphereState(true).EyeHeight==12,"Missing state snapshots preserve legacy sphere choices.");
        var collapsed=new HitSphereStateAppearance{Size=84,Roundness=35,Opacity=80,Background="#12345680",BorderWidth=2,BorderStyle="dashed",BorderColor="#abcdef",ShadowStrength=50,ShadowBlur=12,
            ShowEyes=true,EyeColor="#ffffff",EyeSize=9,EyeHeight=11,EyeGap=7,EyeRoundness=30,EyeOffsetX=2,EyeOffsetY=3,EyeOpacity=70,EyeRotation=-10,TrackPointer=false,
            ShowBackground=true,PlayBackground=true,BackgroundOpacity=60,BackgroundFit="contain",BackgroundPositionX=20,BackgroundPositionY=30,BackgroundBlur=2,BackgroundSaturation=80,BackgroundBrightness=130,BackgroundContrast=110,BackgroundGrayscale=10,BackgroundZoom=120,BackgroundRotation=12};
        var inactive=new HitSphereStateAppearance{Size=40,Roundness=10,Background="#ddeecc",BorderWidth=1,BorderColor="#ff0000",BorderStyle="solid",ShadowStrength=0,ShowEyes=true,EyeColor="#ffff00",EyeSize=4,EyeHeight=6,EyeGap=3,EyeRoundness=20,EyeOffsetX=1,EyeOffsetY=-2,EyeOpacity=40,EyeRotation=15,
            ShowBackground=true,PlayBackground=true,BackgroundOpacity=25,BackgroundFit="stretch",BackgroundPositionX=70,BackgroundPositionY=80,BackgroundBlur=0,BackgroundSaturation=40,BackgroundBrightness=80,BackgroundContrast=90,BackgroundGrayscale=30,BackgroundZoom=130,BackgroundRotation=-10};
        var custom=legacy with{CollapsedSphere=collapsed,InactiveSphere=inactive,Font="georgia",Accent="#aabbcc"};
        appearance.Apply(custom,"default");
        var restarted=new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance;
        Check(restarted.CollapsedSphere==collapsed&&restarted.InactiveSphere==inactive,"Separate state snapshots survive restart.");
        var profile=appearance.Save("Closed states",custom,"default");
        Check(appearance.Profiles().Single().Appearance.CollapsedSphere==collapsed,"Profiles retain independent state customization.");
        var reset=custom.RestoreSphereStateDefaults(false);
        Check(reset.SphereState(false).Size is null&&reset.SphereState(true)==inactive&&reset.Radius==17&&reset.Font=="georgia","Per-state reset restores inheritance without changing the other state or open menu.");
        var all=custom.RestoreSphereDefaults();
        Check(all.SphereState(false).Size is null&&all.SphereState(true).Size is null&&all.Radius==17&&all.BorderWidth==3,"Sphere reset preserves open-menu appearance.");
        var invalid=(custom with{CollapsedSphere=collapsed with{Size=-1,Roundness=999,Opacity=0,Background="red;display:none",BackgroundFit="url(bad)",EyeRotation=999,BackgroundZoom=999}}).Normalize().CollapsedSphere!;
        Check(invalid is{Size:32,Roundness:50,Opacity:10,Background:null,BackgroundFit:null,EyeRotation:180,BackgroundZoom:200},"State input clamps bounds and rejects CSS injection.");
        Directory.CreateDirectory("obj/personalization-tests");
        await File.WriteAllTextAsync("obj/personalization-tests/closed-states.css",appearance.Css(custom));
        await File.WriteAllTextAsync("obj/personalization-tests/collapsed-only.css",appearance.Css(new HitAppearancePreferences{Enabled=true,CollapsedSphere=collapsed}));
        await File.WriteAllTextAsync("obj/personalization-tests/closed-states.json",JsonSerializer.Serialize(custom));
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0ZsAAAAASUVORK5CYII=");
        var first=await appearance.ImportImageAsync(new MemoryStream(png));var second=await appearance.ImportImageAsync(new MemoryStream(png));
        var gallery=new HitAppearancePreferences{Enabled=true,BackgroundMedia=[first,second],BackgroundMode="carousel",BackgroundIntervalSeconds=5,CollapsedSphere=new(){PlayBackground=true},InactiveSphere=new(){PlayBackground=false}};
        appearance.Apply(gallery,"default");
        await using var services=new ServiceCollection().AddLogging().AddSingleton(settings).AddSingleton(appearance).AddSingleton(resources).AddSingleton<IJSRuntime,NoJs>().BuildServiceProvider();
        await using var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>());
        CustomBackground? view=null;int id=0;
        Task<bool> Playing()=>renderer.Dispatcher.InvokeAsync(()=>renderer.Graph(id).Any(f=>f.FrameType==Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Attribute&&f.AttributeName=="data-hit-background-active"&&Equals(f.AttributeValue,"true")));
        await renderer.Dispatcher.InvokeAsync(async()=>{(id,view)=await renderer.MountAsync<CustomBackground>(new(){[nameof(CustomBackground.Active)]=false});});
        Check(await Playing(),"Collapsed playback is active when explicitly enabled.");
        await Task.Delay(5300);
        Check(await renderer.Dispatcher.InvokeAsync(()=>renderer.Graph(id).Any(f=>f.FrameType==Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Attribute&&f.AttributeName=="style"&&f.AttributeValue?.ToString()?.Contains(second)==true)),"The slideshow advances while the sphere is closed.");
        await renderer.Dispatcher.InvokeAsync(async()=>await view!.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(CustomBackground.Inactive)]=true})));
        Check(!await Playing(),"Inactive playback follows its independent preference.");
        await renderer.Dispatcher.InvokeAsync(async()=>await view!.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(CustomBackground.Inactive)]=false})));
        await renderer.Dispatcher.InvokeAsync(async()=>await view!.SetPresentationSuspended(true));
        Check(!await Playing(),"Hidden/reduced-motion presentation pauses the closed slideshow.");
        await renderer.Dispatcher.InvokeAsync(async()=>await view!.SetPresentationSuspended(false));
        await renderer.Dispatcher.InvokeAsync(()=>{resources.Update(true,true,false,false,0);resources.Update(true,true,false,false,5001);});
        await Task.Delay(50);
        Check(!await Playing(),"Resource saving still pauses explicitly enabled closed playback.");
        Check(renderer.Errors.Count==0,"Closed background lifecycle must not produce renderer errors.");
        var exported=appearance.Export(custom with{BackgroundMedia=[first,second],BackgroundMode="smooth"},"default");
        Check(File.ReadAllText(exported).Contains("var(--hit-background-play-state,paused)"),"CSS slideshow exports retain per-state playback settings.");
        Console.WriteLine("PASS: independent closed/inactive appearance, legacy defaults, normalization, profile/restart/reset/export and closed slideshow lifecycle.");
    }
}
