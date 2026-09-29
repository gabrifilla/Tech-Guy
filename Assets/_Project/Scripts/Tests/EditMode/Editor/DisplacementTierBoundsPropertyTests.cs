using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the per-tier displacement bounds enforced by the pure, shared
    /// validator <see cref="DisplacementTier.Classify(in HitReactionRequest)"/> — task 2.2 of
    /// weapon-gameplay-swarm-rework.
    ///
    /// <see cref="DisplacementTier"/> is a static, scene-free classifier, so its ranges can be
    /// property-checked without a live Unity scene. This project cannot resolve FsCheck/CsCheck
    /// packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives
    /// >= 100 deterministic generated cases per property (generating requests across the whole
    /// Micro/Push/Launch input space) and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class DisplacementTierBoundsPropertyTests
    {
        // Feature: weapon-gameplay-swarm-rework, Property 12: Limites por nível de deslocamento.
        // For every HitReactionRequest classified into a displacement tier, the corresponding fields
        // respect that tier's range: Micro with PushDistance in [0; 0.3] and KnockUpHeight =
        // KnockbackDistance = 0; Push with PushDistance in (0.3; 2.0] and no hard CC (control
        // preserved, BreakEffect None); Launch with KnockbackDistance in (2.0; 8.0] or KnockUpHeight
        // in [0.5; 4.0] (control lost, a matching BreakEffect declared).
        // Validates: Requirements 4.1, 4.2, 4.3
        [Test]
        public void ClassifiedTierFieldsRespectTheirRange()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Pick one of the three tiers to generate a valid request for, so every generated
                // case is a genuinely classifiable displacement (the invalid space is Property 13).
                int tier = rng.Next(0, 3);

                float push;
                float knockUp;
                float knockback;
                StanceBreakEffect breakEffect;
                DisplacementTierKind expected;

                switch (tier)
                {
                    case 0: // Micro: PushDistance in [0; 0.3], no launch fields, no break.
                        push = NextFloat(rng, DisplacementTier.MicroPushMin, DisplacementTier.MicroPushMax);
                        knockUp = 0f;
                        knockback = 0f;
                        breakEffect = StanceBreakEffect.None;
                        expected = DisplacementTierKind.Micro;
                        break;

                    case 1: // Push: PushDistance in [0.31; 2.0], no launch fields, no break.
                        push = NextFloat(rng, DisplacementTier.PushDistanceMin, DisplacementTier.PushDistanceMax);
                        knockUp = 0f;
                        knockback = 0f;
                        breakEffect = StanceBreakEffect.None;
                        expected = DisplacementTierKind.Push;
                        break;

                    default: // Launch: either a Knockback axis or a KnockUp axis (never both).
                        expected = DisplacementTierKind.Launch;
                        if (rng.Next(0, 2) == 0)
                        {
                            // Long knockback: KnockbackDistance in [2.01; 8.0], KnockUp clean.
                            push = 0f;
                            knockUp = 0f;
                            knockback = NextFloat(rng, DisplacementTier.KnockbackMin, DisplacementTier.KnockbackMax);
                            breakEffect = StanceBreakEffect.Knockback;
                        }
                        else
                        {
                            // Vertical knock-up: KnockUpHeight in [0.5; 4.0], Knockback clean.
                            push = 0f;
                            knockUp = NextFloat(rng, DisplacementTier.KnockUpMin, DisplacementTier.KnockUpMax);
                            knockback = 0f;
                            breakEffect = StanceBreakEffect.KnockUp;
                        }
                        break;
                }

                // Build the request through its public constructor exactly as a weapon would, so the
                // classifier sees the same fields it will see in play. The constructor clamps negatives
                // to 0, matching the tier field semantics.
                var request = new HitReactionRequest(
                    attacker: null,
                    hitPoint: Vector3.zero,
                    hitDirection: Vector3.forward,
                    reactionType: HitReactionType.Push,
                    strength: HitStrength.Light,
                    stanceDamage: 0f,
                    breakEffect: breakEffect,
                    pushDistance: push,
                    stunDuration: 0f,
                    knockUpHeight: knockUp,
                    knockbackDistance: knockback);

                DisplacementTier.Result result = DisplacementTier.Classify(request);

                string state =
                    $"[tier={expected} push={request.PushDistance} knockUp={request.KnockUpHeight} " +
                    $"knockback={request.KnockbackDistance} break={request.BreakEffect}]";

                // The declared displacement is valid and classified into the intended tier.
                PropertyCheck.That(result.IsValid,
                    $"a valid {expected} declaration must classify as valid, got invalid ({result.Error}) for {state}");
                PropertyCheck.That(result.Tier == expected,
                    $"expected tier {expected} but got {result.Tier} for {state}");

                // The fields respect that tier's range (Property 12 core assertion).
                switch (expected)
                {
                    case DisplacementTierKind.Micro:
                        // R4.1: PushDistance in [0; 0.3], KnockUpHeight = KnockbackDistance = 0.
                        PropertyCheck.That(
                            request.PushDistance >= DisplacementTier.MicroPushMin &&
                            request.PushDistance <= DisplacementTier.MicroPushMax,
                            $"Micro PushDistance out of [{DisplacementTier.MicroPushMin}; {DisplacementTier.MicroPushMax}] for {state}");
                        PropertyCheck.That(request.KnockUpHeight == 0f && request.KnockbackDistance == 0f,
                            $"Micro must have KnockUpHeight = KnockbackDistance = 0 for {state}");
                        break;

                    case DisplacementTierKind.Push:
                        // R4.2: PushDistance in (0.3; 2.0], no hard CC (control preserved).
                        PropertyCheck.That(
                            request.PushDistance > DisplacementTier.MicroPushMax &&
                            request.PushDistance <= DisplacementTier.PushDistanceMax,
                            $"Push PushDistance out of ({DisplacementTier.MicroPushMax}; {DisplacementTier.PushDistanceMax}] for {state}");
                        PropertyCheck.That(request.KnockUpHeight == 0f && request.KnockbackDistance == 0f,
                            $"Push must have KnockUpHeight = KnockbackDistance = 0 for {state}");
                        PropertyCheck.That(request.BreakEffect == StanceBreakEffect.None,
                            $"Push must not declare hard CC (BreakEffect must be None) for {state}");
                        break;

                    default: // Launch
                        // R4.3: KnockbackDistance in (2.0; 8.0] OR KnockUpHeight in [0.5; 4.0],
                        // with the matching break effect declared (control lost). Exactly one axis.
                        bool knockbackAxis =
                            request.BreakEffect == StanceBreakEffect.Knockback &&
                            request.KnockbackDistance > DisplacementTier.PushDistanceMax &&
                            request.KnockbackDistance >= DisplacementTier.KnockbackMin &&
                            request.KnockbackDistance <= DisplacementTier.KnockbackMax &&
                            request.KnockUpHeight == 0f;
                        bool knockUpAxis =
                            request.BreakEffect == StanceBreakEffect.KnockUp &&
                            request.KnockUpHeight >= DisplacementTier.KnockUpMin &&
                            request.KnockUpHeight <= DisplacementTier.KnockUpMax &&
                            request.KnockbackDistance == 0f;
                        PropertyCheck.That(knockbackAxis ^ knockUpAxis,
                            $"Launch must declare exactly one in-range axis (Knockback in ({DisplacementTier.PushDistanceMax}; " +
                            $"{DisplacementTier.KnockbackMax}] or KnockUp in [{DisplacementTier.KnockUpMin}; {DisplacementTier.KnockUpMax}]) " +
                            $"with a matching BreakEffect for {state}");
                        break;
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
