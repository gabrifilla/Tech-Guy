using UnityEngine;

/// <summary>
/// Pure, scene-free decision for the deterministic effect-degrade ladder applied on a Stance Break
/// (Requisito 2.8 / Property 7).
///
/// WHEN a hit breaks an enemy's stance it requests a <see cref="StanceBreakEffect"/>. Each hard-CC
/// channel has an independent resistance in [0, 1] where 1.0 means fully immune. The rule is:
/// <list type="bullet">
///   <item>apply the requested effect when the enemy is <b>not</b> fully immune to it (resistance &lt; 1.0);</item>
///   <item>otherwise degrade down the severity ladder <c>KnockUp → Stun</c> to the lowest-severity
///   effect the enemy is not fully immune to;</item>
///   <item>otherwise resolve to <see cref="StanceBreakEffect.None"/> (a plain interrupt, no hard CC).</item>
/// </list>
///
/// The ladder only steps <c>KnockUp → Stun</c>: a KnockUp-immune enemy that is not Stun-immune is
/// stunned for the same window. Stun does not degrade to anything softer than None. Knockback has no
/// defined degrade target, so a Knockback-immune enemy resolves straight to None. None always
/// resolves to None. This mirrors the branching the owning <c>CombatReactionController.TriggerStanceBreak</c>
/// has always performed; it is extracted here as a single pure function so the ladder can be
/// exercised without a live scene (mirrors <see cref="StanceBreakBounds"/> / <see cref="ControlLockGate"/>).
///
/// A resistance is treated as "fully immune" using the same <c>&gt;= 1f</c> test the controller uses,
/// so values are effectively clamped: anything at or above 1.0 suppresses the channel, anything below
/// lets it through.
/// </summary>
public static class BreakEffectResistance
{
    /// <summary>The resistance value at or above which a channel is considered fully immune.</summary>
    public const float FullImmunity = 1f;

    /// <summary>Whether <paramref name="resistance"/> denotes full immunity (Requisito 2.8).</summary>
    public static bool IsFullyImmune(float resistance) => resistance >= FullImmunity;

    /// <summary>
    /// Resolves the effect that a Stance Break actually applies, degrading the requested
    /// <paramref name="requested"/> effect down the <c>KnockUp → Stun → None</c> ladder according to
    /// the enemy's per-channel resistances (Requisito 2.8 / Property 7).
    ///
    /// The result is one of: the requested effect (when not fully immune to it), a lower-severity
    /// effect the enemy is not fully immune to, or <see cref="StanceBreakEffect.None"/>.
    /// </summary>
    /// <param name="requested">The hard CC the hit asked for.</param>
    /// <param name="stunResistance">Stun resistance in [0, 1] (1 = immune).</param>
    /// <param name="knockUpResistance">KnockUp resistance in [0, 1] (1 = immune).</param>
    /// <param name="knockbackResistance">Knockback resistance in [0, 1] (1 = immune).</param>
    /// <returns>The effect to actually apply after the deterministic degrade.</returns>
    public static StanceBreakEffect Resolve(
        StanceBreakEffect requested,
        float stunResistance,
        float knockUpResistance,
        float knockbackResistance)
    {
        switch (requested)
        {
            case StanceBreakEffect.KnockUp:
                // Requested launch: apply it unless fully immune, then step down to Stun, then None.
                if (!IsFullyImmune(knockUpResistance)) return StanceBreakEffect.KnockUp;
                return IsFullyImmune(stunResistance) ? StanceBreakEffect.None : StanceBreakEffect.Stun;

            case StanceBreakEffect.Stun:
                // Stun has no softer hard-CC below it: apply it or fall through to None.
                return IsFullyImmune(stunResistance) ? StanceBreakEffect.None : StanceBreakEffect.Stun;

            case StanceBreakEffect.Knockback:
                // Knockback has no defined degrade target: apply it or fall through to None.
                return IsFullyImmune(knockbackResistance) ? StanceBreakEffect.None : StanceBreakEffect.Knockback;

            case StanceBreakEffect.None:
            default:
                return StanceBreakEffect.None;
        }
    }
}
