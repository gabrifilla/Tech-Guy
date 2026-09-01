using UnityEngine;

public readonly struct HitReactionRequest
{
    public HitReactionRequest(
        Actor attacker,
        Vector3 hitPoint,
        Vector3 hitDirection,
        HitReactionType reactionType,
        HitStrength strength,
        float poiseDamage,
        float stunDuration,
        float knockbackForce,
        float launchForce,
        bool canAirJuggle,
        bool canRagdoll)
    {
        Attacker = attacker;
        HitPoint = hitPoint;
        HitDirection = hitDirection.sqrMagnitude > Mathf.Epsilon ? hitDirection.normalized : Vector3.forward;
        ReactionType = reactionType;
        Strength = strength;
        PoiseDamage = Mathf.Max(0f, poiseDamage);
        StunDuration = Mathf.Max(0f, stunDuration);
        KnockbackForce = Mathf.Max(0f, knockbackForce);
        LaunchForce = Mathf.Max(0f, launchForce);
        CanAirJuggle = canAirJuggle;
        CanRagdoll = canRagdoll;
    }

    public Actor Attacker { get; }
    public Vector3 HitPoint { get; }
    public Vector3 HitDirection { get; }
    public HitReactionType ReactionType { get; }
    public HitStrength Strength { get; }
    public float PoiseDamage { get; }
    public float StunDuration { get; }
    public float KnockbackForce { get; }
    public float LaunchForce { get; }
    public bool CanAirJuggle { get; }
    public bool CanRagdoll { get; }
}
