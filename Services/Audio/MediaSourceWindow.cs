using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

namespace HoldItTogether.Services.Audio;

/// <summary>Restores an existing media app window. Never launches an application or sends global keys.</summary>
internal static class MediaSourceWindow
{
    public static bool TryActivate(string appId, string mediaTitle)
    {
        if (string.IsNullOrWhiteSpace(appId)) return false;
        var processes = new Dictionary<uint, (string Name, string AppId)>();
        var candidates = new List<(nint Window, int Score)>();
        EnumWindows((window, _) =>
        {
            if (GetWindow(window, 4) != 0 || (GetWindowLong(window, -20) & 0x80) != 0) return true;
            if (DwmGetWindowAttribute(window, 14, out var cloaked, sizeof(int)) == 0 && cloaked != 0) return true;
            GetWindowThreadProcessId(window, out var processId);
            if (processId == Environment.ProcessId) return true;
            if (!processes.TryGetValue(processId, out var process))
                processes[processId] = process = ReadProcess(processId);
            var title = new StringBuilder(1024);
            GetWindowText(window, title, title.Capacity);
            var visible = IsWindowVisible(window);
            if (!visible && title.Length == 0) return true;
            var score = MatchScore(appId, ReadWindowAppId(window), process.AppId, process.Name, title.ToString(), mediaTitle);
            // Windows 10 packaged apps may be hosted by ApplicationFrameHost.
            if (score == 0 && process.Name.Equals("ApplicationFrameHost", StringComparison.OrdinalIgnoreCase))
                EnumChildWindows(window, (child, _) =>
                {
                    GetWindowThreadProcessId(child, out var childProcess);
                    if (!processes.TryGetValue(childProcess, out var identity))
                        processes[childProcess] = identity = ReadProcess(childProcess);
                    score = Math.Max(score, MatchScore(appId, "", identity.AppId, identity.Name, title.ToString(), mediaTitle));
                    return true;
                }, 0);
            if (score > 0) candidates.Add((window, score + (visible ? 1 : 0)));
            return true;
        }, 0);
        var foreground = GetForegroundWindow();
        var target = candidates.OrderByDescending(candidate => candidate.Score)
            .ThenByDescending(candidate => candidate.Window == foreground).FirstOrDefault().Window;
        if (target == 0 || !IsWindow(target)) return false;
        if (target == foreground) return true;
        if (IsIconic(target) || !IsWindowVisible(target)) ShowWindowAsync(target, 9); // SW_RESTORE, preserve maximized state otherwise.
        return SetForegroundWindow(target);
    }

    private static int MatchScore(string source, string windowId, string processId, string processName, string windowTitle, string mediaTitle)
    {
        var exact = source.Equals(windowId, StringComparison.OrdinalIgnoreCase) || source.Equals(processId, StringComparison.OrdinalIgnoreCase);
        var expectedProcess = ProcessNameFor(source);
        if (!exact && (expectedProcess.Length == 0 || !expectedProcess.Equals(processName, StringComparison.OrdinalIgnoreCase))) return 0;
        return (exact ? 100 : 10) + (!string.IsNullOrWhiteSpace(mediaTitle) && windowTitle.Contains(mediaTitle, StringComparison.OrdinalIgnoreCase) ? 5 : 0);
    }

    private static string ProcessNameFor(string id)
    {
        // Packaged IDs are matched by identity, never guessed from their display name.
        if (id.Contains('!')) return "";
        var name = (id.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(id) : id).ToLowerInvariant();
        return name switch
        {
            "chrome" or "google.chrome" => "chrome",
            "msedge" or "microsoft.msedge" or "microsoft.edge" => "msedge",
            "firefox" or "org.mozilla.firefox" => "firefox", "brave" => "brave", "opera" => "opera",
            "videolan.vlc" or "vlc" => "vlc",
            "com.squirrel.spotify.spotify" or "spotify" => "Spotify",
            _ => id.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) ? Path.GetFileNameWithoutExtension(id) : ""
        };
    }

    private static (string Name, string AppId) ReadProcess(uint id)
    {
        try
        {
            using var process = Process.GetProcessById((int)id);
            using var handle = OpenProcess(0x1000, false, id); // PROCESS_QUERY_LIMITED_INFORMATION
            var name = process.ProcessName;
            if (handle.IsInvalid) return (name, "");
            uint length = 0;
            if (GetApplicationUserModelId(handle, ref length, null) != 122 || length is 0 or > 4096) return (name, "");
            var value = new StringBuilder((int)length);
            return (name, GetApplicationUserModelId(handle, ref length, value) == 0 ? value.ToString() : "");
        }
        catch (Exception error) when (error is ArgumentException or InvalidOperationException or System.ComponentModel.Win32Exception)
        { return ("", ""); }
    }

    private static string ReadWindowAppId(nint window)
    {
        IPropertyStore? store = null;
        try
        {
            var iid = typeof(IPropertyStore).GUID;
            if (SHGetPropertyStoreForWindow(window, in iid, out store) != 0 || store is null) return "";
            var key = new PropertyKey { Format = new("9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3"), Id = 5 };
            var result = store.GetValue(in key, out var value);
            try { return result == 0 && value.Type == 31 ? Marshal.PtrToStringUni(value.Pointer) ?? "" : ""; }
            finally { PropVariantClear(ref value); }
        }
        catch (COMException) { return ""; }
        finally { if (store is not null) Marshal.ReleaseComObject(store); }
    }

    [StructLayout(LayoutKind.Sequential)] private struct PropertyKey { public Guid Format; public uint Id; }
    [StructLayout(LayoutKind.Explicit, Size = 24)] private struct PropVariant
    { [FieldOffset(0)] public ushort Type; [FieldOffset(8)] public nint Pointer; }
    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(in PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(in PropertyKey key, in PropVariant value);
        [PreserveSig] int Commit();
    }
    private delegate bool WindowCallback(nint window, nint parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumWindows(WindowCallback callback, nint parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool EnumChildWindows(nint parent, WindowCallback callback, nint parameter);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindowVisible(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsWindow(nint window);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool IsIconic(nint window);
    [DllImport("user32.dll")] private static extern nint GetWindow(nint window, uint command);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongW")] private static extern uint GetWindowLong(nint window, int index);
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint processId);
    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetWindowTextW")] private static extern int GetWindowText(nint window, StringBuilder text, int length);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool ShowWindowAsync(nint window, int command);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetForegroundWindow(nint window);
    [DllImport("dwmapi.dll")] private static extern int DwmGetWindowAttribute(nint window, uint attribute, out int value, int size);
    [DllImport("kernel32.dll")] private static extern SafeProcessHandle OpenProcess(uint access, [MarshalAs(UnmanagedType.Bool)] bool inherit, uint id);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int GetApplicationUserModelId(SafeProcessHandle process, ref uint length, StringBuilder? value);
    [DllImport("shell32.dll")] private static extern int SHGetPropertyStoreForWindow(nint window, in Guid iid, [MarshalAs(UnmanagedType.Interface)] out IPropertyStore? store);
    [DllImport("ole32.dll")] private static extern int PropVariantClear(ref PropVariant value);
}
