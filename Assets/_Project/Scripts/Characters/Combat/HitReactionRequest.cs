using UnityEngine;

/// <summary>
/// Describes what a single hit does to an enemy in the Stance/Stagger system.
///
/// Every hit applies <see cref="StanceDamage"/> and an immediate <see cref="ReactionType"/>
/// (Push or Stagger). When accumulated stance damage breaks the enemy's stance, the
/// <see cref="BreakEffect"/> (Stun / KnockUp / Knockback) is applied, subject to the enemy's
/// resistances and immunities. Basic attacks should use Push/Stagger with a small stance hit and
/// <see cref="StanceBreakEffect.None"/>; only specific skills should request KnockUp or Knockback.
/// </summary>
public readonly struct HitReactionRequest
{
    public Actor Attacker { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitDirection { get; }

    /// <summary>Immediate reaction: None, Push, or Stagger.</summary>
    public HitReactionType ReactionType { get; }
    /// <summary>Metres the enemy is nudged on a Push (kept small so it stays near the player).</summary>
    public float PushDistance { get; }
    /// <summary>Relative attack power for stance breaking thresholds and downgrades.</summary>
    public HitStrength Strength { get; }

    /// <summary>Amount subtracted from the enemy's stance pool on hit.</summary>
    public float StanceDamage { get; }
    /// <summary>Hard CC applied when this hit breaks the enemy's stance.</summary>
    public StanceBreakEffect BreakEffect { get; }

    /// <summary>Seconds of stun on a Stun break (or the airborne lock when knocked up).</summary>
    public float StunDuration { get; }
    /// <summary>Metres the enemy rises on a KnockUp break.</summary>
    public float KnockUpHeight { get; }
    /// <summary>Metres the enemy is thrown on a Knockback break.</summary>
    public float KnockbackDistance { get; }

    public HitReactionRequest(
        Actor attacker,
        Vector3 hitPoint,
        Vector3 hitDirection,
        HitReactionType reactionType,
        HitStrength strength,
        float stanceDamage,
        StanceBreakEffect breakEffect,
        float pushDistance = 0.35f,
        float stunDuration = 1f,
        float knockUpHeight = 2f,
        float knockbackDistance = 4f)
    {
        Attacker = attacker;
        HitPoint = hitPoint;
        HitDirection = hitDirection.sqrMagnitude > Mathf.Epsilon ? hitDirection.normalized : Vector3.forward;
        ReactionType = reactionType;
        Strength = strength;
        StanceDamage = Mathf.Max(0f, stanceDamage);
        BreakEffect = breakEffect;
        PushDistance = Mathf.Max(0f, pushDistance);
        StunDuration = Mathf.Max(0f, stunDuration);
        KnockUpHeight = Mathf.Max(0f, knockUpHeight);
        KnockbackDistance = Mathf.Max(0f, knockbackDistance);
    }
}
