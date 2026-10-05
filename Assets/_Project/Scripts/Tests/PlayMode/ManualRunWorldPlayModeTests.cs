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
            CreateManualWorldFixture(bool missingNavigation = false, int firstEncounterEnemies = 1)
        {
            var template = new GameObject("Manual actor template"); template.SetActive(false); _spawned.Add(template);
            var actor = template.AddComponent<Actor>(); actor.health = 100f;
            template.AddComponent<BoxCollider>();
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
        public IEnumerator ManualSliceBowSelectionRetainsWeaponAndSelectsProceduralBeforeDependencyChecks()
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
                // This minimal fixture intentionally lacks the procedural dependencies; the log
                // proves selection has reached that preserved path before any manual validation.
                LogAssert.Expect(LogType.Error, "ProgressionDirector is missing required dependencies: _generationParams (StageGenerationParams), _enemyPrefab (enemy prefab), _archetypeCatalog (EnemyArchetype catalog), _objective (TMP_Text objective).");
                fixture.director.gameObject.SetActive(true); yield return null;
                Assert.AreEqual(RunProgressionMode.ProceduralLegacy, fixture.director.ActiveProgressionMode);
                Assert.AreEqual(RunWeaponFamily.Bow, WeaponRunModifiers.Identify(_player.CurrentWeapon));
                Assert.IsNull(fixture.director.ManualSession); Assert.IsFalse(fixture.root.activeSelf);
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
