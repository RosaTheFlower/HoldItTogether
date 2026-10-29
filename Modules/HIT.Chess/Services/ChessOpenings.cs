namespace HIT.Chess.Services;

/// <summary>Small offline repertoire, indexed by exact legal board state.</summary>
public static class ChessOpenings
{
    private static readonly Dictionary<string,Move[]> Book=Build();
    private static string Key(Position p)=>string.Join(',',p.Board)+$"/{p.White}/{p.Castling}/{p.EnPassant}";
    private static Dictionary<string,Move[]> Build()
    {
        var entries=new Dictionary<string,HashSet<Move>>();
        string[] lines=["e2e4 e7e5 g1f3 b8c6 f1b5 a7a6","e2e4 e7e5 g1f3 b8c6 f1c4 g8f6","e2e4 c7c5 g1f3 d7d6 d2d4 c5d4 f3d4 g8f6","e2e4 e7e6 d2d4 d7d5 b1c3 g8f6","e2e4 c7c6 d2d4 d7d5 b1c3 d5e4 c3e4","d2d4 d7d5 c2c4 e7e6 b1c3 g8f6","d2d4 g8f6 c2c4 g7g6 b1c3 f8g7","c2c4 e7e5 b1c3 g8f6 g1f3 b8c6","g1f3 d7d5 d2d4 g8f6 c2c4 e7e6"];
        foreach(var line in lines){var p=Position.Initial();foreach(var text in line.Split(' ')){var move=p.LegalMoves().Single(m=>Position.Square(m.From)+Position.Square(m.To)==text);var key=Key(p);if(!entries.TryGetValue(key,out var moves))entries[key]=moves=[];moves.Add(move);p=p.Play(move);}}
        return entries.ToDictionary(pair=>pair.Key,pair=>pair.Value.ToArray());
    }
    public static Move? Choose(Position position,Random random)
    {
        if(!Book.TryGetValue(Key(position),out var moves))return null;
        var legal=position.LegalMoves();var candidates=moves.Where(legal.Contains).ToArray();
        return candidates.Length==0?null:candidates[random.Next(candidates.Length)];
    }
}
