using System;
using UnityEngine;

public sealed partial class ProgressionDirector
{
    private RunStatistics _runStatistics;
    private double _statisticsWallStart, _statisticsScaledStart;
    public RunSummary CurrentRunSummary => _runStatistics?.Snapshot();
    private void MarkStatisticsStart()
    { _statisticsWallStart = Time.realtimeSinceStartupAsDouble; _statisticsScaledStart = Time.timeAsDouble; }
    private void StartRunStatistics()
    {
        _runStatistics?.Dispose();
        _runStatistics = new RunStatistics(_player, ManualSession.Id, ActiveRunSeed, _statisticsWallStart, _statisticsScaledStart);
        _runStatistics.Progress(ManualSession);
        RecordMetaProgressStart();
    }
    private void FinishRunStatistics()
    {
        _runStatistics?.Finish(ManualSession);
        EndFragmentRun();
        RecordMetaProgressFinish();
    }

    // Fase 1a: invisible meta-progression base. Same start/finish points as RunStatistics, once per session.
    [SerializeField] private string _universeId = MetaProgressStore.Universe1;
    private MetaProgressStore _metaProgress;
    private Guid _metaStartedSession, _metaFinishedSession, _metaBossVictorySession;
    public string UniverseId => _universeId;
    /// <summary>Victory counts when the universe boss dies, not when the player walks into the portal.</summary>
    private void RecordMetaProgressBossVictory()
    {
        if (ManualSession == null || _metaProgress == null || _metaStartedSession != ManualSession.Id ||
            _metaBossVictorySession == ManualSession.Id) return;
        _metaBossVictorySession = ManualSession.Id;
        _metaProgress.RecordBossVictory(_universeId);
    }
    private void RecordMetaProgressStart()
    {
        if (ManualSession == null || _metaStartedSession == ManualSession.Id) return;
        _metaStartedSession = ManualSession.Id;
        var started = (_metaProgress ??= MetaProgressStore.CreateDefault()).RecordRunStarted(ManualSession.Id.ToString());
        // Fase 1b ally: 2nd combat room on the very first run, 1st combat room from then on until rescued.
        _npcTargetRoom = started.HasAlly(MetaAllies.Unknown) ? 0 : started.runsStarted <= 1 ? 2 : 1;
    }
    private void RecordMetaProgressFinish()
    {
        if (ManualSession == null || ManualSession.Outcome == RunOutcome.None || _metaProgress == null ||
            _metaStartedSession != ManualSession.Id || _metaFinishedSession == ManualSession.Id) return;
        _metaFinishedSession = ManualSession.Id;
        _metaProgress.RecordRunFinished(ManualSession.Outcome, _universeId, _metaBossVictorySession == ManualSession.Id, ManualSession.Id.ToString());
    }
}
