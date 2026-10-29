# HIT extension packages — format 1 / SDK 1

Application 2v2910; SDK NuGet 1.2.0. Complete signatures, DTOs, capability rules and examples are in [API](API.md).
SDK 1.1 adds IHitApiV2 events/actions without changing the original interfaces or assembly identity.
New event-using packages declare events.subscribe plus the data-specific capability and require the
new host; old contract-1 packages remain supported. See [events and actions](API.md#additive-events-and-actions-sdk-110).
Shared UI rules are in [Menus](MENUS.md); code/lifetime rules are in [Code conventions](CODE-CONVENTIONS.md).

The `.hitmenu` installer, startup loader, public SDK and extension manager are implemented.
A `.hitmenu` file is a ZIP containing compiled .NET components, a manifest and public assets.
The user does not need the .NET SDK or the HIT source to install a package.

## Authoring walkthrough

1. Pack the SDK locally and generate a project with the commands in Build the example and SDK below.
2. Choose a stable lowercase package ID and unique menu IDs. Edit manifest name/author/version separately from those identities.
3. Keep module/service code in Services, layouts in Layout, plain public CSS in Layout/css and assets in Assets.
4. Implement IHitModule; register only private feature services. Each declared root inherits HitMenuComponent.
5. Use Api for host integrations and Context for title/layout/window requests. Declare only the capabilities actually used.
6. Add one menu descriptor per navigation dot. Keep settings/details as subviews of that component.
7. Implement IHitMenuSettings only when the menu provides settings; show Back and support the common right-click contract.
8. Save through Api.Storage on deliberate changes, handle missing data and keep a schema version when migrations are needed.
9. Use visibility/module cancellation, dispose workers/JS/scopes and test hidden/disabled/restarted states.
10. Pack, import into HIT and exercise all three bundled themes plus user menu reorder/disable. New packages activate immediately; replacing loaded code restarts HIT automatically.

The executable example is split into [module/service](../Modules/HIT.Example/Services/ExampleModule.cs),
[Razor layout](../Modules/HIT.Example/Layout/ExampleMenu.razor),
[CSS](../Modules/HIT.Example/Layout/css/notes.css) and [manifest](../Modules/HIT.Example/manifest.json).
It contains note persistence, a settings subview and a deliberate call to the shared HIT timer.
The 1.0.2 example exposes General and Storage topics using the host's shared workspace markup.
The template changes the project/namespace from HIT.Example; review the visible manifest name/author and remove sample actions/capabilities you do not need.
Public component names do not have to begin with HitMenu for external packages: the manifest supplies their exact types.

## User flow

Repository-owned extension settings use the same shared layout as built-ins. Declare a root
`.settings-workspace[data-hit-settings-workspace]`, a `.settings-sidebar` of `.settings-topic`
buttons with `aria-current="page"` for the active topic, and a `.settings-content[data-hit-scroll]`
pane with its heading and native bound form controls. The updated host automatically enhances
selects/ranges with its three-row wheel picker, numeric values, descriptions and cleanup, and supplies
the structural CSS. Keep C# event handlers, normalization, storage and confirmation semantics in
the extension. Native elements remain the underlying value/event boundary; no host assembly
reference or copy of the picker implementation belongs in the module.
Publish the selected page title through Context.SetTitleAsync. IsSettingsOpen remains true for
all topic pages; CloseSettingsAsync returns a subtopic to General, then General to the main view.
Do not duplicate shared title/pin/close/header Back controls in the workspace. Existing SDK menus
use the shared marked-pane wheel routing: scroll while content remains in the requested direction; empty boundaries may navigate HIT. Third-party code can still intercept its own events.

The host owns discovery observers and generated-control listeners. Hidden menu panels dismiss
their picker; removal/disabling releases controls and pending animations. Module disposal still
owns its subscriptions, workers and package-local DOM resources. The host description preference
is applied to marked extension workspaces without exposing global preference writes to modules.


Open **Settings → Extensions**, immediately above the final **Themes** row.
The row opens a separate page. Right-click or the Back button returns to Settings.
The mouse wheel scrolls the extension list without changing HIT menus.

- Drag the grip beside a name or use Up/Down to reorder menus. Pointer dragging starts after six pixels, shows the insertion edge and saves the result immediately.
- Disable a menu with its switch. Built-in Audio and Clock can be disabled; Settings always remains available and last.
- Choose **Install .hitmenu**, or drop one `.hitmenu` file anywhere on HIT, including the collapsed sphere. Dropped `.css` files instead open Themes after importing.
- Installation opens the manager and reports success or a useful error. A new module starts immediately. If its assembly is already loaded at another version, HIT shuts down gracefully and restarts automatically, reopening Extensions.
- Remove an external extension with the trash button and confirmation dialog. All of its menus disappear. Files are deleted on the next startup; saved data remains.
- A newer version is installed alongside the old one. **Restore previous version** selects the retained version, restarting automatically if a different version is loaded.

Reordering and navigation visibility update immediately. Built-in rows show disabled trash icons; their files cannot be removed.
New modules are loaded live; replacement/unloading of executing DLLs is not performed live.
Disabling an already loaded menu disposes its component; module-level background services stop when HIT exits.
If all menus of a package are disabled, its module is not loaded on the next startup.
Enabling a package skipped at startup initializes it immediately.

## On-disk layout

```text
%LOCALAPPDATA%/HoldItTogether/Modules/
  registry.json                 Installed versions, menu order and disabled IDs
  Inbox/                        Optional automatic import folder
    Processed/                  Successfully imported source packages
    Rejected/                   Invalid packages
  Staging/{operation}/          Temporary extraction; removed after the operation
  Installed/{id}/{version}/     Compiled modules and editable assets
  State/{id}/                   User data, independent of module versions
  Logs/                         Bounded per-extension logs
```

Inbox checks every two seconds, waits for stable size/timestamp and exclusive read access, and uses the same installer as the UI.
Deleting an extension does not erase State. Normal theme selection does not write to built-in CSS files.

## Package contents

```text
dev.example.notes-1.0.0.hitmenu
  manifest.json
  Services/
    Example.dll
    Example.deps.json
  Layout/
    css/notes.css
  Assets/
    icon.svg
  Dependencies/                 Optional private .NET dependencies
```

One assembly may contain both services and compiled Razor layouts, as in the shipped example.
Separate assemblies are supported by the manifest. Source files are optional authoring material;
the installer does not compile loose `.cs` or `.razor` files.

```json
{
  "formatVersion": 1,
  "id": "dev.example.notes",
  "name": "Example Notes",
  "author": "Example",
  "version": "1.0.0",
  "hitContractVersion": "1",
  "entryAssembly": "Services/Example.dll",
  "entryType": "Example.Services.ExampleModule",
  "capabilities": ["clock.control"],
  "menus": [{
    "id": "notes",
    "title": "Notes",
    "componentAssembly": "Services/Example.dll",
    "componentType": "Example.Layout.NotesMenu",
    "styles": ["Layout/css/notes.css"]
  }]
}
```

IDs contain lowercase letters, digits, dots or hyphens. Menu identity is `{packageId}:{menuId}`.
Names, translations and version updates must not change IDs. Versions use three or four numeric components.
Updates can increase the version or, after explicit confirmation, downgrade it. Reimporting identical bytes is idempotent.
Clear old versions at the bottom of Extensions removes obsolete installed version directories.
For active packages it preserves the registry-selected and currently loaded versions. Removed packages retain only code still loaded in the current process. State and Logs are kept; linked/locked trees are skipped.
Deleting the previous rollback target clears its registry pointer. Cleanup and activation are serialized.

## Public SDK and services

`Modules/HIT.Sdk` is an independent .NET 8 library. Extensions reference `HIT.Sdk`, not the executable.
Use the Razor SDK and `Microsoft.AspNetCore.App` framework reference for Blazor layouts.
The module entry implements `IHitModule` with a public parameterless constructor:

```csharp
public sealed class NotesModule : IHitModule
{
    public void ConfigureServices(IServiceCollection services)
        => services.AddSingleton<NotesService>();

    public Task StartAsync(IHitApi api, CancellationToken lifetime)
        => Task.CompletedTask;

    public Task StopAsync(CancellationToken cancellationToken)
        => Task.CompletedTask;
}
```

ConfigureServices receives a private module collection. `Api.Services` resolves that module's services;
it does not expose HIT's root container. Create/dispose a scope for scoped registrations.
Background workers must honor the lifetime token and finish in StopAsync. Prefer short initialization;
the first implementation awaits module startup before opening the window.

| SDK surface | Capability | Behavior |
| --- | --- | --- |
| `Api.Storage` | Always available | JSON read/write/delete scoped to this package; 1 MiB per value, 16 MiB total |
| `Api.AssetUrl(path)` | Always available | Returns a URL for a package asset |
| `Api.Log(message)` | Always available | Per-package log, 256 KiB rotation and bounded messages |
| `Api.Media.ReadAsync(token)` | `media.read` | Snapshot of the Windows media session and supported controls |
| `Api.Media.ExecuteAsync(...)` | `media.control` | Playback, previous/next, seek, shuffle, repeat and source activation; uses generation checks |
| `Api.Clock.Read()` | `clock.read` | Current time and shared timer/Pomodoro state |
| `Api.Clock.StartTimer/Execute` | `clock.control` | Controls the same timer/Pomodoro used by the built-in Clock menu |
| `Api.Appearance.ThemeId/ReducedMotion` | Always available | Current theme and motion preference |
| `Api.Appearance.SelectTheme` | `theme.control` | Selects an existing theme through HIT settings |
| `Api.OpenLink(url)` | `links.open` | Opens an HTTP(S) address in the default browser |
| `Api.Notify(message)` | `notifications` | One bounded in-HIT notification, visible with the sphere open; clock alarms take priority |
| `Context.SetPinnedAsync/CloseAsync` | `window.control` | Requests changes from the active menu through the host |

Do not poll an API merely to detect visibility; use Context. Pass cancellation tokens to ongoing I/O.
Do not use display strings, theme names or numeric menu positions as identifiers.

## Layout and navigation

External root components inherit `HitMenuComponent`. The host supplies `Api` as a parameter and `Context` as a cascading value.
The base class supplies Lifetime and VisibleLifetime cancellation tokens; use `OnContextChanged` and `OnDisposeAsync` for work owned by the view.
VisibleLifetime is cancelled when the menu becomes hidden and renewed when visible again.
Component Lifetime is also linked to `Api.Lifetime`, so module shutdown cancels ongoing view work.

```razor
@using HIT.Sdk
@inherits HitMenuComponent

<section class="notes" data-no-drag>
    <p>My menu</p>
</section>
```

Context exposes IsActive, IsVisible, IsPinned, SetTitleAsync and SetLayoutAsync (Default/Compact).
Natural HTML/CSS dimensions drive the existing window measurement; use a fixed natural width rather than viewport percentages.
Only the selected menu may change the common title or layout.

Implement `HIT.Sdk.IHitMenuSettings` for right-click settings on the common title:
IsSettingsOpen, OpenSettingsAsync and CloseSettingsAsync. Return Task and rerender after changing view state.
The host blocks menu-wheel navigation while settings are open and handles right-click return.
Do not duplicate the common title, pin or close controls inside the layout.
Submenus use `data-hit-menu-trigger`, Ctrl actions use `data-ctrl-link`, and interactive surfaces use `data-no-drag`.
The existing [menu interaction guide](MENUS.md) applies to extensions too.

## Public styling

Menu styles remain editable under Installed. The host checks file metadata every two seconds while the menu is visible,
and only rereads/recompiles changed files. Invalid edits preserve the previous valid CSS.
No `.razor.css` or generated scoped stylesheet is required.

- Write plain CSS; no `@layer sphere/menus` wrapper is needed for package-local styles.
- `:root`/`:scope` targets that menu's container. Other selectors are prefixed with its stable menu ID.
- Keyframes and font names are namespaced per menu. Relative `url(...)` resources resolve against the CSS asset URL.
- `@media`, `@supports`, `@font-face` and `@keyframes` are supported. `@import` and CSS nesting are rejected.
- Use `--hit-menu-text`, `--hit-menu-muted`, `--hit-accent`, `--hit-menu-font` and the existing theme variables.
- The global theme remains separate and can style the extension. Reduced motion remains authoritative.
- JavaScript modules may be imported with `"./" + Api.AssetUrl(...)`; dispose references and observers with the component.
- The asset provider serves supported CSS/JS/JSON/image/font/audio types, not DLLs or arbitrary disk paths.

Scoping prevents ordinary styling collisions; trusted .NET/JS extension code is not a sandbox.
The host applies an extension's styles before mounting its component, preventing measurements of unstyled markup.
For asynchronous initial loading, `data-hit-layout-pending="true"` on the root defers opening until the view removes it.

`AssetUrl` returns a document-relative URL such as `extensions/dev.example.notes/1.0.0/Assets/menu.js`.
Native JavaScript `import()` requires the explicit `./` prefix for a relative module specifier:

```csharp
var module = await JS.InvokeAsync<IJSObjectReference>("import", "./" + Api.AssetUrl("Assets/menu.js"));
```

When an extension fails, the error boundary logs the primary exception through `Api.Log` and disables that menu's settings routing.
Disposed components must ignore delayed callbacks and must not start workers or request layout/title changes after disposal.

## Build the example and SDK

From the repository root:

```powershell
dotnet pack Modules/HIT.Sdk/HIT.Sdk.csproj -c Release -o artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project Modules/HIT.Example/HIT.Example.csproj
```

The ready-to-install example is `artifacts/packages/dev.hit.example-1.0.2.hitmenu`.
It demonstrates local note storage, right-click settings, editable CSS and the shared timer API.
It is not automatically installed into the user's profile.

For a project outside this repository:

```powershell
dotnet new install ./Modules/HIT.Example
dotnet new hitmenu -n MyNotes --extensionId dev.example.notes -o ../MyNotes
dotnet restore ../MyNotes/MyNotes.csproj --source ./artifacts/sdk
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project ../MyNotes/MyNotes.csproj
```

The template uses the local SDK project inside this repository and the NuGet package elsewhere.
The feed is local; the SDK has not been published to a public registry.
The packing helper places the project's published assembly/dependencies in Services and copies Layout/css, Assets and Dependencies.
For separate component assemblies, ensure the manifest paths match the produced ZIP. Native/runtime-specific dependencies need their required layout preserved.

## Installation and lifecycle boundaries

### GitHub release updates

An optional manifest field declares a public GitHub repository and release-asset prefix:

```json
{
  "updates": {
    "repository": "RosaTheFlower/HoldItTogether",
    "assetPrefix": "chess-v"
  }
}
```

The repository is an owner/name pair; the prefix contains letters, digits, dots, underscores or hyphens. Assets are named `{assetPrefix}{numericVersion}.hitmenu`; the version must match the enclosed manifest. This additive field preserves contract 1 and needs no SDK update. Legacy Chess uses this source by stable ID when its manifest omits updates. Other extensions without a source remain manually installable.

Settings -> Extensions exposes Check for extension updates and per-package Update to {version} actions. There is no background polling. The host queries the first 20 GitHub releases, skips drafts/prereleases and chooses the highest newer package version among up to 100 assets per release. Host release tags do not determine extension versions. Discovery is bounded to 2 MiB and 15 seconds per check; downloads have a two-minute cancellation deadline and the existing 64 MiB limit. HTTPS asset URLs must belong to the declared GitHub repository. Network/rate-limit errors produce retryable feedback without modifying installation state.

Before the atomic installer runs, the updater validates the archive manifest, exact package ID/version, unchanged update source and no additional capabilities. Changed permissions/source require manual review/import. Existing archive safety, contract and loaded-code restart checks still apply. Temporary downloads are removed, cancellation follows the view lifetime, and saved state is retained. The publisher attaches the package to a public stable release; the host never uploads it.


The installer accepts at most 64 MiB compressed, 128 MiB extracted, 2,000 entries, 20 menus per package and 100 packages.
It rejects parent/absolute paths, Windows reserved names, links, case-insensitive collisions, incompatible contracts and unknown capabilities.
Manifest size is limited to 64 KiB; each stylesheet is limited to 1 MiB.
Assemblies are inspected as metadata during import. The activation service then loads enabled new modules,
or requests a graceful restart when a different version is already executing. The byte installer itself never executes code.

Each loaded module has an AssemblyLoadContext and private service provider. SDK/framework identities are shared with HIT,
including `Microsoft.JSInterop` so injected `IJSRuntime` and JavaScript object references use the host renderer's types.
Component render failures show a local error. Startup failures are listed in Extensions and leave built-in menus usable.
Rollback is explicit; the previous version's files remain available. User-data migration remains the extension author's responsibility.
Unload is cooperative and deferred until exit; the host does not force garbage collection or promise to reclaim arbitrary plugin RAM.

A DLL runs with HIT's privileges. Capabilities gate the public APIs but cannot stop a DLL from calling the OS directly.
Process isolation, a hard memory quota, live replacement of executing DLLs and language-pack installation are future work.
The current English UI catalog is described in [LOCALIZATION.md](LOCALIZATION.md).

## Manifest field reference

| Field | Required contract |
| --- | --- |
| `formatVersion` | Integer 1 |
| `id` | Stable 1-100 character lowercase ID; letters/digits with dot/hyphen-separated segments |
| `name` | Nonblank visible package name, at most 80 characters |
| `author` | Optional string, at most 120 characters |
| `version` | Numeric major.minor.patch with optional fourth component, at most 32 characters |
| `hitContractVersion` | String `"1"` |
| `entryAssembly` | Forward-slash relative .dll path |
| `entryType` | Nonblank module type name, at most 250 characters |
| `capabilities` | Array of supported exact names; omitted means none |
| `menus` | 1-20 descriptors with unique IDs |
| `menus[].id` | Stable ID following the same ID syntax; combines with package ID |
| `menus[].title` | Nonblank display title, at most 80 characters |
| `menus[].componentAssembly` | Relative .dll path; can be the same as entryAssembly |
| `menus[].componentType` | Nonblank root type name, at most 250 characters; derives from HitMenuComponent |
| `menus[].styles` | Up to 10 relative CSS paths, each present and at most 1 MiB |

Paths have at most 220 characters and cannot contain backslashes, colons, absolute/parent/empty segments,
Windows reserved names or trailing dots/spaces. Extraction also rejects duplicate case-insensitive paths and links.
Manifest.json must be at ZIP root. A Services assembly and a Layout assembly are conventions, not mandatory directory names in the loader.
IDs, compatibility and limits are validated before executing package code. Metadata inspection cannot guarantee that arbitrary trusted code will behave correctly.

## Validation

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj
node Tests/browser/extensions.mjs
```

Build the example first. The native tests use a disposable profile below obj and emit the HTML consumed by the browser test.
Coverage includes archive validation, failed-install atomicity, independent component loading, SDK capabilities, storage,
assets, persisted order, built-in disabling, stable selection, right-click return, blocked scroll, versioned updates and rollback.
The browser test checks all three bundled themes, scoped CSS/keyframes and the file-drop stream handoff.

### Confirmed downgrades and removed-package cleanup

Importing a lower version asks for confirmation in the shared host dialog, including file picker, drop and Inbox imports. Cancel leaves the selected version unchanged. An identical retained version can be selected again without overwriting loaded files; different bytes under the same retained version remain rejected. Saved extension data is preserved. Clear old versions also deletes code versions belonging to packages removed in HIT. Currently loaded code is skipped until restart.
