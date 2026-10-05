using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace TechGuy.Tests.EditMode
{
    public sealed class RunRewardPolicyTests
    {
        private ManualRunDefinition _definition;
        private ManualRoutePolicy _route;
        private RunRewardPolicy _rewards;
        private WeaponScript _weapon;
        private WeaponRunModifiers _mods;
        private static RunActionContext Ready => new RunActionContext(true);
        [SetUp] public void SetUp()
        {
            _definition = ManualRunTestFactory.Create(); _weapon = Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(_definition, _weapon, out var snapshot, out string error), error);
            _route = new ManualRoutePolicy(snapshot); _rewards = new RunRewardPolicy(_route);
            _mods = new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
        }
        [TearDown] public void TearDown() { UnityEngine.Object.DestroyImmediate(_definition); }
        private void EnterReward(int id)
        {
            Assert.IsTrue(_route.TryCommitDestination(_route.Session.Id,id,Ready));
            Assert.IsTrue(_route.TryEnterNode(_route.Session.Id,id,Ready));
            Assert.IsTrue(_route.TryResolveEncounter(_route.Session.Id,id,true,Ready));
        }
        private RunRewardRequest Request => new RunRewardRequest(_route.Session.Id,_route.Session.CurrentNode,_route.Session.RewardOrdinal);
        private RunRewardTicket Open(string key = "inventory") => _rewards.Open(Request,Ready,key,
            ManualRewardCatalog.Eligible(_route.Session.Route.Node(Request.NodeId).Reward,_mods,_weapon));
        private void Complete(int id, bool mutation = false)
        {
            EnterReward(id);
            Assert.IsTrue(_route.TryCompleteReward(_route.Session.Id,id,0,mutation,Ready));
        }

        [Test] public void CatalogUsesTenRankLimitedTechniquesFourMutationsAndReachableProtocols()
        {
            Assert.AreEqual(10,ManualRewardCatalog.Eligible(RunRewardKind.Technique,_mods,_weapon).Count);
            Assert.AreEqual(4,ManualRewardCatalog.Eligible(RunRewardKind.Mutation,_mods,_weapon).Count);
            Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Protocol,_mods,_weapon).Select(o=>o.Id),
                Is.EquivalentTo(new[]{"ignite","frost","conductor","detonation"}));
            foreach(var d in WeaponRunModifiers.Catalog.Where(d=>GauntletContentAuthoring.IsSelectedTechnique(d.Kind)))
                for(int i=0;i<d.MaxRank;i++) Assert.IsTrue(_mods.Add(d));
            Assert.IsEmpty(ManualRewardCatalog.Eligible(RunRewardKind.Technique,_mods,_weapon));
            _mods.GauntletMutations.Select(GauntletMutation.Epicenter);
            Assert.AreEqual(3,ManualRewardCatalog.Eligible(RunRewardKind.Mutation,_mods,_weapon).Count);
            var imported=new WeaponRunModifiers(RunWeaponFamily.Gauntlet);
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.ShockRing),out var alias));
            Assert.IsTrue(imported.Add(alias));
            Assert.IsFalse(ManualRewardCatalog.Eligible(RunRewardKind.Mutation,imported,_weapon).Any(o=>o.Id==GauntletContentAuthoring.ForMutation(GauntletMutation.Epicenter).Id));
        }

        [Test] public void ReopenFreezesCardsAndInventoryRevisionRebuildsWithoutSelectingStaleCard()
        {
            EnterReward(1); var first=Open(); var ids=first.Choices.Select(o=>o.Id).ToArray();
            Assert.LessOrEqual(ids.Length,3); Assert.AreEqual(ids.Length,ids.Distinct().Count());
            for(int i=0;i<10;i++) { Assert.AreSame(first,Open()); Assert.That(first.Choices.Select(o=>o.Id),Is.EqualTo(ids)); }
            var revised=Open("new inventory"); Assert.AreEqual(1,revised.Revision);
            int applied=0;
            Assert.IsFalse(_rewards.TryChoose(Request,0,ids[0],Ready,()=>true,()=>{applied++;return true;},null));
            Assert.AreEqual(0,applied); Assert.AreEqual(RunPhase.RewardPending,_route.Session.Phase);
        }

        [Test] public void ClaimPrecedesEffectsAndPublicationAndRejectsReentrantAndDuplicateSelection()
        {
            EnterReward(1); var ticket=Open(); var request=Request; string id=ticket.Choices[0].Id; int applied=0,published=0;
            Assert.IsTrue(_rewards.TryChoose(request,0,id,Ready,()=>true,()=>
            {
                applied++; Assert.AreEqual(RunRewardTicketState.Committing,ticket.State);
                Assert.IsFalse(_rewards.TryChoose(request,0,id,Ready,()=>true,()=>true,null)); return true;
            },resolved=>
            {
                published++; Assert.AreEqual(RunRewardTicketState.Resolved,resolved.State);
                Assert.IsFalse(_rewards.TryChoose(request,0,id,Ready,()=>true,()=>true,null));
            }));
            Assert.AreEqual(1,applied); Assert.AreEqual(1,published); Assert.AreEqual(RunPhase.RouteChoice,_route.Session.Phase);
        }

        [Test] public void PartialExceptionFaultsTicketAndRefusesBlindRetry()
        {
            EnterReward(1); var ticket=Open(); var request=Request; int effects=0;
            Assert.IsFalse(_rewards.TryChoose(request,0,ticket.Choices[0].Id,Ready,()=>true,()=>
            { effects++; throw new InvalidOperationException("after partial effect"); },null));
            Assert.AreEqual(RunRewardTicketState.Faulted,ticket.State); StringAssert.Contains("partial effect",ticket.Diagnostic);
            Assert.IsNull(Open()); Assert.AreEqual(1,effects); Assert.AreEqual(RunPhase.RewardPending,_route.Session.Phase);
        }

        [Test] public void ExhaustedTechniquesOfferDefenseButOccupiedFirstMutationDoesNotForgeGuarantee()
        {
            EnterReward(1);
            var defense=_rewards.Open(Request,Ready,"empty",Array.Empty<RunBoons.Offer>());
            Assert.IsTrue(defense.IsAlternative); Assert.AreEqual("bulwark",defense.Choices.Single().Id);
            Assert.IsTrue(_rewards.TryChoose(Request,0,"bulwark",Ready,()=>true,()=>true,null));
            Complete(2);
            Assert.IsTrue(_route.TryCommitDestination(_route.Session.Id,3,Ready)); Assert.IsTrue(_route.TryEnterNode(_route.Session.Id,3,Ready));
            Assert.IsTrue(_route.TryCompleteRepair(_route.Session.Id,3,Ready)); EnterReward(5);
            foreach(GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation))) _mods.GauntletMutations.Select(mutation);
            var first=Open(); Assert.IsEmpty(first.Choices); Assert.IsFalse(first.IsAlternative);
            Assert.AreEqual(0,_route.Session.MutationAcquisitions); Assert.AreEqual(RunPhase.RewardPending,_route.Session.Phase);
        }

        [TestCase("ignite")] [TestCase("frost")] [TestCase("conductor")] [TestCase("detonation")]
        public void EachProtocolCanAppearAtBothSingleProtocolOpportunitiesWithoutEarlierProtocol(string id)
        {
            var pool=ManualRewardCatalog.Eligible(RunRewardKind.Protocol,_mods,_weapon);
            Assert.IsTrue(pool.Any(o=>o.Id==id));
            Assert.AreEqual(RunRewardKind.Protocol,_route.Session.Route.Node(6).Reward);
            Assert.AreEqual(RunRewardKind.Protocol,_route.Session.Route.Node(8).Reward);
            // Three of four cards: varying real session seeds gives each eligible protocol a reachable offer.
            bool found=false;
            for(int i=0;i<32&&!found;i++)
            {
                var route=new ManualRoutePolicy(_route.Session.Route); var rewards=new RunRewardPolicy(route);
                foreach(int node in new[]{1,2,4,5})
                {
                    Assert.IsTrue(route.TryCommitDestination(route.Session.Id,node,Ready)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,node,Ready));
                    Assert.IsTrue(route.TryResolveEncounter(route.Session.Id,node,true,Ready));
                    Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,Ready));
                }
                Assert.IsTrue(route.TryCommitDestination(route.Session.Id,6,Ready)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,6,Ready));
                Assert.IsTrue(route.TryResolveEncounter(route.Session.Id,6,true,Ready));
                var ticket=rewards.Open(new RunRewardRequest(route.Session.Id,6,0),Ready,"fresh",pool);
                found=ticket.Choices.Any(o=>o.Id==id);
            }
            Assert.IsTrue(found);
        }

        [Test] public void RejectsIneligibleDeadAndOldSessionWithoutApplying()
        {
            EnterReward(1); var ticket=Open(); int count=0; Func<bool> apply=()=>{count++;return true;};
            Assert.IsFalse(_rewards.TryChoose(Request,0,ticket.Choices[0].Id,new RunActionContext(false),()=>true,apply,null));
            Assert.IsFalse(_rewards.TryChoose(Request,0,ticket.Choices[0].Id,Ready,()=>false,apply,null));
            Assert.IsFalse(_rewards.TryChoose(new RunRewardRequest(Guid.NewGuid(),1,0),0,ticket.Choices[0].Id,Ready,()=>true,apply,null));
            Assert.AreEqual(0,count); Assert.AreEqual(RunRewardTicketState.Pending,ticket.State);
        }
    }
}
