# Settings and data management

Settings is always enabled and the last navigation menu. Its current CLR ID is
`HoldItTogether.Components.HitMenus.HitMenuSettings`; historical Settings/Clock IDs migrate when the extension registry loads.
Source: `Components/HitMenus/HitMenuSettings.razor`. Controls follow [Design](DESIGN.md).

## Screens

The main screen contains Always on Top, System Tray, Remove animations, Show tooltips and Compact after,
then forward rows for Window placement, Scroll speed, Info, Extensions and Themes, in that order.
Themes is last and retains a combobox for immediate activation without opening the management screen.
Each subview blocks wheel-based menu navigation and has Back, Escape and right-click return.
The header cover icon temporarily hides music artwork for the current configuration screen, without changing Audio preferences.
It resets when leaving that screen and is disabled when no artwork background is active.

Always on Top reveals Force Always on Top directly below it. This experimental switch has a warning icon
and the tooltip "Experimental. Use at your own risk!". While enabled and visible, a native 500 ms timer
reasserts topmost positioning without activating HIT. Disabling Always on Top also disables Force.
It attempts to stay above fullscreen windows; exclusive fullscreen and protected system surfaces may still cover HIT.
The implementation uses [SetWindowPos](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-setwindowpos)
and [window timers](https://learn.microsoft.com/en-us/windows/win32/winmsg/using-timers).

Compact after replaces the former compact-button preference. It saves `menuCompactDelayMs`, default 5000,
from 0 (Immediately after leaving) through 10000 ms, in 500 ms steps. The header has cover, pin and close only.
Only Audio's Player/Lyrics pages, Clock's main readout and the optional, separately installed [Chess extension](CHESS.md)'s board can compact. All configuration screens,
General Settings and other modules stay expanded permanently. Hover anywhere over HIT prevents compaction, even without movement.
Clock and Chess use miniature layouts; Audio hides secondary controls. Audio settings -> Hide buttons independently
enables this behavior for Player and Lyrics. Open confirmations and active pointer gestures suspend compaction.
The label uses the same primary typography as other settings. Old `showCompactButton` and `audioHideControls` keys are ignored;
the independent audio preferences are `audioHidePlayerButtons` and `audioHideLyricsButtons`, both true by default.

Scroll speed contains Cooldown on/off, Sensitivity (0.25–3, default 1) and Pause (0–2000 ms, default 350).
Disabling cooldown disables Pause; the saved duration remains available when enabled again.

Window placement contains three independent controls: Magnetize (on by default), Allow other monitors (on),
and Edge padding (98%, configurable from 100% down to 85%). Both axes use the same pixel inset, calculated from the shorter work-area dimension. 100% is flush with the usable work-area edge;
lower percentages shrink the centered allowed area, leaving equal insets on opposite edges. Taskbar space is excluded.
Magnetize attracts the visible surface within 12 logical pixels of the padded monitor/taskbar edge; disabling it removes attraction.
Allow other monitors off restricts HIT to the primary work area. When on, dragging can cross the combined monitor areas.
Padding/monitor changes apply immediately. Geometry uses the visible sphere/menu, never the larger transparent canvas.

The last visible anchor is saved on drop and shutdown in `window.json` beside `settings.json`. Startup restores that anchor
and its corner/center growth direction, normalizes it to the available monitor, and clamps it inside the configured padding.
Unplugged monitors or corrupt/missing position files fall back safely to an available/default position. Data reset/delete clears this file.

Themes shows a bounded list with hidden scrollbars and directional overflow hints. Single click selects;
Ctrl+click toggles multiple selections; double-click or Enter activates a theme.
Active and selected states are distinct. Delete selected acts on personal themes only and requires confirmation.
Deleting the active personal theme first persists the Dark fallback. Bundled themes cannot be deleted.
Open folder and Delete selected precede the final Import CSS action.
Importing selects the new personal theme in the list; the user activates it explicitly.
Dropping CSS anywhere on HIT imports it and opens this same screen.

Extensions installs, enables/disables and reorders menus. Built-ins include a disabled trash icon;
Settings also has a disabled drag grip and enable switch. Drag a grip at least six pixels to reorder,
or use the Up/Down buttons. See [Extensions](EXTENSIONS.md) for activation and restart behavior.
Clear old versions is the final action. It removes obsolete installed code directories while preserving
the selected and currently loaded versions, package state and logs. Unavailable rollback pointers are cleared.
Locked or linked directories are skipped and reported; running code is never removed by this action.

## Info

Read-only rows show RAM usage, HIT version, installed extension count (excluding pending removals)
and installed theme count (including the three bundled themes).
The application version uses `vDDMM`, such as `v2710` for 27 October.
RAM samples private resident HIT memory, excluding WebView2 child processes, only while Info is visible.
Check for updates queries the repository's latest published GitHub release with an eight-second timeout.
It compares dated `vDDMM` releases using their publication year and offers Open release when a newer release exists; it never installs an update silently.
The full-width GitHub button shows its logo and “Hold It Together”.

## Resetting and deleting data

Delete my data opens a choice screen. Neither choice changes files until its second confirmation:

| Choice | Removed/reset | Preserved |
| --- | --- | --- |
| Reset all settings | Global/audio/clock preferences, saved window position, clock state, extension State/logs, navigation order and disabled flags | Installed extension versions, personal theme CSS, canonical bundled assets |
| Delete everything | Preferences, saved window position, clock state, personal Themes and the complete user Modules directory, including installed extensions and Inbox | Application distribution and its bundled menus/themes |

Cancel, Escape or right-click cancels a confirmation. Dialog backdrops are transparent and Cancel receives initial focus.
After confirmation, HIT writes a small `reset-request.json` marker in its user-data root and closes normally.
The restart is launched after the clock/module/Inbox workers stop. The next startup applies the request before creating services,
so shutdown writes cannot recreate removed state. No app executable or canonical theme file is deleted.
Reset requests use the same application user-data root documented in [Getting started](GETTING-STARTED.md).
If a restart cannot be requested, the UI cancels the marker and reports that data was kept.

The automated tests use disposable profiles below `obj`; they never reset the user's real profile.
