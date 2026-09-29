using UnityEngine;

/// <summary>
/// Pure, scene-free calculator for the soft-grouping the Manoplas' W (Punhos Relâmpago / Flurry)
/// reapplies to the target it keeps punching (Requisito 7.2). While the Flurry runs, the system
/// reapplies <see cref="SoftGroupingCalculator"/>-style grouping to the struck target so it is kept
/// within <see cref="MaxRadiusFromChainPoint"/> (2.0 m) of the chaining point, respecting the same
/// per-application ceiling the base soft grouping uses.
///
/// It mirrors the bounded-displacement approach the soft-grouping core already uses — it does not
/// open a parallel movement channel — but instead of pulling toward a shared group centre it pulls
/// the Flurry target toward the chaining point and only when the target has drifted beyond the 2.0 m
/// chain radius. Every application still respects the shared per-application cap
/// (<see cref="SoftGroupingConfig.MaxDisplacementPerApplication"/>) and the grouping speed
/// (<see cref="SoftGroupingConfig.MaxSpeed"/>).
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 13.1.
/// Requirements: 7.2.
/// The R7.2 caps live here as clamped math so Property 19 (task 13.2) can verify them without a live
/// Unity scene. The thin caller (<c>BreakerGauntletCombat</c>) applies the returned delta through the
/// target's existing locomotion means via <c>SoftGroupingService.ApplyExternalDisplacement</c>,
/// exactly as the Lança's sweep push does, so the Flurry never duplicates locomotion.
/// </remarks>
public static class FlurryRegroupDisplacement
{
    /// <summary>R7.2 radius the Flurry keeps the struck target within, measured from the chaining point: 2.0 m.</summary>
    public const float MaxRadiusFromChainPoint = 2.0f;

    /// <summary>R7.2 maximum interval between soft-grouping reapplications during the Flurry: 0.25 s.</summary>
    public const float MaxReapplyInterval = 0.25f;

    /// <summary>
    /// Computes the bounded regroup displacement to apply to the Flurry target for a single
    /// reapplication.
    ///
    /// The target is nudged from <paramref name="targetPosition"/> toward <paramref name="chainPoint"/>
    /// only when it lies beyond <see cref="MaxRadiusFromChainPoint"/> of the chaining point, and only
    /// far enough to close the excess distance — never past the 2.0 m radius (no overshoot toward the
    /// centre). The step magnitude is additionally bounded by the shared per-application cap and by
    /// <c>MaxSpeed × deltaTime</c> from <paramref name="config"/> (R7.1 ceilings the grouping already
    /// honours), so a single reapplication never exceeds either limit.
    ///
    /// The result is zero when the config is invalid, the time step is non-positive, the target is
    /// already within the 2.0 m chain radius, or the target sits exactly on the chaining point.
    /// </summary>
    /// <param name="targetPosition">World position of the struck Flurry target.</param>
    /// <param name="chainPoint">World position of the chaining point the target is kept near.</param>
    /// <param name="deltaTime">Elapsed time for this reapplication, in seconds.</param>
    /// <param name="config">The shared grouping caps (per-application distance / speed).</param>
    /// <returns>The world-space displacement to apply this reapplication, clamped to every cap.</returns>
    public static Vector3 ComputeDisplacement(
        Vector3 targetPosition,
        Vector3 chainPoint,
        float deltaTime,
        SoftGroupingConfig config)
    {
        if (config == null || !config.IsValid) return Vector3.zero;
        if (deltaTime <= 0f) return Vector3.zero;

        Vector3 toChain = chainPoint - targetPosition;
        float distance = toChain.magnitude;

        // Only regroup a target that has drifted beyond the 2.0 m chain radius (R7.2). A target still
        // within the radius, or one already at the chaining point, needs no correction.
        if (distance <= MaxRadiusFromChainPoint) return Vector3.zero;

        // Never pull the target past the 2.0 m radius: the most we ever close is the excess distance.
        float excess = distance - MaxRadiusFromChainPoint;

        // Bound the step by the shared per-application cap and by max-speed × dt (R7.1 ceilings), and
        // never overshoot the 2.0 m radius.
        float maxBySpeed = config.MaxSpeed * deltaTime;
        float step = Mathf.Min(excess, config.MaxDisplacementPerApplication, maxBySpeed);
        if (step <= 0f) return Vector3.zero;

        return toChain / distance * step;
    }
}
