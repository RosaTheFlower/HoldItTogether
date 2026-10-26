using System.Runtime.InteropServices;
using Windows.Media;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace HoldItTogether.Services.Audio;

/// <summary>Controls the system's current/playing media session, without synthesizing global media keys.</summary>
public sealed class WindowsMediaService : IHitMediaService, IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _session;
    private string _title = "", _artist = "", _album = "", _trackKey = "";
    private string? _artwork;
    private int _metadataDirty = 1;
    private long _generation;
    private TimeSpan _start;
    private bool _disposed;

    public async Task<MediaSnapshot> ReadAsync(CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            _manager ??= await GlobalSystemMediaTransportControlsSessionManager.RequestAsync().AsTask(token);
            var sessions = _manager.GetSessions();
            var current = _manager.GetCurrentSession();
            var next = IsPlaying(current) ? current : sessions.FirstOrDefault(IsPlaying) ?? current ?? sessions.FirstOrDefault();
            if (!Equals(next, _session)) ChangeSession(next);
            if (_session is null) return MediaSnapshot.Empty with { Generation = _generation };

            if (Interlocked.Exchange(ref _metadataDirty, 0) != 0)
            {
                try
                {
                    var properties = await _session.TryGetMediaPropertiesAsync().AsTask(token);
                    var key = properties.Title + "\n" + properties.Artist + "\n" + properties.AlbumTitle;
                    if (key != _trackKey)
                    {
                        _generation++;
                        _trackKey = key;
                        _artwork = null;
                    }
                    _title = properties.Title;
                    _album = properties.AlbumTitle;
                    _artist = string.IsNullOrWhiteSpace(properties.Artist) ? properties.AlbumArtist : properties.Artist;
                    if (_artwork is null && properties.Thumbnail is not null)
                        _artwork = await ReadArtworkAsync(properties.Thumbnail, token);
                }
                catch
                {
                    Interlocked.Exchange(ref _metadataDirty, 1);
                    throw;
                }
            }

            var playback = _session.GetPlaybackInfo();
            var controls = playback.Controls;
            var timeline = _session.GetTimelineProperties();
            _start = timeline.StartTime;
            var playing = playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;
            var duration = Math.Max(0, (timeline.EndTime - _start).TotalSeconds);
            var position = (timeline.Position - _start).TotalSeconds;
            var rate = playback.PlaybackRate is { } reportedRate && double.IsFinite(reportedRate) && reportedRate > 0 ? reportedRate : 1;
            if (playing && timeline.LastUpdatedTime > DateTimeOffset.MinValue)
                position += Math.Max(0, (DateTimeOffset.UtcNow - timeline.LastUpdatedTime).TotalSeconds) * rate;
            var minimum = Math.Clamp((timeline.MinSeekTime - _start).TotalSeconds, 0, duration);
            var maximum = Math.Clamp((timeline.MaxSeekTime - _start).TotalSeconds, 0, duration);
            return new MediaSnapshot
            {
                Generation = _generation, HasMedia = true,
                Title = string.IsNullOrWhiteSpace(_title) ? "Áudio em reprodução" : _title,
                Artist = string.IsNullOrWhiteSpace(_artist) ? "Mídia do sistema" : _artist,
                Album = _album, CanShuffle = controls.IsShuffleEnabled, Shuffle = playback.IsShuffleActive ?? false,
                Source = SourceName(_session.SourceAppUserModelId), Artwork = _artwork,
                Playing = playing, Position = Math.Clamp(position, 0, duration), Duration = duration,
                SeekMinimum = minimum, SeekMaximum = maximum,
                CanToggle = controls.IsPlayPauseToggleEnabled || (playing ? controls.IsPauseEnabled : controls.IsPlayEnabled),
                CanPrevious = controls.IsPreviousEnabled, CanNext = controls.IsNextEnabled,
                CanSeek = controls.IsPlaybackPositionEnabled && maximum > minimum && duration > 0,
                CanLoop = controls.IsRepeatEnabled,
                Loop = playback.AutoRepeatMode switch
                {
                    MediaPlaybackAutoRepeatMode.Track => MediaLoop.Track,
                    MediaPlaybackAutoRepeatMode.List => MediaLoop.List,
                    _ => MediaLoop.Off
                }
            };
        }
        finally { _gate.Release(); }
    }

    public async Task<bool> ExecuteAsync(MediaAction action, long generation, double position, CancellationToken token)
    {
        await _gate.WaitAsync(token);
        try
        {
            if (_disposed || _session is null || generation != _generation) return false;
            if (action == MediaAction.ActivateSource)
                return MediaSourceWindow.TryActivate(_session.SourceAppUserModelId, _title);
            var playback = _session.GetPlaybackInfo();
            var controls = playback.Controls;
            switch (action)
            {
                case MediaAction.TogglePlayback:
                    if (controls.IsPlayPauseToggleEnabled) return await _session.TryTogglePlayPauseAsync().AsTask(token);
                    if (playback.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing)
                        return controls.IsPauseEnabled && await _session.TryPauseAsync().AsTask(token);
                    return controls.IsPlayEnabled && await _session.TryPlayAsync().AsTask(token);
                case MediaAction.Previous:
                    return controls.IsPreviousEnabled && await _session.TrySkipPreviousAsync().AsTask(token);
                case MediaAction.Next:
                    return controls.IsNextEnabled && await _session.TrySkipNextAsync().AsTask(token);
                case MediaAction.ToggleShuffle:
                    return controls.IsShuffleEnabled && await _session.TryChangeShuffleActiveAsync(!(playback.IsShuffleActive ?? false)).AsTask(token);
                case MediaAction.ToggleLoop:
                    if (!controls.IsRepeatEnabled) return false;
                    var mode = playback.AutoRepeatMode is null or MediaPlaybackAutoRepeatMode.None
                        ? MediaPlaybackAutoRepeatMode.Track : MediaPlaybackAutoRepeatMode.None;
                    return await _session.TryChangeAutoRepeatModeAsync(mode).AsTask(token);
                case MediaAction.Seek:
                    if (Volatile.Read(ref _metadataDirty) != 0 || !controls.IsPlaybackPositionEnabled || !double.IsFinite(position)) return false;
                    var timeline = _session.GetTimelineProperties();
                    var requested = _start + TimeSpan.FromSeconds(position);
                    var ticks = Math.Clamp(requested.Ticks, timeline.MinSeekTime.Ticks, Math.Max(timeline.MinSeekTime.Ticks, timeline.MaxSeekTime.Ticks));
                    return await _session.TryChangePlaybackPositionAsync(ticks).AsTask(token);
                default:
                    return false;
            }
        }
        finally { _gate.Release(); }
    }

    private static bool IsPlaying(GlobalSystemMediaTransportControlsSession? session)
    {
        try { return session?.GetPlaybackInfo()?.PlaybackStatus == GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing; }
        catch (COMException) { return false; }
    }

    public async Task ReleaseArtworkAsync()
    {
        await _gate.WaitAsync();
        try
        {
            _artwork = null;
            Interlocked.Exchange(ref _metadataDirty, 1);
        }
        finally { _gate.Release(); }
    }

    private void ChangeSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_session is not null) _session.MediaPropertiesChanged -= MetadataChanged;
        _session = session;
        _title = _artist = _album = _trackKey = "";
        _artwork = null;
        _generation++;
        Interlocked.Exchange(ref _metadataDirty, 1);
        if (_session is not null) _session.MediaPropertiesChanged += MetadataChanged;
    }

    private void MetadataChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args) =>
        Interlocked.Exchange(ref _metadataDirty, 1);

    private static async Task<string?> ReadArtworkAsync(IRandomAccessStreamReference thumbnail, CancellationToken token)
    {
        using var stream = await thumbnail.OpenReadAsync().AsTask(token);
        if (stream.Size == 0 || stream.Size > 1024 * 1024) return null;
        var mime = stream.ContentType.ToLowerInvariant();

        using var reader = new DataReader(stream);
        var length = (uint)stream.Size;
        if (await reader.LoadAsync(length).AsTask(token) != length) return null;
        var bytes = new byte[length];
        reader.ReadBytes(bytes);
        // Some providers label their cover as application/octet-stream.
        if (bytes.AsSpan().StartsWith(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 })) mime = "image/png";
        else if (bytes.Length >= 3 && bytes[0] == 255 && bytes[1] == 216 && bytes[2] == 255) mime = "image/jpeg";
        else if (bytes.Length >= 6 && System.Text.Encoding.ASCII.GetString(bytes, 0, 3) == "GIF") mime = "image/gif";
        else if (bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" &&
                 System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP") mime = "image/webp";
        if (mime is not ("image/png" or "image/jpeg" or "image/jpg" or "image/webp" or "image/gif")) return null;
        return $"data:{mime};base64,{Convert.ToBase64String(bytes)}";
    }

    private static string SourceName(string id)
    {
        var lower = id.ToLowerInvariant();
        if (lower.Contains("spotify")) return "Spotify";
        if (lower.Contains("chrome")) return "Google Chrome";
        if (lower.Contains("msedge")) return "Microsoft Edge";
        if (lower.Contains("firefox")) return "Firefox";
        if (lower.Contains("vlc")) return "VLC";
        if (lower.Contains("zunemusic") || lower.Contains("mediaplayer")) return "Media Player";
        return Path.GetFileNameWithoutExtension(id.Split('!')[0].Split('_')[0]);
    }

    public void Dispose()
    {
        _gate.Wait();
        try { if (_disposed) return; _disposed = true; ChangeSession(null); _manager = null; }
        finally { _gate.Release(); }
    }
}
