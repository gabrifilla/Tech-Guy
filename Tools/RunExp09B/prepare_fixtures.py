"""Derive bounded09B fixtures from already audited native gameplay contracts09/06.
Run in repository before import; never touches those original test files.
"""
from pathlib import Path
R=Path(__file__).resolve().parents[2];T=R/'Assets/_Project/Scripts/Tests/PlayMode'
s=(T/'RunExp03FirstSectorTests.Spatial09Gameplay.cs').read_text(encoding='utf-8-sig')
header=s[:s.index('        [UnityTest]')]
a=s.index('        private IEnumerator SpatialGameplay09(');b=s.index('        private IEnumerator SpatialLos09(',a)
method=s[a:b].replace('SpatialGameplay09(FirstSectorRoomFamily family,int weapon)','SpatialGameplay09B(FirstSectorRoomFamily family,int turns,int weapon)')
method=method.replace('run-exp-09/gameplay/','run-exp-09b/variants/').replace('+"-"+family+"-"+weapon','+"-"+family+"-"+turns+"-"+weapon')
start=method.index('                ulong seed=0;');end=method.index('                PlayerPrefs.SetInt',start)
method=method[:start]+'                ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family||FirstSectorRoomDistribution.Orientation(seed,1,family,new Vector2(22,18))!=turns||!content.HasCover(seed,definition.Nodes.First(n=>n.Id==1))){seed++;Assert.Less(seed,10000);}\n'+method[end:]
method=method.replace('EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));','yield return SceneManager.LoadSceneAsync("FirstSector");')
method=method.replace('Assert.IsNotNull(layout);Assert.AreEqual(family,layout.Family);','Assert.IsNotNull(layout);Assert.AreEqual(family,layout.Family);Assert.AreEqual(turns,layout.QuarterTurns);Assert.IsTrue(director.ManualEnvironment.SpatialOrientationsEnabled);File.WriteAllText(Path.Combine(_dir,"orientation.txt"),"family="+family+";degrees="+(turns*90)+";seed="+seed);')
method=method.replace('node.Center+Vector3.forward*(weapon==0?.85f:weapon==1?3:1.5f)','node.Center+layout.ToOriented(Vector3.forward*(weapon==0?.85f:weapon==1?3:1.5f))')
method=method.replace('                yield return ObserveNativePoses(director,node);','''                var cover=arena.GetComponentsInChildren<BoxCollider>().Single(c=>c.name=="Cobertura deslocada 0");
                Assert.IsTrue(nav.Warp(new Vector3(cover.bounds.center.x,0,cover.bounds.max.z+1.5f)));yield return new WaitForSeconds(.8f);
                Assert.IsTrue(rig.IsOcclusionFaded(cover),"Oriented cover uses native occlusion");Assert.IsTrue(cover.enabled);Assert.AreEqual(40,camera.fieldOfView);yield return Capture("09B-occlusion-normal-camera");Assert.IsTrue(nav.Warp(node.Center));
                yield return ObserveNativePoses(director,node);''')
# Add a real Bow projectile blocked by the same actual cover, then restore the setup for the native HP-positive check.
method=method.replace('                Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(node.Center+layout.ToOriented', '''                if(weapon==1)
                {
                    var cb=cover.bounds;Assert.IsTrue(nav.Warp(new Vector3(cb.center.x,0,cb.min.z-1.5f)));Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(new Vector3(cb.center.x,0,cb.max.z+1.5f)));Physics.SyncTransforms();
                    var cc=player.GetComponent<CharControlScript>();cc.SendMessage("OnApplicationFocus",true);input.Release();yield return null;yield return null;
                    float ready=Time.realtimeSinceStartup+5;while(!cc.GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,ready);yield return null;}
                    float blockedHp=victim.health;yield return Basic(cc,victim.transform.position);Assert.AreEqual(blockedHp,victim.health,"Native Bow projectile is blocked by oriented solid");yield return Capture("09B-projectile-blocked-by-oriented-cover");Assert.IsTrue(nav.Warp(node.Center));
                }
                Assert.IsTrue(victim.GetComponent<NavMeshAgent>().Warp(node.Center+layout.ToOriented''')
method=method.replace('var floors=layout.Floors;var target=floors.Select(r=>new Vector3(r.center.x,0,r.center.y)).Where(p=>layout.Clear(p,1)).OrderByDescending(p=>p.sqrMagnitude).First();','var destinations=new List<Vector3>();for(float x=layout.MinX+1.5f;x<-layout.MinX-1;x+=1)for(float z=layout.MinZ+1.5f;z<-layout.MinZ-1;z+=1){var point=new Vector3(x,0,z);if(layout.Clear(point,1))destinations.Add(point);}Assert.Greater(destinations.Count,0);var target=destinations.OrderByDescending(p=>p.sqrMagnitude).First();')
wrappers=''
for label,fam in [('L','LChamber'),('Courtyard','AsymmetricCourtyard')]:
 for turns in range(4):
  for weapon,name in enumerate(['Gauntlet','Bow','Spear']):wrappers+=f'        [UnityTest] public IEnumerator Distribution09B{label}{turns*90}{name}()=>SpatialGameplay09B(FirstSectorRoomFamily.{fam},{turns},{weapon});\n'
out=header+wrappers+method+'    }\n}\n'
(R/'Tools/RunExp09B/Gameplay.pending').write_text(out,encoding='utf-8')

s=(T/'RunExp03FirstSectorTests.Production06.cs').read_text(encoding='utf-8-sig');a=s.index('        private IEnumerator PublishedRoute06(');b=s.index('        private sealed class PublishedGameView06',a)
m=s[a:b].replace('PublishedRoute06','PublishedRoute09B').replace('run-exp-06/gameplay/','run-exp-09b/production/').replace('route06.txt','route09b.txt')
m=m.replace('var node=session.Route.Node(nodeId);Assert.IsTrue','var node=session.Route.Node(nodeId);log.Add("geometry node="+nodeId+" family="+(node.SpatialLayout?.Family??FirstSectorRoomFamily.Rectangle)+" degrees="+((node.SpatialLayout?.QuarterTurns??0)*90));Assert.IsTrue')
w=''
for name,weapon,seed in [('Gauntlet',0,'0'),('Bow',1,'2'),('Spear',2,'1'),('Seed5A',0,'5'),('Seed5B',0,'5'),('Seed6',0,'6'),('Random',0,'null')]:w+=f'        [UnityTest] public IEnumerator Distribution09BNexus{name}()=>PublishedRoute09B({weapon},{seed});\n'
head=s[:s.index('        [UnityTest]')]
(R/'Tools/RunExp09B/Production.pending').write_text(head+w+m+'    }\n}\n',encoding='utf-8')

s=(T/'RunExp03FirstSectorTests.Spatial09.cs').read_text(encoding='utf-8-sig').replace('Spatial09FiftyDecksHaveRealConnectedNavMeshAndUsableSockets','Distribution09BFiftyOrientedDecksHaveConnectedNavMeshAndSockets').replace('run-exp-09/structural/','run-exp-09b/structural/')
s=s.replace('EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));','yield return SceneManager.LoadSceneAsync("FirstSector");')
s=s.replace('private const string SpatialReview="Assets/_Project/Scenes/Review/RunExp09SpatialReview.unity";','')
s=s.replace('family,area,floor_rects','family,orientation,area,floor_rects').replace('+layout.Family+","+layout.Area','+layout.Family+","+(layout.QuarterTurns*90)+","+layout.Area')
(R/'Tools/RunExp09B/Nav.pending').write_text(s,encoding='utf-8')
