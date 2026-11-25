using System.Globalization;
using System.Text.Json;

namespace HoldItTogether.Services.Localization;

/// <summary>English source messages are stable keys. Language catalogs provide overrides, never code.</summary>
public static class HitText
{
    public static CultureInfo Culture => CultureInfo.GetCultureInfo(HitLanguageStore.Current.Locale);
    private static readonly Lazy<IReadOnlyDictionary<string, string>> English = new(() =>
    {
        using var stream = typeof(HitText).Assembly.GetManifestResourceStream("HoldItTogether.Resources.en.json");
        return stream is null ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    });
    internal static IReadOnlyDictionary<string, string> Source => English.Value;
    public static string Get(string key) => HitLanguageStore.Current.Messages.TryGetValue(key, out var translated) ? translated : English.Value.TryGetValue(key, out var value) ? value : key;
    public static string Format(string key, params object?[] values) => string.Format(Culture, Get(key), values);
}
