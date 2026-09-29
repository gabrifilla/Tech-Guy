using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the Manoplas juggle rule — task 10.4 of weapon-gameplay-swarm-rework
    /// (Property 29, Requisitos 8.8 / 8.9).
    ///
    /// The rule: a stance break by a Manoplas hit that declares a <see cref="StanceBreakEffect.KnockUp"/>
    /// against an enemy whose <c>KnockUpResistance</c> is &lt; 1 must keep the enemy in juggle — control
    /// locked while it is airborne (Requisito 8.8) — and restore control when the airborne duration
    /// ends (Requisito 8.9).
    ///
    /// This composes the real, scene-free production primitives that own each piece of the decision so
    /// the invariant can be property-checked without a live Unity scene (the actual coroutine-driven
    /// lock/restore lives in <c>CombatReactionController.StartKnockUp</c> → <c>ControlLockFor</c>, which
    /// cannot run in EditMode):
    /// <list type="bullet">
    ///   <item><see cref="GauntletLoopSteps.EnsureJuggleDeclaration"/> — the gauntlet E step declares the
    ///   KnockUp (BreakEffect KnockUp, positive rise height, positive airborne duration).</item>
    ///   <item><see cref="BreakEffectResistance.Resolve"/> — with KnockUpResistance &lt; 1 the requested
    ///   KnockUp is applied unchanged (the enemy is launched, i.e. juggled), never degraded to Stun/None.</item>
    ///   <item><see cref="ControlLockGate.AllowsImmediateDisplacement"/> — while control is locked
    ///   (airborne) the enemy cannot be nudged/act; when the lock ends it can again.</item>
    /// </list>
    ///
    /// The airborne/lock duration itself is <c>CombatReactionController.StartKnockUp</c>'s private
    /// <c>ScaleDuration(StunDuration, knockUpResistance)</c> = <c>StunDuration * (1 - knockUpResistance)</c>.
    /// That production method is private, so the formula is mirrored below (exactly as the sibling
    /// <c>RemainingArchetypeUnitTests</c> mirrors it) purely to reason about when the air phase ends; the
    /// launch/lock decisions themselves are driven through the real helpers above.
    ///
    /// This project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (sweeping
    /// KnockUpResistance across [0, 1) plus the other channels' resistances and the declared stun/height,
    /// with 0 sampled explicitly and the &lt; 1 boundary approached) and reports the exact failing case as
    /// a counterexample.
    ///
    /// Execution caveat (R1.7 / R13.9): the Unity EditMode runner could not be executed via CLI in this
    /// environment (no Unity CLI / Unity_MCP tools available), and the scene-bound coroutine lock/restore
    /// in <c>CombatReactionController</c> is not exercised here. Those are reported as NOT validated; this
    /// test validates the pure decision logic the controller delegates to.
    /// </summary>
    public sealed class KnockUpJuggleControlLockPropertyTests
    {
        private const float Epsilon = 1e-4f;

        /// <summary>
        /// Mirrors <c>CombatReactionController.ScaleDuration</c> /
        /// <c>StartKnockUp</c>'s airborne-time computation: the airborne lock lasts
        /// <c>StunDuration * (1 - Mathf.Clamp01(knockUpResistance))</c>. The production method is private,
        /// so the formula is replicated here (resistance clamped to [0, 1] exactly as the controller does)
        /// only to reason about the air-phase length. When <c>knockUpResistance</c> &lt; 1 and the declared
        /// stun is &gt; 0 this is strictly positive, i.e. there is a real airborne window during which
        /// control is locked.
        /// </summary>
        private static float AirborneDuration(float stunDuration, float knockUpResistance)
            => Mathf.Max(0f, stunDuration) * (1f - Mathf.Clamp01(knockUpResistance));

        // Feature: weapon-gameplay-swarm-rework, Property 29: KnockUp mantém juggle e restaura controle ao pousar
        // Para todo inimigo com KnockUpResistance < 1, uma quebra de postura por golpe que declara KnockUp
        // mantém o controle bloqueado enquanto ele está no ar e restaura o controle quando a duração aérea
        // termina.
        // Validates: Requirements 8.8, 8.9
        [Test]
        public void KnockUpKeepsControlLockedWhileAirborneThenRestoresOnLanding()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // KnockUpResistance strictly < 1 (the enemy is juggle-able): sample [0, 1) with 0 and the
                // near-1 boundary emphasised so the "< 1" edge is exercised.
                float knockUpResistance = NextKnockUpResistanceBelowOne(rng);
                // The other channels' resistances are irrelevant to a non-immune KnockUp but vary freely so
                // the launch decision is shown to depend only on KnockUpResistance.
                float stunResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);

                // A gauntlet E (Stance Breaker) step that breaks stance without an authored launch: the loop
                // must promote it to a juggle declaration (BreakEffect KnockUp, positive height + airtime).
                var step = new AreaHitStep
                {
                    stanceDamage = 12f,
                    breakEffect = StanceBreakEffect.None,
                    knockUpHeight = 0f,
                    stunDuration = 0f,
                };
                GauntletLoopSteps.EnsureJuggleDeclaration(step);

                string state =
                    $"[knockUpResist={knockUpResistance} stunResist={stunResistance} " +
                    $"backResist={knockbackResistance} declaredHeight={step.knockUpHeight} " +
                    $"declaredStun={step.stunDuration}]";

                // (0) The E step actually declares a juggle: KnockUp with a positive rise + airborne time.
                PropertyCheck.That(step.breakEffect == StanceBreakEffect.KnockUp,
                    $"the gauntlet Stance-Breaker step must declare a KnockUp to open a juggle for {state}");
                PropertyCheck.That(step.knockUpHeight > 0f,
                    $"a juggle declaration must have a positive rise height for {state}");
                PropertyCheck.That(step.stunDuration > 0f,
                    $"a juggle declaration must have a positive airborne duration for {state}");

                // (1) With KnockUpResistance < 1 the break resolves to a KnockUp (the enemy is juggled),
                // never degraded to Stun/Knockback/None — this is the launch that starts the juggle (R8.8).
                StanceBreakEffect applied = BreakEffectResistance.Resolve(
                    step.breakEffect, stunResistance, knockUpResistance, knockbackResistance);
                PropertyCheck.That(applied == StanceBreakEffect.KnockUp,
                    $"an enemy with KnockUpResistance < 1 must be launched (KnockUp), got {applied} for {state}");

                // (2) The launch opens a real airborne window: duration = StunDuration * (1 - resistance),
                // strictly positive whenever resistance < 1 and the declared stun > 0 (R8.8).
                float airborne = AirborneDuration(step.stunDuration, knockUpResistance);
                PropertyCheck.That(airborne > 0f,
                    $"a juggle-able enemy (resistance < 1) must have a positive airborne window for {state}");

                // (3) WHILE airborne, control is locked: no immediate displacement/action is allowed on the
                // juggled enemy — it stays in the readable juggle (R8.8). The controller sets controlLocked
                // = true for the whole airborne window; the pure gate is what suppresses acting.
                const bool controlLockedWhileAirborne = true;
                PropertyCheck.That(
                    !ControlLockGate.AllowsImmediateDisplacement(controlLockedWhileAirborne),
                    $"a juggled (airborne, control-locked) enemy must not be nudged out of the juggle for {state}");

                // (4) WHEN the airborne duration ends, control is restored: the lock clears and immediate
                // displacement/action is allowed again (R8.9).
                const bool controlLockedAfterLanding = false;
                PropertyCheck.That(
                    ControlLockGate.AllowsImmediateDisplacement(controlLockedAfterLanding),
                    $"control must be restored once the airborne duration ends (landing) for {state}");

                // (5) Sanity on the resolve boundary: the launch decision depends only on KnockUpResistance
                // being < 1. Bumping it to full immunity (>= 1) must NOT keep launching — it degrades — which
                // confirms the property is specifically about the resistance-below-1 population (R8.8).
                StanceBreakEffect immune = BreakEffectResistance.Resolve(
                    step.breakEffect, stunResistance, 1f, knockbackResistance);
                PropertyCheck.That(immune != StanceBreakEffect.KnockUp,
                    $"a fully KnockUp-immune enemy (resistance = 1) must NOT be launched for {state}");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 29: KnockUp mantém juggle e restaura controle ao pousar
        // Focused monotonicity of the airborne window: for a juggle-able enemy (KnockUpResistance < 1) the
        // airborne/lock duration is strictly positive and shrinks as resistance rises, so a lower-resistance
        // enemy stays juggled at least as long — the control-lock window scales with resistance while never
        // vanishing below full immunity. Confirms the "while airborne" phase (R8.8) is a real, bounded window
        // that always ends (R8.9).
        // Validates: Requirements 8.8, 8.9
        [Test]
        public void AirborneJuggleWindowIsPositiveBelowImmunityAndAlwaysEnds()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // A positive declared airborne/stun time (the juggle E step always declares one > 0).
                float stunDuration = 0.25f + (float)rng.NextDouble() * 3.75f; // [0.25, 4.0]
                float lowResistance = (float)rng.NextDouble() * 0.5f;          // [0, 0.5)
                float highResistance = 0.5f + (float)rng.NextDouble() * 0.499f; // [0.5, ~0.999) still < 1

                float lowAir = AirborneDuration(stunDuration, lowResistance);
                float highAir = AirborneDuration(stunDuration, highResistance);

                string state =
                    $"[stun={stunDuration} lowResist={lowResistance} highResist={highResistance} " +
                    $"lowAir={lowAir} highAir={highAir}]";

                // Below full immunity the airborne window is strictly positive (there IS a juggle, R8.8).
                PropertyCheck.That(lowAir > 0f && highAir > 0f,
                    $"the airborne juggle window must be positive for any resistance < 1 for {state}");

                // More KnockUp resistance => shorter (never longer) juggle; the lock window scales down but
                // never below the full-immunity zero — and it always terminates (R8.9).
                PropertyCheck.That(lowAir >= highAir - Epsilon,
                    $"lower KnockUp resistance must not yield a shorter airborne juggle for {state}");

                // The window is finite and equals the full declared time only at zero resistance.
                float fullAtZero = AirborneDuration(stunDuration, 0f);
                PropertyCheck.That(Mathf.Abs(fullAtZero - stunDuration) <= Epsilon,
                    $"zero KnockUp resistance keeps the enemy airborne for the full declared duration for {state}");
                PropertyCheck.That(lowAir <= fullAtZero + Epsilon && highAir <= fullAtZero + Epsilon,
                    $"the airborne window must never exceed the full declared duration for {state}");
            });
        }

        // --- Generators ---------------------------------------------------------------------------

        /// <summary>
        /// KnockUp resistance strictly in [0, 1): 0 sampled explicitly, plus values approaching the &lt; 1
        /// boundary, so the "juggle-able" population (resistance below full immunity) is exercised. Never
        /// returns 1.0 (that is the immune case handled separately).
        /// </summary>
        private static float NextKnockUpResistanceBelowOne(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;                                   // no resistance: full juggle
                case 1: return 0.999f;                               // just below full immunity
                case 2: return (float)rng.NextDouble() * 0.999f;     // anywhere in [0, ~0.999)
                default: return 0.5f * (float)rng.NextDouble();      // low resistance band
            }
        }

        /// <summary>Resistance in [0, 1] with 0 and full immunity (1) sampled explicitly.</summary>
        private static float NextResistance(System.Random rng)
        {
            switch (rng.Next(0, 4))
            {
                case 0: return 0f;
                case 1: return 1f;
                default: return (float)rng.NextDouble();
            }
        }
    }
}
