---
name: hit-themes
description: "Create or change HIT public CSS, theme imports, personalization, background images, profile export or theme-derived icons. Use for styling contracts and appearance persistence; use hit-ui for interaction behavior and feature skills for media or clock logic."
---

# HIT themes and personalization workflow

## Select the reference

Read [Themes](../../../Docs/THEMES.md) and [Design](../../../Docs/DESIGN.md).
For the editor, profiles, backgrounds or export, read [Settings](../../../Docs/SETTINGS.md).
For player/artwork/effects selectors, read [Audio themes](../../../Docs/AUDIO-THEMES.md).
For clock selectors, read [Clock themes](../../../Docs/CLOCK-THEMES.md).
Read [Menus](../../../Docs/MENUS.md) before changing sizing, scaling or compact layouts.

## Change presentation without breaking the contract

1. Identify whether the request changes a bundled theme, feature CSS, user overrides or
   export/import. Trace that layer's precedence and current public selectors before editing.
   Keep theme identifiers independent of visible names and personal profile identities.
2. Scope feature selectors to the owning menu, including grouped rules and nested media rules.
   Use public CSS files and documented variables/fallbacks. Do not use `.razor.css`, generic
   global feature selectors, inline brand palettes or edits to generated `menus.css`.
3. Preserve Dark/OLED/Light and independent local/global music palette/artwork choices.
   Respect the documented priority of music colors over user colors. Keep optical and color
   effects independent, avoid accidental opacity/filter inheritance and honor reduced motion.
4. For personalization, retain immediate preview and debounced persistence, flush pending saves
   when leaving, and preserve session Undo/reset semantics. Do not reintroduce Apply/Cancel.
   Normalize colors, sizes and enum values at the service boundary, including imported data.
5. For background images, enforce the documented type/signature, byte, dimension and path limits
   in the owning layers. Use validated local assets, not arbitrary filesystem or remote access.
   Preserve independent opacity/fit settings and portable CSS export/reimport with embedded data.
6. Preserve the last valid imported theme when live edits fail. Keep protected bundled themes,
   user files and profile copies distinct; validate export limits and sanitize public CSS values.
   Follow existing asset cleanup ownership instead of deleting files on every preview change.
7. For icon work, preserve the editable SVG and theme-derived native/tray path. Rebuild the
   embedded executable icon only through the documented build tool when its source changes;
   do not rewrite an executable to apply a live theme.

## Verify the changed surface

Follow [Testing and release](../../../Docs/TESTING-RELEASE.md). Select `appearance.mjs`,
`app-icon.mjs`, `menus.mjs` or the relevant feature test. Appearance browser checks depend on
CSS emitted by the host harness; create the fixture before running them.

Check computed styles and visible output across all bundled modes, music on/off, reduced motion,
compact/full and supported scaling where relevant. For editor changes, verify reopen/restart,
pending-save flush, Undo, independent profile copies and export/reimport. Test malformed assets
with disposable state. Update the relevant selector/token table or settings contract in `Docs/`.

## Current contract checkpoints

Treat the Images topic as the managed-image policy surface: configurable finite/unlimited import size and a global GIF permission apply independently of visual profiles. Keep gallery removal two-stage, preserve assets referenced by Undo/profiles, and distinguish exclusive artwork from opt-in blending and its independent opacity. Do not flatten functional color swatches, selected highlights or game-board states when making fields transparent.
