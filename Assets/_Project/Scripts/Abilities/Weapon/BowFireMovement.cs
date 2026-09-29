using System;
using UnityEngine;

/// <summary>
/// Pure, scene-free decision for the Arco's (Bow) <b>reduced movement while firing</b> rule
/// (Requisito 10.1). Unlike the Manoplas/Lança, whose casts fully lock the player in place, the Bow
/// lets the player keep moving while shooting — but at a fraction of full move speed, never at full
/// speed and never at a dead stop.
///
/// The single decision this class owns is: given the player's <i>full</i> move speed, what speed is
/// allowed while firing the Bow? The answer is <c>fullSpeed * fraction</c>, with the fraction pinned
/// strictly inside <c>(0, 1)</c>. That guarantee is exactly what Property 33 asserts: for any full
/// speed &gt; 0 the firing speed is strictly greater than 0 and strictly less than the full speed
/// (Requisito 10.1 — "menor que a velocidade de movimento plena e maior que 0, sem exigir parada
/// total").
///
/// It is a plain C# class (no <see cref="MonoBehaviour"/>) so the coordinating
/// <c>ArsenalCombat</c> stays thin (AGENTS.md) and the reduced-speed rule can be property-tested
/// without a live scene. The scene-side coordinator reuses the existing move-speed system
/// (the <c>NavMeshAgent.speed</c> the player already navigates with) and only asks this class for
/// the reduced value — it never invents a parallel movement channel.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.4.
/// Requirements: 10.1.
/// </remarks>
public sealed class BowFireMovement
{
    private readonly BowFireMovementConfig _config;

    /// <param name="config">The fraction of full speed the player keeps while firing the Bow.</param>
    public BowFireMovement(BowFireMovementConfig config)
    {
        _config = config;
    }

    /// <summary>The configuration this decision was built with.</summary>
    public BowFireMovementConfig Config => _config;

    /// <summary>
    /// The fraction of full move speed the player keeps while firing, clamped strictly inside
    /// <c>(0, 1)</c>. Even if the configured value is authored at or beyond a bound (0, 1, negative,
    /// or above 1), the clamp keeps it strictly between them so the reduced-speed contract of
    /// Requisito 10.1 always holds.
    /// </summary>
    public float ClampedFraction => BowFireMovementConfig.ClampFraction(_config.MoveFraction);

    /// <summary>
    /// The speed the player is allowed to move at while firing the Bow, given their
    /// <paramref name="fullSpeed"/> (the speed the existing locomotion would use with no cast active).
    ///
    /// For any <paramref name="fullSpeed"/> &gt; 0 the result is strictly greater than 0 and strictly
    /// less than <paramref name="fullSpeed"/> (Requisito 10.1). A non-positive full speed has no
    /// movement to reduce, so the result is 0 (the player is already not moving; the Bow never
    /// creates movement out of a standstill).
    /// </summary>
    /// <param name="fullSpeed">The player's full move speed in m/s.</param>
    public float ReducedSpeed(float fullSpeed)
    {
        if (fullSpeed <= 0f) return 0f;
        return fullSpeed * ClampedFraction;
    }
}

/// <summary>
/// Data for the Arco's reduced-movement-while-firing rule (Requisito 10.1): the fraction of full
/// move speed the player keeps while shooting. Held as data (editable in the Inspector) rather than
/// as a magic number inside the coordinator, following AGENTS.md.
///
/// The fraction is interpreted strictly inside <c>(0, 1)</c>: the player moves, but slower than full
/// speed and never stopped. <see cref="ClampFraction"/> enforces that even for out-of-range authored
/// values, so <see cref="BowFireMovement"/> can never produce a full stop or full speed.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 12.4.
/// Requirements: 10.1.
/// </remarks>
[Serializable]
public struct BowFireMovementConfig
{
    /// <summary>
    /// The smallest gap (from 0 and from 1) the clamp keeps the fraction inside, so it is always
    /// <i>strictly</i> between 0 and 1 — never a dead stop, never full speed (Requisito 10.1).
    /// </summary>
    public const float Epsilon = 0.001f;

    /// <summary>Lowest fraction the clamp allows: strictly above 0.</summary>
    public const float MinFraction = Epsilon;

    /// <summary>Highest fraction the clamp allows: strictly below 1.</summary>
    public const float MaxFraction = 1f - Epsilon;

    /// <summary>
    /// The fraction of full move speed the player keeps while firing the Bow. Authored inside
    /// <c>(0, 1)</c>; clamped by <see cref="ClampFraction"/> if it strays to a bound or outside.
    /// </summary>
    [Range(0f, 1f)] public float MoveFraction;

    /// <summary>
    /// Clamps <paramref name="fraction"/> strictly inside <c>(0, 1)</c>: values at or below 0 become
    /// <see cref="MinFraction"/>, values at or above 1 become <see cref="MaxFraction"/>, and NaN
    /// falls back to a conventional half speed. This is the single guard that makes the reduced-speed
    /// contract of Requisito 10.1 hold for any authored value.
    /// </summary>
    public static float ClampFraction(float fraction)
    {
        if (float.IsNaN(fraction)) return 0.5f;
        return Mathf.Clamp(fraction, MinFraction, MaxFraction);
    }

    /// <summary>
    /// A conventional default: half of full speed while firing — clearly slower than sprinting into
    /// position, clearly not a full stop.
    /// </summary>
    public static BowFireMovementConfig Default => new BowFireMovementConfig { MoveFraction = 0.5f };
}
