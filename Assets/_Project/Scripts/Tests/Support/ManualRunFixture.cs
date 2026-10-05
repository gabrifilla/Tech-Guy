using System.Collections.Generic;
using UnityEngine;

namespace TechGuy.Tests
{
    /// <summary>Topology fixture, not an authored arena/layout or tuning approval.</summary>
    public static class ManualRunFixture
    {
        public static ManualRunDefinition Create(GameObject prefab, bool colocated = false)
        {
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
                    colocated ? Vector3.zero : new Vector3(0, 0, i * 30), new Vector2(24, 20), i == 3 ? .35f : 0f, spawns));
            }
            int[,] pairs = { {0,1},{1,2},{2,3},{2,4},{3,5},{4,5},{5,6},{5,7},{6,9},{7,8},{8,9},{9,10} };
            var edges = new List<ManualConnectionDefinition>();
            for (int i = 0; i < pairs.GetLength(0); i++) edges.Add(new ManualConnectionDefinition(i, pairs[i, 0], pairs[i, 1]));
            var definition = ScriptableObject.CreateInstance<ManualRunDefinition>();
            definition.Configure("fixture-d1", 0, 10, nodes, edges, new[] {
                new ManualExclusiveGroup("A", 2, new[]{3}, new[]{4}), new ManualExclusiveGroup("B", 5, new[]{6}, new[]{7,8}) });
            return definition;
        }
    }
}
