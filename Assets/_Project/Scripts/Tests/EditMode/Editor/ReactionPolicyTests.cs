using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    public sealed class ReactionPolicyTests
    {
        // Feature: posture-enemy-interaction, Property 1: interruption rejection does not discard posture.
        // Validates: Requirements 1.1, 1.6, 7.2
        [Test]
        public void BelowThresholdPreservesAttackWhilePostureStillBreaks()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var config = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob);
                float max = 1f + (float)rng.NextDouble() * 100f;
                var state = new PostureReactionState(config, max, 1f, 0f, 0f);
                Assert.IsFalse(ReactionPolicy.ResolveInterrupt(config, false, false, true, false, 0f).CancelsAttack);
                Assert.IsTrue(state.ApplyStanceDamage(max));
                Assert.IsTrue(state.IsVulnerable);
                Assert.IsFalse(state.IsStunned);
            });
        }

        // Feature: posture-enemy-interaction, Property 4: requested/default physical fallback is binary.
        // Validates: Requirements 2.6, 3.7, 3.8
        [Test]
        public void BinaryResistanceAndFinalNonePreservePhysicalContract()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float resistance = (float)rng.NextDouble();
                var small = new ReactionConfiguration(EnemyRank.Normal);
                var medium = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob);
                Assert.AreEqual(StanceBreakEffect.KnockUp, ReactionPolicy.ResolveBreak(small, StanceBreakEffect.None, 0f, resistance, 0f));
                Assert.AreEqual(StanceBreakEffect.Knockdown, ReactionPolicy.ResolveBreak(medium, StanceBreakEffect.None, 0f, 0f, 0f));
                Assert.AreEqual(2f, ReactionPolicy.AllowedDuration(2f, resistance));
                Assert.AreEqual(StanceBreakEffect.Stun, ReactionPolicy.ResolvePhysical(small, StanceBreakEffect.KnockUp, 0f, 1f, 0f));
                Assert.AreEqual(StanceBreakEffect.None, ReactionPolicy.ResolvePhysical(small, StanceBreakEffect.KnockUp, 1f, 1f, 0f));
                Assert.AreEqual(StanceBreakEffect.None, ReactionPolicy.ResolvePhysical(small, StanceBreakEffect.None, 0f, 0f, 0f));
                Assert.AreEqual(0f, ReactionPolicy.AllowedDuration(2f, 1f));
            });
        }

        // Feature: posture-enemy-interaction, Property 7: eligibility precedes the attack window.
        // Validates: Requirements 7.1, 7.2, 7.5, 7.6, 7.12
        [Test]
        public void WindowCannotBypassThresholdProtectionOrBossIdentity()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var medium = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob);
                bool threshold = rng.Next(2) == 0, window = rng.Next(2) == 0;
                Assert.AreEqual(threshold && window,
                    ReactionPolicy.ResolveInterrupt(medium, false, false, window, threshold, 0f).CancelsAttack);
                Assert.IsFalse(ReactionPolicy.ResolveInterrupt(medium, true, false, true, true, 0f).CancelsAttack);
                Assert.IsFalse(ReactionPolicy.ResolveInterrupt(medium, false, true, true, true, 0f).CancelsAttack);
                var boss = new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob);
                Assert.IsFalse(ReactionPolicy.ResolveInterrupt(boss, false, false, true, true, 0f).CancelsAttack);
                Assert.AreEqual(StanceBreakEffect.None, ReactionPolicy.ResolvePhysical(boss, StanceBreakEffect.KnockUp, 1f, 0f, 0f));
            });
        }

        // Feature: posture-enemy-interaction, Property 6: no tick/zero trigger and exact decay deadline.
        // Validates: Requirements 7.3, 7.4, 7.8
        [Test]
        public void ExcessRequiresPositiveContributionAndZeroDoesNotRefreshDecay()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var a = new RankInterruptStagger((EnemyRank)rng.Next(4), 0.5f);
                float threshold = a.Threshold;
                Assert.AreEqual(RankInterruptStagger.Outcome.StaggerOrBreak, a.Accumulate(threshold * 3f, 0f, 2f));
                Assert.AreEqual(threshold * 2f, a.Accumulated, 0.0001f);
                Assert.AreEqual(RankInterruptStagger.Outcome.Preserved, a.Accumulate(0f, 1f, 2f));
                a.Decay(1.99f, 2f); Assert.AreEqual(threshold * 2f, a.Accumulated, 0.0001f);
                a.Decay(2f, 2f); Assert.AreEqual(0f, a.Accumulated);
                Assert.AreEqual(RankInterruptStagger.Outcome.Preserved, a.Accumulate(float.NaN));
                Assert.AreEqual(RankInterruptStagger.Outcome.Preserved, a.Accumulate(float.PositiveInfinity));
            });
        }
    }
}
