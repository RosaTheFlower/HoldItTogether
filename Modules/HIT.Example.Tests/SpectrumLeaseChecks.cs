using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Diagnostics;

internal static class SpectrumLeaseChecks
{
    public static void Run()
    {
        var log=new HitDiagnosticLog();using var spectrum=new AudioSpectrumService(log);
        IDisposable first;
        try{first=spectrum.Acquire();}
        catch(Exception error)when(error is System.Runtime.InteropServices.COMException or System.ComponentModel.Win32Exception or InvalidOperationException)
        {Console.WriteLine("SKIP: default-output device unavailable for live capture lease checks ("+error.GetType().Name+").");return;}
        using(first)
        {
            using(var second=spectrum.Acquire())
            {
                first.Dispose();
                if(spectrum.ActiveCaptures!=1||spectrum.Read().Length!=32)throw new Exception("A remaining SDK owner must retain one shared 32-band capture.");
            }
            if(spectrum.ActiveCaptures!=0)throw new Exception("The final owner must stop capture.");
        }
        if(log.Snapshot().Where(e=>e.Code=="capture.started").Sum(e=>e.Count)!=1)throw new Exception("Two owners opened duplicate captures.");
        using(var sdk=spectrum.Acquire())
        {spectrum.Start();spectrum.Stop();if(spectrum.ActiveCaptures!=1)throw new Exception("A legacy/built-in release must not stop an SDK owner.");}
        if(spectrum.ActiveCaptures!=0)throw new Exception("Lease teardown leaked capture.");
        Console.WriteLine("PASS: live default-output capture shared by two owners, last-owner release, 32 bands and legacy-owner isolation.");
    }
}
