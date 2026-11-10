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
        [Explicit("QUARENTENA-F0: comparacao historica RUN-EXP-04 exige FirstSectorRoomPresentation de sala retangular; ausente nos layouts 09B (triagem 1a)")]
        [UnityTest] public IEnumerator RefinedRoomComparisonsPreserveCollidersCoverAndBothCirculationRoutes()
        {
            foreach(ulong seed in new ulong[]{0,63})
            {
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());yield return SceneManager.LoadSceneAsync("FirstSector");for(int frame=0;frame<4;frame++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);player.AddDamageImmunity(director);
                Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                var arena=director.ManualEnvironment.Node(1).Arena;var presentation=arena.GetComponent<FirstSectorRoomPresentation>();Assert.IsNotNull(presentation);
                var colliders=arena.GetComponentsInChildren<Collider>();var bounds=colliders.Select(c=>c.bounds).ToArray();int count=colliders.Length;
                yield return new WaitForSeconds(2);Time.timeScale=0;
                foreach(bool revised in new[]{false,true})
                {
                    presentation.SetVisible(revised);yield return Capture("04-room-"+(seed==0?"open":"cover")+"-"+(revised?"after":"before")+"-normal-camera");
                    Assert.AreEqual(count,arena.GetComponentsInChildren<Collider>().Length);for(int i=0;i<count;i++)Assert.AreEqual(bounds[i],colliders[i].bounds);
                    var ray=new Ray(node.Center+new Vector3(-3,.8f,-5),Vector3.forward);
                    bool blocked=colliders.Where(c=>!c.isTrigger).Any(c=>{RaycastHit hit;return c.Raycast(ray,out hit,7);});Assert.AreEqual(seed==63,blocked);
                    foreach(float x in new[]{-node.ArenaSize.x*.5f+2,node.ArenaSize.x*.5f-2})
                    {var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(node.Center+new Vector3(x,0,-6),node.Center+new Vector3(x,0,6),NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                }
                Time.timeScale=1;
                if(seed==63)yield return VerifyEmitterCover(director,node,player);
            }
        }
    }
}
