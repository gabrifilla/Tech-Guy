using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Scene-owned map and run presentation. Every mechanical action is revalidated by the director.</summary>
[DefaultExecutionOrder(-100)]
public sealed partial class ManualRunUI : MonoBehaviour
{
    [SerializeField] private ProgressionDirector _director;
    [SerializeField] private PlayerActor _player;
    private RunBoons _run;
    private PauseMenuUI _pause;
    private bool _open = true;
    private RunPhase _knownPhase;
    private bool _observedPhase;
    private readonly RunSelectionReleaseGate _release = new RunSelectionReleaseGate();
    private UnityEngine.AI.NavMeshAgent _agent;
    private bool _ownsMovementStop, _previouslyStopped;
    private ManualRouteSnapshot _knownRoute;
    private ManualRouteMapLayout _layout;
    private int _selectedNode = -1;
    private bool _repairUseSelected = true;
    [System.NonSerialized] private GUIStyle _heading, _body, _small, _detailTitle, _button, _center, _nodeLabel, _badge;
    private static readonly Color Background = new Color(.025f, .038f, .06f, .99f);
    private static readonly Color Panel = new Color(.055f, .077f, .11f);
    private static readonly Color Gold = new Color(1f, .77f, .34f);
    private static readonly Color Completed = new Color(.35f, .83f, .69f);
    private static readonly Color Muted = new Color(.44f, .50f, .60f);
    public bool IsOpen => _open;
    public int InspectedNode => _selectedNode;
    public bool CanConfirmRunAbort => _director && _director.CanOpenManualMenu;
    public bool CanAbortRun => _director && _director.ManualSession != null && !_director.ManualSession.IsClosed;
    public bool HasPauseMap => _director && (_director.ManualSession != null || _director.CurrentGraph != null);

    private void BindPauseMap()
    {
        if (_pause || !_player) return;
        _pause = _player.GetComponent<PauseMenuUI>();
        if (_pause) _pause.BindMap(this);
    }

    // GUIStyle holds native GUI state and must be rebuilt after Editor/domain/hot reload.
    // Allocate lazily inside the first OnGUI for this enabled lifecycle, never each frame.
    private void OnEnable() => ResetStyles();
    private void ResetStyles() => _heading = _body = _small = _detailTitle = _button = _center = _nodeLabel = _badge = null;

    private void Awake()
    {
        if (!_director || !_player)
        { Debug.LogError("ManualRunUI requires director and player.", this); enabled = false; }
    }

    private void Update()
    {
        BindPauseMap();
        var session = _director.ManualSession;
        if (session == null) return;
        if (!_run && _player) _run = _player.GetComponent<RunBoons>();
        if (_knownRoute != session.Route)
        { _knownRoute = session.Route; _layout = new ManualRouteMapLayout(session.Route); }
        var keyboard = Keyboard.current; var gamepad = Gamepad.current; var mouse = Mouse.current;
        bool held = mouse != null && mouse.leftButton.isPressed ||
            keyboard != null && (keyboard.enterKey.isPressed || keyboard.digit1Key.isPressed || keyboard.digit2Key.isPressed || keyboard.escapeKey.isPressed || keyboard.mKey.isPressed) ||
            gamepad != null && (gamepad.buttonSouth.isPressed || gamepad.buttonEast.isPressed || gamepad.selectButton.isPressed);
        _release.Observe(Time.frameCount, held);
        if (_pause && _pause.BlocksInput) return;
        if (!_observedPhase || _knownPhase != session.Phase)
        {
            _observedPhase = true; _knownPhase = session.Phase;
            _open = session.Phase == RunPhase.RouteChoice || session.Phase == RunPhase.RepairPending ||
                session.Phase == RunPhase.RewardPending && (!_run || !_run.IsChoosing) || session.IsClosed;
            var destinations = _director.ManualDestinations;
            _selectedNode = destinations.Count > 0 ? destinations[0] : session.PendingDestination >= 0 ? session.PendingDestination : session.CurrentNode;
            _repairUseSelected = _director.ManualRepairAmount > 0;
        }
        if (session.Phase == RunPhase.RewardPending) _open = !_run || !_run.IsChoosing;
        if (session.Phase == RunPhase.RouteChoice && !ManualRouteMapPresentation.CanSelectDestination(session, _selectedNode))
        { var options = _director.ManualDestinations; if (options.Count > 0) _selectedNode = options[0]; }
        if (keyboard != null && keyboard.mKey.wasPressedThisFrame || gamepad != null && gamepad.selectButton.wasPressedThisFrame)
        { TryOpenMap(); return; }
        if (_open && (!_run || !_run.IsChoosing))
        {
            int direction = keyboard != null && (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame) ||
                gamepad != null && (gamepad.dpad.right.wasPressedThisFrame || gamepad.dpad.down.wasPressedThisFrame) ? 1 :
                keyboard != null && (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame) ||
                gamepad != null && (gamepad.dpad.left.wasPressedThisFrame || gamepad.dpad.up.wasPressedThisFrame) ? -1 : 0;
            if (direction != 0)
            { if (session.Phase == RunPhase.RepairPending) _repairUseSelected = !_repairUseSelected; else MoveInspection(direction); }
            if (keyboard != null && keyboard.digit1Key.wasPressedThisFrame)
            { if (session.Phase == RunPhase.RepairPending) TryResolveRepair(true); else TryChooseDestination(0); }
            else if (keyboard != null && keyboard.digit2Key.wasPressedThisFrame)
            { if (session.Phase == RunPhase.RepairPending) TryResolveRepair(false); else TryChooseDestination(1); }
            else if (keyboard != null && keyboard.enterKey.wasPressedThisFrame || gamepad != null && gamepad.buttonSouth.wasPressedThisFrame)
                TryConfirmInspection();
        }
        ApplyInputLock();
    }

    public bool TryAbortRun()
    {
        var session = _director.ManualSession;
        if (session == null || _pause && _pause.BlocksInput || !_director.TryAbortManualRun(session.Id)) return false;
        ReleasedAction();
        return true;
    }

    public bool TryOpenMap()
    {
        BindPauseMap();
        if (!_pause || !HasPauseMap || !_player || _player.IsDead) return false;
        _pause.Pause();
        return _pause.TryShowMap();
    }

    public bool TryInspectNode(int nodeId)
    {
        if ((!_open && (!_pause || !_pause.IsMapVisible)) || _director.ManualSession == null || _director.ManualSession.Route.Node(nodeId) == null) return false;
        _selectedNode = nodeId;
        return true;
    }

    public bool TryConfirmInspection()
    {
        var session = _director.ManualSession;
        if (!_open || !_release.CanConfirm || session == null || _run && _run.IsChoosing) return false;
        if (session.Phase == RunPhase.RepairPending) return TryResolveRepair(_repairUseSelected);
        if (session.Phase == RunPhase.RewardPending)
        {
            // Hand navigation to the reward owner before it snapshots isStopped.
            // Otherwise closing the reward would restore this map's temporary stop.
            ReleaseNavigationStop();
            if (!_director.TryOpenManualReward(session.Id, session.CurrentNode, session.RewardOrdinal))
            { ApplyInputLock(); return false; }
            ReleasedAction(); return true;
        }
        if (!ManualRouteMapPresentation.CanSelectDestination(session, _selectedNode) ||
            !_director.TryCommitDestination(session.Id, _selectedNode)) return false;
        ReleasedAction(); return true;
    }

    public bool TryChooseDestination(int index)
    {
        var session = _director.ManualSession;
        if (!_open || !_release.CanConfirm || session == null || session.Phase != RunPhase.RouteChoice || _run && _run.IsChoosing) return false;
        var destinations = _director.ManualDestinations;
        if (index < 0 || index >= destinations.Count || !_director.TryCommitDestination(session.Id, destinations[index])) return false;
        _selectedNode = destinations[index];
        ReleasedAction(); return true;
    }

    private bool TryResolveRepair(bool use)
    {
        var session = _director.ManualSession;
        if (!_open || !_release.CanConfirm || session == null || session.Phase != RunPhase.RepairPending || _run && _run.IsChoosing ||
            !_director.TryResolveRepair(session.Id, session.CurrentNode, use)) return false;
        ReleasedAction(); return true;
    }

    private void MoveInspection(int direction)
    {
        var destinations = _director.ManualDestinations;
        if (destinations.Count == 0) return;
        int index = 0;
        for (int i = 0; i < destinations.Count; i++) if (destinations[i] == _selectedNode) index = i;
        _selectedNode = destinations[(index + direction + destinations.Count) % destinations.Count];
    }

    private void ApplyInputLock()
    {
        bool locked = _open || !_release.CanConfirm;
        if (_run) _run.SetManualMenuOpen(locked);
        // Reward selection owns navigation while visible. Keep any existing map ownership
        // until its own release, but never overwrite the reward owner's stop state.
        if (_run && _run.IsChoosing) return;
        if (!_agent && _player) _agent = _player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (!_agent || !_agent.isActiveAndEnabled || !_agent.isOnNavMesh) return;
        if (locked)
        {
            if (!_ownsMovementStop) { _previouslyStopped = _agent.isStopped; _ownsMovementStop = true; }
            _agent.isStopped = true;
            // isStopped preserves the path but can retain native crowd-simulation velocity.
            // Hold zero velocity only while this menu owns the stop, then let navigation resume
            // from the untouched destination when our release gate opens.
            _agent.velocity = Vector3.zero;
        }
        else if (_ownsMovementStop) { _agent.isStopped = _previouslyStopped; _ownsMovementStop = false; }
    }

    private void ReleaseNavigationStop()
    {
        if (_ownsMovementStop && _agent && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.isStopped = _previouslyStopped;
        _ownsMovementStop = false;
    }

    private void ReleasedAction()
    {
        _release.RequireRelease(Time.frameCount);
        if (_player && _player.TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
        _open = false;
        ApplyInputLock();
    }

    private void Styles()
    {
        if (_heading != null) return;
        _heading = LabelStyle(30, FontStyle.Bold);
        _body = LabelStyle(16);
        _small = LabelStyle(13);
        _detailTitle = LabelStyle(23, FontStyle.Bold);
        _center = LabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter);
        _nodeLabel = LabelStyle(12, FontStyle.Normal, TextAnchor.UpperCenter);
        _badge = LabelStyle(14, FontStyle.Bold, TextAnchor.MiddleCenter);
        _badge.normal.textColor = new Color(.06f, .08f, .12f);
        _button = new GUIStyle(GUI.skin.button) { fontSize = 15, fontStyle = FontStyle.Bold, wordWrap = true };
    }

    private static GUIStyle LabelStyle(int size, FontStyle weight = FontStyle.Normal, TextAnchor alignment = TextAnchor.UpperLeft)
    {
        var style = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = weight, wordWrap = true, alignment = alignment };
        style.normal.textColor = new Color(.92f, .94f, .98f);
        return style;
    }

    private void OnGUI()
    {
        var session = _director.ManualSession;
        if (session == null || !_open || Time.timeScale <= 0f || _pause && _pause.BlocksInput || _run && _run.IsChoosing) return;
        Styles();
        Matrix4x4 matrix = GUI.matrix; int depth = GUI.depth; bool enabled = GUI.enabled;
        Color background = GUI.backgroundColor;
        float scale = VirtualScale;
        GUI.matrix = Matrix4x4.TRS(new Vector3((Screen.width - 1280f * scale) * .5f, (Screen.height - 720f * scale) * .5f, 0), Quaternion.identity, Vector3.one * scale);
        GUI.depth = -90;
        try { DrawDecision(session); }
        finally { GUI.matrix = matrix; GUI.depth = depth; GUI.enabled = enabled; GUI.backgroundColor = background; }
    }

    private void DrawDecision(RunSessionState session)
    {
        Fill(new Rect(0, 0, 1280, 720), new Color(.005f, .01f, .025f, .66f));
        Fill(new Rect(276, 140, 728, 438), Background);
        Fill(new Rect(276, 140, 728, 3), Gold);
        GUI.Label(new Rect(302, 165, 660, 50), session.Phase == RunPhase.RepairPending ? "Recuperar antes de seguir" :
            session.Phase == RunPhase.RewardPending ? "Recompensa da sala" : session.IsClosed ? "Run encerrada" : "Escolha a próxima passagem", _heading);
        GUI.Label(new Rect(304, 219, 650, 36), "Consulte o mapa e seus upgrades no pause · Esc / M", _small);
        bool allowed = _release.CanConfirm && _director.CanOpenManualMenu;
        if (session.Phase == RunPhase.RouteChoice)
        {
            var destinations = _director.ManualDestinations;
            for (int i = 0; i < destinations.Count; i++)
            {
                int id = destinations[i]; float x = destinations.Count == 1 ? 484 : 306 + i * 340;
                var card = new Rect(x, 277, 312, 160);
                Fill(card, id == _selectedNode ? new Color(.18f, .15f, .08f) : Panel);
                GUI.Label(new Rect(x + 18, 290, 276, 30), "PASSAGEM " + (i + 1) + "  ·  " + RunMapDiscoveryPresentation.NodeLabel(session, id), _center);
                GUI.Label(new Rect(x + 18, 334, 276, 56), RunMapDiscoveryPresentation.RewardSummary(session, id), _body);
                GUI.Label(new Rect(x + 18, 391, 276, 36), ManualRouteMapPresentation.ExcludedByChoice(session.Route, id).Count > 0 ? "Esta passagem fecha o outro caminho." : "Continua a rota principal.", _small);
                if (GUI.Button(card, GUIContent.none, GUIStyle.none)) _selectedNode = id;
            }
            GUI.enabled = allowed && ManualRouteMapPresentation.CanSelectDestination(session, _selectedNode);
            if (GUI.Button(new Rect(410, 465, 460, 42), "Confirmar passagem  ·  Enter / A", _button)) TryConfirmInspection();
            GUI.enabled = true;
            GUI.Label(new Rect(304, 525, 650, 30), "Setas / direcional: selecionar · 1 / 2: confirmar uma passagem", _small);
        }
        else if (session.Phase == RunPhase.RepairPending)
        {
            GUI.Label(new Rect(306, 282, 650, 68), _director.ManualRepairAmount > 0 ? "Recuperação: +" + _director.ManualRepairAmount.ToString("0") + " de vida." : "Sua vida está cheia. Você pode dispensar o reparo.", _detailTitle);
            GUI.enabled = allowed && _director.ManualRepairAmount > 0;
            if (GUI.Button(new Rect(306, 396, 312, 48), (_repairUseSelected ? "› " : "") + "Usar reparo [1]", _button)) TryResolveRepair(true);
            GUI.enabled = allowed;
            if (GUI.Button(new Rect(646, 396, 312, 48), (!_repairUseSelected ? "› " : "") + "Dispensar [2]", _button)) TryResolveRepair(false);
        }
        else if (session.Phase == RunPhase.RewardPending)
        {
            GUI.Label(new Rect(306, 282, 650, 80), "Escolha a recompensa conquistada para continuar.", _body);
            GUI.enabled = allowed;
            if (GUI.Button(new Rect(410, 396, 460, 48), "Escolher recompensa [Enter]", _button)) TryConfirmInspection();
        }
        else
        {
            GUI.Label(new Rect(306, 282, 650, 80), "Resultado: " + session.Outcome + " · retorno: " + _director.ManualReturnStatus, _body);
            GUI.enabled = _director.ManualReturnStatus == ManualReturnState.Failed;
            if (GUI.Button(new Rect(410, 396, 460, 48), "Tentar retornar ao Nexus", _button)) _director.TryRetryManualReturn(session.Id);
        }
    }

    private int DrawRouteMap(RunSessionState session, Rect area, Vector2 pointerOffset)
    {
        if (_layout == null) _layout = new ManualRouteMapLayout(session.Route);
        Fill(new Rect(area.x - 2, area.y - 7, area.width + 4, area.height + 14), new Color(.035f, .053f, .078f));
        foreach (var edge in session.Route.Connections)
        {
            var fromState = session.NodeState(edge.From); var toState = session.NodeState(edge.To);
            bool excluded = fromState == RunNodeState.Excluded || toState == RunNodeState.Excluded;
            bool traversed = fromState == RunNodeState.Completed && (toState == RunNodeState.Completed || toState == RunNodeState.InProgress || toState == RunNodeState.RewardPending);
            bool next = edge.From == session.CurrentNode && (toState == RunNodeState.Available || toState == RunNodeState.Committed);
            Color color = excluded ? new Color(.19f, .22f, .28f) : traversed ? Completed : next ? Gold : new Color(.32f, .38f, .48f);
            var from = MapPoint(area, edge.From); var to = MapPoint(area, edge.To);
            DrawLine(from + Vector2.right * 34f, to - Vector2.right * 34f, color, traversed || next ? 3f : 1.5f, !traversed && !next);
        }
        int currentLocation = FindManualPlayerNode(session);
        int hovered = -1;
        Vector2 pointer = Mouse.current != null ? VirtualPointer(Mouse.current.position.ReadValue()) - pointerOffset : new Vector2(-1f, -1f);
        foreach (int id in _layout.OrderedNodes)
        {
            var node = session.Route.Node(id); var state = session.NodeState(id);
            var point = MapPoint(area, id);
            Rect rect = new Rect(point.x - 34f, point.y - 32f, 68f, 64f);
            bool over = new Rect(rect.x - 5, rect.y - 7, rect.width + 10, rect.height + 38).Contains(pointer);
            if (over) hovered = id;
            bool discovered = RunMapDiscoveryPresentation.IsDiscovered(session, id);
            Color accent = state == RunNodeState.Excluded ? Muted : discovered ? CategoryColor(node) : new Color(.65f, .70f, .78f);
            Color border = id == currentLocation ? new Color(.24f, .86f, 1f) : state == RunNodeState.Available || state == RunNodeState.Committed ? Gold : state == RunNodeState.Completed ? Completed : accent * .6f;
            if (id == _selectedNode || over) Fill(new Rect(rect.x - 4, rect.y - 4, rect.width + 8, rect.height + 8), Color.white * .9f);
            Fill(rect, border);
            Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4), Panel);
            if (discovered)
            {
                DrawIcon(new Rect(rect.x + 21, rect.y + 8, 26, 26), node, accent);
                GUI.Label(new Rect(rect.x, rect.y + 36, rect.width, 24), RunMapDiscoveryPresentation.NodeLabel(session, id), _center);
            }
            else GUI.Label(new Rect(point.x - 9, rect.y + 12, 26, 40), "?", _heading);
            GUI.Label(new Rect(point.x - 44, rect.yMax + 7, 88, 34), RunMapDiscoveryPresentation.RewardAbbreviation(session, id), _nodeLabel);
            if (state == RunNodeState.Excluded)
            { DrawLine(rect.min + Vector2.one * 9, rect.max - Vector2.one * 9, Muted, 2); DrawLine(new Vector2(rect.xMax - 9, rect.y + 9), new Vector2(rect.x + 9, rect.yMax - 9), Muted, 2); }
            if (state == RunNodeState.Available)
            {
                int option = 0;
                foreach (int destination in _director.ManualDestinations) { option++; if (destination == id) break; }
                Fill(new Rect(rect.x - 7, rect.y - 10, 22, 22), Gold);
                GUI.Label(new Rect(rect.x - 7, rect.y - 10, 22, 22), option.ToString(), _badge);
            }
            if (id == currentLocation)
                GUI.Label(new Rect(point.x - 60, rect.y - 27, 120, 22), "VOCÊ ESTÁ AQUI", _nodeLabel);
            if (GUI.Button(new Rect(rect.x - 5, rect.y - 7, rect.width + 10, rect.height + 38), GUIContent.none, GUIStyle.none)) TryInspectNode(id);
        }
        return hovered;
    }

    private Vector2 MapPoint(Rect area, int id)
    {
        Vector2 point = _layout.Point(id);
        return new Vector2(area.x + 40 + point.x * (area.width - 80), area.y + point.y * area.height);
    }

    private void DrawLegend(Rect rect)
    {
        ManualNodeKind[] kinds = { ManualNodeKind.Combat, ManualNodeKind.Combat, ManualNodeKind.Combat, ManualNodeKind.Repair, ManualNodeKind.Elite, ManualNodeKind.Boss };
        RunRewardKind[] rewards = { RunRewardKind.Technique, RunRewardKind.Protocol, RunRewardKind.Mutation, RunRewardKind.None, RunRewardKind.None, RunRewardKind.None };
        string[] names = { "Technique", "Protocol", "Mutation", "Repair", "Elite", "Boss" };
        for (int i = 0; i < names.Length; i++)
        {
            var icon = new Rect(rect.x + i * rect.width / 6, rect.y + 4, 22, 22);
            DrawIcon(icon, kinds[i], rewards[i], IconColor(kinds[i], rewards[i]));
            GUI.Label(new Rect(icon.x + 27, icon.y, 103, 24), names[i], _small);
        }
        GUI.Label(new Rect(rect.x, rect.y + 31, rect.width, 24), "? = sala não visitada. Recompensas anunciadas ajudam a escolher; salas secretas ficam ocultas até a descoberta.", _small);
    }

    private static Color CategoryColor(ManualNodeDefinition node) => IconColor(node.Kind, node.Reward);
    private static Color IconColor(ManualNodeKind kind, RunRewardKind reward) => kind == ManualNodeKind.Repair ? Completed :
        kind == ManualNodeKind.Elite ? new Color(1f, .47f, .29f) : kind == ManualNodeKind.Boss ? new Color(1f, .34f, .43f) :
        reward == RunRewardKind.Mutation ? new Color(.78f, .57f, 1f) : reward == RunRewardKind.Protocol ? new Color(.39f, .76f, 1f) : Gold;
    private static void DrawIcon(Rect rect, ManualNodeDefinition node, Color color) => DrawIcon(rect, node.Kind, node.Reward, color);
    private static void DrawIcon(Rect r, ManualNodeKind kind, RunRewardKind reward, Color color)
    {
        Vector2 c = r.center;
        if (kind == ManualNodeKind.Repair)
        { Fill(new Rect(c.x - 3, r.y + 2, 6, r.height - 4), color); Fill(new Rect(r.x + 2, c.y - 3, r.width - 4, 6), color); }
        else if (kind == ManualNodeKind.Boss)
        {
            DrawLine(new Vector2(r.x + 3, r.yMax - 4), new Vector2(r.x + 1, r.y + 5), color, 2);
            DrawLine(new Vector2(r.x + 1, r.y + 5), new Vector2(c.x - 4, c.y + 2), color, 2);
            DrawLine(new Vector2(c.x - 4, c.y + 2), new Vector2(c.x, r.y + 1), color, 2);
            DrawLine(new Vector2(c.x, r.y + 1), new Vector2(c.x + 4, c.y + 2), color, 2);
            DrawLine(new Vector2(c.x + 4, c.y + 2), new Vector2(r.xMax - 1, r.y + 5), color, 2);
            DrawLine(new Vector2(r.xMax - 1, r.y + 5), new Vector2(r.xMax - 3, r.yMax - 4), color, 2);
            Fill(new Rect(r.x + 3, r.yMax - 4, r.width - 6, 3), color);
        }
        else if (kind == ManualNodeKind.Elite)
        {
            for (int i = 0; i < 2; i++)
            { DrawLine(new Vector2(r.x + 2, r.y + 9 + i * 9), new Vector2(c.x, r.y + 2 + i * 9), color, 3); DrawLine(new Vector2(c.x, r.y + 2 + i * 9), new Vector2(r.xMax - 2, r.y + 9 + i * 9), color, 3); }
        }
        else if (reward == RunRewardKind.Protocol)
        {
            DrawLine(new Vector2(r.x + 4, c.y), new Vector2(r.xMax - 4, c.y), color, 2);
            DrawLine(new Vector2(c.x, r.y + 4), new Vector2(c.x, r.yMax - 4), color, 2);
            Fill(new Rect(r.x, c.y - 3, 6, 6), color); Fill(new Rect(r.xMax - 6, c.y - 3, 6, 6), color);
            Fill(new Rect(c.x - 3, r.y, 6, 6), color); Fill(new Rect(c.x - 3, r.yMax - 6, 6, 6), color);
        }
        else if (reward == RunRewardKind.Mutation || kind == ManualNodeKind.Start)
        {
            DrawLine(new Vector2(c.x, r.y), new Vector2(r.xMax, c.y), color, 2);
            DrawLine(new Vector2(r.xMax, c.y), new Vector2(c.x, r.yMax), color, 2);
            DrawLine(new Vector2(c.x, r.yMax), new Vector2(r.x, c.y), color, 2);
            DrawLine(new Vector2(r.x, c.y), new Vector2(c.x, r.y), color, 2);
            Fill(new Rect(c.x - 3, c.y - 3, 6, 6), color);
        }
        else
        { DrawLine(new Vector2(r.x + 2, r.y + 2), new Vector2(r.xMax - 2, r.yMax - 2), color, 3); DrawLine(new Vector2(r.xMax - 2, r.y + 2), new Vector2(r.x + 2, r.yMax - 2), color, 3); }
    }

    private static void Fill(Rect rect, Color color)
    { Color previous = GUI.color; GUI.color = color; GUI.DrawTexture(rect, Texture2D.whiteTexture); GUI.color = previous; }
    private static void DrawLine(Vector2 from, Vector2 to, Color color, float width, bool dashed = false)
    {
        Vector2 difference = to - from;
        float length = difference.magnitude;
        if (length <= .01f) return;
        Matrix4x4 previous = GUI.matrix;
        // Compose in virtual-canvas coordinates. RotateAroundPivot mixes its screen pivot with
        // the scaled GUI matrix, scattering icons/edges when the Game view is not 1280x720.
        GUI.matrix = previous * Matrix4x4.TRS(new Vector3(from.x, from.y, 0f),
            Quaternion.Euler(0f, 0f, Mathf.Atan2(difference.y, difference.x) * Mathf.Rad2Deg), Vector3.one);
        if (!dashed) Fill(new Rect(0f, -width * .5f, length, width), color);
        else for (float offset = 0f; offset < length; offset += 12f)
            Fill(new Rect(offset, -width * .5f, Mathf.Min(7f, length - offset), width), color);
        GUI.matrix = previous;
    }
    private static float VirtualScale => Mathf.Max(.1f, Mathf.Min(Screen.width / 1280f, Screen.height / 720f));
    private static Vector2 VirtualPointer(Vector2 screen)
    {
        float scale = VirtualScale;
        return new Vector2((screen.x - (Screen.width - 1280f * scale) * .5f) / scale,
            (Screen.height - screen.y - (Screen.height - 720f * scale) * .5f) / scale);
    }
    private void OnDisable()
    {
        ResetStyles();
        if (_run) _run.SetManualMenuOpen(false);
        if (!_run || !_run.IsChoosing) ReleaseNavigationStop();
        _ownsMovementStop = false;
    }
}
