# Getting started with HIT

HIT (Hold It Together) is a compact Windows desktop companion built with .NET 8, Photino and Blazor.
Its transparent, borderless sphere opens into menus. Audio, Clock and Settings are included;
additional menus can be installed as `.hitmenu` extensions.
The [Chess module](CHESS.md) is one such optional extension. Its source is kept in this repository, but it is built, packaged and installed separately from HIT.

## Requirements and running

- Windows 10 version 1809 or later; the project targets `net8.0-windows10.0.19041.0`.
- Microsoft Edge WebView2 Runtime for the desktop interface.
- The .NET 8 runtime for a framework-dependent distribution.
- To build from source, a .NET SDK capable of targeting .NET 8 and restoring the Windows targeting references.

Run from source:

```powershell
dotnet restore HoldItTogether.csproj
dotnet run --project HoldItTogether.csproj
```

For a release distribution, keep the complete published directory together and start `HoldItTogether.exe`.
Do not distribute the executable alone. CSS, JavaScript, SDK/runtime dependencies and native libraries are required.
Release publication commands are in [Testing and release](TESTING-RELEASE.md).

## The sphere and menu navigation

Hovering opens the sphere temporarily. Clicking keeps it open until explicitly closed or unpinned.
The pin button controls this state; Close collapses and unpins. Clicking outside does not unpin a clicked-open menu.
Drag a free area to reposition HIT. Screen-edge anchoring and magnetism keep the menu usable near monitor edges.
HIT restores its last position at startup. Settings -> Window placement controls Magnetize, Allow other monitors,
and Edge padding (98% by default; 100% is flush, 85% leaves more space).
Do not drag through sliders, text inputs or other controls.

Scroll down/up to move to the next/previous menu, or click a pagination dot. Navigation does not wrap at the ends.
The initial order is Audio, Clock, Settings. Extensions can change the order; Settings is always last and cannot be disabled.

Right-click (M2) a configurable title to open that menu's settings. Right-click inside an open configuration returns.
Alt highlights elements that offer configuration. Ctrl highlights special click actions, including lyric sources and media-app activation.
Escape and explicit Back controls provide keyboard return where supported.

HIT stays off the taskbar. Enable System Tray for its Show HIT, Hide HIT, Restart HIT and Exit actions.
Settings -> Start with Windows optionally starts this distribution at user sign-in; it is off by default.
If the tray is disabled, hiding the native window is not offered by that tray.

## Settings

| Option | Default / behavior |
| --- | --- |
| Always on Top | On; keeps HIT above ordinary windows |
| System Tray | Off; optional Windows notification-area icon |
| Remove animations | Off; disables CSS animation, transitions and smooth scrolling |
| Show tooltips | On; native hover descriptions, independent of accessible names |
| Scroll speed | Sensitivity 1; cooldown enabled, 350 ms |
| Info | RAM usage, version/counts, update checking, GitHub and confirmed data reset/deletion |
| Extensions | Dedicated installation/order/enable/remove screen, immediately above Themes |
| Themes | Final row; combobox for activation plus a management screen for list selection/import/delete |

Scroll sensitivity accepts 0.25-3; cooldown accepts 0-2000 ms. Disable cooldown to pass several menus in one large wheel gesture.
Theme management supports single/multiple selection, double-click activation and confirmed deletion of personal themes.
Built-in themes are protected. See [Settings](SETTINGS.md) for all subviews and data management.

## Audio and Clock

[Audio](AUDIO.md) shows a compatible app's Windows media session. Playback, previous/next, seeking, shuffle and repeat
depend on that app's capabilities. Lyrics need online timestamped results. The visualizer uses the default output's mixed audio.
The artwork button enables cover/palette styling; right-click it for blur and global/lyrics background settings.

[Clock](CLOCK.md) contains system/world clocks, a timer and Pomodoro. Right-click its title for general options;
right-click the system readout for world clocks. When pinned, clicking the readout or five seconds after the pointer leaves HIT
shows a compact clock. Left-click restores controls; hover alone does not.
All pinned menus share Settings -> Compact after, adjustable from immediately after leaving to ten seconds.

## Installing extensions and themes

For an extension, open Settings -> Extensions -> Install .hitmenu, or drop one package onto HIT.
New modules activate immediately. Updating already loaded code restarts HIT automatically after graceful shutdown.
Drag the grip beside a name or use Up/Down to reorder menus.
Switch a menu off to remove it from navigation. Removing an external package asks for confirmation and affects all its menus.
Code/file cleanup completes on the next startup; saved extension data is kept.

For a theme, open Settings -> Themes -> Import CSS, or drop CSS anywhere on HIT. HIT copies it to the personal Themes directory and selects it in management.
Double-click the theme or choose it in the combobox to activate it.
Edit that installed copy to see live changes. See [Themes](THEMES.md) for the accepted CSS format.

## Data and backups

All user state lives below `%LOCALAPPDATA%\HoldItTogether`:

| Location | Contents |
| --- | --- |
| `settings.json` | Global, audio and clock preferences |
| `window.json` | Last visible position, growth direction and monitor reference |
| `Clock/state.json` | Timer/Pomodoro deadlines and pending notices |
| `Themes/` | Personal theme CSS |
| `Modules/registry.json` | Extension versions, menu order and disabled IDs |
| `Modules/Installed/` | Installed extension code and editable assets |
| `Modules/State/` | Extension-owned persistent data |

Close HIT before copying these directories for a consistent backup. Keep the published application separate from user data.
Reinstalling/rebuilding the application does not require deleting preferences.
