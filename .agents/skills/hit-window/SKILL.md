---
name: hit-window
description: "Diagnose or change HIT native window stacking, FORCE, fullscreen focus, taskbar/work-area placement, DPI/monitor coordinates, hit regions, drag anchoring or shell resize/compact animation. Use for Windows/Photino geometry; pair with hit-ui for view interactions."
---

# HIT native window workflow

## Read the geometry and interaction contracts

Read [Architecture](../../../Docs/ARCHITECTURE.md) and [Menus](../../../Docs/MENUS.md).
Read [Settings](../../../Docs/SETTINGS.md) for topmost, FORCE and saved-position policies,
[Design](../../../Docs/DESIGN.md) for animated transitions, and
[Troubleshooting](../../../Docs/TROUBLESHOOTING.md) for known platform limits.

## Establish the failing native state

Record normal versus FORCE, windowed/borderless/exclusive fullscreen, focus/foreground changes,
visible versus collapsed state, monitor work area, taskbar edge and DPI. Separate native bounds,
visible content bounds and retained transparent backing canvas. Locate the owning native service
and shell bridge before changing CSS or adding periodic recovery.

## Preserve native invariants

1. Keep normal topmost reconciliation independent of FORCE. A normal desktop window should not
   require FORCE for documented stacking. Recovery must remain bounded and stop when disabled,
   disposing or in the fullscreen context where the policy suspends it.
2. Never count focusing HIT, minimizing the game or generating an Alt-Tab-like interruption as
   fullscreen overlay success. Preserve non-activating mouse/control behavior. Exclusive
   swap-chain visibility is a documented limit, not a reason to steal foreground focus.
3. Apply full-monitor versus work-area constraints according to fullscreen state, including
   taskbars on every edge. Preserve separate saved-position/restoration rules and animate legal
   correction through the existing transition mechanism. Normalize missing/corrupt monitors and
   changed resolutions; do not assume coordinates are positive or the primary screen is active.
4. Convert CSS/device/native coordinates at the documented boundaries, preserving DPI, viewport
   zoom reset, user scaling, visible grab offsets and growth anchors. Do not apply scaling twice.
   Avoid measure-resize feedback loops and first-frame placement jumps.
5. Retain the transparent backing canvas for documented menu transitions and release it when
   closing. Native input regions must follow actual visible surfaces, preserving antialiased
   edges while letting transparent padding pass input to the app behind it.
6. Coordinate drag, capture, release-click suppression, compact/expand and rapid queued movement.
   Respect reduced motion. Dispose pending native callbacks and owned handles safely; late
   reconciliation must not target a destroyed window.

## Validate native and rendered behavior separately

Use [Testing and release](../../../Docs/TESTING-RELEASE.md): focused `--window-checks` in the
relevant configuration, plus `placement.mjs`/`navigation.mjs` when shell rendering changes.
Use isolated output when an active HIT locks the normal build directory.

Cover taskbar edges, negative monitor coordinates, repeated transitions, mixed DPI and corrupted
saved positions as relevant. Native/headless assertions do not prove real-game acceptance;
report which fullscreen/focus/Explorer cases still need interactive verification. Update the
window/settings contract and retain diagnostics sufficient to distinguish recovery from its trigger.

## Current contract checkpoints

Keep the single startup foreground request separate from non-activating fullscreen/FORCE recovery. Title-pill padding and content-entry transitions must preserve shared/native measurement and grab regions; do not count an entrance transform as new window geometry or fullscreen overlay success.
