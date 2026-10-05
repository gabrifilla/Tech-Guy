using System;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class RunRepairPolicyTests
    {
        private ManualRunDefinition _definition; private ManualRoutePolicy _route; private RunRepairPolicy _repair;
        private static RunActionContext Ready=>new RunActionContext(true);
        [SetUp] public void SetUp()
        {
            _definition=ManualRunTestFactory.Create();
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition,Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]),out var snapshot,out string error),error);
            _route=new ManualRoutePolicy(snapshot); _repair=new RunRepairPolicy(_route);
            foreach(int node in new[]{1,2})
            {
                Assert.IsTrue(_route.TryCommitDestination(_route.Session.Id,node,Ready)); Assert.IsTrue(_route.TryEnterNode(_route.Session.Id,node,Ready));
                Assert.IsTrue(_route.TryResolveEncounter(_route.Session.Id,node,true,Ready)); Assert.IsTrue(_route.TryCompleteReward(_route.Session.Id,node,0,false,Ready));
            }
            Assert.IsTrue(_route.TryCommitDestination(_route.Session.Id,3,Ready)); Assert.IsTrue(_route.TryEnterNode(_route.Session.Id,3,Ready));
        }
        [TearDown] public void TearDown()=>UnityEngine.Object.DestroyImmediate(_definition);
        [TestCase(20f,100f,35f)] [TestCase(90f,100f,10f)] [TestCase(0f,100f,35f)] [TestCase(350f,600f,210f)]
        public void HealUsesCurrentMaximumAndClampsToDeficit(float health,float max,float expected)
        { Assert.IsTrue(RunResourceMath.TryRepair(health,max,.35f,out float amount)); Assert.AreEqual(expected,amount,.0001f); }
        [TestCase(float.NaN,100f,.35f)] [TestCase(20f,float.PositiveInfinity,.35f)] [TestCase(20f,100f,float.NaN)]
        [TestCase(20f,100f,0f)] [TestCase(20f,100f,1.1f)] [TestCase(-1f,100f,.35f)] [TestCase(101f,100f,.35f)]
        [TestCase(0f,0f,.35f)] [TestCase(100f,100f,.35f)]
        public void InvalidOrFullResourcesDoNotProduceHeal(float health,float max,float fraction)
        { Assert.IsFalse(RunResourceMath.TryRepair(health,max,fraction,out _)); }
        [Test] public void ClaimPrecedesHealNotificationAndDuplicateCannotHealAgain()
        {
            int calls=0;
            Assert.IsTrue(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>Ready,amount=>
            {
                calls++; Assert.AreEqual(RunRepairState.Committing,_repair.Result.State);
                Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>Ready,_=>calls++));
            }));
            Assert.AreEqual(1,calls); Assert.AreEqual(RunPhase.RouteChoice,_route.Session.Phase); Assert.IsTrue(_repair.Result.Used);
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>Ready,_=>calls++)); Assert.AreEqual(1,calls);
        }
        [Test] public void FullHealthRejectsUseButSkipConsumesOpportunityWithoutHealing()
        {
            int calls=0;
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,100,100,()=>Ready,_=>calls++)); Assert.IsNull(_repair.Result);
            Assert.IsTrue(_repair.TryResolve(_route.Session.Id,3,false,100,100,()=>Ready,_=>calls++));
            Assert.AreEqual(0,calls); Assert.IsFalse(_repair.Result.Used); Assert.AreEqual(0,_repair.Result.Amount);
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,1,100,()=>Ready,_=>calls++));
        }
        [Test] public void RejectedStateDoesNotClaimAndDeathDuringHealingFaultsWithoutRetry()
        {
            Assert.IsFalse(_repair.TryResolve(Guid.NewGuid(),3,true,20,100,()=>Ready,_=>Assert.Fail()));
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>new RunActionContext(true,paused:true),_=>Assert.Fail()));
            Assert.IsNull(_repair.Result); bool alive=true;
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>new RunActionContext(alive),_=>alive=false));
            Assert.AreEqual(RunRepairState.Faulted,_repair.Result.State); Assert.AreEqual(RunPhase.RepairPending,_route.Session.Phase);
            Assert.IsFalse(_repair.TryResolve(_route.Session.Id,3,true,20,100,()=>Ready,_=>Assert.Fail()));
        }
    }
}
