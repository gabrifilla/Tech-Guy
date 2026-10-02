using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free <see cref="HitStop"/> and
    /// <see cref="HitStopGrouping"/> decisions of combat-foundation-rework (task 9.2), covering the
    /// hit-stop clamp/gate and grouping guarantees of R7.1, R7.2, R7.3 (and the preparatory note for
    /// R7.10).
    ///
    /// <para>
    /// Each property is checked against the real production types (not a re-stated model), driven by
    /// the project's seeded <see cref="PropertyCheck"/> harness over
    /// <see cref="PropertyCheck.DefaultCases"/> (&gt;= 100) deterministic generated cases, since no
    /// FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>P12 — <see cref="HitStop.ClampDuration"/> always lands in <c>[0, 1]</c> and is never
    /// <c>NaN</c>; when the clamped duration is <c>0</c>, <see cref="HitStop.ShouldApply"/> is
    /// <c>false</c> for <em>any</em> <c>enemiesDamaged</c>; and when the clamped duration is &gt; 0
    /// with <c>enemiesDamaged &gt;= 1</c> it is <c>true</c> (R7.1, R7.2).</item>
    /// <item>P13 — <see cref="HitStopGrouping.Combine"/> equals the clamped <em>maximum</em> of the
    /// simultaneous durations, is &lt;= 1, and is never the per-target sum: whenever at least two of
    /// the clamped durations are positive, the combined value is strictly less than their sum
    /// (R7.3).</item>
    /// <item>P14 — (preparation) the time-restore policy must not unconditionally fix the time scale
    /// to <c>1</c>. There is deliberately no pure API for the restore policy at this layer; it is a
    /// runtime concern of the thin <c>HitStopRunner</c> <c>MonoBehaviour</c> and is validated by the
    /// PlayMode runtime test in task 16 (R7.10). This test only anchors the shared contract constant
    /// (<see cref="HitStop.MaxDuration"/> == 1), without fabricating pure behavior that does not
    /// exist.</item>
    /// </list>
    ///
    /// Generators deliberately bias toward the edges that exercise the clamp and the grouping policy:
    /// a mix of in-range values, out-of-range values on both sides, and the special floats
    /// NaN / ±Infinity.
    /// </summary>
    // Feature: combat-foundation-rework, task 9.2. Requirements: R7.1, R7.2, R7.3, R7.10.
    public sealed class HitStopPropertyTests
    {
        // Draws a float that spans the interesting input space for ClampDuration: mostly values in
        // and just outside [0, 1], plus the special floats (NaN, +/-Infinity) that must be sanitized.
        // ClampDuration maps NaN and negatives to 0 and bounds anything above MaxDuration to 1.
        private static float GenBoundary(Random rng)
        {
            switch (rng.Next(0, 7))
            {
                case 0: return float.NaN;
                case 1: return float.PositiveInfinity;
                case 2: return float.NegativeInfinity;
                case 3: return (float)(rng.NextDouble() * 4.0 - 2.0);  // [-2, 2): straddles the range
                case 4: return (float)(-rng.NextDouble() * 100.0);     // clearly below 0
                case 5: return (float)(rng.NextDouble() * 100.0 + 1.0); // clearly above 1
                default: return (float)rng.NextDouble();               // in-range [0, 1)
            }
        }

        // Draws an enemiesDamaged count across the whole gate: 0 (no hit), small positive counts,
        // and extreme values so the gate is exercised for "any" enemiesDamaged.
        private static int GenEnemiesDamaged(Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return 0;
                case 1: return int.MaxValue;
                case 2: return int.MinValue;
                case 3: return -rng.Next(1, 50); // negative: still < 1, must gate off
                default: return rng.Next(1, 20);  // positive: eligible count
            }
        }

        // Feature: combat-foundation-rework, Property 12: ClampDuration always returns a value in
        // [0, 1] and never NaN, for any float input (in-range, out-of-range, NaN, +/-Infinity).
        // Validates: Requirements 7.2
        [Test]
        public void ClampDurationAlwaysInUnitIntervalAndNeverNaN()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float input = GenBoundary(rng);

                float clamped = HitStop.ClampDuration(input);

                PropertyCheck.That(!float.IsNaN(clamped),
                    $"input={input}: ClampDuration returned NaN, but the result must be a real number.");
                PropertyCheck.That(clamped >= 0f && clamped <= HitStop.MaxDuration,
                    $"input={input}: ClampDuration returned {clamped}, outside [0, {HitStop.MaxDuration}].");
            });
        }

        // Feature: combat-foundation-rework, Property 12: when ClampDuration(configured) == 0,
        // ShouldApply(configured, enemiesDamaged) is false for ANY enemiesDamaged; and when the
        // clamped duration is > 0 with enemiesDamaged >= 1, ShouldApply is true.
        // Validates: Requirements 7.1
        [Test]
        public void ShouldApplyFalseWhenClampedZeroAndTrueWhenPositiveAndHit()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float configured = GenBoundary(rng);
                int enemiesDamaged = GenEnemiesDamaged(rng);

                float clamped = HitStop.ClampDuration(configured);
                bool shouldApply = HitStop.ShouldApply(configured, enemiesDamaged);

                if (clamped == 0f)
                {
                    PropertyCheck.That(!shouldApply,
                        $"configured={configured} (clamped=0), enemiesDamaged={enemiesDamaged}: "
                        + "ShouldApply returned true, but a zero-clamped duration must never apply a hit-stop.");
                }
                else
                {
                    // clamped > 0: the gate is driven purely by enemiesDamaged >= 1.
                    bool expected = enemiesDamaged >= 1;
                    PropertyCheck.That(shouldApply == expected,
                        $"configured={configured} (clamped={clamped}), enemiesDamaged={enemiesDamaged}: "
                        + $"ShouldApply returned {shouldApply} but {expected} was expected "
                        + "(clamped > 0 applies iff at least one enemy was damaged).");
                }
            });
        }

        // Builds a list of simultaneous hit-stop durations whose length and values exercise the
        // grouping policy: empty/singleton lists, lists with several positive entries, and entries
        // that must be clamped (out-of-range, NaN, +/-Infinity).
        private static List<float> GenDurations(Random rng)
        {
            int count = rng.Next(0, 7); // 0..6 simultaneous impacts, incl. empty and singleton
            var list = new List<float>(count);
            for (int k = 0; k < count; k++)
            {
                list.Add(GenBoundary(rng));
            }

            return list;
        }

        // Feature: combat-foundation-rework, Property 13: Combine returns the clamped maximum of the
        // simultaneous durations (never the per-target sum), and the result is always within [0, 1].
        // Validates: Requirements 7.3
        [Test]
        public void CombineEqualsClampedMaximumAndStaysInUnitInterval()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                List<float> durations = GenDurations(rng);

                // Independent reference oracle: clamp each entry, then take the maximum (0 if empty).
                float expectedMax = 0f;
                for (int k = 0; k < durations.Count; k++)
                {
                    float clamped = HitStop.ClampDuration(durations[k]);
                    if (clamped > expectedMax)
                    {
                        expectedMax = clamped;
                    }
                }

                float combined = HitStopGrouping.Combine(durations);

                PropertyCheck.That(combined == expectedMax,
                    $"durations=[{string.Join(", ", durations)}]: Combine returned {combined} "
                    + $"but the clamped maximum is {expectedMax}.");
                PropertyCheck.That(combined >= 0f && combined <= HitStop.MaxDuration,
                    $"durations=[{string.Join(", ", durations)}]: Combine returned {combined}, "
                    + $"outside [0, {HitStop.MaxDuration}].");
            });
        }

        // Feature: combat-foundation-rework, Property 13: Combine never behaves like the per-target
        // sum: whenever at least two clamped durations are positive, the combined (max) value is
        // strictly less than the sum of the clamped durations.
        // Validates: Requirements 7.3
        [Test]
        public void CombineIsNeverThePerTargetSumWhenMultiplePositive()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                List<float> durations = GenDurations(rng);

                // Compute the clamped sum and count how many clamped entries are positive.
                float clampedSum = 0f;
                int positiveCount = 0;
                for (int k = 0; k < durations.Count; k++)
                {
                    float clamped = HitStop.ClampDuration(durations[k]);
                    clampedSum += clamped;
                    if (clamped > 0f)
                    {
                        positiveCount++;
                    }
                }

                float combined = HitStopGrouping.Combine(durations);

                // Combine never exceeds the sum (the max of non-negative values <= their sum).
                PropertyCheck.That(combined <= clampedSum,
                    $"durations=[{string.Join(", ", durations)}]: Combine={combined} exceeded the clamped sum {clampedSum}.");

                // With two or more positive clamped entries, the max is STRICTLY less than the sum,
                // proving Combine is the maximum policy and not the additive one.
                if (positiveCount >= 2)
                {
                    PropertyCheck.That(combined < clampedSum,
                        $"durations=[{string.Join(", ", durations)}]: {positiveCount} positive entries, "
                        + $"Combine={combined} equals the clamped sum {clampedSum}, but the grouping policy "
                        + "must be the maximum, strictly below the per-target sum.");
                }
            });
        }

        // Feature: combat-foundation-rework, Property 14 (preparation): the time-restore policy does
        // NOT unconditionally fix the time scale to 1. There is no pure restore API at this layer by
        // design — restoration is a runtime concern of the thin HitStopRunner MonoBehaviour and is
        // validated by the PlayMode runtime test in task 16 (R7.10). This test only anchors the
        // shared duration contract constant so the preparatory decision is documented and asserted
        // at the pure level without inventing behavior that does not exist here.
        // Validates: Requirements 7.10
        [Test]
        public void RestorePolicyAnchoredByMaxDurationContractRuntimeValidatedInTask16()
        {
            // Trivial anchor: the pure layer only owns the [0, 1] duration contract. The upper bound
            // of 1 second is the same unit interval the clamp enforces; the actual "restore respects
            // pause and other modifiers, never fixing to 1" behavior lives in HitStopRunner and is
            // covered by the task 16 PlayMode runtime test, not here.
            Assert.That(HitStop.MaxDuration, Is.EqualTo(1f),
                "HitStop.MaxDuration anchors the [0, 1] duration contract; the time-restore policy is "
                + "validated at runtime in task 16 (HitStopRunner PlayMode test), not at the pure layer.");
        }
    }
}
