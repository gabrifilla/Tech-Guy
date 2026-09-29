using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for AsuraMomentum category-alternation energy gain.
    ///
    /// The pure class <see cref="AsuraMomentum"/> owns the Manoplas Asura energy contract (Requisito
    /// 8.10): the first registration always grants 10, and every later registration grants 25 when
    /// its category (stamina = false / shock = true) differs from the previous one and 10 when it
    /// repeats, with the running total clamped at <see cref="AsuraMomentum.Maximum"/> (100). This test
    /// drives random sequences of RegisterSkill(shock) calls and mirrors the expected energy with an
    /// independent reference implementation, asserting Energy matches at every step.
    /// </summary>
    public sealed class AsuraCategoryAlternationEnergyPropertyTests
    {
        private const int SwitchGain = 25;
        private const int RepeatGain = 10;
        private const int Cap = AsuraMomentum.Maximum; // 100

        // Feature: weapon-gameplay-swarm-rework, Property 30: Ganho de Asura por alternância de categoria
        // Para toda sequência de habilidades registradas em AsuraMomentum, cada registro adiciona 25 de
        // energia quando a categoria (stamina/shock) difere da anterior e 10 quando é igual, com o total
        // limitado a 100. O primeiro registro adiciona 10 (não há categoria anterior).
        // Validates: Requirements 8.10
        [Test]
        public void RegisterSkill_AlternationGrants25_RepeatGrants10_CappedAt100()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Generate a random-length sequence of stamina/shock registrations. Lengths span from a
                // single call (only the first-registration branch) up to well beyond the cap so the
                // clamp at 100 is exercised repeatedly.
                int length = rng.Next(1, 25);

                var momentum = new AsuraMomentum();

                int expected = 0;
                bool? lastShock = null;

                for (int step = 0; step < length; step++)
                {
                    bool shock = rng.Next(0, 2) == 0;

                    // Reference model: +25 on a category switch, +10 on a repeat or the first call, capped.
                    int gain = (lastShock.HasValue && lastShock.Value != shock) ? SwitchGain : RepeatGain;
                    expected = Math.Min(Cap, expected + gain);
                    lastShock = shock;

                    momentum.RegisterSkill(shock);

                    PropertyCheck.That(momentum.Energy == expected,
                        $"length={length}, step={step}, shock={shock}: Energy={momentum.Energy}, expected={expected}");
                    PropertyCheck.That(momentum.Energy >= 0 && momentum.Energy <= Cap,
                        $"Energy out of range: length={length}, step={step}, Energy={momentum.Energy}");
                }
            });
        }
    }
}
