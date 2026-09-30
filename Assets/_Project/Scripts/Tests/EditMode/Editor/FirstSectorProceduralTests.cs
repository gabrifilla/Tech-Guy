using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

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
        public void AuthoredFirstSector_PullsTopDownCameraBackFromTheDefaultFraming()
        {
            Scene scene = SceneManager.GetSceneByPath(ScenePath);
            bool opened = !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
            try
            {
                var roots = scene.GetRootGameObjects();
                var camera = roots
                    .SelectMany(r => r.GetComponentsInChildren<TechGuy.Cameras.TG_TopDown_Camera>(true))
                    .Single(c => c.m_Target != null);
                var player = roots.SelectMany(r => r.GetComponentsInChildren<PlayerActor>(true)).Single();
                Assert.That(camera.m_Target, Is.EqualTo(player.transform),
                    "The gameplay top-down camera must track the player actor.");
                var data = new SerializedObject(camera);
                Assert.That(data.FindProperty("m_Height").floatValue, Is.GreaterThan(9f),
                    "The combat camera must sit higher than the default framing for readability.");
                Assert.That(data.FindProperty("m_Distance").floatValue, Is.GreaterThan(10f),
                    "The combat camera must pull further back than the default framing for readability.");
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
        public void BossDefeatWithNoNextStage_MaterializesExactlyOneExtractionPortalInTheBossRoomBoundToTheNexus()
        {
            // Property 1: the Extraction_Portal is materialized exactly once, only on the boss defeat with no
            // next Stage; while the boss is not defeated no portal exists (R8.1/R8.2).
            // Property 2: the materialized portal's destination is the Nexus (R8.3).
            var host = new GameObject("Extraction portal regression");
            host.SetActive(false);
            var playerObject = new GameObject("Player stand-in");
            playerObject.SetActive(false);
            try
            {
                var director = host.AddComponent<ProgressionDirector>();
                var player = playerObject.AddComponent<PlayerActor>();

                // A Start_Room reaching a Boss_Room, mirroring the sibling directional-graph setup.
                var start = new Room(1, RoomType.Start, Vector2.zero, new Vector2(24, 24), 1)
                    { Visited = true, Cleared = true };
                var boss = new Room(2, RoomType.Boss, new Vector2(24, 0), new Vector2(24, 24), 2);
                var edge = new RoomConnection(1, 2, Direction.East);
                var graph = new RoomGraph(new List<Room> { start, boss },
                    new List<RoomConnection> { edge }, 1, 2);

                typeof(ProgressionDirector).GetField("_graph", PrivateInstance).SetValue(director, graph);
                typeof(ProgressionDirector).GetField("_stageCount", PrivateInstance).SetValue(director, 1);
                typeof(ProgressionDirector).GetField("_player", PrivateInstance).SetValue(director, player);
                Invoke(director, "IndexRooms", graph);
                Invoke(director, "MaterializeGates", graph);

                // Property 1 (before defeat): no Extraction_Portal exists until the boss is defeated (R8.1).
                Assert.That(host.GetComponentsInChildren<ScenePortal>(true), Is.Empty,
                    "No Extraction_Portal must exist before the boss is defeated.");

                // Drive the boss-defeat conclusion directly (the single-Stage, no-next-Stage branch).
                Invoke(director, "AdvanceToNextStageOrConclude");

                // Property 1 (after defeat): exactly one Extraction_Portal materialized as a director child.
                var portals = host.GetComponentsInChildren<ScenePortal>(true);
                Assert.That(portals.Length, Is.EqualTo(1),
                    "Defeating the boss with no next Stage must materialize exactly one Extraction_Portal.");
                var portal = portals[0];
                Assert.That(portal.transform.parent, Is.EqualTo(director.transform),
                    "The Extraction_Portal must be parented to the director.");

                // The portal sits within the Boss_Room's XZ bounds (world center (24,0,0), half-extents 12).
                Vector3 bossCenter = new Vector3(boss.Center.x, 0f, boss.Center.y);
                Vector3 position = portal.transform.position;
                Assert.That(Mathf.Abs(position.x - bossCenter.x), Is.LessThanOrEqualTo(boss.Size.x * 0.5f),
                    "The Extraction_Portal must materialize within the Boss_Room bounds on X.");
                Assert.That(Mathf.Abs(position.z - bossCenter.z), Is.LessThanOrEqualTo(boss.Size.y * 0.5f),
                    "The Extraction_Portal must materialize within the Boss_Room bounds on Z.");

                // Property 2: the destination is the Nexus, from the serialized _returnScene (default NexusLobby).
                Assert.That(portal.Destination, Is.EqualTo("NexusLobby"),
                    "The Extraction_Portal's destination must be the Nexus.");
                Assert.That(director.IsRunComplete, Is.True,
                    "Materializing the portal marks the Run logically complete.");
            }
            finally
            {
                Object.DestroyImmediate(host);
                Object.DestroyImmediate(playerObject);
            }
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

        // Property 4 (playable-procedural-run design): a player death returns to the Nexus through the
        // death flow WITHOUT materializing or depending on the Extraction_Portal. Driving OnPlayerDied by
        // reflection must flip IsRunComplete true (the death handler marks the Run ended within its first
        // statements) while creating zero ScenePortal objects — neither as a child of the director nor
        // anywhere in the scene. ReturnToNexus may attempt a guarded scene load, but in EditMode the coroutine
        // never ticks, so the test only asserts portal absence + IsRunComplete. (R8.6)
        [Test]
        public void PlayerDeath_ReturnsToNexusWithoutMaterializingAnExtractionPortal()
        {
            var host = new GameObject("Player death regression");
            host.SetActive(false);
            try
            {
                var director = host.AddComponent<ProgressionDirector>();
                Assert.That(director.IsRunComplete, Is.False,
                    "The Run must not be complete before the player dies.");
                Assert.That(host.GetComponentsInChildren<ScenePortal>(true), Is.Empty,
                    "No Extraction_Portal must exist before the player dies.");

                // OnPlayerDied ignores its Actor argument, so null exercises the death flow without dragging
                // in a full PlayerActor. StartCoroutine logs on the inactive host; the death flow sets
                // IsRunComplete before it and never materializes a portal, so tolerate that message.
                LogAssert.ignoreFailingMessages = true;
                try
                {
                    typeof(ProgressionDirector)
                        .GetMethod("OnPlayerDied", PrivateInstance)
                        .Invoke(director, new object[] { null });
                }
                finally { LogAssert.ignoreFailingMessages = false; }

                Assert.That(director.IsRunComplete, Is.True,
                    "The death flow must mark the Run complete so it returns to the Nexus (R8.6).");
                Assert.That(host.GetComponentsInChildren<ScenePortal>(true), Is.Empty,
                    "Death must not materialize an Extraction_Portal as a child of the director (R8.6).");
                Assert.That(Object.FindObjectsOfType<ScenePortal>(), Is.Empty,
                    "The death flow must not create any ScenePortal in the scene (R8.6).");
            }
            finally
            {
                Object.DestroyImmediate(host);
            }
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
