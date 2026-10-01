using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 19 of gauntlet-boon-playstyle-overhaul (pure part) — the
    /// pure <see cref="EdgeBand"/> decision core behind the Spear's "Ponto cego"
    /// (<c>EdgeStrike</c>, R13) boon.
    ///
    /// Property 19 (design): a hit lands "on the edge" exactly when it reaches the outermost 20% of
    /// the thrust range (<c>hitDistance &gt;= 0.8 * thrustRange</c>), and an edge hit's extra stance
    /// multiplier is <c>1 + 0.4R</c>. The registry/vulnerability-window part (opening a window on an
    /// edge hit and amplifying subsequent hits) is covered separately in task 17.1; only the pure
    /// predicate and multiplier live in <see cref="EdgeBand"/> and are covered here.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/>
    /// harness drives &gt;= 100 deterministic cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class EdgeBandPropertyTests
    {
        private const int MaxRank = 3;
        private const float Tolerance = 1e-4f;

        // Feature: gauntlet-boon-playstyle-overhaul, Property 19: Ponto cego recompensa a borda (pure part)
        // For any hit distance and thrust range, EdgeBand.IsEdge is true only in the outer 20% of a
        // positive range (hitDistance >= 0.8 * thrustRange) and always false for a non-positive range.
        // Validates: Requirements 13.1, 13.3
        [Test]
        public void IsEdge_TrueOnlyInOuterTwentyPercentOfPositiveRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float thrustRange = SampleRange(rng, i);
                float hitDistance = SampleDistance(rng, i, thrustRange);

                bool isEdge = EdgeBand.IsEdge(hitDistance, thrustRange);

                // A non-positive range defines no edge: never an edge, regardless of distance (R13.1/R13.3).
                if (thrustRange <= 0f)
                {
                    PropertyCheck.That(!isEdge,
                        $"[case #{i}] range={thrustRange} dist={hitDistance}: non-positive range " +
                        $"must never be an edge but IsEdge returned true");
                    return;
                }

                float edgeThreshold = thrustRange * 0.8f;

                // A hit strictly inside the inner 80% (clear of the float-rounding boundary) earns no
                // bonus (R13.3); a hit clearly at/past the outer-20% threshold is always an edge
                // (R13.1). A relative tolerance absorbs float rounding right on the threshold, where
                // either answer is acceptable.
                float boundaryTolerance = System.Math.Max(Tolerance, edgeThreshold * 1e-4f);
                if (hitDistance < edgeThreshold - boundaryTolerance)
                    PropertyCheck.That(!isEdge,
                        $"[case #{i}] range={thrustRange} dist={hitDistance}: inner hit " +
                        $"(below {edgeThreshold}) must not be an edge");
                else if (hitDistance > edgeThreshold + boundaryTolerance)
                    PropertyCheck.That(isEdge,
                        $"[case #{i}] range={thrustRange} dist={hitDistance}: hit past " +
                        $"{edgeThreshold} must be an edge");
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 19: Ponto cego recompensa a borda (pure part)
        // For any rank R, EdgeBand.StanceMultiplier equals 1 + 0.4R (negative ranks clamped to 0) and
        // is non-decreasing in rank.
        // Validates: Requirements 13.1, 13.3
        [Test]
        public void StanceMultiplier_MatchesOnePlusPointFourRank_AndIsMonotonic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, MaxRank + 1); // 0 .. MaxRank

                float multiplier = EdgeBand.StanceMultiplier(rank);
                float expected = 1f + 0.4f * rank;

                // Exact 1 + 0.4R formula (R13.1).
                PropertyCheck.That(System.Math.Abs(multiplier - expected) <= Tolerance,
                    $"[case #{i}] rank={rank}: StanceMultiplier {multiplier} != expected {expected}");

                // Monotonic in rank: a higher rank never rewards less than a lower rank.
                for (int lower = 0; lower < rank; lower++)
                {
                    float lowerMultiplier = EdgeBand.StanceMultiplier(lower);
                    PropertyCheck.That(multiplier >= lowerMultiplier - Tolerance,
                        $"[case #{i}] rank {rank} multiplier {multiplier} < rank {lower} " +
                        $"multiplier {lowerMultiplier} — not non-decreasing in rank");
                }

                // Negative ranks are clamped to 0 ⇒ exactly 1 (no reduction below baseline).
                int negativeRank = -(rng.Next(1, 5));
                float negativeMultiplier = EdgeBand.StanceMultiplier(negativeRank);
                PropertyCheck.That(System.Math.Abs(negativeMultiplier - 1f) <= Tolerance,
                    $"[case #{i}] negative rank {negativeRank}: multiplier {negativeMultiplier} " +
                    $"was not clamped to the baseline 1");
            });
        }

        // Ranges that stress the edge: non-positive (no edge), tiny, and a spread of positive reaches.
        private static float SampleRange(System.Random rng, int index)
        {
            switch (index % 5)
            {
                case 0: return 0f;                                      // degenerate zero range (no edge)
                case 1: return -(float)rng.NextDouble() * 5f;           // negative range (no edge)
                case 2: return 1e-3f;                                   // tiny positive range
                case 3: return 1f + (float)rng.NextDouble() * 9f;       // 1 .. 10 m typical reach
                default: return (float)rng.NextDouble() * 20f;          // 0 .. 20 m anywhere
            }
        }

        // Distances that straddle the 0.8*range threshold: well inside, just below, exactly at, just
        // above, well past, plus negatives and anywhere.
        private static float SampleDistance(System.Random rng, int index, float thrustRange)
        {
            float edge = thrustRange * 0.8f;
            switch (index % 7)
            {
                case 0: return edge;                                    // exactly on the threshold
                case 1: return edge - 1e-2f;                            // just inside the inner 80%
                case 2: return edge + 1e-2f;                            // just into the outer 20%
                case 3: return thrustRange;                             // the full reach (an edge when range > 0)
                case 4: return (float)rng.NextDouble() * System.Math.Max(0f, edge); // 0 .. edge, inner band
                case 5: return -(float)rng.NextDouble() * 3f;           // negative distance
                default: return (float)rng.NextDouble() * 25f;          // anywhere in 0 .. 25 m
            }
        }
    }
}
