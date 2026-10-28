using UnityEngine;

/// <summary>
/// Provides authored positioning suggestions. EnemyAI alone authorizes and applies destinations, so
/// preferred distance cannot overwrite reaction, retreat cooldown, attacks or control locks.
/// </summary>
[RequireComponent(typeof(EnemyAI))]
public sealed class PreferredDistanceLayer : MonoBehaviour
{
    [Tooltip("Per-archetype preferred distances. When null or missing an entry, the CombatRole default is used.")]
    [SerializeField] private PreferredDistanceProfile _profile;

    [Tooltip("How close (meters) to the preferred distance the enemy must be before it stops repositioning, to avoid jitter.")]
    [SerializeField, Min(0.05f)] private float _arriveTolerance = 0.5f;

    private EnemyAI _ai;
    private EnemyVariant _variant;

    // Log the missing-configuration fallback once per enemy so the console isn't spammed every frame (R6.4).
    private bool _loggedMissingConfig;

    private void Awake()
    {
        _ai = GetComponent<EnemyAI>();
        _variant = GetComponent<EnemyVariant>();
    }

    /// <summary>Suggests a target without issuing movement or independently running a player loop.</summary>
    public bool TryGetPositioningTarget(out Vector3 target)
    {
        target = transform.position;
        if (!isActiveAndEnabled || _ai == null || _ai.player == null) return false;
        PreferredDistanceResolver.Resolution resolution = ResolvePreferredDistance();
        float attackRange = _ai.PositioningAttackRange;
        bool meleeApproach = !float.IsInfinity(attackRange) && attackRange > 0f;
        // Perception's melee gate measures centers in 3D. Positioning operates on the ground
        // plane, so reserve the vertical component and preserve the center-distance convention.
        float height = transform.position.y - _ai.player.position.y;
        float planarRange = meleeApproach ? Mathf.Sqrt(Mathf.Max(0f, attackRange * attackRange - height * height)) : float.PositiveInfinity;
        if (meleeApproach && planarRange <= Mathf.Epsilon) return false;
        float preferred = meleeApproach ? Mathf.Min(resolution.PreferredDistance, planarRange * .9f) : resolution.PreferredDistance;
        target = PreferredDistanceResolver.PositioningTarget(
            _ai.player.position, transform.position, preferred);
        target = ApplySeparation(target, resolution.SeparationRadius);
        if (meleeApproach)
        {
            // Separation may spread approach angles, but cannot choose a holding point outside
            // attack reach. Physical collision and NavMesh avoidance remain authoritative.
            Vector3 away = target - _ai.player.position; away.y = 0f;
            target = _ai.player.position + Vector3.ClampMagnitude(away, preferred);
            target.y = transform.position.y;
        }
        Vector3 planar = target - transform.position;
        planar.y = 0f;
        float tolerance = meleeApproach ? Mathf.Min(_arriveTolerance, planarRange - preferred) : _arriveTolerance;
        return planar.sqrMagnitude > tolerance * tolerance;
    }

    /// <summary>
    /// Resolves the preferred distance from the profile with the <see cref="CombatRole"/> fallback,
    /// anchoring the fallback to the enemy's live engagement band. Logs the missing configuration once
    /// (R6.4) when the value came from the fallback rather than an authored entry.
    /// </summary>
    private PreferredDistanceResolver.Resolution ResolvePreferredDistance()
    {
        ArchetypeId? archetype = _variant ? _variant.ArchetypeId : (ArchetypeId?)null;
        CombatRole role = _variant ? _variant.CombatRole : CombatRole.MeleePressure;
        float engagementBand = EngagementBand(archetype);

        PreferredDistanceResolver.Resolution resolution =
            PreferredDistanceResolver.Resolve(_profile, archetype, role, engagementBand);

        if (!resolution.FromProfile && !_loggedMissingConfig)
        {
            _loggedMissingConfig = true;
            string id = archetype.HasValue ? archetype.Value.ToString() : "unassigned";
            Debug.LogWarning(
                $"PreferredDistanceLayer on '{name}': no Preferred_Distance declared for archetype " +
                $"'{id}'; applying the {role} CombatRole default ({resolution.PreferredDistance:0.##}m).",
                this);
        }

        return resolution;
    }

    /// <summary>
    /// The enemy's live engagement band (meters), computed the same way <see cref="EnemyAI"/> does so
    /// the fallback distance tracks the archetype's actual band. Returns 0 when there is no agent, in
    /// which case the resolver uses its fixed per-role constants.
    /// </summary>
    private float EngagementBand(ArchetypeId? archetype)
    {
        float meleeRange = Mathf.Min(_ai.attackRange, 1.45f * Mathf.Clamp(transform.lossyScale.y, .5f, 2f) + .35f);
        return EnemyAttackPatterns.EngagementRange(_ai.AttackTraits, 0, meleeRange, archetype);
    }

    /// <summary>
    /// Pushes the positioning <paramref name="target"/> away from any nearby enemy whose collision
    /// volume would overlap this enemy's <paramref name="separationRadius"/>, so enemies tending to the
    /// same distance spread out instead of stacking (R6.2). The nudge is bounded by the overlap amount
    /// and never moves the target closer than a hair, so it cannot fight the preferred-distance target.
    /// </summary>
    private Vector3 ApplySeparation(Vector3 target, float separationRadius)
    {
        if (separationRadius <= 0f) return target;

        Vector3 push = Vector3.zero;
        Collider[] neighbors = Physics.OverlapSphere(transform.position, separationRadius * 2f);
        for (int i = 0; i < neighbors.Length; i++)
        {
            Collider neighbor = neighbors[i];
            if (neighbor == null) continue;
            if (neighbor.transform == transform || neighbor.transform.IsChildOf(transform)) continue;
            if (neighbor.GetComponentInParent<EnemyAI>() == null) continue;

            Vector3 offset = transform.position - neighbor.transform.position;
            offset.y = 0f;
            float dist = offset.magnitude;
            if (dist <= Mathf.Epsilon || dist >= separationRadius) continue;

            // Weight the push by how deeply the volumes overlap.
            push += (offset / dist) * (separationRadius - dist);
        }

        target += push;
        target.y = transform.position.y;
        return target;
    }
}
