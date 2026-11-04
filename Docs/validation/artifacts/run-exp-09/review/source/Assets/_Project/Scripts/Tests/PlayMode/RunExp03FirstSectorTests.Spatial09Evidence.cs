using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator Spatial09TripleNativeWindElectricityFireNearRingMass()
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/triple/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0); ulong seed=0; while(FirstSectorRoomLayout.SelectFamily(seed,1)!=FirstSectorRoomFamily.FragmentedRing)seed++; SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
            EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<5;i++)yield return null;
            var director=Object.FindAnyObjectByType<ProgressionDirector>();var player=Object.FindAnyObjectByType<PlayerActor>();var session=director.ManualSession;var run=player.GetComponent<RunBoons>();var runtime=player.GetComponent<ProtocolRuntime>();
            player.AddDamageImmunity(director);
            foreach(string id in new[]{TitanicFistsDefinition.Id,"conductor","detonation"})
                Assert.IsTrue((bool)typeof(RunBoons).GetMethod("ApplyOffer",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(run,new object[]{new RunBoons.Offer(id,"fixture","Assisted visual comparison; not natural acquisition"),false}));
            var node=session.Route.Node(1);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-3,0,-3)));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            var enemies=director.GetComponentsInChildren<Actor>().OrderBy(a=>EntityId.ToULong(a.GetEntityId())).ToArray();Assert.GreaterOrEqual(enemies.Length,4);
            Vector3 origin=player.transform.position;
            for(int i=0;i<enemies.Length;i++)
            {
                if(enemies[i].TryGetComponent<EnemyAI>(out var ai))ai.enabled=false;
                if(enemies[i].TryGetComponent<EnemyCombatActions>(out var actions))actions.Cancel();
                Assert.IsTrue(enemies[i].GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-11+i*1.3f,0,-10)));
                enemies[i].SetMaxHealth(1000);enemies[i].RestoreHealthToMax();
            }
            var controls=player.GetComponent<CharControlScript>();
            Assert.IsTrue(enemies[0].GetComponent<NavMeshAgent>().Warp(origin+Vector3.forward*1.2f));yield return new WaitForSeconds(.25f);
            yield return NativeBasic08(controls,player);yield return NativeBasic08(controls,player);
            Assert.IsTrue(enemies[0].GetComponent<NavMeshAgent>().Warp(origin+Vector3.forward*5.8f));
            Assert.IsTrue(enemies[1].GetComponent<NavMeshAgent>().Warp(origin+new Vector3(2,0,5.8f)));
            Assert.IsTrue(enemies[2].GetComponent<NavMeshAgent>().Warp(origin+new Vector3(4,0,5.8f)));
            Assert.IsTrue(enemies[3].GetComponent<NavMeshAgent>().Warp(origin+new Vector3(-2,0,3)));
            enemies[1].SetMaxHealth(.5f);enemies[1].RestoreHealthToMax();Physics.SyncTransforms();
            using(var receipts=new RunStatistics(player,session.Id,1,Time.realtimeSinceStartupAsDouble,Time.timeAsDouble))
            {
                foreach(var enemy in enemies)receipts.Watch(enemy);
                yield return NativeBasic08(controls,player);
                float until=Time.realtimeSinceStartup+5;
                while(runtime.FirstTitanicPosition.HasValue&&runtime.FirstTitanicPosition.Value.z<origin.z+2.3f){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                Assert.Greater(runtime.PendingTitanicWaves,0);
                var attack=enemies[3].GetComponent<EnemyCombatActions>();attack.StartCoroutine(attack.Perform(EnemyAttackKind.HeavySlam,player,10,()=>true));
                yield return null;Assert.IsTrue(attack.IsWindingUp);
                yield return Capture("09-triple-warning");yield return new WaitForSeconds(.1f);yield return Capture("09-triple-travel");
                while(!receipts.Snapshot().RecentDamage.Any(r=>r.ProducerId==TitanicFistsDefinition.Id)){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                var data=receipts.Snapshot();Assert.IsTrue(data.RecentDamage.Any(r=>r.Producer=="Conductor"));Assert.IsTrue(data.RecentDamage.Any(r=>r.Producer=="Detonation"));
                File.WriteAllText(Path.Combine(_dir,"native-damage.json"),JsonUtility.ToJson(data,true));yield return Capture("09-triple-confirmed");
            }
            // Negative case against the real ring mass: terminal wind must stop before its near face.
            enemies=enemies.Where(e=>e&&!e.IsDead).ToArray();Assert.Greater(enemies.Length,0);
            for(int i=0;i<enemies.Length;i++)Assert.IsTrue(enemies[i].GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-11+i*1.3f,0,-10)));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(enemies[0].GetComponent<NavMeshAgent>().Warp(node.Center+Vector3.right*11));
            controls.CancelCombo();float hpBefore=enemies[0].health;int resolved=runtime.TitanicWavesResolved;
            for(int i=0;i<3;i++)
            {
                float end=Time.realtimeSinceStartup+4;while(!controls.TryDirectionalBasicAttack(node.Center+Vector3.right*11)){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
                while(controls.HasScheduledBasic){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
            }
            float stop=Time.realtimeSinceStartup+5;while(runtime.PendingTitanicWaves>0||runtime.TitanicWavesResolved==resolved){Assert.Less(Time.realtimeSinceStartup,stop);yield return null;}
            Assert.Less(runtime.LastTitanicEndPosition.x,node.Center.x+2);Assert.AreEqual(hpBefore,enemies[0].health,"Real mass blocks native wind damage");yield return Capture("09-triple-real-mass-blocks-wind");
            File.WriteAllText(Path.Combine(_dir,"fixture.txt"),"Assisted visual triple: real native boons temporarily applied in memory; AI disabled for controlled target positions, HP changed on target to trigger existing kill detonation, native attack/HP/proc lineage asserted, real HeavySlam telegraph. Separate three-weapon tests have natural AI. No Burn/DoT or boon tuning authored.");
        }

        [UnityTest] public IEnumerator Spatial09RingMassFadesWithoutChangingPhysics()
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/occlusion/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=FirstSectorRoomFamily.FragmentedRing)seed++;
            SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;
            var d=Object.FindAnyObjectByType<ProgressionDirector>();var p=Object.FindAnyObjectByType<PlayerActor>();var n=d.ManualSession.Route.Node(1);
            Assert.IsTrue(p.GetComponent<NavMeshAgent>().Warp(n.Center+new Vector3(5.5f,0,6.5f)));yield return new WaitForSeconds(.8f);
            var mass=d.ManualEnvironment.Node(1).Arena.GetComponentsInChildren<BoxCollider>().Single(c=>c.name=="Central archive mass / ring");
            var rig=Camera.main.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();Assert.IsTrue(rig.IsOcclusionFaded(mass));Assert.IsTrue(mass.enabled);Assert.AreEqual(40,Camera.main.fieldOfView);
            Assert.IsFalse(NavMesh.SamplePosition(n.Center+new Vector3(5.5f,0,0),out var hit,.15f,NavMesh.AllAreas));yield return Capture("09-mass-occlusion-normal-camera");
            File.WriteAllText(Path.Combine(_dir,"passed.txt"),"Native normal camera FOV40; player warped onto north ring circulation, mass observed faded via existing fader. Collider stays enabled; central hole has no NavMesh. No enemy encounter or physics disabled. No new occlusion algorithm.");
        }

        [UnityTest] public IEnumerator Spatial09EditorCostComparison()
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/performance/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            var rows=new List<string>{"family,seed,load_bake_ms,nav_bake_ms,frame_ms,cpu_ns,draw_calls,allocated_bytes,active_enemies,renderers,colliders,nav_triangles"};
            foreach(var family in new[]{FirstSectorRoomFamily.Rectangle,FirstSectorRoomFamily.AsymmetricCourtyard,FirstSectorRoomFamily.LChamber,FirstSectorRoomFamily.ElongatedGallery,FirstSectorRoomFamily.FragmentedRing})
            {
                ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family)seed++;
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());var timer=System.Diagnostics.Stopwatch.StartNew();
                EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;timer.Stop();
                var d=Object.FindAnyObjectByType<ProgressionDirector>();var p=Object.FindAnyObjectByType<PlayerActor>();var s=d.ManualSession;var n=s.Route.Node(1);p.AddDamageImmunity(d);
                Assert.IsTrue(d.TryCommitDestination(s.Id,1));Assert.IsTrue(p.GetComponent<NavMeshAgent>().Warp(n.Center));Assert.IsTrue(d.TryEnterNode(s.Id,1));
                foreach(var coin in d.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                yield return new WaitForSeconds(.6f);
                using(var cpu=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
                {
                    float until=Time.realtimeSinceStartup+3;
                    while(Time.realtimeSinceStartup<until)
                    {
                        yield return null;rows.Add(string.Join(",",family,seed,timer.Elapsed.TotalMilliseconds.ToString(CultureInfo.InvariantCulture),d.ManualEnvironment.LastNavigationBuildMilliseconds.ToString(CultureInfo.InvariantCulture),(Time.unscaledDeltaTime*1000).ToString(CultureInfo.InvariantCulture),cpu.Valid?cpu.LastValue:-1,UnityStats.drawCalls,Profiler.GetTotalAllocatedMemoryLong(),d.GetComponentsInChildren<Actor>().Length,d.ManualEnvironment.GetComponentsInChildren<Renderer>().Length,d.ManualEnvironment.GetComponentsInChildren<Collider>().Length,NavMesh.CalculateTriangulation().indices.Length/3));
                    }
                }
                yield return Capture("09-cost-"+family);
            }
            File.WriteAllLines(Path.Combine(_dir,"frames.csv"),rows);
            File.WriteAllText(Path.Combine(_dir,"conditions.txt"),"Editor only; five different seeded decks, native enemy count varies, 3s each after .6s warmup. Geometry/bake cost is full deck; draw calls are current Game View. Snapshot collection overhead included. Total allocated memory is resident, NOT per-frame GC. MainThread optional recorder; GPU, isolated occlusion CPU, isolated navigation CPU, standalone, equal-density A/B and sustained play NOT MEASURED. "+SystemInfo.processorType+" / "+SystemInfo.graphicsDeviceName+" / "+Screen.width+"x"+Screen.height+" / "+Application.unityVersion);
        }
    }
}
