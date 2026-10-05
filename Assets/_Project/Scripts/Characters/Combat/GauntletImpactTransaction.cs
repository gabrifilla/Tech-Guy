using System;
using System.Collections.Generic;

/// <summary>Stages one target's confirmation and its legacy notifications until the gain commits.</summary>
public sealed class GauntletImpactTransaction : IDisposable
{
    private readonly GauntletEnergyPolicy _policy;
    private readonly Action _release;
    private readonly List<Action> _notifications = new List<Action>();
    private readonly List<GauntletImpactTransaction> _children = new List<GauntletImpactTransaction>();
    private readonly List<ProtocolRootContext> _protocolBatches = new List<ProtocolRootContext>();
    private readonly GauntletImpactTransaction _parent;
    private float _hp;
    private long _break;
    private bool _disposed;
    public GauntletImpactContext Context { get; }
    public bool HasBreak => _break != 0;
    public bool IsCollecting => !_disposed;
    public GauntletImpactTransaction(GauntletEnergyPolicy policy, GauntletImpactContext context, Action release,
        GauntletImpactTransaction parent = null)
    { _policy = policy; Context = context; _release = release; _parent = parent; }
    public void ConfirmHp(float actual) { if (!_disposed && actual > 0f) _hp = actual; }
    internal void HoldProtocolBatch(ProtocolRootContext root)
    { if (!_disposed && root != null && root.BeginBatch()) _protocolBatches.Add(root); }
    public void ConfirmBreak(long sequence) { if (!_disposed && sequence > 0) _break = sequence; }
    public void Notify(Action notification) { if (!_disposed) _notifications.Add(notification); }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try
        {
            if (_parent != null)
            {
                _parent._children.Add(this);
                _parent._notifications.AddRange(_notifications);
                _parent._protocolBatches.AddRange(_protocolBatches); _protocolBatches.Clear();
                return;
            }
            var gains = new List<Action>();
            _policy.ConfirmImpact(Context, _hp, _break, gains.Add);
            foreach (var child in _children)
                _policy.ConfirmImpact(child.Context, child._hp, child._break, gains.Add);
            foreach (var gain in gains) gain();
            foreach (var notification in _notifications) notification();
        }
        finally
        {
            foreach (var root in _protocolBatches) root.EndBatch();
            _protocolBatches.Clear();
            foreach (var child in _children) _policy.Close(child.Context);
            _release?.Invoke();
        }
    }
}
