using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Read-only look-ahead for the in-game route strip: the player's room, the next few rooms and,
/// at a fork, both branches up to where they reconnect. It never changes the session.
/// Discarded rooms are omitted, so after a choice the strip shows only the chosen path.
/// </summary>
public sealed class RouteStripModel
{
    public const int DefaultHorizon = 3;
    private readonly Dictionary<int, Vector2Int> _cells = new Dictionary<int, Vector2Int>();
    private readonly List<int> _nodes = new List<int>();
    private readonly List<ManualConnectionDefinition> _edges = new List<ManualConnectionDefinition>();

    /// <summary>The room the strip starts from (where the player is or last finished).</summary>
    public int Anchor { get; }
    /// <summary>Anchor first, then by column and lane.</summary>
    public IReadOnlyList<int> Nodes => _nodes;
    public IReadOnlyList<ManualConnectionDefinition> Edges => _edges;
    /// <summary>Highest column index (anchor is column 0).</summary>
    public int Columns { get; }
    /// <summary>x = column (0 = anchor), y = lane (-1 top, 0 middle, +1 bottom).</summary>
    public Vector2Int Cell(int nodeId) => _cells[nodeId];
    public bool Contains(int nodeId) => _cells.ContainsKey(nodeId);

    public RouteStripModel(RunSessionState session, int horizon = DefaultHorizon)
    {
        var route = session.Route;
        Anchor = session.CurrentNode;
        horizon = Mathf.Max(1, horizon);

        // Breadth-first by shortest distance, skipping discarded rooms and anything past the Boss.
        var distance = new Dictionary<int, int> { [Anchor] = 0 };
        var queue = new Queue<int>(); queue.Enqueue(Anchor);
        while (queue.Count > 0)
        {
            int from = queue.Dequeue();
            if (distance[from] >= horizon || from == route.BossNode && from != Anchor) continue;
            foreach (var edge in route.Connections)
            {
                if (edge.From != from || distance.ContainsKey(edge.To) || session.NodeState(edge.To) == RunNodeState.Excluded) continue;
                distance[edge.To] = distance[from] + 1;
                queue.Enqueue(edge.To);
            }
        }

        // Longest-path columns among the visible rooms: a reconnection sits after the longer branch.
        var column = distance.Keys.ToDictionary(id => id, id => id == Anchor ? 0 : 1);
        for (int pass = 0; pass < distance.Count; pass++)
        {
            bool changed = false;
            foreach (var edge in route.Connections)
            {
                if (!distance.ContainsKey(edge.From) || !distance.ContainsKey(edge.To) || edge.To == Anchor) continue;
                if (column[edge.To] >= column[edge.From] + 1) continue;
                column[edge.To] = column[edge.From] + 1; changed = true;
            }
            if (!changed) break;
        }

        // Lanes only split while both sides of a fork are still open.
        var lane = distance.Keys.ToDictionary(id => id, _ => 0);
        foreach (var group in route.Groups)
        {
            bool firstOpen = group.FirstBranch.All(id => session.NodeState(id) != RunNodeState.Excluded);
            bool secondOpen = group.SecondBranch.All(id => session.NodeState(id) != RunNodeState.Excluded);
            if (!firstOpen || !secondOpen) continue;
            foreach (int id in group.FirstBranch) if (lane.ContainsKey(id) && id != Anchor) lane[id] = -1;
            foreach (int id in group.SecondBranch) if (lane.ContainsKey(id) && id != Anchor) lane[id] = 1;
        }

        foreach (int id in distance.Keys) _cells[id] = new Vector2Int(column[id], lane[id]);
        _nodes.AddRange(distance.Keys.OrderBy(id => column[id]).ThenBy(id => lane[id]).ThenBy(id => id));
        _edges.AddRange(route.Connections.Where(e => _cells.ContainsKey(e.From) && _cells.ContainsKey(e.To) && e.To != Anchor));
        Columns = _cells.Count == 0 ? 0 : _cells.Values.Max(c => c.x);
    }

    /// <summary>1-based passage number matching the route-choice keys, or 0 when not a current option.</summary>
    public static int OptionNumber(IReadOnlyList<int> destinations, int nodeId)
    {
        if (destinations == null) return 0;
        for (int i = 0; i < destinations.Count; i++) if (destinations[i] == nodeId) return i + 1;
        return 0;
    }
}
