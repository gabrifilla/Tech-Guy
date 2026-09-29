using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the readability limit of the immediate reaction channel enforced by
    /// the pure, scene-free clamp <see cref="ImmediateReactionClamp"/> — task 3.3 of
    /// weapon-gameplay-swarm-rework.
    ///
    /// <see cref="ImmediateReactionClamp"/> is a static, MonoBehaviour-free clamp, so its guarantees
    /// can be property-checked without a live Unity scene. This project cannot resolve FsCheck/CsCheck
    /// packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// >= 100 deterministic generated cases per property (generating arbitrary PushDistance and
    /// rotation across the whole input space, including huge and negative values) and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class ImmediateReactionClampPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 2: Limite de legibilidade da reação imediata.
        // For every immediate reaction Push or Stagger with an arbitrary PushDistance, the resulting
        // displacement in a single hit is at most 0.5 metre and the resulting rotation is at most 15
        // degrees.
        // Validates: Requirements 2.2
        [Test]
        public void ImmediateReactionDisplacementAndRotationStayWithinReadabilityLimit()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Alternate between the two immediate reaction types that actually nudge the enemy.
                HitReactionType reactionType = (rng.Next(0, 2) == 0)
                    ? HitReactionType.Push
                    : HitReactionType.Stagger;

                // Arbitrary requested distance and rotation across the whole space: huge, tiny,
                // negative and zero, so the clamp is exercised well beyond the readable limits.
                float requestedDistance = NextArbitrary(rng);
                float requestedDegrees = NextArbitrary(rng);

                float clampedDistance = ImmediateReactionClamp.ClampPush(reactionType, requestedDistance);
                float clampedDegrees = ImmediateReactionClamp.ClampRotation(reactionType, requestedDegrees);

                string state =
                    $"[reaction={reactionType} requestedDistance={requestedDistance} " +
                    $"requestedDegrees={requestedDegrees} => clampedDistance={clampedDistance} " +
                    $"clampedDegrees={clampedDegrees}]";

                // R2.2: the resulting displacement is at most 0.5 metre and never negative.
                PropertyCheck.That(
                    clampedDistance >= 0f &&
                    clampedDistance <= ImmediateReactionClamp.MaxDisplacementMeters,
                    $"immediate reaction displacement must stay in [0; {ImmediateReactionClamp.MaxDisplacementMeters}] " +
                    $"for {state}");

                // R2.2: the resulting rotation magnitude is at most 15 degrees.
                PropertyCheck.That(
                    Mathf.Abs(clampedDegrees) <= ImmediateReactionClamp.MaxRotationDegrees,
                    $"immediate reaction rotation magnitude must stay within {ImmediateReactionClamp.MaxRotationDegrees} " +
                    $"degrees for {state}");
            });
        }

        /// <summary>
        /// An arbitrary float spanning the full magnitude range a caller might declare: values from
        /// large negatives through zero to large positives (including a few extreme edges), so the
        /// clamp is tested against inputs far outside the readable window.
        /// </summary>
        private static float NextArbitrary(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return 0f;
                case 1: return float.MaxValue;
                case 2: return float.MinValue;
                case 3: return NextFloat(rng, -1000f, 0f); // negative range
                case 4: return NextFloat(rng, 0f, 1000f);  // large positive range
                default: return NextFloat(rng, -50f, 50f);  // around the limits
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
