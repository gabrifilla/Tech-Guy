using System;
using System.Collections.Generic;

/// <summary>
/// Pure, scene-free model of the swarm melee attack-slot system (R5). Owns the concurrent-attacker
/// limit, the set of active token holders, and the waiting queue of excess enemies. Extracted from
/// the <see cref="SwarmAttackCoordinator"/> MonoBehaviour (task 6.5) so the slot invariants can be
/// property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 6.1.
/// Requirements:
///   R5.1 — limit is a configurable value strictly greater than 0 (enforced via <see cref="AttackSlotConfig"/>).
///   R5.2 — while active attackers are at the limit, excess enemies are held waiting instead of attacking.
///   R5.3 — when a holder is interrupted / stunned / stance-broken, its slot is released for another eligible enemy.
///   R5.5 — if no eligible enemy is available for a freed slot, the slot stays free; nothing is pulled out of
///          its preferred distance. This class never fabricates a holder: it only ever grants a token to an
///          enemy that explicitly requested one and remains in the waiting queue, so an empty queue leaves the
///          slot free by construction.
///
/// This class is deliberately identity-agnostic: callers key enemies by any stable reference type
/// (an <c>EnemyAI</c>, a component, etc.). It performs no locomotion, perception, or Unity work —
/// the coordinator MonoBehaviour drives positioning/telegraphs and simply asks this pool whether an
/// enemy currently holds a token.
/// </remarks>
/// <typeparam name="TEnemy">The reference type used to identify an enemy (compared by reference identity).</typeparam>
public sealed class AttackSlotPool<TEnemy> where TEnemy : class
{
    private readonly AttackSlotConfig _config;

    // Active token holders. Membership == "this enemy currently holds an attack slot" (R5.2).
    private readonly HashSet<TEnemy> _activeHolders;

    // Enemies that requested a token while the pool was full, in FIFO arrival order (R5.2).
    // A distinct-membership set guards the queue against duplicate enqueues.
    private readonly Queue<TEnemy> _waiting;
    private readonly HashSet<TEnemy> _waitingSet;

    /// <summary>Creates a pool bound to the given validated config (limit already guaranteed &gt; 0).</summary>
    /// <param name="config">The concurrent-attacker configuration (R5.1).</param>
    public AttackSlotPool(AttackSlotConfig config)
    {
        _config = config;
        _activeHolders = new HashSet<TEnemy>(ReferenceEqualityComparer.Instance);
        _waiting = new Queue<TEnemy>();
        _waitingSet = new HashSet<TEnemy>(ReferenceEqualityComparer.Instance);
    }

    /// <summary>The maximum number of simultaneous melee attackers (R5.1).</summary>
    public int Capacity => _config.MaxConcurrentMelee;

    /// <summary>How many attack tokens are currently held.</summary>
    public int ActiveCount => _activeHolders.Count;

    /// <summary>How many enemies are waiting for a token to free up.</summary>
        // Membership in _waitingSet is authoritative; the _waiting queue can hold stale entries that
    // were released while queued (lazily skipped on dequeue), so count the set, not the queue.
    public int WaitingCount => _waitingSet.Count;

    /// <summary>True while every attack slot is occupied (R5.2 trigger).</summary>
    public bool IsFull => _activeHolders.Count >= _config.MaxConcurrentMelee;

    /// <summary>Whether the given enemy currently holds an attack token and may execute its attack.</summary>
    /// <param name="enemy">The enemy to query.</param>
    public bool HoldsToken(TEnemy enemy) => enemy != null && _activeHolders.Contains(enemy);

    /// <summary>Whether the given enemy is currently waiting for a token.</summary>
    /// <param name="enemy">The enemy to query.</param>
    public bool IsWaiting(TEnemy enemy) => enemy != null && _waitingSet.Contains(enemy);

    /// <summary>
    /// Requests an attack token for <paramref name="enemy"/>. Grants one immediately when a slot is
    /// free (R5.1); otherwise the enemy is placed in the waiting queue and keeps holding its position
    /// (R5.2). Requesting again while already holding a token, or while already queued, is idempotent.
    /// </summary>
    /// <param name="enemy">The enemy requesting to attack.</param>
    /// <returns>True when a token was granted; false when the enemy was queued (or is already queued).</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="enemy"/> is null.</exception>
    public bool TryAcquire(TEnemy enemy)
    {
        if (enemy == null) throw new ArgumentNullException(nameof(enemy));

        // Already attacking: idempotent success.
        if (_activeHolders.Contains(enemy)) return true;

        if (!IsFull)
        {
            // A slot is free and this enemy is eligible → grant the token.
            RemoveFromWaiting(enemy);
            _activeHolders.Add(enemy);
            return true;
        }

        // Pool is full → hold the enemy in waiting position instead of letting it attack (R5.2).
        if (_waitingSet.Add(enemy))
        {
            _waiting.Enqueue(enemy);
        }

        return false;
    }

    /// <summary>
    /// Releases the token held by <paramref name="enemy"/> (called on interrupt / stun / stance-break,
    /// R5.3) and promotes the next eligible waiting enemy into the freed slot. When the queue is empty
    /// the slot simply stays free (R5.5) — no enemy is invented or pulled out of its preferred distance.
    /// Releasing an enemy that holds no token still cleans it out of the waiting queue and attempts a
    /// promotion, so cancelling a queued (not-yet-attacking) enemy is safe.
    /// </summary>
    /// <param name="enemy">The enemy whose slot should be freed.</param>
    /// <returns>
    /// The enemy promoted into the freed slot, or null when no eligible enemy was waiting (slot stays free, R5.5).
    /// </returns>
    /// <exception cref="ArgumentNullException">When <paramref name="enemy"/> is null.</exception>
    public TEnemy Release(TEnemy enemy)
    {
        if (enemy == null) throw new ArgumentNullException(nameof(enemy));

        bool wasHolder = _activeHolders.Remove(enemy);
        RemoveFromWaiting(enemy);

        // Only a freed active slot can be handed to a waiting enemy. If the released enemy was merely
        // queued (not attacking), capacity did not change, so we must not exceed the limit.
        if (!wasHolder) return null;

        return PromoteNextEligible();
    }

    /// <summary>
    /// Pulls the next distinct waiting enemy (FIFO) into a free slot, if one exists and capacity
    /// allows. Returns null when nobody is eligible, leaving the slot free (R5.5).
    /// </summary>
    private TEnemy PromoteNextEligible()
    {
        while (_waiting.Count > 0)
        {
            if (IsFull) return null;

            TEnemy next = _waiting.Dequeue();

            // Skip stale entries whose membership was already cleared (e.g. released while queued).
            if (!_waitingSet.Remove(next)) continue;

            // Guard against an enemy that somehow already holds a token.
            if (_activeHolders.Contains(next)) continue;

            _activeHolders.Add(next);
            return next;
        }

        return null;
    }

    private void RemoveFromWaiting(TEnemy enemy)
    {
        // Lazy removal from the queue: membership is authoritative, the queue is filtered on dequeue.
        _waitingSet.Remove(enemy);
    }

    /// <summary>
    /// Reference-identity comparer so enemies are keyed by instance, never by an overridden
    /// <c>Equals</c>/<c>GetHashCode</c>. Kept local to avoid depending on the .NET version's built-in
    /// <c>ReferenceEqualityComparer</c>, which is not available on all Unity scripting runtimes.
    /// </summary>
    private sealed class ReferenceEqualityComparer : IEqualityComparer<TEnemy>
    {
        public static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        bool IEqualityComparer<TEnemy>.Equals(TEnemy x, TEnemy y) => ReferenceEquals(x, y);
        int IEqualityComparer<TEnemy>.GetHashCode(TEnemy obj)
            => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
