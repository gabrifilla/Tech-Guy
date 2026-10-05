using System;
using System.Collections;
using UnityEngine;

public sealed partial class ProgressionDirector
{
    private IManualRunSceneLoader _manualSceneLoader;
    private bool _manualTeardownDone;
    private ScenePortal _manualPortal;
    public ScenePortal ManualExtractionPortal => _manualPortal;
    public ManualReturnState ManualReturnStatus { get; private set; }
    public int ManualLoadAttempts { get; private set; }

    private bool ResolveManualLifecycle()
    {
        if (ManualSession == null) return false;
        if (ManualSession.IsClosed)
        {
            if (ManualSession.Phase == RunPhase.Returning && ManualReturnStatus == ManualReturnState.Idle)
                BeginManualReturn();
            return true;
        }
        if (!_player || _player.IsDead)
        {
            if ((_boons && _boons.HasConfirmedProtocolBatch) || !ReferenceEquals(_player, null) && (_player.IsResolvingGauntletImpact || _player.HasOpenImpactFeedback)) return true;
            _manualPolicy.TryEndRun(ManualSession.Id, RunOutcome.Dead, new RunActionContext(false));
            BeginManualReturn(); return true;
        }
        if (ManualSession.Phase != RunPhase.FinalResolution) return false;
        if (_player.IsResolvingGauntletImpact || _player.HasOpenImpactFeedback || (_boons && _boons.HasConfirmedProtocolBatch) || (_manualHolder && _manualHolder.IsGamePaused)) return true;
        if (_manualPolicy.TryFinalizeBoss(ManualSession.Id, new RunActionContext(true)))
        {
            _manualEnvironment.ApplyGateState(ManualSession);
            var id = ManualSession.Id;
            _manualPortal = ScenePortal.Spawn(ManualSession.Route.Node(ManualSession.Route.BossNode).Center + Vector3.forward * 7f,
                _player.transform, _returnScene);
            _manualPortal.transform.SetParent(transform, true);
            _manualPortal.ConfigureEntryGuard(() => ClaimManualExtraction(id), BeginManualReturn);
        }
        return true;
    }

    private bool ClaimManualExtraction(Guid sessionId) => _manualPolicy != null &&
        _manualPolicy.TryRequestExtraction(sessionId, ManualContext());

    private void BeginManualReturn()
    {
        if (ManualSession == null || ManualSession.Phase != RunPhase.Returning || ManualReturnStatus != ManualReturnState.Idle) return;
        TeardownManualRun();
        ManualReturnStatus = ManualReturnState.Loading;
        StartCoroutine(LoadManualNexus(ManualSession.Id, ManualSession.Outcome == RunOutcome.Dead));
    }

    private void TeardownManualRun()
    {
        if (_manualTeardownDone) return;
        _manualTeardownDone = true;
        DisposeManualState();
        if (_manualPortal) { Destroy(_manualPortal.gameObject); _manualPortal = null; }
        if (_manualControls) { _manualControls.StopEncounterOffense(); _manualControls.enabled = false; }
        if (_manualHolder) { _manualHolder.TryStopEncounterOffense(); _manualHolder.enabled = false; }
        if (_boons) _boons.EndManualSession();
        if (_manualEnvironment) _manualEnvironment.ApplyGateState(ManualSession);
    }

    private IEnumerator LoadManualNexus(Guid id, bool deathDelay)
    {
        // Preserve the legacy scaled death delay, including pause semantics.
        if (deathDelay) yield return new WaitForSeconds(DeathReturnDelay);
        if (_manualSceneLoader == null) _manualSceneLoader = new ManualRunSceneLoader();
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (ManualSession == null || ManualSession.Id != id || ManualSession.Phase != RunPhase.Returning) yield break;
            if (!_manualSceneLoader.CanLoad(_returnScene))
            {
                Debug.LogError("ProgressionDirector cannot return to the Nexus: scene '" + _returnScene + "' is missing from Build Settings. Aborting the transition without loading another scene (R8.4).", this);
                break;
            }
            ManualLoadAttempts++;
            bool succeeded = false;
            yield return _manualSceneLoader.Load(_returnScene, result => succeeded = result);
            if (succeeded)
            { ManualReturnStatus = ManualReturnState.Completed; _manualPolicy.TryCompleteReturn(id); yield break; }
            yield return null;
        }
        ManualReturnStatus = ManualReturnState.Failed;
        Debug.LogWarning("Manual return remains pending; retry retains the claimed run result.", this);
    }

    public bool TryRetryManualReturn(Guid id)
    {
        if (ManualSession == null || ManualSession.Id != id || ManualSession.Phase != RunPhase.Returning || ManualReturnStatus != ManualReturnState.Failed) return false;
        ManualReturnStatus = ManualReturnState.Loading;
        StartCoroutine(LoadManualNexus(id, false)); return true;
    }
}
