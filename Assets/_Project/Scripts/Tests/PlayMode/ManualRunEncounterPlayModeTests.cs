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
        [UnityTest] public IEnumerator ManualLastBasicBatchWaitsForFeedbackCloseAndPreservesLethalEnergyHitStopAndEpoch()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(firstEncounterEnemies:2);
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var session=fixture.director.ManualSession;
                Assert.IsTrue(fixture.director.TryCommitDestination(session.Id,1)); Assert.IsTrue(fixture.director.TryEnterNode(session.Id,1));
                var enemies=fixture.director.GetComponentsInChildren<Actor>(); Assert.AreEqual(2,enemies.Length);
                foreach(var enemy in enemies) enemy.SetMaxHealth(1f);
                var combat=_player.GetComponent<BreakerGauntletCombat>();
                var contexts=(GauntletContextRegistry)typeof(BreakerGauntletCombat).GetField("_contexts",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(combat);
                long epoch=contexts.Epoch; int resolutions=0,deaths=0;
                fixture.director.ManualEncounterResolved+=request=>resolutions++;
                foreach(var enemy in enemies) enemy.Died+=actor=>
                {
                    deaths++; Assert.AreEqual(RunPhase.Encounter,session.Phase); Assert.AreEqual(0,resolutions);
                    if(deaths==2)
                    {
                        Assert.IsTrue(session.IsEncounterClosing);
                        Assert.IsFalse(_player.GetComponent<AbilityHolder>().TryUseAbility(2),"Candidate closure rejects future input without buffering.");
                    }
                };
                Physics.SyncTransforms();
                using(_player.BeginImpactFeedback(.18f))
                {
                    Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward*2f));
                    Assert.AreEqual(2,deaths); Assert.AreEqual(3,combat.Energy,"The native lethal multi-target basic pays once.");
                    Assert.IsTrue(fixture.director.HasManualCompletionCandidate);
                    Assert.IsFalse(_player.GetComponent<AbilityHolder>().TryStopEncounterOffense(),"Open feedback scope prevents cleanup.");
                    yield return null; Assert.AreEqual(RunPhase.Encounter,session.Phase); Assert.AreEqual(0,resolutions);
                }
                var hitstop=_player.GetComponent<HitStopRunner>(); Assert.IsTrue(hitstop.IsActive);
                yield return new WaitForEndOfFrame();
                Assert.AreEqual(RunPhase.RewardPending,session.Phase); Assert.AreEqual(1,resolutions);
                Assert.AreEqual(3,combat.Energy); Assert.AreEqual(epoch,contexts.Epoch); Assert.IsTrue(hitstop.IsActive,"Confirmed hit-stop must survive cleanup.");
                var run=_player.GetComponent<RunBoons>(); Assert.IsNotNull(run.ManualTicket); Assert.AreEqual(RunRewardTicketState.Pending,run.ManualTicket.State);
                Assert.IsFalse(run.IsChoosing); Assert.IsFalse(fixture.director.TryOpenManualReward(session.Id,1,0));
                yield return new WaitForSeconds(.25f);
                Assert.IsTrue(fixture.director.TryOpenManualReward(session.Id,1,0)); Assert.IsTrue(run.Choose(0));
                Assert.AreEqual(3,combat.Energy); Assert.AreEqual(epoch,contexts.Epoch); Assert.AreEqual(1,resolutions);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualLastSkillCancelsFutureChildrenAfterConfirmedProducerWithoutResettingCategory()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture();
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var session=fixture.director.ManualSession; var run=_player.GetComponent<RunBoons>();
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho),out var echo));
                Assert.IsTrue(run.WeaponModifiers.Add(echo));
                SetPrivate(_player,"baseMaxMana",500f); _player.RefreshResourceStats(true);
                Assert.IsTrue(fixture.director.TryCommitDestination(session.Id,1)); Assert.IsTrue(fixture.director.TryEnterNode(session.Id,1));
                var enemy=fixture.director.GetComponentsInChildren<Actor>().Single(); enemy.transform.position=Vector3.forward*1.2f;
                var holder=_player.GetComponent<AbilityHolder>(); var combat=_player.GetComponent<BreakerGauntletCombat>();
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)run.RunWeapon.abilities[1],GauntletActionSlot.W,
                    run.WeaponModifiers,out var snapshot,run.WeaponModifiers.GauntletMutations));
                var damages=snapshot.CopySteps().Take(snapshot.DirectStepCount).Select(step=>
                    _player.Stats.RollAttackDamage(_player.Stats.CaptureAttackDamage(run.RunWeapon.attackDamage,step.damageMultiplier,step.bonusDamage)).Amount).ToArray();
                enemy.SetMaxHealth(damages.Sum()-damages.Last()*.5f); // Survives prior beats; the actual producer is lethal.
                bool producer=false; int energyAtKill=0,resolutions=0;
                enemy.DamageReceived+=(actor,damage)=>
                {
                    var context=_player.CurrentGauntletImpactContext;
                    if(context.ImpactIndex!=context.Snapshot.LastDirectProducer) return;
                    producer=true; Assert.IsTrue(actor.IsDead); energyAtKill=combat.Energy;
                    Assert.AreEqual(RunPhase.Encounter,session.Phase); Assert.Greater(combat.PendingChildCount,0);
                };
                fixture.director.ManualEncounterResolved+=request=>resolutions++;
                Physics.SyncTransforms(); Assert.IsTrue(holder.TryUseAbility(1));
                float deadline=Time.realtimeSinceStartup+4f;
                while(session.Phase==RunPhase.Encounter&&Time.realtimeSinceStartup<deadline) yield return null;
                Assert.IsTrue(producer); Assert.AreEqual(RunPhase.RewardPending,session.Phase); Assert.AreEqual(1,resolutions);
                Assert.Greater(energyAtKill,0); Assert.AreEqual(energyAtKill,combat.Energy); Assert.AreEqual(0,combat.PendingChildCount); Assert.IsFalse(combat.IsExecuting);
                var momentum=(AsuraMomentum)typeof(BreakerGauntletCombat).GetField("_momentum",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(combat);
                Assert.IsTrue(momentum.IsAlternation(!((BreakerGauntletAbility)run.RunWeapon.abilities[1]).ShockSkill));
                yield return new WaitForSeconds(.4f); Assert.AreEqual(1,resolutions); Assert.AreEqual(energyAtKill,combat.Energy);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualLastEchoClosesItsOwnBatchAndNeverPaysNativeEnergyAgain()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture();
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var session=fixture.director.ManualSession; var run=_player.GetComponent<RunBoons>();
                Assert.IsTrue(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho),out var echo)); Assert.IsTrue(run.WeaponModifiers.Add(echo));
                SetPrivate(_player,"baseMaxMana",500f); _player.RefreshResourceStats(true);
                Assert.IsTrue(fixture.director.TryCommitDestination(session.Id,1)); Assert.IsTrue(fixture.director.TryEnterNode(session.Id,1));
                var enemy=fixture.director.GetComponentsInChildren<Actor>().Single(); enemy.transform.position=Vector3.forward*1.2f;
                var reaction=enemy.GetComponent<CombatReactionController>(); reaction.ConfigureStance(EnemyRank.Normal,10000f,1f,0f,0f,0f,0f,0f,0f);
                var combat=_player.GetComponent<BreakerGauntletCombat>();
                Assert.IsTrue(GauntletExecutionSnapshot.TryBuild((BreakerGauntletAbility)run.RunWeapon.abilities[1],GauntletActionSlot.W,
                    run.WeaponModifiers,out var snapshot,run.WeaponModifiers.GauntletMutations));
                var damages=snapshot.CopySteps().Take(snapshot.DirectStepCount).Select(step=>
                    _player.Stats.RollAttackDamage(_player.Stats.CaptureAttackDamage(run.RunWeapon.attackDamage,step.damageMultiplier,step.bonusDamage)).Amount).ToArray();
                enemy.SetMaxHealth(damages.Sum()+damages.Last()*.55f*.5f); // Survives all direct beats; first real echo is lethal.
                bool echoKilled=false; int energyBeforeEcho=0,resolutions=0;
                enemy.DamageReceived+=(actor,amount)=>
                { var context=_player.CurrentGauntletImpactContext; if(context.Origin==GauntletImpactOrigin.Direct) energyBeforeEcho=combat.Energy; };
                enemy.Died+=actor=>
                {
                    echoKilled=_player.CurrentGauntletImpactContext.Origin==GauntletImpactOrigin.Echo;
                    Assert.AreEqual(RunPhase.Encounter,session.Phase); Assert.AreEqual(energyBeforeEcho,combat.Energy);
                    Assert.IsTrue(_player.IsResolvingGauntletImpact); Assert.IsTrue(_player.HasOpenImpactFeedback);
                };
                fixture.director.ManualEncounterResolved+=request=>resolutions++;
                Physics.SyncTransforms(); Assert.IsTrue(_player.GetComponent<AbilityHolder>().TryUseAbility(1));
                float deadline=Time.realtimeSinceStartup+5f;
                while(session.Phase==RunPhase.Encounter&&Time.realtimeSinceStartup<deadline) yield return null;
                Assert.IsTrue(echoKilled); Assert.AreEqual(RunPhase.RewardPending,session.Phase); Assert.AreEqual(1,resolutions);
                Assert.Greater(energyBeforeEcho,0); Assert.AreEqual(energyBeforeEcho,combat.Energy); Assert.AreEqual(0,combat.PendingChildCount);
                yield return new WaitForSeconds(.3f); Assert.AreEqual(1,resolutions); Assert.AreEqual(energyBeforeEcho,combat.Energy);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualPlayerDeathInLastLethalBatchWinsWithoutRewardPublication()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(firstEncounterEnemies:2);
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var session=fixture.director.ManualSession; Assert.IsTrue(fixture.director.TryCommitDestination(session.Id,1)); Assert.IsTrue(fixture.director.TryEnterNode(session.Id,1));
                var enemies=fixture.director.GetComponentsInChildren<Actor>(); int deaths=0,publications=0,confirmedEnergy=0;
                foreach(var enemy in enemies)
                {
                    enemy.SetMaxHealth(1f); enemy.Died+=actor=>
                    {
                        if(++deaths!=2) return;
                        confirmedEnergy=_player.GetComponent<BreakerGauntletCombat>().Energy;
                        _player.TakeDamage(1000000f);
                    };
                }
                fixture.director.ManualEncounterResolved+=request=>publications++;
                Physics.SyncTransforms(); Assert.IsTrue(_controls.TryDirectionalBasicAttack(Vector3.forward*2f));
                Assert.AreEqual(3,confirmedEnergy); Assert.IsTrue(_player.IsDead);
                yield return null; Assert.AreEqual(RunOutcome.Dead,session.Outcome); Assert.AreEqual(RunPhase.Returning,session.Phase);
                Assert.AreEqual(0,publications); Assert.IsFalse(fixture.director.HasManualCompletionCandidate);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }
    }
}
