using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class RunMapDiscoveryPresentationTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _route;
        private ManualRoutePolicy _policy;
        private static RunActionContext Ready => new RunActionContext(true);

        [SetUp]
        public void SetUp()
        {
            _definition = FluidityRouteTestFactory.Create();
            ResetPolicy();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_definition);

        private void ResetPolicy()
        {
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out _route, out string error), error);
            _policy = new ManualRoutePolicy(_route);
        }

        private void Complete(int nodeId)
        {
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, nodeId, Ready));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, nodeId, Ready));
            if (_route.Node(nodeId).Kind == ManualNodeKind.Repair)
            {
                Assert.IsTrue(_policy.TryCompleteRepair(session.Id, nodeId, Ready));
                return;
            }
            Assert.IsTrue(_policy.TryResolveEncounter(session.Id, nodeId, true, Ready));
            for (int ordinal = 0; ordinal < _route.Node(nodeId).TotalAcquisitions; ordinal++)
                Assert.IsTrue(_policy.TryCompleteReward(session.Id, nodeId, ordinal,
                    _route.Node(nodeId).RewardAt(ordinal) == RunRewardKind.Mutation, Ready));
        }

        [Test]
        public void NewRunKnowsItsStartAndMasksEveryUnvisitedRoomIdentityAndEncounter()
        {
            var session = _policy.Session;
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(session, 0));
            Assert.AreEqual("S", RunMapDiscoveryPresentation.NodeLabel(session, 0));
            foreach (var node in _route.Nodes.Where(node => node.Id != 0))
            {
                Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, node.Id));
                Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, node.Id));
                Assert.AreEqual("SALA NÃO EXPLORADA", RunMapDiscoveryPresentation.Title(session, node.Id));
                Assert.AreEqual("?", RunMapDiscoveryPresentation.EncounterSummary(session, node.Id));
                StringAssert.DoesNotContain(node.Label, RunMapDiscoveryPresentation.Purpose(session, node.Id));
            }
        }

        [TestCase(RunNodeState.Locked)]
        [TestCase(RunNodeState.Available)]
        [TestCase(RunNodeState.Committed)]
        [TestCase(RunNodeState.Excluded)]
        public void LogisticsCannotRevealAnUnvisitedEliteEncounter(RunNodeState state)
        {
            var session = _policy.Session;
            session.SetNode(7, state);
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 7));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, 7));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.EncounterSummary(session, 7));
            Assert.AreEqual("2 Technique", RunMapDiscoveryPresentation.RewardSummary(session, 7));
            StringAssert.DoesNotContain("Elite", RunMapDiscoveryPresentation.StateLabel(session, 7));
            StringAssert.DoesNotContain("E1", RunMapDiscoveryPresentation.BlockingReason(session, 7));
        }

        [Test]
        public void FutureBenefitsRemainAnnouncedWithoutRevealingRoomIdentity()
        {
            var session = _policy.Session;
            Assert.AreEqual("Technique", RunMapDiscoveryPresentation.RewardSummary(session, 1));
            Assert.AreEqual("Mutation", RunMapDiscoveryPresentation.RewardSummary(session, 5));
            Assert.AreEqual("Protocol", RunMapDiscoveryPresentation.RewardSummary(session, 6));
            Assert.AreEqual("2 Technique", RunMapDiscoveryPresentation.RewardSummary(session, 7));
            Assert.AreEqual("Recuperação · até 35% da vida máxima", RunMapDiscoveryPresentation.RewardSummary(session, 3));
            Assert.AreEqual("Extração", RunMapDiscoveryPresentation.RewardSummary(session, 10));
            Assert.AreEqual("MUT", RunMapDiscoveryPresentation.RewardAbbreviation(session, 5));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, 5));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.EncounterSummary(session, 5));
        }

        [Test]
        public void EnteringRevealsTheRoomAndRewardPendingAndCompletionKeepItKnown()
        {
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, 1, Ready));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 1));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, 1, Ready));
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(session, 1));
            Assert.AreEqual("C1", RunMapDiscoveryPresentation.NodeLabel(session, 1));
            StringAssert.Contains("1 adversários", RunMapDiscoveryPresentation.EncounterSummary(session, 1));
            Assert.IsTrue(_policy.TryResolveEncounter(session.Id, 1, true, Ready));
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(session, 1));
            Assert.AreEqual("RECOMPENSA PENDENTE", RunMapDiscoveryPresentation.StateLabel(session, 1));
            Assert.IsTrue(_policy.TryCompleteReward(session.Id, 1, 0, false, Ready));
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(session, 1));
            Assert.AreEqual("VOCÊ ESTÁ AQUI", RunMapDiscoveryPresentation.StateLabel(session, 1));
            Complete(2);
            Assert.AreEqual("CONCLUÍDA", RunMapDiscoveryPresentation.StateLabel(session, 1));
        }

        [Test]
        public void BranchCommitDoesNotRevealChosenOrExcludedRoomBeforeEntry()
        {
            Complete(1);
            Complete(2);
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, 3, Ready));
            Assert.AreEqual("CAMINHO ESCOLHIDO", RunMapDiscoveryPresentation.StateLabel(session, 3));
            Assert.AreEqual("ROTA DESCARTADA", RunMapDiscoveryPresentation.StateLabel(session, 4));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 3));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 4));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, 3));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, 4));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, 3, Ready));
            Assert.AreEqual("RP", RunMapDiscoveryPresentation.NodeLabel(session, 3));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 4));
        }

        [Test]
        public void NextStepsAnnounceBenefitsWhileMaskingUnvisitedLabels()
        {
            Complete(1);
            Complete(2);
            var session = _policy.Session;
            string next = RunMapDiscoveryPresentation.NextSteps(session, 2);
            Assert.AreEqual("? · Recuperação · até 35% da vida máxima\n? · Technique", next);
            StringAssert.DoesNotContain("RP", next);
            StringAssert.DoesNotContain("C3", next);
            Complete(3);
            next = RunMapDiscoveryPresentation.NextSteps(session, 2);
            StringAssert.Contains("RP · Recuperação", next);
            StringAssert.DoesNotContain("C3", next);
        }

        [Test]
        public void OpportunityCostPreservesRewardQuantitiesWithoutNamingUnvisitedAlternative()
        {
            string cost = RunMapDiscoveryPresentation.OpportunityCost(_policy.Session, 6);
            Assert.AreEqual("Esta escolha descarta: ? (2 Technique) → ? (Protocol).", cost);
            StringAssert.DoesNotContain("E1", cost);
            StringAssert.DoesNotContain("E2", cost);
            StringAssert.DoesNotContain("Elite", cost);
        }

        [Test]
        public void AnnouncedBenefitsUseTheAuthoredSequenceRatherThanItsLegacyRewardField()
        {
            _definition.Nodes.Single(node => node.Id == 1).ConfigureRewardSteps(new[] {
                new ManualRewardStep(RunRewardKind.Technique, 1),
                new ManualRewardStep(RunRewardKind.Protocol, 2) });
            ResetPolicy();
            var session = _policy.Session;
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, 1));
            Assert.AreEqual("Technique + 2 Protocol", RunMapDiscoveryPresentation.RewardSummary(session, 1));
            Assert.AreEqual("TEC + 2 PRO", RunMapDiscoveryPresentation.RewardAbbreviation(session, 1));
        }

        [Test]
        public void ProjectionDoesNotCommitOrRevealRoomsOrChangeRewards()
        {
            var session = _policy.Session;
            var states = _route.Nodes.ToDictionary(node => node.Id, node => session.NodeState(node.Id));
            foreach (var node in _route.Nodes)
            {
                RunMapDiscoveryPresentation.NodeLabel(session, node.Id);
                RunMapDiscoveryPresentation.Title(session, node.Id);
                RunMapDiscoveryPresentation.RewardSummary(session, node.Id);
                RunMapDiscoveryPresentation.RewardAbbreviation(session, node.Id);
                RunMapDiscoveryPresentation.EncounterSummary(session, node.Id);
                RunMapDiscoveryPresentation.Purpose(session, node.Id);
                RunMapDiscoveryPresentation.OpportunityCost(session, node.Id);
                RunMapDiscoveryPresentation.NextSteps(session, node.Id);
                RunMapDiscoveryPresentation.StateLabel(session, node.Id);
                RunMapDiscoveryPresentation.BlockingReason(session, node.Id);
            }
            Assert.AreEqual(0, session.CurrentNode);
            Assert.AreEqual(-1, session.PendingDestination);
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
            Assert.AreEqual(0, session.InventoryRevision);
            Assert.AreEqual(0, session.MutationAcquisitions);
            Assert.IsFalse(session.TryBranch("A", out _));
            Assert.IsFalse(session.TryBranch("B", out _));
            foreach (var state in states) Assert.AreEqual(state.Value, session.NodeState(state.Key));
        }

        [Test]
        public void DiscoveryBelongsToItsSessionAndDoesNotCarryIntoAnotherRun()
        {
            Complete(1);
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(_policy.Session, 1));
            var nextRun = new ManualRoutePolicy(_route).Session;
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(nextRun, 1));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(nextRun, 1));
            Assert.AreEqual("Technique", RunMapDiscoveryPresentation.RewardSummary(nextRun, 1));
        }

        [Test]
        public void MissingSessionOrNodeDoesNotExposeOrThrow()
        {
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(null, 0));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(_policy.Session, -99));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(null, 0));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.RewardSummary(_policy.Session, -99));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.EncounterSummary(_policy.Session, -99));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NextSteps(null, 0));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.OpportunityCost(null, 0));
            Assert.AreEqual("INDISPONÍVEL", RunMapDiscoveryPresentation.StateLabel(null, 0));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsVisible(null));
            Assert.IsEmpty(RunMapDiscoveryPresentation.NodeLabel((Room)null));
        }

        [TestCase(RoomType.Combat)]
        [TestCase(RoomType.Boss)]
        [TestCase(RoomType.Treasure)]
        public void ProceduralNormalRoomsAreVisibleButStayAnonymousUntilVisited(RoomType type)
        {
            var room = MakeRoom(2, type);
            Assert.IsTrue(RunMapDiscoveryPresentation.IsVisible(room));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(room));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(room));
            Assert.AreEqual("NÃO EXPLORADA", RunMapDiscoveryPresentation.StateLabel(room));
            room.Visited = true;
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(room));
            Assert.AreNotEqual("?", RunMapDiscoveryPresentation.NodeLabel(room));
            Assert.AreEqual("VISITADA", RunMapDiscoveryPresentation.StateLabel(room));
            room.Cleared = true;
            Assert.AreEqual("CONCLUÍDA", RunMapDiscoveryPresentation.StateLabel(room));
        }

        [Test]
        public void ProceduralStartIsKnownAndClearedStateCanRevealWithoutVisitedFlag()
        {
            var start = MakeRoom(0, RoomType.Start);
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(start));
            Assert.AreEqual("INÍCIO", RunMapDiscoveryPresentation.NodeLabel(start));
            var cleared = MakeRoom(1, RoomType.Combat);
            cleared.Cleared = true;
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(cleared));
            Assert.AreEqual("CONCLUÍDA", RunMapDiscoveryPresentation.StateLabel(cleared));
        }

        [Test]
        public void UnrevealedSecretHasNoPlaceholderAndItsIncidentConnectionIsInvisible()
        {
            var secret = MakeRoom(3, RoomType.Secret);
            var adjacent = MakeRoom(2, RoomType.Combat);
            Assert.IsFalse(RunMapDiscoveryPresentation.IsVisible(secret));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(secret));
            Assert.IsEmpty(RunMapDiscoveryPresentation.NodeLabel(secret));
            Assert.IsEmpty(RunMapDiscoveryPresentation.StateLabel(secret));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsConnectionVisible(adjacent, secret));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsConnectionVisible(secret, adjacent));
            secret.Visited = true;
            secret.Cleared = true;
            Assert.IsFalse(RunMapDiscoveryPresentation.IsVisible(secret), "Revealed is the secret-discovery authority.");
        }

        [Test]
        public void RevealedSecretAppearsAnonymouslyAndIsIdentifiedOnlyAfterVisit()
        {
            var secret = MakeRoom(3, RoomType.Secret);
            var adjacent = MakeRoom(2, RoomType.Combat);
            secret.Revealed = true;
            Assert.IsTrue(RunMapDiscoveryPresentation.IsVisible(secret));
            Assert.IsTrue(RunMapDiscoveryPresentation.IsConnectionVisible(adjacent, secret));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(secret));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(secret));
            secret.Visited = true;
            Assert.AreEqual("SECRETA", RunMapDiscoveryPresentation.NodeLabel(secret));
        }

        private static Room MakeRoom(int id, RoomType type) =>
            new Room(id, type, new Vector2(id * 20f, 0f), new Vector2(16f, 16f), id);
    }
}
