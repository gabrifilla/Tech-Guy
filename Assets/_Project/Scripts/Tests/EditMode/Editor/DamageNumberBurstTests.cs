using NUnit.Framework;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class DamageNumberBurstTests
    {
        [Test]
        public void BurstSumsOnlyConfirmedPositiveDamage_AndPromotesLethal()
        {
            var burst = new DamageNumberBurst();
            Assert.IsTrue(burst.TryBegin(12f, false, 10f));
            Assert.IsTrue(burst.TryAccumulate(7f, false, 10.04f));
            Assert.IsTrue(burst.TryAccumulate(3f, true, 10.1f));
            Assert.AreEqual(22f, burst.Amount);
            Assert.AreEqual(3, burst.HitCount);
            Assert.IsTrue(burst.IsLethal);
            Assert.IsFalse(burst.TryAccumulate(50f, false, 10.11f), "A dead victim cannot renew a lethal confirmation.");
            Assert.AreEqual(22f, burst.Amount);
        }

        [Test]
        public void MergeWindowIsMeasuredFromFirstImpact_AndDoesNotSlideWithHits()
        {
            var burst = new DamageNumberBurst();
            Assert.IsTrue(burst.TryBegin(1f, false, 0f));
            Assert.IsTrue(burst.TryAccumulate(1f, false, .06f));
            Assert.IsTrue(burst.TryAccumulate(1f, false, DamageNumberBurst.MergeWindowSeconds));
            Assert.IsFalse(burst.TryAccumulate(1f, false, .121f));
            Assert.AreEqual(0f, burst.BornAt);
            Assert.AreEqual(3f, burst.Amount);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidDamageCannotCreateOrAlterConfirmation(float amount)
        {
            var burst = new DamageNumberBurst();
            Assert.IsFalse(burst.TryBegin(amount, true, 0f));
            Assert.IsTrue(burst.TryBegin(5f, false, 0f));
            Assert.IsFalse(burst.TryAccumulate(amount, true, .05f));
            Assert.AreEqual(5f, burst.Amount);
            Assert.IsFalse(burst.IsLethal);
            Assert.AreEqual(1, burst.HitCount);
        }

        [Test]
        public void InvalidOrBackwardClockCannotAlterBurst()
        {
            var burst = new DamageNumberBurst();
            Assert.IsFalse(burst.TryBegin(5f, false, float.NaN));
            Assert.IsTrue(burst.TryBegin(5f, false, 10f));
            Assert.IsFalse(burst.TryAccumulate(5f, true, 9f));
            Assert.IsFalse(burst.TryAccumulate(5f, true, float.PositiveInfinity));
            Assert.AreEqual(5f, burst.Amount);
            Assert.IsFalse(burst.IsLethal);
        }

        [Test]
        public void AccumulationRemainsFiniteWhenTwoLargeValidHitsExceedFloatRange()
        {
            var burst = new DamageNumberBurst();
            Assert.IsTrue(burst.TryBegin(float.MaxValue, false, 0f));
            Assert.IsTrue(burst.TryAccumulate(float.MaxValue, false, .05f));
            Assert.AreEqual(float.MaxValue, burst.Amount);
        }

        [Test]
        public void IndependentVictimsDoNotShareDamageOrLifetime()
        {
            // Keep the same 100 seeded cases without depending on a test harness being reorganized.
            var random = new System.Random(146209);
            for (int caseIndex = 0; caseIndex < 100; caseIndex++)
            {
                float first = random.Next(1, 1000), second = random.Next(1, 1000);
                var a = new DamageNumberBurst(); var b = new DamageNumberBurst();
                Assert.IsTrue(a.TryBegin(first, false, 0f));
                Assert.IsTrue(b.TryBegin(second, false, .08f));
                Assert.IsTrue(a.TryAccumulate(second, false, .1f), $"First victim merge in case {caseIndex}.");
                Assert.IsTrue(b.TryAccumulate(first, false, .19f), $"Independent window in case {caseIndex}.");
                Assert.AreEqual(first + second, a.Amount, $"First victim sum in case {caseIndex}.");
                Assert.AreEqual(first + second, b.Amount, $"Second victim sum in case {caseIndex}.");
                Assert.IsFalse(a.TryAccumulate(1f, false, .19f), $"First window remains closed in case {caseIndex}.");
            }
        }
    }
}
