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

```powershell
dotnet pack Modules/HIT.Sdk/HIT.Sdk.csproj -c Release -o artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project Modules/HIT.Example/HIT.Example.csproj
```

Expected: `artifacts/sdk/HIT.Sdk.1.0.2.nupkg` and `artifacts/packages/dev.hit.example-1.0.0.hitmenu`.
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
node Tests/browser/navigation.mjs
node Tests/browser/audio-effects.mjs
node Tests/browser/idle-input.mjs
node Tests/browser/spectrum.mjs
node Tests/browser/placement.mjs
dotnet run --project Modules/HIT.Chess.Tests/HIT.Chess.Tests.csproj
node Tests/browser/chess.mjs
node Tests/browser/app-icon.mjs
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

```powershell
node Tools/Check-Documentation.mjs
node Tools/Check-Documentation.mjs --published artifacts/publish
```

This validates centralized Markdown placement, local file/anchor links, JSON code blocks and documentation publication configuration.
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
packs the SDK and optional Chess/Notes modules, then publishes and ZIPs both host distributions under `artifacts/releases/vDDMM/`:

| Output | Runtime requirements |
| --- | --- |
| `HIT-vDDMM-win-x64-framework-dependent/` and `.zip` | Installed Windows x64 .NET 8 and ASP.NET Core 8 runtimes |
| `HIT-vDDMM-win-x64-self-contained/` and `.zip` | .NET included; no installed .NET runtime required |
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
Application release label is v2710; update HitReleaseDate for the next vDDMM release. SDK 1.0.2/assembly 1.0.0.0 and package contract 1 are separate.
Before release, read [Version 1.0](VERSION-1.0.md), run relevant checks and record the validation/limitations in the commit or release notes.
