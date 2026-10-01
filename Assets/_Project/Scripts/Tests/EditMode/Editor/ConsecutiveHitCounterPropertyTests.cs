using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 11 of gauntlet-boon-playstyle-overhaul — the pure
    /// <see cref="ConsecutiveHitCounter"/> decision core behind the Gauntlet's "Guarda partida"
    /// (<c>GuardBreaker</c>, R5) boon.
    ///
    /// Property 11 (design): every Nth consecutive basic hit completes the run — the run-completing
    /// hit SHALL return <c>true</c> and reset the count so the next run starts clean (stance
    /// multiplier <c>1 + 0.5R</c>) — and any interruption (skill cast / frame without a basic) SHALL
    /// reset the counter.
    ///
    /// This covers the pure contract: across many threshold/sequence combinations <c>RegisterBasicHit</c>
    /// returns <c>true</c> exactly on every Nth call and clears the count after each fire, <c>Reset</c>
    /// clears a partial run mid-sequence, and <c>StanceMultiplier</c> equals <c>1 + 0.5 * rank</c> with a
    /// negative-rank clamp and is monotonic in rank.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/>
    /// harness drives &gt;= 100 deterministic cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class ConsecutiveHitCounterPropertyTests
    {
        private const int MaxRank = 3;
        private const float Tolerance = 1e-5f;

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // For any threshold N >= 1, RegisterBasicHit fires (true) exactly on every Nth call and resets
        // the running count after each fire; between fires Count tracks the partial run.
        // Validates: Requirements 5.1
        [Test]
        public void RegisterBasicHit_FiresEveryNthCall_AndResetsCount()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The ctor clamps thresholds below 1 to 1, so the expected cadence uses the clamped value.
                int threshold = System.Math.Max(1, SampleThreshold(rng, i));
                int hits = rng.Next(0, 40); // 0 .. 39 consecutive basic hits

                var counter = new ConsecutiveHitCounter(threshold);
                int fires = 0;

                for (int hit = 1; hit <= hits; hit++)
                {
                    bool fired = counter.RegisterBasicHit();
                    bool expectedFire = (hit % threshold) == 0;

                    PropertyCheck.That(fired == expectedFire,
                        $"[case #{i}] threshold={threshold} hit#{hit}: RegisterBasicHit returned " +
                        $"{fired}, expected {expectedFire}");

                    if (fired)
                    {
                        fires++;
                        // After a fire the count resets so the next run starts clean (R5.1).
                        PropertyCheck.That(counter.Count == 0,
                            $"[case #{i}] threshold={threshold} hit#{hit}: Count {counter.Count} not " +
                            "reset to 0 after a fire");
                    }
                    else
                    {
                        // Between fires Count equals the partial run length.
                        int expectedCount = hit % threshold;
                        PropertyCheck.That(counter.Count == expectedCount,
                            $"[case #{i}] threshold={threshold} hit#{hit}: Count {counter.Count} != " +
                            $"expected partial {expectedCount}");
                    }
                }

                PropertyCheck.That(fires == hits / threshold,
                    $"[case #{i}] threshold={threshold} hits={hits}: fired {fires} times, " +
                    $"expected {hits / threshold}");
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // Any interruption (Reset) mid-run clears the running count so the next basic hit starts a
        // fresh run and the near-complete streak never fires.
        // Validates: Requirements 5.3
        [Test]
        public void Reset_ClearsPartialRun_AndNeverFiresInterrupted()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int threshold = System.Math.Max(1, SampleThreshold(rng, i));
                var counter = new ConsecutiveHitCounter(threshold);

                // Build a partial run that stops one hit short of firing, when possible.
                int partial = threshold <= 1 ? 0 : rng.Next(1, threshold); // 1 .. N-1 (0 when N == 1)
                for (int hit = 1; hit <= partial; hit++)
                {
                    bool fired = counter.RegisterBasicHit();
                    PropertyCheck.That(!fired,
                        $"[case #{i}] threshold={threshold}: partial hit#{hit} fired early");
                }

                PropertyCheck.That(counter.Count == partial,
                    $"[case #{i}] threshold={threshold}: partial Count {counter.Count} != {partial}");

                // Interruption resets the running count (R5.3).
                counter.Reset();
                PropertyCheck.That(counter.Count == 0,
                    $"[case #{i}] threshold={threshold}: Reset did not clear Count ({counter.Count})");

                // The next run must start clean: the first hit after reset only fires when N == 1.
                bool firstAfterReset = counter.RegisterBasicHit();
                PropertyCheck.That(firstAfterReset == (threshold == 1),
                    $"[case #{i}] threshold={threshold}: first hit after reset fired={firstAfterReset}, " +
                    $"expected {threshold == 1}");

                // Reaching threshold from the fresh run fires exactly on the Nth post-reset hit.
                if (threshold > 1)
                {
                    for (int hit = 2; hit <= threshold; hit++)
                    {
                        bool fired = counter.RegisterBasicHit();
                        bool expectedFire = hit == threshold;
                        PropertyCheck.That(fired == expectedFire,
                            $"[case #{i}] threshold={threshold}: post-reset hit#{hit} fired={fired}, " +
                            $"expected {expectedFire}");
                    }
                }
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 11: Guarda partida ao terceiro básico
        // The run-completing stance multiplier equals 1 + 0.5 * rank, clamps negative ranks to 0, and
        // is non-decreasing in rank.
        // Validates: Requirements 5.1
        [Test]
        public void StanceMultiplier_Equals1Plus0_5R_WithNegativeClamp_AndMonotonic()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(0, MaxRank + 1); // 0 .. MaxRank

                float value = ConsecutiveHitCounter.StanceMultiplier(rank);
                float expected = 1f + 0.5f * rank;

                PropertyCheck.That(System.Math.Abs(value - expected) <= Tolerance,
                    $"[case #{i}] rank={rank}: StanceMultiplier {value} != expected {expected}");

                // Monotonic in rank: a higher rank never yields a smaller multiplier.
                for (int lower = 0; lower < rank; lower++)
                {
                    float lowerValue = ConsecutiveHitCounter.StanceMultiplier(lower);
                    PropertyCheck.That(value >= lowerValue - Tolerance,
                        $"[case #{i}] StanceMultiplier at rank {rank} ({value}) < rank {lower} " +
                        $"({lowerValue}) — not non-decreasing in rank");
                }

                // Negative ranks clamp to 0 ⇒ multiplier exactly 1.
                int negRank = -(rng.Next(1, 5));
                float negValue = ConsecutiveHitCounter.StanceMultiplier(negRank);
                PropertyCheck.That(System.Math.Abs(negValue - 1f) <= Tolerance,
                    $"[case #{i}] rank={negRank}: negative rank should clamp to multiplier 1 but was {negValue}");
            });
        }

        // Thresholds that stress the counter: the degenerate clamp (<= 0, forced to 1), the boon's
        // real threshold of 3, and a spread of larger run lengths.
        private static int SampleThreshold(System.Random rng, int index)
        {
            switch (index % 6)
            {
                case 0: return 1;                 // minimum: every hit fires
                case 1: return 3;                 // the GuardBreaker threshold (R5.1)
                case 2: return 0;                 // clamped to 1
                case 3: return -(rng.Next(1, 5)); // negative, clamped to 1
                case 4: return 2;                 // small run
                default: return rng.Next(4, 9);   // 4 .. 8
            }
        }
    }
}
