using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    [Category("RUNEXP03FirstSector")]
    public sealed partial class RunExp03FirstSectorTests : RunSummaryIsolatedPlayModeFixture
    {
        private bool _hadWeapon,_assist,_hadCoins;private int _weapon,_coins;private string _seed;
        private string _dir;
        [SetUp] public void Preserve()
        {
            _hadWeapon=PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey);_weapon=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey);
            _hadCoins=PlayerPrefs.HasKey(CurrencyWallet.PreferenceKey);_coins=PlayerPrefs.GetInt(CurrencyWallet.PreferenceKey);
            _assist=TitanicConductorPlaytestRewards.Enabled;SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey,false);
            _seed=SessionState.GetString(RunSeedSource.NextEditorSeedKey,"");
            _poseCaptures.Clear();_losVerified=false;
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-03/firstsector/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
        }
        [TearDown] public void Restore()
        {
            // A live Spawner can produce CoinDrop after the initial fixture cleanup.
            // Bank state belongs to the user, even when a fixture fails before room completion.
            foreach(var pickup in Object.FindObjectsByType<CoinPickup>(FindObjectsSortMode.None))Object.DestroyImmediate(pickup.gameObject);
            if(_hadCoins)PlayerPrefs.SetInt(CurrencyWallet.PreferenceKey,_coins);else PlayerPrefs.DeleteKey(CurrencyWallet.PreferenceKey);
            if(_hadWeapon)PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,_weapon);else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
            SessionState.SetBool(TitanicConductorPlaytestRewards.SessionKey,_assist);
            if(_seed.Length==0)SessionState.EraseString(RunSeedSource.NextEditorSeedKey);else SessionState.SetString(RunSeedSource.NextEditorSeedKey,_seed);
            PlayerPrefs.Save();Time.timeScale=1;
        }
        [UnityTest] public IEnumerator GauntletNaturalSeed0ReachesBossTicketsAndNexus()=>Route(0,0);
        [UnityTest] public IEnumerator SpearNaturalSeed1ReachesBossTicketsAndNexus()=>Route(2,1);
        [UnityTest] public IEnumerator BowNaturalSeed2ReachesBossTicketsAndNexus()=>Route(1,2);
        private IEnumerator Capture(string name)
        {
            string path=Path.Combine(_dir,name+".png");ScreenCapture.CaptureScreenshot(path);float end=Time.realtimeSinceStartup+5;
            while(!File.Exists(path)){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}yield return null;
        }
        private IEnumerator Route(int weapon,ulong seed)
        {
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
            yield return SceneManager.LoadSceneAsync("FirstSector");yield return null;yield return null;yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var run=player.GetComponent<RunBoons>();
            Assert.IsNotNull(session);Assert.AreEqual(seed,session.Route.RewardSeed);Assert.IsFalse(session.Route.TitanicConductorPlaytest);Assert.IsTrue(director.ManualEnvironment.HasGeneratedLayout);
            var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
            // Choose among real available paths; no injected spawns, graph nodes, reward tickets or grants.
            var path=session.Route.Paths.OrderByDescending(p=>p.Sum(id=>session.Route.Node(id).Spawns.Count(s=>s.Prefab.name=="Fragmento"||s.Prefab.name=="Emissor")) + p.Count(id=>content.HasCover(seed,session.Route.Node(id)))*2).First();
            var rows=new List<string>{"seed="+seed+" weapon="+weapon+" path="+string.Join("/",path)+"; completion automated; player immune only in fixture"};
            player.AddDamageImmunity(director);bool sawCover=false;var seen=new HashSet<string>();int tickets=0;
            foreach(int nodeId in path.Skip(1))
            {
                var node=session.Route.Node(nodeId);Assert.IsTrue(director.TryCommitDestination(session.Id,nodeId));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,nodeId));
                if(session.Phase==RunPhase.RepairPending){Assert.IsTrue(director.TryResolveRepair(session.Id,nodeId,false));yield return null;continue;}
                yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                rows.Add(nodeId+":"+node.Kind+" cover="+content.HasCover(seed,node)+" "+string.Join("|",node.Spawns.Select(s=>s.Prefab.name)));
                foreach(var actor in director.GetComponentsInChildren<Actor>())
                {
                    var nav=actor.GetComponent<NavMeshAgent>();Assert.IsTrue(nav && nav.isOnNavMesh,actor.name);
                    var route=new NavMeshPath();Assert.IsTrue(nav.CalculatePath(player.transform.position,route));Assert.AreEqual(NavMeshPathStatus.PathComplete,route.status,actor.name);
                    if(actor.GetComponent<FirstSectorEnemyPose>())seen.Add(actor.name.StartsWith("Fragmento")?"Fragmento":"Emissor");
                }
                bool covered=content.HasCover(seed,node);sawCover|=covered;
                if(covered)
                {
                    foreach(float x in new[]{-node.ArenaSize.x*.5f+2,node.ArenaSize.x*.5f-2})
                    {var navPath=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(node.Center+new Vector3(x,0,-6),node.Center+new Vector3(x,0,6),NavMesh.AllAreas,navPath));Assert.AreEqual(NavMeshPathStatus.PathComplete,navPath.status);}
                    Assert.IsTrue(Physics.Raycast(node.Center+new Vector3(-3,.8f,-5),Vector3.forward,7,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));
                }
                if(node.Kind==ManualNodeKind.Combat)
                {
                    yield return new WaitForSeconds(1);yield return Capture("node-"+nodeId+(covered?"-cover":"-open")+"-combat");
                    if(director.GetComponentsInChildren<FirstSectorEnemyPose>().Length>0)yield return ObserveNativePoses(director,node);
                    if(covered&&!_losVerified)yield return VerifyEmitterCover(director,node,player);
                }
                foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
                float deadline=Time.realtimeSinceStartup+8;
                while(session.Phase==RunPhase.Encounter||session.Phase==RunPhase.FinalResolution){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                while(session.Phase==RunPhase.RewardPending)
                {
                    yield return null;if(!run.IsChoosing)Assert.IsTrue(director.TryOpenManualReward(session.Id,nodeId,session.RewardOrdinal));
                    Assert.Greater(run.Choices.Count,0);tickets++;Assert.IsTrue(run.Choose(0));Assert.IsFalse(run.Choose(0));
                }
            }
            Assert.IsTrue(sawCover,"Selected reproducible route must demonstrate cover.");Assert.Greater(seen.Count,0);Assert.Greater(tickets,0);
            Assert.AreEqual(RunPhase.ExtractionAvailable,session.Phase);File.WriteAllLines(Path.Combine(_dir,"route.txt"),rows);
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(director.ManualExtractionPortal.transform.position));Assert.IsTrue(director.ManualExtractionPortal.TryEnter());
            float end=Time.realtimeSinceStartup+8;while(SceneManager.GetActiveScene().name!="NexusBlockout"){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
            Assert.AreEqual(RunPhase.Ended,session.Phase);
        }
    }
}
