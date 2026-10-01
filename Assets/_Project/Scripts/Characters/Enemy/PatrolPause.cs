using UnityEngine;

/// <summary>
/// Pure, scene-free idle-pause timer inserted between patrol walk points. Mirrors the
/// <see cref="PreferredDistanceResolver"/>/<c>TelegraphBeat</c> pattern so its invariants
/// ("a begun pause lasts a duration within the configured [min, max]", "a max of 0 never
/// pauses", "<see cref="Clear"/> exits immediately") can be property-tested without a live
/// Unity scene.
/// </summary>
/// <remarks>
/// Feature: ranged-kiting-and-attack-telegraph-overhaul, task 1.9. Requirements: 14.1, 14.2,
/// 14.3, 14.5, 14.6, 16.1, 16.2.
/// <para>
/// Driven by <c>EnemyAI.Patrol()</c>: on reaching a walk point it calls <see cref="Begin"/> and
/// holds position while <see cref="Tick"/> returns true (R14.1/R14.3); when the pause elapses the
/// enemy selects the next walk point through the existing random/ground-check path unchanged
/// (R14.4). When the player enters sight, dispatch leaves patrol and <see cref="Clear"/> ends the
/// pause immediately without waiting out the remaining duration (R14.5).
/// </para>
/// <para>
/// The pause duration is sampled from the explicit <see cref="System.Random"/> source so a given
/// seed and inputs produce identical, repeatable results per enemy (R16.1/R16.2). Inputs are
/// defensively clamped (min &gt;= 0, max &gt;= min) so a misconfigured range cannot produce a
/// negative pause.
/// </para>
/// </remarks>
public sealed class PatrolPause
{
    private float _remaining;
    private bool _paused;

    /// <summary>
    /// True while the enemy is currently holding position for an active pause. Mirrors the last
    /// value returned by <see cref="Tick"/>; false before any pause begins, after it elapses, and
    /// after <see cref="Clear"/>.
    /// </summary>
    public bool IsPaused => _paused;

    /// <summary>
    /// Begins a pause with a duration sampled uniformly from <c>[minSeconds, maxSeconds]</c>
    /// (R14.1/R14.2). The range is defensively clamped so <c>min &gt;= 0</c> and
    /// <c>max &gt;= min</c>. When the clamped maximum is 0 the enemy never pauses (R14.6): the
    /// pause ends immediately and <see cref="IsPaused"/> stays false.
    /// </summary>
    /// <param name="minSeconds">Minimum pause duration in seconds; clamped to be &gt;= 0.</param>
    /// <param name="maxSeconds">Maximum pause duration in seconds; clamped to be &gt;= the clamped minimum.</param>
    /// <param name="rng">Explicit random source so pause durations are deterministic per seed (R16.2).</param>
    public void Begin(float minSeconds, float maxSeconds, System.Random rng)
    {
        float min = Mathf.Max(0f, minSeconds);
        float max = Mathf.Max(min, maxSeconds);

        if (max <= 0f)
        {
            // max==0 => never pauses (R14.6).
            _remaining = 0f;
            _paused = false;
            return;
        }

        double sample = rng != null ? rng.NextDouble() : 0.0;
        _remaining = min + (float)sample * (max - min);
        _paused = _remaining > 0f;
    }

    /// <summary>
    /// Advances the active pause by <paramref name="dt"/>. Returns true while the pause is still
    /// running (hold position) and false once it has elapsed or no pause is active (R14.1/R14.3/
    /// R14.4). Negative deltas are ignored so time only moves forward.
    /// </summary>
    /// <param name="dt">Elapsed time in seconds since the previous tick.</param>
    /// <returns>True while still pausing; false once the pause has elapsed.</returns>
    public bool Tick(float dt)
    {
        if (!_paused)
        {
            return false;
        }

        if (dt > 0f)
        {
            _remaining -= dt;
        }

        if (_remaining <= 0f)
        {
            _remaining = 0f;
            _paused = false;
        }

        return _paused;
    }

    /// <summary>
    /// Ends any active pause immediately, e.g. when the player enters sight (R14.5). After this
    /// call <see cref="IsPaused"/> is false and <see cref="Tick"/> returns false until the next
    /// <see cref="Begin"/>.
    /// </summary>
    public void Clear()
    {
        _remaining = 0f;
        _paused = false;
    }
}
