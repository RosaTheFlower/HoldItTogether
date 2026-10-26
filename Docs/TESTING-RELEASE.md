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

The root project excludes SDK/example/test source from its own Razor/C# glob and references HIT.Sdk explicitly.
It embeds en.json, generates `wwwroot/css/menus.css`, copies wwwroot and includes `Docs/**/*.md` in build/publish.
Do not duplicate those projects' source inside the executable to solve a reference problem.

## SDK, sample and independent template

```powershell
dotnet pack Sdk/HIT.Sdk/HIT.Sdk.csproj -c Release -o artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project Examples/HIT.Example/HIT.Example.csproj
```

Expected: `artifacts/sdk/HIT.Sdk.1.0.1.nupkg` and `artifacts/packages/dev.hit.example-1.0.0.hitmenu`.
The pack helper publishes the module, normalizes ZIP paths, excludes the host-shared SDK DLL,
copies public asset/dependency directories and checks its own temporary cleanup target.

To verify authoring without the main project's source reference:

```powershell
dotnet new install ./Examples/HIT.Example
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
dotnet run --project Tests/HIT.Extensions.Tests/HIT.Extensions.Tests.csproj
node Tests/browser/extensions.mjs
```

The C# harness uses disposable state below obj and emits `obj/extensions-preview.html` for the browser test.
It checks invalid archive paths/links/collisions, idempotent/atomic installation, contract/capability rejection,
independent component loading, storage, asset boundaries, persisted order, built-in disabling,
stable active selection, settings wheel blocking/M2 return, updates, rollback and retained user data.

The browser test checks Dark/OLED/Light layout, stylesheet scope/keyframe namespacing,
synthetic file-drop streaming handoff, invalid names and listener teardown.
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
| Compact/focus | 3-second Clock compact/M1 restore/no hover/drag; 5-second Audio focus with active primary actions |
| Persistence | Restart/sleep deadlines, no duplicate notices, failed-save recovery, corrupt state fallback |
| Themes | All bundled themes, custom import/live edit/delete, invalid edit fallback, protected defaults, reduced motion |
| Extensions | Real drop/picker/Inbox, restart activation, dependency error, disable/remove, rollback, preserved State |
| Accessibility | Keyboard/context keys, Ctrl/Alt, focus visibility, tooltip-off usability, confirmation cancel/Escape |
| Resources | Repeated visible/hidden cycles, cancellation/disposal, no repeated CSS/image transfer, comparable RAM workload |

## Publishing

```powershell
dotnet publish HoldItTogether.csproj -c Release -o artifacts/publish
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

## Commit and version checklist

Commit `.slnx`, all four `.csproj` files, source, manifest/template, public assets, Resources, Tools, Tests and Docs.
The ignore rules exclude bin/obj/artifacts, .vs, logs, temporary profiles and generated menus.css.
Do not force-add .hitmenu/.nupkg/executable output from artifacts to the source commit.
Application release version is 1.0.0; SDK 1.0.1/assembly 1.0.0.0 and package contract 1 are separate.
Before release, read [Version 1.0](VERSION-1.0.md), run relevant checks and record the validation/limitations in the commit or release notes.
