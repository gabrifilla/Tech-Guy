using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R3 Property 12 of modifier-synergies-theme17
    /// (Isolated burn ticks do not detonate, <see cref="BurnStatus.Tick"/> vs
    /// <see cref="RunSynergyEffects.Resolve"/>).
    ///
    /// A burn's damage-over-time tick calls <c>Actor.TakeDamage</c> directly and is never wired to
    /// the cascade engine, so a burn tick — even one that reduces the enemy to zero health and marks
    /// it dead — must not start a detonation. With Detonation active and reachable neighbours present,
    /// simulating any number of burn ticks (including a lethal final tick) leaves
    /// <see cref="RunSynergyEffects.LastSecondaryHits"/> at zero and deals no damage to the
    /// neighbours: only a death routed through <c>Resolve</c> (an actual attack/kill event) detonates.
    ///
    /// Runs in PlayMode: BurnStatus is a MonoBehaviour on a real Actor and its tick goes through
    /// <c>Actor.TakeDamage</c>; the neighbours are real collidered Actors that a stray cascade would
    /// otherwise reach.
    /// </summary>
    public sealed class DetonationIsolatedBurnTickTests
    {
        private DetonationTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new DetonationTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 12: isolated burn ticks (including a lethal
        // one) never initiate a detonation — LastSecondaryHits stays 0 and no neighbour is hit.
        // Validates: Requirements 3.6
        [UnityTest]
        public IEnumerator Property12_IsolatedBurnTicksDoNotDetonate()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 12);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int detonation = 1 + rng.Next(0, 3);           // Detonation active
                _rig.AddDetonation(detonation);
                _rig.EnableBurn();

                // A fragile burning enemy surrounded by neighbours a real detonation would reach.
                float health = 3f + (float)rng.NextDouble() * 6f; // 3..9
                Actor burning = _rig.BuildEnemy(Vector3.zero, health: health);
                BurnStatus burn = _rig.GiveBurn(burning, dps: 8f, duration: 10f);

                int neighbours = 3 + rng.Next(0, 4);           // 3..6 nearby enemies
                float ringRadius = _rig.BaseRadius * 0.6f;
                int neighbourDamageEvents = 0;
                for (int i = 0; i < neighbours; i++)
                {
                    float angle = i * (Mathf.PI * 2f / neighbours);
                    Actor n = _rig.BuildEnemy(
                        new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius),
                        health: 1000f);
                    n.DamageReceived += (_, __) => neighbourDamageEvents++;
                }

                // Tick the burn repeatedly. Each tick deals _damagePerTick via Host.TakeDamage only.
                // Keep ticking until the enemy dies from the DoT (or a safe cap), so a lethal tick is
                // exercised — that lethal tick still must not detonate.
                int ticks = 0;
                while (!burning.IsDead && ticks < 64)
                {
                    _rig.TickBurnOnce(burn);
                    ticks++;
                    Assert.AreEqual(0, _rig.Synergies.LastSecondaryHits,
                        $"case {c}: a burn tick started a detonation (LastSecondaryHits != 0)");
                }

                Assert.IsTrue(burning.IsDead,
                    $"case {c}: the burn should eventually kill the enemy within the tick cap");
                Assert.AreEqual(0, _rig.Synergies.LastSecondaryHits,
                    $"case {c}: even the lethal burn tick must not detonate");
                Assert.AreEqual(0, neighbourDamageEvents,
                    $"case {c}: no neighbour should take cascade damage from a burn-tick death");
                yield return null;
            }
        }
    }
}
