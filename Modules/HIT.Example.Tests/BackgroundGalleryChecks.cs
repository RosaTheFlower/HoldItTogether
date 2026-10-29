using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

internal static class BackgroundGalleryChecks
{
    public static async Task RunAsync(IServiceProvider services)
    {
        var settings=services.GetRequiredService<HitSettingsService>();
        var appearance=services.GetRequiredService<HitAppearanceService>();
        var original=settings.Current;
        var png=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0ZsAAAAASUVORK5CYII=");
        var first=await appearance.ImportImageAsync(new MemoryStream(png));
        var second=await appearance.ImportImageAsync(new MemoryStream(png));
        settings.Update(value=>value with{Appearance=value.Appearance with{Enabled=true,BackgroundImage=first,BackgroundImages=second}});
        try
        {
            await using var html=new HtmlRenderer(services,services.GetRequiredService<ILoggerFactory>());
            await html.Dispatcher.InvokeAsync(async()=>{
                var preview=await html.RenderComponentAsync<BackgroundImageGallery>(ParameterView.FromDictionary(new Dictionary<string,object?>{[nameof(BackgroundImageGallery.Images)]=new[]{first,second}}));
                Directory.CreateDirectory("obj/menu-tests");
                var markup=preview.ToHtmlString().Replace("appearance-images/"+first,"data:image/png;base64,"+Convert.ToBase64String(png)).Replace("appearance-images/"+second,"data:image/png;base64,"+Convert.ToBase64String(png));
                await File.WriteAllTextAsync("obj/menu-tests/background-gallery.html",markup);
            });
            await using var renderer=new InteractiveRenderer(services,services.GetRequiredService<ILoggerFactory>());
            await renderer.Dispatcher.InvokeAsync(async()=>{
                var (id,view)=await renderer.MountAsync<AppearanceView>(new(){[nameof(AppearanceView.ShowBack)]=false});
                async Task Click(string label,string attribute="aria-label")=>await renderer.DispatchEventAsync(renderer.Event(id,"button",attribute,label,"onclick"),null,new MouseEventArgs());
                void Check(bool value,string message){if(!value)throw new Exception(message);}
                await Click("Select background image 1");
                await Click("Remove selected image");
                Check(appearance.Current.ImageIds.Length==2,"First trash click must not delete an image.");
                await Click("Confirm image removal");
                Check(appearance.Current.ImageIds.SequenceEqual(new[]{second}),"Second trash click removes only the selected image.");
                await Click("Remove all images","title");
                await Click("Cancel removing all images");
                await Click("Confirm removing all images");
                Check(appearance.Current.ImageIds.Length==1,"Canceling Remove all preserves the image selection.");
                await Click("Remove all images","title");
                await Click("Confirm removing all images");
                Check(appearance.Current.ImageIds.Length==0,"Confirmed Remove all clears the image selection.");
                Check(File.Exists(Path.Combine(appearance.ImageDirectory,first))&&File.Exists(Path.Combine(appearance.ImageDirectory,second)),"Removing selections must preserve assets needed by Undo and profiles.");
                Check(renderer.Errors.Count==0,"Gallery interactions must not produce renderer errors.");
            });
        }
        finally { settings.Update(_=>original); appearance.Cancel(); }
        Console.WriteLine("PASS: image gallery selection, two-click individual removal, bulk confirmation/cancel and retained Undo/profile assets.");
    }
}
