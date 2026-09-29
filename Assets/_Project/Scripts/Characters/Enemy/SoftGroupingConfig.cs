using UnityEngine;

/// <summary>
/// Serializable, data-driven caps for the soft-grouping behavior (feature
/// weapon-gameplay-swarm-rework, Requirement 7). Holds the three tunable limits — max speed, radius
/// and max displacement per application — while the R7.1 ceilings are pinned as validation
/// constants so no configuration can exceed the design's readability contract.
/// </summary>
/// <remarks>
/// Feature: weapon-gameplay-swarm-rework, task 8.1.
/// Requirements: 7.1, 7.5.
/// Values remain editable in the Inspector (data are the only source of tunable numbers), but they
/// are always clamped to the R7.1 caps via <see cref="OnValidate"/> and re-clamped by the read-only
/// accessors, keeping <see cref="SoftGroupingCalculator"/> honest even for hand-edited assets.
/// </remarks>
[System.Serializable]
public sealed class SoftGroupingConfig
{
    // R7.1 caps — the design's hard ceilings. Configuration may lower these but never exceed them.
    /// <summary>R7.1 ceiling on grouping speed: at most 1.0 m/s.</summary>
    public const float MaxSpeedCap = 1.0f;
    /// <summary>R7.1 grouping radius: enemies farther than 3.0 m from the group point are not moved.</summary>
    public const float RadiusCap = 3.0f;
    /// <summary>R7.1 ceiling on per-application displacement: at most 0.5 m.</summary>
    public const float MaxDisplacementPerApplicationCap = 0.5f;

    [SerializeField, Range(0f, MaxSpeedCap)] private float maxSpeed = MaxSpeedCap;
    [SerializeField, Range(0f, RadiusCap)] private float radius = RadiusCap;
    [SerializeField, Range(0f, MaxDisplacementPerApplicationCap)] private float maxDisplacementPerApplication = MaxDisplacementPerApplicationCap;

    /// <summary>Maximum grouping speed in m/s, clamped to [0, <see cref="MaxSpeedCap"/>] (R7.1).</summary>
    public float MaxSpeed => Mathf.Clamp(maxSpeed, 0f, MaxSpeedCap);

    /// <summary>Grouping radius in metres, clamped to [0, <see cref="RadiusCap"/>] (R7.1).</summary>
    public float Radius => Mathf.Clamp(radius, 0f, RadiusCap);

    /// <summary>
    /// Maximum displacement per application in metres, clamped to
    /// [0, <see cref="MaxDisplacementPerApplicationCap"/>] (R7.1).
    /// </summary>
    public float MaxDisplacementPerApplication
        => Mathf.Clamp(maxDisplacementPerApplication, 0f, MaxDisplacementPerApplicationCap);

    /// <summary>
    /// A config produces movement only when it has a positive radius, a positive per-application
    /// budget and a positive speed; otherwise the calculator treats it as a no-op.
    /// </summary>
    public bool IsValid => Radius > 0f && MaxDisplacementPerApplication > 0f && MaxSpeed > 0f;

    /// <summary>Default configuration pinned to the R7.1 caps (1.0 m/s, 3.0 m, 0.5 m).</summary>
    public SoftGroupingConfig() { }

    /// <summary>Builds a configuration, clamping each value to its R7.1 ceiling.</summary>
    public SoftGroupingConfig(float maxSpeed, float radius, float maxDisplacementPerApplication)
    {
        this.maxSpeed = Mathf.Clamp(maxSpeed, 0f, MaxSpeedCap);
        this.radius = Mathf.Clamp(radius, 0f, RadiusCap);
        this.maxDisplacementPerApplication = Mathf.Clamp(maxDisplacementPerApplication, 0f, MaxDisplacementPerApplicationCap);
    }

    /// <summary>Re-clamps serialized fields to the R7.1 caps when edited in the Inspector.</summary>
    public void OnValidate()
    {
        maxSpeed = Mathf.Clamp(maxSpeed, 0f, MaxSpeedCap);
        radius = Mathf.Clamp(radius, 0f, RadiusCap);
        maxDisplacementPerApplication = Mathf.Clamp(maxDisplacementPerApplication, 0f, MaxDisplacementPerApplicationCap);
    }
}
