# Clock theme reference

Behavior is documented in [Clock](CLOCK.md); common styling in [Themes](THEMES.md).
The page uses public `wwwroot/css/menus/HitMenuClock.css`; global notices use `wwwroot/css/clock-notifications.css`.
Built-in menu ID: `HoldItTogether.Components.HitMenus.HitMenuClock`.

## Selectors and views

| Selector | Purpose |
| --- | --- |
| `.clock-menu` | Root; natural full width 360 px |
| `.clock-menu.is-compact` | Compact pinned readout |
| `[data-clock-view="main"]` | Main content |
| `[data-clock-view="settings"]` | General clock settings |
| `[data-clock-view="systemsettings"]` | World clocks settings |
| `.clock-tabs`, `.clock-tab.is-current` | System/Timer/Pomodoro selection |
| `.clock-readout[data-clock-mode="system"]` | System readout |
| `.clock-readout[data-clock-mode="timer"]` | Timer readout |
| `.clock-readout[data-clock-mode="pomodoro"]` | Pomodoro readout |
| `.clock-readout.is-alert` | Pending notice state |
| `.clock-digits` | Primary tabular digits |
| `.clock-readout-label`, `.clock-readout-detail` | Full-view context |
| `.clock-secondary`, `.clock-secondary-label`, `.clock-secondary-digits` | Secondary system clock |
| `.clock-full-trigger`, `.clock-compact-trigger` | Real click/settings/drag targets |
| `.clock-duration` | Timer hour/minute/second fields |
| `.clock-progress`, `.clock-progress span` | Real phase progress |
| `.clock-button`, `.clock-button.is-primary` | Action buttons |
| `.settings-workspace`, `.settings-sidebar`, `.settings-content`, `.clock-settings` | Shared General/Pomodoro/World clocks configuration |
| `.clock-switch`, `.clock-setting-number`, `.clock-setting-summary` | Preference controls |
| `.clock-system-settings`, `.clock-world-list` | World clocks page/bounded list |
| `.clock-world-entry`, `.clock-world-preview`, `.clock-world-position` | Collapsed rows |
| `.clock-world-actions`, `.clock-world-icon`, `.clock-world-remove` | Reorder/remove controls |
| `.clock-world-add`, `.clock-world-count`, `.clock-world-field` | Add form/count/fields |
| `.clock-back`, `.clock-error`, `.clock-settings-hint` | Return/error/help |

Use these relative selectors inside `@layer menus` of an imported theme.
Do not prefix the generated `.hit-menu-panel` scope again in a relative selector.
Only target clock classes when changing Clock alone; generic h2/p rules intentionally affect every menu.

## Public variables

| Variable | Default / meaning |
| --- | --- |
| `--clock-text` | `--hit-menu-text` / `--hit-text` |
| `--clock-muted` | `--hit-menu-muted` / `--hit-muted` |
| `--clock-accent` | `--hit-accent` |
| `--clock-surface` | `--hit-open-background` |
| `--clock-digit-size` | 48 px full primary |
| `--clock-compact-digit-size` | 34 px compact primary |
| `--clock-secondary-digit-size` | 20 px full secondary |
| `--clock-compact-secondary-digit-size` | 14 px compact secondary |
| `--clock-progress` | Runtime-calculated percentage; do not hardcode |

Shell compact padding uses `--hit-compact-padding-x` (18 px) and `--hit-compact-padding-y` (14 px),
set in `@layer sphere`. This is separate from Audio's idle-focus padding/animation.
Digits use tabular numerals; avoid proportional fonts that make the native window oscillate as seconds change.

## Compact, secondary clocks and notifications

The host marks `.dynamic-sphere.is-menu-compact` and `.hit-menu-host.is-compact` and hides the header/dots.
Do not force them visible or alter pin state with CSS. The full pinned readout can compact; the compact readout reopens only with M1,
and supports real native drag through `data-hit-drag-handle`. A theme cannot implement dragging.
The second compact readout should remain small/muted and centered below the primary, without a second full label row.

The main pinned view uses `data-hit-idle-action="compact"`; the host's shared `data-hit-compact-delay` supplies 0-10000 ms, default 5000.
These are removed in compact/settings states; they are not the Audio `data-hit-hide-controls` policy.
Hover alone must not expand compact Clock. Preserve trigger roles, focus states and hit targets.

Global `.clock-notice` elements live outside menu panels; style them in `@layer sphere`.
The extension notice reuses that surface. Clock notices have priority, and compact chrome hides them.
Global music colors may color Clock; keep variable inheritance instead of neutralizing it with !important.
Reduced motion removes progress transitions without stopping actual countdowns.

## Example

```css
@layer menus {
    .clock-menu {
        --clock-digit-size: 46px;
        --clock-compact-digit-size: 32px;
        --clock-compact-secondary-digit-size: 13px;
    }
    .clock-tabs { border-radius: 12px; }
    .clock-button { border-radius: 10px; }
    .clock-readout[data-clock-mode="system"] .clock-digits { font-weight: 300; }
}
@layer sphere {
    .clock-notice { border-radius: 14px; }
}
```

Test one/two clocks, seconds on/off, world clock edits/add/reorder/remove, all three functions,
pending notices, full/compact/drag/M2 states, global artwork/colors and all motion/contrast modes.
CSS changes appearance, never time-zone rules, selected function or actual time text.

Transparent Timer/Pomodoro primary actions use the Clock foreground token, never the surface token. Typed clock fields keep a quiet outline; configuration slider values stay borderless. Shared SettingsWorkspace controls and optional entrances apply without changing countdown state.
