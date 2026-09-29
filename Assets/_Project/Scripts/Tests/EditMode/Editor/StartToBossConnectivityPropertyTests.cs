using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the pure <see cref="StageGenerator"/> — task 4.9 of
    /// procedural-stage-room-generation.
    ///
    /// Generation is dependency-free pure C# (no scene state), so it can be property-checked
    /// without a live Unity scene (Property 3). FsCheck/CsCheck cannot be resolved on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives &gt;= 100
    /// deterministic generated cases and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class StartToBossConnectivityPropertyTests
    {
        // Feature: procedural-stage-room-generation, Property 3: Conectividade Start → Boss
        // Para todo RoomGraph gerado com sucesso, existe pelo menos um caminho navegável de
        // Room_Connections da Start_Room até a Boss_Room, comprovado por uma BFS independente sobre
        // o conjunto de Connections (endpoints tratados como não ordenados por serem bidirecionais).
        // Cobre params padrão e configs variadas via reflexão + Validate(), com seeds arbitrárias.
        // Validates: Requirements 1.2
        [Test]
        public void EverySuccessfulGraphHasAPathFromStartToBoss()
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

                bool reachable = BfsReaches(graph, graph.StartRoomId, graph.BossRoomId);
                PropertyCheck.That(reachable,
                    $"{ctx}: no navigable path of connections from Start ({graph.StartRoomId}) " +
                    $"to Boss ({graph.BossRoomId}) — rooms={graph.Rooms.Count}, conns={graph.Connections.Count}.");
            });
        }

        // ---- independent BFS over the connection set ----------------------------------------

        /// <summary>
        /// Breadth-first search from <paramref name="startId"/> over every <see cref="RoomConnection"/>
        /// (hidden connections included: the Start -> Boss path may not need them, but including them
        /// only makes reachability easier to satisfy), treating each connection as bidirectional.
        /// </summary>
        private static bool BfsReaches(RoomGraph graph, int startId, int targetId)
        {
            if (startId == targetId)
            {
                return true;
            }

            var adjacency = new Dictionary<int, List<int>>();
            foreach (Room room in graph.Rooms)
            {
                adjacency[room.Id] = new List<int>();
            }

            foreach (RoomConnection connection in graph.Connections)
            {
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
            SetPrivateInt(parameters, "_minCombatRooms", minCombat);
            SetPrivateInt(parameters, "_maxCombatRooms", maxCombat);
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
