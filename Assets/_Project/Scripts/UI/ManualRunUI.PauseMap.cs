using System.Collections.Generic;
using UnityEngine;

public sealed partial class ManualRunUI
{
    private RoomGraph _pauseLegacyGraph;
    private int _pauseVisibleRoomCount = -1, _pauseLegacyInspection = -1;
    private readonly Dictionary<int, Vector2> _pauseLegacyPoints = new Dictionary<int, Vector2>();
    private readonly Dictionary<int, Room> _pauseLegacyRooms = new Dictionary<int, Room>();

    public void DrawPauseMap(Rect area)
    {
        if (!_pause || !_pause.IsMapVisible || !HasPauseMap) return;
        Styles();
        // Keep graph coordinates on the pause canvas. A nested GUI group applies its
        // clipping origin after matrix rotation, displacing diagonal edges and icons.
        var session = _director.ManualSession;
        if (session == null) { DrawLegacyPauseMap(area); return; }
        if (_knownRoute != session.Route || _layout == null)
        { _knownRoute = session.Route; _layout = new ManualRouteMapLayout(session.Route); }
        if (session.Route.Node(_selectedNode) == null) _selectedNode = session.CurrentNode;
        float graphWidth = area.width - 338f;
        GUI.Label(new Rect(area.x, area.y, graphWidth, 32), "RECOMPENSAS ANUNCIADAS · SALAS REVELADAS AO ENTRAR", _small);
        int hovered = DrawRouteMap(session, new Rect(area.x, area.y + 50, graphWidth, area.height - 172), Vector2.zero);
        DrawLegend(new Rect(area.x, area.yMax - 107, graphWidth, 67));
        GUI.Label(new Rect(area.x, area.yMax - 37, graphWidth, 34), "Verde: concluída · Azul: localização atual · Dourado: passagem disponível · X: caminho descartado", _small);
        DrawPauseRoomDetails(session, hovered >= 0 ? hovered : _selectedNode,
            new Rect(area.x + graphWidth + 18, area.y, 320, area.height));
    }

    private int FindManualPlayerNode(RunSessionState session)
    {
        if (!_player) return session.CurrentNode;
        Vector3 position = _player.transform.position;
        foreach (var node in session.Route.Nodes)
        {
            Vector3 offset = position - node.Center;
            if (Mathf.Abs(offset.x) <= node.ArenaSize.x * .5f && Mathf.Abs(offset.z) <= node.ArenaSize.y * .5f) return node.Id;
        }
        return -1;
    }

    private void DrawPauseRoomDetails(RunSessionState session, int id, Rect panel)
    {
        var node = session.Route.Node(id) ?? session.Route.Node(session.CurrentNode);
        bool discovered = RunMapDiscoveryPresentation.IsDiscovered(session, node.Id);
        Fill(panel, Panel);
        Fill(new Rect(panel.x, panel.y, panel.width, 3), new Color(.24f, .86f, 1f));
        float x = panel.x + 18, width = panel.width - 36;
        GUI.Label(new Rect(x, panel.y + 20, width, 26), RunMapDiscoveryPresentation.StateLabel(session, node.Id), _small);
        GUI.Label(new Rect(x, panel.y + 54, width, 52), RunMapDiscoveryPresentation.Title(session, node.Id), _detailTitle);
        GUI.Label(new Rect(x, panel.y + 113, width, 24), "RECOMPENSA AO CONCLUIR", _small);
        GUI.Label(new Rect(x, panel.y + 141, width, 68), RunMapDiscoveryPresentation.RewardSummary(session, node.Id), _body);
        GUI.Label(new Rect(x, panel.y + 218, width, 66), discovered ? RunMapDiscoveryPresentation.EncounterSummary(session, node.Id) : "Conteúdo e inimigos: ?", _small);
        GUI.Label(new Rect(x, panel.y + 291, width, 73), RunMapDiscoveryPresentation.OpportunityCost(session, node.Id), _small);
        string next = RunMapDiscoveryPresentation.NextSteps(session, node.Id);
        GUI.Label(new Rect(x, panel.y + 370, width, 62), string.IsNullOrEmpty(next) ? "Fim deste percurso." : "DEPOIS DESTA SALA\n" + next, _small);
        GUI.Label(new Rect(x, panel.yMax - 40, width, 34), "Consulta durante a pausa. Nenhuma passagem é confirmada aqui.", _small);
    }

    private void DrawLegacyPauseMap(Rect area)
    {
        var graph = _director.CurrentGraph;
        if (graph == null) return;
        int visibleCount = 0;
        foreach (var room in graph.Rooms) if (RunMapDiscoveryPresentation.IsVisible(room)) visibleCount++;
        if (_pauseLegacyGraph != graph || _pauseVisibleRoomCount != visibleCount)
        {
            _pauseLegacyGraph = graph; _pauseVisibleRoomCount = visibleCount;
            _pauseLegacyPoints.Clear(); _pauseLegacyRooms.Clear();
            Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity), maximum = -minimum;
            foreach (var room in graph.Rooms)
            {
                if (!RunMapDiscoveryPresentation.IsVisible(room)) continue;
                var center = new Vector2(room.Center.x, room.Center.y);
                minimum = Vector2.Min(minimum, center); maximum = Vector2.Max(maximum, center);
                _pauseLegacyRooms[room.Id] = room;
            }
            foreach (var pair in _pauseLegacyRooms)
            {
                Vector2 center = new Vector2(pair.Value.Center.x, pair.Value.Center.y);
                _pauseLegacyPoints[pair.Key] = new Vector2(maximum.x - minimum.x > .01f ? (center.x - minimum.x) / (maximum.x - minimum.x) : .5f,
                    maximum.y - minimum.y > .01f ? 1f - (center.y - minimum.y) / (maximum.y - minimum.y) : .5f);
            }
        }
        float graphWidth = area.width - 338;
        var canvas = new Rect(area.x, area.y + 45, graphWidth, area.height - 113);
        Fill(canvas, new Color(.035f, .053f, .078f));
        GUI.Label(new Rect(area.x, area.y, graphWidth, 34), "? = sala não visitada · Segredos aparecem após a descoberta", _small);
        foreach (var edge in graph.Connections)
        {
            if (edge.Hidden && !edge.Open) continue;
            if (!_pauseLegacyRooms.TryGetValue(edge.RoomAId, out var first) || !_pauseLegacyRooms.TryGetValue(edge.RoomBId, out var second)) continue;
            if (!RunMapDiscoveryPresentation.IsConnectionVisible(first, second)) continue;
            DrawLine(LegacyPoint(canvas, first.Id), LegacyPoint(canvas, second.Id), edge.Open ? Completed : Muted * .65f, 2, !edge.Open);
        }
        int current = _director.CurrentMapRoomId;
        if (!_pauseLegacyRooms.ContainsKey(_pauseLegacyInspection))
            _pauseLegacyInspection = _pauseLegacyRooms.ContainsKey(current) ? current : graph.StartRoomId;
        foreach (var pair in _pauseLegacyRooms)
        {
            var room = pair.Value; Vector2 point = LegacyPoint(canvas, room.Id);
            var tile = new Rect(point.x - 37, point.y - 28, 74, 56);
            Color border = room.Id == current ? new Color(.24f, .86f, 1) : room.Cleared ? Completed : Muted;
            if (room.Id == _pauseLegacyInspection) Fill(new Rect(tile.x - 3, tile.y - 3, tile.width + 6, tile.height + 6), Color.white);
            Fill(tile, border); Fill(new Rect(tile.x + 2, tile.y + 2, tile.width - 4, tile.height - 4), Panel);
            GUI.Label(new Rect(tile.x + 2, tile.y + 8, tile.width - 4, 44), RunMapDiscoveryPresentation.NodeLabel(room), _center);
            if (GUI.Button(tile, GUIContent.none, GUIStyle.none)) _pauseLegacyInspection = room.Id;
        }
        GUI.Label(new Rect(area.x, area.yMax - 53, graphWidth, 48), "Verde: concluída · Azul: você está aqui · Selecione uma sala para consultar.\nAs portas e os encontros mantêm seu estado durante a consulta.", _small);
        var selected = _pauseLegacyRooms.ContainsKey(_pauseLegacyInspection) ? _pauseLegacyRooms[_pauseLegacyInspection] : null;
        if (selected == null) return;
        var panel = new Rect(area.x + graphWidth + 18, area.y, 320, area.height);
        Fill(panel, Panel); Fill(new Rect(panel.x, panel.y, panel.width, 3), new Color(.24f, .86f, 1));
        GUI.Label(new Rect(panel.x + 18, panel.y + 22, 284, 40), RunMapDiscoveryPresentation.StateLabel(selected), _small);
        GUI.Label(new Rect(panel.x + 18, panel.y + 63, 284, 65), RunMapDiscoveryPresentation.NodeLabel(selected), _detailTitle);
        GUI.Label(new Rect(panel.x + 18, panel.y + 150, 284, 135), RunMapDiscoveryPresentation.IsDiscovered(selected) ?
            "Sala explorada. O mapa registra suas visitas e encontros concluídos." : "O conteúdo desta sala será revelado quando você entrar.", _body);
        GUI.Label(new Rect(panel.x + 18, panel.y + 321, 284, 104), "Consulta do setor procedural. Os benefícios são definidos pela recompensa efetivamente oferecida na sala.", _small);
    }

    private Vector2 LegacyPoint(Rect canvas, int id)
    {
        Vector2 p = _pauseLegacyPoints[id];
        return new Vector2(canvas.x + 49 + p.x * (canvas.width - 98), canvas.y + 45 + p.y * (canvas.height - 90));
    }
}
