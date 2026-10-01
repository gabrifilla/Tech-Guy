using UnityEngine;

/// <summary>
/// Pure, scene-free retreat window/cooldown state machine for a single ranged enemy. Models the
/// three-phase kiting lifecycle (<see cref="Phase.Eligible"/> &#8594; <see cref="Phase.Kiting"/>
/// &#8594; <see cref="Phase.Cooldown"/> &#8594; <see cref="Phase.Eligible"/>) that bounds how long an
/// enemy may continuously retreat before it must pause, so melee can close the gap. Extracted so the
/// invariant "the retreat window bounds continuous kiting and the cooldown blocks then re-arms"
/// (Property 7) can be property-tested without a live Unity scene. Mirrors the existing
/// <see cref="PreferredDistanceResolver"/> / <see cref="TelegraphBeat"/> pattern: time is fed in
/// explicitly so a given input sequence produces a deterministic result.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 3.1.
/// Requirements: 3.1, 3.2, 3.3, 3.4, 9.1, 9.4.
/// <para>
/// Driven by <c>EnemyAI.TryMaintainStandoff()</c>: consulted before issuing a retreat destination.
/// <c>wantsToKite</c> means the player is within the Standoff_Distance. When <see cref="Tick"/>
/// returns true the enemy is permitted to kite this frame (set retreat speed, skip firing); when it
/// returns false the enemy does not kite and the existing attack path runs normally (R3.3).
/// </para>
/// <para>
/// State transitions follow design Diagram 2: from <see cref="Phase.Eligible"/>, a
/// <c>wantsToKite</c> frame enters <see cref="Phase.Kiting"/> and accumulates continuous kite time;
/// at <c>&gt;= retreatWindow</c> it transitions to <see cref="Phase.Cooldown"/> and returns false
/// (R3.1/R3.2). In <see cref="Phase.Cooldown"/> it returns false until elapsed <c>&gt;=
/// retreatCooldown</c>, then returns to <see cref="Phase.Eligible"/> (R3.3/R3.4). Leaving the
/// standoff band (<c>wantsToKite == false</c>) returns to <see cref="Phase.Eligible"/> and zeroes
/// the kite timer. Deterministic: identical <c>(wantsToKite, dt, retreatWindow, retreatCooldown,
/// prior state)</c> produce an identical result (R9.4).
/// </para>
/// </remarks>
public sealed class KiteController
{
    /// <summary>The kiting lifecycle phases (design Diagram 2).</summary>
    public enum Phase
    {
        /// <summary>Permitted to kite; a <c>wantsToKite</c> frame begins retreating.</summary>
        Eligible,

        /// <summary>Actively retreating; continuous kite time accrues toward the window.</summary>
        Kiting,

        /// <summary>Window exhausted; kiting blocked until the cooldown elapses.</summary>
        Cooldown
    }

    private Phase _current = Phase.Eligible;
    private float _kiteTime;
    private float _cooldownTime;

    /// <summary>The current phase of the retreat state machine.</summary>
    public Phase Current => _current;

    /// <summary>
    /// Advances the state machine one frame and returns whether kiting is permitted this frame.
    /// <para>
    /// When <paramref name="wantsToKite"/> is false the machine returns to <see cref="Phase.Eligible"/>,
    /// zeroes the accrued kite time, and returns false (the enemy is at/beyond standoff, R2.4). When
    /// <paramref name="wantsToKite"/> is true the behavior depends on the current phase:
    /// </para>
    /// <list type="bullet">
    /// <item><description><see cref="Phase.Eligible"/>: enters <see cref="Phase.Kiting"/> and returns true.</description></item>
    /// <item><description><see cref="Phase.Kiting"/>: accrues continuous kite time; while it is below the
    /// clamped <paramref name="retreatWindow"/> returns true, and once it reaches the window transitions
    /// to <see cref="Phase.Cooldown"/> and returns false (R3.1/R3.2).</description></item>
    /// <item><description><see cref="Phase.Cooldown"/>: accrues cooldown time and returns false until it
    /// reaches the clamped <paramref name="retreatCooldown"/>, then returns to <see cref="Phase.Eligible"/>
    /// and begins kiting again on that same frame (R3.3/R3.4).</description></item>
    /// </list>
    /// Deterministic: identical inputs and prior state produce an identical result (R9.4). Durations are
    /// defensively clamped to be positive before use so a misconfigured value cannot stall the machine.
    /// </summary>
    /// <param name="wantsToKite">Whether the player is within the Standoff_Distance this frame.</param>
    /// <param name="dt">The frame delta time, in seconds; non-positive values accrue no time.</param>
    /// <param name="retreatWindow">Max continuous kite duration before cooldown; constrained to be &gt; 0 (R3.1/R3.5).</param>
    /// <param name="retreatCooldown">Cooldown duration before kiting is permitted again; constrained to be &gt; 0 (R3.2/R3.5).</param>
    /// <returns>True when kiting is permitted this frame; false otherwise.</returns>
    public bool Tick(bool wantsToKite, float dt, float retreatWindow, float retreatCooldown)
    {
        float window = Mathf.Max(Mathf.Epsilon, retreatWindow);
        float cooldown = Mathf.Max(Mathf.Epsilon, retreatCooldown);
        float step = dt > 0f ? dt : 0f;

        // Leaving the standoff band always resets to Eligible and zeroes the kite timer (R2.4).
        if (!wantsToKite)
        {
            _current = Phase.Eligible;
            _kiteTime = 0f;
            _cooldownTime = 0f;
            return false;
        }

        switch (_current)
        {
            case Phase.Eligible:
                // Begin a fresh retreat window.
                _current = Phase.Kiting;
                _kiteTime = 0f;
                return true;

            case Phase.Kiting:
                _kiteTime += step;
                if (_kiteTime >= window)
                {
                    // Window exhausted: stop retreating and enter cooldown (R3.1/R3.2).
                    _current = Phase.Cooldown;
                    _cooldownTime = 0f;
                    _kiteTime = 0f;
                    return false;
                }
                return true;

            case Phase.Cooldown:
                _cooldownTime += step;
                if (_cooldownTime >= cooldown)
                {
                    // Cooldown elapsed: eligible again and resume kiting this frame (R3.4).
                    _current = Phase.Kiting;
                    _cooldownTime = 0f;
                    _kiteTime = 0f;
                    return true;
                }
                return false;

            default:
                return false;
        }
    }

    /// <summary>
    /// Clears all accrued state, returning the machine to its initial <see cref="Phase.Eligible"/>
    /// condition with zeroed kite and cooldown timers.
    /// </summary>
    public void Reset()
    {
        _current = Phase.Eligible;
        _kiteTime = 0f;
        _cooldownTime = 0f;
    }
}
