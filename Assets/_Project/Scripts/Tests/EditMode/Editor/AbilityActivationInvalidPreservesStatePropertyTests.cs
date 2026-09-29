using NUnit.Framework;
using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    using AbilitySlotState = AbilityActivationDecision.AbilitySlotState;

    /// <summary>
    /// EditMode property test for Property 43 of weapon-gameplay-swarm-rework
    /// (Ativação inválida preserva o estado).
    ///
    /// Property 43 — para toda ativação Q/W/E/R rejeitada por qualquer uma das causas do contrato
    /// compartilhado (slot fora de <see cref="AbilitySlotState.Ready"/> — recarga/ativo,
    /// execução em andamento em qualquer slot, ou mana insuficiente após saneamento do custo),
    /// <see cref="AbilityActivationDecision.Evaluate"/> recusa a ativação
    /// (<c>Accepted == false</c>, <c>Failure</c> preenchido), preserva a mana atual sem dedução
    /// (<c>ResultingMana == currentMana</c>) e mantém o estado do slot inalterado
    /// (<c>ResultingState == slotState</c> de entrada). Ver Requisito 13.4 e design (Property 43):
    /// "a mana atual é preservada sem dedução e o estado permanece inalterado".
    ///
    /// A causa de mana insuficiente inclui explicitamente custos degenerados (NaN/Infinity/negativo),
    /// que <see cref="AbilityActivationDecision"/> sanea para 0: quando a mana atual é ela própria
    /// negativa/NaN a comparação <c>currentMana &gt;= 0</c> falha e a ativação é recusada como
    /// NotEnoughMana, sem tocar na reserva.
    ///
    /// Approach: <see cref="AbilityActivationDecision"/> é uma decisão pura (sem MonoBehaviour/cena),
    /// extraída justamente para exercitar o contrato de ativação como lógica pura. Não são editados
    /// arquivos de origem. O harness semeado <see cref="PropertyCheck"/> gera &gt;= 100 casos
    /// determinísticos e reporta o caso falho exato como contraexemplo.
    /// </summary>
    public sealed class AbilityActivationInvalidPreservesStatePropertyTests
    {
        // Slot states that are NOT Ready — every one of these must be rejected as Cooldown.
        private static readonly AbilitySlotState[] NonReadyStates =
        {
            AbilitySlotState.Active,
            AbilitySlotState.Cooldown,
        };

        // The three rejection causes the shared contract encodes, in the same gate order as Evaluate.
        private enum RejectionCause
        {
            NonReadySlot,     // slot != Ready            -> Cooldown  (13.3/13.4)
            CastInProgress,   // anyCastInProgress == true -> Busy      (13.3)
            InsufficientMana, // currentMana < cost        -> NotEnoughMana (13.4)
        }

        // A finite mana value in a wide, realistic-plus range (including 0 and large reserves).
        private static float RandomMana(System.Random rng) => (float)(rng.NextDouble() * 2000.0 - 500.0);

        // A finite, non-negative-ish cost the ability might declare (Evaluate clamps negatives to 0).
        private static float RandomCost(System.Random rng) => (float)(rng.NextDouble() * 500.0 - 50.0);

        // Feature: weapon-gameplay-swarm-rework, Property 43: Ativação inválida preserva o estado
        // For every rejection cause (non-Ready slot, cast in progress, insufficient mana including
        // NaN/Infinity/negative sanitized cost), Evaluate returns Accepted==false with Failure set,
        // ResultingMana == currentMana, and ResultingState == the input slotState (all unchanged).
        // Validates: Requirements 13.4
        [Test]
        public void InvalidActivationRejectsAndPreservesManaAndSlotState()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Cover each cause at least once via the first indices, then sample randomly.
                RejectionCause cause = i switch
                {
                    0 => RejectionCause.NonReadySlot,
                    1 => RejectionCause.CastInProgress,
                    2 => RejectionCause.InsufficientMana,
                    _ => (RejectionCause)rng.Next(0, 3),
                };

                AbilitySlotState slotState;
                bool anyCastInProgress;
                float currentMana;
                float manaCost;

                switch (cause)
                {
                    case RejectionCause.NonReadySlot:
                    {
                        // A slot that is Active or Cooldown is rejected regardless of mana/cast state,
                        // so vary those freely to prove the gate order (this gate wins first).
                        slotState = NonReadyStates[rng.Next(NonReadyStates.Length)];
                        anyCastInProgress = rng.Next(0, 2) == 0;
                        currentMana = RandomMana(rng);
                        manaCost = RandomCost(rng);
                        break;
                    }
                    case RejectionCause.CastInProgress:
                    {
                        // Ready slot, but a cast is executing on some slot -> Busy (mutual exclusion).
                        // Mana can be plentiful; the cast gate must still block and preserve state.
                        slotState = AbilitySlotState.Ready;
                        anyCastInProgress = true;
                        currentMana = (float)(rng.NextDouble() * 2000.0); // >= 0, often ample
                        manaCost = RandomCost(rng);
                        break;
                    }
                    default: // RejectionCause.InsufficientMana
                    {
                        // Ready slot, no cast in progress, but the (sanitized) cost exceeds the reserve.
                        slotState = AbilitySlotState.Ready;
                        anyCastInProgress = false;

                        // Pick between an ordinary "cost > mana" case and a degenerate cost that
                        // sanitizes to 0 combined with a mana that itself fails `mana >= 0`.
                        int variant = rng.Next(0, 4);
                        switch (variant)
                        {
                            case 0: // NaN cost sanitizes to 0; force mana < 0 so 0 > mana rejects.
                                manaCost = float.NaN;
                                currentMana = -(float)(rng.NextDouble() * 500.0) - 0.001f;
                                break;
                            case 1: // +Infinity cost sanitizes to 0; NaN mana makes (mana >= 0) false.
                                manaCost = float.PositiveInfinity;
                                currentMana = float.NaN;
                                break;
                            case 2: // Negative cost sanitizes to 0; negative mana => 0 > mana rejects.
                                manaCost = -(float)(rng.NextDouble() * 100.0) - 1f;
                                currentMana = -(float)(rng.NextDouble() * 500.0) - 0.001f;
                                break;
                            default: // Ordinary insufficiency: a strictly positive cost above the reserve.
                                currentMana = (float)(rng.NextDouble() * 500.0);
                                manaCost = currentMana + (float)(rng.NextDouble() * 500.0) + 0.001f;
                                break;
                        }
                        break;
                    }
                }

                bool hasFiniteActiveTime = rng.Next(0, 2) == 0; // irrelevant on rejection; vary anyway.

                AbilityActivationDecision.Outcome outcome = AbilityActivationDecision.Evaluate(
                    slotState, anyCastInProgress, currentMana, manaCost, hasFiniteActiveTime);

                string ctx =
                    $"cause={cause}, slotState={slotState}, anyCast={anyCastInProgress}, " +
                    $"mana={currentMana}, cost={manaCost}, hasActive={hasFiniteActiveTime}";

                // Rejected: not accepted, and a failure reason is always present.
                PropertyCheck.That(!outcome.Accepted,
                    $"{ctx}: invalid activation must be rejected (Accepted=false), got Accepted=true.");
                PropertyCheck.That(outcome.Failure.HasValue,
                    $"{ctx}: a rejected activation must report a Failure reason, got none.");

                // The specific failure matches the gate that fired (documents the contract order).
                AbilityUseFailure expectedFailure = cause switch
                {
                    RejectionCause.NonReadySlot => AbilityUseFailure.Cooldown,
                    RejectionCause.CastInProgress => AbilityUseFailure.Busy,
                    _ => AbilityUseFailure.NotEnoughMana,
                };
                PropertyCheck.That(outcome.Failure.Value == expectedFailure,
                    $"{ctx}: expected Failure={expectedFailure}, got {outcome.Failure.Value}.");

                // State preserved: slot unchanged.
                PropertyCheck.That(outcome.ResultingState == slotState,
                    $"{ctx}: rejected activation changed slot state to {outcome.ResultingState}, expected {slotState}.");

                // State preserved: mana untouched. Bit-for-bit equality so a NaN reserve is preserved
                // as NaN (a mutation would change the bits) — Equals is used because NaN != NaN.
                PropertyCheck.That(currentMana.Equals(outcome.ResultingMana),
                    $"{ctx}: rejected activation altered mana to {outcome.ResultingMana}, expected {currentMana} unchanged.");
            });
        }
    }
}
