using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using HIT.Chess.Layout;
using HIT.Chess.Services;
using HIT.Sdk;
using HoldItTogether.Services.Extensions;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
static long Perft(Position p, int depth) => depth == 0 ? 1 : p.LegalMoves().Sum(m => Perft(p.Play(m), depth - 1));
static Move Find(Position p, string from, string to, int promotion = 0) => p.LegalMoves().Single(m => Position.Square(m.From) == from && Position.Square(m.To) == to && m.Promotion == promotion);

var initial = Position.Initial();
var varied=Enumerable.Range(0,32).Select(seed=>new ChessSearch(new Random(seed)).Choose(Position.Initial(),Difficulty.Hard,CancellationToken.None)).Distinct().ToArray();
Check(varied.Length>=3&&varied.All(move=>move is not null&&initial.LegalMoves().Contains(move.Value)),"Opening repertoire varies among legal first moves.");
try{new ChessSearch().Choose(initial,Difficulty.Hard,new CancellationToken(true));throw new Exception("Canceled opening lookup returned a move.");}catch(OperationCanceledException){}

Check(Perft(initial, 1) == 20, "Initial perft 1");
Check(Perft(initial, 2) == 400, "Initial perft 2");
Check(Perft(initial, 3) == 8902, "Initial perft 3");
Check(Perft(initial, 4) == 197281, "Initial perft 4");
var kiwipete = Position.FromFen("r3k2r/p1ppqpb1/bn2pnp1/3PN3/1p2P3/2N2Q1p/PPPBBPPP/R3K2R w KQkq - 0 1");
Check(Perft(kiwipete, 1) == 48 && Perft(kiwipete, 2) == 2039 && Perft(kiwipete, 3) == 97862, "Castling / pins perft");
var endgame = Position.FromFen("8/2p5/3p4/KP5r/1R3p1k/8/4P1P1/8 w - - 0 1");
Check(Perft(endgame, 3) == 2812 && Perft(endgame, 4) == 43238, "En passant / check perft");
var promotion = Position.FromFen("r3k2r/Pppp1ppp/1b3nbN/nP6/BBP1P3/q4N2/Pp1P2PP/R2Q1RK1 w kq - 0 1");
Check(Perft(promotion, 3) == 9467, "Promotions / evasions perft");
Console.WriteLine("PASS: standard perft positions, 360,000+ legal-move leaves including castling, pins, en passant and promotions.");

var ep = Position.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
var epMove = Find(ep, "e5", "d6");
Check(ep.Captured(epMove) == -Position.Pawn && ep.Play(epMove).Board[35] == 0, "En passant removes captured pawn");
var pinnedEp = Position.FromFen("k3r3/8/8/3pP3/8/8/8/4K3 w - d6 0 1");
Check(!pinnedEp.LegalMoves().Any(m => Position.Square(m.To) == "d6"), "En passant must not expose king");
var castle = Position.FromFen("r3k2r/8/8/8/8/8/8/R3K2R w KQkq - 0 1");
var castled = castle.Play(Find(castle, "e1", "g1"));
Check(castled.Board[5] == Position.Rook && castled.Board[7] == 0 && (castled.Castling & 3) == 0, "Castling moves rook and clears rights");
var promote = Position.FromFen("4k3/P7/8/8/8/8/8/4K3 w - - 0 1");
Check(promote.LegalMoves().Count(m => m.From == 48) == 4, "All four promotion choices");
var mate = Position.FromFen("7k/6Q1/5K2/8/8/8/8/8 b - - 0 1");
Check(mate.LegalMoves().Count == 0 && mate.InCheck(false), "Checkmate");
var stale = Position.FromFen("7k/5K2/6Q1/8/8/8/8/8 b - - 0 1");
Check(stale.LegalMoves().Count == 0 && !stale.InCheck(false), "Stalemate");
Check(Position.FromFen("7k/8/8/8/8/8/8/K7 w - - 0 1").InsufficientMaterial(), "Bare kings draw");
Check(Position.FromFen("7k/8/8/8/8/8/8/KB6 w - - 0 1").InsufficientMaterial(), "King and bishop draw");
Check(ep.Key() != Position.FromFen("4k3/8/8/3pP3/8/8/8/4K3 w - - 0 1").Key(), "Capturable en passant distinguishes repetition");
Check(pinnedEp.Key() == Position.FromFen("k3r3/8/8/3pP3/8/8/8/4K3 w - - 0 1").Key(), "Illegal en passant does not distinguish repetition");
Check(ChessNotation.Format(initial, Find(initial, "e2", "e4")) == "e4", "Pawn notation");
Check(ChessNotation.Format(ep, epMove) == "exd6", "En passant notation");
Check(ChessNotation.Format(castle, Find(castle, "e1", "g1")) == "O-O", "Castling notation");
Check(ChessNotation.Format(promote, Find(promote, "a7", "a8", Position.Queen)) == "a8=Q+", "Promotion and check notation");
var ambiguous = Position.FromFen("4k3/8/8/8/8/8/8/1N2KN2 w - - 0 1");
Check(ChessNotation.Format(ambiguous, Find(ambiguous, "b1", "d2")) == "Nbd2", "Legal SAN disambiguation");

foreach (var difficulty in Enum.GetValues<Difficulty>())
{
    var watch = Stopwatch.StartNew();
    var move = new ChessSearch().Choose(initial, difficulty, CancellationToken.None);
    Check(move is not null && initial.LegalMoves().Contains(move.Value), "Search produces legal move: " + difficulty);
    Check(watch.ElapsedMilliseconds < 4000, "Search stays bounded: " + difficulty);
    Console.WriteLine($"PASS: {difficulty} search, {watch.ElapsedMilliseconds}ms.");
}
var mateInOne = Position.FromFen("7k/8/5KQ1/8/8/8/8/8 w - - 0 1");
var winning = new ChessSearch().Choose(mateInOne, Difficulty.Hard, CancellationToken.None)!.Value;
Check(mateInOne.Play(winning).InCheck(false) && mateInOne.Play(winning).LegalMoves().Count == 0, "Hard finds mate in one");
using (var canceled = new CancellationTokenSource())
{
    canceled.Cancel();
    try { new ChessSearch().Choose(initial, Difficulty.Hard, canceled.Token); throw new Exception("Search ignored cancellation"); }
    catch (OperationCanceledException) { }
}
Console.WriteLine("PASS: special rules, draws, mate search and cancellation.");

var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime, NoJs>().BuildServiceProvider();
await using (var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>()))
{
    await renderer.Dispatcher.InvokeAsync(async () =>
    {
        var api = new TestApi();
        var host = new CaptureHost();
        var output = await renderer.RenderComponentAsync<CaptureHost>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Api"] = api, ["Capture"] = (Action<ChessMenu>)(c => host.Menu = c) }));
        var menu = host.Menu!;
        string html = output.ToHtmlString();
        Check(html.Split("class=\"chess-square ").Length - 1 == 64 && html.Contains("Win") && html.Contains("Loss"), "Initial board rendering");
        Directory.CreateDirectory("obj/chess-tests");
        await File.WriteAllTextAsync("obj/chess-tests/board.html", html);
        await Call(menu, "ClickSquareAsync", 12); // e2
        Check(output.ToHtmlString().Contains("legal destination"), "Selection reveals legal moves");
        await Call(menu, "ClickSquareAsync", 28); // e4
        Check(api.Store.Value!.Moves.Count == 1, "Move persisted");
        await menu.OpenSettingsAsync();
        Check(menu.IsSettingsOpen && output.ToHtmlString().Contains("Play as") && !output.ToHtmlString().Contains("Resign"), "Settings and no FF action");
        await Call(menu, "set_DraftSide", "black");
        Check(!(bool)Get(menu, "_draftWhite")! && output.ToHtmlString().Contains("value=\"white\"") && output.ToHtmlString().Contains("value=\"black\""), "Selecting Black retains both options");
        await Call(menu, "set_DraftSide", "white");
        Check((bool)Get(menu, "_draftWhite")! && output.ToHtmlString().Contains("value=\"white\"") && output.ToHtmlString().Contains("value=\"black\""), "Selecting White retains both options");
        Check(!output.ToHtmlString().Contains("FACIL") && output.ToHtmlString().Contains(">Easy<") && output.ToHtmlString().Contains(">Medium<") && output.ToHtmlString().Contains(">Hard<"), "All difficulty labels are English");
        await File.WriteAllTextAsync("obj/chess-tests/settings.html", output.ToHtmlString());
        await ((IHitMenuPages)menu).NavigateToPageAsync("Settings/Sound");
        await File.WriteAllTextAsync("obj/chess-tests/sound-settings.html", output.ToHtmlString());
        await ((IHitMenuPages)menu).NavigateToPageAsync("Settings/General");
        await Call(menu, "AskNewGame");
        await File.WriteAllTextAsync("obj/chess-tests/confirmation.html", output.ToHtmlString());
        await Call(menu, "CancelConfirm");
        Check(!output.ToHtmlString().Contains("Board controls"), "Board controls option is removed");
        await Call(menu, "SoundChangedAsync", new ChangeEventArgs { Value = false });
        Check(!api.Store.Value!.SoundEnabled, "Mute is persisted immediately");
        await Call(menu, "NewGameAsync");
        Check(!api.Store.Value!.SoundEnabled, "New game retains mute preference");
        await Call(menu, "SoundChangedAsync", new ChangeEventArgs { Value = true });
        foreach (var move in new[] { new Move(13, 21), new Move(52, 36), new Move(14, 30), new Move(59, 31) }) await Call(menu, "CommitAsync", move);
        Check(api.Store.Value!.Losses == 1 && api.Store.Value.Result.Contains("checkmate"), "Checkmate records a loss");
        Check(output.ToHtmlString().Contains("checkmated") && output.ToHtmlString().Contains("chess-mate-flag") && output.ToHtmlString().Contains("Qh4#"), "Mate flag, tilted king and SAN mate notation");
        await File.WriteAllTextAsync("obj/chess-tests/mate.html", output.ToHtmlString());
        await Call(menu, "CommitAsync", new Move(59, 31));
        Check(api.Store.Value.Losses == 1, "Result cannot be counted twice");
        await menu.OpenSettingsAsync();
        await Call(menu, "NewGameAsync");
        Check(api.Store.Value.Moves.Count == 0 && api.Store.Value.Losses == 1 && api.Store.Value.Minutes == 0, "New game preserves record and unlimited default");

        foreach (var (from, to) in new[] { (6, 21), (62, 45), (21, 6), (45, 62), (6, 21), (62, 45), (21, 6), (45, 62) })
            await Call(menu, "CommitAsync", new Move(from, to));
        Check(api.Store.Value.Result.Contains("threefold") && api.Store.Value.Losses == 1 && api.Store.Value.Wins == 0, "Threefold draw preserves record");
        await menu.OpenSettingsAsync();
        Set(menu, "_draftWhite", false);
        Set(menu, "_draftMinutes", 3);
        await Call(menu, "NewGameAsync");
        Check(api.Store.Value.PlayerWhite == false && api.Store.Value.WhiteSeconds == 180, "Black side and optional time applied");
        Check(output.ToHtmlString().Contains("h1: White rook"), "Black orientation includes reversed board");
        Set(menu, "_lastTick", DateTimeOffset.UtcNow.AddSeconds(-181));
        await Call(menu, "UpdateClock");
        await Call(menu, "SaveAsync");
        Check(api.Store.Value.Result.Contains("time expired") && api.Store.Value.Wins == 1, "Flag fall awards win to Black");

        await menu.OpenSettingsAsync();
        Set(menu, "_draftWhite", true); Set(menu, "_draftMinutes", 0);
        await Call(menu, "NewGameAsync");
        await Call(menu, "CommitAsync", new Move(12, 28));
        await File.WriteAllTextAsync("obj/chess-tests/e4.html", output.ToHtmlString());
        Set(menu, "_connection", new ReadyModule()); Set(menu, "_presentedPly", 0);
        await Call(menu, "ClearSelection");
        Check(output.ToHtmlString().Contains("data-chess-pending=\"true\" style=\"visibility: hidden\""), "Destination is hidden in the Razor render before JavaScript runs");
        await File.WriteAllTextAsync("obj/chess-tests/e4-pending.html", output.ToHtmlString());
        Set(menu, "_connection", null!); Set(menu, "_presentedPly", api.Store.Value!.Moves.Count);
        await Call(menu, "CommitAsync", new Move(51, 35));
        await File.WriteAllTextAsync("obj/chess-tests/d5.html", output.ToHtmlString());
        await Call(menu, "CommitAsync", new Move(28, 35));
        Check(output.ToHtmlString().Contains("Pieces captured by White"), "Capture rail rendered");
        Check(((List<string>)Get(menu, "_notation")!).SequenceEqual(new[] { "e4", "d5", "exd5" }), "Move history contains SAN moves");
        Check(output.ToHtmlString().Split("last-move").Length - 1 == 2, "Last move highlights both squares");
        Check(output.ToHtmlString().Contains("<strong>exd5</strong>"), "Latest move is bold");
        await File.WriteAllTextAsync("obj/chess-tests/played.html", output.ToHtmlString());
        var restored = new CaptureHost();
        var restoredOutput = await renderer.RenderComponentAsync<CaptureHost>(ParameterView.FromDictionary(new Dictionary<string, object?> { ["Api"] = api, ["Capture"] = (Action<ChessMenu>)(c => restored.Menu = c) }));
        Check(restoredOutput.ToHtmlString().Contains("d5: White pawn") && restoredOutput.ToHtmlString().Contains("Win <strong>1"), "Restores board and record");
        Check(((List<int>)Get(restored.Menu!, "_captured")!).SequenceEqual(new[] { -Position.Pawn }), "Restores captured pieces");
        Check(((List<string>)Get(restored.Menu!, "_notation")!).SequenceEqual(new[] { "e4", "d5", "exd5" }), "Restores notation from recorded moves");
        // Simulate the host's context only for the compact request, without starting a search.
        var contextProperty = typeof(HitMenuComponent).GetProperty("Context")!;
        var context = (HitContext)contextProperty.GetValue(menu)!;
        contextProperty.SetValue(menu, context with { IsVisible = true });
        await menu.CompactAsync();
        Check(output.ToHtmlString().Contains("is-compact"), "Pinned visible board compacts");
        await File.WriteAllTextAsync("obj/chess-tests/compact.html",output.ToHtmlString());
        await Call(menu, "ExpandAsync");
        Check(!output.ToHtmlString().Contains("is-compact"), "Board expands on click");
        contextProperty.SetValue(menu, context);
        var captured = (List<int>)Get(menu, "_captured")!;
        captured.Clear(); captured.AddRange(Enumerable.Repeat(-Position.Pawn, 15)); captured.AddRange(Enumerable.Repeat(Position.Pawn, 15));
        await Call(menu, "ClearSelection");
        await File.WriteAllTextAsync("obj/chess-tests/captures.html", output.ToHtmlString());
        await menu.OpenSettingsAsync();
        await Call(menu, "NewGameAsync");
        foreach (var move in new[] { new Move(12, 28), new Move(52, 36), new Move(6, 21), new Move(57, 42), new Move(5, 26), new Move(62, 45) })
            await Call(menu, "CommitAsync", move);
        await File.WriteAllTextAsync("obj/chess-tests/before-castle.html", output.ToHtmlString());
        await Call(menu, "CommitAsync", new Move(4, 6));
        await File.WriteAllTextAsync("obj/chess-tests/castle.html", output.ToHtmlString());

        var blackApi = new TestApi();
        blackApi.Store.Value = new ChessState { PlayerWhite = false, Difficulty = Difficulty.Easy };
        var blackHost = new CaptureHost();
        await renderer.RenderComponentAsync<CaptureHost>(ParameterView.FromDictionary(new Dictionary<string, object?> {
            ["Api"] = blackApi, ["Visible"] = true, ["Capture"] = (Action<ChessMenu>)(c => blackHost.Menu = c) }));
        if (Get(blackHost.Menu!, "_searchTask") is Task computerTask) await computerTask;
        Check(blackApi.Store.Value.Moves.Count == 1 && !(bool)Get(blackHost.Menu!, "_thinking")!, "Choosing Black starts and completes White algorithm opening after visibility initialization");
    });
}
Console.WriteLine("PASS: rendered board, SAN history/save/reload, last-move squares, checkmate flag, captures, White/Black buttons, no FF, new game, draws, clocks and compact/expand.");

var fixture = Path.GetFullPath("artifacts/packages/chess-v1.1.1.hitmenu");
if (File.Exists(fixture))
{
    var store = new ExtensionStore(Path.GetFullPath("obj/chess-tests/install-" + Guid.NewGuid().ToString("N")));
    await using var stream = File.OpenRead(fixture);
    var installed = await store.InstallAsync(stream, Path.GetFileName(fixture));
    Check(installed.Manifest.Id == "dev.hit.chess" && installed.Manifest.Menus.Length == 1, "Real installer validates package");
    Console.WriteLine("PASS: actual HIT installer accepts compiled chess package.");
    await HostRendering.RunAsync(fixture);
}

static Task Call(ChessMenu menu, string name, params object[] args)
{
    var result = typeof(ChessMenu).GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(menu, args);
    // HtmlRenderer does not automatically rerender after directly invoking a private method.
    typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(menu, null);
    return result as Task ?? Task.CompletedTask;
}
static void Set(ChessMenu menu, string name, object value) => typeof(ChessMenu).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(menu, value);
static object? Get(ChessMenu menu, string name) => typeof(ChessMenu).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu);

public sealed class CaptureHost : ComponentBase
{
    [Parameter] public IHitApi Api { get; set; } = default!;
    [Parameter] public bool Visible { get; set; }
    [Parameter] public Action<ChessMenu> Capture { get; set; } = _ => { };
    public ChessMenu? Menu { get; set; }
    protected override void BuildRenderTree(RenderTreeBuilder b)
    {
        var context = new HitContext(true, Visible, true, _ => Task.CompletedTask, _ => Task.CompletedTask, _ => Task.CompletedTask, () => Task.CompletedTask);
        b.OpenComponent<CascadingValue<HitContext>>(0);
        b.AddAttribute(1, "Name", "HitExtension"); b.AddAttribute(2, "Value", context);
        b.AddAttribute(3, "ChildContent", (RenderFragment)(inner => {
            inner.OpenComponent<ChessMenu>(0); inner.AddAttribute(1, "Api", Api);
            inner.AddComponentReferenceCapture(2, c => Capture((ChessMenu)c)); inner.CloseComponent();
        })); b.CloseComponent();
    }
}
public sealed class MemoryStorage : IHitStorage
{
    public ChessState? Value;
    public Task<T?> ReadAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult(Value is null ? default : JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(Value)));
    public Task WriteAsync<T>(string key, T value, CancellationToken cancellationToken = default) { Value = JsonSerializer.Deserialize<ChessState>(JsonSerializer.Serialize(value)); return Task.CompletedTask; }
    public Task DeleteAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
public sealed class TestApi : IHitApi
{
    public MemoryStorage Store { get; } = new();
    public string ExtensionId => "dev.hit.chess";
    public CancellationToken Lifetime => CancellationToken.None;
    public IServiceProvider Services => throw new NotSupportedException();
    public IHitStorage Storage => Store;
    public IHitMedia Media => throw new NotSupportedException();
    public IHitClock Clock => throw new NotSupportedException();
    public IHitAppearance Appearance => throw new NotSupportedException();
    public string AssetUrl(string path) => path;
    public bool OpenLink(string url) => throw new NotSupportedException();
    public void Log(string message) => Console.WriteLine(message);
    public void Notify(string message) => throw new NotSupportedException();
}
public sealed class NoJs : IJSRuntime
{
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => throw new NotSupportedException();
    public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => throw new NotSupportedException();
}
public sealed class ReadyModule : IJSObjectReference
{
    public ValueTask<T> InvokeAsync<T>(string identifier, object?[]? args) => ValueTask.FromResult(default(T)!);
    public ValueTask<T> InvokeAsync<T>(string identifier, CancellationToken cancellationToken, object?[]? args) => InvokeAsync<T>(identifier, args);
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
