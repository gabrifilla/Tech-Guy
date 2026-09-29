using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.10 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 4). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class RoomCountBoundsPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 4: Limites de contagem de salas
        // Para todo RoomGraph gerado com sucesso: exatamente 1 Start_Room, exatamente 1 Boss_Room e
        // um número de Combat_Rooms em [1, 20]. Cobre params padrão e configs variadas (min/max de
        // combate incluindo o teto 20) via reflexão + Validate(), com seeds arbitrárias.
        // Validates: Requirements 1.1, 4.2
        [Test]
        public void SuccessfulGraphsHaveOneStartOneBossAndCombatWithinBounds()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(minCombat={parameters.MinCombatRooms}, " +
                             $"maxCombat={parameters.MaxCombatRooms})";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int start = 0;
                int boss = 0;
                int combat = 0;
                foreach (Room room in graph.Rooms)
                {
                    switch (room.Type)
                    {
                        case RoomType.Start: start++; break;
                        case RoomType.Boss: boss++; break;
                        case RoomType.Combat: combat++; break;
                    }
                }

                PropertyCheck.That(start == 1,
                    $"{ctx}: expected exactly 1 Start_Room, found {start}.");
                PropertyCheck.That(boss == 1,
                    $"{ctx}: expected exactly 1 Boss_Room, found {boss}.");

                // Combat count in [1, 20] (R1.1). The upper bound is the absolute spec ceiling
                // regardless of the configured max.
                PropertyCheck.That(combat >= 1 && combat <= StageGenerationParams.MaxCombatRoomCeiling,
                    $"{ctx}: Combat_Room count {combat} is outside [1, {StageGenerationParams.MaxCombatRoomCeiling}].");

                // The count must also honor the configured [min, max] band (after clamping).
                PropertyCheck.That(combat >= parameters.MinCombatRooms && combat <= parameters.MaxCombatRooms,
                    $"{ctx}: Combat_Room count {combat} is outside the configured band " +
                    $"[{parameters.MinCombatRooms}, {parameters.MaxCombatRooms}].");

                // The single Start/Boss ids must actually reference Start/Boss rooms.
                PropertyCheck.That(TypeOf(graph, graph.StartRoomId) == RoomType.Start,
                    $"{ctx}: StartRoomId {graph.StartRoomId} does not point at a Start_Room.");
                PropertyCheck.That(TypeOf(graph, graph.BossRoomId) == RoomType.Boss,
                    $"{ctx}: BossRoomId {graph.BossRoomId} does not point at a Boss_Room.");
            });
        }

        private static RoomType TypeOf(RoomGraph graph, int id)
        {
            foreach (Room room in graph.Rooms)
            {
                if (room.Id == id)
                {
                    return room.Type;
                }
            }

            // Sentinel: no room with that id. Neither Start nor Boss, so assertions above fail.
            return RoomType.Combat;
        }

        // ---- generators ---------------------------------------------------------------------

        private static StageGenerationParams RandomParams(Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0:
                    return new StageGenerationParams();
                case 1:
                    // Pinned at the ceiling: min == max == 20.
                    return BuildCombat(
                        StageGenerationParams.MaxCombatRoomCeiling, StageGenerationParams.MaxCombatRoomCeiling);
                case 2:
                    // Pinned at the floor: min == max == 1 (single Combat_Room).
                    return BuildCombat(1, 1);
                default:
                {
                    int min = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
                    int max = rng.Next(min, StageGenerationParams.MaxCombatRoomCeiling + 1);
                    return BuildCombat(min, max);
                }
            }
        }

        private static StageGenerationParams BuildCombat(int min, int max)
        {
            var parameters = new StageGenerationParams();
            SetPrivateInt(parameters, "_minCombatRooms", min);
            SetPrivateInt(parameters, "_maxCombatRooms", max);
            parameters.Validate();
            return parameters;
        }

        private static void SetPrivateInt(object target, string fieldName, int value)
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
