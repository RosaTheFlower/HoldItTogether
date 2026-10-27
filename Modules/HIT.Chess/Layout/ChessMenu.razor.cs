using System.Text.Json;
using HIT.Chess.Services;
using HIT.Sdk;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

namespace HIT.Chess.Layout;

public partial class ChessMenu
{
    [Inject] public IJSRuntime JS { get; set; } = default!;
    private ElementReference _root;
    private IJSObjectReference? _js, _connection;
    private Task? _jsInitialization;
    private CancellationToken _viewLifetime;
    private DotNetObjectReference<ChessMenu>? _reference;
    private ChessState _state = new();
    private Position _position = Position.Initial();
    private readonly List<int> _captured = [];
    private readonly List<string> _notation = [];
    private readonly Dictionary<string, int> _repetitions = [];
    private List<Move> _legal = [], _targets = [], _promotion = [];
    private int _selected = -1, _generation, _lastCaptured;
    private Move? _lastMove;
    private bool _loaded, _compact, _thinking, _confirmNew, _confirmScore, _restoreLayout, _disposed;
    private string? _scoreMessage;
    private Difficulty _draftDifficulty;
    private bool _draftWhite;
    // String values avoid bool formatting (True/False) mismatching native option values.
    private string DraftSide { get => _draftWhite ? "white" : "black"; set => _draftWhite = value == "white"; }
    private int _draftMinutes;
    private string _result = "", _storageMessage = "";
    private double _whiteSeconds, _blackSeconds;
    private DateTimeOffset _lastTick = DateTimeOffset.UtcNow;
    private CancellationTokenSource? _searchCancellation;
    private Task? _searchTask, _timerTask, _presentationTask;
    private DateTimeOffset _moveReadyAt = DateTimeOffset.MinValue;
    private int _presentedPly;
    private bool MoveInFlight => DateTimeOffset.UtcNow < _moveReadyAt;
    private readonly SemaphoreSlim _saveGate = new(1, 1);
    public bool IsSettingsOpen { get; private set; }
    private string BoardLabel => !string.IsNullOrEmpty(_result) ? _result :
        $"{(_position.White ? "White" : "Black")} to move{(_position.InCheck(_position.White) ? ", check" : "")}{(_thinking ? ". Algorithm thinking" : "")}.";

    protected override async Task OnInitializedAsync()
    {
        // Keep the token value valid for continuations after its source is disposed.
        _viewLifetime = Lifetime;
        try
        {
            var saved = await Api.Storage.ReadAsync<ChessState>("chess-v1", _viewLifetime);
            if (_disposed || _viewLifetime.IsCancellationRequested) return;
            if (saved is not null)
            {
                if (saved.Schema != 1 || !Enum.IsDefined(saved.Difficulty) || !new[] { 0, 1, 3, 5, 10, 15, 30 }.Contains(saved.Minutes) ||
                    saved.Moves is null || saved.Moves.Count > 3000 || saved.Wins < 0 || saved.Losses < 0 ||
                    !double.IsFinite(saved.WhiteSeconds) || !double.IsFinite(saved.BlackSeconds) || saved.WhiteSeconds < 0 || saved.BlackSeconds < 0)
                    throw new InvalidDataException("Invalid chess save.");
                // Replay legal moves rather than trusting a serialized board or castling flags.
                var test = Position.Initial();
                foreach (var move in saved.Moves)
                {
                    if (!test.LegalMoves().Contains(move)) throw new InvalidDataException("Invalid saved move.");
                    test = test.Play(move);
                }
                _state = saved;
            }
        }
        catch (OperationCanceledException) when (_viewLifetime.IsCancellationRequested) { return; }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            _storageMessage = "Saved game could not be loaded. A new board is ready.";
            Api.Log($"Chess load: {ex.Message}");
        }
        RestoreBoard();
        _whiteSeconds = _state.WhiteSeconds; _blackSeconds = _state.BlackSeconds;
        _result = _state.Result ?? "";
        _lastTick = DateTimeOffset.UtcNow;
        _loaded = true;
        StartClock();
    }

    private void RestoreBoard()
    {
        _position = Position.Initial(); _captured.Clear(); _notation.Clear(); _repetitions.Clear(); _lastMove = null; _lastCaptured = 0;
        RecordPosition();
        foreach (var move in _state.Moves)
        {
            _notation.Add(ChessNotation.Format(_position, move));
            int captured = _position.Captured(move);
            _lastCaptured = captured;
            if (captured != 0) _captured.Add(captured);
            _position = _position.Play(move); _lastMove = move; RecordPosition();
        }
        _legal = _position.LegalMoves();
        _presentedPly = _state.Moves.Count;
        _moveReadyAt = DateTimeOffset.MinValue;
    }

    protected override void OnContextChanged()
    {
        if (_disposed || !_loaded) return;
        if (!Context.IsVisible)
        {
            _searchCancellation?.Cancel();
            _ = SaveAsync();
        }
        else EnsureComputer();
        if (_compact && (!Context.IsPinned || !Context.IsVisible))
        {
            _compact = false;
            _restoreLayout = true;
        }
        // Inactive menus cannot change host layout; defer until selected again.
        if (_restoreLayout && Context.IsActive)
        {
            _restoreLayout = false;
            _ = Context.SetLayoutAsync(HitLayout.Default);
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_disposed) return;
        if (firstRender) await (_jsInitialization = InitializeJsAsync());
        if (_disposed) return;
        if (_connection is not null)
        {
            var generation = _generation;
            var ply = _state.Moves.Count;
            int duration = await _connection.InvokeAsync<int>("sync");
            if (!_disposed && generation == _generation && ply == _state.Moves.Count && _presentedPly != ply)
            {
                _presentedPly = ply;
                SetPresentationDuration(Math.Clamp(duration, 0, 250));
                if (duration == 0) await InvokeAsync(StateHasChanged);
            }
            if (IsSettingsOpen && (_confirmNew || _confirmScore)) await _connection.InvokeVoidAsync("confirm");
        }
    }

    private async Task InitializeJsAsync()
    {
        // AssetUrl is document-relative; import() requires an explicit relative specifier.
        _js = await JS.InvokeAsync<IJSObjectReference>("import", "./" + Api.AssetUrl("Assets/chess.js"));
        if (_disposed) return;
        _reference = DotNetObjectReference.Create(this);
        _connection = await _js.InvokeAsync<IJSObjectReference>("connect", _root, _reference);
    }

    [JSInvokable]
    public Task CompactAsync() => CompactCoreAsync(false);
    public Task CompactNowAsync() => CompactCoreAsync(true);
    private async Task CompactCoreAsync(bool immediate)
    {
        if (_disposed || !Context.IsVisible || (!Context.IsPinned && !immediate) || IsSettingsOpen || _compact || _promotion.Count > 0) return;
        if (!immediate && _connection is not null && await _connection.InvokeAsync<bool>("isPointerOver")) return;
        if (_disposed || !Context.IsVisible || (!Context.IsPinned && !immediate) || IsSettingsOpen || _compact || _promotion.Count > 0) return;
        _compact = true;
        ClearSelection();
        await Context.SetLayoutAsync(HitLayout.Compact);
        if (_disposed) return;
        await InvokeAsync(StateHasChanged);
    }

    private async Task ExpandAsync()
    {
        if (_disposed) return;
        _compact = false;
        await Context.SetLayoutAsync(HitLayout.Default);
    }

    public async Task OpenSettingsAsync()
    {
        if (_disposed) return;
        _compact = false; IsSettingsOpen = true; ClearSelection(); CancelConfirm();
        _draftDifficulty = _state.Difficulty; _draftWhite = _state.PlayerWhite; _draftMinutes = _state.Minutes;
        _searchCancellation?.Cancel();
        await Context.SetLayoutAsync(HitLayout.Default);
        if (_disposed) return;
        await Context.SetTitleAsync("Chess settings");
        if (_disposed) return;
        await InvokeAsync(StateHasChanged);
    }

    public async Task CloseSettingsAsync()
    {
        if (_disposed) return;
        IsSettingsOpen = false; CancelConfirm();
        await Context.SetTitleAsync("Chess");
        if (_disposed) return;
        EnsureComputer();
        await InvokeAsync(StateHasChanged);
    }

    private async Task NewGameAsync()
    {
        if (_disposed) return;
        _generation++; _searchCancellation?.Cancel();
        _state = new ChessState { Difficulty = _draftDifficulty, PlayerWhite = _draftWhite, Minutes = _draftMinutes,
            Wins = _state.Wins, Losses = _state.Losses, WhiteSeconds = _draftMinutes * 60, BlackSeconds = _draftMinutes * 60,
            SoundEnabled = _state.SoundEnabled };
        _result = ""; _whiteSeconds = _blackSeconds = _draftMinutes * 60; _lastTick = DateTimeOffset.UtcNow;
        RestoreBoard(); ClearSelection();
        StartClock();
        await SaveAsync();
        await CloseSettingsAsync();
    }

    private Task RestartAsync()
    {
        if (_disposed || MoveInFlight) return Task.CompletedTask;
        _draftDifficulty = _state.Difficulty; _draftWhite = _state.PlayerWhite; _draftMinutes = _state.Minutes;
        return NewGameAsync();
    }

    private async Task SoundChangedAsync(ChangeEventArgs args)
    {
        if (_disposed) return;
        _state.SoundEnabled = args.Value is true;
        await SaveAsync();
    }

    private void AskNewGame() { CancelConfirm(); _confirmNew = true; }
    private void AskClearScore() { if (_disposed) return; CancelConfirm(); _scoreMessage = null; _confirmScore = true; }
    private void CancelConfirm() { _confirmNew = _confirmScore = false; }
    private async Task ClearScoreAsync()
    {
        if (_disposed || !_confirmScore) return;
        CancelConfirm();
        try
        {
            await _saveGate.WaitAsync(_viewLifetime);
            try
            {
                var snapshot = SnapshotState(); snapshot.Wins = snapshot.Losses = 0;
                await Api.Storage.WriteAsync("chess-v1", snapshot, _viewLifetime);
                _state.Wins = _state.Losses = 0; _scoreMessage = "Score cleared.";
            }
            finally { _saveGate.Release(); }
        }
        catch (OperationCanceledException) when (_viewLifetime.IsCancellationRequested) { }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        { _scoreMessage = "Could not clear the score. Please try again."; Api.Log("Chess score reset: " + error.Message); }
    }

    private async Task ClickSquareAsync(int square)
    {
        if (_disposed) return;
        if (_compact) { await ExpandAsync(); return; }
        UpdateClock();
        if (_thinking || MoveInFlight || !string.IsNullOrEmpty(_result) || _position.White != _state.PlayerWhite || _promotion.Count > 0) return;
        var destinations = _targets.Where(m => m.To == square).ToList();
        if (destinations.Count > 1) { _promotion = destinations; return; }
        if (destinations.Count == 1) { await CommitAsync(destinations[0]); return; }
        if (_selected == square) { ClearSelection(); return; }
        int piece = _position.Board[square];
        if (piece != 0 && (piece > 0) == _state.PlayerWhite)
        {
            _selected = square; _targets = _legal.Where(m => m.From == square).ToList();
        }
        else ClearSelection();
    }

    private Task PromoteAsync(Move move) => CommitAsync(move);
    private void ClearSelection() { _selected = -1; _targets = []; _promotion = []; }
    private void OnKeyDown(KeyboardEventArgs e)
    {
        if (e.Key == "Escape")
        {
            if (_confirmNew || _confirmScore) CancelConfirm();
            else if (IsSettingsOpen) _ = CloseSettingsAsync();
            else ClearSelection();
        }
    }

    private async Task CommitAsync(Move move)
    {
        if (_disposed) return;
        UpdateClock();
        if (!string.IsNullOrEmpty(_result) || !_legal.Contains(move)) return;
        _notation.Add(ChessNotation.Format(_position, move));
        int captured = _position.Captured(move);
        _lastCaptured = captured;
        if (captured != 0) _captured.Add(captured);
        _position = _position.Play(move); _state.Moves.Add(move); _lastMove = move; ClearSelection();
        if (Context.IsVisible && _connection is not null) SetPresentationDuration(250);
        _lastTick = DateTimeOffset.UtcNow;
        RecordPosition(); _legal = _position.LegalMoves();
        if (_legal.Count == 0)
        {
            if (_position.InCheck(_position.White)) Finish(!_position.White, "checkmate");
            else Finish(null, "stalemate");
        }
        else if (_position.InsufficientMaterial()) Finish(null, "insufficient material");
        else if (_position.HalfMoves >= 100) Finish(null, "50-move rule");
        else if (_repetitions[_position.Key()] >= 3) Finish(null, "threefold repetition");
        await SaveAsync();
        EnsureComputer();
    }

    private void RecordPosition()
    {
        string key = _position.Key(); _repetitions[key] = _repetitions.GetValueOrDefault(key) + 1;
    }

    private void Finish(bool? winner, string reason)
    {
        if (!string.IsNullOrEmpty(_result)) return;
        _searchCancellation?.Cancel();
        if (winner is null) _result = $"Draw — {reason}";
        else if (winner == _state.PlayerWhite) { _state.Wins++; _result = $"Win — {reason}"; }
        else { _state.Losses++; _result = $"Loss — {reason}"; }
        _state.Result = _result; ClearSelection();
    }

    private void EnsureComputer()
    {
        if (_disposed || !_loaded || _thinking || _legal.Count == 0 || _viewLifetime.IsCancellationRequested || !Context.IsVisible || IsSettingsOpen ||
            !string.IsNullOrEmpty(_result) || _position.White == _state.PlayerWhite) return;
        _thinking = true;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_viewLifetime, VisibleLifetime);
        _searchCancellation = cancellation;
        _searchTask = RunComputerAsync(_position.Copy(), _generation, _state.Difficulty, cancellation);
    }

    private async Task RunComputerAsync(Position position, int generation, Difficulty difficulty, CancellationTokenSource cancellation)
    {
        try
        {
            // Keep search allocations off the previous move's animation window.
            await WaitForPresentationAsync(cancellation.Token);
            var move = await Task.Run(() => new ChessSearch().Choose(position, difficulty, cancellation.Token), cancellation.Token);
            await WaitForPresentationAsync(cancellation.Token);
            if (!_disposed && move is not null && generation == _generation && !cancellation.IsCancellationRequested)
                await InvokeAsync(() => CommitAsync(move.Value));
        }
        catch (OperationCanceledException) { }
        finally
        {
            if (!_disposed && !_viewLifetime.IsCancellationRequested)
                await InvokeAsync(() => { if (_disposed) return; _thinking = false; _searchCancellation = null; EnsureComputer(); StateHasChanged(); });
            cancellation.Dispose();
        }
    }

    private void SetPresentationDuration(int milliseconds)
    {
        _moveReadyAt = DateTimeOffset.UtcNow.AddMilliseconds(milliseconds);
        if (milliseconds > 0 && (_presentationTask is null || _presentationTask.IsCompleted)) _presentationTask = FinishPresentationAsync();
    }

    private async Task WaitForPresentationAsync(CancellationToken token)
    {
        // Rendering can extend the deadline to the actual start of the compositor animation.
        while (_moveReadyAt - DateTimeOffset.UtcNow is var remaining && remaining > TimeSpan.Zero)
            await Task.Delay(remaining, token);
    }

    private async Task FinishPresentationAsync()
    {
        try
        {
            await WaitForPresentationAsync(_viewLifetime);
            if (!_disposed && Context.IsVisible) await InvokeAsync(StateHasChanged);
        }
        catch (OperationCanceledException) when (_viewLifetime.IsCancellationRequested) { }
    }

    private bool UpdateClock()
    {
        var now = DateTimeOffset.UtcNow;
        var clockStart = _lastTick > _moveReadyAt ? _lastTick : _moveReadyAt;
        double elapsed = Math.Max(0, (now - clockStart).TotalSeconds); _lastTick = now;
        if (!_loaded || _state.Minutes == 0 || !string.IsNullOrEmpty(_result)) return false;
        if (_position.White) _whiteSeconds = Math.Max(0, _whiteSeconds - elapsed);
        else _blackSeconds = Math.Max(0, _blackSeconds - elapsed);
        if (_whiteSeconds <= 0 || _blackSeconds <= 0)
        {
            bool winner = _blackSeconds <= 0;
            // A bare king cannot win on time.
            bool bareKing = !_position.Board.Any(p => p != 0 && (p > 0) == winner && Math.Abs(p) != Position.King);
            Finish(bareKing ? null : winner, "time expired");
        }
        return true;
    }

    private void StartClock()
    {
        if (_state.Minutes > 0 && (_timerTask is null || _timerTask.IsCompleted)) _timerTask = TimerAsync();
    }

    private async Task TimerAsync()
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        int ticks = 0;
        try
        {
            while (_state.Minutes > 0 && await timer.WaitForNextTickAsync(_viewLifetime))
                await InvokeAsync(async () =>
                {
                    if (_disposed) return;
                    bool changed = UpdateClock();
                    if (changed && (ticks++ % 10 == 0 || !string.IsNullOrEmpty(_result))) await SaveAsync();
                    if (changed && Context.IsVisible && !IsSettingsOpen && !_compact) StateHasChanged();
                });
        }
        catch (OperationCanceledException) { }
    }

    private async Task SaveAsync()
    {
        if (_disposed) return;
        try
        {
            await _saveGate.WaitAsync(_viewLifetime);
            try
            {
                _state.WhiteSeconds = _whiteSeconds; _state.BlackSeconds = _blackSeconds;
                // Snapshot avoids a concurrent move mutating an in-flight JSON write.
                var snapshot = SnapshotState();
                await Api.Storage.WriteAsync("chess-v1", snapshot, _viewLifetime);
                _storageMessage = "";
            }
            finally { _saveGate.Release(); }
        }
        catch (OperationCanceledException) when (_viewLifetime.IsCancellationRequested) { }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            _storageMessage = "Could not save. Check storage permissions and free space."; Api.Log($"Chess save: {ex.Message}");
        }
    }
    private ChessState SnapshotState() => new() { Difficulty = _state.Difficulty, PlayerWhite = _state.PlayerWhite,
        Minutes = _state.Minutes, Wins = _state.Wins, Losses = _state.Losses, Moves = [.. _state.Moves],
        Result = _result, WhiteSeconds = _whiteSeconds, BlackSeconds = _blackSeconds, SoundEnabled = _state.SoundEnabled };

    private string SquareClass(int square, bool target) => "chess-square " + ((square % 8 + square / 8) % 2 == 0 ? "dark" : "light") +
        (square == _selected ? " selected" : "") + (target ? " legal" : "") +
        (_lastMove is Move move && (move.From == square || move.To == square) ? " last-move" : "") +
        (Math.Abs(_position.Board[square]) == Position.King && _position.InCheck(_position.Board[square] > 0) ? " in-check" : "");
    private string SquareLabel(int square, bool target) => Position.Square(square) + ": " + (_position.Board[square] == 0 ? "empty" :
        $"{(_position.Board[square] > 0 ? "White" : "Black")} {PieceName(_position.Board[square])}") + (target ? ", legal destination" : "");
    private static string PieceName(int piece) => Math.Abs(piece) switch { 1 => "pawn", 2 => "knight", 3 => "bishop", 4 => "rook", 5 => "queen", 6 => "king", _ => "empty" };
    private bool IsPendingMoveSquare(int square)
    {
        if (_connection is null || _presentedPly == _state.Moves.Count || _lastMove is not Move move) return false;
        if (square == move.To) return true;
        return Math.Abs(_position.Board[move.To]) == Position.King && Math.Abs(move.To - move.From) == 2 &&
            square == (move.To > move.From ? move.From + 1 : move.From - 1);
    }
    private bool IsMatedKing(int square) => _legal.Count == 0 && Math.Abs(_position.Board[square]) == Position.King &&
        (_position.Board[square] > 0) == _position.White && _position.InCheck(_position.White);
    private static string ClockText(double seconds) => $"{(int)Math.Ceiling(seconds) / 60}:{(int)Math.Ceiling(seconds) % 60:00}";

    protected override async ValueTask OnDisposeAsync()
    {
        _disposed = true;
        IsSettingsOpen = false;
        _searchCancellation?.Cancel();
        if (_searchTask is not null) await _searchTask;
        if (_timerTask is not null) await _timerTask;
        if (_presentationTask is not null) await _presentationTask;
        if (_jsInitialization is not null)
        {
            try { await _jsInitialization; }
            catch (JSException) { /* The error boundary already handles initialization failures. */ }
        }
        try
        {
            if (_connection is not null) { await _connection.InvokeVoidAsync("dispose"); await _connection.DisposeAsync(); }
            if (_js is not null) await _js.DisposeAsync();
        }
        catch (JSDisconnectedException) { }
        _reference?.Dispose();
    }
}
