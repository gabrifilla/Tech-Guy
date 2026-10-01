using UnityEngine;

/// <summary>
/// Pure decision helper behind the "Disparo em recuo" (Kiting Step) bow boon (R8). It owns the
/// framework-agnostic math — "is the player retreating?" and "how far should the impulse push?" —
/// so the full rule can be exercised in EditMode without a scene, a <c>NavMeshAgent</c>, or any
/// enemies present. The <c>KitingStepCoordinator</c> <c>MonoBehaviour</c> is the only part that
/// touches scene state; this class references <see cref="Vector3"/> solely for its dot-product and
/// magnitude helpers and intentionally derives nothing from <c>MonoBehaviour</c>.
/// </summary>
public static class KitingImpulse
{
    // Starting point for the reposition distance before the per-rank scale is applied (R8.2).
    private const float BaseDistance = 1.5f;

    // Squared-magnitude floor that distinguishes a real move direction from near-zero jitter, so a
    // standing-still player never triggers an impulse (R8.1/R8.4).
    private const float MinMoveSqrMagnitude = 1e-4f;

    /// <summary>
    /// Minimum time (seconds) between two impulses. The coordinator enforces this window via its
    /// own <c>_nextImpulseAt</c> timestamp so a basic-attack burst cannot chain impulses per frame
    /// (R8.2).
    /// </summary>
    public const float Cooldown = 0.4f;

    /// <summary>
    /// True only when the player is moving (<paramref name="moveDir"/> has non-trivial magnitude)
    /// AND moving away from the nearest enemy — i.e. the dot product of the move direction and the
    /// direction toward the enemy is negative (R8.1/R8.4). Standing still, or moving toward /
    /// perpendicular to the enemy, grants no impulse.
    /// </summary>
    /// <param name="moveDir">The player's current locomotion direction.</param>
    /// <param name="toNearestEnemy">Vector from the player to the nearest enemy.</param>
    public static bool ShouldReposition(Vector3 moveDir, Vector3 toNearestEnemy)
    {
        if (moveDir.sqrMagnitude <= MinMoveSqrMagnitude)
            return false;
        if (toNearestEnemy.sqrMagnitude <= MinMoveSqrMagnitude)
            return false;
        return Vector3.Dot(moveDir.normalized, toNearestEnemy.normalized) < 0f;
    }

    /// <summary>
    /// Reposition distance for the given rank, scaled by <c>1 + 0.25 * rank</c> (R8.2). Negative
    /// ranks are clamped to <c>0</c>, so the result is monotonically non-decreasing in rank.
    /// </summary>
    public static float Distance(int rank)
    {
        int safeRank = rank < 0 ? 0 : rank;
        return BaseDistance * (1f + 0.25f * safeRank);
    }
}
