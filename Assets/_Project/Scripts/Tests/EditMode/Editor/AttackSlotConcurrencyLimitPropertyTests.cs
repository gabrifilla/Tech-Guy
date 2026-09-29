using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the swarm melee attack-slot concurrency limit — task 6.2 of
    /// weapon-gameplay-swarm-rework.
    ///
    /// The pure <see cref="AttackSlotPool{TEnemy}"/> owns the concurrent-attacker limit
    /// (<see cref="AttackSlotConfig.MaxConcurrentMelee"/>, guaranteed &gt; 0 via
    /// <see cref="AttackSlotConfig.FromRawLimit"/>) plus the set of active token holders and the
    /// waiting queue of excess enemies. This test drives a random sequence of acquire / release
    /// operations over a fixed roster of enemies and asserts the R5.1 / R5.2 invariant: the number
    /// of granted attack tokens never exceeds the configured limit, and every enemy that asked to
    /// attack but was denied a token is held in the waiting queue rather than attacking.
    ///
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports
    /// the exact failing operation sequence as a counterexample. Source classes are not modified.
    /// </summary>
    public sealed class AttackSlotConcurrencyLimitPropertyTests
    {
        /// <summary>Reference-identity enemy stand-in; the pool keys enemies by instance.</summary>
        private sealed class FakeEnemy
        {
            public readonly int Id;
            public FakeEnemy(int id) => Id = id;
            public override string ToString() => "E" + Id;
        }

        // Feature: weapon-gameplay-swarm-rework, Property 15: Limite de atacantes simultâneos do enxame.
        // For any sequence of attack-token acquisitions and releases over N enemies, the number of
        // melee attackers holding a granted token never exceeds the configured limit (> 0); excess
        // enemies that requested a token remain waiting instead of attacking.
        // Validates: Requirements 5.1, 5.2
        [Test]
        public void ActiveAttackersNeverExceedLimitAndExcessRemainWaiting()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate a valid limit (> 0) via the data-authored entry point --------------
                // Feed a raw value across the invalid (<= 0) and valid ranges so FromRawLimit's
                // clamp-to-minimum path is exercised too; the resulting Capacity is always >= 1.
                int rawLimit = rng.Next(-2, 9); // -2 .. 8, includes 0 and negatives (get clamped)
                var config = AttackSlotConfig.FromRawLimit(rawLimit, out bool wasClamped);
                var pool = new AttackSlotPool<FakeEnemy>(config);

                PropertyCheck.That(pool.Capacity > 0,
                    $"Capacity must be strictly positive (R5.1); rawLimit={rawLimit} gave Capacity={pool.Capacity}");
                PropertyCheck.That(wasClamped == (rawLimit < AttackSlotConfig.MinConcurrentMelee),
                    $"FromRawLimit clamp flag disagreed with rawLimit={rawLimit} (wasClamped={wasClamped})");

                // ---- fixed roster of enemies -----------------------------------------------------
                int enemyCount = rng.Next(1, 8); // 1 .. 7 enemies contending for slots
                var enemies = new FakeEnemy[enemyCount];
                for (int e = 0; e < enemyCount; e++) enemies[e] = new FakeEnemy(e);

                // Enemies that have asked to attack and have NOT been released since. These are the
                // enemies that MUST either hold a token or be waiting (nothing else is legal for a
                // "wants to attack" enemy under R5.2). Cleared on release.
                var contending = new HashSet<FakeEnemy>();

                // ---- drive a random acquire/release sequence -------------------------------------
                int ops = rng.Next(20, 60);
                for (int op = 0; op < ops; op++)
                {
                    FakeEnemy enemy = enemies[rng.Next(enemyCount)];
                    bool acquire = rng.Next(0, 2) == 0; // ~50/50 acquire vs release

                    if (acquire)
                    {
                        int activeBefore = pool.ActiveCount;
                        bool granted = pool.TryAcquire(enemy);
                        contending.Add(enemy);

                        // A grant only happens when a slot was free; a denial only when the pool was
                        // full. Either way the enemy that requested must now be tracked by the pool.
                        if (granted)
                        {
                            PropertyCheck.That(pool.HoldsToken(enemy),
                                $"case#{i} op#{op}: TryAcquire returned true but {enemy} holds no token");
                            PropertyCheck.That(!pool.IsWaiting(enemy),
                                $"case#{i} op#{op}: granted {enemy} is simultaneously waiting");
                        }
                        else
                        {
                            // Denied only because the pool was already at capacity (R5.2 trigger).
                            PropertyCheck.That(activeBefore >= pool.Capacity,
                                $"case#{i} op#{op}: {enemy} was denied a token while a slot was free " +
                                $"(active={activeBefore}, capacity={pool.Capacity})");
                            PropertyCheck.That(pool.IsWaiting(enemy),
                                $"case#{i} op#{op}: denied {enemy} is not held in the waiting queue (R5.2)");
                            PropertyCheck.That(!pool.HoldsToken(enemy),
                                $"case#{i} op#{op}: denied {enemy} unexpectedly holds a token");
                        }
                    }
                    else
                    {
                        FakeEnemy promoted = pool.Release(enemy);
                        contending.Remove(enemy);

                        // The released enemy must no longer hold a token or wait after release.
                        PropertyCheck.That(!pool.HoldsToken(enemy),
                            $"case#{i} op#{op}: released {enemy} still holds a token");
                        PropertyCheck.That(!pool.IsWaiting(enemy),
                            $"case#{i} op#{op}: released {enemy} is still waiting");

                        // A promotion is only ever an enemy that was waiting; it now holds a token.
                        if (promoted != null)
                        {
                            PropertyCheck.That(pool.HoldsToken(promoted),
                                $"case#{i} op#{op}: promoted {promoted} does not hold a token");
                            PropertyCheck.That(!pool.IsWaiting(promoted),
                                $"case#{i} op#{op}: promoted {promoted} is still waiting");
                        }
                    }

                    // ---- invariants that must hold after EVERY operation -------------------------

                    // R5.1: granted attackers never exceed the configured limit.
                    PropertyCheck.That(pool.ActiveCount <= pool.Capacity,
                        $"case#{i} op#{op}: active attackers {pool.ActiveCount} exceeded capacity {pool.Capacity} (R5.1)");
                    PropertyCheck.That(pool.ActiveCount >= 0,
                        $"case#{i} op#{op}: active count went negative ({pool.ActiveCount})");

                    // IsFull is consistent with the count vs capacity.
                    PropertyCheck.That(pool.IsFull == (pool.ActiveCount >= pool.Capacity),
                        $"case#{i} op#{op}: IsFull={pool.IsFull} disagreed with active={pool.ActiveCount}/cap={pool.Capacity}");

                    // R5.2: every enemy that wants to attack either holds a token or is waiting; a
                    // token holder is never simultaneously waiting. Excess (denied) enemies wait.
                    int holders = 0;
                    int waiters = 0;
                    foreach (FakeEnemy c in contending)
                    {
                        bool holds = pool.HoldsToken(c);
                        bool waits = pool.IsWaiting(c);
                        PropertyCheck.That(holds ^ waits,
                            $"case#{i} op#{op}: contending {c} must be exactly one of holder/waiter " +
                            $"(holds={holds}, waits={waits})");
                        if (holds) holders++; else waiters++;
                    }

                    // Whenever anyone is waiting, the pool must be full (nobody waits while a slot is
                    // free) — i.e. the waiting enemies are genuinely the excess beyond the limit.
                    if (waiters > 0)
                    {
                        PropertyCheck.That(pool.IsFull,
                            $"case#{i} op#{op}: {waiters} enemy(ies) waiting while the pool is not full (R5.2)");
                    }

                    PropertyCheck.That(holders == pool.ActiveCount,
                        $"case#{i} op#{op}: counted {holders} contending holders but ActiveCount={pool.ActiveCount}");
                    PropertyCheck.That(waiters == pool.WaitingCount,
                        $"case#{i} op#{op}: counted {waiters} contending waiters but WaitingCount={pool.WaitingCount}");
                }
            });
        }
    }
}
