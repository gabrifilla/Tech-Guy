using NUnit.Framework;

namespace TechGuy.Tests.Fluidity.EditMode
{
    /// <summary>
    /// New fluidity contracts only. Historical tests are being reorganized by the user and are not
    /// restored here. Distance hysteresis with real navigation remains in RangedFluidityPlayModeTests.
    /// </summary>
    public sealed class RangedFluidityContractTests
    {
        [Test]
        public void LocomotionHysteresisKeepsItsStateAtSmallResidualVelocities()
        {
            Assert.That(EnemyLocomotionPresentation.WantsWalk(0f, false), Is.False);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(.1f, false), Is.True);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(.05f, true), Is.True);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(.05f, false), Is.False);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(.02f, true), Is.False);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(float.NaN, true), Is.False);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(float.PositiveInfinity, true), Is.False);
            Assert.That(EnemyLocomotionPresentation.WantsWalk(float.NegativeInfinity, false), Is.False);
        }

        [Test]
        public void PauseAndInvalidDeltaDoNotAdvanceRetreatOrCooldown()
        {
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
            float[] invalidOrPausedDeltas = { 0f, -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity };
            for (int i = 0; i < 20; i++)
                foreach (float delta in invalidOrPausedDeltas)
                    Assert.That(kite.Tick(true, delta, 1f, 2f), Is.True);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Kiting));
            kite.EndRetreat();
            for (int i = 0; i < 20; i++)
                foreach (float delta in invalidOrPausedDeltas)
                    Assert.That(kite.Tick(i % 2 == 0, delta, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
        }

        [Test]
        public void ArrivalOpensCooldownAndLifecycleResetClearsIt()
        {
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
            Assert.That(kite.Tick(false, .1f, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(kite.Tick(true, .2f, 1f, 2f), Is.False);
            kite.Reset();
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Eligible));
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
        }

        [Test]
        public void LeavingAndReenteringTheThresholdDoesNotEraseRetreatCooldown()
        {
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
            Assert.That(kite.Tick(true, 1f, 1f, 2f), Is.False);
            Assert.That(kite.Tick(false, .1f, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown),
                "The cooldown is gameplay time, not a resettable distance gate.");
            Assert.That(kite.Tick(true, .1f, 1f, 2f), Is.False);
            Assert.That(kite.Tick(false, 1.81f, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Eligible));
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
        }

        [Test]
        public void RepeatedInterruptionCannotEraseOrRestartAnExistingCooldown()
        {
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
            Assert.That(kite.Tick(true, .25f, 1f, 2f), Is.True);
            kite.EndRetreat();
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(kite.Tick(true, 1f, 1f, 2f), Is.False);
            kite.EndRetreat(); // A later interruption must not restart the pause either.
            Assert.That(kite.Tick(false, .99f, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(kite.Tick(false, .02f, 1f, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Eligible));
            Assert.That(kite.Tick(true, 0f, 1f, 2f), Is.True);
        }

        [TestCase(30)]
        [TestCase(60)]
        [TestCase(120)]
        public void ContinuousPressureIsBoundedAndCooldownSurvivesRepeatedThresholdCrossing(int framesPerSecond)
        {
            const float window = 1f, cooldown = 2f;
            float delta = 1f / framesPerSecond;
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, window, cooldown), Is.True);
            float elapsed = 0f;
            bool permitted = true;
            int frames = 0;
            while (permitted && frames < framesPerSecond * 2)
            {
                elapsed += delta;
                permitted = kite.Tick(true, delta, window, cooldown);
                if (elapsed < window) Assert.That(permitted, Is.True, "Continuous retreat must retain its allowed window.");
                frames++;
            }
            Assert.That(permitted, Is.False, "A pressed Shooter must not retreat indefinitely.");
            Assert.That(elapsed, Is.InRange(window, window + delta + .0001f));
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            float cooldownElapsed = 0f;
            frames = 0;
            while (cooldownElapsed + delta < cooldown - delta)
            {
                cooldownElapsed += delta;
                Assert.That(kite.Tick(frames++ % 2 == 0, delta, window, cooldown), Is.False,
                    "Crossing the threshold cannot grant an early retreat during cooldown.");
                Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            }
            Assert.That(kite.Tick(false, cooldown - cooldownElapsed + .001f, window, cooldown), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Eligible));
            Assert.That(kite.Tick(true, 0f, window, cooldown), Is.True);
        }

        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(-1f)]
        public void MalformedDurationsCannotCreateAnUnboundedRetreatOrConsumePausedTime(float malformedDuration)
        {
            var kite = new KiteController();
            Assert.That(kite.Tick(true, 0f, malformedDuration, 2f), Is.True);
            Assert.That(kite.Tick(true, .1f, malformedDuration, 2f), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(kite.Tick(true, 0f, 1f, malformedDuration), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Cooldown));
            Assert.That(kite.Tick(false, .1f, 1f, malformedDuration), Is.False);
            Assert.That(kite.Current, Is.EqualTo(KiteController.Phase.Eligible));
        }
    }
}
