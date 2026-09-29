using UnityEngine;

/// <summary>
/// Pure, scene-free calculator for the gentle attraction the Manoplas' R (Asura) applies to enemies
/// around the burst centre (Requisito 7.3). When Asura is activated, enemies within
/// <see cref="Radius"/> (4.0 m) of the central point are drawn toward it at no more than
/// <see cref="MaxSpeed"/> (2.0 m/s) and by no more than <see cref="MaxTotalDisplacement"/> (1.5 m)
/// across the whole pull; enemies outside the radius are never attracted.
///
/// It mirrors the bounded-displacement approach the soft-grouping core
/// (<see cref="SoftGroupingCalculator"/>) and the Lança's sweep push
/// (<see cref="SpearSweepDisplacement"/>) already use — it does not open a parallel movement channel.
/// The caller spends the 1.5 m total budget across frames, tracked as <c>alreadyMoved</c>, and moves
/// each enemy through its own existing locomotion via
/// <c>SoftGroupingService.ApplyExternalDisplacement</c>.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 13.1.
/// Requirements: 7.3.
/// The R7.3 caps live here as clamped math so Property 20 (task 13.3) can verify them without a live
/// Unity scene.
/// </remarks>
public static class AsuraPullDisplacement
{
    /// <summary>R7.3 attraction radius: only enemies within 4.0 m of the central point are pulled.</summary>
    public const float Radius = 4.0f;

    /// <summary>R7.3 ceiling on the pull speed: at most 2.0 m/s.</summary>
    public const float MaxSpeed = 2.0f;

    /// <summary>R7.3 ceiling on the total displacement a single enemy receives across the pull: 1.5 m.</summary>
    public const float MaxTotalDisplacement = 1.5f;

    /// <summary>
    /// True when the enemy is close enough to be attracted by the Asura pull: within
    /// <see cref="Radius"/> (4.0 m) of the central point and not sitting exactly on it. Enemies
    /// outside the radius are never pulled (R7.3), so callers should skip them entirely.
    /// </summary>
    /// <param name="centerPoint">World position of the Asura pull centre.</param>
    /// <param name="enemyPosition">World position of the candidate enemy.</param>
    /// <returns>True when the enemy is inside the pull radius (and not at the centre).</returns>
    public static bool IsWithinRadius(Vector3 centerPoint, Vector3 enemyPosition)
    {
        float distance = (centerPoint - enemyPosition).magnitude;
        return distance > 0f && distance <= Radius;
    }

    /// <summary>
    /// Computes the bounded pull displacement to apply to one enemy for a single application.
    ///
    /// The enemy is drawn from <paramref name="enemyPosition"/> toward <paramref name="centerPoint"/>.
    /// The step magnitude is the smallest of: the speed cap (<see cref="MaxSpeed"/> ×
    /// <paramref name="deltaTime"/>); the total budget still remaining before
    /// <see cref="MaxTotalDisplacement"/> is reached; and the remaining distance to the centre (so the
    /// enemy never overshoots the central point). This guarantees neither the speed cap nor the 1.5 m
    /// total is exceeded and no impulse jumps past the remaining budget (R7.3).
    ///
    /// The result is zero when the enemy is outside the 4.0 m radius (never pulled), when the time step
    /// is non-positive, when the total budget has already been spent, or when the enemy sits on the
    /// centre.
    /// </summary>
    /// <param name="centerPoint">World position of the Asura pull centre.</param>
    /// <param name="enemyPosition">World position of the enemy being pulled.</param>
    /// <param name="alreadyMoved">Metres already applied to this enemy earlier in the same pull.</param>
    /// <param name="deltaTime">Elapsed time for this application, in seconds.</param>
    /// <returns>The world-space displacement to apply this step, clamped to every R7.3 cap.</returns>
    public static Vector3 ComputeDisplacement(
        Vector3 centerPoint,
        Vector3 enemyPosition,
        float alreadyMoved,
        float deltaTime)
    {
        if (deltaTime <= 0f) return Vector3.zero;

        Vector3 toCenter = centerPoint - enemyPosition;
        float distance = toCenter.magnitude;

        // Enemies outside the 4.0 m radius are not attracted (R7.3); enemies at the centre have
        // nothing to move toward.
        if (distance <= 0f || distance > Radius) return Vector3.zero;

        float remaining = MaxTotalDisplacement - Mathf.Max(0f, alreadyMoved);
        if (remaining <= 0f) return Vector3.zero;

        // Bound by the speed cap, the remaining total budget and the distance to the centre (no
        // overshoot).
        float step = Mathf.Min(MaxSpeed * deltaTime, remaining, distance);
        if (step <= 0f) return Vector3.zero;

        return toCenter / distance * step;
    }
}
