using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Shared base for the non-attack role behaviors (Healer, Shield Support, Spawner,
/// Hazard Caster). It resolves the components every role behavior needs
/// (<see cref="Actor"/>, <see cref="EnemyAI"/>, <see cref="NavMeshAgent"/>,
/// <see cref="CombatReactionController"/>), reports any missing dependency by name at
/// <see cref="Awake"/>, and exposes a single <see cref="CanAct"/> gate that concrete
/// behaviors must consult before issuing any movement or attack command.
///
/// The gate enforces three cross-cutting rules structurally rather than per-archetype:
/// the enemy must be alive, its NavMesh agent must be valid (enabled and on the mesh so
/// all locomotion stays NavMesh-only), and it must not be control-locked (stunned,
/// knocked up, knocked back, or otherwise mid crowd-control). The boolean logic itself
/// is extracted into <see cref="ArchetypeActionGate"/> so it can be property-tested
/// without a live Unity scene.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 1.6, 20.1, 20.2, 20.4, 20.5.</remarks>
[RequireComponent(typeof(Actor), typeof(EnemyAI))]
public abstract class ArchetypeBehavior : MonoBehaviour
{
    /// <summary>The owning enemy's <see cref="Actor"/> (health / death / damage).</summary>
    protected Actor Owner { get; private set; }

    /// <summary>The owning enemy's perception / chase / attack coordinator.</summary>
    protected EnemyAI Ai { get; private set; }

    /// <summary>The NavMesh agent used for all locomotion (never a raw transform write).</summary>
    protected NavMeshAgent Agent { get; private set; }

    /// <summary>
    /// The stance / stagger / crowd-control controller, if present. May be null for
    /// enemies that do not use the Lost-Ark stance model; a missing controller is
    /// treated as "not control-locked".
    /// </summary>
    protected CombatReactionController Reaction { get; private set; }

    /// <summary>
    /// Resolves dependencies and logs any that are missing (R1.6). Derived classes that
    /// override <see cref="Awake"/> must call <c>base.Awake()</c> first.
    /// </summary>
    protected virtual void Awake()
    {
        Owner = GetComponent<Actor>();
        Ai = GetComponent<EnemyAI>();
        Agent = GetComponent<NavMeshAgent>();
        Reaction = GetComponent<CombatReactionController>();
        LogMissingDependencies();
    }

    /// <summary>
    /// True when this behavior may issue movement or attack commands this frame: the
    /// component is active and enabled, the enemy is alive, its NavMesh agent is enabled
    /// and on the mesh, and it is not control-locked (R20.1, R20.2, R20.4, R20.5).
    /// A control-locked or dead enemy issues nothing; a missing
    /// <see cref="CombatReactionController"/> counts as "not control-locked".
    /// </summary>
    protected bool CanAct => ArchetypeActionGate.CanAct(
        isActiveAndEnabled,
        Owner,
        Owner && Owner.IsDead,
        Agent,
        Agent && Agent.enabled,
        Agent && Agent.isOnNavMesh,
        Reaction,
        Reaction && Reaction.IsControlLocked);

    /// <summary>
    /// Logs an error naming every required dependency that failed to resolve, so a
    /// misconfigured prefab is diagnosed at spawn instead of silently doing nothing
    /// (R1.6). Concrete behaviors extend this to validate their own extra references.
    /// </summary>
    protected abstract void LogMissingDependencies();
}

/// <summary>
/// Pure, scene-free evaluation of the archetype "can I act this frame?" gate. Extracted
/// from <see cref="ArchetypeBehavior.CanAct"/> so the control-lock / agent-validity /
/// alive invariant can be property-tested across every combination of states without a
/// live Unity scene. The MonoBehaviour supplies the observed component states; this class
/// holds only the boolean rule.
/// </summary>
/// <remarks>Feature: enemy-swarm-core-archetypes. Requirements: 20.1, 20.2, 20.4, 20.5.</remarks>
public static class ArchetypeActionGate
{
    /// <summary>
    /// Evaluates the gate from the observed component states. A behavior may act only when
    /// it is active and enabled, has a living owner, has a valid NavMesh agent (present,
    /// enabled, and on the mesh), and is not control-locked. Any false input closes the gate.
    /// </summary>
    /// <param name="isActiveAndEnabled">Whether the behaving component is active and enabled.</param>
    /// <param name="hasOwner">Whether an <see cref="Actor"/> owner was resolved.</param>
    /// <param name="ownerDead">Whether the owner is dead. Ignored when <paramref name="hasOwner"/> is false (the missing owner already closes the gate).</param>
    /// <param name="hasAgent">Whether a <see cref="NavMeshAgent"/> was resolved.</param>
    /// <param name="agentEnabled">Whether that agent is enabled.</param>
    /// <param name="agentOnNavMesh">Whether that agent is currently on the NavMesh.</param>
    /// <param name="hasReaction">Whether a <see cref="CombatReactionController"/> is present. A missing controller means "not control-locked".</param>
    /// <param name="controlLocked">Whether the present controller reports a control lock. Ignored when <paramref name="hasReaction"/> is false.</param>
    /// <returns>True only when every gate condition passes.</returns>
    public static bool CanAct(
        bool isActiveAndEnabled,
        bool hasOwner,
        bool ownerDead,
        bool hasAgent,
        bool agentEnabled,
        bool agentOnNavMesh,
        bool hasReaction,
        bool controlLocked)
    {
        if (!isActiveAndEnabled) return false;
        if (!hasOwner || ownerDead) return false;
        if (!hasAgent || !agentEnabled || !agentOnNavMesh) return false;
        if (hasReaction && controlLocked) return false;
        return true;
    }
}
