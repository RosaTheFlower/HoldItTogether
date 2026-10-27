using System.Text.Json;
using System.Text.RegularExpressions;
using HIT.Sdk;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Clock;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Themes;

namespace HoldItTogether.Services.Extensions;

public sealed class ExtensionApi : IHitApi, IHitMedia, IHitClock, IHitAppearance
{
    private readonly ExtensionManifest _manifest;
    private readonly ExtensionStore _store;
    private readonly IHitMediaService _media;
    private readonly HitClockService _clock;
    private readonly HitSettingsService _settings;
    private readonly HitThemeService _themes;
    private readonly IExternalLinkService _links;
    private readonly ExtensionUi _ui;
    public string ExtensionId => _manifest.Id;
    public CancellationToken Lifetime { get; }
    public IServiceProvider Services { get; internal set; } = default!;
    public IHitStorage Storage { get; }
    public IHitMedia Media => this;
    public IHitClock Clock => this;
    public IHitAppearance Appearance => this;
    public string ThemeId => _settings.Current.Theme;
    public bool ReducedMotion => _settings.Current.RemoveAnimations;
    public ExtensionApi(ExtensionManifest manifest, ExtensionStore store, IHitMediaService media, HitClockService clock,
        HitSettingsService settings, HitThemeService themes, IExternalLinkService links, ExtensionUi ui, CancellationToken lifetime = default)
    {
        _manifest = manifest; _store = store; _media = media; _clock = clock; _settings = settings; _themes = themes; _links = links; _ui = ui;
        Lifetime = lifetime;
        Storage = new ExtensionStorage(Path.Combine(store.Root, "State", manifest.Id));
    }
    public void Demand(string capability)
    {
        if (!_manifest.Capabilities.Contains(capability, StringComparer.Ordinal)) throw new InvalidOperationException($"This extension did not declare '{capability}'.");
    }
    public string AssetUrl(string path)
    {
        ExtensionManifest.SafePath(_store.PackagePath(_manifest), path);
        return $"extensions/{ExtensionId}/{_manifest.Version}/" + string.Join('/', path.Split('/').Select(Uri.EscapeDataString));
    }
    public bool OpenLink(string url) { Demand("links.open"); return _links.OpenWebPage(url); }
    public void Notify(string message) { Demand("notifications"); _ui.Notify(_manifest.Name, message[..Math.Min(300, message.Length)]); }
    public void Log(string message)
    {
        var path = Path.Combine(_store.Root, "Logs", ExtensionId + ".log");
        lock (_store)
        {
            if (File.Exists(path) && new FileInfo(path).Length > 256 * 1024) File.Move(path, path + ".previous", true);
            File.AppendAllText(path, $"{DateTimeOffset.UtcNow:O} {message[..Math.Min(2000, message.Length)]}{Environment.NewLine}");
        }
    }
    public async Task<HitMediaState> ReadAsync(CancellationToken cancellationToken = default)
    {
        Demand("media.read"); var state = await _media.ReadAsync(cancellationToken);
        return new(state.Generation, state.HasMedia, state.Title, state.Artist, state.Album, state.Source, state.Artwork,
            state.Playing, state.Position, state.Duration, state.CanToggle, state.CanPrevious, state.CanNext, state.CanSeek,
            state.CanLoop, state.CanShuffle, state.Loop.ToString(), state.Shuffle, state.SeekMinimum, state.SeekMaximum);
    }
    public Task<bool> ExecuteAsync(HitMediaCommand command, long generation, double position = 0, CancellationToken cancellationToken = default)
    {
        Demand("media.control");
        if (!Enum.IsDefined(command) || !double.IsFinite(position)) throw new ArgumentException("Invalid media command.");
        return _media.ExecuteAsync((MediaAction)(int)command, generation, position, cancellationToken);
    }
    public HitClockState Read()
    {
        Demand("clock.read"); var state = _clock.Read();
        return new(state.LocalNow, state.TimerRemaining.TotalSeconds, state.Timer.Running,
            state.PomodoroRemaining.TotalSeconds, state.Pomodoro.Countdown.Running, state.Pomodoro.Phase.ToString());
    }
    public void StartTimer(TimeSpan duration) { Demand("clock.control"); _clock.StartTimer(duration); }
    public void Execute(HitClockCommand command)
    {
        Demand("clock.control");
        switch (command)
        {
            case HitClockCommand.ToggleTimer: _clock.ToggleTimer(); break;
            case HitClockCommand.ResetTimer: _clock.ResetTimer(); break;
            case HitClockCommand.TogglePomodoro: _clock.TogglePomodoro(); break;
            case HitClockCommand.ResetPomodoro: _clock.ResetPomodoro(); break;
            case HitClockCommand.SkipPomodoro: _clock.SkipPomodoro(); break;
            default: throw new ArgumentException("Invalid clock command.");
        }
    }
    public void SelectTheme(string themeId)
    {
        Demand("theme.control");
        if (!_themes.GetThemes().Any(theme => theme.Id == themeId)) throw new ArgumentException("Theme not found.");
        _settings.Update(value => value with { Theme = themeId });
    }
}

internal sealed class ExtensionStorage(string directory) : IHitStorage
{
    private readonly SemaphoreSlim _gate = new(1);
    private string FilePath(string key)
    {
        if (key.Length > 80 || !Regex.IsMatch(key, "^[a-zA-Z0-9_-]+$")) throw new ArgumentException("Use a simple storage key.");
        return Path.Combine(directory, key + ".json");
    }
    public async Task<T?> ReadAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = FilePath(key);
            if (!File.Exists(path)) return default;
            if (new FileInfo(path).Length > 1024 * 1024) throw new IOException("Stored value exceeds 1 MB.");
            return JsonSerializer.Deserialize<T>(await File.ReadAllTextAsync(path, cancellationToken), ExtensionStore.Json);
        }
        finally { _gate.Release(); }
    }
    public async Task WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(value, ExtensionStore.Json);
        if (bytes.Length > 1024 * 1024) throw new IOException("Stored values must not exceed 1 MB.");
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var path = FilePath(key); Directory.CreateDirectory(directory);
            var size = Directory.EnumerateFiles(directory, "*.json").Where(item => item != path).Sum(item => new FileInfo(item).Length);
            if (size + bytes.Length > 16 * 1024 * 1024) throw new IOException("Extension storage exceeds 16 MB.");
            await File.WriteAllBytesAsync(path + ".tmp", bytes, cancellationToken);
            File.Move(path + ".tmp", path, true);
        }
        finally { _gate.Release(); }
    }
    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try { File.Delete(FilePath(key)); } finally { _gate.Release(); }
    }
}

public sealed class ExtensionUi
{
    public (string Title, string Message)? Notice { get; private set; }
    public event Action? NoticeChanged;
    public void Notify(string title, string message) { Notice = (title, message); NoticeChanged?.Invoke(); }
    public void DismissNotice() { Notice = null; NoticeChanged?.Invoke(); }
    public bool ShowExtensions { get; set; }
    public bool ShowThemes { get; set; }
    public string? SelectedThemeId { get; private set; }
    public event Action? OpenRequested;
    public event Action? Changed;
    public string? Message { get; private set; }
    public bool IsError { get; private set; }
    public void Open() { ShowThemes = false; ShowExtensions = true; OpenRequested?.Invoke(); }
    public void OpenThemes(string? selected = null) { SelectedThemeId = selected; ShowExtensions = false; ShowThemes = true; OpenRequested?.Invoke(); }
    public void Report(string message, bool error = false) { Message = message; IsError = error; Changed?.Invoke(); }
    public void Clear() { Message = null; Changed?.Invoke(); }
}
