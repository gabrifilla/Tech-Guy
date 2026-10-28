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
        [UnityTest] public IEnumerator ManualRunRejectsInvalidLoadoutBeforeCancelAndSupportsAllFamiliesWithOwnedCopies()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>(); var source=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0]);
            var bowSource=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[1]);
            var spearSource=Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[2]);
            var sourceJson=new[]{source,bowSource,spearSource}.Select(weapon=>JsonUtility.ToJson(weapon)).ToArray();
            _player.EquipWeapon(source); holder.RefreshLoadout(); var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            var route=BuildManualRewardRoute(); Assert.IsTrue(run.BindManualRun(route));
            var protocols=run.Protocols; var gauntletModifiers=run.WeaponModifiers;
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho),out var technique));
            Assert.IsTrue(gauntletModifiers.Add(technique));
            SetPrivate(_player,"baseMaxMana",500f); _player.RefreshResourceStats(true);
            var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(64);
            Assert.IsTrue(holder.TryUseAbility(1)); var owned=run.RunWeapon; float mana=_player.mana;
            var invalid=UnityEngine.Object.Instantiate(bowSource);
            try
            {
                invalid.abilities=bowSource.abilities.Take(3).ToArray();
                LogAssert.Expect(LogType.Warning,"Manual run rejected weapon change before equip: Run requires a complete four-slot weapon loadout.");
                Assert.IsNull(_player.EquipWeapon(invalid));
                Assert.AreSame(owned,_player.CurrentWeapon); Assert.AreSame(owned,run.RunWeapon); Assert.IsTrue(combat.IsExecuting);
                Assert.AreSame(gauntletModifiers,run.WeaponModifiers); Assert.AreEqual(1,gauntletModifiers.Rank(WeaponBoon.FlurryEcho));
                Assert.AreEqual(64,combat.Energy); Assert.AreEqual(mana,_player.mana);
                Assert.AreSame(protocols,run.Protocols); Assert.AreEqual(route.Session.Id,protocols.SessionId);
                Assert.IsEmpty(run.Acquired); Assert.AreEqual(0,route.Session.InventoryRevision);
            }
            finally { UnityEngine.Object.DestroyImmediate(invalid); }
            Assert.AreSame(owned,_player.EquipWeapon(bowSource)); holder.RefreshLoadout();
            Assert.IsFalse(combat.IsExecuting); Assert.AreEqual(0,combat.Energy,"An authorized swap follows the existing Asura lifecycle.");
            Assert.AreSame(run.RunWeapon,_player.CurrentWeapon); Assert.AreNotSame(bowSource,run.RunWeapon);
            Assert.AreEqual(RunWeaponFamily.Bow,run.WeaponModifiers.Family);
            var bowModifiers=run.WeaponModifiers;
            Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet),out var ricochet));
            Assert.IsTrue(bowModifiers.Add(ricochet));
            for(int slot=0;slot<4;slot++) Assert.AreNotSame(bowSource.abilities[slot],run.RunWeapon.abilities[slot]);
            var bowOwned=run.RunWeapon; Assert.AreSame(bowOwned,_player.EquipWeapon(spearSource)); holder.RefreshLoadout();
            Assert.AreEqual(RunWeaponFamily.Spear,run.WeaponModifiers.Family); Assert.AreNotSame(spearSource,run.RunWeapon);
            for(int slot=0;slot<4;slot++) Assert.AreNotSame(spearSource.abilities[slot],run.RunWeapon.abilities[slot]);
            _player.EquipWeapon(source); holder.RefreshLoadout();
            Assert.AreSame(gauntletModifiers,run.WeaponModifiers); Assert.AreEqual(1,run.WeaponModifiers.Rank(WeaponBoon.FlurryEcho));
            _player.EquipWeapon(bowSource); holder.RefreshLoadout();
            Assert.AreSame(bowModifiers,run.WeaponModifiers); Assert.AreEqual(1,run.WeaponModifiers.Rank(WeaponBoon.Ricochet));
            Assert.AreSame(protocols,run.Protocols); Assert.AreEqual(route.Session.Id,protocols.SessionId); Assert.AreEqual(0,protocols.Revision);
            Assert.That(new[]{source,bowSource,spearSource}.Select(weapon=>JsonUtility.ToJson(weapon)),Is.EqualTo(sourceJson));
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
                // Both current executors are offered; exercise Cadenced Asura on each real path.
                var selected=GauntletMutation.CadencedAsura; string id=GauntletContentAuthoring.ForMutation(selected).Id;
                Assert.IsTrue(run.OfferReward(request));
                Assert.That(run.Choices.Select(o=>o.Id),Is.EquivalentTo(new[]{id, BoonReworkCatalog.UppercutId}));
                run.CloseManualReward(); Assert.AreEqual(0,route.Session.MutationAcquisitions);
                Assert.IsTrue(run.OfferReward(request)); Assert.IsTrue(run.Choose(run.Choices.ToList().FindIndex(o=>o.Id==id))); Assert.AreEqual(1,route.Session.MutationAcquisitions);
                int next=path%2==0?6:7;
                Assert.IsTrue(route.TryCommitDestination(route.Session.Id,next,RewardReady)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,next,RewardReady));
                SetPrivate(_player,"baseMaxMana",500f); SetPrivate(_player,"_manaRegenerationPercentPerSecond",0f); _player.RefreshResourceStats(true);
                var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(100);
                _player.transform.rotation=Quaternion.identity;
                int slot=(int)GauntletMutationSelection.SlotOf(selected); float mana=_player.mana;
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)run.RunWeapon.abilities[slot],(GauntletActionSlot)slot,
                    run.WeaponModifiers,out var preview,run.WeaponModifiers.GauntletMutations));
                var enemy=BuildEnemy(Vector3.forward*1.2f,10000f).actor; Physics.SyncTransforms();
                var impacts=new System.Collections.Generic.List<int>(); int uses=0;
                holder.AbilityUsed+=used=> { if(used==slot) uses++; };
                enemy.DamageReceived+=(_,__)=> {
                    if(_player.CurrentGauntletImpactContext?.Origin==GauntletImpactOrigin.Direct)
                        impacts.Add(_player.CurrentGauntletImpactContext.ImpactIndex);
                };
                Assert.IsTrue(holder.TryUseAbility(slot));
                Assert.AreEqual(selected,combat.ExecutionSnapshot.Mutation); Assert.AreEqual(mana-combat.ExecutionSnapshot.ManaCost,_player.mana,.001f);
                for(int strike=0;strike<2;strike++)
                {
                    float waitDeadline=Time.realtimeSinceStartup+3f;
                    while(!combat.IsCadencedWaiting&&Time.realtimeSinceStartup<waitDeadline) yield return null;
                    Assert.IsTrue(combat.IsCadencedWaiting); Assert.AreEqual(strike+1,impacts.Count);
                    Assert.IsTrue(holder.TryUseAbility(slot));
                    Assert.AreEqual(mana-preview.ManaCost,_player.mana,.001f,"Continuation must not debit again.");
                }
                float deadline=Time.realtimeSinceStartup+3f;
                while(combat.IsExecuting&&Time.realtimeSinceStartup<deadline) yield return null;
                Assert.IsFalse(combat.IsExecuting); Assert.Less(enemy.health,10000f,"Acquired Mutation did not land: "+selected);
                Assert.That(impacts,Is.EqualTo(new[]{0,1,2})); Assert.AreEqual(1,uses);
                Assert.AreEqual(mana-preview.ManaCost,_player.mana,.001f);
                Assert.That(source.abilities,Is.EqualTo(sourceAbilities)); Assert.AreEqual(1,route.Session.MutationAcquisitions);
                UnityEngine.Object.DestroyImmediate(_player.gameObject); if(enemy) UnityEngine.Object.DestroyImmediate(enemy.gameObject);
                UnityEngine.Object.DestroyImmediate(_weapon); yield return null;
            }
        }
    }
}
