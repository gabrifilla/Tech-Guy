using UnityEngine;

/// <summary>
/// Pure, scene-free calculation core of the soft-grouping behavior (feature
/// weapon-gameplay-swarm-rework, Requirement 7). Given an enemy position, a grouping point and a
/// time step, it returns the bounded displacement that nudges the enemy toward the group centre.
///
/// All of the R7.1 caps live here as clamped math so they can be property-tested without a live
/// Unity scene (Property 18 / Property 22): only enemies within <see cref="SoftGroupingConfig.Radius"/>
/// are moved, the effective speed never exceeds <see cref="SoftGroupingConfig.MaxSpeed"/>, and no
/// single application displaces more than <see cref="SoftGroupingConfig.MaxDisplacementPerApplication"/>.
///
/// The behavior is a no-op when the equipped weapon is the Bow (R7.5), which the calculator decides
/// from the equipped <see cref="RunWeaponFamily"/> so the same rule is testable without a scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 8.1.
/// Requirements: 7.1, 7.5.
/// The thin <see cref="SoftGroupingService"/> MonoBehaviour applies the returned delta through the
/// existing locomotion means (NavMeshAgent / CharacterController), mirroring
/// <c>CombatReactionController.MoveStep</c>. This class performs no movement itself.
/// </remarks>
public static class SoftGroupingCalculator
{
    /// <summary>
    /// The soft grouping applies only when the equipped weapon is not the Bow. When the Bow is
    /// equipped the whole behavior is suppressed (R7.5), so callers should skip movement entirely.
    /// </summary>
    /// <param name="equipped">The family of the currently equipped weapon.</param>
    /// <returns>False when the Bow is equipped (no grouping), true otherwise.</returns>
    public static bool AppliesTo(RunWeaponFamily equipped) => equipped != RunWeaponFamily.Bow;

    /// <summary>
    /// Computes the bounded soft-grouping displacement for a single application.
    ///
    /// The enemy is nudged from <paramref name="enemyPosition"/> toward <paramref name="groupPoint"/>
    /// subject to every R7.1 cap:
    /// <list type="bullet">
    /// <item>enemies outside <see cref="SoftGroupingConfig.Radius"/> of the group point are not moved
    /// (zero displacement);</item>
    /// <item>the step never carries the enemy past the group point (no overshoot);</item>
    /// <item>the magnitude is capped by <c>MaxSpeed * deltaTime</c> and by
    /// <c>MaxDisplacementPerApplication</c>, whichever is smaller.</item>
    /// </list>
    /// The result is zero when the Bow is equipped (R7.5), when the config is invalid, when the time
    /// step is non-positive, or when the enemy is already at the group point.
    /// </summary>
    /// <param name="equipped">The family of the currently equipped weapon (Bow ⇒ no-op).</param>
    /// <param name="enemyPosition">World position of the enemy to nudge.</param>
    /// <param name="groupPoint">World position of the group centre the enemy is pulled toward.</param>
    /// <param name="deltaTime">Elapsed time for this application, in seconds.</param>
    /// <param name="config">The validated grouping caps (speed / radius / per-application distance).</param>
    /// <returns>The world-space displacement to apply this step, already clamped to every cap.</returns>
    public static Vector3 ComputeDisplacement(
        RunWeaponFamily equipped,
        Vector3 enemyPosition,
        Vector3 groupPoint,
        float deltaTime,
        SoftGroupingConfig config)
    {
        if (config == null || !config.IsValid) return Vector3.zero;
        if (!AppliesTo(equipped)) return Vector3.zero;
        if (deltaTime <= 0f) return Vector3.zero;

        Vector3 toGroup = groupPoint - enemyPosition;
        float distance = toGroup.magnitude;

        // Only enemies inside the grouping radius are affected (R7.1). Enemies already at the centre
        // have nothing to move toward.
        if (distance <= 0f || distance > config.Radius) return Vector3.zero;

        // Bound the step by the per-application cap and by max-speed * dt (R7.1), and never overshoot
        // the group point.
        float maxBySpeed = config.MaxSpeed * deltaTime;
        float step = Mathf.Min(distance, config.MaxDisplacementPerApplication, maxBySpeed);
        if (step <= 0f) return Vector3.zero;

        return toGroup / distance * step;
    }
}
