using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using HoldItTogether.Services;

internal static class WindowGeometryChecks
{
    public static void Run()
    {
        if(!OperatingSystem.IsWindows())return;
        // A hidden native test window never takes focus or changes the user's desktop.
        var window=CreateWindowEx(0,"STATIC","HIT geometry test",0x80000000,100,100,1000,900,0,0,0,0);
        if(window==0)throw new Win32Exception(Marshal.GetLastWin32Error());
        var type=typeof(DesktopWindowService).Assembly.GetType("HoldItTogether.Services.WindowsWindowBehavior")!;
        if(type.GetMethod("GetPrimaryWorkArea")!.Invoke(null,null) is not System.Drawing.Rectangle||
            type.GetMethod("GetWorkAreas")!.Invoke(null,null) is not System.Drawing.Rectangle[] {Length:>0})
            throw new Exception("Native monitor discovery must include the primary work area.");
        object? behavior=null;
        var region=CreateRectRgn(0,0,0,0);
        try
        {
            behavior=Activator.CreateInstance(type,window,false)!;
            var apply=type.GetMethod("SetContentRegion")!;
            var before=GetGuiResources(GetCurrentProcess(),0);
            foreach(var scale in new[]{1d,1.25,1.5,2})
            {
                apply.Invoke(behavior,[24*scale,30*scale,360*scale,220*scale,16*scale]);
                if(GetWindowRgn(window,region)==0)throw new Exception("Native content region was not applied.");
                var middle=(int)(140*scale);
                if(!PtInRegion(region,(int)(200*scale),middle)||PtInRegion(region,(int)(384*scale)+1,middle)||PtInRegion(region,(int)(24*scale)-1,middle)||
                    PtInRegion(region,(int)(200*scale),(int)(30*scale)-1)||PtInRegion(region,(int)(200*scale),(int)(250*scale)+1)||
                    !PtInRegion(region,(int)(24*scale)+1,(int)(30*scale)+1))
                    throw new Exception("The native bounds must preserve CSS-antialiased corner pixels and exclude one pixel beyond each outer edge.");
            }
            for(var index=0;index<200;index++)apply.Invoke(behavior,[24d,30d,360d+index%2,220d,16d]);
            if(GetGuiResources(GetCurrentProcess(),0)>before+5)throw new Exception("Resizing leaked native region handles.");
        }
        finally{DeleteObject(region);DestroyWindow(window);GC.KeepAlive(behavior);}
        Console.WriteLine("PASS: native outer bounds at 100/125/150/200% DPI, preserved antialiasing pixels, one-pixel outside edges and region-handle lifetime.");
    }
    [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)]
    private static extern nint CreateWindowEx(uint ex,string className,string title,uint style,int x,int y,int width,int height,nint parent,nint menu,nint instance,nint param);
    [DllImport("user32.dll")]private static extern bool DestroyWindow(nint window);
    [DllImport("gdi32.dll")]private static extern nint CreateRectRgn(int left,int top,int right,int bottom);
    [DllImport("gdi32.dll")]private static extern bool PtInRegion(nint region,int x,int y);
    [DllImport("user32.dll")]private static extern int GetWindowRgn(nint window,nint region);
    [DllImport("gdi32.dll")]private static extern bool DeleteObject(nint region);
    [DllImport("kernel32.dll")]private static extern nint GetCurrentProcess();
    [DllImport("user32.dll")]private static extern uint GetGuiResources(nint process,uint flags);
}
