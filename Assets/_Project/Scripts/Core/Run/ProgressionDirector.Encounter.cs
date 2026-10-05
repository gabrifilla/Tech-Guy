using UnityEngine;

public sealed partial class ProgressionDirector
{
    private bool _manualCleanupPending;
    private RunPhase? _manualDisplayedPhase;
    public event System.Action<RunRewardRequest> ManualEncounterResolved;

    private void LateUpdate()
    {
        if (ManualSession != null && _objective && _manualDisplayedPhase != ManualSession.Phase)
        {
            _manualDisplayedPhase = ManualSession.Phase;
            _objective.text = "MANOPLA / ROTA MANUAL\n" +
                (ManualSession.Phase == RunPhase.ExtractionAvailable ? "Entre no portal para extrair ao Nexus." :
                 ManualSession.IsClosed ? "Resultado: " + ManualSession.Outcome + " · retornando ao Nexus" :
                 ManualSession.Route.Node(ManualSession.CurrentNode).Label + " · " + ManualSession.Phase + " · [M] mapa");
        }
        if (ResolveManualLifecycle()) return;
        // Presentation can wait for confirmed hit-stop/cast recovery; no choice is queued or applied.
        // An explicitly dismissed ticket remains closed until the player asks to reopen it.
        if (ManualSession != null && ManualSession.Phase == RunPhase.RewardPending && _boons && _boons.WantsManualRewardPresentation)
            TryOpenManualReward(ManualSession.Id, ManualSession.CurrentNode, ManualSession.RewardOrdinal);
        if(!_manualCompletionCandidate||_manualCleanupPending||ManualSession==null||ManualSession.IsClosed||
            ManualSession.Phase!=RunPhase.Encounter||_manualActors.Count!=0) return;
        // LateUpdate follows synchronous impact callbacks. Explicit leases also cover scopes kept across a frame.
        if(!_player||_player.IsDead)
        {
            _manualCompletionCandidate=false;
            _manualPolicy.TryEndRun(ManualSession.Id,RunOutcome.Dead,new RunActionContext(false));
            return;
        }
        if(_player.IsResolvingGauntletImpact||_player.HasOpenImpactFeedback||
            (_boons&&_boons.HasConfirmedProtocolBatch)||(_manualHolder&&_manualHolder.IsGamePaused)) return;
        _manualCleanupPending=true;
        if(_manualHolder&&!_manualHolder.TryStopEncounterOffense()) { _manualCleanupPending=false; return; }
        var request=new RunRewardRequest(ManualSession.Id,ManualSession.CurrentNode,0);
        // Automatic resolution acknowledges a confirmed hit-stop without waiting for its cosmetic duration.
        // User confirmations still use ManualContext and remain blocked during that interval.
        bool resolved=_manualPolicy.TryResolveEncounter(request.SessionId,request.NodeId,true,new RunActionContext(true));
        _manualCleanupPending=false;
        if(!resolved) return;
        if(_boons) _boons.CloseProtocolEncounter();
        _manualCompletionCandidate=false;
        ManualSession.IsEncounterClosing=false;
        if(_manualEncounterRoot) { Destroy(_manualEncounterRoot); _manualEncounterRoot=null; }
        _manualEnvironment.ApplyGateState(ManualSession);
        if(ManualSession.Phase==RunPhase.RewardPending)
        {
            if(_boons) _boons.PrepareManualReward(request);
            ManualEncounterResolved?.Invoke(request);
            // An active confirmed hit-stop can defer opening, but cannot lose the pending ticket opportunity.
            TryOpenManualReward(request.SessionId,request.NodeId,request.Ordinal);
        }
    }
}
