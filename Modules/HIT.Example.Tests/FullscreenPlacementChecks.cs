using System.Drawing;
using HoldItTogether.Services;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Windows;

internal static class FullscreenPlacementChecks
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        foreach(var monitor in new[]{new Rectangle(0,0,1920,1080),new Rectangle(-1920,-200,1920,1080)})
        {
            Check(WindowsFullscreenTracker.CoversMonitor(monitor,monitor,unchecked((int)0x80000000)),"Borderless monitor coverage must detect fullscreen.");
            Check(!WindowsFullscreenTracker.CoversMonitor(monitor,monitor,0x00C00000),"Ordinary maximized windows must not count as fullscreen.");
            Check(!WindowsFullscreenTracker.CoversMonitor(new(monitor.X,monitor.Y,monitor.Width,monitor.Height-40),monitor,0),"Work-area coverage alone is not fullscreen.");
            foreach(var work in new[]{new Rectangle(monitor.X,monitor.Y,monitor.Width,monitor.Height-40),
                new Rectangle(monitor.X+40,monitor.Y,monitor.Width-40,monitor.Height),
                new Rectangle(monitor.X,monitor.Y+40,monitor.Width,monitor.Height-40),
                new Rectangle(monitor.X,monitor.Y,monitor.Width-40,monitor.Height)})
            {
                var frame=new RectangleF(work.Left>monitor.Left?monitor.Left:monitor.Right-56,
                    work.Top>monitor.Top?monitor.Top:monitor.Bottom-56,56,56);
                var full=WindowPlacement.FullscreenTarget(frame,monitor,null);
                Check(full==frame,"Fullscreen must permit positions in the taskbar strip.");
                var legal=WindowPlacement.ConstrainSurface(full,work);
                Check(legal!=full&&work.Contains(Rectangle.Ceiling(legal)),"A taskbar-overlapping surface must move fully into the normal work area.");
                Check(legal==WindowPlacement.ConstrainSurface(legal,work),"Leaving fullscreen must clamp once to the nearest legal surface.");
                Check(WindowPlacement.ConstrainSurface(new(work.X+100,work.Y+100,56,56),work)==new RectangleF(work.X+100,work.Y+100,56,56),"Already legal positions must not move on exit.");
                var start=full.Location;var end=legal.Location;var previous=start;
                foreach(var progress in new[]{0d,.1,.25,.5,.75,1})
                {
                    var point=WindowPlacement.InterpolatePosition(start,end,progress);
                    Check(point.X>=Math.Min(start.X,end.X)&&point.X<=Math.Max(start.X,end.X)&&
                        point.Y>=Math.Min(start.Y,end.Y)&&point.Y<=Math.Max(start.Y,end.Y),"Movement must never overshoot.");
                    Check(Math.Abs(point.X-end.X)<=Math.Abs(previous.X-end.X)&&Math.Abs(point.Y-end.Y)<=Math.Abs(previous.Y-end.Y),
                        "Animation must approach its destination monotonically.");
                    if(progress==0)Check(point==start,"Animation must start at the current position.");
                    if(progress==1)Check(point==end,"Animation must finish at the legal position.");
                    previous=point;
                }
                var saved=new SavedWindowPosition(full.Right,full.Bottom,1,1,monitor.X,monitor.Y,monitor.Width,monitor.Height);
                Check(WindowPlacement.FullscreenTarget(legal,monitor,saved)==full,"Re-entering fullscreen must restore its saved position, not the clamped desktop position.");
            }
        }
        var path=Path.GetFullPath("obj/fullscreen-tests/"+Guid.NewGuid().ToString("N")+"/window.json");
        var store=new WindowPositionStore(path);
        var desktop=new SavedWindowPosition(500,500,1,1,0,0,1920,1040);
        var fullscreen=desktop with {Y=1080,WorkHeight=1080};
        Check(store.Save(desktop)&&store.SaveFullscreen(fullscreen),"Both positions must persist.");
        Check(new WindowPositionStore(path).Load()==desktop&&new WindowPositionStore(path).LoadFullscreen().Single()==fullscreen,
            "Fullscreen memory must survive restart without overwriting desktop memory.");
        Check(store.SaveFullscreen(fullscreen with {X=400})&&store.LoadFullscreen().Length==1,"Updating the same monitor must replace its position.");
        Check(!store.SaveFullscreen(fullscreen with {X=double.NaN}),"Invalid fullscreen state must not persist.");
        var resized=WindowPlacement.FullscreenTarget(new(50,50,56,56),new(0,0,2560,1440),fullscreen);
        Check(Math.Abs(resized.Bottom-1440)<.01&&Math.Abs(resized.Right-500d/1920*2560)<.01,
            "Saved anchors must adapt proportionally when the monitor resolution changes.");
        for(var index=1;index<20;index++)store.SaveFullscreen(fullscreen with {WorkLeft=index*1920,X=index*1920+300});
        Check(store.LoadFullscreen().Length==16,"Fullscreen monitor history must be bounded.");
        File.WriteAllText(Path.Combine(Path.GetDirectoryName(path)!,"window-fullscreen.json"),"{broken");
        Check(store.LoadFullscreen().Length==0&&store.Load()==desktop,"Corrupt fullscreen memory must preserve desktop state.");
        Console.WriteLine("PASS: fullscreen detection, per-monitor taskbar limits, remembered position, animation endpoints, bounded persistence and corruption fallback.");
    }
}
