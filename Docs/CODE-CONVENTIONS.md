# Code and contribution conventions

These rules apply to new application code, SDK changes and examples. Existing persisted identifiers
may retain historical Portuguese names for compatibility; new public code, UI strings and documentation use English.

## Naming and structure

Use PascalCase for public types/members, camelCase for parameters/locals and `_camelCase` for private fields.
Interfaces start with I; asynchronous operations end with Async and return Task/ValueTask rather than async void.
Use descriptive names tied to the feature; avoid unnecessary abbreviations and generic manager/helper classes.

- `Components/HitMenus/HitMenu{Feature}.razor`: built-in entry only.
- `Components/{Feature}/`: supporting views, forms and detail components.
- `Services/{Feature}/`: integration, domain state, snapshots and commands.
- `wwwroot/css/menus/`: public built-in feature CSS.
- `wwwroot/js/`: DOM-specific behavior with explicit connect/disconnect ownership.
- `Sdk/HIT.Sdk`: public contracts without a reference back to the executable.
- `Examples`, `Tests`, `Tools`, `Resources`, `Docs`: keep their distinct responsibilities.

Match namespaces to ownership. Do not rename a persisted built-in type ID casually; provide a migration when necessary.
Use explicit, stable package/menu IDs and storage keys. Titles and translations are never identities.

## State and component design

Keep Razor focused on presentation and coordinating commands. Prefer immutable records/DTOs for service snapshots
and validated update functions for preferences. Use an enum for mutually exclusive main/detail/settings views.
Derived display state should not be redundantly persisted. Separate temporary previews from committed values.

Do not write preferences, run network lookups or start capture from markup/property getters.
Do not start a new background task on every render. Guard one-time imports and share pending asynchronous imports.
Call StateHasChanged only when visible state actually changed; avoid rebuilds of large static lists/lyrics.
Use stable @key values for identity, not a position that changes after reorder.

## Async, cancellation and threading

Pass CancellationToken through I/O and command boundaries; set appropriate timeouts.
Cancel a superseded search, hidden-view capture or abandoned preview. Check generation/key before accepting an asynchronous result.
Use InvokeAsync before changing renderer-owned state from events or background callbacks.
Await task completion during disposal where the contract requires it; observe errors from intentionally detached work.

Serialize access to shared native resources. Use short locks for synchronous state, SemaphoreSlim for asynchronous resource ownership.
Never await while holding a C# lock. Avoid synchronous waits on UI work; startup composition is separate from renderer execution.
Use Task.Run for CPU/blocking integrations when appropriate, not as a wrapper around ordinary asynchronous I/O.

During view disposal: cancel, stop/await owned work, unsubscribe events, disconnect observers/listeners,
dispose JS references, release native resources and finally dispose token sources/scopes.
Catch JSDisconnectedException during shutdown. Do not let one failed package cleanup prevent disposal of other modules.

## Services and dependency injection

Built-in services are composed in Program.cs. Extensions register their own services through IHitModule.ConfigureServices
and resolve them from Api.Services; they do not receive the host's root provider.
Use singleton only for truly shared state with synchronized access. Create and dispose scopes for scoped services.
Do not hold UI instances, callbacks or ElementReference in a long-lived service without a clear detachable ownership contract.

Prefer interfaces at integration/public boundaries, not for every trivial class.
Snapshots describe state and supported capabilities. Commands validate input and stale generations before changing external state.
Make unsupported commands explicit; do not simulate media control with global key presses.

## Persistence and errors

Use the user's LocalApplicationData directories or Api.Storage. Do not persist preferences in bin, wwwroot,
the repository or an extension's versioned executable directory. Write via temporary files and atomic replacement.
Normalize bounds and validate persisted input; corrupt state should produce a useful fallback/error rather than block startup.
User-data migration is versioned and separate from code-package rollback.

Report actionable errors at the operation/view boundary. Preserve the last valid theme on invalid CSS edits.
Keep local extension failure separate from the entire host. Broad catches are appropriate only at a deliberate
plugin/installation/cleanup boundary; service code should generally catch expected exception categories.
Do not expose secrets, full provider payloads or unlimited exception streams in logs.

## Public UI and styling

Follow [Menus](MENUS.md) and [Themes](THEMES.md). Prefer the existing disclosure, local scroll, button, focus and dialog patterns.
Use semantic labels, button types, accessible names, keyboard focus and destructive confirmations.
No repeated common title/pin/close controls inside a view. Keep controls usable without hover tooltips.

CSS is public and scoped. Use variables/fallbacks, preserve artwork/palette choices and reduced motion,
and avoid `.razor.css`, inline branding rules, viewport-driven sizing loops or generic selectors affecting another menu.
JavaScript handles DOM mechanics, not persistence/domain state. Own and remove every listener/observer it adds.

## Memory and performance

Bound caches by entries/estimated bytes and expiry. Avoid retaining raw audio or decoded images outside their consumer lifetime.
Reuse hot-loop buffers and send only changed data over JS interop. Lazily build large selectors.
Pause work using visibility, except documented service-owned background functions.
Do not add forced GC, artificial working-set trimming or WebView process termination as a RAM policy.
Measure the same workload and distinguish allocation, retained managed heap and total process RAM.

## Review and documentation

Use behavior-focused tests for consequential changes. Do not add tests that only reproduce implementation details.
Build/publish relevant projects and check the distributed assets/docs. Run the documented regression matrix for affected features.
Keep code comments about intent/constraints, not a narration of obvious statements.
Document changed API signatures, capability names, bounds, data paths and selectors before release.
Commit source and supporting resources, not generated bin/obj/artifacts, profiles, logs or local preferences.
