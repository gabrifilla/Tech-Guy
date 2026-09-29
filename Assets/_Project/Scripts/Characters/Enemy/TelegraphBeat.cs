using System;
using UnityEngine;

/// <summary>
/// Pure, scene-free telegraph logic shared by <see cref="EnemyAttackExecution"/> and the
/// archetype role behaviors. Extracted so the universal telegraph invariants can be
/// property-tested without a live Unity scene.
///
/// Three pieces:
///   * <see cref="TelegraphWindupClock"/> — enforces the windup floor (min 0.25s) and the
///     controlling-vs-non-controlling windup ordering, and exposes the monotonic 0->1 fraction.
///   * <see cref="TelegraphIntensity"/> — maps the windup fraction to a monotonically advancing
///     0->1 intensity used to lerp the telegraph appearance toward its impact appearance.
///   * <see cref="SingleBeatResolver"/> — resolves at most one damage beat per attack across the
///     union of telegraphed areas, mirroring the existing single-beat rule.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 2.1, 2.2, 2.3, 2.5, 2.6.</remarks>
public static class TelegraphBeat
{
    /// <summary>The minimum telegraph windup, in seconds, before any damage beat may resolve (R2.1).</summary>
    public const float MinWindupSeconds = 0.25f;
}

/// <summary>
/// Models the telegraph windup timeline: the enforced floor, the controlling-attack ordering, and
/// a monotonically non-decreasing fraction advancing from 0 (initial appearance) to 1 (impact).
/// Contains no Unity scene dependency; time is fed in explicitly so it is deterministically testable.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 2.1, 2.2, 2.3, 2.6.</remarks>
public readonly struct TelegraphWindupClock
{
    /// <summary>The effective windup duration after applying the 0.25s floor (R2.1).</summary>
    public float Duration { get; }

    /// <summary>Whether this attack applies hard crowd-control or player displacement (R2.2).</summary>
    public bool IsControlling { get; }

    /// <summary>
    /// Builds a windup clock from an authored windup value, clamped to the 0.25s floor (R2.1).
    /// </summary>
    public TelegraphWindupClock(float authoredWindup, bool isControlling = false)
    {
        Duration = Mathf.Max(TelegraphBeat.MinWindupSeconds, authoredWindup);
        IsControlling = isControlling;
    }

    /// <summary>
    /// Returns the effective windup for a controlling (Stun / KnockUp / Knockback / pull / hook)
    /// attack, guaranteeing it is at least as long as its non-controlling equivalent and never
    /// shorter than the 0.25s floor (R2.2).
    /// </summary>
    /// <param name="authoredWindup">The attack's own authored windup.</param>
    /// <param name="nonControllingWindup">The equivalent non-controlling attack's effective windup.</param>
    public static TelegraphWindupClock Controlling(float authoredWindup, float nonControllingWindup)
    {
        float floor = Mathf.Max(TelegraphBeat.MinWindupSeconds,
            Mathf.Max(TelegraphBeat.MinWindupSeconds, nonControllingWindup));
        return new TelegraphWindupClock(Mathf.Max(authoredWindup, floor), true);
    }

    /// <summary>
    /// The windup fraction at <paramref name="elapsed"/> seconds, clamped to [0, 1] and
    /// monotonically non-decreasing as elapsed time advances (R2.3, R2.6).
    /// </summary>
    public float FractionAt(float elapsed)
    {
        if (Duration <= 0f) return 1f;
        return Mathf.Clamp01(elapsed / Duration);
    }

    /// <summary>True once the windup has fully elapsed and the damage beat may resolve.</summary>
    public bool IsComplete(float elapsed) => elapsed >= Duration;
}

/// <summary>
/// Maps a windup fraction (0->1) to the telegraph's visual intensity, advancing monotonically from
/// its initial appearance toward its impact appearance. Mirrors the existing color lerp in
/// <see cref="EnemyAttackExecution"/> (<c>Color.Lerp(color, white, 0.65f * fraction)</c>).
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 2.3, 2.6.</remarks>
public readonly struct TelegraphIntensity
{
    /// <summary>How far toward the impact appearance the telegraph lerps at full windup (matches the existing 0.65 factor).</summary>
    public const float ImpactLerp = 0.65f;

    /// <summary>
    /// The intensity at the given windup fraction: 0 at the start, increasing monotonically to
    /// <see cref="ImpactLerp"/> at fraction 1 (R2.3). Never decreases as the fraction advances.
    /// </summary>
    public static float At(float fraction) => ImpactLerp * Mathf.Clamp01(fraction);

    /// <summary>
    /// The color to draw the telegraph at the given windup fraction, lerping from
    /// <paramref name="baseColor"/> toward <paramref name="impactColor"/> (white by default),
    /// matching the appearance produced by <see cref="EnemyAttackExecution"/>.
    /// </summary>
    public static Color ColorAt(Color baseColor, float fraction, Color impactColor)
        => Color.Lerp(baseColor, impactColor, At(fraction));

    /// <summary>Overload using white as the impact color, matching the existing default.</summary>
    public static Color ColorAt(Color baseColor, float fraction)
        => ColorAt(baseColor, fraction, Color.white);
}

/// <summary>
/// Resolves at most one damage beat per attack across the union of an attack's telegraphed areas,
/// consistent with the existing single-beat rule in <see cref="EnemyAttackExecution"/>: overlapping
/// or crossing areas still hit a given target only once per beat (R2.5). Once a beat has resolved
/// for this attack, no further beat is produced.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 2.5.</remarks>
public sealed class SingleBeatResolver
{
    private bool _beatResolved;

    /// <summary>True once this attack has already produced its single damage beat.</summary>
    public bool BeatResolved => _beatResolved;

    /// <summary>
    /// Attempts the single damage beat for this attack. Returns <c>true</c> and marks the beat as
    /// resolved exactly once when the point lies inside at least one area of the union and the
    /// damage is positive; returns <c>false</c> on every subsequent call, when damage is
    /// non-positive, or when the point is outside every area. This is the pure equivalent of the
    /// "iterate areas, apply once, break" loop in <see cref="EnemyAttackExecution"/>.
    /// </summary>
    /// <param name="areas">The union of telegraphed areas for this attack.</param>
    /// <param name="point">The target position tested against the union.</param>
    /// <param name="damage">The beat's damage; a non-positive value never produces a beat.</param>
    public bool TryResolve(EnemyAttackArea[] areas, Vector3 point, float damage)
    {
        if (_beatResolved) return false;
        if (damage <= 0f || areas == null) return false;
        if (!InUnion(areas, point)) return false;
        _beatResolved = true;
        return true;
    }

    /// <summary>Whether <paramref name="point"/> lies inside the union of the given areas.</summary>
    public static bool InUnion(EnemyAttackArea[] areas, Vector3 point)
    {
        if (areas == null) return false;
        for (int i = 0; i < areas.Length; i++)
            if (areas[i].Contains(point)) return true;
        return false;
    }

    /// <summary>Resets the resolver so it can be reused for a fresh attack.</summary>
    public void Reset() => _beatResolved = false;
}
