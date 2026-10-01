using UnityEngine;

/// <summary>
/// Pure, scene-free resolution of the speed and standoff math a Ranged_Enemy uses while kiting.
/// Mirrors the <see cref="PreferredDistanceResolver"/> / <c>TelegraphBeat</c> pattern so the
/// invariants "the retreat multiplier is always clamped to [0.1, 0.9]", "the standoff distance stays
/// inside the engagement band", and "the reposition target scales by a bounded margin" can be
/// property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 3.3. Requirements: 1.1, 1.3, 1.4, 1.5,
/// 2.1, 2.2, 2.3, 9.1.
/// <para>
/// This resolver only computes numbers; the <c>EnemyAI</c> movement layer feeds live state in and
/// applies the result (sets <c>agent.speed</c> while kiting, restores the chase speed on exit, and
/// derives the reposition destination along the player-to-enemy vector). Every input is defensively
/// re-clamped here so an out-of-range serialized value can never produce out-of-range output (R1.5,
/// R16.6), independent of the <c>OnValidate</c> clamping on the serialized config.
/// </para>
/// </remarks>
public static class RetreatCadence
{
    /// <summary>
    /// Lower bound for the retreat speed multiplier (R1.3/R1.5). A kiting Ranged_Enemy never retreats
    /// slower than 10% of its chase/approach speed.
    /// </summary>
    public const float MinRetreatMultiplier = 0.1f;

    /// <summary>
    /// Upper bound for the retreat speed multiplier (R1.3/R1.5). A kiting Ranged_Enemy never retreats
    /// faster than 90% of its chase/approach speed, so retreat is always slower than the chase.
    /// </summary>
    public const float MaxRetreatMultiplier = 0.9f;

    /// <summary>
    /// Clamps the configured retreat speed multiplier to the inclusive range
    /// [<see cref="MinRetreatMultiplier"/>, <see cref="MaxRetreatMultiplier"/>] (R1.3/R1.5).
    /// </summary>
    /// <param name="configured">The serialized Retreat_Speed_Multiplier, possibly out of range.</param>
    /// <returns>The multiplier clamped to the nearest valid bound.</returns>
    public static float ClampRetreatMultiplier(float configured)
    {
        return Mathf.Clamp(configured, MinRetreatMultiplier, MaxRetreatMultiplier);
    }

    /// <summary>
    /// The NavMesh agent speed to use while a Ranged_Enemy is Kiting: the chase/approach speed scaled
    /// by the clamped retreat multiplier (R1.1). When the enemy is not kiting the layer applies the
    /// unscaled chase speed directly (R1.4), so this method is only consulted while kiting.
    /// </summary>
    /// <param name="chaseSpeed">The enemy's chase/approach speed (meters per second); clamped to &gt;= 0.</param>
    /// <param name="retreatMultiplier">The configured retreat multiplier, defensively clamped to [0.1, 0.9].</param>
    /// <returns>The retreat speed, always &gt;= 0 and strictly less than the chase speed.</returns>
    public static float RetreatSpeed(float chaseSpeed, float retreatMultiplier)
    {
        float speed = Mathf.Max(0f, chaseSpeed);
        return speed * ClampRetreatMultiplier(retreatMultiplier);
    }

    /// <summary>
    /// The standoff distance a Ranged_Enemy tries to keep: the engagement band multiplied by the
    /// standoff fraction clamped to [0, 1] (R2.1). Because the fraction is at most 1, the standoff
    /// distance never exceeds the engagement band and therefore stays reachable by melee.
    /// </summary>
    /// <param name="engagementBand">The enemy's engagement band (meters); clamped to &gt;= 0.</param>
    /// <param name="standoffFraction">The configured standoff fraction, defensively clamped to [0, 1].</param>
    /// <returns>The standoff distance, always within [0, engagementBand].</returns>
    public static float StandoffDistance(float engagementBand, float standoffFraction)
    {
        float band = Mathf.Max(0f, engagementBand);
        return band * Mathf.Clamp01(standoffFraction);
    }

    /// <summary>
    /// The repositioning target distance a Ranged_Enemy restores when the player is within the
    /// standoff distance: the standoff distance multiplied by the standoff margin clamped to [1, 2]
    /// (R2.2/R2.3). The margin is never below 1, so the target always sits at or beyond the standoff
    /// distance, and never above 2 so the enemy stays reachable.
    /// </summary>
    /// <param name="standoffDistance">The resolved standoff distance; clamped to &gt;= 0.</param>
    /// <param name="standoffMargin">The configured standoff margin, defensively clamped to [1, 2].</param>
    /// <returns>The reposition target distance, always &gt;= the standoff distance.</returns>
    public static float RepositionTarget(float standoffDistance, float standoffMargin)
    {
        float standoff = Mathf.Max(0f, standoffDistance);
        return standoff * Mathf.Clamp(standoffMargin, 1f, 2f);
    }
}
