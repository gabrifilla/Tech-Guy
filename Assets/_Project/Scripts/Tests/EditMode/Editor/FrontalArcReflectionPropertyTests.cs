using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure Mirror frontal-arc reflection model in
    /// <see cref="FrontalArcReflection"/> — task 8.9 of enemy-swarm-core-archetypes.
    ///
    /// The Mirror's damage-reduction is otherwise scene/MonoBehaviour driven
    /// (<see cref="FrontalReflector"/>); the arc / reflection math was extracted into plain C# so
    /// these invariants can be property-checked without a live Unity scene. The project cannot
    /// resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class FrontalArcReflectionPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 12: Mirror frontal-arc reflection.
        // For a Mirror facing any direction with any protected arc in [90,180]:
        //   - an active-shield player projectile arriving from within the arc is reduced to <=25% (R18.3);
        //   - any hit from outside the arc applies full damage (R18.4);
        //   - any area attack applies full damage regardless of the arc (R18.5);
        //   - while the shield is inactive (stance-broken / control-locked) every hit applies full
        //     damage (R18.6/R18.7).
        // The returned fraction is always within [0, 1].
        // Validates: Requirements 18.3, 18.4, 18.5, 18.6, 18.7
        [Test]
        public void FrontalArcReflectionHonoursArcShieldAndAttackType()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float arc = RandomArc(rng);
                Vector3 mirrorForward = RandomGroundDirection(rng);

                // Decide whether we aim the approach inside or outside the protected arc, then build a
                // matching direction. The half-arc is what IsWithinArc compares the angle against.
                float halfArc = arc * 0.5f;
                bool aimInside = rng.Next(0, 2) == 0;
                Vector3 approach = aimInside
                    ? DirectionAtAngle(rng, mirrorForward, RandomAngle(rng, 0f, halfArc - 1f))
                    : DirectionAtAngle(rng, mirrorForward, RandomAngle(rng, halfArc + 1f, 180f));

                bool shieldActive = rng.Next(0, 2) == 0;
                bool isPlayerProjectile = rng.Next(0, 2) == 0;
                bool isAreaAttack = rng.Next(0, 2) == 0;

                bool within = FrontalArcReflection.IsWithinArc(mirrorForward, approach, arc);
                float fraction = FrontalArcReflection.DamageFraction(
                    shieldActive, isPlayerProjectile, isAreaAttack, mirrorForward, approach, arc);

                string ctx =
                    $"arc={arc} halfArc={halfArc} aimInside={aimInside} within={within} " +
                    $"shieldActive={shieldActive} isPlayerProjectile={isPlayerProjectile} " +
                    $"isAreaAttack={isAreaAttack} fwd={mirrorForward} approach={approach} fraction={fraction}";

                // The fraction is always a valid damage multiplier.
                PropertyCheck.That(fraction >= 0f && fraction <= 1f,
                    $"fraction out of [0,1]. {ctx}");

                if (!shieldActive)
                {
                    // R18.6/R18.7: no protection at all while the shield is down.
                    PropertyCheck.That(Mathf.Approximately(fraction, 1f),
                        $"shield inactive must apply full damage. {ctx}");
                    return;
                }

                if (isAreaAttack)
                {
                    // R18.5: area attacks ignore the arc entirely.
                    PropertyCheck.That(Mathf.Approximately(fraction, 1f),
                        $"area attack must apply full damage regardless of arc. {ctx}");
                    return;
                }

                if (!isPlayerProjectile)
                {
                    // Only player projectiles are reflected; anything else takes full damage.
                    PropertyCheck.That(Mathf.Approximately(fraction, 1f),
                        $"non-player-projectile must apply full damage. {ctx}");
                    return;
                }

                // A player projectile with an active shield: the arc decides.
                if (within)
                {
                    // R18.3: frontal player projectile is reduced to at most 25%.
                    PropertyCheck.That(fraction <= FrontalArcReflection.MaxReflectedFraction + 1e-6f,
                        $"frontal player projectile must be reduced to <=25%. {ctx}");
                }
                else
                {
                    // R18.4: a hit from outside the arc applies full damage.
                    PropertyCheck.That(Mathf.Approximately(fraction, 1f),
                        $"out-of-arc player projectile must apply full damage. {ctx}");
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>A protected arc width spanning the full allowed range [90,180].</summary>
        private static float RandomArc(System.Random rng)
        {
            return FrontalArcReflection.MinArcDegrees
                + (float)rng.NextDouble()
                * (FrontalArcReflection.MaxArcDegrees - FrontalArcReflection.MinArcDegrees);
        }

        /// <summary>A uniformly random angle in degrees within [min, max]; clamped to be non-negative.</summary>
        private static float RandomAngle(System.Random rng, float min, float max)
        {
            if (max < min) max = min;
            min = Mathf.Max(0f, min);
            return min + (float)rng.NextDouble() * (max - min);
        }

        /// <summary>A random non-degenerate direction on the ground plane (y == 0).</summary>
        private static Vector3 RandomGroundDirection(System.Random rng)
        {
            float theta = (float)rng.NextDouble() * 2f * Mathf.PI;
            return new Vector3(Mathf.Cos(theta), 0f, Mathf.Sin(theta));
        }

        /// <summary>
        /// A ground-plane direction rotated <paramref name="angleDegrees"/> away from
        /// <paramref name="reference"/>, in a randomly chosen rotational sign. The result is what a hit
        /// approaching at that separation from the Mirror's forward would look like.
        /// </summary>
        private static Vector3 DirectionAtAngle(System.Random rng, Vector3 reference, float angleDegrees)
        {
            float sign = rng.Next(0, 2) == 0 ? 1f : -1f;
            return Quaternion.AngleAxis(sign * angleDegrees, Vector3.up) * reference;
        }
    }
}
