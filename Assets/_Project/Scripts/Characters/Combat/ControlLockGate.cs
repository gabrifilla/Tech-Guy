/// <summary>
/// Pure, shared decision gate for the additional-displacement suppression rule (Requisito 4.5).
///
/// WHILE an enemy has its control locked (stunned or airborne from a knock-up), any ADDITIONAL
/// immediate displacement — a Micro_Displacement or a Push nudge — from subsequent hits must be
/// suppressed so the enemy is not shoved out of the readable stun/juggle. WHEN the lock ends, the
/// same hits are allowed to displace again. Launch (hard CC from a Stance Break) is a separate
/// channel and is NOT governed by this gate; only the immediate-reaction nudge is.
///
/// This is a static, scene-free predicate so the invariant can be property-checked without a live
/// Unity scene (mirrors <see cref="DisplacementTier"/> / <see cref="ImmediateReactionClamp"/>). The
/// <see cref="CombatReactionController"/> keeps the state (its <c>controlLocked</c> flag) and simply
/// asks this gate whether an immediate displacement of a given tier may be applied this frame.
/// </summary>
public static class ControlLockGate
{
    /// <summary>
    /// Whether an immediate-reaction displacement of <paramref name="tier"/> may be applied to an
    /// enemy whose control-lock state is <paramref name="controlLocked"/> (Requisito 4.5).
    ///
    /// Micro_Displacement and Push are suppressed while control-locked and allowed again once the
    /// lock ends. Launch is not an immediate-reaction tier and is always allowed here — hard CC is
    /// resolved through the Stance Break channel, not this gate.
    /// </summary>
    public static bool AllowsDisplacement(bool controlLocked, DisplacementTierKind tier)
    {
        if (!controlLocked) return true;

        // Locked: suppress the additional Micro/Push nudge; leave Launch (hard CC) untouched.
        return tier == DisplacementTierKind.Launch;
    }

    /// <summary>
    /// Convenience overload used by the immediate reaction: an immediate reaction only ever produces
    /// a Micro_Displacement or a Push (never a Launch — that comes from a Stance Break), so while the
    /// enemy is control-locked no immediate displacement is allowed.
    /// </summary>
    public static bool AllowsImmediateDisplacement(bool controlLocked) => !controlLocked;
}
