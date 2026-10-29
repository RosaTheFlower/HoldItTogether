---
name: hit-audio
description: "Implement or diagnose HIT media sessions, player controls, lyrics, cover artwork, music palettes, shared output capture, FFT spectrum or OnBeat effects. Use for audio behavior and lifecycle; pair with hit-extensions for public media contracts or hit-themes for CSS contracts."
---

# HIT audio workflow

## Read and locate ownership

Read [Audio](../../../Docs/AUDIO.md), then the affected section of
[Audio themes](../../../Docs/AUDIO-THEMES.md). Consult [Performance](../../../Docs/PERFORMANCE.md)
for capture, polling or cache changes, and [API](../../../Docs/API.md) for exposed snapshots,
commands or spectrum leases. Use [Troubleshooting](../../../Docs/TROUBLESHOOTING.md) for
provider/session failures before changing fallback behavior.

## Preserve media correctness and resource ownership

1. Trace Windows integration/service snapshots, renderer state and JS/CSS presentation separately.
   Keep external operations out of rendering/getters and update renderer-owned state via
   `InvokeAsync`. Cancel superseded requests and reject stale session/track generations.
2. Honor each source's supported commands and current identity. Unsupported actions must remain
   explicit; do not synthesize global media-key presses or infer a video capability from a title.
   Distinguish provider limitations from a host regression.
3. For lyrics, preserve track matching, provider attribution, timed/untimed distinctions,
   Ctrl/source and seek behavior. Keep positive/negative caches bounded according to the guide.
   Do not fabricate lyrics, timing or a successful provider result on a miss.
4. For covers/palettes, retain independent screen-local, Lyrics, other-menu and global choices.
   Cache/deduplicate image transfers and release expensive view resources during sleep/disposal.
   Preserve the documented fade on artwork disappearance/reappearance and reduced-motion behavior.
5. For spectrum, use actual default-output loopback FFT through the shared capture owner.
   Acquire/release consumer leases; one departing consumer must not stop another's capture.
   Preserve the documented band contract, cadence and full-DPI canvas. Do not add microphone
   capture, raw PCM retention or random animation as an audio-level substitute.
6. For OnBeat/effects, preserve silence/steady-tone rejection, attack/decay and independent
   optical/color paths. Reuse hot-loop buffers, avoid per-frame DOM churn and keep static
   filters unchanged during animation. Synthetic FFT belongs in tests, not production capture.
7. Pause view polling/capture when hidden or resource-resting, while retaining documented
   service-owned metadata work. Cancellation/disposal must prevent a late wake from restarting
   a disposed view. Reopening must resume from current service state.

## Verify the changed path

Use [Testing and release](../../../Docs/TESTING-RELEASE.md) for fixtures and choose among
`audio-effects.mjs`, `spectrum.mjs`, `artwork-fade.mjs`, `resource-saving.mjs` and audio portions
of `menus.mjs`. Use `--sdk-spectrum-checks` only when validating actual output-device ownership;
record SKIP as a limitation if the device is unavailable.

Check rapid source/track changes, unsupported commands, silence, no artwork/lyrics, hidden/resting
cycles and pending work during disposal as relevant. Distinguish synthetic/browser results from
real media-provider/device acceptance. Update Audio/API/theme guides for any changed contracts.

## Current contract checkpoints

Lyrics layout choices preserve line timestamps, seek capability and untimed fallback. Word rise estimates presentation timing from the first two thirds of a line interval; never describe it as true word-level vocal alignment. Animate only current-line word spans, synchronize pause/seek without rebuilding the tree, and release handles/listeners on hide/disposal. Use lyrics-layouts.mjs after generating host fixtures for direction, preview radios, reduced motion and lifecycle coverage.

Preserve the separate artwork/background blend switch and 0-100% cover opacity. Exclusive mode suppresses the personal layer without disposing it; cover removal restores it during the fade. Lyrics may be timed or explicitly untimed; only timed results permit seeking/following. YouTube matching uses normalized metadata, not fingerprinting; do not advertise a private Spotify lyrics endpoint as implemented.
