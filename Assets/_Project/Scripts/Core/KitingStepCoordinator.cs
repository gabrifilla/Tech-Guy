using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Kiting Step boon (gauntlet-boon-playstyle-overhaul R8, "Disparo em recuo"). A run-scoped coordinator
/// added to the player by <c>RunBoons</c> while the equipped family is Bow (R8.5).
///
/// When the player fires a Bow basic WHILE retreating from the nearest enemy, this grants a short
/// navmesh-routed reposition impulse (R8.1/R8.3). The framework-agnostic decision — "is the player
/// moving away?" and "how far should the impulse push?" — lives in the pure <see cref="KitingImpulse"/>
/// helper; this MonoBehaviour only observes the basic-attack event, finds the nearest enemy, and moves
/// the player's own <see cref="NavMeshAgent"/>. It never teleports: the destination is clamped to the
/// walkable mesh via <see cref="NavMeshAgent.Raycast"/> before <see cref="NavMeshAgent.Move"/>, mirroring
/// the <c>_agent.Raycast</c>/<c>_agent.Move</c> pattern already used in <c>BreakerGauntletCombat</c>, so
/// the impulse can never cross solid scenery (R8.3).
///
/// The per-impulse cooldown (<see cref="KitingImpulse.Cooldown"/>) is enforced here through
/// <c>_nextImpulseAt</c> so a burst of basics cannot chain impulses every frame (R8.2).
///
/// Lifecycle: the <see cref="CharControlScript.BasicAttackPerformed"/> subscription is dropped by
/// <see cref="OnDestroy"/> when the component is torn down (and the coordinator GameObject dies with the
/// run), so a previous run's boon never repositions the player in a later run.
/// </summary>
public sealed class KitingStepCoordinator : MonoBehaviour
{
    // Radius in which the coordinator looks for the nearest enemy to judge the retreat direction. Wide
    // enough to cover ranged kiting without pulling in enemies on the far side of the arena.
    private const float NearestEnemySearchRadius = 14f;

    // Reused non-alloc buffer for the nearest-enemy OverlapSphere, mirroring ArsenalCombat/
    // BreakerGauntletCombat's enemy-search pattern so no per-hit garbage is produced.
    private static readonly Collider[] Overlap = new Collider[32];

    private PlayerActor _player;
    private CharControlScript _controls;    // exposes the basic-attack event + the current move direction
    private NavMeshAgent _agent;            // the PLAYER's agent (repositions the player — R8.3)
    private int _rank;
    private float _nextImpulseAt;           // R8.2: per-impulse cooldown gate

    /// <summary>
    /// Binds this coordinator to the run. Safe to call again when the boon rank increases: the previous
    /// subscription is dropped first so no handler is registered twice, then re-added at the new rank.
    /// </summary>
    public void Configure(PlayerActor player, CharControlScript controls, NavMeshAgent agent, int rank)
    {
        Unsubscribe();
        _player = player;
        _controls = controls;
        _agent = agent;
        _rank = Mathf.Max(0, rank);
        Subscribe();
    }

    // CharControlScript.BasicAttackPerformed already exists; only the Bow basic reaches here because the
    // coordinator is only created WHILE the family is Bow (R8.5).
    private void Subscribe() { if (_controls) _controls.BasicAttackPerformed += OnBasicAttack; }

    private void Unsubscribe() { if (_controls) _controls.BasicAttackPerformed -= OnBasicAttack; }

    private void OnBasicAttack()
    {
        if (_rank <= 0 || !_player || Time.time < _nextImpulseAt) return;   // R8.2 cooldown

        Vector3 moveDir = _controls ? _controls.CurrentMoveDirection : Vector3.zero;
        Actor nearest = FindNearestEnemy();
        if (!nearest) return;

        Vector3 toEnemy = nearest.transform.position - _player.transform.position;
        if (!KitingImpulse.ShouldReposition(moveDir, toEnemy)) return;      // R8.1/R8.4 (away-only)

        _nextImpulseAt = Time.time + KitingImpulse.Cooldown;

        Vector3 origin = _player.transform.position;
        Vector3 destination = origin + moveDir.normalized * KitingImpulse.Distance(_rank);
        if (_agent && _agent.enabled && _agent.isOnNavMesh)
        {
            if (_agent.Raycast(destination, out NavMeshHit edge)) destination = edge.position; // R8.3
            _agent.Move(destination - origin);
        }
    }

    // Nearest living enemy within the search radius, excluding the player. Mirrors the project's
    // OverlapSphere + GetComponentInParent<Actor> enemy-search pattern (ArsenalCombat/BreakerGauntletCombat)
    // rather than a scene lookup, so no FindObjectOfType/GameObject.Find is used.
    private Actor FindNearestEnemy()
    {
        Vector3 center = _player.transform.position;
        int count = Physics.OverlapSphereNonAlloc(center, NearestEnemySearchRadius, Overlap,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        Actor best = null;
        float nearestSqr = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            Actor candidate = Overlap[i] ? Overlap[i].GetComponentInParent<Actor>() : null;
            if (!candidate || candidate == _player || candidate.IsDead || !candidate.isActiveAndEnabled) continue;
            Vector3 delta = candidate.transform.position - center; delta.y = 0f;
            float sqr = delta.sqrMagnitude;
            if (sqr < nearestSqr) { nearestSqr = sqr; best = candidate; }
        }
        return best;
    }

    private void OnDestroy() => Unsubscribe();
}
