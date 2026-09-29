using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the bounded, non-negative stance subtraction enforced by the pure,
    /// scene-free helper <see cref="StanceBreakBounds.SubtractStance(float, float, float)"/> — task 3.5
    /// of weapon-gameplay-swarm-rework.
    ///
    /// <see cref="StanceBreakBounds"/> is a static, MonoBehaviour-free helper, so the stance
    /// subtraction can be property-checked without a live Unity scene (the owning
    /// <c>CombatReactionController</c> subtracts through here). This project cannot resolve
    /// FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property
    /// (generating current stance, stance damage and multiplier across the [0.0; 5.0] window and
    /// beyond) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class StanceSubtractionBoundsPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 3: Subtração de postura limitada e não-negativa.
        // For every current stance value, StanceDamage and stance multiplier in [0.0; 5.0], the new
        // stance reserve equals max(0, current − StanceDamage × multiplier) and never drops below 0.
        // Validates: Requirements 2.4
        [Test]
        public void StanceSubtractionEqualsClampedFormulaAndStaysNonNegative()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Current stance and stance damage are non-negative reserves/values, generated across
                // a wide range including 0 so the floor and the exhaustion boundary are exercised.
                float currentStance = NextNonNegative(rng);
                float stanceDamage = NextNonNegative(rng);

                // Multiplier generated across [0.0; 5.0] most of the time, but also out of range so the
                // clamp is exercised: the formula must always use the clamped multiplier.
                float rawMultiplier = NextMultiplier(rng);
                float clampedMultiplier = StanceBreakBounds.ClampStanceDamageMultiplier(rawMultiplier);

                float result = StanceBreakBounds.SubtractStance(currentStance, stanceDamage, rawMultiplier);

                float expected = Mathf.Max(0f, currentStance - stanceDamage * clampedMultiplier);

                string state =
                    $"[current={currentStance} stanceDamage={stanceDamage} rawMultiplier={rawMultiplier} " +
                    $"clampedMultiplier={clampedMultiplier} => result={result} expected={expected}]";

                // R2.4: the multiplier used is clamped to [0.0; 5.0].
                PropertyCheck.That(
                    clampedMultiplier >= StanceBreakBounds.MinStanceDamageMultiplier &&
                    clampedMultiplier <= StanceBreakBounds.MaxStanceDamageMultiplier,
                    $"clamped multiplier must stay in [{StanceBreakBounds.MinStanceDamageMultiplier}; " +
                    $"{StanceBreakBounds.MaxStanceDamageMultiplier}] for {state}");

                // R2.4: the reserve never drops below 0.
                PropertyCheck.That(result >= 0f,
                    $"stance reserve must never drop below 0 for {state}");

                // R2.4: the reserve equals max(0, current − StanceDamage × clampedMultiplier).
                PropertyCheck.That(Mathf.Abs(result - expected) <= 1e-3f * (1f + Mathf.Abs(expected)),
                    $"stance reserve must equal max(0, current − StanceDamage × multiplier) for {state}");

                // The subtraction never increases the reserve above its starting value.
                PropertyCheck.That(result <= currentStance + 1e-3f * (1f + Mathf.Abs(currentStance)),
                    $"stance subtraction must never raise the reserve above the current value for {state}");
            });
        }

        /// <summary>
        /// A non-negative float spanning the range a caller might supply for the current stance
        /// reserve or the declared stance damage: 0, small, and large values (the request ctor already
        /// floors negatives to 0, so callers pass non-negative stance damage).
        /// </summary>
        private static float NextNonNegative(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;
                case 1: return NextFloat(rng, 0f, 1f);      // near the floor
                case 2: return NextFloat(rng, 0f, 100f);    // typical reserve range
                case 3: return NextFloat(rng, 0f, 10000f);  // large reserve
                default: return NextFloat(rng, 0f, 50f);    // around common values
            }
        }

        /// <summary>
        /// A multiplier spanning both the valid [0.0; 5.0] window and out-of-range values (negative and
        /// far above the cap) so the clamp inside <see cref="StanceBreakBounds.SubtractStance"/> is
        /// tested against inputs outside the allowed interval.
        /// </summary>
        private static float NextMultiplier(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return StanceBreakBounds.MinStanceDamageMultiplier; // 0
                case 1: return StanceBreakBounds.MaxStanceDamageMultiplier; // 5
                case 2: return NextFloat(rng, -1000f, 0f);  // below range
                case 3: return NextFloat(rng, 5f, 1000f);   // above range
                default: return NextFloat(                    // inside [0; 5]
                    rng,
                    StanceBreakBounds.MinStanceDamageMultiplier,
                    StanceBreakBounds.MaxStanceDamageMultiplier);
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
