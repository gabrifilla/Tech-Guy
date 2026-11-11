using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator Distribution09BL0Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,0,0);
        [UnityTest] public IEnumerator Distribution09BL0Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,0,1);
        [UnityTest] public IEnumerator Distribution09BL0Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,0,2);
        [UnityTest] public IEnumerator Distribution09BL90Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,1,0);
        [UnityTest] public IEnumerator Distribution09BL90Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,1,1);
        [UnityTest] public IEnumerator Distribution09BL90Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,1,2);
        [UnityTest] public IEnumerator Distribution09BL180Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,2,0);
        [UnityTest] public IEnumerator Distribution09BL180Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,2,1);
        [UnityTest] public IEnumerator Distribution09BL180Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,2,2);
        [UnityTest] public IEnumerator Distribution09BL270Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,3,0);
        [UnityTest] public IEnumerator Distribution09BL270Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,3,1);
        [UnityTest] public IEnumerator Distribution09BL270Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.LChamber,3,2);
        [UnityTest] public IEnumerator Distribution09BCourtyard0Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,0,0);
        [UnityTest] public IEnumerator Distribution09BCourtyard0Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,0,1);
        [UnityTest] public IEnumerator Distribution09BCourtyard0Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,0,2);
        [UnityTest] public IEnumerator Distribution09BCourtyard90Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,1,0);
        [UnityTest] public IEnumerator Distribution09BCourtyard90Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,1,1);
        [UnityTest] public IEnumerator Distribution09BCourtyard90Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,1,2);
        [UnityTest] public IEnumerator Distribution09BCourtyard180Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,2,0);
        [UnityTest] public IEnumerator Distribution09BCourtyard180Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,2,1);
        [UnityTest] public IEnumerator Distribution09BCourtyard180Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,2,2);
        [UnityTest] public IEnumerator Distribution09BCourtyard270Gauntlet()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,3,0);
        [UnityTest] public IEnumerator Distribution09BCourtyard270Bow()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,3,1);
        [UnityTest] public IEnumerator Distribution09BCourtyard270Spear()=>SpatialGameplay09B(FirstSectorRoomFamily.AsymmetricCourtyard,3,2);
        private IEnumerator SpatialGameplay09B(FirstSectorRoomFamily family,int turns,int weapon)
        {
            using(var input=new PublishedInput06())
            {
                _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09b/variants/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+family+"-"+turns+"-"+weapon);Directory.CreateDirectory(_dir);
                var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
                var definition=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
                ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family||FirstSectorRoomDistribution.Orientation(seed,1,family,new Vector2(22,18))!=turns||!content.HasCover(seed,definition.Nodes.First(n=>n.Id==1))){seed++;Assert.Less(seed,10000);}
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
                yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
                var director=Object.FindAnyObjectByType<ProgressionDirector>();var player=Object.FindAnyObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);var layout=node.SpatialLayout;
                Assert.IsNotNull(layout);Assert.AreEqual(family,layout.Family);Assert.AreEqual(turns,layout.QuarterTurns);Assert.IsTrue(director.ManualEnvironment.SpatialOrientationsEnabled);File.WriteAllText(Path.Combine(_dir,"orientation.txt"),"family="+family+";degrees="+(turns*90)+";seed="+seed);
                // Controlled six-role encounter to compare the same real executors across footprints; no pool/weight changes.
                var names=new[]{"Fragmento","Emissor","Grunt","Heavy","Hooker","Healer"};
                var spawns=names.Select(name=>new ManualEnemySpawn(name=="Fragmento"||name=="Emissor"?content.AdditionalEnemies.Single(p=>p.name==name):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+name+".prefab"),Vector3.zero)).ToList();
                typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,layout.Arrange(spawns));
                player.AddDamageImmunity(director);var nav=player.GetComponent<NavMeshAgent>();
                Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(nav.Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                // Spawn positions, for the quiet-centre fixture below (encounter AI is room-wide aggro since 10/11/2026).
                var spawnPositions=director.GetComponentsInChildren<EnemyAI>().ToDictionary(e=>e,e=>e.transform.position);
                var arena=director.ManualEnvironment.Node(1).Arena;
                Assert.IsTrue(director.ManualEnvironment.Connections.Where(b=>session.Route.Connections.Any(e=>e.Id==b.ConnectionId&&e.From==1)).All(b=>b.Barrier.activeSelf));
                yield return new WaitForSeconds(.3f);yield return Capture("09-gameplay-overview");
                ExportSpatial09(director,node,weapon,seed);
                var camera=Camera.main;var rig=camera.GetComponent<TechGuy.Cameras.TG_TopDown_Camera>();
                var position=camera.transform.position;var rotation=camera.transform.rotation;bool ortho=camera.orthographic;float size=camera.orthographicSize;
                try
                {
                    rig.enabled=false;camera.orthographic=true;camera.orthographicSize=Mathf.Max(node.ArenaSize.x,node.ArenaSize.y)*.63f;
                    camera.transform.SetPositionAndRotation(node.Center+Vector3.up*45,Quaternion.Euler(90,0,0));
                    yield return Capture("09-technical-top-down");
                }
                finally{camera.orthographic=ortho;camera.orthographicSize=size;camera.transform.SetPositionAndRotation(position,rotation);rig.enabled=true;}
                foreach(var actor in director.GetComponentsInChildren<Actor>())
                {var path=new NavMeshPath();Assert.IsTrue(actor.GetComponent<NavMeshAgent>().CalculatePath(player.transform.position,path));Assert.AreEqual(NavMeshPathStatus.PathComplete,path.status);}
                var cover=arena.GetComponentsInChildren<BoxCollider>().Single(c=>c.name=="Cobertura deslocada 0");
                Assert.IsTrue(nav.Warp(new Vector3(cover.bounds.center.x,0,cover.bounds.max.z+1.5f)));yield return new WaitForSeconds(.8f);
                Assert.IsTrue(rig.IsOcclusionFaded(cover),"Oriented cover uses native occlusion");Assert.IsTrue(cover.enabled);Assert.AreEqual(40,camera.fieldOfView);yield return Capture("09B-occlusion-normal-camera");Assert.IsTrue(nav.Warp(node.Center));
                yield return ObserveNativePoses(director,node);
                yield return SpatialLos09(director,node,player);
                var destinations=new List<Vector3>();for(float x=layout.MinX+1.5f;x<-layout.MinX-1;x+=1)for(float z=layout.MinZ+1.5f;z<-layout.MinZ-1;z+=1){var point=new Vector3(x,0,z);if(layout.Clear(point,1))destinations.Add(point);}
                Assert.Greater(destinations.Count,0,"A real clear floor destination is required");var target=destinations.OrderByDescending(p=>p.sqrMagnitude).First();
                Assert.IsTrue(nav.SetDestination(node.Center+target));var start=player.transform.position;yield return new WaitForSeconds(1);Assert.Greater((player.transform.position-start).sqrMagnitude,.5f);
                yield return Capture("09-actual-player-circulation");nav.ResetPath();Assert.IsTrue(nav.Warp(node.Center));
                // Basic HP impact in the quiet center. Temporary actor HP/position/AI fixture, restored before skills.
                var victim=director.GetComponentsInChildren<Actor>().First(a=>a.name.StartsWith("Fragmento"));var ai=victim.GetComponent<EnemyAI>();bool enabled=ai.enabled;var old=victim.transform.position;
                ai.InterruptAttack();ai.enabled=false;victim.SetMaxHealth(1000);victim.RestoreHealthToMax();
                // Quiet centre: the other roles go back to their spawns with AI paused for the isolated basic/dash checks
                // (room-wide aggro would otherwise crowd the immune player). Restored before the forced kills.
                var paused=new List<EnemyAI>();
                foreach(var pair in spawnPositions){var other=pair.Key;if(!other||other==ai||!other.enabled)continue;other.InterruptAttack();other.enabled=false;other.agent.Warp(pair.Value);paused.Add(other);}
                foreach(var shot in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))Object.Destroy(shot.gameObject); // shots already in flight
                yield return null;Physics.SyncTransforms();
                if(weapon==1)
                {
                    var cb=cover.bounds;Assert.IsTrue(nav.Warp(new Vector3(cb.center.x,0,cb.min.z-1.5f)));Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(new Vector3(cb.center.x,0,cb.max.z+1.5f)));Physics.SyncTransforms();
                    var cc=player.GetComponent<CharControlScript>();cc.SendMessage("OnApplicationFocus",true);input.Release();yield return null;yield return null;
                    float ready=Time.realtimeSinceStartup+5;while(!cc.GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,ready);yield return null;}
                    float blockedHp=victim.health;yield return Basic(cc,victim.transform.position);Assert.AreEqual(blockedHp,victim.health,"Native Bow projectile is blocked by oriented solid");yield return Capture("09B-projectile-blocked-by-oriented-cover");Assert.IsTrue(nav.Warp(node.Center));
                }
                Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(node.Center+layout.ToOriented(Vector3.forward*(weapon==0?.85f:weapon==1?3:1.5f))));Physics.SyncTransforms();
                var controls=player.GetComponent<CharControlScript>();controls.SendMessage("OnApplicationFocus",true);input.Release();yield return null;yield return null;
                float until=Time.realtimeSinceStartup+5;while(!controls.GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,until,"Input fixture focused/released");yield return null;}
                float hp=victim.health;yield return Basic(controls,victim.transform.position);Assert.Less(victim.health,hp,"Native basic damages target in "+family+" weapon="+weapon);
                Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(old));ai.enabled=enabled;
                typeof(PlayerActor).GetField("baseMaxMana",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(player,1500f);player.RefreshResourceStats();var holder=player.GetComponent<AbilityHolder>();
                for(int slot=0;slot<4;slot++)
                {
                    player.mana=1500;if(weapon==0&&slot==3)player.GetComponent<BreakerGauntletCombat>().AddAsuraEnergy(100);
                    Assert.IsTrue(holder.TryUseAbility(slot));yield return new WaitForSeconds(.12f);yield return Capture("09-native-skill-"+slot);
                    until=Time.realtimeSinceStartup+15;while(holder.IsCasting){Assert.Less(Time.realtimeSinceStartup,until);if(holder.CanFinishSequence)holder.TryFinishSequence();yield return null;}
                    yield return new WaitForSeconds(.15f);
                }
                var dash=AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");player.mana=1500;Assert.IsTrue(holder.TryUseDash(dash));yield return new WaitForSeconds(.4f);
                Assert.IsTrue(layout.Contains(player.transform.position-node.Center,.3f));Assert.IsTrue(nav.isOnNavMesh);yield return Capture("09-dash-recovered");
                foreach(var other in paused)if(other)other.enabled=true;
                foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
                until=Time.realtimeSinceStartup+12;while(session.Phase==RunPhase.Encounter){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                var run=player.GetComponent<RunBoons>();while(session.Phase==RunPhase.RewardPending)
                {if(!run.IsChoosing)Assert.IsTrue(director.TryOpenManualReward(session.Id,1,session.RewardOrdinal));yield return null;yield return Capture("09-C05-ticket-"+session.RewardOrdinal);Assert.IsTrue(run.Choose(0));}
                int next=session.Route.Paths.First().SkipWhile(n=>n!=1).Skip(1).First();Assert.IsTrue(director.TryCommitDestination(session.Id,next));
                var edge=session.Route.Connections.First(e=>e.From==1&&e.To==next);var points=ProceduralRunGeometryPlan.Create(session.Route)[edge.Id];
                var gate=director.ManualEnvironment.Connections.First(b=>b.ConnectionId==edge.Id);Assert.IsFalse(gate.Barrier.activeSelf);
                var outward=(points[1]-points[0]).normalized;Assert.IsTrue(nav.Warp(points[0]-outward));Assert.IsTrue(nav.SetDestination(points[0]+outward*3));
                until=Time.realtimeSinceStartup+12;while(Vector3.Distance(player.transform.position,points[0]+outward*3)>1){Assert.Less(Time.realtimeSinceStartup,until,"Actual connector crossing");yield return null;}
                yield return Capture("09-door-open-physical-traversal");
                Assert.IsTrue(nav.Warp(session.Route.Node(next).Center));Assert.IsTrue(director.TryEnterNode(session.Id,next));yield return null;yield return Capture("09-next-node");
                File.WriteAllText(Path.Combine(_dir,"passed.txt"),"Controlled six-role roster; native AI/executors and geometry, player immunity; basic HP check with temporary target setup, mana/Asura fixture; forced kills for completion, real tickets and physical connector segment then warp to next center. Not human playthrough. seed="+seed);
            }
        }

    }
}
