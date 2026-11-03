using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
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
        [UnityTest] public IEnumerator Art04BeforeAfterUsesIdenticalRuntimeCameraAndPreservesNativePresentation()
        {
            // Open arena prevents the comparison camera's covers from hiding either silhouette.
            // Cover gameplay is exercised separately; only the presentation fixture changes here.
            SessionState.SetString(RunSeedSource.NextEditorSeedKey,"0");yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
            var names=new[]{"Fragmento","Emissor"};var spawns=names.Select((name,i)=>new ManualEnemySpawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp04/"+name+".prefab"),new Vector3(i==0?-6:6,0,0))).ToList();
            typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,spawns);
            player.AddDamageImmunity(director);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            var poses=director.GetComponentsInChildren<FirstSectorEnemyPose>();Assert.AreEqual(2,poses.Length);yield return new WaitForSeconds(1);
            Time.timeScale=0;
            foreach(string version in new[]{"03","04"})
            {
                foreach(var pose in poses)SwapArtMeshes(pose,version);
                yield return Capture("04-art-comparison-"+version+"-normal-camera");
            }
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
            var cameraBehaviours=camera.GetComponents<MonoBehaviour>().Where(b=>b.enabled).ToArray();foreach(var behaviour in cameraBehaviours)behaviour.enabled=false;
            try
            {
                foreach(var pose in poses)
                {
                    camera.orthographicSize=1.65f;camera.transform.position=pose.transform.position+new Vector3(2.5f,2.4f,2.5f);camera.transform.LookAt(pose.transform.position+Vector3.up*.4f);
                    foreach(string version in new[]{"03","04"}){SwapArtMeshes(pose,version);yield return Capture("04-art-"+(pose.Ranged?"Emissor":"Fragmento")+"-"+version+"-identical-closeup");}
                }
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;foreach(var behaviour in cameraBehaviours)if(behaviour)behaviour.enabled=true;Time.timeScale=1;}
            _poseCaptures.Clear();yield return ObserveNativePoses(director,node);
            yield return CaptureArtAttackComparisons(poses);
            foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
            yield return null;yield return Capture("04-art-disassembly");yield return new WaitForSeconds(.5f);Assert.AreEqual(0,director.GetComponentsInChildren<FirstSectorEnemyDespawn>().Length);
        }
        private IEnumerator CaptureArtAttackComparisons(FirstSectorEnemyPose[] poses)
        {
            var captured=new System.Collections.Generic.HashSet<string>();float deadline=Time.time+8;
            while(captured.Count<4&&Time.time<deadline)
            {
                foreach(var pose in poses)
                {
                    if(pose.Phase!="windup"&&pose.Phase!="attack")continue;
                    string key=(pose.Ranged?"Emissor":"Fragmento")+"-"+pose.Phase;if(!captured.Add(key))continue;
                    // A native executor reached this phase. Freeze that exact instant for both meshes;
                    // no animation trigger or offensive VFX is fabricated by the comparison.
                    Time.timeScale=0;
                    try
                    {
                        foreach(string version in new[]{"03","04"})
                        {foreach(var actor in poses)SwapArtMeshes(actor,version);yield return Capture("04-art-phase-"+key+"-"+version+"-normal-camera");}
                    }
                    finally{foreach(var actor in poses)SwapArtMeshes(actor,"04");Time.timeScale=1;}
                }
                yield return null;
            }
            Assert.AreEqual(4,captured.Count,"Both native windup and attack phases must have paired comparisons.");
        }
        private static void SwapArtMeshes(FirstSectorEnemyPose pose,string version)
        {
            string name=pose.Ranged?"Emissor":"Fragmento";var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp"+version+"/"+name+".prefab");
            var before=source.GetComponentsInChildren<MeshFilter>(true);var live=pose.GetComponentsInChildren<MeshFilter>(true);Assert.AreEqual(before.Length,live.Length);
            for(int i=0;i<live.Length;i++){Assert.AreEqual(before[i].name,live[i].name);live[i].sharedMesh=before[i].sharedMesh;}
        }
    }
}
