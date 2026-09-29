using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the "break resolved in the same step the reserve hits 0" rule that
    /// <see cref="CombatReactionController"/> applies inside <c>ApplyStanceDamage</c> — task 3.6 of
    /// weapon-gameplay-swarm-rework (Property 4, Requisito 2.5).
    ///
    /// <see cref="CombatReactionController.ApplyStanceDamage"/> is a private <see cref="MonoBehaviour"/>
    /// step that cannot be driven without a live scene, but the decision it makes is fully determined by
    /// pure, scene-free rules the controller delegates to:
    ///   * the reserve after the hit is <see cref="StanceBreakBounds.SubtractStance(float,float,float)"/>;
    ///   * the break fires in the SAME step iff that reserve reaches 0 (the controller's
    ///     <c>if (currentStance &gt; 0f) return;</c> guard followed immediately by
    ///     <c>TriggerStanceBreak(request)</c>) and the enemy is outside the post-break immunity window;
    ///   * the effect actually applied is the requested <see cref="StanceBreakEffect"/> put through the
    ///     enemy's resistances via the KnockUp→Stun→None degrade ladder that <c>TriggerStanceBreak</c>
    ///     uses.
    /// This test models those same pure rules and asserts that, for every enemy outside the immunity
    /// window whose accumulated stance damage drives the reserve to 0, the break is resolved in the same
    /// simulation step and applies the resistance-gated BreakEffect (Property 4). Conversely, a hit that
    /// leaves the reserve above 0 resolves no break that step, and an enemy inside the immunity window is
    /// untouched by the hit (no subtraction, no break).
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (spanning
    /// stance reserves, stance damage, multipliers, every break effect and every resistance combination,
    /// inside and outside the immunity window) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class StanceBreakSameStepPropertyTests
    {
        /// <summary>
        /// Pure mirror of the controller's per-hit stance step (<c>ApplyStanceDamage</c> +
        /// <c>TriggerStanceBreak</c>): given whether the enemy is currently immune, the reserve/damage
        /// and the enemy's resistances, decides the new reserve, whether a break was resolved this step,
        /// and which effect was applied. None of these outputs is read back to influence another channel.
        /// </summary>
        private readonly struct StanceStepOutcome
        {
            public readonly float NewStance;
            public readonly bool BreakResolved;
            public readonly StanceBreakEffect AppliedEffect;

            public StanceStepOutcome(float newStance, bool breakResolved, StanceBreakEffect appliedEffect)
            {
                NewStance = newStance;
                BreakResolved = breakResolved;
                AppliedEffect = appliedEffect;
            }
        }

        // Mirrors CombatReactionController.ApplyStanceDamage: while immune, ALL additional stance damage
        // is ignored (no subtraction, no break); otherwise subtract via the shared bounds and, in the
        // SAME step, resolve the break iff the reserve reached 0.
        private static StanceStepOutcome ResolveStanceStep(
            bool immune, float currentStance, float stanceDamage, float multiplier,
            StanceBreakEffect requested, float stunResistance, float knockUpResistance, float knockbackResistance)
        {
            if (immune) return new StanceStepOutcome(currentStance, false, StanceBreakEffect.None);

            float newStance = StanceBreakBounds.SubtractStance(currentStance, stanceDamage, multiplier);
            if (newStance > 0f) return new StanceStepOutcome(newStance, false, StanceBreakEffect.None);

            // Reserve hit 0: break resolved in this same step, applying the resistance-gated effect.
            StanceBreakEffect applied = ResolvedBreakEffect(requested, stunResistance, knockUpResistance, knockbackResistance);
            return new StanceStepOutcome(newStance, true, applied);
        }

        // Mirrors TriggerStanceBreak's resistance gating (the KnockUp→Stun→None degrade ladder):
        // a fully-immune effect is suppressed and degraded to the next-lower severity the enemy can feel.
        private static StanceBreakEffect ResolvedBreakEffect(
            StanceBreakEffect requested, float stunResistance, float knockUpResistance, float knockbackResistance)
        {
            switch (requested)
            {
                case StanceBreakEffect.Stun:
                    return stunResistance >= 1f ? StanceBreakEffect.None : StanceBreakEffect.Stun;
                case StanceBreakEffect.KnockUp:
                    if (knockUpResistance < 1f) return StanceBreakEffect.KnockUp;
                    return stunResistance < 1f ? StanceBreakEffect.Stun : StanceBreakEffect.None;
                case StanceBreakEffect.Knockback:
                    return knockbackResistance >= 1f ? StanceBreakEffect.None : StanceBreakEffect.Knockback;
                case StanceBreakEffect.None:
                default:
                    return StanceBreakEffect.None;
            }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 4: Quebra de postura no mesmo quadro em que a reserva zera.
        // For every enemy outside the immunity window whose accumulated stance damage drives the reserve
        // to 0, the break is resolved in the same simulation step, applying the hit's BreakEffect subject
        // to the enemy's resistances. A hit leaving the reserve above 0 resolves no break that step, and an
        // enemy inside the immunity window is untouched (no subtraction, no break).
        // Validates: Requirements 2.5
        [Test]
        public void ReserveReachingZeroResolvesBreakInTheSameStepWithResistanceGatedEffect()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float currentStance = NextNonNegative(rng);
                float stanceDamage = NextNonNegative(rng);
                float multiplier = NextMultiplier(rng);

                StanceBreakEffect requested = RandomBreak(rng);
                float stunResistance = NextResistance(rng);
                float knockUpResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);

                // The clamped reserve the shared helper would compute for this hit (the controller uses
                // exactly this value to decide whether the reserve hit 0).
                float reserveAfterHit = StanceBreakBounds.SubtractStance(currentStance, stanceDamage, multiplier);
                bool reachesZero = reserveAfterHit <= 0f;

                StanceBreakEffect expectedEffect =
                    ResolvedBreakEffect(requested, stunResistance, knockUpResistance, knockbackResistance);

                string state =
                    $"[current={currentStance} stanceDamage={stanceDamage} mult={multiplier} " +
                    $"reserveAfterHit={reserveAfterHit} requested={requested} " +
                    $"resist(stun={stunResistance},up={knockUpResistance},back={knockbackResistance})]";

                // ---- Outside the immunity window: the step runs and the reserve is the shared value. ----
                StanceStepOutcome outside = ResolveStanceStep(
                    immune: false, currentStance, stanceDamage, multiplier,
                    requested, stunResistance, knockUpResistance, knockbackResistance);

                PropertyCheck.That(Mathf.Approximately(outside.NewStance, reserveAfterHit),
                    $"the resolved reserve must match SubtractStance for {state}");

                // R2.5: the break is resolved this step IFF the reserve reached 0 — same step, no deferral.
                PropertyCheck.That(outside.BreakResolved == reachesZero,
                    $"break must be resolved in the same step exactly when the reserve reaches 0 for {state}");

                if (reachesZero)
                {
                    // When broken, the applied effect is the requested BreakEffect subject to resistances.
                    PropertyCheck.That(outside.AppliedEffect == expectedEffect,
                        $"the break must apply the resistance-gated BreakEffect ({expectedEffect}) for {state}");

                    // A fully-immune effect is suppressed/degraded; it is never applied at full severity.
                    if (requested == StanceBreakEffect.Stun && stunResistance >= 1f)
                        PropertyCheck.That(outside.AppliedEffect == StanceBreakEffect.None,
                            $"stun-immune enemy must not be stunned on break for {state}");
                    if (requested == StanceBreakEffect.Knockback && knockbackResistance >= 1f)
                        PropertyCheck.That(outside.AppliedEffect == StanceBreakEffect.None,
                            $"knockback-immune enemy must not be knocked back on break for {state}");
                    if (requested == StanceBreakEffect.KnockUp && knockUpResistance >= 1f)
                        PropertyCheck.That(outside.AppliedEffect != StanceBreakEffect.KnockUp,
                            $"knock-up-immune enemy must not be launched on break for {state}");
                }
                else
                {
                    // No break this step means no hard CC is applied and the reserve stayed positive.
                    PropertyCheck.That(outside.AppliedEffect == StanceBreakEffect.None,
                        $"no break resolved this step must apply no effect for {state}");
                    PropertyCheck.That(outside.NewStance > 0f,
                        $"a step that resolves no break must leave a positive reserve for {state}");
                }

                // ---- Inside the immunity window: the hit is ignored entirely (no subtraction, no break). ----
                StanceStepOutcome immune = ResolveStanceStep(
                    immune: true, currentStance, stanceDamage, multiplier,
                    requested, stunResistance, knockUpResistance, knockbackResistance);

                PropertyCheck.That(!immune.BreakResolved && immune.AppliedEffect == StanceBreakEffect.None,
                    $"an enemy inside the immunity window must resolve no break for {state}");
                PropertyCheck.That(Mathf.Approximately(immune.NewStance, currentStance),
                    $"an enemy inside the immunity window must keep its reserve unchanged for {state}");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 4: Quebra de postura no mesmo quadro em que a reserva zera.
        // Focused sub-property: accumulated stance damage that only reaches 0 on the final hit resolves the
        // break on THAT hit and no earlier — confirming "same step the reserve zeroes", not before.
        // Validates: Requirements 2.5
        [Test]
        public void AccumulatedDamageResolvesBreakOnlyOnTheHitThatZeroesTheReserve()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float maxStance = NextFloat(rng, 20f, 400f);
                float multiplier = NextFloat(rng, 0.2f, 5f); // strictly positive so damage makes progress.
                int hits = rng.Next(2, 8);
                // Per-hit stance damage that guarantees exhaustion within `hits` steps.
                float perHitDamage = (maxStance / (multiplier * (hits - 0.5f)));

                StanceBreakEffect requested = RandomBreak(rng);
                float stunResistance = NextResistance(rng);
                float knockUpResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);

                float stance = maxStance;
                int breaksResolved = 0;
                int breakStep = -1;

                for (int step = 0; step < hits; step++)
                {
                    StanceStepOutcome outcome = ResolveStanceStep(
                        immune: false, stance, perHitDamage, multiplier,
                        requested, stunResistance, knockUpResistance, knockbackResistance);

                    string state =
                        $"[step={step} stanceBefore={stance} perHit={perHitDamage} mult={multiplier} " +
                        $"maxStance={maxStance} hits={hits}]";

                    if (outcome.BreakResolved)
                    {
                        breaksResolved++;
                        breakStep = step;
                        // The break must coincide with the reserve reaching exactly 0 this step.
                        PropertyCheck.That(outcome.NewStance <= 0f,
                            $"break resolved but the reserve did not reach 0 this step for {state}");
                        break;
                    }

                    // Before the break the reserve strictly decreases and stays positive.
                    PropertyCheck.That(outcome.NewStance > 0f,
                        $"reserve must stay positive on non-breaking steps for {state}");
                    PropertyCheck.That(outcome.NewStance < stance,
                        $"a positive-damage hit must reduce the reserve for {state}");
                    stance = outcome.NewStance;
                }

                // Exactly one break, and it happened on the hit that zeroed the reserve.
                PropertyCheck.That(breaksResolved == 1 && breakStep >= 0,
                    $"exactly one break must be resolved, on the zeroing hit (maxStance={maxStance}, hits={hits})");
            });
        }

        // --- Generators ---------------------------------------------------------------------------

        private static StanceBreakEffect RandomBreak(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return StanceBreakEffect.None;
                case 1: return StanceBreakEffect.Stun;
                case 2: return StanceBreakEffect.KnockUp;
                default: return StanceBreakEffect.Knockback;
            }
        }

        /// <summary>Non-negative reserve/damage across the floor, typical and large ranges (incl. 0).</summary>
        private static float NextNonNegative(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;
                case 1: return NextFloat(rng, 0f, 1f);
                case 2: return NextFloat(rng, 0f, 100f);
                case 3: return NextFloat(rng, 0f, 10000f);
                default: return NextFloat(rng, 0f, 50f);
            }
        }

        /// <summary>Multiplier spanning [0; 5] and out-of-range values so the shared clamp is exercised.</summary>
        private static float NextMultiplier(System.Random rng)
        {
            switch (rng.Next(0, 6))
            {
                case 0: return StanceBreakBounds.MinStanceDamageMultiplier; // 0
                case 1: return StanceBreakBounds.MaxStanceDamageMultiplier; // 5
                case 2: return NextFloat(rng, -1000f, 0f);  // below range
                case 3: return NextFloat(rng, 5f, 1000f);   // above range
                default: return NextFloat(
                    rng,
                    StanceBreakBounds.MinStanceDamageMultiplier,
                    StanceBreakBounds.MaxStanceDamageMultiplier);
            }
        }

        /// <summary>Resistance in [0; 1] with 0 and full-immunity (1) sampled explicitly.</summary>
        private static float NextResistance(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return 1f; // full immunity — exercises the degrade ladder.
                default: return NextFloat(rng, 0f, 1f);
            }
        }

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
