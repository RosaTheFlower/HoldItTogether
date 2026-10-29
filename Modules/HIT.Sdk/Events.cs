namespace HIT.Sdk;

/// <summary>Optional additive API. Test `api is IHitApiV2` before using it.</summary>
public interface IHitApiV2 : IHitApi
{
    IHitEvents Events {get;}
    IHitActions Actions {get;}
}
public enum HitSurfaceState { Collapsed, Open, Compact }
public sealed record HitHostState(HitSurfaceState Surface,bool Visible,bool Pinned,bool Sleeping);
public sealed record HitAppearanceState(string ThemeId,bool ReducedMotion,IReadOnlyDictionary<string,string> Tokens);
public sealed record HitSpectrumFrame(IReadOnlyList<double> Bands,bool Capturing,string? Error);
public interface IHitEvents
{
    HitHostState ReadHost();
    HitAppearanceState ReadAppearance();
    IDisposable SubscribeHost(Action<HitHostState> observer,CancellationToken cancellationToken=default);
    IDisposable SubscribeAppearance(Action<HitAppearanceState> observer,CancellationToken cancellationToken=default);
    IDisposable SubscribeMedia(Action<HitMediaState> observer,CancellationToken cancellationToken=default);
    IDisposable SubscribeSpectrum(Action<HitSpectrumFrame> observer,CancellationToken cancellationToken=default);
}
public sealed record HitActionDescriptor(string Id,string Title);
public interface IHitActions
{
    IDisposable Register(string id,string title,Func<CancellationToken,Task> execute);
    IReadOnlyList<HitActionDescriptor> List();
    Task<bool> ExecuteAsync(string qualifiedId,CancellationToken cancellationToken=default);
}
