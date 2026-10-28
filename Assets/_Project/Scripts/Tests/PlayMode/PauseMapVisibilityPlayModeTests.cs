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
    public sealed class PauseMapVisibilityPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private int _selection;
        private bool _oldAudioPause;
        private Keyboard _oldKeyboard, _keyboard;
        private bool _oldKeyboardEnabled;
        [SetUp] public void Setup()
        {
            _selection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            _oldAudioPause = AudioListener.pause;
            _oldKeyboard = Keyboard.current;
            _oldKeyboardEnabled = _oldKeyboard != null && _oldKeyboard.enabled;
            if (_oldKeyboardEnabled) InputSystem.DisableDevice(_oldKeyboard);
            _keyboard = InputSystem.AddDevice<Keyboard>();
        }
        [TearDown] public void Cleanup()
        {
            InputSystem.RemoveDevice(_keyboard);
            if (_oldKeyboardEnabled) { InputSystem.EnableDevice(_oldKeyboard); _oldKeyboard.MakeCurrent(); }
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _selection);
            Time.timeScale = 1f; AudioListener.pause = _oldAudioPause;
        }
        private static IEnumerator Load()
        { yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null; }

        [UnityTest] public IEnumerator PauseMapAnnouncesRewardsButCannotCommitOrRevealAnUnvisitedRoom()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>(); var pause = player.GetComponent<PauseMenuUI>();
            var session = director.ManualSession; int next = director.ManualDestinations[0];
            Vector3 position = player.transform.position; float health = player.health, mana = player.mana;
            var gates = director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy).ToArray();
            pause.Pause(); Assert.IsTrue(pause.TryShowMap());
            Assert.IsTrue(ui.TryInspectNode(next));
            Assert.AreEqual("?", RunMapDiscoveryPresentation.NodeLabel(session, next));
            Assert.That(RunMapDiscoveryPresentation.RewardSummary(session, next), Does.Contain("Technique"));
            Assert.IsFalse(ui.TryConfirmInspection()); Assert.IsFalse(ui.TryChooseDestination(0));
            Assert.AreEqual(-1, session.PendingDestination);
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
            yield return new WaitForSecondsRealtime(.15f);
            Assert.AreEqual(position, player.transform.position);
            Assert.AreEqual(health, player.health); Assert.AreEqual(mana, player.mana);
            Assert.That(director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy), Is.EqualTo(gates));
            Assert.IsFalse(RunMapDiscoveryPresentation.IsDiscovered(session, next));
            pause.ReturnToPauseHome(); Assert.IsTrue(pause.IsPaused); Assert.IsFalse(pause.IsMapVisible);
            pause.Resume(); Assert.IsFalse(ui.TryConfirmInspection(), "The resume frame is still input-blocked.");
            yield return null; yield return null;
            Assert.IsTrue(ui.TryChooseDestination(0));
            Assert.AreEqual(position, player.transform.position, "Choosing a passage must not teleport.");
        }

        [UnityTest] public IEnumerator MapShortcutWorksDuringCombatAndEscapeReturnsThroughPauseBeforeResuming()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>(); var pause = player.GetComponent<PauseMenuUI>();
            int next = director.ManualDestinations[0];
            Assert.IsTrue(ui.TryChooseDestination(0));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(director.ManualSession.Route.Node(next).Center));
            Assert.IsTrue(director.TryEnterNode(director.ManualSession.Id, next));
            yield return null; yield return null;
            Assert.AreEqual(RunPhase.Encounter, director.ManualSession.Phase);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.M)); yield return null;
            Assert.IsTrue(pause.IsPaused); Assert.IsTrue(pause.IsMapVisible); Assert.AreEqual(0f, Time.timeScale);
            int enemies = director.ManualEnemiesRemaining;
            Assert.IsTrue(RunMapDiscoveryPresentation.IsDiscovered(director.ManualSession, next));
            Assert.IsFalse(ui.TryConfirmInspection());
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); yield return null;
            Assert.IsTrue(pause.IsPaused); Assert.IsFalse(pause.IsMapVisible);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); yield return null;
            Assert.IsFalse(pause.IsPaused); Assert.AreEqual(1f, Time.timeScale);
            Assert.AreEqual(RunPhase.Encounter, director.ManualSession.Phase);
            Assert.AreEqual(enemies, director.ManualEnemiesRemaining);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.IsFalse(player.GetComponent<AbilityHolder>().IsGamePaused);
        }

        [UnityTest] public IEnumerator PlayingHidesAcquiredUpgradesAndStageObjectiveWhileKeepingCoreHud()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>(); var ui = director.GetComponent<ManualRunUI>();
            var run = player.GetComponent<RunBoons>(); int next = director.ManualDestinations[0];
            Assert.IsTrue(ui.TryChooseDestination(0));
            Assert.IsTrue(player.GetComponent<NavMeshAgent>().Warp(director.ManualSession.Route.Node(next).Center));
            Assert.IsTrue(director.TryEnterNode(director.ManualSession.Id, next));
            yield return null; yield return null;
            foreach (var enemy in director.GetComponentsInChildren<Actor>()) enemy.TakeDamage(100000f);
            float deadline = Time.realtimeSinceStartup + 4f;
            while (director.ManualSession.Phase != RunPhase.RouteChoice)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline);
                if (run.IsChoosing) Assert.IsTrue(run.Choose(0));
                yield return null;
            }
            Assert.Greater(run.Acquired.Count, 0);
            Assert.IsTrue(ui.TryChooseDestination(0)); yield return null; yield return null;
            var rewardUI = player.GetComponent<RunRewardUI>();
            Assert.IsFalse(rewardUI.IsBuildVisible);
            var objective = (TMPro.TMP_Text)typeof(ProgressionDirector).GetField("_objective", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(director);
            Assert.IsNotNull(objective); Assert.IsFalse(objective.enabled);
            var hud = player.GetComponent<CharControlScript>().HUD;
            Assert.IsNotNull(hud); Assert.IsTrue(hud.gameObject.activeInHierarchy);
            Assert.IsTrue(hud.HealthFill.enabled); Assert.IsTrue(hud.ManaFill.enabled);
            Assert.Greater(hud.Slots.Length, 0);
            var pause = player.GetComponent<PauseMenuUI>(); pause.Pause(); Assert.IsTrue(pause.TryShowUpgrades());
            Assert.IsTrue(rewardUI.IsBuildVisible);
            Assert.Greater(run.Acquired.Count, 0, "Inspecting never consumes upgrades.");
        }
    }
}
