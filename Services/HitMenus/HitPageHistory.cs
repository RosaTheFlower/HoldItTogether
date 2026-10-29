namespace HoldItTogether.Services.HitMenus;

/// <summary>Session-local, bounded history of stable pages within one retained menu.</summary>
public sealed class HitPageHistory
{
    private readonly List<string> _pages = [];
    private int _position = -1;
    public void Visit(string page)
    {
        if (string.IsNullOrWhiteSpace(page) || page.Length > 100 || (_position >= 0 && _pages[_position] == page)) return;
        _pages.RemoveRange(_position + 1, _pages.Count - _position - 1);
        _pages.Add(page);
        if (_pages.Count > 50) _pages.RemoveAt(0);
        _position = _pages.Count - 1;
    }
    public string? Peek(int direction) => _position + direction >= 0 && _position + direction < _pages.Count ? _pages[_position + direction] : null;
    public void Move(int direction) { if (Peek(direction) is not null) _position += direction; }
}
