using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R2 Property 8 of modifier-synergies-theme17
    /// (Thermal Shock is Resonance-amplified, <see cref="PlayerOnHitEffects.ApplyElements"/> ->
    /// <see cref="RunSynergyEffects.ReportImpact"/>).
    ///
    /// When Thermal Shock triggers with one or more Resonance ranks active, the burst is reported
    /// through the same impact path Resonance amplifies. <c>TriggerThermalShock</c> first applies the
    /// instantaneous burst d*m, then routes the dealt amount through <c>ReportImpact</c>, which emits a
    /// single bounded impact amplified by (1 + 0.4 * r * s), where r is the Resonance rank and s is the
    /// number of active statuses on the target. So one triggering application with Resonance active
    /// deals:  burst (d*m)  +  amplified impact (d*m) * (1 + 0.4*r*s).
    ///
    /// The amplified impact is a single hit (LastSecondaryHits increments by exactly one), so it never
    /// exceeds the cascade's per-report bound. Runs in PlayMode: the statuses are MonoBehaviour
    /// components on a real Actor and both the burst and the reported impact go through
    /// <c>Actor.TakeDamage</c>.
    /// </summary>
    public sealed class ThermalShockResonanceAmplifiedTests
    {
        private ThermalShockTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ThermalShockTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 8: with Resonance rank r active a single
        // triggering application deals the burst plus one amplified impact of (d*m)*(1 + 0.4*r*s),
        // emitted as a single bounded impact.
        // Validates: Requirements 2.6
        [UnityTest]
        public IEnumerator Property8_ShockBurstIsResonanceAmplifiedAsSingleImpact()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 8);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.5f + (float)rng.NextDouble() * 3f;
                float damage = 5f + (float)rng.NextDouble() * 20f;
                int resonance = 1 + rng.Next(0, 3); // 1..3 (Resonance active)

                _rig.ConfigureThermalShock(multiplier);
                RunSynergyEffects synergies = _rig.AddResonance(resonance);
                _rig.EnableBurn(); // fire becomes active this call

                Actor enemy = _rig.BuildEnemy();
                _rig.GiveChill(enemy); // fire meets existing chill -> exactly one shock

                // After the burst applies burn, the enemy carries burn + chill => s = 2 statuses.
                const int statuses = 2;

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                float burst = damage * multiplier;
                float amplification = 1f + 0.4f * resonance * statuses;
                float expected = burst + burst * amplification; // burst + single amplified impact

                Assert.LessOrEqual(Mathf.Abs(dealt - expected), 1e-2f * Mathf.Max(1f, expected),
                    $"case {c}: r={resonance} s={statuses} dealt={dealt}, expected burst({burst}) + " +
                    $"amplified({burst * amplification}) = {expected}");

                // The amplified burst is emitted as a single bounded impact.
                Assert.AreEqual(1, synergies.LastSecondaryHits,
                    $"case {c}: expected exactly one bounded impact from the shock, " +
                    $"saw {synergies.LastSecondaryHits}");
                yield return null;
            }
        }

        // Feature: modifier-synergies-theme17, Property 8 (no amplification without Resonance): with
        // Resonance rank 0 the impact path is a no-op, so the shock is exactly the unamplified burst.
        // Validates: Requirements 2.6
        [UnityTest]
        public IEnumerator Property8_NoResonanceLeavesBurstUnamplified()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 88);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                float multiplier = 0.5f + (float)rng.NextDouble() * 3f;
                float damage = 5f + (float)rng.NextDouble() * 20f;

                _rig.ConfigureThermalShock(multiplier);
                _rig.EnableBurn(); // no Resonance granted this case

                Actor enemy = _rig.BuildEnemy();
                _rig.GiveChill(enemy);

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                float burst = damage * multiplier;
                Assert.LessOrEqual(Mathf.Abs(dealt - burst), 1e-2f * Mathf.Max(1f, burst),
                    $"case {c}: with no Resonance the shock should equal the raw burst {burst}, dealt={dealt}");
                yield return null;
            }
        }
    }
}
