using System.Text.Json;

namespace HoldItTogether.Services.Settings;

public sealed class HitSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private readonly object _sync = new();
    private readonly string _path;
    private HitSettings _current;
    private Action<HitSettings>? _applyRuntime;

    public HitSettingsService() : this(Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "HoldItTogether", "settings.json"))
    {
    }

    public HitSettingsService(string path)
    {
        _path = Path.GetFullPath(path);
        _current = Load();
    }

    public HitSettings Current => Volatile.Read(ref _current);
    public string? RuntimeError { get; private set; }
    public event Action? Changed;

    public void BindRuntime(Action<HitSettings> apply)
    {
        _applyRuntime = apply;
        try
        {
            apply(Current);
            RuntimeError = null;
        }
        catch (Exception error) when (error is InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            // Explorer may be unavailable during startup. Keep the app usable
            // and report the problem in Settings instead of crashing startup.
            RuntimeError = error.Message;
        }
    }

    public void UnbindRuntime() => _applyRuntime = null;

    public void Update(Func<HitSettings, HitSettings> change)
    {
        lock (_sync)
        {
            var previous = _current;
            var next = change(previous).Normalize();
            if (next == previous && RuntimeError is null)
            {
                return;
            }

            try
            {
                _applyRuntime?.Invoke(next);
                Save(next);
                Volatile.Write(ref _current, next);
                RuntimeError = null;
            }
            catch
            {
                try { _applyRuntime?.Invoke(previous); }
                catch { /* Preserve the original apply/save error. */ }
                throw;
            }
        }

        Changed?.Invoke();
    }

    private HitSettings Load()
    {
        try
        {
            if (File.Exists(_path))
            {
                return (JsonSerializer.Deserialize<HitSettings>(File.ReadAllText(_path), JsonOptions)
                        ?? new HitSettings()).Normalize();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        {
            // A damaged settings file must not prevent the app from opening.
        }

        return new HitSettings();
    }

    private void Save(HitSettings settings)
    {
        var directory = Path.GetDirectoryName(_path)!;
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".settings-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
            File.Move(temporary, _path, true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }
}
