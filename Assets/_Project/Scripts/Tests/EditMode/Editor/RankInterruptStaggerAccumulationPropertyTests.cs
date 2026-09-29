using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the rank-resisted interruption-stagger accumulation rule
    /// (Requisito 7.8 / Property 24) — task 8.6 of weapon-gameplay-swarm-rework.
    ///
    /// The behavior is otherwise driven by the scene-bound <see cref="CombatReactionController"/>
    /// (which owns one accumulator per enemy and feeds it interruption stagger values), but the pure
    /// R7.8 accounting lives in <see cref="RankInterruptStagger"/> so it can be property-checked
    /// without a live Unity scene: for a sequence of interruptions, below the rank threshold each
    /// <see cref="RankInterruptStagger.Accumulate"/> call preserves the enemy's state, and only once
    /// the running total reaches or exceeds the rank threshold does it fire a stagger/break, carrying
    /// the remainder into the next cycle. The per-rank threshold is strictly increasing by rarity.
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property
    /// (min 128) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class RankInterruptStaggerAccumulationPropertyTests
    {
        private static readonly EnemyRank[] Ranks =
        {
            EnemyRank.Normal,
            EnemyRank.Elite,
            EnemyRank.Legendary,
            EnemyRank.Boss
        };

        // Feature: weapon-gameplay-swarm-rework, Property 24: Acúmulo de stagger por resistência de raridade
        // Para toda sequência de interrupções sobre um inimigo com resistência de postura por raridade,
        // stagger ou quebra só é aplicado quando o acúmulo atinge ou excede o limiar da raridade; abaixo
        // do limiar o estado do inimigo é preservado. Cada Accumulate abaixo do limiar devolve Preserved
        // e apenas soma ao total; ao atingir/exceder o limiar devolve StaggerOrBreak e consome o limiar,
        // carregando o resto para o próximo ciclo. Valores não-positivos não contribuem e preservam o
        // estado. Cobre raridades e resistências arbitrárias com sequências de tamanho variável.
        // Validates: Requirements 7.8
        [Test]
        public void InterruptStaggerFiresOnlyAtOrAboveRankThresholdAndCarriesRemainder()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                EnemyRank rank = Ranks[rng.Next(Ranks.Length)];
                float resistance = RandomResistance(rng);
                var accumulator = new RankInterruptStagger(rank, resistance);

                float threshold = accumulator.Threshold;
                const float epsilon = 1e-4f;

                // The threshold is a real, positive amount for every rank/resistance (a 0-resistance
                // enemy still needs the minimum fraction of the rank base).
                PropertyCheck.That(threshold > 0f,
                    $"rank={rank} resistance={resistance}: threshold={threshold} is not strictly positive.");

                // Drive a sequence of interruptions and mirror the pure accounting the class performs.
                float expectedTotal = 0f;
                int sequence = rng.Next(1, 12);
                for (int step = 0; step < sequence; step++)
                {
                    float staggerValue = RandomStaggerValue(rng, threshold);

                    // Predict from the mirror model BEFORE the call.
                    float projectedTotal = expectedTotal + (staggerValue > 0f ? staggerValue : 0f);
                    bool expectedFire = projectedTotal + 1e-6f >= threshold;

                    RankInterruptStagger.Outcome outcome = accumulator.Accumulate(staggerValue);

                    string ctx =
                        $"rank={rank} resistance={resistance} threshold={threshold} step={step} " +
                        $"staggerValue={staggerValue} preTotal={expectedTotal} projected={projectedTotal} " +
                        $"outcome={outcome} accumulated={accumulator.Accumulated}";

                    if (expectedFire)
                    {
                        // R7.8: at or above the threshold, a stagger/break fires.
                        PropertyCheck.That(outcome == RankInterruptStagger.Outcome.StaggerOrBreak,
                            $"{ctx}: expected StaggerOrBreak once the running total reached the threshold.");
                        // All whole thresholds the running total covers are consumed this call, so the carried
                        // remainder always ends strictly below the threshold (never a full un-fired threshold).
                                                expectedTotal = projectedTotal;
                        while (expectedTotal + 1e-6f >= threshold) expectedTotal -= threshold;
                        if (expectedTotal < 0f) expectedTotal = 0f;
                    }
                    else
                    {
                        // R7.8: below the threshold the enemy's state is preserved (nothing fires) and
                        // only the running total grows.
                        PropertyCheck.That(outcome == RankInterruptStagger.Outcome.Preserved,
                            $"{ctx}: expected Preserved while the running total is below the threshold.");
                        expectedTotal = projectedTotal;
                    }

                    // The class's accumulated total tracks the same pure model exactly.
                    PropertyCheck.That(Mathf.Abs(accumulator.Accumulated - expectedTotal) <= epsilon,
                        $"{ctx}: accumulated {accumulator.Accumulated} diverged from expected {expectedTotal}.");
                    // The carried remainder is always non-negative and never itself a fresh full threshold.
                    PropertyCheck.That(accumulator.Accumulated >= -epsilon,
                        $"{ctx}: accumulated went negative.");
                    PropertyCheck.That(accumulator.Accumulated < threshold + epsilon,
                        $"{ctx}: a full threshold remained un-fired in the carried remainder.");
                }
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 24: Acúmulo de stagger por resistência de raridade
        // O limiar de acúmulo cresce estritamente com a raridade (Normal < Elite < Legendary < Boss)
        // para a mesma resistência de postura, de modo que raridades mais duras exigem mais stagger de
        // interrupção acumulado antes de sofrer stagger/quebra.
        // Validates: Requirements 7.8
        [Test]
        public void ThresholdStrictlyIncreasesWithRarityAtEqualResistance()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float resistance = RandomResistance(rng);

                float normal = RankInterruptStagger.ThresholdFor(EnemyRank.Normal, resistance);
                float elite = RankInterruptStagger.ThresholdFor(EnemyRank.Elite, resistance);
                float legendary = RankInterruptStagger.ThresholdFor(EnemyRank.Legendary, resistance);
                float boss = RankInterruptStagger.ThresholdFor(EnemyRank.Boss, resistance);

                string ctx =
                    $"resistance={resistance} normal={normal} elite={elite} " +
                    $"legendary={legendary} boss={boss}";

                PropertyCheck.That(normal < elite,
                    $"{ctx}: Normal threshold is not strictly below Elite.");
                PropertyCheck.That(elite < legendary,
                    $"{ctx}: Elite threshold is not strictly below Legendary.");
                PropertyCheck.That(legendary < boss,
                    $"{ctx}: Legendary threshold is not strictly below Boss.");

                // A stagger value that would break a Normal enemy leaves a tougher rarity's state
                // preserved at the same resistance (it takes strictly more accumulation to fire).
                var boss1 = new RankInterruptStagger(EnemyRank.Boss, resistance);
                RankInterruptStagger.Outcome outcome = boss1.Accumulate(normal);
                PropertyCheck.That(outcome == RankInterruptStagger.Outcome.Preserved,
                    $"{ctx}: a Normal-sized interruption ({normal}) staggered a Boss at the same resistance.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>Stance resistance spanning the [0, 1] endpoints and the interior.</summary>
        private static float RandomResistance(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                        // no resistance -> minimum threshold fraction
                case 1: return 1f;                        // full resistance -> full rank threshold
                case 2: return (float)rng.NextDouble();   // interior
                default: return Mathf.Clamp01((float)rng.NextDouble() * 1.2f - 0.1f); // near/at the clamp edges
            }
        }

        /// <summary>
        /// Interruption stagger contributions spanning non-positive (ignored) values, small partial
        /// amounts that only cross the threshold after several steps, near-threshold values, and large
        /// values that fire immediately and leave a carried remainder.
        /// </summary>
        private static float RandomStaggerValue(System.Random rng, float threshold)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return ((float)rng.NextDouble() - 0.7f) * threshold; // may be <= 0 -> ignored
                case 1: return (float)rng.NextDouble() * threshold * 0.4f;   // small partial
                case 2: return threshold;                                     // exactly the threshold
                case 3: return threshold * (0.9f + (float)rng.NextDouble() * 0.2f); // near the threshold
                default: return threshold * (1f + (float)rng.NextDouble() * 2f);    // over -> fires + remainder
            }
        }
    }
}
