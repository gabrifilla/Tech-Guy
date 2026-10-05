using System;
using NUnit.Framework;

namespace TechGuy.Tests.EditMode
{
    public sealed class ImpactFeedbackScopeTests
    {
        // Feature: posture-enemy-interaction, Property 10: maximum, confirmed damage and exactly one close.
        // Validates: Requirements 5.8, 5.9, 5.10
        [Test] public void MaximumSurvivesLethalDamageReentrancyAndRepeatedClose()
        {
            PropertyCheck.ForAll((rng, i) =>
            {
                int calls = 0; float applied = 0f;
                float common = (float)rng.NextDouble(), emphasis = (float)rng.NextDouble();
                var root = new ImpactFeedbackScope(value => { calls++; applied = value; }, common);
                var child = root.Child(execution: 9, impactIndex: 2);
                root.ConfirmDamage(true); child.ConfirmBreak(true, emphasis);
                root.Dispose(); Assert.AreEqual(0, calls);
                child.Dispose(); child.Dispose(); root.Dispose();
                Assert.AreEqual(1, calls); Assert.AreEqual(Math.Max(common, emphasis), applied);
                Assert.AreEqual(9, child.Execution); Assert.AreEqual(2, child.ImpactIndex);
                Assert.Throws<ObjectDisposedException>(() => root.Child());
            });
        }
        [Test] public void NoDamageZeroAndIndependentCallsNeverCreateOrMergeCandidates()
        {
            int calls = 0;
            using (var noDamage = new ImpactFeedbackScope(_ => calls++, 0.1f)) noDamage.ConfirmDamage(false);
            using (var disabled = new ImpactFeedbackScope(_ => calls++)) { disabled.ConfirmDamage(true); disabled.ConfirmBreak(false, 1f); }
            Assert.AreEqual(0, calls);
            for (int i = 0; i < 2; i++)
                using (var independent = new ImpactFeedbackScope(_ => calls++, 0.1f)) independent.ConfirmDamage(true);
            Assert.AreEqual(2, calls);
        }
        [Test] public void FinallyClosesOnlyConfirmedContributionsAfterException()
        {
            int calls = 0;
            Assert.Throws<InvalidOperationException>(() =>
            {
                using (var root = new ImpactFeedbackScope(_ => calls++, 0.1f))
                using (var child = root.Child())
                {
                    child.ConfirmDamage(true);
                    throw new InvalidOperationException();
                }
            });
            Assert.AreEqual(1, calls);
        }
    }
}
