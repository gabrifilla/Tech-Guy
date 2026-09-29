using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure control-lock gate in <see cref="ArchetypeActionGate.CanAct"/>
    /// — task 7.2 of enemy-swarm-core-archetypes.
    ///
    /// <see cref="ArchetypeBehavior.CanAct"/> is otherwise scene/component driven; the boolean rule was
    /// extracted into <see cref="ArchetypeActionGate"/> so this cross-cutting invariant can be
    /// property-checked without a live Unity scene. The project cannot resolve FsCheck/CsCheck packages
    /// on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives >= 100
    /// deterministic generated cases per property (generating every agent/lock state combination) and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class ControlLockGatePropertyTests
    {
        // Feature: enemy-swarm-core-archetypes, Property 11: Control-locked archetype issues no move or attack commands.
        // For any archetype whose control is locked, no move or attack command may be issued this frame;
        // likewise when its agent is disabled or off the NavMesh, or its owner is missing/dead, or the
        // behaving component is inactive, the gate is closed and nothing is issued (no error, state
        // unchanged). Over every generated combination of the eight observed states, CanAct returns true
        // if and only if every gate condition passes, and is always false when a live-reaction control
        // lock is present.
        // Validates: Requirements 20.1, 20.2, 20.4, 20.5
        [Test]
        public void ControlLockedOrInvalidAgentGateIsClosed()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate the full space of the eight observed component states.
                bool isActiveAndEnabled = rng.Next(0, 2) == 0;
                bool hasOwner = rng.Next(0, 2) == 0;
                bool ownerDead = rng.Next(0, 2) == 0;
                bool hasAgent = rng.Next(0, 2) == 0;
                bool agentEnabled = rng.Next(0, 2) == 0;
                bool agentOnNavMesh = rng.Next(0, 2) == 0;
                bool hasReaction = rng.Next(0, 2) == 0;
                bool controlLocked = rng.Next(0, 2) == 0;

                bool actual = ArchetypeActionGate.CanAct(
                    isActiveAndEnabled,
                    hasOwner,
                    ownerDead,
                    hasAgent,
                    agentEnabled,
                    agentOnNavMesh,
                    hasReaction,
                    controlLocked);

                // Independent reference of the gate rule (R20.1/R20.2/R20.4/R20.5): every condition
                // must pass, and a present controller reporting a lock closes the gate.
                bool aliveOwner = hasOwner && !ownerDead;
                bool validAgent = hasAgent && agentEnabled && agentOnNavMesh;
                bool locked = hasReaction && controlLocked;
                bool expected = isActiveAndEnabled && aliveOwner && validAgent && !locked;

                PropertyCheck.That(actual == expected,
                    $"gate mismatch for [active={isActiveAndEnabled} owner={hasOwner} dead={ownerDead} " +
                    $"agent={hasAgent} agentEnabled={agentEnabled} onMesh={agentOnNavMesh} " +
                    $"reaction={hasReaction} locked={controlLocked}]: expected {expected}, got {actual}");

                // R20.1/R20.5: a control-locked archetype (present controller + lock) issues no command,
                // regardless of every other state.
                if (hasReaction && controlLocked)
                {
                    PropertyCheck.That(!actual,
                        "control-locked archetype (reaction present + locked) must not act, but the gate opened");
                }

                // R20.2/R20.4: a disabled or off-NavMesh (or absent) agent closes the gate so movement is
                // skipped, independent of lock/owner state.
                if (!hasAgent || !agentEnabled || !agentOnNavMesh)
                {
                    PropertyCheck.That(!actual,
                        "an invalid NavMesh agent must skip movement (gate closed), but the gate opened");
                }

                // A dead or missing owner never acts.
                if (!hasOwner || ownerDead)
                {
                    PropertyCheck.That(!actual,
                        "a dead or missing owner must not act, but the gate opened");
                }

                // An inactive/disabled behaving component never acts.
                if (!isActiveAndEnabled)
                {
                    PropertyCheck.That(!actual,
                        "an inactive/disabled component must not act, but the gate opened");
                }
            });
        }

        // Feature: enemy-swarm-core-archetypes, Property 11: Control-locked archetype issues no move or attack commands.
        // Focused sub-property: whenever a live reaction controller reports a control lock, the gate is
        // closed for EVERY combination of the remaining states (the key R20 rule that a control-locked
        // archetype issues no commands). Also confirms a missing controller counts as "not locked".
        // Validates: Requirements 20.1, 20.5
        [Test]
        public void LiveControlLockAlwaysSuppressesActionAndMissingReactionIsNotLocked()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                bool isActiveAndEnabled = rng.Next(0, 2) == 0;
                bool hasOwner = rng.Next(0, 2) == 0;
                bool ownerDead = rng.Next(0, 2) == 0;
                bool hasAgent = rng.Next(0, 2) == 0;
                bool agentEnabled = rng.Next(0, 2) == 0;
                bool agentOnNavMesh = rng.Next(0, 2) == 0;
                bool controlLocked = rng.Next(0, 2) == 0;

                // With a live reaction controller reporting a lock, the gate is always closed.
                bool lockedGate = ArchetypeActionGate.CanAct(
                    isActiveAndEnabled, hasOwner, ownerDead,
                    hasAgent, agentEnabled, agentOnNavMesh,
                    hasReaction: true, controlLocked: true);

                PropertyCheck.That(!lockedGate,
                    "a live control lock must always close the gate regardless of other states");

                // A missing controller means "not control-locked": its controlLocked flag is ignored, so
                // the gate matches the equivalent state with no lock present.
                bool noReactionGate = ArchetypeActionGate.CanAct(
                    isActiveAndEnabled, hasOwner, ownerDead,
                    hasAgent, agentEnabled, agentOnNavMesh,
                    hasReaction: false, controlLocked: controlLocked);
                bool notLockedGate = ArchetypeActionGate.CanAct(
                    isActiveAndEnabled, hasOwner, ownerDead,
                    hasAgent, agentEnabled, agentOnNavMesh,
                    hasReaction: true, controlLocked: false);

                PropertyCheck.That(noReactionGate == notLockedGate,
                    "a missing reaction controller must behave identically to a present-but-unlocked one");
            });
        }
    }
}
