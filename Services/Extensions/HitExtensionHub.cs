using System.Collections.ObjectModel;
using System.Threading.Channels;
using HIT.Sdk;
using HoldItTogether.Services.Audio;
using HoldItTogether.Services.Settings;
using HoldItTogether.Services.Diagnostics;

namespace HoldItTogether.Services.Extensions;

public sealed class HitExtensionHub : IAsyncDisposable
{
    private readonly IHitMediaService _media;
    private readonly AudioSpectrumService _spectrum;
    private readonly HitSettingsService _settings;
    private readonly HitResourcePolicy _resources;
    private readonly ExtensionStore _store;
    private readonly HitDiagnosticLog _log;
    private readonly object _sync=new();
    private readonly List<ISubscription> _subscriptions=[];
    private readonly Dictionary<string,RegisteredAction> _actions=[];
    private readonly CancellationTokenSource _lifetime=new();
    private readonly SemaphoreSlim _changed=new(0,1);
    private Task? _worker;
    private bool _disposed;
    private HitHostState _host=new(HitSurfaceState.Collapsed,false,false,false);
    private IReadOnlyDictionary<string,string> _tokens=new ReadOnlyDictionary<string,string>(new Dictionary<string,string>());
    private HitMediaState? _lastMedia;
    private bool _systemReducedMotion;
    public HitExtensionHub(IHitMediaService media,AudioSpectrumService spectrum,HitSettingsService settings,HitResourcePolicy resources,ExtensionStore store,HitDiagnosticLog log)
    {_media=media;_spectrum=spectrum;_settings=settings;_resources=resources;_store=store;_log=log;settings.Changed+=AppearanceChanged;resources.Changed+=ResourcesChanged;store.Changed+=StoreChanged;}
    internal bool Enabled(string owner)=>_store.State.Packages.FirstOrDefault(p=>p.Manifest.Id==owner&&!p.PendingDelete) is { } package&&package.Manifest.Menus.Any(menu=>_store.IsEnabled(owner+":"+menu.Id));
    public HitHostState Host {get{lock(_sync)return _host with{Sleeping=_resources.IsSleeping};}}
    public HitAppearanceState Appearance {get{lock(_sync)return new(_settings.Current.Theme,_settings.Current.RemoveAnimations||_systemReducedMotion,_tokens);}}
    public void PublishHost(HitSurfaceState surface,bool visible,bool pinned)
    {var next=new HitHostState(surface,visible,pinned,_resources.IsSleeping);lock(_sync){if(next==_host)return;_host=next;}Publish(next);Signal();}
    public void PublishTokens(Dictionary<string,string> tokens,bool systemReducedMotion=false)
    {
        var safe=tokens.Where(p=>p.Key.StartsWith("--hit-",StringComparison.Ordinal)&&p.Key.Length<=80&&p.Value.Length<=512).Take(32).ToDictionary();
        lock(_sync){if(_systemReducedMotion==systemReducedMotion&&_tokens.Count==safe.Count&&safe.All(p=>_tokens.TryGetValue(p.Key,out var value)&&value==p.Value))return;_systemReducedMotion=systemReducedMotion;_tokens=new ReadOnlyDictionary<string,string>(safe);}
        Publish(Appearance);
    }
    private void AppearanceChanged()=>Publish(Appearance);
    private void ResourcesChanged(){PublishHost(Host.Surface,Host.Visible,Host.Pinned);Signal();}
    private void StoreChanged(){Publish(Host);Publish(Appearance);Signal();}
    private void Signal(){try{if(_changed.CurrentCount==0)_changed.Release();}catch(ObjectDisposedException){}catch(SemaphoreFullException){}}
    internal IDisposable Subscribe<T>(string owner,Action<T> observer,CancellationToken token,Func<bool> enabled)
    {
        ArgumentNullException.ThrowIfNull(observer);token.ThrowIfCancellationRequested();
        var visual=typeof(T)==typeof(HitMediaState)||typeof(T)==typeof(HitSpectrumFrame);
        var subscription=new Subscription<T>(owner,observer,()=>enabled()&&(!visual||(!_resources.IsSleeping&&Host.Visible)),token,_log,Remove);
        lock(_sync)
        {
            if(_disposed){subscription.Dispose();throw new ObjectDisposedException(nameof(HitExtensionHub));}
            if(_subscriptions.Count(s=>s.Owner==owner)>=32){subscription.Dispose();throw new InvalidOperationException("At most 32 subscriptions per package.");}
            _subscriptions.Add(subscription);_worker??=Task.Run(RunAsync);
        }
        if(typeof(T)==typeof(HitHostState))subscription.Send((T)(object)Host);
        if(typeof(T)==typeof(HitAppearanceState))subscription.Send((T)(object)Appearance);
        if(typeof(T)==typeof(HitMediaState)&&_lastMedia is { } cached)subscription.Send((T)(object)cached);
        if(token.IsCancellationRequested){subscription.Dispose();token.ThrowIfCancellationRequested();}
        Signal();return subscription;
    }
    private void Remove(ISubscription subscription){lock(_sync)_subscriptions.Remove(subscription);Signal();}
    private void Publish<T>(T value){ISubscription[] subscribers;lock(_sync)subscribers=_subscriptions.ToArray();foreach(var subscriber in subscribers)if(subscriber is Subscription<T> typed)typed.Send(value);}
    private async Task RunAsync()
    {
        IDisposable? capture=null;var mediaAt=DateTime.MinValue;var outputAt=DateTime.MinValue;
        try
        {
            while(!_lifetime.IsCancellationRequested)
            {
                bool wantsMedia,wantsSpectrum;
                lock(_sync){wantsMedia=_subscriptions.Any(s=>s is Subscription<HitMediaState>&&Enabled(s.Owner));wantsSpectrum=_subscriptions.Any(s=>s is Subscription<HitSpectrumFrame>&&Enabled(s.Owner));}
                if(_resources.IsSleeping||!Host.Visible){wantsMedia=false;wantsSpectrum=false;}
                if(!wantsSpectrum)ReleaseCapture(ref capture);
                if(!wantsMedia)_lastMedia=null;
                if(!wantsMedia&&!wantsSpectrum){await _changed.WaitAsync(_lifetime.Token);continue;}
                if(wantsMedia&&DateTime.UtcNow>=mediaAt)
                {
                    mediaAt=DateTime.UtcNow.AddMilliseconds(500);
                    try{using var timeout=CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);timeout.CancelAfter(4000);var media=ExtensionApi.MediaState(await _media.ReadAsync(timeout.Token));if(media!=_lastMedia){_lastMedia=media;Publish(media);}}
                    catch(Exception error)when(error is System.Runtime.InteropServices.COMException or UnauthorizedAccessException or InvalidOperationException or OperationCanceledException){_log.Record("sdk","media.read.failed");}
                }
                if(wantsSpectrum)
                {
                    try{capture??=_spectrum.Acquire();if(DateTime.UtcNow>=outputAt){_spectrum.RefreshOutput();outputAt=DateTime.UtcNow.AddSeconds(2);}if(_spectrum.Error is not null)throw new InvalidOperationException("Audio output unavailable.");Publish(new HitSpectrumFrame(Array.AsReadOnly(_spectrum.Read()),true,null));}
                    catch(Exception error)when(error is System.Runtime.InteropServices.COMException or System.ComponentModel.Win32Exception or InvalidOperationException){ReleaseCapture(ref capture);Publish(new HitSpectrumFrame(Array.AsReadOnly(new double[32]),false,"Audio output unavailable."));_log.Record("sdk","spectrum.failed");await Task.Delay(1000,_lifetime.Token);}
                }
                await Task.Delay(wantsSpectrum?50:500,_lifetime.Token);
            }
        }
        catch(OperationCanceledException)when(_lifetime.IsCancellationRequested){}
        finally{ReleaseCapture(ref capture);}
    }
    private void ReleaseCapture(ref IDisposable? capture){var owned=capture;capture=null;try{owned?.Dispose();}catch(Exception error)when(error is System.Runtime.InteropServices.COMException or InvalidOperationException){_log.Record("sdk","spectrum.stop.failed");}}
    private sealed record RegisteredAction(string Owner,HitActionDescriptor Descriptor,Func<CancellationToken,Task> Execute,CancellationToken Lifetime);
    internal IDisposable Register(string owner,string id,string title,Func<CancellationToken,Task> execute,CancellationToken lifetime)
    {
        if(!ExtensionManifest.ValidId(id)||string.IsNullOrWhiteSpace(title)||title.Length>80)throw new ArgumentException("Invalid action ID/title.");
        ArgumentNullException.ThrowIfNull(execute);lifetime.ThrowIfCancellationRequested();var key=owner+":"+id;
        lock(_sync){ObjectDisposedException.ThrowIf(_disposed,this);if(_actions.ContainsKey(key)||_actions.Values.Count(a=>a.Owner==owner)>=32)throw new InvalidOperationException("Duplicate action or package action limit reached.");_actions.Add(key,new(owner,new(key,title),execute,lifetime));}
        return new ActionLease(()=>{lock(_sync)_actions.Remove(key);},lifetime);
    }
    internal HitActionDescriptor[] Actions(){lock(_sync)return _actions.Values.Where(a=>Enabled(a.Owner)&&!a.Lifetime.IsCancellationRequested).Select(a=>a.Descriptor).ToArray();}
    internal async Task<bool> ExecuteAsync(string id,CancellationToken token)
    {
        RegisteredAction? action;lock(_sync)_actions.TryGetValue(id,out action);
        if(action is null||!Enabled(action.Owner)||action.Lifetime.IsCancellationRequested)return false;
        using var linked=CancellationTokenSource.CreateLinkedTokenSource(token,action.Lifetime,_lifetime.Token);
        await action.Execute(linked.Token);return true;
    }
    private interface ISubscription:IDisposable{string Owner{get;}}
    private sealed class Subscription<T>:ISubscription
    {
        public string Owner{get;}
        private readonly Channel<T> _queue=Channel.CreateBounded<T>(new BoundedChannelOptions(1){FullMode=BoundedChannelFullMode.DropOldest});
        private readonly CancellationTokenSource _stop=new();private readonly CancellationTokenRegistration _registration;
        private readonly Action<ISubscription> _remove;private int _disposed;
        public Subscription(string owner,Action<T> observer,Func<bool> enabled,CancellationToken token,HitDiagnosticLog log,Action<ISubscription> remove)
        {Owner=owner;_remove=remove;_registration=token.Register(Dispose);_=Task.Run(async()=>{try{await foreach(var value in _queue.Reader.ReadAllAsync(_stop.Token)){if(enabled())try{observer(value);}catch{log.Record("sdk","observer.failed");}}}catch(OperationCanceledException){}finally{_stop.Dispose();}});}
        public void Send(T value)=>_queue.Writer.TryWrite(value);
        public void Dispose(){if(Interlocked.Exchange(ref _disposed,1)!=0)return;_stop.Cancel();_queue.Writer.TryComplete();_registration.Unregister();_remove(this);}
    }
    private sealed class ActionLease:IDisposable
    {
        private Action? _remove;private readonly CancellationTokenRegistration _registration;
        public ActionLease(Action remove,CancellationToken token){_remove=remove;_registration=token.Register(Dispose);}
        public void Dispose(){Interlocked.Exchange(ref _remove,null)?.Invoke();_registration.Unregister();}
    }
    public async ValueTask DisposeAsync()
    {
        ISubscription[] subscriptions;lock(_sync){if(_disposed)return;_disposed=true;subscriptions=_subscriptions.ToArray();_actions.Clear();}
        _settings.Changed-=AppearanceChanged;_resources.Changed-=ResourcesChanged;_store.Changed-=StoreChanged;
        foreach(var item in subscriptions)item.Dispose();_lifetime.Cancel();if(_worker is not null)await _worker;_lifetime.Dispose();_changed.Dispose();
    }
}
