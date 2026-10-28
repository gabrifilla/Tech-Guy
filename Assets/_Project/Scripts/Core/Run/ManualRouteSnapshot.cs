using System.Collections.Generic;

/// <summary>Validated copies of authoring lists and values. Unity asset references retain their identity.</summary>
public sealed class ManualRouteSnapshot
{
    private readonly Dictionary<int, ManualNodeDefinition> _nodes = new Dictionary<int, ManualNodeDefinition>();
    private readonly List<ManualConnectionDefinition> _connections = new List<ManualConnectionDefinition>();
    private readonly List<ManualExclusiveGroup> _groups = new List<ManualExclusiveGroup>();
    private readonly List<int[]> _paths = new List<int[]>();
    public string DefinitionId { get; }
    public int StartNode { get; }
    public int BossNode { get; }
    // Seed for reward draws is independent of session identity and world/decorative RNG.
    public ulong? RewardSeed { get; }
    public IReadOnlyCollection<ManualNodeDefinition> Nodes => _nodes.Values;
    public IReadOnlyList<ManualConnectionDefinition> Connections => _connections.AsReadOnly();
    public IReadOnlyList<ManualExclusiveGroup> Groups => _groups.AsReadOnly();
    public ManualNodeDefinition Node(int id) => _nodes.TryGetValue(id, out var node) ? node : null;
    public IReadOnlyList<int[]> Paths
    {
        get { var result = new List<int[]>(); foreach (var path in _paths) result.Add((int[])path.Clone()); return result.AsReadOnly(); }
    }
    internal ManualRouteSnapshot(ManualRunDefinition definition, List<int[]> paths)
    {
        DefinitionId = definition.DefinitionId; StartNode = definition.StartNode; BossNode = definition.BossNode;
        foreach (var node in definition.Nodes) _nodes.Add(node.Id, node.Copy());
        foreach (var edge in definition.Connections) _connections.Add(new ManualConnectionDefinition(edge.Id, edge.From, edge.To));
        foreach (var group in definition.Groups) _groups.Add(new ManualExclusiveGroup(group.Id, group.DecisionNode, group.FirstBranch, group.SecondBranch));
        foreach (var path in paths) _paths.Add((int[])path.Clone());
    }

    internal ManualRouteSnapshot(ManualRouteSnapshot source, IEnumerable<ManualNodeDefinition> nodes, ulong? rewardSeed = null)
    {
        DefinitionId = source.DefinitionId; StartNode = source.StartNode; BossNode = source.BossNode;
        RewardSeed = rewardSeed ?? source.RewardSeed;
        foreach (var node in nodes) _nodes.Add(node.Id, node.Copy());
        foreach (var edge in source.Connections) _connections.Add(new ManualConnectionDefinition(edge.Id, edge.From, edge.To));
        foreach (var group in source.Groups) _groups.Add(new ManualExclusiveGroup(group.Id, group.DecisionNode, group.FirstBranch, group.SecondBranch));
        foreach (var path in source.Paths) _paths.Add((int[])path.Clone());
    }
}
