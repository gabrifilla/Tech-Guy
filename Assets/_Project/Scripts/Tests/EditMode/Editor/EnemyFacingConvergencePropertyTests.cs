using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure bounded-turn resolver <see cref="EnemyFacing"/> —
    /// task 1.2 of ranged-kiting-and-attack-telegraph-overhaul.
    ///
    /// <see cref="EnemyAI.FacePlayer"/> replaces its instant <c>Quaternion.LookRotation</c> snap with a
    /// bounded step so enemies turn naturally; the invariants "the step never overshoots the target"
    /// (converges with non-increasing remaining angle, never flips sign) and "facing is reported once
    /// the remaining angle is within the epsilon" were extracted into the scene-free
    /// <see cref="EnemyFacing"/> so they can be property-checked without a live Unity scene. This
    /// project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (min 128)
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class EnemyFacingConvergencePropertyTests
    {
        // Feature: ranged-kiting-and-attack-telegraph-overhaul, Property 1: Facing converges without overshoot
        // For any current forward direction, target direction (non-degenerate), angular speed in
        // [90,1440], and dt > 0, EnemyFacing.StepTowards produces a forward whose signed angle to the
        // target has absolute value <= the prior angle and never changes sign; and once the remaining
        // angle <= 0.5 degrees, IsFacing is true.
        // Validates: Requirements 10.1, 10.3, 10.4, 10.6, 16.3
        [Test]
        public void FacingConvergesTowardTargetWithoutOvershootOrSignFlip()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                Vector3 target = RandomDirection(rng);
                Vector3 forward = RandomDirection(rng);

                float angularSpeed = RandomAngularSpeed(rng);
                float dt = RandomDeltaTime(rng);

                // A stable rotation axis fixed from the initial geometry: a correct bounded turn rotates
                // the forward about this axis toward the target without ever passing it. The signed angle
                // measured about this axis therefore stays the same sign and shrinks in magnitude; an
                // overshoot would flip the sign, and a sign flip is exactly what we forbid (R10.1/R10.6/R16.3).
                Vector3 axis = Vector3.Cross(forward, target);
                bool axisDefined = axis.sqrMagnitude > 1e-10f;
                if (axisDefined)
                    axis.Normalize();

                float previousAbsAngle = Vector3.Angle(forward, target);
                float previousSignedAngle = axisDefined ? SignedAngle(forward, target, axis) : previousAbsAngle;
                float initialSign = Mathf.Sign(previousSignedAngle);

                // Step repeatedly: convergence is a per-step invariant, so iterate enough frames to also
                // reach the facing epsilon and confirm IsFacing turns true (R10.3/R10.4). The step closes
                // at least ClampAngularSpeed(angularSpeed) * dt degrees per frame, so size the loop to the
                // worst case (a near-180 degree gap at the slowest clamped speed and smallest dt) with a
                // comfortable margin, independent of the generated speed/dt.
                float degreesPerStep = EnemyFacing.ClampAngularSpeed(angularSpeed) * dt;
                int steps = Mathf.CeilToInt(360f / Mathf.Max(degreesPerStep, 1e-4f)) + 8;
                bool reachedFacing = false;

                for (int step = 0; step < steps; step++)
                {
                    Vector3 next = EnemyFacing.StepTowards(forward, target, angularSpeed, dt);

                    // The step always yields a unit-length forward direction.
                    PropertyCheck.That(Mathf.Abs(next.magnitude - 1f) <= 1e-3f,
                        $"forward={forward} target={target} angularSpeed={angularSpeed} dt={dt}: " +
                        $"stepped forward {next} is not unit length (|{next.magnitude}|)");

                    float absAngle = Vector3.Angle(next, target);

                    // R10.1/R10.6/R16.3: the remaining angle never grows (non-increasing). A small epsilon
                    // absorbs floating-point rounding on near-converged steps.
                    PropertyCheck.That(absAngle <= previousAbsAngle + 1e-3f,
                        $"forward={forward} target={target} angularSpeed={angularSpeed} dt={dt}: " +
                        $"remaining angle grew from {previousAbsAngle} to {absAngle} (overshoot)");

                    // R10.1/R16.3: no sign flip about the fixed axis -> the turn never crossed past the
                    // target. Only meaningful while the forward and target are not (anti)parallel.
                    if (axisDefined)
                    {
                        float signedAngle = SignedAngle(next, target, axis);
                        if (Mathf.Abs(signedAngle) > EnemyFacing.FacingEpsilonDegrees)
                        {
                            PropertyCheck.That(Mathf.Sign(signedAngle) == initialSign,
                                $"forward={forward} target={target} angularSpeed={angularSpeed} dt={dt}: " +
                                $"signed angle flipped sign from {previousSignedAngle} to {signedAngle} (overshoot past target)");
                        }
                        previousSignedAngle = signedAngle;
                    }

                    previousAbsAngle = absAngle;
                    forward = next;

                    // R10.3/R10.4: once within the epsilon, IsFacing must report true.
                    if (absAngle <= EnemyFacing.FacingEpsilonDegrees)
                    {
                        PropertyCheck.That(EnemyFacing.IsFacing(forward, target),
                            $"forward={forward} target={target}: remaining angle {absAngle} <= " +
                            $"{EnemyFacing.FacingEpsilonDegrees} but IsFacing returned false");
                        reachedFacing = true;
                        break;
                    }
                }

                // With a positive clamped angular speed and dt, the bounded step must converge inside the
                // facing epsilon within the iterated frames (R10.1/R10.3): it closes a bounded 180-degree
                // gap at >= MinAngularSpeed * dt per step.
                PropertyCheck.That(reachedFacing,
                    $"forward (initial direction) toward target={target} angularSpeed={angularSpeed} dt={dt}: " +
                    $"did not reach the facing epsilon ({EnemyFacing.FacingEpsilonDegrees} deg) within {steps} steps; " +
                    $"final remaining angle {previousAbsAngle}");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// A non-degenerate direction on/around the horizontal plane with some vertical component so the
        /// turn is genuinely 3D. Re-rolls on the vanishingly rare zero vector so inputs are always valid
        /// (the degenerate zero-direction behavior is covered by example tests, not this property).
        /// </summary>
        private static Vector3 RandomDirection(System.Random rng)
        {
            Vector3 v;
            do
            {
                v = new Vector3(
                    ((float)rng.NextDouble() - 0.5f) * 2f,
                    ((float)rng.NextDouble() - 0.5f) * 2f,
                    ((float)rng.NextDouble() - 0.5f) * 2f);
            }
            while (v.sqrMagnitude < 1e-4f);
            return v.normalized;
        }

        /// <summary>Angular speeds spanning the clamp bounds and both out-of-range sides (R10.2).</summary>
        private static float RandomAngularSpeed(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return EnemyFacing.MinAngularSpeed;                                   // lower bound
                case 1: return EnemyFacing.MaxAngularSpeed;                                   // upper bound
                case 2: return ((float)rng.NextDouble() - 0.5f) * 4000f;                      // may be out of range -> clamped
                default:
                    return EnemyFacing.MinAngularSpeed +
                           (float)rng.NextDouble() * (EnemyFacing.MaxAngularSpeed - EnemyFacing.MinAngularSpeed);
            }
        }

        /// <summary>Positive frame delta times from tiny to a large hitch.</summary>
        private static float RandomDeltaTime(System.Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return 1f / 240f;                               // high frame rate
                case 1: return 1f / 60f;                                // typical
                default: return 1f / 60f + (float)rng.NextDouble() * 0.1f; // slower / hitch
            }
        }

        /// <summary>
        /// Signed angle (degrees) from <paramref name="from"/> to <paramref name="to"/> measured about a
        /// fixed <paramref name="axis"/>, mirroring <see cref="Vector3.SignedAngle"/> but with an explicit
        /// stable axis so overshoot past the target shows up as a sign flip.
        /// </summary>
        private static float SignedAngle(Vector3 from, Vector3 to, Vector3 axis)
        {
            float unsigned = Vector3.Angle(from, to);
            float sign = Mathf.Sign(Vector3.Dot(axis, Vector3.Cross(from, to)));
            return unsigned * sign;
        }
    }
}
