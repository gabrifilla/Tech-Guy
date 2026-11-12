using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    /// <summary>Room packs (default since 11/11/2026): rosters per pack, fodder outside the melee cap, Editor switch to compare.</summary>
    public sealed class RoomPacksPrototypePlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private bool _previous;

        [SetUp] public void Remember() => _previous = EditorPrefs.GetBool(ProgressionDirector.RoomPacksPrefKey, true);
        [TearDown] public void Restore() => EditorPrefs.SetBool(ProgressionDirector.RoomPacksPrefKey, _previous);

        private static IEnumerator LoadFirstSector(bool packs)
        {
            EditorPrefs.SetBool(ProgressionDirector.RoomPacksPrefKey, packs);
            SessionState.SetString(RunSeedSource.NextEditorSeedKey, "7");
            yield return SceneManager.LoadSceneAsync("FirstSector");
            for (int frame = 0; frame < 4; frame++) yield return null;
        }

        private static string Roster(ManualNodeDefinition node) =>
            string.Join(",", node.Spawns.Select(s => s.Prefab.GetComponent<EnemyVariant>().ArchetypeId).OrderBy(id => id));

        [UnityTest]
        public IEnumerator PacksReplaceNormalCombatRostersUnlessSwitchedOff()
        {
            yield return LoadFirstSector(false);
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            Assert.IsNotNull(director.ManualSession);
            Assert.IsTrue(director.ManualSession.Route.Nodes.All(n => director.RoomPackFor(n.Id) == null), "Switched off: the old uniform draw, no pack.");

            yield return LoadFirstSector(true);
            director = Object.FindFirstObjectByType<ProgressionDirector>();
            var combat = director.ManualSession.Route.Nodes.Where(n => n.Kind == ManualNodeKind.Combat).ToList();
            Assert.IsNotEmpty(combat);
            foreach (var node in combat)
            {
                string pack = director.RoomPackFor(node.Id);
                Assert.AreEqual(ProgressionDirector.PackName(node.Id), pack, "Room " + node.Id);
                Assert.AreEqual(pack == "Fornalha" ? "Heavy,HazardCaster,Swarm,Swarm,Swarm,Swarm,Swarm,Swarm,Swarm,Swarm,Swarm"
                    : pack == "Anzol" ? "Rush,Rush,Rush,Rush,Charger,Bomber,Hooker" : "Grunt,Grunt,Grunt,Shooter,Shooter,SpreadShooter",
                    Roster(node), "Roster of " + pack);
                Assert.IsTrue(node.Spawns.All(s => s.Rank == EnemyRank.Normal));
                if (node.SpatialLayout != null) Assert.IsTrue(node.Spawns.All(s => node.SpatialLayout.Clear(s.Offset, 1f)), "Safe slots in room " + node.Id);
            }
            Assert.IsTrue(director.ManualSession.Route.Nodes.Where(n => n.Kind != ManualNodeKind.Combat).All(n => director.RoomPackFor(n.Id) == null),
                "Elite, boss and other rooms are untouched.");
        }

        [Test]
        public void FodderAttacksOutsideTheMeleeCapOnlyWhenConfigured()
        {
            var room = new GameObject("Pack coordinator");
            try
            {
                var coordinator = room.AddComponent<SwarmAttackCoordinator>();
                coordinator.ConfigureCapacity(3);
                var swarm = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Swarm.prefab");
                var grunt = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab");
                var swarmAi = swarm.GetComponent<EnemyAI>(); var gruntAi = grunt.GetComponent<EnemyAI>();
                Assert.IsFalse(coordinator.IsUncapped(swarmAi), "Capped by default.");
                coordinator.ConfigureUncapped(new[] { ArchetypeId.Swarm, ArchetypeId.Rush });
                Assert.IsTrue(coordinator.IsUncapped(swarmAi));
                Assert.IsFalse(coordinator.IsUncapped(gruntAi), "Anchors and line still use tokens.");
            }
            finally { Object.DestroyImmediate(room); }
        }

        [Test]
        public void ChargerEngagesFromItsChargeBand()
        {
            Assert.AreEqual(EnemyAttackPatterns.ChargerEngagementRange,
                EnemyAttackPatterns.EngagementRange(EnemyAttackTraits.None, 0, 1.8f, ArchetypeId.Charger));
            Assert.Greater(EnemyAttackPatterns.ChargerEngagementRange, 5f, "The charge opens from a distance, not in contact.");
        }
    }
}
