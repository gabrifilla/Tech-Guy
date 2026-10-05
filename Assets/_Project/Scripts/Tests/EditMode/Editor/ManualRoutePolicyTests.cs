using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class ManualRoutePolicyTests
    {
        private ManualRunDefinition _definition;
        private ManualRouteSnapshot _route;
        private ManualRoutePolicy _policy;
        private static RunActionContext Ready => new RunActionContext(true);
        [SetUp] public void SetUp()
        {
            _definition = ManualRunTestFactory.Create();
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,
                Resources.Load<WeaponScript>("Weapons/Melee/Gauntlet/Gauntlet"), out _route, out string error), error);
            _policy = new ManualRoutePolicy(_route);
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(_definition); }
        private string State() => _policy.Session.Phase + ":" + _policy.Session.CurrentNode + ":" + _policy.Session.PendingDestination +
            ":" + _policy.Session.RewardOrdinal + ":" + _policy.Session.InventoryRevision + ":" + _policy.Session.MutationAcquisitions +
            ":" + _policy.Session.Outcome + ":" + string.Join(",", _route.Nodes.OrderBy(n => n.Id).Select(n => _policy.Session.NodeState(n.Id)));
        private void Enter(int node)
        {
            Assert.IsTrue(_policy.TryCommitDestination(_policy.Session.Id, node, Ready));
            Assert.IsTrue(_policy.TryEnterNode(_policy.Session.Id, node, Ready));
        }
        private void Complete(int node)
        {
            Enter(node);
            if (_route.Node(node).Kind == ManualNodeKind.Repair)
            { Assert.IsTrue(_policy.TryCompleteRepair(_policy.Session.Id, node, Ready)); return; }
            Assert.IsTrue(_policy.TryResolveEncounter(_policy.Session.Id, node, true, Ready));
            for (int ordinal = 0; ordinal < _route.Node(node).Acquisitions; ordinal++)
                Assert.IsTrue(_policy.TryCompleteReward(_policy.Session.Id, node, ordinal, _route.Node(node).Reward == RunRewardKind.Mutation, Ready));
        }

        [TestCase(true,true)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(false,false)]
        public void FourPathsPreserveExclusionsAtReconnectAndReachBossWithMutation(bool repair, bool elite)
        {
            Complete(1); Complete(2); Complete(repair ? 3 : 4); Complete(5);
            Complete(elite ? 7 : 6); if (elite) Complete(8); Complete(9);
            Assert.AreEqual(RunNodeState.Excluded, _policy.Session.NodeState(repair ? 4 : 3));
            Assert.AreEqual(RunNodeState.Excluded, _policy.Session.NodeState(elite ? 6 : 7));
            if (!elite) Assert.AreEqual(RunNodeState.Excluded, _policy.Session.NodeState(8));
            Assert.That(_policy.AvailableDestinations, Is.EqualTo(new[]{10}));
            Enter(10); Assert.IsTrue(_policy.TryResolveEncounter(_policy.Session.Id, 10, true, Ready));
            Assert.IsTrue(_policy.TryFinalizeBoss(_policy.Session.Id, Ready));
            Assert.AreEqual(RunPhase.ExtractionAvailable, _policy.Session.Phase);
            Assert.AreEqual(1, _policy.Session.MutationAcquisitions);
        }

        [TestCase("dead")] [TestCase("pause")] [TestCase("hitstop")] [TestCase("cast")] [TestCase("dash")] [TestCase("cleanup")]
        public void RejectedConfirmationDoesNotCommitOrQueueIntent(string reason)
        {
            var context = new RunActionContext(reason != "dead", reason == "pause", reason == "hitstop",
                reason == "cast", reason == "dash", reason == "cleanup");
            string before = State();
            Assert.IsFalse(_policy.TryCommitDestination(_policy.Session.Id, 1, context)); Assert.AreEqual(before, State());
            Assert.IsTrue(_policy.TryCommitDestination(_policy.Session.Id, 1, Ready));
        }

        [Test] public void DuplicateAndOutOfOrderEntryCannotMoveTheLogicalFrontier()
        {
            string initial = State();
            Assert.IsFalse(_policy.TryCommitDestination(_policy.Session.Id, 10, Ready)); Assert.AreEqual(initial, State());
            Assert.IsFalse(_policy.TryEnterNode(_policy.Session.Id, 1, Ready)); Assert.AreEqual(initial, State());
            Assert.IsTrue(_policy.TryCommitDestination(_policy.Session.Id, 1, Ready)); string committed = State();
            Assert.IsFalse(_policy.TryCommitDestination(_policy.Session.Id, 1, Ready)); Assert.AreEqual(committed, State());
            Assert.IsFalse(_policy.TryEnterNode(_policy.Session.Id, 2, Ready)); Assert.AreEqual(committed, State());
            Assert.IsTrue(_policy.TryEnterNode(_policy.Session.Id, 1, Ready)); string entered = State();
            Assert.IsFalse(_policy.TryEnterNode(_policy.Session.Id, 1, Ready)); Assert.AreEqual(entered, State());
            Assert.IsFalse(_policy.TryResolveEncounter(_policy.Session.Id, 1, false, Ready)); Assert.AreEqual(entered, State());
        }

        [Test] public void BranchRejectedDuringEncounterIsNotCommittedAndCompletedNodesCannotBeReentered()
        {
            Complete(1); Enter(2); string before = State();
            Assert.IsFalse(_policy.TryCommitDestination(_policy.Session.Id, 3, Ready)); Assert.AreEqual(before, State());
            Assert.IsFalse(_policy.Session.TryBranch("A", out _));
            Assert.IsTrue(_policy.TryResolveEncounter(_policy.Session.Id, 2, true, Ready));
            Assert.IsTrue(_policy.TryCompleteReward(_policy.Session.Id, 2, 0, false, Ready));
            before = State(); Assert.IsFalse(_policy.TryCommitDestination(_policy.Session.Id, 1, Ready)); Assert.AreEqual(before, State());
        }

        [Test] public void RewardOrdinalsAreUniqueAndFirstMutationCannotResolveAsFallback()
        {
            Complete(1); Complete(2); Complete(3); Enter(5);
            Assert.IsTrue(_policy.TryResolveEncounter(_policy.Session.Id, 5, true, Ready)); string before = State();
            Assert.IsFalse(_policy.TryCompleteReward(_policy.Session.Id, 5, 0, false, Ready)); Assert.AreEqual(before, State());
            Assert.IsTrue(_policy.TryCompleteReward(_policy.Session.Id, 5, 0, true, Ready));
            before = State(); Assert.IsFalse(_policy.TryCompleteReward(_policy.Session.Id, 5, 0, true, Ready)); Assert.AreEqual(before, State());
            Enter(7); Assert.IsTrue(_policy.TryResolveEncounter(_policy.Session.Id, 7, true, Ready));
            Assert.IsTrue(_policy.TryCompleteReward(_policy.Session.Id, 7, 0, false, Ready)); before = State();
            Assert.IsFalse(_policy.TryCompleteReward(_policy.Session.Id, 7, 0, false, Ready)); Assert.AreEqual(before, State());
            Assert.AreEqual(RunPhase.RewardPending, _policy.Session.Phase);
            Assert.IsTrue(_policy.TryCompleteReward(_policy.Session.Id, 7, 1, false, Ready));
            Assert.AreEqual(RunPhase.RouteChoice, _policy.Session.Phase);
        }

        [Test] public void ClosedAndOldSessionsCannotContributeToANewRun()
        {
            Guid old = _policy.Session.Id;
            Assert.IsTrue(_policy.TryEndRun(old, RunOutcome.Aborted, Ready)); string before = State();
            Assert.IsFalse(_policy.TryCommitDestination(old, 1, Ready)); Assert.AreEqual(before, State());
            Assert.IsFalse(_policy.TryEndRun(old, RunOutcome.Dead, new RunActionContext(false)));
            _policy = new ManualRoutePolicy(_route); Assert.AreNotEqual(old, _policy.Session.Id);
            before = State(); Assert.IsFalse(_policy.TryCommitDestination(old, 1, Ready)); Assert.AreEqual(before, State());
            Assert.AreEqual(0, _policy.Session.MutationAcquisitions); Assert.AreEqual(0, _policy.Session.InventoryRevision);
        }

        [Test] public void DeathWinsFinalResolutionAndExtractionRequiresAliveCommitExactlyOnce()
        {
            foreach (int node in new[]{1,2,3,5,6,9,10}) Complete(node);
            Assert.IsTrue(_policy.TryFinalizeBoss(_policy.Session.Id, new RunActionContext(false)));
            Assert.AreEqual(RunOutcome.Dead, _policy.Session.Outcome);
            Assert.IsFalse(_policy.TryRequestExtraction(_policy.Session.Id, Ready));
            _policy = new ManualRoutePolicy(_route);
            foreach (int node in new[]{1,2,4,5,7,8,9,10}) Complete(node);
            Assert.IsTrue(_policy.TryFinalizeBoss(_policy.Session.Id, Ready)); string before = State();
            Assert.IsFalse(_policy.TryRequestExtraction(_policy.Session.Id, new RunActionContext(false))); Assert.AreEqual(before, State());
            Assert.IsTrue(_policy.TryRequestExtraction(_policy.Session.Id, Ready));
            Assert.AreEqual(RunOutcome.Extracted, _policy.Session.Outcome);
            Assert.IsFalse(_policy.TryRequestExtraction(_policy.Session.Id, Ready));
            Assert.IsFalse(_policy.TryEndRun(_policy.Session.Id, RunOutcome.Dead, new RunActionContext(false)));
        }
    }
}
