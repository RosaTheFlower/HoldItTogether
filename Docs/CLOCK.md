# Clock: world clocks, timer and Pomodoro

Entry: `Components/HitMenus/HitMenuRelogio.razor`; views in `Components/Clock`, service in `Services/Clock`.
Clock is initially second, but user menu order can change. Settings remains last.
There is no Reminders feature in 1.0.

## Functions and compact mode

- **Clock:** primary time/date, optional seconds and optional smaller secondary system clock.
- **Timer:** hours/minutes/seconds, start, pause/resume and reset; durations from one second through 24 hours.
- **Pomodoro:** focus, short/long breaks, pause/resume, restart and skip.

M2 on the title opens general settings. M2 on the system readout (full or compact) opens World clocks.
Both settings views block wheel-based menu switching and support M2 return without unpinning.

While pinned, a click on the full readout or three seconds without interaction requests Compact.
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
The list is a bounded local scroll region rather than page/window scroll.

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
