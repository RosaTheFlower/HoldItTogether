using HIT.Sdk;

namespace HIT.Example.Services;

/// <summary>Declare events.subscribe and host.read; pass VisibleLifetime from a menu.</summary>
public static class EventExample
{
    public static IDisposable ObserveHost(IHitApi api,Action<HitHostState> observer,CancellationToken visibleLifetime)
    {
        if(api is not IHitApiV2 current)throw new NotSupportedException("This example requires a host shipping HIT.Sdk 1.1.0.");
        // Observers run off the renderer thread; marshal UI updates with InvokeAsync in the caller.
        return current.Events.SubscribeHost(observer,visibleLifetime);
    }
}
