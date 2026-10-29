# Settings and data management

Settings is always enabled and the last navigation menu. Its current CLR ID is
`HoldItTogether.Components.HitMenus.HitMenuSettings`; historical Settings/Clock IDs migrate when the extension registry loads.
Source: `Components/HitMenus/HitMenuSettings.razor`. Controls follow [Design](DESIGN.md).

## Screens

Audio settings -> Lyrics includes a preview-based radio picker for Down, Up, Left, Right, Spotlight and Word rise. It applies and saves immediately; existing profiles default to Down. See [Audio presentation preferences](AUDIO.md#lyrics-presentation-preferences) for ordering, timing and untimed-lyrics behavior.

The compact workspace has a persistent left sidebar: General, Appearance, Themes, Personalization,
Extensions, Interaction, Scroll, Accessibility, Window placement and bottom-pinned Info. A bounded
right pane scrolls the selected topic. Themes and Personalization are separate topics. Themes has a
full-width quick-selection combobox followed by installed-theme management and CSS import.
Info opens Changelog and Performance as separate detail views; returning closes just that view. Changelog,
Check for updates and GitHub share one row, followed by Delete my data.
The right pane blocks wheel-based menu navigation; scrolling elsewhere can switch host menus unless
a combobox is open. An open combobox blocks wheel navigation across the interface until dismissed.
The workspace stays in configuration mode for artwork and idle policies. Its topics omit the
redundant Back to Settings control; the sidebar, shared header Back, Escape and right-click provide
navigation. The header chevron is hidden on General and returns one level on topic/detail pages.
All topics publish their own header title; Info, Extensions and Window placement also have visible
content headings. Install .hitmenu aligns to the right of the Extensions heading.
The header cover icon temporarily hides music artwork for the current configuration screen, without changing Audio preferences.
It resets when leaving that screen and is disabled when no artwork background is active.

Start with Windows is off by default and saves `startWithWindows`. It registers only HIT for the current user in
`HKCU\Software\Microsoft\Windows\CurrentVersion\Run`, quoting the executable path. Disabling it removes that entry.
It launches at sign-in; Windows may delay startup or independently disable an entry in Startup Apps.
The current executable location is refreshed on launch, so keep the published directory in its intended permanent location before enabling it.
Registration failures are shown in Settings and the preference is rolled back. Resetting settings also disables startup on the next launch.
See Microsoft's [Run key reference](https://learn.microsoft.com/en-us/windows/win32/setupapi/run-and-runonce-registry-keys).

When System Tray is enabled, its context menu offers Show HIT, Hide HIT, Restart HIT and Exit.
Restart HIT closes normally, saves window position and stops module/media workers before launching the same distribution again.

Always on Top has a permanently visible Force Always on Top dependent row, disabled when ordinary
topmost is off or the platform does not support it. This experimental switch has a warning icon
and the tooltip "Experimental. Use at your own risk!". While enabled and visible, a native 500 ms timer
reasserts topmost positioning without activating HIT. Disabling Always on Top also disables Force.
It attempts to stay above fullscreen windows; exclusive fullscreen and protected system surfaces may still cover HIT.
The implementation uses [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)
and [window timers](https://learn.microsoft.com/en-us/windows/win32/winmsg/using-timers).

Compact after replaces the former compact-button preference. It saves `menuCompactDelayMs`, default 5000,
from 0 (Immediately after leaving) through 10000 ms, in 500 ms steps. Header actions are cover, pin
and close; Back appears beside the title whenever the current page has a parent.
Only Audio's Player/Lyrics pages, Clock's main readout and the optional, separately installed [Chess extension](CHESS.md)'s board can compact. All configuration screens,
General Settings and other modules stay expanded permanently. Hover anywhere over HIT prevents compaction, even without movement.
Clock and Chess use miniature layouts; Audio hides secondary controls. Audio settings -> Hide buttons independently
enables this behavior for Player and Lyrics. Open confirmations and active pointer gestures suspend compaction.
The label uses the same primary typography as other settings. Old `showCompactButton` and `audioHideControls` keys are ignored;
the independent audio preferences are `audioHidePlayerButtons` and `audioHideLyricsButtons`, both true by default.

Scroll speed contains Cooldown on/off, Sensitivity (0.25–3, default 1) and Pause (0–2000 ms, default 350).
Disabling cooldown disables Pause; the saved duration remains available when enabled again.

Window placement contains three independent controls: Magnetize (on by default), Allow other monitors (on),
and Edge padding (98%, configurable from 100% down to 85%). Both axes use the same pixel inset, calculated from the shorter work-area dimension. 100% is flush with the usable work-area edge;
lower percentages shrink the centered allowed area, leaving equal insets on opposite edges. Taskbar space is excluded.
The editable Edge padding value retains a visible `%` suffix. Its range declares `data-setting-unit`
for the settings adapter, which keeps the number and unit in one aligned field.
Magnetize attracts the visible surface within 12 logical pixels of the padded monitor/taskbar edge; disabling it removes attraction.
Snap to center (`windowMagnetizeCenter`, default false) independently snaps the visible surface's X
and Y centers within 30 logical pixels (2.5 times the edge threshold), only while Magnetize is enabled.
Moving beyond that range releases the axis. Dropping on a center axis also fixes the resize pivot
to that axis, so closing the panel leaves the sphere centered there. Its disabled row remains visible
when Magnetize is off.

Appearance and Personalization both start with `showConfigurationCoverButton` (default true),
which removes the temporary header artwork button without changing the saved artwork policy.
They also expose `hideArtworkInSettings` (default false), applied to configuration cover backgrounds,
and `titleDisplayMode` (0=name, 1=name/icon default, 2=icon). Its example choices use equal Clock-style
segments, with a selected highlight and visually hidden native radios that retain keyboard access.
Accessibility exposes
`showSettingDescriptions` (default true). These preferences apply immediately and persist independently.
All core options have explanations; nested form controls receive explanatory help from the scoped
settings control adapter when they do not already supply help. Action-button explanations use
tooltips instead of visible secondary text; file-upload controls are not augmented with descriptions.
Sliders retain plain editable numeric values without input boxes or spinner arrows, and service-side
normalization. Custom listboxes use a rounded continuous wheel of up to three visible rows, without
empty end padding; additional choices are reached by wheel or keyboard navigation.
The active item is always centered; wheel and arrow keys move to and immediately apply the next
enabled option. The wheel wraps at either end and accumulates small touchpad movements. Enter or
click confirms and closes; Escape restores the selection from before opening. Clicking outside
keeps the current selection and closes. Hidden/removed menus dismiss their picker and release its
wheel lock. No browser-native select popup or idle refresh loop is used, and listeners/transient
DOM are released on teardown.
Opening uses a short fade and slight movement/scale over 180 ms; closing fades out over 140 ms.
A closing popup is inert and removed at animation completion. Rapid reopening, view changes and
disposal cancel pending cleanup and release the transient DOM immediately. Remove animations and
the system reduced-motion preference skip both effects and remove closed popups immediately.
Allow other monitors off restricts HIT to the primary work area. When on, dragging can cross the combined monitor areas.
Padding/monitor changes apply immediately. Geometry uses the visible sphere/menu, never the larger transparent canvas.

The last visible anchor is saved on drop and shutdown in `window.json` beside `settings.json`. Startup restores that anchor
and its corner/center growth direction, normalizes it to the available monitor, and clamps it inside the configured padding.
Unplugged monitors or corrupt/missing position files fall back safely to an available/default position. Data reset/delete clears this file.

Themes shows a bounded list with hidden scrollbars and directional overflow hints. Single click selects;
Ctrl+click toggles multiple selections; double-click or Enter activates a theme.
Active and selected states are distinct. Delete selected acts on personal themes only and requires confirmation.
Deleting the active personal theme first persists the Dark fallback. Bundled themes cannot be deleted.
Open folder and Delete selected precede the final Import CSS action.
Importing selects the new personal theme in the list; the user activates it explicitly.
Dropping CSS anywhere on HIT imports it and opens this same screen.

Extensions installs, enables/disables and reorders menus. Built-ins include a disabled trash icon;
Settings also has a disabled drag grip and enable switch. Drag a grip at least six pixels to reorder,
or use the Up/Down buttons. See [Extensions](EXTENSIONS.md) for activation and restart behavior.
Clear old versions is the final action. It removes obsolete installed code directories while preserving
the selected versions of active packages, all currently loaded versions, package state and logs. Code from removed packages is eligible for cleanup immediately unless loaded. Unavailable rollback pointers are cleared.
Locked or linked directories are skipped and reported; running code is never removed by this action.

## Mouse navigation

Mouse side-button navigation and Hold title to configure are ON by default. The title hold duration
defaults to 700 ms (300-1500 ms). Releasing early, moving more than six pixels, leaving the surface,
scrolling, losing focus or cancelling the pointer cancels the hold. The current title receives a
rounded accent pill that fills over the configured hold duration. A clipped duplicate of the title
uses the background color only over the filled portion, including its icon. Cancelling or completing
the hold removes both decorative layers. Reduced motion keeps a static held-state indication.

Mouse Back returns one configuration level, or returns Lyrics to Player, before moving to the previous
top-level menu. Mouse Forward moves to the next top-level menu and does nothing inside configuration.
Neither direction wraps. M2 inside configuration returns only one level. Opening and closing use
separate physical gestures. Dialogs keep their own cancellation behavior. The side-button preference
affects HIT only; global mouse bindings are not registered. Existing title M2/keyboard access remains.

## Personalization and visual profiles

Settings -> Personalization opens the editor in its own sidebar topic, separate from Themes.
It also includes the shared configuration-artwork and menu-title preferences from Appearance.
The right options pane owns scrolling; the embedded editor does not trap wheel input in an
unbounded nested scroller. All palette, shape, image and visual-profile controls remain reachable.
Back/M2/mouse Back returns from Personalization to General. Changes apply immediately and save automatically
after a 250 ms pause. Leaving the editor flushes any pending save; there is no Apply or Cancel button.
Use custom appearance enables
overrides for background, accent, text, border color, open-surface corner radius (0-48 px) and border
width (0-4 px). Existing profiles without this preference keep their current theme unchanged.
Colors accept six-digit hex values. The selected base theme remains underneath the overrides.

Editing a visual option enables custom appearance automatically. The master switch can disable it
without discarding the saved values. Undo this session restores the appearance and theme that were
active when the editor opened; Restore theme defaults disables the overrides. Both save automatically.
Allow artwork colors is ON by
default: active music coloring remains authoritative. Turn it OFF to prioritize the personal palette
while retaining the actual cover/effects. Shape overrides remain independent of music colors.

Save new profile stores the current choices under a 1-64-character name. Up to 50 profiles
are stored atomically in Appearance/profiles.json. Use profile immediately activates and saves its
appearance and base theme. Duplicate profile creates an independent named copy. A missing base
theme falls back to HIT Dark Theme. Invalid profile files show an error and are not overwritten.

Export CSS saves the draft plus its base-theme CSS under Appearance/Exports and shows the local
path. It can be imported through Themes like other CSS (1 MiB limit). Export contains appearance,
not behavioral settings. Canonical bundled CSS is never modified. Reset settings preserves these
visual profiles/exports but resets the active overrides; Delete everything removes Appearance too.

Additional options include collapsed-sphere background, muted text and eye colors, eye size/gap,
pointer following, control radius, border style, font (system/Arial/monospace) and shadow strength.
They are included in saved profiles and exported CSS, with normalized ranges and allowlisted values.

Background images are copied to Appearance/Images with generated IDs. PNG/JPEG/WebP/GIF are supported
with a configurable size limit (2 MB by default) and 4096 x 4096 pixels; browser decoding validates the image before activation. Choose
Fill, Fit or Stretch and set independent image opacity. Remove all images clears the active selection after confirmation;
the original copy is kept for other saved profiles. The artwork-priority option also lets a visible
music cover take precedence over the personal image. Only validated image filenames are served
through appearance-images; profile JSON and arbitrary paths are not exposed. CSS exports embed
the image as a data URL for portability; images that push the combined CSS over the existing
1 MiB theme limit must be reduced before export. Settings retain filenames, not image data.

## Idle transparency

The Idle transparency disclosure in Settings contains an Automatic transparency switch (OFF by
default), independent Collapsed sphere and Compact menu switches (ON), and Inactive open panel
(OFF). The master switch must be enabled before any state fades. Audio's hidden-controls compact
mode counts as compact; an ordinary open menu uses the open-panel preference.

Fade after defaults to 5 seconds (0-60 seconds), Idle opacity to 40% (10-90%) and Fade duration to
650 ms (0-3000 ms). These preferences persist independently of resource saving. The delay starts
after pointer departure or entering an eligible state. Hover, pointer/button/keyboard interaction,
focused editing and active keyboard focus restore normal opacity immediately; they interrupt the
fade rather than waiting for its completion. Dialogs, promotion choices, visible notices and
configuration pages are protected. Pointer gestures and dragging suspend the fade.

Remove animations and system reduced motion skip the transition, preserving the configured delay
and target opacity. Disabling the feature or disposing the view restores opacity and cancels the
timer. Fading uses one idle timer and CSS opacity; it does not wake the resource policy or alter
native hit testing, geometry or click-through behavior. Click-through remains a separate future
option. Text scaling/contrast improvements from the broader accessibility roadmap are not part
of this opacity delivery.

## Automatic resource saving

Automatic resource saving is ON by default, including profiles created before this setting existed.
The persisted key is `automaticResourceSaving`. Turn it OFF to disable automatic resource sleep.

After five seconds without pointer interaction, focus or dragging, the collapsed sphere rests.
A compact menu is still an open menu and does not rest merely from inactivity. A fullscreen window
can also trigger rest when FORCE is OFF and it completely covers HIT's visible surface: monitor
coverage plus native Z-order are checked, so a borderless window behind HIT is not sufficient.
Windows-reported exclusive fullscreen is treated as covering that monitor. Detection remains
best-effort; a fullscreen app on another monitor does not cover HIT.

Rest cancels media presentation polling, spectrum capture/FFT and lyrics requests, releases
retained artwork and lyrics cache/model references, disconnects spectrum/effects and pauses CSS
animations. Theme file polling skips reads until wake. Hover, opening/focusing HIT, disabling the
option or removal of coverage from an open menu restores visual work; a still-collapsed sphere
may remain resting until interaction. Retained menu instances preserve navigation and user state.

Timers, alarms, sound and necessary background module workers continue. Extensions receive hidden
visibility while the host rests; independent extension workers must cooperate with that lifecycle.
A small native pointer/coverage check and background deadlines remain active. This is cooperative
resource sleep, not suspension of the application or WebView2 process. RAM reduction depends on
released allocations and runtime reclamation; no fixed savings or forced memory trimming is promised.

## Fullscreen placement

Detected fullscreen uses the complete monitor area on that monitor, including the taskbar strip
and without edge padding. Other monitors keep their usual work-area restrictions. This behavior
is automatic and does not change the saved edge-padding or allowed-monitor preferences.

Fullscreen anchors are stored separately in `window-fullscreen.json` beside `window.json`, with
at most 16 monitor-origin entries. Re-entering fullscreen restores that monitor's saved anchor;
resolution changes scale it proportionally. On leaving fullscreen, a legal current position stays
in place; an invalid position moves to the nearest allowed point. Transitions take 420 ms and
become immediate with Remove animations enabled. Dragging interrupts the transition.

HIT avoids mouse activation while fullscreen is detected, so normal mouse controls can operate
without automatically taking foreground focus. Keyboard entry may require explicitly leaving the
game. FORCE pauses repeated ordering during fullscreen. Windows-reported exclusive fullscreen
also suspends ordinary recovery; an exclusive-game overlay is not guaranteed. See
[troubleshooting](TROUBLESHOOTING.md) for presentation-mode limits.

## Info

Read-only rows show HIT version, installed extension count (excluding pending removals)
and installed theme count (including the three bundled themes).
The application version uses `NvDDMM`, such as `2v2910` for generation 2 on 29 October.
Info -> Performance contains RAM, CPU and Export diagnostics. These controls are not mounted on
Info's main screen. The shared header chevron closes Performance and restores Info; no inline Info
button repeats that navigation.
RAM usage reports the host; WebView2 RAM reports its descendant browser processes; Total RAM sums
both. CPU is normalized to total logical CPU capacity and appears after two samples. Sampling runs
only while Performance is open and visible. Leaving it cancels and awaits its display monitor.
Private resident memory is preferred consistently across the group;
if unavailable, the entire group uses working sets (shared pages may repeat). Partial samples are
marked. The first sample after more than five seconds without sampling resets the CPU baseline.
Export diagnostics saves a local JSON report under `Diagnostics/`, including measurements, version,
sleep/capture state and the latest 128 structured lifecycle/error events. Consecutive identical events
coalesce with a count. No media titles, URLs, exception messages or extension content are collected.
Export performs no upload. Reset/delete data also removes exported reports from this directory.
Automatic reports are also saved about once per minute under `Diagnostics/Automatic/`, even while
the interface rests. The first is written after one minute. A separate sampler keeps Info from
disturbing the automatic CPU baseline; CPU is averaged over the recording interval, not instantaneous.
Thirty fixed JSON slots retain recent reports and replace the oldest slot when full. Writes use a
temporary file and atomic replacement; failures are logged and retried on the next interval. There
are no notifications, uploads, renderer activation or automatic memory trimming. Closing HIT stops
the worker before audio/resource services are disposed. Manual exports remain separate.
Check for updates queries the repository's latest published GitHub release with an eight-second timeout.
It compares generation first and calendar date second, using the publication year and offers Open release when a newer release exists; it never installs an update silently.
Its button shows the shared loading spinner while checking. Success/error feedback opens above the
action row without changing its position; the feedback can be dismissed. The GitHub button shares
the row with Changelog and Check for updates, showing its logo and “Hold It Together”.

## Resetting and deleting data

Delete my data opens a choice screen. Neither choice changes files until its second confirmation:

| Choice | Removed/reset | Preserved |
| --- | --- | --- |
| Reset all settings | Global/audio/clock preferences, saved window position, clock state, extension State/logs, navigation order and disabled flags | Installed extension versions, personal theme CSS, canonical bundled assets |
| Delete everything | Preferences, saved window position, clock state, personal Themes and the complete user Modules directory, including installed extensions and Inbox | Application distribution and its bundled menus/themes |

Cancel, Escape or right-click cancels a confirmation. Dialog backdrops are transparent and Cancel receives initial focus.
After confirmation, HIT writes a small `reset-request.json` marker in its user-data root and closes normally.
The restart is launched after the clock/module/Inbox workers stop. The next startup applies the request before creating services,
so shutdown writes cannot recreate removed state. No app executable or canonical theme file is deleted.
Reset requests use the same application user-data root documented in [Getting started](GETTING-STARTED.md).
If a restart cannot be requested, the UI cancels the marker and reports that data was kept.

The automated tests use disposable profiles below `obj`; they never reset the user's real profile.

On the first valid native surface placement, HIT requests foreground activation once on an ordinary desktop. Windows can refuse the foreground request. Existing fullscreen contexts retain non-activating behavior, and regular resize/recovery never repeatedly steals focus. Extensions delegates wheel scrolling to the bounded options pane instead of trapping it in a nested list. M2 on the Visualizer button selects its visible Audio settings topic.

Settings workspaces constrain their grid row to the available height. Each options pane and topic sidebar can scroll independently, including Audio, Clock, Notes, Extensions, Info and Personalization. Nested theme lists use the outer options scroller. Background image uses a full-width file-picker surface and a Remove all images action opposite its section heading.

### Background modes

Personalization keeps the Background style wheel selector beside Remove all images. Single image replaces the current selection; Carousel and Smooth transition accept multiple local files and append them, up to eight images. Remove all images clears the entire selection after confirmation. Selected images are shown in a horizontally scrollable preview carousel. Each file retains the PNG/JPEG/WebP/GIF, configured file-size limit and decoded 4096 by 4096 limits; a failed batch preserves the previous selection.

Image opacity is an explicitly titled, editable percentage. Image interval is 5-3600 seconds (default 15). Smooth transition offers Fade, Slide and Zoom, with duration 100-4000 ms (default 1500). The whole set and mode/timing preferences persist in settings and visual profiles. CSS export embeds the selected images and slideshow rules, subject to the existing total 1 MB theme limit.

The live slideshow uses two presentation layers and advances only while HIT is open and resource-awake. The selected frame is retained during pause; resuming starts a fresh interval. Remove animations and reduced motion suppress transition motion. Personal backgrounds remain mounted underneath artwork while open; enabling or hiding the reading cover fades the artwork over the background in 650 ms. This does not change music-color precedence.

GIF87a/GIF89a and animated WebP backgrounds retain their native browser animation in single and multi-image modes. The original validated local file is served and embedded in exports; images are not flattened to a still frame. The same configured file-size limit and decoded 4096 by 4096 limits apply. Slideshow switching pauses in resource sleep; playback of the visible animated image is handled by WebView2.

Show background when collapsed defaults to ON for compatibility with existing personal images. OFF hides the image layer on the collapsed sphere; ON keeps the selected image and its native GIF/WebP animation visible. This preference does not resume carousel switching while collapsed. It is saved in settings/profiles and included in exported CSS.

In the Images sidebar topic, Image size limit accepts any finite MB value from 0.25 MB (250 KB) upward, or Unlimited image size. Units are decimal (1 MB = 1,000,000 bytes). The default is 2 MB; null in BackgroundImageLimitMb denotes unlimited. The limit applies to each new image import, not existing references. File contents stream through an 80 KB buffer into a temporary file with atomic rename, including in unlimited mode. The decoded dimension limit remains 4096 by 4096 and portable CSS retains its total 1 MB limit.

### Per-menu page history and wheel boundaries

Interaction exposes independent Mouse Back button and Mouse Forward button choices: Page history, HIT menus, and Parent page, then HIT menus (the compatibility default for existing preferences). The existing navigation enable switch gates both. History retains at most 50 stable page IDs per mounted menu during the current session, including configuration topics, Lyrics and nested Info pages. Returning restores the visited page; a new visit clears the forward branch. Hiding a menu retains its view/history; disabling/removing it clears them. History is not written to disk.

System Settings sidebar topics are peers without a header Back chevron. Separate Info details (Performance, Changelog, Manage your data) retain a single-level parent action. Within configuration, a region with actual overflow owns the wheel at both ends as well as during scrolling. Only clean non-scrollable areas can pass wheel input to HIT navigation. Inputs, open pickers and dialogs retain their own wheel behavior.

### Preview carousel and image removal

Personalization previews every selected image in a horizontal carousel with Previous/Next buttons, wheel scrolling and keyboard arrows. Images use lazy decoding and the original animated files. Select a thumbnail to reveal its trash button. The first trash activation arms removal; a second activation removes that image from the active set. Selecting another image, moving focus away, Escape or changing the set cancels the armed state. Remove all images opens the shared confirmation dialog with Cancel initially focused; only the explicit confirmation clears the set. These actions update the saved selection immediately through the normal debounce. Files are retained for session Undo and saved profiles; removal does not invalidate another profile. The preview does not auto-advance or create its own polling worker.

Accessibility includes Menu entrance animations (ON by default), independently controlling the short menu/topic reveal. Remove animations disables and locks this preference in the UI; system reduced motion suppresses the effect as well. Settings slider rows use small inline labels, tracks, editable values and explicit units rather than separate headings. Their help remains in tooltips.

### Images topic and GIF policy

Settings -> Images centralizes the maximum file size in MB and Unlimited image size, moved out of Personalization without changing the persisted Appearance.BackgroundImageLimitMb key. Changes apply immediately without enabling custom appearance. Allow GIF backgrounds defaults ON; OFF rejects GIF signatures on import and suppresses managed GIF image URLs, slideshow frames, gallery thumbnails and CSS export. References and files remain intact for re-enabling the option. WebP remains supported independently. GIF policy is global and cannot be overridden by a visual profile; embedded third-party theme CSS is outside this managed-asset policy.

### Infinite menu

General exposes Infinite menu (InfiniteMenu, default OFF). It makes top-level wheel and configured menu-mode side-button navigation wrap at both ends. A full cycle reaches the original menu without changing order or the protected final Settings entry. Page-history mode keeps its own bounded history and does not wrap. Wheel cooldown, sensitivity, widget/modal ownership and Ctrl-wheel guards still apply.

### Configuration gesture and Info refinements

Held M1 on a title is disabled while the host is in any configuration page; it creates no pill. The host bridge validates the same condition and an in-progress hold is canceled if configuration opens. From the Lyrics reading title, M2 or a permitted M1 hold opens Audio settings directly on Lyrics.

Info keeps its three shared-row actions and Delete my data at the bottom of its options area. Update status appears as plain muted text immediately above those actions, without popup framing or a dismiss button. A newer-release link remains available when applicable.

Changelog reads the latest published GitHub release notes whenever opened, displaying the release tag/name and body as escaped plain text with Markdown preserved as text. Drafts are not shown. A bounded 1 MB response and 8-second timeout apply; loading uses the shared spinner and failures offer Retry. Closing cancels/awaits the request. No background polling or arbitrary release HTML is executed.
