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

Settings itself does not advertise title settings. Its Extensions subview uses the interface internally for wheel blocking and return.
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

Built-in `HitMenuViewport` supports X/Y scaling from 75-200%. It separates natural content size from measured scaled size.
Do not overwrite its JS-owned measurement styles from Razor. Audio settings remain at 100% while editing the player scale.
External menus can set CSS dimensions; the public SDK does not expose the internal viewport component as a service.

Built-ins use `HitScrollRegion` for bounded local reading/lists; its DOM wheel listener runs before delegated Blazor navigation.
External layouts must provide the same bounded local behavior and dispose native listeners they create.
Keep sliders from propagating wheel when adjusting them. Do not leave a native select focused after its final selection if that traps navigation.

## Compact layout and inactivity

Menus may request Default or Compact. Compact hides shared header/pagination and changes padding.
Request it only on state changes, and restore Default before showing settings/forms.

Clock is the reference: while pinned, three seconds without interaction or a click on the full readout requests Compact.
Only M1 reopens its controls; hover does not. M2 on a system readout opens world clocks.
The compact readout is draggable with a six-pixel movement threshold; a drag suppresses only its corresponding click.
The secondary clock is smaller/muted/centered below the primary.

Audio uses a different shared focus policy: after five seconds of inactivity while open and pinned,
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

Movement/keyboard/action restores controls. Polling, text changes and hover caused by resize do not reset inactivity.
The player contracts its window; lyrics keep the player dimensions and use the released reading space.
The shared transition uses `--hit-focus-progress` (0-1), 1200 ms layout easing and a 450 ms fade, and can reverse smoothly.
`data-hit-controls-hidden`, `data-hit-idle-view` and `data-hit-layout-animating` belong to the shell.
Reduced motion bypasses the CSS animation. Compact-clock idle uses `data-hit-idle-action="compact"`
and `data-hit-idle-delay="3000"`; `hit-menu-idle` is an event, not permission to change a hidden menu's layout.

## Accessibility, themes and performance

Use buttons for actions, real labels, accessible names, keyboard focus and native dialogs for destructive confirmations.
HIT content is non-selectable by default; an intentional text-editing field may support its native editing behavior.
Keep controls usable with tooltips disabled. Respect font/color variables and high contrast across Dark/OLED/Light themes.
No `.razor.css`: public CSS is part of the product's customization contract.

Build large option lists only when opened. Preserve drafts in view state instead of hidden DOM.
Compare visible state before rerendering large trees; do not render lyrics at every position tick within the same verse.
Cancel obsolete I/O, detach events and dispose JS/resources. Full conventions: [Code conventions](CODE-CONVENTIONS.md).
