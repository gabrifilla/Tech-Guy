using System;

public enum ProtocolProducer { Direct, Technique, Echo, Persistent, Conductor, Detonation, ThermalShock, Overflow, BurnTick, Environment }

public readonly struct ProtocolTargetHandle : IEquatable<ProtocolTargetHandle>
{
    public ulong TargetId { get; }
    public long Generation { get; }
    public ProtocolTargetHandle(ulong targetId, long generation)
    {
        if (targetId == 0 || generation < 0) throw new ArgumentOutOfRangeException(nameof(targetId));
        TargetId = targetId; Generation = generation;
    }
    public bool Equals(ProtocolTargetHandle other) => TargetId == other.TargetId && Generation == other.Generation;
    public override bool Equals(object obj) => obj is ProtocolTargetHandle other && Equals(other);
    public override int GetHashCode() => TargetId.GetHashCode() * 397 ^ Generation.GetHashCode();
}

public static class ProtocolEligibility
{
    public static bool AppliesElements(ProtocolProducer producer) => producer >= ProtocolProducer.Direct && producer <= ProtocolProducer.Detonation;
    public static bool Conducts(ProtocolProducer producer) => AppliesElements(producer);
    public static bool Detonates(ProtocolProducer producer) => Enum.IsDefined(typeof(ProtocolProducer), producer) && producer != ProtocolProducer.Environment;
    public static bool Shocks(ProtocolProducer producer) => AppliesElements(producer);
    public static bool Overflows(ProtocolProducer producer) => producer == ProtocolProducer.Direct;
    public static bool Amplifies(ProtocolProducer producer) => producer >= ProtocolProducer.Technique && producer <= ProtocolProducer.Overflow;
}

/// <summary>One opt-in basic-derived branch. Native Protocol eligibility is deliberately unchanged.</summary>
[Flags] internal enum ProtocolBasicEffects { None = 0, Conductor = 1, Detonation = 2, Both = Conductor | Detonation }

internal sealed class ProtocolBasicLineage
{
    private readonly System.Collections.Generic.HashSet<ProtocolTargetHandle> _targets;
    private bool _conductor, _detonation;
    private readonly ProtocolBasicEffects _effects;
    internal long ParentExecutionId { get; }
    internal long RootId { get; }
    internal string ProducerId { get; }
    internal ProtocolBasicLineage(long parent, long root, System.Collections.Generic.IEnumerable<ProtocolTargetHandle> excluded, string producerId, ProtocolBasicEffects effects)
    { _effects = effects; ProducerId = producerId; ParentExecutionId = parent; RootId = root; _targets = new System.Collections.Generic.HashSet<ProtocolTargetHandle>(excluded); }
    internal bool ClaimTarget(ProtocolTargetHandle target) => _targets.Add(target);
    internal bool ClaimEmission(ProtocolProducer source, ProtocolProducer effect)
    {
        if (effect == ProtocolProducer.Conductor && source == ProtocolProducer.Technique && !_conductor && (_effects & ProtocolBasicEffects.Conductor) != 0)
        { _conductor = true; return true; }
        if (effect == ProtocolProducer.Detonation && (source == ProtocolProducer.Technique || source == ProtocolProducer.Conductor) && !_detonation && (_effects & ProtocolBasicEffects.Detonation) != 0)
        { _detonation = true; return true; }
        return false;
    }
}
