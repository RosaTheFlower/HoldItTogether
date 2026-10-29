# Build, test, package and release

Run commands from the repository root on Windows. A .NET SDK that targets .NET 8 is required.
Use a Visual Studio/SDK version that understands `.slnx`, or build the individual `.csproj` files below.
Node and Google Chrome are needed only for the included browser test, not for running HIT.

## Build and run

```powershell
dotnet restore HoldItTogether.csproj
dotnet build HoldItTogether.csproj -c Release
dotnet run --project HoldItTogether.csproj
```

The root project excludes `Modules/**`, `Tests/**` and `artifacts/**` from its own source/content globs and references HIT.Sdk explicitly.
All five independent module/SDK/test projects live below `Modules`; built-in menus remain under Components.
It embeds en.json, generates `wwwroot/css/menus.css`, copies wwwroot and includes `Docs/**/*.md` in build/publish.
Do not duplicate those projects' source inside the executable to solve a reference problem.

## SDK, sample and independent template

SDK event/action checks exercise new capability gates, shared media delivery, immutable token/state
snapshots, action discovery/execution, duplicate rejection, package disable/re-enable, resource sleep
and visible/module cancellation. The existing extension-load tests continue using contract 1. Real
output-device spectrum sharing can be checked separately (no recording or microphone):

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --sdk-spectrum-checks
```

It checks two owners/one capture, final release, 32 bands and legacy-owner isolation, or reports SKIP
when no default output is available. Real output-device disconnection/reconnection remains a manual check.

```powershell
dotnet pack Modules/HIT.Sdk/HIT.Sdk.csproj -c Release -o artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project Modules/HIT.Example/HIT.Example.csproj
```

Expected: `artifacts/sdk/HIT.Sdk.1.2.0.nupkg` and `artifacts/packages/dev.hit.example-1.0.2.hitmenu`.
The pack helper publishes the module, normalizes ZIP paths, excludes the host-shared SDK DLL,
copies public asset/dependency directories and checks its own temporary cleanup target.

To verify authoring without the main project's source reference:

```powershell
dotnet new install ./Modules/HIT.Example
dotnet new hitmenu -n MyNotes --extensionId dev.example.notes -o ../MyNotes
dotnet restore ../MyNotes/MyNotes.csproj --source ./artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project ../MyNotes/MyNotes.csproj
```

This installs the template into the developer's local template catalog. Package publication to an external feed is a separate release action.
The example can use a local project reference inside this repository; outside it, it uses the SDK NuGet package.
Do not reuse an already cached package version after changing its public bits: increment the package version deliberately.

## Automated extension checks

Build the example first, then:

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj
node Tests/browser/extensions.mjs
```

The C# harness uses disposable state below obj and emits `obj/extensions-preview.html` for the browser test.
It checks invalid archive paths/links/collisions, idempotent/atomic installation, contract/capability rejection,
independent component loading, storage, asset boundaries, persisted order, built-in disabling,
stable active selection, settings wheel blocking/M2 return, updates, rollback and retained user data.

The browser test checks Dark/OLED/Light layout, stylesheet scope/keyframe namespacing,
CSS/package file-drop streaming handoff, real pointer dragging from the grip, built-in removal protection, invalid names and listener teardown.
The native harness also checks live activation, settings subviews, selection/activation/multi-delete,
25-200% preference persistence, historical ID migration and deferred reset/delete using disposable profiles.

```powershell
node Tests/browser/menus.mjs
node Tests/browser/settings-design.mjs
node Tests/browser/shared-settings.mjs
node Tests/browser/navigation.mjs
node Tests/browser/audio-effects.mjs
node Tests/browser/idle-input.mjs
node Tests/browser/idle-opacity.mjs
node Tests/browser/appearance.mjs
node Tests/browser/mouse-navigation.mjs
node Tests/browser/resource-saving.mjs
node Tests/browser/artwork-fade.mjs
node Tests/browser/spectrum.mjs
node Tests/browser/spectrum-composition.mjs
node Tests/browser/spectrum-compaction.mjs
node Tests/browser/placement.mjs
dotnet run --project Modules/HIT.Chess.Tests/HIT.Chess.Tests.csproj
node Tests/browser/chess.mjs
node Tests/browser/app-icon.mjs
```

For the shared-menu UI work, the focused host harness stops after package/component/settings,
SDK-event and live component sleep checks, before unrelated native desktop checks:

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --ui-checks
```

The menu browser test consumes `obj/menu-tests` emitted by the example tests. It checks shared controls in all themes,
hidden overflow hints, safe transparent confirmations, lyrics centering at 25/100/200%, shell artwork/effects,
seekable bottom-edge timeline and resource cleanup. The Chess guide describes its full regression suite.
Audio regressions include exact black/white screenshot pixels, Light cover contrast, zero-distortion blur/saturation,
attribution alignment/visibility and the unsaved screen-local header cover toggle.
The effects test checks independent optical/color filters, radial blur, silence/steady-tone rejection,
attack/decay from synthetic FFT input, radial beat shaking, reduced motion and no static-filter churn during drift.
The idle/input test checks stationary-hover protection, departure timing/0-10-second settings, configuration-screen exclusion,
independent Player/Lyrics hide choices, confirmation protection, the allowed board/clock views, icon hover tooltips and blocked page zoom.
The spectrum test checks actual-band canvas drawing, a single DOM layer, zero per-frame DOM mutations, native DPI/content scaling and disposal.
It also checks Bars/Line drawing, immediate height changes without new FFT frames, equal amplitude
between styles and retained renderer options after buffer release. Host interaction checks exercise
the actual Visualizer enable/style/height controls, persisted values and invalid-style rejection.
Visualizer expansion checks cover six distinct spectrum presentations, height through 200%,
unchanged layout bounds, restored style/amplitude after buffer release for every mode, and reduced-motion
input/silence response. The host checks save/reload each style through the actual select at 200%.
Wave checks cover four distinct variants, energy response, reduced-motion silence and edge-to-edge
coverage at 100-500% Size. Drawing checks verify independent Y, reflected spectrum pixels, ceiling
bars, retained size/position/flips after buffer release and unchanged native bounds. Composition tests
verify actual screenshot pixels for all seven CSS formulas over dark/light/colored covers, unchanged
foreground UI pixels, independent opacity with artwork colors, white-source inversion and pointer
passthrough. Host controls save/reload size, position, opacity, flips and composition independently,
reject unknown composition values and migrate retired Filled settings to Smooth.
The compaction test uses `obj/menu-tests/player-spectrum.html` emitted by the host harness. It checks
animated scene insets, intermediate canvas sizes, nonempty buffers in the same ResizeObserver cycle,
retained canvas identity, interrupted compact/expand and final disposal. It does not prove native
WebView2 presentation behavior on every live desktop configuration.
The placement test uses actual Clock/Chess/Audio markup, samples all corners/center throughout compact/expand,
samples the first frame of sphere opening/closing, and checks compact-clock drag/release with a changed growth direction.
Native placement tests cover oversized transparent canvases, repeated transitions at 100-200% DPI, secondary/negative monitor
coordinates, visible grab offsets, edge padding/magnetism, saved-position restart/corruption and calendar release comparison.
Native regressions cover topmost preference visibility/normalization, optical preference persistence/bounds,
old-version cleanup preserving loaded/current code, two-action score reset preserving moves and host-requested Clock/Chess compaction.
For focused native-window checks without building a sample package:

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --window-checks
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Debug -- --window-checks
```

These checks deliberately demote a hidden native window and verify ordinary topmost recovery without
FORCE, bounded diagnostics, unchanged-preference reconciliation, OFF with queued recovery, healthy
resizing without recovery churn, FORCE shutdown, focus preservation and destruction with pending work.
The deliberate demotion tests recovery; it does not identify the original intermittent desktop trigger.
Fullscreen checks also exercise non-activating mouse policy, suspension of recovery for an injected
exclusive context, resumption on exit, taskbar placement on every edge, negative coordinates,
fullscreen position persistence, resolution normalization and bounded/corrupt state fallback.
Resource-saving checks cover five-second entry, interrupted grace, collapsed sphere versus open
compact menu, coverage removal, immediate wake, defaults/migration and the actual Settings switch.
Diagnostic checks cover descendant process isolation, consistent memory fallback, CPU identity/deltas,
bounded/coalesced reports and release of native sampling handles. Audio lifecycle checks include 12
rapid sleep/wake cycles and verify that disposed views do not restart polling. Follow
[scenario recording](PERFORMANCE.md#repeatable-scenario-recording) for the separate live baseline.
Automatic reporting tests use an accelerated interval to check scheduled writes, bounded 30-slot
rotation, complete JSON replacement, no wake from resource sleep, stopped writes after disposal and
nonfatal file-access failure handling. Production uses a one-minute interval.
Appearance checks cover real editor autosave, undo and pending-save flush on exit, preference/profile persistence, independent
copies, CSS-safe color/size normalization, missing theme fallback and CSS export/reimport. The
appearance browser test consumes generated CSS from the C# checks and verifies actual computed
palette/shape, music-color precedence, override persistence after theme reload and removal.
Background tests cover signature/size/path validation, image-reference persistence and portable
data-URL export; browser checks decode the exported image, reject oversized dimensions and verify
image opacity/fit, font, eye size and border style.
An interactive Audio component test verifies polling cancellation, artwork release, timer-state
preservation and polling resumption. The browser resource-saving test checks paused animations,
blocked pointer writes and released/recreated spectrum buffers. These do not constitute total-RAM
benchmarks or live-game occlusion validation.
Live game acceptance: test LoL windowed, borderless and fullscreen; confirm no automatic focus loss,
mouse playback/drag controls, full-monitor reach, leave/re-enter position restoration, rapid mode
switches, compact/expand during movement, Remove animations and mixed-DPI monitors. An exclusive
swap chain may hide HIT; do not count forced minimization of the game as overlay success.

Hidden native-window tests check outer input bounds at 100/125/150/200% DPI, preservation of CSS-antialiased corner pixels,
exclusion one pixel beyond the outer edges and region-handle ownership.
These do not substitute for a live fullscreen, real-device beat/sound or Explorer-drop check on the user's machine.
The navigation test samples every frame during rapid wheel input in all three themes, checking opaque content,
one retained native canvas and release on closing. Native tests also cover resize-induced pointer-exit recovery.
The navigation fixture checks corner alignment before the first resize/reveal and native zoom-reset requests at startup/opening.
Its Chrome path is Windows-specific. Synthetic browser drop does not replace checking a real Explorer-to-Photino drag.
Other audio/clock/theme behaviors have the acceptance matrix below; temporary historical obj harnesses are not part of a clean checkout.

## Documentation checks

Settings design checks use `node Tests/browser/settings-design.mjs` after the host harness emits
`obj/menu-tests/` fixtures. They cover the topic sidebar, bottom Info, centered wheel selection and
wrapping, live option changes, Escape restoration, open-picker wheel blocking and hidden-view
cleanup, three-row rounded popup layout, opening/closing animation cleanup, rapid reopening,
reduced motion/Remove animations, plain editable ranges, CSS-import click targets, Edge padding alignment and percentage
suffix, configuration headings, right-aligned extension installation, Clock-style
title segments, real wheel scrolling to Personalization's final profile controls, separate Performance,
upward update feedback without layout shifts, the shared loading spinner, no idle DOM churn and
listener teardown. The menu harness verifies shared appearance preferences, header-button persistence
and single-level Changelog/Performance return. Host checks verify title/Back publication, header
return from Performance through Info to General, and no Back action at the root. Update checking/feedback fixtures validate presentation
without relying on a live GitHub response.
`shared-settings.mjs` uses the emitted Audio/Clock/Notes workspaces and the Chess harness's settings
fixture. It checks repository-owned sidebars, active headings, fixed workspace bounds, automatic
extension controls, theme consistency, description propagation, feature sliders and cleanup. Build
and pack the current Example first; the host harness loads the actual `.hitmenu` rather than source.
Prepare Chess fixtures with its module harness before running shared settings/browser checks.
Center-axis magnetism and a sphere that stays centered while collapsing can be
checked independently of desktop topmost state with:

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --placement-checks
```

```powershell
node Tools/Check-Documentation.mjs
node Tools/Check-Documentation.mjs --published artifacts/publish
```

This validates centralized technical Markdown placement, the root `AGENTS.md` and repository
`SKILL.md` instruction exceptions, skill names/descriptions, local file/anchor links, JSON code
blocks and documentation publication configuration. Skill frontmatter uses the small checked
format described in [Agent workflows](AGENT-WORKFLOWS.md#keeping-instructions-accurate).
It does not access the network or claim that every external provider page is currently reachable.
After adding a public API/manifest field/selector, also inspect its reference and compile the affected example.

## User-facing acceptance matrix

| Area | Check |
| --- | --- |
| Sphere | Hover/click/pin/close, click outside, drag, edge/corner anchoring, hit testing with apps behind |
| Navigation | Dots, wheel both directions, no wrap, cooldown on/off, reordered state, disabled menus, Settings last |
| Settings | Preferences saved, tray show/hide/exit, no taskbar button, RAM definition, tooltip toggle |
| Audio | Supported/unsupported capabilities, pause/next/previous/seek/shuffle/repeat and stale-generation rejection |
| Lyrics | Provider miss/fallback, matched track, current-verse animation/seek, Ctrl source, M2 return and unchanged geometry |
| Artwork | Blur limits, local/global colors, other-menu and Lyrics cover toggles, theme restoration |
| Spectrum | Real output, silence, output change, alone/together with cover, hidden capture suspension |
| Clock | Seconds/dual/world zones, reorder/add/remove, DST/date boundary, timer/Pomodoro pause/restart/skip |
| Compact/focus | 5-second departure default, 0-10-second override, hover/gesture/modal guards, no configuration compaction, Clock/Chess M1 restore and independent Player/Lyrics hiding |
| Persistence | Restart/sleep deadlines, no duplicate notices, failed-save recovery, corrupt state fallback |
| Themes | All bundled themes, custom import/live edit/delete, invalid edit fallback, protected defaults, reduced motion |
| Extensions | Real drop/picker/Inbox, immediate new-module activation, automatic restart on loaded-code updates, dependency error, disable/remove, rollback, preserved State |
| Data management | Reset preferences while retaining installed files, full personal-data deletion, cancellation and graceful restart |
| Accessibility | Keyboard/context keys, Ctrl/Alt, focus visibility, tooltip-off usability, confirmation cancel/Escape |
| Resources | Repeated visible/hidden cycles, cancellation/disposal, no repeated CSS/image transfer, comparable RAM workload |

## Publishing

For a standard installation wizard with Release/Dev profiles and optional Chess, use [Windows installer](INSTALLER.md).
`powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Build-Installer.ps1` builds/tests the release and compiles that installer.

Build the complete Windows x64 release from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Build-Release.ps1 -RunTests
```

The script builds every solution project, runs the module/host integration suites when `-RunTests` is supplied,
packs the SDK and optional Chess/Notes modules, then publishes and ZIPs both host distributions under `artifacts/releases/NvDDMM/`:

| Output | Runtime requirements |
| --- | --- |
| `HIT-NvDDMM-win-x64-framework-dependent/` and `.zip` | Installed Windows x64 .NET 8 and ASP.NET Core 8 runtimes |
| `HIT-NvDDMM-win-x64-self-contained/` and `.zip` | .NET included; no installed .NET runtime required |
| `modules/` | Separate optional `.hitmenu` packages; install through Extensions |
| `sdk/` | NuGet SDK for module authors |

Both host executables embed `Resources/hit.ico` and include the logo assets for live themed window/tray icons.
The script validates embedded icon resources and runtime presence/absence before creating each ZIP.
An existing release directory is preserved; repeated builds use a timestamp suffix. Temporary profiles and user data are excluded.
The checked-in ICO is ready to build; Node/Chrome are only needed to regenerate it after SVG changes or run browser tests.
Both host distributions still need WebView2. They are complete portable folders, not single executable files.
The deployment distinction follows Microsoft's [.NET publishing reference](https://learn.microsoft.com/en-us/dotnet/core/deploying/).

To publish only the host manually:

```powershell
dotnet publish HoldItTogether.csproj -c Release -r win-x64 --self-contained false -o artifacts/publish
```

This is the normal framework-dependent output. If distributing a self-contained Windows x64 build:

```powershell
dotnet publish HoldItTogether.csproj -c Release -r win-x64 --self-contained true -o artifacts/publish-win-x64
```

Restore/runtime pack availability is required for that target. WebView2 remains a separate prerequisite.
Keep all output files together. Do not trim or convert to a single-file distribution without explicitly validating
dynamic plugin loading, native libraries, public assets and compatibility; those modes are not the documented default.

Confirm the output contains HoldItTogether.exe, HIT.Sdk.dll, required .NET/native dependencies,
wwwroot with three bundled themes and extension JS/CSS, and the complete Docs directory.
Publish into a clean/versioned destination for distribution so obsolete output files are not shipped accidentally.
Old output folders can retain historical documentation from previous builds; building does not recursively clean unrelated files.
The default user profile is not part of the release payload.
Chess is not part of this host payload. Its source/tests share the repository and solution, but its `.hitmenu` is built and distributed separately; see [Chess](CHESS.md).
If offering Chess alongside a host release, label the package as an optional extension requiring installation, not as a built-in menu.

## Commit and version checklist

Commit `.slnx`, all project files, Modules, source, manifest/template, public assets, Resources, Tools, Tests and Docs.
The ignore rules exclude bin/obj/artifacts, .vs, logs, temporary profiles and generated menus.css.
Do not force-add .hitmenu/.nupkg/executable output from artifacts to the source commit.
Application release label is 2v2910; set HitReleaseGeneration and HitReleaseDate for the next NvDDMM release. SDK 1.2.0/assembly 1.0.0.0 and package contract 1 are separate.
Before release, read [Release scope](VERSION-1.0.md), run relevant checks and record the validation/limitations in the commit or release notes.

Lyrics provider checks use deterministic HTTP fixtures for video title/channel normalization, duration-safe plain fallback, Lyrics.ovh attribution, cache reuse and cancellation. Browser checks distinguish plain paragraphs from timestamped seek controls. Live provider availability and startup foreground behavior require desktop/network acceptance beyond those fixtures.

Focused package lifecycle checks: `dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --package-checks`. These use disposable profiles and cover downgrade cancellation/confirmation, retained-version reuse, removed-package cleanup, loaded-code protection and preserved saved data. The example package fixture must be built first.

The focused UI harness also verifies background-mode persistence/normalization, GIF serving/export, portable slideshow CSS and the actual CustomBackground component advancing, pausing on resource sleep, resuming and disposing. Appearance and Settings browser checks verify portable slideshow compilation, the retained layer under artwork, the visible Image opacity title, style-picker placement and the image-preview carousel. Real GIF/WebP playback and final transition appearance remain interactive acceptance.

Image-size checks exercise the exact decimal 250 KB minimum, preserved unlimited preferences, a streamed 3 MB import beyond the former cap and removal of temporary files on size rejection. Browser checks confirm the size controls remain reachable in Settings -> Images.

The --ui-checks harness covers independent Back/Forward page history, peer Settings topics without a header parent, nested Info restore, cleared forward branches, and persisted 0/100% artwork coverage. settings-design.mjs verifies intermediate sidebar-highlight positions, final alignment, transparent controls, full action rows, live slider-fill values and directional wheel-boundary ownership. The generated SDK 1.2.0 package and Notes/Chess 1.0.2 packages were built; this does not establish installer or live desktop acceptance.

Background-gallery regression checks mount the real Personalization view and verify first-trash activation, confirmed single removal, bulk cancel/confirm and preserved Undo/profile assets. Browser checks exercise the preview scroll controls, exclusive/blended background opacity, and nonwrapping Visualizer orientation labels. Animated-image appearance still requires live visual acceptance.

Run node Tests/browser/menu-transitions.mjs after generating the host fixtures to verify entrance/topic changes, interrupted transitions, unchanged rerenders, the entrance toggle, Remove animations, reduced motion and cleanup. shared-settings.mjs also verifies common inline slider geometry across Audio pages and Clock preferences.

Image policy checks verify the Images topic saves finite/unlimited size and GIF permissions, denied GIF signatures/URLs and restoration when re-enabled. The menus browser check renders actual Timer/Pomodoro action buttons in all three bundled themes; mouse-navigation checks aligned 8 px title/inverse padding.

## 2v2910 commit review

Extension update regression: pack current Chess first, then run `dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --update-checks`. Deterministic HTTP fixtures exercise legacy discovery, stable asset selection, excluded draft/prerelease and foreign-host assets, mismatched identity/version, additional permission rejection, HTTP failure, cancellation, stale candidates and preserved State. Live downloads require a published release. The Chess packer emits `artifacts/packages/chess-v1.1.1.hitmenu`; release and installer tooling consume that pattern.

The final isolated Release solution build passed with zero warnings/errors. The complete host/extension harness passed after replacing an obsolete hardcoded fixture-version assertion; independent Chess checks and settings/shared-menu/transition/mouse/appearance browser checks passed. The native topmost initialization assertion failed once in an intermediate run and passed in the other runs; this review does not establish that intermittent issue as fixed. No installer was built or interactively tested.

The solution groups Application, SDK, Extensions and Tests without moving project identities. dotnet clean completed successfully. Generated output, IDE state and the local roadmap remain ignored. Automated approval blocked broader recursive generated-folder deletion, so historical ignored experiment directories were left in place. Git diff --check and a tracked/untracked audit found no conflict markers, missing final newlines or generated/local files in the source change set. New source/instruction files must be included when preparing the commit; nothing was staged or committed by this review.

Run dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --navigation-checks for bounded/infinite navigation and generation/date release-order checks without the package fixture.

Gesture refinements verify configuration titles cannot create a hold pill, Lyrics title settings select Lyrics, configuration scroll regions retain wheel at both boundaries, and Info actions sit at the pane bottom with static muted update status.
