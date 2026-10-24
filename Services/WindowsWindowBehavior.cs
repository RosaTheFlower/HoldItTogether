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
    private const uint NoSize = 0x0001;
    private const uint NoMove = 0x0002;
    private const uint NoZOrder = 0x0004;
    private const uint DoNotActivate = 0x0010;
    private const uint FrameChanged = 0x0020;
    private const nuint SubclassId = 1;

    private readonly nint _handle;
    private readonly SubclassCallback _callback;
    private readonly WindowsTrayIcon? _tray;
    private bool _alwaysOnTop;

    public WindowsWindowBehavior(nint handle, bool alwaysOnTop = true)
    {
        _handle = handle;
        _alwaysOnTop = alwaysOnTop;
        _tray = new WindowsTrayIcon(handle);
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
            throw;
        }
    }
    internal static int GetInteractiveStyle(int style) =>
        (style & ~(Layered | TransparentInput | NoActivate | AppWindow)) | ToolWindow;

    public bool IsVisible => IsWindowVisible(_handle);

    public bool OwnsPointerWindow(Point point)
    {
        var hit = WindowFromPoint(point);
        return hit != 0 && GetAncestor(hit, 2) == _handle; // GA_ROOT
    }

    public void ApplySettings(bool alwaysOnTop, bool systemTray)
    {
        _tray?.SetEnabled(systemTray);
        if (_alwaysOnTop == alwaysOnTop) return;

        if (!SetWindowPos(_handle, new nint(alwaysOnTop ? -1 : -2), 0, 0, 0, 0,
            NoMove | NoSize | DoNotActivate))
        {
            throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        _alwaysOnTop = alwaysOnTop;
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

    public static Rectangle? GetWorkArea(Point reference)
    {
        var monitor = MonitorFromPoint(reference, 2); // MONITOR_DEFAULTTONEAREST
        var info = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };

        if (monitor == 0 || !GetMonitorInfo(monitor, ref info))
        {
            return null;
        }

        return Rectangle.FromLTRB(
            info.Work.Left, info.Work.Top, info.Work.Right, info.Work.Bottom);
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

            case 0x0021: // WM_MOUSEACTIVATE: activate and deliver the very first click.
                return 1; // MA_ACTIVATE

            case 0x031E: // WM_DWMCOMPOSITIONCHANGED
                var margins = new Margins { Left = -1, Right = -1, Top = -1, Bottom = -1 };
                DwmExtendFrameIntoClientArea(handle, ref margins);
                break;

            case 0x0082: // WM_NCDESTROY: detach while this managed delegate is still alive.
                _tray?.Dispose();
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

    [DllImport("user32.dll")]
    private static extern nint GetAncestor(nint window, uint flags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint handle, int command);

    [DllImport("gdi32.dll")]
    private static extern nint GetStockObject(int index);
}