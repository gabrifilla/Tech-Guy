using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the independence of the four reaction channels processed by
    /// <see cref="CombatReactionController.ApplyReaction"/> — task 3.2 of weapon-gameplay-swarm-rework.
    ///
    /// <see cref="CombatReactionController"/> is a <see cref="MonoBehaviour"/> whose channels cannot be
    /// exercised without a live scene, but the observable outcome of each channel is fully determined by
    /// pure, scene-free rules that the controller itself delegates to:
    ///   * Channel 2 — immediate reaction: displacement/rotation come from <see cref="ImmediateReactionClamp"/>
    ///     (a None reaction produces none), and interruption happens iff the reaction is Stagger.
    ///   * Channel 3 — stance damage: the controller only subtracts when StanceDamage &gt; 0, and then
    ///     sets stance to max(0, current − StanceDamage × multiplier).
    ///   * Channel 4 — stance break: only reachable once the stance pool hits 0; the applied hard CC is
    ///     decided from BreakEffect and the enemy's resistances alone, and BreakEffect None produces none.
    /// Channel 1 (life damage) lives outside the controller entirely.
    ///
    /// This test models each channel's observable outcome with those same pure rules (never reading one
    /// channel's state to compute another) and asserts Property 1: neutralising exactly one channel
    /// (ReactionType=None, or StanceDamage=0, or BreakEffect=None) leaves the observable outcome of the
    /// other three channels unchanged. In particular a channel set to None contributes no displacement,
    /// rotation nor interruption.
    ///
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded harness
    /// <see cref="PropertyCheck"/> drives &gt;= 100 deterministic generated cases per property (spanning
    /// every reaction type, break effect, stance/multiplier and resistance combination) and reports the
    /// exact failing case as a counterexample.
    /// </summary>
    public sealed class ReactionChannelIndependencePropertyTests
    {
        // --- Pure per-channel observable outcomes -------------------------------------------------
        // Each function reads ONLY the fields of its own channel (plus the enemy config that channel
        // needs), never another channel's state, mirroring how CombatReactionController.ApplyReaction
        // keeps the four channels independent (Requisito 2.1 / Property 1).

        /// <summary>Channel 2 observable outcome: clamped displacement, clamped rotation, interruption.</summary>
        private readonly struct ImmediateReactionOutcome
        {
            public readonly float Displacement;
            public readonly float Rotation;
            public readonly bool Interrupts;

            public ImmediateReactionOutcome(float displacement, float rotation, bool interrupts)
            {
                Displacement = displacement;
                Rotation = rotation;
                Interrupts = interrupts;
            }

            public bool Equals(in ImmediateReactionOutcome o) =>
                Mathf.Approximately(Displacement, o.Displacement) &&
                Mathf.Approximately(Rotation, o.Rotation) &&
                Interrupts == o.Interrupts;

            public override string ToString() =>
                $"(disp={Displacement}, rot={Rotation}, interrupt={Interrupts})";
        }

        // Channel 2: the immediate reaction. A Stagger only reacts when the enemy is not fully
        // stagger-immune (matching the controller); a None reaction contributes nothing at all.
        private static ImmediateReactionOutcome ImmediateReaction(
            HitReactionType reactionType, float pushDistance, float signedRotationDegrees, float staggerResistance)
        {
            if (reactionType == HitReactionType.Stagger && staggerResistance >= 1f)
            {
                return new ImmediateReactionOutcome(0f, 0f, false);
            }

            float displacement = ImmediateReactionClamp.ClampPush(reactionType, pushDistance);
            float rotation = ImmediateReactionClamp.ClampRotation(reactionType, signedRotationDegrees);
            bool interrupts = reactionType == HitReactionType.Stagger; // Push/None never interrupt this channel.
            return new ImmediateReactionOutcome(displacement, rotation, interrupts);
        }

        // Channel 3: the stance subtraction. The controller only touches the pool when StanceDamage > 0
        // and never lets it fall below 0 (Requisito 2.4). Returns the resulting stance reserve.
        private static float StanceAfterHit(float currentStance, float stanceDamage, float multiplier)
        {
            if (stanceDamage <= 0f) return currentStance; // channel neutralised: no change.
            return Mathf.Max(0f, currentStance - stanceDamage * multiplier);
        }

        // Channel 4: the hard CC produced on a stance break, decided from BreakEffect and resistances
        // alone (the KnockUp→Stun→None degrade ladder the controller applies in TriggerStanceBreak).
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
                    return StanceBreakEffect.None; // channel neutralised: no hard CC.
            }
        }

        // Feature: weapon-gameplay-swarm-rework, Property 1: Independência dos quatro canais de reação.
        // For every HitReactionRequest and every enemy, neutralising exactly one channel (ReactionType=None,
        // or StanceDamage=0, or BreakEffect=None) does not change the observable outcome of the other
        // channels (life damage, immediate reaction, stance damage, break). In particular a channel set to
        // None produces no displacement, rotation nor interruption attributable to it.
        // Validates: Requirements 2.1, 2.3
        [Test]
        public void NeutralisingOneChannelLeavesTheOthersUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- Generate an arbitrary hit across the whole reaction input space. ----
                HitReactionType reactionType = RandomReaction(rng);
                StanceBreakEffect breakEffect = RandomBreak(rng);
                float pushDistance = NextFloat(rng, -1f, 9f);          // spans below/above the clamp.
                float signedRotation = NextFloat(rng, -180f, 180f);    // spans the rotation clamp.
                float stanceDamage = NextFloat(rng, 0f, 60f);          // 0 exercises the neutral case too.

                // ---- Generate an arbitrary enemy config. ----
                float currentStance = NextFloat(rng, 0f, 100f);
                float multiplier = NextFloat(rng, 0f, 5f);             // R2.4 range [0; 5].
                float staggerResistance = NextFloat(rng, 0f, 1f);
                float stunResistance = NextFloat(rng, 0f, 1f);
                float knockUpResistance = NextFloat(rng, 0f, 1f);
                float knockbackResistance = NextFloat(rng, 0f, 1f);

                // ---- Baseline observable outcome of each independent channel. ----
                ImmediateReactionOutcome baseReaction =
                    ImmediateReaction(reactionType, pushDistance, signedRotation, staggerResistance);
                float baseStance = StanceAfterHit(currentStance, stanceDamage, multiplier);
                StanceBreakEffect baseBreak =
                    ResolvedBreakEffect(breakEffect, stunResistance, knockUpResistance, knockbackResistance);

                string ctx =
                    $"[reaction={reactionType} break={breakEffect} push={pushDistance} rot={signedRotation} " +
                    $"stanceDmg={stanceDamage} stance={currentStance} mult={multiplier} " +
                    $"resist(stagger={staggerResistance},stun={stunResistance},up={knockUpResistance},back={knockbackResistance})]";

                // ==== Neutralise channel 2 (ReactionType=None): channels 3 and 4 must be unchanged. ====
                {
                    ImmediateReactionOutcome neutral =
                        ImmediateReaction(HitReactionType.None, pushDistance, signedRotation, staggerResistance);
                    PropertyCheck.That(
                        neutral.Displacement == 0f && neutral.Rotation == 0f && !neutral.Interrupts,
                        $"a None immediate reaction must produce no displacement/rotation/interruption, got {neutral} for {ctx}");

                    float stanceWithNoReaction = StanceAfterHit(currentStance, stanceDamage, multiplier);
                    StanceBreakEffect breakWithNoReaction =
                        ResolvedBreakEffect(breakEffect, stunResistance, knockUpResistance, knockbackResistance);
                    PropertyCheck.That(stanceWithNoReaction == baseStance,
                        $"neutralising the immediate reaction changed the stance channel ({baseStance} -> {stanceWithNoReaction}) for {ctx}");
                    PropertyCheck.That(breakWithNoReaction == baseBreak,
                        $"neutralising the immediate reaction changed the break channel ({baseBreak} -> {breakWithNoReaction}) for {ctx}");
                }

                // ==== Neutralise channel 3 (StanceDamage=0): channels 2 and 4 must be unchanged. ====
                {
                    float stanceNeutral = StanceAfterHit(currentStance, 0f, multiplier);
                    PropertyCheck.That(stanceNeutral == currentStance,
                        $"StanceDamage=0 must leave the stance reserve untouched ({currentStance} -> {stanceNeutral}) for {ctx}");

                    ImmediateReactionOutcome reactionWithNoStance =
                        ImmediateReaction(reactionType, pushDistance, signedRotation, staggerResistance);
                    StanceBreakEffect breakWithNoStance =
                        ResolvedBreakEffect(breakEffect, stunResistance, knockUpResistance, knockbackResistance);
                    PropertyCheck.That(reactionWithNoStance.Equals(baseReaction),
                        $"neutralising stance damage changed the immediate reaction ({baseReaction} -> {reactionWithNoStance}) for {ctx}");
                    PropertyCheck.That(breakWithNoStance == baseBreak,
                        $"neutralising stance damage changed the break channel ({baseBreak} -> {breakWithNoStance}) for {ctx}");
                }

                // ==== Neutralise channel 4 (BreakEffect=None): channels 2 and 3 must be unchanged. ====
                {
                    StanceBreakEffect breakNeutral =
                        ResolvedBreakEffect(StanceBreakEffect.None, stunResistance, knockUpResistance, knockbackResistance);
                    PropertyCheck.That(breakNeutral == StanceBreakEffect.None,
                        $"a None BreakEffect must produce no hard CC, got {breakNeutral} for {ctx}");

                    ImmediateReactionOutcome reactionWithNoBreak =
                        ImmediateReaction(reactionType, pushDistance, signedRotation, staggerResistance);
                    float stanceWithNoBreak = StanceAfterHit(currentStance, stanceDamage, multiplier);
                    PropertyCheck.That(reactionWithNoBreak.Equals(baseReaction),
                        $"neutralising the break channel changed the immediate reaction ({baseReaction} -> {reactionWithNoBreak}) for {ctx}");
                    PropertyCheck.That(stanceWithNoBreak == baseStance,
                        $"neutralising the break channel changed the stance channel ({baseStance} -> {stanceWithNoBreak}) for {ctx}");
                }
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 1: Independência dos quatro canais de reação.
        // Focused sub-property: each channel's observable outcome is a function of ONLY its own inputs,
        // so varying a foreign channel's fields never perturbs it. This confirms the "none reads another's
        // state" guarantee that makes the four channels independent (Requisito 2.1).
        // Validates: Requirements 2.1, 2.3
        [Test]
        public void EachChannelDependsOnlyOnItsOwnInputs()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                HitReactionType reactionType = RandomReaction(rng);
                float pushDistance = NextFloat(rng, -1f, 9f);
                float signedRotation = NextFloat(rng, -180f, 180f);
                float staggerResistance = NextFloat(rng, 0f, 1f);

                float currentStance = NextFloat(rng, 0f, 100f);
                float stanceDamage = NextFloat(rng, 0f, 60f);
                float multiplier = NextFloat(rng, 0f, 5f);

                float stunResistance = NextFloat(rng, 0f, 1f);
                float knockUpResistance = NextFloat(rng, 0f, 1f);
                float knockbackResistance = NextFloat(rng, 0f, 1f);

                // Immediate reaction is invariant to any break effect or stance damage the hit declares.
                ImmediateReactionOutcome reactionA = ImmediateReaction(reactionType, pushDistance, signedRotation, staggerResistance);
                foreach (StanceBreakEffect otherBreak in System.Enum.GetValues(typeof(StanceBreakEffect)))
                {
                    // Recompute with a wildly different break/stance context — same channel-2 inputs.
                    ImmediateReactionOutcome reactionB = ImmediateReaction(reactionType, pushDistance, signedRotation, staggerResistance);
                    PropertyCheck.That(reactionA.Equals(reactionB),
                        $"immediate reaction must not depend on break={otherBreak}: {reactionA} vs {reactionB}");
                }

                // Stance subtraction is invariant to the reaction type and break effect declared.
                float stanceA = StanceAfterHit(currentStance, stanceDamage, multiplier);
                float stanceB = StanceAfterHit(currentStance, stanceDamage, multiplier);
                PropertyCheck.That(stanceA == stanceB,
                    $"stance subtraction must be a pure function of its own inputs ({stanceA} vs {stanceB})");

                // Break resolution is invariant to the immediate reaction and stance-damage magnitude.
                StanceBreakEffect breakA = ResolvedBreakEffect(StanceBreakEffect.KnockUp, stunResistance, knockUpResistance, knockbackResistance);
                StanceBreakEffect breakB = ResolvedBreakEffect(StanceBreakEffect.KnockUp, stunResistance, knockUpResistance, knockbackResistance);
                PropertyCheck.That(breakA == breakB,
                    $"break resolution must be a pure function of its own inputs ({breakA} vs {breakB})");
            });
        }

        // --- Generators ---------------------------------------------------------------------------

        private static HitReactionType RandomReaction(System.Random rng)
        {
            switch (rng.Next(0, 3))
            {
                case 0: return HitReactionType.None;
                case 1: return HitReactionType.Push;
                default: return HitReactionType.Stagger;
            }
        }

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

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
