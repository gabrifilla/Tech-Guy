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
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace TechGuy.Tests
{
    public sealed partial class RunExp03FirstSectorTests
    {
        [UnityTest] public IEnumerator Published06GauntletNexusPortalRealRewardsBossAndReturn()=>PublishedRoute06(0,0);
        [UnityTest] public IEnumerator Published06BowNexusPortalRealRewardsBossAndReturn()=>PublishedRoute06(1,2);
        [UnityTest] public IEnumerator Published06SpearNexusPortalRealRewardsBossAndReturn()=>PublishedRoute06(2,1);
        [UnityTest] public IEnumerator Published06RandomUnassistedRouteUsesNormalPresentation()=>PublishedRoute06(0,null);
        [UnityTest] public IEnumerator Published06FocusModalAndRemappedSubmitCannotLeakIntoCombat()
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-06/input/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            const string binding="TechGuy.Settings.DirectBinding.v2.Skill1";bool had=PlayerPrefs.HasKey(binding);int old=PlayerPrefs.GetInt(binding);
            using(var input=new PublishedInput06())using(var size=new PublishedGameView06(1280,800))
            try
            {
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0);SessionState.SetString(RunSeedSource.NextEditorSeedKey,"63");yield return SceneManager.LoadSceneAsync("FirstSector");for(int i=0;i<4;i++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var run=player.GetComponent<RunBoons>();var node=session.Route.Node(1);player.AddDamageImmunity(director);
                Assert.IsTrue(director.TryCommitDestination(session.Id,1));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,1));yield return null;
                foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
                float until=Time.realtimeSinceStartup+10;while(session.Phase==RunPhase.Encounter){Assert.Less(Time.realtimeSinceStartup,until);yield return null;}
                while(session.Phase==RunPhase.RewardPending&&session.RewardOrdinal<node.TotalAcquisitions-1)
                {if(!run.IsChoosing)Assert.IsTrue(director.TryOpenManualReward(session.Id,1,session.RewardOrdinal));for(int i=0;i<3;i++)yield return null;Assert.IsTrue(player.GetComponent<RewardSelectionView>().Confirm());yield return null;}
                if(!run.IsChoosing)Assert.IsTrue(director.TryOpenManualReward(session.Id,1,session.RewardOrdinal));for(int i=0;i<3;i++)yield return null;
                var view=player.GetComponent<RewardSelectionView>();var pause=player.GetComponent<PauseMenuUI>();var ticket=run.ManualTicket;int owned=run.Acquired.Count;
                view.SendMessage("OnApplicationFocus",false);input.Hold(Key.Enter);yield return null;yield return null;Assert.AreEqual(owned,run.Acquired.Count);
                view.SendMessage("OnApplicationFocus",true);yield return null;yield return null;Assert.AreEqual(owned,run.Acquired.Count,"Held submit after focus regain must require release");
                input.Release();yield return null;yield return null;view.ToggleDetails();yield return input.Press(KeyCode.Return);Assert.AreEqual(owned,run.Acquired.Count);Assert.IsTrue(view.DetailsVisible);
                yield return input.Press(KeyCode.Escape);Assert.IsFalse(view.DetailsVisible);Assert.IsFalse(pause.IsPaused);
                PlayerPrefs.SetInt(binding,(int)KeyCode.Return);pause.Pause();yield return null;pause.Resume();yield return null;yield return null;
                var model=RewardChoicePresentation.For(run,new RunBoons.Offer(BoonReworkCatalog.UppercutId,"Uppercut do Eclipse","Q: descrição"));Assert.IsTrue(model.Scope.Contains(GamePreferences.BindingLabel(GameControl.Skill1)));
                int casts=0,basics=0;player.GetComponent<AbilityHolder>().AbilityUsed+=slot=>casts++;player.GetComponent<CharControlScript>().BasicAttackPerformed+=()=>basics++;
                string id=run.Choices[view.Selected].Id;input.Hold(Key.Enter);yield return null;yield return null;
                Assert.AreEqual(RunRewardTicketState.Resolved,ticket.State);Assert.AreEqual(id,ticket.ResultId);Assert.AreEqual(owned+1,run.Acquired.Count);Assert.AreEqual(0,casts);Assert.AreEqual(0,basics);Assert.IsFalse(view.Confirm());
                int next=session.Route.Paths.First().SkipWhile(n=>n!=1).Skip(1).First();Assert.IsTrue(director.TryCommitDestination(session.Id,next));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(session.Route.Node(next).Center));Assert.IsTrue(director.TryEnterNode(session.Id,next));
                yield return null;yield return null;Assert.AreEqual(0,casts,"Held submit cannot become remapped skill in next encounter");
                input.Release();yield return null;yield return null;yield return input.Press(KeyCode.Return);yield return new WaitForSeconds(.15f);Assert.Greater(casts,0,"Fresh released press works after UI ownership returns");
                yield return Capture("06-remapped-Enter-recovered-native-combat");
                File.WriteAllText(Path.Combine(_dir,"input06.txt"),"Real final room1 ticket resolved exactly once. UI ignores focus loss/held return, Escape closes details before pause. Skill1 temporarily remapped to Enter; submit does not cast through UI or enter the next real encounter held. Fresh press after release casts. Native core release gates unchanged; player immunity only in fixture.");
            }
            finally{if(had)PlayerPrefs.SetInt(binding,old);else PlayerPrefs.DeleteKey(binding);PlayerPrefs.Save();}
        }

        private IEnumerator PublishedRoute06(int weapon,ulong? seed)
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-06/gameplay/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
            string unlock="TechGuy.Loadout.Unlocked."+weapon;bool hadUnlock=PlayerPrefs.HasKey(unlock);int unlocked=PlayerPrefs.GetInt(unlock);
            var input=new PublishedInput06();
            var gameView=new PublishedGameView06(weapon==0&&seed.HasValue?1160:weapon==2?1920:1280,weapon==0&&seed.HasValue?600:weapon==2?1080:800);
            try
            {
                // Loadout access is temporary fixture setup, never a claim of earning the unlock.
                if(weapon>0)PlayerPrefs.SetInt(unlock,1);PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,weapon);
                if(seed.HasValue)SessionState.SetString(RunSeedSource.NextEditorSeedKey,seed.Value.ToString());else SessionState.EraseString(RunSeedSource.NextEditorSeedKey);
                yield return SceneManager.LoadSceneAsync("NexusBlockout");for(int i=0;i<4;i++)yield return null;
                var hubPlayer=Object.FindFirstObjectByType<PlayerActor>();var lobby=Object.FindFirstObjectByType<LobbyInteraction>();
                var stations=(LobbyInteraction.Station[])typeof(LobbyInteraction).GetField("_stations",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(lobby);
                var entry=stations.Single(p=>p.destinationScene=="FirstSector");
                Assert.IsTrue(NavMesh.SamplePosition(entry.anchor.position,out var entryPoint,2,NavMesh.AllAreas));Assert.IsTrue(hubPlayer.GetComponent<NavMeshAgent>().Warp(entryPoint.position));
                float readyEnd=Time.realtimeSinceStartup+5;while(!hubPlayer.GetComponent<CharControlScript>().GameplayInputReady){Assert.Less(Time.realtimeSinceStartup,readyEnd);yield return null;}
                yield return input.Press(GamePreferences.Binding(GamePreferences.InteractionAction));
                float loadEnd=Time.realtimeSinceStartup+15;while(SceneManager.GetActiveScene().name!="FirstSector"){Assert.Less(Time.realtimeSinceStartup,loadEnd);yield return null;}for(int i=0;i<4;i++)yield return null;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();var player=Object.FindFirstObjectByType<PlayerActor>();var session=director.ManualSession;var run=player.GetComponent<RunBoons>();
                Assert.IsFalse(TitanicConductorPlaytestRewards.Enabled);Assert.IsFalse(session.Route.TitanicConductorPlaytest);Assert.AreEqual((RunWeaponFamily)weapon,run.WeaponModifiers.Family);
                Assert.IsNull(Object.FindFirstObjectByType<RunExp03RewardLab>());if(seed.HasValue)Assert.AreEqual(seed.Value,session.Route.RewardSeed);
                player.AddDamageImmunity(director);
                var content=AssetDatabase.LoadAssetAtPath<FirstSectorContentDefinition>("Assets/_Project/ScriptableObjects/Run/FirstSectorContent.asset");
                ulong actualSeed=session.Route.RewardSeed.Value;
                var path=session.Route.Paths.OrderByDescending(p=>p.Sum(id=>session.Route.Node(id).Spawns.Count(s=>s.Prefab.name=="Fragmento"||s.Prefab.name=="Emissor"))+p.Count(id=>content.HasCover(actualSeed,session.Route.Node(id)))*2).First();
                var log=new List<string>{"Normal Nexus portal, native director/session/rosters/RNG/tickets. Automated warp/kill completion, player immunity and temporary loadout access only in fixture. No reward grants, review menus, forced offers or guarantee of synergy.","weapon="+weapon+" seed="+actualSeed+" random="+(!seed.HasValue)+" path="+string.Join("/",path)};
                int tickets=0;bool canceled=false;bool sawArt=false,sawCover=false;
                foreach(int nodeId in path.Skip(1))
                {
                    var node=session.Route.Node(nodeId);Assert.IsTrue(director.TryCommitDestination(session.Id,nodeId));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,nodeId));yield return null;
                    if(session.Phase==RunPhase.RepairPending){Assert.IsTrue(director.TryResolveRepair(session.Id,nodeId,false));yield return null;continue;}
                    foreach(var coin in director.GetComponentsInChildren<CoinDrop>())Object.DestroyImmediate(coin);
                    if(node.Kind==ManualNodeKind.Combat)
                    {
                        var arena=director.ManualEnvironment.Node(nodeId).Arena;Assert.IsTrue(arena.GetComponent<FirstSectorEnvironmentArt>().Visible);
                        sawCover|=content.HasCover(actualSeed,node);sawArt|=director.GetComponentsInChildren<FirstSectorEnemyPose>().Length>0;
                        foreach(var actor in director.GetComponentsInChildren<Actor>())
                        {var agent=actor.GetComponent<NavMeshAgent>();var navPath=new NavMeshPath();Assert.IsTrue(agent.CalculatePath(player.transform.position,navPath));Assert.AreEqual(NavMeshPathStatus.PathComplete,navPath.status);}
                        yield return new WaitForSeconds(.4f);yield return Capture("06-normal-node-"+nodeId+"-"+(content.HasCover(actualSeed,node)?"cover":"open")+"-weapon-"+weapon);
                    }
                    foreach(var actor in director.GetComponentsInChildren<Actor>())actor.TakeDamage(1000000);
                    float end=Time.realtimeSinceStartup+10;while(session.Phase==RunPhase.Encounter||session.Phase==RunPhase.FinalResolution){Assert.Less(Time.realtimeSinceStartup,end);yield return null;}
                    while(session.Phase==RunPhase.RewardPending)
                    {
                        if(!run.IsChoosing)Assert.IsTrue(director.TryOpenManualReward(session.Id,nodeId,session.RewardOrdinal));
                        for(int frame=0;frame<3;frame++)yield return null;
                        var ui=player.GetComponent<RunRewardUI>();var view=player.GetComponent<RewardSelectionView>();
                        Assert.IsNotNull(view);Assert.AreSame(view,ui.SelectionView);Assert.AreEqual(1,player.GetComponents<RewardSelectionView>().Length);
                        Assert.IsTrue(ui.enabled);Assert.IsTrue(view.enabled);Assert.IsFalse(view.ShowReviewControls);Assert.AreEqual(RewardSelectionView.Layout.Synthesis05,view.Concept);Assert.IsTrue(view.Visible);
                        var ticket=run.ManualTicket;Assert.AreEqual(RunRewardTicketState.Pending,ticket.State);var ids=run.Choices.Select(o=>o.Id).ToArray();int acquired=run.Acquired.Count;
                        if(!canceled)
                        {
                            view.ToggleDetails();Assert.IsFalse(view.Confirm());Assert.IsTrue(view.Cancel());Assert.IsFalse(view.DetailsVisible);Assert.AreEqual(acquired,run.Acquired.Count);
                            Assert.IsTrue(view.Cancel());Assert.AreEqual(RunRewardTicketState.Pending,ticket.State);Assert.IsFalse(run.IsChoosing);
                            Assert.IsTrue(director.TryOpenManualReward(session.Id,nodeId,session.RewardOrdinal));for(int frame=0;frame<3;frame++)yield return null;
                            CollectionAssert.AreEqual(ids,run.Choices.Select(o=>o.Id).ToArray());Assert.AreSame(ticket,run.ManualTicket);canceled=true;
                        }
                        Assert.Greater(run.Choices.Count,0);int ordinal=session.RewardOrdinal;string id=run.Choices[0].Id;view.Select(0);
                        yield return Capture("06-published-C05-weapon-"+weapon+"-node-"+nodeId+"-ticket-"+ordinal);
                        Assert.IsTrue(view.Confirm());Assert.IsFalse(view.Confirm());Assert.AreEqual(RunRewardTicketState.Resolved,ticket.State);Assert.AreEqual(id,ticket.ResultId);
                        Assert.AreEqual(acquired+(id==RunBoonOfferCatalog.ExhaustedId?0:1),run.Acquired.Count);tickets++;
                        log.Add("node="+nodeId+" ordinal="+ordinal+" id="+id+" resolvedOnce=true view=C05");yield return null;
                    }
                }
                Assert.Greater(tickets,0);Assert.IsTrue(canceled);Assert.AreEqual(RunPhase.ExtractionAvailable,session.Phase);
                if(seed.HasValue){Assert.IsTrue(sawCover);Assert.IsTrue(sawArt);}
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(director.ManualExtractionPortal.transform.position));Assert.IsTrue(director.ManualExtractionPortal.TryEnter());
                float deadline=Time.realtimeSinceStartup+10;while(SceneManager.GetActiveScene().name!="NexusBlockout"){Assert.Less(Time.realtimeSinceStartup,deadline);yield return null;}
                Assert.AreEqual(RunOutcome.Extracted,session.Outcome);Assert.AreEqual(RunPhase.Ended,session.Phase);yield return Capture("06-returned-Nexus-weapon-"+weapon);
                File.WriteAllLines(Path.Combine(_dir,"route06.txt"),log);
            }
            finally{gameView.Dispose();input.Dispose();if(hadUnlock)PlayerPrefs.SetInt(unlock,unlocked);else PlayerPrefs.DeleteKey(unlock);PlayerPrefs.Save();}
        }

        private sealed class PublishedGameView06 : IDisposable
        {
            private readonly EditorWindow _view;private readonly object _group;private readonly int _oldIndex,_index;
            private const BindingFlags Flags=BindingFlags.Instance|BindingFlags.Public|BindingFlags.NonPublic;
            public PublishedGameView06(int width,int height)
            {
                var assembly=typeof(Editor).Assembly;var type=assembly.GetType("UnityEditor.GameView");_view=Resources.FindObjectsOfTypeAll(type).Cast<EditorWindow>().First();_oldIndex=(int)type.GetProperty("selectedSizeIndex",Flags).GetValue(_view);
                var sizes=assembly.GetType("UnityEditor.GameViewSizes");_group=sizes.GetMethod("GetGroup").Invoke(sizes.BaseType.GetProperty("instance").GetValue(null),new object[]{0});
                var sizeType=assembly.GetType("UnityEditor.GameViewSize");var enumType=assembly.GetType("UnityEditor.GameViewSizeType");
                var size=Activator.CreateInstance(sizeType,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance,null,new object[]{Enum.ToObject(enumType,1),width,height,"RUN-EXP-06 published capture"},null);
                _group.GetType().GetMethod("AddCustomSize").Invoke(_group,new[]{size});_index=(int)_group.GetType().GetMethod("GetTotalCount").Invoke(_group,null)-1;
                type.GetProperty("selectedSizeIndex",Flags).SetValue(_view,_index);_view.Focus();
            }
            public void Dispose()
            {
                if(_view)_view.GetType().GetProperty("selectedSizeIndex",Flags).SetValue(_view,_oldIndex);
                _group.GetType().GetMethod("RemoveCustomSize").Invoke(_group,new object[]{_index});
            }
        }

        private sealed class PublishedInput06 : IDisposable
        {
            private readonly Keyboard _oldKeyboard,_keyboard;private readonly Mouse _oldMouse,_mouse;
            private readonly bool _keyboardEnabled,_mouseEnabled;
            private readonly InputSettings.BackgroundBehavior _background;
            private readonly InputSettings.EditorInputBehaviorInPlayMode _editorInput;
            public PublishedInput06()
            {
                _background=InputSystem.settings.backgroundBehavior;_editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;
                InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                _oldKeyboard=Keyboard.current;_oldMouse=Mouse.current;_keyboardEnabled=_oldKeyboard!=null&&_oldKeyboard.enabled;_mouseEnabled=_oldMouse!=null&&_oldMouse.enabled;
                if(_keyboardEnabled)InputSystem.DisableDevice(_oldKeyboard);if(_mouseEnabled)InputSystem.DisableDevice(_oldMouse);
                _keyboard=InputSystem.AddDevice<Keyboard>("Published06 route");_mouse=InputSystem.AddDevice<Mouse>("Published06 route");_keyboard.MakeCurrent();_mouse.MakeCurrent();
            }
            public IEnumerator Press(KeyCode code)
            {
                if(code>=KeyCode.Mouse0&&code<=KeyCode.Mouse4)
                {
                    var button=code==KeyCode.Mouse0?MouseButton.Left:code==KeyCode.Mouse1?MouseButton.Right:code==KeyCode.Mouse2?MouseButton.Middle:code==KeyCode.Mouse3?MouseButton.Back:MouseButton.Forward;
                    InputSystem.QueueStateEvent(_mouse,new MouseState().WithButton(button));
                }
                else
                {
                    string name=code==KeyCode.Return?"Enter":code==KeyCode.LeftControl?"LeftCtrl":code==KeyCode.RightControl?"RightCtrl":code>=KeyCode.Alpha0&&code<=KeyCode.Alpha9?"Digit"+((int)code-(int)KeyCode.Alpha0):code.ToString();
                    InputSystem.QueueStateEvent(_keyboard,new KeyboardState((Key)Enum.Parse(typeof(Key),name,true)));
                }
                yield return null;InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.QueueStateEvent(_mouse,new MouseState());yield return null;yield return null;
            }
            public void Hold(Key key)=>InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));
            public void Release(){InputSystem.QueueStateEvent(_keyboard,new KeyboardState());InputSystem.QueueStateEvent(_mouse,new MouseState());}
            public void Dispose()
            {
                InputSystem.RemoveDevice(_keyboard);InputSystem.RemoveDevice(_mouse);if(_keyboardEnabled)InputSystem.EnableDevice(_oldKeyboard);if(_mouseEnabled)InputSystem.EnableDevice(_oldMouse);_oldKeyboard?.MakeCurrent();_oldMouse?.MakeCurrent();
                InputSystem.settings.backgroundBehavior=_background;InputSystem.settings.editorInputBehaviorInPlayMode=_editorInput;
            }
        }
    }
}
