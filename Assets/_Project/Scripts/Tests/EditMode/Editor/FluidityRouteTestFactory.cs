using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>Isolated D1 route fixture for the fluidity suite; independent of legacy test assemblies.</summary>
    internal static class FluidityRouteTestFactory
    {
        private const string PublishedDefinitionPath = "Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset";

        public static ManualRunDefinition Create()
        {
            var published = AssetDatabase.LoadAssetAtPath<ManualRunDefinition>(PublishedDefinitionPath);
            GameObject prefab = published ? published.Nodes.Where(n => n.IsEncounter).SelectMany(n => n.Spawns)
                .Select(s => s.Prefab).FirstOrDefault(p => p && p.GetComponent<Actor>()) : null;
            if (!prefab) throw new InvalidOperationException("Fluidity route fixture requires a published Actor prefab at " + PublishedDefinitionPath);
            var nodes = new List<ManualNodeDefinition>();
            string[] labels = { "S", "C1", "C2", "RP", "C3", "C4", "C5", "E1", "E2", "C6", "B" };
            for (int id = 0; id < labels.Length; id++)
            {
                ManualNodeKind kind = id == 0 ? ManualNodeKind.Start : id == 3 ? ManualNodeKind.Repair :
                    id == 7 ? ManualNodeKind.Elite : id == 10 ? ManualNodeKind.Boss : ManualNodeKind.Combat;
                RunRewardKind reward = id == 0 || id == 3 || id == 10 ? RunRewardKind.None :
                    id == 5 ? RunRewardKind.Mutation : id == 6 || id == 8 ? RunRewardKind.Protocol : RunRewardKind.Technique;
                int acquisitions = reward == RunRewardKind.None ? 0 : id == 7 ? 2 : 1;
                var spawns = new List<ManualEnemySpawn>();
                if (kind != ManualNodeKind.Start && kind != ManualNodeKind.Repair)
                    spawns.Add(new ManualEnemySpawn(prefab, Vector3.zero,
                        kind == ManualNodeKind.Elite ? EnemyRank.Elite : kind == ManualNodeKind.Boss ? EnemyRank.Boss : EnemyRank.Normal));
                nodes.Add(new ManualNodeDefinition(id, labels[id], kind, reward, acquisitions,
                    new Vector3(0f, 0f, id * 30f), new Vector2(24f, 20f), id == 3 ? .35f : 0f, spawns));
            }
            // Four D1 paths: Repair/Technique and normal/Elite+follow-up; reconnection is shared.
            int[,] pairs = { {0,1}, {1,2}, {2,3}, {2,4}, {3,5}, {4,5}, {5,6}, {5,7}, {6,9}, {7,8}, {8,9}, {9,10} };
            var edges = new List<ManualConnectionDefinition>();
            for (int id = 0; id < pairs.GetLength(0); id++)
                edges.Add(new ManualConnectionDefinition(id, pairs[id, 0], pairs[id, 1]));
            var definition = ScriptableObject.CreateInstance<ManualRunDefinition>();
            definition.Configure("fluidity-map-d1-fixture", 0, 10, nodes, edges, new[] {
                new ManualExclusiveGroup("A", 2, new[] {3}, new[] {4}),
                new ManualExclusiveGroup("B", 5, new[] {6}, new[] {7,8}) });
            return definition;
        }

        public static void Set(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            if (field == null) throw new ArgumentException("Missing serialized fixture field " + fieldName, nameof(fieldName));
            field.SetValue(target, value);
        }
    }
}
