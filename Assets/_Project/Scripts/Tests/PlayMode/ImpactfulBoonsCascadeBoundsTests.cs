using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for Property 13 of impactful-weapon-boons (cascade bounds invariant).
    ///
    /// Property 13 (design): <em>for any</em> enemy density and any combination of Split Arrow and
    /// Elemental Overflow, a single frame's secondary impacts SHALL not exceed
    /// <c>MaxSecondaryHits</c> (32). <b>Validates: Requirements 2.4, 8.3.</b>
    ///
    /// This is the impactful-weapon-boons complement to the modifier-synergies-theme17 cascade-bounds
    /// tests (<see cref="GlobalCascadeBoundsTests"/>/<c>DetonationCascadeBoundsTests</c>) and the Split
    /// Arrow generation-bound test (<see cref="SplitArrowGenerationBoundTests"/>). Where those bound each
    /// channel in isolation, this test presses BOTH new bounded channels together in one frame:
    /// <list type="bullet">
    /// <item><see cref="SplitArrowCoordinator"/> caps its per-frame spawns via <c>_spawnedThisFrame + rank
    /// &lt;= 32</c> (R2.4), so a dense mass kill never fans more than 32 split arrows in a frame.</item>
    /// <item><see cref="PlayerOnHitEffects"/>'s Elemental Overflow routes each burst through
    /// <see cref="RunSynergyEffects.ReportImpact"/>, which increments <c>LastSecondaryHits</c> and refuses
    /// once it reaches <c>MaxSecondaryHits</c> (R8.3), so a dense sequence of status-carrying hits never
    /// emits more than 32 bounded impacts in a frame.</item>
    /// </list>
    /// Both are exercised in the same frame (no <c>yield return</c> between them) over generated enemy
    /// densities, and each channel's per-frame total is asserted to stay at or below 32; advancing a
    /// frame is then shown to reset the split budget so the bound is per-frame, not a hard lifetime cap.
    ///
    /// Runs in PlayMode because Split Arrow fires real <see cref="ArsenalProjectile"/>s and Overflow runs
    /// through the real element registry + cascade engine against real <see cref="Actor"/>s/status
    /// components. The seeded harness drives &gt;= 100 deterministic generated cases.
    /// </summary>
    public sealed class ImpactfulBoonsCascadeBoundsTests
    {
        private const int MaxSecondaryHits = 32;

        private static readonly FieldInfo FiresArrowsField =
            typeof(WeaponScript).GetField("_firesArrows", BindingFlags.Instance | BindingFlags.NonPublic);

        private ArsenalProjectileTestRig _rig;
        private GameObject _playerGo;                       // separate host for the Overflow channel
        private readonly List<GameObject> _extra = new List<GameObject>();

        [SetUp]
        public void SetUp() => _rig = new ArsenalProjectileTestRig();

        [TearDown]
        public void TearDown()
        {
            foreach (ArsenalProjectile stray in Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None))
                if (stray) Object.DestroyImmediate(stray.gameObject);
            foreach (GameObject go in _extra) if (go) Object.DestroyImmediate(go);
            _extra.Clear();
            if (_playerGo) Object.DestroyImmediate(_playerGo);
            _playerGo = null;
            _rig?.TearDown();
        }

        // Feature: impactful-weapon-boons, Property 13: cascade bounds invariant.
        // Across any enemy density, driving Split Arrow kills AND Elemental Overflow status-hits in the
        // SAME frame keeps each bounded channel's per-frame total at or below MaxSecondaryHits (32):
        // Split Arrow spawns <= 32 arrows/frame, Overflow emits <= 32 bounded impacts/frame. Advancing a
        // frame resets the split budget.
        // Validates: Requirements 2.4, 8.3
        [UnityTest]
        public IEnumerator Property13_SplitArrowAndOverflowStayWithinPerFrameBudget()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 13);

            // --- Split Arrow channel: bow owner + coordinator ---
            _rig.BuildOwner();
            EquipBow();
            var splitHooks = new HookBus();
            SplitArrowCoordinator coordinator = _rig.Owner.gameObject.AddComponent<SplitArrowCoordinator>();

            // --- Overflow channel: its own player host with Resonance active so bursts route through
            // ReportImpact (the LastSecondaryHits-bounded path). ---
            _playerGo = new GameObject("ImpactfulCascadeOverflowPlayer");
            _playerGo.SetActive(false);
            PlayerOnHitEffects effects = _playerGo.AddComponent<PlayerOnHitEffects>();
            RunSynergyEffects synergies = effects.Synergies;
            _playerGo.SetActive(true);

            for (int c = 0; c < cases; c++)
            {
                int splitRank = 1 + rng.Next(0, 3);  // 1..3
                coordinator.Configure(_rig.Owner, splitHooks, splitRank);

                int overflowRank = 1 + rng.Next(0, 3);   // 1..3
                int resonanceRank = 1 + rng.Next(0, 3);  // 1..3 (active -> ReportImpact path)
                // Reset the two registries to the target ranks for this case.
                effects.Clear();
                for (int r = 0; r < overflowRank; r++) effects.EnableElementalOverflow();
                for (int r = 0; r < resonanceRank; r++) synergies.Add(RunSynergy.Resonance);

                // Dense frame: many simultaneous kills AND many status-carrying overflow hits, all in one
                // frame. rank*victims and hitCount both far exceed 32 so the bounds must clamp.
                int victims = 40 + rng.Next(0, 41);   // 40..80 kills this frame
                int overflowHits = 40 + rng.Next(0, 41); // 40..80 status hits this frame

                var beforeArrows = SnapshotArrows();

                // Split Arrow: mass kill in this frame.
                for (int v = 0; v < victims; v++)
                {
                    Vector3 pos = new Vector3(
                        (float)(rng.NextDouble() * 16.0 - 8.0), 0f,
                        (float)(rng.NextDouble() * 16.0 - 8.0));
                    Actor victim = BuildEnemy(pos);
                    splitHooks.RaiseKill(victim);
                }
                int splitsThisFrame = ArrowsSpawnedSince(beforeArrows);

                // Overflow: many status-carrying direct hits in the SAME frame. Each ReportImpact bumps
                // LastSecondaryHits; the engine refuses once it reaches the cap, so the running total is
                // the per-frame secondary-hit count for this channel.
                for (int h = 0; h < overflowHits; h++)
                {
                    Actor enemy = BuildEnemy(new Vector3(h, 0f, 0f), health: 100000f);
                    BurnStatus.Apply(enemy, 5f, 5f);
                    float damage = 5f + (float)rng.NextDouble() * 20f;
                    effects.ApplyTo(enemy, damage);
                }
                int overflowImpactsThisFrame = synergies.LastSecondaryHits;

                // R2.4: Split Arrow never fans more than 32 arrows in a frame.
                Assert.LessOrEqual(splitsThisFrame, MaxSecondaryHits,
                    $"case {c}: Split Arrow spawned {splitsThisFrame} arrows in one frame (rank {splitRank}), " +
                    $"exceeds cap {MaxSecondaryHits}");

                // R8.3: Overflow never emits more than 32 bounded impacts in a frame.
                Assert.LessOrEqual(overflowImpactsThisFrame, MaxSecondaryHits,
                    $"case {c}: Overflow emitted {overflowImpactsThisFrame} bounded impacts in one frame " +
                    $"(rank {overflowRank}, resonance {resonanceRank}), exceeds cap {MaxSecondaryHits}");

                // Property 13 (combined): the two bounded channels together stay inside their per-frame
                // budgets — neither can be pushed past 32 by the other's activity in the same frame.
                Assert.IsTrue(splitsThisFrame <= MaxSecondaryHits && overflowImpactsThisFrame <= MaxSecondaryHits,
                    $"case {c}: combined frame breached a per-frame budget " +
                    $"(splits={splitsThisFrame}, overflow={overflowImpactsThisFrame})");

                // Advance a frame; the split budget resets so a fresh kill fans again (per-frame, not lifetime).
                yield return null;
                var beforeReset = SnapshotArrows();
                Actor freshVictim = BuildEnemy(Vector3.forward * 3f);
                splitHooks.RaiseKill(freshVictim);
                int afterFrame = ArrowsSpawnedSince(beforeReset);
                Assert.AreEqual(splitRank, afterFrame,
                    $"case {c}: a new frame must reset the split budget and allow {splitRank} splits again");

                CleanupEnemiesAndArrows();
                yield return null;
            }
        }

        // ---- helpers ----

        private void EquipBow()
        {
            Assert.IsNotNull(FiresArrowsField, "WeaponScript._firesArrows field not found via reflection");
            FiresArrowsField.SetValue(_rig.Weapon, true);
            Assert.IsTrue(_rig.Weapon.FiresArrows, "test bow must report FiresArrows");
        }

        private Actor BuildEnemy(Vector3 position, float health = 1000f)
        {
            Actor enemy = _rig.BuildEnemy(position, health);
            _extra.Add(enemy.gameObject);
            return enemy;
        }

        private static HashSet<ArsenalProjectile> SnapshotArrows() =>
            new HashSet<ArsenalProjectile>(Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None));

        private int ArrowsSpawnedSince(HashSet<ArsenalProjectile> before)
        {
            int spawned = 0;
            foreach (ArsenalProjectile p in Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None))
            {
                if (before.Contains(p)) continue;
                spawned++;
                _extra.Add(p.gameObject);
            }
            return spawned;
        }

        private void CleanupEnemiesAndArrows()
        {
            foreach (ArsenalProjectile stray in Object.FindObjectsByType<ArsenalProjectile>(FindObjectsSortMode.None))
                if (stray) Object.DestroyImmediate(stray.gameObject);
            foreach (GameObject go in _extra) if (go) Object.DestroyImmediate(go);
            _extra.Clear();
        }
    }
}
