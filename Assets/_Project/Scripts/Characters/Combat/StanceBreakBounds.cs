using UnityEngine;

/// <summary>
/// Pure, scene-independent bounds for the stance-break channel of the reaction system
/// (Requisitos 2.4, 2.6, 2.7).
///
/// The stance subtraction, post-break immunity window and recovery delay all have configurable
/// values that must stay inside fixed, data-driven ranges no matter what an <see cref="EnemyProfile"/>,
/// a rank default or the Inspector supplies:
/// <list type="bullet">
///   <item>the stance-damage multiplier lives in [0.0; 5.0] (Requisito 2.4);</item>
///   <item>the post-break immunity window lives in [0.1; 10.0] seconds (Requisito 2.6);</item>
///   <item>the stance-recovery delay lives in [0.0; 10.0] seconds (Requisito 2.7).</item>
/// </list>
///
/// This is the single shared point that clamps those values (Requisito 1.4). It is a static class
/// with no <see cref="MonoBehaviour"/> dependency so the clamps can be exercised without a scene,
/// per the project rules. The owning <c>CombatReactionController</c> clamps through here whenever it
/// is configured (rank defaults, profile config, Inspector validation), so the stance subtraction
/// <c>max(0, current − StanceDamage × multiplier)</c> and the break bookkeeping always operate on
/// in-range values.
/// </summary>
public static class StanceBreakBounds
{
    /// <summary>Minimum stance-damage multiplier applied to incoming stance damage (Requisito 2.4).</summary>
    public const float MinStanceDamageMultiplier = 0f;

    /// <summary>Maximum stance-damage multiplier applied to incoming stance damage (Requisito 2.4).</summary>
    public const float MaxStanceDamageMultiplier = 5f;

    /// <summary>Shortest post-break immunity window, in seconds (Requisito 2.6).</summary>
    public const float MinBreakImmunitySeconds = 0.1f;

    /// <summary>Longest post-break immunity window, in seconds (Requisito 2.6).</summary>
    public const float MaxBreakImmunitySeconds = 10f;

    /// <summary>Shortest post-break recovery delay, in seconds (Requisito 2.7).</summary>
    public const float MinRecoveryDelaySeconds = 0f;

    /// <summary>Longest post-break recovery delay, in seconds (Requisito 2.7).</summary>
    public const float MaxRecoveryDelaySeconds = 10f;

    /// <summary>
    /// Clamps a stance-damage multiplier to [0.0; 5.0] (Requisito 2.4). Values outside the range are
    /// pulled to the nearest bound so the subtraction <c>StanceDamage × multiplier</c> can never scale
    /// beyond 5× nor go negative.
    /// </summary>
    public static float ClampStanceDamageMultiplier(float multiplier)
        => Mathf.Clamp(multiplier, MinStanceDamageMultiplier, MaxStanceDamageMultiplier);

    /// <summary>Clamps a post-break immunity window to [0.1; 10.0] seconds (Requisito 2.6).</summary>
    public static float ClampBreakImmunitySeconds(float seconds)
        => Mathf.Clamp(seconds, MinBreakImmunitySeconds, MaxBreakImmunitySeconds);

    /// <summary>Clamps a post-break recovery delay to [0.0; 10.0] seconds (Requisito 2.7).</summary>
    public static float ClampRecoveryDelaySeconds(float seconds)
        => Mathf.Clamp(seconds, MinRecoveryDelaySeconds, MaxRecoveryDelaySeconds);

    /// <summary>
    /// Computes the stance reserve after a hit: <c>max(0, current − stanceDamage × multiplier)</c>
    /// with the multiplier clamped to [0.0; 5.0] first (Requisito 2.4). The reserve never drops below
    /// 0. Callers pass the already-validated stance damage (the request ctor does Max(0, …)).
    /// </summary>
    /// <param name="currentStance">The enemy's current stance reserve.</param>
    /// <param name="stanceDamage">The stance damage declared by the hit (non-negative).</param>
    /// <param name="multiplier">The enemy's stance-damage multiplier (clamped here).</param>
    /// <returns>The new stance reserve, never below 0.</returns>
    public static float SubtractStance(float currentStance, float stanceDamage, float multiplier)
    {
        float clampedMultiplier = ClampStanceDamageMultiplier(multiplier);
        float applied = Mathf.Max(0f, stanceDamage) * clampedMultiplier;
        return Mathf.Max(0f, currentStance - applied);
    }

    /// <summary>
    /// The stance reserve restored the instant a Stance Break occurs (Requisito 2.7): the reserve is
    /// set back to the enemy's maximum so the broken enemy starts from a full pool. Mirrors the
    /// <c>currentStance = maxStance</c> assignment the owning <c>CombatReactionController</c> performs
    /// on break; extracted here as pure logic so the restore-to-max invariant can be exercised without
    /// a scene. The maximum is floored at the same minimum the controller enforces (Max(1, …)).
    /// </summary>
    /// <param name="maxStance">The enemy's maximum stance reserve at the moment of the break.</param>
    /// <returns>The restored reserve, equal to the (floored) maximum.</returns>
    public static float RestoredStanceOnBreak(float maxStance) => Mathf.Max(1f, maxStance);

    /// <summary>
    /// The earliest time stance recovery may resume after a Stance Break (Requisito 2.7): recovery is
    /// deferred until <paramref name="breakTime"/> plus the recovery delay, with the delay clamped to
    /// [0.0; 10.0] s first. Mirrors the <c>nextStanceRecoveryTime = Time.time + delay</c> bookkeeping
    /// the owning <c>CombatReactionController</c> performs on break; extracted here as pure logic so
    /// the deferred-recovery invariant can be exercised without a scene.
    /// </summary>
    /// <param name="breakTime">The time (seconds) at which the break resolved.</param>
    /// <param name="recoveryDelay">The configured recovery delay (clamped here to [0.0; 10.0] s).</param>
    /// <returns>The absolute time (seconds) at which recovery may resume.</returns>
    public static float NextRecoveryTimeOnBreak(float breakTime, float recoveryDelay)
        => breakTime + ClampRecoveryDelaySeconds(recoveryDelay);

    /// <summary>
    /// Whether stance recovery may resume at <paramref name="now"/> given the deferred recovery time
    /// computed at the break (Requisito 2.7). Mirrors the controller's recovery gate
    /// (<c>Time.time &lt; nextStanceRecoveryTime</c> blocks recovery): recovery stays paused until the
    /// configured delay has elapsed, then resumes.
    /// </summary>
    /// <param name="now">The current time (seconds).</param>
    /// <param name="nextRecoveryTime">The deferred recovery time from <see cref="NextRecoveryTimeOnBreak"/>.</param>
    /// <returns><c>true</c> once the delay has elapsed; otherwise <c>false</c>.</returns>
    public static bool CanRecoverAt(float now, float nextRecoveryTime) => now >= nextRecoveryTime;
}
