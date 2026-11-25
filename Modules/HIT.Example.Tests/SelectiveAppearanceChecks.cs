using System.Text;
using System.Text.Json;
using HoldItTogether.Components.Settings;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

internal static class SelectiveAppearanceChecks
{
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }

    public static async Task RunAsync(string repo)
    {
        var root = Path.GetFullPath("obj/selective-appearance-tests/" + Guid.NewGuid().ToString("N"));
        var settings = new HitSettingsService(Path.Combine(root, "settings.json"));
        var themes = new HitThemeService(settings, Path.Combine(root, "Themes"), Path.Combine(repo, "wwwroot/themes"));
        var appearance = new HitAppearanceService(settings, themes, Path.Combine(root, "Appearance"));
        const string baseCss = "@layer sphere {:root {--hit-background:#0a4020;--hit-open-background:#0a4020;--hit-text:#b7f0b2;--hit-font:'Courier New',monospace;} .dynamic-sphere {background-color:#0a4020;} .dynamic-sphere.is-open {border-radius:7px;outline:3px dotted #00ff22;} .dynamic-sphere .sphere-title {font-size:19px;font-weight:400;letter-spacing:2px;}}";
        await using var source = new MemoryStream(Encoding.UTF8.GetBytes(baseCss));
        var theme = await themes.ImportAsync("Old TV.css", source);
        themes.Select(theme);
        var initial = new HitAppearancePreferences();
        Check((initial with { Enabled = true }).Css() == "", "Enabling personalization alone must preserve the active CSS theme.");
        var image = await appearance.ImportImageAsync(new MemoryStream(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a0ZsAAAAASUVORK5CYII=")));
        var wallpaper = (initial with { Enabled = true, BackgroundMedia = [image] }).WithEditsFrom(initial);
        Check(wallpaper.EditedProperties == nameof(HitAppearancePreferences.BackgroundMedia), "A wallpaper edit owns only the media selection.");
        appearance.Apply(wallpaper, theme);
        var restored = new HitSettingsService(Path.Combine(root, "settings.json")).Current.Appearance;
        Check(restored.EditedProperties == wallpaper.EditedProperties && restored.MediaIds.SequenceEqual(wallpaper.MediaIds), "Edited properties and wallpaper survive restart.");
        var profile = appearance.Save("Wallpaper over CSS", wallpaper, theme);
        Check(appearance.Profiles().Single().Appearance.EditedProperties == wallpaper.EditedProperties, "Profiles preserve which properties inherit from the theme.");
        var css = appearance.Css(restored, true);
        Check(css.Contains("data:image/png;base64,") && !css.Contains("--hit-background:") && !css.Contains("font-family:") && !css.Contains("outline") && !css.Contains("box-shadow"), "Wallpaper CSS leaves palette, typography, borders and shadows inherited.");
        var defaults = new HitAppearancePreferences();
        var color = (wallpaper with { Background = defaults.Background }).WithEditsFrom(wallpaper, nameof(HitAppearancePreferences.Background));
        Check(appearance.Css(color).Contains("--hit-background:#17191e;") && !appearance.Css(color).Contains("font-family:"), "Explicitly editing a value equal to an editor default still overrides that property alone.");
        var radius = (wallpaper with { Radius = defaults.Radius }).WithEditsFrom(wallpaper, nameof(HitAppearancePreferences.Radius));
        Check(radius.Css().Contains("border-radius:22px!important;") && !radius.Css().Contains("outline"), "A same-value radius edit does not claim the border.");
        var legacy = new HitAppearancePreferences { Enabled = true, BackgroundImage = image };
        Check(!appearance.Css(legacy).Contains("--hit-background:"), "Legacy wallpaper-only snapshots inherit unmodified editor defaults.");
        Check((legacy with { Accent = "#123456" }).Css().Contains("--hit-accent:#123456;"), "Legacy non-default choices remain active.");
        var oldFontDefault = legacy with { Font = "system" };
        Check(!appearance.Css(oldFontDefault).Contains("font-family:"), "The historical system-font default must not replace a CSS theme's font.");
        Check(oldFontDefault.WithEditsFrom(oldFontDefault, nameof(HitAppearancePreferences.Font)).Css().Contains("Segoe UI"), "Explicitly confirming the stored system preset still applies it.");
        var safe = (wallpaper with { EditedProperties = "Unknown,Background,Background,;}body{display:none" }).Normalize();
        Check(safe.EditedProperties == "Background", "Persisted property names are allowlisted and normalized.");
        var previewed = (initial with { Enabled = true, BorderStyle = "dashed" }).WithEditsFrom(initial, nameof(HitAppearancePreferences.BorderStyle));
        var concurrentWallpaper = (previewed with { BackgroundMedia = [image] }).WithEditsFrom(previewed);
        var cancelled = concurrentWallpaper.RestoreSelectFrom(initial, nameof(HitAppearancePreferences.BorderStyle));
        Check(cancelled.Enabled && cancelled.MediaIds.SequenceEqual(new[] { image }) && cancelled.EditedProperties == nameof(HitAppearancePreferences.BackgroundMedia), "Cancelling a picker restores inheritance without losing a concurrent wallpaper import.");
        var exported = appearance.Export(profile.Appearance, profile.Theme);
        await using var exportedSource = File.OpenRead(exported);
        await themes.ImportAsync("Old TV with wallpaper.css", exportedSource);
        Directory.CreateDirectory("obj/personalization-tests");
        await File.WriteAllTextAsync("obj/personalization-tests/inherited-theme.css", baseCss);
        await File.WriteAllTextAsync("obj/personalization-tests/wallpaper-only.css", css);
        await File.WriteAllTextAsync("obj/personalization-tests/explicit-background.css", appearance.Css(color, true));
        await File.WriteAllTextAsync("obj/personalization-tests/selective-export.css", File.ReadAllText(exported));
        Check(JsonSerializer.Deserialize<HitAppearancePreferences>(JsonSerializer.Serialize(color))!.EditedProperties == color.EditedProperties, "Explicit default edits retain their identity through JSON.");
        await using var services = new ServiceCollection().AddLogging().AddSingleton(settings).AddSingleton(appearance).AddSingleton<IJSRuntime,NoJs>().BuildServiceProvider();
        await using var renderer = new InteractiveRenderer(services, services.GetRequiredService<ILoggerFactory>());
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var (id, view) = await renderer.MountAsync<AppearanceView>(new() { [nameof(AppearanceView.ShowBack)] = false, [nameof(AppearanceView.Page)] = "Shape and typography", [nameof(AppearanceView.SphereMode)] = false });
            await renderer.DispatchEventAsync(renderer.Event(id, "select", "aria-label", "Custom border style", "onhitselectstart"), null, EventArgs.Empty);
            await renderer.DispatchEventAsync(renderer.Event(id, "select", "aria-label", "Custom border style", "onchange"), null, new ChangeEventArgs { Value = "dashed" });
            await renderer.DispatchEventAsync(renderer.Event(id, "select", "aria-label", "Custom border style", "onchange"), null, new ChangeEventArgs { Value = "solid" });
            await renderer.DispatchEventAsync(renderer.Event(id, "select", "aria-label", "Custom border style", "onhitselectcancel"), null, EventArgs.Empty);
            Check(appearance.Current.EditedProperties == wallpaper.EditedProperties && !appearance.Css(appearance.Current).Contains("outline"), "Escape restores inherited theme styling, not just the select's displayed default.");
            await renderer.DispatchEventAsync(renderer.Event(id, "select", "aria-label", "Custom border style", "onchange"), null, new ChangeEventArgs { Value = "solid" });
            Check(appearance.Css(appearance.Current).Contains("outline-style:solid;") && !appearance.Css(appearance.Current).Contains("outline-width:"), "Confirming an unchanged editor default overrides only that property through the real view.");
            await view.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string,object?> { [nameof(AppearanceView.Page)] = "", [nameof(AppearanceView.SphereMode)] = false }));
            await renderer.DispatchEventAsync(renderer.Event(id, "button", "aria-label", "Restore theme defaults", "onclick"), null, new MouseEventArgs());
            Check(appearance.Current is { Enabled: false, EditedProperties: "" }, "Restore defaults clears every property override.");
            await renderer.DispatchEventAsync(renderer.Event(id, "button", "aria-label", "Undo appearance changes", "onclick"), null, new MouseEventArgs());
            Check(appearance.Current.EditedProperties == wallpaper.EditedProperties && appearance.Current.MediaIds.SequenceEqual(wallpaper.MediaIds), "Undo restores wallpaper and inheritance after reset.");
            view.Flush();
            Check(settings.Current.Appearance.EditedProperties == wallpaper.EditedProperties, "Undo persists the restored inheritance selection.");
            Check(renderer.Errors.Count == 0, "Selective editing/reset/Undo must not produce renderer errors.");
        });
        Console.WriteLine("PASS: CSS-theme inheritance, wallpaper-only overrides, explicit default edits, legacy migration, profile/restart persistence and portable selective export/reimport.");
    }
}
