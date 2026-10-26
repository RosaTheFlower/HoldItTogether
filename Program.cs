using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Components;
using HoldItTogether.Services;
using HoldItTogether.Services.HitMenus;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;
using HoldItTogether.Services.Extensions;
using Microsoft.Extensions.FileProviders;

namespace HoldItTogether;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();
        builder.Services.AddSingleton<IHitMediaService, WindowsMediaService>();
        builder.Services.AddSingleton<AudioSpectrumService>();
        builder.Services.AddSingleton<LyricsService>();
        builder.Services.AddSingleton(provider => new HitClockService(provider.GetRequiredService<HitSettingsService>()));
        builder.Services.AddSingleton<IExternalLinkService, ExternalLinkService>();
        builder.Services.AddSingleton<HitSettingsService>();
        builder.Services.AddSingleton<HitMemorySampler>();
        builder.Services.AddSingleton<HitThemeService>();
        builder.Services.AddSingleton<DesktopWindowService>();
        builder.Services.AddSingleton<HitMenuCatalog>();
        builder.Services.AddSingleton<ExtensionStore>();
        builder.Services.AddSingleton<ExtensionUi>();
        builder.Services.AddSingleton<ExtensionRuntime>();
        builder.Services.AddSingleton<ExtensionInbox>();
        builder.Services.AddSingleton<IFileProvider>(provider => new CompositeFileProvider(
            new ExtensionFileProvider(provider.GetRequiredService<ExtensionStore>()),
            new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "wwwroot"))));
        builder.RootComponents.Add<App>("app");

        var app = builder.Build();
        var settings = app.Services.GetRequiredService<HitSettingsService>();
        var clock = app.Services.GetRequiredService<HitClockService>();
        var extensions = app.Services.GetRequiredService<ExtensionRuntime>();
        extensions.InitializeAsync().GetAwaiter().GetResult();
        app.Services.GetRequiredService<HitMenuCatalog>().Refresh();
        var inbox = app.Services.GetRequiredService<ExtensionInbox>();
        inbox.Start();

        app.MainWindow
            .SetTitle("Hold It Together")
            .SetChromeless(true)
            .SetTransparent(true)
            .SetTopMost(settings.Current.AlwaysOnTop)
            .SetContextMenuEnabled(false)
            .SetUseOsDefaultSize(false)
            .SetUseOsDefaultLocation(false)
            .SetResizable(false)
            .SetSize(104, 104)
            .Center();

        app.Services.GetRequiredService<DesktopWindowService>().Attach(app.MainWindow);

        try { app.Run(); }
        finally
        {
            inbox.DisposeAsync().AsTask().GetAwaiter().GetResult();
            extensions.DisposeAsync().AsTask().GetAwaiter().GetResult();
            settings.UnbindRuntime();
            clock.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.Services.GetRequiredService<AudioSpectrumService>().Dispose();
            app.Services.GetRequiredService<LyricsService>().Dispose();
            (app.Services.GetRequiredService<IHitMediaService>() as IDisposable)?.Dispose();
        }
    }
}
