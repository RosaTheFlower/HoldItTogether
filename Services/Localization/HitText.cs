using System.Globalization;
using System.Text.Json;

namespace HoldItTogether.Services.Localization;

/// <summary>English source messages are stable keys. Future language packages provide overrides, never code.</summary>
public static class HitText
{
    public static CultureInfo Culture { get; } = CultureInfo.GetCultureInfo("en-US");
    private static readonly Lazy<IReadOnlyDictionary<string, string>> English = new(() =>
    {
        using var stream = typeof(HitText).Assembly.GetManifestResourceStream("HoldItTogether.Resources.en.json");
        return stream is null ? new Dictionary<string, string>() : JsonSerializer.Deserialize<Dictionary<string, string>>(stream) ?? [];
    });
    public static string Get(string key) => English.Value.TryGetValue(key, out var value) ? value : key;
    public static string Format(string key, params object?[] values) => string.Format(Culture, Get(key), values);
}
