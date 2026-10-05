using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest] public IEnumerator ManualRunRejectsOtherFamiliesBeforeCancelEquipAndClonesCompatibleSwap()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>(); var source=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            _player.EquipWeapon(source); holder.RefreshLoadout(); var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
            SetPrivate(_player,"baseMaxMana",500f); _player.RefreshResourceStats(true);
            var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(64);
            Assert.IsTrue(holder.TryUseAbility(1)); var owned=run.RunWeapon;
            foreach(int index in new[]{1,2})
            {
                Assert.IsNull(_player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[index])));
                Assert.AreSame(owned,_player.CurrentWeapon); Assert.AreSame(owned,run.RunWeapon); Assert.IsTrue(combat.IsExecuting);
                Assert.AreEqual(64,combat.Energy);
            }
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.Momentum),out var technique));
            Assert.IsTrue(run.WeaponModifiers.Add(technique));
            _player.EquipWeapon(source); holder.RefreshLoadout();
            Assert.AreSame(run.RunWeapon,_player.CurrentWeapon); Assert.AreNotSame(source,run.RunWeapon);
            Assert.AreNotSame(owned,run.RunWeapon); Assert.AreEqual(1,run.WeaponModifiers.Rank(WeaponBoon.Momentum));
            Assert.AreEqual(0,combat.Energy,"An actual compatible weapon swap follows the existing Asura lifecycle.");
        }

        [UnityTest] public IEnumerator ManualImpossibleFirstMutationKeepsBossBlockedAndAllowsAbortWithoutVictory()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture();
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=(ManualRoutePolicy)typeof(ProgressionDirector).GetField("_manualPolicy",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fixture.director);
                foreach(int node in new[]{1,2,4})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,false,RewardReady)); }
                ReachManualReward(route,5); var run=_player.GetComponent<RunBoons>();
                foreach(GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation))) Assert.IsTrue(run.WeaponModifiers.GauntletMutations.Select(mutation));
                LogAssert.Expect(LogType.Error,new Regex("Manual Mutation guarantee unavailable for session .*"));
                Assert.IsTrue(fixture.director.TryOpenManualReward(route.Session.Id,5,0)); Assert.IsTrue(run.ManualRecoveryRequired); Assert.IsEmpty(run.Choices);
                run.CloseManualReward(); Assert.AreEqual(RunPhase.RewardPending,route.Session.Phase);
                Assert.IsTrue(fixture.director.TryOpenManualReward(route.Session.Id,5,0)); Assert.AreEqual(0,route.Session.MutationAcquisitions);
                Assert.IsFalse(fixture.director.TryCommitDestination(route.Session.Id,10));
                foreach(GauntletMutation mutation in Enum.GetValues(typeof(GauntletMutation)))
                    Assert.IsTrue(run.WeaponModifiers.GauntletMutations.TryGet(GauntletMutationSelection.SlotOf(mutation),out _),"Pending recovery must not erase slots to fabricate eligibility.");
                SetManualField(fixture.director,"_returnScene","MissingManualRecoveryTestScene");
                LogAssert.Expect(LogType.Error,"ProgressionDirector cannot return to the Nexus: scene 'MissingManualRecoveryTestScene' is missing from Build Settings. Aborting the transition without loading another scene (R8.4).");
                Assert.IsTrue(fixture.director.TryAbortManualRun(route.Session.Id)); Assert.AreEqual(RunOutcome.Aborted,route.Session.Outcome);
                Assert.AreEqual(RunPhase.Returning,route.Session.Phase); Assert.IsFalse(fixture.director.TryAbortManualRun(route.Session.Id));
                Assert.IsFalse(run.OfferReward(new RunRewardRequest(route.Session.Id,5,0)));
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator FourPathsAcquireMutationAtC4AndExecuteItsRealShapeInNextCombat()
        {
            Assert.IsTrue(TryBakeFullFloor());
            var mutations=(GauntletMutation[])Enum.GetValues(typeof(GauntletMutation));
            for(int path=0;path<4;path++)
            {
                BuildPlayer(Vector3.zero); var holder=_player.gameObject.AddComponent<AbilityHolder>();
                var source=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]); var sourceAbilities=source.abilities.ToArray();
                _player.EquipWeapon(source); holder.RefreshLoadout(); var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
                var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
                foreach(int node in new[]{1,2})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,false,RewardReady)); }
                if(path<2)
                {
                    Assert.IsTrue(route.TryCommitDestination(route.Session.Id,3,RewardReady)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,3,RewardReady));
                    Assert.IsTrue(route.TryCompleteRepair(route.Session.Id,3,RewardReady));
                }
                else { ReachManualReward(route,4); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,4,0,false,RewardReady)); }
                ReachManualReward(route,5); var request=new RunRewardRequest(route.Session.Id,5,0);
                var rewards=(RunRewardPolicy)typeof(RunBoons).GetField("_manualRewards",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run);
                var selected=mutations[path]; string id=GauntletContentAuthoring.ForMutation(selected).Id;
                var eligible=ManualRewardCatalog.Eligible(RunRewardKind.Mutation,run.WeaponModifiers,run.RunWeapon);
                rewards.Open(request,RewardReady,ManualRewardCatalog.InventoryKey(run.WeaponModifiers,run.RunWeapon,route.Session.InventoryRevision),eligible.Where(o=>o.Id==id).ToArray());
                Assert.IsTrue(run.OfferReward(request)); run.CloseManualReward(); Assert.AreEqual(0,route.Session.MutationAcquisitions);
                Assert.IsTrue(run.OfferReward(request)); Assert.IsTrue(run.Choose(0)); Assert.AreEqual(1,route.Session.MutationAcquisitions);
                int next=path%2==0?6:7;
                Assert.IsTrue(route.TryCommitDestination(route.Session.Id,next,RewardReady)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,next,RewardReady));
                SetPrivate(_player,"baseMaxMana",500f); SetPrivate(_player,"_manaRegenerationPercentPerSecond",0f); _player.RefreshResourceStats(true);
                var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
                _player.transform.rotation=Quaternion.identity;
                int slot=(int)GauntletMutationSelection.SlotOf(selected); float mana=_player.mana;
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)run.RunWeapon.abilities[slot],(GauntletActionSlot)slot,
                    run.WeaponModifiers,out var preview,run.WeaponModifiers.GauntletMutations));
                // Demolishing Advance has no trajectory damage; its actual uppercut lands after displacement.
                float distance=selected==GauntletMutation.DemolishingAdvance?preview.AdvanceDistance+1.2f:1.2f;
                var enemy=BuildEnemy(Vector3.forward*distance,1000f).actor; Physics.SyncTransforms();
                Assert.IsTrue(holder.TryUseAbility(slot));
                Assert.AreEqual(selected,combat.ExecutionSnapshot.Mutation); Assert.AreEqual(mana-combat.ExecutionSnapshot.ManaCost,_player.mana,.001f);
                float deadline=Time.realtimeSinceStartup+5f;
                while(combat.IsExecuting&&Time.realtimeSinceStartup<deadline) yield return null;
                Assert.IsFalse(combat.IsExecuting); Assert.Less(enemy.health,1000f,"Acquired Mutation did not land in representative combat: "+selected);
                Assert.That(source.abilities,Is.EqualTo(sourceAbilities)); Assert.AreEqual(1,route.Session.MutationAcquisitions);
                UnityEngine.Object.DestroyImmediate(_player.gameObject); if(enemy) UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(_weapon); yield return null;
            }
        }
    }
}
