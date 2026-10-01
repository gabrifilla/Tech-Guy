using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 17 of gauntlet-boon-playstyle-overhaul — the pure
    /// <see cref="SpacingBand"/> decision core behind the Spear's "Recuo controlado"
    /// (<c>SpacingRecoil</c>, R11) boon.
    ///
    /// Property 17 (design): for any current distance and rank R, when the Thrust connects the
    /// step-back SHALL move the player so the resulting distance never passes the band's upper edge
    /// (clamped), growing with R; when the Thrust connects nothing, no step-back SHALL be applied.
    ///
    /// Only the clamping / monotonicity part lives in the pure class and is covered here: the
    /// "no connect ⇒ no-op" guard (R11.4) is the <c>primaryHits &gt; 0</c> check in
    /// <c>ArsenalCombat</c>, which calls <see cref="SpacingBand.StepBack"/> only on connect — so the
    /// pure contract is that the returned step is always non-negative, never carries the player past
    /// <see cref="SpacingBand.Max"/>, and is non-decreasing in rank. The degenerate rank &lt;= 0 case
    /// (no boon acquired) yields exactly zero displacement, matching a no-op.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/>
    /// harness drives &gt;= 100 deterministic cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SpacingBandPropertyTests
    {
        private const int MaxRank = 3;
        private const float Tolerance = 1e-4f;

        // Feature: gauntlet-boon-playstyle-overhaul, Property 17: Recuo controlado assenta na banda
        // For any current distance d and rank R, SpacingBand.StepBack is non-negative, clamps the
        // resulting distance to the band's upper edge (d + step <= Max), and is non-decreasing in R.
        // A non-positive rank produces no step (the no-op/no-connect baseline).
        // Validates: Requirements 11.1, 11.2, 11.4
        [Test]
        public void StepBack_ClampsToBandUpperEdge_AndGrowsWithRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float distance = SampleDistance(rng, i);
                int rank = rng.Next(0, MaxRank + 1); // 0 .. MaxRank

                float step = SpacingBand.StepBack(distance, rank);

                // Always non-negative.
                PropertyCheck.That(step >= 0f,
                    $"[case #{i}] d={distance} rank={rank}: step {step} is negative");

                // Never carries the player past the band's upper edge (clamped) — R11.2.
                // The step only ever pushes the player back toward Max; a player who already starts
                // beyond Max is left exactly where they are (step 0), so the resulting distance can
                // stay above Max. The real contract is that the step never increases distance past
                // the greater of the current distance and the band's upper edge.
                float resulting = distance + step;
                float upperBound = System.Math.Max(distance, SpacingBand.Max);
                PropertyCheck.That(resulting <= upperBound + Tolerance,
                    $"[case #{i}] d={distance} rank={rank}: resulting distance {resulting} " +
                    $"exceeds upper bound {upperBound} (band Max {SpacingBand.Max}, step={step})");

                // Rank 0 (and any non-positive rank) means no boon acquired ⇒ no displacement.
                if (rank <= 0)
                    PropertyCheck.That(step == 0f,
                        $"[case #{i}] d={distance} rank={rank}: expected no step at rank 0 but was {step}");

                // Monotonic in rank: a higher rank never steps back less than a lower rank at the same
                // distance — the recoil grows with R (R11.2, Property 2) up to the band clamp.
                for (int lower = 0; lower < rank; lower++)
                {
                    float lowerStep = SpacingBand.StepBack(distance, lower);
                    PropertyCheck.That(step >= lowerStep - Tolerance,
                        $"[case #{i}] d={distance}: step at rank {rank} ({step}) < step at rank " +
                        $"{lower} ({lowerStep}) — not non-decreasing in rank");
                }

                // Negative ranks are treated as 0 (no step), same as the no-op baseline.
                float negStep = SpacingBand.StepBack(distance, -(rng.Next(1, 5)));
                PropertyCheck.That(negStep == 0f,
                    $"[case #{i}] d={distance}: negative rank produced a non-zero step {negStep}");
            });
        }

        // Distances that stress the band: below the lower edge, inside the band, both edges exactly,
        // just past the upper edge (where any step must clamp to zero), and far outside.
        private static float SampleDistance(System.Random rng, int index)
        {
            switch (index % 8)
            {
                case 0: return SpacingBand.Min;                                     // lower edge
                case 1: return SpacingBand.Max;                                     // upper edge (step clamps to 0)
                case 2: return SpacingBand.Max + 1e-3f;                             // just past upper edge
                case 3: return 0f;                                                  // degenerate zero distance
                case 4: return SpacingBand.Min + (float)rng.NextDouble() *
                               (SpacingBand.Max - SpacingBand.Min);                 // inside the band
                case 5: return (float)rng.NextDouble() * SpacingBand.Min;           // 0 .. Min, below the band
                case 6: return SpacingBand.Max + (float)rng.NextDouble() * 20f;     // well above the band
                default: return (float)rng.NextDouble() * 30f;                      // anywhere in 0 .. 30 m
            }
        }
    }
}
