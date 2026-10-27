# Shared HIT menu contract

This contract applies to built-in menus and external `.hitmenu` layouts.
Use [Internal menus](INTERNAL-MENUS.md) for application-source registration,
or [Extensions](EXTENSIONS.md) and [API](API.md) for independent modules.

## One entry, several views

A menu creates one navigation dot and owns one entry component. Details, reading, object configuration,
general configuration and confirmation views belong to that same menu; they do not create separate entry files/dots.
Prefer an explicit view enum for mutually exclusive screens. Define Back, Escape, M2 and sizing behavior for each screen.

| View | Purpose | Host behavior |
| --- | --- | --- |
| Main | Frequent information/actions | Normal menu navigation |
| Detail/reading | Lyrics or expanded content | Return to the owning main screen |
| Menu settings | Preferences for that menu | M2 on title; blocks wheel-based menu switching |
| Object settings | Preferences for one control/object | M2 on the object; same settings-open contract |
| Confirmation | Identified destructive operation | Modal focus, safe cancel, explicit second action |

The host owns the single h1 title, pin/close buttons, margins and dots.
Use h2/h3 for content sections. Do not duplicate the common header inside the menu.
Publish a title when entering a view; do not republish unchanged titles on every render.

## Navigation, state and visibility

Scroll down/up advances/returns without wrapping. Large gestures can traverse several menus when cooldown is disabled.
Dots provide deliberate click/keyboard navigation even when settings block the wheel.
The initial built-in order is Audio, Clock, Settings; persisted user order takes priority.
Settings stays last and enabled. State is keyed by stable menu ID, never only by numeric position.
Historical Clock/Settings CLR IDs migrate to their English names when the registry loads.

The first visit mounts a view. Normal navigation hides it with hidden/inert and preserves state.
Disabling/removing unmounts it. A hidden view is not included in geometry measurement.
IsActive means selected; IsVisible means selected and the sphere is open. Polling, capture and observers
belonging to presentation must respect visibility. Use service-owned background work only when the feature needs it.

| Built-in `HitMenuContext` | External `HitContext` |
| --- | --- |
| `IsActive`, `IsVisible`, `IsPinned` | Same state |
| `TitleRequested.InvokeAsync(title)` | `SetTitleAsync(title)` |
| `LayoutRequested.InvokeAsync(HitMenuLayout...)` | `SetLayoutAsync(HitLayout...)` |
| `CloseRequested.InvokeAsync()` | `CloseAsync()`; needs `window.control` |
| No direct pin callback in the built-in record | `SetPinnedAsync(bool)`; needs `window.control` |

External context functions are delegates supplied by the host. Title/layout requests from inactive menus are ignored.
Pin/close are checked against the current visible menu, including when a caller retained an old context.

## Settings and right-click return

Implement `HIT.Sdk.IHitMenuSettings` for extension roots; the built-in interface derives from it.

```csharp
public bool IsSettingsOpen => _view == MenuView.Settings;

public Task OpenSettingsAsync() => InvokeAsync(() => {
    _view = MenuView.Settings;
    StateHasChanged();
});

public Task CloseSettingsAsync() => InvokeAsync(() => {
    _view = MenuView.Main;
    StateHasChanged();
});
```

Include object-setting views in IsSettingsOpen too. The flag blocks menu-wheel navigation immediately.
The title supports right-click, ContextMenu, Shift+F10 and Enter when focused.
M2 inside open settings returns without collapsing/unpinning HIT. The host records state at pointer-down,
so the same M2 that opens settings does not immediately close them.

Settings itself does not advertise title settings. Its Scroll speed, Info, Extensions and Themes subviews use the interface internally for wheel blocking and return.
Provide a visible Back button and keyboard access. Avoid intercepting the configurable title or object trigger with a competing return handler.

## Object configuration and modifier hints

```razor
<button type="button" data-no-drag data-hit-menu-trigger="appearance"
        @onclick="ToggleAppearance"
        @oncontextmenu="OpenAppearanceSettings"
        @oncontextmenu:preventDefault="true" @oncontextmenu:stopPropagation="true">
    Appearance
</button>
```

The marker announces a real M2 configuration target; it does not implement the action.
Do not mark ordinary Back controls or non-configurable objects.

| Gesture/marker | Indication and behavior |
| --- | --- |
| Ctrl over `data-ctrl-link="true"` | Solid underline/click cursor; handler must validate Ctrl+M1 |
| Alt over `data-hit-menu-trigger` | Dotted underline/dashed highlight; M2 opens the actual settings |
| M2 on a configurable title | Menu settings |
| M2 on a configurable object | That object's settings |
| `data-no-drag` | Interactive region does not move the native window |
| `data-hit-drag-handle` | Delayed drag handle that preserves a simple click |

The shell manages modifier attributes and clears them on key release/focus loss. Alt alone does not open a menu.
Disabled controls do not receive active hints. In Audio, Ctrl+click opens the actual lyric-source URL or activates the existing media app;
the latter also supports Ctrl+Enter. Native drag ignores Ctrl actions on marked objects.

## Size, scaling and scroll regions

Use a natural content width and normal flow. The host adds header/margins/pagination and measures changes,
including live CSS edits. Avoid 100vw/100vh and feedback loops where content size depends on window size.
A request for 800 px content is measured, but content larger than the physical work area still needs responsive layout/scroll.
Native anchoring, edge limits and magnetism belong to the shell.
Anchors and drag grabs refer to the visible surface, independently of the transparent native canvas.
The pivot is selected from the dropped surface's monitor position, remains fixed through size changes, and is returned
to JavaScript before publishing geometry for its new alignment. Dragging freezes the visible dimensions until release.
In-flight older resize responses cannot change a newer drag's alignment; canvas-size updates are sent before the next paint.
Only visible bounds are constrained to the padded work area. Fitting a large menu does not overwrite its original drop anchor.
All three compactable menus use this same placement path. See [Settings](SETTINGS.md) for monitor, magnetism, padding and position persistence.

The shell reserves a canvas while opening so the sphere animates inside a stable native window.
It retains the largest visited dimensions until closing, avoiding native WebView shrink/repaint flashes during wheel navigation.
The active surface is opaque. Content fades only on opening, without translation, scale or blur; switching visible menu panels does not restart an opacity animation.
The reserved canvas is released after closing finishes. Wheel input grants a short pointer-exit grace period while geometry settles.
The native Windows region follows the surface's outer rectangle, excluding the retained canvas and padding.
It uses [SetWindowRgn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowrgn),
which clips the native window and transfers region ownership to Windows.
Areas one physical pixel beyond the outer bounds remain available to other apps; region handles transfer to Windows and are not leaked.
Chromium alone draws the antialiased round corners. A binary GDI rounded mask must not clip those edge pixels.
Dialogs are centered within the visible sphere, rather than the larger retained canvas.
The anchor/alignment is resolved before the first reveal to avoid a collapsed sphere briefly appearing at a canvas corner.
Page zoom resets to 100% at startup and each menu opening. Capture-phase listeners block Ctrl/Meta+wheel and zoom keys;
Audio's explicit Size preference still scales its content independently of browser zoom.
External menu children mount after their public styles apply; a view still loading its initial data may declare
`data-hit-layout-pending="true"` on its root to defer initial expansion until its real dimensions are available.
Remove the attribute when ready. Inactive views do not block another menu's expansion.
Loading a menu during navigation does not fade the entire shared chrome to an empty background.

Configuration screens expose the header's temporary cover toggle beside Pin/Close.
`data-hit-configuration` on the host declares configuration state; `data-hit-view` markers distinguish its screens.
`data-audio-reading-hidden` hides the existing artwork layer without changing its saved `data-audio-cover` request.
The override belongs to the current menu/view only, resets on navigation, and never persists to storage.

Built-in `HitMenuViewport` supports X/Y scaling from 25-200%. It separates natural content size from measured scaled size.
Do not overwrite its JS-owned measurement styles from Razor. Audio settings scale with the player and lyrics.
Lyrics follow layout coordinates divided by the actual transform scale. Shell-owned artwork stays independent of content scaling.
External menus can set CSS dimensions; the public SDK does not expose the internal viewport component as a service.

Built-ins use `HitScrollRegion` for bounded local reading/lists; its DOM wheel listener runs before delegated Blazor navigation.
External layouts must provide the same bounded local behavior and dispose native listeners they create.
Keep sliders from propagating wheel when adjusting them. Do not leave a native select focused after its final selection if that traps navigation.

## Compact layout and inactivity

Menus may request Default or Compact. Compact hides shared header/pagination and changes padding.
Request it only on state changes, and restore Default before showing settings/forms.
Only pinned Audio Player/Lyrics, main Clock views and the board of the separately installed [Chess extension](CHESS.md) use the pointer-departure timer in `hit-menu-chrome.js`: five seconds by default,
configurable through Settings -> Compact after from immediately to ten seconds. Hover prevents compaction.
An open native confirmation, promotion chooser, pointer gesture or native drag suspends the timer.
`HIT.Sdk.IHitMenuCompact` exposes `Task CompactNowAsync()` for dedicated miniature layouts.
Clock/Chess enter those layouts; Audio hides secondary controls when its page-specific preference permits it.
Settings/forms, General Settings and all unrelated modules never compact; there is no generic summary fallback.
Audio settings -> Hide buttons persists independent Player and Lyrics switches; changing one does not affect the other.
There is no shared compact header button. Legacy module `hit-menu-idle` events remain a fallback; modules do not own a second timer.

Clock is the reference: while pinned, the shared departure delay or a click on the full readout requests Compact.
Only M1 reopens its controls; hover does not. M2 on a system readout opens world clocks.
The compact readout is draggable with a six-pixel movement threshold; a drag suppresses only its corresponding click.
The secondary clock is smaller/muted/centered below the primary.

Audio uses a dedicated compact presentation: after the shared pointer-departure delay while open and pinned,
hide the common header and marked secondary actions while keeping playback/lyrics interactive.

```html
<section data-hit-hide-controls="true" data-hit-view="player">
  <div class="hit-idle-collapse" data-hit-collapse="player">
    <div class="hit-idle-collapse-content">
      <div data-hit-idle-controls>Secondary actions</div>
    </div>
  </div>
</section>
```

Actual pointer movement/keyboard/action restores Audio controls. Polling, text changes and hover caused by resize do not reset the departure timer.
The player contracts its window; lyrics keep the player dimensions and use the released reading space.
The shared transition uses `--hit-focus-progress` (0-1), 1200 ms layout easing and a 450 ms fade, and can reverse smoothly.
`data-hit-controls-hidden`, `data-hit-idle-view` and `data-hit-layout-animating` belong to the shell.
Reduced motion bypasses the CSS animation. `data-hit-compact-delay` on the sphere supplies the shared delay;
per-menu hardcoded delays are not used. `hit-menu-idle` is an event, not permission to change a hidden menu's layout.

## Accessibility, themes and performance

Use buttons for actions, real labels, accessible names, keyboard focus and native dialogs for destructive confirmations.
HIT content is non-selectable by default; an intentional text-editing field may support its native editing behavior.
Keep controls usable with tooltips disabled. Respect font/color variables and high contrast across Dark/OLED/Light themes.
No `.razor.css`: public CSS is part of the product's customization contract.
Use [Design](DESIGN.md) for common controls, confirmation geometry, colors and scroll hints.

Build large option lists only when opened. Preserve drafts in view state instead of hidden DOM.
Compare visible state before rerendering large trees; do not render lyrics at every position tick within the same verse.
Cancel obsolete I/O, detach events and dispose JS/resources. Full conventions: [Code conventions](CODE-CONVENTIONS.md).
