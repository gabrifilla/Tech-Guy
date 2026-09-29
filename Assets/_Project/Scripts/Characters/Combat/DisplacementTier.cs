using UnityEngine;

/// <summary>
/// The three intensities of physical displacement a hit can impose on an enemy (Requisito 4).
/// A hit expresses its displacement exclusively through the existing <see cref="HitReactionRequest"/>
/// fields (<see cref="HitReactionRequest.PushDistance"/>, <see cref="HitReactionRequest.KnockUpHeight"/>,
/// <see cref="HitReactionRequest.KnockbackDistance"/>) — there is no parallel displacement channel.
/// </summary>
public enum DisplacementTierKind
{
    /// <summary>Readability nudge from basic attacks; PushDistance 0–0.3, no launch.</summary>
    Micro,
    /// <summary>Moderate displacement from specific abilities; PushDistance 0.31–2.0, no launch.</summary>
    Push,
    /// <summary>Strong displacement (long knockback or vertical knock-up) tied to break/skills.</summary>
    Launch
}

/// <summary>
/// Pure, shared validator that classifies a <see cref="HitReactionRequest"/> into a
/// <see cref="DisplacementTierKind"/> from its existing displacement fields, and validates the
/// declared ranges (Requisito 4.1–4.4). This is the single shared point every weapon relies on
/// (Requisito 1.4); it is a static class with no scene dependency so it is testable in isolation.
///
/// When a declaration is absent, zero, or out of range, the displacement is rejected and the hit is
/// treated as <see cref="DisplacementTierKind.Micro"/>, with an error indication surfaced through the
/// <see cref="Result"/>. Classification never mutates the request and never touches the other hit
/// channels (life damage, stance damage, break) — only the displacement tier is decided here
/// (Requisito 4.6).
/// </summary>
public static class DisplacementTier
{
    // Micro (Requisito 4.1): PushDistance 0–0.3, KnockUpHeight = 0, KnockbackDistance = 0.
    public const float MicroPushMin = 0f;
    public const float MicroPushMax = 0.3f;

    // Push (Requisito 4.2): PushDistance 0.31–2.0, KnockUpHeight = 0, KnockbackDistance = 0.
    public const float PushDistanceMin = 0.31f;
    public const float PushDistanceMax = 2.0f;

    // Launch — long knockback (Requisito 4.3): KnockbackDistance 2.01–8.0.
    public const float KnockbackMin = 2.01f;
    public const float KnockbackMax = 8.0f;

    // Launch — vertical knock-up (Requisito 4.3): KnockUpHeight 0.5–4.0.
    public const float KnockUpMin = 0.5f;
    public const float KnockUpMax = 4.0f;

    /// <summary>
    /// The outcome of classifying a hit's declared displacement.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>The tier the hit should apply. When <see cref="IsValid"/> is false this is Micro.</summary>
        public DisplacementTierKind Tier { get; }

        /// <summary>True when the declared displacement fell inside a defined tier's ranges.</summary>
        public bool IsValid { get; }

        /// <summary>
        /// Human-readable error indication for an invalid declaration (Requisito 4.6). Empty when valid.
        /// </summary>
        public string Error { get; }

        public Result(DisplacementTierKind tier, bool isValid, string error)
        {
            Tier = tier;
            IsValid = isValid;
            Error = error ?? string.Empty;
        }

        internal static Result Valid(DisplacementTierKind tier) => new Result(tier, true, string.Empty);
        internal static Result Downgraded(string error) => new Result(DisplacementTierKind.Micro, false, error);
    }

    /// <summary>
    /// Classifies and validates the displacement declared by <paramref name="request"/>.
    /// Returns the matching tier when the declaration is valid, or a Micro-downgraded result with an
    /// error indication when the declaration is absent, zero, or out of range (Requisito 4.6).
    /// This function does not alter the request nor the other hit channels.
    /// </summary>
    public static Result Classify(in HitReactionRequest request)
    {
        return Classify(request.PushDistance, request.KnockUpHeight, request.KnockbackDistance, request.BreakEffect);
    }

    /// <summary>
    /// Field-level overload used by weapons/tests that build displacement from raw values before a
    /// request exists. Same validation rules as <see cref="Classify(in HitReactionRequest)"/>.
    /// </summary>
    public static Result Classify(float pushDistance, float knockUpHeight, float knockbackDistance, StanceBreakEffect breakEffect)
    {
        // A hit may declare at most one launch axis. A Launch is declared by the matching break effect
        // together with a positive value on its axis. Detect launch intent first so a stray push value
        // alongside a launch declaration is treated as an invalid (mixed) declaration.
        bool declaresKnockback = breakEffect == StanceBreakEffect.Knockback && knockbackDistance > 0f;
        bool declaresKnockUp = breakEffect == StanceBreakEffect.KnockUp && knockUpHeight > 0f;

        if (declaresKnockback || declaresKnockUp)
        {
            return ClassifyLaunch(pushDistance, knockUpHeight, knockbackDistance, declaresKnockback, declaresKnockUp);
        }

        // No launch declared: the other launch fields must be clean (0) for a valid Micro/Push.
        if (knockUpHeight > 0f || knockbackDistance > 0f)
        {
            return Result.Downgraded(
                $"Invalid displacement declaration: launch fields set without a matching BreakEffect " +
                $"(KnockUpHeight={knockUpHeight}, KnockbackDistance={knockbackDistance}, BreakEffect={breakEffect}). Downgraded to Micro.");
        }

        // Micro: 0 <= PushDistance <= 0.3.
        if (pushDistance >= MicroPushMin && pushDistance <= MicroPushMax)
        {
            return Result.Valid(DisplacementTierKind.Micro);
        }

        // Push: 0.31 <= PushDistance <= 2.0.
        if (pushDistance >= PushDistanceMin && pushDistance <= PushDistanceMax)
        {
            return Result.Valid(DisplacementTierKind.Push);
        }

        // PushDistance out of every defined range (e.g. negative — clamped to 0 by the ctor — or > 2.0).
        return Result.Downgraded(
            $"Invalid displacement declaration: PushDistance={pushDistance} is outside the Micro " +
            $"({MicroPushMin}–{MicroPushMax}) and Push ({PushDistanceMin}–{PushDistanceMax}) ranges. Downgraded to Micro.");
    }

    private static Result ClassifyLaunch(
        float pushDistance,
        float knockUpHeight,
        float knockbackDistance,
        bool declaresKnockback,
        bool declaresKnockUp)
    {
        // A launch declaration must not mix both axes.
        if (declaresKnockback && declaresKnockUp)
        {
            return Result.Downgraded(
                $"Invalid displacement declaration: both KnockbackDistance={knockbackDistance} and " +
                $"KnockUpHeight={knockUpHeight} declared. Downgraded to Micro.");
        }

        if (declaresKnockback)
        {
            if (knockbackDistance >= KnockbackMin && knockbackDistance <= KnockbackMax)
            {
                return Result.Valid(DisplacementTierKind.Launch);
            }

            return Result.Downgraded(
                $"Invalid displacement declaration: KnockbackDistance={knockbackDistance} is outside the " +
                $"Launch range ({KnockbackMin}–{KnockbackMax}). Downgraded to Micro.");
        }

        // declaresKnockUp
        if (knockUpHeight >= KnockUpMin && knockUpHeight <= KnockUpMax)
        {
            return Result.Valid(DisplacementTierKind.Launch);
        }

        return Result.Downgraded(
            $"Invalid displacement declaration: KnockUpHeight={knockUpHeight} is outside the " +
            $"Launch range ({KnockUpMin}–{KnockUpMax}). Downgraded to Micro.");
    }
}
