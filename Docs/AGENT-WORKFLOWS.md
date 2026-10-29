# Repository agent workflows

HIT keeps durable project instructions in [AGENTS.md](../AGENTS.md) and task-specific workflows
in `.agents/skills/`. The technical guides in this directory remain the canonical references;
skills select and apply those contracts rather than maintaining separate copies of them.

## Scope and discovery

The root `AGENTS.md` provides project-wide ownership, language, lifecycle and verification rules.
Each skill has a required `SKILL.md` containing its name, triggering description, focused workflow
and links to relevant guides. Its `agents/openai.yaml` provides display metadata and an invocation
example. These files apply to this repository; no user-wide installation or connector is required.

Codex discovers repository skills from `.agents/skills/`. Invoke one explicitly by its name,
for example `$hit-extensions`, or describe a task that matches its description. If the client has
not refreshed the skill catalog, restart the session. See the official
[skill discovery reference](https://learn.chatgpt.com/docs/build-skills) and
[project guidance reference](https://learn.chatgpt.com/docs/customization/overview) for the
client mechanism; HIT implementation rules come from the guides below.

## Workflow coverage

| Skill | Use when | Primary technical references |
| --- | --- | --- |
| [hit-ui](../.agents/skills/hit-ui/SKILL.md) | Changing views, settings controls, gestures, navigation or accessibility | [Menus](MENUS.md), [Design](DESIGN.md), [Settings](SETTINGS.md), [Internal menus](INTERNAL-MENUS.md), [Localization](LOCALIZATION.md) |
| [hit-themes](../.agents/skills/hit-themes/SKILL.md) | Changing public CSS, themes, personalization, backgrounds, exports or icons | [Themes](THEMES.md), [Design](DESIGN.md), [Settings](SETTINGS.md), [Audio themes](AUDIO-THEMES.md), [Clock themes](CLOCK-THEMES.md) |
| [hit-audio](../.agents/skills/hit-audio/SKILL.md) | Changing sessions, controls, lyrics, covers, palettes, capture or effects | [Audio](AUDIO.md), [Audio themes](AUDIO-THEMES.md), [API](API.md) |
| [hit-clock](../.agents/skills/hit-clock/SKILL.md) | Changing world clocks, timer, Pomodoro, deadlines or notices | [Clock](CLOCK.md), [Clock themes](CLOCK-THEMES.md), [API](API.md) |
| [hit-chess](../.agents/skills/hit-chess/SKILL.md) | Changing the independent Chess module | [Chess](CHESS.md), [Menus](MENUS.md), [Extensions](EXTENSIONS.md) |
| [hit-extensions](../.agents/skills/hit-extensions/SKILL.md) | Changing SDK contracts, capabilities, events/actions, package loading or isolation | [API](API.md), [Extensions](EXTENSIONS.md), [Architecture](ARCHITECTURE.md) |
| [hit-window](../.agents/skills/hit-window/SKILL.md) | Changing native stacking, focus, fullscreen placement, DPI, bounds or hit regions | [Architecture](ARCHITECTURE.md), [Menus](MENUS.md), [Settings](SETTINGS.md), [Troubleshooting](TROUBLESHOOTING.md) |
| [hit-performance](../.agents/skills/hit-performance/SKILL.md) | Changing resource sleep, diagnostics, sampling or measured resource use | [Performance](PERFORMANCE.md), [Settings](SETTINGS.md), [Troubleshooting](TROUBLESHOOTING.md) |
| [hit-release](../.agents/skills/hit-release/SKILL.md) | Selecting verification or producing SDK/module/application/installer output | [Testing and release](TESTING-RELEASE.md), [Installer](INSTALLER.md), [Release scope](VERSION-1.0.md) |

All workflows inherit the root guidance and [Code conventions](CODE-CONVENTIONS.md).
The [Getting started](GETTING-STARTED.md) guide describes user-visible setup and navigation;
consult it when a task changes onboarding or the behavior described to users.

## Selecting a workflow

Start with the skill for the affected owner. Add another only when the task changes a contract
owned by that area. For example:

- Adding an optional SDK event uses `hit-extensions`; include `hit-audio` if it changes shared capture.
- Adding a setting uses `hit-ui`; include `hit-themes` if it changes appearance persistence/export.
- Fixing compact-window placement uses `hit-window`; include the feature skill if its markup changes.
- Investigating RAM uses `hit-performance`; include `hit-chess` only if evidence leads to module work.

Do not read every skill or run every test for every task. Read the selected skill's required
references, follow its conditional references as needed, and use the documented checks relevant
to the resulting behavior. Instructions do not imply permission to publish externally or access
unrelated accounts, nor do they require an extra approval step for ordinary authorized edits.

## Keeping instructions accurate

1. Update the canonical technical guide and affected example when behavior or a public contract changes.
2. Update a skill only if its routing, decisions, invariants or verification path changes.
   Keep detailed signatures, numeric limits and exhaustive selector tables in their canonical guides.
   The UI skill records shared workspace conventions; [Design](DESIGN.md) owns their dimensions,
   markup, controls and lifecycle, and [Menus](MENUS.md) owns navigation behavior.
3. Update `AGENTS.md` for project-wide guidance or ownership changes, and this index for skill
   additions, removals or renamed responsibilities. Do not turn temporary investigations or a
   local roadmap into unconditional project rules.
4. Keep each skill instruction-only unless an executable helper or asset has a concrete purpose.
   Use lowercase hyphenated names matching their directory. Keep triggering descriptions specific
   enough to distinguish neighboring workflows and metadata prompts consistent with the name.
5. Run [documentation checks](TESTING-RELEASE.md#documentation-checks). Review sample tasks
   against the routing descriptions and check that the workflow leads to the documented owner,
   lifecycle and validation path. A structural check cannot establish behavioral skill quality.

Technical Markdown stays under `Docs/`. The checker permits only the root `AGENTS.md` and
`.agents/skills/{name}/SKILL.md` as instruction exceptions and validates their local links too.
The repository uses a deliberately small frontmatter format: `name: hit-name` followed by a
single-line JSON-quoted `description`. JSON quoting is valid YAML and keeps the checked format
unambiguous without adding a YAML dependency to the documentation tool.

Published documentation continues to copy `Docs/**/*.md`; agent instructions are checkout
tooling, not an application runtime dependency. Links from guides to source or instruction files
refer to a repository checkout, as other source-code links do.

## Current maintenance baseline

The application label is 2v2910; SDK and extension-package versions remain independent. UI work uses the shared inline controls, page/history identities, Images policy and reduced-motion-aware DOM transitions from Design/Menus. Release review checks tracked and untracked source together; keep the local roadmap, profiles, bin/obj/artifacts and IDE state out of commits. Read the matching domain workflow rather than duplicating runtime specifications in skills.
