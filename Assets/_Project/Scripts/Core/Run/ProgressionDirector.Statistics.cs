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
    }
    private void FinishRunStatistics() => _runStatistics?.Finish(ManualSession);
}
