using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the deterministic effect-degrade ladder applied on a Stance Break —
    /// task 3.10 of weapon-gameplay-swarm-rework (Property 7, Requisito 2.8).
    ///
    /// The rule is a pure, scene-free decision extracted into <see cref="BreakEffectResistance"/>: given
    /// the requested <see cref="StanceBreakEffect"/> and each hard-CC channel's resistance in [0, 1]
    /// (1.0 = fully immune), the effect actually applied is
    /// <list type="bullet">
    ///   <item>the requested effect when the enemy is NOT fully immune to it (resistance &lt; 1.0);</item>
    ///   <item>otherwise the lowest-severity effect it is not fully immune to along the KnockUp→Stun
    ///   ladder;</item>
    ///   <item>otherwise <see cref="StanceBreakEffect.None"/>.</item>
    /// </list>
    /// This test drives <see cref="BreakEffectResistance.Resolve"/> directly (the pure helper the owning
    /// <c>CombatReactionController.TriggerStanceBreak</c> delegates to) and checks its output against an
    /// independent expected model derived from the acceptance criteria, without touching the controller.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (spanning
    /// every requested effect and every per-channel resistance combination, with 0 and full-immunity 1.0
    /// sampled explicitly and out-of-range values exercising the &gt;= 1 clamp) and reports the exact
    /// failing case as a counterexample.
    /// </summary>
    public sealed class BreakEffectDeterministicDegradePropertyTests
    {
        /// <summary>
        /// Independent expected model of Requisito 2.8, derived straight from the acceptance criteria
        /// (NOT from <see cref="BreakEffectResistance.Resolve"/>'s branching) so the test can falsify the
        /// helper rather than merely restate it. Severity ladder is KnockUp (high) → Stun (low); Knockback
        /// and None have no softer target.
        /// </summary>
        private static StanceBreakEffect ExpectedApplied(
            StanceBreakEffect requested, float stunResistance, float knockUpResistance, float knockbackResistance)
        {
            bool stunImmune = stunResistance >= 1f;
            bool knockUpImmune = knockUpResistance >= 1f;
            bool knockbackImmune = knockbackResistance >= 1f;

            switch (requested)
            {
                case StanceBreakEffect.KnockUp:
                    // Requested launch: keep it unless immune, then step down the ladder to Stun, then None.
                    if (!knockUpImmune) return StanceBreakEffect.KnockUp;
                    if (!stunImmune) return StanceBreakEffect.Stun;
                    return StanceBreakEffect.None;

                case StanceBreakEffect.Stun:
                    // Nothing softer than Stun on the ladder: apply it or fall through to None.
                    return stunImmune ? StanceBreakEffect.None : StanceBreakEffect.Stun;

                case StanceBreakEffect.Knockback:
                    // Knockback has no defined degrade target: apply it or fall through to None.
                    return knockbackImmune ? StanceBreakEffect.None : StanceBreakEffect.Knockback;

                case StanceBreakEffect.None:
                default:
                    return StanceBreakEffect.None;
            }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 7: Degradê determinístico de efeito por imunidade.
        // For every combination of resistances and requested BreakEffect, the applied effect is the
        // requested one when the enemy is not fully immune to it (resistance 1.0); otherwise it is the
        // lowest-severity effect it is not fully immune to along the KnockUp→Stun ladder, or None when no
        // effect is applicable.
        // Validates: Requirements 2.8
        [Test]
        public void ResolveDegradesRequestedEffectDeterministicallyByImmunity()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                StanceBreakEffect requested = RandomBreak(rng);
                float stunResistance = NextResistance(rng);
                float knockUpResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);

                StanceBreakEffect applied = BreakEffectResistance.Resolve(
                    requested, stunResistance, knockUpResistance, knockbackResistance);
                StanceBreakEffect expected = ExpectedApplied(
                    requested, stunResistance, knockUpResistance, knockbackResistance);

                string state =
                    $"[requested={requested} " +
                    $"resist(stun={stunResistance},up={knockUpResistance},back={knockbackResistance})]";

                // Core property: the resolved effect matches the deterministic degrade model exactly.
                PropertyCheck.That(applied == expected,
                    $"resolved effect {applied} did not match the deterministic degrade model {expected} for {state}");

                bool stunImmune = BreakEffectResistance.IsFullyImmune(stunResistance);
                bool knockUpImmune = BreakEffectResistance.IsFullyImmune(knockUpResistance);
                bool knockbackImmune = BreakEffectResistance.IsFullyImmune(knockbackResistance);

                // A requested effect is applied unchanged exactly when the enemy is not fully immune to it.
                switch (requested)
                {
                    case StanceBreakEffect.KnockUp:
                        if (!knockUpImmune)
                            PropertyCheck.That(applied == StanceBreakEffect.KnockUp,
                                $"non-immune KnockUp must be applied unchanged for {state}");
                        else
                            // KnockUp-immune must never launch; it degrades to Stun (if feelable) or None.
                            PropertyCheck.That(applied != StanceBreakEffect.KnockUp,
                                $"KnockUp-immune enemy must not be launched for {state}");
                        break;

                    case StanceBreakEffect.Stun:
                        PropertyCheck.That(
                            applied == (stunImmune ? StanceBreakEffect.None : StanceBreakEffect.Stun),
                            $"Stun resolves to Stun unless stun-immune (then None) for {state}");
                        break;

                    case StanceBreakEffect.Knockback:
                        PropertyCheck.That(
                            applied == (knockbackImmune ? StanceBreakEffect.None : StanceBreakEffect.Knockback),
                            $"Knockback resolves to Knockback unless knockback-immune (then None) for {state}");
                        break;

                    case StanceBreakEffect.None:
                        PropertyCheck.That(applied == StanceBreakEffect.None,
                            $"a None request always resolves to None for {state}");
                        break;
                }

                // Ladder invariant: the applied effect is never MORE severe than what was requested, and it
                // is only ever the requested effect or a lower rung the enemy can still feel, or None.
                PropertyCheck.That(Severity(applied) <= Severity(requested),
                    $"the applied effect must never be more severe than the requested one for {state}");

                // Fully-immune channels can never surface their own effect after the degrade.
                if (stunImmune)
                    PropertyCheck.That(applied != StanceBreakEffect.Stun,
                        $"stun-immune enemy must never end up Stunned for {state}");
                if (knockUpImmune)
                    PropertyCheck.That(applied != StanceBreakEffect.KnockUp,
                        $"knock-up-immune enemy must never end up KnockedUp for {state}");
                if (knockbackImmune)
                    PropertyCheck.That(applied != StanceBreakEffect.Knockback,
                        $"knockback-immune enemy must never end up KnockedBack for {state}");

                // Determinism: resolving the same inputs again yields the identical effect.
                StanceBreakEffect again = BreakEffectResistance.Resolve(
                    requested, stunResistance, knockUpResistance, knockbackResistance);
                PropertyCheck.That(again == applied,
                    $"Resolve must be deterministic for identical inputs for {state}");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 7: Degradê determinístico de efeito por imunidade.
        // Focused sub-property for the ladder itself: a KnockUp-immune-but-not-Stun-immune enemy is
        // stunned (KnockUp→Stun step), while a KnockUp-and-Stun-immune enemy resolves to None. Confirms
        // the degrade steps down exactly one rung to the lowest-severity feelable effect.
        // Validates: Requirements 2.8
        [Test]
        public void KnockUpDegradesToStunWhenLaunchImmuneButStunnable()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Fully immune to launch by construction; sample the stun channel across [0, 1] incl. 1.0.
                float knockUpResistance = NextFloat(rng, BreakEffectResistance.FullImmunity, 3f); // >= 1 -> immune
                float stunResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);

                StanceBreakEffect applied = BreakEffectResistance.Resolve(
                    StanceBreakEffect.KnockUp, stunResistance, knockUpResistance, knockbackResistance);

                string state =
                    $"[requested=KnockUp up={knockUpResistance} stun={stunResistance} back={knockbackResistance}]";

                if (BreakEffectResistance.IsFullyImmune(stunResistance))
                    PropertyCheck.That(applied == StanceBreakEffect.None,
                        $"KnockUp+Stun-immune enemy must resolve to None for {state}");
                else
                    PropertyCheck.That(applied == StanceBreakEffect.Stun,
                        $"KnockUp-immune-but-stunnable enemy must be Stunned for {state}");
            });
        }

        // --- Severity ladder (higher value = more severe) -----------------------------------------
        // KnockUp is the most severe hard CC in scope, then Stun; Knockback sits above Stun as a hard CC
        // but has no ladder target; None is the softest outcome.
        private static int Severity(StanceBreakEffect effect)
        {
            switch (effect)
            {
                case StanceBreakEffect.KnockUp: return 3;
                case StanceBreakEffect.Knockback: return 2;
                case StanceBreakEffect.Stun: return 1;
                case StanceBreakEffect.None:
                default: return 0;
            }
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

        /// <summary>
        /// Resistance in [0, 1] with 0 and full-immunity (1) sampled explicitly, plus occasional
        /// out-of-range values so the helper's <c>&gt;= 1</c> immunity clamp is exercised.
        /// </summary>
        private static float NextResistance(System.Random rng)
        {
            switch (rng.Next(0, 5))
            {
                case 0: return 0f;
                case 1: return 1f;                       // exact full immunity
                case 2: return NextFloat(rng, 1f, 5f);   // above range -> still immune (>= 1)
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
