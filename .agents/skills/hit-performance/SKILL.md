---
name: hit-performance
description: "Investigate or optimize HIT CPU/RAM, resource-saving sleep, hidden-view lifecycle, automatic diagnostics, scenario exports, caches or high-frequency rendering. Use for measured performance work; avoid attributing host/WebView memory to a feature without evidence."
---

# HIT resource and diagnostics workflow

## Read the measurement contract

Read [Performance](../../../Docs/PERFORMANCE.md) and relevant
[Troubleshooting](../../../Docs/TROUBLESHOOTING.md) sections. For policy/preferences, read
[Settings](../../../Docs/SETTINGS.md); for extension delivery, read
[API](../../../Docs/API.md). Load the feature guide for the actual producer/consumer under study.

## Measure before attributing

1. Define a repeatable scenario: same build/configuration, warmup, theme, media/cover, extensions,
   window state and duration. Record visible, fully collapsed, compact-menu and covered-fullscreen
   states separately; these are not interchangeable resource-policy inputs.
2. Use the documented Info/export/scenario recorder rather than ad hoc process-name totals.
   Separate host, descendant WebView2 and combined figures. Preserve private-working-set versus
   fallback-working-set definitions and partial flags; committed memory is a different metric.
3. For CPU, preserve PID/start-identity checks, elapsed deltas and processor normalization.
   Keep automatic diagnostics and Info sampling baselines independent. Use comparable medians,
   ranges and trends; one high sample or a synthetic test is not proof of a leak or improvement.

## Optimize the actual owner

1. Trace retained work to its lifetime: renderer, JS observer/animation, shared service, native
   capture lease or extension module. Pause view-owned work while hidden/resting. Retain
   documented independent clock deadlines and other service-owned background responsibilities.
2. Preserve resource-saving opt-in and grace/wake semantics. Fully collapsed sphere differs from
   an open compact menu; fullscreen coverage and FORCE affect eligibility. Wake promptly without
   allowing a late completion to restart disposed polling.
3. Bound caches, channels, retained images, reports and logs by documented capacity/lifetime.
   Reuse hot-loop buffers, deduplicate interop transfers, release unneeded decoded artwork and
   disconnect observers/listeners. Avoid per-frame DOM writes and repeated unchanged snapshots.
4. Keep diagnostics local, bounded and free of media/provider payloads. Preserve the documented
   automatic cadence/rotation, complete-file replacement, nonfatal access failures and shutdown.
   Collection must not wake the feature work that resource sleep suspended.
5. Do not force GC, trim working sets, terminate WebView processes or reduce artwork quality to
   manufacture lower RAM figures. Do not treat a process export as evidence of a specific
   renderer allocation without additional measurements.

## Verify lifecycle, then compare the workload

Choose host diagnostics/resource checks and `resource-saving.mjs`, `artwork-fade.mjs` or the
feature tests from [Testing and release](../../../Docs/TESTING-RELEASE.md). Include repeated
sleep/wake, interrupted grace, disable/dispose and pending-work cases for lifecycle changes.

Repeat the original live workload to support a performance claim. Report measurement conditions,
which metric changed, observed range/trend and remaining uncertainty. If only structural tests
ran, report lifecycle correctness without claiming a total-RAM reduction. Update Performance
and any changed policy/API guide, preserving historical measurement conditions.

## Current contract checkpoints

Separate configurable image file-size policy from decoded browser memory: unlimited imports stream through bounded buffers but do not imply unlimited decoded-memory safety. Gallery decoding is lazy and slideshow work follows open/resting lifecycle. Bounded page history and entrance observers must release on removal and must not replay on high-frequency status updates.
