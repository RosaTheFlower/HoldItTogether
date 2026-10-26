using System.Reflection;
using System.Runtime.Loader;
using HIT.Sdk;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.Extensions.DependencyInjection;

namespace HoldItTogether.Services.Extensions;

public sealed record LoadedMenu(string Id, string Name, Type ComponentType, ExtensionApi Api, string[] Styles);

public sealed class ExtensionRuntime(ExtensionStore store, IHitMediaService media, HitClockService clock,
    HitSettingsService settings, HitThemeService themes, IExternalLinkService links, ExtensionUi ui) : IAsyncDisposable
{
    private readonly List<(ExtensionLoadContext Load, IHitModule Module, ServiceProvider Services, CancellationTokenSource Lifetime)> _modules = [];
    private readonly CancellationTokenSource _lifetime = new();
    private bool _started, _disposed;
    public List<LoadedMenu> Menus { get; } = [];
    public Dictionary<string, string> Errors { get; } = [];
    public Dictionary<string, string> LoadedVersions { get; } = [];
    public async Task InitializeAsync()
    {
        if (_started) return; _started = true;
        store.FinishPendingDeletes();
        foreach (var installed in store.State.Packages.Where(item => !item.PendingDelete))
        {
            var manifest = installed.Manifest;
            if (!manifest.Menus.Any(menu => store.IsEnabled(manifest.Id + ":" + menu.Id))) continue;
            ExtensionLoadContext? load = null; ServiceProvider? provider = null; IHitModule? module = null;
            var moduleLifetime = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            try
            {
                manifest.Validate();
                var directory = store.PackagePath(manifest);
                load = new(directory, manifest.EntryAssembly);
                var assembly = load.LoadFromAssemblyPath(ExtensionManifest.SafePath(directory, manifest.EntryAssembly));
                var moduleType = assembly.GetType(manifest.EntryType, true)!;
                if (!typeof(IHitModule).IsAssignableFrom(moduleType) || moduleType.IsAbstract) throw new InvalidDataException("The entry type must implement IHitModule.");
                module = (IHitModule)Activator.CreateInstance(moduleType)!;
                var services = new ServiceCollection();
                var api = new ExtensionApi(manifest, store, media, clock, settings, themes, links, ui, moduleLifetime.Token);
                module.ConfigureServices(services);
                services.AddSingleton<IHitApi>(api);
                provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
                api.Services = provider;
                var menus = new List<LoadedMenu>();
                foreach (var menu in manifest.Menus)
                {
                    var component = load.LoadFromAssemblyPath(ExtensionManifest.SafePath(directory, menu.ComponentAssembly)).GetType(menu.ComponentType, true)!;
                    if (!typeof(HitMenuComponent).IsAssignableFrom(component) || component.IsAbstract || component.ContainsGenericParameters)
                        throw new InvalidDataException("Menu components must inherit HitMenuComponent.");
                    menus.Add(new(manifest.Id + ":" + menu.Id, menu.Title, component, api, menu.Styles.Select(api.AssetUrl).ToArray()));
                }
                await module.StartAsync(api, moduleLifetime.Token);
                Menus.AddRange(menus); _modules.Add((load, module, provider, moduleLifetime)); LoadedVersions[manifest.Id] = manifest.Version;
            }
            catch (Exception error)
            {
                Errors[manifest.Id] = "Could not load this extension: " + (error.GetBaseException().Message);
                try { moduleLifetime.Cancel(); } catch (AggregateException) { }
                if (module is not null) try { await module.StopAsync(CancellationToken.None); } catch { }
                if (provider is not null) try { await provider.DisposeAsync(); } catch { }
                load?.Unload();
                moduleLifetime.Dispose();
            }
        }
    }
    public async ValueTask DisposeAsync()
    {
        if (_disposed) return; _disposed = true;
        try { _lifetime.Cancel(); } catch (AggregateException) { }
        Menus.Clear();
        foreach (var item in Enumerable.Reverse(_modules))
        {
            try { await item.Module.StopAsync(CancellationToken.None); } catch { }
            try { await item.Services.DisposeAsync(); } catch { }
            item.Load.Unload();
            item.Lifetime.Dispose();
        }
        _modules.Clear(); LoadedVersions.Clear(); _lifetime.Dispose();
    }
}

internal sealed class ExtensionLoadContext : AssemblyLoadContext
{
    private readonly string _root;
    private readonly AssemblyDependencyResolver _resolver;
    private readonly Dictionary<string, string> _assemblies = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Shared = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") ?? "")
        .Split(Path.PathSeparator).Select(Path.GetFileNameWithoutExtension).OfType<string>()
        .Where(name => name.StartsWith("System.") || name.StartsWith("Microsoft.AspNetCore.") || name.StartsWith("Microsoft.Extensions.") || name is "System" or "netstandard" or "mscorlib")
        .Append(typeof(IHitModule).Assembly.GetName().Name!).ToHashSet(StringComparer.OrdinalIgnoreCase);
    public ExtensionLoadContext(string root, string entry) : base(isCollectible: true)
    {
        _root = Path.GetFullPath(root);
        _resolver = new(ExtensionManifest.SafePath(root, entry));
        foreach (var file in Directory.EnumerateFiles(root, "*.dll", SearchOption.AllDirectories))
        {
            try
            {
                var name = AssemblyName.GetAssemblyName(file).Name!;
                if (Shared.Contains(name)) continue;
                if (!_assemblies.TryAdd(name, file)) throw new InvalidDataException("Duplicate assembly identity in the package.");
            }
            catch (BadImageFormatException) { /* Native dependency, resolved separately. */ }
        }
    }
    protected override Assembly? Load(AssemblyName name)
    {
        if (Shared.Contains(name.Name!)) return Default.LoadFromAssemblyName(name);
        var path = _resolver.ResolveAssemblyToPath(name) ?? _assemblies.GetValueOrDefault(name.Name!);
        if (path is null) throw new FileNotFoundException($"Package dependency not found: {name.Name}");
        return LoadFromAssemblyPath(Checked(path));
    }
    protected override nint LoadUnmanagedDll(string name)
    {
        var path = _resolver.ResolveUnmanagedDllToPath(name);
        return path is null ? 0 : LoadUnmanagedDllFromPath(Checked(path));
    }
    private string Checked(string path) => Path.GetFullPath(path).StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
        ? path : throw new InvalidDataException("Dependency path escapes the package.");
}
