---
name: hit-ui
description: "Implement or review HIT menu views, settings forms, navigation, mouse and keyboard gestures, accessibility, shared controls or built-in localization. Use for Razor/DOM interaction work; pair with hit-window for native geometry and hit-themes for theme or personalization contracts."
---

# HIT menu and interaction workflow

## Read the relevant contracts

Read [Menus](../../../Docs/MENUS.md) and [Design](../../../Docs/DESIGN.md).
For preferences or title gestures, read [Settings](../../../Docs/SETTINGS.md).
For a built-in entry, read [Internal menus](../../../Docs/INTERNAL-MENUS.md).
For new labels, read [Localization](../../../Docs/LOCALIZATION.md).
Use [Architecture](../../../Docs/ARCHITECTURE.md) when changing host/view ownership.

## Implement within the existing host

1. Identify the entry, supporting views, context and explicit main/detail/settings state.
   Keep one catalog entry per menu. Use stable IDs and keys so reorder preserves view state.
   Preserve Settings as the protected final menu.
2. Reuse host chrome and shared controls such as `HitDisclosure`, `HitScrollRegion` and
   `.hit-setting-row`, `.hit-button`, `.hit-switch`, `.hit-back` and `.hit-link-row`.
   Keep expensive disclosures unmounted until expanded; scroll locally rather than growing
   a page beyond the native window. Check actual theme markup before choosing selectors.
3. Resolve navigation at the current view depth. M2 and an enabled side-button Back return
   one configuration level before leaving a menu. Honor each side button's configured Page history, HIT menus or legacy behavior; use stable page IDs through IHitMenuPages when available. Block wheel menu switching where the active settings/editing contract requires it.
4. For title M2/optional M1 hold, use the shared gesture mechanism and persisted preference. Never start a held-M1 pill inside configuration; Lyrics reading titles target Lyrics settings.
   Cancel on interruption and suppress the resulting click/context event after a consumed hold.
   One gesture must cause one action; do not attach competing return handlers to nested views.
   Distinguish the implemented hold feedback from future design proposals in the guides.
5. Preserve hover, keyboard, drag and modal guards for automatic compaction/opacity.
   Only documented views participate in compaction; configuration/forms must remain usable.
   Keep compact menus distinct from the fully collapsed sphere and resource sleep.
6. Add semantic buttons, independent accessible names, visible focus and keyboard alternatives.
   Keep controls usable with tooltips disabled. Use the documented modal confirmation with
   safe initial focus and cancellation for destructive operations.
7. Add English source messages through `Text.Get`/`Text.Format` and `Resources/en.json`.
   Preserve numbered placeholders, stable IDs and user-provided text. Do not introduce a
   translation-package format or SDK localization API as though it already exists.

## Apply the shared settings workspace

Use the current Settings workspace in [Design](../../../Docs/DESIGN.md) as the baseline when
creating or remodeling configuration screens. Reuse the shared workspace/control implementation;
do not fork the layout or build another select popup for each feature.

- Group preferences into visible left-side topics with a bounded right options pane. Configuration
  must not be reachable only through M2, an unlabeled icon or a hidden subview. Keep existing M2 and
  held-title gestures as shortcuts to pages also exposed in navigation. Keep all dependent options
  visible and disabled when unavailable. A lazy per-item editor is different from a hidden menu.
- Publish each page's title and use the host's Back chevron for single-level return. Do not duplicate
  the host controls or add another Back-to-feature button inside the workspace. Preserve the visible
  page when navigating between menus and stop view work when hidden.
- Use transparent fields, subtle permitted borders, full-width/equally shared action rows, darker slider thumbs and the moving sidebar highlight from Design. Use full-row switch labels, explanatory text controlled by the description preference, tooltip
  help for actions and compact inline slider rows (small label, track, editable value, unit) with no separate heading or spinner arrows. Use SettingRange for built-in sliders and tooltip help instead of stacked slider descriptions. Preserve
  service validation, immediate application and existing save/reset/confirmation boundaries.
- Use the shared rounded wheel combobox: three visible rows, active choice centered, wheel/keyboard
  selection, no empty end padding, and menu-wheel blocking while open. Preserve Escape restoration,
  reduced-motion behavior, opening/closing cleanup and teardown. Use Clock-style equal-width
  highlighted segments for short mutually exclusive choices; retain native keyboard semantics.
- Let the right pane own scrolling; retain wheel at both ends of actually scrollable configuration regions; only clean non-scrollable areas release it. Remove unbounded nested overflow/overscroll traps. Reuse
  `HitSpinner` for pending work and keep action feedback from shifting the layout.
- Independent extensions use the documented declarative workspace markup and public host controls;
  keep their projects SDK-only. Do not add an executable reference to reuse a Razor component.

Technical contracts, selector details and dimensions belong in the linked guides, not duplicate
skill reference files. Apply these conventions to the requested screens without changing their
domain behavior or adding unrelated options.

## Verify and document

Use [Testing and release](../../../Docs/TESTING-RELEASE.md) to prepare the host fixture and
select `menus.mjs`, `navigation.mjs`, `mouse-navigation.mjs`, `idle-input.mjs` or
`idle-opacity.mjs` according to the affected behavior. Do not run unrelated suites by default.
Use `settings-design.mjs` for the shared picker/input lifecycle and `shared-settings.mjs` for
cross-menu workspace changes; the latter requires both host and Chess fixtures.

Check Dark/OLED/Light, long labels, tooltip-off use, keyboard focus, nested Back, rapid/repeated
gestures and reduced motion. Verify hidden/reopened views retain appropriate state and dispose
their listeners when disabled. For layout affecting native bounds, also use `hit-window`.
Update the canonical interaction/settings guide and report any remaining interactive checks.

## Current contract checkpoints

Use the current shared inline slider row (small label, track, editable value, unit), thin outlines on typed fields except slider values, and full/equally shared action rows. Infinite menu wraps top-level navigation only; preserve independent page-history modes and the 50-visit session bound. Honor the independent menu-entry toggle, global Remove animations and reduced motion.
