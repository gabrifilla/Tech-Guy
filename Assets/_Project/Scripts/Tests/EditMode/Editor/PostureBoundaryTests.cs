using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class PostureBoundaryTests
    {
        // Feature: posture-enemy-interaction, Property 9: bounded authoring and independent expiration.
        // Validates: Requirements 6.1, 6.2, 6.3, 7.7
        [Test]
        public void KnockdownPhasesAndReapplicationPoliciesExpireTheirOwnTokens()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var data = new ReactionAuthoring();
                int policy = rng.Next(3);
                JsonUtility.FromJsonOverwrite("{\"_overrideReapplication\":true,\"_reapplication\":" + policy + "}", data);
                var config = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob, data);
                var s = new PostureReactionState(config, 100f, 1f, 0f, 0f);
                int first = s.StartEffect(StanceBreakEffect.Knockdown, 1.3f);
                Assert.AreEqual(KnockdownPhase.Falling, s.CurrentKnockdownPhase);
                s.Advance(0.21f); Assert.AreEqual(KnockdownPhase.Grounded, s.CurrentKnockdownPhase);
                s.Advance(0.8f); Assert.AreEqual(KnockdownPhase.Recovering, s.CurrentKnockdownPhase);
                s.Advance(0.3f); Assert.AreEqual(KnockdownPhase.None, s.CurrentKnockdownPhase);
                Assert.IsFalse(s.EndEffect(first, s.Generation));
                int original = s.StartEffect(StanceBreakEffect.Stun, 2f);
                s.Advance(1f);
                int repeated = s.StartEffect(StanceBreakEffect.Stun, 2f);
                Assert.AreEqual(policy == 0, repeated == 0);
                s.Advance(1.1f);
                Assert.AreEqual(policy != 0, s.IsStunned);
                s.Advance(5f); Assert.IsFalse(s.IsStunned);
                Assert.IsFalse(s.EndEffect(original, s.Generation));
            });
        }

        // Feature: posture-enemy-interaction, Property 12: fallback never bypasses immunity.
        // Validates: Requirements 2.6, 3.8, 6.8, 6.9
        [Test]
        public void KnockdownAndBossExceptionsRevalidateEveryFallbackChannel()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var data = new ReactionAuthoring();
                JsonUtility.FromJsonOverwrite("{\"_overrideKnockdownResistance\":true,\"_knockdownResistance\":1}", data);
                var medium = new ReactionConfiguration(EnemyRank.Normal, ReactionCategory.MediumMob, data);
                Assert.AreEqual(StanceBreakEffect.Stun, ReactionPolicy.ResolveBreak(medium, StanceBreakEffect.None, 0f, 0f, 0f));
                Assert.AreEqual(StanceBreakEffect.None, ReactionPolicy.ResolveBreak(medium, StanceBreakEffect.None, 1f, 0f, 0f));
                bool exception = rng.Next(2) == 0;
                JsonUtility.FromJsonOverwrite("{\"_allowBossLaunch\":" + (exception ? "true" : "false") + "}", data);
                var boss = new ReactionConfiguration(EnemyRank.Boss, ReactionCategory.SmallMob, data);
                Assert.AreEqual(exception ? StanceBreakEffect.KnockUp : StanceBreakEffect.None,
                    ReactionPolicy.ResolvePhysical(boss, StanceBreakEffect.KnockUp, 1f, 0f, 0f));
                Assert.AreEqual(StanceBreakEffect.None, ReactionPolicy.ResolvePhysical(boss, StanceBreakEffect.Knockback, 0f, 0f, 0f));
            });
        }

        [Test]
        public void InvalidContributionsDoNotDelayRecoveryOrCreateEffects()
        {
            var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Normal), 100f, 1f, 25f, 1f);
            s.ApplyStanceDamage(50f); s.Advance(0.9f);
            foreach (float value in new[] { 0f, -1f, float.NaN, float.PositiveInfinity })
            {
                Assert.IsFalse(s.ApplyStanceDamage(value));
                Assert.AreEqual(0, s.StartEffect(StanceBreakEffect.Stun, value));
            }
            s.Advance(1.1f); Assert.AreEqual(75f, s.CurrentStance);
            s.Advance(float.PositiveInfinity); Assert.AreEqual(2f, s.Now);
            Assert.AreEqual(StanceBreakEffect.None, s.ActiveMotion);
        }
    }
}
