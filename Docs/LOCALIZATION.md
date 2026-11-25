# Languages and source messages

English (en-US) is the default interface language and canonical source/documentation language.
Portuguese (Brazil, pt-BR) is also embedded. Open **Settings > Info > Languages** to select
a language or install a local JSON translation. **Apply and restart HIT** saves the choice
and requests a graceful restart. Import alone does not change the active language. If restart
fails, the saved selection takes effect on the next application start.

The detail page uses the shared settings wheel combobox and full-width import/apply actions
at the bottom of the options pane. Explanatory text follows Show option descriptions. The import
action overlays its accessible file input instead of exposing native file-control chrome. Both
actions use the shared busy indicator; feedback reserves space above them and errors are announced.
The native select publishes data-hit-select-value so an import updates the shared picker. Layout
stays within the existing workspace and inherits the active theme's control colors.

Resources/en.json and Resources/pt-BR.json are embedded. HitText resolves selected overrides,
then English, then the source key. Text.Culture follows the active language. User labels,
provider metadata, time-zone IDs, package IDs, CLR IDs and persistence keys remain unchanged.

## Translation format

A UTF-8 JSON file uses format version 1 and exactly these four fields:

```json
{
  "version": 1,
  "locale": "es-ES",
  "name": "Español",
  "messages": {
    "Settings": "Configuración",
    "Open {0}": "Abrir {0}"
  }
}
```

Use a recognized canonical lowercase two/three-letter language code, optionally followed
by an uppercase two-letter region. Names contain 1–64 characters without control characters.
Limits: 1 MiB per file, 32 imports, 8,192 characters per message. Only known English source
keys are accepted. Duplicate keys, blank messages, malformed composite formats and changed
numbered argument occurrences or format specifiers are rejected. Missing messages fall back to English. Catalogs
contain data only and Razor renders translations as text. Imports replace the same locale
atomically after validation. Embedded English and Portuguese cannot be replaced.

Copy Resources/pt-BR.json as a complete example, change locale/name and translate its values.
Resources/en.json lists authoritative keys. Later releases can add messages; partial catalogs
retain English fallback. Keys from newer releases are rejected if unknown to this version.

## Persistence and reset

HitLanguageStore owns catalogs and selection.json under
%LOCALAPPDATA%/HoldItTogether/Languages. Invalid catalogs are skipped; missing or invalid
selection falls back to English. Reset all settings clears selection and preserves imported
files; Delete everything removes imports. Embedded languages remain available.

## Maintaining messages

Use Text.Get("Settings") for labels and accessible names, or Text.Format("Open {0}", name)
for complete sentences with numbered placeholders. The Text alias is supplied by
Components/_Imports.razor. Add English source keys to Resources/en.json and translate them
in Resources/pt-BR.json. Keep internal identities out of the catalog.

Portuguese covers all current source-catalog keys. Some service diagnostics and historical
literal accessible names still bypass Text; this is not complete translation of every possible
diagnostic. Independent SDK extensions own their strings; no SDK localization API is exposed.
Check long labels, keyboard focus and tooltip-off use in the desktop application.

## Validation

Focused disposable-profile checks cover embedded coverage, default selection, import persistence,
invalid metadata and placeholders, built-in protection, corrupt-file fallback and both reset modes.

```powershell
dotnet run --project Modules/HIT.Example.Tests/HIT.Example.Tests.csproj -c Release -- --language-checks
node Tools/Check-Documentation.mjs
```
