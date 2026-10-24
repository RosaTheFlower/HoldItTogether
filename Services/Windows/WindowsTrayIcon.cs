using System.Drawing;
using System.Runtime.InteropServices;

namespace HoldItTogether.Services.Windows;

internal sealed class WindowsTrayIcon(nint window) : IDisposable
{
    private const uint CallbackMessage = 0x8031;
    private readonly uint _taskbarCreated = RegisterWindowMessage("TaskbarCreated");
    private bool _enabled;
    private bool _added;
    private bool _version4;

    public bool Enabled => _enabled;

    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            _enabled = true;
            if (!_added && !Add())
            {
                throw new InvalidOperationException("Não foi possível criar o ícone na bandeja do Windows.");
            }
        }
        else
        {
            if (_added)
            {
                var data = Data();
                ShellNotifyIcon(2, ref data); // NIM_DELETE
                _added = false;
            }
            _enabled = false;
            if (!IsWindowVisible(window))
            {
                Show();
            }
        }
    }

    public bool HandleMessage(uint message, nuint wParam, nint lParam)
    {
        if (_taskbarCreated != 0 && message == _taskbarCreated)
        {
            _added = false;
            if (_enabled) Add();
            return true;
        }

        if (message != CallbackMessage)
        {
            return false;
        }

        var notification = (uint)(lParam.ToInt64() & 0xffff);
        if (notification is 0x0202 or 0x0203 or 0x0400 or 0x0401) // click / select
        {
            Show();
        }
        else if (notification is 0x0205 or 0x007B) // right-click / context menu
        {
            var point = new Point(
                (short)(wParam & 0xffff), (short)((wParam >> 16) & 0xffff));
            if (!_version4 || (point.X == -1 && point.Y == -1))
            {
                GetCursorPos(out point);
            }
            ShowMenu(point);
        }

        return true;
    }

    private bool Add()
    {
        var data = Data();
        if (data.Icon == 0 || !ShellNotifyIcon(0, ref data)) // NIM_ADD
        {
            return false;
        }

        _added = true;
        data.Version = 4; // NOTIFYICON_VERSION_4
        _version4 = ShellNotifyIcon(4, ref data);
        return true;
    }

    private NotifyIconData Data() => new()
    {
        Size = (uint)Marshal.SizeOf<NotifyIconData>(),
        Window = window,
        Id = 1,
        Flags = 1 | 2 | 4 | 0x80, // MESSAGE | ICON | TIP | SHOWTIP
        Callback = CallbackMessage,
        Icon = LoadIcon(0, new nint(32512)), // shared IDI_APPLICATION
        Tip = "HIT · Hold It Together",
        Info = "",
        InfoTitle = ""
    };

    private void Show()
    {
        ShowWindow(window, 9); // SW_RESTORE
        SetForegroundWindow(window);
    }

    private void ShowMenu(Point location)
    {
        var menu = CreatePopupMenu();
        if (menu == 0)
        {
            return;
        }

        try
        {
            AppendMenu(menu, 0, 1, "Mostrar HIT");
            AppendMenu(menu, 0, 2, "Ocultar HIT");
            AppendMenu(menu, 0x800, 0, null); // separator
            AppendMenu(menu, 0, 3, "Sair");
            SetForegroundWindow(window);
            var command = TrackPopupMenu(menu, 0x100 | 0x2, location.X, location.Y, 0, window, 0);
            switch (command)
            {
                case 1: Show(); break;
                case 2: ShowWindow(window, 0); break;
                case 3: PostMessage(window, 0x0010, 0, 0); break; // WM_CLOSE
                default:
                    var data = Data();
                    ShellNotifyIcon(3, ref data); // NIM_SETFOCUS
                    break;
            }
            PostMessage(window, 0, 0, 0);
        }
        finally
        {
            DestroyMenu(menu);
        }
    }

    public void Dispose()
    {
        if (_added)
        {
            var data = Data();
            ShellNotifyIcon(2, ref data);
        }
        _added = false;
        _enabled = false;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NotifyIconData
    {
        public uint Size;
        public nint Window;
        public uint Id, Flags, Callback;
        public nint Icon;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)] public string Tip;
        public uint State, StateMask;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)] public string Info;
        public uint Version;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)] public string InfoTitle;
        public uint InfoFlags;
        public Guid Guid;
        public nint BalloonIcon;
    }

    [DllImport("shell32.dll", EntryPoint = "Shell_NotifyIconW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShellNotifyIcon(uint operation, ref NotifyIconData data);

    [DllImport("user32.dll", EntryPoint = "LoadIconW")]
    private static extern nint LoadIcon(nint instance, nint resource);

    [DllImport("user32.dll", EntryPoint = "RegisterWindowMessageW", CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string name);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ShowWindow(nint window, int command);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool IsWindowVisible(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetForegroundWindow(nint window);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out Point point);

    [DllImport("user32.dll")]
    private static extern nint CreatePopupMenu();

    [DllImport("user32.dll", EntryPoint = "AppendMenuW", CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool AppendMenu(nint menu, uint flags, nuint id, string? text);

    [DllImport("user32.dll")]
    private static extern uint TrackPopupMenu(nint menu, uint flags, int x, int y, int reserved, nint owner, nint rect);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool DestroyMenu(nint menu);

    [DllImport("user32.dll", EntryPoint = "PostMessageW")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool PostMessage(nint window, uint message, nuint wParam, nint lParam);
}
