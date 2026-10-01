using UnityEngine;

/// <summary>
/// Pure, scene-free resolution of an enemy's jittered attack interval. Extracted from
/// <see cref="EnemyAI"/> so the universal invariants "the effective interval stays within the
/// jitter band", "it never drops below the recovery/min-interval floor", "it is always &gt; 0",
/// and "it is deterministic per seed" (Property 3, Requirements 12.2/12.3/12.4/12.5/16.2/16.4)
/// can be property-tested without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.5. Requirements: 12.1, 12.2, 12.3,
/// 12.4, 12.5, 12.6, 16.1, 16.2.
/// <para>
/// Mirrors the existing <see cref="PreferredDistanceResolver"/> / <see cref="TelegraphBeat"/> pure
/// pattern: a <see langword="static"/> resolver with explicit inputs (including an explicit
/// <see cref="System.Random"/> source so a given seed and input produce identical output, R16.2) and
/// defensive input clamping so an out-of-range serialized jitter can never produce an out-of-band
/// interval (R16.6). It never introduces a new timing channel: the base interval and floor are the
/// same <c>Mathf.Max(_attackRecovery, timeBetweenAttacks)</c> values <c>EnemyAI</c> already uses, so
/// the existing recovery guarantee is preserved (R12.4).
/// </para>
/// </remarks>
public static class CadenceJitter
{
    /// <summary>Minimum configurable jitter fraction (no variation) (R12.2).</summary>
    public const float MinJitter = 0f;

    /// <summary>Maximum configurable jitter fraction (plus or minus 50 percent) (R12.2).</summary>
    public const float MaxJitter = 0.5f;

    /// <summary>
    /// Clamps the configured jitter fraction to the inclusive range [<see cref="MinJitter"/>,
    /// <see cref="MaxJitter"/>] (R12.2), so an out-of-range serialized value can never widen the
    /// jitter band beyond its documented bounds.
    /// </summary>
    public static float ClampJitter(float configured) => Mathf.Clamp(configured, MinJitter, MaxJitter);

    /// <summary>
    /// Resolves the effective attack interval by applying a bounded random jitter to
    /// <paramref name="baseInterval"/>. The result is sampled uniformly within
    /// <c>[base*(1-j), base*(1+j)]</c> where <c>j</c> is the clamped jitter fraction (R12.3), floored
    /// at <c>max(floor, Epsilon)</c> so the existing recovery/min-interval guarantee is preserved and
    /// the interval is always strictly greater than 0 (R12.4/R12.6). When
    /// <paramref name="jitterFraction"/> is 0 the base interval is returned exactly, matching the
    /// existing deterministic timing (R12.5). Deterministic for identical inputs and the same
    /// <paramref name="rng"/> sequence (R16.2/R16.4).
    /// </summary>
    /// <param name="baseInterval">The base attack cadence (seconds) before jitter is applied.</param>
    /// <param name="jitterFraction">The fractional variation; clamped to [0, 0.5] (R12.2).</param>
    /// <param name="floor">The minimum allowed interval (e.g. <c>Max(_attackRecovery, timeBetweenAttacks)</c>).</param>
    /// <param name="rng">The explicit random source; when null the base interval (clamped to the floor) is returned.</param>
    /// <returns>The effective interval, always &gt; 0 and never below the effective floor.</returns>
    public static float Effective(float baseInterval, float jitterFraction, float floor, System.Random rng)
    {
        // The interval can never be <= 0: a non-positive floor still clamps up to Epsilon (R12.4/R12.6).
        float effectiveFloor = Mathf.Max(floor, Mathf.Epsilon);

        // No variation requested (or no random source): return the base cadence exactly, floored (R12.5).
        float clampedJitter = ClampJitter(jitterFraction);
        if (clampedJitter <= 0f || rng == null)
        {
            return Mathf.Max(baseInterval, effectiveFloor);
        }

        // Sample uniformly in [base*(1-j), base*(1+j)] (R12.3).
        float safeBase = Mathf.Max(0f, baseInterval);
        float min = safeBase * (1f - clampedJitter);
        float max = safeBase * (1f + clampedJitter);
        float sampled = min + (float)rng.NextDouble() * (max - min);

        // Floor the sampled interval; the result is always > 0 (R12.4/R12.6).
        return Mathf.Max(sampled, effectiveFloor);
    }
}
