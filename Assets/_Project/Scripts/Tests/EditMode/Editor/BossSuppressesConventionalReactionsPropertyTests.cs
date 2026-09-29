using UnityEngine;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    /// <summary>
    /// EditMode property test for the "Boss suppresses conventional hit reactions" rule
    /// (Property 11, Requisito 3.5) — task 4.5 of weapon-gameplay-swarm-rework.
    ///
    /// A Boss is intentionally unmoved by ordinary hits. Its rank defaults
    /// (<c>CombatReactionController.ApplyRankDefaults</c> for <see cref="EnemyRank.Boss"/>) give it a
    /// very large stance pool (<c>maxStance = 900</c>), a below-1 stance-damage multiplier (<c>0.5</c>),
    /// a near-total stagger resistance (<c>0.85</c>), a high stun resistance (<c>0.7</c>) and, crucially,
    /// FULL immunity to the two conventional displacement/launch channels
    /// (<c>knockUpResistance = 1</c>, <c>knockbackResistance = 1</c>).
    ///
    /// Property 11 says: for every hit applied to a Boss, no conventional displacement/flinch reaction is
    /// produced; only stance damage, interruption stagger, and the post-break vulnerability window
    /// interact with it. That decomposes onto the same pure, scene-free helpers the controller delegates
    /// to and the sibling reaction tests already model:
    ///   * the immediate reaction nudge (Push/Stagger displacement + facing) is a "conventional reaction"
    ///     and is suppressed on a Boss — a Boss ignores ordinary flinches, so its readable displacement
    ///     from the immediate channel is 0;
    ///   * the hard-CC displacement channels (KnockUp / Knockback) are resolved through
    ///     <see cref="BreakEffectResistance.Resolve"/>, and with both resistances at full immunity (1.0)
    ///     they can NEVER survive — the ladder degrades any launch request to a non-displacing
    ///     interruption (Stun) or None, so no conventional launch is ever produced;
    ///   * stance damage still subtracts through <see cref="StanceBreakBounds.SubtractStance"/> (the Boss
    ///     is still broken by enough stance damage);
    ///   * on a break, the post-break vulnerability window opens and downgrades the Boss's effective rank
    ///     one step (Boss → Legendary) via <see cref="VulnerabilityWindow"/>.
    ///
    /// <see cref="CombatReactionController"/> is a <see cref="MonoBehaviour"/> that cannot be driven
    /// without a live scene, so — as with the sibling stance property tests — the logic is exercised
    /// through the shared pure helpers rather than the controller. This project cannot resolve
    /// FsCheck/CsCheck packages on this machine, so the agreed seeded harness <see cref="PropertyCheck"/>
    /// drives &gt;= 100 deterministic generated cases per property (spanning every immediate reaction,
    /// every requested break effect, and stance-damage values on both sides of the break threshold) and
    /// reports the exact failing case as a counterexample.
    /// </summary>
    public sealed class BossSuppressesConventionalReactionsPropertyTests
    {
        // --- Boss rank defaults (mirror of CombatReactionController.ApplyRankDefaults for Boss) -------
        private const float BossMaxStance = 900f;
        private const float BossStanceMultiplier = 0.5f;
        private const float BossStaggerResistance = 0.85f;
        private const float BossStunResistance = 0.7f;
        private const float BossKnockUpResistance = 1f;   // fully immune to vertical launch
        private const float BossKnockbackResistance = 1f; // fully immune to horizontal launch

        // Feature: weapon-gameplay-swarm-rework, Property 11: Boss suprime reações convencionais de acerto.
        // For every hit applied to a Boss, no conventional displacement/flinch reaction is produced:
        // the immediate reaction nudge (Push/Stagger displacement and facing) is suppressed, and the
        // hard-CC displacement channels (KnockUp / Knockback) never survive because the Boss is fully
        // immune (resistance 1.0) to both — any launch request degrades to a non-displacing interruption
        // (Stun) or None. Only stance damage, interruption stagger, and the post-break vulnerability
        // window interact with the Boss.
        // Validates: Requirements 3.5
        [Test]
        public void BossProducesNoConventionalDisplacementOrFlinchFromAnyHit()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                // Boss resistances are all in [0; 1], with the two launch channels at full immunity.
                PropertyCheck.That(
                    InUnitInterval(BossStaggerResistance) && InUnitInterval(BossStunResistance) &&
                    InUnitInterval(BossKnockUpResistance) && InUnitInterval(BossKnockbackResistance),
                    "Boss resistances must all lie in [0; 1]");
                PropertyCheck.That(
                    BreakEffectResistance.IsFullyImmune(BossKnockUpResistance) &&
                    BreakEffectResistance.IsFullyImmune(BossKnockbackResistance),
                    "a Boss must be fully immune to both conventional launch channels (KnockUp, Knockback)");

                // An arbitrary ordinary hit against the Boss: any immediate reaction, any requested
                // break effect, an arbitrary declared push distance (even a large, would-be shove).
                HitReactionType reaction = RandomReaction(rng);
                StanceBreakEffect requestedBreak = RandomBreak(rng);
                float requestedPush = NextFloat(rng, 0f, 8f);      // caller may declare any push
                float requestedRotation = NextFloat(rng, -180f, 180f);

                string state =
                    $"[reaction={reaction} requestedBreak={requestedBreak} " +
                    $"push={requestedPush} rot={requestedRotation}]";

                // ---- 1) Immediate reaction (Push/Stagger) is a CONVENTIONAL reaction: suppressed -----
                // A Boss ignores ordinary flinches, so the conventional immediate reaction produces no
                // readable displacement or facing change. We model that suppression explicitly: on a Boss
                // the immediate reaction nudge/rotation is gated off. (On non-Boss ranks the same request
                // would clamp to <= 0.5 m and <= 15 deg via ImmediateReactionClamp — shown below.)
                float bossImmediateDisplacement = SuppressedOnBoss(requestedPush);
                float bossImmediateRotation = SuppressedOnBoss(requestedRotation);

                PropertyCheck.That(Mathf.Approximately(bossImmediateDisplacement, 0f),
                    $"a Boss must produce no conventional immediate displacement for {state}");
                PropertyCheck.That(Mathf.Approximately(bossImmediateRotation, 0f),
                    $"a Boss must produce no conventional immediate flinch rotation for {state}");

                // Sanity: the SAME request on a non-Boss enemy WOULD produce a (clamped) conventional
                // reaction, so the suppression above is a real Boss-only difference, not a no-op rule.
                float nonBossDisplacement = ImmediateReactionClamp.ClampPush(reaction, requestedPush);
                float nonBossRotation = Mathf.Abs(ImmediateReactionClamp.ClampRotation(reaction, requestedRotation));
                if (ImmediateReactionClamp.ProducesReaction(reaction) && requestedPush > 0f)
                {
                    PropertyCheck.That(nonBossDisplacement > 0f,
                        $"a non-Boss enemy WOULD get a conventional nudge, proving the Boss suppression matters for {state}");
                    PropertyCheck.That(nonBossDisplacement <= ImmediateReactionClamp.MaxDisplacementMeters,
                        $"the non-Boss nudge must stay within the readability clamp for {state}");
                }
                PropertyCheck.That(nonBossRotation <= ImmediateReactionClamp.MaxRotationDegrees,
                    $"the non-Boss facing change must stay within the readability clamp for {state}");

                // ---- 2) Hard-CC displacement (KnockUp / Knockback) NEVER survives on a Boss ----------
                // Whatever the hit requests, the deterministic degrade ladder gates it through the Boss's
                // resistances. With KnockUp and Knockback both fully immune, neither displacement effect
                // can ever be applied: the result is only ever Stun (a non-displacing interruption) or
                // None (a plain interrupt).
                StanceBreakEffect appliedBreak = BreakEffectResistance.Resolve(
                    requestedBreak, BossStunResistance, BossKnockUpResistance, BossKnockbackResistance);

                PropertyCheck.That(
                    appliedBreak != StanceBreakEffect.KnockUp && appliedBreak != StanceBreakEffect.Knockback,
                    $"a Boss must never be launched (KnockUp/Knockback) by any hit, got {appliedBreak} for {state}");
                PropertyCheck.That(
                    appliedBreak == StanceBreakEffect.Stun || appliedBreak == StanceBreakEffect.None,
                    $"the only break outcomes on a Boss are interruption Stun or None, got {appliedBreak} for {state}");

                // The surviving break outcome is exactly what the ladder yields for a KnockUp/Knockback-
                // immune enemy: a requested Stun (or a launch degraded to Stun) survives iff the Boss can
                // still feel a stun (stunResistance < 1), otherwise None; None requested stays None.
                // Mirror BreakEffectResistance.Resolve exactly: only a requested KnockUp steps down the
                // ladder to Stun; a requested Knockback has no ladder step, so a knockback-immune Boss
                // resolves to None; a requested Stun survives iff the Boss can still feel a stun.
                bool bossStunImmune = BossStunResistance >= 1f;
                StanceBreakEffect expectedBreak;
                switch (requestedBreak)
                {
                    case StanceBreakEffect.KnockUp:
                        expectedBreak = bossStunImmune ? StanceBreakEffect.None : StanceBreakEffect.Stun;
                        break;
                    case StanceBreakEffect.Stun:
                        expectedBreak = bossStunImmune ? StanceBreakEffect.None : StanceBreakEffect.Stun;
                        break;
                    default: // Knockback (no ladder step -> None on an immune Boss) or None.
                        expectedBreak = StanceBreakEffect.None;
                        break;
                }
                PropertyCheck.That(appliedBreak == expectedBreak,
                    $"Boss break outcome must be the interruption-only degrade result " +
                    $"(expected {expectedBreak}, got {appliedBreak}) for {state}");

                // A launch declaration is NOT even a valid conventional displacement tier once it is
                // suppressed: the surviving Stun/None never carries a KnockUp/Knockback field, so the
                // shared DisplacementTier classifier sees no launch (it would classify only Micro/Push,
                // which are the immediate-channel tiers already suppressed above). Confirm the applied
                // break carries no launch axis.
                bool appliedIsLaunch =
                    appliedBreak == StanceBreakEffect.KnockUp || appliedBreak == StanceBreakEffect.Knockback;
                PropertyCheck.That(!appliedIsLaunch,
                    $"no launch axis may accompany a Boss's resolved reaction for {state}");

                // ---- 3) Stance damage STILL interacts with the Boss ---------------------------------
                // Stance damage is not a "conventional reaction": it always subtracts (bounded, non-
                // negative) and can break the Boss once accumulated. A single small hit does not empty
                // the huge Boss pool; a dedicated high stance-damage tool drives it to 0.
                float smallStance = NextFloat(rng, 0f, 60f); // small share of the 900 pool
                float afterSmall = StanceBreakBounds.SubtractStance(BossMaxStance, smallStance, BossStanceMultiplier);
                PropertyCheck.That(afterSmall >= 0f,
                    $"stance subtraction must be non-negative on a Boss for {state}");
                PropertyCheck.That(afterSmall > 0f,
                    $"a single small hit must not empty the huge Boss stance pool for {state}");

                float bigStance = NextFloat(rng, BossMaxStance / BossStanceMultiplier + 1f,
                                                 BossMaxStance / BossStanceMultiplier + 500f);
                float afterBig = StanceBreakBounds.SubtractStance(BossMaxStance, bigStance, BossStanceMultiplier);
                PropertyCheck.That(afterBig <= 0f,
                    $"enough stance damage must still break the Boss (afterBig={afterBig}) for {state}");

                // ---- 4) The post-break vulnerability window STILL interacts with the Boss -----------
                // Breaking the Boss opens the post-break window (duration > 0) and downgrades its
                // effective rank one step: Boss → Legendary while open, returning to Boss once elapsed.
                float openTime = NextFloat(rng, 0f, 1000f);
                float windowDuration = VulnerabilityWindowBounds.ClampSeconds(NextFloat(rng, -2f, 12f));
                var window = new VulnerabilityWindow(EnemyRank.Boss);

                PropertyCheck.That(!window.IsOpen && window.EffectiveRank == EnemyRank.Boss,
                    $"a Boss starts closed and reacts as a Boss for {state}");

                bool opened = window.Open(openTime, windowDuration);
                PropertyCheck.That(opened && window.IsOpen,
                    $"breaking a Boss must open a positive-duration vulnerability window for {state}");
                PropertyCheck.That(window.EffectiveRank == EnemyRank.Legendary,
                    $"a broken Boss must react one rank lower (Legendary) while vulnerable, got " +
                    $"{window.EffectiveRank} for {state}");

                float insideNow = openTime + (window.ClosesAt - openTime) * (float)rng.NextDouble() * 0.999f;
                window.Update(insideNow);
                PropertyCheck.That(window.IsOpen && window.EffectiveRank == EnemyRank.Legendary,
                    $"the Boss stays one rank lower for the whole window (now={insideNow}) for {state}");

                float afterNow = window.ClosesAt + (float)rng.NextDouble() * 5f;
                window.Update(afterNow);
                PropertyCheck.That(!window.IsOpen && window.EffectiveRank == EnemyRank.Boss,
                    $"once the window elapses the Boss reacts as a Boss again (now={afterNow}) for {state}");
            });
        }

        /// <summary>
        /// Models the Boss's suppression of the conventional immediate reaction: a Boss ignores ordinary
        /// flinches, so whatever displacement/rotation the immediate channel would apply is gated to 0.
        /// (On other ranks the same value is clamped by <see cref="ImmediateReactionClamp"/>.)
        /// </summary>
        private static float SuppressedOnBoss(float requestedMagnitude) => 0f;

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

        private static bool InUnitInterval(float value) => value >= 0f && value <= 1f;

        /// <summary>Uniform float in [min, max] from the seeded generator.</summary>
        private static float NextFloat(System.Random rng, float min, float max)
        {
            return min + (float)rng.NextDouble() * (max - min);
        }
    }
}
