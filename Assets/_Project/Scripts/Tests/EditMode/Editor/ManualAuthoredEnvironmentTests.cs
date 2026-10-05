using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class ManualAuthoredEnvironmentTests
    {
        [Test] public void SavedDefinitionAndPrefabReloadWithBakedNavigationAndCompleteBindings()
        {
            AssetDatabase.ImportAsset(RunStructureAuthoring.DefinitionPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.ImportAsset(RunStructureAuthoring.PrefabPath, ImportAssetOptions.ForceUpdate);
            var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(RunStructureAuthoring.DefinitionPath);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var route, out string error), error);
            var root = PrefabUtility.LoadPrefabContents(RunStructureAuthoring.PrefabPath);
            try
            {
                var environment = root.GetComponent<ManualStageEnvironment>();
                Assert.IsTrue(environment.Validate(route, out error), error);
                Assert.IsFalse(environment.GeometryRoot.activeSelf);
                Assert.IsTrue(EditorUtility.IsPersistent(environment.Navigation.navMeshData));
                Assert.AreEqual(RunStructureAuthoring.NavigationPath, AssetDatabase.GetAssetPath(environment.Navigation.navMeshData));
                Assert.AreEqual(11, environment.Nodes.Count); Assert.AreEqual(12, environment.Connections.Count);
                Assert.IsTrue(environment.Connections.All(c => c.Barrier.GetComponent<EncounterGates>() && c.Barrier.GetComponent<UnityEngine.AI.NavMeshObstacle>().carving));
                Assert.IsFalse(root.GetComponentsInChildren<Component>(true).Any(c => c == null));
                var graph = ManualRoomProjection.Create(route, environment);
                Assert.AreEqual(RoomType.Treasure, graph.Rooms.Single(r => r.Id == 3).Type);
                Assert.AreEqual(RoomType.Combat, graph.Rooms.Single(r => r.Id == 7).Type);
                Assert.AreEqual(Direction.West, graph.Connections.Single(e => e.RoomAId == 2 && e.RoomBId == 3).SideFromA);
                Assert.AreEqual(new Vector2(500,160), graph.Rooms.Single(r => r.Id == 5).Center);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        [Test] public void AuthoredEliteAndBossUseExistingContentAndExplicitMechanicalRanks()
        {
            var definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(RunStructureAuthoring.DefinitionPath);
            var elite = definition.Nodes.Single(n => n.Kind == ManualNodeKind.Elite);
            Assert.AreEqual(5, elite.Spawns.Count);
            Assert.AreEqual(1, elite.Spawns.Count(s => s.Rank == EnemyRank.Elite));
            Assert.AreEqual(4, elite.Spawns.Count(s => s.Rank == EnemyRank.Normal));
            Assert.IsTrue(elite.Spawns.All(s => EditorUtility.IsPersistent(s.Prefab)));
            var boss = definition.Nodes.Single(n => n.Kind == ManualNodeKind.Boss);
            Assert.AreEqual(EnemyRank.Boss, boss.Spawns.Single().Rank);
        }
    }
}
