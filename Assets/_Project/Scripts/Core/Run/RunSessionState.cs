using System;
using System.Collections.Generic;

public enum RunPhase { RouteChoice, Traversal, Encounter, RewardPending, RepairPending, FinalResolution, ExtractionAvailable, Returning, Ended }
public enum RunNodeState { Locked, Available, Committed, InProgress, RewardPending, Completed, Excluded }
public enum RunOutcome { None, Dead, Extracted, Aborted }

/// <summary>Observed world facts for a single synchronous request. Default rejects everything.</summary>
public readonly struct RunActionContext
{
    public bool Alive { get; }
    public bool Paused { get; }
    public bool HitStop { get; }
    public bool Casting { get; }
    public bool Dashing { get; }
    public bool CleanupPending { get; }
    public bool CanConfirm => Alive && !Paused && !HitStop && !Casting && !Dashing && !CleanupPending;
    public RunActionContext(bool alive, bool paused = false, bool hitStop = false, bool casting = false,
        bool dashing = false, bool cleanupPending = false)
    { Alive = alive; Paused = paused; HitStop = hitStop; Casting = casting; Dashing = dashing; CleanupPending = cleanupPending; }
}

public sealed class RunSessionState
{
    private readonly Dictionary<int, RunNodeState> _nodes = new Dictionary<int, RunNodeState>();
    private readonly Dictionary<string, int> _branches = new Dictionary<string, int>();
    public Guid Id { get; } = Guid.NewGuid();
    public ManualRouteSnapshot Route { get; }
    public RunPhase Phase { get; internal set; } = RunPhase.RouteChoice;
    public RunOutcome Outcome { get; internal set; }
    public int CurrentNode { get; internal set; }
    public int PendingDestination { get; internal set; } = -1;
    public int RewardOrdinal { get; internal set; }
    public int MutationAcquisitions { get; internal set; }
    public int ProtocolAcquisitions { get; internal set; }
    public int InventoryRevision { get; internal set; }
    public bool IsClosed => Phase == RunPhase.Returning || Phase == RunPhase.Ended;
    public bool IsEncounterClosing { get; internal set; }
    public RunNodeState NodeState(int id) => _nodes.TryGetValue(id, out var state) ? state : RunNodeState.Locked;
    public bool TryBranch(string group, out int destination) => _branches.TryGetValue(group, out destination);
    internal void SetNode(int id, RunNodeState state) => _nodes[id] = state;
    internal void SetBranch(string group, int destination) => _branches.Add(group, destination);
    internal RunSessionState(ManualRouteSnapshot route)
    {
        Route = route ?? throw new ArgumentNullException(nameof(route)); CurrentNode = route.StartNode;
        foreach (var node in route.Nodes) _nodes.Add(node.Id, RunNodeState.Locked);
        _nodes[CurrentNode] = RunNodeState.Completed;
        foreach (var edge in route.Connections) if (edge.From == CurrentNode) _nodes[edge.To] = RunNodeState.Available;
    }
}
