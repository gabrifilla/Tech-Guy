using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.17 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 11). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class CombatRoomSpawnPointPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 11: Cada Combat_Room tem spawn point
        // Para todo RoomGraph gerado, toda Combat_Room tem uma Composition não-nula (a
        // representação, no modelo puro, de >= 1 spawn point / EnemyRespawnPoint — R1.6). Como o
        // gerador alimenta o planner com os 16 arquétipos disponíveis, a Composition tem >= 1 slot
        // e TargetDensity >= 1 (na prática >= o mínimo do orçamento, 4). Salas Start/Treasure/Secret
        // permanecem sem Composition (null). Cobre params padrão e configs variadas via reflexão +
        // Validate(), com seeds arbitrárias de 64 bits.
        // Validates: Requirements 1.6
        [Test]
        public void EveryCombatRoomHasANonNullCompositionWithAtLeastOneSlot()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} minCombat={parameters.MinCombatRooms} " +
                             $"maxCombat={parameters.MaxCombatRooms} budget=[{parameters.DensityBudgetMin}," +
                             $"{parameters.DensityBudgetMax}]";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int combatRooms = 0;
                foreach (Room room in graph.Rooms)
                {
                    if (room.Type == RoomType.Combat)
                    {
                        combatRooms++;

                        // The Combat_Room's "spawn point" is modeled by a non-null Composition (R1.6).
                        PropertyCheck.That(room.Composition != null,
                            $"{ctx}: Combat_Room {room.Id} has a null Composition (no spawn point).");

                        // Archetypes are always available, so the composition must carry >= 1 slot
                        // and a positive target density (>= 1 enemy to place).
                        PropertyCheck.That(room.Composition.Slots != null && room.Composition.Slots.Count >= 1,
                            $"{ctx}: Combat_Room {room.Id} composition has no archetype slot.");
                        PropertyCheck.That(room.Composition.TargetDensity >= 1,
                            $"{ctx}: Combat_Room {room.Id} composition has non-positive TargetDensity " +
                            $"({room.Composition.TargetDensity}).");

                        // With the full archetype catalog the density lands within the configured
                        // budget, which itself sits within the spec floor/ceiling [4, 30].
                        PropertyCheck.That(
                            room.Composition.TargetDensity >= StageGenerationParams.DensityBudgetFloor &&
                            room.Composition.TargetDensity <= StageGenerationParams.DensityBudgetCeiling,
                            $"{ctx}: Combat_Room {room.Id} TargetDensity {room.Composition.TargetDensity} " +
                            $"outside the spec range [4, 30].");
                    }
                    else if (room.Type == RoomType.Start ||
                             room.Type == RoomType.Treasure ||
                             room.Type == RoomType.Secret)
                    {
                        // Non-combat, non-boss rooms carry no composition.
                        PropertyCheck.That(room.Composition == null,
                            $"{ctx}: room {room.Id} of type {room.Type} unexpectedly has a Composition.");
                    }
                }

                // R1.1: at least one Combat_Room is always present.
                PropertyCheck.That(combatRooms >= 1,
                    $"{ctx}: graph has no Combat_Room.");
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
