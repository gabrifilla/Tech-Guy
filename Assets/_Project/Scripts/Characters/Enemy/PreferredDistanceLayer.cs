using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Thin, additive layer on top of <see cref="EnemyAI"/> that makes a non-attacking enemy tend toward
/// its archetype's preferred combat distance while keeping collision separation from its neighbors
/// (R6.1/R6.2). It never duplicates perception or the attack pipeline: it only nudges the existing
/// <see cref="NavMeshAgent"/> toward a positioning target when the enemy is idle-in-combat (in sight,
/// not winding up an attack, not control-locked). All decision math lives in the pure
/// <see cref="PreferredDistanceResolver"/> so it can be property-tested without a scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 7.1. Requirements: 6.1, 6.2, 6.4.
/// <para>
/// The preferred distance is authored data (<see cref="PreferredDistanceProfile"/>) keyed by
/// <see cref="ArchetypeId"/>. When the enemy's archetype has no entry, the resolver falls back to the
/// enemy's <see cref="CombatRole"/> default (always &gt; 0) and this layer logs the absence exactly
/// once with a stable identifier so the gap is traceable (R6.4), without interrupting behavior.
/// </para>
/// </remarks>
[RequireComponent(typeof(EnemyAI))]
public sealed class PreferredDistanceLayer : MonoBehaviour
{
    [Tooltip("Per-archetype preferred distances. When null or missing an entry, the CombatRole default is used.")]
    [SerializeField] private PreferredDistanceProfile _profile;

    [Tooltip("How close (meters) to the preferred distance the enemy must be before it stops repositioning, to avoid jitter.")]
    [SerializeField, Min(0.05f)] private float _arriveTolerance = 0.5f;

    private EnemyAI _ai;
    private NavMeshAgent _agent;
    private CombatReactionController _reaction;
    private EnemyVariant _variant;
    private Actor _owner;

    // Log the missing-configuration fallback once per enemy so the console isn't spammed every frame (R6.4).
    private bool _loggedMissingConfig;

    private void Awake()
    {
        _ai = GetComponent<EnemyAI>();
        _agent = GetComponent<NavMeshAgent>();
        _reaction = GetComponent<CombatReactionController>();
        _variant = GetComponent<EnemyVariant>();
        _owner = GetComponent<Actor>();
    }

    private void Update()
    {
        if (Time.timeScale <= 0f) return;
        if (!ShouldReposition()) return;

        Transform player = _ai.player;
        PreferredDistanceResolver.Resolution resolution = ResolvePreferredDistance();

        Vector3 target = PreferredDistanceResolver.PositioningTarget(
            player.position, transform.position, resolution.PreferredDistance);

        // Separation: keep the target off any neighbor's collision volume so enemies don't stack. This
        // is a small, bounded nudge on the already-computed positioning target, not a second solver.
        target = ApplySeparation(target, resolution.SeparationRadius);

        // Only reposition when meaningfully off the preferred distance, so the enemy settles instead of
        // jittering. Movement goes exclusively through the existing NavMeshAgent (no raw transform move).
        Vector3 planar = target - transform.position;
        planar.y = 0f;
        if (planar.magnitude <= _arriveTolerance) return;

        if (NavMesh.SamplePosition(target, out NavMeshHit hit, resolution.PreferredDistance + 1f, NavMesh.AllAreas))
        {
            _agent.SetDestination(hit.position);
        }
    }

    /// <summary>
    /// True only when the enemy is idle-in-combat: alive, has a player and a usable agent, is not
    /// control-locked (stunned/airborne), and is not currently winding up an attack. This defers to
    /// <see cref="EnemyAI"/> for attack state so the layer never fights the attack pipeline (R6.2 "while
    /// not executing an attack").
    /// </summary>
    private bool ShouldReposition()
    {
        if (_ai == null || _agent == null) return false;
        if (!_agent.enabled || !_agent.isOnNavMesh || _agent.isStopped) return false;
        if (_ai.player == null) return false;
        if (_owner && _owner.IsDead) return false;
        if (_reaction && _reaction.IsControlLocked) return false;
        if (_ai.IsWindingUp) return false;

        // Only hold position once the player is actually in sight; out of sight, EnemyAI's patrol owns
        // movement and we must not override it.
        return _ai.playerInSightRange;
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
