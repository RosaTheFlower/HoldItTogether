namespace HIT.Chess.Services;

public sealed class ChessState
{
    public int Schema { get; set; } = 1;
    public Difficulty Difficulty { get; set; } = Difficulty.Medium;
    public bool PlayerWhite { get; set; } = true;
    public int Minutes { get; set; }
    public int Wins { get; set; }
    public int Losses { get; set; }
    public bool SoundEnabled { get; set; } = true;
    public List<Move> Moves { get; set; } = [];
    public string Result { get; set; } = "";
    public double WhiteSeconds { get; set; }
    public double BlackSeconds { get; set; }
}
