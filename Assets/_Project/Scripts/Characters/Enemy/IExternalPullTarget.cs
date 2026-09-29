using UnityEngine;

/// <summary>
/// Contract for an actor that can be displaced by an external, bounded pull (the Hooker's hook,
/// feature enemy-swarm-core-archetypes, Requirement 12). When the Hooker's <c>HookLine</c> attack
/// connects within its hook-line tolerance, it invokes <see cref="BeginExternalPull"/> so the
/// target moves toward the hook source over a bounded duration and then regains control.
/// </summary>
/// <remarks>
/// Defining the entry point as an interface keeps <c>EnemyCombatActions</c> decoupled from the
/// concrete player type and mirrors the codebase's existing <see cref="IDamageAbsorber"/> hook.
/// <c>PlayerActor</c> implements this interface in task 9.1 (<c>BeginExternalPull</c> suspends
/// <c>CharControlScript</c> agent steering, moves the player agent toward the source over the
/// bounded duration using the NavMeshAgent, then restores control — always restoring via cleanup
/// even if the source dies mid-pull, and ending early if the source is control-locked). Until
/// task 9.1 lands, no type implements this interface, so <c>HookLine</c> resolves every hit as a
/// harmless miss (no displacement) rather than breaking compilation.
/// </remarks>
public interface IExternalPullTarget
{
    /// <summary>
    /// Begins a bounded pull of this target toward <paramref name="source"/>. Implementations must
    /// complete the pull and return movement control within <paramref name="maxDuration"/> seconds
    /// (Requirements 12.3, 12.6), and end early returning control if the source becomes
    /// control-locked mid-pull (Requirement 12.7).
    /// </summary>
    /// <param name="source">The Hooker the target is pulled toward.</param>
    /// <param name="maxDuration">Upper bound on the pull duration in seconds (≤ 1.5s per R12.3/R12.6).</param>
    void BeginExternalPull(Transform source, float maxDuration);
}
