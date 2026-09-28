using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R2 Property 7 of modifier-synergies-theme17
    /// (Thermal Shock preserves both statuses, <see cref="PlayerOnHitEffects.ApplyElements"/>).
    ///
    /// When Thermal Shock triggers, both the BurnStatus and the ChillStatus remain present on the
    /// enemy with their remaining durations unchanged by the trigger — the shock removes neither
    /// element and refreshes neither timer. The scenario applies frost onto a pre-existing burn
    /// (burn is NOT re-applied this call), so the burn's remaining duration sampled before the call
    /// must be identical afterward: any change would come from the shock. The chill is created by the
    /// same call and must simply remain present.
    ///
    /// Runs in PlayMode because BurnStatus/ChillStatus are MonoBehaviour components on a real Actor.
    /// StatusEffect.RemainingTime is read via the rig's reflection accessor. No Update runs during the
    /// synchronous call, so time decay cannot confound the duration comparison.
    /// </summary>
    public sealed class ThermalShockStatusPreservationTests
    {
        private ThermalShockTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ThermalShockTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 7: after a Thermal Shock trigger both statuses
        // remain present with remaining durations unchanged by the trigger.
        // Validates: Requirements 2.5
        [UnityTest]
        public IEnumerator Property7_BothStatusesRemainWithUnchangedDurations()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 7);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.5f + (float)rng.NextDouble() * 3f;
                float damage = 5f + (float)rng.NextDouble() * 20f;
                float burnDuration = 2f + (float)rng.NextDouble() * 8f; // 2..10

                _rig.ConfigureThermalShock(multiplier);
                // Enable frost only (chance 1); burn is pre-existing and NOT re-applied this call.
                _rig.EnableChill(1f);

                Actor enemy = _rig.BuildEnemy();
                BurnStatus burn = _rig.GiveBurn(enemy, dps: 4f, duration: burnDuration);
                Assert.IsNotNull(burn, $"case {c}: pre-existing burn should exist");

                float burnBefore = _rig.Remaining(burn);

                float healthBefore = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage); // frost meets existing burn -> one shock
                float dealt = healthBefore - enemy.health;

                // The shock must have fired (frost applied onto existing burn).
                Assert.Greater(dealt, 0f, $"case {c}: expected a Thermal Shock burst");

                // Both statuses still present...
                Assert.IsTrue(_rig.HasBurn(enemy), $"case {c}: burn was removed by the shock");
                Assert.IsTrue(_rig.HasChill(enemy), $"case {c}: chill was removed by the shock");

                // ...and the pre-existing burn's remaining duration is unchanged by the trigger.
                float burnAfter = _rig.Remaining(burn);
                Assert.AreEqual(burnBefore, burnAfter, 1e-4f,
                    $"case {c}: burn duration changed by the trigger: {burnBefore} -> {burnAfter}");
                yield return null;
            }
        }
    }
}
