/// <summary>
/// Pure, framework-agnostic helper for the Spear's ideal spacing band, reused by the
/// "Recuo controlado" (<c>SpacingRecoil</c>, R11) boon. It knows nothing about Unity
/// (no <c>MonoBehaviour</c>, no <c>Time</c>, no <c>NavMeshAgent</c>, no scene), so it is 100%
/// testable in EditMode and forms the reusable decision core behind the boon's recoil.
///
/// The band <c>[<see cref="Min"/>, <see cref="Max"/>]</c> mirrors the one used by
/// <c>PerfectSpacing</c>, so both boons agree on what "ideal distance" means.
/// </summary>
public static class SpacingBand
{
    /// <summary>Lower edge of the ideal spacing band, in meters (same as <c>PerfectSpacing</c>).</summary>
    public const float Min = 3.5f;

    /// <summary>Upper edge of the ideal spacing band, in meters (same as <c>PerfectSpacing</c>).</summary>
    public const float Max = 6.5f;

    /// <summary>
    /// Computes the backward step (in meters) the player should take after a Thrust connects (R11.1).
    /// The intended displacement grows with <paramref name="rank"/> by a factor of
    /// <c>0.2 * rank</c> applied to the current distance, but the resulting player distance is
    /// clamped so it never passes <see cref="Max"/> — the player never steps out of the band (R11.2).
    /// Negative ranks are clamped to <c>0</c> and the returned step is never negative.
    /// </summary>
    /// <param name="currentDistance">Current distance between the player and the target, in meters.</param>
    /// <param name="rank">Boon rank. Values below <c>0</c> are treated as <c>0</c> (no step).</param>
    /// <returns>The non-negative backward step to apply, bounded so distance never exceeds <see cref="Max"/>.</returns>
    public static float StepBack(float currentDistance, int rank)
    {
        float desired = currentDistance * (0.2f * System.Math.Max(0, rank)); // intended displacement
        float target = System.Math.Min(Max, currentDistance + desired);      // clamped to the band's upper edge
        return System.Math.Max(0f, target - currentDistance);                // never passes Max, never negative
    }
}
