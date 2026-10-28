using UnityEngine;

/// <summary>Scene-free charge and targeting contracts for the Bow slice.</summary>
public static class BowBoonModel
{
    public const float FullChargeSeconds = .8f;
    public const float MaximumHoldSeconds = 1.6f;
    public const float ShortRadius = .18f;
    public const float ChargedRadius = .7f;
    public const float PriorityConeDegrees = 50f;
    public const float BondSeconds = 6f;
    public const float SealSeconds = 3f;
    public const float MineSeconds = 6f;
    public const float MineArmSeconds = .25f;
    public const float MineRadius = 2.5f;
    public const int MaximumMines = 3;
    public static bool IsCharged(float seconds) => ReactionNumeric.IsFinite(seconds) && seconds >= FullChargeSeconds;
    public static float JudgmentScale(float seconds) => IsCharged(seconds) ? 1f : .6f;
    public static bool WithinCone(Vector3 origin, Vector3 forward, Vector3 target, float range)
    {
        if (!ReactionNumeric.IsFinite(origin) || !ReactionNumeric.IsFinite(forward) ||
            !ReactionNumeric.IsFinite(target) || !ReactionNumeric.IsFinite(range) || range <= 0) return false;
        forward.y = 0; Vector3 delta = target - origin; delta.y = 0;
        return forward.sqrMagnitude > .0001f && delta.sqrMagnitude > .0001f && delta.sqrMagnitude <= range * range &&
            Vector3.Angle(forward, delta) <= PriorityConeDegrees;
    }
}

/// <summary>A shot snapshot. Only a confirmed native arrow may mark or consume a mark.</summary>
public sealed class BowBoonShot
{
    private readonly BowBoonCombat _owner;
    private readonly long _epoch;
    private readonly bool _bond, _charged;
    public float SweepRadius { get; }
    public bool IgnorePriority => _bond;
    public bool CuttingReturn { get; }
    public bool Concentrated { get; }
    public bool IsCurrent => _owner && _owner.IsShotCurrent(_epoch);
    internal BowBoonShot(BowBoonCombat owner, long epoch, bool bond = false, bool charged = false,
        bool cuttingReturn = false, bool concentrated = false, float radius = 0)
    { _owner = owner; _epoch = epoch; _bond = bond; _charged = charged;
      CuttingReturn = cuttingReturn; Concentrated = concentrated; SweepRadius = radius; }
    public float DamageScale(Actor target, ProtocolProducer producer)
        => IsCurrent && _charged && producer == ProtocolProducer.Direct && _owner.HasConsumableMark(target) ? Concentrated ? 1.5f : 1.25f : 1f;
    public void Confirm(Actor target, long generation, ProtocolProducer producer)
    {
        if (!IsCurrent || producer != ProtocolProducer.Direct || !target || target.CombatGeneration != generation) return;
        _owner.ConfirmArrow(target, generation, _bond, _charged);
    }
    public static BowBoonShot Native(PlayerActor owner)
        => owner && owner.TryGetComponent(out BowBoonCombat combat) ? combat.NativeShot() : null;
}
