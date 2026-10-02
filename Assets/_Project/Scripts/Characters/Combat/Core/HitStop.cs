using System.Collections.Generic;

/// <summary>
/// Pure, scene-free (non-<c>MonoBehaviour</c>) hit-stop decision for a single ImpactEvent: how long
/// a hit-stop should last and whether it applies at all (R7.1, R7.2, R7.8). The thin
/// <c>MonoBehaviour</c> <c>HitStopRunner</c> consumes these decisions and translates them into a
/// <c>Time.timeScale</c> effect while preserving other time modifiers; nothing here touches the
/// engine, so the decision stays deterministic and property-testable in isolation (R8.7), matching
/// the other Núcleo_Compartilhado Core types.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 9.1. Requirements: R7.1, R7.2, R7.3, R7.8.</remarks>
public static class HitStop
{
    /// <summary>
    /// Upper bound of a hit-stop duration, in seconds: the closed interval is <c>[0, 1]</c> (R7.2).
    /// A configured duration of <c>0</c> keeps the "do not pause" decision.
    /// </summary>
    public const float MaxDuration = 1f;

    /// <summary>
    /// Clamps a configured hit-stop duration to the closed interval <c>[0, MaxDuration]</c> =
    /// <c>[0, 1]</c> seconds (R7.2). Values below <c>0</c> clamp to <c>0</c>, values above
    /// <see cref="MaxDuration"/> clamp to <see cref="MaxDuration"/>, and <c>NaN</c> is treated as
    /// <c>0</c> — consistent with the other Core clamps (e.g. <c>CommitmentRules</c>,
    /// <c>ActionTimeline</c>) — so a corrupt asset can never leave a NaN duration in the combat loop.
    /// A clamped <c>0</c> is the explicit "no pause" value.
    /// </summary>
    /// <param name="configured">The authored hit-stop duration, in seconds.</param>
    /// <returns>The duration constrained to <c>[0, 1]</c>.</returns>
    public static float ClampDuration(float configured)
    {
        if (configured < 0f || float.IsNaN(configured))
        {
            return 0f;
        }

        return configured > MaxDuration ? MaxDuration : configured;
    }

    /// <summary>
    /// Decides whether a hit-stop should be applied for an ImpactEvent: returns <c>true</c>
    /// <em>if and only if</em> the clamped duration is strictly greater than <c>0</c> <em>and</em>
    /// the ImpactEvent resolved damage on at least one enemy (<paramref name="enemiesDamaged"/>
    /// &gt;= 1) (R7.1, R7.8). A clamped duration of <c>0</c> yields <c>false</c> regardless of how
    /// many enemies were damaged, so a disabled hit-stop never pauses the game.
    /// </summary>
    /// <param name="configuredDuration">The authored hit-stop duration, in seconds (clamped internally).</param>
    /// <param name="enemiesDamaged">The number of enemies the ImpactEvent resolved damage on.</param>
    /// <returns>
    /// <c>true</c> when <see cref="ClampDuration"/> of <paramref name="configuredDuration"/> is
    /// greater than <c>0</c> and <paramref name="enemiesDamaged"/> is at least <c>1</c>; otherwise
    /// <c>false</c>.
    /// </returns>
    public static bool ShouldApply(float configuredDuration, int enemiesDamaged)
    {
        return ClampDuration(configuredDuration) > 0f && enemiesDamaged >= 1;
    }
}

/// <summary>
/// Pure, scene-free (non-<c>MonoBehaviour</c>) grouping policy for simultaneous hit-stops: a single
/// ImpactEvent that strikes N enemies, or several impacts resolved at the same instant, produce
/// <em>one</em> grouped feedback. The combined duration follows an explicit policy — the clamped
/// <em>maximum</em> — and is <strong>never</strong> the per-target sum of the N durations (R7.3).
/// Being engine-free keeps it deterministic and property-testable in isolation (R8.7).
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 9.1. Requirements: R7.3.</remarks>
public static class HitStopGrouping
{
    /// <summary>
    /// Combines the hit-stop durations of simultaneous impacts into the single grouped duration,
    /// returning the clamped <em>maximum</em> and never the per-target sum (R7.3). Each duration is
    /// individually clamped to <c>[0, 1]</c> via <see cref="HitStop.ClampDuration"/> before the
    /// maximum is taken. A <c>null</c> or empty list returns <c>0</c> (no pause).
    /// </summary>
    /// <param name="simultaneousDurations">
    /// The authored hit-stop durations of the impacts resolved at the same instant.
    /// </param>
    /// <returns>The clamped maximum of the provided durations, or <c>0</c> when there are none.</returns>
    public static float Combine(IReadOnlyList<float> simultaneousDurations)
    {
        if (simultaneousDurations == null || simultaneousDurations.Count == 0)
        {
            return 0f;
        }

        float max = 0f;
        for (int i = 0; i < simultaneousDurations.Count; i++)
        {
            float clamped = HitStop.ClampDuration(simultaneousDurations[i]);
            if (clamped > max)
            {
                max = clamped;
            }
        }

        return max;
    }
}
