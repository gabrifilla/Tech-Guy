using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free <see cref="ExecutionImpactLedger"/> and its
    /// companion <see cref="ExecutionId"/> of combat-foundation-rework (task 8.2), covering the
    /// deduplication guarantee of R8.9: an ImpactEvent is counted exactly once per
    /// <c>(ExecutionId, eventIndex)</c> pair even when the logical clock and an Animation Event both
    /// signal it.
    ///
    /// <para>
    /// The property is checked against the real production <see cref="ExecutionImpactLedger"/> type
    /// (not a re-stated model), driven by the project's seeded <see cref="PropertyCheck"/> harness
    /// over <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic generated cases, since
    /// no FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>P15 — on a fresh ledger, for any sequence of emission calls built from a few distinct
    /// <see cref="ExecutionId"/> values (via <see cref="ExecutionId.Next"/>) and random
    /// <c>eventIndex</c> ints (with repeats), <see cref="ExecutionImpactLedger.TryEmit"/> returns
    /// <c>true</c> exactly on the <em>first</em> occurrence of each distinct pair and <c>false</c> on
    /// every subsequent call with the same pair. Distinct pairs (differing in either the
    /// <see cref="ExecutionId"/> or the <c>eventIndex</c>) each earn their own first-true. A reference
    /// <see cref="HashSet{T}"/> in the test computes the expected boolean independently.</item>
    /// </list>
    /// </summary>
    // Feature: combat-foundation-rework, task 8.2. Requirements: R8.9.
    public sealed class ExecutionImpactLedgerPropertyTests
    {
        // Draws a small count of distinct ExecutionId values for the case, each produced by the real
        // monotonic Next() so the test exercises genuine ids rather than a stand-in. A small pool is
        // used deliberately so random eventIndex collisions produce frequent repeated pairs — the
        // situation the ledger exists to deduplicate.
        private static ExecutionId[] GenDistinctExecutionIds(Random rng)
        {
            int count = rng.Next(1, 5); // 1..4 distinct executions
            var ids = new ExecutionId[count];
            for (int k = 0; k < count; k++)
            {
                ids[k] = ExecutionId.Next();
            }

            return ids;
        }

        // Draws an eventIndex from a deliberately narrow range so repeats across the generated
        // sequence are common (the dedup path), while still including a few larger/edge values.
        private static int GenEventIndex(Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0;
                case 1: return int.MaxValue;
                case 2: return int.MinValue;
                default: return rng.Next(0, 6); // small range => frequent collisions
            }
        }

        // Feature: combat-foundation-rework, Property 15: on a fresh ExecutionImpactLedger, TryEmit
        // returns true exactly once per (ExecutionId, eventIndex) pair (the first occurrence) and
        // false on every repeat; distinct pairs each get their own first-true.
        // Validates: Requirements 8.9
        [Test]
        public void TryEmitIsTrueExactlyOncePerExecutionEventPair()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var ledger = new ExecutionImpactLedger();
                ExecutionId[] ids = GenDistinctExecutionIds(rng);

                // Independent reference oracle: a pair is "first seen" iff Add returns true here too.
                var seen = new HashSet<(int Execution, int EventIndex)>();

                int callCount = rng.Next(2, 40);
                for (int k = 0; k < callCount; k++)
                {
                    ExecutionId id = ids[rng.Next(0, ids.Length)];
                    int eventIndex = GenEventIndex(rng);

                    bool expected = seen.Add((id.Value, eventIndex));
                    bool actual = ledger.TryEmit(id, eventIndex);

                    PropertyCheck.That(actual == expected,
                        $"call #{k}: TryEmit(ExecutionId({id.Value}), {eventIndex}) returned {actual} "
                        + $"but the pair was {(expected ? "seen for the first time" : "already emitted")}, so {expected} was expected.");
                }
            });
        }

        // Feature: combat-foundation-rework, Property 15: distinct pairs each earn their own
        // first-true, and a strict repeat of any emitted pair is always rejected — verified on a
        // mix of pairs that differ by eventIndex only, by ExecutionId only, or by both.
        // Validates: Requirements 8.9
        [Test]
        public void DistinctPairsEachFirstTrueAndRepeatsAlwaysFalse()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var ledger = new ExecutionImpactLedger();

                // Two distinct executions so we can vary ExecutionId as well as eventIndex.
                ExecutionId a = ExecutionId.Next();
                ExecutionId b = ExecutionId.Next();
                PropertyCheck.That(a != b, $"ExecutionId.Next() produced colliding ids {a.Value} and {b.Value}.");

                // Build a set of distinct pairs that differ by eventIndex, by ExecutionId, or both.
                int pairCount = rng.Next(2, 12);
                var pairs = new List<(ExecutionId Id, int EventIndex)>();
                var distinctKeys = new HashSet<(int, int)>();
                int guard = 0;
                while (pairs.Count < pairCount && guard < pairCount * 8)
                {
                    guard++;
                    ExecutionId id = rng.Next(0, 2) == 0 ? a : b;
                    int eventIndex = rng.Next(0, 5);
                    if (distinctKeys.Add((id.Value, eventIndex)))
                    {
                        pairs.Add((id, eventIndex));
                    }
                }

                // First emission of every distinct pair must be true (first-true per distinct pair).
                for (int k = 0; k < pairs.Count; k++)
                {
                    bool first = ledger.TryEmit(pairs[k].Id, pairs[k].EventIndex);
                    PropertyCheck.That(first,
                        $"pair #{k} (ExecutionId({pairs[k].Id.Value}), {pairs[k].EventIndex}): first emission "
                        + "returned false, but each distinct pair must earn its own first-true.");
                }

                // A second emission of every one of those pairs must be false (always rejected).
                for (int k = 0; k < pairs.Count; k++)
                {
                    bool repeat = ledger.TryEmit(pairs[k].Id, pairs[k].EventIndex);
                    PropertyCheck.That(!repeat,
                        $"pair #{k} (ExecutionId({pairs[k].Id.Value}), {pairs[k].EventIndex}): repeat emission "
                        + "returned true, but any repeat of an already-emitted pair must be false.");
                }
            });
        }
    }
}
