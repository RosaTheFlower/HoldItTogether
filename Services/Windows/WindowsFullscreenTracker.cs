using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace HoldItTogether.Services.Windows;

internal sealed record FullscreenContext(nint Window, Rectangle Monitor, bool Exclusive);

/// <summary>Observes desktop events without activating or modifying the foreground application.</summary>
internal sealed class WindowsFullscreenTracker : IDisposable
{
    private readonly nint _host;
    private readonly Action<FullscreenContext?> _changed;
    private readonly EventCallback _callback;
    private readonly List<nint> _hooks = new();
    private FullscreenContext? _current;
    private bool _disposed;

    public WindowsFullscreenTracker(nint host, Action<FullscreenContext?> changed)
    {
        _host = host;
        _changed = changed;
        _callback = OnEvent;
        foreach (var range in new (uint First, uint Last)[] { (3, 3), (0x16, 0x17), (0x800B, 0x800B) })
        {
            var hook = SetWinEventHook(range.First, range.Last, 0, _callback, 0, 0, 0);
            if (hook != 0) _hooks.Add(hook);
        }
        Refresh();
    }

    private void OnEvent(nint hook, uint kind, nint window, int objectId, int childId, uint thread, uint time)
    {
        if (_disposed || window == 0) return;
        if (kind == 0x800B && (objectId != 0 || childId != 0 ||
            (window != GetForegroundWindow() && window != _current?.Window))) return;
        Refresh();
    }

    public void Refresh()
    {
        if (_disposed) return;
        var foreground = GetAncestor(GetForegroundWindow(), 2);
        // Explicitly focusing HIT must not make the game-monitor placement oscillate.
        var candidate = foreground == _host ? _current?.Window ?? 0 : foreground;
        var next = Read(candidate, _host);
        if (next == _current) return;
        _current = next;
        _changed(next);
    }

    internal static FullscreenContext? Read(nint window, nint host)
    {
        if (window == 0 || window == host || window == GetShellWindow() ||
            !IsWindowVisible(window) || IsIconic(window)) return null;
        var name = new StringBuilder(128);
        GetClassName(window, name, name.Capacity);
        if (name.ToString() is "Progman" or "WorkerW" or "Shell_TrayWnd" or "Shell_SecondaryTrayWnd") return null;
        var monitor = new MonitorInfo { Size = (uint)Marshal.SizeOf<MonitorInfo>() };
        if (!GetMonitorInfo(MonitorFromWindow(window, 2), ref monitor) || !GetWindowRect(window, out var bounds)) return null;
        if (DwmGetWindowAttribute(window, 9, out var frame, Marshal.SizeOf<Rect>()) == 0) bounds = frame;
        var area = Rectangle.FromLTRB(monitor.Monitor.Left, monitor.Monitor.Top, monitor.Monitor.Right, monitor.Monitor.Bottom);
        if (!CoversMonitor(Rectangle.FromLTRB(bounds.Left, bounds.Top, bounds.Right, bounds.Bottom),
            area, GetWindowLong(window, -16))) return null;
        var exclusive = SHQueryUserNotificationState(out var state) == 0 && state == 3;
        return new(window, area, exclusive);
    }

    internal static bool CoversMonitor(Rectangle frame, Rectangle monitor, int style) =>
        monitor.Width > 0 && monitor.Height > 0 && (style & 0x00C00000) == 0 &&
        Math.Abs((long)frame.Left - monitor.Left) <= 2 && Math.Abs((long)frame.Top - monitor.Top) <= 2 &&
        Math.Abs((long)frame.Right - monitor.Right) <= 2 && Math.Abs((long)frame.Bottom - monitor.Bottom) <= 2;

    public void Dispose()
    {
        _disposed = true;
        foreach (var hook in _hooks) UnhookWinEvent(hook);
        _hooks.Clear();
    }

    [StructLayout(LayoutKind.Sequential)] private struct Rect { public int Left, Top, Right, Bottom; }
    [StructLayout(LayoutKind.Sequential)] private struct MonitorInfo { public uint Size; public Rect Monitor, Work; public uint Flags; }
    private delegate void EventCallback(nint hook, uint kind, nint window, int objectId, int childId, uint thread, uint time);
    [DllImport("user32.dll")] private static extern nint SetWinEventHook(uint first, uint last, nint module, EventCallback callback, uint process, uint thread, uint flags);
    [DllImport("user32.dll")] private static extern bool UnhookWinEvent(nint hook);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern nint GetAncestor(nint window, uint flags);
    [DllImport("user32.dll")] private static extern nint GetShellWindow();
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern nint MonitorFromWindow(nint window, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetMonitorInfoW")] private static extern bool GetMonitorInfo(nint monitor, ref MonitorInfo info);
    [DllImport("user32.dll")] private static extern bool GetWindowRect(nint window, out Rect bounds);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)] private static extern int GetClassName(nint window, StringBuilder name, int size);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, uint attribute, out Rect bounds, int size);
    [DllImport("shell32.dll")] private static extern int SHQueryUserNotificationState(out int state);
}
