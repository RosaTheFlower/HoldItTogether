# HIT documentation

All maintained technical documentation lives in this directory. Repository agent instructions
live in `AGENTS.md` and `.agents/skills/`, with their index linked below. Start here whether you use HIT,
develop a menu, write a theme, or maintain the application. Documentation and public code examples
use English, matching the interface. Commands run from the repository root unless stated otherwise.

## Find the right guide

| Goal | Guide |
| --- | --- |
| Install, run, navigate and configure HIT | [Getting started](GETTING-STARTED.md) |
| Build and distribute the Release/Dev installation wizard | [Windows installer](INSTALLER.md) |
| Understand release versions, implemented features and limits | [Release scope](VERSION-1.0.md) |
| Understand components, services, persistence and startup | [Architecture](ARCHITECTURE.md) |
| Follow the shared interaction/layout contract | [Menus](MENUS.md) |
| Apply shared controls, confirmations, spacing and theme rules | [Design system](DESIGN.md) |
| Manage settings, themes, updates and personal data | [Settings](SETTINGS.md) |
| Add a menu compiled into the application | [Internal menus](INTERNAL-MENUS.md) |
| Build, install and distribute an independent `.hitmenu` | [Extensions](EXTENSIONS.md) |
| Call the public SDK: media, clock, storage, theme and window | [API reference](API.md) |
| Follow code, naming, threading and resource conventions | [Code conventions](CODE-CONVENTIONS.md) |
| Use and maintain repository agent instructions and skills | [Agent workflows](AGENT-WORKFLOWS.md) |
| Create a theme or follow the visual design system | [Themes and design](THEMES.md) |
| Understand the music player, lyrics and capture | [Audio](AUDIO.md) |
| Style the player, artwork, lyrics and visualizer | [Audio theme reference](AUDIO-THEMES.md) |
| Understand clocks, timer, Pomodoro and persistence | [Clock](CLOCK.md) |
| Build, install and play the independent chess menu | [Chess extension](CHESS.md) |
| Style full/compact/world clocks | [Clock theme reference](CLOCK-THEMES.md) |
| Respect memory budgets and interpret measurements | [Performance](PERFORMANCE.md) |
| Add English messages and prepare translation support | [Localization](LOCALIZATION.md) |
| Build, test, package, publish and prepare a commit | [Testing and release](TESTING-RELEASE.md) |
| Diagnose installation, media, CSS and startup problems | [Troubleshooting](TROUBLESHOOTING.md) |

## Recommended reading paths

**Users:** Getting started, then Audio/Clock and Troubleshooting as needed.

**Extension authors:** Menus -> Extensions -> API -> Code conventions -> Themes.
Use the Notes example in `Modules/HIT.Example` and the `HIT.Sdk` project.

**Theme authors:** Themes -> Audio themes -> Clock themes. A theme changes CSS presentation;
it does not need a compiled extension.

**Maintainers:** Architecture -> Code conventions -> Performance -> Testing and release.
Built-in menu source remains the reference for integration details, while the SDK is the public extension contract.

## Version and source of truth

| Item | Version |
| --- | --- |
| Application release | 2v2910 (29 October) |
| Built-in Audio / Clock | 1.2.0 / 1.2.0 (bundled with HIT) |
| Optional Chess package | 1.1.0 |
| Public SDK package | 1.2.0 |
| SDK assembly identity | 1.0.0.0 |
| `.hitmenu` format / `hitContractVersion` | `1` / `"1"` |
| Imported CSS theme contract | 1 |

These versions describe different contracts. A package's own version is independent of all of them.
Current constraints are documented as implemented; planned features are explicitly marked.
The documentation is copied to build/publish output under `Docs/`.
Source-code links target a repository checkout. The published guides are self-contained;
source projects, examples and test harnesses are obtained with the repository rather than shipped as executable output.

## Keeping the documentation complete

Update the relevant reference and example whenever changing a public API, preference, lifecycle,
selector, attribute, manifest field, limit or user interaction. Keep one canonical explanation per subject
and link to it instead of copying conflicting instructions. Preserve historical measurement conditions.
Run [documentation checks](TESTING-RELEASE.md#documentation-checks) after moving or renaming any guide.
