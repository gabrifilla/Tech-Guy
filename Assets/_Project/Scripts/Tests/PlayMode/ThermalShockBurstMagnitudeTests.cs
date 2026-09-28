using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R2 Property 6 of modifier-synergies-theme17
    /// (Thermal Shock burst magnitude, <see cref="PlayerOnHitEffects.ApplyElements"/>).
    ///
    /// When Thermal Shock triggers it applies exactly one instantaneous damage instance whose
    /// magnitude is the triggering hit's damage times the configured multiple (d * m, > 0), applied
    /// in addition to and distinct from the ongoing burn/chill damage-over-time. The burst is
    /// verified as a single synchronous <c>Actor.TakeDamage</c> during the <c>ApplyElements</c> call
    /// (DoT ticks happen later in <c>Update</c>, not during the call), captured via
    /// <c>Actor.DamageReceived</c>.
    ///
    /// Runs in PlayMode because the statuses are MonoBehaviour components on a real Actor and the
    /// burst goes through <c>Actor.TakeDamage</c>. Resonance rank is 0 so the burst is not amplified.
    /// </summary>
    public sealed class ThermalShockBurstMagnitudeTests
    {
        private ThermalShockTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ThermalShockTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 6: a single triggering application produces
        // exactly one instantaneous burst of magnitude d * m (> 0), distinct from the DoT.
        // Validates: Requirements 2.4
        [UnityTest]
        public IEnumerator Property6_BurstIsSingleInstantaneousDamageTimesMultiplier()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 6);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.25f + (float)rng.NextDouble() * 3f; // > 0
                float damage = 5f + (float)rng.NextDouble() * 20f;        // > 0

                _rig.ConfigureThermalShock(multiplier);
                _rig.EnableBurn(); // fire becomes active this call

                Actor enemy = _rig.BuildEnemy();
                _rig.GiveChill(enemy); // fire will meet this existing chill -> exactly one shock

                // Capture instantaneous damage instances applied synchronously during the call.
                var bursts = new List<float>();
                void OnDamage(Actor a, float amount) => bursts.Add(amount);
                enemy.DamageReceived += OnDamage;

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                enemy.DamageReceived -= OnDamage;

                float expected = damage * multiplier;

                Assert.Greater(expected, 0f, $"case {c}: configured burst must be > 0");
                Assert.AreEqual(1, bursts.Count,
                    $"case {c}: expected exactly one instantaneous burst, saw {bursts.Count}");
                Assert.LessOrEqual(Mathf.Abs(bursts[0] - expected), 1e-2f * Mathf.Max(1f, expected),
                    $"case {c}: burst magnitude={bursts[0]}, expected d*m={expected}");
                Assert.LessOrEqual(Mathf.Abs(dealt - expected), 1e-2f * Mathf.Max(1f, expected),
                    $"case {c}: total synchronous damage={dealt}, expected single burst {expected}");

                // The burn DoT is present but must NOT have contributed to the synchronous burst:
                // it is a separate effect that ticks in Update.
                Assert.IsTrue(_rig.HasBurn(enemy), $"case {c}: burn DoT should coexist with the burst");
                yield return null;
            }
        }
    }
}
