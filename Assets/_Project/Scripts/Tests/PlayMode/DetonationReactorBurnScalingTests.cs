using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R3 Property 11 of modifier-synergies-theme17
    /// (Reactor scales burn without a new DoT, <see cref="PlayerOnHitEffects.ApplyElements"/> /
    /// <see cref="RunSynergyEffects.BurnScaling"/>).
    ///
    /// Reactor amplifies an enemy's burn purely through the existing burn-scaling rule
    /// (<c>BurnScaling = 0.12 * Rank(Reactor)</c>, added into the burn's damage-per-second when
    /// <c>ApplyElements</c> applies fire). It must not spawn a second damage-over-time effect: after
    /// any number of Reactor-scaled applications the enemy carries exactly one <see cref="BurnStatus"/>
    /// (<c>[DisallowMultipleComponent]</c> guarantees a single component) and no other DoT type, and a
    /// Reactor-scaled burn ticks for strictly more than an unscaled burn of the same base — the
    /// amplification lives in the one burn, not in an additional effect.
    ///
    /// Runs in PlayMode: BurnStatus is a MonoBehaviour on a real Actor and its tick calls
    /// <c>Actor.TakeDamage</c>.
    /// </summary>
    public sealed class DetonationReactorBurnScalingTests
    {
        private DetonationTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new DetonationTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 11: with Reactor active, repeated on-hit
        // applications leave exactly one BurnStatus and no separate DoT; BurnScaling = 0.12*rank and
        // the single burn ticks for more than an equivalent unscaled burn.
        // Validates: Requirements 3.5
        [UnityTest]
        public IEnumerator Property11_ReactorScalesTheSingleBurn_NoExtraDoT()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 11);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();

                int reactor = 1 + rng.Next(0, 3);              // 1..3
                _rig.AddReactor(reactor);
                _rig.EnableBurn(dps: 4f, duration: 6f);

                // BurnScaling exposed by the cascade must equal 0.12 * Reactor rank exactly.
                Assert.AreEqual(0.12f * reactor, _rig.Synergies.BurnScaling, 1e-5f,
                    $"case {c}: BurnScaling should be 0.12*{reactor}");

                Actor enemy = _rig.BuildEnemy(Vector3.zero);

                // Apply on-hit elements several times; DisallowMultipleComponent must keep one burn.
                int applications = 1 + rng.Next(0, 5);         // 1..5
                float damage = 5f + (float)rng.NextDouble() * 20f;
                for (int i = 0; i < applications; i++)
                    _rig.Effects.ApplyElements(enemy, damage);

                Assert.AreEqual(1, _rig.BurnCount(enemy),
                    $"case {c}: expected exactly one BurnStatus after {applications} applications, " +
                    $"saw {_rig.BurnCount(enemy)} (a new DoT was created)");

                // A Reactor-scaled burn tick must exceed an unscaled burn tick of the same base dps.
                BurnStatus scaledBurn = enemy.GetComponent<BurnStatus>();
                float scaledTick = SingleTickDamage(scaledBurn, enemy);

                // Control: identical base burn with NO Reactor scaling on a fresh enemy.
                _rig.TearDown();
                _rig = new DetonationTestRig();
                _rig.Build();
                _rig.EnableBurn(dps: 4f, duration: 6f);        // no Reactor granted
                Actor plain = _rig.BuildEnemy(Vector3.zero);
                for (int i = 0; i < applications; i++)
                    _rig.Effects.ApplyElements(plain, damage);
                Assert.AreEqual(1, _rig.BurnCount(plain), $"case {c}: control should carry one burn");
                float unscaledTick = SingleTickDamage(plain.GetComponent<BurnStatus>(), plain);

                Assert.Greater(scaledTick, unscaledTick,
                    $"case {c}: Reactor-scaled burn tick ({scaledTick}) must exceed unscaled " +
                    $"({unscaledTick}); the amplification must live in the single burn");
                yield return null;
            }
        }

        /// <summary>Measures the damage of one burn tick by invoking the burn's tick and reading health delta.</summary>
        private float SingleTickDamage(BurnStatus burn, Actor enemy)
        {
            float before = enemy.health;
            _rig.TickBurnOnce(burn); // one tick interval, drives Host.TakeDamage(_damagePerTick)
            return before - enemy.health;
        }
    }
}
