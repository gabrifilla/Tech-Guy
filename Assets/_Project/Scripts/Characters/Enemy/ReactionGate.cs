using UnityEngine;

/// <summary>
/// Pure, scene-free perception reaction gate for a single enemy. Models the short delay between an
/// enemy first seeing the player and the enemy beginning to chase/attack, plus the sight-loss reset
/// that re-arms that delay once the player has been out of sight long enough. Extracted so the
/// invariants "blocks until the delay elapses, then opens while continuously in sight, then re-arms
/// after a continuous sight-loss window" (Property 2) can be property-tested without a live Unity
/// scene. Mirrors the existing <see cref="PreferredDistanceResolver"/> / <see cref="TelegraphBeat"/>
/// pattern: time is fed in explicitly so a given input sequence produces a deterministic result.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.3.
/// Requirements: 11.1, 11.2, 11.3, 11.4, 11.5, 11.6, 16.1, 16.2.
/// <para>
/// The gate is distinct from <c>CombatReactionController</c>; it governs only the perception reaction
/// delay and is driven by <c>EnemyAI.Update()</c> right after <c>UpdatePerception()</c>, as an
/// additional gate on beginning chase/attack. All existing perception gates remain unchanged (R11.7).
/// </para>
/// </remarks>
public sealed class ReactionGate
{
    /// <summary>Minimum configurable reaction delay, in seconds (R11.2).</summary>
    public const float MinReactionDelay = 0f;

    /// <summary>Maximum configurable reaction delay, in seconds (R11.2).</summary>
    public const float MaxReactionDelay = 2f;

    /// <summary>Minimum configurable sight-loss reset, in seconds (R11.5).</summary>
    public const float MinSightLossReset = 0f;

    /// <summary>Maximum configurable sight-loss reset, in seconds (R11.5).</summary>
    public const float MaxSightLossReset = 10f;

    private float _inSightTime;
    private float _outOfSightTime;
    private bool _reacted;

    /// <summary>
    /// True once the reaction delay has elapsed while the player has been continuously in sight, and
    /// stays true for as long as the player remains continuously in sight (R11.4). Reset to false
    /// when the delay re-arms after a continuous sight-loss window (R11.5).
    /// </summary>
    public bool Reacted => _reacted;

    /// <summary>
    /// Advances the gate one frame and returns <see cref="Reacted"/>.
    /// <para>
    /// While <paramref name="inSight"/> is true, continuous in-sight time accrues; once it reaches the
    /// clamped <paramref name="reactionDelay"/> the gate opens (R11.1/R11.3/R11.4). A clamped
    /// <paramref name="reactionDelay"/> of 0 opens the gate on the first in-sight frame (R11.6).
    /// </para>
    /// <para>
    /// While <paramref name="inSight"/> is false, continuous out-of-sight time accrues; once it reaches
    /// the clamped <paramref name="sightLossReset"/> the gate re-arms (clears <see cref="Reacted"/> and
    /// zeroes the accrued in-sight time) so the next detection incurs the delay again (R11.5). Losing
    /// sight for less than the reset does not re-arm the gate.
    /// </para>
    /// Deterministic: an identical sequence of (inSight, dt, reactionDelay, sightLossReset) produces an
    /// identical result (R16.1/R16.2). Inputs are defensively clamped before use (R11.2/R11.5).
    /// </summary>
    /// <param name="inSight">Whether the player is in sight this frame (from the existing perception gates).</param>
    /// <param name="dt">The frame delta time, in seconds; non-positive values accrue no time.</param>
    /// <param name="reactionDelay">The configured reaction delay; clamped to [0, 2] (R11.2).</param>
    /// <param name="sightLossReset">The configured sight-loss reset; clamped to [0, 10] (R11.5).</param>
    public bool Tick(bool inSight, float dt, float reactionDelay, float sightLossReset)
    {
        float delay = Mathf.Clamp(reactionDelay, MinReactionDelay, MaxReactionDelay);
        float reset = Mathf.Clamp(sightLossReset, MinSightLossReset, MaxSightLossReset);
        float step = dt > 0f ? dt : 0f;

        if (inSight)
        {
            // Continuous in-sight: stop accruing the sight-loss window and advance toward reacting.
            _outOfSightTime = 0f;
            _inSightTime += step;
            if (_inSightTime >= delay)
            {
                _reacted = true;
            }
        }
        else
        {
            // Continuous out-of-sight: advance the sight-loss window; re-arm once it is reached.
            _outOfSightTime += step;
            if (_outOfSightTime >= reset)
            {
                _reacted = false;
                _inSightTime = 0f;
            }
        }

        return _reacted;
    }

    /// <summary>
    /// Clears all accrued state, returning the gate to its initial armed, not-yet-reacted condition.
    /// </summary>
    public void Reset()
    {
        _inSightTime = 0f;
        _outOfSightTime = 0f;
        _reacted = false;
    }
}
