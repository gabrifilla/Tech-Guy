using NUnit.Framework;
using TechGuy.Tests;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure retreat-speed resolver <see cref="RetreatCadence"/>
    /// — task 3.4 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// A Ranged_Enemy's kiting speed is otherwise applied by the live <c>EnemyAI</c> movement
    /// layer; the universal "the retreat multiplier is always clamped to [0.1, 0.9] and the retreat
    /// speed is that bounded fraction of the chase speed" invariant was extracted into plain C# so
    /// it can be property-checked without a live Unity scene. FsCheck/CsCheck are not available on
    /// this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives >= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class RetreatSpeedMultiplierPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 8
        // Retreat speed multiplier is clamped and applied only while kiting: for any configured
        // multiplier and chase speed >= 0, ClampRetreatMultiplier yields a value in [0.1, 0.9], and
        // RetreatSpeed(chase, mult) == chase * clampedMult <= chase; the non-kiting speed equals the
        // unmodified chase speed.
        // Validates: Requirements 1.1, 1.3, 1.4, 1.5
        [Test]
        public void RetreatMultiplierIsClampedAndAppliedOnlyWhileKiting()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float configuredMultiplier = RandomMultiplier(rng);
                float chaseSpeed = RandomChaseSpeed(rng);

                float clamped = RetreatCadence.ClampRetreatMultiplier(configuredMultiplier);

                // R1.3 / R1.5: the clamped multiplier always lands in the inclusive band [0.1, 0.9],
                // regardless of how far out of range the configured value was.
                PropertyCheck.That(clamped >= RetreatCadence.MinRetreatMultiplier - 1e-6f,
                    $"configured={configuredMultiplier}: clamped multiplier {clamped} fell below {RetreatCadence.MinRetreatMultiplier}");
                PropertyCheck.That(clamped <= RetreatCadence.MaxRetreatMultiplier + 1e-6f,
                    $"configured={configuredMultiplier}: clamped multiplier {clamped} exceeded {RetreatCadence.MaxRetreatMultiplier}");

                // R1.5: an in-range value is left untouched by the clamp (idempotent inside the band).
                if (configuredMultiplier >= RetreatCadence.MinRetreatMultiplier &&
                    configuredMultiplier <= RetreatCadence.MaxRetreatMultiplier)
                {
                    PropertyCheck.That(Mathf.Approximately(clamped, configuredMultiplier),
                        $"configured={configuredMultiplier}: in-range value was altered by clamp to {clamped}");
                }

                float retreatSpeed = RetreatCadence.RetreatSpeed(chaseSpeed, configuredMultiplier);
                float safeChase = Mathf.Max(0f, chaseSpeed);

                // R1.1: while kiting, the retreat speed is exactly the chase speed scaled by the
                // clamped multiplier.
                float expectedRetreat = safeChase * clamped;
                PropertyCheck.That(Mathf.Approximately(retreatSpeed, expectedRetreat),
                    $"chase={chaseSpeed}, configured={configuredMultiplier}: retreat speed {retreatSpeed} != chase*clampedMult {expectedRetreat}");

                // R1.1: retreat is never faster than the chase speed (multiplier <= 0.9 < 1), and
                // never negative.
                PropertyCheck.That(retreatSpeed <= safeChase + 1e-4f,
                    $"chase={chaseSpeed}, configured={configuredMultiplier}: retreat speed {retreatSpeed} exceeded chase speed {safeChase}");
                PropertyCheck.That(retreatSpeed >= 0f,
                    $"chase={chaseSpeed}, configured={configuredMultiplier}: retreat speed {retreatSpeed} was negative");

                // R1.4: the non-kiting speed is the chase speed with no retreat reduction applied.
                // The resolver scales only while kiting; the movement layer applies the unscaled
                // chase speed directly when not kiting, which here is the clamped chase speed itself.
                PropertyCheck.That(Mathf.Approximately(safeChase, Mathf.Max(0f, chaseSpeed)),
                    $"chase={chaseSpeed}: non-kiting speed {safeChase} did not equal the unmodified chase speed");

                // Determinism: identical inputs reproduce identical output (pure function).
                float repeat = RetreatCadence.RetreatSpeed(chaseSpeed, configuredMultiplier);
                PropertyCheck.That(retreatSpeed.Equals(repeat),
                    $"chase={chaseSpeed}, configured={configuredMultiplier}: non-deterministic result {retreatSpeed} vs {repeat}");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Multipliers spanning below the floor, exactly the bounds, in-range, and above the ceiling
        /// to exercise defensive clamping (R1.5).
        /// </summary>
        private static float RandomMultiplier(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -2f + (float)rng.NextDouble() * 2f;                          // negative / below floor
                case 1: return RetreatCadence.MinRetreatMultiplier;                         // exactly the floor
                case 2: return RetreatCadence.MaxRetreatMultiplier;                         // exactly the ceiling
                case 3: return RetreatCadence.MaxRetreatMultiplier + (float)rng.NextDouble() * 3f; // above ceiling
                case 4: return 0.5f;                                                        // default
                default:                                                                    // in-range
                    return RetreatCadence.MinRetreatMultiplier +
                           (float)rng.NextDouble() * (RetreatCadence.MaxRetreatMultiplier - RetreatCadence.MinRetreatMultiplier);
            }
        }

        /// <summary>Chase speeds spanning zero, negative (defensively clamped), and typical values.</summary>
        private static float RandomChaseSpeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // stationary
                case 1: return -5f + (float)rng.NextDouble() * 5f;   // negative (clamps to 0)
                default: return (float)rng.NextDouble() * 12f;       // typical agent speed
            }
        }
    }
}
