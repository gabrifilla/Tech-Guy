using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.14 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 8). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class TreasureAccessibleOrDiscardedPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 8: Treasure acessível ou descartada
        // Para todo RoomGraph gerado com sucesso, SE uma Treasure_Room está presente ENTÃO ela tem
        // >= 1 conexão e é alcançável a partir da Start_Room usando apenas conexões NÃO ocultas
        // (BFS excluindo conexões Hidden). Se a Treasure não está presente (descartada ou não
        // sorteada), as demais salas ainda formam um grafo válido conexo Start -> Boss. Cobre
        // params padrão e configs variadas via reflexão + Validate(), com seeds arbitrárias de
        // 64 bits (incluindo 0 e ulong.MaxValue).
        // Validates: Requirements 4.6, 4.7
        [Test]
        public void PresentTreasureIsAccessibleFromStartViaNonHiddenConnections()
        {
            var generator = new StageGenerator();

            PropertyCheck.ForAll((rng, i) =>
            {
                StageGenerationParams parameters = RandomParams(rng);
                ulong seed = RandomSeed(rng);

                StageGenerationResult result = generator.Generate(parameters, seed);

                string ctx = $"seed={seed} treasureP={parameters.TreasureProbability} " +
                             $"secretP={parameters.SecretProbability}";

                PropertyCheck.That(result.Success,
                    $"{ctx}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;
                PropertyCheck.That(graph != null, $"{ctx}: successful result carried a null graph.");

                // The rest of the graph is always a valid connected Start -> Boss layout, whether
                // or not a Treasure is present (R4.7: discarding the Treasure preserves the rest).
                PropertyCheck.That(
                    ReachableExcludingHidden(graph, graph.StartRoomId, graph.BossRoomId),
                    $"{ctx}: no non-hidden path from Start ({graph.StartRoomId}) to Boss ({graph.BossRoomId}).");

                Room treasure = FindSingle(graph, RoomType.Treasure);
                if (treasure == null)
                {
                    // Absent or discarded Treasure is allowed; nothing more to assert here.
                    return;
                }

                // A present Treasure has >= 1 connection (R4.6).
                PropertyCheck.That(treasure.Connections != null && treasure.Connections.Count >= 1,
                    $"{ctx}: present Treasure_Room {treasure.Id} has no connection.");

                // ... and is reachable from Start using only non-hidden connections (R4.6):
                // an accessible Treasure must never sit behind a hidden Secret connection.
                PropertyCheck.That(
                    ReachableExcludingHidden(graph, graph.StartRoomId, treasure.Id),
                    $"{ctx}: present Treasure_Room {treasure.Id} is not reachable from Start via non-hidden connections.");
            });
        }

        // ---- graph helpers ------------------------------------------------------------------

        /// <summary>
        /// BFS from <paramref name="startId"/> to <paramref name="targetId"/> over the graph's
        /// connections, EXCLUDING any connection flagged <see cref="RoomConnection.Hidden"/>.
        /// Models "accessible from the Start_Room without a Discovery_Action" (R4.6).
        /// </summary>
        private static bool ReachableExcludingHidden(RoomGraph graph, int startId, int targetId)
        {
            if (startId == targetId)
            {
                return true;
            }

            var adjacency = new Dictionary<int, List<int>>(graph.Rooms.Count);
            foreach (Room room in graph.Rooms)
            {
                adjacency[room.Id] = new List<int>();
            }

            foreach (RoomConnection connection in graph.Connections)
            {
                if (connection.Hidden)
                {
                    continue;
                }

                if (adjacency.TryGetValue(connection.RoomAId, out List<int> a))
                {
                    a.Add(connection.RoomBId);
                }

                if (adjacency.TryGetValue(connection.RoomBId, out List<int> b))
                {
                    b.Add(connection.RoomAId);
                }
            }

            if (!adjacency.ContainsKey(startId) || !adjacency.ContainsKey(targetId))
            {
                return false;
            }

            var visited = new HashSet<int> { startId };
            var queue = new Queue<int>();
            queue.Enqueue(startId);
            while (queue.Count > 0)
            {
                int current = queue.Dequeue();
                foreach (int neighbor in adjacency[current])
                {
                    if (neighbor == targetId)
                    {
                        return true;
                    }

                    if (visited.Add(neighbor))
                    {
                        queue.Enqueue(neighbor);
                    }
                }
            }

            return false;
        }

        private static Room FindSingle(RoomGraph graph, RoomType type)
        {
            Room found = null;
            foreach (Room room in graph.Rooms)
            {
                if (room.Type == type)
                {
                    found = room;
                }
            }

            return found;
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Builds a params instance spanning the input space, biasing the special-room
        /// probabilities toward the high end so many cases actually draw a Treasure. Private
        /// serialized fields are written via reflection then clamped by Validate().
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
            // Bias treasure/secret probabilities high to exercise the Treasure paths often.
            float treasureP = HighProbability(rng);
            float secretP = HighProbability(rng);

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

        private static float HighProbability(Random rng)
        {
            // Skew toward the ceiling (0.95) so specials appear in most cases.
            float t = 0.5f + 0.5f * (float)rng.NextDouble();
            return StageGenerationParams.SpecialProbabilityFloor +
                   t * (StageGenerationParams.SpecialProbabilityCeiling - StageGenerationParams.SpecialProbabilityFloor);
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
