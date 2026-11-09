using System.Collections;
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
        [UnityTest] public IEnumerator Archive05ThreeWeaponNativeSkillsSpecialAndDashRemainPlayable()
        {
            for(int weapon=0;weapon<3;weapon++)
            {
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);SessionState.SetString(RunSeedSource.NextEditorSeedKey,"63");
                yield return SceneManager.LoadSceneAsync("FirstSector");for(int frame=0;frame<4;frame++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
                player.AddDamageImmunity(director);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(-7,0,-5)));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                Assert.IsTrue(director.ManualEnvironment.Node(1).Arena.GetComponent<FirstSectorEnvironmentArt>().Visible);
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                typeof(PlayerActor).GetField("baseMaxMana",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,1500f);player.RefreshResourceStats();
                var holder=player.GetComponent<AbilityHolder>();int uses=0;holder.AbilityUsed+=slot=>uses++;
                // Native encounter input opens only after observing a released frame.
                var controls=player.GetComponent<CharControlScript>();float inputEnd=Time.realtimeSinceStartup+5;
                while(!controls.GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,inputEnd,"Wait for native release gate");yield return null;}
                for(int slot=0;slot<4;slot++)
                {
                    player.mana=1500;if(weapon==0&&slot==3)player.GetComponent<BreakerGauntletCombat>().AddAsuraEnergy(100);
                    float mana=player.mana;Assert.IsTrue(holder.TryUseAbility(slot),"weapon="+weapon+" native slot="+slot);Assert.AreEqual(slot+1,uses);
                    Assert.That(player.mana,Is.EqualTo(mana-holder.ActiveAbilities[slot].ManaCost).Within(.001));
                    yield return new WaitForSeconds(.12f);yield return Capture("05-room-weapon-"+weapon+"-native-slot-"+slot);
                    float end=Time.realtimeSinceStartup+12;
                    while(holder.IsCasting){Assert.Less(Time.realtimeSinceStartup,end);if(holder.CanFinishSequence)holder.TryFinishSequence();yield return null;}
                    yield return new WaitForSeconds(.15f);
                }
                var dash=AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");player.mana=1500;
                Assert.IsTrue(holder.TryUseDash(dash));yield return new WaitForSeconds(.4f);Assert.IsFalse(dash.IsDashing(player.gameObject));Assert.AreEqual(5,uses);
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().isOnNavMesh);yield return Capture("05-room-weapon-"+weapon+"-dash-recovered");
                File.WriteAllText(Path.Combine(_dir,"05-room-weapon-"+weapon+".txt"),"Real FirstSector seed63, native current Q/E/R slots0/1/2 and special3 each accepted once with actual mana cost, followed by dash and recovery. Native AI/damage/nav stay active; player immune with fixture mana/Asura resource. Does not assert every skill damages each enemy; separate HP attribution and wall-contact selections cover those contracts.");
            }
        }
        [Explicit("QUARENTENA-F0: rota/salas publicadas 09/09B; causa nao determinada")]
        [UnityTest] public IEnumerator Archive05ComparisonPreservesCoverRoutesLosMovementAndAbilities()
        {
            foreach(ulong seed in new ulong[]{0,63})
            {
                SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());yield return SceneManager.LoadSceneAsync("FirstSector");for(int frame=0;frame<4;frame++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);
                player.AddDamageImmunity(director);Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                var arena=director.ManualEnvironment.Node(1).Arena;var presentation=arena.GetComponent<FirstSectorEnvironmentArt>();Assert.IsNotNull(presentation);
                var colliders=arena.GetComponentsInChildren<Collider>();var bounds=colliders.Select(c=>c.bounds).ToArray();int count=colliders.Length;
                foreach(var material in arena.GetComponentsInChildren<MeshRenderer>().SelectMany(r=>r.sharedMaterials).Distinct())if(material)for(int pass=0;pass<material.passCount;pass++)ShaderUtil.CompilePass(material,pass,false);
                float deadline=Time.realtimeSinceStartup+30;while(ShaderUtil.anythingCompiling){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                yield return new WaitForSeconds(1);Time.timeScale=0;
                foreach(bool revised in new[]{false,true})
                {
                    presentation.SetVisible(revised);yield return Capture("05-room-"+(seed==0?"open":"cover")+"-"+(revised?"after":"before")+"-normal-camera");
                    Assert.AreEqual(count,arena.GetComponentsInChildren<Collider>().Length);for(int i=0;i<count;i++)Assert.AreEqual(bounds[i],colliders[i].bounds);
                    var ray=new Ray(node.Center+new Vector3(-3,.8f,-5),Vector3.forward);
                    Assert.AreEqual(seed==63,colliders.Where(c=>!c.isTrigger).Any(c=>{RaycastHit hit;return c.Raycast(ray,out hit,7);}));
                    foreach(float x in new[]{-node.ArenaSize.x*.5f+2,node.ArenaSize.x*.5f-2})
                    {var path=new NavMeshPath();Assert.IsTrue(NavMesh.CalculatePath(node.Center+new Vector3(x,0,-6),node.Center+new Vector3(x,0,6),NavMesh.AllAreas,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                }
                Time.timeScale=1;
                if(seed==63)
                {
                    yield return ObserveNativePoses(director,node);
                    yield return VerifyEmitterCover(director,node,player);
                    var nav=player.GetComponent<NavMeshAgent>();var path=new NavMeshPath();var start=node.Center+new Vector3(-7,0,-5);var target=node.Center+new Vector3(-7,0,5);
                    Assert.IsTrue(nav.Warp(start));Assert.IsTrue(nav.CalculatePath(target,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);Assert.IsTrue(nav.SetDestination(target));
                    float until=Time.time+1;while(Time.time<until)yield return null;Assert.Greater((player.transform.position-start).sqrMagnitude,.5f);yield return Capture("05-room-player-circulating");
                }
                File.WriteAllText(Path.Combine(_dir,"05-room-contract-"+seed+".txt"),"Native collider count/bounds identical in04/05 comparison; two complete circulation paths; cover ray; actual nav movement and native emitter LOS/reacquisition for63. Art is optional local render layer, no graph/spawn/cover chance changes.");
            }
        }
    }
}
