using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HoldItTogether.Services.Localization;

public sealed record HitLanguage(string Locale, string Name, Dictionary<string, string> Messages);

/// <summary>Bounded data-only catalogs. Selection takes effect on the next application start.</summary>
public sealed class HitLanguageStore
{
    public const int MaximumBytes = 1024 * 1024;
    private static Lazy<HitLanguageStore> DefaultStore = new(() => new(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoldItTogether", "Languages")));
    private static Lazy<HitLanguage> Active = new(() => Default.Selected());
    // Test hosts configure a disposable profile before constructing any components.
    internal static void ConfigureTestProfile(string root)
    {
        if (Active.IsValueCreated) throw new InvalidOperationException("Language profile is already active.");
        DefaultStore = new(() => new(root));
        Active = new(() => Default.Selected());
    }
    public static HitLanguageStore Default => DefaultStore.Value;
    public static HitLanguage Current => Active.Value;
    private readonly string _root;
    public HitLanguageStore(string root) => _root = Path.GetFullPath(root);
    public IReadOnlyList<HitLanguage> Installed()
    {
        var result = new List<HitLanguage> { new("en-US", "English", []) };
        using var stream = typeof(HitText).Assembly.GetManifestResourceStream("HoldItTogether.Resources.pt-BR.json");
        if (stream is not null) result.Add(Parse(ReadBounded(stream)));
        if (Directory.Exists(_root))
            foreach (var path in Directory.EnumerateFiles(_root, "*.json").Where(p => Path.GetFileName(p) != "selection.json").Take(32))
                try
                {
                    using var file = File.OpenRead(path);
                    var language = Parse(ReadBounded(file));
                    if (!result.Any(item => item.Locale.Equals(language.Locale, StringComparison.OrdinalIgnoreCase))) result.Add(language);
                }
                catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException or ArgumentException or FormatException) { }
        return result;
    }
    public string SelectedLocale
    {
        get
        {
            try
            {
                using var file = File.OpenRead(Path.Combine(_root, "selection.json"));
                return JsonSerializer.Deserialize<string>(ReadBounded(file)) ?? "en-US";
            }
            catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException or JsonException) { return "en-US"; }
        }
    }
    private HitLanguage Selected()
    {
        try { return Installed().FirstOrDefault(item => item.Locale == SelectedLocale) ?? new("en-US", "English", []); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return new("en-US", "English", []); }
    }
    public void Select(string locale)
    {
        if (!Installed().Any(item => item.Locale == locale)) throw new InvalidDataException("Unknown language.");
        Write("selection.json", JsonSerializer.SerializeToUtf8Bytes(locale));
    }
    public HitLanguage Import(byte[] bytes)
    {
        var language = Parse(bytes);
        if (language.Locale is "en-US" or "pt-BR") throw new InvalidDataException("Built-in languages cannot be replaced.");
        if (!Installed().Any(item => item.Locale == language.Locale) && Installed().Count >= 34)
            throw new InvalidDataException("Too many languages.");
        Write(language.Locale + ".json", bytes);
        return language;
    }
    private void Write(string name, byte[] bytes)
    {
        Directory.CreateDirectory(_root);
        var path = Path.Combine(_root, name);
        var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllBytes(temporary, bytes); File.Move(temporary, path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private static byte[] ReadBounded(Stream stream)
    {
        using var output = new MemoryStream();
        var buffer = new byte[8192];
        int count;
        while ((count = stream.Read(buffer)) > 0)
        {
            if (output.Length + count > MaximumBytes) throw new InvalidDataException("Language file exceeds 1 MB.");
            output.Write(buffer, 0, count);
        }
        return output.ToArray();
    }
    public static HitLanguage Parse(byte[] bytes)
    {
        if (bytes.Length > MaximumBytes) throw new InvalidDataException("Language file exceeds 1 MB.");
        using var document = JsonDocument.Parse(bytes, new JsonDocumentOptions { MaxDepth = 8 });
        var root = document.RootElement;
        if (root.ValueKind != JsonValueKind.Object) throw new InvalidDataException("Invalid language catalog.");
        var fields = root.EnumerateObject().ToArray();
        if (fields.Length != 4 || fields.Select(p => p.Name).Distinct().Count() != 4 ||
            !root.TryGetProperty("version", out var version) || version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out var number) || number != 1 ||
            !root.TryGetProperty("locale", out var localeValue) || localeValue.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("name", out var nameValue) || nameValue.ValueKind != JsonValueKind.String ||
            !root.TryGetProperty("messages", out var messages) || messages.ValueKind != JsonValueKind.Object)
            throw new InvalidDataException("Invalid language catalog.");
        var locale = localeValue.GetString()!;
        var name = nameValue.GetString()!;
        if (!Regex.IsMatch(locale, "^[a-z]{2,3}(-[A-Z]{2})?$") || CultureInfo.GetCultureInfo(locale).Name != locale ||
            string.IsNullOrWhiteSpace(name) || name.Length > 64 || name.Any(char.IsControl))
            throw new InvalidDataException("Invalid language metadata.");
        var catalog = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var entry in messages.EnumerateObject())
        {
            if (!HitText.Source.ContainsKey(entry.Name) || entry.Value.ValueKind != JsonValueKind.String)
                throw new InvalidDataException("Unknown language message.");
            var text = entry.Value.GetString()!;
            if (string.IsNullOrWhiteSpace(text) || text.Length > 8192 || !catalog.TryAdd(entry.Name, text))
                throw new InvalidDataException("Invalid language message.");
            var source = CompositeFormat.Parse(entry.Name);
            var translated = CompositeFormat.Parse(text);
            foreach (Match alignment in Regex.Matches(text, @"\{\d+,\s*(-?\d+)"))
                if (!int.TryParse(alignment.Groups[1].Value, out var width) || width is < -8192 or > 8192)
                    throw new InvalidDataException("Translation alignment is too large.");
            static string Arguments(string value) => string.Join("\n", Regex.Matches(value.Replace("{{", "").Replace("}}", ""), @"\{\d+(?:,[^{}:]+)?(?::[^{}]+)?\}").Select(m => m.Value).Order());
            if (source.MinimumArgumentCount != translated.MinimumArgumentCount || Arguments(entry.Name) != Arguments(text))
                throw new InvalidDataException("Translation placeholders do not match.");
            // Exercise format specifiers with string arguments before accepting the file.
            _ = string.Format(CultureInfo.InvariantCulture, text, Enumerable.Repeat<object>("", translated.MinimumArgumentCount).ToArray());
        }
        if (catalog.Count == 0) throw new InvalidDataException("Empty language catalog.");
        return new(locale, name, catalog);
    }
}
