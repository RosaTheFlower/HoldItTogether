using System.IO.Compression;
using System.Text.Json;

namespace HoldItTogether.Services.Extensions;

/// <summary>Imports an explicitly selected installer package before any module code is loaded.</summary>
internal static class ExtensionPackageBootstrap
{
    public static async Task InstallAsync(ExtensionStore store,string packagePath)
    {
        await using var stream=File.OpenRead(Path.GetFullPath(packagePath));
        if(stream.Length>ExtensionStore.MaxPackageBytes)throw new InvalidDataException("The package is too large.");
        using(var archive=new ZipArchive(stream,ZipArchiveMode.Read,true))
        {
            var entry=archive.GetEntry("manifest.json")??throw new InvalidDataException("Missing manifest.json.");
            if(entry.Length>65536)throw new InvalidDataException("Oversized manifest.json.");
            await using var input=entry.Open();
            var manifest=await JsonSerializer.DeserializeAsync<ExtensionManifest>(input,ExtensionStore.Json)??throw new InvalidDataException("Invalid manifest.");
            manifest.Validate();
            var existing=store.State.Packages.FirstOrDefault(item=>item.Manifest.Id==manifest.Id&&!item.PendingDelete);
            if(existing is not null&&Version.Parse(existing.Manifest.Version)>=Version.Parse(manifest.Version))return;
        }
        stream.Position=0;
        // Use the standard package validator, storage boundaries and version checks.
        await store.InstallAsync(stream,Path.GetFileName(packagePath));
    }
    public static async Task<int> RunAsync(string[] args)
    {
        if(args.Length is not (2 or 4)||args[0]!="--hit-install-package"||
            (args.Length==4&&args[2]!="--hit-module-root"))return 2;
        try
        {
            var store=args.Length==4?new ExtensionStore(args[3]):new ExtensionStore();
            store.FinishPendingDeletes();
            await InstallAsync(store,args[1]);
            return 0;
        }
        catch(Exception error)when(error is IOException or UnauthorizedAccessException or InvalidDataException or InvalidOperationException)
        {
            Console.Error.WriteLine("Package installation failed: "+error.Message);
            return 1;
        }
    }
}
