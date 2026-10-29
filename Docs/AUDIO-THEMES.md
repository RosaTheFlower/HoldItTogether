# Audio theme reference

Use the global [theme contract](THEMES.md). Imported themes wrap these relative selectors in `@layer menus`.
Built-in structural CSS is `wwwroot/css/menus/HitMenuAudio.css`; shell-wide artwork is in `wwwroot/css/audio-shell.css`.
Behavior: `wwwroot/js/audio-player.js` and `wwwroot/js/audio-effects.js`. Styling must not invent media state or intercept real actions.

## Root, state and layout

Built-in menu ID: `HoldItTogether.Components.HitMenus.HitMenuAudio`.
The natural player width is 360 px before host margins/scale. The runtime viewport keeps the Lyrics view at the same current player dimensions.

| Selector/attribute | Meaning |
| --- | --- |
| `.audio-menu` | Player root |
| `.audio-menu.is-empty` | No compatible media |
| `.audio-menu.is-artwork` | Cover/color mode requested |
| `.audio-menu.is-lyrics` | Dedicated reading view |
| `.audio-menu.is-customizing` | Artwork appearance view |
| `.audio-menu.is-menu-settings` | Menu preferences |
| `[data-hit-view="effects"]` | Dedicated background effects screen |
| `[data-hit-view="colors"]` | Dedicated color-adjustment screen |
| `[data-hit-view="controls"]` | Hide buttons screen with independent Player/Lyrics switches |
| `.dynamic-sphere[data-audio-effects="true"]` | Runtime optical layers own filtering and transforms |
| `[data-audio-neutral-lyrics="true"]` | Theme-derived black/white reading surface |
| `.dynamic-sphere[data-audio-reading-hidden="true"]` | Temporary configuration-screen cover override; preserve it |
| `.audio-menu.has-spectrum` | Visualizer enabled |
| `.audio-menu.hide-source` | Source label preference off |
| `[data-hit-view="player/lyrics/appearance/settings"]` | Actual current view; values are alternatives, not the literal slash string |
| `[data-audio-palette="true"]` | Derived palette successfully applied |
| `[data-audio-lyrics-cover="true"]` | User permits cover behind lyrics |
| `.audio-player-content.is-obscured` | Underlying player not interactive while a subview is present |

Do not force is-obscured content visible/interactive. Preserve hidden/inert and semantic disabled states.
Runtime palette attributes are independent of classes rewritten by Blazor and must remain intact.

## Public selectors

| Area | Selectors |
| --- | --- |
| Metadata | `.audio-track`, `.audio-cover`, `.audio-metadata`, `.audio-source`, `.audio-status.is-playing`, `.audio-title`, `.audio-artist` |
| Timeline | `.audio-progress`, `.audio-timeline`, `.audio-times` |
| Transport | `.audio-toolbar`, `.audio-transport`, `.audio-control`, `.audio-play`, `.audio-shuffle`, `.audio-loop-one`, `.audio-glyph` |
| Secondary actions | `.audio-tools`, `.audio-tool-group`, `.audio-artwork-toggle`, `[aria-controls="audio-lyrics-view"]`, `.audio-visualizer-toggle` |
| Background | `.audio-scene`, `.audio-artwork-wash`, `.audio-artwork-veil`, `.audio-spectrum`, `.audio-spectrum canvas`; `.spectrum-bar` is a legacy hook |
| Overlays | `.audio-overlay-view`, `.audio-player-shell`, `.audio-view-back`, `.audio-error` |
| Lyrics | `.audio-lyrics-view`, `.lyrics-heading`, `.lyrics-provider`, `.lyrics-viewport`, `.lyrics-lines`, `.lyric-line`, `.lyrics-empty`, `.lyrics-back` |
| Lyrics metadata | `.lyrics-attribution`, `.lyrics-track-name`, `.lyrics-provider` |
| Edge timeline | `.sphere-lyrics-timeline`, an unscaled child of the sphere; style in `@layer sphere` |
| Current timing | `.lyric-line.is-current`, `.is-past`, `.is-future`, `[aria-current="true"]` |
| Artwork form | `.audio-appearance-view`, `.appearance-blur`, `.appearance-global`, `.appearance-suboption`, `.appearance-switch` |
| Menu form | `.settings-workspace[data-hit-settings-workspace]`, `.settings-sidebar`, `.settings-content`, `.audio-settings-view`, `.audio-setting-row` |
| Effects form | `.audio-effects-view`, `.audio-effect-row` |
| Color form / scrolling | `.audio-color-view`, `.audio-effects-scroll` |
| Button-hiding form | `.audio-controls-view` |
| Optical layers | `.audio-artwork-optics`, `.audio-artwork-copy`, `.audio-artwork-lens`, `.audio-artwork-chromatic`, `.audio-artwork-vignette`, `.audio-artwork-grain` |
| Scaling | `.hit-menu-scale`, `.hit-menu-viewport`, `.hit-menu-surface` |

The shared UI remodel replaces the former size disclosure's markup. Retarget custom form rules for
`.audio-size-disclosure`, `.audio-size-uniform` and `.audio-size-axis` to the Size topic's
`.settings-number`/`.settings-number-controls` rows. Stored size preferences and the Audio menu ID
are unchanged; settings render at 100% while the player/lyrics retain their configured scale.

Check the canonical CSS/component when adding a selector: classes describe the current markup, not a permanent arbitrary DOM extension API.

All ten visualizer styles share `.audio-spectrum canvas`, its computed color and the existing background
placement/opacity. Style and height are normalized user preferences applied by the canvas renderer,
not additional DOM layers. Height scales drawing amplitude within the same bounds; it does not
resize the player. The height range is 10-200%; vertical peaks can clip within that canvas.
Radial styles use circular geometry based on its smaller dimension. Filled spectrum was removed;
its persisted value maps to Smooth line. Preserve canvas dimensions and lifecycle across
styles; see [Audio](AUDIO.md#visualizer) for their stable preference values and presentation details.

`.audio-spectrum-layer` is an absolute, clipped, pointer-transparent sibling of `.audio-scene`.
It preserves scene extents and stays at z-index 0 in every mode, after the artwork scene in paint
order and below foreground UI. Compositing must never promote it above text, controls or forms.
The root publishes
`data-spectrum-style` (lowercase enum name) and `data-spectrum-blend` (lowercase composition name).
Do not apply isolation to the new layer: that would prevent blending against its local backdrop.
The audio root continues to isolate effects from other menus.

The renderer sets `--audio-visualizer-opacity` on `.audio-spectrum` from the user's 0-100% preference
(default 55%). This supersedes the earlier per-Wave/non-Normal opacity rules and works with artwork
palette colors. Root artwork variables remain JS-owned and are not overwritten by Razor opacity updates.
Size is 100-500%, distinct from Height (10-200%). Position Y is -100 to 100% of drawing height.
Flip X/Y are drawing reflections; they do not transform controls or move native input regions.
Every `data-spectrum-style` starting with `wave` uses the full scene area with no inset side gaps
and overscans horizontally at all sizes. Its smooth vertical fit keeps high zoom visible.
Scene insets interpolate with `--hit-focus-progress`; backing-buffer resize is repainted synchronously
to avoid a transparent frame during compaction. Preserve clipping, input passthrough and reduced motion.

## Variables

| Variable | Default / responsibility |
| --- | --- |
| `--audio-foreground` | Menu text with shell fallback |
| `--audio-muted` | Secondary text with shell fallback |
| `--audio-accent` | Active controls/accent |
| `--audio-surface` | Play/control contrast |
| `--audio-spectrum-opacity` | 0.11 without artwork |
| `--audio-spectrum-art-opacity` | 0.10 with artwork |
| `--audio-cover-opacity` | 0.32 |
| `--audio-cover-blur` | 8 px default; runtime preference can supply 0-40 px |
| `--audio-cover-zoom` | 1-1.8, runtime preference |
| `--audio-cover-saturation` | 0-2, runtime preference |
| `--audio-distortion-filter` | Legacy fallback filter hook; the effects engine owns SVG distortion on `.audio-artwork-optics` |
| `--hit-neutral-surface/text/muted` | Black/white lyrics surface tokens, defined on the sphere area |
| `--audio-lyric-size` | 20 px |
| `--audio-lyric-gap` | 18 px |
| `--audio-lyric-future-blur` | 0.6 px |
| `--audio-scene-inset-top` | 64 px |
| `--audio-scene-inset-side` | 24 px |
| `--audio-scene-inset-bottom` | 44 px |

JS supplies `--audio-artwork` and extracted `--audio-art-*` colors, including foreground, muted, accent, surface and background.
`--audio-progress` is a real playback percentage, `--lyric-distance` is derived from verse position,
and spectrum values represent real captured bands. Do not replace them with arbitrary animation values.
Natural size/scale variables under `--hit-menu-*` are maintained by the viewport; do not overwrite measurement ownership.

## Layer order and artwork preservation

The unscaled sphere backdrop owns the cover; the local scene retains the readability veil and transparent spectrum, with controls/lyrics above.
Legacy `.audio-artwork-wash` markup remains a styling hook but no longer duplicates the shell image.
With `data-audio-effects`, the backdrop is a container rather than a second background image.
Its optical child combines independent Gaussian/radial blur, SVG displacement/color balance and CSS color filters.
Enable color adjustments gates the complete color stack. Temperature/tint never enable turbulence; inactive optional layers are absent.
Lens/chromatic image layers inherit the same cover; vignette/grain stay above the optical filter.
Inline filter/transform values follow saved settings and the real OnBeat envelope. Preserve these ownership boundaries.
Animations stop when the cover is hidden, neutral lyrics are active, HIT closes or reduced motion applies.
Disposal removes the SVG filter, layers, observers, media-query listener and pending animation frame.
Background layers ignore pointer input. Spectrum and artwork must work separately and together with unchanged geometry.
Prefer adjusting public opacity variables over replacing the scene with an opaque generic background.

Local cover mode moves title/artist above the timeline and uses the cover as background rather than a side thumbnail.
In Lyrics, palette can remain without cover; `[data-audio-lyrics-cover="true"]` allows the image unless neutral reading is selected.
Do not use a generic `.is-lyrics` rule to suppress an explicitly enabled lyrics cover.
The same user blur value applies; do not force another blur with !important.

Global music styling lives on `.dynamic-sphere[data-audio-theme="global"]`.
Local music backgrounds also override the shell's solid theme surface, including Light, so white cannot wash out the cover.
`[data-audio-cover="true"] > .sphere-audio-backdrop` controls the shared cover surface.
Customize shell backgrounds through `@layer sphere`; controls/lyrics through `@layer menus`.
Do not neutralize global music variables with fixed theme colors. When music mode turns off, the selected base theme must reappear.

## Lyrics, idle focus and interaction hints

The viewport exposes `data-lyrics-layout` values `down`, `up`, `left`, `right`, `spotlight` and `wordrise`. `.lyrics-lines` retains chronological DOM order; reverse modes change flex order only. `.lyric-line.is-current/is-past/is-future` and original seek timestamps remain available. Horizontal modes use `--lyrics-viewport-width`; vertical modes keep `--lyrics-viewport-size`, both measured in unscaled layout coordinates. Preserve the bounded viewport, edge mask and current-line centering when styling these modes.

Only the active Word rise line contains `.lyric-word` spans. JavaScript owns their temporary transform/opacity animations, using `data-line-start` and `data-line-end`; do not override timing or insert simulated lyric content. `.lyrics-layout-picker`, `.lyrics-layout-options`, `.lyrics-layout-card` and `.lyrics-layout-preview` style the settings radio previews. SVG paths inherit the current theme/accent and use no raster assets or background animation.

The preview picker uses three equal columns and two rows of outlined 12px-radius cards, with an accent border/fill and `.lyrics-layout-selected` checkmark for the selected choice. The native radios retain keyboard/focus semantics; the checkmark is decorative. Hover is subtle and selection does not change card dimensions. There is one outer card boundary rather than an additional frame around each SVG.

The current verse follows real timestamps and is highlighted/scrolled by JS. Click-to-seek depends on CanSeek.
Keep `.lyrics-viewport` bounded/local-scroll and its text/button focus legible.
Do not use pseudo-elements to replace real song/verse text, fake timing or create extra controls.

The shared pointer-departure delay (five seconds by default, 0-10 seconds in Settings) hides the header/secondary controls,
including the Lyrics Back button, but leaves the lyric source and main actions usable. Hover anywhere over HIT prevents compaction.
Audio settings -> Hide buttons independently opts Player and Lyrics into this policy; configuration screens never compact.
The player contracts top/tool space; Lyrics uses that space for reading and compensates for scale Y to keep physical window height unchanged.
`--hit-menu-padding-top` (normally 64 px), `--hit-menu-idle-padding-top` (normally 24 px) and shared focus progress must stay coordinated.
Do not undo hidden/inert controls with CSS. Transitions follow the shared [menu contract](MENUS.md).

Ctrl marks `.audio-title` and `.lyrics-provider` for their validated actions. Alt marks the configurable title/artwork button.
Do not put a permanently clickable cursor on disabled or modifier-only actions without the corresponding state.

## Example: typography and restrained spectrum

```css
@layer menus {
    .audio-menu {
        --audio-lyric-size: 22px;
        --audio-lyric-gap: 20px;
        --audio-spectrum-opacity: 0.08;
        --audio-spectrum-art-opacity: 0.07;
    }
    .audio-title { font-weight: 600; }
    .lyrics-provider { letter-spacing: 0.02em; }
    .audio-control { border-radius: 9px; }
}
```

Test with and without media/capabilities, all four view states, arbitrary cover brightness,
visualizer alone/cover alone/both, lyrics cover and neutral mode on/off, global cover/colors on/off, scales 25/100/200,
idle reversal, Ctrl/Alt, reduced motion and keyboard focus. See [Audio](AUDIO.md) for supported behavior.

The host sets --audio-cover-opacity from AudioArtworkBackgroundOpacity (0?100%, normalized at persistence). It scales the complete artwork/effect layer, leaving the custom background and foreground UI independent.

The scene sets data-audio-background-blend on the sphere from AudioArtworkBlendWithBackground. With active cover and blend disabled, the personal backdrop is kept mounted but transitions to zero opacity. data-audio-reading-hidden, data-audio-cover-hiding and neutral Lyrics bypass suppression so the background can return during cover removal. Reduced motion skips the transition.

AudioEffectSlider delegates to shared SettingRange. The public audio-effect-row class remains on its root, which is now an inline configuration row rather than a label wrapper; target descendants instead of requiring a label element. Sliders use small inline labels and explicit editable values/units, with help in tooltips.
