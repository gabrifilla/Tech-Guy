using UnityEngine;

/// <summary>
/// Pure, scene-free model of the Mirror's frontal-arc damage reduction. Extracted from the
/// <see cref="FrontalReflector"/> MonoBehaviour so the arc / reflection math can be property-tested
/// without a live Unity scene (task 8.9, Property 12).
///
/// The Mirror protects a fixed frontal arc (90–180°) centred on its forward direction. Given the
/// direction an incoming hit approaches from, whether the hit is a player projectile, whether it is
/// an area attack, and whether the shield is currently active, this returns the fraction of the
/// incoming damage that should actually be applied to the Mirror:
/// <list type="bullet">
///   <item>A player projectile arriving from within the frontal arc while the shield is active is
///   reduced to at most 25% (R18.3).</item>
///   <item>A hit from outside the arc applies full damage (R18.4).</item>
///   <item>An area attack applies full damage regardless of the arc (R18.5).</item>
///   <item>While the shield is inactive (stance-broken / control-locked) every hit applies full
///   damage (R18.6/R18.7).</item>
/// </list>
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.8. Requirements: 18.2, 18.3, 18.4, 18.5, 18.6, 18.7.</remarks>
public static class FrontalArcReflection
{
    /// <summary>Smallest protected arc the design allows (R18.2).</summary>
    public const float MinArcDegrees = 90f;

    /// <summary>Largest protected arc the design allows (R18.2).</summary>
    public const float MaxArcDegrees = 180f;

    /// <summary>The most damage a reflected frontal projectile may deal, as a fraction (R18.3).</summary>
    public const float MaxReflectedFraction = 0.25f;

    /// <summary>
    /// Clamps a configured arc to the design-allowed range of 90–180 degrees (R18.2). Values outside
    /// the range are pinned to the nearest bound rather than rejected, so a misconfigured Inspector
    /// value still yields a valid protected arc.
    /// </summary>
    public static float ClampArc(float arcDegrees) => Mathf.Clamp(arcDegrees, MinArcDegrees, MaxArcDegrees);

    /// <summary>
    /// True when a hit approaching along <paramref name="approachDirection"/> lies inside the frontal
    /// arc of a Mirror facing <paramref name="mirrorForward"/>. The approach direction points from the
    /// Mirror toward where the hit is coming from (i.e. the attacker's bearing). A hit is frontal when
    /// the angle between the Mirror's forward and that bearing is at most half the protected arc.
    /// Degenerate directions (near-zero vectors) are treated as not frontal so they take full damage.
    /// </summary>
    /// <param name="mirrorForward">The Mirror's forward direction (need not be normalized).</param>
    /// <param name="approachDirection">Direction from the Mirror toward the incoming hit's source (need not be normalized).</param>
    /// <param name="arcDegrees">The protected arc width in degrees; clamped to 90–180.</param>
    public static bool IsWithinArc(Vector3 mirrorForward, Vector3 approachDirection, float arcDegrees)
    {
        // Flatten to the ground plane: the arc is defined about the Mirror's facing, not its pitch.
        mirrorForward.y = 0f;
        approachDirection.y = 0f;

        if (mirrorForward.sqrMagnitude < 1e-6f || approachDirection.sqrMagnitude < 1e-6f)
            return false;

        float halfArc = ClampArc(arcDegrees) * 0.5f;
        float angle = Vector3.Angle(mirrorForward, approachDirection);
        return angle <= halfArc + 1e-4f;
    }

    /// <summary>
    /// Returns the fraction of incoming damage (in <c>[0, 1]</c>) that should be applied to the Mirror
    /// for a hit approaching along <paramref name="approachDirection"/>.
    ///
    /// Full damage (fraction 1) applies when: the shield is inactive, the hit is an area attack, the
    /// hit is not a player projectile, or the hit comes from outside the frontal arc. Only a player
    /// projectile arriving from within the arc while the shield is active is reduced, and then to at
    /// most <see cref="MaxReflectedFraction"/>.
    /// </summary>
    /// <param name="shieldActive">Whether the frontal shield is currently active (false while stance-broken / control-locked).</param>
    /// <param name="isPlayerProjectile">Whether the incoming hit is a player projectile (only projectiles are reflected).</param>
    /// <param name="isAreaAttack">Whether the incoming hit is an area attack (area always applies full damage, R18.5).</param>
    /// <param name="mirrorForward">The Mirror's forward direction.</param>
    /// <param name="approachDirection">Direction from the Mirror toward the incoming hit's source.</param>
    /// <param name="arcDegrees">The protected arc width in degrees; clamped to 90–180.</param>
    public static float DamageFraction(
        bool shieldActive,
        bool isPlayerProjectile,
        bool isAreaAttack,
        Vector3 mirrorForward,
        Vector3 approachDirection,
        float arcDegrees)
    {
        if (!shieldActive) return 1f;          // stance-broken / control-locked: no protection (R18.6/R18.7)
        if (isAreaAttack) return 1f;           // area attacks ignore the arc (R18.5)
        if (!isPlayerProjectile) return 1f;    // only player projectiles are reflected (R18.3/R18.4)
        if (!IsWithinArc(mirrorForward, approachDirection, arcDegrees)) return 1f; // outside the arc: full (R18.4)

        return MaxReflectedFraction;           // frontal player projectile: reduced to ≤25% (R18.3)
    }
}
