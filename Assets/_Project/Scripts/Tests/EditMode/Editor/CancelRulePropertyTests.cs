using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure <see cref="CancelRule"/> struct of
    /// combat-foundation-rework (task 4.3), covering the clamp/validation guarantees of R4.1/R4.8.
    ///
    /// <para>
    /// Property 5 has two facets, each checked as its own universal property against the real
    /// production struct (not a re-stated model), driven by the project's seeded
    /// <see cref="PropertyCheck"/> harness over <see cref="PropertyCheck.DefaultCases"/> (>= 100)
    /// deterministic generated cases, since no FsCheck/CsCheck package can be resolved on this
    /// machine:
    /// </para>
    /// <list type="number">
    /// <item>For <em>any</em> constructor inputs — including out-of-range, NaN and infinite values,
    /// and any <see cref="CancelTarget"/> — the built rule satisfies the invariant
    /// <c>0 &lt;= Start &lt;= End &lt;= 1</c> (R4.1 invariant via the R4.8 runtime clamp defense).</item>
    /// <item><see cref="CancelRule.Validate"/> returns <c>true</c> if and only if the raw bounds
    /// satisfy <c>0 &lt;= start &lt;= end &lt;= 1</c> (R4.8). NaN is never in <c>[0, 1]</c> because
    /// every NaN comparison is false, so <see cref="CancelRule.Validate"/> must reject it.</item>
    /// </list>
    ///
    /// Generators deliberately bias toward the edges that exercise the clamp and the biconditional:
    /// a mix of in-range values, out-of-range values on both sides, and the special floats
    /// NaN / ±Infinity.
    /// </summary>
    // Feature: combat-foundation-rework, task 4.3. Requirements: R4.1, R4.8.
    public sealed class CancelRulePropertyTests
    {
        // Draws a float that spans the interesting input space for the constructor: mostly values
        // in and just outside [0, 1], plus the special floats (NaN, +/-Infinity) that must not break
        // the clamp invariant. The constructor sanitizes NaN to 0 and Mathf.Clamp01 bounds the rest.
        private static float GenBoundary(Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return float.NaN;
                case 1: return float.PositiveInfinity;
                case 2: return float.NegativeInfinity;
                case 3: return (float)(rng.NextDouble() * 4.0 - 2.0); // [-2, 2): straddles the range
                case 4: return (float)(-rng.NextDouble() * 100.0);    // clearly below 0
                case 5: return (float)(rng.NextDouble() * 100.0 + 1.0); // clearly above 1
                default: return (float)rng.NextDouble();              // in-range [0, 1)
            }
        }

        // Draws any of the three cancel destinations so the invariant is exercised across targets.
        private static CancelTarget GenTarget(Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return CancelTarget.Dash;
                case 1: return CancelTarget.Basic;
                default: return CancelTarget.Skill;
            }
        }

        // The reference definition of the invariant, evaluated directly on the raw inputs. For NaN
        // (or either infinity) at least one comparison is false, so this returns false — which is
        // exactly what Validate must agree with.
        private static bool InRangeOrdered(float start, float end)
        {
            return start >= 0f && start <= 1f
                && end >= 0f && end <= 1f
                && start <= end;
        }

        // Feature: combat-foundation-rework, Property 5: for any constructor inputs (including
        // out-of-range, NaN and infinite values) and any CancelTarget, the built rule satisfies the
        // clamp invariant 0 <= Start <= End <= 1.
        // Validates: Requirements 4.1
        [Test]
        public void ConstructedRuleAlwaysSatisfiesClampInvariant()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                CancelTarget target = GenTarget(rng);
                float startInput = GenBoundary(rng);
                float endInput = GenBoundary(rng);

                var rule = new CancelRule(target, startInput, endInput);

                PropertyCheck.That(rule.Target == target,
                    $"target={target}, startInput={startInput}, endInput={endInput}: Target={rule.Target} should be preserved.");
                PropertyCheck.That(rule.Start >= 0f && rule.Start <= 1f,
                    $"startInput={startInput}, endInput={endInput}: Start={rule.Start} is outside [0, 1].");
                PropertyCheck.That(rule.End >= 0f && rule.End <= 1f,
                    $"startInput={startInput}, endInput={endInput}: End={rule.End} is outside [0, 1].");
                PropertyCheck.That(rule.Start <= rule.End,
                    $"startInput={startInput}, endInput={endInput}: Start={rule.Start} must be <= End={rule.End}.");
            });
        }

        // Feature: combat-foundation-rework, Property 5: Validate(start, end, out _) returns true if
        // and only if 0 <= start <= end <= 1 (NaN/±Infinity are never in [0, 1], so Validate rejects
        // them because every comparison against a NaN is false).
        // Validates: Requirements 4.8
        [Test]
        public void ValidateIsTrueIffBoundsAreInRangeAndOrdered()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float startInput = GenBoundary(rng);
                float endInput = GenBoundary(rng);

                bool expected = InRangeOrdered(startInput, endInput);
                bool actual = CancelRule.Validate(startInput, endInput, out string error);

                PropertyCheck.That(actual == expected,
                    $"startInput={startInput}, endInput={endInput}: Validate returned {actual} "
                    + $"but 0 <= start <= end <= 1 is {expected} (error=\"{error}\").");

                // The out-parameter contract mirrors ActionTimeline.Validate: empty message on
                // success, a non-empty diagnostic naming the offending bound on failure.
                if (actual)
                {
                    PropertyCheck.That(error == string.Empty,
                        $"startInput={startInput}, endInput={endInput}: Validate succeeded but left a non-empty error \"{error}\".");
                }
                else
                {
                    PropertyCheck.That(!string.IsNullOrEmpty(error),
                        $"startInput={startInput}, endInput={endInput}: Validate failed but produced no error message.");
                }
            });
        }
    }
}
