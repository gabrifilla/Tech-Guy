using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class SpearBoonTests
    {
        [Test]
        public void HorizonRequiresACrossingAndNeverAcceptsParallelOrCollinearCasts()
        {
            Vector3 a = Vector3.zero, b = Vector3.forward * 8;
            Assert.IsTrue(SpearBoonGeometry.Crosses(a, b, new Vector3(-2, 0, 4), new Vector3(2, 0, 4)));
            Assert.IsFalse(SpearBoonGeometry.Crosses(a, b, Vector3.right, Vector3.right + b));
            Assert.IsFalse(SpearBoonGeometry.Crosses(a, b, a, b));
            Assert.IsFalse(SpearBoonGeometry.Crosses(a, b, new Vector3(-2, 0, 9), new Vector3(2, 0, 9)));
            Assert.IsFalse(SpearBoonGeometry.Crosses(a, b, a, a));
            Assert.IsFalse(SpearBoonGeometry.Crosses(a, b, a, Vector3.right * 4), "A shared launch point is not a deliberate crossing.");
        }
        [Test]
        public void PlanarCrossingDoesNotDependOnHeightOrEndpointOrder()
        {
            Vector3 a = Vector3.zero, b = Vector3.forward * 8, c = new Vector3(-2, 10, 4), d = new Vector3(2, -10, 4);
            Assert.IsTrue(SpearBoonGeometry.Crosses(b, a, c, d));
            Assert.IsTrue(SpearBoonGeometry.Crosses(a, b, d, c));
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(float.NegativeInfinity)]
        public void InvalidGeometryCannotTriggerAnImpact(float invalid)
        {
            var point = new Vector3(invalid, 0, 0);
            Assert.IsFalse(SpearBoonGeometry.Crosses(point, Vector3.forward, Vector3.left, Vector3.right));
            Assert.IsTrue(float.IsPositiveInfinity(SpearBoonGeometry.DistanceToSegment(point, Vector3.zero, Vector3.forward)));
        }
        [Test]
        public void SegmentDistanceClampsBothEndsAndHandlesAStationaryPulse()
        {
            Assert.AreEqual(2, SpearBoonGeometry.DistanceToSegment(Vector3.back * 2, Vector3.zero, Vector3.forward * 3), .0001f);
            Assert.AreEqual(1, SpearBoonGeometry.DistanceToSegment(new Vector3(1, 0, 2), Vector3.zero, Vector3.forward * 3), .0001f);
            Assert.AreEqual(5, SpearBoonGeometry.DistanceToSegment(Vector3.forward * 5, Vector3.zero, Vector3.zero), .0001f);
        }
        [Test]
        public void EachOrbitTurnClosesAtTheSamePointWithoutMovingItsCenter()
        {
            Vector3 center = new Vector3(4, 0, 8);
            for (int turn = 0; turn <= 3; turn++)
                Assert.Less(Vector3.Distance(center + Vector3.forward * 2.5f,
                    SpearBoonGeometry.OrbitPoint(center, Vector3.forward, turn)), .0001f);
            Assert.AreEqual(2.5f, Vector3.Distance(center, SpearBoonGeometry.OrbitPoint(center, Vector3.forward, .375f)), .0001f);
        }
        [Test]
        public void OrbitRejectsInvalidRouteDataInsteadOfProducingANonFinitePosition()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SpearBoonGeometry.OrbitPoint(Vector3.zero, Vector3.forward, float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpearBoonGeometry.OrbitPoint(Vector3.zero, Vector3.forward, 0, float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(() => SpearBoonGeometry.OrbitPoint(Vector3.zero, Vector3.forward, 0, 0));
            Assert.Throws<ArgumentException>(() => SpearBoonGeometry.OrbitPoint(new Vector3(float.NaN, 0, 0), Vector3.forward, 0));
        }
        [Test]
        public void ReceiptIsPerVictimGenerationAndPhysicalPassage()
        {
            var receipts = new SpearBoonReceipts();
            Assert.IsTrue(receipts.TryClaim(7, 1, 0)); Assert.IsFalse(receipts.TryClaim(7, 1, 0));
            Assert.IsTrue(receipts.TryClaim(7, 1, 1)); Assert.IsTrue(receipts.TryClaim(7, 2, 0));
            Assert.IsTrue(receipts.TryClaim(8, 1, 0)); Assert.AreEqual(4, receipts.Count);
        }
        [Test]
        public void ReturnMarkRequiresConfirmedOutgoingAndConsumesOnlyOnceForThatGeneration()
        {
            var receipts = new SpearBoonReceipts();
            Assert.IsFalse(receipts.TryConfirmReturn(7, 1));
            receipts.ConfirmOutgoing(7, 1);
            Assert.IsTrue(receipts.TryConfirmReturn(7, 1)); Assert.IsFalse(receipts.TryConfirmReturn(7, 1));
            Assert.IsFalse(receipts.TryConfirmReturn(7, 2));
            receipts.ConfirmOutgoing(7, 2); Assert.IsTrue(receipts.TryConfirmReturn(7, 2));
        }
        [Test]
        public void ExecutableCatalogHasSixUniqueDefinitionsAndExplicitDependencies()
        {
            var definitions = SpearBoonCatalog.Definitions().ToArray();
            Assert.AreEqual(6, definitions.Length);
            Assert.AreEqual(6, definitions.Select(d => d.Id).Distinct().Count());
            Assert.IsTrue(definitions.All(d => d.EnabledForOffers));
            Assert.IsTrue(BoonCatalogValidator.TryCreateSnapshot(definitions, out _, out var error), error);
            var line = definitions.Single(d => d.Id == SpearBoonCatalog.ReturnLineId);
            CollectionAssert.AreEquivalent(new[] { SpearBoonCatalog.OrbitId, SpearBoonCatalog.ReturnMarkId }, line.RequiredBoonIds);
            Assert.IsFalse(line.ImpactRules.CanCreateCausalRoot); Assert.IsFalse(line.ImpactRules.CanGenerateEnergy);
            Assert.IsTrue(definitions.All(d => d.ImpactRules.MaxDerivedImpactsPerCast <= 32 && d.ImpactRules.MaxDepth <= 4));
        }
    }
}
