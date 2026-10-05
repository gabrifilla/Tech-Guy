using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests
{
    public sealed class ProtocolRewardSelectionTests
    {
        [TestCase("Ignite,Conductor,Frost,Resonance")]
        [TestCase("Ignite,Reactor,Detonation,Resonance")]
        [TestCase("Frost,Detonation,Ignite,Reactor")]
        [TestCase("Frost,Conductor,Overflow,Resonance")]
        public void PublishedPrioritiesPermitTheDeclaredCompositions(string choices)
        {
            var source = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            Assert.That(source.TryCreateSnapshot(out var snapshot, out _), Is.True);
            UnityEngine.Object.DestroyImmediate(source);
            var inventory = new ProtocolInventory(Guid.NewGuid(), snapshot); int opportunity = 1;
            foreach (string choice in choices.Split(','))
            {
                var kind = (ProtocolKind)Enum.Parse(typeof(ProtocolKind), choice);
                var cards = ProtocolRewardSelection.Select(inventory, opportunity++);
                Assert.That(cards.Count, Is.LessThanOrEqualTo(3)); Assert.That(cards, Does.Contain(kind));
                Assert.That(inventory.TryAcquire(inventory.SessionId, kind, inventory.Revision, _ => true), Is.True);
            }
        }
        [Test] public void LegacyNodeUsesItsOldCategoryAndAuthoredMixedStepsHaveDistinctOrdinals()
        {
            var node = new ManualNodeDefinition(1, "C1", ManualNodeKind.Combat, RunRewardKind.Technique, 1,
                Vector3.zero, new Vector2(24, 20), 0, null);
            Assert.That(node.RewardAt(0), Is.EqualTo(RunRewardKind.Technique));
            Assert.That(node.RewardAt(1), Is.EqualTo(RunRewardKind.None));
            node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Technique, 1), new ManualRewardStep(RunRewardKind.Protocol, 1) });
            Assert.That(node.TotalAcquisitions, Is.EqualTo(2));
            Assert.That(node.RewardAt(1), Is.EqualTo(RunRewardKind.Protocol));
            Assert.That(node.RewardAt(2), Is.EqualTo(RunRewardKind.None));
        }
    }
}
