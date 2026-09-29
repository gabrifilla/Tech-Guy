using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.7 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 1). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class GenerationDeterminismPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 1: Determinismo da geração
        // Para todo Run_Seed de 64 bits e todo StageGenerationParams, gerar duas vezes com os
        // mesmos parâmetros e a mesma seed produz sempre dois RoomGraphs bem-sucedidos e
        // estruturalmente idênticos (mesmo nº de Rooms, mesmos Room IDs, mesmas Room_Connections,
        // mesmos Room_Types e mesma Room_Composition). Cobre params padrão e configs variadas via
        // reflexão + Validate(), com seeds arbitrárias de 64 bits e os limites (0 e ulong.MaxValue).
        // Validates: Requirements 1.3, 2.3
        [Test]
        public void SameSeedAndParamsProduceStructurallyIdenticalGraphs()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult first = generator.Generate(parameters, seed);
                StageGenerationResult second = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(minCombat={parameters.MinCombatRooms}, " +
                             $"maxCombat={parameters.MaxCombatRooms}, treasureP={parameters.TreasureProbability}, " +
                             $"secretP={parameters.SecretProbability})";

                // With the default retry limit and grid growth the core always produces a
                // connected Start -> Boss graph, so both attempts must succeed (R1.3/R2.3).
                PropertyCheck.That(first.Success,
                    $"{ctx}: first generation failed unexpectedly ({first.FailureReason}).");
                PropertyCheck.That(second.Success,
                    $"{ctx}: second generation failed unexpectedly ({second.FailureReason}).");

                PropertyCheck.That(first.Graph != null && second.Graph != null,
                    $"{ctx}: a successful result carried a null graph.");

                // Determinism: same seed + same params => structurally identical graphs.
                PropertyCheck.That(first.Graph.StructurallyEquals(second.Graph),
                    $"{ctx}: two generations with the same seed/params were not structurally equal " +
                    $"(rooms1={first.Graph.Rooms.Count}, rooms2={second.Graph.Rooms.Count}, " +
                    $"conns1={first.Graph.Connections.Count}, conns2={second.Graph.Connections.Count}).");

                // Structural equality must be symmetric.
                PropertyCheck.That(second.Graph.StructurallyEquals(first.Graph),
                    $"{ctx}: StructurallyEquals was not symmetric for the same seed/params.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space: the default, plus configs with
        /// varied combat-room bounds, density budgets, variety targets and special probabilities.
        /// Private serialized fields are written via reflection then clamped by Validate(), exactly
        /// as the Editor would.
        /// </summary>
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
