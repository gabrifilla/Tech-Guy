using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    internal static class ManualRunTestFactory
    {
        public static ManualRunDefinition Create()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Enemies/Grunt.prefab");
            if (!prefab)
            {
                string guid = AssetDatabase.FindAssets("Grunt t:Prefab", new[] { "Assets/_Project" })
                    .First(g => AssetDatabase.GUIDToAssetPath(g).EndsWith("/Grunt.prefab"));
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
            }
            var nodes = new List<ManualNodeDefinition>();
            string[] labels = { "S", "C1", "C2", "RP", "C3", "C4", "C5", "E1", "E2", "C6", "B" };
            for (int i = 0; i < labels.Length; i++)
            {
                ManualNodeKind kind = i == 0 ? ManualNodeKind.Start : i == 3 ? ManualNodeKind.Repair :
                    i == 7 ? ManualNodeKind.Elite : i == 10 ? ManualNodeKind.Boss : ManualNodeKind.Combat;
                RunRewardKind reward = i == 0 || i == 3 || i == 10 ? RunRewardKind.None :
                    i == 5 ? RunRewardKind.Mutation : i == 6 || i == 8 ? RunRewardKind.Protocol : RunRewardKind.Technique;
                var spawns = new List<ManualEnemySpawn>();
                if (kind != ManualNodeKind.Start && kind != ManualNodeKind.Repair) spawns.Add(new ManualEnemySpawn(prefab, Vector3.zero));
                nodes.Add(new ManualNodeDefinition(i, labels[i], kind, reward, reward == RunRewardKind.None ? 0 : i == 7 ? 2 : 1,
                    new Vector3(0, 0, i * 30), new Vector2(24, 20), i == 3 ? .35f : 0f, spawns));
            }
            int[,] pairs = { {0,1},{1,2},{2,3},{2,4},{3,5},{4,5},{5,6},{5,7},{6,9},{7,8},{8,9},{9,10} };
            var edges = new List<ManualConnectionDefinition>();
            for (int i = 0; i < pairs.GetLength(0); i++) edges.Add(new ManualConnectionDefinition(i, pairs[i, 0], pairs[i, 1]));
            var definition = ScriptableObject.CreateInstance<ManualRunDefinition>();
            definition.Configure("fixture-d1", 0, 10, nodes, edges, new[] {
                new ManualExclusiveGroup("A", 2, new[]{3}, new[]{4}), new ManualExclusiveGroup("B", 5, new[]{6}, new[]{7,8}) });
            return definition;
        }
        public static void Set(object target, string field, object value) =>
            target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
    }

    public sealed class ManualRunDefinitionTests
    {
        private ManualRunDefinition _definition;
        private WeaponScript _weapon;
        [SetUp] public void SetUp()
        {
            _definition = ManualRunTestFactory.Create();
            _weapon = Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet");
        }
        [TearDown] public void TearDown() { if (_definition) UnityEngine.Object.DestroyImmediate(_definition); }

        [Test] public void D1HasExactlyFourPathsWithExpectedCombatCountsAndMandatoryMutation()
        {
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition, _weapon, out var snapshot, out string error), error);
            Assert.That(snapshot.Paths.Select(p => p.Count(id => snapshot.Node(id).IsEncounter)).OrderBy(n => n), Is.EqualTo(new[]{6,7,7,8}));
            Assert.IsTrue(snapshot.Paths.All(p => p.Contains(5)));
            Assert.IsTrue(snapshot.Paths.Any(p => !p.Contains(7)));
            Assert.AreEqual(0, (int)RunProgressionMode.ProceduralLegacy);
        }

        [Test] public void SnapshotDoesNotObserveInspectorEditsOrReturnedPathChanges()
        {
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition, _weapon, out var snapshot, out _));
            ManualRunTestFactory.Set(_definition.Nodes[3], "_repairFraction", .75f);
            snapshot.Paths[0][0] = 999;
            ManualRunTestFactory.Set(_definition.Groups[0], "_firstBranch", new List<int>{999});
            Assert.AreEqual(.35f, snapshot.Node(3).RepairFraction);
            Assert.AreEqual(0, snapshot.Paths[0][0]);
            Assert.AreEqual(3, snapshot.Groups[0].FirstBranch[0]);
        }

        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-.1f)] [TestCase(0f)] [TestCase(1.1f)]
        public void InvalidRawRepairFractionRejectsWithoutSnapshot(float fraction)
        {
            ManualRunTestFactory.Set(_definition.Nodes[3], "_repairFraction", fraction);
            Assert.IsFalse(ManualRouteValidator.TrySnapshot(_definition, _weapon, out var snapshot, out var error));
            Assert.IsNull(snapshot); Assert.IsNotEmpty(error);
        }

        [TestCase("duplicateNode")] [TestCase("invalidEnum")] [TestCase("invalidRank")] [TestCase("missingPrefab")]
        [TestCase("outsideSpawn")] [TestCase("negativeCount")] [TestCase("mutationMissing")]
        [TestCase("cycle")] [TestCase("dangling")] [TestCase("crossBranch")]
        [TestCase("duplicateEdge")] [TestCase("incompleteGroup")] [TestCase("unreachable")]
        public void InvalidGraphOrContentIsRejected(string defect)
        {
            switch (defect)
            {
                case "duplicateNode": ManualRunTestFactory.Set(_definition.Nodes[1], "_id", 0); break;
                case "invalidEnum": ManualRunTestFactory.Set(_definition.Nodes[1], "_kind", (ManualNodeKind)99); break;
                case "invalidRank": ManualRunTestFactory.Set(_definition.Nodes[1].Spawns[0], "_rank", (EnemyRank)99); break;
                case "missingPrefab": ManualRunTestFactory.Set(_definition.Nodes[1].Spawns[0], "_prefab", null); break;
                case "outsideSpawn": ManualRunTestFactory.Set(_definition.Nodes[1].Spawns[0], "_offset", new Vector3(100,0,0)); break;
                case "negativeCount": ManualRunTestFactory.Set(_definition.Nodes[1], "_acquisitions", -1); break;
                case "mutationMissing": ManualRunTestFactory.Set(_definition.Nodes[5], "_reward", RunRewardKind.Technique); break;
                case "cycle": ManualRunTestFactory.Set(_definition.Connections[11], "_to", 0); break;
                case "dangling": ManualRunTestFactory.Set(_definition.Connections[11], "_to", 999); break;
                case "crossBranch": ManualRunTestFactory.Set(_definition.Connections[4], "_to", 4); break;
                case "duplicateEdge": ManualRunTestFactory.Set(_definition.Connections[1], "_id", 0); break;
                case "incompleteGroup": ManualRunTestFactory.Set(_definition.Groups[1], "_secondBranch", new List<int>{7}); break;
                case "unreachable": ManualRunTestFactory.Set(_definition.Connections[3], "_to", 3); break;
            }
            Assert.IsFalse(ManualRouteValidator.TrySnapshot(_definition, _weapon, out var snapshot, out string error));
            Assert.IsNull(snapshot); Assert.IsNotEmpty(error);
        }

        [Test] public void IncompatibleOrMissingLoadoutIsRejectedBeforeCreatingSessionData()
        {
            Assert.IsFalse(ManualRouteValidator.TrySnapshot(_definition, null, out var snapshot, out _));
            Assert.IsNull(snapshot);
            var bow = Resources.Load<WeaponScript>("Weapons/Ranged/Bow_arrow/Bow");
            Assert.IsFalse(ManualRouteValidator.TrySnapshot(_definition, bow, out snapshot, out _));
            Assert.IsNull(snapshot);
        }

        [Test] public void AuthoringSaveAndReloadPreservesListsNumbersAndPrefabReferences()
        {
            string path = "Assets/_Project/Scripts/Tests/EditMode/Editor/ManualRunPersistence-" + Guid.NewGuid().ToString("N") + ".asset";
            try
            {
                AssetDatabase.CreateAsset(_definition, path); AssetDatabase.SaveAssetIfDirty(_definition);
                Resources.UnloadAsset(_definition); _definition = null;
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                _definition = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(path);
                Assert.AreEqual(11, _definition.Nodes.Count); Assert.AreEqual(12, _definition.Connections.Count);
                Assert.AreEqual(.35f, _definition.Nodes[3].RepairFraction);
                Assert.IsNotNull(_definition.Nodes[1].Spawns[0].Prefab);
                Assert.AreEqual(2, _definition.Groups[1].SecondBranch.Count);
                Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition, _weapon, out _, out string error), error);
            }
            finally { _definition = null; AssetDatabase.DeleteAsset(path); }
        }
    }
}
