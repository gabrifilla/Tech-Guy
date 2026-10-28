using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class GroundMovementBufferTests
    {
        [Test]
        public void LatestValidDestinationReplacesPrevious_AndIsConsumedOnce()
        {
            var buffer = new GroundMovementBuffer();
            Assert.IsTrue(buffer.Store(Vector3.right, 1f, .35f));
            Assert.IsTrue(buffer.Store(Vector3.forward * 7f, 1.1f, .35f));
            Assert.IsTrue(buffer.TryTake(1.2f, out Vector3 taken));
            Assert.AreEqual(Vector3.forward * 7f, taken);
            Assert.IsFalse(buffer.HasPending);
            Assert.IsFalse(buffer.TryTake(1.2f, out _));
        }

        [Test]
        public void DeadlineIsInclusive_AndTheNextClockStepExpiresPermanently()
        {
            var buffer = new GroundMovementBuffer();
            Assert.IsTrue(buffer.Store(Vector3.right, 4f, .25f));
            Assert.IsTrue(buffer.TryTake(4.25f, out _));
            Assert.IsTrue(buffer.Store(Vector3.forward, 4f, .25f));
            Assert.IsFalse(buffer.TryTake(4.2501f, out _));
            Assert.IsFalse(buffer.HasPending);
            Assert.IsFalse(buffer.TryTake(4.1f, out _), "Expired input must not replay after the clock changes.");
        }

        [TestCase(0f)]
        [TestCase(-.1f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        [TestCase(float.NegativeInfinity)]
        public void InvalidLifetimePreservesPreviousValidOrder(float lifetime)
        {
            var buffer = new GroundMovementBuffer();
            Assert.IsTrue(buffer.Store(Vector3.right, 1f, .35f));
            Assert.IsFalse(buffer.Store(Vector3.forward, 1f, lifetime));
            Assert.IsTrue(buffer.TryTake(1.1f, out Vector3 taken));
            Assert.AreEqual(Vector3.right, taken);
        }

        [Test]
        public void NonFiniteDestinationOrClockCannotDestroyAValidIntent()
        {
            foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
            {
                var buffer = new GroundMovementBuffer();
                Assert.IsTrue(buffer.Store(Vector3.right, 1f, .35f));
                Assert.IsFalse(buffer.Store(new Vector3(invalid, 0, 0), 1f, .35f));
                Assert.IsFalse(buffer.Store(new Vector3(0, invalid, 0), 1f, .35f));
                Assert.IsFalse(buffer.Store(new Vector3(0, 0, invalid), 1f, .35f));
                Assert.IsFalse(buffer.Store(Vector3.forward, invalid, .35f));
                Assert.IsFalse(buffer.TryTake(invalid, out _));
                Assert.IsTrue(buffer.HasPending);
                Assert.IsTrue(buffer.TryTake(1.1f, out Vector3 taken));
                Assert.AreEqual(Vector3.right, taken);
            }
        }

        [Test]
        public void FiniteInputsWithOverflowingDeadlineAreRejectedBeforeReplacingIntent()
        {
            var buffer = new GroundMovementBuffer();
            Assert.IsTrue(buffer.Store(Vector3.right, 1f, .35f));
            Assert.IsFalse(buffer.Store(Vector3.forward, float.MaxValue, float.MaxValue));
            Assert.IsTrue(buffer.TryTake(1.1f, out Vector3 taken));
            Assert.AreEqual(Vector3.right, taken);
        }

        [Test]
        public void DiscardCannotReplayAndTheBufferAcceptsANewIntent()
        {
            var buffer = new GroundMovementBuffer();
            Assert.IsTrue(buffer.Store(Vector3.right, 1f, .35f));
            buffer.Discard(); buffer.Discard();
            Assert.IsFalse(buffer.HasPending);
            Assert.IsFalse(buffer.TryTake(1f, out _));
            Assert.IsTrue(buffer.Store(Vector3.forward, 2f, .35f));
            Assert.IsTrue(buffer.TryTake(2f, out Vector3 taken));
            Assert.AreEqual(Vector3.forward, taken);
        }
    }
}
