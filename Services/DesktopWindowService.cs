using HoldItTogether.Services.Settings;
using System.Drawing;
using System.Runtime.InteropServices;
using Photino.NET;

namespace HoldItTogether.Services;

public sealed class DesktopWindowService
{
    private readonly HitSettingsService? _settings;
    public DesktopWindowService(HitSettingsService? settings = null) => _settings = settings;

    private PhotinoWindow? _window;
    private WindowsWindowBehavior? _windowsBehavior;
    private WindowAnchor? _anchor;
    private Rectangle? _workArea;
    private PointerBounds? _pointerBounds;
    private DragOrigin? _dragOrigin;

    public readonly record struct WindowOrigin(double X, double Y);

    public bool UsesNativePointerTracking => OperatingSystem.IsWindows();

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
                if (_windowsBehavior is { } behavior)
                    behavior.ApplySettings(value.AlwaysOnTop, value.SystemTray);
                else
                    window.SetTopMost(value.AlwaysOnTop);
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
                SelectAnchor(window);
            }

            var anchor = _anchor!.Value;
            if (OperatingSystem.IsWindows())
            {
                _workArea = WindowsWindowBehavior.GetWorkArea(
                    WindowPlacement.MonitorReference(anchor, _workArea)) ?? _workArea;
            }

            var placement = WindowPlacement.Calculate(anchor, nextWidth, nextHeight, _workArea);
            if (WindowBounds(window) != placement)
            {
                SetBounds(window, placement);
            }

            // A single snapshot avoids false hover exits during native moves.
            Volatile.Write(ref _pointerBounds, new PointerBounds(
                placement.Left + contentLeft * scale,
                placement.Top + contentTop * scale,
                contentWidth * scale,
                contentHeight * scale,
                Math.Max(0, borderRadius * scale),
                scale));

            origin = new WindowOrigin(anchor.PivotX, anchor.PivotY);
        });

        return origin;
    }

    public bool TryBeginDrag(double offsetX, double offsetFromBottom, double scale)
    {
        if (_window is not { } window || _windowsBehavior is null ||
            !double.IsFinite(offsetX) || !double.IsFinite(offsetFromBottom) ||
            !double.IsFinite(scale) || scale <= 0)
        {
            return false;
        }

        window.Invoke(() =>
        {
            if (_anchor is null)
            {
                SelectAnchor(window);
            }

            _dragOrigin = new DragOrigin(offsetX * scale, offsetFromBottom * scale);
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
            if (_dragOrigin is not { } drag || !GetCursorPos(out var cursor))
            {
                return;
            }

            var current = WindowBounds(window);
            var centerX = cursor.X - drag.OffsetX;
            var bottom = (int)Math.Round(cursor.Y + drag.OffsetFromBottom);
            _workArea = WindowsWindowBehavior.GetWorkArea(cursor) ?? _workArea;

            var placement = WindowPlacement.Constrain(new Rectangle(
                (int)Math.Round(centerX - current.Width / 2d),
                bottom - current.Height, current.Width, current.Height), _workArea);

            var anchor = _anchor!.Value;
            _anchor = WindowPlacement.AnchorAt(placement, anchor.PivotX, anchor.PivotY);
            MoveBounds(window, current, placement);
        });
    }

    public void EndDrag()
    {
        if (_window is not { } window || _dragOrigin is null)
        {
            return;
        }

        window.Invoke(() =>
        {
            _dragOrigin = null;
            // Choose once after release, without moving the current window.
            // This corner stays fixed through both expansion and collapse.
            SelectAnchor(window);
        });
    }

    // Disposal may run after the native window has already been destroyed.
    public void CancelDrag() => _dragOrigin = null;

    private void SelectAnchor(PhotinoWindow window)
    {
        var bounds = WindowBounds(window);
        if (OperatingSystem.IsWindows())
        {
            _workArea = WindowsWindowBehavior.GetWorkArea(new Point(
                bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2)) ?? _workArea;
        }

        _anchor = WindowPlacement.SelectAnchor(bounds, _workArea);
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

    private sealed record DragOrigin(double OffsetX, double OffsetFromBottom);

    private sealed record PointerBounds(
        double Left, double Top, double Width, double Height, double Radius, double Scale);
}