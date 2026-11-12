using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Plans a new run from the current authored room vocabulary. It changes data only:
/// the environment owns materialization, navigation and scene references.
/// </summary>
public static class ManualRunRandomizer
{
    private const ulong LayoutStream = 0x4C41594F5554UL;
    private const ulong EnemyStream = 0x454E454D494553UL;
    private const ulong RewardStream = 0x524557415244UL;
    private const int MinimumEnemies = 4;
    private const int MaximumEnemies = 10;
    private const float SpawnInset = 3f;

    public static ManualRouteSnapshot Generate(ManualRouteSnapshot source, ulong runSeed,
        IReadOnlyList<GameObject> enemyPrefabs, FirstSectorContentDefinition content = null, bool spatialLayouts = false,
        bool spatialVariety = true, bool spatialOrientations = false)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var ordered = source.Nodes.OrderBy(node => node.Id).ToList();
        var depths = Depths(source);
        var layout = new SeededRng(SeedDerivation.SplitMix64(runSeed ^ LayoutStream));
        var levels = new float[depths.Values.Max() + 1];
        for (int i = 1; i < levels.Length; i++) levels[i] = levels[i - 1] + layout.NextInt(38, 51);
        var lanes = new Dictionary<int, float>();
        foreach (var group in source.Groups.OrderBy(group => group.Id, StringComparer.Ordinal))
        {
            float lane = layout.NextInt(48, 61) * (layout.Roll(.5) ? 1f : -1f);
            foreach (int id in group.FirstBranch) lanes.Add(id, lane);
            foreach (int id in group.SecondBranch) lanes.Add(id, -lane);
        }
        Vector3 origin = source.Node(source.StartNode).Center;
        var pool = BuildPool(source, enemyPrefabs);
        if(content)
        {
            var seen=new HashSet<GameObject>(pool);
            foreach(var prefab in content.AdditionalEnemies)AddPrefab(prefab,pool,seen);
        }
        var fighters = pool.Where(prefab => !IsSupport(prefab)).ToList();
        if (fighters.Count == 0)
            throw new InvalidOperationException("Procedural run needs at least one combat-capable enemy prefab.");
        var common = new HashSet<int>(source.Paths[0]);
        foreach (var path in source.Paths) common.IntersectWith(path);
        int openingReward = ordered.Where(node => common.Contains(node.Id) && node.Kind == ManualNodeKind.Combat &&
            node.Reward != RunRewardKind.Mutation).OrderBy(node => depths[node.Id]).ThenBy(node => node.Id)
            .Select(node => node.Id).DefaultIfEmpty(-1).First();
        var generated = new List<ManualNodeDefinition>();
        var spatialFamilies = spatialLayouts && content && spatialVariety
            ? FirstSectorRoomDistribution.Plan(source,runSeed,content.SpatialVariety) : null;
        foreach (var node in ordered)
        {
            ulong nodeSeed = SeedDerivation.SplitMix64(runSeed ^ unchecked((ulong)node.Id));
            var enemies = new SeededRng(SeedDerivation.SplitMix64(nodeSeed ^ EnemyStream));
            var rewards = new SeededRng(SeedDerivation.SplitMix64(nodeSeed ^ RewardStream));
            var size = new Vector2(layout.NextInt(22, 31), layout.NextInt(18, 25));
            var center = origin + new Vector3(lanes.TryGetValue(node.Id, out float x) ? x : 0f, 0f, levels[depths[node.Id]]);
            var steps = PlanRewards(node, rewards, node.Id == openingReward);
            var spawns = PlanEnemies(node, size, depths[node.Id], pool, fighters, enemies);
            if(content && content.HasCover(runSeed,node))spawns=FirstSectorContentDefinition.SafeSpawns(spawns,size);
            if(content && node.Kind==ManualNodeKind.Combat)spawns=FirstSectorEncounterLayout.Arrange(spawns,runSeed,node.Id);
            FirstSectorRoomLayout spatial = null;
            if (spatialLayouts && content && node.Kind == ManualNodeKind.Combat)
            {
                var family = spatialFamilies != null ? spatialFamilies[node.Id] : FirstSectorRoomLayout.SelectFamily(runSeed, node.Id);
                if (family != FirstSectorRoomFamily.Rectangle)
                {
                    int turns=spatialOrientations?FirstSectorRoomDistribution.Orientation(runSeed,node.Id,family,size):0;
                    spatial = new FirstSectorRoomLayout(family, size, content.HasCover(runSeed,node),turns);
                    size = spatial.Size;
                    spawns = spatial.Arrange(spawns);
                }
            }
            RunRewardKind first = steps.Count == 0 ? RunRewardKind.None : steps[0].Kind;
            int acquisitions = steps.Count == 0 ? 0 : steps[0].Quantity;
            var copy = new ManualNodeDefinition(node.Id, node.Label, node.Kind, first, acquisitions,
                center, size, node.RepairFraction, spawns);
            copy.ConfigureRewardSteps(steps);
            copy.ConfigureSpatialLayout(spatial);
            generated.Add(copy);
        }
        Compact(source, generated, depths, origin);
        return new ManualRouteSnapshot(source, generated, runSeed);
    }

    // Route compaction (11/11/2026, playtest: walking between rooms took too long). The fixed spacing above is sized for
    // the worst case (30 m rotated spatial rooms), which left 14–32 m corridors and ~50 m branch legs. Once every room's real
    // footprint is known, each depth level sits just past the deepest rooms around it and each branch just past its widest
    // room. Draws, seeds and room contents are untouched: only centres move.
    internal const float CorridorGap = 14f;   // edge-to-edge distance between consecutive levels (4 m port stubs + 6 m between)
    internal const float BranchGap = 16f;     // extra lateral distance past a branch's widest room

    private static Vector2 Footprint(ManualNodeDefinition node)
    {
        var size = node.ArenaSize;
        return node.SpatialLayout != null && node.SpatialLayout.QuarterTurns % 2 == 1 ? new Vector2(size.y, size.x) : size;
    }

    private static void Compact(ManualRouteSnapshot source, List<ManualNodeDefinition> nodes, Dictionary<int, int> depths, Vector3 origin)
    {
        if (nodes.Count == 0) return;
        int maxDepth = depths.Values.Max();
        var halfDepth = new float[maxDepth + 1];
        foreach (var node in nodes) halfDepth[depths[node.Id]] = Mathf.Max(halfDepth[depths[node.Id]], Footprint(node).y * .5f);
        var levels = new float[maxDepth + 1];
        for (int i = 1; i <= maxDepth; i++) levels[i] = levels[i - 1] + halfDepth[i - 1] + halfDepth[i] + CorridorGap;

        var byId = nodes.ToDictionary(node => node.Id);
        var lanes = new Dictionary<int, float>();
        foreach (var group in source.Groups)
        {
            var branchIds = group.FirstBranch.Concat(group.SecondBranch).Where(byId.ContainsKey).ToList();
            if (branchIds.Count == 0) continue;
            float widest = branchIds.Max(id => Footprint(byId[id]).x * .5f);
            float lane = widest + BranchGap;
            // Keep each branch on the side the draw chose.
            float firstSign = group.FirstBranch.Select(id => byId.TryGetValue(id, out var n) ? Mathf.Sign(n.Center.x - origin.x) : 0f)
                .FirstOrDefault(sign => sign != 0f);
            if (firstSign == 0f) firstSign = 1f;
            foreach (int id in group.FirstBranch) lanes[id] = lane * firstSign;
            foreach (int id in group.SecondBranch) lanes[id] = -lane * firstSign;
        }
        foreach (var node in nodes)
            node.ConfigureCenter(origin + new Vector3(lanes.TryGetValue(node.Id, out float x) ? x : 0f, 0f, levels[depths[node.Id]]));
    }

    private static Dictionary<int, int> Depths(ManualRouteSnapshot route)
    {
        var result = new Dictionary<int, int>();
        foreach (var path in route.Paths)
            for (int i = 0; i < path.Length; i++)
                if (!result.TryGetValue(path[i], out int depth) || depth < i) result[path[i]] = i;
        if (result.Count != route.Nodes.Count)
            throw new InvalidOperationException("Procedural route contains an unreachable room.");
        return result;
    }

    private static List<GameObject> BuildPool(ManualRouteSnapshot source, IReadOnlyList<GameObject> candidates)
    {
        var result = new List<GameObject>();
        var seen = new HashSet<GameObject>();
        if (candidates != null)
            foreach (var candidate in candidates) AddPrefab(candidate, result, seen);
        // Authored ordinary enemies are a safe, explicit fallback for incomplete catalog bindings.
        foreach (var node in source.Nodes.OrderBy(node => node.Id))
            foreach (var spawn in node.Spawns)
                if (spawn.Rank == EnemyRank.Normal) AddPrefab(spawn.Prefab, result, seen);
        return result;
    }

    private static void AddPrefab(GameObject prefab, List<GameObject> pool, HashSet<GameObject> seen)
    {
        if (prefab && prefab.GetComponent<Actor>() && prefab.GetComponent<EnemyAI>() &&
            !prefab.GetComponent<SectorBoss>() && seen.Add(prefab)) pool.Add(prefab);
    }

    private static bool IsSupport(GameObject prefab)
    {
        var variant = prefab.GetComponent<EnemyVariant>();
        return variant && (variant.ArchetypeId == ArchetypeId.Healer || variant.ArchetypeId == ArchetypeId.ShieldSupport);
    }

    private static List<ManualEnemySpawn> PlanEnemies(ManualNodeDefinition node, Vector2 size, int depth,
        List<GameObject> pool, List<GameObject> fighters, SeededRng rng)
    {
        var result = new List<ManualEnemySpawn>();
        if (!node.IsEncounter) return result;
        if (node.Kind == ManualNodeKind.Boss)
        {
            foreach (var spawn in node.Spawns)
                result.Add(new ManualEnemySpawn(spawn.Prefab, new Vector3(
                    Mathf.Clamp(spawn.Offset.x, -size.x * .5f + SpawnInset, size.x * .5f - SpawnInset),
                    spawn.Offset.y,
                    Mathf.Clamp(spawn.Offset.z, -size.y * .5f + SpawnInset, size.y * .5f - SpawnInset)), spawn.Rank));
            return result;
        }
        int count = Mathf.Clamp(MinimumEnemies + depth / 2 + rng.NextInt(-1, 3) +
            (node.Kind == ManualNodeKind.Elite ? 1 : 0), MinimumEnemies, MaximumEnemies);
        var positions = SpawnPositions(size, rng);
        if (node.Kind == ManualNodeKind.Elite)
        {
            var elite = node.Spawns.FirstOrDefault(spawn => spawn.Rank == EnemyRank.Elite);
            if (elite == null) throw new InvalidOperationException("Elite room has no authored Elite enemy.");
            result.Add(new ManualEnemySpawn(elite.Prefab, positions[0], EnemyRank.Elite));
        }
        // Draw a different subset each room and guarantee two active attackers before supports.
        var shuffledFighters = new List<GameObject>(fighters);
        Shuffle(shuffledFighters, rng);
        int supportCount = 0;
        int fighterCount = 0;
        while (result.Count < count)
        {
            GameObject prefab;
            if (fighterCount < Math.Min(3, shuffledFighters.Count))
                prefab = shuffledFighters[fighterCount];
            else prefab = pool[rng.NextInt(0, pool.Count)];
            if (IsSupport(prefab))
            {
                if (supportCount >= 2 || fighterCount < 2) prefab = fighters[rng.NextInt(0, fighters.Count)];
                else supportCount++;
            }
            if (!IsSupport(prefab)) fighterCount++;
            result.Add(new ManualEnemySpawn(prefab, positions[result.Count], EnemyRank.Normal));
        }
        return result;
    }

    private static List<Vector3> SpawnPositions(Vector2 size, SeededRng rng)
    {
        var positions = new List<Vector3>(12);
        float halfX = size.x * .5f - SpawnInset;
        float halfZ = size.y * .5f - SpawnInset;
        for (int row = 0; row < 3; row++)
            for (int column = 0; column < 4; column++)
                positions.Add(new Vector3(-halfX + (column + .5f) * halfX * .5f + (rng.NextFloat() - .5f) * .6f,
                    0f, -halfZ + (row + .5f) * halfZ * (2f / 3f) + (rng.NextFloat() - .5f) * .6f));
        Shuffle(positions, rng);
        return positions;
    }

    private static List<ManualRewardStep> PlanRewards(ManualNodeDefinition node, SeededRng rng, bool opening)
    {
        var result = new List<ManualRewardStep>();
        if (!node.IsEncounter || node.Kind == ManualNodeKind.Boss) return result;
        if (node.Reward == RunRewardKind.Mutation)
        {
            result.Add(new ManualRewardStep(RunRewardKind.Mutation, 1));
            return result;
        }
        if (opening)
        {
            result.Add(new ManualRewardStep(RunRewardKind.Technique, 1));
            result.Add(new ManualRewardStep(RunRewardKind.Protocol, 1));
            return result;
        }
        int count = node.Kind == ManualNodeKind.Elite ? rng.NextInt(2, 4) : rng.NextInt(1, 3);
        RunRewardKind first = rng.Roll(.5) ? RunRewardKind.Technique : RunRewardKind.Protocol;
        for (int i = 0; i < count; i++)
        {
            RunRewardKind kind = i % 2 == 0 ? first : first == RunRewardKind.Technique ? RunRewardKind.Protocol : RunRewardKind.Technique;
            if (result.Count > 0 && result[result.Count - 1].Kind == kind)
            {
                var previous = result[result.Count - 1];
                result[result.Count - 1] = new ManualRewardStep(kind, previous.Quantity + 1);
            }
            else result.Add(new ManualRewardStep(kind, 1));
        }
        return result;
    }

    private static void Shuffle<T>(List<T> values, SeededRng rng)
    {
        for (int i = values.Count - 1; i > 0; i--)
        {
            int chosen = rng.NextInt(0, i + 1);
            T value = values[i]; values[i] = values[chosen]; values[chosen] = value;
        }
    }
}
