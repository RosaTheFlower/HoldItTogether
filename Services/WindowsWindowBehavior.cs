using HoldItTogether.Services.Windows;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;

namespace HoldItTogether.Services;

/// <summary>
/// Keeps the WebView transparent while retaining normal Windows mouse input.
/// This instance and its subclass delegate live as long as DesktopWindowService.
/// </summary>
internal sealed class WindowsWindowBehavior
{
    private const int ExtendedStyle = -20;
    private const int Layered = 0x00080000;
    private const int TransparentInput = 0x00000020;
    private const int NoActivate = 0x08000000;
    private const int AppWindow = 0x00040000;
    private const int ToolWindow = 0x00000080;
    private const int Topmost = 0x00000008;
    private const uint NoSize = 0x0001;
    private const uint NoMove = 0x0002;
    private const uint NoZOrder = 0x0004;
    private const uint DoNotActivate = 0x0010;
    private const uint FrameChanged = 0x0020;
    private const nuint SubclassId = 1;
    private const nuint ForceTopmostTimer = 0x484954;
    private const uint RecoverTopmostMessage = 0x8052;
    private const nuint PlacementAnimationTimer = 0x484955;
    private WindowsFullscreenTracker? _fullscreenTracker;
    private FullscreenContext? _fullscreen;
    private Action<FullscreenContext?>? _fullscreenChanged;
    private Action? _placementAnimation;

    private readonly nint _handle;
    private readonly SubclassCallback _callback;
    private readonly WindowsTrayIcon? _tray;
    private readonly WindowsApplicationIcon _icon;
    private bool _alwaysOnTop;
    private bool _forceAlwaysOnTop;
    private bool _topmostRecoveryPending, _applyingTopmost, _destroyed;
    private readonly Queue<TopmostRecovery> _topmostRecoveries = new();
    internal readonly record struct TopmostRecovery(DateTimeOffset At, uint Trigger, int BeforeStyle,
        int AfterStyle, int Error);
    internal TopmostRecovery[] TopmostRecoveries => _topmostRecoveries.ToArray();
    private (int Left,int Top,int Right,int Bottom)? _contentRegion;

    public WindowsWindowBehavior(nint handle, bool alwaysOnTop = true,Action? restart=null)
    {
        _handle = handle;
        _alwaysOnTop = alwaysOnTop;
        _tray = new WindowsTrayIcon(handle,restart);
        _icon=new WindowsApplicationIcon(handle);
        _icon.LoadDefault();_tray.SetIcon(_icon.Small);
        _callback = WindowProcedure;

        if (!SetWindowSubclass(handle, _callback, SubclassId, 0))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }

        var wasVisible = IsWindowVisible(handle);
        if (wasVisible) ShowWindow(handle, 0); // refresh Shell taskbar registration

        try
        {
            // A layered host can pass clicks through even when its child WebView
            // draws visible content. DWM glass keeps a normal native input surface.
            var style = GetWindowLong(handle, ExtendedStyle);
            Marshal.SetLastPInvokeError(0);
            var previousStyle = SetWindowLong(handle, ExtendedStyle, GetInteractiveStyle(style));
            if (previousStyle == 0 && Marshal.GetLastPInvokeError() is var error && error != 0)
            {
                throw new Win32Exception(error);
            }

            ExtendGlass(handle);

            if (!SetWindowPos(handle, new nint(alwaysOnTop ? -1 : -2), 0, 0, 0, 0,
                NoMove | NoSize | DoNotActivate | FrameChanged))
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            InvalidateRect(handle, 0, true);
            if (wasVisible) ShowWindow(handle, 4); // SW_SHOWNOACTIVATE, still a tool window
        }
        catch
        {
            // Never leave a native callback pointing at a failed, unrooted instance.
            RemoveWindowSubclass(handle, _callback, SubclassId);
            _tray.Dispose();_icon.Dispose();
            throw;
        }
    }
    internal static int GetInteractiveStyle(int style) =>
        (style & ~(Layered | TransparentInput | NoActivate | AppWindow)) | ToolWindow;

    public bool IsVisible => IsWindowVisible(_handle);
    public void TrackFullscreen(Action<FullscreenContext?> changed)
    {
        _fullscreenChanged = changed;
        _fullscreenTracker = new WindowsFullscreenTracker(_handle, SetFullscreenContext);
    }

    internal void SetFullscreenContext(FullscreenContext? context)
    {
        if (_destroyed || context == _fullscreen) return;
        _fullscreen = context;
        var style = GetWindowLong(_handle, ExtendedStyle);
        SetWindowLong(_handle, ExtendedStyle, context is null ? style & ~NoActivate : style | NoActivate);
        _fullscreenChanged?.Invoke(context);
        // Do not fight an exclusive swap chain. Borderless uses the normal compositor policy.
        if (context is { Exclusive: false } && _alwaysOnTop) ApplyNativeTopmost(true);
        else if (context is null) QueueTopmostRecovery(0x8053);
    }

    public void SetPlacementAnimation(Action? tick)
    {
        _placementAnimation = tick;
        if (tick is null) KillTimer(_handle, PlacementAnimationTimer);
        else if (SetTimer(_handle, PlacementAnimationTimer, 16, 0) == 0)
        {
            _placementAnimation = null;
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }
    public void SetIcon(byte[] png){_icon.SetPng(png);_tray?.SetIcon(_icon.Small);}

    public bool OwnsPointerWindow(Point point)
    {
        var hit = WindowFromPoint(point);
        return hit != 0 && GetAncestor(hit, 2) == _handle; // GA_ROOT
    }

    public void ApplySettings(bool alwaysOnTop, bool systemTray, bool forceAlwaysOnTop = false)
    {
        _fullscreenTracker?.Refresh();
        // Reapplying unrelated preferences must not restore a hidden window.
        if (_tray is { } tray && tray.Enabled != systemTray) tray.SetEnabled(systemTray);
        var previous = _alwaysOnTop;
        _alwaysOnTop = alwaysOnTop;
        if (IsNativeTopmost != alwaysOnTop && (!alwaysOnTop || _fullscreen?.Exclusive != true) && !ApplyNativeTopmost(alwaysOnTop))
        {
            _alwaysOnTop = previous;
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        var force = alwaysOnTop && forceAlwaysOnTop;
        if (force == _forceAlwaysOnTop) return;
        if (force)
        {
            if (SetTimer(_handle, ForceTopmostTimer, 500, 0) == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        else KillTimer(_handle, ForceTopmostTimer);
        _forceAlwaysOnTop = force;
    }

    private bool IsNativeTopmost => (GetWindowLong(_handle, ExtendedStyle) & Topmost) != 0;

    private bool ApplyNativeTopmost(bool enabled)
    {
        _applyingTopmost = true;
        try
        {
            return SetWindowPos(_handle, new nint(enabled ? -1 : -2), 0, 0, 0, 0,
                NoMove | NoSize | DoNotActivate);
        }
        finally { _applyingTopmost = false; }
    }

    private void QueueTopmostRecovery(uint trigger)
    {
        if (_destroyed || !_alwaysOnTop || _fullscreen?.Exclusive == true || _applyingTopmost || _topmostRecoveryPending || IsNativeTopmost) return;
        // Leave the current native transition before changing Z-order. One queued check coalesces events.
        _topmostRecoveryPending = true;
        if (!PostMessage(_handle, RecoverTopmostMessage, trigger, 0))
        {
            var error = Marshal.GetLastWin32Error();
            _topmostRecoveryPending = false;
            RecordTopmostRecovery(trigger, GetWindowLong(_handle, ExtendedStyle), error);
        }
    }

    private void RecordTopmostRecovery(uint trigger, int beforeStyle, int error)
    {
        if (_topmostRecoveries.Count == 32) _topmostRecoveries.Dequeue();
        _topmostRecoveries.Enqueue(new(DateTimeOffset.UtcNow, trigger, beforeStyle,
            GetWindowLong(_handle, ExtendedStyle), error));
    }

    public void SetBounds(int left, int top, int width, int height)
    {
        // One update avoids the transient positions produced by separate
        // SetSize/SetLocation calls. Hover animations must not steal focus.
        if (!SetWindowPos(_handle, 0, left, top, width, height, NoZOrder | DoNotActivate))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
    }

    public void SetContentRegion(double left, double top, double width, double height, double radius)
    {
        var bounds = ((int)Math.Floor(left),(int)Math.Floor(top),(int)Math.Ceiling(left+width),
            (int)Math.Ceiling(top+height));
        if (_contentRegion == bounds) return;
        // Keep Chromium's antialiased corners intact. A binary GDI round mask clipped their edge pixels.
        var region = CreateRectRgn(bounds.Item1,bounds.Item2,bounds.Item3,bounds.Item4);
        if (region == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        // Windows owns a successfully applied region. Transparent canvas/padding never captures input.
        if (SetWindowRgn(_handle,region,false) == 0)
        {
            var error=Marshal.GetLastWin32Error();
            DeleteObject(region);
            throw new Win32Exception(error);
        }
        _contentRegion = bounds;
    }

    public static Rectangle? GetWorkArea(Point reference)
    {
        var monitor = MonitorFromPoint(reference, 2); // MONITOR_DEFAULTTONEAREST
        return ReadWorkArea(monitor);
    }
    public static Rectangle? GetPrimaryWorkArea()=>ReadWorkArea(MonitorFromWindow(0,1));
    public static Rectangle? GetMonitorBounds(Point reference)=>ReadWorkArea(MonitorFromPoint(reference,2),true);
    public static Rectangle? GetPrimaryMonitorBounds()=>ReadWorkArea(MonitorFromWindow(0,1),true);
    public static Rectangle[] GetWorkAreas()
    {
        var areas=new List<Rectangle>();
        MonitorCallback callback=(nint monitor,nint context,ref Rect bounds,nint state)=>{
            if(ReadWorkArea(monitor) is { } area)areas.Add(area);return true;
        };
        EnumDisplayMonitors(0,0,callback,0);GC.KeepAlive(callback);return areas.ToArray();
    }
    private static Rectangle? ReadWorkArea(nint monitor, bool full = false)
    {
        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };

        if (monitor == 0 || !GetMonitorInfo(monitor, ref info))
        {
            return null;
        }

        var bounds = full ? info.Monitor : info.Work;
        return Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
    }

    private static void ExtendGlass(nint handle)
    {
        var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
        Marshal.ThrowExceptionForHR(DwmExtendFrameIntoClientArea(handle, ref margins));
    }

    private nint WindowProcedure(nint handle, uint message, nuint wParam, nint lParam,
        nuint subclassId, nuint referenceData)
    {
        if (_tray?.HandleMessage(message, wParam, lParam) == true) return 0;

        switch (message)
        {
            case RecoverTopmostMessage:
                _topmostRecoveryPending = false;
                _fullscreenTracker?.Refresh();
                if (!_destroyed && _alwaysOnTop && _fullscreen?.Exclusive != true && !IsNativeTopmost)
                {
                    var before = GetWindowLong(handle, ExtendedStyle);
                    var success = ApplyNativeTopmost(true);
                    var error = success ? 0 : Marshal.GetLastWin32Error();
                    RecordTopmostRecovery((uint)wParam, before, error);
                }
                return 0;
            case 0x0047: // WM_WINDOWPOSCHANGED: includes native Z-order changes.
            case 0x0018: // WM_SHOWWINDOW
            case 0x0006: // WM_ACTIVATE
            case 0x001C: // WM_ACTIVATEAPP
            case 0x007E: // WM_DISPLAYCHANGE
            case 0x0218: // WM_POWERBROADCAST
                var result = DefSubclassProc(handle, message, wParam, lParam);
                if (message is 0x007E or 0x0218) _fullscreenTracker?.Refresh();
                QueueTopmostRecovery(message);
                return result;
            case 0x0113 when wParam == PlacementAnimationTimer:
                _placementAnimation?.Invoke();
                return 0;
            case 0x0113 when wParam == ForceTopmostTimer: // WM_TIMER, on the native UI thread.
                _fullscreenTracker?.Refresh();
                if (_forceAlwaysOnTop && _alwaysOnTop && _fullscreen is null && IsVisible && GetAncestor(GetForegroundWindow(), 2) != _handle)
                    ApplyNativeTopmost(true);
                return 0;
            case 0x0014: // WM_ERASEBKGND: zero-alpha black is transparent on the DWM frame.
                if (GetClientRect(handle, out var bounds))
                {
                    FillRect((nint)wParam, ref bounds, GetStockObject(4)); // BLACK_BRUSH
                }
                return 1;

            case 0x000F: // WM_PAINT: avoid Photino's opaque light/dark theme background.
                var context = BeginPaint(handle, out var paint);
                if (context != 0)
                {
                    FillRect(context, ref paint.Paint, GetStockObject(4));
                    EndPaint(handle, ref paint);
                }
                return 0;

            case 0x0084: // WM_NCHITTEST: the borderless host is a normal client input area.
                return 1; // HTCLIENT

            case 0x0021: // Deliver mouse input without taking the game out of fullscreen.
                _fullscreenTracker?.Refresh();
                return _fullscreen is null ? 1 : 3; // MA_ACTIVATE / MA_NOACTIVATE

            case 0x031E: // WM_DWMCOMPOSITIONCHANGED
                var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(handle, ref margins);
                break;

            case 0x0082: // WM_NCDESTROY: detach while this managed delegate is still alive.
                _destroyed = true;
                _fullscreenTracker?.Dispose();
                _fullscreenChanged = null;
                _placementAnimation = null;
                KillTimer(handle, PlacementAnimationTimer);
                _topmostRecoveryPending = false;
                KillTimer(handle, ForceTopmostTimer);
                _forceAlwaysOnTop = false;
                _tray?.Dispose();
                _icon.Dispose();
                RemoveWindowSubclass(handle, _callback, SubclassId);
                break;
        }

        return DefSubclassProc(handle, message, wParam, lParam);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Margins
    {
        public int Left, Right, Top, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Rect
    {
        public int Left, Top, Right, Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MonitorInfo
    {
        public uint Size;
        public Rect Monitor;
        public Rect Work;
        public uint Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PaintStruct
    {
        public nint Context;
        [MarshalAs(UnmanagedType.Bool)] public bool Erase;
        public Rect Paint;
        [MarshalAs(UnmanagedType.Bool)] public bool Restore;
        [MarshalAs(UnmanagedType.Bool)] public bool Update;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)] public byte[] Reserved;
    }

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate nint SubclassCallback(
        nint handle, uint message, nuint wParam, nint lParam, nuint subclassId, nuint referenceData);
    private delegate bool MonitorCallback(nint monitor,nint context,ref Rect bounds,nint state);
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool EnumDisplayMonitors(nint context,nint clip,MonitorCallback callback,nint state);
    [DllImport("user32.dll")]
    private static extern nint MonitorFromWindow(nint window,uint flags);

    [DllImport("comctl32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowSubclass(
        nint handle, SubclassCallback callback, nuint subclassId, nuint referenceData);

    [DllImport("comctl32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool RemoveWindowSubclass(nint handle, SubclassCallback callback, nuint subclassId);

    [DllImport("comctl32.dll")]
    private static extern nint DefSubclassProc(nint handle, uint message, nuint wParam, nint lParam);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
    private static extern int GetWindowLong(nint handle, int index);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
    private static extern int SetWindowLong(nint handle, int index, int value);

    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(
        nint handle, nint insertAfter, int x, int y, int width, int height, uint flags);

    [DllImport("user32.dll", EntryPoint = "PostMessageW", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint handle, uint message, nuint wParam, nint lParam);

    [DllImport("dwmapi.dll")]
    private static extern int DwmExtendFrameIntoClientArea(nint handle, ref Margins margins);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool InvalidateRect(nint handle, nint rect, [MarshalAs(UnmanagedType.Bool)] bool erase);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetClientRect(nint handle, out Rect rect);

    [DllImport("user32.dll")]
    private static extern nint BeginPaint(nint handle, out PaintStruct paint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool EndPaint(nint handle, ref PaintStruct paint);

    [DllImport("user32.dll")]
    private static extern int FillRect(nint context, ref Rect rect, nint brush);

    [DllImport("user32.dll")]
    private static extern nint MonitorFromPoint(Point point, uint flags);

    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint handle);

    [DllImport("user32.dll")]
    private static extern nint WindowFromPoint(Point point);
    [DllImport("user32.dll", SetLastError = true)]
    private static extern nuint SetTimer(nint window, nuint id, uint milliseconds, nint callback);
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool KillTimer(nint window, nuint id);
    [DllImport("user32.dll")]
    private static extern nint GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint handle, int command);

    [DllImport("gdi32.dll")]
    private static extern nint GetStockObject(int index);
    [DllImport("gdi32.dll",SetLastError=true)]
    private static extern nint CreateRectRgn(int left,int top,int right,int bottom);
    [DllImport("user32.dll",SetLastError=true)]
    private static extern int SetWindowRgn(nint window,nint region,[MarshalAs(UnmanagedType.Bool)] bool redraw);
    [DllImport("gdi32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]
    private static extern bool DeleteObject(nint item);
}
