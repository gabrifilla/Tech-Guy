using System.Collections;
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
        [UnityTest] public IEnumerator NativeThreeWeaponBasicsHitBothCandidatesAndRespectCover()
        {
            for(int weapon=0;weapon<3;weapon++)
            {
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);SessionState.SetString(RunSeedSource.NextEditorSeedKey,"63");
                yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;
                Assert.AreEqual(0,player.GetComponent<RunBoons>().Acquired.Count);
                var node=session.Route.Node(1);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                var actors=director.GetComponentsInChildren<Actor>();var candidates=actors.Where(a=>a.GetComponent<FirstSectorEnemyPose>()).ToArray();Assert.AreEqual(2,candidates.Length,"Seed63 first roster must contain the two real candidates.");
                player.AddDamageImmunity(director);
                // Explicit collision fixture: freeze enemies and isolate their colliders. Natural-AI runs and performance tests are separate.
                foreach(var actor in actors)
                {
                    foreach(var behaviour in actor.GetComponents<MonoBehaviour>())if(!(behaviour is Actor)&&!(behaviour is CombatReactionController))behaviour.enabled=false;
                    var agent=actor.GetComponent<NavMeshAgent>();if(agent&&agent.isOnNavMesh)agent.isStopped=true;
                    foreach(var collider in actor.GetComponentsInChildren<Collider>())collider.enabled=false;
                    foreach(var coin in actor.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                }
                var cover=director.ManualEnvironment.Node(1).Arena.GetComponentsInChildren<BoxCollider>().First(c=>c.name=="Cobertura deslocada 0").bounds;
                var controls=player.GetComponent<CharControlScript>();
                foreach(var target in candidates)
                {
                    target.GetComponent<CapsuleCollider>().enabled=true;target.SetMaxHealth(1000);target.RestoreHealthToMax();
                    Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.center.x,0,cover.min.z-.8f)));
                    Assert.IsTrue(target.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.center.x,0,cover.max.z+.8f)));Physics.SyncTransforms();float hp=target.health;
                    yield return Basic(controls,target.transform.position);Assert.AreEqual(hp,target.health,"Solid cover: "+target.name+" weapon="+weapon);
                    Assert.IsTrue(target.GetComponent<NavMeshAgent>().Warp(player.transform.position+Vector3.right*(weapon==0?.85f:weapon==1?3:1.5f)));Physics.SyncTransforms();
                    yield return Basic(controls,target.transform.position);Assert.Less(target.health,hp,"Open-space native basic must hit "+target.name+" weapon="+weapon);
                    yield return Capture("05-native-basic-weapon-"+weapon+"-"+target.name);
                    target.GetComponent<CapsuleCollider>().enabled=false;
                }
            }
        }
        private static IEnumerator Basic(CharControlScript controls,Vector3 target)
        {
            controls.CancelCombo();float end=Time.time+4;
            while(!controls.TryDirectionalBasicAttack(target)){Assert.Less(Time.time,end);yield return null;}
            yield return new WaitForSeconds(1.1f);Assert.IsFalse(controls.HasScheduledBasic);
        }
    }
}
