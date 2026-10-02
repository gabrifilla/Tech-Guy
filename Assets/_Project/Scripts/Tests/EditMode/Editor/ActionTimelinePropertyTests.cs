using System;
using NUnit.Framework;
using UnityEngine;
using Random = System.Random;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure <see cref="ActionTimeline"/> struct of
    /// combat-foundation-rework (task 2.3), covering the phase/clamp guarantees of R2.2/R2.3.
    ///
    /// <para>
    /// Two universal properties are checked against the real production struct (not a re-stated
    /// model), driven by the project's seeded <see cref="PropertyCheck"/> harness over
    /// <see cref="PropertyCheck.DefaultCases"/> (>= 100) deterministic generated cases, since no
    /// FsCheck/CsCheck package can be resolved on this machine:
    /// </para>
    /// <list type="number">
    /// <item>For <em>any</em> constructor inputs — including out-of-range, NaN and infinite values —
    /// the built timeline satisfies the invariant <c>0 &lt;= StartupEnd &lt;= ActiveEnd &lt;= 1</c>
    /// (R2.3, the runtime clamp defense).</item>
    /// <item><see cref="ActionTimeline.PhaseOf"/> is monotonic across increasing progress: the phase
    /// order Startup → Active → Recovery never regresses as progress grows (R2.2).</item>
    /// </list>
    ///
    /// Generators deliberately bias toward the edges that exercise the clamp: a mix of in-range
    /// values, out-of-range values on both sides, and the special floats NaN / ±Infinity.
    /// </summary>
    // Feature: combat-foundation-rework, task 2.3. Requirements: R2.2, R2.3.
    public sealed class ActionTimelinePropertyTests
    {
        // Draws a float that spans the interesting input space for the constructor: mostly values
        // in and just outside [0, 1], plus the special floats (NaN, +/-Infinity) that must not break
        // the clamp invariant. Unity's Mathf.Clamp01 maps NaN to 0, so the invariant must still hold.
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

        // Numeric order of a phase on the Startup(0) -> Active(1) -> Recovery(2) timeline, used to
        // assert PhaseOf never regresses as progress increases.
        private static int PhaseOrder(ActionPhase phase)
        {
            switch (phase)
            {
                case ActionPhase.Startup: return 0;
                case ActionPhase.Active: return 1;
                case ActionPhase.Recovery: return 2;
                default: return int.MaxValue;
            }
        }

        // Feature: combat-foundation-rework, Property 1: for any constructor inputs (including
        // out-of-range, NaN and infinite values), the built timeline satisfies the clamp invariant
        // 0 <= StartupEnd <= ActiveEnd <= 1.
        // Validates: Requirements 2.3
        [Test]
        public void ConstructedTimelineAlwaysSatisfiesClampInvariant()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float startupInput = GenBoundary(rng);
                float activeInput = GenBoundary(rng);

                var timeline = new ActionTimeline(startupInput, activeInput);

                PropertyCheck.That(timeline.StartupEnd >= 0f && timeline.StartupEnd <= 1f,
                    $"startupInput={startupInput}, activeInput={activeInput}: StartupEnd={timeline.StartupEnd} is outside [0, 1].");
                PropertyCheck.That(timeline.ActiveEnd >= 0f && timeline.ActiveEnd <= 1f,
                    $"startupInput={startupInput}, activeInput={activeInput}: ActiveEnd={timeline.ActiveEnd} is outside [0, 1].");
                PropertyCheck.That(timeline.StartupEnd <= timeline.ActiveEnd,
                    $"startupInput={startupInput}, activeInput={activeInput}: StartupEnd={timeline.StartupEnd} must be <= ActiveEnd={timeline.ActiveEnd}.");
            });
        }

        // Feature: combat-foundation-rework, Property 2: PhaseOf is monotonic across increasing
        // progress — the resolved phase (Startup -> Active -> Recovery) never regresses as progress grows.
        // Validates: Requirements 2.2
        [Test]
        public void PhaseOfIsMonotonicAcrossIncreasingProgress()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var timeline = new ActionTimeline((float)rng.NextDouble(), (float)rng.NextDouble());

                // Walk a strictly increasing sequence of progress samples spanning [0, 1] (and a bit
                // beyond on both ends) and assert the phase order never decreases.
                int samples = rng.Next(8, 32);
                float previousProgress = -0.25f;
                int previousOrder = PhaseOrder(timeline.PhaseOf(previousProgress));

                for (int s = 0; s < samples; s++)
                {
                    // Strictly positive step keeps the sequence increasing; the +1.25 upper reach makes
                    // sure the walk ends firmly inside Recovery.
                    float step = (float)(rng.NextDouble() * (1.5 / samples)) + 1e-4f;
                    float progress = previousProgress + step;

                    int order = PhaseOrder(timeline.PhaseOf(progress));
                    PropertyCheck.That(order >= previousOrder,
                        $"startupEnd={timeline.StartupEnd:F4}, activeEnd={timeline.ActiveEnd:F4}: "
                        + $"phase regressed from order {previousOrder} at progress {previousProgress:F4} "
                        + $"to order {order} at progress {progress:F4}.");

                    previousOrder = order;
                    previousProgress = progress;
                }
            });
        }
    }
}
