using System.Collections;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ManualRunPlayerFeedbackTests : RunSummaryIsolatedPlayModeFixture
    {
        private int _selection;
        [SetUp] public void Setup() { _selection=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0); }
        [TearDown] public void Teardown() { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,_selection); Time.timeScale=1f; }
        private static IEnumerator Load()
        { yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null; }

        [UnityTest] public IEnumerator PublishedFloorClickActuallyMovesAfterChoosingDestination()
        {
            yield return Load();
            var director=Object.FindFirstObjectByType<ProgressionDirector>(); var player=Object.FindFirstObjectByType<PlayerActor>();
            var ui=director.GetComponent<ManualRunUI>(); Assert.IsTrue(ui.TryChooseDestination(0));
            yield return null; yield return null;
            var original=Mouse.current; bool enabled=original!=null&&original.enabled;
            if(enabled) InputSystem.DisableDevice(original);
            var mouse=InputSystem.AddDevice<Mouse>();
            try
            {
                var controls=player.GetComponent<CharControlScript>(); var agent=player.GetComponent<NavMeshAgent>();
                Vector3 destination=player.transform.position+Vector3.right*4;
                var camera=(Camera)typeof(CharControlScript).GetField("mainCamera",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(controls);
                Vector3 screen=camera.WorldToScreenPoint(destination);
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(screen.x,screen.y) }.WithButton(MouseButton.Left)); InputSystem.Update();
                // Same raycast/filter/NavMesh path as a mouse click, without bypassing it with SetDestination or Warp.
                typeof(CharControlScript).GetMethod("ClickToMove",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(controls,null);
                Assert.Less(Vector3.Distance(destination,agent.destination),.4f,"Ground must be clickable through the authored player's layer mask.");
                InputSystem.QueueStateEvent(mouse,new MouseState { position=new Vector2(screen.x,screen.y) }); InputSystem.Update();
                float deadline=Time.realtimeSinceStartup+4f;
                while(Vector3.Distance(player.transform.position,destination)>.6f)
                { Assert.Less(Time.realtimeSinceStartup,deadline,"The real agent must walk to the clicked floor."); yield return null; }
                Assert.AreEqual(RunPhase.Traversal,director.ManualSession.Phase);
            }
            finally { InputSystem.RemoveDevice(mouse); if(enabled) InputSystem.EnableDevice(original); }
        }

        [UnityTest] public IEnumerator FirstTwoRoomsNativeLethalFeedbackAutomaticallyPresentsRewardAndRespectsDismissal()
        {
            yield return Load();
            var director=Object.FindFirstObjectByType<ProgressionDirector>(); var player=Object.FindFirstObjectByType<PlayerActor>();
            var ui=director.GetComponent<ManualRunUI>(); var run=player.GetComponent<RunBoons>(); var controls=player.GetComponent<CharControlScript>();
            int expectedAcquisitions=0, expectedProtocols=0;
            for(int room=1;room<=2;room++)
            {
                Assert.IsTrue(ui.TryChooseDestination(0));
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(director.ManualSession.Route.Node(room).Center));
                Assert.IsTrue(director.TryEnterNode(director.ManualSession.Id,room));
                yield return null; yield return null;
                var node=director.ManualSession.Route.Node(room);
                var enemies=director.GetComponentsInChildren<Actor>(); Assert.AreEqual(node.Spawns.Count,enemies.Length,
                    "The encounter must materialize every enemy in this run snapshot.");
                expectedAcquisitions+=node.TotalAcquisitions;
                expectedProtocols+=Enumerable.Range(0,node.TotalAcquisitions).Count(ordinal=>node.RewardAt(ordinal)==RunRewardKind.Protocol);
                for(int i=0;i<enemies.Length;i++)
                {
                    enemies[i].SetMaxHealth(1);
                    var nav=enemies[i].GetComponent<NavMeshAgent>();
                    float lateral=(i-(enemies.Length-1)*.5f)*.15f;
                    Assert.IsTrue(nav.Warp(player.transform.position+Vector3.forward*1.2f+Vector3.right*lateral));
                }
                Physics.SyncTransforms();
                using(player.BeginImpactFeedback(.3f))
                { Assert.IsTrue(controls.TryDirectionalBasicAttack(player.transform.position+Vector3.forward*2)); Assert.IsTrue(director.HasManualCompletionCandidate); }
                yield return new WaitForEndOfFrame();
                Assert.AreEqual(RunPhase.RewardPending,director.ManualSession.Phase); Assert.IsFalse(run.IsChoosing,"Confirmed hit-stop still blocks opening.");
                float deadline=Time.realtimeSinceStartup+3f;
                while(!run.IsChoosing) { Assert.Less(Time.realtimeSinceStartup,deadline,"Pending reward must open automatically after feedback."); yield return null; }
                var ticket=run.ManualTicket;
                Assert.AreEqual(node.RewardAt(0),ticket.AnnouncedKind);
                run.CloseManualReward(); yield return null; yield return null;
                Assert.IsFalse(run.IsChoosing,"Explicit dismissal must remain closed."); Assert.AreSame(ticket,run.ManualTicket);
                Assert.IsTrue(director.TryOpenManualReward(director.ManualSession.Id,room,0)); Assert.IsTrue(run.Choose(0));
                for(int ordinal=1;ordinal<node.TotalAcquisitions;ordinal++)
                {
                    Assert.AreEqual(RunPhase.RewardPending,director.ManualSession.Phase,
                        "The room must remain pending until its generated reward sequence is complete.");
                    Assert.AreEqual(node.RewardAt(ordinal),run.ManualTicket.AnnouncedKind);
                    Assert.IsFalse(run.Choose(0),"One confirmation cannot consume the next ticket in the same frame.");
                    yield return null; yield return null;
                    Assert.IsTrue(run.Choose(0));
                }
                Assert.AreEqual(RunPhase.RouteChoice,director.ManualSession.Phase);
                yield return new WaitForSeconds(.8f);
            }
            Assert.AreEqual(expectedAcquisitions,run.Acquired.Count);
            Assert.AreEqual(expectedProtocols,director.ManualSession.ProtocolAcquisitions);
            Assert.AreEqual(2,director.ManualDestinations.Count,"C2 exposes both Repair and Technique routes.");
        }
    }
}
