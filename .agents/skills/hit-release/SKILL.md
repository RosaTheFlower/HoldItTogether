---
name: hit-release
description: "Build, validate, package or prepare a HIT release, SDK NuGet, .hitmenu module, developer kit or Windows installer. Use for release tooling and selecting verification across projects; ordinary fixes need only relevant checks, not a complete installer build."
---

# HIT verification and distribution workflow

## Choose the requested deliverable

Read [Testing and release](../../../Docs/TESTING-RELEASE.md). For a Windows installer, also
read [Installer](../../../Docs/INSTALLER.md). For modules, read
[Extensions](../../../Docs/EXTENSIONS.md) or [Chess](../../../Docs/CHESS.md).
Use [Release scope](../../../Docs/VERSION-1.0.md) and [Docs/README](../../../Docs/README.md)
to distinguish implemented features and independent version contracts.

## Select and run the appropriate path

1. Identify whether the task needs a build, focused regression, SDK pack, module package,
   portable publication, full release or installation wizard. Use documented scripts and their
   actual parameters; inspect them before invoking a costly or distributing operation.
2. Build the affected `.csproj` on Windows with a compatible .NET SDK. Use individual projects
   if the installed tooling cannot load `.slnx`. Preserve root source/content exclusions so module,
   test and artifact trees are not compiled/copied into the application accidentally.
3. Prepare prerequisites in dependency order. The full example/host harness needs the packaged
   example fixture. Browser suites may consume snapshots emitted by that harness; Chrome is
   needed for those suites, not for running HIT. Focused `--window-checks` does not need the
   sample package. Record unavailable real-device checks as SKIP, not PASS.
4. If output is locked, choose an isolated task-specific `OutputPath` and `CopyRetryCount=0`.
   Preserve the user's running instance. Keep disposable test profiles separate from real data.
5. For changed SDK bits, deliberately version the NuGet package and compile an independent
   example. Do not reuse cached package contents under an unchanged published version or confuse
   NuGet version with assembly identity or the `.hitmenu` contract.
6. For full release/installer requests, use `Tools/Build-Release.ps1 -RunTests` and the documented
   `Tools/Build-Installer.ps1` path as appropriate. Preserve the Release/Dev distinction: Dev is a
   developer kit of the same application build. Keep optional Chess and Notes distribution rules,
   SDK/runtime assets and developer-only source/package placement aligned with the guide.
7. For installer tooling/runtime bootstrapper changes, preserve signature verification, per-user
   defaults, runtime detection and graceful in-use handling. Verify Release/Dev with/without
   optional Chess. Do not force-kill HIT or remove retained AppData during routine upgrade/uninstall.

## Check the distributed result

Run `node Tools/Check-Documentation.mjs` for documentation/instruction changes and its documented
`--published` mode against a publication when one was produced. Check required CSS/JS/icons,
SDK, Docs and module assets in the actual output, not merely successful compilation.

Use the affected acceptance matrix from Testing and release. Distinguish compilation, synthetic
browser checks, native checks and interactive Windows acceptance in the delivery report. Do not
claim an installer was tested when only the host was built.

Keep generated packages, outputs, logs and profiles under ignored locations. Update release,
installer or API documentation according to the actual deliverable; prepare a focused commit
description only when requested, without staging unrelated/local planning material.

## Current contract checkpoints

Build labels from HitReleaseGeneration and HitReleaseDate (current application 2v2910), keeping numeric Windows versions and SDK/package identities separate. Update ordering must compare generation before date and accept old vDDMM tags as generation one. Review tracked and untracked source plus git diff --check; clean only resolved generated paths, preserve running outputs and IDE/user profiles, and keep the local roadmap ignored. Never imply that a clean diff means installers/live native scenarios were validated.
