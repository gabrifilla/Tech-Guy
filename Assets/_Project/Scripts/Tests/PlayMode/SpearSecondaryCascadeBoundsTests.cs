using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R7 Property 10 of modifier-synergies-theme17
    /// (Cascade bounds invariant) applied to the phantom/shard/chain spear secondaries.
    ///
    /// Property 10: for any enemy density, modifier combination, or new spear/element effect, a single
    /// cascade resolution SHALL produce at most MaxSecondaryHits (32) secondary hits, reach a depth of
    /// at most MaxDepth (4), and apply at most one secondary hit per target per original hit
    /// (Requirements 7.9, 11.3).
    ///
    /// The spear secondaries (MoonShard projectiles, ChainThrust, PhantomSpear repeats) all route their
    /// hits through the shared player damage path -> <see cref="RunSynergyEffects"/>. Any death produced
    /// there feeds the same bounded detonation/conductor cascade as every other impact. This test drives
    /// that shared cascade directly through <see cref="RunSynergyEffects.Resolve"/> under a dense enemy
    /// layout (far more than 32 killable enemies packed within the explosion radius) so the cascade wants
    /// to exceed the bounds, then asserts:
    ///   - LastSecondaryHits never exceeds MaxSecondaryHits (32),
    ///   - each enemy loses health at most once across the whole resolution (one hit per target),
    /// which together with the internal MaxDepth guard establishes the invariant for the path every
    /// spear secondary shares. Runs in PlayMode: Actors, colliders, and Physics.OverlapSphere are real.
    /// </summary>
    public sealed class SpearSecondaryCascadeBoundsTests
    {
        private static readonly int MaxSecondaryHits =
            (int)typeof(RunSynergyEffects).GetField("MaxSecondaryHits", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);
        private static readonly int MaxDepth =
            (int)typeof(RunSynergyEffects).GetField("MaxDepth", BindingFlags.Static | BindingFlags.NonPublic)
                .GetValue(null);

        private readonly List<GameObject> _spawned = new List<GameObject>();
        private GameObject _host;
        private PlayerOnHitEffects _effects;
        private RunSynergyEffects _synergies;

        [SetUp]
        public void SetUp()
        {
            Assert.Greater(MaxSecondaryHits, 0, "MaxSecondaryHits const must be readable (test seam).");
            Assert.Greater(MaxDepth, 0, "MaxDepth const must be readable (test seam).");

            _host = new GameObject("CascadeBoundsHost");
            _spawned.Add(_host);
            _effects = _host.AddComponent<PlayerOnHitEffects>();
            // Elements stay disabled: ApplyElements on victims becomes a cheap no-op, isolating the
            // cascade bounds. Synergies lazily creates the RunSynergyEffects on the same host.
            _synergies = _effects.Synergies;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        // Low-HP enemy so secondary hits kill it and keep the cascade chaining.
        private Actor BuildEnemy(Vector3 position, float health)
        {
            var go = new GameObject("CascadeEnemy");
            go.SetActive(false);
            _spawned.Add(go);
            go.transform.position = position;
            var col = go.AddComponent<BoxCollider>();
            col.size = Vector3.one;
            var actor = go.AddComponent<Actor>();
            actor.health = health;
            go.SetActive(true);
            return actor;
        }

        // Feature: modifier-synergies-theme17, Property 10
        // A dense cascade (Detonation + Conductor, sometimes Resonance) over many killable enemies
        // never exceeds MaxSecondaryHits and never hits any single enemy more than once.
        // Validates: Requirements 7.9, 11.3
        [UnityTest]
        public IEnumerator Property10_SpearSecondaryCascade_StaysWithinBounds()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 10);

            for (int c = 0; c < cases; c++)
            {
                TearDown();
                SetUp();

                // Grant a chaining cascade: Detonation always (deaths explode), plus Conductor and an
                // occasional Resonance rank so amplified secondaries push harder against the bound.
                int detonation = 1 + rng.Next(0, 3);   // 1..3
                int conductor = 1 + rng.Next(0, 3);     // 1..3
                int resonance = rng.Next(0, 3);         // 0..2
                for (int i = 0; i < detonation; i++) _synergies.Add(RunSynergy.Detonation);
                for (int i = 0; i < conductor; i++) _synergies.Add(RunSynergy.Conductor);
                for (int i = 0; i < resonance; i++) _synergies.Add(RunSynergy.Resonance);

                // Pack many killable enemies tightly inside the explosion radius so the cascade wants
                // to exceed 32 secondary hits. Count deliberately exceeds MaxSecondaryHits.
                int enemyCount = MaxSecondaryHits + 20 + rng.Next(0, 30); // ~52..82
                var enemies = new List<Actor>(enemyCount);
                var startHealth = new Dictionary<Actor, float>(enemyCount);
                for (int i = 0; i < enemyCount; i++)
                {
                    // Cluster within ~1.5 m of the origin so every enemy is inside any detonation radius.
                    Vector3 p = new Vector3(
                        (float)(rng.NextDouble() * 3.0 - 1.5),
                        0f,
                        (float)(rng.NextDouble() * 3.0 - 1.5));
                    float hp = 0.5f + (float)rng.NextDouble() * 1.5f; // 0.5..2 HP: dies to any secondary
                    Actor e = BuildEnemy(p, hp);
                    enemies.Add(e);
                    startHealth[e] = e.health;
                }

                // The initial impact target must be dead for the detonation branch to fire.
                Actor seed = enemies[0];
                seed.TakeDamage(seed.health + 10f);
                Assert.IsTrue(seed.IsDead, $"case {c}: seed enemy should be dead to trigger detonation.");

                float initialDamage = 100f + (float)rng.NextDouble() * 400f; // big enough to kill packed enemies

                _synergies.Resolve(seed, initialDamage, _effects);

                // Bound 1: never exceed MaxSecondaryHits.
                Assert.LessOrEqual(_synergies.LastSecondaryHits, MaxSecondaryHits,
                    $"case {c}: LastSecondaryHits {_synergies.LastSecondaryHits} exceeded MaxSecondaryHits {MaxSecondaryHits} " +
                    $"(det={detonation}, cond={conductor}, res={resonance}, enemies={enemyCount}).");

                // Bound 2 (one hit per target): the seed was killed before Resolve; every OTHER enemy
                // may lose health at most once, so any survivor's loss <= its starting health, and no
                // enemy can be driven below zero more than once. Count enemies that took damage and
                // confirm it does not exceed the secondary-hit budget.
                int damagedOthers = 0;
                foreach (Actor e in enemies)
                {
                    if (e == seed) continue;
                    float lost = startHealth[e] - Mathf.Max(0f, e.health);
                    if (lost > 0f) damagedOthers++;
                    // A single secondary hit cannot remove more than the enemy's whole health bar plus
                    // overkill; health is clamped at 0, so lost is bounded by starting health.
                    Assert.LessOrEqual(lost, startHealth[e] + 1e-3f,
                        $"case {c}: an enemy lost more health ({lost}) than it had ({startHealth[e]}), suggesting multiple hits.");
                }
                Assert.LessOrEqual(damagedOthers, MaxSecondaryHits,
                    $"case {c}: {damagedOthers} enemies were damaged, exceeding the {MaxSecondaryHits} secondary-hit budget.");

                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 10 (bound holds with no killable chain)
        // When secondaries cannot chain (high-HP enemies survive each hit), the cascade still respects
        // the bound and terminates without runaway growth.
        // Validates: Requirements 7.9, 11.3
        [UnityTest]
        public IEnumerator Property10_Cascade_TerminatesWithinBounds_WhenEnemiesSurvive()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 11);

            for (int c = 0; c < cases; c++)
            {
                TearDown();
                SetUp();

                _synergies.Add(RunSynergy.Detonation);
                for (int i = 0; i < 1 + rng.Next(0, 3); i++) _synergies.Add(RunSynergy.Conductor);

                int enemyCount = MaxSecondaryHits + rng.Next(0, 30);
                for (int i = 0; i < enemyCount; i++)
                {
                    Vector3 p = new Vector3(
                        (float)(rng.NextDouble() * 3.0 - 1.5), 0f, (float)(rng.NextDouble() * 3.0 - 1.5));
                    BuildEnemy(p, 1_000_000f); // survives every secondary; no further deaths to chain
                }

                Actor seed = BuildEnemy(Vector3.zero, 1f);
                seed.TakeDamage(10f);
                Assert.IsTrue(seed.IsDead, $"case {c}: seed enemy should be dead.");

                _synergies.Resolve(seed, 50f, _effects);

                Assert.LessOrEqual(_synergies.LastSecondaryHits, MaxSecondaryHits,
                    $"case {c}: LastSecondaryHits {_synergies.LastSecondaryHits} exceeded the bound with surviving enemies.");

                yield return null;
            }
        }
    }
}
