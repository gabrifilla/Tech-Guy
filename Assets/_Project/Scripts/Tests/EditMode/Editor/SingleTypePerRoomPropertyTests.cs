using System;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.11 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 5). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class SingleTypePerRoomPropertyTests
    {
        private static readonly RoomType[] DefinedTypes = (RoomType[])Enum.GetValues(typeof(RoomType));

        // Feature: procedural-stage-room-generation, Property 5: Tipo único por sala
        // Para toda Room de um RoomGraph gerado, ela tem exatamente um Room_Type. Como o enum
        // RoomType é de valor único, "exatamente um tipo" significa que cada sala carrega um valor de
        // enum DEFINIDO (nenhuma sala fica sem tipo/inválida) e que as invariantes de contagem se
        // mantêm (exatamente 1 Start e exatamente 1 Boss). Cobre params padrão e configs variadas.
        // Validates: Requirements 4.1
        [Test]
        public void EveryRoomHasExactlyOneDefinedRoomType()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} params(minCombat={parameters.MinCombatRooms}, " +
                             $"maxCombat={parameters.MaxCombatRooms}, treasureP={parameters.TreasureProbability}, " +
                             $"secretP={parameters.SecretProbability})";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                int start = 0;
                int boss = 0;
                foreach (Room room in graph.Rooms)
                {
                    // Every room must carry a defined enum value (never untyped/invalid).
                    PropertyCheck.That(IsDefinedType(room.Type),
                        $"{ctx}: room {room.Id} has an undefined RoomType value ({(int)room.Type}).");

                    if (room.Type == RoomType.Start) start++;
                    else if (room.Type == RoomType.Boss) boss++;
                }

                // Single-valued type is reinforced by the count invariants: exactly one Start and one
                // Boss (a room cannot be both, since Type holds a single enum value).
                PropertyCheck.That(start == 1,
                    $"{ctx}: expected exactly 1 Start_Room type, found {start}.");
                PropertyCheck.That(boss == 1,
                    $"{ctx}: expected exactly 1 Boss_Room type, found {boss}.");
            });
        }

        private static bool IsDefinedType(RoomType type)
        {
            foreach (RoomType defined in DefinedTypes)
            {
                if (defined == type)
                {
                    return true;
                }
            }

            return false;
        }

        // ---- generators ---------------------------------------------------------------------

        private static StageGenerationParams RandomParams(Random rng)
        {
            if (rng.Next(0, 3) == 0)
            {
                return new StageGenerationParams();
            }

            var parameters = new StageGenerationParams();
            int minCombat = rng.Next(1, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);
            // Bias probabilities high so Treasure/Secret rooms actually appear and get type-checked too.
            float treasureP = StageGenerationParams.SpecialProbabilityCeiling;
            float secretP = StageGenerationParams.SpecialProbabilityCeiling;

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
            SetPrivateFloat(parameters, "_treasureProbability", treasureP);
            SetPrivateFloat(parameters, "_secretProbability", secretP);
            parameters.Validate();
            return parameters;
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
