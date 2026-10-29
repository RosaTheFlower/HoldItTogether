using HoldItTogether.Services.HitMenus;
using HoldItTogether.Services.Settings;

internal static class NavigationChecks
{
    public static void Run()
    {
        static void Check(bool value,string message){if(!value)throw new Exception(message);}
        var settings=new HitSettings {InfiniteMenu=true,ScrollCooldownMs=100};
        var navigation=new HitMenuNavigator(3);
        Check(navigation.Scroll(0,-120,0,false,0,settings)&&navigation.SelectedIndex==2,"Infinite wheel navigation wraps backward from the first menu.");
        Check(navigation.Scroll(0,120,0,false,200,settings)&&navigation.SelectedIndex==0,"Infinite wheel navigation wraps forward from the last menu.");
        for(var i=1;i<=3;i++)Check(navigation.Scroll(0,120,0,false,200+i*200,settings),"Every wheel step advances around the loop.");
        Check(navigation.SelectedIndex==0,"A complete loop returns to the initial menu.");
        Check(!navigation.Scroll(0,120,0,false,801,settings),"Infinite mode preserves wheel cooldown.");
        Check(!navigation.Scroll(0,120,0,true,1000,settings),"Infinite mode preserves zoom guards.");
        Check(navigation.Select(-1,true)&&navigation.SelectedIndex==2,"Explicit menu-mode Back supports wrapping.");
        Check(!navigation.Select(3)&&navigation.SelectedIndex==2,"Normal selection stays bounded unless wrap is requested.");
        var bounded=new HitMenuNavigator(3);
        Check(!bounded.Scroll(0,-120,0,false,0,new()),"Infinite mode defaults OFF and preserves the first boundary.");
        Check(!new HitMenuNavigator(0).Scroll(0,120,0,false,0,settings)&&!new HitMenuNavigator(1).Scroll(0,120,0,false,0,settings),"Empty and single-menu catalogs stay stable.");
        Console.WriteLine("PASS: infinite/bounded navigation, full cycles, explicit Back, cooldown, zoom guards and small catalogs.");
    }
}
