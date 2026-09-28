using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R10 Property 24 of modifier-synergies-theme17:
    /// escalation tier mapping and monotonicity.
    ///
    /// <see cref="SystemBreakState"/> is a plain C# class (no <c>MonoBehaviour</c>) so this test
    /// exercises the pure escalation logic directly — no scene, no GameObject, no reflection.
    /// The property spans two claims from the design:
    ///   1. Mapping: for any interacting-modifier count c, the tier equals the number of thresholds
    ///      in {3,6,9} not exceeding c (threshold &lt;= c, equivalently c &gt;= threshold), and a
    ///      freshly-evaluated <see cref="SystemBreakState"/> reports that same tier.
    ///   2. Monotonicity: for any two tiers a &lt; b, presented intensity and frequency at b are
    ///      &gt;= those at a (non-decreasing with tier).
    ///
    /// Thresholds are duplicated here from the (private) definition so the test pins the intended
    /// {3,6,9} contract independently rather than reading the implementation's own array.
    /// </summary>
    public sealed class SystemBreakEscalationTierTests
    {
        // Mirror of SystemBreakState's intended thresholds (R10.1). Kept local so the test asserts
        // the contract rather than echoing whatever the implementation happens to hold.
        private static readonly int[] ExpectedThresholds = { 3, 6, 9 };

        private static int ExpectedTier(int count)
        {
            int tier = 0;
            for (int i = 0; i < ExpectedThresholds.Length; i++)
                if (count >= ExpectedThresholds[i]) tier++;
            return tier;
        }

        // Feature: modifier-synergies-theme17, Property 24: tier = count of thresholds in {3,6,9}
        // not exceeding c, and a fresh SystemBreakState.Evaluate reports that same tier.
        // Validates: Requirements 10.1
        [Test]
        public void TierEqualsCountOfThresholdsNotExceedingCount()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Cover negatives (clamped to 0), the exact boundaries, and well past the top tier.
                int count = rng.Next(-5, 40);

                int expected = ExpectedTier(count < 0 ? 0 : count);

                int viaStatic = SystemBreakState.TierFor(count);
                PropertyCheck.That(viaStatic == expected,
                    $"count={count}: TierFor returned {viaStatic}, expected {expected}");

                // Tier is bounded to 0..MaxTier for every input.
                PropertyCheck.That(viaStatic >= 0 && viaStatic <= SystemBreakState.MaxTier,
                    $"count={count}: tier {viaStatic} out of range 0..{SystemBreakState.MaxTier}");

                // A fresh state (no sink) must land on the same tier after evaluating the count.
                var state = new SystemBreakState();
                int viaEvaluate = state.Evaluate(count);
                PropertyCheck.That(viaEvaluate == expected,
                    $"count={count}: Evaluate returned {viaEvaluate}, expected {expected}");
                PropertyCheck.That(state.Tier == expected,
                    $"count={count}: Tier property {state.Tier}, expected {expected}");
            });
        }

        // Feature: modifier-synergies-theme17, Property 24: intensity and frequency are
        // non-decreasing across tiers a < b (higher tiers present at >= intensity/frequency).
        // Validates: Requirements 10.3
        [Test]
        public void IntensityAndFrequencyAreNonDecreasingWithTier()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Pick two tiers in 0..MaxTier and order them a <= b.
                int a = rng.Next(0, SystemBreakState.MaxTier + 1);
                int b = rng.Next(0, SystemBreakState.MaxTier + 1);
                if (a > b) { int t = a; a = b; b = t; }

                float ia = SystemBreakState.IntensityFor(a);
                float ib = SystemBreakState.IntensityFor(b);
                float fa = SystemBreakState.FrequencyFor(a);
                float fb = SystemBreakState.FrequencyFor(b);

                PropertyCheck.That(ib >= ia,
                    $"intensity not monotonic: tier {a} -> {ia}, tier {b} -> {ib}");
                PropertyCheck.That(fb >= fa,
                    $"frequency not monotonic: tier {a} -> {fa}, tier {b} -> {fb}");
            });
        }
    }
}
