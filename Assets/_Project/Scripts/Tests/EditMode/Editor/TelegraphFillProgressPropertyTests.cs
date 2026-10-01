using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure fill-progress logic in
    /// <see cref="TelegraphFill.ProgressAt"/> — task 4.2 of
    /// ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// The telegraph fill is otherwise scene/coroutine driven; the progress invariant was extracted
    /// into plain C# so it can be property-checked without a live Unity scene. The project cannot
    /// resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class TelegraphFillProgressPropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 10
        // Fill progress is monotonic in [0,1], reaches 1 at impact, and clamps out-of-range input.
        // For any non-decreasing sequence of windup fractions (including values outside [0,1]),
        // TelegraphFill.ProgressAt returns values in [0,1] that are non-decreasing, equal to the
        // clamped fraction, and equal to 1 when the fraction >= 1.
        // Validates: Requirements 6.2, 6.4, 8.4, 8.5, 9.3, 9.5
        [Test]
        public void ProgressIsMonotonicClampedAndReachesOneAtImpact()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A non-decreasing sequence of windup fractions spanning well below 0, through
                // [0,1], and well past 1 so both clamp boundaries are exercised.
                int samples = rng.Next(4, 12);
                float fraction = -1f - (float)rng.NextDouble() * 2f; // start clearly below 0
                float prevProgress = float.NegativeInfinity;

                for (int s = 0; s < samples; s++)
                {
                    float progress = TelegraphFill.ProgressAt(fraction);

                    // R8.4, R9.5: output is always clamped to [0,1].
                    PropertyCheck.That(progress >= 0f && progress <= 1f,
                        $"progress {progress} out of [0,1] at fraction={fraction}");

                    // R8.4, R9.5: progress equals the clamped fraction.
                    float expected = Mathf.Clamp01(fraction);
                    PropertyCheck.That(Mathf.Approximately(progress, expected),
                        $"progress {progress} != clamp01(fraction)={expected} at fraction={fraction}");

                    // R6.2, R8.5, R9.3: non-decreasing as the (non-decreasing) fraction advances.
                    PropertyCheck.That(progress >= prevProgress - 1e-6f,
                        $"progress decreased: {prevProgress} -> {progress} at fraction={fraction}");

                    // R6.4, R6.2: reaches exactly 1 at or beyond impact (fraction >= 1).
                    if (fraction >= 1f)
                        PropertyCheck.That(Mathf.Approximately(progress, 1f),
                            $"progress {progress} not 1 at impact (fraction={fraction})");

                    // At or below the start (fraction <= 0) progress is pinned to 0.
                    if (fraction <= 0f)
                        PropertyCheck.That(Mathf.Approximately(progress, 0f),
                            $"progress {progress} not 0 at fraction={fraction} (<= 0)");

                    prevProgress = progress;

                    // Advance the fraction by a non-negative step so the input stays non-decreasing.
                    fraction += (float)rng.NextDouble() * 0.8f;
                }

                // Explicit impact check: at exactly fraction 1 the fill is complete.
                PropertyCheck.That(Mathf.Approximately(TelegraphFill.ProgressAt(1f), 1f),
                    $"progress at fraction=1 was {TelegraphFill.ProgressAt(1f)}, expected 1");
            });
        }
    }
}
