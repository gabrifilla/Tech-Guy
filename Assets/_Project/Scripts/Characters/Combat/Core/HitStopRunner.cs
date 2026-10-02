using System.Collections;
using UnityEngine;

/// <summary>
/// The thin <c>MonoBehaviour</c> runtime that turns a pure <see cref="HitStop"/> decision into a very
/// short, real-time pause of <c>Time.timeScale</c> (R7.1, R7.2, R7.10). All of the "should it apply
/// and for how long" logic lives in the scene-free <see cref="HitStop"/> / <see cref="HitStopGrouping"/>
/// Núcleo_Compartilhado types; this component only <em>drives the engine</em> from the duration those
/// decisions produce, so the decision stays testable in isolation (R8.7) and the per-ImpactEvent/group
/// single application is enforced at the call site (the executor), never here.
///
/// <para><strong>Contract (R7.2):</strong> <see cref="Apply"/> clamps the requested duration through
/// <see cref="HitStop.ClampDuration"/>; a clamped value of <c>0</c> (a disabled profile, e.g. the Asura
/// burst pulses authored with duration 0) is a no-op that <em>never touches</em> <c>Time.timeScale</c>.
/// </para>
///
/// <para><strong>Real time (R7.1):</strong> the pause is measured in unscaled/real time via
/// <see cref="WaitForSecondsRealtime"/>, so setting <c>Time.timeScale</c> to the near-zero hit-stop
/// scale does not stall the pause's own countdown.</para>
///
/// <para><strong>Restore policy (R7.10):</strong> when the hit-stop begins it captures the
/// <em>current</em> <c>Time.timeScale</c> as the baseline and, on finish, restores <em>that captured
/// baseline</em> — never an unconditional <c>1</c> — so a slow-mo or any other active time modifier is
/// preserved. If a pause (or any external scale change) occurs mid-hit-stop, the runner yields to it:
/// on restore it only writes the baseline back when the current scale is still the hit-stop scale this
/// runner set. If something else changed <c>Time.timeScale</c> in the meantime (e.g. the pause menu set
/// it to 0, or a slow-mo effect changed it), the runner leaves that intended scale untouched rather
/// than fighting it. This is the "do not fix unconditionally to 1" guarantee of R7.10.</para>
///
/// <para><strong>Overlap:</strong> a new <see cref="Apply"/> while a hit-stop is already active does not
/// re-capture the baseline (so the pre-hit-stop scale is never lost) and extends the active window to
/// the larger of the remaining and the newly requested real-time duration — it never shortens an
/// in-flight hit-stop. Grouping of simultaneous impacts (the clamped maximum, never the sum) is the
/// job of <see cref="HitStopGrouping.Combine"/> at the call site; the runner only honors whatever single
/// duration it is handed.</para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 16.1. Requirements: R7.1, R7.2, R7.3, R7.4, R7.5, R7.10.</remarks>
[DisallowMultipleComponent]
public sealed class HitStopRunner : MonoBehaviour
{
    /// <summary>
    /// The near-zero scale applied while a hit-stop is active. A true <c>0</c> would be
    /// indistinguishable from a menu pause (several systems treat <c>Time.timeScale &lt;= 0</c> as
    /// "paused"); a tiny positive value gives the characteristic frozen-frame feel while keeping the
    /// world technically running, and lets the restore policy detect "did anyone else change the scale".
    /// </summary>
    private const float HitStopScale = 0.0001f;

    private Coroutine _running;

    // The Time.timeScale observed the instant the hit-stop began — the value restored on finish
    // (R7.10). Captured once per hit-stop window and never re-captured while active, so an overlapping
    // Apply cannot overwrite the real pre-hit-stop scale with the frozen HitStopScale.
    private float _baselineScale = 1f;

    // The scale this runner wrote when it started the active hit-stop. On restore the runner only
    // reinstates the baseline when Time.timeScale is still exactly this value, i.e. nobody else (pause
    // menu, slow-mo) changed it meanwhile; otherwise it yields to that external intent (R7.10).
    private float _appliedScale = 1f;

    // Remaining real-time seconds the active hit-stop should still last. Extended (never shortened) by
    // an overlapping Apply so the longest requested pause wins.
    private float _remaining;

    /// <summary>True while a hit-stop is currently holding <c>Time.timeScale</c> at the frozen scale.</summary>
    public bool IsActive => _running != null;

    /// <summary>
    /// Applies a hit-stop of <paramref name="duration"/> seconds (clamped to <c>[0, 1]</c> via
    /// <see cref="HitStop.ClampDuration"/>) measured in real time. A clamped duration of <c>0</c> is a
    /// no-op that does not touch <c>Time.timeScale</c> (R7.2). When no hit-stop is active it captures the
    /// current <c>Time.timeScale</c> as the restore baseline (R7.10), freezes to the near-zero hit-stop
    /// scale and schedules the restore; when one is already active it only extends the remaining real-time
    /// window to the larger of the two durations without re-capturing the baseline.
    /// </summary>
    /// <param name="duration">The requested hit-stop duration in seconds (pre-clamp).</param>
    public void Apply(float duration)
    {
        float clamped = HitStop.ClampDuration(duration);
        if (clamped <= 0f)
        {
            // R7.2: a disabled/zero hit-stop must not alter the game's time scale at all.
            return;
        }

        if (_running != null)
        {
            // Overlap: keep the longest requested pause, never cut an in-flight hit-stop short, and do
            // NOT re-capture the baseline (that would freeze the already-frozen scale). Grouping of
            // simultaneous impacts is HitStopGrouping.Combine's job at the call site.
            if (clamped > _remaining) _remaining = clamped;
            return;
        }

        if (!isActiveAndEnabled)
        {
            // A disabled runner cannot run a coroutine; applying the scale without a way to restore it
            // would strand the game frozen, so decline rather than risk a permanent freeze.
            return;
        }

        _baselineScale = Time.timeScale;
        _appliedScale = HitStopScale;
        _remaining = clamped;
        Time.timeScale = _appliedScale;
        _running = StartCoroutine(RunHitStop());
    }

    private IEnumerator RunHitStop()
    {
        // Real-time countdown so the frozen Time.timeScale does not stall the pause's own clock (R7.1).
        // Re-read _remaining each loop so an overlapping Apply that extended it is honored.
        while (_remaining > 0f)
        {
            float wait = _remaining;
            _remaining = 0f;
            yield return new WaitForSecondsRealtime(wait);
        }

        RestoreTimeScale();
        _running = null;
    }

    /// <summary>
    /// Restores the captured baseline scale, but only when the current <c>Time.timeScale</c> is still the
    /// frozen scale this runner set — i.e. nothing else changed it meanwhile. If an external system (the
    /// pause menu, a slow-mo effect) changed the scale during the hit-stop, its intended value is left in
    /// place rather than overwritten, honoring R7.10's "do not restore unconditionally to 1" (and, more
    /// generally, do not fight an active pause/modifier).
    /// </summary>
    private void RestoreTimeScale()
    {
        if (Mathf.Approximately(Time.timeScale, _appliedScale))
        {
            Time.timeScale = _baselineScale;
        }
    }

    // If the runner is torn down (room change, player destroyed, scene unload) while a hit-stop is still
    // holding the frozen scale, release it so the world never stays stuck at the near-zero scale. Same
    // yield-to-external policy as the normal finish.
    private void OnDisable()
    {
        if (_running == null) return;
        StopCoroutine(_running);
        _running = null;
        _remaining = 0f;
        RestoreTimeScale();
    }
}
