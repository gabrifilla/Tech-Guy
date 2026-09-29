using UnityEngine;

/// <summary>
/// Pure, scene-independent readability clamp for the immediate reaction channel of a hit
/// (Requisito 2.2 / Property 2).
///
/// The immediate reaction (<see cref="HitReactionType.Push"/> / <see cref="HitReactionType.Stagger"/>)
/// is only a small "keep the enemy near the player" nudge; it must never turn into a visible shove or
/// spin. This class is the single point that clamps the resulting displacement to at most 0.5 metre
/// and the resulting rotation to at most 15 degrees per hit, independently of whatever
/// <see cref="HitReactionRequest.PushDistance"/> the caller declares.
///
/// It is a plain C# class (no <see cref="MonoBehaviour"/>) so the clamp can be exercised without a
/// scene, per the project rules. The owning <c>CombatReactionController</c> calls
/// <see cref="ClampPush"/> before nudging, and treats a <see cref="HitReactionType.None"/> reaction
/// as producing no displacement/rotation at all (Requisito 2.3).
/// </summary>
public static class ImmediateReactionClamp
{
    /// <summary>Maximum displacement, in metres, an immediate reaction may impose per hit (Requisito 2.2).</summary>
    public const float MaxDisplacementMeters = 0.5f;

    /// <summary>Maximum rotation, in degrees, an immediate reaction may impose per hit (Requisito 2.2).</summary>
    public const float MaxRotationDegrees = 15f;

    /// <summary>
    /// Returns true when <paramref name="reactionType"/> is an immediate reaction that may nudge the
    /// enemy. <see cref="HitReactionType.None"/> is not clampable: it must produce no displacement,
    /// rotation nor interruption (Requisito 2.3), so callers should skip the reaction channel entirely.
    /// </summary>
    public static bool ProducesReaction(HitReactionType reactionType)
    {
        return reactionType == HitReactionType.Push || reactionType == HitReactionType.Stagger;
    }

    /// <summary>
    /// Clamps a requested immediate-reaction displacement to the readability limit. A
    /// <see cref="HitReactionType.None"/> reaction yields 0 (no displacement). Any other reaction is
    /// clamped to <see cref="MaxDisplacementMeters"/>; negative inputs (already impossible via the
    /// request ctor, which does Max(0, …)) are treated as 0.
    /// </summary>
    /// <param name="reactionType">The immediate reaction requested by the hit.</param>
    /// <param name="requestedDistance">The distance the caller would like to nudge, in metres.</param>
    /// <returns>The displacement to actually apply, in [0, <see cref="MaxDisplacementMeters"/>].</returns>
    public static float ClampPush(HitReactionType reactionType, float requestedDistance)
    {
        if (!ProducesReaction(reactionType)) return 0f;
        if (requestedDistance <= 0f) return 0f;
        return Mathf.Min(requestedDistance, MaxDisplacementMeters);
    }

    /// <summary>
    /// Clamps a requested immediate-reaction rotation to the readability limit. A
    /// <see cref="HitReactionType.None"/> reaction yields 0 (no rotation). The magnitude is limited to
    /// <see cref="MaxRotationDegrees"/> while preserving the sign of the requested rotation.
    /// </summary>
    /// <param name="reactionType">The immediate reaction requested by the hit.</param>
    /// <param name="requestedDegrees">The rotation the caller would like to apply, in degrees (signed).</param>
    /// <returns>The rotation to actually apply, with magnitude in [0, <see cref="MaxRotationDegrees"/>].</returns>
    public static float ClampRotation(HitReactionType reactionType, float requestedDegrees)
    {
        if (!ProducesReaction(reactionType)) return 0f;
        return Mathf.Clamp(requestedDegrees, -MaxRotationDegrees, MaxRotationDegrees);
    }
}
