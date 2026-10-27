# Windows installer

The installer is a single Windows x64 setup executable with the .NET runtime included.
It uses a standard English wizard: Welcome, installation folder, Release/Dev profile, optional Chess,
shortcut options, confirmation, progress and Finish with an optional launch action.
The default is a per-user installation under `%LOCALAPPDATA%\Programs\HoldItTogether`, without administrator privileges.

## Profiles and optional modules

| Selection | Installed files |
| --- | --- |
| Release | HIT, .NET runtime, native dependencies, themes, artwork and icon assets |
| Dev | Everything in Release plus `Developer/Source`, SDK NuGet package, Notes `.hitmenu`, docs and build tools |
| Install Chess | The separate Chess `.hitmenu`, automatically imported before HIT opens; available with either profile |

Dev is a developer kit, not an unstable application channel. Both profiles install the same HIT application build.
The SDK contract DLL is a required runtime dependency in both profiles; only Dev includes the SDK NuGet authoring package and source projects.
Notes is supplied for development and is not automatically activated. Chess remains a separate, optional extension.
The Dev source copy includes independent module source/tests; selecting Chess controls installation of its playable module.
Developer tools such as a .NET SDK, Node, Chrome and Inno Setup are not installed automatically.

The setup contains both profiles' payloads. Choosing Release writes only its selected files; it does not install the developer kit.
This avoids dependency on hosted archives or additional downloads. WebView2 is the exception: when absent,
Setup runs the included Microsoft-signed Evergreen bootstrapper, which requires Internet access.
If WebView2 cannot be installed, Setup reports the prerequisite problem before copying HIT.
See Microsoft's [WebView2 distribution guide](https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution).

## Build

Install [Inno Setup 6.7 or later](https://jrsoftware.org/isdl.php), then run:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Build-Installer.ps1
```

This first runs `Build-Release.ps1 -RunTests`, then stages the self-contained application, separate modules and developer kit.
The build downloads the Microsoft WebView2 bootstrapper if needed and verifies its Authenticode signature.
The compiler can be supplied explicitly using `-Compiler C:\Tools\InnoSetup\ISCC.exe`.
The script also recognizes `artifacts/tools/innosetup/ISCC.exe` and standard Inno Setup installation locations.

Reuse a release that contains the current host's installer import command:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Build-Installer.ps1 -ReleaseDirectory artifacts/releases/v2710
```

The output is `installer/HIT-vDDMM-win-x64-Setup.exe` within that release directory.
The build stages source from an explicit directory list, excluding bin/obj/artifacts, Git/IDE state, links and user settings.
All generated payloads, compiler downloads and setup executables remain ignored under `artifacts`.
`Installer/HoldItTogether.iss` is the maintained installer definition; follow the official [Inno Setup documentation](https://jrsoftware.org/ishelp/).

## Installation, upgrades and uninstall

The wizard defaults to Release with Chess unchecked. Automation can use `/PROFILE=release` or `/PROFILE=dev`,
`/CHESS=0` or `/CHESS=1`, and the standard `/DIR`, `/VERYSILENT` and `/SUPPRESSMSGBOXES` switches.
The launch action is skipped for silent installs.
Windows' Restart Manager asks to close applications holding installed files; Setup does not force-kill HIT.
Optional Chess imports use the normal package validator, storage boundaries and version rules, before loading module code.
Reinstalling an equal or older optional package keeps the existing selected module and its data.

Changing to Release does not erase an existing developer work directory. Keep personal projects outside the installer-managed kit:
upgrading Dev can replace its supplied source files. The source repository is never modified by an installed copy.

The Start menu shortcut and optional desktop shortcut use the application's embedded `hit.ico`.
The desktop option is unchecked by default; selecting it creates a shortcut named `HIT`.
Windows Installed Apps includes the uninstaller. Uninstall removes installed files/shortcuts and removes the Windows startup entry
only if it points to that installation. It preserves personal settings, games, scores, themes and installed extension state in AppData.
Use Settings -> Info -> Delete my data for deliberate removal of personal data.
The generated installer is unsigned unless separately signed with a publisher's code-signing certificate before distribution.

## Validation

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Test-Installer.ps1 -ReleaseDirectory artifacts/releases/v2710
```

The integration helper compiles a test variant without shortcuts or uninstall registry registration, installs all four
Release/Dev and Chess on/off combinations into disposable `obj` directories, and verifies installed files and extension registration.
Chess test imports use isolated module roots, not the real user's profile. A preinstalled WebView2 runtime is required for these tests;
the test build never installs prerequisites. It also builds the installed Dev source solution, checks the optional `HIT` shortcut in an isolated test desktop directory, and checks silent uninstall and preserved extension state.
