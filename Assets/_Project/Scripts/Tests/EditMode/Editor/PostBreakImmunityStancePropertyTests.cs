using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the post-break immunity gate of the Stance/Stagger reaction
    /// system — task 3.7 of weapon-gameplay-swarm-rework (Property 5, Requisito 2.6).
    ///
    /// The gate lives in <c>CombatReactionController.ApplyStanceDamage</c> as a pure time
    /// comparison: <c>if (Time.time &lt; breakImmuneUntil) return;</c>. While the current time is
    /// inside the post-break immunity window, ALL additional stance damage is ignored, so the
    /// enemy's reserve stays untouched by that hit; the window length itself is clamped to
    /// [0.1; 10] s by <see cref="StanceBreakBounds.ClampBreakImmunitySeconds"/>. That decision is
    /// scene-free logic, so it is reproduced here as a pure gate over the same inputs
    /// (current time vs. the armed <c>breakImmuneUntil</c>) and the same
    /// <see cref="StanceBreakBounds.SubtractStance"/> subtraction the controller uses — without
    /// touching <c>CombatReactionController</c>.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases per property
    /// (sampling times across and around the immunity window plus arbitrary stance damage and
    /// multipliers) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class PostBreakImmunityStancePropertyTests
    {
        /// <summary>
        /// Pure model of the controller's stance-damage gate (Requisito 2.6): while
        /// <paramref name="now"/> is before the armed <paramref name="breakImmuneUntil"/> instant,
        /// the hit is ignored and the reserve is returned unchanged; otherwise the reserve is
        /// reduced by the same <see cref="StanceBreakBounds.SubtractStance"/> the controller applies.
        /// Mirrors <c>if (Time.time &lt; breakImmuneUntil) return;</c> followed by the subtraction.
        /// </summary>
        private static float ApplyStanceDamageGated(
            float currentStance, float now, float breakImmuneUntil, float stanceDamage, float multiplier)
        {
            if (now < breakImmuneUntil) return currentStance; // immune: ignore ALL additional stance damage
            return StanceBreakBounds.SubtractStance(currentStance, stanceDamage, multiplier);
        }

        // Feature: weapon-gameplay-swarm-rework, Property 5: Imunidade pós-quebra ignora dano de postura adicional.
        // For every hit with stance damage applied while the current time is inside the post-break
        // immunity window (now < breakImmuneUntil), the enemy's stance reserve remains unchanged by
        // that hit. As a control, a hit applied at or after the window expires (now >= breakImmuneUntil)
        // with positive stance damage does subtract, proving the gate is meaningful and not a no-op.
        // Validates: Requirements 2.6
        [Test]
        public void HitDuringPostBreakImmunityWindowLeavesStanceUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Reserve any enemy stance value, and a stance-damage multiplier across the full
                // clamped range [0; 5] (SubtractStance clamps it too).
                float maxStance = 1f + (float)rng.NextDouble() * 899f; // 1..900, spanning the rank defaults
                float currentStance = (float)rng.NextDouble() * maxStance; // 0..maxStance
                float multiplier = NextFloat(rng,
                    StanceBreakBounds.MinStanceDamageMultiplier, StanceBreakBounds.MaxStanceDamageMultiplier);

                // A break just happened at breakTime; the window is a clamped [0.1; 10] s duration,
                // exactly as CombatReactionController arms breakImmuneUntil = Time.time + clamped(window).
                float breakTime = (float)rng.NextDouble() * 1000f; // arbitrary simulation clock
                float windowSeconds = StanceBreakBounds.ClampBreakImmunitySeconds(
                    NextFloat(rng, -5f, 15f)); // sample below/above the range too; clamp pins it in-band
                float breakImmuneUntil = breakTime + windowSeconds;

                // Positive stance damage — the hit genuinely carries stance damage (matches the
                // controller only reaching ApplyStanceDamage when StanceDamage > 0).
                float stanceDamage = NextFloat(rng, 0.01f, 300f);

                string state =
                    $"[current={currentStance} max={maxStance} mult={multiplier} " +
                    $"breakTime={breakTime} window={windowSeconds} immuneUntil={breakImmuneUntil} " +
                    $"stanceDamage={stanceDamage}]";

                // ---- Inside the immunity window: now in [breakTime; breakImmuneUntil) -----------
                // Pick a time strictly before the window closes, so the gate must ignore the hit.
                float span = breakImmuneUntil - breakTime; // == windowSeconds >= 0.1
                float insideNow = breakTime + (float)rng.NextDouble() * span * 0.999f; // never reaches the close
                PropertyCheck.That(insideNow < breakImmuneUntil,
                    $"test setup: insideNow must be within the window for {state}");

                float afterImmune = ApplyStanceDamageGated(
                    currentStance, insideNow, breakImmuneUntil, stanceDamage, multiplier);

                // Property 5 core assertion: the reserve is untouched by a hit during immunity.
                PropertyCheck.That(afterImmune == currentStance,
                    $"stance changed during post-break immunity (got {afterImmune}, expected {currentStance}) " +
                    $"at now={insideNow} for {state}");

                // ---- Control: at/after the window closes, positive damage does subtract ---------
                // now >= breakImmuneUntil ends immunity; with stanceDamage > 0 and mult > 0 (and a
                // non-zero reserve) the reserve must strictly decrease, confirming the gate is real.
                if (currentStance > 0f && multiplier > 0f)
                {
                    float outsideNow = breakImmuneUntil + (float)rng.NextDouble() * 5f; // at or past the close
                    float afterExpiry = ApplyStanceDamageGated(
                        currentStance, outsideNow, breakImmuneUntil, stanceDamage, multiplier);
                    PropertyCheck.That(afterExpiry <= currentStance,
                        $"stance must not increase once immunity ends (got {afterExpiry}) for {state}");
                    PropertyCheck.That(afterExpiry < currentStance,
                        $"positive stance damage after immunity must reduce the reserve " +
                        $"(got {afterExpiry}, was {currentStance}) at now={outsideNow} for {state}");
                }
            });
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
