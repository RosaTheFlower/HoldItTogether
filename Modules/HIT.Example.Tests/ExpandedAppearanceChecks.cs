using System.Reflection;
using System.Text.Json;
using HoldItTogether.Components;
using HoldItTogether.Components.Settings;
using HoldItTogether.Services;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class ExpandedAppearanceChecks
{
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task RunAsync(HitSettingsService settings,HitAppearanceService appearance,IServiceProvider services,string root)
    {
        var fonts=await appearance.Fonts.GetAsync();
        Check(!OperatingSystem.IsWindows()||fonts.Count>0,"DirectWrite must enumerate installed font families.");
        Check(fonts.SequenceEqual(fonts.Order(StringComparer.OrdinalIgnoreCase))&&fonts.Distinct(StringComparer.OrdinalIgnoreCase).Count()==fonts.Count,"The font catalogue contains sorted unique families.");
        await File.WriteAllTextAsync("obj/personalization-tests/installed-fonts.json",JsonSerializer.Serialize(fonts));
        var fontDefault=new HitAppearancePreferences{Enabled=true};
        Check(!fontDefault.Css().Contains("font-family:")&&!fontDefault.Css().Contains("--hit-font:"),"Keep theme default must emit no font-family override.");
        Check((fontDefault with{Font="system"}).Normalize().Font=="system","Legacy explicit system font remains supported.");
        var chosen=fonts.FirstOrDefault()??"Example Font";
        var custom=fontDefault with{Font="custom",CustomFont=chosen,TextSize=15,TextLineHeight=1.8,SphereSize=80,InactiveSphereSize=40,CollapsedBackgroundOpacity=70,BackgroundBrightness=120,BackgroundContrast=130,BackgroundGrayscale=25,BackgroundZoom=120,BackgroundRotation=12,EyeOffsetX=3,EyeOffsetY=-2,ShadowColor="#12345680"};
        appearance.Apply(custom,"default");
        var copy=new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance;
        Check(JsonSerializer.Serialize(copy)==JsonSerializer.Serialize(custom),"Expanded visual preferences survive restart.");
        var export=appearance.Export(custom,"default");
        Check(File.ReadAllText(export).Contains(HitAppearancePreferences.QuoteFont(chosen)),"Custom font family names are included in exported CSS.");
        await File.WriteAllTextAsync("obj/personalization-tests/expanded.css",appearance.Css(custom));
        var unsafeName=custom with{CustomFont="Font';display:none;/*"};
        Check(unsafeName.Normalize().FontFamily!.Contains("\\'"),"Custom font names must be quoted as CSS data.");
        Check((custom with{CustomFont="bad\nfont"}).Normalize().CustomFont is null,"Control characters are rejected in persisted font names.");
        var ids=Enumerable.Range(0,40).Select(i=>i.ToString("x32")+".png").ToArray();
        settings.Update(s=>s with{BackgroundMediaLimit=64,Appearance=custom with{BackgroundMedia=ids}});
        Check(settings.Current.Appearance.MediaIds.Length==40,"Selections beyond the previous eight-item limit must be retained.");
        settings.Update(s=>s with{BackgroundMediaLimit=8});
        Check(settings.Current.Appearance.MediaIds.Length==40,"Reducing the import limit must preserve existing references.");
        var profile=appearance.Save("Large gallery",settings.Current.Appearance,"default");
        Check(appearance.Profiles().Single(p=>p.Id==profile.Id).Appearance.MediaIds.SequenceEqual(ids),"Larger galleries survive profile save/reload.");
        Check((new HitSettings{BackgroundMediaLimit=int.MaxValue}).Normalize().BackgroundMediaLimit==256,"The gallery remains bounded.");
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var (policyId,policy)=await renderer.MountAsync<ImageSettingsView>();
                await renderer.DispatchEventAsync(renderer.Event(policyId,"input","aria-label","Maximum background items","oninput"),null,new ChangeEventArgs{Value="64"});
                Check(settings.Current.BackgroundMediaLimit==64&&settings.Current.Appearance.MediaIds.Length==40,"The Images settings control saves a larger limit without trimming references.");
                var changes=new List<string>();
                var (id,picker)=await renderer.MountAsync<InstalledFontPicker>(new(){[nameof(InstalledFontPicker.Changed)]=EventCallback.Factory.Create<string>(changes,(string value)=>changes.Add(value))});
                if(fonts.Count>0)
                {
                    await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Search fonts","oninput"),null,new ChangeEventArgs{Value=chosen});
                    await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Use font "+chosen,"onclick"),null,new Microsoft.AspNetCore.Components.Web.MouseEventArgs());
                    Check(changes.Single()==chosen,"Installed-font search and selection must use the real enumerated family.");
                }
                Check(renderer.Errors.Count==0,"Installed font picker must render without errors.");
            });
        }
        await CheckSphereAsync(root);
        await BackgroundImportChecks.RunAsync(root);
        Console.WriteLine($"PASS: {fonts.Count} installed font families, theme-default/custom fonts, expanded CSS/persistence, configurable gallery limit and sphere inactivity behavior.");
    }

    private static async Task CheckSphereAsync(string root)
    {
        var settings=new HitSettingsService(Path.Combine(root,"sphere.json"));
        settings.Update(s=>s with{ShrinkInactiveSphere=false,SphereShrinkDelayMs=0});
        var resources=new HitResourcePolicy();var log=new HitDiagnosticLog();
        var store=new ExtensionStore(Path.Combine(root,"sphere-modules"));
        using var spectrum=new AudioSpectrumService(log);
        await using var hub=new HitExtensionHub(new FakeMedia(),spectrum,settings,resources,store,log);
        await using var provider=new ServiceCollection().AddLogging().AddSingleton(settings).AddSingleton(resources).AddSingleton(hub)
            .AddSingleton<ExtensionUi>().AddSingleton<IJSRuntime,NoJs>()
            .AddSingleton(new DesktopWindowService(settings,new WindowPositionStore(Path.Combine(root,"window.json")))).BuildServiceProvider();
        await using var renderer=new InteractiveRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        SphereLifecyclePreview? sphere=null;
        void Input(string name,params object[] args)=>typeof(DynamicSphere).GetMethod(name,BindingFlags.NonPublic|BindingFlags.Instance)!.Invoke(sphere,args);
        string State()=>typeof(DynamicSphere).GetField("_state",BindingFlags.NonPublic|BindingFlags.Instance)!.GetValue(sphere)!.ToString()!;
        await renderer.Dispatcher.InvokeAsync(async()=>{(_,sphere)=await renderer.MountAsync<SphereLifecyclePreview>();Input("SetPointerInside",true);});
        await Task.Delay(180);
        await renderer.Dispatcher.InvokeAsync(()=>{Check(State()=="Open","Hover still opens an unpinned menu.");Input("SetPointerInside",false);});
        await Task.Delay(300);
        await renderer.Dispatcher.InvokeAsync(()=>Check(State()=="Away","Leaving an unpinned menu still closes it without shrinking."));
        await renderer.Dispatcher.InvokeAsync(()=>settings.Update(s=>s with{ShrinkInactiveSphere=true}));await Task.Delay(50);
        await renderer.Dispatcher.InvokeAsync(()=>Check(State()=="Idle","Enabling inactivity shrink applies the configured delay."));
        await renderer.Dispatcher.InvokeAsync(()=>settings.Update(s=>s with{ShrinkInactiveSphere=false}));await Task.Delay(30);
        await renderer.Dispatcher.InvokeAsync(()=>{Check(State()=="Away","Disabling shrink restores the normal sphere.");Input("PinOpen");Input("SetPointerInside",false);});await Task.Delay(250);
        await renderer.Dispatcher.InvokeAsync(()=>Check(State()=="Open"&&renderer.Errors.Count==0,"Pinned menus remain open independently of sphere shrinking."));
    }
}

// Exercise the real shell state/timers without starting a native window or rendering unrelated menus.
public sealed class SphereLifecyclePreview : DynamicSphere
{
    protected override void BuildRenderTree(RenderTreeBuilder builder) { }
    protected override Task OnAfterRenderAsync(bool firstRender)=>Task.CompletedTask;
}
