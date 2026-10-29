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
        if(args.FirstOrDefault()=="--hit-profile")
        {Environment.ExitCode=HitPerformanceRecorder.RunAsync(args).GetAwaiter().GetResult();return;}
        if(args.FirstOrDefault()=="--hit-install-package")
        {Environment.ExitCode=ExtensionPackageBootstrap.RunAsync(args).GetAwaiter().GetResult();return;}
        System.Globalization.CultureInfo.DefaultThreadCurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        HitDataService.ApplyPending();
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();
        builder.Services.AddSingleton<IHitMediaService, WindowsMediaService>();
        builder.Services.AddSingleton<AudioSpectrumService>();
        builder.Services.AddSingleton<LyricsService>();
        builder.Services.AddSingleton(provider => new HitClockService(provider.GetRequiredService<HitSettingsService>()));
        builder.Services.AddSingleton<IExternalLinkService, ExternalLinkService>();
        builder.Services.AddSingleton<HitSettingsService>();
        builder.Services.AddSingleton<WindowPositionStore>();
        builder.Services.AddSingleton<HitMemorySampler>();
        builder.Services.AddSingleton<HitPerformanceSampler>();
        builder.Services.AddSingleton<HitDiagnosticLog>();
        builder.Services.AddSingleton<HitAutomaticReporter>();
        builder.Services.AddSingleton<HitResourcePolicy>();
        builder.Services.AddSingleton<HitUpdateService>();
        builder.Services.AddSingleton<HitDataService>();
        builder.Services.AddSingleton<HitApplicationService>();
        builder.Services.AddSingleton<HitThemeService>();
        builder.Services.AddSingleton<HitAppearanceService>();
        builder.Services.AddSingleton<DesktopWindowService>();
        builder.Services.AddSingleton<HitMenuCatalog>();
        builder.Services.AddSingleton<ExtensionStore>();
        builder.Services.AddSingleton<ExtensionUi>();
        builder.Services.AddSingleton<ExtensionRuntime>();
        builder.Services.AddSingleton<HitExtensionHub>();
        builder.Services.AddSingleton<ExtensionActivationService>();
        builder.Services.AddSingleton<ExtensionInbox>();
        builder.Services.AddSingleton<IFileProvider>(provider => new CompositeFileProvider(
            new AppearanceImageFileProvider(provider.GetRequiredService<HitAppearanceService>()),
            new ExtensionFileProvider(provider.GetRequiredService<ExtensionStore>()),
            new PhysicalFileProvider(Path.Combine(AppContext.BaseDirectory, "wwwroot"))));
        builder.RootComponents.Add<App>("app");

        var app = builder.Build();
        var settings = app.Services.GetRequiredService<HitSettingsService>();
        var diagnostics = app.Services.GetRequiredService<HitDiagnosticLog>();
        diagnostics.Record("host","started");
        var clock = app.Services.GetRequiredService<HitClockService>();
        var extensions = app.Services.GetRequiredService<ExtensionRuntime>();
        extensions.InitializeAsync().GetAwaiter().GetResult();
        app.Services.GetRequiredService<HitMenuCatalog>().Refresh();
        if (args.Contains("--hit-open=extensions")) app.Services.GetRequiredService<ExtensionUi>().ShowExtensions = true;
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
            .SetZoom(100)
            .SetSize(104, 104);

        var desktop=app.Services.GetRequiredService<DesktopWindowService>();
        desktop.ConfigureInitialLocation(app.MainWindow);
        desktop.Attach(app.MainWindow);
        app.Services.GetRequiredService<HitApplicationService>().BindClose(() => app.MainWindow.Invoke(() => app.MainWindow.Close()));

        var reporter=app.Services.GetRequiredService<HitAutomaticReporter>();
        reporter.Start();
        try { app.Run(); }
        finally
        {
            diagnostics.Record("host","stopping");
            reporter.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.Services.GetRequiredService<DesktopWindowService>().SavePosition();
            inbox.DisposeAsync().AsTask().GetAwaiter().GetResult();
            extensions.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.Services.GetRequiredService<HitExtensionHub>().DisposeAsync().AsTask().GetAwaiter().GetResult();
            settings.UnbindRuntime();
            clock.DisposeAsync().AsTask().GetAwaiter().GetResult();
            app.Services.GetRequiredService<AudioSpectrumService>().Dispose();
            app.Services.GetRequiredService<LyricsService>().Dispose();
            (app.Services.GetRequiredService<IHitMediaService>() as IDisposable)?.Dispose();
            app.Services.GetRequiredService<HitApplicationService>().StartPending();
        }
    }
}
