using System;
using System.Collections.Generic;

/// <summary>
/// The single advance source for one concrete execution of an Ação_Ofensiva (R2.4–R2.7, R8.9). It
/// is driven once per Quadro_de_Simulacao (<c>FixedUpdate</c>) by <see cref="Advance"/> with the
/// action's normalized <c>[0, 1]</c> progress and is the only place that decides, from the authored
/// <see cref="CombatActionProfile.ImpactEvents"/>, when a discrete ImpactEvent fires or an
/// ImpactWindow opens/closes:
/// <list type="bullet">
/// <item>While the action is in <see cref="ActionPhase.Startup"/>, nothing is emitted and no window
/// opens (R2.4).</item>
/// <item>While in <see cref="ActionPhase.Active"/>, each authored <c>ImpactEvent</c> whose
/// <see cref="ImpactEvent.At"/> has been crossed is emitted once — a discrete impact via
/// <c>onEmit</c>, or an open window via <c>onWindowOpen</c> — within one advance tick of the
/// configured instant (R2.5).</item>
/// <item>An open ImpactWindow is closed via <c>onWindowClose</c> when progress reaches its
/// <see cref="ImpactEvent.WindowEnd"/>, and on entry into <see cref="ActionPhase.Recovery"/> every
/// still-open window closes and no further ImpactEvent of this action is emitted (R2.6).</item>
/// </list>
///
/// <para>
/// Each emission (clock-driven or an Animation Event via <see cref="Signal"/>) is keyed by
/// <c>(ExecutionId, ImpactEvent.Index)</c> through a single <see cref="ExecutionImpactLedger"/>, so
/// whichever source fires an index first wins and the other is a no-op — the logical clock is the
/// source of truth and Animation Events only signal, never double-count (R8.9). The owning executor
/// hands this scheduler a fresh <see cref="ExecutionId"/> per execution.
/// </para>
///
/// <para>
/// Being pure and scene-free (non-<c>MonoBehaviour</c>), it is deterministic and property-testable
/// in isolation (R8.7); it holds no Unity references and reaches gameplay only through the emission
/// callbacks the executor supplies.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 15.1. Requirements: R2.4, R2.5, R2.6, R2.7, R8.9.</remarks>
public sealed class ActionImpactScheduler
{
    private readonly ExecutionId _executionId;
    private readonly ExecutionImpactLedger _ledger;
    private readonly ActionTimeline _timeline;
    private readonly IReadOnlyList<ImpactEvent> _events;

    // The index into _events of each window this scheduler has opened and not yet closed, so entry
    // into Recovery (or crossing WindowEnd) can close exactly those once.
    private readonly List<int> _openWindows = new List<int>();

    private readonly Action<ImpactEvent> _onEmit;
    private readonly Action<ImpactEvent> _onWindowOpen;
    private readonly Action<ImpactEvent> _onWindowClose;

    private ActionPhase _phase = ActionPhase.Startup;
    private bool _recoveryEntered;
    private bool _started;

    /// <summary>The identity of the execution this scheduler advances; the ledger key's first half.</summary>
    public ExecutionId ExecutionId => _executionId;

    /// <summary>The phase resolved at the most recent <see cref="Advance"/> (Startup before the first tick).</summary>
    public ActionPhase Phase => _phase;

    /// <summary>True once <see cref="Advance"/> has observed entry into Recovery; no impact emits afterwards (R2.6).</summary>
    public bool RecoveryEntered => _recoveryEntered;

    /// <summary>True while at least one ImpactWindow opened by this scheduler is still open.</summary>
    public bool HasOpenWindow => _openWindows.Count > 0;

    /// <summary>
    /// Builds a scheduler for one execution.
    /// </summary>
    /// <param name="executionId">Fresh id for this execution (ledger key half); from <see cref="ExecutionId.Next"/>.</param>
    /// <param name="ledger">The dedup ledger shared with any Animation Event path for this execution.</param>
    /// <param name="timeline">The action's fixed-duration <see cref="ActionTimeline"/> (phase boundaries).</param>
    /// <param name="events">The authored ImpactEvents to schedule; a <c>null</c> list behaves as empty.</param>
    /// <param name="onEmit">Invoked once per discrete ImpactEvent that fires (ledger-gated).</param>
    /// <param name="onWindowOpen">Invoked once when an ImpactWindow opens (ledger-gated).</param>
    /// <param name="onWindowClose">Invoked once when an opened ImpactWindow closes (not ledger-gated).</param>
    public ActionImpactScheduler(
        ExecutionId executionId,
        ExecutionImpactLedger ledger,
        ActionTimeline timeline,
        IReadOnlyList<ImpactEvent> events,
        Action<ImpactEvent> onEmit,
        Action<ImpactEvent> onWindowOpen = null,
        Action<ImpactEvent> onWindowClose = null)
    {
        _executionId = executionId;
        _ledger = ledger ?? throw new ArgumentNullException(nameof(ledger));
        _timeline = timeline;
        _events = events ?? Array.Empty<ImpactEvent>();
        _onEmit = onEmit;
        _onWindowOpen = onWindowOpen;
        _onWindowClose = onWindowClose;
    }

    /// <summary>
    /// Advances the single logical clock to <paramref name="progress"/> (normalized <c>[0, 1]</c>),
    /// to be called once per <c>FixedUpdate</c>. Resolves the current phase from the
    /// <see cref="ActionTimeline"/> and, while Active, emits every authored ImpactEvent whose
    /// <see cref="ImpactEvent.At"/> has been crossed and closes any window that has reached its
    /// <see cref="ImpactEvent.WindowEnd"/>. The first observation of Recovery closes all open windows
    /// and latches <see cref="RecoveryEntered"/> so no further impact emits (R2.5, R2.6). Emission is
    /// idempotent per index through the ledger, so re-advancing the same progress never re-emits.
    /// </summary>
    /// <param name="progress">Normalized action progress in <c>[0, 1]</c> for this frame.</param>
    public void Advance(float progress)
    {
        _started = true;
        _phase = _timeline.PhaseOf(progress);

        if (_phase == ActionPhase.Recovery)
        {
            if (!_recoveryEntered)
            {
                _recoveryEntered = true;
                CloseAllWindows();
            }

            return;
        }

        if (_phase != ActionPhase.Active)
        {
            // Startup: emit nothing, open nothing (R2.4).
            return;
        }

        // Active: fire every impact whose instant has been crossed, and close any window that ended.
        for (int i = 0; i < _events.Count; i++)
        {
            ImpactEvent impact = _events[i];

            if (impact.OpensWindow)
            {
                if (progress >= impact.At && progress < impact.WindowEnd)
                {
                    OpenWindow(i, impact);
                }
                else if (progress >= impact.WindowEnd)
                {
                    // The window's whole span elapsed within this phase: open-then-close it once so a
                    // sweep that both opened and ended between two ticks is still accounted for.
                    OpenWindow(i, impact);
                    CloseWindow(i, impact);
                }
            }
            else if (progress >= impact.At)
            {
                if (_ledger.TryEmit(_executionId, impact.Index))
                {
                    _onEmit?.Invoke(impact);
                }
            }
        }
    }

    /// <summary>
    /// The Animation Event signalling path for the ImpactEvent at <paramref name="eventIndex"/>
    /// within <see cref="CombatActionProfile.ImpactEvents"/> (R2.5, R8.9). It routes through the
    /// <em>same</em> ledger as the logical clock: it emits only if this execution has not already
    /// emitted that index (and never once Recovery has been entered), so the clock and an Animation
    /// Event can both signal the same impact without double-counting. Returns <c>true</c> when this
    /// call is the one that emitted.
    /// </summary>
    /// <param name="eventIndex">Index into the authored ImpactEvents array to signal.</param>
    /// <returns><c>true</c> if this signal emitted the impact; <c>false</c> if it was a no-op.</returns>
    public bool Signal(int eventIndex)
    {
        if (_recoveryEntered || eventIndex < 0 || eventIndex >= _events.Count)
        {
            return false;
        }

        ImpactEvent impact = _events[eventIndex];
        if (impact.OpensWindow)
        {
            return false; // windows are opened by the clock, not signalled discretely
        }

        if (_ledger.TryEmit(_executionId, impact.Index))
        {
            _onEmit?.Invoke(impact);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Forces every still-open ImpactWindow closed and latches <see cref="RecoveryEntered"/> so no
    /// further impact emits. Used when the execution ends early (cancel/dodge/death) so a window
    /// never outlives its action (R2.6).
    /// </summary>
    public void StopAndCloseWindows()
    {
        _recoveryEntered = true;
        CloseAllWindows();
    }

    private void OpenWindow(int eventIndex, ImpactEvent impact)
    {
        if (_openWindows.Contains(eventIndex))
        {
            return;
        }

        if (_ledger.TryEmit(_executionId, impact.Index))
        {
            _openWindows.Add(eventIndex);
            _onWindowOpen?.Invoke(impact);
        }
    }

    private void CloseWindow(int eventIndex, ImpactEvent impact)
    {
        if (_openWindows.Remove(eventIndex))
        {
            _onWindowClose?.Invoke(impact);
        }
    }

    private void CloseAllWindows()
    {
        if (_openWindows.Count == 0)
        {
            return;
        }

        for (int i = _openWindows.Count - 1; i >= 0; i--)
        {
            int eventIndex = _openWindows[i];
            _openWindows.RemoveAt(i);
            if (eventIndex >= 0 && eventIndex < _events.Count)
            {
                _onWindowClose?.Invoke(_events[eventIndex]);
            }
        }
    }

    // Suppresses the unused-field warning on _started while making its purpose explicit: it records
    // whether the clock has ticked at least once (useful for executor assertions/future reporting in
    // task 15.2); it is intentionally not read inside the scheduler yet.
    internal bool HasStarted => _started;
}
