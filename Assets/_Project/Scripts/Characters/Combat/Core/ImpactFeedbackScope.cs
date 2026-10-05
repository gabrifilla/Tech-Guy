using System;

/// <summary>Explicit local transaction shared by synchronous callbacks; no clock/global grouping.</summary>
public sealed class ImpactFeedbackScope : IDisposable
{
    private sealed class Group
    {
        public int Depth;
        public float Maximum;
        public bool Closing, Closed;
        public Action<float> Apply;
    }
    private readonly Group _group;
    private readonly bool _owner;
    private bool _disposed;
    public long Execution { get; }
    public int ImpactIndex { get; }
    public float CommonDuration { get; }
    public float Maximum => _group.Maximum;
    public bool IsClosed => _group.Closed;

    public ImpactFeedbackScope(Action<float> apply, float commonDuration = 0f, long execution = 0, int impactIndex = -1)
    {
        _group = new Group { Apply = apply, Depth = 1 }; _owner = true;
        CommonDuration = HitStop.ClampDuration(commonDuration); Execution = execution; ImpactIndex = impactIndex;
    }
    private ImpactFeedbackScope(ImpactFeedbackScope parent, float duration, long execution, int impactIndex)
    {
        if (parent._disposed || parent._group.Closing || parent._group.Closed) throw new ObjectDisposedException(nameof(ImpactFeedbackScope));
        _group = parent._group; _group.Depth++;
        CommonDuration = HitStop.ClampDuration(duration); Execution = execution; ImpactIndex = impactIndex;
    }
    public ImpactFeedbackScope Child(float? duration = null, long execution = 0, int impactIndex = -1)
        => new ImpactFeedbackScope(this, duration ?? CommonDuration, execution, impactIndex);
    public void ConfirmDamage(bool applied)
    {
        if (applied) Contribute(CommonDuration);
    }
    public void ConfirmBreak(bool occurred, float duration)
    {
        if (occurred) Contribute(duration);
    }
    private void Contribute(float duration)
    {
        if (_disposed || _group.Closed) throw new ObjectDisposedException(nameof(ImpactFeedbackScope));
        _group.Maximum = HitStopGrouping.Combine(new[] { _group.Maximum, duration });
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        if (_owner) _group.Closing = true;
        _group.Depth--;
        if (!_group.Closing || _group.Depth != 0 || _group.Closed) return;
        _group.Closed = true;
        if (_group.Maximum > 0f) _group.Apply?.Invoke(_group.Maximum);
    }
}
