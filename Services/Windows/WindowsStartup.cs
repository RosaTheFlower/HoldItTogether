using Microsoft.Win32;

namespace HoldItTogether.Services.Windows;

internal static class WindowsStartup
{
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal static string Command(string executable,string? assembly=null) =>
        "\""+executable+"\""+(assembly is null?"":" \""+assembly+"\"");
    public static void Apply(bool enabled)
    {
        if(!OperatingSystem.IsWindows())return;
        try
        {
            using var key=Registry.CurrentUser.CreateSubKey(RunKey,true);
            if(!enabled){key.DeleteValue("HoldItTogether",false);return;}
            var executable=Environment.ProcessPath??throw new InvalidOperationException("The application path is unavailable.");
            var command=Command(executable,Path.GetFileNameWithoutExtension(executable).Equals("dotnet",StringComparison.OrdinalIgnoreCase)?Environment.GetCommandLineArgs()[0]:null);
            if(command.Length>260)throw new InvalidOperationException("The application path is too long for Windows startup. Move HIT to a shorter path.");
            if(!Equals(key.GetValue("HoldItTogether"),command))key.SetValue("HoldItTogether",command,RegistryValueKind.String);
        }
        catch(Exception error)when(error is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {throw new InvalidOperationException("Could not change Windows startup registration.",error);}
    }
}
