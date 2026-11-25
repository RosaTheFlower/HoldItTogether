# HIT 3v2511 — 25 November 2026

## Languages

- Added Info > Languages with embedded English and Brazilian Portuguese. English remains the default.
- Added validated JSON language imports, English fallback and a saved selection applied on restart.
- Translated all 674 current source-catalog messages into Portuguese. Some legacy diagnostics and independent extension strings remain outside the host catalog.
- Aligned language selection/import with shared settings controls, keyboard focus, loading feedback and theme colors.

## Configuration and appearance

- Added topic-specific header icons that also recognize localized built-in titles.
- Grouped sphere behavior, placement, inactive appearance and transparency under HIT Sphere.
- Expanded personalization categories, RGBA color controls, installed-font selection and visual profiles.
- Added local video backgrounds with posters, independent closed/inactive sphere appearance and playback preferences.
- Preserved inherited theme properties when only selected appearance properties are edited.
- Refined shared combobox sizing, explicit selection confirmation, page history, full-width actions and background-gallery removal.

## Compatibility

- Fixed settings reset when no Languages directory exists yet.
- Bundled current-build release notes for offline reading, alongside published GitHub notes.
- Isolated regression-test language profiles and scoped the Chess startup-failure fixture to Chess.

- Application label: 3v2511; Windows file version: 2026.11.25.0.
- SDK remains 1.2.0 with assembly identity 1.0.0.0. Package and theme contracts remain version 1.
- Chess remains a separately installed optional extension, version 1.1.1.
- Existing settings, menu/package IDs, theme IDs and stored media references are preserved.

These notes describe the local build. Installer creation, GitHub publication and live desktop/audio acceptance are separate release steps.
