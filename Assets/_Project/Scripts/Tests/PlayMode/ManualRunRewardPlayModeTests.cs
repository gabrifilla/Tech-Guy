using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private static readonly RunActionContext RewardReady = new RunActionContext(true);
        private static void ReachManualReward(ManualRoutePolicy route,int node)
        {
            Assert.IsTrue(route.TryCommitDestination(route.Session.Id,node,RewardReady));
            Assert.IsTrue(route.TryEnterNode(route.Session.Id,node,RewardReady));
            Assert.IsTrue(route.TryResolveEncounter(route.Session.Id,node,true,RewardReady));
        }
        private ManualRoutePolicy BuildManualRewardRoute()
        {
            var prefab=new GameObject("Reward test actor template"); prefab.SetActive(false); _spawned.Add(prefab); prefab.AddComponent<Actor>();
            var definition=ManualRunFixture.Create(prefab);
            Assert.IsTrue(ManualRouteValidator.TrySnapshot(definition,Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]),out var snapshot,out string error),error);
            UnityEngine.Object.DestroyImmediate(definition); return new ManualRoutePolicy(snapshot);
        }

        [UnityTest] public IEnumerator ManualRewardDefenseAppliesActualArmorOnceAndPreservesResourcesAndSourceAssets()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            var source=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var sourceAbilities=source.abilities.ToArray();
            _player.EquipWeapon(source); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
            foreach(var d in WeaponRunModifiers.Catalog.Where(d=>GauntletContentAuthoring.IsSelectedTechnique(d.Kind)))
                for(int i=0;i<d.MaxRank;i++) Assert.IsTrue(run.WeaponModifiers.Add(d));
            ReachManualReward(route,1); var request=new RunRewardRequest(route.Session.Id,1,0);
            var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(52);
            _player.health=123; _player.mana=81;
            float armor=_player.Stats.GetStat(PlayerStatType.Armor); int legacyEvents=0,manualEvents=0;
            run.RewardChosen+=()=>legacyEvents++;
            run.ManualRewardChosen+=ticket=> { manualEvents++; Assert.IsFalse(run.Choose(0)); };
            Assert.IsTrue(run.OfferReward(request)); Assert.AreEqual("bulwark",run.Choices.Single().Id);
            var ids=run.Choices.Select(o=>o.Id).ToArray(); run.CloseManualReward();
            Assert.AreEqual(RunPhase.RewardPending,route.Session.Phase);
            Assert.IsTrue(run.OfferReward(request)); Assert.That(run.Choices.Select(o=>o.Id),Is.EqualTo(ids));
            Assert.IsTrue(run.Choose(0)); Assert.IsFalse(run.Choose(0)); Assert.IsFalse(run.OfferReward(request));
            Assert.AreEqual(armor+40,_player.Stats.GetStat(PlayerStatType.Armor));
            Assert.AreEqual(123,_player.health); Assert.AreEqual(81,_player.mana); Assert.AreEqual(52,combat.Energy);
            Assert.AreEqual(1,manualEvents); Assert.AreEqual(0,legacyEvents); Assert.AreEqual(1,run.Acquired.Count);
            Assert.That(source.abilities,Is.EqualTo(sourceAbilities)); Assert.AreNotSame(source,run.RunWeapon);
        }

        [UnityTest] public IEnumerator ManualRewardStaleInventoryClickRebuildsThenRealMutationIsRecordedOnlyOnAcquisition()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route)); ReachManualReward(route,1);
            var request=new RunRewardRequest(route.Session.Id,1,0); Assert.IsTrue(run.OfferReward(request));
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(run.Choices[0].Id,out var d));
            for(int i=0;i<d.MaxRank;i++) Assert.IsTrue(run.WeaponModifiers.Add(d));
            Assert.IsFalse(run.Choose(0)); Assert.AreEqual(1,run.ManualTicket.Revision);
            Assert.IsFalse(run.Choices.Any(o=>o.Id==d.Id)); Assert.IsEmpty(run.Acquired);
            Assert.IsTrue(run.Choose(0)); ReachManualReward(route,2);
            Assert.IsTrue(route.TryCompleteReward(route.Session.Id,2,0,false,RewardReady));
            Assert.IsTrue(route.TryCommitDestination(route.Session.Id,3,RewardReady)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,3,RewardReady));
            Assert.IsTrue(route.TryCompleteRepair(route.Session.Id,3,RewardReady)); ReachManualReward(route,5);
            request=new RunRewardRequest(route.Session.Id,5,0); Assert.IsTrue(run.OfferReward(request));
            Assert.AreEqual(0,route.Session.MutationAcquisitions); run.CloseManualReward();
            Assert.AreEqual(0,route.Session.MutationAcquisitions); Assert.IsTrue(run.OfferReward(request));
            string id=run.Choices[0].Id; Assert.IsTrue(GauntletContentAuthoring.TryMutation(id,out var mutation));
            yield return null; // A new selection requires a released input frame after the previous acquisition.
            Assert.IsTrue(run.Choose(0)); Assert.AreEqual(1,route.Session.MutationAcquisitions);
            Assert.IsTrue(run.WeaponModifiers.GauntletMutations.TryGet(GauntletMutationSelection.SlotOf(mutation),out var selected));
            Assert.AreEqual(mutation,selected); Assert.AreEqual(RunRewardTicketState.Resolved,run.ManualTicket.State);
        }

        [UnityTest] public IEnumerator ManualEliteDeathBetweenOrdinalsCannotAcquireSecondReward()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
            foreach(int node in new[]{1,2,4,5})
            { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,RewardReady)); }
            ReachManualReward(route,7);
            foreach(var d in WeaponRunModifiers.Catalog.Where(d=>GauntletContentAuthoring.IsSelectedTechnique(d.Kind)))
                for(int i=0;i<d.MaxRank;i++) Assert.IsTrue(run.WeaponModifiers.Add(d));
            run.ManualRewardChosen+=ticket=>run.OfferReward(new RunRewardRequest(route.Session.Id,7,1));
            Assert.IsTrue(run.OfferReward(new RunRewardRequest(route.Session.Id,7,0))); Assert.IsTrue(run.Choose(0));
            yield return null; // The input gate is released; rejection must come from the dead-player guard.
            float armor=_player.Stats.GetStat(PlayerStatType.Armor); _player.TakeDamage(1000000f); Assert.IsTrue(_player.IsDead);
            Assert.IsFalse(run.Choose(0)); Assert.AreEqual(1,run.Acquired.Count);
            Assert.AreEqual(armor,_player.Stats.GetStat(PlayerStatType.Armor)); Assert.AreEqual(RunRewardTicketState.Pending,run.ManualTicket.State);
            yield return null; Assert.IsFalse(run, "Death destroys the player-owned run and clears temporary inventory.");
            // Run lifecycle completion belongs to task 12; this fixture isolates rejection of the second effect.
        }

        [UnityTest] public IEnumerator ManualProtocolTicketsApplyAllFourExistingEffectsWithNoPriorProtocol()
        {
            Assert.IsTrue(TryBakeFullFloor());
            foreach(string id in new[]{"ignite","frost","conductor","detonation"})
            {
                BuildPlayer(Vector3.zero); var holder=_player.gameObject.AddComponent<AbilityHolder>();
                _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
                var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
                var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
                // Earlier rooms are resolved by the policy fixture; this test isolates the real Protocol applicator.
                foreach(int node in new[]{1,2,4,5})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,RewardReady)); }
                bool elite=id=="frost"||id=="detonation";
                if(elite)
                {
                    ReachManualReward(route,7);
                    Assert.IsTrue(route.TryCompleteReward(route.Session.Id,7,0,false,RewardReady));
                    Assert.IsTrue(route.TryCompleteReward(route.Session.Id,7,1,false,RewardReady));
                }
                int target=elite?8:6; ReachManualReward(route,target);
                var request=new RunRewardRequest(route.Session.Id,target,0);
                var rewards=(RunRewardPolicy)typeof(RunBoons).GetField("_manualRewards",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run);
                var eligible=ManualRewardCatalog.Eligible(RunRewardKind.Protocol,run.WeaponModifiers,run.RunWeapon);
                Assert.That(eligible.Select(o=>o.Id),Is.EquivalentTo(new[]{"ignite","frost","conductor","detonation"}));
                // Prefreeze one eligible card to exercise every effect deterministically; distribution has separate policy tests.
                rewards.Open(request,RewardReady,ManualRewardCatalog.InventoryKey(run.WeaponModifiers,run.RunWeapon,route.Session.InventoryRevision),
                    eligible.Where(o=>o.Id==id).ToArray());
                Assert.IsEmpty(run.Acquired); Assert.IsTrue(run.OfferReward(request)); Assert.IsTrue(run.Choose(0));
                Assert.AreEqual(id,run.Acquired.Single().Id);
                var effects=_player.OnHitEffects;
                if(id=="ignite")
                { Assert.IsTrue((bool)typeof(PlayerOnHitEffects).GetField("_burnEnabled",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(effects)); }
                else if(id=="frost") Assert.AreEqual(.35f,effects.ChillChance);
                else Assert.AreEqual(1,effects.Synergies.Rank(id=="conductor"?RunSynergy.Conductor:RunSynergy.Detonation));
                Assert.AreEqual(0,effects.Synergies.Rank(RunSynergy.Reactor)); Assert.AreEqual(0,effects.Synergies.Rank(RunSynergy.Resonance));
                UnityEngine.Object.DestroyImmediate(_player.gameObject); yield return null;
            }
        }
    }
}
