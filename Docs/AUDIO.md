# Audio: playback, lyrics and visualization

Entry: `Components/HitMenus/HitMenuAudio.razor`. Supporting views live in `Components/Audio`;
services in `Services/Audio`; public layout in `wwwroot/css/menus/HitMenuAudio.css`.
The built-in player uses internal services. External menus use the media subset in [API](API.md).

## Windows session and commands

WindowsMediaService uses GlobalSystemMediaTransportControlsSessionManager. It prefers the current playing session,
then another playing session, then the current/first available session. It displays title, artist, album/source and supported artwork.
This is session metadata, not song recognition from captured sound. Apps/games without a published media session cannot supply these controls.

Play/pause, previous/next, seek, repeat and shuffle use that session's capabilities.
Commands carry Generation so an old action cannot target a newly selected session/track.
The timeline extrapolates from the Windows position/update time. Dragging only previews; commit sends seek.
Unsupported controls are disabled. Rejection/native errors remain local and polling retries without global keyboard shortcuts.

Ctrl+click the title or Ctrl+Enter activates an existing matching media app window.
MediaSourceWindow matches window/process AppUserModelID or known desktop process identity, rather than arbitrary title substrings.
The song title only prioritizes windows already identified as that app. Activation restores a minimized window and requests foreground;
Windows can refuse it. HIT does not launch another app to simulate this action.

## Lyrics search and reading

LyricsService tries LRCLIB exact/search, then SyncLRC, then NetEase search/LRC.
Search alternatives validate title, artist and duration to avoid another track/version. Cleaning may remove redundant artist prefixes
and common Official Video-style suffixes. Provider requests have six-second deadlines and 1 MiB response bounds.
SyncLRC has a local rate budget below the cited provider's 30/minute allowance.

Only timestamped lyrics enable the button. Plain text is not assigned fabricated timing.
The parser supports multiple timestamps per row and a global offset, bounded to 3,000 rows/5,000 timestamps.
Timing depends on the provider and source app's timeline. A failed lookup can remain unavailable without breaking playback.

Cache: at most 40 results and estimated 4 MiB. Hits expire after one hour; negative results after two minutes.
Oversized lyrics are still returned in full but need not be retained in the cache. No lyrics are saved on disk.
Track changes/hidden views cancel obsolete lookups. Hidden reading DOM/observers are removed while the model is kept for resumption.
Position updates within the same verse do not rebuild the entire verse tree.

The dedicated Lyrics view keeps the player's current dimensions. Current/past/future verses animate and follow the real timestamp.
Click a verse to seek when supported. M2, Escape or Back returns to Audio without unpinning.
Ctrl+click the source opens the real result URL through the validated default-browser service.
LRCLIB uses a result API ID when known; SyncLRC uses its actual query URL; NetEase uses the found track page.

## Visualizer

AudioSpectrumService uses NAudio.Wasapi loopback on the default multimedia output.
It does not open the microphone, save recordings or isolate one application's audio.
Other apps in the output mix contribute; exclusive/protected/rerouted playback may not appear.

AudioSpectrumAnalyzer mixes channels, applies a Hann window and 2,048-sample FFT and produces 32 logarithmic bands,
approximately 40 Hz-16 kHz. PCM16/24/32 and float are supported. Silence clears levels even when no new buffers arrive.
One buffer is reused per visualizer session; the UI receives up to 20 updates/second through JS instead of rebuilding Razor per frame.
Capture stops on close/menu change/disable and checks output changes periodically. Startup failure releases native resources.

The spectrum is a background layer and does not change player geometry. It can coexist with artwork and Lyrics independently.
Reduced motion removes CSS smoothing but does not replace real spectrum values with fabricated animation.

## Artwork, palette and appearance

The artwork button enables a cover background, dominant palette and compact track metadata above the timeline without a side cover.
Right-click it to open Music theme settings: blur 0-40 px (default 8), Artwork behind lyrics,
Use colors throughout HIT and an independent Artwork behind other menus suboption.
Turning the mode off restores the selected global theme. All bundled themes preserve enabled artwork/colors.

Lyrics use palette colors when artwork mode is on; cover behind lyrics is off by default and uses the same blur when enabled.
Global colors can continue on other menus; global cover is separately configurable. These choices do not resize the Lyrics window.
Changing blur reuses artwork already held by JS instead of retransmitting base64. Temporary color-extraction images are released.
When hidden without global palette, the player/service/JS release their cover references; the source thumbnail is read again on return.

No like/playlist-editing, video-player embedding or audio-output-selection button is included.
The visualizer follows the Windows default output; it does not reroute playback.

## Menu settings and saved keys

M2 on the current menu title opens Audio settings. Size is a compact uniform slider; expand for independent axes.
Settings remain at 100% scale; the player and lyrics receive the selected scale on return.
Hide buttons is available while pinned: five seconds of real inactivity hides the common header and secondary artwork/lyrics/visualizer controls.
Playback/timeline/verse actions remain usable. The player contracts; lyrics reclaim reading space without resizing the window.
Real movement/interaction restores controls; media polling and verse changes do not count as user input.

| JSON key | Default / bounds |
| --- | --- |
| `audioScaleX`, `audioScaleY` | 100; 75-200 percent |
| `audioHideControls` | false |
| `audioShowSource` | true |
| `audioArtworkMode` | false |
| `audioArtworkBlur` | 8; 0-40 px |
| `audioArtworkGlobalAccent` | false |
| `audioArtworkGlobalCover` | true, effective with global colors/mode |
| `audioArtworkLyricsCover` | false |

Audio settings and artwork settings both set IsSettingsOpen so wheel navigation is blocked and M2 returns.
Show source controls the origin label in normal and compact artwork layouts.
Shared idle/modifier behavior belongs to shell JS, not duplicate listeners in Audio.

## Ownership and diagnostics

Media access is serialized; reads use cancellation/timeouts. Visible metadata polling is 500 ms;
hidden global palette metadata checks occur about every two seconds without loopback capture.
Capture and lyrics lookup otherwise stop when hidden. Services own native/network state; Razor owns view selection/previews;
JS owns spectrum DOM, palette extraction and lyric scrolling. See [Architecture](ARCHITECTURE.md) and [Performance](PERFORMANCE.md).

Styling selectors and runtime variables: [Audio themes](AUDIO-THEMES.md).
User/provider/platform troubleshooting: [Troubleshooting](TROUBLESHOOTING.md).

## Provider and native references

- [LRCLIB documentation](https://lrclib.net/docs)
- [SyncLRC](https://synclrc.dev/)
- [NetEase Music](https://music.163.com/)
- [Window property store](https://learn.microsoft.com/en-us/windows/win32/api/shellapi/nf-shellapi-shgetpropertystoreforwindow)
- [Process AppUserModelID](https://learn.microsoft.com/en-us/windows/win32/api/appmodel/nf-appmodel-getapplicationusermodelid)
- [Restore/show window](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-showwindowasync)
- [Foreground request](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setforegroundwindow)
