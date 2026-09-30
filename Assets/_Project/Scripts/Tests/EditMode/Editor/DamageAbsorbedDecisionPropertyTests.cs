using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure absorption decision
    /// <see cref="Actor.WasFullyAbsorbed(float, float)"/> — task 8.1 of combat-balance-tuning
    /// (Requisito 7.3), backing the <c>DamageAbsorbed</c> event of task 6.1.
    ///
    /// <see cref="Actor.WasFullyAbsorbed(float, float)"/> is a static, side-effect-free decision so
    /// it can be property-checked without a live scene or a live <c>IDamageAbsorber</c>. The contract
    /// is: a hit is fully absorbed <b>iff</b> the absorber received a positive input
    /// (<c>absorberInput &gt; 0</c>) and returned a non-positive leftover (<c>leftover &lt;= 0</c>) —
    /// i.e. the absorber consumed everything and nothing reached health. This project cannot resolve
    /// FsCheck/CsCheck on this machine, so the seeded harness <see cref="PropertyCheck"/> drives
    /// >= 100 deterministic cases per property (with NaN/Infinity/extreme injections) and reports the
    /// exact failing pair as a counterexample.
    ///
    /// Validates: Requirements 7.3.
    /// </summary>
    public sealed class DamageAbsorbedDecisionPropertyTests
    {
        // True IFF input > 0 AND leftover <= 0. The property asserts equivalence against the
        // independently-computed reference predicate across the whole (input, leftover) space.
        [Test]
        public void FullyAbsorbedIffPositiveInputAndNoLeftover()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextSignedCandidate(rng);
                float leftover = NextSignedCandidate(rng);

                bool actual = Actor.WasFullyAbsorbed(input, leftover);
                bool expected = input > 0f && leftover <= 0f;

                PropertyCheck.That(actual == expected,
                    $"WasFullyAbsorbed({input:R}, {leftover:R}) returned {actual}, expected {expected} " +
                    $"(rule: input>0 && leftover<=0)");
            });
        }

        // Positive input with no leftover (leftover <= 0) must always report absorbed.
        [Test]
        public void PositiveInputAndNonPositiveLeftoverIsAbsorbed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextStrictlyPositive(rng);
                float leftover = NextNonPositive(rng);

                PropertyCheck.That(Actor.WasFullyAbsorbed(input, leftover),
                    $"positive input with non-positive leftover must be absorbed, " +
                    $"got false for input={input:R} leftover={leftover:R}");
            });
        }

        // Non-positive input (input <= 0) must never report absorbed, regardless of leftover.
        [Test]
        public void NonPositiveInputIsNeverAbsorbed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextNonPositive(rng);
                float leftover = NextSignedCandidate(rng);

                PropertyCheck.That(!Actor.WasFullyAbsorbed(input, leftover),
                    $"non-positive input must never be absorbed, " +
                    $"got true for input={input:R} leftover={leftover:R}");
            });
        }

        // Positive leftover (leftover > 0) must never report absorbed: something reached health.
        [Test]
        public void PositiveLeftoverIsNeverAbsorbed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = NextSignedCandidate(rng);
                float leftover = NextStrictlyPositive(rng);

                PropertyCheck.That(!Actor.WasFullyAbsorbed(input, leftover),
                    $"a positive leftover means damage reached health, must not be absorbed, " +
                    $"got true for input={input:R} leftover={leftover:R}");
            });
        }

        // ---- Generators ----
        // NaN comparisons are all false, so WasFullyAbsorbed(NaN, x) and WasFullyAbsorbed(x, NaN)
        // both fall out of the > 0 / <= 0 checks naturally; injecting NaN/Infinity/extremes keeps
        // the equivalence property honest against those edge inputs.

        /// <summary>Any sign: negatives, zero, positives, plus NaN/+/-Infinity/extremes.</summary>
        private static float NextSignedCandidate(System.Random rng)
        {
            switch (rng.Next(0, 8))
            {
                case 0: return 0f;
                case 1: return float.NaN;
                case 2: return float.PositiveInfinity;
                case 3: return float.NegativeInfinity;
                case 4: return -NextFloat(rng, 0f, 10000f);
                case 5: return NextFloat(rng, 0f, 10000f);
                case 6: return NextFloat(rng, 0f, 0.0001f);   // tiny positive near the boundary
                default: return -NextFloat(rng, 0f, 0.0001f); // tiny negative near the boundary
            }
        }

        /// <summary>Strictly positive values, occasionally +Infinity.</summary>
        private static float NextStrictlyPositive(System.Random rng)
        {
            if (rng.Next(0, 8) == 0) return float.PositiveInfinity;
            // Bias away from zero so the value is genuinely > 0.
            return NextFloat(rng, float.Epsilon, 10000f) + float.Epsilon;
        }

        /// <summary>Non-positive values (zero and negatives), occasionally -Infinity.</summary>
        private static float NextNonPositive(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return float.NegativeInfinity;
                default: return -NextFloat(rng, 0f, 10000f);
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
