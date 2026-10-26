# English source messages and future language packs

The canonical interface/source documentation language is English (`en-US`).
Existing user-entered labels, provider metadata and Windows time-zone display strings retain their source language.
Stable built-in CLR IDs, theme IDs and persisted JSON keys are not translated/renamed with visible labels.

`Resources/en.json` is embedded as `HoldItTogether.Resources.en.json`.
`Services/Localization/HitText.cs` loads it lazily. Components receive the `Text` alias from `_Imports.razor`.

```razor
<button type="button" aria-label="@Text.Get("Open settings")">
    @Text.Get("Settings")
</button>
```

Source-message strings act as keys. Missing entries fall back to the key rather than displaying an empty label.
For complete sentences use `Text.Format("Move {0} up", name)` and numbered placeholders,
not concatenations whose word order cannot be translated. `Text.Culture` currently returns en-US.
Keep labels/tooltips/accessible names useful independently; disabling hover tooltips must not remove accessibility names.

## Adding a message

1. Choose a clear English source message and use it through Text.Get/Format.
2. Add its key/value to en.json with matching numbered placeholders.
3. Keep IDs, enum values, API names, CSS selectors and storage keys outside the translated catalog.
4. Check long-label layout, keyboard focus, dialogs and plural/format behavior.
5. Update tests that intentionally assert a user-visible label.

Some service diagnostics and dynamic sentences remain source literals. A complete localization delivery must route those
remaining messages and any historical generated fallbacks through the same mechanism before claiming full language coverage.
SDK extensions currently own their own visible strings; no public translation service is exposed in SDK 1.

## Future package boundary

Runtime language import/selection is not implemented in 1.0. A future translation package should be data-only:
locale, catalog version, message overrides and optional extension-owned namespaces.
Validate total size, supported keys and placeholder compatibility; retain English fallback and notify views to rerender on change.
No executable code, system API access or theme mutation belongs in a translation file.
Define migration/versioning before shipping a format; do not present a proposed language ZIP as an installable `.hitmenu` feature.
