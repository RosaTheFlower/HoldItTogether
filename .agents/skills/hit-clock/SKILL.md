---
name: hit-clock
description: "Implement or review HIT system/world clocks, timer, Pomodoro, timezone handling, persisted countdown deadlines or clock notices. Use for clock-domain behavior and its views; pair with hit-extensions for SDK clock changes and hit-themes for clock CSS."
---

# HIT clock workflow

## Read the contract

Read [Clock](../../../Docs/CLOCK.md). Use [Clock themes](../../../Docs/CLOCK-THEMES.md)
for styling, [Menus](../../../Docs/MENUS.md) for compact/settings interaction, and
[API](../../../Docs/API.md) for shared Clock commands. Follow
[Code conventions](../../../Docs/CODE-CONVENTIONS.md) for persistence and threading.

## Keep deadlines independent of presentation

1. Separate `HitClockService` state, persisted deadlines, background completion work and visible
   display ticks. Extensions operate on this same service; do not create a second timer engine
   inside a component or SDK adapter.
2. Persist running countdowns as UTC deadlines and paused countdowns as remaining duration.
   Derive display values from those facts rather than decrementing a UI counter. Presentation
   sleep, hidden views and restart must not stop or rescale a running countdown.
3. Preserve completion idempotence and the documented restart/long-sleep behavior. A failed save
   must not replay sound/notices; do not replay every missed phase after sleep. Keep Pomodoro
   completed-round versus skipped-phase semantics and subsequent-phase duration updates.
4. Normalize settings against the guide's current bounds. Use stable Windows time-zone IDs,
   actual `TimeZoneInfo` rules and `local` semantics, including DST and date boundaries.
   Preserve user-entered labels, ordered identities and at least one valid world clock.
5. Keep world-clock forms lazy and local-scrollable, with edit state following the item after
   reorder. Preserve a draft when persistence fails. Keep source labels separate from IDs.
6. Follow the shared compact contract: eligible readouts, M1 restore, drag threshold/release-click
   suppression, hover protection and M2 return. Compact mode retains the selected clock function.
   Do not compact configuration forms or tie deadline progress to layout animation.
7. Preserve in-HIT notice priorities and sound preferences. Do not claim Windows toast support,
   reminders, computer-clock mutation or public preference editing unless implementing a new
   documented contract explicitly requested by the user.

## Verify transitions and recovery

Select checks from [Testing and release](../../../Docs/TESTING-RELEASE.md), using host clock
checks and `menus.mjs`, `idle-input.mjs` or `placement.mjs` when those surfaces change.
Use disposable clock state for save/reload/corruption tests.

For timing changes, check pause/resume, restart, sleep past completion, repeated completion
processing, failed save and long/short-break progression. For zones, include DST and crossing
midnight. Verify hidden/compact display work stops while service deadlines remain correct.
Update Clock, Clock themes or API according to the contract affected.

## Current contract checkpoints

Use shared inline settings sliders for Pomodoro durations and the common workspace/history rules. On transparent action buttons, Timer and Pomodoro text follows the Clock foreground rather than its surface color. Menu-entry and sidebar animations must not restart, delay or alter domain deadlines.
