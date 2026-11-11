using System;
using System.Collections;
using System.Collections.Generic;
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
            RecordMetaProgressBossVictory(); // idempotent; normally already recorded by the Boss-rank death hook
            StartCoroutine(PresentManualVictory(ManualSession.Id));
        }
        return true;
    }

    // Fase 1a — victory moment: banner first, then the portal. The player walks in whenever they want.
    [SerializeField] private string _victoryBannerText = "UNIVERSO 1 CONCLUÍDO";
    [SerializeField, Min(0f)] private float _victoryBannerSeconds = 2.5f;
    private UniverseVictoryBanner _victoryBanner;
    /// <summary>True between the boss finalization and the portal opening.</summary>
    public bool ManualVictoryPresenting { get; private set; }

    private IEnumerator PresentManualVictory(Guid id)
    {
        ManualVictoryPresenting = true;
        _victoryBanner = UniverseVictoryBanner.Show(transform, _victoryBannerText, _victoryBannerSeconds);
        float elapsed = 0f;
        // Unscaled so the final hit-stop does not stretch the banner; frozen while paused.
        while (elapsed < _victoryBannerSeconds)
        {
            if (!VictoryStillPending(id)) { EndVictoryPresentation(); yield break; }
            if (Time.timeScale > 0f && !(_manualHolder && _manualHolder.IsGamePaused)) elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
        ManualVictoryPresenting = false;
        if (!VictoryStillPending(id)) yield break;
        _manualPortal = ScenePortal.Spawn(ManualSession.Route.Node(ManualSession.Route.BossNode).Center + Vector3.forward * 7f,
            _player.transform, _returnScene);
        _manualPortal.transform.SetParent(transform, true);
        _manualPortal.ConfigureEntryGuard(() => ClaimManualExtraction(id), BeginManualReturn);
    }

    private bool VictoryStillPending(Guid id) => ManualSession != null && ManualSession.Id == id &&
        ManualSession.Phase == RunPhase.ExtractionAvailable && _player && !_player.IsDead;

    private void EndVictoryPresentation()
    {
        ManualVictoryPresenting = false;
        if (_victoryBanner) Destroy(_victoryBanner.gameObject);
        _victoryBanner = null;
    }

    private void OnManualBossDied(Actor boss)
    {
        if (boss) boss.Died -= OnManualBossDied;
        // Final impact: reuse the player's existing hit-stop clock (extends an in-flight stop, never shortens it).
        if (_player && _player.TryGetComponent(out HitStopRunner hitStop)) hitStop.Apply(_victoryHitStopSeconds);
        RecordMetaProgressBossVictory();
        OnManualBossVictoryFragments();
        if (ManualSession != null) StartCoroutine(ClearBossRoom(ManualSession.Id));
    }

    /// <summary>
    /// After the boss death callback chain (next frame), remove what is left of the boss room: spawners stop feeding
    /// it, remaining enemies are removed without a death (no coins, no kill credit; their projectiles cascade on
    /// owner loss) and hazard zones are destroyed. The existing frontier then closes the encounter normally.
    /// </summary>
    private IEnumerator ClearBossRoom(Guid id)
    {
        yield return null;
        if (ManualSession == null || ManualSession.Id != id || ManualSession.IsClosed) yield break;
        DetachManualSpawners();
        _manualActors.RemoveWhere(actor => !actor);
        foreach (var actor in new List<Actor>(_manualActors))
        {
            if (actor.IsDead) continue;
            actor.Died -= OnManualEnemyDied;
            _manualActors.Remove(actor);
            Destroy(actor.gameObject);
        }
        if (_manualEncounterRoot)
            foreach (var zone in _manualEncounterRoot.GetComponentsInChildren<HazardZone>(true)) Destroy(zone.gameObject);
        if (_manualActors.Count == 0 && ManualSession.Phase == RunPhase.Encounter)
        { _manualCompletionCandidate = true; ManualSession.IsEncounterClosing = true; }
    }

    /// <summary>Remaining live enemies of the current encounter (read-only view for tests and tools).</summary>
    public int ManualLiveEnemyCount { get { int count = 0; foreach (var actor in _manualActors) if (actor && !actor.IsDead) count++; return count; } }
    [SerializeField, Range(0f, 1f)] private float _victoryHitStopSeconds = .3f;

    private bool ClaimManualExtraction(Guid sessionId) => _manualPolicy != null &&
        _manualPolicy.TryRequestExtraction(sessionId, ManualContext());

    private void BeginManualReturn()
    {
        if (ManualSession == null || ManualSession.Phase != RunPhase.Returning || ManualReturnStatus != ManualReturnState.Idle) return;
        TeardownManualRun();
        RunResultsHandoff.Expect(ManualSession.Id.ToString());
        ManualReturnStatus = ManualReturnState.Loading;
        StartCoroutine(LoadManualNexus(ManualSession.Id, ManualSession.Outcome == RunOutcome.Dead));
    }

    private void TeardownManualRun()
    {
        if (_manualTeardownDone) return;
        _manualTeardownDone = true;
        FinishRunStatistics();
        DisposeManualState();
        if (_manualPortal) { Destroy(_manualPortal.gameObject); _manualPortal = null; }
        EndVictoryPresentation();
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
