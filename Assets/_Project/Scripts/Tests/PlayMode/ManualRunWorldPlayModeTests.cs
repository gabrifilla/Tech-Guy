using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static void SetManualField(object target, string name, object value) => target.GetType()
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private (ProgressionDirector director, ManualStageEnvironment environment, GameObject root, ManualRunDefinition definition, GameObject proceduralRoot)
            CreateManualWorldFixture(bool missingNavigation = false, int firstEncounterEnemies = 1, bool firstEncounterSpawner = false)
        {
            var template = new GameObject("Manual actor template"); template.SetActive(false); _spawned.Add(template);
            var actor = template.AddComponent<Actor>(); actor.health = 100f;
            template.AddComponent<BoxCollider>();
            if (firstEncounterSpawner)
            {
                var swarm = new GameObject("Produced swarm template"); swarm.SetActive(false); _spawned.Add(swarm);
                swarm.AddComponent<Actor>().health = 25f; swarm.AddComponent<BoxCollider>();
                template.AddComponent<UnityEngine.AI.NavMeshAgent>();
                template.AddComponent<EnemyAI>().enabled = false;
                var spawner = template.AddComponent<SpawnerBehavior>();
                SetManualField(spawner, "_swarmPrefab", swarm);
                SetManualField(spawner, "_spawnInterval", 1000f);
                SetManualField(spawner, "_livingCap", 1);
                SetManualField(spawner, "_spawnRadius", .2f);
            }
            var definition = ManualRunFixture.Create(template, true);
            if(firstEncounterEnemies>1)
            {
                var authoredNodes=definition.Nodes.ToList(); var original=authoredNodes[1]; var spawns=new List<ManualEnemySpawn>();
                for(int i=0;i<firstEncounterEnemies;i++) spawns.Add(new ManualEnemySpawn(template,new Vector3(i*.3f,0,1.2f)));
                authoredNodes[1]=new ManualNodeDefinition(original.Id,original.Label,original.Kind,original.Reward,original.Acquisitions,
                    original.Center,original.ArenaSize,original.RepairFraction,spawns);
                definition.Configure(definition.DefinitionId,definition.StartNode,definition.BossNode,authoredNodes,definition.Connections.ToList(),definition.Groups.ToList());
            }
            var directorObject = new GameObject("Manual director fixture"); directorObject.SetActive(false); _spawned.Add(directorObject);
            var root = new GameObject("Authored fixture root"); root.SetActive(false); _spawned.Add(root);
            var proceduralRoot = new GameObject("Procedural fixture root"); proceduralRoot.SetActive(false); _spawned.Add(proceduralRoot);
            var proceduralEnvironment = directorObject.AddComponent<ProceduralStageEnvironment>();
            SetManualField(proceduralEnvironment, "_geometry", proceduralRoot.transform);
            var environment = directorObject.AddComponent<ManualStageEnvironment>();
            var nav = root.AddComponent<NavMeshSurface>();
            if (!missingNavigation) nav.navMeshData = _navMesh;
            var nodes = new List<ManualNodeBinding>(); var edges = new List<ManualConnectionBinding>();
            foreach (var node in definition.Nodes)
            {
                var arena = new GameObject(node.Label); arena.transform.SetParent(root.transform, false);
                var volume = arena.AddComponent<BoxCollider>(); volume.isTrigger = true; volume.size = new Vector3(4,5,4);
                var reward = new GameObject("Reward anchor"); reward.transform.SetParent(arena.transform, false);
                var binding = new ManualNodeBinding();
                SetManualField(binding, "_nodeId", node.Id); SetManualField(binding, "_arena", arena.transform);
                SetManualField(binding, "_entryVolume", volume); SetManualField(binding, "_rewardAnchor", reward.transform); nodes.Add(binding);
            }
            foreach (var edge in definition.Connections)
            {
                var barrier = new GameObject("Barrier " + edge.Id); barrier.transform.SetParent(root.transform, false);
                barrier.transform.position = Vector3.right * (10 + edge.Id); barrier.AddComponent<BoxCollider>();
                var binding = new ManualConnectionBinding(); SetManualField(binding, "_connectionId", edge.Id);
                SetManualField(binding, "_barrier", barrier); SetManualField(binding, "_fromAnchor", barrier.transform);
                SetManualField(binding, "_toAnchor", barrier.transform); edges.Add(binding);
            }
            SetManualField(environment, "_geometryRoot", root); SetManualField(environment, "_navigation", nav);
            SetManualField(environment, "_nodes", nodes); SetManualField(environment, "_connections", edges);
            var director = directorObject.AddComponent<ProgressionDirector>();
            SetManualField(director, "_progressionMode", RunProgressionMode.ManualSlice);
            SetManualField(director, "_manualDefinition", definition); SetManualField(director, "_manualEnvironment", environment);
            SetManualField(director, "_player", _player);
            SetManualField(director, "_environment", proceduralEnvironment);
            return (director, environment, root, definition, proceduralRoot);
        }

        [UnityTest]
        public IEnumerator ManualSliceInitializesOneOwnerAndDefersLastDeathWithoutProceduralGeneration()
        {
            int oldIndex = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture = CreateManualWorldFixture();
            try
            {
                if (_navMeshInstance.valid) _navMeshInstance.Remove();
                fixture.director.gameObject.SetActive(true); yield return null; yield return null;
                Assert.AreEqual(RunProgressionMode.ManualSlice, fixture.director.ActiveProgressionMode);
                Assert.IsNotNull(fixture.director.ManualSession); Assert.IsNull(fixture.director.CurrentGraph);
                Assert.IsTrue(fixture.root.activeSelf); Assert.IsTrue(_agent.isOnNavMesh);
                Assert.IsFalse(fixture.proceduralRoot.activeSelf);
                Assert.IsFalse(fixture.director.TryEnterNode(fixture.director.ManualSession.Id, 2));
                Assert.IsTrue(fixture.director.TryCommitDestination(fixture.director.ManualSession.Id, 1));
                Assert.IsTrue(fixture.director.TryEnterNode(fixture.director.ManualSession.Id, 1));
                var encounter = fixture.director.GetComponentsInChildren<Actor>().Single();
                encounter.TakeDamage(encounter.health + 1f);
                Assert.IsTrue(fixture.director.HasManualCompletionCandidate);
                Assert.AreEqual(RunPhase.Encounter, fixture.director.ManualSession.Phase);
                yield return null;
                Assert.AreEqual(RunPhase.RewardPending, fixture.director.ManualSession.Phase,
                    "Task 11 resolves only after the death callback's mechanical frame has finished.");
                Assert.IsFalse(fixture.director.HasManualCompletionCandidate);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, oldIndex); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest]
        public IEnumerator ManualProducedSwarmKeepsEncounterOpenAndIsRemovedOnCompletion()
        {
            bool hadSelection = PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey);
            int oldIndex = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture = CreateManualWorldFixture(firstEncounterSpawner: true);
            try
            {
                if (_navMeshInstance.valid) _navMeshInstance.Remove();
                fixture.director.gameObject.SetActive(true); yield return null; yield return null; yield return null;
                var session = fixture.director.ManualSession;
                Assert.IsNotNull(session);
                Assert.IsTrue(fixture.director.TryCommitDestination(session.Id, 1));
                Assert.IsTrue(fixture.director.TryEnterNode(session.Id, 1));
                var spawner = fixture.director.GetComponentsInChildren<SpawnerBehavior>().Single();
                Assert.AreEqual(1, fixture.director.ManualEnemiesRemaining);
                typeof(SpawnerBehavior).GetMethod("TrySpawnOne", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(spawner, null);
                var produced = fixture.director.GetComponentsInChildren<Actor>(true).Single(enemy => enemy != spawner.GetComponent<Actor>());
                produced.gameObject.SetActive(true);
                Assert.AreEqual(2, fixture.director.ManualEnemiesRemaining, "A produced enemy joins the owning encounter before rewards can resolve.");
                Assert.IsTrue(produced.transform.IsChildOf(fixture.director.transform));
                var membership = produced.GetComponent<ProtocolEncounterMembership>();
                Assert.IsNotNull(membership);
                Assert.IsTrue(membership.Matches(session.Id, 1));
                spawner.GetComponent<Actor>().TakeDamage(1000000f);
                yield return null;
                Assert.AreEqual(RunPhase.Encounter, session.Phase, "Killing the original Spawner cannot ignore a surviving Swarm.");
                Assert.AreEqual(1, fixture.director.ManualEnemiesRemaining);
                Assert.IsFalse(fixture.director.HasManualCompletionCandidate);
                produced.TakeDamage(1000000f);
                float deadline = Time.realtimeSinceStartup + 3f;
                while (session.Phase == RunPhase.Encounter)
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                Assert.AreEqual(RunPhase.RewardPending, session.Phase);
                Assert.AreEqual(0, fixture.director.ManualEnemiesRemaining);
                yield return null;
                Assert.IsTrue(!produced, "Encounter cleanup removes the produced actor with its encounter root.");
                Assert.IsEmpty(fixture.director.GetComponentsInChildren<Actor>(true));
            }
            finally
            {
                if (hadSelection) PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, oldIndex); else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
                UnityEngine.Object.DestroyImmediate(fixture.definition);
            }
        }

        [UnityTest]
        public IEnumerator ManualSliceInvalidEnvironmentRefusesBeforeAnyWorldMaterialization()
        {
            int oldIndex = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture = CreateManualWorldFixture(true);
            try
            {
                LogAssert.Expect(LogType.Error, "Manual run refused: Manual geometry, baked navigation or binding count is invalid.");
                fixture.director.gameObject.SetActive(true); yield return null;
                Assert.IsFalse(fixture.director.enabled); Assert.IsNull(fixture.director.ManualSession);
                Assert.IsNull(fixture.director.CurrentGraph); Assert.IsFalse(fixture.root.activeSelf);
                Assert.IsFalse(fixture.director.IsStageBuilt);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, oldIndex); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest]
        public IEnumerator ManualSliceBowSelectionRetainsWeaponAndBindsCompatibleInventoryBeforeLegacyDependencyChecks()
        {
            const string unlockKey = "TechGuy.Loadout.Unlocked.1";
            int oldIndex = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            bool hadUnlock = PlayerPrefs.HasKey(unlockKey); int oldUnlock = PlayerPrefs.GetInt(unlockKey, 0);
            PlayerPrefs.SetInt(unlockKey, 1); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 1);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture = CreateManualWorldFixture();
            try
            {
                if (_navMeshInstance.valid) _navMeshInstance.Remove();
                fixture.director.gameObject.SetActive(true); yield return null; yield return null; yield return null;
                Assert.AreEqual(RunProgressionMode.ManualSlice, fixture.director.ActiveProgressionMode);
                Assert.AreEqual(RunWeaponFamily.Bow, WeaponRunModifiers.Identify(_player.CurrentWeapon));
                Assert.IsNotNull(fixture.director.ManualSession); Assert.IsTrue(fixture.root.activeSelf);
                Assert.IsFalse(fixture.proceduralRoot.activeSelf); Assert.IsNull(fixture.director.CurrentGraph);
                var run = _player.GetComponent<RunBoons>();
                Assert.IsNotNull(run); Assert.AreEqual(RunWeaponFamily.Bow, run.WeaponModifiers.Family);
                Assert.AreEqual(fixture.director.ManualSession.Id, run.Protocols.SessionId);
                Assert.AreSame(run.Protocols,_player.GetComponent<ProtocolRuntime>().Inventory);
                Assert.IsNotEmpty(ManualRewardCatalog.Eligible(RunRewardKind.Mutation,run.WeaponModifiers,_player.CurrentWeapon,run.Protocols));
            }
            finally
            {
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, oldIndex);
                if (hadUnlock) PlayerPrefs.SetInt(unlockKey, oldUnlock); else PlayerPrefs.DeleteKey(unlockKey);
                UnityEngine.Object.DestroyImmediate(fixture.definition);
            }
        }
    }
}
