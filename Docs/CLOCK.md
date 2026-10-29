# Clock: world clocks, timer and Pomodoro

Entry: `Components/HitMenus/HitMenuClock.razor`; views in `Components/Clock`, service in `Services/Clock`.
Clock is initially second, but user menu order can change. Settings remains last.
There is no Reminders feature in 1.0.

## Functions and compact mode

- **Clock:** primary time/date, optional seconds and optional smaller secondary system clock.
- **Timer:** hours/minutes/seconds, start, pause/resume and reset; durations from one second through 24 hours.
- **Pomodoro:** focus, short/long breaks, pause/resume, restart and skip.

M2 or the optional held-title gesture opens the shared settings workspace. Its visible sidebar has
General, Pomodoro and World clocks; M1 reaches every page. M2 on the system readout (full or compact)
remains a shortcut to World clocks. The right options pane scrolls locally, while wheel input outside
it can navigate menus. The header chevron, M2, side Back and Escape return a subtopic to General,
then General to the main readout without unpinning.
Pomodoro exposes all duration sliders and automatic continuation together, without a hidden group.
These controls keep the documented normalization and immediate persistence; they do not replace
the service's independent deadline engine.

While pinned, a click on the full readout or the shared pointer-departure delay requests Compact.
The default is five seconds; Settings -> Compact after configures immediately through ten seconds.
Hover anywhere over HIT prevents timer-based compaction.
The selected function remains the visible readout; the timer/Pomodoro keeps working.
M1 restores controls, hover does not. Drag the compact readout to move the window without reopening it;
capture starts after six pixels and suppresses the release click associated with that drag.
The secondary compact clock shows only small muted digits centered beneath the primary.

## World clocks

General settings can show the first two clocks. The World clocks page renames, changes zone, adds, removes
and reorders up to 12 entries, with at least one remaining.
Defaults: System (`local`) and New York (`Eastern Standard Time`); dual-clock display is off.
Only the first/second are displayed; additional entries are retained for rearrangement.

Each row is a collapsed disclosure containing name/time/position and a chevron.
Only one edit/add form is open. Expensive zone option lists mount only on expansion.
After reorder, the editing state follows its item. Successful addition closes/clears its form; failed persistence preserves the draft.
The shared right pane owns bounded scrolling; the embedded list does not create an unbounded
nested scroll trap. Per-item editors remain lazy so zone lists mount only when explicitly expanded.

TimeZoneInfo provides actual Windows zone rules, including daylight saving at the displayed instant.
`local` follows the computer's zone; other entries use a zone ID, not a fixed UTC offset.
Examples: `E. South America Standard Time` for Brasilia and `Eastern Standard Time` for New York.
The primary date follows its chosen zone across midnight. Normalization drops invalid zones,
limits names to 40 characters and restores a local clock if the list becomes unusable.
User-entered labels are retained in their original language.

## General preferences

These keys live inside `clock` in settings.json:

| Key | Default / range |
| --- | --- |
| `showSeconds` | true |
| `showTwoClocks` | false |
| `systemClocks` | Ordered array of name/timeZoneId; 1-12 entries |
| `selectedMode` | System/Timer/Pomodoro enum value serialized by settings |
| `focusMinutes` | 25; 1-180 |
| `shortBreakMinutes` | 5; 1-60 |
| `longBreakMinutes` | 15; 1-120 |
| `roundsBeforeLongBreak` | 4; 2-12 |
| `autoContinue` | true |
| `soundEnabled` | true |

A completed focus counts a round; skipping does not. After the configured number of completed focus rounds,
the following break is long. Auto-continue decides whether the next phase starts running.
New durations affect subsequent phases; an already running/paused phase is not silently rescaled.

## Deadlines, persistence and notices

HitClockService is a singleton independent of mounted/visible UI. Its one-second worker processes deadlines and notices.
UI display ticks only while visible. Running countdowns persist UTC deadlines; paused countdowns persist remaining duration.
Remaining time is derived from the deadline rather than decrementing a display counter.

State is stored in `%LOCALAPPDATA%\HoldItTogether\Clock\state.json` with temporary-file replacement.
Restart/sleep recovery processes a completion once rather than repeating alarms or replaying many missed phases.
After a long sleep, the next Pomodoro phase is prepared from the return time.
No per-second save occurs without a change. A failed completion save keeps the in-memory result and retries without repeating its sound.
Load/migration filters corrupt/obsolete notice data while retaining compatible timer/Pomodoro state.

Clock notices are inside HIT, not Windows toast notifications, and can be dismissed.
They have priority over extension notices and are hidden with compact chrome.
Sound is a platform beep when enabled; no external audio file/player is required.

## Integrations and public styling

SDK Clock.Read/StartTimer/Execute acts on this same service. It does not create an independent timer per extension,
or expose world-clock editing/Pomodoro preference editing. See [API](API.md).

Clock uses time-zone conversion, not computer-clock mutation. CSS cannot change a zone or countdown duration.
Public selectors, digit sizing, compact padding and dual-clock details are in [Clock themes](CLOCK-THEMES.md).
General compact/drag/M2/Alt contracts are in [Menus](MENUS.md).

Clock configuration uses the shared small inline slider row and optional menu/topic entrance. These presentation effects never restart countdowns or alter persisted deadlines. Native wheel ownership releases empty directional boundaries to top-level navigation; Infinite menu applies there, independently of per-menu Back/Forward history.

Clock configuration identifies the built-in feature as version 1.2.0. It is shipped and updated with HIT, not installed or updated as a separate extension.
