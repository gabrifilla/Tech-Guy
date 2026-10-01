using UnityEngine;

/// <summary>
/// Pure, scene-free normalization of an enemy's current movement speed into a [0,1] blend value for
/// the locomotion animator parameter. Extracted from <see cref="EnemyAI"/> so the universal invariant
/// "the blend value is always within [0,1]" (Property 6, Requirements 15.2/15.6/16.5) can be
/// property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.11. Requirements: 15.2, 15.6, 16.1.
/// <para>
/// Mirrors the existing <see cref="PreferredDistanceResolver"/> / <see cref="TelegraphBeat"/> /
/// <see cref="CadenceJitter"/> pure pattern: a <see langword="static"/> resolver with explicit inputs
/// and no scene dependency. Defensive handling of a non-positive <c>maxSpeed</c> (returning 0) keeps
/// the output in range even when the agent reports a degenerate speed, so an out-of-range input can
/// never produce an out-of-range blend value (R16.5).
/// </para>
/// </remarks>
public static class MovementBlend
{
    /// <summary>
    /// Normalizes <paramref name="currentSpeed"/> against <paramref name="maxSpeed"/> into the inclusive
    /// range [0,1] for the locomotion blend parameter: <c>currentSpeed / maxSpeed</c> clamped to [0,1]
    /// (R15.2). When <paramref name="maxSpeed"/> is less than or equal to 0 the result is 0, avoiding a
    /// divide-by-zero and keeping the output in range for a degenerate agent speed (R15.6/R16.5).
    /// </summary>
    /// <param name="currentSpeed">The agent's current speed (e.g. <c>agent.velocity.magnitude</c>).</param>
    /// <param name="maxSpeed">The agent's maximum speed (e.g. <c>agent.speed</c>).</param>
    /// <returns>The movement blend value, always within [0,1].</returns>
    public static float Normalize(float currentSpeed, float maxSpeed)
    {
        if (maxSpeed <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(currentSpeed / maxSpeed);
    }
}
