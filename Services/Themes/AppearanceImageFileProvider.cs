using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.FileProviders.Physical;
using Microsoft.Extensions.Primitives;
namespace HoldItTogether.Services.Themes;
public sealed class AppearanceImageFileProvider(HitAppearanceService appearance):IFileProvider
{
    public IFileInfo GetFileInfo(string subpath)
    {
        var parts=subpath.TrimStart('/').Split('/');
        if(parts.Length==2&&parts[0]=="appearance-videos")
        {
            var id=parts[1];
            if(!HitAppearanceService.IsVideo(id)&&!(id.EndsWith(".jpg",StringComparison.Ordinal)&&HitAppearanceService.IsVideo(id[..^4])))return new NotFoundFileInfo(subpath);
            var video=new FileInfo(Path.Combine(appearance.VideoDirectory,id));
            return video.Exists&&(video.Attributes&FileAttributes.ReparsePoint)==0&&(new DirectoryInfo(appearance.VideoDirectory).Attributes&FileAttributes.ReparsePoint)==0?new PhysicalFileInfo(video):new NotFoundFileInfo(subpath);
        }
        if(parts.Length!=2||parts[0]!="appearance-images"||!HitAppearanceService.ValidImageId(parts[1]))return new NotFoundFileInfo(subpath);
        if(!appearance.GifBackgroundsAllowed&&parts[1].EndsWith(".gif",StringComparison.Ordinal))return new NotFoundFileInfo(subpath);
        var file=new FileInfo(Path.Combine(appearance.ImageDirectory,parts[1]));
        return file.Exists&&(file.Attributes&FileAttributes.ReparsePoint)==0&&(new DirectoryInfo(appearance.ImageDirectory).Attributes&FileAttributes.ReparsePoint)==0?new PhysicalFileInfo(file):new NotFoundFileInfo(subpath);
    }
    public IDirectoryContents GetDirectoryContents(string subpath)=>NotFoundDirectoryContents.Singleton;
    public IChangeToken Watch(string filter)=>NullChangeToken.Singleton;
}
