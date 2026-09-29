using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 42 of weapon-gameplay-swarm-rework
    /// (Exclusão mútua durante a execução).
    ///
    /// Property 42 — para toda habilidade em execução, qualquer tentativa de ativar outra habilidade
    /// Q/W/E/R é bloqueada até a conclusão da habilidade em execução. Ver Requisito 13.3.
    ///
    /// Modelado por <see cref="AbilityActivationDecision.Evaluate"/>, a lógica pura (sem
    /// MonoBehaviour/cena) que espelha os gates de <c>AbilityHolder.TryUseAbility</c>. Enquanto
    /// <c>anyCastInProgress</c> é verdadeiro (uma habilidade executa):
    ///   • um slot <see cref="AbilityActivationDecision.AbilitySlotState.Ready"/> é rejeitado com
    ///     <see cref="AbilityUseFailure.Busy"/>, preservando a mana e o estado do slot inalterados;
    ///   • um slot que NÃO está Ready é rejeitado antes, com <see cref="AbilityUseFailure.Cooldown"/>
    ///     (o gate de cooldown/active precede o de exclusão mútua), também sem tocar mana/estado.
    ///
    /// Como controle que delimita a faixa bloqueada: com a mesma entrada, mas sem execução em curso
    /// (<c>anyCastInProgress == false</c>) e mana suficiente, um slot Ready é aceito — o
    /// comportamento oposto que confirma que é a execução em curso que causa o bloqueio.
    ///
    /// Approach: a entrada é gerada a partir do Random semeado; nenhuma fonte é editada. O helper é
    /// puro, então o contrato yes/no + estado-resultante é exercitado diretamente.
    /// </summary>
    public sealed class MutualExclusionDuringExecutionPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 42: Exclusão mútua durante a execução
        // While a cast is executing (anyCastInProgress == true), Evaluate blocks any other Q/W/E/R:
        // a Ready slot is rejected as Busy and a non-Ready slot as Cooldown, both leaving mana and
        // slot state unchanged. Control: without an in-progress cast, a Ready slot with enough mana
        // is accepted.
        // Validates: Requirements 13.3
        [Test]
        public void InProgressCastBlocksEveryOtherSlotAndPreservesManaAndState()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A cast is executing: mutual exclusion (13.3) must reject any activation attempt.
                const bool anyCastInProgress = true;

                float currentMana = (float)(rng.NextDouble() * 100.0);   // 0..100
                float manaCost = (float)(rng.NextDouble() * 20.0);       // 0..20 (<= reserve possible)
                bool hasFiniteActiveTime = rng.Next(0, 2) == 0;

                // ---- Ready slot: blocked as Busy, mana and state untouched --------------------------
                var ready = AbilityActivationDecision.Evaluate(
                    AbilityActivationDecision.AbilitySlotState.Ready,
                    anyCastInProgress,
                    currentMana,
                    manaCost,
                    hasFiniteActiveTime);

                PropertyCheck.That(!ready.Accepted,
                    $"case#{i}: a Ready slot must be rejected while a cast is in progress, was accepted");
                PropertyCheck.That(ready.Failure == AbilityUseFailure.Busy,
                    $"case#{i}: an in-progress cast must reject a Ready slot as Busy, got {ready.Failure}");
                PropertyCheck.That(ready.ResultingMana == currentMana,
                    $"case#{i}: a blocked activation must preserve mana ({currentMana}), got {ready.ResultingMana}");
                PropertyCheck.That(ready.ResultingState == AbilityActivationDecision.AbilitySlotState.Ready,
                    $"case#{i}: a blocked Ready slot must remain Ready, got {ready.ResultingState}");

                // ---- Non-Ready slot: the cooldown/active gate precedes mutual exclusion --------------
                // Force the two non-Ready states (Active, Cooldown), covering both explicitly.
                var nonReadyState = (i % 2 == 0)
                    ? AbilityActivationDecision.AbilitySlotState.Active
                    : AbilityActivationDecision.AbilitySlotState.Cooldown;

                var nonReady = AbilityActivationDecision.Evaluate(
                    nonReadyState,
                    anyCastInProgress,
                    currentMana,
                    manaCost,
                    hasFiniteActiveTime);

                PropertyCheck.That(!nonReady.Accepted,
                    $"case#{i}: a {nonReadyState} slot must be rejected, was accepted");
                PropertyCheck.That(nonReady.Failure == AbilityUseFailure.Cooldown,
                    $"case#{i}: a {nonReadyState} slot must be rejected as Cooldown, got {nonReady.Failure}");
                PropertyCheck.That(nonReady.ResultingMana == currentMana,
                    $"case#{i}: a rejected {nonReadyState} slot must preserve mana ({currentMana}), got {nonReady.ResultingMana}");
                PropertyCheck.That(nonReady.ResultingState == nonReadyState,
                    $"case#{i}: a rejected {nonReadyState} slot must keep its state, got {nonReady.ResultingState}");

                // ---- Control: without an in-progress cast, the same Ready slot is accepted -----------
                // Guarantee sufficient mana so only the mutual-exclusion gate distinguishes the cases.
                float affordableCost = (currentMana <= 0f) ? 0f : (float)(rng.NextDouble() * currentMana);
                var control = AbilityActivationDecision.Evaluate(
                    AbilityActivationDecision.AbilitySlotState.Ready,
                    false,
                    currentMana,
                    affordableCost,
                    hasFiniteActiveTime);

                PropertyCheck.That(control.Accepted,
                    $"case#{i}: with no cast in progress and enough mana, a Ready slot must be accepted");
                PropertyCheck.That(control.Failure == null,
                    $"case#{i}: an accepted activation must carry no failure, got {control.Failure}");
            });
        }
    }
}
