using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class BoonRewardSeedTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _source;

        [SetUp]
        public void SetUp()
        {
            _definition = FluidityRouteTestFactory.Create();
            Assert.That(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out _source, out string error), Is.True, error);
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_definition);

        [TestCase(0UL)]
        [TestCase(1701UL)]
        [TestCase(ulong.MaxValue)]
        public void SameRunSeedReproducesCardsAcrossIndependentSessions(ulong seed)
        {
            var first = Open(seed);
            var second = Open(seed);
            Assert.That(second.route.Session.Id, Is.Not.EqualTo(first.route.Session.Id));
            Assert.That(second.ticket.Choices.Select(o => o.Id), Is.EqualTo(first.ticket.Choices.Select(o => o.Id)));
            Assert.That(first.route.Session.Route.RewardSeed, Is.EqualTo(seed));
            Assert.That(_source.RewardSeed, Is.Null, "Runtime generation must not mutate authoring.");
        }

        [Test]
        public void ReopenKeepsCardsAndCatalogEnumerationOrderDoesNotAffectDraw()
        {
            var first = Open(51UL);
            var choices = first.ticket.Choices.Select(o => o.Id).ToArray();
            var reopened = first.policy.Open(first.ticket.Request, new RunActionContext(true), "unchanged", Offers().Reverse().ToArray());
            Assert.That(reopened, Is.SameAs(first.ticket));
            Assert.That(reopened.Choices.Select(o => o.Id), Is.EqualTo(choices));
            var independent = Open(51UL, true);
            Assert.That(independent.ticket.Choices.Select(o => o.Id), Is.EqualTo(choices));
        }

        [Test]
        public void DistinctRunSeedsProduceVarietyWithoutUsingUnityRandom()
        {
            var state = UnityEngine.Random.state;
            try
            {
                UnityEngine.Random.InitState(7919);
                float expected = UnityEngine.Random.value;
                UnityEngine.Random.InitState(7919);
                var signatures = Enumerable.Range(0, 32).Select(i => string.Join(",", Open((ulong)i).ticket.Choices.Select(o => o.Id))).ToArray();
                Assert.That(signatures.Distinct().Count(), Is.GreaterThan(8));
                Assert.That(UnityEngine.Random.value, Is.EqualTo(expected));
            }
            finally { UnityEngine.Random.state = state; }
        }

        [Test]
        public void InventoryRevisionIsReproducibleAndStaleSelectionIsRejected()
        {
            var first = Open(42UL);
            var second = Open(42UL);
            string previous = first.ticket.Choices[0].Id;
            int revision = first.ticket.Revision;
            var a = first.policy.Open(first.ticket.Request, new RunActionContext(true), "changed", Offers());
            var b = second.policy.Open(second.ticket.Request, new RunActionContext(true), "changed", Offers());
            Assert.That(a.Revision, Is.EqualTo(revision + 1));
            Assert.That(a.Choices.Select(o => o.Id), Is.EqualTo(b.Choices.Select(o => o.Id)));
            bool applied = false;
            Assert.That(first.policy.TryChoose(a.Request, revision, previous, new RunActionContext(true),
                () => true, () => { applied = true; return true; }, null), Is.False);
            Assert.That(applied, Is.False);
        }

        [Test]
        public void AlternativeCardsHaveTheSameCanonicalDrawRegardlessOfCallerOrder()
        {
            var first = Open(68UL);
            var second = Open(68UL);
            var a = first.policy.Open(first.ticket.Request, new RunActionContext(true), "exhausted", Array.Empty<RunBoons.Offer>(), Offers());
            var b = second.policy.Open(second.ticket.Request, new RunActionContext(true), "exhausted", Array.Empty<RunBoons.Offer>(),
                Offers().Reverse().Concat(Offers()).ToArray());
            Assert.That(a.IsAlternative, Is.True);
            Assert.That(b.IsAlternative, Is.True);
            Assert.That(a.Choices.Select(o => o.Id), Is.EqualTo(b.Choices.Select(o => o.Id)));
            Assert.That(a.Choices.Select(o => o.Id).Distinct().Count(), Is.EqualTo(a.Choices.Count));
        }

        private (ManualRoutePolicy route, RunRewardPolicy policy, RunRewardTicket ticket) Open(ulong seed, bool reverse = false)
        {
            var generated = ManualRunRandomizer.Generate(_source, seed, Array.Empty<GameObject>());
            var route = new ManualRoutePolicy(generated);
            var context = new RunActionContext(true);
            Assert.That(route.TryCommitDestination(route.Session.Id, 1, context), Is.True);
            Assert.That(route.TryEnterNode(route.Session.Id, 1, context), Is.True);
            Assert.That(route.TryResolveEncounter(route.Session.Id, 1, true, context), Is.True);
            var policy = new RunRewardPolicy(route);
            var offers = Offers();
            var ticket = policy.Open(new RunRewardRequest(route.Session.Id, 1, 0), context, "unchanged",
                reverse ? offers.Reverse().ToArray() : offers);
            Assert.That(ticket, Is.Not.Null);
            return (route, policy, ticket);
        }

        private static RunBoons.Offer[] Offers() => Enumerable.Range(0, 12)
            .Select(i => new RunBoons.Offer("seed_test_" + i, "Test " + i, "Isolated RNG fixture")).ToArray();
    }
}
