using System;

/// <summary>Confirmed damage merged only within a fixed, short presentation window per victim.</summary>
/// <remarks>The caller supplies its existing scaled presentation clock. No combat events are emitted.</remarks>
public sealed class DamageNumberBurst
{
    public const float MergeWindowSeconds = 0.12f;
    private double _amount;
    private bool _started;
    public float Amount => (float)Math.Min(_amount, float.MaxValue);
    public bool IsLethal { get; private set; }
    public float BornAt { get; private set; }
    public int HitCount { get; private set; }

    public static bool IsValidAmount(float amount) => amount > 0f && IsFinite(amount);
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    public bool TryBegin(float amount, bool lethal, float now)
    {
        if (_started || !IsValidAmount(amount) || !IsFinite(now)) return false;
        _started = true;
        _amount = amount;
        IsLethal = lethal;
        BornAt = now;
        HitCount = 1;
        return true;
    }

    public bool TryAccumulate(float amount, bool lethal, float now)
    {
        if (!_started || IsLethal || !IsValidAmount(amount) || !IsFinite(now) ||
            now < BornAt || now - BornAt > MergeWindowSeconds) return false;
        _amount += amount;
        IsLethal |= lethal;
        HitCount++;
        return true;
    }
}
