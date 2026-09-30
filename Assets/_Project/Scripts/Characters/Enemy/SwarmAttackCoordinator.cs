using UnityEngine;

/// <summary>
/// Thin per-room / per-encounter <see cref="MonoBehaviour"/> that hosts the swarm melee attack-slot
/// system (R5). It owns a pure <see cref="AttackSlotPool{TEnemy}"/> keyed by <see cref="EnemyAI"/> and
/// exposes a tiny request/return API that <see cref="EnemyAI"/> calls around its telegraphed attack.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 6.5.
/// Requirements:
///   R5.2 — <see cref="EnemyAI"/> asks for a token before starting <c>TelegraphedAttack</c>; while the
///          pool is full the request is denied and the enemy holds its waiting position instead of attacking.
///   R5.3 — the token is returned on interrupt / stun / stance-break through <see cref="EnemyAI.InterruptAttack"/>
///          (which the <see cref="CombatReactionController"/> already invokes on those events), freeing the
///          slot for another eligible enemy.
///   R5.4 — the slot system is implemented once here, decoupled from every weapon; no per-weapon logic lives in it.
///
/// All the slot invariants (limit, waiting queue, promotion, "leave the slot free when nobody is
/// eligible" R5.5) live in the pure <see cref="AttackSlotPool{TEnemy}"/>, which is property-tested
/// without a scene. This component only bridges the Unity lifecycle to that pool and validates the
/// authored limit. It performs no perception and no locomotion — <see cref="EnemyAI"/> keeps driving
/// movement and telegraphs; the coordinator merely answers "may this enemy attack right now?".
/// </remarks>
public sealed class SwarmAttackCoordinator : MonoBehaviour
{
    [Tooltip("Maximum number of melee enemies in this room/encounter that may attack the player at once. " +
             "A non-positive value is treated as missing and clamped up to 1 (logged once).")]
    [SerializeField, Min(AttackSlotConfig.MinConcurrentMelee)] private int _maxConcurrentMelee = 3;

    private AttackSlotPool<EnemyAI> _pool;

    /// <summary>The concurrent-attacker limit for this encounter (always &gt; 0, R5.1).</summary>
    public int Capacity => Pool.Capacity;

    /// <summary>How many enemies currently hold an attack token in this encounter.</summary>
    public int ActiveCount => Pool.ActiveCount;

    /// <summary>How many enemies are currently waiting for a token to free up.</summary>
    public int WaitingCount => Pool.WaitingCount;

    private void Awake()
    {
        // Build the pool eagerly so the limit is validated (and any missing/invalid authored value is
        // logged) before the first enemy requests a slot.
        EnsurePool();
    }

    /// <summary>
    /// Sets the concurrent-attacker limit at runtime before enemies request slots (used by the
    /// procedural director, which materializes one coordinator per combat room). Rebuilds the pool so
    /// the new capacity takes effect; a non-positive value is clamped up to 1 (R5.1).
    /// </summary>
    /// <param name="maxConcurrentMelee">Desired simultaneous melee attackers for this encounter.</param>
    public void ConfigureCapacity(int maxConcurrentMelee)
    {
        _maxConcurrentMelee = maxConcurrentMelee;
        _pool = null;
        EnsurePool();
    }


    /// <summary>
    /// Requests an attack token for <paramref name="enemy"/> before it starts its telegraphed attack
    /// (R5.2). Returns true when a slot was granted; false when the encounter is at its limit, in which
    /// case the enemy is held in the waiting queue and should keep its position instead of attacking.
    /// </summary>
    /// <param name="enemy">The enemy requesting to attack.</param>
    public bool TryAcquireSlot(EnemyAI enemy)
    {
        if (enemy == null) return false;
        return Pool.TryAcquire(enemy);
    }

    /// <summary>
    /// Returns the token held (or the waiting spot occupied) by <paramref name="enemy"/> on interrupt /
    /// stun / stance-break (R5.3), freeing its slot so the next eligible waiting enemy can be promoted.
    /// When nobody is eligible the slot simply stays free (R5.5); no enemy is pulled out of position.
    /// Releasing an enemy that holds no token is safe and idempotent.
    /// </summary>
    /// <param name="enemy">The enemy whose slot should be freed.</param>
    /// <returns>The enemy promoted into the freed slot, or null when none was eligible.</returns>
    public EnemyAI ReleaseSlot(EnemyAI enemy)
    {
        if (enemy == null) return null;
        return Pool.Release(enemy);
    }

    /// <summary>Whether <paramref name="enemy"/> currently holds a token and may execute its attack.</summary>
    /// <param name="enemy">The enemy to query.</param>
    public bool HoldsToken(EnemyAI enemy) => enemy != null && Pool.HoldsToken(enemy);

    private AttackSlotPool<EnemyAI> Pool
    {
        get
        {
            EnsurePool();
            return _pool;
        }
    }

    private void EnsurePool()
    {
        if (_pool != null) return;

        AttackSlotConfig config = AttackSlotConfig.FromRawLimit(_maxConcurrentMelee, out bool wasClamped);
        if (wasClamped)
        {
            Debug.LogWarning(
                $"SwarmAttackCoordinator on '{name}': maxConcurrentMelee was {_maxConcurrentMelee} " +
                $"(<= 0); clamping to {config.MaxConcurrentMelee} (R5.1).",
                this);
            _maxConcurrentMelee = config.MaxConcurrentMelee;
        }

        _pool = new AttackSlotPool<EnemyAI>(config);
    }
}
