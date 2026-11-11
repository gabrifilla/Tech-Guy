using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.Fluidity.EditMode
{
    // Route: S0 → C1 → C2 → {RP3 | C4} → C5(Mut) → {C6 | E7 → E8} → C9 → B10.
    public sealed class RouteStripModelTests
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

        [Test] public void ForkShowsBothBranchesOnSeparateLanesAndWhereTheyReconnect()
        {
            Complete(1); Complete(2);
            var strip = new RouteStripModel(_policy.Session);
            Assert.AreEqual(2, strip.Anchor);
            Assert.AreEqual(new Vector2Int(0, 0), strip.Cell(2));
            Assert.AreEqual(new Vector2Int(1, -1), strip.Cell(3));
            Assert.AreEqual(new Vector2Int(1, 1), strip.Cell(4));
            Assert.AreEqual(new Vector2Int(2, 0), strip.Cell(5));
            // The next fork is already visible two rooms ahead.
            Assert.AreEqual(new Vector2Int(3, -1), strip.Cell(6));
            Assert.AreEqual(new Vector2Int(3, 1), strip.Cell(7));
            Assert.IsFalse(strip.Contains(8), "Horizon is three rooms");
        }

        [Test] public void UnequalBranchesPlaceReconnectionAfterTheLongerBranch()
        {
            foreach (int id in new[] { 1, 2, 3, 5 }) Complete(id);
            var strip = new RouteStripModel(_policy.Session);
            Assert.AreEqual(new Vector2Int(1, -1), strip.Cell(6));
            Assert.AreEqual(new Vector2Int(1, 1), strip.Cell(7));
            Assert.AreEqual(new Vector2Int(2, 1), strip.Cell(8));
            Assert.AreEqual(new Vector2Int(3, 0), strip.Cell(9));
            foreach (var edge in strip.Edges)
                Assert.Greater(strip.Cell(edge.To).x, strip.Cell(edge.From).x, edge.From + " → " + edge.To);
        }

        [Test] public void ChosenPathCollapsesToOneLaneAndHidesTheDiscardedBranch()
        {
            Complete(1); Complete(2);
            var session = _policy.Session;
            Assert.IsTrue(_policy.TryCommitDestination(session.Id, 4, Ready));
            var strip = new RouteStripModel(session);
            Assert.IsFalse(strip.Contains(3));
            Assert.AreEqual(new Vector2Int(1, 0), strip.Cell(4));
            Assert.IsFalse(strip.Edges.Any(e => e.To == 3 || e.From == 3));
        }

        [Test] public void StripStopsAtTheBossAndNeverChangesTheSession()
        {
            foreach (int id in new[] { 1, 2, 3, 5, 6, 9 }) Complete(id);
            var session = _policy.Session;
            int current = session.CurrentNode, revision = session.InventoryRevision;
            var strip = new RouteStripModel(session);
            Assert.That(strip.Nodes, Is.EqualTo(new[] { 9, 10 }));
            Assert.AreEqual(current, session.CurrentNode);
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
            Assert.AreEqual(revision, session.InventoryRevision);
        }

        [Test] public void OptionNumbersMatchTheRouteChoiceKeys()
        {
            Complete(1); Complete(2);
            var destinations = _policy.AvailableDestinations;
            Assert.AreEqual(1, RouteStripModel.OptionNumber(destinations, 3));
            Assert.AreEqual(2, RouteStripModel.OptionNumber(destinations, 4));
            Assert.AreEqual(0, RouteStripModel.OptionNumber(destinations, 5));
        }
    }
}
