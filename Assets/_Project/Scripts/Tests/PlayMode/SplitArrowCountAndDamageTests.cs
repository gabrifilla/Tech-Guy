using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for Property 6 (Split arrow count and damage) of impactful-weapon-boons.
    ///
    /// Property 6: for any SplitArrow rank r >= 1, a kill SHALL spawn exactly r arrows on distinct
    /// headings, each carrying 50% of the killing arrow's damage multiplier
    /// (Validates: Requirements 2.1, 2.2).
    ///
    /// The behaviour lives in <see cref="SplitArrowCoordinator"/>: on <c>HookBus.OnKill</c> it fans
    /// out <c>rank</c> fresh arrows via <see cref="ArsenalProjectile.Fire"/> on a symmetric fan
    /// (<c>HeadingSpreadDeg = 40</c>) at a fixed <c>SplitDamageMultiplier = .5f</c>.
    ///
    /// Runs in PlayMode because <see cref="ArsenalProjectile.Fire"/> instantiates real projectile
    /// GameObjects (physics-driven), so we count and inspect the spawned <see cref="ArsenalProjectile"/>
    /// instances directly: their launch <c>forward</c> vectors prove distinct headings, and their
    /// private <c>_multiplier</c> proves the 50% damage carry. The rig owner has no TwinShot modifier,
    /// so <c>Fire</c> spawns exactly one projectile per requested split (no extra fan arrows).
    ///
    /// Ranks 1..3 are covered (SplitArrow's MaxRank is 3). A distinct class/file name avoids clashing
    /// with the Task 2.1 generation-bound test.
    /// </summary>
    public sealed class SplitArrowCountAndDamageTests
    {
        // The killing arrow's damage multiplier the coordinator halves (its SplitDamageMultiplier).
        private const float ExpectedSplitMultiplier = .5f;

        private ArsenalProjectileTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown()
        {
            // Destroy any split arrows the coordinator spawned during a case (the rig only tracks the
            // objects it created explicitly), then tear the rig down.
            foreach (ArsenalProjectile stray in Object.FindObjectsByType<ArsenalProjectile>())
                if (stray) Object.DestroyImmediate(stray.gameObject);
            _rig?.TearDown();
        }

        // Feature: impactful-weapon-boons, Property 6: a rank-r kill spawns exactly r split arrows on
        // distinct headings, each carrying 50% of the killing arrow's damage.
        // Validates: Requirements 2.1, 2.2
        [UnityTest]
        public IEnumerator Property6_KillSpawnsRankArrowsOnDistinctHeadingsAtHalfDamage()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 16);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ArsenalProjectileTestRig();
                _rig.BuildOwner();
                MakeWeaponFireArrows(_rig.Weapon); // SplitArrowCoordinator.OnKill gates on FiresArrows

                int rank = 1 + rng.Next(0, 3); // 1..3 (SplitArrow MaxRank == 3)

                var hooks = new HookBus();
                var coordinatorGo = new GameObject("SplitArrowCoordinator");
                var coordinator = coordinatorGo.AddComponent<SplitArrowCoordinator>();
                coordinator.Configure(_rig.Owner, hooks, rank);

                // A victim to "kill" at a randomized position; splits spawn from its position.
                var victimPos = new Vector3(
                    (float)(rng.NextDouble() * 8.0 - 4.0), 0f,
                    (float)(rng.NextDouble() * 8.0 - 4.0));
                Actor victim = _rig.BuildEnemy(victimPos);

                // Snapshot the projectiles that already exist, then raise the kill on this frame.
                var before = new HashSet<ArsenalProjectile>(
                    Object.FindObjectsByType<ArsenalProjectile>());

                hooks.RaiseKill(victim);

                // Collect exactly the projectiles the kill spawned (same frame, before Update runs).
                var spawned = new List<ArsenalProjectile>();
                foreach (ArsenalProjectile candidate in Object.FindObjectsByType<ArsenalProjectile>())
                    if (!before.Contains(candidate)) spawned.Add(candidate);

                // R2.1: exactly `rank` arrows spawn.
                Assert.AreEqual(rank, spawned.Count,
                    $"case {c}: rank {rank} kill spawned {spawned.Count} arrows, expected exactly {rank}");

                // R2.1: every spawned arrow leaves on a distinct heading (no two identical forward dirs).
                for (int i = 0; i < spawned.Count; i++)
                {
                    for (int j = i + 1; j < spawned.Count; j++)
                    {
                        Vector3 a = spawned[i].transform.forward;
                        Vector3 b = spawned[j].transform.forward;
                        float angle = Vector3.Angle(a, b);
                        Assert.Greater(angle, 0.5f,
                            $"case {c}: arrows {i} and {j} share a heading (angle {angle:F3} deg, rank {rank})");
                    }
                }

                // R2.2: each split carries 50% of the killing arrow's damage, expressed as the Fire
                // multiplier. With no TwinShot on the owner, Fire passes the multiplier through unscaled.
                foreach (ArsenalProjectile arrow in spawned)
                    Assert.AreEqual(ExpectedSplitMultiplier, _rig.Multiplier(arrow), 1e-4f,
                        $"case {c}: split arrow multiplier {_rig.Multiplier(arrow)} != {ExpectedSplitMultiplier} (rank {rank})");

                // Clean up this case's spawned splits and coordinator before the next iteration.
                foreach (ArsenalProjectile arrow in spawned)
                    if (arrow) Object.DestroyImmediate(arrow.gameObject);
                Object.DestroyImmediate(coordinatorGo);

                yield return null;
            }
        }

        /// <summary>
        /// Flips the rig weapon's private <c>_firesArrows</c> flag so it identifies as a bow. The rig
        /// builds a generic test weapon; the Split Arrow coordinator only fires on a bow, so the test
        /// enables that gate here without widening the production API.
        /// </summary>
        private static void MakeWeaponFireArrows(WeaponScript weapon)
        {
            FieldInfo firesArrows = typeof(WeaponScript).GetField(
                "_firesArrows", BindingFlags.Instance | BindingFlags.NonPublic);
            firesArrows.SetValue(weapon, true);
        }
    }
}
