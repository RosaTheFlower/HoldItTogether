using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Components;
using HoldItTogether.Services;
using HoldItTogether.Services.HitMenus;
using Microsoft.Extensions.DependencyInjection;
using Photino.Blazor;

namespace HoldItTogether;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        var builder = PhotinoBlazorAppBuilder.CreateDefault(args);

        builder.Services.AddLogging();
        builder.Services.AddSingleton<HitSettingsService>();
        builder.Services.AddSingleton<HitMemorySampler>();
        builder.Services.AddSingleton<HitThemeService>();
        builder.Services.AddSingleton<DesktopWindowService>();
        builder.Services.AddSingleton<HitMenuCatalog>();
        builder.RootComponents.Add<App>("app");

        var app = builder.Build();
        var settings = app.Services.GetRequiredService<HitSettingsService>();

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
        finally { settings.UnbindRuntime(); }
    }
}
