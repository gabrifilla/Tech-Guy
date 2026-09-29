/// <summary>
/// Pure, scene-free decision for the shared Q/W/E/R activation contract that
/// <see cref="AbilityHolder"/> enforces (Requisitos 13.2, 13.3, 13.4). It exists so the activation
/// contract can be exercised as pure logic — mirroring the extraction pattern already used for
/// <see cref="AsuraMomentum"/> — without standing up a PlayerActor/scene.
///
/// The order of the gates below is identical to <see cref="AbilityHolder.TryUseAbility"/> +
/// <c>CheckUse</c>: a slot that is not <see cref="AbilitySlotState.Ready"/> is rejected as being on
/// cooldown/active (13.3/13.4), an in-progress cast on any slot blocks every other slot (13.3),
/// and insufficient mana is rejected without touching the meter (13.4). Only when all gates pass is
/// the activation accepted, at which point the cost is deducted and the slot leaves Ready (13.2).
///
/// This is a *decision*, not the executor: <see cref="AbilityHolder"/> keeps owning the real timers,
/// the actual <c>Ability.TryActivate</c> call and the ArsenalCombat/BreakerGauntletCombat casting
/// flags. This helper simply reproduces the yes/no + resulting-state contract those checks encode.
/// </summary>
public static class AbilityActivationDecision
{
    /// <summary>Ready/Active/Cooldown mirror of <c>AbilityHolder</c>'s private state machine.</summary>
    public enum AbilitySlotState
    {
        Ready,
        Active,
        Cooldown
    }

    /// <summary>Immutable outcome of evaluating an activation request.</summary>
    public readonly struct Outcome
    {
        public readonly bool Accepted;
        public readonly AbilityUseFailure? Failure;
        /// <summary>Mana remaining after the decision (unchanged unless the activation is accepted).</summary>
        public readonly float ResultingMana;
        /// <summary>The slot's state after the decision (unchanged unless the activation is accepted).</summary>
        public readonly AbilitySlotState ResultingState;

        internal Outcome(bool accepted, AbilityUseFailure? failure, float resultingMana, AbilitySlotState resultingState)
        {
            Accepted = accepted;
            Failure = failure;
            ResultingMana = resultingMana;
            ResultingState = resultingState;
        }
    }

    /// <summary>
    /// Evaluates a single Q/W/E/R activation request.
    /// </summary>
    /// <param name="slotState">Current state of the slot being activated.</param>
    /// <param name="anyCastInProgress">True while any Manoplas/Arco/Lança cast is executing (mutual exclusion, 13.3).</param>
    /// <param name="currentMana">Current mana reserve.</param>
    /// <param name="manaCost">Mana cost declared by the ability (clamped to &gt;= 0).</param>
    /// <param name="hasFiniteActiveTime">
    /// True when the activated ability has an active window (activeTime &gt; 0 or a combat executor
    /// duration): the slot becomes <see cref="AbilitySlotState.Active"/>. False means it goes
    /// straight to <see cref="AbilitySlotState.Cooldown"/>. Matches AbilityHolder's timer branch.
    /// </param>
    public static Outcome Evaluate(
        AbilitySlotState slotState,
        bool anyCastInProgress,
        float currentMana,
        float manaCost,
        bool hasFiniteActiveTime)
    {
        // 13.3/13.4 — a slot that is not Ready (already active or cooling down) is rejected as Cooldown,
        // preserving mana and the slot's state unchanged. Mirrors `states[index] != Ready`.
        if (slotState != AbilitySlotState.Ready)
            return Reject(AbilityUseFailure.Cooldown, currentMana, slotState);

        // 13.3 — mutual exclusion: while any ability is executing, activating another is Busy.
        if (anyCastInProgress)
            return Reject(AbilityUseFailure.Busy, currentMana, slotState);

        float cost = Sanitize(manaCost);

        // 13.4 — insufficient mana rejects the activation and preserves the reserve untouched.
        if (!(currentMana >= cost))
            return Reject(AbilityUseFailure.NotEnoughMana, currentMana, slotState);

        // 13.2 — accepted: deduct the cost and leave Ready (Active if it has a window, else Cooldown).
        float remaining = currentMana - cost;
        var next = hasFiniteActiveTime ? AbilitySlotState.Active : AbilitySlotState.Cooldown;
        return new Outcome(true, null, remaining, next);
    }

    private static Outcome Reject(AbilityUseFailure failure, float mana, AbilitySlotState state) =>
        new Outcome(false, failure, mana, state);

    // Matches Ability.ManaCost, which clamps negatives to 0; NaN/Infinity collapse to 0 so a broken
    // asset can never make the mana comparison pass spuriously.
    private static float Sanitize(float manaCost)
    {
        if (float.IsNaN(manaCost) || float.IsInfinity(manaCost)) return 0f;
        return manaCost < 0f ? 0f : manaCost;
    }
}
