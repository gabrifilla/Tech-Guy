using UnityEngine;

/// <summary>
/// Pure, scene-independent evaluator for the Lança's Sweet Spot (Requisito 9.1/9.2).
///
/// Given a hit point and the geometry of a thrust (its origin, direction and reach), it decides whether
/// the hit landed in the tip region and returns the stance/resource bonus multipliers to apply. Inside
/// the Sweet Spot the multipliers come from the <see cref="SweetSpotConfig"/> and are strictly greater
/// than 1.0; outside, both are exactly 1.0 (the baseline). This guarantees a direct tip hit is always
/// rewarded more than an equivalent hit outside the tip (Requisito 9.2).
///
/// A thrust is modelled as a segment from <c>origin</c> to <c>origin + direction * reach</c>: the spear
/// tip is the far end. A hit counts as a tip hit when its projection onto the thrust axis falls inside
/// the trailing <see cref="SweetSpotConfig.TipFraction"/> of the reach and its perpendicular distance
/// from the axis is within <see cref="SweetSpotConfig.LateralRadius"/>. Hits behind the origin, past the
/// tip, or too far off-axis are outside.
///
/// This is injected into <c>ArsenalCombat</c> as a plain C# collaborator so the coordinating
/// <see cref="MonoBehaviour"/> stays thin (AGENTS.md); it holds no Unity references of its own and is
/// fully testable without a scene. Wiring into <c>ArsenalCombat</c> is a separate task.
/// </summary>
public sealed class SweetSpot
{
    private readonly SweetSpotConfig _config;

    /// <summary>Creates an evaluator bound to a thrust ability's Sweet Spot configuration.</summary>
    /// <param name="config">The tip region and bonus multipliers for this thrust.</param>
    public SweetSpot(SweetSpotConfig config)
    {
        _config = config;
    }

    /// <summary>The configuration this evaluator was built with.</summary>
    public SweetSpotConfig Config => _config;

    /// <summary>
    /// The outcome of evaluating a hit against a thrust: whether it landed in the tip region and the
    /// bonus multipliers to apply. Multipliers are strictly greater than 1.0 inside a valid Sweet Spot
    /// and exactly 1.0 outside.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>True when the hit landed in the tip Sweet Spot.</summary>
        public bool IsSweetSpot { get; }

        /// <summary>Multiplier to apply to stance damage for this hit (1.0 outside, &gt; 1.0 inside).</summary>
        public float StanceMultiplier { get; }

        /// <summary>Multiplier to apply to resource gain for this hit (1.0 outside, &gt; 1.0 inside).</summary>
        public float ResourceMultiplier { get; }

        public Result(bool isSweetSpot, float stanceMultiplier, float resourceMultiplier)
        {
            IsSweetSpot = isSweetSpot;
            StanceMultiplier = stanceMultiplier;
            ResourceMultiplier = resourceMultiplier;
        }

        /// <summary>The neutral (outside) result: no tip hit, baseline multipliers of 1.0.</summary>
        public static Result Outside =>
            new Result(false, SweetSpotConfig.OutsideMultiplier, SweetSpotConfig.OutsideMultiplier);
    }

    /// <summary>
    /// Evaluates a hit against the thrust geometry.
    /// </summary>
    /// <param name="hitPoint">World-space point where the thrust connected.</param>
    /// <param name="origin">World-space start of the thrust (the wielder).</param>
    /// <param name="direction">Thrust direction; need not be normalised. A zero direction cannot form a tip region, so the hit is treated as outside.</param>
    /// <param name="reach">Length of the thrust in metres. A non-positive reach has no tip region, so the hit is outside.</param>
    /// <returns>
    /// A tip result with the configured bonus multipliers when the hit lands inside a valid Sweet Spot;
    /// otherwise the neutral outside result (both multipliers 1.0). When the config is invalid (a bonus
    /// not strictly above 1.0), the result is always outside so an inside hit can never be rewarded
    /// with a non-strict bonus (Requisito 9.2).
    /// </returns>
    public Result Evaluate(Vector3 hitPoint, Vector3 origin, Vector3 direction, float reach)
    {
        if (!IsInsideTipRegion(hitPoint, origin, direction, reach) || !_config.IsValid)
        {
            return Result.Outside;
        }

        return new Result(true, _config.StanceBonus, _config.ResourceBonus);
    }

    /// <summary>
    /// Geometric test only: whether <paramref name="hitPoint"/> falls inside the tip region of the
    /// thrust, independent of whether the config's bonuses are valid. Exposed for callers/tests that
    /// want the spatial decision without the bonus contract.
    /// </summary>
    public bool IsInsideTipRegion(Vector3 hitPoint, Vector3 origin, Vector3 direction, float reach)
    {
        if (reach <= 0f) return false;

        float dirLength = direction.magnitude;
        if (dirLength <= Mathf.Epsilon) return false;

        Vector3 axis = direction / dirLength; // unit thrust direction
        Vector3 toHit = hitPoint - origin;

        // Distance of the hit projected onto the thrust axis (how far along the thrust it landed).
        float along = Vector3.Dot(toHit, axis);

        // The tip region is the trailing fraction of the reach, measured back from the tip.
        float regionStart = reach * (1f - _config.TipFraction);
        if (along < regionStart || along > reach) return false;

        // Perpendicular distance from the thrust axis must be within the lateral tolerance.
        Vector3 alongVector = axis * along;
        float perpendicular = (toHit - alongVector).magnitude;
        return perpendicular <= _config.LateralRadius;
    }
}
