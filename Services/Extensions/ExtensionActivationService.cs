namespace HoldItTogether.Services.Extensions;

/// <summary>New modules start immediately; replacing loaded code restarts only after graceful shutdown.</summary>
public sealed class ExtensionActivationService(ExtensionRuntime runtime, HitApplicationService application, ExtensionUi ui)
{
    public async Task ActivateAsync(string id, string name)
    {
        if (await runtime.ActivateAsync(id)) ui.Report(name + " is ready. Enable its menus below.");
        else
        {
            ui.Report(name + " updated. Restarting HIT…");
            await application.RestartAsync("extensions");
        }
    }
}
