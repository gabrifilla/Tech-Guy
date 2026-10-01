using System.Collections.Generic;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free object pool
    /// <see cref="SimpleObjectPool{T}"/> — task 2.4 of project-cleanup-optimization
    /// (Requisitos 3.1, 3.2, 3.3, 3.7, 3.8, 10.2).
    ///
    /// <see cref="SimpleObjectPool{T}"/> is 100% framework-agnostic (no <see cref="UnityEngine.MonoBehaviour"/>,
    /// no scene, no <c>GameObject</c>), so its acquire/release contract can be property-checked without a
    /// live Unity scene — exactly the pure logic floor the spec says is the only CLI-automatable target.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing operation sequence as a counterexample — matching the
    /// convention of the sibling pure-logic property tests (e.g. ProjectileMotionPropertyTests,
    /// CombatBalanceBoundsPropertyTests).
    ///
    /// Validates: Requirements 3.1, 3.2, 3.3, 3.7, 3.8, 10.2
    /// </summary>
    public sealed class SimpleObjectPoolPropertyTests
    {
        /// <summary>
        /// A trivial, uniquely-identified reference type the pool manages. Each instance carries a
        /// monotonic id from its factory so the test can prove identity-level invariants (no live
        /// duplicate handed out, instances are reused rather than leaked).
        /// </summary>
        private sealed class Boxed
        {
            public int Id { get; }
            public Boxed(int id) { Id = id; }
        }

        // Feature: project-cleanup-optimization, Property 1: SimpleObjectPool<T> — acquire nunca
        // entrega instância viva duplicada; expande quando vazio; release permite reuso; CreatedCount
        // nunca excede o high-water-mark de instâncias simultâneas.
        //
        // For any interleaving of Acquire/Release operations, we drive a model of the pool and assert,
        // after every single operation, that:
        //   (a) Acquire never returns null and never hands out an instance that is already live
        //       (no duplicate live instance), and it reuses a free instance when one exists, only
        //       creating a brand-new one (expansion) when the free set is empty (R3.1/R3.2/R3.3/R3.7);
        //   (b) Release parks a live instance so it becomes reusable, while a null or already-free
        //       (duplicate) release is a no-op that does not corrupt the counts (R3.8);
        //   (c) CreatedCount is a high-water-mark that only ever grows and never exceeds the maximum
        //       number of simultaneously-live instances observed so far — proof that instances are
        //       reused, not leaked (R3.7/R3.8/R10.2);
        //   (d) the derived accounting holds: CreatedCount == LiveCount + FreeCount and
        //       LiveCount == live.Count at all times.
        // Validates: Requirements 3.1, 3.2, 3.3, 3.7, 3.8, 10.2
        [Test]
        public void AcquireReleaseNeverDuplicatesLiveAndCreatedCountTracksHighWaterMark()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Optional prewarm so the "reuse before expand" branch is exercised from the start,
                // including a 0-prewarm cold pool that must expand on the first acquire.
                int prewarm = rng.Next(0, 5);
                int nextId = 0;
                var pool = new SimpleObjectPool<Boxed>(() => new Boxed(nextId++), prewarm);

                string ctx = $"[prewarm={prewarm}]";

                // Prewarm creates exactly `prewarm` free instances up front (R3.7 expansion-by-factory).
                PropertyCheck.That(pool.CreatedCount == prewarm,
                    $"{ctx}: prewarm created {pool.CreatedCount} instead of {prewarm}");
                PropertyCheck.That(pool.FreeCount == prewarm,
                    $"{ctx}: prewarm left FreeCount={pool.FreeCount} instead of {prewarm}");
                PropertyCheck.That(pool.LiveCount == 0,
                    $"{ctx}: prewarm left LiveCount={pool.LiveCount} instead of 0");

                // Model state: the set of instances currently handed out (live).
                var live = new HashSet<Boxed>();
                // High-water-mark of simultaneously-live instances we have observed.
                int maxSimultaneousLive = 0;
                int createdBefore = pool.CreatedCount;

                int ops = rng.Next(20, 60);
                for (int op = 0; op < ops; op++)
                {
                    // Bias toward Acquire so the pool is pushed to expand, but keep Release frequent
                    // enough that reuse happens. When nothing is live, we must Acquire.
                    bool doAcquire = live.Count == 0 || rng.Next(0, 100) < 60;

                    int freeBefore = pool.FreeCount;
                    int liveCreatedBefore = pool.CreatedCount;

                    if (doAcquire)
                    {
                        Boxed item = pool.Acquire();

                        // (a) Acquire never returns null (the factory never returns null here) ...
                        PropertyCheck.That(item != null,
                            $"{ctx} op#{op}: Acquire returned null");
                        // ... and never hands out an instance that is already live (no live duplicate).
                        PropertyCheck.That(!live.Contains(item),
                            $"{ctx} op#{op}: Acquire handed out an already-live instance id={item.Id}");

                        if (freeBefore > 0)
                        {
                            // A free instance existed: it must be reused, not newly created.
                            PropertyCheck.That(pool.CreatedCount == liveCreatedBefore,
                                $"{ctx} op#{op}: Acquire created a new instance ({liveCreatedBefore} -> {pool.CreatedCount}) despite {freeBefore} free");
                        }
                        else
                        {
                            // Free set was empty: the pool must expand by exactly one (R3.7).
                            PropertyCheck.That(pool.CreatedCount == liveCreatedBefore + 1,
                                $"{ctx} op#{op}: empty-pool Acquire did not expand by one ({liveCreatedBefore} -> {pool.CreatedCount})");
                        }

                        live.Add(item);
                        if (live.Count > maxSimultaneousLive) maxSimultaneousLive = live.Count;
                    }
                    else
                    {
                        // Exercise all Release paths: a genuine live item, a null, or a double-release.
                        int kind = rng.Next(0, 10);
                        if (kind < 7 && live.Count > 0)
                        {
                            // Release a currently-live instance -> it becomes reusable (R3.8).
                            Boxed victim = PickOne(live, rng);
                            pool.Release(victim);
                            live.Remove(victim);

                            PropertyCheck.That(pool.FreeCount == freeBefore + 1,
                                $"{ctx} op#{op}: releasing a live instance did not grow FreeCount ({freeBefore} -> {pool.FreeCount})");
                            // Release never creates: CreatedCount is unchanged.
                            PropertyCheck.That(pool.CreatedCount == liveCreatedBefore,
                                $"{ctx} op#{op}: Release changed CreatedCount ({liveCreatedBefore} -> {pool.CreatedCount})");
                        }
                        else if (kind < 9)
                        {
                            // Null release is a no-op guard.
                            pool.Release(null);
                            PropertyCheck.That(pool.FreeCount == freeBefore,
                                $"{ctx} op#{op}: null Release changed FreeCount ({freeBefore} -> {pool.FreeCount})");
                            PropertyCheck.That(pool.CreatedCount == liveCreatedBefore,
                                $"{ctx} op#{op}: null Release changed CreatedCount");
                        }
                        else
                        {
                            // Double-release guard: releasing an already-free instance must be ignored
                            // so it cannot be parked twice and corrupt the counts (R3.8). We fabricate
                            // this by releasing a live item, then releasing it again.
                            if (live.Count > 0)
                            {
                                Boxed victim = PickOne(live, rng);
                                pool.Release(victim);
                                live.Remove(victim);
                                int freeAfterFirst = pool.FreeCount;

                                pool.Release(victim); // second release of the same instance
                                PropertyCheck.That(pool.FreeCount == freeAfterFirst,
                                    $"{ctx} op#{op}: double Release parked the same instance twice ({freeAfterFirst} -> {pool.FreeCount})");
                                PropertyCheck.That(pool.CreatedCount == liveCreatedBefore,
                                    $"{ctx} op#{op}: double Release changed CreatedCount");
                            }
                        }
                    }

                    // ---- invariants that must hold after EVERY operation ----

                    // (d) Derived accounting: created == live + free, and LiveCount mirrors the model.
                    PropertyCheck.That(pool.LiveCount == live.Count,
                        $"{ctx} op#{op}: LiveCount={pool.LiveCount} disagreed with model live={live.Count}");
                    PropertyCheck.That(pool.CreatedCount == pool.LiveCount + pool.FreeCount,
                        $"{ctx} op#{op}: CreatedCount={pool.CreatedCount} != Live({pool.LiveCount})+Free({pool.FreeCount})");
                    PropertyCheck.That(pool.FreeCount >= 0 && pool.LiveCount >= 0,
                        $"{ctx} op#{op}: negative count Live={pool.LiveCount} Free={pool.FreeCount}");

                    // (c) High-water-mark: CreatedCount only ever grows ...
                    PropertyCheck.That(pool.CreatedCount >= createdBefore,
                        $"{ctx} op#{op}: CreatedCount shrank ({createdBefore} -> {pool.CreatedCount})");
                    createdBefore = pool.CreatedCount;

                    // ... and never exceeds the peak of simultaneously-live instances (plus any
                    // prewarmed-but-never-acquired instances, which are free capacity, not leaks).
                    // Expressed as: CreatedCount <= max(peak live, prewarm). The pool only creates a
                    // new instance when the free set is empty, so it can never hold more instances
                    // than the most it ever had to serve at once.
                    int allowedPeak = maxSimultaneousLive > prewarm ? maxSimultaneousLive : prewarm;
                    PropertyCheck.That(pool.CreatedCount <= allowedPeak,
                        $"{ctx} op#{op}: CreatedCount={pool.CreatedCount} exceeded high-water-mark {allowedPeak} (peakLive={maxSimultaneousLive})");
                }

                // Final sweep: release everything still live; the pool must now hold exactly its
                // high-water-mark worth of free instances and zero live, proving full reuse with no leak.
                foreach (var item in live)
                {
                    pool.Release(item);
                }
                int finalPeak = maxSimultaneousLive > prewarm ? maxSimultaneousLive : prewarm;
                PropertyCheck.That(pool.LiveCount == 0,
                    $"{ctx}: after releasing all, LiveCount={pool.LiveCount} instead of 0");
                PropertyCheck.That(pool.FreeCount == pool.CreatedCount,
                    $"{ctx}: after releasing all, FreeCount={pool.FreeCount} != CreatedCount={pool.CreatedCount}");
                PropertyCheck.That(pool.CreatedCount <= finalPeak,
                    $"{ctx}: final CreatedCount={pool.CreatedCount} exceeded high-water-mark {finalPeak}");
            });
        }

        // Feature: project-cleanup-optimization, Property 1 (focused expansion/reuse facet):
        // acquiring N instances with all previous ones still live always forces N creations
        // (expansion when empty, R3.7); releasing them all and re-acquiring the same N must reuse the
        // existing instances with ZERO further creation (release permits reuse, R3.8) — a direct,
        // isolated demonstration that CreatedCount is pinned to the simultaneous-live high-water-mark.
        // Validates: Requirements 3.1, 3.2, 3.3, 3.7, 3.8, 10.2
        [Test]
        public void ReleaseEnablesReuseSoCreatedCountStaysAtPeakSimultaneousLive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int nextId = 0;
                var pool = new SimpleObjectPool<Boxed>(() => new Boxed(nextId++));

                int n = rng.Next(1, 25);

                // Round 1: hold all n live at once -> the empty pool must expand n times (R3.7).
                var firstBatch = new HashSet<Boxed>();
                for (int k = 0; k < n; k++)
                {
                    Boxed item = pool.Acquire();
                    PropertyCheck.That(item != null, $"n={n}: Acquire returned null on round 1 #{k}");
                    PropertyCheck.That(firstBatch.Add(item),
                        $"n={n}: round 1 handed out a live duplicate id={item.Id} at #{k}");
                }
                PropertyCheck.That(pool.CreatedCount == n,
                    $"n={n}: holding {n} live did not create exactly {n} (got {pool.CreatedCount})");
                PropertyCheck.That(pool.LiveCount == n && pool.FreeCount == 0,
                    $"n={n}: round 1 end Live={pool.LiveCount} Free={pool.FreeCount}, expected {n}/0");

                // Return them all -> every instance becomes reusable.
                foreach (var item in firstBatch) pool.Release(item);
                PropertyCheck.That(pool.FreeCount == n && pool.LiveCount == 0,
                    $"n={n}: after release-all Free={pool.FreeCount} Live={pool.LiveCount}, expected {n}/0");

                int createdAfterRound1 = pool.CreatedCount;

                // Round 2: re-acquire n -> all must come from the free set, ZERO new creation (R3.8),
                // because the simultaneous-live high-water-mark is still n.
                var secondBatch = new HashSet<Boxed>();
                for (int k = 0; k < n; k++)
                {
                    Boxed item = pool.Acquire();
                    PropertyCheck.That(item != null, $"n={n}: Acquire returned null on round 2 #{k}");
                    PropertyCheck.That(secondBatch.Add(item),
                        $"n={n}: round 2 handed out a live duplicate id={item.Id} at #{k}");
                    PropertyCheck.That(firstBatch.Contains(item),
                        $"n={n}: round 2 handed out a freshly-created instance id={item.Id} instead of reusing");
                }
                PropertyCheck.That(pool.CreatedCount == createdAfterRound1,
                    $"n={n}: round 2 created new instances ({createdAfterRound1} -> {pool.CreatedCount}) instead of reusing");
                PropertyCheck.That(pool.CreatedCount == n,
                    $"n={n}: CreatedCount drifted from the high-water-mark {n} (got {pool.CreatedCount})");
            });
        }

        /// <summary>Picks a pseudo-random element from a set using the seeded generator.</summary>
        private static Boxed PickOne(HashSet<Boxed> set, System.Random rng)
        {
            int target = rng.Next(0, set.Count);
            int idx = 0;
            foreach (var item in set)
            {
                if (idx == target) return item;
                idx++;
            }
            // Unreachable for a non-empty set; satisfies the compiler.
            return null;
        }
    }
}
