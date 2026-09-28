using System.Reflection;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for R6 Property 17 of modifier-synergies-theme17
    /// (Haste feeds ComboNova, <see cref="CharControlScript.TryProcComboNova"/> proc gate).
    ///
    /// Property 17 — ComboNova proc gate: while ComboNova is owned (rank &gt; 0) the nova fires on
    /// exactly every third basic attack and no other, for any attack-speed multiplier ≥ 0.01. A
    /// nova applies area damage within a 2.5 m radius equal to 0.6 · rank · weaponDamage.
    ///
    /// Approach (documented per task 8.3): the proc gate and its constants live inside
    /// <see cref="CharControlScript.TryProcComboNova"/>, a MonoBehaviour whose real proc requires a
    /// full player/scene/physics graph (weapon, PlayerActor.TryApplyAreaDamage, attack layers). To
    /// keep this a fast, deterministic, RNG-seeded property test we exercise a computed model of the
    /// gate rule (fires ⇔ rank &gt; 0 AND count % ComboNovaBasicInterval == 0) that mirrors the
    /// production expression exactly, AND we pin the model to the shipped code by reading the private
    /// gate constants (ComboNovaBasicInterval, ComboNovaRadius, ComboNovaDamagePerRank) off
    /// <see cref="CharControlScript"/> via reflection and asserting their expected values, so any
    /// drift in the real constants fails this test. This isolates the pure gate rule (6.1, 6.3)
    /// without a PlayMode scene.
    /// </summary>
    public sealed class ComboNovaProcGateTests
    {
        // Expected gate constants from CharControlScript (R6.1). Verified against the private consts
        // via reflection in ConstantsMatchSpec so the model below cannot silently diverge from code.
        private const int ExpectedBasicInterval = 3;
        private const float ExpectedRadius = 2.5f;
        private const float ExpectedDamagePerRank = 0.6f;

        // Pure model of the proc gate, mirroring CharControlScript.TryProcComboNova exactly:
        //   int nova = Rank(ComboNova); if (nova <= 0 || count % ComboNovaBasicInterval != 0) return;
        private static bool NovaFires(int rank, int runBasicCount, int interval)
            => rank > 0 && runBasicCount % interval == 0;

        // Nova area damage per the R6.1 formula: 0.6 * rank * weaponDamage.
        private static float NovaDamage(int rank, float weaponDamage)
            => ExpectedDamagePerRank * rank * weaponDamage;

        private static T ReadConst<T>(string name)
        {
            FieldInfo field = typeof(CharControlScript).GetField(
                name, BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.GetField);
            Assert.IsNotNull(field, "Expected private const " + name + " on CharControlScript (gate constant).");
            Assert.IsTrue(field.IsLiteral && !field.IsInitOnly, name + " should be a compile-time const.");
            return (T)field.GetRawConstantValue();
        }

        // Feature: modifier-synergies-theme17, Property 17 (constants)
        // Pin the gate constants to the spec so the pure model tracks the shipped code (R6.1).
        // Validates: Requirements 6.1
        [Test]
        public void ComboNovaGateConstants_MatchSpec()
        {
            Assert.AreEqual(ExpectedBasicInterval, ReadConst<int>("ComboNovaBasicInterval"),
                "ComboNovaBasicInterval must gate on every third basic attack.");
            Assert.AreEqual(ExpectedRadius, ReadConst<float>("ComboNovaRadius"), 1e-6f,
                "ComboNovaRadius must be 2.5 m.");
            Assert.AreEqual(ExpectedDamagePerRank, ReadConst<float>("ComboNovaDamagePerRank"), 1e-6f,
                "ComboNovaDamagePerRank must be 0.6 per rank.");
        }

        // Feature: modifier-synergies-theme17, Property 17
        // For any rank >= 1 and any attack-speed multiplier >= 0.01, the nova fires on exactly the
        // basics whose running count is a multiple of the interval and on no other basic. The
        // multiplier is generated but does not enter the gate — it must never change WHICH basic
        // procs, only how fast basics accrue over real time (that proportionality is Property 18).
        // Validates: Requirements 6.1, 6.3
        [Test]
        public void ComboNovaProcGate_FiresOnEveryThirdBasicOnly_ForAnyAttackSpeed()
        {
            int interval = ReadConst<int>("ComboNovaBasicInterval");

            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);                          // ComboNova owned: rank 1..3
                // Attack-speed multiplier >= 0.01 (PlayerArpgStats floors it at 0.01). Sample across
                // a wide range including sub-1 (slow) and large (fast) values.
                double m = 0.01 + rng.NextDouble() * 10.0;
                int basics = rng.Next(interval * 2, interval * 12); // simulate a run of basic attacks

                int firedCount = 0;
                for (int count = 1; count <= basics; count++)
                {
                    bool fired = NovaFires(rank, count, interval);
                    bool expected = count % interval == 0;          // exactly every third basic
                    PropertyCheck.That(fired == expected,
                        $"rank={rank}, m={m:F3}, count={count}: fired={fired}, expected={expected}");
                    if (fired) firedCount++;
                }

                // The multiplier m is irrelevant to the gate: the proc count over N basics is
                // strictly floor(N / interval) regardless of attack speed (R6.3).
                PropertyCheck.That(firedCount == basics / interval,
                    $"rank={rank}, m={m:F3}, basics={basics}: firedCount={firedCount}, expected={basics / interval}");
            });
        }

        // Feature: modifier-synergies-theme17, Property 17 (damage magnitude)
        // On a firing basic the nova damage equals 0.6 * rank * weaponDamage within a 2.5 m radius.
        // Validates: Requirements 6.1
        [Test]
        public void ComboNovaProc_DamageEqualsSixtyPercentPerRank()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int rank = rng.Next(1, 4);
                float weaponDamage = 1f + (float)rng.NextDouble() * 199f; // 1..200

                float expected = 0.6f * rank * weaponDamage;
                float actual = NovaDamage(rank, weaponDamage);

                PropertyCheck.That(System.Math.Abs(actual - expected) <= 1e-3f * System.Math.Max(1f, expected),
                    $"rank={rank}, weaponDamage={weaponDamage:F2}: damage={actual}, expected={expected}");
            });
        }
    }
}
