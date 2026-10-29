# Chess extension

Chess is an optional, independent SDK 1 extension, built from `Modules/HIT.Chess`.
Its source and tests are versioned alongside HIT in this repository, but Chess is not a built-in HIT feature or part of the host application build/publish output.
Build and distribute its `.hitmenu` package separately, then install it through Extensions. HIT runs without Chess installed.
It does not modify the built-in menus or require a network connection.

## Build and install

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools/Pack-HitMenu.ps1 -Project Modules/HIT.Chess/HIT.Chess.csproj
```

Install `artifacts/packages/dev.hit.chess-1.1.0.hitmenu` through Settings -> Extensions -> Install .hitmenu,
or drop the package on HIT. A new installation loads immediately; updating a loaded Chess version restarts HIT automatically.
The stable menu ID is `dev.hit.chess:chess`. At the user's request, the 1.0.1 settings redesign was reverted and its generated package removed. Version 1.0.0 retains the compact previous interface, native selectors and the existing saved-game format. The host header identifies Chess with a knight icon.
If 1.0.1 was already installed, import 1.0.0 and confirm the downgrade. Saved state is kept; replacing loaded code requires the normal graceful restart.
See [Extensions](EXTENSIONS.md) for package installation and lifecycle details.

## Playing

Click one of your pieces to lift it and mark its legal destinations. Click a marked square to move.
Click the selected piece again, another empty square, or press Escape to cancel selection.
Pawn promotion offers Queen, Rook, Bishop and Knight. The board turns to put your chosen side at the bottom.

The main view contains a horizontal move history above the board, your cumulative Win / Loss record, and captured pieces.
Moves use standard algebraic notation (SAN), including captures, castling, disambiguation, promotion, check and mate.
The latest move is bold; older moves are muted. The history scrolls horizontally, follows the latest move, and is rebuilt from saved moves after restart.
Captured pieces appear in a single strip below the board, with White and Black groups side by side regardless of board orientation.
Both teams stay visible on the same line. Each group scrolls horizontally when full, without visible scrollbars.
The main content stays 360 px wide, including when either side has captures.
The last move colors both source and destination squares. Check has a red inset; a checkmated king tilts beneath a red flag.
The game result appears below the board so the final position stays visible.
Draws do not change Win / Loss. Record and in-progress moves persist through the package-scoped storage API.

Right-click the shared title to open settings. The title also supports the standard keyboard settings gesture.
All controls are in English. Settings provide Easy, Medium or Hard, White or Black, time per side and Move sounds.
The previous compact settings screen keeps all game options together, including Move sounds and Reset score. It does not opt into the new shared workspace.
The side select binds explicit string values to avoid boolean option-formatting mismatches.
Reset score is the first settings action. Its second confirmation resets persisted Win/Loss only;
Cancel/Escape leave the record intact. The current game, moves, clock and options are preserved.
There is no Board controls entry in settings; the board gestures documented below remain available.
Choose New game to apply the game options; replacing an active game requires a native modal confirmation with Cancel focused initially.
Confirmation uses the host's shared `.hit-confirm` design, matching theme/extension deletion dialogs in every theme.
Backdrops are transparent, without dimming or blur; Cancel receives initial focus.
Back, right-click return or Escape returns to the board. New game still applies draft options only after confirmation. Restarting a game preserves the record.
The New game action below an ended game restarts with the current preferences.
Result and confirmation buttons use the same 32 px height, 8 px corners, muted secondary treatment and accent primary treatment as Clock buttons.

There is no FF / Resign action. Starting another game does not change the record.

## Board annotations

The board's right mouse button is reserved for annotations and never opens settings.
Right-click a square to toggle a red mark. Hold the right button on one square, drag to another and release to toggle an arrow.
Knight-move arrows use an L shape. Several arrows and marked squares can coexist.
Repeat the same gesture to remove a mark; left-click, Escape, a played move or a new board clears all annotations.
Annotations do not change game state, have no network dependency and are not persisted.
The SVG overlay scales with the board and respects the current orientation.

## Compact board and themes

While pinned and visible, leaving HIT starts the shared countdown (default five seconds) before requesting the public Compact layout.
The pointer anywhere over HIT, including the header, prevents compact mode even without movement or window focus.
Re-entering cancels the pending countdown. The component rechecks hover before accepting the compact request.
The board shrinks from 360 px to 152 px; pieces remain visible, while history, scores, capture rows and shared chrome are hidden.
Click the miniature board to expand; hovering does not expand it. The miniature is also a native drag handle.
The full board has square corners; only the miniature rounds its corners. Right-click still annotates the board.
Unpinning or leaving the menu restores the regular layout on return.
The promotion chooser keeps the full board open until a choice or cancellation.
Settings -> Compact after configures the shared delay from immediately after leaving to ten seconds. There is no compact header button.

Board squares and sculpted Staunton-style SVG pieces use live HIT theme variables. Pieces have individual gradients, engraved details and contrasting side colors.
All three bundled themes are supported. SVG gradient IDs are unique per piece instance.
Public CSS lives in `Layout/css/chess.css`; custom themes can override the `--chess-*` properties or piece classes.
Variables with `white` / `black` describe chess sides rather than the current UI theme.
The stylesheet uses longhand color/font/border declarations to preserve variable substitution through HIT's current CSS compiler.
Motion preferences remain authoritative.

## Motion, sound and scrolling

Moves animate over 250 ms with smooth acceleration/deceleration, including both king and rook when castling.
The moving piece continues from its lifted selection position and uses compositor transforms to avoid a jump at the start.
Pending destination pieces are hidden in the Razor render itself, before the JavaScript animation starts.
JavaScript keeps them hidden across intervening renders until landing, preventing a flash on the destination square.
The algorithm waits for the preceding animation before searching/replying, and board moves remain blocked until the current move lands.
The forced presentation interval is not charged to the next side's clock. Restoring a saved position does not replay its animations or sounds.
HIT's Remove animations and the system's reduced-motion preference skip animations and cancel any in progress.
Annotations remain available during the visual transition.

Move sounds are enabled by default. Quiet move and capture sounds are synthesized locally through Web Audio; no downloads or audio files are needed.
Sounds play when the piece lands. Normal moves use a single wooden tap; captures use a lower contact followed by a distinct brighter clack 75 ms later.
The Move sounds switch mutes both immediately and saves the preference independently of the next-game options.
The browser may require an initial click or keypress before it can play audio. Audio sources/context and animation resources are released when the menu is disposed.

Scroll areas keep mouse-wheel, touchpad and keyboard scrolling, with no visible scrollbar.
A short background-to-transparent gradient covers the clipped edge only when more content exists in that direction.
History and capture rows show left/right hints. Edge state updates after scrolling, content changes and resizing.
The hints do not intercept input and disappear when the corresponding end is reached.

## Algorithm and rules

This is a hand-written, local chess algorithm: iterative deepening, negamax, alpha-beta pruning,
capture ordering, bounded quiescence search, material and positional evaluation.
It has no AI model, learned weights, network API or external chess engine dependency.

| Level | Main search depth | Search budget |
| --- | --- | --- |
| Easy | 1 ply, randomized among near-best candidates | 180 ms |
| Medium | Up to 3 plies | 750 ms |
| Hard | Up to 5 plies | 2,200 ms |

Budgets are checked during search; hardware changes the completed depth. Difficulty is relative, without an Elo claim.
Search runs off the renderer thread and is cancelled when hidden, while settings are open, on a new game or on disposal.
An unfinished computer turn resumes when the board becomes visible again.

Legal-move filtering protects the king and covers castling, en passant and all four promotions.
Checkmate, stalemate and common insufficient-material positions end the game.
The 50-move rule and threefold repetition are automatically adjudicated as draws, without a separate claim button.
The algorithm does not track repetition inside its speculative search tree; the actual game does.

Time is unlimited by default. Optional controls provide 1, 3, 5, 10, 15 or 30 minutes **per side**, without increment.
The clock starts with the new game and keeps running through settings, hidden views and compact mode while HIT is running.
Closing HIT pauses the saved clock; it resumes from the most recently saved remaining time.
Timed games checkpoint every ten seconds as well as on moves, results and visibility changes.
Timeout is a draw when the opponent has only a king; otherwise it awards the opponent a win.
The clock does not attempt exhaustive FIDE dead-position adjudication for unusual constructed positions.

## Validation

```powershell
dotnet run --project Modules/HIT.Chess.Tests/HIT.Chess.Tests.csproj -c Release
node Tests/browser/chess.mjs
```

The native tests cover standard perft positions, pins, special moves, search/cancellation,
rendered selection, SAN notation/history, save/reload, captures, last-move squares, mate king/flag, results, Black orientation, clocks, compact requests and real package import.
They also load the actual installed assembly through DynamicSphere and HitMenuHost, check the shared `IJSRuntime` identity,
and repeat open/close, settings and compact transitions. Actual White/Black select events verify both choices survive repeated selection; sound switch events verify persistent mute.
Pacing checks ensure an easy opponent cannot reply during the player's animation, input resumes after the animation and forced delays do not consume clock time.
They also check that extension UI is not mounted before its stylesheet finishes applying.
This exercises extension assembly resolution rather than only direct component rendering.
They also simulate JavaScript startup failures and queued settings callbacks after disposal, checking that failed menus release their settings references.
They generate board/settings markup for the browser test.
The headless Chrome test resolves the exact JavaScript import URL captured from the installed .NET component and checks all three bundled themes through HIT's actual extension CSS compiler,
vector piece colors, selection lift, last-move colors, history scrolling, same-line capture groups/overflow,
real mouse-driven square/arrow toggles, knight arrows, suppression of board settings, the shared five-second inactivity signal,
stationary hover over the board and header, square/full and rounded/miniature corners, mate appearance,
English settings without FF or Board controls, invisible scrollbars/directional edge gradients, move/castling animations,
250 ms duration and lift continuity, a delayed-JavaScript destination-flash regression, a separated two-part capture sound at landing, mute and reduced motion,
Clock-style result buttons, transparent confirmation backdrops, safe modal focus and resource cleanup.
Opening checks use the actual animated host, verifying stylesheet/readiness gating, a stable reserved native canvas,
a fixed corner anchor, content without blur/slide, canvas release on closing and immediate sizing with reduced motion.
Screenshots are written to `obj/chess-tests/browser`.
Native desktop focus, physical dragging and installation/restart still require a running HIT session for manual verification.

Version 1.0.2 keeps the restored compact UI and adds the optional SDK 1.2.0 page-history contract. It requires a host shipping SDK 1.2.0; existing 1.0.0 packages remain supported by the host. The board/game rules and saved-game identity are unchanged.

## Chess 1.1.0

The new optional package adopts the host settings workspace with General, Sound and Score topics, shared wheel selects, descriptions and host Back navigation. The board, saved-game ID, score and rules stay compatible. Existing installed extensions are not silently replaced; import the new package to use its layout. The host also normalizes the legacy New game action so the existing form remains usable without upgrading.

New game and Reset score confirmations use equal-width actions in a single flex row, with single-line labels. The host preserves this geometry for older installed packages as well. Browser checks verify full-row sizing, label fit and shared confirmation styling in all three bundled themes.

Settings labels share one left edge and retain the flexible portion of each row. Difficulty, side and time selectors occupy a fixed 132-pixel right column, keeping their labels and controls on the same line.

A small offline repertoire indexes exact legal board states and varies initial moves and replies across nine opening lines. Each candidate is checked against legal moves; leaving the repertoire resumes the existing bounded iterative search. Cancellation is honored before opening selection. No engine/network/model service was added.
