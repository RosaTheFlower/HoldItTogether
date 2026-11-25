using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class BackgroundImportChecks
{
    public static async Task RunAsync(string root)
    {
        var settings=new HitSettingsService(Path.Combine(root,"import-settings.json"));
        var themes=new HitThemeService(settings,Path.Combine(root,"ImportThemes"),Path.Combine(Environment.CurrentDirectory,"wwwroot/themes"));
        var appearance=new HitAppearanceService(settings,themes,Path.Combine(root,"ImportAppearance"));
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0ZsAAAAASUVORK5CYII=");
        var seed=await appearance.ImportImageAsync(new MemoryStream(png));
        settings.Update(s=>s with{BackgroundMediaLimit=16,Appearance=s.Appearance with{Enabled=true,BackgroundMode="carousel",BackgroundMedia=[seed]}});
        await using var provider=new ServiceCollection().AddLogging().AddSingleton(settings).AddSingleton(themes).AddSingleton(appearance).AddSingleton<IJSRuntime,ImageValidationJs>().BuildServiceProvider();
        await using var renderer=new InteractiveRenderer(provider,provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async()=>{
            var (id,view)=await renderer.MountAsync<AppearanceView>(new(){[nameof(AppearanceView.ShowBack)]=false,[nameof(AppearanceView.Page)]="Background"});
            InputFile Picker()=>renderer.Graph(id).Where(frame=>frame.FrameType==Microsoft.AspNetCore.Components.RenderTree.RenderTreeFrameType.Component&&frame.Component is InputFile).Select(frame=>(InputFile)frame.Component).Single();
            await Picker().OnChange.InvokeAsync(new InputFileChangeEventArgs(Enumerable.Range(0,9).Select(i=>(IBrowserFile)new FileFixture("image.png",png)).ToArray()));
            var selection=appearance.Current.MediaIds;
            if(selection.Length!=10||Directory.GetFiles(appearance.ImageDirectory).Length!=10)throw new Exception("A larger allowed batch must import beyond eight items through the real editor command.");
            var importedCss=appearance.Css(appearance.Current);
            if(importedCss.Contains("--hit-background:")||importedCss.Contains("font-family:")||importedCss.Contains("outline")||importedCss.Contains("box-shadow"))throw new Exception("The editor's import command must leave unrelated CSS theme properties inherited.");
            await Picker().OnChange.InvokeAsync(new InputFileChangeEventArgs([new FileFixture("valid.png",png),new FileFixture("broken.png","invalid file"u8.ToArray())]));
            if(!appearance.Current.MediaIds.SequenceEqual(selection)||Directory.GetFiles(appearance.ImageDirectory).Length!=10)throw new Exception("A failed batch must preserve the selection and remove only its new unused assets.");
            settings.Update(s=>s with{BackgroundMediaLimit=8});
            await Picker().OnChange.InvokeAsync(new InputFileChangeEventArgs([new FileFixture("extra.png",png)]));
            if(!appearance.Current.MediaIds.SequenceEqual(selection)||Directory.GetFiles(appearance.ImageDirectory).Length!=10)throw new Exception("Reducing the limit must reject an append without truncating or deleting existing assets.");
            if(renderer.Errors.Count!=0)throw new Exception("Background import produced renderer errors.");
        });
    }
    private sealed class FileFixture(string name,byte[] bytes):IBrowserFile
    {
        public string Name=>name;
        public DateTimeOffset LastModified=>DateTimeOffset.UnixEpoch;
        public long Size=>bytes.Length;
        public string ContentType=>"image/png";
        public Stream OpenReadStream(long maxAllowedSize=512000,CancellationToken cancellationToken=default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if(Size>maxAllowedSize)throw new IOException("Fixture exceeds size limit.");
            return new MemoryStream(bytes,false);
        }
    }
    // Browser decoding is covered separately by personalization.mjs; this exercises the C# transaction.
    private sealed class ImageValidationJs:IJSRuntime,IJSObjectReference
    {
        public ValueTask<T> InvokeAsync<T>(string identifier,object?[]? args)=>ValueTask.FromResult(identifier=="import"?(T)(object)this:identifier=="validateImage"?(T)(object)true:default!);
        public ValueTask<T> InvokeAsync<T>(string identifier,CancellationToken token,object?[]? args){token.ThrowIfCancellationRequested();return InvokeAsync<T>(identifier,args);}
        public ValueTask DisposeAsync()=>ValueTask.CompletedTask;
    }
}
