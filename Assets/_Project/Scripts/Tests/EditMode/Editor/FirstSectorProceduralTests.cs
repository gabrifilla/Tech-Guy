using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TechGuy.Tests
{
    public sealed class FirstSectorProceduralTests
    {
        private const string ScenePath = "Assets/_Project/Scenes/FirstSector.unity";
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;

        [Test]
        public void AuthoredFirstSector_HasFiveCombatRoomsGuardianAndCompletePrefabCatalog()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                Assert.That(roots.SelectMany(r => r.GetComponentsInChildren<FirstSectorDirector>()).Count(), Is.Zero);
                var director = roots.SelectMany(r => r.GetComponentsInChildren<ProgressionDirector>()).Single();
                var data = new SerializedObject(director);
                Assert.That(data.FindProperty("_stageCount").intValue, Is.EqualTo(1));
                Assert.That(data.FindProperty("_requiredBossAccessFragments").intValue, Is.EqualTo(3));
                Assert.That(data.FindProperty("_environment").objectReferenceValue, Is.Not.Null);
                Assert.That(data.FindProperty("_player").objectReferenceValue, Is.Not.Null);
                Assert.That(data.FindProperty("_objective").objectReferenceValue, Is.Not.Null);
                var prefabs = (EnemyVariant[])typeof(ProgressionDirector).GetField("_archetypePrefabs", PrivateInstance).GetValue(director);
                Assert.That(prefabs.Length, Is.EqualTo(16));
                Assert.That(prefabs.Select(p => p.ArchetypeId).Distinct().Count(), Is.EqualTo(16));
                var spawner = prefabs.Single(p => p.ArchetypeId == ArchetypeId.Spawner).GetComponent<SpawnerBehavior>();
                Assert.That(new SerializedObject(spawner).FindProperty("_swarmPrefab").objectReferenceValue, Is.Not.Null);
                var parameters = (StageGenerationParams)typeof(ProgressionDirector).GetField("_generationParams", PrivateInstance).GetValue(director);
                for (ulong seed = 1; seed <= 100; seed++)
                {
                    var result = new StageGenerator().Generate(parameters, seed);
                    Assert.That(result.Success, Is.True, $"seed {seed}");
                    Assert.That(result.Graph.Rooms.Count(r => r.Type == RoomType.Combat), Is.EqualTo(5));
                    Assert.That(result.Graph.Rooms.Count(r => r.Type == RoomType.Start), Is.EqualTo(1));
                    Assert.That(result.Graph.Rooms.Count(r => r.Type == RoomType.Boss), Is.EqualTo(1));
                    foreach (var room in result.Graph.Rooms.Where(r => r.Type == RoomType.Combat))
                    {
                        Assert.That(room.Size, Is.EqualTo(new Vector2(24, 24)));
                        Assert.That(room.Composition.TargetDensity, Is.InRange(8, 16));
                        Assert.That(room.Composition.DistinctArchetypes, Is.GreaterThanOrEqualTo(4));
                    }
                    Assert.That(result.Graph.Rooms.Where(r => r.Type == RoomType.Combat)
                        .Select(ProceduralStageEnvironment.ResolveArenaShape).Distinct().Count(), Is.EqualTo(5),
                        "The first sector's five combat rooms must use five different arena profiles.");
                    Assert.That(result.Graph.Rooms.Single(r => r.Type == RoomType.Boss).Composition.Slots[0].ArchetypeId,
                        Is.EqualTo(ArchetypeId.Heavy));
                }
            }
            finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
        }

        [Test]
        public void BossSeal_RequiresAllThreeAccessFragmentsBeforeOpening()
        {
            var host = new GameObject("Boss seal regression");
            try
            {
                var gates = host.AddComponent<EncounterGates>();
                gates.ConfigureDirectional(Vector3.zero, new Vector2(24, 24), 4f);
                gates.ConfigureBossSeal(Direction.North, 3);
                gates.OpenDoor(Direction.North);
                Assert.That(gates.IsDoorSealed(Direction.North), Is.True);
                gates.SetBossSealProgress(2);
                gates.OpenDoor(Direction.North);
                Assert.That(gates.IsDoorSealed(Direction.North), Is.True);
                Assert.That(gates.IsBossSealReady, Is.False);
                gates.SetBossSealProgress(3);
                gates.OpenDoor(Direction.North);
                Assert.That(gates.IsDoorSealed(Direction.North), Is.False);
                Assert.That(gates.IsBossSealReady, Is.True);
                Assert.That(host.transform.Cast<Transform>().Count(t => t.name.StartsWith("Fragmento de acesso")),
                    Is.EqualTo(3));
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void ThirdCombatReward_ChargesSealAndUnlocksReachedBossDoor()
        {
            var host = new GameObject("Boss access progression regression");
            host.SetActive(false);
            try
            {
                var director = host.AddComponent<ProgressionDirector>();
                var approach = new Room(1, RoomType.Combat, Vector2.zero, new Vector2(24, 24), 1)
                    { Visited = true, Cleared = true };
                var boss = new Room(2, RoomType.Boss, new Vector2(24, 0), new Vector2(24, 24), 2);
                var edge = new RoomConnection(1, 2, Direction.East);
                var graph = new RoomGraph(new List<Room> { approach, boss },
                    new List<RoomConnection> { edge }, 1, 2);
                typeof(ProgressionDirector).GetField("_graph", PrivateInstance).SetValue(director, graph);
                typeof(ProgressionDirector).GetField("_rewardRoomId", PrivateInstance).SetValue(director, 1);
                typeof(ProgressionDirector).GetField("_bossAccessFragments", PrivateInstance).SetValue(director, 2);
                Invoke(director, "IndexRooms", graph);
                Invoke(director, "MaterializeGates", graph);
                var gates = host.GetComponentInChildren<EncounterGates>(true);
                Assert.That(gates.IsDoorSealed(Direction.East), Is.True);

                Invoke(director, "OnRewardChosen");

                Assert.That(director.BossAccessFragments, Is.EqualTo(3));
                Assert.That(director.HasBossAccess, Is.True);
                Assert.That(gates.IsBossSealReady, Is.True);
                Assert.That(gates.IsDoorSealed(Direction.East), Is.False);
                Assert.That(edge.Open, Is.True);
            }
            finally { Object.DestroyImmediate(host); }
        }

        [Test]
        public void EnteringFromRoomB_SealsSharedDoorAndClearRestoresBacktracking()
        {
            var host = new GameObject("Director regression");
            host.SetActive(false);
            try
            {
                var director = host.AddComponent<ProgressionDirector>();
                var edge = new RoomConnection(1, 2, Direction.East);
                var a = new Room(1, RoomType.Combat, Vector2.zero, new Vector2(24,24), 1) { Visited = true, Cleared = true };
                var b = new Room(2, RoomType.Combat, new Vector2(24,0), new Vector2(24,24), 2) { Visited = true };
                var graph = new RoomGraph(new List<Room>{a,b}, new List<RoomConnection>{edge}, 1, 2);
                typeof(ProgressionDirector).GetField("_graph", PrivateInstance).SetValue(director, graph);
                Invoke(director, "IndexRooms", graph);
                Invoke(director, "MaterializeGates", graph);
                var gates = host.GetComponentInChildren<EncounterGates>(true);
                gates.OpenDoor(Direction.East);
                Invoke(director, "SealRoomDoors", 2);
                Assert.That(gates.IsDoorSealed(Direction.East), Is.True);
                Assert.That(gates.transform.childCount, Is.EqualTo(1), "Room B must reuse the existing shared door.");
                b.Cleared = true;
                Invoke(director, "OpenEligibleExits", 2);
                Assert.That(gates.IsDoorSealed(Direction.East), Is.False, "The cleared room behind us must remain accessible.");
            }
            finally { Object.DestroyImmediate(host); }
        }

        [TestCase(Direction.East)]
        [TestCase(Direction.West)]
        public void EastWestDoor_SpansTheZAxis(Direction side)
        {
            var host = new GameObject("Door geometry regression");
            try
            {
                var gates = host.AddComponent<EncounterGates>();
                gates.ConfigureDirectional(Vector3.zero, new Vector2(24,24), 4f);
                gates.AddDoor(side);
                Physics.SyncTransforms();
                var bounds = host.GetComponentInChildren<BoxCollider>().bounds;
                Assert.That(bounds.size.z, Is.EqualTo(4f).Within(.01f));
                Assert.That(bounds.size.x, Is.EqualTo(.4f).Within(.01f));
            }
            finally { Object.DestroyImmediate(host); }
        }

        private static void Invoke(ProgressionDirector director, string name, object argument)
            => typeof(ProgressionDirector).GetMethod(name, PrivateInstance).Invoke(director, new[] { argument });

        private static void Invoke(ProgressionDirector director, string name)
            => typeof(ProgressionDirector).GetMethod(name, PrivateInstance).Invoke(director, null);
    }
}
