using UnityEngine;

/// <summary>
/// Route strip at the top of the screen while choosing the next passage: the player's room,
/// the next rooms and, at a fork, both branches with their announced rewards up to where they
/// reconnect. Read-only; the full map stays in the pause menu.
/// </summary>
public sealed partial class ManualRunUI
{
    private const float StripColumn = 74f, StripLane = 30f, StripNode = 13f;
    private static readonly Color Here = new Color(.24f, .86f, 1f);
    private RouteStripModel _strip;
    private int _stripRevision = -1;
    private RunSessionState _stripSession;
    [System.NonSerialized] private GUIStyle _stripLabel, _stripBadge;

    public RouteStripModel RouteStrip => _strip;

    // Only while choosing the next passage (after a room is finished), not during combat.
    private bool ShouldDrawRouteStrip(RunSessionState session) =>
        session != null && !session.IsClosed && session.Phase == RunPhase.RouteChoice && !(_run && _run.IsChoosing);

    private RouteStripModel CurrentStrip(RunSessionState session)
    {
        // Rebuild only when the route state changes (choice, entry, completion).
        int revision = session.CurrentNode * 7919 + session.PendingDestination * 31 + (int)session.Phase;
        foreach (var node in session.Route.Nodes) revision = revision * 17 + (int)session.NodeState(node.Id);
        if (_strip == null || _stripSession != session || revision != _stripRevision)
        { _strip = new RouteStripModel(session); _stripSession = session; _stripRevision = revision; }
        return _strip;
    }

    private void StripStyles()
    {
        if (_stripLabel != null) return;
        _stripLabel = LabelStyle(10, FontStyle.Bold, TextAnchor.UpperCenter);
        _stripLabel.wordWrap = false;
        _stripBadge = LabelStyle(11, FontStyle.Bold, TextAnchor.MiddleCenter);
        _stripBadge.normal.textColor = new Color(.06f, .08f, .12f);
    }

    private void DrawRouteStrip(RunSessionState session)
    {
        StripStyles();
        var strip = CurrentStrip(session);
        if (strip.Nodes.Count <= 1) return;
        // 40 px side padding fits the 72 px reward labels centred under the first/last node.
        float width = 80f + strip.Columns * StripColumn;
        // Top-left: door markers often project onto the top centre (doors ahead of the camera).
        var panel = new Rect(12f, 10f, width, 118f);
        Fill(panel, new Color(.02f, .03f, .05f, .78f));
        Fill(new Rect(panel.x, panel.y, panel.width, 2), Gold * new Color(1, 1, 1, .8f));
        Vector2 origin = new Vector2(panel.x + 40f, panel.y + 54f);
        var hint = new Rect(panel.x, panel.yMax + 4f, Mathf.Max(220f, panel.width), 18f);
        Fill(hint, new Color(.02f, .03f, .05f, .78f));
        GUI.Label(hint, "Encoste em uma porta para seguir", _stripLabel);
        Vector2 At(int id) { var cell = strip.Cell(id); return origin + new Vector2(cell.x * StripColumn, cell.y * StripLane); }

        float pulse = .65f + .35f * Mathf.PingPong(Time.unscaledTime * 1.6f, 1f);
        foreach (var edge in strip.Edges)
        {
            var toState = session.NodeState(edge.To);
            bool next = edge.From == strip.Anchor && (toState == RunNodeState.Available || toState == RunNodeState.Committed);
            bool walked = toState == RunNodeState.InProgress || toState == RunNodeState.RewardPending || toState == RunNodeState.Completed;
            Color color = walked ? Completed : next ? Gold * new Color(1, 1, 1, pulse) : new Color(.55f, .6f, .7f, .7f);
            Vector2 from = At(edge.From), to = At(edge.To), direction = (to - from).normalized;
            Vector2 start = from + direction * (StripNode + 4f), end = to - direction * (StripNode + 5f);
            DrawLine(start, end, color, next || walked ? 2.5f : 1.5f, !next && !walked);
            // Arrow head shows the direction of travel.
            Vector2 side = new Vector2(-direction.y, direction.x);
            DrawLine(end, end - direction * 6f + side * 4f, color, 2f);
            DrawLine(end, end - direction * 6f - side * 4f, color, 2f);
        }

        var destinations = session.Phase == RunPhase.RouteChoice ? _director.ManualDestinations : null;
        foreach (int id in strip.Nodes)
        {
            var node = session.Route.Node(id); var state = session.NodeState(id);
            Vector2 center = At(id);
            if (id == strip.Anchor)
            {
                FillDiamond(center, StripNode + 4f, Here);
                FillDiamond(center, StripNode + 1f, Panel);
                // Simple "player" glyph: head + shoulders.
                Fill(new Rect(center.x - 3, center.y - 8, 6, 6), Here);
                Fill(new Rect(center.x - 7, center.y, 14, 6), Here);
                GUI.Label(new Rect(center.x - 30, center.y + 17, 60, 14), "VOCÊ", _stripLabel);
                continue;
            }
            bool option = state == RunNodeState.Available && destinations != null && RouteStripModel.OptionNumber(destinations, id) > 0;
            Color border = state == RunNodeState.Committed ? Gold : option ? Gold * new Color(1, 1, 1, pulse) :
                state == RunNodeState.InProgress || state == RunNodeState.RewardPending ? Here :
                state == RunNodeState.Completed ? Completed : new Color(.45f, .5f, .6f);
            Color icon = IconColor(node.Kind, node.Reward);
            FillDiamond(center, StripNode + 2f, border);
            FillDiamond(center, StripNode, Panel);
            DrawIcon(new Rect(center.x - 7, center.y - 7, 14, 14), node, icon);
            GUI.Label(new Rect(center.x - 36, center.y + 14, 72, 14), RunMapDiscoveryPresentation.RewardAbbreviation(session, id), _stripLabel);
            if (option)
            {
                var badge = new Rect(center.x - StripNode - 9, center.y - StripNode - 4, 15, 15);
                Fill(badge, Gold);
                GUI.Label(badge, RouteStripModel.OptionNumber(destinations, id).ToString(), _stripBadge);
            }
        }
    }

    private static void FillDiamond(Vector2 center, float radius, Color color)
    {
        Matrix4x4 previous = GUI.matrix;
        // Same virtual-canvas composition as DrawLine: rotate around the node centre.
        GUI.matrix = previous * Matrix4x4.TRS(new Vector3(center.x, center.y, 0f), Quaternion.Euler(0f, 0f, 45f), Vector3.one);
        float side = radius * 1.41421356f;
        Fill(new Rect(-side * .5f, -side * .5f, side, side), color);
        GUI.matrix = previous;
    }
}
