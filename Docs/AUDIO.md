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
Capture stops when neither a visible visualizer nor a visible/global OnBeat cover needs it, and checks output changes periodically. Startup failure releases native resources.

The spectrum is a background layer and does not change player geometry. It can coexist with artwork and Lyrics independently.
The 32 bands are drawn on one full-DPI canvas with bounded interpolation, replacing 32 animated DOM layers.
Updates do not change DOM styles or rebuild bars. The canvas/gradient buffers and listeners are released when disabled/disposed.
Its enabled state persists in `audioVisualizerEnabled`; capture resumes only while Audio is visible.
Reduced motion removes CSS smoothing but does not replace real spectrum values with fabricated animation.

## Artwork, palette and appearance

The artwork button enables a cover background, dominant palette and compact track metadata above the timeline without a side cover.
Right-click it to open the compact Artwork screen: OLED / white lyrics background, Artwork behind lyrics,
Use color throughout HIT and an independent Artwork behind other menus suboption.
Background effects opens a bounded, scrollable screen with hidden scrollbars and overflow hints.
Choose Gaussian or Radial blur (0-40 px equivalent, default 8), zoom 100-180%, distortion 0-30,
Lens 0-100%, chromatic shift 0-20 px, vignette 0-100%, grain 0-30%, slow drift and OnBeat.
Lens magnifies a feathered central region; Radial averages seven progressively scaled cover copies instead of applying Gaussian blur.
Color adjustments is a separate subview: saturation, brightness, contrast, temperature, tint, hue, sepia and grayscale.
Enable color adjustments is off by default. Until enabled, sliders are disabled and their filters are entirely absent;
turning the switch off retains the saved values. Color balance uses its own SVG filter and never invokes distortion/turbulence.
Blur and all color filters work without distortion. Reduced motion disables drift, OnBeat animation and radial shaking.

OnBeat uses rising low-frequency energy from the same real output-device FFT as the visualizer.
An adaptive threshold and refractory interval reject silence and steady tones; no synthetic beat timer runs.
A detected beat has an 85 ms attack and a decaying envelope settling within 760 ms.
Enabled effects blend from neutral to their configured values; between beats only the selected blur remains.
With Radial selected, that blur also shakes gently sideways on beats. Beat strength spans 0-200%.
Native capture is shared with the visualizer and can continue while a global cover is visible on another menu.
It stops when HIT closes or no enabled visible effect needs it. Unavailable audio output reports an error and can be retried by toggling OnBeat/the visualizer.
The neutral lyrics option uses true black in dark/OLED themes and white in Light, independently of the music palette.
It suppresses the cover, spectrum, gradients and readability veil behind Lyrics until turned off, without discarding the saved cover preference.
Blur and saturation remain independent of distortion, including when distortion is zero.
Turning the mode off restores the selected global theme. All bundled themes preserve enabled artwork/colors.

Lyrics use palette colors when artwork mode is on; cover behind lyrics is off by default and uses the same blur when enabled.
Global colors can continue on other menus; global cover is separately configurable. These choices do not resize the Lyrics window.
Changing effects reuses artwork already held by JS instead of retransmitting base64. Temporary color-extraction images are released.
Artwork fills the unscaled sphere backdrop, so changing the player/settings size cannot leave a smaller cover behind it.
When hidden without global palette, the player/service/JS release their cover references; the source thumbnail is read again on return.

No like/playlist-editing, video-player embedding or audio-output-selection button is included.
The visualizer follows the Windows default output; it does not reroute playback.

## Menu settings and saved keys

M2 on the current menu title opens Audio settings. Size is a compact uniform slider; expand for independent axes.
The player, lyrics and settings all use the chosen 25-200% X/Y scale.
Lyrics centering converts screen coordinates back to layout coordinates; resizing must not move the active verse outside its viewport.
The lyric-source row also shows the song name in muted text on the opposite side.
Show song title in lyrics and Show lyrics source independently hide those labels; both are enabled by default.
Their typography and baseline match, and hiding both releases the attribution row's reading space.
Show timeline in lyrics adds an interactive seek track flush with the sphere's bottom edge, without buttons.
Track thickness is configurable from 1-6 px (default 2); dragging commits against the captured media generation.
While pinned, the shared pointer-departure delay hides the common header and secondary artwork/lyrics/visualizer controls.
The default is five seconds; Settings -> Compact after configures 0-10 seconds. Hover prevents compaction.
Audio settings -> Hide buttons opens its own configuration screen, with independent Player and Lyrics switches.
Each switch controls secondary-button hiding and compaction on that page only. Both default to enabled and persist independently.
Every audio configuration screen, including Hide buttons, stays expanded regardless of either switch.
Playback/timeline/verse actions remain usable. The player contracts; lyrics reclaim reading space without resizing the window.
Real movement/interaction restores controls; media polling and verse changes do not count as user input.

| JSON key | Default / bounds |
| --- | --- |
| `audioScaleX`, `audioScaleY` | 100; 25-200 percent |
| `menuCompactDelayMs` | Shared setting, 5000; 0-10000 ms |
| `audioHidePlayerButtons`, `audioHideLyricsButtons` | true; independent page compaction switches |
| `audioShowSource` | true |
| `audioArtworkMode` | false |
| `audioArtworkBlur` | 8; 0-40 px |
| `audioArtworkGlobalAccent` | false |
| `audioArtworkGlobalCover` | true, effective with global colors/mode |
| `audioArtworkLyricsCover` | false |
| `audioLyricsNeutralBackground` | false |
| `audioLyricsShowTitle`, `audioLyricsShowSource` | true, independent switches |
| `audioArtworkZoom` | 100; 100-180 percent |
| `audioArtworkDistortion` | 0; 0-30 |
| `audioArtworkSaturation` | 80; 0-200 percent |
| `audioArtworkColorEnabled` | false; gates all color-adjustment filters, preserving stored values |
| `audioArtworkDrift` | false |
| `audioArtworkRadialBlur` | false; Gaussian is default |
| `audioArtworkLens` | 0; 0-100 percent |
| `audioArtworkChromatic` | 0; 0-20 px |
| `audioArtworkVignette` | 0; 0-100 percent |
| `audioArtworkGrain` | 0; 0-30 percent |
| `audioArtworkBrightness` | 100; 20-200 percent |
| `audioArtworkContrast` | 100; 25-200 percent |
| `audioArtworkTemperature`, `audioArtworkTint` | 0; -100 to 100 |
| `audioArtworkHue` | 0; -180 to 180 degrees |
| `audioArtworkSepia`, `audioArtworkGrayscale` | 0; 0-100 percent |
| `audioArtworkOnBeat` | false |
| `audioArtworkBeatStrength` | 100; 0-200 percent |
| `audioVisualizerEnabled` | false |
| `audioLyricsTimeline` | false |
| `audioLyricsTimelineThickness` | 2; 1-6 px |

Audio settings, Hide buttons, artwork settings, background effects and color adjustments set IsSettingsOpen so wheel navigation is blocked and M2 returns.
Show source controls the origin label in normal and compact artwork layouts.
Shared idle/modifier behavior belongs to shell JS, not duplicate listeners in Audio.
On a configuration screen, the cover icon beside Pin/Close can temporarily hide an available artwork background.
This is a screen-local reading override: it never writes preferences, resets on leaving that screen and is unavailable when no artwork background is active.

## Ownership and diagnostics

Media access is serialized; reads use cancellation/timeouts. Visible metadata polling is 500 ms;
hidden global palette metadata checks occur about every two seconds. OnBeat may share loopback capture while its global cover is visible.
Capture otherwise stops when hidden; lyrics lookup stops when Audio is hidden. Services own native/network state; Razor owns view selection/previews;
JS owns spectrum DOM, palette extraction, optical effects/envelopes and lyric scrolling. See [Architecture](ARCHITECTURE.md) and [Performance](PERFORMANCE.md).
Steady drift runs as a compositor animation with no per-frame JS/DOM writes. Static SVG/filter/layer values are cached;
optional radial/lens/chromatic/noise layers and SVG graphs are constructed only when configured, and removed when disabled.
Beat-dependent filter writes are capped near 30 Hz while beat transforms remain frame-paced.
OnBeat captures only while playback is running and at least one enabled effect needs a beat signal. Unchanged/silent FFT frames skip WebView IPC.
Playback-position changes do not rebuild settings or hidden global-cover screens. Disabled lyrics timelines do not send per-render JS updates.

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
