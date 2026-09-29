using UnityEngine;

/// <summary>
/// Pure, scene-free heal-clamp math extracted from <see cref="Actor.Heal(float)"/> so it can be
/// property-tested without a live Unity <see cref="MonoBehaviour"/> (task 2.3, Property 8).
///
/// <see cref="Actor.Heal(float)"/> restores health as <c>min(maxHealth, health + amount)</c> and is a
/// no-op for a non-positive <paramref name="amount"/> (or a dead actor). This helper models the same
/// clamp so the invariant "healing never exceeds the maximum and never lowers health" (R13.3) can be
/// checked directly.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 2.3. Requirements: 13.3.</remarks>
public static class HealMath
{
    /// <summary>
    /// Returns the health after applying a heal of <paramref name="amount"/> to
    /// <paramref name="health"/>, clamped to <paramref name="maxHealth"/>. A non-positive
    /// <paramref name="amount"/> leaves health unchanged, matching <see cref="Actor.Heal(float)"/>'s
    /// no-op semantics. The result is always in <c>[health, maxHealth]</c> for a starting health at
    /// or below the maximum.
    /// </summary>
    public static float Clamp(float health, float maxHealth, float amount)
    {
        if (amount <= 0f) return health;
        return Mathf.Min(maxHealth, health + amount);
    }
}
