using System.Runtime.InteropServices;

namespace HoldItTogether.Services.Clock;

public interface IClockSound { void Play(); }
internal sealed class ClockSound : IClockSound
{
    public void Play() { if (OperatingSystem.IsWindows()) MessageBeep(0x40); }
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool MessageBeep(uint kind);
}
