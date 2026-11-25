using HoldItTogether.Components.HitMenus;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.Logging;

internal static class SphereSettingsChecks
{
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task RunAsync(HitSettingsService settings,HitAppearanceService appearance,IServiceProvider services,string root)
    {
        var original=settings.Current;
        try
        {
            var custom=original.Appearance with{Font="georgia",Accent="#123456",SphereSize=88,EyeHeight=14,Radius=17,BorderWidth=3,BackgroundWhenCollapsed=false,EditedProperties=null};
            settings.Update(s=>s with{Appearance=custom,SphereShrinkDelayMs=4000,MenuCompactDelayMs=1000,AlwaysOnTop=false,IdleOpacityEnabled=true,WindowMagnetize=false});
            await using var renderer=new InteractiveRenderer(services,(ILoggerFactory)services.GetService(typeof(ILoggerFactory))!);
            await renderer.Dispatcher.InvokeAsync(async()=>
            {
                var (id,view)=await renderer.MountAsync<HitMenuSettings>();
                async Task Change(string label,object value,string tag="input",string eventName="onchange")=>await renderer.DispatchEventAsync(renderer.Event(id,tag,"aria-label",label,eventName),null,new ChangeEventArgs{Value=value});
                await view.NavigateToPageAsync("Sphere/Behavior");
                await Change("Compact after","2500",eventName:"oninput");
                await Change("Sphere shrink delay","3000",eventName:"oninput");
                Check(settings.Current.MenuCompactDelayMs==2500&&settings.Current.SphereShrinkDelayMs==3000,"Sphere behavior controls retain immediate persistence.");
                await view.NavigateToPageAsync("Personalization/Sphere");
                Check(view.CurrentPage=="Sphere/Collapsed sphere","The legacy Sphere page redirects into the centralized topic.");
                await Change("Sphere size","96",eventName:"oninput");
                await view.NavigateToPageAsync("Personalization/Eyes");
                Check(view.CurrentPage=="Sphere/Collapsed sphere","The legacy Eyes page redirects into the centralized topic.");
                await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Expand Eyes","onclick"),null,new MouseEventArgs());
                await Change("Eye height","18",eventName:"oninput");
                Check(appearance.Current.SphereState(false).Size==96&&appearance.Current.SphereState(false).EyeHeight==18&&appearance.Current.SphereState(true).EyeHeight==14,"Sphere subpages retain one appearance draft.");
                await view.NavigateToPageAsync("Sphere/Inactive sphere");
                await Change("Sphere size","48",eventName:"oninput");
                await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Expand Background","onclick"),null,new MouseEventArgs());
                await Change("Show background",true);await Change("Play videos and slideshows",true);
                Check(appearance.Current.SphereState(true) is{Size:48,PlayBackground:true,ShowBackground:true}&&appearance.Current.SphereState(false).Size==96,"Inactive controls change only the inactive sphere snapshot.");
                await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Reset Inactive sphere appearance","onclick"),null,new MouseEventArgs());
                Check(appearance.Current.SphereState(true).Size is null&&appearance.Current.SphereState(false).Size==96,"A state reset preserves customization of the other state.");
                await view.NavigateToPageAsync("Placement");
                Check(view.CurrentPage=="Sphere/Window placement","Legacy placement remains reachable through page history.");
                await view.CloseSettingsAsync();
                Check(view.CurrentPage=="Sphere/"&&!view.CanGoBack,"Back from a sphere child returns one level to its root.");
                await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Restore sphere defaults","onclick"),null,new MouseEventArgs());
                var defaults=new HitSettings();var saved=settings.Current;
                Check(saved.MenuCompactDelayMs==defaults.MenuCompactDelayMs&&saved.SphereShrinkDelayMs==defaults.SphereShrinkDelayMs&&saved.ShrinkInactiveSphere==defaults.ShrinkInactiveSphere,"Restore resets compaction and shrink policy.");
                Check(saved.AlwaysOnTop==defaults.AlwaysOnTop&&saved.WindowMagnetize==defaults.WindowMagnetize&&!saved.IdleOpacityEnabled,"Restore resets placement, stacking and idle opacity preferences.");
                Check(saved.Appearance.SphereSize is null&&saved.Appearance.EyeHeight is null&&saved.Appearance.BackgroundWhenCollapsed,"Restore removes sphere-specific appearance overrides.");
                Check(saved.Appearance.Radius==17&&saved.Appearance.BorderWidth==3&&saved.Appearance.Font==custom.Font&&saved.Appearance.Accent==custom.Accent&&saved.Appearance.MediaIds.SequenceEqual(custom.MediaIds)&&saved.Theme==original.Theme,"Restore preserves unrelated fonts, palette, theme and background references.");
                var restarted=new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                Check(restarted.Appearance.Font==custom.Font&&restarted.Appearance.SphereSize is null&&restarted.MenuCompactDelayMs==defaults.MenuCompactDelayMs,"Sphere defaults survive restart.");
                await view.NavigateToPageAsync("Personalization/");
                Check(view.IsPersonalizationOverview&&renderer.Event(id,"input","aria-label","Use custom appearance","onchange")!=0,"Switching from Sphere to Personalization restores its own editor and master switch.");
                Check(renderer.Errors.Count==0,"Sphere navigation/edit/reset must not produce renderer errors.");
            });
        }
        finally{settings.Update(_=>original);appearance.Cancel();}
        Console.WriteLine("PASS: centralized sphere controls, child Back, legacy page redirects, retained draft and scoped default restoration/persistence.");
    }
}
