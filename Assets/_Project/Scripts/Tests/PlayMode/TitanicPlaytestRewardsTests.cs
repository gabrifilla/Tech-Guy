using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest] public IEnumerator TitanicPlaytestFirstRoomGuaranteesBothThroughRealTicketsAndEveryRoomHasMutation()
        {
            bool previous = UnityEditor.SessionState.GetBool(TitanicConductorPlaytestRewards.SessionKey, false);
            try
            {
                yield return PrepareTitanic(false);
                var original = BuildManualRewardRoute().Session.Route;
                UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, true);
                var assisted = TitanicConductorPlaytestRewards.Apply(original, _titanRun.RunWeapon);
                Assert.AreEqual(RunRewardKind.Technique, original.Node(1).RewardAt(0));
                foreach (var node in assisted.Nodes.Where(n => n.Kind == ManualNodeKind.Combat || n.Kind == ManualNodeKind.Elite))
                { Assert.AreEqual(RunRewardKind.Mutation, node.Reward); Assert.AreEqual(2, node.TotalAcquisitions); Assert.AreEqual(RunRewardKind.Mutation, node.RewardAt(0)); Assert.AreEqual(RunRewardKind.Protocol, node.RewardAt(1)); }
                Assert.AreEqual(original.Node(original.BossNode).TotalAcquisitions, assisted.Node(assisted.BossNode).TotalAcquisitions);
                UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, false); // Captured run stays consistent.
                var route = new ManualRoutePolicy(assisted); Assert.IsTrue(_titanRun.BindManualRun(route));
                ReachManualReward(route, 1);
                foreach (var wanted in new[] { TitanicFistsDefinition.Id, "conductor" })
                {
                    Assert.IsTrue(_titanRun.OfferReward(new RunRewardRequest(route.Session.Id, 1, route.Session.RewardOrdinal)));
                    yield return null; yield return null;
                    Assert.AreEqual(wanted, _titanRun.Choices.Single().Id); Assert.IsTrue(_titanRun.Choose(0)); Assert.IsFalse(_titanRun.Choose(0));
                }
                Assert.AreEqual(RunPhase.RouteChoice, route.Session.Phase);
                Assert.AreEqual(1, route.Session.MutationAcquisitions); Assert.AreEqual(1, route.Session.ProtocolAcquisitions);
                Assert.AreEqual(TitanicFistsDefinition.Id, _titanRun.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
                Assert.AreEqual(1, _titanRun.Protocols.Rank(ProtocolKind.Conductor));
                ReachManualReward(route, 2);
                Assert.IsTrue(_titanRun.OfferReward(new RunRewardRequest(route.Session.Id, 2, 0)));
                yield return null; yield return null;
                Assert.AreEqual(RunRewardKind.Mutation, _titanRun.ManualTicket.AnnouncedKind);
                Assert.IsFalse(_titanRun.Choices.Any(x => x.Id == TitanicFistsDefinition.Id), "Acquired mutation never duplicated.");
                Assert.Greater(_titanRun.Choices.Count, 0); Assert.IsTrue(_titanRun.Choose(0));
                Assert.AreEqual(2, route.Session.MutationAcquisitions);
            }
            finally { UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, previous); }
        }

        [UnityTest] public IEnumerator TitanicPlaytestDisabledOrOtherWeaponKeepsNormalRewards()
        {
            bool previous = UnityEditor.SessionState.GetBool(TitanicConductorPlaytestRewards.SessionKey, false);
            try
            {
                yield return PrepareTitanic(false); var source = BuildManualRewardRoute().Session.Route;
                UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, false);
                Assert.AreSame(source, TitanicConductorPlaytestRewards.Apply(source, _titanRun.RunWeapon));
                UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, true);
                Assert.AreSame(source, TitanicConductorPlaytestRewards.Apply(source, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1])));
                Assert.AreSame(source, TitanicConductorPlaytestRewards.Apply(source, Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2])));
            }
            finally { UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, previous); }
        }
    }

    public sealed class TitanicPlaytestProductionRewardsTests : RunSummaryIsolatedPlayModeFixture
    {
        [UnityTest] public IEnumerator FirstSectorFirstClearOffersBothWithAssistanceEnabled()
        {
            bool previous = UnityEditor.SessionState.GetBool(TitanicConductorPlaytestRewards.SessionKey, false);
            try
            {
                UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, true);
                yield return UnityEngine.SceneManagement.SceneManager.LoadSceneAsync("FirstSector");
                yield return null; yield return null; yield return null;
                var director = Object.FindFirstObjectByType<ProgressionDirector>();
                var player = Object.FindFirstObjectByType<PlayerActor>(); var run = player.GetComponent<RunBoons>();
                var session = director.ManualSession;
                Assert.IsTrue(session.Route.TitanicConductorPlaytest);
                Assert.IsNull(run.WeaponModifiers.Expansion.MutationId(BoonSlot.Basic));
                Assert.AreEqual(0, run.Protocols.Rank(ProtocolKind.Conductor));
                player.AddDamageImmunity(director);
                Assert.IsTrue(director.TryCommitDestination(session.Id, 1));
                Assert.IsTrue(player.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(session.Route.Node(1).Center));
                Assert.IsTrue(director.TryEnterNode(session.Id, 1)); yield return null;
                foreach (var enemy in director.GetComponentsInChildren<Actor>())
                {
                    if (enemy.TryGetComponent<CoinDrop>(out var coins)) Object.DestroyImmediate(coins);
                    enemy.TakeDamage(1000000);
                }
                float deadline = Time.realtimeSinceStartup + 5;
                while (session.Phase == RunPhase.Encounter || player.IsCombatHitStopped)
                { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
                foreach (var wanted in new[] { TitanicFistsDefinition.Id, "conductor" })
                {
                    Assert.IsTrue(director.TryOpenManualReward(session.Id, 1, session.RewardOrdinal));
                    yield return null; yield return null;
                    Assert.AreEqual(wanted, run.Choices.Single().Id); Assert.IsTrue(run.Choose(0));
                }
                Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
                Assert.AreEqual(1, session.MutationAcquisitions); Assert.AreEqual(1, session.ProtocolAcquisitions);
                Assert.AreEqual(1, run.Protocols.Rank(ProtocolKind.Conductor));
            }
            finally { UnityEditor.SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey, previous); }
        }
    }
}
