using System.Diagnostics;

namespace HoldItTogether.Services;

public interface IExternalLinkService
{
    bool OpenWebPage(string? address);
}

public sealed class ExternalLinkService : IExternalLinkService
{
    public static bool IsWebAddress(string? address) => Uri.TryCreate(address, UriKind.Absolute, out var uri) &&
        uri.Scheme is "https" or "http" && !string.IsNullOrWhiteSpace(uri.Host) && string.IsNullOrEmpty(uri.UserInfo);

    public bool OpenWebPage(string? address)
    {
        if (!IsWebAddress(address)) return false;
        try
        {
            Process.Start(new ProcessStartInfo(new Uri(address!).AbsoluteUri) { UseShellExecute = true });
            return true;
        }
        catch (Exception error) when (error is System.ComponentModel.Win32Exception or InvalidOperationException)
        { return false; }
    }
}
