using UnityEngine;

/// <summary>
/// Pure, scene-free bounded-turn resolver for orienting an enemy toward the player over time
/// instead of snapping instantly. Extracted from <see cref="EnemyAI"/> so the facing invariants
/// "the step never overshoots the target" and "facing is reached within the epsilon" (Property 1,
/// Requirements 10.1/10.3/10.6/16.3) can be property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.1. Requirements: 10.1, 10.2, 10.3, 10.5, 10.6, 16.1.
/// <para>
/// Mirrors the existing <c>PreferredDistanceResolver</c>/<c>TelegraphBeat</c> pattern: a static class
/// of deterministic math fed explicit inputs. <see cref="EnemyAI.FacePlayer"/> replaces its instant
/// <c>transform.rotation = Quaternion.LookRotation(direction)</c> snap with
/// <c>Quaternion.LookRotation(EnemyFacing.StepTowards(transform.forward, direction, angularSpeed, Time.deltaTime))</c>,
/// and gates facing-dependent actions on <see cref="IsFacing"/> exactly as the instant snap allowed.
/// </para>
/// </remarks>
public static class EnemyFacing
{
    /// <summary>Minimum configurable Angular_Speed in degrees per second (R10.2).</summary>
    public const float MinAngularSpeed = 90f;

    /// <summary>Maximum configurable Angular_Speed in degrees per second (R10.2).</summary>
    public const float MaxAngularSpeed = 1440f;

    /// <summary>
    /// The remaining signed angle (degrees) at or below which the enemy forward is treated as
    /// facing the player, permitting facing-gated actions (R10.3/R10.4).
    /// </summary>
    public const float FacingEpsilonDegrees = 0.5f;

    /// <summary>
    /// Clamps a configured Angular_Speed to the inclusive [<see cref="MinAngularSpeed"/>,
    /// <see cref="MaxAngularSpeed"/>] range (R10.2), so an out-of-range serialized value can never
    /// produce an out-of-range turn step.
    /// </summary>
    public static float ClampAngularSpeed(float configured)
        => Mathf.Clamp(configured, MinAngularSpeed, MaxAngularSpeed);

    /// <summary>
    /// Produces the next forward direction stepping from <paramref name="currentForward"/> toward
    /// <paramref name="targetDir"/>, limited to <c>ClampAngularSpeed(angularSpeed) * dt</c> degrees.
    /// Uses <see cref="Vector3.RotateTowards"/>-style clamping so the step never exceeds the
    /// remaining angle: it converges without overshooting past the target and never flips sign
    /// (R10.1/R10.3/R10.6/R16.3). When <paramref name="targetDir"/> has a squared magnitude at or
    /// below <see cref="Mathf.Epsilon"/>, the current forward is returned unchanged (R10.5).
    /// </summary>
    /// <param name="currentForward">The enemy's current forward direction.</param>
    /// <param name="targetDir">The desired direction toward the player (need not be normalized).</param>
    /// <param name="angularSpeed">The configured Angular_Speed in degrees per second (clamped internally).</param>
    /// <param name="dt">The frame delta time in seconds; non-positive dt yields no rotation.</param>
    /// <returns>The normalized next forward direction.</returns>
    public static Vector3 StepTowards(Vector3 currentForward, Vector3 targetDir, float angularSpeed, float dt)
    {
        // Degenerate target direction: leave the rotation unchanged for this frame (R10.5).
        if (targetDir.sqrMagnitude <= Mathf.Epsilon)
            return currentForward;

        // A degenerate current forward cannot define a rotation axis; snap straight to the target.
        if (currentForward.sqrMagnitude <= Mathf.Epsilon)
            return targetDir.normalized;

        float maxRadians = Mathf.Max(0f, ClampAngularSpeed(angularSpeed)) * Mathf.Max(0f, dt) * Mathf.Deg2Rad;

        // RotateTowards clamps to the remaining angle, so the step never overshoots the target
        // and the forward direction converges monotonically (R10.1/R10.3/R10.6/R16.3).
        return Vector3.RotateTowards(currentForward.normalized, targetDir.normalized, maxRadians, 0f).normalized;
    }

    /// <summary>
    /// Returns true once the remaining angle between <paramref name="currentForward"/> and
    /// <paramref name="targetDir"/> is at or below <see cref="FacingEpsilonDegrees"/>, at which
    /// point the enemy forward is treated as facing the player and facing-gated actions may proceed
    /// (R10.3/R10.4). A degenerate target direction is treated as already facing (R10.5): no further
    /// turning is possible, so facing-gated behavior proceeds exactly as the instant-snap did.
    /// </summary>
    public static bool IsFacing(Vector3 currentForward, Vector3 targetDir)
    {
        if (targetDir.sqrMagnitude <= Mathf.Epsilon)
            return true;
        if (currentForward.sqrMagnitude <= Mathf.Epsilon)
            return false;

        return Vector3.Angle(currentForward, targetDir) <= FacingEpsilonDegrees;
    }
}
