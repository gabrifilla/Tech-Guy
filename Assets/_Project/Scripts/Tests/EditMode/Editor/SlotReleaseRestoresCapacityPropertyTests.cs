using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the slot-release/capacity-restore invariant of the pure, scene-free
    /// <see cref="AttackSlotPool{TEnemy}"/> — task 6.3 of weapon-gameplay-swarm-rework.
    ///
    /// <see cref="AttackSlotPool{TEnemy}"/> is a plain C# model of the swarm attack-slot system (R5),
    /// so its promotion invariant can be property-checked without a live Unity scene. This project
    /// cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (varying
    /// capacity and the number of waiters across the input space) and reports the exact failing case
    /// as a counterexample.
    ///
    /// The enemy identity is modelled by a distinct reference type (<see cref="Enemy"/>) so the pool
    /// keys holders/waiters by instance, exactly as it does in play.
    /// </summary>
    public sealed class SlotReleaseRestoresCapacityPropertyTests
    {
        /// <summary>Minimal reference-typed stand-in for an enemy identity (compared by reference).</summary>
        private sealed class Enemy
        {
            public readonly int Id;
            public Enemy(int id) => Id = id;
            public override string ToString() => $"E{Id}";
        }

        // Feature: weapon-gameplay-swarm-rework, Property 16: Devolução de slot restaura capacidade.
        // For every pool at its limit (ActiveCount == Capacity) with one or more eligible waiters,
        // releasing an active holder (interrupt / stun / stance-break, R5.3) promotes exactly one
        // waiting enemy into the freed slot: the promoted enemy was waiting and now holds a token,
        // ActiveCount stays == Capacity, and WaitingCount decreases by exactly 1. With no waiters,
        // the released slot simply stays free (ActiveCount == Capacity - 1) and nothing is invented.
        // Validates: Requirements 5.3
        [Test]
        public void ReleaseFromFullPoolPromotesExactlyOneEligibleWaiter()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate a pool at its limit with a random capacity and a random number of waiters.
                int capacity = rng.Next(1, 6);           // R5.1: limit strictly > 0.
                int waiterCount = rng.Next(0, 5);        // 0 waiters exercises the "slot stays free" branch.

                var config = new AttackSlotConfig(capacity);
                var pool = new AttackSlotPool<Enemy>(config);

                // Fill every slot: the first `capacity` acquirers become active holders.
                var holders = new List<Enemy>(capacity);
                for (int h = 0; h < capacity; h++)
                {
                    var enemy = new Enemy(h);
                    bool granted = pool.TryAcquire(enemy);
                    PropertyCheck.That(granted, $"acquiring into a free slot must grant a token (capacity={capacity})");
                    holders.Add(enemy);
                }

                // Excess enemies request while full and are held waiting (R5.2), in FIFO arrival order.
                var waiters = new List<Enemy>(waiterCount);
                for (int w = 0; w < waiterCount; w++)
                {
                    var enemy = new Enemy(capacity + w);
                    bool granted = pool.TryAcquire(enemy);
                    PropertyCheck.That(!granted, $"acquiring while full must queue, not grant (capacity={capacity})");
                    waiters.Add(enemy);
                }

                // Precondition: the pool is exactly at its limit with the expected waiter count.
                PropertyCheck.That(pool.ActiveCount == capacity,
                    $"pool must start full: ActiveCount={pool.ActiveCount} expected {capacity}");
                PropertyCheck.That(pool.IsFull, "pool must report IsFull before release");
                PropertyCheck.That(pool.WaitingCount == waiterCount,
                    $"waiting count must match queued enemies: WaitingCount={pool.WaitingCount} expected {waiterCount}");

                // Release a randomly chosen active holder (interrupt / stun / stance-break).
                Enemy released = holders[rng.Next(0, holders.Count)];
                Enemy promoted = pool.Release(released);

                string state = $"[capacity={capacity} waiters={waiterCount} released={released}]";

                if (waiterCount > 0)
                {
                    // Exactly one eligible waiter is promoted into the freed slot.
                    PropertyCheck.That(promoted != null,
                        $"a full pool with waiters must promote one waiter on release for {state}");
                    PropertyCheck.That(waiters.Contains(promoted),
                        $"the promoted enemy must be one of the waiting enemies for {state} (promoted={promoted})");
                    PropertyCheck.That(promoted == waiters[0],
                        $"promotion must respect FIFO order (expected {waiters[0]}, got {promoted}) for {state}");
                    PropertyCheck.That(pool.HoldsToken(promoted),
                        $"the promoted enemy must now hold a token for {state} (promoted={promoted})");

                    // Capacity is restored: still exactly full, waiting queue shrank by exactly one.
                    PropertyCheck.That(pool.ActiveCount == capacity,
                        $"ActiveCount must stay == Capacity after promotion: got {pool.ActiveCount} for {state}");
                    PropertyCheck.That(pool.WaitingCount == waiterCount - 1,
                        $"WaitingCount must decrease by exactly 1: got {pool.WaitingCount} expected {waiterCount - 1} for {state}");
                    PropertyCheck.That(!pool.IsWaiting(promoted),
                        $"the promoted enemy must no longer be waiting for {state} (promoted={promoted})");
                }
                else
                {
                    // No eligible waiter: the slot stays free, nothing is fabricated (R5.5).
                    PropertyCheck.That(promoted == null,
                        $"with no waiters, release must promote nobody (slot stays free) for {state} (promoted={promoted})");
                    PropertyCheck.That(pool.ActiveCount == capacity - 1,
                        $"ActiveCount must drop by exactly one freed slot: got {pool.ActiveCount} expected {capacity - 1} for {state}");
                    PropertyCheck.That(pool.WaitingCount == 0,
                        $"WaitingCount must remain 0 when there were no waiters for {state}");
                    PropertyCheck.That(!pool.IsFull,
                        $"pool must no longer be full after freeing a slot with no waiter for {state}");
                }

                // In every case the released holder no longer holds a token.
                PropertyCheck.That(!pool.HoldsToken(released),
                    $"the released enemy must not hold a token after release for {state}");
            });
        }
    }
}
