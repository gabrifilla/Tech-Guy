using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.18 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 15). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class BossUniqueAndEligiblePropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 15: Chefe único e elegível
        // Para todo RoomGraph gerado, existe exatamente uma Boss_Room; o Id da Boss_Room bate com
        // graph.BossRoomId; e a Boss_Room tem uma Composition não-nula com >= 1 slot de arquétipo
        // (o inimigo principal elegível a ser marcado como SectorBoss — R7.1/R7.2). Cobre params
        // padrão e configs variadas via reflexão + Validate(), com seeds arbitrárias de 64 bits.
        // Validates: Requirements 7.1, 7.2
        [Test]
        public void ExactlyOneBossRoomWithAnEligiblePrimaryEnemy()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} minCombat={parameters.MinCombatRooms} " +
                             $"maxCombat={parameters.MaxCombatRooms}";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int bossCount = 0;
                Room boss = null;
                foreach (Room room in graph.Rooms)
                {
                    if (room.Type == RoomType.Boss)
                    {
                        bossCount++;
                        boss = room;
                    }
                }

                // Exactly one Boss_Room.
                PropertyCheck.That(bossCount == 1,
                    $"{ctx}: expected exactly one Boss_Room but found {bossCount}.");

                // The graph's BossRoomId points at that single Boss_Room.
                PropertyCheck.That(boss.Id == graph.BossRoomId,
                    $"{ctx}: Boss_Room Id {boss.Id} does not match graph.BossRoomId {graph.BossRoomId}.");

                // Eligibility (R7.1/R7.2): the Boss_Room carries a composition with >= 1 archetype
                // slot — the primary enemy the SectorBoss will mark. Generation would have failed
                // (Success=false, no partial graph) had there been no eligible primary enemy.
                PropertyCheck.That(boss.Composition != null,
                    $"{ctx}: Boss_Room {boss.Id} has a null Composition (no eligible enemy).");
                PropertyCheck.That(boss.Composition.Slots != null && boss.Composition.Slots.Count >= 1,
                    $"{ctx}: Boss_Room {boss.Id} composition has no archetype slot to mark as SectorBoss.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        private static StageGenerationParams RandomParams(Random rng)
        {
            if (rng.Next(0, 4) == 0)
            {
                return new StageGenerationParams();
            }

            var parameters = new StageGenerationParams();
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int budgetMin = rng.Next(
                StageGenerationParams.DensityBudgetFloor, StageGenerationParams.DensityBudgetCeiling + 1);
            int budgetMax = rng.Next(budgetMin, StageGenerationParams.DensityBudgetCeiling + 1);
            int variety = rng.Next(
                StageGenerationParams.VarietyTargetFloor, StageGenerationParams.VarietyTargetCeiling + 1);
            float treasureP = RandomProbability(rng);
            float secretP = RandomProbability(rng);

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
            SetPrivateInt(parameters, "_densityBudgetMin", budgetMin);
            SetPrivateInt(parameters, "_densityBudgetMax", budgetMax);
            SetPrivateInt(parameters, "_varietyTarget", variety);
            SetPrivateFloat(parameters, "_treasureProbability", treasureP);
            SetPrivateFloat(parameters, "_secretProbability", secretP);
            parameters.Validate();
            return parameters;
        }

        private static float RandomProbability(Random rng)
        {
            return StageGenerationParams.SpecialProbabilityFloor +
                   (float)rng.NextDouble() *
                   (StageGenerationParams.SpecialProbabilityCeiling - StageGenerationParams.SpecialProbabilityFloor);
        }

        private static void SetPrivateInt(object target, string fieldName, int value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on StageGenerationParams.");
            field.SetValue(target, value);
        }

        private static void SetPrivateFloat(object target, string fieldName, float value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Expected private field '{fieldName}' on StageGenerationParams.");
            field.SetValue(target, value);
        }

        private static ulong RandomSeed(Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0:
                    return 0UL;
                case 1:
                    return ulong.MaxValue;
                default:
                    uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
                    return ((ulong)high << 32) | low;
            }
        }
    }
}
