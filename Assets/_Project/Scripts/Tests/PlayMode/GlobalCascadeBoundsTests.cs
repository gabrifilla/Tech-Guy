using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for the global (end-to-end) form of Property 10 of
    /// modifier-synergies-theme17 (cascade bounds invariant), covering R11 task 15.6.
    ///
    /// This is the broadest cascade-bounds property: across <em>any</em> modifier combination
    /// (randomized Detonation/Conductor/Reactor/Resonance ranks), burn/chill pre-arming that mixes
    /// combustion and shatter deaths, dense enemy layouts, <em>and</em> a live <see cref="HookBus"/>
    /// subscriber that itself re-enters the cascade engine (calling
    /// <see cref="RunSynergyEffects.ReportImpact"/> and <see cref="RunSynergyEffects.Resolve"/> from
    /// inside an <c>OnExplosion</c> notification), the run stays inside the hard bounds:
    /// <list type="bullet">
    /// <item>at most <c>MaxSecondaryHits = 32</c> secondary hits (R3.4/8.11/11.3);</item>
    /// <item>generations bounded by <c>MaxDepth = 4</c> — asserted through its consequence, the cascade
    /// always terminates and never breaches the hit cap;</item>
    /// <item>at most one secondary hit per target per original hit (the <c>_visited</c> set).</item>
    /// </list>
    ///
    /// The subscriber's re-entrancy is the key stress for R8.11: a hook listener that tries to spawn
    /// more secondary hits must not be able to push the total past the cap. <see cref="RunSynergyEffects"/>
    /// guards re-entrant <c>Resolve</c>/<c>ReportImpact</c> calls with its <c>_resolving</c> flag, so
    /// a subscriber firing mid-cascade cannot start a second, unbounded cascade.
    ///
    /// The HookBus is wired through real <c>RunBoons</c> ownership (see
    /// <see cref="DetonationTestRig.AttachHookBus"/>), so <c>Resolve</c> raises real notifications.
    /// Runs in PlayMode: real Actors, real status components, real overlap/raycast reachability.
    /// </summary>
    public sealed class GlobalCascadeBoundsTests
    {
        private const int MaxSecondaryHits = 32;

        private DetonationTestRig _rig;

        [TearDown]
        public void TearDown()
        {
            _rig?.TearDown();
            _rig = null;
        }

        // Feature: modifier-synergies-theme17, Property 10 (global): across any modifier combination,
        // a re-entrant hook subscriber, and mixed spear/element/detonation effects, the cascade never
        // exceeds 32 secondary hits, never hits a target more than once per original hit, and always
        // terminates (depth bounded by MaxDepth=4).
        // Validates: Requirements 3.4, 7.9, 8.11, 11.3
        [UnityTest]
        public IEnumerator Property10_GlobalCascadeStaysWithinBounds()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 106);

            for (int c = 0; c < cases; c++)
            {
                _rig?.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                // Randomize the full synergy mix. At least one of detonation/conductor must be > 0 so
                // the cascade actually fans out; resonance/reactor add amplification pressure.
                int detonation = rng.Next(0, 4);
                int conductor = rng.Next(0, 4);
                int reactor = rng.Next(0, 4);
                int resonance = rng.Next(0, 4);
                if (detonation == 0 && conductor == 0) detonation = 1 + rng.Next(0, 3);
                _rig.AddDetonation(detonation);
                _rig.AddConductor(conductor);
                _rig.AddReactor(reactor);
                for (int r = 0; r < resonance; r++) _rig.Synergies.Add(RunSynergy.Resonance);
                _rig.EnableBurn();

                // A live HookBus whose OnExplosion subscriber re-enters the cascade engine. Under the
                // _resolving guard these re-entrant calls must be no-ops that cannot exceed the cap.
                HookBus hooks = _rig.AttachHookBus();
                int explosionCount = 0;
                hooks.OnExplosion += origin =>
                {
                    explosionCount++;
                    // A malicious/naive subscriber tries to trigger more secondary hits mid-cascade.
                    _rig.Synergies.Resolve(_rig.Seed, 40f, _rig.Effects);
                    _rig.Synergies.ReportImpact(_rig.Seed, 40f, _rig.Effects);
                };

                // Dense grid of fragile enemies so fragments kill their victims and chain onward,
                // pressing against MaxDepth and MaxSecondaryHits.
                int side = 6 + rng.Next(0, 3); // 6..8 -> 36..64 enemies
                float spacing = 0.9f + (float)rng.NextDouble() * 0.5f;
                var perActorHits = new Dictionary<Actor, int>();

                Actor seed = null;
                for (int x = 0; x < side; x++)
                for (int z = 0; z < side; z++)
                {
                    var pos = new Vector3(x * spacing, 0f, z * spacing);
                    Actor enemy = _rig.BuildEnemy(pos, health: 1f + (float)rng.NextDouble() * 3f);
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

                _rig.SetSeed(seed);
                _rig.KillInPlace(seed);
                Assert.IsTrue(seed.IsDead, $"case {c}: seed enemy must be dead to detonate");

                _rig.Resolve(seed, 60f);

                int hits = _rig.Synergies.LastSecondaryHits;
                Assert.LessOrEqual(hits, MaxSecondaryHits,
                    $"case {c}: cascade produced {hits} secondary hits, exceeds cap {MaxSecondaryHits} " +
                    $"(detonation={detonation}, conductor={conductor}, reactor={reactor}, resonance={resonance}, " +
                    $"explosions={explosionCount})");

                // One secondary hit per target per original hit. The seed's own initial kill is a
                // separate DamageReceived, so allow it up to 2 total.
                foreach (KeyValuePair<Actor, int> kv in perActorHits)
                {
                    int allowed = kv.Key == seed ? 2 : 1;
                    Assert.LessOrEqual(kv.Value, allowed,
                        $"case {c}: enemy {kv.Key.name} took {kv.Value} hits, expected <= {allowed} " +
                        "(one secondary hit per target per original hit)");
                }

                yield return null;
            }
        }
    }
}
