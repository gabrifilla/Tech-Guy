using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure projectile state machine in <see cref="ProjectileMotion"/>
    /// (with <see cref="ProjectileHit"/> as the mocked per-step collision) — task 4.2 of
    /// enemy-swarm-core-archetypes.
    ///
    /// The live <see cref="EnemyProjectile"/> is scene/MonoBehaviour driven (transform moves,
    /// SphereCast clipping, overlap tests, material release, Destroy). The universal invariants —
    /// damage applied at most once, mark-for-destruction on hit / range exhaustion / geometry block,
    /// inertness afterwards, and the owner-death cascade — were extracted into the plain-C#
    /// <see cref="ProjectileMotion"/> so they can be property-checked without a live Unity scene.
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property
    /// and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class ProjectileMotionPropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 5: Projectile applies damage at most once, then is destroyed.
        // For any projectile and any travel path (random sequence of Step calls with mocked
        // ProjectileHit — clear travel, target overlap, geometry block, range exhaustion — plus an
        // occasional ResolveImpact for the arced path), damage is applied at most once across the
        // whole life; the projectile marks itself for destruction on hit / range exhaustion /
        // geometry block; and once marked it is inert (every further Step/ResolveImpact applies no
        // damage and moves no distance).
        // Validates: Requirements 7.4, 7.5, 8.4, 19.2, 19.3, 19.4, 19.5
        [Test]
        public void ProjectileAppliesDamageAtMostOnceThenIsInert()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float range = RandomRange(rng);
                var motion = new ProjectileMotion(range);

                // A non-positive range means the projectile has no distance to cover and is marked
                // for destruction immediately, but still never damages on creation.
                PropertyCheck.That(!motion.DamageApplied,
                    $"range={range}: projectile applied damage on creation");
                if (range <= 0f)
                {
                    PropertyCheck.That(motion.IsMarkedForDestruction,
                        $"range={range}: non-positive range was not marked for destruction on creation");
                }

                int damageCount = 0;      // number of times damage was applied across the whole life
                bool wasMarked = motion.IsMarkedForDestruction;
                float lastTravelled = motion.Travelled;

                int steps = rng.Next(3, 14);
                for (int s = 0; s < steps; s++)
                {
                    bool markedBefore = motion.IsMarkedForDestruction;
                    float travelledBefore = motion.Travelled;
                    bool damageAppliedBefore = motion.DamageApplied;

                    // Occasionally exercise the arced-impact path instead of a travel step.
                    bool useImpact = rng.Next(0, 5) == 0;
                    if (useImpact)
                    {
                        bool targetInArea = rng.Next(0, 2) == 0;
                        bool applied = motion.ResolveImpact(targetInArea);
                        if (applied) damageCount++;

                        // ResolveImpact never applies damage once already marked for destruction.
                        if (markedBefore)
                        {
                            PropertyCheck.That(!applied,
                                $"range={range}: ResolveImpact applied damage while already marked for destruction");
                        }
                        // Any resolve (hit or miss) leaves the projectile marked for destruction.
                        PropertyCheck.That(motion.IsMarkedForDestruction,
                            $"range={range}: ResolveImpact did not mark the projectile for destruction");
                        // AppliedDamage is reported only when the flag flips this call.
                        PropertyCheck.That(applied == (!damageAppliedBefore && motion.DamageApplied),
                            $"range={range}: ResolveImpact return value disagreed with DamageApplied transition");
                    }
                    else
                    {
                        ProjectileHit hit = RandomHit(rng, motion.RemainingRange);
                        float intended = RandomIntendedStep(rng, range);
                        ProjectileStepResult result = motion.Step(intended, hit);

                        if (result.AppliedDamage) damageCount++;

                        if (markedBefore)
                        {
                            // Once marked, a step is inert: no damage, no movement, Inert outcome.
                            PropertyCheck.That(result.Outcome == ProjectileOutcome.Inert,
                                $"range={range}: step after destruction reported {result.Outcome}, expected Inert");
                            PropertyCheck.That(!result.AppliedDamage,
                                $"range={range}: inert step applied damage");
                            PropertyCheck.That(Mathf.Approximately(result.MovedDistance, 0f),
                                $"range={range}: inert step moved {result.MovedDistance}");
                            PropertyCheck.That(Mathf.Approximately(motion.Travelled, travelledBefore),
                                $"range={range}: inert step changed travelled {travelledBefore} -> {motion.Travelled}");
                        }
                        else
                        {
                            // A hit (and only a hit) applies damage and marks for destruction.
                            PropertyCheck.That(result.AppliedDamage == (result.Outcome == ProjectileOutcome.Hit),
                                $"range={range}: AppliedDamage disagreed with Hit outcome {result.Outcome}");

                            // Any terminal outcome (Hit / Blocked / RangeExhausted) marks for destruction.
                            bool terminal = result.Outcome != ProjectileOutcome.Travelling;
                            PropertyCheck.That(motion.IsMarkedForDestruction == terminal,
                                $"range={range}: outcome {result.Outcome} but IsMarkedForDestruction={motion.IsMarkedForDestruction}");
                            PropertyCheck.That(result.ShouldDestroy == terminal,
                                $"range={range}: ShouldDestroy disagreed with terminal outcome {result.Outcome}");

                            // Travel is bounded by the configured range and never runs backwards.
                            PropertyCheck.That(motion.Travelled >= travelledBefore - 1e-6f,
                                $"range={range}: travelled ran backwards {travelledBefore} -> {motion.Travelled}");
                            PropertyCheck.That(motion.Travelled <= motion.Range + 1e-4f,
                                $"range={range}: travelled {motion.Travelled} exceeded range {motion.Range}");
                        }
                    }

                    // The single-damage flag latches: it never clears once set.
                    if (damageAppliedBefore)
                    {
                        PropertyCheck.That(motion.DamageApplied,
                            $"range={range}: DamageApplied cleared after being set");
                    }

                    // Destruction is permanent: it never un-marks.
                    if (markedBefore)
                    {
                        PropertyCheck.That(motion.IsMarkedForDestruction,
                            $"range={range}: IsMarkedForDestruction cleared after being set");
                    }

                    wasMarked = motion.IsMarkedForDestruction;
                    lastTravelled = motion.Travelled;
                }

                // Damage is applied at most once across the entire life (R19.2).
                PropertyCheck.That(damageCount <= 1,
                    $"range={range}: damage applied {damageCount} times (expected at most 1)");

                // DamageApplied reflects whether the single beat landed.
                PropertyCheck.That(motion.DamageApplied == (damageCount == 1),
                    $"range={range}: DamageApplied={motion.DamageApplied} but observed damageCount={damageCount}");

                // If damage was applied, the projectile is (and stays) marked for destruction.
                if (motion.DamageApplied)
                {
                    PropertyCheck.That(motion.IsMarkedForDestruction,
                        $"range={range}: damage applied but not marked for destruction");
                }
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 6: Owner death cascades to in-flight projectiles.
        // For any set of in-flight projectiles whose owner dies or is disabled (MarkOwnerGone), every
        // one is marked for destruction and applies no further damage on any subsequent Step or
        // ResolveImpact — regardless of the collision facts fed afterwards.
        // Validates: Requirements 19.6
        [Test]
        public void OwnerDeathCascadesAndSuppressesFurtherDamage()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A "set" of in-flight projectiles owned by the same enemy.
                int count = rng.Next(1, 6);
                var projectiles = new ProjectileMotion[count];
                for (int p = 0; p < count; p++) projectiles[p] = new ProjectileMotion(RandomRange(rng));

                // Advance each projectile a few frames of ordinary flight before the owner dies.
                // Some may already have hit/expired; the cascade must still hold for the rest.
                foreach (var motion in projectiles)
                {
                    int preSteps = rng.Next(0, 4);
                    for (int s = 0; s < preSteps; s++)
                    {
                        ProjectileHit hit = RandomHit(rng, motion.RemainingRange);
                        motion.Step(RandomIntendedStep(rng, motion.Range), hit);
                    }
                }

                // Record which projectiles had already dealt their single point of damage.
                var damagedBefore = new bool[count];
                for (int p = 0; p < count; p++) damagedBefore[p] = projectiles[p].DamageApplied;

                // The owner dies / is disabled: cascade the mark to every in-flight projectile.
                foreach (var motion in projectiles) motion.MarkOwnerGone();

                for (int p = 0; p < count; p++)
                {
                    var motion = projectiles[p];

                    // Every projectile is now marked for destruction (R19.6).
                    PropertyCheck.That(motion.IsMarkedForDestruction,
                        $"projectile {p}: not marked for destruction after owner death");

                    // The owner cascade never itself applies damage: the flag is unchanged.
                    PropertyCheck.That(motion.DamageApplied == damagedBefore[p],
                        $"projectile {p}: MarkOwnerGone changed DamageApplied {damagedBefore[p]} -> {motion.DamageApplied}");

                    // Any subsequent Step / ResolveImpact — even one that would normally hit — applies
                    // no further damage and leaves the projectile inert.
                    int postSteps = rng.Next(2, 6);
                    for (int s = 0; s < postSteps; s++)
                    {
                        if (rng.Next(0, 2) == 0)
                        {
                            // A hit-facts step that would have damaged a live projectile.
                            ProjectileStepResult result = motion.Step(RandomIntendedStep(rng, motion.Range),
                                ProjectileHit.HitTarget(motion.RemainingRange));
                            PropertyCheck.That(!result.AppliedDamage,
                                $"projectile {p}: Step applied damage after owner death");
                            PropertyCheck.That(result.Outcome == ProjectileOutcome.Inert,
                                $"projectile {p}: Step after owner death reported {result.Outcome}, expected Inert");
                        }
                        else
                        {
                            // An impact resolve with the target inside the area would normally damage.
                            bool applied = motion.ResolveImpact(true);
                            PropertyCheck.That(!applied,
                                $"projectile {p}: ResolveImpact applied damage after owner death");
                        }

                        // The single-damage flag is unchanged by any post-death interaction.
                        PropertyCheck.That(motion.DamageApplied == damagedBefore[p],
                            $"projectile {p}: post-death interaction changed DamageApplied to {motion.DamageApplied}");
                    }
                }
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Ranges spanning negative (immediate destroy), zero, and positive travel distances.</summary>
        private static float RandomRange(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -5f + (float)rng.NextDouble() * 5f;  // negative .. 0
                case 1: return 0f;                                  // exactly zero
                case 2: return (float)rng.NextDouble() * 1.5f;      // short range (may exhaust fast)
                default: return 2f + (float)rng.NextDouble() * 40f; // typical .. long range
            }
        }

        /// <summary>Intended per-frame step distances spanning negative, zero, small, and overshoot.</summary>
        private static float RandomIntendedStep(System.Random rng, float range)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return -1f + (float)rng.NextDouble() * 1f;         // negative .. 0 (clamped)
                case 1: return 0f;                                         // exactly zero
                case 2: return (float)rng.NextDouble() * 0.5f;             // small step
                default: return 0.5f + (float)rng.NextDouble() *
                    Mathf.Max(1f, range + 5f);                             // may overshoot remaining range
            }
        }

        /// <summary>
        /// A mocked per-step collision: a mix of clear travel, target overlap, geometry block, and
        /// steps that reach exactly the remaining range (range exhaustion). The allowed distance is
        /// kept sane relative to the remaining range.
        /// </summary>
        private static ProjectileHit RandomHit(System.Random rng, float remainingRange)
        {
            float safeRemaining = Mathf.Max(0f, remainingRange);
            switch (rng.Next(0, 4))
            {
                case 0: // target overlap along the travelled segment
                    return ProjectileHit.HitTarget((float)rng.NextDouble() * safeRemaining);
                case 1: // geometry blocked short of the intended travel
                    return ProjectileHit.HitGeometry((float)rng.NextDouble() * safeRemaining * 0.5f);
                case 2: // a clear step that consumes the entire remaining range (exhaustion)
                    return ProjectileHit.Clear(safeRemaining);
                default: // ordinary clear travel of a partial segment
                    return ProjectileHit.Clear((float)rng.NextDouble() * safeRemaining);
            }
        }
    }
}
