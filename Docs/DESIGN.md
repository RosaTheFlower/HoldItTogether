# HIT design system

This is the shared visual and interaction reference for built-in menus and `.hitmenu` modules.
It consolidates the existing Clock controls, Settings rows, sphere chrome, public theme variables and Chess interactions.
Chess references in this guide describe the separately installed [Chess extension](CHESS.md), not a built-in HIT menu. Sharing the host's design and animation rules does not make a module part of HIT.
Use it with [Menus](MENUS.md) for host behavior, [Themes](THEMES.md) for CSS compilation,
and [Code conventions](CODE-CONVENTIONS.md) for implementation and ownership.

## Principles

Host-owned and Notes configuration menus use a compact 610 by 420 CSS-pixel workspace with a left
topic sidebar, no vertical divider and a bounded right content scroller. Chess 1.1.0 adopts this workspace; older installed versions retain their compact settings UI. Info is pinned to the sidebar bottom. Themes and Personalization
have separate topics. Wheel input outside the right pane can navigate host menus. All preference rows
include explanatory text, controlled by Show option descriptions; action buttons use tooltips.
Dependent settings remain visible but disabled. Labels toggle switches over their complete row;
sliders have plain numeric entry without a visible input box or spinner arrows. Settings comboboxes
use a rounded continuous wheel listbox with up to three visible rows: the active row and closed label are centered, wheel/arrow input
applies the next enabled option, and the rows wrap without blank end padding. Enter/click confirms;
Escape restores the opening selection. While open, the picker blocks wheel menu navigation across
the interface. Its root marker is `data-hit-select-open="true"`, removed on dismissal or teardown.
The closed control has a 16-pixel radius and its popup an 18-pixel radius. Popup opening/closing
uses 180/140 ms fade and slight transform effects; closing content is inert. Reduced motion and
Remove animations disable these effects. Teardown and rapid reopening release pending closing DOM.
Title presentation is a themed, full-width horizontal example-based radio group with three equally
sized choices: name, name/icon, icon. Follow Clock's three-mode segments: a quiet rounded track,
inset selected highlight and no visible radio dots, preserving native keyboard and focus behavior.
The Settings icon is a shared eight-tooth outlined gear with a circular center, rendered by
`HitSettingsIcon` in the header and title previews. All modes
retain accessible names. Future feature screens will use a bottom-pinned **Extras** entry in the
same role as Info; Extras is recorded only and is not implemented in this delivery.

Built-in feature menus reuse `Components/Settings/SettingsWorkspace.razor`. Its public structural
CSS is `wwwroot/css/menus/SharedSettings.css`, included in generated `menus.css`. Independent
extensions opt into the same layout with `.settings-workspace[data-hit-settings-workspace]`, a
`.settings-sidebar` containing `.settings-topic` buttons with `aria-current="page"`, and one bounded
`.settings-content[data-hit-scroll]` pane. The host enhances their native bound selects/ranges through
`settings-controls.js`; extensions retain C# event handlers and SDK-only project references. No
per-extension selector implementation or application-project reference is needed.
Give each workspace topic a `data-hit-view` marker so screen-local artwork overrides reset when
switching topics. Built-in topic keys remain untranslated identities; only their visible labels
pass through localization.

`HitSettingsControls` discovers marked workspaces once, disconnects removed roots and preserves
retained hidden views. `data-hit-setting-descriptions` on the shared sphere supplies the global
description preference to independent workspaces. Picker visibility/disposal releases wheel locks,
animations, listeners and transient DOM. Only the options pane owns overflow; nested feature panels
must not trap wheel input. M2 shortcuts can target existing topics, but no preference page is
reachable exclusively through an object gesture. Feature-specific subtopics return to General,
then to their owning main view; Settings -> General remains the protected root.

HIT is a small desktop surface. Keep frequent information close to its controls, use quiet secondary text,
and give each screen one purpose. All application-owned labels, accessible names, comments, identifiers and documentation use English.
Track titles, lyrics, imported names and user-entered content retain their original language.

The host renders one title, an optional Back chevron immediately before it, pin, close and navigation
dots. `.sphere-heading` groups the chevron and title; `.sphere-action-back` follows the theme's
header-action and focus styles. The chevron appears only when the menu can return one level.
A module never duplicates this shared chrome.
Header actions stay in this order: temporary artwork override, pin, close. The artwork override can
be removed through Appearance or Personalization. There is no compact button.
Only pinned Audio Player/Lyrics, the Chess board and the main Clock readout can compact after the shared pointer-departure delay.
The default is five seconds; Settings configures 0-10 seconds. General Settings, all forms/subviews and unrelated modules never compact.
Audio settings -> Hide buttons has independent Player/Lyrics switches that opt each page into this behavior.
Clock digits and the Chess board resize continuously with their enclosing HIT surface over 820 ms, using the same easing and retained corner/center anchor in both directions. Avoid an immediate content-size jump followed by an empty shrinking container. Remove animations and reduced-motion preferences disable this interpolation.
Settings and reading screens remain subviews of their owning menu, without additional navigation dots.
Feature subviews have a small Back control, support Escape and follow the documented right-click return
behavior. The Settings workspace uses its topic sidebar and the shared header instead of repeating
Back to Settings. Its topic and Info detail views publish their own header titles. Info -> Performance
has no inline Info button; the header chevron returns to Info. Configuration panels include h2
headings, including Info, Extensions and Window placement. Install .hitmenu sits at the right of
the Extensions toolbar.

Use the shared `HitSpinner` (`.hit-spinner`) for asynchronous loading/checking/importing/saving
states, with a localized accessible label. The 16-pixel ring inherits the current theme color;
reduced motion makes it static. Keep the action's accessible name and dimensions stable while busy.
Upload/install labels can use `.hit-busy-action` with `.hit-busy-label` and an overlaid
`.hit-busy-indicator`, preserving the label's intrinsic size while `aria-busy="true"`.
The initial application bootstrap keeps its sphere loading artwork. Info's `.info-update-feedback`
opens above its action row as an overlay, without displacing controls.

## Theme tokens and contrast

The editable application logo is `wwwroot/icons/hit.svg`: the sphere's circular face and two eyes, preserving the CSS face's proportions.
The window and tray icons are rasterized from that SVG with the active theme's collapsed-sphere background and eye colors, and refresh on theme selection or valid live CSS edits.
`Resources/hit.ico` contains 16–256 px default artwork for the executable and the initial native window; rebuild it with `node Tools/Build-AppIcon.mjs` after changing the SVG.
Explorer/executable file icons use this embedded default; running window/tray icons follow the live theme. No executable is rewritten on a theme change.

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
Dark, true-black OLED and Light must retain legible text, shared wheel selectors, switches and focus rings.
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

Confirmation actions share one flex row with equal widths and single-line labels. Do not combine zero-width flex items with a grid container; the host enforces this layout even when an older extension supplies its own dialog styles.

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
Keep a full-width theme combobox in Settings -> Themes for quick activation.

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

Settings groups preferences into sidebar topics, with separate Themes and Personalization and Info
at the bottom. Scroll owns Cooldown, Sensitivity and Pause. Info owns read-only version/counts,
update checking, a Changelog reading view, a Performance detail view, GitHub and data reset/deletion.
Performance owns RAM, CPU and local diagnostics, with display work stopped on exit. Changelog, update checking
and GitHub share one action row.
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

Title holds use an accent-fill pill with an inverse-color clipped title overlay. Both progress layers follow the configured hold duration and are removed on cancellation/completion. Reduced motion uses a static held state. Chess deliberately keeps its previous compact form and uses a knight header icon.

## Quiet controls and action rows

Keep editable slider values and other text/number fields transparent, without decorative colored boxes. Color swatches keep their actual color but have no outer decorative border. Closed comboboxes and action buttons use transparent surfaces; subtle theme-derived borders are allowed, matching the former combobox/button outline. Retain keyboard focus outlines, selection highlights, switches and game-board colors as functional states. Picker flyouts retain an opaque theme surface for readable choices.

Unless a specific layout has been requested, standalone action buttons fill the available row. Buttons sharing a row divide its full width equally, with only the standard inter-button gap. Do not leave unused trailing space. Explicit exceptions include header/player controls, icons, sidebar topics, segmented choices, and the Background style/Remove all images group. These rules cover host menus, Notes and the retained Chess form without replacing its board layout.

Menu-title pills have 2 px extra padding. Their inverse text overlay uses the same inset. Sidebar selection uses a single theme-colored moving highlight; reduced motion makes the position change immediate.

Settings slider tracks use the accent color for their filled portion and a transparent unfilled portion bounded by a thin theme-derived outline. The circular thumb uses a darker accent mix (65% accent and 35% black). Editable values remain transparent; the player playback timeline retains its dedicated presentation.

## Unified configuration rows and entrances

Settings, AudioConfig and ClockConfig share the same row typography, switch behavior, fields, comboboxes and action layouts. Every built-in configuration slider uses SettingRange, including AudioEffectSlider (a compatibility wrapper), Personalization, placement and idle-opacity settings. A slider has no separate heading or stacked control block: one small muted label sits to its left, followed by the track, editable numeric value and unit. Long help lives in the label tooltip, retaining the visible label when tooltips are disabled. Action buttons retain full-row/equal-share sizing; explicit icon, player and preview controls remain compact.

Menu entrance animations add a 180 ms opacity/4 px reveal when a menu becomes visible or a stable data-hit-view marker changes. Ordinary rerenders never replay it. New visits cancel any previous entrance. The Menu entrance animations preference disables these effects independently; Remove animations and system reduced motion also suppress and cancel them. Observers and animation handles are released by HitSettingsControls.

Editable text/search/number fields and textareas have the same thin theme-derived outline as other interactive controls. Slider numeric values remain borderless. Clock primary actions on transparent surfaces use the Clock foreground, including Timer and Pomodoro. Title hold pills use 2 px vertical and 8 px horizontal padding, mirrored exactly by the inverse-text layer.

Configuration scroll regions with actual overflow retain wheel ownership even at their first/last position. Non-scrollable blank areas can navigate HIT. Info actions use a bottom-aligned footer; update feedback is muted inline text above it. Configuration titles do not offer held-M1 progress pills.
