using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        [UnityTest] public IEnumerator Dcc05ArtComparisonsKeepNativePhasesFeetDamageAndCleanup()
        {
            SessionState.SetString(RunSeedSource.NextEditorSeedKey,"0");yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
            var spawns=new[]{"Fragmento","Emissor"}.Select((name,i)=>new ManualEnemySpawn(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp05/"+name+".prefab"),new Vector3(i==0?-5:5,0,1))).ToList();
            typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,spawns);
            player.AddDamageImmunity(director);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));
            // Neutral art comparison only: freeze AI before its first update, allow native pose/IK
            // to settle. Re-enable it for all motion/attack/hit/cleanup observations below.
            var ais=director.GetComponentsInChildren<EnemyAI>();foreach(var ai in ais)ai.enabled=false;
            // Compile the new imported material passes before taking presentation evidence.
            // First-use Editor shader work may otherwise capture Unity's temporary cyan surface.
            foreach(var material in director.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct())
                if(material)for(int pass=0;pass<material.passCount;pass++)ShaderUtil.CompilePass(material,pass,false);
            float shaderDeadline=Time.realtimeSinceStartup+30;
            while(ShaderUtil.anythingCompiling){Assert.Less(Time.realtimeSinceStartup,shaderDeadline,"Wait for actual material passes before comparison captures.");yield return null;}
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            for(int i=0;i<4;i++)yield return null;
            var poses=director.GetComponentsInChildren<FirstSectorEnemyPose>();Assert.AreEqual(2,poses.Length);Time.timeScale=0;
            foreach(string version in new[]{"04","05"}){foreach(var pose in poses)SwapArt05(pose,version);yield return Capture("05-art-"+version+"-normal-camera");}
            var camera=Camera.main;var position=camera.transform.position;var rotation=camera.transform.rotation;float size=camera.orthographicSize;
            var behaviours=camera.GetComponents<MonoBehaviour>().Where(b=>b.enabled).ToArray();var canvases=Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None).Where(c=>c.enabled).ToArray();
            foreach(var b in behaviours)b.enabled=false;foreach(var canvas in canvases)canvas.enabled=false;
            try
            {
                foreach(var pose in poses)
                {
                    camera.orthographicSize=1.35f;camera.transform.position=pose.transform.position+new Vector3(2.5f,2.4f,2.5f);camera.transform.LookAt(pose.transform.position+Vector3.up*.35f);
                    foreach(string version in new[]{"04","05"}){SwapArt05(pose,version);yield return Capture("05-art-"+(pose.Ranged?"Emissor":"Fragmento")+"-"+version+"-neutral-closeup");}
                }
            }
            finally{camera.transform.SetPositionAndRotation(position,rotation);camera.orthographicSize=size;foreach(var b in behaviours)if(b)b.enabled=true;foreach(var canvas in canvases)if(canvas)canvas.enabled=true;Time.timeScale=1;foreach(var ai in ais)if(ai)ai.enabled=true;}
            _poseCaptures.Clear();yield return ObserveNativePoses(director,node);
            var phases=new HashSet<string>();float deadline=Time.time+8;
            while(phases.Count<4&&Time.time<deadline)
            {
                foreach(var pose in poses)
                {
                    if(pose.Phase!="windup"&&pose.Phase!="attack")continue;string key=(pose.Ranged?"Emissor":"Fragmento")+"-"+pose.Phase;if(!phases.Add(key))continue;
                    Time.timeScale=0;try{foreach(string version in new[]{"04","05"}){foreach(var actor in poses)SwapArt05(actor,version);yield return Capture("05-art-phase-"+key+"-"+version);}}
                    finally{foreach(var actor in poses)SwapArt05(actor,"05");Time.timeScale=1;}
                }
                yield return null;
            }
            Assert.AreEqual(4,phases.Count,"Both real native windup/attack phases must remain observable.");
            foreach(var pose in poses)
            {var actor=pose.GetComponent<Actor>();float hp=actor.health;actor.TakeDamage(1);yield return null;Assert.Less(actor.health,hp);Assert.AreEqual("hit",pose.Phase);yield return Capture("05-art-hit-"+(pose.Ranged?"Emissor":"Fragmento"));}
            foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
            yield return null;yield return Capture("05-art-disassembly");yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0,director.GetComponentsInChildren<FirstSectorEnemyDespawn>().Length);
            File.WriteAllText(Path.Combine(_dir,"05-art-fixture.txt"),"Controlled pair of real05 prefabs. Neutral comparison pauses AI only for presentation, with identical camera/light/pose04vs05; native current pose shared for form/material comparison. Then AI, navigation, poses, emission and damage active; native player immunity only in visual fixture. Foot placement checked by ObserveNativePoses. Not human acceptance or natural distribution.");
        }
        private static void SwapArt05(FirstSectorEnemyPose pose,string version)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/FirstSector/RunExp"+version+"/"+(pose.Ranged?"Emissor":"Fragmento")+".prefab");
            var original=source.GetComponentsInChildren<MeshFilter>(true);var live=pose.GetComponentsInChildren<MeshFilter>(true);Assert.AreEqual(original.Length,live.Length);
            for(int i=0;i<live.Length;i++){Assert.AreEqual(original[i].name,live[i].name);live[i].sharedMesh=original[i].sharedMesh;live[i].GetComponent<MeshRenderer>().sharedMaterials=original[i].GetComponent<MeshRenderer>().sharedMaterials;}
        }
    }
}
