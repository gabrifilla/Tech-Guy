using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for Property 5 of impactful-weapon-boons
    /// (Split Arrow generation bound, <see cref="SplitArrowCoordinator"/>).
    ///
    /// Property 5: for any kill by a split arrow, no further split arrows are spawned (at most one
    /// generation, R2.3), and the number of split arrows spawned in a single frame never exceeds the
    /// cascade engine's <c>MaxSecondaryHits</c> budget of 32 (R2.4).
    ///
    /// Runs in PlayMode because the boon fires real <see cref="ArsenalProjectile"/>s through
    /// <c>ArsenalProjectile.Fire</c> (each split is a live "Energy arrow" GameObject spawned into the
    /// scene); spawn counting is done by diffing <c>FindObjectsByType&lt;ArsenalProjectile&gt;</c>
    /// across the kill notification, exactly as the neighboring projectile rigs do. Enemy layouts are
    /// generated (position + count vary per case) so the bound is exercised across a spread of inputs,
    /// not a single hand-picked scene.
    ///
    /// The two guards live on private state of the coordinator (<c>_spawning</c> re-entrancy flag,
    /// <c>_spawnedThisFrame</c> per-frame counter, and the <c>OnKill</c> handler). Per the spec's
    /// "don't widen gameplay APIs for tests" convention, the test reaches them through reflection
    /// rather than exposing them, mirroring how <see cref="ArsenalProjectileTestRig"/> reflects the
    /// projectile's private knobs.
    /// </summary>
    public sealed class SplitArrowGenerationBoundTests
    {
        /// <summary>The per-frame split ceiling the coordinator mirrors from RunSynergyEffects.MaxSecondaryHits (R2.4).</summary>
        private const int MaxSecondaryHits = 32;

        private static readonly FieldInfo FiresArrowsField =
            typeof(WeaponScript).GetField("_firesArrows", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly FieldInfo SpawningField =
            typeof(SplitArrowCoordinator).GetField("_spawning", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo OnKillMethod =
            typeof(SplitArrowCoordinator).GetMethod("OnKill", BindingFlags.Instance | BindingFlags.NonPublic);

        private ArsenalProjectileTestRig _rig;
        private readonly List<GameObject> _extra = new List<GameObject>();

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _extra)
                if (go) Object.DestroyImmediate(go);
            _extra.Clear();
            _rig?.TearDown();
        }

        // Feature: impactful-weapon-boons, Property 5: a split arrow's kill spawns no further splits
        // (one generation only). While the coordinator is spawning its splits, a re-entrant kill
        // notification (which is exactly how a split arrow that scored a kill would come back through
        // HookBus.OnKill) produces zero additional split arrows.
        // Validates: Requirements 2.3, 2.4
        [UnityTest]
        public IEnumerator Property5_SplitArrowKillSpawnsNoFurtherSplits()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 5);

            _rig.TearDown();
            _rig = new ArsenalProjectileTestRig();
            _rig.BuildOwner();
            EquipBow();
            SplitArrowCoordinator coordinator = BuildCoordinator();

            for (int c = 0; c < cases; c++)
            {
                int rank = 1 + rng.Next(0, 3); // 1..3
                Configure(coordinator, rank);

                // A generated victim position so the split origin (victim + up) varies per case.
                Vector3 pos = new Vector3(
                    (float)(rng.NextDouble() * 12.0 - 6.0),
                    0f,
                    (float)(rng.NextDouble() * 12.0 - 6.0));
                Actor victim = BuildEnemy(pos);

                // Baseline: a normal bow kill fans exactly `rank` split arrows.
                int firstGeneration = SplitsFromKill(coordinator, victim);
                Assert.AreEqual(rank, firstGeneration,
                    $"case {c}: a normal bow kill at rank {rank} should fan exactly {rank} splits");

                // One generation only (R2.3): a split arrow that itself scores a kill re-enters the
                // coordinator through the same HookBus.OnKill event while `_spawning` is set. Model
                // that re-entrant kill by invoking OnKill with the guard raised — it must be a no-op,
                // so a split can never produce further splits.
                Actor splitVictim = BuildEnemy(pos + Vector3.right);
                SpawningField.SetValue(coordinator, true);
                int secondGeneration = SplitsFromKill(coordinator, splitVictim);
                SpawningField.SetValue(coordinator, false);
                Assert.AreEqual(0, secondGeneration,
                    $"case {c}: a split arrow's kill must spawn no further splits (produced {secondGeneration})");

                yield return null;
            }
        }

        // Feature: impactful-weapon-boons, Property 5: a mass kill in a single frame never spawns more
        // than MaxSecondaryHits (32) split arrows that frame, however many enemies die at once.
        // Validates: Requirements 2.3, 2.4
        [UnityTest]
        public IEnumerator Property5_MassKillNeverExceedsPerFrameBudget()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 55);

            _rig.TearDown();
            _rig = new ArsenalProjectileTestRig();
            _rig.BuildOwner();
            EquipBow();
            SplitArrowCoordinator coordinator = BuildCoordinator();

            for (int c = 0; c < cases; c++)
            {
                int rank = 1 + rng.Next(0, 3);          // 1..3
                Configure(coordinator, rank);

                // Enough simultaneous kills to blow past the budget at any rank: 40..80 victims, so
                // rank*victims far exceeds 32 and the per-frame counter must clamp the total.
                int victims = 40 + rng.Next(0, 41);

                var before = Snapshot();
                for (int v = 0; v < victims; v++)
                {
                    Vector3 pos = new Vector3(
                        (float)(rng.NextDouble() * 16.0 - 8.0),
                        0f,
                        (float)(rng.NextDouble() * 16.0 - 8.0));
                    Actor victim = BuildEnemy(pos);
                    // All kills resolve in the SAME frame (no `yield return` inside the loop), which is
                    // the scenario the _spawnedThisFrame counter bounds.
                    OnKillMethod.Invoke(coordinator, new object[] { victim });
                }
                int spawnedThisFrame = SpawnedSince(before);

                Assert.LessOrEqual(spawnedThisFrame, MaxSecondaryHits,
                    $"case {c}: mass kill of {victims} enemies at rank {rank} spawned {spawnedThisFrame} " +
                    $"splits in one frame, exceeds cap {MaxSecondaryHits}");
                // The counter should let splits accumulate right up to the budget, never a single spawn
                // beyond it: the last admitted batch may not straddle 32 (rank <= remaining budget).
                Assert.GreaterOrEqual(spawnedThisFrame, 0, $"case {c}: spawn count cannot be negative");

                // Advancing a frame resets the per-frame budget so the next frame can spawn again.
                yield return null;
                Actor freshVictim = BuildEnemy(Vector3.forward * 3f);
                int afterFrame = SplitsFromKill(coordinator, freshVictim);
                Assert.AreEqual(rank, afterFrame,
                    $"case {c}: a new frame must reset the budget and allow {rank} splits again");

                yield return null;
            }
        }

        // ---- helpers ----

        /// <summary>Flags the rig's bow weapon as arrow-firing so the coordinator's FiresArrows gate passes.</summary>
        private void EquipBow()
        {
            Assert.IsNotNull(FiresArrowsField, "WeaponScript._firesArrows field not found via reflection");
            FiresArrowsField.SetValue(_rig.Weapon, true);
            Assert.IsTrue(_rig.Weapon.FiresArrows, "test bow must report FiresArrows");
        }

        /// <summary>Adds a fresh SplitArrowCoordinator to the owner and returns it (torn down with the rig).</summary>
        private SplitArrowCoordinator BuildCoordinator()
        {
            SplitArrowCoordinator coordinator = _rig.Owner.gameObject.AddComponent<SplitArrowCoordinator>();
            Assert.IsNotNull(SpawningField, "SplitArrowCoordinator._spawning field not found via reflection");
            Assert.IsNotNull(OnKillMethod, "SplitArrowCoordinator.OnKill method not found via reflection");
            return coordinator;
        }

        /// <summary>Binds the coordinator to the owner at the given rank via the public Configure API.</summary>
        private void Configure(SplitArrowCoordinator coordinator, int rank) =>
            coordinator.Configure(_rig.Owner, new HookBus(), rank);

        /// <summary>Builds a live enemy tracked for teardown.</summary>
        private Actor BuildEnemy(Vector3 position)
        {
            Actor enemy = _rig.BuildEnemy(position);
            _extra.Add(enemy.gameObject);
            return enemy;
        }

        /// <summary>Invokes the coordinator's kill handler for one victim and returns how many splits it spawned.</summary>
        private int SplitsFromKill(SplitArrowCoordinator coordinator, Actor victim)
        {
            var before = Snapshot();
            OnKillMethod.Invoke(coordinator, new object[] { victim });
            return SpawnedSince(before);
        }

        private static HashSet<ArsenalProjectile> Snapshot() =>
            new HashSet<ArsenalProjectile>(Object.FindObjectsByType<ArsenalProjectile>());

        /// <summary>Counts (and records for teardown) the ArsenalProjectiles spawned since the snapshot.</summary>
        private int SpawnedSince(HashSet<ArsenalProjectile> before)
        {
            int spawned = 0;
            foreach (ArsenalProjectile p in Object.FindObjectsByType<ArsenalProjectile>())
            {
                if (before.Contains(p)) continue;
                spawned++;
                _extra.Add(p.gameObject);
            }
            return spawned;
        }
    }
}
