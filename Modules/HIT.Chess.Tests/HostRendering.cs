using System.Reflection;
using HIT.Sdk;
using HoldItTogether.Components;
using HoldItTogether.Services;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Extensions;
using HoldItTogether.Services.HitMenus;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class HostRendering
{
    public static async Task RunAsync(string fixture)
    {
        string root = Path.GetFullPath("obj/chess-tests/host-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var store = new ExtensionStore(Path.Combine(root, "Modules"));
        await using (var stream = File.OpenRead(fixture)) await store.InstallAsync(stream, Path.GetFileName(fixture));
        var settings = new HitSettingsService(Path.Combine(root, "settings.json"));
        var ui = new ExtensionUi();
        var themes = new HitThemeService(settings, Path.Combine(root, "Themes"), Path.GetFullPath("wwwroot/themes"));
        await using var clock = new HitClockService(settings, Path.Combine(root, "clock.json"), TimeProvider.System, new SilentSound(), false);
        var media = new SilentMedia();
        await using var runtime = new ExtensionRuntime(store, media, clock, settings, themes, new NoLinks(), ui);
        await runtime.InitializeAsync();
        if (runtime.Errors.Count > 0) throw new Exception(string.Join("; ", runtime.Errors.Values));
        var menu = runtime.Menus.Single();
        if (System.Runtime.Loader.AssemblyLoadContext.GetLoadContext(menu.ComponentType.Assembly) == System.Runtime.Loader.AssemblyLoadContext.Default)
            throw new Exception("Chess must be loaded as an installed extension, independently of the test assembly.");
        if (menu.ComponentType.GetProperty("JS")?.PropertyType != typeof(IJSRuntime))
            throw new Exception("The installed menu must resolve the host IJSRuntime identity.");
        foreach (var builtIn in HitMenuCatalog.Discover(typeof(HitMenuCatalog).Assembly))
            if (builtIn.Id != HitMenuCatalog.SettingsId) store.SetEnabled(builtIn.Id, false);
        store.SetOrder([menu.Id]);
        using var catalog = new HitMenuCatalog(store, runtime);
        var js = new HostJs();
        var services = new ServiceCollection().AddLogging().AddSingleton(store).AddSingleton(runtime).AddSingleton(ui).AddSingleton(catalog)
            .AddSingleton(settings).AddSingleton(themes).AddSingleton(clock).AddSingleton<IHitMediaService>(media)
            .AddSingleton<DesktopWindowService>().AddSingleton<HitMemorySampler>().AddSingleton<AudioSpectrumService>()
            .AddSingleton(new LyricsService(new HttpClient())).AddSingleton<IExternalLinkService, NoLinks>()
            .AddSingleton<HoldItTogether.Services.HitApplicationService>().AddSingleton<ExtensionActivationService>()
            .AddSingleton<HoldItTogether.Services.Diagnostics.HitUpdateService>().AddSingleton<HoldItTogether.Services.Settings.HitDataService>()
            .AddSingleton<IJSRuntime>(js);
        await using var provider = services.BuildServiceProvider();
        await using var renderer = new TestRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            js.StylesReady = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var pending = renderer.BeginMount<DynamicSphere>();
            if (renderer.Components(pending.Id).Any(c => c.GetType().FullName == "HIT.Chess.Layout.ChessMenu"))
                throw new Exception("Extension UI mounted before its stylesheet was applied.");
            js.StylesReady.SetResult();
            await pending.Rendered;
            for (int attempt = 0; attempt < 40 && !renderer.Components(pending.Id).Any(c => c.GetType().FullName == "HIT.Chess.Layout.ChessMenu"); attempt++) await Task.Delay(5);
            if (!renderer.Components(pending.Id).Any(c => c.GetType().FullName == "HIT.Chess.Layout.ChessMenu"))
                throw new Exception("Styled extension UI did not mount after styles were ready.");
            renderer.Unmount(pending.Id);
            js.StylesReady = null;
            var (id, sphere) = await renderer.MountAsync<DynamicSphere>();
            await Invoke(sphere, "PinOpen");
            renderer.AssertHealthy();
            for (int i = 0; i < 5; i++)
            {
                await Invoke(sphere, "PinOpen");
                var chess = renderer.Components(id).Single(c => c.GetType().FullName == "HIT.Chess.Layout.ChessMenu");
                await ((HIT.Sdk.IHitMenuSettings)chess).OpenSettingsAsync();
                if(await renderer.Components(id).OfType<HitMenuHost>().Single().CompactCurrentAsync())
                    throw new Exception("Chess settings must never accept a compact request.");
                if (!renderer.ConfigurationOpen(id)) throw new Exception("The shared header must recognize Chess settings for its temporary cover control.");
                await renderer.DispatchEventAsync(renderer.EventByLabel(id, "Play as", "onchange"), null, new ChangeEventArgs { Value = "black" });
                if ((bool)chess.GetType().GetField("_draftWhite", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chess)!)
                    throw new Exception("Black option did not select Black.");
                await renderer.DispatchEventAsync(renderer.EventByLabel(id, "Play as", "onchange"), null, new ChangeEventArgs { Value = "white" });
                if (!(bool)chess.GetType().GetField("_draftWhite", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(chess)!)
                    throw new Exception("White option did not select White.");
                await renderer.DispatchEventAsync(renderer.EventByLabel(id, "Move sounds", "onchange"), null, new ChangeEventArgs { Value = false });
                if ((await menu.Api.Storage.ReadAsync<HIT.Chess.Services.ChessState>("chess-v1"))!.SoundEnabled)
                    throw new Exception("Mute switch did not persist.");
                await renderer.DispatchEventAsync(renderer.EventByLabel(id, "Move sounds", "onchange"), null, new ChangeEventArgs { Value = true });
                await ((HIT.Sdk.IHitMenuSettings)chess).CloseSettingsAsync();
                if (renderer.ConfigurationOpen(id)) throw new Exception("The shared header must stop treating the board as configuration.");
                await Invoke(chess, "CompactAsync");
                await Invoke(chess, "ExpandAsync");
                await Invoke(sphere, "CloseSphere");
                renderer.AssertHealthy();
            }
            await File.WriteAllTextAsync("obj/chess-tests/import-url.json", System.Text.Json.JsonSerializer.Serialize(js.Imports.First(path => path.Contains("/Assets/chess.js"))));

            await Invoke(sphere, "PinOpen");
            foreach (var field in new[] { "_pinnedOpen", "_pointerInside", "_focused" })
                typeof(DynamicSphere).GetField(field, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(sphere, false);
            await Invoke(sphere, "HandleMenuWheelAsync", new WheelEventArgs { DeltaY = 120 });
            await Task.Delay(230);
            if (typeof(DynamicSphere).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sphere)?.ToString() != "Open")
                throw new Exception("Wheel navigation must not collapse on a transient resize-induced pointer exit.");
            await Invoke(sphere, "SetPointerInside", true);
            await Task.Delay(850);
            if (typeof(DynamicSphere).GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(sphere)?.ToString() != "Open")
                throw new Exception("Pointer recovery must cancel the delayed collapse.");
            var navigationHost = renderer.Components(id).OfType<HitMenuHost>().Single();
            await Invoke(navigationHost, "SelectMenuAsync", 0);

            await Invoke(sphere, "PinOpen");
            var pacedChess = renderer.Components(id).Single(c => c.GetType().FullName == "HIT.Chess.Layout.ChessMenu");
            var pacedState = pacedChess.GetType().GetField("_state", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pacedChess)!;
            var difficultyProperty = pacedState.GetType().GetProperty("Difficulty")!;
            difficultyProperty.SetValue(pacedState, Enum.Parse(difficultyProperty.PropertyType, "Easy"));
            js.PresentationDuration = 250;
            await Invoke(pacedChess, "ClickSquareAsync", 12);
            var pacing = System.Diagnostics.Stopwatch.StartNew();
            await Invoke(pacedChess, "ClickSquareAsync", 28);
            await Task.Delay(80);
            if (((System.Collections.ICollection)pacedState.GetType().GetProperty("Moves")!.GetValue(pacedState)!).Count != 1)
                throw new Exception("The algorithm replied before the player's animation finished.");
            if (pacedChess.GetType().GetField("_searchTask", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pacedChess) is Task reply) await reply;
            if (pacing.ElapsedMilliseconds < 225) throw new Exception("Move pacing is shorter than the presentation window.");
            await Invoke(pacedChess, "ClickSquareAsync", 8);
            if ((int)pacedChess.GetType().GetField("_selected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pacedChess)! != -1)
                throw new Exception("Player input was accepted during the opponent's animation.");
            pacedState.GetType().GetProperty("Minutes")!.SetValue(pacedState, 1);
            pacedChess.GetType().GetField("_whiteSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pacedChess, 60d);
            pacedChess.GetType().GetField("_blackSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pacedChess, 60d);
            pacedChess.GetType().GetField("_lastTick", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(pacedChess, DateTimeOffset.UtcNow.AddMilliseconds(-400));
            await Invoke(pacedChess, "UpdateClock");
            if ((double)pacedChess.GetType().GetField("_whiteSeconds", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pacedChess)! != 60)
                throw new Exception("Forced presentation time was charged to the next player's clock.");
            pacedState.GetType().GetProperty("Minutes")!.SetValue(pacedState, 0);
            await Task.Delay(320);
            await Invoke(pacedChess, "ClickSquareAsync", 8);
            if ((int)pacedChess.GetType().GetField("_selected", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(pacedChess)! != 8)
                throw new Exception("Player input did not resume after the animation.");
            js.PresentationDuration = 0;

            settings.Update(value=>value with {MenuCompactDelayMs=5000});
            await Invoke(sphere,"SetPinned",false);
            await navigationHost.CompactCurrentAsync();
            if (!(bool)pacedChess.GetType().GetField("_compact",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(pacedChess)!)
                throw new Exception("The header button must immediately compact the current board.");
            if (typeof(DynamicSphere).GetField("_state",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(sphere)?.ToString()!="Open")
                throw new Exception("Compacting the board must keep HIT open.");
            await Invoke(pacedChess,"ExpandAsync");
            await ((HIT.Sdk.IHitMenuSettings)pacedChess).OpenSettingsAsync();
            pacedState.GetType().GetProperty("Wins")!.SetValue(pacedState,7);
            pacedState.GetType().GetProperty("Losses")!.SetValue(pacedState,4);
            await Invoke(pacedChess,"SaveAsync");
            await Invoke(pacedChess,"AskClearScore");
            var beforeClear=await menu.Api.Storage.ReadAsync<HIT.Chess.Services.ChessState>("chess-v1");
            if (beforeClear!.Wins!=7 || beforeClear.Losses!=4) throw new Exception("The first clear action cannot reset scores.");
            await Invoke(pacedChess,"CancelConfirm");
            await Invoke(pacedChess,"ClearScoreAsync");
            if ((await menu.Api.Storage.ReadAsync<HIT.Chess.Services.ChessState>("chess-v1"))!.Wins!=7) throw new Exception("Canceled clear cannot reset scores.");
            await Invoke(pacedChess,"AskClearScore");
            await Invoke(pacedChess,"ClearScoreAsync");
            var afterClear=await menu.Api.Storage.ReadAsync<HIT.Chess.Services.ChessState>("chess-v1");
            if (afterClear!.Wins!=0 || afterClear.Losses!=0 || !afterClear.Moves.SequenceEqual(beforeClear.Moves))
                throw new Exception("Confirmed clear must persist zero scores and keep the current game.");
            await ((HIT.Sdk.IHitMenuSettings)pacedChess).CloseSettingsAsync();

            var clockMenu=HitMenuCatalog.Discover(typeof(HitMenuCatalog).Assembly).Single(item=>item.ComponentType?.Name=="HitMenuClock");
            store.SetEnabled(clockMenu.Id,true);
            await Invoke(navigationHost,"SelectMenuAsync",catalog.Menus.ToList().FindIndex(item=>item.Id==clockMenu.Id));
            await Invoke(sphere,"SetPinned",false);
            await navigationHost.CompactCurrentAsync();
            var clockView=renderer.Components(id).Single(component=>component.GetType().Name=="HitMenuClock");
            if ((bool)clockView.GetType().GetField("_expanded",BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(clockView)!)
                throw new Exception("The header command must pin and compact Clock in the same action.");
            await Invoke(navigationHost,"SelectMenuAsync",catalog.Menus.ToList().FindIndex(item=>item.Id==menu.Id));

            // Exercise the boundary and settings routes after a real descendant disposal.
            js.FailConnect = true;
            var (failedId, failedSphere) = await renderer.MountAsync<DynamicSphere>();
            await Invoke(failedSphere, "PinOpen");
            var failedHost = renderer.Components(failedId).OfType<HitMenuHost>().Single();
            if ((bool)typeof(HitMenuHost).GetProperty("CurrentHasSettings", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(failedHost)!)
                throw new Exception("Failed menus must not advertise settings.");
            await failedHost.OpenCurrentSettingsAsync();
            typeof(HitMenuHost).GetField("_contextStartedInSettings", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(failedHost, true);
            await Invoke(failedHost, "ReturnFromSettingsAsync", new Microsoft.AspNetCore.Components.Web.MouseEventArgs { Button = 2 });
            var disposedChess = js.ChessMenus.Last();
            if (!(bool)disposedChess.GetType().GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(disposedChess)!)
                throw new Exception("Startup failure must dispose the chess view.");
            // A delayed handler already queued before disposal must also be harmless.
            await ((HIT.Sdk.IHitMenuSettings)disposedChess).OpenSettingsAsync();
            await ((HIT.Sdk.IHitMenuSettings)disposedChess).CloseSettingsAsync();
            await Invoke(disposedChess, "CompactAsync");
            var staleContext = ((HitMenuComponent)disposedChess).Context;
            await staleContext.SetTitleAsync("Stale title");
            await staleContext.SetLayoutAsync(HitLayout.Compact);
            await staleContext.SetPinnedAsync(true);
            await staleContext.CloseAsync();
            var retainedTitles = (Dictionary<string, string>)typeof(HitMenuHost).GetField("_titles", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(failedHost)!;
            if (retainedTitles.ContainsKey(menu.Id)) throw new Exception("Disposed menus cannot change the host title.");
            renderer.AssertHealthy();
            string log = await File.ReadAllTextAsync(Path.Combine(store.Root, "Logs", "dev.hit.chess.log"));
            if (!log.Contains("Deliberate JS startup failure")) throw new Exception("Primary extension error must be persisted.");
        });
        Console.WriteLine("PASS: installed chess, settings, 250ms pacing, input/clock synchronization, failure logging, disposal and late callbacks.");
    }

    private static async Task Invoke(object component, string name, params object[] args)
    {
        var result = component.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)!.Invoke(component, args);
        if (result is Task task) await task;
        typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(component, null);
    }
    private sealed class SilentSound : IClockSound { public void Play() { } }
    private sealed class NoLinks : IExternalLinkService { public bool OpenWebPage(string? address) => false; }
    private sealed class SilentMedia : IHitMediaService
    {
        public Task<MediaSnapshot> ReadAsync(CancellationToken token) => Task.FromResult(MediaSnapshot.Empty);
        public Task<bool> ExecuteAsync(MediaAction action, long generation, double position, CancellationToken token) => Task.FromResult(false);
    }
    private sealed class HostJs : IJSRuntime
    {
        public readonly List<string> Imports = [];
        public readonly List<IComponent> ChessMenus = [];
        public bool FailConnect;
        public int PresentationDuration;
        public TaskCompletionSource? StylesReady;
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args)
        {
            if (identifier == "import" && args?[0] is string import) Imports.Add(import);
            if (identifier == "import" && args?[0] is string path && !path.StartsWith("./") && !path.StartsWith('/') && !Uri.IsWellFormedUriString(path, UriKind.Absolute))
                throw new JSException($"Failed to resolve module specifier '{path}'. Relative references must start with '/', './', or '../'.");
            return ValueTask.FromResult(typeof(T) == typeof(IJSObjectReference) ? (T)(object)new HostModule(this) : default!);
        }
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<T>(identifier, args);
    }
    private sealed class HostModule(HostJs owner) : IJSObjectReference
    {
        public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args)
        {
            if (identifier == "styles" && owner.StylesReady is { } ready) return WaitForStylesAsync<T>(ready.Task);
            if (identifier == "sync" && typeof(T) == typeof(int)) return ValueTask.FromResult((T)(object)owner.PresentationDuration);
            if (identifier == "connect" && args?[1] is { } reference)
            {
                owner.ChessMenus.Add((IComponent)reference.GetType().GetProperty("Value")!.GetValue(reference)!);
                if (owner.FailConnect) throw new JSException("Deliberate JS startup failure");
            }
            return ValueTask.FromResult(typeof(T) == typeof(IJSObjectReference) ? (T)(object)new HostModule(owner) : default!);
        }
        public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken token, object?[]? args) => InvokeAsync<T>(identifier, args);
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        private static async ValueTask<T> WaitForStylesAsync<T>(Task ready) { await ready; return default!; }
    }
    private sealed class TestRenderer(IServiceProvider services, ILoggerFactory logger) : Renderer(services, logger)
    {
        public override Dispatcher Dispatcher { get; } = Dispatcher.CreateDefault();
        private readonly List<Exception> _errors = [];
        protected override void HandleException(Exception exception) { _errors.Add(exception); Console.WriteLine(exception); }
        protected override Task UpdateDisplayAsync(in RenderBatch batch) => Task.CompletedTask;
        public void AssertHealthy() { if (_errors.Count > 0) throw new AggregateException(_errors); }
        public async Task<(int, T)> MountAsync<T>() where T : IComponent
        {
            var pending = BeginMount<T>();
            await pending.Rendered; return (pending.Id, pending.Component);
        }
        public (int Id, T Component, Task Rendered) BeginMount<T>() where T : IComponent
        {
            var component = (T)InstantiateComponent(typeof(T)); int id = AssignRootComponentId(component);
            return (id, component, RenderRootComponentAsync(id));
        }
        public void Unmount(int id) => RemoveRootComponent(id);
        public IEnumerable<IComponent> Components(int id)
        {
            var range = GetCurrentRenderTreeFrames(id);
            foreach (var frame in range.Array.Take(range.Count).ToArray())
                if (frame.FrameType == RenderTreeFrameType.Component)
                {
                    yield return frame.Component;
                    foreach (var child in Components(frame.ComponentId)) yield return child;
                }
        }
        private IEnumerable<RenderTreeFrame> Frames(int id)
        {
            var range = GetCurrentRenderTreeFrames(id);
            foreach (var frame in range.Array.Take(range.Count).ToArray())
            {
                yield return frame;
                if (frame.FrameType == RenderTreeFrameType.Component)
                    foreach (var child in Frames(frame.ComponentId)) yield return child;
            }
        }
        public bool ConfigurationOpen(int id) => Frames(id).Any(frame => frame.FrameType == RenderTreeFrameType.Attribute && frame.AttributeName == "data-hit-configuration" && Equals(frame.AttributeValue, "true"));
        public ulong EventByLabel(int id, string label, string eventName = "onclick")
        {
            var frames = Frames(id).ToArray();
            for (int i = 0; i < frames.Length; i++)
            {
                if (frames[i].FrameType != RenderTreeFrameType.Element) continue;
                var attributes = frames.Skip(i + 1).TakeWhile(f => f.FrameType == RenderTreeFrameType.Attribute).ToArray();
                if (attributes.Any(f => f.AttributeName == "aria-label" && Equals(f.AttributeValue, label)))
                    return attributes.Single(f => f.AttributeName == eventName).AttributeEventHandlerId;
            }
            throw new Exception("Button not found: " + label);
        }
    }
}
