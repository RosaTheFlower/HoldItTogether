# Memory and performance

The host's policy is to avoid unnecessary work/retention while preserving real features, image quality and visualizer rate.
Extension authors must follow the same ownership rules; trusted plugin code does not have an enforced RAM quota.
Measurements below record the controlled optimization review from 2026-10-26, not a new total-RAM benchmark for release 1.0.

## Current policies

SDK event subscriptions use one pending value per observer, replace stale queued values, and are
limited to 32 per package. Media is queried once per shared bridge interval (500 ms), not per extension.
SDK spectrum consumers share the built-in capture through leases; the last owner releases it. The
bridge parks when no eligible media/spectrum subscribers remain, during sleep and while native-hidden.
Host/appearance updates are event-driven and coalesced across the WebView bridge. Callbacks remain
trusted in-process code; lifecycle cancellation cannot forcibly interrupt a running observer.

Idle transparency is a separate opt-in presentation behavior: one departure timer and a CSS opacity
transition, with immediate interaction recovery. It has no recurring animation loop or renderer IPC
and does not disable native hit testing. Resource sleep and idle opacity can operate together.

Opt-in control (enabled by default): [automatic resource saving](SETTINGS.md#automatic-resource-saving).
After a five-second idle grace, a collapsed sphere or a fully fullscreen-covered host without FORCE
can pause visual work. Open compact menus remain active when visible. Wake restores media polling
and view work. Artwork, spectrum buffers/effects and lyrics references/cache are released; no forced
GC, WebView process termination or working-set trimming is used. Theme monitor timer ticks skip
file reads during rest, while the lightweight pointer/coverage check remains active for wake.
Domain clock deadlines and extension-owned necessary workers are not suspended. No measured
total-RAM reduction is claimed for this change; compare host plus WebView2 under the same workload.
Artwork reveal/release uses a 650 ms opacity fade; outgoing image/effect references are retained
only until the fade finishes. Rapid reopen invalidates pending cleanup. Reduced motion skips the
fade, and component disposal releases immediately. Spectrum capture still stops without waiting
for the visual fade.

| Area | Policy |
| --- | --- |
| Global CSS | Cache only base and selected snapshots; check length/write/creation metadata before reread/hash |
| Theme application | Coalesce overlapping requests; keep unchanged picker lists; update visible errors/options only when changed |
| Theme/extension CSS edits | Global monitor 1 second; visible extension styles 2 seconds; unchanged files are not retransmitted/recompiled |
| RAM/CPU display | Mount under Info -> Performance and sample only while that detail is visible; leaving it cancels/awaits the display monitor; separate host, descendant WebView2 and group total; consistent private resident/working-set fallback and partial-sample labels |
| Lyrics cache | At most 40 entries / estimated 4 MiB; positive 60 minutes, negative 2 minutes; prune expired entries |
| Large lyrics | Return full supported result without requiring it to remain cached |
| Lyrics parsing | 3,000 rows / 5,000 timestamps, bounded provider response; streaming line enumeration |
| Lyrics rendering | Same-verse position changes do not rebuild the button tree; changed verse/state still updates |
| Hidden lyrics | Remove view/observers, retain current model for resumption |
| Artwork | Reuse active source over JS; release inactive cover references; no downsampling/quality loss |
| Color extraction | Release temporary image elements after analysis |
| JS modules | Share pending import per view and dispose references/observers on teardown |
| Tooltips | Process changed/added subtrees instead of rescanning all UI for every mutation |
| Time zones | Lazy option catalog; cache used zones with a bound; folded settings do not render hundreds of options |
| Spectrum | 32 real bands / up to 20 inputs per second; one full-DPI canvas with bounded interpolation; no per-band DOM mutation; skip identical FFT frames |
| OnBeat | Shares spectrum capture; 85 ms attack / bounded decay; no idle synthetic beat loop; frames only during drift or an active envelope |
| Cover effects | Reuse one cover URL; construct optional layers/graphs only when configured; keep temperature/tint separate from distortion/turbulence |
| Optical writes | Cache unchanged filters/SVG attributes/layer visibility; drift changes compositor transform only; beat filters update at most about 30 Hz |
| Audio rendering | Position-only polling does not redraw settings or inactive global covers; disabled timelines skip JS synchronization |
| Compact policy | Shared 0-10-second departure timer only for Player/Lyrics, board and main clock; page-specific Audio hide switches; settings/forms never compact |
| Experimental topmost | 500 ms native UI-thread timer only while Force Always on Top is enabled; no focus activation |
| Ordinary topmost recovery | Coalesced native-event state checks; reapply only if the native topmost flag is missing; no polling timer; latest 32 recovery diagnostics retained in memory |
| Fullscreen placement | Foreground/minimize/location WinEvent hooks with early filtering; hooks released on native teardown. A 16 ms native timer exists only during the 420 ms placement transition. FORCE's timer can refresh detection but skips repeated Z-order writes during fullscreen. |
| Extension persistence | 1 MiB/value and 16 MiB/package JSON disk quota; these are storage limits, not memory limits |
| Extension Inbox/logs | 2-second stable-file checks; bounded scan/history/log messages and log rotation |

Capture and ordinary view polling stop while hidden unless an enabled OnBeat global cover is visible on another menu.
Closing HIT stops capture. Clock deadlines deliberately run independently of the view.
Global music palette metadata can continue at about two-second intervals to keep other menus' cover/colors current.
These exceptions have explicit owners and shutdown paths.
Color adjustments require the persisted Enable color adjustments switch, off by default. Disabled colors add no CSS/SVG filter work.
Steady artwork drift is a compositor animation, with zero per-frame JS/DOM writes. Spectrum tests also verify zero DOM mutations
while drawing changing FFT input, full native-DPI buffers and release on disposal. These are focused regression checks, not a measured FPS claim.

No production forced GC, working-set emptying, image-quality reduction or WebView process termination is used to make RAM look lower.
Layouts must not add resize feedback loops or use animation timers to fabricate data.

## Historical controlled measurements

Same Windows machine, Release/.NET 8, same data before/after. Temporary local harnesses were below obj;
they are historical measurements, not a committed benchmark suite available in every checkout.

| Scenario | Before | After |
| --- | ---: | ---: |
| Allocations, 1,000 unchanged base + OLED queries | 153.52 MiB | 0.60 MiB |
| Allocations, 100 queries with 512 KiB personal CSS | 256.73 MiB | 0.072 MiB |
| Allocations, 100 positions within one verse of a 300-verse view | 12.52 MiB | 0.051 MiB |
| Renders for those 100 positions | 100 | 0 |
| Zone options in two folded clock settings | 435 | 0 |
| HTML for those settings | 40,820 bytes | 2,654 bytes |
| Retained cache heap after 40 extensive synthetic lyrics | 60.02 MiB | 3.97 MiB |

Synchronous allocations used GC.GetAllocatedBytesForCurrentThread; render allocations used GC.GetTotalAllocatedBytes.
Cache retention used GC.GetTotalMemory after collection in the test only. Each synthetic lyric had 2,800 long verses;
this protects against growth and does not represent normal song size. The cache byte budget is an estimate of managed ownership.

Allocation totals and retained managed heap are not total resident application RAM.
No live HIT process was running at the original baseline, so the review did not measure a total working-set before/after.
WebView2, decoded images, JIT and the active track contribute additional memory.

## Measuring a new change

Use the same Release build configuration, warm-up duration, source app, track/cover, menu order, theme and visible state.
Record whether WebView child processes are included. Compare zero/one/several enabled extensions and repeated navigation/disable cycles.
Separate startup/JIT allocation, transient allocation, stable retained heap, native resources and total process RAM.
Do not compare the Settings figure to a task-manager group total that includes WebView children.
The historical Settings figure above was host-only; the current Info -> Performance view also has an explicitly
labeled group total. Shared working-set fallback is not unique physical RAM. CPU deltas match PID
and process start time; new/exited processes can make short intervals less representative.

## Repeatable scenario recording

The headless recorder samples an explicitly selected running PID and its descendant WebView2
processes, once per second, without opening Info or changing HIT's state. Use the published DLL:

```powershell
dotnet artifacts/hit-v2-diagnostics/HoldItTogether.dll --hit-profile 12345 30 sphere-rest obj/performance/sphere-rest.json
```

Replace 12345 with the target HIT PID. Duration is 2-600 seconds; reports mark incomplete recordings
and partial samples. Recording stops if the root exits or its PID is reused. Choose a different output
file per run. The recorder has no background residency after exit. It records process metrics and a
user-supplied scenario label, not media metadata or browser content.

Use a warmed Release build with identical source, artwork, theme and extension set. Record 30 seconds
each for: sphere with saving OFF; sphere with saving ON after the five-second grace; open menu;
visible compact menu; Audio visualizer/effects; restored Audio after 20 collapse/reopen cycles. Repeat
with no extensions and the normal extension set. Compare medians/ranges, process count and handle
trends; do not infer leaks from startup/JIT or one retained-memory sample. Keep the pointer away from
the resting sphere. Document the actual state and settings alongside each label.

Current validation covers native sampler handle ownership and 12 rapid component sleep/wake cycles,
including no polling restart after disposal. Live Release scenario comparisons have not been collected
in this checkout: no running HIT instance was available during this work. No RAM/CPU savings percentage
or total-process memory plateau is claimed. The broader live baseline remains an acceptance item.

Sleep now skips idle renderer dispatches from the 50 ms native pointer loop; native wake checks remain.
Diagnostics keep only 128 structured events in memory. Automatic reports sample and write approximately
once per minute, including during rest, into 30 rotating slots under Diagnostics/Automatic. This adds
one low-frequency native sample and a bounded JSON write per interval without rendering or waking the
interface. CPU uses its own interval baseline; Info remains independent. Manual exports are separate.
Nothing is uploaded. The Chess memory investigation is deferred while these reports collect a timeline.

For an extension: navigate away, collapse HIT, disable its menu and restart with it disabled.
Confirm view work cancels when hidden, resources release on disposal and module workers stop on module lifetime cancellation.
Some loader/native memory can remain until exit; cooperative unload is not a guarantee of immediate RAM reclamation.
Fix ownership before proposing artificial collection/trim policies.

Configuration history is capped at 50 session visits per retained menu and stores stable IDs rather than serialized component/domain state. Menu entrances are event-driven, cancel on hidden/removal/reduced motion and do not restart on ordinary media/clock updates. Unlimited image import size is a file policy, not a decoded-memory guarantee; imports stream through bounded buffers and preserve the separate dimension limit.
