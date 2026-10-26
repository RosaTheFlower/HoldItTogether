# HIT public API reference

Reference: application 1.0, NuGet `HIT.Sdk` 1.0.1, namespace `HIT.Sdk`, contract `"1"`.
The authoritative declarations are [Contracts.cs](../Sdk/HIT.Sdk/Contracts.cs).
These are in-process C# contracts, not HTTP endpoints. An extension references the SDK, never the executable's internal service types.
Installation/manifest/build instructions: [Extensions](EXTENSIONS.md).

## Module entry and service container

```csharp
public interface IHitModule
{
    void ConfigureServices(IServiceCollection services);
    Task StartAsync(IHitApi api, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}
```

The interface supplies no-op defaults. The entry type must have a public parameterless constructor.
ConfigureServices runs once before constructing this module's private provider.
StartAsync runs at host startup after menu component validation; it receives the module lifetime token.
Return after initialization/starting cancellable workers; do not await an endless worker here.
StopAsync is the cleanup hook on exit or failed startup. Stop/await owned work and release resources.

Api.Services resolves only module-owned registrations and IHitApi. It is not the host's root container.
Resolve custom services with `Api.Services.GetRequiredService<T>()`; create/dispose a scope for scoped registrations.
Standard `[Inject]` on a Blazor component is still resolved by the host renderer, so do not use it to request
private module services. Host-provided renderer integrations such as IJSRuntime can be used normally.

## Root component lifecycle

Derive each manifest root component from `HitMenuComponent`:

| Member | Meaning |
| --- | --- |
| `Api` | Host-supplied `[Parameter] IHitApi` for this package |
| `Context` | Cascading `HitContext`, name `HitExtension` |
| `Lifetime` | View-owned token, cancelled on disposal and linked to module lifetime after parameter processing |
| `VisibleLifetime` | Token for the current visible period; already cancelled while hidden |
| `OnContextChanged()` | Protected virtual hook after the base processes visibility |
| `OnDisposeAsync()` | Protected virtual ValueTask hook for cleanup |

The base seals OnParametersSet to maintain token ownership. Use OnContextChanged rather than replacing that method.
OnInitialized/OnInitializedAsync remain normal Blazor hooks; Api is already assigned during initialization,
but Context/visibility-driven work should be handled in the context hook. Use Api.Lifetime for module-linked initialization I/O.
The host owns root construction/disposal. Supporting child components must receive the state/services they need.

## `IHitApi`

| Member | Contract |
| --- | --- |
| `string ExtensionId` | Stable owning package ID |
| `CancellationToken Lifetime` | Cancelled on module shutdown or failed startup |
| `IServiceProvider Services` | Private module provider |
| `IHitStorage Storage` | Scoped JSON persistence |
| `IHitMedia Media` | Shared Windows media adapter |
| `IHitClock Clock` | Shared clock/timer/Pomodoro adapter |
| `IHitAppearance Appearance` | Theme ID, reduced motion and theme selection |
| `string AssetUrl(string path)` | Validated package-relative browser URL |
| `bool OpenLink(string url)` | Open HTTP(S) with the default browser |
| `void Log(string message)` | Bounded package log |
| `void Notify(string message)` | In-HIT notice |

## Capability names

Declare exact, case-sensitive capability names in manifest `capabilities`:

| Capability | Required for |
| --- | --- |
| `media.read` | Media.ReadAsync |
| `media.control` | Media.ExecuteAsync |
| `clock.read` | Clock.Read |
| `clock.control` | Clock.StartTimer and Clock.Execute |
| `window.control` | Context.SetPinnedAsync and Context.CloseAsync |
| `theme.control` | Appearance.SelectTheme |
| `links.open` | OpenLink |
| `notifications` | Notify |

Storage, assets, logs, identity, lifetime, service resolution, appearance reads and title/layout requests need no extra capability.
Missing capabilities throw InvalidOperationException. Unknown names are rejected during installation.
Read/control are separate: declaring media.control does not grant media.read.
Capabilities define the host API contract; a trusted in-process DLL can still call the OS directly.

## `HitContext` and layout

```csharp
public sealed record HitContext(
    bool IsActive, bool IsVisible, bool IsPinned,
    Func<string, Task> SetTitleAsync,
    Func<HitLayout, Task> SetLayoutAsync,
    Func<bool, Task> SetPinnedAsync,
    Func<Task> CloseAsync);
```

IsActive means selected; IsVisible also requires an open sphere. A pinned sphere is not necessarily your active menu.
SetTitleAsync updates the common title (host truncates to 100 characters).
SetLayoutAsync accepts `HitLayout.Default` or `HitLayout.Compact`; it changes common chrome/padding, not arbitrary pixel size.
CSS natural dimensions drive the window size. Inactive title/layout requests are ignored.
SetPinnedAsync/CloseAsync require window.control and act only for the current visible menu.
Read context values anew after navigation; do not assume an old IsActive value still grants control.

```csharp
await Context.SetTitleAsync("Notes settings");
await Context.SetLayoutAsync(HitLayout.Default);
```

`IHitMenuSettings` exposes `bool IsSettingsOpen`, `Task OpenSettingsAsync()` and `Task CloseSettingsAsync()`.
The host opens it via M2 on the title, blocks menu-wheel navigation while true and handles M2 return.
The component owns its view state and must request a rerender. Full interaction rules: [Menus](MENUS.md).

## Storage

```csharp
Task<T?> ReadAsync<T>(string key, CancellationToken cancellationToken = default);
Task WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default);
Task DeleteAsync(string key, CancellationToken cancellationToken = default);
```

Keys use 1-80 ASCII letters/digits/underscore/hyphen. They are names, not file paths.
Data lives in `Modules/State/{ExtensionId}/{key}.json`. JSON uses camelCase and indented serialization.
Read of a missing key returns default(T). Invalid JSON/I/O can throw; authors define a useful fallback/migration.
Each serialized value is limited to 1 MiB; the package JSON directory is limited to 16 MiB by SDK writes.
Writes serialize/validate, use a temporary file and replace the target. Methods are serialized within this storage instance.
Deletion of a package does not erase State. Save on deliberate changes, never per render/timer tick without changed data.

```csharp
public sealed record NoteOptions(int SchemaVersion, string Text);

var options = await Api.Storage.ReadAsync<NoteOptions>("options", Api.Lifetime)
              ?? new NoteOptions(1, "");
await Api.Storage.WriteAsync("options", options with { Text = "A short note" }, Api.Lifetime);
```

## Media snapshots and commands

```csharp
Task<HitMediaState> ReadAsync(CancellationToken cancellationToken = default);
Task<bool> ExecuteAsync(HitMediaCommand command, long generation,
    double position = 0, CancellationToken cancellationToken = default);
```

ReadAsync returns the system media session chosen by HIT. No sound-recognition API is provided.
Use bounded/visibility-aware polling if your menu needs updates; there is no SDK media event subscription in 1.0.

| HitMediaState member | Meaning |
| --- | --- |
| `Generation` | Session/track generation; send it back with commands |
| `HasMedia` | A usable media session exists |
| `Title`, `Artist`, `Album`, `Source` | Metadata/source label |
| `Artwork` | Optional supported image data URL; release your retained reference when unused |
| `Playing` | Current playing state |
| `Position`, `Duration` | Seconds |
| `SeekMinimum`, `SeekMaximum` | Allowed seek range in seconds |
| `CanToggle`, `CanPrevious`, `CanNext`, `CanSeek` | Advertised playback capabilities |
| `CanLoop`, `CanShuffle` | Advertised repeat/shuffle capabilities |
| `Loop` | `Off`, `Track` or `List` as a string |
| `Shuffle` | Current shuffle state |

HitMediaCommand values: TogglePlayback, Previous, Next, Seek, ToggleLoop, ToggleShuffle, ActivateSource.
Seek uses `position` in seconds; commands require a defined enum and finite position. Capability checks and
generation checks protect the native operation. False means rejected/unsupported/stale; transport/native errors can throw.
ActivateSource restores an existing matching app window; it does not launch a new app or send global keys.

```csharp
using var timeout = CancellationTokenSource.CreateLinkedTokenSource(VisibleLifetime);
timeout.CancelAfter(TimeSpan.FromSeconds(4));
var media = await Api.Media.ReadAsync(timeout.Token);
if (media.HasMedia && media.CanSeek)
{
    var accepted = await Api.Media.ExecuteAsync(HitMediaCommand.Seek,
        media.Generation, Math.Clamp(30, media.SeekMinimum, media.SeekMaximum), timeout.Token);
    // Display a useful rejection state when accepted is false.
}
```

This snippet belongs to a visible action handler, with the caller handling expected cancellation/errors.
There is no public lyric-provider, FFT-stream, playlist-editing or output-device-selection API in SDK 1.
The built-in player contains those supported internal integrations described in [Audio](AUDIO.md).

## Clock

```csharp
HitClockState Read();
void StartTimer(TimeSpan duration);
void Execute(HitClockCommand command);
```

Read returns LocalNow (DateTimeOffset), TimerRemaining (seconds), TimerRunning, PomodoroRemaining (seconds),
PomodoroRunning and PomodoroPhase (`Focus`, `ShortBreak`, `LongBreak`). It reads the shared clock service.
StartTimer replaces the shared timer with a running duration from one second through 24 hours; invalid durations throw.
HitClockCommand values: ToggleTimer, ResetTimer, TogglePomodoro, ResetPomodoro, SkipPomodoro.
These affect the same state as the built-in Clock menu and its persistent notices.

```csharp
Api.Clock.StartTimer(TimeSpan.FromMinutes(5));
Api.Clock.Execute(HitClockCommand.ToggleTimer);
```

Use deliberate user actions for global changes. The SDK does not expose editing world-clock preferences or Pomodoro durations.
Those are built-in settings in 1.0. See [Clock](CLOCK.md).

## Appearance, assets, links, logging and notices

Appearance.ThemeId and Appearance.ReducedMotion read current host preferences.
SelectTheme accepts an existing exact ID, needs theme.control and persists through host settings.
IDs: `default`, `bundled:HIT-OLED-OFF-WHITE.css`, `bundled:HIT-LIGHT.css`, or the exact `custom:{filename}.css` ID.
The SDK has no theme-enumeration/import API or appearance change event; CSS inheritance updates presentation automatically.

AssetUrl validates a package-relative path and returns `extensions/{id}/{version}/{path}` with encoded path segments.
Use it for `<img>` URLs or JS module imports. It does not guarantee existence or permit parent/absolute paths.
Supported browser file extensions: `.css`, `.js`, `.json`, `.png`, `.jpg`, `.jpeg`, `.gif`, `.webp`, `.svg`,
`.woff`, `.woff2`, `.ttf`, `.otf`, `.mp3`, `.wav`. DLLs and the root manifest are not served as assets.

OpenLink requires links.open, accepts only HTTP(S) with a real host and no embedded user credentials,
and returns whether opening the default browser succeeded. Do not turn Ctrl hints into an unvalidated action.

Log writes UTC timestamps under `Modules/Logs/{id}.log`; messages are truncated to 2,000 characters.
The log rotates to one `.previous` file after exceeding 256 KiB. Do not log secrets or poll payloads continuously.

Notify requires notifications and truncates the message to 300 characters. The package name is the title.
Only the latest extension notice is kept; the user dismisses it. It appears inside open HIT, yields priority to clock notices,
and is hidden by compact chrome. It does not show a Windows toast or force another application to the foreground.

## Error and compatibility checklist

Declare capabilities, check supported actions, send current generation, pass cancellation, bound worker intervals,
handle missing data/errors and release retained resources. Do not claim OS isolation or a guaranteed plugin RAM quota.
Use only SDK contracts for public compatibility; undocumented internal types can change independently.
Additive APIs and breaking contract changes require deliberate versioning and updated examples/tests.
