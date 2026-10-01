using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure movement-blend normalization in <see cref="MovementBlend"/>.
    /// The locomotion blend is otherwise set on a live animator inside a scene; the invariant
    /// "the blend value is always within [0,1]" was extracted into plain C# so it can be
    /// property-checked without a Unity scene. FsCheck/CsCheck cannot be resolved on this machine,
    /// so the seeded <see cref="PropertyCheck"/> harness drives >= 100 deterministic generated
    /// cases and reports the exact failing input as a counterexample.
    /// </summary>
    public sealed class MovementBlendRangePropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 6
        // Movement blend is in [0,1]: for any non-negative current speed and positive max speed,
        // MovementBlend.Normalize returns a value in [0,1]; max speed <= 0 returns 0.
        // Validates: Requirements 15.2, 15.6, 16.5
        [Test]
        public void MovementBlendIsAlwaysWithinUnitRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float currentSpeed = RandomSpeed(rng);
                float maxSpeed = RandomMaxSpeed(rng);

                float blend = MovementBlend.Normalize(currentSpeed, maxSpeed);

                // R15.2/R16.5: the blend value is always within [0,1], even for degenerate or
                // out-of-range inputs (negative, zero, or current speed exceeding max speed).
                PropertyCheck.That(blend >= 0f && blend <= 1f,
                    $"current={currentSpeed}, max={maxSpeed}: blend {blend} fell outside [0,1]");

                // R15.6: a non-positive max speed yields exactly 0 (no divide-by-zero, in range).
                if (maxSpeed <= 0f)
                {
                    PropertyCheck.That(blend == 0f,
                        $"max={maxSpeed} <= 0 should yield blend 0 but was {blend}");
                }
                else
                {
                    // For a positive max speed the result matches current/max clamped to [0,1].
                    float expected = Mathf.Clamp01(currentSpeed / maxSpeed);
                    PropertyCheck.That(Mathf.Approximately(blend, expected),
                        $"current={currentSpeed}, max={maxSpeed}: blend {blend} != expected {expected}");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Current speeds spanning negative, zero, within range, and above a typical max speed so the
        /// clamp and divide-by-zero guards are exercised.
        /// </summary>
        private static float RandomSpeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return -5f + (float)rng.NextDouble() * 5f;    // negative .. 0
                case 1: return 0f;                                    // exactly zero
                case 2: return (float)rng.NextDouble() * 10f;         // 0 .. 10
                default: return (float)rng.NextDouble() * 1000f;      // large, may exceed max
            }
        }

        /// <summary>
        /// Max speeds spanning negative, zero, and positive so the non-positive guard (returns 0) and
        /// the normal normalization path are both covered.
        /// </summary>
        private static float RandomMaxSpeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return -3f + (float)rng.NextDouble() * 3f;    // negative .. 0
                case 1: return 0f;                                    // exactly zero
                default: return 0.01f + (float)rng.NextDouble() * 20f; // positive
            }
        }
    }
}
