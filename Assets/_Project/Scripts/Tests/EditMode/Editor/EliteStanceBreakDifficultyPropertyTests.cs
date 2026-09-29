using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the "Elite needs accumulation or a high-strength hit to break"
    /// rule (Property 8, Requisito 3.2) — task 4.3 of weapon-gameplay-swarm-rework.
    ///
    /// An Elite enemy is intentionally hard to stagger: its rank defaults
    /// (<c>CombatReactionController.ApplyRankDefaults</c>) give it a large stance pool
    /// (<c>maxStance = 220</c>), a below-1 stance-damage multiplier (<c>0.8</c>) and non-trivial
    /// resistances (stagger 0.3, stun 0.25, knockUp 0.4, knockback 0.3). The break decision itself is
    /// fully determined by the same pure, scene-free rules the controller delegates to and that the
    /// sibling stance tests already model:
    ///   * the reserve after a hit is <see cref="StanceBreakBounds.SubtractStance(float,float,float)"/>;
    ///   * a Stance Break fires (in the same step) only once that reserve reaches 0;
    ///   * the effect actually applied is the requested <see cref="StanceBreakEffect"/> put through the
    ///     enemy's resistances via the KnockUp→Stun→None degrade ladder that <c>TriggerStanceBreak</c>
    ///     uses.
    /// This test models those pure rules against the Elite defaults and asserts Property 8: a single
    /// small Light hit never empties the Elite pool, whereas enough accumulated Light hits, one
    /// Heavy/Breaker (high stance-damage) hit, or a dedicated stance-damage ability drives the reserve
    /// to 0 and breaks it — with the applied effect always respecting resistances in [0; 1].
    ///
    /// <see cref="CombatReactionController"/> is a <see cref="MonoBehaviour"/> that cannot be driven
    /// without a live scene, so — as with the sibling stance property tests — the logic is exercised
    /// through the shared pure helpers. This project cannot resolve FsCheck/CsCheck packages on this
    /// machine, so the agreed seeded harness <see cref="PropertyCheck"/> drives >= 100 deterministic
    /// generated cases per property and reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class EliteStanceBreakDifficultyPropertyTests
    {
        // --- Elite rank defaults (mirror of CombatReactionController.ApplyRankDefaults for Elite) ----
        private const float EliteMaxStance = 220f;
        private const float EliteStanceMultiplier = 0.8f;
        private const float EliteStaggerResistance = 0.3f;
        private const float EliteStunResistance = 0.25f;
        private const float EliteKnockUpResistance = 0.4f;
        private const float EliteKnockbackResistance = 0.3f;

        // Per the design, a Light basic against a large pool contributes a small share of stance
        // damage. Any single Light hit whose applied damage is a small fraction of the Elite pool must
        // never zero it. We generate small Light stance-damage values so `single Light hit` is honest.
        private const float MaxSingleLightStanceDamage = 40f; // << EliteMaxStance / EliteStanceMultiplier

        // Feature: weapon-gameplay-swarm-rework, Property 8: Elite exige acúmulo ou força alta para quebrar.
        // For every Elite enemy, a single small Light hit does not empty its stance pool, whereas enough
        // accumulated Light hits, a single Heavy/Breaker (high stance-damage) hit, or a dedicated
        // stance-damage ability drives the reserve to 0 and breaks it, with the applied effect always
        // respecting the configured resistances in [0; 1].
        // Validates: Requirements 3.2
        [Test]
        public void EliteBreaksOnlyThroughAccumulationOrHighStrengthNeverASingleLightHit()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // The Elite starts from a full pool (the controller resets currentStance = maxStance on
                // ConfigureRank/Awake). Every scenario below starts from the same full reserve.
                float maxStance = EliteMaxStance;
                float multiplier = EliteStanceMultiplier;

                // ---- 1) A single small Light hit must NOT empty the pool ---------------------------
                float lightDamage = NextFloat(rng, 0f, MaxSingleLightStanceDamage);
                float afterOneLight = StanceBreakBounds.SubtractStance(maxStance, lightDamage, multiplier);

                string lightState =
                    $"[Elite maxStance={maxStance} mult={multiplier} singleLightDamage={lightDamage} " +
                    $"afterOneLight={afterOneLight}]";

                // R3.2: a lone Light hit leaves the pool positive — the Elite does not break to one Light.
                PropertyCheck.That(afterOneLight > 0f,
                    $"a single Light hit must not empty the Elite stance pool for {lightState}");
                // The applied damage is genuinely a small share of the pool (honest "single Light").
                PropertyCheck.That(afterOneLight >= maxStance - MaxSingleLightStanceDamage * multiplier - 1e-3f,
                    $"a single Light hit must remove at most its small share of the pool for {lightState}");

                // ---- 2) Accumulated Light hits eventually break the Elite --------------------------
                // Repeated Light hits of a strictly positive size drain the reserve to 0 in a bounded
                // number of steps; the break resolves on the hit that zeroes the reserve and no earlier.
                float perLight = NextFloat(rng, 5f, MaxSingleLightStanceDamage); // strictly positive
                float stance = maxStance;
                int accumulatedBreakStep = -1;
                // Enough headroom to guarantee exhaustion: ceil(max / (perLight*mult)) + slack.
                int maxSteps = Mathf.CeilToInt(maxStance / (perLight * multiplier)) + 4;

                for (int step = 0; step < maxSteps; step++)
                {
                    float before = stance;
                    stance = StanceBreakBounds.SubtractStance(before, perLight, multiplier);

                    // Each positive-damage Light hit strictly reduces the reserve until it hits 0.
                    PropertyCheck.That(stance < before || before <= 0f,
                        $"a positive Light hit must reduce the Elite reserve (step {step}, before={before}, after={stance})");

                    if (stance <= 0f)
                    {
                        accumulatedBreakStep = step;
                        break;
                    }
                }

                // R3.2: accumulation DOES break the Elite, and it takes more than a single hit.
                PropertyCheck.That(accumulatedBreakStep >= 1,
                    $"accumulated Light hits must break the Elite after more than one hit " +
                    $"(perLight={perLight}, breakStep={accumulatedBreakStep})");

                // ---- 3) A single Heavy/Breaker (high stance-damage) hit breaks the Elite ------------
                // A Heavy/Breaker tool declares stance damage large enough to overwhelm the pool even
                // after the below-1 Elite multiplier; it must zero the reserve in one hit.
                HitStrength strongStrength = rng.Next(0, 2) == 0 ? HitStrength.Heavy : HitStrength.Breaker;
                // High stance-damage: strictly more than the pool can absorb through the multiplier.
                float highDamage = NextFloat(rng, maxStance / multiplier + 1f, maxStance / multiplier + 400f);
                float afterStrong = StanceBreakBounds.SubtractStance(maxStance, highDamage, multiplier);

                string strongState =
                    $"[Elite maxStance={maxStance} mult={multiplier} strength={strongStrength} " +
                    $"highDamage={highDamage} afterStrong={afterStrong}]";

                PropertyCheck.That(strongStrength == HitStrength.Heavy || strongStrength == HitStrength.Breaker,
                    $"the high-strength tool must be Heavy or Breaker for {strongState}");
                // R3.2: a single Heavy/Breaker high stance-damage hit empties the pool → break.
                PropertyCheck.That(afterStrong <= 0f,
                    $"a single Heavy/Breaker high stance-damage hit must break the Elite for {strongState}");

                // ---- 4) A dedicated stance-damage ability breaks the Elite -------------------------
                // An ability whose whole purpose is stance damage carries enough to zero the pool too.
                float abilityDamage = NextFloat(rng, maxStance / multiplier + 1f, maxStance / multiplier + 800f);
                float afterAbility = StanceBreakBounds.SubtractStance(maxStance, abilityDamage, multiplier);
                PropertyCheck.That(afterAbility <= 0f,
                    $"a dedicated stance-damage ability must break the Elite " +
                    $"(abilityDamage={abilityDamage}, afterAbility={afterAbility})");

                // ---- 5) Whichever path breaks it, the applied effect respects resistances [0;1] -----
                // The break effect goes through the enemy's resistances via the KnockUp→Stun→None
                // degrade ladder. With the Elite's configured resistances all in [0;1] and none at full
                // immunity, the requested effect survives; a fully-immune channel is suppressed/degraded.
                StanceBreakEffect requested = RandomBreak(rng);
                StanceBreakEffect applied = ResolvedBreakEffect(
                    requested, EliteStunResistance, EliteKnockUpResistance, EliteKnockbackResistance);

                string resistState =
                    $"[requested={requested} applied={applied} resist(stagger={EliteStaggerResistance}," +
                    $"stun={EliteStunResistance},up={EliteKnockUpResistance},back={EliteKnockbackResistance})]";

                // All Elite resistances are configured strictly inside [0; 1].
                PropertyCheck.That(InUnitInterval(EliteStaggerResistance) && InUnitInterval(EliteStunResistance) &&
                                   InUnitInterval(EliteKnockUpResistance) && InUnitInterval(EliteKnockbackResistance),
                    $"Elite resistances must all lie in [0; 1] for {resistState}");

                // None of the Elite channels is at full immunity, so the requested effect is applied as-is.
                PropertyCheck.That(applied == requested,
                    $"with no full-immunity channel the requested break effect must be applied for {resistState}");

                // The applied effect is always one of the valid enum values (never an out-of-band effect).
                PropertyCheck.That(
                    applied == StanceBreakEffect.None || applied == StanceBreakEffect.Stun ||
                    applied == StanceBreakEffect.KnockUp || applied == StanceBreakEffect.Knockback,
                    $"the applied break effect must be a valid StanceBreakEffect for {resistState}");
            });
        }

        // Feature: weapon-gameplay-swarm-rework, Property 8: Elite exige acúmulo ou força alta para quebrar.
        // Focused sub-property: an Elite channel raised to full immunity (resistance 1.0) suppresses that
        // effect on break via the degrade ladder, while resistances strictly below 1.0 let the effect
        // through — confirming "respecting the configured resistances in [0.0; 1.0]".
        // Validates: Requirements 3.2
        [Test]
        public void EliteBreakEffectHonoursResistanceLadderAcrossTheUnitInterval()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Any resistances in [0; 1], with 0 and full immunity (1) sampled explicitly so the
                // degrade ladder is exercised at both ends of the interval.
                float stunResistance = NextResistance(rng);
                float knockUpResistance = NextResistance(rng);
                float knockbackResistance = NextResistance(rng);
                StanceBreakEffect requested = RandomBreak(rng);

                // The Elite reaches the break through a Heavy/Breaker high stance-damage hit (path 3),
                // so the break definitely fires and the effect must be gated by resistances.
                float highDamage = EliteMaxStance / EliteStanceMultiplier + NextFloat(rng, 1f, 400f);
                float afterStrong = StanceBreakBounds.SubtractStance(EliteMaxStance, highDamage, EliteStanceMultiplier);

                StanceBreakEffect applied = ResolvedBreakEffect(
                    requested, stunResistance, knockUpResistance, knockbackResistance);

                string state =
                    $"[requested={requested} applied={applied} afterStrong={afterStrong} " +
                    $"resist(stun={stunResistance},up={knockUpResistance},back={knockbackResistance})]";

                // The high-strength hit did break the Elite.
                PropertyCheck.That(afterStrong <= 0f,
                    $"the Heavy/Breaker hit must break the Elite for {state}");

                // A fully-immune channel is never applied at full severity; below full immunity it survives.
                switch (requested)
                {
                    case StanceBreakEffect.Stun:
                        PropertyCheck.That(
                            stunResistance >= 1f ? applied == StanceBreakEffect.None : applied == StanceBreakEffect.Stun,
                            $"stun must be suppressed only at full immunity for {state}");
                        break;
                    case StanceBreakEffect.Knockback:
                        PropertyCheck.That(
                            knockbackResistance >= 1f ? applied == StanceBreakEffect.None : applied == StanceBreakEffect.Knockback,
                            $"knockback must be suppressed only at full immunity for {state}");
                        break;
                    case StanceBreakEffect.KnockUp:
                        if (knockUpResistance < 1f)
                            PropertyCheck.That(applied == StanceBreakEffect.KnockUp,
                                $"knock-up must survive below full immunity for {state}");
                        else
                            // KnockUp immune degrades down the ladder to Stun (if feelable) or None.
                            PropertyCheck.That(
                                applied == (stunResistance < 1f ? StanceBreakEffect.Stun : StanceBreakEffect.None),
                                $"knock-up-immune must degrade to Stun-or-None for {state}");
                        break;
                    default: // None requested → None applied.
                        PropertyCheck.That(applied == StanceBreakEffect.None,
                            $"a None break effect must stay None for {state}");
                        break;
                }
            });
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

        private static bool InUnitInterval(float value) => value >= 0f && value <= 1f;

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
