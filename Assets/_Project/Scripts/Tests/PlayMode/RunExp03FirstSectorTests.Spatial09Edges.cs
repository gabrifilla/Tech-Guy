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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator Spatial09ReinforcementsConcaveEdgesAndHookRecovery()
        {
            using(var input=new PublishedInput06())
            {
                _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09/edges/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
                var rows=new List<string>{"family,produced,validSamples,hookRecovery"};
                foreach(var family in new[]{FirstSectorRoomFamily.AsymmetricCourtyard,FirstSectorRoomFamily.LChamber,FirstSectorRoomFamily.ElongatedGallery,FirstSectorRoomFamily.FragmentedRing})
                {
                    ulong seed=0;while(FirstSectorRoomLayout.SelectFamily(seed,1)!=family)seed++;
                    SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.ToString());EditorSceneManager.LoadSceneInPlayMode(SpatialReview,new LoadSceneParameters(LoadSceneMode.Single));for(int i=0;i<4;i++)yield return null;
                    var d=Object.FindAnyObjectByType<ProgressionDirector>();var p=Object.FindAnyObjectByType<PlayerActor>();var s=d.ManualSession;var n=s.Route.Node(1);var l=n.SpatialLayout;
                    var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Spawner.prefab");
                    var hook=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/EnemyArchetypes/Hooker.prefab");
                    typeof(ManualNodeDefinition).GetField("_spawns",BindingFlags.Instance|BindingFlags.NonPublic).SetValue(n,l.Arrange(new[]{new ManualEnemySpawn(prefab,Vector3.zero),new ManualEnemySpawn(hook,Vector3.zero)}));
                    p.SetMaxHealth(10000);p.RestoreHealthToMax();Assert.IsTrue(d.TryCommitDestination(s.Id,1));Assert.IsTrue(p.GetComponent<NavMeshAgent>().Warp(n.Center));Assert.IsTrue(d.TryEnterNode(s.Id,1));yield return null;
                    foreach(var coin in d.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                    var spawner=d.GetComponentInChildren<SpawnerBehavior>();var produced=new List<Actor>();spawner.Produced+=a=>produced.Add(a);
                    // Stress the native sampler from clear points beside every concave/outer contour; no new spawn algorithm.
                    var sample=typeof(SpawnerBehavior).GetMethod("TrySampleSpawnPosition",BindingFlags.Instance|BindingFlags.NonPublic);int valid=0;
                    foreach(var edge in l.Boundaries)
                    {
                        var middle=(edge.A+edge.B)*.5f-edge.Outward*1.5f;var local=new Vector3(middle.x,0,middle.y);if(!l.Clear(local,.7f))continue;
                        Assert.IsTrue(spawner.GetComponent<NavMeshAgent>().Warp(n.Center+local));
                        for(int k=0;k<16;k++)
                        {
                            object[] args={Vector3.zero};if(!(bool)sample.Invoke(spawner,args))continue;
                            var point=(Vector3)args[0];Assert.IsTrue(l.Contains(point-n.Center,.1f),family+" sampled outside real footprint: "+(point-n.Center));valid++;
                        }
                    }
                    Assert.Greater(valid,20);Assert.IsTrue(spawner.GetComponent<NavMeshAgent>().Warp(n.Center+Vector3.back*8));
                    float until=Time.realtimeSinceStartup+14;while(produced.Count==0){Assert.Less(Time.realtimeSinceStartup,until,"Native timed reinforcement");yield return null;}
                    foreach(var a in produced){Assert.IsTrue(l.Contains(a.transform.position-n.Center,.1f));Assert.IsTrue(a.GetComponent<NavMeshAgent>().isOnNavMesh);}
                    Vector3 near=family==FirstSectorRoomFamily.AsymmetricCourtyard?new Vector3(l.MinX+5.3f,0,l.Depth*.5f-2):family==FirstSectorRoomFamily.LChamber?new Vector3(-1.3f,0,6):family==FirstSectorRoomFamily.ElongatedGallery?new Vector3(4.7f,0,-11):new Vector3(.7f,0,0);
                    Vector3 direction=family==FirstSectorRoomFamily.AsymmetricCourtyard?Vector3.left:Vector3.right;
                    var nav=p.GetComponent<NavMeshAgent>();Assert.IsTrue(nav.Warp(n.Center+near));p.transform.rotation=Quaternion.LookRotation(direction);
                    UnityEngine.InputSystem.InputSystem.QueueStateEvent(UnityEngine.InputSystem.Mouse.current,new UnityEngine.InputSystem.LowLevel.MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(p.transform.position+direction*10)});yield return null;yield return null;
                    var dash=AssetDatabase.LoadAssetAtPath<DashScript>("Assets/_Project/ScriptableObjects/Abilities/Dash/Dash.asset");
                    // Dash cooldown 1.2 s since the threat pass (11/11/2026): the previous family's hook-recovery dash may still be cooling down.
                    float cooled=Time.realtimeSinceStartup+5;while(dash.GetRemainingCooldown(p.gameObject)>0){Assert.Less(Time.realtimeSinceStartup,cooled);yield return null;}
                    p.GetComponent<CharControlScript>().SendMessage("OnApplicationFocus",true); // explicit virtual focus in a background Editor, as in the hook fixture
                    float focused=Time.realtimeSinceStartup+4;while(!p.GetComponent<CharControlScript>().GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,focused,"Dash fixture focus/release ready");yield return null;}
                    Assert.IsTrue(p.GetComponent<AbilityHolder>().TryUseDash(dash),"Concave dash accepted; cooldown="+dash.GetRemainingCooldown(p.gameObject));yield return new WaitForSeconds(.4f);
                    Assert.IsTrue(l.Contains(p.transform.position-n.Center,.1f),"Native dash stays within real concave footprint");Assert.IsTrue(nav.isOnNavMesh);yield return Capture("09-concave-dash-"+family);
                    float ready=Time.realtimeSinceStartup+15;while(dash.GetRemainingCooldown(p.gameObject)>0){Assert.Less(Time.realtimeSinceStartup,ready);yield return null;}
                    float lane=family==FirstSectorRoomFamily.AsymmetricCourtyard?7:-11;
                    Assert.IsTrue(nav.Warp(n.Center+new Vector3(lane,0,-6)));yield return VerifySpatialHook09(d,n,p,1);
                    yield return Capture("09-reinforcements-"+family);rows.Add(family+","+produced.Count+","+valid+",passed");
                    var sv=SceneView.lastActiveSceneView;if(sv!=null)
                    {
                        sv.LookAtDirect(n.Center,Quaternion.Euler(65,0,0),Mathf.Max(n.ArenaSize.x,n.ArenaSize.y)*.75f);SceneView.RepaintAll();yield return null;
                        var camera=sv.camera;var old=camera.targetTexture;var active=RenderTexture.active;var rt=new RenderTexture(1280,800,24);var tex=new Texture2D(1280,800,TextureFormat.RGB24,false);
                        try{camera.targetTexture=rt;camera.Render();RenderTexture.active=rt;tex.ReadPixels(new Rect(0,0,1280,800),0,0);tex.Apply();File.WriteAllBytes(Path.Combine(_dir,"09-scene-view-"+family+".png"),tex.EncodeToPNG());}
                        finally{camera.targetTexture=old;RenderTexture.active=active;Object.DestroyImmediate(rt);Object.DestroyImmediate(tex);}
                    }
                }
                File.WriteAllLines(Path.Combine(_dir,"reinforcements.csv"),rows);
                File.WriteAllText(Path.Combine(_dir,"fixture.txt"),"Controlled real Spawner+Hooker roster, native timer/sampler/nav/AI/HP with player HP10000, no immunity. Repositioned Spawner near contour edges, 16 native sampler calls each; native timed reinforcements and existing Hook recovery test. Scene View camera renders are technical editor inspection; no gameplay camera settings changed.");
            }
        }
        private IEnumerator VerifySpatialHook09(ProgressionDirector director,ManualNodeDefinition node,PlayerActor player,int count)
        {
            var hookers=director.GetComponentsInChildren<EnemyVariant>().Where(v=>v.ArchetypeId==ArchetypeId.Hooker).ToArray();Assert.AreEqual(count,hookers.Length);
            // Explicit positional fixture in the entered room: use the open right lane for actual connections.
            float lane=node.SpatialLayout.Family==FirstSectorRoomFamily.AsymmetricCourtyard?7:-11;
            for(int i=0;i<hookers.Length;i++){hookers[i].GetComponent<EnemyAI>().InterruptAttack();Assert.IsTrue(hookers[i].GetComponent<NavMeshAgent>().Warp(node.Center+new Vector3(lane-i*.3f,0,-1+i*3.5f)));}
            var oldKeyboard=Keyboard.current;var oldMouse=Mouse.current;bool keyboardEnabled=oldKeyboard!=null&&oldKeyboard.enabled,mouseEnabled=oldMouse!=null&&oldMouse.enabled;
            var background=InputSystem.settings.backgroundBehavior;var editor=InputSystem.settings.editorInputBehaviorInPlayMode;Keyboard keyboard=null;Mouse mouse=null;
            try
            {
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                if(keyboardEnabled)InputSystem.DisableDevice(oldKeyboard);if(mouseEnabled)InputSystem.DisableDevice(oldMouse);keyboard=InputSystem.AddDevice<Keyboard>("RUNEXP04 mixed hook fixture");mouse=InputSystem.AddDevice<Mouse>("RUNEXP04 mixed hook fixture");keyboard.MakeCurrent();mouse.MakeCurrent();InputSystem.QueueStateEvent(keyboard,new KeyboardState());
                yield return null;yield return null;
                float deadline=Time.time+12;
                while(!player.IsBeingPulled||hookers.Any(h=>h.GetComponent<EnemyCombatActions>().ResolvedImpacts==0)){Assert.Less(Time.time,deadline,"Actual HookLine connections/emissions must occur in the mixed room.");yield return null;}
                float lockedAt=Time.time;deadline=Time.time+2;
                while(player.IsBeingPulled){Assert.Less(Time.time,deadline,"Repeated native hooks must release player control.");yield return null;}
                float release=Time.time-lockedAt;var controls=player.GetComponent<CharControlScript>();InputSystem.QueueStateEvent(keyboard,new KeyboardState());yield return null;yield return null;controls.SendMessage("OnApplicationFocus",true);
                deadline=Time.realtimeSinceStartup+4;while(!controls.GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,deadline,"Hook fixture focus/release ready");yield return null;}
                Key right;Assert.IsTrue(Enum.TryParse(GamePreferences.Binding(GameControl.MoveRight).ToString(),true,out right),"Current movement binding must be delivered by the keyboard fixture.");
                Vector3 start=player.transform.position;InputSystem.QueueStateEvent(keyboard,new KeyboardState(right));yield return new WaitForSeconds(.15f);
                Assert.IsFalse(player.IsBeingPulled,"Recovery must contain an actionable movement window.");float moved=Vector3.Distance(start,player.transform.position);
                File.WriteAllText(Path.Combine(_dir,"hook-state-"+node.SpatialLayout.Family+".txt"),"from="+start+" to="+player.transform.position+" ready="+controls.GameplayInputReady+" focus="+controls.DirectInputHasFocus+" stopped="+player.GetComponent<NavMeshAgent>().isStopped+" blocked="+player.GetComponent<AbilityHolder>().BlocksWorldInput+" configured="+controls.ConfiguredMovementDirection+" actual="+controls.ActualMovementVelocity);
                Assert.Greater(moved,.15f);
                InputSystem.QueueStateEvent(keyboard,new KeyboardState());InputSystem.QueueStateEvent(mouse,new MouseState{position=(Vector2)Camera.main.WorldToScreenPoint(player.transform.position+Vector3.back*8)});yield return null;yield return null;
                var dash=controls.dashScript;Assert.IsNotNull(dash);Assert.IsTrue(player.GetComponent<AbilityHolder>().TryUseDash(dash),"Hook recovery dash accepted; cooldown="+dash.GetRemainingCooldown(player.gameObject));Assert.IsTrue(dash.IsDashing(player.gameObject),"Native dash started after pull");yield return new WaitForSeconds(.08f);
                yield return Capture("04-mixed-hooks-"+count+"-seed-"+director.ManualSession.Route.RewardSeed+"-action-window");
                deadline=Time.time+2;while(dash.IsDashing(player.gameObject)){Assert.Less(Time.time,deadline);yield return null;}
                File.WriteAllText(Path.Combine(_dir,"04-hook-recovery-"+director.ManualSession.Route.RewardSeed+"-"+count+".txt"),"One/two real Hookers mixed with production candidates; initial right-lane positioning explicit; AI/damage active, no immunity. Native pull observed; bounded release="+release+"s; real rebound movement="+moved+"m; native dash active observed. Not physical game-feel acceptance.");
            }
            finally
            {
                if(keyboard!=null)InputSystem.RemoveDevice(keyboard);if(mouse!=null)InputSystem.RemoveDevice(mouse);if(keyboardEnabled)InputSystem.EnableDevice(oldKeyboard);if(mouseEnabled)InputSystem.EnableDevice(oldMouse);oldKeyboard?.MakeCurrent();oldMouse?.MakeCurrent();InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editor;
            }
        }
    }
}
