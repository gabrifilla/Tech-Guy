using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure jittered-interval resolver <see cref="CadenceJitter"/>
    /// — task 1.6 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// The attack cadence is otherwise driven by live <c>EnemyAI</c> timing; these universal
    /// invariants were extracted into plain C# so they can be property-checked without a live Unity
    /// scene. FsCheck/CsCheck are not available on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class CadenceJitterBandPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 3
        // Cadence jitter stays in band, respects the floor, is always positive, and is deterministic
        // per seed: for any base interval, jitter fraction, floor, and seed, the effective interval
        // lies within [base*(1-j), base*(1+j)] (clamped jitter), is never below max(floor, Epsilon),
        // is strictly > 0, and the same (inputs, seed) always produces the same result. jitter == 0
        // returns the base interval exactly (floored).
        // Validates: Requirements 12.2, 12.3, 12.4, 12.5, 16.2, 16.4
        [Test]
        public void CadenceJitterStaysInBandRespectsFloorIsPositiveAndDeterministic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float baseInterval = RandomInterval(rng);
                float jitterFraction = RandomJitter(rng);
                float floor = RandomFloor(rng);
                int seed = rng.Next();

                float effectiveFloor = Mathf.Max(floor, Mathf.Epsilon);
                float clampedJitter = CadenceJitter.ClampJitter(jitterFraction);

                float result = CadenceJitter.Effective(baseInterval, jitterFraction, floor, new System.Random(seed));

                // R12.4 / R12.6: the interval is always strictly > 0 and never below the effective floor.
                PropertyCheck.That(result > 0f,
                    $"base={baseInterval}, jitter={jitterFraction}, floor={floor}: effective interval {result} was not > 0");
                PropertyCheck.That(result >= effectiveFloor - 1e-5f,
                    $"base={baseInterval}, jitter={jitterFraction}, floor={floor}: effective interval {result} fell below floor {effectiveFloor}");

                // R12.5: jitter == 0 returns the base interval exactly (floored to the effective floor).
                if (clampedJitter <= 0f)
                {
                    float expectedZeroJitter = Mathf.Max(baseInterval, effectiveFloor);
                    PropertyCheck.That(Mathf.Approximately(result, expectedZeroJitter),
                        $"base={baseInterval}, floor={floor}: zero-jitter interval {result} != expected {expectedZeroJitter}");
                }

                // R12.2 / R12.3: when jitter is active the sampled value stays inside the band
                // [base*(1-j), base*(1+j)] before the floor is applied. The floor can only raise the
                // result, so a result above the band's upper edge is a band violation; a result below
                // the band's lower edge is only acceptable because the floor raised it to the floor.
                if (clampedJitter > 0f)
                {
                    float safeBase = Mathf.Max(0f, baseInterval);
                    float bandMax = safeBase * (1f + clampedJitter);

                    PropertyCheck.That(result <= Mathf.Max(bandMax, effectiveFloor) + 1e-4f,
                        $"base={baseInterval}, jitter={jitterFraction}, floor={floor}: effective interval {result} exceeded band max {bandMax}");

                    // Below the band lower edge is only possible when the floor forced it up.
                    float bandMin = safeBase * (1f - clampedJitter);
                    PropertyCheck.That(result >= bandMin - 1e-4f || result >= effectiveFloor - 1e-5f,
                        $"base={baseInterval}, jitter={jitterFraction}, floor={floor}: effective interval {result} fell below band min {bandMin} without the floor explaining it");
                }

                // R16.2 / R16.4: deterministic per seed — identical inputs and the same seed reproduce
                // the exact same effective interval.
                float repeat = CadenceJitter.Effective(baseInterval, jitterFraction, floor, new System.Random(seed));
                PropertyCheck.That(result.Equals(repeat),
                    $"base={baseInterval}, jitter={jitterFraction}, floor={floor}, seed={seed}: non-deterministic result {result} vs {repeat}");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Base intervals spanning zero, small, and typical attack cadences.</summary>
        private static float RandomInterval(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // degenerate zero base
                case 1: return (float)rng.NextDouble() * 0.5f;       // very small
                default: return 0.5f + (float)rng.NextDouble() * 6f; // typical cadence
            }
        }

        /// <summary>Jitter fractions including out-of-range values to exercise defensive clamping (R16.6).</summary>
        private static float RandomJitter(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;                                   // no variation (R12.5)
                case 1: return -0.3f + (float)rng.NextDouble() * 0.3f; // negative (clamps to 0)
                case 2: return CadenceJitter.MaxJitter;              // exactly the max
                case 3: return 0.5f + (float)rng.NextDouble() * 1f;  // above max (clamps to 0.5)
                default: return (float)rng.NextDouble() * CadenceJitter.MaxJitter; // in-range
            }
        }

        /// <summary>Floors spanning negative, zero, and positive to exercise the Epsilon/positive guarantee.</summary>
        private static float RandomFloor(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return -1f + (float)rng.NextDouble();        // negative (clamps up to Epsilon)
                case 1: return 0f;                                   // zero (clamps up to Epsilon)
                default: return (float)rng.NextDouble() * 3f;        // positive floor
            }
        }
    }
}
