using NUnit.Framework;
using UnityEngine;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property tests for the pure, scene-free HUD string formatter
    /// <see cref="HudFormatter"/> — task 7.4 of project-cleanup-optimization
    /// (Requisitos 2.2, 2.6).
    ///
    /// <see cref="HudFormatter"/> was extracted so the HUD can dirty-track its TMP labels without
    /// changing a single rendered character: each method must reproduce the EXACT inline format
    /// string that <see cref="PlayerHUD"/> used before dirty-tracking (health/mana resource label,
    /// asura label). Because the formatter is 100% framework-agnostic (no scene, no
    /// <see cref="UnityEngine.MonoBehaviour"/>), the "same state ⇒ identical string" guarantee can
    /// be property-checked without a live Unity scene — the pure-logic floor the spec says is the
    /// only CLI-automatable target.
    ///
    /// The test is a true oracle: it rebuilds the current inline formula INDEPENDENTLY (the literal
    /// interpolation PlayerHUD used, written out by hand here) and asserts the formatter output is
    /// byte-identical to it for the same state. If anyone silently drifts the formatter away from
    /// the inline convention, these properties fail.
    ///
    /// This project cannot resolve FsCheck/CsCheck on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property (128 by
    /// default) and reports the exact failing state as a counterexample — matching the convention of
    /// the sibling pure-logic property tests (e.g. SimpleObjectPoolPropertyTests,
    /// ProjectileMotionPropertyTests).
    ///
    /// Validates: Requirements 2.2, 2.6
    /// </summary>
    public sealed class HudFormatterMatchesInlineFormulaPropertyTests
    {
        // Feature: project-cleanup-optimization, Property 4: para o mesmo estado, a string do
        // formatter é idêntica à fórmula atual (vida/mana/asura).
        //
        // For any health/mana resource state (current, max), HudFormatter.Resource(current, max)
        // must equal the exact inline formula PlayerHUD used before dirty-tracking:
        //   $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}"
        // Health and mana share the SAME resource label formula in the HUD (RefreshResources calls
        // HudFormatter.Resource for both), so proving the resource formatter here covers vida AND
        // mana (R2.2 same format, R2.6 identical rendered character).
        // Validates: Requirements 2.2, 2.6
        [Test]
        public void ResourceFormatterIsByteIdenticalToInlineHealthManaFormula()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float current = RandomResourceValue(rng);
                float max = RandomResourceValue(rng);

                // The oracle: the literal inline interpolation the HUD used for both the health and
                // the mana labels (ceil-to-int current / ceil-to-int max).
                string expected = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";

                string actual = HudFormatter.Resource(current, max);

                PropertyCheck.That(actual == expected,
                    $"[current={current:R}, max={max:R}] Resource formatter '{actual}' != inline formula '{expected}'");
            });
        }

        // Feature: project-cleanup-optimization, Property 4 (asura facet): para o mesmo estado, a
        // string do formatter de Asura é idêntica à fórmula inline atual.
        //
        // For any asura state (ready flag, energy), HudFormatter.Asura(ready, energy) must equal the
        // exact inline formula PlayerHUD used:
        //   ready ? "ASURA PRONTO" : $"ASURA   {energy} / 100"
        // (three spaces after "ASURA" when not ready). Both branches are exercised across the full
        // energy range (R2.2 same format, R2.6 identical rendered character).
        // Validates: Requirements 2.2, 2.6
        [Test]
        public void AsuraFormatterIsByteIdenticalToInlineAsuraFormula()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                bool ready = rng.Next(0, 2) == 0;
                int energy = RandomEnergy(rng);

                // The oracle: the literal inline branch the HUD used for the asura label.
                string expected = ready ? "ASURA PRONTO" : $"ASURA   {energy} / 100";

                string actual = HudFormatter.Asura(ready, energy);

                PropertyCheck.That(actual == expected,
                    $"[ready={ready}, energy={energy}] Asura formatter '{actual}' != inline formula '{expected}'");
            });
        }

        // ---- generators ---------------------------------------------------------------------

        /// <summary>
        /// Resource values spanning the states a health/mana bar can report: negative (dead/overdrain),
        /// exactly zero, fractional values straddling the ceil boundary (e.g. 0.0001, 41.5, 99.999 —
        /// where CeilToInt rounds up), whole numbers, and large pools. This stresses the exact rounding
        /// the inline formula performs, which is where any formatter drift would show up.
        /// </summary>
        private static float RandomResourceValue(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return -5f + (float)rng.NextDouble() * 5f;           // negative .. 0
                case 1: return 0f;                                           // exactly zero
                case 2: return (float)rng.NextDouble();                      // (0,1) -> ceils to 1
                case 3: return (float)rng.NextDouble() * 150f;               // typical fractional pool
                case 4: return rng.Next(0, 1000);                            // whole number
                default: return 1000f + (float)rng.NextDouble() * 9000f;     // large pool
            }
        }

        /// <summary>
        /// Asura energy values spanning the full 0..100 charge range plus out-of-band values
        /// (negative, overcharged) so the formatter's integer interpolation is exercised everywhere
        /// the inline formula would be.
        /// </summary>
        private static int RandomEnergy(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0;                       // empty
                case 1: return 100;                     // full
                case 2: return rng.Next(1, 100);        // partial charge
                case 3: return rng.Next(-50, 0);        // out-of-band negative
                default: return rng.Next(101, 500);     // out-of-band overcharge
            }
        }
    }
}
