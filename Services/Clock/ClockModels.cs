namespace HoldItTogether.Services.Clock;

public enum ClockMode { System, Timer, Pomodoro }
public enum PomodoroPhase { Focus, ShortBreak, LongBreak }
public enum ClockNoticeKind { Timer, Pomodoro }

public sealed record ClockPreferences
{
    public bool ShowSeconds { get; init; } = true;
    public bool ShowTwoClocks { get; init; }
    public SystemClock[] SystemClocks { get; init; } = [new("System", ClockTimeZones.LocalId), new("New York", "Eastern Standard Time")];
    public ClockMode SelectedMode { get; init; }
    public int FocusMinutes { get; init; } = 25;
    public int ShortBreakMinutes { get; init; } = 5;
    public int LongBreakMinutes { get; init; } = 15;
    public int RoundsBeforeLongBreak { get; init; } = 4;
    public bool AutoContinue { get; init; } = true;
    public bool SoundEnabled { get; init; } = true;

    public ClockPreferences Normalize() => this with
    {
        SelectedMode = Enum.IsDefined(SelectedMode) ? SelectedMode : ClockMode.System,
        SystemClocks = ClockTimeZones.Normalize(SystemClocks),
        FocusMinutes = Math.Clamp(FocusMinutes, 1, 180), ShortBreakMinutes = Math.Clamp(ShortBreakMinutes, 1, 60),
        LongBreakMinutes = Math.Clamp(LongBreakMinutes, 1, 120), RoundsBeforeLongBreak = Math.Clamp(RoundsBeforeLongBreak, 2, 12)
    };
}

public sealed record ClockCountdown
{
    public TimeSpan Duration { get; init; } = TimeSpan.FromMinutes(5);
    public TimeSpan Remaining { get; init; } = TimeSpan.FromMinutes(5);
    public DateTimeOffset? Deadline { get; init; }
    public bool Completed { get; init; }
    public bool Running => Deadline is not null;
    public TimeSpan At(DateTimeOffset now) => TimeSpan.FromTicks(Math.Clamp(
        Deadline is { } deadline ? (deadline - now).Ticks : Remaining.Ticks, 0, Duration.Ticks));
    public static ClockCountdown Ready(TimeSpan duration) => new() { Duration = duration, Remaining = duration };
}

public sealed record PomodoroState
{
    public PomodoroPhase Phase { get; init; }
    public int CompletedFocusRounds { get; init; }
    public ClockCountdown Countdown { get; init; } = ClockCountdown.Ready(TimeSpan.FromMinutes(25));
}
public sealed record SystemClock(string Name, string TimeZoneId);
public sealed record ClockNotice(Guid Id, ClockNoticeKind Kind, string Title, string Message, DateTimeOffset Created);
public sealed record ClockState
{
    public ClockCountdown Timer { get; init; } = new();
    public PomodoroState Pomodoro { get; init; } = new();
    public ClockNotice[] Notices { get; init; } = [];
}
public sealed record ClockSnapshot(DateTimeOffset LocalNow, ClockCountdown Timer, TimeSpan TimerRemaining,
    PomodoroState Pomodoro, TimeSpan PomodoroRemaining,
    IReadOnlyList<ClockNotice> Notices, string? StorageError);

public static class ClockFormatting
{
    public static string Countdown(TimeSpan value)
    {
        var seconds = (long)Math.Ceiling(Math.Max(0, value.TotalSeconds));
        return seconds >= 3600 ? $"{seconds / 3600:00}:{seconds / 60 % 60:00}:{seconds % 60:00}"
            : $"{seconds / 60:00}:{seconds % 60:00}";
    }
    public static string ModeName(ClockMode mode) => mode switch
    { ClockMode.Timer => "Timer", ClockMode.Pomodoro => "Pomodoro", _ => "Clock" };
    public static string PhaseName(PomodoroPhase phase) => phase switch
    { PomodoroPhase.ShortBreak => "Short break", PomodoroPhase.LongBreak => "Long break", _ => "Focus" };
}
