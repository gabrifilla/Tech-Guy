using UnityEngine;

/// <summary>
/// Pure, scene-free calculator for the limited displacement the Lança's orbital sweep (W) imparts to
/// an enemy it hits (Requisito 7.4). It mirrors the bounded-displacement approach the soft-grouping
/// core (<see cref="SoftGroupingCalculator"/>) already uses — it does not open a parallel movement
/// channel — but nudges the enemy along the sweep direction rather than toward a group centre, and
/// enforces the sweep's own caps:
/// <list type="bullet">
/// <item>speed at most <see cref="MaxSpeed"/> (1.5 m/s), so a single step over <c>deltaTime</c> never
/// exceeds <c>MaxSpeed * deltaTime</c>;</item>
/// <item>total displacement across the whole sweep at most <see cref="MaxTotalDisplacement"/>
/// (0.75 m), tracked by the caller as <c>alreadyMoved</c>;</item>
/// <item>no instantaneous impulse larger than the remaining budget — the step is always the smaller
/// of the speed cap and the remaining total budget.</item>
/// </list>
/// The thin caller (<c>ArsenalCombat</c>) applies the returned delta through the enemy's existing
/// locomotion means, exactly as <c>SoftGroupingService</c> does, so the sweep never duplicates
/// locomotion.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 11.5.
/// Requirements: 7.4.
/// The R7.4 caps live here as clamped math so Property 21 (task 11.6) can verify them without a live
/// Unity scene.
/// </remarks>
public static class SpearSweepDisplacement
{
    /// <summary>R7.4 ceiling on the sweep push speed: at most 1.5 m/s.</summary>
    public const float MaxSpeed = 1.5f;

    /// <summary>R7.4 ceiling on the total displacement a swept enemy receives across the sweep: 0.75 m.</summary>
    public const float MaxTotalDisplacement = 0.75f;

    /// <summary>
    /// Computes the bounded displacement to apply to a swept enemy for a single application.
    ///
    /// The enemy is nudged along <paramref name="sweepDirection"/>. The step magnitude is the smaller
    /// of the speed cap (<see cref="MaxSpeed"/> × <paramref name="deltaTime"/>) and the budget still
    /// remaining before the <see cref="MaxTotalDisplacement"/> total is reached, so neither the speed
    /// nor the total cap is ever exceeded and no impulse jumps past the remaining budget (R7.4).
    /// The result is zero when the direction is degenerate, the time step is non-positive, or the
    /// total budget has already been spent.
    /// </summary>
    /// <param name="sweepDirection">World-space direction of the sweep (need not be normalised).</param>
    /// <param name="alreadyMoved">Metres already applied to this enemy earlier in the same sweep.</param>
    /// <param name="deltaTime">Elapsed time for this application, in seconds.</param>
    /// <returns>The world-space displacement to apply this step, clamped to every R7.4 cap.</returns>
    public static Vector3 ComputeDisplacement(Vector3 sweepDirection, float alreadyMoved, float deltaTime)
    {
        if (deltaTime <= 0f) return Vector3.zero;

        float dirLength = sweepDirection.magnitude;
        if (dirLength <= Mathf.Epsilon) return Vector3.zero;

        float remaining = MaxTotalDisplacement - Mathf.Max(0f, alreadyMoved);
        if (remaining <= 0f) return Vector3.zero;

        float step = Mathf.Min(MaxSpeed * deltaTime, remaining);
        if (step <= 0f) return Vector3.zero;

        return sweepDirection / dirLength * step;
    }
}
