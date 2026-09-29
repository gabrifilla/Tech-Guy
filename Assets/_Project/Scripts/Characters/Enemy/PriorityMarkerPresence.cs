/// <summary>
/// Pure, scene-free model of the priority-marker presence rule. Extracted from the
/// <see cref="PriorityTargetMarker"/> MonoBehaviour so the invariant "a persistent priority
/// indicator is present if and only if the enemy is a priority target and is alive" can be
/// property-tested without a live Unity scene (task 8.11, Property 13).
///
/// The predicate is deliberately trivial in isolation; keeping it here (rather than inline in the
/// MonoBehaviour) is what lets the test exercise every combination of (priority, alive) and assert
/// the iff, and also gives the role behaviors and <c>EnemyVariant</c> a single source of truth for
/// "should this enemy show a marker at all?".
/// </summary>
/// <remarks>
/// Feature: enemy-swarm-core-archetypes, task 8.10.
/// Requirements: 13.8, 14.6, 16.7, 21.1, 21.2, 21.4.
/// </remarks>
public static class PriorityMarkerPresence
{
    /// <summary>
    /// The persistent priority marker is present exactly when the enemy is a declared priority
    /// target and is currently alive. A dead enemy shows no marker (removed on death, R21.4/R13.8/
    /// R14.6/R16.7); a non-priority enemy never shows one (R21.1/R21.2).
    /// </summary>
    /// <param name="isPriorityTarget">Whether the enemy's archetype declares itself a priority target.</param>
    /// <param name="isAlive">Whether the enemy is currently alive (not dead / not removed).</param>
    /// <returns>True only when the enemy is both a priority target and alive.</returns>
    public static bool ShouldShowMarker(bool isPriorityTarget, bool isAlive)
        => isPriorityTarget && isAlive;

    /// <summary>
    /// The transient role-action cue is present only when the persistent marker would be present
    /// (priority target and alive) AND the enemy is currently performing its role action. On death
    /// both the marker and the cue vanish in the same frame because <see cref="ShouldShowMarker"/>
    /// becomes false, which forces this to false as well (R21.3, R21.4).
    /// </summary>
    /// <param name="isPriorityTarget">Whether the enemy's archetype declares itself a priority target.</param>
    /// <param name="isAlive">Whether the enemy is currently alive (not dead / not removed).</param>
    /// <param name="isPerformingRoleAction">Whether the enemy is mid heal / shield / spawn action.</param>
    /// <returns>True only when the marker is present and a role action is in progress.</returns>
    public static bool ShouldShowRoleActionCue(bool isPriorityTarget, bool isAlive, bool isPerformingRoleAction)
        => ShouldShowMarker(isPriorityTarget, isAlive) && isPerformingRoleAction;
}
