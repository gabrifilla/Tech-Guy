using UnityEngine;

/// <summary>Pure retreat window/cooldown clock. All durations use the supplied combat delta time.</summary>
/// <remarks>
/// Gameplay fluidity revision, authorized 2026-10-11: crossing the distance threshold never erases
/// cooldown. Every completed/blocked retreat opens the same punishable cooldown, including arrival.
/// Reset is reserved for lifecycle cleanup, not perception or distance changes.
/// </remarks>
public sealed class KiteController
{
    public enum Phase { Eligible, Kiting, Cooldown }
    private Phase _current = Phase.Eligible;
    private float _kiteTime;
    private float _cooldownTime;
    public Phase Current => _current;

    public bool Tick(bool wantsToKite, float dt, float retreatWindow, float retreatCooldown)
    {
        float step = FiniteNonNegative(dt);
        float window = PositiveDuration(retreatWindow);
        float cooldown = PositiveDuration(retreatCooldown);

        // The cooldown clock has priority over the distance gate (R3.3).
        if (_current == Phase.Cooldown)
        {
            _cooldownTime += step;
            if (_cooldownTime < cooldown) return false;
            _current = Phase.Eligible;
            _cooldownTime = 0f;
        }

        if (!wantsToKite)
        {
            if (_current == Phase.Kiting) EndRetreat();
            return false;
        }

        if (_current == Phase.Eligible)
        {
            _current = Phase.Kiting;
            _kiteTime = 0f;
            return true;
        }

        _kiteTime += step;
        if (_kiteTime >= window)
        {
            EndRetreat();
            return false;
        }
        return true;
    }

    /// <summary>Arrival, obstacle, displacement budget or interruption ends retreat and starts cooldown.</summary>
    public void EndRetreat()
    {
        if (_current != Phase.Kiting) return;
        _current = Phase.Cooldown;
        _kiteTime = 0f;
        _cooldownTime = 0f;
    }

    public void Reset()
    {
        _current = Phase.Eligible;
        _kiteTime = 0f;
        _cooldownTime = 0f;
    }

    private static float FiniteNonNegative(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? 0f : value;
    private static float PositiveDuration(float value) =>
        float.IsNaN(value) || float.IsInfinity(value) || value <= 0f ? Mathf.Epsilon : value;
}
