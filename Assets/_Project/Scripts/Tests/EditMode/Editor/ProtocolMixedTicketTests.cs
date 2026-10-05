using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class ProtocolMixedTicketTests
    {
        [TestCase(1)] [TestCase(2)] [TestCase(8)] [TestCase(9)]
        public void PublishedCatalogRequiresEveryD1Opportunity(int changedNode)
        {
            var definition = ManualRunTestFactory.Create();
            var catalog = ScriptableObject.CreateInstance<ProtocolCatalogDefinition>();
            try
            {
                definition.ConfigureProtocolCatalog(catalog);
                foreach (var node in definition.Nodes)
                    if (node.Id == 1 || node.Id == 2 || node.Id == 9)
                        node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Technique, 1), new ManualRewardStep(RunRewardKind.Protocol, 1) });
                    else if (node.Id == 8)
                        node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Protocol, 2) });
                Assert.That(ManualRouteValidator.Validate(definition, out _, out string error), Is.True, error);
                foreach (var node in definition.Nodes)
                    if (node.Id == changedNode)
                        node.ConfigureRewardSteps(new[] { new ManualRewardStep(node.Reward, 1) });
                Assert.That(ManualRouteValidator.Validate(definition, out _, out error), Is.False);
                StringAssert.Contains("Published D1 reward count", error);
            }
            finally { Object.DestroyImmediate(definition); Object.DestroyImmediate(catalog); }
        }
        [Test] public void MixedStepsKeepRoomPendingAndDeduplicateEachOrdinal()
        {
            var definition = ManualRunTestFactory.Create();
            try
            {
                foreach (var node in definition.Nodes) if (node.Id == 1)
                    node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Technique, 1), new ManualRewardStep(RunRewardKind.Protocol, 1) });
                Assert.That(ManualRouteValidator.TrySnapshot(definition,
                    Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var snapshot, out string error), Is.True, error);
                var route = new ManualRoutePolicy(snapshot); var ready = new RunActionContext(true); var id = route.Session.Id;
                Assert.That(route.TryCommitDestination(id, 1, ready), Is.True);
                Assert.That(route.TryEnterNode(id, 1, ready), Is.True);
                Assert.That(route.TryResolveEncounter(id, 1, true, ready), Is.True);
                var rewards = new RunRewardPolicy(route); var first = new RunRewardRequest(id, 1, 0);
                var firstCard = new[] { new RunBoons.Offer("tech", "Technique", "fixture") };
                var ticket = rewards.Open(first, ready, "r0", firstCard);
                Assert.That(rewards.Open(first, ready, "r0", firstCard), Is.SameAs(ticket));
                int applications = 0;
                Assert.That(rewards.TryChoose(first, 0, "tech", ready, () => true, () =>
                {
                    applications++;
                    Assert.That(rewards.TryChoose(first, 0, "tech", ready, () => true, () => true, null), Is.False);
                    return true;
                }, null), Is.True);
                Assert.That(applications, Is.EqualTo(1));
                Assert.That(route.Session.Phase, Is.EqualTo(RunPhase.RewardPending));
                Assert.That(route.Session.RewardOrdinal, Is.EqualTo(1));
                Assert.That(rewards.Open(first, ready, "r0", firstCard), Is.Null);
                var second = new RunRewardRequest(id, 1, 1);
                var secondCard = new[] { new RunBoons.Offer("ignite", "Ignite", "fixture") };
                ticket = rewards.Open(second, ready, "r1", secondCard);
                Assert.That(ticket.AnnouncedKind, Is.EqualTo(RunRewardKind.Protocol));
                Assert.That(rewards.TryChoose(second, 0, "ignite", ready, () => true, () => false, null), Is.False);
                Assert.That(route.Session.RewardOrdinal, Is.EqualTo(1));
                Assert.That(rewards.TryChoose(second, 0, "ignite", ready, () => true, () => true, null), Is.True);
                Assert.That(route.Session.ProtocolAcquisitions, Is.EqualTo(1));
                Assert.That(route.Session.Phase, Is.EqualTo(RunPhase.RouteChoice));
                Assert.That(rewards.TryChoose(second, 0, "ignite", ready, () => true, () => true, null), Is.False);
            }
            finally { Object.DestroyImmediate(definition); }
        }
    }
}
