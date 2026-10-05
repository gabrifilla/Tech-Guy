using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Scene-owned presentation. Every action is validated by the director, never by GUI state.</summary>
[DefaultExecutionOrder(-100)]
public sealed class ManualRunUI : MonoBehaviour
{
    [SerializeField] private ProgressionDirector _director;
    [SerializeField] private PlayerActor _player;
    private RunBoons _run;
    private bool _open = true;
    private RunPhase _knownPhase;
    private bool _observedPhase;
    private readonly RunSelectionReleaseGate _release = new RunSelectionReleaseGate();
    private Vector2 _scroll;
    private UnityEngine.AI.NavMeshAgent _agent;
    private bool _ownsMovementStop, _previouslyStopped;
    public bool IsOpen => _open;

    private void Awake()
    {
        if (!_director || !_player) { Debug.LogError("ManualRunUI requires director and player.",this); enabled=false; }
    }

    private void Update()
    {
        var session=_director.ManualSession;
        if(session==null) return;
        if(!_run && _player) _run=_player.GetComponent<RunBoons>();
        if(!_observedPhase || _knownPhase!=session.Phase)
        {
            _observedPhase=true; _knownPhase=session.Phase;
            _showRoute=false;
            _open=session.Phase==RunPhase.RouteChoice || session.Phase==RunPhase.RepairPending || session.IsClosed;
        }
        bool held=Mouse.current!=null&&Mouse.current.leftButton.isPressed || Keyboard.current!=null&&(Keyboard.current.enterKey.isPressed||Keyboard.current.digit1Key.isPressed||Keyboard.current.digit2Key.isPressed);
        _release.Observe(Time.frameCount,held);
        if(Keyboard.current!=null&&Keyboard.current.mKey.wasPressedThisFrame&&_director.CanOpenManualMenu&&
            (!_run||!_run.IsChoosing)&&session.Phase!=RunPhase.Encounter)
        {
            if(_open) ReleasedAction();
            else _open=true;
        }
        if(_open&&(!_run||!_run.IsChoosing)&&Keyboard.current!=null)
        {
            if(Keyboard.current.digit1Key.wasPressedThisFrame) TryChooseDestination(0);
            else if(Keyboard.current.digit2Key.wasPressedThisFrame) TryChooseDestination(1);
        }
        bool locked=_open || !_release.CanConfirm;
        if(_run) _run.SetManualMenuOpen(locked);
        if(!_agent&&_player) _agent=_player.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if(_agent&&_agent.isActiveAndEnabled&&_agent.isOnNavMesh)
        {
            if(locked)
            {
                if(!_ownsMovementStop) { _previouslyStopped=_agent.isStopped; _ownsMovementStop=true; }
                // Player initialization/warping may restore navigation after this owner acquired its lock.
                _agent.isStopped=true;
            }
            else if(!locked&&_ownsMovementStop) { _agent.isStopped=_previouslyStopped; _ownsMovementStop=false; }
        }
    }

    private void ReleasedAction()
    {
        _release.RequireRelease(Time.frameCount);
        if(_player&&_player.TryGetComponent(out CharControlScript controls)) controls.RequireAttackRelease();
        _open=false;
    }

    private GUIStyle _heading, _body, _small, _cardTitle, _button;
    private bool _showRoute;
    private void Styles()
    {
        if(_heading!=null) return;
        _heading=new GUIStyle(GUI.skin.label) { fontSize=30,fontStyle=FontStyle.Bold,wordWrap=true };
        _body=new GUIStyle(GUI.skin.label) { fontSize=16,wordWrap=true };
        _small=new GUIStyle(_body) { fontSize=13 };
        _cardTitle=new GUIStyle(_heading) { fontSize=24 };
        _button=new GUIStyle(GUI.skin.button) { fontSize=16,fontStyle=FontStyle.Bold };
    }
    private static void Fill(Rect rect,Color color)
    { var previous=GUI.color; GUI.color=color; GUI.DrawTexture(rect,Texture2D.whiteTexture); GUI.color=previous; }
    private void OnGUI()
    {
        var session=_director.ManualSession;
        if(session==null||Time.timeScale<=0f||(_run&&_run.IsChoosing)) return;
        Styles();
        var previous=GUI.matrix; int previousDepth=GUI.depth;
        GUI.depth=-100;
        float scale=Mathf.Max(.1f,Mathf.Min(Screen.width/1280f,Screen.height/720f));
        GUI.matrix=Matrix4x4.Scale(Vector3.one*scale);
        try
        {
            if(!_open)
            {
                Fill(new Rect(22,140,530,56),new Color(.02f,.035f,.06f,.9f));
                string message=session.Phase==RunPhase.Traversal ? "Siga para "+session.Route.Node(session.PendingDestination).Label+" · clique no piso para andar" :
                    session.Phase==RunPhase.Encounter ? (_director.HasManualCompletionCandidate?"Finalizando o último impacto…":"Elimine os adversários · "+_director.ManualEnemiesRemaining+" restantes") :
                    session.Phase==RunPhase.RewardPending ? "Recompensa pendente · [M] para escolher" : "[M] para consultar a rota";
                GUI.Label(new Rect(38,151,498,40),message,_small); return;
            }
            Fill(new Rect(130,65,1020,565),new Color(.025f,.035f,.06f,.99f));
            GUI.Label(new Rect(170,89,820,25),"PRIMEIRO SETOR  /  MANOPLA  /  "+session.Route.Node(session.CurrentNode).Label,_small);
            GUI.Label(new Rect(170,119,900,48),session.Phase==RunPhase.RouteChoice?"Escolha seu próximo destino":session.Phase==RunPhase.RepairPending?"Uma pausa para reparar":session.IsClosed?"Run encerrada":"Seu percurso",_heading);
            GUI.Label(new Rect(170,179,900,42),"Vida: "+(_player?_player.health.ToString("0")+" / "+_player.maxHealth.ToString("0"):"—")+
                "    ·    Mutation adquirida: "+(session.MutationAcquisitions>0?"sim":"ainda não")+"    ·    [M] fecha/reabre",_body);
            bool previousEnabled=GUI.enabled;
            GUI.enabled=_release.CanConfirm&&(_director.CanOpenManualMenu||session.IsClosed);
            if(GUI.Button(new Rect(925,92,180,32),_showRoute?"Ver decisões":"Ver rota completa",_button)) _showRoute=!_showRoute;
            if(_showRoute)
            {
                GUI.Label(new Rect(170,225,900,24),"Verde: passagem aberta · cinza: fechada · salas excluídas permanecem indisponíveis",_small);
                GUILayout.BeginArea(new Rect(170,258,935,260));
                _scroll=GUILayout.BeginScrollView(_scroll); DrawRouteMap(session); GUILayout.EndScrollView(); GUILayout.EndArea();
            }
            else if(session.Phase==RunPhase.RouteChoice)
            {
                var destinations=_director.ManualDestinations;
                for(int i=0;i<destinations.Count;i++) DrawDestination(new Rect(destinations.Count==1?390:170+i*480,232,450,260),destinations[i],i);
                GUI.Label(new Rect(170,506,920,35),"A escolha abre a passagem indicada. Depois, caminhe até a sala. O outro ramo da bifurcação fica indisponível.",_small);
            }
            else if(session.Phase==RunPhase.RepairPending)
            {
                GUI.Label(new Rect(170,248,900,50),"Recuperação agora: +"+_director.ManualRepairAmount.ToString("0")+" HP",_cardTitle);
                GUI.Label(new Rect(170,308,900,70),"Cura 35% da vida máxima, limitada à vida perdida. Não recupera mana nem Asura. Você pode usar ou dispensar esta oportunidade uma vez.",_body);
                GUI.enabled=previousEnabled&&_release.CanConfirm&&_director.CanOpenManualMenu&&_director.ManualRepairAmount>0;
                if(GUI.Button(new Rect(170,411,440,52),"Usar Repair",_button)&&_director.TryResolveRepair(session.Id,session.CurrentNode,true)) ReleasedAction();
                GUI.enabled=previousEnabled&&_release.CanConfirm&&_director.CanOpenManualMenu;
                if(GUI.Button(new Rect(650,411,440,52),"Dispensar e seguir",_button)&&_director.TryResolveRepair(session.Id,session.CurrentNode,false)) ReleasedAction();
            }
            else if(session.Phase==RunPhase.RewardPending)
            {
                GUI.Label(new Rect(170,252,900,80),"Encontro concluído. Sua recompensa continua disponível; escolha uma melhoria para liberar a próxima sala.",_body);
                if(GUI.Button(new Rect(390,390,450,54),"Escolher recompensa",_button)&&_director.TryOpenManualReward(session.Id,session.CurrentNode,session.RewardOrdinal)) ReleasedAction();
            }
            else if(session.IsClosed)
            {
                GUI.Label(new Rect(170,260,900,70),"Resultado: "+session.Outcome+" · retorno: "+_director.ManualReturnStatus,_body);
                if(_director.ManualReturnStatus==ManualReturnState.Failed&&GUI.Button(new Rect(390,390,450,54),"Tentar carregar Nexus novamente",_button)) _director.TryRetryManualReturn(session.Id);
            }
            else GUI.Label(new Rect(170,265,900,100),session.Phase==RunPhase.Traversal?"Passagem aberta para "+session.Route.Node(session.PendingDestination).Label+". Feche a tela e clique no piso para caminhar.":"Acompanhe o percurso pelo mapa. O portal aparece após vencer o Boss.",_body);
            if(!session.IsClosed)
            {
                if(GUI.Button(new Rect(170,557,450,42),"Voltar ao jogo [M]",_button)) ReleasedAction();
                if(GUI.Button(new Rect(650,557,440,42),"Abortar e voltar ao Nexus",_button)&&_director.TryAbortManualRun(session.Id)) ReleasedAction();
            }
            GUI.enabled=previousEnabled;
        }
        finally { GUI.matrix=previous; GUI.depth=previousDepth; }
    }
    public bool TryChooseDestination(int index)
    {
        if(!_open||!_release.CanConfirm||_director.ManualSession==null||_director.ManualSession.Phase!=RunPhase.RouteChoice) return false;
        var destinations=_director.ManualDestinations;
        if(index<0||index>=destinations.Count||!_director.TryCommitDestination(_director.ManualSession.Id,destinations[index])) return false;
        ReleasedAction(); return true;
    }
    private void DrawDestination(Rect rect,int id,int index)
    {
        var node=_director.ManualSession.Route.Node(id);
        Color accent=node.Kind==ManualNodeKind.Repair?new Color(.3f,.9f,.65f):node.Kind==ManualNodeKind.Elite?new Color(1f,.5f,.3f):
            node.Reward==RunRewardKind.Mutation?new Color(.75f,.55f,1f):node.Reward==RunRewardKind.Protocol?new Color(.35f,.75f,1f):new Color(1f,.8f,.35f);
        Fill(rect,new Color(.06f,.08f,.12f)); Fill(new Rect(rect.x,rect.y,rect.width,5),accent);
        GUI.Label(new Rect(rect.x+22,rect.y+20,400,26),"["+(index+1)+"]  "+node.Label+"  ·  "+(node.Kind==ManualNodeKind.Repair?"REPARAÇÃO":node.Kind==ManualNodeKind.Elite?"ELITE":node.Kind==ManualNodeKind.Boss?"BOSS":"COMBATE"),_small);
        string benefit=node.Kind==ManualNodeKind.Repair?"Recuperar vida":node.Kind==ManualNodeKind.Boss?"Enfrentar o Boss":RewardSummary(node);
        GUI.Label(new Rect(rect.x+22,rect.y+56,400,38),benefit,_cardTitle);
        string description=node.Kind==ManualNodeKind.Repair?"Sem combate. Cura até 35% da vida máxima, limitada ao déficit.":
            node.Kind==ManualNodeKind.Boss?"Confronto final. A vitória libera o portal de extração.":node.Reward==RunRewardKind.Technique?"Aprimore golpes, impacto ou cadência da Manopla.":
            node.Reward==RunRewardKind.Mutation?"Transforme uma ação da Manopla. Aquisição necessária para liberar o Boss.":"Efeito universal: fogo, gelo, condução ou detonação.";
        GUI.Label(new Rect(rect.x+22,rect.y+104,400,65),description,_body);
        GUI.Label(new Rect(rect.x+22,rect.y+177,400,38),node.Kind==ManualNodeKind.Elite?"1 Elite + 4 comuns. Depois: combate extra com 2 Protocols.":node.Kind==ManualNodeKind.Repair?"Abre mão da Technique do caminho de combate.":node.Spawns.Count+" adversários nesta sala.",_small);
        if(GUI.Button(new Rect(rect.x+22,rect.y+214,400,34),"Escolher este caminho",_button)) TryChooseDestination(index);
    }
    private void OnDisable()
    {
        if(_run) _run.SetManualMenuOpen(false);
        if(_ownsMovementStop&&_agent&&_agent.isActiveAndEnabled&&_agent.isOnNavMesh) _agent.isStopped=_previouslyStopped;
        _ownsMovementStop=false;
    }
    private static string RewardSummary(ManualNodeDefinition node)
    {
        if(node.RewardSteps==null||node.RewardSteps.Count==0) return node.Acquisitions+" "+node.Reward;
        return string.Join(" + ",System.Linq.Enumerable.Select(node.RewardSteps,s=>s.Quantity+" "+s.Kind));
    }

    private void DrawRouteMap(RunSessionState session)
    {
        Rect area=GUILayoutUtility.GetRect(820,360);
        foreach(var edge in session.Route.Connections)
        {
            Vector2 from=MapPoint(area,session.Route.Node(edge.From).Center),to=MapPoint(area,session.Route.Node(edge.To).Center);
            bool closed=true;
            foreach(var binding in _director.ManualEnvironment.Connections) if(binding.ConnectionId==edge.Id) closed=binding.Barrier.activeSelf;
            var color=GUI.color;
            GUI.color=closed?new Color(.5f,.5f,.5f):new Color(.3f,.85f,.45f);
            // Axis-aligned segments preserve the scroll view's clipping rectangle.
            GUI.DrawTexture(new Rect(Mathf.Min(from.x,to.x),from.y,Mathf.Max(2,Mathf.Abs(to.x-from.x)),2),Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(to.x,Mathf.Min(from.y,to.y),2,Mathf.Max(2,Mathf.Abs(to.y-from.y))),Texture2D.whiteTexture);
            GUI.color=color;
        }
        foreach(var node in session.Route.Nodes)
        {
            var point=MapPoint(area,node.Center); var old=GUI.color;
            GUI.color=session.NodeState(node.Id)==RunNodeState.Excluded?Color.gray:node.Id==session.CurrentNode?new Color(.4f,1f,.65f):Color.white;
            string benefit=node.Kind==ManualNodeKind.Repair?"Repair · "+(node.RepairFraction*100).ToString("0")+"% HP":
                node.Kind==ManualNodeKind.Boss?"Portal após vitória":node.Kind==ManualNodeKind.Start?"Escolha a rota":RewardSummary(node);
            GUI.Box(new Rect(point.x-68,point.y-17,136,34),node.Label+" · "+node.Kind+"\n"+benefit);
            GUI.color=old;
        }
    }

    private static Vector2 MapPoint(Rect area,Vector3 center) =>
        new Vector2(area.x+90+(center.x-460)/80*(area.width-180),area.y+20+center.z/320*320);
}
