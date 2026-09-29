using System.Collections;
using UnityEngine;

/// <summary>
/// Drives the Hazard_Caster's territory-control loop: on a serialized cadence it runs the
/// <see cref="EnemyAttackKind.HazardPlace"/> attack — a placement telegraph of at least 0.25s
/// (R11.3, owned by <see cref="EnemyCombatActions"/>) — and, once that telegraph completes, it
/// activates a <see cref="HazardZone"/> of the per-instance configured variant at the telegraphed
/// position (R11.4). The behavior is the piece that owns the zone lifecycle; the pipeline case only
/// shows the pre-activation windup and never touches a zone, so R11.4 ("the behavior activates the
/// zone") lives here.
///
/// Combat_Role Territory-Control (R11.1) is declared on the archetype asset; this behavior only drives
/// the zone placement. The variant (fire / electric / slow) is authored per instance through a
/// serialized <see cref="HazardZone"/> prefab reference (R11.2) rather than a discovered singleton,
/// matching AGENTS.md (serialized references, no <c>FindObjectOfType</c>).
///
/// The MonoBehaviour stays thin. It owns only the cadence timer and the "telegraph complete → spawn +
/// Activate" wiring, driven by a coroutine gated on <see cref="ArchetypeBehavior.CanAct"/> — never
/// heavy per-frame <see cref="Update"/> logic. When it cannot act (dead, off-NavMesh, or
/// control-locked) it neither telegraphs nor places a zone, and it waits for control to return before
/// the next cast.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.3. Requirements: 11.1, 11.2, 11.3, 11.4.</remarks>
[DisallowMultipleComponent]
public sealed class HazardCasterBehavior : ArchetypeBehavior
{
    [Header("Cadence")]
    [Tooltip("Seconds between hazard-zone placements. A serialized interval timer drives the loop; " +
             "no heavy per-frame Update logic. Casting is gated on CanAct.")]
    [SerializeField, Min(0.25f)] private float _castInterval = 6f;

    [Tooltip("Optional delay before the first placement so a freshly spawned caster does not " +
             "telegraph on its very first frame.")]
    [SerializeField, Min(0f)] private float _initialDelay = 1f;

    [Header("Variant (R11.2)")]
    [Tooltip("The HazardZone prefab this caster places. Authoring the fire / electric / slow variant " +
             "per instance is done by assigning the matching HazardZone prefab here — the ZoneKind and " +
             "its tuning live on the prefab (serialized reference, no FindObjectOfType).")]
    [SerializeField] private HazardZone _zonePrefab;

    private EnemyCombatActions _combat;
    private Coroutine _loop;

    /// <summary>
    /// Resolves the extra dependencies this behavior needs on top of the base
    /// (<see cref="Actor"/> / <see cref="EnemyAI"/> / agent / reaction), then validates them.
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        _combat = GetComponent<EnemyCombatActions>();
    }

    private void OnEnable() => _loop = StartCoroutine(CastLoop());

    private void OnDisable()
    {
        if (_loop != null) { StopCoroutine(_loop); _loop = null; }
    }

    /// <summary>
    /// The cadence loop. It waits the initial delay, then repeatedly waits the cast interval and, when
    /// <see cref="ArchetypeBehavior.CanAct"/> is true and a target is in range, runs one placement.
    /// The wait is a single timed yield per cycle rather than per-frame polling, so no heavy Update
    /// work is introduced. If the caster cannot act at the moment its timer elapses (dead, off-NavMesh,
    /// or control-locked), the cycle is skipped and it retries on the next interval — nothing is
    /// telegraphed or placed while control-locked (R20 via the gate).
    /// </summary>
    private IEnumerator CastLoop()
    {
        if (_initialDelay > 0f) yield return new WaitForSeconds(_initialDelay);

        while (true)
        {
            yield return new WaitForSeconds(Mathf.Max(0.25f, _castInterval));

            if (!CanAct || _combat == null || _zonePrefab == null) continue;

            Actor target = ResolveTarget();
            if (!target || target.IsDead) continue;

            yield return PlaceHazard(target);
        }
    }

    /// <summary>
    /// Runs one hazard placement: it drives the shared <see cref="EnemyAttackKind.HazardPlace"/>
    /// pipeline (the ≥0.25s placement telegraph, R11.3) and, only if that telegraph completed without
    /// being cancelled, spawns the configured <see cref="HazardZone"/> at the telegraphed position and
    /// calls <see cref="HazardZone.Activate"/> (R11.4). The <see cref="ArchetypeBehavior.CanAct"/> gate
    /// is folded into the pipeline's <c>canAttack</c> predicate so a control-lock mid-windup cancels the
    /// placement and no zone is created.
    /// </summary>
    private IEnumerator PlaceHazard(Actor target)
    {
        // The pipeline reports the telegraphed footprint centre via AttackCenter; capture the resolved
        // world position after the windup so the zone lands exactly where the warning ring was drawn.
        yield return _combat.Perform(EnemyAttackKind.HazardPlace, target, 0f, () => CanAct);

        // Only activate when the placement telegraph fully completed for THIS HazardPlace (R11.4). A
        // cancelled or interrupted windup (control-lock, target lost) leaves Completed false → no zone.
        if (!_combat.Completed || _combat.CurrentKind != EnemyAttackKind.HazardPlace) yield break;

        Vector3 zonePoint = _combat.AttackCenter;
        HazardZone zone = Instantiate(_zonePrefab, zonePoint, Quaternion.identity);
        zone.Activate();
    }

    /// <summary>
    /// Resolves the player target through the already-resolved <see cref="EnemyAI"/> reference (its
    /// serialized <c>player</c> transform), reusing the same target the attack pipeline uses rather than
    /// discovering one independently.
    /// </summary>
    private Actor ResolveTarget()
    {
        if (!Ai || !Ai.player) return null;
        Actor actor = Ai.player.GetComponentInParent<Actor>();
        return actor ? actor : Ai.player.GetComponentInChildren<Actor>();
    }

    /// <summary>
    /// Logs an error naming every required dependency that failed to resolve, so a misconfigured
    /// Hazard_Caster prefab is diagnosed at spawn instead of silently placing nothing (R1.6). Extends
    /// the base validation with this behavior's own references.
    /// </summary>
    protected override void LogMissingDependencies()
    {
        if (!GetComponent<Actor>())
            Debug.LogError($"{name}: HazardCasterBehavior requires an Actor component.", this);
        if (!GetComponent<EnemyAI>())
            Debug.LogError($"{name}: HazardCasterBehavior requires an EnemyAI component.", this);
        if (!GetComponent<EnemyCombatActions>())
            Debug.LogError($"{name}: HazardCasterBehavior requires an EnemyCombatActions component to run the HazardPlace telegraph.", this);
        if (_zonePrefab == null)
            Debug.LogError($"{name}: HazardCasterBehavior has no HazardZone prefab assigned; it cannot place a hazard variant (R11.2).", this);
    }
}
