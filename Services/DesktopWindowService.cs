using HoldItTogether.Services.Settings;
using System.Drawing;
using System.Runtime.InteropServices;
using Photino.NET;

namespace HoldItTogether.Services;

public sealed class DesktopWindowService
{
    private readonly HitSettingsService? _settings;
    private readonly WindowPositionStore _positions;
    private HitSettings _options;
    private bool _positionLoaded,_restoring;
    private Rectangle? _dragBounds;
    public DesktopWindowService(HitSettingsService? settings = null,WindowPositionStore? positions=null)
    { _settings=settings;_positions=positions??new();_options=settings?.Current??new(); }

    private PhotinoWindow? _window;
    private WindowsWindowBehavior? _windowsBehavior;
    private WindowAnchor? _anchor;
    private Rectangle? _workArea;
    private PointerBounds? _pointerBounds;
    private DragOrigin? _dragOrigin;

    public readonly record struct WindowOrigin(double X, double Y);

    public bool UsesNativePointerTracking => OperatingSystem.IsWindows();

    public void ConfigureInitialLocation(PhotinoWindow window)
    {
        if(OperatingSystem.IsWindows()&&TryRestoreAnchor()&&_anchor is { } anchor)
        {
            var placement=WindowPlacement.PlaceSurface(anchor,104,104,new RectangleF(24,24,56,56),PaddedArea(_workArea));
            window.SetLocation(placement.Location);
        }
        else window.Center();
    }

    public void Attach(PhotinoWindow window)
    {
        _window = window;
        window.RegisterWindowCreatedHandler((_, _) =>
        {
            if (OperatingSystem.IsWindows())
            {
                _windowsBehavior = new WindowsWindowBehavior(
                    window.WindowHandle, _settings?.Current.AlwaysOnTop ?? true);
            }

            _settings?.BindRuntime(value => window.Invoke(() =>
            {
                var changed=value.WindowAllowOtherMonitors!=_options.WindowAllowOtherMonitors||value.WindowEdgeReachPercent!=_options.WindowEdgeReachPercent;
                _options=value;
                if (_windowsBehavior is { } behavior)
                    behavior.ApplySettings(value.AlwaysOnTop, value.SystemTray, value.ForceAlwaysOnTop);
                else
                    window.SetTopMost(value.AlwaysOnTop);
                if(changed)ReapplyPlacement(window);
            }));
        });
    }
    public WindowOrigin ResizeToContent(
        double width, double height, double scale,
        double contentLeft, double contentTop,
        double contentWidth, double contentHeight, double borderRadius)
    {
        if (_window is not { } window ||
            !double.IsFinite(width) || !double.IsFinite(height) ||
            !double.IsFinite(scale) || width <= 0 || height <= 0 || scale <= 0 ||
            !double.IsFinite(contentLeft) || !double.IsFinite(contentTop) ||
            !double.IsFinite(contentWidth) || !double.IsFinite(contentHeight) ||
            !double.IsFinite(borderRadius) || contentWidth <= 0 || contentHeight <= 0)
        {
            return new WindowOrigin(0.5, 0.5);
        }

        var nextWidth = (int)Math.Ceiling(Math.Clamp(width * scale, 1, 4096));
        var nextHeight = (int)Math.Ceiling(Math.Clamp(height * scale, 1, 4096));
        var origin = new WindowOrigin(0.5, 0.5);

        window.Invoke(() =>
        {
            if (_anchor is null)
            {
                InitializeAnchor(window);
            }

            var anchor = _anchor!.Value;
            if (OperatingSystem.IsWindows())
            {
                _workArea = ResolveWorkArea(WindowPlacement.MonitorReference(anchor, _workArea));
            }

            var placement = WindowPlacement.PlaceSurface(anchor,nextWidth,nextHeight,
                new RectangleF((float)(contentLeft*scale),(float)(contentTop*scale),
                    (float)(contentWidth*scale),(float)(contentHeight*scale)),PaddedArea(_workArea));
            if (WindowBounds(window) != placement)
            {
                SetBounds(window, placement);
            }
            _windowsBehavior?.SetContentRegion(contentLeft * scale, contentTop * scale,
                contentWidth * scale, contentHeight * scale, borderRadius * scale);

            // A single snapshot avoids false hover exits during native moves.
            Volatile.Write(ref _pointerBounds, new PointerBounds(
                placement.Left + contentLeft * scale,
                placement.Top + contentTop * scale,
                contentWidth * scale,
                contentHeight * scale,
                Math.Max(0, borderRadius * scale),
                scale));
            if(_restoring)
            {
                _restoring=false;
                _anchor=WindowPlacement.AnchorAt(new RectangleF((float)(placement.Left+contentLeft*scale),(float)(placement.Top+contentTop*scale),
                    (float)(contentWidth*scale),(float)(contentHeight*scale)),anchor.PivotX,anchor.PivotY);
            }

            origin = new WindowOrigin(anchor.PivotX, anchor.PivotY);
        });

        return origin;
    }

    public WindowOrigin PrepareSurface()
    {
        if (_window is not { } window) return new(.5,.5);
        var origin = new WindowOrigin(.5,.5);
        window.Invoke(() =>
        {
            window.SetZoom(100);
            if (_anchor is null) InitializeAnchor(window);
            origin = new(_anchor!.Value.PivotX,_anchor.Value.PivotY);
        });
        return origin;
    }

    public bool TryBeginDrag(double offsetX, double offsetY, double scale)
    {
        if (_window is not { } window || _windowsBehavior is null ||
            !double.IsFinite(offsetX) || !double.IsFinite(offsetY) ||
            !double.IsFinite(scale) || scale <= 0)
        {
            return false;
        }

        window.Invoke(() =>
        {
            if (_anchor is null)
            {
                InitializeAnchor(window);
            }

            _dragOrigin = new DragOrigin(offsetX * scale, offsetY * scale);
            var areas=_options.WindowAllowOtherMonitors?WindowsWindowBehavior.GetWorkAreas():
                WindowsWindowBehavior.GetPrimaryWorkArea() is { } primary?[primary]:Array.Empty<Rectangle>();
            _dragBounds=areas.Length>0?areas.Select(area=>WindowPlacement.InsetWorkArea(area,_options.WindowEdgeReachPercent)).Aggregate(Rectangle.Union):null;
        });
        return true;
    }

    public void MoveDrag()
    {
        if (_window is not { } window || _windowsBehavior is null)
        {
            return;
        }

        window.Invoke(() =>
        {
            if (_dragOrigin is not { } drag || Volatile.Read(ref _pointerBounds) is not { } visible || !GetCursorPos(out var cursor))
            {
                return;
            }

            var current = WindowBounds(window);
            _workArea = ResolveWorkArea(cursor);
            var frame=new RectangleF((float)visible.Left,(float)visible.Top,(float)visible.Width,(float)visible.Height);
            var placement=WindowPlacement.DragSurface(current,frame,cursor,new PointF((float)drag.OffsetX,(float)drag.OffsetY),_dragBounds??PaddedArea(_workArea),
                _options.WindowMagnetize,PaddedArea(_workArea),12*visible.Scale);
            var surface=new RectangleF(frame.Left+placement.Left-current.Left,frame.Top+placement.Top-current.Top,frame.Width,frame.Height);

            var anchor = _anchor!.Value;
            _anchor = WindowPlacement.AnchorAt(surface, anchor.PivotX, anchor.PivotY);
            MoveBounds(window, current, placement);
        });
    }

    public WindowOrigin EndDrag()
    {
        if (_window is not { } window || _dragOrigin is null)
        {
            return _anchor is { } current ? new(current.PivotX,current.PivotY) : new(.5,.5);
        }

        window.Invoke(() =>
        {
            _dragOrigin = null;
            // Choose once after release, without moving the current window.
            // This corner stays fixed through both expansion and collapse.
            SelectAnchor(window);
            SavePosition();
        });
        return new(_anchor!.Value.PivotX,_anchor.Value.PivotY);
    }

    // Disposal may run after the native window has already been destroyed.
    public void CancelDrag() => _dragOrigin = null;

    private void SelectAnchor(PhotinoWindow window)
    {
        var bounds = Volatile.Read(ref _pointerBounds) is { } surface ?
            new RectangleF((float)surface.Left,(float)surface.Top,(float)surface.Width,(float)surface.Height) : WindowBounds(window);
        if (OperatingSystem.IsWindows())
        {
            _workArea = ResolveWorkArea(new Point((int)(bounds.Left + bounds.Width / 2), (int)(bounds.Top + bounds.Height / 2)));
        }

        _anchor = WindowPlacement.SelectAnchor(bounds, _workArea);
    }
    private Rectangle? PaddedArea(Rectangle? area)=>area is { } value?WindowPlacement.InsetWorkArea(value,_options.WindowEdgeReachPercent):null;
    private Rectangle? ResolveWorkArea(Point reference)=>
        (_options.WindowAllowOtherMonitors?WindowsWindowBehavior.GetWorkArea(reference):WindowsWindowBehavior.GetPrimaryWorkArea())??_workArea;
    private void InitializeAnchor(PhotinoWindow window)
    {
        if(TryRestoreAnchor())return;
        SelectAnchor(window);
    }
    private bool TryRestoreAnchor()
    {
        if(!_positionLoaded)
        {
            _positionLoaded=true;
            if(_positions.Load() is { } saved)
            {
                var oldArea=new Rectangle(saved.WorkLeft,saved.WorkTop,saved.WorkWidth,saved.WorkHeight);
                var oldAnchor=new WindowAnchor(saved.X,saved.Y,saved.PivotX,saved.PivotY);
                _workArea=ResolveWorkArea(WindowPlacement.MonitorReference(oldAnchor,oldArea));
                if(_workArea is { } area)
                {
                    _anchor=new WindowAnchor(area.Left+(saved.X-oldArea.Left)/oldArea.Width*area.Width,
                        area.Top+(saved.Y-oldArea.Top)/oldArea.Height*area.Height,saved.PivotX,saved.PivotY);
                    _restoring=true;return true;
                }
            }
        }
        return false;
    }
    private void ReapplyPlacement(PhotinoWindow window)
    {
        if(Volatile.Read(ref _pointerBounds) is not { } visible||_anchor is not { } anchor)return;
        _workArea=ResolveWorkArea(new Point((int)(visible.Left+visible.Width/2),(int)(visible.Top+visible.Height/2)));
        var frame=WindowPlacement.ConstrainSurface(new((float)visible.Left,(float)visible.Top,(float)visible.Width,(float)visible.Height),PaddedArea(_workArea));
        var current=WindowBounds(window);
        MoveBounds(window,current,new Rectangle(current.Left+(int)Math.Round(frame.Left-visible.Left),
            current.Top+(int)Math.Round(frame.Top-visible.Top),current.Width,current.Height));
        _anchor=WindowPlacement.SelectAnchor(frame,_workArea);
    }
    public void SavePosition()
    {
        if(_window is null||Volatile.Read(ref _pointerBounds) is not { } visible||_anchor is not { } anchor||_workArea is not { } area)return;
        _positions.Save(new(visible.Left+visible.Width*anchor.PivotX,visible.Top+visible.Height*anchor.PivotY,
            anchor.PivotX,anchor.PivotY,area.Left,area.Top,area.Width,area.Height));
    }

    private static Rectangle WindowBounds(PhotinoWindow window) =>
        new(window.Left, window.Top, window.Width, window.Height);

    private void SetBounds(PhotinoWindow window, Rectangle bounds)
    {
        if (_windowsBehavior is { } behavior)
        {
            behavior.SetBounds(bounds.Left, bounds.Top, bounds.Width, bounds.Height);
        }
        else
        {
            window.SetSize(bounds.Width, bounds.Height);
            window.SetLocation(bounds.Location);
        }
    }

    private void MoveBounds(PhotinoWindow window, Rectangle current, Rectangle next)
    {
        if (current == next)
        {
            return;
        }

        SetBounds(window, next);
        if (Volatile.Read(ref _pointerBounds) is { } bounds)
        {
            Volatile.Write(ref _pointerBounds, bounds with
            {
                Left = bounds.Left + next.Left - current.Left,
                Top = bounds.Top + next.Top - current.Top
            });
        }
    }

    public readonly record struct ThemePointer(double X, double Y);

    /// <summary>Normalized desktop cursor direction, using the same DPI-aware surface bounds as hit testing.</summary>
    public ThemePointer? GetThemePointer()
    {
        if (!UsesNativePointerTracking || _windowsBehavior?.IsVisible != true ||
            Volatile.Read(ref _pointerBounds) is not { } bounds || !GetCursorPos(out var cursor))
            return null;
        return new ThemePointer(
            Math.Round(Math.Clamp((cursor.X - bounds.Left - bounds.Width / 2) / Math.Max(80 * bounds.Scale, bounds.Width / 2), -1, 1), 2),
            Math.Round(Math.Clamp((cursor.Y - bounds.Top - bounds.Height / 2) / Math.Max(80 * bounds.Scale, bounds.Height / 2), -1, 1), 2));
    }

    public bool? IsPointerOverContent(bool wasInside)
    {
        if (_windowsBehavior is { IsVisible: false }) return false;
        if (!UsesNativePointerTracking ||
            Volatile.Read(ref _pointerBounds) is not { } bounds ||
            !GetCursorPos(out var cursor))
        {
            return null;
        }

        if (_windowsBehavior is { } behavior && !behavior.OwnsPointerWindow(cursor))
        {
            return false;
        }

        return Contains(bounds, cursor, wasInside);
    }

    private static bool Contains(PointerBounds bounds, Point cursor, bool wasInside)
    {
        var tolerance = wasInside ? 3 * bounds.Scale : 0;
        var x = cursor.X - bounds.Left + tolerance;
        var y = cursor.Y - bounds.Top + tolerance;
        var width = bounds.Width + tolerance * 2;
        var height = bounds.Height + tolerance * 2;

        if (x < 0 || y < 0 || x > width || y > height)
        {
            return false;
        }

        // Entry follows the visible rounded shape. Once entered, keep the full
        // rectangle active so a changing corner radius cannot revoke hover
        // beneath a stationary cursor while opening from a corner.
        if (wasInside)
        {
            return true;
        }

        var radius = Math.Min(bounds.Radius, Math.Min(width, height) / 2);
        var nearestX = Math.Clamp(x, radius, width - radius);
        var nearestY = Math.Clamp(y, radius, height - radius);
        var dx = x - nearestX;
        var dy = y - nearestY;
        return dx * dx + dy * dy <= radius * radius;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    private sealed record DragOrigin(double OffsetX, double OffsetY);

    private sealed record PointerBounds(
        double Left, double Top, double Width, double Height, double Radius, double Scale);
}
