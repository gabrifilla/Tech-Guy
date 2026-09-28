using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R3 Property 10 of modifier-synergies-theme17
    /// (Cascade bounds invariant on the detonation path, <see cref="RunSynergyEffects.Resolve"/>).
    ///
    /// However a detonation cascade fans out across combustion and shatter deaths, it stays within
    /// the hard bounds: at most <c>MaxSecondaryHits = 32</c> secondary hits, generations never exceed
    /// <c>MaxDepth = 4</c>, and no enemy is dealt more than one secondary hit per original hit (the
    /// <c>_visited</c> set). This is stressed with a dense grid of low-health enemies — every fragment
    /// kills its victim, so victims re-detonate and the cascade chains through several generations —
    /// with burn/chill sprinkled so both combustion and shatter classifications occur in the same run.
    ///
    /// The per-target one-hit rule is checked by counting, per enemy, how many times the cascade dealt
    /// it damage after the initial (manual) kill. The depth bound is enforced structurally by the
    /// <c>impact.Depth &gt;= MaxDepth</c> guard; here it is asserted through its consequence — the run
    /// terminates and the total hit count never breaches 32. Runs in PlayMode: real Actors, real
    /// status components, real overlap/raycast reachability.
    /// </summary>
    public sealed class DetonationCascadeBoundsTests
    {
        private const int MaxSecondaryHits = 32;

        private DetonationTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new DetonationTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 10: a detonation cascade (combustion/shatter
        // mixed) never exceeds 32 secondary hits, never hits a target more than once per original
        // hit, and always terminates (depth bounded by MaxDepth=4).
        // Validates: Requirements 3.4, 7.9, 8.11, 11.3
        [UnityTest]
        public IEnumerator Property10_CascadeStaysWithinBounds()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 10);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int detonation = 1 + rng.Next(0, 3);       // 1..3
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();                          // cascade applies burn to victims

                // Dense grid of fragile enemies so fragments kill their victims and chain onward,
                // pressing against MaxDepth and MaxSecondaryHits.
                int side = 6 + rng.Next(0, 3);              // 6..8 -> 36..64 enemies
                float spacing = 0.9f + (float)rng.NextDouble() * 0.5f; // 0.9..1.4 units apart
                var perActorHits = new Dictionary<Actor, int>();

                Actor seed = null;
                for (int x = 0; x < side; x++)
                for (int z = 0; z < side; z++)
                {
                    var pos = new Vector3(x * spacing, 0f, z * spacing);
                    // Fragile so any fragment (>= a few damage) is lethal and re-detonates.
                    Actor enemy = _rig.BuildEnemy(pos, health: 1f + (float)rng.NextDouble() * 3f);
                    // Randomly pre-arm elements so both combustion and shatter occur in the cascade.
                    int roll = rng.Next(0, 4);
                    if (roll == 1 || roll == 3) _rig.GiveBurn(enemy);
                    if (roll == 2 || roll == 3) _rig.GiveChill(enemy);
                    enemy.DamageReceived += (a, _) =>
                    {
                        perActorHits.TryGetValue(a, out int n);
                        perActorHits[a] = n + 1;
                    };
                    if (x == side / 2 && z == side / 2) seed = enemy;
                }

                // Kill the central enemy so it detonates; the manual kill is one DamageReceived on the
                // seed and is excluded from the per-target cascade-hit accounting below.
                _rig.KillInPlace(seed);
                Assert.IsTrue(seed.IsDead, $"case {c}: seed enemy must be dead to detonate");

                _rig.Resolve(seed, 60f);

                int hits = _rig.Synergies.LastSecondaryHits;
                Assert.LessOrEqual(hits, MaxSecondaryHits,
                    $"case {c}: cascade produced {hits} secondary hits, exceeds cap {MaxSecondaryHits}");

                // One secondary hit per target: no enemy takes more than one cascade hit. The seed's
                // own initial kill is a separate DamageReceived, so allow the seed up to 2 total
                // (initial kill + at most its own single fragment if ever re-entered — which _visited
                // forbids, so in practice the seed is never a cascade victim).
                foreach (KeyValuePair<Actor, int> kv in perActorHits)
                {
                    int allowed = kv.Key == seed ? 2 : 1;
                    Assert.LessOrEqual(kv.Value, allowed,
                        $"case {c}: enemy {kv.Key.name} took {kv.Value} hits, expected <= {allowed} " +
                        $"(one secondary hit per target per original hit)");
                }
                yield return null;
            }
        }
    }
}
