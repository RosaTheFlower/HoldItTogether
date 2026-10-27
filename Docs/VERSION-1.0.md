# HIT release contract

The public application version is `v2710`: `v` + two-digit day + two-digit month, currently 27 October.
`HitReleaseDate` in `HoldItTogether.csproj` is the single release-date source (`2026-10-27`).
It generates the displayed/product informational version. Windows file/package metadata uses the numeric date
`2026.10.27.0` / `2026.10.27`; the application assembly identity stays `1.0.0.0` for compatibility.
Changing the release date generates the next `vDDMM` label. Builds on the same release date share that label.
Update checks order valid date tags by calendar date, using the GitHub release's publication year across New Year.
Legacy semantic tags are ignored by the date-version update comparison.
The SDK package is `1.0.2`; its assembly identity remains `1.0.0.0`.
The external manifest uses `formatVersion: 1` and `hitContractVersion: "1"`.
Do not change these values together automatically: each protects a different compatibility boundary.

## Included features

- Transparent/chromeless sphere, pointer-responsive mascot, click-to-pin, explicit close/unpin, drag and edge anchoring.
- Scroll/dot menu navigation, persisted custom order, built-in disabling, protected final Settings menu.
- Windows media mini-player, supported playback commands, synced online lyrics with seeking/source links,
  independent background spectrum and cover/palette modes.
- System/world clocks, optional secondary clock, timer and Pomodoro with persistent deadlines and notices.
- Three bundled protected themes, personal CSS import/delete/live edits and reduced-motion preference.
- `.hitmenu` installation by picker, file drop or Inbox; versioned updates, explicit rollback and deferred deletion.
- Independent SDK, local NuGet package generation, menu template, Notes example and extension integration/browser tests.
- English interface and a source-message catalog for future localization.
- Bounded lyrics cache, metadata-cached CSS, visibility-aware work and reusable visualizer buffers.
- Shared configurable current-menu compaction after pointer departure, experimental Force Always on Top and obsolete extension-version cleanup.
- Native input regions restricted to the surface's outer bounds while preserving CSS corner antialiasing, blocked browser-zoom shortcuts and 100% zoom reset on opening.
- Independent optical/color artwork effects with real FFT-driven OnBeat envelopes.

## Separately distributed modules

[Chess](CHESS.md) is an optional `.hitmenu` extension with its own manifest, package version, engine, settings and score-reset confirmation.
Its source and tests are committed in this repository under `Modules/`; it is not a built-in feature or a dependency of the HIT host.
Building the solution also builds the module projects for development. Publishing `HoldItTogether.csproj` does not install or bundle Chess.

## Defined limitations

| Area | 1.0 behavior |
| --- | --- |
| New extension code | Activates immediately after installation/enabling |
| Updated loaded code | Automatically restarts HIT after graceful shutdown |
| Disabled loaded menu | Component removed immediately; module services finish at exit |
| Loaded DLL unloading | Cooperative at shutdown; no forced live replacement |
| Extension execution | Trusted .NET/JS in the HIT process; API capabilities are not OS sandboxing |
| Per-extension RAM quota | Not enforced; resource ownership/cancellation is contractual |
| SDK distribution | Can be packed locally; no public package feed is published by this repository workflow |
| Translation packages | Planned; English catalog exists, runtime language import/selection does not |
| Loose Razor/source import | Not compiled by the installer; authors build their package |
| Media features | Limited by the selected app's Windows media session capabilities |
| Lyrics | Only real timed results; online provider availability/matching can fail |
| Spectrum | Default-output mix, not isolated to one app |
| OnBeat | Uses that same output mix; no per-track/source isolation |
| Force Always on Top | Experimental reassertion without activation; cannot guarantee visibility over exclusive fullscreen/system surfaces |
| OS notifications | SDK notifications are in-HIT notices, not Windows toast notifications |

## Compatibility and release policy

Persisted IDs and JSON keys are independent of UI labels. Clock/Settings CLR IDs now use English names with registry migration;
keep bundled theme IDs stable. Keep extension data outside versioned installation directories.
Breaking SDK changes require a deliberate contract version and migration/rejection policy.
Package updates use a higher numeric package version and retain the previous version for rollback.
Rollback changes selected code; an extension is responsible for compatible user-data migrations.

Do not label a capability as implemented until the service, UI, lifecycle, public styling and validation exist.
Document any release limitation in the relevant reference and this file when it affects users or package compatibility.

## Release checklist

Use the commands and acceptance matrix in [Testing and release](TESTING-RELEASE.md).
Confirm clean builds, package imports, blocked invalid archives, SDK-only authoring, navigation after reorder,
public CSS edits, all bundled themes, user-data preservation and the complete published `Docs/` directory.
Version control includes source, manifests, SDK, examples, tests, tools and documentation; generated output stays ignored.
