using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ManualPublishedRunPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private readonly string[] _preferenceKeys =
        { WeaponLoadout.PreferenceKey, "TechGuy.Loadout.Unlocked.1", "TechGuy.Loadout.Unlocked.2" };
        private readonly int[] _oldPreferences = new int[3];
        private readonly bool[] _hadPreferences = new bool[3];
        [SetUp]
        public void Setup()
        {
            for (int i = 0; i < _preferenceKeys.Length; i++)
            { _hadPreferences[i] = PlayerPrefs.HasKey(_preferenceKeys[i]); _oldPreferences[i] = PlayerPrefs.GetInt(_preferenceKeys[i], 0); }
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(_preferenceKeys[1], 1); PlayerPrefs.SetInt(_preferenceKeys[2], 1);
        }
        [TearDown]
        public void Teardown()
        {
            for (int i = 0; i < _preferenceKeys.Length; i++)
                if (_hadPreferences[i]) PlayerPrefs.SetInt(_preferenceKeys[i], _oldPreferences[i]); else PlayerPrefs.DeleteKey(_preferenceKeys[i]);
            PlayerPrefs.Save();
            Time.timeScale = 1f;
        }

        private static IEnumerator Load(string scene)
        {
            yield return SceneManager.LoadSceneAsync(scene);
            yield return null; yield return null; yield return null;
        }

        [UnityTest] public IEnumerator FourPublishedPathsReachActualBossAndExtractToNexusWithFreshRuns()
        {
            System.Guid previous=System.Guid.Empty;
            for(int family=0;family<3;family++)
            for(int path=0;path<4;path++)
            {
                PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,family);
                yield return Load("NexusLobby");
                Assert.IsNotNull(Object.FindFirstObjectByType<LobbyInteraction>());
                yield return Load("FirstSector");
                var director=Object.FindFirstObjectByType<ProgressionDirector>(); var player=Object.FindFirstObjectByType<PlayerActor>();
                Assert.AreEqual(RunProgressionMode.ManualSlice,director.ActiveProgressionMode);
                Assert.IsTrue(director.ManualEnvironment.HasGeneratedLayout);
                Assert.AreEqual((RunWeaponFamily)family,WeaponRunModifiers.Identify(player.CurrentWeapon));
                Assert.AreEqual(1,Object.FindObjectsByType<Unity.AI.Navigation.NavMeshSurface>(FindObjectsSortMode.None).Count(surface=>surface.isActiveAndEnabled),"Exactly one mode owns active navigation.");
                var session=director.ManualSession; Assert.AreNotEqual(previous,session.Id); previous=session.Id;
                Assert.AreEqual(0,session.MutationAcquisitions); Assert.AreEqual(player.maxHealth,player.health);
                var run=player.GetComponent<RunBoons>(); Assert.IsEmpty(run.Acquired);
                Assert.AreEqual((RunWeaponFamily)family,run.WeaponModifiers.Family);
                Assert.AreEqual(session.Id,run.Protocols.SessionId);
                Assert.AreSame(run.Protocols,player.GetComponent<ProtocolRuntime>().Inventory);
                if(family==0) Assert.AreEqual(0,player.GetComponent<BreakerGauntletCombat>().Energy);
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

        [UnityTest] public IEnumerator PublishedDeathReturnsToNexusAndBowUsesGeneratedRoomsWithCompatibleInventory()
        {
            yield return Load("FirstSector");
            var director=Object.FindFirstObjectByType<ProgressionDirector>(); var session=director.ManualSession;
            Object.FindFirstObjectByType<PlayerActor>().TakeDamage(1000000f);
            float end=Time.realtimeSinceStartup+8f;
            while(SceneManager.GetActiveScene().name!="NexusLobby") { Assert.Less(Time.realtimeSinceStartup,end); yield return null; }
            Assert.AreEqual(RunOutcome.Dead,session.Outcome); Assert.AreEqual(RunPhase.Ended,session.Phase);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey,1); yield return Load("FirstSector");
            director=Object.FindFirstObjectByType<ProgressionDirector>();
            Assert.AreEqual(RunProgressionMode.ManualSlice,director.ActiveProgressionMode); Assert.IsNotNull(director.ManualSession);
            Assert.IsTrue(director.IsStageBuilt); Assert.IsNull(director.CurrentGraph);
            Assert.IsTrue(director.ManualEnvironment.HasGeneratedLayout);
            var bowPlayer=Object.FindFirstObjectByType<PlayerActor>(); var bowRun=bowPlayer.GetComponent<RunBoons>();
            Assert.AreEqual(RunWeaponFamily.Bow,WeaponRunModifiers.Identify(bowPlayer.CurrentWeapon));
            Assert.AreEqual(RunWeaponFamily.Bow,bowRun.WeaponModifiers.Family);
            Assert.AreEqual(director.ManualSession.Id,bowRun.Protocols.SessionId);
            Assert.AreSame(bowRun.Protocols,bowPlayer.GetComponent<ProtocolRuntime>().Inventory);
            Assert.IsNotEmpty(ManualRewardCatalog.Eligible(RunRewardKind.Mutation,bowRun.WeaponModifiers,bowPlayer.CurrentWeapon,bowRun.Protocols));
        }

        [UnityTest] public IEnumerator PublishedSpearUsesGeneratedRoomsWithCompatibleLocalProtocolInventory()
        {
            const string unlockKey = "TechGuy.Loadout.Unlocked.2";
            bool hadUnlock = PlayerPrefs.HasKey(unlockKey); int previousUnlock = PlayerPrefs.GetInt(unlockKey);
            try
            {
                PlayerPrefs.SetInt(unlockKey, 1); PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 2);
                yield return Load("FirstSector");
                var director = Object.FindFirstObjectByType<ProgressionDirector>(); var player = Object.FindFirstObjectByType<PlayerActor>();
                Assert.That(director.ActiveProgressionMode, Is.EqualTo(RunProgressionMode.ManualSlice));
                Assert.That(director.ManualSession, Is.Not.Null); Assert.That(director.IsStageBuilt, Is.True);
                Assert.That(director.ManualEnvironment.HasGeneratedLayout, Is.True);
                Assert.That(WeaponRunModifiers.Identify(player.CurrentWeapon), Is.EqualTo(RunWeaponFamily.Spear));
                var run = player.GetComponent<RunBoons>(); var runtime = player.GetComponent<ProtocolRuntime>();
                Assert.That(run.Protocols, Is.Not.Null); Assert.That(runtime.Inventory, Is.SameAs(run.Protocols));
                Assert.That(run.Protocols.Revision, Is.Zero);
                Assert.That(run.Protocols.SessionId, Is.EqualTo(director.ManualSession.Id));
                Assert.That(run.WeaponModifiers.Family, Is.EqualTo(RunWeaponFamily.Spear));
                Assert.That(ManualRewardCatalog.Eligible(RunRewardKind.Mutation,run.WeaponModifiers,player.CurrentWeapon,run.Protocols), Is.Not.Empty);
            }
            finally
            {
                if (hadUnlock) PlayerPrefs.SetInt(unlockKey, previousUnlock); else PlayerPrefs.DeleteKey(unlockKey);
            }
        }
        [UnityTest]
        public IEnumerator PublishedWeaponSwapsKeepFamilyInventoryAndOwnedAbilityCopiesWithinOneSession()
        {
            yield return Load("FirstSector");
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var run = player.GetComponent<RunBoons>();
            var session = director.ManualSession;
            var protocols = run.Protocols;
            var gauntlet = run.WeaponModifiers;
            Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.FlurryEcho), out var flurry), Is.True);
            Assert.That(gauntlet.Add(flurry), Is.True);
            Assert.That(WeaponLoadout.Select(player, 1), Is.True);
            yield return null;
            var bow = run.WeaponModifiers;
            Assert.That(bow.Family, Is.EqualTo(RunWeaponFamily.Bow));
            Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.Ricochet), out var ricochet), Is.True);
            Assert.That(bow.Add(ricochet), Is.True);
            AssertOwnedClone(player, run);
            Assert.That(WeaponLoadout.Select(player, 2), Is.True);
            yield return null;
            var spear = run.WeaponModifiers;
            Assert.That(spear.Family, Is.EqualTo(RunWeaponFamily.Spear));
            Assert.That(WeaponRunModifiers.TryDefinition(WeaponRunModifiers.CatalogId(WeaponBoon.EchoThrust), out var echo), Is.True);
            Assert.That(spear.Add(echo), Is.True);
            AssertOwnedClone(player, run);
            Assert.That(WeaponLoadout.Select(player, 0), Is.True);
            yield return null;
            Assert.That(run.WeaponModifiers, Is.SameAs(gauntlet));
            Assert.That(run.WeaponModifiers.Rank(WeaponBoon.FlurryEcho), Is.EqualTo(1));
            AssertOwnedClone(player, run);
            Assert.That(WeaponLoadout.Select(player, 1), Is.True);
            yield return null;
            Assert.That(run.WeaponModifiers, Is.SameAs(bow));
            Assert.That(run.WeaponModifiers.Rank(WeaponBoon.Ricochet), Is.EqualTo(1));
            Assert.That(WeaponLoadout.Select(player, 2), Is.True);
            yield return null;
            Assert.That(run.WeaponModifiers, Is.SameAs(spear));
            Assert.That(run.WeaponModifiers.Rank(WeaponBoon.EchoThrust), Is.EqualTo(1));
            Assert.That(director.ManualSession, Is.SameAs(session));
            Assert.That(run.Protocols, Is.SameAs(protocols));
            Assert.That(protocols.SessionId, Is.EqualTo(session.Id));
            Assert.That(protocols.Revision, Is.Zero);
        }

        private static void AssertOwnedClone(PlayerActor player, RunBoons run)
        {
            var source = WeaponLoadout.LoadSelected();
            Assert.That(player.CurrentWeapon, Is.SameAs(run.RunWeapon));
            Assert.That(run.RunWeapon, Is.Not.SameAs(source));
            for (int slot = 0; slot < 4; slot++)
                Assert.That(run.RunWeapon.abilities[slot], Is.Not.SameAs(source.abilities[slot]));
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
