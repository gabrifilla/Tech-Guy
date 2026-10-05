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
