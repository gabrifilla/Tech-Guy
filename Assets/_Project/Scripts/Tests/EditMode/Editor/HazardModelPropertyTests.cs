using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure hazard-zone logic in <see cref="HazardZoneModel"/>
    /// (<see cref="HazardTickTimer"/> and <see cref="HazardSlow"/>) — task 8.2 of
    /// enemy-swarm-core-archetypes.
    ///
    /// The hazard zone is otherwise scene/trigger driven; its two universal invariants — periodic
    /// interval-driven damage that stops on exit, and a movement-slow round trip that is a speed
    /// identity — were extracted into plain C# so they can be property-checked without a live Unity
    /// scene. The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed
    /// seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per
    /// property and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class HazardModelPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 10: Hazard periodic damage and slow round-trip.
        // Periodic-damage half: while the player occupies the zone, a random sequence of Advance(dt)
        // steps yields a total number of whole ticks equal to floor(totalElapsed / interval); no tick
        // is ever emitted on Enter (occupancy must accrue a full interval first); ticks are strictly
        // interval-driven, not per-frame (a step shorter than the interval owes 0). After Exit() the
        // accumulator is cleared and no further ticks are owed once outside.
        // Validates: Requirements 11.5, 11.6, 11.7, 11.8
        [Test]
        public void PeriodicDamageIsIntervalDrivenAndStopsOnExit()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float interval = RandomInterval(rng);
                var timer = new HazardTickTimer(interval);
                float clampedInterval = timer.Interval;

                // R11.5: the interval is band-clamped to 0.25–1.0s.
                PropertyCheck.That(
                    clampedInterval >= HazardZoneModel.MinDamageInterval - 1e-6f &&
                    clampedInterval <= HazardZoneModel.MaxDamageInterval + 1e-6f,
                    $"interval={interval}: clamped interval {clampedInterval} outside [0.25,1.0]");

                // R11.5: no tick is owed before entering (unoccupied Advance yields nothing).
                PropertyCheck.That(timer.Advance(RandomStep(rng, clampedInterval)) == 0,
                    "unoccupied timer emitted a tick before Enter()");

                // R11.5: Enter() itself never emits a tick and starts a fresh interval.
                timer.Enter();
                PropertyCheck.That(timer.Occupied, "Enter() did not mark the zone occupied");
                PropertyCheck.That(timer.Pending == 0f, "Enter() did not reset the accumulator to 0");

                // Feed a random sequence of Advance(dt) steps and count whole ticks.
                int steps = rng.Next(1, 40);
                float totalElapsed = 0f;
                int totalTicks = 0;
                for (int s = 0; s < steps; s++)
                {
                    float dt = RandomStep(rng, clampedInterval);
                    int ticks = timer.Advance(dt);

                    // R11.5: interval-driven, not per-frame — a lone sub-interval step owes 0.
                    PropertyCheck.That(ticks >= 0, $"Advance returned a negative tick count {ticks}");

                    totalElapsed += Math.Max(0f, dt);
                    totalTicks += ticks;
                }

                // R11.5: total whole ticks equals floor(totalElapsed / interval), within a rounding
                // tolerance of one tick for accumulated float error over many steps.
                int expectedTicks = Mathf.FloorToInt(totalElapsed / clampedInterval);
                PropertyCheck.That(Math.Abs(totalTicks - expectedTicks) <= 1,
                    $"interval={clampedInterval}: emitted {totalTicks} ticks over elapsed {totalElapsed}, " +
                    $"expected floor {expectedTicks}");

                // The leftover accumulator is always a partial interval (never a whole owed tick).
                PropertyCheck.That(timer.Pending < clampedInterval + 1e-4f,
                    $"pending accumulator {timer.Pending} >= interval {clampedInterval} (owed but unemitted tick)");

                // R11.6: after Exit() no further ticks are owed and the accumulator is cleared, even
                // for a large step that would otherwise cross several intervals.
                timer.Exit();
                PropertyCheck.That(!timer.Occupied, "Exit() did not mark the zone unoccupied");
                PropertyCheck.That(timer.Pending == 0f, "Exit() did not clear the accumulator");
                PropertyCheck.That(timer.Advance(clampedInterval * 10f + 5f) == 0,
                    "timer emitted a tick after Exit() (owed ticks once outside the zone)");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 10: Hazard periodic damage and slow round-trip.
        // Slow half: for a fraction clamped to the 20%–60% band the speed multiplier lies in
        // [0.4, 0.8], applying the slow strictly reduces any positive base speed, and an
        // enter-then-exit round trip restores the exact pre-slow speed for any base speed (a speed
        // identity), regardless of the slow fraction.
        // Validates: Requirements 11.7, 11.8
        [Test]
        public void SlowReducesSpeedInBandAndRoundTripIsIdentity()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float fraction = RandomFraction(rng);
                var slow = new HazardSlow(fraction);

                // R11.7: the fraction is band-clamped to 20%–60%.
                PropertyCheck.That(
                    slow.Fraction >= HazardZoneModel.MinSlowFraction - 1e-6f &&
                    slow.Fraction <= HazardZoneModel.MaxSlowFraction + 1e-6f,
                    $"fraction={fraction}: clamped fraction {slow.Fraction} outside [0.2,0.6]");

                // R11.7: multiplier = 1 - fraction lies in [0.4, 0.8].
                PropertyCheck.That(
                    slow.SpeedMultiplier >= 0.4f - 1e-6f && slow.SpeedMultiplier <= 0.8f + 1e-6f,
                    $"fraction={slow.Fraction}: multiplier {slow.SpeedMultiplier} outside [0.4,0.8]");

                float baseSpeed = RandomSpeed(rng);

                // R11.7: applying the slow reduces a positive base speed (and never increases it).
                float slowed = slow.ApplyTo(baseSpeed);
                if (baseSpeed > 0f)
                {
                    PropertyCheck.That(slowed < baseSpeed,
                        $"fraction={slow.Fraction} base={baseSpeed}: slowed speed {slowed} not below base");
                }
                PropertyCheck.That(slowed <= baseSpeed + 1e-6f,
                    $"fraction={slow.Fraction} base={baseSpeed}: slow increased speed to {slowed}");

                // R11.8: enter-then-exit restores the exact pre-slow value (a speed identity).
                float restored = slow.Restore(baseSpeed);
                PropertyCheck.That(restored == baseSpeed,
                    $"fraction={slow.Fraction} base={baseSpeed}: Restore returned {restored}, not the exact base");

                float roundTrip = slow.RoundTrip(baseSpeed);
                PropertyCheck.That(roundTrip == baseSpeed,
                    $"fraction={slow.Fraction} base={baseSpeed}: RoundTrip returned {roundTrip}, not the exact base");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Intervals spanning below, inside, and above the 0.25–1.0s band, plus the exact edges.</summary>
        private static float RandomInterval(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -0.5f + (float)rng.NextDouble() * 0.5f;      // negative .. 0
                case 1: return (float)rng.NextDouble() * 0.25f;            // 0 .. below floor
                case 2: return 0.25f;                                      // exact floor
                case 3: return 1.0f;                                       // exact ceiling
                case 4: return 1.0f + (float)rng.NextDouble() * 3f;        // above ceiling
                default: return 0.25f + (float)rng.NextDouble() * 0.75f;   // inside the band
            }
        }

        /// <summary>Advance steps spanning sub-interval, near-interval, and multi-interval sizes.</summary>
        private static float RandomStep(System.Random rng, float interval)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return (float)rng.NextDouble() * interval;             // sub-interval (owes 0 alone)
                case 1: return interval;                                       // exactly one interval
                case 2: return interval * (1f + (float)rng.NextDouble() * 4f); // spans several intervals
                default: return (float)rng.NextDouble() * interval * 2f;       // mixed
            }
        }

        /// <summary>Slow fractions spanning below, inside, and above the 20%–60% band, plus the edges.</summary>
        private static float RandomFraction(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -0.2f + (float)rng.NextDouble() * 0.2f;   // negative .. 0
                case 1: return (float)rng.NextDouble() * 0.20f;         // 0 .. below floor
                case 2: return 0.20f;                                   // exact floor
                case 3: return 0.60f;                                   // exact ceiling
                case 4: return 0.60f + (float)rng.NextDouble() * 0.5f;  // above ceiling
                default: return 0.20f + (float)rng.NextDouble() * 0.40f; // inside the band
            }
        }

        /// <summary>Base speeds spanning zero, small, and large positive values.</summary>
        private static float RandomSpeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return (float)rng.NextDouble() * 1f;    // small
                case 2: return 1f + (float)rng.NextDouble() * 9f; // typical agent speeds
                default: return 10f + (float)rng.NextDouble() * 90f; // large
            }
        }
    }
}
