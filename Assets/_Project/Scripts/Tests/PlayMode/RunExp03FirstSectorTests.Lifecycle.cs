using System.Collections;
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
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator RefinedNativeDamagePauseControlReuseAndDespawnRemainEncounterOwned()
        {
            SessionState.SetString(RunSeedSource.NextEditorSeedKey,"63");yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
            var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
            player.SetMaxHealth(10000);player.RestoreHealthToMax();Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(7,0,-4)));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
            foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
            var poses=director.GetComponentsInChildren<FirstSectorEnemyPose>();Assert.AreEqual(2,poses.Length);
            yield return VerifyEmitterCover(director,node,player);
            foreach(var pose in poses)Assert.IsTrue(pose.GetComponent<NavMeshAgent>().Warp(player.transform.position+new Vector3(pose.Ranged?-4:1,0,0)));
            float health=player.health,end=Time.time+15;
            while(poses.Any(p=>p.GetComponent<EnemyCombatActions>().ResolvedImpacts==0)){Assert.Less(Time.time,end,"Both real executors must resolve attacks.");yield return null;}
            Assert.Less(player.health,health);yield return Capture("refined-native-damage-mixed-roster");
            var camera=Camera.main;var cameraPosition=camera.transform.position;var cameraRotation=camera.transform.rotation;float cameraSize=camera.orthographicSize;
            var cameraBehaviours=camera.GetComponents<MonoBehaviour>().Where(b=>b.enabled).ToArray();foreach(var behaviour in cameraBehaviours)behaviour.enabled=false;
            try
            {
                foreach(var pose in poses)
                {camera.orthographicSize=1.7f;camera.transform.position=pose.transform.position+new Vector3(2.5f,2.4f,2.5f);camera.transform.LookAt(pose.transform.position+Vector3.up*.4f);yield return Capture((pose.Ranged?"emissor":"fragmento")+"-refined-closeup-real-runtime");}
            }
            finally{camera.transform.SetPositionAndRotation(cameraPosition,cameraRotation);camera.orthographicSize=cameraSize;foreach(var behaviour in cameraBehaviours)if(behaviour)behaviour.enabled=true;}
            Time.timeScale=0;var positions=poses.Select(p=>p.transform.position).ToArray();var feet=poses.SelectMany(p=>p.GetComponentsInChildren<Transform>()).Select(t=>t.position).ToArray();float pausedHp=player.health;
            yield return new WaitForSecondsRealtime(.25f);Assert.AreEqual(pausedHp,player.health);
            var transforms=poses.SelectMany(p=>p.GetComponentsInChildren<Transform>()).ToArray();var feetAfter=transforms.Select(t=>t.position).ToArray();for(int i=0;i<feet.Length;i++)Assert.Less(Vector3.Distance(feet[i],feetAfter[i]),.001f,"Paused transform "+transforms[i].name);Time.timeScale=1;
            foreach(var pose in poses)
            {
                var actor=pose.GetComponent<Actor>();var reaction=pose.GetComponent<CombatReactionController>();
                reaction.ApplyReaction(new HitReactionRequest(player,pose.transform.position,Vector3.forward,HitReactionType.Stagger,HitStrength.Heavy,10000,StanceBreakEffect.Stun,0,.25f));
                actor.TakeDamage(.1f);Assert.IsTrue(reaction.IsControlLocked);
                // A screenshot can take longer than the authored .25s control window on a busy Editor.
                // Freeze the gameplay clock before capture, then verify pause preserves the real lock.
                Time.timeScale=0;yield return Capture((pose.Ranged?"emissor":"fragmento")+"-native-hit-control");
                yield return new WaitForSecondsRealtime(.15f);Assert.IsTrue(reaction.IsControlLocked);Time.timeScale=1;
                yield return new WaitForSeconds(1);Assert.IsFalse(reaction.IsControlLocked);
                long generation=actor.CombatGeneration;actor.gameObject.SetActive(false);yield return null;actor.gameObject.SetActive(true);yield return null;Assert.Greater(actor.CombatGeneration,generation);Assert.IsFalse(actor.IsDead);Assert.IsFalse(reaction.IsControlLocked);
            }
            File.WriteAllText(Path.Combine(_dir,"refined-lifecycle.txt"),"HP real antes="+health+" depois="+player.health+"; impactos="+string.Join("/",poses.Select(p=>p.GetComponent<EnemyCombatActions>().ResolvedImpacts))+"; pausa congela hierarquia; CC finito; nova geração após reativação; sem imunidade. Fixture posicional inicial declarada.");
            foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);yield return null;yield return Capture("refined-native-despawn");yield return new WaitForSeconds(.5f);
            Assert.AreEqual(0,director.GetComponentsInChildren<Actor>().Length);Assert.AreEqual(0,director.GetComponentsInChildren<FirstSectorEnemyDespawn>().Length);
        }
    }
}
