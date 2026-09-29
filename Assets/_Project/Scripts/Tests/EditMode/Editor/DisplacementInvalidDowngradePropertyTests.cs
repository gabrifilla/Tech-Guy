using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for invalid-displacement downgrade — task 2.3 of
    /// weapon-gameplay-swarm-rework.
    ///
    /// <see cref="DisplacementTier.Classify(in HitReactionRequest)"/> is the single pure, shared
    /// validator every weapon relies on (R1.4). When a hit's declared displacement is absent, zero,
    /// or out of every defined tier range (R4.6), the displacement must be rejected and the hit
    /// treated as <see cref="DisplacementTierKind.Micro"/>, an error indication must be surfaced, and
    /// the remaining hit channels (life damage, stance damage, break) must be left untouched.
    ///
    /// Since <c>Classify</c> is pure it never mutates its argument, so "the remaining effects of the
    /// hit are unchanged" is validated by snapshotting every non-displacement field of the
    /// <see cref="HitReactionRequest"/> and asserting the struct is byte-for-byte equal after
    /// classification.
    ///
    /// The project cannot resolve FsCheck/CsCheck packages on this machine, so the agreed seeded
    /// harness <see cref="PropertyCheck"/> drives >= 100 deterministic generated cases and reports
    /// the exact failing case as a counterexample.
    /// </summary>
    public sealed class DisplacementInvalidDowngradePropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 13: Declaração de deslocamento inválida rebaixa para Micro sem afetar outros efeitos.
        // Para todo HitReactionRequest cujos campos de deslocamento estão ausentes, iguais a 0 ou fora
        // das faixas definidas, o deslocamento é rejeitado e tratado como Micro, uma indicação de erro
        // é registrada, e os demais efeitos do acerto permanecem inalterados.
        // Validates: Requirements 4.6
        [Test]
        public void InvalidDisplacementIsDowngradedToMicroWithErrorAndOtherEffectsUnchanged()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // ---- generate an INVALID displacement declaration -------------------------------
                // Every branch below produces a declaration that is out of every defined tier range,
                // so DisplacementTier must reject it and downgrade to Micro (R4.6).
                float pushDistance;
                float knockUpHeight;
                float knockbackDistance;
                StanceBreakEffect breakEffect;
                string kind;
                MakeInvalidDisplacement(rng, out pushDistance, out knockUpHeight, out knockbackDistance, out breakEffect, out kind);

                // ---- generate the OTHER (non-displacement) hit effects, kept alongside ----------
                // These are the channels that must survive classification unchanged. They are chosen
                // independently of the displacement so the property covers any hit shape.
                var attacker = (Actor)null; // classification never dereferences the attacker
                var hitPoint = RandomVector(rng);
                var hitDirection = RandomVector(rng);
                var reactionType = (HitReactionType)rng.Next(0, 3);
                var strength = (HitStrength)rng.Next(0, 4);
                float stanceDamage = (float)rng.NextDouble() * 50f;
                float stunDuration = (float)rng.NextDouble() * 10f;

                var request = new HitReactionRequest(
                    attacker,
                    hitPoint,
                    hitDirection,
                    reactionType,
                    strength,
                    stanceDamage,
                    breakEffect,
                    pushDistance,
                    stunDuration,
                    knockUpHeight,
                    knockbackDistance);

                // Snapshot every field so we can prove none of the other channels moved.
                var before = request;

                // ---- classify --------------------------------------------------------------------
                DisplacementTier.Result result = DisplacementTier.Classify(in request);

                // ---- assertions ------------------------------------------------------------------
                // (1) The displacement is rejected and treated as Micro.
                PropertyCheck.That(result.Tier == DisplacementTierKind.Micro,
                    $"[{kind}] push={pushDistance} up={knockUpHeight} back={knockbackDistance} break={breakEffect}: " +
                    $"invalid declaration classified as {result.Tier}, expected Micro");

                PropertyCheck.That(!result.IsValid,
                    $"[{kind}] push={pushDistance} up={knockUpHeight} back={knockbackDistance} break={breakEffect}: " +
                    "invalid declaration was reported as valid");

                // (2) An error indication is registered.
                PropertyCheck.That(!string.IsNullOrEmpty(result.Error),
                    $"[{kind}] push={pushDistance} up={knockUpHeight} back={knockbackDistance} break={breakEffect}: " +
                    "no error indication was surfaced for an invalid declaration");

                // (3) The remaining effects of the hit are unchanged. Classify is pure, so the whole
                //     request (including the non-displacement channels) must be identical afterwards.
                PropertyCheck.That(RequestsEqual(before, request),
                    $"[{kind}] classification mutated the HitReactionRequest: " +
                    $"reaction {before.ReactionType}->{request.ReactionType}, strength {before.Strength}->{request.Strength}, " +
                    $"stance {before.StanceDamage}->{request.StanceDamage}, break {before.BreakEffect}->{request.BreakEffect}, " +
                    $"stun {before.StunDuration}->{request.StunDuration}");

                // The non-displacement channels also carry through into the result's meaning: the
                // downgrade only touches the displacement tier, never the break effect the hit
                // requested — verify the break channel is still exactly what the hit declared.
                PropertyCheck.That(request.BreakEffect == breakEffect,
                    $"[{kind}] break effect changed from {breakEffect} to {request.BreakEffect} during classification");
                PropertyCheck.That(request.StanceDamage == before.StanceDamage,
                    $"[{kind}] stance damage changed during classification");

                // ---- cross-check the field-level overload agrees --------------------------------
                // Weapons that build displacement from raw values before a request exists use the
                // field overload; it must reach the same downgrade decision for the same inputs.
                DisplacementTier.Result fieldResult =
                    DisplacementTier.Classify(request.PushDistance, request.KnockUpHeight, request.KnockbackDistance, request.BreakEffect);
                PropertyCheck.That(fieldResult.Tier == DisplacementTierKind.Micro && !fieldResult.IsValid,
                    $"[{kind}] field overload disagreed with the request overload: tier={fieldResult.Tier} valid={fieldResult.IsValid}");
                PropertyCheck.That(!string.IsNullOrEmpty(fieldResult.Error),
                    $"[{kind}] field overload produced no error indication for an invalid declaration");
            });
        }

        // ---- generators ---------------------------------------------------------------------------

        /// <summary>
        /// Produces a displacement declaration guaranteed to be invalid (rejected by
        /// <see cref="DisplacementTier"/>). Covers the distinct ways a declaration can be invalid:
        /// PushDistance above the Push ceiling, PushDistance in the (0.3, 0.31) gap, launch fields set
        /// without a matching BreakEffect, and knockback / knock-up values above or below the Launch
        /// range even with a matching BreakEffect. Note the <see cref="HitReactionRequest"/>
        /// constructor clamps negatives to 0, so every generated value below is already non-negative
        /// and lands strictly outside every defined tier range.
        /// </summary>
        private static void MakeInvalidDisplacement(
            System.Random rng,
            out float pushDistance,
            out float knockUpHeight,
            out float knockbackDistance,
            out StanceBreakEffect breakEffect,
            out string kind)
        {
            switch (rng.Next(0, 7))
            {
                case 0:
                    // PushDistance strictly above the Push ceiling (2.0), no launch.
                    pushDistance = DisplacementTier.PushDistanceMax + 0.01f + (float)rng.NextDouble() * 100f;
                    knockUpHeight = 0f;
                    knockbackDistance = 0f;
                    breakEffect = StanceBreakEffect.None;
                    kind = "push-over-ceiling";
                    return;

                case 1:
                    // Launch fields set but WITHOUT a matching BreakEffect => mixed/invalid.
                    pushDistance = 0f;
                    knockUpHeight = 0f;
                    knockbackDistance = DisplacementTier.KnockbackMin + (float)rng.NextDouble() * 3f;
                    breakEffect = StanceBreakEffect.None; // no matching Knockback break
                    kind = "knockback-without-break";
                    return;

                case 2:
                    // KnockUp field set but WITHOUT a matching KnockUp break => invalid.
                    pushDistance = 0f;
                    knockUpHeight = DisplacementTier.KnockUpMin + (float)rng.NextDouble() * 3f;
                    knockbackDistance = 0f;
                    breakEffect = StanceBreakEffect.Stun; // wrong break for a knock-up
                    kind = "knockup-without-matching-break";
                    return;

                case 3:
                    // Declares Knockback break but the distance is OUT OF the Launch range (too high).
                    pushDistance = 0f;
                    knockUpHeight = 0f;
                    knockbackDistance = DisplacementTier.KnockbackMax + 0.01f + (float)rng.NextDouble() * 50f;
                    breakEffect = StanceBreakEffect.Knockback;
                    kind = "knockback-over-range";
                    return;

                case 4:
                    // Declares KnockUp break but the height is OUT OF the Launch range (too high).
                    pushDistance = 0f;
                    knockUpHeight = DisplacementTier.KnockUpMax + 0.01f + (float)rng.NextDouble() * 50f;
                    knockbackDistance = 0f;
                    breakEffect = StanceBreakEffect.KnockUp;
                    kind = "knockup-over-range";
                    return;

                case 5:
                    // Knockback break declared but the distance is BELOW the Launch floor (in the
                    // (0, 2.01) gap) — a rejected launch declaration.
                    pushDistance = 0f;
                    knockUpHeight = 0f;
                    knockbackDistance = 0.01f + (float)rng.NextDouble() * (DisplacementTier.KnockbackMin - 0.02f);
                    breakEffect = StanceBreakEffect.Knockback;
                    kind = "knockback-under-range";
                    return;

                default:
                    // PushDistance in the invalid gap: strictly between the Micro ceiling (0.3) and the
                    // Push floor (0.31), with no launch. This is out of every defined range.
                    pushDistance = DisplacementTier.MicroPushMax + 0.001f
                        + (float)rng.NextDouble() * (DisplacementTier.PushDistanceMin - DisplacementTier.MicroPushMax - 0.002f);
                    knockUpHeight = 0f;
                    knockbackDistance = 0f;
                    breakEffect = StanceBreakEffect.None;
                    kind = "push-in-gap";
                    return;
            }
        }

        private static Vector3 RandomVector(System.Random rng)
        {
            return new Vector3(
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 20f,
                ((float)rng.NextDouble() - 0.5f) * 20f);
        }

        /// <summary>
        /// Structural equality over every <see cref="HitReactionRequest"/> field. Used to prove the
        /// classification left the whole request — displacement and non-displacement channels alike —
        /// untouched (a pure validator must not mutate its input).
        /// </summary>
        private static bool RequestsEqual(in HitReactionRequest a, in HitReactionRequest b)
        {
            return ReferenceEquals(a.Attacker, b.Attacker)
                && a.HitPoint == b.HitPoint
                && a.HitDirection == b.HitDirection
                && a.ReactionType == b.ReactionType
                && a.PushDistance == b.PushDistance
                && a.Strength == b.Strength
                && a.StanceDamage == b.StanceDamage
                && a.BreakEffect == b.BreakEffect
                && a.StunDuration == b.StunDuration
                && a.KnockUpHeight == b.KnockUpHeight
                && a.KnockbackDistance == b.KnockbackDistance;
        }
    }
}
