using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R2 Property 5 of modifier-synergies-theme17
    /// (no Thermal Shock without an active opposite element, <see cref="PlayerOnHitEffects.ApplyElements"/>).
    ///
    /// If an element application does not result in that element becoming active on the enemy — for
    /// example the chill-chance roll does not succeed and no ChillStatus is added or refreshed — then
    /// no Thermal Shock is triggered for that application, even when the opposite element is present.
    /// The chill roll is driven through the seeded RNG seam
    /// (<see cref="PlayerOnHitEffects.SetChillRollForTests"/>): a roll above the configured chance =
    /// failed roll = no frost applied = no shock.
    ///
    /// Runs in PlayMode because BurnStatus/ChillStatus are MonoBehaviour components on a real Actor
    /// and Thermal Shock calls <c>Actor.TakeDamage</c>. Shock presence is measured by dealt damage
    /// (Resonance rank 0, so the amplified impact path is a no-op).
    /// </summary>
    public sealed class ThermalShockNoOppositeElementTests
    {
        private ThermalShockTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ThermalShockTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 5: a failed chill roll means frost never
        // becomes active, so frost-onto-existing-burn triggers no Thermal Shock.
        // Validates: Requirements 2.3
        [UnityTest]
        public IEnumerator Property5_FailedChillRollTriggersNoShock()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 5);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.5f + (float)rng.NextDouble() * 3f;
                float damage = 5f + (float)rng.NextDouble() * 20f;
                // Configured chance in (0,1); the roll is forced strictly above it so it always fails.
                float chance = 0.1f + (float)rng.NextDouble() * 0.8f; // 0.1..0.9

                _rig.ConfigureThermalShock(multiplier);
                _rig.EnableChill(chance); // this also installs a deterministic 0-roll...
                // ...override it with a roll strictly greater than the chance -> frost never applies.
                float failedRoll = Mathf.Clamp01(chance + 0.05f + (float)rng.NextDouble() * (1f - chance));
                _rig.SetChillRoll(() => failedRoll);

                Actor enemy = _rig.BuildEnemy();
                _rig.GiveBurn(enemy); // opposite element present, but frost will fail to apply

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                Assert.IsFalse(_rig.HasChill(enemy),
                    $"case {c}: chill should not have applied (roll={failedRoll} > chance={chance})");
                Assert.LessOrEqual(dealt, 1e-3f,
                    $"case {c}: no shock expected when frost failed to apply, but dealt={dealt}");
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 5 (no opposite element present): applying a
        // single element with no pre-existing opposite never triggers Thermal Shock.
        // Validates: Requirements 2.3
        [UnityTest]
        public IEnumerator Property5_NoPreexistingOppositeTriggersNoShock()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 55);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.5f + (float)rng.NextDouble() * 3f;
                float damage = 5f + (float)rng.NextDouble() * 20f;
                bool applyFire = rng.Next(0, 2) == 0;

                _rig.ConfigureThermalShock(multiplier);
                if (applyFire) _rig.EnableBurn();
                else _rig.EnableChill(1f); // frost always applies, but no burn present

                Actor enemy = _rig.BuildEnemy(); // no pre-existing status of any kind

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                Assert.LessOrEqual(dealt, 1e-3f,
                    $"case {c}: applying {(applyFire ? "fire" : "frost")} with no opposite element " +
                    $"should not shock, but dealt={dealt}");
                yield return null;
            }
        }
    }
}
