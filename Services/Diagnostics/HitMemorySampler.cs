using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HoldItTogether.Services.Diagnostics;

public readonly record struct HitMemoryUsage(long Bytes, int ProcessCount, bool IsPrivateWorkingSet = false)
{
    public double Megabytes => Bytes / (1024d * 1024);
}

public sealed class HitMemorySampler
{
    public HitMemoryUsage Sample()
    {
        if (OperatingSystem.IsWindows())
        {
            var counters = new ProcessMemoryCounters
            {
                Size = (uint)Marshal.SizeOf<ProcessMemoryCounters>()
            };

            // Private resident memory of this process, not the browser process
            // group or private committed/virtual memory.
            if (GetProcessMemoryInfo(new nint(-1), ref counters, counters.Size) &&
                counters.PrivateWorkingSetSize > 0)
            {
                return new HitMemoryUsage((long)counters.PrivateWorkingSetSize, 1, true);
            }
        }

        // Older Windows versions may not expose PROCESS_MEMORY_COUNTERS_EX2.
        using var process = Process.GetCurrentProcess();
        process.Refresh();
        return new HitMemoryUsage(process.WorkingSet64, 1);
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessMemoryCounters
    {
        public uint Size, PageFaultCount;
        public nuint PeakWorkingSetSize, WorkingSetSize;
        public nuint QuotaPeakPagedPoolUsage, QuotaPagedPoolUsage;
        public nuint QuotaPeakNonPagedPoolUsage, QuotaNonPagedPoolUsage;
        public nuint PagefileUsage, PeakPagefileUsage, PrivateUsage;
        public nuint PrivateWorkingSetSize;
        public ulong SharedCommitUsage;
    }

    [DllImport("psapi.dll", EntryPoint = "GetProcessMemoryInfo", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetProcessMemoryInfo(
        nint process, ref ProcessMemoryCounters counters, uint size);
}