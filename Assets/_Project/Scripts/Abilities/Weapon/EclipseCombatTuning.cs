using System;
using UnityEngine;

/// <summary>Raw authoring for the Phase C slice; validation never normalizes invalid values.</summary>
[Serializable]
public sealed class EclipseCombatTuning
{
    [SerializeField] private float _duration = .55f;
    [SerializeField] private float _reach = 2.4f;
    [SerializeField] private float _width = 2.8f;
    [SerializeField] private float _launchHeight = 1.5f;
    [SerializeField] private float _launchSeconds = 1.6f;
    [SerializeField] private float _markSeconds = 3f;
    [SerializeField] private float _fallRadius = 2.5f;
    [SerializeField] private float _fallDamageFraction = .35f;
    [SerializeField] private float _fallPostureFraction = .35f;
    [SerializeField] private int _maxMarks = 64;
    public bool TrySnapshot(out EclipseCombatSettings settings, out string diagnostic)
    {
        settings = null; diagnostic = "Invalid Eclipse timing, geometry, damage fraction or mark limit.";
        if (!Range(_duration, .1f, 5f) || !Range(_reach, .1f, 5f) || !Range(_width, .1f, 5f) ||
            !Range(_launchHeight, .01f, DisplacementTier.KnockUpMax) || !Range(_launchSeconds, .01f, 10f) ||
            !Range(_markSeconds, .1f, 10f) || !Range(_fallRadius, .1f, 5f) ||
            !Range(_fallDamageFraction, 0f, 1f) || !Range(_fallPostureFraction, 0f, 1f) || _maxMarks < 1 || _maxMarks > 64) return false;
        settings = new EclipseCombatSettings(_duration, _reach, _width, _launchHeight, _launchSeconds,
            _markSeconds, _fallRadius, _fallDamageFraction, _fallPostureFraction, _maxMarks);
        diagnostic = null; return true;
    }
    private static bool Range(float value, float min, float max)
        => !float.IsNaN(value) && !float.IsInfinity(value) && value >= min && value <= max;
}

public sealed class EclipseCombatSettings
{
    public float Duration { get; }
    public float Reach { get; }
    public float Width { get; }
    public float LaunchHeight { get; }
    public float LaunchSeconds { get; }
    public float MarkSeconds { get; }
    public float FallRadius { get; }
    public float FallDamageFraction { get; }
    public float FallPostureFraction { get; }
    public int MaxMarks { get; }
    internal EclipseCombatSettings(float duration, float reach, float width, float height, float launchSeconds,
        float markSeconds, float radius, float damageFraction, float postureFraction, int maxMarks)
    {
        Duration = duration; Reach = reach; Width = width; LaunchHeight = height; LaunchSeconds = launchSeconds;
        MarkSeconds = markSeconds; FallRadius = radius; FallDamageFraction = damageFraction;
        FallPostureFraction = postureFraction; MaxMarks = maxMarks;
    }
}

/// <summary>Single-use mark state, independent of Unity time, rendering, and target pooling.</summary>
public sealed class EclipseMarkState
{
    public float Remaining { get; private set; }
    public bool IsArmed { get; private set; }
    public EclipseMarkState(float seconds)
    {
        if (float.IsNaN(seconds) || float.IsInfinity(seconds) || seconds <= 0) throw new ArgumentOutOfRangeException(nameof(seconds));
        Remaining = seconds;
    }
    public void Advance(float delta)
    {
        if (float.IsNaN(delta) || float.IsInfinity(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        if (!IsArmed) Remaining = Mathf.Max(0, Remaining - delta);
    }
    public bool TryArm(bool nativeFinisher, bool sameProducer)
    {
        if (!nativeFinisher || sameProducer || IsArmed || Remaining <= 0) return false;
        IsArmed = true; return true;
    }
}
