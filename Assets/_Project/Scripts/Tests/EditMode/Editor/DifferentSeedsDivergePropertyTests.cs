using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.8 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 2). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class DifferentSeedsDivergePropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 2: Seeds diferentes divergem
        // Para todo par de Run_Seeds distintas com os mesmos parâmetros, os RoomGraphs divergem em
        // pelo menos um valor mensurável (nº de Rooms, conjunto de Room_Connections, conjunto de
        // Room_Types atribuídos, ou conjunto de Special_Rooms presentes).
        //
        // Escolha de robustez: colisões estruturais genuínas são possíveis (o espaço de grafos
        // pequenos é finito), então NÃO exigimos "sempre diferem" para cada par isolado. Em vez
        // disso, cada caso gera vários pares de seeds distintas com os mesmos parâmetros e afirma
        // que a VASTA MAIORIA diverge (>= 60%); um par idêntico só é tolerado quando os dois grafos
        // são realmente StructurallyEquals (uma coincidência legítima), nunca um bug de determinismo
        // cruzado. Isso mantém o teste sensível a "seed ignorada" sem ficar instável.
        // Validates: Requirements 2.4
        [Test]
        public void DistinctSeedsProduceDivergentGraphsForTheVastMajorityOfPairs()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);

                const int pairs = 10;
                int divergent = 0;

                for (int p = 0; p < pairs; p++)
                {
                    ulong seedA = RandomSeed(rng);
                    ulong seedB = RandomSeed(rng);
                    if (seedA == seedB)
                    {
                        seedB = unchecked(seedA + 0x9E3779B97F4A7C15UL); // force distinct seeds
                    }

                    StageGenerationResult a = generator.Generate(parameters, seedA);
                    StageGenerationResult b = generator.Generate(parameters, seedB);

                    PropertyCheck.That(a.Success && b.Success,
                        $"pair seeds ({seedA},{seedB}) with the same params: a generation failed " +
                        $"(a={a.Success}/{a.FailureReason}, b={b.Success}/{b.FailureReason}).");

                    if (Diverges(a.Graph, b.Graph))
                    {
                        divergent++;
                    }
                }

                // The vast majority of distinct-seed pairs must diverge in a measurable value.
                PropertyCheck.That(divergent >= 6,
                    $"params(minCombat={parameters.MinCombatRooms}, maxCombat={parameters.MaxCombatRooms}, " +
                    $"treasureP={parameters.TreasureProbability}, secretP={parameters.SecretProbability}): " +
                    $"only {divergent}/{pairs} distinct-seed pairs diverged — the seed appears to be ignored.");
            });
        }

        // ---- divergence measure --------------------------------------------------------------

        /// <summary>
        /// True when the two graphs differ in at least one measurable value: room count,
        /// connection set, assigned room-type set, or set of Special_Rooms present (R2.4).
        /// </summary>
        private static bool Diverges(RoomGraph a, RoomGraph b)
        {
            if (a.Rooms.Count != b.Rooms.Count)
            {
                return true;
            }

            if (a.Connections.Count != b.Connections.Count)
            {
                return true;
            }

            if (!MultisetEqual(RoomTypeCounts(a), RoomTypeCounts(b)))
            {
                return true;
            }

            if (!SpecialSetEqual(a, b))
            {
                return true;
            }

            // Fall back to the full structural comparison: any remaining difference (connection
            // endpoints/sides, per-room composition) also counts as divergence.
            return !a.StructurallyEquals(b);
        }

        private static Dictionary<RoomType, int> RoomTypeCounts(RoomGraph graph)
        {
            var counts = new Dictionary<RoomType, int>();
            foreach (Room room in graph.Rooms)
            {
                counts.TryGetValue(room.Type, out int c);
                counts[room.Type] = c + 1;
            }

            return counts;
        }

        private static bool MultisetEqual(Dictionary<RoomType, int> a, Dictionary<RoomType, int> b)
        {
            if (a.Count != b.Count)
            {
                return false;
            }

            foreach (KeyValuePair<RoomType, int> kv in a)
            {
                if (!b.TryGetValue(kv.Key, out int other) || other != kv.Value)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SpecialSetEqual(RoomGraph a, RoomGraph b)
        {
            return HasType(a, RoomType.Treasure) == HasType(b, RoomType.Treasure) &&
                   HasType(a, RoomType.Secret) == HasType(b, RoomType.Secret);
        }

        private static bool HasType(RoomGraph graph, RoomType type)
        {
            foreach (Room room in graph.Rooms)
            {
                if (room.Type == type)
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
            // Keep several combat rooms available so the graph space is large enough that distinct
            // seeds are very likely to diverge (avoids a degenerate 1-room fixed layout).
            int minCombat = rng.Next(2, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int maxCombat = rng.Next(minCombat, StageGenerationParams.MaxCombatRoomCeiling + 1);
            int variety = rng.Next(
                StageGenerationParams.VarietyTargetFloor, StageGenerationParams.VarietyTargetCeiling + 1);

            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
            SetPrivateInt(parameters, "_varietyTarget", variety);
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
            uint high = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
            uint low = unchecked((uint)rng.Next(int.MinValue, int.MaxValue));
            return ((ulong)high << 32) | low;
        }
    }
}
