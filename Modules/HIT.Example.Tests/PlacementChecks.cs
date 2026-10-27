using System.Drawing;
using HoldItTogether.Services;
using HoldItTogether.Services.Diagnostics;
using HoldItTogether.Services.Settings;

internal static class PlacementChecks
{
    private static void Check(bool value,string message){if(!value)throw new Exception(message);}
    public static void Run()
    {
        foreach(var work in new[]{new Rectangle(0,0,1920,1040),new Rectangle(-1920,0,1920,1040),new Rectangle(1920,-200,1920,1040)})
        foreach(var scale in new[]{1d,1.25,1.5,2})
        foreach(var pivot in new (double X,double Y)[]{(0d,0d),(1d,0d),(0d,1d),(1d,1d),(.5,.5)})
        {
            var start=new RectangleF(work.Left+work.Width*(float)(pivot.X==.5?.5:pivot.X==0?.2:.8),
                work.Top+work.Height*(float)(pivot.Y==.5?.5:pivot.Y==0?.2:.8),56,56);
            var anchor=WindowPlacement.SelectAnchor(start,work);
            Check(anchor.PivotX==pivot.X&&anchor.PivotY==pivot.Y,"Direction must be chosen from the dropped visible surface.");
            for(var cycle=0;cycle<30;cycle++)
            foreach(var size in new[]{(408d,310d),(188d,180d),(408d,310d),(180d,90d),(408d,250d),(56d,56d)})
            {
                // Oversized backing storage deliberately has a different center/corner from the visible menu.
                var width=size.Item1*scale;var height=size.Item2*scale;
                var local=new RectangleF((float)(24+(1600-width)*pivot.X),(float)(24+(1100-height)*pivot.Y),(float)width,(float)height);
                var placed=WindowPlacement.PlaceSurface(anchor,1648,1148,local,null);
                Check(Math.Abs(placed.Left+local.Left+width*pivot.X-anchor.X)<=.51&&Math.Abs(placed.Top+local.Top+height*pivot.Y-anchor.Y)<=.51,
                    "Clock/Chess/Audio compact/expand and reopen cycles must preserve the same visible anchor without cumulative movement.");
            }
        }
        var area=new Rectangle(0,0,1920,1040);
        var canvas=new Rectangle(-700,-400,1400,900);
        var visible=new RectangleF(500,400,56,56);
        var target=WindowPlacement.DragSurface(canvas,visible,new Point(1300,800),new PointF(12,16),area);
        Check(target.Left+visible.Left-canvas.Left==1288&&target.Top+visible.Top-canvas.Top==784,"A grab follows the cursor using the visible surface, regardless of the retained canvas.");
        var boundary=WindowPlacement.DragSurface(canvas,visible,new Point(1900,1000),new PointF(12,16),area);
        Check(boundary.Left+visible.Left-canvas.Left==1864&&boundary.Top+visible.Top-canvas.Top==984,"Only the visible surface can be constrained at monitor edges.");
        var initial=WindowPlacement.SelectAnchor(new RectangleF(200,180,56,56),area);
        _=WindowPlacement.PlaceSurface(initial,2400,1600,new(24,24,2300,1300),area);
        var restored=WindowPlacement.PlaceSurface(initial,2400,1600,new(24,24,56,56),area);
        Check(restored.Left+24==200&&restored.Top+24==180,"Fitting a large menu must not overwrite the drop anchor or glue later small views to an edge.");
        var padded=WindowPlacement.InsetWorkArea(area,98);
        Check(padded.Left>area.Left&&padded.Top>area.Top&&padded.Right<area.Right,"Default 98% edge reach leaves a small inset inside the work area.");
        Check(padded.Left-area.Left==padded.Top-area.Top&&area.Right-padded.Right==area.Bottom-padded.Bottom,"Edge padding uses the same pixel distance horizontally and vertically.");
        Check(WindowPlacement.InsetWorkArea(area,100)==area&&WindowPlacement.InsetWorkArea(area,85).Width<padded.Width,"100% is flush and 85% provides more space.");
        var near=new RectangleF(100,100,56,56);var plain=new Rectangle(76,76,600,700);
        var snapped=WindowPlacement.DragSurface(plain,near,new Point(padded.Left+16,padded.Top+16),new PointF(12,12),padded,true,padded);
        var unsnapped=WindowPlacement.DragSurface(plain,near,new Point(padded.Left+16,padded.Top+16),new PointF(12,12),padded,false,padded);
        Check(snapped.Left+24==padded.Left&&unsnapped.Left+24==padded.Left+4,"Magnetize attracts only near edges and can be disabled.");
        var positionPath=Path.GetFullPath("obj/placement-tests/"+Guid.NewGuid().ToString("N")+"/window.json");
        var positions=new WindowPositionStore(positionPath);
        var saved=new SavedWindowPosition(-340,220,1,0,-1920,0,1920,1040);
        Check(positions.Load() is null&&positions.Save(saved)&&new WindowPositionStore(positionPath).Load()==saved,"Last visible anchor survives a fresh application session, including secondary monitors.");
        File.WriteAllText(positionPath,"{broken");Check(positions.Load() is null,"Corrupt saved positions safely fall back to default placement.");
        Check(new HitSettings().WindowEdgeReachPercent==98&&(new HitSettings {WindowEdgeReachPercent=20}).Normalize().WindowEdgeReachPercent==85,"Placement defaults and bounds");

        Check(HitUpdateService.VersionLabel=="v"+HitUpdateService.ReleaseDate.ToString("ddMM"),"Public version is vDDMM from build release metadata.");
        var date=new DateOnly(2026,10,27);
        Check(HitUpdateService.IsNewerRelease("v0111",null,date),"Date versions are compared by month/day, never lexicographically.");
        Check(!HitUpdateService.IsNewerRelease("v2610",null,date)&&!HitUpdateService.IsNewerRelease("v2710",null,date),"Older/same date releases do not offer updates.");
        Check(HitUpdateService.IsNewerRelease("v0101",new DateTimeOffset(2027,1,1,0,0,0,TimeSpan.Zero),date),"Published release year handles New Year correctly.");
        Check(!HitUpdateService.IsNewerRelease("v3102",null,date)&&!HitUpdateService.IsNewerRelease("v1.0.0",null,date),"Invalid dates and legacy semantic tags do not crash update checks.");
        Console.WriteLine("PASS: visible-surface anchoring, repeated compact/expand, 100–200% DPI, negative/multiple monitor coordinates, drag offsets, edge recovery and vDDMM release ordering.");
    }
}
