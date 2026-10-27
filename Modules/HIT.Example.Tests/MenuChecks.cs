using System.Text.Json;
using HoldItTogether.Components;
using HoldItTogether.Components.Audio;
using HoldItTogether.Components.HitMenus;
using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class MenuChecks
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    public static async Task RunAsync(IServiceProvider services, string root, string repo)
    {
        var settings = services.GetRequiredService<HitSettingsService>();
        settings.Update(value => value with { AudioScaleX = 25, AudioScaleY = 200, AudioVisualizerEnabled = true, AudioLyricsTimeline = true, AudioLyricsTimelineThickness = 6, AudioArtworkZoom = 180, AudioArtworkDistortion = 30, AudioLyricsShowTitle = false, AudioLyricsShowSource = false });
        var saved = new HitSettingsService(Path.Combine(root, "settings.json")).Current;
        Check(saved.AudioScaleX == 25 && saved.AudioScaleY == 200 && saved.AudioVisualizerEnabled && saved.AudioLyricsTimeline && saved.AudioLyricsTimelineThickness == 6, "Audio scale, visualizer and timeline preferences must persist");
        Check(!saved.AudioLyricsShowTitle && !saved.AudioLyricsShowSource, "Lyrics title/source visibility must persist independently");
        Check((saved with { AudioScaleX = -1, AudioScaleY = 500, AudioLyricsTimelineThickness = 99 }).Normalize() is { AudioScaleX: 25, AudioScaleY: 200, AudioLyricsTimelineThickness: 6 }, "Audio preference limits");
        settings.Update(value => value with { AlwaysOnTop=true, ForceAlwaysOnTop=true, MenuCompactDelayMs=0,
            AudioArtworkOnBeat=true, AudioArtworkRadialBlur=true, AudioArtworkLens=65, AudioArtworkTemperature=-40,AudioArtworkColorEnabled=true });
        saved = new HitSettingsService(Path.Combine(root,"settings.json")).Current;
        Check(saved.ForceAlwaysOnTop && saved.MenuCompactDelayMs==0 && saved.AudioArtworkOnBeat && saved.AudioArtworkRadialBlur && saved.AudioArtworkLens==65 && saved.AudioArtworkTemperature==-40&&saved.AudioArtworkColorEnabled,"Experimental, departure timer and color-enable/optical preferences persist");
        Check(!new HitSettings().AudioArtworkColorEnabled,"Color adjustments are opt-in by default");
        Check(new HitSettings().MenuCompactDelayMs==5000 && (saved with {MenuCompactDelayMs=20000}).Normalize().MenuCompactDelayMs==10000,"Shared compact delay defaults to five seconds and is bounded");
        Check(!(saved with {AlwaysOnTop=false}).Normalize().ForceAlwaysOnTop,"Force topmost requires ordinary topmost");
        Check((saved with {AudioArtworkLens=999,AudioArtworkBrightness=-1,AudioArtworkHue=-999,AudioArtworkBeatStrength=999}).Normalize()
            is {AudioArtworkLens:100,AudioArtworkBrightness:20,AudioArtworkHue:-180,AudioArtworkBeatStrength:200},"Optical preference bounds");
        settings.Update(_ => new());
        var themes = services.GetRequiredService<HitThemeService>();
        var first = await themes.ImportAsync("Test One.css", new MemoryStream("@layer menus {:root {--hit-menu-text:#eee}}"u8.ToArray()));
        var second = await themes.ImportAsync("Test Two.css", new MemoryStream("@layer menus {:root {--hit-menu-text:#ddd}}"u8.ToArray()));
        await using (var interactive = new InteractiveRenderer(services, services.GetRequiredService<ILoggerFactory>()))
        {
            await interactive.Dispatcher.InvokeAsync(async () =>
            {
                var (id, view) = await interactive.MountAsync<HitMenuSettings>();
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Menu compact delay","onchange"),null,new ChangeEventArgs {Value="10000"});
                Check(settings.Current.MenuCompactDelayMs==10000,"The global compact delay saves from Settings");
                bool HasLabel(string label) => interactive.Graph(id).Any(frame=>frame.FrameType==RenderTreeFrameType.Attribute && frame.AttributeName=="aria-label" && Equals(frame.AttributeValue,label));
                Check(HasLabel("Force Always on Top"),"Force topmost appears under Always on Top");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Always on Top","onchange"),null,new ChangeEventArgs {Value=false});
                Check(!HasLabel("Force Always on Top") && !settings.Current.ForceAlwaysOnTop,"Disabling topmost hides and disables its experimental mode");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Always on Top","onchange"),null,new ChangeEventArgs {Value=true});
                async Task Click(string label) => await interactive.DispatchEventAsync(interactive.Event(id,"button","aria-label",label,"onclick"),null,new MouseEventArgs());
                await Click("Open Window placement");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Magnetize","onchange"),null,new ChangeEventArgs {Value=false});
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Allow other monitors","onchange"),null,new ChangeEventArgs {Value=false});
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Edge padding","onchange"),null,new ChangeEventArgs {Value="85"});
                var placementSaved=new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                Check(!placementSaved.WindowMagnetize&&!placementSaved.WindowAllowOtherMonitors&&placementSaved.WindowEdgeReachPercent==85,"Placement subview saves all three independent options");
                await view.CloseSettingsAsync();settings.Update(value=>value with {WindowMagnetize=true,WindowAllowOtherMonitors=true,WindowEdgeReachPercent=98});
                await Click("Open Scroll speed");
                Check(view.IsSettingsOpen, "Scroll speed must be a separate navigation-blocking view");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Scroll sensitivity","onchange"),null,new ChangeEventArgs {Value="2.5"});
                Check(settings.Current.ScrollSensitivity == 2.5, "Sensitivity must save from the new screen");
                await view.CloseSettingsAsync();
                await Click("Open Info");
                Check(interactive.Graph(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent.Contains("HIT version") || frame.FrameType == RenderTreeFrameType.Markup && frame.MarkupContent.Contains("HIT version")), "Info screen must display version: " + string.Join("; ", interactive.Errors));
                await view.CloseSettingsAsync();
                await Click("Manage themes");
                await interactive.DispatchEventAsync(interactive.Event(id,"button","data-theme-id",first,"onclick"),null,new MouseEventArgs());
                Check(settings.Current.Theme == "default", "Single click must select without activating");
                await interactive.DispatchEventAsync(interactive.Event(id,"button","data-theme-id",second,"onclick"),null,new MouseEventArgs {CtrlKey=true});
                await interactive.DispatchEventAsync(interactive.Event(id,"button","data-theme-id",first,"ondblclick"),null,new MouseEventArgs());
                Check(settings.Current.Theme == first, "Double click must activate");
                await Click("Delete selected themes");
                Check(themes.GetThemes().Any(theme=>theme.Id==first), "First deletion action must not remove files");
                await Click("Confirm theme deletion");
                Check(!themes.GetThemes().Any(theme=>theme.Id==first || theme.Id==second) && settings.Current.Theme=="default", "Confirmed multi-delete must remove selected personal themes and restore fallback");
                await view.CloseSettingsAsync();
                Check(interactive.Errors.Count==0,string.Join("; ",interactive.Errors));
                var (audioId, audio) = await interactive.MountAsync<HitMenuAudio>();
                await audio.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Show song title in lyrics","onchange"),null,new ChangeEventArgs {Value=false});
                Check(!settings.Current.AudioLyricsShowTitle && settings.Current.AudioLyricsShowSource, "Audio Config must change lyrics title without hiding source");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Show lyrics source","onchange"),null,new ChangeEventArgs {Value=false});
                Check(!settings.Current.AudioLyricsShowSource, "Audio Config source switch must save");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Hide buttons","onclick"),null,new MouseEventArgs());
                Check(audio.IsSettingsOpen,"Hide buttons is its own configuration screen");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Hide player buttons","onchange"),null,new ChangeEventArgs {Value=false});
                Check(!settings.Current.AudioHidePlayerButtons&&settings.Current.AudioHideLyricsButtons,"Player hiding is independent from lyrics hiding");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Hide lyrics buttons","onchange"),null,new ChangeEventArgs {Value=false});
                var controlsSaved=new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                Check(!controlsSaved.AudioHideLyricsButtons&&!controlsSaved.AudioHidePlayerButtons,"Both page-specific hiding choices persist");
                await audio.CloseSettingsAsync();
                Check(audio.IsSettingsOpen,"Back from Hide buttons returns to Audio settings");
                await audio.CloseSettingsAsync();
                Check(!audio.IsSettingsOpen&&interactive.Graph(audioId).Any(frame=>frame.FrameType==RenderTreeFrameType.Attribute&&frame.AttributeName=="data-hit-hide-controls"&&Equals(frame.AttributeValue,"false")),"Player page opts out of compaction when its switch is off");
                settings.Update(value=>value with {AudioHidePlayerButtons=true,AudioHideLyricsButtons=true});
                settings.Update(value => value with { AudioLyricsShowTitle = true, AudioLyricsShowSource = true });
                typeof(HitMenuAudio).GetMethod("OpenColors",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(audio,null);
                typeof(ComponentBase).GetMethod("StateHasChanged",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(audio,null);
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Enable color adjustments","onchange"),null,new ChangeEventArgs {Value=true});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Temperature","oninput"),null,new ChangeEventArgs {Value="50"});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Enable color adjustments","onchange"),null,new ChangeEventArgs {Value=false});
                Check(!settings.Current.AudioArtworkColorEnabled&&settings.Current.AudioArtworkTemperature==50,"Disabling color adjustments persists the switch and preserves slider values");
            });
        }
        await using (var html = new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await html.Dispatcher.InvokeAsync(async () =>
            {
                var preview = Path.Combine(repo,"obj/menu-tests"); Directory.CreateDirectory(preview);
                async Task Save<T>(string name, Dictionary<string,object?>? parameters = null) where T:IComponent
                {
                    var component=await html.RenderComponentAsync<T>(parameters is null?ParameterView.Empty:ParameterView.FromDictionary(parameters));
                    await File.WriteAllTextAsync(Path.Combine(preview,name+".html"),component.ToHtmlString());
                }
                await Save<HitMenuSettings>("settings");
                await Save<WindowPlacementView>("placement",new(){["Settings"]=settings.Current});
                await Save<HitInfoView>("info");
                var infoPreview = await html.RenderComponentAsync<InfoPreview>();
                InfoPreview.Instance!.ShowData();
                await File.WriteAllTextAsync(Path.Combine(preview,"info-data.html"),infoPreview.ToHtmlString());
                await Save<HitThemePicker>("themes",new(){["Detailed"]=true});
                await Save<AudioAppearanceView>("artwork",new(){["Settings"]=settings.Current});
                await Save<AudioEffectsView>("effects",new(){["Settings"]=settings.Current});
                await Save<AudioColorView>("colors",new(){["Settings"]=settings.Current});
                await Save<AudioControlsView>("audio-controls",new(){["Settings"]=settings.Current});
                await Save<AudioSettingsView>("audio-settings",new(){["Settings"]=settings.Current with {AudioLyricsTimeline=true}});
                await Save<HitMenuAudio>("player");
                await Save<HitMenuClock>("clock");
                var clockPreview=await html.RenderComponentAsync<ClockPlacementPreview>();
                await ClockPlacementPreview.Instance!.CompactAsync();
                await File.WriteAllTextAsync(Path.Combine(preview,"clock-compact.html"),clockPreview.ToHtmlString());
                await Save<AudioLyricsView>("lyrics",new(){["Title"]="A sample track",["Lyrics"]=new SyncedLyrics(Enumerable.Range(0,30).Select(index=>new LyricLine(index*5,"Lyric line "+index)).ToArray()),["Position"]=75d,["CanSeek"]=true});
                foreach (var (name, title, source) in new[] { ("lyrics-title",true,false), ("lyrics-source",false,true), ("lyrics-hidden",false,false) })
                {
                    await Save<AudioLyricsView>(name,new(){["Title"]="A sample track",["Lyrics"]=new SyncedLyrics([new(0,"A lyric line")]),["ShowTitle"]=title,["ShowSource"]=source});
                }
            });
        }
        var resetRoot=Path.Combine(root,"reset-profile");
        void Seed()
        {
            foreach(var folder in new[]{"Clock","Themes","Modules/Installed/test","Modules/State/test","Modules/Logs"})Directory.CreateDirectory(Path.Combine(resetRoot,folder));
            foreach(var file in new[]{"settings.json","Clock/state.json","Themes/custom.css","Modules/Installed/test/module.dll","Modules/State/test/data.json","Modules/Logs/test.log"})File.WriteAllText(Path.Combine(resetRoot,file),"test");
            File.WriteAllText(Path.Combine(resetRoot,"Modules/registry.json"),JsonSerializer.Serialize(new ExtensionRegistry{Order=["old"],DisabledMenus=["old"]},ExtensionStore.Json));
        }
        Seed(); var data=new HitDataService(resetRoot); data.Request(false);
        Check(File.Exists(Path.Combine(resetRoot,"settings.json")),"Requesting reset must defer changes until all services stop");
        HitDataService.ApplyPending(resetRoot);
        Check(!File.Exists(Path.Combine(resetRoot,"settings.json"))&&!Directory.Exists(Path.Combine(resetRoot,"Modules/State")),"Settings reset must clear global and module data");
        Check(File.Exists(Path.Combine(resetRoot,"Themes/custom.css"))&&File.Exists(Path.Combine(resetRoot,"Modules/Installed/test/module.dll")),"Settings reset must preserve installed themes/modules");
        var registry=JsonSerializer.Deserialize<ExtensionRegistry>(File.ReadAllText(Path.Combine(resetRoot,"Modules/registry.json")),ExtensionStore.Json)!;
        Check(registry.Order.Length==0&&registry.DisabledMenus.Length==0,"Reset must restore navigation preferences");
        Seed(); data.Request(true); HitDataService.ApplyPending(resetRoot);
        Check(!Directory.Exists(Path.Combine(resetRoot,"Themes"))&&!Directory.Exists(Path.Combine(resetRoot,"Modules"))&&!Directory.Exists(Path.Combine(resetRoot,"Clock")),"Delete everything must clear personal themes, modules and saved state");
        var legacy=Path.Combine(root,"legacy-modules");Directory.CreateDirectory(legacy);
        File.WriteAllText(Path.Combine(legacy,"registry.json"),JsonSerializer.Serialize(new ExtensionRegistry { Order=["HoldItTogether.Components.HitMenus.HitMenu\u0052elogio"],DisabledMenus=["HoldItTogether.Components.HitMenus.HitMenu\u0043onfiguracoes"] },ExtensionStore.Json));
        var migrated=new ExtensionStore(legacy);
        Check(migrated.State.Order[0].EndsWith("HitMenuClock")&&migrated.State.DisabledMenus[0].EndsWith("HitMenuSettings"),"Historical menu IDs must migrate without losing order/preferences");
        Console.WriteLine("PASS: Settings subviews, selection vs activation, multi-theme deletion, persisted Audio preferences, deferred data reset/delete and historical ID migration.");
    }
}

public sealed class ClockPlacementPreview : ComponentBase
{
    public static ClockPlacementPreview? Instance {get;private set;}
    private HitMenuClock? _clock;
    protected override void OnInitialized()=>Instance=this;
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<HoldItTogether.Services.HitMenus.HitMenuContext>>(0);
        builder.AddAttribute(1,"Name","HitMenu");
        builder.AddAttribute(2,"Value",new HoldItTogether.Services.HitMenus.HitMenuContext(true,default,true,default,true));
        builder.AddAttribute(3,"ChildContent",(RenderFragment)(content=>{
            content.OpenComponent<HitMenuClock>(0);content.AddComponentReferenceCapture(1,value=>_clock=(HitMenuClock)value);content.CloseComponent();
        }));builder.CloseComponent();
    }
    public Task CompactAsync()=>_clock!.CompactNowAsync();
}

public sealed class InfoPreview : ComponentBase
{
    public static InfoPreview? Instance { get; private set; }
    private HitInfoView? _view;
    protected override void OnInitialized() => Instance = this;
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<HitInfoView>(0);
        builder.AddComponentReferenceCapture(1, component => _view=(HitInfoView)component);
        builder.CloseComponent();
    }
    public void ShowData()
    {
        typeof(HitInfoView).GetField("_showData",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(_view,true);
        typeof(ComponentBase).GetMethod("StateHasChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(_view,null);
    }
}
