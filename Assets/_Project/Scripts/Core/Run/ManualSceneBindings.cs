using System;
using UnityEngine;

/// <summary>Scene-owned references, kept separate from the definition asset.</summary>
[Serializable]
public sealed class ManualNodeBinding
{
    [SerializeField] private int _nodeId;
    [SerializeField] private Transform _arena;
    [SerializeField] private BoxCollider _entryVolume;
    [SerializeField] private Transform _rewardAnchor;
    public int NodeId => _nodeId;
    public Transform Arena => _arena;
    public BoxCollider EntryVolume => _entryVolume;
    public Transform RewardAnchor => _rewardAnchor;
}

[Serializable]
public sealed class ManualConnectionBinding
{
    [SerializeField] private int _connectionId;
    [SerializeField] private GameObject _barrier;
    [SerializeField] private Transform _fromAnchor;
    [SerializeField] private Transform _toAnchor;
    public int ConnectionId => _connectionId;
    public GameObject Barrier => _barrier;
    public Transform FromAnchor => _fromAnchor;
    public Transform ToAnchor => _toAnchor;
}
