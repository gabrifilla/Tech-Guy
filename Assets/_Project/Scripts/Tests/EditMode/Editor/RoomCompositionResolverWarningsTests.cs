// Feature: procedural-stage-room-generation
// Validates: Requirements 5.7, 5.8, 5.9
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>
    /// EditMode example tests for the three composition warning branches of
    /// <see cref="RoomCompositionResolver"/> (task 7.3):
    /// <list type="bullet">
    /// <item>R5.9 — no valid <c>EnemyRespawnPoint</c> ⇒ empty composition, one warning, nothing spawned.</item>
    /// <item>R5.7 — enemies with no reachable NavMesh point after 20 attempts ⇒ discarded, one per-room summary warning.</item>
    /// <item>R5.8 — fewer enemies placed than the Density_Budget minimum (4) ⇒ short-count warning.</item>
    /// </list>
    /// These run without any baked NavMesh, so <c>NavMesh.SamplePosition</c> fails and every enemy is
    /// discarded before spawn — which is exactly what exercises R5.7 and R5.8 together. Assertions read
    /// the observable <see cref="RoomActivationResult"/> fields (counts + Warnings) rather than depending
    /// on log capture; incidental warnings from the resolver / spawn point are silenced via
    /// <see cref="LogAssert.ignoreFailingMessages"/> so the tests stay focused on the result.
    /// </summary>
    public sealed class RoomCompositionResolverWarningsTests
    {
        private readonly List<GameObject> _created = new List<GameObject>();
        private RoomCompositionResolver _resolver;

        [SetUp]
        public void SetUp()
        {
            // Debug.LogWarning/LogError raised by the resolver (and by EnemyRespawnPoint when its
            // prefab is null) are expected here; do not let them fail the test — assert on the result.
            LogAssert.ignoreFailingMessages = true;
            _resolver = new RoomCompositionResolver();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            _resolver = null;
            LogAssert.ignoreFailingMessages = false;
        }

        // --- R5.9: no valid spawn point -------------------------------------------------------

        [Test]
        public void Activate_NullSpawnPoints_LeavesCompositionEmptyAndWarns()
        {
            Room room = BuildCombatRoom(id: 7, enemyCount: 5);

            RoomActivationResult result = _resolver.Activate(
                room,
                spawnPoints: null,
                archetypeCatalog: null);

            Assert.AreEqual(0, result.PlacedCount, "No enemy should be placed without a valid spawn point.");
            Assert.AreEqual(0, result.SpawnedActors.Count, "No actor should be spawned without a valid spawn point.");
            Assert.AreEqual(5, result.RequestedCount, "The requested count should still reflect the composition.");
            Assert.IsFalse(result.BossMarked, "A Combat_Room must never mark a boss.");
            Assert.GreaterOrEqual(result.Warnings.Count, 1, "R5.9 must record at least one warning.");
            Assert.IsTrue(WarningReferencesRoom(result, room.Id),
                "The R5.9 warning should name the affected room.");
        }

        [Test]
        public void Activate_AllNullSpawnPoints_LeavesCompositionEmptyAndWarns()
        {
            Room room = BuildCombatRoom(id: 11, enemyCount: 6);
            var spawnPoints = new List<EnemyRespawnPoint> { null, null, null };

            RoomActivationResult result = _resolver.Activate(room, spawnPoints, archetypeCatalog: null);

            Assert.AreEqual(0, result.PlacedCount, "An all-null spawn point list is treated as no valid spawn point.");
            Assert.AreEqual(0, result.SpawnedActors.Count, "Nothing should spawn when every spawn point is null.");
            Assert.IsFalse(result.BossMarked, "A Combat_Room must never mark a boss.");
            Assert.GreaterOrEqual(result.Warnings.Count, 1, "R5.9 must record at least one warning.");
            Assert.IsTrue(WarningReferencesRoom(result, room.Id),
                "The R5.9 warning should name the affected room.");
        }

        // --- R5.7: NavMesh discards + R5.8: short count ---------------------------------------

        [Test]
        public void Activate_NoNavMesh_DiscardsEnemiesAndRaisesDiscardAndShortCountWarnings()
        {
            Room room = BuildCombatRoom(id: 3, enemyCount: 8);
            var spawnPoints = new List<EnemyRespawnPoint> { CreateSpawnPoint() };

            RoomActivationResult result = _resolver.Activate(room, spawnPoints, archetypeCatalog: null);

            // With no baked NavMesh, every sample fails, so nothing is placed and all are discarded.
            Assert.AreEqual(8, result.RequestedCount, "Requested count should match the composition total.");
            Assert.AreEqual(0, result.PlacedCount, "No enemy can be placed without a NavMesh.");
            Assert.AreEqual(8, result.DiscardedCount, "Every requested enemy should be discarded (requested - placed).");
            Assert.Greater(result.DiscardedCount, 0, "R5.7 requires at least one discard to be observable.");
            Assert.IsFalse(result.BossMarked, "A Combat_Room must never mark a boss.");

            // R5.7: one per-room discard summary warning; R5.8: one short-count warning (placed < 4).
            Assert.GreaterOrEqual(result.Warnings.Count, 2,
                "Both the R5.7 discard and R5.8 short-count warnings should be present.");

            Assert.IsTrue(AnyWarningContains(result, "descartado"),
                "R5.7 should warn about discarded enemies.");
            Assert.IsTrue(AnyWarningContains(result, "R5.7"),
                "R5.7 warning should be tagged with the requirement id.");
            Assert.IsTrue(AnyWarningContains(result, "R5.8"),
                "R5.8 short-count warning should be tagged with the requirement id.");
            Assert.IsTrue(AnyWarningContains(result, "4"),
                "R5.8 warning should mention the Density_Budget minimum (4).");
            Assert.IsTrue(WarningReferencesRoom(result, room.Id),
                "Both warnings should name the affected room.");
        }

        [Test]
        public void Activate_NullPrefabSpawnPoint_DiscardsEnemiesAndWarns()
        {
            // A real EnemyRespawnPoint whose prefab is null returns null from SpawnEnemy(); even if a
            // NavMesh point were found the spawn would fail, so enemies are discarded either way.
            Room room = BuildCombatRoom(id: 21, enemyCount: 5);
            var spawnPoints = new List<EnemyRespawnPoint> { CreateSpawnPoint() };

            RoomActivationResult result = _resolver.Activate(room, spawnPoints, archetypeCatalog: null);

            Assert.AreEqual(0, result.PlacedCount, "A null-prefab spawn point cannot place any enemy.");
            Assert.Greater(result.DiscardedCount, 0, "Enemies should be discarded when nothing can be placed.");
            Assert.IsFalse(result.BossMarked, "A Combat_Room must never mark a boss.");
            Assert.IsTrue(AnyWarningContains(result, "R5.8"),
                "Placing fewer than 4 enemies must raise the R5.8 short-count warning.");
            Assert.IsTrue(WarningReferencesRoom(result, room.Id),
                "The warnings should name the affected room.");
        }

        // --- helpers --------------------------------------------------------------------------

        /// <summary>
        /// Builds a Combat_Room with a single-slot composition requesting <paramref name="enemyCount"/>
        /// enemies of the Grunt archetype, sized large enough that sampling is bounded (but still fails
        /// with no baked NavMesh).
        /// </summary>
        private static Room BuildCombatRoom(int id, int enemyCount)
        {
            var slots = new List<ArchetypeSlot> { new ArchetypeSlot(ArchetypeId.Grunt, enemyCount) };
            var composition = new RoomComposition(slots, enemyCount);
            return new Room(
                id,
                RoomType.Combat,
                center: new Vector2(10f, 10f),
                size: new Vector2(12f, 12f),
                depth: 1,
                connections: null,
                composition: composition);
        }

        private EnemyRespawnPoint CreateSpawnPoint()
        {
            var go = new GameObject("EnemyRespawnPointDouble");
            _created.Add(go);
            return go.AddComponent<EnemyRespawnPoint>();
        }

        private static bool AnyWarningContains(RoomActivationResult result, string fragment)
        {
            for (int i = 0; i < result.Warnings.Count; i++)
            {
                if (result.Warnings[i] != null && result.Warnings[i].Contains(fragment))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool WarningReferencesRoom(RoomActivationResult result, int roomId)
        {
            return AnyWarningContains(result, $"#{roomId}");
        }
    }
}
