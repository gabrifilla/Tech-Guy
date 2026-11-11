using System.Collections.Generic;
using UnityEngine;

/// <summary>A door of the current room that leads to a route node.</summary>
public readonly struct RouteDoor
{
    public readonly int NodeId;
    public readonly Bounds Bounds;
    public RouteDoor(int nodeId, Bounds bounds) { NodeId = nodeId; Bounds = bounds; }
}

/// <summary>Pure door-touch rules, kept apart from the scene so they can be tested.</summary>
public static class RouteDoorTouch
{
    public const float TouchDistance = 1.1f, ArmDistance = 2.2f;

    /// <summary>Horizontal distance from a position to the door volume (height ignored).</summary>
    public static float Distance(Bounds door, Vector3 position)
    {
        position.y = door.center.y;
        return Mathf.Sqrt(door.SqrDistance(position));
    }

    /// <summary>Nearest door, or -1 when there is none.</summary>
    public static int Nearest(IReadOnlyList<RouteDoor> doors, Vector3 position, out float distance)
    {
        int best = -1; distance = float.PositiveInfinity;
        for (int i = 0; i < doors.Count; i++)
        {
            float d = Distance(doors[i].Bounds, position);
            if (d < distance) { distance = d; best = i; }
        }
        return best;
    }
}

/// <summary>
/// Route choice without a modal: every door of the finished room shows the reward of the room
/// behind it, and walking into an available door confirms that passage through the director.
/// A touch only counts after the player has been away from all doors once in this choice,
/// so standing next to a door when the choice opens never commits it.
/// </summary>
public sealed partial class ManualRunUI
{
    private readonly List<RouteDoor> _doors = new List<RouteDoor>();
    private RunSessionState _doorSession;
    private int _doorNode = -1;
    private bool _doorArmed;
    public IReadOnlyList<RouteDoor> RouteDoors => _doors;
    public bool RouteDoorsArmed => _doorArmed;

    private void RefreshDoors(RunSessionState session)
    {
        if (_doorSession == session && _doorNode == session.CurrentNode) return;
        _doorSession = session; _doorNode = session.CurrentNode; _doors.Clear();
        var environment = _director.ManualEnvironment;
        if (!environment) return;
        foreach (var binding in environment.Connections)
        {
            ManualConnectionDefinition edge = null;
            foreach (var candidate in session.Route.Connections) if (candidate.Id == binding.ConnectionId) edge = candidate;
            if (edge == null || edge.From != session.CurrentNode || !binding.Barrier) continue;
            var collider = binding.Barrier.GetComponent<Collider>();
            if (collider) _doors.Add(new RouteDoor(edge.To, collider.bounds));
        }
    }

    private void UpdateDoors(RunSessionState session)
    {
        if (session.Phase != RunPhase.RouteChoice || !_player || _player.IsDead || _run && _run.IsChoosing)
        { _doorArmed = false; return; }
        RefreshDoors(session);
        int index = RouteDoorTouch.Nearest(_doors, _player.transform.position, out float distance);
        if (index < 0) return;
        if (!_doorArmed) { _doorArmed = distance > RouteDoorTouch.ArmDistance; return; }
        if (distance > RouteDoorTouch.TouchDistance) return;
        int node = _doors[index].NodeId;
        if (!ManualRouteMapPresentation.CanSelectDestination(session, node)) return;
        // The director revalidates (dash, hit-stop, pause); a refused touch simply retries next frame.
        if (_director.TryCommitDestination(session.Id, node)) { _selectedNode = node; _doorArmed = false; }
    }

    private void DrawDoorMarkers(RunSessionState session)
    {
        var camera = Camera.main;
        if (!camera || session.Phase != RunPhase.RouteChoice) return;
        RefreshDoors(session);
        StripStyles();
        float pulse = .65f + .35f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f);
        var destinations = _director.ManualDestinations;
        foreach (var door in _doors)
        {
            var state = session.NodeState(door.NodeId);
            if (state == RunNodeState.Excluded || state == RunNodeState.Completed) continue;
            // ~1 m above the floor: the gate centre (3 m) projects off the top edge for far-wall doors.
            Vector3 anchor = new Vector3(door.Bounds.center.x, door.Bounds.min.y + 1f, door.Bounds.center.z);
            Vector3 screen = camera.WorldToScreenPoint(anchor);
            if (screen.z <= 0f) continue;
            Vector2 center = VirtualPointer(screen);
            var node = session.Route.Node(door.NodeId);
            bool selectable = ManualRouteMapPresentation.CanSelectDestination(session, door.NodeId);
            Color border = selectable ? Gold * new Color(1, 1, 1, pulse) : new Color(1f, .34f, .43f);
            FillDiamond(center, 22f, new Color(.02f, .03f, .05f, .7f));
            FillDiamond(center, 19f, border);
            FillDiamond(center, 16f, Panel);
            DrawIcon(new Rect(center.x - 9, center.y - 9, 18, 18), node, IconColor(node.Kind, node.Reward));
            string label = RunMapDiscoveryPresentation.RewardAbbreviation(session, door.NodeId);
            if (!selectable && door.NodeId == session.Route.BossNode) label = "BLOQUEADO · PRECISA DE MUTATION";
            var labelRect = new Rect(center.x - 90, center.y + 22, 180, 16);
            Fill(new Rect(center.x - label.Length * 3.6f - 6, labelRect.y, label.Length * 7.2f + 12, 16), new Color(.02f, .03f, .05f, .7f));
            GUI.Label(labelRect, label, _stripLabel);
            int option = RouteStripModel.OptionNumber(destinations, door.NodeId);
            if (selectable && option > 0)
            {
                var badge = new Rect(center.x - 30, center.y - 26, 16, 16);
                Fill(badge, Gold);
                GUI.Label(badge, option.ToString(), _stripBadge);
            }
        }
    }
}
