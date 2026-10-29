---
name: hit-chess
description: "Change or diagnose the independent HIT Chess extension, including legal moves, game state, board interaction, animations, persistence, sounds, scores or its .hitmenu packaging. Use for Modules/HIT.Chess; use hit-performance for measured host/WebView memory investigations."
---

# HIT Chess workflow

## Read and preserve the module boundary

Read [Chess](../../../Docs/CHESS.md) and the relevant shared behavior in
[Menus](../../../Docs/MENUS.md). For package or SDK changes, read
[Extensions](../../../Docs/EXTENSIONS.md) and [API](../../../Docs/API.md).
Chess is an optional independent module, not a built-in host entry.

## Change the correct layer

1. Locate rules/game-state ownership in C#, presentation in the module's Razor/CSS and DOM,
   annotations, sounds and move effects in its JS. Keep dependencies within the public SDK;
   do not reference the application or copy Chess source into its executable.
2. For rules, preserve legal-move validation, check/mate/stalemate and the guide's supported
   special moves/draw rules. Separate game history, score and UI selection/annotation state.
   Update behavior checks for the changed rule rather than testing only a helper's output.
   Keep search off the renderer, bounded and cancellable when hidden, in settings, on a new
   game or on disposal. An unfinished computer turn resumes when the board is visible again.
3. Preserve durable state, normalized input and reset scope. Score reset must retain moves
   where documented; clearing a score and starting/resetting a game are distinct operations.
   Exercise failed/corrupt saves under a disposable package state directory.
4. For movement, preserve the destination-piece hiding and move-ghost lifetime, both castling
   pieces, sound ordering and interrupted/rapid-move handling described in the guide. Do not
   reveal a second piece or leave an invisible destination after cancellation.
5. Preserve the full square board and compact round presentation, shared host-requested compact
   behavior, M1 restore and M2 return. Keep board coordinates, annotations and pointer hit testing
   correct under orientation, scaling and compact/expand transitions.
6. Stop view-owned presentation work when hidden/disposed, while preserving game-clock rules:
   a timed game continues through settings, hidden views and compact mode while HIT runs;
   closing HIT pauses its saved remaining time. Do not charge forced move-animation time to
   the next side or replay animations/sounds on restore. Release module-owned resources through
   their actual lifetime rather than forcing host/WebView cleanup.

## Verify and package only as required

Run the relevant module checks:

```powershell
dotnet run --project Modules/HIT.Chess.Tests/HIT.Chess.Tests.csproj
```

Follow [Testing and release](../../../Docs/TESTING-RELEASE.md) and the Chess guide for
`Tests/browser/chess.mjs` and any generated fixtures. Use `placement.mjs` for shared geometry
changes. For distribution, use the documented packer and validate installation as an independent
`.hitmenu`; a host build alone does not ship updated Chess code.

Update Chess and any affected public contracts. For CPU/RAM claims, use `hit-performance` and
measured scenarios; observing Chess in use does not establish which process/subsystem retains memory.

## Current contract checkpoints

Chess 1.1.0 explicitly adopts the shared General/Sound/Score settings workspace at the user's request, preserving the board, knight identity and saved-game format. Existing installed packages remain unchanged until imported. Validate repertoire moves and off-book search rather than adding uncontrolled random moves. Its optional SDK page contract participates in host history. Shared flat-control styling must preserve board squares, piece contrast, annotations and game-state behavior.
