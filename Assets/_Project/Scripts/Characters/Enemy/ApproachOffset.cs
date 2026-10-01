using UnityEngine;

/// <summary>
/// Pure, scene-free resolver for the bounded chase-approach displacement added to an enemy's chase
/// destination so a group closing in reads as a crowd rather than a dead-straight formation. Mirrors
/// the <see cref="PreferredDistanceResolver"/> / <c>TelegraphBeat</c> pattern: all randomness is driven
/// by an explicit <see cref="System.Random"/> so a given seed and input sequence are deterministic and
/// property-testable without a live Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.7. Requirements: 13.2, 13.3, 13.8, 16.1, 16.2.
/// <para>
/// The offset lies on the ground plane (<c>y == 0</c>) and its magnitude never exceeds the configured
/// <paramref name="magnitude"/> clamped to [<see cref="MinMagnitude"/>, <see cref="MaxMagnitude"/>]. It is
/// refreshed to a fresh seeded sample only when the refresh interval elapses; between refreshes the prior
/// sample is held fixed so the enemy does not jitter frame to frame (R13.3). A configured magnitude of 0
/// yields <see cref="Vector3.zero"/> so the enemy chases directly toward the player (R13.8). The MonoBehaviour
/// is responsible for NavMesh sampling and the off-mesh fallback (R13.6); this class only computes the offset.
/// </para>
/// </remarks>
public sealed class ApproachOffset
{
    /// <summary>Minimum configurable offset magnitude, in units (R13.2).</summary>
    public const float MinMagnitude = 0f;

    /// <summary>Maximum configurable offset magnitude, in units (R13.2).</summary>
    public const float MaxMagnitude = 4f;

    /// <summary>Minimum configurable refresh interval, in seconds (R13.3).</summary>
    public const float MinRefresh = 0.1f;

    /// <summary>Maximum configurable refresh interval, in seconds (R13.3).</summary>
    public const float MaxRefresh = 10f;

    private Vector3 _current = Vector3.zero;
    private float _elapsed;
    private bool _hasSample;

    /// <summary>
    /// Advances the offset by <paramref name="dt"/> seconds and returns the current bounded ground-plane
    /// offset vector. On the first call, and whenever the refresh interval has elapsed since the last
    /// refresh, a new seeded sample is drawn; otherwise the prior sample is held fixed (R13.3). The returned
    /// vector always has magnitude &le; the clamped <paramref name="magnitude"/>, lies on the ground plane
    /// (<c>y == 0</c>), and equals <see cref="Vector3.zero"/> when the clamped magnitude is 0 (R13.8).
    /// Deterministic: identical (dt, magnitude, refreshInterval) sequences and an identically seeded
    /// <paramref name="rng"/> produce identical results (R16.1, R16.2).
    /// </summary>
    /// <param name="dt">Elapsed time since the last tick, in seconds. Non-positive values do not advance the refresh timer.</param>
    /// <param name="magnitude">The configured offset magnitude; defensively clamped to [<see cref="MinMagnitude"/>, <see cref="MaxMagnitude"/>].</param>
    /// <param name="refreshInterval">The configured refresh interval; defensively clamped to [<see cref="MinRefresh"/>, <see cref="MaxRefresh"/>].</param>
    /// <param name="rng">The explicit random source driving the sample; must not be null.</param>
    /// <returns>The current bounded ground-plane offset to add to the chase destination.</returns>
    public Vector3 Tick(float dt, float magnitude, float refreshInterval, System.Random rng)
    {
        float clampedMagnitude = Mathf.Clamp(magnitude, MinMagnitude, MaxMagnitude);
        float clampedRefresh = Mathf.Clamp(refreshInterval, MinRefresh, MaxRefresh);

        // A zero magnitude means a dead-straight beeline: no offset, no retained sample (R13.8).
        if (clampedMagnitude <= 0f)
        {
            _current = Vector3.zero;
            _hasSample = false;
            _elapsed = 0f;
            return Vector3.zero;
        }

        if (dt > 0f)
        {
            _elapsed += dt;
        }

        if (!_hasSample || _elapsed >= clampedRefresh)
        {
            _current = Sample(clampedMagnitude, rng);
            _elapsed = 0f;
            _hasSample = true;
        }

        return _current;
    }

    /// <summary>
    /// Clears the retained sample and refresh timer so the next <see cref="Tick"/> draws a fresh sample.
    /// </summary>
    public void Reset()
    {
        _current = Vector3.zero;
        _elapsed = 0f;
        _hasSample = false;
    }

    /// <summary>
    /// Draws a bounded ground-plane sample: a uniformly random heading with a radius uniform in
    /// [0, <paramref name="magnitude"/>], guaranteeing the returned vector's magnitude never exceeds the
    /// configured magnitude. The sample lies on the ground plane (<c>y == 0</c>).
    /// </summary>
    private static Vector3 Sample(float magnitude, System.Random rng)
    {
        double angle = (rng?.NextDouble() ?? 0d) * (System.Math.PI * 2d);
        float radius = (float)((rng?.NextDouble() ?? 0d) * magnitude);
        float x = radius * (float)System.Math.Cos(angle);
        float z = radius * (float)System.Math.Sin(angle);
        return new Vector3(x, 0f, z);
    }
}
