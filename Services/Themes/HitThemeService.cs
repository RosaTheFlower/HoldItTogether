using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.Themes;

public sealed record HitThemeOption(string Id, string Name, bool CanDelete);
public sealed record HitThemeSnapshot(string Revision, string Css, string? Error);

/// <summary>Reads public CSS. Selecting/deleting/importing themes never writes bundled files.</summary>
public sealed class HitThemeService
{
    public const string DefaultFileName = "HIT-OBSIDIAN-EYES.css";
    public const int MaxCssBytes = 1024 * 1024;
    private readonly HitSettingsService _settings;
    private readonly string _bundledDirectory;
    public string DirectoryPath { get; }
    public string? RuntimeError { get; internal set; }

    public HitThemeService(HitSettingsService settings) : this(settings,
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "HoldItTogether", "Themes"),
        Path.Combine(AppContext.BaseDirectory, "wwwroot", "themes")) { }

    public HitThemeService(HitSettingsService settings, string directory, string bundledDirectory)
    {
        _settings = settings;
        DirectoryPath = Path.GetFullPath(directory);
        _bundledDirectory = Path.GetFullPath(bundledDirectory);
    }

    public IReadOnlyList<HitThemeOption> GetThemes()
    {
        var themes = new List<HitThemeOption> { new("default", "HIT Obsidian Eyes", false) };
        // Only the canonical default is bundled. Old build remnants are never offered as themes.
        AddCustomThemes(themes);
        return themes.AsReadOnly();
    }

    private void AddCustomThemes(List<HitThemeOption> themes)
    {
        if (!Directory.Exists(DirectoryPath)) return;
        foreach (var path in Directory.EnumerateFiles(DirectoryPath, "*.css").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            themes.Add(new("custom:" + Path.GetFileName(path), Path.GetFileNameWithoutExtension(path), true));
    }

    public HitThemeSnapshot ReadBase() => ReadDefault();
    public HitThemeSnapshot ReadCurrent()
    {
        var id = _settings.Current.Theme;
        // Compatibility with selections saved before Obsidian became the default.
        if (id == "default" || id.StartsWith("bundled:", StringComparison.Ordinal))
            return ReadDefault();
        try
        {
            var path = Resolve(id);
            if (!File.Exists(path))
                return ReadDefault("Tema não encontrado. O HIT está usando Obsidian Eyes.");
            return ReadCss(path, id);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException)
        {
            return ReadDefault("Não foi possível ler o tema. O HIT está usando Obsidian Eyes.");
        }
    }

    private HitThemeSnapshot ReadDefault(string? error = null)
    {
        var path = Path.Combine(_bundledDirectory, DefaultFileName);
        try
        {
            if (!File.Exists(path)) return new("default:" + error, "", error);
            var snapshot = ReadCss(path, "default");
            return snapshot with { Revision = snapshot.Revision + ":" + error, Error = error ?? snapshot.Error };
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            return new("default-error", "", "Não foi possível carregar o tema padrão.");
        }
    }

    private static HitThemeSnapshot ReadCss(string path, string id)
    {
        if (new FileInfo(path).Length > MaxCssBytes)
            return new("large:" + id, "", "O tema deve ter no máximo 1 MB.");
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        var css = reader.ReadToEnd();
        if (Encoding.UTF8.GetByteCount(css) > MaxCssBytes)
            return new("large:" + id, "", "O tema deve ter no máximo 1 MB.");
        return new(id + ":" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(css))), css, null);
    }
    public void Select(string id)
    {
        if (!GetThemes().Any(theme => theme.Id == id))
            throw new ArgumentException("Tema não encontrado.");
        _settings.Update(value => value with { Theme = id });
    }

    public async Task<string> ImportAsync(string fileName, Stream source)
    {
        if (!fileName.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Selecione um arquivo CSS.");
        using var buffer = new MemoryStream();
        var block = new byte[8192];
        int count;
        while ((count = await source.ReadAsync(block)) > 0)
        {
            if (buffer.Length + count > MaxCssBytes) throw new IOException("O tema deve ter no máximo 1 MB.");
            await buffer.WriteAsync(block.AsMemory(0, count));
        }
        var name = Path.GetFileNameWithoutExtension(fileName);
        name = string.Concat(name.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_' or ' ')).Trim();
        if (string.IsNullOrEmpty(name)) name = "Meu tema";
        name = name[..Math.Min(name.Length, 64)];
        Directory.CreateDirectory(DirectoryPath);
        for (var suffix = 0; ; suffix++)
        {
            var destinationName = name + (suffix == 0 ? "" : " " + suffix) + ".css";
            var path = Path.Combine(DirectoryPath, destinationName);
            FileStream target;
            try { target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.Read); }
            catch (IOException) when (File.Exists(path)) { continue; }
            try
            {
                await using (target)
                {
                    buffer.Position = 0;
                    await buffer.CopyToAsync(target);
                }
                return "custom:" + destinationName;
            }
            catch
            {
                File.Delete(path);
                throw;
            }
        }
    }

    public void Delete(string id)
    {
        if (!id.StartsWith("custom:", StringComparison.Ordinal))
            throw new InvalidOperationException("O tema padrão não pode ser excluído.");
        var path = Resolve(id);
        // Persist the fallback before deleting, so a failed save cannot strand the selection.
        if (_settings.Current.Theme == id) Select("default");
        File.Delete(path);
    }

    public void OpenFolder()
    {
        Directory.CreateDirectory(DirectoryPath);
        Process.Start(new ProcessStartInfo { FileName = DirectoryPath, UseShellExecute = true });
    }

    private string Resolve(string id)
    {
        var bundled = id.StartsWith("bundled:", StringComparison.Ordinal);
        if (!bundled && !id.StartsWith("custom:", StringComparison.Ordinal))
            throw new ArgumentException("Identificador de tema inválido.");
        var name = id[(bundled ? 8 : 7)..];
        if (string.IsNullOrWhiteSpace(name) || name != Path.GetFileName(name) ||
            name.Contains('/') || name.Contains('\\') || !name.EndsWith(".css", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Nome de tema inválido.");
        return Path.Combine(bundled ? _bundledDirectory : DirectoryPath, name);
    }
}