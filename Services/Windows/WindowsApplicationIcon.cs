using System.Runtime.InteropServices;

namespace HoldItTogether.Services.Windows;

internal sealed class WindowsApplicationIcon(nint window) : IDisposable
{
    private nint _small,_large;
    public nint Small=>_small;
    public void LoadDefault()
    {
        var file=Path.Combine(AppContext.BaseDirectory,"Resources","hit.ico");
        if(File.Exists(file))Replace(LoadImage(0,file,1,32,32,0x10),LoadImage(0,file,1,64,64,0x10));
    }
    public void SetPng(byte[] png)
    {
        if(png.Length is <8 or >262144||!png.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}))return;
        Replace(CreateIconFromResourceEx(png,(uint)png.Length,true,0x30000,32,32,0),
            CreateIconFromResourceEx(png,(uint)png.Length,true,0x30000,64,64,0));
    }
    private void Replace(nint small,nint large)
    {
        if(small==0||large==0){if(small!=0)DestroyIcon(small);if(large!=0)DestroyIcon(large);return;}
        SendMessage(window,0x80,0,small);SendMessage(window,0x80,1,large);
        var oldSmall=_small;var oldLarge=_large;_small=small;_large=large;
        if(oldSmall!=0)DestroyIcon(oldSmall);if(oldLarge!=0)DestroyIcon(oldLarge);
    }
    public void Dispose(){if(_small!=0)DestroyIcon(_small);if(_large!=0)DestroyIcon(_large);_small=_large=0;}
    [DllImport("user32.dll",EntryPoint="LoadImageW",CharSet=CharSet.Unicode)]
    private static extern nint LoadImage(nint instance,string name,uint type,int width,int height,uint flags);
    [DllImport("user32.dll")]
    private static extern nint CreateIconFromResourceEx(byte[] data,uint size,[MarshalAs(UnmanagedType.Bool)]bool icon,uint version,int width,int height,uint flags);
    [DllImport("user32.dll",EntryPoint="SendMessageW")]
    private static extern nint SendMessage(nint window,uint message,nuint parameter,nint data);
    [DllImport("user32.dll")]
    [return:MarshalAs(UnmanagedType.Bool)]private static extern bool DestroyIcon(nint icon);
}
