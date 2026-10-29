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
        Check(new HitSettings() is { AudioVisualizerStyle: AudioVisualizerStyle.Bars, AudioVisualizerHeightPercent: 100 }, "Legacy preferences retain the original bars and full height");
        Check((new HitSettings { AudioVisualizerStyle = (AudioVisualizerStyle)999, AudioVisualizerHeightPercent = -1 }).Normalize()
            is { AudioVisualizerStyle: AudioVisualizerStyle.Bars, AudioVisualizerHeightPercent: 10 }, "Invalid visualizer preferences fall back and clamp");
        Check((new HitSettings { AudioVisualizerHeightPercent = 999 }).Normalize().AudioVisualizerHeightPercent == 200, "Visualizer amplification is bounded to 200 percent");
        Check(new HitSettings() is {AudioVisualizerSizePercent:100,AudioVisualizerBlend:AudioVisualizerBlend.Normal},"Existing profiles retain original size and normal composition");
        Check((new HitSettings {AudioVisualizerSizePercent=999,AudioVisualizerBlend=(AudioVisualizerBlend)999}).Normalize()
            is {AudioVisualizerSizePercent:500,AudioVisualizerBlend:AudioVisualizerBlend.Normal},"Size and composition normalize safely");
        Check((new HitSettings {AudioVisualizerSizePercent=-1}).Normalize().AudioVisualizerSizePercent==100,"Minimum visualizer size is bounded");
        Check((new HitSettings {AudioVisualizerStyle=(AudioVisualizerStyle)3}).Normalize().AudioVisualizerStyle==AudioVisualizerStyle.Smooth,"Retired Filled value migrates to Smooth without reusing IDs");
        Check((new HitSettings {AudioVisualizerOpacityPercent=-1,AudioVisualizerPositionYPercent=999}).Normalize()
            is {AudioVisualizerOpacityPercent:0,AudioVisualizerPositionYPercent:100},"Visualizer opacity and position normalize independently");
        settings.Update(value => value with { AudioScaleX = 25, AudioScaleY = 200, AudioVisualizerEnabled = true, AudioLyricsTimeline = true, AudioLyricsTimelineThickness = 6, AudioArtworkZoom = 180, AudioArtworkDistortion = 30, AudioLyricsShowTitle = false, AudioLyricsShowSource = false });
        var saved = new HitSettingsService(Path.Combine(root, "settings.json")).Current;
        Check(saved.AudioScaleX == 25 && saved.AudioScaleY == 200 && saved.AudioVisualizerEnabled && saved.AudioLyricsTimeline && saved.AudioLyricsTimelineThickness == 6, "Audio scale, visualizer and timeline preferences must persist");
        Check(!saved.AudioLyricsShowTitle && !saved.AudioLyricsShowSource, "Lyrics title/source visibility must persist independently");
        Check((saved with { AudioScaleX = -1, AudioScaleY = 500, AudioLyricsTimelineThickness = 99 }).Normalize() is { AudioScaleX: 25, AudioScaleY: 200, AudioLyricsTimelineThickness: 6 }, "Audio preference limits");
        settings.Update(value => value with { StartWithWindows=true,AlwaysOnTop=true, ForceAlwaysOnTop=true, MenuCompactDelayMs=0,
            AudioArtworkOnBeat=true, AudioArtworkRadialBlur=true, AudioArtworkLens=65, AudioArtworkTemperature=-40,AudioArtworkColorEnabled=true });
        saved = new HitSettingsService(Path.Combine(root,"settings.json")).Current;
        Check(saved.StartWithWindows&&!new HitSettings().StartWithWindows,"Start with Windows persists and is off by default");
        Check(HoldItTogether.Services.Windows.WindowsStartup.Command(@"C:\Program Files\HIT\HoldItTogether.exe")=="\"C:\\Program Files\\HIT\\HoldItTogether.exe\"","Startup executable paths with spaces are quoted");
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
                Check(!view.CanGoBack,"The General settings topic has no parent page");
                bool HasLabel(string label) => interactive.Graph(id).Any(frame=>frame.FrameType==RenderTreeFrameType.Attribute && frame.AttributeName=="aria-label" && Equals(frame.AttributeValue,label));
                async Task Click(string label) => await interactive.DispatchEventAsync(interactive.Event(id,"button","aria-label",label,"onclick"),null,new MouseEventArgs());
                async Task Change(string label,object value,string evt="onchange")=>await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label",label,evt),null,new ChangeEventArgs {Value=value});
                Check(HasLabel("Open Info")&&HasLabel("Open Accessibility"),"Sidebar exposes all categories including INFO");
                await Change("Infinite menu",true);
                Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.InfiniteMenu,"Infinite menu persists from General");
                await Change("Infinite menu",false);
                await Change("Always on Top",false);
                Check(HasLabel("Force Always on Top")&&!settings.Current.ForceAlwaysOnTop,"Unavailable FORCE stays discoverable");
                await Change("Always on Top",true);
                await Click("Open Appearance");
                Check(!view.CanGoBack,"Peer settings topics have no header Back");
                await Change("Hide artwork in settings",true);
                await Change("Show artwork button in settings",false);
                Check(!new HitSettingsService(Path.Combine(root,"settings.json")).Current.ShowConfigurationCoverButton,"Header cover-button visibility persists independently of the artwork policy");
                await Change("Icon only",true);
                Check(settings.Current.TitleDisplayMode==2,"Example radio changes the global title presentation");
                await Change("Name and icon",true);
                await Click("Open Interaction");await Change("Compact after",10000d,"oninput");
                Check(settings.Current.MenuCompactDelayMs==10000,"Compact delay persists from its topic");
                await Change("Title hold duration",900d,"oninput");
                await Click("Open Images");
                await Change("Allow GIF backgrounds",false);
                await Change("Maximum size per image (MB)","12");
                Check(!new HitSettingsService(Path.Combine(root,"settings.json")).Current.AllowGifBackgrounds&&settings.Current.Appearance.BackgroundImageLimitMb==12,"Images topic persists GIF policy and the image limit");
                await Change("Allow GIF backgrounds",true);
                await Change("Unlimited image size",true);
                Check(settings.Current.Appearance.BackgroundImageLimitMb is null,"Images topic supports an unlimited size policy");
                await Change("Unlimited image size",false);
                await Click("Open Accessibility");
                await Change("Menu entrance animations",false);
                Check(!new HitSettingsService(Path.Combine(root,"settings.json")).Current.MenuEntranceAnimations,"Menu entrance preference persists independently of other animation settings");
                await Change("Menu entrance animations",true);
                await Change("Automatic transparency",true);
                await Change("Idle opacity",25d,"oninput");
                await Change("Show option descriptions",false);
                Check(settings.Current is {IdleOpacityPercent:25,ShowSettingDescriptions:false,HideArtworkInSettings:true},"New preferences and range values persist independently");
                await Click("Open Window placement");await Change("Snap to center",true);
                Check(settings.Current.WindowMagnetizeCenter,"Center magnetism saves from its disabled-aware row");
                await Click("Open General");
                var originalAppearance=settings.Current.Appearance;
                Check(HasLabel("Open Personalization"),"Personalization has its own topic.");
                await Click("Manage themes");
                await Click("Open Personalization");
                Check(HasLabel("Hide artwork in settings")&&HasLabel("Menu titles")&&HasLabel("Show artwork button in settings"),"Personalization also exposes the shared artwork/title options");
                await Change("Show artwork button in settings",true);
                Check(settings.Current.ShowConfigurationCoverButton,"Personalization updates the same header preference");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Use custom appearance","onchange"),null,new ChangeEventArgs{Value=true});
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Custom background","oninput"),null,new ChangeEventArgs{Value="#123456"});
                Check(services.GetRequiredService<HitAppearanceService>().Current.Background=="#123456"&&!HasLabel("Apply appearance"),"Personalization must update immediately without an Apply button.");
                await Task.Delay(400);Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance.Background=="#123456","Color changes must save automatically after the short coalescing delay.");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Custom background","oninput"),null,new ChangeEventArgs{Value="#654321"});
                await Click("Undo appearance changes");await Task.Delay(400);Check(settings.Current.Appearance==originalAppearance,"Undo must persist the appearance from editor entry.");
                await interactive.DispatchEventAsync(interactive.Event(id,"input","aria-label","Custom background","oninput"),null,new ChangeEventArgs{Value="#654321"});
                await view.CloseSettingsAsync();
                await Task.Delay(100);Check(settings.Current.Appearance.Background=="#654321","Leaving the editor must flush a pending automatic save rather than cancel it.");
                Check(HasLabel("Open Personalization")&&!HasLabel("Use custom appearance"),"Leaving Personalization returns to the topic list.");
                await view.CloseSettingsAsync();
                services.GetRequiredService<HitAppearanceService>().Apply(originalAppearance,settings.Current.Theme);
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
                Check(!HasLabel("Performance"),"Performance details are not mounted on Info's main screen");
                await Click("Open Performance");
                Check(HasLabel("Performance")&&!HasLabel("Changelog")&&!HasLabel("Back to Info"),"Performance uses the shared header Back instead of an inline Info button");
                await view.CloseSettingsAsync();
                Check(HasLabel("Open Performance"),"Performance returns one level to Info");
                Check(interactive.Graph(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent.Contains("HIT version") || frame.FrameType == RenderTreeFrameType.Markup && frame.MarkupContent.Contains("HIT version")), "Info screen must display version: " + string.Join("; ", interactive.Errors));
                await Click("Changelog");
                Check(!HasLabel("Back to Info")&&!HasLabel("Changelog")&&interactive.Graph(id).Any(frame=>frame.FrameType==RenderTreeFrameType.Attribute&&frame.AttributeName=="data-hit-view"&&Equals(frame.AttributeValue,"changelog")),"Changelog uses its own read-only view with shared header navigation");
                await view.CloseSettingsAsync();
                Check(HasLabel("Changelog"),"Changelog returns one level to Info");
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
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","data-hit-menu-trigger","visualizer","oncontextmenu"),null,new MouseEventArgs{Button=2});
                Check(audio.IsSettingsOpen,"M2 on Visualizer opens its settings topic");
                await audio.NavigateToPageAsync("Lyrics");
                await audio.OpenSettingsAsync();
                Check(audio.CurrentPage=="Settings/Lyrics","Opening configuration from Lyrics title selects Lyrics settings directly");
                await audio.CloseSettingsAsync();await audio.CloseSettingsAsync();
                await audio.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Visualizer","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Enable visualizer","onchange"),null,new ChangeEventArgs {Value=true});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"select","aria-label","Visualizer style","onchange"),null,new ChangeEventArgs {Value="Line"});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Visualizer height","oninput"),null,new ChangeEventArgs {Value="35"});
                var visualizerSaved = new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                Check(visualizerSaved is { AudioVisualizerEnabled: true, AudioVisualizerStyle: AudioVisualizerStyle.Line, AudioVisualizerHeightPercent: 35 }, "Actual visualizer controls immediately save enable/style/height");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"select","aria-label","Visualizer style","onchange"),null,new ChangeEventArgs {Value="999"});
                Check(settings.Current.AudioVisualizerStyle == AudioVisualizerStyle.Line,"Undefined visualizer styles from UI are rejected");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Visualizer height","oninput"),null,new ChangeEventArgs {Value="200"});
                foreach (var style in Enum.GetValues<AudioVisualizerStyle>())
                {
                    await interactive.DispatchEventAsync(interactive.Event(audioId,"select","aria-label","Visualizer style","onchange"),null,new ChangeEventArgs {Value=style.ToString()});
                    var persisted = new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                    Check(persisted.AudioVisualizerStyle==style && persisted.AudioVisualizerHeightPercent==200,"Every style persists with 200 percent height through the actual controls");
                }
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Enable visualizer","onchange"),null,new ChangeEventArgs {Value=false});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Visualizer size","oninput"),null,new ChangeEventArgs {Value="500"});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Visualizer position · Y","oninput"),null,new ChangeEventArgs {Value="-35"});
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Visualizer opacity","oninput"),null,new ChangeEventArgs {Value="73"});
                foreach(var blend in Enum.GetValues<AudioVisualizerBlend>())
                {
                    await interactive.DispatchEventAsync(interactive.Event(audioId,"select","aria-label","Compositing mode","onchange"),null,new ChangeEventArgs {Value=blend.ToString()});
                    var persisted=new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                    Check(persisted.AudioVisualizerBlend==blend&&persisted.AudioVisualizerSizePercent==500
                        &&persisted.AudioVisualizerPositionYPercent==-35&&persisted.AudioVisualizerOpacityPercent==73,"Composition, size, Y and opacity persist through real controls");
                }
                await interactive.DispatchEventAsync(interactive.Event(audioId,"select","aria-label","Compositing mode","onchange"),null,new ChangeEventArgs {Value="999"});
                Check(settings.Current.AudioVisualizerBlend==AudioVisualizerBlend.Exclusion,"Unknown composition from the UI is rejected");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Flip visualizer horizontally","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Flip visualizer vertically","onclick"),null,new MouseEventArgs());
                var flipped=new HitSettingsService(Path.Combine(root,"settings.json")).Current;
                Check(flipped.AudioVisualizerFlipX&&flipped.AudioVisualizerFlipY,"Independent flip buttons persist both axes");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Flip visualizer horizontally","onclick"),null,new MouseEventArgs());
                Check(!settings.Current.AudioVisualizerFlipX&&settings.Current.AudioVisualizerFlipY,"Changing X preserves the Y orientation");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Flip visualizer vertically","onclick"),null,new MouseEventArgs());
                settings.Update(value=>value with {AudioVisualizerBlend=AudioVisualizerBlend.Normal,AudioVisualizerSizePercent=100,AudioVisualizerPositionYPercent=0,AudioVisualizerOpacityPercent=55});
                settings.Update(value => value with { AudioVisualizerStyle=AudioVisualizerStyle.Bars, AudioVisualizerHeightPercent=100 });
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Artwork","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Blend artwork with personal background","onchange"),null,new ChangeEventArgs {Value=true});
                Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.AudioArtworkBlendWithBackground,"Artwork/background blend preference persists independently of opacity");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Blend artwork with personal background","onchange"),null,new ChangeEventArgs {Value=false});
                foreach(var opacity in new[]{0,100,32})
                {
                    await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Artwork over background","oninput"),null,new ChangeEventArgs {Value=opacity.ToString()});
                    Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.AudioArtworkBackgroundOpacity==opacity,"Artwork coverage persists across its full range independently of personal-image opacity");
                }
                await audio.CloseSettingsAsync();
                await audio.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Lyrics","onclick"),null,new MouseEventArgs());
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
                await audio.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Size","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(audioId,"input","aria-label","Audio width","oninput"),null,new ChangeEventArgs{Value=125d});
                Check(settings.Current.AudioScaleX==125,"Visible Size topic applies changes immediately");
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Background effects","onclick"),null,new MouseEventArgs());
                Check(interactive.Graph(audioId).Any(f=>f.FrameType==RenderTreeFrameType.Attribute&&f.AttributeName=="aria-label"&&Equals(f.AttributeValue,"Beat strength")),"Dependent beat strength stays present even when disabled");
                await audio.CloseSettingsAsync();await audio.CloseSettingsAsync();
                var (clockId,clockView)=await interactive.MountAsync<HitMenuClock>();
                await clockView.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(clockId,"button","aria-label","Open Pomodoro","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(clockId,"input","aria-label","Focus minutes","oninput"),null,new ChangeEventArgs{Value=35d});
                Check(settings.Current.Clock.FocusMinutes==35,"Pomodoro preferences are directly reachable and persist");
                await interactive.DispatchEventAsync(interactive.Event(clockId,"button","aria-label","Open World clocks","onclick"),null,new MouseEventArgs());
                await interactive.DispatchEventAsync(interactive.Event(clockId,"button","aria-label","Expand Add clock","onclick"),null,new MouseEventArgs());
                Check(interactive.Graph(clockId).Any(f=>f.FrameType==RenderTreeFrameType.Attribute&&f.AttributeName=="aria-label"&&Equals(f.AttributeValue,"New clock time zone")),"World-clock forms are reachable using M1 from the sidebar");
                await clockView.CloseSettingsAsync();Check(clockView.IsSettingsOpen,"Clock subtopics return to General first");
                await clockView.CloseSettingsAsync();Check(!clockView.IsSettingsOpen,"Clock General returns to its main readout");
                settings.Update(value => value with { AudioLyricsShowTitle = true, AudioLyricsShowSource = true });
                await audio.OpenSettingsAsync();
                await interactive.DispatchEventAsync(interactive.Event(audioId,"button","aria-label","Open Color adjustments","onclick"),null,new MouseEventArgs());
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
                await Save<ImageSettingsView>("images");
                await Save<HitMenuSettings>("settings");
                foreach (var type in new[]{typeof(HitMenuAudio),typeof(HitMenuClock)})
                {
                    var workspace=await html.RenderComponentAsync<BuiltInSettingsPreview>(ParameterView.FromDictionary(new Dictionary<string,object?>{["MenuType"]=type}));
                    await BuiltInSettingsPreview.Instance!.OpenAsync();
                    await File.WriteAllTextAsync(Path.Combine(preview,type==typeof(HitMenuAudio)?"audio-workspace.html":"clock-workspace.html"),workspace.ToHtmlString());
                }
                var example=services.GetRequiredService<ExtensionRuntime>().Menus.First(item=>item.ComponentType.Name=="ExampleMenu");
                HIT.Sdk.IHitMenuSettings? exampleSettings=null;
                var notes=await html.RenderComponentAsync<SampleHost>(ParameterView.FromDictionary(new Dictionary<string,object?>{["Api"]=example.Api,["ComponentType"]=example.ComponentType,["SettingsCaptured"]=(Action<HIT.Sdk.IHitMenuSettings>)(value=>exampleSettings=value)}));
                await exampleSettings!.OpenSettingsAsync();
                await File.WriteAllTextAsync(Path.Combine(preview,"notes-workspace.html"),notes.ToHtmlString());
                await Save<AudioSettingsView>("audio-size",new(){["Settings"]=settings.Current,["Topic"]="Size"});
                await Save<AudioVisualizerView>("visualizer",new(){["Settings"]=settings.Current});
                await Save<AudioSettingsView>("audio-lyrics-settings",new(){["Settings"]=settings.Current,["Topic"]="Lyrics"});
                await Save<HoldItTogether.Components.Clock.ClockSettingsView>("clock-pomodoro",new(){["Options"]=settings.Current.Clock,["Topic"]="Pomodoro"});
                await Save<HoldItTogether.Components.Clock.ClockSystemSettingsView>("clock-world-settings",new(){["Options"]=settings.Current.Clock});
                await Save<WindowPlacementView>("placement",new(){["Settings"]=settings.Current});
                await Save<TitlePresentationSettings>("title-presentation");
                await Save<AppearanceView>("personalization",new(){["ShowBack"]=false});
                await Save<HitPerformanceView>("performance");
                await Save<HitInfoView>("info",new(){["ShowBack"]=false});
                var infoPreview = await html.RenderComponentAsync<InfoPreview>();
                InfoPreview.Instance!.ShowUpdateFeedback();
                await File.WriteAllTextAsync(Path.Combine(preview,"info-updated.html"),infoPreview.ToHtmlString());
                InfoPreview.Instance!.ShowChecking();
                await File.WriteAllTextAsync(Path.Combine(preview,"info-checking.html"),infoPreview.ToHtmlString());
                await InfoPreview.Instance!.ShowDataAsync();
                await File.WriteAllTextAsync(Path.Combine(preview,"info-data.html"),infoPreview.ToHtmlString());
                await Save<HitThemePicker>("themes",new(){["Detailed"]=true});
                await Save<AudioAppearanceView>("artwork",new(){["Settings"]=settings.Current});
                await Save<AudioEffectsView>("effects",new(){["Settings"]=settings.Current});
                await Save<AudioColorView>("colors",new(){["Settings"]=settings.Current});
                await Save<AudioControlsView>("audio-controls",new(){["Settings"]=settings.Current});
                await Save<AudioSettingsView>("audio-settings",new(){["Settings"]=settings.Current with {AudioLyricsTimeline=true}});
                await Save<HitMenuAudio>("player");
                settings.Update(value=>value with {AudioVisualizerEnabled=true,AudioVisualizerStyle=AudioVisualizerStyle.Wave});
                await Save<HitMenuAudio>("player-spectrum");
                settings.Update(value=>value with {AudioVisualizerEnabled=false,AudioVisualizerStyle=AudioVisualizerStyle.Bars});
                await Save<HitMenuClock>("clock");
                var savedClockOptions=settings.Current.Clock;
                settings.Update(s=>s with{Clock=s.Clock with{SelectedMode=HoldItTogether.Services.Clock.ClockMode.Timer}});
                await Save<HitMenuClock>("clock-timer");
                settings.Update(s=>s with{Clock=s.Clock with{SelectedMode=HoldItTogether.Services.Clock.ClockMode.Pomodoro}});
                await Save<HitMenuClock>("clock-pomodoro-main");
                settings.Update(s=>s with{Clock=savedClockOptions});
                var clockPreview=await html.RenderComponentAsync<ClockPlacementPreview>();
                await ClockPlacementPreview.Instance!.CompactAsync();
                await File.WriteAllTextAsync(Path.Combine(preview,"clock-compact.html"),clockPreview.ToHtmlString());
                await Save<AudioLyricsView>("lyrics",new(){["Title"]="A sample track",["Lyrics"]=new SyncedLyrics(Enumerable.Range(0,30).Select(index=>new LyricLine(index*5,"Lyric line "+index)).ToArray()),["Position"]=75d,["CanSeek"]=true});
                await Save<AudioLyricsView>("lyrics-plain",new(){["Title"]="A sample track",["Lyrics"]=new SyncedLyrics(Enumerable.Range(0,35).Select(index=>new LyricLine(0,"Plain verse "+index)).ToArray(),"Lyrics.ovh","https://api.lyrics.ovh/v1/Artist/Song",false),["Position"]=75d,["CanSeek"]=true});
                foreach (var (name, title, source) in new[] { ("lyrics-title",true,false), ("lyrics-source",false,true), ("lyrics-hidden",false,false) })
                {
                    await Save<AudioLyricsView>(name,new(){["Title"]="A sample track",["Lyrics"]=new SyncedLyrics([new(0,"A lyric line")]),["ShowTitle"]=title,["ShowSource"]=source});
                }
            });
        }
        var resetRoot=Path.Combine(root,"reset-profile");
        void Seed()
        {
            foreach(var folder in new[]{"Clock","Themes","Diagnostics","Modules/Installed/test","Modules/State/test","Modules/Logs"})Directory.CreateDirectory(Path.Combine(resetRoot,folder));
            foreach(var file in new[]{"settings.json","window.json","window-fullscreen.json","Clock/state.json","Themes/custom.css","Modules/Installed/test/module.dll","Modules/State/test/data.json","Modules/Logs/test.log"})File.WriteAllText(Path.Combine(resetRoot,file),"test");
            File.WriteAllText(Path.Combine(resetRoot,"Modules/registry.json"),JsonSerializer.Serialize(new ExtensionRegistry{Order=["old"],DisabledMenus=["old"]},ExtensionStore.Json));
        }
        Seed(); var data=new HitDataService(resetRoot); data.Request(false);
        Check(File.Exists(Path.Combine(resetRoot,"settings.json")),"Requesting reset must defer changes until all services stop");
        HitDataService.ApplyPending(resetRoot);
        Check(!File.Exists(Path.Combine(resetRoot,"settings.json"))&&!Directory.Exists(Path.Combine(resetRoot,"Modules/State")),"Settings reset must clear global and module data");
        Check(!File.Exists(Path.Combine(resetRoot,"window.json"))&&!File.Exists(Path.Combine(resetRoot,"window-fullscreen.json")),"Settings reset must clear desktop and fullscreen position memory.");
        Check(!Directory.Exists(Path.Combine(resetRoot,"Diagnostics")),"Settings reset must clear exported local diagnostics.");
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

public sealed class BuiltInSettingsPreview : ComponentBase
{
    [Parameter] public Type MenuType { get; set; } = typeof(HitMenuAudio);
    public static BuiltInSettingsPreview? Instance { get; private set; }
    private HIT.Sdk.IHitMenuSettings? _menu;
    protected override void OnInitialized()=>Instance=this;
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent(0,MenuType);
        builder.AddComponentReferenceCapture(1,value=>_menu=(HIT.Sdk.IHitMenuSettings)value);
        builder.CloseComponent();
    }
    public Task OpenAsync()=>_menu!.OpenSettingsAsync();
}

public sealed class InfoPreview : ComponentBase
{
    public static InfoPreview? Instance { get; private set; }
    private HitInfoView? _view;
    protected override void OnInitialized() => Instance = this;
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<HitInfoView>(0);
        builder.AddAttribute(1, "ShowBack", false);
        builder.AddComponentReferenceCapture(2, component => _view=(HitInfoView)component);
        builder.CloseComponent();
    }
    public Task ShowDataAsync() => _view!.OpenDataAsync();
    public void ShowUpdateFeedback()
    {
        typeof(HitInfoView).GetField("_update",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(_view,new HoldItTogether.Services.Diagnostics.HitUpdateResult("You are using the latest published version."));
        Refresh();
    }
    public void ShowChecking()
    {
        typeof(HitInfoView).GetField("_checking",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.SetValue(_view,true);
        Refresh();
    }
    private void Refresh()
    {
        typeof(ComponentBase).GetMethod("StateHasChanged",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!.Invoke(_view,null);
    }
}
