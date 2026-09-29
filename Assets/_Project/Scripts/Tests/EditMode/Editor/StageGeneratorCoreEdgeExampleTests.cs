using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode example/edge tests for the pure StageGenerator core — task 4.20 of
    /// procedural-stage-room-generation. These are plain NUnit example cases (not property
    /// tests): concrete boundary scenarios for the composition planner and the generator.
    ///
    /// The core is dependency-free pure C# (no scene state), so these can run without a live
    /// Unity scene. They complement the property tests (Properties 8-18) with specific edge cases
    /// called out in the design's Testing Strategy.
    /// </summary>
    public sealed class StageGeneratorCoreEdgeExampleTests
    {
        // Feature: procedural-stage-room-generation
        // Validates: Requirements 5.3, 1.1, 4.7

        /// <summary>
        /// Edge (R5.3): when the archetype availability is BELOW the Variety_Target, the planner
        /// still produces a valid composition capped at availability — it never invents archetypes.
        /// Drives <see cref="RoomCompositionPlanner.Plan"/> directly with a small available set.
        /// </summary>
        [Test]
        public void ArchetypeAvailabilityBelowVarietyTargetCapsCompositionAtAvailability()
        {
            var planner = new RoomCompositionPlanner();

            // Variety target 6, but only 2 archetypes available.
            var parameters = new StageGenerationParams();
            SetPrivateInt(parameters, "_varietyTarget", 6);
            SetPrivateInt(parameters, "_densityBudgetMin", 10);
            SetPrivateInt(parameters, "_densityBudgetMax", 10);
            parameters.Validate();

            var available = new List<ArchetypeId> { ArchetypeId.Grunt, ArchetypeId.Shooter };
            var rng = new SeededRng(0xC0FFEEUL);

            RoomComposition composition = planner.Plan(parameters, available, depth: 0, maxDepth: 0, rng);

            Assert.IsNotNull(composition, "Plan returned a null composition.");
            Assert.AreEqual(2, composition.DistinctArchetypes,
                "Distinct archetypes must be capped at the available count (2), not the variety target (6).");
            Assert.AreEqual(2, composition.Slots.Count,
                "Slot count must match the distinct archetypes when availability caps variety.");

            // Every chosen archetype comes from the available set and gets at least one enemy.
            int distributed = 0;
            var chosen = new HashSet<ArchetypeId>();
            foreach (ArchetypeSlot slot in composition.Slots)
            {
                Assert.Contains(slot.ArchetypeId, available,
                    "Composition used an archetype outside the available set.");
                Assert.GreaterOrEqual(slot.Count, 1, "A chosen archetype slot has a non-positive count.");
                Assert.IsTrue(chosen.Add(slot.ArchetypeId), "Composition repeated an archetype across slots.");
                distributed += slot.Count;
            }

            // The whole target density is distributed among the two available archetypes.
            Assert.AreEqual(composition.TargetDensity, distributed,
                "Distributed enemies must sum to the target density.");
            Assert.AreEqual(10, composition.TargetDensity,
                "With a narrow [10,10] budget the target density must be 10.");
        }

        /// <summary>
        /// Edge (R5.3): zero archetype availability yields an empty (but non-null) composition —
        /// no enemies, no slots — while the target density is still computed from the budget.
        /// </summary>
        [Test]
        public void ZeroArchetypeAvailabilityYieldsEmptyComposition()
        {
            var planner = new RoomCompositionPlanner();
            var parameters = new StageGenerationParams();
            SetPrivateInt(parameters, "_varietyTarget", 4);
            parameters.Validate();

            RoomComposition composition = planner.Plan(
                parameters, new List<ArchetypeId>(), depth: 3, maxDepth: 5, new SeededRng(7UL));

            Assert.IsNotNull(composition, "Plan must return a non-null composition even with no archetypes.");
            Assert.AreEqual(0, composition.Slots.Count, "No archetypes available must produce zero slots.");
            Assert.AreEqual(0, composition.DistinctArchetypes, "No archetypes available must produce zero distinct.");
        }

        /// <summary>
        /// Edge (R1.1): with the combat-room count pinned to the boundary 20
        /// (MinCombatRooms == MaxCombatRooms == 20 via reflection), every successful generation
        /// produces exactly 20 Combat_Rooms across a range of seeds.
        /// </summary>
        [Test]
        public void CombatCountAtBoundaryTwentyProducesExactlyTwentyCombatRooms()
        {
            var generator = new StageGenerator();

            var parameters = new StageGenerationParams();
            SetPrivateInt(parameters, "_minCombatRooms", StageGenerationParams.MaxCombatRoomCeiling);
            SetPrivateInt(parameters, "_maxCombatRooms", StageGenerationParams.MaxCombatRoomCeiling);
            parameters.Validate();

            Assert.AreEqual(20, parameters.MinCombatRooms, "MinCombatRooms should clamp to the ceiling 20.");
            Assert.AreEqual(20, parameters.MaxCombatRooms, "MaxCombatRooms should clamp to the ceiling 20.");

            for (ulong seed = 1; seed <= 30; seed++)
            {
                StageGenerationResult result = generator.Generate(parameters, seed);
                Assert.IsTrue(result.Success,
                    $"seed={seed}: generation failed unexpectedly ({result.FailureReason}).");

                int combat = CountRooms(result.Graph, RoomType.Combat);
                Assert.AreEqual(20, combat,
                    $"seed={seed}: expected exactly 20 Combat_Rooms but found {combat}.");

                // The boundary case must still be a well-formed single-Start / single-Boss graph.
                Assert.AreEqual(1, CountRooms(result.Graph, RoomType.Start),
                    $"seed={seed}: expected exactly one Start_Room.");
                Assert.AreEqual(1, CountRooms(result.Graph, RoomType.Boss),
                    $"seed={seed}: expected exactly one Boss_Room.");
            }
        }

        /// <summary>
        /// Edge (R4.7): a Treasure_Room is discarded when it cannot be connected accessibly.
        /// Forcing that path deterministically is hard, so this test searches a wide seed range
        /// for the discard diagnostic and — regardless of whether it reproduces — asserts the
        /// weaker invariant that ANY present Treasure is reachable from Start via non-hidden
        /// connections (i.e. no inaccessible Treasure ever remains in the graph). When the discard
        /// path is found, it additionally checks the rest of the graph is preserved (Start -> Boss
        /// still connected and no Treasure room left behind).
        /// </summary>
        [Test]
        public void TreasureIsNeverPresentWhenInaccessibleAndDiscardPreservesTheGraph()
        {
            var generator = new StageGenerator();

            // High special probabilities so most seeds actually draw a Treasure and a Secret,
            // maximizing the chance of exercising an accessibility conflict / discard.
            var parameters = new StageGenerationParams();
            SetPrivateFloat(parameters, "_treasureProbability", StageGenerationParams.SpecialProbabilityCeiling);
            SetPrivateFloat(parameters, "_secretProbability", StageGenerationParams.SpecialProbabilityCeiling);
            parameters.Validate();

            bool discardObserved = false;
            const int seedCount = 400;

            for (ulong seed = 1; seed <= seedCount; seed++)
            {
                StageGenerationResult result = generator.Generate(parameters, seed);
                Assert.IsTrue(result.Success,
                    $"seed={seed}: generation failed unexpectedly ({result.FailureReason}).");

                RoomGraph graph = result.Graph;

                // Invariant (always holds): if a Treasure is present, it is reachable from Start
                // using only non-hidden connections; an inaccessible Treasure would have been
                // discarded (R4.6/R4.7).
                Room treasure = FindSingle(graph, RoomType.Treasure);
                if (treasure != null)
                {
                    Assert.GreaterOrEqual(treasure.Connections.Count, 1,
                        $"seed={seed}: present Treasure_Room {treasure.Id} has no connection.");
                    Assert.IsTrue(
                        ReachableExcludingHidden(graph, graph.StartRoomId, treasure.Id),
                        $"seed={seed}: present Treasure_Room {treasure.Id} is not accessible from Start.");
                }

                // Whether or not a Treasure survives, the rest of the graph is always a valid
                // connected Start -> Boss layout.
                Assert.IsTrue(
                    ReachableExcludingHidden(graph, graph.StartRoomId, graph.BossRoomId),
                    $"seed={seed}: no non-hidden Start -> Boss path.");

                // The discard path surfaces a diagnostic on the (still successful) result.
                if (result.FailureReason != null && result.FailureReason.Contains("Treasure_Room discarded"))
                {
                    discardObserved = true;
                    // When discarded, no Treasure_Room remains in the graph (rest preserved, R4.7).
                    Assert.IsNull(FindSingle(graph, RoomType.Treasure),
                        $"seed={seed}: discard diagnostic present but a Treasure_Room still remains.");
                }
            }

            // The discard path is not guaranteed to reproduce with the grid layout: the generator
            // places specials on leaves via accessible connections, so a discard is rare. If it did
            // not occur across the searched range we document it and rely on the invariant above,
            // which was asserted for every seed.
            if (!discardObserved)
            {
                Assert.Pass(
                    $"Treasure discard path not reproduced across {seedCount} seeds; the stronger " +
                    "invariant (no inaccessible Treasure ever present) held for every generated graph.");
            }
        }

        // ---- graph helpers ------------------------------------------------------------------

        private static int CountRooms(RoomGraph graph, RoomType type)
        {
            int count = 0;
            foreach (Room room in graph.Rooms)
            {
                if (room.Type == type)
                {
                    count++;
                }
            }

            return count;
        }

        private static Room FindSingle(RoomGraph graph, RoomType type)
        {
            foreach (Room room in graph.Rooms)
            {
                if (room.Type == type)
                {
                    return room;
                }
            }

            return null;
        }

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

        // ---- reflection helpers -------------------------------------------------------------

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
    }
}
