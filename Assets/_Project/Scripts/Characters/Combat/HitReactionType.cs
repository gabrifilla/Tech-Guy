/// <summary>
/// The immediate, guaranteed reaction a hit produces regardless of stance. Hard crowd-control
/// (stun / knock-up / knockback) is NOT applied here — it only happens on a Stance Break.
/// </summary>
public enum HitReactionType
{
    /// <summary>No visible reaction, only stance damage is applied.</summary>
    None,
    /// <summary>Small physical displacement that keeps the enemy near the player and does not remove control.</summary>
    Push,
    /// <summary>Brief interruption of the enemy's current action (hit reaction). Default for basic attacks.</summary>
    Stagger
}

/// <summary>
/// The hard crowd-control that triggers when an enemy's stance is broken. Chosen per attack, and
/// gated by each enemy's resistances/immunities.
/// </summary>
public enum StanceBreakEffect
{
    /// <summary>Break only staggers (longer interrupt), no hard CC.</summary>
    None,
    /// <summary>Enemy is unable to act for a duration.</summary>
    Stun,
    /// <summary>Enemy is launched into the air and cannot act until it lands (air juggle).</summary>
    KnockUp,
    /// <summary>Enemy is thrown a significant distance away from the player. Reserved for specific skills.</summary>
    Knockback
}
