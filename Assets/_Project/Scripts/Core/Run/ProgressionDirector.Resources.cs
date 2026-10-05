using System;
using UnityEngine;

public sealed partial class ProgressionDirector
{
    private RunRepairPolicy _manualRepair;
    private bool _manualResourcesInitialized;
    public RunRepairResult ManualRepairResult => _manualRepair?.Result;
    public float ManualRepairAmount => ManualSession != null && ManualSession.Phase == RunPhase.RepairPending && _player &&
        RunResourceMath.TryRepair(_player.health,_player.maxHealth,ManualSession.Route.Node(ManualSession.CurrentNode).RepairFraction,out float amount) ? amount : 0;
    public event Action<RunRepairResult> ManualRepairResolved;

    private void InitializeManualResourcesOnce()
    {
        if(_manualResourcesInitialized) return;
        _manualResourcesInitialized=true; _manualRepair=new RunRepairPolicy(_manualPolicy);
        _player.RefreshResourceStats(fillToMax:true);
        // Fresh RunBoons weapon clone configures a new gauntlet resource once. No transition calls this path.
        _manualHolder=_player.GetComponent<AbilityHolder>(); _manualHolder.RefreshLoadout();
    }

    public void RebuildManualResourceStats()
    {
        if(ManualSession==null||ManualSession.IsClosed||!_player) return;
        _player.RefreshResourceStatsPreservingAmounts();
    }

    public bool TryResolveRepair(Guid sessionId,int nodeId,bool use)
    {
        if(_manualRepair==null||!_player||!_manualRepair.TryResolve(sessionId,nodeId,use,_player.health,_player.maxHealth,
            ManualContext,amount=>_player.Heal(amount)))
        {
            if(_manualRepair?.Result?.State==RunRepairState.Faulted)
                Debug.LogError("Manual Repair failed; abort instead of retrying: "+_manualRepair.Result.Diagnostic,this);
            return false;
        }
        _manualEnvironment.ApplyGateState(ManualSession);
        if(_manualControls) _manualControls.RequireAttackRelease();
        ManualRepairResolved?.Invoke(_manualRepair.Result); return true;
    }
}
