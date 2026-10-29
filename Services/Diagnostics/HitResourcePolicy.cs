namespace HoldItTogether.Services.Diagnostics;

/// <summary>Cooperative visual-resource sleep. Domain workers such as alarms are independent.</summary>
public sealed class HitResourcePolicy
{
    private readonly HitDiagnosticLog? _diagnostics;
    public HitResourcePolicy(HitDiagnosticLog? diagnostics=null)=>_diagnostics=diagnostics;
    public const int IdleDelayMs = 5000;
    private long? _idleSince;
    private volatile bool _isSleeping;
    public bool IsSleeping { get=>_isSleeping; private set=>_isSleeping=value; }
    public event Action? Changed;

    public void Update(bool enabled, bool sphereCollapsed, bool coveredByFullscreen, bool interacting, long now)
    {
        if (!enabled || interacting || (!sphereCollapsed && !coveredByFullscreen))
        {
            Wake();
            return;
        }
        _idleSince ??= now;
        if (!IsSleeping && now - _idleSince >= IdleDelayMs)
        {
            IsSleeping = true;
            _diagnostics?.Record("resources","sleep");
            Changed?.Invoke();
        }
    }

    public void Wake()
    {
        _idleSince = null;
        if (!IsSleeping) return;
        IsSleeping = false;
        _diagnostics?.Record("resources","wake");
        Changed?.Invoke();
    }
}
