using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace HoldItTogether.Services.Extensions;

public sealed class ExtensionFileProvider(ExtensionStore store) : IFileProvider
{
    private static readonly HashSet<string> AssetTypes = new(StringComparer.OrdinalIgnoreCase)
    { ".css", ".js", ".json", ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg", ".woff", ".woff2", ".ttf", ".otf", ".mp3", ".wav" };
    public IFileInfo GetFileInfo(string subpath)
    {
        var parts = subpath.TrimStart('/').Split('/', 4);
        if (parts.Length != 4 || parts[0] != "extensions" || !ExtensionManifest.ValidId(parts[1]) ||
            !AssetTypes.Contains(Path.GetExtension(parts[3])) || parts[3] == "manifest.json") return new NotFoundFileInfo(subpath);
        var package = store.State.Packages.FirstOrDefault(item => item.Manifest.Id == parts[1]);
        if (package is null) return new NotFoundFileInfo(subpath);
        // Previous versions remain addressable until restart, while their component is mounted.
        if (!Version.TryParse(parts[2], out _) || parts[2].Any(c => !char.IsAsciiDigit(c) && c != '.')) return new NotFoundFileInfo(subpath);
        try
        {
            var root = Path.Combine(store.Root, "Installed", parts[1], parts[2]);
            var path = ExtensionManifest.SafePath(root, parts[3]);
            if (!File.Exists(path)) return new NotFoundFileInfo(subpath);
            for (var current = new FileInfo(path) as FileSystemInfo; current is not null; current = current is FileInfo file ? file.Directory : ((DirectoryInfo)current).Parent)
            {
                if ((current.Attributes & FileAttributes.ReparsePoint) != 0) return new NotFoundFileInfo(subpath);
                if (current.FullName == store.Root) break;
            }
            return new Microsoft.Extensions.FileProviders.Physical.PhysicalFileInfo(new FileInfo(path));
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or ArgumentException) { return new NotFoundFileInfo(subpath); }
    }
    public IDirectoryContents GetDirectoryContents(string subpath) => NotFoundDirectoryContents.Singleton;
    public IChangeToken Watch(string filter) => NullChangeToken.Singleton;
}
