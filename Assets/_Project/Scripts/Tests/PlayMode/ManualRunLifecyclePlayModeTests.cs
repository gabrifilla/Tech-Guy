using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed partial class BasicAttackR1PlayModeTests
    {
        private sealed class TestManualLoader : IManualRunSceneLoader
        {
            public int Calls;
            public bool Succeed;
            public bool CanLoad(string scene) => true;
            public IEnumerator Load(string scene, Action<bool> completed)
            { Calls++; yield return null; completed(Succeed); }
        }

        private static ManualRoutePolicy LifecycleRoute(ProgressionDirector director) =>
            (ManualRoutePolicy)typeof(ProgressionDirector).GetField("_manualPolicy",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(director);

        // Earlier encounters are policy stand-ins. The final actor death, portal and load lifecycle are real.
        private static void ReachLifecycleBoss(ManualRoutePolicy route)
        {
            foreach(int node in new[]{1,2,4,5,6,9})
            {
                ReachManualReward(route,node);
                Assert.IsTrue(route.TryCompleteReward(route.Session.Id,node,0,node==5,RewardReady));
            }
            Assert.IsTrue(route.TryCommitDestination(route.Session.Id,10,RewardReady));
        }

        [UnityTest] public IEnumerator ManualExtractionClaimsOnceAndRetryPreservesResultAndTeardown()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(); var loader=new TestManualLoader();
            try
            {
                var meta=MetaProgressStore.CreateDefault(); StringAssert.StartsWith(MetaProgressTestIsolation.Directory,meta.FilePath,"Tests never touch the real profile.");
                var before=meta.Load();
                SetManualField(fixture.director,"_manualSceneLoader",loader);
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=LifecycleRoute(fixture.director); ReachLifecycleBoss(route);
                Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,10));
                foreach(var enemy in fixture.director.GetComponentsInChildren<Actor>()) enemy.TakeDamage(1000000f);
                yield return null; yield return null;
                Assert.AreEqual(RunPhase.ExtractionAvailable,route.Session.Phase); Assert.AreEqual(RunOutcome.None,route.Session.Outcome);
                // Fase 1a: banner first, portal only after it; the player is never teleported.
                Assert.IsTrue(fixture.director.ManualVictoryPresenting); Assert.IsNull(fixture.director.ManualExtractionPortal);
                var banner=fixture.director.GetComponentInChildren<UniverseVictoryBanner>(); Assert.IsNotNull(banner); Assert.AreEqual("UNIVERSO 1 CONCLUÍDO",banner.Text);
                var standing=_player.transform.position;
                var atBossDeath=meta.Load(); // Victory counts at the boss death, before the portal.
                Assert.AreEqual(before.victories+1,atBossDeath.victories); Assert.AreEqual(before.runsFinished,atBossDeath.runsFinished);
                CollectionAssert.Contains(atBossDeath.universesCleared,MetaProgressStore.Universe1);
                yield return RunVictoryTestSupport.WaitForPortal(fixture.director);
                Assert.IsFalse(fixture.director.ManualVictoryPresenting); Assert.Less(Vector3.Distance(standing,_player.transform.position),.05f,"No automatic teleport.");
                Assert.AreEqual(RunOutcome.None,route.Session.Outcome,"The player enters the portal when they want.");
                var portal=fixture.director.ManualExtractionPortal; Assert.IsNotNull(portal);
                var agent=_player.GetComponent<UnityEngine.AI.NavMeshAgent>(); Assert.IsTrue(agent.Warp(portal.transform.position));
                Assert.IsTrue(portal.TryEnter()); Assert.AreEqual(RunOutcome.Extracted,route.Session.Outcome);
                Assert.IsFalse(portal.TryEnter()); Assert.IsFalse(fixture.director.TryAbortManualRun(route.Session.Id));
                LogAssert.Expect(LogType.Warning,"Manual return remains pending; retry retains the claimed run result.");
                for(int i=0;i<8;i++) yield return null;
                Assert.AreEqual(ManualReturnState.Failed,fixture.director.ManualReturnStatus); Assert.AreEqual(2,loader.Calls);
                Assert.IsFalse(fixture.director.TryRetryManualReturn(Guid.NewGuid()));
                loader.Succeed=true; Assert.IsTrue(fixture.director.TryRetryManualReturn(route.Session.Id));
                Assert.IsFalse(fixture.director.TryRetryManualReturn(route.Session.Id));
                yield return null; yield return null; yield return null;
                Assert.AreEqual(RunOutcome.Extracted,route.Session.Outcome); Assert.AreEqual(RunPhase.Ended,route.Session.Phase);
                Assert.AreEqual(3,loader.Calls); Assert.IsNull(fixture.director.ManualExtractionPortal);
                Assert.AreEqual(route.Session.Id.ToString(),RunResultsHandoff.Consume(),"Retry retains the expected presentation identity.");
                Assert.IsEmpty(_player.GetComponent<RunBoons>().Acquired);
                var after=meta.Load();
                Assert.AreEqual(before.runsStarted+1,after.runsStarted); Assert.AreEqual(before.runsFinished+1,after.runsFinished);
                Assert.AreEqual(before.victories+1,after.victories); Assert.AreEqual(before.deaths,after.deaths);
                CollectionAssert.Contains(after.universesCleared,MetaProgressStore.Universe1);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualBossBatchDeathWinsAndNeverCreatesPortal()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(); var loader=new TestManualLoader { Succeed=true };
            try
            {
                SetManualField(fixture.director,"_manualSceneLoader",loader);
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=LifecycleRoute(fixture.director); ReachLifecycleBoss(route); Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,10));
                var enemy=fixture.director.GetComponentInChildren<Actor>(); enemy.Died+=actor=>_player.TakeDamage(1000000f);
                enemy.TakeDamage(1000000f); yield return null;
                Assert.AreEqual(RunOutcome.Dead,route.Session.Outcome); Assert.IsNull(fixture.director.ManualExtractionPortal);
                Assert.AreEqual(0,loader.Calls,"Death retains the legacy delay.");
                yield return new WaitForSeconds(.6f); yield return null; yield return null;
                Assert.AreEqual(RunPhase.Ended,route.Session.Phase); Assert.AreEqual(1,loader.Calls);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualDeathDuringVictoryBannerNeverOpensPortalAndKeepsVictory()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(); var loader=new TestManualLoader { Succeed=true };
            try
            {
                SetManualField(fixture.director,"_manualSceneLoader",loader);
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var meta=MetaProgressStore.CreateDefault(); var before=meta.Load();
                var route=LifecycleRoute(fixture.director); ReachLifecycleBoss(route); Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,10));
                fixture.director.GetComponentInChildren<Actor>().TakeDamage(1000000f);
                yield return null; yield return null;
                Assert.IsTrue(fixture.director.ManualVictoryPresenting);
                _player.TakeDamage(1000000f);
                yield return new WaitForSeconds(3.5f); yield return null; yield return null;
                Assert.AreEqual(RunOutcome.Dead,route.Session.Outcome); Assert.IsNull(fixture.director.ManualExtractionPortal);
                Assert.IsFalse(fixture.director.ManualVictoryPresenting); Assert.IsNull(fixture.director.GetComponentInChildren<UniverseVictoryBanner>());
                // The boss died first: the victory stands and the late death only closes the run.
                var after=meta.Load(); Assert.AreEqual(before.victories+1,after.victories); Assert.AreEqual(before.deaths,after.deaths);
                Assert.AreEqual(before.runsFinished+1,after.runsFinished);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator BossDeathClearsRemainingEnemiesAndHazardsOfTheBossRoom()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(); var loader=new TestManualLoader { Succeed=true };
            try
            {
                SetManualField(fixture.director,"_manualSceneLoader",loader);
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=LifecycleRoute(fixture.director); ReachLifecycleBoss(route);
                // A production-shaped boss: Boss rank, so the director attaches SectorBoss and the boss-death hook.
                var bossTemplate=new GameObject("Boss template"); bossTemplate.SetActive(false); _spawned.Add(bossTemplate);
                bossTemplate.AddComponent<Actor>().health=100f; bossTemplate.AddComponent<BoxCollider>();
                bossTemplate.AddComponent<UnityEngine.AI.NavMeshAgent>(); bossTemplate.AddComponent<EnemyAI>().enabled=false;
                typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(route.Session.Route.Node(10),
                    new System.Collections.Generic.List<ManualEnemySpawn>{ new ManualEnemySpawn(bossTemplate,Vector3.zero,EnemyRank.Boss) });
                Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,10));
                var boss=fixture.director.GetComponentInChildren<SectorBoss>()?.GetComponent<Actor>(); Assert.IsNotNull(boss,"The boss spawn carries SectorBoss.");
                var encounterRoot=(GameObject)typeof(ProgressionDirector).GetField("_manualEncounterRoot",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(fixture.director);
                // A produced add enters through the production hook; a hazard zone sits under the encounter root like a caster's zone.
                var add=new GameObject("Boss room add"); add.AddComponent<BoxCollider>(); var addActor=add.AddComponent<Actor>(); addActor.health=100f;
                add.transform.position=boss.transform.position+Vector3.right*2f;
                typeof(ProgressionDirector).GetMethod("OnManualEnemyProduced",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(fixture.director,new object[]{addActor});
                var hazard=new GameObject("Boss room hazard"); hazard.transform.SetParent(encounterRoot.transform,false); hazard.AddComponent<HazardZone>();
                Assert.AreEqual(2,fixture.director.ManualLiveEnemyCount);
                boss.TakeDamage(1000000f);
                yield return null; yield return null; yield return null;
                Assert.IsTrue(addActor==null,"The remaining enemy is removed with the boss."); Assert.IsTrue(hazard==null,"Boss-room hazards are cleared.");
                Assert.AreEqual(0,fixture.director.ManualLiveEnemyCount);
                Assert.AreEqual(RunPhase.ExtractionAvailable,route.Session.Phase,"The cleared room closes through the normal frontier.");
                Assert.IsTrue(fixture.director.ManualVictoryPresenting);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }

        [UnityTest] public IEnumerator ManualDeathBeforePortalRejectsResidualContactAndOldSessionRequests()
        {
            int old=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);
            Assert.IsTrue(TryBakeFullFloor()); BuildPlayer(Vector3.zero); _player.EquipWeapon(WeaponLoadout.LoadSelected());
            var fixture=CreateManualWorldFixture(); var loader=new TestManualLoader { Succeed=true };
            try
            {
                SetManualField(fixture.director,"_manualSceneLoader",loader);
                if(_navMeshInstance.valid) _navMeshInstance.Remove(); fixture.director.gameObject.SetActive(true);
                yield return null; yield return null; yield return null;
                var route=LifecycleRoute(fixture.director); ReachLifecycleBoss(route); Assert.IsTrue(fixture.director.TryEnterNode(route.Session.Id,10));
                fixture.director.GetComponentInChildren<Actor>().TakeDamage(1000000f);
                yield return null; yield return null;
                yield return RunVictoryTestSupport.WaitForPortal(fixture.director);
                var portal=fixture.director.ManualExtractionPortal;
                Assert.IsTrue(_player.GetComponent<UnityEngine.AI.NavMeshAgent>().Warp(portal.transform.position));
                _player.TakeDamage(1000000f); Assert.IsFalse(portal.TryEnter());
                yield return null; Assert.AreEqual(RunOutcome.Dead,route.Session.Outcome);
                var next=BuildManualRewardRoute(); Assert.AreNotEqual(route.Session.Id,next.Session.Id);
                Assert.IsFalse(next.TryCommitDestination(route.Session.Id,1,RewardReady));
                Assert.IsFalse(next.TryCompleteReward(route.Session.Id,1,0,false,RewardReady));
                Assert.IsFalse(next.TryCompleteRepair(route.Session.Id,3,RewardReady));
                Assert.IsFalse(next.TryRequestExtraction(route.Session.Id,RewardReady));
                Assert.AreEqual(RunPhase.RouteChoice,next.Session.Phase); Assert.AreEqual(0,next.Session.MutationAcquisitions);
                Assert.AreEqual(RunNodeState.Available,next.Session.NodeState(1));
                yield return new WaitForSeconds(.6f); yield return null; yield return null;
                Assert.AreEqual(RunOutcome.Dead,route.Session.Outcome); Assert.AreEqual(1,loader.Calls);
            }
            finally { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,old); UnityEngine.Object.DestroyImmediate(fixture.definition); }
        }
    }
}
