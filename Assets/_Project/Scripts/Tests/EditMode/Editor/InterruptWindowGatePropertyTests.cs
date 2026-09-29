using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the conditional-interruption rule in
    /// <see cref="InterruptWindowGate.Resolve"/> — task 8.5 of weapon-gameplay-swarm-rework
    /// (Requisitos 7.6, 7.7 / Property 23).
    ///
    /// The live pipeline keeps the interruptible-window state on the acting <see cref="EnemyAI"/>
    /// (its <c>IsWindingUp</c> flag) and asks this pure gate what an incoming interruption should do
    /// this frame. The rule was extracted into the scene-free static
    /// <see cref="InterruptWindowGate"/> so the cross-cutting invariant can be property-checked
    /// without a live Unity scene (mirrors <c>ControlLockGate</c> / <c>BreakEffectResistance</c>).
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases (every
    /// window state) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class InterruptWindowGatePropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 23: Interrupção condicionada à janela interrompível
        // Para todo inimigo atingido por uma habilidade de interrupção: se o ataque está em janela
        // interrompível, o ataque é cancelado e seu beat de dano pendente é suprimido no mesmo quadro;
        // caso contrário, o ataque em curso é preservado, o beat não é suprimido e uma reação de
        // ausência de interrupção é sinalizada. Os dois resultados são exclusivos e exaustivos.
        // Validates: Requirements 7.6, 7.7
        [Test]
        public void InterruptionIsGatedByTheInterruptibleWindow()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate the full space of the single observed state: whether the target's attack is
                // currently in its interruptible window (winding up, beat not yet resolved).
                bool attackInInterruptibleWindow = rng.Next(0, 2) == 0;

                InterruptWindowGate.Decision decision =
                    InterruptWindowGate.Resolve(attackInInterruptibleWindow);

                if (attackInInterruptibleWindow)
                {
                    // R7.6: in-window hit cancels the attack AND suppresses its pending beat same-frame,
                    // and does NOT signal a no-interruption reaction.
                    PropertyCheck.That(decision.CancelsAttack,
                        "in-window interruption did not cancel the attack (R7.6)");
                    PropertyCheck.That(decision.SuppressesBeat,
                        "in-window interruption did not suppress the pending damage beat (R7.6)");
                    PropertyCheck.That(!decision.SignalsNoInterruption,
                        "in-window interruption wrongly signalled a no-interruption reaction (R7.6)");
                }
                else
                {
                    // R7.7: out-of-window hit preserves the attack (no cancel), does NOT suppress the
                    // beat, and signals the no-interruption reaction.
                    PropertyCheck.That(!decision.CancelsAttack,
                        "out-of-window interruption wrongly cancelled the attack (R7.7)");
                    PropertyCheck.That(!decision.SuppressesBeat,
                        "out-of-window interruption wrongly suppressed the pending damage beat (R7.7)");
                    PropertyCheck.That(decision.SignalsNoInterruption,
                        "out-of-window interruption did not signal the no-interruption reaction (R7.7)");
                }

                // Exclusive and exhaustive: exactly one of the two outcomes holds, so cancellation and
                // no-interruption signalling are mutually exclusive, and the beat is suppressed iff the
                // attack is cancelled.
                PropertyCheck.That(decision.CancelsAttack != decision.SignalsNoInterruption,
                    $"outcomes not mutually exclusive for inWindow={attackInInterruptibleWindow}: " +
                    $"cancels={decision.CancelsAttack} signalsNoInterruption={decision.SignalsNoInterruption}");
                PropertyCheck.That(decision.CancelsAttack == decision.SuppressesBeat,
                    $"beat suppression must track attack cancellation for inWindow={attackInInterruptibleWindow}: " +
                    $"cancels={decision.CancelsAttack} suppresses={decision.SuppressesBeat}");
            });
        }
    }
}
