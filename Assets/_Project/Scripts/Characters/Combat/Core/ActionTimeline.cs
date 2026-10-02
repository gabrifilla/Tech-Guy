using UnityEngine;

/// <summary>
/// Models the normalized <c>[0, 1]</c> timeline of a fixed-duration Ação_Ofensiva, dividing it
/// into exactly three contiguous, non-overlapping phases via the serialized boundaries
/// <see cref="StartupEnd"/> and <see cref="ActiveEnd"/> (R2.1, R2.2). It resolves the current
/// <see cref="ActionPhase"/> from a progress value and offers a non-throwing editor validation.
///
/// <para>
/// This type governs <em>phase commitment only</em> — it does not emit ImpactEvents nor open
/// ImpactWindows. Being a pure, scene-free (non-<c>MonoBehaviour</c>) <c>readonly struct</c>, it
/// is deterministic and property-testable in isolation (R8.7). The runtime constructor clamps
/// its inputs to <c>0 &lt;= startupEnd &lt;= activeEnd &lt;= 1</c> as a defense so a bad asset can
/// never leave the combat loop in an inconsistent state; editor-time rejection of invalid design
/// data (without silent mutation) is handled separately by <see cref="Validate"/> (R2.3).
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 2.1. Requirements: R2.1, R2.2, R2.3.</remarks>
public readonly struct ActionTimeline
{
    /// <summary>
    /// End of the Startup phase / start of the Active phase, as a fraction of the timeline.
    /// Always in <c>[0, 1]</c> and never greater than <see cref="ActiveEnd"/>.
    /// </summary>
    public float StartupEnd { get; }

    /// <summary>
    /// End of the Active phase / start of the Recovery phase, as a fraction of the timeline.
    /// Always in <c>[0, 1]</c> and never less than <see cref="StartupEnd"/>.
    /// </summary>
    public float ActiveEnd { get; }

    /// <summary>
    /// Builds a timeline, clamping the boundaries to satisfy
    /// <c>0 &lt;= startupEnd &lt;= activeEnd &lt;= 1</c> as a runtime defense (R2.3). Each boundary is
    /// first clamped to <c>[0, 1]</c>; <see cref="ActiveEnd"/> is then raised to at least
    /// <see cref="StartupEnd"/> so the Active phase never has negative length.
    /// </summary>
    /// <param name="startupEnd">Desired end of Startup / start of Active.</param>
    /// <param name="activeEnd">Desired end of Active / start of Recovery.</param>
    public ActionTimeline(float startupEnd, float activeEnd)
    {
        // Mathf.Clamp01 leaves NaN untouched (NaN compares false against both bounds), so sanitize
        // NaN to 0 first; otherwise a NaN boundary from a corrupt asset would survive the clamp and
        // break the 0 <= StartupEnd <= ActiveEnd <= 1 runtime invariant (R2.3 safe fallback).
        float clampedStartupEnd = Mathf.Clamp01(float.IsNaN(startupEnd) ? 0f : startupEnd);
        float clampedActiveEnd = Mathf.Clamp01(float.IsNaN(activeEnd) ? 0f : activeEnd);

        if (clampedActiveEnd < clampedStartupEnd)
        {
            clampedActiveEnd = clampedStartupEnd;
        }

        StartupEnd = clampedStartupEnd;
        ActiveEnd = clampedActiveEnd;
    }

    /// <summary>
    /// Resolves the phase for a timeline position: Startup for <c>[0, StartupEnd)</c>, Active for
    /// <c>[StartupEnd, ActiveEnd)</c> and Recovery for <c>[ActiveEnd, 1]</c> (R2.1, R2.2).
    /// Positions at or beyond <see cref="ActiveEnd"/> (including the terminal <c>1</c>) resolve to
    /// Recovery, and values outside <c>[0, 1]</c> fall into the nearest edge phase.
    /// </summary>
    /// <param name="progress">Normalized position along the timeline.</param>
    /// <returns>The <see cref="ActionPhase"/> that owns <paramref name="progress"/>.</returns>
    public ActionPhase PhaseOf(float progress)
    {
        if (progress < StartupEnd)
        {
            return ActionPhase.Startup;
        }

        if (progress < ActiveEnd)
        {
            return ActionPhase.Active;
        }

        return ActionPhase.Recovery;
    }

    /// <summary>
    /// Maps a global timeline position in <c>[0, 1]</c> to the progress <em>within</em> the phase
    /// that owns it, as a fraction in <c>[0, 1]</c> (R2.5). The three phases map their own sub-range
    /// to <c>[0, 1]</c>: Startup over <c>[0, StartupEnd)</c>, Active over <c>[StartupEnd, ActiveEnd)</c>
    /// and Recovery over <c>[ActiveEnd, 1]</c>. A phase of zero (or negative) length reports
    /// <c>1</c> for any position inside it (the phase is already complete), and positions outside
    /// <c>[0, 1]</c> clamp to the nearest edge. This is the single source the executors use to build
    /// the unified <c>(ActionPhase, localProgress)</c> report so a fixed-duration action and a
    /// <see cref="ChannelProgress"/> share one phase-local mapping (R8.8).
    /// </summary>
    /// <param name="progress">Normalized global position along the timeline.</param>
    /// <returns>The progress within <see cref="PhaseOf"/>(<paramref name="progress"/>), in <c>[0, 1]</c>.</returns>
    public float LocalProgressOf(float progress)
    {
        float clamped = Mathf.Clamp01(float.IsNaN(progress) ? 0f : progress);

        float start;
        float end;
        switch (PhaseOf(clamped))
        {
            case ActionPhase.Startup:
                start = 0f;
                end = StartupEnd;
                break;
            case ActionPhase.Active:
                start = StartupEnd;
                end = ActiveEnd;
                break;
            default: // Recovery
                start = ActiveEnd;
                end = 1f;
                break;
        }

        float span = end - start;
        if (span <= 0f)
        {
            return 1f;
        }

        return Mathf.Clamp01((clamped - start) / span);
    }

    /// <summary>
    /// Non-throwing editor validation of raw design boundaries against the invariant
    /// <c>0 &lt;= startupEnd &lt;= activeEnd &lt;= 1</c> (R2.3). Returns <c>true</c> with an empty
    /// <paramref name="error"/> when valid; otherwise returns <c>false</c> and sets
    /// <paramref name="error"/> to a clear message naming the offending boundary, without mutating
    /// the design data. Callers (e.g. <c>OnValidate</c>) keep the last valid values on failure.
    /// </summary>
    /// <param name="startupEnd">The authored end of Startup to validate.</param>
    /// <param name="activeEnd">The authored end of Active to validate.</param>
    /// <param name="error">Receives a human-readable description of the invalid boundary, or an
    /// empty string when the values are valid.</param>
    /// <returns><c>true</c> when the boundaries satisfy the invariant; otherwise <c>false</c>.</returns>
    public static bool Validate(float startupEnd, float activeEnd, out string error)
    {
        if (startupEnd < 0f || startupEnd > 1f)
        {
            error = $"startupEnd ({startupEnd}) must be within [0, 1].";
            return false;
        }

        if (activeEnd < 0f || activeEnd > 1f)
        {
            error = $"activeEnd ({activeEnd}) must be within [0, 1].";
            return false;
        }

        if (startupEnd > activeEnd)
        {
            error = $"startupEnd ({startupEnd}) must be less than or equal to activeEnd ({activeEnd}).";
            return false;
        }

        error = string.Empty;
        return true;
    }
}
