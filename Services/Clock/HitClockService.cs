using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Nodes;
using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services.Clock;

/// <summary>Persistent deadlines independent of the selected/visible menu.</summary>
public sealed class HitClockService : IAsyncDisposable
{
    private readonly object _sync = new();
    private readonly HitSettingsService _settings;
    private readonly string _path;
    private readonly TimeProvider _time;
    private readonly IClockSound _sound;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task? _worker;
    private ClockState _state = new();
    private bool _dirty, _disposed;
    private string? _storageError;
    public event Action? Changed;
    private static readonly JsonSerializerOptions Json = new()
    { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, WriteIndented = true, Converters = { new JsonStringEnumConverter() } };

    public HitClockService(HitSettingsService settings) : this(settings, Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HoldItTogether", "Clock", "state.json"),
        TimeProvider.System, new ClockSound(), true) { }

    public HitClockService(HitSettingsService settings, string path, TimeProvider time, IClockSound sound, bool runWorker)
    {
        _settings = settings; _path = Path.GetFullPath(path); _time = time; _sound = sound;
        Load();
        _settings.Changed += PreferencesChanged;
        if (runWorker) _worker = RunAsync(_lifetime.Token);
    }

    public ClockSnapshot Read()
    {
        Advance();
        lock (_sync)
        {
            var now = _time.GetUtcNow();
            return new(_time.GetLocalNow(), _state.Timer, _state.Timer.At(now), _state.Pomodoro, _state.Pomodoro.Countdown.At(now),
                Array.AsReadOnly(_state.Notices), _storageError);
        }
    }

    public void StartTimer(TimeSpan duration)
    {
        if (duration < TimeSpan.FromSeconds(1) || duration > TimeSpan.FromHours(24)) throw new ArgumentException("Use um tempo entre 1 segundo e 24 horas.");
        Change((state, now) => state with { Timer = ClockCountdown.Ready(duration) with { Deadline = now + duration } });
    }
    public void ToggleTimer() => Change((state, now) => state with { Timer = Toggle(state.Timer, now) });
    public void ResetTimer() => Change((state, _) => state with { Timer = ClockCountdown.Ready(state.Timer.Duration) });
    public void TogglePomodoro() => Change((state, now) => state with { Pomodoro = state.Pomodoro with { Countdown = Toggle(state.Pomodoro.Countdown, now) } });
    public void ResetPomodoro() => Change((state, _) => state with { Pomodoro = new() { Countdown = ClockCountdown.Ready(PhaseDuration(PomodoroPhase.Focus)) } });
    public void SkipPomodoro() => Change((state, now) => state with { Pomodoro = NextPhase(state.Pomodoro, now, false) });

    public void DismissNotice(Guid id) => Change((state, _) => state with { Notices = state.Notices.Where(item => item.Id != id).ToArray() });

    private static ClockCountdown Toggle(ClockCountdown timer, DateTimeOffset now) => timer.Running
        ? timer with { Remaining = timer.At(now), Deadline = null }
        : timer with { Deadline = now + (timer.Completed ? timer.Duration : timer.Remaining), Completed = false };

    private TimeSpan PhaseDuration(PomodoroPhase phase) => TimeSpan.FromMinutes(phase switch
    { PomodoroPhase.ShortBreak => _settings.Current.Clock.ShortBreakMinutes, PomodoroPhase.LongBreak => _settings.Current.Clock.LongBreakMinutes, _ => _settings.Current.Clock.FocusMinutes });

    private PomodoroState NextPhase(PomodoroState current, DateTimeOffset start, bool countFocus)
    {
        var completed = current.CompletedFocusRounds + (countFocus && current.Phase == PomodoroPhase.Focus ? 1 : 0);
        var phase = current.Phase != PomodoroPhase.Focus ? PomodoroPhase.Focus :
            countFocus && completed > 0 && completed % _settings.Current.Clock.RoundsBeforeLongBreak == 0 ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
        var countdown = ClockCountdown.Ready(PhaseDuration(phase));
        if (_settings.Current.Clock.AutoContinue && current.Countdown.Running) countdown = countdown with { Deadline = start + countdown.Duration };
        return new() { Phase = phase, CompletedFocusRounds = completed, Countdown = countdown };
    }

    public void Advance()
    {
        var notify = false; var changed = false;
        lock (_sync)
        {
            if (_disposed) return;
            var now = _time.GetUtcNow(); var next = _state;
            var notices = new List<ClockNotice>();
            if (next.Timer.Deadline is { } timerDue && timerDue <= now)
            {
                next = next with { Timer = next.Timer with { Deadline = null, Remaining = TimeSpan.Zero, Completed = true } };
                notices.Add(new(Guid.NewGuid(), ClockNoticeKind.Timer, "Temporizador concluído", "O tempo terminou.", now));
            }
            if (next.Pomodoro.Countdown.Deadline is { } pomoDue && pomoDue <= now)
            {
                var phase = ClockFormatting.PhaseName(next.Pomodoro.Phase);
                // After sleep/late processing, start the next phase now instead of crediting unattended cycles.
                var start = now - pomoDue > TimeSpan.FromSeconds(5) ? now : pomoDue;
                var following = NextPhase(next.Pomodoro, start, true);
                next = next with { Pomodoro = following };
                notices.Add(new(Guid.NewGuid(), ClockNoticeKind.Pomodoro, $"{phase} concluído", $"{ClockFormatting.PhaseName(following.Phase)}{(following.Countdown.Running ? " em andamento." : " pronta para iniciar.")}", now));
            }
            if (notices.Count > 0)
            {
                next = next with { Notices = next.Notices.Concat(notices).TakeLast(20).ToArray() };
                _state = next; _dirty = true; changed = true; notify = true;
            }
            if (_dirty)
            {
                try { Save(_state); _dirty = false; changed |= _storageError is not null; _storageError = null; }
                catch (Exception error) when (IsStorageError(error))
                { changed |= _storageError is null; _storageError = "Não foi possível salvar o estado do relógio. Tentando novamente."; }
            }
        }
        if (notify && _settings.Current.Clock.SoundEnabled) _sound.Play();
        if (changed) Changed?.Invoke();
    }

    private void Change(Func<ClockState, DateTimeOffset, ClockState> change)
    {
        Advance();
        lock (_sync)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var next = change(_state, _time.GetUtcNow());
            if (next == _state && !_dirty && _storageError is null) return;
            Save(next); _state = next; _dirty = false; _storageError = null;
        }
        Changed?.Invoke();
    }
    private void PreferencesChanged()
    {
        try
        {
            Change((state, _) => state.Pomodoro.Countdown is { Running: false, Completed: false } timer && timer.Remaining == timer.Duration
                ? state with { Pomodoro = state.Pomodoro with { Countdown = ClockCountdown.Ready(PhaseDuration(state.Pomodoro.Phase)) } } : state);
        }
        catch (Exception error) when (IsStorageError(error))
        { lock (_sync) _storageError = "Não foi possível salvar os ajustes do relógio."; Changed?.Invoke(); }
    }
    private async Task RunAsync(CancellationToken token)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        try { while (await timer.WaitForNextTickAsync(token)) Advance(); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
    }
    private static bool IsStorageError(Exception error) => error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception;
    private void Save(ClockState state)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try { File.WriteAllText(temporary, JsonSerializer.Serialize(state, Json)); File.Move(temporary, _path, true); }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    private void Load()
    {
        _state = new() { Pomodoro = new() { Countdown = ClockCountdown.Ready(PhaseDuration(PomodoroPhase.Focus)) } };
        try
        {
            if (!File.Exists(_path)) return;
            if (new FileInfo(_path).Length > 256 * 1024) throw new JsonException();
            var document = JsonNode.Parse(File.ReadAllText(_path)) as JsonObject ?? throw new JsonException();
            // Migrate older profiles without losing active timers or Pomodoro deadlines.
            var migrated = document.Remove("reminders");
            if (document["notices"] is JsonArray notices)
            {
                for (var index = notices.Count - 1; index >= 0; index--)
                {
                    var kind = (notices[index] as JsonObject)?["kind"] as JsonValue;
                    var valid = kind is not null &&
                        (kind.TryGetValue<string>(out var text) && Enum.TryParse<ClockNoticeKind>(text, true, out var parsed) && Enum.IsDefined(parsed) ||
                         kind.TryGetValue<int>(out var number) && Enum.IsDefined((ClockNoticeKind)number));
                    if (!valid) { notices.RemoveAt(index); migrated = true; }
                }
            }
            var loaded = document.Deserialize<ClockState>(Json) ?? throw new JsonException();
            static ClockCountdown Clean(ClockCountdown? value, TimeSpan fallback)
            {
                if (value is null || value.Duration < TimeSpan.FromSeconds(1) || value.Duration > TimeSpan.FromHours(24)) return ClockCountdown.Ready(fallback);
                return value with { Remaining = TimeSpan.FromTicks(Math.Clamp(value.Remaining.Ticks, 0, value.Duration.Ticks)) };
            }
            var pomo = loaded.Pomodoro ?? new();
            _state = loaded with
            {
                Timer = Clean(loaded.Timer, TimeSpan.FromMinutes(5)),
                Pomodoro = pomo with { Phase = Enum.IsDefined(pomo.Phase) ? pomo.Phase : PomodoroPhase.Focus,
                    CompletedFocusRounds = Math.Clamp(pomo.CompletedFocusRounds, 0, 1000000), Countdown = Clean(pomo.Countdown, PhaseDuration(PomodoroPhase.Focus)) },
                Notices = (loaded.Notices ?? []).Where(item => item is not null && item.Id != Guid.Empty && !string.IsNullOrWhiteSpace(item.Title) && item.Title.Length <= 160 && item.Message?.Length <= 240).TakeLast(20).ToArray()
            };
            _dirty = migrated;
        }
        catch (Exception error) when (IsStorageError(error) || error is JsonException)
        { _storageError = "Não foi possível restaurar o estado salvo do relógio."; }
    }
    public async ValueTask DisposeAsync()
    {
        lock (_sync) { if (_disposed) return; _disposed = true; }
        _settings.Changed -= PreferencesChanged;
        _lifetime.Cancel();
        if (_worker is not null) await _worker;
        _lifetime.Dispose();
    }
}
