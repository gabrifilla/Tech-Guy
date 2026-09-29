using System;

/// <summary>
/// Pure, scene-free configuration for the swarm melee attack-slot system (R5). Holds the single
/// tunable value that governs how many melee enemies may attack the player at once, kept out of the
/// <see cref="SwarmAttackCoordinator"/> MonoBehaviour so the limit and its validation can be
/// property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 6.1.
/// Requirements: 5.1 (limit &gt; 0), 5.2, 5.3, 5.5. Data model: AttackSlotConfig.
///
/// The value is validated to be strictly greater than zero on construction (R5.1). The intended
/// data-authored source (an <c>EnemyProfile</c>/room asset field) feeds a raw integer into
/// <see cref="FromRawLimit"/>, which clamps to a safe minimum of 1 and reports whether the authored
/// value was missing/invalid so callers can log the absence without breaking the encounter.
/// </remarks>
public readonly struct AttackSlotConfig
{
    /// <summary>The minimum valid concurrent-attacker limit. The system requires at least one slot (R5.1).</summary>
    public const int MinConcurrentMelee = 1;

    /// <summary>
    /// Maximum number of melee enemies that may hold an attack token (and therefore attack the
    /// player) simultaneously. Always strictly greater than zero (R5.1).
    /// </summary>
    public int MaxConcurrentMelee { get; }

    /// <summary>
    /// Creates a validated config. Throws when <paramref name="maxConcurrentMelee"/> is not strictly
    /// greater than zero, enforcing the R5.1 invariant at the type boundary.
    /// </summary>
    /// <param name="maxConcurrentMelee">The concurrent melee attacker limit; must be &gt; 0.</param>
    /// <exception cref="ArgumentOutOfRangeException">When the limit is not strictly greater than zero.</exception>
    public AttackSlotConfig(int maxConcurrentMelee)
    {
        if (maxConcurrentMelee < MinConcurrentMelee)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxConcurrentMelee),
                maxConcurrentMelee,
                "AttackSlotConfig.maxConcurrentMelee must be strictly greater than 0 (R5.1).");
        }

        MaxConcurrentMelee = maxConcurrentMelee;
    }

    /// <summary>
    /// Builds a config from a raw, possibly data-authored value. A missing or non-positive value is
    /// clamped up to <see cref="MinConcurrentMelee"/> so the encounter always has a usable limit
    /// (R5.1); <paramref name="wasClamped"/> reports the substitution so the caller can log the
    /// missing/invalid authored value without interrupting play.
    /// </summary>
    /// <param name="rawLimit">The authored limit as read from data (may be 0 or negative when unset).</param>
    /// <param name="wasClamped">True when the raw value was invalid and clamped to the minimum.</param>
    /// <returns>A valid <see cref="AttackSlotConfig"/>.</returns>
    public static AttackSlotConfig FromRawLimit(int rawLimit, out bool wasClamped)
    {
        if (rawLimit < MinConcurrentMelee)
        {
            wasClamped = true;
            return new AttackSlotConfig(MinConcurrentMelee);
        }

        wasClamped = false;
        return new AttackSlotConfig(rawLimit);
    }
}
