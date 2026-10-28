using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class BowBoonTests
    {
        [TestCase(0f, false, .6f)] [TestCase(.7999f, false, .6f)]
        [TestCase(.8f, true, 1f)] [TestCase(1.6f, true, 1f)]
        public void JudgmentThresholdChangesGeometryBudget(float seconds, bool charged, float scale)
        { Assert.AreEqual(charged, BowBoonModel.IsCharged(seconds)); Assert.AreEqual(scale, BowBoonModel.JudgmentScale(seconds)); }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(-1f)]
        public void InvalidChargeNeverQualifies(float seconds) => Assert.IsFalse(BowBoonModel.IsCharged(seconds));
        [Test] public void PriorityRequiresForwardConeAndActualShotRange()
        {
            Assert.IsTrue(BowBoonModel.WithinCone(Vector3.zero, Vector3.forward, Vector3.forward * 10, 10));
            Assert.IsFalse(BowBoonModel.WithinCone(Vector3.zero, Vector3.forward, Vector3.forward * 10.01f, 10));
            Assert.IsFalse(BowBoonModel.WithinCone(Vector3.zero, Vector3.forward, Vector3.back, 10));
            Assert.IsFalse(BowBoonModel.WithinCone(Vector3.zero, Vector3.zero, Vector3.forward, 10));
            Assert.IsFalse(BowBoonModel.WithinCone(Vector3.zero, Vector3.forward, Vector3.right, 10));
        }
        [TestCase(float.NaN)] [TestCase(float.PositiveInfinity)] [TestCase(0f)] [TestCase(-1f)]
        public void InvalidRangeNeverGrantsPriority(float range)
            => Assert.IsFalse(BowBoonModel.WithinCone(Vector3.zero, Vector3.forward, Vector3.forward, range));
        [Test] public void ExecutableCatalogHasStableUniqueIdsAndExplicitDependencies()
        {
            var definitions = BowBoonCatalog.Definitions().ToArray();
            Assert.AreEqual(7, definitions.Length); Assert.AreEqual(7, definitions.Select(d => d.Id).Distinct().Count());
            Assert.IsTrue(BoonCatalogValidator.TryCreateSnapshot(definitions, out _, out var diagnostic), diagnostic);
            var duo = definitions.Single(d => d.Id == BowBoonCatalog.HuntingDetonatorId);
            CollectionAssert.AreEquivalent(new[] { BowBoonCatalog.MineGardenId, BowBoonCatalog.PredatorSealId }, duo.RequiredBoonIds);
            Assert.AreEqual(3, definitions.Single(d => d.Id == BowBoonCatalog.MineGardenId).ImpactRules.MaxInstancesPerCast);
            foreach (var mutation in definitions.Where(d => d.Family == BoonFamily.Mutation))
            { Assert.IsTrue(mutation.EnabledForOffers); Assert.IsTrue(mutation.ImpactRules.CanCreateCausalRoot); Assert.IsFalse(mutation.ImpactRules.CanGenerateEnergy); }
        }
    }
}
