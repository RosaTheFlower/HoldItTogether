namespace HIT.Chess.Services;

/// <summary>Standard algebraic notation, derived from the same legal position used to play.</summary>
public static class ChessNotation
{
    public static string Format(Position position, Move move)
    {
        int type = Math.Abs(position.Board[move.From]);
        string text;
        if (type == Position.King && Math.Abs(move.To - move.From) == 2)
            text = move.To > move.From ? "O-O" : "O-O-O";
        else
        {
            bool capture = position.Captured(move) != 0;
            text = type == Position.Pawn ? capture ? ((char)('a' + move.From % 8)).ToString() : "" : Symbol(type);
            if (type != Position.Pawn)
            {
                var others = position.LegalMoves().Where(m => m.To == move.To && m.From != move.From && position.Board[m.From] == position.Board[move.From]).ToArray();
                if (others.Length > 0)
                {
                    if (others.All(m => m.From % 8 != move.From % 8)) text += (char)('a' + move.From % 8);
                    else if (others.All(m => m.From / 8 != move.From / 8)) text += move.From / 8 + 1;
                    else text += Position.Square(move.From);
                }
            }
            text += (capture ? "x" : "") + Position.Square(move.To);
            if (move.Promotion != 0) text += "=" + Symbol(move.Promotion);
        }
        var next = position.Play(move);
        if (next.InCheck(next.White)) text += next.LegalMoves().Count == 0 ? "#" : "+";
        return text;
    }

    private static string Symbol(int type) => type switch { Position.Knight => "N", Position.Bishop => "B", Position.Rook => "R", Position.Queen => "Q", Position.King => "K", _ => "" };
}
