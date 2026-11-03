using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using Object=UnityEngine.Object;

/// <summary>Explicit review-scene/command-line harness; never attached to production scenes.</summary>
public sealed class RunExp05StandaloneProfile : MonoBehaviour
{
    [SerializeField] private GameObject[] _oldArt,_newArt;
    [SerializeField] private GameObject _grunt;
    [Serializable] private sealed class Sample {public float frameMs;public double gpuMs;public long memory;public int active,moving,windups,projectiles;public long[] markers;}
    [Serializable] private sealed class Report
    {
        public string version,unity,gpu,cpu,fixture;public int population,renderers,triangles,materials,width,height,impacts;public string[] markerNames;public Sample[] samples;
    }
    private string _output;
    private IEnumerator Start()
    {
        if(!Environment.GetCommandLineArgs().Contains("--runexp05-profile")){enabled=false;yield break;}
        DontDestroyOnLoad(gameObject);Application.runInBackground=true;QualitySettings.vSyncCount=0;Application.targetFrameRate=-1;
        _output=Path.Combine(Application.dataPath,"../ProfileResults");Directory.CreateDirectory(_output);
        foreach(int count in new[]{8,32,64})
        {
            yield return SceneManager.LoadSceneAsync("RunExp05ProfileFirstSector");for(int frame=0;frame<5;frame++)yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;
            if(director==null||session==null)throw new Exception("Actual FirstSector director/session missing");
            var node=session.Route.Nodes.First(n=>n.Kind==ManualNodeKind.Combat);var roster=new List<ManualEnemySpawn>();
            for(int i=0;i<count;i++)roster.Add(new ManualEnemySpawn(i%3==2?_grunt:_newArt[i%3],new Vector3((i%8-3.5f)*1.6f,0,(i/8-3.5f)*1.6f)));
            typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,roster);
            player.AddDamageImmunity(this);
            if(!director.TryCommitDestination(session.Id,node.Id)||!player.GetComponent<NavMeshAgent>().Warp(node.Center+Vector3.forward*8)||!director.TryEnterNode(session.Id,node.Id))throw new Exception("Native encounter entry failed");
            foreach(var drop in director.GetComponentsInChildren<CoinDrop>())Destroy(drop);
            var actors=director.GetComponentsInChildren<Actor>();if(actors.Length!=count)throw new Exception("Population mismatch");
            var rooms=Object.FindObjectsByType<FirstSectorEnvironmentArt>(FindObjectsSortMode.None);
            foreach(string version in count==32?new[]{"05","04"}:new[]{"04","05"})
            {
                bool current=version=="05";foreach(var room in rooms)room.SetVisible(current);
                foreach(var pose in director.GetComponentsInChildren<FirstSectorEnemyPose>())
                {
                    var source=(current?_newArt:_oldArt)[pose.Ranged?1:0];var from=source.GetComponentsInChildren<MeshFilter>();var to=pose.GetComponentsInChildren<MeshFilter>();
                    for(int i=0;i<to.Length;i++){to[i].sharedMesh=from[i].sharedMesh;to[i].GetComponent<MeshRenderer>().sharedMaterials=from[i].GetComponent<MeshRenderer>().sharedMaterials;}
                    typeof(FirstSectorEnemyPose).GetField("_laminatedPresentation",BindingFlags.NonPublic|BindingFlags.Instance).SetValue(pose,current);
                }
                yield return new WaitForSeconds(1.5f);
                var handles=new List<ProfilerRecorderHandle>();ProfilerRecorderHandle.GetAvailable(handles);var recorders=new Dictionary<string,ProfilerRecorder>();
                var wanted=new[]{"Main Thread","BehaviourUpdate","NavMeshManager","RUNEXP03.EnemyPose","GC Allocated In Frame","Draw Calls Count","Batches Count","MeshSkinning.Update"};
                try
                {
                    foreach(var handle in handles){var description=ProfilerRecorderHandle.GetDescription(handle);if(wanted.Contains(description.Name)&&!recorders.ContainsKey(description.Name))recorders.Add(description.Name,ProfilerRecorder.StartNew(description.Category,description.Name,1,ProfilerRecorderOptions.Default|ProfilerRecorderOptions.SumAllSamplesInFrame));}
                    var samples=new List<Sample>();float end=Time.realtimeSinceStartup+4;var timing=new FrameTiming[1];
                    while(Time.realtimeSinceStartup<end)
                    {
                        FrameTimingManager.CaptureFrameTimings();yield return null;
                        double gpu=FrameTimingManager.GetLatestTimings(1,timing)>0?timing[0].gpuFrameTime:-1;
                        samples.Add(new Sample{frameMs=Time.unscaledDeltaTime*1000,gpuMs=gpu>0?gpu:-1,memory=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong(),active=actors.Count(a=>a&&a.GetComponent<EnemyAI>().enabled&&a.GetComponent<NavMeshAgent>().isOnNavMesh),moving=actors.Count(a=>a&&a.GetComponent<NavMeshAgent>().velocity.sqrMagnitude>.01f),windups=actors.Count(a=>a&&a.GetComponent<EnemyCombatActions>().IsWindingUp),projectiles=Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None).Length,markers=recorders.Values.Select(r=>r.LastValue).ToArray()});
                    }
                    var renderers=actors.SelectMany(a=>a.GetComponentsInChildren<MeshRenderer>()).ToArray();
                    var report=new Report{version=version,unity=Application.unityVersion,gpu=SystemInfo.graphicsDeviceName,cpu=SystemInfo.processorType,population=count,width=Screen.width,height=Screen.height,renderers=renderers.Length,triangles=actors.Sum(a=>a.GetComponentsInChildren<MeshFilter>().Sum(f=>f.sharedMesh.triangles.Length/3)),materials=renderers.SelectMany(r=>r.sharedMaterials).Distinct().Count(),impacts=actors.Sum(a=>a.GetComponent<EnemyCombatActions>().ResolvedImpacts),markerNames=recorders.Keys.ToArray(),samples=samples.ToArray(),fixture="Standalone release; same native controlled 1:1:1 cohort per04/05 pair, AI/nav/damage/poses active, player immune and idle. All combat-room environment layers toggled for each version. Sequential pairs (32 reverses order), ongoing AI positions differ; sampler adds LINQ/GC. FirstSector scene copy with only seed0 pinned for diagnostic build; production scene unchanged. Not hardware budget, complete combat benchmark or isolated GPU cost. -1 GPU means unavailable. Skinned Grunt triangles excluded from rigid triangle count."};
                    File.WriteAllText(Path.Combine(_output,"standalone-"+count+"-"+version+".json"),JsonUtility.ToJson(report,true));ScreenCapture.CaptureScreenshot(Path.Combine(_output,"standalone-"+count+"-"+version+".png"));yield return null;
                    if(samples.Any(s=>s.active!=count))throw new Exception("An enemy left the active cohort");
                }
                finally{foreach(var recorder in recorders.Values)recorder.Dispose();}
            }
            foreach(var actor in actors)actor.TakeDamage(1000000);yield return new WaitForSeconds(.6f);
        }
        File.WriteAllText(Path.Combine(_output,"complete.txt"),"All six native active-cohort samples completed.");Application.Quit(0);
    }
}
