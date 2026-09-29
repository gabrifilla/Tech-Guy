using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for Property 28 of weapon-gameplay-swarm-rework
    /// (R nega Asura quando a energia não está cheia).
    ///
    /// Property 28 — para todo valor de <see cref="AsuraMomentum.Energy"/> em [0; 99], ativar R
    /// (modelado por <see cref="AsuraMomentum.TryConsume"/>) nega a entrada no modo Asura
    /// (retorna <c>false</c>, <see cref="AsuraMomentum.IsReady"/> permanece falso) e preserva a
    /// energia atual sem alteração. Ver Requisito 8.7: "IF o jogador ativa R com AsuraMomentum
    /// abaixo de 100, THEN … negar a entrada no modo Asura e preservar o valor atual … sem alteração".
    ///
    /// Como controle, no limite superior <see cref="AsuraMomentum.Maximum"/> (Energy == 100) a
    /// ativação entra em Asura (retorna <c>true</c>) e zera a energia — o comportamento oposto que
    /// delimita a faixa negada (Requisito 8.6).
    ///
    /// Approach: <see cref="AsuraMomentum"/> é uma classe pura (sem MonoBehaviour/cena), extraída
    /// justamente para ser testável sem cena. A energia é semeada apenas pelos pontos de entrada
    /// públicos (<see cref="AsuraMomentum.AddEnergy"/> e <see cref="AsuraMomentum.RegisterSkill"/>),
    /// nunca por reflexão, para exercitar o contrato real.
    /// </summary>
    public sealed class AsuraDenyWhenNotFullPropertyTests
    {
        // Builds an AsuraMomentum whose Energy lands exactly on `target` in [0; 100], using only the
        // public entry points. RegisterSkill (alternating categories) is exercised on some cases so
        // the seeding path is not limited to AddEnergy.
        private static AsuraMomentum WithEnergy(System.Random rng, int target)
        {
            var meter = new AsuraMomentum();

            // Occasionally warm up via RegisterSkill (grants 10/25, capped at 100) before topping
            // up with AddEnergy to hit `target` exactly. AddEnergy clamps at Maximum and ignores
            // negatives, so a single top-up of (target - Energy) reaches the target precisely for
            // any Energy <= target.
            if (rng.Next(0, 2) == 0)
            {
                int skills = rng.Next(0, 6);
                for (int s = 0; s < skills && meter.Energy < target; s++)
                {
                    meter.RegisterSkill(rng.Next(0, 2) == 0);
                }
                if (meter.Energy > target)
                {
                    // Overshot the target via RegisterSkill; restart clean and seed purely additively.
                    meter = new AsuraMomentum();
                }
            }

            meter.AddEnergy(target - meter.Energy);
            return meter;
        }

        // Feature: weapon-gameplay-swarm-rework, Property 28: R nega Asura quando a energia não está cheia
        // For any Energy in [0; 99], TryConsume denies Asura (false) and leaves Energy unchanged;
        // as a control, at Energy == 100 TryConsume enters Asura (true) and zeroes the meter.
        // Validates: Requirements 8.7
        [Test]
        public void RDeniesAsuraAndPreservesEnergyWhenBelowMaximum()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Sample the denied band [0; 99], with the two boundaries (0 and 99) forced so the
                // extremes are always covered alongside the random interior values.
                int energy = i switch
                {
                    0 => 0,
                    1 => AsuraMomentum.Maximum - 1,             // 99
                    _ => rng.Next(0, AsuraMomentum.Maximum),    // 0..99
                };

                var meter = WithEnergy(rng, energy);

                PropertyCheck.That(meter.Energy == energy,
                    $"seed setup failed: expected Energy={energy}, got {meter.Energy}");
                PropertyCheck.That(!meter.IsReady,
                    $"Energy={energy} (< {AsuraMomentum.Maximum}) must report IsReady=false, got true");

                bool entered = meter.TryConsume();

                PropertyCheck.That(!entered,
                    $"Energy={energy}: TryConsume must deny Asura (return false) below the cap, got true");
                PropertyCheck.That(meter.Energy == energy,
                    $"Energy={energy}: denied activation must preserve energy unchanged, got {meter.Energy}");
                PropertyCheck.That(!meter.IsReady,
                    $"Energy={energy}: meter must remain not-ready after a denied activation");

                // Control at the boundary that IS allowed: a full meter enters Asura and zeroes.
                var full = WithEnergy(rng, AsuraMomentum.Maximum);
                PropertyCheck.That(full.IsReady,
                    $"full meter must report IsReady=true, got Energy={full.Energy}");
                bool enteredFull = full.TryConsume();
                PropertyCheck.That(enteredFull,
                    "Energy==100: TryConsume must enter Asura (return true)");
                PropertyCheck.That(full.Energy == 0,
                    $"Energy==100: entering Asura must zero the meter, got {full.Energy}");
            });
        }
    }
}
