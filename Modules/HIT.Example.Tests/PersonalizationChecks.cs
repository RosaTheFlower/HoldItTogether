using HoldItTogether.Components.HitMenus;
using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class PersonalizationChecks
{
    private static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
    public static async Task RunAsync(string repo)
    {
        await AppearanceChecks.RunAsync(repo);
        await SelectiveAppearanceChecks.RunAsync(repo);
        await ClosedSphereChecks.RunAsync(repo);
        var root=Path.GetFullPath("obj/personalization-tests/"+Guid.NewGuid().ToString("N"));
        var settings=new HitSettingsService(Path.Combine(root,"settings.json"));
        var themes=new HitThemeService(settings,Path.Combine(root,"Themes"),Path.Combine(repo,"wwwroot/themes"));
        var appearance=new HitAppearanceService(settings,themes,Path.Combine(root,"Appearance"));
        Check(!settings.Current.PersonalizationListView,"Categories must be the default.");
        foreach(var hex in new[]{"#123456","#12345600","#12345680","#123456ff"})
        {
            Check(HitColor.TryParse(hex,out var color),"Six/eight digit HEX must be accepted.");
            var (h,s,v)=color.Hsv();var restored=HitColor.FromHsv(h,s,v,color.Alpha);
            Check(restored==color,"HSV conversion must retain RGBA channels.");
        }
        Check(!HitColor.TryParse("#123456;display:none",out _),"Invalid CSS must not be accepted as a color.");
        var alpha=new HitAppearancePreferences{Enabled=true,Background="#12345680",Eyes="#abcdef00"};
        appearance.Apply(alpha,"default");
        Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance.Background=="#12345680","Alpha must survive restart.");
        Check(appearance.Css(alpha).Contains("#12345680"),"Alpha must be emitted in CSS.");
        var details=alpha with{TitleSize=18,TitleWeight=700,TitleLetterSpacing=1.5,EyeHeight=12,EyeRoundness=20,ShadowBlur=40,ShadowOffset=15,BackgroundPositionX=20,BackgroundPositionY=80,BackgroundBlur=4,BackgroundSaturation=65};
        appearance.Apply(details,"default");
        Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance==details,"New details must survive restart.");
        var detailProfile=appearance.Save("Details",details,"default");
        Check(appearance.Profiles().Single().Appearance==details,"Profiles must retain new details.");
        var normalized=(details with{TitleSize=999,TitleWeight=-100,TitleLetterSpacing=double.NaN,EyeHeight=100,EyeRoundness=-5,ShadowBlur=-1,ShadowOffset=999,BackgroundPositionX=-1,BackgroundPositionY=101,BackgroundBlur=99,BackgroundSaturation=999}).Normalize();
        Check(normalized.TitleSize==24&&normalized.TitleWeight==300&&normalized.TitleLetterSpacing==0&&normalized.EyeHeight==24&&normalized.EyeRoundness==0&&normalized.ShadowBlur==0&&normalized.ShadowOffset==30&&normalized.BackgroundPositionX==0&&normalized.BackgroundPositionY==100&&normalized.BackgroundBlur==30&&normalized.BackgroundSaturation==200,"Invalid detail values must normalize at the service boundary.");
        Directory.CreateDirectory("obj/personalization-tests");
        await File.WriteAllTextAsync("obj/personalization-tests/details.css",appearance.Css(details));
        appearance.Apply(alpha,"default");
        Directory.CreateDirectory("obj/personalization-tests");
        await File.WriteAllTextAsync("obj/personalization-tests/alpha.css",appearance.Css(alpha));
        var bytes=new byte[64];System.Text.Encoding.ASCII.GetBytes("ftyp").CopyTo(bytes,4);
        var video=await appearance.ImportVideoAsync(new MemoryStream(bytes));
        var provider=new AppearanceImageFileProvider(appearance);
        Check(provider.GetFileInfo("appearance-videos/"+video).Exists,"Validated video IDs must be served.");
        Check(!provider.GetFileInfo("appearance-videos/../settings.json").Exists,"Video assets must not expose arbitrary paths.");
        try{await appearance.ImportVideoAsync(new MemoryStream("invalid video"u8.ToArray()));throw new Exception("Invalid video accepted.");}catch(InvalidDataException){}
        settings.Update(s=>s with{BackgroundVideoLimitMb=.25});
        var oversized=new byte[250001];bytes.CopyTo(oversized,0);
        try{await appearance.ImportVideoAsync(new MemoryStream(oversized));throw new Exception("Video limit ignored.");}catch(InvalidDataException){}
        Check(!Directory.EnumerateFiles(appearance.VideoDirectory,"*.tmp").Any(),"Failed video imports must remove temporary files.");
        var poster="data:image/jpeg;base64,"+Convert.ToBase64String(new byte[]{255,216,255,217});
        await appearance.SaveVideoPosterAsync(video,poster);
        var mixed=alpha with{BackgroundMedia=[video],BackgroundMode="single"};
        appearance.Apply(mixed,"default");
        Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance.MediaIds.SequenceEqual(new[]{video}),"Video selections must survive restart.");
        var profile=appearance.Save("Video profile",mixed,"default");
        Check(appearance.Profiles().Single(p=>p.Id==profile.Id).Appearance.MediaIds.SequenceEqual(profile.Appearance.MediaIds),"Profiles must retain video selections.");
        try{appearance.Export(mixed,"default");throw new Exception("Video export skipped confirmation.");}catch(InvalidOperationException){}
        var exported=appearance.Export(mixed,"default",true);
        Check(File.ReadAllText(exported).Contains("data:image/jpeg;base64,")&&!File.ReadAllText(exported).Contains("appearance-videos/"),"Confirmed video exports must embed a portable fixed frame.");
        var image=new string('a',32)+".png";
        Check((new HitAppearancePreferences{BackgroundImage=image}).MediaIds.SequenceEqual(new[]{image}),"Absent media list must load legacy images.");
        Check((new HitAppearancePreferences{BackgroundImage=image,BackgroundMedia=[]}).Normalize().MediaIds.Length==0,"An explicitly empty list must not resurrect legacy images.");
        await using var services=new ServiceCollection().AddLogging().AddSingleton(settings).AddSingleton(themes).AddSingleton(appearance).AddSingleton<ExtensionUi>().AddSingleton<IJSRuntime,NoJs>().BuildServiceProvider();
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var (id,view)=await renderer.MountAsync<HitMenuSettings>();
                await view.NavigateToPageAsync("Personalization/");
                Check(!view.CanGoBack&&view.IsPersonalizationOverview,"Category overview is a root page with the master switch.");
                await view.NavigateToPageAsync("Personalization/Colors");
                Check(view.CanGoBack&&view.CurrentPage=="Personalization/Colors","Categories must participate in page history.");
                await view.TogglePreviewAsync();Check(view.IsPreviewHidden,"Preview must hide the UI.");
                await view.CloseSettingsAsync();Check(!view.IsPreviewHidden&&view.CurrentPage=="Personalization/Colors","First Escape must restore the same page.");
                await view.CloseSettingsAsync();Check(view.CurrentPage=="Personalization/","Back must return to the category overview.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Use custom appearance","onchange"),null,new ChangeEventArgs{Value=false});
                Check(!view.AppearanceEnabled&&!appearance.Current.Enabled,"The header switch disables preview without losing its values.");
                await Task.Delay(350);
                Check(!new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance.Enabled,"Disabling from the header must save automatically.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Use custom appearance","onchange"),null,new ChangeEventArgs{Value=true});
                Check(view.AppearanceEnabled&&appearance.Current.Enabled,"The header switch restores custom appearance.");
                settings.Update(s=>s with{PersonalizationListView=true});
                await view.NavigateToPageAsync("Personalization/Colors");
                Check(view.CanGoBack,"Legacy list preferences must not bypass category navigation.");
                await view.NavigateToPageAsync("Personalization/Shape and typography");
                await renderer.DispatchEventAsync(renderer.Event(id,"select","aria-label","Font family","onchange"),null,new ChangeEventArgs{Value="custom"});
                Check(view.CurrentPage=="Personalization/Shape and typography","Preview/restoration changes must not open custom fonts.");
                await renderer.DispatchEventAsync(renderer.Event(id,"select","aria-label","Font family","onchange"),null,new ChangeEventArgs{Value="arial"});
                await renderer.DispatchEventAsync(renderer.Event(id,"select","aria-label","Font family","oninput"),null,new ChangeEventArgs{Value="custom"});
                Check(view.CurrentPage=="Personalization/Custom font","Only confirming the Custom font action opens its page.");
                Check(appearance.Current.Font=="arial","Opening the chooser keeps the applied font until a family is chosen.");
                await view.CloseSettingsAsync();
                Check(view.CurrentPage=="Personalization/Shape and typography","Installed fonts returns to its typography parent.");
                await view.TogglePreviewAsync();await view.NavigateToPageAsync("Main");Check(!view.IsPreviewHidden&&!view.IsPersonalizing,"Leaving personalization must reset preview.");
                await view.SetParametersAsync(ParameterView.Empty);
                Check(renderer.Errors.Count==0,"Personalization navigation must not produce rendering errors: "+string.Join(";",renderer.Errors));
            });
        }
        settings.Update(s=>s with{PersonalizationListView=true});
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var original=settings.Current.Appearance;
                var (id,view)=await renderer.MountAsync<AppearanceView>(new(){[nameof(AppearanceView.ShowBack)]=false,[nameof(AppearanceView.Page)]="Shape and typography"});
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Interface text size","oninput"),null,new ChangeEventArgs{Value="17"});
                await view.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(AppearanceView.Page)]="Shape and typography"}));
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Title size","oninput"),null,new ChangeEventArgs{Value="20"});
                Check(appearance.Current.TextSize==17&&appearance.Current.TitleSize==20,"Category changes retain the same draft and its new details.");
                await view.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(AppearanceView.Page)]=""}));
                await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label","Undo appearance changes","onclick"),null,new MouseEventArgs());
                Check(System.Text.Json.JsonSerializer.Serialize(appearance.Current)==System.Text.Json.JsonSerializer.Serialize(original),"Session Undo restores all values and ordered media across categories.");
                Check(renderer.Errors.Count==0,"Detail editing must not produce renderer errors.");
            });
        }
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var (id,view)=await renderer.MountAsync<AppearanceView>(new(){[nameof(AppearanceView.ShowBack)]=false,[nameof(AppearanceView.Page)]="Visual profiles"});
                async Task Click(string label)=>await renderer.DispatchEventAsync(renderer.Event(id,"button","aria-label",label,"onclick"),null,new MouseEventArgs());
                var before=Directory.GetFiles(Path.GetDirectoryName(exported)!,"*.css").Length;
                await Click("Export CSS");
                Check(Directory.GetFiles(Path.GetDirectoryName(exported)!,"*.css").Length==before,"Opening export confirmation must not create CSS.");
                await Click("Cancel video CSS export");
                await Click("Export with fixed frames");
                Check(Directory.GetFiles(Path.GetDirectoryName(exported)!,"*.css").Length==before,"Canceled export must not allow a stale confirm action.");
                await Click("Export CSS");await Click("Export with fixed frames");
                Check(Directory.GetFiles(Path.GetDirectoryName(exported)!,"*.css").Length==before+1,"Confirmed export creates exactly one CSS file.");
                Check(renderer.Errors.Count==0,"Export confirmation must not produce renderer errors.");
            });
        }
        await using(var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var changed=new List<string>();
                var (id,picker)=await renderer.MountAsync<SettingColor>(new(){[nameof(SettingColor.Value)]="#12345680",[nameof(SettingColor.Changed)]=EventCallback.Factory.Create<string>(changed,(string value)=>changed.Add(value))});
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","HEX color","oninput"),null,new ChangeEventArgs{Value="#12"});
                Check(changed.Count==0,"Incomplete HEX must not overwrite the last valid color.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","HEX color","oninput"),null,new ChangeEventArgs{Value="#aabbcc00"});
                Check(changed.Last()=="#aabbcc00","Fully transparent colors must be editable.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Alpha","oninput"),null,new ChangeEventArgs{Value="100"});
                Check(changed.Last()=="#aabbcc","Opaque colors must retain the legacy six-digit format.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","R color channel","oninput"),null,new ChangeEventArgs{Value="255"});
                Check(changed.Last()=="#ffbbcc","RGB byte fields publish the selected channels.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Hue","oninput"),null,new ChangeEventArgs{Value="0"});
                Check(HitColor.TryParse(changed.Last(),out var red)&&red.Red==255&&red.Green==red.Blue,"Hue selection updates RGB without losing brightness.");
                await renderer.DispatchEventAsync(renderer.Event(id,"input","aria-label","Alpha","oninput"),null,new ChangeEventArgs{Value="50"});
                Check(HitColor.TryParse(changed.Last(),out var half)&&half.Alpha==128,"The opacity track retains RGBA precision.");
                Check(renderer.Errors.Count==0,"Color interactions must not produce renderer errors.");
            });
        }
        var saved=settings.Current;
        await SphereSettingsChecks.RunAsync(settings,appearance,services,root);
        await ExpandedAppearanceChecks.RunAsync(settings,appearance,services,root);
        settings.Update(_=>saved);appearance.Cancel();
        await using(var renderer=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>()))
        {
            await renderer.Dispatcher.InvokeAsync(async()=>{
                Directory.CreateDirectory("obj/personalization-tests");
                var workspace=await renderer.RenderComponentAsync<BuiltInSettingsPreview>(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(BuiltInSettingsPreview.MenuType)]=typeof(HitMenuSettings)}));
                foreach(var page in new[]{"","General","Colors","Background","Shape and typography","Custom font","Visual profiles"})
                {
                    var view=await renderer.RenderComponentAsync<AppearanceView>(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(AppearanceView.ShowBack)]=false,[nameof(AppearanceView.Page)]=page}));
                    await File.WriteAllTextAsync("obj/personalization-tests/"+(page.Length==0?"overview":page.ToLowerInvariant().Replace(' ','-'))+".html",view.ToHtmlString());
                    await BuiltInSettingsPreview.Instance!.NavigateAsync("Personalization/"+page);
                    await File.WriteAllTextAsync("obj/personalization-tests/workspace-"+(page.Length==0?"overview":page.ToLowerInvariant().Replace(' ','-'))+".html",workspace.ToHtmlString());
                }
                foreach(var page in new[]{""}.Concat(SphereSettingsView.Pages))
                {
                    await BuiltInSettingsPreview.Instance!.NavigateAsync("Sphere/"+page);
                    await File.WriteAllTextAsync("obj/personalization-tests/workspace-sphere-"+(page.Length==0?"overview":page.ToLowerInvariant().Replace(' ','-'))+".html",workspace.ToHtmlString());
                }
            });
        }
        Console.WriteLine("PASS: RGBA/HSV, detail normalization/profile persistence, legacy media, video/poster export, category navigation, master switch autosave and transient preview.");
    }
}
