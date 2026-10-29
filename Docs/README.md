<div align="center">

HIT — Hold It Together
A small space for the things you use every day.

A lightweight, customizable desktop companion for Windows. Control your music, follow lyrics, stay focused, and bring your favorite tools together in one expandable interface.
<p>
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=flat-square&logo=windows&logoColor=white" />
  <img alt=".NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square&logo=dotnet&logoColor=white" />
  <img alt="Photino and Blazor" src="https://img.shields.io/badge/Photino%20%2B%20Blazor-Desktop-555?style=flat-square" />
  <img alt="HIT release 2v2910" src="https://img.shields.io/badge/Release-2v2910-333?style=flat-square" />
</p>


[**Get started**](GETTING-STARTED.md)

<img src="https://github.com/user-attachments/assets/2b161490-7227-4917-94af-80c789f389dd" alt="HIT desktop interface" width="100%" />

<sub>One compact companion. Music, time, and more, right where you need them.</sub>
</div>

Meet HIT
HIT starts as a discreet, borderless sphere on your desktop. Hover to open it, click to keep it open, and scroll to switch between menus. Move it around your screen, pin it when needed, or let it return to its compact form.

It includes an audio player, clocks and focus tools, plus deep appearance customization. Install additional .hitmenu extensions to make HIT work the way you do, without rebuilding the app.
- Out of the way until you need it. A compact desktop surface that stays off the taskbar.
- Made to feel personal. Themes, artwork, backgrounds, animations, and visual profiles.
- Designed to grow. Independently installed menus powered by a public .NET SDK.
- Built for daily use. Persistent timers, configurable navigation, and resource-aware behavior.
Features
Music and synchronized lyrics
Control compatible Windows media sessions without constantly switching apps. View track information and artwork, manage playback, and follow lyrics with layouts made for different listening styles.
- Play/pause, previous/next, seeking, and supported shuffle/repeat controls.
- Synchronized lyrics from multiple providers, with unsynchronized reading when timestamps are unavailable.
- Six lyric layouts: Down, Up, Left, Right, Spotlight, and Word Rise.
- Audio-reactive visualizers with adjustable styles, positioning, and opacity.
- Press Space to play or pause while HIT's Audio menu is visible.
Clock, timers, and focus
Keep the time nearby, wherever you're working.
- Local and configurable world clocks.
- A timer with pause, resume, and reset controls.
- Pomodoro sessions with customizable focus and break durations.
- Compact pinned readouts and timers that continue independently of the visible UI.
A desktop companion that adapts
- Reorder or disable menus, navigate with the mouse wheel, and enable Infinite Menu wrapping.
- Configure mouse-button gestures, menu transitions, and window placement.
- Snap to screen edges or optionally to the center; use multiple monitors.
- Adjust animation, tooltip, and accessibility preferences.
<details>
<summary><strong>See more of HIT</strong></summary>
<br />
<p align="center">
  <img src="https://github.com/user-attachments/assets/5e46ab7b-d647-4331-abd2-9aff1eafa39e" alt="Additional HIT desktop interface preview" width="100%" />
</p>
<p align="center">
  <img src="https://github.com/user-attachments/assets/55b8afad-1f89-4887-bbc6-f8a69fc1cb35" alt="Additional HIT desktop interface and menu preview" width="100%" />
</p>
</details>

Personalization
Go beyond choosing a light or dark theme. HIT lets you edit its appearance live, save visual profiles, and export your customizations as CSS.
<p align="center">
  <img src="https://github.com/user-attachments/assets/a5c7cb29-5479-40d3-a4e6-95c72a404c48" alt="HIT live personalization settings" width="657" />
</p>


Choose from the bundled Dark, OLED, and Light themes, import custom CSS, or personalize the interface using backgrounds, album artwork blending, slideshows, animated images, and independent opacity controls. Changes to installed custom theme files can be reflected without rebuilding HIT.

[Explore themes and styling →](THEMES.md) · [Personalization settings →](SETTINGS.md)
Extensions
HIT is designed around modular menus. Add features using .hitmenu packages, install them from the app or by drag-and-drop, and manage their order and enabled state from Settings.
<table>
  <tr>
    <td valign="top" width="55%">
      <h3>Chess: an optional extension</h3>
      <p>Chess is distributed as its own <code>.hitmenu</code> package rather than being built into HIT. It demonstrates how independent modules can share the same window, navigation, and design system as the built-in features.</p>
      <p>HIT also supports extension version management and GitHub-based update discovery.</p>
      <p><a href="CHESS.md">Explore Chess</a> · <a href="EXTENSIONS.md">Extension guide</a></p>
    </td>
    <td align="center" valign="middle" width="45%">
      <img src="https://github.com/user-attachments/assets/4880afb4-45f3-4c9d-9c83-4dc2aa6cf361" alt="Chess extension running inside HIT" width="300" />
    </td>
  </tr>
</table>

Build your own menus
The public HIT SDK lets developers create independent .NET extensions with their own views and assets, using documented APIs for supported host integrations. Packages include compiled components, a manifest, and resources; end users don't need the .NET SDK or project source to install them.

[Create an extension](EXTENSIONS.md) · [SDK reference](API.md) · [Menu design contract](MENUS.md)
Get started
Platform: Windows 10 (version 1809 or later) or Windows 11, x64.

The recommended installation method is the Windows installer, which bundles the application and .NET runtime. It also handles the WebView2 prerequisite when needed.
Installer option	What's included
Release	HIT and everything needed for normal use.
Dev	Release plus the SDK package, example extension, source, documentation, and development tools.
Chess (optional)	Installs the separate Chess extension with either profile.


For installation steps and requirements, read the [Getting started guide](GETTING-STARTED.md) or [Windows installer guide](INSTALLER.md).
<details>
<summary><strong>Run from source</strong></summary>


From the repository root, with a suitable .NET SDK installed:
dotnet restore HoldItTogether.csproj
dotnet run --project HoldItTogether.csproj
See [Getting started](GETTING-STARTED.md) for prerequisites and [Testing and release](TESTING-RELEASE.md) for publishing instructions.
</details>

Built with
.NET 8 · Blazor · Photino · Windows WebView2

The host provides the sphere interface, navigation, settings, and extension lifecycle. Built-in menus use application services, while external modules use a versioned SDK. CSS themes change presentation without changing application behavior.

For implementation details, see [Architecture](ARCHITECTURE.md), [Design system](DESIGN.md), and [Performance](PERFORMANCE.md).
Documentation
The guides below cover setup, features, design conventions, extension development, and maintenance. All maintained technical documentation lives in this directory. Repository agent instructions live in AGENTS.md and .agents/skills/; documentation and public code examples use English, matching the application. Unless stated otherwise, commands run from the repository root.
<details open>
<summary><strong>User guides</strong></summary>

Goal	Guide
Install, run, navigate and configure HIT	[Getting started](GETTING-STARTED.md)
Build and distribute the Release/Dev installation wizard	[Windows installer](INSTALLER.md)
Understand release versions, implemented features and limits	[Release scope](VERSION-1.0.md)
Manage settings, themes, updates and personal data	[Settings](SETTINGS.md)
Create a theme or follow the visual design system	[Themes and design](THEMES.md)
Understand the music player, lyrics and capture	[Audio](AUDIO.md)
Style the player, artwork, lyrics and visualizer	[Audio theme reference](AUDIO-THEMES.md)
Understand clocks, timer, Pomodoro and persistence	[Clock](CLOCK.md)
Style full/compact/world clocks	[Clock theme reference](CLOCK-THEMES.md)
Build, install and play the independent chess menu	[Chess extension](CHESS.md)
Diagnose installation, media, CSS and startup problems	[Troubleshooting](TROUBLESHOOTING.md)


</details>

<details>
<summary><strong>Development and maintainer guides</strong></summary>

Goal	Guide
Understand components, services, persistence and startup	[Architecture](ARCHITECTURE.md)
Follow the shared interaction/layout contract	[Menus](MENUS.md)
Apply shared controls, confirmations, spacing and theme rules	[Design system](DESIGN.md)
Add a menu compiled into the application	[Internal menus](INTERNAL-MENUS.md)
Build, install and distribute an independent .hitmenu	[Extensions](EXTENSIONS.md)
Call the public SDK: media, clock, storage, theme and window	[API reference](API.md)
Follow code, naming, threading and resource conventions	[Code conventions](CODE-CONVENTIONS.md)
Use and maintain repository agent instructions and skills	[Agent workflows](AGENT-WORKFLOWS.md)
Respect memory budgets and interpret measurements	[Performance](PERFORMANCE.md)
Add English messages and prepare translation support	[Localization](LOCALIZATION.md)
Build, test, package, publish and prepare a commit	[Testing and release](TESTING-RELEASE.md)


</details>

Recommended reading paths
- Users: Getting started → Audio / Clock → Troubleshooting.
- Extension authors: Menus → Extensions → API → Code conventions → Themes. Start with Modules/HIT.Example and HIT.Sdk.
- Theme authors: Themes → Audio themes → Clock themes. Themes alter presentation, not app behavior.
- Maintainers: Architecture → Code conventions → Performance → Testing and release. Built-in menu source is the integration reference; the SDK is the public extension contract.
Versions and compatibility
Component or contract	Version
HIT application	2v2910 (29 October)
Built-in Audio / Clock	1.2.0 / 1.2.0 (bundled with HIT)
Optional Chess package	1.1.1
Public SDK package	1.2.0
SDK assembly identity	1.0.0.0
.hitmenu format / hitContractVersion	1 / "1"
Imported CSS theme contract	1


These versions describe different contracts. An extension package's version is independent of the host, SDK, and theme format. Implemented limitations are documented in their respective guides; planned features are identified as such.
<details>
<summary><strong>Documentation maintenance and distribution notes</strong></summary>


The documentation is copied into the build/publish output under Docs/. Source-code links target a repository checkout. The published guides are self-contained; source projects, examples, and test harnesses are obtained from the repository, rather than shipped as executable output.

When changing a public API, preference, lifecycle, selector, attribute, manifest field, limit, or interaction, update the canonical reference and corresponding example. Avoid duplicating conflicting instructions, preserve historical measurement conditions, and run documentation checks after moving or renaming a guide.
</details>
