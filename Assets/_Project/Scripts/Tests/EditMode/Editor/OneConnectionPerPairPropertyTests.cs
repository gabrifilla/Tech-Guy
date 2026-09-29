using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.16 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 10). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class OneConnectionPerPairPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 10: Um par de portas por conexão
        // Para todo par adjacente de Rooms existe exatamente UMA RoomConnection (sem conexões
        // duplicadas entre as mesmas duas salas; extremidades não ordenadas). Além disso, cada
        // RoomConnection referencia duas salas existentes e distintas. Cobre params padrão e
        // configs variadas via reflexão + Validate(), com seeds arbitrárias de 64 bits.
        // Validates: Requirements 1.5
        [Test]
        public void EachAdjacentPairHasExactlyOneConnectionBetweenExistingDistinctRooms()
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

                var roomIds = new HashSet<int>();
                foreach (Room room in graph.Rooms)
                {
                    PropertyCheck.That(roomIds.Add(room.Id),
                        $"{ctx}: duplicate Room Id {room.Id} in the graph.");
                }

                var seenPairs = new HashSet<long>();
                foreach (RoomConnection connection in graph.Connections)
                {
                    // Each connection references two EXISTING, DISTINCT rooms.
                    PropertyCheck.That(connection.RoomAId != connection.RoomBId,
                        $"{ctx}: connection is a self-loop on room {connection.RoomAId}.");
                    PropertyCheck.That(roomIds.Contains(connection.RoomAId),
                        $"{ctx}: connection references non-existent room {connection.RoomAId}.");
                    PropertyCheck.That(roomIds.Contains(connection.RoomBId),
                        $"{ctx}: connection references non-existent room {connection.RoomBId}.");

                    // Endpoints unordered: exactly one connection per adjacent pair.
                    long key = UnorderedPairKey(connection.RoomAId, connection.RoomBId);
                    PropertyCheck.That(seenPairs.Add(key),
                        $"{ctx}: duplicate connection between rooms " +
                        $"{connection.RoomAId} and {connection.RoomBId}.");
                }
            });
        }

        /// <summary>Order-independent 64-bit key for an unordered pair of non-negative room ids.</summary>
        private static long UnorderedPairKey(int a, int b)
        {
            int lo = Math.Min(a, b);
            int hi = Math.Max(a, b);
            return ((long)lo << 32) | (uint)hi;
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
