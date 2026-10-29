using System.Diagnostics;

namespace HIT.Chess.Services;

public enum Difficulty { Easy, Medium, Hard }

// Classical, local search: hand-written evaluation, negamax and alpha-beta pruning.
// No model, network, learned weights or external chess service is involved.
public sealed class ChessSearch
{
    private readonly Random _random;
    public ChessSearch(Random? random=null)=>_random=random??Random.Shared;
    private readonly Stopwatch _clock = new();
    private CancellationToken _token;
    private int _budget, _nodes;
    public static readonly int[] Values = [0, 100, 320, 335, 500, 900, 20000];

    public Move? Choose(Position position, Difficulty difficulty, CancellationToken token)
    {
        token.ThrowIfCancellationRequested();
        if(ChessOpenings.Choose(position,_random) is { } opening)return opening;
        _token = token; _nodes = 0;
        _budget = difficulty switch { Difficulty.Easy => 180, Difficulty.Medium => 750, _ => 2200 };
        int depthLimit = difficulty switch { Difficulty.Easy => 1, Difficulty.Medium => 3, _ => 5 };
        _clock.Restart();
        var moves = Order(position, position.LegalMoves()).ToList();
        if (moves.Count == 0) return null;
        Move best = moves[0];
        for (int depth = 1; depth <= depthLimit; depth++)
        {
            var scored = new List<(Move Move, int Score)>();
            try
            {
                foreach (var move in moves)
                {
                    CheckBudget();
                    int score = -Search(position.Play(move), depth - 1, -100000, 100000, 1);
                    scored.Add((move, score));
                }
            }
            catch (SearchExpiredException) { break; }
            scored.Sort((a, b) => b.Score.CompareTo(a.Score));
            best = scored[0].Move;
            if (difficulty == Difficulty.Easy)
            {
                var candidates = scored.Where(s => s.Score >= scored[0].Score - 130).ToArray();
                best = candidates[_random.Next(candidates.Length)].Move;
            }
            moves = scored.Select(s => s.Move).ToList();
        }
        token.ThrowIfCancellationRequested();
        return best;
    }

    private int Search(Position p, int depth, int alpha, int beta, int ply)
    {
        if ((++_nodes & 127) == 0) CheckBudget();
        var legal = p.LegalMoves();
        bool check = p.InCheck(p.White);
        if (legal.Count == 0) return check ? -90000 + ply : 0;
        if (p.HalfMoves >= 100 || p.InsufficientMaterial()) return 0;
        if (depth <= 0)
        {
            int stand = Evaluate(p);
            if (depth <= -5) return stand;
            if (!check)
            {
                if (stand >= beta) return beta;
                alpha = Math.Max(alpha, stand);
                legal = legal.Where(m => p.Captured(m) != 0 || m.Promotion != 0).ToList();
            }
        }
        foreach (var move in Order(p, legal))
        {
            int score = -Search(p.Play(move), depth - 1, -beta, -alpha, ply + 1);
            if (score >= beta) return beta;
            alpha = Math.Max(alpha, score);
        }
        return alpha;
    }

    private static IEnumerable<Move> Order(Position p, IEnumerable<Move> moves) => moves.OrderByDescending(m =>
        10 * Values[Math.Abs(p.Captured(m))] - (p.Captured(m) == 0 ? 0 : Values[Math.Abs(p.Board[m.From])]) + Values[m.Promotion]);

    private static int Evaluate(Position p)
    {
        int score = 0;
        for (int s = 0; s < 64; s++)
        {
            int piece = p.Board[s], type = Math.Abs(piece);
            if (piece == 0) continue;
            int rank = piece > 0 ? s / 8 : 7 - s / 8;
            int center = 7 - Math.Abs(2 * (s % 8) - 7) - Math.Abs(2 * (s / 8) - 7);
            int positional = type switch { Position.Pawn => rank * 9 + center * 2, Position.Knight => center * 9,
                Position.Bishop => center * 5, Position.Rook => rank * 3, Position.Queen => center * 2, _ => rank < 2 ? 12 : -center * 3 };
            score += Math.Sign(piece) * (Values[type] + positional);
        }
        return p.White ? score : -score;
    }

    private void CheckBudget()
    {
        _token.ThrowIfCancellationRequested();
        if (_clock.ElapsedMilliseconds >= _budget) throw new SearchExpiredException();
    }
    private sealed class SearchExpiredException : Exception { }
}
