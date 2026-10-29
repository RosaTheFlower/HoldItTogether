using System.Drawing;
using HoldItTogether.Services.Settings;

namespace HoldItTogether.Services;

internal readonly record struct WindowAnchor(double X, double Y, double PivotX, double PivotY);

internal static class WindowPlacement
{
    public static RectangleF FullscreenTarget(RectangleF current,Rectangle monitor,SavedWindowPosition? saved)
    {
        if(saved is {IsValid:true})
        {
            var x=monitor.Left+(saved.X-saved.WorkLeft)/saved.WorkWidth*monitor.Width;
            var y=monitor.Top+(saved.Y-saved.WorkTop)/saved.WorkHeight*monitor.Height;
            current.Location=new((float)(x-current.Width*saved.PivotX),(float)(y-current.Height*saved.PivotY));
        }
        return ConstrainSurface(current,monitor);
    }
    public static PointF InterpolatePosition(PointF from,PointF to,double progress)
    {
        var eased=1-Math.Pow(1-Math.Clamp(progress,0,1),3);
        return new((float)(from.X+(to.X-from.X)*eased),(float)(from.Y+(to.Y-from.Y)*eased));
    }
    public static WindowAnchor SelectAnchor(RectangleF bounds, Rectangle? workArea, bool centerMagnetized = false)
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

        var anchor = AnchorAt(bounds, pivotX, pivotY);
        if (centerMagnetized && workArea is { } centerArea)
        {
            var centerX = centerArea.Left + centerArea.Width / 2d;
            var centerY = centerArea.Top + centerArea.Height / 2d;
            var alignedX = Math.Abs(bounds.Left + bounds.Width / 2d - centerX) < 2;
            var alignedY = Math.Abs(bounds.Top + bounds.Height / 2d - centerY) < 2;
            // Preserve each snapped axis even when the other axis chooses an edge pivot.
            anchor = new(alignedX ? centerX : anchor.X, alignedY ? centerY : anchor.Y,
                alignedX ? .5 : anchor.PivotX, alignedY ? .5 : anchor.PivotY);
        }
        return anchor;
    }

    public static WindowAnchor AnchorAt(RectangleF bounds, double pivotX, double pivotY) =>
        new(bounds.Left + bounds.Width * pivotX, bounds.Top + bounds.Height * pivotY, pivotX, pivotY);

    public static Rectangle Calculate(
        WindowAnchor anchor, int width, int height, Rectangle? workArea) =>
        Constrain(new Rectangle(
            (int)Math.Round(anchor.X - width * anchor.PivotX),
            (int)Math.Round(anchor.Y - height * anchor.PivotY),
            width, height), workArea);

    public static Rectangle PlaceSurface(WindowAnchor anchor, int canvasWidth, int canvasHeight,
        RectangleF localSurface, Rectangle? workArea)
    {
        var surface = ConstrainSurface(new RectangleF(
            (float)(anchor.X - localSurface.Width * anchor.PivotX),
            (float)(anchor.Y - localSurface.Height * anchor.PivotY),localSurface.Width,localSurface.Height),workArea);
        // Transparent backing storage may extend beyond the monitor. Only visible content is constrained.
        return new Rectangle((int)Math.Round(surface.Left-localSurface.Left),
            (int)Math.Round(surface.Top-localSurface.Top),canvasWidth,canvasHeight);
    }

    public static RectangleF ConstrainSurface(RectangleF surface, Rectangle? workArea) =>
        workArea is { Width: > 0, Height: > 0 } area ? new RectangleF(
            Math.Clamp(surface.Left,area.Left,Math.Max(area.Left,area.Right-surface.Width)),
            Math.Clamp(surface.Top,area.Top,Math.Max(area.Top,area.Bottom-surface.Height)),surface.Width,surface.Height) : surface;

    public static Rectangle DragSurface(Rectangle canvas,RectangleF visible,Point cursor,PointF grab,Rectangle? workArea,
        bool magnetize=false,Rectangle? snapArea=null,double threshold=12,bool magnetizeCenter=false)
    {
        var moved=ConstrainSurface(new RectangleF(cursor.X-grab.X,cursor.Y-grab.Y,visible.Width,visible.Height),workArea);
        if(magnetize&&snapArea is { } edge)
        {
            var left=Math.Abs(moved.Left-edge.Left);var right=Math.Abs(moved.Right-edge.Right);
            var top=Math.Abs(moved.Top-edge.Top);var bottom=Math.Abs(moved.Bottom-edge.Bottom);
            if(Math.Min(left,right)<=threshold)moved.X=left<=right?edge.Left:edge.Right-moved.Width;
            if(Math.Min(top,bottom)<=threshold)moved.Y=top<=bottom?edge.Top:edge.Bottom-moved.Height;
            if(magnetizeCenter){
                var centerX=edge.Left+(edge.Width-moved.Width)/2f;var centerY=edge.Top+(edge.Height-moved.Height)/2f;
                if(Math.Abs(moved.X-centerX)<=threshold*2.5)moved.X=centerX;
                if(Math.Abs(moved.Y-centerY)<=threshold*2.5)moved.Y=centerY;
            }
        }
        return new Rectangle((int)Math.Round(moved.Left-(visible.Left-canvas.Left)),
            (int)Math.Round(moved.Top-(visible.Top-canvas.Top)),canvas.Width,canvas.Height);
    }

    public static Rectangle InsetWorkArea(Rectangle area,int reachPercent)
    {
        var inset=(100-Math.Clamp(reachPercent,85,100))/200d;
        var padding=(int)Math.Round(Math.Min(area.Width,area.Height)*inset);
        return new Rectangle(area.Left+padding,area.Top+padding,Math.Max(1,area.Width-2*padding),Math.Max(1,area.Height-2*padding));
    }

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
