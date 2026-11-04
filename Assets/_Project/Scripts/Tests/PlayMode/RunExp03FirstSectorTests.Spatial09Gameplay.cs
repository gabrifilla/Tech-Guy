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
        [UnityTest] public IEnumerator Spatial09CourtyardGauntlet()=>SpatialGameplay09(FirstSectorRoomFamily.AsymmetricCourtyard,0);
        [UnityTest] public IEnumerator Spatial09CourtyardBow()=>SpatialGameplay09(FirstSectorRoomFamily.AsymmetricCourtyard,1);
        [UnityTest] public IEnumerator Spatial09CourtyardSpear()=>SpatialGameplay09(FirstSectorRoomFamily.AsymmetricCourtyard,2);
        [UnityTest] public IEnumerator Spatial09LGauntlet()=>SpatialGameplay09(FirstSectorRoomFamily.LChamber,0);
        [UnityTest] public IEnumerator Spatial09LBow()=>SpatialGameplay09(FirstSectorRoomFamily.LChamber,1);
        [UnityTest] public IEnumerator Spatial09LSpear()=>SpatialGameplay09(FirstSectorRoomFamily.LChamber,2);
        [UnityTest] public IEnumerator Spatial09GalleryGauntlet()=>SpatialGameplay09(FirstSectorRoomFamily.ElongatedGallery,0);
        [UnityTest] public IEnumerator Spatial09GalleryBow()=>SpatialGameplay09(FirstSectorRoomFamily.ElongatedGallery,1);
        [UnityTest] public IEnumerator Spatial09GallerySpear()=>SpatialGameplay09(FirstSectorRoomFamily.ElongatedGallery,2);
        [UnityTest] public IEnumerator Spatial09RingGauntlet()=>SpatialGameplay09(FirstSectorRoomFamily.FragmentedRing,0);
        [UnityTest] public IEnumerator Spatial09RingBow()=>SpatialGameplay09(FirstSectorRoomFamily.FragmentedRing,1);
        [UnityTest] public IEnumerator Spatial09RingSpear()=>SpatialGameplay09(FirstSectorRoomFamily.FragmentedRing,2);

        private IEnumerator SpatialGameplay09(FirstSectorRoomFamily family,int weapon)
        {
            using(var input=new PublishedInput06())
            {
                _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/gameplay/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff")+"-"+family+"-"+weapon);Directory.CreateDirectory(_dir);
                var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
                var definition=AssetDatabase.LoadAssetAtPath<ManualRunDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorManual.asset");
                ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family||!content.HasCover(seed,definition.Nodes.First(n=>n.Id==1)))seed++;
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());
                EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;
                var director=Object.FindAnyObjectByType<ProgressionDirector>();var player=Object.FindAnyObjectByType<PlayerActor>();var session=director.ManualSession;var node=session.Route.Node(1);var layout=node.SpatialLayout;
                Assert.IsNotNull(layout);Assert.AreEqual(family,layout.Family);
                // Controlled six-role encounter to compare the same real executors across footprints; no pool/weight changes.
                var names=new[]{"Fragmento","Emissor","Grunt","Heavy","Hooker","Healer"};
                var spawns=names.Select(name=>new ManualEnemySpawn(name=="Fragmento"||name=="Emissor"?content.AdditionalEnemies.Single(p=>p.name==name):AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/"+name+".prefab"),Vector3.zero)).ToList();
                typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(node,layout.Arrange(spawns));
                player.AddDamageImmunity(director);var nav=player.GetComponent<NavMeshAgent>();
                Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(nav.Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
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
                yield return ObserveNativePoses(director,node);
                yield return SpatialLos09(director,node,player);
                var floors=layout.Floors;var target=floors.Select(r=>new Vector3(r.center.x,0,r.center.y)).Where(p=>layout.Clear(p,1)).OrderByDescending(p=>p.sqrMagnitude).First();
                Assert.IsTrue(nav.SetDestination(node.Center+target));var start=player.transform.position;yield return new WaitForSeconds(1);Assert.Greater((player.transform.position-start).sqrMagnitude,.5f);
                yield return Capture("09-actual-player-circulation");nav.ResetPath();Assert.IsTrue(nav.Warp(node.Center));
                // Basic HP impact in the quiet center. Temporary actor HP/position/AI fixture, restored before skills.
                var victim=director.GetComponentsInChildren<Actor>().First(a=>a.name.StartsWith("Fragmento"));var ai=victim.GetComponent<EnemyAI>();bool enabled=ai.enabled;var old=victim.transform.position;
                ai.InterruptAttack();ai.enabled=false;victim.SetMaxHealth(1000);victim.RestoreHealthToMax();
                Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(node.Center+Vector3.forward*(weapon==0?.85f:weapon==1?3:1.5f)));Physics.SyncTransforms();
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

        private IEnumerator SpatialLos09(ProgressionDirector director,ManualNodeDefinition node,PlayerActor player)
        {
            var emitter=director.GetComponentsInChildren<FirstSectorEnemyPose>().First(p=>p.Ranged);var actions=emitter.GetComponent<EnemyCombatActions>();var ai=emitter.GetComponent<EnemyAI>();var agent=emitter.GetComponent<NavMeshAgent>();
            var cover=director.ManualEnvironment.Node(node.Id).Arena.GetComponentsInChildren<BoxCollider>().First(c=>c.name=="Cobertura deslocada 0").bounds;
            Vector3 oldPlayer=player.transform.position,oldEnemy=emitter.transform.position;
            ai.InterruptAttack();Assert.IsTrue(agent.Warp(new Vector3(cover.center.x,0,cover.min.z-1.3f)));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.center.x,0,cover.max.z+1.4f)));Physics.SyncTransforms();
            Assert.IsTrue(Physics.Linecast(emitter.transform.position+Vector3.up*.8f,player.transform.position+Vector3.up*.8f,out var hit,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore));
            Assert.AreEqual("Cobertura deslocada 0",hit.collider.name,"The actual cover collider is the LOS obstruction");
            int impacts=actions.ResolvedImpacts;yield return new WaitForSeconds(.7f);Assert.AreEqual(impacts,actions.ResolvedImpacts,"Cover blocks native Emissor impact");yield return Capture("09-emissor-cover-blocked");
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(new Vector3(cover.min.x-1.4f,0,cover.min.z-1.3f)));
            float until=Time.realtimeSinceStartup+12;while(actions.ResolvedImpacts==impacts){Assert.Less(Time.realtimeSinceStartup,until,"Ranged reacquisition");yield return null;}
            yield return Capture("09-emissor-LOS-reacquired");Assert.IsTrue(agent.Warp(oldEnemy));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(oldPlayer));
        }

        [Serializable] private sealed class Box09{public string name;public Vector3 center,size;}
        [Serializable] private sealed class Footprint09
        {
            public string family;public int weapon;public string seed;public Vector3 center;public Vector2 size;
            public Box09[] floors,walls,covers;public Vector3[] sockets,spawns,navVertices;public int[] navIndices;public string[] roster;
        }
        private void ExportSpatial09(ProgressionDirector director,ManualNodeDefinition node,int weapon,ulong seed)
        {
            var arena=director.ManualEnvironment.Node(node.Id).Arena;var colliders=arena.GetComponentsInChildren<BoxCollider>();
            Func<BoxCollider,Box09> box=c=>new Box09{name=c.name,center=c.bounds.center-node.Center,size=c.bounds.size};
            var triangles=NavMesh.CalculateTriangulation();var verts=new List<Vector3>();var indices=new List<int>();
            for(int i=0;i<triangles.indices.Length;i+=3)
            {
                var a=triangles.vertices[triangles.indices[i]]-node.Center;var b=triangles.vertices[triangles.indices[i+1]]-node.Center;var c=triangles.vertices[triangles.indices[i+2]]-node.Center;
                if(new[]{a,b,c}.Any(p=>Mathf.Abs(p.x)>node.ArenaSize.x*.5f||Mathf.Abs(p.z)>node.ArenaSize.y*.5f))continue;
                int index=verts.Count;verts.AddRange(new[]{a,b,c});indices.AddRange(new[]{index,index+1,index+2});
            }
            var sockets=new List<Vector3>();foreach(var edge in director.ManualSession.Route.Connections)
            {
                var binding=director.ManualEnvironment.Connections.First(b=>b.ConnectionId==edge.Id);
                if(edge.From==node.Id)sockets.Add(binding.FromAnchor.position-node.Center);if(edge.To==node.Id)sockets.Add(binding.ToAnchor.position-node.Center);
            }
            var data=new Footprint09{family=node.SpatialLayout.Family.ToString(),weapon=weapon,seed=seed.ToString(),center=node.Center,size=node.ArenaSize,
                floors=colliders.Where(c=>c.name=="Walkable arena").Select(box).ToArray(),walls=colliders.Where(c=>c.name=="Arena wall"||c.name.StartsWith("Central archive")).Select(box).ToArray(),covers=colliders.Where(c=>c.name.StartsWith("Cobertura")).Select(box).ToArray(),
                sockets=sockets.ToArray(),spawns=node.Spawns.Select(s=>s.Offset).ToArray(),roster=node.Spawns.Select(s=>s.Prefab.name).ToArray(),navVertices=verts.ToArray(),navIndices=indices.ToArray()};
            File.WriteAllText(Path.Combine(_dir,"actual-geometry-navmesh.json"),JsonUtility.ToJson(data,true));
        }
    }
}
