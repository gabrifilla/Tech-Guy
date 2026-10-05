using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
#if UNITY_EDITOR
        private (ProgressionDirector director,ManualStageEnvironment environment) CreateAuthoredManualFixture()
        {
            var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Run/FirstSectorManualEnvironment.prefab")); _spawned.Add(root);
            var environment=root.GetComponent<ManualStageEnvironment>();
            var owner=new GameObject("Authored manual test director"); owner.SetActive(false); _spawned.Add(owner);
            var director=owner.AddComponent<ProgressionDirector>();
            SetManualField(director,"_progressionMode",RunProgressionMode.ManualSlice);
            SetManualField(director,"_manualDefinition",AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset"));
            SetManualField(director,"_manualEnvironment",environment); SetManualField(director,"_player",_player);
            return(director,environment);
        }

        [UnityTest] public IEnumerator AuthoredEliteSpawnsExistingRankAndTwoActualDefenseAcquisitionsRequireInputRelease()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            var keyboard=InputSystem.AddDevice<Keyboard>(); var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
                if(_navMeshInstance.valid) _navMeshInstance.Remove();
                var fixture=CreateAuthoredManualFixture(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=(ManualRoutePolicy)typeof(ProgressionDirector).GetField("_manualPolicy",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(fixture.director);
                // Prior node results are stand-ins to isolate E1 composition and its two real acquisitions.
                foreach(int node in new[]{1,2,4,5})
                { ReachManualReward(route,node); Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,RewardReady)); }
                Assert.IsTrue(fixture.director.TryCommitDestination(route.Session.Id,7));
                Assert.IsTrue(_agent.Warp(route.Session.Route.Node(7).Center)); Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,7));
                yield return null;
                var enemies=fixture.director.GetComponentsInChildren<Actor>(); Assert.AreEqual(5,enemies.Length);
                Assert.AreEqual(1,enemies.Count(e=>e.GetComponent<CombatReactionController>().Rank==EnemyRank.Elite));
                Assert.AreEqual(4,enemies.Count(e=>e.GetComponent<CombatReactionController>().Rank==EnemyRank.Normal));
                var elite=enemies.Single(e=>e.GetComponent<CombatReactionController>().Rank==EnemyRank.Elite);
                Assert.IsNotNull(elite.GetComponent<EnemyVariant>().Profile); Assert.Greater(elite.GetComponent<EnemyVariant>().Profile.HealthMultiplier,1f);
                Assert.Greater(elite.GetComponent<CombatReactionController>().MaxStance,0);
                foreach(var enemy in enemies) enemy.TakeDamage(enemy.health+1f);
                Assert.IsTrue(fixture.director.HasManualCompletionCandidate);
                yield return null;
                Assert.AreEqual(RunPhase.RewardPending,route.Session.Phase,"The real director must close the Elite encounter frontier.");
                var run=_player.GetComponent<RunBoons>();
                foreach(var d in WeaponRunModifiers.Catalog.Where(d=>GauntletContentAuthoring.IsSelectedTechnique(d.Kind)))
                    for(int i=0;i<d.MaxRank;i++) Assert.IsTrue(run.WeaponModifiers.Add(d));
                float armor=_player.Stats.GetStat(PlayerStatType.Armor);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState(Key.Digit1)); InputSystem.Update();
                Assert.IsTrue(fixture.director.TryOpenManualReward(route.Session.Id,7,0)); Assert.AreEqual("bulwark",run.Choices.Single().Id);
                Assert.IsTrue(run.Choose(0)); Assert.AreEqual(armor+40,_player.Stats.GetStat(PlayerStatType.Armor));
                Assert.AreEqual(1,run.ManualTicket.Request.Ordinal); Assert.AreEqual(RunPhase.RewardPending,route.Session.Phase);
                Assert.IsFalse(run.Choose(0)); yield return null; Assert.IsFalse(run.Choose(0),"Holding the selection key must not accept ordinal 1.");
                InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); yield return null;
                Assert.IsTrue(run.Choose(0)); Assert.AreEqual(armor+80,_player.Stats.GetStat(PlayerStatType.Armor));
                Assert.AreEqual(2,run.Acquired.Count); Assert.AreEqual(RunPhase.RouteChoice,route.Session.Phase);
                Assert.AreEqual(RunNodeState.Available,route.Session.NodeState(8)); Assert.AreEqual(RunNodeState.Excluded,route.Session.NodeState(6));
                Assert.IsFalse(run.Choose(0)); Assert.IsFalse(fixture.director.TryOpenManualReward(route.Session.Id,7,1));
            }
            finally { InputSystem.RemoveDevice(keyboard); InputSystem.RemoveDevice(mouse); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); }
        }
#endif
    }
}
