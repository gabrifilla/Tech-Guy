using System;
using UnityEngine;

/// <summary>
/// Data configuration for the Lança's Sweet Spot mechanic (Requisito 9.2). It describes, per thrust
/// ability, the tip region relative to the spear tip and the bonus multipliers granted to a direct
/// thrust hit that lands inside that region.
///
/// The region is expressed relative to the thrust geometry so the same config works regardless of the
/// weapon's absolute reach:
/// <list type="bullet">
/// <item><description><see cref="TipFraction"/> — the trailing fraction of the reach, measured back from
/// the tip along the thrust axis, that counts as the Sweet Spot (e.g. 0.2 = the last 20% of the reach).</description></item>
/// <item><description><see cref="LateralRadius"/> — how far off the thrust axis a hit may land and still
/// count as a tip hit (a thrust is a line, but hits have some width).</description></item>
/// </list>
///
/// The bonus multipliers apply on top of a baseline of 1.0 and are only meaningful inside the region.
/// To honour Requisito 9.2 ("strictly greater than a hit outside the Sweet Spot") they are validated to
/// be strictly greater than 1.0; <see cref="IsValid"/> reports whether a config satisfies that contract.
///
/// This is a plain, serializable value type with no scene dependency, so it can be embedded on an
/// ability asset and exercised in isolation. It is consumed by <see cref="SweetSpot"/>.
/// </summary>
[Serializable]
public struct SweetSpotConfig
{
    /// <summary>The multiplier baseline applied to a hit that lands outside the Sweet Spot (no bonus).</summary>
    public const float OutsideMultiplier = 1f;

    /// <summary>Smallest positive amount by which an inside bonus must exceed the outside baseline.</summary>
    public const float MinBonusEpsilon = 0.0001f;

    [SerializeField, Range(0f, 1f), Tooltip("Trailing fraction of the reach (measured back from the tip) that counts as the Sweet Spot.")]
    private float _tipFraction;

    [SerializeField, Min(0f), Tooltip("How far off the thrust axis a hit may land and still count as a tip hit, in metres.")]
    private float _lateralRadius;

    [SerializeField, Tooltip("Stance-damage bonus multiplier inside the Sweet Spot. Must be strictly greater than 1.")]
    private float _stanceBonus;

    [SerializeField, Tooltip("Resource-gain bonus multiplier inside the Sweet Spot. Must be strictly greater than 1.")]
    private float _resourceBonus;

    /// <summary>
    /// Builds a Sweet Spot configuration.
    /// </summary>
    /// <param name="tipFraction">Trailing fraction of the reach counted as the tip region; clamped to [0,1].</param>
    /// <param name="lateralRadius">Off-axis tolerance in metres; clamped to be non-negative.</param>
    /// <param name="stanceBonus">Stance bonus multiplier inside the region (should be &gt; 1).</param>
    /// <param name="resourceBonus">Resource bonus multiplier inside the region (should be &gt; 1).</param>
    public SweetSpotConfig(float tipFraction, float lateralRadius, float stanceBonus, float resourceBonus)
    {
        _tipFraction = Mathf.Clamp01(tipFraction);
        _lateralRadius = Mathf.Max(0f, lateralRadius);
        _stanceBonus = stanceBonus;
        _resourceBonus = resourceBonus;
    }

    /// <summary>Trailing fraction of the reach that counts as the Sweet Spot, clamped to [0,1].</summary>
    public float TipFraction => Mathf.Clamp01(_tipFraction);

    /// <summary>Off-axis tolerance (metres) within which a hit still counts as a tip hit; never negative.</summary>
    public float LateralRadius => Mathf.Max(0f, _lateralRadius);

    /// <summary>Stance-damage bonus multiplier granted inside the Sweet Spot.</summary>
    public float StanceBonus => _stanceBonus;

    /// <summary>Resource-gain bonus multiplier granted inside the Sweet Spot.</summary>
    public float ResourceBonus => _resourceBonus;

    /// <summary>
    /// True when both bonus multipliers are strictly greater than the outside baseline (Requisito 9.2)
    /// and the tip region has a positive extent along the axis. An invalid config would let an inside
    /// hit grant a bonus no larger than an outside hit, violating the strict-greater contract.
    /// </summary>
    public bool IsValid =>
        TipFraction > 0f &&
        _stanceBonus > OutsideMultiplier + MinBonusEpsilon * 0.5f &&
        _resourceBonus > OutsideMultiplier + MinBonusEpsilon * 0.5f;

    /// <summary>
    /// A sensible default: the tip is the trailing 25% of the reach with a small off-axis tolerance, and
    /// both bonuses are a clear step above the baseline. Useful for tests and inspector defaults.
    /// </summary>
    public static SweetSpotConfig Default => new SweetSpotConfig(0.25f, 0.5f, 1.5f, 1.5f);
}
