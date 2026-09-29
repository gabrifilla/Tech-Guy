using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.12 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 6). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class AtMostOneTreasureAndSecretPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 6: No máximo uma Treasure e uma Secret
        // Para todo RoomGraph gerado, o número de Treasure_Rooms é <= 1 e o número de Secret_Rooms é
        // <= 1. Cobre params padrão e configs variadas; usa probabilidades altas (0.95) para forçar a
        // aparição frequente das especiais e realmente exercitar o teto, com seeds arbitrárias.
        // Validates: Requirements 4.5
        [Test]
        public void AtMostOneTreasureAndAtMostOneSecretPerGraph()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(treasureP={parameters.TreasureProbability}, " +
                             $"secretP={parameters.SecretProbability})";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int treasure = 0;
                int secret = 0;
                foreach (Room room in graph.Rooms)
                {
                    if (room.Type == RoomType.Treasure) treasure++;
                    else if (room.Type == RoomType.Secret) secret++;
                }

                PropertyCheck.That(treasure <= 1,
                    $"{ctx}: expected at most 1 Treasure_Room, found {treasure}.");
                PropertyCheck.That(secret <= 1,
                    $"{ctx}: expected at most 1 Secret_Room, found {secret}.");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        private static StageGenerationParams RandomParams(Random rng)
        {
            var parameters = new StageGenerationParams();
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);

            // Half the cases pin probabilities at the ceiling to force specials to appear (exercising
            // the "at most one" cap); the rest use arbitrary probabilities within [0.05, 0.95].
            float treasureP;
            float secretP;
            if (rng.Next(0, 2) == 0)
            {
                treasureP = StageGenerationParams.SpecialProbabilityCeiling;
                secretP = StageGenerationParams.SpecialProbabilityCeiling;
            }
            else
            {
                treasureP = RandomProbability(rng);
                secretP = RandomProbability(rng);
            }

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
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
