using System.ComponentModel;
using System.Runtime.InteropServices;
using HoldItTogether.Services;
using HoldItTogether.Services.Windows;
using System.Drawing;

internal static class WindowTopmostChecks
{
    public static void Run()
    {
        if (!OperatingSystem.IsWindows()) return;
        var foreground = GetForegroundWindow();
        var window = CreateWindowEx(0, "STATIC", "HIT topmost regression", 0x80000000,
            -10000, -10000, 100, 100, 0, 0, 0, 0);
        if (window == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
        WindowsWindowBehavior? behavior = null;
        try
        {
            behavior = new WindowsWindowBehavior(window, true);
            Check(IsTopmost(window), "Initialization must apply ordinary topmost.");
            Demote(window);
            Check(!IsTopmost(window), "The test must reproduce actual native topmost loss.");
            Pump(window);
            Check(IsTopmost(window), "Ordinary mode must recover native topmost loss without FORCE.");
            Check(behavior.TopmostRecoveries is [{ Error: 0 }], "Recovery must retain bounded native diagnostics.");
            for (var index = 0; index < 50; index++)
            {
                behavior.SetBounds(-10000 + index, -10000, 100 + index, 100);
                Pump(window);
                Check(IsTopmost(window), "Move/resize must preserve ordinary topmost.");
            }
            Check(behavior.TopmostRecoveries.Length == 1, "Healthy move/resize must not cause recovery writes.");
            for (var index = 0; index < 40; index++)
            {
                Demote(window);
                Pump(window);
                Check(IsTopmost(window), "Repeated native losses must recover.");
            }
            Check(behavior.TopmostRecoveries.Length == 32 && behavior.TopmostRecoveries.All(entry =>
                entry.Error == 0 && (entry.BeforeStyle & 8) == 0 && (entry.AfterStyle & 8) != 0),
                "Diagnostics must retain only the last 32 recoveries and actual before/after styles.");
            Demote(window);
            behavior.ApplySettings(false, false);
            Pump(window);
            Check(!IsTopmost(window), "OFF must cancel a previously queued recovery.");
            behavior.ApplySettings(true, false);
            Check(IsTopmost(window), "Re-enabling must apply topmost.");
            Demote(window);
            behavior.ApplySettings(true, false);
            Check(IsTopmost(window), "Unchanged preferences must reconcile actual native state.");
            Pump(window);
            behavior.ApplySettings(false, false);
            behavior.SetBounds(-10000, -10000, 110, 110);
            Pump(window);
            Check(!IsTopmost(window), "Movement must respect OFF.");
            behavior.ApplySettings(true, false, true);
            behavior.ApplySettings(false, false);
            SendMessage(window, 0x0113, 0x484954, 0);
            Pump(window);
            Check(!IsTopmost(window), "Disabling must also stop FORCE recovery.");
            behavior.ApplySettings(true, false, true);
            behavior.SetFullscreenContext(new(window, new Rectangle(0, 0, 1920, 1080), false));
            Check(SendMessage(window, 0x0021, 0, 0) == 3 && (GetWindowLong(window, -20) & 0x08000000) != 0,
                "Fullscreen mouse input must use MA_NOACTIVATE and WS_EX_NOACTIVATE.");
            Demote(window);
            behavior.SetFullscreenContext(new(window, new Rectangle(0, 0, 1920, 1080), true));
            SendMessage(window, 0x0113, 0x484954, 0);
            Pump(window);
            Check(!IsTopmost(window), "Neither queued normal recovery nor FORCE may fight exclusive fullscreen.");
            behavior.ApplySettings(true, false, true);
            Check(!IsTopmost(window), "Unchanged settings must also respect exclusive fullscreen.");
            behavior.SetFullscreenContext(null);
            Pump(window);
            Check(IsTopmost(window) && SendMessage(window, 0x0021, 0, 0) == 1 &&
                (GetWindowLong(window, -20) & 0x08000000) == 0,
                "Leaving fullscreen must restore native recovery and normal mouse activation.");
            behavior.ApplySettings(false, false);
            Check(!IsWindowVisible(window), "Native tests must keep the window hidden.");
            Check(GetForegroundWindow() == foreground, "Native recovery must not steal foreground focus.");
            behavior.ApplySettings(true, false);
            Demote(window); // Destroy with a pending recovery message.
        }
        finally
        {
            DestroyWindow(window);
            Pump(window);
            GC.KeepAlive(behavior);
        }
        Console.WriteLine("PASS: native topmost recovery, fullscreen non-activation, exclusive FORCE suspension, OFF, bounds, focus and teardown.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
    private static bool IsTopmost(nint window) => (GetWindowLong(window, -20) & 8) != 0;
    private static void Demote(nint window)
    {
        if (!SetWindowPos(window, new nint(-2), 0, 0, 0, 0, 0x13))
            throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    private static void Pump(nint window)
    {
        var count = 0;
        while (PeekMessage(out var message, window, 0, 0, 1))
        {
            if (++count > 100) throw new Exception("Native recovery must not create a message loop.");
            DispatchMessage(ref message);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Message
    {
        public nint Window;
        public uint Id;
        public nuint Parameter;
        public nint Data;
        public uint Time;
        public int X, Y;
        public uint Private;
    }
    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern nint CreateWindowEx(uint ex, string className, string title, uint style, int x, int y,
        int width, int height, nint parent, nint menu, nint instance, nint param);
    [DllImport("user32.dll")] private static extern bool DestroyWindow(nint window);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern int GetWindowLong(nint window, int index);
    [DllImport("user32.dll", SetLastError = true)] private static extern bool SetWindowPos(nint window, nint after,
        int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll")] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll", EntryPoint = "PeekMessageW")] private static extern bool PeekMessage(out Message message,
        nint window, uint minimum, uint maximum, uint remove);
    [DllImport("user32.dll", EntryPoint = "DispatchMessageW")] private static extern nint DispatchMessage(ref Message message);
    [DllImport("user32.dll", EntryPoint = "SendMessageW")] private static extern nint SendMessage(nint window, uint message, nuint parameter, nint data);
}
