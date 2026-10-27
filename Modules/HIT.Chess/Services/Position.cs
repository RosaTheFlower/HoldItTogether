namespace HIT.Chess.Services;

// Positive pieces are White. Squares run from a1 (0) to h8 (63).
public readonly record struct Move(int From, int To, int Promotion = 0);

public sealed class Position
{
    public const int Pawn = 1, Knight = 2, Bishop = 3, Rook = 4, Queen = 5, King = 6;
    public int[] Board { get; set; } = new int[64];
    public bool White { get; set; } = true;
    public int Castling { get; set; } = 15;
    public int EnPassant { get; set; } = -1;
    public int HalfMoves { get; set; }

    public static Position Initial() => FromFen("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

    public static Position FromFen(string fen)
    {
        var parts = fen.Split(' ');
        var p = new Position { White = parts[1] == "w", Castling = 0, HalfMoves = int.Parse(parts[4]) };
        var ranks = parts[0].Split('/');
        for (int r = 0; r < 8; r++)
        {
            int f = 0;
            foreach (var c in ranks[7 - r])
            {
                if (char.IsDigit(c)) { f += c - '0'; continue; }
                int piece = " pnbrqk".IndexOf(char.ToLowerInvariant(c));
                p.Board[r * 8 + f++] = char.IsUpper(c) ? piece : -piece;
            }
        }
        for (int i = 0; i < 4; i++) if (parts[2].Contains("KQkq"[i])) p.Castling |= 1 << i;
        if (parts[3] != "-") p.EnPassant = (parts[3][1] - '1') * 8 + parts[3][0] - 'a';
        return p;
    }

    public Position Copy() => new() { Board = (int[])Board.Clone(), White = White, Castling = Castling, EnPassant = EnPassant, HalfMoves = HalfMoves };
    public static string Square(int square) => $"{(char)('a' + square % 8)}{square / 8 + 1}";
    public bool InCheck(bool white) => Attacked(Array.IndexOf(Board, white ? King : -King), !white);

    public bool Attacked(int square, bool byWhite)
    {
        if (square < 0) return true;
        int sign = byWhite ? 1 : -1, x = square % 8, y = square / 8;
        for (int from = 0; from < 64; from++)
        {
            int piece = Board[from];
            if (piece * sign <= 0) continue;
            int dx = x - from % 8, dy = y - from / 8, type = Math.Abs(piece);
            if (type == Pawn && dy == sign && Math.Abs(dx) == 1) return true;
            if (type == Knight && Math.Abs(dx) * Math.Abs(dy) == 2) return true;
            if (type == King && Math.Max(Math.Abs(dx), Math.Abs(dy)) == 1) return true;
            bool diagonal = Math.Abs(dx) == Math.Abs(dy) && dx != 0;
            bool straight = (dx == 0) != (dy == 0);
            if (!((diagonal && (type == Bishop || type == Queen)) || (straight && (type == Rook || type == Queen)))) continue;
            int step = Math.Sign(dx) + 8 * Math.Sign(dy), cursor = from + step;
            while (cursor != square && Board[cursor] == 0) cursor += step;
            if (cursor == square) return true;
        }
        return false;
    }

    public List<Move> LegalMoves()
    {
        var result = new List<Move>(48);
        int side = White ? 1 : -1;
        void Add(int from, int to)
        {
            if (Math.Abs(Board[to]) == King) return;
            if (Math.Abs(Board[from]) == Pawn && (to / 8 == 0 || to / 8 == 7))
                foreach (int promotion in new[] { Queen, Rook, Bishop, Knight }) Test(new(from, to, promotion));
            else Test(new(from, to));
        }
        void Test(Move move)
        {
            var next = Play(move);
            if (!next.InCheck(White)) result.Add(move);
        }
        for (int from = 0; from < 64; from++)
        {
            if (Board[from] * side <= 0) continue;
            int x = from % 8, y = from / 8, type = Math.Abs(Board[from]);
            if (type == Pawn)
            {
                int next = from + 8 * side;
                if (next is < 0 or >= 64) continue;
                if (Board[next] == 0)
                {
                    Add(from, next);
                    if (y == (White ? 1 : 6) && Board[next + 8 * side] == 0) Add(from, next + 8 * side);
                }
                foreach (int dx in new[] { -1, 1 })
                {
                    int to = next + dx;
                    if (x + dx is < 0 or > 7) continue;
                    if (Board[to] * side < 0 || (to == EnPassant && Board[to] == 0 && Board[to - 8 * side] == -side * Pawn)) Add(from, to);
                }
                continue;
            }
            var directions = type == Knight ? KnightDirections : type == Bishop ? Diagonals : type == Rook ? Straights : AllDirections;
            foreach (var (dx, dy) in directions)
            {
                int tx = x + dx, ty = y + dy;
                while (tx is >= 0 and < 8 && ty is >= 0 and < 8)
                {
                    int to = ty * 8 + tx;
                    if (Board[to] * side > 0) break;
                    Add(from, to);
                    if (Board[to] != 0 || type is King or Knight) break;
                    tx += dx; ty += dy;
                }
            }
            int home = White ? 4 : 60, rights = White ? Castling : Castling >> 2;
            if (type != King || from != home || InCheck(White)) continue;
            if ((rights & 1) != 0 && Board[home + 3] == side * Rook && Board[home + 1] == 0 && Board[home + 2] == 0 &&
                !Attacked(home + 1, !White) && !Attacked(home + 2, !White)) Add(from, home + 2);
            if ((rights & 2) != 0 && Board[home - 4] == side * Rook && Board[home - 1] == 0 && Board[home - 2] == 0 && Board[home - 3] == 0 &&
                !Attacked(home - 1, !White) && !Attacked(home - 2, !White)) Add(from, home - 2);
        }
        return result;
    }

    public int Captured(Move move) => Board[move.To] != 0 ? Board[move.To] :
        Math.Abs(Board[move.From]) == Pawn && move.To == EnPassant ? -Board[move.From] : 0;

    public Position Play(Move move)
    {
        var p = Copy();
        int piece = Board[move.From], side = White ? 1 : -1;
        p.HalfMoves = Math.Abs(piece) == Pawn || Captured(move) != 0 ? 0 : HalfMoves + 1;
        p.Board[move.From] = 0;
        if (Math.Abs(piece) == Pawn && move.To == EnPassant && Board[move.To] == 0) p.Board[move.To - side * 8] = 0;
        p.Board[move.To] = move.Promotion != 0 ? side * move.Promotion : piece;
        if (Math.Abs(piece) == King)
        {
            p.Castling &= White ? ~3 : ~12;
            if (Math.Abs(move.To - move.From) == 2)
            {
                int rookFrom = move.To > move.From ? move.From + 3 : move.From - 4;
                int rookTo = move.To > move.From ? move.From + 1 : move.From - 1;
                p.Board[rookTo] = p.Board[rookFrom]; p.Board[rookFrom] = 0;
            }
        }
        foreach (int square in new[] { move.From, move.To })
            p.Castling &= square switch { 0 => ~2, 7 => ~1, 56 => ~8, 63 => ~4, _ => ~0 };
        p.EnPassant = Math.Abs(piece) == Pawn && Math.Abs(move.To - move.From) == 16 ? (move.To + move.From) / 2 : -1;
        p.White = !White;
        return p;
    }

    // A non-capturable en-passant target does not distinguish repeated positions.
    public string Key() => string.Join(',', Board) + $"/{White}/{Castling}/" +
        (EnPassant >= 0 && LegalMoves().Any(m => m.To == EnPassant && Math.Abs(Board[m.From]) == Pawn) ? EnPassant : -1);

    public bool InsufficientMaterial()
    {
        var pieces = Enumerable.Range(0, 64).Where(s => Board[s] != 0 && Math.Abs(Board[s]) != King).ToArray();
        if (pieces.Any(s => Math.Abs(Board[s]) is Pawn or Rook or Queen)) return false;
        if (pieces.Length <= 1) return true;
        return pieces.All(s => Math.Abs(Board[s]) == Bishop) && pieces.Select(s => (s % 8 + s / 8) % 2).Distinct().Count() == 1;
    }

    private static readonly (int, int)[] KnightDirections = [(1, 2), (2, 1), (-1, 2), (-2, 1), (1, -2), (2, -1), (-1, -2), (-2, -1)];
    private static readonly (int, int)[] Diagonals = [(1, 1), (1, -1), (-1, 1), (-1, -1)];
    private static readonly (int, int)[] Straights = [(1, 0), (-1, 0), (0, 1), (0, -1)];
    private static readonly (int, int)[] AllDirections = [.. Diagonals, .. Straights];
}
