using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        [UnityTest] public IEnumerator ManualRepairUsesRealHealthEventAndHudWithoutChangingManaAsuraOrCooldowns()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture();
            try
            {
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=(ManualRoutePolicy)typeof(ProgressionDirector).GetField("_manualPolicy",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fixture.director);
                foreach(int node in new[]{1,2})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,false,RewardReady)); }
                Assert.IsTrue(route.TryCommitDestination(route.Session.Id,3,RewardReady)); Assert.IsTrue(route.TryEnterNode(route.Session.Id,3,RewardReady));
                var holder=_player.GetComponent<AbilityHolder>(); var combat=_player.GetComponent<BreakerGauntletCombat>(); combat.AddAsuraEnergy(61);
                SetPrivate(_player,"_manaRegenerationPercentPerSecond",0f);
                SetPrivate(_player,"baseMaxMana",100f); _player.RefreshResourceStatsPreservingAmounts(); _player.mana=42;
                _player.health=_player.maxHealth-10f;
#if UNITY_EDITOR
                var hudObject=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/PlayerHUD.prefab")); _spawned.Add(hudObject);
                var hud=hudObject.GetComponent<PlayerHUD>(); hud.Configure(_player); yield return null;
#endif
                var timers=(float[])typeof(AbilityHolder).GetField("cooldownTimers",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(holder);
                timers[0]=7f; float beforeTimer=timers[0]; int notifications=0;
                _player.HealthChanged+=actor=>
                {
                    notifications++;
                    Assert.IsFalse(fixture.director.TryResolveRepair(route.Session.Id,3,true),"Healing callback cannot reclaim Repair.");
                };
                Assert.AreEqual(10f,fixture.director.ManualRepairAmount);
                Assert.IsFalse(fixture.director.TryResolveRepair(Guid.NewGuid(),3,true));
                Assert.IsTrue(fixture.director.TryResolveRepair(route.Session.Id,3,true));
                Assert.AreEqual(_player.maxHealth,_player.health); Assert.AreEqual(1,notifications);
                Assert.AreEqual(42,_player.mana); Assert.AreEqual(61,combat.Energy); Assert.AreEqual(beforeTimer,timers[0]);
                Assert.IsFalse(fixture.director.TryResolveRepair(route.Session.Id,3,true));
                Assert.IsTrue(fixture.director.ManualRepairResult.Used); Assert.AreEqual(10,fixture.director.ManualRepairResult.Amount);
#if UNITY_EDITOR
                Assert.AreEqual(1f,hud.HealthFill.fillAmount);
#endif
                _player.health=87; fixture.director.RebuildManualResourceStats();
                Assert.AreEqual(87,_player.health);
                // Director reactivation keeps this session and never repeats resource initialization.
                var session=fixture.director.ManualSession; fixture.director.gameObject.SetActive(false); fixture.director.gameObject.SetActive(true); yield return null;
                Assert.AreSame(session,fixture.director.ManualSession); Assert.AreEqual(87,_player.health); Assert.AreEqual(61,combat.Energy);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualResourceRebuildKeepsAbsoluteHpAfterMaximumChangeAndDoesNotReplayIronHeart()
        {
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero);
            var holder=_player.gameObject.AddComponent<AbilityHolder>();
            _player.EquipWeapon(Resources.Load<WeaponScript>(WeaponLoadout.ResourcePaths[0])); holder.RefreshLoadout();
            var run=_player.gameObject.AddComponent<RunBoons>(); yield return null;
            _player.health=97; float maximum=_player.maxHealth;
            var source=_player.gameObject; _player.Stats.AddModifier(new PlayerStatModifier(PlayerStatType.MaxHealthBonus,PlayerStatModifierMode.Flat,100),source);
            _player.RefreshResourceStatsPreservingAmounts(); Assert.AreEqual(maximum+100,_player.maxHealth); Assert.AreEqual(97,_player.health);
            _player.health=_player.maxHealth; _player.Stats.RemoveModifiersFrom(source); _player.RefreshResourceStatsPreservingAmounts();
            Assert.AreEqual(maximum,_player.health,"A decreased maximum clamps HP.");
            // The legacy pickup still grants +100 and a full heal exactly when acquired.
            _player.health=19; run.OfferReward(1);
            var choices=(List<RunBoons.Offer>)typeof(RunBoons).GetField("_choices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run);
            choices.Clear(); choices.Add(new RunBoons.Offer("vitality","Coração de ferro","+100/full heal"));
            Assert.IsTrue(run.Choose(0)); Assert.AreEqual(maximum+100,_player.maxHealth); Assert.AreEqual(_player.maxHealth,_player.health);
            _player.health=73; _player.RefreshResourceStatsPreservingAmounts(); Assert.AreEqual(73,_player.health);
            Assert.AreEqual(maximum+100,_player.maxHealth); Assert.AreEqual(1,run.Acquired.Count);
        }
    }
}
