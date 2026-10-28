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

        // Fixture preparation follows the executable pool, including existing Augments.
        // It deliberately grants test inventory; it is not evidence of a natural reward economy.
        private static void AcquireRewardFixtureOffer(RunBoons run, string id)
        {
            if (BoonExpansionCatalog.TryDefinition(id, out _))
                Assert.IsTrue(BoonExpansionCatalog.TryAcquire(id, run.WeaponModifiers, run.RunWeapon, run.Protocols, out var error), error);
            else if (id == BoonReworkCatalog.ViolentFallId)
                Assert.IsTrue(run.WeaponModifiers.TryAcquireViolentFall());
            else
            {
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(id, out var definition), id);
                Assert.IsTrue(run.WeaponModifiers.Add(definition), id);
            }
        }
        private static void ExhaustCurrentTechniqueFixture(RunBoons run)
        {
            for (int guard = 0; guard < 200; guard++)
            {
                var pool = ManualRewardCatalog.Eligible(RunRewardKind.Technique, run.WeaponModifiers, run.RunWeapon, run.Protocols);
                if (pool.Count == 0) return;
                AcquireRewardFixtureOffer(run, pool[0].Id);
            }
            Assert.Fail("Technique fixture did not exhaust the real compatible pool.");
        }

        [UnityTest] public IEnumerator ManualExhaustedTechniqueAppliesRetainedProtocolOnceAndPreservesResourcesAndSourceAssets()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            var source=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var sourceAbilities=source.abilities.ToArray();
            string sourceJson=JsonUtility.ToJson(source);
            var abilityJson=sourceAbilities.Select(ability=>JsonUtility.ToJson(ability)).ToArray();
            _player.EquipWeapon(source); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
            ExhaustCurrentTechniqueFixture(run);
            ReachManualReward(route,1); var request=new RunRewardRequest(route.Session.Id,1,0);
            var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(52);
            _player.health=123; _player.mana=81;
            float armor=_player.Stats.GetStat(PlayerStatType.Armor); int legacyEvents=0,manualEvents=0;
            run.RewardChosen+=()=>legacyEvents++;
            run.ManualRewardChosen+=ticket=> { manualEvents++; Assert.IsFalse(run.Choose(0)); };
            Assert.IsTrue(run.OfferReward(request)); Assert.IsTrue(run.ManualTicket.IsAlternative);
            Assert.That(run.Choices.Count,Is.InRange(1,3));
            Assert.IsFalse(run.Choices.Any(offer=>offer.Id=="bulwark"));
            Assert.IsTrue(run.Choices.All(offer=>ProtocolInventory.TryKind(offer.Id,out var kind)&&RunBoonOfferCatalog.Includes(kind)));
            string selectedId=run.Choices[0].Id; Assert.IsTrue(ProtocolInventory.TryKind(selectedId,out var selectedProtocol));
            var ids=run.Choices.Select(o=>o.Id).ToArray(); run.CloseManualReward();
            Assert.AreEqual(RunPhase.RewardPending,route.Session.Phase);
            Assert.IsTrue(run.OfferReward(request)); Assert.That(run.Choices.Select(o=>o.Id),Is.EqualTo(ids));
            Assert.IsTrue(run.Choose(0)); Assert.IsFalse(run.Choose(0)); Assert.IsFalse(run.OfferReward(request));
            Assert.AreEqual(armor,_player.Stats.GetStat(PlayerStatType.Armor));
            Assert.AreEqual(1,run.Protocols.Rank(selectedProtocol)); Assert.AreEqual(1,route.Session.ProtocolAcquisitions);
            AssertRetainedProtocolEffect(_player,selectedId);
            Assert.AreEqual(123,_player.health); Assert.AreEqual(81,_player.mana); Assert.AreEqual(52,combat.Energy);
            Assert.AreEqual(1,manualEvents); Assert.AreEqual(0,legacyEvents); Assert.AreEqual(1,run.Acquired.Count);
            Assert.That(source.abilities,Is.EqualTo(sourceAbilities)); Assert.AreNotSame(source,run.RunWeapon);
            Assert.AreEqual(sourceJson,JsonUtility.ToJson(source));
            Assert.That(source.abilities.Select(ability=>JsonUtility.ToJson(ability)),Is.EqualTo(abilityJson));
        }

        [UnityTest] public IEnumerator ManualRewardStaleInventoryClickRebuildsThenRealMutationIsRecordedOnlyOnAcquisition()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route)); ReachManualReward(route,1);
            var request=new RunRewardRequest(route.Session.Id,1,0); Assert.IsTrue(run.OfferReward(request));
            string staleId = run.Choices[0].Id;
            int maxRank = WeaponRunModifiers.TryDefinition(staleId, out var d) ? d.MaxRank : 1;
            for (int i = 0; i < maxRank; i++) AcquireRewardFixtureOffer(run, staleId);
            Assert.IsFalse(run.Choose(0)); Assert.AreEqual(1,run.ManualTicket.Revision);
            Assert.IsFalse(run.Choices.Any(o=>o.Id==staleId)); Assert.IsEmpty(run.Acquired);
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

        [UnityTest] public IEnumerator ManualProtocolTicketsApplyEachRetainedEffectFromActualCompatibleOffers()
        {
            Assert.IsTrue(TryBakeFullFloor());
            foreach(string id in new[]{"ignite","frost","conductor","detonation"})
            {
                BuildPlayer(Vector3.zero); var holder=_player.gameObject.AddComponent<AbilityHolder>();
                _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
                var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
                var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
                // Fill unrelated inventory ranks without applying their effects. This leaves the desired
                // retained Protocol as the sole eligible offer while testing the real ticket/applicator.
                Assert.IsTrue(ProtocolInventory.TryKind(id,out var targetProtocol));
                foreach(var other in new[]{ProtocolKind.Ignite,ProtocolKind.Frost,ProtocolKind.Conductor,ProtocolKind.Detonation})
                    if(other!=targetProtocol)
                        while(run.Protocols.CanAcquire(other))
                            Assert.IsTrue(run.Protocols.TryAcquire(run.Protocols.SessionId,other,run.Protocols.Revision,_=>true));
                foreach(int node in new[]{1,2,4,5})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,RewardReady)); }
                ReachManualReward(route,6);
                var request=new RunRewardRequest(route.Session.Id,6,0);
                var eligible=ManualRewardCatalog.Eligible(RunRewardKind.Protocol,run.WeaponModifiers,run.RunWeapon,run.Protocols);
                Assert.That(eligible.Select(offer=>offer.Id),Is.EqualTo(new[]{id}));
                Assert.IsEmpty(run.Acquired); Assert.IsTrue(run.OfferReward(request));
                Assert.AreEqual(id,run.Choices.Single().Id); Assert.IsFalse(run.ManualTicket.IsAlternative);
                Assert.IsTrue(run.Choose(0)); Assert.IsFalse(run.Choose(0));
                Assert.AreEqual(id,run.Acquired.Single().Id); Assert.AreEqual(1,run.Protocols.Rank(targetProtocol));
                Assert.AreEqual(1,route.Session.ProtocolAcquisitions);
                AssertRetainedProtocolEffect(_player,id);
                Assert.AreEqual(0,_player.OnHitEffects.Synergies.Rank(RunSynergy.Reactor));
                Assert.AreEqual(0,_player.OnHitEffects.Synergies.Rank(RunSynergy.Resonance));
                UnityEngine.Object.DestroyImmediate(_player.gameObject); yield return null;
            }
        }

        private static void AssertRetainedProtocolEffect(PlayerActor player,string id)
        {
            var effects=player.OnHitEffects;
            bool burn=(bool)typeof(PlayerOnHitEffects).GetField("_burnEnabled",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(effects);
            Assert.AreEqual(id=="ignite",burn);
            Assert.AreEqual(id=="frost"?.35f:0f,effects.ChillChance);
            Assert.AreEqual(id=="conductor"?1:0,effects.Synergies.Rank(RunSynergy.Conductor));
            Assert.AreEqual(id=="detonation"?1:0,effects.Synergies.Rank(RunSynergy.Detonation));
        }
    }
}
