using System.IO.Compression;
using System.Text.Json;
using HIT.Sdk;
using HoldItTogether.Services;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.HitMenus;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using HoldItTogether.Components.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Microsoft.AspNetCore.Components.RenderTree;
using HoldItTogether.Components;

var repo = Path.GetFullPath(args.FirstOrDefault() ?? Environment.CurrentDirectory);
var fixture = Path.Combine(repo, "artifacts/packages/dev.hit.example-1.0.0.hitmenu");
if (!File.Exists(fixture)) throw new Exception("Build the sample using Tools/Pack-HitMenu.ps1 first.");
var root = Path.Combine(repo, "obj/extension-tests", Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
var store = new ExtensionStore(Path.Combine(root, "Modules"));
var settings = new HitSettingsService(Path.Combine(root, "settings.json"));
var ui = new ExtensionUi();
var themes = new HitThemeService(settings, Path.Combine(root, "Themes"), Path.Combine(repo, "wwwroot/themes"));
await using var clock = new HitClockService(settings, Path.Combine(root, "clock.json"), TimeProvider.System, new SilentSound(), false);
var media = new FakeMedia();
await using var runtime = new ExtensionRuntime(store, media, clock, settings, themes, new NoLinks(), ui);
static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
async Task<InstalledExtension> Install(byte[] bytes, string name = "sample.hitmenu")
{ await using var stream = new MemoryStream(bytes); return await store.InstallAsync(stream, name); }
var original = await File.ReadAllBytesAsync(fixture);
await runtime.InitializeAsync();
Check(runtime.Menus.Count == 0, "Empty startup should not load a module");
var installed = await Install(original);
Check(installed.Manifest.Id == "dev.hit.example", "Package identity");
Check(!System.Runtime.Loader.AssemblyLoadContext.Default.Assemblies.Any(assembly => assembly.GetName().Name == "HIT.Example"), "Import must not execute/load the plugin");
Check((await Install(original)).Hash == installed.Hash && store.State.Packages.Length == 1, "Idempotent import");
Check(!Directory.EnumerateFileSystemEntries(Path.Combine(store.Root, "Staging")).Any(), "Staging cleanup");

byte[] Alter(string? path = null, string? contents = null, Action<JsonObjectBuilder>? manifest = null, bool link = false)
{
    using var output = new MemoryStream();
    using (var source = new ZipArchive(new MemoryStream(original), ZipArchiveMode.Read))
    using (var target = new ZipArchive(output, ZipArchiveMode.Create, true))
    {
        foreach (var entry in source.Entries)
        {
            var copy = target.CreateEntry(entry.FullName);
            using var destination = copy.Open();
            if (entry.FullName == "manifest.json" && manifest is not null)
            {
                using var text = new StreamReader(entry.Open()); var value = System.Text.Json.Nodes.JsonNode.Parse(text.ReadToEnd())!.AsObject();
                manifest(new(value)); using var writer = new StreamWriter(destination); writer.Write(value.ToJsonString());
            }
            else { using var input = entry.Open(); input.CopyTo(destination); }
        }
        if (path is not null)
        {
            var entry = target.CreateEntry(path); if (link) entry.ExternalAttributes = 0xA000 << 16;
            using var writer = new StreamWriter(entry.Open()); writer.Write(contents ?? "unexpected");
        }
    }
    return output.ToArray();
}
async Task Reject(byte[] bytes, string name)
{
    try { await Install(bytes); throw new Exception("Accepted invalid package: " + name); }
    catch (Exception error) when (error is InvalidDataException or JsonException or BadImageFormatException or NullReferenceException) { }
    Check(store.State.Packages.Single().Hash == installed.Hash, "Failed install changed registry: " + name);
}
await Reject(Alter("../escape.txt"), "parent traversal");
await Reject(Alter("C:/escape.txt"), "absolute path");
await Reject(Alter("Assets/CON.txt"), "reserved name");
await Reject(Alter("Layout/css/NOTES.css"), "case collision");
await Reject(Alter("Assets/link", link: true), "symlink");
await Reject(Alter(manifest: m => m.Set("hitContractVersion", "99")), "SDK incompatibility");
await Reject(Alter(manifest: m => m.Set("capabilities", new[] { "unknown.permission" })), "unknown capability");
await Reject(Alter(manifest: m => m.Set("menus", Array.Empty<object>())), "empty menus");
Console.WriteLine("PASS: package validation, traversal/links/collisions, compatibility, idempotence and failed-install atomicity.");

Check(await runtime.ActivateAsync(installed.Manifest.Id), "A new module should activate without restarting HIT");
Check(runtime.Errors.Count == 0, string.Join("; ", runtime.Errors.Values));
Check(runtime.Menus.Count == 1, "Standalone SDK component load");
var menu = runtime.Menus.Single();
Check(menu.ComponentType.Assembly.GetReferencedAssemblies().All(name => name.Name != "HoldItTogether"), "Example depends on app internals");
Check(System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(menu.ComponentType.Assembly) != System.Runtime.Loader.AssemblyLoadContext.Default, "Plugin context");
await menu.Api.Storage.WriteAsync("test", new[] { "persisted" });
Check((await menu.Api.Storage.ReadAsync<string[]>("test"))![0] == "persisted", "Scoped storage");
try { await menu.Api.Storage.WriteAsync("../outside", 1); throw new Exception("Unsafe storage key accepted"); } catch (ArgumentException) { }
try { await menu.Api.Media.ReadAsync(); throw new Exception("Undeclared media capability accepted"); } catch (InvalidOperationException) { }
menu.Api.Clock.StartTimer(TimeSpan.FromMinutes(5));
Check(clock.Read().Timer.Running, "Public clock API");

var files = new ExtensionFileProvider(store);
Check(files.GetFileInfo(menu.Styles[0]).Exists, "Package CSS served");
Check(!files.GetFileInfo("extensions/dev.hit.example/1.0.0/Services/HIT.Example.dll").Exists, "DLL exposed as asset");
Check(!files.GetFileInfo("extensions/dev.hit.example/1.0.0/../../registry.json").Exists, "Asset traversal");
using var catalog = new HitMenuCatalog(store, runtime);
var firstId = catalog.Menus[0].Id;
catalog.Move(menu.Id, firstId);
Check(catalog.Menus[0].Id == menu.Id && catalog.Menus[^1].Id == HitMenuCatalog.SettingsId, "Persistent menu order");
catalog.Place(menu.Id, firstId, true);
Check(catalog.Menus.ToList().FindIndex(item => item.Id == menu.Id) == catalog.Menus.ToList().FindIndex(item => item.Id == firstId) + 1, "Pointer drop must honor the insertion edge");
catalog.Place(menu.Id, firstId, false);
Check(catalog.Menus[0].Id == menu.Id, "Pointer drop before target");
store.SetEnabled(menu.Id, false);
Check(catalog.Menus.All(item => item.Id != menu.Id), "Disabled menu still navigable");
store.SetEnabled(HitMenuCatalog.SettingsId, false);
Check(catalog.Menus[^1].Id == HitMenuCatalog.SettingsId, "Settings must remain enabled");
store.SetEnabled(menu.Id, true);
Check(new ExtensionStore(store.Root).State.Order[0] == menu.Id, "Order not persisted");
Console.WriteLine("PASS: external assembly loading, API capabilities, scoped storage, assets, ordering and enable/disable.");

var services = new ServiceCollection().AddLogging().AddSingleton(store).AddSingleton(runtime).AddSingleton(ui).AddSingleton(catalog)
    .AddSingleton(settings).AddSingleton(themes).AddSingleton(clock).AddSingleton<IHitMediaService>(media)
    .AddSingleton<HoldItTogether.Services.Diagnostics.HitMemorySampler>().AddSingleton<AudioSpectrumService>()
    .AddSingleton(new LyricsService(new HttpClient())).AddSingleton<IExternalLinkService, NoLinks>()
    .AddSingleton<HoldItTogether.Services.HitApplicationService>().AddSingleton<ExtensionActivationService>()
            .AddSingleton<HoldItTogether.Services.Diagnostics.HitUpdateService>().AddSingleton<HoldItTogether.Services.Settings.HitDataService>()
            .AddSingleton<IJSRuntime, NoJs>();
await using var provider = services.BuildServiceProvider();
await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
await renderer.Dispatcher.InvokeAsync(async () =>
{
    var html = await renderer.RenderComponentAsync<ExtensionsView>();
    var content = html.ToHtmlString();
    Check(content.Contains("Install .hitmenu") && content.Contains("dev.hit.example:notes") && content.Contains("Move Notes up"), "Extension manager controls");
    await File.WriteAllTextAsync(Path.Combine(repo, "obj/extensions-preview.html"), content);
    var component = await renderer.RenderComponentAsync<SampleHost>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Api"] = menu.Api, ["ComponentType"] = menu.ComponentType }));
    Check(component.ToHtmlString().Contains("What are you working on?"), "External Razor component render");
});
Console.WriteLine("PASS: rendered extension manager and independently compiled Razor UI.");

await using (var interactive = new InteractiveRenderer(provider, provider.GetRequiredService<ILoggerFactory>()))
{
    await interactive.Dispatcher.InvokeAsync(async () =>
    {
        var (id, host) = await interactive.MountAsync<HitMenuHost>();
        Check(interactive.Errors.Count == 0, string.Join("; ", interactive.Errors.Select(error => error.ToString())));
        await host.ShowExtensionsAsync();
        await interactive.DispatchEventAsync(interactive.Event(id, "button", "aria-label", "Open Extensions", "onclick"), null, new MouseEventArgs());
        Check(interactive.Graph(id).Any(frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent.Contains("Drag menus to reorder")), "Extensions page did not open");
        var field = typeof(HitMenuHost).GetField("_selectedId", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        await host.HandleWheelAsync(new WheelEventArgs { DeltaY = -120 });
        Check((string?)field.GetValue(host) == HitMenuCatalog.SettingsId, "Extensions did not block menu scroll");
        await interactive.DispatchEventAsync(interactive.Event(id, "button", "aria-label", "Move Clock up", "onclick"), null, new MouseEventArgs());
        Check((string?)field.GetValue(host) == HitMenuCatalog.SettingsId, "Reordering switched the active page");
        await interactive.DispatchEventAsync(interactive.Event(id, "input", "aria-label", "Enable Audio", "onchange"), null, new ChangeEventArgs { Value = false });
        Check(catalog.Menus.All(item => item.ComponentType?.Name != "HitMenuAudio"), "Builtin disable failed");
        await interactive.DispatchEventAsync(interactive.Event(id, "section", "class", "extensions-view", "oncontextmenu"), null, new MouseEventArgs { Button = 2 });
        Check(interactive.Graph(id).Any(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "aria-label" && Equals(frame.AttributeValue, "Manage themes")), "Right-click did not return to Settings: " + string.Join("; ",interactive.Errors));
        Check(interactive.Errors.Count == 0, string.Join("; ", interactive.Errors.Select(error => error.ToString())));
    });
}
Console.WriteLine("PASS: real host navigation, reorder with stable selection, builtin disable, blocked scroll and right-click return.");
await MenuChecks.RunAsync(provider, root, repo);
WindowGeometryChecks.Run();
PlacementChecks.Run();

var update = Alter(manifest: m => m.Set("version", "1.1.0"));
await Install(update);
Check(!await runtime.ActivateAsync(installed.Manifest.Id), "Replacing loaded code must request a graceful restart");
Check(store.State.Packages.Single().PreviousVersion == "1.0.0", "Previous version lost");
Check(Directory.Exists(Path.Combine(store.Root, "Installed/dev.hit.example/1.0.0")), "Loaded version overwritten");
await Install(Alter(manifest:m=>m.Set("version","1.2.0")));
var cleared = await runtime.ClearOldVersionsAsync();
Check(cleared.Removed==1 && cleared.Skipped==0,"Cleanup removes obsolete unloaded version");
Check(Directory.Exists(Path.Combine(store.Root,"Installed/dev.hit.example/1.0.0")) && Directory.Exists(Path.Combine(store.Root,"Installed/dev.hit.example/1.2.0")),"Cleanup preserves loaded and selected versions");
Check(store.State.Packages.Single().PreviousVersion is null && File.Exists(Path.Combine(store.Root,"State/dev.hit.example/test.json")),"Cleanup clears unavailable rollback and preserves user data");
Check((await runtime.ClearOldVersionsAsync()).Removed==0,"Cleanup is idempotent");
// Reinstall a rollback target to exercise the existing rollback/removal path.
await Install(Alter(manifest:m=>m.Set("version","1.3.0")));
store.RestorePrevious("dev.hit.example");
Check(store.State.Packages.Single().Manifest.Version == "1.2.0", "Rollback failed");
store.Remove("dev.hit.example");
Check(catalog.Menus.All(item => item.ExtensionId != "dev.hit.example"), "Deleted package still visible");
Check(File.Exists(Path.Combine(store.Root, "State/dev.hit.example/test.json")), "Deletion removed user data");
Console.WriteLine("PASS: versioned update, rollback, deferred removal and preserved user data.");
Console.WriteLine("All extension tests passed. Test data: " + root);

record JsonObjectBuilder(System.Text.Json.Nodes.JsonObject Value) { public void Set<T>(string key, T value) => Value[key] = JsonSerializer.SerializeToNode(value); }
sealed class SilentSound : IClockSound { public void Play() { } }
sealed class NoLinks : IExternalLinkService { public bool OpenWebPage(string? address) => false; }
sealed class FakeMedia : IHitMediaService
{
    public Task<MediaSnapshot> ReadAsync(CancellationToken token) => Task.FromResult(MediaSnapshot.Empty);
    public Task<bool> ExecuteAsync(MediaAction action, long generation, double position, CancellationToken token) => Task.FromResult(false);
}
sealed class NoJs : IJSRuntime
{
    public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => ValueTask.FromResult(identifier == "import" ? (T)(object)new NoJsModule() : default!);
    public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<T>(identifier, args);
}
sealed class NoJsModule : IJSObjectReference
{
    public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => ValueTask.FromResult(default(T)!);
    public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<T>(identifier, args);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
sealed class InteractiveRenderer(IServiceProvider services, ILoggerFactory logger) : Renderer(services, logger)
{
    public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
    public List<Exception> Errors { get; } = [];
    protected override void HandleException(Exception exception) => Errors.Add(exception);
    protected override Task UpdateDisplayAsync(in RenderBatch batch) => Task.CompletedTask;
    public async Task<(int, T)> MountAsync<T>() where T : IComponent
    {
        var component = (T)InstantiateComponent(typeof(T)); var id = AssignRootComponentId(component);
        await RenderRootComponentAsync(id); return (id, component);
    }
    public IEnumerable<RenderTreeFrame> Graph(int id) => Graph(id, []);
    private IEnumerable<RenderTreeFrame> Graph(int id, HashSet<int> visited)
    {
        if (!visited.Add(id)) yield break;
        var range = GetCurrentRenderTreeFrames(id);
        foreach (var frame in range.Array.Take(range.Count).ToArray())
        {
            yield return frame;
            if (frame.FrameType == RenderTreeFrameType.Component) foreach (var child in Graph(frame.ComponentId, visited)) yield return child;
        }
    }
    public ulong Event(int id, string tag, string attribute, string value, string eventName)
    {
        var frames = Graph(id).ToArray();
        for (var i = 0; i < frames.Length; i++)
        {
            if (frames[i].FrameType != RenderTreeFrameType.Element || frames[i].ElementName != tag) continue;
            var attrs = frames.Skip(i + 1).TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
            if (attrs.Any(frame => frame.AttributeName == attribute && Equals(frame.AttributeValue, value)))
                return attrs.First(frame => frame.AttributeName == eventName).AttributeEventHandlerId;
        }
        throw new Exception("Control not found: " + value);
    }
}
public sealed class SampleHost : ComponentBase
{
    [Parameter] public IHitApi Api { get; set; } = default!;
    [Parameter] public Type ComponentType { get; set; } = default!;
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.OpenComponent<CascadingValue<HitContext>>(0);
        builder.AddAttribute(1, "Name", "HitExtension");
        builder.AddAttribute(2, "Value", new HitContext(true, true, false, _ => Task.CompletedTask, _ => Task.CompletedTask, _ => Task.CompletedTask, () => Task.CompletedTask));
        builder.AddAttribute(3, "ChildContent", (RenderFragment)(content =>
        { content.OpenComponent(0, ComponentType); content.AddAttribute(1, "Api", Api); content.CloseComponent(); }));
        builder.CloseComponent();
    }
}
