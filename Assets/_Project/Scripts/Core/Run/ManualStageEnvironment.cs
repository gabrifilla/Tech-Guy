using System;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

/// <summary>Authored scene adapter. No route decisions, geometry generation or runtime bake.</summary>
public sealed partial class ManualStageEnvironment : MonoBehaviour
{
    [SerializeField] private GameObject _geometryRoot;
    [SerializeField] private NavMeshSurface _navigation;
    [SerializeField] private List<ManualNodeBinding> _nodes = new List<ManualNodeBinding>();
    [SerializeField] private List<ManualConnectionBinding> _connections = new List<ManualConnectionBinding>();
    public GameObject GeometryRoot => _geometryRoot;
    public NavMeshSurface Navigation => _navigation;
    public IReadOnlyList<ManualNodeBinding> Nodes => _nodes.AsReadOnly();
    public IReadOnlyList<ManualConnectionBinding> Connections => _connections.AsReadOnly();

    public bool Validate(ManualRouteSnapshot route, out string error)
    {
        error = null;
        if (route == null || !_geometryRoot || !_navigation || !_navigation.navMeshData ||
            !_navigation.transform.IsChildOf(_geometryRoot.transform) || _nodes == null || _connections == null ||
            _nodes.Count != route.Nodes.Count || _connections.Count != route.Connections.Count)
        { error = "Manual geometry, baked navigation or binding count is invalid."; return false; }
        var ids = new HashSet<int>();
        foreach (var node in _nodes)
        {
            if (node == null || !ids.Add(node.NodeId) || route.Node(node.NodeId) == null || !node.Arena ||
                !node.EntryVolume || !node.EntryVolume.isTrigger || !node.RewardAnchor ||
                !node.Arena.IsChildOf(_geometryRoot.transform) || !node.EntryVolume.transform.IsChildOf(node.Arena) ||
                !node.RewardAnchor.IsChildOf(node.Arena) || Vector3.Distance(node.Arena.position, route.Node(node.NodeId).Center) > .01f)
            { error = "Invalid manual node binding."; return false; }
        }
        ids.Clear();
        foreach (var connection in _connections)
        {
            if (connection == null || !ids.Add(connection.ConnectionId) || !connection.Barrier || !connection.FromAnchor || !connection.ToAnchor ||
                !connection.Barrier.transform.IsChildOf(_geometryRoot.transform) || !connection.Barrier.GetComponent<Collider>())
            { error = "Invalid manual connection binding."; return false; }
            bool found = false;
            foreach (var edge in route.Connections) if (edge.Id == connection.ConnectionId) found = true;
            if (!found) { error = "Unknown manual connection binding."; return false; }
        }
        return true;
    }

    public void SetActive(bool active) { if (_geometryRoot) _geometryRoot.SetActive(active); }
    public ManualNodeBinding Node(int id) => _nodes.Find(n => n.NodeId == id);
    public bool ContainsPlayer(int nodeId, Vector3 position)
    {
        var node = Node(nodeId);
        return node != null && node.EntryVolume && node.EntryVolume.bounds.Contains(position);
    }

    public void ApplyGateState(RunSessionState session)
    {
        foreach (var binding in _connections)
        {
            ManualConnectionDefinition edge = null;
            foreach (var candidate in session.Route.Connections) if (candidate.Id == binding.ConnectionId) edge = candidate;
            bool traversal = edge != null && session.Phase == RunPhase.Traversal &&
                edge.From == session.CurrentNode && edge.To == session.PendingDestination;
            bool backtrack = edge != null && session.NodeState(edge.From) == RunNodeState.Completed && session.NodeState(edge.To) == RunNodeState.Completed;
            bool safePhase = session.Phase == RunPhase.RouteChoice || session.Phase == RunPhase.Traversal || session.Phase == RunPhase.ExtractionAvailable;
            bool open = safePhase && (traversal || backtrack);
            if (binding.Barrier.TryGetComponent<EncounterGates>(out var gates)) gates.SetAuthoredOpen(open);
            else binding.Barrier.SetActive(!open); // Minimal fixtures may omit the authored gate adapter.
        }
    }
}
