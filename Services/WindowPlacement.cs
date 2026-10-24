using System.Drawing;

namespace HoldItTogether.Services;

internal readonly record struct WindowAnchor(double X, double Y, double PivotX, double PivotY);

internal static class WindowPlacement
{
    public static WindowAnchor SelectAnchor(Rectangle bounds, Rectangle? workArea)
    {
        var pivotX = 0.5;
        var pivotY = 0.5;

        if (workArea is { Width: > 0, Height: > 0 } area)
        {
            var relativeX = (bounds.Left + bounds.Width / 2d - area.Left) / area.Width;
            var relativeY = (bounds.Top + bounds.Height / 2d - area.Top) / area.Height;
            var central = relativeX >= 1d / 3 && relativeX <= 2d / 3 &&
                          relativeY >= 1d / 3 && relativeY <= 2d / 3;

            // Only the central region grows in all four directions. Elsewhere,
            // use the nearest corner, considering both horizontal and vertical
            // distance. Keep that corner fixed until the next drag ends.
            if (!central)
            {
                pivotX = relativeX < 0.5 ? 0 : 1;
                pivotY = relativeY < 0.5 ? 0 : 1;
            }
        }

        return AnchorAt(bounds, pivotX, pivotY);
    }

    public static WindowAnchor AnchorAt(Rectangle bounds, double pivotX, double pivotY) =>
        new(bounds.Left + bounds.Width * pivotX, bounds.Top + bounds.Height * pivotY, pivotX, pivotY);

    public static Rectangle Calculate(
        WindowAnchor anchor, int width, int height, Rectangle? workArea) =>
        Constrain(new Rectangle(
            (int)Math.Round(anchor.X - width * anchor.PivotX),
            (int)Math.Round(anchor.Y - height * anchor.PivotY),
            width, height), workArea);

    public static Rectangle Constrain(Rectangle bounds, Rectangle? workArea)
    {
        if (workArea is not { Width: > 0, Height: > 0 } area)
        {
            return bounds;
        }

        return new Rectangle(
            Math.Clamp(bounds.Left, area.Left, Math.Max(area.Left, area.Right - bounds.Width)),
            Math.Clamp(bounds.Top, area.Top, Math.Max(area.Top, area.Bottom - bounds.Height)),
            bounds.Width, bounds.Height);
    }

    public static Point MonitorReference(WindowAnchor anchor, Rectangle? workArea)
    {
        var x = (int)Math.Round(anchor.X);
        var y = (int)Math.Round(anchor.Y);

        // An anchor on a shared monitor edge belongs to its original monitor,
        // rather than to the adjacent monitor on the other side of that edge.
        if (workArea is { Width: > 0, Height: > 0 } area)
        {
            x = Math.Clamp(x, area.Left, area.Right - 1);
            y = Math.Clamp(y, area.Top, area.Bottom - 1);
        }

        return new Point(x, y);
    }
}