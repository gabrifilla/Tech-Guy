using UnityEngine;

/// <summary>
/// Pure, scene-free model of the Spawner's living-produced-Swarm count. Extracted from the
/// <see cref="SpawnerBehavior"/> MonoBehaviour so the invariant "the living count stays within
/// [0, cap], never spawns while at cap, and decrements exactly once per produced-Swarm death"
/// can be property-tested without a live Unity scene (task 8.7, Property 9).
///
/// The struct holds only the concurrency arithmetic: the MonoBehaviour owns the Unity concerns
/// (timer, NavMesh sampling, instantiation, <c>Actor.Died</c> subscription) and asks this model
/// whether a spawn is permitted and records spawns / deaths through it.
/// </summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes, task 8.6.
/// Requirements: 16.2, 16.3, 16.4, 16.5.
/// </remarks>
public struct SpawnerLivingCount
{
    /// <summary>Inclusive lower bound for the configured living cap (R16.3).</summary>
    public const int MinCap = 1;

    /// <summary>Inclusive upper bound for the configured living cap (R16.3).</summary>
    public const int MaxCap = 100;

    private readonly int _cap;
    private int _living;

    /// <summary>
    /// Creates a living-count model with the given cap, clamped into the valid 1..100 range (R16.3).
    /// The living count starts at zero.
    /// </summary>
    /// <param name="cap">The configured maximum number of concurrently living produced Swarms.</param>
    public SpawnerLivingCount(int cap)
    {
        _cap = Mathf.Clamp(cap, MinCap, MaxCap);
        _living = 0;
    }

    /// <summary>The effective living cap after clamping to the valid range (R16.3).</summary>
    public int Cap => _cap;

    /// <summary>The current count of concurrently living produced Swarms. Always in [0, <see cref="Cap"/>].</summary>
    public int Living => _living;

    /// <summary>
    /// True when a spawn is permitted this attempt: the current living count is strictly below the
    /// cap. At the cap no further spawn is allowed until a death lowers the count (R16.4).
    /// </summary>
    public bool CanSpawn => _living < _cap;

    /// <summary>
    /// Records a successful spawn by incrementing the living count. No-op when already at the cap so
    /// the count can never exceed it (R16.2, R16.3, R16.4). Callers should gate on <see cref="CanSpawn"/>
    /// before instantiating; this guard keeps the invariant even under an unexpected call order.
    /// </summary>
    /// <returns>True if the spawn was recorded; false if the count was already at the cap.</returns>
    public bool RecordSpawn()
    {
        if (_living >= _cap) return false;
        _living++;
        return true;
    }

    /// <summary>
    /// Records the death of one produced Swarm by decrementing the living count, never below zero
    /// (R16.5). Each produced Swarm's <c>Died</c> event maps to exactly one call.
    /// </summary>
    /// <returns>True if a death was recorded; false if the count was already zero.</returns>
    public bool RecordDeath()
    {
        if (_living <= 0) return false;
        _living--;
        return true;
    }
}
