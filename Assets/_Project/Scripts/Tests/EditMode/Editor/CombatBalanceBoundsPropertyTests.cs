using UnityEngine;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free clamps in
    /// <see cref="CombatBalanceBounds"/> — task 8.1 of combat-balance-tuning (Requisito 7.3).
    ///
    /// <see cref="CombatBalanceBounds"/> is a static class with fixed, data-driven ranges and no
    /// <see cref="MonoBehaviour"/> dependency, so every clamp can be property-checked without a live
    /// Unity scene. This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property,
    /// spanning the in-range / below / above / NaN / Infinity / extreme input space, and reports the
    /// exact failing input as a counterexample.
    ///
    /// Validates: Requirements 7.3 (cobertura para os clamps dos knobs).
    /// </summary>
    public sealed class CombatBalanceBoundsPropertyTests
    {
        // ---- ClampProjectileSweepRadius: result always in [0.05, 1.0]; inside passes through;
        // below -> min; above -> max. ----
        [Test]
        public void ClampProjectileSweepRadiusStaysInRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextRadiusCandidate(rng);
                float result = CombatBalanceBounds.ClampProjectileSweepRadius(input);

                string state = $"[input={input:R} result={result:R}]";

                // The clamp is only defined for finite inputs; NaN is not a meaningful sweep radius,
                // so Mathf.Clamp's NaN behaviour is out of scope here (generator excludes NaN).
                PropertyCheck.That(
                    result >= CombatBalanceBounds.MinProjectileSweepRadius &&
                    result <= CombatBalanceBounds.MaxProjectileSweepRadius,
                    $"sweep radius must land in [{CombatBalanceBounds.MinProjectileSweepRadius}; " +
                    $"{CombatBalanceBounds.MaxProjectileSweepRadius}] for {state}");

                if (input >= CombatBalanceBounds.MinProjectileSweepRadius &&
                    input <= CombatBalanceBounds.MaxProjectileSweepRadius)
                {
                    // In-range values pass through unchanged.
                    PropertyCheck.That(result == input,
                        $"an in-range radius must pass through unchanged for {state}");
                }
                else if (input < CombatBalanceBounds.MinProjectileSweepRadius)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MinProjectileSweepRadius,
                        $"a below-range radius must clamp to the min for {state}");
                }
                else // input > Max (covers PositiveInfinity)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MaxProjectileSweepRadius,
                        $"an above-range radius must clamp to the max for {state}");
                }
            });
        }

        // ---- ClampHitboxDimension: always >= MinHitboxDimension, strictly positive;
        // NaN and non-positive -> MinHitboxDimension; finite in-range passes through. ----
        [Test]
        public void ClampHitboxDimensionIsStrictlyPositive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextDimensionCandidate(rng);
                float result = CombatBalanceBounds.ClampHitboxDimension(input);

                string state = $"[input={input:R} result={result:R}]";

                PropertyCheck.That(result >= CombatBalanceBounds.MinHitboxDimension,
                    $"hitbox dimension must stay >= {CombatBalanceBounds.MinHitboxDimension} for {state}");
                PropertyCheck.That(result > 0f,
                    $"hitbox dimension must be strictly positive for {state}");
                PropertyCheck.That(!float.IsNaN(result),
                    $"hitbox dimension must never be NaN for {state}");

                if (float.IsNaN(input) || input < CombatBalanceBounds.MinHitboxDimension)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MinHitboxDimension,
                        $"NaN / non-positive / below-min dimension must snap to the min for {state}");
                }
                else
                {
                    // Finite, >= min (covers PositiveInfinity, which is > min and passes through).
                    PropertyCheck.That(result == input,
                        $"an in-range dimension must pass through unchanged for {state}");
                }
            });
        }

        // ---- ClampHitboxSize: each component clamped strictly positive (reuses ClampHitboxDimension
        // per axis). ----
        [Test]
        public void ClampHitboxSizeClampsEachAxisStrictlyPositive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var input = new Vector3(
                    NextDimensionCandidate(rng),
                    NextDimensionCandidate(rng),
                    NextDimensionCandidate(rng));
                Vector3 result = CombatBalanceBounds.ClampHitboxSize(input);

                string state = $"[input={input:R} result={result:R}]";

                foreach (float axis in new[] { result.x, result.y, result.z })
                {
                    PropertyCheck.That(axis >= CombatBalanceBounds.MinHitboxDimension && axis > 0f,
                        $"every hitbox-size axis must be strictly positive (>= {CombatBalanceBounds.MinHitboxDimension}) for {state}");
                    PropertyCheck.That(!float.IsNaN(axis),
                        $"no hitbox-size axis may be NaN for {state}");
                }

                // Each axis must match the per-axis clamp exactly (proves it reuses ClampHitboxDimension).
                PropertyCheck.That(
                    result.x == CombatBalanceBounds.ClampHitboxDimension(input.x) &&
                    result.y == CombatBalanceBounds.ClampHitboxDimension(input.y) &&
                    result.z == CombatBalanceBounds.ClampHitboxDimension(input.z),
                    $"each axis must equal the per-axis dimension clamp for {state}");
            });
        }

        // ---- ClampSkillManaCostMultiplier: always in [0.1, 2.0]; inside passes through;
        // below -> min; above -> max. ----
        [Test]
        public void ClampSkillManaCostMultiplierStaysInRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextMultiplierCandidate(rng);
                float result = CombatBalanceBounds.ClampSkillManaCostMultiplier(input);

                string state = $"[input={input:R} result={result:R}]";

                PropertyCheck.That(
                    result >= CombatBalanceBounds.MinSkillManaCostMultiplier &&
                    result <= CombatBalanceBounds.MaxSkillManaCostMultiplier,
                    $"mana multiplier must land in [{CombatBalanceBounds.MinSkillManaCostMultiplier}; " +
                    $"{CombatBalanceBounds.MaxSkillManaCostMultiplier}] for {state}");

                if (input >= CombatBalanceBounds.MinSkillManaCostMultiplier &&
                    input <= CombatBalanceBounds.MaxSkillManaCostMultiplier)
                {
                    PropertyCheck.That(result == input,
                        $"an in-range multiplier must pass through unchanged for {state}");
                }
                else if (input < CombatBalanceBounds.MinSkillManaCostMultiplier)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MinSkillManaCostMultiplier,
                        $"a below-range multiplier must clamp to the min for {state}");
                }
                else
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MaxSkillManaCostMultiplier,
                        $"an above-range multiplier must clamp to the max for {state}");
                }
            });
        }

        // ---- ClampBaseHealth: always strictly > 0 (>= MinBaseHealth); NaN / non-positive ->
        // MinBaseHealth; finite positive passes through. ----
        [Test]
        public void ClampBaseHealthIsStrictlyPositive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextDimensionCandidate(rng);
                float result = CombatBalanceBounds.ClampBaseHealth(input);

                string state = $"[input={input:R} result={result:R}]";

                PropertyCheck.That(result >= CombatBalanceBounds.MinBaseHealth && result > 0f,
                    $"base health must be strictly positive (>= {CombatBalanceBounds.MinBaseHealth}) for {state}");
                PropertyCheck.That(!float.IsNaN(result),
                    $"base health must never be NaN for {state}");

                if (float.IsNaN(input) || input < CombatBalanceBounds.MinBaseHealth)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MinBaseHealth,
                        $"NaN / non-positive / below-min health must snap to the min for {state}");
                }
                else
                {
                    PropertyCheck.That(result == input,
                        $"an in-range health must pass through unchanged for {state}");
                }
            });
        }

        // ---- ClampArmor: always >= 0 (MinArmor); NaN / negative -> 0; finite non-negative passes
        // through. ----
        [Test]
        public void ClampArmorIsNonNegative()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextArmorCandidate(rng);
                float result = CombatBalanceBounds.ClampArmor(input);

                string state = $"[input={input:R} result={result:R}]";

                PropertyCheck.That(result >= CombatBalanceBounds.MinArmor,
                    $"armor must stay >= {CombatBalanceBounds.MinArmor} for {state}");
                PropertyCheck.That(!float.IsNaN(result),
                    $"armor must never be NaN for {state}");

                if (float.IsNaN(input) || input < CombatBalanceBounds.MinArmor)
                {
                    PropertyCheck.That(result == CombatBalanceBounds.MinArmor,
                        $"NaN / negative armor must snap to 0 for {state}");
                }
                else
                {
                    PropertyCheck.That(result == input,
                        $"a non-negative armor must pass through unchanged for {state}");
                }
            });
        }

        // ---- Generators ----
        // Each generator mixes uniform random values across and around the target range with
        // occasional Infinity/NaN/extreme injections, so every property covers the
        // in-range / below / above / NaN / Infinity edge cases across >= 100 seeded cases.

        /// <summary>Finite radius candidates spanning below/inside/above the sweep range (no NaN;
        /// NaN is not a meaningful sweep radius and Mathf.Clamp's NaN result is out of scope).</summary>
        private static float NextRadiusCandidate(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return float.PositiveInfinity;                 // above -> max
                case 1: return -NextFloat(rng, 0f, 100f);              // below (negative) -> min
                case 2: return NextFloat(rng, 0f, CombatBalanceBounds.MinProjectileSweepRadius); // below min
                case 3: return NextFloat(rng, CombatBalanceBounds.MaxProjectileSweepRadius, 1000f); // above max
                default:
                    // Inside the valid range (inclusive of the bounds).
                    return NextFloat(rng,
                        CombatBalanceBounds.MinProjectileSweepRadius,
                        CombatBalanceBounds.MaxProjectileSweepRadius);
            }
        }

        /// <summary>Dimension/health candidates: NaN, +/-Infinity, negatives, zero, sub-min, and
        /// valid positives, exercising the strictly-positive snap-to-min branch.</summary>
        private static float NextDimensionCandidate(System.Random rng)
        {
            switch (rng.Next(0, 8))
            {
                case 0: return float.NaN;                              // -> min
                case 1: return float.PositiveInfinity;                 // finite check: > min, passes through
                case 2: return float.NegativeInfinity;                 // < min -> min
                case 3: return 0f;                                     // non-positive -> min
                case 4: return -NextFloat(rng, 0f, 1000f);             // negative -> min
                case 5: return NextFloat(rng, 0f, CombatBalanceBounds.MinHitboxDimension); // sub-min -> min
                default:
                    return NextFloat(rng, CombatBalanceBounds.MinHitboxDimension, 500f); // valid
            }
        }

        /// <summary>Mana-multiplier candidates spanning below/inside/above the [0.1; 2.0] range,
        /// plus Infinity (no NaN; Mathf.Clamp's NaN behaviour is out of scope).</summary>
        private static float NextMultiplierCandidate(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return float.PositiveInfinity;                 // above -> max
                case 1: return -NextFloat(rng, 0f, 10f);               // below (negative) -> min
                case 2: return NextFloat(rng, 0f, CombatBalanceBounds.MinSkillManaCostMultiplier); // below min
                case 3: return NextFloat(rng, CombatBalanceBounds.MaxSkillManaCostMultiplier, 100f); // above max
                default:
                    return NextFloat(rng,
                        CombatBalanceBounds.MinSkillManaCostMultiplier,
                        CombatBalanceBounds.MaxSkillManaCostMultiplier);
            }
        }

        /// <summary>Armor candidates: NaN, +/-Infinity, negatives, zero, and valid non-negatives,
        /// exercising the non-negative snap-to-zero branch.</summary>
        private static float NextArmorCandidate(System.Random rng)
        {
            switch (rng.Next(0, 7))
            {
                case 0: return float.NaN;                              // -> 0
                case 1: return float.NegativeInfinity;                 // < 0 -> 0
                case 2: return float.PositiveInfinity;                 // >= 0, passes through
                case 3: return -NextFloat(rng, 0f, 1000f);             // negative -> 0
                case 4: return 0f;                                     // boundary, passes through
                default:
                    return NextFloat(rng, 0f, 1000f);                  // valid non-negative
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
