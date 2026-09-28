using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// PlayMode property test for R2 Property 4 of modifier-synergies-theme17
    /// (Ignite + Frost -> Thermal Shock, <see cref="PlayerOnHitEffects.ApplyElements"/>).
    ///
    /// An opposite element triggers Thermal Shock at most once per qualifying element application
    /// within a single <c>ApplyElements</c> call: fire onto an existing chill is one shock, frost
    /// onto an existing burn is one shock, and a single call that applies both onto both pre-existing
    /// opposites triggers up to twice (once per qualifying application) and never chains beyond that.
    /// Runs in PlayMode because BurnStatus/ChillStatus are MonoBehaviour components on a real Actor
    /// and Thermal Shock calls <c>Actor.TakeDamage</c>.
    ///
    /// Shock count is measured by dealt damage: with Resonance rank 0 the Resonance impact path is a
    /// no-op, so total damage over the call equals shockCount * damageDealt * multiplier exactly.
    /// </summary>
    public sealed class ThermalShockAtMostOncePerApplicationTests
    {
        private ThermalShockTestRig _rig;

        [SetUp]
        public void SetUp() => _rig = new ThermalShockTestRig();

        [TearDown]
        public void TearDown() => _rig?.TearDown();

        // Feature: modifier-synergies-theme17, Property 4: an opposite element triggers at most one
        // Thermal Shock per qualifying element application (up to twice per call only when both qualify).
        // Validates: Requirements 2.1, 2.2, 2.7
        [UnityTest]
        public IEnumerator Property4_AtMostOneShockPerQualifyingApplication()
        {
            const int cases = 100;
            var rng = new System.Random(PropertyCheck.DefaultSeed + 4);

            for (int c = 0; c < cases; c++)
            {
                _rig.TearDown();
                _rig = new ThermalShockTestRig();
                _rig.BuildEffects();

                // Randomize which opposite elements pre-exist and which get applied this call.
                bool preBurn = rng.Next(0, 2) == 0;
                bool preChill = rng.Next(0, 2) == 0;
                float multiplier = 0.5f + (float)rng.NextDouble() * 3f; // 0.5..3.5, > 0
                float damage = 5f + (float)rng.NextDouble() * 20f;      // 5..25, > 0

                _rig.ConfigureThermalShock(multiplier);
                _rig.EnableBurn();          // fire becomes active this call
                _rig.EnableChill(1f);       // frost becomes active this call (deterministic roll)

                Actor enemy = _rig.BuildEnemy();
                if (preBurn) _rig.GiveBurn(enemy);
                if (preChill) _rig.GiveChill(enemy);

                // Expected qualifying applications:
                //   fire meets existing chill  -> preChill
                //   frost meets existing burn  -> preBurn
                int expectedShocks = (preChill ? 1 : 0) + (preBurn ? 1 : 0);

                float before = enemy.health;
                _rig.Effects.ApplyElements(enemy, damage);
                float dealt = before - enemy.health;

                float perShock = damage * multiplier;
                float expectedDealt = expectedShocks * perShock;

                Assert.LessOrEqual(Mathf.Abs(dealt - expectedDealt), 1e-2f * Mathf.Max(1f, expectedDealt),
                    $"case {c}: preBurn={preBurn} preChill={preChill} dealt={dealt}, expected {expectedDealt} " +
                    $"({expectedShocks} shock(s) x {perShock})");

                // Never more than two shocks per call, and never more than one per qualifying application.
                Assert.LessOrEqual(expectedShocks, 2, $"case {c}: more than two qualifying applications");
                yield return null;
            }
        }
    }
}
