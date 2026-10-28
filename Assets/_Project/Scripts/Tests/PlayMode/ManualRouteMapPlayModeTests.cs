using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class ManualRouteMapPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private int _selection;
        private Mouse _originalMouse, _mouse;
        private Keyboard _originalKeyboard, _keyboard;
        private Gamepad _originalGamepad, _gamepad;
        private bool _mouseEnabled, _keyboardEnabled, _gamepadEnabled, _audioPaused;

        [SetUp] public void SetUp()
        {
            _audioPaused = AudioListener.pause;
            _selection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey, 0);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, 0);
            _originalMouse = Mouse.current; _mouseEnabled = _originalMouse != null && _originalMouse.enabled;
            _originalKeyboard = Keyboard.current; _keyboardEnabled = _originalKeyboard != null && _originalKeyboard.enabled;
            _originalGamepad = Gamepad.current; _gamepadEnabled = _originalGamepad != null && _originalGamepad.enabled;
            if (_mouseEnabled) InputSystem.DisableDevice(_originalMouse);
            if (_keyboardEnabled) InputSystem.DisableDevice(_originalKeyboard);
            if (_gamepadEnabled) InputSystem.DisableDevice(_originalGamepad);
            _mouse = InputSystem.AddDevice<Mouse>();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _gamepad = InputSystem.AddDevice<Gamepad>();
        }
        [TearDown] public void TearDown()
        {
            var pause = Object.FindFirstObjectByType<PauseMenuUI>();
            if (pause) pause.Resume();
            InputSystem.RemoveDevice(_mouse); InputSystem.RemoveDevice(_keyboard); InputSystem.RemoveDevice(_gamepad);
            if (_mouseEnabled) InputSystem.EnableDevice(_originalMouse);
            if (_keyboardEnabled) InputSystem.EnableDevice(_originalKeyboard);
            if (_gamepadEnabled) InputSystem.EnableDevice(_originalGamepad);
            PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _selection);
            Time.timeScale = 1f; AudioListener.pause = _audioPaused;
        }
        private static IEnumerator Load()
        { yield return SceneManager.LoadSceneAsync("FirstSector"); yield return null; yield return null; yield return null; }

        [UnityTest] public IEnumerator FuturePreviewHasNoEffectsAndConfirmationOpensOnlyChosenPassageWithoutTeleport()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>();
            var session = director.ManualSession;
            Vector3 position = player.transform.position; float health = player.health;
            var gates = director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy).ToArray();
            Assert.IsTrue(ui.TryInspectNode(session.Route.BossNode));
            Assert.IsFalse(ui.TryConfirmInspection(), "A future destination is an inspection, never an available action.");
            Assert.IsFalse(ui.TryInspectNode(-999));
            Assert.AreEqual(session.Route.BossNode, ui.InspectedNode);
            Assert.That(director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy), Is.EqualTo(gates));
            Assert.AreEqual(RunPhase.RouteChoice, session.Phase);
            Assert.AreEqual(-1, session.PendingDestination);
            Assert.AreEqual(health, player.health);
            Assert.AreEqual(position, player.transform.position);
            int destination = director.ManualDestinations[0];
            Assert.IsTrue(ui.TryInspectNode(destination));
            Assert.IsTrue(ui.TryConfirmInspection());
            Assert.AreEqual(RunPhase.Traversal, session.Phase);
            Assert.AreEqual(destination, session.PendingDestination);
            Assert.AreEqual(position, player.transform.position, "Confirmation opens a corridor; it must not teleport the player.");
            foreach (var edge in session.Route.Connections)
                Assert.AreEqual(edge.From != session.CurrentNode || edge.To != destination,
                    director.ManualEnvironment.Connections.Single(c => c.ConnectionId == edge.Id).Barrier.activeInHierarchy);
            Assert.IsFalse(ui.TryConfirmInspection(), "One confirmation cannot select another destination.");
        }

        [UnityTest] public IEnumerator MapShortcutBlocksTheSameWorldClickAndPreservesNavigationAcrossPause()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>();
            var pause = player.GetComponent<PauseMenuUI>();
            var controls = player.GetComponent<CharControlScript>();
            var agent = player.GetComponent<NavMeshAgent>();
            Assert.IsTrue(ui.TryChooseDestination(0));
            yield return null; yield return null;
            Vector3 destination = player.transform.position + Vector3.right * 3f;
            Assert.IsTrue(agent.SetDestination(destination));
            yield return null;
            Vector3 projected = Camera.main.WorldToScreenPoint(player.transform.position + Vector3.forward * 3f);
            Assert.Greater(projected.z, 0f);
            Vector2 screen = projected;
            // M/View now opens the read-only pause map. A simultaneous world click must
            // still be consumed before CharControlScript processes its movement input.
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen }.WithButton(MouseButton.Left));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.M));
            yield return null;
            Assert.IsTrue(pause.IsMapVisible);
            Assert.IsTrue(pause.BlocksInput);
            Assert.IsFalse(ui.IsOpen, "Consulting the pause map does not reopen the route decision.");
            Assert.AreEqual(0f, Time.timeScale);
            Assert.Less(Vector3.Distance(destination, agent.destination), .01f,
                "The same-frame UI click must not replace the world destination.");
            Assert.IsFalse(controls.TryDirectionalBasicAttack(player.transform.position + Vector3.forward * 2));
            yield return new WaitForEndOfFrame();
            Vector3 stopped = player.transform.position;
            yield return null; yield return null;
            Assert.Less(Vector3.Distance(stopped, player.transform.position), .02f,
                "The paused consultation must keep navigation stable.");
            Assert.Less(Vector3.Distance(destination, agent.destination), .01f);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return null;
            Assert.IsTrue(pause.IsPaused);
            Assert.IsFalse(pause.IsMapVisible, "Esc returns to pause home before resuming.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape));
            yield return null;
            Assert.IsFalse(pause.IsPaused);
            Assert.IsTrue(pause.BlocksInput, "The resume frame cannot replay the held world click.");
            Assert.IsFalse(controls.TryDirectionalBasicAttack(player.transform.position + Vector3.forward * 2));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null; yield return null;
            Assert.IsFalse(pause.BlocksInput);
            Assert.Less(Vector3.Distance(destination, agent.destination), .01f,
                "A world click held across inspection cannot create a new navigation order.");
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = screen });
            yield return null; yield return null;
            Assert.IsFalse(player.GetComponent<RunBoons>().BlocksManualOffense);
            Assert.IsFalse(agent.isStopped, "Pause must preserve the path's previous navigation state.");
            float resumeDeadline = Time.realtimeSinceStartup + 3f;
            while (Vector3.Distance(stopped, player.transform.position) < .5f)
            { Assert.Less(Time.realtimeSinceStartup, resumeDeadline, "The untouched path must resume after pause."); yield return null; }
            Assert.Less(Vector3.Distance(destination, agent.destination), .01f);
        }

        [UnityTest] public IEnumerator GamepadConfirmsAnAvailablePassageAndViewConsultsWithoutChangingItsDestination()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>();
            var pause = player.GetComponent<PauseMenuUI>();
            int destination = director.ManualDestinations[0];
            Assert.AreEqual(1, director.ManualDestinations.Count, "The initial decision has one available passage.");
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.DpadRight));
            yield return null;
            Assert.AreEqual(destination, ui.InspectedNode,
                "The transient decision cycles available passages; future-room consultation belongs to pause.");
            Assert.AreEqual(RunPhase.RouteChoice, director.ManualSession.Phase);
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return null;
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            Assert.AreEqual(RunPhase.Traversal, director.ManualSession.Phase);
            Assert.AreEqual(destination, director.ManualSession.PendingDestination);
            Assert.IsFalse(ui.IsOpen);
            yield return null; yield return null;
            Assert.IsFalse(ui.TryConfirmInspection(), "A held confirmation must not commit another route action.");
            // View can open read-only consultation even while confirmation is still held.
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.Select));
            yield return null;
            Assert.IsTrue(pause.IsMapVisible);
            Assert.AreEqual(0f, Time.timeScale);
            Assert.IsTrue(ui.TryInspectNode(director.ManualSession.Route.BossNode));
            Assert.IsFalse(ui.TryConfirmInspection());
            Assert.AreEqual(RunPhase.Traversal, director.ManualSession.Phase);
            Assert.AreEqual(destination, director.ManualSession.PendingDestination);
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null;
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South).WithButton(GamepadButton.Start));
            yield return null;
            Assert.IsFalse(pause.IsPaused);
            Assert.IsTrue(pause.BlocksInput);
            Assert.IsFalse(ui.TryConfirmInspection());
            InputSystem.QueueStateEvent(_gamepad, new GamepadState().WithButton(GamepadButton.South));
            yield return null; yield return null;
            Assert.IsFalse(ui.IsOpen);
            Assert.AreEqual(destination, director.ManualSession.PendingDestination);
            InputSystem.QueueStateEvent(_gamepad, new GamepadState());
            yield return null; yield return null;
            Assert.IsFalse(pause.BlocksInput);
            Assert.IsTrue(ui.TryOpenMap());
            Assert.IsTrue(pause.IsMapVisible);
            Assert.IsFalse(ui.IsOpen, "Consultation remains separate from route confirmation.");
            pause.Resume();
            yield return null;
        }

        [UnityTest] public IEnumerator RewardDecisionHandsNavigationToTheRewardAndPreservesAnExistingStopOwner()
        {
            foreach (bool previouslyStopped in new[] { false, true })
            {
                yield return Load();
                var director = Object.FindFirstObjectByType<ProgressionDirector>();
                var player = Object.FindFirstObjectByType<PlayerActor>();
                var ui = director.GetComponent<ManualRunUI>();
                var run = player.GetComponent<RunBoons>();
                var agent = player.GetComponent<NavMeshAgent>();
                int destination = director.ManualDestinations[0];
                Assert.IsTrue(ui.TryChooseDestination(0));
                Assert.IsTrue(agent.Warp(director.ManualSession.Route.Node(destination).Center));
                Assert.IsTrue(director.TryEnterNode(director.ManualSession.Id, destination));
                yield return null; yield return null;
                agent.isStopped = previouslyStopped; // A non-menu stop owner can precede the reward.
                // Deterministic resolution exercises real tickets, not combat feel.
                foreach (var enemy in director.GetComponentsInChildren<Actor>()) enemy.TakeDamage(100000f);
                float deadline = Time.realtimeSinceStartup + 4f;
                while (!run.IsChoosing)
                { Assert.Less(Time.realtimeSinceStartup, deadline, "Native completion must present the reward."); yield return null; }
                run.CloseManualReward();
                yield return null; yield return null;
                Assert.IsTrue(ui.IsOpen, "Dismissing a pending reward returns to its transient decision.");
                Assert.IsTrue(agent.isStopped, "The pending reward decision still owns a navigation stop.");
                Assert.IsTrue(run.BlocksManualOffense);
                Assert.IsTrue(ui.TryConfirmInspection());
                Assert.IsTrue(run.IsChoosing);
                yield return null; yield return null;
                Assert.IsTrue(agent.isStopped, "The reward keeps its stop after decision input releases.");
                run.CloseManualReward();
                yield return null; yield return null;
                Assert.IsTrue(ui.IsOpen);
                Assert.IsTrue(agent.isStopped, "Dismissal returns ownership to the pending decision.");

                // Finish every real ticket, then choose a passage: movement should resume
                // only after both the reward and route decision have released their stops.
                deadline = Time.realtimeSinceStartup + 4f;
                while (director.ManualSession.Phase == RunPhase.RewardPending)
                {
                    Assert.Less(Time.realtimeSinceStartup, deadline);
                    if (!run.IsChoosing) Assert.IsTrue(ui.TryConfirmInspection());
                    yield return null; yield return null;
                    Assert.IsTrue(run.Choose(0));
                    yield return null; yield return null;
                }
                Assert.AreEqual(RunPhase.RouteChoice, director.ManualSession.Phase);
                Assert.IsTrue(ui.TryChooseDestination(0));
                yield return null; yield return null;
                Assert.AreEqual(previouslyStopped, agent.isStopped,
                    "Reward/decision ownership must restore the stop state that preceded them.");
                if (previouslyStopped) continue;
                Vector3 start = player.transform.position;
                Vector3 target = start + Vector3.right * 3f;
                Assert.IsTrue(agent.SetDestination(target));
                deadline = Time.realtimeSinceStartup + 3f;
                while (Vector3.Distance(start, player.transform.position) < .5f)
                { Assert.Less(Time.realtimeSinceStartup, deadline, "Navigation must actually resume after choosing the passage."); yield return null; }
                Assert.Less(Vector3.Distance(target, agent.destination), .01f);
            }
        }

        [UnityTest] public IEnumerator PausedConfirmationKeepsDestinationResourcesAndGatesUnchanged()
        {
            yield return Load();
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var player = Object.FindFirstObjectByType<PlayerActor>();
            var ui = director.GetComponent<ManualRunUI>();
            Assert.IsTrue(ui.TryInspectNode(director.ManualDestinations[0]));
            var gates = director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy).ToArray();
            float health = player.health;
            Time.timeScale = 0f;
            Assert.IsFalse(ui.TryConfirmInspection());
            Assert.AreEqual(RunPhase.RouteChoice, director.ManualSession.Phase);
            Assert.AreEqual(-1, director.ManualSession.PendingDestination);
            Assert.AreEqual(health, player.health);
            Assert.That(director.ManualEnvironment.Connections.Select(c => c.Barrier.activeInHierarchy), Is.EqualTo(gates));
            Time.timeScale = 1f;
            Assert.IsTrue(ui.TryConfirmInspection());
        }
    }
}
