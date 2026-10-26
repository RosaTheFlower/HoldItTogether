namespace HoldItTogether.Services.Audio;

public enum MediaAction { TogglePlayback, Previous, Next, Seek, ToggleLoop, ToggleShuffle, ActivateSource }
public enum MediaLoop { Off, Track, List }

public sealed record MediaSnapshot
{
    public static MediaSnapshot Empty { get; } = new();
    public long Generation { get; init; }
    public bool HasMedia { get; init; }
    public string Title { get; init; } = "Nada tocando por enquanto";
    public string Artist { get; init; } = "Abra uma música ou um vídeo em um player compatível.";
    public string Album { get; init; } = "";
    public string Source { get; init; } = "";
    public string? Artwork { get; init; }
    public bool Playing { get; init; }
    public bool CanToggle { get; init; }
    public bool CanPrevious { get; init; }
    public bool CanNext { get; init; }
    public bool CanSeek { get; init; }
    public bool CanShuffle { get; init; }
    public bool Shuffle { get; init; }
    public bool CanLoop { get; init; }
    public MediaLoop Loop { get; init; }
    public double Position { get; init; }
    public double Duration { get; init; }
    public double SeekMinimum { get; init; }
    public double SeekMaximum { get; init; }
}

public interface IHitMediaService
{
    Task<MediaSnapshot> ReadAsync(CancellationToken token);
    Task<bool> ExecuteAsync(MediaAction action, long generation, double position, CancellationToken token);
}
