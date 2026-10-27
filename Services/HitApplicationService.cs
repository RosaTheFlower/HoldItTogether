using System.Diagnostics;

namespace HoldItTogether.Services;

/// <summary>Restarts the current distribution after a deliberate user operation.</summary>
public sealed class HitApplicationService
{
    private Action? _close;
    private int _restarting;
    private ProcessStartInfo? _pending;
    public void BindClose(Action close) => _close = close;
    public Task RestartAsync(string? openView = null)
    {
        if (_close is null) throw new InvalidOperationException("Restart HIT to complete this operation.");
        if (Interlocked.Exchange(ref _restarting, 1) != 0) return Task.CompletedTask;
        try
        {
            var executable = Environment.ProcessPath ?? throw new InvalidOperationException("The application path is unavailable.");
            var start = new ProcessStartInfo(executable) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = AppContext.BaseDirectory };
            var arguments = Environment.GetCommandLineArgs();
            if (Path.GetFileNameWithoutExtension(executable).Equals("dotnet", StringComparison.OrdinalIgnoreCase)) start.ArgumentList.Add(arguments[0]);
            foreach (var argument in arguments.Skip(1).Where(argument => !argument.StartsWith("--hit-open=", StringComparison.Ordinal))) start.ArgumentList.Add(argument);
            if (openView == "extensions") start.ArgumentList.Add("--hit-open=extensions");
            _pending = start;
            _close();
            return Task.CompletedTask;
        }
        catch { _pending = null; Volatile.Write(ref _restarting, 0); throw; }
    }
    public void StartPending()
    {
        if (_pending is not { } start) return;
        _pending = null;
        using var next = Process.Start(start) ?? throw new InvalidOperationException("Could not restart HIT.");
    }
}
