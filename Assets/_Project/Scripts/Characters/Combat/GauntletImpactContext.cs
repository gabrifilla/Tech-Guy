using System;

public enum GauntletImpactOrigin { Direct = 0, Technique = 1, Echo = 2, Persistent = 3, External = 4 }

/// <summary>Identity captured at acceptance. Never derives provenance from current equipment.</summary>
public sealed class GauntletImpactContext
{
    public GauntletExecutionSnapshot Snapshot { get; }
    public Actor Owner { get; }
    public RunWeaponFamily Family { get; }
    public long Epoch { get; }
    public long Execution { get; }
    public long RootExecution { get; }
    public GauntletActionSlot Slot { get; }
    public bool Shock { get; }
    public GauntletImpactOrigin Origin { get; }
    public string ProducerId { get; }
    public bool EnergyEligible { get; }
    public bool BelongsToR => Slot == GauntletActionSlot.R;
    public int MomentumRank { get; }
    public int AsuraFistRank { get; }
    public int ReserveRank { get; }
    public int ImpactIndex { get; }
    public ulong TargetId { get; }
    public long TargetGeneration { get; }

    internal GauntletImpactContext(Actor owner, RunWeaponFamily family, long epoch, long execution,
        long root, GauntletActionSlot slot, bool shock, GauntletImpactOrigin origin, string producer,
        bool eligible, int momentum, int fist, int reserve, int impact = -1, ulong target = 0, long targetGeneration = 0, GauntletExecutionSnapshot snapshot = null)
    {
        Snapshot = snapshot;
        Owner = owner; Family = family; Epoch = epoch; Execution = execution; RootExecution = root;
        Slot = slot; Shock = shock; Origin = origin; ProducerId = producer; EnergyEligible = eligible;
        MomentumRank = momentum; AsuraFistRank = fist; ReserveRank = reserve;
        ImpactIndex = impact; TargetId = target; TargetGeneration = targetGeneration;
    }

    public GauntletImpactContext ForImpact(int index, int targetId, long targetGeneration) => ForImpact(index, checked((ulong)targetId), targetGeneration);

    public GauntletImpactContext ForImpact(int index, ulong targetId, long targetGeneration)
    {
        if (index < 0 || targetId == 0 || targetGeneration < 0) throw new ArgumentOutOfRangeException(nameof(index));
        return new GauntletImpactContext(Owner, Family, Epoch, Execution, RootExecution, Slot, Shock,
            Origin, ProducerId, EnergyEligible, MomentumRank, AsuraFistRank, ReserveRank, index, targetId, targetGeneration, Snapshot);
    }
}

public readonly struct ConfirmedHpReceipt
{
    public GauntletImpactContext Context { get; }
    public float Damage { get; }
    public float SourceDamage { get; }
    public ConfirmedHpReceipt(GauntletImpactContext context, float actualDamage, float? sourceDamage = null)
    {
        if (context == null || context.ImpactIndex < 0 || context.TargetId == 0) throw new ArgumentException("An impact context is required.");
        if (float.IsNaN(actualDamage) || float.IsInfinity(actualDamage) || actualDamage <= 0f)
            throw new ArgumentOutOfRangeException(nameof(actualDamage));
        float source = sourceDamage ?? actualDamage;
        if (!ReactionNumeric.IsFinite(source) || source <= 0f) throw new ArgumentOutOfRangeException(nameof(sourceDamage));
        Context = context; Damage = actualDamage; SourceDamage = source;
    }
}

public readonly struct ConfirmedBreakReceipt
{
    public GauntletImpactContext Context { get; }
    public long BreakId { get; }
    public ConfirmedBreakReceipt(GauntletImpactContext context, long breakId)
    {
        if (context == null || context.ImpactIndex < 0 || context.TargetId == 0) throw new ArgumentException("An impact context is required.");
        if (breakId <= 0) throw new ArgumentOutOfRangeException(nameof(breakId));
        Context = context; BreakId = breakId;
    }
}

