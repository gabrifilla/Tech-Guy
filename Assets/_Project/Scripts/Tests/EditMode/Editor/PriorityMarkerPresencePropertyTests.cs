using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure priority-marker presence rule in
    /// <see cref="PriorityMarkerPresence"/> — task 8.11 of enemy-swarm-core-archetypes.
    ///
    /// <see cref="PriorityTargetMarker"/> is otherwise scene/component driven; the boolean rule
    /// "a persistent priority indicator is present iff the enemy is a priority target and alive"
    /// was extracted into <see cref="PriorityMarkerPresence"/> so this cross-cutting invariant can
    /// be property-checked without a live Unity scene. The project cannot resolve FsCheck/CsCheck
    /// packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// >= 100 deterministic generated cases per property (exercising every (priority, alive) and
    /// (priority, alive, role-action) combination) and reports the exact failing case as a
    /// counterexample.
    /// </summary>
    public sealed class PriorityMarkerPresencePropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 13: Priority marker exists exactly for alive priority archetypes.
        // Over all combinations of (isPriorityTarget, isAlive), ShouldShowMarker is true if and only
        // if BOTH are true: a dead priority target shows no marker (R21.4), and a non-priority enemy
        // never shows one (R21.1/R21.2). The transient role-action cue is false whenever the marker
        // is false, so death collapses both the marker and the cue in the same frame (R21.3/R21.4,
        // and the healer/shield/spawner priority cues R13.8/R14.6/R16.7).
        // Validates: Requirements 13.8, 14.6, 16.7, 21.1, 21.2, 21.4
        [Test]
        public void MarkerExistsIffPriorityTargetAndAliveAndCueCollapsesWithMarker()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate the full space of the two observed marker states plus the role-action flag.
                bool isPriorityTarget = rng.Next(0, 2) == 0;
                bool isAlive = rng.Next(0, 2) == 0;
                bool isPerformingRoleAction = rng.Next(0, 2) == 0;

                bool marker = PriorityMarkerPresence.ShouldShowMarker(isPriorityTarget, isAlive);
                bool cue = PriorityMarkerPresence.ShouldShowRoleActionCue(
                    isPriorityTarget, isAlive, isPerformingRoleAction);

                // R21.1/R21.2/R21.4: the persistent marker is present iff priority target AND alive.
                bool expectedMarker = isPriorityTarget && isAlive;
                PropertyCheck.That(marker == expectedMarker,
                    $"marker mismatch for [priority={isPriorityTarget} alive={isAlive}]: " +
                    $"expected {expectedMarker}, got {marker}");

                // R21.4/R13.8/R14.6/R16.7: a dead priority target shows no marker.
                if (isPriorityTarget && !isAlive)
                {
                    PropertyCheck.That(!marker,
                        "a dead priority target must show no marker, but the marker was present");
                }

                // R21.1/R21.2: a non-priority enemy never shows a marker, alive or not.
                if (!isPriorityTarget)
                {
                    PropertyCheck.That(!marker,
                        "a non-priority enemy must never show a marker, but the marker was present");
                }

                // R21.3: the role-action cue is present only when the marker is present AND a role
                // action is in progress.
                bool expectedCue = expectedMarker && isPerformingRoleAction;
                PropertyCheck.That(cue == expectedCue,
                    $"cue mismatch for [priority={isPriorityTarget} alive={isAlive} " +
                    $"roleAction={isPerformingRoleAction}]: expected {expectedCue}, got {cue}");

                // R21.4: whenever the marker is absent the cue must also be absent (death collapses
                // both in the same frame), regardless of whether a role action is nominally running.
                if (!marker)
                {
                    PropertyCheck.That(!cue,
                        "the role-action cue must be absent whenever the marker is absent, but the cue was present");
                }

                // The cue can never be present without the marker (the cue is a strict refinement of
                // the marker), so a live role action alone never shows a cue on a dead/non-priority enemy.
                PropertyCheck.That(!cue || marker,
                    "the role-action cue was present while the marker was absent (cue must imply marker)");
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 13: Priority marker exists exactly for alive priority archetypes.
        // Focused facet: death is the single collapse point. For any priority target performing a role
        // action while alive (marker + cue present), transitioning to dead in the same frame removes
        // BOTH the marker and the cue (R21.4), and no non-priority archetype ever gains a marker by
        // dying or performing a role action.
        // Validates: Requirements 21.1, 21.2, 21.4
        [Test]
        public void DeathRemovesMarkerAndCueForPriorityTargetsInTheSameFrame()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                bool isPriorityTarget = rng.Next(0, 2) == 0;
                bool isPerformingRoleAction = rng.Next(0, 2) == 0;

                // Alive state.
                bool aliveMarker = PriorityMarkerPresence.ShouldShowMarker(isPriorityTarget, true);
                bool aliveCue = PriorityMarkerPresence.ShouldShowRoleActionCue(
                    isPriorityTarget, true, isPerformingRoleAction);

                // Same states after death (single-frame transition).
                bool deadMarker = PriorityMarkerPresence.ShouldShowMarker(isPriorityTarget, false);
                bool deadCue = PriorityMarkerPresence.ShouldShowRoleActionCue(
                    isPriorityTarget, false, isPerformingRoleAction);

                // R21.4: death removes both marker and cue for everyone, in the same frame.
                PropertyCheck.That(!deadMarker,
                    "death must remove the marker, but a dead enemy still showed one");
                PropertyCheck.That(!deadCue,
                    "death must remove the role-action cue, but a dead enemy still showed one");

                if (isPriorityTarget)
                {
                    // A live priority target does show the marker (and the cue iff performing a role
                    // action) — confirming the transition is an actual removal, not a no-op.
                    PropertyCheck.That(aliveMarker,
                        "a live priority target must show the marker");
                    PropertyCheck.That(aliveCue == isPerformingRoleAction,
                        $"a live priority target's cue must equal its role-action state " +
                        $"(roleAction={isPerformingRoleAction}), got {aliveCue}");
                }
                else
                {
                    // R21.1/R21.2: a non-priority archetype never shows a marker or cue, alive or dead.
                    PropertyCheck.That(!aliveMarker && !aliveCue,
                        "a non-priority archetype must never show a marker or cue, even while alive");
                }
            });
        }
    }
}
