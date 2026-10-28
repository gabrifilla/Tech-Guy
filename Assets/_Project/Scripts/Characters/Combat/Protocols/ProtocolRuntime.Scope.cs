using System;

public sealed partial class ProtocolRuntime
{
    private ProtocolRootContext _areaRoot;
    private int _areaScopes;
    private ProtocolProducer _scopeProducer = ProtocolProducer.Direct;
    private int _scopeDepth;
    private long _nextReservedProducer;
    private ProtocolExecution _activeExecution;
    public ProtocolProducer ScopedProducer => _areaScopes > 0 ? _scopeProducer : ProtocolProducer.Direct;
    public int ScopedDepth => _areaScopes > 0 ? _scopeDepth : 0;
    internal bool ScopedAnalyticsSecondary => _scopeProducer != ProtocolProducer.Direct &&
        !(_activeExecution != null && _activeExecution.OwnsRoot && _activeExecution.Producer == _scopeProducer);
    public ProtocolExecution BeginExecution(ProtocolProducer producer = ProtocolProducer.Direct)
    { using (Profile()) return new ProtocolExecution(this, BeginStandalone(producer), producer, 0, true, producer == ProtocolProducer.Direct ? 0 : 1); }
    public ProtocolExecution ReserveExecution(ProtocolProducer producer)
    {
        using (Profile())
        {
        var root = _areaScopes > 0 ? _areaRoot : null;
        long id = -checked(++_nextReservedProducer);
        bool admitted = root != null && (_activeExecution != null && _activeExecution.ReservationId != 0 ?
            root.ReserveDescendant(_activeExecution.ReservationId, id) : root.ReserveChild(id));
        if (!admitted) root = null;
        return new ProtocolExecution(this, root, producer, id, false, producer == ProtocolProducer.Direct ? _scopeDepth : Math.Max(1, _scopeDepth));
        }
    }
    public IDisposable OpenExecutionScope(ProtocolExecution execution)
    {
        using (Profile())
        {
        var previous = _areaRoot; var previousProducer = _scopeProducer; var previousExecution = _activeExecution; int previousDepth = _scopeDepth;
        _areaRoot = execution != null && execution.Owner == this && execution.IsValid ? execution.Root : null;
        _scopeProducer = execution?.Producer ?? ProtocolProducer.Direct; _areaScopes++;
        _activeExecution = execution;
        _scopeDepth = execution?.Depth ?? 0;
        return new ProtocolScope(this, () => { _areaScopes--; _areaRoot = previous; _scopeProducer = previousProducer; _activeExecution = previousExecution; _scopeDepth = previousDepth; });
        }
    }
    internal IDisposable OpenCallbackScope(ProtocolRootContext root, int parentDepth)
    {
        using (Profile())
        {
        var previous = _areaRoot; var previousProducer = _scopeProducer; int previousDepth = _scopeDepth;
        _areaRoot = root; _scopeProducer = ProtocolProducer.Technique; _scopeDepth = parentDepth + 1; _areaScopes++;
        return new ProtocolScope(this, () => { _areaScopes--; _areaRoot = previous; _scopeProducer = previousProducer; _scopeDepth = previousDepth; });
        }
    }
    public IDisposable BeginAreaScope(GauntletImpactContext context)
    {
        using (Profile())
        {
        if (context != null) return null;
        var previous = _areaRoot;
        bool owns = _areaScopes == 0;
        if (owns) _areaRoot = BeginStandalone();
        _areaScopes++;
        return new ProtocolScope(this, () => { _areaScopes--; if (owns) _areaRoot?.Seal(); _areaRoot = previous; });
        }
    }
    public IDisposable BeginImpactScope(GauntletImpactContext context, out ProtocolRootContext root)
    {
        using (Profile())
        {
        TouchFrame();
        root = context != null ? FindRoot(context) : _areaScopes > 0 ? _areaRoot : BeginStandalone();
        if (context != null || _areaScopes > 0 || root == null) return null;
        var owned = root; return new ProtocolScope(this, owned.Seal);
        }
    }
    public float AmplifyChild(ProtocolRootContext root, ProtocolProducer producer, ProtocolHitSnapshot before, float damage)
    {
        using (Profile())
        {
        if (!Matches(root) || !ProtocolEligibility.Amplifies(producer)) return damage;
        float amplified = damage * (1f + _inventory.Catalog.Get(ProtocolKind.Resonance).DamagePerRank *
            root.Rank(ProtocolKind.Resonance) * ((before.Burn ? 1 : 0) + (before.Chill ? 1 : 0)));
        return ReactionNumeric.IsFinite(amplified) ? amplified : damage;
        }
    }
    private sealed class ProtocolScope : IDisposable
    {
        private Action _release;
        private readonly ProtocolRuntime _owner;
        public ProtocolScope(ProtocolRuntime owner, Action release) { _owner = owner; _release = release; }
        public void Dispose() { var release = _release; _release = null; using (_owner.Profile()) release?.Invoke(); }
    }
}

/// <summary>A reserved producer retains its execution across frames without keeping a global active scope.</summary>
public sealed class ProtocolExecution : IDisposable
{
    internal ProtocolRuntime Owner { get; }
    internal ProtocolRootContext Root { get; }
    public ProtocolProducer Producer { get; }
    public int Depth { get; }
    private readonly long _id;
    internal long ReservationId => _id;
    private readonly bool _ownsRoot;
    internal bool OwnsRoot => _ownsRoot;
    private bool _closed;
    private readonly Guid _sessionId;
    private readonly int _encounterId;
    private readonly long _lifecycleRevision;
    public bool IsLifecycleCurrent => !_closed && Owner && Owner.IsOpen && Owner.LifecycleRevision == _lifecycleRevision && Owner.Inventory.SessionId == _sessionId && Owner.EncounterId == _encounterId;
    public bool IsValid => !_closed && Root != null && Root.IsValid && (_ownsRoot || Root.HasChild(_id));
    internal ProtocolExecution(ProtocolRuntime owner, ProtocolRootContext root, ProtocolProducer producer, long id, bool ownsRoot, int depth)
    { Owner = owner; Root = root; Producer = producer; _id = id; _ownsRoot = ownsRoot;
        _sessionId = owner.Inventory.SessionId; _encounterId = owner.EncounterId; _lifecycleRevision = owner.LifecycleRevision; Depth = depth; }
    public void Dispose()
    {
        using (Owner.Profile())
        {
        if (_closed) return;
        _closed = true;
        if (_ownsRoot) Root?.Seal(); else Root?.ReleaseChild(_id);
        }
    }
}
