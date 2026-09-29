using UnityEngine;

/// <summary>
/// Pure, scene-free resolution of an archetype's preferred combat distance and the positioning
/// target math that keeps a non-attacking enemy at that distance. Extracted from
/// <see cref="PreferredDistanceLayer"/> so the invariants "a resolved preferred distance is always
/// &gt; 0" and "the positioning target tends toward the preferred distance" (Property 17,
/// Requirements 6.1/6.2/6.4) can be property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 7.1. Requirements: 6.1, 6.2, 6.4.
/// <para>
/// This resolver never invents a new numeric channel: a declared <see cref="PreferredDistanceProfile"/>
/// entry wins; otherwise it falls back to the <see cref="CombatRole"/> default (always &gt; 0), which
/// is derived from the same engagement bands <c>EnemyAI</c>/<c>EnemyAttackPatterns</c> already use. The
/// engagement/standoff numbers passed in by the layer let a caller anchor the fallback to the enemy's
/// live band so melee enemies tend close and ranged enemies tend to their kite distance.
/// </para>
/// </remarks>
public static class PreferredDistanceResolver
{
    /// <summary>
    /// Absolute floor for any resolved preferred distance. Guarantees the &gt; 0 invariant (R6.1)
    /// even if a profile author leaves a zero/negative value or a role default were misconfigured.
    /// </summary>
    public const float MinPreferredDistance = 0.1f;

    /// <summary>Default separation radius (meters) when a profile declares a non-positive one.</summary>
    public const float DefaultSeparationRadius = 0.75f;

    /// <summary>
    /// The resolved outcome of a preferred-distance lookup: the distance the enemy tends toward
    /// (always &gt; 0), the collision-separation radius, and whether the value came from an authored
    /// per-archetype entry (<see cref="FromProfile"/> true) or the <see cref="CombatRole"/> fallback.
    /// </summary>
    public readonly struct Resolution
    {
        public readonly float PreferredDistance;
        public readonly float SeparationRadius;
        public readonly bool FromProfile;

        public Resolution(float preferredDistance, float separationRadius, bool fromProfile)
        {
            PreferredDistance = Mathf.Max(MinPreferredDistance, preferredDistance);
            SeparationRadius = Mathf.Max(0f, separationRadius);
            FromProfile = fromProfile;
        }
    }

    /// <summary>
    /// The per-<see cref="CombatRole"/> default preferred distance used when an archetype declares no
    /// value (R6.4). Melee-pressure roles tend close; ranged/denial/control roles tend to a mid band;
    /// support/priority roles hang back. Every branch returns a value strictly &gt; 0 (R6.1). The
    /// caller may pass its live engagement band so the fallback tracks the enemy's actual band; a
    /// non-positive band falls back to the fixed role constant.
    /// </summary>
    public static float RoleDefaultDistance(CombatRole role, float engagementBand)
    {
        float band = engagementBand > 0f ? engagementBand : 0f;
        switch (role)
        {
            case CombatRole.MeleePressure:
            case CombatRole.ComboFodder:
            case CombatRole.SwarmFuel:
                // Tend to just inside melee reach so they crowd the front line.
                return band > 0f ? Mathf.Max(MinPreferredDistance, band * 0.9f) : 2.0f;
            case CombatRole.PlayerDisplacement:
                // Close enough to commit a shove/charge but not overlapping.
                return band > 0f ? Mathf.Max(MinPreferredDistance, band * 0.8f) : 2.5f;
            case CombatRole.RangedPressure:
            case CombatRole.ProjectileDenial:
                // Hold a firing lane a good way out from the player.
                return band > 0f ? Mathf.Max(MinPreferredDistance, band * 0.75f) : 9.0f;
            case CombatRole.TerritoryControl:
                // Stay back to lob/zone without being reached.
                return band > 0f ? Mathf.Max(MinPreferredDistance, band * 0.85f) : 10.0f;
            case CombatRole.AllySupport:
            case CombatRole.PriorityThreat:
                // Priority/support enemies keep their distance behind the line.
                return band > 0f ? Mathf.Max(MinPreferredDistance, band * 0.95f) : 12.0f;
            default:
                return band > 0f ? Mathf.Max(MinPreferredDistance, band) : 6.0f;
        }
    }

    /// <summary>
    /// Resolves the preferred distance for an archetype: prefers a declared
    /// <see cref="PreferredDistanceProfile"/> entry, otherwise falls back to the
    /// <see cref="CombatRole"/> default (R6.4). The result's <see cref="Resolution.PreferredDistance"/>
    /// is always &gt; 0 (R6.1). When <paramref name="profile"/> is null or has no entry for
    /// <paramref name="archetype"/>, <see cref="Resolution.FromProfile"/> is false so the caller can
    /// log the missing configuration exactly once.
    /// </summary>
    /// <param name="profile">The per-archetype distance profile asset, or null when none is assigned.</param>
    /// <param name="archetype">The enemy's archetype id, or null for a non-archetype enemy.</param>
    /// <param name="role">The enemy's primary combat role, used for the fallback distance.</param>
    /// <param name="engagementBand">The enemy's live engagement band (meters) to anchor the fallback; pass 0 to use fixed role constants.</param>
    public static Resolution Resolve(PreferredDistanceProfile profile, ArchetypeId? archetype, CombatRole role, float engagementBand)
    {
        if (profile != null && archetype is ArchetypeId id &&
            profile.TryGetPreferredDistance(id, out float declared, out float separation))
        {
            return new Resolution(declared, separation, fromProfile: true);
        }

        return new Resolution(RoleDefaultDistance(role, engagementBand), DefaultSeparationRadius, fromProfile: false);
    }

    /// <summary>
    /// Computes the ground-plane positioning target for a non-attacking enemy: the point on the ray
    /// from the player toward the enemy that sits exactly at the resolved preferred distance (R6.2).
    /// The result always lies at <paramref name="preferredDistance"/> from the player, so the enemy
    /// "tends toward" its preferred distance whether it is currently too close or too far (Property 17).
    /// Y is preserved from the enemy so the target stays on the enemy's own plane; when the enemy sits
    /// on top of the player, an arbitrary stable direction is chosen to avoid a zero-length ray.
    /// </summary>
    /// <param name="playerPosition">The player's world position.</param>
    /// <param name="enemyPosition">The enemy's current world position.</param>
    /// <param name="preferredDistance">The resolved preferred distance (assumed &gt; 0).</param>
    /// <returns>The world-space point the enemy should move toward to hold its preferred distance.</returns>
    public static Vector3 PositioningTarget(Vector3 playerPosition, Vector3 enemyPosition, float preferredDistance)
    {
        float distance = Mathf.Max(MinPreferredDistance, preferredDistance);

        Vector3 away = enemyPosition - playerPosition;
        away.y = 0f;
        float planar = away.magnitude;
        Vector3 direction = planar > Mathf.Epsilon ? away / planar : Vector3.forward;

        Vector3 target = playerPosition + direction * distance;
        target.y = enemyPosition.y;
        return target;
    }
}
