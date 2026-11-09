using System;
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
        [Explicit("QUARENTENA-F0: rota/salas publicadas 09/09B; causa nao determinada")]
        [UnityTest] public IEnumerator ConsolidatedRolesAlonePairAndMixedUseNativeExecutorsInBothLayouts()
        {
            var groups=new[]{new[]{"Fragmento"},new[]{"Emissor"},new[]{"Fragmento","Emissor"},new[]{"Fragmento","Emissor","Grunt","Healer"},new[]{"Fragmento","Emissor","Hooker"},new[]{"Fragmento","Emissor","Hooker","Hooker"}};
            var log=new List<string>{"Explicit controlled roster fixture, real FirstSector director/AI/damage; no immunity. Not natural distribution evidence."};
            foreach(ulong seed in new ulong[]{0,63})foreach(var names in groups)
            {
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());yield return SceneManager.LoadSceneAsync("FirstSector");for(int frame=0;frame<4;frame++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
                var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
                var spawns=names.Select((name,i)=>new ManualEnemySpawn(name=="Fragmento"||name=="Emissor"?content.AdditionalEnemies.Single(p=>p.name==name):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+name+".prefab"),new Vector3(i<2?7:-7,0,-2+i*2))).ToList();
                typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,spawns);
                player.SetMaxHealth(10000);player.RestoreHealthToMax();Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(7,0,-6)));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                var candidates=director.GetComponentsInChildren<FirstSectorEnemyPose>();Assert.Greater(candidates.Length,0);float hp=player.health;
                foreach(var actor in director.GetComponentsInChildren<Actor>())
                {var path=new NavMeshPath();Assert.IsTrue(actor.GetComponent<NavMeshAgent>().CalculatePath(player.transform.position,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                // A ranged impact counter marks emission; the projectile still needs to travel.
                float end=Time.time+12;while(candidates.Any(p=>p.GetComponent<EnemyCombatActions>().ResolvedImpacts==0)||player.health>=hp){Assert.Less(Time.time,end,string.Join("/",names));yield return null;}
                Assert.Less(player.health,hp,"Real enemy damage must resolve.");
                yield return Capture("04-roles-"+seed+"-"+string.Join("-",names));
                if(names.Contains("Hooker"))yield return VerifyMixedHookRecovery(director,node,player,names.Count(n=>n=="Hooker"));
                if(seed==63&&names.Contains("Emissor")&&!names.Contains("Hooker"))yield return VerifyEmitterCover(director,node,player);
                log.Add("seed="+seed+" roster="+string.Join("/",names)+" HP="+hp+"->"+player.health+" impacts="+string.Join("/",candidates.Select(p=>p.GetComponent<EnemyCombatActions>().ResolvedImpacts)));
                foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
                end=Time.realtimeSinceStartup+8;while(session.Phase==RunPhase.Encounter){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
                Assert.AreEqual(RunPhase.RewardPending,session.Phase);
            }
            File.WriteAllLines(Path.Combine(_dir,"04-controlled-encounters.txt"),log);
        }
    }
}
