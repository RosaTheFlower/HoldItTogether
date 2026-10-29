# Troubleshooting

Use the complete published folder and back up user data before intentionally resetting it.
User state paths are in [Getting started](GETTING-STARTED.md); build commands are in [Testing and release](TESTING-RELEASE.md).

| Symptom | What to check |
| --- | --- |
| App type missing in Program.cs | Razor SDK is active; App is under Components; Program imports `HoldItTogether.Components`; build the main project |
| DirectoryNotFoundException for wwwroot | Launch the complete build/publish output; wwwroot must accompany the executable |
| Window opens without content | WebView2/runtime prerequisites, required JS/CSS and full dependency output; retain the first useful startup error |
| Unexpected terminal window | Main OutputType must remain WinExe; CLI build tools can still use a terminal |
| Window cannot be clicked/appears click-through | Check native hit-test/content bounds and transparent-window behavior; a CSS theme should not override pointer input |
| Hover repeatedly opens/closes | Check resize-induced pointer transitions, natural dimensions and viewport feedback loops; do not remove shell geometry guards |
| Controls do not act | Confirm no hidden/inert overlay, supported media capability and current generation; CSS cannot implement Blazor actions |
| Page/window scroll appears | Use natural width, bounded local scroll and avoid 100vw/100vh; inspect oversized theme fonts/content |
| Wheel stops after selecting a theme | The select should release focus; settings views intentionally block menu navigation |
| No taskbar entry | Intended behavior; enable System Tray for Show/Hide/Exit |
| HIT falls behind ordinary windows | Enable Always on Top and test with FORCE off. Native events now repair a missing topmost flag without activating HIT. If it persists, record the build, transition, source app and monitor/DPI; the original intermittent trigger still needs live verification. Other topmost windows and exclusive fullscreen are separate cases. |
| Game minimizes or HIT disappears in fullscreen | Fullscreen detection now uses non-activating mouse policy and pauses repeated FORCE ordering. Exclusive D3D detection is best-effort and ordinary desktop windows cannot guarantee drawing over an exclusive swap chain. Use borderless for a composited overlay and report the exact presentation mode if disruption persists. |
| Compact Clock does not open on hover | Intended; use M1. Drag moves it; M2 opens settings |
| RAM differs from Task Manager | Info -> Performance separates HIT, descendant WebView2 and Total RAM. Check private resident versus working-set fallback and partial-sample labels; compare the same process group and workload. |

## Audio and lyrics

No media metadata usually means the app does not expose a compatible Windows media session.
Check the source app's controls/capabilities rather than assuming every app supports seek/shuffle/repeat.
Ctrl activation requests an existing window and can be refused by Windows; it does not launch the source app.

Lyrics can be unavailable due to missing real timestamps, network/provider failure, strict matching or the negative cache.
Do not synthesize timing or choose an unrelated version to make the button active. A failed lookup is temporarily cached.
Check actual title/artist/duration and the real result URL. Hidden/superseded lookups are intentionally cancelled.

The spectrum follows the default output mix. Check silence, exclusive/protected playback, another routed device
or an output change. Reopen the visualizer after a capture error. It does not record or use the microphone.

If cover and spectrum conflict, inspect their independent layers rather than disabling one feature.
Check Lyrics cover/global cover toggles separately. A custom theme should not hide layers or force fixed music colors.

## Themes

Edit the imported copy in the personal Themes directory, not the original file selected elsewhere.
Use the supported @layer sphere/menus structure, no CSS nesting/@import, and keep the file at most 1 MiB.
A rejected edit preserves the last valid style. Missing selected files fall back to Dark.
Unsupported relative personal-theme resources are not served; package-local asset URLs are a different mechanism.
Bundled themes are intentionally protected. Create/import a copy for a personal variant.

## Extensions

New packages activate immediately; replacing loaded code restarts HIT automatically. Check Settings -> Extensions for load errors, a disabled switch or pending deletion.
Enabling a package skipped at startup loads its code without a restart.
Removing one menu's package removes all menus declared by that package; saved State remains.
Loaded module services finish at exit even after a view is disabled.

Installation rejects wrong extensions, malformed ZIP/manifest, paths/links/collisions, limits, unknown capability names
and unsupported contract versions. Reimporting identical bytes is harmless; changed bytes require a newer version.
Do not zip an extra wrapper directory around manifest.json. Use forward-slash package paths.

Load failures: verify entry/component assembly and type names, public module constructor,
HitMenuComponent inheritance, private dependencies/.deps.json and one shared SDK/framework identity.
Do not package a different executable's internals or duplicate conflicting managed assembly identities.
Use Restore previous version for an incompatible update; HIT restarts automatically if a different version is loaded. Data migration/rollback compatibility remains the author's responsibility.

Api.Services resolves the private module container; injecting a private service with normal Blazor [Inject] is not equivalent.
Missing declared capabilities throw before host API commands. Supported media commands can still be rejected by the source app.
A menu-local render failure should leave built-in navigation available; check the corresponding UI/log rather than replacing host CSS.

If an extension using JavaScript interop is followed by a `RenderTreeDiffBuilder` NullReferenceException during pointer updates,
inspect the first exception. `Package dependency not found: Microsoft.JSInterop` means the host loader failed to share
the renderer's interop assembly. Rebuild/update HIT with the corrected loader and restart; reinstalling the same `.hitmenu`
does not repair host assembly resolution. The chess integration test covers this failure through the actual installed assembly.

If a menu reports a display error and a later settings gesture throws `ObjectDisposedException` from `HitMenuComponent.Lifetime`,
inspect `Modules/Logs/{packageId}.log` for the primary render error. `Failed to resolve module specifier` with a path starting
`extensions/` means the extension omitted the `./` prefix required by native JavaScript imports. The current Chess package uses the correct path
and ignores late callbacks; the host also clears settings references after an error boundary disposes a menu.

Inbox files need to finish copying and be released for exclusive read. Success goes to Processed; invalid packages to Rejected.
Package log files are under Modules/Logs if the extension calls Api.Log. Startup failures also appear in the manager.
The package loader is not a sandbox or hard RAM limiter; disable/restart a faulty trusted package and inspect its lifecycle code.

If navigation wraps unexpectedly, check General -> Infinite menu. Back/Forward page history and top-level menu cycling are separate policies. If a managed GIF disappears, check Images -> Allow GIF backgrounds; references are preserved. If a new release appears incorrectly ordered, confirm both HitReleaseGeneration and HitReleaseDate and the NvDDMM tag.
