using UnityEngine;

/// <summary>One short-lived ground intent; independent from offensive cancel/buffer ownership.</summary>
internal sealed class GroundMovementBuffer
{
    private Vector3 _destination;
    private float _expiresAt;
    internal bool HasPending { get; private set; }
    internal bool Store(Vector3 destination, float clock, float lifetime)
    {
        if (!IsFinite(destination) || !IsFinite(clock) || !IsFinite(lifetime) || lifetime <= 0f) return false;
        float expiresAt = clock + lifetime;
        if (!IsFinite(expiresAt)) return false;
        _destination = destination;
        _expiresAt = expiresAt;
        HasPending = true;
        return true;
    }
    internal bool TryTake(float clock, out Vector3 destination)
    {
        destination = default;
        if (!HasPending || !IsFinite(clock)) return false;
        if (clock > _expiresAt) { Discard(); return false; }
        destination = _destination;
        Discard();
        return true;
    }
    internal void Discard() { HasPending = false; }
    internal static bool IsFinite(Vector3 value) => IsFinite(value.x) && IsFinite(value.y) && IsFinite(value.z);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
