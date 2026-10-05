using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ManualPublishedRunPlayModeTests
    {
        private int _oldSelection;
        [SetUp] public void Setup() { _oldSelection=PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey,0); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,0); }
        [TearDown] public void Teardown() { PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,_oldSelection); Time.timeScale=1f; }

        private static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null; yield return null; yield return null;
        }

        [UnityTest] public IEnumerator FourPublishedPathsReachActualBossAndExtractToNexusWithFreshRuns()
        {
            System.Guid previous=System.Guid.Empty;
            for(int path=0;path<4;path++)
            {
                yield return Load("NexusLobby");
                Assert.IsNotNull(Object.FindFirstObjectByType<LobbyInteraction>());
                yield return Load("FirstSector");
                var director=Object.FindFirstObjectByType<ProgressionDirector>(); var player=Object.FindFirstObjectByType<PlayerActor>();
                Assert.AreEqual(RunProgressionMode.ManualSlice,director.ActiveProgressionMode);
                Assert.AreEqual(1,Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None).Count(surface=>surface.isActiveAndEnabled),"Exactly one mode owns active navigation.");
                var session=director.ManualSession; Assert.AreNotEqual(previous,session.Id); previous=session.Id;
                Assert.AreEqual(0,session.MutationAcquisitions); Assert.AreEqual(player.maxHealth,player.health);
                var run=player.GetComponent<RunBoons>(); Assert.IsEmpty(run.Acquired); Assert.AreEqual(0,player.GetComponent<BreakerGauntletCombat>().Energy);
                int[] nodes=path%2==0?new[]{1,2,3,5}:new[]{1,2,4,5};
                nodes=nodes.Concat(path<2?new[]{6,9,10}:new[]{7,8,9,10}).ToArray();
                foreach(int node in nodes)
                {
                    Assert.IsTrue(director.TryCommitDestination(session.Id,node),"Commit "+node+" path "+path);
                    var agent=player.GetComponent<NavMeshAgent>(); Assert.IsTrue(agent.Warp(session.Route.Node(node).Center));
                    Assert.IsTrue(director.TryEnterNode(session.Id,node));
                    if(session.Phase==RunPhase.RepairPending)
                    { Assert.IsTrue(director.TryResolveRepair(session.Id,node,false)); yield return null; continue; }
                    // Deterministic fixture kill; actual authored composition, Actor.Died and frontier execute.
                    foreach(var enemy in director.GetComponentsInChildren<Actor>()) enemy.TakeDamage(1000000f);
                    float deadline=Time.realtimeSinceStartup+3f;
                    while(session.Phase==RunPhase.Encounter||session.Phase==RunPhase.FinalResolution)
                    { Assert.Less(Time.realtimeSinceStartup,deadline); yield return null; }
                    while(session.Phase==RunPhase.RewardPending)
                    {
                        yield return null; yield return null;
                        if(!run.IsChoosing) Assert.IsTrue(director.TryOpenManualReward(session.Id,node,session.RewardOrdinal));
                        Assert.Greater(run.Choices.Count,0); Assert.IsTrue(run.Choose(0));
                    }
                    if(node==5) Assert.AreEqual(1,session.MutationAcquisitions);
                    yield return null;
                }
                Assert.AreEqual(RunPhase.ExtractionAvailable,session.Phase);
                var portal=director.ManualExtractionPortal; Assert.IsNotNull(portal);
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(portal.transform.position)); Assert.IsTrue(portal.TryEnter());
                Assert.AreEqual(RunOutcome.Extracted,session.Outcome);
                float end=Time.realtimeSinceStartup+8f;
                while(SceneManager.GetActiveScene().name!="NexusLobby") { Assert.Less(Time.realtimeSinceStartup,end); yield return null; }
                yield return null; Assert.AreEqual(RunPhase.Ended,session.Phase);
            }
        }

        [UnityTest] public IEnumerator PublishedDeathReturnsToNexusAndBowStillUsesLegacyWorld()
        {
            yield return Load("FirstSector");
            var director=Object.FindFirstObjectByType<ProgressionDirector>(); var session=director.ManualSession;
            Object.FindFirstObjectByType<PlayerActor>().TakeDamage(1000000f);
            float end=Time.realtimeSinceStartup+8f;
            while(SceneManager.GetActiveScene().name!="NexusLobby") { Assert.Less(Time.realtimeSinceStartup,end); yield return null; }
            Assert.AreEqual(RunOutcome.Dead,session.Outcome); Assert.AreEqual(RunPhase.Ended,session.Phase);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,1); yield return Load("FirstSector");
            director=Object.FindFirstObjectByType<ProgressionDirector>();
            Assert.AreEqual(RunProgressionMode.ProceduralLegacy,director.ActiveProgressionMode); Assert.IsNull(director.ManualSession);
            Assert.IsTrue(director.IsStageBuilt); Assert.IsNotNull(director.CurrentGraph);
            Assert.AreEqual(RunWeaponFamily.Bow,WeaponRunModifiers.Identify(Object.FindFirstObjectByType<PlayerActor>().CurrentWeapon));
        }

        [UnityTest] public IEnumerator PublishedSpearStillUsesLegacyWorldWithLocalProtocolInventory()
        {
            const string unlockKey = "TechGuy.Loadout.Unlocked.2";
            bool hadUnlock = PlayerPrefs.HasKey(unlockKey); int previousUnlock = PlayerPrefs.GetInt(unlockKey);
            try
            {
                PlayerPrefs.SetInt(unlockKey, 1); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 2);
                yield return Load("FirstSector");
                var director = Object.FindFirstObjectByType<ProgressionDirector>(); var player = Object.FindFirstObjectByType<PlayerActor>();
                Assert.That(director.ActiveProgressionMode, Is.EqualTo(RunProgressionMode.ProceduralLegacy));
                Assert.That(director.ManualSession, Is.Null); Assert.That(director.IsStageBuilt, Is.True);
                Assert.That(WeaponRunModifiers.Identify(player.CurrentWeapon), Is.EqualTo(RunWeaponFamily.Spear));
                var run = player.GetComponent<RunBoons>(); var runtime = player.GetComponent<ProtocolRuntime>();
                Assert.That(run.Protocols, Is.Not.Null); Assert.That(runtime.Inventory, Is.SameAs(run.Protocols));
                Assert.That(run.Protocols.Revision, Is.Zero);
            }
            finally
            {
                if (hadUnlock) PlayerPrefs.SetInt(unlockKey, previousUnlock); else PlayerPrefs.DeleteKey(unlockKey);
            }
        }
        [UnityTest] public IEnumerator PublishedMapOwnsInputAndClosingWaitsForClickRelease()
        {
            yield return Load("FirstSector");
            var player=Object.FindFirstObjectByType<PlayerActor>(); var ui=Object.FindFirstObjectByType<ManualRunUI>();
            var holder=player.GetComponent<AbilityHolder>(); var controls=player.GetComponent<CharControlScript>();
            var originalMouse=UnityEngine.InputSystem.Mouse.current;
            bool originalEnabled=originalMouse!=null&&originalMouse.enabled;
            var settings=UnityEngine.InputSystem.InputSystem.settings;
            var oldBackground=settings.backgroundBehavior;
            var oldEditorInput=settings.editorInputBehaviorInPlayMode;
            // MCP runs without Game View focus; keep the synthetic held button alive across updates.
            settings.backgroundBehavior=UnityEngine.InputSystem.InputSettings.BackgroundBehavior.IgnoreFocus;
            settings.editorInputBehaviorInPlayMode=UnityEngine.InputSystem.InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            if(originalEnabled) UnityEngine.InputSystem.InputSystem.DisableDevice(originalMouse);
            var mouse=UnityEngine.InputSystem.InputSystem.AddDevice<UnityEngine.InputSystem.Mouse>();
            try
            {
                Assert.IsTrue(ui.IsOpen,"Initial map open"); Assert.IsTrue(holder.BlocksWorldInput,"Initial map blocks input");
                Assert.IsFalse(holder.TryUseAbility(0)); Assert.IsFalse(controls.TryDirectionalBasicAttack(player.transform.position+Vector3.forward));
                Assert.IsTrue(player.GetComponent<NavMeshAgent>().isStopped,"Initial map stops movement");
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState().WithButton(UnityEngine.InputSystem.LowLevel.MouseButton.Left));
                UnityEngine.InputSystem.InputSystem.Update();
                Assert.IsTrue(mouse.leftButton.isPressed,"Fixture close click held");
                typeof(ManualRunUI).GetMethod("ReleasedAction",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance).Invoke(ui,null);
                yield return null;
                Assert.IsFalse(ui.IsOpen); Assert.IsTrue(holder.BlocksWorldInput,"Held close click keeps input locked");
                Assert.IsFalse(holder.TryUseAbility(0));
                UnityEngine.InputSystem.InputSystem.QueueStateEvent(mouse,new UnityEngine.InputSystem.LowLevel.MouseState());
                UnityEngine.InputSystem.InputSystem.Update(); yield return null; yield return null;
                Assert.IsFalse(holder.BlocksWorldInput); Assert.IsFalse(player.GetComponent<NavMeshAgent>().isStopped);
                Time.timeScale=0f;
                var director=Object.FindFirstObjectByType<ProgressionDirector>();
                Assert.IsFalse(director.TryCommitDestination(director.ManualSession.Id,1)); Time.timeScale=1f;
            }
            finally
            {
                UnityEngine.InputSystem.InputSystem.RemoveDevice(mouse);
                settings.backgroundBehavior=oldBackground;
                settings.editorInputBehaviorInPlayMode=oldEditorInput;
                if(originalEnabled) UnityEngine.InputSystem.InputSystem.EnableDevice(originalMouse);
            }
        }
    }
}
