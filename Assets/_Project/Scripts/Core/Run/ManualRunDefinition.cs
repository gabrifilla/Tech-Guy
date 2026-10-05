using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunProgressionMode { ProceduralLegacy = 0, ManualSlice = 1 }
public enum ManualNodeKind { Start, Combat, Elite, Repair, Boss }
public enum RunRewardKind { None, Technique, Protocol, Mutation }

[Serializable]
public sealed class ManualRewardStep
{
    [SerializeField] private RunRewardKind _kind;
    [SerializeField] private int _quantity;
    public RunRewardKind Kind => _kind;
    public int Quantity => _quantity;
    public ManualRewardStep(RunRewardKind kind, int quantity) { _kind = kind; _quantity = quantity; }
}

[Serializable]
public sealed class ManualEnemySpawn
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private Vector3 _offset;
    [SerializeField] private EnemyRank _rank = EnemyRank.Normal;
    public GameObject Prefab => _prefab;
    public Vector3 Offset => _offset;
    public EnemyRank Rank => _rank;
    public ManualEnemySpawn(GameObject prefab, Vector3 offset, EnemyRank rank = EnemyRank.Normal)
    { _prefab = prefab; _offset = offset; _rank = rank; }
}

[Serializable]
public sealed class ManualNodeDefinition
{
    [SerializeField] private int _id;
    [SerializeField] private string _label;
    [SerializeField] private ManualNodeKind _kind;
    [SerializeField] private RunRewardKind _reward;
    [SerializeField] private int _acquisitions;
    [SerializeField] private Vector3 _center;
    [SerializeField] private Vector2 _arenaSize = new Vector2(24, 20);
    [SerializeField] private float _repairFraction;
    [SerializeField] private List<ManualEnemySpawn> _spawns = new List<ManualEnemySpawn>();
    [SerializeField] private List<ManualRewardStep> _rewardSteps = new List<ManualRewardStep>();
    public IReadOnlyList<ManualRewardStep> RewardSteps => _rewardSteps?.AsReadOnly();
    public int TotalAcquisitions
    {
        get { if (_rewardSteps == null || _rewardSteps.Count == 0) return _acquisitions;
            int total = 0; foreach (var step in _rewardSteps) if (step != null) total = checked(total + step.Quantity); return total; }
    }
    public RunRewardKind RewardAt(int ordinal)
    {
        if (ordinal < 0 || ordinal >= TotalAcquisitions) return RunRewardKind.None;
        if (_rewardSteps == null || _rewardSteps.Count == 0) return _reward;
        foreach (var step in _rewardSteps) { if (ordinal < step.Quantity) return step.Kind; ordinal -= step.Quantity; }
        return RunRewardKind.None;
    }
    public void ConfigureRewardSteps(IEnumerable<ManualRewardStep> steps)
    {
        _rewardSteps = new List<ManualRewardStep>();
        foreach (var step in steps) _rewardSteps.Add(step == null ? null : new ManualRewardStep(step.Kind, step.Quantity));
    }
    public int Id => _id;
    public string Label => _label;
    public ManualNodeKind Kind => _kind;
    public RunRewardKind Reward => _reward;
    public int Acquisitions => _acquisitions;
    public Vector3 Center => _center;
    public Vector2 ArenaSize => _arenaSize;
    public float RepairFraction => _repairFraction;
    public IReadOnlyList<ManualEnemySpawn> Spawns => _spawns?.AsReadOnly();
    public bool IsEncounter => Kind == ManualNodeKind.Combat || Kind == ManualNodeKind.Elite || Kind == ManualNodeKind.Boss;
    public ManualNodeDefinition(int id, string label, ManualNodeKind kind, RunRewardKind reward,
        int acquisitions, Vector3 center, Vector2 arenaSize, float repairFraction, IEnumerable<ManualEnemySpawn> spawns)
    {
        _id = id; _label = label; _kind = kind; _reward = reward; _acquisitions = acquisitions;
        _center = center; _arenaSize = arenaSize; _repairFraction = repairFraction;
        _spawns = spawns == null ? new List<ManualEnemySpawn>() : new List<ManualEnemySpawn>(spawns);
    }
    internal ManualNodeDefinition Copy()
    {
        var spawns = new List<ManualEnemySpawn>();
        foreach (var spawn in _spawns) spawns.Add(new ManualEnemySpawn(spawn.Prefab, spawn.Offset, spawn.Rank));
        var copy = new ManualNodeDefinition(Id, Label, Kind, Reward, Acquisitions, Center, ArenaSize, RepairFraction, spawns);
        if (_rewardSteps != null) copy.ConfigureRewardSteps(_rewardSteps);
        return copy;
    }
}

[Serializable]
public sealed class ManualConnectionDefinition
{
    [SerializeField] private int _id;
    [SerializeField] private int _from;
    [SerializeField] private int _to;
    public int Id => _id;
    public int From => _from;
    public int To => _to;
    public ManualConnectionDefinition(int id, int from, int to) { _id = id; _from = from; _to = to; }
}

[Serializable]
public sealed class ManualExclusiveGroup
{
    [SerializeField] private string _id;
    [SerializeField] private int _decisionNode;
    [SerializeField] private List<int> _firstBranch = new List<int>();
    [SerializeField] private List<int> _secondBranch = new List<int>();
    public string Id => _id;
    public int DecisionNode => _decisionNode;
    public IReadOnlyList<int> FirstBranch => _firstBranch?.AsReadOnly();
    public IReadOnlyList<int> SecondBranch => _secondBranch?.AsReadOnly();
    public ManualExclusiveGroup(string id, int decisionNode, IEnumerable<int> first, IEnumerable<int> second)
    { _id = id; _decisionNode = decisionNode; _firstBranch = new List<int>(first); _secondBranch = new List<int>(second); }
}

[CreateAssetMenu(menuName = "Tech Guy/Run/Manual Run Definition")]
public sealed class ManualRunDefinition : ScriptableObject
{
    [SerializeField] private int _version = 1;
    [SerializeField] private string _definitionId;
    [SerializeField] private RunWeaponFamily _family = RunWeaponFamily.Gauntlet;
    [SerializeField] private int _startNode;
    [SerializeField] private int _bossNode;
    [SerializeField] private List<ManualNodeDefinition> _nodes = new List<ManualNodeDefinition>();
    [SerializeField] private List<ManualConnectionDefinition> _connections = new List<ManualConnectionDefinition>();
    [SerializeField] private List<ManualExclusiveGroup> _groups = new List<ManualExclusiveGroup>();
    [SerializeField] private ProtocolCatalogDefinition _protocolCatalog;
    public ProtocolCatalogDefinition ProtocolCatalog => _protocolCatalog;
    public void ConfigureProtocolCatalog(ProtocolCatalogDefinition catalog) => _protocolCatalog = catalog;
    public int Version => _version;
    public string DefinitionId => _definitionId;
    public RunWeaponFamily Family => _family;
    public int StartNode => _startNode;
    public int BossNode => _bossNode;
    public IReadOnlyList<ManualNodeDefinition> Nodes => _nodes?.AsReadOnly();
    public IReadOnlyList<ManualConnectionDefinition> Connections => _connections?.AsReadOnly();
    public IReadOnlyList<ManualExclusiveGroup> Groups => _groups?.AsReadOnly();

    // Explicit authoring API for builders and isolated fixtures; never used by OnValidate.
    public void Configure(string id, int start, int boss, IEnumerable<ManualNodeDefinition> nodes,
        IEnumerable<ManualConnectionDefinition> connections, IEnumerable<ManualExclusiveGroup> groups)
    {
        _definitionId = id; _startNode = start; _bossNode = boss;
        _nodes = new List<ManualNodeDefinition>(nodes);
        _connections = new List<ManualConnectionDefinition>(connections);
        _groups = new List<ManualExclusiveGroup>(groups);
    }

    private void OnValidate()
    {
        if (!ManualRouteValidator.Validate(this, out _, out string error))
            Debug.LogWarning("Manual run authoring '" + _definitionId + "': " + error, this);
    }
}
