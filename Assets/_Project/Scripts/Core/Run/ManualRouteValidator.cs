using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class ManualRouteValidator
{
    public static bool TrySnapshot(ManualRunDefinition definition, WeaponScript weapon,
        out ManualRouteSnapshot snapshot, out string error)
    {
        snapshot = null;
        if (!ValidateLoadout(weapon, out error)) return false;
        if (!Validate(definition, out var paths, out error)) return false;
        snapshot = new ManualRouteSnapshot(definition, paths);
        return true;
    }

    public static bool ValidateLoadout(WeaponScript weapon, out string error)
    {
        error = null;
        if (!weapon || WeaponRunModifiers.Identify(weapon) != RunWeaponFamily.Gauntlet ||
            weapon.abilities == null || weapon.abilities.Length != 4)
        { error = "Manual slice requires the four-slot Gauntlet loadout."; return false; }
        var mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
        foreach (GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
        {
            var slot = GauntletMutationSelection.SlotOf(mutation);
            var selection = new GauntletMutationSelection();
            selection.Select(mutation);
            if (!(weapon.abilities[(int)slot] is BreakerGauntletAbility ability) ||
                !GauntletExecutionSnapshot.TryBuild(ability, slot, mods, out _, selection))
            { error = "Mutation target is invalid: " + mutation; return false; }
        }
        return true;
    }

    public static bool Validate(ManualRunDefinition definition, out List<int[]> paths, out string error)
    {
        paths = new List<int[]>(); error = null;
        if (!definition || definition.Version != 1 || string.IsNullOrWhiteSpace(definition.DefinitionId) ||
            definition.Family != RunWeaponFamily.Gauntlet || definition.Nodes == null || definition.Nodes.Count != 11 ||
            definition.Connections == null || definition.Connections.Count != 12 || definition.Groups == null || definition.Groups.Count != 2)
            return Fail("Invalid definition header or D1 node/connection/group count.", out error);
        var nodes = new Dictionary<int, ManualNodeDefinition>();
        foreach (var node in definition.Nodes)
        {
            if (node == null || node.Id < 0 || nodes.ContainsKey(node.Id) || string.IsNullOrWhiteSpace(node.Label) ||
                !Enum.IsDefined(typeof(ManualNodeKind), node.Kind) || !Enum.IsDefined(typeof(RunRewardKind), node.Reward))
                return Fail("Invalid or duplicate node.", out error);
            if (!Finite(node.Center) || !Bounded(node.Center.x, -2000, 2000) || !Bounded(node.Center.z, -2000, 2000) ||
                !Bounded(node.ArenaSize.x, 4, 128) || !Bounded(node.ArenaSize.y, 4, 128) ||
                !Bounded(node.RepairFraction, 0, 1) || node.Spawns == null || node.Spawns.Count > 32)
                return Fail("Invalid numeric authoring for node " + node.Id, out error);
            if (node.IsEncounter != (node.Spawns.Count > 0) ||
                (node.Reward == RunRewardKind.None ? node.Acquisitions != 0 : node.Acquisitions < 1 || node.Acquisitions > 2) ||
                (node.Kind == ManualNodeKind.Repair ? node.RepairFraction <= 0 : node.RepairFraction != 0))
                return Fail("Invalid reward, encounter or Repair quantity for node " + node.Id, out error);
            foreach (var spawn in node.Spawns)
                if (spawn == null || !spawn.Prefab || !spawn.Prefab.GetComponent<Actor>() || !Enum.IsDefined(typeof(EnemyRank), spawn.Rank) || !Finite(spawn.Offset) ||
                    Mathf.Abs(spawn.Offset.x) > node.ArenaSize.x * .5f - 1 || Mathf.Abs(spawn.Offset.z) > node.ArenaSize.y * .5f - 1 ||
                    !Bounded(spawn.Offset.y, 0, 5))
                    return Fail("Invalid prefab or spawn outside arena for node " + node.Id, out error);
            nodes.Add(node.Id, node);
            if (node.RewardSteps != null && node.RewardSteps.Count > 0)
            {
                if (node.RewardSteps.Count > 3 || node.Reward == RunRewardKind.None)
                    return Fail("Invalid reward steps for node " + node.Id, out error);
                foreach (var step in node.RewardSteps)
                    if (step == null || step.Kind == RunRewardKind.None || !Enum.IsDefined(typeof(RunRewardKind), step.Kind) || step.Quantity < 1 || step.Quantity > 2)
                        return Fail("Invalid reward step for node " + node.Id, out error);
                if (node.TotalAcquisitions > 3 || node.RewardAt(0) != node.Reward)
                    return Fail("Incoherent reward steps for node " + node.Id, out error);
                if (node.RewardSteps.Any(s => s.Kind == RunRewardKind.Mutation) && node.Reward != RunRewardKind.Mutation)
                    return Fail("Mutation step moved outside its guaranteed node.", out error);
            }
        }
        if (!nodes.TryGetValue(definition.StartNode, out var start) || start.Kind != ManualNodeKind.Start ||
            !nodes.TryGetValue(definition.BossNode, out var boss) || boss.Kind != ManualNodeKind.Boss ||
            nodes.Values.Count(n => n.Kind == ManualNodeKind.Start) != 1 || nodes.Values.Count(n => n.Kind == ManualNodeKind.Boss) != 1)
            return Fail("Invalid start or unique Boss.", out error);
        var outgoing = nodes.Keys.ToDictionary(id => id, id => new List<int>());
        var edgeIds = new HashSet<int>();
        foreach (var edge in definition.Connections)
        {
            if (edge == null || edge.Id < 0 || !edgeIds.Add(edge.Id) || !nodes.ContainsKey(edge.From) ||
                !nodes.ContainsKey(edge.To) || edge.From == edge.To || outgoing[edge.From].Contains(edge.To))
                return Fail("Invalid or duplicate connection.", out error);
            outgoing[edge.From].Add(edge.To);
        }
        // All nodes, including unreachable ones, must be acyclic before path enumeration.
        var marks = new Dictionary<int, int>();
        foreach (int id in nodes.Keys) if (Cyclic(id, outgoing, marks)) return Fail("Progression cycle.", out error);
        var visited = new HashSet<int>();
        if (!Enumerate(start.Id, boss.Id, outgoing, new List<int>(), paths, visited) || visited.Count != nodes.Count)
            return Fail("Unreachable node, dead end or excessive paths.", out error);
        var signatures = paths.Select(p => string.Join("/", p.Select(id => Token(nodes[id])))).OrderBy(p => p).ToArray();
        string prefix = "Start:None:0/Combat:Technique:1/Combat:Technique:1/";
        var expected = new List<string>();
        foreach (string first in new[] { "Repair:None:0/", "Combat:Technique:1/" })
            foreach (string second in new[] { "Combat:Protocol:1/", "Elite:Technique:2/Combat:Protocol:1/" })
                expected.Add(prefix + first + "Combat:Mutation:1/" + second + "Combat:Technique:1/Boss:None:0");
        if (!signatures.SequenceEqual(expected.OrderBy(p => p))) return Fail("Paths do not implement D1 rewards and 6/7/7/8 encounters.", out error);
        if (definition.ProtocolCatalog)
        {
            if (!definition.ProtocolCatalog.TryCreateSnapshot(out _, out string catalogError))
                return Fail("Invalid published Protocol catalog: " + catalogError, out error);
            foreach (var node in nodes.Values)
            {
                bool mixed = node.Id == 1 || node.Id == 2 || node.Id == 9;
                int expectedCount = mixed || node.Id == 8 ? 2 : node.Acquisitions;
                if (node.TotalAcquisitions != expectedCount)
                    return Fail("Published D1 reward count differs at node " + node.Id, out error);
                for (int ordinal = 0; ordinal < expectedCount; ordinal++)
                {
                    RunRewardKind expectedKind = mixed && ordinal == 1 ? RunRewardKind.Protocol : node.Reward;
                    if (node.RewardAt(ordinal) != expectedKind)
                        return Fail("Published D1 reward order differs at node " + node.Id, out error);
                }
            }
        }
        var groups = new HashSet<string>(); var branchNodes = new HashSet<int>(); var decisions = new HashSet<int>();
        foreach (var group in definition.Groups)
        {
            if (group == null || string.IsNullOrWhiteSpace(group.Id) || !groups.Add(group.Id) ||
                !nodes.ContainsKey(group.DecisionNode) || !decisions.Add(group.DecisionNode) ||
                group.FirstBranch == null || group.SecondBranch == null || group.FirstBranch.Count == 0 || group.SecondBranch.Count == 0 ||
                outgoing[group.DecisionNode].Count != 2 ||
                !outgoing[group.DecisionNode].Contains(group.FirstBranch[0]) || !outgoing[group.DecisionNode].Contains(group.SecondBranch[0]))
                return Fail("Invalid exclusive branch group.", out error);
            foreach (var branch in new[] { group.FirstBranch, group.SecondBranch })
                for (int i = 0; i < branch.Count; i++)
                    if (!nodes.ContainsKey(branch[i]) || !branchNodes.Add(branch[i]) ||
                        (i > 0 && !outgoing[branch[i - 1]].Contains(branch[i]))) return Fail("Invalid branch membership.", out error);
            foreach (var path in paths)
            {
                bool a = group.FirstBranch.All(path.Contains), b = group.SecondBranch.All(path.Contains);
                if (!path.Contains(group.DecisionNode) || a == b ||
                    (a && group.SecondBranch.Any(path.Contains)) || (b && group.FirstBranch.Any(path.Contains)))
                    return Fail("Exclusive groups disagree with paths.", out error);
            }
        }
        // Both branching points must be declared; membership must include all pre-reconnection nodes.
        if (outgoing.Count(pair => pair.Value.Count == 2) != 2 || branchNodes.Count != 5 ||
            outgoing.Any(pair => pair.Value.Count > 2 || (pair.Value.Count == 2 && !decisions.Contains(pair.Key))))
            return Fail("Incomplete branch groups.", out error);
        return true;
    }

    private static string Token(ManualNodeDefinition node) => node.Kind + ":" + node.Reward + ":" + node.Acquisitions;
    private static bool Cyclic(int id, Dictionary<int, List<int>> edges, Dictionary<int, int> marks)
    {
        if (marks.TryGetValue(id, out int mark)) return mark == 1;
        marks[id] = 1;
        foreach (int next in edges[id]) if (Cyclic(next, edges, marks)) return true;
        marks[id] = 2; return false;
    }
    private static bool Enumerate(int id, int boss, Dictionary<int, List<int>> edges, List<int> prefix, List<int[]> paths, HashSet<int> visited)
    {
        visited.Add(id); prefix.Add(id);
        if (id == boss)
        {
            if (edges[id].Count != 0) return false;
            paths.Add(prefix.ToArray());
        }
        else
        {
            if (edges[id].Count == 0) return false;
            foreach (int next in edges[id]) if (!Enumerate(next, boss, edges, prefix, paths, visited)) return false;
        }
        prefix.RemoveAt(prefix.Count - 1); return paths.Count <= 4;
    }
    private static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    private static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    private static bool Bounded(float value, float min, float max) => Finite(value) && value >= min && value <= max;
    private static bool Fail(string message, out string error) { error = message; return false; }
}
