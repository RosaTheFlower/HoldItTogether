using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace HIT.Sdk;

/// <summary>Called once at startup. Only this module's services belong in this collection.</summary>
public interface IHitModule
{
    void ConfigureServices(IServiceCollection services) { }
    Task StartAsync(IHitApi api, CancellationToken cancellationToken) => Task.CompletedTask;
    Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public interface IHitMenuSettings
{
    bool IsSettingsOpen { get; }
    Task OpenSettingsAsync();
    Task CloseSettingsAsync();
}

public enum HitLayout { Default, Compact }

public sealed record HitContext(bool IsActive, bool IsVisible, bool IsPinned,
    Func<string, Task> SetTitleAsync, Func<HitLayout, Task> SetLayoutAsync,
    Func<bool, Task> SetPinnedAsync, Func<Task> CloseAsync);

/// <summary>Do not inject host internals. Api.Services resolves only module-owned services.</summary>
public abstract class HitMenuComponent : ComponentBase, IAsyncDisposable
{
    [Parameter, EditorRequired] public IHitApi Api { get; set; } = default!;
    [CascadingParameter(Name = "HitExtension")] public HitContext Context { get; set; } = default!;
    private readonly CancellationTokenSource _lifetime = new();
    private CancellationTokenSource? _visible;
    private CancellationTokenRegistration? _moduleCancellation;
    public CancellationToken Lifetime => _lifetime.Token;
    public CancellationToken VisibleLifetime => _visible?.Token ?? new CancellationToken(true);
    protected sealed override void OnParametersSet()
    {
        _moduleCancellation ??= Api.Lifetime.Register(() => _lifetime.Cancel());
        if (Context.IsVisible && _visible is null) _visible = CancellationTokenSource.CreateLinkedTokenSource(Lifetime);
        else if (!Context.IsVisible && _visible is not null) { _visible.Cancel(); _visible.Dispose(); _visible = null; }
        OnContextChanged();
    }
    protected virtual void OnContextChanged() { }
    protected virtual ValueTask OnDisposeAsync() => ValueTask.CompletedTask;
    public async ValueTask DisposeAsync()
    {
        _lifetime.Cancel(); _visible?.Cancel();
        try { await OnDisposeAsync(); }
        finally { _moduleCancellation?.Dispose(); _visible?.Dispose(); _lifetime.Dispose(); }
        GC.SuppressFinalize(this);
    }
}

public interface IHitApi
{
    string ExtensionId { get; }
    CancellationToken Lifetime { get; }
    IServiceProvider Services { get; }
    IHitStorage Storage { get; }
    IHitMedia Media { get; }
    IHitClock Clock { get; }
    IHitAppearance Appearance { get; }
    string AssetUrl(string path);
    bool OpenLink(string url);
    void Log(string message);
    void Notify(string message);
}

public interface IHitStorage
{
    Task<T?> ReadAsync<T>(string key, CancellationToken cancellationToken = default);
    Task WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default);
    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}

public enum HitMediaCommand { TogglePlayback, Previous, Next, Seek, ToggleLoop, ToggleShuffle, ActivateSource }
public sealed record HitMediaState(long Generation, bool HasMedia, string Title, string Artist, string Album,
    string Source, string? Artwork, bool Playing, double Position, double Duration,
    bool CanToggle, bool CanPrevious, bool CanNext, bool CanSeek, bool CanLoop, bool CanShuffle,
    string Loop, bool Shuffle, double SeekMinimum, double SeekMaximum);
public interface IHitMedia
{
    Task<HitMediaState> ReadAsync(CancellationToken cancellationToken = default);
    Task<bool> ExecuteAsync(HitMediaCommand command, long generation, double position = 0, CancellationToken cancellationToken = default);
}
public enum HitClockCommand { ToggleTimer, ResetTimer, TogglePomodoro, ResetPomodoro, SkipPomodoro }
public sealed record HitClockState(DateTimeOffset LocalNow, double TimerRemaining, bool TimerRunning,
    double PomodoroRemaining, bool PomodoroRunning, string PomodoroPhase);
public interface IHitClock
{
    HitClockState Read();
    void StartTimer(TimeSpan duration);
    void Execute(HitClockCommand command);
}
public interface IHitAppearance
{
    string ThemeId { get; }
    bool ReducedMotion { get; }
    void SelectTheme(string themeId);
}
