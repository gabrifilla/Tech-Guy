using UnityEngine;

/// <summary>
/// Pure, scene-free logic for a <see cref="HazardZone"/>. Extracted from the MonoBehaviour so the two
/// universal hazard invariants can be property-tested without a live Unity scene (task 8.2,
/// Property 10):
///   * <see cref="HazardTickTimer"/> — the fixed-interval accumulator that decides how many periodic
///     damage ticks a fire/electric zone owes while the player occupies it (R11.5), and which stops
///     accumulating the moment the player leaves (R11.6).
///   * <see cref="HazardSlow"/> — the movement-slow round trip: applying the slow reduces speed by a
///     bounded factor (R11.7) and removing it restores the exact pre-slow value (R11.8), so an
///     enter-then-exit is a speed identity.
/// The MonoBehaviour maps these decisions onto Unity — the trigger volume, the actual
/// <see cref="Actor.TakeDamage"/> call, and the player-stat modifier that carries the slow.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes, task 8.1. Requirements: 11.5, 11.6, 11.7, 11.8.</remarks>
public static class HazardZoneModel
{
    /// <summary>The smallest allowed periodic-damage interval, in seconds (R11.5).</summary>
    public const float MinDamageInterval = 0.25f;

    /// <summary>The largest allowed periodic-damage interval, in seconds (R11.5).</summary>
    public const float MaxDamageInterval = 1.0f;

    /// <summary>The smallest allowed slow, expressed as the fraction of speed removed (R11.7).</summary>
    public const float MinSlowFraction = 0.20f;

    /// <summary>The largest allowed slow, expressed as the fraction of speed removed (R11.7).</summary>
    public const float MaxSlowFraction = 0.60f;

    /// <summary>The smallest allowed zone lifetime, in seconds (R11.9).</summary>
    public const float MinLifetime = 3f;

    /// <summary>The largest allowed zone lifetime, in seconds (R11.9).</summary>
    public const float MaxLifetime = 15f;

    /// <summary>Clamps a configured damage interval to the design's 0.25–1.0s band (R11.5).</summary>
    public static float ClampInterval(float interval) => Mathf.Clamp(interval, MinDamageInterval, MaxDamageInterval);

    /// <summary>Clamps a configured slow to the design's 20%–60% band (R11.7).</summary>
    public static float ClampSlowFraction(float slowFraction) => Mathf.Clamp(slowFraction, MinSlowFraction, MaxSlowFraction);

    /// <summary>Clamps a configured zone lifetime to the design's 3–15s band (R11.9).</summary>
    public static float ClampLifetime(float lifetime) => Mathf.Clamp(lifetime, MinLifetime, MaxLifetime);
}

/// <summary>
/// Fixed-interval tick accumulator for a fire/electric hazard zone. While the player occupies the
/// zone, elapsed time is accumulated and converted into whole damage ticks, one per fixed interval
/// (R11.5). The moment the player leaves, <see cref="Exit"/> clears the accumulator so no further
/// ticks are owed (R11.6); a fresh entry starts the interval over.
///
/// The timer never emits a tick on entry (occupancy must accrue a full interval first), never emits
/// a partial tick, and — for a bounded elapsed time — never emits more ticks than the elapsed time
/// divides into whole intervals. This keeps periodic damage strictly interval-driven rather than
/// per-frame.
/// </summary>
public sealed class HazardTickTimer
{
    private readonly float _interval;
    private float _accumulator;
    private bool _occupied;

    /// <summary>Builds a tick timer with an interval clamped to the 0.25–1.0s band (R11.5).</summary>
    public HazardTickTimer(float interval)
    {
        _interval = HazardZoneModel.ClampInterval(interval);
    }

    /// <summary>The fixed interval between ticks, in seconds, after band-clamping.</summary>
    public float Interval => _interval;

    /// <summary>Whether the player is currently considered inside the zone.</summary>
    public bool Occupied => _occupied;

    /// <summary>Seconds accrued toward the next tick but not yet spent.</summary>
    public float Pending => _accumulator;

    /// <summary>
    /// Marks the player as having entered the zone. Occupancy starts a fresh interval: no tick is
    /// owed on entry, so the accumulator resets to 0. Re-entering an already-occupied zone is a no-op.
    /// </summary>
    public void Enter()
    {
        if (_occupied) return;
        _occupied = true;
        _accumulator = 0f;
    }

    /// <summary>
    /// Marks the player as having left the zone and clears any partial progress so no further ticks
    /// are owed once outside (R11.6). Leaving an empty zone is a no-op.
    /// </summary>
    public void Exit()
    {
        _occupied = false;
        _accumulator = 0f;
    }

    /// <summary>
    /// Advances the timer by <paramref name="deltaTime"/> seconds and returns how many whole damage
    /// ticks came due this step. Returns 0 when the zone is unoccupied or when less than a full
    /// interval has accrued; the leftover time is carried forward so ticks stay evenly spaced (R11.5).
    /// A non-positive delta or interval yields no ticks.
    /// </summary>
    public int Advance(float deltaTime)
    {
        if (!_occupied || deltaTime <= 0f || _interval <= 0f) return 0;

        _accumulator += deltaTime;
        if (_accumulator < _interval) return 0;

        int ticks = Mathf.FloorToInt(_accumulator / _interval);
        _accumulator -= ticks * _interval;
        return ticks;
    }
}

/// <summary>
/// The movement-slow round trip for a slow hazard zone. Applying the slow multiplies the player's
/// speed by <c>(1 - fraction)</c> for a bounded fraction in the 20%–60% band (R11.7); removing it
/// restores the exact pre-slow value (R11.8). The scene-side <see cref="HazardZone"/> carries this
/// slow as a source-keyed <c>PlayerStatModifier</c> whose add/remove is a multiplicative identity,
/// so this model captures the same math: <see cref="RestoredSpeed"/> after an <see cref="ApplyTo"/>
/// followed by <see cref="Restore"/> equals the original speed for any base speed.
/// </summary>
public readonly struct HazardSlow
{
    /// <summary>The band-clamped fraction of speed the slow removes while occupied (R11.7).</summary>
    public float Fraction { get; }

    /// <summary>Builds a slow with a fraction clamped to the 20%–60% band (R11.7).</summary>
    public HazardSlow(float fraction)
    {
        Fraction = HazardZoneModel.ClampSlowFraction(fraction);
    }

    /// <summary>The multiplier applied to speed while the player occupies the zone: <c>1 - fraction</c>.</summary>
    public float SpeedMultiplier => 1f - Fraction;

    /// <summary>The slowed speed for a given pre-slow <paramref name="baseSpeed"/> (R11.7).</summary>
    public float ApplyTo(float baseSpeed) => baseSpeed * SpeedMultiplier;

    /// <summary>
    /// The speed after leaving the zone, given the <paramref name="baseSpeed"/> captured before the
    /// slow was applied. The slow is removed as a whole, so this is exactly the pre-slow value (R11.8).
    /// </summary>
    public float Restore(float baseSpeed) => baseSpeed;

    /// <summary>
    /// The speed restored after an apply-then-remove round trip on <paramref name="baseSpeed"/>.
    /// Provided so a property test can assert the round trip is a speed identity (R11.8): entering
    /// then leaving returns the exact original speed regardless of the slow fraction or base speed.
    /// </summary>
    public float RoundTrip(float baseSpeed) => Restore(baseSpeed);
}
