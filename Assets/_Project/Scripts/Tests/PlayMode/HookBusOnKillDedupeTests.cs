using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R8 Property 21 of modifier-synergies-theme17
    /// (<see cref="HookBus.RaiseKill"/> dedupe).
    ///
    /// Property 21: for any sequence of kill notifications — including repeats for the same enemy
    /// interleaved with distinct enemies — the bus SHALL invoke <c>OnKill</c> exactly once per
    /// distinct enemy (Requirement 8.3). The bus keys dedupe on the <see cref="Actor"/> reference
    /// via its internal <c>_killed</c> set.
    ///
    /// Runs in PlayMode because <c>OnKill</c> carries an <see cref="Actor"/> (a MonoBehaviour):
    /// creating an Actor runs its <c>Awake</c>, so real component lifecycle is required. Each
    /// generated case builds a fresh set of enemies and a fresh <see cref="HookBus"/>, subscribes a
    /// per-enemy counter, fires a randomized notification sequence (with repeats), and asserts every
    /// distinct enemy was reported exactly once and the total callback count equals the distinct
    /// enemy count.
    /// </summary>
    public sealed class HookBusOnKillDedupeTests
    {
        private HookBusTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new HookBusTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 21: OnKill fires exactly once per distinct
        // enemy, even when the same enemy is reported killed many times and distinct enemies are
        // interleaved. Validates: Requirements 8.3
        [UnityTest]
        public IEnumerator Property21_OnKillFiresOncePerDistinctEnemy()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 21);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new HookBusTestRig();

                // 1..6 distinct enemies; each will be notified 1..5 times in a shuffled sequence.
                int enemyCount = 1 + rng.Next(0, 6);
                var enemies = new List<Actor>(enemyCount);
                for (int e = 0; e < enemyCount; e++)
                    enemies.Add(_rig.BuildEnemy());

                var bus = new HookBus();
                var callbacks = new Dictionary<Actor, int>();
                foreach (Actor enemy in enemies) callbacks[enemy] = 0;

                int totalCallbacks = 0;
                bus.OnKill += a =>
                {
                    totalCallbacks++;
                    if (callbacks.ContainsKey(a)) callbacks[a]++;
                };

                // Build a notification sequence with repeats, then shuffle so repeats interleave
                // with distinct enemies (order must not affect the once-per-enemy guarantee).
                var sequence = new List<Actor>();
                foreach (Actor enemy in enemies)
                {
                    int repeats = 1 + rng.Next(0, 5); // 1..5 notifications for this enemy
                    for (int r = 0; r < repeats; r++) sequence.Add(enemy);
                }
                for (int s = sequence.Count - 1; s > 0; s--)
                {
                    int j = rng.Next(0, s + 1);
                    (sequence[s], sequence[j]) = (sequence[j], sequence[s]);
                }

                // A null notification must be ignored (guarded by the bus) and never counted.
                if (rng.Next(0, 3) == 0) sequence.Insert(rng.Next(0, sequence.Count + 1), null);

                foreach (Actor a in sequence) bus.RaiseKill(a);

                foreach (Actor enemy in enemies)
                    Assert.AreEqual(1, callbacks[enemy],
                        $"case {c}: enemy fired {callbacks[enemy]} times, expected exactly 1");

                Assert.AreEqual(enemyCount, totalCallbacks,
                    $"case {c}: total OnKill callbacks {totalCallbacks}, expected {enemyCount} (one per distinct enemy)");

                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 21 (dedupe scope): the dedupe set is
        // per-HookBus, so a fresh bus reports the same enemy again — dedupe is run-scoped state, not
        // a permanent block on the Actor. Validates: Requirements 8.3
        [UnityTest]
        public IEnumerator Property21_DedupeIsPerBusInstance()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 210);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new HookBusTestRig();

                Actor enemy = _rig.BuildEnemy();

                var busA = new HookBus();
                int firedA = 0;
                busA.OnKill += _ => firedA++;
                int repeatsA = 1 + rng.Next(0, 4);
                for (int r = 0; r < repeatsA; r++) busA.RaiseKill(enemy);
                Assert.AreEqual(1, firedA, $"case {c}: first bus should fire once for the enemy");

                // A second, independent bus has its own dedupe set and fires once for the same enemy.
                var busB = new HookBus();
                int firedB = 0;
                busB.OnKill += _ => firedB++;
                int repeatsB = 1 + rng.Next(0, 4);
                for (int r = 0; r < repeatsB; r++) busB.RaiseKill(enemy);
                Assert.AreEqual(1, firedB, $"case {c}: second bus should fire once for the same enemy");

                yield return null;
            }
        }
    }
}
