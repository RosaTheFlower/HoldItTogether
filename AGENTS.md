# HIT project guidance

## Start with the documented contract

HIT is a Windows desktop application using Photino, Blazor and WebView2. Read
[Docs/README.md](Docs/README.md) for the documentation index and current versions,
[Architecture](Docs/ARCHITECTURE.md) for ownership, and
[Code conventions](Docs/CODE-CONVENTIONS.md) before changing application code.
Load only the additional guides relevant to the task.

Use `Docs/` as the canonical description of implemented behavior. Check the affected source
alongside its guide. If they disagree, identify the discrepancy and resolve it within the task;
do not silently replace documented behavior or describe a proposal as implemented.
Explicit user instructions determine the requested scope. A local roadmap is planning material,
not a prerequisite for working from a clean checkout.

Keep source identifiers, comments, interface messages and maintained documentation in English.
Preserve user-entered labels and provider metadata in their original language. Follow the user's
language when discussing the work. Route new built-in UI messages through `Text.Get`/`Text.Format`
and `Resources/en.json` as described in [Localization](Docs/LOCALIZATION.md).

## Choose the relevant workflow

Project skills live in `.agents/skills/`. Read the matching `SKILL.md` before using its workflow;
combine skills only when the change crosses their boundaries.

| Work | Skill |
| --- | --- |
| Menu views, settings controls, input, accessibility and localization | [hit-ui](.agents/skills/hit-ui/SKILL.md) |
| CSS contracts, themes, personalization, backgrounds and icons | [hit-themes](.agents/skills/hit-themes/SKILL.md) |
| Media sessions, player, lyrics, artwork, FFT and audio effects | [hit-audio](.agents/skills/hit-audio/SKILL.md) |
| World clocks, timer, Pomodoro, persisted deadlines and notices | [hit-clock](.agents/skills/hit-clock/SKILL.md) |
| Independent Chess module, rules, board, persistence and packaging | [hit-chess](.agents/skills/hit-chess/SKILL.md) |
| Public SDK, extension host, capabilities, events, actions and packages | [hit-extensions](.agents/skills/hit-extensions/SKILL.md) |
| Native window, stacking, focus, DPI, monitors and shell geometry | [hit-window](.agents/skills/hit-window/SKILL.md) |
| Resource sleep, diagnostics, CPU/RAM measurement and optimization | [hit-performance](.agents/skills/hit-performance/SKILL.md) |
| Build/test selection, SDK/package distribution and Windows installer | [hit-release](.agents/skills/hit-release/SKILL.md) |

See [Agent workflows](Docs/AGENT-WORKFLOWS.md) for invocation, coverage and maintenance.

## Respect ownership and stable contracts

- `Program.cs` owns composition, startup and shutdown. Feature services own I/O, domain state
  and validated commands; Razor coordinates presentation. JavaScript owns DOM mechanics.
- `Components/DynamicSphere.razor` owns shared shell behavior and native geometry coordination.
  `Components/HitMenuHost.razor` owns menu identity, retained instances and navigation.
- `Components/HitMenus` contains built-in entry points only. Put supporting views in
  `Components/{Feature}` and services in `Services/{Feature}`. One menu keeps its detail/settings
  views inside that menu. Do not duplicate the host title, pin, close controls or navigation dots.
- `Modules/HIT.Sdk` contains independent public contracts and must not reference the executable.
  `HIT.Example` and `HIT.Chess` are independent extensions; Chess is not a built-in menu.
- Preserve persisted CLR type IDs, package/menu IDs, storage keys and published CSS contracts.
  A title is not an identity. Introduce explicit migrations for identity changes.
- A visited menu stays mounted while hidden; hiding pauses presentation, whereas disabling/removing
  disposes its view. Keep service-owned background work separate from visible-view work.

## Preserve lifecycle, data and presentation

Use immutable snapshots and normalized preferences. Pass cancellation through asynchronous work,
reject stale results and marshal renderer updates with `InvokeAsync`. During disposal, cancel and
await owned work, unsubscribe callbacks, disconnect DOM observers/listeners, then release JS,
native resources and scopes. Do not let one extension cleanup failure block other cleanup.

Keep user state under the documented LocalApplicationData paths or extension `Api.Storage`;
write through temporary files and atomic replacement. Exercise destructive/reset/corruption
cases with disposable profiles. Preserve unrelated user files and the last valid theme on failure.

Use the shared controls, scoped public CSS, theme variables, accessible names, keyboard/focus
behavior and reduced-motion rules in [Design](Docs/DESIGN.md) and [Menus](Docs/MENUS.md).
Do not edit generated `wwwroot/css/menus.css`, introduce `.razor.css`, or move domain/persistence
logic into JavaScript or markup getters.

Pause polling, capture and animation when their consumer is hidden/resting unless the guide
documents independent background ownership. Bound caches, queues and diagnostics. Do not use
forced GC, working-set trimming, WebView process termination or degraded artwork as a RAM policy.

## Validate the affected behavior

Commands run from the repository root on Windows. The baseline application build is:

```powershell
dotnet build HoldItTogether.csproj -c Release
```

Select focused checks from [Testing and release](Docs/TESTING-RELEASE.md); prepare its documented
fixtures before running browser tests. Add behavior-focused regression coverage for consequential
changes rather than tests that repeat private implementation. Documentation-only changes need
documentation checks, not an application build:

```powershell
node Tools/Check-Documentation.mjs
```

If a running HIT locks build output, use an isolated output directory and `CopyRetryCount=0`
instead of stopping the user's instance. Keep native fullscreen, real audio and Explorer-drop
acceptance separate from what headless/browser checks actually establish.

Update the relevant `Docs/` guide and example when changing a public API, capability, preference,
selector, interaction, persistence format or limit. Technical documentation stays in `Docs/`;
the root `AGENTS.md` and `.agents/skills/{name}/SKILL.md` are instruction-file exceptions.
Keep generated `bin`, `obj`, `artifacts`, logs and personal profiles out of source changes.

Report the resulting behavior, checks actually run and material limitations. Do not claim a
package, installer, live-game check or performance improvement without corresponding evidence.

Release labels derive from HitReleaseGeneration and HitReleaseDate; keep SDK/package versions and persisted identities independent. Commit review includes untracked source and shared-control/Docs/skill changes, while the local roadmap, generated outputs and user/IDE state remain ignored. Clean only verified workspace-local generated paths and preserve outputs used by a running instance.
