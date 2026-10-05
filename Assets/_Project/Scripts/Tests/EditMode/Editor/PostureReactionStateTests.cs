using NUnit.Framework;
using TechGuy.Tests;

namespace TechGuy.Tests.EditMode
{
    public sealed class PostureReactionStateTests
    {
        // Feature: posture-enemy-interaction, Property 5: a legitimate break commits once and opens even without CC.
        // Validates: Requirements 4.1, 4.5, 4.6, 4.7
        [Test]
        public void BreakCommitsOnceEvenForReentrantPressure()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                float max = 1f + (float)rng.NextDouble() * 900f;
                var s = new PostureReactionState(new ReactionConfiguration((EnemyRank)rng.Next(4)), max, 1f, 25f, 2f);
                Assert.IsTrue(s.ApplyStanceDamage(max));
                Assert.AreEqual(max, s.CurrentStance);
                Assert.IsTrue(s.IsVulnerable);
                Assert.IsFalse(s.ApplyStanceDamage(max * 10f));
                Assert.AreEqual(max, s.CurrentStance);
            });
        }

        // Feature: posture-enemy-interaction, Property 8: gameplay clock and recovery respect pause and hit-stop.
        // Validates: Requirements 6.1, 6.3, 6.4, 6.5
        [Test]
        public void RecoveryClockIsFrozenAndClamped()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Normal), 100f, 1f, 25f, 2f);
                s.ApplyStanceDamage(50f);
                s.Advance(100f, paused:true); s.Advance(100f, hitStopped:true);
                Assert.AreEqual(0f, s.Now); Assert.AreEqual(50f, s.CurrentStance);
                s.Advance(2f); Assert.AreEqual(50f, s.CurrentStance);
                s.Advance(1f); Assert.AreEqual(75f, s.CurrentStance);
                s.Advance(1f + (float)rng.NextDouble() * 100f); Assert.AreEqual(100f, s.CurrentStance);
            });
        }

        // Feature: posture-enemy-interaction, Property 9: protected resumption cannot emit fictitious breaks.
        // Validates: Requirements 7.9, 7.11
        [Test]
        public void ResumeHasCompletionFailureAndWatchdogExits()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Elite), 10f, 1f, 0f, 0f);
                Assert.IsTrue(s.ApplyStanceDamage(10f));
                s.Advance(4.1f);
                Assert.IsFalse(s.IsControlLocked); Assert.IsTrue(s.IsResumeProtected);
                Assert.IsFalse(s.ApplyStanceDamage(20f));
                int mode = rng.Next(4);
                if (mode == 0) { s.BeginOffensiveAction(7); s.EndOffensiveAction(7); }
                if (mode == 1) s.EvaluateResumeEligibility(false);
                if (mode == 2) s.CancelOffensiveActionExternally();
                if (mode == 3) { s.Advance(10f); Assert.IsTrue(s.ResumeWatchdogExpired); }
                Assert.IsFalse(s.IsResumeProtected);
                Assert.IsTrue(s.ApplyStanceDamage(10f));
            });
        }

        // Feature: posture-enemy-interaction, Property 13: incompatible motion has one deterministic authority.
        // Validates: Requirements 2.1, 2.4, 6.2, 6.3
        [Test]
        public void MotionPriorityInvalidatesOldTokensAndGenerations()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Normal), 100f, 1f, 0f, 0f);
                int generation = s.Generation;
                int launch = s.StartEffect(StanceBreakEffect.KnockUp, 1f);
                int down = s.StartEffect(StanceBreakEffect.Knockdown, 1f);
                Assert.IsFalse(s.Owns(launch, generation));
                Assert.IsTrue(s.Owns(down, generation));
                Assert.AreEqual(0, s.StartEffect(StanceBreakEffect.Knockback, 0.5f));
                Assert.AreEqual(StanceBreakEffect.Knockdown, s.ActiveMotion);
                s.Reset();
                Assert.IsFalse(s.EndEffect(down, generation));
                Assert.IsFalse(s.IsControlLocked);
            });
        }

        // Feature: posture-enemy-interaction, Property 14: independent physical and opening ownership.
        // Validates: Requirements 4.4, 6.1, 6.2, 6.9
        [Test]
        public void OpeningWithoutHardCcAndOverlappingEffectsHaveCorrectProperties()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Boss), 10f, 1f, 0f, 0f);
                s.ApplyStanceDamage(10f);
                Assert.IsTrue(s.IsControlLocked); Assert.IsFalse(s.IsStunned);
                int stun = s.StartEffect(StanceBreakEffect.Stun, 1f);
                int launch = s.StartEffect(StanceBreakEffect.KnockUp, 2f);
                int gen = s.Generation;
                Assert.IsTrue(s.IsStunned);
                s.EndEffect(stun, gen);
                Assert.IsTrue(s.IsControlLocked); Assert.IsTrue(s.Owns(launch, gen));
                s.Advance(2.1f);
                Assert.IsTrue(s.IsControlLocked); Assert.IsFalse(s.IsStunned);
                s.Advance(1f);
                Assert.IsFalse(s.IsControlLocked);
            });
        }

        [Test]
        public void SmallMobReapplicationIsLimitedWithoutExtendingForever()
        {
            var s = new PostureReactionState(new ReactionConfiguration(EnemyRank.Normal), 100f, 1f, 0f, 0f);
            int token = s.StartEffect(StanceBreakEffect.KnockUp, 2f);
            for (int i = 0; i < 9; i++) { s.Advance(0.5f); token = s.StartEffect(StanceBreakEffect.KnockUp, 2f); }
            s.Advance(0.6f);
            Assert.IsFalse(s.IsControlLocked);
            Assert.IsFalse(s.Owns(token, s.Generation));
        }
    }
}
