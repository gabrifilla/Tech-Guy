using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 41 of weapon-gameplay-swarm-rework
    /// (Ativação deduz mana e inicia recarga).
    ///
    /// Property 41 — para toda habilidade ativada com mana suficiente e fora de recarga, a mana é
    /// reduzida pelo custo da habilidade e sua recarga é iniciada, tornando-a indisponível até a
    /// recarga expirar (ver Requisito 13.2 e design.md). Modelado pela lógica pura
    /// <see cref="AbilityActivationDecision.Evaluate"/>: para um slot <c>Ready</c>, sem nenhum cast
    /// em progresso e com <c>currentMana &gt;= cost</c>, a decisão:
    ///   • aceita (<see cref="AbilityActivationDecision.Outcome.Accepted"/> == true, sem falha);
    ///   • deduz exatamente o custo saneado — <c>ResultingMana == currentMana - Sanitize(cost)</c>;
    ///   • deixa o estado <c>Ready</c> para iniciar a recarga — <c>Active</c> quando há janela ativa
    ///     (<c>hasFiniteActiveTime</c>), caso contrário <c>Cooldown</c> — nunca permanecendo
    ///     <c>Ready</c>, ou seja, torna-se indisponível para reativação imediata.
    ///
    /// Approach: <see cref="AbilityActivationDecision"/> é lógica pura (sem MonoBehaviour/cena),
    /// extraída para reproduzir o contrato de ativação de <c>AbilityHolder</c> sem levantar um
    /// PlayerActor/cena — mesmo padrão de <c>AsuraMomentum</c>. As entradas são geradas dentro do
    /// espaço válido da propriedade (slot Ready, sem cast, mana suficiente), com os limites
    /// (custo 0, custo == mana) forçados, e o custo é saneado com a mesma regra da fonte (negativos
    /// e NaN/Infinity colapsam para 0) apenas para prever o resultado esperado.
    /// </summary>
    public sealed class AbilityActivationDeductsManaAndStartsRechargePropertyTests
    {
        // Mirrors AbilityActivationDecision's private Sanitize (Ability.ManaCost clamps negatives to
        // 0; NaN/Infinity collapse to 0) so the test predicts the accepted deduction independently.
        private static float SanitizeCost(float manaCost)
        {
            if (float.IsNaN(manaCost) || float.IsInfinity(manaCost)) return 0f;
            return manaCost < 0f ? 0f : manaCost;
        }

        // Feature: weapon-gameplay-swarm-rework, Property 41: Ativação deduz mana e inicia recarga
        // For a Ready slot with no cast in progress and currentMana >= sanitized cost, Evaluate
        // accepts, deducts exactly the cost (ResultingMana == currentMana - cost), and leaves Ready
        // to start the recharge (Active when it has an active window, else Cooldown).
        // Validates: Requirements 13.2
        [Test]
        public void ActivationDeductsManaAndLeavesReadyToStartRecharge()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A non-negative, finite cost in a broad range, with the two boundaries forced:
                //   i == 0 -> cost 0 (free ability), i == 1 -> cost equal to the whole reserve.
                float cost = i switch
                {
                    0 => 0f,
                    _ => (float)rng.NextDouble() * 100f, // 0 .. 100
                };
                float sanitized = SanitizeCost(cost);

                // currentMana >= cost is the property's precondition. Case i == 1 forces the exact
                // boundary (mana == cost); other cases add a non-negative surplus on top of the cost.
                float currentMana = i == 1
                    ? sanitized
                    : sanitized + (float)rng.NextDouble() * 100f;

                // The recharge branch is orthogonal to the deduction; sample both so the resulting
                // state assertion covers Active (has window) and Cooldown (no window).
                bool hasFiniteActiveTime = rng.Next(0, 2) == 0;

                var outcome = AbilityActivationDecision.Evaluate(
                    slotState: AbilityActivationDecision.AbilitySlotState.Ready,
                    anyCastInProgress: false,
                    currentMana: currentMana,
                    manaCost: cost,
                    hasFiniteActiveTime: hasFiniteActiveTime);

                PropertyCheck.That(outcome.Accepted,
                    $"cost={cost}, mana={currentMana}: a Ready slot with enough mana and no cast in progress must be accepted");
                PropertyCheck.That(!outcome.Failure.HasValue,
                    $"cost={cost}, mana={currentMana}: an accepted activation must carry no failure, got {outcome.Failure}");

                float expectedRemaining = currentMana - sanitized;
                PropertyCheck.That(outcome.ResultingMana == expectedRemaining,
                    $"cost={cost}, mana={currentMana}: accepted activation must deduct the sanitized cost, expected ResultingMana={expectedRemaining}, got {outcome.ResultingMana}");

                var expectedState = hasFiniteActiveTime
                    ? AbilityActivationDecision.AbilitySlotState.Active
                    : AbilityActivationDecision.AbilitySlotState.Cooldown;
                PropertyCheck.That(outcome.ResultingState == expectedState,
                    $"cost={cost}, mana={currentMana}, hasFiniteActiveTime={hasFiniteActiveTime}: slot must leave Ready to start recharge (expected {expectedState}), got {outcome.ResultingState}");
                PropertyCheck.That(outcome.ResultingState != AbilityActivationDecision.AbilitySlotState.Ready,
                    $"cost={cost}, mana={currentMana}: accepted activation must not remain Ready — the recharge must start, making it unavailable");
            });
        }
    }
}
