<div align="center">

# HIT · Hold It Together

**Your music. Your time. Your desktop.**

A customizable Windows desktop companion that brings music, lyrics, clocks and focus tools into one compact, expandable sphere.

<p>
  <img alt="Windows 10 and 11" src="https://img.shields.io/badge/Windows-10%20%7C%2011-0078D4?style=flat-square" />
  <img alt="HIT 3v2511" src="https://img.shields.io/badge/HIT-3v2511-555?style=flat-square" />
  <img alt="Built with .NET 8" src="https://img.shields.io/badge/.NET-8-512BD4?style=flat-square" />
  <img alt="English and Brazilian Portuguese" src="https://img.shields.io/badge/Languages-EN%20%7C%20PT--BR-39766A?style=flat-square" />
</p>

[**Releases**](https://github.com/RosaTheFlower/HoldItTogether/releases) · [**Get started**](GETTING-STARTED.md) · [**What's new**](CHANGELOG.md) · [**Build an extension**](EXTENSIONS.md)

<img src="https://github.com/user-attachments/assets/2b161490-7227-4917-94af-80c789f389dd" alt="HIT desktop interface showing its compact expandable menus" width="100%" />

<sub>A small space for the things you use every day.</sub>

</div>

## Meet HIT

HIT stays nearby as a borderless sphere, off the taskbar. Hover to open it, click to keep it open, and scroll to switch menus. Drag a free area to move it; pin the tools you need and let supported views become compact when idle.

Audio, Clock and Settings come built in. Themes, visual profiles and installable `.hitmenu` extensions let you make the rest your own.

| Listen | Keep time | Make it yours |
| --- | --- | --- |
| Playback controls, artwork, synchronized lyrics and audio-reactive visualizers | System/world clocks, persistent timers and customizable Pomodoro sessions | Dark, OLED and Light themes, live appearance editing, backgrounds and extensions |

## What's new in 3v2511

- **Languages:** English by default, Brazilian Portuguese built in, and validated JSON translation imports under Info → Languages.
- **Personalization:** RGBA colors, installed fonts, visual profiles, local video backgrounds and selective theme overrides.
- **HIT Sphere:** grouped behavior, placement, closed/inactive appearance and background playback preferences.
- **Interface:** page-specific header icons, refined shared controls and clearer language installation feedback.
- **Release notes:** current-build changelog available offline, alongside notes from the latest published release.

Read the [complete changelog](CHANGELOG.md) for compatibility details and release notes.

## Music without switching windows

Control compatible Windows media sessions, see the current artwork and follow the lyrics while you work.

- Play/pause, previous/next, seeking and supported shuffle/repeat controls.
- Synchronized lyrics with six layouts: Down, Up, Left, Right, Spotlight and Word rise.
- Plain lyric reading when synchronized timestamps are unavailable.
- Visualizer styles, artwork effects and music-derived interface colors.
- Space to play/pause while the Audio menu is visible and keyboard focus permits it.

Playback commands depend on the media application's capabilities. Online lyrics depend on provider availability; the visualizer captures the default output mix.

<details>
<summary><strong>Explore the interface</strong></summary>

<p align="center">
  <img src="https://github.com/user-attachments/assets/5e46ab7b-d647-4331-abd2-9aff1eafa39e" alt="Additional HIT desktop interface preview" width="100%" />
</p>
<p align="center">
  <img src="https://github.com/user-attachments/assets/55b8afad-1f89-4887-bbc6-f8a69fc1cb35" alt="Additional HIT menus and appearance preview" width="100%" />
</p>

</details>

## Time to focus

Keep local and world clocks nearby, run a timer or start a Pomodoro session with your own focus and break durations. Timer deadlines survive restarts, and background clock work continues independently of the visible menu.

Choose compact pinned readouts, notification sounds and an optional secondary clock. [Explore Clock →](CLOCK.md)

## A desktop that feels like yours

Start with **Dark**, **OLED** or **Light**, import CSS, or edit your appearance live. Change colors, typography, borders and shadows; blend album artwork with your background; use images, slideshows or silent local videos.

Save independent visual profiles, undo session edits and export CSS. Video exports use confirmed fixed frames. Closed and inactive sphere states have their own appearance and playback preferences, subject to resource-saving and reduced-motion rules.

<p align="center">
  <img src="https://github.com/user-attachments/assets/a5c7cb29-5479-40d3-a4e6-95c72a404c48" alt="HIT personalization settings preview" width="657" />
</p>

[Personalization settings](SETTINGS.md#personalization-and-visual-profiles) · [Themes and CSS](THEMES.md) · [Languages and translations](LOCALIZATION.md)

## Add your own tools

Install `.hitmenu` packages from Settings, by drag-and-drop or through the documented Inbox workflow. Reorder menus and manage their enabled state without rebuilding HIT.

<table>
  <tr>
    <td valign="top" width="55%">
      <h3>Chess, when you want it</h3>
      <p>Play inside the same compact workspace. Chess is a separate optional extension with its own package, settings and saved game state.</p>
      <p>It is installed separately or selected as an optional installer component.</p>
      <p><a href="CHESS.md">Explore Chess</a> · <a href="EXTENSIONS.md">Extension guide</a></p>
    </td>
    <td align="center" valign="middle" width="45%">
      <img src="https://github.com/user-attachments/assets/4880afb4-45f3-4c9d-9c83-4dc2aa6cf361" alt="Optional Chess extension running inside HIT" width="300" />
    </td>
  </tr>
</table>

For developers, the public **HIT SDK** provides versioned APIs for independent .NET extensions with their own views, assets and storage. Start with the Notes example in <code>Modules/HIT.Example</code>. Extensions execute trusted code in the host process; install packages from authors you trust.

[Create an extension](EXTENSIONS.md) · [SDK reference](API.md) · [Menu design contract](MENUS.md)

## Get started

**Windows 10 version 1809 or later / Windows 11 · x64 · WebView2**

Open [Releases](https://github.com/RosaTheFlower/HoldItTogether/releases) to find published downloads. The source version shown here may be newer than the latest published build.

The Windows installer includes the .NET runtime and handles a missing WebView2 prerequisite. Installing WebView2 requires an Internet connection when it is absent.

| Installer choice | Includes |
| --- | --- |
| **Release** | HIT for normal use, with the .NET runtime |
| **Dev** | Release plus the SDK package, example, source, documentation and development tools |
| **Chess** (optional) | The separate Chess extension, available with either profile |

Portable builds must keep their complete published folder together. Framework-dependent builds additionally need the .NET 8 and ASP.NET Core 8 x64 runtimes.

**Updating:** Info → Check for updates compares the installed version with the latest published GitHub release. When a newer version is found, Open release opens its download page; HIT does not automatically download or install the application update.

[Installation and first steps](GETTING-STARTED.md) · [Installer details](INSTALLER.md) · [Troubleshooting](TROUBLESHOOTING.md)

<details>
<summary><strong>Run from source</strong></summary>

Install a .NET SDK capable of targeting .NET 8, plus WebView2. From the repository root on Windows:

```powershell
dotnet restore HoldItTogether.csproj
dotnet run --project HoldItTogether.csproj
```

See [Testing and release](TESTING-RELEASE.md) for build, verification and publication workflows. The application uses .NET 8, Blazor, Photino and Windows WebView2.

</details>

## Documentation

Technical guides live in `Docs/` and use English as the canonical source language. Commands run from the repository root unless stated otherwise. Repository instructions live in `AGENTS.md` and `.agents/skills/`.

### Use and customize HIT

| Guide | Covers |
| --- | --- |
| [Getting started](GETTING-STARTED.md) | Installation, navigation and first steps |
| [Settings](SETTINGS.md) | Preferences, personalization, updates and personal data |
| [Audio](AUDIO.md) | Playback, lyrics, capture and effects |
| [Clock](CLOCK.md) | World clocks, timers and Pomodoro |
| [Themes](THEMES.md) | CSS imports and appearance contracts |
| [Languages](LOCALIZATION.md) | Built-in languages and JSON translation imports |
| [Chess](CHESS.md) | Optional extension, packaging and gameplay |
| [Changelog](CHANGELOG.md) | Current-build changes |
| [Troubleshooting](TROUBLESHOOTING.md) | Common problems and diagnostics |

### Build and maintain

| Guide | Covers |
| --- | --- |
| [Architecture](ARCHITECTURE.md) | Components, services, startup and ownership |
| [Code conventions](CODE-CONVENTIONS.md) | Naming, threading, persistence and resources |
| [Design system](DESIGN.md) · [Menus](MENUS.md) | Shared controls, navigation, accessibility and lifecycle |
| [Internal menus](INTERNAL-MENUS.md) | Built-in feature entry points |
| [Extensions](EXTENSIONS.md) · [API reference](API.md) | Packages, SDK contracts and examples |
| [Audio CSS](AUDIO-THEMES.md) · [Clock CSS](CLOCK-THEMES.md) | Feature styling references |
| [Performance](PERFORMANCE.md) | Resource ownership, measurement and diagnostics |
| [Release scope](VERSION-1.0.md) | Implemented features, compatibility and limitations |
| [Testing and release](TESTING-RELEASE.md) · [Installer](INSTALLER.md) | Validation and distribution |
| [Agent workflows](AGENT-WORKFLOWS.md) | Repository instructions and skill maintenance |

### Versions and compatibility

| Component or contract | Version |
| --- | --- |
| **HIT application** | **3v2511 · 25 November 2026** |
| Built-in Audio / Clock | 1.2.0 / 1.2.0 |
| Optional Chess package | 1.1.1 |
| Public SDK package | 1.2.0 |
| SDK assembly identity | 1.0.0.0 |
| `.hitmenu` format / `hitContractVersion` | 1 / `"1"` |
| Imported CSS theme / language JSON format | 1 / 1 |

Application labels, SDK/package versions and persisted identities have independent compatibility rules. The guides are copied into build/publish output under `Docs/`; source examples and test harnesses come from the repository.
