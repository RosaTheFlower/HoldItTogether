# HIT 1.0 release contract

Application version is `1.0.0`, defined in `HoldItTogether.csproj`.
The SDK package is `1.0.1`; its assembly identity remains `1.0.0.0`.
The external manifest uses `formatVersion: 1` and `hitContractVersion: "1"`.
Do not change these values together automatically: each protects a different compatibility boundary.

## Included in 1.0

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

## Defined limitations

| Area | 1.0 behavior |
| --- | --- |
| New/updated extension code | Activates after restart |
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
| OS notifications | SDK notifications are in-HIT notices, not Windows toast notifications |

## Compatibility and release policy

Persisted IDs and JSON keys are independent of UI labels. Preserve built-in CLR IDs and bundled theme IDs
when renaming visible titles. Keep extension data outside versioned installation directories.
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
