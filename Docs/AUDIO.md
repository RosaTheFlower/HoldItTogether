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
Space toggles playback while Audio is the visible open menu, including its Player, Lyrics and configuration pages. It uses the current session's play/pause capability and ignores held-key repeats, modifiers, text composition, editable fields, focused interactive controls, open pickers/dialogs and resource sleep. It is local to the HIT window, not a system-wide media shortcut.
MediaSourceWindow matches window/process AppUserModelID or known desktop process identity, rather than arbitrary title substrings.
The song title only prioritizes windows already identified as that app. Activation restores a minimized window and requests foreground;
Windows can refuse it. HIT does not launch another app to simulate this action.

## Lyrics search and reading

LyricsService tries LRCLIB exact/search, then SyncLRC, then NetEase search/LRC. Matching LRCLIB plain text is retained as a fallback; Lyrics.ovh is queried when no result remains. Attribution uses the actual provider/result URL.
Search alternatives validate title, artist and duration to avoid another track/version. Cleaning may remove redundant artist prefixes
and common Official Video-style suffixes. Provider requests have six-second deadlines and 1 MiB response bounds.
SyncLRC has a local rate budget below the cited provider's 30/minute allowance.

Timestamped lyrics are preferred. If none are available, matching LRCLIB plain lyrics or Lyrics.ovh provide a scrollable reading view labeled as unsynchronized, without seek buttons, automatic verse following or the synced timeline. Plain text is never assigned fabricated timing.
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

### Lyrics presentation preferences

Audio settings -> Lyrics provides six native radio choices with theme-derived SVG previews. Selection applies immediately and persists as `audioLyricsLayout`: Down=0 (default), Up=1, Left=2, Right=3, Spotlight=4, WordRise=5. Invalid values normalize to Down. These choices preserve attribution and click-to-seek timestamps.

Down keeps previous lines above and upcoming lines below the current line. Up reverses their visual order. Left places previous lines to the left and upcoming lines to the right; Right reverses that order. Horizontal modes center the active line along X, keep left-aligned text and convert wheel input to local horizontal scrolling. Manual browsing delays automatic following by the existing 1.8-second interval. Spotlight displays only the active line, centered vertically and aligned left. Untimed lyrics always retain the plain reading list without fabricated timing or seek targets.

Word rise retains the default vertical context and raises words in sequence within the current line. The sequence occupies the first two thirds of the interval to the next lyric timestamp, or the track duration for the last line. Individual durations are weighted by word length; each word starts halfway through its predecessor's animation. Durations are normalized so the overlapping sequence still finishes at two thirds. A short 0.4em rise and symmetric ease-in/out soften the movement. This is an estimated presentation based on line timestamps, not provider-supplied word or vocal alignment. Missing/nonpositive intervals leave readable static text. Pause freezes progress; seeking, reopening and track changes restore progress from the media position rather than restarting at zero.

Only the current line allocates word spans/animation handles. Position corrections do not rebuild the lyric tree; there is no additional media polling or perpetual JavaScript animation loop. Remove animations/system reduced motion shows static words; resource sleep pauses the effects. Hidden views dispose animations, observers, timers and listeners. Timing accuracy still depends on the media application and lyrics provider.

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
Audio settings -> Visualizer opens a dedicated subpage with an enable switch, style selection and immediate height
adjustment from 10% to 200% of the existing drawing area. Height changes amplitude, not player/window
dimensions or capture gain. Defaults remain Bars and 100%, preserving existing installations.
`audioVisualizerStyle` stores the stable numeric values below; undefined numeric values normalize to Bars.
Retired value 3 (Filled spectrum) migrates to Smooth line. The removed value is not reused.
`audioVisualizerHeightPercent` defaults to 100 and clamps to 10-200. All styles use the same
32-band snapshot, canvas and interpolation; changing style/height does not start another capture.
Renderer preferences survive buffer release during scene/resource transitions. Line connects FFT
frequency bands; it is not a time-domain waveform. A true waveform requires a separate temporal-data
contract and is not included in this delivery; the common height parameter can also serve that renderer.
Reduced motion removes CSS smoothing but does not replace real spectrum values with fabricated animation.

| Style | Persisted value | Presentation |
| --- | --- | --- |
| Bars | 0 | Original bottom-aligned gradient bars |
| Line | 1 | Straight segments connecting frequency bands |
| Smooth line | 2 | Quadratic interpolation of the frequency-band contour |
| Mirrored spectrum | 4 | Two smooth contours reflected about the vertical midpoint |
| Radial bars | 5 | 32 outward spokes around a fixed center |
| Spectrum ring | 6 | Closed contour with band amplitude mapped to radius |
| Wave | 7 | Three overlapping continuous curves with a smooth audio-energy envelope |
| Wave · Soft | 8 | One broad, slower continuous curve |
| Wave · Ribbons | 9 | Three closed ribbons around the wave contour, not bottom-filled spectrum areas |
| Wave · Pulse | 10 | Two denser curves with faster phase response |

The radial styles use the smaller canvas dimension so they remain circular at non-square player
dimensions. Their height control scales the radial excursion. All modes are frequency-spectrum
presentations, including the wave-like smooth/mirrored shapes; none claims to show PCM over time.
Amplification above 100% keeps the existing native/window bounds. Vertical peaks may clip at the
canvas edges, while radial modes reserve room for their maximum excursion. Silence returns to
the small static baseline/inner ring, with no decorative oscillation or rotation. Rendering performs
no per-frame DOM mutation, retains no raw audio and uses the existing bounded interpolation loop.

All four Wave variants are aesthetic audio-energy envelopes rather than reconstructions of PCM.
They follow RMS energy of smoothed FFT bands, with interpolated individual bands modulating the
curves. Their paths extend 6% beyond each horizontal edge of the drawing area at every Size setting;
the native/menu bounds clip the ends. There are no side gaps or visible waveform endpoints.
Wave Size increases excursion and wavelength while a smooth vertical fit keeps the curves in view
at high zoom/height. Phase advances only during requested drawing frames, not through an independent
perpetual animation. Silence flattens lines/collapses ribbons and stops interpolation; reduced motion
fixes phase while retaining amplitude response. Theme accent colors remain authoritative.

**Size** is separate from Height: `audioVisualizerSizePercent` defaults to 100 and clamps to 100-500.
Older values below 100 normalize to 100. It enlarges the drawing without allocating a larger canvas
or changing window bounds. Radial, Ring and Mirrored scale about the center; bottom-aligned styles
scale about the bottom center. Wave variants retain their full-width overscan as described above.
For radial modes, Size enlarges both the base circle and its excursion, whereas Height changes
excursion only. Large sizes/heights can crop the drawing at the existing canvas edges.

**Position Y** persists in `audioVisualizerPositionYPercent`, default 0, range -100 to 100.
It shifts the drawing by a percentage of the current canvas height: negative moves up, positive
moves down. It is independent of zoom and follows the available area during compact/expand.

**Opacity** persists in `audioVisualizerOpacityPercent`, default 55, range 0-100. It applies directly
and uniformly in every style/compositing mode, including with an artwork palette. Zero hides the
visual drawing; it does not change audio volume or capture ownership. Updating opacity does not
replace the root's JS-owned artwork/palette styles.

**Flip X / Flip Y** are independent toggle buttons backed by `audioVisualizerFlipX` and
`audioVisualizerFlipY`, both false by default. X reflects frequency distribution horizontally;
Y anchors bottom-aligned bars/lines at the ceiling. Both apply to every style and survive buffer
release. Position Y stays in menu coordinates, so positive values still move down after inversion.

**Compositing mode** persists in `audioVisualizerBlend`: Normal=0, Invert=1, Difference=2,
Overlay=3, Screen=4, Multiply=5, Exclusion=6. Invalid enum values normalize to Normal. Every mode
keeps a pointer-transparent visualizer above the cover/background and below text, lyrics, controls
and configuration forms. Non-Normal modes blend only with the background below them; they cannot
invert or recolor foreground UI. Invert makes the source
white before Difference composition. Its result is alpha-weighted by the drawing's coverage.
These modes cannot invert pixels of other applications behind the transparent desktop window.
Blending against transparent regions may be less visible than against actual HIT artwork/content.
Size, Height, Position Y, Opacity, flips and composition apply immediately and persist independently. Settings -> Visualizer
remains a separate configuration subpage and returns one level to Audio settings.

During compact/expand, scene insets follow the shared animated focus progress instead of switching
at its endpoints. The same canvas stays mounted and repaints its current values synchronously inside
each ResizeObserver callback after changing the backing dimensions; no blank frame is deferred to the
next animation frame. Actual sleep/disposal still releases the backing buffers and capture lease.

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

M2 or the optional held-title gesture opens Audio settings. Its shared workspace exposes General,
Size, Lyrics, Artwork, Background effects, Color adjustments, Visualizer and Hide buttons directly
in the sidebar. Object M2 gestures remain shortcuts to those visible pages. There are no hidden
configuration-only routes or additional inline Back buttons. A subtopic returns to General, then
General returns to the player using the header chevron, M2, side Back or Escape.
Size shows uniform, width and height sliders together, with editable plain values. The chosen
25-200% X/Y scale applies to the player and lyrics. Configuration stays at 100% in the common
610 x 420 workspace so changing player scale does not shrink its controls.
Lyrics centering converts screen coordinates back to layout coordinates; resizing must not move the active verse outside its viewport.
The lyric-source row also shows the song name in muted text on the opposite side.
Show song title in lyrics and Show lyrics source independently hide those labels; both are enabled by default.
Their typography and baseline match, and hiding both releases the attribution row's reading space.
Show timeline in lyrics adds an interactive seek track flush with the sphere's bottom edge, without buttons.
Track thickness is configurable from 1-6 px (default 2); dragging commits against the captured media generation.
While pinned, the shared pointer-departure delay hides the common header and secondary artwork/lyrics/visualizer controls.
The default is five seconds; Settings -> Compact after configures 0-10 seconds. Hover prevents compaction.
Audio settings -> Hide buttons selects a visible topic with independent Player and Lyrics switches.
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
| `audioVisualizerStyle` | Bars; stable values listed under Visualizer; retired 3 migrates to Smooth |
| `audioVisualizerHeightPercent` | 100; 10-200 percent |
| `audioVisualizerSizePercent` | 100; 100-500 percent |
| `audioVisualizerPositionYPercent` | 0; -100 to 100 percent of current canvas height |
| `audioVisualizerOpacityPercent` | 55; 0-100 percent |
| `audioVisualizerFlipX`, `audioVisualizerFlipY` | false; independent orientation toggles |
| `audioVisualizerBlend` | Normal; stable values listed under Visualizer |
| `audioLyricsTimeline` | false |
| `audioLyricsTimelineThickness` | 2; 1-6 px |

All Audio configuration topics set IsSettingsOpen. Their right options pane scrolls locally, while
wheel input elsewhere can navigate host menus; an open combobox temporarily blocks menu-wheel
navigation everywhere. M2 and header Back return a single configuration level.
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


YouTube/browser metadata can separate artist and song from artist-first video titles, remove official-video decorations and resolve Topic/VEVO channel suffixes. Matching still validates title/artist; a video with a different duration can use plain text without borrowing album timestamps. This is metadata matching, not audio fingerprinting or video-caption extraction. Spotify playback uses the published Windows media metadata and the same independent lyrics providers; the public Spotify API does not offer a documented lyrics integration in this implementation. No browser cookies or Spotify account tokens are read.

Lyrics uses a musical-note host title icon and the shared host Back chevron; the duplicate Back to Audio button is removed. M2 and keyboard return remain available. Personal background images stay beneath the artwork layer; reading-cover toggles fade over that background for 650 ms, with immediate changes when animations are disabled.

Artwork -> Artwork over background controls cover opacity from 0 to 100% (32% compatibility default), applied immediately and persisted independently of personal-image opacity. In blend mode, 0% reveals the personal background; 100% makes the cover layer opaque. Existing cover visibility, effects and fade rules still apply.

Blend artwork with personal background is a separate persisted Audio -> Artwork switch, default OFF. OFF makes artwork exclusive while its cover is visible; the personal layer remains mounted at zero opacity and returns with the cover fade. ON displays both layers using their independent opacity settings. Hiding artwork or starting its disappearance restores the personal layer during the existing 650 ms fade. This does not change artwork color precedence. Visualizer Flip X/Flip Y labels remain on a single line with equal-width buttons.

The Lyrics reading title routes M2/held-M1 configuration shortcuts directly to the Lyrics topic. Held-M1 title shortcuts are unavailable once any configuration is open.

Audio configuration identifies the built-in feature as version 1.2.0. It is shipped and updated with HIT, not installed or updated as a separate extension.
