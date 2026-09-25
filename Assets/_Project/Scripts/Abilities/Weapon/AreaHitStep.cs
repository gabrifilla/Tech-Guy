using System;
using UnityEngine;

public enum AreaHitShape
{
    Box,
    Sphere
}

[Serializable]
public class AreaHitStep
{
    [Min(0f)] public float delay;
    [Min(0f)] public float rangeOverride;
    public AreaHitShape hitShape = AreaHitShape.Box;
    public Vector3 boxSize = new Vector3(3f, 2f, 0f);
    [Min(0f)] public float sphereRadius = 1.5f;
    public LayerMask targetLayers;
    [Min(0f)] public float damageMultiplier = 1f;
    public float bonusDamage;
    public Vector3 localOffset;

    [Header("Reaction")]
    [Tooltip("Immediate reaction: Push (small nudge) or Stagger (short interrupt). Hard CC comes from a Stance Break.")]
    public HitReactionType reactionType = HitReactionType.Stagger;
    public HitStrength hitStrength = HitStrength.Light;
    [Tooltip("Metres the enemy is nudged on Push.")]
    [Min(0f)] public float pushDistance = 0.35f;

    [Header("Stance")]
    [Tooltip("Stance damage dealt by this hit. When the enemy's stance breaks, the effect below triggers.")]
    [Min(0f)] public float stanceDamage = 12f;
    [Tooltip("Hard CC applied when this hit breaks the enemy's stance (subject to enemy resistances).")]
    public StanceBreakEffect breakEffect = StanceBreakEffect.None;
    [Tooltip("Stun/airborne seconds on a Stun or KnockUp break.")]
    [Min(0f)] public float stunDuration = 1f;
    [Tooltip("Rise height in metres on a KnockUp break.")]
    [Min(0f)] public float knockUpHeight = 2f;
    [Tooltip("Throw distance in metres on a Knockback break (reserve for skills meant to launch enemies away).")]
    [Min(0f)] public float knockbackDistance = 4f;
}
