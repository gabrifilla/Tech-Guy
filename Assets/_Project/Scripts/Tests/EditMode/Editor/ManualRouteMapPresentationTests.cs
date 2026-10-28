using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    public sealed class ManualRouteMapPresentationTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _route;
        private ManualRoutePolicy _policy;
        private static RunActionContext Ready => new RunActionContext(true);

        [SetUp] public void SetUp()
        {
            _definition = FluidityRouteTestFactory.Create();
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out _route, out string error), error);
            _policy = new ManualRoutePolicy(_route);
        }
        [TearDown] public void TearDown() { Object.DestroyImmediate(_definition); }

        private void Complete(int id)
        {
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, id, Ready));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, id, Ready));
            if (_route.Node(id).Kind == ManualNodeKind.Repair)
            { Assert.IsTrue(_policy.TryCompleteRepair(session.Id, id, Ready)); return; }
            Assert.IsTrue(_policy.TryResolveEncounter(session.Id, id, true, Ready));
            for (int ordinal = 0; ordinal < _route.Node(id).TotalAcquisitions; ordinal++)
                Assert.IsTrue(_policy.TryCompleteReward(session.Id, id, ordinal,
                    _route.Node(id).RewardAt(ordinal) == RunRewardKind.Mutation, Ready));
        }

        [Test] public void CompleteGraphIsVisibleAndAllConnectionsAdvanceTowardBoss()
        {
            var layout = new ManualRouteMapLayout(_route);
            Assert.AreEqual(_route.Nodes.Count, layout.OrderedNodes.Distinct().Count());
            Assert.AreEqual(_route.StartNode, layout.OrderedNodes.First());
            Assert.AreEqual(_route.BossNode, layout.OrderedNodes.Last());
            foreach (var node in _route.Nodes)
            {
                Assert.That(layout.Point(node.Id).x, Is.InRange(0f, 1f));
                Assert.That(layout.Point(node.Id).y, Is.InRange(.1f, .9f));
            }
            foreach (var edge in _route.Connections)
                Assert.Greater(layout.Point(edge.To).x, layout.Point(edge.From).x, edge.From + " → " + edge.To);
        }

        [Test] public void UnequalBranchLengthsKeepEliteFollowupOnItsLaneAndReconnectAfterBothBranches()
        {
            var layout = new ManualRouteMapLayout(_route);
            Assert.AreNotEqual(layout.Point(3).y, layout.Point(4).y);
            Assert.AreNotEqual(layout.Point(6).y, layout.Point(7).y);
            Assert.AreEqual(layout.Point(7).y, layout.Point(8).y);
            Assert.Greater(layout.Point(9).x, layout.Point(6).x);
            Assert.Greater(layout.Point(9).x, layout.Point(8).x);
            Assert.AreEqual(.5f, layout.Point(9).y);
        }

        [Test] public void MovingAuthoredWorldCentersCannotCollapseTheDecisionMap()
        {
            var before = new ManualRouteMapLayout(_route);
            foreach (var node in _definition.Nodes) FluidityRouteTestFactory.Set(node, "_center", Vector3.zero);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]), out var moved, out string error), error);
            var after = new ManualRouteMapLayout(moved);
            foreach (var node in _route.Nodes) Assert.AreEqual(before.Point(node.Id), after.Point(node.Id));
        }

        [TestCase(1280, 720)] [TestCase(1920, 1080)]
        public void NodesAndTheirLabelsDoNotOverlapAtSupportedResolutions(int width, int height)
        {
            var layout = new ManualRouteMapLayout(_route);
            float scale = Mathf.Min(width / 1280f, height / 720f);
            var occupied = _route.Nodes.Select(node => {
                Vector2 point = layout.Point(node.Id);
                // Visible footprint includes the node, current marker and reward labels.
                return new Rect((102 + point.x * 734 - 45) * scale,
                    (215 + point.y * 305 - 59) * scale, 90 * scale, 132 * scale);
            }).ToArray();
            for (int i = 0; i < occupied.Length; i++)
            {
                Assert.GreaterOrEqual(occupied[i].xMin, 0f);
                Assert.LessOrEqual(occupied[i].xMax, width);
                Assert.GreaterOrEqual(occupied[i].yMin, 0f);
                Assert.LessOrEqual(occupied[i].yMax, height);
                for (int j = i + 1; j < occupied.Length; j++)
                    Assert.IsFalse(occupied[i].Overlaps(occupied[j]), "Map labels overlap for nodes " + i + "/" + j);
            }
        }

        [Test] public void SeeingFutureDestinationsDoesNotMakeThemSelectableOrChangeTheSession()
        {
            var session = _policy.Session;
            int current = session.CurrentNode;
            foreach (var node in _route.Nodes)
            {
                ManualRouteMapPresentation.RewardSummary(node);
                ManualRouteMapPresentation.BlockingReason(session, node.Id);
                ManualRouteMapPresentation.NextSteps(session, node.Id);
                Assert.AreEqual(node.Id == 1, ManualRouteMapPresentation.CanSelectDestination(session, node.Id));
            }
            Assert.AreEqual(current, session.CurrentNode);
            Assert.AreEqual(-1, session.PendingDestination);
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
            Assert.AreEqual(0, session.InventoryRevision);
        }

        [Test] public void ConfirmedChoiceMarksItsDestinationAndPermanentlyDiscardsAlternativeAtReconnect()
        {
            Complete(1); Complete(2);
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(_policy.Session, 3));
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(_policy.Session, 4));
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, 3, Ready));
            Assert.AreEqual("CAMINHO ESCOLHIDO", ManualRouteMapPresentation.StateLabel(session, 3));
            Assert.AreEqual("ROTA DESCARTADA", ManualRouteMapPresentation.StateLabel(session, 4));
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 4));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, 3, Ready));
            Assert.IsTrue(_policy.TryCompleteRepair(session.Id, 3, Ready));
            Complete(5);
            Assert.AreEqual("ROTA DESCARTADA", ManualRouteMapPresentation.StateLabel(session, 4));
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(session, 6));
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(session, 7));
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 8));
        }

        [Test] public void PendingRewardAndCombatCannotEnableConfirmation()
        {
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, 1, Ready));
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 1));
            Assert.IsTrue(_policy.TryEnterNode(session.Id, 1, Ready));
            Assert.AreEqual("EM COMBATE", ManualRouteMapPresentation.StateLabel(session, 1));
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 2));
            Assert.IsTrue(_policy.TryResolveEncounter(session.Id, 1, true, Ready));
            Assert.AreEqual("RECOMPENSA PENDENTE", ManualRouteMapPresentation.StateLabel(session, 1));
            StringAssert.Contains("recompensa pendente", ManualRouteMapPresentation.BlockingReason(session, 2));
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 2));
        }

        [Test] public void RewardSummaryUsesExactAuthoredSequenceRatherThanLegacySingleReward()
        {
            var node = _definition.Nodes.Single(n => n.Id == 1);
            node.ConfigureRewardSteps(new[] { new ManualRewardStep(RunRewardKind.Technique, 1), new ManualRewardStep(RunRewardKind.Protocol, 2) });
            Assert.AreEqual("Technique + 2 Protocol", ManualRouteMapPresentation.RewardSummary(node));
            Assert.AreEqual("TEC + 2 PRO", ManualRouteMapPresentation.RewardAbbreviation(node));
        }

        [Test] public void CostAnnouncesWholeExcludedBranchIncludingItsExtraEncounter()
        {
            Assert.That(ManualRouteMapPresentation.ExcludedByChoice(_route, 6), Is.EqualTo(new[] { 7, 8 }));
            string cost = ManualRouteMapPresentation.OpportunityCost(_route, 6);
            StringAssert.Contains("E1 (2 Technique)", cost);
            StringAssert.Contains("E2 (Protocol)", cost);
            Assert.IsEmpty(ManualRouteMapPresentation.ExcludedByChoice(_route, 9));
        }

        [Test] public void ClosedRunNeverOffersConfirmationEvenWhenANodeWasAvailable()
        {
            var session = _policy.Session;
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(session, 1));
            Assert.IsTrue(_policy.TryEndRun(session.Id, RunOutcome.Aborted, Ready));
            Assert.AreEqual(RunNodeState.Available, session.NodeState(1));
            foreach (var node in _route.Nodes)
                Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, node.Id));
            StringAssert.Contains("encerrada", ManualRouteMapPresentation.BlockingReason(session, 1));
        }

        [Test] public void BossBecomesSelectableOnlyThroughResolvedRouteAndMutationAcquisition()
        {
            var session = _policy.Session;
            Assert.IsFalse(ManualRouteMapPresentation.CanSelectDestination(session, 10));
            StringAssert.Contains("Mutation", ManualRouteMapPresentation.BlockingReason(session, 10));
            foreach (int id in new[] { 1, 2, 3, 5, 6, 9 }) Complete(id);
            Assert.AreEqual(1, session.MutationAcquisitions);
            Assert.IsTrue(ManualRouteMapPresentation.CanSelectDestination(session, 10));
            Assert.AreEqual("PRÓXIMO DESTINO", ManualRouteMapPresentation.StateLabel(session, 10));
        }
    }
}
