<div align="center">

HIT — Hold It Together
Your desktop, a little more together.

A compact, customizable Windows companion that keeps music, time, and useful tools within reach, without another full-sized window getting in the way.

Windows 10/11 · .NET 8 · Photino · Blazor

[Get started](GETTING-STARTED.md) · [Explore extensions](EXTENSIONS.md) · [Customize HIT](THEMES.md) · Documentation
</div>

Meet HIT
HIT lives as a small, borderless sphere on your desktop. Hover to open it, click to keep it open, and scroll to move between menus. Drag it where you want it; edge snapping and optional center snapping help it stay in place.

It starts with Audio, Clock, and Settings. Add more features through independently installable .hitmenu extensions, or make it yours with themes, backgrounds, and live appearance editing.
Preview
<p align="center">
  <img src="https://github.com/user-attachments/assets/2b161490-7227-4917-94af-80c789f389dd" alt="HIT desktop preview 1" width="100%" />
</p>

<details>
<summary><strong>More screenshots</strong></summary>
<br />
<p align="center">
  <img src="https://github.com/user-attachments/assets/5e46ab7b-d647-4331-abd2-9aff1eafa39e" alt="HIT desktop preview 2" width="100%" />
  <img src="https://github.com/user-attachments/assets/55b8afad-1f89-4887-bbc6-f8a69fc1cb35" alt="HIT desktop preview 3" width="100%" />
</p>
</details>

What you can do
Music, without switching windows
- Control playback, skip tracks, seek, and use supported shuffle/repeat actions through compatible Windows media sessions.
- View synchronized lyrics with six layouts, including Spotlight and Word Rise; read unsynchronized lyrics when timestamps are unavailable.
- Enable audio-reactive visualizers, album artwork, and customizable backgrounds.
- Press Space to pause or resume playback while the Audio menu is active.
Time, focus, and everyday utilities
- See your local time and configure world clocks.
- Run a timer or Pomodoro sessions with configurable focus and break durations.
- Keep countdowns running independently of the visible interface, including recovery after sleep or restart.
- Use compact pinned views to keep information visible without taking up unnecessary space.
Make HIT yours
- Switch between bundled Dark, OLED, and Light themes, or import your own CSS.
- Adjust appearance live, save visual profiles, and export CSS.
- Use artwork blending, image backgrounds, GIFs, and slideshows.
- Configure navigation, animations, window placement, tooltips, and accessibility preferences.
Extend it
- Install .hitmenu packages from the app or by dragging them onto HIT.
- Enable, disable, reorder, update, and restore extension versions.
- Build your own menus using the public HIT SDK.
- Try Chess as an optional, separately installed extension.
Getting started
HIT supports Windows 10 (1809+) and Windows 11 on x64 systems.

The simplest route is the Windows installer, which offers:
Profile	Includes
Release	HIT and the runtime dependencies needed to run it
Dev	Release plus developer source, SDK package, sample Notes extension, docs, and build tools
Optional Chess	A separate Chess extension, available with either profile


The installer handles the WebView2 prerequisite when needed. For setup details, prerequisites, and building from source, see [Getting started](GETTING-STARTED.md) and [Windows installer](INSTALLER.md).
How it works
HIT is a Windows desktop application built with .NET 8, Photino, and Blazor. Its host owns the shared sphere, navigation, settings, and extension lifecycle. Built-in features use application services; external menus are distributed as .hitmenu packages with compiled .NET components, a manifest, and public assets.

Themes are CSS-based and separate from extension behavior. This means you can change the look without recompiling the application, or add new functionality without modifying the HIT host.
Documentation
All maintained technical documentation lives in this directory. Repository agent instructions live in AGENTS.md and .agents/skills/. Documentation and public code examples use English, matching the interface. Commands run from the repository root unless stated otherwise.
Find the right guide
Goal	Guide
Install, run, navigate and configure HIT	[Getting started](GETTING-STARTED.md)
Build and distribute the Release/Dev installation wizard	[Windows installer](INSTALLER.md)
Understand release versions, implemented features and limits	[Release scope](VERSION-1.0.md)
Understand components, services, persistence and startup	[Architecture](ARCHITECTURE.md)
Follow the shared interaction/layout contract	[Menus](MENUS.md)
Apply shared controls, confirmations, spacing and theme rules	[Design system](DESIGN.md)
Manage settings, themes, updates and personal data	[Settings](SETTINGS.md)
Add a menu compiled into the application	[Internal menus](INTERNAL-MENUS.md)
Build, install and distribute an independent .hitmenu	[Extensions](EXTENSIONS.md)
Call the public SDK: media, clock, storage, theme and window	[API reference](API.md)
Follow code, naming, threading and resource conventions	[Code conventions](CODE-CONVENTIONS.md)
Use and maintain repository agent instructions and skills	[Agent workflows](AGENT-WORKFLOWS.md)
Create a theme or follow the visual design system	[Themes and design](THEMES.md)
Understand the music player, lyrics and capture	[Audio](AUDIO.md)
Style the player, artwork, lyrics and visualizer	[Audio theme reference](AUDIO-THEMES.md)
Understand clocks, timer, Pomodoro and persistence	[Clock](CLOCK.md)
Build, install and play the independent chess menu	[Chess extension](CHESS.md)
Style full/compact/world clocks	[Clock theme reference](CLOCK-THEMES.md)
Respect memory budgets and interpret measurements	[Performance](PERFORMANCE.md)
Add English messages and prepare translation support	[Localization](LOCALIZATION.md)
Build, test, package, publish and prepare a commit	[Testing and release](TESTING-RELEASE.md)
Diagnose installation, media, CSS and startup problems	[Troubleshooting](TROUBLESHOOTING.md)


Recommended reading paths
Users: Getting started, then Audio/Clock and Troubleshooting as needed.

Extension authors: Menus → Extensions → API → Code conventions → Themes. Use the Notes example in Modules/HIT.Example and the HIT.Sdk project.

Theme authors: Themes → Audio themes → Clock themes. A theme changes CSS presentation; it does not need a compiled extension.

Maintainers: Architecture → Code conventions → Performance → Testing and release. Built-in menu source remains the reference for integration details, while the SDK is the public extension contract.
Version and source of truth
Item	Version
Application release	2v2910 (29 October)
Built-in Audio / Clock	1.2.0 / 1.2.0 (bundled with HIT)
Optional Chess package	1.1.1
Public SDK package	1.2.0
SDK assembly identity	1.0.0.0
.hitmenu format / hitContractVersion	1 / "1"
Imported CSS theme contract	1


These versions describe different contracts. A package's own version is independent of all of them. Current constraints are documented as implemented; planned features are explicitly marked.

The documentation is copied to build/publish output under Docs/. Source-code links target a repository checkout. The published guides are self-contained; source projects, examples and test harnesses are obtained with the repository rather than shipped as executable output.
Keeping the documentation complete
Update the relevant reference and example whenever changing a public API, preference, lifecycle, selector, attribute, manifest field, limit or user interaction. Keep one canonical explanation per subject and link to it instead of copying conflicting instructions. Preserve historical measurement conditions.

Run documentation checks after moving or renaming any guide.
