using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure, scene-free <see cref="KitingImpulse"/> decision helper —
    /// task 6.4 of gauntlet-boon-playstyle-overhaul (Property 14, "Disparo em recuo só ao afastar-se").
    ///
    /// <see cref="KitingImpulse"/> owns the framework-agnostic math behind the Kiting Step (R8) bow
    /// boon: whether a fired shot grants a reposition impulse (<see cref="KitingImpulse.ShouldReposition"/>)
    /// and how far that impulse pushes at a given rank (<see cref="KitingImpulse.Distance"/>). Because it
    /// is a plain static class that references <see cref="Vector3"/> only for dot/magnitude helpers, the
    /// full rule can be exercised here without a scene, a <c>NavMeshAgent</c>, or any enemies — the
    /// per-impulse <see cref="KitingImpulse.Cooldown"/> window itself is enforced by the coordinator's
    /// timestamp, so this test asserts that the window is a positive constant the coordinator can gate on.
    ///
    /// Property 14 (design/R8.1/R8.2/R8.4): for any move direction and vector to the nearest enemy, the
    /// impulse is granted iff the player is moving (non-trivial magnitude) AND moving away from the enemy
    /// (dot of the two directions &lt; 0); the impulse distance equals base × (1 + 0.25R) and is therefore
    /// monotonically non-decreasing in rank.
    ///
    /// FsCheck/CsCheck cannot be resolved on this machine, so the seeded <see cref="PropertyCheck"/>
    /// harness drives &gt;= 100 deterministic generated cases per property (sampling retreating, approaching,
    /// perpendicular, zero, and near-zero vectors across all ranks) and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class KitingImpulsePropertyTests
    {
        // Mirror of KitingImpulse's own private BaseDistance (R8.2 starting point). The test asserts the
        // observable scaling base × (1 + 0.25R) rather than reaching into the class internals; BaseDistance
        // is recovered as Distance(0) so the test stays correct even if the constant is re-tuned.
        private const float PerRank = 0.25f;
        private const float Tolerance = 1e-4f;

        // Feature: gauntlet-boon-playstyle-overhaul, Property 14
        // The reposition impulse is granted iff the player is moving away from the nearest enemy
        // (non-trivial move direction AND negative dot of the two directions). Standing still, moving
        // toward, or moving perpendicular to the enemy grants no impulse.
        // Validates: Requirements 8.1, 8.4
        [Test]
        public void ShouldReposition_GrantedIffMovingAwayFromNearestEnemy()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Six families stress every branch of the decision: a genuine retreat, a genuine
                // approach, a perpendicular slide, a standing-still frame, a zero enemy vector, and a
                // fully random pair.
                int family = i % 6;
                Vector3 moveDir;
                Vector3 toEnemy;

                switch (family)
                {
                    case 0: // Moving away: pick a non-zero enemy direction, move roughly opposite it.
                        toEnemy = NonZeroVector(rng);
                        moveDir = -toEnemy.normalized * NextFloat(rng, 0.5f, 5f) + Jitter(rng, 0.2f);
                        break;
                    case 1: // Moving toward: move roughly along the enemy direction.
                        toEnemy = NonZeroVector(rng);
                        moveDir = toEnemy.normalized * NextFloat(rng, 0.5f, 5f) + Jitter(rng, 0.2f);
                        break;
                    case 2: // Perpendicular slide: a move orthogonal to the enemy direction (dot ~ 0).
                        toEnemy = new Vector3(NextFloat(rng, 1f, 5f), 0f, 0f);
                        moveDir = new Vector3(0f, 0f, NextFloat(rng, -5f, 5f));
                        if (moveDir.sqrMagnitude < 1e-3f) moveDir = new Vector3(0f, 0f, 1f);
                        break;
                    case 3: // Standing still: zero (or near-zero) move direction.
                        toEnemy = NonZeroVector(rng);
                        moveDir = Jitter(rng, 0.005f);
                        break;
                    case 4: // No enemy direction: zero target vector.
                        moveDir = NonZeroVector(rng);
                        toEnemy = Vector3.zero;
                        break;
                    default: // Fully random pair in [-5, 5]^3.
                        moveDir = RandomVector(rng, 5f);
                        toEnemy = RandomVector(rng, 5f);
                        break;
                }

                bool actual = KitingImpulse.ShouldReposition(moveDir, toEnemy);

                // Oracle: the impulse is granted exactly when the player is moving (non-trivial move
                // direction) AND moving away from the enemy (dot of normalized directions < 0). A
                // near-zero move or enemy vector must deny.
                bool moving = moveDir.sqrMagnitude > 1e-4f;
                bool haveEnemy = toEnemy.sqrMagnitude > 1e-4f;
                bool expected = moving && haveEnemy
                    && Vector3.Dot(moveDir.normalized, toEnemy.normalized) < 0f;

                PropertyCheck.That(actual == expected,
                    $"[case #{i}, family={family}] moveDir={moveDir} toEnemy={toEnemy}: " +
                    $"ShouldReposition returned {actual} but expected {expected} " +
                    $"(moving={moving}, haveEnemy={haveEnemy})");

                // Symmetry sanity: a strictly retreating move must grant, its exact opposite (approach)
                // must deny — guards against a sign flip in the dot test.
                if (family == 0 && expected)
                {
                    PropertyCheck.That(!KitingImpulse.ShouldReposition(-moveDir, toEnemy),
                        $"[case #{i}] reversing a granted retreat into an approach must deny the impulse");
                }
            });
        }

        // Feature: gauntlet-boon-playstyle-overhaul, Property 14
        // The impulse distance equals base × (1 + 0.25R) for every rank, is monotonically
        // non-decreasing in rank, and the per-impulse Cooldown is a positive constant the coordinator
        // can gate on so impulses cannot chain every frame.
        // Validates: Requirements 8.2
        [Test]
        public void Distance_ScalesByQuarterPerRankMonotonicallyAndCooldownIsPositive()
        {
            // The per-impulse cooldown must be a positive window regardless of rank (R8.2).
            PropertyCheck.That(KitingImpulse.Cooldown > 0f,
                $"KitingImpulse.Cooldown must be a positive per-impulse window but was {KitingImpulse.Cooldown}");

            float baseDistance = KitingImpulse.Distance(0);
            PropertyCheck.That(baseDistance > 0f,
                $"base reposition distance (Distance(0)) must be positive but was {baseDistance}");

            PropertyCheck.ForAll((rng, i) =>
            {
                // Sample ranks 0..3 (the catalog MaxRank) plus a negative rank to confirm the clamp.
                int rank = rng.Next(-1, 4);
                int safeRank = rank < 0 ? 0 : rank;

                float distance = KitingImpulse.Distance(rank);
                float expected = baseDistance * (1f + PerRank * safeRank);

                PropertyCheck.That(Mathf.Abs(distance - expected) <= Tolerance * Mathf.Max(1f, expected),
                    $"[case #{i}] rank={rank}: Distance returned {distance} but expected " +
                    $"base({baseDistance}) × (1 + {PerRank}×{safeRank}) = {expected}");

                // Negative ranks clamp to the rank-0 baseline (no shrinking below base).
                if (rank < 0)
                    PropertyCheck.That(Mathf.Approximately(distance, baseDistance),
                        $"[case #{i}] negative rank {rank} must clamp to the base distance {baseDistance} " +
                        $"but was {distance}");

                // Monotonic non-decreasing in rank: Distance(r) <= Distance(r+1) for r in [0, 2].
                for (int r = 0; r < 3; r++)
                    PropertyCheck.That(KitingImpulse.Distance(r) <= KitingImpulse.Distance(r + 1) + Tolerance,
                        $"[case #{i}] Distance must be non-decreasing in rank but Distance({r})=" +
                        $"{KitingImpulse.Distance(r)} > Distance({r + 1})={KitingImpulse.Distance(r + 1)}");
            });
        }

        /// <summary>A non-zero vector in [-5, 5]^3 (retried until its magnitude is well above the floor).</summary>
        private static Vector3 NonZeroVector(System.Random rng)
        {
            Vector3 v;
            do { v = RandomVector(rng, 5f); } while (v.sqrMagnitude < 0.25f);
            return v;
        }

        /// <summary>A uniform vector with each component in [-scale, scale].</summary>
        private static Vector3 RandomVector(System.Random rng, float scale)
        {
            return new Vector3(
                NextFloat(rng, -scale, scale),
                NextFloat(rng, -scale, scale),
                NextFloat(rng, -scale, scale));
        }

        /// <summary>A small uniform perturbation with each component in [-amount, amount].</summary>
        private static Vector3 Jitter(System.Random rng, float amount)
        {
            return new Vector3(
                NextFloat(rng, -amount, amount),
                NextFloat(rng, -amount, amount),
                NextFloat(rng, -amount, amount));
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
