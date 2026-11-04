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
        [UnityTest] public IEnumerator Distribution09BNexusGauntlet()=>PublishedRoute09B(0,0);
        [UnityTest] public IEnumerator Distribution09BNexusBow()=>PublishedRoute09B(1,2);
        [UnityTest] public IEnumerator Distribution09BNexusSpear()=>PublishedRoute09B(2,1);
        [UnityTest] public IEnumerator Distribution09BNexusSeed5A()=>PublishedRoute09B(0,5);
        [UnityTest] public IEnumerator Distribution09BNexusSeed5B()=>PublishedRoute09B(0,5);
        [UnityTest] public IEnumerator Distribution09BNexusSeed6()=>PublishedRoute09B(0,6);
        [UnityTest] public IEnumerator Distribution09BNexusRandom()=>PublishedRoute09B(0,null);
        private IEnumerator PublishedRoute09B(int weapon,ulong? seed)
        {
            _dir=Path.GetFullPath("Docs/validation/artifacts/run-exp-09b/production/"+DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff"));Directory.CreateDirectory(_dir);
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
                    var node=session.Route.Node(nodeId);log.Add("geometry node="+nodeId+" family="+(node.SpatialLayout?.Family??FirstSectorRoomFamily.Rectangle)+" degrees="+((node.SpatialLayout?.QuarterTurns??0)*90));Assert.IsTrue(director.TryCommitDestination(session.Id,nodeId));Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(node.Center));Assert.IsTrue(director.TryEnterNode(session.Id,nodeId));yield return null;
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
                File.WriteAllLines(Path.Combine(_dir,"route09b.txt"),log);
            }
            finally{gameView.Dispose();input.Dispose();if(hadUnlock)PlayerPrefs.SetInt(unlock,unlocked);else PlayerPrefs.DeleteKey(unlock);PlayerPrefs.Save();}
        }

    }
}
