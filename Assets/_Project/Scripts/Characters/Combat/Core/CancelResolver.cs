/// <summary>
/// Resolves whether the destination of a cancel is available <em>right now</em> — cooldown,
/// resource, Asura energy, or any other existing precondition — as evaluated by the coordinator
/// (<c>CharControlScript</c> / <c>AbilityHolder</c>) that owns the scene state. The combat core
/// stays scene-free by delegating this one engine-dependent check back through this delegate
/// (R4.3 step 3), so <see cref="CancelResolver"/> itself never touches the Unity scene.
/// </summary>
/// <param name="target">The destination whose availability is being queried.</param>
/// <returns><c>true</c> when a cancel into <paramref name="target"/> can be serviced now.</returns>
/// <remarks>Feature: combat-foundation-rework, task 4.2. Requirements: R3.3, R4.3, R4.4.</remarks>
public delegate bool DestinationAvailability(CancelTarget target);

/// <summary>
/// The outcome of a single <see cref="CancelResolver.Resolve"/> evaluation (R4.3). Exactly one
/// value is returned: <see cref="Authorized"/> only when every check in the safe ordering passes;
/// otherwise the first failing check names the reason, which lets the coordinator react correctly
/// (buffer on a temporal miss, reject once on unavailability, keep the current action otherwise)
/// without re-running the checks.
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 4.2. Requirements: R3.3, R4.3, R4.4.</remarks>
public enum CancelDecision
{
    /// <summary>Every check passed; the current action may be canceled into the destination (R4.3).</summary>
    Authorized,

    /// <summary>No CancelRule for the destination, or its window is closed at the current progress (R4.1, R4.2).</summary>
    DeniedWindowClosed,

    /// <summary>The window is open but the command is no longer valid (e.g. target lost) (R4.3).</summary>
    DeniedCommandInvalid,

    /// <summary>The destination is unavailable (cooldown/resource/Asura); the current action is preserved (R4.4).</summary>
    DeniedUnavailable,

    /// <summary>The transition itself is not feasible even though everything else allows it (R4.3).</summary>
    DeniedInfeasible
}

/// <summary>
/// The single, shared authorization used by <em>every</em> Categoria_de_Compromisso and by the dash
/// (R3.3, R4.3). It evaluates the per-destination <see cref="CancelRuleSet"/> together with the
/// destination's availability, encapsulating the <strong>safe ordering</strong> required by R4.3 so
/// that no coordinator reimplements it (and no hidden per-category exceptions creep into the
/// controllers, R3.8).
///
/// <para>
/// The checks run <em>before</em> the current action is ended, in this order: (1) a
/// <see cref="CancelRule"/> exists for the destination and is open at the current progress;
/// (2) the command is still valid; (3) the destination is available
/// (<see cref="DestinationAvailability"/>); (4) the transition is feasible. Only when all four pass
/// is <see cref="CancelDecision.Authorized"/> returned. In particular,
/// <see cref="CancelDecision.DeniedUnavailable"/> leaves the current action completely untouched —
/// the caller keeps its action and phase unchanged, never leaving the Jogador without an action
/// (R4.4).
/// </para>
///
/// <para>
/// Being a pure, scene-free (non-<c>MonoBehaviour</c>) type, it is deterministic and
/// property-testable in isolation (R8.7). It defends against <c>null</c> <paramref name="rules"/>
/// (treated as "no rule" ⇒ <see cref="CancelDecision.DeniedWindowClosed"/>) and <c>null</c>
/// <paramref name="availability"/> (treated as "unavailable" ⇒
/// <see cref="CancelDecision.DeniedUnavailable"/>) so a mis-wired coordinator can never crash the
/// combat loop.
/// </para>
/// </summary>
/// <remarks>Feature: combat-foundation-rework, task 4.2. Requirements: R3.3, R4.3, R4.4.</remarks>
public static class CancelResolver
{
    /// <summary>
    /// Evaluates the safe cancel ordering (R4.3) against the active action's rules and the
    /// destination's availability, returning a single <see cref="CancelDecision"/>. The checks are
    /// short-circuited in order, so the returned value is the <em>first</em> one that fails; it is
    /// <see cref="CancelDecision.Authorized"/> only when all four pass.
    /// </summary>
    /// <param name="rules">
    /// The active action's per-destination cancel rules. A <c>null</c> set is treated as having no
    /// rule for any destination, yielding <see cref="CancelDecision.DeniedWindowClosed"/> (R4.2).
    /// </param>
    /// <param name="target">The destination the player is trying to cancel into.</param>
    /// <param name="progress">
    /// The normalized position used to test the destination's <see cref="CancelRule"/> window.
    /// </param>
    /// <param name="commandStillValid">
    /// <c>true</c> when the originating command remains valid (e.g. its target is still eligible);
    /// <c>false</c> yields <see cref="CancelDecision.DeniedCommandInvalid"/> (R4.3 step 2).
    /// </param>
    /// <param name="availability">
    /// Coordinator-supplied check for cooldown/resource/Asura availability of
    /// <paramref name="target"/> (R4.3 step 3). A <c>null</c> delegate is treated as unavailable,
    /// yielding <see cref="CancelDecision.DeniedUnavailable"/> and preserving the current action
    /// (R4.4).
    /// </param>
    /// <param name="transitionFeasible">
    /// <c>true</c> when actually transitioning into the destination is feasible; <c>false</c>
    /// yields <see cref="CancelDecision.DeniedInfeasible"/> (R4.3 step 4).
    /// </param>
    /// <returns>
    /// <see cref="CancelDecision.Authorized"/> when every check passes; otherwise the first failing
    /// check's decision.
    /// </returns>
    public static CancelDecision Resolve(
        CancelRuleSet rules,
        CancelTarget target,
        float progress,
        bool commandStillValid,
        DestinationAvailability availability,
        bool transitionFeasible)
    {
        // (1) A CancelRule must exist for the destination AND be open at the current progress.
        // No set, no rule, or a closed window all mean "cancel into this destination is forbidden
        // here" (R4.1, R4.2). A null set is defended as "no rule".
        if (rules == null || !rules.TryGet(target, out CancelRule rule) || !rule.IsOpenAt(progress))
        {
            return CancelDecision.DeniedWindowClosed;
        }

        // (2) The originating command must still be valid (e.g. its target not lost) (R4.3 step 2).
        if (!commandStillValid)
        {
            return CancelDecision.DeniedCommandInvalid;
        }

        // (3) The destination must be available now (cooldown/resource/Asura) (R4.3 step 3). A null
        // delegate is defended as "unavailable". DeniedUnavailable preserves the current action and
        // its phase — the caller changes nothing, so the Jogador is never left without an action
        // (R4.4).
        if (availability == null || !availability(target))
        {
            return CancelDecision.DeniedUnavailable;
        }

        // (4) Finally, the transition itself must be feasible (R4.3 step 4).
        if (!transitionFeasible)
        {
            return CancelDecision.DeniedInfeasible;
        }

        // All four checks passed: the cancel is authorized (R4.3).
        return CancelDecision.Authorized;
    }
}
