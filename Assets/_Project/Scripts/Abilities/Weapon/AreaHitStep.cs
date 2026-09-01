using System;
using UnityEngine;

[Serializable]
public class AreaHitStep
{
    [Min(0f)] public float delay;
    [Min(0f)] public float rangeOverride;
    public Vector3 boxSize = new Vector3(3f, 2f, 0f);
    public LayerMask targetLayers;
    [Min(0f)] public float damageMultiplier = 1f;
    public float bonusDamage;
    public Vector3 localOffset;

    [Header("Reaction")]
    public HitReactionType reactionType = HitReactionType.Flinch;
    public HitStrength hitStrength = HitStrength.Light;
    [Min(0f)] public float poiseDamage = 10f;
    [Min(0f)] public float stunDuration = 0.35f;
    [Min(0f)] public float knockbackForce = 4f;
    [Min(0f)] public float launchForce;
    public bool canAirJuggle = true;
    public bool canRagdoll;
}
