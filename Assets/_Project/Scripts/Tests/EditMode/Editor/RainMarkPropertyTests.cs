using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property + example tests for Property 16 of gauntlet-boon-playstyle-overhaul (the
    /// <c>RainMark</c> / "Chuva marcadora" boon: the Bow R marks, amplifies, and slows enemies).
    ///
    /// Property 16 (design): while an enemy is marked, the direct-damage amplifier
    /// <c>RainMarkRegistry.AmplifierFor</c> is <c>1 + 0.2R</c>, and exactly <c>1</c> whenever the enemy
    /// is NOT marked (never marked, or after the mark has expired); the movement slow applied when the
    /// enemy is marked is restored when the mark expires (a clean round-trip). (R10.1/R10.2/R10.3.)
    ///
    /// Approach: the whole contract lives in the real run-scoped <see cref="RainMarkRegistry"/>, so this
    /// test drives the <b>real</b> component. The only Unity couplings are time and the per-enemy slow;
    /// both are behind injectable seams (<c>ConfigureForTests</c>), so the amplifier/expiry decision and
    /// the slow round-trip can be exercised deterministically without a scene clock or a NavMeshAgent:
    ///
    ///  - A test clock (a mutable field) replaces <c>Time.time</c>, so the test advances time explicitly
    ///    and the registry's expiry (<c>ExpireElapsed</c>) and amplifier read the same controllable now.
    ///  - A recording <see cref="SlowSpy"/> replaces the production NavMeshAgent-backed slow handle, so
    ///    the test observes exactly when the slow is applied and restored — proving the round-trip
    ///    (R10.3) without a live agent, while the production path uses the real agent.
    ///
    /// Real <see cref="Actor"/> GameObjects are used as mark keys (created per case and destroyed with
    /// <c>DestroyImmediate</c>), matching how the registry keys its marks.
    ///
    /// The project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    // Feature: gauntlet-boon-playstyle-overhaul, Property 16
    public sealed class RainMarkPropertyTests
    {
        private const float Tolerance = 1e-4f;
        private const float Duration = RainMarkRegistry.Duration; // mark lifetime, in seconds
        private const float Slow = 0.4f;                          // design slow fraction while marked (R10.3)

        // Recording slow handle: counts Apply/Restore and the last applied fraction, so the test can
        // assert the mark's slow was applied once and restored exactly once on expiry/teardown (R10.3).
        private sealed class SlowSpy : RainMarkRegistry.ISlowHandle
        {
            public int Applied;
            public int Restored;
            public float LastFraction;
            public void Apply(float slowFraction) { Applied++; LastFraction = slowFraction; }
            public void Restore() { Restored++; }
        }

        // Builds a fresh registry on a throwaway GameObject with a controllable clock and a per-enemy
        // SlowSpy factory. The caller advances `clockHolder[0]` to move time. Returns the registry plus
        // the owning GameObject (for teardown) and the map of enemy -> its SlowSpy.
        private static RainMarkRegistry MakeRegistry(float amplify, float slow, float[] clockHolder,
            Dictionary<Actor, SlowSpy> spies, out GameObject owner)
        {
            owner = new GameObject("RainMarkRegistry");
            var registry = owner.AddComponent<RainMarkRegistry>();
            registry.Configure(amplify, slow);
            registry.ConfigureForTests(() => clockHolder[0], enemy =>
            {
                var spy = new SlowSpy();
                if (enemy) spies[enemy] = spy;
                return spy;
            });
            return registry;
        }

        private static Actor MakeEnemy(string name)
        {
            var go = new GameObject(name);
            return go.AddComponent<Actor>();
        }

        // Property 16: while marked, AmplifierFor == 1 + 0.2R; exactly 1 before any mark and after the
        // mark expires. The slow is applied on mark and restored when the mark expires.
        // Validates: Requirements 10.1, 10.2, 10.3
        [Test]
        public void MarkedAmplifierIsOnePlusPointTwoRank_AndSlowRoundTripsOnExpiry()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);            // [1,3] — catalog MaxRank is 3
                float amplify = 0.2f * rank;          // R10.2: +20% per rank
                float expectedAmp = 1f + amplify;

                var clock = new float[] { (float)rng.NextDouble() * 100f }; // arbitrary start time
                var spies = new Dictionary<Actor, SlowSpy>();
                RainMarkRegistry registry = MakeRegistry(amplify, Slow, clock, spies, out GameObject owner);
                Actor enemy = MakeEnemy($"enemy_{i}");

                string state = $"[case #{i}] rank={rank} start={clock[0]}";
                try
                {
                    // Before any mark: amplifier is exactly 1 and no slow has been applied (R10.2).
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemy) - 1f) <= Tolerance,
                        $"{state}: unmarked amplifier must be 1 (got {registry.AmplifierFor(enemy)})");
                    PropertyCheck.That(!registry.IsMarked(enemy), $"{state}: enemy must start unmarked");

                    // Mark the enemy: amplifier becomes 1 + 0.2R and the slow is applied once (R10.1/R10.3).
                    registry.Mark(enemy);
                    PropertyCheck.That(registry.IsMarked(enemy), $"{state}: enemy must be marked after Mark");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemy) - expectedAmp) <= Tolerance,
                        $"{state}: marked amplifier must be {expectedAmp} (got {registry.AmplifierFor(enemy)})");
                    PropertyCheck.That(spies.TryGetValue(enemy, out SlowSpy spy) && spy.Applied == 1 && spy.Restored == 0,
                        $"{state}: slow must be applied exactly once and not yet restored");
                    PropertyCheck.That(Math.Abs(spies[enemy].LastFraction - Slow) <= Tolerance,
                        $"{state}: slow fraction must be {Slow} (got {spies[enemy].LastFraction})");

                    // Any time strictly inside the window: still marked, amplifier still 1 + 0.2R.
                    float inside = clock[0] + (float)rng.NextDouble() * Duration * 0.999f;
                    clock[0] = inside;
                    registry.ExpireElapsed(); // mirrors Update(): expires nothing yet
                    PropertyCheck.That(registry.IsMarked(enemy),
                        $"{state}: enemy must stay marked at now={inside} (< expiry)");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemy) - expectedAmp) <= Tolerance,
                        $"{state}: amplifier must stay {expectedAmp} inside the window (got {registry.AmplifierFor(enemy)})");
                    PropertyCheck.That(spies[enemy].Restored == 0,
                        $"{state}: slow must not be restored while the mark is active");

                    // Advance to/past expiry: the mark lifts, amplifier returns to 1, slow is restored once.
                    clock[0] += Duration + (float)rng.NextDouble() * 5f; // at or beyond ClosesAt
                    registry.ExpireElapsed();
                    PropertyCheck.That(!registry.IsMarked(enemy),
                        $"{state}: enemy must be unmarked after expiry (now={clock[0]})");
                    PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemy) - 1f) <= Tolerance,
                        $"{state}: expired amplifier must return to 1 (got {registry.AmplifierFor(enemy)})");
                    PropertyCheck.That(spies[enemy].Applied == 1 && spies[enemy].Restored == 1,
                        $"{state}: slow must round-trip (applied once, restored once) — applied={spies[enemy].Applied}, restored={spies[enemy].Restored}");
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                    UnityEngine.Object.DestroyImmediate(owner);
                }
            });
        }

        // Property 16 (multi-enemy facet): with several enemies, only marked-and-active enemies amplify,
        // and each marked enemy's slow is independently restored exactly once when its mark expires.
        // Validates: Requirements 10.2, 10.3
        [Test]
        public void OnlyActivelyMarkedEnemiesAmplify_AndEachSlowRoundTripsIndependently()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);
                float expectedAmp = 1f + 0.2f * rank;

                var clock = new float[] { (float)rng.NextDouble() * 50f };
                var spies = new Dictionary<Actor, SlowSpy>();
                RainMarkRegistry registry = MakeRegistry(0.2f * rank, Slow, clock, spies, out GameObject owner);

                int count = rng.Next(1, 5);
                var enemies = new Actor[count];
                var marked = new bool[count];
                for (int e = 0; e < count; e++) enemies[e] = MakeEnemy($"enemy_{i}_{e}");

                string state = $"[case #{i}] rank={rank} count={count}";
                try
                {
                    // Mark a random subset; the rest stay unmarked and must never amplify.
                    for (int e = 0; e < count; e++)
                    {
                        marked[e] = rng.Next(0, 2) == 0;
                        if (marked[e]) registry.Mark(enemies[e]);
                    }

                    for (int e = 0; e < count; e++)
                    {
                        float amp = registry.AmplifierFor(enemies[e]);
                        float want = marked[e] ? expectedAmp : 1f;
                        PropertyCheck.That(Math.Abs(amp - want) <= Tolerance,
                            $"{state}: enemy#{e} marked={marked[e]} amplifier {amp} != {want}");
                    }

                    // Expire everything.
                    clock[0] += Duration + 1f;
                    registry.ExpireElapsed();

                    for (int e = 0; e < count; e++)
                    {
                        PropertyCheck.That(Math.Abs(registry.AmplifierFor(enemies[e]) - 1f) <= Tolerance,
                            $"{state}: enemy#{e} amplifier must return to 1 after expiry");
                        if (marked[e])
                            PropertyCheck.That(spies[enemies[e]].Applied == 1 && spies[enemies[e]].Restored == 1,
                                $"{state}: enemy#{e} slow must round-trip once");
                        else
                            PropertyCheck.That(!spies.ContainsKey(enemies[e]),
                                $"{state}: enemy#{e} was never marked, so no slow handle must exist");
                    }
                }
                finally
                {
                    for (int e = 0; e < count; e++)
                        if (enemies[e]) UnityEngine.Object.DestroyImmediate(enemies[e].gameObject);
                    UnityEngine.Object.DestroyImmediate(owner);
                }
            });
        }

        // Example: rank 2 marked amplifier is exactly 1.4, and the slow round-trips when the mark expires.
        [Test]
        public void Rank2_MarkedAmplifierIs1Point4_AndSlowRestoresOnExpiry()
        {
            var clock = new float[] { 0f };
            var spies = new Dictionary<Actor, SlowSpy>();
            RainMarkRegistry registry = MakeRegistry(0.4f, Slow, clock, spies, out GameObject owner);
            Actor enemy = MakeEnemy("enemy");

            try
            {
                registry.Mark(enemy);
                Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(1.4f).Within(Tolerance),
                    "Rank 2 marked amplifier must be 1 + 0.2*2 = 1.4.");
                Assert.That(spies[enemy].Applied, Is.EqualTo(1), "Slow applied once on mark.");

                // Advance past the mark duration: expiry restores the pending slow (round-trip, R10.3).
                clock[0] += Duration + 0.5f;
                registry.ExpireElapsed();
                Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(1f).Within(Tolerance),
                    "Amplifier returns to 1 once the mark expires.");
                Assert.That(spies[enemy].Restored, Is.EqualTo(1), "Expiry restores the pending slow.");
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(owner);
            }
        }

        // Example: an unmarked enemy never amplifies and never gets a slow handle.
        [Test]
        public void UnmarkedEnemy_AmplifierIsOne_NoSlow()
        {
            var clock = new float[] { 0f };
            var spies = new Dictionary<Actor, SlowSpy>();
            RainMarkRegistry registry = MakeRegistry(0.6f, Slow, clock, spies, out GameObject owner);
            Actor enemy = MakeEnemy("enemy");

            Assert.That(registry.AmplifierFor(enemy), Is.EqualTo(1f).Within(Tolerance),
                "An unmarked enemy must not be amplified.");
            Assert.That(spies.ContainsKey(enemy), Is.False, "An unmarked enemy must have no slow handle.");

            UnityEngine.Object.DestroyImmediate(enemy.gameObject);
            UnityEngine.Object.DestroyImmediate(owner);
        }
    }
}
