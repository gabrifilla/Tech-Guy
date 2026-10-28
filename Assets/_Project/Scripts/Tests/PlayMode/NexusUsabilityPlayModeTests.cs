using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace TechGuy.Tests
{
    public sealed class NexusUsabilityPlayModeTests : RunSummaryIsolatedPlayModeFixture
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private Keyboard _oldKeyboard;
        private Mouse _oldMouse;
        private bool _keyboardEnabled, _mouseEnabled;
        private PlayerActor _player;
        private int _selection;
        private bool _hadSelection;
        private readonly bool[] _hadUnlock = new bool[2];
        private readonly int[] _unlock = new int[2];
        private InputSettings.BackgroundBehavior _background;
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
        private float _timeScale;
        private bool _audio;

        [UnitySetUp] public IEnumerator Setup()
        {
            _timeScale = Time.timeScale; _audio = AudioListener.pause;
            Time.timeScale = 1; AudioListener.pause = false;
            _background = InputSystem.settings.backgroundBehavior;
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            _hadSelection = PlayerPrefs.HasKey(WeaponLoadout.PreferenceKey);
            _selection = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey);
            // Fixture-only access; prices and production unlock policy are untouched.
            for (int i = 0; i < 2; i++)
            {
                string key = "TechGuy.Loadout.Unlocked." + (i + 1);
                _hadUnlock[i] = PlayerPrefs.HasKey(key); _unlock[i] = PlayerPrefs.GetInt(key);
                PlayerPrefs.SetInt(key, 1);
            }
            _oldKeyboard = Keyboard.current; _oldMouse = Mouse.current;
            _keyboardEnabled = _oldKeyboard != null && _oldKeyboard.enabled;
            _mouseEnabled = _oldMouse != null && _oldMouse.enabled;
            if (_keyboardEnabled) InputSystem.DisableDevice(_oldKeyboard);
            if (_mouseEnabled) InputSystem.DisableDevice(_oldMouse);
            _keyboard = InputSystem.AddDevice<Keyboard>(); _mouse = InputSystem.AddDevice<Mouse>();
            yield return SceneManager.LoadSceneAsync("NexusLobby");
            yield return null; yield return null;
            _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.IsTrue(WeaponLoadout.Select(_player, 0));
            _player.GetComponent<AbilityHolder>().RefreshLoadout();
        }

        [UnityTearDown] public IEnumerator Cleanup()
        {
            InputSystem.RemoveDevice(_keyboard); InputSystem.RemoveDevice(_mouse);
            if (_keyboardEnabled && _oldKeyboard != null) InputSystem.EnableDevice(_oldKeyboard);
            if (_mouseEnabled && _oldMouse != null) InputSystem.EnableDevice(_oldMouse);
            if (_hadSelection) PlayerPrefs.SetInt(WeaponLoadout.PreferenceKey, _selection);
            else PlayerPrefs.DeleteKey(WeaponLoadout.PreferenceKey);
            for (int i = 0; i < 2; i++)
            {
                string key = "TechGuy.Loadout.Unlocked." + (i + 1);
                if (_hadUnlock[i]) PlayerPrefs.SetInt(key, _unlock[i]); else PlayerPrefs.DeleteKey(key);
            }
            PlayerPrefs.Save();
            InputSystem.settings.backgroundBehavior = _background;
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
            Time.timeScale = _timeScale; AudioListener.pause = _audio;
            yield return null;
        }

        // Production pointer raycast, collision and NavMesh movement. No Warp/SetDestination/API movement.
        private IEnumerator Walk(Vector3 destination)
        {
            var point = Camera.main.WorldToScreenPoint(destination);
            Assert.Greater(point.z, 0);
            Assert.That(point.x, Is.InRange(0, Screen.width));
            Assert.That(point.y, Is.InRange(0, Screen.height));
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point, buttons = 2 });
            yield return null;
            InputSystem.QueueStateEvent(_mouse, new MouseState { position = point });
            float deadline = Time.realtimeSinceStartup + 10;
            while (Vector3.Distance(_player.transform.position, destination) > .6f)
            {
                Assert.Less(Time.realtimeSinceStartup, deadline, "Pointer navigation failed: " + destination);
                yield return null;
            }
            yield return null;
        }

        [UnityTest] public IEnumerator PedestalKeyEquipsWithoutCastingAndDoesNotRepeatWhileHeld()
        {
            yield return Walk(new Vector3(-7, .06f, -5));
            yield return Walk(new Vector3(-8.5f, .06f, -4.8f));
            int casts = 0;
            _player.GetComponent<AbilityHolder>().AbilityUsed += slot => casts++;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null; yield return null;
            Assert.AreEqual("Bow", _player.CurrentWeapon.name, "E must equip the highlighted pedestal.");
            Assert.AreEqual(0, casts, "Interaction must not also cast a skill.");
            var equipped = _player.HandTransform.GetChild(0);
            yield return new WaitForSeconds(.3f);
            Assert.AreSame(equipped, _player.HandTransform.GetChild(0), "Holding interaction must not equip repeatedly.");
            yield return Walk(new Vector3(-4.9f, .06f, -4.8f));
            Assert.AreEqual("Bow", _player.CurrentWeapon.name, "Walking to another pedestal with E held must not equip it.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.AreEqual("Spear", _player.CurrentWeapon.name);
            Assert.AreEqual(0, casts);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            yield return SceneManager.LoadSceneAsync("NexusLobby"); yield return null; yield return null;
            _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual("Spear", _player.CurrentWeapon.name, "Loadout must survive scene reload.");
            yield return Walk(new Vector3(-7, .06f, -5));
            yield return Walk(new Vector3(-11.8f, .06f, -4.8f));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.AreEqual("Gauntlet", _player.CurrentWeapon.name);
        }

        [UnityTest] public IEnumerator ResultsBlockPedestalKeyAndKeepLoadoutUnchanged()
        {
            yield return Walk(new Vector3(-4.9f, .06f, -4.8f));
            var presenter = Object.FindFirstObjectByType<RunResultsPresenter>();
            presenter.OpenManual(); // API setup; E below is simulated input.
            Assert.IsTrue(presenter.IsOpen);
            var weapon = _player.CurrentWeapon;
            int saved = PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            yield return null; yield return null;
            Assert.AreSame(weapon, _player.CurrentWeapon, "A modal must block hidden pedestal actions.");
            Assert.AreEqual(saved, PlayerPrefs.GetInt(WeaponLoadout.PreferenceKey));
            presenter.Close(); yield return null;
            Assert.IsTrue(presenter.BlocksInput, "Held E must retain the release barrier.");
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null; yield return null;
            Assert.IsFalse(presenter.BlocksInput);
        }

        [UnityTest] public IEnumerator CombatHudIsReservedForTrainingAndPauseBlocksEquipment()
        {
            var hud = Object.FindFirstObjectByType<PlayerHUD>();
            Assert.IsFalse(hud.GetComponent<Canvas>().enabled, "Arrival must prioritize hub activities.");
            yield return Walk(new Vector3(-7, .06f, -5));
            yield return Walk(new Vector3(-10, .06f, -1));
            yield return Walk(new Vector3(-11, .06f, 2));
            Assert.IsTrue(hud.GetComponent<Canvas>().enabled, "The existing training target needs the combat HUD.");
            yield return Walk(new Vector3(-9.5f, .06f, -1));
            yield return Walk(new Vector3(-8.5f, .06f, -4.8f));
            var pause = _player.GetComponent<PauseMenuUI>();
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); yield return null;
            Assert.IsTrue(pause.IsPaused);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E)); yield return null; yield return null;
            Assert.AreEqual("Gauntlet", _player.CurrentWeapon.name);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); yield return null;
            Assert.IsFalse(pause.IsPaused);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.IsFalse(_player.GetComponent<AbilityHolder>().BlocksWorldInput);
        }

        [UnityTest] public IEnumerator PointerWalkAndInteractionStartIncursionThenAbortReturnsMatchingResults()
        {
            yield return Walk(new Vector3(-3, .06f, -4));
            yield return Walk(new Vector3(3, .06f, -1));
            yield return Walk(new Vector3(8, .06f, 3));
            yield return Walk(new Vector3(9.5f, .06f, 5.4f));
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.E));
            float deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "FirstSector")
            { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null; yield return null; yield return null;
            _player = Object.FindFirstObjectByType<PlayerActor>();
            Assert.AreEqual(RunWeaponFamily.Gauntlet, WeaponRunModifiers.Identify(_player.CurrentWeapon));
            Assert.IsTrue(Object.FindFirstObjectByType<PlayerHUD>().GetComponent<Canvas>().enabled);
            var director = Object.FindFirstObjectByType<ProgressionDirector>();
            var session = director.ManualSession.Id;
            // Lifecycle API regression, explicitly distinct from the pointer/keyboard journey above.
            Assert.IsTrue(director.TryAbortManualRun(session));
            deadline = Time.realtimeSinceStartup + 15;
            while (SceneManager.GetActiveScene().name != "NexusLobby")
            { Assert.Less(Time.realtimeSinceStartup, deadline); yield return null; }
            yield return null; yield return null;
            var results = Object.FindFirstObjectByType<RunResultsPresenter>();
            Assert.IsTrue(results.IsOpen);
            Assert.AreEqual(session.ToString(), results.Model.SessionId);
            Assert.AreEqual("Aborted", results.Model.Outcome);
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(Key.Escape)); yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState()); yield return null; yield return null;
            Assert.IsFalse(results.BlocksInput);
            _player = Object.FindFirstObjectByType<PlayerActor>();
            yield return Walk(new Vector3(-4.9f, .06f, -4.8f));
        }
    }
}
