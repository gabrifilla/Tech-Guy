using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure Spawner living-count arithmetic in
    /// <see cref="SpawnerLivingCount"/> — task 8.7 of enemy-swarm-core-archetypes.
    ///
    /// The <see cref="SpawnerBehavior"/> is scene/MonoBehaviour driven (timer, NavMesh sampling,
    /// instantiation, <c>Actor.Died</c> subscription), so its concurrency invariant was extracted
    /// into plain C# so it can be property-checked without a live Unity scene. The project cannot
    /// resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SpawnerLivingCountPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 9: Spawner living count never exceeds its cap.
        // For any configured cap and any interleaving of spawn/death operations, the living count
        // stays within [0, cap]; a spawn attempt at the cap is refused and does not increment (no
        // spawn at cap); every recorded death decrements by exactly one and never below zero (one
        // decrement per death); and the effective cap is always clamped to [1, 100]. The property is
        // checked against an independent expected count tracked in the test harness.
        // Validates: Requirements 16.2, 16.3, 16.4, 16.5
        [Test]
        public void LivingCountStaysWithinCapAcrossSpawnDeathInterleavings()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int authoredCap = RandomCap(rng);
                var model = new SpawnerLivingCount(authoredCap);

                // R16.3: the effective cap is clamped into the valid [1, 100] range regardless of the
                // authored value (negative, zero, in-range, or above the max).
                int expectedCap = Mathf.Clamp(authoredCap, SpawnerLivingCount.MinCap, SpawnerLivingCount.MaxCap);
                PropertyCheck.That(model.Cap == expectedCap,
                    $"authoredCap={authoredCap}: effective cap {model.Cap} != clamped {expectedCap}");
                PropertyCheck.That(model.Cap >= SpawnerLivingCount.MinCap && model.Cap <= SpawnerLivingCount.MaxCap,
                    $"authoredCap={authoredCap}: effective cap {model.Cap} outside [1,100]");
                PropertyCheck.That(model.Living == 0,
                    $"authoredCap={authoredCap}: living count did not start at zero (was {model.Living})");

                // An independent expected count mirrors the same rules so the model is validated
                // against a second, hand-tracked source of truth rather than only against itself.
                int expectedLiving = 0;

                int operations = rng.Next(20, 120);
                for (int op = 0; op < operations; op++)
                {
                    // CanSpawn must agree with "expected count strictly below the cap" (R16.4).
                    bool expectedCanSpawn = expectedLiving < model.Cap;
                    PropertyCheck.That(model.CanSpawn == expectedCanSpawn,
                        $"cap={model.Cap}, living={model.Living}: CanSpawn {model.CanSpawn} != expected {expectedCanSpawn}");

                    // Bias slightly toward spawning so the cap is actually reached and the at-cap
                    // refusal path is exercised; still interleave deaths freely.
                    bool trySpawn = rng.Next(0, 3) != 0; // ~2/3 spawn attempts, ~1/3 death attempts

                    if (trySpawn)
                    {
                        int livingBefore = model.Living;
                        bool atCap = model.Living >= model.Cap;
                        bool recorded = model.RecordSpawn();

                        if (atCap)
                        {
                            // R16.2/R16.4: no spawn at cap — the attempt is refused and the count is
                            // unchanged, so the count can never exceed the cap.
                            PropertyCheck.That(!recorded,
                                $"cap={model.Cap}: RecordSpawn returned true while at cap (living was {livingBefore})");
                            PropertyCheck.That(model.Living == livingBefore,
                                $"cap={model.Cap}: at-cap spawn changed living {livingBefore} -> {model.Living}");
                        }
                        else
                        {
                            // Below the cap a spawn is accepted and increments by exactly one.
                            PropertyCheck.That(recorded,
                                $"cap={model.Cap}: RecordSpawn refused below cap (living was {livingBefore})");
                            PropertyCheck.That(model.Living == livingBefore + 1,
                                $"cap={model.Cap}: spawn changed living by !=1 ({livingBefore} -> {model.Living})");
                            expectedLiving++;
                        }
                    }
                    else
                    {
                        int livingBefore = model.Living;
                        bool wasZero = model.Living <= 0;
                        bool recorded = model.RecordDeath();

                        if (wasZero)
                        {
                            // R16.5: a death at zero is a no-op — the count never goes below zero.
                            PropertyCheck.That(!recorded,
                                $"cap={model.Cap}: RecordDeath returned true at zero living");
                            PropertyCheck.That(model.Living == 0,
                                $"cap={model.Cap}: death drove living below zero (now {model.Living})");
                        }
                        else
                        {
                            // R16.5: one decrement per death — exactly one, never below zero.
                            PropertyCheck.That(recorded,
                                $"cap={model.Cap}: RecordDeath refused with living {livingBefore}");
                            PropertyCheck.That(model.Living == livingBefore - 1,
                                $"cap={model.Cap}: death changed living by !=-1 ({livingBefore} -> {model.Living})");
                            expectedLiving--;
                        }
                    }

                    // R16.2: after every operation the living count stays within [0, cap] and matches
                    // the independent expected count.
                    PropertyCheck.That(model.Living >= 0 && model.Living <= model.Cap,
                        $"cap={model.Cap}: living {model.Living} escaped [0,{model.Cap}] after op {op}");
                    PropertyCheck.That(model.Living == expectedLiving,
                        $"cap={model.Cap}: living {model.Living} diverged from expected {expectedLiving} after op {op}");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Authored caps spanning below the minimum (negative / zero), the exact bounds, in-range,
        /// and above the maximum so the clamp to [1, 100] is exercised on both sides.
        /// </summary>
        private static int RandomCap(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return rng.Next(-10, 1);   // negative .. 0 (below MinCap)
                case 1: return SpawnerLivingCount.MinCap;                  // exactly 1
                case 2: return SpawnerLivingCount.MaxCap;                  // exactly 100
                case 3: return rng.Next(101, 200); // above MaxCap
                // Small in-range caps make it cheap to actually hit the cap during the run.
                case 4: return rng.Next(1, 6);
                default: return rng.Next(1, 101);  // anywhere in range
            }
        }
    }
}
