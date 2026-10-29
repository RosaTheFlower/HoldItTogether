# Themes and visual design — contract 1

HIT themes are public CSS files. They style the sphere, shared chrome and menu content after compilation,
without editing the application source. Extension-local layout CSS is a separate contract described in [Extensions](EXTENSIONS.md).
Source files are under `wwwroot/css` and `wwwroot/themes`; paths in this guide are repository-relative.

## Three kinds of stylesheet

| Kind | Location/format | Ownership |
| --- | --- | --- |
| Structural CSS | `wwwroot/css/*.css`, `css/menus/*.css` | Public layout rules distributed with HIT |
| Global theme | `wwwroot/themes` or personal Themes directory; `@layer sphere/menus` | Sphere/menu appearance; personal files can be imported |
| Extension-local CSS | Installed package files declared in manifest styles; plain CSS | One menu's structure/appearance, scoped by its ID |

Do not use `.razor.css`: customization is intentionally possible after build.
Themes cannot create a service, persist a preference, add a real menu, change time zones or implement a click handler.
Use an extension for behavior/HTML changes, a theme for presentation.

## Bundled themes and protection

| Display name | Persisted ID | Canonical file |
| --- | --- | --- |
| HIT Dark Theme | `default` | `wwwroot/themes/HIT-OBSIDIAN-EYES.css` |
| HIT OLED | `bundled:HIT-OLED-OFF-WHITE.css` | `wwwroot/themes/HIT-OLED-OFF-WHITE.css` |
| HIT Light Theme | `bundled:HIT-LIGHT.css` | `wwwroot/themes/HIT-LIGHT.css` |

Dark is the base/default/fallback. Historical filenames/IDs remain compatible with existing settings.
OLED uses true black and off-white; Light uses light surfaces/dark text.
The explicit catalog ignores unrelated old CSS files left in an output directory.
The UI and service reject deletion of bundled themes. Imported copies are independent personal themes.
Choosing/importing/deleting a personal theme never rewrites or deletes canonical CSS.

## Importing, editing and removing

For built-in palette/shape editing without writing CSS, use Settings -> Personalization.
It applies a separate compiled CSS override above the selected theme, saves changes automatically and can save named visual
profiles or export a theme-compatible CSS file including the base theme. See
[personalization](SETTINGS.md#personalization-and-visual-profiles) for autosave/undo and artwork-color
precedence. Disabling the override restores the existing theme rather than rewriting any theme file.

Open the Settings -> Themes topic for management, or use its full-width combobox for immediate activation.
The list separates selection from activation: click selects, Ctrl+click selects several, double-click or Enter activates.
Activating a list item also refreshes the quick combobox label and its next opening selection. The native select publishes `data-hit-select-value` with the active theme so the shared DOM adapter observes programmatic Blazor value changes without polling or dispatching another change event.
Open folder and Delete selected sit above the final Import CSS action. CSS may also be dropped anywhere on HIT.
An imported `.css` file is copied into `%LOCALAPPDATA%\HoldItTogether\Themes`; edit that installed copy.
The maximum file size is 1 MiB. The filename is sanitized and collisions receive suffixes.
Theme IDs use `custom:{installed filename}`.

The active theme/base monitor checks metadata every second and rereads/hashes only changes.
Edits do not require recompilation or restart. Choosing a theme releases the select's focus so wheel navigation resumes.
If an active personal file disappears, HIT falls back to Dark and reports it.
A rejected CSS edit preserves the previous valid applied personal CSS.
Delete selected uses a native confirmation dialog for the selected personal themes; deleting the active personal theme persists the fallback before removing the file.
Bundled confirmation dialogs use a transparent backdrop without background dimming or blur, while retaining modal focus and Cancel/Escape behavior.

## Minimal complete theme

```css
@layer sphere {
    :root {
        --hit-open-background: #17191e;
        --hit-idle-background: #101219;
        --hit-text: #f1f2f4;
        --hit-muted: #9299a5;
        --hit-accent: #d7dfec;
        --sphere-text: #f1f2f4;
        --hit-open-radius: 22px;
    }
    .sphere-action { border-radius: 8px; }
}

@layer menus {
    :root {
        --hit-menu-font: "Segoe UI Variable", "Segoe UI", sans-serif;
        --hit-menu-text: #e8ebf0;
        --hit-menu-muted: #9299a5;
    }
    h2 { font-weight: 500; }
    p { line-height: 1.6; }
}
```

Use one or both supported top-level layers. Inside sphere, `:root` becomes `.sphere-area`;
inside menus it becomes `.hit-menu-panel[data-hit-menu]`. Other selectors are prefixed to that area.
Do not manually copy the compiler prefix into a relative imported-theme selector: that can produce nested duplicate scopes.
The same layout style written as extension CSS uses plain CSS without these layer wrappers.

Supported rules inside the areas: full selectors, pseudo-elements, custom properties, @media, @supports,
@keyframes and @font-face. Animation/font names are rewritten to avoid theme-area collisions.
CSS nesting, @import, arbitrary top-level rules and extra layers are rejected.
The browser parses CSS; malformed declarations may be discarded by its CSS parser.
An unsupported structural rule causes an application error while retaining the last valid theme.

Personal theme URLs should be HTTPS/data URLs or installed fonts. Paths relative to the personal Themes folder are not served.
Package-local CSS is different: relative resources resolve through the extension asset provider.
Keep fonts/assets reasonably small and make network/font failure usable with local fallbacks.

## Cascade and public variables

Structural CSS loads first. The canonical Dark theme remains underneath custom/bundled overrides,
so partial themes inherit a complete appearance. Normal specificity still applies.
Use feature classes to override more specific rules instead of setting all elements with !important.

| Variable | Purpose |
| --- | --- |
| `--hit-font`, `--hit-mono-font` | UI and numeric font stacks |
| `--hit-background`, `--hit-open-background`, `--hit-idle-background` | General/open/closed surfaces |
| `--hit-text`, `--hit-muted`, `--hit-accent` | Shell text, support text and accent |
| `--sphere-text` | Shared title/action foreground |
| `--hit-open-radius` | Open surface radius |
| `--hit-menu-font`, `--hit-menu-text`, `--hit-menu-muted` | Menu typography/colors with shell fallbacks |
| `--sphere-ease`, `--sphere-settle` | Shared expansion/settling easing |
| `--hit-track-pointer` | Mascot pointer-follow request (1/0) |
| `--hit-eye-size`, `--hit-eye-gap` | Default mascot eye dimensions |
| `--hit-menu-padding-top`, `--hit-menu-idle-padding-top` | Normal/focused host top padding |
| `--hit-compact-padding-x`, `--hit-compact-padding-y` | Compact host padding |

Some variables are calculated runtime state: do not force `--hit-focus-progress`, natural/scaled measurement variables,
audio palette outputs or percentage progress to static values.
Complete per-feature references: [Audio themes](AUDIO-THEMES.md), [Clock themes](CLOCK-THEMES.md).

## Mascot and motion

The default mascot uses `::before`/`::after` as two white eyes and hides them while open.
Themes can change pseudo-elements, colors, shapes, transforms and animations within the existing DOM.
They cannot add arbitrary independently interactive DOM objects or run JavaScript as a CSS theme.

With `--hit-track-pointer: 1`, the Windows shell supplies up to 20 changed updates/second while collapsed:
`--hit-pointer-x/y` in -1..1 and `--hit-pointer-abs-x/y` in 0..1. Updates are rounded and CSS smooths movement.
Native collapsed tracking pauses with the menu open; non-Windows pointer events have only the available browser surface.
Use 0 to opt out. Respect actual HIT states rather than faking open/closed behavior with CSS.

Remove animations is authoritative: a separate final stylesheet disables animation, transitions and smooth scrolling,
including rules from a theme. It does not pause real timers, playback, pointer state or service updates.
Also honor prefers-reduced-motion in new styles. Avoid intense continuous animation inside content.

## Artwork and music colors have priority

All bundled and personal themes must preserve an enabled music cover/palette.
Do not hide `.sphere-audio-backdrop`/`.audio-artwork-wash`, force `--audio-art-*` values, or replace the foreground/accent
on `.audio-menu[data-audio-palette="true"]` with fixed branding colors.
Use `--audio-*` with `--hit-*` fallbacks. Global colors use `data-audio-theme="global"` on the sphere.
Turning the music mode off restores the selected theme; selecting OLED/Light does not switch music styling off.
The player, lyrics cover toggle and other-menu cover toggle are independent; [Audio themes](AUDIO-THEMES.md) explains their selectors.

## Design system

The complete shared reference is [Design](DESIGN.md); Settings screens and reset behavior are in [Settings](SETTINGS.md).
`wwwroot/css/hit-controls.css` normalizes buttons, switches, native selects, Back/forward rows and confirmations
across Audio, Clock, Settings, Extensions and the optional Chess extension when installed, using theme variables rather than independent fixed palettes.

HIT is a small desktop surface: concise information, quiet spacing and controls that do not compete with content.
Default direction: graphite #17191e, primary #e8ebf0-#f1f2f4, secondary #9299a5,
3-5% separators, cool off-white accent, Segoe UI Variable and tabular numeric fonts.
Titles are about 14 px/600, controls 12 px/500, support text 10-11 px. Open corners are about 22 px; controls 7-9 px.
Use quick response with long deceleration, never delayed interactivity. Transparent pin/close buttons have a rising light hover treatment.

Disclosures use title -> main control -> chevron on one line. They start collapsed.
Large fields mount only when expanded. A plain RAM label needs no subtitle.
Settings topics open dedicated views; Info stays at the sidebar bottom. Themes retains a full-width
combobox for quick activation, and Personalization has its own topic.
All lists retain scrolling with hidden scrollbars and subtle hints only at clipped edges.
Destructive actions use native dialogs with safe initial focus, cancel/Escape, an explicit second action and modal wheel/focus handling.

## Validation for a new theme

Test collapsed/hover/open/pinned/compact states, mouse tracking, modifier hints, focus and keyboard navigation.
Test Audio with neither, either and both spectrum/artwork enabled; lyrics cover on/off; global cover/color on/off;
Clock full/compact/dual/world settings; Settings disclosures and Extensions including confirmation.
Check Dark/OLED/Light-style contrast, large fonts, scaling, monitor edges, reduced motion and missing external assets.
Do not add page scroll, invisible clickable controls, content-selection regressions or resize oscillation.

### Personal background slideshow selectors

The custom background remains the `.sphere-custom-backdrop` child of the sphere, ordered below `.sphere-audio-backdrop`. Live slides use at most two `.custom-background-frame` children; the incoming `.custom-background-enter` supports `fade`, `slide`, `zoom` or `cut`. These presentation classes do not change the menu foreground layer. Embedded exports place keyframes inside `@layer sphere` so normal theme scoping renames them. GIF and WebP exports retain their original encoded animation; CSS export still has the total 1 MB limit.

Managed GIF assets obey Settings -> Images -> Allow GIF backgrounds. Disabling it suppresses their local asset URLs and embedded personal-image exports while retaining file references. Images of another supported format can still display in the selected sequence.
