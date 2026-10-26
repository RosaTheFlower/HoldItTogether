# Audio theme reference

Use the global [theme contract](THEMES.md). Imported themes wrap these relative selectors in `@layer menus`.
Built-in structural CSS is `wwwroot/css/menus/HitMenuAudio.css`; shell-wide artwork is in `wwwroot/css/audio-shell.css`.
Behavior: `wwwroot/js/audio-player.js`. Styling must not invent media state or intercept real actions.

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
| Background | `.audio-scene`, `.audio-artwork-wash`, `.audio-artwork-veil`, `.audio-spectrum`, `.spectrum-bar` |
| Overlays | `.audio-overlay-view`, `.audio-player-shell`, `.audio-view-back`, `.audio-error` |
| Lyrics | `.audio-lyrics-view`, `.lyrics-heading`, `.lyrics-provider`, `.lyrics-viewport`, `.lyrics-lines`, `.lyric-line`, `.lyrics-empty`, `.lyrics-back` |
| Current timing | `.lyric-line.is-current`, `.is-past`, `.is-future`, `[aria-current="true"]` |
| Artwork form | `.audio-appearance-view`, `.appearance-blur`, `.appearance-global`, `.appearance-suboption`, `.appearance-switch` |
| Menu form | `.audio-settings-view`, `.audio-size-disclosure`, `.audio-size-uniform`, `.audio-size-axis`, `.audio-setting-row` |
| Scaling | `.hit-menu-scale`, `.hit-menu-viewport`, `.hit-menu-surface` |

Check the canonical CSS/component when adding a selector: classes describe the current markup, not a permanent arbitrary DOM extension API.

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

The scene places cover wash behind its readability veil, then the transparent spectrum, with controls/lyrics above.
Background layers ignore pointer input. Spectrum and artwork must work separately and together with unchanged geometry.
Prefer adjusting public opacity variables over replacing the scene with an opaque generic background.

Local cover mode moves title/artist above the timeline and uses the cover as background rather than a side thumbnail.
In Lyrics, palette can remain without cover; `[data-audio-lyrics-cover="true"]` allows the image.
Do not use a generic `.is-lyrics` rule to suppress an explicitly enabled lyrics cover.
The same user blur value applies; do not force another blur with !important.

Global music styling lives on `.dynamic-sphere[data-audio-theme="global"]`.
`[data-audio-cover="true"] > .sphere-audio-backdrop` controls the shared cover surface.
Customize shell backgrounds through `@layer sphere`; controls/lyrics through `@layer menus`.
Do not neutralize global music variables with fixed theme colors. When music mode turns off, the selected base theme must reappear.

## Lyrics, idle focus and interaction hints

The current verse follows real timestamps and is highlighted/scrolled by JS. Click-to-seek depends on CanSeek.
Keep `.lyrics-viewport` bounded/local-scroll and its text/button focus legible.
Do not use pseudo-elements to replace real song/verse text, fake timing or create extra controls.

Five-second pinned inactivity hides the header/secondary controls, including the Lyrics Back button, but leaves the lyric source and main actions usable.
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
visualizer alone/cover alone/both, lyrics cover on/off, global cover/colors on/off, scales 75/100/200,
idle reversal, Ctrl/Alt, reduced motion and keyboard focus. See [Audio](AUDIO.md) for supported behavior.
