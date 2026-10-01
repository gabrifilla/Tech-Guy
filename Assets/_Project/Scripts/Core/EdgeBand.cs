/// <summary>
/// Pure, framework-agnostic helper for the Spear's "edge" of the thrust range, reused by the
/// "Ponto cego" (<c>EdgeStrike</c>, R13) boon. It knows nothing about Unity (no <c>MonoBehaviour</c>,
/// no <c>Time</c>, no scene), so it is 100% testable in EditMode and forms the reusable decision core
/// behind the boon's edge-hit detection and extra stance damage.
///
/// Hitting at the edge of the thrust's reach rewards precise spacing: only the outermost 20% of the
/// range counts as the edge, so a hit lands "on the edge" exactly when it reaches far enough.
/// </summary>
public static class EdgeBand
{
    /// <summary>
    /// Returns <c>true</c> when a hit landed in the outermost 20% of the thrust range (R13.1): the
    /// edge is <c><paramref name="hitDistance"/> &gt;= 0.8 * <paramref name="thrustRange"/></c>.
    /// A non-positive <paramref name="thrustRange"/> has no edge, so it always returns <c>false</c>.
    /// Hits closer than the edge return <c>false</c> and earn no bonus (R13.3).
    /// </summary>
    /// <param name="hitDistance">Distance from the player to the target at the moment of the hit, in meters.</param>
    /// <param name="thrustRange">Reach of the thrust, in meters. Values of <c>0</c> or less define no edge.</param>
    /// <returns><c>true</c> only when the hit lands in the outer 20% of the thrust range.</returns>
    public static bool IsEdge(float hitDistance, float thrustRange)
        => thrustRange > 0f && hitDistance >= thrustRange * 0.8f;

    /// <summary>
    /// Extra stance-damage multiplier applied by an edge hit (R13.1): <c>1 + 0.4 * rank</c>.
    /// Negative ranks are clamped to <c>0</c>, so a misconfigured rank never reduces stance damage.
    /// </summary>
    public static float StanceMultiplier(int rank) => 1f + 0.4f * System.Math.Max(0, rank);
}
