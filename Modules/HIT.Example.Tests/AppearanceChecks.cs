using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Components.Settings;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class AppearanceChecks
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static async Task RunAsync(string repo)
    {
        var root=Path.GetFullPath("obj/appearance-tests/"+Guid.NewGuid().ToString("N"));
        var settings=new HitSettingsService(Path.Combine(root,"settings.json"));
        var themes=new HitThemeService(settings,Path.Combine(root,"Themes"),Path.Combine(repo,"wwwroot/themes"));
        var service=new HitAppearanceService(settings,themes,Path.Combine(root,"Appearance"));
        var original=settings.Current;
        var custom=new HitAppearancePreferences{Enabled=true,Background="#123456",Accent="#ff8800",Text="#eeeeee",Border="#abcdef",Radius=12,BorderWidth=2,AllowArtworkColors=false};
        service.Preview(custom,"default");
        Check(service.Current==custom&&settings.Current==original,"Preview must not persist preferences.");
        service.Cancel();Check(service.Current==original.Appearance&&service.PreviewTheme is null,"Cancel must restore the saved appearance and theme.");
        service.Apply(custom,"default");
        Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance==custom,"Applied appearance must survive restart.");
        var one=service.Save("My colors",custom,"default");
        var two=service.Save("My colors copy",one.Appearance,one.Theme);
        Check(one.Id!=two.Id&&service.Profiles().Length==2,"Profile copies must be independent entries.");
        Check(new HitAppearanceService(settings,themes,Path.Combine(root,"Appearance")).Profiles().Length==2,"Visual profiles must survive restart.");
        var safe=(custom with{Background="red;}body{display:none",Radius=999,BorderWidth=-1}).Normalize();
        Check(safe.Background=="#17191e"&&safe.Radius==48&&safe.BorderWidth==0,"Color/CSS injection and geometry must normalize safely.");
        Check(new HitAppearancePreferences().Css()=="","Disabled personalization must add no CSS.");
        var exported=service.Export(custom,"default");
        await using(var source=File.OpenRead(exported)){var id=await themes.ImportAsync("Exported.css",source);themes.Select(id);Check(themes.ReadCurrent().Css.Contains("#123456"),"Exported CSS must import through the normal theme path.");}
        Directory.CreateDirectory("obj/appearance-tests");File.Copy(exported,"obj/appearance-tests/export.css",true);
        File.WriteAllText("obj/appearance-tests/artwork-allowed.css",(custom with{AllowArtworkColors=true}).Css());
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0ZsAAAAASUVORK5CYII=");
        var image=await service.ImportImageAsync(new MemoryStream(png));
        var provider=new AppearanceImageFileProvider(service);
        Check(provider.GetFileInfo("appearance-images/"+image).Exists&&!provider.GetFileInfo("appearance-images/../profiles.json").Exists,"Only validated image IDs may be served; profile data must not be exposed.");
        var imagePreferences=custom with{BackgroundImage=image,ImageOpacity=55,ImageFit="contain",Eyes="#112233",EyeSize=12,EyeGap=14,ControlRadius=16,ShadowStrength=0,BorderStyle="dashed",Font="mono"};
        service.Apply(imagePreferences,"default");Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance==imagePreferences,"Extended appearance and image references must persist.");
        var imageExport=service.Export(imagePreferences,"default");
        Check(File.ReadAllText(imageExport).Contains("data:image/png;base64,"),"Export must embed local images rather than depend on machine-local paths.");
        File.Copy(imageExport,"obj/appearance-tests/background.css",true);
        File.WriteAllText("obj/appearance-tests/background-allowed.css", service.Css(imagePreferences with {AllowArtworkColors=true}, true));
        var secondImage = await service.ImportImageAsync(new MemoryStream(png));
        var slideshow = imagePreferences with { BackgroundImages = secondImage, BackgroundMode = "smooth", BackgroundIntervalSeconds = 5, BackgroundTransitionMs = 700, BackgroundTransition = "zoom" };
        service.Apply(slideshow,"default");
        var restored = new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance;
        Check(restored == slideshow && restored.ImageIds.SequenceEqual(new[]{image,secondImage}),"Slideshow preferences and image order must persist.");
        var bounded = (slideshow with { BackgroundIntervalSeconds = 0, BackgroundTransitionMs = 99999, BackgroundMode = "bad", BackgroundTransition = "bad", BackgroundImages = "../invalid.png," + secondImage }).Normalize();
        Check(bounded is { BackgroundIntervalSeconds:5,BackgroundTransitionMs:4000,BackgroundMode:"single",BackgroundTransition:"fade" } && bounded.ImageIds.Length == 2,"Slideshow limits and IDs normalize safely.");
        var slideshowExport = service.Export(slideshow,"default");
        Check(service.Css(slideshow with {Enabled=false},true)=="","Disabled appearance must not export slideshow overrides.");
        await using (var source = File.OpenRead(slideshowExport)) await themes.ImportAsync("Slideshow.css",source);
        File.Copy(slideshowExport,"obj/appearance-tests/slideshow.css",true);
        Check(File.ReadAllText(slideshowExport).Contains("@keyframes hit-export-background") && !File.ReadAllText(slideshowExport).Contains("appearance-images/"),"Slideshow CSS must be portable and pass normal theme import.");
        var gif = await service.ImportImageAsync(new MemoryStream(Convert.FromBase64String("R0lGODlhAQABAIAAAAAAAP///yH5BAEAAAAALAAAAAABAAEAAAIBRAA7")));
        Check(gif.EndsWith(".gif") && provider.GetFileInfo("appearance-images/"+gif).Exists, "GIF signatures and local serving are supported.");
        settings.Update(s=>s with{AllowGifBackgrounds=false});
        Check(service.ImageUrl(gif) is null&&!provider.GetFileInfo("appearance-images/"+gif).Exists,"Disabling GIF backgrounds suppresses managed GIF assets without deleting references");
        try{await using var gifSource=File.OpenRead(Path.Combine(service.ImageDirectory,gif));await service.ImportImageAsync(gifSource);throw new Exception("Disabled GIF import accepted.");}catch(InvalidDataException){}
        settings.Update(s=>s with{AllowGifBackgrounds=true});
        Check(service.ImageUrl(gif) is not null,"Re-enabling GIF backgrounds restores retained assets");
        Check(service.Css(imagePreferences with { BackgroundImage=gif },true).Contains("data:image/gif;base64,"),"GIF export preserves its original bytes and MIME type.");
        Check(service.Css(imagePreferences with { BackgroundWhenCollapsed=false }).Contains(".dynamic-sphere:not(.is-open) > .sphere-custom-backdrop{display:none!important;}"),"Collapsed-background visibility is independently configurable.");
        var largeImage = new byte[3_000_000]; png.CopyTo(largeImage,0);
        service.Preview(imagePreferences with { BackgroundImageLimitMb=.25 },"default");
        Check(service.ImageByteLimit==250_000,"The minimum image limit is exactly 250 KB.");
        try { await service.ImportImageAsync(new MemoryStream(largeImage)); throw new Exception("Configured image limit was ignored."); } catch (InvalidDataException) { }
        Check(!Directory.EnumerateFiles(service.ImageDirectory,"*.tmp").Any(),"Rejected streaming imports leave no temporary files.");
        service.Apply(imagePreferences with { BackgroundImageLimitMb=null },"default");
        Check(new HitSettingsService(Path.Combine(root,"settings.json")).Current.Appearance.BackgroundImageLimitMb is null && service.ImageByteLimit==long.MaxValue,"Unlimited size persists without a hidden finite cap.");
        var unlimitedImage = await service.ImportImageAsync(new MemoryStream(largeImage));
        Check(new FileInfo(Path.Combine(service.ImageDirectory,unlimitedImage)).Length==largeImage.Length,"Unlimited imports stream original bytes beyond the old 2 MB limit.");
        Check((imagePreferences with {BackgroundImageLimitMb=.01}).Normalize().BackgroundImageLimitMb==.25,"Finite limits normalize to at least 250 KB.");
        service.Preview(slideshow,"default");
        var resources = new HitResourcePolicy();
        await using (var renderingServices = new ServiceCollection().AddLogging().AddSingleton(service).AddSingleton(settings).AddSingleton(resources).BuildServiceProvider())
        await using (var renderer = new HtmlRenderer(renderingServices,renderingServices.GetRequiredService<ILoggerFactory>()))
        {
            var view = await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<CustomBackground>(ParameterView.FromDictionary(new Dictionary<string,object?> { [nameof(CustomBackground.Active)] = true })));
            var first = await renderer.Dispatcher.InvokeAsync(view.ToHtmlString);
            Check(first.Contains(image) && !first.Contains(secondImage),"Live carousel initially loads only its current frame.");
            await Task.Delay(5200);
            var advanced = await renderer.Dispatcher.InvokeAsync(view.ToHtmlString);
            Check(advanced.Contains(image) && advanced.Contains(secondImage),"Live carousel advances into a two-frame transition.");
            await renderer.Dispatcher.InvokeAsync(() => { resources.Update(true,true,false,false,0); resources.Update(true,true,false,false,5001); });
            var sleeping = await renderer.Dispatcher.InvokeAsync(view.ToHtmlString);
            await Task.Delay(5200);
            Check(sleeping == await renderer.Dispatcher.InvokeAsync(view.ToHtmlString),"Sleep cancels slideshow switching and retains the current frame.");
            await renderer.Dispatcher.InvokeAsync(resources.Wake);
            await Task.Delay(5200);
            Check((await renderer.Dispatcher.InvokeAsync(view.ToHtmlString)).Contains(image),"Wake resumes slideshow switching.");
        }
        var invalid=(imagePreferences with{BackgroundImage="../../secret.png",EyeSize=500,EyeGap=-5,ControlRadius=100,ShadowStrength=500,Font="bad;font",BorderStyle="url(bad)",ImageOpacity=200,ImageFit="bad"}).Normalize();
        Check(invalid is{BackgroundImage:null,EyeSize:16,EyeGap:0,ControlRadius:24,ShadowStrength:150,Font:"system",BorderStyle:"solid",ImageOpacity:100,ImageFit:"cover"},"Extended options must clamp and reject unsafe CSS/path values.");
        try{await service.ImportImageAsync(new MemoryStream(new byte[HitAppearanceService.MaxImageBytes+1]));throw new Exception("Oversized image accepted.");}catch(InvalidDataException){}
        try{await service.ImportImageAsync(new MemoryStream("not an image"u8.ToArray()));throw new Exception("Invalid image accepted.");}catch(InvalidDataException){}
        try{service.Save(" ",custom,"default");throw new Exception("Empty profile name accepted.");}catch(ArgumentException){}
        Check(service.ValidTheme("custom:missing.css")=="default","Missing profile base themes must safely fall back.");
        Console.WriteLine("PASS: appearance preview/cancel/apply, persisted independent profiles, safe CSS normalization, theme export/reimport and missing-base fallback.");
    }
}
