using System.Globalization;
using System.Text.RegularExpressions;

namespace HoldItTogether.Services.Themes;

public readonly record struct HitColor(byte Red, byte Green, byte Blue, byte Alpha = 255)
{
    public string Hex => $"#{Red:x2}{Green:x2}{Blue:x2}" + (Alpha == 255 ? "" : $"{Alpha:x2}");

    public static bool TryParse(string? value, out HitColor color)
    {
        color = default;
        if (value is null || !Regex.IsMatch(value, "^#[0-9a-fA-F]{6}([0-9a-fA-F]{2})?$", RegexOptions.CultureInvariant)) return false;
        byte Part(int start) => byte.Parse(value.AsSpan(start, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        color = new(Part(1), Part(3), Part(5), value.Length == 9 ? Part(7) : (byte)255);
        return true;
    }

    public (double Hue, double Saturation, double Brightness) Hsv()
    {
        var r = Red / 255d; var g = Green / 255d; var b = Blue / 255d;
        var max = Math.Max(r, Math.Max(g, b)); var min = Math.Min(r, Math.Min(g, b)); var delta = max - min;
        var hue = delta == 0 ? 0 : max == r ? 60 * (((g - b) / delta + 6) % 6) : max == g ? 60 * ((b - r) / delta + 2) : 60 * ((r - g) / delta + 4);
        return (hue, max == 0 ? 0 : delta / max * 100, max * 100);
    }

    public static HitColor FromHsv(double hue, double saturation, double brightness, byte alpha)
    {
        var h = ((hue % 360) + 360) % 360 / 60; var s = Math.Clamp(saturation, 0, 100) / 100; var v = Math.Clamp(brightness, 0, 100) / 100;
        var c = v * s; var x = c * (1 - Math.Abs(h % 2 - 1)); var m = v - c;
        var (r, g, b) = h switch { < 1 => (c, x, 0d), < 2 => (x, c, 0d), < 3 => (0d, c, x), < 4 => (0d, x, c), < 5 => (x, 0d, c), _ => (c, 0d, x) };
        return new((byte)Math.Round((r + m) * 255), (byte)Math.Round((g + m) * 255), (byte)Math.Round((b + m) * 255), alpha);
    }
}
