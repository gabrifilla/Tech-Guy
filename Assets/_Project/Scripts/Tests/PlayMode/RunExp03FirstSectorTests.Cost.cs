using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator IntegratedActiveCost8()=>IntegratedCost(8);
        [UnityTest] public IEnumerator IntegratedActiveCost32()=>IntegratedCost(32);
        [UnityTest] public IEnumerator IntegratedActiveCost64()=>IntegratedCost(64);
        [Serializable] private sealed class FrameCost
        { public int frame,active,moving,windups,projectiles,drawCalls,batches,queue,roots;public float dtMs;public long[] markers; }
        [Serializable] private sealed class IntegratedCostReport
        { public int count,renderers,triangles,materials,skinned,skinnedTriangles,impacts,droppedQueue;public double spawnMs,cleanupMs;public string fixture;public string[] markerNames;public FrameCost[] samples; }
        private IEnumerator IntegratedCost(int count)
        {
            SessionState.SetString(RunSeedSource.NextEditorSeedKey,"0");yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
            var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");Assert.IsFalse(content.HasCover(0,node));
            // Controlled population only: replace this session snapshot's roster, then use the real director.
            // No published data changes; native AI, damage, navigation, coordinator, membership and poses stay active.
            var pool=new[]{content.AdditionalEnemies[0],content.AdditionalEnemies[1],AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Grunt.prefab")};
            Assert.IsTrue(pool.All(p=>p));
            var spawns=new List<ManualEnemySpawn>();
            for(int i=0;i<count;i++){int x=i%8,z=i/8;spawns.Add(new ManualEnemySpawn(pool[i%3],new Vector3((x-3.5f)*1.6f,0,(z-3.5f)*1.6f)));}
            typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,spawns);
            Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+Vector3.forward*8));player.AddDamageImmunity(director);
            var watch=System.Diagnostics.Stopwatch.StartNew();Assert.IsTrue(director.TryEnterNode(session.Id,1));watch.Stop();double spawnMs=watch.Elapsed.TotalMilliseconds;
            var actors=director.GetComponentsInChildren<Actor>();Assert.AreEqual(count,actors.Length);
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            yield return new WaitForSeconds(1);
            var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);
            var wanted=new[]{"Main Thread","BehaviourUpdate","NavMeshManager","Components.NavMeshAgent.State","RUNEXP03.EnemyPose","GC Allocated In Frame","Draw Calls Count","Batches Count","MeshSkinning.Update","MeshSkinning.Skin"};
            var recorders=new Dictionary<string,ProfilerRecorder>();
            var runtime=player.GetComponent<ProtocolRuntime>();Assert.IsNotNull(runtime);Assert.IsNotNull(runtime.Budget);
            try
            {
                foreach(var handle in handles){var d=ProfilerRecorderHandle.GetDescription(handle);if(wanted.Contains(d.Name)&&!recorders.ContainsKey(d.Name))recorders.Add(d.Name,ProfilerRecorder.StartNew(d.Category,d.Name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame));}
                var rows=new List<FrameCost>();float end=Time.time+5;
                while(Time.time<end)
                {
                    yield return null;
                    rows.Add(new FrameCost{frame=Time.frameCount,dtMs=Time.unscaledDeltaTime*1000,active=actors.Count(a=>a&&a.GetComponent<EnemyAI>().enabled&&a.GetComponent<NavMeshAgent>().isOnNavMesh),moving=actors.Count(a=>a&&a.GetComponent<NavMeshAgent>().velocity.sqrMagnitude>.01f),windups=actors.Count(a=>a&&a.GetComponent<EnemyCombatActions>().IsWindingUp),projectiles=Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length,drawCalls=UnityStats.drawCalls,batches=-1,queue=runtime.Budget.MaximumQueue,roots=runtime.Budget.LiveRoots,markers=recorders.Select(p=>p.Value.LastValue).ToArray()});
                }
                Assert.IsTrue(rows.All(r=>r.active==count));
                var renderers=director.GetComponentsInChildren<MeshRenderer>();
                var report=new IntegratedCostReport{count=count,renderers=actors.Sum(a=>a.GetComponentsInChildren<MeshRenderer>().Length),triangles=actors.Sum(a=>a.GetComponentsInChildren<MeshFilter>().Sum(m=>m.sharedMesh.triangles.Length/3)),materials=actors.SelectMany(a=>a.GetComponentsInChildren<MeshRenderer>()).SelectMany(r=>r.sharedMaterials).Distinct().Count(),skinned=actors.Sum(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>().Length),spawnMs=spawnMs,impacts=actors.Sum(a=>a.GetComponent<EnemyCombatActions>().ResolvedImpacts),fixture="Editor; roster controlado 1:1:1 Fragmento/Emissor/Grunt no snapshot; spawn pelo diretor nativo. AI/nav/dano/animações ativos. Jogador imune, moedas removidas apenas na fixture. Amostrador LINQ/UTF acrescenta CPU e GC. BehaviourUpdate é agregado, não CPU isolada da AI. Sem aprovação de orçamento standalone.",markerNames=recorders.Keys.ToArray(),samples=rows.ToArray()};
                report.skinnedTriangles=actors.SelectMany(a=>a.GetComponentsInChildren<SkinnedMeshRenderer>()).Sum(r=>r.sharedMesh.triangles.Length/3);report.droppedQueue=runtime.Budget.DroppedQueueItems;
                yield return Capture("integrated-active-"+count);
                watch.Restart();foreach(var actor in actors)actor.TakeDamage(1000000);
                float deadline=Time.realtimeSinceStartup+8;while(session.Phase==RunPhase.Encounter){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                yield return new WaitForSeconds(.4f);watch.Stop();report.cleanupMs=watch.Elapsed.TotalMilliseconds;
                Assert.AreEqual(0,director.GetComponentsInChildren<Actor>().Length);Assert.AreEqual(0,director.GetComponentsInChildren<FirstSectorEnemyDespawn>().Length);
                Assert.AreEqual(0,Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length);
                File.WriteAllText(Path.Combine(_dir,"integrated-active-"+count+".json"),JsonUtility.ToJson(report,true));
            }
            finally{foreach(var recorder in recorders.Values)recorder.Dispose();}
        }
    }
}
