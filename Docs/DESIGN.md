# HIT design system

This is the shared visual and interaction reference for built-in menus and `.hitmenu` modules.
It consolidates the existing Clock controls, Settings rows, sphere chrome, public theme variables and Chess interactions.
Chess references in this guide describe the separately installed [Chess extension](CHESS.md), not a built-in HIT menu. Sharing the host's design and animation rules does not make a module part of HIT.
Use it with [Menus](MENUS.md) for host behavior, [Themes](THEMES.md) for CSS compilation,
and [Code conventions](CODE-CONVENTIONS.md) for implementation and ownership.

## Principles

HIT is a small desktop surface. Keep frequent information close to its controls, use quiet secondary text,
and give each screen one purpose. All application-owned labels, accessible names, comments, identifiers and documentation use English.
Track titles, lyrics, imported names and user-entered content retain their original language.

The host renders one title, pin, close button and navigation dots. A module never duplicates them.
Header actions stay in this order: temporary artwork override, pin, close. There is no compact button.
Only pinned Audio Player/Lyrics, the Chess board and the main Clock readout can compact after the shared pointer-departure delay.
The default is five seconds; Settings configures 0-10 seconds. General Settings, all forms/subviews and unrelated modules never compact.
Audio settings -> Hide buttons has independent Player/Lyrics switches that opt each page into this behavior.
Clock digits and the Chess board resize continuously with their enclosing HIT surface over 820 ms, using the same easing and retained corner/center anchor in both directions. Avoid an immediate content-size jump followed by an empty shrinking container. Remove animations and reduced-motion preferences disable this interpolation.
Settings and reading screens remain subviews of their owning menu, without additional navigation dots.
Every subview has a small Back control, supports Escape and follows the documented right-click return behavior.

## Theme tokens and contrast

Every menu inherits its active theme. Feature-specific variables may alias these shared tokens;
they must not introduce an independent fixed palette for settings or confirmation screens.

| Token | Role |
| --- | --- |
| `--hit-menu-font`, falling back to `--hit-font` | Segoe UI Variable / system sans-serif |
| `--hit-mono-font` | Tabular times, values and counters |
| `--hit-menu-text`, falling back to `--hit-text` | Primary text |
| `--hit-menu-muted`, falling back to `--hit-muted` | Secondary information and inactive controls |
| `--hit-accent` | Selected/active state and primary action |
| `--hit-open-background` | Menu and confirmation surface |
| `--hit-error` | Errors/destructive emphasis with a contrasting fallback |
| `--hit-open-radius` | Shared outer shape |

Derive borders and quiet hover fills with `color-mix` from foreground/muted tokens, rather than white-only overlays.
Dark, true-black OLED and Light must retain legible text, native selects, switches and focus rings.
Music-derived colors remain authoritative when enabled. A neutral lyrics surface can independently use black or white for readability.

## Measurements and typography

| Element | Reference |
| --- | --- |
| Shared title | 14 px, weight 600 |
| Setting label | 12 px, weight 500 |
| Buttons / values | 11–12 px |
| Supporting text / Back | 10–11 px, muted |
| Setting row | At least 44 px, 8–12 px vertical padding |
| Standard button | At least 32 px high, 8 px corners, 7 × 13 px padding |
| Switch | 34 × 20 px, 12 px thumb |
| Control corners | 7–9 px |
| Natural Audio / Clock / Chess width | 360 px before host padding and scaling |
| Settings / management width | 440–480 px as needed |

Use flex/grid and natural content dimensions. Long text truncates where it is metadata and wraps where it explains an action/error.
Do not add arbitrary cards around every row, repeated titles, implementation jargon or hover-only required controls.

## Shared controls

Use `.hit-setting-row`, `.hit-button`, `.hit-switch`, `.hit-back`, `.hit-link-row` and `.hit-confirm`
from the public shared stylesheet for new screens. Existing feature classes remain useful theme hooks.
The shared rules also normalize older Clock, Audio, Settings, Extensions and Chess controls.

- Setting rows place the label on the left and a switch/select/value on the right, with a faint bottom separator.
- Sliders have a label and a stable-width tabular value. Wheel changes do not navigate to another menu.
- Buttons use quiet muted text and a subtle foreground-derived border/fill. Primary actions use accent with contrasting surface text.
- Destructive actions remain clearly named and use the same geometry; never rely on red alone to communicate deletion.
- Forward rows fill the available width and show a small right chevron. Back is a small muted left-arrow link.
- Disclosures retain title → main control → chevron, and mount their expensive content only while expanded.
- Native selects release focus after selection where it would otherwise trap wheel navigation.

## Confirmation and data deletion

Use a native modal dialog with the same fonts, background tokens, corners and buttons as Settings.
Its backdrop is transparent: no dimming, background fade or blur. Keep focus within the dialog and focus Cancel initially.
Escape, Cancel and right-click cancel safely. An explicit confirming action performs the operation.

Name the affected object or scope. New game, theme removal and extension removal use this same pattern.
Chess Reset score uses the same confirmation and resets only Win/Loss, preserving the current position and options.
Data management has a separate choice screen: reset settings while keeping installed themes/extensions,
or delete all personal themes, extensions and saved data. Each choice requires its own second confirmation.
Bundled themes and built-in modules are never deleted; disabled trash icons may communicate this protection.

## Lists, selection, scrolling and dragging

No visible scrollbar is used. Wheel, touchpad and keyboard scrolling remain available in bounded regions.
Only edges with hidden content show a small surface-to-transparent gradient or equivalent mask.
The hint must not intercept input or fade the entire screen.

Theme lists distinguish selection from activation. Single click selects; Ctrl+click toggles multiple selections;
double click or Enter activates. The current theme is separately marked. Delete applies only to selected personal themes and confirms first.
Keep a theme combobox in Settings for quick activation; the Themes management screen is the final Settings row.

Extension drag handles use real pointer dragging, a six-pixel threshold and visible insertion feedback.
Keep Up/Down buttons for keyboard access. Settings remains last and cannot be disabled or reordered.
Dropping `.css` or `.hitmenu` anywhere on HIT imports it and opens the appropriate management screen.

## Motion, geometry and scale

Interactions respond immediately. Use the sphere's shared decelerating easing for size changes,
reserve the final native canvas before opening and fade content without translating or blurring it.
Never measure unstyled extension markup or repeatedly move the native window during opening.
Keep the open surface opaque and retain its largest native canvas until closing. Switching menu panels must not fade to empty content.
Configuration screens offer a temporary cover toggle beside Pin/Close; it is never saved and resets on leaving the screen.

Chess moves take 250 ms, hide the destination piece before animation starts and include both castling pieces.
Block overlapping moves until landing. Captured pieces share one strip below the board; history runs inline above it.
Full boards have square corners; the smaller compact board alone has rounded corners.

Audio supports independent X/Y scaling from 25–200%, including settings and lyrics.
Measure natural dimensions before transforms. Lyrics scrolling uses layout coordinates, not scaled screen coordinates.
Artwork backgrounds fill the actual sphere independently of the content transform.
The optional lyrics timeline follows the sphere's bottom edge with a configurable thin track and no transport buttons.

Respect both Remove animations and `prefers-reduced-motion`. Skip presentation motion while preserving real clocks,
audio spectrum values, playback position and game state.

## Information architecture

Settings keeps the common switches, followed by Scroll speed, Info, Extensions and Themes (last).
Scroll speed owns Cooldown, Sensitivity and Pause. Info owns read-only RAM/version/counts,
update checking, the full-width GitHub link and data reset/deletion.
Audio separates compact artwork choices from a dedicated Background effects screen.
The Clock remains the visual reference for compact action buttons; its explanatory pinned-mode paragraph is unnecessary in the UI.

Independent module projects, their tests and the SDK live under `Modules/`. Built-in menus stay in `Components/HitMenus`,
with their supporting components/services in the existing feature directories. Shared public assets remain under `wwwroot`;
extension-local distributable assets remain inside their module.

## Review checklist

Check all three bundled themes, music colors on/off, keyboard/focus and tooltip-off use, modal cancel/confirm,
long names, empty/populated/overflow lists, 25/100/200% Audio size, full/compact states,
stationary hover, reduced motion and screen-edge anchoring. Verify no page scroll, visible scrollbar,
resize loop, destination flash, inaccessible action or background polling from an inactive view.
Use [Testing and release](TESTING-RELEASE.md) for build, package and browser checks.
